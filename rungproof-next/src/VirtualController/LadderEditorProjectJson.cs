using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RungProof.Next.VirtualController;

public sealed record LadderEditorProjectLoadResult(
    LadderEditorDocument? Document,
    IReadOnlyList<LadderValidationIssue> Issues)
{
    public bool IsReadable => Document is not null && Issues.Count == 0;
    public int SourceSchemaVersion { get; init; }
}

/// <summary>
/// Persistence for the mutable offline engineering project. Unlike
/// LadderProgramJson, this format deliberately preserves compiler-invalid
/// work in progress. Loading proves only that the editor document is readable;
/// Verify + Load remains the separate executable safety boundary.
/// </summary>
public static class LadderEditorProjectJson
{
    public const int SchemaVersion = 6;
    public const string Kind = "rungproof-ladder-project";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        MaxDepth = 256,
    };

    public static string Save(LadderEditorDocument document) => Save(document, contentOnly: false);

    public static bool HasUnsavedChanges(LadderEditorDocument document, string? savedJson)
    {
        if (savedJson is null) return true;
        try
        {
            var loaded = Load(savedJson);
            if (!loaded.IsReadable) return true;
            // Selecting a block is navigation. Preserve it in saved files,
            // but do not confuse it with an edit to the project contents.
            return !string.Equals(Save(loaded.Document!, contentOnly: true),
                Save(document, contentOnly: true), StringComparison.Ordinal);
        }
        catch (JsonException) { return true; }
    }

    private static string Save(LadderEditorDocument document, bool contentOnly)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.EnsureInstanceDataBlocks();
        var snapshot = document.CaptureSnapshot();
        var dto = new ProjectDto
        {
            SchemaVersion = SchemaVersion,
            Kind = Kind,
            Id = snapshot.Id,
            Name = snapshot.Name,
            SourceSceneId = snapshot.SourceSceneId,
            ScanPeriodMs = snapshot.ScanPeriod.TotalMilliseconds,
            ActiveBlockIndex = contentOnly ? 0 : snapshot.ActiveBlockIndex,
            EntryBlockId = snapshot.EntryBlockId,
            // Loading repairs the allocation cursor against imported stable IDs.
            // That bookkeeping does not change authored project content.
            NextId = contentOnly ? 0 : snapshot.NextId,
            Tags = snapshot.Tags.Select(Tag).ToList(),
            Blocks = snapshot.Blocks.Select(Block).ToList(),
            Tasks = snapshot.Tasks.Select(Task).ToList(),
            // Schema 5 keeps this legacy field for older readers. Anchor it to
            // the default table so changing the active navigation tab cannot
            // change serialized project content.
            WatchVariables = (snapshot.WatchTables?.FirstOrDefault()?.Variables
                ?? snapshot.WatchVariables).ToList(),
            WatchTables = (snapshot.WatchTables ?? []).Select(table => new WatchTableDto
            {
                Name = table.Name,
                Variables = table.Variables.ToList(),
            }).ToList(),
            // Selecting a watch table is navigation, like selecting a block.
            // Preserve it in a saved file without marking the project content dirty.
            ActiveWatchTableIndex = contentOnly ? 0 : snapshot.ActiveWatchTableIndex,
            Instances = (snapshot.Instances ?? []).ToList(),
            TagTables = (snapshot.TagTables ?? ["Default tag table"]).ToList(),
        };
        return JsonSerializer.Serialize(dto, Options);
    }

    public static LadderEditorProjectLoadResult Load(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<ProjectDto>(json, Options);
            if (dto is null)
                return Invalid("Project document must be a JSON object.");
            if (dto.SchemaVersion is not (1 or 2 or 3 or 4 or 5 or SchemaVersion))
                return Invalid($"Unsupported editor project schemaVersion '{dto.SchemaVersion}'.");
            if (!string.Equals(dto.Kind, Kind, StringComparison.Ordinal))
                return Invalid($"Unsupported project kind '{dto.Kind}'.");
            if (dto.Tags is null || dto.Blocks is null || dto.Tasks is null || dto.WatchVariables is null || dto.Instances is null)
                return Invalid("Editor project collections must not be null.");
            if (dto.SchemaVersion >= 5 && dto.WatchTables is null)
                return Invalid("Schema 5 requires watchTables.");
            if (dto.Blocks.Count == 0)
                return Invalid("Editor project must contain at least one block/routine.");
            // JSON arrays can contain null despite nonnullable DTO annotations.
            // Validate from the outside in before dereferencing nested entries.
            if (dto.Tags.Any(tag => tag is null) || dto.Tasks.Any(task => task is null)
                || dto.Blocks.Any(block => block is null) || dto.WatchVariables.Any(name => name is null)
                || dto.Instances.Any(instance => instance is null || instance.Id is null || instance.Name is null || instance.BlockId is null))
                return Invalid("Editor project collections must not contain null entries.");
            if (dto.WatchTables?.Any(table => table is null || table.Name is null || table.Variables is null
                    || table.Variables.Any(name => name is null)) == true)
                return Invalid("Watch tables and their symbols must not contain null entries.");
            if (dto.WatchTables is { Count: > 0 } && (dto.WatchTables.Any(table => string.IsNullOrWhiteSpace(table.Name))
                    || dto.WatchTables.GroupBy(table => table.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1)
                    || dto.WatchTables.Any(table => table.Variables!.GroupBy(name => name, StringComparer.Ordinal).Any(group => group.Count() > 1))))
                return Invalid("Watch-table names must be nonempty and unique; symbols must be unique within each table.");
            if (dto.Blocks.Any(block => block.Rungs is null || block.Interface is null
                    || block.Rungs.Any(rung => rung is null) || block.Interface.Any(parameter => parameter is null))
                || dto.Blocks.SelectMany(block => block.Rungs).Any(rung => rung.Branches is null
                    || rung.Branches.Any(branch => branch is null))
                || dto.Blocks.SelectMany(block => block.Rungs).SelectMany(rung => rung.Branches)
                    .Any(branch => branch.Contacts is null || branch.Contacts.Any(contact => contact is null)))
                return Invalid("Editor project block, rung, branch, and contact collections must not be null.");
            if (dto.SchemaVersion == 1 && dto.Blocks.Any(block => block.Rungs.Any(rung => rung.Branches.Count == 0)))
                return Invalid("Every network/rung must retain at least one branch path.");
            if (dto.SchemaVersion >= 2 && dto.Blocks.Any(block => block.Rungs.Any(rung => rung.Root is null)))
                return Invalid($"Schema {dto.SchemaVersion} requires a recursive root for every network.");
            if (dto.Blocks.SelectMany(b => b.Rungs).Any(r => r.CallBindings?.Any(b => b is null || b.ParameterId is null || b.Symbol is null
                || b.EnumMembers?.Any(m => m is null) == true) == true))
                return Invalid("CALL bindings must not contain null entries.");

            var issues = new List<LadderValidationIssue>();
            var tags = dto.Tags.Select((tag, index) => ParseTag(tag, index, issues)).ToArray();
            var blocks = dto.Blocks.Select((block, index) => ParseBlock(block, index, issues, dto.SchemaVersion)).ToArray();
            var tasks = dto.Tasks.Select((task, index) => ParseTask(task, index, issues)).ToArray();
            if (issues.Count > 0) return new(null, issues);
            ValidateIdentities(blocks, tasks);
            static void UniqueNonempty(IEnumerable<string> ids)
            { if (ids.Where(id => id.Length > 0).GroupBy(id => id, StringComparer.Ordinal).Any(g => g.Count() > 1)) throw new ArgumentException("Duplicate stable symbol or instance identity."); }
            UniqueNonempty(tags.Select(t => t.Id)); UniqueNonempty(dto.Instances.Select(i => i.Id));
            foreach (var block in blocks) UniqueNonempty(block.Interface.Select(p => p.Id));

            var activeBlockIndex = Math.Clamp(dto.ActiveBlockIndex, 0, blocks.Length - 1);
            var nextId = Math.Max(Math.Max(1, dto.NextId), NextAvailableStableId(blocks, tasks));
            var watchTables = dto.SchemaVersion >= 5 && dto.WatchTables is { Count: > 0 }
                ? dto.WatchTables.Select(table => new EditableWatchTable(table.Name!, table.Variables!)).ToArray()
                : [new EditableWatchTable("Watch table 1", dto.WatchVariables)];
            var snapshot = new LadderEditorSnapshot(
                dto.Id ?? string.Empty,
                dto.Name ?? string.Empty,
                dto.SourceSceneId ?? string.Empty,
                TimeSpan.FromMilliseconds(dto.ScanPeriodMs),
                tags,
                blocks,
                tasks,
                dto.WatchVariables.ToArray(),
                activeBlockIndex,
                dto.EntryBlockId ?? string.Empty,
                nextId, dto.SchemaVersion == 1 ? [] : dto.Instances,
                (IReadOnlyList<string>?)dto.TagTables
                    ?? tags.Select(tag => tag.TagTable).Distinct(StringComparer.Ordinal).DefaultIfEmpty("Default tag table").ToArray(),
                watchTables, Math.Clamp(dto.ActiveWatchTableIndex, 0, watchTables.Length - 1));
            var document = new LadderEditorDocument();
            document.RestoreSnapshot(snapshot);
            return new(document, []) { SourceSchemaVersion = dto.SchemaVersion };
        }
        catch (JsonException exception)
        {
            return Invalid($"Malformed editor project JSON: {exception.Message}");
        }
        catch (Exception exception) when (exception is InvalidOperationException or OverflowException or ArgumentException)
        {
            return Invalid($"Unreadable editor project: {exception.Message}");
        }
    }

    private static void ValidateIdentities(EditableBlock[] blocks, EditableTask[] tasks)
    {
        var blockIds = new HashSet<string>(StringComparer.Ordinal);
        var taskIds = new HashSet<string>(StringComparer.Ordinal);
        var elementIds = new HashSet<string>(StringComparer.Ordinal);
        static void Add(ISet<string> ids, string id, string path)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(path + ": ID is required.");
            if (!ids.Add(id)) throw new ArgumentException(path + ": duplicate ID '" + id + "'.");
        }
        foreach (var block in blocks)
        {
            Add(blockIds, block.Id, "blocks.id");
            foreach (var rung in block.Rungs)
            {
                Add(elementIds, rung.Id, "networks.id");
                EditableLadderTree.Validate(rung.Root, "networks[" + rung.Id + "].root", elementIds);
                if (rung.IsLabel)
                    Add(elementIds, EffectiveNetworkLabelId(rung), "networks[" + rung.Id + "].networkLabelId");
                if (rung.HasTerminalAction)
                    Add(elementIds, EffectiveActionId(rung), "networks[" + rung.Id + "].actionId");
            }
        }
        foreach (var task in tasks) Add(taskIds, task.Id, "tasks.id");
    }

    private static string EffectiveActionId(EditableRung rung) => !string.IsNullOrWhiteSpace(rung.TerminalActionId)
        ? rung.TerminalActionId : rung.Id + "-" + (rung.IsTimer ? "timer" : rung.IsTimerReset ? "timer-reset"
        : rung.IsCounter ? "counter" : rung.IsCounterReset ? "counter-reset" : rung.IsCounterLoad ? "counter-load"
        : rung.IsNumericOperation ? "numeric" : rung.IsCall ? "call" : rung.IsReturn ? "return"
        : rung.IsJump ? "jump" : "coil");
    private static string EffectiveNetworkLabelId(EditableRung rung) => !string.IsNullOrWhiteSpace(rung.NetworkLabelId)
        ? rung.NetworkLabelId : rung.Id + "-label";

    private static int NextAvailableStableId(IEnumerable<EditableBlock> blocks, IEnumerable<EditableTask> tasks)
    {
        var maximum = 0;
        void Include(string id)
        {
            var separator = id.LastIndexOf('-');
            if (separator >= 0 && int.TryParse(id.AsSpan(separator + 1), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var suffix))
                maximum = Math.Max(maximum, suffix);
        }

        foreach (var block in blocks)
        {
            Include(block.Id);
            foreach (var rung in block.Rungs)
            {
                Include(rung.Id);
                Include(EffectiveActionId(rung));
                foreach (var node in EditableLadderTree.Visit(rung.Root)) Include(node.Id);
            }
        }
        foreach (var task in tasks) Include(task.Id);
        return maximum == int.MaxValue ? int.MaxValue : maximum + 1;
    }

    private static TagDto Tag(PlcVariable source) => new()
    {
        SymbolId = source.Id,
        Name = source.Name,
        Type = source.Type.ToString(),
        Role = source.Role.ToString(),
        Initial = source.Type is PlcVariableType.Timer or PlcVariableType.Time && source.InitialValue is TimeSpan time
            ? time.TotalMilliseconds
            : source.InitialValue,
        Binding = source.Binding,
        Aggregate = source.Aggregate,
        EnumMembers = source.EnumMembers?.ToList(),
        TagTable = source.TagTable,
        Address = source.Address,
        Comment = source.Comment,
    };

    private static BlockDto Block(EditableBlock source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        BlockType = source.BlockType.ToString(),
        SemanticMode = source.SemanticMode.ToString(),
        Interface = source.Interface.Select(parameter => new InterfaceDto
        {
            Name = parameter.Name,
            Type = parameter.Type.ToString(),
            Section = parameter.Section.ToString(),
            Initial = parameter.InitialValue is TimeSpan time ? time.TotalMilliseconds : parameter.InitialValue,
            DataType = parameter.DataType,
            Id = parameter.Id, EnumMembers = parameter.EnumMembers?.ToList(),
            Retain = parameter.Retain,
            Comment = parameter.Comment,
        }).ToList(),
        Rungs = source.Rungs.Select(Rung).ToList(),
    };

    private static RungDto Rung(EditableRung source) => new()
    {
        Id = source.Id,
        Label = source.Label,
        CoilVariable = source.CoilVariable,
        CoilMode = source.CoilMode.ToString(),
        TimerVariable = source.TimerVariable,
        TimerPresetMs = source.TimerPreset.TotalMilliseconds,
        TimerPresetAssigned = source.TimerPresetAssigned ? null : false,
        TimerPresetOperand = source.TimerPresetAssigned
            && !string.Equals(source.TimerPresetOperand, LadderTimeLiteral.Format(source.TimerPreset), StringComparison.Ordinal)
                ? source.TimerPresetOperand : null,
        TimerResetOperand = source.IsTimer && source.TimerKind == LadderTimerKind.RetentiveOnDelay
            && !string.Equals(source.TimerResetOperand, "FALSE", StringComparison.OrdinalIgnoreCase)
                ? source.TimerResetOperand : null,
        TimerKind = source.TimerKind.ToString(),
        TimerInstanceDataBlock = source.TimerInstanceDataBlock,
        IsTimer = source.IsTimer,
        IsTimerReset = source.IsTimerReset,
        IsCounter = source.IsCounter,
        IsCounterReset = source.IsCounterReset,
        IsCounterLoad = source.IsCounterLoad,
        CounterKind = source.CounterKind.ToString(),
        CounterVariable = source.CounterVariable,
        CounterPreset = source.CounterPreset,
        CounterValueType = source.CounterValueType.ToString(),
        CounterInstanceDataBlock = source.CounterInstanceDataBlock,
        CounterLoadInput = source.IsCounter && source.CounterKind == LadderCounterKind.CountDown ? source.CounterLoadInput : null,
        CounterResetInput = source.IsCounter && source.CounterKind == LadderCounterKind.CountUp
            && !string.Equals(source.CounterResetInput, "FALSE", StringComparison.OrdinalIgnoreCase) ? source.CounterResetInput : null,
        IsNumericOperation = source.IsNumericOperation,
        NumericOperationKind = source.NumericOperationKind.ToString(),
        NumericSourceA = source.NumericSourceA,
        NumericSourceB = source.NumericSourceB,
        NumericSourceC = source.NumericSourceC,
        NumericDestination = source.NumericDestination,
        NumericDataType = source.NumericDataType == LadderNumericDataType.Unspecified ? null : source.NumericDataType.ToString(),
        NumericInputDataType = source.NumericInputDataType == LadderNumericDataType.Unspecified ? null : source.NumericInputDataType.ToString(),
        NumericOutputDataType = source.NumericOutputDataType == LadderNumericDataType.Unspecified ? null : source.NumericOutputDataType.ToString(),
        IsCall = source.IsCall,
        CallTarget = source.CallTarget,
        CallSemanticMode = source.CallSemanticMode.ToString(),
        CallBindings = source.CallBindings.ToList(), CallInstanceId = source.CallInstanceId,
        IsReturn = source.IsReturn,
        ReturnValueKind = source.ReturnValueKind.ToString(),
        ReturnValueOperand = source.ReturnValueOperand,
        IsJump = source.IsJump,
        IsLabel = source.IsLabel,
        NetworkLabelId = source.IsLabel ? EffectiveNetworkLabelId(source) : string.Empty,
        NetworkLabelName = source.NetworkLabelName,
        ProgramControlLabel = source.ProgramControlLabel,
        Root = source.Root,
        TerminalActionId = EffectiveActionId(source),
        HasTerminalAction = source.HasTerminalAction,
        HasEmptyInstructionPlaceholder = source.HasEmptyInstructionPlaceholder,
        LegacyEmptyBranchSemantics = source.LegacyEmptyBranchSemantics,
        Branches = [],
    };

    private static TaskDto Task(EditableTask source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Kind = source.Kind.ToString(),
        PeriodMs = source.Period.TotalMilliseconds,
        Priority = source.Priority,
        EntryBlock = source.EntryBlock,
    };

    private static PlcVariable ParseTag(TagDto dto, int index, ICollection<LadderValidationIssue> issues)
    {
        var type = ParseEnum(dto.Type, PlcVariableType.Bool, $"$.tags[{index}].type", issues);
        var role = ParseEnum(dto.Role, PlcVariableRole.Memory, $"$.tags[{index}].role", issues);
        var initial = ParseInitial(dto.Initial, type, $"$.tags[{index}].initial", issues);
        return new PlcVariable(dto.Name ?? string.Empty, type, role, initial, dto.Binding ?? string.Empty, dto.Aggregate, dto.EnumMembers, dto.SymbolId ?? string.Empty,
            string.IsNullOrWhiteSpace(dto.TagTable) ? "Default tag table" : dto.TagTable,
            dto.Address ?? string.Empty, dto.Comment ?? string.Empty);
    }

    private static EditableBlock ParseBlock(BlockDto dto, int blockIndex, ICollection<LadderValidationIssue> issues, int version)
    {
        var defaultType = blockIndex == 0 ? LadderBlockType.OrganizationBlock : LadderBlockType.Function;
        var blockType = string.IsNullOrWhiteSpace(dto.BlockType)
            ? defaultType
            : ParseEnum(dto.BlockType, defaultType, $"$.blocks[{blockIndex}].blockType", issues);
        var block = new EditableBlock(dto.Id ?? string.Empty, dto.Name ?? string.Empty, blockType);
        block.SemanticMode = version == 1 || string.IsNullOrEmpty(dto.SemanticMode) ? LadderSemanticMode.Legacy
            : ParseEnum(dto.SemanticMode, LadderSemanticMode.Legacy, $"$.blocks[{blockIndex}].semanticMode", issues);
        for (var parameterIndex = 0; parameterIndex < dto.Interface.Count; parameterIndex++)
        {
            var source = dto.Interface[parameterIndex];
            var path = $"$.blocks[{blockIndex}].interface[{parameterIndex}]";
            var type = ParseEnum(source.Type, PlcVariableType.Bool, $"{path}.type", issues);
            var section = ParseEnum(source.Section, LadderInterfaceSection.Input, $"{path}.section", issues);
            var initial = ParseInitial(source.Initial, type, $"{path}.initial", issues);
            block.Interface.Add(new LadderInterfaceParameter(source.Name ?? string.Empty, type, section, initial, source.DataType ?? string.Empty,
                source.Id ?? string.Empty, source.EnumMembers, source.Retain, source.Comment ?? string.Empty));
        }
        for (var rungIndex = 0; rungIndex < dto.Rungs.Count; rungIndex++)
            block.Rungs.Add(ParseRung(dto.Rungs[rungIndex], blockIndex, rungIndex, issues, version));
        return block;
    }

    private static EditableRung ParseRung(
        RungDto dto,
        int blockIndex,
        int rungIndex,
        ICollection<LadderValidationIssue> issues, int version)
    {
        var path = $"$.blocks[{blockIndex}].rungs[{rungIndex}]";
        var rung = new EditableRung(dto.Id ?? string.Empty, dto.Label ?? string.Empty, dto.CoilVariable ?? string.Empty)
        {
            CoilMode = ParseEnum(dto.CoilMode, LadderCoilMode.Assign, $"{path}.coilMode", issues),
            TimerVariable = dto.TimerVariable ?? string.Empty,
            TimerPreset = TimeSpan.FromMilliseconds(dto.TimerPresetMs),
            TimerPresetAssigned = dto.TimerPresetAssigned ?? true,
            TimerPresetOperand = dto.TimerPresetOperand
                ?? (dto.TimerPresetAssigned == false ? string.Empty : LadderTimeLiteral.Format(TimeSpan.FromMilliseconds(dto.TimerPresetMs))),
            TimerResetOperand = dto.TimerResetOperand ?? "FALSE",
            TimerKind = ParseEnum(dto.TimerKind, LadderTimerKind.OnDelay, $"{path}.timerKind", issues),
            TimerInstanceDataBlock = dto.TimerInstanceDataBlock,
            IsTimer = dto.IsTimer,
            IsTimerReset = dto.IsTimerReset,
            IsCounter = dto.IsCounter,
            IsCounterReset = dto.IsCounterReset,
            IsCounterLoad = dto.IsCounterLoad,
            CounterKind = ParseEnum(dto.CounterKind, LadderCounterKind.CountUp, $"{path}.counterKind", issues),
            CounterVariable = dto.CounterVariable ?? string.Empty,
            CounterPreset = dto.CounterPreset,
            CounterValueType = version == 1 || string.IsNullOrWhiteSpace(dto.CounterValueType)
                ? LadderCounterValueType.Int
                : ParseEnum(dto.CounterValueType, LadderCounterValueType.Int, $"{path}.counterValueType", issues),
            CounterInstanceDataBlock = dto.CounterInstanceDataBlock,
            CounterLoadInput = dto.CounterLoadInput ?? "FALSE",
            CounterResetInput = dto.CounterResetInput ?? "FALSE",
            IsNumericOperation = dto.IsNumericOperation,
            NumericOperationKind = ParseEnum(dto.NumericOperationKind, LadderNumericOperationKind.Move,
                $"{path}.numericOperationKind", issues),
            NumericSourceA = dto.NumericSourceA ?? string.Empty,
            NumericSourceB = dto.NumericSourceB ?? string.Empty,
            NumericSourceC = dto.NumericSourceC ?? string.Empty,
            NumericDestination = dto.NumericDestination ?? string.Empty,
            NumericDataType = string.IsNullOrWhiteSpace(dto.NumericDataType)
                ? LadderNumericDataType.Unspecified
                : ParseEnum(dto.NumericDataType, LadderNumericDataType.Unspecified,
                    $"{path}.numericDataType", issues),
            NumericInputDataType = string.IsNullOrWhiteSpace(dto.NumericInputDataType)
                ? LadderNumericDataType.Unspecified
                : ParseEnum(dto.NumericInputDataType, LadderNumericDataType.Unspecified,
                    $"{path}.numericInputDataType", issues),
            NumericOutputDataType = string.IsNullOrWhiteSpace(dto.NumericOutputDataType)
                ? LadderNumericDataType.Unspecified
                : ParseEnum(dto.NumericOutputDataType, LadderNumericDataType.Unspecified,
                    $"{path}.numericOutputDataType", issues),
            IsCall = dto.IsCall,
            CallTarget = dto.CallTarget ?? string.Empty,
            CallSemanticMode = version == 1 || string.IsNullOrEmpty(dto.CallSemanticMode) ? LadderSemanticMode.Legacy
                : ParseEnum(dto.CallSemanticMode, LadderSemanticMode.Legacy, path + ".callSemanticMode", issues),
            CallInstanceId = version == 1 ? string.Empty : dto.CallInstanceId ?? string.Empty,
            CallBindings = version == 1 ? [] : (dto.CallBindings ?? []).Select(binding => binding with
            { Literal = binding.Literal is JsonElement json ? ParseInitial(json, binding.LiteralType ?? PlcVariableType.Bool, path + ".callBindings.literal", issues) : binding.Literal }).ToArray(),
            IsReturn = dto.IsReturn,
            ReturnValueKind = string.IsNullOrWhiteSpace(dto.ReturnValueKind)
                ? LadderReturnValueKind.Rlo
                : ParseEnum(dto.ReturnValueKind, LadderReturnValueKind.Rlo,
                    $"{path}.returnValueKind", issues),
            ReturnValueOperand = dto.ReturnValueOperand ?? string.Empty,
            IsJump = dto.IsJump,
            IsLabel = dto.IsLabel,
            NetworkLabelId = dto.NetworkLabelId ?? string.Empty,
            NetworkLabelName = dto.NetworkLabelName ?? (dto.IsLabel ? dto.ProgramControlLabel : null) ?? string.Empty,
            ProgramControlLabel = dto.ProgramControlLabel ?? string.Empty,
            TerminalActionId = dto.TerminalActionId ?? string.Empty,
            HasTerminalAction = version == 1 || dto.HasTerminalAction,
            HasEmptyInstructionPlaceholder = dto.HasEmptyInstructionPlaceholder,
            LegacyEmptyBranchSemantics = version == 1 || dto.LegacyEmptyBranchSemantics,
        };
        // Schema 4 and earlier serialized LABEL as the terminal action. Migrate
        // that representation to independent network metadata without creating
        // an empty hidden coil.
        if (dto.IsLabel && dto.NetworkLabelName is null && !dto.IsTimer && !dto.IsTimerReset
            && !dto.IsCounter && !dto.IsCounterReset && !dto.IsCounterLoad
            && !dto.IsNumericOperation && !dto.IsCall && !dto.IsReturn && !dto.IsJump
            && string.IsNullOrWhiteSpace(dto.CoilVariable))
        {
            rung.HasTerminalAction = false;
            rung.NetworkLabelId = string.IsNullOrWhiteSpace(rung.NetworkLabelId)
                ? rung.TerminalActionId : rung.NetworkLabelId;
            rung.TerminalActionId = string.Empty;
        }
        if (version >= 2)
        {
            if (dto.Root is null) throw new ArgumentException(path + ".root is required.");
            rung.SetRoot(dto.Root);
            return rung;
        }
        for (var branchIndex = 0; branchIndex < dto.Branches.Count; branchIndex++)
        {
            var branchDto = dto.Branches[branchIndex];
            var branch = new EditableBranch(branchDto.Id ?? string.Empty);
            for (var contactIndex = 0; contactIndex < branchDto.Contacts.Count; contactIndex++)
            {
                var contact = branchDto.Contacts[contactIndex];
                var contactPath = $"{path}.branches[{branchIndex}].contacts[{contactIndex}]";
                branch.Contacts.Add(new EditableContact(
                    contact.Id ?? string.Empty,
                    contact.Variable ?? string.Empty,
                    contact.NormallyClosed,
                    contact.IsComparison,
                    ParseEnum(contact.CompareOperator, LadderCompareOperator.Equal,
                        $"{contactPath}.compareOperator", issues),
                    contact.RightOperand ?? string.Empty,
                    ParseEnum(contact.EdgeMode, LadderEdgeMode.None, $"{contactPath}.edgeMode", issues),
                    string.IsNullOrWhiteSpace(contact.ComparisonValueType)
                        ? LadderComparisonValueType.Unspecified
                        : ParseEnum(contact.ComparisonValueType, LadderComparisonValueType.Unspecified,
                            $"{contactPath}.comparisonValueType", issues))
                { EdgeMemoryOperand = contact.EdgeMemoryOperand });
            }
            rung.Branches.Add(branch);
        }
        // v1 generated this container ID during BuildProgram. Preserve it when
        // making the tree explicit so references and observations remain stable.
        if (rung.Root.Kind == LadderNodeKind.Parallel)
            rung.SetRoot(rung.Root with { Id = rung.Id + "-parallel" });
        return rung;
    }

    private static EditableTask ParseTask(TaskDto dto, int index, ICollection<LadderValidationIssue> issues) => new(
        dto.Id ?? string.Empty,
        dto.Name ?? string.Empty,
        ParseEnum(dto.Kind, LadderTaskKind.Continuous, $"$.tasks[{index}].kind", issues),
        TimeSpan.FromMilliseconds(dto.PeriodMs),
        dto.Priority,
        dto.EntryBlock ?? string.Empty);

    private static T ParseEnum<T>(string? value, T fallback, string path, ICollection<LadderValidationIssue> issues)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)) return parsed;
        issues.Add(new("EPJ001", path, $"Unknown {typeof(T).Name} value '{value}'."));
        return fallback;
    }

    private static object ParseInitial(
        object? value,
        PlcVariableType type,
        string path,
        ICollection<LadderValidationIssue> issues)
    {
        try
        {
            var element = value is JsonElement json ? json : JsonSerializer.SerializeToElement(value, Options);
            return type switch
            {
                PlcVariableType.Bool => element.GetBoolean(),
                PlcVariableType.String or PlcVariableType.Enum when element.ValueKind == JsonValueKind.String => element.GetString()!,
                PlcVariableType.String or PlcVariableType.Enum => throw new InvalidOperationException("Text initial value must be a JSON string."),
                PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Counter => element.GetInt64(),
                PlcVariableType.Real => element.GetDouble(),
                PlcVariableType.Time => TimeSpan.FromMilliseconds(element.GetDouble()),
                PlcVariableType.Timer => TimeSpan.FromMilliseconds(element.GetDouble()),
                PlcVariableType.Struct or PlcVariableType.Array => element.Clone(),
                _ => false,
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        {
            issues.Add(new("EPJ001", path,
                $"Initial value is not valid for {type.ToString().ToUpperInvariant()}."));
            return type switch
            {
                PlcVariableType.Bool => false,
                PlcVariableType.Real => 0.0,
                PlcVariableType.Timer => TimeSpan.Zero,
                PlcVariableType.Time => TimeSpan.Zero,
                _ => 0L,
            };
        }
    }

    private static LadderEditorProjectLoadResult Invalid(string message) =>
        new(null, [new LadderValidationIssue("EPJ001", "$", message)]);

    private sealed class ProjectDto
    {
        public int SchemaVersion { get; set; }
        public string? Kind { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? SourceSceneId { get; set; }
        public double ScanPeriodMs { get; set; }
        public int ActiveBlockIndex { get; set; }
        public string? EntryBlockId { get; set; }
        public int NextId { get; set; }
        public List<TagDto> Tags { get; set; } = [];
        public List<BlockDto> Blocks { get; set; } = [];
        public List<TaskDto> Tasks { get; set; } = [];
        public List<string> WatchVariables { get; set; } = [];
        public List<WatchTableDto>? WatchTables { get; set; }
        public int ActiveWatchTableIndex { get; set; }
        public List<LadderInstance> Instances { get; set; } = [];
        public List<string>? TagTables { get; set; }
    }

    private sealed class WatchTableDto
    {
        public string? Name { get; set; }
        public List<string>? Variables { get; set; }
    }

    private sealed class TagDto
    {
        public string? SymbolId { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Role { get; set; }
        public object? Initial { get; set; }
        public string? Binding { get; set; }
        public PlcAggregateSchema? Aggregate { get; set; }
        public List<string>? EnumMembers { get; set; }
        public string? TagTable { get; set; }
        public string? Address { get; set; }
        public string? Comment { get; set; }
    }

    private sealed class BlockDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? BlockType { get; set; }
        public string? SemanticMode { get; set; }
        public List<InterfaceDto> Interface { get; set; } = [];
        public List<RungDto> Rungs { get; set; } = [];
    }

    private sealed class InterfaceDto
    {
        public string? Id { get; set; }
        public List<string>? EnumMembers { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Section { get; set; }
        public object? Initial { get; set; }
        public string? DataType { get; set; }
        public bool Retain { get; set; }
        public string? Comment { get; set; }
    }

    private sealed class RungDto
    {
        public string? Id { get; set; }
        public string? Label { get; set; }
        public string? CoilVariable { get; set; }
        public string? CoilMode { get; set; }
        public string? TimerVariable { get; set; }
        public double TimerPresetMs { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TimerPresetAssigned { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TimerPresetOperand { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TimerResetOperand { get; set; }
        public string? TimerKind { get; set; }
        public LadderInstanceDataBlock? TimerInstanceDataBlock { get; set; }
        public bool IsTimer { get; set; }
        public bool IsTimerReset { get; set; }
        public bool IsCounter { get; set; }
        public bool IsCounterReset { get; set; }
        public bool IsCounterLoad { get; set; }
        public string? CounterKind { get; set; }
        public string? CounterVariable { get; set; }
        public long CounterPreset { get; set; }
        public string? CounterValueType { get; set; }
        public LadderInstanceDataBlock? CounterInstanceDataBlock { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CounterLoadInput { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CounterResetInput { get; set; }
        public bool IsNumericOperation { get; set; }
        public string? NumericOperationKind { get; set; }
        public string? NumericSourceA { get; set; }
        public string? NumericSourceB { get; set; }
        public string? NumericSourceC { get; set; }
        public string? NumericDestination { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NumericDataType { get; set; }
        public string? NumericInputDataType { get; set; }
        public string? NumericOutputDataType { get; set; }
        public bool IsCall { get; set; }
        public string? CallTarget { get; set; }
        public string? CallSemanticMode { get; set; }
        public string? CallInstanceId { get; set; }
        public List<LadderCallBinding>? CallBindings { get; set; }
        public bool IsReturn { get; set; }
        public string? ReturnValueKind { get; set; }
        public string? ReturnValueOperand { get; set; }
        public bool IsJump { get; set; }
        public bool IsLabel { get; set; }
        public string? NetworkLabelId { get; set; }
        public string? NetworkLabelName { get; set; }
        public string? ProgramControlLabel { get; set; }
        public EditableLadderNode? Root { get; set; }
        public string? TerminalActionId { get; set; }
        public bool HasTerminalAction { get; set; } = true;
        public bool HasEmptyInstructionPlaceholder { get; set; }
        public bool LegacyEmptyBranchSemantics { get; set; }
        public List<BranchDto> Branches { get; set; } = [];
    }

    private sealed class BranchDto
    {
        public string? Id { get; set; }
        public List<ContactDto> Contacts { get; set; } = [];
    }

    private sealed class ContactDto
    {
        public string? Id { get; set; }
        public string? Variable { get; set; }
        public bool NormallyClosed { get; set; }
        public bool IsComparison { get; set; }
        public string? CompareOperator { get; set; }
        public string? RightOperand { get; set; }
        public string? EdgeMode { get; set; }
        public string? EdgeMemoryOperand { get; set; }
        public string? ComparisonValueType { get; set; }
    }

    private sealed class TaskDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Kind { get; set; }
        public double PeriodMs { get; set; }
        public int Priority { get; set; }
        public string? EntryBlock { get; set; }
    }
}
