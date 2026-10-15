"""Build original passive readouts; no numeric behavior or OEM identity claim.

Run in Blender with RUNGPROOF_PROJECT_ROOT. RUNGPROOF_READOUT_FILTER optionally
selects comma-separated slugs. The default repairs the six remaining mismatches.
"""
from __future__ import annotations
import importlib.util
import json
import math
import os
from pathlib import Path
import bpy
from mathutils import Vector

SPECS = {
    "cut_length_display": ("LENGTH_DISPLAY", "LENGTH", "NO MEASUREMENT"),
    "numeric_measurement_display": ("STATIC_READOUT", "MEASUREMENT", "NO LIVE VALUE"),
    "numeric_result_display": ("STATIC_READOUT", "RESULT", "NO LIVE VALUE"),
    "numeric_selector_display": ("STATIC_READOUT", "FUNCTION", "NO LIVE VALUE"),
    "occupancy_counter_display": ("STATIC_READOUT", "COUNT", "NO LIVE VALUE"),
    "progress_display": ("STATIC_READOUT", "PROGRESS", "NO LIVE VALUE"),
    "weight_display": ("STATIC_READOUT", "WEIGHT", "NO LIVE VALUE"),
}


def build_readouts(root: Path, slugs: list[str]) -> None:
    if not slugs or any(slug not in SPECS for slug in slugs):
        raise ValueError(f"Unknown or empty readout selection: {slugs}")
    root = root.resolve()
    os.environ["RUNGPROOF_ASSET_FILTER"] = "__helpers_only__"
    spec = importlib.util.spec_from_file_location("readout_exports", root / "tools/modeling/build_material_flow_assets.py")
    flow = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(flow)
    flow.BASE = root / "assets/training_accessories"
    bpy.context.preferences.filepaths.save_version = 0
    help_spec = importlib.util.spec_from_file_location("readout_help", root / "tools/generate_help_documents.py")
    help_module = importlib.util.module_from_spec(help_spec)
    assert help_spec.loader is not None
    help_spec.loader.exec_module(help_module)
    catalog_path = root / "assets/catalog/candidates.catalog.json"
    register_path = root / "assets/catalog/industrial-reference-register.json"
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    register = json.loads(register_path.read_text(encoding="utf-8"))
    for slug in slugs:
        prefix, title, boundary = SPECS[slug]

        def display(materials):
            # Blender Z is height. The mast seats in both base and housing.
            flow.box(f"{prefix}_base", (0, 0, .04), (.85, .70, .08), materials["zinc"])
            flow.box(f"{prefix}_mast", (0, .14, .69), (.085, .085, 1.22), materials["blue"])
            flow.box(f"{prefix}_housing", (0, .06, 1.51), (.82, .24, .48), materials["steel"])
            flow.box(f"{prefix}_screen", (0, -.069, 1.51), (.70, .018, .34), materials["black"], collision=False)
            flow.text(f"{prefix}_static_legend", f"{title}\n{boundary}", (0, -.084, 1.51), .055,
                      materials["white"], (math.pi / 2, 0, 0))

        review = flow.BASE / slug / "review"
        archive = review / "historical_invalid_identity_20261005"
        archive.mkdir(parents=True, exist_ok=True)
        for file in review.iterdir():
            if file.is_file() and file.name not in (".gdignore", "repair_scope.json") and not file.name.startswith(slug + "_"):
                if not (archive / file.name).exists():
                    file.rename(archive / file.name)
        flow.save(slug, display)
        graph = bpy.context.evaluated_depsgraph_get()
        points = []
        for obj in bpy.context.scene.objects:
            if obj.type == "MESH" and obj.get("rungproof_asset"):
                evaluated = obj.evaluated_get(graph)
                points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
        if abs(min(p.z for p in points)) >= .001:
            raise ValueError(f"{slug}: wrong floor datum")
        size = [max(p[a] for p in points) - min(p[a] for p in points) for a in range(3)]
        asset_id = f"training.accessory.{slug}.v1"
        asset = next(a for a in catalog["assets"] if a["id"] == asset_id)
        asset.pop("genericBasisAssetId", None)
        asset["bounds"] = dict(widthM=size[0], heightM=size[2], depthM=size[1])
        asset["kinematics"] = []
        asset["animationTags"] = []
        asset["quality"] = dict(status="candidate", blindReviewId=None, recognitionConfidence=None,
                                topologyReviewed=False, materialReviewed=False,
                                scaleReviewed=False, animationReviewed=False)
        register["entries"] = [entry for entry in register["entries"] if entry["assetId"] != asset_id]
        scope = f"Original static readout displaying {title} / {boundary}; no numeric point or measurement behavior is bound."
        (review / "repair_scope.json").write_text(json.dumps({
            "assetId": asset_id, "status": "candidate-unapproved", "scope": scope,
            "historicalEvidence": "historical_invalid_identity_20261005 is invalid for this replacement.",
        }, indent=2) + "\n", encoding="utf-8")
        help_text = help_module.asset_help(asset, "candidate", {})
        help_text += (f"\n## Replacement scope - 2026-10-05\n\n{scope} "
                      "Historical substitute-model evidence is invalid for the replacement. Independent approval remains pending.\n")
        (root / "docs/help/assets" / f"{asset_id}.md").write_text(help_text, encoding="utf-8")
        print(f"STATIC_READOUT_BUILT {slug} no live numeric behavior; independent approval pending")
    catalog_path.write_text(json.dumps(catalog, indent=2) + "\n", encoding="utf-8")
    register_path.write_text(json.dumps(register, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    selection = os.environ.get("RUNGPROOF_READOUT_FILTER", "")
    # Preserve the repaired cut-length package unless explicitly selected.
    slugs = [v.strip() for v in selection.split(",") if v.strip()] if selection else [slug for slug in SPECS if slug != "cut_length_display"]
    build_readouts(Path(os.environ["RUNGPROOF_PROJECT_ROOT"]), slugs)
