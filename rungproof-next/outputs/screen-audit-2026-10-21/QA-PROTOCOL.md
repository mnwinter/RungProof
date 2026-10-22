# RungProof screen playtest protocol

Status: real screen execution is underway through the user-authorized, app-pinned OS-input/pixel bridge. All 77 scene identities have completed the bounded entry/Run-attempt/combined Stop+Reset breadth capture and independent review. Five curriculum procedures visibly reached 5/5; deeper learner-fixture cases continue. Every scene remains overall PARTIAL. Consult coverage-ledger.json and provisional-issues.md for current scoped evidence and issue dispositions.

## Scope and execution order

Use route-reconciliation.md and coverage-ledger.json as the reconciled manifest: 77 unique scene identities, grouped as five demo aliases, six USER CREATED entries and 66 LABS entries. The five demo-associated IDs are included in those 77, not five additional standalone variants. The Scenario menu, Scenario Browser and Viewer each route those IDs through the same scene-selection path. Track existing demo case evidence once and test alternate navigation surfaces separately; do not impose hypothetical unexposed runtime variants.

Execute the five curriculum demos in lesson order, then every remaining lab in numeric chapter/lesson order, then other scenes. One agent owns UI input at a time. Independent agents may review captured evidence and coverage without changing application state. Do not repair defects during discovery or create a remediation plan before the complete audit.

## Evidence contract

- Use visible app controls for all behavioral steps. No injected state, backend behavior scripts, synthetic rendered walkthroughs, or headless tests as substitutes.
- Record build identity as shown by the app, local revision and dirty-worktree context, window size, entry route, loaded project, initial state, and evidence timestamps. A commit alone does not identify a dirty build.
- Establish expected behavior from visible instructions and governing lesson documentation. Record unclear expectations as specification gaps, not invented failures.
- Maintain separate PASS, PARTIAL, FAIL, BLOCKED, NOT TESTED, and justified N/A cells for each test case. A scoped PASS covers only its written procedure; loading successfully is not a scene-wide pass.
- Capture before/action/after evidence for failures, including visible command, output, feedback, and completion state where available. A still image alone cannot establish responsiveness or timing.
- Use only offline simulation. Do not initiate external PLC transport.

### Pixel review and learner test inputs

- All behavioral inputs use normal OS mouse/keyboard actions. Saved PNGs are actual native-window pixels; visible Signals/Watch values count as screen evidence. OCR of those pixels is an observation aid, not simulator-state access.
- Use contact sheets made solely from saved actual captures to review identity/render breadth efficiently. Label each thumbnail with its source scene ID, retain full originals, and inspect full-size frames where wording, geometry, state or a suspected anomaly is ambiguous. A compressed overview cannot prove fine layout or control responsiveness.
- When small-label OCR is uncertain, inspect the original and retry a crop/enlargement of the same pixels. Preserve both. An OCR miss is not an application defect or evidence that a point is absent. Never infer a missing value from the expected result.
- Disposable `.rpproj.json` learner fixtures are declarative test inputs. Prepare them only from exact scene declarations and supported saved-project forms; do not compile or execute them behind the UI. Select the matching source scene first, then load through File > Open Ladder Agent Project using folder Path plus file leaf. Validate/load/run only through visible controls. A mismatched source is blocked, not automatically switched.
- A clean no-ladder-networks Run block in an unsolved exercise is a learner prerequisite, not a failed plant function. Author/load compatible logic through the UI before judging normal machine behavior. Clearly label reference-training choices versus original source requirements.
- Breadth Stop followed by Reset has only one final checkpoint, so it cannot independently prove the Stop transition. Deeper tests require separate before/action/after captures for each playback operation. Reset may restore declared nonzero/true initial values; do not assume it always zeroes outputs.

## Per-entry case matrix

| Case | Screen procedure and required observation |
|---|---|
| Entry | Open from the visible browser; confirm correct title, scene, lesson, project, and instructions; note missing or inaccessible entries. |
| Initial state | Observe before Run; verify documented initial indications, counters, actuators, and controls; note any prior-scene state leakage. |
| Controls | Exercise every exposed button, selector, setpoint, and interactive scene object; check labels, hit targets, momentary release, toggling, ranges, and visible response. |
| Normal flow | Perform the documented task through UI inputs; observe sequence order and feedback through completion. For an intentionally unsolved lab, test its documented starter/learner workflow; do not expect automatic completion. |
| Playback | Exercise app Run/Stop/Reset separately from machine Start/Stop/Reset when both exist; verify the documented distinction. |
| Interrupted flow | Stop at an intermediate stage; observe motion and state; restart through the documented route and verify consistent recovery. |
| Trip and permissives | Where exposed, operate each simulated trip and permissive during operation; observe inhibition, indication, recovery, and fresh-start requirements specified by the lesson. Never assume a teaching trip is safety-rated. |
| Reset | Reset idle, mid-cycle, after completion, and after a simulated fault where supported; verify documented counters, outputs, scene objects, and learner-progress behavior separately. |
| Repeatability | Complete a second full cycle after reset; test held/repeated input where meaningful; check duplicate counts, false edges, stale completion, and stuck outputs. |
| Navigation | Switch away and return, move between lessons, open lesson logic/help, and test return paths; observe state retention and wrong-scene/project associations. |
| Presentation | Check readability, clipping, occlusion, status meaning, camera access, and alignment between visible movement, indications, and lesson feedback. |

For timing checks record the displayed timer and observable transitions. Do not claim precise scan-time accuracy from UI timing alone. Use N/A with a reason for unavailable controls; use BLOCKED when a required path cannot be reached.

## Defect record

Each issue receives a stable QA identifier, affected scene/workflow IDs, case ID, title, severity, reproducibility count, initial conditions, numbered screen actions, expected result and source, actual result, evidence paths, and any workaround observed. Separate observed facts from suspected causes. Retry a failure once from a documented clean reset unless doing so risks losing unique evidence; record intermittent results.

Severity: S1 crash/data loss or audit-wide blocker; S2 required lesson/control flow unusable; S3 partial functional failure with a viable workaround; S4 presentation or usability defect. Track coverage impact separately from severity. The unavailable screen-control surface is an environment blocker, not an app defect.

Deduplicate confirmed identical symptoms using one master issue with every affected entry linked. Preserve scene-specific reproduction differences; do not merge merely similar failures on an assumed common cause.

## Completion and later planning gate

Every manifest entry must have a case-level disposition and evidence reference. Reconcile visible entries with the source manifest and account for additions, omissions, hidden entries, and demo overlaps. BLOCKED and NOT TESTED remain incomplete and cannot count toward completed function testing.

Only after screen coverage is complete and all issues are consolidated, prioritize repairs by severity, breadth, lesson dependencies, evidence-supported shared causes, effort, and regression risk. Define the smallest validating retest and affected-scene regression coverage for each proposed repair. This document defines the QA process only; no repair priorities have been selected.
