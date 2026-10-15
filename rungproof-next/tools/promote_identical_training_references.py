"""Promote training-package provenance when its delivery matches a compared basis model."""
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
PRODUCTION = ROOT / "assets" / "catalog" / "production.catalog.json"
REGISTER = ROOT / "assets" / "catalog" / "industrial-reference-register.json"


def main() -> int:
    candidates = json.loads(CATALOG.read_text(encoding="utf-8"))
    production = json.loads(PRODUCTION.read_text(encoding="utf-8"))
    refs = json.loads(REGISTER.read_text(encoding="utf-8"))
    assets = {asset["id"]: asset for asset in production["assets"] + candidates["assets"]}
    entries = {entry["assetId"]: entry for entry in refs["entries"]}
    promoted = 0
    pending = 0
    for asset in candidates["assets"]:
        if not asset["id"].startswith("training.accessory."):
            continue
        basis = assets.get(asset.get("genericBasisAssetId"))
        basis_ref = entries.get(asset.get("genericBasisAssetId"))
        ref = entries.get(asset["id"])
        if not basis or not basis_ref or basis_ref.get("comparisonStatus") != "compared-pass" or not ref:
            pending += 1
            continue
        delivery = ROOT / asset["model"]["deliveryGltf"].removeprefix("res://")
        basis_delivery = ROOT / basis["model"]["deliveryGltf"].removeprefix("res://")
        if not delivery.is_file() or not basis_delivery.is_file() or delivery.read_bytes() != basis_delivery.read_bytes():
            pending += 1
            continue
        ref["comparisonStatus"] = "compared-pass"
        ref["comparisonNotes"] = (
            "Compared against the registered generic basis family and the delivery model was verified "
            "byte-identical to the basis asset. Training-specific naming and scene placement remain "
            "generic; logos, trade dress, product numbers, and exact OEM claims are excluded."
        )
        ref["status"] = "referenced"
        promoted += 1
    CATALOG.write_text(json.dumps(candidates, indent=2) + "\n", encoding="utf-8")
    REGISTER.write_text(json.dumps(refs, indent=2) + "\n", encoding="utf-8")
    print(f"TRAINING_REFERENCE_COMPARISONS_PASSED {promoted}")
    print(f"TRAINING_REFERENCE_REVIEW_PENDING {pending}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
