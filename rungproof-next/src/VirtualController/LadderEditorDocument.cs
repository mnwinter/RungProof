using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed record EditableContact(
    string Id,
    string Variable,
    bool NormallyClosed,
    bool IsComparison = false,
    LadderCompareOperator CompareOperator = LadderCompareOperator.Equal,
    string RightOperand = "",
    LadderEdgeMode EdgeMode = LadderEdgeMode.None,
    LadderComparisonValueType ComparisonValueType = LadderComparisonValueType.Unspecified)
{
    public string? EdgeMemoryOperand { get; init; }
}

public sealed class EditableBranch
{
    public string Id { get; }
    private EditableLadderNode _standalone;
    private Func<EditableLadderNode>? _read;
    private Action<EditableLadderNode>? _write;
    internal EditableLadderNode Node => _read?.Invoke() ?? _standalone;
    public EditorTreeList<EditableContact> Contacts { get; }
    public EditableBranch(string id)
    {
        Id = id; _standalone = new(id, LadderNodeKind.Series, Children: Array.Empty<EditableLadderNode>());
        Contacts = new(() => Node.IsLeaf ? [EditableLadderTree.ToContact(Node)]
            : (Node.Children ?? []).Where(n => n.IsLeaf).Select(EditableLadderTree.ToContact).ToList(), CommitContacts);
    }
    internal EditableBranch(string id, Func<EditableLadderNode> read, Action<EditableLadderNode> write) : this(id)
    { _read = read; _write = write; }
    internal void Attach(Func<EditableLadderNode> read, Action<EditableLadderNode> write) { _read = read; _write = write; }
    private void CommitContacts(List<EditableContact> contacts)
    {
        var current = Node;
        EditableLadderNode replacement;
        if (current.IsLeaf)
        {
            if (contacts.Count == 1 && contacts[0].Id == current.Id) replacement = EditableLadderTree.FromContact(contacts[0]);
            else throw new InvalidOperationException("A leaf-root edit requires a recursive subtree command.");
        }
        else if ((current.Children ?? []).Any(n => !n.IsLeaf))
        {
            var originalIds = (current.Children ?? []).Where(n => n.IsLeaf).Select(n => n.Id).ToArray();
            if (!contacts.Select(c => c.Id).SequenceEqual(originalIds))
                throw new InvalidOperationException("Nested structural edits require an exact subtree command.");
            var byId = contacts.ToDictionary(c => c.Id, EditableLadderTree.FromContact);
            replacement = current with { Children = Array.AsReadOnly((current.Children ?? []).Select(n => n.IsLeaf ? byId[n.Id] : n).ToArray()) };
        }
        else replacement = current with { Children = Array.AsReadOnly(contacts.Select(EditableLadderTree.FromContact).ToArray()) };
        if (_write is null) { EditableLadderTree.Validate(replacement); _standalone = replacement; }
        else _write(replacement);
    }
}

public sealed class EditableRung
{
    public string Id { get; }
    public string Label { get; set; }
    public string CoilVariable { get; set; }
    public LadderCoilMode CoilMode { get; set; } = LadderCoilMode.Assign;
    public string TimerVariable { get; set; } = string.Empty;
    public TimeSpan TimerPreset { get; set; } = TimeSpan.FromSeconds(1);
    public bool TimerPresetAssigned { get; set; } = true;
    public string TimerPresetOperand { get; set; } = "T#1000ms";
    public string TimerResetOperand { get; set; } = "FALSE";
    public LadderTimerKind TimerKind { get; set; } = LadderTimerKind.OnDelay;
    public LadderInstanceDataBlock? TimerInstanceDataBlock { get; set; }
    public bool IsTimer { get; set; }
    public bool IsTimerReset { get; set; }
    public bool IsCounter { get; set; }
    public bool IsCounterReset { get; set; }
    public bool IsCounterLoad { get; set; }
    public LadderCounterKind CounterKind { get; set; } = LadderCounterKind.CountUp;
    public string CounterVariable { get; set; } = string.Empty;
    public long CounterPreset { get; set; } = 10;
    public LadderCounterValueType CounterValueType { get; set; } = LadderCounterValueType.Int;
    public LadderInstanceDataBlock? CounterInstanceDataBlock { get; set; }
    public string CounterLoadInput { get; set; } = "FALSE";
    public string CounterResetInput { get; set; } = "FALSE";
    public bool IsNumericOperation { get; set; }
    public LadderNumericOperationKind NumericOperationKind { get; set; } = LadderNumericOperationKind.Move;
    public string NumericSourceA { get; set; } = "0";
    public string NumericSourceB { get; set; } = "0";
    public string NumericSourceC { get; set; } = "0";
    public string NumericDestination { get; set; } = string.Empty;
    public LadderNumericDataType NumericDataType { get; set; } = LadderNumericDataType.Unspecified;
    public LadderNumericDataType NumericInputDataType { get; set; } = LadderNumericDataType.Unspecified;
    public LadderNumericDataType NumericOutputDataType { get; set; } = LadderNumericDataType.Unspecified;
    public bool IsCall { get; set; }
    public string CallTarget { get; set; } = string.Empty;
    public LadderSemanticMode CallSemanticMode { get; set; }
    public IReadOnlyList<LadderCallBinding> CallBindings { get; set; } = [];
    public string CallInstanceId { get; set; } = string.Empty;
    public bool IsReturn { get; set; }
    public LadderReturnValueKind ReturnValueKind { get; set; } = LadderReturnValueKind.Rlo;
    public string ReturnValueOperand { get; set; } = string.Empty;
    public bool IsJump { get; set; }
    public bool IsLabel { get; set; }
    // LABEL is network metadata in TIA Portal, not the network's terminal
    // action. Keep its identity/name separate so a destination network can
    // still contain a coil, box, call, or program-control output.
    public string NetworkLabelId { get; set; } = string.Empty;
    public string NetworkLabelName { get; set; } = string.Empty;
    public string ProgramControlLabel { get; set; } = string.Empty;
    public EditableLadderNode Root { get; private set; }
    public string TerminalActionId { get; set; } = string.Empty;
    public bool HasTerminalAction { get; set; } = true;
    // TIA lets a user place an empty instruction box in LAD before selecting
    // the actual instruction. It is an authoring placeholder, never executable.
    public bool HasEmptyInstructionPlaceholder { get; set; }
    public bool LegacyEmptyBranchSemantics { get; set; }
    public EditorTreeList<EditableBranch> Branches { get; }
    public bool IsFlat => Root.Kind == LadderNodeKind.Series && (Root.Children ?? []).All(n => n.IsLeaf)
        || Root.Kind == LadderNodeKind.Parallel && (Root.Children ?? []).All(n => n.Kind == LadderNodeKind.Series && (n.Children ?? []).All(c => c.IsLeaf));

    public EditableRung(string id, string label, string coilVariable)
    {
        Id = id;
        Label = label;
        CoilVariable = coilVariable;
        Root = new(id + "-logic", LadderNodeKind.Parallel, Children: Array.Empty<EditableLadderNode>());
        Branches = new(ReadBranches, CommitBranches);
    }
    public void SetRoot(EditableLadderNode root)
    {
        EditableLadderTree.Validate(root);
        // Detach caller-owned child lists; an accepted tree cannot change behind history/layout.
        Root = EditableLadderTree.Import(EditableLadderTree.Export(root));
    }
    private List<EditableBranch> ReadBranches()
    {
        IEnumerable<EditableLadderNode> nodes = IsFlat
            ? Root.Kind == LadderNodeKind.Series ? [Root] : Root.Children ?? []
            : EditableLadderTree.Visit(Root).Where(n => n.IsLeaf && !EditableLadderTree.Visit(Root).Any(p => p.Kind == LadderNodeKind.Series && p.Children?.Any(c => c.Id == n.Id) == true)
                || n.Kind == LadderNodeKind.Series);
        return nodes.Select(n => new EditableBranch(n.Id,
            () => EditableLadderTree.Find(Root, n.Id) ?? throw new InvalidOperationException("Branch target was removed."),
            replacement => SetRoot(EditableLadderTree.Replace(Root, n.Id, replacement)))).ToList();
    }
    private void CommitBranches(List<EditableBranch> branches)
    {
        if (!IsFlat) throw new InvalidOperationException("Nested branch edits require an exact subtree command.");
        var nodes = branches.Select(b => b.Node).ToArray();
        SetRoot(nodes.Length == 1 ? nodes[0] : new(Root.Kind == LadderNodeKind.Parallel ? Root.Id : Id + "-parallel",
            LadderNodeKind.Parallel, Children: Array.AsReadOnly(nodes)));
        foreach (var branch in branches) branch.Attach(
            () => EditableLadderTree.Find(Root, branch.Id) ?? throw new InvalidOperationException("Branch target was removed."),
            replacement => SetRoot(EditableLadderTree.Replace(Root, branch.Id, replacement)));
    }
}

public sealed class EditableBlock
{
    public string Id { get; }
    public string Name { get; set; }
    public LadderBlockType BlockType { get; set; }
    public LadderSemanticMode SemanticMode { get; set; }
    public List<LadderInterfaceParameter> Interface { get; } = [];
    public List<EditableRung> Rungs { get; } = [];

    public EditableBlock(string id, string name, LadderBlockType blockType = LadderBlockType.Function)
    {
        Id = id;
        Name = name;
        BlockType = blockType;
    }
}

public sealed class EditableTask
{
    public string Id { get; }
    public string Name { get; set; }
    public LadderTaskKind Kind { get; set; }
    public TimeSpan Period { get; set; }
    public int Priority { get; set; }
    public string EntryBlock { get; set; }

    public EditableTask(string id, string name, LadderTaskKind kind, TimeSpan period, int priority, string entryBlock)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Period = period;
        Priority = priority;
        EntryBlock = entryBlock;
    }
}

public sealed class EditableWatchTable
{
    private const string DefaultId = "default-watch-table-1";
    public string Id { get; }
    public string Name { get; set; }
    public List<EditableWatchRow> Rows { get; } = [];
    public EditableWatchVariableList Variables { get; }

    public EditableWatchTable(string name, IEnumerable<string>? variables = null, string? id = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
        Name = name;
        Variables = new EditableWatchVariableList(this);
        if (variables is not null) Variables.AddRange(variables);
    }

    public static EditableWatchTable CreateDefault(IEnumerable<string>? variables = null) =>
        new("Watch table 1", variables, DefaultId);

    internal void ValidateRows()
        => ValidateRows(null);

    internal void ValidateRows(IReadOnlyList<PlcVariable>? variables)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in Rows)
        {
            if (row is null || string.IsNullOrWhiteSpace(row.Id)
                || string.IsNullOrWhiteSpace(row.Reference)
                || string.IsNullOrWhiteSpace(row.DisplayFormat)
                || !Enum.IsDefined(row.ReferenceKind))
                throw new ArgumentException($"Watch table '{Name}' contains a row with missing or invalid required fields.");
            if (!ids.Add(row.Id))
                throw new ArgumentException($"Watch table '{Name}' contains duplicate row identity '{row.Id}'.");
            var reference = row.Reference.Trim();
            var key = row.ReferenceKind == WatchReferenceKind.AbsoluteAddress
                ? "address:" + (SiemensPlcTagAddress.TryGetWatchAddressIdentity(reference, out var addressIdentity)
                    ? addressIdentity
                    : reference.TrimStart('%').ToUpperInvariant())
                : "symbol:" + reference;
            if (!references.Add(key))
                throw new ArgumentException($"Watch table '{Name}' contains duplicate reference '{row.Reference}'.");
            if (row.ReferenceKind == WatchReferenceKind.AbsoluteAddress
                && !SiemensPlcTagAddress.TryValidateWatchAddress(reference, out var error))
                throw new ArgumentException($"Watch table '{Name}' contains invalid absolute address: {error}");
            if (variables is not null)
            {
                var variable = row.ReferenceKind == WatchReferenceKind.Symbolic
                    ? variables.FirstOrDefault(tag => tag.Name.Equals(reference, StringComparison.Ordinal))
                    : variables.FirstOrDefault(tag => SiemensPlcTagAddress.MatchesWatchTagAddress(reference, tag));
                if (!WatchTableDisplayFormat.IsValid(variable?.Type, row.DisplayFormat))
                    throw new ArgumentException(variable is null
                        ? $"Watch table '{Name}' row '{row.Id}' uses a display format other than Auto for an unresolved reference."
                        : $"Watch table '{Name}' row '{row.Id}' uses display format '{row.DisplayFormat}' which is not valid for {variable.Type.ToString().ToUpperInvariant()}.");
            }
        }
    }
}

/// <summary>
/// Mutable editor document for the offline Ladder workbench. The execution
/// runtime remains immutable: BuildProgram creates and validates a fresh IR
/// snapshot before a program can be loaded or scanned.
/// </summary>
public sealed partial class LadderEditorDocument
{
    private int _nextId = 1;
    public string Id { get; set; } = "offline-controller";
    public string Name { get; set; } = "Main";
    public string SourceSceneId { get; set; } = string.Empty;
    public TimeSpan ScanPeriod { get; set; } = TimeSpan.FromMilliseconds(20);
    public List<PlcVariable> Tags { get; } = [];
    public List<PlcUserConstant> UserConstants { get; } = [];
    public List<string> TagTables { get; } = ["Default tag table"];
    public List<LadderInstance> Instances { get; } = [];
    public List<EditableBlock> Blocks { get; } = [];
    public List<EditableTask> Tasks { get; } = [];
    public List<EditableWatchTable> WatchTables { get; } = [new("Watch table 1")];
    public int ActiveWatchTableIndex { get; private set; }
    public EditableWatchTable ActiveWatchTable => WatchTables[Math.Clamp(ActiveWatchTableIndex, 0, WatchTables.Count - 1)];
    // Compatibility surface for existing single-table projects and callers.
    public EditableWatchVariableList WatchVariables => ActiveWatchTable.Variables;
    public IEnumerable<string> AllWatchVariables => WatchTables.SelectMany(table => table.Variables).Distinct(StringComparer.Ordinal);
    public int ActiveBlockIndex { get; private set; }
    public string EntryBlockId { get; set; } = string.Empty;
    public List<EditableRung> Rungs => EnsureActiveBlock().Rungs;

    /// <summary>
    /// Resets every project-owned authoring field. Runtime state is deliberately
    /// outside this document and is invalidated by the shell after this call.
    /// </summary>
    public void ResetProject(string id, string name, TimeSpan scanPeriod)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Project ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Project name cannot be empty.", nameof(name));
        if (scanPeriod <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(scanPeriod));
        Id = id.Trim();
        Name = name.Trim();
        SourceSceneId = string.Empty;
        ScanPeriod = scanPeriod;
        Tags.Clear();
        UserConstants.Clear();
        TagTables.Clear();
        TagTables.Add("Default tag table");
        Instances.Clear();
        Blocks.Clear();
        Tasks.Clear();
        WatchTables.Clear();
        WatchTables.Add(EditableWatchTable.CreateDefault());
        ActiveWatchTableIndex = 0;
        ActiveBlockIndex = 0;
        EntryBlockId = string.Empty;
        _nextId = 1;
        AddBlock(Name);
        InvalidateCommandTargets();
    }

    public EditableWatchTable AddWatchTable(string name)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0) throw new InvalidOperationException("Watch table name cannot be empty.");
        if (WatchTables.Any(table => table.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Watch table name '{normalized}' already exists.");
        var table = new EditableWatchTable(normalized);
        WatchTables.Add(table);
        ActiveWatchTableIndex = WatchTables.Count - 1;
        return table;
    }

    public void SelectWatchTable(int index)
    {
        if (index < 0 || index >= WatchTables.Count) throw new ArgumentOutOfRangeException(nameof(index));
        ActiveWatchTableIndex = index;
    }

    public bool RemoveActiveWatchTable()
    {
        if (WatchTables.Count <= 1) return false;
        WatchTables.RemoveAt(ActiveWatchTableIndex);
        ActiveWatchTableIndex = Math.Clamp(ActiveWatchTableIndex, 0, WatchTables.Count - 1);
        return true;
    }

    public void RenameActiveWatchTable(string name)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0) throw new InvalidOperationException("Watch table name cannot be empty.");
        if (WatchTables.Where((_, index) => index != ActiveWatchTableIndex)
            .Any(table => table.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Watch table name '{normalized}' already exists.");
        ActiveWatchTable.Name = normalized;
    }

    public EditableWatchTable DuplicateActiveWatchTable()
    {
        var source = ActiveWatchTable;
        var baseName = source.Name + " Copy";
        var name = baseName;
        for (var suffix = 2; WatchTables.Any(table => table.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); suffix++)
            name = $"{baseName} {suffix}";
        var copy = new EditableWatchTable(name);
        copy.Rows.AddRange(source.Rows.Select(row => row.Duplicate()));
        WatchTables.Insert(ActiveWatchTableIndex + 1, copy);
        ActiveWatchTableIndex++;
        return copy;
    }

    public bool MoveActiveWatchTable(int offset)
    {
        if (offset is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(offset));
        var destination = ActiveWatchTableIndex + offset;
        if (destination < 0 || destination >= WatchTables.Count) return false;
        (WatchTables[ActiveWatchTableIndex], WatchTables[destination]) =
            (WatchTables[destination], WatchTables[ActiveWatchTableIndex]);
        ActiveWatchTableIndex = destination;
        return true;
    }

    public EditableBlock AddBlock(string name, LadderBlockType? blockType = null)
    {
        var normalizedName = ValidateUniqueName(name, Blocks.Select(block => block.Name), "Block/routine",
            StringComparison.OrdinalIgnoreCase);
        if (Tags.Any(tag => tag.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Block/routine name '{normalizedName}' conflicts with a PLC tag.");
        if (UserConstants.Any(constant => constant.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Block/routine name '{normalizedName}' conflicts with a PLC user constant.");
        var type = blockType ?? (Blocks.Count == 0 ? LadderBlockType.OrganizationBlock : LadderBlockType.Function);
        var block = new EditableBlock(NewId("block"), normalizedName, type);
        Blocks.Add(block);
        if (Blocks.Count == 1)
        {
            ActiveBlockIndex = 0;
            EntryBlockId = block.Id;
            if (Tasks.Count == 0)
                Tasks.Add(new EditableTask(NewId("task"), "MainTask", LadderTaskKind.Continuous, ScanPeriod, 10, block.Id));
        }
        return block;
    }

    public LadderInterfaceParameter AddInterfaceParameter(
        string blockId,
        string name,
        PlcVariableType type,
        LadderInterfaceSection section,
        object? initialValue = null,
        string dataType = "",
        bool retain = false,
        string comment = "")
    {
        var block = Blocks.FirstOrDefault(candidate => candidate.Id.Equals(blockId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Block '{blockId}' does not exist.");
        if (block.BlockType == LadderBlockType.DataBlock && section != LadderInterfaceSection.Static)
            throw new InvalidOperationException("Global DB declarations use static storage; Input, Output, InOut, and Temp are code-block interface sections.");
        var normalizedName = ValidateUniqueName(
            name,
            block.Interface.Select(parameter => parameter.Name),
            "Interface parameter",
            block.BlockType == LadderBlockType.DataBlock ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        var value = initialValue ?? type switch
        {
            PlcVariableType.Bool => false,
            PlcVariableType.Real => 0.0,
        PlcVariableType.String => string.Empty,
        PlcVariableType.Enum => "Stopped",
            PlcVariableType.Timer => TimeSpan.Zero,
            _ => 0L,
        };
        var parameter = new LadderInterfaceParameter(normalizedName, type, section, value, dataType,
            Id: "formal-" + Guid.NewGuid().ToString("N"), Retain: retain, Comment: comment.Trim());
        block.Interface.Add(parameter);
        return parameter;
    }

    public bool RemoveInterfaceParameter(string blockId, int index)
    {
        var block = Blocks.FirstOrDefault(candidate => candidate.Id.Equals(blockId, StringComparison.Ordinal));
        if (block is null || index < 0 || index >= block.Interface.Count) return false;
        block.Interface.RemoveAt(index);
        return true;
    }

    public EditableBlock RenameBlock(string blockId, string name)
    {
        var block = Blocks.FirstOrDefault(candidate => candidate.Id.Equals(blockId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Block/routine '{blockId}' does not exist.");
        var normalizedName = ValidateUniqueName(
            name,
            Blocks.Where(candidate => !ReferenceEquals(candidate, block)).Select(candidate => candidate.Name),
            "Block/routine",
            StringComparison.OrdinalIgnoreCase);
        if (Tags.Any(tag => tag.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Block/routine name '{normalizedName}' conflicts with a PLC tag.");
        if (UserConstants.Any(constant => constant.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Block/routine name '{normalizedName}' conflicts with a PLC user constant.");
        block.Name = normalizedName;
        return block;
    }

    public bool CanRemoveBlock(string blockId, out string reason)
    {
        var index = Blocks.FindIndex(block => block.Id.Equals(blockId, StringComparison.Ordinal));
        if (index < 0)
        {
            reason = $"Block/routine '{blockId}' does not exist.";
            return false;
        }
        var block = Blocks[index];
        if (Blocks.Count == 1)
        {
            reason = "The controller must retain at least one block/routine.";
            return false;
        }
        if (EntryBlockId.Equals(blockId, StringComparison.Ordinal))
        {
            reason = $"'{block.Name}' is the controller entry block and cannot be removed.";
            return false;
        }
        var owningTasks = Tasks.Where(task => task.EntryBlock.Equals(blockId, StringComparison.Ordinal))
            .Select(task => task.Name).ToArray();
        if (owningTasks.Length > 0)
        {
            reason = $"'{block.Name}' is assigned to {string.Join(", ", owningTasks)}. Reassign those tasks/OBs first.";
            return false;
        }
        var callers = Blocks.SelectMany(owner => owner.Rungs
                .Where(rung => rung.IsCall && rung.CallTarget.Equals(blockId, StringComparison.Ordinal))
                .Select(rung => $"{owner.Name}/{rung.Label}"))
            .ToArray();
        if (callers.Length > 0)
        {
            reason = $"'{block.Name}' is referenced by CALL/JSR at {string.Join(", ", callers)}. Remove those calls first.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryRemoveBlock(string blockId, out string reason)
    {
        if (!CanRemoveBlock(blockId, out reason)) return false;
        var index = Blocks.FindIndex(block => block.Id.Equals(blockId, StringComparison.Ordinal));
        Blocks.RemoveAt(index);
        if (ActiveBlockIndex > index) ActiveBlockIndex--;
        else if (ActiveBlockIndex == index) ActiveBlockIndex = Math.Min(index, Blocks.Count - 1);
        return true;
    }

    public EditableTask AddTask(
        string name,
        LadderTaskKind kind,
        TimeSpan period,
        int priority,
        string entryBlock)
    {
        var normalizedName = ValidateUniqueName(name, Tasks.Select(task => task.Name), "Task/OB");
        var targetBlock = Blocks.FirstOrDefault(block => block.Id.Equals(entryBlock, StringComparison.Ordinal));
        if (targetBlock is null)
            throw new InvalidOperationException($"Task/OB entry block '{entryBlock}' does not exist.");
        if (targetBlock.BlockType == LadderBlockType.DataBlock)
            throw new InvalidOperationException($"Data block '{targetBlock.Name}' contains declarations and cannot be a task/OB entry.");
        var task = new EditableTask(NewId("task"), normalizedName, kind, period, priority, entryBlock);
        Tasks.Add(task);
        return task;
    }

    public EditableTask RenameTask(string taskId, string name)
    {
        var task = Tasks.FirstOrDefault(candidate => candidate.Id.Equals(taskId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Task/OB '{taskId}' does not exist.");
        task.Name = ValidateUniqueName(
            name,
            Tasks.Where(candidate => !ReferenceEquals(candidate, task)).Select(candidate => candidate.Name),
            "Task/OB");
        return task;
    }

    public bool CanRemoveTask(string taskId, out string reason)
    {
        var index = Tasks.FindIndex(task => task.Id.Equals(taskId, StringComparison.Ordinal));
        if (index < 0)
        {
            reason = $"Task/OB '{taskId}' does not exist.";
            return false;
        }
        if (Tasks.Count == 1)
        {
            reason = "The controller must retain at least one task/OB schedule.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    public bool TryRemoveTask(string taskId, out string reason)
    {
        if (!CanRemoveTask(taskId, out reason)) return false;
        var index = Tasks.FindIndex(task => task.Id.Equals(taskId, StringComparison.Ordinal));
        Tasks.RemoveAt(index);
        return true;
    }

    public void SelectBlock(int index)
    {
        if (index < 0 || index >= Blocks.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (ActiveBlockIndex != index) InvalidateCommandTargets(contentChanged: false);
        ActiveBlockIndex = index;
    }

    public string AddTagTable(string name)
    {
        var normalizedName = ValidateUniqueName(name, TagTables, "Tag table", StringComparison.OrdinalIgnoreCase);
        TagTables.Add(normalizedName);
        return normalizedName;
    }

    /// <summary>Checks the persisted PLC tag-table declaration list. A null list is a legacy project with no explicit table list.</summary>
    public static bool TryValidateTagTableNames(IEnumerable<string?>? names, out string error)
    {
        error = string.Empty;
        if (names is null) return true;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "PLC tag-table names must not be null, empty, or whitespace.";
                return false;
            }
            var normalized = name.Trim();
            if (!string.Equals(name, normalized, StringComparison.Ordinal))
            {
                error = $"PLC tag-table name '{name}' has leading or trailing whitespace.";
                return false;
            }
            if (!seen.Add(normalized))
            {
                error = $"PLC tag-table name '{normalized}' is duplicated; names must be unique without regard to case.";
                return false;
            }
        }
        if (seen.Count > 0 && !seen.Contains("Default tag table"))
        {
            error = "The built-in Default tag table must be present in every explicit PLC tag-table list.";
            return false;
        }
        return true;
    }

    /// <summary>Renames an existing PLC tag table and updates each member tag atomically.</summary>
    public string RenameTagTable(string currentName, string newName)
    {
        var normalizedCurrentName = currentName?.Trim();
        var index = TagTables.FindIndex(name => name.Equals(normalizedCurrentName, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new InvalidOperationException($"PLC tag table '{currentName}' does not exist.");
        if (TagTables[index].Equals("Default tag table", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The default PLC tag table cannot be renamed.");
        var normalized = newName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException("PLC tag-table name cannot be empty.");
        if (TagTables.Where((_, tableIndex) => tableIndex != index)
            .Any(name => name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"PLC tag table '{normalized}' already exists.");

        var oldName = TagTables[index];
        TagTables[index] = normalized;
        for (var tagIndex = 0; tagIndex < Tags.Count; tagIndex++)
            if (Tags[tagIndex].TagTable.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                Tags[tagIndex] = Tags[tagIndex] with { TagTable = normalized };
        for (var constantIndex = 0; constantIndex < UserConstants.Count; constantIndex++)
            if (UserConstants[constantIndex].TagTable.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                UserConstants[constantIndex] = UserConstants[constantIndex] with { TagTable = normalized };
        return normalized;
    }

    /// <summary>Removes an empty non-default PLC tag table. Failed attempts never mutate the document.</summary>
    public bool TryRemoveTagTable(string name, out string reason)
    {
        var normalizedName = name?.Trim();
        var index = TagTables.FindIndex(table => table.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            reason = $"PLC tag table '{name}' does not exist.";
            return false;
        }
        var existingName = TagTables[index];
        if (existingName.Equals("Default tag table", StringComparison.OrdinalIgnoreCase))
        {
            reason = "The default PLC tag table cannot be deleted.";
            return false;
        }
        if (Tags.Any(tag => tag.TagTable.Equals(existingName, StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"PLC tag table '{existingName}' is not empty.";
            return false;
        }
        if (UserConstants.Any(constant => constant.TagTable.Equals(existingName, StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"PLC tag table '{existingName}' is not empty.";
            return false;
        }
        TagTables.RemoveAt(index);
        reason = string.Empty;
        return true;
    }

    public PlcUserConstant AddUserConstant(
        string name,
        PlcVariableType type,
        string value,
        string tagTable = "Default tag table",
        string comment = "")
    {
        var normalizedName = ValidateCpuConstantName(name);
        if (!IsSupportedUserConstantType(type))
            throw new InvalidOperationException("User constants support BOOL, INT, DINT, REAL, TIME, and STRING in this offline subset.");
        if (!TryParseInitialValue(type, value, out _, out var error))
            throw new InvalidOperationException(error);
        var canonicalTable = EnsureTagTable(tagTable);
        var constant = new PlcUserConstant(Guid.NewGuid().ToString("N"), normalizedName, type, value, canonicalTable, comment.Trim());
        UserConstants.Add(constant);
        return constant;
    }

    public PlcUserConstant UpdateUserConstant(
        string constantId,
        string name,
        PlcVariableType type,
        string value,
        string comment,
        string? tagTable = null)
    {
        var index = UserConstants.FindIndex(constant => constant.Id.Equals(constantId, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException($"PLC user constant '{constantId}' does not exist.");
        var current = UserConstants[index];
        var normalizedName = ValidateCpuConstantName(name, constantId);
        if (!IsSupportedUserConstantType(type))
            throw new InvalidOperationException("User constants support BOOL, INT, DINT, REAL, TIME, and STRING in this offline subset.");
        if (!TryParseInitialValue(type, value, out _, out var error))
            throw new InvalidOperationException(error);
        var requestedTable = string.IsNullOrWhiteSpace(tagTable) ? current.TagTable : tagTable.Trim();
        var canonicalTable = TagTables.FirstOrDefault(table =>
            table.Equals(requestedTable, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"PLC tag table '{requestedTable}' does not exist.");
        var updated = current with
        {
            Name = normalizedName,
            Type = type,
            Value = value,
            TagTable = canonicalTable,
            Comment = comment.Trim(),
        };
        UserConstants[index] = updated;
        return updated;
    }

    public bool RemoveUserConstant(string constantId)
    {
        var index = UserConstants.FindIndex(constant => constant.Id.Equals(constantId, StringComparison.Ordinal));
        if (index < 0) return false;
        UserConstants.RemoveAt(index);
        return true;
    }

    public static bool IsSupportedUserConstantType(PlcVariableType type) => type is
        PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real
            or PlcVariableType.Time or PlcVariableType.String;

    public static bool TryValidateUserConstants(
        IEnumerable<PlcUserConstant?>? constants,
        IEnumerable<PlcVariable>? tags,
        IEnumerable<EditableBlock>? blocks,
        IEnumerable<string?>? tagTables,
        out string error)
    {
        error = string.Empty;
        var items = (constants ?? []).ToArray();
        var tableNames = new HashSet<string>((tagTables ?? []).Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim()), StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var reserved = new HashSet<string>((tags ?? []).Select(tag => tag.Name), StringComparer.OrdinalIgnoreCase);
        reserved.UnionWith((blocks ?? []).Select(block => block.Name));
        foreach (var constant in items)
        {
            if (constant is null)
            {
                error = "PLC user constants must not contain null entries.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(constant.Id) || !ids.Add(constant.Id))
            {
                error = "PLC user-constant IDs must be nonempty and unique.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(constant.Name) || constant.Name != constant.Name.Trim()
                || constant.Name.Contains('"'))
            {
                error = "PLC user-constant names must be nonempty, trimmed, and must not contain quotation marks.";
                return false;
            }
            if (!names.Add(constant.Name) || !reserved.Add(constant.Name))
            {
                error = $"PLC user-constant name '{constant.Name}' conflicts with a PLC tag, block, or another constant; names are case-insensitive and CPU-wide.";
                return false;
            }
            if (!tableNames.Contains(constant.TagTable))
            {
                error = $"PLC user constant '{constant.Name}' references missing tag table '{constant.TagTable}'.";
                return false;
            }
            if (!IsSupportedUserConstantType(constant.Type))
            {
                error = $"PLC user constant '{constant.Name}' uses an unsupported type '{constant.Type}'.";
                return false;
            }
            if (!TryParseInitialValue(constant.Type, constant.Value, out _, out var valueError))
            {
                error = $"PLC user constant '{constant.Name}' has an invalid value: {valueError}";
                return false;
            }
        }
        return true;
    }

    private string ValidateCpuConstantName(string name, string? excludedConstantId = null)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0 || normalized.Contains('"'))
            throw new InvalidOperationException("PLC user-constant name cannot be empty or contain quotation marks.");
        // TIA Portal keeps a duplicate declaration and appends a sequential
        // suffix. Apply that rule across the CPU-wide namespace (tags, blocks,
        // and user constants) rather than rejecting the declaration.
        var requestedName = normalized;
        var suffix = 0;
        while (CpuNameIsAssigned(normalized, excludedConstantId))
            normalized = $"{requestedName}({++suffix})";
        return normalized;
    }

    private bool CpuNameIsAssigned(string name, string? excludedConstantId) =>
        Tags.Any(tag => tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        || Blocks.Any(block => block.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        || UserConstants.Any(constant => constant.Id != excludedConstantId
            && constant.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Moves a tag by its stable global identity (or its legacy name when it has no ID).</summary>
    public PlcVariable MoveTag(string tagIdentity, string targetTableName)
    {
        var matchingTags = Tags.Select((tag, index) => (tag, index))
            .Where(entry => string.Equals(LadderCallBindings.GlobalId(entry.tag), tagIdentity, StringComparison.Ordinal))
            .ToArray();
        if (matchingTags.Length != 1)
            throw new InvalidOperationException(matchingTags.Length == 0
                ? $"PLC tag identity '{tagIdentity}' does not exist."
                : $"PLC tag identity '{tagIdentity}' is ambiguous.");
        var normalizedTableName = targetTableName?.Trim();
        var tableName = TagTables.FirstOrDefault(table => table.Equals(normalizedTableName, StringComparison.OrdinalIgnoreCase));
        if (tableName is null) throw new InvalidOperationException($"PLC tag table '{targetTableName}' does not exist.");
        var (tag, index) = matchingTags[0];
        var moved = tag with { TagTable = tableName };
        Tags[index] = moved;
        return moved;
    }

    public PlcVariable AddAggregateTag(string name, PlcVariableRole role, PlcVariableType type, PlcAggregateSchema schema, object initialValue, string binding = "", string tagTable = "Default tag table", string address = "", string comment = "")
    {
        if (!PlcAggregates.IsAggregate(type)) throw new ArgumentException("Aggregate tag requires STRUCT or ARRAY.");
        var normalizedName = ValidateCpuSymbolName(name);
        var canonicalTagTable = EnsureTagTable(tagTable);
        var tag = new PlcVariable(normalizedName, type, role, initialValue, binding.Trim(), schema, TagTable: canonicalTagTable,
            Address: address.Trim(), Comment: comment.Trim());
        PlcAggregates.Expand(Tags.Append(tag).ToArray());
        Tags.Add(tag); return tag;
    }

    public PlcVariable AddTag(
        string name,
        PlcVariableRole role,
        string binding = "",
        PlcVariableType type = PlcVariableType.Bool,
        object? initialValue = null,
        IReadOnlyList<string>? enumMembers = null,
        string tagTable = "Default tag table",
        string address = "",
        string comment = "")
    {
        var members = type == PlcVariableType.Enum ? enumMembers ?? PlcTextValues.MotorStates : null;
        if (type == PlcVariableType.Enum && !PlcTextValues.IsValidDomain(members))
            throw new InvalidOperationException("ENUM members must be 1–32 distinct identifiers.");
        if (members is not null) members = Array.AsReadOnly(members.ToArray());
        var normalizedInitialValue = initialValue ?? (type == PlcVariableType.Enum ? members![0] : InitialValue(type));
        ValidateInitialValue(type, normalizedInitialValue);
        var normalizedName = ValidateCpuSymbolName(name);
        var canonicalTagTable = EnsureTagTable(tagTable);
        var tag = new PlcVariable(normalizedName, type, role, normalizedInitialValue, binding.Trim(), EnumMembers: members,
            TagTable: canonicalTagTable, Address: address.Trim(), Comment: comment.Trim());
        if (PlcTextValues.IsText(type) && !PlcTextValues.IsValid(tag, normalizedInitialValue))
            throw new InvalidOperationException("Invalid STRING value or ENUM domain/member.");
        Tags.Add(tag);
        return tag;
    }

    private string EnsureTagTable(string tagTable)
    {
        if (string.IsNullOrWhiteSpace(tagTable)) throw new InvalidOperationException("A tag table name is required.");
        var existingTable = TagTables.FirstOrDefault(existing => existing.Equals(tagTable, StringComparison.OrdinalIgnoreCase));
        return existingTable ?? AddTagTable(tagTable);
    }

    public PlcVariable UpdateTag(
        string currentName,
        string newName,
        PlcVariableType type,
        PlcVariableRole role,
        string binding = "",
        object? initialValue = null,
        PlcAggregateSchema? aggregate = null,
        IReadOnlyList<string>? enumMembers = null,
        string? address = null,
        string? comment = null)
    {
        var index = Tags.FindIndex(tag => tag.Name.Equals(currentName, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException($"Tag '{currentName}' does not exist.");
        var normalizedName = ValidateCpuSymbolName(newName, index);
        if (type is PlcVariableType.Timer or PlcVariableType.Counter)
        {
            role = PlcVariableRole.Memory;
            binding = string.Empty;
        }
        var current = Tags[index];
        var normalizedInitialValue = initialValue ?? (current.Type == type ? current.InitialValue : InitialValue(type));
        var updatedSchema = PlcAggregates.IsAggregate(type) ? aggregate ?? (current.Type == type ? current.Aggregate : null) : null;
        if (PlcAggregates.IsAggregate(type)) PlcAggregates.Expand([current with { Name = normalizedName, Type = type, InitialValue = normalizedInitialValue, Aggregate = updatedSchema }]);
        else ValidateInitialValue(type, normalizedInitialValue);
        var members = type == PlcVariableType.Enum ? enumMembers ?? (current.Type == type ? current.EnumMembers : PlcTextValues.MotorStates) : null;
        if (members is not null) members = Array.AsReadOnly(members.ToArray());
        var updated = new PlcVariable(normalizedName, type, role, normalizedInitialValue, binding.Trim(), updatedSchema, members,
            current.Id, current.TagTable, address?.Trim() ?? current.Address, comment?.Trim() ?? current.Comment);
        if (PlcTextValues.IsText(type) && !PlcTextValues.IsValid(updated, normalizedInitialValue))
            throw new InvalidOperationException("Invalid STRING value or ENUM domain/member.");
        if (PlcAggregates.IsAggregate(type) || Tags.Any(tag => PlcAggregates.IsAggregate(tag.Type)))
            PlcAggregates.Expand(Tags.Where((_, tagIndex) => tagIndex != index).Append(updated).ToArray());
        if (!current.Name.Equals(normalizedName, StringComparison.Ordinal))
        {
            RenameTagReferences(current, updated);
            foreach (var table in WatchTables)
            for (var watchIndex = 0; watchIndex < table.Variables.Count; watchIndex++)
                if (table.Variables[watchIndex].Equals(current.Name, StringComparison.Ordinal)
                    || table.Variables[watchIndex].StartsWith(current.Name + ".", StringComparison.Ordinal)
                    || table.Variables[watchIndex].StartsWith(current.Name + "[", StringComparison.Ordinal))
                    table.Variables[watchIndex] = normalizedName + table.Variables[watchIndex][current.Name.Length..];
        }
        Tags[index] = updated;
        return updated;
    }

    public int CountTagReferences(string name)
    {
        var count = 0;
        var referencedTag = Tags.FirstOrDefault(tag => tag.Name.Equals(name, StringComparison.Ordinal));
        bool Matches(string operand) => operand.Equals(name, StringComparison.Ordinal)
            || operand.StartsWith(name + ".", StringComparison.Ordinal)
            || operand.StartsWith(name + "[", StringComparison.Ordinal);
        foreach (var block in Blocks)
        foreach (var rung in block.Rungs)
        {
            foreach (var contact in rung.Branches.SelectMany(branch => branch.Contacts))
            {
                if (Matches(contact.Variable)) count++;
                if (contact.IsComparison && Matches(contact.RightOperand)) count++;
                if (contact.EdgeMemoryOperand is { } edgeMemory && Matches(edgeMemory)) count++;
            }
            if ((rung.IsTimer || rung.IsTimerReset) && Matches(rung.TimerVariable)) count++;
            else if ((rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad) && Matches(rung.CounterVariable)) count++;
            if (rung.IsCounter && rung.CounterKind == LadderCounterKind.CountDown && Matches(rung.CounterLoadInput)) count++;
            if (rung.IsCounter && rung.CounterKind == LadderCounterKind.CountUp && Matches(rung.CounterResetInput)) count++;
            else if (rung.IsNumericOperation)
            {
                if (Matches(rung.NumericSourceA)) count++;
                if (LadderNumericOperationRules.RequiresSourceB(rung.NumericOperationKind) && Matches(rung.NumericSourceB)) count++;
                if (LadderNumericOperationRules.RequiresSourceC(rung.NumericOperationKind) && Matches(rung.NumericSourceC)) count++;
                if (Matches(rung.NumericDestination)) count++;
            }
            else if (!rung.IsCall && !rung.IsReturn && !rung.IsJump && rung.HasTerminalAction && Matches(rung.CoilVariable)) count++;
            if (referencedTag is not null)
                count += rung.CallBindings.Count(binding => binding.Kind == LadderActualKind.Symbol
                    && TryGetCallTagSuffix(referencedTag, binding.Symbol, out _));
        }
        return count;
    }

    public bool TryRemoveTag(string name, out int referenceCount)
    {
        referenceCount = CountTagReferences(name);
        if (referenceCount > 0) return false;
        var index = Tags.FindIndex(tag => tag.Name.Equals(name, StringComparison.Ordinal));
        if (index < 0) return false;
        Tags.RemoveAt(index);
        foreach (var table in WatchTables)
            table.Variables.RemoveAll(item => item.Equals(name, StringComparison.Ordinal) || item.StartsWith(name + ".", StringComparison.Ordinal) || item.StartsWith(name + "[", StringComparison.Ordinal));
        return true;
    }

    private void RenameTagReferences(PlcVariable oldTag, PlcVariable updatedTag)
    {
        var oldName = oldTag.Name;
        var newName = updatedTag.Name;
        string RenameOperand(string operand)
        {
            if (operand.Equals(oldName, StringComparison.Ordinal)) return newName;
            return operand.StartsWith(oldName + ".", StringComparison.Ordinal) || operand.StartsWith(oldName + "[", StringComparison.Ordinal)
                ? newName + operand[oldName.Length..]
                : operand;
        }
        foreach (var block in Blocks)
        foreach (var rung in block.Rungs)
        {
            foreach (var branch in rung.Branches)
            for (var index = 0; index < branch.Contacts.Count; index++)
            {
                var contact = branch.Contacts[index];
                branch.Contacts[index] = contact with
                {
                    Variable = RenameOperand(contact.Variable),
                    RightOperand = RenameOperand(contact.RightOperand),
                    EdgeMemoryOperand = contact.EdgeMemoryOperand is null ? null : RenameOperand(contact.EdgeMemoryOperand),
                };
            }
            rung.CoilVariable = RenameOperand(rung.CoilVariable);
            rung.TimerVariable = RenameOperand(rung.TimerVariable);
            rung.CounterVariable = RenameOperand(rung.CounterVariable);
            rung.CounterLoadInput = RenameOperand(rung.CounterLoadInput);
            rung.CounterResetInput = RenameOperand(rung.CounterResetInput);
            rung.NumericSourceA = RenameOperand(rung.NumericSourceA);
            rung.NumericSourceB = RenameOperand(rung.NumericSourceB);
            rung.NumericSourceC = RenameOperand(rung.NumericSourceC);
            rung.NumericDestination = RenameOperand(rung.NumericDestination);

            var oldGlobalId = LadderCallBindings.GlobalId(oldTag);
            var newGlobalId = LadderCallBindings.GlobalId(updatedTag);
            if (oldGlobalId.Equals(newGlobalId, StringComparison.Ordinal)) continue;
            var bindings = rung.CallBindings.ToArray();
            var changed = false;
            for (var index = 0; index < bindings.Length; index++)
            {
                var binding = bindings[index];
                if (binding.Kind != LadderActualKind.Symbol
                    || !TryGetCallTagSuffix(oldTag, binding.Symbol, out var suffix)) continue;
                bindings[index] = binding with { Symbol = "global:" + newGlobalId + suffix };
                changed = true;
            }
            if (changed) rung.CallBindings = bindings;
        }
    }

    private static bool TryGetCallTagSuffix(PlcVariable tag, string symbol, out string suffix)
    {
        var prefix = "global:" + LadderCallBindings.GlobalId(tag);
        if (!PlcAggregates.IsAggregate(tag.Type) && symbol.Equals(prefix, StringComparison.Ordinal))
        {
            suffix = string.Empty;
            return true;
        }

        if (PlcAggregates.IsAggregate(tag.Type))
        {
            foreach (var leaf in PlcAggregates.Expand([tag]))
            {
                var leafSuffix = leaf.Name[tag.Name.Length..];
                if (!symbol.Equals(prefix + leafSuffix, StringComparison.Ordinal)) continue;
                suffix = leafSuffix;
                return true;
            }
        }

        suffix = string.Empty;
        return false;
    }

    private static object InitialValue(PlcVariableType type) => type switch
    {
        PlcVariableType.Timer => TimeSpan.Zero,
        PlcVariableType.Counter => 0L,
        PlcVariableType.Int => 0L,
        PlcVariableType.DInt => 0L,
        PlcVariableType.Real => 0.0,
        PlcVariableType.Time => TimeSpan.Zero,
        _ => false,
    };

    public static bool TryParseInitialValue(
        PlcVariableType type,
        string text,
        out object value,
        out string error)
    {
        var normalized = text.Trim();
        error = string.Empty;
        switch (type)
        {
            case PlcVariableType.String:
                value = text;
                if (text.Length <= PlcTextValues.MaximumLength) return true;
                error = "STRING initial value exceeds 255 characters.";
                break;
            case PlcVariableType.Enum:
                value = normalized;
                if (normalized.Length > 0 && normalized.Length <= 64) return true;
                error = "ENUM initial value must name a declared member.";
                break;
            case PlcVariableType.Bool:
                if (normalized.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || normalized == "1")
                {
                    value = true;
                    return true;
                }
                if (normalized.Equals("FALSE", StringComparison.OrdinalIgnoreCase) || normalized == "0")
                {
                    value = false;
                    return true;
                }
                error = "BOOL initial value must be TRUE, FALSE, 1, or 0.";
                break;
            case PlcVariableType.Int:
                if (short.TryParse(normalized, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var intValue))
                {
                    value = (long)intValue;
                    return true;
                }
                error = $"INT initial value must be an integer from {short.MinValue} to {short.MaxValue}.";
                break;
            case PlcVariableType.DInt:
                if (int.TryParse(normalized, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var dIntValue))
                {
                    value = (long)dIntValue;
                    return true;
                }
                error = $"DINT initial value must be an integer from {int.MinValue} to {int.MaxValue}.";
                break;
            case PlcVariableType.Real:
                if (double.TryParse(normalized, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var realValue)
                    && double.IsFinite(realValue))
                {
                    value = realValue;
                    return true;
                }
                error = "REAL initial value must be a finite invariant-culture number.";
                break;
            case PlcVariableType.Time:
                if (LadderTimeLiteral.TryParse(normalized, out var timeValue))
                {
                    value = timeValue;
                    return true;
                }
                error = "TIME initial value must be a valid S7 TIME literal from T#0ms through T#2147483647ms.";
                break;
            case PlcVariableType.Timer:
            case PlcVariableType.Counter:
                value = type == PlcVariableType.Timer ? TimeSpan.Zero : 0L;
                return true;
            default:
                error = $"Unsupported tag type '{type}'.";
                break;
        }
        value = InitialValue(type);
        return false;
    }

    private static void ValidateInitialValue(PlcVariableType type, object value)
    {
        var valid = type switch
        {
            PlcVariableType.Bool => value is bool,
            PlcVariableType.String or PlcVariableType.Enum => value is string text && text.Length <= PlcTextValues.MaximumLength,
            PlcVariableType.Int => value is long intValue && intValue is >= short.MinValue and <= short.MaxValue,
            PlcVariableType.DInt => value is long dIntValue && dIntValue is >= int.MinValue and <= int.MaxValue,
            PlcVariableType.Real => value is double realValue && double.IsFinite(realValue),
            PlcVariableType.Time => value is TimeSpan timeValue && timeValue >= TimeSpan.Zero
                && timeValue.TotalMilliseconds <= int.MaxValue && timeValue.Ticks % TimeSpan.TicksPerMillisecond == 0,
            PlcVariableType.Timer => value is TimeSpan,
            PlcVariableType.Counter => value is long,
            _ => false,
        };
        if (!valid) throw new InvalidOperationException($"Invalid {type.ToString().ToUpperInvariant()} initial value '{value}'.");
    }

    public EditableRung AddRung(string label, string coilVariable)
    {
        var activeBlock = EnsureActiveBlock();
        if (activeBlock.BlockType == LadderBlockType.DataBlock)
            throw new InvalidOperationException($"Data block '{activeBlock.Name}' contains declarations and cannot contain LAD networks.");
        var rung = new EditableRung(NewId("rung"), label, coilVariable);
        rung.Branches.Add(new EditableBranch(NewId("branch")));
        Rungs.Add(rung);
        return rung;
    }

    public EditableRung AddTimerRung(
        string label,
        string timerVariable,
        TimeSpan preset,
        LadderTimerKind kind = LadderTimerKind.OnDelay)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsTimer = true;
        rung.TimerVariable = timerVariable;
        rung.TimerPreset = preset;
        rung.TimerPresetAssigned = true;
        rung.TimerPresetOperand = LadderTimeLiteral.Format(preset);
        rung.TimerKind = kind;
        EnsureInstanceDataBlocks();
        return rung;
    }

    public EditableRung AddTimerResetRung(string label, string timerVariable)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsTimerReset = true;
        rung.TimerVariable = timerVariable;
        EnsureInstanceDataBlocks();
        return rung;
    }

    public EditableRung AddCounterRung(
        string label,
        string counterVariable,
        long preset,
        bool reset = false,
        LadderCounterKind kind = LadderCounterKind.CountUp,
        bool load = false,
        LadderCounterValueType valueType = LadderCounterValueType.Int)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsCounter = !reset && !load;
        rung.IsCounterReset = reset;
        rung.IsCounterLoad = load;
        rung.CounterKind = kind;
        rung.CounterVariable = counterVariable;
        rung.CounterPreset = preset;
        rung.CounterValueType = valueType;
        EnsureInstanceDataBlocks();
        return rung;
    }

    /// <summary>
    /// Assigns single-instance DB identities to IEC timer/counter calls and
    /// makes reset/load instructions reference the same DB as their instance.
    /// Missing assignments from older project files are migrated deterministically.
    /// </summary>
    public void EnsureInstanceDataBlocks()
    {
        var rungs = Blocks.SelectMany(block => block.Rungs).ToArray();
        var assigned = rungs.SelectMany(rung => new[] { rung.TimerInstanceDataBlock, rung.CounterInstanceDataBlock })
            .Concat(Instances.Select(instance => instance.InstanceDataBlock))
            .Where(db => db is not null).Select(db => db!).ToArray();
        var usedNumbers = assigned.Where(db => db.Number > 0).Select(db => db.Number).ToHashSet();
        var usedNames = assigned.Where(db => !string.IsNullOrWhiteSpace(db.Name)).Select(db => db.Name).ToHashSet(StringComparer.Ordinal);

        int NextNumber()
        {
            var candidate = 1;
            while (usedNumbers.Contains(candidate)) candidate++;
            usedNumbers.Add(candidate);
            return candidate;
        }
        string NextName(string prefix)
        {
            var candidate = 0;
            while (usedNames.Contains($"{prefix}_{candidate}_DB")) candidate++;
            var name = $"{prefix}_{candidate}_DB";
            usedNames.Add(name);
            return name;
        }
        LadderInstanceDataBlock Complete(LadderInstanceDataBlock? current, string prefix)
        {
            var number = current?.Number > 0 ? current.Number : NextNumber();
            var name = string.IsNullOrWhiteSpace(current?.Name) ? NextName(prefix) : current.Name.Trim();
            usedNumbers.Add(number);
            usedNames.Add(name);
            return new LadderInstanceDataBlock(number, name);
        }

        var timerInstances = new Dictionary<string, LadderInstanceDataBlock>(StringComparer.Ordinal);
        var counterInstances = new Dictionary<string, LadderInstanceDataBlock>(StringComparer.Ordinal);
        foreach (var rung in rungs.Where(rung => rung.IsTimer))
        {
            rung.TimerInstanceDataBlock = Complete(rung.TimerInstanceDataBlock, "IEC_Timer");
            timerInstances.TryAdd(rung.TimerVariable, rung.TimerInstanceDataBlock);
        }
        foreach (var rung in rungs.Where(rung => rung.IsCounter))
        {
            rung.CounterInstanceDataBlock = Complete(rung.CounterInstanceDataBlock, "IEC_Counter");
            counterInstances.TryAdd(rung.CounterVariable, rung.CounterInstanceDataBlock);
        }
        foreach (var rung in rungs.Where(rung => rung.IsCounterLoad))
        {
            if (counterInstances.TryGetValue(rung.CounterVariable, out var instanceDb))
                rung.CounterInstanceDataBlock = instanceDb;
            else
            {
                rung.CounterInstanceDataBlock = Complete(rung.CounterInstanceDataBlock, "IEC_Counter");
                counterInstances.TryAdd(rung.CounterVariable, rung.CounterInstanceDataBlock);
            }
        }
        foreach (var rung in rungs.Where(rung => rung.IsTimerReset))
            rung.TimerInstanceDataBlock = timerInstances.TryGetValue(rung.TimerVariable, out var instanceDb)
                ? instanceDb : Complete(rung.TimerInstanceDataBlock, "IEC_Timer");
        foreach (var rung in rungs.Where(rung => rung.IsCounterReset))
            rung.CounterInstanceDataBlock = counterInstances.TryGetValue(rung.CounterVariable, out var instanceDb)
                ? instanceDb : Complete(rung.CounterInstanceDataBlock, "IEC_Counter");

        for (var index = 0; index < Instances.Count; index++)
        {
            var instance = Instances[index];
            var prefix = "FB_" + DbIdentifier(instance.Name);
            Instances[index] = instance with
            {
                InstanceDataBlock = Complete(instance.InstanceDataBlock, prefix),
            };
        }
    }

    private static string DbIdentifier(string value)
    {
        var characters = value.Trim().Select(character =>
            char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
        var identifier = new string(characters).Trim('_');
        if (identifier.Length == 0) identifier = "Instance";
        if (!char.IsLetter(identifier[0]) && identifier[0] != '_') identifier = "I_" + identifier;
        return identifier;
    }

    public EditableRung AddNumericOperationRung(
        string label,
        LadderNumericOperationKind kind,
        string sourceA,
        string sourceB,
        string destination,
        string sourceC = "0")
    {
        var rung = AddRung(label, string.Empty);
        rung.IsNumericOperation = true;
        rung.NumericOperationKind = kind;
        rung.NumericSourceA = sourceA;
        rung.NumericSourceB = sourceB;
        rung.NumericSourceC = sourceC;
        rung.NumericDestination = destination;
        return rung;
    }

    public EditableRung AddCallRung(string label, string targetBlock)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsCall = true;
        rung.CallTarget = targetBlock;
        return rung;
    }

    public EditableRung AddReturnRung(string label)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsReturn = true;
        return rung;
    }

    public EditableRung AddJumpRung(string label, string targetLabel)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsJump = true;
        rung.ProgramControlLabel = targetLabel.Trim();
        return rung;
    }

    public EditableRung AddLabelRung(string label, string name)
    {
        var rung = AddRung(label, string.Empty);
        rung.IsLabel = true;
        rung.HasTerminalAction = false;
        rung.NetworkLabelId = NewStableId(rung.Id, "label");
        rung.NetworkLabelName = name.Trim();
        // Preserve the legacy shared field for old editor consumers. New
        // executable exports use NetworkLabelName.
        rung.ProgramControlLabel = rung.NetworkLabelName;
        return rung;
    }

    public EditableContact AddContact(int rungIndex, int branchIndex, string variable, bool normallyClosed)
    {
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        return InsertContact(rungIndex, branchIndex, contacts.Count, variable, normallyClosed);
    }

    public EditableContact InsertContact(
        int rungIndex,
        int branchIndex,
        int contactIndex,
        string variable,
        bool normallyClosed)
    {
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        if (contactIndex < 0 || contactIndex > contacts.Count)
            throw new ArgumentOutOfRangeException(nameof(contactIndex));
        var contact = new EditableContact(NewId("contact"), variable, normallyClosed);
        contacts.Insert(contactIndex, contact);
        return contact;
    }

    public EditableContact InsertEdgeContact(
        int rungIndex,
        int branchIndex,
        int contactIndex,
        string variable,
        LadderEdgeMode edgeMode)
    {
        if (edgeMode == LadderEdgeMode.None)
            throw new ArgumentException("An edge contact requires Rising or Falling mode.", nameof(edgeMode));
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        if (contactIndex < 0 || contactIndex > contacts.Count)
            throw new ArgumentOutOfRangeException(nameof(contactIndex));
        var edgeMemoryOperand = CreateEdgeMemoryOperand();
        var contact = new EditableContact(
            NewId("edge"), variable, false, EdgeMode: edgeMode) { EdgeMemoryOperand = edgeMemoryOperand };
        contacts.Insert(contactIndex, contact);
        return contact;
    }

    public string CreateEdgeMemoryOperand()
    {
        var memoryName = "edge_memory_1";
        for (var suffix = 2; Tags.Any(tag => tag.Name.Equals(memoryName, StringComparison.Ordinal)); suffix++)
            memoryName = $"edge_memory_{suffix}";
        var tag = AddTag(memoryName, PlcVariableRole.Memory, comment: "TIA P/N edge memory");
        return Blocks[ActiveBlockIndex].SemanticMode == LadderSemanticMode.Parameterized
            ? "global:" + LadderCallBindings.GlobalId(tag)
            : tag.Name;
    }

    internal EditableLadderNode AssignFreshEdgeMemoryOperands(EditableLadderNode root, ISet<string>? nodeIds = null)
    {
        EditableLadderNode Copy(EditableLadderNode node)
        {
            var children = node.Children?.Select(Copy).ToArray();
            return node with
            {
                Children = children is null ? null : Array.AsReadOnly(children),
                EdgeMemoryOperand = node.Kind == LadderNodeKind.Contact && node.EdgeMode != LadderEdgeMode.None
                    && (nodeIds is null || nodeIds.Contains(node.Id))
                    ? CreateEdgeMemoryOperand() : node.EdgeMemoryOperand,
            };
        }
        return Copy(root);
    }

    public EditableContact AddComparison(
        int rungIndex,
        int branchIndex,
        string leftOperand,
        LadderCompareOperator comparison,
        string rightOperand,
        LadderComparisonValueType comparisonValueType = LadderComparisonValueType.Unspecified)
    {
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        return InsertComparison(rungIndex, branchIndex, contacts.Count, leftOperand, comparison, rightOperand,
            comparisonValueType);
    }

    public EditableContact InsertComparison(
        int rungIndex,
        int branchIndex,
        int contactIndex,
        string leftOperand,
        LadderCompareOperator comparison,
        string rightOperand,
        LadderComparisonValueType comparisonValueType = LadderComparisonValueType.Unspecified)
    {
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        if (contactIndex < 0 || contactIndex > contacts.Count)
            throw new ArgumentOutOfRangeException(nameof(contactIndex));
        var element = new EditableContact(
            NewId("compare"), leftOperand, false, true, comparison, rightOperand.Trim(),
            ComparisonValueType: comparisonValueType);
        contacts.Insert(contactIndex, element);
        return element;
    }

    public EditableBranch AddParallelBranch(int rungIndex)
    {
        var branch = new EditableBranch(NewId("branch"));
        Rungs[rungIndex].Branches.Add(branch);
        return branch;
    }

    /// <summary>
    /// Captures an isolated rung/network template for the editor clipboard.
    /// Stable IDs are intentionally retained in the template so Copy itself
    /// cannot mutate the document. PasteRung regenerates every identity.
    /// </summary>
    public EditableRung CaptureRung(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        return CloneRungTemplate(Rungs[rungIndex], preserveIds: true);
    }

    /// <summary>
    /// Inserts a copied rung/network and assigns new rung, branch, and contact
    /// IDs. This prevents clipboard operations from creating duplicate stable
    /// identities in the compiler/runtime monitor index.
    /// </summary>
    public EditableRung PasteRung(EditableRung template, int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (insertionIndex < 0 || insertionIndex > Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));
        var pasted = CloneRungTemplate(template, preserveIds: false);
        pasted.SetRoot(AssignFreshEdgeMemoryOperands(pasted.Root));
        Rungs.Insert(insertionIndex, pasted);
        EnsureInstanceDataBlocks();
        return pasted;
    }

    /// <summary>Copies one condition instruction into an exact series slot.</summary>
    public EditableContact PasteContact(
        EditableContact template,
        int rungIndex,
        int branchIndex,
        int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(template);
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        if (insertionIndex < 0 || insertionIndex > contacts.Count)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));
        var prefix = template.IsComparison
            ? "compare"
            : template.EdgeMode == LadderEdgeMode.None ? "contact" : "edge";
        var pasted = template with { Id = NewId(prefix) };
        if (pasted.EdgeMode != LadderEdgeMode.None)
            pasted = pasted with { EdgeMemoryOperand = CreateEdgeMemoryOperand() };
        contacts.Insert(insertionIndex, pasted);
        return pasted;
    }

    private EditableRung CloneRungTemplate(EditableRung source, bool preserveIds)
    {
        var rung = new EditableRung(preserveIds ? source.Id : NewId("rung"), source.Label, source.CoilVariable)
        {
            CoilMode = source.CoilMode,
            TimerVariable = source.TimerVariable,
            TimerPreset = source.TimerPreset,
            TimerPresetAssigned = source.TimerPresetAssigned,
            TimerPresetOperand = source.TimerPresetOperand,
            TimerResetOperand = source.TimerResetOperand,
            TimerKind = source.TimerKind,
            TimerInstanceDataBlock = preserveIds ? source.TimerInstanceDataBlock : null,
            IsTimer = source.IsTimer,
            IsTimerReset = source.IsTimerReset,
            IsCounter = source.IsCounter,
            IsCounterReset = source.IsCounterReset,
            IsCounterLoad = source.IsCounterLoad,
            CounterKind = source.CounterKind,
            CounterVariable = source.CounterVariable,
            CounterPreset = source.CounterPreset,
            CounterValueType = source.CounterValueType,
            CounterInstanceDataBlock = preserveIds ? source.CounterInstanceDataBlock : null,
            CounterLoadInput = source.CounterLoadInput,
            CounterResetInput = source.CounterResetInput,
            IsNumericOperation = source.IsNumericOperation,
            NumericOperationKind = source.NumericOperationKind,
            NumericSourceA = source.NumericSourceA,
            NumericSourceB = source.NumericSourceB,
            NumericSourceC = source.NumericSourceC,
            NumericDestination = source.NumericDestination,
        NumericDataType = source.NumericDataType,
        NumericInputDataType = source.NumericInputDataType,
        NumericOutputDataType = source.NumericOutputDataType,
            IsCall = source.IsCall,
            CallTarget = source.CallTarget,
            CallSemanticMode = source.CallSemanticMode, CallBindings = source.CallBindings.ToArray(), CallInstanceId = source.CallInstanceId,
            IsReturn = source.IsReturn,
            ReturnValueKind = source.ReturnValueKind,
            ReturnValueOperand = source.ReturnValueOperand,
            IsJump = source.IsJump,
            IsLabel = source.IsLabel,
            NetworkLabelId = preserveIds ? source.NetworkLabelId
                : source.IsLabel ? NewId("label") : string.Empty,
            NetworkLabelName = source.NetworkLabelName,
            ProgramControlLabel = source.ProgramControlLabel,
        };
        rung.TerminalActionId = preserveIds ? source.TerminalActionId : NewId("action");
        rung.HasTerminalAction = source.HasTerminalAction;
        rung.HasEmptyInstructionPlaceholder = source.HasEmptyInstructionPlaceholder;
        rung.LegacyEmptyBranchSemantics = source.LegacyEmptyBranchSemantics;
        rung.SetRoot(preserveIds ? EditableLadderTree.Import(EditableLadderTree.Export(source.Root))
            : EditableLadderTree.CloneWithFreshIds(source.Root, () => NewId("node")));
        return rung;
    }

    public void RemoveParallelBranch(int rungIndex, int branchIndex)
    {
        var branches = Rungs[rungIndex].Branches;
        if (branches.Count <= 1)
            throw new InvalidOperationException("A rung/network must retain at least one branch path.");
        branches.RemoveAt(branchIndex);
    }

    public void RemoveContact(int rungIndex, int branchIndex, int contactIndex)
    {
        var rung = Rungs[rungIndex]; var branch = rung.Branches[branchIndex];
        if (branch.Node.IsLeaf || (branch.Node.Children ?? []).Any(n => !n.IsLeaf))
            RemoveSubtree(rungIndex, branch.Contacts[contactIndex].Id);
        else branch.Contacts.RemoveAt(contactIndex);
    }

    public void ReplaceContact(int rungIndex, int branchIndex, int contactIndex, EditableContact replacement)
    {
        var current = Rungs[rungIndex].Branches[branchIndex].Contacts[contactIndex];
        if (!string.Equals(current.Id, replacement.Id, StringComparison.Ordinal))
            throw new InvalidOperationException("Replacing a contact must preserve its stable element ID.");
        Rungs[rungIndex].Branches[branchIndex].Contacts[contactIndex] = replacement;
    }

    public int MoveContact(int rungIndex, int branchIndex, int contactIndex, int offset)
    {
        var contacts = Rungs[rungIndex].Branches[branchIndex].Contacts;
        if (!Rungs[rungIndex].IsFlat)
            throw new InvalidOperationException("Nested movement requires an exact subtree command.");
        if (contactIndex < 0 || contactIndex >= contacts.Count)
            throw new ArgumentOutOfRangeException(nameof(contactIndex));
        var destination = Math.Clamp(contactIndex + offset, 0, contacts.Count - 1);
        if (destination == contactIndex) return contactIndex;
        var contact = contacts[contactIndex];
        contacts.RemoveAt(contactIndex);
        contacts.Insert(destination, contact);
        return destination;
    }

    public void RemoveRung(int rungIndex) => Rungs.RemoveAt(rungIndex);

    public EditableLadderNode CaptureSubtree(int rungIndex, string nodeId) =>
        EditableLadderTree.Find(Rungs[rungIndex].Root, nodeId)
        ?? throw new ArgumentException($"logic: node '{nodeId}' does not exist.");

    public void InsertSubtree(int rungIndex, string targetId, EditableLadderNode template, bool after, bool freshIds = true) =>
        ApplyTreeMutation(rungIndex, root => EditableLadderTree.Insert(root, targetId,
            freshIds ? EditableLadderTree.CloneWithFreshIds(template, () => NewId("node")) : template,
            after, () => NewId("node")));

    public void InsertSubtreeAt(int rungIndex, string parentId, int slot, EditableLadderNode template, bool freshIds = true) =>
        ApplyTreeMutation(rungIndex, root => EditableLadderTree.InsertChild(root, parentId, slot,
            freshIds ? EditableLadderTree.CloneWithFreshIds(template, () => NewId("node")) : template));

    public void WrapParallel(int rungIndex, string nodeId) =>
        ApplyTreeMutation(rungIndex, root => EditableLadderTree.WrapParallel(root, nodeId, () => NewId("node")));

    public void RemoveSubtree(int rungIndex, string nodeId) =>
        ApplyTreeMutation(rungIndex, root => EditableLadderTree.RemoveSubtree(root, nodeId, () => NewId("node")));

    public void MoveSubtree(int rungIndex, string nodeId, string parentId, int slot) =>
        ApplyTreeMutation(rungIndex, root => EditableLadderTree.MoveSubtree(root, nodeId, parentId, slot, () => NewId("node")));

    public void ReplaceSubtree(int rungIndex, string nodeId, EditableLadderNode replacement) =>
        ApplyTreeMutation(rungIndex, root => EditableLadderTree.Replace(root, nodeId, replacement));

    /// <summary>Explicit, history-owned conversion: empty Series paths become TRUE wires.</summary>
    public void ConvertToRecursiveSemantics(int rungIndex) => Rungs[rungIndex].LegacyEmptyBranchSemantics = false;

    private void ApplyTreeMutation(int rungIndex, Func<EditableLadderNode, EditableLadderNode> mutation)
    {
        var rung = Rungs[rungIndex];
        if (rung.LegacyEmptyBranchSemantics)
            throw new InvalidOperationException("Convert this legacy network to recursive semantics explicitly before structural editing; empty Series paths become TRUE wires.");
        var oldNextId = _nextId;
        try
        {
            var replacement = mutation(rung.Root);
            ValidateTreeReplacement(rungIndex, replacement);
            rung.SetRoot(replacement);
        }
        catch { _nextId = oldNextId; throw; }
    }

    internal void ValidateTreeReplacement(int rungIndex, EditableLadderNode replacement)
    {
            var rung = Rungs[rungIndex];
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var existing in Blocks.SelectMany(b => b.Rungs))
            {
                if (!occupied.Add(existing.Id)) throw new ArgumentException($"Duplicate network ID '{existing.Id}'.");
                foreach (var id in TerminalIdsForRung(existing))
                    if (!occupied.Add(id)) throw new ArgumentException($"Duplicate terminal ID '{id}'.");
                if (!ReferenceEquals(existing, rung)) EditableLadderTree.Validate(existing.Root, "logic", occupied);
            }
            EditableLadderTree.Validate(replacement, "logic", occupied);
    }

    private static IEnumerable<string> TerminalIdsForRung(EditableRung rung)
    {
        if (rung.IsLabel)
            yield return string.IsNullOrWhiteSpace(rung.NetworkLabelId)
                ? NewStableId(rung.Id, "label") : rung.NetworkLabelId;
        if (!rung.HasTerminalAction) yield break;
        if (!string.IsNullOrEmpty(rung.TerminalActionId)) { yield return rung.TerminalActionId; yield break; }
        var suffix = rung.IsTimer ? "timer" : rung.IsTimerReset ? "timer-reset" : rung.IsCounter ? "counter"
            : rung.IsCounterReset ? "counter-reset" : rung.IsCounterLoad ? "counter-load" : rung.IsNumericOperation ? "numeric"
            : rung.IsCall ? "call" : rung.IsReturn ? "return" : rung.IsJump ? "jump" : "coil";
        yield return NewStableId(rung.Id, suffix);
    }

    public LadderProgram BuildProgram()
    {
        EnsureInstanceDataBlocks();
        EnsureActiveBlock();
        LadderNetwork BuildNetwork(EditableRung rung)
        {
            var logic = EditableLadderTree.Export(rung.Root);
            if (rung.LegacyEmptyBranchSemantics)
            {
                if (!rung.IsFlat) throw new InvalidOperationException("Legacy empty-branch semantics requires a flat network.");
                var surviving = rung.Branches.Where(b => b.Contacts.Count > 0).Select(b => EditableLadderTree.Export(b.Node)).ToArray();
                logic = surviving.Length == 1 ? surviving[0]
                    : new LadderNode(rung.Root.Kind == LadderNodeKind.Parallel ? rung.Root.Id : NewStableId(rung.Id, "parallel"),
                        LadderNodeKind.Parallel, Children: surviving);
            }
            string ActionId(string suffix) => string.IsNullOrWhiteSpace(rung.TerminalActionId) ? NewStableId(rung.Id, suffix) : rung.TerminalActionId;
            var timerPreset = rung.TimerPreset;
            if (rung.IsTimer && rung.TimerPresetAssigned
                && rung.TimerPresetOperand is { Length: > 0 } presetOperand
                && !LadderTimeLiteral.TryParse(presetOperand, out _)
                && Tags.FirstOrDefault(tag => tag.Name == presetOperand && tag.Type == PlcVariableType.Time)?.InitialValue is TimeSpan timeInitial)
                timerPreset = timeInitial;
            var usesCoil = rung.HasTerminalAction && !rung.IsTimer && !rung.IsTimerReset && !rung.IsCounter && !rung.IsCounterReset && !rung.IsCounterLoad
                && !rung.IsNumericOperation && !rung.IsCall && !rung.IsReturn && !rung.IsJump;
            return new LadderNetwork(
                rung.Id,
                rung.Label,
                logic,
                usesCoil ? new LadderCoil(ActionId("coil"), rung.CoilVariable, rung.CoilMode) : null,
                rung.HasTerminalAction && rung.IsTimer ? new LadderTimer(ActionId("timer"), rung.TimerVariable, timerPreset,
                    rung.TimerKind, rung.TimerInstanceDataBlock, rung.TimerPresetAssigned,
                    string.Equals(rung.TimerPresetOperand, LadderTimeLiteral.Format(rung.TimerPreset), StringComparison.Ordinal)
                        ? null : rung.TimerPresetOperand,
                    rung.TimerResetOperand) : null,
                rung.HasTerminalAction && rung.IsCounter ? new LadderCounter(ActionId("counter"), rung.CounterVariable, rung.CounterPreset, rung.CounterKind, rung.CounterValueType, rung.CounterInstanceDataBlock, rung.CounterLoadInput, rung.CounterResetInput) : null,
                rung.HasTerminalAction && rung.IsCounterReset ? new LadderCounterReset(ActionId("counter-reset"), rung.CounterVariable, rung.CounterInstanceDataBlock) : null,
                rung.HasTerminalAction && rung.IsNumericOperation ? new LadderNumericOperation(
                    ActionId("numeric"),
                    rung.NumericOperationKind,
                    rung.NumericSourceA,
                    rung.NumericSourceB,
                    rung.NumericDestination,
                    rung.NumericSourceC)
                {
                    DataType = rung.NumericDataType,
                    InputDataType = rung.NumericInputDataType,
                    OutputDataType = rung.NumericOutputDataType,
                } : null,
                rung.HasTerminalAction && rung.IsCall ? new LadderCall(ActionId("call"), rung.CallTarget, rung.CallSemanticMode, rung.CallBindings.ToArray(), rung.CallInstanceId) : null,
                rung.HasTerminalAction && rung.IsReturn ? new LadderReturn(
                    ActionId("return"), rung.ReturnValueKind, rung.ReturnValueOperand) : null,
                rung.HasTerminalAction && rung.IsCounterLoad ? new LadderCounterLoad(ActionId("counter-load"), rung.CounterVariable, rung.CounterPreset, rung.CounterValueType, rung.CounterInstanceDataBlock) : null,
                rung.HasTerminalAction && rung.IsTimerReset ? new LadderTimerReset(ActionId("timer-reset"), rung.TimerVariable, rung.TimerInstanceDataBlock) : null,
                rung.HasTerminalAction && rung.IsJump ? new LadderJump(ActionId("jump"), rung.ProgramControlLabel) : null,
                rung.IsLabel ? new LadderLabel(
                    string.IsNullOrWhiteSpace(rung.NetworkLabelId) ? NewStableId(rung.Id, "label") : rung.NetworkLabelId,
                    string.IsNullOrWhiteSpace(rung.NetworkLabelName) ? rung.ProgramControlLabel : rung.NetworkLabelName) : null);
        }
        var blocks = Blocks.Select(block => new LadderBlock(
            block.Id,
            block.Name,
            block.Rungs.Select(BuildNetwork).ToArray(),
            block.BlockType,
            block.Interface.ToArray(), block.SemanticMode)).ToArray();
        var tasks = Tasks.Select(task => new LadderTask(
            task.Id,
            task.Name,
            task.Kind,
            task.Kind == LadderTaskKind.Continuous ? ScanPeriod : task.Period,
            task.Priority,
            task.EntryBlock)).ToArray();
        return new LadderProgram(
            2,
            Id,
            Name,
            "LD",
            ScanPeriod,
            Tags.ToArray(),
            blocks[0].Networks,
            blocks,
            string.IsNullOrWhiteSpace(EntryBlockId) ? blocks[0].Id : EntryBlockId,
            tasks,
            AllWatchVariables.ToArray(), Instances.ToArray());
    }

    public void ReplaceFromProgram(LadderProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        var sourceBlocks = program.Blocks is { Count: > 0 }
            ? program.Blocks : [new LadderBlock("main", program.Name, program.Networks)];
        // Validate the entire import before touching the current draft.
        var occupied = new HashSet<string>(StringComparer.Ordinal);
        var prepared = new Dictionary<string, EditableLadderNode>(StringComparer.Ordinal);
        foreach (var block in sourceBlocks)
        for (var index = 0; index < block.Networks.Count; index++)
        {
            var n = block.Networks[index];
            var path = $"blocks[{block.Id}].networks[{index}]";
            if (string.IsNullOrWhiteSpace(n.Id) || !occupied.Add(n.Id))
                throw new ArgumentException($"{path}.id: missing or duplicate network ID '{n.Id}'.");
            var actions = TerminalIds(n).ToArray();
            if (actions.Length > 1) throw new ArgumentException($"{path}: a network permits at most one terminal action.");
            foreach (var id in actions)
                if (string.IsNullOrWhiteSpace(id) || !occupied.Add(id))
                    throw new ArgumentException($"{path}.action.id: missing or duplicate terminal ID '{id}'.");
            if (n.LabelInstruction is { } networkLabel
                && (string.IsNullOrWhiteSpace(networkLabel.Id) || !occupied.Add(networkLabel.Id)))
                throw new ArgumentException($"{path}.label.id: missing or duplicate LABEL ID '{networkLabel.Id}'.");
            var root = EditableLadderTree.Import(n.Logic, path + ".logic");
            EditableLadderTree.Validate(root, path + ".logic", occupied);
            prepared.Add(n.Id, root);
        }
        var newBlocks = new List<EditableBlock>();
        foreach (var source in sourceBlocks)
        {
            var block = new EditableBlock(source.Id, source.Name, source.BlockType) { SemanticMode = source.SemanticMode };
            block.Interface.AddRange(source.Interface ?? []);
            foreach (var n in source.Networks)
            {
                var rung = new EditableRung(n.Id, n.Label, n.Coil?.Variable ?? string.Empty)
                {
                    TerminalActionId = TerminalIds(n).FirstOrDefault() ?? string.Empty,
                    HasTerminalAction = TerminalIds(n).Any(),
                    CoilMode = n.Coil?.Mode ?? LadderCoilMode.Assign,
                    IsTimer = n.Timer is not null, TimerVariable = n.Timer?.Variable ?? n.TimerReset?.Variable ?? string.Empty,
                    TimerPreset = n.Timer?.Preset ?? TimeSpan.FromSeconds(1),
                    TimerPresetAssigned = n.Timer?.PresetAssigned ?? true,
                    TimerPresetOperand = n.Timer is null || !n.Timer.PresetAssigned ? string.Empty
                        : n.Timer.PresetOperand ?? LadderTimeLiteral.Format(n.Timer.Preset),
                    TimerResetOperand = n.Timer?.ResetOperand ?? "FALSE",
                    TimerKind = n.Timer?.Kind ?? LadderTimerKind.OnDelay,
                    IsTimerReset = n.TimerReset is not null,
                    IsCounter = n.Counter is not null, IsCounterReset = n.CounterReset is not null,
                    IsCounterLoad = n.CounterLoad is not null, CounterVariable = n.Counter?.Variable ?? n.CounterReset?.Variable ?? n.CounterLoad?.Variable ?? string.Empty,
                    CounterPreset = n.Counter?.Preset ?? n.CounterLoad?.Preset ?? 10, CounterKind = n.Counter?.Kind ?? LadderCounterKind.CountUp,
                    CounterValueType = n.Counter?.ValueType ?? n.CounterLoad?.ValueType ?? LadderCounterValueType.Int,
                    TimerInstanceDataBlock = n.Timer?.InstanceDataBlock ?? n.TimerReset?.InstanceDataBlock,
                    CounterInstanceDataBlock = n.Counter?.InstanceDataBlock ?? n.CounterReset?.InstanceDataBlock ?? n.CounterLoad?.InstanceDataBlock,
                    CounterLoadInput = n.Counter?.LoadInput ?? "FALSE",
                    CounterResetInput = n.Counter?.ResetInput ?? "FALSE",
                    IsNumericOperation = n.NumericOperation is not null, NumericOperationKind = n.NumericOperation?.Kind ?? LadderNumericOperationKind.Move,
                    NumericSourceA = n.NumericOperation?.SourceA ?? "0", NumericSourceB = n.NumericOperation?.SourceB ?? "0",
                    NumericSourceC = n.NumericOperation?.SourceC ?? "0", NumericDestination = n.NumericOperation?.Destination ?? string.Empty,
                    NumericDataType = n.NumericOperation?.DataType ?? LadderNumericDataType.Unspecified,
                    NumericInputDataType = n.NumericOperation?.InputDataType ?? LadderNumericDataType.Unspecified,
                    NumericOutputDataType = n.NumericOperation?.OutputDataType ?? LadderNumericDataType.Unspecified,
                    IsCall = n.Call is not null, CallTarget = n.Call?.TargetBlock ?? string.Empty,
                    CallSemanticMode = n.Call?.SemanticMode ?? LadderSemanticMode.Legacy, CallBindings = n.Call?.Bindings?.ToArray() ?? [], CallInstanceId = n.Call?.InstanceId ?? string.Empty,
                    IsReturn = n.Return is not null,
                    ReturnValueKind = n.Return?.ValueKind ?? LadderReturnValueKind.Rlo,
                    ReturnValueOperand = n.Return?.Operand ?? string.Empty,
                    IsJump = n.Jump is not null, IsLabel = n.LabelInstruction is not null,
                    NetworkLabelId = n.LabelInstruction?.Id ?? string.Empty,
                    NetworkLabelName = n.LabelInstruction?.Name ?? string.Empty,
                    ProgramControlLabel = n.Jump?.TargetLabel ?? (n.Jump is null ? n.LabelInstruction?.Name : null) ?? string.Empty,
                };
                rung.SetRoot(prepared[n.Id]); block.Rungs.Add(rung);
            }
            newBlocks.Add(block);
        }
        Id = program.Id; Name = program.Name; ScanPeriod = program.ScanPeriod;
        Tags.Clear(); Tags.AddRange(program.Variables);
        TagTables.Clear();
        TagTables.Add("Default tag table");
        foreach (var tagTable in Tags.Select(tag => string.IsNullOrWhiteSpace(tag.TagTable)
                     ? "Default tag table" : tag.TagTable.Trim())
                 .Distinct(StringComparer.OrdinalIgnoreCase))
            if (!TagTables.Contains(tagTable, StringComparer.OrdinalIgnoreCase)) TagTables.Add(tagTable);
        // Constants are editor metadata and are not part of LadderProgram yet.
        // Replacing from an external executable program must not retain stale declarations.
        UserConstants.Clear();
        Instances.Clear(); Instances.AddRange(program.Instances ?? []);
        WatchTables.Clear(); WatchTables.Add(EditableWatchTable.CreateDefault(program.WatchVariables)); ActiveWatchTableIndex = 0;
        Blocks.Clear(); Blocks.AddRange(newBlocks); Tasks.Clear(); _nextId = 1;
        EnsureInstanceDataBlocks();
        EntryBlockId = string.IsNullOrWhiteSpace(program.EntryBlock) ? Blocks[0].Id : program.EntryBlock;
        if (program.Tasks is { Count: > 0 })
            Tasks.AddRange(program.Tasks.Select(t => new EditableTask(t.Id, t.Name, t.Kind, t.Period, t.Priority, t.EntryBlock)));
        else Tasks.Add(new EditableTask(NewId("task"), "MainTask", LadderTaskKind.Continuous, ScanPeriod, 10, EntryBlockId));
        ActiveBlockIndex = Math.Max(0, Blocks.FindIndex(b => b.Id == EntryBlockId));
        InvalidateCommandTargets();
    }

    private static IEnumerable<string> TerminalIds(LadderNetwork n)
    {
        if (n.Coil is not null) yield return n.Coil.Id;
        if (n.Timer is not null) yield return n.Timer.Id;
        if (n.TimerReset is not null) yield return n.TimerReset.Id;
        if (n.Counter is not null) yield return n.Counter.Id;
        if (n.CounterReset is not null) yield return n.CounterReset.Id;
        if (n.CounterLoad is not null) yield return n.CounterLoad.Id;
        if (n.NumericOperation is not null) yield return n.NumericOperation.Id;
        if (n.Call is not null) yield return n.Call.Id;
        if (n.Return is not null) yield return n.Return.Id;
        if (n.Jump is not null) yield return n.Jump.Id;
    }

    public static LadderEditorDocument CreateConveyorExample()
    {
        var document = new LadderEditorDocument { Id = "offline-conveyor", Name = "Conveyor_Main" };
        document.AddTag("start_command", PlcVariableRole.Input, "operator.start");
        document.AddTag("stop_command", PlcVariableRole.Input, "operator.stop");
        document.AddTag("simulated_photoeye", PlcVariableRole.Input, "simulated_photoeye");
        document.AddTag("seal_in", PlcVariableRole.Memory);
        document.AddTag("conveyor_running", PlcVariableRole.Output, "conveyor_running");
        document.WatchVariables.AddRange(document.Tags.Select(tag => tag.Name));

        document.AddRung("Start/Stop seal-in and photoeye permissive", "seal_in");
        document.AddContact(0, 0, "start_command", false);
        document.AddContact(0, 0, "stop_command", true);
        document.AddContact(0, 0, "simulated_photoeye", true);
        document.AddParallelBranch(0);
        document.AddContact(0, 1, "seal_in", false);
        document.AddContact(0, 1, "stop_command", true);
        document.AddContact(0, 1, "simulated_photoeye", true);

        document.AddRung("Conveyor output command", "conveyor_running");
        document.AddContact(1, 0, "seal_in", false);
        document.AddContact(1, 0, "simulated_photoeye", true);
        return document;
    }

    private string NewId(string prefix)
    {
        var occupied = new HashSet<string>(Blocks.Select(b => b.Id).Concat(Tasks.Select(t => t.Id)), StringComparer.Ordinal);
        foreach (var rung in Blocks.SelectMany(b => b.Rungs))
        {
            occupied.Add(rung.Id);
            foreach (var node in EditableLadderTree.Visit(rung.Root)) occupied.Add(node.Id);
            occupied.Add(rung.TerminalActionId);
            foreach (var actionId in TerminalIdsForRung(rung)) occupied.Add(actionId);
        }
        string candidate;
        do { candidate = $"{prefix}-{_nextId++}"; } while (occupied.Contains(candidate));
        return candidate;
    }
    private static string NewStableId(string owner, string suffix) => $"{owner}-{suffix}";

    private static string ValidateUniqueName(string name, IEnumerable<string> existingNames, string objectKind)
        => ValidateUniqueName(name, existingNames, objectKind, StringComparison.Ordinal);

    private static string ValidateUniqueName(
        string name,
        IEnumerable<string> existingNames,
        string objectKind,
        StringComparison comparison)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0) throw new InvalidOperationException($"{objectKind} name cannot be empty.");
        if (existingNames.Any(existing => existing.Equals(normalized, comparison)))
            throw new InvalidOperationException($"{objectKind} name '{normalized}' already exists.");
        return normalized;
    }

    private string ValidateCpuSymbolName(string name, int excludedTagIndex = -1)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0) throw new InvalidOperationException("Tag name cannot be empty.");
        if (Tags.Where((_, index) => index != excludedTagIndex)
            .Any(tag => tag.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Tag '{normalized}' already exists; PLC tag names are case-insensitive across tag tables.");
        if (Blocks.Any(block => block.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Tag name '{normalized}' conflicts with a block/routine name.");
        if (UserConstants.Any(constant => constant.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Tag name '{normalized}' conflicts with a PLC user constant.");
        return normalized;
    }

    private EditableBlock EnsureActiveBlock()
    {
        if (Blocks.Count == 0) AddBlock(string.IsNullOrWhiteSpace(Name) ? "Main" : Name);
        ActiveBlockIndex = Math.Clamp(ActiveBlockIndex, 0, Blocks.Count - 1);
        return Blocks[ActiveBlockIndex];
    }

}

