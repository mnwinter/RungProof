"""Build the first RungProof production candidate in Blender.

The asset is intentionally assembled from recognizable industrial subassemblies:
fabric belt, crowned drums, return/idler rollers, channel frame, adjustable legs,
pillow-block bearings, shaft guard, helical gearmotor, E-stop pull cord, and a
local junction box. Units are metres and object names are stable simulator IDs.
"""

from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET_ROOT = ROOT / "assets" / "material_handling" / "belt_conveyor_600x6000"
SOURCE = ASSET_ROOT / "source" / "belt_conveyor_600x6000.blend"
DELIVERY = ASSET_ROOT / "delivery" / "belt_conveyor_600x6000.glb"
COLLISION = ASSET_ROOT / "collision" / "belt_conveyor_600x6000_collision.glb"
REVIEW = ASSET_ROOT / "review"
TEXTURES = ASSET_ROOT / "textures"
for folder in (SOURCE.parent, DELIVERY.parent, COLLISION.parent, REVIEW, TEXTURES):
    folder.mkdir(parents=True, exist_ok=True)


def generated_texture(name, pixel_function, colorspace="sRGB", size=256):
    image = bpy.data.images.new(name, width=size, height=size, alpha=False)
    pixels = []
    for y in range(size):
        for x in range(size):
            pixels.extend((*pixel_function(x / size, y / size), 1.0))
    image.pixels.foreach_set(pixels)
    image.colorspace_settings.name = colorspace
    image.filepath_raw = str(TEXTURES / f"{name}.png")
    image.file_format = "PNG"
    image.save()
    return image


BELT_BASE_IMAGE = generated_texture(
    "belt_rubber_basecolor",
    lambda u, v: (
        0.050 + 0.020 * (0.5 + 0.5 * math.sin(u * math.tau * 12)) + 0.010 * (0.5 + 0.5 * math.sin(v * math.tau * 24)),
        0.053 + 0.017 * (0.5 + 0.5 * math.sin(u * math.tau * 12)),
        0.052 + 0.014 * (0.5 + 0.5 * math.sin(v * math.tau * 24)),
    ),
)
BELT_ROUGHNESS_IMAGE = generated_texture(
    "belt_rubber_roughness",
    lambda u, v: (0.97 + 0.03 * (0.5 + 0.5 * math.sin((u + v) * math.tau * 16)),) * 3,
    "Non-Color",
)
BELT_NORMAL_IMAGE = generated_texture(
    "belt_rubber_normal",
    lambda u, v: (
        0.5 + 0.025 * math.sin(v * math.tau * 80),
        0.5 + 0.018 * math.sin(u * math.tau * 42),
        1.0,
    ),
    "Non-Color",
)


def material(name, color, metallic=0.0, roughness=0.45):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def belt_material():
    mat = material("MAT_Belt_Rubber_Fabric", (0.018, 0.020, 0.019), 0.0, 0.82)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    # Industrial fabric-backed rubber is matte. Zeroing the direct dielectric
    # lobe prevents rectangular area lights from reading as loose white parts.
    bsdf.inputs["Specular IOR Level"].default_value = 0.0
    base = nodes.new("ShaderNodeTexImage")
    base.name = "Belt base color"
    base.image = BELT_BASE_IMAGE
    base.extension = "REPEAT"
    links.new(base.outputs["Color"], bsdf.inputs["Base Color"])
    roughness = nodes.new("ShaderNodeTexImage")
    roughness.name = "Belt roughness"
    roughness.image = BELT_ROUGHNESS_IMAGE
    roughness.extension = "REPEAT"
    links.new(roughness.outputs["Color"], bsdf.inputs["Roughness"])
    normal_texture = nodes.new("ShaderNodeTexImage")
    normal_texture.name = "Belt normal"
    normal_texture.image = BELT_NORMAL_IMAGE
    normal_texture.extension = "REPEAT"
    normal = nodes.new("ShaderNodeNormalMap")
    normal.inputs["Strength"].default_value = 0.22
    links.new(normal_texture.outputs["Color"], normal.inputs["Color"])
    links.new(normal.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


STEEL = material("MAT_Steel_Powdercoat_RAL5010", (0.025, 0.19, 0.34), 0.62, 0.29)
DARK_STEEL = material("MAT_BlackOxide_Steel", (0.028, 0.035, 0.04), 0.78, 0.24)
GALV = material("MAT_Galvanized_Steel", (0.47, 0.51, 0.54), 0.78, 0.33)
BELT = belt_material()
MOTOR = material("MAT_Motor_RAL5018", (0.01, 0.34, 0.40), 0.55, 0.31)
GEARBOX = material("MAT_Gearbox_Aluminum", (0.30, 0.33, 0.35), 0.82, 0.23)
YELLOW = material("MAT_Safety_Yellow", (0.95, 0.57, 0.015), 0.25, 0.34)
RED = material("MAT_EStop_Red", (0.72, 0.012, 0.008), 0.18, 0.29)
ORANGE = material("MAT_Cord_Orange", (1.0, 0.16, 0.01), 0.05, 0.42)
WHITE = material("MAT_Label_White", (0.82, 0.84, 0.82), 0.05, 0.52)


def finish(obj, name, mat, bevel=0.0, smooth=False):
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("Edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        mod.limit_method = "ANGLE"
    if smooth:
        for poly in obj.data.polygons:
            poly.use_smooth = True
    obj["rungproof_asset"] = True
    return obj


def box(name, loc, dims, mat=STEEL, bevel=0.006):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, bevel)


def cyl(name, loc, radius, depth, mat=DARK_STEEL, axis="Y", vertices=64):
    rot = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    return finish(bpy.context.object, name, mat, 0.0015, True)


def bolt(name, loc, axis="Z"):
    rot = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.009, depth=0.008, location=loc, rotation=rot)
    return finish(bpy.context.object, name, DARK_STEEL, 0.0005)


def torus(name, loc, major, minor, mat=DARK_STEEL, axis="Y"):
    rot = (math.pi / 2, 0, 0) if axis == "Y" else (0, math.pi / 2, 0)
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=64, minor_segments=12, location=loc, rotation=rot)
    return finish(bpy.context.object, name, mat, 0, True)


def beam_between(name, start, end, width, mat=STEEL):
    start_v, end_v = Vector(start), Vector(end)
    delta = end_v - start_v
    obj = box(name, (start_v + end_v) / 2, (delta.length, width, width), mat, 0.004)
    obj.rotation_euler = delta.to_track_quat("X", "Z").to_euler()
    return obj


def bearing_unit(prefix, x, y, z):
    box(f"{prefix}_slide", (x, y, z), (0.15, 0.052, 0.112), GALV, 0.012)
    cyl(f"{prefix}_insert", (x, y, z), 0.040, 0.060, DARK_STEEL, "Y", 64)
    for dx in (-0.053, 0.053):
        bolt(f"{prefix}_bolt_{'A' if dx < 0 else 'B'}", (x + dx, y, z - 0.041), "Y")


def leg_station(index, x):
    for side, y in (("L", -0.43), ("R", 0.43)):
        box(f"LEG_{index}_{side}_upright", (x, y, 0.47), (0.060, 0.060, 0.80), STEEL, 0.006)
        box(f"LEG_{index}_{side}_foot", (x, y, 0.047), (0.20, 0.16, 0.014), GALV, 0.004)
        cyl(f"LEG_{index}_{side}_leveler", (x, y, 0.083), 0.017, 0.08, DARK_STEEL, "Z", 40)
        for dx in (-0.065, 0.065):
            for dy in (-0.045, 0.045):
                bolt(f"LEG_{index}_{side}_anchor_{dx}_{dy}", (x + dx, y + dy, 0.057), "Z")
        box(f"LEG_{index}_{side}_top_bracket", (x, y, 0.88), (0.18, 0.085, 0.035), GALV, 0.004)
    box(f"LEG_{index}_crossbar", (x, 0, 0.52), (0.055, 0.84, 0.055), STEEL, 0.004)


def continuous_belt(name, half_length, center_z, radius, width, thickness):
    path = []
    path.append((-half_length, center_z + radius, 0.0, 1.0))
    path.append((half_length, center_z + radius, 0.0, 1.0))
    for i in range(1, 25):
        angle = math.pi / 2 - math.pi * i / 24
        path.append((half_length + radius * math.cos(angle), center_z + radius * math.sin(angle), math.cos(angle), math.sin(angle)))
    path.append((-half_length, center_z - radius, 0.0, -1.0))
    for i in range(1, 25):
        angle = -math.pi / 2 - math.pi * i / 24
        path.append((-half_length + radius * math.cos(angle), center_z + radius * math.sin(angle), math.cos(angle), math.sin(angle)))

    verts = []
    for x, z, nx, nz in path:
        outer = (x + nx * thickness / 2, z + nz * thickness / 2)
        inner = (x - nx * thickness / 2, z - nz * thickness / 2)
        verts.extend([
            (outer[0], -width / 2, outer[1]),
            (outer[0], width / 2, outer[1]),
            (inner[0], -width / 2, inner[1]),
            (inner[0], width / 2, inner[1]),
        ])
    faces = []
    count = len(path)
    for i in range(count):
        j = (i + 1) % count
        a, b = i * 4, j * 4
        faces.extend([
            (a, b, b + 1, a + 1),
            (a + 2, a + 3, b + 3, b + 2),
            (a, a + 2, b + 2, b),
            (a + 1, b + 1, b + 3, a + 3),
        ])
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    cumulative = [0.0]
    for index in range(1, len(path)):
        previous, current = path[index - 1], path[index]
        cumulative.append(cumulative[-1] + math.hypot(current[0] - previous[0], current[1] - previous[1]))
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            path_index, cross_section = divmod(vertex_index, 4)
            uv_layer.data[loop_index].uv = (
                cumulative[path_index] / 0.18,
                0.0 if cross_section in (0, 2) else 1.0,
            )
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, name, BELT, 0, True)
    return obj


def add_text_label(text, loc, rotation, size=0.045):
    bpy.ops.object.text_add(location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.0006
    return finish(obj, f"LABEL_{text}", WHITE)


def tube_path(name, points, radius, mat=DARK_STEEL):
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
    return finish(obj, name, mat)


# Reset to a deterministic empty file.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

# Coherent low-profile slider-bed conveyor: 6.0 m nominal frame and 600 mm belt.
DRUM_X, DRUM_Z, DRUM_R = 2.935, 0.985, 0.065
continuous_belt("KIN_belt_surface", DRUM_X, DRUM_Z, DRUM_R, 0.600, 0.010)
box("BELT_vulcanized_splice", (-1.55, 0, 1.0605), (0.014, 0.594, 0.002), DARK_STEEL, 0.001)
box("BED_stainless_slider", (0, 0, 1.032), (5.72, 0.585, 0.012), GALV, 0.002)
for side, y in (("L", -0.345), ("R", 0.345)):
    box(f"FRAME_{side}_profile", (0, y, 0.955), (5.84, 0.060, 0.120), STEEL, 0.008)
    box(f"FRAME_{side}_slot", (0, y + (-0.031 if side == "L" else 0.031), 0.955), (5.72, 0.006, 0.018), DARK_STEEL, 0.002)
for i, x in enumerate((-2.55, -1.70, -0.85, 0, 0.85, 1.70, 2.55)):
    box(f"FRAME_crossmember_{i:02d}", (x, 0, 0.925), (0.055, 0.63, 0.045), GALV, 0.004)

for role, x in (("tail", -DRUM_X), ("drive", DRUM_X)):
    cyl(f"KIN_{role}_drum", (x, 0, DRUM_Z), DRUM_R - 0.006, 0.615, DARK_STEEL, "Y", 96)
    cyl(f"{role}_shaft", (x, 0, DRUM_Z), 0.018, 0.84, GALV, "Y", 48)
    bearing_unit(f"{role}_bearing_L", x, -0.385, DRUM_Z)
    bearing_unit(f"{role}_bearing_R", x, 0.385, DRUM_Z)

# Tail screw take-up is aligned with the sliding bearing blocks.
for side, y in (("L", -0.385), ("R", 0.385)):
    cyl(f"TAKEUP_{side}_screw", (-3.005, y, DRUM_Z), 0.009, 0.17, GALV, "X", 40)
    box(f"TAKEUP_{side}_nut", (-3.065, y, DRUM_Z), (0.028, 0.052, 0.052), DARK_STEEL, 0.003)
    box(f"TAKEUP_{side}_endplate", (-3.085, y, DRUM_Z), (0.022, 0.12, 0.15), GALV, 0.004)

for i, x in enumerate((-2.25, -1.10, 0.05, 1.20, 2.25)):
    cyl(f"KIN_return_idler_{i:02d}", (x, 0, 0.900), 0.022, 0.590, DARK_STEEL, "Y", 64)
    for side, y in (("L", -0.325), ("R", 0.325)):
        box(f"RETURN_{i:02d}_{side}_bracket", (x, y, 0.910), (0.030, 0.035, 0.085), GALV, 0.004)

for i, x in enumerate((-2.15, 0.0, 2.15)):
    leg_station(i + 1, x)
for side, y in (("L", -0.43), ("R", 0.43)):
    beam_between(f"BRACE_{side}_A", (-2.15, y, 0.25), (0.0, y, 0.78), 0.035)
    beam_between(f"BRACE_{side}_B", (0.0, y, 0.25), (2.15, y, 0.78), 0.035)

# Right-side hollow-shaft gearmotor: output is coaxial with the head pulley;
# motor input is perpendicular to the reducer output, as on a worm gearmotor.
box("DRIVE_mount_bracket", (2.935, 0.475, 0.885), (0.34, 0.24, 0.030), GALV, 0.005)
box("DRIVE_gearbox", (2.935, 0.475, DRUM_Z), (0.235, 0.205, 0.235), GEARBOX, 0.028)
cyl("DRIVE_output_flange", (2.935, 0.365, DRUM_Z), 0.078, 0.030, GEARBOX, "Y", 64)
cyl("DRIVE_output_hub", (2.935, 0.375, DRUM_Z), 0.050, 0.105, DARK_STEEL, "Y", 64)
cyl("DRIVE_motor_body", (2.665, 0.505, DRUM_Z), 0.105, 0.390, MOTOR, "X", 96)
cyl("DRIVE_motor_fan_cowl", (2.445, 0.505, DRUM_Z), 0.116, 0.065, MOTOR, "X", 96)
for i, x in enumerate((2.53, 2.59, 2.65, 2.71, 2.77)):
    torus(f"DRIVE_motor_fin_{i:02d}", (x, 0.505, DRUM_Z), 0.108, 0.006, MOTOR, "X")
box("DRIVE_terminal_box", (2.665, 0.505, 1.105), (0.20, 0.17, 0.11), MOTOR, 0.014)
box("DRIVE_motor_nameplate", (2.665, 0.615, DRUM_Z), (0.145, 0.006, 0.065), WHITE, 0.003)
for dx, dz in ((-0.060, -0.060), (-0.060, 0.060), (0.060, -0.060), (0.060, 0.060)):
    bolt(f"DRIVE_flange_bolt_{dx}_{dz}", (2.935 + dx, 0.343, DRUM_Z + dz), "Y")
for dx in (-0.115, 0.115):
    for dy in (-0.075, 0.075):
        bolt(f"DRIVE_mount_bolt_{dx}_{dy}", (2.935 + dx, 0.475 + dy, 0.906), "Z")
box("GUARD_coupling_shell", (2.935, 0.355, DRUM_Z), (0.22, 0.075, 0.185), YELLOW, 0.018)
box("GUARD_coupling_inset", (2.935, 0.314, DRUM_Z), (0.15, 0.008, 0.105), DARK_STEEL, 0.020)
for dx in (-0.082, 0.082):
    for dz in (-0.067, 0.067):
        bolt(f"GUARD_fastener_{dx}_{dz}", (2.935 + dx, 0.309, DRUM_Z + dz), "Y")

# Fixed controls mounted below and outside the operator-side frame. The vertical
# standoff remains visible from either side so the station cannot read as a
# floating box when the frame obscures its horizontal mounting plate.
box("CTRL_station_drop", (1.95, -0.405, 0.875), (0.055, 0.09, 0.28), GALV, 0.004)
box("CTRL_station_bracket", (1.95, -0.465, 0.755), (0.20, 0.14, 0.045), GALV, 0.004)
box("CTRL_station", (1.95, -0.525, 0.820), (0.18, 0.11, 0.18), YELLOW, 0.022)
cyl("CTRL_estop_button", (1.95, -0.593, 0.840), 0.044, 0.040, RED, "Y", 64)
box("CTRL_junction_bracket", (0.0, -0.43, 0.57), (0.06, 0.12, 0.30), GALV, 0.004)
box("CTRL_junction_box", (0.0, -0.515, 0.57), (0.27, 0.13, 0.31), GALV, 0.018)
box("CTRL_junction_lid", (0.0, -0.587, 0.57), (0.23, 0.018, 0.27), WHITE, 0.010)
for dx in (-0.09, 0.09):
    for dz in (-0.11, 0.11):
        bolt(f"CTRL_lid_screw_{dx}_{dz}", (dx, -0.600, 0.57 + dz), "Y")
cyl("CTRL_cable_gland", (0.0, -0.515, 0.39), 0.022, 0.065, DARK_STEEL, "Z", 40)
box("CTRL_cable_tray", (1.30, 0.25, 0.735), (2.55, 0.07, 0.045), GALV, 0.004)
tube_path("CTRL_power_cable", [
    (0.0, -0.515, 0.390),
    (0.0, -0.515, 0.710),
    (0.0, 0.250, 0.710),
    (2.665, 0.250, 0.710),
    (2.665, 0.505, 0.710),
    (2.665, 0.505, 1.050),
], 0.010)
tube_path("CTRL_station_cable", [
    (0.0, -0.515, 0.710),
    (0.0, 0.250, 0.710),
    (1.95, 0.250, 0.710),
    (1.95, -0.405, 0.710),
    (1.95, -0.405, 0.820),
], 0.007)

# Pull cord follows the accessible side of the fixed frame.
curve = bpy.data.curves.new("EStop_pullcord_curve", "CURVE")
curve.dimensions = "3D"
curve.bevel_depth = 0.006
curve.bevel_resolution = 3
spline = curve.splines.new("POLY")
spline.points.add(1)
spline.points[0].co = (-2.55, -0.455, 0.875, 1)
spline.points[1].co = (1.67, -0.455, 0.875, 1)
cord = bpy.data.objects.new("SAFETY_pullcord", curve)
bpy.context.collection.objects.link(cord)
finish(cord, "SAFETY_pullcord", ORANGE)
box("SAFETY_pull_switch_bracket", (1.78, -0.385, 0.875), (0.045, 0.14, 0.16), GALV, 0.004)
box("SAFETY_pull_switch", (1.78, -0.475, 0.875), (0.16, 0.11, 0.14), YELLOW, 0.018)
cyl("SAFETY_pull_eye", (1.685, -0.475, 0.875), 0.020, 0.055, DARK_STEEL, "X", 48)
cyl("SAFETY_tail_spring", (-2.66, -0.455, 0.875), 0.018, 0.22, GALV, "X", 48)
box("SAFETY_tail_anchor", (-2.81, -0.405, 0.875), (0.035, 0.14, 0.15), GALV, 0.004)
for x in (-1.45, -0.25, 0.95):
    box(f"SAFETY_cord_guide_{x}", (x, -0.405, 0.875), (0.025, 0.08, 0.09), GALV, 0.004)

# Stable connector empties used by the simulator snapping system.
for name, location, kind in (
    ("CONN_material_in", (-3.00, 0, 1.055), "material_flow"),
    ("CONN_material_out", (3.00, 0, 1.055), "material_flow"),
    ("CONN_power", (0.0, -0.62, 0.39), "electrical_24vdc"),
):
    empty = bpy.data.objects.new(name, None)
    empty.location = location
    empty.empty_display_type = "ARROWS"
    empty["connector_kind"] = kind
    empty["rungproof_asset"] = True
    bpy.context.collection.objects.link(empty)

# Export production geometry before adding review-only floor/camera/lights.
bpy.ops.object.select_all(action="DESELECT")
for obj in bpy.context.scene.objects:
    if obj.get("rungproof_asset"):
        obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(DELIVERY), export_format="GLB", use_selection=True, export_apply=True)

# Conservative collision proxies exported separately.
bpy.ops.object.select_all(action="DESELECT")
collision_objects = [
    box("COLLISION_frame", (0, 0, 0.96), (6.05, 0.78, 0.18), None, 0),
    box("COLLISION_belt", (0, 0, 1.055), (6.02, 0.61, 0.04), None, 0),
    box("COLLISION_drive", (2.72, 0.51, 0.99), (0.62, 0.40, 0.34), None, 0),
]
for obj in collision_objects:
    obj["rungproof_asset"] = False
    obj.display_type = "WIRE"
    obj.hide_render = True
    obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(COLLISION), export_format="GLB", use_selection=True, export_apply=True)

# Review scene.
floor = box("REVIEW_floor", (0, 0, -0.015), (9.5, 6.5, 0.03), material("MAT_Floor", (0.12, 0.14, 0.16), 0.0, 0.76), 0)
floor["rungproof_asset"] = False
for obj in collision_objects:
    obj.hide_viewport = True

world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.025, 0.035, 0.05, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.30

def area(name, loc, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    obj.location = loc
    bpy.context.collection.objects.link(obj)
    point_camera(obj, Vector((0, 0, 0.8)))


def point_camera(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


area("KEY_softbox", (-3.8, -4.5, 6.8), 850, 4.0, (1.0, 0.93, 0.84))
area("FILL_softbox", (4.5, 2.5, 4.2), 500, 3.5, (0.76, 0.84, 1.0))
area("RIM_softbox", (0.0, 4.2, 5.0), 600, 3.0, (0.68, 0.79, 1.0))

cam_data = bpy.data.cameras.new("ReviewCamera")
cam = bpy.data.objects.new("ReviewCamera", cam_data)
bpy.context.collection.objects.link(cam)
bpy.context.scene.camera = cam
cam.data.lens = 58

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.image_settings.color_mode = "RGBA"

views = {
    "hero": ((6.8, -6.3, 3.6), (0.0, 0.0, 0.72)),
    "operator_side": ((0.0, -8.8, 2.25), (0.0, 0.0, 0.76)),
    "drive_detail": ((4.65, 3.25, 2.15), (2.72, 0.30, 0.95)),
    "tail_detail": ((-4.45, -2.75, 2.10), (-2.75, -0.05, 0.96)),
    "underside": ((4.6, -5.8, 1.35), (0.0, 0.0, 0.58)),
    "end_alignment": ((4.75, -0.25, 1.65), (2.72, 0.0, 0.96)),
    "blind_review": ((6.6, -6.0, 3.25), (0.0, 0.0, 0.72)),
}
for name, (position, target) in views.items():
    cam.location = position
    point_camera(cam, Vector(target))
    scene.render.filepath = str(REVIEW / f"{name}.png")
    bpy.ops.render.render(write_still=True)

# Motion proof uses a review-only witness carton. It is deliberately created
# after export, so neither the package nor the evidence labels ship with the
# conveyor asset. The second frame advances the witness by the commanded
# 0.65 m/s over a two-second interval and rotates every driven element by the
# matching angular displacement.
witness = box("REVIEW_motion_witness", (-1.60, 0.0, 1.205), (0.42, 0.42, 0.28), YELLOW, 0.018)
witness["rungproof_asset"] = False
cam.location = views["hero"][0]
point_camera(cam, Vector(views["hero"][1]))
scene.render.filepath = str(REVIEW / "state_stopped.png")
bpy.ops.render.render(write_still=True)
witness.location.x += 1.30
for obj in bpy.data.objects:
    if obj.name.startswith(("KIN_drive_drum", "KIN_tail_drum")):
        obj.rotation_euler.y += 1.30 / (DRUM_R - 0.006)
    elif obj.name.startswith("KIN_return_idler"):
        obj.rotation_euler.y += 1.30 / 0.022
scene.render.filepath = str(REVIEW / "state_running.png")
bpy.ops.render.render(write_still=True)
witness.hide_render = True

# A one-metre striped gauge provides unambiguous visual scale. Each band is
# 100 mm high; the gauge is review-only and never enters the delivered GLB.
scale_parts = []
for index in range(10):
    part = box(
        f"REVIEW_scale_band_{index:02d}",
        (-2.20, -1.05, 0.05 + index * 0.10),
        (0.10, 0.10, 0.10),
        WHITE if index % 2 == 0 else ORANGE,
        0.002,
    )
    part["rungproof_asset"] = False
    scale_parts.append(part)
scale_label = add_text_label("1.0 m", (-2.20, -1.12, 1.08), (math.radians(90), 0, 0), 0.09)
scale_label["rungproof_asset"] = False
scale_parts.append(scale_label)
cam.location = (6.8, -7.2, 3.4)
point_camera(cam, Vector((-0.25, -0.10, 0.70)))
scene.render.filepath = str(REVIEW / "scale_reference.png")
bpy.ops.render.render(write_still=True)
for part in scale_parts:
    part.hide_render = True

# Topology evidence overlays the actual source mesh edges on the shaded asset.
# Wire duplicates are review-only and are removed from visibility afterwards.
wire_material = material("REVIEW_Wire_Cyan", (0.02, 0.95, 1.0), 0.0, 0.65)
wire_objects = []
for source_obj in list(bpy.data.objects):
    if source_obj.type != "MESH" or not source_obj.get("rungproof_asset"):
        continue
    duplicate = source_obj.copy()
    duplicate.data = source_obj.data.copy()
    duplicate.name = f"REVIEW_wire_{source_obj.name}"
    duplicate["rungproof_asset"] = False
    duplicate.data.materials.clear()
    duplicate.data.materials.append(wire_material)
    duplicate.modifiers.clear()
    modifier = duplicate.modifiers.new("Topology wire", "WIREFRAME")
    modifier.thickness = 0.0014
    modifier.use_replace = True
    modifier.use_even_offset = True
    bpy.context.collection.objects.link(duplicate)
    wire_objects.append(duplicate)
cam.location = (5.4, -4.8, 2.75)
point_camera(cam, Vector((0.0, 0.0, 0.82)))
scene.render.filepath = str(REVIEW / "wireframe.png")
bpy.ops.render.render(write_still=True)
for obj in wire_objects:
    obj.hide_render = True

# Thumbnail is a copy rendered at catalog dimensions.
cam.location = views["hero"][0]
point_camera(cam, Vector(views["hero"][1]))
scene.render.resolution_x = 640
scene.render.resolution_y = 400
scene.render.filepath = str(ASSET_ROOT / "thumbnail.png")
bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
print(f"RUNGPROOF_ASSET_BUILT {DELIVERY}")
