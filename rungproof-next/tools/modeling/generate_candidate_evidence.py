"""Render local evidence views for candidate assets without changing catalogs.

This creates local geometry/scale/wireframe evidence only. It deliberately does
not create blind-recognition or independent-acceptance records.
"""
from __future__ import annotations

import math
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "assets" / "scene_support"
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
TARGETS = {
    "liquid_metering_skid": "KIN_metering_pump_shaft",
    "two_position_container_receiver": "KIN_receiver_roller_left",
    "three_height_parcel_sensor_bank": None,
    "parking_barrier": "KIN_barrier_boom",
    "occupancy_display": None,
}


def point_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    low = Vector((1e9, 1e9, 1e9))
    high = Vector((-1e9, -1e9, -1e9))
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            low.x, low.y, low.z = min(low.x, world.x), min(low.y, world.y), min(low.z, world.z)
            high.x, high.y, high.z = max(high.x, world.x), max(high.y, world.y), max(high.z, world.z)
    return low, high


def render(source: Path, output: Path, kinematic_node: str | None) -> None:
    required = ["hero.png", "scale_reference.png", "wireframe.png"]
    if kinematic_node:
        required.extend(("state_stopped.png", "state_running.png"))
    if all((output / name).exists() for name in required):
        print(f"LOCAL_CANDIDATE_EVIDENCE_SKIP_COMPLETE {source.parent.parent.name}")
        return
    bpy.ops.wm.open_mainfile(filepath=str(source))
    objects = [o for o in bpy.context.scene.objects if o.type == "MESH" and o.get("rungproof_asset")]
    if not objects:
        objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not objects:
        raise RuntimeError(f"No mesh geometry found in {source}")
    low, high = bounds(objects)
    center = (low + high) / 2
    span = high - low
    radius = max(span.length * 1.45, 3.5)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    world = scene.world or bpy.data.worlds.new("Review World")
    scene.world = world
    world.color = (0.018, 0.024, 0.028)

    bpy.ops.mesh.primitive_cube_add(location=(center.x, center.y, low.z - .04))
    floor = bpy.context.object
    floor.name = "LOCAL_REVIEW_FLOOR"
    floor.dimensions = (max(span.x * 2.2, 4), max(span.y * 2.2, 4), .05)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    for i, (loc, energy, size) in enumerate((((4, -5, 6), 1300, 4), ((-4, -1, 3), 850, 3), ((0, 4, 5), 950, 3))):
        data = bpy.data.lights.new(f"LOCAL_REVIEW_LIGHT_{i}", "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(data.name, data)
        bpy.context.collection.objects.link(light)
        light.location = loc
        point_at(light, center)

    camera_data = bpy.data.cameras.new("LOCAL_REVIEW_CAMERA")
    camera = bpy.data.objects.new("LOCAL_REVIEW_CAMERA", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera_data.lens = 58
    camera.location = (center.x + radius * math.cos(math.radians(305)), center.y + radius * math.sin(math.radians(305)), center.z + radius * .38)
    point_at(camera, center)

    output.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(output / "hero.png")
    bpy.ops.render.render(write_still=True)

    # A one-metre cube is a physical scale witness, not a decorative label.
    bpy.ops.mesh.primitive_cube_add(location=(high.x + .65, center.y, .5))
    scale_cube = bpy.context.object
    scale_cube.name = "ONE_METRE_SCALE_WITNESS"
    scale_cube.dimensions = (1, 1, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    scene.render.filepath = str(output / "scale_reference.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(scale_cube, do_unlink=True)

    for obj in objects:
        obj.hide_render = False
        obj.display_type = "WIRE"
    scene.render.filepath = str(output / "wireframe.png")
    bpy.ops.render.render(write_still=True)
    for obj in objects:
        obj.display_type = "TEXTURED"

    if kinematic_node:
        pivot = bpy.data.objects.get(kinematic_node)
        if pivot:
            for angle, name in ((0, "state_stopped.png"), (math.radians(90), "state_running.png")):
                pivot.rotation_euler.x = angle
                scene.render.filepath = str(output / name)
                bpy.ops.render.render(write_still=True)

    floor.hide_render = True


def catalog_targets() -> dict[str, tuple[Path, Path, str | None]]:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    targets = {}
    for asset in catalog["assets"]:
        source = ROOT / asset["model"]["sourceBlend"].removeprefix("res://")
        asset_root = source.parent.parent
        review = asset_root / "review"
        node = None
        if asset.get("kinematics"):
            node = asset["kinematics"][0].get("nodePath")
            if node:
                node = node.rsplit("/", 1)[-1]
        targets[asset["id"]] = (source, review, node)
    return targets


if "--all" in sys.argv:
    targets = catalog_targets()
else:
    targets = {
        f"scene_support.{slug}": (
            ASSETS / slug / "source" / f"{slug}.blend",
            ASSETS / slug / "review",
            node,
        )
        for slug, node in TARGETS.items()
    }

rendered = 0
for asset_id, (source, output, kinematic_node) in targets.items():
    if not source.exists():
        print(f"LOCAL_CANDIDATE_EVIDENCE_SKIP {asset_id}: missing {source}")
        continue
    render(source, output, kinematic_node)
    rendered += 1
print(f"LOCAL_CANDIDATE_EVIDENCE_RENDERED {rendered}")
