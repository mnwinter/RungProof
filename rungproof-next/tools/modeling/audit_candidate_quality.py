"""Audit candidate Blender sources without changing catalog acceptance flags."""
from __future__ import annotations

import json
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
OUTPUT = ROOT / "build" / "candidate-quality-audit.json"


def audit(asset: dict) -> dict:
    source = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
    bpy.ops.wm.open_mainfile(filepath=str(source))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    asset_meshes = [obj for obj in meshes if obj.get("rungproof_asset")] or meshes
    topology_ok = bool(asset_meshes) and all(
        len(obj.data.vertices) > 0
        and len(obj.data.polygons) > 0
        and all(max(0.0, d) > 1e-6 for d in obj.dimensions)
        for obj in asset_meshes
    )
    material_ok = bool(asset_meshes) and all(
        len(obj.data.materials) > 0
        and all(0 <= face.material_index < len(obj.data.materials) for face in obj.data.polygons)
        for obj in asset_meshes
    )
    kinematic_nodes = []
    for axis in asset.get("kinematics", []):
        node = axis.get("nodePath", "").rsplit("/", 1)[-1]
        found = bpy.data.objects.get(node) is not None
        kinematic_nodes.append({"node": node, "found": found})
    source_review = source.parent.parent / "review"
    animation_ok = all(
        (source_review / name).is_file()
        for name in (("state_stopped.png", "state_running.png") if kinematic_nodes else ())
    )
    if not kinematic_nodes:
        animation_ok = True
    return {
        "assetId": asset["id"],
        "source": str(source.relative_to(ROOT)),
        "meshCount": len(asset_meshes),
        "topologyOk": topology_ok,
        "materialOk": material_ok,
        "kinematicNodes": kinematic_nodes,
        "animationEvidenceOk": animation_ok,
    }


catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
results = [audit(asset) for asset in catalog["assets"]]
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
OUTPUT.write_text(json.dumps({"version": 1, "assets": results}, indent=2) + "\n", encoding="utf-8")
print(
    "CANDIDATE_QUALITY_AUDIT",
    len(results),
    "topology_ok=", sum(x["topologyOk"] for x in results),
    "material_ok=", sum(x["materialOk"] for x in results),
    "animation_evidence_ok=", sum(x["animationEvidenceOk"] for x in results),
)
