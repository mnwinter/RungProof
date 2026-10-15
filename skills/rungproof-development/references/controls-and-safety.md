# Controls architecture and safety boundary

## Scan and plant ownership

Maintain this causal chain:

```text
operator/scene input
  -> symbolic PLC input image
  -> accepted ladder scan
  -> PLC-owned output command
  -> plant integration/physics
  -> PC-owned measured feedback
  -> next accepted scan
  -> renderer and observations
```

The plant may model travel, inertia, collision, sensor windows, and measured
position. It may not decide the next process phase, fabricate later actuator
commands, count a PLC sequence on the PLC's behalf, or bypass the displayed
program. High-level commands such as `gantry_cycle` are architectural warning
signs when the lesson claims to teach individual axes, interlocks, or sequence
logic.

Each moving demo should expose meaningful machine controls and visible logic:

- momentary Start/Run and Stop behavior;
- Reset/recovery conditions;
- System Enable or equivalent permissions when appropriate;
- permissives and interlocks;
- simulated trip/E-stop response and restart inhibition for hazardous motion;
- output commands and measured feedback used by later transitions.

App playback controls do not substitute for scene machine controls. A control
that has no scan-visible effect is decorative and fails acceptance.

## External PLC boundary

External PLC operation is exceptional and requires explicit task scope and
operator authorization. Before any connection:

- identify the exact CPU, profile, IP, rack/slot, DB, and configured point set;
- confirm PC-to-PLC writes and PLC-to-PC reads separately;
- confirm no physical `%I` or `%Q` write and no CPU-mode control;
- confirm PLC watchdog/safe-value behavior and normal machine safety remain in
  force;
- distinguish read-only diagnostics from a write-enabled live session;
- keep the default disconnected and do not auto-resume after readiness loss.

Offline tests use an in-memory adapter and must prove that no real transport was
constructed or contacted where that invariant is relevant. A successful build,
package, simulated scan, or read-only diagnostic does not authorize or prove a
live write.

## Claims

RungProof is an IEC 61131-3-aligned learning/simulation subset. It is not a
certified PLC runtime, safety controller, Siemens implementation, or
commissioned machine control. PLCopen XML, if considered, is interchange—not
proof of execution equivalence.

State the evidence level explicitly:

1. source/configuration inspection;
2. deterministic offline test;
3. headless application route;
4. normal-renderer visual inspection;
5. installed-TIA matched comparison;
6. isolated authorized bench observation;
7. commissioned equipment result.

Never collapse one level into a higher one.
