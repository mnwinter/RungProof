"""Render review evidence for a static factory-kit asset.

The asset name and two close-detail labels are supplied through environment
variables so an evidence package remains tied to its rendered source asset.
"""
from __future__ import annotations

import os
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET_NAME = os.environ.get("RUNGPROOF_FACTORY_REVIEW_ASSET", "safety_bollard")
DETAIL_ONE = os.environ.get("RUNGPROOF_FACTORY_REVIEW_DETAIL_ONE", "base_and_anchors")
DETAIL_TWO = os.environ.get("RUNGPROOF_FACTORY_REVIEW_DETAIL_TWO", "reflective_sleeve_and_cap")
VIEW_Y_SIGN = float(os.environ.get("RUNGPROOF_FACTORY_REVIEW_Y_SIGN", "-1"))
REVIEW_RESOLUTION = int(os.environ.get("RUNGPROOF_FACTORY_REVIEW_RESOLUTION", "960"))
# Asset families do not share a single best inspection angle.  Keep the normal
# wide three-quarter view by default, while allowing a sensor review to use a
# more side-on camera that can show both an active face and its target.
HERO_X_FACTOR = float(os.environ.get("RUNGPROOF_FACTORY_REVIEW_HERO_X_FACTOR", "1.42"))
HERO_Y_FACTOR = float(os.environ.get("RUNGPROOF_FACTORY_REVIEW_HERO_Y_FACTOR", "2.00"))
ASSET = Path(os.environ["RUNGPROOF_REVIEW_ASSET_ROOT"]) if os.environ.get("RUNGPROOF_REVIEW_ASSET_ROOT") else ROOT / "assets" / "factory_kit" / ASSET_NAME
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
    span = max((high - low).x, (high - low).y, (high - low).z, 1.0)
    bpy.ops.mesh.primitive_plane_add(size=8, location=(center.x, center.y, low.z - .025))
    floor = bpy.context.object
    floor.data.materials.append(material("Review floor", (.07, .09, .11), .72))
    world = scene.world or bpy.data.worlds.new("Review world")
    scene.world = world
    world.color = (.015, .022, .032)
    for index, (offset, energy, size) in enumerate((((3, -4, 4), 800, 2.8), ((-3, -2, 3), 460, 2.2), ((1, 4, 3), 560, 2.4))):
        data = bpy.data.lights.new(f"Review light {index}", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(offset)
        aim(light, center)
    camera_data = bpy.data.cameras.new("Review camera")
    camera_data.lens = 65
    camera = bpy.data.objects.new("Review camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    # 640 px remains sufficient for a source-blind identity review while
    # making batch evidence generation practical on CPU-only workstations.
    # Keep 960 px as the default for final presentation evidence.
    scene.render.resolution_x = scene.render.resolution_y = REVIEW_RESOLUTION
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    # Frame the entire assembly at the same apparent scale whether it is a
    # compact bollard or a four-metre perimeter component.
    # Keep a conservative margin for tall, narrow assemblies (such as a floor
    # scale indicator).  The former lens/distance combination clipped their
    # upper geometry despite the bounds calculation being correct.
    hero = center + Vector((span * HERO_X_FACTOR, VIEW_Y_SIGN * span * HERO_Y_FACTOR, span * 1.12))
    target = center + Vector((0, 0, .05))
    render(scene, camera, floor, REVIEW / "hero.png", hero, target)
    shutil.copyfile(REVIEW / "hero.png", REVIEW / "blind_review.png")
    render(scene, camera, floor, REVIEW / f"{DETAIL_ONE}.png", center + Vector((1.7, -2.4, .05)), Vector((0, 0, .12)))
    render(scene, camera, floor, REVIEW / f"{DETAIL_TWO}.png", center + Vector((1.1, -2.0, 1.0)), Vector((0, 0, .95)))
    render(scene, camera, floor, REVIEW / "opposite_side.png", center + Vector((-span * 1.42, -VIEW_Y_SIGN * span * 2.00, span * 1.12)), target)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(high.x + span * .18, center.y, low.z + .5))
    scale = bpy.context.object
    scale.data.materials.append(material("One metre reference", (.9, .18, .02), .38))
    render(scene, camera, floor, REVIEW / "scale_reference.png", center + Vector((span * 1.48, VIEW_Y_SIGN * span * 2.05, span * 1.12)), (center + scale.location) / 2)
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
    print(f"FACTORY_KIT_REVIEW_RENDERED {ASSET_NAME} {REVIEW}")


if __name__ == "__main__":
    main()
