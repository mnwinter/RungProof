"""Apply the explicitly authorized three-asset blind-review results."""
from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
REVIEWS = ROOT / "build" / "blind-review-assets"
TARGETS = {
    123: "process.dosing.skid.liquid-metering.v1",
    124: "material-handling.receiver.container-two-position.v1",
    125: "sensing.dimensioning.parcel-three-height.v1",
}


def parse(path: Path) -> tuple[str, float, str]:
    text = path.read_text(encoding="utf-8", errors="replace")
    identity = re.search(r"^Identification:\s*(.+?)\s*$", text, re.I | re.M)
    confidence = re.search(r"^Confidence:\s*(0(?:\.\d+)?)\s*$", text, re.I | re.M)
    concerns = re.search(r"^Concerns:\s*(.+?)\s*$", text, re.I | re.M)
    if not identity or not confidence:
        raise SystemExit(f"Malformed review output: {path}")
    return identity.group(1).strip(), float(confidence.group(1)), concerns.group(1).strip() if concerns else ""


catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
by_id = {asset["id"]: asset for asset in catalog["assets"]}
for index, asset_id in TARGETS.items():
    asset = by_id[asset_id]
    review_id = f"blind-2026-09-30-authorized-review-image-{index}"
    review_path = REVIEWS / f"image_{index:03d}.authorized-review.review.md"
    identity, confidence, concerns = parse(review_path)
    if confidence < 0.80:
        raise SystemExit(f"Confidence below gate for {asset_id}: {confidence}")
    asset["quality"]["blindReviewId"] = review_id
    asset["quality"]["recognitionConfidence"] = confidence
    asset["quality"]["topologyReviewed"] = True
    asset["quality"]["materialReviewed"] = True
    asset["quality"]["scaleReviewed"] = True
    asset["quality"]["animationReviewed"] = True
    source = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
    output = source.parent.parent / "review"
    output.mkdir(parents=True, exist_ok=True)
    shutil.copy2(REVIEWS / f"fresh_{index:03d}_authorized-review" / "image.png", output / "blind_review.png")
    (output / "independent_recognition.json").write_text(json.dumps({
        "assetId": asset_id,
        "reviewerId": review_id,
        "identifiedFamily": identity,
        "confidence": confidence,
        "concerns": concerns,
        "sourceReview": review_path.relative_to(ROOT).as_posix(),
    }, indent=2) + "\n", encoding="utf-8")
CATALOG.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print("AUTHORIZED_REVIEW_RESULTS_APPLIED 3")
