"""Promote reviewed migrated scene documents back to their source contracts.

The migrated document is a source scene plus a generated ``migration`` block.
Visual review frequently corrects positions, rotations, dimensions, or equipment
choices in that migrated copy. This tool makes promotion explicit so a later
migration cannot silently discard those accepted corrections.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "rungproof-next" / "scenes" / "catalog" / "original-scenes.catalog.json"


def canonical(document: dict[str, object]) -> str:
    return json.dumps(document, sort_keys=True, separators=(",", ":"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Write reviewed migrated content back to each declared source file.",
    )
    args = parser.parse_args()

    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    divergent: list[tuple[Path, dict[str, object]]] = []
    for entry in catalog["scenes"]:
        migrated_path = ROOT / "rungproof-next" / str(entry["path"]).removeprefix("res://")
        source_path = ROOT / str(entry["sourceFile"])
        reviewed = json.loads(migrated_path.read_text(encoding="utf-8"))
        reviewed.pop("migration", None)
        source = json.loads(source_path.read_text(encoding="utf-8"))
        if canonical(reviewed) != canonical(source):
            divergent.append((source_path, reviewed))

    for source_path, reviewed in divergent:
        print(f"{'PROMOTE' if args.apply else 'DIVERGED'} {source_path.relative_to(ROOT)}")
        if args.apply:
            source_path.write_text(json.dumps(reviewed, indent=2) + "\n", encoding="utf-8")

    print(f"REVIEWED_SCENE_DIVERGENCES {len(divergent)}")
    if divergent and not args.apply:
        print("Run with --apply to promote the reviewed scene contracts explicitly.")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
