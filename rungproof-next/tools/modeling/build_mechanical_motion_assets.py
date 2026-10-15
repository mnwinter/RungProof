"""Build reusable industrial actuator, motor, gearbox, and linear-motion assets."""
from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
BASE = ROOT / "assets" / "mechanical_motion"
BUILT: list[str] = []


def clean():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights, bpy.data.materials):
        for block in list(blocks):
            if block.users == 0:
                blocks.remove(block)


def mat(name, color, metal=0.0, rough=.38, alpha=1.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, alpha)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    if alpha < 1:
        bsdf.inputs["Alpha"].default_value = alpha
        m.surface_render_method = "DITHERED"
    return m


def finish(obj, name, material=None, bevel=.004, smooth=False, collision=True):
    obj.name = name
    if material:
        obj.data.materials.append(material)
    if bevel:
        mod = obj.modifiers.new("Manufactured edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        mod.limit_method = "ANGLE"
    if smooth and hasattr(obj.data, "polygons"):
        for p in obj.data.polygons:
            p.use_smooth = True
    obj["rungproof_asset"] = True
    obj["rungproof_collision"] = collision
    return obj


def box(name, loc, dims, material, bevel=.004, collision=True, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bevel, False, collision)


def cyl(name, loc, radius, depth, material, axis="Z", vertices=64, collision=True):
    rot = (math.pi/2,0,0) if axis == "Y" else ((0,math.pi/2,0) if axis == "X" else (0,0,0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    return finish(bpy.context.object, name, material, .002, True, collision)


def torus(name, loc, major, minor, material, axis="Z", collision=True):
    rot = (math.pi/2,0,0) if axis == "Y" else ((0,math.pi/2,0) if axis == "X" else (0,0,0))
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=64,
                                    minor_segments=16, location=loc, rotation=rot)
    return finish(bpy.context.object, name, material, 0, True, collision)


def tube(name, start, end, radius, material, collision=True):
    a, b = Vector(start), Vector(end)
    d = b-a
    obj = cyl(name, (a+b)/2, radius, d.length, material, "Z", 48, collision)
    obj.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    return obj


def curved_tube(name, points, radius, material, collision=False):
    curve=bpy.data.curves.new(name,"CURVE");curve.dimensions="3D";curve.bevel_depth=radius;curve.bevel_resolution=4;curve.resolution_u=16
    spline=curve.splines.new("BEZIER");spline.bezier_points.add(len(points)-1)
    for bp,co in zip(spline.bezier_points,points):bp.co=co;bp.handle_left_type="AUTO";bp.handle_right_type="AUTO"
    obj=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material);bpy.ops.object.select_all(action="DESELECT");obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target="MESH")
    return finish(obj,name,None,0,True,collision)


def cut_cyl(target, name, loc, radius, depth, axis="Z", vertices=64):
    rot = (math.pi/2,0,0) if axis == "Y" else ((0,math.pi/2,0) if axis == "X" else (0,0,0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    cutter = bpy.context.object
    mod = target.modifiers.new(name, "BOOLEAN")
    mod.operation = "DIFFERENCE"; mod.solver = "EXACT"; mod.object = cutter
    # Boolean must precede the presentation bevel; otherwise Blender may apply
    # the bore against already-beveled topology and leave a false dark cap.
    target.modifiers.move(len(target.modifiers)-1, 0)
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def cut_box(target, name, loc, dims):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    cutter=bpy.context.object;cutter.dimensions=dims;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=target.modifiers.new(name,"BOOLEAN");mod.operation="DIFFERENCE";mod.solver="EXACT";mod.object=cutter
    target.modifiers.move(len(target.modifiers)-1,0);bpy.context.view_layer.objects.active=target
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)


def text_label(name, body, loc, size, material, rotation=(-math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rotation)
    obj=bpy.context.object;obj.data.body=body;obj.data.align_x="CENTER";obj.data.align_y="CENTER";obj.data.size=size;obj.data.extrude=.002;obj.data.bevel_depth=.001
    if rotation==(-math.pi/2,0,0):obj.scale.x=-1
    bpy.ops.object.convert(target="MESH")
    return finish(obj,name,material,0,False,False)


def common():
    return {
        "steel": mat("Machined steel", (.42,.46,.47), .86, .21),
        "dark": mat("Dark cast metal", (.035,.055,.065), .70, .31),
        "aluminum": mat("Extruded aluminum", (.60,.64,.65), .78, .23),
        "blue": mat("Industrial blue", (.025,.20,.52), .32, .28),
        "yellow": mat("Safety yellow", (.95,.53,.015), .20, .31),
        "red": mat("Industrial red", (.65,.018,.012), .25, .30),
        "orange": mat("Elastomer orange", (.90,.20,.012), .02, .61),
        "black": mat("Black polymer", (.012,.017,.019), .06, .52),
        "brass": mat("Brass fitting", (.66,.48,.17), .72, .24),
        "rubber": mat("Black rubber", (.008,.012,.013), .01, .78),
        "copper": mat("Copper winding", (.46,.12,.035), .72, .28),
        "white": mat("Label white", (.84,.87,.87), .08, .44),
        "tube_blue": mat("Pneumatic tube blue", (.02,.46,.83), .04, .30),
    }


def bolt(M, name, loc, axis="Z", radius=.028, depth=.035):
    cyl(name, loc, radius, depth, M["steel"], axis, 12)


def pneumatic_port(M, name, loc, axis="Z"):
    cyl(name+"_thread", loc, .045, .075, M["brass"], axis, 12)
    cyl(name+"_elbow", (loc[0],loc[1],loc[2]+(.045 if axis=="Z" else 0)), .06, .04, M["black"], axis, 24)


def motor_body(M, origin=(0,0,0), length=.62, radius=.23, prefix="MOTOR"):
    x,y,z=origin
    cyl(prefix+"_frame", (x,y,z), radius, length, M["blue"], "X", 96)
    for px in (x-length/2+.035,x+length/2-.035):
        cyl(prefix+f"_endbell_{px}", (px,y,z), radius*1.04, .07, M["dark"], "X", 96)
    for angle in range(0,360,30):
        a=math.radians(angle)
        yy=y+math.cos(a)*(radius+.012); zz=z+math.sin(a)*(radius+.012)
        box(prefix+f"_fin_{angle}",(x,yy,zz),(length*.72,.025,.045),M["blue"],.004,False,
            rotation=(angle*math.pi/180,0,0))
    box(prefix+"_terminal_box",(x,y-.22,z+.25),(.25,.25,.18),M["dark"],.018)
    cyl(prefix+"_gland",(x,y-.36,z+.25),.047,.10,M["black"],"Y",32)
    for sx in (-.18,.18): box(prefix+f"_foot_{sx}",(x+sx,y,z-.25),(.20,.44,.07),M["dark"],.008)


def iso_tie_rod_cylinder(M):
    # Installed horizontal cylinder with four tie rods, clevises and two ports.
    cyl("BARREL_honed",(0,0,.58),.205,.82,M["aluminum"],"X",96)
    for x in (-.46,.46):
        box(f"END_CAP_{x}",(x,0,.58),(.11,.52,.52),M["dark"],.025)
        for y in (-.20,.20):
            for z in (.38,.78): bolt(M,f"TIEROD_NUT_{x}_{y}_{z}",(x+(.062 if x>0 else -.062),y,z),"X",.035,.035)
    for y in (-.20,.20):
        for z in (.38,.78): tube(f"TIE_ROD_{y}_{z}",(-.46,y,z),(.46,y,z),.022,M["steel"])
    rod=cyl("KIN_rod",(.80,0,.58),.075,.70,M["steel"],"X",96)
    cyl("ROD_CLEVIS",(1.17,0,.58),.15,.18,M["dark"],"Y",64)
    cut_cyl(bpy.context.object,"CLEVIS_PIN_BORE",(1.17,0,.58),.055,.22,"Y")
    box("REAR_CLEVIS",(-.61,0,.58),(.20,.20,.38),M["dark"],.025)
    pneumatic_port(M,"PORT_A",(-.28,0,.84),"Z"); pneumatic_port(M,"PORT_B",(.28,0,.84),"Z")
    box("MOUNT_BASE",(0,0,.12),(1.15,.54,.10),M["dark"],.012)


def guided_cylinder(M):
    box("GUIDE_BODY",(-.28,0,.42),(.82,.72,.72),M["aluminum"],.035)
    box("FRONT_BEARING_BLOCK",(.17,0,.42),(.14,.76,.76),M["dark"],.022)
    plate=box("KIN_TOOL_PLATE",(.82,0,.42),(.15,.82,.84),M["blue"],.025)
    for y in (-.23,.23):
        cyl(f"GUIDE_ROD_{y}",(.49,y,.42),.065,.64,M["steel"],"X",96)
        for z in (.18,.66): bolt(M,f"PLATE_BOLT_{y}_{z}",(.92,y,z),"X",.035,.035)
    cyl("PISTON_ROD",(.49,0,.42),.075,.64,M["steel"],"X",96)
    for y in (-.23,.23):
        pneumatic_port(M,f"PORT_{y}",(-.30,y,.81),"Z")
        tube(f"AIR_TUBE_{y}",(-.30,y,.90),(-.55,y,1.10),.024,M["tube_blue"],False)
    box("SENSOR_GROOVE_TOP",(-.25,-.20,.785),(.48,.07,.018),M["dark"],.002,False)
    box("MOUNT_BASE",(-.12,0,.08),(1.25,.90,.10),M["dark"],.012)
    text_label("PNEUMATIC_LABEL","GUIDED CYLINDER",(-.35,-.366,.42),.055,M["blue"],(math.pi/2,0,0))


def rodless_cylinder(M):
    box("EXTRUSION",(0,0,.34),(1.72,.34,.36),M["aluminum"],.045)
    for x in (-.91,.91): box(f"END_CAP_{x}",(x,0,.34),(.13,.43,.45),M["dark"],.025)
    box("SEAL_SLOT",(0,0,.535),(1.55,.035,.018),M["black"],.002,False)
    carriage=box("KIN_CARRIAGE",(.18,0,.64),(.42,.58,.20),M["blue"],.025)
    for x in (.03,.33):
        for y in (-.20,.20): bolt(M,f"CARRIAGE_BOLT_{x}_{y}",(x,y,.752),"Z",.026,.026)
    pneumatic_port(M,"PORT_LEFT",(-.91,-.24,.34),"Y");pneumatic_port(M,"PORT_RIGHT",(.91,-.24,.34),"Y")
    for x in (-.65,.65): box(f"FOOT_{x}",(x,0,.08),(.28,.56,.08),M["dark"],.010)


def rotary_actuator(M):
    box("ACTUATOR_BODY",(0,0,.40),(.66,.62,.58),M["aluminum"],.055)
    # Opposed rack-piston end housings give the classic pneumatic rotary-actuator form.
    cyl("RACK_PISTON_LEFT",(-.42,0,.40),.205,.30,M["blue"],"X",96)
    cyl("RACK_PISTON_RIGHT",(.42,0,.40),.205,.30,M["blue"],"X",96)
    cyl("LEFT_END_CAP",(-.60,0,.40),.22,.08,M["dark"],"X",96)
    cyl("RIGHT_END_CAP",(.60,0,.40),.22,.08,M["dark"],"X",96)
    cyl("KIN_OUTPUT_FLANGE",(0,0,.735),.22,.12,M["dark"],"Z",96)
    cyl("OUTPUT_SHAFT",(0,0,.86),.075,.18,M["steel"],"Z",96)
    for a in range(0,360,90):
        x=.145*math.cos(math.radians(a));y=.145*math.sin(math.radians(a))
        bolt(M,f"FLANGE_BOLT_{a}",(x,y,.802),"Z",.025,.025)
    pneumatic_port(M,"PORT_CW",(-.20,-.36,.42),"Y");pneumatic_port(M,"PORT_CCW",(.20,-.36,.42),"Y")
    box("ANGLE_STOP_LEFT",(-.39,.18,.56),(.14,.13,.17),M["dark"],.016)
    box("ANGLE_STOP_RIGHT",(.39,.18,.56),(.14,.13,.17),M["dark"],.016)
    box("MOUNT",(0,0,.08),(.82,.72,.10),M["dark"],.012)
    # Calibrated rotary table, hard-stop scale, and paired air lines distinguish
    # this from an electric right-angle gearbox.
    torus("ANGLE_SCALE",(0,0,.80),.250,.012,M["white"],"Z",False)
    for a in range(0,181,15):
        rad=math.radians(a);x=.250*math.cos(rad);y=.250*math.sin(rad)
        box(f"ANGLE_TICK_{a}",(x,y,.823),(.012,.045 if a%45 else .070,.015),M["black"],.001,False,rotation=(0,0,rad))
    for x,color in ((-.20,M["tube_blue"]),(.20,M["red"])):
        tube(f"AIR_LINE_{x}",(x,-.43,.42),(x,-.78,.62),.025,color,False)
    text_label("ROTARY_LABEL","PNEUMATIC 180",(0,-.316,.42),.055,M["blue"],(math.pi/2,0,0))
    text_label("PORT_A_LABEL","A",(-.20,-.374,.53),.050,M["white"],(math.pi/2,0,0))
    text_label("PORT_B_LABEL","B",(.20,-.374,.53),.050,M["white"],(math.pi/2,0,0))
    box("ROTATION_POINTER",(.18,0,.83),(.24,.035,.035),M["red"],.006,False)


def hydraulic_welded_cylinder(M):
    cyl("HYD_BARREL",(0,0,.63),.27,1.15,M["blue"],"X",128)
    for x in (-.62,.62):
        cyl(f"END_CAP_{x}",(x,0,.63),.32,.13,M["dark"],"X",96)
        torus(f"CAP_SEAL_{x}",(x+(.07 if x>0 else -.07),0,.63),.275,.018,M["black"],"X")
    cyl("KIN_CHROME_ROD",(1.02,0,.63),.105,.82,M["steel"],"X",128)
    clevis=box("ROD_CLEVIS",(1.50,0,.63),(.30,.38,.45),M["dark"],.045)
    cut_cyl(clevis,"CLEVIS_BORE",(1.50,0,.63),.09,.50,"Y")
    rear=box("REAR_CLEVIS",(-.86,0,.63),(.30,.38,.45),M["dark"],.045)
    cut_cyl(rear,"REAR_BORE",(-.86,0,.63),.09,.50,"Y")
    for x in (-.42,.42):
        cyl(f"SAE_PORT_{x}",(x,-.25,.88),.065,.12,M["steel"],"Z",16)
        tube(f"HOSE_{x}",(x,-.25,.95),(x,-.65,1.06),.040,M["rubber"],False)


def parallel_gripper(M):
    # Horizontal robot wrist -> gripper body -> opposed jaws makes the tool
    # direction and gripping action readable without labels or scene context.
    cyl("ROBOT_WRIST",(-.72,0,.56),.23,.34,M["blue"],"X",96)
    cyl("ROBOT_WRIST_FLANGE",(-.50,0,.56),.27,.12,M["dark"],"X",96)
    for a in range(0,360,90):
        y=.19*math.cos(math.radians(a));z=.56+.19*math.sin(math.radians(a));bolt(M,f"WRIST_BOLT_{a}",(-.565,y,z),"X",.025,.025)
    box("GRIPPER_BODY",(-.12,0,.56),(.62,.70,.46),M["blue"],.045)
    box("JAW_GUIDE",(.23,0,.56),(.14,.82,.38),M["steel"],.015)
    for y in (-.23,.23):
        box(f"KIN_JAW_SLIDE_{y}",(.28,y,.56),(.20,.20,.24),M["steel"],.018)
        box(f"FINGER_{y}",(.63,y,.56),(.58,.16,.24),M["dark"],.014)
        box(f"HOOK_{y}",(.88,y,.42),(.16,.16,.34),M["dark"],.014)
        box(f"GRIP_PAD_{y}",(.88,y-(.09 if y>0 else -.09),.42),(.18,.035,.23),M["rubber"],.004)
    pneumatic_port(M,"PORT_OPEN",(-.18,-.38,.66),"Y");pneumatic_port(M,"PORT_CLOSE",(.05,-.38,.66),"Y")
    curved_tube("OPEN_AIR",[(-.18,-.44,.66),(-.38,-.62,.78),(-.62,-.56,.82)],.022,M["tube_blue"])
    curved_tube("CLOSE_AIR",[(.05,-.44,.66),(-.10,-.64,.92),(-.48,-.58,.96)],.022,M["red"])
    cyl("WORKPIECE",(.88,0,.42),.13,.52,M["aluminum"],"Z",96,False)
    text_label("GRIPPER_LABEL","2-JAW GRIPPER",(-.12,-.356,.56),.052,M["white"],(math.pi/2,0,0))
    box("DISPLAY_BASE",(.10,0,.06),(2.05,1.12,.08),M["dark"],.010,False)


def vacuum_gripper(M):
    for y in (-.27,.27):box(f"CROSSBEAM_{y}",(0,y,.76),(1.35,.11,.12),M["aluminum"],.018)
    box("VACUUM_MANIFOLD",(-.20,0,.92),(.55,.42,.24),M["blue"],.030)
    cyl("ROBOT_INTERFACE",(-.78,0,.92),.22,.16,M["dark"],"X",96)
    cyl("ROBOT_WRIST",(-1.02,0,.92),.18,.40,M["blue"],"X",96)
    for x in (-.38,.38):
        for y in (-.22,.22):
            tube(f"DROP_{x}_{y}",(x,y,.76),(x,y,.34),.032,M["steel"])
            cyl(f"COMPLIANCE_SLEEVE_{x}_{y}",(x,y,.52),.060,.16,M["black"],"Z",48)
            cyl(f"CUP_NECK_{x}_{y}",(x,y,.34),.055,.10,M["black"],"Z",48)
            # Cup lip plus tapered bell creates the unmistakable vacuum-tool silhouette.
            bpy.ops.mesh.primitive_cone_add(vertices=64, radius1=.135, radius2=.060, depth=.14, location=(x,y,.23))
            finish(bpy.context.object,f"SUCTION_CUP_{x}_{y}",M["rubber"],.002,True)
            torus(f"CUP_LIP_{x}_{y}",(x,y,.16),.112,.018,M["rubber"],"Z")
            curved_tube(f"VACUUM_HOSE_{x}_{y}",[(x,y,.62),(x*.65,y*.65,.90),(-.18,0,.98)],.018,M["tube_blue"])
    cyl("VACUUM_INLET",(-.20,-.28,.92),.055,.18,M["brass"],"Y",16)
    box("CARTON_WORKPIECE",(0,0,-.02),(1.35,.90,.30),mat("Carton",(.48,.27,.10),.01,.72),.025,False)
    box("VACUUM_EJECTOR",(.15,0,.96),(.24,.18,.16),M["yellow"],.018)
    curved_tube("SUPPLY_AIR",[(.26,0,.96),(.48,-.22,1.08),(.70,-.42,.96)],.022,M["tube_blue"])
    text_label("VACUUM_LABEL","VACUUM TOOL",(-.20,-.216,.92),.047,M["white"],(math.pi/2,0,0))


def servo_motor(M):
    box("SERVO_FRAME",(0,0,.48),(.68,.48,.48),M["blue"],.035)
    for x in (-.22,-.08,.08,.22): box(f"SERVO_GROOVE_{x}",(x,-.243,.48),(.018,.018,.39),M["dark"],.002,False)
    box("FRONT_FLANGE",(.40,0,.48),(.12,.66,.66),M["aluminum"],.018)
    cyl("KIN_SERVO_SHAFT",(.57,0,.48),.075,.28,M["steel"],"X",96)
    for y in (-.25,.25):
        for z in (.23,.73): bolt(M,f"FLANGE_BOLT_{y}_{z}",(.47,y,z),"X",.028,.025)
    cyl("POWER_CONNECTOR",(-.12,0,.78),.080,.16,M["orange"],"Z",32)
    cyl("ENCODER_CONNECTOR",(.16,0,.78),.065,.14,M["orange"],"Z",32)
    curved_tube("POWER_CABLE",[(-.12,0,.86),(-.20,-.22,1.02),(-.38,-.48,.88)],.038,M["rubber"])
    curved_tube("ENCODER_CABLE",[(.16,0,.85),(.24,-.18,.98),(.34,-.43,.82)],.030,M["rubber"])
    cyl("ENCODER_CAP",(-.40,0,.48),.19,.10,M["blue"],"X",96)
    text_label("SERVO_LABEL","AC SERVO",(0,-.246,.48),.060,M["white"],(math.pi/2,0,0))


def stepper_motor(M):
    box("NEMA_FRAME",(0,0,.47),(.58,.56,.56),M["dark"],.028)
    for x in (-.20,0,.20): box(f"LAMINATION_{x}",(x,0,.47),(.045,.565,.565),M["steel"],.004)
    box("FRONT_PLATE",(.33,0,.47),(.10,.62,.62),M["aluminum"],.014)
    cyl("KIN_STEPPER_SHAFT",(.47,0,.47),.065,.30,M["steel"],"X",96)
    for y in (-.245,.245):
        for z in (.225,.715): bolt(M,f"MOUNT_HOLE_{y}_{z}",(.39,y,z),"X",.027,.025)
    tube("MOTOR_CABLE",(-.31,-.18,.32),(-.62,-.45,.18),.035,M["rubber"],False)
    for i,c in enumerate((M["red"],M["blue"],M["white"],M["black"])):
        tube(f"LEAD_{i}",(-.61,-.45+i*.025,.18),(-.82,-.55+i*.04,.12),.010,c,False)


def helical_gearmotor(M):
    motor_body(M,(-.45,0,.48),.66,.23,"MOTOR")
    cyl("INLINE_GEARBOX",(.05,0,.48),.34,.48,M["aluminum"],"X",12)
    cyl("OUTPUT_FLANGE",(.34,0,.48),.27,.12,M["dark"],"X",96)
    cyl("KIN_OUTPUT_SHAFT",(.49,0,.48),.085,.30,M["steel"],"X",96)
    for a in range(0,360,45):
        y=.21*math.cos(math.radians(a));z=.48+.21*math.sin(math.radians(a))
        bolt(M,f"GEARBOX_BOLT_{a}",(.405,y,z),"X",.022,.022)
    box("GEARBOX_FOOT",(.05,0,.14),(.62,.62,.10),M["dark"],.012)


def worm_gearmotor(M):
    motor_body(M,(-.46,0,.48),.62,.22,"MOTOR")
    box("WORM_GEARBOX",(.05,0,.50),(.56,.60,.64),M["aluminum"],.085)
    cyl("OUTPUT_FLANGE",(.05,.36,.50),.25,.13,M["dark"],"Y",96)
    cyl("KIN_OUTPUT_HUB",(.05,.52,.50),.085,.34,M["steel"],"Y",96)
    box("OUTPUT_KEY",(.05,.63,.585),(.055,.15,.035),M["steel"],.003)
    for side in (-1,1):
        for z in (.31,.69): box(f"GEARBOX_RIB_{side}_{z}",(side*.25,0,z),(.045,.62,.055),M["dark"],.005)
    for a in range(0,360,90):
        x=.05+.18*math.cos(math.radians(a));z=.50+.18*math.sin(math.radians(a))
        bolt(M,f"OUTPUT_BOLT_{a}",(x,.43,z),"Y",.025,.025)
    cyl("OIL_FILL",(.05,0,.86),.045,.10,M["brass"],"Z",12)
    cyl("OIL_SIGHT",(.34,-.16,.43),.050,.025,M["orange"],"X",48,False)
    box("GEARBOX_FOOT",(.05,0,.13),(.70,.72,.10),M["dark"],.012)
    # Inspection-side cutaway exposes a bronze worm wheel and tangent worm,
    # eliminating pump/blower ambiguity while remaining a plausible training asset.
    cyl("INSPECTION_RING",(.05,-.35,.50),.245,.08,M["dark"],"Y",96,False)
    cyl("WORM_WHEEL",(.05,-.405,.50),.185,.035,M["brass"],"Y",48,False)
    for a in range(0,360,15):
        x=.05+.19*math.cos(math.radians(a));z=.50+.19*math.sin(math.radians(a))
        box(f"WORM_WHEEL_TOOTH_{a}",(x,-.426,z),(.045,.035,.045),M["brass"],.003,False)
    cyl("WORM_SHAFT",(-.18,-.435,.27),.040,.46,M["steel"],"X",64,False)
    for x in (-.34,-.27,-.20,-.13,-.06):torus(f"WORM_THREAD_{x}",(x,-.435,.27),.045,.008,M["steel"],"X",False)
    box("REDUCER_NAMEPLATE",(.05,-.326,.78),(.43,.018,.14),M["blue"],.004,False)
    text_label("RATIO_PLATE","WORM REDUCER 40:1",(.05,-.338,.78),.041,M["white"],(math.pi/2,0,0))


def pillow_block(M):
    body=box("CAST_HOUSING",(0,0,.34),(.78,.30,.47),M["blue"],.09)
    cut_cyl(body,"BEARING_BORE",(0,0,.42),.155,.38,"Y")
    cyl("KIN_INNER_RACE",(0,0,.42),.155,.34,M["steel"],"Y",96)
    cut_cyl(bpy.context.object,"SHAFT_BORE",(0,0,.42),.090,.40,"Y")
    cyl("SHAFT",(0,0,.42),.087,.86,M["steel"],"Y",96,False)
    box("BASE",(0,0,.10),(1.12,.46,.14),M["blue"],.035)
    for x in (-.42,.42):
        bolt(M,f"ANCHOR_{x}",(x,0,.18),"Z",.045,.05)
        torus(f"ANCHOR_WASHER_{x}",(x,0,.185),.055,.012,M["steel"],"Z")
    cyl("GREASE_ZERK",(.20,0,.64),.025,.12,M["brass"],"Z",12)


def jaw_coupling(M):
    # Separated hubs expose the interlocking metal jaws and orange spider.
    cyl("KIN_HUB_INPUT",(-.31,0,.42),.22,.28,M["aluminum"],"X",96)
    cyl("KIN_HUB_OUTPUT",(.31,0,.42),.22,.28,M["aluminum"],"X",96)
    for side in (-1,1):
        x=side*.105
        for a in (0,120,240):
            ang=math.radians(a)
            box(f"JAW_{side}_{a}",(x,.13*math.cos(ang),.42+.13*math.sin(ang)),(.19,.115,.115),M["aluminum"],.018,
                rotation=(ang,0,0))
    cyl("ELASTOMER_SPIDER_CORE",(0,0,.42),.095,.16,M["orange"],"X",48,False)
    for a in (60,180,300):
        ang=math.radians(a)
        box(f"SPIDER_LOBE_{a}",(0,.135*math.cos(ang),.42+.135*math.sin(ang)),(.15,.13,.13),M["orange"],.035,False,
            rotation=(ang,0,0))
    for side in (-1,1):
        cyl(f"SHAFT_{side}",(side*.62,0,.42),.09,.68,M["steel"],"X",96,False)
        bolt(M,f"SETSCREW_{side}",(side*.31,-.17,.55),"Z",.027,.06)
    box("DISPLAY_BASE",(0,0,.08),(1.75,.62,.08),M["dark"],.010,False)


def linear_guide(M):
    rail=box("PROFILE_RAIL",(0,0,.17),(2.0,.28,.20),M["steel"],.012)
    for x in (-.80,-.40,0,.40,.80): cut_cyl(rail,f"RAIL_COUNTERBORE_{x}",(x,0,.25),.034,.08,"Z",48)
    for y in (-.145,.145): box(f"RACEWAY_GROOVE_{y}",(0,y,.18),(1.92,.018,.065),M["dark"],.002,False)
    carriage=box("KIN_GUIDE_CARRIAGE",(.20,0,.38),(.50,.58,.34),M["blue"],.045)
    cut_box(carriage,"CARRIAGE_WRAP_SLOT",(.20,0,.22),(.56,.31,.22))
    for x in (.04,.36):
        for y in (-.18,.18): bolt(M,f"CARRIAGE_BOLT_{x}_{y}",(x,y,.56),"Z",.025,.024)
    # Exposed bearing-return caps and wipers visually distinguish a linear guide from a plain block.
    for x in (-.05,.45): box(f"WIPER_{x}",(x,0,.38),(.045,.59,.30),M["black"],.012)


def ball_screw_actuator(M):
    for y in (-.27,.27): box(f"SIDE_RAIL_{y}",(0,y,.25),(2.15,.10,.22),M["aluminum"],.018)
    cyl("BALL_SCREW",(0,0,.31),.055,1.90,M["steel"],"X",96)
    for x in [i*.12-.84 for i in range(15)]: torus(f"SCREW_THREAD_{x}",(x,0,.31),.057,.006,M["steel"],"X",False)
    for x in (-1.03,1.03): box(f"END_SUPPORT_{x}",(x,0,.31),(.15,.68,.54),M["dark"],.025)
    box("KIN_BALL_NUT_CARRIAGE",(.18,0,.55),(.48,.72,.26),M["blue"],.035)
    cyl("BALL_NUT",(.18,0,.31),.14,.28,M["brass"],"X",64)
    # Integrated servo drive gives scale and makes the powered function explicit.
    box("SERVO_FRAME",(-1.34,0,.31),(.48,.50,.50),M["dark"],.030)
    box("SERVO_FLANGE",(-1.075,0,.31),(.10,.61,.61),M["aluminum"],.014)
    box("BASE",(0,0,.08),(2.85,.82,.10),M["dark"],.012)


def rack_pinion_actuator(M):
    box("BASE_EXTRUSION",(0,0,.18),(2.25,.66,.22),M["aluminum"],.022)
    box("RACK",(0,-.18,.34),(1.95,.11,.12),M["steel"],.004)
    for i in range(22):
        x=-.90+i*.086
        box(f"RACK_TOOTH_{i}",(x,-.18,.425),(.055,.11,.07),M["steel"],.003,rotation=(0,math.radians(45),0))
    box("KIN_CARRIAGE",(.12,0,.62),(.56,.72,.28),M["blue"],.035)
    cyl("PINION_COVER",(.12,-.38,.47),.20,.13,M["yellow"],"Y",64)
    cyl("KIN_PINION_SHAFT",(.12,-.48,.47),.07,.20,M["steel"],"Y",64)
    motor_body(M,(.12,-.82,.47),.48,.18,"DRIVE_MOTOR")
    for x in (-1.05,1.05): box(f"END_STOP_{x}",(x,0,.41),(.12,.72,.45),M["dark"],.018)


BUILDERS = {
    "iso_tie_rod_pneumatic_cylinder": iso_tie_rod_cylinder,
    "guided_pneumatic_cylinder": guided_cylinder,
    "rodless_pneumatic_cylinder": rodless_cylinder,
    "pneumatic_rotary_actuator": rotary_actuator,
    "hydraulic_welded_body_cylinder": hydraulic_welded_cylinder,
    "parallel_two_jaw_gripper": parallel_gripper,
    "vacuum_multi_cup_gripper": vacuum_gripper,
    "ac_servo_motor": servo_motor,
    "nema_stepper_motor": stepper_motor,
    "inline_helical_gearmotor": helical_gearmotor,
    "right_angle_worm_gearmotor": worm_gearmotor,
    "pillow_block_bearing": pillow_block,
    "flexible_jaw_coupling": jaw_coupling,
    "profile_rail_linear_guide": linear_guide,
    "ball_screw_linear_actuator": ball_screw_actuator,
    "rack_pinion_linear_actuator": rack_pinion_actuator,
}


def point(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat("-Z","Y").to_euler()


def save(slug, builder):
    clean(); M=common(); builder(M)
    root=BASE/slug
    for part in ("source","delivery","collision","review"): (root/part).mkdir(parents=True,exist_ok=True)
    for path in (root/"source",root/"review"):(path/".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"))
    objects=[o for o in bpy.context.scene.objects if o.type=="MESH" and o.get("rungproof_asset")]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    coll=[o for o in objects if o.get("rungproof_collision")]
    mins=Vector((1e9,1e9,1e9));maxs=Vector((-1e9,-1e9,-1e9))
    for o in coll:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c)
            mins.x,mins.y,mins.z=min(mins.x,w.x),min(mins.y,w.y),min(mins.z,w.z)
            maxs.x,maxs.y,maxs.z=max(maxs.x,w.x),max(maxs.y,w.y),max(maxs.z,w.z)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_cube_add(location=(mins+maxs)/2)
    proxy=bpy.context.object;proxy.name="COLLISION_primary";proxy.dimensions=maxs-mins
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.export_scene.gltf(filepath=str(root/"collision"/f"{slug}_collision.glb"),export_format="GLB",use_selection=True,export_apply=True)
    bpy.data.objects.remove(proxy,do_unlink=True)
    vmin=Vector((1e9,1e9,1e9));vmax=Vector((-1e9,-1e9,-1e9))
    for o in objects:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c)
            vmin.x,vmin.y,vmin.z=min(vmin.x,w.x),min(vmin.y,w.y),min(vmin.z,w.z)
            vmax.x,vmax.y,vmax.z=max(vmax.x,w.x),max(vmax.y,w.y),max(vmax.z,w.z)
    box("REVIEW_floor",(0,0,mins.z-.035),(max(4,vmax.x-vmin.x+1.5),max(4,vmax.y-vmin.y+1.5),.05),M["dark"],.002,False)
    bpy.context.object["rungproof_asset"]=False
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034)
    center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.23,2.25)
    for i,(loc,energy,size) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        data=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");data.energy=energy;data.shape="DISK";data.size=size
        light=bpy.data.objects.new(data.name,data);light.location=loc;bpy.context.collection.objects.link(light);point(light,center)
    data=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",data);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;data.lens=58
    scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for index,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.52);point(cam,center)
        scene.render.filepath=str(root/"review"/f"{slug}_{index+1:02d}.png");bpy.ops.render.render(write_still=True)
    primary_review=(root/"review"/f"{slug}_01.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)


asset_filter={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not asset_filter or slug in asset_filter: save(slug,builder)
print("MECHANICAL_MOTION_ASSETS_BUILT",len(BUILT))
