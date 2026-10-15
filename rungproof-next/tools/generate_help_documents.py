"""Generate contract-derived help for every catalog asset and migrated scene.

The generator deliberately does not invent PLC addresses or ownership for asset
signals.  Asset contracts describe reusable interfaces; scene documents bind
symbolic points and are the authority for PC/PLC/SIM ownership.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DOC_ROOT = ROOT / "docs" / "help"
ASSET_DOCS = DOC_ROOT / "assets"
SCENE_DOCS = DOC_ROOT / "scenes"
REFERENCE_REGISTER = ROOT / "assets" / "catalog" / "industrial-reference-register.json"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def text(value: object) -> str:
    return "" if value is None else str(value)


def cell(value: object) -> str:
    return text(value).replace("|", "\\|").replace("\n", "<br>")


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def asset_help(asset: dict, catalog_kind: str, references: dict[str, dict]) -> str:
    quality = asset["quality"]
    lines = [
        f"# {asset['displayName']} help",
        "",
        f"Asset ID: `{asset['id']}`  ",
        f"Catalog status: **{catalog_kind} / {quality['status']}**  ",
        f"Category: `{asset['category']}`",
        "",
        "## Purpose and integration boundary",
        "",
        "This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.",
        "",
        "## Physical envelope",
        "",
        f"- Width: {asset['bounds']['widthM']} m",
        f"- Height: {asset['bounds']['heightM']} m",
        f"- Depth: {asset['bounds']['depthM']} m",
        f"- Source: `{asset['model']['sourceBlend']}`",
        f"- Delivery: `{asset['model']['deliveryGltf']}`",
        "",
        "## Expected reusable I/O",
        "",
    ]
    signals = asset.get("signals", [])
    if signals:
        lines += ["| Signal | Type | Direction | Unit | Description |", "| --- | --- | --- | --- | --- |"]
        for signal in signals:
            lines.append("| `{}` | `{}` | `{}` | {} | {} |".format(
                cell(signal["id"]), cell(signal["dataType"]), cell(signal["direction"]),
                cell(signal.get("unit")), cell(signal.get("description"))))
    else:
        lines.append("No reusable external signals are declared. Do not invent I/O for this passive asset.")
    lines += [
        "",
        "Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.",
        "",
        "## Kinematics",
        "",
    ]
    kinematics = asset.get("kinematics", [])
    if kinematics:
        lines += ["| ID | Kind | Node | Range | Maximum rate |", "| --- | --- | --- | --- | --- |"]
        for item in kinematics:
            extent = f"{item.get('minimum')} to {item.get('maximum')} {item.get('unit', '')}".strip()
            lines.append("| `{}` | {} | `{}` | {} | {} |".format(
                cell(item["id"]), cell(item["kind"]), cell(item["nodePath"]), cell(extent), cell(item.get("maximumRate"))))
    else:
        lines.append("No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.")
    lines += ["", "## Connectors", ""]
    connectors = asset.get("connectors", [])
    if connectors:
        lines += ["| ID | Type | Orientation |", "| --- | --- | --- |"]
        for connector in connectors:
            lines.append("| `{}` | {} | {} |".format(cell(connector.get("id")), cell(connector.get("type")), cell(connector.get("orientation"))))
    else:
        lines.append("No reusable connector contract is declared.")
    if catalog_kind == "candidate":
        lines += ["", "## Admission status", "", "This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass."]
    reference = references.get(asset["id"])
    lines += ["", "## Industrial reference basis", ""]
    if reference:
        lines += [f"Generic reference family: **{reference['referenceFamily']}**.", f"Source-model review: **{reference['comparisonStatus']}** — {reference['comparisonNotes']}", "", "| OEM source | Type | Accessed |", "| --- | --- | --- |"]
        for source in reference["sources"]:
            lines.append(f"| [{source['publisher']}: {source['title']}]({source['url']}) | {source['sourceType']} | {source['accessed']} |")
        lines += ["", "Modeled family features:"] + [f"- {item}" for item in reference["modeledFeatures"]]
        lines += ["", "Intentionally generic / not claimed:"] + [f"- {item}" for item in reference["intentionallyGeneric"]]
    else:
        lines.append("No OEM-family reference record is registered yet. This asset is in source-provenance remediation and must not be represented as an exact real-world component.")
    return "\n".join(lines)


def scene_help(scene: dict, catalog_entry: dict) -> str:
    simulation = scene.get("simulation", {})
    points = simulation.get("points", [])
    lines = [
        f"# {scene['name']} help",
        "",
        f"Scene ID: `{scene['id']}`  ",
        f"Migrated source: `{catalog_entry['sourceFile']}`  ",
        f"Scene contract: `{catalog_entry['path']}`",
        "",
        "## Purpose",
        "",
        scene.get("description", "No description is declared."),
        "",
        "## Expected I/O to operate this scene",
        "",
        "All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.",
        "",
    ]
    if points:
        lines += ["| Point | Type | Owner | Initial value |", "| --- | --- | --- | --- |"]
        for point in points:
            lines.append("| `{}` | `{}` | **{}** | `{}` |".format(
                cell(point["name"]), cell(point["type"]), cell(point["owner"]), cell(point.get("initial"))))
    else:
        lines.append("This migrated scene declares no symbolic I/O points.")
    lines += ["", "## Operator actions", ""]
    actions = simulation.get("actions", [])
    if actions:
        lines += ["| Action | Type | Bound point/sequence |", "| --- | --- | --- |"]
        for action in actions:
            target = action.get("point", action.get("sequence", ""))
            lines.append("| `{}` | `{}` | `{}` |".format(cell(action.get("label", action["id"])), cell(action["type"]), cell(target)))
    else:
        lines.append("No operator action contract is declared.")
    lines += ["", "## Equipment bindings", ""]
    bindings = simulation.get("pointBindings", [])
    if bindings:
        lines += ["| Symbolic point | Equipment | Mode |", "| --- | --- | --- |"]
        for binding in bindings:
            lines.append("| `{}` | `{}` | `{}` |".format(cell(binding["point"]), cell(binding["equipmentId"]), cell(binding["mode"])))
    else:
        lines.append("No point-to-equipment bindings are declared.")
    lines += ["", "## Expected equipment", ""]
    lines += ["| ID | Type | Label |", "| --- | --- | --- |"]
    for equipment in scene.get("equipment", []):
        lines.append("| `{}` | `{}` | {} |".format(cell(equipment["id"]), cell(equipment["type"]), cell(equipment.get("label"))))
    safe_state = simulation.get("safeState", {})
    lines += ["", "## Stop and safety boundary", "", "A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning."]
    if safe_state:
        lines += ["", "Declared simulation safe state:", "", "| Point | Value |", "| --- | --- |"]
        for key, value in safe_state.items():
            lines.append(f"| `{cell(key)}` | `{cell(value)}` |")
    guide = scene.get("training", {}).get("machineGuide")
    if guide:
        lines += ["", "## Machine guide", "", guide.get("purpose", "")]
        for heading, key in (("Start conditions", "startConditions"), ("Normal sequence", "normalSequence"), ("Expected observations", "expectedObservations")):
            values = guide.get(key, [])
            if values:
                lines += ["", f"### {heading}", ""]
                lines += [f"- {value}" for value in values]
    return "\n".join(lines)


def scene_io_table(scene: dict) -> str:
    """Render the current symbolic point contract without operation guidance."""
    points = scene.get("simulation", {}).get("points", [])
    if not points:
        return "This migrated scene declares no symbolic I/O points.\n"
    lines = ["| Point | Type | Owner | Initial value |", "| --- | --- | --- | --- |"]
    for point in points:
        lines.append("| `{}` | `{}` | **{}** | `{}` |".format(
            cell(point["name"]), cell(point["type"]), cell(point["owner"]), cell(point.get("initial"))))
    return "\n".join(lines) + "\n"


def sync_scene_io(document: str, scene: dict) -> str:
    """Replace only the I/O table, its heading, and Scene ID; fail on ambiguity.

    A missing section is appended. Authored notes inside an existing section are
    preserved, as are all operation, sequence, action, and safety sections.
    """
    newline = "\r\n" if "\r\n" in document else "\n"
    document = document.replace("\r\n", "\n")
    ids = list(re.finditer(r"^Scene ID:.*$", document, re.MULTILINE))
    if len(ids) > 1:
        raise ValueError("Duplicate Scene ID lines")
    identifier = f"Scene ID: `{scene['id']}`"
    if ids:
        match = ids[0]
        spacing = re.search(r"[ \t]*$", match.group()).group()
        document = document[:match.start()] + identifier + spacing + document[match.end():]
    else:
        title = re.match(r"#[^\n]*\n", document)
        position = title.end() if title else 0
        document = document[:position] + "\n" + identifier + "\n" + document[position:]
    sections = list(re.finditer(r"^## (?:Expected I/O to operate this scene|Symbolic I/O)[ \t]*$", document, re.MULTILINE))
    if len(sections) > 1:
        raise ValueError("Duplicate I/O sections")
    heading = "## Expected I/O to operate this scene"
    table = scene_io_table(scene)
    if not sections:
        return (document + ("" if document.endswith("\n\n") else "\n") + heading + "\n\n" + table).replace("\n", newline)
    match = sections[0]
    following = re.search(r"^## ", document[match.end():], re.MULTILINE)
    end = match.end() + following.start() if following else len(document)
    body = document[match.end():end]
    tables = list(re.finditer(r"(?:^\|[^\n]*\n?)+", body, re.MULTILINE))
    empty = "This migrated scene declares no symbolic I/O points."
    if len(tables) > 1:
        raise ValueError("Multiple tables in I/O section")
    if tables:
        block = tables[0]
        rows = block.group().splitlines()
        if len(rows) < 2 or not re.fullmatch(r"\|(?:\s*:?-+:?\s*\|){4,}", rows[1]):
            raise ValueError("Malformed I/O table")
        columns = [re.split(r"(?<!\\)\|", row)[1:-1] for row in rows]
        headers = [value.strip() for value in columns[0][:4]]
        if headers[:3] != ["Point", "Type", "Owner"] or headers[3] not in ("Initial", "Initial value"):
            raise ValueError("Unrecognized I/O table header")
        if any(len(row) != len(columns[0]) for row in columns):
            raise ValueError("Malformed I/O table columns")
        # Keep authored explanations as notes outside the canonical table. Even
        # removed-point notes survive; this mode does not rewrite their meaning.
        notes = []
        for row in columns[2:]:
            for label, value in zip(columns[0][4:], row[4:]):
                if value.strip():
                    notes.append(f"- {row[0].strip()} — {label.strip()}: {value.strip()}")
        if notes:
            table += "\nAuthored I/O notes (retained from the prior table):\n\n" + "\n".join(notes) + "\n"
        body = body[:block.start()] + table + body[block.end():]
    elif empty in body:
        body = body.replace(empty, table.rstrip("\n"), 1)
    else:
        raise ValueError("I/O section has no recognizable contract table")
    return (document[:match.start()] + heading + body + document[end:]).replace("\n", newline)


def sync_scene_documents() -> int:
    # Validate every document before writing any, so malformed input causes no
    # partial catalog update. This path never generates asset help or the index.
    updates = []
    for item in load(ROOT / "scenes/catalog/original-scenes.catalog.json")["scenes"]:
        scene = load(ROOT / item["path"].removeprefix("res://"))
        path = SCENE_DOCS / f"{scene['id']}.md"
        with path.open(encoding="utf-8", newline="") as source:
            original = source.read()
        try:
            revised = sync_scene_io(original, scene)
        except ValueError as error:
            raise ValueError(f"{path}: {error}") from error
        if revised != original:
            updates.append((path, revised))
    for path, revised in updates:
        with path.open("w", encoding="utf-8", newline="") as destination:
            destination.write(revised)
    print(f"HELP_SCENE_IO_SYNCED changed={len(updates)}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sync-scene-io", action="store_true",
                        help="Sync only scene IDs and I/O tables, retaining authored prose")
    args = parser.parse_args()
    if args.sync_scene_io:
        return sync_scene_documents()
    references = {entry["assetId"]: entry for entry in load(REFERENCE_REGISTER)["entries"]}
    catalogs = (("production", load(ROOT / "assets" / "catalog" / "production.catalog.json")),
                ("candidate", load(ROOT / "assets" / "catalog" / "candidates.catalog.json")))
    asset_entries = []
    for kind, catalog in catalogs:
        for asset in catalog["assets"]:
            write(ASSET_DOCS / f"{asset['id']}.md", asset_help(asset, kind, references))
            asset_entries.append((asset["id"], asset["displayName"], kind))

    scene_catalog = load(ROOT / "scenes" / "catalog" / "original-scenes.catalog.json")
    scene_entries = []
    for item in scene_catalog["scenes"]:
        scene_path = ROOT / item["path"].removeprefix("res://")
        scene = load(scene_path)
        write(SCENE_DOCS / f"{scene['id']}.md", scene_help(scene, item))
        scene_entries.append((scene["id"], scene["name"]))

    index = ["# RungProof Next help index", "", "Generated from the current catalogs and migrated scene contracts. Do not edit generated documents by hand.", "", "## Assets", ""]
    index += [f"- [{name}](assets/{asset_id}.md) — {kind}" for asset_id, name, kind in asset_entries]
    index += ["", "## Scenes", ""]
    index += [f"- [{name}](scenes/{scene_id}.md)" for scene_id, name in scene_entries]
    write(DOC_ROOT / "README.md", "\n".join(index))
    print(f"HELP_DOCUMENTS_GENERATED assets={len(asset_entries)} scenes={len(scene_entries)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
