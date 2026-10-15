using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    public bool VerifyWorkspaceProjectGuard(Action makeWorkspaceDirty, Action approveReplacement, out string result)
    {
        var sceneId = _activeScene!.Id;
        var original = CaptureSceneLadderDraft();
        var drafts = _sceneLadderDrafts.ToArray();
        var path = System.IO.Path.Combine(OS.GetUserDataDir(), $"workspace-project-guard-{Guid.NewGuid():N}.rpproj.json");
        try
        {
            const string marker = "Workspace cross-scene open review";
            _ladderDocument.Rungs[0].Label = marker;
            var saved = TrySaveLadderProject(path, out _);
            SceneRequested?.Invoke("scene-2-conveyor-pusher");
            makeWorkspaceDirty();
            var before = LadderEditorProjectJson.Save(_ladderDocument);
            var queued = !TryOpenLadderProject(path, out _)
                && _unsavedWorkspaceDialog.Visible && _activeScene?.Id == "scene-2-conveyor-pusher"
                && LadderEditorProjectJson.Save(_ladderDocument) == before;
            approveReplacement();
            var resumed = _activeScene?.Id == sceneId && _ladderDocument.Rungs[0].Label == marker
                && _virtualProgram?.Networks[0].Label == marker;
            result = $"projectQueued={queued} projectResumed={resumed}";
            return saved && queued && resumed;
        }
        finally
        {
            VirtualControllerDisabled?.Invoke();
            if (_activeScene?.Id != sceneId) SceneRequested?.Invoke(sceneId);
            _ladderDocument.RestoreSnapshot(original.Document);
            _ladderSavedProjectJson = original.SavedJson;
            _ladderProjectPath = original.ProjectPath;
            _ladderHistory = original.History;
            _sceneLadderDrafts.Clear();
            foreach (var entry in drafts) _sceneLadderDrafts.Add(entry.Key, entry.Value);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            System.IO.File.Delete(path);
        }
    }
}
