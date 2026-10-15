"""Apply source-audit quality flags only to independently reviewed candidates."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
AUDIT = ROOT / "build" / "candidate-quality-audit.json"

catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
audit = {item["assetId"]: item for item in json.loads(AUDIT.read_text(encoding="utf-8"))["assets"]}
updated = 0
for asset in catalog["assets"]:
    item = audit[asset["id"]]
    if not asset["quality"].get("blindReviewId"):
        continue
    if not (item["topologyOk"] and item["materialOk"] and item["animationEvidenceOk"]):
        raise SystemExit(f"Source audit failed for reviewed candidate: {asset['id']}")
    asset["quality"]["topologyReviewed"] = True
    asset["quality"]["materialReviewed"] = True
    asset["quality"]["animationReviewed"] = True
    updated += 1
CATALOG.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(f"CANDIDATE_QUALITY_FLAGS_APPLIED {updated}")
