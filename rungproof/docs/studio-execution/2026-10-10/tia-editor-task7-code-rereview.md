# Task 7 scoped re-review

2026-10-11. Verdict: **REVISE — residual identity collisions**. Reviewed the five fixes and sentinel correction against current source. No tests/builds run by this reviewer; root is running the integrated suite. This verdict concerns executable semantics, not the unimplemented Task 8 UI.

## Original findings disposition

- Exact global/frame/instance storage-key collision: corrected for identical root keys; private key selection and explicit instance membership preserve legacy globals. Derived member and edge identity collisions below remain.
- Empty/whitespace implicit typed entry: corrected by effective-entry normalization.
- Qualified global retentive timer Stop: corrected by resolving raw operands to global storage names and retaining separate instance registration.
- Bare global timer/counter members: primary case corrected; synthetic invalid-name member resolution still needs correction below.
- History invocation transfers: SnapshotBytes now charges every invocation/transfer including monitoring-off snapshots. **Correction to my original report:** InstructionObservationCapture.EstimatedBytes is dynamically calculated, so attached operands were already charged after Freeze. The earlier claim that this value was cached/stale was incorrect. The snapshot omission was real; the new CalledInstanceId charge is appropriate. Performance/current-attempt allocation remains a separate unqualified gate.

## Required residual fixes

### P1 — Generated timer/counter member names can alias legal scalar globals

`LadderCallFrames.cs:AvailableStorageKey`, `LadderCallBindings.cs:LowerForValidation`, `VirtualControllerRuntime.cs:809,836`.

AvailableStorageKey checks only the root declaration key, not the virtual member keys later appended by ResolveCallStorage. Declare a legal REAL global `@instance/one/clock.ET` initialized to 777, plus FB instance `one` with Static TIMER formal `clock`. A typed `local:clock.ET` becomes `@instance/one/clock.ET`; ResolveNumericRaw returns the global scalar 777 before checking the timer member, even though the local timer actually accumulated 10 ms. The analogous BOOL `.Q`/`.DN` case returns the wrong signal. Compiler local aliases have the same exact-root-only selection problem with globals such as `@local/fb/clock.ET`.

Reserve the complete virtual member namespace when selecting private aliases, or use typed storage/member references that cannot collide. Regression: numeric and BOOL scalar globals named like generated member paths; assert explicit global access still reads the global, while local member access reads the correct instance across multiple calls/reset. Cover counter ACC/CV and timer ET/Q.

### P2 — Invalid-scope sentinel can resolve as a legal member

`LadderCallBindings.cs:178–184`.

The sentinel now avoids exact occupied declarations, but validators also recognize TIMER/COUNTER member expressions. Declare global TIMER `clock` and another legal TIMER `@unqualified/clock`. A forbidden bare typed operand `clock.ET` lowers to `@unqualified/clock.ET`. That full name is not in occupied, but ordinary numeric validation accepts it as the second timer's member. Compile therefore succeeds and runtime executes the original bare reference against `clock`, defeating explicit scope.

Prefer an explicit scoped-reference validation diagnostic rather than synthesizing a string and depending on later validators. If retaining a sentinel, prove it cannot resolve as either a declaration OR any supported member, including future aliases. Regression: exact sentinel names plus TIMER/COUNTER roots, BOOL and numeric member variants, all must reject the original bare expression.

### P2 — Edge memory has the same unseparated legacy/generated identity namespace

`LadderInstanceStorage.cs:InstructionMemoryKey,CounterMemoryKey`.

Legacy edge memory key is raw instruction ID, while parameterized FB edge keys are generated strings. IDs only need to be nonblank and globally unique; a legacy edge node ID `@edge/instance/one/edge` is distinct from FB node ID `edge` but produces the same runtime key for instance `one`. Each execution then overwrites the other's prior input, creating lost or spurious pulses. CounterMemoryKey similarly combines an instance key and instruction ID into the same dictionary used for raw legacy counter IDs.

Use disjoint structured keys, or collision-free assignment against all raw legacy keys, rather than relying on escaped segments alone. Regression: two differing input signals evaluated in a legacy edge and a typed FB edge with these distinct source IDs; establish baselines then transition each independently and assert one pulse only on its own transition. Add the corresponding counter-key collision case.

The new tests for the original five issues are useful and the fixes are directionally sound. These residual cases are consequences of accepted unrestricted source identities and must either be handled without corruption or rejected explicitly before execution. No additional broad redesign is requested; do not weaken legacy identity compatibility silently.
