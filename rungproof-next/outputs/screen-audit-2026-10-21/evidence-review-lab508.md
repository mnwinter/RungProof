# Lab 5.8 independent selected-checkpoint review

Reviewed16selected native checkpoints in two lab508-selected-review sheets retaining hinged deck, both road gates, visible traffic head,15Signals and runtime. Loaded learner fixture scope only; no reviewer UI/backend operations. No hardware vehicles/boats are modeled or credited.

OPEN without stoppedack4810closes barriers0 and shows physicalred but holds bridgehome0, raise/lowerFalse, requestTrue/stoppedFalse. Stoppedack4890permits actualhinge14degrees/raiseTrue. Ackloss4915 andheld4982freeze18.2degrees/samephysicalpose with bothcommandsFalse while runtime advances67scans. Restore5029resumesraise27.65. AppStop5062 andheld5062freeze39.2/samepose, allPLCcommandsFalse and trafficheadunlit across1.2seconds. Run5074resumesraise43.4/red, later5275deck70/raisedlimitTrue/homeFalse, raise/lowerFalse, barriersclosed0/red andtrafficreleaseFalse.

CLOSE6234lowersactualdeck60.55 then6338at24.15, lowerTrue/raiseFalse/barriersclosed/red. Home7767deck0/homeTrue/raisedFalse, bothmotioncommandsFalse, barriers90/openTrue andvisiblegreen/trafficreleaseTrue. Physicaldeckhinge andgateorientation agree with nativeangle/endpoints. ResetSTOPPED0returnsdeck0/gates0, bothcommandsFalse, request/stoppedFalse andlightsunlit; Run132reopensgates90/green whilehome. Freshrepeat533again70/raisedTrue/commandsOff/barriersclosed/red; freshCLOSE890returns0/homeTrue/gates90/green. No simultaneousraise/lower is seen in these selectedstates.

No new issue. Exactinterlock response scans, continuousdirectionexclusion, Acklossduringlowering, operatorrequestreversalmidstroke, independentmotion_inhibitedinput and abnormalendpointfeedback remain unproven. No whole-scene PASS.
