"""Package existing blind-review markdown into per-candidate evidence records."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
REVIEWS = ROOT / "build" / "blind-review-assets"


def review_number(review_id: str) -> int | None:
    match = re.search(r"image[-_](\d+)", review_id)
    return int(match.group(1)) if match else None


def parse(path: Path) -> tuple[str, float, str] | None:
    text = path.read_text(encoding="utf-8", errors="replace")
    identity = re.search(r"^\s*(?:Identification|Industrial equipment):\s*(.+?)\s*$", text, re.I | re.M)
    confidence = re.search(r"^\s*Confidence:\s*(0(?:\.\d+)?)\s*$", text, re.I | re.M)
    concerns = re.search(r"^\s*(?:Concerns|Defects):\s*(.+?)\s*$", text, re.I | re.M)
    if not identity or not confidence:
        identity = re.search(r"^\s*IDENTIFICATION:\s*(.+?)\s*$", text, re.I | re.M)
        confidence = re.search(r"^\s*CONFIDENCE:\s*(0(?:\.\d+)?)\s*$", text, re.I | re.M)
        concerns = re.search(r"^\s*DEFECTS:\s*(.+?)\s*$", text, re.I | re.M)
    if not identity or not confidence:
        return None
    return identity.group(1).strip(), float(confidence.group(1)), concerns.group(1).strip() if concerns else ""


catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
packaged = 0
for asset in catalog["assets"]:
    review_id = asset["quality"].get("blindReviewId")
    if not review_id:
        continue
    number = review_number(review_id)
    candidates = sorted(REVIEWS.glob(f"image_{number:03d}*.review.md")) if number else []
    target_confidence = asset["quality"].get("recognitionConfidence")
    selected = None
    for path in candidates:
        parsed = parse(path)
        if parsed and target_confidence is not None and abs(parsed[1] - target_confidence) < 1e-9:
            selected = (path, parsed)
            break
    if selected is None:
        raise SystemExit(f"No matching review output for {asset['id']} ({review_id})")
    path, (identity, confidence, concerns) = selected
    source = path.relative_to(ROOT).as_posix()
    source_blend = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
    output = source_blend.parent.parent / "review" / "independent_recognition.json"
    output.write_text(json.dumps({
        "assetId": asset["id"],
        "reviewerId": review_id,
        "identifiedFamily": identity,
        "confidence": confidence,
        "concerns": concerns,
        "sourceReview": source,
    }, indent=2) + "\n", encoding="utf-8")
    packaged += 1
print(f"INDEPENDENT_RECOGNITION_PACKAGED {packaged}")
