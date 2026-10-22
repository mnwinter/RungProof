# Lab2.22 native motion and interruption evidence review

Reviewed33 motion00..32 frames, acceptedSTART Health crop, finalhome nativepoints, and8 completed interruption/repeatframes. Six lab222-review sheets plus2interrupt-review sheets preserve readable actualSignals and physicalplant. No reviewerUI/backend checks. Matching QA reference scope; not entirelesson completion.

Both drillposition values rise7.06→23.53→54.12→70.59→100 with feeds/runsTrue and depthTrue at100. Physical spindle/quill projections change with these phases, though exactsmallstroke distances are not measured. A retracts first97.5→72.5→55→22.5→5→0 whileBstays100/depthTrue/feedTrue; transfer remainsposition0/homeTrue/extendFalse. OnceAhomeTrue, B retracts97.5→72.5→47.5→30→5→0. Transfer only appearsmoving whenBalsohomeTrue: extension26.67→70→100/endTrue; laterretract68.33→35→1.67→0/homeTrue. Physicalplate/slide unmistakably movesrightthenleft in agreement. Allcapturedtransfermotion cases havebothdrillhomeTrue; this is bounded observational proof, not forcedunsafecommand interlock challenge.

Finalmotion24..32 andfinalhome showbothhomeTrue/positions0, drillfeed/retract/runFalse, transferhomeTrue/position0/commandsFalse. Explicit finalnativepoints verifybothfeedFalse despitehelperOCRfragmentmiss; no appfailure inferred. cycle_complete staysFalse/statusamber throughout this fixtureonepass; no fullSIMcyclecomplete claim or defect inferred fromthat.

Interruption: AppStopmidfeed early/late bothSTOPPEDscan63, A/Bposition55.2941, allfeed/retract/runFalse, samephysicalpose after1.2secondplannedwait. Runscan92 resumesbothfeeds/runs andposition89.4118. ResetafterinterruptionSTOPPEDscan0 returnsbothdrills/transferhome0 andallcommandsFalse. Freshrepeat feed37.6471, staggeredAhome/Bdepth100, transfer55 onlybothhome, finalallhome0/off corroborate a second boundedmotionpass. Health explicitlyrecords STARTaccepted/momentary beforeoriginalpass.

No newdemonstratedissue. Exactstrokevelocity, spindlecontinuousrotation, arbitraryunsafecommand injection, preciseacceptedpulsewidth, machineStopdistinctfromAppStop, fullSIMcompletion andalternatelearnerlogic remainunproven. No whole-scenePASS.
