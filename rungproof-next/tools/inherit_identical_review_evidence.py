"""Carry forward independent review only for byte-identical delivery models."""
from __future__ import annotations

import json
import hashlib
import shutil
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"


def main() -> int:
    document = json.loads(CATALOG.read_text(encoding="utf-8"))
    production = json.loads((ROOT / "assets" / "catalog" / "production.catalog.json").read_text(encoding="utf-8"))
    all_assets = production["assets"] + document["assets"]
    by_id = {asset["id"]: asset for asset in all_assets}
    reviewed_by_hash = {}
    for candidate in all_assets:
        delivery = ROOT / candidate["model"]["deliveryGltf"].removeprefix("res://")
        review = ROOT / candidate["model"]["sourceBlend"].removeprefix("res://")
        review = review.parent.parent / "review" / "independent_recognition.json"
        if delivery.is_file() and review.is_file():
            digest = hashlib.sha256(delivery.read_bytes()).hexdigest()
            reviewed_by_hash.setdefault(digest, candidate)
    inherited = 0
    pending = 0
    for asset in document["assets"]:
        if not asset["id"].startswith("training.accessory."):
            continue
        source = by_id.get(asset.get("genericBasisAssetId"))
        delivery = ROOT / asset["model"]["deliveryGltf"].removeprefix("res://")
        source_delivery = ROOT / source["model"]["deliveryGltf"].removeprefix("res://") if source else None
        source_review = ROOT / source["model"]["sourceBlend"].removeprefix("res://") if source else None
        source_review = source_review.parent.parent / "review" / "independent_recognition.json" if source_review else None
        if not delivery.is_file():
            pending += 1
            continue
        if source is None or not source_delivery.is_file() or delivery.read_bytes() != source_delivery.read_bytes() or not source_review.is_file():
            digest = hashlib.sha256(delivery.read_bytes()).hexdigest()
            source = reviewed_by_hash.get(digest)
            if source is None:
                pending += 1
                continue
            source_review = ROOT / source["model"]["sourceBlend"].removeprefix("res://")
            source_review = source_review.parent.parent / "review" / "independent_recognition.json"
        review = json.loads(source_review.read_text(encoding="utf-8"))
        review["assetId"] = asset["id"]
        review["identifiedFamily"] = f"{review['identifiedFamily']} (identical generic basis model)"
        review["sourceAssetId"] = source["id"]
        review["evidenceBasis"] = "delivery GLB is byte-identical to the independently reviewed generic basis asset"
        destination = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
        destination = destination.parent.parent / "review" / "independent_recognition.json"
        destination.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
        asset["quality"]["blindReviewId"] = f"inherited:{review['reviewerId']}"
        asset["quality"]["recognitionConfidence"] = review["confidence"]
        asset["quality"]["evidenceBasis"] = "identical-delivery-independent-review"
        inherited += 1
    CATALOG.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print(f"IDENTICAL_REVIEW_EVIDENCE_INHERITED {inherited}")
    print(f"ASSET_REVIEW_PENDING {pending}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
