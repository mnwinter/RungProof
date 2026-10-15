using System;
using System.IO;
using System.Linq;
using Godot;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyWorkspaceReplacementGuards()
    {
        var original = CreateWorkspaceDocument();
        var originalBaseline = _workspaceSavedSnapshotJson;
        var shell = _simulatorShell!;
        var guard = shell.GetNode<ConfirmationDialog>("UnsavedWorkspaceDialog");
        var saveDialog = shell.GetNode<FileDialog>("SaveWorkspaceDialog");
        var path = ProjectSettings.GlobalizePath($"res://.tools/workspace-guard-review-{Guid.NewGuid():N}.json");
        var actions = 0;
        try
        {
            PlaceAsset(_candidateCatalog!.Assets.First(item => item.Id == "material-handling.belt-conveyor.600x6000.v1"));
            var edited = CreateWorkspaceDocument();
            var queued = !GuardWorkspaceReplacement(() => actions++) && guard.Visible && actions == 0;
            guard.EmitSignal(AcceptDialog.SignalName.Canceled);
            var cancelled = _pendingWorkspaceAction is null && actions == 0 && shell.IsWorkspaceDirty && _workspaceNodes.Count == 1;
            GuardWorkspaceReplacement(() => actions++);
            guard.EmitSignal(AcceptDialog.SignalName.CustomAction, "save");
            var saveOffered = saveDialog.Visible && !guard.Visible;
            saveDialog.EmitSignal(AcceptDialog.SignalName.Canceled);
            var saveCancelled = _pendingWorkspaceAction is null && actions == 0 && shell.IsWorkspaceDirty;
            GuardWorkspaceReplacement(() => actions++);
            guard.EmitSignal(AcceptDialog.SignalName.CustomAction, "save");
            WorkspaceRepository.Save(edited, path);
            using (var reader = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
                SaveWorkspaceToPath(path);
            var failureBlocked = _pendingWorkspaceAction is not null && actions == 0 && guard.Visible && shell.IsWorkspaceDirty;
            guard.EmitSignal(AcceptDialog.SignalName.CustomAction, "save");
            SaveWorkspaceToPath(path);
            var saveResumed = actions == 1 && _pendingWorkspaceAction is null && !shell.IsWorkspaceDirty;

            PlaceAsset(_candidateCatalog.Assets.First(item => item.Id == "sensing.photoelectric.through-beam.v1"));
            GuardWorkspaceReplacement(() => actions++);
            guard.EmitSignal(AcceptDialog.SignalName.Confirmed);
            var discardDoesNotFakeSave = actions == 2 && shell.IsWorkspaceDirty;
            shell.RequestSceneChange("scene-2-conveyor-pusher");
            var sceneWaiting = _currentSceneId == original.SourceSceneId && _workspaceNodes.Count == 2 && guard.Visible;
            guard.EmitSignal(AcceptDialog.SignalName.Canceled);
            var sceneCancelled = _currentSceneId == original.SourceSceneId && _workspaceNodes.Count == 2;
            LoadWorkspaceFromPath(path);
            var loadWaiting = guard.Visible && _workspaceNodes.Count == 2;
            guard.EmitSignal(AcceptDialog.SignalName.Canceled);
            var loadCancelled = _workspaceNodes.Count == 2;
            shell.RequestWindowClose();
            var closeWaiting = guard.Visible && _pendingWorkspaceAction is not null;
            guard.EmitSignal(AcceptDialog.SignalName.Canceled);
            var closeCancelled = _pendingWorkspaceAction is null && shell.IsWorkspaceDirty;
            LoadWorkspaceFromPath(path);
            guard.EmitSignal(AcceptDialog.SignalName.Confirmed);
            var loadResumed = _workspaceNodes.Count == 1 && !shell.IsWorkspaceDirty;
            shell.GetNode<AcceptDialog>("WorkspaceFeedbackDialog").Hide();

            var crossScene = shell.VerifyWorkspaceProjectGuard(
                () => PlaceAsset(_candidateCatalog.Assets.First(item => item.Id == "sensing.photoelectric.through-beam.v1")),
                () => guard.EmitSignal(AcceptDialog.SignalName.Confirmed), out var projectResult);
            var passed = queued && cancelled && saveOffered && saveCancelled && failureBlocked && saveResumed
                && discardDoesNotFakeSave && sceneWaiting && sceneCancelled && loadWaiting && loadCancelled
                && closeWaiting && closeCancelled && loadResumed && crossScene;
            GD.Print($"WORKSPACE_REPLACEMENT_VERIFY {(passed ? "PASS" : "FAIL")} queue={queued} cancel={cancelled} saveOffer={saveOffered} saveCancel={saveCancelled} failureBlocks={failureBlocked} saveResume={saveResumed} discardRetainsDirty={discardDoesNotFakeSave} sceneWait={sceneWaiting} sceneCancel={sceneCancelled} loadWait={loadWaiting} loadCancel={loadCancelled} closeWait={closeWaiting} closeCancel={closeCancelled} loadResume={loadResumed} project={crossScene} {projectResult}");
            if (!passed) throw new InvalidOperationException("Unsaved workspace replacement guard failed.");
        }
        finally
        {
            CancelPendingWorkspaceAction();
            ApplyWorkspaceSnapshot(original);
            _undoHistory.Clear();
            _redoHistory.Clear();
            _workspaceSavedSnapshotJson = originalBaseline;
            RefreshWorkspaceDirty();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private void VerifyWorkspaceDirtyUndo()
    {
        var original = CreateWorkspaceDocument();
        var originalBaseline = _workspaceSavedSnapshotJson;
        var path = ProjectSettings.GlobalizePath($"res://.tools/workspace-baseline-review-{Guid.NewGuid():N}.json");
        try
        {
            PlaceAsset(_candidateCatalog!.Assets.First(item => item.Id == "material-handling.belt-conveyor.600x6000.v1"));
            var editedDirty = _simulatorShell!.IsWorkspaceDirty;
            UndoWorkspace();
            var undoClean = !_simulatorShell.IsWorkspaceDirty;
            var undoSelectionClear = _transformGizmo?.Visible != true
                && !_simulatorShell.GetNode<RichTextLabel>("Workspace/RightDock/InspectorTabs/Inspector").Text.Contains("Selected workspace object", StringComparison.Ordinal);
            RedoWorkspace();
            var redoDirty = _simulatorShell.IsWorkspaceDirty;
            SaveWorkspaceToPath(path);
            var savedClean = !_simulatorShell.IsWorkspaceDirty && File.Exists(path);
            PlaceAsset(_candidateCatalog.Assets.First(item => item.Id == "sensing.photoelectric.through-beam.v1"));
            var savedEditDirty = _simulatorShell.IsWorkspaceDirty;
            UndoWorkspace();
            var savedUndoClean = !_simulatorShell.IsWorkspaceDirty;
            RedoWorkspace();
            var savedRedoDirty = _simulatorShell.IsWorkspaceDirty;
            var passed = editedDirty && undoClean && undoSelectionClear && redoDirty && savedClean && savedEditDirty && savedUndoClean && savedRedoDirty;
            GD.Print($"WORKSPACE_DIRTY_VERIFY {(passed ? "PASS" : "FAIL")} edited={editedDirty} undoClean={undoClean} undoSelectionClear={undoSelectionClear} redoDirty={redoDirty} savedClean={savedClean} savedEdit={savedEditDirty} savedUndo={savedUndoClean} savedRedo={savedRedoDirty}");
            if (!passed) throw new InvalidOperationException("Workspace dirty state does not reflect Undo/Redo to its saved baseline.");
        }
        finally
        {
            ApplyWorkspaceSnapshot(original);
            _undoHistory.Clear();
            _redoHistory.Clear();
            _workspaceSavedSnapshotJson = originalBaseline;
            RefreshWorkspaceDirty();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void VerifyWorkspaceWriteFailure(WorkspaceDocument document)
    {
        // Windows file sharing blocks replacement while this reader owns the
        // destination. Only this verifier's uniquely named disposable file is used.
        if (!OperatingSystem.IsWindows()) return;
        var path = ProjectSettings.GlobalizePath($"res://.tools/workspace-write-review-{Guid.NewGuid():N}.json");
        try
        {
            WorkspaceRepository.Save(document, path);
            var original = File.ReadAllText(path);
            var updated = document with { Placements = document.Placements.Select((placement, index) => index == 0
                ? placement with { Position = [placement.Position[0] + 0.1, placement.Position[1], placement.Position[2]] }
                : placement).ToArray() };
            var blocked = false;
            using (var reader = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
            {
                try { WorkspaceRepository.Save(updated, path); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { blocked = true; }
            }
            var preserved = blocked && File.ReadAllText(path) == original;
            var cleaned = !Directory.EnumerateFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.tmp").Any();
            WorkspaceRepository.Save(updated, path);
            var retry = File.ReadAllText(path) != original;
            var passed = preserved && cleaned && retry;
            GD.Print($"WORKSPACE_WRITE_GUARD_VERIFY {(passed ? "PASS" : "FAIL")} preserved={preserved} cleaned={cleaned} retry={retry}");
            if (!passed) throw new InvalidOperationException("Workspace write failure did not preserve its previous saved contents.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
