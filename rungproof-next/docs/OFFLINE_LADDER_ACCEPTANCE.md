# Offline Ladder engineering acceptance

This document is the completion gate for the simulator-native Ladder
engineering environment. It records evidence for the requested product scope;
it is not a claim of Siemens TIA Portal, Rockwell Studio 5000, IEC
certification, vendor-project compatibility, or physical PLC commissioning.

## Product boundary

- Execution is local, deterministic, and simulation-only.
- Scene I/O uses declared symbolic bindings and never resolves `%I`, `%Q`, DB,
  controller tags, slots, paths, or network addresses.
- `SceneIoImageMapper` is a pure translation boundary between authored tag
  names and simulator point names. `SceneIoBindingValidator` rejects invalid
  type, ownership, direction, and duplicate-output mappings before load.
- Opening an editable `.rpproj.json` project does not compile, load, run, or
  replace the current runtime. **Verify + Load** is the separate fail-closed
  transition to a fresh immutable executable program.
- Forces apply only to local BOOL input/output images. Stop drives outputs safe;
  Reset removes all forces. No physical PLC transport is constructed.

## Requirement evidence

| Requirement | Authoritative implementation/evidence | Status |
|---|---|---:|
| TIA-style and Studio 5000-style workflows | Separate workbench trees, terminology, palettes, active editor identities, contextual property dialogs, resizable/collapsible docks, and native interaction verifier in `SimulatorShell` | Proven |
| Interactive project navigation | Selectable controller/project tree; block/routine, task/OB, tag, watch, search, and result navigation exercised by `--verify-ladder-editor` | Proven |
| Tags and data types | BOOL, INT, DINT, REAL, TIMER, COUNTER declarations; typed startup values; reference-safe rename/delete; persistence and runtime initialization tests | Proven |
| Arbitrary graphical rungs/networks | Exact insertion slots, selection, move, deletion, copy/paste, multiple rungs, parallel branch add/remove, segmented conductor rendering, and layout verifier | Proven |
| Coils and latches | Assign/OTE, Set/OTL, Reset/OTU runtime, scan-order, JSON, editor, and native palette tests | Proven |
| Timers and counters | TON, TOF, TP, TONR/RTO, reset; CTU, CTD, load, reset; members, presets, lifecycle, persistence, and editor tests | Proven |
| Comparisons, math, moves | EQ/NE/GT/GE/LT/LE; MOV; arithmetic, scientific, scaling, and conversion operations with compile/runtime domain diagnostics | Proven |
| Calls and program control | CALL/JSR, RETURN/RET, JMP/LABEL/LBL; missing/recursive/duplicate-target validation and infinite-jump watchdog tests | Proven |
| Blocks/routines and tasks/OBs | Stable-ID lifecycle, protected deletion, continuous/periodic scheduling, priority order, persistence, Undo/Redo, and native UI tests | Proven |
| Deterministic scan | Fixed sample-execute-commit-plant-publish order, repeatability, task ordering, scan-quantized timers, and complete runtime unit suite | Proven |
| 3D scene I/O | BOOL and numeric binding validation; shared symbolic image mapper; native conveyor/photoeye and DINT fan-selector end-to-end verifiers | Proven |
| Monitoring | Immutable snapshots, element energization, rails/branches/instructions, scan/lifecycle state, timers/counters, watch rows, stale-program suppression | Proven |
| Simulator-only forcing | Input/output force tests, visible force state, invalid-target rejection, Stop-safe and Reset-clear lifecycle, native runtime verifier | Proven |
| Validation and diagnostics | Structured compiler/binding issues with codes and paths, navigable Error List, runtime diagnostics, stale-state handling | Proven |
| Search and cross-reference | Semantic project search; declaration/read/write/member/call/task/JMP/LBL indexing and result navigation tests | Proven |
| Undo/Redo and clipboard | Exact snapshots, stable identities, redo invalidation, tag/block/task/watch edits, instruction and rung clipboard tests | Proven |
| Instruction help | Complete catalog coverage, vendor-specific presentation terms, operands, restrictions, examples, and F1/native UI tests | Proven |
| Project Save/Open/New | Editable invalid-WIP persistence, malformed-input rejection, stable-ID repair, atomic New Project, exact dirty baseline, and native UI tests | Proven |
| End-to-end engineering path | One 134th focused test authors, saves, opens, validates bindings, compiles, loads, scans, maps scene I/O, publishes monitoring, forces, Stops safely, and Resets | Proven |
| No real PLC access | Test output explicitly reports transport not constructed and connection not attempted; virtual-controller core has no PLC transport dependency | Proven |

The detailed instruction-by-instruction matrix and diagnostic codes remain in
`VIRTUAL_CONTROLLER_CONFORMANCE.md`. Program and scan semantics remain in
`VIRTUAL_CONTROLLER_PROGRAM_SCHEMA.md`.

## Reproducible commands

Run from `rungproof-next` in PowerShell:

```powershell
& '.\.tools\dotnet\dotnet.exe' build .\RungProof.Next.csproj --no-restore
& '.\.tools\dotnet\dotnet.exe' run --project .\tests\RungProof.Next.VirtualController.Tests.csproj

$godot = '.\.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --path . -- --verify-app-shell
& $godot --headless --path . -- --verify-virtual-controller
& $godot --headless --path . -- --verify-numeric-scene-io
& $godot --headless --path . -- --verify-ladder-editor

# UI geometry and pointer/gizmo checks require a real window. A headless Godot
# viewport is 64x64 here and is intentionally not valid acceptance evidence.
& $godot --path . --resolution 2048x1152 --position 0,0 -- --verify-ui-density
& $godot --path . --resolution 2048x1152 --position 0,0 -- --verify-hud
& $godot --path . --resolution 2048x1152 --position 0,0 -- --verify-workspace
& $godot --path . --resolution 2048x1152 --position 0,0 -- --verify-scene-controls
& $godot --path . --resolution 2048x1152 --position 0,0 -- --verify-camera-input

py -3 tools\verify_scene_contracts.py --godot $godot
```

## Current acceptance record

Verified on 2026-10-02:

- build: 0 warnings, 0 errors;
- focused virtual-controller suite: 134 passed, 0 failed;
- real PLC transport constructed: false;
- real PLC connection attempted: false;
- app shell, virtual BOOL scene I/O, numeric DINT scene I/O, Ladder editor,
  one-to-one DPI, HUD, workspace/gizmos, scene controls, and camera input: pass;
- native scene contracts: 26 passed, 0 failed;
- independent visual audit of current TIA and Studio captures: no remaining
  P1/P2/P3 findings.

This acceptance record proves the local build and tested simulator behavior.
It does not prove vendor project import/export, online edits, safety integrity,
or commissioning against physical equipment.
