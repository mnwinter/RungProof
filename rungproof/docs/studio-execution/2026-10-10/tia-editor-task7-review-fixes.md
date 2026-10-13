# Task 7 review fixes

2026-10-11. Implementation response to `tia-editor-task7-code-review.md`. Root owns all test/build runs and final acceptance. No native or performance claim.

Root's `integration24red` reproduced the five review findings and the related compiler synthetic-namespace collision (273 other tests passed). Fixes now awaiting green and scoped re-review:

1. Runtime instance/frame storage chooses private keys against every declared global and currently occupied private key. Instance identity maps retain the chosen key; initialization and cleanup cannot overwrite/delete a legal global of the preferred private name. Timer/counter instance policy uses explicit membership, never a name prefix. Compiler local aliases similarly avoid expanded global names, preserving legal `@local/...` declarations. No new global-name restriction.
2. Entry validation uses the effective first-block fallback for empty/whitespace EntryBlock. Both FC and FB implicit entries reject before execution.
3. Retentive timer registration now resolves qualified global operands to actual storage names and separately registers local instance timers. FC/FB global RTO tests compare accumulation across Stop/Run/Reset with a stable ID different from display name.
4. Bare global TIMER/COUNTER member roots are detected during lowering, covering BOOL and numeric reads. Explicit global and local members continue using declared scope. `integration-25.log` passed the other 279 tests and reproduced the invalid-scope sentinel collision with a legal diagnostic-like global name. The sentinel now chooses an undeclared name against all occupied aliases/globals; final rerun pending.
5. Accepted-scan history charges every retained invocation and transfer, including values, reference/phase/formal strings and record overhead. Monitoring-off transfers are included. `InstructionObservationCapture.EstimatedBytes` already computes from current records on access, so attached post-freeze operands are counted without a cache invalidation change; CalledInstanceId is now charged too. Conservative duplicate representation charging is intentional. Tests execute 20 typed calls with 256 STRING Inputs, monitoring off/on, then drive history beyond 64 MiB and check whole-entry eviction plus prior-envelope immutability.

Current-attempt memory/performance budgets and remaining Task 7 acceptance combinations are still unqualified; these corrections do not establish full editor completion.

## Residual namespace re-review corrections

The subsequent `tia-editor-task7-code-rereview.md` identified three remaining namespace boundaries. Root's `integration27-red` reproduced all five new regression fixtures while 280 existing tests passed. The implementation now:

- Reserves each private storage root and every supported TIMER/COUNTER virtual member name against global declarations and other private storage. Compiler aliases use the same reservation policy. Legal global names remain unrestricted, including names that resemble internal storage.
- Diagnoses invalid parameterized operand scope directly against source operands with `VC_CALL_SCOPE` before lowering. No synthetic invalid-operand sentinel remains; timer/counter member names cannot turn an invalid bare operand into an accepted reference.
- Keys edge and counter previous-input memory by separate scope and source-ID fields. A legacy instruction ID that resembles an internal scoped name cannot alias an instance's history. Counter observations query the same structured key as execution.

The regression fixtures cover private ET/Q and ACC/CV collisions, compiler alias collisions, Reset, direct bare-member scope diagnostics, and independent legacy versus instance edge/counter transitions. Green rerun and scoped re-review remain pending at this entry.

Root verification: integration28 passed all 285 tests; build28 was clean. All five residual regressions passed. Source frozen for final scoped re-review. Performance qualification remains root-owned and pending.
