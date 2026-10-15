"""Build reusable process-fluid, pneumatic-preparation, and separation assets."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"process_fluid";BUILT=[]

def clean():
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes,bpy.data.curves,bpy.data.cameras,bpy.data.lights,bpy.data.materials):
        for block in list(blocks):
            if block.users==0:blocks.remove(block)

def mat(name,color,metal=0,rough=.4):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;b=m.node_tree.nodes.get("Principled BSDF");b.inputs["Base Color"].default_value=(*color,1);b.inputs["Metallic"].default_value=metal;b.inputs["Roughness"].default_value=rough;return m

def finish(o,name,m=None,bevel=.004,smooth=False,collision=True):
    o.name=name
    if m:o.data.materials.append(m)
    if bevel:
        q=o.modifiers.new("Manufactured edge radius","BEVEL");q.width=bevel;q.segments=3;q.limit_method="ANGLE"
    if smooth and hasattr(o.data,"polygons"):
        for p in o.data.polygons:p.use_smooth=True
    o["rungproof_asset"]=True;o["rungproof_collision"]=collision;return o

def box(name,loc,dims,m,bevel=.004,collision=True,rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc,rotation=rotation);o=bpy.context.object;o.dimensions=dims;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m,bevel,False,collision)

def cyl(name,loc,r,depth,m,axis="Z",verts=64,collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc,rotation=rot);return finish(bpy.context.object,name,m,.002,True,collision)

def sphere(name,loc,r,m,collision=True,scale=(1,1,1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=32,radius=r,location=loc);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m,.002,True,collision)

def torus(name,loc,major,minor,m,axis="Z",collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=64,minor_segments=16,location=loc,rotation=rot);return finish(bpy.context.object,name,m,0,True,collision)

def tube(name,a,b,r,m,collision=True):
    a,b=Vector(a),Vector(b);d=b-a;o=cyl(name,(a+b)/2,r,d.length,m,"Z",48,collision);o.rotation_euler=d.to_track_quat("Z","Y").to_euler();return o

def hose(name,points,r,m,collision=False):
    curve=bpy.data.curves.new(name+"Curve","CURVE");curve.dimensions="3D";curve.resolution_u=3;curve.bevel_depth=r;curve.bevel_resolution=4
    spline=curve.splines.new("BEZIER");spline.bezier_points.add(len(points)-1)
    for bp,co in zip(spline.bezier_points,points):bp.co=co;bp.handle_left_type="AUTO";bp.handle_right_type="AUTO"
    o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target="MESH");return finish(bpy.context.object,name,m,0,True,collision)

def spur_gear(name,center,root_r,outer_r,teeth,thickness,m,phase=0):
    """Closed, extruded spur-gear silhouette with four profile points per tooth."""
    cx,cy,cz=center;profile=[]
    for tooth in range(teeth):
        base=phase+2*math.pi*tooth/teeth
        for fraction,radius in ((0.00,root_r),(0.22,outer_r),(0.78,outer_r),(1.00,root_r)):
            a=base+2*math.pi*fraction/teeth;profile.append((cx+radius*math.cos(a),cz+radius*math.sin(a)))
    n=len(profile);verts=[]
    for y in (cy-thickness/2,cy+thickness/2):verts.extend((x,y,z) for x,z in profile)
    front_center=len(verts);verts.append((cx,cy-thickness/2,cz));back_center=len(verts);verts.append((cx,cy+thickness/2,cz));faces=[]
    for i in range(n):
        j=(i+1)%n;faces.append((front_center,j,i));faces.append((back_center,n+i,n+j));faces.append((i,j,n+j,n+i))
    mesh=bpy.data.meshes.new(name+"Mesh");mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return finish(o,name,m,.002,False,False)

def text_mesh(name,body,loc,size,m,rotation=(math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rotation);o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.002;o.data.bevel_depth=.001;bpy.ops.object.convert(target="MESH");return finish(o,name,m,0,False,False)

def common():return {"steel":mat("Painted steel",(.055,.11,.14),.70,.28),"iron":mat("Cast iron blue",(.025,.18,.38),.45,.32),"ss":mat("Stainless steel",(.56,.61,.62),.86,.18),"zinc":mat("Zinc plated",(.45,.49,.49),.72,.25),"yellow":mat("Safety yellow",(.95,.53,.01),.18,.31),"red":mat("Valve red",(.68,.018,.012),.24,.29),"black":mat("Rubber black",(.012,.018,.020),.04,.68),"white":mat("Gauge white",(.87,.89,.86),.03,.38),"glass":mat("Sight glass",(.16,.42,.52),.18,.12),"copper":mat("Copper",(.58,.22,.07),.72,.24),"orange":mat("Actuator orange",(.92,.25,.02),.24,.31),"tan":mat("Filter media",(.58,.42,.21),.02,.70)}

def flange(M,name,x,r=.34,axis="X",z=.75):
    cyl(name,(x,0,z),r,.09,M["iron"],axis,64)
    ring=.76*r
    if axis=="X":
        for a in range(0,360,45):cyl(f"{name}_BOLT_{a}",(x-.052,ring*math.cos(math.radians(a)),z+ring*math.sin(math.radians(a))),.018,.12,M["zinc"],"X",20,False)

def pipe_run(M,length=2.3,r=.20,z=.75):
    cyl("PIPE",(0,0,z),r,length,M["ss"],"X",64);flange(M,"FLANGE_IN",-length/2+.05,r*1.55,z=z);flange(M,"FLANGE_OUT",length/2-.05,r*1.55,z=z)

def handwheel(M,z,r=.30,node="KIN_HANDWHEEL"):
    torus(node,(0,0,z),r,.028,M["red"],"Z")
    cyl("HANDWHEEL_HUB",(0,0,z),.055,.09,M["red"],"Z",40)
    for a in range(0,360,45):tube(f"SPOKE_{a}",(0,0,z),(r*math.cos(math.radians(a)),r*math.sin(math.radians(a)),z),.014,M["red"],False)

def motor(M,loc,axis="X",name="MOTOR"):
    x,y,z=loc;cyl(name,loc,.28,.62,M["iron"],axis,64);cyl(name+"_FAN",(x-.35,y,z),.30,.09,M["black"],axis,64);box(name+"_TERMINAL",(x,y-.25,z+.25),(.28,.22,.20),M["steel"],.018)
    for a in range(0,360,45):cyl(f"{name}_FIN_{a}",loc,.305,.45,M["iron"],axis,32,False)

def gate_valve(M):
    pipe_run(M);box("GATE_BODY",(0,0,.75),(.50,.76,.72),M["iron"],.10);box("BONNET_FLANGE",(0,0,1.12),(.58,.82,.13),M["iron"],.035);box("BONNET",(0,0,1.35),(.42,.60,.40),M["iron"],.08)
    for x in (-.21,.21):
        for y in (-.32,.32):cyl(f"BONNET_BOLT_{x}_{y}",(x,y,1.22),.022,.13,M["zinc"],"Z",20,False)
    for y in (-.24,.24):box(f"YOKE_{y}",(0,y,1.70),(.10,.08,.62),M["iron"],.010)
    cyl("KIN_STEM",(0,0,1.76),.045,.86,M["zinc"],"Z");handwheel(M,2.10,.34)

def globe_valve(M):
    pipe_run(M);sphere("GLOBE_BODY",(0,0,.76),.47,M["iron"],scale=(.85,1,1));cyl("BONNET_NECK",(0,0,1.19),.26,.36,M["iron"],"Z");cyl("KIN_STEM",(0,0,1.58),.045,.55,M["zinc"],"Z");handwheel(M,1.84,.31)
    cyl("FLOW_ARROW",(.0,-.225,.72),.055,.40,M["yellow"],"X",32,False)

def butterfly_valve(M):
    # Two short pipe stubs leave the wafer bore visible; the disc is shown partly open.
    cyl("PIPE_LEFT",(-.70,0,.75),.22,.82,M["ss"],"X",64);cyl("PIPE_RIGHT",(.70,0,.75),.22,.82,M["ss"],"X",64);flange(M,"FLANGE_IN",-1.08,.34,z=.75);flange(M,"FLANGE_OUT",1.08,.34,z=.75)
    cyl("WAFER_BODY",(0,0,.75),.47,.14,M["iron"],"X",96);disk=cyl("KIN_DISC",(0,0,.75),.34,.035,M["yellow"],"X",64);disk.rotation_euler.z+=math.radians(32);cyl("STEM",(0,0,1.12),.050,.62,M["zinc"],"Z",48)
    for a in range(0,360,45):
        y=.46*math.cos(math.radians(a));z=.75+.46*math.sin(math.radians(a));box(f"WAFER_LUG_{a}",(0,y,z),(.18,.09,.09),M["iron"],.025)
    cyl("LOCKING_QUADRANT",(0,0,1.34),.18,.045,M["zinc"],"Z",48);box("KIN_LEVER",(.31,0,1.42),(.72,.08,.08),M["red"],.018,rotation=(0,0,math.radians(12)));cyl("LEVER_GRIP",(.65,.14,1.42),.055,.17,M["black"],"Y",32)

def check_valve(M):
    pipe_run(M);sphere("SWING_CHECK_BODY",(0,0,.76),.47,M["iron"],scale=(1.0,.92,.92));cyl("BOLTED_COVER",(0,0,1.14),.38,.12,M["iron"],"Z",64)
    for x in (-.28,.28):
        for y in (-.20,.20):cyl(f"COVER_BOLT_{x}_{y}",(x,y,1.25),.025,.08,M["zinc"],"Z",24,False)
    # Side inspection cutaway exposes the defining hinged swing disc.
    cyl("INSPECTION_WINDOW",(0,-.445,.77),.30,.025,M["black"],"Y",64,False);cyl("HINGE_PIN",(-.18,-.47,1.00),.055,.12,M["zinc"],"Y",32,False)
    disk=cyl("SWING_DISC",(.03,-.475,.73),.25,.035,M["yellow"],"Y",64,False);disk.rotation_euler.x=math.radians(-18)
    tube("SWING_ARM",(-.18,-.49,1.00),(.00,-.49,.83),.025,M["zinc"],False)

def knife_gate(M):
    pipe_run(M,2.15,.21,.72);box("WAFER_BODY",(0,0,.72),(.26,.86,.92),M["iron"],.035);box("KIN_GATE_BLADE",(0,0,1.32),(.06,.62,.92),M["ss"],.006);cyl("STEM",(0,0,1.90),.04,.72,M["zinc"],"Z",48);handwheel(M,2.23,.32)
    for y in (-.34,.34):box(f"YOKE_{y}",(0,y,1.72),(.12,.10,1.35),M["iron"],.010)

def control_valve(M):
    pipe_run(M);sphere("GLOBE_CONTROL_BODY",(0,0,.76),.46,M["iron"],scale=(.85,1,1));cyl("BONNET",(0,0,1.16),.25,.30,M["iron"],"Z");cyl("KIN_STEM",(0,0,1.49),.045,.45,M["zinc"],"Z",48)
    cyl("DIAPHRAGM_LOWER",(0,0,1.64),.46,.18,M["orange"],"Z",96);cyl("DIAPHRAGM_UPPER",(0,0,1.83),.46,.18,M["orange"],"Z",96);cyl("AIR_SPRING_CAP",(0,0,1.97),.24,.17,M["orange"],"Z",64)
    box("POSITIONER",(.47,0,1.55),(.28,.34,.46),M["steel"],.030);tube("AIR_TUBE",(.47,-.18,1.60),(.28,-.18,1.85),.025,M["copper"],False)

def centrifugal_pump(M):
    box("BASE",(0,0,.10),(2.25,.84,.20),M["steel"],.015);motor(M,(-.65,0,.52));cyl("COUPLING_GUARD",(-.16,0,.52),.20,.45,M["yellow"],"X",64)
    cyl("VOLUTE",(.48,0,.52),.48,.34,M["iron"],"X",96);sphere("VOLUTE_SWELL",(.48,0,.52),.45,M["iron"],scale=(.38,1,1));cyl("SUCTION_FLANGE",(.75,0,.52),.31,.14,M["iron"],"X",64);cyl("SUCTION_NOZZLE",(1.02,0,.52),.20,.45,M["ss"],"X",64)
    cyl("DISCHARGE_NOZZLE",(.48,0,1.00),.16,.48,M["ss"],"Z",64);cyl("DISCHARGE_FLANGE",(.48,0,1.25),.27,.11,M["iron"],"Z",64)

def diaphragm_pump(M):
    box("BASE",(0,0,.08),(1.35,.90,.16),M["steel"],.015);cyl("AIR_CENTER",(0,0,.67),.25,.58,M["orange"],"Z",64)
    for x in (-.43,.43):
        sphere(f"DIAPHRAGM_{x}",(x,0,.67),.38,M["iron"],scale=(.35,1,1));cyl(f"COVER_{x}",(x,0,.67),.35,.12,M["iron"],"X",80)
        for a in range(0,360,45):cyl(f"COVER_BOLT_{x}_{a}",(x+(-.07 if x<0 else .07),.27*math.cos(math.radians(a)),.67+.27*math.sin(math.radians(a))),.018,.11,M["zinc"],"X",18,False)
    for z,n in ((.26,"INLET"),(1.08,"OUTLET")):tube(n+"_MANIFOLD",(-.43,0,z),(.43,0,z),.11,M["ss"]);cyl(n+"_FLANGE",(0,0,z),.22,.10,M["iron"],"Y",64)

def gear_pump(M):
    # Compact hydraulic gear pump shown as a training cutaway, without a motor/reducer silhouette.
    box("BASE",(0,0,.08),(2.20,1.00,.16),M["steel"],.012);box("GEAR_PUMP_BODY",(0,0,.55),(.82,.78,.82),M["iron"],.08)
    # Input shaft is coaxial with the left gear (Y axis) and exits the rear cover.
    cyl("DRIVE_MOUNT_FLANGE",(-.17,.47,.55),.30,.14,M["iron"],"Y",64);cyl("KIN_PUMP_SHAFT",(-.17,.70,.55),.065,.42,M["zinc"],"Y",40);box("SHAFT_KEY",(-.15,.83,.61),(.04,.18,.04),M["yellow"],.004,False)
    # Opposed side ports and modest hoses leave the front cutaway unobstructed.
    for i,x in enumerate((-.52,.52)):
        cyl(f"PORT_{i}",(x,.10,.55),.105,.24,M["ss"],"X",48);cyl(f"PORT_COLLAR_{i}",(x+(-.14 if x<0 else .14),.10,.55),.17,.08,M["iron"],"X",48)
        side=-1 if x<0 else 1;hose(f"PROCESS_HOSE_{i}",[(x+side*.18,.10,.55),(x+side*.38,.10,.55),(x+side*.56,.18,.36)],.035,M["glass"])
        box(f"HEX_FITTING_{i}",(x+side*.18,.10,.55),(.16,.20,.20),M["zinc"],.015)
    for x in (-.28,.28):
        for z in (.25,.85):cyl(f"BODY_BOLT_{x}_{z}",(x,-.40,z),.022,.08,M["zinc"],"Y",20,False)
    # Open training cover shows the two externally meshing gears, removing gearbox ambiguity.
    box("CUTAWAY_WINDOW",(0,-.402,.55),(.66,.018,.64),M["black"],.012,False)
    spur_gear("PUMP_GEAR_DRIVE",(-.17,-.43,.55),.115,.180,12,.055,M["yellow"],0)
    spur_gear("PUMP_GEAR_DRIVEN",(.17,-.43,.55),.115,.180,12,.055,M["yellow"],math.pi/12)
    cyl("DRIVE_GEAR_HUB",(-.17,-.475,.55),.050,.075,M["zinc"],"Y",32,False)
    cyl("DRIVEN_GEAR_HUB",(.17,-.475,.55),.050,.075,M["zinc"],"Y",32,False)
    # A bolted cutaway frame keeps the gears visually inside the pump casing.
    box("WINDOW_TOP",(0,-.45,.85),(.76,.05,.06),M["iron"],.010,False);box("WINDOW_BOTTOM",(0,-.45,.25),(.76,.05,.06),M["iron"],.010,False)
    box("WINDOW_LEFT",(-.35,-.45,.55),(.06,.05,.66),M["iron"],.010,False);box("WINDOW_RIGHT",(.35,-.45,.55),(.06,.05,.66),M["iron"],.010,False)
    for x in (-.35,.35):
        for z in (.25,.85):cyl(f"WINDOW_BOLT_{x}_{z}",(x,-.49,z),.018,.05,M["zinc"],"Y",18,False)
    for x in (-.28,.28):box(f"PUMP_FOOT_{x}",(x,0,.18),(.20,.64,.12),M["iron"],.015)
    # Discharge gauge and inlet strainer establish installed pump service context.
    tube("GAUGE_LINE",(.52,.10,.67),(.68,.10,.92),.025,M["copper"],False);cyl("PRESSURE_GAUGE",(.68,-.02,1.02),.16,.07,M["steel"],"Y",48,False);cyl("PRESSURE_FACE",(.68,-.06,1.02),.135,.012,M["white"],"Y",48,False);box("GAUGE_NEEDLE",(.68,-.073,1.02),(.015,.015,.10),M["red"],.002,False,rotation=(math.radians(35),0,0))
    cyl("SUCTION_STRAINER",(-1.08,.18,.36),.13,.28,M["zinc"],"X",48);torus("STRAINER_RING",(-1.23,.18,.36),.13,.018,M["iron"],"X",False)
    # Hydraulic power-unit context with motor aligned to the drive gear's Y-axis shaft.
    box("OIL_RESERVOIR",(0,0,-.22),(2.30,1.55,.52),M["steel"],.035);box("RESERVOIR_TOP",(0,0,.06),(2.36,1.61,.10),M["iron"],.015)
    cyl("PUMP_MOTOR",(-.17,.98,.55),.27,.62,M["iron"],"Y",64);cyl("MOTOR_FAN",(-.17,1.33,.55),.29,.09,M["black"],"Y",64);box("MOTOR_TERMINAL",(-.17,1.00,.84),(.26,.24,.20),M["steel"],.018);box("COUPLING_GUARD",(-.17,.62,.55),(.42,.34,.42),M["yellow"],.07)
    cyl("BREATHER_FILL",(-.78,.35,.18),.12,.24,M["black"],"Z",40);box("LEVEL_GAUGE",(.88,-.79,-.20),(.14,.04,.34),M["glass"],.010,False)
    hose("SUCTION_TO_TANK",[(-.70,.18,.36),(-.88,.18,.14),(-.72,.18,-.02)],.055,M["glass"]);hose("RETURN_TO_TANK",[(.70,.18,.36),(.92,.30,.16),(.72,.30,-.02)],.045,M["glass"])
    text_mesh("TRAINER_NAMEPLATE","EXTERNAL GEAR PUMP  TRAINER",(0,-.786,-.25),.095,M["white"]);text_mesh("INLET_MARK","IN",(-.72,-.786,.10),.075,M["white"]);text_mesh("OUTLET_MARK","OUT",(.72,-.786,.10),.075,M["white"])

def gear_pump_catalog(M):
    """Manufacturer-grounded bushing-block external gear pump form."""
    # Pump axis X: SAE two-bolt mount and keyed shaft at the drive end.
    box("SAE_MOUNT_FLANGE",(-.62,0,.58),(.14,1.02,.88),M["iron"],.08)
    for y in (-.39,.39):cyl(f"MOUNT_HOLE_{y}",(-.70,y,.58),.075,.17,M["black"],"X",32,False)
    cyl("FRONT_BEARING_BOSS",(-.73,0,.58),.28,.24,M["iron"],"X",64);cyl("KIN_PUMP_SHAFT",(-1.00,0,.58),.075,.46,M["zinc"],"X",40);box("SHAFT_KEY",(-1.12,-.02,.645),(.20,.04,.04),M["yellow"],.004,False)
    # Three-piece pressure-balanced housing: end cover, gear section, rear cover.
    box("FRONT_COVER",(-.38,0,.58),(.34,.74,.72),M["iron"],.07);box("GEAR_SECTION",(.08,0,.58),(.58,.72,.70),M["ss"],.045);box("REAR_COVER",(.48,0,.58),(.26,.74,.72),M["iron"],.07)
    # Four longitudinal through bolts are a defining external-gear-pump cue.
    for y in (-.31,.31):
        for z in (.31,.85):
            cyl(f"THRU_BOLT_{y}_{z}",(.03,y,z),.025,1.02,M["zinc"],"X",24,False);cyl(f"BOLT_HEAD_{y}_{z}",(-.51,y,z),.052,.07,M["zinc"],"X",24,False);cyl(f"BOLT_NUT_{y}_{z}",(.61,y,z),.052,.07,M["zinc"],"X",24,False)
    # Unequal side ports communicate suction versus pressure service.
    cyl("SUCTION_PORT",(.10,-.47,.58),.17,.28,M["ss"],"Y",64);cyl("SUCTION_FLANGE",(.10,-.63,.58),.25,.10,M["iron"],"Y",64)
    cyl("PRESSURE_PORT",(.10,.45,.58),.115,.25,M["ss"],"Y",48);cyl("PRESSURE_FLANGE",(.10,.60,.58),.19,.09,M["iron"],"Y",48)
    for y,side,r in ((-.72,-1,.065),(.69,1,.050)):
        hose(f"HYDRAULIC_LINE_{side}",[(.10,y,.58),(.10,y+side*.20,.58),(.28,y+side*.34,.38)],r,M["glass"])
    box("PUMP_FOOT",(.10,0,.12),(1.20,.72,.14),M["steel"],.015)
    # Installed drive context separates a pump package from encoders and reducers.
    box("SKID_BASE",(-.65,0,.04),(3.40,1.35,.12),M["steel"],.018);motor(M,(-1.65,0,.58),"X","PUMP_MOTOR");box("COUPLING_GUARD",(-1.13,0,.58),(.48,.48,.48),M["yellow"],.08)
    cyl("SUCTION_STRAINER",(.28,-1.08,.38),.13,.30,M["zinc"],"Y",48);torus("STRAINER_RING",(.28,-1.24,.38),.13,.018,M["iron"],"Y",False)
    tube("GAUGE_LINE",(.10,.78,.58),(.34,.86,.88),.025,M["copper"],False);cyl("PRESSURE_GAUGE",(.34,.82,.99),.16,.07,M["steel"],"Y",48,False);cyl("PRESSURE_FACE",(.34,.78,.99),.135,.012,M["white"],"Y",48,False);box("GAUGE_NEEDLE",(.34,.765,.99),(.015,.015,.10),M["red"],.002,False,rotation=(math.radians(35),0,0))

def frl(M):
    box("WALL_BRACKET",(0,.12,1.00),(1.42,.12,.18),M["steel"],.012)
    xs=(-.48,0,.48)
    for x,label in zip(xs,("FILTER","REGULATOR","LUBRICATOR")):
        cyl(label+"_HEAD",(x,0,1.02),.22,.30,M["iron"],"Z",64);cyl(label+"_BOWL",(x,0,.64),.18,.48,M["glass"],"Z",64)
        torus(label+"_GUARD_TOP",(x,0,.84),.19,.018,M["zinc"]);torus(label+"_GUARD_BOTTOM",(x,0,.44),.19,.018,M["zinc"])
        for a in range(0,360,45):tube(f"{label}_GUARD_{a}",(x+.19*math.cos(math.radians(a)),.19*math.sin(math.radians(a)),.44),(x+.19*math.cos(math.radians(a)),.19*math.sin(math.radians(a)),.84),.012,M["zinc"],False)
    tube("AIR_MANIFOLD",(-.70,0,1.02),(.70,0,1.02),.10,M["ss"]);cyl("KIN_PRESSURE_KNOB",(0,0,1.31),.12,.20,M["black"],"Z",48)
    cyl("GAUGE_BODY",(0,-.30,1.12),.20,.08,M["steel"],"Y",64);cyl("GAUGE_FACE",(0,-.345,1.12),.17,.012,M["white"],"Y",64,False);cyl("GAUGE_NEEDLE",(0,-.355,1.12),.012,.19,M["red"],"Z",16,False)
    cyl("FILTER_DRAIN",(-.48,0,.34),.035,.16,M["black"],"Z",24);cyl("LUBRICATOR_FILL",(.48,0,1.25),.07,.12,M["zinc"],"Z",32);sphere("OIL_DRIP_DOME",(.48,-.20,1.18),.075,M["glass"],False,scale=(1,.55,1))

def valve_manifold(M):
    box("DIN_BASE",(0,0,.18),(1.90,.62,.20),M["steel"],.015)
    for i,x in enumerate([-.72,-.48,-.24,0,.24,.48,.72]):
        box(f"VALVE_SLICE_{i}",(x,0,.48),(.20,.54,.52),M["iron"],.018);box(f"SOLENOID_{i}",(x,.08,.83),(.16,.30,.22),M["black"],.015);box(f"DIN_PLUG_{i}",(x,.16,1.01),(.13,.20,.12),M["steel"],.010);cyl(f"LED_{i}",(x,-.16,.88),.025,.025,M["yellow"],"Y",20,False)
        for y in (-.18,.18):
            cyl(f"PUSHIN_PORT_{i}_{y}",(x,y,.78),.040,.08,M["zinc"],"Z",24);cyl(f"TUBE_COLLAR_{i}_{y}",(x,y,.84),.052,.035,M["orange"],"Z",24)
        cyl(f"MANUAL_OVERRIDE_{i}",(x,-.285,.83),.020,.025,M["red"],"Y",16,False)
    box("END_BLOCK_LEFT",(-.94,0,.48),(.22,.56,.60),M["orange"],.025);box("END_BLOCK_RIGHT",(.94,0,.48),(.22,.56,.60),M["orange"],.025);cyl("SUPPLY_PORT",(-1.08,0,.48),.11,.20,M["ss"],"X",48)
    for x in (-.94,.94):cyl(f"EXHAUST_MUFFLER_{x}",(x,-.40,.42),.09,.22,M["tan"],"Y",32)
    box("FIELDBUS_NODE",(-1.18,.18,.88),(.28,.30,.38),M["steel"],.025);cyl("M12_NETWORK",(-1.18,.02,1.05),.055,.08,M["black"],"Y",24)
    hose("MAIN_AIR_SUPPLY",[(-1.10,0,.48),(-1.32,-.18,.52),(-1.48,-.36,.30)],.055,M["glass"])

def heat_exchanger(M):
    cyl("SHELL",(0,0,1.00),.48,2.55,M["ss"],"X",96);cyl("HEAD_LEFT",(-1.31,0,1.00),.52,.28,M["iron"],"X",96);cyl("HEAD_RIGHT",(1.31,0,1.00),.52,.28,M["iron"],"X",96)
    for x in (-1.16,1.16):cyl(f"TUBESHEET_{x}",(x,0,1.00),.58,.10,M["iron"],"X",96)
    for x,z in ((-.70,1.49),(.70,.51)):cyl(f"SHELL_NOZZLE_{x}",(x,0,z),.15,.40,M["ss"],"Z",48);cyl(f"SHELL_FLANGE_{x}",(x,0,z+(.22 if z>1 else -.22)),.25,.09,M["iron"],"Z",64)
    for x in (-1.48,1.48):cyl(f"TUBE_NOZZLE_{x}",(x,0,1.00),.21,.38,M["ss"],"X",64);cyl(f"TUBE_FLANGE_{x}",(x+(-.22 if x<0 else .22),0,1.00),.31,.09,M["iron"],"X",64)
    for x in (-.78,.78):box(f"SADDLE_{x}",(x,0,.42),(.18,.82,.55),M["steel"],.025);box(f"FOOT_{x}",(x,0,.08),(.58,.94,.12),M["steel"],.015)

def cyclone(M):
    cyl("CYCLONE_BARREL",(0,0,1.65),.62,1.18,M["ss"],"Z",96);bpy.ops.mesh.primitive_cone_add(vertices=96,radius1=.13,radius2=.62,depth=1.35,location=(0,0,.39));finish(bpy.context.object,"CYCLONE_CONE",M["ss"],.003,True)
    cyl("VORTEX_FINDER",(0,0,2.45),.20,.62,M["ss"],"Z",64);cyl("TOP_FLANGE",(0,0,2.74),.31,.10,M["iron"],"Z",64);tube("TANGENTIAL_INLET",(-.95,-.34,2.02),(-.35,-.34,2.02),.20,M["ss"]);box("INLET_TRANSITION",(-.45,-.34,2.02),(.48,.38,.42),M["ss"],.035)
    cyl("DUST_AIRLOCK",(0,0,-.37),.22,.30,M["iron"],"Z",64)
    for a in (30,150,270):
        x=.72*math.cos(math.radians(a));y=.72*math.sin(math.radians(a));tube(f"LEG_{a}",(x,y,1.55),(x,y,-.52),.055,M["steel"]);box(f"FOOT_{a}",(x,y,-.57),(.24,.24,.10),M["steel"],.010)

def baghouse(M):
    box("FILTER_HOUSING",(0,0,1.65),(1.45,1.05,1.75),M["steel"],.045);box("CLEAN_AIR_PLENUM",(0,0,2.68),(1.52,1.12,.36),M["iron"],.045)
    for x in (-.45,0,.45):
        box(f"ACCESS_DOOR_{x}",(x,-.535,1.70),(.38,.045,1.20),M["iron"],.018,False);cyl(f"DOOR_LATCH_{x}",(x+.12,-.57,1.70),.025,.07,M["zinc"],"Y",20,False);cyl(f"PULSE_VALVE_{x}",(x,-.72,2.72),.09,.22,M["orange"],"Y",36)
        tube(f"PULSE_DROP_{x}",(x,-.72,2.68),(x,-.72,2.34),.035,M["copper"],False)
    tube("COMPRESSED_AIR_HEADER",(-.68,-.72,2.86),(.68,-.72,2.86),.075,M["copper"],False)
    bpy.ops.mesh.primitive_cone_add(vertices=64,radius1=.18,radius2=.70,depth=.85,location=(0,0,.35));finish(bpy.context.object,"DUST_HOPPER",M["steel"],.003,True)
    tube("DIRTY_AIR_INLET",(-1.08,0,1.35),(-.72,0,1.35),.25,M["ss"]);tube("CLEAN_AIR_OUTLET",(.72,0,2.68),(1.15,0,2.68),.24,M["ss"])
    cyl("ROTARY_AIRLOCK",(0,0,-.17),.24,.42,M["orange"],"Y",48);cyl("AIRLOCK_MOTOR",(.42,0,-.17),.15,.38,M["iron"],"X",48)
    for x in (-.58,.58):
        for y in (-.42,.42):tube(f"LEG_{x}_{y}",(x,y,1.18),(x,y,-.36),.06,M["steel"]);box(f"FOOT_{x}_{y}",(x,y,-.41),(.22,.22,.10),M["steel"],.010)

BUILDERS={"flanged_gate_valve":gate_valve,"flanged_globe_valve":globe_valve,"wafer_butterfly_valve":butterfly_valve,"swing_check_valve":check_valve,"knife_gate_valve":knife_gate,"pneumatic_control_valve":control_valve,"end_suction_centrifugal_pump":centrifugal_pump,"air_operated_double_diaphragm_pump":diaphragm_pump,"external_gear_pump":gear_pump,"filter_regulator_lubricator":frl,"pneumatic_valve_manifold":valve_manifold,"shell_tube_heat_exchanger":heat_exchanger,"cyclone_separator":cyclone,"pulse_jet_dust_collector":baghouse}

def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat("-Z","Y").to_euler()
def save(slug,builder):
    clean();M=common();builder(M);root=BASE/slug
    for p in ("source","delivery","collision","review"):(root/p).mkdir(parents=True,exist_ok=True)
    for p in (root/"source",root/"review"):(p/".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"))
    objects=[o for o in bpy.context.scene.objects if o.type=="MESH" and o.get("rungproof_asset")]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    coll=[o for o in objects if o.get("rungproof_collision")];mins=Vector((1e9,1e9,1e9));maxs=Vector((-1e9,-1e9,-1e9))
    for o in coll:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c);mins.x,mins.y,mins.z=min(mins.x,w.x),min(mins.y,w.y),min(mins.z,w.z);maxs.x,maxs.y,maxs.z=max(maxs.x,w.x),max(maxs.y,w.y),max(maxs.z,w.z)
    bpy.ops.object.select_all(action="DESELECT");bpy.ops.mesh.primitive_cube_add(location=(mins+maxs)/2);proxy=bpy.context.object;proxy.name="COLLISION_primary";proxy.dimensions=maxs-mins;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bpy.ops.export_scene.gltf(filepath=str(root/"collision"/f"{slug}_collision.glb"),export_format="GLB",use_selection=True,export_apply=True);bpy.data.objects.remove(proxy,do_unlink=True)
    vmin=Vector((1e9,1e9,1e9));vmax=Vector((-1e9,-1e9,-1e9))
    for o in objects:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c);vmin.x,vmin.y,vmin.z=min(vmin.x,w.x),min(vmin.y,w.y),min(vmin.z,w.z);vmax.x,vmax.y,vmax.z=max(vmax.x,w.x),max(vmax.y,w.y),max(vmax.z,w.z)
    floor=box("REVIEW_floor",(0,0,mins.z-.035),(max(4,vmax.x-vmin.x+1.5),max(4,vmax.y-vmin.y+1.5),.05),M["steel"],.002,False);floor["rungproof_asset"]=False
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034);center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.15,2.3)
    for i,(loc,e,s) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for i,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.46);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    primary_review=(root/"review"/f"{slug}_01.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)

flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("PROCESS_FLUID_ASSETS_BUILT",len(BUILT))
