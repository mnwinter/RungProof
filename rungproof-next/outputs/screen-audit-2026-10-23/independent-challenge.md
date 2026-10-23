# Independent challenge: parcel reset geometry proposal

Read-only source review of the reset decision table and regression obligations. No UI, model execution, application/fixture change, Git or D write. This note adds a scope counterexample, not another acceptance gate or a global repair plan.

## Current authored failure: no stronger competing cause found

Native evidence establishes first/fresh-scene success and repeated same-scene AppReset count1 stall. It does not expose stored private dimensions. The source mechanism is credible: ResetSimulation restores root positions but only sequence-registered rotations; controller projection writes parcel yaw directly; ResetParcelTransferPlant reads delivered **world** AABB sizes before projecting new zero-angle model poses. The new model stores those dimensions readonly. Zero-looking reset geometry can therefore coexist with previously rotated dimensions.

I found no source-supported feedback-cache explanation that contradicts that mechanism:

- ResetParcelTransferPlant reacquires parcel nodes, recomputes bounds/heights, reacquires support-belt mesh references and constructs a new model. It does not reuse the old model's parcel Route/Outbound/Arrived fields.
- Generic Reset restores initial points/previous initial values; the parcel scene declares no simulation rules or edgeRules that would re-latch an old receipt/class on the next EvaluateRules call.
- Main.ResetActiveController invokes scene reset before controller Reset/output commit. CommitVirtualControllerSnapshot projects output dictionaries; it does not reconstruct a second correctly sized model after the parcel roots are visually zeroed. Thus later command zeroing does not repair the sampled geometry.
- Table motion controllers reset before parcel reconstruction; cached table-drive/stripe nodes are visual and are not among the parcel-load meshes measured for dimensions. Support belt references are reacquired. No stale cloned-support reference was demonstrated for this unchanged authored scene.

This is source reasoning, not proof that the exact failing native run stored a particular width. The existing real-composition/two-cycle/support-negative regression obligation is appropriately stronger than a pure nominal-dimension test.

## Genuine counterexample to a broader claim: nonidentity scene parent

Restoring parcel **local** home yaw before a **world** AABB measurement is not a general coordinate-frame repair. ParcelTransfer.cs takes homes from local Position.X and constructs support sample vectors directly from model X/Z, while support belts are tested through GlobalTransform inverse and tables use GlobalPosition.

For a scene parent translated1m inX, those model samples omit the parent's translation. For a parent yaw45degrees, a medium footprint approximately0.900×0.799m has a world AABB about1.201×1.201m even when the parcel's local home yaw is0. The world-AABB sizes are then treated as local rigid-body dimensions and rotated again during support sampling. Restoring local yaw alone cannot fix either example. These are analytical source counterexamples, not executed model tests.

The actual authored SceneComposer root is a new identity Node3D; current parcel roots have no declared rotations, shareY1.055 and obtain their intended size through the mapped child model scale. Accordingly, this counterexample does **not** falsify the proposed narrow correction for the observed default scene. Do not expand the present native audit into arbitrary parent-transform permutations. Just avoid describing a local-yaw reset as a universal world/local geometry solution.

## Bounds origin and scale boundary

DeliveredEquipmentBounds merges every descendant mesh's world AABB and consumes Size, while the model places a symmetric footprint around the parcel root. An offset child/unequal-origin clone or decorative mesh could make that envelope differ from the actual body; restoring yaw would not remove an origin offset. No such history-dependent child mutation was found in this controller path, so this is not an alternative diagnosis of the present failure.

Preserve authored scale and unrelated editor-owned transforms. Hard-coding Rotation=zero for every root is broader than restoring only controller-owned parcel home orientation. A saved nonzero home orientation would still need a consistent dimension frame; the current authored model explicitly projects flat yaw-only poses, so general tilted/reshaped workspace support must not be implied.

## Verdict

The proposed narrow ownership/order correction remains plausible for the observed authored scene. The current obligations already cover the meaningful same-instance reset, actual delivered geometry and real support callback; no new mandatory gate is proposed. A future successful repair still needs the existing integrated and native two-cycle evidence. Parent-frame/origin robustness is a limitation on any broader claim, not a reason to discard the confirmed repeat stall or postpone its narrow diagnosis indefinitely.
