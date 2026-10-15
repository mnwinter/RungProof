using System;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    // Native QA only: one-shot failure after real composition/registration.
    private bool _injectWorkspaceDriveRegistrationFailure;

    private void ApplyWorkspaceDriveRegistrationTransaction(WorkspaceDocument connected, string instanceId)
    {
        // Capture before RecordUndo: a rejected connection must not consume the
        // previous Undo/Redo history or replace its saved dirty baseline.
        var rollback = CaptureWorkspaceReplacementRollback();
        try
        {
            RecordUndo();
            RebuildTemplateWorkspace(connected);
            if (_injectWorkspaceDriveRegistrationFailure)
            {
                _injectWorkspaceDriveRegistrationFailure = false;
                throw new InvalidOperationException("Injected drive registration failure after actual rebuild.");
            }
            SetSelection([instanceId]);
        }
        catch (Exception registrationError)
        {
            // Reuse the existing exact editor/path/baseline/history rollback.
            // Recovery is stopped; never implicitly resume the previous program.
            try { rollback(); }
            catch (Exception recoveryError)
            {
                throw new InvalidOperationException("Drive connection failed and workspace recovery also failed: "
                    + recoveryError.Message, new AggregateException(registrationError, recoveryError));
            }
            throw new InvalidOperationException("Drive connection failed; previous workspace restored and stopped: "
                + registrationError.Message, registrationError);
        }
    }
}
