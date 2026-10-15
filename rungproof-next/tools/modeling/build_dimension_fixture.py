"""Original static dimension fixture for the manual box-validity exercise.

Run with Blender and RUNGPROOF_PROJECT_ROOT. Sensor heads illustrate placement;
there is no distance acquisition, calibration, or numeric dimension algorithm.
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
spec = importlib.util.spec_from_file_location("fixture_exports", ROOT / "tools/modeling/build_material_flow_assets.py")
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
flow.BASE = ROOT / "assets/training_accessories"
bpy.context.preferences.filepaths.save_version = 0


def fixture(m):
    # Blender Z is height. The carrying surface ends exactly at Z=0.900.
    flow.box("DIMENSION_tabletop", (0, 0, .875), (1.60, 1.30, .05), m["zinc"])
    for x in (-.65, .65):
        for y in (-.50, .50):
            flow.box(f"DIMENSION_table_foot_{x}_{y}", (x, y, .04), (.23, .23, .08), m["zinc"])
            flow.box(f"DIMENSION_table_leg_{x}_{y}", (x, y, .465), (.085, .085, .81), m["blue"])
    for x in (-.98, .98):
        flow.box(f"DIMENSION_portal_foot_{x}", (x, 0, .04), (.30, .40, .08), m["zinc"])
        flow.box(f"DIMENSION_portal_post_{x}", (x, 0, 1.10), (.08, .08, 2.08), m["blue"])
    flow.box("DIMENSION_crossbeam", (0, 0, 2.12), (2.04, .09, .09), m["blue"])
    flow.box("DIMENSION_rear_foot", (0, .85, .04), (.30, .30, .08), m["zinc"])
    flow.box("DIMENSION_rear_post", (0, .85, .71), (.08, .08, 1.30), m["blue"])
    flow.box("DIMENSION_head_X", (-.875, 0, 1.28), (.14, .12, .18), m["black"])
    flow.cyl("DIMENSION_lens_X", (-.798, 0, 1.28), .035, .016, m["red"], "X", 32, False)
    flow.box("DIMENSION_head_Z", (0, .75, 1.28), (.14, .14, .18), m["black"])
    flow.cyl("DIMENSION_lens_Z", (0, .675, 1.28), .035, .016, m["red"], "Y", 32, False)
    flow.box("DIMENSION_head_Y", (0, 0, 2.01), (.16, .14, .14), m["black"])
    flow.cyl("DIMENSION_lens_Y", (0, 0, 1.935), .035, .016, m["red"], "Z", 32, False)
    flow.box("DIMENSION_scope_plate", (0, -.658, .81), (.95, .025, .18), m["white"], collision=False)
    flow.text("DIMENSION_static_scope", "DIMENSION FIXTURE\nMANUAL VALIDITY", (0, -.675, .81), .055,
              m["black"], (math.pi / 2, 0, 0))


slug = "dimension_sensors"
review = flow.BASE / slug / "review"
archive = review / "historical_invalid_identity_20261005"
archive.mkdir(parents=True, exist_ok=True)
for file in review.iterdir():
    if file.is_file() and file.name not in (".gdignore", "repair_scope.json") and not file.name.startswith(slug + "_"):
        if not (archive / file.name).exists():
            file.rename(archive / file.name)
flow.save(slug, fixture)
graph = bpy.context.evaluated_depsgraph_get()
points = []
for obj in bpy.context.scene.objects:
    if obj.type == "MESH" and obj.get("rungproof_asset"):
        evaluated = obj.evaluated_get(graph)
        points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
if abs(min(p.z for p in points)) >= .001:
    raise ValueError("Dimension fixture has an incorrect floor datum")
size = [max(p[a] for p in points) - min(p[a] for p in points) for a in range(3)]
catalog_path = ROOT / "assets/catalog/candidates.catalog.json"
catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
asset_id = "training.accessory.dimension_sensors.v1"
asset = next(a for a in catalog["assets"] if a["id"] == asset_id)
asset.pop("genericBasisAssetId", None)
asset["bounds"] = dict(widthM=size[0], heightM=size[2], depthM=size[1])
asset["kinematics"] = []
asset["animationTags"] = []
asset["quality"] = dict(status="candidate", blindReviewId=None, recognitionConfidence=None,
                        topologyReviewed=False, materialReviewed=False, scaleReviewed=False, animationReviewed=False)
catalog_path.write_text(json.dumps(catalog, indent=2) + "\n", encoding="utf-8")
register_path = ROOT / "assets/catalog/industrial-reference-register.json"
register = json.loads(register_path.read_text(encoding="utf-8"))
register["entries"] = [e for e in register["entries"] if e["assetId"] != asset_id]
register_path.write_text(json.dumps(register, indent=2) + "\n", encoding="utf-8")
scope = "Original static bench with three orthogonal sensor heads. Manual validity inputs only; no numeric dimension acquisition or calibration."
(review / "repair_scope.json").write_text(json.dumps({"assetId": asset_id, "status": "candidate-unapproved",
    "scope": scope, "historicalEvidence": "historical_invalid_identity_20261005 is invalid for this replacement."}, indent=2) + "\n", encoding="utf-8")
help_spec = importlib.util.spec_from_file_location("fixture_help", ROOT / "tools/generate_help_documents.py")
help_module = importlib.util.module_from_spec(help_spec)
help_spec.loader.exec_module(help_module)
text = help_module.asset_help(asset, "candidate", {})
text += f"\n## Replacement scope - 2026-10-05\n\n{scope} Historical shutter evidence is invalid; independent approval remains pending.\n"
(ROOT / "docs/help/assets" / f"{asset_id}.md").write_text(text, encoding="utf-8")
print("DIMENSION_FIXTURE_BUILT static manual-validity scope; independent approval pending")
