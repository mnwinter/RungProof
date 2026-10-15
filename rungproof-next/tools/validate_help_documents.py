"""Ensure generated help covers every declared catalog and scene I/O item."""
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
HELP = ROOT / "docs" / "help"
ISSUES: list[str] = []


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def require(path: Path, needle: str) -> None:
    if not path.is_file():
        issue = f"missing help document: {path.relative_to(ROOT)}"
    elif needle not in path.read_text(encoding="utf-8"):
        issue = f"{path.relative_to(ROOT)} missing {needle!r}"
    else:
        return
    # Report the whole catalog in one run; never turn missing documentation
    # into a pass. Deduplicate a missing file's repeated contract checks.
    if issue not in ISSUES:
        ISSUES.append(issue)


def main() -> int:
    ISSUES.clear()
    asset_count = 0
    for catalog_name in ("production.catalog.json", "candidates.catalog.json"):
        for asset in load(ROOT / "assets" / "catalog" / catalog_name)["assets"]:
            path = HELP / "assets" / f"{asset['id']}.md"
            require(path, f"Asset ID: `{asset['id']}`")
            require(path, "## Industrial reference basis")
            signals = asset.get("signals", [])
            if signals:
                require(path, "## Expected reusable I/O")
                for signal in signals:
                    # Verify the whole contract row, rather than merely the signal name.
                    # This prevents a document from silently losing the type, direction,
                    # unit, or declared behavior that tells a scene author how to use it.
                    expected = "| `{}` | `{}` | `{}` | {} | {} |".format(
                        signal["id"], signal["dataType"], signal["direction"],
                        signal.get("unit") or "", signal.get("description") or "")
                    require(path, expected)
            else:
                require(path, "No reusable external signals are declared. Do not invent I/O for this passive asset.")
            for motion in asset.get("kinematics", []):
                require(path, f"`{motion['nodePath']}`")
            asset_count += 1
    scene_count = 0
    for item in load(ROOT / "scenes" / "catalog" / "original-scenes.catalog.json")["scenes"]:
        scene = load(ROOT / item["path"].removeprefix("res://"))
        path = HELP / "scenes" / f"{scene['id']}.md"
        require(path, f"Scene ID: `{scene['id']}`")
        points = scene.get("simulation", {}).get("points", [])
        if points:
            require(path, "## Expected I/O to operate this scene")
            for point in points:
                # Include ownership and initial state as well as the tag name/type:
                # these are necessary to distinguish simulator feedback, PLC commands,
                # and internal points during integration.
                expected = "| `{}` | `{}` | **{}** | `{}` |".format(
                    point["name"], point["type"], point["owner"], point.get("initial"))
                require(path, expected)
        else:
            require(path, "This migrated scene declares no symbolic I/O points.")
        scene_count += 1
    require(HELP / "README.md", "# RungProof Next help index")
    if ISSUES:
        for issue in ISSUES:
            print(f"HELP_DOCUMENTS_ISSUE {issue}")
        print(f"HELP_DOCUMENTS_INVALID issues={len(ISSUES)} assets={asset_count} scenes={scene_count}")
        return 1
    print(f"HELP_DOCUMENTS_VALID assets={asset_count} scenes={scene_count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
