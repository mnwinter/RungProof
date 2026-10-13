# Task 7 final scoped code review

2026-10-11. Verdict: **APPROVE the reviewed execution foundation for Task 8 integration.** No unresolved blocking defect found in the five original fixes and three residual namespace fixes. This is source review approval, not whole-editor, native, performance or release acceptance.

Root reports `integration28`: 285 passed, zero failed; `build28`: clean. This reviewer inspected implementation and regression source but ran no builds/tests independently.

## Residual findings closed

1. **Private root/member aliasing:** `LadderStorageNamespace.Names/Reserve` now reserves the root and every supported TIMER/COUNTER member against global declarations and other private identities. Compiler aliases and runtime storage both use it. Runtime seeds occupied names from all expanded globals; frame cleanup removes only names reserved for that private declaration. Tests cover numeric ET, BOOL Q, counter ACC/CV, compiler alias collision and reset without corrupting globals.
2. **Resolvable invalid-scope sentinel:** removed. `ValidateScopedOperands` now diagnoses original operands directly before lowering, checking explicit global/local identities and valid typed member names. Literals are permitted only at applicable operand positions; ordinary instruction validation still checks type and operation legality. Sentinel-like legal globals can no longer convert a rejected bare expression into an accepted reference. Tests cover timer/counter numeric and BOOL member paths.
3. **Legacy/generated edge memory collision:** `InstructionMemoryIdentity` separates scope and source ID. Legacy scope is empty; FB/FC scope is explicit and disjoint. Counter previous-input keys also separate instance identity and instruction ID. Runtime writes, reset/clear operations and observations use the structured keys. Tests independently transition legacy and typed signals with deliberately colliding former textual keys.

The earlier effective-entry, qualified global RTO retention, exact storage collisions and transfer-history accounting fixes remain consistent with the contracts. The prior rereview correction stands: observation EstimatedBytes was already dynamic; the actual omitted history payload was Snapshot.CallInvocations, now charged.

## Task 8 gate and remaining evidence

Task 8 may expose the parameterized authoring controls against this foundation, provided every control produces the reviewed explicit modes/IDs/bindings and passes the same compiler gate. Invalid drafts must remain editable without replacing a valid running program. This approval does not cover new conversion/UI logic before its own review.

Contract acceptance combinations still requiring qualification include all static-timer true/false/true sequences and reset combinations, STRING/ENUM/INT binding/domain boundaries, normal nested distinct-instance calls and persistent Output scenarios, parameterized monitoring-on/off equality including force/abort, complete malformed-schema and migration fixtures, and the proposed performance/current-attempt memory budgets. These are retained verification obligations, not newly established source defects. Root should close them before claiming all functions fully qualified. Native editing, exported candidate identity and whole-editor routes remain separate gates.
