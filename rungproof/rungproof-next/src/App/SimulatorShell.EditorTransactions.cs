using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private sealed record PendingEditorForm(Func<bool> HasPending, Func<bool> Apply, Action Discard);
    private readonly List<PendingEditorForm> _pendingEditorForms = [];
    private ConfirmationDialog? _pendingEditorDialog;
    private Action? _pendingEditorContinuation;
    private Action? _pendingEditorNavigationCancelled;

    /// <summary>Integrated scene saves must resolve staged ladder forms before serializing or marking saved.</summary>
    public void ResolveIntegratedProjectSave(Action save, Action? cancelled = null)
        => ResolvePendingEditorForms(save, cancelled);

    /// <summary>Navigation and Save share the same explicit resolution; cancellation never invokes continuation.</summary>
    private void ResolvePendingEditorForms(Action continuation, Action? navigationCancelled = null)
    {
        if (!_pendingEditorForms.Any(form => form.HasPending())) { continuation(); return; }
        _pendingEditorContinuation = continuation;
        _pendingEditorNavigationCancelled = navigationCancelled;
        if (_pendingEditorDialog is null)
        {
            _pendingEditorDialog = new ConfirmationDialog
            {
                Name = "ResolvePendingEditorForms", Title = "Pending editor changes",
                DialogText = "Apply the pending form before continuing, discard it, or cancel navigation.",
                OkButtonText = "Apply", CancelButtonText = "Cancel"
            };
            _pendingEditorDialog.AddButton("Discard", false, "discard");
            _pendingEditorDialog.Confirmed += () =>
            {
                var pending = _pendingEditorForms.Where(form => form.HasPending()).ToArray();
                // Each form owns a captured document revision. Sequential application would
                // invalidate later forms and leave a partial commit. Reject before any form runs.
                if (pending.Length > 1)
                {
                    _pendingEditorContinuation = null;
                    _pendingEditorNavigationCancelled?.Invoke(); _pendingEditorNavigationCancelled = null;
                    _pendingEditorDialog.DialogText = "Multiple forms contain pending edits. Nothing was applied. Cancel and discard extra forms before applying one form, or choose Discard to discard all pending edits.";
                    return;
                }
                foreach (var form in pending)
                    if (!form.Apply()) { _pendingEditorContinuation = null; _pendingEditorNavigationCancelled?.Invoke(); _pendingEditorNavigationCancelled = null; return; }
                var next = _pendingEditorContinuation; _pendingEditorContinuation = null; _pendingEditorNavigationCancelled = null; next?.Invoke();
            };
            _pendingEditorDialog.CustomAction += action =>
            {
                if (action != "discard") return;
                foreach (var form in _pendingEditorForms.Where(form => form.HasPending()).ToArray()) form.Discard();
                // Individual discard refreshes may be suppressed while sibling forms remain dirty.
                // Reload controls only after every flag is cleared, including the other editor tab.
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                _pendingEditorDialog.Hide(); var next = _pendingEditorContinuation; _pendingEditorContinuation = null; _pendingEditorNavigationCancelled = null; next?.Invoke();
            };
            _pendingEditorDialog.Canceled += () => { _pendingEditorContinuation = null; _pendingEditorNavigationCancelled?.Invoke(); _pendingEditorNavigationCancelled = null; };
            AddChild(_pendingEditorDialog);
        }
        var multiplePending = _pendingEditorForms.Count(form => form.HasPending()) > 1;
        _pendingEditorDialog.GetOkButton().Disabled = multiplePending;
        _pendingEditorDialog.DialogText = multiplePending
            ? "Multiple forms contain pending edits. Combined Apply is unavailable. Cancel and discard extra forms before applying one form, or choose Discard to discard all pending edits."
            : "Apply the pending form before continuing, discard it, or cancel navigation.";
        _pendingEditorDialog.PopupCentered();
    }
}
