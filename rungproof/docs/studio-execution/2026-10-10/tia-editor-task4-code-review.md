# Task 4 persistence review

2026-10-11. Independent static review of LadderEditorProjectJson, LadderProjectFileStore, WorkspaceProjectBundleRepository, Shell save/open paths, integrated Main save boundary and relevant tests. No reviewer builds or tests. Evidence inspected: storage-03.log (14 checks pass), bundle-green-03.log and production verifier source, integration-37 migration/recursive tests.

## Decision: revise

### P1 — integrated workspace Save bypasses pending editor forms

`src/App/Main.cs:4085` SaveWorkspaceToPath constructs its bundle with CaptureIntegratedLadderDraft, saves immediately, then calls MarkIntegratedLadderDraftSaved and CompletePendingWorkspaceAction. `src/App/SimulatorShell.IntegratedProject.cs:13` serializes the committed document only; staged operand/property/tag/block/interface/task/CALL edits are absent. The workspace File menu, workspace save dialog FileSelected and ShowWorkspaceSaveDialog do not enter ResolvePendingEditorForms. Standalone ShowLadderSaveDialog and Save As do.

Trigger: type an unapplied operand or property change, then invoke workspace Save through the shell File route (or save after leaving the ladder view while pending fields remain). The application reports the integrated project saved with old committed ladder values, without the agreed Apply/Discard/Cancel decision. A queued destructive workspace action can also continue after this save boundary.

Fix the central integrated-save boundary before any capture, file write, saved-marker update or pending-action continuation. Reuse the pending resolver; do not auto-commit on blur. Preserve existing synchronous no-pending callers. Regression cases: pending edit -> Cancel leaves file/markers/action unchanged; invalid Apply leaves file/markers/action unchanged; valid Apply saves exactly one committed transaction; Discard writes the unchanged committed draft; multi-form pending follows the existing explicit combined-Apply rejection policy. Exercise actual production workspace save entry routes, not only the serializer.

## Scoped accepted findings

- Standalone writes reject existing malformed/future-schema files before SaveValidated. Bundle writes read/validate the existing outer and inner versions before replacement. Future/malformed rejection does not rewrite those destinations in the inspected single-writer path.
- SaveValidated validates before destination mutation, writes a unique same-directory temporary file, flushes it, rereads and validates it, then uses File.Replace or non-overwriting File.Move. Read-only and backup failures preserve the original. Cleanup targets only its own temporary path.
- Migration copies the original file with File.Copy to a unique non-overwriting backup. This preserves raw bytes in source. Bundle verifier compares ReadAllBytes using SequenceEqual, covering original outer-bundle bytes rather than just reserialized inner content.
- Editor schema 2 preserves recursive root, terminal identity/presence and legacy-empty-branch semantics. Schema 1 explicitly retains Legacy CALL semantics, migrates flat branches without Boolean redistribution and retains generated parallel identity. Duplicate tree IDs and invalid depth fail before document publication. Allocation cursor repair does not mark an otherwise unchanged saved document dirty.
- Shell project path/saved JSON are assigned after successful standalone write. Pending snapshot saves remove their queued item only after successful write; a failed write does not authorize the queued destructive action.
- Readable compiler-invalid drafts remain saveable; executable compilation is separate from structural file readability.

## Remaining evidence gates and limits

- storage-03 covers validation-before-write, reread failure, read-only target, locked migration source, unique backup and Save As preservation. It does not inject an operating-system failure specifically inside File.Replace, power loss, or disk-full behavior. Do not claim crash/power-loss recovery qualification from these 14 checks.
- Standalone backup test compares ASCII text; exact-byte behavior follows File.Copy source and is directly exercised for integrated bundles. A standalone BOM/non-ASCII/raw-byte test would strengthen that boundary.
- Existing read/validate followed by replace is a single-writer workflow. No compare-and-swap or external-edit conflict detection exists between the initial read and final replacement. Multi-process/external-editor conflict handling is not established by this review.
- Native file-dialog cancel/overwrite and saved-marker behavior still need frozen-candidate interaction evidence. No exported-candidate, physical PLC, release or distribution approval is implied.

Review scope is current changing source; rerun targeted integrated-save regression and relevant storage/bundle checks after the P1 fix.

## Rereview: integrated Save guard

Root reproduced the original bypass in editor-transactions-21-red before fixing it. Current Main.SaveWorkspaceToPath routes through SimulatorShell.ResolveIntegratedProjectSave before SaveResolvedWorkspaceToPath captures data or updates markers. Cancel and failed Apply call CancelPendingWorkspaceAction; successful Apply/Discard alone invoke the save continuation. No new flow defect found in this fix. Source-level P1 is closed, pending fresh production-route verifier results. The new verifier covers Cancel and invalid Apply no-write/no-marker cases; add successful Apply and Discard plus queued-action cancellation to complete behavioral evidence. No reviewer build run.

## Related A04 rereview

AggregateArrayDraft.TryChangeType now changes only Type and retains visible/hidden literal strings. AggregateTagEditor copies edited text before changing type, resizes by absolute index, and rebuilds fields with preserved strings. TryRead validates each field against the selected type, so malformed/incompatible text remains pending. This corrects the approved A04 contract; no new source defect found. integration-41.log reports 314 passed/0 failed, including updated pure array semantics. New production-widget round-trip cases await root native verification. Earlier missing-A04 ledger status is a historical checkpoint superseded by this source fix, not a continuing reset-values defect.
