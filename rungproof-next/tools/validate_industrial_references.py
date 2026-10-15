"""Validate OEM-family provenance records for generic catalog assets."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REGISTER = ROOT / "assets" / "catalog" / "industrial-reference-register.json"


def load_catalog_assets() -> dict[str, str]:
    assets: dict[str, str] = {}
    for kind, name in (("production", "production.catalog.json"), ("candidate", "candidates.catalog.json")):
        for asset in json.loads((ROOT / "assets" / "catalog" / name).read_text(encoding="utf-8"))["assets"]:
            if asset["id"] in assets:
                raise ValueError(f"duplicate catalog asset {asset['id']}")
            assets[asset["id"]] = kind
    return assets


def entry_gaps(entry: dict) -> list[str]:
    gaps: list[str] = []
    for field in ("assetId", "referenceFamily", "status", "comparisonStatus", "comparisonNotes", "modeledFeatures", "intentionallyGeneric", "sources"):
        if not entry.get(field):
            gaps.append(f"missing {field}")
    if entry.get("status") not in {"referenced", "needs-remodel", "provisional"}:
        gaps.append("invalid status")
    if entry.get("comparisonStatus") not in {"reference-identified", "compared-pass", "remodel-required"}:
        gaps.append("invalid comparisonStatus")
    if not isinstance(entry.get("modeledFeatures"), list) or not all(isinstance(item, str) and item.strip() for item in entry.get("modeledFeatures", [])):
        gaps.append("invalid modeledFeatures")
    if not isinstance(entry.get("intentionallyGeneric"), list) or not all(isinstance(item, str) and item.strip() for item in entry.get("intentionallyGeneric", [])):
        gaps.append("invalid intentionallyGeneric")
    for source in entry.get("sources", []):
        for field in ("publisher", "title", "url", "sourceType", "accessed"):
            if not isinstance(source.get(field), str) or not source[field].strip():
                gaps.append(f"source missing {field}")
        if isinstance(source.get("url"), str) and not source["url"].startswith("https://"):
            gaps.append("source URL must use https")
        if source.get("sourceType") not in {"oem-datasheet", "oem-manual", "oem-product-page"}:
            gaps.append("source must be an OEM document or product page")
    return gaps


def reference_gaps(asset_id: str, *, require_reference: bool) -> list[str]:
    register = json.loads(REGISTER.read_text(encoding="utf-8"))
    matches = [entry for entry in register["entries"] if entry.get("assetId") == asset_id]
    if not matches:
        return ["missing industrial OEM reference"] if require_reference else []
    if len(matches) != 1:
        return ["duplicate industrial OEM reference records"]
    gaps = entry_gaps(matches[0])
    if require_reference and matches[0].get("comparisonStatus") != "compared-pass":
        gaps.append("source-model comparison has not passed")
    return gaps


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict-production", action="store_true", help="fail until every production asset has a valid reference record")
    parser.add_argument("--strict-all", action="store_true", help="fail until every production and candidate asset has a valid source-model reference record")
    args = parser.parse_args()
    assets = load_catalog_assets()
    register = json.loads(REGISTER.read_text(encoding="utf-8"))
    assert register["version"] == 1
    ids = [entry.get("assetId") for entry in register["entries"]]
    assert len(ids) == len(set(ids)), "duplicate industrial reference entries"
    for entry in register["entries"]:
        assert entry["assetId"] in assets, f"unknown asset in reference register: {entry['assetId']}"
        gaps = entry_gaps(entry)
        assert not gaps, f"invalid reference record {entry['assetId']}: {'; '.join(gaps)}"
    production = [asset_id for asset_id, kind in assets.items() if kind == "production"]
    candidates = [asset_id for asset_id, kind in assets.items() if kind == "candidate"]
    referenced_ids = {entry["assetId"] for entry in register["entries"]}
    compared_ids = {entry["assetId"] for entry in register["entries"] if entry["comparisonStatus"] == "compared-pass"}
    production_missing = [asset_id for asset_id in production if reference_gaps(asset_id, require_reference=True)]
    candidate_missing = [asset_id for asset_id in candidates if reference_gaps(asset_id, require_reference=True)]
    print(f"INDUSTRIAL_REFERENCE_VALID: {len(register['entries'])} records")
    print(f"PRODUCTION_REFERENCE_COVERAGE: {len(set(production) & referenced_ids)} / {len(production)}")
    print(f"PRODUCTION_SOURCE_MODEL_PASSED: {len(set(production) & compared_ids)} / {len(production)}")
    print(f"CANDIDATE_REFERENCE_COVERAGE: {len(set(candidates) & referenced_ids)} / {len(candidates)}")
    print(f"CANDIDATE_SOURCE_MODEL_PASSED: {len(set(candidates) & compared_ids)} / {len(candidates)}")
    for asset_id in production_missing:
        print(f"REFERENCE_REMEDIATION_REQUIRED {asset_id}")
    for asset_id in candidate_missing:
        print(f"CANDIDATE_REFERENCE_REMEDIATION_REQUIRED {asset_id}")
    if (args.strict_production and production_missing) or (args.strict_all and (production_missing or candidate_missing)):
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
