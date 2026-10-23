# Parcel V7 fresh-scene versus AppReset — independent native comparison

I inspected13 original PNGs, the matching `native-ui-actions.jsonl` records and the controlled action plans. All13 SHA256 values match their native log records; the exact manifest is [parcel-v7-controlled-comparison-review-hashes.json](parcel-v7-controlled-comparison-review-hashes.json). AppPID12768, native1021×660, local simulator; PLC disconnected. No reviewer UI input, simulator queries or application edits.

The frozen V7 input remains SHA256 `36de084f73c356c761440e21609b44dbb71a339573837349dc8422212c63fd6a`.

## What the fresh-scene sequence actually proves

- `parcel-v7-fresh-reload-leave.png` still shows Lab2.23. Its filename alone does **not** prove leaving. The later `owned-discard` frame still has the unsaved-workspace dialog.
- `parcel-v7-fresh-reload-select-parcel.png` actually shows Lab2.25 Inspection Light Toggle behind the menu. This is the real observed away-scene witness. The later `fresh-reload-scene.png` shows Lab2.23 with three requeued parcels and LADDER NOT LOADED, rather than the old stopped runtime.
- `fresh-reload-imported.png` visibly names the exact V7 file and retentive-handoff diagnostic draft. Native input records then show Run and a physical Start(612,386), followed by waits/captures.
- `fresh-reload-progress-5.png`, scan1783, is still count1. Its apparent receiver layout is not completion evidence.
- **`fresh-reload-progress-6.png`, scan2010**, directly shows sorted_count3, misrouteFalse, route0, tableclear/alignedTrue and loadedFalse. Infeed_gate_open/infeed_run/table_transfer_run areFalse. Three separate cartons rest on the front/lower, back/diagonal and right receiving surfaces. This is a second successful three-receipt result after actual scene reconstruction.

The all_received/cycle_complete rows and conveyors_run are above the visible scroll position in that final comparison image; I do not infer their values from its filename or from the three lower command rows. The original [first V7 review](parcel-v7-native-review.md) separately establishes complete/all_received and all four BOOL commands off for that first batch.

## Controlled same-scene AppReset outcome

The exact `parcel-v7-controlled-reset-cycle.json` plan uses AppPlayback Reset, AppPlayback Run, one physical Start, then six waits/captures. There is no automatic handoff predicate or AppStop in this plan.

| Original | Direct visible result |
|---|---|
| `controlled-reset-zero.png` | STOPPED scan0/count0; three parcels requeued; route0, tableclearTrue/loadedFalse/alignedFalse; shown commandsFalse. Footer reports controller reset/stopped/new Start required. |
| `controlled-reset-run.png` | RUNNING scan8/count0/route0; tableclear/alignedTrue, loadedFalse; commandsFalse before Start. |
| `controlled-reset-progress-3.png` | RUNNING scan731; count1, route0.5, alignedTrue, clearFalse/loadedFalse; table_transfer_runTrue/transfer_inhibitedTrue; small_detectedTrue, misrouteFalse; infeed and gateFalse. |
| `controlled-reset-progress-6.png` | RUNNING scan1411; same public state and same carton positions, count1. |
| `controlled-reset-stall-held.png` | RUNNING scan3786; same count1/route0.5/loadedFalse/clearFalse/transferTrue/inhibitedTrue, same physical arrangement. One carton is on the lower receiver, another at the primary transfer area, another at infeed. |
| `controlled-reset-private-live.png` | Actual native Watch is RUNNING scan4732. qa_step50, qa_scan_step50, qa_last_step50, qa_class2; route_position0.5, conveyors_runTrue, small_detectedTrue. These are read-only monitor values, not simulator-API data. |

The public stall persists across3055 advancing scans from the first listed stalled sample to the held sample, and the live Watch confirms the controller remains running afterward. This is not an OCR-triggered Stop or a paused scan. It reproduces the earlier [V7 repeat failure](parcel-v7-repeat-review.md) without that observer helper.

## Scope and causal confidence

**Native conclusion:** same frozen V7 succeeds after genuine scene reconstruction, then fails its same-scene AppReset/Run/Start repeat at count1 again. First-batch success and repeat failure both remain valid. Normal operation is possible; reset/repeat reliability is failed in these observed attempts.

This comparison strongly supports a history-dependent initialization difference. It does not directly expose the model's stored dimensions, the generic rotation-registration set or support samples. [Source decision table](parcel-sensor-reset-decision-table.md) demonstrates an orientation/world-AABB reconstruction hazard and supplies a geometric counterexample; that is separate source/model evidence. The native comparison is consistent with that hypothesis, but does not by itself prove geometry is the sole cause or constitute a verified repair. No application change or post-fix result exists in this review.

Moving-handoff AppStop/recovery remains unexecuted. A later Stop at an already-stalled state remains only its previously reviewed command-removal/hold case. No full parcel acceptance or all77-scene completion is asserted.
