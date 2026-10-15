"""Register reusable scene loads and workpieces as candidate assets."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
SPECS = {
    "corrugated_shipping_carton": ("loads.carton.corrugated-rsc.v1", "Corrugated Shipping Carton - Regular Slotted", "loads/cartons", [0.85, 0.72, 0.74], ["corrugated shipping carton", "parcel box"]),
    "hdpe_jerry_can": ("loads.container.jerry-can-hdpe.v1", "Handled HDPE Jerry Can", "loads/containers", [0.64, 1.22, 0.44], ["hdpe jerry can", "coolant jug"]),
    "reusable_process_bottle": ("loads.container.process-bottle.v1", "Reusable Capped Process Bottle", "loads/containers", [0.54, 1.20, 0.54], ["reusable process bottle", "capped bottle"]),
    "rectangular_machining_blank": ("tooling.workholding.machine-vise-stock.v1", "Machine Vise with Rectangular 4140 Stock", "tooling/workholding", [1.18, 0.61, 0.80], ["machine vise", "rectangular machining stock", "workholding"]),
    "modular_assembly_fixture": ("tooling.fixture.modular-two-clamp.v1", "Two-Clamp Modular Assembly Fixture", "tooling/fixtures", [1.44, 0.62, 0.96], ["modular assembly fixture", "toggle clamp fixture"]),
    "clamped_plate_workholding": ("tooling.fixture.clamped-plate.v1", "Clamped Plate Workholding Fixture", "tooling/fixtures", [1.48, 0.36, 0.92], ["clamped plate fixture", "workholding plate"]),
}


def main():
    document = json.loads(CATALOG.read_text(encoding="utf-8"))
    by_id = {item["id"]: item for item in document["assets"]}
    # Do not retain models that failed context-free recognition review.
    by_id.pop("loads.workpiece.machining-blank-rectangular.v1", None)
    for slug, (asset_id, name, category, bounds, tags) in SPECS.items():
        existing_quality = by_id.get(asset_id, {}).get("quality")
        by_id[asset_id] = {
            "id": asset_id,
            "displayName": name,
            "category": category,
            "tags": tags,
            "model": {
                "sourceBlend": f"res://assets/scene_loads/{slug}/source/{slug}.blend",
                "deliveryGltf": f"res://assets/scene_loads/{slug}/delivery/{slug}.glb",
                "lodFiles": [],
                "collisionFile": f"res://assets/scene_loads/{slug}/collision/{slug}_collision.glb",
                "thumbnailFile": f"res://assets/scene_loads/{slug}/thumbnail.png",
            },
            "bounds": {"widthM": bounds[0], "heightM": bounds[1], "depthM": bounds[2]},
            "connectors": [], "kinematics": [], "signals": [],
            "quality": existing_quality or {"status": "candidate", "blindReviewId": None, "recognitionConfidence": None,
                "topologyReviewed": False, "materialReviewed": False, "scaleReviewed": True, "animationReviewed": False},
        }
    document["assets"] = list(by_id.values())
    CATALOG.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print("REGISTERED_CANDIDATES", len(document["assets"]))


if __name__ == "__main__":
    main()
