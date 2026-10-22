using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    public bool VerifyAuditDiagnosticRecovery(out string result)
    {
        static IEnumerable<Node> Descendants(Node root)
        {
            foreach (var child in root.GetChildren())
            {
                yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
        var summaries = Descendants(this).OfType<Label>()
            .Where(label => label.Name == "ValidationSummary").ToArray();
        var saved = _ladderDocument.CaptureSnapshot();
        if (summaries.Length == 0 || !TryBuildCurrentLadderProgram(out _, out _))
        { result = "valid initial draft/error summary unavailable"; return false; }
        try
        {
            RunRequested?.Invoke();
            SetVirtualControllerValidation(new[] { new LadderValidationIssue("EDIT001", "$.blocks", "Previous empty draft") });
            SetWorkspaceStatus("RUN BLOCKED - previous invalid draft", true);
            // Same already-loaded program: no attachment callback can clear this.
            RunRequested?.Invoke();
            var cleared = _ladderMonitorMatchesLoadedProgram
                && summaries.All(label => label.Text.StartsWith("0 ERRORS", StringComparison.Ordinal))
                && !_ladderWorkspaceStatus.Text.Contains("BLOCKED", StringComparison.Ordinal);
            foreach (var block in _ladderDocument.Blocks) block.Rungs.Clear();
            RunRequested?.Invoke();
            PublishVerifiedDraftState();
            var invalidRetained = !_ladderMonitorMatchesLoadedProgram
                && summaries.All(label => label.Text.Contains("ERROR", StringComparison.Ordinal)
                    && !label.Text.StartsWith("0 ERRORS", StringComparison.Ordinal));
            result = $"successfulRunClearsPreviousErrors={cleared} invalidDraftErrorsRetained={invalidRetained}";
            return cleared && invalidRetained;
        }
        finally
        {
            _ladderDocument.RestoreSnapshot(saved);
            RunRequested?.Invoke();
        }
    }
}
