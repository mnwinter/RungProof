# Pause / scan peer review V1

Reviewer: complete_dispatch_art. Read-only technical source review; no engine launch, builds, implementation edits or captures by this reviewer. Reviewed Main.ScanInspection.cs, Main.cs lifecycle/action/physics hooks, VirtualControllerSession.cs, immutable scan records, SimulatorShell.ScanInspection.cs, SceneControlInteractor.cs, finite-workspace observers and pause-scan-implementation-plan.md.

## Disposition

PASS WITH EVIDENCE LIMITS for the inspected source contracts. The three previously reported integration defects are corrected. Native QA and actual visual acceptance remain separate; native-v1 was still running when reviewed and is not treated as a terminal pass.

## Corrections verified in source

- Main live _PhysicsProcess now supplies CaptureScanInspectionPlantInputs, matching Step's actual post-plant PC history image. PLC-owned points are excluded from measured feedback.
- Pause immediately disables decorative ambient motion; Main's per-frame ambient enable expression additionally excludes IsPaused. Composition root descendants, including Always processors, are recursively leased Disabled. Main-side interactor animation/latched-release callbacks also skip paused wall time.
- Monitor VirtualStart/VirtualStop now reject paused changed/invalid drafts and absent/non-BOOL input binding. Declared PC scene actions queue only session inputs, with no direct plant mutation. Other loading/moving actions are blocked while paused; EmergencyStop/Stop/Reset retain explicit existing lifecycle behavior.
- IntegratedDraftChanged publishes scan availability immediately. Unavailable reasons now distinguish unsupported scene, stopped/no loaded current controller, and current draft mismatch/invalidity.

## Lifecycle and observer findings

Unchanged user Run resumes the same controller session after restoring original process modes; watch-only metadata is excluded from execution identity. Changed valid user Run explicitly loads the current draft with resetScene:false. Invalid Run stops the old session and exposes diagnostics. Step rejects invalid/mismatched drafts without accepting a scan. Force set/remove/clear UI paths all reject while paused.

Run, Stop, Reset and Disable restore valid nondeleted nodes to their original ProcessMode. Scene replacement goes through Disable; paused layout edits through RecordUndo/ApplyWorkspaceSnapshot explicitly Stop, clear history and advance epoch before rebuild. Stop retains its established command-clearing/backlog/pulse semantics, unlike Pause.

Session Advance refuses paused wall time before accruing the accumulator. Step uses the same accepted-cycle pipeline, bypasses the accumulator and remains paused. Pipeline order is input sample/overlay, effective force image, PLC scan, output commits, actual plant period, pulse cleanup, immutable history, ScanCompleted and snapshot publication. Finite observers are attached to accepted scan events only; Pause itself does not issue Stop or zero roller coast speed. Manual Stop still retires unfinished observation through the existing observer lifecycle.

History dictionaries/text values and diagnostic lists own immutable primitive copies. Selection uses retained scan number and exposes HISTORICAL ACCEPTED SCAN without modifying live controller/plant. Ring capacity128 and dropped count are explicit; replaced/reset session history is cleared. No replay/rewind or inferred causal highlight is introduced.

## Evidence still required

Root reports build-v3 zero warnings/errors. pause-scan-native-v1.log was in progress at this review snapshot; no terminal RESULT was yet available. Inspected emitted named QA checks were true; this does not imply the remaining cases passed. The automated normal-Step/history parity fixtures call the shared session path directly, so the new live Main callback and sibling ventilation fix deserve explicit ordinary-physics checks as well as source review.

Actual paused timer/carry/coast/history/invalid frames at1280/1600 remain necessary for readable PAUSED state, selected historical provenance, toolbar width and rung visibility. Operator click input feedback is deliberately held in paused wall time; it must not be mistaken for an accepted plant signal. External PLC and challenge paths remain outside the supported pause/scan feature. No physical safety, continuous machinery clearance or commissioning claim follows from this review.
