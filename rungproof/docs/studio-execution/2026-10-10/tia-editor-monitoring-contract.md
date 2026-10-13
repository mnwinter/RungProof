# TIA editor instruction monitoring contract

Status: proposed detailed design, pending review and implementation authorization. 2026-10-11. Resolves monitoring corrections 4 and 7 in `tia-editor-dot-design-review.md`. No product changes, build, benchmark, or native input test performed. Budgets below are proposed pass criteria, not measured capabilities. All source references are relative to `rungproof-next/` at the audited working-tree revision; freeze exact commit/file hashes with implementation evidence.

## 1. Required result

Every supported instruction exposes its last actual evaluation, typed consumed operands, and effect in the selected execution context. Snapshot values remain a separate view. A disabled/unimplemented required function is not completion. Parameterized CALL monitoring depends on the separate executable CALL contract; legacy target-only CALL remains shared-global execution. Declaration InitialValue never qualifies as a live parameter.

Instrumentation observes the existing executor in place. It must not reevaluate an instruction, resample inputs, modify scan order, change force precedence, or affect output/state values. No vendor parity claim is created.

## 2. Current evidence and gaps

| Source | Confirmed implementation / consequence |
|---|---|
| `src/VirtualController/LadderProgram.cs:39-63` | Four node kinds, seven comparisons, three edge modes. |
| `src/VirtualController/LadderProgram.cs:80-182` | Three coil modes, four timers, two counters, 25 numeric/text operations, target-only CALL, RETURN/JMP/LABEL. |
| `src/VirtualController/LadderInstructionCatalog.cs:42-235` | 54 catalog entries: 22 explicit initial entries, seven comparisons, numeric-0 through numeric-24. Includes editor commands `branch`/`rung`; they are not standalone data writes. |
| `src/VirtualController/VirtualControllerRuntime.cs:15-56` | Element state is only ID/energized; snapshot has final global values, instance maps, tasks, forces and string diagnostics. |
| `src/VirtualController/VirtualControllerRuntime.cs:303-320` | Running scan clears element map; due tasks are priority ordered. |
| `src/VirtualController/VirtualControllerRuntime.cs:328-587` | Network execution, watchdog, operations, calls and final forces. Watchdog partial scan still increments scan/time and returns a stopped snapshot. |
| `src/VirtualController/VirtualControllerRuntime.cs:665-739` | All children evaluated; local contact/compare result is distinct from upstream continuity. |
| `src/VirtualController/VirtualControllerRuntime.cs:792-797` | INT/DINT normalization rounds half to even, then clamps. |
| `src/VirtualController/VirtualControllerSession.cs:159-192` | Session accepts scan-number increment, commits outputs, advances plant and publishes events/history. It currently does not distinguish watchdog partial scan from successful completion. |
| `src/VirtualController/VirtualControllerScanCompleted.cs:19-25` | Completed record stores images/diagnostics, not instruction observations. Existing history cannot reconstruct instruction-time values. |
| `src/App/SimulatorShell.cs:9597-9617` | Draft/load match uses serialized program excluding watches; mismatch removes canvas snapshot. |
| `src/App/LadderEditorCanvas.cs:663-769` | Timer condensed state, V25 counter rows, math expressions without consumed values. |

## 3. Identity and publication envelope

Proposed field names below describe a contract, not existing APIs.

| Field | Definition / invariant |
|---|---|
| SessionId | New GUID for each controller session, prevents cross-session collisions. |
| ProgramContentId | SHA-256 of versioned canonical executable program representation including IDs, tags/types/initial values, tasks, bindings and execution settings. Excludes watches, selection and presentation-only state. Until canonicalization is implemented, retain conservative existing serialized match in addition to identity. |
| LoadId | New GUID for every successful load, including reload of identical content. Never reused. Failed loads do not replace it. |
| ExecutionGeneration | Monotonic within session; increments on successful load and Reset. Stop/Run/Pause/force changes do not reset scan identity; they are separate publications. |
| ScanAttemptNumber | Monotonic within generation, starts at 1 when execution begins; includes aborted attempts. Reset produces no attempt. |
| RuntimeScanNumber | Existing runtime counter, retained for compatibility, explicitly not sufficient identity. |
| PublicationRevision | Strictly increasing within SessionId for every published runtime envelope, including load/reset/run/stop/pause/force. Never reset by Reset. Historical UI selection has a separate UI revision and does not increment runtime publication revision. |
| PublicationKind | Loaded, Reset, Run, Stop, Pause, ForceChanged, ScanCompleted, ScanAborted. Historical selection references a stored envelope; it does not fabricate a new execution result. UI selection revisions are separate from runtime revision. |
| ObservationOrigin | Full SessionId/LoadId/generation/attempt of the contained observations, or absent. Lifecycle publications may retain a reference to prior observations but cannot restamp them as newly evaluated. |
| Completion | NoExecution, Completed, Aborted, or PublicationFailed; separate from Running/Stopped/Paused playback state. |
| Phase completion | InputSampled, ProgramExecuted, OutputsCommitted, PlantAdvanced, PostPlantSampled flags with explicit failure code/phase. ProgramExecuted means full successful executor traversal; partial execution is separately marked. |
| Image provenance | SampledInputs (before forcing), EffectiveInputs (consumed input image), ProgramEndValues (before output force), PublishedValues (after force/lifecycle), PostPlantInputs (later geometry). Each explicitly labeled; absence has a reason. |

Identity checks precede value binding. A presenter must require matching loaded program and draft, and selected instance context where applicable. Same scan number after Reset or reload must never match an old observation. Watch edits may preserve monitoring; executable edits suppress it. A content-equivalent undo may restore it only against the current loaded identity. ProgramContentId does not replace LoadId or generation.

Lifecycle publications: render current published values with their revision/kind and show old evaluations only as **last execution: generation G, scan A**. On Reset clear observations. On Stop do not retain green flow as current execution; prior evaluation is available in detail/history. Force changes may change published values without changing consumed values. Paused accepted scan is valid historical execution, not stale draft.

## 4. Abort and failure contract

Watchdog completion is **ScanAborted / partial observations**, never a fully completed scan. Include reason, last fully observed instruction, total executed networks, and abort phase. Partial memory effects and output overrides must remain visible as partial execution; do not invent rollback. Preserve current runtime behavior during instrumentation: the existing watchdog safe BOOL/numeric outputs and current session commit/plant sequence are recorded, not silently redesigned. Text-output watchdog policy needs a separate execution decision; do not label every output safe when source only resets BOOL/numeric outputs.

If a commit/plant callback fails, record the phases actually completed and a publication failure; do not claim atomic hardware/output commit. This design concerns offline execution. A generation change during a cycle must prevent publishing observations as belonging to the newer generation. Capture immutable identity at cycle start and validate before publication; an interrupted result is marked abandoned/aborted under its original identity, never relabeled.

Instructions not reached in an aborted attempt have status **not observed; scan aborted**. Earlier observations are explicitly partial. Recoverable arithmetic or formatting diagnostics do not themselves abort the whole scan; record instruction failure disposition while allowing Completed if remaining execution completes.

## 5. Observation model and invocation selection

Each recorded observation contains:

- Full origin identity; stable instruction ID and owning block ID; TaskId; monotonically increasing ExecutionOrdinal within this attempt.
- FrameInvocationId and immutable call path of `(call-site ID, target block ID, invocation ordinal, instance ID if real instance semantics exist)`. Task root is always included. Legacy calls have no FB instance ID.
- ObservationCountInScan per instruction across all invocations; context-specific count for exact selected `(task, call-site path, instance)` where captured. Labels always say **this scan**. Counts are not lifetime totals or edge counts.
- Typed operands as consumed, including literal/tag/member identity, declared type, value, and unit only if known. Source unavailable is not a numeric zero. Unused source B/C are NotApplicable.
- Local condition result, network enable and control outcome as distinct fields; edge contacts record previous-known flag, previous/current BOOL and pulse result.
- Destination/instance before, calculated result when applicable, stored value after, write disposition, normalization flags (rounded/clamped), and structured diagnostics tied to instruction/operand.
- Final published destination may be separately resolved from envelope, labeled final snapshot. It never overwrites StoredAfter.

Default view: **last invocation this scan**, with invocation count and reachable task/block/call path. An exact selected instance/path must either match a retained observation or show **context not captured**. Never substitute another instance's last invocation. For multi-invocation programs expose a context selector; a global summary is allowed only when explicitly labeled global last invocation. A bounded trace is optional detail, not the source of the authoritative final observation.

## 6. Complete catalog coverage and dispositions

Disposition vocabulary: `NotApplicable` (read/control only), `SkippedDisabled` (action not performed), `WrittenChanged`, `WrittenSameValue`, `StateUpdatedChanged`, `StateUpdatedSameValue`, `RetainedNoWrite`, `FailedNoWrite`, `FailedClearedDestination`. Keep `Evaluated`, `NotEvaluated` and `Unavailable` orthogonal. `ForcedAfterWrite` is a final publication override, not a substitute for the instruction write disposition.

| Catalog keys / exact enum coverage | Captured operands and result | Effect / disposition rules |
|---|---|---|
| no/nc; Contact + EdgeMode.None | Consumed BOOL and local inversion result; network continuity separate | NotApplicable |
| edge-rising/edge-falling; Rising/Falling | Previous/current BOOL, baseline-known, pulse result | Internal edge-state update; no user destination write. First baseline is explicitly not a pulse. |
| branch/rung; Series/Parallel/network | Child results, local aggregate result, network enable | NotApplicable. Containers need their own IDs; editor insertion commands do not pretend to execute. |
| coil/set/reset; Assign/Set/Reset | Network enable, destination before/after | Assign writes even with false enable; Set/Reset false is SkippedDisabled. True Set/Reset and all Assign distinguish same-value writes. |
| ton/tof/tp/rto; OnDelay/OffDelay/Pulse/RetentiveOnDelay | Configured preset, instance before/after including input/elapsed/timing/done | Evaluate on both true and false enable. StateUpdatedChanged/SameValue; false is never generically disabled. |
| timer-reset | Enable and instance before/after | True reset StateUpdatedChanged/SameValue; false SkippedDisabled. |
| ctu/ctd; CountUp/CountDown | Box input and prior instruction edge input; configured preset; shared instance before/after | Both enable states update edge/instance model; distinguish edge increment/decrement from StateUpdatedSameValue; never confuse instance CountInput with box input. |
| counter-load/counter-reset | Enable, configured LOAD preset, instance before/after; edge-reset effect | True StateUpdatedChanged/SameValue; false SkippedDisabled. |
| compare-0..6; Equal/NotEqual/GreaterThan/GreaterOrEqual/LessThan/LessOrEqual/ContainsCode | Consumed A/B, numeric or text, actual local result | NotApplicable; invalid dynamic code gets structured diagnostic/result as executor supplies. Never recompute using final values. |
| numeric-0 Move | Numeric/text/enum A, destination type and before/after | True WrittenChanged/SameValue with numeric normalization recorded; false SkippedDisabled. |
| numeric-1..5 Add/Subtract/Multiply/Divide/Modulo | A/B, calculated result, destination before/after | Success WrittenChanged/SameValue; divide/mod zero or nonfinite FailedNoWrite; false SkippedDisabled. |
| numeric-6..8 Absolute/Negate/SquareRoot | A, calculated result, before/after | Domain/nonfinite FailedNoWrite; success written; false skipped. |
| numeric-9 Exponentiate | A/B, calculated result, before/after | Domain/nonfinite FailedNoWrite; success written; false skipped. |
| numeric-10..16 NaturalLog/Sine/Cosine/Tangent/ArcSine/ArcCosine/ArcTangent | A, result, before/after | Applicable domain/nonfinite FailedNoWrite; success written; false skipped. |
| numeric-17 Truncate | A, truncated result, before/after | Record subsequent destination normalization independently; success written; false skipped. |
| numeric-18/19 Normalize/Scale | MIN=A, VALUE=B, MAX=C, result, before/after | Invalid range/nonfinite FailedNoWrite; success written; false skipped. |
| numeric-20 Convert | A, source numeric value, destination type/normalization, before/after | Successful conversion written even when equal; record half-even/clamp effects; false skipped. |
| numeric-21..23 Round/Ceiling/Floor | A, calculated result, normalized stored value | Success written; nonfinite FailedNoWrite; false skipped. |
| numeric-24 FormatText | Consumed numeric A/template B, formatted text, destination before/after | Invalid template/result is FailedClearedDestination, even if already empty; success written; false skipped. |
| call; LadderCall | Enable, target, frame/path; actual parameter captures only after CALL semantics implemented | Legacy CALL true Entered, false SkippedDisabled. Control effect NotApplicable for direct write; nested writes retain their own records. Parameterized copy-in/out must become explicit call-phase observations under separate contract. |
| return; LadderReturn | Enable, frame exited and resume context | Taken/NotTaken; NotApplicable write disposition. |
| jump; LadderJump | Enable, source/destination network, frame | Taken/NotTaken; NotApplicable. |
| label; LadderLabel | Visit, local enable, frame | Visited; not a taken branch; NotApplicable. |

Compiler-expanded aggregate leaves are typed operands under these families; whole aggregate display is informational and must not invent unsupported arithmetic. Interface declarations are informational until bound by executable semantics. No word-bitwise/shift/rotate enum exists in this source. Task due/priority/execution count is task metadata, not an extra instruction catalog entry.

Coverage gate: automated test enumerates all enum members and `LadderInstructionCatalog.Entries`, requires exact classification in the presenter/observation registry, rejects unknown new members, and verifies key-to-operation arity from `LadderNumericOperationRules`. Assert 54 current keys as a review snapshot, but compare sets rather than relying only on count. Every accepted registry entry must have a value/status rendering test; every data-writing family needs disposition tests.

## 7. Unavailability and diagnostic contract

Distinct reasons: NoProgramLoaded, DraftMismatch, InvalidDraft, NoExecutionYet, NotEvaluatedThisScan, ActionDisabled, PartialScanNotReached, ObservationCapacityExceeded, ContextNotCaptured, HistoryEvicted, OperandMissing, UnsupportedRuntimeBinding, NotApplicable, CaptureFault. FALSE/zero/empty strings are valid values. Text empty string renders `""`; truncation reports omitted length and full-value availability. Runtime missing operand is an invariant diagnostic, not silently zero. UI parsing existing freeform diagnostics is transitional only; structured code/instruction/operand/phase attribution is the target.

## 8. Proposed bounded capacity and performance acceptance

These are **proposed engineering budgets**, unmeasured. Measure release build on Matt's recorded Windows CPU/RAM/GPU, Godot/.NET versions, power mode, and viewport; record baseline monitoring-off and monitoring-on with identical seed and workloads. Failure requires optimization or reviewed budget changes, not retroactive claims of passing.

| Dimension | Proposed limit / required behavior |
|---|---|
| Registered observable IDs | 20,000 per program for guaranteed full last-observation capture. Larger programs may still execute; explicitly mark monitoring capacity exceeded before Run. Do not silently omit IDs. |
| Evaluations | Up to existing 10,000-network watchdog threshold per attempt; count all occurrences for registered IDs, independent of trace capacity. Count uses 64-bit checked/saturating representation with explicit saturation flag. |
| Context slots | 40,000 retained `(instruction, path)` records per active attempt. If exhausted, preserve global per-instruction final record; missing selected context is explicit ContextNotCaptured. Never reuse a slot for another context without exposing eviction. |
| Optional ordered trace | 10,000 events per attempt, ring retaining newest; show total/retained/dropped and first retained ordinal. Global final records/counts remain authoritative, unaffected by trace eviction. |
| History | Up to 128 immutable envelopes and total 64 MiB observation/history payload budget, whichever first. Evict whole oldest envelopes, report count and retained attempt range. A single oversize entry may be uncached; current selected scan remains visible and explains history omission. |
| Text capture | Maximum 4,096 UTF-16 code units per operand in retained observation, retaining total length and truncation marker; no surrogate splitting. Numeric/Boolean remain exact. Full final text may be inspected from its snapshot; full consumed historical text unavailable if truncated, explicitly stated. Text cut-off never changes executed value. |
| Call path | Interned immutable path IDs, depth up to 64 for monitoring. Beyond cap keep real execution unaffected, record path unavailable/truncated; exact instance binding prohibited without full identity. Execution recursion/depth policy belongs to compiler/runtime contract. |
| UI publication | At most 30 presentation refreshes/s; execution/history capture continues at scan rate. Show selected origin ID; coalescing never merges operands from different scans. |
| Baseline workload execution overhead | Added monitoring p95 <=15% and <=1 ms/scan; both thresholds must pass. p99 <=2 ms/scan added. Measure at least 10,000 scans after 1,000 warm-up. |
| Allocation | Baseline added allocation <=256 KiB/scan, large workload <=4 MiB/scan, bounded retained observation+history memory <=96 MiB after GC. Record bytes/scan and GC pause distribution; budgets exclude preexisting plant assets. |
| Render | 60 Hz target; p95 total frame <=16.7 ms and p99 <=33.3 ms for baseline viewport; monitoring refresh CPU p95 <=4 ms. Large workload visible subset <=200 boxes per frame under same budgets. Virtualize offscreen rows. |

Workloads: W1 250 networks, 1,000 nodes, 1,000 primitive tags, 20 timers, 20 counters, 50 math operations, continuous 10 ms task plus 100 ms task; W2 2,000 networks/10,000 nodes/10,000 tags, four tasks (10/20/100/500 ms), 100 call sites and 500 invocations/scan; W3 repeated calls/jumps up to 9,999 networks then watchdog at 10,001, plus shared destination/instance writes; W4 100 text operations with 0/64/4,096/65,536-code-unit text and overflow context/history caps. W4 verifies graceful, truthful truncation, not full uncapped capture. Each includes deterministic output equality with instrumentation disabled. Keep machine-wide background load recorded and repeat three independent runs.

Native readability matrix: use the common semantics contract matrix, 1280x720 and 1600x900 logical-pixel viewports at 100/150/200% Windows DPI, editor zoom 75/100/150%, both styles. Record physical backing dimensions and display availability; unavailable physical configurations remain unverified rather than counted from synthetic captures. At minimum size use scrolling/detail expansion, never overlapping/shrinking labels into illegibility. Include dense branches, long symbols, text, error states, paused history. Proposed minimum main value text 12 logical pixels at 100% zoom, full value accessible in selected detail. Actual pointer/keyboard tests remain pending permitted input; no-focus screenshots only prove rendered appearance.

W4 text lengths above the existing 255-character STRING language limit are serializer/presenter robustness fixtures only, not runnable supported programs. The compiler must reject oversized language values; separately constructed observation fixtures exercise truncation without expanding the executable type contract. Monitoring path capacity 64 is a defensive storage ceiling; executable call-depth limit remains 32 from the semantics contract.

## 9. Implementation sequence and acceptance tests

1. Add immutable envelope/typed observation contracts and coverage registry. Tests cover all 54 catalog keys and every enum member; serialization/history copies cannot mutate earlier observations.
2. Instrument actual execution and typed normalization at existing evaluation points. Preserve legacy state/diagnostics exactly; monitor-off/on golden comparison covers outputs, memory, timers, counters, task order, scan/time and errors.
3. Add session publication identity/phase tracking, aborted classification and retained history. Tests reuse scan number after Reset/reload, same-content reload, Stop/Run, force changes without execution, paused step, history selection and mid-cycle lifecycle callback changes.
4. Presenter tests for every unavailable reason/disposition; compare before later MOV, same-value write, disabled MOV, false TON/TOF/OTE, shared CTU/LOAD/RES, timer reset, force override, failed math retaining destination, failed format clearing empty/nonempty text.
5. Invocation tests: two tasks and two call sites invoke same block with differing values; choose context and inspect last global invocation/path/count. Repeated jump yields count this scan. Overflow trace/context/history must not substitute another invocation. Aborted not-reached instructions are distinct from false.
6. Integrate readable boxes and selected detail with one layout model for drawing/hit-testing. Native captures and actual input tests for both styles, minimum viewport/DPI/zoom, long values, exact diagnostic operand navigation and panels reopening from collapsed docks.
7. Run proposed workloads, record measurements and candidate identity. All required functions remain open until tests pass, including parameterized CALL observation dependent on the separate CALL contract. Source tests alone cannot qualify a different exported build.

## Review checklist

Accept/revise the identity envelope; partial-scan versus completed classification; last-invocation context contract; complete coverage/disposition mapping; bounded capture behavior; and proposed workloads/budgets. Review must also explicitly decide whether the separate executor change to watchdog text outputs/plant advancement is in scope. This packet itself authorizes no semantic execution changes and closes no measured or native gate.

### MON-ABORT-01: confirmed qualification defect and proposed correction

The watchdog path sets executionFault/Stopped, drives BOOL/numeric outputs to defaults, then still increments `_scanNumber` and simulated time (`VirtualControllerRuntime.cs:328-342,569-586`). `ExecuteAcceptedCycle` tests only `snapshot.ScanNumber == previous + 1`, then commits outputs, advances plant and constructs `VirtualControllerScanCompleted` (`VirtualControllerSession.cs:177-191`). Therefore the current “accepted/completed” classification is insufficient for watchdog aborts.

**Decision proposed for review:** emit `ScanAborted` with partial observations and completed phase flags, exclude it from successful accepted-scan totals, retain it in history as an aborted attempt, and show an unmistakable aborted badge. Preserve existing attempted-scan numbering/time advancement, memory effects, BOOL/numeric safe values, output commits and plant step during this monitoring change. Do not call that preserved path a successful scan. Keep a separate count of successfully completed attempts. A compatibility event may continue carrying its legacy name only if all consumers receive and check the explicit completion classification; migrate success-assuming consumers before claiming the defect fixed.

Tests: execute a deliberate backward-jump watchdog fixture using only in-memory runtime/plant adapters; assert attempt increments once, successful count does not increment, controller stops, observed prefix is partial, unreached rows say aborted, BOOL/numeric published outputs match legacy behavior, and commit/plant callbacks retain their existing count and order. Compare monitoring on/off outcomes. Test a recoverable DIV fault separately: full scan completes, destination retains prior value, instruction reports FailedNoWrite, and completed count increments. A thrown commit or plant callback must produce a failed-phase envelope and never a successful completion. This classification change is a reviewed contract change; changing actual fault-output policy or plant advancement requires separate explicit scope and tests.
