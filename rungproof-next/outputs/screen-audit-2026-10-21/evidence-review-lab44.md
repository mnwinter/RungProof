# Lab4.4 independent native evidence review

Reviewed22 frames in lab44-cases.json, native Signals and physical four-color tower/completionpilot through three lab44-review sheets. Full start-red confirms Lab4.4. No reviewer UI/backend operations; explicitlyloadedreference scope.

Idle STEP leaves active/completion/colorsFalse. START produces activeTrue/redonly, unchanged after1second idle. STEP produces amberonly; START duringactive leavesamber, notredrestart. Next STEPgreenonly, thenblueonly, next STEPallcolordark/activeFalse/completionTrue with separategreencompletionpilot. ExtraSTEP retainscompleted. NewSTART resetscompletion andred; STEPamber repeats.

AppStop fromactiveamber clearsactive/completion/allcolors andphysicaltower/pilot, STOPPED2876. Run2903 staysclear; STEPwithoutStart2963 staysclear. FreshSTART3022 givesred. ActiveAppReset clearsall atSTOPPEDscan0; Run25 staysclear. FreshSTART86 theneachSTEP147/208/269/329 repeatsred/amber/green/blue/completedallcolordark+greenpilot.

Exactphysicaltowercolors agree with actual tower_red/amber/green/blue Signals throughout; completionpilot agrees with sequence_complete. Native panel additionally shows settled sequence_start/sequence_step_dueFalse; exactacceptedpulsewidth not observed and is not a defect. No newissue. Held/same-scan/simultaneouscommands, exactscanlatency andalternatelearnerlogic unproven; no whole-scenePASS.
