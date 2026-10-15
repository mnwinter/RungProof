"""Register the palletized case load as a candidate catalog asset."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
ASSET_ID = "loads.palletized-cases.gma-48x40.v1"


def main():
    document = json.loads(CATALOG.read_text(encoding="utf-8"))
    by_id = {item["id"]: item for item in document["assets"]}
    by_id[ASSET_ID] = {
        "id": ASSET_ID,
        "displayName": "Palletized Corrugated Case Load - GMA 48 x 40 in",
        "category": "loads/palletized-goods",
        "tags": ["loaded shipping pallet", "palletized case load", "cartons on pallet"],
        "model": {
            "sourceBlend": "res://assets/loads/palletized_case_load/source/palletized_case_load.blend",
            "deliveryGltf": "res://assets/loads/palletized_case_load/delivery/palletized_case_load.glb",
            "lodFiles": [],
            "collisionFile": "res://assets/loads/palletized_case_load/collision/palletized_case_load_collision.glb",
            "thumbnailFile": "res://assets/loads/palletized_case_load/thumbnail.png",
        },
        "bounds": {"widthM": 1.219, "heightM": 0.97, "depthM": 1.06},
        "connectors": [],
        "kinematics": [],
        "signals": [],
        "quality": {
            "status": "candidate",
            "blindReviewId": None,
            "recognitionConfidence": None,
            "topologyReviewed": False,
            "materialReviewed": False,
            "scaleReviewed": True,
            "animationReviewed": False,
        },
    }
    document["assets"] = list(by_id.values())
    CATALOG.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print("REGISTERED_CANDIDATES", len(document["assets"]))


if __name__ == "__main__":
    main()
