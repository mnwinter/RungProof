# Full instruction monitoring audit

Date: 2026-10-11. Read-only source audit of the active Studio checkout after V25 counter readouts. No product code, build, runtime test, or native UI interaction performed. This report is design evidence, not acceptance evidence.

## Finding

The existing snapshot supports useful current-value readouts for every implemented operand family, but it does not capture instruction-time operands or results. A correct first release can show **values at snapshot**, paired with independently recorded **last evaluation this scan**. It must not imply that final values were the values consumed or written by a particular instruction. Real call-instance interfaces need execution semantics, not a UI lookup.

## Snapshot contract and limitations

- `VirtualControllerRuntime.cs:15-56`: element state contains only ID and energized Boolean. Snapshot includes BOOL, numeric, text, timer, counter, aggregate, task, force, diagnostics, and output maps. There is no per-instruction typed operand record, write success, call path, execution ordinal, or invocation count.
- Running scans clear elements and diagnostics (`:303`); visited instructions repopulate elements. Missing element means not evaluated in that running scan, not FALSE. A present false math/CALL/RES element means its rung was evaluated false and its action was skipped. False timers and assignment coils still execute their defined state updates.
- Elements are keyed by stable instruction ID. Repeated calls, multiple tasks using the same block, and backward jumps overwrite the prior element evaluation. The value is the last evaluation in the scan, not a complete trace.
- Contacts and comparisons evaluate all children without short-circuiting (`:665-739`). Their element result is the local condition, not upstream rung continuity. Canvas combines continuity separately. A comparison can evaluate true on a false rung.
- Timer/counter states are global symbolic instances keyed by tag. Timers have one owning timer instruction enforced by the compiler (`LadderCompiler.cs:179`), but reset instructions can alter the instance later. Counters can share an instance; later CTU/CTD/LOAD/RES can change its preset, count, input, and done state. V25's Count input is instance snapshot state, not necessarily this box's input. Show configured preset separately when it differs from the current instance preset.
- Values are after program execution and output force application; accepted input images were sampled before plant advancement. `VirtualControllerAcceptedScan` separately holds post-plant inputs. Do not mix those later geometry signals into instruction values.
- `Stop()` changes outputs and timers but does not clear elements (`:175-199`); force operations also publish snapshots without advancing the scan. Thus a snapshot with scan number N is not automatically the immutable accepted result of scan N. Stopped/forced state can coexist with prior evaluation highlights. Label lifecycle state and prior evaluation, or carry explicit provenance before saying accepted scan throughout.
- Shell compares serialized loaded program with the current draft, excluding watch variables (`SimulatorShell.cs:9597-9617`). Invalid or changed drafts receive null monitoring. Preserve this gate in all readouts; never silently fall back to the old program.
- Use TryGet-based resolution with an explicit unavailable result. Runtime resolver defaults for unknown values (false/zero) are not appropriate UI behavior. Preserve zero, FALSE, empty string, missing, not executed, and not monitored as different states.

## Complete supported-family matrix

| Family / instructions | Current canvas | Correct additions using existing snapshot | Requires instrumentation or new semantics |
|---|---|---|---|
| NO/NC (XIC/XIO) | Symbol/tag and continuity color | BOOL/member value at snapshot; local last evaluation; upstream continuity separately; force indicator | BOOL read at evaluation, if later writes change it |
| Rising/falling edge contacts | Edge symbol and continuity | Current BOOL value; recorded pulse evaluation; not evaluated state | Previous sampled BOOL and edge-memory baseline; pulse cause and invocation |
| Series/parallel branches and rung | Conductors and color | Evaluated/not evaluated and local result, with monitor lifecycle | Full execution sequence and reason a rung was skipped |
| Assignment, Set, Reset coils (OTE/OTL/OTU) | Tag/symbol and rung color | Destination BOOL at snapshot; last rung condition; assignment versus conditional-write wording; forced output badge | Value before/after this write, actual write event, distinction from later overwrites |
| TON/TOF/TP/TONR-RTO | Configured PT; monitored ET and condensed Q/DN/TIMING/IDLE | Separate preset, elapsed, input, timing, done; units; instance snapshot label | Exact instance before/after this execution; reset/later-call effects |
| Timer reset RT/RES | Mnemonic and instance only | Same timer instance readout plus reset rung condition and action enabled/skipped | Proof this reset changed state and what it cleared |
| CTU/CTD | V25 preset/count/input/done | Clarify global instance state; configured versus instance preset; separate box evaluation; missing/not executed states | Per-instruction count edge and before/after count |
| Counter LOAD/RES | V25 common counter readout | Clarify LOAD/RESET enable versus instance CountInput; same preset distinction | Exact load/reset effect before later writes |
| EQ/NE/GT/GE/LT/LE | Operand expressions and color | A/B current numeric or text values; units/types where known; recorded condition result | A/B as consumed, especially self-modifying rungs and shared tags |
| CODE MATCH | Expression and color | Current text/code and recorded result; full text available on selection | Evaluated text/code; structured invalid-code diagnostic |
| MOV/MOVE numeric | Source expression/destination only | Current source and destination; destination type; evaluated/disabled status | Captured source and value actually written |
| MOV text/ENUM | Same generic box | Quoted source/destination strings, explicit empty string, enum type; truncation with full-value detail | Captured transfer and typed write outcome |
| ADD/SUB/MUL/DIV/MOD/EXPT | Source expressions/destination only | A/B and destination current values; recorded enable; relevant runtime diagnostics | Raw calculated result, operands consumed, write success, later overwrite |
| ABS/NEG/SQRT/LN/SIN/COS/TAN/ASIN/ACOS/ATAN | Same generic box | A and destination current values; type/units from supported metadata only | Captured result and structured domain/nonfinite status |
| TRUNC/CONVERT/ROUND/CEIL/FLOOR | Same generic box | Current source/destination and declared destination type; source result versus rounded/clamped stored value explained in detail | Exact pre-normalization result and whether conversion clamped; never infer from final destination |
| NORM_X/SCALE_X (CPT aliases) | Three expressions/destination only | MIN/VALUE/MAX current values and destination | Consumed operands, computed result, invalid range outcome |
| FORMAT_TEXT | Generic source/destination | Current numeric A/template B/destination string; explicit empty result and diagnostic | Captured format input/result; typed failure (runtime clears destination on invalid format) |
| CALL/JSR | Target display name and rung color | Target and called/skipped/not evaluated using element presence/state; globals can be viewed as globals | Parameter passing, local/temp/static execution storage, instance identity, binding/result values, call-specific trace |
| RETURN/RET | Mnemonic and rung color | Taken/not taken/not evaluated; textual current-frame meaning | Actual caller/frame/task and exact skipped range |
| JMP | Label and rung color | Taken/not taken/not evaluated; target name | Invocation sequence/count and executed path through loops |
| LABEL/LBL | Destination/name | Visited/not visited and local rung result (not a branch decision) | Visit count and incoming jump path |
| Continuous/periodic tasks | Existing controller task status | Due/period/priority/execution count from Tasks | Per-task ownership of an overwritten element result |
| Aggregate fields/array leaves | Operands usable through flattened variables; aggregate watch exists | Resolve exact flattened leaf first; display type and value; use aggregate watch for whole structure | Arbitrary dynamic indexing or block-local aggregate semantics not implemented |

No supported bitwise word AND/OR/XOR/shift/rotate family was found in the instruction enum/catalog. Do not manufacture such boxes or vendor status bits. Branch AND/OR are Boolean control flow, not word operations.

## CALL interfaces: important boundary

`LadderProgram.cs:194-232` stores interface declarations (Input/Output/InOut/Static/Temp). `LadderEditorDocument` preserves these declarations. Runtime CALL simply schedules the target block networks (`VirtualControllerRuntime.cs:526-536`); it does not instantiate, bind, copy inputs/outputs, or resolve local storage. Compiler/runtime have no interface execution mapping. Do not display declaration InitialValue as a live parameter, or resolve an interface name against an unrelated global of the same name. Display interface declarations as **declaration only / no runtime binding** until an explicit, independently designed executable interface model exists.

## Prioritized design

1. Introduce one pure monitor-value presenter with explicit states: not monitored, unavailable, not evaluated this scan, evaluated false/true, and lifecycle context (running/paused/stopped/reset). Resolve literals, exact BOOL/numeric/text leaves, supported timer/counter members, and aggregate leaves without runtime evaluation or mutation. Preserve invariant number formatting and units; quote text and visibly represent empty strings.
2. Use it across all boxes and contacts. Label numeric operands/destinations **snapshot values**, and condition/enable **last evaluation**. Give timers the same readable instance fields as counters; add timer reset state. Correct counter CountInput semantics and configured/instance preset distinction. Do not recompute comparison truth from final values.
3. Make layout derive from the same readout model used to draw/hit-test: source count, rows, measured string width, horizontal clearance, row/branch height, and selected-detail expansion. Both vendor styles must remain readable at supported zoom and DPI. Long text belongs in expandable detail; ellipsis must remain obvious and full operand identity accessible.
4. If the user expects values *at instruction execution*, add immutable structured instruction observations in runtime. Capture typed consumed operands, enable/result, pre/post destination, write status, structured diagnostics, execution count, and last invocation/task/call path. Capture inline during real evaluation; never rerun instructions to populate UI. Keep allocation bounded and explicitly choose last invocation or a capped trace. Feed observations through accepted history snapshots.
5. Treat executable CALL interfaces as a separate feature: scope, binding, aliasing, storage lifetime, reentrancy, initialization, and invocation identity must be specified and compiled before exposing live parameter values.

## Tests required before implementation acceptance

- Presenter: FALSE/0/empty string versus missing; numeric/text literals; enum; aggregate leaf; counter ACC/CV/PRE/PV/Q/DN; timer ET/PT/Q/DN/TT; unknown member unavailable; invariant decimal formatting; very long operands/text.
- Scan-order honesty: comparison true before a later MOV changes its operand; MOV source/destination same tag; multiple writes to one destination; output forcing differs from coil rung; two CTU/CTD boxes and LOAD/RES sharing a counter; timer followed by RT. Assert labels cannot imply final values caused earlier results.
- Execution states: false math and false CALL skip; false TON/TOF and OTE execute state changes; skipped CALL target, RETURN tail, JMP range, non-due periodic task; repeated CALL and backward jump overwrite explicitly labeled last evaluation.
- Lifecycle: reset scan zero, run before first scan, pause/step, stop after energized scan, force change without scan, stale/invalid draft, undo restoring match, watch-only edit retaining match, history selection. Old element data must not become a fresh execution claim.
- Faults: DIV/MOD zero, SQRT/LN/ASIN/ACOS domain, exponent overflow, invalid MIN/MAX, format failure, invalid code token, watchdog abort. Green enable is not successful write. Existing diagnostic strings can be shown as diagnostics; reliable structured per-box success needs observation data.
- Conversion: half-to-even rounding, integer saturation, negative truncation, nonfinite results, later overwrite; assert final destination is never labeled calculated result without a recorded result.
- Native render: both styles; all families; dense parallel branches; long symbols; text/enum content; selected and unselected; zoom/scroll; status/readouts/conductors/hit targets do not overlap. Capture actual page screenshots after changes.

## Evidence and limits

Inspected `LadderInstructionCatalog.cs`, `LadderProgram.cs`, `LadderCompiler.cs`, `LadderEditorDocument.cs`, `VirtualControllerRuntime.cs`, `VirtualControllerSession.cs`, `VirtualControllerAcceptedScan.cs`, `LadderEditorCanvas.cs`, and shell monitor/watch code. No feature edits made. No build or UI acceptance claimed.
