"""Execute every migrated scene's authored verification cases in Godot."""
from __future__ import annotations

import argparse
import json
import math
import os
import subprocess
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "scenes" / "catalog" / "original-scenes.catalog.json"
OUTPUT = ROOT / "build" / "scene-contract-verification"


def verify_catalog_metadata(entry: dict, scene: dict) -> None:
    """Keep scene-browser metadata synchronized with the actual contract."""
    equipment = scene.get("equipment", [])
    actual_types = {item["type"] for item in equipment}
    if entry.get("equipmentCount") != len(equipment):
        raise ValueError(f"{entry['id']}: catalog equipmentCount differs from scene contract")
    declared_types = entry.get("equipmentTypes", [])
    if set(declared_types) != actual_types or len(declared_types) != len(actual_types):
        raise ValueError(f"{entry['id']}: catalog equipmentTypes differs from scene contract")


def verify_visual_layout(scene_id: str, scene: dict[str, object]) -> None:
    """Reject authored layouts that deliberately stack peer controls in depth.

    Simulation contracts prove point behavior but cannot see a camera-facing
    occlusion mistake. Scenes may declare minimum axis separation for controls
    whose simultaneous visibility is part of the training exercise.
    """
    equipment = {
        str(item["id"]): item
        for item in scene.get("equipment", [])
        if isinstance(item, dict) and isinstance(item.get("id"), str)
    }
    layout = scene.get("visualLayout", {})
    contracts = layout.get("separationContracts", []) if isinstance(layout, dict) else []
    axis_index = {"x": 0, "y": 1, "z": 2}
    for contract in contracts:
        if not isinstance(contract, dict):
            raise ValueError(f"{scene_id}: visual layout contract must be an object")
        ids = contract.get("equipmentIds", [])
        axis = contract.get("axis")
        minimum = contract.get("minimumSeparationM")
        if (not isinstance(ids, list) or len(ids) != 2 or axis not in axis_index
                or not isinstance(minimum, (int, float)) or minimum <= 0):
            raise ValueError(f"{scene_id}: invalid visual layout separation contract")
        try:
            positions = [equipment[str(item_id)]["position"] for item_id in ids]
            separation = abs(float(positions[0][axis_index[axis]]) - float(positions[1][axis_index[axis]]))
        except (KeyError, IndexError, TypeError, ValueError) as error:
            raise ValueError(f"{scene_id}: invalid visual layout equipment reference") from error
        if separation < minimum:
            raise ValueError(
                f"{scene_id}: {ids[0]} and {ids[1]} are only {separation:g} m apart on {axis}; "
                f"minimum is {minimum:g} m"
            )


def verify_photoeye_product_envelope(scene_id: str, scene: dict[str, object]) -> None:
    """Require a conveyor photoeye's beam to pass through its declared product.

    A working boolean transition is not evidence that a modeled beam is placed
    physically. The optical centerline must sit above the conveyor deck and
    inside every product's vertical envelope at that inspection station.
    """
    simulation = scene.get("simulation", {})
    if not isinstance(simulation, dict) or not simulation.get("photoeyeId"):
        return
    equipment = {
        str(item["id"]): item
        for item in scene.get("equipment", [])
        if isinstance(item, dict) and isinstance(item.get("id"), str)
    }
    photoeye_id = str(simulation["photoeyeId"])
    photoeye = equipment.get(photoeye_id)
    if photoeye is None:
        raise ValueError(f"{scene_id}: photoeyeId '{photoeye_id}' is not equipment")
    config = photoeye.get("config", {})
    if not isinstance(config, dict) or not isinstance(config.get("beamCenterHeightM"), (int, float)):
        raise ValueError(f"{scene_id}: photoeye '{photoeye_id}' requires numeric beamCenterHeightM")
    # The two lens elevations can differ. Check the beam where the product
    # travels, rather than comparing the lower lens to the belt elevation.
    # This authored-envelope check supports unrotated, unscaled sensor roots;
    # transformed installations need a world-space geometry audit instead.
    if (any(float(value) != 0 for value in photoeye.get("rotation", [0, 0, 0]))
            or any(float(value) != 1 for value in photoeye.get("scale", [1, 1, 1]))):
        raise ValueError(f"{scene_id}: transformed photoeye requires a world-space product-envelope audit")
    sensor_position = photoeye.get("position", [0, 0, 0])
    lower_height = float(config["beamCenterHeightM"])
    upper_height = float(config.get("positiveBeamCenterHeightM", lower_height))
    negative_stand = -float(config.get("span", 1.44)) / 2
    positive_stand = float(config.get("positiveStandPositionM", -negative_stand))
    if not math.isfinite(negative_stand) or not math.isfinite(positive_stand) or positive_stand <= negative_stand:
        raise ValueError(f"{scene_id}: photoeye requires finite separated stand positions")
    conveyor = equipment.get(str(simulation.get("conveyorId", "")))
    deck = None
    if conveyor is not None:
        conveyor_config = conveyor.get("config", {})
        if isinstance(conveyor_config, dict) and isinstance(conveyor_config.get("deckHeight"), (int, float)):
            deck = float(conveyor_config["deckHeight"])
    product_ids = simulation.get("productIds", [])
    if not product_ids and isinstance(simulation.get("productId"), str):
        product_ids = [simulation["productId"]]
    if not isinstance(product_ids, list) or not product_ids:
        raise ValueError(f"{scene_id}: conveyor photoeye has no declared products to validate")
    for product_id in product_ids:
        product = equipment.get(str(product_id))
        if product is None:
            raise ValueError(f"{scene_id}: product '{product_id}' is not equipment")
        position = product.get("position", [])
        product_config = product.get("config", {})
        size = product_config.get("size", []) if isinstance(product_config, dict) else []
        if (not isinstance(position, list) or len(position) < 3
                or not isinstance(size, list) or len(size) < 2):
            raise ValueError(f"{scene_id}: product '{product_id}' has no vertical envelope")
        bottom = float(position[1])
        top = bottom + float(size[1])
        local_z = float(position[2]) - float(sensor_position[2])
        if not negative_stand < local_z < positive_stand:
            raise ValueError(f"{scene_id}: product '{product_id}' travels outside the photoeye stand span")
        centerline = (float(sensor_position[1]) + lower_height
                      + (upper_height - lower_height) * (local_z - negative_stand) / (positive_stand - negative_stand))
        if deck is not None and centerline <= deck:
            raise ValueError(f"{scene_id}: photoeye beam {centerline:g} m is not above conveyor deck {deck:g} m")
        if not bottom < centerline < top:
            raise ValueError(
                f"{scene_id}: photoeye beam {centerline:g} m misses product '{product_id}' envelope "
                f"({bottom:g}–{top:g} m)"
            )


def verify_all_photoeye_geometry(scene_id: str, scene: dict[str, object]) -> None:
    """Require every through-beam asset to declare a physical beam elevation.

    A default model height is not an authored installation.  Scenes without a
    conveyor sequence still need an explicit optical centreline so a later
    layout change cannot quietly place the beam through a rail or below a load.
    """
    for equipment in scene.get("equipment", []):
        if not isinstance(equipment, dict) or equipment.get("type") != "photoeye":
            continue
        config = equipment.get("config", {})
        height = config.get("beamCenterHeightM") if isinstance(config, dict) else None
        # Match the compositor's floor-mounted stand minimum. An upper-level
        # conveyor can require a beam above 2.5 m (the chain lift uses 3.2 m);
        # a universal ceiling cannot establish whether that beam meets its load.
        # Product-envelope and installation audits make that separate check.
        if (isinstance(height, bool) or not isinstance(height, (int, float))
                or not math.isfinite(height) or height < 0.3):
            raise ValueError(
                f"{scene_id}: through-beam '{equipment.get('id', '?')}' requires a finite "
                "numeric beamCenterHeightM >= 0.3 m"
            )
        if "positiveBeamCenterHeightM" in config:
            positive_height = config["positiveBeamCenterHeightM"]
            if (isinstance(positive_height, bool) or not isinstance(positive_height, (int, float))
                    or not math.isfinite(positive_height) or positive_height < 0.3):
                raise ValueError(f"{scene_id}: positiveBeamCenterHeightM must be finite numeric >= 0.3 m")


def verify_peer_controls_are_not_depth_stacked(scene_id: str, scene: dict[str, object]) -> None:
    """Reject separate peer controls/lights that differ only in camera depth.

    Independent pushbutton stations and single-tier lamps are not a layered
    panel.  If peers share X and are separated only on Z, the default operator
    view reads them as objects in front of each other.  Author them inline on X
    or model an intentional shared stack assembly instead.
    """
    equipment = [item for item in scene.get("equipment", []) if isinstance(item, dict)]
    for equipment_types, peer_label in (({"switch", "rotarySwitch"}, "control"), ({"indicator"}, "indicator")):
        peers = [item for item in equipment if item.get("type") in equipment_types]
        for index, first in enumerate(peers):
            for second in peers[index + 1:]:
                first_position = first.get("position", [])
                second_position = second.get("position", [])
                if (not isinstance(first_position, list) or len(first_position) < 3
                        or not isinstance(second_position, list) or len(second_position) < 3):
                    raise ValueError(f"{scene_id}: {peer_label} requires 3D positions")
                try:
                    x_separation = abs(float(first_position[0]) - float(second_position[0]))
                    z_separation = abs(float(first_position[2]) - float(second_position[2]))
                except (TypeError, ValueError) as error:
                    raise ValueError(f"{scene_id}: {peer_label} position must be numeric") from error
                if x_separation < 0.35 and z_separation >= 0.35:
                    raise ValueError(
                        f"{scene_id}: {peer_label} peers '{first.get('id')}' and "
                        f"'{second.get('id')}' are depth-stacked; arrange inline or model one assembly"
                    )


def verify_cross_conveyor_pusher(scene_id: str, scene: dict[str, object]) -> None:
    """Require a conveyor pusher's stroke to move across, and toward, its belt.

    Pneumatic pusher models stroke along their local +X axis.  A scene root
    yaw must rotate that axis across the conveyor rather than letting the rod
    travel with the product.  The pusher also has to aim from its installed
    side toward the belt centreline, not away from it.
    """
    simulation = scene.get("simulation", {})
    if not isinstance(simulation, dict) or not simulation.get("pusherId"):
        return
    equipment = {
        str(item["id"]): item
        for item in scene.get("equipment", [])
        if isinstance(item, dict) and isinstance(item.get("id"), str)
    }
    pusher_id = str(simulation["pusherId"])
    conveyor_id = str(simulation.get("conveyorId", ""))
    pusher = equipment.get(pusher_id)
    conveyor = equipment.get(conveyor_id)
    if pusher is None or conveyor is None:
        raise ValueError(f"{scene_id}: pusher/conveyor reference is not equipment")
    if pusher.get("type") != "pusher":
        raise ValueError(f"{scene_id}: pusherId '{pusher_id}' is not a pusher")
    rotation = pusher.get("rotation", [])
    pusher_position = pusher.get("position", [])
    conveyor_position = conveyor.get("position", [])
    if (not isinstance(rotation, list) or len(rotation) < 2
            or not isinstance(pusher_position, list) or len(pusher_position) < 3
            or not isinstance(conveyor_position, list) or len(conveyor_position) < 3):
        raise ValueError(f"{scene_id}: pusher requires rotation and 3D positions")
    try:
        yaw = math.radians(float(rotation[1]))
        pusher_z = float(pusher_position[2])
        conveyor_z = float(conveyor_position[2])
    except (TypeError, ValueError) as error:
        raise ValueError(f"{scene_id}: pusher rotation/position must be numeric") from error

    # Godot yaw rotates local +X to (cos(yaw), 0, -sin(yaw)).
    stroke_x = math.cos(yaw)
    stroke_z = -math.sin(yaw)
    if abs(stroke_z) < 0.9 or abs(stroke_x) > 0.2:
        raise ValueError(f"{scene_id}: pusher '{pusher_id}' stroke is not cross-conveyor")
    if (conveyor_z - pusher_z) * stroke_z <= 0:
        raise ValueError(f"{scene_id}: pusher '{pusher_id}' is aimed away from the conveyor")


def verify_pusher_travel_axis(scene_id: str, scene: dict[str, object]) -> None:
    """Keep each pneumatic pusher's local stroke aligned to its declared path."""
    for pusher in (item for item in scene.get("equipment", [])
                   if isinstance(item, dict) and item.get("type") == "pusher"):
        config = pusher.get("config", {})
        rotation = pusher.get("rotation", [])
        axis = config.get("travelAxis") if isinstance(config, dict) else None
        if axis not in {"x", "z"} or not isinstance(rotation, list) or len(rotation) < 2:
            raise ValueError(f"{scene_id}: pusher '{pusher.get('id', '?')}' requires travelAxis and yaw")
        try:
            yaw = math.radians(float(rotation[1]))
        except (TypeError, ValueError) as error:
            raise ValueError(f"{scene_id}: pusher yaw must be numeric") from error
        stroke_x, stroke_z = math.cos(yaw), -math.sin(yaw)
        along_path = abs(stroke_x) if axis == "x" else abs(stroke_z)
        across_path = abs(stroke_z) if axis == "x" else abs(stroke_x)
        if along_path < 0.9 or across_path > 0.2:
            raise ValueError(f"{scene_id}: pusher '{pusher.get('id')}' yaw does not match travelAxis '{axis}'")


def verify(godot: Path, scene_id: str) -> dict[str, object]:
    completed = subprocess.run(
        [
            str(godot),
            "--headless",
            "--path",
            str(ROOT),
            "--",
            f"--scene-id={scene_id}",
            "--verify-scene-contract",
        ],
        cwd=ROOT,
        env=os.environ.copy(),
        text=True,
        encoding="utf-8",
        errors="replace",
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        timeout=90,
        check=False,
    )
    log_path = OUTPUT / f"{scene_id}.log"
    log_path.write_text(completed.stdout, encoding="utf-8")
    passed = completed.returncode == 0 and "SCENE_CONTRACT_RESULT PASS" in completed.stdout
    return {
        "sceneId": scene_id,
        "passed": passed,
        "exitCode": completed.returncode,
        "log": str(log_path.relative_to(ROOT)).replace("\\", "/"),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--godot", type=Path, required=True)
    parser.add_argument("--workers", type=int, default=4)
    args = parser.parse_args()
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    scene_ids: list[str] = []
    for entry in catalog["scenes"]:
        scene_path = ROOT / entry["path"].removeprefix("res://")
        scene = json.loads(scene_path.read_text(encoding="utf-8"))
        verify_catalog_metadata(entry, scene)
        verify_visual_layout(entry["id"], scene)
        verify_all_photoeye_geometry(entry["id"], scene)
        verify_peer_controls_are_not_depth_stacked(entry["id"], scene)
        verify_photoeye_product_envelope(entry["id"], scene)
        verify_cross_conveyor_pusher(entry["id"], scene)
        verify_pusher_travel_axis(entry["id"], scene)
        if scene.get("verification", {}).get("cases"):
            scene_ids.append(entry["id"])

    OUTPUT.mkdir(parents=True, exist_ok=True)
    results: list[dict[str, object]] = []
    with ThreadPoolExecutor(max_workers=max(1, args.workers)) as executor:
        futures = {executor.submit(verify, args.godot.resolve(), scene_id): scene_id for scene_id in scene_ids}
        for future in as_completed(futures):
            result = future.result()
            results.append(result)
            print(f"SCENE_CONTRACT {result['sceneId']} {'PASS' if result['passed'] else 'FAIL'}")

    results.sort(key=lambda item: str(item["sceneId"]))
    passed = sum(bool(item["passed"]) for item in results)
    summary = {"sceneCount": len(results), "passed": passed, "failed": len(results) - passed, "results": results}
    (OUTPUT / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(f"SCENE_CONTRACTS_PASS {passed}")
    print(f"SCENE_CONTRACTS_FAIL {len(results) - passed}")
    return 0 if passed == len(results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
