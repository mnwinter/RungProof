"""Build recognizable production-machine and machine-tool catalog assets."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"production-machines";BUILT=[]
def clean():
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes,bpy.data.curves,bpy.data.cameras,bpy.data.lights,bpy.data.materials):
        for block in list(blocks):
            if block.users==0:blocks.remove(block)
def mat(name,c,metal=0,rough=.4):
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;b=m.node_tree.nodes.get("Principled BSDF");b.inputs["Base Color"].default_value=(*c,1);b.inputs["Metallic"].default_value=metal;b.inputs["Roughness"].default_value=rough;return m
def finish(o,name,m=None,bevel=.004,smooth=False,collision=True):
    o.name=name
    if m:o.data.materials.append(m)
    if bevel:q=o.modifiers.new("Manufactured edge radius","BEVEL");q.width=bevel;q.segments=3;q.limit_method="ANGLE"
    if smooth and hasattr(o.data,"polygons"):
        for p in o.data.polygons:p.use_smooth=True
    o["rungproof_asset"]=True;o["rungproof_collision"]=collision;return o
def box(n,l,d,m,b=.004,c=True,r=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=l,rotation=r);o=bpy.context.object;o.dimensions=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m,b,False,c)
def cyl(n,l,rad,depth,m,axis="Z",verts=64,c=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=rad,depth=depth,location=l,rotation=rot);return finish(bpy.context.object,n,m,.002,True,c)
def torus(n,l,major,minor,m,axis="Z",c=False):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=64,minor_segments=12,location=l,rotation=rot);return finish(bpy.context.object,n,m,0,True,c)
def tube(n,a,b,rad,m,c=True):
    a,b=Vector(a),Vector(b);v=b-a;o=cyl(n,(a+b)/2,rad,v.length,m,"Z",40,c);o.rotation_euler=v.to_track_quat("Z","Y").to_euler();return o
def text(n,body,l,size,m):
    bpy.ops.object.text_add(location=l,rotation=(math.pi/2,0,0));o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.002;o.data.bevel_depth=.001;bpy.ops.object.convert(target="MESH");return finish(o,n,m,0,False,False)
def common():return {"blue":mat("Machine blue",(.025,.17,.34),.55,.30),"green":mat("Machine green",(.08,.28,.15),.45,.35),"gray":mat("Machine gray",(.40,.45,.46),.75,.25),"dark":mat("Machine dark",(.018,.025,.030),.25,.52),"steel":mat("Machined steel",(.58,.62,.63),.88,.16),"yellow":mat("Safety yellow",(.96,.55,.01),.20,.30),"orange":mat("Safety orange",(.95,.22,.01),.18,.30),"red":mat("Safety red",(.75,.015,.01),.18,.30),"white":mat("Label white",(.91,.92,.89),.02,.36),"glass":mat("Safety glass",(.06,.32,.39),.10,.18),"black":mat("Rubber",(.008,.012,.015),.02,.70),"copper":mat("Copper",(.62,.25,.06),.72,.22)}
def base(M,w,d,z=.10):box("MACHINE_BASE",(0,0,z/2),(w,d,z),M["dark"],.015)
def motor(M,l,axis="X",s=1,node="MOTOR"):
    cyl(node,l,.18*s,.45*s,M["blue"],axis,64)
    for i in range(8):
        a=2*math.pi*i/8;box(f"{node}_FIN_{i}",(l[0],l[1]+.19*s*math.cos(a),l[2]+.19*s*math.sin(a)),(.34*s,.027*s,.055*s),M["blue"],.002,False)
def wheel(n,l,r,w,M,axis="Y",c=False):
    cyl(n,l,r,w,M["steel"],axis,64,c);cyl(n+"_HUB",l,r*.18,w*1.12,M["dark"],axis,32,c)
def handwheel(M,n,l,r=.13,axis="Y"):
    torus(n,l,r,.018,M["dark"],axis,False);cyl(n+"_HUB",l,.035,.06,M["steel"],axis,32,False)
    for a in (0,math.pi/2):
        if axis=="Y":tube(n+f"_SPOKE_{a}",(l[0]-r*math.cos(a),l[1],l[2]-r*math.sin(a)),(l[0]+r*math.cos(a),l[1],l[2]+r*math.sin(a)),.008,M["dark"],False)

def hydraulic_c_press(M):
    base(M,1.55,1.10);box("C_FRAME_BACK",(.45,.18,1.25),(.48,.72,2.35),M["blue"],.08);box("LOWER_BOLSTER",(-.12,.05,.48),(1.18,.76,.30),M["blue"],.045);box("UPPER_CROWN",(-.10,.15,2.20),(1.18,.78,.35),M["blue"],.05)
    cyl("HYDRAULIC_CYLINDER",(-.30,.10,1.92),.22,.62,M["gray"],"Z",64);cyl("KIN_PRESS_RAM",(-.30,.10,1.42),.13,.62,M["steel"],"Z",48);box("UPPER_DIE",(-.30,.10,1.08),(.48,.46,.16),M["dark"],.015);box("LOWER_DIE",(-.30,.10,.72),(.60,.52,.17),M["dark"],.015);box("TWO_HAND_CONTROL",(-.60,-.58,.92),(.65,.20,.20),M["yellow"],.03)
    for x in (-.82,-.38):cyl(f"PALM_BUTTON_{x}",(x,-.70,.94),.055,.04,M["red"],"Y",24,False)
    box("PRESS_NAMEPLATE",(.45,-.38,1.47),(.36,.025,.62),M["white"],.008,False);text("PRESS_LABEL","HYDRAULIC\nC-FRAME PRESS",(.45,-.40,1.47),.045,M["dark"])
def gap_frame_press(M):
    base(M,1.80,1.22);box("CAST_GAP_FRAME",(.42,.18,1.25),(.60,.86,2.38),M["green"],.10);box("PRESS_BED",(-.20,.05,.53),(1.25,.82,.35),M["green"],.05);box("CROWN",(-.13,.18,2.18),(1.22,.88,.42),M["green"],.06)
    wheel("KIN_FLYWHEEL",(.50,-.52,2.02),.43,.16,M,"Y");cyl("CRANKSHAFT",(.08,-.46,2.03),.11,.78,M["steel"],"Y",40);tube("CONNECTING_ROD",(.08,-.42,1.92),(-.18,-.42,1.43),.055,M["steel"],False);box("KIN_SLIDE",(-.18,.05,1.31),(.58,.55,.55),M["gray"],.025);box("PUNCH",(-.18,.05,.97),(.34,.32,.14),M["dark"],.012);box("DIE",(-.18,.05,.76),(.52,.44,.14),M["dark"],.012);box("FLYWHEEL_GUARD",(.50,-.62,2.02),(.94,.05,.94),M["yellow"],.05,False)
def press_brake(M):
    base(M,3.20,1.35);box("LEFT_SIDE_FRAME",(-1.38,.10,1.10),(.34,1.02,2.05),M["blue"],.06);box("RIGHT_SIDE_FRAME",(1.38,.10,1.10),(.34,1.02,2.05),M["blue"],.06);box("UPPER_BEAM",(0,.10,2.02),(2.65,.92,.42),M["blue"],.05);box("KIN_PRESS_BEAM",(0,-.02,1.38),(2.58,.48,.32),M["gray"],.025);box("UPPER_PUNCH",(0,-.20,1.14),(2.48,.10,.23),M["steel"],.006);box("LOWER_DIE",(0,-.20,.83),(2.60,.22,.22),M["dark"],.008)
    for x in (-1.00,1.00):cyl(f"HYDRAULIC_CYLINDER_{x}",(x,.10,1.86),.16,.68,M["gray"],"Z",48)
    box("CNC_CONTROL",(1.62,-.62,1.45),(.42,.25,.72),M["dark"],.025);box("CNC_SCREEN",(1.62,-.76,1.58),(.28,.02,.22),M["glass"],.005,False);box("FOOT_PEDAL",(-.75,-.80,.16),(.30,.38,.13),M["yellow"],.025)
def horizontal_bandsaw(M):
    base(M,2.55,1.05);box("COOLANT_BASE",(0,.06,.47),(2.25,.84,.82),M["blue"],.05);box("VISE_FIXED",(-.20,-.23,1.02),(.18,.62,.43),M["gray"],.018);box("KIN_VISE_MOVING",(.25,-.23,1.02),(.18,.62,.43),M["gray"],.018);box("WORKPIECE",(.02,-.23,1.05),(.75,.20,.20),M["steel"],.01)
    # Horizontal pivoting bow with two wheel housings and a visible lower blade run.
    box("KIN_SAW_BOW",(0,.08,1.62),(2.12,.28,.56),M["green"],.10,r=(0,.10,0));wheel("BAND_WHEEL_LEFT",(-.78,-.08,1.63),.36,.10,M,"Y",False);wheel("BAND_WHEEL_RIGHT",(.78,.08,1.78),.36,.10,M,"Y",False);box("VISIBLE_BAND_BLADE",(0,-.16,1.33),(1.55,.018,.025),M["steel"],.001,False,r=(0,.10,0));cyl("BOW_PIVOT",(-1.03,.12,1.32),.13,.50,M["dark"],"Y",40);box("CONTROL_PANEL",(1.12,-.38,1.22),(.38,.22,.65),M["dark"],.025)
def cold_saw(M):
    base(M,1.95,1.25);box("SAW_PEDESTAL",(.18,.18,.55),(1.45,.82,.92),M["blue"],.055);box("CHIP_TRAY",(-.15,-.38,1.00),(1.52,.52,.10),M["gray"],.012)
    # Stock crosses the blade plane through a two-jaw vise, making this a cutoff saw.
    box("ROUND_STOCK",(-.10,-.48,1.13),(1.65,.16,.16),M["steel"],.030);box("FIXED_VISE_JAW",(.30,-.36,1.18),(.14,.38,.40),M["dark"],.012);box("MOVING_VISE_JAW",(-.38,-.36,1.18),(.14,.38,.40),M["dark"],.012);tube("VISE_SCREW",(-.72,-.36,1.18),(-.38,-.36,1.18),.045,M["steel"]);handwheel(M,"VISE_HANDWHEEL",(-.80,-.36,1.18),.13,"X")
    cyl("KIN_SAW_BLADE",(-.04,-.25,1.58),.46,.040,M["steel"],"Y",96,False);torus("BLADE_TEETH",(-.04,-.25,1.58),.465,.022,M["dark"],"Y",False);box("UPPER_BLADE_GUARD",(-.04,-.20,1.81),(.92,.12,.49),M["yellow"],.07,False)
    tube("KIN_SAW_HEAD_ARM",(.52,.08,1.78),(.20,-.12,1.66),.11,M["blue"]);cyl("HEAD_PIVOT",(.58,.08,1.82),.16,.48,M["dark"],"Y",40);motor(M,(.48,.20,1.55),"Y",.68,"SAW_MOTOR");box("OPERATOR_HANDLE",(-.53,-.35,1.94),(.52,.07,.07),M["black"],.025,r=(0,-.28,0));box("COLD_SAW_SIGN",(.48,-.25,.60),(.58,.025,.22),M["white"],.008,False);text("COLD_SAW_LABEL","COLD SAW",(.48,-.27,.60),.055,M["dark"])
def pedestal_grinder(M):
    base(M,1.55,.90);box("PEDESTAL",(0,.08,.75),(.46,.42,1.35),M["blue"],.06);motor(M,(0,.04,1.55),"X",1.15,"KIN_GRINDER_MOTOR")
    for x,s in ((-.48,-1),(.48,1)):
        wheel(f"GRINDING_WHEEL_{x}",(x,.04,1.55),.30,.10,M,"X",False);box(f"WHEEL_GUARD_{x}",(x,.04,1.55),(.16,.68,.68),M["yellow"],.08,False);box(f"WORK_REST_{x}",(x,-.34,1.39),(.34,.22,.08),M["gray"],.01);box(f"EYE_SHIELD_{x}",(x,-.35,1.72),(.36,.025,.28),M["glass"],.012,False)
def manual_lathe(M):
    base(M,3.05,1.15);box("LATHE_BED",(0,.08,.72),(2.85,.68,.35),M["blue"],.045)
    for y in (-.18,.18):box(f"BED_WAY_{y}",(0,y,.94),(2.62,.11,.10),M["steel"],.008)
    box("HEADSTOCK",(-1.03,.08,1.34),(.72,.76,.85),M["green"],.055);cyl("KIN_SPINDLE_CHUCK",(-.61,-.02,1.42),.30,.28,M["dark"],"X",64);cyl("WORKPIECE",(.10,-.02,1.42),.10,1.22,M["steel"],"X",48)
    for a in (0,2.094,4.188):box(f"CHUCK_JAW_{a}",(-.45,.18*math.cos(a)-.02,1.42+.18*math.sin(a)),(.16,.09,.09),M["steel"],.006,False,r=(a,0,0))
    box("KIN_CARRIAGE",(.10,-.02,1.07),(.78,.72,.28),M["gray"],.025);box("CROSS_SLIDE",(.10,-.08,1.25),(.50,.48,.16),M["gray"],.012);cyl("TOOL_POST",(.10,-.08,1.40),.15,.22,M["dark"],"Z",40);box("CUTTING_TOOL",(-.12,-.08,1.48),(.52,.08,.08),M["steel"],.005);box("TAILSTOCK",(1.00,.07,1.28),(.52,.63,.70),M["green"],.045);cyl("TAILSTOCK_QUILL",(.66,-.02,1.43),.10,.48,M["steel"],"X",40);handwheel(M,"TAILSTOCK_HANDWHEEL",(1.30,-.31,1.18),.16,"Y")
def vertical_mill(M):
    base(M,1.55,1.40);box("MILL_COLUMN",(0,.36,1.22),(.72,.62,2.22),M["green"],.07);box("KNEE",(0,.02,.86),(.86,.72,.50),M["green"],.045);box("KIN_X_TABLE",(0,-.22,1.18),(1.52,.48,.18),M["gray"],.012)
    for y in (-.36,-.22,-.08):box(f"TABLE_T_SLOT_{y}",(0,y,1.29),(1.42,.025,.025),M["dark"],.002,False)
    box("RAM",(0,.05,2.22),(.70,.98,.30),M["green"],.04);box("MILLING_HEAD",(0,-.36,1.98),(.55,.48,.72),M["blue"],.055);cyl("KIN_SPINDLE",(0,-.38,1.48),.08,.62,M["steel"],"Z",48);cyl("COLLET_TOOL",(0,-.38,1.13),.035,.28,M["dark"],"Z",32);motor(M,(0,-.04,2.52),"Z",.85,"SPINDLE_MOTOR");handwheel(M,"X_HANDWHEEL",(.87,-.22,1.18),.14,"X")
def injection_molder(M):
    base(M,4.10,1.38);box("CLAMP_BASE",(-1.05,.08,.64),(1.78,1.15,1.05),M["blue"],.06);box("INJECTION_BASE",(1.10,.08,.64),(1.92,1.15,1.05),M["blue"],.06)
    for x in (-1.70,-.35):box(f"CLAMP_PLATEN_{x}",(x,.05,1.48),(.24,.96,1.32),M["gray"],.025)
    for y,z in ((-.35,1.12),(.35,1.12),(-.35,1.84),(.35,1.84)):tube(f"TIE_BAR_{y}_{z}",(-1.70,y,z),(-.35,y,z),.045,M["steel"])
    box("KIN_MOVING_PLATEN",(-.78,.05,1.48),(.20,.92,1.18),M["steel"],.02);box("MOLD_A",(-.66,.05,1.48),(.20,.72,.82),M["dark"],.012);box("MOLD_B",(-.47,.05,1.48),(.20,.72,.82),M["dark"],.012)
    cyl("KIN_INJECTION_SCREW",(.82,.04,1.52),.16,1.62,M["steel"],"X",64);cyl("BARREL_HEATER_BANDS",(.82,.04,1.52),.22,1.45,M["orange"],"X",64,False)
    for x in (.20,.52,.84,1.16,1.48):torus(f"HEATER_BAND_{x}",(x,.04,1.52),.225,.024,M["dark"],"X",False)
    cyl("FEED_HOPPER",(1.34,.04,2.18),.38,.66,M["steel"],"Z",64);cyl("HOPPER_THROAT",(1.34,.04,1.78),.13,.30,M["steel"],"Z",40);motor(M,(1.63,.04,1.52),"X",.72,"SCREW_DRIVE");box("OPERATOR_CONTROL",(1.67,-.72,1.46),(.52,.24,.88),M["dark"],.03);box("HMI",(1.67,-.86,1.63),(.36,.02,.28),M["glass"],.005,False)
def welding_positioner(M):
    base(M,1.75,1.55);box("POSITIONER_COLUMN",(.18,.34,.67),(.64,.62,1.22),M["blue"],.06);cyl("KIN_TILT_TRUNNION",(.02,.06,1.28),.20,.88,M["steel"],"X",48)
    # Table is shown tilted 35 degrees, with a fixture and weldment on its face.
    table=cyl("KIN_ROTARY_TABLE",(-.12,-.25,1.40),.56,.13,M["gray"],"Y",72);table.rotation_euler.x=math.radians(35);face=cyl("TABLE_FACE",(-.12,-.30,1.43),.50,.025,M["dark"],"Y",64,False);face.rotation_euler.x=math.radians(35)
    box("WELD_FIXTURE",(-.12,-.48,1.47),(.62,.18,.18),M["steel"],.012,r=(math.radians(35),0,0));tube("WELDMENT_VERTICAL",(-.28,-.54,1.48),(-.28,-.78,1.92),.075,M["steel"],False);tube("WELDMENT_HORIZONTAL",(-.28,-.78,1.92),(.24,-.78,1.92),.075,M["steel"],False)
    # Torch makes the process use explicit without relying on a nameplate.
    tube("WELDING_TORCH",(.58,-.72,1.88),(.22,-.70,1.62),.055,M["dark"],False);cyl("TORCH_NOZZLE",(.18,-.69,1.59),.05,.17,M["copper"],"Z",32,False);tube("TORCH_CABLE",(.58,-.72,1.88),(.76,-.32,.86),.025,M["black"],False)
    box("FOOT_CONTROL",(-.62,-.83,.15),(.34,.42,.13),M["yellow"],.025);box("CONTROL_PENDANT",(.68,-.48,.88),(.36,.22,.58),M["dark"],.025);box("POSITIONER_SIGN",(.18,-.01,.55),(.46,.025,.30),M["white"],.008,False);text("POSITIONER_LABEL","WELDING\nPOSITIONER",(.18,-.03,.55),.042,M["dark"])
def spot_welder(M):
    base(M,1.85,1.35);box("WELDER_TRANSFORMER_CABINET",(.43,.22,1.08),(.76,.78,2.04),M["blue"],.07);box("UPPER_C_ARM",(-.22,.08,1.88),(1.22,.28,.25),M["copper"],.055);box("LOWER_C_ARM",(-.22,.08,.92),(1.22,.28,.25),M["copper"],.055);box("BACK_SPINE",(-.75,.08,1.40),(.25,.28,.78),M["copper"],.045)
    cyl("PNEUMATIC_CYLINDER",(-.52,.08,1.73),.18,.44,M["gray"],"Z",48);cyl("KIN_UPPER_ELECTRODE",(-.52,.08,1.38),.060,.50,M["copper"],"Z",40);cyl("UPPER_TIP",(-.52,.08,1.17),.032,.16,M["steel"],"Z",32,False);cyl("LOWER_ELECTRODE",(-.52,.08,1.02),.060,.35,M["copper"],"Z",40);cyl("LOWER_TIP",(-.52,.08,1.19),.032,.16,M["steel"],"Z",32,False);box("OVERLAPPED_SHEET_A",(-.52,.08,1.20),(.90,.52,.025),M["steel"],.003,False);box("OVERLAPPED_SHEET_B",(-.36,.08,1.24),(.90,.52,.025),M["steel"],.003,False,r=(0,0,.10))
    tube("WATER_HOSE_SUPPLY",(-.20,-.12,1.86),(.36,-.28,1.58),.025,M["blue"],False);tube("WATER_HOSE_RETURN",(-.20,.28,.94),(.36,.38,1.18),.025,M["red"],False);box("WELD_CONTROLLER",(.48,-.52,1.30),(.50,.24,.75),M["dark"],.025);box("SPOT_WELDER_SIGN",(.48,-.66,.78),(.48,.025,.30),M["white"],.008,False);text("SPOT_WELDER_LABEL","RESISTANCE\nSPOT WELDER",(.48,-.68,.78),.038,M["dark"]);box("FOOT_SWITCH",(-.65,-.72,.15),(.32,.42,.13),M["yellow"],.025)
def parts_washer(M):
    base(M,1.70,1.30);box("WASH_CABINET",(0,.10,.90),(1.48,1.10,1.62),M["gray"],.06);box("KIN_FRONT_DOOR",(0,-.48,1.08),(1.27,.08,1.12),M["blue"],.035);box("VIEW_WINDOW",(0,-.535,1.22),(.72,.025,.52),M["glass"],.012,False);box("DOOR_HANDLE",(.46,-.59,.92),(.25,.08,.06),M["dark"],.02,False)
    cyl("KIN_WASH_BASKET",(0,.02,.72),.47,.10,M["steel"],"Z",64,False);tube("SPRAY_MANIFOLD",(-.52,.02,.82),(.52,.02,.82),.035,M["steel"],False)
    for x in (-.40,-.20,0,.20,.40):cyl(f"SPRAY_NOZZLE_{x}",(x,.02,.74),.018,.10,M["orange"],"Z",24,False)
    box("CONTROL_PANEL",(.48,-.57,1.56),(.38,.10,.30),M["dark"],.018);cyl("ESTOP",(.58,-.64,1.52),.045,.03,M["red"],"Y",24,False);text("WASHER_LABEL","AQUEOUS\nPARTS WASHER",(-.30,-.59,1.56),.043,M["white"])
def rotary_barrel_tumbler(M):
    base(M,2.55,1.45);box("LEFT_FRAME",(-.93,.08,.92),(.22,1.04,1.55),M["blue"],.04);box("RIGHT_FRAME",(.93,.08,.92),(.22,1.04,1.55),M["blue"],.04);box("FRAME_CROSSMEMBER",(0,.08,.35),(1.92,.22,.22),M["blue"],.035)
    # Eight-sided horizontal barrel is the characteristic mass-finishing vessel.
    cyl("KIN_ROTARY_BARREL",(0,.08,1.25),.60,1.58,M["green"],"X",8);cyl("BARREL_LEFT_END",(-.80,.08,1.25),.53,.08,M["dark"],"X",8);cyl("BARREL_RIGHT_END",(.80,.08,1.25),.53,.08,M["dark"],"X",8)
    box("OPEN_BARREL_MOUTH",(0,-.612,1.25),(.82,.025,.58),M["dark"],.035,False);box("OPEN_CHARGING_HATCH",(0,-.48,1.78),(.82,.10,.54),M["gray"],.025,False,r=(math.radians(-22),0,0));box("HATCH_HANDLE",(0,-.66,1.92),(.30,.08,.06),M["dark"],.018,False);text("HATCH_LABEL","LOAD / UNLOAD",(0,-.72,1.80),.038,M["dark"])
    for i in range(34):
        x=-.33+.66*((i%9)/8);z=1.06+.36*(((i//9)%4)/3);box(f"VISIBLE_BARREL_MEDIA_{i}",(x,-.65,z),(.060,.040,.075),M["white"],.008,False,r=(.2*i,.1*i,.31*i))
    for i,(x,z) in enumerate(((-.22,1.20),(.02,1.33),(.23,1.13))):box(f"VISIBLE_WORKPIECE_{i}",(x,-.68,z),(.22,.08,.065),M["steel"],.009,False,r=(.12,0,.55*i))
    cyl("BARREL_SHAFT_LEFT",(-1.10,.08,1.25),.09,.42,M["steel"],"X",40);cyl("BARREL_SHAFT_RIGHT",(1.10,.08,1.25),.09,.42,M["steel"],"X",40);motor(M,(1.47,.08,.70),"X",.72,"TUMBLER_DRIVE_MOTOR");box("GEAR_REDUCER",(1.13,.08,.95),(.44,.48,.48),M["gray"],.045);tube("CHAIN_DRIVE",(1.12,.08,1.12),(1.12,.08,1.25),.07,M["dark"],False)
    box("DISCHARGE_TRAY",(0,-.62,.55),(1.48,.62,.12),M["steel"],.025,r=(.08,0,0))
    for i in range(26):
        x=-.62+1.24*((i%9)/8);y=-.76+.30*((i//9)/2);box(f"TUMBLING_MEDIA_{i}",(x,y,.68+.018*(i%3)),(.055,.040,.075),M["white"],.008,False,r=(.2*i,.1*i,.33*i))
    for i,x in enumerate((-.42,0,.42)):box(f"FINISHED_PART_{i}",(x,-.72,.76),(.26,.12,.07),M["steel"],.009,False,r=(.12,0,.35*i))
    box("CONTROL_BOX",(-1.25,-.45,.82),(.46,.30,.72),M["dark"],.025);box("TUMBLER_SIGN",(-1.25,-.62,.82),(.42,.025,.42),M["white"],.008,False);text("TUMBLER_LABEL","ROTARY BARREL\nMASS FINISHER",(-1.25,-.64,.82),.034,M["dark"])
def laser_marker(M):
    base(M,1.65,1.35);box("ENCLOSED_LASER_CABINET",(0,.10,1.05),(1.50,1.18,1.92),M["blue"],.06);box("KIN_SAFETY_DOOR",(0,-.52,1.08),(1.28,.08,1.42),M["gray"],.035);box("LASER_WINDOW",(0,-.575,1.22),(.72,.025,.60),M["glass"],.012,False);box("DOOR_HANDLE",(.47,-.63,.93),(.24,.07,.06),M["dark"],.018,False)
    box("WORK_TABLE",(0,.02,.70),(1.02,.78,.10),M["steel"],.010);box("KIN_LASER_HEAD",(0,.10,1.48),(.30,.30,.42),M["dark"],.025);cyl("FOCUS_LENS",(0,.10,1.22),.08,.12,M["orange"],"Z",32,False);box("PART_FIXTURE",(0,.02,.82),(.55,.38,.14),M["steel"],.012);box("HMI_PENDANT",(.82,-.54,1.42),(.42,.24,.66),M["dark"],.03);box("HMI_SCREEN",(.82,-.68,1.54),(.28,.02,.24),M["glass"],.005,False);box("LASER_WARNING",(-.50,-.58,1.75),(.30,.025,.26),M["yellow"],.008,False);text("LASER_LABEL","LASER",(-.50,-.60,1.75),.060,M["dark"])

BUILDERS={"hydraulic_c_frame_press":hydraulic_c_press,"mechanical_gap_frame_press":gap_frame_press,"cnc_press_brake":press_brake,"horizontal_bandsaw":horizontal_bandsaw,"cold_saw":cold_saw,"pedestal_grinder":pedestal_grinder,"manual_engine_lathe":manual_lathe,"vertical_knee_mill":vertical_mill,"injection_molding_machine":injection_molder,"welding_positioner":welding_positioner,"resistance_spot_welder":spot_welder,"aqueous_parts_washer":parts_washer,"rotary_barrel_tumbler":rotary_barrel_tumbler,"enclosed_laser_marking_station":laser_marker}
def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat("-Z","Y").to_euler()
def save(slug,builder):
    clean();M=common();builder(M);root=BASE/slug
    for p in ("source","delivery","collision","review"):(root/p).mkdir(parents=True,exist_ok=True)
    for p in (root/"source",root/"review"):(p/".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"));objects=[o for o in bpy.context.scene.objects if o.type=="MESH" and o.get("rungproof_asset")]
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
    floor=box("REVIEW_floor",(0,0,mins.z-.035),(max(4,vmax.x-vmin.x+1.5),max(4,vmax.y-vmin.y+1.5),.05),M["gray"],.002,False);floor["rungproof_asset"]=False;world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034);center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.18,2.4)
    for i,(loc,e,s) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for i,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.46);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    # This upright machine family is most legible from the front three-quarter
    # camera, which exposes guarding, tools, and operator-side controls.
    primary_review=(root/"review"/f"{slug}_03.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)
flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("PRODUCTION_MACHINE_ASSETS_BUILT",len(BUILT))
