# Dot actual V14 operator review

Read `operator-station-helper-implementation-v13f.md` and individually opened all14 actual `v14-<sceneId>.operator.png` files under APP/.tools/studio-verification. No engine, build, input or product edits. Capture identity/native qualification remains separate.

| Exact scene | Actual observation |
|---|---|
| lab-2-13-coolant-jug-fill | JUG FILL / START readable, plate separated above button. |
| lab-2-14-sump-pump | SUMP CYCLE / START readable, no implication of direct pump-run output. |
| lab-2-17-pallet-robot | PALLET UNLOAD / START readable and clear of button. |
| lab-2-18-pallet-pickup | MODE AUTO/MAN, AUTO START and JOG readable. MODE plate appears to crowd or overlap top white selector-ring/detent graphic in projection by roughly3–5px. Verify full visible selector envelope, beyond measured SELECTOR_bezel, before no-overlap acceptance. No ordinal state assertion; knob still visible. |
| lab-2-19-service-door | CLOSED NC INPUT and OPEN NC INPUT readable, separated from pilots/buttons. Displayed closed_limit_nc False/open_limit_nc True agrees with off amber/lit green; this identifies raw channels, not inferred physical endpoints. Left cabinet edge remains wall-occluded; all three button faces and both captions visible. |
| lab-2-20-bottle-shuttle | SHUTTLE START readable, no plate/control overlap seen. |
| lab-2-21-tote-finishing | TOTE LINE START readable; field inspection annotation remains outside this operator view. |
| lab-2-22-dual-spindle | DUAL DRILL START readable; field mechanism outside scope of close station view. |
| lab-2-23-parcel-sorter | SORT BATCH START readable; no invented direct motor command semantics. |
| lab-2-24-robot-cnc | CNC CELL START readable; sidebar honestly retains offline-reference/interlock limitations. |
| lab-5-09-bag-indexing-conveyor | RUN COMMAND OUTPUT and REVERSE CMD OUTPUT readable, separated below respective green/amber pilots. Original START and PAUSE CLEAR retained; selector detent glyphs tiny/low contrast. |
| lab-6-08-hand-dryer | HEATER CMD OUTPUT above amber pilot and BLOWER CMD OUTPUT below green pilot readable; clear of HANDS IN and remaining-time display. Compact blower plate fits without visible clipping. Labels identify commands rather than actual temperature/airflow. |
| scene-1-conveyor-stop | START/STOP readable; truthfully identifies single toggle station. Demo3 header alias retained. |
| scene-2-conveyor-pusher | START/STOP readable, separated above button; full pusher context requires overview. |

## Verdict

All18 new identification texts are readable at displayed1600x900 size. Thirteen stations have no observed new caption overlap; pickup has the specific projected ring/plate concern above requiring measured-envelope review or a small placement correction. Do not claim universal no-overlap until resolved. Still screenshots do not prove mode-state switching, toggle start/stop request selection, mouse picks, reset or source-binding parity.

Most tower-bearing views clip upper caps/red region behind HUD; use overview/details for complete head evidence. Station captions do not close field-detail/purpose requirements, Handle OPEN or material75 HELD. No all77 approval follows.
