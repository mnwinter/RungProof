"""Render a complete review package for the four-position selector station."""

from __future__ import annotations

import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "scene_core" / "rotary_selector_station"
SOURCE = ASSET / "source" / "rotary_selector_station.blend"
REVIEW = ASSET / "review"


def aim(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def material(name, color, roughness=.55):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1)
    value.metallic = .05
    value.roughness = roughness
    return value


def asset_bounds(objects):
    low = Vector((float("inf"),) * 3)
    high = Vector((float("-inf"),) * 3)
    for obj in objects:
        if obj.type not in {"MESH", "CURVE", "FONT"}:
            continue
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            low = Vector((min(low.x, world.x), min(low.y, world.y), min(low.z, world.z)))
            high = Vector((max(high.x, world.x), max(high.y, world.y), max(high.z, world.z)))
    return low, high


def render(scene, camera, floor, output, location, target, show_floor=True):
    floor.hide_render = not show_floor
    camera.location = location
    aim(camera, target)
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)


def main():
    REVIEW.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    scene = bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    visual = [obj for obj in scene.objects if obj.get("rungproof_asset")]
    low, high = asset_bounds(visual)
    center = (low + high) / 2

    bpy.ops.mesh.primitive_plane_add(size=8, location=(center.x, center.y, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(material("Review floor", (.07, .09, .11), .72))
    world = scene.world or bpy.data.worlds.new("Review world")
    scene.world = world
    world.color = (.015, .022, .032)
    for index, (offset, energy, size) in enumerate((((3, -4, 4), 850, 3.0), ((-3, -2, 2.5), 420, 2.2), ((1, 3, 3), 620, 2.8))):
        data = bpy.data.lights.new(f"Review light {index}", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(offset)
        aim(light, center)

    camera_data = bpy.data.cameras.new("Review camera")
    camera_data.lens = 58
    camera = bpy.data.objects.new("Review camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    front = center + Vector((1.0, -1.8, .58)) * 1.25
    render(scene, camera, floor, REVIEW / "hero.png", front, center)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")
    render(scene, camera, floor, REVIEW / "selector_dial_and_labels.png", center + Vector((.22, -1.3, .25)),
           Vector((0, -.20, 1.16)))
    render(scene, camera, floor, REVIEW / "pedestal_and_anchors.png", center + Vector((1.25, -1.05, -.35)),
           Vector((0, 0, .32)))
    render(scene, camera, floor, REVIEW / "rear_enclosure_and_gland.png", center + Vector((-.85, 1.35, .35)), center)
    # Source opens in its position-0/off detent.  Position 3 demonstrates the
    # opposite AUTO detent and proves the whole kinematic group travels.
    render(scene, camera, floor, REVIEW / "state_stopped.png", front, center)

    pivot = scene.objects["KIN_selector_handle"]
    pivot.rotation_euler.y = .960  # Position 3: +55 degrees in the front plane.
    render(scene, camera, floor, REVIEW / "state_auto.png", front, center)
    # The generic production-evidence contract calls its actuated witness
    # `state_running.png`; for this discrete operator that witness is the
    # position-3/AUTO detent rather than a continuously running machine.
    render(scene, camera, floor, REVIEW / "state_running.png", front, center)
    pivot.rotation_euler.y = 0

    bpy.ops.mesh.primitive_cube_add(size=1, location=(high.x + .60, center.y, low.z + .5))
    scale = bpy.context.object
    scale.data.materials.append(material("One metre reference", (.9, .18, .02), .38))
    render(scene, camera, floor, REVIEW / "scale_reference.png", center + Vector((1.1, -1.45, .50)),
           (center + scale.location) / 2)
    bpy.data.objects.remove(scale, do_unlink=True)

    wire = material("Review wire", (.02, .85, 1.0), .35)
    clones = []
    for obj in visual:
        if obj.type not in {"MESH", "CURVE", "FONT"}:
            continue
        obj.hide_render = True
        clone = obj.copy()
        clone.data = obj.data.copy()
        clone.data.materials.clear()
        clone.data.materials.append(wire)
        scene.collection.objects.link(clone)
        if clone.type == "MESH":
            modifier = clone.modifiers.new("Review wireframe", "WIREFRAME")
            modifier.thickness = .0025
        clones.append(clone)
    render(scene, camera, floor, REVIEW / "wireframe.png", front, center)
    for obj in visual:
        obj.hide_render = False
    for clone in clones:
        bpy.data.objects.remove(clone, do_unlink=True)

    shutil.copyfile(REVIEW / "hero.png", ASSET / "thumbnail.png")
    print(f"ROTARY_SELECTOR_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
