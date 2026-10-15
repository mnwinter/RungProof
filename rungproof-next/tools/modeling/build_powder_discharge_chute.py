"""Replace the incorrectly copied door with a static, open gravity-chute model.

This is original simulator geometry, not a powder-flow or fabrication design.
Run with the project's Blender binary and RUNGPROOF_PROJECT_ROOT set.
"""
from __future__ import annotations

import importlib.util
import json
import math
import os
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
# Reuse the existing material/export/review pipeline without rebuilding its
# other asset families. The imported file only builds matching filter entries.
os.environ["RUNGPROOF_ASSET_FILTER"] = "__helpers_only__"
spec = importlib.util.spec_from_file_location(
    "material_flow_helpers", ROOT / "tools/modeling/build_material_flow_assets.py"
)
flow = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(flow)
flow.BASE = ROOT / "assets/training_accessories"
bpy.context.preferences.filepaths.save_version = 0


def sheet(name, vertices, material):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], [tuple(range(len(vertices)))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    obj["rungproof_asset"] = True
    obj["rungproof_collision"] = True
    solid = obj.modifiers.new("Sheet thickness", "SOLIDIFY")
    solid.thickness = 0.006
    bevel = obj.modifiers.new("Sheet edge radius", "BEVEL")
    bevel.width = 0.002
    bevel.segments = 2
    return obj


def chute(materials):
    # Blender Z is height. The clear channel slopes along X, with an open
    # inlet/outlet and two raised sidewalls. No door curtain or motor remains.
    inlet_x, outlet_x = -0.8, 0.8
    inlet_z, outlet_z = 1.5, 0.55
    half_width, wall_height = 0.32, 0.24
    sheet("CHUTE_bottom", [
        (inlet_x, -half_width, inlet_z), (outlet_x, -half_width, outlet_z),
        (outlet_x, half_width, outlet_z), (inlet_x, half_width, inlet_z),
    ], materials["alum"])
    for side in (-1, 1):
        y = side * half_width
        sheet(f"CHUTE_side_{side}", [
            (inlet_x, y, inlet_z), (outlet_x, y, outlet_z),
            (outlet_x, y, outlet_z + wall_height),
            (inlet_x, y, inlet_z + wall_height),
        ], materials["alum"])
        flow.tube(f"CHUTE_upper_lip_{side}",
                  (inlet_x, y, inlet_z + wall_height),
                  (outlet_x, y, outlet_z + wall_height), 0.014, materials["zinc"])
    # Two cross-supports meet the underside; legs terminate at floor feet.
    slope_angle = math.atan2(inlet_z - outlet_z, outlet_x - inlet_x)
    normal_x, normal_z = math.sin(slope_angle), math.cos(slope_angle)
    for x in (-0.55, 0.55):
        bottom_z = inlet_z + (x - inlet_x) * (outlet_z - inlet_z) / (outlet_x - inlet_x)
        # A horizontal support corner pierced the inclined channel. Align
        # the support with the sheet and offset it below the sheet underside.
        beam_x = x - normal_x * 0.046
        beam_z = bottom_z - normal_z * 0.046
        flow.box(f"CHUTE_crossbeam_{x}", (beam_x, 0, beam_z),
                 (0.085, 0.78, 0.08), materials["steel"],
                 rotation=(0, slope_angle, 0))
        for y in (-0.35, 0.35):
            leg_top = beam_z - 0.04 / normal_z
            flow.box(f"CHUTE_leg_{x}_{y}", (beam_x, y, (leg_top + 0.08) / 2),
                     (0.065, 0.065, leg_top - 0.08), materials["blue"])
            flow.box(f"CHUTE_foot_{x}_{y}", (beam_x, y, 0.04),
                     (0.22, 0.20, 0.08), materials["zinc"])
    for y in (-0.35, 0.35):
        flow.tube(f"CHUTE_frame_brace_{y}", (-0.55, y, 0.22),
                  (0.55, y, 0.53), 0.022, materials["steel"])
    flow.box("CHUTE_nameplate", (-0.30, -0.337, 1.34),
             (0.43, 0.016, 0.13), materials["steel"], 0.006, False)
    flow.text("CHUTE_label", "DISCHARGE", (-0.30, -0.349, 1.34),
              0.06, materials["white"], (math.pi / 2, 0, 0))
    bpy.context.view_layer.update()
    normal = Vector((normal_x, 0, normal_z))
    inlet = Vector((inlet_x, 0, inlet_z))
    graph = bpy.context.evaluated_depsgraph_get()
    for obj in bpy.context.scene.objects:
        if obj.name.startswith("CHUTE_crossbeam_"):
            evaluated = obj.evaluated_get(graph)
            clearance = max(normal.dot(evaluated.matrix_world @ Vector(corner) - inlet)
                            for corner in evaluated.bound_box)
            assert clearance <= -0.005, f"{obj.name} pierces the channel: {clearance}"
    print("CHUTE_CHANNEL_SUPPORT_CLEARANCE_PASS")


flow.save("powder_discharge_chute", chute)
# Reset identity/evidence for this changed model. A successful door review
# cannot approve a chute, and static geometry has no animated node contract.
points = []
graph = bpy.context.evaluated_depsgraph_get()
for obj in bpy.context.scene.objects:
    if obj.type == "MESH" and obj.get("rungproof_asset"):
        evaluated = obj.evaluated_get(graph)
        points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
dimensions = [max(point[axis] for point in points) - min(point[axis] for point in points)
              for axis in range(3)]
catalog_path = ROOT / "assets/catalog/candidates.catalog.json"
catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
asset = next(asset for asset in catalog["assets"]
             if asset["id"] == "training.accessory.powder_discharge_chute.v1")
asset.pop("genericBasisAssetId", None)
asset["bounds"] = {"widthM": dimensions[0], "heightM": dimensions[2], "depthM": dimensions[1]}
asset["kinematics"] = []
asset["animationTags"] = []
asset["quality"] = {
    "status": "candidate", "blindReviewId": None, "recognitionConfidence": None,
    "topologyReviewed": False, "materialReviewed": False,
    "scaleReviewed": False, "animationReviewed": False,
}
catalog_path.write_text(json.dumps(catalog, indent=2) + "\n", encoding="utf-8")
register_path = ROOT / "assets/catalog/industrial-reference-register.json"
register = json.loads(register_path.read_text(encoding="utf-8"))
register["entries"] = [entry for entry in register["entries"] if entry["assetId"] != asset["id"]]
register_path.write_text(json.dumps(register, indent=2) + "\n", encoding="utf-8")
print("POWDER_CHUTE_BUILT static open channel; independent approval pending")
