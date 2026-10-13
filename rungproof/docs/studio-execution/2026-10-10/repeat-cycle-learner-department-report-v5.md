# Task 3 learner native QA and capture report

Status: source prepared; root owns build/native/capture execution and preserved logs. No visual pass claimed. Fixture is private, explicit QA only and never factory initialization.

## Source-verified APIs
`OpenNativeMotionReviewProject`, `CaptureIntegratedLadderDraft`, actual `SaveWorkspaceToPath` / `LoadWorkspaceFromPath`, `RunEditorDraftController`, `VirtualControllerSession.Advance`, `HistoryRecordingEnabled`, `CaptureRepeatCyclePlantState`, `VerifyScanInspectionHistorySelection`, `ShowRepeatCycleHintForReview`, `ScrollRepeatCycleTrialForReview` were inspected in current source. Trace copies preplant immediately before shared `AdvanceAcceptedScenePlant`, afterplant immediately after, with independent post signal images; it never reads the observer's consumed pair. Existing `ScanInspectionNativePose` structural path normalization is reused.

## Root commands
From `rungproof/rungproof-next`:
```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-repeat-cycle-learner
& './tools/capture_demo_nonfocus.ps1' -SceneId lab-4-03-repeat-cycle-counter -CapturePath '<fresh absolute path>.png' -View split -RepeatLearnerState intended -Viewport 1600x900
```
Expected terminal learner marker `REPEAT_CYCLE_LEARNER_VERIFY PASS failures=0 evidence=...`. Intended Observed, request-count NeedsAttention, preset-two NeedsAttention with two genuine returns, held-active Incomplete after20 accepted seconds with one genuine return. Every observerON/OFF × historyON/OFF accepted trace at20/40/100ms must compare exactly. Wrong cases are expected failures of learner protocol, not failures of verifier. Wall watchdog distinguishes infrastructure failures from accepted20s deadline.

Repeat captures with scene/split and hint/history/invalid at1600x900; bounded1280x720 additionally. Existing PNG/identity refuses overwrite. Tool checks process exit, fresh PNG signature dimensions, actual ready marker, same DLL hash, held clock identity, source hash and PNG hash; inspection remains separate.

## Coverage and limits
Initial harness checks disk Open -> joint save -> navigateaway -> exact draft/layout reopen -> current Run; acceptedN/N+1 count lag; subsequenthold >=.5seconds; all-point/native pose/readout/material traces; historyindependence; unsupportedPauseStep; partialStop/resume historical evidence; invalidmidfeed no stale scan/pose; exactsavedinvalidblocked; measuredReset home; ordinaryframeRun watchdog. Deterministic manualaccepted ticking is explicitly separate from actualframe sample.

Initial source still needs fullsnapshot trace, direct expectedreadout/lamp assertions, alias/internalcounter rename, force/currentness/layout/stale retirement checks, comprehensivephysicalReset before claiming full frozenprotocol native coverage. Captures require hintbutton/scroll hooks and actualimage review by root/Dot.

## Concrete review concern
Terminal NeedsAttention view retains its last assessed actualreturn total while plant can continue. Earlyrequestcounter failure yields card Actual modeled returns0 while physical repeatedreturns continue. This is historical bounded assessment; unqualified currentcard wording might mislead. Root/Dot must review wrong-case image before deciding a narrow presentation change. No pure/integration/UI changes made by QA.

## Root v1 evidence and v2 assertions
Root reported v1 appbuild PASS0warnings0errors and native PASS187True/0False/noERROR/noException, exit0, exact DLL FF597490...4362, log `.tools/studio-verification/repeat-cycle-learner-native-v1.log`. This is the initial boundedcoverage result.

v2 adds full snapshot/root transform, allLENS semantic trace; exact COUNT newline F0 and pernativecolor lamps; wrongrequest >=4physicalreturns/count1/notcomplete; aliasesalltags + internalcounterrename; force retirement after removal; draftedit no outputassignment; retainedstale callback; Stopno acceptedclock/pose; Resetphysicalhome/spindle/readout/lamps/history; currentcard explicitassessed/elapsed and authoredmachine placement identity retirement RED assertions. These last two are expected to expose product provenance/currentness defects if missing; QA does not repair UI/integration outside its lease.

Capture modes additionally request-count/preset-two/held-active and run20acceptedseconds to reveal realwrongconsequences. Hintmode invokes existingbuttonhelper and allmode cardscrollhelper. No learnerPause substitution.

v3 frozen revision: fixed nullable metadata compilefailure; added standalone freshfactoryzero; explicit wrongcapture20acceptedseconds/status/readout/lamps and wrongtwo/heldDone checks; invalid current capture joint saved after editor invalidRun; metadata assessmentStatus string; tool expectedstatuses asserted and PowerShell parser PASS. Actual layout evidence prints authored machine_0 physicalassembly root/class/before-after localtransform; RepeatCycleHeadMotion is its child. Source `CreateRepeatCycleMachine` confirms machine_0 is controlledasset machine root, not the kinematic descendant.

Root reported v3 appbuild PASS0warnings0errors; functionalnative rootprocess started with preserved log `.tools/studio-verification/repeat-cycle-learner-native-v3-red.log`. Native terminal and renderedcapture inspection pending. Compile v2 CS8602 was infrastructure/sourcecompilefailure only, not functionalRED evidence.

## Preserved functional v3 RED
Root observed terminal `REPEAT_CYCLE_LEARNER_VERIFY FAIL failures=9`:239True/9False, noERROR/Exception, DLL9C0609BB...024BF40F, `.tools/studio-verification/repeat-cycle-learner-native-v3-red.log`. Six assessedelapsed card failures at20/40/100 historyON/OFF; three authoredphysicalassembly layoutretirement failures. These are concreteproductbehavior failures, independent of compilation.

Authoredlayout evidence changes whole machine_0 controlledasset root .1m inX, leaving child RepeatCycleHeadMotion identity intact. Native before/after transform is printed. Current identity layoutfingerprint only includes `_workspaceNodes` placements; default authored scene reports placements0, so physicalwholemachine movement remained undetected. Integration owner must decide minimal scene-layout identity coverage.

Root also reported engine shell exit0 despite emitted diagnosticFAIL. Verifier currently calls `GetTree().Quit(failures==0?0:1)`; source/pipeline investigation required before claiming diagnostic process exit reliability. Productfix owner now exclusively editing integration/UI; QA/toolsource remains frozen. Root later releasesQA for stricter meaning assertions (frozenfailed/historical versus continuingObserved; absentacceptedcount unavailable no fabricatedzero) and exit semantics investigation.

Correction from root: v3 unifiedshell exit0 came from trailing Get-Content, not Godot. Godot exitcode was not captured by wrapper. Verifier Quit(failures?1) is correct; do not change process code without demonstrateddefect. Root nextrun will save `$LASTEXITCODE` immediately after Godot and exit with same after logread. DiagnosticFAIL9 remains authoritative functionalRED.

## v4 final QA assertions prepared
SequentialQAlease after integrationfix2: meaningassertions enforce assessed+elapsed plus frozenacceptedassessment for failed/incomplete/historical, updatingacceptedassessment for currentObserved; nullacceptedview returns/count/elapsed unavailable instead offabricatedzero. Existing actual unsupportedPauseStep/partialStopresume/historyselect/invalidmidfeed/exactsavedinvalidreopen and ordinaryframeRun now explicitly at20/40/100ms. No Main.cs/productintegration/UI modifications byQA. Capturetool now checks real8bytePNGsignature and exactrequested1600x900/1280x720 dimensions; parserPASS. App/toolsource frozen forroot finalbuild/native.

Narrowreviewfix: capturetool additionally verifies firstchunk length13 and exactIHDR bytes12..15 before readingdimensions; length>=24/signature/requestedviewport guards remain. ParserPASS. Tool immediatelyrefrozen; appsource untouched.

## v4 actual functional GREEN, bounded
Readpreserved `.tools/studio-verification/repeat-cycle-learner-native-v4-green.log`:290True/0False, `REPEAT_CYCLE_LEARNER_VERIFY PASS failures=0`, `ROOT_GODOT_EXIT=0`, evidence `.tools/repeat-cycle-learner-4227f4525f954f2188d2dd08f8086101`. Root reports DLL229D7...F4006. v3RED9 -> v4GREEN290 verifies authoredlayout and assessed/frozen boundaries in actual nativeworkflow plus allperiodmatrix. No visual/capturepass claimed.

Root finalreview separately found validA->validBedit->Reset->Run pendingResetlost lifecyclecase. Integrationowner is adding actualRED and fixround3 under exclusivelease; v4GREEN excludes thatnewcase and finalassembly captures remainpending. QA/toolsource remains frozen.
