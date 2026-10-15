"""Write explicit candidate review records for lesson-specific asset packages."""
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"


def main() -> int:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    assets = [item for item in catalog["assets"] if item["id"].startswith("training.accessory.")]
    for asset in assets:
        source = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
        review = source.parent.parent / "review"
        review.mkdir(parents=True, exist_ok=True)
        (review / "visual_review.md").write_text(
            "\n".join([
                f"# Candidate visual review — {asset['displayName']}",
                "",
                f"Asset ID: `{asset['id']}`",
                "Status: **candidate / review pending**",
                "",
                "This package is a generic, copyright-safe training accessory "
                "derived from an existing reusable equipment family. It is "
                "not an exact OEM part and is not approved for production.",
                "",
                "## Required review views",
                "",
                "- Hero view",
                "- Left and right side views",
                "- Mechanism/detail view where applicable",
                "- Opposite-end and underside views where applicable",
                "- End-alignment/clearance view where applicable",
                "- Context-free blind-recognition image",
                "- Wireframe/topology view",
                "- One-meter scale-reference view",
                "- Animated-state captures for every declared kinematic axis",
                "",
                "## Current evidence",
                "",
                "The package contains source, delivery, collision, and thumbnail "
                "resources. The copied family evidence is retained as provenance "
                "context, but it does not count as an independent review of this "
                "named training accessory.",
                "",
                "## Open admission gates",
                "",
                "- Asset-specific topology, material, and scale review.",
                "- Independent context-free recognition with confidence of at least 0.80.",
                "- Final comparison of the generic model against its registered reference family.",
                "- Promotion decision after all evidence validators pass.",
            ]) + "\n",
            encoding="utf-8",
        )
    print(f"TRAINING_REVIEW_RECORDS_WRITTEN {len(assets)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
