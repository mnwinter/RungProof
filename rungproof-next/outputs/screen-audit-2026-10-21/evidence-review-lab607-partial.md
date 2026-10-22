# Lab 6.7 partial independent selected-checkpoint review

Reviewed eightselectednative frames in two lab607-partial-review sheets plus post-input-error native12625. Correctsceneidentifiedbyactualheader; canceledloader/OCRnamefailures excluded. No reviewer UI/backend operations. Partial12/22kgfixturecases only; no fullscenecredit.

Default12result4523shows physicalbagatnormalreceiver, bag_at_normal_exitTrue/rejectFalse, x2.62/z0/speed0, class_result1/class_1_count1/class_2_count0/class_3_count0 and allthreeprocesscommandsFalse. Actualfixture_mass_kg12 is visible; transientweighstation/weight_valid measurement is notcaptured. The OCR class_l/class_1 mismatch is evidence-readingerror, not appfailure.

Stoppedpreparation6340places nextbagatentry-3.2 withfixturemass22; PLC-visiblecounters0 whileStopped. ActualRUNNING6381moves physicalbagatx-2.79/speed.5/infeedTrue and republishesretainedclass1count1. Stop6419andheld6419freezeactualbagx-2.41/samepose across1.2seconds; speed0/infeedFalse/dischargeFalse/weighFalse and PLCcounters0. Run6433resumesphysicalbagx-2.27/.5/infeedTrue andrepublishesclass1count1. Actualnormal-exit7016andresult8605show mass22/class_result2/class2count1/class1retained1/class3zero, normalexitTrue/rejectFalse/x2.62/z0/allcommandsFalse. Post-input-error12625independentlyshowsunchanged22/end/counters1,1,0:32kgcasehadnotexecuted.

No new issue. Class3/rejectroute, actualweight-validatstation, scale/diverterreadyloss, Reset, classificationboundaries, repeatedsameclass and exactcountedge timing remainunproven. Bridgeinputfailure is not simulatorbehavior evidence.

## Completion of remaining selected cases

New six-checkpoint lab607-completion-review.jpg, actual not-ready pointpanel and compact35frame lab607-r4-weigh-rows.jpg extend the earlier partialscope. Class3 native24778has fixturemass32/class_result3, classcounts1/1/1, rejectexitTrue/normalFalse, bagx2.3/z-2.75594/speed0 and alltravelcommandsFalse. Actualphysicalbag is on the lateralrejectconveyor, distinct from12/22normalreceiver. Diverterendpositiondiffersphysically; transientreject_selectTrue/rotationtrajectory was notsampled and is notinferred from endpointalone.

ResetSTOPPED0clears allclasscounts/result0, mass12, bagentry-3.2 andcommandsOff. ScaleReadyFalse allows boundedinfeedjog: early90x-2.3/.5/infeedTrue; filename held317stillapproachingx-.0300028/.5, so not a stoppedstate. Later independentlyread not-ready-check pointpanel shows bag_at_scaleTrue/xabout0/speed0, scale_readyFalse/weighFalse/weight0/validFalse/class0/counts0/alltravelOff. NativeHealth364explicitlyrejects LOADbeforecurrentbagexits andbothtravelcommandsstop. No ignoredbuttondefect.

RestoreReady captured burst00/RUNNING2028: physicalbag supportedonscale, bag_at_scaleTrue/xabout0, speed0/infeed+dischargeFalse, weigh_cycleTrue/weight_kg12/weight_validTrue. Compactactualbottomrows across35frames show this validweighphase at00;01..34weighFalse/weight0/validFalse. This directlycloses the earliermissingweighphaseevidence, without claiming exactduration. Repeatfinal19461has normalexitTrue/rejectFalse/x2.62/z0, class_result1/classcounts1/0/0 andtravelOff. Scalepermissiverestoration thusweighs/classifies anddischargesfresh12bag afterReset.

No newissue. Boundedcases now cover12/22normalroutes,32rejectroute, movingStopretention, Reset, scale-not-readystationhold andactual12validweigh/repeat. Classificationboundaryvalues, diverter-not-readyinterruption, exactweightvalidpulsewidth, rejectmidtravelStop andcontinuousdivertermotion remainunproven. No whole-scene PASS.
