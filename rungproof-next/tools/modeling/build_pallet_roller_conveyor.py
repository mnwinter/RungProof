"""Build a production-candidate chain-driven pallet roller conveyor.

The assembly is a 4 m long, 1 m clear-width powered conveyor for standard
pallet loads. Units are metres. Stable names beginning with KIN_, DRIVE_,
SENSOR_, and CTRL_ are simulator binding points rather than decorative labels.
"""

from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET_ROOT = ROOT / "assets" / "material_handling" / "pallet_roller_conveyor_1000x4000"
SOURCE = ASSET_ROOT / "source" / "pallet_roller_conveyor_1000x4000.blend"
DELIVERY = ASSET_ROOT / "delivery" / "pallet_roller_conveyor_1000x4000.glb"
COLLISION = ASSET_ROOT / "collision" / "pallet_roller_conveyor_1000x4000_collision.glb"
REVIEW = ASSET_ROOT / "review"
for folder in (SOURCE.parent, DELIVERY.parent, COLLISION.parent, REVIEW):
    folder.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0


def material(name, color, metallic=0.0, roughness=0.45):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


FRAME = material("MAT_Frame_RAL5010", (0.025, 0.18, 0.32), 0.62, 0.30)
ROLLER = material("MAT_Roller_Galvanized", (0.42, 0.47, 0.50), 0.88, 0.24)
GALV = material("MAT_Galvanized", (0.48, 0.52, 0.54), 0.78, 0.34)
BLACK = material("MAT_BlackOxide", (0.025, 0.030, 0.034), 0.72, 0.30)
MOTOR = material("MAT_Motor_RAL5018", (0.01, 0.34, 0.40), 0.55, 0.31)
GEARBOX = material("MAT_Gearbox_Aluminum", (0.31, 0.34, 0.36), 0.82, 0.24)
YELLOW = material("MAT_Safety_Yellow", (0.95, 0.56, 0.015), 0.22, 0.35)
RED = material("MAT_EStop_Red", (0.72, 0.012, 0.008), 0.16, 0.30)
ORANGE = material("MAT_Cable_Orange", (1.0, 0.16, 0.01), 0.04, 0.44)
WHITE = material("MAT_Label_White", (0.84, 0.86, 0.84), 0.04, 0.55)
SENSOR = material("MAT_Sensor_Plastic", (0.08, 0.10, 0.11), 0.05, 0.38)
LENS = material("MAT_Sensor_Lens", (0.56, 0.05, 0.015), 0.10, 0.18)
WOOD = material("MAT_Pallet_Wood", (0.43, 0.24, 0.09), 0.02, 0.72)


def finish(obj, name, mat=None, bevel=0.0, smooth=False, asset=True):
    obj.name = name
    if mat is not None:
        obj.data.materials.append(mat)
    if bevel:
        modifier = obj.modifiers.new("Edge radius", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        modifier.limit_method = "ANGLE"
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    obj["rungproof_asset"] = asset
    return obj


def box(name, location, dimensions, mat=FRAME, bevel=0.006, asset=True):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, bevel, asset=asset)


def cyl(name, location, radius, depth, mat=ROLLER, axis="Y", vertices=64, asset=True):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation
    )
    return finish(bpy.context.object, name, mat, 0.0012, True, asset)


def bolt(name, location, axis="Z", asset=True):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=6, radius=0.009, depth=0.010, location=location, rotation=rotation
    )
    return finish(bpy.context.object, name, BLACK, 0.0004, asset=asset)


def torus(name, location, major, minor, mat=BLACK, axis="Y", asset=True):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else (0, math.pi / 2, 0)
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major,
        minor_radius=minor,
        major_segments=64,
        minor_segments=10,
        location=location,
        rotation=rotation,
    )
    return finish(bpy.context.object, name, mat, smooth=True, asset=asset)


def beam_between(name, start, end, width, mat=FRAME, asset=True):
    start_v, end_v = Vector(start), Vector(end)
    delta = end_v - start_v
    obj = box(name, (start_v + end_v) / 2, (delta.length, width, width), mat, 0.004, asset)
    obj.rotation_euler = delta.to_track_quat("X", "Z").to_euler()
    return obj


def tube_path(name, points, radius, mat=BLACK, asset=True):
    curve = bpy.data.curves.new(f"{name}_curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 3
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat, asset=asset)


def add_text_label(text, location, rotation, size=0.075):
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.0007
    return finish(obj, f"REVIEW_label_{text}", WHITE, asset=False)


def sprocket(name, x, y, z, radius=0.052):
    cyl(f"{name}_plate", (x, y, z), radius, 0.024, BLACK, "Y", 48)
    cyl(f"{name}_hub", (x, y, z), 0.024, 0.036, GALV, "Y", 48)
    for index in range(12):
        angle = index * math.tau / 12
        tooth = box(
            f"{name}_tooth_{index:02d}",
            (x + math.cos(angle) * (radius + 0.006), y, z + math.sin(angle) * (radius + 0.006)),
            (0.014, 0.028, 0.014),
            BLACK,
            0.002,
        )
        tooth.rotation_euler.y = -angle


def support_station(index, x):
    for side, y in (("L", -0.64), ("R", 0.64)):
        box(f"LEG_{index}_{side}_upright", (x, y, 0.48), (0.070, 0.070, 0.82), FRAME, 0.006)
        box(f"LEG_{index}_{side}_foot", (x, y, 0.047), (0.22, 0.18, 0.014), GALV, 0.004)
        cyl(f"LEG_{index}_{side}_leveler", (x, y, 0.083), 0.017, 0.08, BLACK, "Z", 40)
        for dx in (-0.073, 0.073):
            for dy in (-0.052, 0.052):
                bolt(f"LEG_{index}_{side}_anchor_{dx}_{dy}", (x + dx, y + dy, 0.057))
        box(f"LEG_{index}_{side}_saddle", (x, y, 0.91), (0.20, 0.11, 0.045), GALV, 0.004)
    box(f"LEG_{index}_crossbar", (x, 0, 0.55), (0.065, 1.22, 0.065), FRAME, 0.005)


def point_camera(camera, target):
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()


def add_area(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    obj.location = location
    bpy.context.collection.objects.link(obj)
    point_camera(obj, Vector((0, 0, 0.75)))


# Deterministic empty scene.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
    for block in list(blocks):
        if block.users == 0:
            blocks.remove(block)


# 4 m chain-driven pallet roller conveyor, 1 m clear roller face.
ROLLER_Z = 1.055
ROLLER_RADIUS = 0.038
ROLLER_LENGTH = 1.02
ROLLER_COUNT = 31
ROLLER_PITCH = 0.128
start_x = -(ROLLER_COUNT - 1) * ROLLER_PITCH / 2

for side, y in (("L", -0.585), ("R", 0.585)):
    box(f"FRAME_{side}_channel", (0, y, 0.955), (4.05, 0.11, 0.22), FRAME, 0.008)
    box(f"FRAME_{side}_top_flange", (0, y, 1.075), (4.05, 0.16, 0.025), GALV, 0.004)
    box(f"FRAME_{side}_guide", (0, y, 1.145), (4.05, 0.035, 0.12), FRAME, 0.005)

for index in range(ROLLER_COUNT):
    x = start_x + index * ROLLER_PITCH
    cyl(f"KIN_roller_{index:02d}", (x, 0, ROLLER_Z), ROLLER_RADIUS, ROLLER_LENGTH, ROLLER, "Y", 64)
    cyl(f"ROLLER_{index:02d}_shaft_L", (x, -0.545, ROLLER_Z), 0.012, 0.10, GALV, "Y", 32)
    cyl(f"ROLLER_{index:02d}_shaft_R", (x, 0.545, ROLLER_Z), 0.012, 0.10, GALV, "Y", 32)
    sprocket(f"DRIVE_sprocket_{index:02d}", x, -0.655, ROLLER_Z, 0.046)

# Roller-chain runs and a removable full-length guard.
tube_path("DRIVE_chain_upper", [(start_x - 0.05, -0.655, 1.105), (start_x + (ROLLER_COUNT - 1) * ROLLER_PITCH + 0.05, -0.655, 1.105)], 0.009, BLACK)
tube_path("DRIVE_chain_lower", [(start_x - 0.05, -0.655, 1.005), (start_x + (ROLLER_COUNT - 1) * ROLLER_PITCH + 0.05, -0.655, 1.005)], 0.009, BLACK)
guard = box("GUARD_chain_removable", (0, -0.680, 1.045), (4.00, 0.160, 0.23), FRAME, 0.010)
box("GUARD_chain_label", (0.70, -0.755, 1.045), (0.42, 0.008, 0.075), YELLOW, 0.006)
for x in (-1.80, -1.20, -0.60, 0.0, 0.60, 1.20, 1.80):
    bolt(f"GUARD_chain_fastener_{x}", (x, -0.758, 1.045), "Y")

# Under-slung helical gearmotor and chain reduction to roller 25.  The drive
# cradle visibly transfers motor and reducer weight back into the conveyor
# frame; no equipment is supported by an isolated plate.
DRIVE_X = 1.28
box("DRIVE_mount_plate", (DRIVE_X, -0.72, 0.77), (0.46, 0.30, 0.025), GALV, 0.005)
for x in (DRIVE_X - 0.17, DRIVE_X + 0.17):
    box(f"DRIVE_cradle_rail_{x}", (x, -0.96, 0.69), (0.055, 0.72, 0.065), FRAME, 0.005)
    box(f"DRIVE_cradle_hanger_{x}", (x, -0.62, 0.82), (0.055, 0.055, 0.30), FRAME, 0.005)
box("DRIVE_cradle_crossmember", (DRIVE_X, -1.25, 0.69), (0.46, 0.065, 0.065), FRAME, 0.005)
box("DRIVE_gearbox", (DRIVE_X, -0.79, 0.83), (0.25, 0.24, 0.25), GEARBOX, 0.030)
cyl("DRIVE_output_flange", (DRIVE_X, -0.665, 0.83), 0.080, 0.035, GEARBOX, "Y", 64)
sprocket("DRIVE_motor_sprocket", DRIVE_X, -0.645, 0.83, 0.065)
cyl("DRIVE_motor_body", (DRIVE_X, -1.035, 0.83), 0.115, 0.43, MOTOR, "Y", 96)
cyl("DRIVE_motor_fan_cowl", (DRIVE_X, -1.28, 0.83), 0.126, 0.075, MOTOR, "Y", 96)
for y in (-1.18, -1.10, -1.02, -0.94, -0.86):
    torus(f"DRIVE_motor_fin_{y}", (DRIVE_X, y, 0.83), 0.118, 0.006, MOTOR, "Y")
box("DRIVE_terminal_box", (DRIVE_X, -1.035, 1.05), (0.19, 0.19, 0.11), MOTOR, 0.014)
tube_path("DRIVE_drop_chain_A", [(DRIVE_X - 0.070, -0.655, 0.83), (DRIVE_X - 0.070, -0.655, 1.055)], 0.010, BLACK)
tube_path("DRIVE_drop_chain_B", [(DRIVE_X + 0.070, -0.655, 0.83), (DRIVE_X + 0.070, -0.655, 1.055)], 0.010, BLACK)
box("GUARD_drive_drop", (DRIVE_X, -0.680, 0.925), (0.25, 0.160, 0.40), YELLOW, 0.014)
for dx in (-0.075, 0.075):
    for dz in (-0.14, 0.14):
        bolt(f"GUARD_drive_bolt_{dx}_{dz}", (DRIVE_X + dx, -0.758, 0.925 + dz), "Y")

# Three braced support stations.
for station, x in enumerate((-1.62, 0.0, 1.62), 1):
    support_station(station, x)
for side, y in (("L", -0.64), ("R", 0.64)):
    beam_between(f"BRACE_{side}_A", (-1.62, y, 0.28), (0.0, y, 0.80), 0.040)
    beam_between(f"BRACE_{side}_B", (0.0, y, 0.28), (1.62, y, 0.80), 0.040)

# Pallet-presence photoeye, reflector, local E-stop, and routed cabling.
box("SENSOR_photoeye_bracket", (-1.42, -0.665, 1.16), (0.055, 0.16, 0.20), GALV, 0.004)
box("SENSOR_photoeye", (-1.42, -0.755, 1.19), (0.10, 0.12, 0.085), SENSOR, 0.015)
cyl("SENSOR_photoeye_lens", (-1.42, -0.822, 1.19), 0.022, 0.018, LENS, "Y", 48)
box("SENSOR_reflector_bracket", (-1.42, 0.665, 1.16), (0.055, 0.16, 0.20), GALV, 0.004)
box("SENSOR_reflector", (-1.42, 0.755, 1.19), (0.10, 0.025, 0.10), WHITE, 0.008)
ESTOP_X = 1.82
box("CTRL_estop_drop", (ESTOP_X, -0.66, 0.86), (0.055, 0.12, 0.31), GALV, 0.004)
box("CTRL_estop_station", (ESTOP_X, -0.755, 0.78), (0.18, 0.12, 0.18), YELLOW, 0.022)
cyl("CTRL_estop_button", (ESTOP_X, -0.825, 0.80), 0.045, 0.042, RED, "Y", 64)
box("CTRL_junction_bracket", (0.0, 0.66, 0.61), (0.06, 0.14, 0.30), GALV, 0.004)
box("CTRL_junction_box", (0.0, 0.755, 0.61), (0.29, 0.14, 0.32), GALV, 0.018)
box("CTRL_junction_lid", (0.0, 0.832, 0.61), (0.25, 0.018, 0.28), WHITE, 0.010)
for dx in (-0.10, 0.10):
    for dz in (-0.115, 0.115):
        bolt(f"CTRL_lid_screw_{dx}_{dz}", (dx, 0.844, 0.61 + dz), "Y")
box("CTRL_cable_tray", (0.0, 0.38, 0.74), (2.90, 0.08, 0.05), GALV, 0.004)
tube_path("CTRL_motor_cable", [(0.0, 0.755, 0.45), (0.0, 0.38, 0.70), (DRIVE_X, 0.38, 0.70), (DRIVE_X, -1.035, 0.86), (DRIVE_X, -1.035, 0.975)], 0.010, BLACK)
tube_path("CTRL_sensor_cable", [(0.0, 0.755, 0.50), (-1.42, 0.38, 0.70), (-1.42, -0.755, 1.14)], 0.007, BLACK)
tube_path("CTRL_estop_cable", [(0.0, 0.755, 0.56), (1.40, 0.38, 0.70), (ESTOP_X, -0.66, 0.70)], 0.007, BLACK)
cyl("CTRL_motor_gland", (DRIVE_X, -1.035, 0.975), 0.022, 0.055, BLACK, "Z", 40)
for index, x in enumerate((-1.25, -0.42, 0.42, 1.25)):
    box(f"CTRL_tray_clamp_{index:02d}", (x, 0.38, 0.775), (0.035, 0.105, 0.028), GALV, 0.004)

# Typed connection points.
for name, location, kind in (
    ("CONN_material_in", (-2.05, 0, 1.10), "pallet_flow"),
    ("CONN_material_out", (2.05, 0, 1.10), "pallet_flow"),
    ("CONN_power", (0.0, 0.86, 0.45), "electrical_3phase"),
):
    empty = bpy.data.objects.new(name, None)
    empty.location = location
    empty.empty_display_type = "ARROWS"
    empty["connector_kind"] = kind
    empty["rungproof_asset"] = True
    bpy.context.collection.objects.link(empty)

# Export production geometry before adding review-only objects.
bpy.ops.object.select_all(action="DESELECT")
for obj in bpy.context.scene.objects:
    if obj.get("rungproof_asset"):
        obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(DELIVERY), export_format="GLB", use_selection=True, export_apply=True)

# Conservative collision proxies.
bpy.ops.object.select_all(action="DESELECT")
collision_objects = [
    box("COLLISION_frame", (0, 0, 1.00), (4.12, 1.30, 0.30), None, 0, False),
    box("COLLISION_drive", (DRIVE_X, -0.94, 0.86), (0.52, 0.62, 0.46), None, 0, False),
]
for obj in collision_objects:
    obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(COLLISION), export_format="GLB", use_selection=True, export_apply=True)
for obj in collision_objects:
    obj.hide_render = True

# Review studio.
floor = box("REVIEW_floor", (0, 0, -0.02), (11, 8, 0.04), GALV, 0, False)
world = bpy.data.worlds.new("ReviewWorld")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.006, 0.010, 0.014, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.24
bpy.context.scene.world = world
add_area("KEY_softbox", (-3.6, -4.8, 6.4), 900, 4.0, (1.0, 0.93, 0.84))
add_area("FILL_softbox", (4.4, 3.2, 4.3), 560, 3.4, (0.74, 0.84, 1.0))
add_area("RIM_softbox", (0, 4.4, 5.2), 620, 3.0, (0.66, 0.78, 1.0))

camera_data = bpy.data.cameras.new("ReviewCamera")
camera = bpy.data.objects.new("ReviewCamera", camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera.data.lens = 58
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False

views = {
    "hero": ((5.5, -5.5, 3.25), (0, 0, 0.75)),
    "operator_side": ((0, -7.5, 2.15), (0, 0, 0.78)),
    "drive_detail": ((3.05, -3.25, 1.75), (1.25, -0.55, 0.88)),
    "infeed_detail": ((-3.30, -2.7, 1.95), (-1.55, -0.05, 1.02)),
    "underside": ((3.8, 5.0, 1.45), (0, 0, 0.58)),
    "end_alignment": ((4.30, -0.10, 1.65), (1.80, 0, 1.00)),
    "blind_review": ((5.3, -5.2, 3.0), (0, 0, 0.75)),
}
for name, (position, target) in views.items():
    camera.location = position
    point_camera(camera, Vector(target))
    scene.render.filepath = str(REVIEW / f"{name}.png")
    bpy.ops.render.render(write_still=True)

# Open-guard mechanical evidence.
guard.hide_render = True
bpy.data.objects["GUARD_drive_drop"].hide_render = True
camera.location = (2.7, -3.0, 1.55)
point_camera(camera, Vector((0.90, -0.57, 0.96)))
scene.render.filepath = str(REVIEW / "chain_drive_open.png")
bpy.ops.render.render(write_still=True)
guard.hide_render = False
bpy.data.objects["GUARD_drive_drop"].hide_render = False

# Motion witness: a recognizable 1200 x 1000 mm timber pallet advances one
# metre. It is review-only and does not ship with the conveyor.
pallet_parts = []
for index, y in enumerate((-0.40, 0.0, 0.40)):
    pallet_parts.append(box(f"REVIEW_pallet_stringer_{index}", (-1.25, y, 1.145), (1.20, 0.10, 0.10), WOOD, 0.008, False))
for index, x_offset in enumerate((-0.53, -0.35, -0.17, 0.0, 0.17, 0.35, 0.53)):
    pallet_parts.append(box(f"REVIEW_pallet_deck_{index}", (-1.25 + x_offset, 0, 1.215), (0.13, 1.00, 0.045), WOOD, 0.006, False))
for index, x_offset in enumerate((-0.46, 0.0, 0.46)):
    for y in (-0.40, 0.0, 0.40):
        pallet_parts.append(box(f"REVIEW_pallet_block_{index}_{y}", (-1.25 + x_offset, y, 1.145), (0.14, 0.14, 0.10), WOOD, 0.006, False))
camera.location = views["hero"][0]
point_camera(camera, Vector(views["hero"][1]))
scene.render.filepath = str(REVIEW / "state_stopped.png")
bpy.ops.render.render(write_still=True)
for part in pallet_parts:
    part.location.x += 1.00
for obj in bpy.data.objects:
    if obj.name.startswith("KIN_roller_"):
        obj.rotation_euler.y += 1.00 / ROLLER_RADIUS
scene.render.filepath = str(REVIEW / "state_running.png")
bpy.ops.render.render(write_still=True)
for part in pallet_parts:
    part.hide_render = True

# One-metre scale gauge with ten 100 mm bands.
scale_parts = []
for index in range(10):
    part = box(
        f"REVIEW_scale_{index:02d}",
        (-1.80, -1.55, 0.05 + index * 0.10),
        (0.10, 0.10, 0.10),
        WHITE if index % 2 == 0 else ORANGE,
        0.002,
        False,
    )
    scale_parts.append(part)
scale_parts.append(add_text_label("1.0 m", (-1.80, -1.62, 1.08), (math.radians(90), 0, 0), 0.09))
camera.location = (5.6, -6.4, 3.15)
point_camera(camera, Vector((-0.15, -0.10, 0.72)))
scene.render.filepath = str(REVIEW / "scale_reference.png")
bpy.ops.render.render(write_still=True)
for part in scale_parts:
    part.hide_render = True

# Source topology overlay.
wire_mat = material("REVIEW_Wire_Cyan", (0.02, 0.95, 1.0), 0.0, 0.65)
wire_objects = []
for source_obj in list(bpy.data.objects):
    if source_obj.type != "MESH" or not source_obj.get("rungproof_asset"):
        continue
    duplicate = source_obj.copy()
    duplicate.data = source_obj.data.copy()
    duplicate.name = f"REVIEW_wire_{source_obj.name}"
    duplicate["rungproof_asset"] = False
    duplicate.data.materials.clear()
    duplicate.data.materials.append(wire_mat)
    duplicate.modifiers.clear()
    modifier = duplicate.modifiers.new("Topology wire", "WIREFRAME")
    modifier.thickness = 0.0013
    modifier.use_replace = True
    modifier.use_even_offset = True
    bpy.context.collection.objects.link(duplicate)
    wire_objects.append(duplicate)
camera.location = (4.6, -4.2, 2.55)
point_camera(camera, Vector((0, 0, 0.82)))
scene.render.filepath = str(REVIEW / "wireframe.png")
bpy.ops.render.render(write_still=True)
for obj in wire_objects:
    obj.hide_render = True

# Catalog thumbnail.
camera.location = views["hero"][0]
point_camera(camera, Vector(views["hero"][1]))
scene.render.resolution_x = 640
scene.render.resolution_y = 400
scene.render.filepath = str(ASSET_ROOT / "thumbnail.png")
bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
print(f"RUNGPROOF_ASSET_BUILT {DELIVERY}")
