"""Render an inspectable stopped/extended review package for the pusher source.

The source file is opened read-only.  The extended frame is created in-memory
using the same 1.35 m kinematic contract as the Godot motion controller.
"""

from __future__ import annotations

import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET_ROOT = ROOT / "assets" / "scene_core" / "pneumatic_pusher"
SOURCE = ASSET_ROOT / "source" / "pneumatic_pusher.blend"
REVIEW = ASSET_ROOT / "review"
STROKE_M = 1.35


def aim(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def material(name: str, color: tuple[float, float, float], roughness: float) -> bpy.types.Material:
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = roughness
    return value


def bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    low, high = Vector((float("inf"),) * 3), Vector((float("-inf"),) * 3)
    for obj in objects:
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            low = Vector((min(low.x, point.x), min(low.y, point.y), min(low.z, point.z)))
            high = Vector((max(high.x, point.x), max(high.y, point.y), max(high.z, point.z)))
    return low, high


def render(scene: bpy.types.Scene, camera: bpy.types.Object, path: Path, location: Vector,
           target: Vector, floor: bpy.types.Object, show_floor: bool = True) -> None:
    floor.hide_render = not show_floor
    camera.location = location
    aim(camera, target)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def set_extension(value: float) -> None:
    for obj in bpy.context.scene.objects:
        if not obj.name.startswith("KIN_pusher_"):
            continue
        if obj.name.startswith("KIN_pusher_rod"):
            obj.location.x += value * 0.5
            obj.scale.x *= 1.0 + value / STROKE_M
        else:
            obj.location.x += value


def main() -> None:
    REVIEW.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    scene = bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    visual = [obj for obj in scene.objects if obj.type == "MESH"]
    low, high = bounds(visual)
    center, extent = (low + high) / 2, high - low
    # Keep the machinery large enough to inspect; the floor is deliberately
    # much larger than the asset and must not influence this framing radius.
    radius = max(extent.length * .72, 3.2)

    bpy.ops.mesh.primitive_plane_add(size=radius * 4, location=(center.x, center.y, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(material("Review floor", (.055, .070, .090), .74))
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.color = (.012, .018, .028)
    for i, (offset, energy, size) in enumerate((((3.0, -3.5, 4.0), 1300, 4.0),
                                                  ((-2.4, -2.0, 2.4), 900, 3.0),
                                                  ((1.0, 3.0, 2.5), 1000, 3.4))):
        data = bpy.data.lights.new(f"REVIEW_light_{i}", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        lamp = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(lamp)
        lamp.location = center + Vector(offset)
        aim(lamp, center)
    data = bpy.data.cameras.new("REVIEW_camera")
    data.lens = 58
    camera = bpy.data.objects.new("REVIEW_camera", data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    # Blender 5.2 keeps the Eevee identifier as BLENDER_EEVEE.
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    views = {
        "hero.png": (Vector((1.25, -1.35, .78)), center, True),
        "cylinder_and_valve.png": (Vector((-.65, -1.10, .45)), Vector((-.45, -.05, .68)), True),
        "carriage_and_guide.png": (Vector((1.15, -.90, .42)), Vector((1.04, 0, .70)), True),
        "transfer_alignment.png": (Vector((2.0, -.05, .55)), Vector((1.65, 0, .52)), True),
        "underside.png": (Vector((.85, -1.0, -1.20)), Vector((.62, 0, .18)), False),
    }
    for name, (direction, target, show_floor) in views.items():
        render(scene, camera, REVIEW / name, center + direction * radius, target, floor, show_floor)
    render(scene, camera, REVIEW / "state_stopped.png", center + Vector((1.25, -1.35, .78)) * radius,
           center, floor)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(high.x + .72, center.y, low.z + .5))
    cube = bpy.context.object
    cube.data.materials.append(material("One metre reference", (.92, .23, .02), .32))
    render(scene, camera, REVIEW / "scale_reference.png", center + Vector((1.3, -1.25, .75)) * radius,
           (center + cube.location) / 2, floor)
    bpy.data.objects.remove(cube, do_unlink=True)

    wire = material("Review wireframe", (.03, .82, .98), .30)
    copies: list[bpy.types.Object] = []
    for obj in visual:
        obj.hide_render = True
        clone = obj.copy()
        clone.data = obj.data.copy()
        scene.collection.objects.link(clone)
        clone.hide_render = False
        clone.data.materials.clear()
        clone.data.materials.append(wire)
        modifier = clone.modifiers.new("Review wireframe", "WIREFRAME")
        modifier.thickness = .003
        copies.append(clone)
    render(scene, camera, REVIEW / "wireframe.png", center + Vector((1.25, -1.35, .78)) * radius,
           center, floor)
    for clone in copies:
        bpy.data.objects.remove(clone, do_unlink=True)
    for obj in visual:
        obj.hide_render = False

    set_extension(STROKE_M)
    render(scene, camera, REVIEW / "state_running.png", center + Vector((1.25, -1.35, .78)) * radius,
           Vector((1.35, 0, .55)), floor)
    shutil.copyfile(REVIEW / "hero.png", ASSET_ROOT / "thumbnail.png")
    print(f"PUSHER_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
