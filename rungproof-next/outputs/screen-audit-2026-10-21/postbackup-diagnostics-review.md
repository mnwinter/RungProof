# QA-UI-002 B5 exact native retest — partial fix, remains open

New untracked report only. Original issue ID retained; no source/tracked-report/Git edits and no duplicate defect.

Independent actual `small-diagnostics-empty-run.png` shows fresh Lab2.1 blank ladder correctly rejects Run: no controller loaded/STOPPED, EDIT001 no ladder networks, red RUN BLOCKED footer. Root then physically adds Network and NO contact→material_call_on coil; no generated program/backend state edits.

Actual settled `small-diagnostics-error-list-settled.png` shows online energized rail, MONITOR RUNNING around scan1338, top LOCAL RUNTIME RUNNING scan1343, Error List0 ERRORS—verification passed, and bottom Now Playing RUNNING. Nevertheless the Scene I/O strip still displays red RUN BLOCKED—current ladder draft is invalid. This persists after multiple advancing scans, so it is a demonstrated stale footer, not a transient compile transition.

Expected: after successful verification/load/Run of current valid draft, all current-draft status surfaces reflect that verified state. Actual: editor diagnostics and runtime presentation are repaired, but outer footer retains previous invalid-draft conclusion. QA-UI-002 is partially improved, NOT closed. Runtime execution is not blocked in this reproduction; misleading status is the remaining S3 defect.

Possible source boundary: outer Main footer label not refreshed when SimulatorShell clears verified diagnostics. That is a source hypothesis, not a proven causing callback; no change was applied. Current native evidence is sufficient to keep the original issue open without reopening fixed Error List behavior.
