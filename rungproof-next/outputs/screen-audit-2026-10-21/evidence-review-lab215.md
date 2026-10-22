# Lab2.15 independent native review

Reviewed15 completedcases plusbaseline: eightactualSignals, fan/cabinetphysicalview, runtime in2 lab215-review sheets. Explicitlearnerfixture scope; no reviewerUI/backend tests.

With lightrequestFalse, selector1/2/3/0 produces fanrunTrue/True/True/False andspeed35/65/100/0. HoodlightonFalse/pilotdarkthroughout. Independently enablinghoodlight atselector0 giveshoodlightonTrue/whitecabinetpilot whilefanoff0; secondfull1/2/3/0speedwrap retainswhitepilotTrue. Turninglightoffat0darkenspilotwithoutfanspeedchange.

Lifecycleinputsselector1/lightrequestTrue givefanTrue/speed35/lightTrue. AppStop retainsinputs1/True butfanFalse/speed0/lightFalse, whitepilotdark,STOPPED3631. Run3657 republishesfanTrue/speed35/lightTrue/whitepilot. AppResetSTOPPEDscan0 clearsselector0/requestFalse/alloutputs0False.

Fanbladeorientationvariesbetweensampledrunningframes, consistentwithboundedfananimation, butstillframesdonotprove continuousrotationor35vs65vs100actualspeedratios. low_speed/medium_speed/high_speed remainFalseinallframes andphysicalspeedstackdark, so thisfixturedoesnotprove optionalindividualspeedindicatorchannels. Numeric speedclaimscomefromflatSignals, notphysicalspeeddisplay. This is anexplicitcoveragegap, notappdefectwithoutcontrollerassignmentproof. No newdemonstratedissue; postResetRun/freshrepeat andexactlatencyremainunproven. No whole-scenePASS.

Contractclarification follow-up: help explicitly requires onespeedindication, and threespeedflags areSIM-owned, notPLC-owned. Sixrunning-speedframes in two lightstates demonstrate allthreeFalse/dark despitecorrect35/65/100 commands. Previouslydescribedcoveragegap is now QA-VIS-002 S3 demonstratedfeedback/contractmismatch; exactcauseunknown. Noactualfanrotationrateclaim.
