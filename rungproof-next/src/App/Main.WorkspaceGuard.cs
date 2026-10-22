using System;

namespace RungProof.Next.App;

public partial class Main
{
    private Action? _pendingWorkspaceAction;
    private bool _workspaceReplacementAuthorized;

    // True lets the caller continue synchronously. False means the complete
    // replacement operation is waiting for Save, Discard, or Cancel.
    private bool GuardWorkspaceReplacement(Action action)
    {
        if (!GuardChallengeSceneReplacement(action)) return false;
        RefreshWorkspaceDirty(); // Includes the actual integrated ladder draft.
        if (_workspaceReplacementAuthorized || _simulatorShell?.IsWorkspaceDirty != true) return true;
        if (_pendingWorkspaceAction is not null)
        {
            // Do not silently ignore a later demo selection. Keep the latest
            // destination and bring the Save / Discard / Cancel choice forward.
            _pendingWorkspaceAction = action;
            _simulatorShell.ShowUnsavedWorkspaceDialog(
                "A new scene was requested while this decision was open. The latest request is now waiting.\n\n");
            return false;
        }
        _pendingWorkspaceAction = action;
        _simulatorShell.ShowUnsavedWorkspaceDialog();
        return false;
    }

    private void CancelPendingWorkspaceAction()
    {
        _pendingWorkspaceAction = null;
        _simulatorShell?.HideWorkspaceReplacementDialogs();
    }

    private void CompletePendingWorkspaceAction()
    {
        var action = _pendingWorkspaceAction;
        CancelPendingWorkspaceAction();
        if (action is null) return;
        // Discard authorizes this replacement; it must not label the current
        // unsaved document as saved if a later ladder guard is cancelled.
        _workspaceReplacementAuthorized = true;
        try { action(); }
        finally { _workspaceReplacementAuthorized = false; }
    }
}
