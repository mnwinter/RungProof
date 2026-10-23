# Current issue scopes and remaining discovery

Application, old evidence, Git and D backup are unchanged. This is discovery reconciliation, not the final remediation plan.

## Confirmed current findings

**Parcel Reset/repeat reliability:** frozen diagnostic V7 first batch completes three supported receivers with count3/complete/all_received and commands off. A genuine scene reconstruction and reimport completes three receipts again. Two subsequent same-scene AppReset/Run/Start attempts stall count1 with route0.5, alignedTrue, clearFalse/loadedFalse and transferTrue/inhibitedTrue. Normal success and repeat FAIL both remain valid. [Controlled comparison](parcel-v7-controlled-comparison-review.md), [first normal](parcel-v7-native-review.md), [first reset-repeat failure](parcel-v7-repeat-review.md). The source geometry/reset hazard is separate causal evidence; native images do not uniquely establish root cause. Historical V3/V4 stalls remain their exact controller outcomes. V7 phase30/count0 moving-handoff Stop/Run-alone recovery is now independently CLOSED to three receipts/all commands off; the separately reviewed stalled-state Stop remains its narrower history. Reset/repeat failure persists.

**QA-UI-002 footer:** valid authored Run clears ErrorList and executes the ladder, while outer RUNBLOCKED remains misleading. Exact repro is done; no newer closure is credited.

## Closed named discovery gaps

- Q01 drawbridge actual descending acknowledgement loss and restoration to home/open barriers/traffic release: [review](drawbridge-descent-review.md).
- Q06 Demo1 AppStop/Run/Reset at count1, followed by fresh2/3 threshold: [review](demo1-appstop-review.md). No count2 Stop retest required.
- Counter4.5/4.6 named upper boundaries, ARRAY all10 zones/end hold, and representative Viewer/Browser/Cancel/Discard/Save/reopen: latest linked reviews in the matrix. They are not remaining execution tasks.

## Route/contract questions

- Radar inspection is advertised but has no equipment action mapping; generic action panel is hidden. Prior body/transmitter clicks retain opaque/inspectionFalse. No supported click target is established, so do not classify arbitrary failed clicks as a ghost-material defect or mark the feature N/A.
- Five legacy scenes2.14/2.17/2.20/2.21/2.22 expose mapped START, whose native response is supported; separate declared Stop/Reset has no grounded physical route. Keep that documentation/accessibility question separate from already tested app playback.
- Gallery empty-belt motion is visually inconclusive; mapped motor/fan/CNC and gallery control toggles have bounded evidence.

## Current execution scope

[Native queue](native-test-queue.md):12 READY cases, six scoped closures and Q23 Stop/recovery CLOSED with Reset/repeat FAIL. Its JSON is the exact current case list, not a generic request to rerun every PARTIAL cell. Priorities remain grouping release, knife stroke, bag forward PAUSE transition (continued motion expected), while Demo2 AppStop is now closed. Lab9.3 finite operand1/10/wrap cases use actual A NEXT/B NEXT controls; no arbitrary values or25-pair expansion.

The19 original bounded-control scenes plus the newly closed drawbridge are now normalized to SCOPED_PASS controls with native citations. Historical P remains in JSON supersedes. No source-only or untested action was converted to a behavioral pass.

## Exact Q23 split now current

[Phase-qualified recovery](parcel-v7-handoff-recovery-review.md) proves actualphase30/class2/count0 atStop1333, heldpose/scan/allcommandsOff, then Run alone to3/allreceived/complete/route0/all4Off. Do not describe it as retention of count1 or as a new Start/Reset recovery. Reset/repeat remains separately FAIL; geometry hypothesis is source-supported, not a native-proven sole cause.

Additional scoped closures: Q07 retained-REQUEST freshTON after AppStop; Q20 acknowledged activeTP trigger/nonextension/freshretrigger; Q22 activeinhibit→ResetSTOPPED0 declared outputTrue→Runbaseline. References are in the current matrix and hard index; exact20ms peak and allzeroReset are not requirements.

Q04 coatinginitialindexStop/Run-alone→supportedexit2.23/alloff is now closed by10-originalreview. Its initial ventFalse predicate and actualmovingcube are observed; no pendingindexStopcase remains.
