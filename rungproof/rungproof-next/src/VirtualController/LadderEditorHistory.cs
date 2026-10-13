using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>
/// Exact, in-memory snapshot of the mutable offline editor document. This is
/// deliberately separate from the executable LadderProgram IR: editor history
/// must also preserve an invalid work-in-progress document and its stable IDs.
/// </summary>
public sealed record LadderEditorSnapshot(
    string Id,
    string Name,
    string SourceSceneId,
    TimeSpan ScanPeriod,
    IReadOnlyList<PlcVariable> Tags,
    IReadOnlyList<EditableBlock> Blocks,
    IReadOnlyList<EditableTask> Tasks,
    IReadOnlyList<string> WatchVariables,
    int ActiveBlockIndex,
    string EntryBlockId,
    int NextId, IReadOnlyList<LadderInstance>? Instances = null);

public sealed class LadderEditorHistory
{
    private sealed record Entry(string Description, LadderEditorSnapshot Snapshot);

    private readonly int _capacity;
    private readonly List<Entry> _undo = [];
    private readonly List<Entry> _redo = [];

    public LadderEditorHistory(int capacity = 100)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string UndoDescription => CanUndo ? _undo[^1].Description : string.Empty;
    public string RedoDescription => CanRedo ? _redo[^1].Description : string.Empty;

    public void Execute(LadderEditorDocument document, string description, Action mutation)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(mutation);
        Record(document, description);
        mutation();
    }

    public void Record(LadderEditorDocument document, string description)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("History description is required.", nameof(description));

        _undo.Add(new Entry(description.Trim(), document.CaptureSnapshot()));
        if (_undo.Count > _capacity) _undo.RemoveAt(0);
        _redo.Clear();
        document.InvalidateCommandTargets();
    }

    public bool Undo(LadderEditorDocument document, out string description)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!CanUndo)
        {
            description = string.Empty;
            return false;
        }

        var entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(new Entry(entry.Description, document.CaptureSnapshot()));
        document.RestoreSnapshot(entry.Snapshot);
        description = entry.Description;
        return true;
    }

    public bool Redo(LadderEditorDocument document, out string description)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!CanRedo)
        {
            description = string.Empty;
            return false;
        }

        var entry = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(new Entry(entry.Description, document.CaptureSnapshot()));
        document.RestoreSnapshot(entry.Snapshot);
        description = entry.Description;
        return true;
    }

    public LadderEditorHistory Copy()
    {
        // Entries contain detached document snapshots recorded before edits.
        var copy=new LadderEditorHistory(_capacity);
        copy._undo.AddRange(_undo);copy._redo.AddRange(_redo);
        return copy;
    }    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}

public sealed partial class LadderEditorDocument
{
    public LadderEditorSnapshot CaptureSnapshot() => new(
        Id,
        Name,
        SourceSceneId,
        ScanPeriod,
        Tags.ToArray(),
        Blocks.Select(CloneBlock).ToArray(),
        Tasks.Select(CloneTask).ToArray(),
        WatchVariables.ToArray(),
        ActiveBlockIndex,
        EntryBlockId,
        _nextId, Instances.ToArray());

    public void RestoreSnapshot(LadderEditorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        InvalidateCommandTargets();
        Id = snapshot.Id;
        Name = snapshot.Name;
        SourceSceneId = snapshot.SourceSceneId;
        ScanPeriod = snapshot.ScanPeriod;
        Tags.Clear();
        Tags.AddRange(snapshot.Tags);
        Instances.Clear(); Instances.AddRange(snapshot.Instances ?? []);
        Blocks.Clear();
        Blocks.AddRange(snapshot.Blocks.Select(CloneBlock));
        Tasks.Clear();
        Tasks.AddRange(snapshot.Tasks.Select(CloneTask));
        WatchVariables.Clear();
        WatchVariables.AddRange(snapshot.WatchVariables);
        ActiveBlockIndex = snapshot.ActiveBlockIndex;
        EntryBlockId = snapshot.EntryBlockId;
        _nextId = snapshot.NextId;
    }

    private static EditableBlock CloneBlock(EditableBlock source)
    {
        var block = new EditableBlock(source.Id, source.Name, source.BlockType) { SemanticMode = source.SemanticMode };
        block.Interface.AddRange(source.Interface);
        foreach (var sourceRung in source.Rungs)
        {
            var rung = new EditableRung(sourceRung.Id, sourceRung.Label, sourceRung.CoilVariable)
            {
                CoilMode = sourceRung.CoilMode,
                TimerVariable = sourceRung.TimerVariable,
                TimerPreset = sourceRung.TimerPreset,
                TimerKind = sourceRung.TimerKind,
                IsTimer = sourceRung.IsTimer,
                IsTimerReset = sourceRung.IsTimerReset,
                IsCounter = sourceRung.IsCounter,
                IsCounterReset = sourceRung.IsCounterReset,
                IsCounterLoad = sourceRung.IsCounterLoad,
                CounterKind = sourceRung.CounterKind,
                CounterVariable = sourceRung.CounterVariable,
                CounterPreset = sourceRung.CounterPreset,
                IsNumericOperation = sourceRung.IsNumericOperation,
                NumericOperationKind = sourceRung.NumericOperationKind,
                NumericSourceA = sourceRung.NumericSourceA,
                NumericSourceB = sourceRung.NumericSourceB,
                NumericSourceC = sourceRung.NumericSourceC,
                NumericDestination = sourceRung.NumericDestination,
                IsCall = sourceRung.IsCall,
                CallTarget = sourceRung.CallTarget,
                CallSemanticMode = sourceRung.CallSemanticMode, CallBindings = sourceRung.CallBindings.ToArray(), CallInstanceId = sourceRung.CallInstanceId,
                IsReturn = sourceRung.IsReturn,
                IsJump = sourceRung.IsJump,
                IsLabel = sourceRung.IsLabel,
                ProgramControlLabel = sourceRung.ProgramControlLabel,
            };
            rung.TerminalActionId = sourceRung.TerminalActionId;
            rung.HasTerminalAction = sourceRung.HasTerminalAction;
            rung.LegacyEmptyBranchSemantics = sourceRung.LegacyEmptyBranchSemantics;
            rung.SetRoot(EditableLadderTree.Import(EditableLadderTree.Export(sourceRung.Root)));
            block.Rungs.Add(rung);
        }
        return block;
    }

    private static EditableTask CloneTask(EditableTask source) => new(
        source.Id,
        source.Name,
        source.Kind,
        source.Period,
        source.Priority,
        source.EntryBlock);
}

