"""Prepare a local manifest for the three candidates lacking blind review."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
OUTPUT = ROOT / "build" / "blind-review-assets" / "pending-authorization.json"
TARGETS = {
    "process.dosing.skid.liquid-metering.v1",
    "material-handling.receiver.container-two-position.v1",
    "sensing.dimensioning.parcel-three-height.v1",
}

catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
items = []
for asset in catalog["assets"]:
    if asset["id"] not in TARGETS:
        continue
    thumbnail = asset["model"]["thumbnailFile"].removeprefix("res://")
    path = ROOT / thumbnail
    items.append({
        "assetId": asset["id"],
        "displayName": asset["displayName"],
        "thumbnail": thumbnail,
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "externalReviewStatus": "pending-explicit-authorization",
    })
if {item["assetId"] for item in items} != TARGETS:
    raise SystemExit("Pending review target set does not match the expected three assets")
OUTPUT.write_text(json.dumps({"version": 1, "assets": items}, indent=2) + "\n", encoding="utf-8")
print(f"PENDING_REVIEW_MANIFEST {len(items)}")
