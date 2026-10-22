# Post-fix screen retest queue and six remaining scenes

Fresh built app launch reported by root:PID31316, initially Lab2.15. Bridgev9 focus/DPI transport is being prepared; oldv8PID32944 is stale. This is not confirmation that v9 is live or app input is ready. SESSION-STATE transport fields will be updated only after root supplies live-v9 identity. No UI input or runtime tests performed to prepare this queue.

All seven fixes are implemented/awaiting root UI verification. Prior evidence is pre-fix; no historical case status is relabeled post-fixPASS. Use economical batches: one correct-scene/header+RUNNING baseline, actual control checkpoints and a final relevant feedback/recovery checkpoint; retain error frames, avoid full screenshot sweeps.

## Seven fix retests

| Order / issue | Minimal real-screen batch / acceptance | Boundary |
|---|---|---|
| 1 QA-VIS-002 (current fume scene) | Loadmatching2.15fixture throughnormalUI,Run. Selector0/1/2/3 withlightoff thenon: expectedSIMflags000/100/010/001 alongsidefan0/35/65/100 andphysicalstackdark/green/amber/red. AppStopclearsflags/lamps; Runrestoresappropriateflag; Resetalloff. | Actualspeedratios/fanmotionnotrequiredtoverifyindicatorfix. |
| 2 QA-FUNC-001 | For each of nine commands below, correctmatchingloadedcontrollerRUNNING; satisfydeclaredrequirements; clickactualphysicalcommand; checkHealthaccepted/nopriorbindingrejectionandnewPCrequestpath. Repeatfreshpressifpulsealreadysettled; migrateddraftshouldcontainnewInputbinding. | LegacydiagnosticprobesholdoutputsFalse: acceptancecanprovebindingfixonly, neverwholeprocessnormalflow. Capturefreshrequest/HealthroutewithoutinventingPLCstate. Learnerprocessmotionneedsrealcompatiblelogic. |
| 3 QA-FUNC-002 | Use11.7legitimatetransportfixture; establishactualpalletmoving/all3runsTrue; clickphysicalcommonstop; verifycommon_stop_requestTrue/allzonecommandsFalse/poseholdswhileRUNNING; clearstopandrecordfixturefresh-clearrestartpolicy. | AppPlaybackStopisadifferentcontrol; cannotclosephysicalrouteissue. |
| 4 QA-REF-001 | GUIopenrepairedactualshipped09-sum-counter-reference.rpproj.json, notunchangedoldstagedcopyorlegallearnerfixture. VerifyLoad/Run; qualify2+5/completeeventcount1,heldnoextra,freshcomplete2; Stop/Runretention,Reset0. | Sourcepreset32767/INT repair; fullcounterlimit/overflownotrequiredfororiginalloadblocker. |
| 5 QA-UI-002 | Startinvalidemptydraft/Runerror; visiblyauthorvalidcontact+coil,Runandsettleddiagnostics:0currenterrors/nofalseRUNBLOCKEDwhilemonitoring. ThenmakecurrentdraftinvalidandRun:realerrorretained. | Normalfixtureloaddoesnotreproducevisibleauthoringpath; exactdraftmatchguardmustnoterasegenuinechangedinvaliddraft. |
| 6 QA-DOC-002 | Viewrenderedhelp forDemo1/2/4 andLab2.7; confirmmissingpoints/actions/displays/equipmentnowlisted, NCactionmappingexplicit. | Sourcecomparisonalreadycomplete; remainingretestisrenderedcontent/reachabilityonly. |
| 7 QA-DOC-003 | ViewDemo4renderedprocedure:Run,machineSTART,qualifiers,oneNEWPALLETpulseperevent/release; comparevisiblecabinetlabels. | NoreturntostaleOFF/ONtogglewording; broadcurriculumrerunnotrequiredforwordingfixalone. |

### Exact nine new command bindings

| Scene / physical action | New BOOL PC request binding | Declared prerequisites |
|---|---|---|
| lab-2-11-inbound-tote-stop / start-cycle | start_cycle_request | none declared |
| lab-2-12-assembly-lift / raise-lift | raise_request | none declared |
| lab-2-12-assembly-lift / lower-lift | lower_request | none declared |
| lab-2-13-coolant-jug-fill / start-fill | start_fill_request | none declared |
| lab-2-16-safe-drill / start-drill | start_drill_request | {"left_hand_request":true,"right_hand_request":true,"workpiece_present":true,"drill_at_top":true} |
| lab-2-19-service-door / open-door | open_request | none declared |
| lab-2-19-service-door / close-door | close_request | none declared |
| lab-2-23-parcel-sorter / start-sort | start_sort_request | none declared |
| lab-2-24-robot-cnc / start-cnc | start_cnc_request | {"cnc_ready":true} |

Existing saved diagnostic probes were written before requestpointswereadded. NormalmatchingSceneIOmigrationnowaddsnewdeclaredPCinputs; inspectvisibleeditorbinding/tagvaluesbeforeRun. Do notrewriteoldfailureevidenceorcountdiagnosticprogramoutputFalseasprocessfailure.9.3stagedexistingreferenceisalsooldpre-fixhash; choosecurrentrepairedshippedfileandrecordidentity.

## Six remaining standalone normal routes

| Order | Scene / input | Minimum actual normal and lifecycle scope |
|---|---|---|
| 1 | conveyor-cell / fixture-conveyor-cell-normal | Correctnewapp/bridgeDPIidentity;normalFileOpen/VerifyLoad/Run;physicalstartviaoperator.start→conveyor/cartons/photoeyecycle;physicalstopoperator.stop;simE-stop/reset/freshstart;AppStop/Run/Resetseparately. PriornewDPI-invalidattemptsexcluded. |
| 2 | scene-1-conveyor-stop / fixture-scene-1-conveyor-stop-normal | PhysicalSTARTmovespackageuntilactualsimulated_photoeyeTrue;PLCconveyor_runningFalseholdsatsensor;Reset/freshrepeat. |
| 3 | tank-radar / fixture-tank-radar-normal | Physicalcycle/manualdrainrequestschangePLCfill/drain;actuallevelrises/falls,radardistanceinverse,signalmA=4+.16*level;inspectionghostview;Stop/Reset/repeat. Fixturetwo-modeplanttest,nothysteresisautomation. |
| 4 | equipment-gallery / nolearnerfixture | LocateactualSIMgallery_animationtoggle;visiblemotor/conveyor/fan/machinemotionstart/stopandrepeat;stop-allresponse. AppRunemptyladderguardseparatefromSIMroute. |
| 5 | tank-high-low / nosuppliedmanualroutefixture | Physicallyattemptadvertisedpump/draincontrolswithmatchingvalidcontrollerloaded;sourceactionsstilllackcommandbinding,recordexactaccepted/rejectedresponsebeforeanynewdefectclaim. Iflegitimaterouteexists,actual25/75switchcrossings/StopResetrepeat. |
| 6 | tank-level / nosuppliedmanualroutefixture | Sameadvertisedrouteacceptance/rejectioncheck;legitimatefill/drainactual20/80switchesand4–20mAtransmitter/StopResetrepeat. Nooutputinjectionorinventedbinding. |

Coverage source:coverage-ledger.json (77uniqueIDs,59reviewednon-demo normalpaths,5demo workflows,7binding-blockednormalpaths,6standalonepending). AlloverallPARTIAL; openindependentcasegapsremain. Fixsource/automatedevidence:remediation-evidence.md; userauthorizedplan:../../docs/SCREEN_AUDIT_REMEDIATION_PLAN.md. Noissuecloseduntilrootpost-fixUIevidenceisreviewed.
