"""Build factory utility-system assets with recognizable functional geometry."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"utilities";BUILT=[]
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
def sphere(n,l,rad,m,c=True,scale=(1,1,1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,radius=rad,location=l);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m,.002,True,c)
def tube(n,a,b,rad,m,c=True):
    a,b=Vector(a),Vector(b);v=b-a;o=cyl(n,(a+b)/2,rad,v.length,m,"Z",40,c);o.rotation_euler=v.to_track_quat("Z","Y").to_euler();return o
def torus(n,l,major,minor,m,axis="Z",c=False):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=12,location=l,rotation=rot);return finish(bpy.context.object,n,m,0,True,c)
def text(n,body,l,size,m):
    bpy.ops.object.text_add(location=l,rotation=(math.pi/2,0,0));o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.002;o.data.bevel_depth=.001;bpy.ops.object.convert(target="MESH");return finish(o,n,m,0,False,False)
def common():return {"steel":mat("Utility blue",(.025,.18,.32),.56,.30),"light":mat("Panel gray",(.55,.60,.61),.70,.25),"dark":mat("Machine dark",(.018,.030,.035),.20,.55),"ss":mat("Stainless",(.58,.63,.64),.86,.18),"copper":mat("Copper",(.60,.24,.07),.74,.22),"white":mat("Label white",(.90,.91,.88),.02,.38),"red":mat("Safety red",(.72,.018,.012),.18,.31),"green":mat("Status green",(.02,.55,.12),.08,.31),"yellow":mat("Safety yellow",(.96,.55,.01),.16,.30),"orange":mat("Utility orange",(.95,.27,.015),.20,.30),"black":mat("Rubber",(.01,.015,.018),.03,.68),"screen":mat("Display",(.02,.34,.46),.08,.18),"tan":mat("Insulation",(.58,.43,.24),.02,.72)}
def base(M,w,d,z=.08):box("BASE",(0,0,z/2),(w,d,z),M["dark"],.012)
def motor(M,l=(0,0,.35),axis="X",scale=1,node="MOTOR"):
    cyl(node,l,.22*scale,.52*scale,M["steel"],axis,64)
    for i in range(8):
        a=2*math.pi*i/8;off=.235*scale;box(f"{node}_FIN_{i}",(l[0],l[1]+off*math.cos(a),l[2]+off*math.sin(a)),(.42*scale,.035*scale,.07*scale),M["steel"],.003)
    box(f"{node}_JBOX",(l[0],l[1]-.27*scale,l[2]+.18*scale),(.24*scale,.20*scale,.20*scale),M["dark"],.015)
def gauge(M,l):
    cyl("GAUGE_BODY",l,.09,.06,M["light"],"Y",48,False);cyl("GAUGE_FACE",(l[0],l[1]-.035,l[2]),.073,.008,M["white"],"Y",48,False);tube("GAUGE_NEEDLE",(l[0],l[1]-.043,l[2]),(l[0]+.04,l[1]-.043,l[2]+.035),.006,M["red"],False)
def fan(M,l,r=.32,node="KIN_FAN"):
    torus(node+"_RING",l,r,.025,M["dark"],"Z");cyl(node+"_HUB",l,.055,.05,M["light"],"Z",32,False)
    for i in range(6):box(f"{node}_BLADE_{i}",(l[0]+.16*math.cos(i*math.pi/3),l[1]+.16*math.sin(i*math.pi/3),l[2]),(.28,.065,.025),M["light"],.015,False,r=(0,0,i*math.pi/3+.35))

def screw_compressor(M):
    base(M,2.4,1.15);box("COMPRESSOR_CABINET",(0,0,.78),(2.25,1.05,1.45),M["steel"],.06);box("SERVICE_DOOR",(-.47,-.545,.80),(1.05,.035,1.25),M["light"],.025,False);box("CONTROL_PANEL",(.67,-.57,.98),(.55,.05,.52),M["dark"],.025,False);box("DISPLAY",(.67,-.605,1.08),(.32,.015,.13),M["screen"],.006,False);text("LABEL","ROTARY SCREW\nAIR COMPRESSOR",(.67,-.616,.85),.055,M["white"])
    for x in (-.90,-.65,-.40,-.15):box(f"INLET_VENT_{x}",(x,-.565,.32),(.16,.02,.27),M["black"],.004,False)
    cyl("ESTOP",(.89,-.61,.72),.045,.025,M["red"],"Y",24,False);tube("AIR_OUT",(1.00,.20,1.30),(1.35,.20,1.30),.09,M["copper"])
def air_receiver(M):
    base(M,1.35,1.35);cyl("RECEIVER_SHELL",(0,0,1.35),.48,2.05,M["steel"],"Z",64);sphere("TOP_HEAD",(0,0,2.38),.48,M["steel"],scale=(1,1,.45));sphere("BOTTOM_HEAD",(0,0,.32),.48,M["steel"],scale=(1,1,.45))
    for x in (-.34,.34):tube(f"LEG_{x}",(x,0,.45),(x,0,.09),.07,M["dark"])
    tube("AIR_INLET",(-.72,0,1.45),(-.48,0,1.45),.10,M["ss"]);tube("AIR_OUTLET",(.48,0,1.75),(.72,0,1.75),.10,M["ss"]);tube("DRAIN",(0,0,.12),(0,0,-.05),.035,M["copper"]);gauge(M,(0,-.55,2.18));cyl("SAFETY_RELIEF",(.18,0,2.70),.045,.24,M["red"],"Z",32)
def refrigerated_dryer(M):
    base(M,1.45,1.00);box("DRYER_CABINET",(0,.12,.72),(1.32,.70,1.32),M["light"],.045);box("CONTROL_PANEL",(.28,-.255,.98),(.56,.035,.40),M["dark"],.018,False);box("DISPLAY",(.28,-.28,1.08),(.28,.012,.10),M["screen"],.005,False);text("DRYER_LABEL","REFRIGERATED\nAIR DRYER",(.28,-.29,.83),.042,M["white"])
    # Open lower service bay exposes the refrigeration circuit rather than
    # presenting as a generic electrical cabinet.
    box("SERVICE_BAY",(-.34,-.255,.47),(.52,.04,.62),M["dark"],.010,False);cyl("HERMETIC_COMPRESSOR",(-.38,-.34,.35),.16,.35,M["steel"],"Z",48);tube("REFRIGERANT_LINE_A",(-.38,-.34,.52),(-.08,-.34,.66),.025,M["copper"],False);tube("REFRIGERANT_LINE_B",(-.38,-.34,.18),(-.08,-.34,.28),.025,M["copper"],False);fan(M,(-.18,-.32,.72),.17,"KIN_DRYER_CONDENSER_FAN")
    for z in (.20,.29,.38,.47,.56,.65):box(f"CONDENSER_GRILLE_{z}",(.40,.485,z),(.38,.018,.035),M["black"],.002,False)
    tube("WET_AIR_IN",(-.88,0,1.10),(-.66,0,1.10),.08,M["red"]);tube("DRY_AIR_OUT",(.66,0,1.10),(.88,0,1.10),.08,M["steel"]);cyl("AUTO_DRAIN",(0,-.12,.08),.055,.16,M["orange"],"Z",32)
def desiccant_dryer(M):
    base(M,1.75,.95)
    for x in (-.43,.43):
        cyl(f"TOWER_{x}",(x,0,1.15),.28,1.65,M["steel"],"Z",64);sphere(f"TOP_{x}",(x,0,1.98),.28,M["steel"],scale=(1,1,.45));sphere(f"BOTTOM_{x}",(x,0,.32),.28,M["steel"],scale=(1,1,.45));cyl(f"VALVE_TOP_{x}",(x,0,2.23),.09,.18,M["orange"],"Z",32);cyl(f"VALVE_BOTTOM_{x}",(x,0,.08),.09,.18,M["orange"],"Z",32)
    tube("TOP_MANIFOLD",(-.43,0,2.28),(.43,0,2.28),.075,M["copper"]);tube("BOTTOM_MANIFOLD",(-.43,0,.02),(.43,0,.02),.075,M["copper"]);box("CONTROLLER",(0,-.43,1.15),(.44,.22,.52),M["dark"],.02);box("DISPLAY",(0,-.56,1.27),(.25,.02,.10),M["screen"],.004,False);text("LABEL","TWIN TOWER\nDESICCANT DRYER",(0,-.575,1.07),.036,M["white"])
def air_cooled_chiller(M):
    base(M,2.6,1.35);box("CHILLER_BODY",(0,0,.68),(2.48,1.22,1.22),M["light"],.05);box("CONTROL_PANEL",(0,-.64,.77),(.65,.04,.70),M["dark"],.02,False);box("DISPLAY",(0,-.67,.90),(.34,.015,.13),M["screen"],.005,False);text("LABEL","AIR COOLED\nWATER CHILLER",(0,-.68,.60),.050,M["white"])
    for x in (-.72,.72):fan(M,(x,0,1.32),.36,f"KIN_CONDENSER_FAN_{x}")
    for x in (-.90,-.55,.55,.90):box(f"COIL_{x}",(x,-.625,.42),(.25,.02,.55),M["dark"],.003,False)
    tube("CHW_SUPPLY",(-1.42,.30,.38),(-1.20,.30,.38),.09,M["steel"]);tube("CHW_RETURN",(-1.42,-.30,.38),(-1.20,-.30,.38),.09,M["steel"])
def cooling_tower(M):
    base(M,2.35,1.85);box("COLD_WATER_BASIN",(0,0,.28),(2.18,1.72,.46),M["steel"],.04)
    # Wet-tower fill pack remains visible behind open intake louvers.
    box("FILL_MEDIA",(0,0,1.15),(1.82,1.34,1.40),M["dark"],.025,False)
    for y in (-.69,.69):
        for z in (.58,.76,.94,1.12,1.30,1.48):box(f"AIR_INTAKE_LOUVER_{y}_{z}",(0,y,z),(1.92,.055,.075),M["light"],.004,False,r=(.14*(1 if y>0 else -1),0,0))
    for x in (-.92,.92):box(f"CORNER_COLUMN_{x}",(x,0,1.18),(.13,1.52,1.62),M["steel"],.012)
    box("DRIFT_ELIMINATOR",(0,0,1.78),(1.90,1.45,.16),M["light"],.015);cyl("FAN_STACK",(0,0,2.05),.63,.34,M["steel"],"Z",64);fan(M,(0,0,2.30),.52,"KIN_TOWER_FAN");cyl("FAN_GUARD_OUTER",(0,0,2.34),.60,.035,M["dark"],"Z",64,False);box("FAN_GEAR_MOTOR",(0,0,2.48),(.22,.22,.18),M["dark"],.025)
    for a in range(0,360,30):tube(f"FAN_GUARD_RADIAL_{a}",(0,0,2.36),(.58*math.cos(math.radians(a)),.58*math.sin(math.radians(a)),2.36),.008,M["dark"],False)
    for z in (.48,.70,.92,1.14,1.36,1.58,1.80):box(f"ACCESS_LADDER_RUNG_{z}",(1.10,-.35,z),(.42,.045,.035),M["yellow"],.003,False)
    tube("ACCESS_LADDER_LEFT",(.90,-.35,.40),(.90,-.35,1.90),.025,M["yellow"],False);tube("ACCESS_LADDER_RIGHT",(1.30,-.35,.40),(1.30,-.35,1.90),.025,M["yellow"],False)
    tube("HOT_WATER_IN",(-1.35,0,1.65),(-.92,0,1.65),.11,M["copper"]);tube("SPRAY_HEADER",(-.72,0,1.68),(.72,0,1.68),.055,M["copper"],False);tube("COLD_WATER_OUT",(.98,0,.30),(1.35,0,.30),.12,M["steel"]);box("COOLING_TOWER_NAMEPLATE",(0,-.73,.30),(.72,.025,.18),M["white"],.008,False);text("COOLING_TOWER_LABEL","COOLING TOWER",(0,-.75,.30),.052,M["dark"])
def firetube_boiler(M):
    base(M,2.9,1.45);cyl("BOILER_SHELL",(0,0,1.08),.60,2.05,M["steel"],"X",64);sphere("FRONT_HEAD",(-1.03,0,1.08),.60,M["steel"],scale=(.42,1,1));sphere("REAR_HEAD",(1.03,0,1.08),.60,M["steel"],scale=(.42,1,1));cyl("BURNER",(-1.48,0,1.08),.30,.55,M["orange"],"X",48);motor(M,(-1.80,0,1.08),"X",.55,"BURNER_BLOWER")
    cyl("STACK",(.72,0,2.02),.20,1.10,M["dark"],"Z",48);tube("STEAM_OUT",(0,0,1.67),(0,0,2.08),.11,M["ss"]);gauge(M,(-.45,-.64,1.55));box("BOILER_CONTROL",(.45,-.72,.88),(.48,.26,.68),M["light"],.025);text("LABEL","FIRETUBE\nSTEAM BOILER",(.45,-.865,.91),.045,M["dark"])
def vacuum_pump(M):
    base(M,1.95,.90);motor(M,(-.55,0,.43),"X",.85,"KIN_MOTOR_SHAFT");cyl("DRIVE_SHAFT",(-.12,0,.43),.055,.30,M["ss"],"X",32);cyl("FLEXIBLE_COUPLING",(.01,0,.43),.11,.16,M["orange"],"X",32);box("COUPLING_GUARD",(0,0,.44),(.34,.34,.34),M["yellow"],.035,False)
    box("ROTARY_VANE_PUMP_BODY",(.47,0,.44),(.70,.58,.68),M["dark"],.055);cyl("PUMP_END_COVER",(.83,0,.44),.26,.06,M["steel"],"X",56);cyl("OIL_SIGHT_GLASS",(.47,-.31,.32),.065,.025,M["screen"],"Y",32,False);cyl("EXHAUST_MIST_FILTER",(.40,0,1.02),.15,.48,M["light"],"Z",48);tube("VACUUM_INLET",(.74,0,.78),(.98,0,.78),.10,M["ss"]);gauge(M,(.80,-.28,.70));text("VACUUM_LABEL","ROTARY VANE\nVACUUM PUMP",(.47,-.335,.51),.035,M["white"])
def regenerative_blower(M):
    base(M,1.85,.95);motor(M,(-.53,0,.46),"X",.82,"KIN_MOTOR_SHAFT");cyl("BLOWER_DRIVE_SHAFT",(-.12,0,.47),.050,.30,M["ss"],"X",32);box("COUPLING_GUARD",(0,0,.47),(.30,.30,.30),M["yellow"],.035,False);torus("SIDE_CHANNEL_CASING",(.43,0,.50),.34,.15,M["light"],"X",True);cyl("IMPELLER_HUB",(.43,-.01,.50),.10,.34,M["steel"],"X",40)
    for i in range(16):
        a=2*math.pi*i/16;box(f"ANNULAR_RIB_{i}",(.43,.39*math.cos(a),.50+.39*math.sin(a)),(.08,.055,.18),M["steel"],.006,False,r=(a,0,0))
    tube("BLOWER_INLET",(.30,0,.84),(.30,0,1.20),.10,M["ss"]);tube("BLOWER_OUTLET",(.68,0,.79),(.68,0,1.15),.10,M["ss"]);box("INLET_FILTER",(.30,0,1.34),(.30,.30,.24),M["dark"],.035);box("OUTLET_SILENCER",(.68,0,1.29),(.25,.25,.28),M["steel"],.035);text("BLOWER_LABEL","REGENERATIVE\nBLOWER",(.43,-.37,.22),.032,M["dark"])
def hydraulic_power_unit(M):
    base(M,2.1,1.15);box("RESERVOIR",(0,0,.48),(1.95,1.02,.78),M["steel"],.045);box("TOP_PLATE",(0,0,.91),(2.02,1.08,.09),M["dark"],.012);motor(M,(-.48,0,1.28),"Z",.85,"KIN_PUMP_MOTOR");cyl("PUMP_BELLHOUSING",(-.48,0,.98),.22,.22,M["light"],"Z",48);box("VALVE_MANIFOLD",(.35,0,1.18),(.58,.38,.42),M["light"],.025)
    for x in (.18,.35,.52):cyl(f"SOLENOID_{x}",(x,-.29,1.25),.07,.24,M["orange"],"Y",32)
    gauge(M,(.74,-.24,1.40));cyl("RETURN_FILTER",(.74,.24,1.30),.14,.55,M["dark"],"Z",48);box("LEVEL_GAUGE",(-.82,-.53,.48),(.10,.025,.48),M["screen"],.01,False)
def lubrication_skid(M):
    base(M,2.05,1.10)
    # Familiar automatic-lubricator architecture: clear grease reservoir on a
    # compact pump/controller, feeding a physically separate progressive divider.
    box("AUTOMATIC_PUMP_BASE",(-.48,0,.38),(.68,.66,.62),M["steel"],.050);box("PUMP_DISPLAY",(-.48,-.345,.43),(.34,.025,.15),M["screen"],.008,False);cyl("KIN_LUBE_PUMP",(-.48,0,.73),.16,.20,M["dark"],"Z",40)
    cyl("TRANSPARENT_GREASE_RESERVOIR",(-.48,0,1.20),.31,.78,M["tan"],"Z",64);cyl("GREASE_FOLLOWER_PLATE",(-.48,0,1.48),.285,.075,M["dark"],"Z",48);cyl("RESERVOIR_LID",(-.48,0,1.63),.33,.12,M["steel"],"Z",48)
    box("PUMP_NAMEPLATE",(-.48,-.35,.24),(.54,.025,.18),M["white"],.008,False);text("LUBE_LABEL","AUTOMATIC\nGREASE PUMP",(-.48,-.37,.24),.038,M["dark"])
    box("PROGRESSIVE_DIVIDER_BLOCK",(.45,0,.80),(.62,.38,.70),M["light"],.025);text("DIVIDER_LABEL","PROGRESSIVE\nDIVIDER",(.45,-.205,.80),.035,M["dark"])
    tube("PUMP_TO_DIVIDER",(-.14,-.18,.55),(.14,-.18,.62),.022,M["black"],False)
    for i,z in enumerate((.55,.68,.81,.94,1.07)):
        cyl(f"METERING_OUTLET_{i}",(.78,-.10,z),.028,.13,M["copper"],"X",24);tube(f"GREASE_HOSE_{i}",(.84,-.10,z),(1.10,-.30+(.15*(i%3)),.30+.14*i),.016,M["black"],False)
    gauge(M,(.20,-.36,.35))
def nitrogen_generator(M):
    base(M,2.75,1.15)
    for x in (-.48,.48):
        cyl(f"PSA_TOWER_{x}",(x,0,1.18),.31,1.72,M["light"],"Z",64);sphere(f"PSA_TOP_{x}",(x,0,2.04),.31,M["light"],scale=(1,1,.42));sphere(f"PSA_BOTTOM_{x}",(x,0,.32),.31,M["light"],scale=(1,1,.42));cyl(f"TOP_VALVE_{x}",(x,0,2.27),.075,.20,M["orange"],"Z",32)
    tube("PSA_HEADER",(-.48,0,2.35),(.48,0,2.35),.07,M["copper"]);box("OXYGEN_ANALYZER",(0,-.55,1.20),(.76,.24,.82),M["dark"],.025);box("PURITY_DISPLAY",(0,-.69,1.44),(.42,.02,.16),M["screen"],.005,False);text("PURITY_LABEL","O2 ANALYZER\nNITROGEN 99.9%",(0,-.705,1.15),.042,M["white"])
    for i,x in enumerate((-.42,-.14,.14,.42)):cyl(f"PSA_SWITCHING_VALVE_{i}",(x,-.24,.20),.065,.20,M["orange"],"Y",32)
    for i,x in enumerate((-.26,0,.26)):box(f"ANALYZER_FLOWTUBE_{i}",(x,-.69,.88),(.055,.025,.28),M["screen"],.008,False)
    # A horizontal green product receiver is deliberately unlike the two
    # vertical adsorption towers, removing the three-identical-vessel reading.
    cyl("NITROGEN_PRODUCT_RECEIVER",(1.06,0,.70),.30,.84,M["green"],"X",56);sphere("N2_RECEIVER_LEFT",(.64,0,.70),.30,M["green"],scale=(.38,1,1));sphere("N2_RECEIVER_RIGHT",(1.48,0,.70),.30,M["green"],scale=(.38,1,1));tube("N2_PRODUCT_LINE",(.48,0,2.35),(1.06,0,1.00),.045,M["copper"],False);box("N2_FLOWMETER",(.72,-.38,1.48),(.10,.08,.50),M["screen"],.010,False);tube("N2_OUTLET",(1.48,0,.70),(1.78,0,.70),.07,M["green"]);box("N2_RECEIVER_LABEL",(1.06,-.31,.70),(.58,.025,.30),M["white"],.008,False);text("N2_LABEL","N2 PRODUCT\nRECEIVER",(1.06,-.33,.70),.042,M["green"])
    box("GENERATOR_HEADER_SIGN",(0,-.50,1.92),(.76,.08,.28),M["green"],.015,False);text("GENERATOR_SIGN","PSA N2 GENERATOR",(0,-.555,1.92),.052,M["white"])
def plate_heat_exchanger(M):
    base(M,1.65,.90);box("FIXED_FRAME",(-.55,0,.88),(.16,.72,1.55),M["steel"],.025);box("MOVABLE_FRAME",(.55,0,.88),(.16,.72,1.55),M["steel"],.025)
    for i,x in enumerate([-.42+i*.07 for i in range(13)]):box(f"PLATE_{i}",(x,0,.88),(.025,.60,1.35),(M["ss"] if i%2 else M["light"]),.003)
    for y,z in ((-.25,.45),(.25,.45),(-.25,1.30),(.25,1.30)):cyl(f"PORT_{y}_{z}",(-.66,y,z),.10,.25,M["ss"],"X",40)
    for y in (-.30,.30):tube(f"TIE_BAR_{y}",(-.62,y,.22),(.62,y,.22),.035,M["dark"]);tube(f"TOP_BAR_{y}",(-.62,y,1.60),(.62,y,1.60),.035,M["dark"])
def condensate_return(M):
    base(M,2.55,1.30);box("VENTED_RECEIVER_TANK",(-.28,0,.60),(1.55,1.10,.96),M["steel"],.055);box("RECEIVER_TOP",(-.28,0,1.10),(1.62,1.16,.10),M["dark"],.015);tube("CONDENSATE_INLET",(-1.28,0,.92),(-1.05,0,.92),.10,M["copper"]);tube("ATMOSPHERIC_VENT",(-.68,0,1.12),(-.68,0,1.75),.08,M["ss"])
    # Side-mounted close-coupled pumps avoid transformer-like top bushings.
    for i,(x,y) in enumerate(((.72,-.25),(.72,.28))):
        motor(M,(x,y,.54),"X",.48,f"KIN_PUMP_{0.62 if i==0 else 0.98}");torus(f"CENTRIFUGAL_VOLUTE_{i}",(1.02,y,.54),.18,.075,M["steel"],"X",True);cyl(f"PUMP_HUB_{i}",(1.02,y,.54),.065,.22,M["dark"],"X",32);tube(f"PUMP_SUCTION_{i}",(.50,y,.22),(.88,y,.43),.055,M["copper"]);tube(f"PUMP_DISCHARGE_{i}",(1.02,y,.72),(1.02,-.52,.72+.10*i),.055,M["copper"])
    tube("COMMON_DISCHARGE",(.72,-.52,.82),(1.38,-.52,.82),.075,M["copper"]);cyl("FLOAT_LEVEL_SWITCH",(-.86,-.34,1.30),.08,.42,M["orange"],"Z",32);box("LEVEL_SIGHT_GLASS",(-1.08,-.58,.62),(.10,.025,.60),M["screen"],.010,False);box("LEVEL_CONTROL",(-.28,-.59,.72),(.42,.18,.56),M["light"],.025);box("RETURN_NAMEPLATE",(-.28,-.695,.44),(.78,.025,.24),M["white"],.008,False);text("RETURN_LABEL","STEAM CONDENSATE\nRETURN UNIT",(-.28,-.715,.44),.039,M["dark"])

BUILDERS={"rotary_screw_air_compressor":screw_compressor,"vertical_air_receiver":air_receiver,"refrigerated_air_dryer":refrigerated_dryer,"twin_tower_desiccant_dryer":desiccant_dryer,"air_cooled_water_chiller":air_cooled_chiller,"induced_draft_cooling_tower":cooling_tower,"horizontal_firetube_boiler":firetube_boiler,"rotary_vane_vacuum_pump":vacuum_pump,"regenerative_blower":regenerative_blower,"hydraulic_power_unit":hydraulic_power_unit,"central_lubrication_skid":lubrication_skid,"psa_nitrogen_generator":nitrogen_generator,"plate_frame_heat_exchanger":plate_heat_exchanger,"condensate_return_unit":condensate_return}
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
    floor=box("REVIEW_floor",(0,0,mins.z-.035),(max(4,vmax.x-vmin.x+1.5),max(4,vmax.y-vmin.y+1.5),.05),M["light"],.002,False);floor["rungproof_asset"]=False;world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.022,.030,.034);center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.18,2.4)
    for i,(loc,e,s) in enumerate((((4,-4,6),1200,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    for i,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*.46);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    # The front three-quarter view exposes process connections and operator
    # features; retain it as both the catalog thumbnail and blind-review proof.
    primary_review=(root/"review"/f"{slug}_03.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)
flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("UTILITY_ASSETS_BUILT",len(BUILT))
