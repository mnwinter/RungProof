# Lab 5.9 independent selected-checkpoint review

Reviewed16selectednative frames in two lab509-selected-review sheets, retaining supportedbag, entry/exit beamregion, tenSignals andruntime. Loaded learnerfixture scope only; no reviewer UI/backend operations.

FreshSTART1970movesbag from entry-1.74 forward atbelt_speed.5/runTrue/reverseFalse, stoppingphysicallyatexit1.28 with exitTrue/runFalse/speed0/PAUSEclearFalse2301. Laterexitpause4030andSTARTblocked4108retain sameposition/off. CLEARalone4194sets pause_clearTrue butdoesnotstart. FreshSTART4266commandsreverseTrue/runTrue/speed-.5 andbagmoves1.08. Clearloss4325/held4399freezeactualbag.62/samepose withspeed0/runFalse/reverseTrue while runtimeprogresses74scans. Clearrestore4439resumesreverse to.35.

AppStop4472andheld4472freezeactualbag.02/samepose across1.2seconds; run/reverseFalse/speed0. Run4484restoresreverse/runTrue/speed-.5 andbag-.1, eventuallyentry5452bag-1.58/entryTrue/exitFalse/run+reverseFalse/speed0. Physicalbagremains supportedonbelt throughoutselectedmotion; endpointbeams agree with nativeposition/flags. ResetRun25returnsinitial-2/entryTrue, commandsFalse andpauseclearFalse. Freshrepeat reachesexit555/1.28/stopped, then afterCLEARalone/freshSTART reachesentry966/-1.58/stopped, confirmingbothdirections/endpointsagain.

No new issue. Exactendpoint/scanlatency, entry-directionpauseinterruption, midtravelfreshSTART behavior, independentmotion_inhibited, abnormalbeams andcontinuousbeltanimation remain unproven. No whole-scene PASS.
