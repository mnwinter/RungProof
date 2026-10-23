# Parcel V7 independent native review — first completed batch

Reviewed eight original PNGs and corresponding entries in `native-ui-actions.jsonl`. All eight recalculated SHA256 values match the recorded originals. AppPID12768, native1021×660, visible Lab2.23 Parcel Size Sorter; local runtime RUNNING and PLC disconnected. This reviewer supplied no input. The unchanged application is running a revised V7 diagnostic learner controller; this is **not an application repair**.

| Original | Actual visible result |
|---|---|
| [engineering-signals-initial](parcel-v7-r1-engineering-signals-initial.png) | Scan1158; count0; received/completeFalse; conveyors_run/infeed_run/gate_openFalse; table clear/alignedTrue, loadedFalse; route0. Three queued parcels at infeed visible. |
| [physical-start](parcel-v7-r1-physical-start.png) | Scan7263; physical green cabinet button click(539,396), accepted-action footer SORT THREE-PARCEL BATCH MOMENTARY. conveyors_run/infeed_runTrue, count0, completeFalse. |
| [process-started-wide](parcel-v7-r1-process-started-wide.png) | Scan7331; large_detectedTrue, gate_openTrue, infeed_runTrue; count0. Moving queue geometry differs from initial state. |
| [progress1](parcel-v7-r1-progress-1.png) | Scan7606; count0; medium_detectedTrue; gate closed and infeedFalse; a parcel visibly across the primary transfer area, table clearFalse/loadedFalse. |
| [progress2](parcel-v7-r1-progress-2.png) | Scan7884; count1; route0.5, small_detectedTrue. One carton on front/lower receiver, next near primary center; misrouteFalse. |
| [progress3](parcel-v7-r1-progress-3.png) | Scan8160; count1; route1, primary_table_loadedTrue/clearFalse/alignedTrue. Actual next carton over round transfer table; first remains on receiver. |
| [progress4](parcel-v7-r1-progress-4.png) | Scan8440; count2; route0/tableclearTrue/loadedFalse. Three distinct cartons visible across the three receiver paths; received/completeFalse while last receipt is pending. |
| [progress5](parcel-v7-r1-progress-5.png) | Scan8715; **sorted_count3, all_parcels_receivedTrue, cycle_completeTrue, misroute_detectedFalse**, route0/tableclearTrue/alignedTrue/loadedFalse. conveyors_runFalse/infeed_runFalse/infeed_effectiveFalse. **infeed_gate_openFalse**; the adjacent infeed_gate_clear feedback isTrue. |

The final original visibly contains three separate cartons resting on supported receiver conveyor surfaces: one lower/front, one upper/back diagonal, and one rightward horizontal. Their placement is materially different from the original queued infeed. Wide-scale pixels alone do not provide readable lane names or precise class measurements; the receipt count and misroute flag are additional visible plant feedback, not a substitute for physical observations.

The logged batch contains one physical Start and then waits/camera framing; progress frames are separated by approximately5.5s wall-clock logging. Final observed completion is at most about30s after the captured Start (scan delta1452≈29.04s at displayed20ms scan). No exact completion instant is claimed. Timestamp strings use−07:00 despite the `utc` property;21:22 corresponds to04:22 UTC October23.

## Reconciliation and remaining boundary

V7 provides a viable LAD policy that completes one actual native three-parcel batch on the unchanged application. A blanket claim that normal completion is impossible or universally app-blocked is contradicted by this evidence. Historical V3 count1 and V4 count2 stalls remain genuine outcomes of those specific controllers; neither their evidence nor the separate feedback/physical-contract concerns are erased by this successful policy. This review does not resolve their cause or classify the controller workaround as an application repair.

Application Stop at medium handoff/recovery, Reset and repeatability are pending at this stage. No all-scenes or full parcel lifecycle acceptance is claimed.


## Correction and final lower-Signals confirmation

An initial reviewer transcription confused adjacent rows in progress5. Reinspection of the exact original shows **infeed_gate_clearTrue** (feedback) and **infeed_gate_openFalse** (command). The table above is corrected; the earlier gate-openTrue statement is withdrawn.

The additional [complete-lower-signals](parcel-v7-r1-complete-lower-signals.png) original, scan12119, directly shows table_transfer_runFalse, infeed_gate_openFalse, infeed_runFalse, route_position0, sorted_count3 and transfer_inhibitedFalse. Its recalculated SHA256 matches its action-log record. Combined with progress5's conveyors_runFalse, the four declared BOOL actuator commands are observedOFF after completion; route_position isREAL0. These are two successive final observations, not a claim of simultaneous hidden-state inspection.
