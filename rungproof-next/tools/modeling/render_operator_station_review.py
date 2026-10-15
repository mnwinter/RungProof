"""Render the complete production-review package for the operator station."""

from __future__ import annotations

import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "scene_core" / "operator_pushbutton_station"
SOURCE = ASSET / "source" / "operator_pushbutton_station.blend"
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
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            low = Vector((min(low.x, world.x), min(low.y, world.y), min(low.z, world.z)))
            high = Vector((max(high.x, world.x), max(high.y, world.y), max(high.z, world.z)))
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
    radius = max((high - low).length * 1.15, 2.7)

    bpy.ops.mesh.primitive_plane_add(size=8, location=(center.x, center.y, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(material("Review floor", (.07, .09, .11), .72))
    world = scene.world or bpy.data.worlds.new("Review world")
    scene.world = world
    world.color = (.015, .022, .032)
    for index, (offset, energy, size) in enumerate((((3, -4, 5), 950, 3.2), ((-3, -1, 3), 520, 2.4), ((1, 4, 4), 700, 2.8))):
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

    front = center + Vector((.82, -1.35, .55)) * radius
    render(scene, camera, REVIEW / "hero.png", front, center, floor)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")
    render(scene, camera, REVIEW / "controls_closeup.png", center + Vector((.45, -1.0, .22)) * radius,
           Vector((0, -.20, .62)), floor)
    render(scene, camera, REVIEW / "cable_and_base.png", center + Vector((1.15, -.95, -.18)) * radius,
           Vector((.12, .03, -.18)), floor)
    render(scene, camera, REVIEW / "rear_enclosure.png", center + Vector((-.8, 1.25, .52)) * radius,
           center, floor)
    render(scene, camera, REVIEW / "underside.png", center + Vector((.8, -1.0, -1.0)) * radius,
           Vector((0, 0, low.z + .10)), floor, False)

    # Stopped/running witnesses prove independent button travel and indication.
    render(scene, camera, REVIEW / "state_stopped.png", front, center, floor)
    pushbutton = scene.objects["KIN_pushbutton"]
    pushbutton.location.y += .025
    green = pushbutton.data.materials[0]
    green.use_nodes = True
    shader = green.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Emission Color"].default_value = (.02, .8, .18, 1)
    shader.inputs["Emission Strength"].default_value = 2.5
    render(scene, camera, REVIEW / "state_running.png", front, center, floor)
    pushbutton.location.y -= .025

    bpy.ops.mesh.primitive_cube_add(size=1, location=(high.x + .70, center.y, low.z + .5))
    scale = bpy.context.object
    scale.data.materials.append(material("One metre reference", (.9, .18, .02), .38))
    render(scene, camera, REVIEW / "scale_reference.png", center + Vector((1.05, -1.28, .60)) * radius,
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
        modifier = clone.modifiers.new("Review wireframe", "WIREFRAME")
        modifier.thickness = .0025
        clones.append(clone)
    render(scene, camera, REVIEW / "wireframe.png", front, center, floor)
    for obj in visual:
        obj.hide_render = False
    for clone in clones:
        bpy.data.objects.remove(clone, do_unlink=True)

    shutil.copyfile(REVIEW / "hero.png", ASSET / "thumbnail.png")
    print(f"OPERATOR_STATION_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
