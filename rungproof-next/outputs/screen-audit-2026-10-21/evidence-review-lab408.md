# Lab 4.8 independent selected-checkpoint review

Reviewed selected native normal count1/count2/count3, transfer/recovered gate, explicit guard Health and clear-loss checkpoints in lab408-selected-review-1/2.jpg; no exhaustive full-frame sweep. Loaded learner CTU fixture scope only; no reviewer UI/backend operations.

Normal actual count1/3756, count2/8125 and count3/11966 correspond to one/two/three physical cartons; group_ready onlyTrue at3, release initiallyFalse and guided stop lowered0 physicallyblocking. Transfer12730 shows group_releaseTrue/conveyorTrue, stop_position100/raisedTrue and raisedgate, threecartons onreceivingrollers, receiver_detected/occupiedTrue. Transfer13017 shows receiveroccupiedTrue/transfer_completeTrue, conveyor/release/readyFalse and gateclosing6; latercompleted15539retainscount3, gatefullylowered0 and allthreecartonsreceiver. Fixture CTU does not automaticallyresetcount: initial expected0 was a QA expectation error, notappbug. First short package_detected pulse was missed by slow polling; count1 is actualnativeproof, no missingpulsebug. Full transientcountedge timing remains unproven.

Native Health11751 explicitly rejects prematureLOAD: requires idlefeed/freegroupposition, wait for carton tostop orReset afterthreecartontransfer. RetriedLOAD later producescount3/physicalthirdcarton, consistent with guard. This is not a failed button.

Fresh repeat firsttravel245 has count1/pulseTrue/conveyorTrue. Clearloss265 andheld336 retaincount1/package_detectedTrue butconveyorFalse/releaseFalse, samecartonpose and loweredstop0 across1.2seconds while runtime advances. Clearrestore585conveyorTrue and cartonmoved, retainedcount1. Filename second-count actual948stillcount1, so not credited as secondcount. Repeat release/Stop recovery remains pending terminal confirmation at this report checkpoint.

No new issue. Stop during physical release, complete freshthreecartonrepeat, countpulseexacttiming, invalidsensor/fault cases and continuousrollerrotation remain unproven. No whole-scene PASS.

## Proper r3 moving Stop and completed repeat

Eight selectednative frames in lab408-r3-review.jpg close moving-carton AppStop/repeat coverage. Secondmoving8369has two physicalcartons/count2, conveyorTrue, package_detectedTrue and loweredstop0. Stop8409/held8409 freezes samecartonpose, conveyor/releaseFalse, visiblecount0, sensorTrue across1.2seconds. Run8423republishesretainedcount2/conveyorTrue and cartonadvances without an additional count; secondstaged9197has two cartons/count2/sensorFalse. Thirdload reachescount3/readyTrue by9664, stillreleaseFalse/stop0. The filenames release-checkpoint9664 and transfer-checkpoint9850 are pre-release actualstates, not releaseproof. Lateractualcomplete10456shows threecartons onreceiver, receiveroccupiedTrue/transfer_completeTrue, gate0, count3retained and conveyor/ready/releaseFalse. Normal earlier12730remainsraisedgate/releaseproof. No Stop during release is claimed.

The earlier r2 release wait failed because second/thirdLOAD actions were explicitly guarded whilefeednotidle; Health explains spacing. It never reached Stop actions and adds no release/Stop credit. No appissue. MovingfeedStop/Run and completedfreshthreecartonrepeat are now supported; physicalrelease-specific interruption remains unproven.
