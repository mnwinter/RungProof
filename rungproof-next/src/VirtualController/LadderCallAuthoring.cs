using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed record CallAuthoringIssue(string Field, string Message);

/// <summary>One detached CALL edit owns conversion, identities, instances and bindings.</summary>
public sealed class LadderCallAuthoring
{
    private readonly LadderEditTransaction _transaction;
    private readonly string _ownerId;
    private readonly string _rungId;
    public LadderEditorDocument Draft => _transaction.Draft;
    public EditableBlock Owner => Draft.Blocks.Single(b => b.Id == _ownerId);
    public EditableRung Call => Owner.Rungs.Single(r => r.Id == _rungId);
    public EditableBlock? Target => Draft.Blocks.FirstOrDefault(b => b.Id == Call.CallTarget);
    public bool IsPending => _transaction.IsPending;
    public LadderCallAuthoring(LadderEditorDocument document, string ownerId, string rungId)
        : this(document, ownerId, rungId, "Edit CALL and bindings") { }
    private LadderCallAuthoring(LadderEditorDocument document, string ownerId, string rungId, string description)
    { _transaction = new(document, description); _ownerId = ownerId; _rungId = rungId; if (Call.CallSemanticMode == LadderSemanticMode.Parameterized) EnsureIdentities(Draft); }

    /// <summary>
    /// Builds a dropped FC/FB call entirely inside the detached draft. The
    /// source rung and history remain untouched until the completed CALL form
    /// commits, so Cancel restores the exact pre-drag state and Apply is one
    /// undoable operation.
    /// </summary>
    public static LadderCallAuthoring CreateForDroppedBlock(
        LadderEditorDocument document, string ownerId, string rungId, string targetId)
    {
        var edit = new LadderCallAuthoring(document, ownerId, rungId,
            "Insert FC/FB call from Program blocks");
        var target = edit.Draft.Blocks.FirstOrDefault(block => block.Id == targetId)
            ?? throw new InvalidOperationException("Dropped block no longer exists.");
        if (target.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock))
            throw new InvalidOperationException("Only FC or FB blocks are callable.");
        if (target.Id == edit.Owner.Id)
            throw new InvalidOperationException("A block cannot call itself through this authoring route.");
        if (target.SemanticMode != LadderSemanticMode.Parameterized)
            throw new InvalidOperationException("Convert the target FC/FB to parameterized mode first.");

        var call = edit.Call;
        call.HasTerminalAction = true;
        call.HasEmptyInstructionPlaceholder = false;
        call.IsTimer = false;
        call.IsTimerReset = false;
        call.IsCounter = false;
        call.IsCounterReset = false;
        call.IsCounterLoad = false;
        call.IsNumericOperation = false;
        call.IsCall = true;
        call.IsReturn = false;
        call.IsJump = false;
        call.IsLabel = false;
        call.CallTarget = string.Empty;
        call.CallBindings = [];
        call.CallInstanceId = string.Empty;
        edit.SelectTarget(target.Id);
        return edit;
    }
    public void Cancel() => _transaction.Cancel();
    public void SelectTarget(string targetId)
    {
        var target = Draft.Blocks.Single(b => b.Id == targetId);
        if (target.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock)) throw new InvalidOperationException("Only FC or FB blocks are callable.");
        if (Call.CallTarget != targetId) { Call.CallBindings = []; Call.CallInstanceId = ""; }
        Call.CallTarget = targetId; Call.CallSemanticMode = target.SemanticMode;
        if (target.SemanticMode == LadderSemanticMode.Parameterized) EnsureIdentities(Draft);
    }
    public static void EnsureIdentities(LadderEditorDocument document)
    {
        // Reserve the complete exposed identity namespace, including aggregate leaf suffixes.
        // Keep an unambiguous legacy fallback ID so existing qualified references survive.
        // When a name is reused after rename, the old explicit ID owns that namespace.
        string[] Identities(PlcVariable tag, string id) => PlcAggregates.IsAggregate(tag.Type)
            ? new[] { id }.Concat(PlcAggregates.Expand([tag]).Select(leaf => id + leaf.Name[tag.Name.Length..])).ToArray()
            : [id];
        var reserved = document.Tags.Where(t => !string.IsNullOrWhiteSpace(t.Id)).SelectMany(t => Identities(t, t.Id)).ToHashSet(StringComparer.Ordinal);
        foreach (var tag in document.Tags.Where(t => string.IsNullOrWhiteSpace(t.Id)).ToArray())
        {
            var index = document.Tags.IndexOf(tag); var candidate = tag.Name;
            if (Identities(tag, candidate).Any(reserved.Contains))
            {
                do { candidate = "global-" + Guid.NewGuid().ToString("N"); }
                while (Identities(tag, candidate).Any(reserved.Contains));
            }
            document.Tags[index] = tag with { Id = candidate };
            foreach (var identity in Identities(tag, candidate)) reserved.Add(identity);
        }
        foreach (var block in document.Blocks)
            for (var i = 0; i < block.Interface.Count; i++)
                if (string.IsNullOrWhiteSpace(block.Interface[i].Id)) block.Interface[i] = block.Interface[i] with { Id = "formal-" + Guid.NewGuid().ToString("N") };
    }
    public void ConvertTargetToParameterized()
    {
        var target = Target ?? throw new InvalidOperationException("Choose a target first.");
        ConvertBlock(Draft, target.Id); Call.CallSemanticMode = LadderSemanticMode.Parameterized;
    }
    public static void ConvertBlock(LadderEditorDocument document, string blockId)
    {
        var block = document.Blocks.Single(b => b.Id == blockId);
        if (block.SemanticMode == LadderSemanticMode.Parameterized) return;
        if (block.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock)) throw new InvalidOperationException("Parameterized mode requires FC or FB.");
        if (document.EntryBlockId == blockId || document.Tasks.Any(t => t.EntryBlock == blockId)) throw new InvalidOperationException("Task entry blocks cannot be converted to parameterized calls.");
        EnsureIdentities(document);
        var globals = PlcAggregates.Expand(document.Tags).ToDictionary(v => v.Name, StringComparer.Ordinal);
        string Qualify(string operand)
        {
            var name = operand;
            var member = "";
            if (!globals.ContainsKey(name))
            {
                var dot = operand.LastIndexOf('.');
                if (dot > 0 && globals.TryGetValue(operand[..dot], out var declaration) && declaration.Type is PlcVariableType.Timer or PlcVariableType.Counter)
                { name = operand[..dot]; member = operand[dot..]; }
            }
            if (!globals.ContainsKey(name)) return operand; // Unresolved stays an explicit diagnostic.
            var root = document.Tags.FirstOrDefault(t => t.Name == name || PlcAggregates.IsAggregate(t.Type) && (name.StartsWith(t.Name + ".", StringComparison.Ordinal) || name.StartsWith(t.Name + "[", StringComparison.Ordinal)));
            return root is null ? operand : "global:" + root.Id + name[root.Name.Length..] + member;
        }
        EditableLadderNode Node(EditableLadderNode n) => n with { Variable = Qualify(n.Variable), RightOperand = Qualify(n.RightOperand), Children = n.Children?.Select(Node).ToArray() };
        foreach (var r in block.Rungs)
        {
            r.SetRoot(Node(r.Root)); r.CoilVariable = Qualify(r.CoilVariable); r.TimerVariable = Qualify(r.TimerVariable); r.CounterVariable = Qualify(r.CounterVariable);
            r.NumericSourceA = Qualify(r.NumericSourceA); r.NumericSourceB = Qualify(r.NumericSourceB); r.NumericSourceC = Qualify(r.NumericSourceC); r.NumericDestination = Qualify(r.NumericDestination);
        }
        block.SemanticMode = LadderSemanticMode.Parameterized;
        // Other call sites are left untouched: mode mismatches become navigable compiler diagnostics.
        // Conversion never guesses their actuals or instances.
    }
    public string CreateInstance(string name)
    {
        if (Target is not { BlockType: LadderBlockType.FunctionBlock, SemanticMode: LadderSemanticMode.Parameterized }) throw new InvalidOperationException("Instances require a parameterized FB target.");
        name = CheckInstanceName(name, "");
        var id = "instance-" + Guid.NewGuid().ToString("N"); Draft.Instances.Add(new(id, name, Target.Id));
        Draft.EnsureInstanceDataBlocks(); Call.CallInstanceId = id; return id;
    }
    public void RenameInstance(string id, string name)
    {
        var index = Draft.Instances.FindIndex(i => i.Id == id); if (index < 0) throw new InvalidOperationException("Instance no longer exists.");
        Draft.Instances[index] = Draft.Instances[index] with { Name = CheckInstanceName(name, id) };
    }
    public void AssignInstanceDataBlock(string id, int number, string name)
    {
        if (number is < 1 or > 65535) throw new InvalidOperationException("Instance DB number must be from 1 through 65535.");
        name = name.Trim();
        if (name.Length == 0 || !char.IsLetter(name[0]) && name[0] != '_'
            || name.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
            throw new InvalidOperationException("Enter a valid DB symbol: start with a letter or underscore and use only letters, digits, or underscores.");
        var index = Draft.Instances.FindIndex(instance => instance.Id == id);
        if (index < 0) throw new InvalidOperationException("Instance no longer exists.");
        var otherDbs = Draft.Instances.Where(instance => instance.Id != id).Select(instance => instance.InstanceDataBlock)
            .Concat(Draft.Blocks.SelectMany(block => block.Rungs).SelectMany(rung => new[] { rung.TimerInstanceDataBlock, rung.CounterInstanceDataBlock }))
            .Where(db => db is not null).Select(db => db!).ToArray();
        if (otherDbs.Any(db => db.Number == number)) throw new InvalidOperationException($"DB{number} is already assigned to another instance.");
        if (otherDbs.Any(db => db.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException($"DB symbol '{name}' is already assigned.");
        Draft.Instances[index] = Draft.Instances[index] with { InstanceDataBlock = new LadderInstanceDataBlock(number, name) };
    }
    private string CheckInstanceName(string name, string except)
    {
        name = name.Trim(); if (name.Length == 0 || Draft.Instances.Any(i => i.Id != except && i.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Enter a nonempty, unique instance name."); return name;
    }
    public static string? ValidateFormal(LadderBlockType blockType, LadderInterfaceParameter formal)
    {
        if (formal.Section == LadderInterfaceSection.Static && blockType != LadderBlockType.FunctionBlock) return "Static storage requires an FB.";
        var scalar = formal.Type is PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.String or PlcVariableType.Enum;
        if (!scalar && !(blockType == LadderBlockType.FunctionBlock && formal.Section == LadderInterfaceSection.Static && formal.Type is PlcVariableType.Timer or PlcVariableType.Counter))
            return formal.Type + " is unsupported for " + formal.Section + "; use scalar parameters, or FB Static TIMER/COUNTER.";
        if (formal.Type == PlcVariableType.Enum && (!PlcTextValues.IsValidDomain(formal.EnumMembers) || !(formal.EnumMembers ?? []).Contains(Convert.ToString(formal.InitialValue)))) return "ENUM needs a domain containing its initial member.";
        return null;
    }
    public void Bind(LadderCallBinding binding) => Call.CallBindings = Call.CallBindings.Where(b => b.ParameterId != binding.ParameterId).Append(binding).ToArray();
    public IReadOnlyList<CallAuthoringIssue> Validate()
    {
        var issues = new List<CallAuthoringIssue>(); var target = Target;
        if (target is null || target.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock)) return [new("target", "Choose an FC or FB target.")];
        if (Call.CallSemanticMode != target.SemanticMode) return [new("mode", "CALL mode must match its target. Choose the target or explicitly convert it.")];
        if (Call.CallSemanticMode == LadderSemanticMode.Parameterized)
        {
            if (target.BlockType == LadderBlockType.FunctionBlock && !Draft.Instances.Any(i => i.Id == Call.CallInstanceId && i.BlockId == target.Id)) issues.Add(new("instance", "Choose or create an instance of this FB."));
            foreach (var formal in target.Interface.Where(p => p.Section is LadderInterfaceSection.Input or LadderInterfaceSection.Output or LadderInterfaceSection.InOut))
                if (Call.CallBindings.Count(b => b.ParameterId == formal.Id) != 1) issues.Add(new("binding:" + formal.Id, "Exactly one actual is required for " + formal.Name + "."));
        }
        var prefix = $"$.blocks[{Draft.Blocks.IndexOf(Owner)}].networks[{Owner.Rungs.IndexOf(Call)}].call";
        var targetPrefix = $"$.blocks[{Draft.Blocks.IndexOf(target)}].interface";
        foreach (var issue in LadderCompiler.Validate(Draft.BuildProgram()).Where(i => i.Path.StartsWith(prefix, StringComparison.Ordinal) || i.Path.StartsWith(targetPrefix, StringComparison.Ordinal) || i.Code == "VC_CALL_ID"))
        {
            var field = issue.Path.Contains(".instanceId", StringComparison.Ordinal) ? "instance" : "target";
            var start = issue.Path.IndexOf(".bindings[", StringComparison.Ordinal);
            if (start >= 0 && int.TryParse(issue.Path[(start + 10)..].Split(']')[0], out var index) && index < Call.CallBindings.Count) field = "binding:" + Call.CallBindings[index].ParameterId;
            issues.Add(new(field, issue.Message));
        }
        return issues;
    }
    public bool Commit(LadderEditorHistory history, bool locked, out string error) => _transaction.TryCommit(history, locked, out error, _ => Validate().FirstOrDefault()?.Message);
}
