# Two-annex implementation handoff V15

Approved measured plan implemented in ONLY src/App/DemoEnvironmentPresentation.IndustrialSix.cs and src/App/DemoEnvironmentPresentation.NextProcessRooms2.cs. Source frozen; root owns combined build, native77 verification and all actual renders. This document is a source handoff, not a passing-render or native-completion claim.

## Frozen files / before provenance

- IndustrialSix.cs before B0FB622A1CE45D2AFC1695111E58FC0859A12C3D22F59BEA6789D7A94A6B775F; final5031C48788A7A88D52E685E4FEB2B456BA500F85ECB7A47D19A1697DAC5666BB.
- NextProcessRooms2.cs before AD0E0C258866A67E2AFDD65DD7B887BE069C82145EEC00FD625193A536ACE8EF; final1CB11F9F38E504C48C43ED7515A930448B08D8B14B209584445DDF01CE3CE44C.
- Exact beforecopies and before-hashes.json retained at .superpowers/sdd/repeat-cycle-execution-tasks/before/purpose-two-annexes-v15 in studio worktree root. Both files were existing untracked studio files; no unrelated file reset, commit or cleanup.
- Narrow before/after no-index whitespace checks exit0 for both files; source diff read. No build/engine/input from this agent.

## Inspection cell contract

Only inspection-side-light changes: outward bay depth2m, same2.4m frontage, same1.2m backdoor and original main-room entrance/breastwork/sill. Main WholeIndustrialFoundation expands outward1.4m as a SINGLE slab; old inspection-only AnnexContinuousFoundation omitted to avoid coincident surfaces. Existing connected cheek/back facade/roof nodes move/extend to approved depth.

Expected new nodes under IndustrialServiceSide/ConnectedPurposeServiceBay:

- InspectionReferenceCounter:1.6x.06x.40m; top1.05/bottom.99.
- InspectionCounterFoot{LeftBack,LeftFront,RightBack,RightFront}:four .06x.03x.06m, bottom-.005/top.025.
- InspectionCounterLeg{same suffixes}:four .035x.965x.035m, bottom.025/top.99.
- PassiveInspectionReferenceTray:.50x.03x.30m, bottom1.05/top1.08.
- InspectionCounterCompleteApproach:metadata-only Node3D at identity transform; localRouteBounds consumed by existing Main.EnvironmentReview parent-global-transform path. Width1.2, height2, both endpoint caps included. Door and sweep use the same inspectionDoorWidth constant. actualWorkingClearDepth1.31, actualDoorWidth1.2, localStandingCenter midpoint of actual rear inner face and counter rear edge. routeScope explicitly exterior-apron→counter access, not main-room control access or physical compliance.

Expected actual WORLD bounds are in measured proposal. Full route X[-9.728001,-7.083001],Z[-2.239766,-1.039766],Y0–2; floorleft-9.828001; exact backdoor sameZedges. Native must report actual bounds rather than trusting metadata. Doorway/sweep sides touch by design: full1.2 width has no lateral tolerance. Any positive volume into frame/jamb is a defect; float-only shared-plane residual must be distinguished without shrinking declared width or excluding obstruction.

## Vision reference-storage contract

Existing OutgoingDockServiceExtension stays3x1.6m with existing frontage/window/cutaway roof/foundation. Local Annex helper returns its already-created Node3D so ONLY vision adds storage; other call sites ignore return and retain existing construction.

Expected new nodes under ProcessSideContext/OutgoingDockServiceExtension:

- ReferenceStorageFoot{LeftBack,LeftFront,RightBack,RightFront}:four .08x.03x.08m, bottom-.005/top.025.
- ReferenceStorageUpright{same suffixes}:four .04x1.465x.04m, bottom.025/top1.49.
- PassiveReferenceShelf0/1/2:1.8x.04x.40m, tops.45/.95/1.45.
- PassiveReferenceTray0/1/2:.40x.06x.30m, bases equal tier tops; staggered along shelf width. Plain trays, no active loads or vision/calibration targets.
- ReferenceStorageCaptionBearing:1.65x.10x.02m, centerY1.36/top1.41 contacts top-shelf underside1.41. Inside frame footprint.
- PassiveProductionPurposeLabel:PASSIVE REFERENCE STORAGE, font24/pixel.0017, on bearing face with .012m standoff; fixed text, no tag/runtime link. Actual renderer must confirm ink fit/readability and bearing attachment.

World storage frame footprint X[-15.20,-14.80],Z[-8.153822,-6.353822]. Lower shelf lies below framed aperture sill. Middle/top shelves should establish recognizable storage in normal view; all3tiers need detail. No new personnel entry or accessibility claim.

## Root verification conditions

Run combined build and existing77 environment/mount/control gates. Fresh full-world mesh dumps must show feet→uprights/legs→shelves/counter→trays and caption→shelf contact, continuous floor under full inspection sweep/endcaps, actual wall opening clear, no retained visible plant/architecture collision. Compare retained machine/control identity/transforms/bindings; ensure all new geometry remains passive sibling environment with no collider/controller/click action. Fresh normal+split+purpose views individually reviewed by root/Dot must establish purposeful visible depth/storage/counter rather than node-name presence. Existing loaded/runtime behavior and material75 hold remain independent. Earlier V14/V15b images are before evidence, preserved.
