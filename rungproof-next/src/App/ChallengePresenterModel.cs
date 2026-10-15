using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RungProof.Next.Challenges;

namespace RungProof.Next.App;

public enum ChallengeHorizontalNavigation { Left, Right, Home, End }
public readonly record struct ChallengeKeyboardPosition(int Contact, int Insertion, bool Output);

/// <summary>Renderer-neutral projections only. No mutation, award, runtime or file access.</summary>
public static class ChallengePresenterModel
{
    public static bool IsPaletteInstruction(string name) => name is "AddNormallyOpen" or "AddNormallyClosed"
        or "AddAssignCoil" or "AddBranch" or "RemoveBranch" or "AddRung" or "RemoveInstruction";

    public static int ClampSelection(int count, int selected)
        => count <= 0 ? -1 : Math.Clamp(selected, 0, count - 1);

    public static int RevealVertical(double center, double viewportHeight, int offset)
    {
        // Include the operand label above and branch symbol/caption below.
        var top = center - 55;
        var bottom = center + 55;
        if (top < offset) return Math.Max(0, (int)Math.Floor(top));
        if (bottom > offset + viewportHeight) return Math.Max(0, (int)Math.Ceiling(bottom - viewportHeight));
        return offset;
    }

    public static string Status(ChallengePresentationState state) =>
        $"{state.State} · {state.StatusText} · {(state.IsCurrentProof ? "CURRENT PROOF" : "No current proof")}" +
        (state.PersistenceStatus == ChallengePersistenceStatus.NotSaved ? " · Progress not saved" : "") +
        (state.MutationLocked ? $" · worker cleanup pending: {state.PendingAction}; editing locked, read/copy/save available" : "");

    public static string Ledger(ChallengeDefinition definition, ChallengeRunResult? result,
        IReadOnlyList<ChallengeProgressRecord> progress)
    {
        var text = new StringBuilder("Test ledger — historical evidence; edits invalidate current proof.\n");
        if (result is not null) text.AppendLine($"{result.Outcome}: {result.Code} {result.Diagnostic}");
        foreach (var test in definition.Cases)
        {
            var row = result?.Cases.FirstOrDefault(item => item.CaseId == test.Id);
            text.AppendLine($"{test.Id}: {row?.Outcome.ToString() ?? "Not Run"}");
            if (row is null) continue;
            text.AppendLine($"Expected: {row.Expected}\nObserved: {row.Observed}");
            if (row.FirstFailure is { } failure)
                text.AppendLine($"First failure cycle {failure.Cycle}, case sequence {failure.CaseSequence}, scan {failure.ScanNumber}, {failure.SimulatedTime.TotalMilliseconds:0} ms");
        }
        text.AppendLine($"Historical local completions: {progress.Count(row => row.Identity.ChallengeId == definition.Id)}. Historical records never award current proof.");
        return text.ToString();
    }

    public static string Hints(ChallengeDefinition definition, int count)
        => string.Join("\n\n", definition.Hints.Take(Math.Clamp(count, 0, definition.Hints.Count))
            .Select((hint, index) => $"Hint {index + 1}: {hint}"));

    public static string TraceRow(ChallengeObservation row)
        => $"HISTORICAL — {row.CaseId}, cycle {row.Cycle}, sequence {row.CaseSequence}, scan {row.ScanNumber}\n"
            + $"Time {row.SimulatedTime.TotalMilliseconds:0} ms\nInputs: {Map(row.EffectiveInputs)}\nApplied outputs: {Map(row.AppliedOutputs)}\n"
            + $"Carton present: {row.PlantAfter.ObjectPresent}; completed: {row.PlantAfter.Completed}\n"
            + string.Join("; ", row.PlantAfter.Numbers.Select(pair => $"{pair.Key}={pair.Value:R}"))
            + "\n" + string.Join("; ", row.PlantAfter.Text.Select(pair => $"{pair.Key}={pair.Value}"))
            + "\nDiagnostics: " + string.Join("; ", row.RuntimeDiagnostics);

    public static ChallengeKeyboardPosition Navigate(int contactCount, ChallengeKeyboardPosition current,
        ChallengeHorizontalNavigation key)
    {
        if (contactCount < 0) throw new ArgumentOutOfRangeException(nameof(contactCount));
        var last = checked(contactCount * 2 + 1);
        var position = current.Output ? last : current.Contact >= 0 ? current.Contact * 2 + 1 : Math.Max(0, current.Insertion) * 2;
        position = key switch
        {
            ChallengeHorizontalNavigation.Home => 0,
            ChallengeHorizontalNavigation.End => last,
            ChallengeHorizontalNavigation.Left => Math.Clamp(position - 1, 0, last),
            ChallengeHorizontalNavigation.Right => Math.Clamp(position + 1, 0, last),
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
        return position == last ? new(-1, -1, true)
            : position % 2 == 0 ? new(-1, position / 2, false) : new(position / 2, -1, false);
    }

    public static string CaseDisclosure(ChallengeCaseKind kind) => kind switch
    {
        ChallengeCaseKind.ColdStart => "Idle without Start: command off and carton at origin.",
        ChallengeCaseKind.Holding => "Start then release: remember the run request and continue traveling.",
        ChallengeCaseKind.BeamStop => "Photoeye feedback: inhibit the next sampled scan and hold the carton.",
        ChallengeCaseKind.ProcessStop => "Process Stop: ordinary input stops motion and remains stopped.",
        ChallengeCaseKind.SimultaneousStartStop => "Simultaneous Start and Stop: Stop takes priority; no motion.",
        ChallengeCaseKind.BlockedStart => "Start while beam blocked: stay stopped; no bypass.",
        ChallengeCaseKind.ResetRepeat => "Reset and repeat: clean controller/plant state, then stop at the beam again.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    private static string Map(IReadOnlyDictionary<string, bool> values)
        => string.Join("; ", values.Select(pair => $"{pair.Key}={(pair.Value ? "TRUE" : "FALSE")}"));
}
