# V5 native input evidence and remaining hold

The human authorized screen, keyboard and mouse use. Root operated the actual exported Windows app through the computer-use skill. Earlier input-reservation statements in the acceptance checklist are superseded.

## Exact candidate

`rungproof-next/artifacts/offline-demo/20261011-005354-3c94d048`

Candidate manifest SHA256: `4EDA253511D25813DB3AEB419B323971EC02B8FA88146DED51A8C2DD4BB495E9`.

The export producer passed source UTF8, semantic manifest, package integrity and the required exported verifier gates. A complete copy at `artifacts/offline-demo-copy-verification/cb142e62079b4551ad56e3de3ce41820` passed the package checker for all 209 listed files. Root independently compared all 210 actual files, including the manifest, against the original. Dot independently verified all 209 listed hashes and the manifest hash. These checks establish copied bytes, not release approval. Original candidates remain immutable.

## Challenge fixes and actual input checks

- Invalid starter Test now displays its validation issue in the challenge header, remains Idle, starts no worker and awards no progress. Actual rendered text was readable at the observed native window size; the challenge header retained its height.
- Blank output coils now show `Select output…`. Assign preserves a blank operand rather than committing the placeholder. Actual mouse selection and keyboard dropdown selection committed `ConveyorRun`; Undo restored the blank operand. The saved project contains `ConveyorRun`, with no placeholder operand.
- Keyboard contact insertion, opening coil properties, Undo/Redo, Test, results inspection and saving an editable draft were exercised. The intentionally incorrect circuit failed the independent cases without a current proof. This is not a complete keyboard-only accessibility acceptance.
- The exported native UI verifier separately passed invalid starter, external guards, passing result/save/reload, failure evidence, held-worker cancellation and stale-award rejection. Its output explicitly leaves keyboard and screen-reader acceptance false.

Actual draft evidence is outside the immutable package at `.tools/studio-verification/native-input-v5-graded.rpproj.json`.

## Demo samples

Demo1 actual pulse counting reached three and lit the lamp; Stop removed the output. Demo2 actual request led to a delayed lamp; exact delay timing was not measured. Demo3 actual Start moved the carton to its photoeye, stopped the motor there, and Reset restored the initial position and inputs. Demo5 actual Start moved one carton onto the pallet, and Stop returned the sampled outputs to their stopped state. These are bounded workflow samples, not full five-demo acceptance.

## Demo4 evidence correction: prior HOLD withdrawn

The prior zero-at-scan-1162 report was a screenshot misread and is unsupported. Direct inspection of the original `.tools/studio-verification/native-v5-demo4-discrepancy.png` shows `pallet_count = 1`, all three inputs TRUE, and `pallet_count_valid = False`. The authored counter preset is five; validity requires its done bit and both permissives. Count one with validity false is expected. This screenshot does not establish a counting defect or a failed ordered retry. The prior zero-count behavior HOLD is withdrawn; complete native acceptance is not established.

Origin: chat `Plan RungProof’s Direction`, thread `01a12741-8c6c-7d71-bd15-fc03cb58c06c`, native input turn `01a12869-cca7-7b61-8db1-19e63ba6ec85`. Original records: `C:/Users/mattn/.codex/sessions/2026/10/10/rollout-2026-10-10T12-19-29-01a12741-8c6c-7d71-bd15-fc03cb58c06c.jsonl`, lines 4581–4642. Line 4634 saved the original PNG at 2026-10-10 18:01:06 PDT (2026-10-11 01:01:06 UTC). PNG SHA256: `43D1104217959EBBE40B545E21D3F79596405DBF90C38B0BFBCB32CDE9666521`. The incorrect zero-count commentary at line 4641 is superseded by this correction.

The exact originating artifact was V5 `20261011-005354-3c94d048`, built 2026-10-10 17:54:30 PDT (2026-10-11 00:54:30 UTC), source commit `1aeee4d7ed23fa938f5227942e7727b4ef6981dc` with dirty source captured by its freeze inventory. This is the immutable export identified above, not the later v4 visual source revision. Ordered retry: reload Demo4 from catalog, Reset, Run, TypeValid ON at 18:00:23, CountRequest ON at 18:00:28, then Detected ON at 18:00:34 PDT. The saved settled view at scan 1162 shows the resulting count one. Earlier detection before permissives cannot establish a failure: enabling permissives while detection is held does not manufacture a detection edge.

Subsequent unchanged V5 sessions passed:

1. Fresh launch, open Demo4 Logic/Watch, ordinary Run, then TypeValid ON, CountRequest ON, Detected ON: count one, effective watch detector TRUE with GOOD scan status. Stop/Reset/Run in that loaded session also counted one.
2. Catalog reload, Reset before Run, ordered toggles and four additional OFF/ON detector edges: count five, validity TRUE. Original screenshot: `.tools/studio-verification/native-v5-demo4-retry-five-count.png`.
3. Fresh launch directly into Demo4 operator view without opening Logic, Reset before first Run, ordered toggles: count one at scan 26. Original screenshot: `.tools/studio-verification/native-v5-demo4-fresh-operator-reset-pass.png`.

These recorded samples are consistent with the corrected original screenshot. There is no demonstrated earlier zero-count failure to explain. Dot and an independent specialist found no defensible code defect in the factory packet, edge/counter implementation or Reset path. No Demo4 behavioral changes were made.

A further fresh-process control, Demo3 Run/Start/Stop/Reset then Demo4 ordered inputs, counted one (scan 27). That same process subsequently entered the challenge, edited and graded the intentionally incorrect circuit, saved its draft, left to Demo3 Run/Start/Stop/Reset, then returned to Demo4 and counted one (scan 430). Screenshots are `native-v5-demo3-history-demo4-pass.png` and `native-v5-challenge-history-demo4-pass.png` under the same verification directory. This second route had already visited Demo4 before challenge entry; it does not isolate first Demo4 entry after a challenge. The diagnostic challenge draft is preserved as `native-v5-history-challenge.rpproj.json`.

First Demo4 entry after challenge departure remains separately unverified in the broader session-history matrix. It is not a proven defect and does not justify a behavior fix or a defect HOLD. If a real counting failure is independently observed later, preserve effective permissives, `always_scan`, counter ACC, task runs and loaded CALL/block state before proposing a change. No fresh UI input, tests, captures or implementation were performed for this saved-evidence correction. Any change outside the approved challenge and five-demo presentation scope needs a concrete plan, Dot review and human authorization.

## Open acceptance

Compact native layout, horizontal scrolling, complete keyboard-only navigation, modal Escape behavior, actual Windows DPI settings and screen reader remain unaccepted. Clean-machine, GPU, asset/runtime/font rights, signing and distribution gates remain open. No live PLC, physical safety or commissioning qualification is claimed. No commit, push, deployment or public distribution occurred.
