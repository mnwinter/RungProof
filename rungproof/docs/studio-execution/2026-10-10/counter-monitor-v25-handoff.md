# Approved counter monitor correction — V25
User approved explicit preset/current/input/done labels in both editor tabs. Shared LadderEditorCanvas now draws accepted snapshot counters; existing matching-program gate suppresses stale state. Wider block, conductor gap, hit rectangle and selection rectangle updated. Runtime counter semantics unchanged.

Red test: counter-monitor-red.log failed specifically because monitoring presentation was absent. Green: counter-monitor-exported.log terminal exit0,53checks,zero ERROR; source unit suite164passed. Actual exported counter-0-2.png and counter-1-3.png individually inspected under .tools/studio-verification/counter-monitor-exported. At1600x900 collapse existing Scene I/O panel; no claim the larger block is fully visible without scrolling in every dock configuration.

First export rejected source freeze because Godot generated Main.CounterMonitorReview.cs.uid during import. Retained unqualified. Second export passed freeze and all required package suites.
Candidate: C:\Users\mattn\Documents\Codex\Worktrees\rungproof-studio-20261010\rungproof\rungproof-next\artifacts\internal-test\counter-monitor-v25-20261011-b
Final manifest SHA256: CE827044A589257BEB57E52A4D54B96EF733F1FEB73958B9B1933300117A36B6
Desktop shortcut readback verified: C:\Users\mattn\OneDrive\Desktop\RungProof Test.lnk
Package inventory236files PASS. Existing candidate preserved. No reset/commit/push/livePLC. Handle remainsOPEN; wider ladder quality review remains outside this fix.
