# V16 bounded passive-purpose preservation review

Read-only review, 2026-10-11. No source edits/build/engine. Evidence: industrial-mount-evidence-identity-review-v16.json and passive-cabinet-ledger-check-v16.json beside this report.

425/425 current frozen inputs match; both inventory hashes match; frozen/current DLL **03D36DF05C938EF51650AB9FC56A98EF56189498626E87F4FBBA009D2F228328**. All15 expected capture sidecars were available at the snapshot: image/assembly/engine hashes and unchanged-during-capture identity all match, zero capture ERROR/Exception/FAIL. File availability does not independently establish batch terminal exit. Visual review remains separate.

Actual environments-v16.log terminal: PASS77, exact ledger, sibling hierarchy, world axes, restore pass, runtime unchanged; zero ERROR/Exception/FAIL. Its ledger documents generated geometry/opening checks, not physical clearance acceptance.

## Cabinet geometry independently checked

| Exact cabinet | Original retained world minimum | Original retained world maximum | Actual detail meshes outside envelope |
|---|---|---|---|
| Drive ClosedCommunicationsServiceBox |(-7.644,.925,-2.0775) |(-7.444,1.675,-1.5275) |0 of4 |
| Chicken ClosedPackagingSupplyCupboard |(-8.156,0,-3.551236) |(-7.856,1.4,-2.951236) |0 of4 |

These bounds reconstruct the original .55×.75×.20m and .60×1.4×.30m bodies in their rotated world frame from the unchanged outward/back face and X/Y bearing. Both door hinges, inset door and pull remain inside, using .5mm tolerance for float arithmetic. Shell retains original outer/back face and vertical bearing; Chicken bottom remainsY0. Shell inward/front plane25mm inset and door rear26mm inset now overlap1mm, closing the prior3mm air gap. Door front10mm inset; caption9.5mm inset places its text .5mm outward from door surface while still inside original envelope. Actual transform signs orient detail fronts inward toward the annex mouth, not the back wall.

Main.EnvironmentReview.cs now also measures each caption's font width and multi-line height, transforms the ink AABB and verifies original-envelope containment. Native PASS proves that bounded caption containment check ran successfully; it does not independently establish pixel readability or perfect typography.

## Board association and unchanged functional source

Actual ledger NorthCallBoard backplate worldX range−2.35…−.95 (center−1.65), SouthCallBoard .95…2.35 (center1.65). This matches current mounted North-before-South world-X order. Teaching-suite association code reads actual attached controls/backplates, validates exact source IDs/actions/faces/unique PC BOOL bindings, and changes only passive board names/text/provenance metadata. Its temporary renaming avoids identity/name collision. No board or field transform is changed.

V14b→V16 freeze comparison identifies nine changed inputs: IndustrialSix, NextProcessRooms2, ProductionRooms, RemainingProductionNine, TeachingSuites, Main.ControlInstallationCapture, Main.DemoEnvironment, Main.EnvironmentReview and capture_demo_nonfocus.ps1. No new source inputs. All scene/catalog inputs, all37 SceneSimulationRuntime inputs, all56 Workspace inputs and all26 Controller-named inputs match their V14b hashes. This is source-preservation evidence; clean environment77 is not a substitute for every learner regression gate.

No concrete footprint/door-bearing/caption-envelope defect remains in the inspected ledger and source. Actual15-frame visual acceptance belongs to the separate rendered review. Handle OPEN; material75 HELD.
