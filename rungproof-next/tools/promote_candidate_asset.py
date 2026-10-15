"""Promote one fully evidenced candidate into the production catalog."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from validate_asset_evidence import evidence_gaps
from validate_industrial_references import reference_gaps


ROOT = Path(__file__).resolve().parents[1]
CANDIDATES = ROOT / "assets" / "catalog" / "candidates.catalog.json"
PRODUCTION = ROOT / "assets" / "catalog" / "production.catalog.json"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("asset_id")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    candidates = json.loads(CANDIDATES.read_text(encoding="utf-8"))
    production = json.loads(PRODUCTION.read_text(encoding="utf-8"))
    matches = [item for item in candidates["assets"] if item["id"] == args.asset_id]
    if len(matches) != 1:
        raise SystemExit(f"Expected one candidate {args.asset_id!r}, found {len(matches)}")
    if any(item["id"] == args.asset_id for item in production["assets"]):
        raise SystemExit(f"Production catalog already contains {args.asset_id!r}")

    asset = matches[0]
    gaps = evidence_gaps(asset, require_independent_review=True)
    gaps += reference_gaps(asset["id"], require_reference=True)
    if gaps:
        raise SystemExit("Promotion blocked: " + "; ".join(gaps))
    print(f"PROMOTION_READY {args.asset_id}")
    if not args.apply:
        print("Dry run only; pass --apply to update both catalogs.")
        return 0

    candidates["assets"] = [item for item in candidates["assets"] if item["id"] != args.asset_id]
    asset["quality"]["status"] = "approved"
    production["assets"].append(asset)
    production["assets"].sort(key=lambda item: item["id"])
    CANDIDATES.write_text(json.dumps(candidates, indent=2) + "\n", encoding="utf-8")
    PRODUCTION.write_text(json.dumps(production, indent=2) + "\n", encoding="utf-8")
    print(f"PROMOTED {args.asset_id}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
