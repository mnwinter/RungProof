# Rotary selector delivery audit - 2026-10-04

Disposition: **candidate; prior four-position approval invalidated**.

The delivered GLB contains POSITION_label_0/1/2 and three matching tick meshes,
not the four detents claimed by the archived 2026-09-21 review. Its handle pivot
quaternion rotates around Godot Z; the front dial is in the X/Y plane. The old
runtime incorrectly applied Y rotation while retaining its authored Z angle.
The source builder also currently authors only three marks. Existing pictures
and the archived independent result do not establish the current delivery's
four-position animation or the newly composed scene variants.

The production entry was moved to candidates and its quality flags cleared.
The original recognition JSON and visual review are preserved under
archive-20261004-stale-four-position-review. No new independent pass is claimed.
The master GLB/Blend and historical pictures are unchanged.

SceneComposer now creates 2-4 configured radial detents/number labels and hides
the master's fixed markings. SelectorSwitchController turns the actual handle
around Z and honors initialPosition. The runtime projects both integer and BOOL
selector inputs through this same mapping without writing PLC-owned outputs.
Imported pointer geometry is checked through every declared action and Reset
in Bay Light Selector, Maintenance Beacon, Fume Extractor and Pallet Pickup.
Count, alignment and pointer-plane checks failed before repair; Reset already
passed. All 20 final selector checks pass, including tick/plate contact. Final
native five-view and close observations cover the Bay Light 2-position variant
and Maintenance Beacon 4-position variant, including actual 3D clicks and Reset.
Other variants still need fresh native observation after this shared change.
Evidence is tracked separately in docs/MULTI_ANGLE_SCENE_REVIEW.md.
Full master re-authoring, approval/help reconciliation and fresh
independent asset recognition remain pending.
