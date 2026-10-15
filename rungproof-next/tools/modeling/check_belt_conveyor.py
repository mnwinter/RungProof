"""Mechanical and review-artifact checks for the conveyor candidate.

Run inside Blender after opening the generated source .blend. Visual review is
still mandatory; these assertions prevent known spatial failures from quietly
returning between renders.
"""

from __future__ import annotations

import os
from pathlib import Path

import bpy


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "material_handling" / "belt_conveyor_600x6000"


def world_bounds(name):
    obj = bpy.data.objects[name]
    points = [obj.matrix_world @ type(obj.location)(corner) for corner in obj.bound_box]
    return (
        tuple(min(point[i] for point in points) for i in range(3)),
        tuple(max(point[i] for point in points) for i in range(3)),
    )


required = {
    "KIN_belt_surface",
    "KIN_drive_drum",
    "KIN_tail_drum",
    "DRIVE_gearbox",
    "DRIVE_motor_body",
    "DRIVE_output_flange",
    "tail_bearing_L_slide",
    "tail_bearing_R_slide",
    "TAKEUP_L_screw",
    "TAKEUP_R_screw",
    "SAFETY_pullcord",
    "SAFETY_pull_switch",
    "SAFETY_tail_anchor",
    "CTRL_junction_box",
    "CTRL_power_cable",
}
missing = required.difference(bpy.data.objects.keys())
assert not missing, f"Missing required mechanical objects: {sorted(missing)}"

belt_min, belt_max = world_bounds("KIN_belt_surface")
frame_min, frame_max = world_bounds("FRAME_L_profile")
cord_min, cord_max = world_bounds("SAFETY_pullcord")
assert 0.590 <= belt_max[1] - belt_min[1] <= 0.610, "Belt width drifted from 600 mm."
assert belt_max[2] > frame_max[2], "Carrying belt no longer clears the side frame."
assert cord_max[2] < frame_max[2], "Pull cord has moved into the conveying surface envelope."

for side in ("L", "R"):
    screw_min, screw_max = world_bounds(f"TAKEUP_{side}_screw")
    slide_min, slide_max = world_bounds(f"tail_bearing_{side}_slide")
    plate_min, plate_max = world_bounds(f"TAKEUP_{side}_endplate")
    assert screw_max[0] >= slide_min[0], f"{side} take-up screw no longer reaches bearing slide."
    assert screw_min[0] <= plate_max[0], f"{side} take-up screw no longer reaches end plate."

reviews = (
    "hero",
    "operator_side",
    "drive_detail",
    "tail_detail",
    "underside",
    "end_alignment",
    "blind_review",
    "state_stopped",
    "state_running",
    "scale_reference",
    "wireframe",
    "godot_runtime",
    "godot_state_stopped",
    "godot_state_running",
)
for review in reviews:
    path = ASSET / "review" / f"{review}.png"
    assert path.is_file() and path.stat().st_size > 50_000, f"Missing/invalid review render: {path}"

print("MECHANICAL_CHECKS_PASS")
print(f"REVIEW_VIEWS_PASS {len(reviews)}")
