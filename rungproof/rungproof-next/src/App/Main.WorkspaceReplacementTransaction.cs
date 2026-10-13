using System;
using System.Linq;
using Godot;
namespace RungProof.Next.App;
public partial class Main
{
    // Only the coordinated native verifier sets this one-shot boundary fault.
    private string? _injectWorkspaceReplacementFailure;
    private Action CaptureWorkspaceReplacementRollback()
    {
        var layout=CreateWorkspaceDocument();
        var restoreEditor=_simulatorShell!.CaptureIntegratedEditorRollback();
        var savedLayout=_workspaceSavedSnapshotJson;var savedDraft=_workspaceSavedLadderJson;
        var undo=_undoHistory.ToArray();var redo=_redoHistory.ToArray();
        return () =>
        {
            // Recompose the original graph; never resume the previous controller.
            RebuildTemplateWorkspace(layout);
            DisableVirtualController();
            restoreEditor();
            _workspaceSavedSnapshotJson=savedLayout;_workspaceSavedLadderJson=savedDraft;
            _undoHistory.Clear();_undoHistory.AddRange(undo);
            _redoHistory.Clear();_redoHistory.AddRange(redo);
            _simulatorShell.SetIntegratedSceneIdentity();RefreshWorkspaceDirty();
            GD.Print("WORKSPACE_REPLACEMENT_ROLLBACK restored=true stopped=true scene="+layout.SourceSceneId);
        };
    }
}
