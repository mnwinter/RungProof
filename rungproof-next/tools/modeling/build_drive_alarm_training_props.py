"""Original static training props; no fieldbus, string parser or OEM approval.

Run with Blender and RUNGPROOF_PROJECT_ROOT. Only these three packages change.
The VFD uses the existing electrical-family shape, with a neutral DEMO display.
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
os.environ["RUNGPROOF_ASSET_FILTER"] = "__helpers_only__"


def helpers(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / "tools/modeling" / file)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


flow = helpers("drive_prop_exports", "build_material_flow_assets.py")
electrical = helpers("drive_prop_shapes", "build_electrical_controls_assets.py")
flow.BASE = ROOT / "assets/training_accessories"
bpy.context.preferences.filepaths.save_version = 0


def stand(materials, width, panel_bottom, panel_top, rear_y):
    # Blender Z is height; feet start at zero. These are illustrative supports.
    # The rear mast must land inside the foot, including the deeper VFD mount.
    flow.box("STAND_base", (0, rear_y / 2, .04), (width, .85 + rear_y, .08), materials["steel"])
    flow.box("STAND_mast", (0, rear_y, (.08 + panel_top) / 2),
             (.09, .09, panel_top - .08), materials["blue"])
    flow.box("STAND_backboard", (0, rear_y - .035, (panel_bottom + panel_top) / 2),
             (width - .1, .06, panel_top - panel_bottom), materials["steel"])


def vfd(materials):
    # Use the actual drive-family geometry rather than its unrelated wall copy.
    drive_materials = electrical.common()
    electrical.vfd(drive_materials)
    for obj in list(bpy.context.scene.objects):
        if obj.name == "DISPLAY_TEXT":
            bpy.data.objects.remove(obj, do_unlink=True)
        elif obj.get("rungproof_asset"):
            obj.location.z += .75
            if obj.name.startswith("KEY_"):
                # The family builder placed these behind the keypad face.
                obj.location.y -= .075
    electrical.text("DISPLAY_DEMO", "DEMO", (0, -.304, 1.68),
                    .045, drive_materials["white"])
    stand(materials, 1.15, .72, 2.05, .54)
    # Two spacer rails bridge the rear heatsink to its backboard.
    for z in (.84, 1.88):
        flow.box(f"VFD_mount_rail_{z}", (0, .50, z), (.70, .045, .06), materials["zinc"])


def alarm_display(materials):
    stand(materials, 1.55, 1.08, 1.87, .17)
    flow.box("ALARM_DISPLAY_enclosure", (0, 0, 1.47), (1.30, .26, .68), materials["steel"])
    flow.box("ALARM_DISPLAY_screen", (0, -.137, 1.49),
             (1.10, .014, .45), materials["black"], collision=False)
    flow.text("ALARM_DISPLAY_static_legend", "ALARM TEXT\nSYMBOLIC INPUTS", (0, -.151, 1.49),
              .090, materials["white"], (math.pi / 2, 0, 0))
    flow.text("ALARM_DISPLAY_model_label", "TRAINING DISPLAY", (0, -.15, 1.20),
              .045, materials["white"], (math.pi / 2, 0, 0))


def status_indicator(materials):
    stand(materials, .8, 1.03, 1.70, .15)
    flow.box("STATUS_indicator_housing", (0, 0, 1.37), (.55, .21, .62), materials["steel"])
    flow.cyl("LENS_red", (0, -.126, 1.48), .105, .04, materials["red"], "Y", 48, False)
    flow.text("STATUS_indicator_label", "DRIVE ALARM", (0, -.134, 1.21),
              .055, materials["white"], (math.pi / 2, 0, 0))


catalog_path = ROOT / "assets/catalog/candidates.catalog.json"
catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
register_path = ROOT / "assets/catalog/industrial-reference-register.json"
register = json.loads(register_path.read_text(encoding="utf-8"))
for slug, builder in (("vfd_diagnostic_panel", vfd),
                      ("fieldbus_alarm_string_display", alarm_display),
                      ("drive_status_indicator", status_indicator)):
    flow.save(slug, builder)
    graph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH" and obj.get("rungproof_asset"):
            evaluated = obj.evaluated_get(graph)
            points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    dimensions = [max(p[a] for p in points) - min(p[a] for p in points) for a in range(3)]
    assert abs(min(p.z for p in points)) < .001, f"{slug}: feet not grounded"
    asset_id = f"training.accessory.{slug}.v1"
    asset = next(a for a in catalog["assets"] if a["id"] == asset_id)
    asset.pop("genericBasisAssetId", None)
    asset["bounds"] = dict(widthM=dimensions[0], heightM=dimensions[2], depthM=dimensions[1])
    asset["kinematics"] = []
    asset["animationTags"] = []
    asset["quality"] = dict(status="candidate", blindReviewId=None, recognitionConfidence=None,
                            topologyReviewed=False, materialReviewed=False,
                            scaleReviewed=False, animationReviewed=False)
    register["entries"] = [e for e in register["entries"] if e["assetId"] != asset_id]
catalog_path.write_text(json.dumps(catalog, indent=2) + "\n", encoding="utf-8")
register_path.write_text(json.dumps(register, indent=2) + "\n", encoding="utf-8")
print("DRIVE_ALARM_PROPS_BUILT grounded original static props; independent approval pending")
