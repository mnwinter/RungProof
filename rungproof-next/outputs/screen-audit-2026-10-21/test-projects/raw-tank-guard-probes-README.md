# Raw tank manual-output guard probes

Read nonlab-ui-contracts.md first. These two diagnostics isolate normal physical manual-action routing after a valid controller is loaded and RUNNING. They are NOT normal tank solutions and earn NO normal-flow/fill/drain coverage.

| Probe filename | Exact target scene | Guard controls to exercise |
|---|---|---|
|probe-tank-high-low-manual-output-guard.rpproj.json|tank-high-low|Physical pump toggle-pump→inlet_pump_run; drain toggle-drain→drain_valve_open|
|probe-tank-level-manual-output-guard.rpproj.json|tank-level|Physical pump toggle-pump→inlet_pump_run; drain toggle-drain→drain_valve_open|

Each schema9 project includes exact PC/PLC declarations/types/initials, excluding SIM tank_level. An unbound qa_probe_false BOOL Memory initialFALSE with no writer drives a NO contact→Assign inlet_pump_run FALSE. drain_valve_open retains its declared FALSE initial value. No invented PC command aliases, simulator feedback, plant rules or process sequence. No DBs/instances.

Through the normal UI select matching scene, Open probe, Verify+Load, Run, verify scan progression, then physically click pump and drain controls. Both advertised actions target PLC outputs but declare no controllerStartBinding/controllerStopBinding. Expected guard candidate is `Action blocked · this scene action has no command binding in the loaded ladder program`; only actual Health event history proves what happened. A stationary tank is expected with false command and is NOT evidence of this guard or an app defect. Correct mapped physical clicks must be grounded from screen, not invoked through backend or hidden buttons.

Health history retains feedback while bottom workspace status can be overwritten by view changes. App Run/Stop/Reset remain playback operations, separate from missing physical command bindings. Static JSON/shape checks only; no compiler/runtime/UI execution. Probe-only hashes/statuses are in raw-tank-guard-probe-manifest.json, separate from supplemental normal fixtures.
