"""Mechanical and evidence checks for the pallet roller conveyor candidate."""

from __future__ import annotations

import os
from pathlib import Path

import bpy


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "material_handling" / "pallet_roller_conveyor_1000x4000"


def bounds(name):
    obj = bpy.data.objects[name]
    points = [obj.matrix_world @ type(obj.location)(corner) for corner in obj.bound_box]
    return (
        tuple(min(point[index] for point in points) for index in range(3)),
        tuple(max(point[index] for point in points) for index in range(3)),
    )


required = {
    "FRAME_L_channel",
    "FRAME_R_channel",
    "GUARD_chain_removable",
    "DRIVE_gearbox",
    "DRIVE_motor_body",
    "DRIVE_output_flange",
    "DRIVE_chain_upper",
    "DRIVE_chain_lower",
    "SENSOR_photoeye",
    "SENSOR_reflector",
    "CTRL_estop_station",
    "CTRL_junction_box",
    "CTRL_motor_cable",
    "CTRL_motor_gland",
}
missing = required.difference(bpy.data.objects.keys())
assert not missing, f"Missing required objects: {sorted(missing)}"

rollers = sorted(name for name in bpy.data.objects.keys() if name.startswith("KIN_roller_"))
assert len(rollers) == 31, f"Expected 31 powered rollers, found {len(rollers)}."
roller_centres = [bpy.data.objects[name].matrix_world.translation for name in rollers]
assert max(abs(point.z - roller_centres[0].z) for point in roller_centres) < 0.001
first_min, first_max = bounds(rollers[0])
assert 1.015 <= first_max[1] - first_min[1] <= 1.025, "Roller face is not 1.02 m."

sensor = bpy.data.objects["SENSOR_photoeye"].matrix_world.translation
reflector = bpy.data.objects["SENSOR_reflector"].matrix_world.translation
assert abs(sensor.x - reflector.x) < 0.001
assert abs(sensor.z - reflector.z) < 0.001
assert sensor.y < 0 < reflector.y, "Photoeye and reflector no longer oppose each other."

guard_min, guard_max = bounds("GUARD_chain_removable")
sprocket_min, sprocket_max = bounds("DRIVE_sprocket_15_plate")
assert guard_min[1] < sprocket_min[1] and guard_max[1] > sprocket_max[1]
assert guard_min[2] < sprocket_min[2] and guard_max[2] > sprocket_max[2]

reviews = (
    "hero",
    "operator_side",
    "drive_detail",
    "infeed_detail",
    "underside",
    "end_alignment",
    "blind_review",
    "chain_drive_open",
    "state_stopped",
    "state_running",
    "scale_reference",
    "wireframe",
    "godot_state_stopped",
    "godot_state_running",
)
for review in reviews:
    path = ASSET / "review" / f"{review}.png"
    assert path.is_file() and path.stat().st_size > 50_000, f"Missing/invalid review: {path}"

print("PALLET_ROLLER_MECHANICAL_CHECKS_PASS")
print(f"ROLLER_COUNT_PASS {len(rollers)}")
print(f"REVIEW_VIEWS_PASS {len(reviews)}")
