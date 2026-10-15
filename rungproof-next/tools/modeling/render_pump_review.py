"""Render production evidence for the corrected end-suction pump skid."""

from __future__ import annotations

import math
import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "scene_core" / "centrifugal_pump_skid"
SOURCE = ASSET / "source" / "centrifugal_pump_skid.blend"
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
    low, high = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for obj in objects:
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            low = Vector((min(low.x, point.x), min(low.y, point.y), min(low.z, point.z)))
            high = Vector((max(high.x, point.x), max(high.y, point.y), max(high.z, point.z)))
    return low, high


def render(scene, camera, path, location, target, floor, show_floor=True):
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
    visual = [obj for obj in scene.objects if obj.type in {"MESH", "CURVE"} and obj.get("rungproof_asset")]
    low, high = bounds(visual)
    center = (low + high) / 2
    radius = max((high - low).length * 1.05, 4.2)
    bpy.ops.mesh.primitive_plane_add(size=12, location=(center.x, center.y, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(material("Review floor", (.07, .09, .11), .72))
    world = scene.world or bpy.data.worlds.new("Review world")
    scene.world, world.color = world, (.015, .022, .032)
    for index, (offset, energy, size) in enumerate((((4, -5, 6), 1100, 4), ((-4, -1, 3), 600, 3), ((1, 5, 4), 760, 3))):
        data = bpy.data.lights.new(f"Review light {index}", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(offset)
        aim(light, center)
    data = bpy.data.cameras.new("Review camera")
    data.lens = 58
    camera = bpy.data.objects.new("Review camera", data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    hero = center + Vector((1.10, -1.12, .62)) * radius
    render(scene, camera, REVIEW / "hero.png", hero, center, floor)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")
    render(scene, camera, REVIEW / "end_suction_alignment.png", center + Vector((1.55, -.12, .22)) * radius,
           Vector((.90, 0, .65)), floor)
    render(scene, camera, REVIEW / "discharge_and_volute.png", center + Vector((.75, -1.20, .85)) * radius,
           Vector((.65, 0, .83)), floor)
    render(scene, camera, REVIEW / "coupling_guard.png", center + Vector((.15, -1.35, .30)) * radius,
           Vector((.02, 0, .50)), floor)
    rear = center + Vector((-1.42, .20, .35)) * radius
    render(scene, camera, REVIEW / "motor_fan.png", rear, Vector((-1.0, 0, .50)), floor)
    render(scene, camera, REVIEW / "underside.png", center + Vector((.80, -1.0, -.70)) * radius,
           Vector((0, 0, low.z + .10)), floor, False)
    render(scene, camera, REVIEW / "state_stopped.png", rear, Vector((-1.0, 0, .50)), floor)
    scene.objects["KIN_pump_shaft"].rotation_euler.x = math.radians(22.5)
    render(scene, camera, REVIEW / "state_running.png", rear, Vector((-1.0, 0, .50)), floor)
    scene.objects["KIN_pump_shaft"].rotation_euler.x = 0

    bpy.ops.mesh.primitive_cube_add(size=1, location=(high.x + .70, center.y, low.z + .5))
    scale = bpy.context.object
    scale.data.materials.append(material("One metre reference", (.9, .18, .02), .38))
    render(scene, camera, REVIEW / "scale_reference.png", center + Vector((1.15, -1.20, .62)) * radius,
           (center + scale.location) / 2, floor)
    bpy.data.objects.remove(scale, do_unlink=True)

    wire = material("Review wire", (.02, .85, 1.0), .35)
    clones = []
    for obj in visual:
        obj.hide_render = True
        if obj.type != "MESH":
            continue
        clone = obj.copy()
        clone.data = obj.data.copy()
        clone.data.materials.clear()
        clone.data.materials.append(wire)
        scene.collection.objects.link(clone)
        clone.modifiers.new("Review wireframe", "WIREFRAME").thickness = .0025
        clones.append(clone)
    render(scene, camera, REVIEW / "wireframe.png", hero, center, floor)
    for obj in visual:
        obj.hide_render = False
    for clone in clones:
        bpy.data.objects.remove(clone, do_unlink=True)
    shutil.copyfile(REVIEW / "hero.png", ASSET / "thumbnail.png")
    print(f"PUMP_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
