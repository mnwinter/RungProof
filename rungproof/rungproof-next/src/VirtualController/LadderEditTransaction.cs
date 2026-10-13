using System;

namespace RungProof.Next.VirtualController;

/// <summary>A detached draft. No document or history mutation occurs until the whole edit validates.</summary>
public sealed class LadderEditTransaction
{
    private readonly LadderEditorDocument _source;
    private readonly long _revision;
    private readonly string _description;
    private bool _closed;
    public LadderEditorDocument Draft { get; }
    public bool IsPending => !_closed;

    public LadderEditTransaction(LadderEditorDocument source, string description)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _revision = source.Revision;
        _description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Description required.", nameof(description)) : description;
        Draft = new LadderEditorDocument();
        Draft.RestoreSnapshot(source.CaptureSnapshot());
    }

    public void FocusLost() { /* Keep pending values visible; blur never commits. */ }
    public void Cancel() => _closed = true;

    public bool TryCommit(LadderEditorHistory history, bool mutationLocked, out string error,
        Func<LadderEditorDocument, string?>? validate = null)
    {
        error = _closed ? "Edit is closed." : mutationLocked ? "Editing is locked." :
            _source.Revision != _revision ? "The document changed. Reopen this edit." : string.Empty;
        if (error.Length > 0) return false;
        try
        {
            error = validate?.Invoke(Draft) ?? string.Empty;
            if (error.Length > 0) return false;
            // Capture the completed edit before touching history. Validators and draft mutations
            // can fail safely without leaving a half-applied operation or a spurious undo entry.
            var completed = Draft.CaptureSnapshot();
            history.Execute(_source, _description, () => _source.RestoreSnapshot(completed));
            _closed = true;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        { error = exception.Message; return false; }
    }
}
