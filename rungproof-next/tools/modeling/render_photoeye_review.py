"""Render an inspectable review package for the source through-beam photoeye.

This intentionally opens the editable Blender source rather than the delivery
GLB, so a review can expose source topology, mounts, and cable routing before
catalog promotion. It does not save the source file.
"""

from __future__ import annotations

import math
import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET_ROOT = ROOT / "assets" / "scene_core" / "through_beam_photoeye"
SOURCE = ASSET_ROOT / "source" / "through_beam_photoeye.blend"
REVIEW = ASSET_ROOT / "review"


def aim(camera: bpy.types.Object, target: Vector) -> None:
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()


def principled_material(name: str, color: tuple[float, float, float], roughness: float) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = roughness
    return material


def bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    minimum = Vector((float("inf"),) * 3)
    maximum = Vector((float("-inf"),) * 3)
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            minimum = Vector((min(minimum.x, world.x), min(minimum.y, world.y), min(minimum.z, world.z)))
            maximum = Vector((max(maximum.x, world.x), max(maximum.y, world.y), max(maximum.z, world.z)))
    return minimum, maximum


def render(scene: bpy.types.Scene, camera: bpy.types.Object, path: Path,
           location: Vector, target: Vector, floor: bpy.types.Object | None,
           show_floor: bool = True) -> None:
    if floor is not None:
        floor.hide_render = not show_floor
    camera.location = location
    aim(camera, target)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main() -> None:
    REVIEW.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    scene = bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    visual = [obj for obj in scene.objects if obj.type in {"MESH", "CURVE"}]
    minimum, maximum = bounds(visual)
    center = (minimum + maximum) / 2
    extent = maximum - minimum
    radius = max(extent.length * 1.35, 2.8)

    bpy.ops.mesh.primitive_plane_add(size=max(radius * 4, 6), location=(center.x, center.y, minimum.z - 0.025))
    floor = bpy.context.object
    floor.name = "REVIEW_floor"
    floor_material = principled_material("Review floor", (0.075, 0.095, 0.115), 0.72)
    floor.data.materials.append(floor_material)

    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.color = (0.018, 0.025, 0.035)
    for index, (offset, energy, size) in enumerate((
        ((1.4, -1.6, 1.9), 900, 3.2),
        ((-1.3, -0.8, 1.2), 550, 2.2),
        ((0.3, 1.8, 1.6), 700, 2.6),
    )):
        data = bpy.data.lights.new(f"REVIEW_light_{index}", "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(offset)
        aim(light, center)

    camera_data = bpy.data.cameras.new("REVIEW_camera")
    camera_data.lens = 56
    camera = bpy.data.objects.new("REVIEW_camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    view_data = {
        "hero.png": (Vector((1.15, -1.20, 0.82)), center, True),
        "emitter_face.png": (Vector((0.24, -0.98, 0.24)), Vector((0, minimum.y + 0.06, 0.92)), True),
        "receiver_face.png": (Vector((-0.24, 0.98, 0.24)), Vector((0, maximum.y - 0.06, 0.92)), True),
        "rear_cable_mount.png": (Vector((0.84, 0.94, 0.38)), Vector((0, maximum.y - 0.03, 0.70)), True),
        "underside.png": (Vector((0.92, -0.78, -1.45)), Vector((0, 0, minimum.z + 0.16)), False),
        "side_alignment.png": (Vector((1.72, 0.0, 0.36)), center, True),
    }
    for name, (direction, target, show_floor) in view_data.items():
        render(scene, camera, REVIEW / name, center + direction * radius, target, floor, show_floor)

    # Context-free copy for blind recognition. The file name intentionally does
    # not disclose the family to a reviewer.
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(maximum.x + 0.72, center.y, minimum.z + 0.5))
    scale_cube = bpy.context.object
    scale_cube.name = "REVIEW_one_meter_reference"
    scale_material = principled_material("Review reference orange", (0.85, 0.14, 0.015), 0.36)
    scale_cube.data.materials.append(scale_material)
    render(scene, camera, REVIEW / "scale_reference.png", center + Vector((1.25, -1.25, 0.82)) * radius,
           (center + scale_cube.location) / 2, floor, True)
    bpy.data.objects.remove(scale_cube, do_unlink=True)

    wire_material = bpy.data.materials.new("Review wireframe")
    wire_material.diffuse_color = (0.05, 0.8, 0.95, 1)
    wire_material.metallic = 0.1
    wire_material.roughness = 0.35
    wire_copies: list[bpy.types.Object] = []
    for obj in visual:
        obj.hide_render = True
        if obj.type != "MESH":
            continue
        clone = obj.copy()
        clone.data = obj.data.copy()
        clone.name = f"REVIEW_wire_{obj.name}"
        scene.collection.objects.link(clone)
        clone.hide_render = False
        clone.data.materials.clear()
        clone.data.materials.append(wire_material)
        modifier = clone.modifiers.new("Review wireframe", "WIREFRAME")
        modifier.thickness = 0.003
        wire_copies.append(clone)
    render(scene, camera, REVIEW / "wireframe.png", center + Vector((1.15, -1.20, 0.82)) * radius,
           center, floor, True)
    for obj in visual:
        obj.hide_render = False
    for clone in wire_copies:
        bpy.data.objects.remove(clone, do_unlink=True)

    shutil.copyfile(REVIEW / "hero.png", ASSET_ROOT / "thumbnail.png")
    print(f"PHOTOEYE_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
