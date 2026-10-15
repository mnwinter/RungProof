"""Migrate RungProof's original authored scenes into the Godot scene catalog.

The source files remain authoritative and untouched.  The generated copies add
only migration metadata, so training, simulation, verification, and PLC profile
contracts survive byte-for-byte as JSON values.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[2]
SOURCE = PROJECT / "prototype" / "scenes"
DESTINATION = PROJECT / "rungproof-next" / "scenes" / "migrated"
CATALOG = PROJECT / "rungproof-next" / "scenes" / "catalog" / "original-scenes.catalog.json"

ASSET_MAPPINGS = {
    "conveyor": "material-handling.belt-conveyor.600x6000.v1",
    "photoeye": "sensing.photoelectric.through-beam.v1",
    "switch": "controls.operator-station.single-pushbutton.v1",
    "indicator": "controls.beacon.single-tier.v1",
    "pusher": "actuation.pneumatic-pusher.1350mm.v1",
    "motor": "drives.motor.ac-induction.v1",
    "pipe": "process.pipe.flanged-spool.v1",
    "tank": "process.tank.vertical-3000x5000.v1",
    "pump": "process.pump.centrifugal-skid.v1",
    "valve": "process.valve.actuated-ball.v1",
    "levelSensor": "sensing.level.analog-4-20ma.v1",
    "radarLevelSensor": "sensing.level.radar.v1",
    "rotarySwitch": "controls.operator-station.selector.v1",
    "fan": "air-handling.fan.axial-1900.v1",
    "liftTable": "material-handling.lift.scissor-table.v1",
    "drillPress": "machining.drill-press.pedestal.v1",
    "robotArm": "robotics.robot.six-axis-medium.v1",
    "rotaryTable": "material-handling.table.powered-rotary.v1",
    "rollerShutter": "access-control.door.roller-shutter.v1",
    "machine": "machining.machine.enclosed-center.v1",
    # Reviewed scene-specific assemblies rendered by SceneComposer. These IDs
    # are explicit procedural mappings until each assembly is promoted into a
    # standalone production-catalog GLB; they must not fall back to a generic
    # machine model or be reported as unresolved.
    "palletLoad": "loads.palletized-cases.gma-48x40.v1",
    "toteFiller": "process.packaging.filler.tote-volumetric.v1",
    "toteCapper": "process.packaging.capper.tote-inline.v1",
    "toteLabeler": "process.packaging.labeler.tote-pressure-sensitive.v1",
    "toteVision": "inspection.vision.tote-multicamera.v1",
    "meteringSkid": "process.dosing.skid.liquid-metering.v1",
    "containerReceiver": "material-handling.receiver.container-two-position.v1",
    "sizeSensorBank": "sensing.dimensioning.parcel-three-height.v1",
}

# These are scene dressing/load shapes, not reusable equipment assets.  They
# are intentionally rendered by the scene composer and are not promoted into
# the enterprise asset catalog.
SCENE_PROPS = {"box"}


def canonical(document: dict[str, object]) -> str:
    return json.dumps(document, sort_keys=True, separators=(",", ":"))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--accept-source",
        action="store_true",
        help="Explicitly replace divergent migrated copies with authoritative source edits.",
    )
    parser.add_argument(
        "--accept-scene",
        action="append",
        default=[],
        metavar="SCENE_ID",
        help="Replace one intentionally repaired scene while preserving all other reviewed native scenes.",
    )
    args = parser.parse_args()
    DESTINATION.mkdir(parents=True, exist_ok=True)
    CATALOG.parent.mkdir(parents=True, exist_ok=True)

    entries: list[dict[str, object]] = []
    type_counts: Counter[str] = Counter()

    source_files = sorted((*SOURCE.glob("*.json"), *SOURCE.glob("*.plcscene")))
    for source_path in source_files:
        document = json.loads(source_path.read_text(encoding="utf-8"))
        scene_id = document["id"]
        equipment_types = sorted({item["type"] for item in document.get("equipment", [])})
        type_counts.update(item["type"] for item in document.get("equipment", []))
        unresolved = sorted(
            equipment_type
            for equipment_type in equipment_types
            if equipment_type not in ASSET_MAPPINGS and equipment_type not in SCENE_PROPS
        )

        migrated_name = f"{scene_id}.scene.json"
        document["migration"] = {
            "sourceFile": f"prototype/scenes/{source_path.name}",
            "sourceFormat": source_path.suffix.lstrip("."),
            "assetMappings": ASSET_MAPPINGS,
            "scenePropTypes": sorted(SCENE_PROPS),
            "unresolvedAssetTypes": unresolved,
            "visualStatus": "partial" if unresolved else "mapped",
            "productionApproved": False,
            "reason": "Production approval requires every reusable asset and the complete scene to pass visual review.",
        }
        output_path = DESTINATION / migrated_name
        catalog_document = document
        if output_path.exists():
            reviewed = json.loads(output_path.read_text(encoding="utf-8"))
            reviewed_without_metadata = dict(reviewed)
            reviewed_without_metadata.pop("migration", None)
            source_document = dict(document)
            source_document.pop("migration", None)
            accept_this_scene = args.accept_source or scene_id in args.accept_scene
            if canonical(reviewed_without_metadata) != canonical(source_document) and not accept_this_scene:
                # A native scene may contain reviewed transforms or contract repairs
                # that intentionally diverge from the legacy prototype.  Preserve it
                # and continue migrating missing scenes instead of aborting the whole
                # catalog at the first reviewed file.
                catalog_document = reviewed
            else:
                output_path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
        else:
            output_path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")

        catalog_equipment_types = sorted(
            {item["type"] for item in catalog_document.get("equipment", [])}
        )
        catalog_unresolved = sorted(
            equipment_type
            for equipment_type in catalog_equipment_types
            if equipment_type not in ASSET_MAPPINGS and equipment_type not in SCENE_PROPS
        )

        entries.append(
            {
                "id": scene_id,
                "name": catalog_document.get("name", scene_id),
                "path": f"res://scenes/migrated/{migrated_name}",
                "sourceFile": f"prototype/scenes/{source_path.name}",
                "equipmentCount": len(catalog_document.get("equipment", [])),
                "equipmentTypes": catalog_equipment_types,
                "unresolvedAssetTypes": catalog_unresolved,
                "visualStatus": "partial" if catalog_unresolved else "mapped",
            }
        )

    catalog = {
        "version": 1,
        "catalogId": "rungproof-original-scenes-migrated",
        "sourceRoot": "prototype/scenes",
        "sceneCount": len(entries),
        "equipmentInstanceCount": sum(type_counts.values()),
        "equipmentTypeCounts": dict(sorted(type_counts.items())),
        "scenes": entries,
    }
    CATALOG.write_text(json.dumps(catalog, indent=2) + "\n", encoding="utf-8")
    print(f"MIGRATED_SCENES {len(entries)}")
    print(f"MIGRATED_EQUIPMENT {sum(type_counts.values())}")
    print(f"CATALOG {CATALOG}")


if __name__ == "__main__":
    main()
