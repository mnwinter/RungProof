# Typed lessons implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Run drive alarm matching, chicken label processing, and a motor state machine through edited, verified, loaded ladder projects and scene I/O.

**Architecture:** Extend the existing scalar controller with bounded STRING and declared ENUM domains. Matching, formatting and state changes execute in the loaded ladder program. Scene adapters supply received messages, measured product weight and physical process feedback; they animate committed commands.

**Tech Stack:** C#/.NET 10, Godot 4.7.2, existing deterministic controller test runner.

**Spec:** Approved scope recorded in this plan: implement the three lessons in the order below, verify an authored edit changes each outcome, restore it, and exercise Stop/Reset. This is an offline simulator without vendor runtime or physical commissioning claims.

## Global constraints

- Preserve unrelated working files and the existing 107 untracked-file hashes.
- No live PLC connection, push, merge, publication, deployment or monitoring.
- STRING values are at most 255 UTF-16 code units, without silent truncation.
- ENUM declarations list 1–32 distinct identifier members; values must belong to the declared domain.
- Stop clears outputs, freezes scene time, and suppresses held startup edges; Reset restores declared values and scene process state.

## Review focus

- F0030 and prefix/suffix lookalikes must not match selected code F003.
- Invalid/oversized sampled text must fail before changing the text input image.
- Printing must capture the PLC label text and require measured stable weight and printer readiness.
- Applying must require a completed printed job, with no duplicate jobs from held requests.
- Fault and Stop dominate Start; clearing/resetting a fault must require a fresh Start edge.

### Task 1: STRING path and drive matching

Files: existing VirtualController scalar/compiler/runtime/JSON/editor/session/I/O files; new `PlcTextValues.cs`, `TypedDataTests.cs`, `AuthoredLessonLadderPrograms.cs`; drive contract and scene adapter; typed editor UI.

Interfaces: `VirtualControllerRuntime.Scan(boolInputs, numericInputs = null, textInputs = null)`; immutable snapshot `TextVariables`/`TextOutputs`; text sampling/commit callbacks on `VirtualControllerSession.Advance`; `LadderCompareOperator.ContainsCode` uses ordinal complete alphanumeric tokens.

- [x] Add a failing type-support test, run it and retain the red result.
- [x] Implement STRING declarations, literals, comparison, MOVE, persistence and I/O mapping; validate boundaries and direction.
- [x] Add editable authored drive project with `selected_alarm_code = F003`, real received text and valid/reset gates.
- [x] Run controller tests/build and actual scene verifier; inspect native UI, edit F003 to F030, Verify/Load, observe changed result, restore, Stop/Reset.
- [x] Include the drive deliverable in the focused shared typed-lessons checkpoint.

### Task 2: Chicken weighing, formatting, printing and application

Files: new `ChickenLabelPlantModel.cs`, portable process tests and scene adapter; chicken contract/help; authored project and FORMAT_TEXT editor support.

Interfaces: `FormatText` consumes numeric SourceA, STRING template SourceB and writable STRING destination. Invariant formatting accepts one `{0:F0}` through `{0:F6}` placeholder; malformed templates and oversized results diagnose and clear the destination. Plant progression is deterministic, gated by loaded PLC commands, with 0.5 s weighing, 0.6 s printing and 0.5 s applying.

- [x] Prove process timing, missing interlocks, immutable printed text and held requests in deterministic tests (26 passing checks; separate retained red result not recorded).
- [x] Implement the process and actual paper/application transforms; publish weight and completion feedback separately from PLC requests.
- [x] Load editable ladder formatting the measured 1.237 kg product as `CHICKEN 1.237 kg`; change F3 to F2, Verify/Load and prove changed label text, restore, Stop/Reset.
- [x] Run relevant regressions and native rendered workflow; include in the shared typed-lessons checkpoint.

### Task 3: Motor ENUM state machine

Files: declared ENUM validation/persistence/editor and motor contract/adapter; authored motor state ladder and tests.

Interfaces: ENUM values are named strings checked against each tag's domain. The motor domain is `Stopped, Running, Fault`. MOV and equality use valid members/same domains. The scene reads committed `motor_running` and displays committed `motor_state`; it contains no hidden controller state chart.

- [x] Add domain, priority, restart and lifecycle tests (passing controller tests; separate retained red result not recorded).
- [x] Implement declaration/domain support and editable ladder with fault latch, reset and fresh Start edge.
- [x] Observe shaft rotation and named state from the loaded program; edit a transition member, Verify/Load and prove changed behavior, restore, Stop/Reset.
- [x] Run controller/app regressions, verify preserved files and refresh coverage; include in the focused shared checkpoint.

Independent preparation is delegated to narrowly owned UI, plant model and authored-project files. Shared core and scene integration remain with the primary agent, which reviews and verifies all results.

## Evidence checkpoint

See `../../TYPED_LESSON_IMPLEMENTATION.md` for final build hash, 159 controller/26 plant/33 engine checks, native semantic edits/restoration/Stop/Reset and final ink inspection. Relevant regressions pass. Original 107 unrelated hashes remain unchanged; coverage preserves mixed historical provenance. The coupled type, UI and scene integration is saved as one focused local checkpoint; its exact Git hash is reported after the commit succeeds.
