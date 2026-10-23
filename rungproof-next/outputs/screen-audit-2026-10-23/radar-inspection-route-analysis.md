# Radar inspection advertised action — UI route analysis

Read-only source review; no UI/bridge input, state queries or source changes.

**No finite normal visible UI route was found for `tank-radar`'s advertised inspection toggle.** This is an exposed-action contract/accessibility gap supported by source, not proof that the ghost renderer itself fails.

## Exact chain

- `docs/help/scenes/tank-radar.md` Operator actions advertises **Inspection view: ghost shell and roof**. Scene JSON teaching text explicitly instructs using the inspection view and toggling again to restore opacity.
- `scenes/migrated/tank-radar.scene.json:228–231` declares simulation action `toggle-inspection`, type toggle, SIM point `tank_inspection_view`. The runtime toggle dispatch exists (`SceneSimulationRuntime.cs:680+`), and `SceneSimulationRuntime.RadarView.cs:9–60` changes shell/roof materials and internal surface visibility from that SIM point.
- Only equipment actions in this scene map pump/drain stations (`toggle-pump`, `toggle-drain`). Tank/transmitter have no inspection action and no sceneControls child mapping. `SceneControlInteractor.cs:166–193` creates pickable control bindings only from explicit config.action or sceneControls; it has no Tank/radar/display fallback, double-click inference, or generic inspector command.
- `SimulatorShell.cs:4433` constructs generic OperatorActions with Visible=false. Rebuild at4925–5001 registers action buttons/events but never makes their parent visible. Search of all `_operatorActions` usages found no visibility restoration.
- Guided DO NEXT ACTION does not provide an alternative: it is hidden for noncurriculum scenes (`SimulatorShell.cs:5148+`); its dispatch chooses the five-demo curriculum's next control point. Radar is not one of those demos.
- Top View lists Operator Console, Viewer Hierarchy, Engineering Layout, Immersive Scenario, Scenario+Ladder Split, framing and interface scale (`SimulatorShell.cs:3389+`); none dispatch inspection. Inspector/asset preview routes inspect properties or catalog geometry and do not invoke this action. Source-wide references to exact toggle-inspection/tank_inspection_view identify renderer, declaration and direct `Main.RadarReview` verifier calls, not a menu/context/hotkey route.

`Main.RadarReview` directly calling ExecuteAction proves a testing seam exists, not that a learner can click it. SIM ownership also makes editing/forcing the value in a controller an invalid substitute for testing the advertised presentation control.

## Minimal additional native evidence if needed

One current radar scene frame showing its opaque shell, visible Operator area and `tank_inspection_viewFalse` Signals; one open View menu frame documenting available view commands. Existing native body/transmitter no-effect attempts can be retained as supporting observations only. Arbitrary further clicks, all Inspector permutations, or hidden action invocation are unnecessary: there is no grounded physical mapping to target. Do not label opacity after an unbound body click a runtime material defect. The bounded unresolved item is **advertised inspection action lacks a normal exposed route**; any fix should expose an intentional presentation-only control or correct the advertised contract, preserving the existing physical pump/drain controls.
