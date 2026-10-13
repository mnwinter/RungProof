# Repeat-cycle small-card framing review v8

Reviewed 2026-10-11. Verdict: APPROVED, no actionable source findings.

Compared SimulatorShell.LessonObservedTrial.cs with the exact task2-small-card-framing-v7.cs before copy. The sole difference changes the no-hint diagnostic scroll target from the entire VBox card to its first Control child.

BuildRepeatCycleObservedTrialCard adds SectionLabel("REPEAT CYCLE · OBSERVED TRIAL") first. SectionLabel returns Label, a Control, so GetChild<Control>(0) identifies the actual heading. Remaining result/count/provenance labels follow it. Scrolling to the heading avoids asking EnsureControlVisible to accommodate the oversized whole card and thereby placing its bottom above the small viewport's heading.

The visible-hint branch retains exactly the same hint target. This does not change the normal hint button, card layout, observer status/count/provenance, or plant/controller behavior. The only caller found is Main.RepeatCycleLearnerCapture.cs in the diagnostic capture path; normal user flow has no new automatic scrolling.

This is source approval only. Actual final-binary 1280x720 no-hint and visible-hint captures must confirm the respective heading/hint is in frame; oversized cards still legitimately require ordinary user scrolling to see all content. No engine, product edits, primary-screen use or input interaction occurred during review.
