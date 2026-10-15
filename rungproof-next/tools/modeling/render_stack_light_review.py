"""Render production evidence for the integrated three-tier stack light."""

from __future__ import annotations

import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "scene_core" / "stack_light_3_tier"
SOURCE = ASSET / "source" / "stack_light_3_tier.blend"
REVIEW = ASSET / "review"


def aim(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def mat(name, color, roughness=.55):
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


def render(scene, camera, path, location, target, floor):
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
    radius = max((high - low).length * 1.18, 2.8)
    bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(mat("Review floor", (.07, .09, .11), .72))
    world = scene.world or bpy.data.worlds.new("Review world")
    scene.world, world.color = world, (.015, .022, .032)
    for index, (offset, energy, size) in enumerate((((3, -4, 5), 900, 3.2), ((-3, -1, 3), 480, 2.4), ((1, 4, 4), 650, 2.8))):
        data = bpy.data.lights.new(f"Review light {index}", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(offset)
        aim(light, center)
    data = bpy.data.cameras.new("Review camera")
    data.lens = 60
    camera = bpy.data.objects.new("Review camera", data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    hero = center + Vector((1.05, -1.25, .45)) * radius
    render(scene, camera, REVIEW / "hero.png", hero, center, floor)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")
    render(scene, camera, REVIEW / "tower_closeup.png", center + Vector((.8, -1.12, .28)) * radius,
           Vector((0, 0, 1.48)), floor)
    render(scene, camera, REVIEW / "base_and_pole.png", center + Vector((1.0, -1.1, -.20)) * radius,
           Vector((0, 0, .35)), floor)
    render(scene, camera, REVIEW / "state_stopped.png", hero, center, floor)

    colors = {"LENS_green": (.01, 1.0, .12), "LENS_amber": (1.0, .35, .01), "LENS_red": (1.0, .01, .01)}
    for name, color in colors.items():
        material = scene.objects[name].data.materials[0]
        material.use_nodes = True
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Emission Color"].default_value = (*color, 1)
        shader.inputs["Emission Strength"].default_value = 4.0
    render(scene, camera, REVIEW / "state_running.png", hero, center, floor)

    bpy.ops.mesh.primitive_cube_add(size=1, location=(high.x + .72, 0, low.z + .5))
    scale = bpy.context.object
    scale.data.materials.append(mat("One metre reference", (.9, .18, .02), .38))
    render(scene, camera, REVIEW / "scale_reference.png", center + Vector((1.18, -1.30, .48)) * radius,
           (center + scale.location) / 2, floor)
    bpy.data.objects.remove(scale, do_unlink=True)

    wire = mat("Review wire", (.02, .85, 1.0), .35)
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
    print(f"STACK_LIGHT_REVIEW_RENDERED {REVIEW}")


if __name__ == "__main__":
    main()
