# Seven legacy sequence scenes: static plant ownership review

This is a source-supported coverage concern, not a confirmed runtime defect. No new UI test or application edit was performed.

All seven scenes remain simulation.type=sequence; none opts into controllerSequenceCommand or one of the dedicated command-driven plant branches in AdvanceSimulation. Their newly bound start actions can request the selected ladder, but that alone does not start legacy scene steps. AdvanceSimulation returns when _activeStepIndex<0. ProjectReferencePositionFeedback explicitly returns under UsesExternalClock or selected external playback. Point bindings animate components/readouts from current commands; they are not proof that transported-object position, limits or process sensors advance. Do not restore scene-sequence startup from a START request as a shortcut: that would place sequence decisions outside LAD.

| Scene | PLC actuator image | Plant-owned physical integration / feedback needed if gap reproduced |
|---|---|---|
|2.11 inbound stop|conveyor_run|Integrate tote travel while conveyor runs; derive scanner beam, scanner process completion (explicit dwell contract), SIM completion/color from physical state.|
|2.12 lift|lift_up/lift_down|Integrate bounded lift position; derive top/bottom PC limits. Conflict must hold/report, never choose a direction silently.|
|2.13 fill|conveyor_run/fill_valve_open/fill_skid_run|Move jug only from conveyor command; integrate fill only at station with authorized valve/pump conditions; derive presence/high probe and displayed percent.|
|2.16 drill|drill_run|Requires an explicit actuator contract: existing one BOOL is not separate feed/retract. Define what its mechanical cycle represents before implementing bottom/top travel. Keep two-hand qualification, start latch and restart policy in LAD.|
|2.19 door|motor_open/motor_close|Integrate bounded door position; derive NC limit polarity from actual endpoints. Both directions active must hold/report.|
|2.23 sorter|conveyors_run/route_position|Move each parcel according to actual belt command and route setting; derive size sensors from object geometry/location; count only completed physical transfers. LAD chooses route.|
|2.24 robot-CNC|infeed_run/outfeed_run/robot_run/cnc_run|Separate bounded actuator mechanisms, pickup sensor and CNC completion. Explicitly define robot command semantics and machining physical dwell. LAD owns ordering/qualification, not plant implicit step scheduling.|

Minimal shared ownership model if live evidence confirms the gap: one accepted controller tick advances mechanism pose from current actuator outputs; mechanical state produces PC sensor feedback and SIM readouts; the next ladder scan consumes those sensors. Stop freezes mechanics and command clearing changes displayed eligibility; Reset restores physical initial state. No controller-owned output may be written by plant code. Preserve standalone legacy reference playback separately. Prefer small scene-specific plant adapters with explicit configuration over a generic hidden sequence runner.

Before each adapter change, obtain native input→accepted command→no/proper physical movement and sensor evidence; then verify normal endpoints, command removal, conflict where applicable, Stop/Run and Reset with learner LAD. Unknown equipment dwell/return behavior must be documented rather than guessed.
