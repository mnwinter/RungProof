# Lab 2.18 independent selected-checkpoint review

Reviewed18selectednativeframes in three lab218-selected-review sheets, baseline, actualHealth and finalrepeatendpointpointcrop. Actualsupportedpallet, beamregion, eightSignals/runtime retained. Loadedfixture uses a two-second TPjog; this is a QAparameter, not universalplantbehavior. No reviewer UI/backend operations.

JOGinauto3635leavesposition0/runFalse. AutoSTARTmovesphysicalpallet27.0178/runTrue/green. Stop3820andheld3820freeze36.754/samepose/runFalse across1.2seconds. Run3834and3888retain36.754/runFalse withoutStart; freshSTART3913moves40.8918/runTrue, final6400pickupTrue/position100/completeTrue/runFalse andphysicalpalletatbeam. Resetreturns0; ManualMode85andAutoStart148leaveAutoFalse/runFalse/position0.

PhysicalJOGdown228startsrunTrue/position3.16425. Heldaftertimeout344isrunFalse/position24.097 whilemouseisstilldown; mouseup382retains24.097/off. FreshJOG407moves27.0178 thenended507holds48.1939/off; laterjog4ended784holds96.3879/off. Freshjogsprogresspallettoactualmanualcompletion32795position100/pickupTrue/completeTrue/runFalse/AutoFalse. This provesboundedoneincrementperheldpressunderTPfixture, not exacttwo-secondedge timing. Health951showsacceptedJOGevents, not an explicitpickupguardrejection; do notcredit a blockedpickupaction solely fromits filename.

ResetAuto25clearsposition/completion/run andrestoresAutoTrue. Independentlycroppedactualrepeatendpoint1792678012280465400showsAutoTrue/position100/pickupTrue/completeTrue/runFalse, closingfreshAutorepeat. Actualpalletstaysoverconveyor/supportthroughselectedpositions. No newissue. Pickupguardexplicitrejection, modeswitchwhilemoving, exactTPduration/pulsewidth, loadremoval andfaults remainunproven. No whole-scene PASS.
