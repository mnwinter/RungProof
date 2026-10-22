using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private sealed record PendingEditorForm(Func<string> Describe, Func<bool> HasPending, Func<bool> Apply, Action Discard);
    private readonly List<PendingEditorForm> _pendingEditorForms = [];
    private ConfirmationDialog? _pendingEditorDialog;
    private Action? _pendingEditorContinuation;
    private Action? _pendingEditorNavigationCancelled;

    /// <summary>Integrated scene saves must resolve staged ladder forms before serializing or marking saved.</summary>
    public void ResolveIntegratedProjectSave(Action save, Action? cancelled = null)
        => ResolvePendingEditorForms(save, cancelled);

    /// <summary>Integrated workspace replacement must resolve staged ladder forms before replacing the project.</summary>
    public void ResolveIntegratedProjectReplacement(Action replace, Action? cancelled = null)
        => ResolvePendingEditorForms(replace, cancelled);

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
                DialogText = "Apply the pending edit before continuing, discard it, or cancel navigation.",
                OkButtonText = "APPLY AND CONTINUE", CancelButtonText = "CANCEL",
                MinSize = new Vector2I(720, 250)
            };
            _pendingEditorDialog.GetLabel().AddThemeFontSizeOverride("font_size", 16);
            _pendingEditorDialog.AddButton("DISCARD AND CONTINUE", false, "discard");
            _pendingEditorDialog.Confirmed += () =>
            {
                var pending = _pendingEditorForms.Where(form => form.HasPending()).ToArray();
                // Each form owns a captured document revision. Sequential application would
                // invalidate later forms and leave a partial commit. Reject before any form runs.
                if (pending.Length > 1)
                {
                    _pendingEditorContinuation = null;
                    _pendingEditorNavigationCancelled?.Invoke(); _pendingEditorNavigationCancelled = null;
                    _pendingEditorDialog.DialogText = BuildPendingEditorDialogText(pending, multiple: true, applyRejected: true);
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
        var pendingForms = _pendingEditorForms.Where(form => form.HasPending()).ToArray();
        var multiplePending = pendingForms.Length > 1;
        _pendingEditorDialog.GetOkButton().Disabled = multiplePending;
        _pendingEditorDialog.DialogText = BuildPendingEditorDialogText(pendingForms, multiplePending, applyRejected: false);
        _pendingEditorDialog.PopupCentered(new Vector2I(720, 250));
    }

    private static string BuildPendingEditorDialogText(PendingEditorForm[] pending, bool multiple, bool applyRejected)
    {
        static string SafeDescription(PendingEditorForm form)
        {
            var description = form.Describe().Trim();
            return description.Length == 0 ? "Unnamed editor form" : description;
        }

        if (!multiple)
            return $"Pending edit: {SafeDescription(pending[0])}\n\nApply and continue, discard and continue, or cancel and remain here.";

        var list = string.Join("\n", pending.Select(form => $"- {SafeDescription(form)}"));
        var prefix = applyRejected
            ? "Nothing was applied. More than one editor contains pending changes:"
            : "More than one editor contains pending changes:";
        return $"{prefix}\n{list}\n\nCombined Apply is unavailable because each form owns a separate revision. Discard all and continue, or cancel and resolve each edit individually.";
    }
}
