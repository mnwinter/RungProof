"""Build reusable product-load and workpiece assets used by migrated scenes."""
from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
BASE = ROOT / "assets" / "scene_loads"
bpy.context.preferences.filepaths.save_version = 0


def clean():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights, bpy.data.materials):
        for block in list(blocks):
            if block.users == 0:
                blocks.remove(block)


def material(name, color, metallic=0.0, roughness=0.5):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return value


def finish(item, name, mat, bevel=0.006, smooth=False, collision=True):
    item.name = name
    item.data.materials.append(mat)
    if bevel:
        edge = item.modifiers.new("Manufactured edge radius", "BEVEL")
        edge.width = bevel
        edge.segments = 3
        edge.limit_method = "ANGLE"
    if smooth:
        for polygon in item.data.polygons:
            polygon.use_smooth = True
    item["rungproof_asset"] = True
    item["rungproof_collision"] = collision
    return item


def box(name, location, dimensions, mat, bevel=0.006, collision=True, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    item = bpy.context.object
    item.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(item, name, mat, bevel, False, collision)


def cylinder(name, location, radius, depth, mat, axis="Z", collision=True, vertices=64):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    return finish(bpy.context.object, name, mat, 0.002, True, collision)


def text_mesh(name, value, location, size, mat, rotation=(math.pi / 2, 0, 0)):
    data = bpy.data.curves.new(name, "FONT")
    data.body = value; data.align_x = "CENTER"; data.align_y = "CENTER"; data.size = size
    data.extrude = 0.003; data.bevel_depth = 0.001
    item = bpy.data.objects.new(name, data); bpy.context.collection.objects.link(item)
    item.location = location; item.rotation_euler = rotation
    bpy.context.view_layer.objects.active = item; item.select_set(True)
    bpy.ops.object.convert(target="MESH")
    return finish(item, name, mat, 0, False, False)


def mats():
    return {
        "kraft": material("Kraft corrugated board", (0.40, 0.20, 0.065), 0.0, 0.92),
        "tape": material("Carton tape", (0.67, 0.43, 0.15), 0.0, 0.82),
        "white": material("Label white", (0.92, 0.93, 0.90), 0.0, 0.68),
        "black": material("Printed black", (0.012, 0.016, 0.018), 0.0, 0.50),
        "blue": material("HDPE blue", (0.02, 0.22, 0.52), 0.0, 0.35),
        "natural": material("Natural HDPE", (0.82, 0.85, 0.80), 0.0, 0.42),
        "red": material("Cap red", (0.68, 0.015, 0.012), 0.05, 0.32),
        "steel": material("Machined steel", (0.40, 0.46, 0.49), 0.86, 0.18),
        "dark": material("Dark steel", (0.025, 0.034, 0.040), 0.50, 0.30),
        "yellow": material("Fixture yellow", (0.92, 0.54, 0.015), 0.18, 0.30),
    }


def carton(m):
    box("CARTON_BODY", (0, 0, 0.36), (0.85, 0.72, 0.72), m["kraft"], 0.018)
    # Four folded top flaps and a narrow center tape seam avoid the appearance
    # of cabinet rails while preserving unmistakable RSC carton construction.
    box("TOP_FLAP_LEFT", (-0.215, 0, 0.726), (0.41, 0.69, 0.012), m["kraft"], 0.004, False)
    box("TOP_FLAP_RIGHT", (0.215, 0, 0.728), (0.41, 0.69, 0.012), m["kraft"], 0.004, False)
    box("TOP_TAPE_SEAM", (0, 0, 0.740), (0.075, 0.71, 0.010), m["tape"], 0.002, False)
    box("SHIPPING_LABEL", (0, -0.366, 0.40), (0.34, 0.012, 0.20), m["white"], 0.003, False)
    for i in range(10):
        box(f"BARCODE_{i}", (-0.14 + i * 0.03, -0.374, 0.39),
            (0.012 if i % 3 else 0.018, 0.006, 0.13), m["black"], 0.001, False)
    text_mesh("CARTON_PRINT", "HANDLE WITH CARE", (0, -0.375, 0.21), .065, m["black"])


def jerry_can(m):
    box("JUG_BODY", (0, 0, 0.48), (0.62, 0.42, 0.88), m["natural"], 0.075)
    box("JUG_SHOULDER", (0.04, 0, 0.91), (0.50, 0.39, 0.20), m["natural"], 0.055)
    cylinder("JUG_NECK", (0.16, 0, 1.06), 0.105, 0.16, m["natural"])
    cylinder("JUG_CAP", (0.16, 0, 1.16), 0.13, 0.10, m["red"], vertices=48)
    for i in range(12):
        angle = math.radians(i * 30)
        box(f"JUG_CAP_GRIP_{i}", (0.16 + .135 * math.cos(angle), .135 * math.sin(angle), 1.16),
            (.018, .018, .08), m["dark"], .002, False, rotation=(0, 0, angle))
    # Four supported bars form a true open carry handle rather than a painted recess.
    box("HANDLE_TOP", (-0.17, 0, 1.08), (0.30, 0.10, 0.08), m["blue"], 0.030)
    box("HANDLE_LEFT", (-0.30, 0, 0.98), (0.07, 0.10, 0.25), m["blue"], 0.028)
    box("HANDLE_RIGHT", (-0.04, 0, 1.00), (0.07, 0.10, 0.21), m["blue"], 0.028)
    for x in (-0.22, 0, 0.22):
        box(f"MOLDED_RIB_{x}", (x, -0.216, 0.47), (0.045, 0.018, 0.62), m["blue"], 0.010, False)
    box("JUG_LABEL", (0, -0.228, 0.46), (0.32, 0.014, 0.24), m["white"], 0.004, False)
    for i, z in enumerate((.28,.36,.44,.52,.60,.68)):
        box(f"VOLUME_GRADUATION_{i}", (.265,-.228,z),(.07 if i%2 else .11,.014,.012),m["dark"],.002,False)
    box("HAZARD_DIAMOND",(-.12,-.238,.46),(.13,.014,.13),m["yellow"],.003,False,rotation=(0,0,math.pi/4))
    text_mesh("JUG_PRINT", "COOLANT 20 L", (0,-.239,.55),.055,m["dark"])


def process_bottle(m):
    cylinder("BOTTLE_BODY", (0, 0, 0.43), 0.24, 0.72, m["natural"], vertices=96)
    bpy.ops.mesh.primitive_cone_add(vertices=96, radius1=.24, radius2=.105, depth=.22, location=(0,0,.90))
    finish(bpy.context.object,"BOTTLE_SHOULDER",m["natural"],.003,True,True)
    cylinder("BOTTLE_NECK", (0, 0, 1.06), 0.095, 0.18, m["natural"], vertices=64)
    cylinder("BOTTLE_CAP", (0, 0, 1.18), 0.105, 0.10, m["blue"], vertices=48)
    for i in range(12):
        angle = math.radians(i * 30)
        box(f"CAP_GRIP_{i}", (0.108 * math.cos(angle), 0.108 * math.sin(angle), 1.18),
            (0.014, 0.014, 0.08), m["dark"], 0.002, False, rotation=(0, 0, angle))
    box("BOTTLE_LABEL", (0, -0.246, 0.46), (0.34, 0.014, 0.40), m["white"], 0.004, False)
    box("BOTTLE_LABEL_BAND", (0, -0.255, 0.55), (0.30, 0.006, 0.09), m["blue"], 0.002, False)
    for i in range(8):
        box(f"BOTTLE_BARCODE_{i}",(-.12+i*.034,-.259,.39),(.012,.004,.09),m["black"],.001,False)
    # Give the product name and capacity separate regions. The old single
    # line overran the label and intersected its blue band.
    text_mesh("BOTTLE_PRINT_PROCESS", "PROCESS FLUID", (0, -.261, .635), .025, m["dark"])
    text_mesh("BOTTLE_PRINT_CAPACITY", "1 L", (0, -.261, .55), .043, m["white"])
    for z in (0.12, 0.72):
        cylinder(f"BOTTLE_MOLDED_RIB_{z}", (0, 0, z), 0.248, 0.018, m["blue"], collision=False, vertices=96)
    target_objects=[item for item in bpy.context.scene.objects if item.type=="MESH" and item.get("rungproof_asset")]
    # Review-only bottle-line context uses two smaller copies of the same
    # package silhouette, rather than unrelated cylinders that imply process
    # plumbing. All context is excluded from delivery.
    context=[]
    context.append(box("REFERENCE_BELT",(0,0,-.08),(1.10,1.80,.12),m["dark"],.010,False))
    context.append(box("REFERENCE_RAIL_LEFT",(-.58,0,.10),(.08,1.80,.30),m["steel"],.010,False))
    context.append(box("REFERENCE_RAIL_RIGHT",(.58,0,.10),(.08,1.80,.30),m["steel"],.010,False))
    for copy_index,y in enumerate((-.62,.62)):
        for source in target_objects:
            clone=source.copy();clone.data=source.data.copy();bpy.context.collection.objects.link(clone)
            clone.name=f"REFERENCE_BOTTLE_{copy_index}_{source.name}";clone.scale*=.62
            clone.location=source.location*.62+Vector((0,y,0));context.append(clone)
    for item in context:item["rungproof_review_only"]=True


def machining_blank(m):
    # Ship the workholding context with the stock. A bare metal primitive is
    # visually ambiguous; the fixed/moving jaws and lead screw make this read
    # as a CNC-machine vise holding rectangular raw stock.
    box("VISE_BASE", (0, 0, .07), (1.18, .78, .14), m["blue"], .025)
    box("VISE_FIXED_JAW", (0, .27, .22), (1.04, .14, .28), m["dark"], .018)
    box("VISE_MOVING_JAW", (0, -.27, .22), (1.04, .14, .28), m["dark"], .018)
    box("FIXED_JAW_PLATE", (0, .192, .27), (.94, .025, .16), m["blue"], .004)
    box("MOVING_JAW_PLATE", (0, -.192, .27), (.94, .025, .16), m["blue"], .004)
    box("RECTANGULAR_4140_STOCK", (0, 0, .39), (.78, .36, .32), m["steel"], .012)
    box("MACHINED_TOP_FACE", (0, 0, .555), (.68, .28, .012), m["steel"], .003, False)
    for x in (-.42, .42):
        cylinder(f"VISE_MOUNT_BOLT_{x}", (x, 0, .16), .045, .12, m["dark"], vertices=48)
    cylinder("VISE_LEAD_SCREW", (0, -.49, .18), .055, .36, m["steel"], axis="Y", vertices=48)
    cylinder("VISE_HANDLE_HUB", (0, -.69, .18), .09, .10, m["dark"], axis="Y", vertices=48)
    box("VISE_HANDLE", (0, -.75, .18), (.62, .045, .045), m["steel"], .015, rotation=(0, .45, 0))
    text_mesh("STOCK_MARK", "4140", (0, -.187, .43), .09, m["dark"], rotation=(math.pi/2, 0, 0))
    context=[]
    context.append(box("REFERENCE_MACHINE_TABLE",(0,0,-.10),(1.55,1.35,.16),m["dark"],.010,False))
    for x in (-.55,-.18,.18,.55):
        context.append(box(f"REFERENCE_T_SLOT_{x}",(x,0,-.005),(.055,1.20,.025),m["steel"],.002,False))
    context.append(box("REFERENCE_VISE_JAW_FIXED",(0,.43,.11),(.92,.12,.22),m["blue"],.012,False))
    context.append(box("REFERENCE_VISE_JAW_MOVING",(0,-.43,.11),(.92,.12,.22),m["blue"],.012,False))
    for item in context:item["rungproof_review_only"]=True


def fixture_plate(m):
    box("FIXTURE_BASE", (0, 0, 0.09), (1.40, 0.92, 0.18), m["steel"], 0.018)
    for x in (-0.54, 0.54):
        for y in (-0.34, 0.34):
            cylinder(f"MOUNT_HOLE_{x}_{y}", (x, y, 0.188), 0.045, 0.018, m["black"], collision=False, vertices=48)
    for x in (-0.38, 0.38):
        cylinder(f"LOCATING_PIN_{x}", (x, 0.18, 0.26), 0.045, 0.30, m["dark"], vertices=48)
        box(f"TOGGLE_CLAMP_BASE_{x}", (x, -0.22, 0.25), (0.22, 0.18, 0.14), m["yellow"], 0.020)
        box(f"TOGGLE_CLAMP_ARM_{x}", (x, -0.08, 0.38), (0.08, 0.34, 0.08), m["dark"], 0.015, rotation=(0.40, 0, 0))
        cylinder(f"CLAMP_PAD_{x}", (x, 0.08, 0.37), 0.055, 0.08, m["dark"], vertices=48)


def clamped_plate(m):
    box("FIXTURE_SUBPLATE",(0,0,.045),(1.48,.92,.09),m["dark"],.012)
    box("STEEL_WORKPIECE",(0,0,.15),(1.24,.70,.12),m["steel"],.008)
    for x in (-.52,.52):
        for y in (-.32,.32):
            cylinder(f"FIXTURE_BOLT_{x}_{y}",(x,y,.14),.045,.20,m["dark"],vertices=48)
    for x in (-.46,.46):
        box(f"TOE_CLAMP_{x}",(x,-.31,.25),(.30,.18,.09),m["yellow"],.014,rotation=(0,.12 if x<0 else -.12,0))
        box(f"CLAMP_SLOT_{x}",(x,-.31,.299),(.07,.10,.012),m["dark"],.010,False)
        cylinder(f"CLAMP_STUD_{x}",(x,-.31,.24),.032,.30,m["dark"],vertices=40)
        cylinder(f"CLAMP_WASHER_{x}",(x,-.31,.315),.060,.018,m["steel"],collision=False,vertices=48)
        cylinder(f"CLAMP_HEX_NUT_{x}",(x,-.31,.345),.052,.045,m["dark"],collision=False,vertices=6)
        box(f"STEP_BLOCK_{x}",(x,-.43,.18),(.20,.14,.22),m["dark"],.008)
        for step in range(3):
            box(f"STEP_BLOCK_TOOTH_{x}_{step}",(x,-.355-step*.035,.285-step*.045),(.18,.035,.035),m["steel"],.003,False)
    for x in (-.42,.42):
        cylinder(f"LOCATOR_{x}",(x,.30,.21),.035,.20,m["dark"],vertices=40)


BUILDERS = {
    "corrugated_shipping_carton": carton,
    "hdpe_jerry_can": jerry_can,
    "reusable_process_bottle": process_bottle,
    "rectangular_machining_blank": machining_blank,
    "modular_assembly_fixture": fixture_plate,
    "clamped_plate_workholding": clamped_plate,
}


def point_at(item, target):
    item.rotation_euler = (Vector(target) - item.location).to_track_quat("-Z", "Y").to_euler()


def save(slug, builder):
    clean()
    m = mats()
    builder(m)
    root = BASE / slug
    for folder in ("source", "delivery", "collision", "review"):
        (root / folder).mkdir(parents=True, exist_ok=True)
    for folder in (root / "source", root / "review"):
        (folder / ".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root / "source" / f"{slug}.blend"))
    objects = [item for item in bpy.context.scene.objects if item.type == "MESH" and item.get("rungproof_asset") and not item.get("rungproof_review_only")]
    bpy.ops.object.select_all(action="DESELECT")
    for item in objects:
        item.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(filepath=str(root / "delivery" / f"{slug}.glb"), export_format="GLB", use_selection=True, export_apply=True)
    minimum = Vector((1e9, 1e9, 1e9)); maximum = Vector((-1e9, -1e9, -1e9))
    for item in objects:
        for corner in item.bound_box:
            world = item.matrix_world @ Vector(corner)
            minimum.x, minimum.y, minimum.z = min(minimum.x, world.x), min(minimum.y, world.y), min(minimum.z, world.z)
            maximum.x, maximum.y, maximum.z = max(maximum.x, world.x), max(maximum.y, world.y), max(maximum.z, world.z)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_cube_add(location=(minimum + maximum) / 2)
    proxy = bpy.context.object; proxy.name = "COLLISION_primary"; proxy.dimensions = maximum - minimum
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.export_scene.gltf(filepath=str(root / "collision" / f"{slug}_collision.glb"), export_format="GLB", use_selection=True, export_apply=True)
    bpy.data.objects.remove(proxy, do_unlink=True)
    center = (minimum + maximum) / 2; span = maximum - minimum; radius = max(span.length * 1.55, 2.8)
    floor = box("REVIEW_floor", (0, 0, -0.035), (4.2, 4.2, 0.05), m["dark"], 0.002, False); floor["rungproof_asset"] = False
    world = bpy.context.scene.world or bpy.data.worlds.new("World"); bpy.context.scene.world = world; world.color = (0.018, 0.024, 0.028)
    for i, (location, energy, size) in enumerate((((3, -4, 4), 1200, 3), ((-3, -1, 2.5), 750, 2.5), ((0, 3, 4), 850, 3))):
        data = bpy.data.lights.new(f"REVIEW_light_{i}", "AREA"); data.energy = energy; data.shape = "DISK"; data.size = size
        light = bpy.data.objects.new(data.name, data); light.location = location; bpy.context.collection.objects.link(light); point_at(light, center)
    camera_data = bpy.data.cameras.new("REVIEW_camera"); camera = bpy.data.objects.new("REVIEW_camera", camera_data); bpy.context.collection.objects.link(camera); bpy.context.scene.camera = camera; camera_data.lens = 58
    scene = bpy.context.scene; scene.render.engine = "BLENDER_EEVEE"; scene.render.resolution_x = scene.render.resolution_y = 900; scene.render.resolution_percentage = 100; scene.render.image_settings.file_format = "PNG"
    for index, angle in enumerate((305, 215, 35, 125), 1):
        radians = math.radians(angle); camera.location = (center.x + radius * math.cos(radians), center.y + radius * math.sin(radians), center.z + radius * 0.38); point_at(camera, center)
        scene.render.filepath = str(root / "review" / f"{slug}_{index:02d}.png"); bpy.ops.render.render(write_still=True)
    (root / "thumbnail.png").write_bytes((root / "review" / f"{slug}_01.png").read_bytes())
    (root / "review" / "hero.png").write_bytes((root / "review" / f"{slug}_01.png").read_bytes())
    (root / "review" / "blind_review.png").write_bytes((root / "review" / f"{slug}_02.png").read_bytes())

    # Explicit metric scale witness. This is review-only and never exported in
    # the delivery GLB.
    ruler_x = maximum.x + max(span.x * .20, .24)
    scale_items = [box("REVIEW_SCALE_1M", (ruler_x, center.y, .50), (.035, .035, 1.0), m["white"], .002, False)]
    for tick in range(11):
        scale_items.append(box(f"REVIEW_SCALE_TICK_{tick}", (ruler_x - .05, center.y, tick * .10),
                               (.12 if tick in (0, 5, 10) else .075, .025, .012), m["yellow"], .001, False))
    scale_items.append(text_mesh("REVIEW_SCALE_LABEL", "1 m", (ruler_x, center.y - .025, .94), .08, m["white"]))
    camera.location = (center.x + radius * 1.18 * math.cos(math.radians(305)), center.y + radius * 1.18 * math.sin(math.radians(305)), center.z + radius * .48)
    point_at(camera, Vector(((minimum.x + ruler_x) / 2, center.y, max(center.z, .50))))
    scene.render.filepath = str(root / "review" / "scale_reference.png"); bpy.ops.render.render(write_still=True)
    for item in scale_items:
        bpy.data.objects.remove(item, do_unlink=True)

    # Topology witness derived from exact copies of the delivery meshes. The
    # Wireframe modifier produces real renderable edges instead of relying on
    # viewport-only overlay settings.
    wire_items = []
    review_context = [item for item in bpy.context.scene.objects if item.type == "MESH" and item not in objects]
    for item in review_context:
        item.hide_render = True
    for item in objects:
        item.hide_render = True
        clone = item.copy(); clone.data = item.data.copy(); bpy.context.collection.objects.link(clone)
        clone.hide_render = False
        clone.name = f"REVIEW_WIRE_{item.name}"
        clone.data.materials.clear(); clone.data.materials.append(m["white"])
        wire = clone.modifiers.new("Topology wire", "WIREFRAME"); wire.thickness = .004; wire.use_replace = True
        wire_items.append(clone)
    scene.render.engine = "BLENDER_EEVEE"
    camera.location = (center.x + radius * math.cos(math.radians(305)), center.y + radius * math.sin(math.radians(305)), center.z + radius * .38)
    point_at(camera, center)
    scene.render.filepath = str(root / "review" / "wireframe.png"); bpy.ops.render.render(write_still=True)
    for item in objects:
        item.hide_render = False
    for item in review_context:
        item.hide_render = False
    for item in wire_items:
        bpy.data.objects.remove(item, do_unlink=True)
    print("SCENE_LOAD_BUILT", slug)


if __name__ == "__main__":
    selected = {value.strip() for value in os.environ.get("RUNGPROOF_ASSET_FILTER", "").split(",") if value.strip()}
    for name, build in BUILDERS.items():
        if not selected or name in selected:
            save(name, build)
