# Known limitations

## Current Godot application

The current application is `rungproof-next/`: an offline ladder editor/runtime
with five authored demos, 77 scenes, and a separate guarded external-PLC bridge.
No standalone Next release artifact has been qualified. Current release gaps
and verification limits are recorded in [demo release review](DEMO_RELEASE_REVIEW_2026-10-10.md).
TIA/Logix-style editing does not establish vendor project compatibility or CPU
equivalence. Local simulator tests do not establish live PLC commissioning.

## Historical Qt pilot limitations

These limits must remain visible in pilot documentation and support responses.

- The native application supports one production scene: Scene 2 - Conveyor
  Pusher. Other listed scenes are review/prototype material and fail closed.
- RungProof does not execute ladder logic or emulate a Siemens CPU/TIA Portal.
  The native application currently has no offline controller that can drive the
  Run workflow. This is intentional for the present PLC-led engineering
  pilot: the connected PLC executes the ladder logic.
- Live PLC compatibility is limited to the exact approved TIA V17/S7-1500/
  standard-DB14 bench configuration. That acceptance is still outstanding.
- A normal build hides and command-guards **Connect Real PLC**. An internal
  test build created with the explicit `-EnableRealPlc` switch can write the
  four declared PC-owned DB14 feedback/heartbeat points only after the operator
  reviews and authorizes the exact scope in the application.
- The read-only PLC check verifies configured addresses and observed values; it
  does not prove symbolic names, ladder semantics, watchdog correctness, or
  machine safety.
- The PLC watchdog is the final command-safe authority. RungProof cannot prove
  the state of unobserved physical outputs.
- The default renderer prioritizes VM/RDP reliability over CAD/physics fidelity.
  Models are deterministic training representations, not digital twins.
- Scene assets are still awaiting explicit product-owner visual approval.
- The Scene Editor and browser player are not packaged or supported.
- The current release candidate has no automatic updater or telemetry.
- The external pilot artifact must not be delivered until code signing,
  installer/rollback, Qt distribution, legal, and support approvals are closed.
