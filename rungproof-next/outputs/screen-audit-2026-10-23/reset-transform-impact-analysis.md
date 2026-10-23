# Reset transform ordering impact — bounded read-only analysis

No application changes, runtime execution or UI input. This examines the adapter initialization seam exposed by the native parcel fresh-load success versus Reset/repeat stall. Native causality is not inferred solely from source.

## Parcel-specific structural risk

`SceneSimulationRuntime.cs:596–604` restores every authored equipment root position, but root rotation only when `_sequenceRotatedEquipment` contains its name. Controller-owned parcel projection directly writes `_parcelLoads[i].Rotation` (`ParcelTransfer.cs:163`), outside that legacy sequence bookkeeping. `ResetParcelTransferPlant` then reads `DeliveredEquipmentBounds` **before** its final `ProjectParcelTransferPlant` resets the model/rendered rotations (`ParcelTransfer.cs:38–65`). It uses those world-aligned Size.X/Size.Z values as model load dimensions. A previously rotated non-square carton can therefore seed swapped/expanded dimensions even though the subsequent displayed root becomes home-aligned. Resetting position alone cannot fix that geometry snapshot.

The latest native V7 run completed3 on first load, while actual Reset/repeat stalls1 with route0.5/transferTrue/inhibitedTrue. This is consistent with the structural risk, but does not independently establish which parcel/dimension or whether this is the sole cause. Private phase50/class2 was supplied by root's separate live review, not measured by this analysis. Preserve earlier V3/V4 failures separately.

## Cross-scene comparison

| Family / inspected seam | Why it differs or residual candidate |
|---|---|
| Drawbridge | Directly rotated nodes are dedicated BridgeMotionPivot children. Reset model uses declared parameters and projects reset angles, rather than rebuilding load dimensions from those rotated meshes. No same geometry-sampling pattern found; latest descent recovery is separate native proof. |
| Guarded drill / service door / assembly mechanisms | Generic `EquipmentMotionController.ResetMotion()` runs before adapter reset, restoring cached authored node transforms/scales and follower home. Pure models reset from parameters, then project actual position. This protects the sampled mechanism transforms; no same root-rotation dependency identified. |
| CNC tending | Generic robot and equipment ResetMotion precede adapter initialization. Stock projection changes GlobalPosition, not stock root rotation; reset restores equipment positions before stock bounds sampling. Clamp members use once-cached authored transforms. No matching parcel-style changing stock orientation→fresh AABB dimensions path found. This is a structural assessment, not fresh repeated-cycle proof. |
| Jug / inbound scanner | Reset samples delivered load/belt/beam geometry, but these adapters translate load roots without rotating them. Generic nozzle motion is reset before jug tip sampling. Their world-space geometry checks merit preserving existing repeat evidence, but no identical retained-load-rotation path found. |
| Luggage | Reset explicitly sets diverter gate Rotation=Zero **before** corner projection. Bag movement translates the bag; no bag orientation change found. Gate-sensitive geometry sampling has an explicit neutralization unlike parcel. |
| Grouping / palletizer / coating | Authored gate/rod/roller/tool/vane transforms are captured once and reused, rather than recaptured on each Reset from current moved geometry. Reset constructs/resets model and projects cached baseline. Coating workpiece translates; vane's cached home is restored during projection. No matching AABB re-seeding found. |
| Cable / conveyor-zone visual rotations | Reset model/phase values project payoff/roll/drive rotations; these rotations do not feed newly captured transported-load dimensions. Rotation remaining until projection is not by itself equivalent to parcel's geometry initialization risk. |

## Reparenting boundary

Runtime search found no scene-adapter `Reparent` operation. `PalletRobotMotion` and scene composer helpers reparent visual members during assembly; this differs from repeatedly attaching/reparenting transported stock during each cycle. Do not classify all robot scenes as affected based only on assembly reparenting. Cached authored transforms and reset order must be assessed at the actual sampling site.

## Conclusion and scope

The concrete high-risk pattern is **parcel root orientation retained → world AABB sampled as intrinsic dimensions → rotation later normalized**. No second confirmed native defect or equally direct duplicate was established in the inspected adapters. Generic Reset intentionally preserves unrelated workspace rotations/scales, so indiscriminately restoring every equipment transform would risk erasing learner workspace edits. Any future repair should explicitly own baseline geometry for the affected controller adapter or sample intrinsic dimensions in a stable reference frame, then verify fresh-load versus rotated-phase Reset invariance and preserve real workspace transforms. This is an impact analysis, not a new mandatory cross-scene permutation matrix or prioritized remediation plan.
