# QA-UI-002 stale outer status — causal analysis

Read-only source analysis and independent reinspection of original native [blank Run](../screen-audit-2026-10-21/small-diagnostics-empty-run.png) and [settled valid Run](../screen-audit-2026-10-21/small-diagnostics-error-list-settled.png). No source repairs, runtime queries or UI input.

## Established behavior and exact seam

Blank draft correctly produces EDIT001, no controller/STOPPED and red outer RUN BLOCKED. After normal +NETWORK then NO toolbar insertion and App Playback Run, original settled pixels show energized monitor rails, editor RUNNING scan1338, top/local scan1343 and0ERRORS verification passed, while the outer Scene I/O strip still asserts RUN BLOCKED/current draft invalid. This is contradictory current-state wording, not a controller execution failure.

Source makes the split explicit:

1. `Main.cs:1591–1596` invalid `RunEditorDraftController` path stops, publishes validation, and calls `SetWorkspaceStatus("RUN BLOCKED · current ladder draft is invalid", true)`.
2. Valid path at1604–1611 attaches only if execution identity changed, then calls `PublishVerifiedDraftState()` followed by `RunActiveController()`.
3. `SimulatorShell.cs:21113–21122` verifies exact loaded-draft identity and clears validation refreshers plus **_ladderWorkspaceStatus** only. It never changes **_workspaceStatus**.
4. Ordinary local `RunActiveController` calls `_virtualController.Run()` and returns (`Main.cs:1660s`), without publishing an outer successful Run status. External Run explicitly publishes its outer status; the local branch differs.
5. `UpdateVirtualController` updates watch/canvas, **_ladderWorkspaceStatus**, controller/cycle labels per snapshot (`SimulatorShell.cs:21161+`), but does not replace **_workspaceStatus**.
6. `SetWorkspaceStatus` at23893 simply assigns the persistent outer label/color and records an operator event. There is no expiry or diagnostic identity attached to its text. Thus a valid local Run can leave the previous invalid-Run event text indefinitely as though it were current state.

This is a source-supported causal path, stronger than the earlier hypothesis that Main owned an unrefreshed separate footer. Both labels belong to SimulatorShell; the publication scopes differ.

## Narrow coherent repair options — not an implementation plan

- Small behavior fix: explicitly publish a successful local Run outer status after the actual controller accepts Run. Keep validation/drive/E-stop/challenge/external readiness gates authoritative; do not announce RUNNING before those gates. A successful Verify+Load can publish VALID/LOADED separately from RUNNING.
- More robust bounded fix: give the transient outer diagnostic an owner/category and draft identity; successful verification clears only the prior current-draft-invalid category for the exact loaded draft. Avoid string matching or blindly replacing arbitrary latest safety/drive/save errors.
- A larger single-status-view-model change could derive all current runtime/validation surfaces from one state. It is unnecessary solely to fix this demonstrated seam and would require broader regression work.

Do not reset program memory, discard drafts, clear every diagnostic on any scan, or replace real readiness errors merely to align labels.

## Regression obligations

The existing `SimulatorShell.AuditDiagnosticsReview.cs:25–37` seeds this exact outer failure but its `cleared` assertion checks summaries and **_ladderWorkspaceStatus**, omitting **_workspaceStatus**. Consequently it can pass while the native defect remains. Extend coverage to exact outer message/category/color after actual RunRequested; include same already-loaded valid program, invalid→valid edited program, and invalid draft while an older valid program exists. Verify invalid errors remain, blocked E-stop/drive/external states are not overwritten, successful Verify+Load is distinguished from actual Run, and metadata-only Watch edits do not reset memory. Then repeat the original native blankRun→+NETWORK→NO→App Playback Run route; automated label checks alone are not screen proof.

## Other status surfaces

The same generic last-event label can retain other messages until another `SetWorkspaceStatus` writer replaces them; source alone does not prove another misleading native case. Current editor validation, monitor, top runtime/cycle and bottom Now Playing are demonstrably refreshed in this exact repro. Health history retaining the old rejection is legitimate event history. Track this as the existing single QA-UI-002 issue, not one defect per scene/status surface. No other stale-state defect is confirmed here.

## Exact authoring input clarification

`../screen-audit-2026-10-21/postbackup-small-ui-actions.jsonl` rows99–102 record +NETWORK(770,185), NO toolbar(354,185), then App Playback menu(335,25) and Run. There is no separate coil-insertion click in this recorded authoring sequence. The resulting original settled rung visibly contains material_call_pressed contact and material_call_on coil; the report does not attribute that coil to an additional human click. Original settled pixels directly confirm0ERRORS, energized supply rail and RUNNING/advancing scan, as stated above.
