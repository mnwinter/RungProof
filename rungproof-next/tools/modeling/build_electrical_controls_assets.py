"""Build reusable electrical distribution and industrial-control hardware."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"electrical_controls";BUILT=[]

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
    if bevel:q=o.modifiers.new("Manufactured edge radius","BEVEL");q.width=bevel;q.segments=3;q.limit_method="ANGLE"
    if smooth and hasattr(o.data,"polygons"):
        for p in o.data.polygons:p.use_smooth=True
    o["rungproof_asset"]=True;o["rungproof_collision"]=collision;return o
def box(name,loc,dims,m,bevel=.004,collision=True,rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc,rotation=rotation);o=bpy.context.object;o.dimensions=dims;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m,bevel,False,collision)
def cyl(name,loc,r,depth,m,axis="Z",verts=48,collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc,rotation=rot);return finish(bpy.context.object,name,m,.002,True,collision)
def tube(name,a,b,r,m,collision=False):
    a,b=Vector(a),Vector(b);d=b-a;o=cyl(name,(a+b)/2,r,d.length,m,"Z",32,collision);o.rotation_euler=d.to_track_quat("Z","Y").to_euler();return o
def text(name,body,loc,size,m,rotation=(math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rotation);o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.002;o.data.bevel_depth=.001;bpy.ops.object.convert(target="MESH");return finish(o,name,m,0,False,False)
def common():return {"steel":mat("Enclosure gray",(.24,.29,.31),.58,.31),"dark":mat("Control dark",(.025,.04,.05),.25,.45),"black":mat("Insulator black",(.012,.016,.018),.03,.67),"white":mat("Label white",(.88,.90,.88),.02,.39),"blue":mat("Control blue",(.02,.22,.48),.22,.32),"green":mat("PCB green",(.01,.24,.09),.12,.42),"red":mat("Safety red",(.72,.015,.01),.18,.30),"yellow":mat("Safety yellow",(.95,.55,.01),.15,.31),"orange":mat("Terminal orange",(.95,.28,.015),.14,.34),"copper":mat("Copper",(.65,.25,.07),.75,.23),"screen":mat("Display",(.03,.35,.48),.08,.18),"alum":mat("Aluminum",(.58,.62,.63),.82,.21)}

def din_rail(M,length=.9,z=.18):
    box("DIN_RAIL",(0,.05,z),(length,.06,.055),M["alum"],.002);box("DIN_LIP_TOP",(0,.02,z+.045),(length,.025,.035),M["alum"],.002);box("DIN_LIP_BOTTOM",(0,.02,z-.045),(length,.025,.035),M["alum"],.002)
def screw(M,name,loc):cyl(name,loc,.022,.025,M["zinc"] if "zinc" in M else M["alum"],"Y",20,False)
def vent_slots(M,prefix,x0,z0,count,w=.28):
    for i in range(count):box(f"{prefix}_{i}",(x0,-.151,z0+i*.055),(w,.015,.022),M["black"],.002,False)

def mcc_bucket(M):
    # Open MCC section with the feeder bucket visibly racked partway out.
    box("MCC_SECTION",(0,.32,1.00),(1.28,.56,2.00),M["steel"],.035);box("OPEN_COMPARTMENT",(0,-.005,1.02),(1.10,.035,1.58),M["black"],.010,False)
    for z in (.22,.58,.94,1.30,1.66):box(f"CUBICLE_RAIL_{z}",(0,-.08,z),(1.08,.62,.055),M["alum"],.004)
    box("BUCKET_CASE",(0,-.42,.96),(1.04,.86,.58),M["steel"],.025);box("DOOR",(0,-.87,.96),(.98,.07,.54),M["steel"],.020);box("KIN_DISCONNECT_HANDLE",(-.28,-.93,1.00),(.13,.10,.34),M["red"],.025,rotation=(0,0,math.radians(-18)))
    box("NAMEPLATE",(.19,-.916,1.08),(.42,.018,.13),M["white"],.008,False);text("MCC_LABEL","MCC  FEEDER",(.19,-.932,1.08),.050,M["black"])
    for x,c in ((-.18,M["red"]),(0,M["yellow"]),(.18,M["green"])):cyl(f"PILOT_{x}",(x,-.43,.55),.055,.045,c,"Y",32,False)
    for x in (-.32,0,.32):box(f"BUS_STAB_{x}",(x,.06,1.02),(.11,.40,.20),M["copper"],.008);box("RACKING_SOCKET",(0,-.93,.82),(.13,.05,.13),M["black"],.008)
def vfd(M):
    box("HEATSINK_BLOCK",(0,.17,.60),(.82,.30,1.20),M["alum"],.018);box("VFD_BODY",(0,-.06,.60),(.70,.34,1.18),M["dark"],.045);box("KEYPAD",(0,-.26,.82),(.45,.055,.42),M["steel"],.025);box("DISPLAY",(0,-.295,.93),(.28,.012,.12),M["screen"],.006,False);text("DISPLAY_TEXT","60.0 Hz",(0,-.304,.93),.045,M["white"])
    for i,x in enumerate((-.14,0,.14)):cyl(f"KEY_{i}",(x,-.235,.72),.038,.025,(M["green"] if i==0 else M["red"] if i==2 else M["white"]),"Y",24,False)
    for x in [-.36+i*.09 for i in range(9)]:box(f"HEATSINK_FIN_{x}",(x,.37,.60),(.035,.22,1.12),M["alum"],.003)
    text("VFD_LABEL","VARIABLE FREQUENCY DRIVE",(0,-.245,.37),.040,M["white"]);for_x=(-.22,0,.22)
    for x in for_x:box(f"POWER_TERMINAL_{x}",(x,-.20,.18),(.14,.10,.13),M["orange"],.012)
def servo_drive(M):
    # Narrow book-form axis amplifier: deep extruded heat sink, DC-bus/power
    # terminals, removable I/O plugs, encoder D-sub and dual motion-network ports.
    box("SERVO_HEATSINK",(0,.14,.62),(.42,.20,1.12),M["alum"],.014)
    # Rear fins stay within the amplifier's book-form envelope instead of
    # dominating the silhouette like transformer cooling radiators.
    for x in (-.18,-.12,-.06,0,.06,.12,.18):
        box(f"HEATSINK_FIN_{x}",(x,.29,.62),(.018,.18,1.04),M["alum"],.002)
    box("SERVO_DRIVE",(0,-.08,.62),(.50,.38,1.20),M["dark"],.030)
    box("STATUS_PANEL",(0,-.285,.94),(.40,.035,.34),M["steel"],.018,False)
    box("AXIS_DISPLAY",(-.07,-.306,1.01),(.20,.012,.10),M["screen"],.005,False)
    text("SERVO_TEXT","AXIS 1",(-.07,-.316,1.01),.034,M["white"])
    for i,z in enumerate((.91,.84,.77)):
        cyl(f"SERVO_STATUS_{i}",(.13,-.292,z),.014,.015,(M["green"] if i<2 else M["red"]),"Y",16,False)
    # Three-phase motor/brake connector and removable control terminal strip.
    text("SERVO_DRIVE_LABEL","SERVO DRIVE",(0,-.316,.81),.044,M["white"])
    box("MOTOR_POWER_CONNECTOR",(-.12,-.31,.59),(.19,.12,.20),M["orange"],.012)
    text("MOTOR_LABEL","MOTOR",(-.12,-.379,.70),.026,M["white"])
    for i,x in enumerate((-.18,-.12,-.06)):cyl(f"MOTOR_SOCKET_{i}",(x,-.378,.59),.018,.018,M["black"],"Y",16,False)
    box("CONTROL_IO_CONNECTOR",(.12,-.31,.59),(.17,.12,.20),M["blue"],.010)
    for row,z in enumerate((.54,.60,.66)):
        for col,x in enumerate((.08,.12,.16)):cyl(f"IO_SOCKET_{row}_{col}",(x,-.378,z),.010,.016,M["black"],"Y",12,False)
    # Encoder feedback connector with screw posts.
    box("ENCODER_DSUB",(-.11,-.314,.37),(.22,.12,.13),M["blue"],.012)
    text("ENCODER_LABEL","ENC",(-.11,-.383,.45),.024,M["white"])
    for x in (-.20,-.02):cyl(f"ENCODER_SCREW_{x}",(x,-.382,.37),.014,.018,M["alum"],"Y",14,False)
    # Two daisy-chain Ethernet/motion ports and a separate brake port.
    for i,x in enumerate((.05,.16)):
        box(f"MOTION_RJ45_{i}",(x,-.314,.36),(.085,.12,.14),M["black"],.005)
        cyl(f"MOTION_LED_{i}",(x,-.383,.45),.010,.012,M["green"],"Y",12,False)
    box("BRAKE_CONNECTOR",(0,-.314,.18),(.28,.12,.11),M["orange"],.010)
    for i,x in enumerate((-.105,-.035,.035,.105)):cyl(f"BRAKE_SOCKET_{i}",(x,-.382,.18),.013,.016,M["black"],"Y",14,False)
    # Covered line/DC-bus plug; exposed copper bars would read as a transformer.
    box("DC_BUS_COVER",(0,-.03,1.25),(.42,.28,.14),M["dark"],.012)
    for i,x in enumerate((-.15,-.05,.05,.15)):box(f"DC_BUS_PLUG_{i}",(x,-.19,1.25),(.075,.08,.10),M["orange"],.006)
def plc_rack(M):
    box("BACKPLANE",(0,.06,.42),(1.50,.12,.72),M["steel"],.020)
    widths=[.26,.20,.20,.20,.20,.20];x=-.57
    for i,w in enumerate(widths):
        color=M["dark"] if i==0 else M["blue"] if i==1 else M["steel"];box(f"MODULE_{i}",(x+w/2,-.04,.44),(w,.20,.62),color,.018);text(f"MODULE_LABEL_{i}",("POWER" if i==0 else "CPU" if i==1 else f"I/O {i-1}"),(x+w/2,-.151,.62),.043,M["white"])
        for j in range(8):cyl(f"LED_{i}_{j}",(x+w/2,-.151,.25+j*.045),.012,.012,(M["green"] if j%3 else M["yellow"]),"Y",16,False)
        x+=w+.015
    box("ETHERNET_PORT",(-.32,-.16,.23),(.10,.05,.11),M["black"],.003);box("KEY_SWITCH",(-.32,-.17,.78),(.08,.05,.08),M["white"],.006)
def hmi(M):
    box("HMI_BEZEL",(0,0,.48),(1.22,.14,.88),M["dark"],.035);box("TOUCHSCREEN",(0,-.085,.50),(1.04,.018,.70),M["screen"],.015,False)
    box("UI_HEADER",(0,-.101,.77),(1.00,.008,.12),M["blue"],.004,False);box("UI_PROCESS",(-.24,-.102,.48),(.46,.008,.36),M["green"],.004,False);box("UI_ALARMS",(.31,-.102,.48),(.50,.008,.36),M["steel"],.004,False)
    text("HMI_SCREEN","LINE 1   AUTO\nRUNNING   125 ppm",(0,-.113,.51),.070,M["white"]);text("HMI_HEADER","OPERATOR INTERFACE",(0,-.113,.77),.052,M["white"])
    for x in (-.50,.50):
        for z in (.13,.83):cyl(f"BEZEL_SCREW_{x}_{z}",(x,-.10,z),.018,.015,M["alum"],"Y",16,False)
def transformer(M):
    box("CORE_CENTER",(0,0,.55),(.42,.50,.72),M["steel"],.015);box("WINDING_PRIMARY",(-.28,0,.55),(.26,.54,.62),M["copper"],.04);box("WINDING_SECONDARY",(.28,0,.55),(.26,.54,.62),M["copper"],.04)
    for z in (.20,.90):box(f"CORE_YOKE_{z}",(0,0,z),(.92,.56,.14),M["steel"],.012)
    for i,x in enumerate((-.36,-.18,.18,.36)):cyl(f"TERMINAL_{i}",(x,-.38,.95),.045,.20,M["black"],"Z",24);box("BASE",(0,0,.08),(1.05,.72,.14),M["steel"],.012)
def disconnect(M):
    box("DISCONNECT_BODY",(0,0,.60),(.70,.38,1.15),M["steel"],.035);cyl("KIN_HANDLE_HUB",(0,-.23,.67),.14,.09,M["yellow"],"Y",48);box("KIN_HANDLE",(.15,-.28,.80),(.12,.10,.55),M["red"],.035,rotation=(0,0,math.radians(-35)));text("DISCONNECT_LABEL","ON\n\nOFF",(-.22,-.245,.70),.075,M["black"])
    # A small sealed inspection row makes the modeled family visibly fused
    # without suggesting that a user can access energized line components.
    box("FUSE_VIEW_WINDOW",(0,-.205,.28),(.43,.020,.13),M["dark"],.010,False)
    for x in (-.13,0,.13):
        cyl(f"FUSE_INDICATOR_{x}",(x,-.222,.28),.036,.012,M["yellow"],"Y",20,False)
    text("FUSED_MARK","FUSED",(0,-.228,.14),.035,M["black"])
    for x in (-.20,0,.20):cyl(f"CABLE_GLAND_{x}",(x,0,.02),.06,.16,M["black"],"Z",32)
def breaker(M):
    box("MCCB_BODY",(0,0,.48),(.62,.34,.90),M["dark"],.035);box("KIN_BREAKER_HANDLE",(0,-.21,.55),(.20,.10,.34),M["white"],.028,rotation=(0,0,math.radians(-12)));text("BREAKER_LABEL","MCCB\n250 A",(0,-.226,.76),.060,M["white"])
    for x in (-.20,0,.20):
        box(f"LINE_LUG_{x}",(x,0,.98),(.14,.28,.16),M["copper"],.015);box(f"LOAD_LUG_{x}",(x,0,-.02),(.14,.28,.16),M["copper"],.015)
def terminal_strip(M):
    din_rail(M,1.50,.16)
    for i,x in enumerate([-.65+i*.10 for i in range(14)]):
        color=M["orange"] if i in (0,13) else M["blue"] if i in (5,6) else M["white"];box(f"TERMINAL_BLOCK_{i}",(x,0,.36),(.085,.25,.34),color,.008)
        for y in (-.14,.14):cyl(f"SCREW_{i}_{y}",(x,y,.43),.020,.035,M["alum"],"Y",18,False)
    box("END_STOP_LEFT",(-.76,0,.34),(.08,.28,.40),M["dark"],.008);box("END_STOP_RIGHT",(.76,0,.34),(.08,.28,.40),M["dark"],.008)
def power_supply(M):
    box("PSU_BODY",(0,0,.50),(.58,.34,.95),M["steel"],.025);text("PSU_LABEL","POWER SUPPLY\n24 VDC   10 A",(0,-.186,.67),.060,M["black"]);vent_slots(M,"PSU_VENT",0,.18,6,.42);cyl("DC_OK",(-.13,-.19,.40),.025,.025,M["green"],"Y",18,False);box("V_ADJUST",(.13,-.19,.40),(.08,.025,.08),M["orange"],.004)
    for row,z in enumerate((.08,.96)):
        for i,x in enumerate((-.18,-.06,.06,.18)):box(f"TERMINAL_{row}_{i}",(x,-.20,z),(.09,.12,.10),M["orange"],.007)
    for side in (-.30,.30):
        for i,z in enumerate((.28,.42,.56,.70)):box(f"PSU_SIDE_VENT_{side}_{i}",(side,0,z),(.018,.28,.055),M["black"],.002,False)
def ethernet_switch(M):
    box("SWITCH_BODY",(0,0,.36),(1.05,.34,.58),M["dark"],.025);text("SWITCH_LABEL","MANAGED ETHERNET",(0,-.186,.60),.055,M["white"])
    for i,x in enumerate([-.38+i*.11 for i in range(8)]):box(f"RJ45_{i}",(x,-.19,.34),(.085,.06,.12),M["black"],.004);cyl(f"LINK_LED_{i}",(x,-.225,.45),.012,.012,M["green"],"Y",12,False)
    for x in (-.42,.42):box(f"FIBER_PORT_{x}",(x,-.19,.16),(.14,.06,.10),M["blue"],.005)
def safety_relay(M):
    # 22.5 mm-class DIN safety module shown on a long rail.  The removable
    # terminal decks and dense wire-entry pattern prevent it reading as an enclosure.
    din_rail(M,1.10,.12)
    box("RELAY_BASE",(0,.03,.52),(.34,.38,.88),M["black"],.016)
    box("SAFETY_RELAY",(0,-.13,.52),(.30,.15,.74),M["yellow"],.018)
    box("TOP_TERMINAL_DECK",(0,-.15,.94),(.34,.28,.18),M["green"],.012)
    box("BOTTOM_TERMINAL_DECK",(0,-.15,.10),(.34,.28,.18),M["green"],.012)
    for row,z in enumerate((.10,.94)):
        for col,x in enumerate((-.12,-.04,.04,.12)):
            cyl(f"WIRE_ENTRY_{row}_{col}",(x,-.305,z),.018,.026,M["black"],"Y",16,False)
            cyl(f"TERM_SCREW_{row}_{col}",(x,-.19,z+.045),.014,.018,M["alum"],"Z",16,False)
    box("FUNCTION_PANEL",(0,-.225,.55),(.24,.035,.50),M["white"],.010,False)
    text("SAFETY_LABEL","DUAL CHANNEL\nSAFETY RELAY",(0,-.248,.67),.035,M["black"])
    for i,(x,z,c) in enumerate(((-.07,.52,M["green"]),(0,.52,M["green"]),(.07,.52,M["red"]))):
        cyl(f"STATUS_{i}",(x,-.253,z),.016,.015,c,"Y",16,False)
    cyl("RESET_DIAL",(0,-.255,.39),.040,.018,M["blue"],"Y",24,False)
    text("CHANNEL_MARKS","CH1   CH2\nRESET",(0,-.249,.30),.028,M["black"])
def motor_starter(M):
    box("CONTACTOR",(0,0,.65),(.50,.34,.62),M["dark"],.025);box("OVERLOAD",(0,0,.27),(.50,.34,.34),M["steel"],.025);text("STARTER_LABEL","MOTOR\nSTARTER",(0,-.19,.68),.055,M["white"])
    for row,z in enumerate((.07,.47,.90)):
        for i,x in enumerate((-.16,0,.16)):box(f"POWER_LUG_{row}_{i}",(x,-.18,z),(.10,.08,.10),M["copper"],.008)
    cyl("RESET",(-.12,-.19,.25),.035,.025,M["red"],"Y",20,False);cyl("CLASS_DIAL",(.12,-.19,.25),.045,.025,M["white"],"Y",20,False)
def soft_starter(M):
    box("SOFT_STARTER",(0,0,.64),(.64,.42,1.18),M["dark"],.035);box("KEYPAD",(0,-.24,.82),(.38,.06,.36),M["steel"],.020);box("DISPLAY",(0,-.275,.92),(.22,.012,.10),M["screen"],.004,False);text("SOFT_TEXT","SOFT STARTER",(0,-.284,.75),.047,M["white"]);vent_slots(M,"SINK",0,.34,5,.44)
    text("LINE_LABEL","LINE  L1   L2   L3",(0,-.235,1.17),.030,M["white"]);text("LOAD_LABEL","LOAD  T1   T2   T3",(0,-.285,.13),.030,M["white"])
    for i,x in enumerate((-.18,0,.18)):
        box(f"LINE_TERMINAL_{i}",(x,0,1.28),(.13,.34,.16),M["copper"],.008)
        # Load lugs project through the lower front face so both terminal sets
        # remain visible in ordinary catalog views.
        box(f"LOAD_TERMINAL_{i}",(x,-.24,.16),(.13,.18,.16),M["copper"],.008)

BUILDERS={"mcc_withdrawable_bucket":mcc_bucket,"wall_vfd":vfd,"book_servo_drive":servo_drive,"plc_rack":plc_rack,"industrial_hmi":hmi,"control_transformer":transformer,"fused_disconnect":disconnect,"molded_case_breaker":breaker,"din_terminal_strip":terminal_strip,"din_24v_power_supply":power_supply,"managed_ethernet_switch":ethernet_switch,"safety_relay":safety_relay,"contactor_overload_starter":motor_starter,"soft_starter":soft_starter}
def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat("-Z","Y").to_euler()
def save(slug,builder):
    clean();M=common();M["green"]=M["green"];builder(M);root=BASE/slug
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
    floor=box("REVIEW_floor",(0,0,mins.z-.035),(max(3.2,vmax.x-vmin.x+1.2),max(3.2,vmax.y-vmin.y+1.2),.05),M["steel"],.002,False);floor["rungproof_asset"]=False;world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034);center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.30,2.1)
    for i,(loc,e,s) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=60;scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for i,angle in enumerate((35,125,215,305)):a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.45);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    # Camera three is the front three-quarter view for this upright equipment
    # family.  It exposes the operator face, terminals, and withdrawal hardware
    # instead of treating a rear heat sink or enclosure side as the review image.
    primary_review=(root/"review"/f"{slug}_03.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)
flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("ELECTRICAL_CONTROL_ASSETS_BUILT",len(BUILT))
