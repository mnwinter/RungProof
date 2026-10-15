"""Offline validation for the clean-sheet RungProof Next catalog."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    catalog_path = ROOT / "assets" / "catalog" / "production.catalog.json"
    candidates_path = ROOT / "assets" / "catalog" / "candidates.catalog.json"
    plan_path = ROOT / "assets" / "catalog" / "portfolio.plan.json"
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    candidates = json.loads(candidates_path.read_text(encoding="utf-8"))
    plan = json.loads(plan_path.read_text(encoding="utf-8"))
    assert catalog["version"] == 1
    assert catalog["catalogId"] == "rungproof-production"
    assert isinstance(catalog["assets"], list)
    ids = [asset["id"] for asset in catalog["assets"]]
    assert len(ids) == len(set(ids))
    candidate_ids = [asset["id"] for asset in candidates["assets"]]
    assert len(candidate_ids) == len(set(candidate_ids))
    assert not set(ids).intersection(candidate_ids)
    for asset in catalog["assets"]:
        assert asset["quality"]["status"] == "approved"
        assert asset["quality"]["recognitionConfidence"] >= 0.80
    for asset in candidates["assets"]:
        assert asset["quality"]["status"] != "approved"
        for field in ("sourceBlend", "deliveryGltf", "collisionFile", "thumbnailFile"):
            resource = asset["model"][field]
            assert resource.startswith("res://")
            assert (ROOT / resource.removeprefix("res://")).is_file(), resource
    assert plan["target"]["productionFamilies"] >= 500
    assert sum(item["familyTarget"] for item in plan["categories"]) == plan["target"]["productionFamilies"]
    print(f"CATALOG_VALID: {len(ids)} production assets")
    print(f"CANDIDATES_VALID: {len(candidate_ids)} candidate assets")
    print(f"PORTFOLIO_TARGET: {plan['target']['productionFamilies']} families")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
