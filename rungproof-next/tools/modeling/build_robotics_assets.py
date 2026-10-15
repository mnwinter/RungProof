"""Build industrial robot architectures and end-of-arm tooling assets."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"robotics";BUILT=[]
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
def sphere(n,l,rad,m,c=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,radius=rad,location=l);return finish(bpy.context.object,n,m,.002,True,c)
def torus(n,l,major,minor,m,axis="Z",c=False):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=12,location=l,rotation=rot);return finish(bpy.context.object,n,m,0,True,c)
def tube(n,a,b,rad,m,c=True):
    a,b=Vector(a),Vector(b);v=b-a;o=cyl(n,(a+b)/2,rad,v.length,m,"Z",40,c);o.rotation_euler=v.to_track_quat("Z","Y").to_euler();return o
def text(n,body,l,size,m):
    bpy.ops.object.text_add(location=l,rotation=(math.pi/2,0,0));o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.002;o.data.bevel_depth=.001;bpy.ops.object.convert(target="MESH");return finish(o,n,m,0,False,False)
def common():return {"orange":mat("Robot orange",(.94,.22,.015),.28,.28),"blue":mat("Robot blue",(.02,.22,.48),.42,.28),"white":mat("Robot white",(.76,.79,.78),.52,.24),"dark":mat("Robot dark",(.012,.018,.022),.20,.55),"steel":mat("Machined steel",(.57,.62,.64),.90,.15),"yellow":mat("Safety yellow",(.96,.56,.01),.16,.30),"red":mat("Safety red",(.72,.012,.008),.18,.30),"green":mat("Tool green",(.04,.36,.14),.36,.33),"copper":mat("Copper",(.65,.25,.055),.76,.20),"glass":mat("Optical glass",(.015,.18,.28),.08,.12),"black":mat("Cable rubber",(.005,.008,.010),.02,.74)}
def base(M,w,d):box("BASE",(0,0,.06),(w,d,.12),M["dark"],.018)
def joint(M,n,l,r=.18,axis="Y"):cyl(n,l,r,.26,M["blue"],axis,64)
def robot_label(M,body,l,size=.04):box("NAMEPLATE",(l[0],l[1]+.015,l[2]),(.62,.025,.24),M["white"],.008,False);text("LABEL",body,(l[0],l[1],l[2]),size,M["dark"])

def scara(M):
    base(M,1.25,1.10);cyl("SCARA_PEDESTAL",(0,.18,.72),.25,1.28,M["white"],"Z",64);joint(M,"KIN_JOINT_1",(0,.18,1.42),.22,"Z");box("ARM_1",(.38,.18,1.42),(.76,.24,.22),M["white"],.10);joint(M,"KIN_JOINT_2",(.76,.18,1.42),.18,"Z");box("ARM_2",(.98,-.06,1.42),(.62,.22,.20),M["white"],.09,r=(0,0,-.72));joint(M,"KIN_JOINT_3",(1.20,-.28,1.42),.15,"Z");cyl("KIN_Z_QUILL",(1.20,-.28,1.05),.07,.78,M["steel"],"Z",40);cyl("KIN_TOOL_ROTATION",(1.20,-.28,.64),.12,.12,M["dark"],"Z",40);box("TWO_JAW_GRIPPER",(1.20,-.28,.52),(.30,.18,.14),M["orange"],.025);robot_label(M,"SCARA ROBOT",(0,-.09,.65),.045)
def delta(M):
    base(M,2.65,2.35)
    cyl("OVERHEAD_DELTA_HOUSING",(0,0,2.62),.82,.28,M["white"],"Z",6);cyl("CENTER_CABLE_ENTRY",(0,0,2.84),.17,.22,M["dark"],"Z",40)
    for i,a in enumerate((0,2.094,4.188)):
        x=.92*math.cos(a);y=.92*math.sin(a);box(f"FRAME_POST_{i}",(x,y,1.38),(.14,.14,2.62),M["blue"],.025);tube(f"TOP_FRAME_{i}",(x,y,2.68),(.92*math.cos(a+2.094),.92*math.sin(a+2.094),2.68),.07,M["blue"]);joint(M,f"KIN_SHOULDER_{i}",(x,y,2.44),.16,"Y");box(f"DELTA_SERVO_{i}",(x,y,2.66),(.30,.30,.30),M["orange"],.045)
        ex=.45*math.cos(a);ey=.45*math.sin(a);tube(f"UPPER_ARM_{i}",(x,y,2.28),(ex,ey,1.70),.045,M["orange"]);tube(f"PARALLEL_LINK_A_{i}",(ex-.05*math.sin(a),ey+.05*math.cos(a),1.70),(.23*math.cos(a)-.05*math.sin(a),.23*math.sin(a)+.05*math.cos(a),.83),.020,M["steel"],False);tube(f"PARALLEL_LINK_B_{i}",(ex+.05*math.sin(a),ey-.05*math.cos(a),1.70),(.23*math.cos(a)+.05*math.sin(a),.23*math.sin(a)-.05*math.cos(a),.83),.020,M["steel"],False)
    cyl("KIN_MOVING_PLATFORM",(0,0,.80),.27,.12,M["orange"],"Z",48);cyl("VACUUM_TOOL",(0,0,.62),.08,.28,M["dark"],"Z",32);torus("SUCTION_CUP",(0,0,.46),.09,.025,M["black"],"Z",False);box("PICK_CONVEYOR",(0,0,.23),(1.80,.54,.16),M["dark"],.025)
    for x in (-.70,-.35,0,.35,.70):cyl(f"CONVEYOR_ROLLER_{x}",(x,0,.34),.055,.50,M["steel"],"Y",32,False)
    box("PICK_PRODUCT",(0,0,.47),(.24,.20,.12),M["yellow"],.018,False);box("DELTA_SIGN",(0,-.82,2.56),(.82,.025,.26),M["white"],.008,False);text("DELTA_LABEL","DELTA PICK ROBOT",(0,-.84,2.56),.050,M["dark"])
def cobot(M):
    base(M,1.15,1.15);cyl("COBOT_BASE",(0,0,.25),.27,.38,M["white"],"Z",64);joint(M,"KIN_J1",(0,0,.50),.19,"Z");tube("LOWER_ARM",(0,0,.58),(.15,0,1.18),.15,M["white"]);joint(M,"KIN_J2",(.15,0,1.18),.19,"Y");tube("UPPER_ARM",(.15,0,1.18),(.52,-.04,1.72),.14,M["white"]);joint(M,"KIN_J3",(.52,-.04,1.72),.18,"Y");tube("FOREARM",(.52,-.04,1.72),(.83,-.10,1.38),.12,M["white"]);joint(M,"KIN_J4",(.83,-.10,1.38),.14,"X");tube("WRIST",(.83,-.10,1.38),(1.00,-.16,1.22),.10,M["white"]);joint(M,"KIN_J5",(1.00,-.16,1.22),.12,"Y");cyl("KIN_J6",(1.08,-.22,1.12),.10,.18,M["blue"],"Z",40);box("COBOT_GRIPPER",(1.08,-.22,.98),(.30,.18,.18),M["dark"],.025)
def cartesian(M):
    base(M,3.20,2.15)
    for x in (-1.38,1.38):
        for y in (-.82,.82):box(f"GANTRY_POST_{x}_{y}",(x,y,1.25),(.16,.16,2.38),M["blue"],.025)
    for y in (-.82,.82):box(f"X_RAIL_{y}",(0,y,2.43),(2.92,.18,.18),M["steel"],.018)
    box("KIN_X_BRIDGE",(0,0,2.40),(.22,1.72,.28),M["orange"],.025);box("KIN_Y_CARRIAGE",(0,-.10,2.34),(.52,.42,.30),M["dark"],.025);box("KIN_Z_AXIS",(0,-.10,1.62),(.22,.22,1.55),M["white"],.025);box("GANTRY_GRIPPER",(0,-.10,.77),(.46,.26,.20),M["orange"],.025);robot_label(M,"XYZ GANTRY",(0,-.94,1.25),.05)
def palletizer(M):
    base(M,2.65,2.10);cyl("PALLETIZER_BASE",(-.42,.34,.40),.42,.68,M["orange"],"Z",64);joint(M,"KIN_AXIS_1",(-.42,.34,.76),.30,"Z");joint(M,"KIN_AXIS_2",(-.42,.34,1.02),.25,"Y");tube("MAIN_ARM",(-.42,.34,1.02),(-.12,.34,2.02),.18,M["orange"]);joint(M,"KIN_AXIS_3",(-.12,.34,2.02),.22,"Y");tube("FOREARM",(-.12,.34,2.02),(.62,.20,1.52),.16,M["orange"]);tube("PARALLEL_LINK",(-.44,.52,1.08),(.42,.40,1.64),.055,M["steel"],False);joint(M,"KIN_AXIS_4",(.62,.20,1.52),.15,"Z");box("PALLET_GRIPPER_HEAD",(.62,.20,1.30),(.58,.52,.20),M["dark"],.025);box("FORK_LEFT",(.78,.02,1.08),(.82,.10,.10),M["steel"],.012);box("FORK_RIGHT",(.78,.38,1.08),(.82,.10,.10),M["steel"],.012)
    # Integral pallet-and-carton pickup demonstrates the level-tooling geometry.
    for y in (-.18,.18):
        for x in (.58,.92):box(f"CARTON_{x}_{y}",(x,y,.88),(.30,.30,.28),M["white"],.018,False)
    for y in (-.30,0,.30):box(f"PALLET_DECK_{y}",(.78,y,.58),(1.05,.18,.10),M["steel"],.010,False)
    for x in (.38,.78,1.18):box(f"PALLET_BLOCK_{x}",(x,0,.45),(.16,.70,.18),M["dark"],.008,False)
    box("PALLETIZER_SIGN",(-.42,-.10,.42),(.62,.025,.26),M["white"],.008,False);text("PALLETIZER_LABEL","4-AXIS PALLETIZER",(-.42,-.12,.42),.042,M["dark"])
def robot_track(M):
    base(M,3.55,1.15)
    for y in (-.28,.28):box(f"RAIL_{y}",(0,y,.22),(3.30,.10,.16),M["steel"],.010)
    for x in (-1.45,-.48,.48,1.45):box(f"FLOOR_ANCHOR_{x}",(x,0,.10),(.18,.86,.10),M["dark"],.008)
    box("GEAR_RACK",(0,-.12,.34),(3.15,.08,.08),M["dark"],.006);box("KIN_TRACK_CARRIAGE",(0,0,.48),(.82,.78,.38),M["orange"],.035);cyl("ROBOT_MOUNTING_FLANGE",(0,0,.73),.30,.14,M["steel"],"Z",48);box("CABLE_ENERGY_CHAIN",(0,.48,.38),(2.70,.14,.20),M["black"],.025);box("SERVO_DRIVE_END",(-1.72,0,.42),(.30,.62,.58),M["blue"],.035)
    # A compact articulated-arm silhouette on the carriage makes its seventh-axis purpose explicit.
    joint(M,"TRACK_ROBOT_J1",(0,0,.92),.18,"Z");tube("TRACK_ROBOT_LOWER",(0,0,1.00),(.12,0,1.48),.12,M["white"]);joint(M,"TRACK_ROBOT_J2",(.12,0,1.48),.15,"Y");tube("TRACK_ROBOT_UPPER",(.12,0,1.48),(.48,-.03,1.70),.10,M["white"]);joint(M,"TRACK_ROBOT_WRIST",(.48,-.03,1.70),.10,"Y");box("TRACK_ROBOT_TOOL",(.58,-.04,1.60),(.22,.16,.20),M["dark"],.018);robot_label(M,"ROBOT 7TH AXIS",(-1.72,-.33,.42),.034)
def tool_changer(M):
    base(M,1.45,1.15);box("ROBOT_WRIST_STUB",(-.36,0,.70),(.42,.42,.82),M["orange"],.045);cyl("KIN_MASTER_COUPLER",(-.08,0,.70),.29,.20,M["steel"],"X",64);cyl("TOOL_COUPLER",(.22,0,.70),.29,.20,M["dark"],"X",64)
    for i,a in enumerate(range(0,360,45)):
        y=.21*math.cos(math.radians(a));z=.70+.21*math.sin(math.radians(a));cyl(f"UTILITY_PORT_{i}",(.34,y,z),.030,.08,(M["blue"] if i%2 else M["red"]),"X",24,False)
    cyl("LOCKING_PISTON",(.05,-.22,.70),.055,.24,M["yellow"],"Y",28);box("TOOL_SIDE_PLATE",(.53,0,.70),(.34,.56,.56),M["blue"],.035);robot_label(M,"AUTO TOOL\nCHANGER",(.52,-.30,.70),.035)
def mig_torch(M):
    base(M,1.55,1.10);box("ROBOT_MOUNT",(-.50,0,.74),(.32,.40,.48),M["orange"],.04);tube("TORCH_BODY",(-.30,0,.74),(.22,0,.74),.10,M["dark"]);tube("CURVED_NECK",(.22,0,.74),(.55,-.05,.48),.065,M["copper"]);cyl("GAS_NOZZLE",(.60,-.06,.43),.10,.28,M["copper"],"Z",48);cyl("CONTACT_TIP",(.60,-.06,.25),.026,.20,M["steel"],"Z",24,False);tube("WELD_WIRE",(.60,-.06,.23),(.60,-.06,.05),.008,M["steel"],False);tube("TORCH_CABLE",(-.55,.10,.84),(-.88,.24,.38),.075,M["black"],False);box("ANTI_COLLISION_MOUNT",(-.28,0,.74),(.18,.50,.50),M["red"],.035);robot_label(M,"MIG WELD\nTORCH",(-.45,-.27,.38),.038)
def spot_gun(M):
    base(M,1.55,1.30);box("ROBOT_MOUNT",(.48,.16,1.12),(.44,.50,.58),M["orange"],.045);box("C_GUN_BACK",(-.08,.10,1.10),(.34,.34,1.28),M["blue"],.08);box("UPPER_ARM",(-.42,.10,1.67),(.74,.30,.20),M["copper"],.05);box("LOWER_ARM",(-.42,.10,.53),(.74,.30,.20),M["copper"],.05);cyl("KIN_MOVING_ELECTRODE",(-.72,.10,1.30),.055,.68,M["copper"],"Z",36);cyl("FIXED_ELECTRODE",(-.72,.10,.83),.055,.50,M["copper"],"Z",36);cyl("SERVO_ACTUATOR",(-.10,.10,1.72),.18,.46,M["dark"],"Z",48);tube("WATER_HOSE_A",(-.20,-.08,1.62),(.40,-.18,1.25),.025,M["blue"],False);tube("WATER_HOSE_B",(-.20,.28,.60),(.40,.34,.94),.025,M["red"],False);robot_label(M,"SERVO SPOT\nWELD GUN",(.48,-.12,.76),.032)
def paint_gun(M):
    base(M,1.45,1.10);box("ROBOT_MOUNT",(-.50,0,.72),(.34,.42,.50),M["orange"],.04);box("SPRAY_GUN_BODY",(-.10,0,.72),(.58,.34,.32),M["white"],.055);cyl("AIR_CAP",(.24,0,.72),.18,.16,M["steel"],"X",48);cyl("FLUID_NOZZLE",(.36,0,.72),.055,.18,M["copper"],"X",32,False)
    for i,a in enumerate(range(0,360,60)):
        y=.13*math.cos(math.radians(a));z=.72+.13*math.sin(math.radians(a));cyl(f"AIR_JET_{i}",(.34,y,z),.016,.12,M["dark"],"X",16,False)
    cyl("PAINT_CUP",(-.10,0,1.05),.18,.42,M["white"],"Z",48);tube("ATOMIZING_AIR",(-.38,.16,.68),(-.72,.28,.36),.030,M["blue"],False);tube("FLUID_HOSE",(-.38,-.16,.68),(-.72,-.28,.36),.030,M["green"],False);robot_label(M,"ROBOT PAINT\nSPRAY GUN",(-.30,-.30,.38),.032)
def spindle(M):
    base(M,1.25,1.15);box("ROBOT_MOUNT",(0,.25,1.02),(.62,.44,.34),M["orange"],.04);cyl("KIN_SPINDLE_MOTOR",(0,0,.82),.27,.72,M["blue"],"Z",64)
    for i in range(10):
        a=2*math.pi*i/10;box(f"COOLING_FIN_{i}",(.29*math.cos(a),.29*math.sin(a),.86),(.055,.055,.50),M["blue"],.006,False,r=(0,0,a))
    cyl("SPINDLE_NOSE",(0,0,.40),.18,.20,M["steel"],"Z",48);cyl("ER_COLLET",(0,0,.26),.11,.18,M["dark"],"Z",32);cyl("CUTTING_TOOL",(0,0,.02),.035,.42,M["steel"],"Z",24,False);tube("COOLANT_LINE",(.22,.10,.52),(.14,.04,.18),.025,M["blue"],False);robot_label(M,"ROBOTIC\nSPINDLE",(0,-.33,.76),.038)
def magnetic_gripper(M):
    base(M,1.55,1.20);box("ROBOT_MOUNT",(0,.18,1.04),(.52,.52,.32),M["orange"],.04);box("MAGNET_BACKPLATE",(0,0,.82),(1.12,.68,.22),M["blue"],.035)
    for x in (-.38,0,.38):
        for y in (-.20,.20):cyl(f"ELECTROMAGNET_{x}_{y}",(x,y,.61),.15,.32,M["dark"],"Z",48);torus(f"MAGNET_FACE_{x}_{y}",(x,y,.44),.11,.025,M["steel"],"Z",False)
    box("STEEL_PLATE_LOAD",(0,0,.28),(1.30,.82,.08),M["steel"],.010,False);tube("POWER_CABLE",(.52,.22,.92),(.78,.38,.52),.045,M["black"],False);robot_label(M,"MAGNETIC\nGRIPPER",(0,-.38,.83),.038)
def fork_gripper(M):
    base(M,1.80,1.50);box("ROBOT_MOUNT",(0,.35,1.25),(.55,.55,.36),M["orange"],.04);box("FORK_BACKPLATE",(0,.14,.92),(1.12,.30,.82),M["blue"],.045);box("KIN_FORK_LEFT",(-.36,-.28,.53),(.18,1.10,.16),M["steel"],.018);box("KIN_FORK_RIGHT",(.36,-.28,.53),(.18,1.10,.16),M["steel"],.018);box("FORK_HEEL_LEFT",(-.36,.12,.72),(.18,.18,.46),M["steel"],.018);box("FORK_HEEL_RIGHT",(.36,.12,.72),(.18,.18,.46),M["steel"],.018);cyl("FORK_SPACING_ACTUATOR",(0,.22,.92),.08,.68,M["dark"],"X",40);robot_label(M,"PALLET FORK\nEND EFFECTOR",(0,-.03,1.10),.034)
def vision_camera(M):
    base(M,1.65,1.25);box("FIXED_CAMERA_STAND",(0,.34,.78),(.22,.22,1.42),M["steel"],.035);box("ADJUSTABLE_CAMERA_BRACKET",(0,.10,1.45),(.68,.18,.18),M["steel"],.025);box("STEREO_CAMERA_BODY",(0,-.10,1.38),(1.18,.42,.64),M["blue"],.055);box("FRONT_FACE",(0,-.335,1.38),(1.02,.06,.52),M["dark"],.025,False)
    for x in (-.32,.32):cyl(f"STEREO_LENS_{x}",(x,-.39,1.43),.16,.16,M["steel"],"Y",56,False);cyl(f"LENS_GLASS_{x}",(x,-.485,1.43),.115,.035,M["glass"],"Y",56,False);torus(f"LED_RING_{x}",(x,-.51,1.43),.135,.018,M["white"],"Y",False)
    box("STRUCTURED_LIGHT_PROJECTOR",(0,-.43,1.18),(.28,.13,.16),M["white"],.020,False);cyl("PROJECTOR_GLASS",(0,-.51,1.18),.052,.035,M["red"],"Y",32,False);box("M12_POWER",(-.28,.16,1.22),(.14,.14,.18),M["dark"],.015);box("M12_ETHERNET",(.28,.16,1.22),(.14,.14,.18),M["dark"],.015);box("VISION_NAMEPLATE",(0,-.39,1.68),(.82,.025,.16),M["white"],.008,False);text("VISION_LABEL","STEREO 3D VISION CAMERA",(0,-.41,1.68),.035,M["dark"])
    # Calibration target is a recognizable companion for machine-vision setup.
    box("CALIBRATION_TARGET",(0,-.52,.48),(.82,.08,.58),M["white"],.012,False)
    for ix in range(6):
        for iz in range(4):
            if (ix+iz)%2==0:box(f"CHECKER_{ix}_{iz}",(-.34+ix*.135,-.57,.28+iz*.135),(.125,.018,.125),M["dark"],.001,False)

BUILDERS={"scara_robot":scara,"delta_pick_robot":delta,"six_axis_cobot":cobot,"cartesian_gantry_robot":cartesian,"four_axis_palletizer":palletizer,"robot_linear_track":robot_track,"automatic_tool_changer":tool_changer,"robotic_mig_torch":mig_torch,"servo_spot_weld_gun":spot_gun,"robotic_paint_spray_gun":paint_gun,"robotic_high_speed_spindle":spindle,"electromagnetic_sheet_gripper":magnetic_gripper,"robotic_pallet_fork":fork_gripper,"industrial_3d_vision_camera":vision_camera}
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
    floor=box("REVIEW_floor",(0,0,mins.z-.035),(max(4,vmax.x-vmin.x+1.5),max(4,vmax.y-vmin.y+1.5),.05),M["white"],.002,False);floor["rungproof_asset"]=False;world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034);center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.18,2.4)
    for i,(loc,e,s) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for i,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.46);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    (root/"thumbnail.png").write_bytes((root/"review"/f"{slug}_01.png").read_bytes());BUILT.append(slug)
flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("ROBOTICS_ASSETS_BUILT",len(BUILT))
