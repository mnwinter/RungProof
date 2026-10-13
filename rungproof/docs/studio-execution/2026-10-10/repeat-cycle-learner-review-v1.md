# Task 3 independent source/spec review v1

Status: CHANGES_REQUESTED for bounded QA coverage/evidence qualification. No newly demonstrated product defect. No native GREEN or rendered acceptance claimed.

Read task-3-brief.md, task-3-report.md, and authoritative rungproof/docs/studio-execution/2026-10-10/repeat-cycle-studio-implementation-plan.md. Reviewed new Main.RepeatCycleLearnerReview.cs and Main.RepeatCycleLearnerCapture.cs; compared Main.cs and tools/capture_demo_nonfocus.ps1 against the exact before/task3-* baselines, not HEAD. Supporting source inspected for fixture construction, scan snapshot diagnostics, copied signal images, observer callback generation, and reducer retirement. No build, engine, capture, or product edit performed.

## Findings

1. P2 — Complete lifecycle cases at all three required periods (Main.RepeatCycleLearnerReview.cs:160-175). The accepted behavior matrix and separate retirement helper cover 20/40/100 ms, but unsupported Pause/Step, partial Stop/resume historical continuity, read-only history selection, invalid-midfeed freeze, and exact saved-invalid reopen are only executed with hardcoded 20 ms. The Task 3 native brief specifies the three-period protocol. A 40/100 ms regression in those paths can therefore leave this verifier green. Move the lifecycle sequence into a 20/40/100 loop, give assertions period-qualified names, and preserve actual Run/Open/joint-save/reopen operations. The ordinary-frame watchdog may remain a separately labeled bounded sample, unless broader frame coverage is claimed.

2. P2 — Qualify the actual requested capture dimensions and correct the signature claim (tools/capture_demo_nonfocus.ps1:256-260; task-3-report.md Root commands). The report claims PNG signature/dimension validation, but the wrapper only checks >=24 bytes and width>=1200,height>=675. A 1600x900 image can pass a requested 1280x720 bounded capture, defeating the explicit second-width gate, and arbitrary bytes are not checked for PNG/IHDR identity. For repeat captures, require the PNG signature plus IHDR header and exact dimensions parsed from Viewport before emitting PASS; correct report wording if any check remains unimplemented. This minimum-size implementation predates Task 3, but its new Task 3 evidence claim and required bounded viewport gate rely on it. No malformed or wrong-sized actual capture is alleged.

## Source observations that support provisional approval

- Explicit diagnostic flag and capture-state hooks are narrow and inactive for ordinary learner operation. Factory startup remains separate from diagnostic reference construction. No hidden reference injection was found in the scoped hooks.
- Fixtures use actual disk Open, joint SaveWorkspaceToPath, navigate-away, LoadWorkspaceFromPath, exact draft/layout comparison, then RunEditorDraftController. Invalid fixtures follow the same disk workflow. Alias mode renames all tags, including internal counter references through the editor API.
- Intended, request-count, preset-two, held-active cases are meaningful perturbations of the diagnostic fixture. Independent return edge totals expose repeated wrong-request motion, two-return completion, and held Done. Expected real COUNT text and indicator material values are asserted.
- Trace comparisons cover every combination of observer on/off and history on/off at 20/40/100 ms, with identical actions. They include full session snapshots (including diagnostics/counters), sampled/effective/applied/post inputs, independently captured pre/post plant values, points, root pose/visibility, existing structural native-pose normalization, labels, and lens semantics. Serialization occurs within the accepted callback, so later mutation of live points does not rewrite saved rows.
- Generation-retirement check is meaningful for the current integration: invoking a stale callback without its guard would enter ObserveRepeatCycleScan with a missing pair and replace the retired detail, which the exact view comparison detects. Force removal does not restart assessment. Draft retirement explicitly checks no output/snapshot assignment. Reset checks physical home/spindle, readout/lamps, clock and history.
- Capture explicitly holds the diagnostic clock, never substitutes learner Pause, preserves current draft metadata, compares actual DLL hashes, requires ready/PNG save markers, rejects existing primary PNG/identity metadata, and states source-to-build qualification and visual review separately. Hint and history modes use existing shell review hooks. No capture has been inspected in this review.

## Preserved evidence and outstanding gates

The preserved native-v3-red log was read at its terminal: REPEAT_CYCLE_LEARNER_VERIFY FAIL failures=9. Root reports 239 True/9 False, DLL 9C0609BB9F7298DA1FF936DC2EF95A11473DDB5424BBC00C44616CDD024BF40F, build zero warnings/errors. The six assessed/elapsed wording failures and three authored physical assembly layout-retirement failures belong to already assigned integration/UI fixes and are not duplicated as new findings here.

The root shell wrapper's trailing Get-Content produced the observed shell exit 0; it did not preserve Godot's exit. Verifier GetTree().Quit(failures==0?0:1) is correct in source. The diagnostic FAIL remains authoritative; a fresh run must retain LASTEXITCODE immediately. No exit-code product defect inferred.

Pending: product fixes, stronger card semantic assertions requested by root, the two QA qualification findings above, fresh native proof with exact DLL identity and preserved exit code, actual 1600 and bounded 1280 scene/split/card/hint/history/invalid image inspection. Wrong-case renders also require explicit evidence/provenance review. Whole-product 77/material 75 HELD status stays separate; this review grants no blanket lesson, visual, physical or release approval.

## Reviewed source SHA256

- Main.RepeatCycleLearnerReview.cs: EF7A72036062F50CB6CEC7723A1C19B7AC03298218D7F16912186665B80C8E31
- Main.RepeatCycleLearnerCapture.cs: 42C67705FF9E26D9D38F1A7E9B3C3DCB025272E712B4FFAB0B8E3CA2C423A53D
- Main.cs: 659F96670C4A870736E85C6B2D9B1CA5BC59BB361B504CCE1669557EE5B9E966
- tools/capture_demo_nonfocus.ps1: F6C364DDD0A10FE37AB84AB82D0A3740734E2649F1607DB7748C72C17DAABDAD
