# Demo functional acceptance — 2026-10-08

> **Scope correction (2026-10-13):** The records below describe previously
> verified local behaviors only. Demo 5's former palletizer pass does not
> satisfy the newly required PLC architecture: it still has a single
> `gantry_cycle` output and does not prove independently called axis/motor
> blocks, machine-level enable/Stop/Reset/safety-status logic, or visible
> encoder-driven transitions. The requirements at the end supersede the earlier
> “all five rows meet” statement for any overall demo-acceptance claim. These
> initial five rows are not a complete inventory of every learner-facing demo
> in the app; gallery-wide acceptance remains open.

> **Current Demo 5 correction (later 2026-10-14):** The aggregate
> `gantry_cycle`/plant-phase defect has been removed. Main now owns the scanned
> state sequence and calls X/Y/Z axis plus vacuum/gripper FB instances with
> distinct DB101–DB104. The current offline joint audit passes the bounded
> ownership/functional cases recorded at the end of this document. Native visual,
> installed-TIA, real-hardware, and other-demo control gates remain open.

This matrix compares authored ladder execution with scene behavior. Native observations used ordinary local simulation, with the physical PLC disconnected. Deterministic integration uses the same controller/session, input mapping, output projection and scene physics; it establishes modeled behavior and transforms, not physical equipment performance. Native screenshots establish the visible behavior separately.

| Demo / loaded logic | Expected output and scene sequence | Observed native behavior | Timing, feedback, Stop and Reset evidence |
| --- | --- | --- | --- |
| 1 / Press_Count_Main, CTU preset 3 | Each operator pulse counts once; lamp off for counts 1–2, on at 3; idle does not recount. | Native process 28244: first two pulses lamp off; third pulse scan1568 showed threshold_lamp TRUE and green lamp. | Main.PressCountWorkflowReview.cs drives actual scene actions through physics, checks count and projected lamp each pulse and idle. Native Stop2246 cleared lamp; Reset0 restored initial scene. Counter memory is retained by Stop and cleared by Reset. |
| 2 / Delayed_Lamp_Main, TON 2 s | Maintained request waits 100 ordinary 20 ms scans before lamp turns on; OFF resets timer/output. Stop cancels delay; retained ON after Run needs a fresh full delay. | Process28244: request TRUE/lampFALSE scan1143, later lampTRUE/visible green1604; OFF2784 lampFALSE. These screenshots do not measure the exact two-second boundary. | Main.TimerLessonReview.cs tests actual authored program/scene at scan99 ET1.98/outputFALSE and scan100 ET2/outputTRUE, OFF, Stop, restart and Reset. Native Stop3409, Reset0. Exact timing is model scan evidence, not wall-clock/native pixel timing certification. |
| 3 / Height_Sorting_Main, 600 mm boundary | App Run alone is idle. System Enable plus machine Start admits the repeatable 420/820/600 mm sequence. Cartons above 600 mm divert to the side line; cartons at or below 600 mm pass the main line. Stop, simulated trip, overload, and Reset are scan-visible. | Current headless `--verify-demo-sequence` passed all 25 checks: 420 mm main, 820 mm side, 600 mm main, PLC pusher command on the tall carton, machine Stop pose hold, trip fresh-start inhibition, Reset, and repeat. | The authored ladder owns run state, compare, divert memory, commands, FB calls, and faults. `ConveyorPlantModel` only integrates commands and publishes height, limits, and exit sensors. This is offline modeled evidence, not native visual or safety proof. |
| 4 / Batch_Process_Main, parameterized FB/FC and DB101 | Valid type and count request gate fresh detection edges. `FB_ValidPalletCounter` stores its CTU in Static `CountState` within assigned DB101 and copies typed count/done outputs to Main; `FC_BatchValidation` gates validity. | Current controller and five-demo checks counted 1–5, rejected invalid/held events, published same-scan DINT count, retained state across Stop, cleared on machine Reset, and began a repeat batch at 1. | The real instance DB, typed bindings, FC call, monitorable outputs, and restart behavior are source- and scan-verified. This is a count/inspection lesson, not pallet motion. |
| 5 / Palletizing_Cell_Main, Main plus axis/gripper/encoder FBs | Fresh Start with pallet permissive commands pickup, attachment, measured transfer, release, and home. Main consumes encoder-FB position/home/fault outputs and process feedback to advance four placements. | Current `--verify-five-demo-learner` passed the four-carton 20 ms scan trace through steps 10–100, all six axis directions, four placement/home edges, Stop, trip, permissive loss, Reset, wrong-program, and invalid-current cases. | X/Y/Z axis DB101–DB103, gripper DB104, and X/Y/Z encoder DB105–DB107 are distinct. The plant remains kinematic/feedback-only. Offline success does not prove native appearance, installed TIA parity, hardware, or safety. |

## Current verification records

- `.tools/functional-final-controller.log`: 149 passed, 0 failed; no real PLC transport constructed or connection attempted.
- `.tools/functional-final-app-shell.log`: PASS, all five demo scene/title/program/interface selections matched.
- `.tools/functional-final-virtual-controller.log`: PASS, scene feedback, output force, Stop override and Reset.
- `.tools/functional-final-ladder-editor.log`: PASS, focus-owned shortcuts, editing, persistence, watch, validation and interaction regressions.
- `.tools/demo-sequence-integration.log`: 16 integration checks passed, including the previously missing clean Demo3 input→motion→sensor route.
- `.tools/demo-functional-timers.log`: exact modeled timer boundaries and Stop/restart/Reset checks.
- `.tools/demo-functional-scene-geometry.log`: PASS, including Demo1 action/count/lamp and Demo4 count/readout integration; bounds screening is separate from native visual evidence.
- `.tools/demo-functional-palletizer.log`: 60 successful checks, 2,576 ordinary 20 ms cycle samples, failures 0. Modeled attachment, transfer feedback and phase Stop/resume/Reset passed.

The five rows above passed only their earlier bounded local functional checks.
Those results do not pass the new machine-control/architecture gate below,
especially Demo 5's explicit PLC-owned sequencing requirement. The strengthened
Demo 3 check explicitly verifies output/seal TRUE on the first geometry-derived
photoeye scan and FALSE on the immediately following controller scan. The
timer boundaries and former palletizer phase checks are deterministic model
integration; neither proves the new PLC ownership architecture.

Source checkpoint was d8598d1 plus the reviewed local functional and QA changes. Native process28244 included the force-panel and shortcut corrections; process31316 also included the input-force release correction. The new Demo3 verifier changes QA dispatch only. The preserved all-angle motion PNG sequences have their own older frozen build identity; their final visual review must retain that provenance.


## Final checkpoint verification

Fresh current-source rerun logs use `.tools/checkpoint-final-*.log`: build zero warnings/errors; controller 149 PASS/0 FAIL; app-shell, virtual-controller UI and ladder-editor PASS; Demo3 16 integration checks PASS; exact timer boundaries PASS; full scene geometry/workflow verifier PASS; palletizer 55 checks/2,576 modeled samples/0 failures; capture sequence/sheet/AVI Python tests 29 PASS. This ordinary headless palletizer rerun omits --visual-scene-review; the earlier 60-check run also exercised five opt-in review-clock assertions. No real PLC transport was constructed or connection attempted. The app-shell negative missing-workspace test retains its expected WORKSPACE_LOAD_FAILED warning. These automated reruns supplement the native observations above and do not replace them.

All 25 preserved native motion cases are now visually accepted for their frozen captured-source scope, with 34,980 PNGs and complete per-frame geometry/load mappings. See NATIVE_MOTION_ACCEPTANCE.json for capture/current build identities, per-case review hashes, retained artifacts and final verification-log hashes. Exact reconstruction proves all 93 scene source files unchanged; current editor/UI/force/QA changes are verified separately.

## New machine-control acceptance gate

The earlier table is not a control-architecture audit. Before representing any
machine demo as complete, record each operator control and trace it through its
scene binding, symbolic input, rung/block, output, sensor feedback,
stop/reset response, and fresh-restart behavior. The current source audit did
not find a Demo 3 safety-trip input or operator control. Add and test that
missing route in the current build; a check from a different scene or older
build cannot stand in for it. Repeat the inventory across the full demo
gallery, not just these five starter lessons.

| Demo | Required machine-control proof |
| --- | --- |
| 1, press-count lamp | Visible pulse and machine Reset controls; prove the operator input, counter/output rung, and reset result. Do not add an E-stop to this non-motion exercise. |
| 2, delayed lamp | Visible request, Stop, and Reset controls; prove the timer and output de-energize/reset correctly. Do not add an E-stop to this non-motion exercise. |
| 3, conveyor | Add a learner-visible simulated safety-trip status/control; prove Stop, Reset, blocked restart while safety is tripped or the photoeye is blocked, and fresh restart after the cause is clear and reset is acknowledged. |
| 4, batch/pallet count | Visible request/Stop/Reset behavior; prove gated counting, Stop behavior, count retention/clear policy, and no fabricated detection edge. Apply a safety-trip control only if the scene has actuator hazards. |
| 5, palletizing gantry | Implement the architecture in `TIA_PORTAL_PARITY_AUDIT.md`: System Enable; independently called axis/motor FB instances with separate DBs; vacuum/gripper FB; safety-status interlock; visible seal-in/sequence logic; separate Stop, Reset, and trip; encoder/sensor transitions; no background phase sequencer. |

RungProof's app-level Run/Stop/Reset lifecycle is not a replacement for
machine-level operator controls. A simulated safety input/status is an
instructional interlock only and must never be represented as a certified
safety function.

### PLC ownership and teaching progression gate

Apply this gate to **every learner-facing demo**, not just the palletizer or
the five starter lessons below. A scene plant
may integrate commanded motion and publish measured switches, encoder values,
and process feedback. It may not choose the next machine state, fabricate a
completion, or convert a generic request such as `gantry_cycle` into a hidden
sequence. State transitions, permissions, interlocks, timers, counters,
faults, reset behavior, and actuator commands must be attributable to the
scanned ladder or called PLC blocks. If a lesson intentionally models a
device-local behavior outside the PLC (for example, continuous mechanical
coast-down), document that behavior and keep it bounded to the plant model.

The five authored demos form one progressive curriculum. Complexity must grow
through added control concepts and plant behavior, not by inflating tag or rung
counts. Every signal must correspond to a real modeled control, sensor,
actuator, process value, or PLC state used by the program. Each demo must be
repeatable from its own machine-level controls; app playback Reset is not a
substitute for a PLC reset rung.

| Demo | Intended progression and meaningful behavior | Hard acceptance check |
| --- | --- | --- |
| 1 — Press count | First scan-level lesson: a momentary operator pulse increments a CTU; the threshold drives a visible lamp; a dedicated machine Reset clears the counter and output so the exercise can be repeated without resetting the scene. | Press 1–2 times, verify lamp off; press 3, verify lamp on; pulse Reset and verify CV/lamp clear; repeat the full cycle. A held button counts once per accepted rising edge. No E-stop is added to this non-motion exercise. |
| 2 — Delayed lamp | Add maintained request, a TON, elapsed-time observation, a distinct Stop and machine Reset. Teach request drop, Stop, and Reset effects as separate control paths. | Verify the exact scan boundary, request-off timer reset, Stop output removal, Reset timer clear, and a repeat cycle without app Reset. No E-stop is added to this non-motion exercise. |
| 3 — Height sorting pusher cell | Replace the stop-only lesson with a real material-routing decision. A measured carton height is compared against a documented threshold in LAD. Tall cartons are diverted by a pusher to a side takeaway line; shorter cartons continue on the main line. Add feed/transfer sensing, pusher extended/retracted feedback, system enable, Start/Stop/Reset, and a simulated motion-trip interlock with fresh-start inhibition. | Exercise short, tall, and boundary-height cartons. Trace measured height → PLC compare/state → conveyor and pusher FB/DB outputs → plant movement → destination feedback. Verify short cartons pass, tall cartons transfer once, pusher retracts before the next feed, blocked sensors inhibit restart, trip requires clear+Reset+fresh Start, and Reset restores a repeatable test state. The simulated trip is not safety-rated. |
| 4 — Batch inspection and accumulation | Add reusable FB/FC boundaries, typed interfaces, instance state, validated product inputs, edge qualification, batch target/ready logic, and PLC-owned count publication. The physical lesson remains a count/inspection cell, not an invented pallet-moving machine. | Verify invalid products do not count; enabling a permissive while detection is held does not invent an edge; only a new accepted event increments the count. Stop policy and Reset policy are explicit, repeatable, and visible; FB instances are called from Main with inspectable DBs where stateful. Verify batch completion and start a second batch using machine Reset. |
| 5 — Integrated palletizing cell | Capstone: multi-axis motion, measured encoder/limit feedback, gripper process feedback, multiple FB instances with distinct DBs, coordinated sequence in Main, permissives, faults, layer pattern/count, and recovery. I/O scale follows the actual equipment; no arbitrary tag target. | Prove each sequence transition is decided by scanned LAD and called blocks; each actuator command reaches the plant and feedback returns on a later scan. Exercise all axes/directions, grip/release, four placements, Stop/restart, trip/permissive/fault inhibition, Reset and a second complete layer. No plant-owned phase enum or high-level `gantry_cycle` command may sequence motion. |

The catalog currently contains five authored demo entries. The demo menu,
checked-in project fixtures, scene bindings, PLC tags, and functional checks
must all describe the same five projects. Any additional practice scenes in the
larger gallery are not silently counted as authored demos or used as a proxy
for one of these five acceptance rows.

### Static source inventory — current implementation

This is a static source review, not a running-scene or functional pass. It
records the current source state; every behavior remains open until its
controls, ladder scans, scene response, and feedback are verified together.

| Demo | Source-confirmed baseline and gap |
| --- | --- |
| 1 — Press count | `pulse_received` drives the CTU and `threshold_lamp`; the scene has a pulse button, lamp, and `reset_pressed` input/rung. The focused workflow review exercises reset and a repeated count cycle. No E-stop is needed for this non-motion exercise. |
| 2 — Delayed lamp | `timer_request` drives TON and `delayed_lamp`; visible machine Stop/Reset inputs clear the timer and disarm restart until a fresh request-off/request-on cycle. The timer review exercises exact scan boundaries and repeat. No E-stop is needed for this non-motion exercise. |
| 3 — Height sorting pusher cell | Authored project targets `scene-2-conveyor-pusher`; the 600 mm compare, 420/820/600 mm route sequence, main/side feedback, machine controls, trip/overload recovery, pusher/conveyor FBs, and DB101/DB102 passed current offline verification. Native visual inspection remains separate. |
| 4 — Batch inspection and accumulation | The counter is now a parameterized FB with Static CTU state in assigned DB101, explicit typed inputs/outputs, a called FC validation stage, correct raw detection-edge qualification, machine controls, and repeatable Reset behavior. The learner-facing `NEW PALLET` input is a momentary event: one press produces at most one accepted rising edge and releases automatically, while TYPE VALID and COUNT REQUEST remain maintained permissives. Current offline controller and five-demo checks pass. |
| 5 — Palletizing cell | Main owns the numeric sequence. Axis/gripper calls use DB101–DB104; three encoder FB calls use DB105–DB107 and pass measured position/home/fault status into Main. Current offline controller, palletizer, fixture, and five-demo checks pass; native and installed-TIA evidence remain separate. |

The specific Demo 5 plant interface and scene controls are also documented in
`docs/TIA_PORTAL_PARITY_AUDIT.md`. Static inspection alone does not establish
which robot/auxiliary assets are visible in the user's current build; retain
the per-asset command/feedback audit as a required native visual check.

Keep the app's Run/Stop/Reset controls separate from machine Start/Stop/Reset;
neither set may impersonate the other. Add a simulated safety-trip control only
to a lesson with a meaningful motion hazard or safety-interlock objective. The
simulator may expose a non-certified safety-status input for instruction, but
must not teach that ordinary ladder code is a substitute for a safety-rated
relay/PLC and validated safety design. Siemens describes emergency-stop
shutdown through fail-safe outputs with feedback checks before restart in its
[Emergency stop example](https://docs.tia.siemens.cloud/r/fail_safe_modules_manual_collection_enus_20/comprehensive-information/safety-engineering-with-as-interface-in-the-simatic-et-200sp-including-typical-circuit-diagram/circuit-diagram-example/operating-principle/emergency-stop?contentId=H766T6VTo8qs9OaxLMoxhA);
RungProof's software-only status bit must remain clearly labeled as a teaching
simulation, not an implementation of that fail-safe function.

The network renderer has an independent TIA presentation gate: every network's
first rung begins at the left power rail; branches split and rejoin as connected
rung topology; there are no detached inner rails, stubs, or conductor gaps.
Operand representation and supplementary tag information are independent
settings. When tag information is hidden, remove optional labels such as
`pick_timer.Q` and `pick_timer.ET` while retaining actual instruction pins,
operands, and the instance DB heading where TIA displays it. Do not mark these
criteria passed from source inspection alone; require same-state captures from
the running RungProof editor and the user's installed TIA version.

### User evidence added 2026-10-13

The latest request tightens the preceding matrix into a hard acceptance gate:

- Every demo needs visible, machine-level controls for its lesson, including a
  logic-connected reset. Motion demos need machine Start/Stop, System Enable,
  explicit permissives/interlocks, trip handling, and fresh-start inhibition.
  App playback controls do not count.
- Each separately controlled motor/axis in an actuator demo must be represented
  by an inspectable PLC block/FB with its own instance DB and called from Main.
  Demo 5's scanned sequence must use encoder/limit and gripper/process feedback
  to decide the next commands. `PalletizerPlantModel` may model motion and
  sensors; it may not sequence the gantry or choose placement steps.
- Use an instructional safety-trip input only for a lesson with an actuator
  hazard. Identify it as a simulation; standard ladder logic and this offline
  controller are not a validated E-stop or safety function. This distinction
  also avoids a meaningless E-stop control on non-motion counter/timer lessons.
- Close each screenshot issue against matched states in the running RungProof
  editor and installed TIA: connected network/branch origins, hidden versus
  shown supplementary tags, and the TON's assigned/unassigned PT plus aligned
  Q/ET pin rows. Screenshots and source review must be tracked separately from
  scan/scene functional evidence.

The older functional passes above remain historical evidence for their bounded
behaviors only. They do not pass these new architecture or TIA visual gates.

### Demo 5 current offline implementation evidence — 2026-10-14

`--audit-palletizer` now passes with 0 failures across 2,457 ordinary 20 ms
controller/plant samples. One Start executed four placements through visible PLC
steps 10–100. The trace observed X/Y/Z positive and negative commands, vacuum
attachment/release, placed counts 0–4, four physical home-return edges, and a
terminal home state with all commands off. Separate cases passed machine Stop,
fresh restart, simulated-trip inhibition, pallet-permissive loss, machine Reset,
opposed-command rejection, and vacuum-loss fault handling. The build is clean
and the virtual-controller/editor suite passes 359/359.

The plant no longer contains a palletizing phase enum and no `gantry_cycle` tag
exists in the authored project or scene contract. Its remaining behavior is
bounded kinematic integration, material attachment/release, automatic magazine
presentation, and measured feedback. The full five-demo learner regression now
passes with zero failures. A native station audit also passes with all six
learner controls mounted and bound on the cabinet, and the normal-renderer
initial/control and completed-state captures were visually inspected. This
passes the local source/scan/scene/control-station slice for Demo 5, but not
installed-TIA parity, detailed visual acceptance of every Main/FB/instance-DB
view, live PLC execution, physical motion, or safety validation. Demos 1–4 still
need their current machine-control/reset acceptance work, so all-demo
acceptance is still **OPEN**.

The 2026-10-14 screenshots further confirm the required checks for connected
network origins, hidden/shown supplementary tag information, TON Q/ET row
alignment, and an explicitly unassigned PT operand. See the dated evidence
section in `TIA_PORTAL_PARITY_AUDIT.md`; those captures are references, not proof
that the current RungProof view passes.

### Demo 4 current production-path screen evidence — 2026-10-21

The deterministic `operator-demo4-active` review state now advances only the
same production **NEXT ACTION** control exposed to the learner. It loads and
runs the current Demo 4 draft, issues machine Start, enables Type OK and Count
Request, submits five momentary New Pallet edges, performs the required machine
Reset, starts again, and submits the first valid event of batch two. The route
is idempotent so repeated capture initialization cannot advance the lesson a
second time.

The inspected 1600×900 normal-renderer capture
`outputs/demo4-current-functional-pass.png` shows the real completion dialog,
local runtime RUNNING, maintained permissives ON, and the captured proof:
first batch count 5, machine Reset to 0, and batch-two count 1. The focused
capture now waits for the route's explicit prepared state, preventing the
former in-progress fourth-event frame from being saved as evidence. The focused
build passed with 0 warnings and 0 errors. This is local production-handler,
offline scan, and rendered UI evidence; PLC transport remained disconnected.

### Demo 1 current production-path screen evidence — 2026-10-21

Demo 1 no longer awards a curriculum pass at the first CTU threshold alone.
Its session gate now requires first count 3, the dedicated machine Reset to
publish both lamp OFF and count 0, and a second three-edge count. The progress
bar distinguishes first-count, reset, and repeat-count phases. Pass evidence is
deferred until the controller CTU snapshot and the visible published count
describe the same accepted scan, preventing a misleading `CV=2` / visible
`COUNT 3` dialog.

The idempotent `operator-demo1-active` review state uses only the learner's
production **NEXT ACTION** control to load/run, submit the two three-edge
cycles, and operate machine Reset. The inspected 1600×900 normal-renderer
capture `outputs/demo1-passed-reset-repeat.png` shows the real pass dialog with
`press_count=CV=3 Q=TRUE`, `press_count_value=3`, the threshold lamp ON, and
recovery proof `first count 3 + machine RESET to 0 + repeat count 3`. The
focused build passed with 0 warnings and 0 errors. PLC transport remained
disconnected.

### Demo 2 current production-path screen evidence — 2026-10-21

Demo 2 no longer awards a curriculum pass when the delayed lamp first reaches
Q. Its session gate now requires a complete first 2000 ms TON, a dedicated
machine Stop with the lamp de-energized, machine Reset with ET returned to 0,
REQUEST OFF to clear the maintained command, REQUEST ON again, and a second
complete 2000 ms TON. The progress presentation distinguishes all five phases.

The idempotent `operator-demo2-active` review state drives the same production
**NEXT ACTION** control and waits on accepted runtime state between each phase.
The inspected 1600×900 normal-renderer capture
`outputs/demo2-passed-stop-reset-repeat.png` shows the real pass dialog with
`delay_timer=ET=2000ms Q=TRUE`, visible elapsed 2000, delayed lamp TRUE, and
the complete Stop/Reset/request-clear/repeat proof. The focused build passed
with 0 warnings and 0 errors. PLC transport remained disconnected.

### Demo 3 current production-path screen evidence — 2026-10-21

Demo 3 no longer awards a curriculum pass after the three carton routes alone.
Its session gate now requires the 420 mm main-line route, 820 mm side divert,
and inclusive 600 mm main-line route, followed by machine Stop/fresh Start,
simulated trip clear/machine Reset/fresh Start, and simulated drive-overload
clear/machine Reset/fresh Start. The progress presentation names each recovery
phase, and PLC-owned `machine_run` gates are read from the accepted controller
snapshot rather than the scene's projected copy.

The idempotent `operator-demo3-active` review state uses only the learner's
production **NEXT ACTION** control. Its capture is held until the full recovery
route explicitly reports prepared/passed, preventing a fixed-frame screenshot
from recording an in-progress state. The inspected 1600×900 normal-renderer
capture `outputs/demo3-passed-full-recovery.png` shows the real pass dialog,
`machine_run=TRUE`, both simulated faults OFF, and recovery proof `three carton
routes + machine STOP/restart + trip clear/RESET/restart + overload
clear/RESET/restart`. The focused build passed with 0 warnings and 0 errors.
This is local production-handler, offline scan, and rendered UI evidence; PLC
transport remained disconnected.

### Five-demo cumulative production-path evidence — 2026-10-21

The deterministic `operator-five-demo-active` route now runs Demos 1–5 in one
continuous curriculum session. It changes scenes through the normal scene
request path, invokes each lesson's idempotent production **NEXT ACTION** route,
and refuses to advance until that lesson is present in the session completion
set. The generic screenshot timer is held until all five lessons report passed.

The inspected 1600×900 normal-renderer capture
`outputs/five-demo-current-complete.png` shows the application's own
**Five-demo curriculum complete** dialog, `All five demos passed this session`,
and the bottom status `DEMO 5/5 · 5 PASSED`. The final proof remains the real
Demo 5 scan with `placed_cartons=4`, `layer_complete=TRUE`, and its complete
recovery history. This current-build rerun followed the Demo 3 infeed and Demo 4
capture-timing corrections. The focused build passed with 0 warnings and 0 errors. This
is cumulative local production-handler/offline-scan/rendered evidence, not
installed-TIA, OS pointer, live-PLC, commissioning, or safety proof.

### Demo 5 current production-path screen evidence — 2026-10-21

Demo 5 no longer treats a Reset and repeated layer alone as the full recovery
exercise. After the first PLC-owned four-carton layer, the learner must Reset
the completed layer, restart, prove machine Stop/fresh Start, prove simulated
trip clear/Reset/fresh Start, remove and restore the pallet-position permissive,
Reset again, issue a final fresh Start, and complete the second four-carton
layer. The ordering matches the authored PLC: `layer_complete` inhibits a new
Start until the completed layer is Reset.

The idempotent `operator-demo5-active` review state advances only the learner's
production **NEXT ACTION** control and waits for each PLC-owned state transition
and measured pick/place sequence. The inspected 1600×900 normal-renderer
capture `outputs/demo5-passed-full-recovery.png` shows the real pass dialog with
`placed_cartons=4`, `layer_complete=TRUE`, both recovery inputs restored, and
the complete two-layer Stop/trip/pallet-loss recovery proof. The focused build
passed with 0 warnings and 0 errors. This is local production-handler, offline
scan, and rendered UI evidence; PLC transport remained disconnected.
