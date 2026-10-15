# ADR 0002: Native bounded virtual controller for Phase 1

- Status: Accepted
- Date: 2026-09-30

## Context

RungProof Next needs an offline learning loop in which a user can load a small
IEC 61131-3-aligned Ladder Diagram program, run it against the existing plant
simulation, and inspect deterministic results. This is not a PLC emulator,
vendor compatibility claim, safety verifier, or route to physical I/O.

The authoritative product target is `rungproof-next`, the active Godot/C# plant
runtime and operator UI. The root Python/Qt application remains packaged pilot
and interface-reference material. Building the feature there would duplicate
the current plant runtime and leave the active product unchanged.

## Decision

Phase 1 uses a small native C# execution core behind a renderer-neutral program
model. The supported subset is BOOL LD contacts, coils, series, and parallel
branches. RungProof owns the deterministic scan coordinator and keeps plant
physics separate from program memory.

The fixed scan sequence is:

1. sample declared PC-owned simulation inputs;
2. execute validated LD networks in document order;
3. commit only declared PLC-owned BOOL outputs;
4. advance the plant by exactly one scan period;
5. publish an immutable snapshot for the UI.

Run, Stop, and Reset are the only controller lifecycle commands. Stop forces
declared outputs false. Reset clears program memory, scan time, pending operator
inputs, and plant state. No `stepPassed` command exists.

The virtual-controller module has no PLC transport dependency. The Godot host
continues to construct a disconnected runtime client. Plant bindings reject
unknown or non-PLC-owned output points instead of silently writing them.

## Candidate evaluation

| Candidate | Useful capability | Phase 1 decision |
|---|---|---|
| Native C# core | Small, testable BOOL LD subset; direct fit with current app and plant clock | Selected |
| PLCopen XML | Standardized exchange representation; IEC 61131-10 successor to the PLCopen XML specification | Future import/export boundary, not an execution engine |
| Beremiz + MatIEC | Mature IEC toolchain; MatIEC compiles IEC languages to C/C++ | Defer adapter spike: broad toolchain footprint and GPL boundaries require process isolation and review |
| OpenPLC Runtime v4 | Active C/C++ runtime, compile/load/debug services, deterministic scheduling concepts | Defer external-engine adapter spike; current host/deployment flow is substantially larger than Phase 1 |
| IronPLC | MIT Rust compiler/runtime/VM with PLCopen input direction | Watch/spike candidate; upstream describes the project as a prototype and current language coverage is ST-centered |
| RuSTy | LLVM-based ST compiler | ST-oriented future candidate; license and native integration need review |
| Eclipse 4diac FORTE | Credible IEC 61499 runtime | Rejected as primary because IEC 61499 event/function-block execution is not the requested IEC 61131-3 LD model |

The complete language, execution path, scan, inspection, Windows packaging,
integration, maintenance, licensing, offline-test, and risk review is recorded
in `VIRTUAL_CONTROLLER_ENGINE_EVALUATION.md`. License/source provenance is
summarized separately in `VIRTUAL_CONTROLLER_PROVENANCE.md`.

## Consequences

- Phase 1 behavior is narrow enough to prove exhaustively and does not claim
  compatibility with Siemens, Rockwell, CODESYS, OpenPLC, or any other vendor.
- The JSON model and compiler boundary are intentionally separate. Future FBD,
  ST, or PLCopen XML front ends can lower into a versioned execution IR without
  coupling the UI to an engine.
- A future external engine must run behind a typed adapter boundary. It may not
  gain direct access to physical PLC transport, scene internals, or arbitrary
  host code execution.
- Timers, counters, arithmetic, retentive memory, tasks, online change, forcing,
  fieldbus, and real PLC I/O remain out of scope.
