# Bounded operator station readability plan

Read-only source/render review, 2026-10-11. Proposal only; no product edits/build/engine. Handle OPEN; material75 HELD. This changes identification, not curriculum or controller behavior.

## Exact map

All source paths below are `rungproof-next/scenes/migrated/<sceneId>.scene.json`. None of these controls has an explicit config.faceLabel. Eight original cycle START actions are legitimate; improve their readable station/function identification without claiming that START directly commands a motor/valve.

| Scene ID | Equipment ID | Exact action/type/point | Proposed passive strip text |
|---|---|---|---|
| lab-2-13-coolant-jug-fill | fill_start | start-fill / start / absent | JUG FILL\nSTART |
| lab-2-14-sump-pump | sump_start | start-sump / start / absent | SUMP CYCLE\nSTART |
| lab-2-17-pallet-robot | robot_cycle_start | start-robot / start / absent | PALLET UNLOAD\nSTART |
| lab-2-20-bottle-shuttle | shuttle_start | start-shuttle / start / absent | SHUTTLE\nSTART |
| lab-2-21-tote-finishing | line_start | start-line / start / absent | TOTE LINE\nSTART |
| lab-2-22-dual-spindle | dual_drill_start | start-dual-drill / start / absent | DUAL DRILL\nSTART |
| lab-2-23-parcel-sorter | sorter_start | start-sort / start / absent | SORT BATCH\nSTART |
| lab-2-24-robot-cnc | cnc_cell_start | start-cnc / start / absent | CNC CELL\nSTART |
| lab-2-18-pallet-pickup | auto_start | start-auto / start / absent | AUTO\nSTART |
| lab-2-18-pallet-pickup | pickup_mode (rotarySwitch) | set-auto / togglePoint / auto_mode | MODE\nAUTO / MAN |
| lab-2-18-pallet-pickup | manual_jog | manual-jog / start / absent | retain existing JOG strip |
| scene-1-conveyor-stop | scene1_station | scene-toggle / toggle / conveyor_running | START / STOP |
| scene-2-conveyor-pusher | scene2_station | scene-toggle / toggle / conveyor_running | START / STOP |

New coverage: twelve controls across eleven scenes, plus retention of one already mapped JOG control. SUMP CYCLE avoids implying a direct pump-run command. The CNC label identifies the requested cell cycle; it adds no interlock or physical commissioning claim.

## Grounded behavior and guards

`src/Scenes/SceneSimulationRuntime.cs:658` dispatches start to StartSequence and toggle/togglePoint to TogglePoint. `src/App/Main.cs:4897` selects controllerStartBinding or controllerStopBinding from the current PLC-owned point state, then pulses the matching BOOL input. The two scene-toggle stations have authored operator.start/operator.stop and conveyor_running, so static START alone omits their real stop role. These are not two independent physical buttons.

Pickup selector source line72 declares rotarySwitch, action set-auto, two positions and initialPosition1. Action toggles auto_mode; selector point binding is auto_mode|selector. AUTO start requires auto_mode=true and pickup_sensor=false; JOG requires auto_mode=false and pickup_sensor=false. Both carry distinct request bindings. Do not assert knob position0/1 correspondence from initialPosition alone: verify actual current selector controller mapping before engraving position-specific marks. Neutral MODE / AUTO / MAN identification is safe; no automatic text-state inference is needed.

Use the existing helper `SceneComposer.InstallationLegends.cs` as sole presentation owner. Its FaceContract dictionary/ValidateFaceAction and post-packing ConfigureMappedOperatorStrips already support exact panel button mapping. Add the eleven button rows with exact scene/equipment/action/type/point guards; additionally guard authored controller request bindings where present (including both start/stop bindings for the toggle stations). Preserve explicit faceLabel precedence and all JSON, IDs, action requirements, source pointBindings, pick targets, lens paths and root transforms. Missing controllerStartBinding on jug/sorter/CNC stays missing: labels must not silently create bindings or make controller-driven operation succeed.

Selector cannot enter current button-only helper: it requires BUTTON_retaining_ring and type switch. Add a separate exact pickup_mode presentation contract/geometry path that measures the retained selector bezel and cabinet door. Guard type rotarySwitch, action set-auto, actionType togglePoint, point auto_mode, selector binding, positionCount2 and initialPosition1; do not weaken all button guards to accept arbitrary selectors. Keep existing JOG support and avoid duplicate identification nodes.

## Geometry and acceptance

Reuse successful panel strip principles: fixed readable glyph scale, .16m two-line height, measured cabinet-door contact, at least .02m head clearance, width bounded by measured head pitch and fascia. Measure text fit; PALLET UNLOAD may need additional horizontal width in its single-control cabinet rather than smaller glyphs or a third line. Pickup's three-control layout must fit MODE/AUTO MAN, AUTO START and JOG at existing pitch without touching knobs/buttons or overlapping neighbors. Hide only exact superseded tiny plate/ink after a successful replacement; do not hide bezel or alter picking.

Native gate must include explicit twelve new associations, exact source action guards, plate bearing/fascia extents/head gap/text fit, retained pick/controller/lens nodes, and rejection of source drift. Actual operator captures for all eleven scenes must show readable function and action at the same framing; pickup should additionally show both mode states and matching separate START/JOG behavior without relabeling source state. The two conveyor stations need stopped/running action checks establishing existing start/stop input selection. Offline source/geometry checks do not prove live PLC or physical safety.

## Actual image review limits

Individually inspected all ten named `all77-remaining-operator-v12/<sceneId>-operator.png` images (eight cycle stations and both conveyor stations), plus `v13-d-lab-2-18-pallet-pickup.operator.png`. V12 frames show tiny generic START plates above otherwise exposed single buttons; the conveyor UI correctly says START / STOP while the physical plate says START. Pickup V13d shows readable JOG but tiny START and no readable mode identification. These historical V12 views ground the defect, not final V13e pixel qualification. Root should capture the exact final candidate after the bounded repair; no new frames were launched here.
