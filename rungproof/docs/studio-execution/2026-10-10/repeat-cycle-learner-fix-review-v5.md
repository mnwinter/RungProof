# Task 3 fix round 1 source re-review

Status: SOURCE_APPROVED_WITH_RUNTIME_AND_RENDER_GATES_PENDING. Both v1 P2 findings are addressed in source. No new blocking source finding in this scoped re-review.

Reviewed current Main.RepeatCycleLearnerReview.cs, relevant capture-tool validation and task-3-report.md v4 addendum against task-3-review-v1.md. Main.RepeatCycleLearnerCapture.cs and Main.cs hashes remain identical to the prior reviewed revision. No build, native execution, capture, or product edits performed.

## Finding resolution

- Lifecycle period coverage: resolved. The complete unsupported Pause/Step, Stop/resume historical continuity, history-selection read-only, invalid-midfeed freeze, exact saved-invalid reopen, Reset and ordinary real-frame sample now execute within foreach 20/40/100. Fixture creation and invalid draft construction use ms, and RepeatLearnerTick uses the actual session period. Real-frame physics is disabled again before the next fixture, preserving deterministic setup. The actual disk Open/joint-save/navigate/reopen/Run workflow is retained.
- PNG/requested viewport qualification: resolved. Capture checks all eight signature bytes, the first chunk's big-endian length 13 and IHDR marker, then exact decoded width/height against the validated Viewport argument before PASS. Both supported widths therefore have a real equality gate. This is signature/header/dimension checking, not a claim to decode the entire PNG or establish visual quality.

The lifecycle assertion names still omit the period suffix recommended in v1, producing repeated names in the native log. This is a nonblocking diagnostic readability limitation: the source executes all three periods, and fixture/source context plus ordered log occurrence can distinguish them. Future edits can append ms without changing behavior; no rebuild is requested solely for naming during the frozen run.

## Added assertion review

RepeatLearnerCardBoundary verifies assessed/elapsed wording and mutually excludes the wrong frozen/updating assessment phrase. Wrong cases require frozen accepted assessment; intended current Observed requires updating accepted assessment. Stop additionally checks frozen presentation. The factory-null case requires unavailable returns/count/elapsed, so an invented zero cannot satisfy that assertion. These checks improve semantic coverage without setting presentation state, outputs, counters, or observation results themselves.

The observer/history invariance matrix and actual runtime readout/lamp assertions are unchanged. No newly introduced factory fixture path, hidden learner solution, observer output mutation, clock worker, learner Pause substitute, or wider scene hook was found in the scoped additions.

## Evidence boundaries

Root reports v4 build zero warnings/errors and DLL 229D7BAC4164F1026BD2744A345A7869D6493090F3C8708B9CC21BE58E5F4006; that build was not run by this reviewer. Native v4 was pending at assignment, with root preserving LASTEXITCODE immediately. The path name repeat-cycle-learner-native-v4-green.log is not itself evidence of PASS. This review does not assert native results or repair of the nine prior product failures; root must inspect terminal assertions and preserved process exit from the exact binary.

Actual rendered image inspection at 1600 and bounded 1280 remains required, including scene/split/card/hint/history/invalid and wrong-case provenance. Whole-product 77/material 75 HELD remains separate. No blanket lesson, visual, physical or release approval.

## Reviewed source SHA256

- Main.RepeatCycleLearnerReview.cs: 4794998468890CACA0E5A4B0059143C16229E025B33DB45FD33CAE4DCF7F0CC7
- Main.RepeatCycleLearnerCapture.cs: 42C67705FF9E26D9D38F1A7E9B3C3DCB025272E712B4FFAB0B8E3CA2C423A53D
- Main.cs: 659F96670C4A870736E85C6B2D9B1CA5BC59BB361B504CCE1669557EE5B9E966
- tools/capture_demo_nonfocus.ps1: 3DE007112478EAEBC10EC399CECF89B9C6DE3D4A99FC4C58E5577C6FBCDF61C5
