"""Render acceptance evidence for a one-operator pedestal command station."""
from __future__ import annotations

import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET_NAME = os.environ.get("RUNGPROOF_REVIEW_ASSET", "single_pushbutton_station")
KIN_NODE = os.environ.get("RUNGPROOF_REVIEW_KIN_NODE", "KIN_pushbutton")
ASSET = ROOT / "assets" / "scene_core" / ASSET_NAME
SOURCE = ASSET / "source" / f"{ASSET_NAME}.blend"
REVIEW = ASSET / "review"


def aim(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def material(name, color, roughness=.55):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1)
    value.metallic = .05
    value.roughness = roughness
    return value


def bounds(objects):
    low = Vector((float("inf"),) * 3)
    high = Vector((float("-inf"),) * 3)
    for obj in objects:
        if obj.type not in {"MESH", "CURVE", "FONT"}:
            continue
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            low = Vector((min(low.x, point.x), min(low.y, point.y), min(low.z, point.z)))
            high = Vector((max(high.x, point.x), max(high.y, point.y), max(high.z, point.z)))
    return low, high


def render(scene, camera, floor, path, location, target, show_floor=True):
    floor.hide_render = not show_floor
    camera.location = location
    aim(camera, target)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    REVIEW.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    scene = bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    visual = [obj for obj in scene.objects if obj.get("rungproof_asset")]
    low, high = bounds(visual)
    center = (low + high) / 2

    bpy.ops.mesh.primitive_plane_add(size=8, location=(center.x, center.y, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(material("Review floor", (.07, .09, .11), .72))
    world = scene.world or bpy.data.worlds.new("Review world")
    scene.world = world
    world.color = (.015, .022, .032)
    for index, (offset, energy, size) in enumerate((((3, -4, 4), 720, 2.8), ((-3, -2, 3), 440, 2.2), ((1, 4, 3), 560, 2.4))):
        data = bpy.data.lights.new(f"Review light {index}", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(offset)
        aim(light, center)

    camera_data = bpy.data.cameras.new("Review camera")
    # Catalog evidence must be close enough to inspect controls and mounting
    # details without using a crop to conceal the full supported assembly.
    camera_data.lens = 70
    camera = bpy.data.objects.new("Review camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    hero = center + Vector((2.1, -3.4, 1.85))
    target = center + Vector((0, -.08, .05))
    render(scene, camera, floor, REVIEW / "hero.png", hero, target)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")
    render(scene, camera, floor, REVIEW / "button_and_legend.png", center + Vector((.55, -1.40, .40)), Vector((0, -.20, .76)))
    render(scene, camera, floor, REVIEW / "pedestal_and_base.png", center + Vector((1.65, -2.15, .05)), Vector((0, 0, .26)))
    render(scene, camera, floor, REVIEW / "rear_and_entry.png", center + Vector((-1.45, 2.15, .50)), Vector((0, .06, .36)))

    # This is an offline visual witness only: it verifies the declared operator
    # travel, not a connected PLC signal or a safety function.
    render(scene, camera, floor, REVIEW / "state_stopped.png", hero, target)
    button = scene.objects[KIN_NODE]
    button.location.y += .018
    render(scene, camera, floor, REVIEW / "state_running.png", hero, target)
    button.location.y -= .018

    bpy.ops.mesh.primitive_cube_add(size=1, location=(high.x + .75, center.y, low.z + .5))
    scale = bpy.context.object
    scale.data.materials.append(material("One metre reference", (.9, .18, .02), .38))
    render(scene, camera, floor, REVIEW / "scale_reference.png", center + Vector((2.3, -3.0, 1.55)), (center + scale.location) / 2)
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
    render(scene, camera, floor, REVIEW / "wireframe.png", hero, target)
    for obj in visual:
        obj.hide_render = False
    for clone in clones:
        bpy.data.objects.remove(clone, do_unlink=True)

    shutil.copyfile(REVIEW / "hero.png", ASSET / "thumbnail.png")
    print(f"PEDESTAL_OPERATOR_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
