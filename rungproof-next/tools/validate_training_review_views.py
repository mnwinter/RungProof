"""Audit review-view coverage for the training candidate packages."""
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
CORE = {"hero.png", "blind_review.png", "scale_reference.png", "wireframe.png", "visual_review.md", "independent_recognition.json"}


def main() -> int:
    document = json.loads(CATALOG.read_text(encoding="utf-8"))
    failures: list[str] = []
    checked = 0
    for asset in document["assets"]:
        if not asset["id"].startswith("training.accessory."):
            continue
        checked += 1
        source = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
        review = source.parent.parent / "review"
        names = {path.name for path in review.iterdir() if path.is_file()} if review.is_dir() else set()
        missing = sorted(CORE - names)
        detail_images = [name for name in names if name.endswith(".png") and name not in CORE]
        if missing:
            failures.append(f"{asset['id']}: missing {', '.join(missing)}")
        if len(detail_images) < 4:
            failures.append(f"{asset['id']}: fewer than four supplemental review images")
        if asset.get("kinematics") and not {"state_stopped.png", "state_running.png"}.issubset(names):
            failures.append(f"{asset['id']}: missing animated-state review images")
    print(f"TRAINING_REVIEW_VIEW_PACKAGES {checked}")
    print(f"TRAINING_REVIEW_VIEW_FAILURES {len(failures)}")
    for failure in failures:
        print(f"REVIEW_VIEW_PENDING {failure}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
