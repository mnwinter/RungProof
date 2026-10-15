"""Build reusable material-flow and bulk-handling equipment families."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"material_flow";BUILT=[]

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

def torus(name,loc,major,minor,m,axis="Z",collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=64,minor_segments=12,location=loc,rotation=rot);return finish(bpy.context.object,name,m,0,True,collision)

def tube(name,a,b,r,m,collision=True):
    a,b=Vector(a),Vector(b);d=b-a;o=cyl(name,(a+b)/2,r,d.length,m,"Z",48,collision);o.rotation_euler=d.to_track_quat("Z","Y").to_euler();return o

def u_trough(name,length,radius,loc,m):
    # Open-top half-cylinder trough along X, with real inner/outer shell thickness.
    verts=[];faces=[];segments=32;thickness=.035;x0,x1=loc[0]-length/2,loc[0]+length/2
    for x in (x0,x1):
        for r in (radius,radius-thickness):
            for i in range(segments+1):
                a=math.pi+i*math.pi/segments;verts.append((x,loc[1]+r*math.cos(a),loc[2]+r*math.sin(a)))
    ring=segments+1
    for end in range(2):
        base=end*2*ring
        for i in range(segments):faces.append((base+i,base+i+1,base+ring+i+1,base+ring+i))
    for shell in range(2):
        a0=shell*ring;a1=2*ring+shell*ring
        for i in range(segments):faces.append((a0+i,a1+i,a1+i+1,a0+i+1))
    for i in (0,segments):faces.append((i,2*ring+i,3*ring+i,ring+i))
    mesh=bpy.data.meshes.new(name+"Mesh");mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return finish(o,name,m,.003,True)

def helical_flight(name,length,inner,outer,turns,loc,m):
    verts=[];faces=[];steps=turns*32
    for i in range(steps+1):
        t=i/steps;x=loc[0]-length/2+length*t;a=t*turns*2*math.pi
        for r in (inner,outer):verts.append((x,loc[1]+r*math.cos(a),loc[2]+r*math.sin(a)))
    for i in range(steps):faces.append((i*2,i*2+1,i*2+3,i*2+2))
    mesh=bpy.data.meshes.new(name+"Mesh");mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return finish(o,name,m,.002,True,False)

def text(name,body,loc,size,m,rotation=(-math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rotation);o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.002;o.data.bevel_depth=.001
    if rotation==(-math.pi/2,0,0):o.scale.x=-1
    bpy.ops.object.convert(target="MESH");return finish(o,name,m,0,False,False)

def common():return {"steel":mat("Painted steel",(.055,.12,.16),.72,.29),"zinc":mat("Zinc plated",(.48,.52,.52),.75,.26),"blue":mat("Industrial blue",(.025,.22,.55),.28,.30),"yellow":mat("Safety yellow",(.95,.55,.012),.18,.32),"red":mat("Safety red",(.68,.015,.01),.18,.31),"black":mat("Rubber black",(.012,.018,.020),.04,.66),"white":mat("Label white",(.85,.88,.88),.05,.42),"alum":mat("Aluminum",(.59,.63,.64),.80,.22),"green":mat("Modular belt green",(.025,.28,.12),.04,.58),"carton":mat("Carton",(.48,.27,.10),.01,.72),"brass":mat("Brass",(.67,.48,.16),.70,.24)}

def motor(M,loc,prefix="DRIVE",axis="X"):
    x,y,z=loc;cyl(prefix+"_MOTOR",loc,.18,.48,M["blue"],axis,64);cyl(prefix+"_FAN",(x-.27 if axis=="X" else x,y,z),.19,.08,M["black"],axis,64);box(prefix+"_TERMINAL",(x,y-.18,z+.18),(.22,.20,.16),M["steel"],.018)

def legs(M,length,width,height=.72):
    for x in (-length*.40,length*.40):
        for y in (-width*.46,width*.46):box(f"LEG_{x}_{y}",(x,y,height/2),(.07,.07,height),M["steel"],.004)
        box(f"CROSS_{x}",(x,0,height*.45),(.08,width,.07),M["steel"],.004)
        for y in (-width*.46,width*.46):box(f"FOOT_{x}_{y}",(x,y,.035),(.20,.16,.07),M["zinc"],.006)

def gravity_roller(M):
    L,W,Z=2.4,.78,.88
    for y in (-W/2,W/2):box(f"SIDE_{y}",(0,y,Z),(L,.10,.25),M["steel"],.008)
    for i,x in enumerate([-.98+i*.14 for i in range(15)]):cyl(f"ROLLER_{i}",(x,0,Z+.07),.045,W-.10,M["zinc"],"Y",48)
    legs(M,L,W,Z-.12);box("CARTON",(.30,0,1.16),(.50,.55,.48),M["carton"],.025,False)

def chain_pallet(M):
    L,W,Z=2.6,1.05,.82
    for y in (-.31,.31):
        box(f"CHAIN_RAIL_{y}",(0,y,Z),(L,.16,.18),M["steel"],.008)
        for i,x in enumerate([-.98+i*.16 for i in range(13)]):box(f"KIN_CHAIN_SLAT_{y}_{i}",(x,y,Z+.12),(.12,.18,.055),M["black"],.006)
    for x in (-1.18,1.18):
        for y in (-.31,.31):cyl(f"SPROCKET_{x}_{y}",(x,y,Z+.04),.10,.15,M["zinc"],"Y",24)
    legs(M,L,W,Z-.12);motor(M,(1.12,-.58,.64),"CHAIN_DRIVE")
    # True block pallet with fork openings, lower deck, three stringers and cargo.
    for y in (-.38,0,.38):box(f"PALLET_STRINGER_{y}",(0,y,1.02),(1.10,.12,.12),M["carton"],.008,False)
    for x in (-.45,-.22,0,.22,.45):box(f"PALLET_DECK_{x}",(x,0,1.14),(.16,1.00,.08),M["carton"],.006,False)
    for x in (-.42,0,.42):box(f"PALLET_BOTTOM_{x}",(x,0,.94),(.18,1.00,.06),M["carton"],.006,False)
    box("PALLETIZED_LOAD",(0,0,1.52),(.72,.72,.66),M["carton"],.025,False)

def slat_conveyor(M):
    L,W,Z=2.5,.82,.80
    box("FRAME",(0,0,Z-.05),(L,W,.30),M["steel"],.018)
    for i,x in enumerate([-1.10+i*.11 for i in range(21)]):box(f"KIN_SLAT_{i}",(x,0,Z+.15),(.095,W-.08,.055),M["zinc"],.005)
    for x in (-1.10,1.10):cyl(f"CHAIN_SHAFT_{x}",(x,0,Z),.11,W-.06,M["black"],"Y",32)
    legs(M,L,W,Z-.20);motor(M,(1.05,-.62,.55),"SLAT_DRIVE");box("WORKPIECE",(.25,0,1.07),(.45,.45,.28),M["alum"],.015,False)

def modular_belt(M):
    L,W,Z=2.5,.85,.82
    for y in (-.43,.43):box(f"SIDE_{y}",(0,y,Z),(L,.08,.28),M["alum"],.008)
    for i,x in enumerate([-1.10+i*.10 for i in range(23)]):
        for j,y in enumerate((-.30,-.15,0,.15,.30)):box(f"KIN_BELT_LINK_{i}_{j}",(x,y,Z+.12),(.085,.135,.035),M["green"],.004)
    legs(M,L,W,Z-.12);motor(M,(1.05,-.62,.58),"BELT_DRIVE")
    for y in (-.33,.33):box(f"GUIDE_{y}",(0,y,Z+.35),(L,.035,.36),M["white"],.006)

def popup_transfer(M):
    L,W,Z=2.1,1.15,.82
    for y in (-.47,.47):box(f"ROLLER_SIDE_{y}",(0,y,Z),(L,.09,.25),M["steel"],.008)
    for i,x in enumerate([-.85+i*.17 for i in range(11)]):cyl(f"ROLLER_{i}",(x,0,Z+.08),.045,W-.12,M["zinc"],"Y",40)
    # Chains run along X, perpendicular to the Y-axis conveyor rollers; no rotary table.
    for y in (-.28,0,.28):
        box(f"KIN_POPUP_CHAIN_{y}",(.0,y,Z+.21),(1.72,.12,.10),M["yellow"],.008)
        for i,x in enumerate([-.76+i*.12 for i in range(13)]):box(f"CHAIN_PAD_{y}_{i}",(x,y,Z+.275),(.09,.11,.035),M["black"],.004)
        for x in (-.80,.80):cyl(f"CHAIN_SPROCKET_{y}_{x}",(x,y,Z+.22),.075,.11,M["black"],"Y",20)
    box("LIFT_FRAME",(0,0,Z-.16),(1.65,.86,.12),M["blue"],.010);cyl("KIN_LIFT_CYLINDER",(0,0,.42),.10,.58,M["blue"],"Z",64)
    for x in (-.55,.55):cyl(f"LIFT_GUIDE_{x}",(x,0,.48),.035,.45,M["zinc"],"Z",48)
    legs(M,L,W,Z-.12);text("TRANSFER_LABEL","90 DEG TRANSFER",(0,-.516,Z-.02),.065,M["white"],(math.pi/2,0,0))

def vertical_lift(M):
    H,W,D=2.6,1.25,.95
    for x in (-W/2,W/2):box(f"MAST_{x}",(x,0,H/2),(.12,.16,H),M["steel"],.010)
    for z in (.18,1.25,2.32):box(f"MAST_BRACE_{z}",(0,0,z),(W,.14,.10),M["steel"],.006)
    platform=box("KIN_LIFT_CARRIAGE",(0,.05,1.05),(1.05,.85,.16),M["yellow"],.015)
    for i,x in enumerate([-.40+i*.13 for i in range(7)]):cyl(f"CARRIAGE_ROLLER_{i}",(x,.05,1.15),.035,.72,M["zinc"],"Y",32,False)
    for x in (-.48,.48):
        box(f"CAGE_POST_{x}",(x,.34,1.62),(.05,.05,1.05),M["yellow"],.004,False)
        box(f"CAGE_POST_REAR_{x}",(x,-.34,1.62),(.05,.05,1.05),M["yellow"],.004,False)
    for z in (1.38,1.86,2.12):
        box(f"CAGE_RAIL_FRONT_{z}",(0,.34,z),(1.0,.04,.04),M["yellow"],.003,False)
        box(f"CAGE_RAIL_REAR_{z}",(0,-.34,z),(1.0,.04,.04),M["yellow"],.003,False)
    for x in (-.48,.48):
        for z in (.92,1.18):cyl(f"GUIDE_WHEEL_{x}_{z}",(x,-.08,z),.07,.08,M["black"],"Y",32)
    for x in (-.48,.48):box(f"CARRIAGE_RAIL_{x}",(x,.05,1.52),(.06,.06,.80),M["steel"],.004)
    box("TOP_DRIVE",(0,0,2.55),(.78,.50,.22),M["blue"],.030);motor(M,(0,-.38,2.55),"LIFT_DRIVE","Y")
    for x in (-.38,.38):tube(f"CHAIN_{x}",(x,0,2.43),(x,0,1.14),.018,M["black"],False)
    for x in (-W/2,W/2):box(f"FOOT_{x}",(x,0,.05),(.40,.65,.10),M["steel"],.010)
    text("VRC_LABEL","VERTICAL CONVEYOR",(0,-.091,2.35),.060,M["white"],(math.pi/2,0,0))

def screw_conveyor(M):
    L,Z=2.7,.74
    u_trough("OPEN_U_TROUGH",L,.34,(0,0,Z+.18),M["alum"])
    cyl("KIN_SCREW_SHAFT",(0,0,Z),.055,2.45,M["steel"],"X",64)
    helical_flight("AUGER_HELICAL_FLIGHT",2.35,.065,.28,8,(0,0,Z),M["zinc"])
    for x in (-1.20,1.20):cyl(f"END_BEARING_{x}",(x,0,Z),.34,.10,M["steel"],"X",64)
    motor(M,(-1.55,0,Z),"SCREW_DRIVE");box("INLET_HOPPER",(.80,0,1.26),(.55,.55,.42),M["yellow"],.025)
    box("PARTIAL_COVER",(.68,0,1.08),(.72,.70,.06),M["steel"],.010)
    legs(M,L,.72,Z-.18)

def bucket_elevator(M):
    H,W=.0+2.75,.78
    box("LEG_CASING",(0,0,1.35),(W,.52,2.45),M["alum"],.035)
    box("BOOT",(0,0,.18),(1.02,.78,.36),M["steel"],.045);box("HEAD",(0,0,2.62),(1.05,.80,.42),M["steel"],.055)
    # Inspection opening shows belt and buckets while casing remains credible.
    box("INSPECTION_WINDOW",(0,-.271,1.38),(.54,.018,1.45),M["black"],.010,False)
    box("KIN_BUCKET_BELT",(0,-.285,1.38),(.20,.025,1.42),M["black"],.002,False)
    for i,z in enumerate([.78+i*.24 for i in range(6)]):
        box(f"BUCKET_{i}",(0,-.34,z),(.46,.22,.13),M["yellow"],.020,False,rotation=(math.radians(-12),0,0))
    box("INLET_CHUTE",(-.70,0,.32),(.55,.58,.26),M["yellow"],.025);box("DISCHARGE_CHUTE",(.70,0,2.60),(.60,.56,.28),M["yellow"],.025)
    motor(M,(0,-.62,2.63),"HEAD_DRIVE","Y");text("ELEVATOR_LABEL","BUCKET ELEVATOR",(0,-.286,2.04),.065,M["white"],(math.pi/2,0,0))

def vibratory_bowl(M):
    cyl("VIBRATORY_BASE",(0,0,.18),.58,.30,M["blue"],"Z",96)
    for a in range(0,360,120):
        x=.35*math.cos(math.radians(a));y=.35*math.sin(math.radians(a));cyl(f"ISOLATOR_{a}",(x,y,.35),.07,.22,M["black"],"Z",48)
    bpy.ops.mesh.primitive_torus_add(major_radius=.46,minor_radius=.10,major_segments=96,minor_segments=24,location=(0,0,.70));finish(bpy.context.object,"BOWL_RIM",M["zinc"],0,True)
    cyl("BOWL_FLOOR",(0,0,.58),.50,.12,M["zinc"],"Z",96)
    # Rising spiral track and representative fasteners.
    for i in range(26):
        a=math.radians(i*23);r=.30+.006*i;z=.61+.016*i
        box(f"KIN_SPIRAL_TRACK_{i}",(r*math.cos(a),r*math.sin(a),z),(.18,.09,.035),M["yellow"],.006,False,rotation=(0,0,a))
    for i in range(18):
        a=math.radians(i*137);r=.25*math.sqrt(i/18);cyl(f"PART_{i}",(r*math.cos(a),r*math.sin(a),.70),.025,.08,M["brass"],"Z",20,False)
    box("EXIT_TRACK",(.78,0,1.04),(.72,.16,.10),M["yellow"],.010);text("BOWL_LABEL","VIBRATORY FEEDER",(0,-.59,.20),.065,M["white"],(math.pi/2,0,0))

def hopper_gate(M):
    # Four-sided tapered hopper with structural legs and powered slide gate.
    for y in (-.42,.42):box(f"HOPPER_SIDE_Y_{y}",(0,y,1.45),(1.15,.08,.85),M["alum"],.020,rotation=(math.radians(18 if y<0 else -18),0,0))
    for x in (-.52,.52):box(f"HOPPER_SIDE_X_{x}",(x,0,1.45),(.08,.80,.85),M["alum"],.020,rotation=(0,math.radians(-18 if x<0 else 18),0))
    box("THROAT",(0,0,.94),(.48,.48,.25),M["steel"],.020)
    box("KIN_SLIDE_GATE",(.12,0,.82),(.68,.52,.08),M["yellow"],.010);cyl("GATE_CYLINDER",(-.58,0,.82),.09,.72,M["blue"],"X",64)
    for x in (-.66,.66):
        for y in (-.50,.50):box(f"LEG_{x}_{y}",(x,y,.58),(.09,.09,1.16),M["steel"],.006)
    for x in (-.66,.66):
        for y in (-.50,.50):box(f"FOOT_{x}_{y}",(x,y,.04),(.24,.20,.08),M["zinc"],.006)
    box("TOP_FLANGE",(0,0,1.90),(1.48,1.18,.10),M["steel"],.010);text("HOPPER_LABEL","SLIDE GATE",(0,-.56,1.05),.065,M["white"],(math.pi/2,0,0))

def conveyor_diverter(M):
    L,W,Z=2.2,.82,.82
    for y in (-.40,.40):box(f"SIDE_{y}",(0,y,Z),(L,.08,.25),M["steel"],.008)
    for i,x in enumerate([-.92+i*.16 for i in range(12)]):cyl(f"ROLLER_{i}",(x,0,Z+.08),.045,W-.10,M["zinc"],"Y",40)
    pivot=(-.18,-.30,Z+.27);cyl("DIVERTER_PIVOT",pivot,.08,.12,M["steel"],"Z",64)
    arm=box("KIN_DIVERTER_ARM",(.42,-.02,Z+.28),(1.15,.10,.20),M["yellow"],.018,rotation=(0,0,math.radians(24)))
    cyl("DIVERTER_ACTUATOR",(-.18,-.55,Z+.16),.065,.48,M["blue"],"Y",48);tube("ACTUATOR_LINK",(-.18,-.38,Z+.22),(.18,-.12,Z+.28),.035,M["steel"])
    legs(M,L,W,Z-.12);box("CARTON",(.56,.20,1.15),(.42,.42,.42),M["carton"],.022,False)

def pallet_stop(M):
    box("MOUNT_FRAME",(0,0,.22),(.82,.58,.22),M["steel"],.018)
    cyl("PNEUMATIC_CYLINDER",(0,0,.40),.10,.42,M["blue"],"Z",64)
    box("KIN_STOP_BLADE",(0,0,.74),(.54,.14,.48),M["yellow"],.020)
    for x in (-.28,.28):
        tube(f"GUIDE_{x}",(x,0,.34),(x,0,.76),.035,M["steel"]);box(f"FOOT_{x}",(x,0,.06),(.22,.40,.08),M["zinc"],.008)
    for y in (-.18,.18):cyl(f"BUMPER_{y}",(0,y,.86),.06,.12,M["black"],"Y",48)
    text("STOP_LABEL","PALLET STOP",(0,-.297,.34),.055,M["white"],(math.pi/2,0,0))

BUILDERS={"gravity_roller_conveyor":gravity_roller,"pallet_chain_conveyor":chain_pallet,"steel_slat_conveyor":slat_conveyor,"modular_plastic_belt_conveyor":modular_belt,"popup_chain_transfer":popup_transfer,"vertical_reciprocating_conveyor":vertical_lift,"open_trough_screw_conveyor":screw_conveyor,"bucket_elevator":bucket_elevator,"vibratory_bowl_feeder":vibratory_bowl,"bulk_hopper_slide_gate":hopper_gate,"powered_diverter_arm":conveyor_diverter,"pneumatic_pallet_stop":pallet_stop}

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
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034);center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.20,2.3)
    for i,(loc,e,s) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for i,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.48);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    primary_review=(root/"review"/f"{slug}_01.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)

flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("MATERIAL_FLOW_ASSETS_BUILT",len(BUILT))
