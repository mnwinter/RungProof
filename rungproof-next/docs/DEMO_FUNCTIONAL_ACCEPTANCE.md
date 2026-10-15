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
| 3 / Conveyor_Sequence_Main, seal-in and photoeye inhibit | Run alone idle. Start pulse seals output; actual carton travel blocks beam; next scan removes seal/output and carton holds. Stop pulse before beam stops travel. | Rebuilt process31316: Run40 idle594; Start1270 outputTRUE/position0.39/photoeyeFALSE and carton displacement. At1761 position0.50/photoeyeTRUE/outputFALSE; unchanged2373. | Main.DemoSequenceReview.cs checks real carton transform and visible beam nodes, released Start/no forces, next-scan feedback, early Stop pulse, pose hold and controller Stop/Reset. Native Stop3560 retained beam carton; Reset0 restored position0/beam clear/outputFALSE. |
| 4 / Batch_Process_Main, OB/FB/FC | Valid type and count request gate fresh detection edges; enabling a gate with detection already held must not manufacture an edge. Count/readout advances to5, validity lamp turns on. | Process28244: invalid detection count0; enabled gates with held detection remained0; fresh edges counted1–5, held input retained1, threshold scan9427 showed validTRUE and green lamp. | Main.TransferCartonReview.cs checks actual action→same-scan numeric output/readout, held detection, loss of validity and retained counter memory. Native Stop11338 outputcount0/validFALSE; Reset0 initial inputs/count. This is a manual detection/count lesson, not a commanded pallet transport animation. |
| 5 / Palletizing_Cell_Main, OB/2FB/2FC/2DB | Fresh Start with pallet permissive commands pickup, vacuum attachment, carry, release and home. Placement feedback counts real transfers; four complete the layer. Lost permissive holds motion; restore alone cannot restart. Full layer rejects fifth Load. | Process28244: four cartons visibly retained, placed1/2/3/4, cycleinactive/faultFALSE, fourth layer_completeTRUE at13614. Missing permissive blocked second Start6114; restore6900 idle, fresh Start7616 cycled. Fifth Load22016 explicitly blocked; Start22374 no extra placement. | Main.PalletizerAudit.cs drives actual loaded ladder and plant, checks attached carton/tool alignment, away/place/completion/home feedback and count after each transfer, Stop in pickup/carry/release/return, explicit resume and Reset. Native Stop22638 outputsFALSE/placed4 retained; Reset0 initial home/placed0. Native endpoint screenshots are supplemented by controller-driven phase/transform checks. |

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

The five starter lessons below should show a clear progression rather than
hiding their learning objective in scene code. Extend the same PLC-ownership
and appropriate-control audit to each additional demo in the gallery:

| Demo | Learner-visible control and PLC lesson | Hard acceptance check |
| --- | --- | --- |
| 1 — Press count | Machine-level pulse/count/reset controls; CTU owns the count and lamp decision. | Operate each control and trace it through the input tag, scanned counter/output logic, and lamp. Reset must clear the counter through the authored project. |
| 2 — Delayed lamp | Request, Stop, and Reset; TON owns the delay and output decision. | Prove the timer starts, resets on the defined condition, and cannot be cleared only by app lifecycle controls. |
| 3 — Conveyor | Start/Stop/Reset plus simulated safety-trip status; seal-in and restart interlocks own conveyor permission. | Trace safety trip, blocked restart, reset/clear, and fresh start through ladder scans and measured conveyor/photoeye feedback. The safety simulation is not safety-rated. |
| 4 — Batch process | Start/Stop/Reset and explicit type/count permissives; visible FB/FC calls own edge qualification, counting, and publication. | Operate permissives and reset; prove held detection does not fabricate edges and no background script performs the count or decision. |
| 5 — Palletizing cell | System Enable, Start/Stop/Reset, simulated safety status, axis permissions/interlocks, and separate X/Y/Z and vacuum/gripper FB instances with DBs. | Main owns sequence and each scanned motor command. Encoder/limit and gripper feedback advance it. Trace stop, permissive/safety loss, reset/restart inhibit, pickup, four placements, and layer completion. No plant-owned phase enum or `gantry_cycle` request may select the movement sequence. |

### Static source inventory — current implementation

This is a static source review, not a running-scene or functional pass. It is a
baseline for the changes above; each row stays open until its controls are
operated and the ladder, accepted scans, scene response, and feedback are
inspected together.

| Demo | Source-confirmed baseline and gap |
| --- | --- |
| 1 — Press count | `pulse_received` drives the CTU and `threshold_lamp`; the scene has a pulse button and lamp. No machine Reset input/rung is authored. No E-stop is needed for this non-motion exercise. |
| 2 — Delayed lamp | `timer_request` drives TON and `delayed_lamp`; the scene has a selector and lamp. There is no distinct machine Reset control/rung. No E-stop is needed for this non-motion exercise. |
| 3 — Conveyor | Main has Start/Stop seal-in and photoeye logic and the plant follows `conveyor_running`. System Enable, Reset, safety-trip/restart inhibit, motor fault feedback, and a dedicated motor FB/instance DB are absent from the current authored logic. |
| 4 — Batch process | Validation and edge-qualified counting use FB/FC logic, but no Reset input/rung is authored and the legacy FB call has no assigned instance DB. This scene is a count station, not a pallet-motion cell. |
| 5 — Palletizing cell | **Superseded baseline:** Main formerly latched `gantry_cycle` and `PalletizerPlantModel` owned the phases. Current source uses a visible numeric sequence, machine controls, encoder/process transitions, three X/Y/Z axis FB instances, one vacuum/gripper FB instance, distinct DB101–DB104, and six direction outputs. See the dated implementation evidence below. |

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
