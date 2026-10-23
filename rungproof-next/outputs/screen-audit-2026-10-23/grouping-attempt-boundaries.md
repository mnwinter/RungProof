# Grouping attempt boundaries

These are QA setup/timing outcomes, not application defects. Original images,
input plans and action logs remain intact.

- `grouping-select-r1-1792734198077020300`: the first exact-label helper saw
  Grouping at native y607, but a deferred popup scroll changed the row before
  selection. After the owned draft was discarded the actual title was Lab9.10.
  The fixture-open attempt in that wrong scene is not a grouping test. Root
  selected the settled visible Grouping row manually in r2; its actual title
  and imported `QA_grouping` block are captured before Run.
- `grouping-r2-controls-view-plan.json`: down was delivered, but the following
  move incorrectly included `button`. The bridge rejected the request and
  released held buttons. Only the corrected r2b down/move/up plan is credited.
- `grouping-r2b-release-candidate.png`: actual releaseTrue/count3/gate100 and
  completeFalse were observed, but the subsequent manual Stop landed after
  completion (transferTrue/gate0; scan11348). This does not close release Stop.
- r3 uses real AppReset/Run and physical Enable/Clear/three Load actions with
  independent count1/count2 observations. The frozen pixel observer waits for
  actual release feedback, rechecks it with the Playback menu open, and sends
  Stop once. Its immediate capture can show the previous RUNNING frame; use
  settled `stop-held.png` plus `grouping-r3-held-second.png` for the stopped hold.
  Recovery is recorded separately by `grouping-r3-recovery-plan.json` and uses
  only AppRun, with no new Load, Start, Reset or simulator state mutation.

The independent `grouping-release-stop-review.md` owns the resulting acceptance
scope. A helper result or this setup note alone is not a native PASS.
