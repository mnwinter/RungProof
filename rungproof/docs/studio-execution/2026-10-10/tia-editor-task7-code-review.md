# Task 7 independent code review

2026-10-11. Verdict: **REVISE before Task 8 acceptance**. Read-only review of the frozen compile/schema/runtime sources against the semantics and monitoring contracts and implementation handoff. Root reports integration23: 270 passed; this reviewer ran no builds or tests. Findings below are source-proven execution paths; the proposed reproductions still need root-run regressions. No claim of complete Siemens parity or native qualification.

## Findings

### P1 — Internal call storage can overwrite and delete declared globals

`LadderCallFrames.cs:34–50,115–117,144–146`; `LadderCompiler.cs:60–63`.

The compiler permits any nonblank unique global name. Runtime internal storage shares the same dictionaries and string namespace: `@instance/<escaped instance>/<escaped formal>` and `@frame/<number>/<escaped formal>`. Escaping the segments prevents collisions between internal identities but does not separate globals from internal storage. A valid global named `@instance/one/out` is overwritten by initialization of instance `one`, formal `out`. A global named `@frame/2/out` is overwritten by that FC frame and removed from values AND declarations on return. This causes unrelated global corruption and can make subsequent validated instructions throw.

Fix with disjoint typed storage identities/dictionaries, or an explicit versioned reserved namespace validation policy that preserves existing legacy programs rather than silently changing their meaning. Regression: both collision names, differing initial values/types, instantiate/call/return/reset, then read and write the original global; assert no corruption or a clear compile diagnostic before execution.

### P1 — Empty entry fallback bypasses the prohibition on typed task entry

`LadderCallBindings.cs:116–120`; `LadderCompiler.cs:292`; `VirtualControllerRuntime.cs:119–121,335–353`.

Validation checks the literal `program.EntryBlock`, but compiler/runtime use first block when empty/whitespace. With first block Parameterized and no explicit tasks, a typed body is executed without TryEnterCall, input sampling, instance frame or storage mapping. Local operations can read default/ghost values or throw, despite successful compile. Validate the effective entry after the same normalization used for task construction. Regression: empty and whitespace entry, typed first FC and FB, no tasks; reject without replacing a loaded runtime. This confirms the implementer's known likely gap.

### P2 — Qualified global retentive timer loses retained state on Stop

`VirtualControllerRuntime.cs:114–118,198–208`; `LadderInstanceStorage.cs:39–46`.

The retentive set is populated from raw source timer operands. In a parameterized body an RTO uses `global:timer-id`; execution resolves that to the global timer's actual name. Stop looks up the actual timer key and misses the raw qualified reference, so it resets ET/Q as though nonretentive. RegisterInstanceRetentiveTimers handles local FB timers only. Normalize global timer operands to ultimate global names when registering retentive timers; preserve existing legacy behavior. Regression: FC and FB using explicit global RTO, accumulate, Stop, Run and Reset; compare with equivalent legacy global RTO, including stable ID different from display name.

### P2 — Bare global timer/counter members still bind implicitly inside typed blocks

`LadderCallBindings.cs:158–168`.

The bare-name rejection checks expanded declarations by exact operand name. A bare `counter.ACC`, `counter.DN`, `timer.ET` or `timer.Q` is not a declaration itself, so Map returns it unchanged. Ordinary member validation accepts the root declaration and runtime reads shared global state. This violates explicit scope and makes a same-named local/static declaration particularly misleading. Reject unqualified recognized members based on their root declaration, while keeping constants and supported quoted text intact. Regression: BOOL and numeric timer/counter members in typed contacts/compares/MOV; bare fails, explicit global identity succeeds, explicit local static member succeeds. This confirms the implementer's remaining review target.

### P2 — CALL transfers bypass retained-history payload accounting

`LadderCallFrames.cs:148–176`; `VirtualControllerRuntime.cs:914`; `VirtualControllerAcceptedScan.cs:8–19`.

Every invocation retains all transfers in Snapshot.CallInvocations, even when monitoring is disabled. SnapshotBytes does not charge that collection. AttachCallObservations replaces operands and diagnostics after collector Freeze without recalculating capture EstimatedBytes. Thus both snapshot transfers and newly attached observation payload escape the advertised 64 MiB history charge. Large formal lists/repeated calls can retain much more memory than reported and defeat whole-envelope eviction; this is a correctness issue independent of the still-unmeasured performance targets.

Charge every retained transfer/value/string and attached operand, avoiding incorrect double-count assumptions; consider bounded invocation retention with explicit unavailable/count metadata rather than silent omission. Regression: repeated typed calls with many STRING formals, monitoring on/off, history overflow; independently account retained payload and verify eviction/omission metadata and immutable prior envelopes. Do not treat existing collector cap tests as coverage of post-Freeze additions.

## Reviewed paths without a new blocking defect found

- Binding completeness, exact formal types, Input read-only checks, Output/InOut writable actual validation and explicit Discard.
- Initial values flow through ordinary declaration validation after lowering; no need to duplicate those checks solely because the first binding-validation loop omits them.
- Ultimate forwarded InOut identity is resolved before frame creation or body effects; Output plus Input alias remains sampled/copy-out semantics.
- Normal RETURN and fallthrough perform copy-out; watchdog/active-instance failures suppress it while FB persistent values and prior global/InOut effects survive.
- Per-instance static timer quantum is consumed only by Advance and is not replenished by reset. Legacy global timer per-evaluation policy remains separate.
- FB instance memory and escaped internal segments; FC edge ancestry and shared serial-task call path policy.
- Force publication is applied after program execution, consistent with current global write behavior; existing force test covers publication rather than every within-scan observation.
- Schema readers now reject the particular null required record fields identified by the implementer. This is not exhaustive malformed-input qualification.

## Remaining acceptance coverage

In addition to the exact regressions above: true/false/true repeated static timers for every kind; nested normal distinct-instance calls and same-instance sequential persistent Output; typed STRING/ENUM literals and invalid domains/lengths, INT boundaries, unknown enum record values; parameterized monitoring-on/off equality including abort and forces; transfer history bounds; source/editor/runtime roundtrip with all new metadata and invalid drafts; malformed JSON must diagnose without exceptions. Existing tests are meaningful but do not close these combinations or migration fixtures. UI authoring, native checks, performance budgets and same-candidate export remain outside this review.

Fix the five concrete findings and obtain scoped re-review. Task 8 may be designed against these contracts, but do not call executable CALL support accepted or expose it as complete on the strength of the 270-test count alone.
