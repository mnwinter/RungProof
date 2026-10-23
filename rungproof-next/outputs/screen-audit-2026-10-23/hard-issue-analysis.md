# Hard-issue causal analysis: parcel and stale Run feedback

Status: independent source/model analysis, 2026-10-23 UTC. Application and historical fixtures unchanged. This is not a final all-scene remediation plan or native acceptance. Root owns all actual screen input. New candidates below are declarative QA ladder projects opened through the normal UI.

## What changed in our understanding

1. Actual V3/V4 ladder documents, executed by the actual compiler, I/O mapper and accepted-scan session against the actual parcel model, reproduce their distinct historical native failure signatures. A hand-written phase approximation is no longer the only causal evidence.
2. V3 has a demonstrated extra admission-transfer scan. V4 removes it, then over-admits the following small parcel. The final clearance/inhibit guard is preventing an unsafe index, so weakening that guard would hide the admission failure.
3. Existing declared drives can complete the batch with a bounded LAD-owned handoff policy. This is a diagnostic counterexample to an impossible-to-complete model, not proof that every documented metering/sensor detail is correct. One generic dwell fails; class-dependent dwell passes normal operation; ordinary TON dwell fails sufficiently late Stop/Run interruptions. A separate retentive V7 candidate passes those bounded model cases.
4. QA-UI-002 has a specific two-label publication gap. The existing recovery verifier checks the inner ladder label and misses the persistent outer WorkspaceStatus label. This explains its historical PASS alongside the native red footer.

## Evidence boundary and reproducibility

The canonical source commit inspected was d0c4566bc956706ab3430e25d4142b03f4431e9c. The parcel-model source SHA256 remains ad2b8f8699aeb66f3e5fa653ead0cf22ceb71721ff863dce2c12f5053e9f5d56. Original V3/V4 hashes were checked unchanged. New source/fixture hash closure is in hard-issue-analysis-provenance.json; exact fixture read/load/compile/binding/roundtrip checks are in diagnostic-fixture-validation.json.

This agent independently inspected the historical native originals small-parcel-v4-final-signals.png and small-diagnostics-error-list-settled.png. They confirm the prior review's visible count2/inhibited parcel state and the running/zero-error/red-footer contradiction. They are historical native evidence, not a new application run by this agent.

The isolated harness ParcelAcceptedScan.csproj explicitly links current src/VirtualController/*.cs and src/Scenes/ParcelTransferPlantModel.cs read-only. It loads saved editor projects through LadderEditorProjectJson, compiles them, maps actual symbolic input/output images, and uses VirtualControllerSession.Advance. It does not construct Godot, a scene runtime, real PLC transport, a native app, or a live connection. No hidden app state was accessed. Generated C# bin/obj live under the repository sibling _agent-hard-issue-analysis-20261023, outside rungproof-next; the reviewed source is .cs.txt to avoid the application compile glob.

Important omissions: no delivered Godot mesh-support callback, exact native world-AABB measurement, renderer, native timing/geometry observation, or app lifecycle callback is exercised. Stop model experiments explicitly mirror runtime Stop plus output commit and verify no accepted plant tick during the stopped hold. These do not replace a clicked Stop/restart test.

From rungproof-next, reproduce one group at a time:

    .tools/dotnet/dotnet.exe run --project outputs/screen-audit-2026-10-23/ParcelAcceptedScan.csproj -- <absolute-rungproof-next> <absolute-new-audit-dir> validate

Replace validate with baseline, scan-sweep, meter, class-meter, retentive-meter or late-stop. Historical V3/V4 projects are never rewritten. Re-execution writes only owned diagnostic outputs/candidates; frozen candidate hashes are asserted by validate. Do not rerun candidate generation while root is opening that same file.

## Parcel causal chain and ownership

- Saved project: historical fixture-new-binding-lab-2-23-v3.rpproj.json or v4. Each is schema10, 21 networks,20 variables and passes current scene binding validation. The lesson supplies no parcel reference LAD; these are QA-authored learner policies, not shipped reference programs (docs/help/scenes/lab-2-23-parcel-sorter.md:97).
- PC-owned: measured center/clear/aligned/rear-clear, size beams, inhibition, misroute and all-received. PLC-owned: conveyors_run, infeed_run, table_transfer_run, infeed_gate_open and route_position. SIM sorted_count/cycle_complete are observations, not PLC commands.
- Main.cs:1415-1454 applies SceneIoImageMapper. Main.cs:4897-4904 calls the accepted-scan session. VirtualControllerSession.cs:305-340 samples, executes ladder, commits commands, then advances plant one ScanPeriod. A render frame can execute several accepted scans; wall time and capture names do not change the20ms motion quantum.
- SceneSimulationRuntime.ParcelTransfer.cs:140-144 forwards those commands to the model;165-172 publishes feedback. Its delivered-support callback is95-120, omitted from the isolated model harness.
- Current V3/V4 snapshot command phase is intentionally separated from phase transitions by ladder network order. The runtime is executing those networks as authored; the evidence does not show a generic PLC scan-order bug.

### V3: premature route commitment before indexing

Exact accepted-scan witness in v3-20ms-authored.jsonl and parcel-causal-witnesses.json:

| Scan | Sample/commands and model result |
| --- | --- |
|575|Medium centerX=-0.018, Route=-1, center-loadedTRUE after plant step.|
|576|LAD samples loadedTRUE but snapshot phase30 still commands transferTRUE. qa_step becomes40. Plant commits medium Route0 at unchangedX=-0.018; TableLoaded becomesFALSE.|
|577|LAD now commands route0.5 and transferFALSE. TableLoadedFALSE and TableClearFALSE prohibit indexing, so TablePosition stays0 and alignmentFALSE.|
|5000|Count1, phase40, class2, loadedFALSE/clearFALSE/alignedFALSE, no completion.|

Source boundaries: ParcelTransferPlantModel.cs:30-32 defines loaded/clear;76 permits indexing only with transferOFF and loadedORclear;113-119 commits a route and returns before physical movement. V4's extra NC primary_table_loaded in the phase20/30 transfer branch removes precisely that commanded extra admission pulse. V4 then rotates and delivers the medium in the same actual-source harness.

Interpretation: a V3 fixture command-order defect is directly demonstrated. A broader measured-feedback contract concern remains: the help calls primary_table_loaded a25mm geometric center sensor, but the model excludes Route>=0 atline30. At scan576 loaded changesFALSE while the carton position remains inside that25mm center radius. If the intended signal means physical center presence, that internal-route filter violates the documented meaning. If it means uncommitted load eligible to index, the help/name must make that distinction. Simply removing the filter is not a coherent correction: rotation at82 and outbound motion at98 also branch on Route and could then disagree about which mechanism carries the carton. Resolve the complete occupancy/transfer-ownership contract before editing one predicate.

### V4: following parcel crosses the bar before close can act

At20ms and0.9m/s a commanded drive advances18mm per accepted scan. The contact queue compresses to20mm spacing (model128); the visible bar/rear-clear allowance is15mm (71-74,131-132). A sampled rear-clear event changes the phase after the current command networks; the next phase's close command is later. The trailing parcel can already overlap the bar plane by then.

Exact actual-LAD witness:

- Scan453: medium rear=-2.682; small front=-2.702; gate commandTRUE.
- Scan454: qa_step changes30, snapshot still20, gate commandTRUE. Medium rear=-2.664; small front=-2.684, already16mm beyond the gate planeX=-2.7.
- Scan455: commandFALSE, but overlap is real. Effective GateOpen staysTRUE and InhibitedTRUE. This prevents putting the bar through the carton; it does not independently stop the infeed belt.
- V4 keeps infeed running until the medium reaches center. Small reachesX about-0.788, Route=-1, inside the primary deck. Medium routes correctly and arrives at its receiver.
- Final phase50 waits for primary clear. The unclassified small parcel makes clearFALSE. Table target remains0.5; moving that still-incoming parcel along straightX is inhibited by121-123. Count2 persists. The pure harness reproduces this without support rejection, so a support callback is not necessary to cause this failure.

A sufficient conservative gap/reaction condition in this simplified chain is gap greater than15mm plus two per-scan travels. For the fixed20mm contact gap, that bound is exceeded at the20ms authored scan. This is a dimensional argument, not a calibrated throughput specification or an exact universal threshold. At fixed initial phasing, tested1/2/2.5/3ms cases complete;4/5/10/20/40ms fail. Some discrete phasings can pass outside a sufficient bound. Changing scan rate is therefore a cause probe, not a recommended repair or performance workaround.

### Perturbations and contradictions

| Hypothesis | Evidence / conclusion |
| --- | --- |
|Floating-point touching reopens a never-opened gate|Earlier closed-only actual-source experiment ran4000ticks without reopening. Authored doubles, float32 dimensions and world-AABB arithmetic variants reproduce V4 failure. Finite16mm crossing is not epsilon.|
|V3 and V4 are the same final cause|Contradicted: V3 stalls phase40 before rotation; V4 correctly rotates/delivers medium and stalls phase50 on a different load.|
|Godot support geometry alone causes count2|Not required for this failure: actual-source no-support-callback model also stalls count2 with matching phase/feedback. Native delivered support could still add separate restrictions.|
|The LAD omits return-home logic|Contradicted: phase50 clear→60 and home-aligned60→10 exist. They wait behind legitimate clearFALSE.|
|Wider initial queue spacing fixes metering|Counterfactual50/100/200mm initial gaps still fail. While waiting against the closed gate, the queue recompresses to the hardcoded20mm contact separation. Maintaining a controlled gap is different from changing spawn positions.|
|A generic700ms handoff solves admission|Contradicted: it protects table clearance but advances the follower beyond the size beam. Count2/phase10 then stalls without the next current class indication. Tested600/700/800/1000ms generic dwell variants all fail.|
|Any Stop after class-dependent timing succeeds|Contradicted for ordinary TON: sufficiently late phase30 Stop resets its elapsed/done state; Run repeats feed time and loses the follower indication. An early60ms Stop happens to pass, so testing one early interruption would have missed the defect.|
|Model fundamentally cannot complete the three routes with existing I/O|Contradicted by the bounded V6/V7 LAD-owned policy experiments. This does not prove the visible escapement alone fulfills every help claim.|
|Lower render FPS or longer wall-time wait resolves this steady stall|No. The fixed accepted20ms scan drives travel and the final state has no enabling transition. More unchanged scans cannot restore clear/center eligibility.|

## Diagnostic candidates and lifecycle reasoning

V5 generic700ms is a preserved failed experiment. V6 uses class-dependent300/500/700ms ordinary TONs after rear-clear. The varying dwell respects the parcel lengths: the current carton must reach the primary drive, while the following carton must still intersect the upstream size beam for the next admission. One generic time did not meet both constraints.

V6 completes3 with no misroute at10/20/40ms scan and at95/105percent of authored speed. This modest sensitivity check does not establish arbitrary-speed support. Model normal completion occurred at24.94 accepted seconds at20ms before the additional feedback-consumption scan. Its sufficiently late phase30 Stop/Run cases fail. VirtualControllerRuntime.cs:257-280 explicitly resets ordinary timer ET/Q at Stop while retaining sequence memory, so restarting a travel allowance causes overfeed.

V7 preserves elapsed allowance across Stop using three RetentiveOnDelay timers plus three explicit LAD reset networks outside each active class/phase. The plant still integrates commands only; no hidden sequencer or physical state override is added. V7 has27 networks/23 variables, schema10, existing declared scene I/O unchanged, and no hardware addresses. Current parser/compiler/SceneIoBindingValidator and editor-program roundtrip all pass.

Frozen V7 file: fixture-diagnostic-parcel223-v7-retentive-class-metered.rpproj.json

SHA256: 36de084f73c356c761440e21609b44dbb71a339573837349dc8422212c63fd6a

Bounded actual-source results:

- Normal:3correct arrivals, phase0, all BOOL outputsOFF, numeric route0 at1248accepted20ms scans (24.96s). Physical completion is consumed by the subsequent LAD scan; do not stop observing at the first count3 frame.
-13 independent interruption points: phase20/classes1-3, phase30/classes1-3, phase40/classes2-3, phase50/classes1-3, phase60/classes2-3. Each actual Session.Stop removes outputs, a5s stopped advance accepts0scans and leaves pose unchanged, then Run completes3/phase0/outputsOFF.
- Four further medium phase30 interruptions after15/25/50/90 active phase scans complete identically. The corresponding V6 cases all stop at count2/phase10. V7 retains the measured300/500ms timer allowance across Stop; V6 loses it.
- Original V3/V4 and all scene/application sources remain untouched. No native acceptance is credited from these model results. Retentive state resets at phase exit and explicit AppReset; this is the diagnostic fixture's restart policy, not a safety-certified or Siemens-parity claim.

Detailed outputs: parcel-accepted-scan-baseline-summary.json, parcel-accepted-scan-scan-sweep-summary.json, parcel-accepted-scan-meter-summary.json, parcel-accepted-scan-class-meter-summary.json, parcel-accepted-scan-retentive-meter-summary.json, parcel-accepted-scan-late-stop-summary.json. Per-scan traces exist for the baseline V3/V4 and normal V6/V7. The first unversioned parcel-accepted-scan-summary.json predates a corrected follower metric; use the named baseline file and causal witnesses for current conclusions.

### Smallest useful native witness

Root should open the frozen V7 through normal File Open after selecting Lab2.23, explicitly Reset, AppRun and physically Start. Observe actual three distinct receivers, sorted_count3, all_parcels_receivedTRUE, cycle_completeTRUE, misrouteFALSE, then ladder commandsOFF on a subsequent settled scan. This discriminates an inadequate V4 admission policy from an inherently blocked current model without changing the app.

The hard lifecycle witness is medium/class2 phase30 after infeed_run becomesFALSE while primary_table_loaded is stillFALSE and table_transfer_runTRUE. The following small_detected indication should remainTRUE. Click AppStop; require actual mid-move pose hold and all commandsOFF. Then click Run: V7 should retain its expired handoff allowance, keep infeedOFF while the medium centers, then complete all3. Keep private positions/model values out of the native result; cite only visible Signals/Watch/LAD/geometry and actual input logs. Repeat from Reset and verify the initial queue/count/phase. V6 is an optional negative-control fixture; no need to manufacture a new app issue from its known diagnostic-policy failure.

Residual contract question: the single bar can remain mechanically open against an already admitted follower even when commandFALSE. V7 meters that follower with the explicit infeed drive. A native V7 pass would establish viable commanded flow, not prove that the escapement independently isolates one carton, calibrated friction, arbitrary lengths/speed, or all recovery phases. The observed V3/V4 failures remain historical failures. Future issue taxonomy should distinguish QA program policy from any demonstrated plant-feedback contract defect.

## QA-UI-002: two independent status surfaces

Historical native small-diagnostics-error-list-settled.png shows local runtimeRUNNING/scan1343, ladder monitorRUNNING/scan1338, ErrorList0ERRORS/verificationpassed, and outer Scene I/O redRUNBLOCKED/current draft invalid. This is an S3 feedback defect; the screenshot does not show an execution failure.

Source causal map:

    invalid Run -> Main.RunEditorDraftController validation fails
      -> SetVirtualControllerValidation(issues)
      -> SetWorkspaceStatus(RUN BLOCKED, isError:true) -> outer _workspaceStatus
    visibly author a valid current draft -> Run validation succeeds
      -> load/match exact draft -> PublishVerifiedDraftState()
      -> validation refreshers cleared + inner _ladderWorkspaceStatus updated
      -> runtime.Run -> snapshot refresh updates inner monitor/runtime labels
      -> outer _workspaceStatus still contains the earlier red failure

Exact locations:

- Main.cs:632 binds RunRequested to RunEditorDraftController.
- Main.cs:1591-1596 writes the failed validation list and outer RUNBLOCKED.
- Main.cs:1606-1611 loads/matches the current executable draft, publishes verified state, then runs.
- SimulatorShell.cs:21113-21121 checks exact loaded-draft identity, clears validation refreshers and writes only the inner ladder workspace label.
- SimulatorShell.cs:21162-21190 refreshes inner monitor, controller and cycle labels; it does not clear the outer footer.
- SimulatorShell.cs:3592-3599 creates named outer WorkspaceStatus;3658 adds it to DiagnosticsHeader/SceneIO strip;23893-23898 sets its text/color and records an operator event.
- SimulatorShell.cs:21380-21398 derives exact monitor/draft identity excluding WatchVariables. Keep that guard: an old running controller must not make a changed invalid draft look valid.

Verifier blind spot: SimulatorShell.AuditDiagnosticsReview.cs:29-35 explicitly seeds SetWorkspaceStatus(RUNBLOCKED), but its cleared predicate checks ValidationSummary and _ladderWorkspaceStatus. It never checks _workspaceStatus, the label it seeded. The same issue is not caught by the broader editor verifier's validationCleared predicate at SimulatorShell.cs:22052-22057, which checks issue count, inner summary and tab only. A green automated recovery result therefore did not cover the actual user-facing defect.

### Competing explanations

| Explanation | Assessment |
| --- | --- |
|Current authored ladder remains invalid|Contradicted in native scope by0ERRORS, successful monitor/run and actual loaded identity path. Real invalid drafts must still retain their errors.|
|Controller is blocked despite green monitor|Contradicted by advancing native scans; remaining defect is contradictory feedback.|
|A deferred compile update will eventually clear the footer|The relevant success paths never write that label; settled native scans already advance far beyond the action.|
|A missed event subscription prevents all validation UI updates|Inner ErrorList and monitor already update. The source has a narrower publication omission.|
|The verifier proved both labels correct|Contradicted by its exact predicate: it inspects a different label from the seeded outer error.|

### Narrow future correction boundary, not an implementation

Any later change should publish successful local Run status to the outer surface only after current-draft validation/match and actual local Run acceptance. Do not clear it globally on every snapshot: an older running program, simulated E-stop, drive readiness failure, external readiness or a newly invalid draft can coexist with snapshots. PublishVerifiedDraftState is also used by Verify/Load-style routes, so setting RUNNING inside it before RunActiveController readiness gates would invent success. Main.cs:1598-1601 and1642-1645 retain the explicit simulated E-stop gate;1630-1639 is a separate external path.

A small status-owner/category token would make stale validation replacement less brittle than substring matching. A narrower local Run success update may suffice once all readiness branches are traced. This report selects neither architecture nor a broad refactor; it identifies where the existing claim and update diverge.

Future discriminating screen retest: blank current draftRun→realEDIT001/red outer footer; visibly add valid Network+NO+coil→Run; wait settled scans and require0ERRORS plus a noncontradictory outer footer. Then invalidate the current draft while the older program exists and Run again; require actual validation errors and blocked status retained. Separately keep the explicit simulated E-stop/readiness rejection visible. A valid fixture loaded directly skips the original error-to-authoring transition and does not prove this fix.

Persistence/ownership implications: this footer correction concerns transient UI status and event history, not LAD schema, scan memory, saved runtime state or point ownership. It should not modify a learner's document or award a curriculum PASS. V7's new timers are additive private learner-memory declarations persisted only in its versioned QA project, using the existing schema. Restoring old V3/V4 saved projects must preserve their original authored behavior; do not silently rewrite their logic or relabel them as accepted.

## Stop boundary and handoff

No application repair or final all-scene remediation plan is produced here. Root retains the full77-scene discovery obligation and owns native tests. This analysis leaves source-based uncertainty smaller and supplies one explicit native discriminator. At04:00 America/Los_Angeles (2026-10-23T11:00:00Z), stop project activity and follow root's backup/skill/pause work order. No worker from this analysis should remain running beyond that cutoff.
