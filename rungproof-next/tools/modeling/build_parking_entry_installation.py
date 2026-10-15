"""Original, scene-scoped parking training models. No manufacturer dimensions.

Blender Z is height; +Y becomes Godot -Z (the vehicle's forward direction).
The solid pad and marked bays support the complete prescribed vehicle route.
"""
import os, math, importlib.util
from pathlib import Path
import bpy

ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
os.environ["RUNGPROOF_ASSET_FILTER"] = "__helpers_only__"
spec = importlib.util.spec_from_file_location("flow", Path(__file__).with_name("build_material_flow_assets.py"))
flow = importlib.util.module_from_spec(spec); spec.loader.exec_module(flow)
BASE = ROOT / "assets/scene_installations/parking_entry"

def vehicle(M):
    body = flow.mat("Parking vehicle blue", (.025,.30,.58), .35,.27)
    glass = flow.mat("Opaque training glazing", (.045,.12,.17), .45,.2)
    flow.box("VEHICLE_chassis", (0,0,.42), (1.56,3.65,.22), M["black"], .07)
    flow.box("VEHICLE_body", (0,0,.68), (1.62,3.8,.48), body, .12)
    flow.box("VEHICLE_cabin", (0,-.15,1.16), (1.44,1.94,.65), glass, .13)
    flow.box("VEHICLE_roof", (0,-.15,1.5), (1.46,1.84,.08), body, .035)
    for side in (-1,1):
        for y in (-1.25,1.25):
            flow.cyl(f"WHEEL_{side}_{y}", (side*.79,y,.30), .30,.22,M["black"],"X")
            flow.cyl(f"HUB_{side}_{y}", (side*.915,y,.30), .17,.04,M["alum"],"X")
        for y,m in ((1.91,M["white"]),(-1.91,M["red"])):
            flow.box(f"LIGHT_{side}_{y}",(side*.55,y,.71),(.28,.035,.14),m,.015)
    flow.box("FRONT_bumper",(0,1.93,.44),(1.60,.06,.12),M["black"],.018)
    flow.box("REAR_bumper",(0,-1.93,.44),(1.60,.06,.12),M["black"],.018)

def cabinet(M):
    flow.box("CABINET_foot",(-.30,0,.04),(.70,.65,.08),M["steel"],.015)
    flow.box("CABINET_housing",(-.30,0,.55),(.55,.50,1.02),M["yellow"],.04)
    flow.box("CABINET_service_door",(-.30,-.257,.55),(.43,.025,.80),M["white"],.01)
    flow.cyl("HINGE_shaft",(0,0,1.08),.10,.64,M["steel"],"Y")
    flow.text("CABINET_label","BARRIER",(-.3,-.277,.66),.075,M["black"],(math.pi/2,0,0))

def boom(M):
    # Hinge datum is the asset origin, not its minimum bounding box corner.
    flow.box("BOOM_arm",(1.8,0,0),(3.6,.12,.15),M["white"],.014)
    for i in range(6):
        flow.box(f"BOOM_red_band_{i}",(.30+i*.57,-.062,0),(.22,.006,.145),M["red"],.002)
        flow.box(f"BOOM_rear_band_{i}",(.30+i*.57,.062,0),(.22,.006,.145),M["red"],.002)

def pad(M):
    asphalt=flow.mat("Parking pad",(.14,.17,.19),0,.93)
    flow.box("ROAD_supported_slab",(0,-.5,.02),(14,21,.04),asphalt,.006)
    # Localized parking-space markings, not a repeating background pattern.
    for side in (-1,1):
        x=side*3.5
        for y in (5.85,8.15): flow.box(f"BAY_{side}_edge_{y}",(x,y,.044),(4.4,.065,.008),M["white"],.001,False)
        flow.box(f"BAY_{side}_end",(side*5.7,7,.044),(.065,2.3,.008),M["white"],.001,False)
    flow.box("ENTRY_stop_mark",(0,-2.25,.044),(3.0,.13,.008),M["white"],.001,False)

for slug,builder in (("vehicle",vehicle),("cabinet",cabinet),("boom",boom),("pad",pad)):
    flow.clean(); builder(flow.common()); root=BASE/slug
    (root/"source").mkdir(parents=True,exist_ok=True); (root/"delivery").mkdir(exist_ok=True)
    (root/"source/.gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"parking_{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
print("PARKING_INSTALLATION_BUILT four source and delivery assets")
