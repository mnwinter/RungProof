"""Verify that production assets carry inspectable quality evidence.

Candidates may be incomplete while they are being authored.  Production assets
may not: a catalog flag is never a substitute for the actual review material.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
COMMON_REVIEW_FILES = {
    "hero.png",
    "blind_review.png",
    "scale_reference.png",
    "wireframe.png",
    "visual_review.md",
}
KINEMATIC_REVIEW_FILES = {"state_stopped.png", "state_running.png"}


def resource_path(resource: str) -> Path:
    if not resource.startswith("res://"):
        raise ValueError(f"Catalog resource is not project-relative: {resource}")
    return ROOT / resource.removeprefix("res://")


def evidence_gaps(asset: dict, require_independent_review: bool = False) -> list[str]:
    model = asset["model"]
    source = resource_path(model["sourceBlend"])
    asset_root = source.parent.parent
    review = asset_root / "review"
    gaps: list[str] = []
    for field in ("sourceBlend", "deliveryGltf", "collisionFile", "thumbnailFile"):
        if not resource_path(model[field]).is_file():
            gaps.append(f"missing {field}")
    for name in sorted(COMMON_REVIEW_FILES):
        if not (review / name).is_file():
            gaps.append(f"missing review/{name}")
    if asset.get("kinematics"):
        for name in sorted(KINEMATIC_REVIEW_FILES):
            if not (review / name).is_file():
                gaps.append(f"missing review/{name}")
    quality = asset["quality"]
    if quality.get("recognitionConfidence") is None or quality["recognitionConfidence"] < 0.80:
        gaps.append("blind-recognition confidence below 0.80")
    for field in ("topologyReviewed", "materialReviewed", "scaleReviewed", "animationReviewed"):
        if not quality.get(field):
            gaps.append(f"quality flag {field}=false")
    if require_independent_review:
        review_result = review / "independent_recognition.json"
        if not review_result.is_file():
            gaps.append("missing review/independent_recognition.json")
        else:
            try:
                result = json.loads(review_result.read_text(encoding="utf-8"))
                assert result["assetId"] == asset["id"]
                assert isinstance(result["identifiedFamily"], str) and result["identifiedFamily"].strip()
                assert isinstance(result["reviewerId"], str) and result["reviewerId"].strip()
                assert result["reviewerId"].strip().lower() not in {"self", "author", "generator"}
                assert result["confidence"] >= 0.80
            except (AssertionError, KeyError, TypeError, ValueError, json.JSONDecodeError):
                gaps.append("invalid review/independent_recognition.json")
    return gaps


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict-candidates", action="store_true",
                        help="fail if any candidate is missing production evidence")
    args = parser.parse_args()
    catalogs = {
        "production": ROOT / "assets" / "catalog" / "production.catalog.json",
        "candidate": ROOT / "assets" / "catalog" / "candidates.catalog.json",
    }
    incomplete_candidates: list[tuple[str, list[str]]] = []
    approved = 0
    for kind, path in catalogs.items():
        for asset in json.loads(path.read_text(encoding="utf-8"))["assets"]:
            gaps = evidence_gaps(asset, require_independent_review=kind == "production")
            if kind == "production":
                assert asset["quality"]["status"] == "approved", asset["id"]
                assert not gaps, f"Production asset {asset['id']} lacks evidence: {'; '.join(gaps)}"
                approved += 1
            elif gaps:
                incomplete_candidates.append((asset["id"], gaps))

    candidate_count = len(json.loads(catalogs["candidate"].read_text(encoding="utf-8"))["assets"])
    complete_candidates = candidate_count - len(incomplete_candidates)
    print(f"PRODUCTION_EVIDENCE_VALID: {approved} assets")
    print(f"CANDIDATE_REVIEW_PACKAGE_COMPLETE: {complete_candidates} / {candidate_count}")
    print(f"CANDIDATE_REVIEW_PACKAGE_INCOMPLETE: {len(incomplete_candidates)}")
    for asset_id, gaps in incomplete_candidates:
        print(f"EVIDENCE_PENDING {asset_id}: {', '.join(gaps)}")
    if args.strict_candidates and incomplete_candidates:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
