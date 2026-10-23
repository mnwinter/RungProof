# Post-backup drill and elevator acceptance checkpoint

SourceB5/backuppush41188c70 unchanged; rootliveapp44956, QAbridgev10small1021x660. New local QA inputs/screens only.

Drill V3: small-drill-r1 actualfeed→AppStop freezes midstroke, bothlimitsFalse/allmotioncommandsOff. r2Run completes, AppReset returnsactualtop withhandrequestsFalse andoutputsOff. r3freshqualifiedStart completes withtopTrue/cycleTrue/allcommandsOff. r4handlossarrestsfeed; restoringhandalone keepsmidstroke/alloutputsOff. r5freshStartmidstroke staysinactive; explicitAppReset thenhands+freshStart completes. This is the QA ladder policy, not certifiedsafety or a claim of an exposedretractbutton.

Elevator V2 QA-onlydwell: originalV1 preserved; adds1secondholdafteractualupperfeedback beforedescent. small-elevator-r1-upper-6.png directlyshows upperTrue/position99.9999/upFalse/downFalse andactualcaratupper. Subsequentnativeframesdescendtolow. Small-fontOCRmisseswereQAreaderfailures, notsensorfailures. R3before-lossSignals showsDownTrue/upFalse/car81.9999. PhysicaldoorsinputOff thenloss/settled-heldSignals showposition56.6666/bothcommandsFalse/doorsFalse acrossadvancingruntime; r4restore returnsactualbottom at_lowerTrue/updownFalse/allthreepermsTrue. No all-phase permissivecrossproduct or industrialelevator safety claim.

Remainingknownissues: Parceloveradmission/count2stall andstaleRUNBLOCKEDfooter remainopen; full77sceneaudit notcomplete. NextspecificP1gaps: fullten-zoneARRAYlesson, mappedgalleryCNC/conveyoranimations andremainingdemo controls. Newpostbackuprequirementsreportreconcilesboundedclosureswithoutrewritingbacked-uphistory.
