# All-scene realism: representative checkpoint

Scope is all 77 catalog scenes and labs. Current wider visual rollout remains held for parent/human review of the corrected representatives. This is progress evidence, not final acceptance.

## Current verified evidence

- Build: zero warnings and errors.
- `realism-all-environments-v1.log`: current 77-scene environment checks pass exact catalog coverage, world axes, sibling hierarchy and restoration of runtime/environment state.
- `realism-all-installations-v2.log`: native composed checks pass for 77 scenes, 71 authored-case scenes, 163 mounted functional picks, 138 accepted press-feedback checks, 17 selector changes, and eight prerequisite-blocked actions. Twelve known non-head bindings and seven retained field bindings have narrower explicitly logged coverage. These are scripted native checks, not keyboard qualification or physical clearance.
- Latest `realism-all-installations-v4.log` confirms the same 77-scene checks plus 35 selected PLC-owned BOOL indicator output-projection fixtures (false/off, true/emitting, false/off and reset). Three complex tower bindings and four PC-owned limit indicators are explicitly excluded from forced BOOL-output fixtures and retain their authored coverage. A failed verifier compile preceded an old-DLL v3 run; that v3 log does not prove the new indicator coverage. V4 follows a fresh successful build.
- `realism-grouping-dimensions-v2.json`: actual 68 visible `KIN_group_roller_*` mesh crowns are Y=0.90 m. Line long axis is world +X; environment yaw is zero. Cabinet top is Y=1.85 m; button centers Y=1.1672 m.
- Corrected camera V2 clears inherited viewport offsets; both operator and side views show the whole cabinet. V1 clipped views are superseded.
- Operator V3 replaces only selected panel pilots with shallow lens geometry while retaining the named output lens. ENABLE/CLEAR/LOAD are readable. Protected cable routing is inside the cabinet.
- `realism-grouping-access-v1/` contains fresh full, split, operator and side renders with the representative's unnecessary rear storage removed. Other packaging scenes retain their existing furniture pending their own access review.

All artifact paths above are under `rungproof-next/.tools/studio-verification/` in the isolated studio worktree.

The captured set's `render-source-identity.json` records eight presentation/camera source SHA256 values. Those files' last-write times preceded the first capture (2026-10-11 03:56:08 UTC) when this snapshot was taken. This is post-capture source/timestamp evidence. The original launcher did not record its DLL hash, so no exact original binary hash is claimed. Subsequent product geometry remains unchanged; opt-in verifier and evidence tooling have been improved.

The launcher now records exact application DLL, engine and PNG SHA256 values in per-image `.identity.json` files, and refuses qualification if the DLL changes during capture. `realism-capture-identity-v1/bench.scene.png` successfully exercised this instrumentation and was actually inspected by root. Its rendered DLL SHA256 is `24E25B9FF5B64CA9ECF3390AA50DB648DC71906358A3FC7669E5E3946C47819F`. This tooling check does not broaden representative approval or catalog visual acceptance.

## Remaining review gates

Dot actually inspected all four fresh representative images and passed the bounded visual approach/proportion review. Parent/human wider-rollout approval is still pending.

`realism-grouping-native-v5.log` passes the selected recheck after pilot geometry changed. The explicit output-projection fixture exercises the actual `group_ready` binding: false/off, true/emitting, false/off, reset/off. It does not claim an authored controller program test.

Actual door world bounds start `(7.80,0.21,1.04)` with size `(1.72,1.57,0.035)` m. A conservative 90-degree front reserve starts `(7.80,0.21,1.075)` with size `(1.72,1.57,1.72)` m and is disjoint from upright field and environment meshes. This is a static reserved rectangle, not an animated door test.

The declared carton translation envelope starts `(-4.825,0.90,-0.350)` with size `(11.650,0.745,0.719)` m and does not intersect the standing zone. The floor approach from the cutaway front and along the line is visibly clear in the fresh renders. No traversable external route through the decorative warehouse recess is claimed. The 1 m standing zone alone does not establish continuous human clearance. No physical safety, accessibility, ergonomic, commissioning or release approval is inferred.

The original 154-image catalog inspection predates this expanded mounting/access work. It cannot establish visual acceptance of the current all-scene revision. After the representative gate clears, the current 77 scenes require fresh full/split captures and individual visual inspection before completion.
