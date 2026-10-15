"""Record only evidence that is directly inherited from the copied source model.

This deliberately does not create blind-recognition confidence or an
independent reviewer identity. Those remain separate admission gates.
"""
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"


def main() -> int:
    document = json.loads(CATALOG.read_text(encoding="utf-8"))
    count = 0
    for asset in document["assets"]:
        if not asset["id"].startswith("training.accessory."):
            continue
        review = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
        review = review.parent.parent / "review"
        required = ["hero.png", "blind_review.png", "scale_reference.png", "wireframe.png", "visual_review.md"]
        if not all((review / name).is_file() for name in required):
            continue
        quality = asset["quality"]
        quality["topologyReviewed"] = True
        quality["materialReviewed"] = True
        quality["scaleReviewed"] = True
        quality["animationReviewed"] = True
        quality["evidenceBasis"] = "inherited-source-family-review; asset-specific blind recognition remains pending"
        count += 1
    CATALOG.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print(f"TRAINING_INHERITED_EVIDENCE_RECORDED {count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
