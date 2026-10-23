# Parcel V7 phase-qualified application Stop / Run recovery

Independent original-pixel review, no UI input/state queries. AppPID12768, native1021×660, Lab2.23/local simulator/PLC disconnected. All12 originals cited below rehash-match their native action log or helper result-manifest entries. This is a fresh-reloaded V7 attempt, separate from the failed Reset/repeat.

## Stop qualification and acceptance

[observe031](parcel-v7-watch-handoff-r3-1792731378310689700/observe-031.png) shows RUNNING scan1315 and actual Watch MONITOR VALUE rows qa_step30, qa_scan_step30, qa_last_step30, qa_class2; conveyors_runTRUE/route0. The helper records exactly one Stop attempt. Its [immediate](parcel-v7-watch-handoff-r3-1792731378310689700/stop-immediate-watch.png) original still shows open menu/RUNNING scan1333, so that frame alone is not accepted-Stop proof. The [held Watch](parcel-v7-watch-handoff-r3-1792731378310689700/stop-held-watch.png) as actually saved shows STOPPED1333; the independently later [settled](parcel-v7-handoff-r3-post-helper-settled.png) original clearly confirms STOPPED1333, conveyors_runFALSE and retained private30/30/30/class2. This supports application Stop in the intended medium-class admission/handoff phase, not a claim that every phase30 pose is identical.

[Stopped physical](parcel-v7-handoff-r3-stopped-physical.png) and [held physical](parcel-v7-handoff-r3-stopped-physical-held.png), separated by logged1.5s, both show STOPPED1333 and the same actual carton poses: a carton partly across the primary approach/table region, a carton toward the front receiver path, and a remaining infeed carton. Both show count0, tableclearFalse/loadedFalse, route output0, table_transfer_runFalse/infeed_runFalse/gate_openFalse. transfer_inhibitedTrue persists. Combined with settled Watch conveyorsFalse, all four declared BOOL commands are observedOFF. The receipt count was0 at this interruption: do not describe it as retaining an already-counted receipt.

## Run alone and actual recovery

The action log `parcel-v7-handoff-r3-recover.json` contains view navigation, then App Playback menu(203,34)→Run(220,70); after that only waits and Signals scrolling. There is **no second physical Start or Reset**.

| Original | Actual recovery observation |
|---|---|
| [run-alone](parcel-v7-handoff-r3-run-alone.png) | RUNNING1341; count0; table_transfer_runTrue, actual tableclear/loadedFalse; retained process resumes. |
| [recovery1](parcel-v7-handoff-r3-recovery-1.png) | Scan1568; count1, route0.5/tableloadedTrue/clearFalse; cartons have moved relative to stopped pair. |
| [recovery3](parcel-v7-handoff-r3-recovery-3.png) | Scan2019; count1, route0/tableclearTrue, transfer_inhibitedFalse; parcels distributed along receivers. |
| [recovery5](parcel-v7-handoff-r3-recovery-5.png) | Scan2473; count3, route0, infeed/gate/table commandsFalse, transfer_inhibitedFalse. Three cartons visibly on separate supported receiver paths. |
| [recovery6](parcel-v7-handoff-r3-recovery-6.png) | Scan2700; same final count3/pose/commands. |
| [upper Signals](parcel-v7-handoff-r3-recovery-upper-signals.png) | Scan2739; all_parcels_receivedTrue/cycle_completeTrue/misrouteFalse; conveyors_runFalse/infeed_runFalse/gate_openFalse, aligned+clearTrue/loadedFalse and route0/count3. |

This exact fresh-load medium-class phase-qualified AppStop holds actual pose/scan and Run alone resumes to three completed receipts. Native recovery is observed; no application repair is implied. This **does not clear the independently observed actual Reset/repeat count1 stall** or the source-supported transform-ordering concern. It also does not establish arbitrary phase/permutation robustness or a safety-rated stop. Helper transitional labels are not a new product issue.
