using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RungProof.Next.VirtualController;

public sealed record CompiledLadderProgram(
    LadderProgram Source,
    IReadOnlyDictionary<string, PlcVariable> Variables
);

public sealed record LadderCompileResult(
    CompiledLadderProgram? Program,
    IReadOnlyList<LadderValidationIssue> Issues
)
{
    public bool IsValid => Program is not null && Issues.Count == 0;
}

public static class LadderCompiler
{
    public static LadderCompileResult Compile(LadderProgram program)
    {
        var issues = Validate(program);
        return issues.Count == 0
            ? new(new CompiledLadderProgram(
                program,
                PlcAggregates.Expand(program.Variables).Select(item => item with { EnumMembers = item.EnumMembers is null ? null : Array.AsReadOnly(item.EnumMembers.ToArray()) }).ToDictionary(item => item.Name, StringComparer.Ordinal)), [])
            : new(null, issues);
    }

    public static IReadOnlyList<LadderValidationIssue> Validate(LadderProgram program)
    {
        var issues = new List<LadderValidationIssue>();
        try
        {
            issues.AddRange(LadderCallBindings.Validate(program));
            if (issues.Count > 0) return issues;
            program = LadderCallBindings.LowerForValidation(program);
        }
        catch (ArgumentException exception) { return [new("VC002", "$.variables", exception.Message)]; }
        try { program = program with { Variables = PlcAggregates.Expand(program.Variables), WatchVariables = program.WatchVariables?.Where(name => !program.Variables.Any(v => v.Name == name && PlcAggregates.IsAggregate(v.Type))).ToArray() }; }
        catch (ArgumentException exception) { return [new("VC002", "$.variables", exception.Message)]; }
        if (program.SchemaVersion is not (1 or 2))
            issues.Add(new("VC100", "$.schemaVersion", "Only schemaVersion 1 and 2 are supported."));
        if (string.IsNullOrWhiteSpace(program.Id))
            issues.Add(new("VC100", "$.id", "Program ID is required."));
        if (string.IsNullOrWhiteSpace(program.Name))
            issues.Add(new("VC100", "$.name", "Program name is required."));
        if (!program.Language.Equals("LD", StringComparison.OrdinalIgnoreCase))
            issues.Add(new("VC100", "$.language", "Phase 1 supports Ladder Diagram (LD) only."));
        if (program.ScanPeriod < TimeSpan.FromMilliseconds(1))
            issues.Add(new("VC100", "$.scanPeriodMs", "scanPeriodMs must be at least 1 ms for the offline simulator."));

        var blocks = program.Blocks is { Count: > 0 }
            ? program.Blocks
            : [new LadderBlock("main", program.Name, program.Networks)];
        var blockNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < blocks.Count; index++)
        {
            var name = blocks[index].Name;
            if (!string.IsNullOrWhiteSpace(name) && !blockNames.Add(name))
                issues.Add(new("VC004", $"$.blocks[{index}].name", $"Duplicate CPU symbol '{name}' (block/routine names are case-insensitive)."));
        }

        var variables = new Dictionary<string, PlcVariable>(StringComparer.Ordinal);
        var variableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < program.Variables.Count; index++)
        {
            var variable = program.Variables[index];
            var path = $"$.variables[{index}]";
            if (string.IsNullOrWhiteSpace(variable.Name))
                issues.Add(new("VC001", $"{path}.name", "Variable name is required."));
            else
            {
                if (!variableNames.Add(variable.Name))
                    issues.Add(new("VC004", $"{path}.name", $"Duplicate CPU symbol '{variable.Name}' (PLC tag names are case-insensitive across tag tables)."));
                if (blockNames.Contains(variable.Name))
                    issues.Add(new("VC004", $"{path}.name", $"PLC tag '{variable.Name}' conflicts with a block/routine name."));
                variables.TryAdd(variable.Name, variable);
            }
            if (variable.Type is not (PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.Time or PlcVariableType.Timer or PlcVariableType.Counter or PlcVariableType.String or PlcVariableType.Enum))
                issues.Add(new("VC002", $"{path}.type", $"The current runtime supports BOOL, INT, DINT, REAL, TIME, TIMER, and COUNTER; '{variable.Name}' is {variable.Type}."));
            if (PlcTextValues.IsText(variable.Type) && !PlcTextValues.IsValid(variable, variable.InitialValue))
                issues.Add(new("VC002", $"{path}.initial", "STRING requires at most 255 characters; ENUM requires a valid domain and named member."));
            if (variable.Type != PlcVariableType.Enum && variable.EnumMembers is not null)
                issues.Add(new("VC002", $"{path}.enumMembers", "Only ENUM declarations may define members."));
            if (variable.Type == PlcVariableType.Bool && variable.InitialValue is not bool)
                issues.Add(new("VC002", $"{path}.initial", $"BOOL variable '{variable.Name}' requires a Boolean initial value."));
            if (variable.Type is PlcVariableType.Int or PlcVariableType.DInt && variable.InitialValue is not (int or long))
                issues.Add(new("VC002", $"{path}.initial", $"{variable.Type.ToString().ToUpperInvariant()} variable '{variable.Name}' requires an integer initial value."));
            var integerInitial = variable.InitialValue switch
            {
                int value => (long?)value,
                long value => value,
                _ => null,
            };
            if (variable.Type == PlcVariableType.Int && integerInitial is { } intInitial
                && (intInitial < short.MinValue || intInitial > short.MaxValue))
                issues.Add(new("VC002", $"{path}.initial", $"INT variable '{variable.Name}' initial value must be from {short.MinValue} to {short.MaxValue}."));
            if (variable.Type == PlcVariableType.DInt && integerInitial is { } dIntInitial
                && (dIntInitial < int.MinValue || dIntInitial > int.MaxValue))
                issues.Add(new("VC002", $"{path}.initial", $"DINT variable '{variable.Name}' initial value must be from {int.MinValue} to {int.MaxValue}."));
            if (variable.Type == PlcVariableType.Real && variable.InitialValue is not (double or float or int or long))
                issues.Add(new("VC002", $"{path}.initial", $"REAL variable '{variable.Name}' requires a numeric initial value."));
            var realInitial = variable.InitialValue switch
            {
                double value => (double?)value,
                float value => value,
                int value => value,
                long value => value,
                _ => null,
            };
            if (variable.Type == PlcVariableType.Real && realInitial is { } finiteInitial
                && !double.IsFinite(finiteInitial))
                issues.Add(new("VC002", $"{path}.initial", $"REAL variable '{variable.Name}' requires a finite initial value."));
            if (variable.Type == PlcVariableType.Time
                && (variable.InitialValue is not TimeSpan timeInitial || timeInitial < TimeSpan.Zero
                    || timeInitial.TotalMilliseconds > int.MaxValue
                    || timeInitial.Ticks % TimeSpan.TicksPerMillisecond != 0))
                issues.Add(new("VC002", $"{path}.initial", $"TIME variable '{variable.Name}' requires an integer millisecond value from 0 through {int.MaxValue} ms."));
            if (variable.Type == PlcVariableType.Timer && variable.Role != PlcVariableRole.Memory)
                issues.Add(new("VC005", $"{path}.role", $"TIMER instance '{variable.Name}' must use the Memory role."));
            if (variable.Type == PlcVariableType.Counter && variable.Role != PlcVariableRole.Memory)
                issues.Add(new("VC005", $"{path}.role", $"COUNTER instance '{variable.Name}' must use the Memory role."));
        }
        var watched = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < (program.WatchVariables?.Count ?? 0); index++)
        {
            var name = program.WatchVariables![index];
            var path = $"$.watchVariables[{index}]";
            if (!variables.ContainsKey(name))
                issues.Add(new("VC001", path, $"Watch-table symbol '{name}' is not declared."));
            else if (!watched.Add(name))
                issues.Add(new("VC004", path, $"Duplicate watch-table symbol '{name}'."));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var blockIds = new HashSet<string>(StringComparer.Ordinal);
        var timerInstructionPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var edgeMemoryOperands = new HashSet<string>(StringComparer.Ordinal);
        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            var block = blocks[blockIndex];
            var blockPath = $"$.blocks[{blockIndex}]";
            var blockLabels = new Dictionary<string, int>(StringComparer.Ordinal);
            AddId(block.Id, $"{blockPath}.id", ids, issues);
            if (!blockIds.Add(block.Id))
                issues.Add(new("VC004", $"{blockPath}.id", $"Duplicate block ID '{block.Id}'."));
            if (string.IsNullOrWhiteSpace(block.Name))
                issues.Add(new("VC100", $"{blockPath}.name", "Block name is required."));
            for (var labelIndex = 0; labelIndex < block.Networks.Count; labelIndex++)
            {
                var label = block.Networks[labelIndex].LabelInstruction;
                if (label is null) continue;
                var labelPath = $"{blockPath}.networks[{labelIndex}].label";
                if (string.IsNullOrWhiteSpace(label.Name))
                    issues.Add(new("VC014", $"{labelPath}.name", "LBL/LABEL name is required."));
                else if (!blockLabels.TryAdd(label.Name, labelIndex))
                    issues.Add(new("VC014", $"{labelPath}.name", $"Duplicate block-local label '{label.Name}'."));
            }
            for (var index = 0; index < block.Networks.Count; index++)
            {
                var network = block.Networks[index];
                var path = $"{blockPath}.networks[{index}]";
                AddId(network.Id, $"{path}.id", ids, issues);
                ValidateNode(network.Logic, $"{path}.logic", variables, ids, issues, program.SchemaVersion,
                    edgeMemoryOperands: edgeMemoryOperands);
                var outputCount = (network.Coil is null ? 0 : 1)
                    + (network.Timer is null ? 0 : 1)
                    + (network.Counter is null ? 0 : 1)
                    + (network.CounterReset is null ? 0 : 1)
                    + (network.NumericOperation is null ? 0 : 1)
                    + (network.Call is null ? 0 : 1)
                    + (network.Return is null ? 0 : 1)
                    + (network.CounterLoad is null ? 0 : 1)
                    + (network.TimerReset is null ? 0 : 1)
                    + (network.Jump is null ? 0 : 1);
                // TIA LABEL identifies the destination network; it is not an
                // output instruction and may coexist with the network action.
                // Retain label-only networks as a readable legacy no-op.
                if (outputCount != 1 && !(outputCount == 0 && network.LabelInstruction is not null))
                    issues.Add(new("VC006", path, "A network requires exactly one output instruction: coil, timer/reset, counter reset/load, numeric operation, call, return, or jump. LABEL is network metadata."));
                if (network.Coil is not null)
                {
                    AddId(network.Coil.Id, $"{path}.coil.id", ids, issues);
                    if (!variables.TryGetValue(network.Coil.Variable, out var coilVariable))
                        issues.Add(new("VC001", $"{path}.coil.variable", $"Unknown coil variable '{network.Coil.Variable}'."));
                    else if (coilVariable.Type != PlcVariableType.Bool)
                        issues.Add(new("VC002", $"{path}.coil.variable", $"Coil target '{coilVariable.Name}' must be BOOL."));
                    else if (coilVariable.Role == PlcVariableRole.Input)
                        issues.Add(new("VC005", $"{path}.coil.variable", $"Input '{coilVariable.Name}' is not a valid coil target."));
                }
                if (network.Timer is not null)
                {
                    AddId(network.Timer.Id, $"{path}.timer.id", ids, issues);
                    if (!Enum.IsDefined(network.Timer.Kind))
                        issues.Add(new("VC100", $"{path}.timer.kind", $"Unknown timer kind '{network.Timer.Kind}'."));
                    if (!variables.TryGetValue(network.Timer.Variable, out var timerVariable))
                        issues.Add(new("VC001", $"{path}.timer.variable", $"Unknown timer instance '{network.Timer.Variable}'."));
                    else if (timerVariable.Type != PlcVariableType.Timer)
                        issues.Add(new("VC002", $"{path}.timer.variable", $"{network.Timer.Kind} instance '{timerVariable.Name}' must be TIMER."));
                    if (!network.Timer.PresetAssigned)
                        issues.Add(new("VC007", $"{path}.timer.presetMs", $"{network.Timer.Kind} preset PT is not assigned."));
                    else if (network.Timer.Preset <= TimeSpan.Zero)
                        issues.Add(new("VC007", $"{path}.timer.presetMs", $"{network.Timer.Kind} preset must be greater than zero."));
                    else if (network.Timer.PresetOperand is { } presetOperand
                        && !LadderTimeLiteral.TryParse(presetOperand, out _))
                    {
                        if (!variables.TryGetValue(presetOperand, out var presetVariable))
                            issues.Add(new("VC001", $"{path}.timer.presetOperand", $"Unknown TIME preset operand '{presetOperand}'."));
                        else if (presetVariable.Type != PlcVariableType.Time)
                            issues.Add(new("VC002", $"{path}.timer.presetOperand", $"Timer PT operand '{presetOperand}' must be TIME, not {presetVariable.Type.ToString().ToUpperInvariant()}."));
                        else if (presetVariable.InitialValue is not TimeSpan initialTime || initialTime <= TimeSpan.Zero)
                            issues.Add(new("VC007", $"{path}.timer.presetOperand", $"Timer PT TIME tag '{presetOperand}' must have an initial value greater than zero."));
                    }
                    else if (network.Timer.PresetOperand is { } literalOperand
                        && LadderTimeLiteral.TryParse(literalOperand, out var parsedLiteral)
                        && parsedLiteral != network.Timer.Preset)
                        issues.Add(new("VC007", $"{path}.timer.presetMs",
                            $"PT operand '{literalOperand}' does not match the timer preset."));
                    if (network.Timer.Kind == LadderTimerKind.RetentiveOnDelay)
                    {
                        var resetOperand = network.Timer.ResetOperand ?? "FALSE";
                        if (string.IsNullOrWhiteSpace(resetOperand))
                            issues.Add(new("VC007", $"{path}.timer.resetOperand", "TONR reset input R is not assigned."));
                        else
                            ValidateBooleanOperand(resetOperand, $"{path}.timer.resetOperand", variables, issues);
                    }
                    if (timerInstructionPaths.TryGetValue(network.Timer.Variable, out var firstTimerPath))
                        issues.Add(new("VC013", $"{path}.timer.variable",
                            $"TIMER instance '{network.Timer.Variable}' is already owned by {firstTimerPath}; one timer instruction must own each instance."));
                    else
                        timerInstructionPaths[network.Timer.Variable] = $"{path}.timer";
                }
                if (network.TimerReset is not null)
                {
                    AddId(network.TimerReset.Id, $"{path}.timerReset.id", ids, issues);
                    if (!variables.TryGetValue(network.TimerReset.Variable, out var timerVariable))
                        issues.Add(new("VC001", $"{path}.timerReset.variable", $"Unknown timer instance '{network.TimerReset.Variable}'."));
                    else if (timerVariable.Type != PlcVariableType.Timer)
                        issues.Add(new("VC002", $"{path}.timerReset.variable", $"Timer reset instance '{timerVariable.Name}' must be TIMER."));
                }
                if (network.Counter is not null)
                {
                    AddId(network.Counter.Id, $"{path}.counter.id", ids, issues);
                    ValidateCounterInstance(network.Counter.Variable, $"{path}.counter.variable", variables, issues);
                    if (network.Counter.Kind == LadderCounterKind.CountDown)
                        ValidateBooleanOperand(network.Counter.LoadInput, $"{path}.counter.loadInput", variables, issues);
                    else
                        ValidateBooleanOperand(network.Counter.ResetInput, $"{path}.counter.resetInput", variables, issues);
                    if (network.Counter.ValueType == LadderCounterValueType.Unspecified)
                        issues.Add(new("VC007", $"{path}.counter.valueType", "Counter data type is not assigned."));
                    if (network.Counter.Preset <= 0)
                        issues.Add(new("VC008", $"{path}.counter.preset", $"{network.Counter.Kind} preset must be greater than zero."));
                    if (network.Counter.ValueType != LadderCounterValueType.Unspecified
                        && network.Counter.Preset > LadderCounterValueTypeRange.Maximum(network.Counter.ValueType))
                        issues.Add(new("VC008", $"{path}.counter.preset", $"Preset {network.Counter.Preset} exceeds the {network.Counter.ValueType} counter range."));
                }
                if (network.CounterReset is not null)
                {
                    AddId(network.CounterReset.Id, $"{path}.counterReset.id", ids, issues);
                    ValidateCounterInstance(network.CounterReset.Variable, $"{path}.counterReset.variable", variables, issues);
                }
                if (network.CounterLoad is not null)
                {
                    AddId(network.CounterLoad.Id, $"{path}.counterLoad.id", ids, issues);
                    ValidateCounterInstance(network.CounterLoad.Variable, $"{path}.counterLoad.variable", variables, issues);
                    if (network.CounterLoad.Preset <= 0)
                        issues.Add(new("VC008", $"{path}.counterLoad.preset", "Counter load preset must be greater than zero."));
                    if (network.CounterLoad.ValueType == LadderCounterValueType.Unspecified)
                        issues.Add(new("VC007", $"{path}.counterLoad.valueType", "Counter data type is not assigned."));
                    else if (network.CounterLoad.Preset > LadderCounterValueTypeRange.Maximum(network.CounterLoad.ValueType))
                        issues.Add(new("VC008", $"{path}.counterLoad.preset", $"Preset {network.CounterLoad.Preset} exceeds the {network.CounterLoad.ValueType} counter range."));
                }
                if (network.NumericOperation is not null)
                {
                    var operation = network.NumericOperation;
                    AddId(operation.Id, $"{path}.numericOperation.id", ids, issues);
                    if (!Enum.IsDefined(operation.Kind))
                        issues.Add(new("VC100", $"{path}.numericOperation.operation", $"Unknown numeric operation '{operation.Kind}'."));
                    if (!Enum.IsDefined(operation.DataType))
                        issues.Add(new("VC100", $"{path}.numericOperation.dataType", $"Unknown numeric instruction data type '{operation.DataType}'."));
                    if (LadderNumericOperationRules.HasTiaDualDataTypeSelectors(operation.Kind))
                    {
                        if (!Enum.IsDefined(operation.InputDataType))
                            issues.Add(new("VC100", $"{path}.numericOperation.inputDataType", $"Unknown {operation.Kind} input data type '{operation.InputDataType}'."));
                        if (!Enum.IsDefined(operation.OutputDataType))
                            issues.Add(new("VC100", $"{path}.numericOperation.outputDataType", $"Unknown {operation.Kind} output data type '{operation.OutputDataType}'."));
                    }
                    else if (LadderNumericOperationRules.HasTiaDataTypeSelector(operation.Kind))
                    {
                        if (!LadderNumericOperationRules.SupportsTiaDataType(operation.Kind, operation.DataType))
                            issues.Add(new("VC002", $"{path}.numericOperation.dataType",
                                $"{operation.Kind.ToString().ToUpperInvariant()} does not support the selected simulator data type '{operation.DataType}'."));
                        else
                            ValidateArithmeticDataType(operation, path, variables, issues);
                    }
                    if (operation.Kind == LadderNumericOperationKind.FormatText
                        || operation.Kind == LadderNumericOperationKind.Move
                            && variables.TryGetValue(operation.Destination, out var textDestination) && PlcTextValues.IsText(textDestination.Type))
                        ValidateTextOperation(operation, path, variables, issues);
                    else
                    {
                        ValidateNumericOperand(operation.SourceA, $"{path}.numericOperation.sourceA", variables, issues);
                        if (LadderNumericOperationRules.RequiresSourceB(operation.Kind))
                            ValidateNumericOperand(operation.SourceB, $"{path}.numericOperation.sourceB", variables, issues);
                        if (LadderNumericOperationRules.RequiresSourceC(operation.Kind))
                            ValidateNumericOperand(operation.SourceC, $"{path}.numericOperation.sourceC", variables, issues);
                        if (!variables.TryGetValue(operation.Destination, out var destination))
                            issues.Add(new("VC001", $"{path}.numericOperation.destination", $"Unknown numeric destination '{operation.Destination}'."));
                        else if (destination.Type is not (PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real))
                            issues.Add(new("VC002", $"{path}.numericOperation.destination", $"Numeric destination '{operation.Destination}' must be INT, DINT, or REAL."));
                        else if (destination.Role == PlcVariableRole.Input)
                            issues.Add(new("VC005", $"{path}.numericOperation.destination", $"Input '{operation.Destination}' is not a writable numeric destination."));
                        if (operation.Kind == LadderNumericOperationKind.Convert)
                            ValidateConvertTypes(operation, path, variables, issues);
                        if (operation.Kind == LadderNumericOperationKind.Truncate)
                            ValidateTruncateDataType(operation, path, variables, issues);
                        if (LadderNumericOperationRules.IsRoundingConversion(operation.Kind))
                            ValidateRoundingConversionTypes(operation, path, variables, issues);
                        if (operation.Kind is LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale)
                            ValidateScalingTypes(operation, path, variables, issues);
                        if (operation.Kind == LadderNumericOperationKind.Move
                            && variables.TryGetValue(operation.Destination, out var moveDestination)
                            && TryGetMoveNumericSourceType(operation.SourceA, variables) is { } moveSourceType
                            && moveSourceType != moveDestination.Type)
                            issues.Add(new("VC002", $"{path}.numericOperation.sourceA",
                                $"MOVE source type {moveSourceType} must match destination type {moveDestination.Type} in the supported IEC-checked numeric subset; use CONVERT for a type conversion."));
                        if (operation.Kind == LadderNumericOperationKind.SquareRoot
                            && double.TryParse(operation.SourceA, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var radicand)
                            && radicand < 0)
                            issues.Add(new("VC009", $"{path}.numericOperation.sourceA", "SQRT literal source cannot be negative."));
                        if (LadderNumericOperationRules.IsScientificUnaryMath(operation.Kind)
                            && double.TryParse(operation.SourceA, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var unarySource)
                            && !LadderNumericOperationRules.IsDomainValid(operation.Kind, unarySource))
                            issues.Add(new("VC009", $"{path}.numericOperation.sourceA", $"{operation.Kind} literal source is outside its mathematical domain."));
                        if (operation.Kind == LadderNumericOperationKind.Exponentiate
                            && double.TryParse(operation.SourceA, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var powerBase)
                            && double.TryParse(operation.SourceB, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var exponent)
                            && !LadderNumericOperationRules.IsDomainValid(operation.Kind, powerBase, exponent))
                            issues.Add(new("VC009", $"{path}.numericOperation", "EXPT literal operands are outside the real-number domain."));
                        if (operation.Kind is LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale
                            && double.TryParse(operation.SourceA, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var rangeMinimum)
                            && double.TryParse(operation.SourceC, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var rangeMaximum)
                            && !LadderNumericOperationRules.IsDomainValid(operation.Kind, rangeMinimum, 0, rangeMaximum))
                            issues.Add(new("VC009", $"{path}.numericOperation", $"{operation.Kind} requires MIN to be less than MAX."));
                    }
                }
                if (network.Call is not null)
                {
                    AddId(network.Call.Id, $"{path}.call.id", ids, issues);
                    if (!blocks.Any(candidate => candidate.Id == network.Call.TargetBlock))
                        issues.Add(new("VC010", $"{path}.call.targetBlock", $"Unknown call target block '{network.Call.TargetBlock}'."));
                }
                if (network.Return is not null)
                {
                    AddId(network.Return.Id, $"{path}.return.id", ids, issues);
                    if (!Enum.IsDefined(network.Return.ValueKind))
                        issues.Add(new("VC100", $"{path}.return.valueKind",
                            $"Unknown RET return-value kind '{network.Return.ValueKind}'."));
                    else if (network.Return.ValueKind == LadderReturnValueKind.Operand)
                    {
                        if (string.IsNullOrWhiteSpace(network.Return.Operand))
                            issues.Add(new("VC007", $"{path}.return.operand",
                                "RET Value requires a BOOL operand."));
                        else
                            ValidateBooleanOperand(network.Return.Operand, $"{path}.return.operand", variables, issues);
                    }
                }
                if (network.Jump is not null)
                {
                    AddId(network.Jump.Id, $"{path}.jump.id", ids, issues);
                    if (string.IsNullOrWhiteSpace(network.Jump.TargetLabel))
                        issues.Add(new("VC014", $"{path}.jump.targetLabel", "JMP target label is required."));
                    else if (!blockLabels.ContainsKey(network.Jump.TargetLabel))
                        issues.Add(new("VC014", $"{path}.jump.targetLabel",
                            $"Unknown block-local jump label '{network.Jump.TargetLabel}'."));
                }
                if (network.LabelInstruction is not null)
                    AddId(network.LabelInstruction.Id, $"{path}.label.id", ids, issues);
            }
        }
        ValidateEdgeMemoryAddresses(blocks, variables, issues);
        var entryBlock = string.IsNullOrWhiteSpace(program.EntryBlock) ? blocks[0].Id : program.EntryBlock;
        if (!blockIds.Contains(entryBlock))
            issues.Add(new("VC010", "$.entryBlock", $"Unknown entry block '{entryBlock}'."));
        else if (blocks.First(block => block.Id == entryBlock).BlockType == LadderBlockType.DataBlock)
            issues.Add(new("VC010", "$.entryBlock", "A data block contains declarations and cannot be the executable program entry."));
        var tasks = program.Tasks is { Count: > 0 }
            ? program.Tasks
            : [new LadderTask("main-task", "MainTask", LadderTaskKind.Continuous, program.ScanPeriod, 10, entryBlock)];
        var taskNames = new HashSet<string>(StringComparer.Ordinal);
        for (var taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
        {
            var task = tasks[taskIndex];
            var path = $"$.tasks[{taskIndex}]";
            AddId(task.Id, $"{path}.id", ids, issues);
            if (string.IsNullOrWhiteSpace(task.Name))
                issues.Add(new("VC012", $"{path}.name", "Task name is required."));
            else if (!taskNames.Add(task.Name))
                issues.Add(new("VC012", $"{path}.name", $"Duplicate task name '{task.Name}'."));
            if (!blockIds.Contains(task.EntryBlock))
                issues.Add(new("VC012", $"{path}.entryBlock", $"Unknown task entry block '{task.EntryBlock}'."));
            else if (blocks.First(block => block.Id == task.EntryBlock).BlockType == LadderBlockType.DataBlock)
                issues.Add(new("VC012", $"{path}.entryBlock", "A data block contains declarations and cannot be a task entry."));
            if (task.Priority < 0)
                issues.Add(new("VC012", $"{path}.priority", "Task priority must be zero or greater; lower values execute first."));
            if (task.Kind == LadderTaskKind.Periodic)
            {
                if (task.Period < program.ScanPeriod)
                    issues.Add(new("VC012", $"{path}.periodMs", "Periodic task period cannot be shorter than the base scan period."));
                else
                {
                    var ratio = task.Period.TotalMilliseconds / program.ScanPeriod.TotalMilliseconds;
                    if (Math.Abs(ratio - Math.Round(ratio)) > 1e-9)
                        issues.Add(new("VC012", $"{path}.periodMs", "Periodic task period must be an integer multiple of the base scan period."));
                }
            }
        }
        ValidateInstanceDataBlocks(program with { Blocks = blocks }, issues);
        ValidateCallGraph(blocks, issues);
        return issues;
    }

    private static void ValidateEdgeMemoryAddresses(
        IReadOnlyList<LadderBlock> blocks,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        var seenAddresses = new Dictionary<string, (string Operand, string Path)>(StringComparer.OrdinalIgnoreCase);
        static IEnumerable<LadderNode> Visit(LadderNode node)
        {
            yield return node;
            foreach (var child in node.Children ?? [])
                foreach (var descendant in Visit(child)) yield return descendant;
        }

        foreach (var blockIndex in Enumerable.Range(0, blocks.Count))
        {
            var block = blocks[blockIndex];
            for (var networkIndex = 0; networkIndex < block.Networks.Count; networkIndex++)
            {
                var network = block.Networks[networkIndex];
                var networkPath = $"$.blocks[{blockIndex}].networks[{networkIndex}].logic";
                foreach (var node in Visit(network.Logic))
                {
                    if (string.IsNullOrWhiteSpace(node.EdgeMemoryOperand)
                        || !variables.TryGetValue(node.EdgeMemoryOperand, out var tag)
                        || string.IsNullOrWhiteSpace(tag.Address)) continue;

                    var address = tag.Address.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).TrimStart('%');
                    if (!Regex.IsMatch(address, @"^(M\d+\.[0-7]|DB\d+\.DBX\d+\.[0-7])$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        issues.Add(new("VC100", $"{networkPath}.edgeMemory",
                            $"Edge-memory BOOL '{tag.Name}' must use an M bit or DBX bit address; '{tag.Address}' is not an allowed absolute storage area."));
                        continue;
                    }

                    if (seenAddresses.TryGetValue(address, out var first))
                        issues.Add(new("VC004", $"{networkPath}.edgeMemory",
                            $"P/N edge-memory address '{tag.Address}' is already used by '{first.Operand}' at {first.Path}."));
                    else
                        seenAddresses.Add(address, (tag.Name, $"{networkPath}.edgeMemory"));
                }
            }
        }
    }

    private static void ValidateInstanceDataBlocks(
        LadderProgram program,
        ICollection<LadderValidationIssue> issues)
    {
        var byNumber = new Dictionary<int, (string Owner, string Name, string Path)>();
        var byName = new Dictionary<string, (string Owner, int Number, string Path)>(StringComparer.OrdinalIgnoreCase);
        var byOwner = new Dictionary<string, (LadderInstanceDataBlock Db, string Path)>(StringComparer.Ordinal);

        void Register(string owner, string path, LadderInstanceDataBlock? db)
        {
            // Programs authored before the editor exposed DB assignment remain
            // readable; the editor migrates those calls to a generated DB.
            if (db is null) return;
            if (db.Number is < 1 or > 65535)
                issues.Add(new("VC016", path + ".instanceDataBlock.number", "Instance DB number must be from 1 through 65535."));
            if (string.IsNullOrWhiteSpace(db.Name))
                issues.Add(new("VC016", path + ".instanceDataBlock.name", "Instance DB symbolic name is required."));

            if (byOwner.TryGetValue(owner, out var firstForOwner)
                && firstForOwner.Db != db)
                issues.Add(new("VC016", path + ".instanceDataBlock", $"Instruction instance '{owner}' is assigned to more than one DB; reset/load calls must reference its owning DB."));
            else byOwner.TryAdd(owner, (db, path));

            if (byNumber.TryGetValue(db.Number, out var firstNumber) && firstNumber.Owner != owner)
                issues.Add(new("VC016", path + ".instanceDataBlock.number", $"DB{db.Number} is already assigned to '{firstNumber.Owner}' at {firstNumber.Path}."));
            else byNumber.TryAdd(db.Number, (owner, db.Name, path));

            if (byName.TryGetValue(db.Name, out var firstName) && firstName.Owner != owner)
                issues.Add(new("VC016", path + ".instanceDataBlock.name", $"DB symbol '{db.Name}' is already assigned to '{firstName.Owner}' at {firstName.Path}."));
            else byName.TryAdd(db.Name, (owner, db.Number, path));
        }

        foreach (var block in program.Blocks ?? [])
        foreach (var network in block.Networks)
        {
            if (network.Timer is { } timer)
                Register("TIMER:" + timer.Variable, $"blocks[{block.Id}].networks[{network.Id}].timer", timer.InstanceDataBlock);
            if (network.TimerReset is { } timerReset)
                Register("TIMER:" + timerReset.Variable, $"blocks[{block.Id}].networks[{network.Id}].timerReset", timerReset.InstanceDataBlock);
            if (network.Counter is { } counter)
                Register("COUNTER:" + counter.Variable, $"blocks[{block.Id}].networks[{network.Id}].counter", counter.InstanceDataBlock);
            if (network.CounterReset is { } counterReset)
                Register("COUNTER:" + counterReset.Variable, $"blocks[{block.Id}].networks[{network.Id}].counterReset", counterReset.InstanceDataBlock);
            if (network.CounterLoad is { } counterLoad)
                Register("COUNTER:" + counterLoad.Variable, $"blocks[{block.Id}].networks[{network.Id}].counterLoad", counterLoad.InstanceDataBlock);
        }
        foreach (var instance in program.Instances ?? [])
            Register("FB_INSTANCE:" + instance.Id, $"instances[{instance.Id}]", instance.InstanceDataBlock);
    }

    private static void ValidateCallGraph(
        IReadOnlyList<LadderBlock> blocks,
        ICollection<LadderValidationIssue> issues)
    {
        var edges = blocks.ToDictionary(
            block => block.Id,
            block => block.Networks.Where(network => network.Call is not null)
                .Select(network => network.Call!.TargetBlock).ToArray(),
            StringComparer.Ordinal);
        // Imported projects encode CALL depth through a flat array of IDs.
        // JSON nesting limits cannot protect a recursive graph walk. Keep
        // traversal frames on the heap, preserving gray/black cycle detection.
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);
        var typedDepths = new Dictionary<string, int>(StringComparer.Ordinal);
        var blockModes = blocks.ToDictionary(b => b.Id, b => b.SemanticMode, StringComparer.Ordinal);
        foreach (var block in blocks)
        {
            if (visited.Contains(block.Id)) continue;
            var stack = new Stack<(string Id, int NextTarget)>();
            visiting.Add(block.Id);
            stack.Push((block.Id, 0));
            while (stack.Count > 0)
            {
                var frame = stack.Pop();
                var targets = edges[frame.Id];
                if (frame.NextTarget == targets.Length)
                {
                    depths[frame.Id] = 1 + targets.Where(depths.ContainsKey).Select(t => depths[t]).DefaultIfEmpty(0).Max();
                    typedDepths[frame.Id] = blockModes[frame.Id] == LadderSemanticMode.Parameterized
                        ? depths[frame.Id]
                        : targets.Where(t => typedDepths.GetValueOrDefault(t) > 0).Select(t => typedDepths[t] + 1).DefaultIfEmpty(0).Max();
                    if (typedDepths[frame.Id] > 32)
                        issues.Add(new("VC_CALL_DEPTH", "$.blocks", $"Call depth exceeds the maximum of 32 at '{frame.Id}'."));
                    visiting.Remove(frame.Id);
                    visited.Add(frame.Id);
                    continue;
                }
                stack.Push((frame.Id, frame.NextTarget + 1));
                var target = targets[frame.NextTarget];
                if (!edges.ContainsKey(target)) continue; // Already diagnosed by CALL validation.
                if (visiting.Contains(target))
                {
                    issues.Add(new("VC011", "$.blocks", $"Recursive block call cycle includes '{block.Name}'."));
                    return;
                }
                if (visited.Contains(target)) continue;
                visiting.Add(target);
                stack.Push((target, 0));
            }
        }
    }

    private static void ValidateTextOperation(LadderNumericOperation operation, string path,
        IReadOnlyDictionary<string, PlcVariable> variables, ICollection<LadderValidationIssue> issues)
    {
        if (!variables.TryGetValue(operation.Destination, out var destination) || !PlcTextValues.IsText(destination.Type))
        {
            issues.Add(new("VC002", path, "Text operation requires a declared STRING/ENUM destination."));
            return;
        }
        if (destination.Role == PlcVariableRole.Input) issues.Add(new("VC005", path, "Text destination must be writable."));
        if (operation.Kind == LadderNumericOperationKind.Move)
        {
            if (!PlcTextValues.OperandCompatible(operation.SourceA, destination, variables))
                issues.Add(new("VC002", path, "MOV requires a compatible text tag or valid quoted member/literal."));
            return;
        }
        if (destination.Type != PlcVariableType.String) issues.Add(new("VC002", path, "FORMAT_TEXT destination must be STRING."));
        ValidateNumericOperand(operation.SourceA, path + ".sourceA", variables, issues);
        var template = new PlcVariable("template", PlcVariableType.String, PlcVariableRole.Memory, "");
        if (!PlcTextValues.OperandCompatible(operation.SourceB, template, variables))
            issues.Add(new("VC002", path, "FORMAT_TEXT template must be STRING or a quoted literal."));
        if (PlcTextValues.TryLiteral(operation.SourceB, out var literal) && !PlcTextValues.TryFormat(0, literal, out _))
            issues.Add(new("VC009", path, "FORMAT_TEXT needs exactly one {0:F0} through {0:F6} placeholder."));
    }

    private static void ValidateTextComparison(LadderNode node, string path,
        IReadOnlyDictionary<string, PlcVariable> variables, ICollection<LadderValidationIssue> issues)
    {
        if (node.CompareOperator == LadderCompareOperator.ContainsCode && PlcTextValues.TryLiteral(node.RightOperand, out var code)
            && (code.Length == 0 || code.Any(character => !char.IsLetterOrDigit(character))))
            issues.Add(new("VC009", path, "CODE MATCH selector must be a nonempty alphanumeric code."));
        PlcVariable? domain = variables.TryGetValue(node.Variable, out var left) && PlcTextValues.IsText(left.Type) ? left
            : variables.TryGetValue(node.RightOperand, out var right) && PlcTextValues.IsText(right.Type) ? right
            : new PlcVariable("literal", PlcVariableType.String, PlcVariableRole.Memory, "");
        if (!PlcTextValues.OperandCompatible(node.Variable, domain, variables)
            || !PlcTextValues.OperandCompatible(node.RightOperand, domain, variables)
            || node.CompareOperator is not (LadderCompareOperator.Equal or LadderCompareOperator.NotEqual or LadderCompareOperator.ContainsCode)
            || node.CompareOperator == LadderCompareOperator.ContainsCode && domain.Type != PlcVariableType.String)
            issues.Add(new("VC002", path, "Text comparison requires matching domains; ENUM supports EQ/NE, STRING also supports CODE MATCH."));
    }

    private static void ValidateNode(
        LadderNode node,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ISet<string> ids,
        ICollection<LadderValidationIssue> issues, int schemaVersion = 1, int depth = 1,
        ISet<string>? edgeMemoryOperands = null)
    {
        if (depth > 32)
        {
            issues.Add(new("VC003", path, "Logic nesting exceeds the supported depth of 32."));
            return;
        }
        AddId(node.Id, $"{path}.id", ids, issues);
        if (node.OpenEnd)
            issues.Add(new("VC003", $"{path}.openEnd",
                "Parallel branch is open and must be closed before the program can be loaded."));
        if (node.Kind == LadderNodeKind.Contact)
        {
            if (!Enum.IsDefined(node.EdgeMode))
                issues.Add(new("VC100", $"{path}.edge", $"Unknown edge mode '{node.EdgeMode}'."));
            if (node.EdgeMode != LadderEdgeMode.None && node.NormallyClosed)
                issues.Add(new("VC100", $"{path}.contact", "Edge contacts cannot also be normally closed."));
            if (node.EdgeMode == LadderEdgeMode.None && node.EdgeMemoryOperand is not null)
                issues.Add(new("VC100", $"{path}.edgeMemory", "Only P/N edge contacts may use an edge-memory operand."));
            if (node.EdgeMemoryOperand is { } edgeMemory)
            {
                if (string.IsNullOrWhiteSpace(edgeMemory))
                    issues.Add(new("VC001", $"{path}.edgeMemory", "Edge-memory operand cannot be empty."));
                else if (edgeMemoryOperands is not null && !edgeMemoryOperands.Add(edgeMemory))
                    issues.Add(new("VC004", $"{path}.edgeMemory", $"P/N edge-memory operand '{edgeMemory}' must be unique."));
                if (variables.TryGetValue(edgeMemory, out var memoryTag))
                {
                    if (memoryTag.Type != PlcVariableType.Bool)
                        issues.Add(new("VC002", $"{path}.edgeMemory", $"Edge-memory operand '{memoryTag.Name}' must be BOOL."));
                    if (memoryTag.Role != PlcVariableRole.Memory)
                        issues.Add(new("VC005", $"{path}.edgeMemory", $"Edge-memory operand '{memoryTag.Name}' must use writable Memory storage."));
                }
                else if (!string.IsNullOrWhiteSpace(edgeMemory))
                    issues.Add(new("VC001", $"{path}.edgeMemory", $"Unknown edge-memory operand '{edgeMemory}'."));
            }
            if (node.Variable.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                || node.Variable.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                // BOOL user constants are lowered to immutable literals before
                // compile; the runtime already resolves these literal contacts.
            }
            else if (variables.TryGetValue(node.Variable, out var variable))
            {
                if (variable.Type != PlcVariableType.Bool)
                    issues.Add(new("VC002", $"{path}.variable", $"Contact variable '{variable.Name}' must be BOOL."));
            }
            else if (!TryResolveTimerMember(node.Variable, variables) && !TryResolveCounterMember(node.Variable, variables))
                issues.Add(new("VC001", $"{path}.variable", $"Unknown contact variable '{node.Variable}'."));
            return;
        }
        if (node.Kind == LadderNodeKind.Compare)
        {
            if (node.CompareOperator == LadderCompareOperator.ContainsCode
                || PlcTextValues.IsOperand(node.Variable, variables) || PlcTextValues.IsOperand(node.RightOperand, variables))
                ValidateTextComparison(node, path, variables, issues);
            else
            {
                ValidateNumericOperand(node.Variable, $"{path}.variable", variables, issues);
                ValidateNumericOperand(node.RightOperand, $"{path}.rightOperand", variables, issues);
                ValidateComparisonValueType(node, path, variables, issues);
            }
            return;
        }

        var children = node.Children ?? [];
        if (schemaVersion == 1 && children.Count == 0)
            issues.Add(new("VC003", $"{path}.children", $"{node.Kind} requires at least one child."));
        if (schemaVersion == 1 && node.Kind == LadderNodeKind.Parallel && children.Count < 2)
            issues.Add(new("VC003", $"{path}.children", "Parallel logic requires at least two branches."));
        if (node.Kind == LadderNodeKind.Parallel)
        {
            static bool ContainsInstruction(LadderNode expression) => expression.Kind is LadderNodeKind.Contact or LadderNodeKind.Compare
                || (expression.Children ?? []).Any(ContainsInstruction);
            for (var index = 0; index < children.Count; index++)
                if (!ContainsInstruction(children[index]))
                    issues.Add(new("VC003", $"{path}.children[{index}]",
                        "Parallel branch is open and must contain at least one LAD element before the program can be loaded."));
        }
        for (var index = 0; index < children.Count; index++)
            ValidateNode(children[index], $"{path}.children[{index}]", variables, ids, issues, schemaVersion, depth + 1, edgeMemoryOperands);
    }

    private static bool TryResolveTimerMember(string name, IReadOnlyDictionary<string, PlcVariable> variables)
    {
        var dot = name.LastIndexOf('.');
        if (dot <= 0) return false;
        var instance = name[..dot];
        var member = name[(dot + 1)..];
        return member is "DN" or "Q" or "TT"
            && variables.TryGetValue(instance, out var variable)
            && variable.Type == PlcVariableType.Timer;
    }

    private static bool TryResolveCounterMember(string name, IReadOnlyDictionary<string, PlcVariable> variables)
    {
        var dot = name.LastIndexOf('.');
        if (dot <= 0) return false;
        var instance = name[..dot];
        var member = name[(dot + 1)..];
        return member is "DN" or "Q"
            && variables.TryGetValue(instance, out var variable)
            && variable.Type == PlcVariableType.Counter;
    }

    private static void ValidateNumericOperand(
        string operand,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        if (double.TryParse(operand, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _)) return;
        if (variables.TryGetValue(operand, out var variable))
        {
            if (variable.Type is not (PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real))
                issues.Add(new("VC002", path, $"Numeric operand '{operand}' must be INT, DINT, or REAL."));
            return;
        }
        var dot = operand.LastIndexOf('.');
        if (dot > 0 && variables.TryGetValue(operand[..dot], out var instance))
        {
            var member = operand[(dot + 1)..];
            if (instance.Type == PlcVariableType.Counter && member is "ACC" or "CV" or "PRE" or "PV") return;
            if (instance.Type == PlcVariableType.Timer && member is "ET" or "PT") return;
        }
        issues.Add(new("VC001", path, $"Unknown numeric operand '{operand}'."));
    }

    private static void ValidateArithmeticDataType(
        LadderNumericOperation operation,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        if (operation.Kind == LadderNumericOperationKind.Exponentiate)
        {
            ValidateExponentiateDataType(operation, path, variables, issues);
            return;
        }
        if (operation.Kind == LadderNumericOperationKind.Truncate)
        {
            ValidateTruncateDataType(operation, path, variables, issues);
            return;
        }
        if (operation.DataType == LadderNumericDataType.Unspecified
            && operation.Kind is not (LadderNumericOperationKind.Absolute or LadderNumericOperationKind.Negate)
            && !LadderNumericOperationRules.IsFloatingUnaryMath(operation.Kind)) return;
        if (!variables.TryGetValue(operation.Destination, out var destination)) return;

        var selected = operation.DataType is LadderNumericDataType.Auto or LadderNumericDataType.Unspecified
            ? LadderNumericDataTypePresentation.FromVariableType(destination.Type)
            : operation.DataType;
        if (!LadderNumericOperationRules.SupportsTiaDataType(operation.Kind, selected))
        {
            issues.Add(new("VC002", $"{path}.numericOperation.dataType",
                $"{operation.Kind.ToString().ToUpperInvariant()} resolved to unsupported simulator data type '{selected}'."));
            return;
        }
        var expectedType = selected switch
        {
            LadderNumericDataType.Int => PlcVariableType.Int,
            LadderNumericDataType.DInt => PlcVariableType.DInt,
            LadderNumericDataType.Real => PlcVariableType.Real,
            _ => (PlcVariableType?)null,
        };
        if (expectedType is null) return;

        if (destination.Type != expectedType.Value)
            issues.Add(new("VC002", $"{path}.numericOperation.destination",
                $"Selected {selected} arithmetic requires a matching {selected} destination."));

        var typedOperands = LadderNumericOperationRules.RequiresSourceB(operation.Kind)
            ? new[] { (operation.SourceA, "sourceA"), (operation.SourceB, "sourceB") }
            : new[] { (operation.SourceA, "sourceA") };
        foreach (var (operand, suffix) in typedOperands)
        {
            if (variables.TryGetValue(operand, out var source))
            {
                if (source.Type != expectedType.Value)
                    issues.Add(new("VC002", $"{path}.numericOperation.{suffix}",
                        $"Selected {selected} arithmetic requires a matching {selected} tag; '{operand}' is {source.Type}."));
                continue;
            }

            if (selected == LadderNumericDataType.Real
                || !double.TryParse(operand, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var literal)) continue;

            var minimum = selected == LadderNumericDataType.Int ? short.MinValue : int.MinValue;
            var maximum = selected == LadderNumericDataType.Int ? short.MaxValue : int.MaxValue;
            if (literal != Math.Truncate(literal) || literal < minimum || literal > maximum)
                issues.Add(new("VC002", $"{path}.numericOperation.{suffix}",
                    $"Literal '{operand}' is not representable as {selected} ({minimum}..{maximum})."));
        }
    }

    private static void ValidateExponentiateDataType(
        LadderNumericOperation operation,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        if (variables.TryGetValue(operation.Destination, out var destination)
            && destination.Type != PlcVariableType.Real)
            issues.Add(new("VC002", $"{path}.numericOperation.destination",
                "EXPT requires a REAL destination in the supported Siemens subset."));

        if (TryGetMoveNumericSourceType(operation.SourceA, variables) is { } baseType
            && baseType != PlcVariableType.Real)
            issues.Add(new("VC002", $"{path}.numericOperation.sourceA",
                $"EXPT base input IN1 must be REAL; '{operation.SourceA}' is {baseType}."));

        var exponentType = operation.DataType;
        var declaredExponentType = TryGetMoveNumericSourceType(operation.SourceB, variables);
        if (exponentType == LadderNumericDataType.Unspecified)
            exponentType = LadderNumericDataTypePresentation.FromVariableType(declaredExponentType);
        if (exponentType == LadderNumericDataType.Unspecified) return;

        var expectedExponentType = PlcTypeForNumericDataType(exponentType);
        if (declaredExponentType is { } declared && expectedExponentType is { } expected
            && declared != expected)
            issues.Add(new("VC002", $"{path}.numericOperation.sourceB",
                $"Selected EXPT exponent type {exponentType} requires a matching {expected} tag; '{operation.SourceB}' is {declared}."));

        if (expectedExponentType is PlcVariableType.Int or PlcVariableType.DInt
            && double.TryParse(operation.SourceB, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var literal))
        {
            var minimum = expectedExponentType == PlcVariableType.Int ? short.MinValue : int.MinValue;
            var maximum = expectedExponentType == PlcVariableType.Int ? short.MaxValue : int.MaxValue;
            if (literal != Math.Truncate(literal) || literal < minimum || literal > maximum)
                issues.Add(new("VC002", $"{path}.numericOperation.sourceB",
                    $"EXPT exponent literal '{operation.SourceB}' is not representable as {exponentType} ({minimum}..{maximum})."));
        }
    }

    private static void ValidateTruncateDataType(
        LadderNumericOperation operation,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        var inputType = operation.InputDataType == LadderNumericDataType.Unspecified
            ? LadderNumericDataType.Real
            : operation.InputDataType;
        if (!LadderNumericDataTypePresentation.TiaInputTypesFor(operation.Kind).Contains(inputType)
            || inputType == LadderNumericDataType.Unspecified)
            issues.Add(new("VC007", $"{path}.numericOperation.inputDataType",
                "TRUNC input type must be REAL in the supported Siemens subset."));

        var sourceType = TryGetMoveNumericSourceType(operation.SourceA, variables);
        if (sourceType is not null && sourceType != PlcVariableType.Real)
            issues.Add(new("VC002", $"{path}.numericOperation.sourceA",
                $"TRUNC input IN must be REAL in the supported Siemens subset; '{operation.SourceA}' is {sourceType}."));

        if (!variables.TryGetValue(operation.Destination, out var destination)) return;
        var selected = operation.OutputDataType != LadderNumericDataType.Unspecified
            ? operation.OutputDataType
            : operation.DataType != LadderNumericDataType.Unspecified
                ? operation.DataType
                : LadderNumericDataTypePresentation.FromVariableType(destination.Type);
        if (!LadderNumericDataTypePresentation.TiaOutputTypesFor(operation.Kind).Contains(selected)
            || selected == LadderNumericDataType.Unspecified)
        {
            issues.Add(new("VC002", $"{path}.numericOperation.dataType",
                $"TRUNC resolved to unsupported simulator output data type '{selected}'."));
            return;
        }

        if (PlcTypeForNumericDataType(selected) is { } expectedOutput && destination.Type != expectedOutput)
            issues.Add(new("VC002", $"{path}.numericOperation.destination",
                $"Selected TRUNC output type {selected} requires a matching {expectedOutput} destination."));
    }

    private static void ValidateConvertTypes(
        LadderNumericOperation operation,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        var inputType = operation.InputDataType;
        var sourceDeclaredType = TryGetMoveNumericSourceType(operation.SourceA, variables);
        if (inputType == LadderNumericDataType.Unspecified)
            inputType = LadderNumericDataTypePresentation.FromVariableType(sourceDeclaredType);
        if (!LadderNumericOperationRules.SupportsTiaDataType(LadderNumericOperationKind.Convert, inputType))
            issues.Add(new("VC007", $"{path}.numericOperation.inputDataType",
                "CONVERT input type must be assigned or inferable from a typed source tag; supported offline types are INT, DINT, and REAL."));
        else
        {
            var expectedInput = PlcTypeForNumericDataType(inputType);
            if (sourceDeclaredType is { } declaredInput && expectedInput is { } selectedInput
                && declaredInput != selectedInput)
                issues.Add(new("VC002", $"{path}.numericOperation.sourceA",
                    $"CONVERT input type {selectedInput} does not match source tag type {declaredInput}."));
            if (sourceDeclaredType is null
                && double.TryParse(operation.SourceA, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var literal)
                && inputType is LadderNumericDataType.Int or LadderNumericDataType.DInt
                && (literal != Math.Truncate(literal)
                    || literal < (inputType == LadderNumericDataType.Int ? short.MinValue : int.MinValue)
                    || literal > (inputType == LadderNumericDataType.Int ? short.MaxValue : int.MaxValue)))
                issues.Add(new("VC002", $"{path}.numericOperation.sourceA",
                    $"Literal '{operation.SourceA}' is not representable as CONVERT input type {inputType}."));
        }

        var outputType = operation.OutputDataType;
        if (outputType == LadderNumericDataType.Unspecified
            && variables.TryGetValue(operation.Destination, out var destination))
            outputType = LadderNumericDataTypePresentation.FromVariableType(destination.Type);
        if (!LadderNumericOperationRules.SupportsTiaDataType(LadderNumericOperationKind.Convert, outputType))
            issues.Add(new("VC007", $"{path}.numericOperation.outputDataType",
                "CONVERT output type must be assigned or inferable from a typed destination tag; supported offline types are INT, DINT, and REAL."));
        else if (variables.TryGetValue(operation.Destination, out var typedDestination)
            && PlcTypeForNumericDataType(outputType) is { } expectedOutput
            && typedDestination.Type != expectedOutput)
            issues.Add(new("VC002", $"{path}.numericOperation.destination",
                $"CONVERT output type {expectedOutput} does not match destination tag type {typedDestination.Type}."));
    }

    private static void ValidateRoundingConversionTypes(
        LadderNumericOperation operation,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        var mnemonic = LadderNumericOperationRules.RoundingMnemonic(operation.Kind);
        var inputType = operation.InputDataType == LadderNumericDataType.Unspecified
            ? LadderNumericDataType.Real
            : operation.InputDataType;
        if (!LadderNumericDataTypePresentation.TiaInputTypesFor(operation.Kind).Contains(inputType)
            || inputType == LadderNumericDataType.Unspecified)
            issues.Add(new("VC007", $"{path}.numericOperation.inputDataType",
                $"{mnemonic} input type must be REAL in the supported Siemens subset."));

        var sourceType = TryGetMoveNumericSourceType(operation.SourceA, variables);
        if (sourceType is not null && sourceType != PlcVariableType.Real)
            issues.Add(new("VC002", $"{path}.numericOperation.sourceA",
                $"{mnemonic} input IN must be REAL; '{operation.SourceA}' is {sourceType}."));

        var outputType = operation.OutputDataType;
        if (outputType == LadderNumericDataType.Unspecified
            && variables.TryGetValue(operation.Destination, out var destination))
            outputType = LadderNumericDataTypePresentation.FromVariableType(destination.Type);
        if (!LadderNumericDataTypePresentation.TiaOutputTypesFor(operation.Kind).Contains(outputType)
            || outputType == LadderNumericDataType.Unspecified)
        {
            issues.Add(new("VC007", $"{path}.numericOperation.outputDataType",
                $"{mnemonic} output type must be assigned or inferable as INT, DINT, or REAL."));
            return;
        }

        if (variables.TryGetValue(operation.Destination, out var typedDestination)
            && PlcTypeForNumericDataType(outputType) is { } expectedOutput
            && typedDestination.Type != expectedOutput)
            issues.Add(new("VC002", $"{path}.numericOperation.destination",
                $"{mnemonic} output type {expectedOutput} does not match destination tag type {typedDestination.Type}."));
    }

    private static void ValidateScalingTypes(
        LadderNumericOperation operation,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        var inputOperand = operation.Kind == LadderNumericOperationKind.Normalize
            ? operation.SourceA : operation.SourceB;
        var inputType = operation.InputDataType;
        if (inputType == LadderNumericDataType.Unspecified)
            inputType = operation.Kind == LadderNumericOperationKind.Scale
                ? LadderNumericDataType.Real
                : LadderNumericDataTypePresentation.FromVariableType(TryGetMoveNumericSourceType(inputOperand, variables));
        if (inputType == LadderNumericDataType.Unspecified
            && variables.TryGetValue(operation.Destination, out var inferredDestination))
            inputType = LadderNumericDataTypePresentation.FromVariableType(inferredDestination.Type);

        var outputType = operation.OutputDataType;
        if (outputType == LadderNumericDataType.Unspecified
            && variables.TryGetValue(operation.Destination, out var destination))
            outputType = LadderNumericDataTypePresentation.FromVariableType(destination.Type);

        ValidateSide(inputType, true);
        ValidateSide(outputType, false);

        var rangeType = operation.Kind == LadderNumericOperationKind.Normalize ? inputType : outputType;
        var rangeOperands = new[] { (operation.SourceA, "sourceA"), (operation.SourceC, "sourceC") };
        foreach (var (operand, suffix) in rangeOperands) ValidateOperand(operand, suffix, rangeType);
        if (operation.Kind == LadderNumericOperationKind.Normalize)
            ValidateOperand(operation.SourceB, "sourceB", inputType);
        else
            ValidateOperand(operation.SourceB, "sourceB", LadderNumericDataType.Real);

        if (variables.TryGetValue(operation.Destination, out var typedDestination)
            && PlcTypeForNumericDataType(outputType) is { } expectedOutput
            && typedDestination.Type != expectedOutput)
            issues.Add(new("VC002", $"{path}.numericOperation.destination",
                $"{operation.Kind} output type {expectedOutput} does not match destination tag type {typedDestination.Type}."));

        void ValidateSide(LadderNumericDataType selected, bool input)
        {
            var choices = input
                ? LadderNumericDataTypePresentation.TiaInputTypesFor(operation.Kind)
                : LadderNumericDataTypePresentation.TiaOutputTypesFor(operation.Kind);
            if (!choices.Contains(selected) || selected == LadderNumericDataType.Unspecified)
                issues.Add(new("VC007", $"{path}.numericOperation.{(input ? "inputDataType" : "outputDataType")}",
                    $"{operation.Kind} {(input ? "input" : "output")} type must resolve to a supported offline type."));
        }

        void ValidateOperand(string operand, string suffix, LadderNumericDataType expectedType)
        {
            if (TryGetMoveNumericSourceType(operand, variables) is { } declared
                && PlcTypeForNumericDataType(expectedType) is { } expected && declared != expected)
            {
                issues.Add(new("VC002", $"{path}.numericOperation.{suffix}",
                    $"{operation.Kind} {suffix} requires {expected}; '{operand}' is {declared}."));
                return;
            }
            if (expectedType is not (LadderNumericDataType.Int or LadderNumericDataType.DInt)
                || !double.TryParse(operand, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var literal)) return;
            var minimum = expectedType == LadderNumericDataType.Int ? short.MinValue : int.MinValue;
            var maximum = expectedType == LadderNumericDataType.Int ? short.MaxValue : int.MaxValue;
            if (literal != Math.Truncate(literal) || literal < minimum || literal > maximum)
                issues.Add(new("VC002", $"{path}.numericOperation.{suffix}",
                    $"Literal '{operand}' is not representable as {expectedType} ({minimum}..{maximum})."));
        }
    }

    private static PlcVariableType? PlcTypeForNumericDataType(LadderNumericDataType dataType) => dataType switch
    {
        LadderNumericDataType.Int => PlcVariableType.Int,
        LadderNumericDataType.DInt => PlcVariableType.DInt,
        LadderNumericDataType.Real => PlcVariableType.Real,
        _ => null,
    };

    private static PlcVariableType? TryGetMoveNumericSourceType(
        string operand, IReadOnlyDictionary<string, PlcVariable> variables)
    {
        // Numeric literals are treated as constants of the destination type.
        // Variable operands carry a declared type and must match exactly for
        // the simulator's IEC-checked MOVE subset. Timer values are TIME and
        // therefore cannot silently flow into an INT/DINT/REAL destination.
        if (double.TryParse(operand, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _)) return null;
        if (variables.TryGetValue(operand, out var variable)) return variable.Type;
        var dot = operand.LastIndexOf('.');
        if (dot <= 0 || !variables.TryGetValue(operand[..dot], out var instance)) return null;
        var member = operand[(dot + 1)..];
        if (instance.Type == PlcVariableType.Counter && member is "ACC" or "CV" or "PRE" or "PV")
            return PlcVariableType.DInt;
        if (instance.Type == PlcVariableType.Timer && member is "ET" or "PT")
            return PlcVariableType.Time;
        return null;
    }

    private static void ValidateComparisonValueType(
        LadderNode node,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        var selected = node.ComparisonValueType;
        if (selected == LadderComparisonValueType.Unspecified) return;
        if (selected is not (LadderComparisonValueType.Int or LadderComparisonValueType.DInt or LadderComparisonValueType.Real))
        {
            issues.Add(new("VC002", $"{path}.dataType",
                $"The offline comparator currently validates INT, DINT, and REAL selections only; '{selected}' remains unsupported by the typed tag model."));
            return;
        }

        var expected = selected switch
        {
            LadderComparisonValueType.Int => PlcVariableType.Int,
            LadderComparisonValueType.DInt => PlcVariableType.DInt,
            _ => PlcVariableType.Real,
        };
        foreach (var (operand, suffix) in new[] { (node.Variable, "variable"), (node.RightOperand, "rightOperand") })
        {
            if (variables.TryGetValue(operand, out var variable))
            {
                if (variable.Type != expected)
                    issues.Add(new("VC002", $"{path}.{suffix}",
                        $"Comparator data type {selected} requires a matching {selected} tag; '{operand}' is {variable.Type}."));
                continue;
            }
            if (selected == LadderComparisonValueType.Real) continue;
            if (!long.TryParse(operand, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var literal)) continue;
            var minimum = selected == LadderComparisonValueType.Int ? short.MinValue : int.MinValue;
            var maximum = selected == LadderComparisonValueType.Int ? short.MaxValue : int.MaxValue;
            if (literal < minimum || literal > maximum)
                issues.Add(new("VC002", $"{path}.{suffix}",
                    $"Literal '{operand}' is outside the {selected} comparison range ({minimum}..{maximum})."));
        }
    }

    private static void ValidateCounterInstance(
        string name,
        string path,
        IReadOnlyDictionary<string, PlcVariable> variables,
        ICollection<LadderValidationIssue> issues)
    {
        if (!variables.TryGetValue(name, out var variable))
            issues.Add(new("VC001", path, $"Unknown counter instance '{name}'."));
        else if (variable.Type != PlcVariableType.Counter)
            issues.Add(new("VC002", path, $"Counter instruction instance '{name}' must be COUNTER."));
    }

    private static void ValidateBooleanOperand(string operand, string path,
        IReadOnlyDictionary<string, PlcVariable> variables, ICollection<LadderValidationIssue> issues)
    {
        if (operand.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
            || operand.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return;
        if (variables.TryGetValue(operand, out var variable))
        {
            if (variable.Type != PlcVariableType.Bool)
                issues.Add(new("VC002", path, $"Boolean operand '{operand}' must be BOOL."));
            return;
        }
        if (TryResolveTimerMember(operand, variables) || TryResolveCounterMember(operand, variables)) return;
        issues.Add(new("VC001", path, $"Unknown Boolean operand '{operand}'."));
    }

    private static void AddId(
        string id,
        string path,
        ISet<string> ids,
        ICollection<LadderValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(id))
            issues.Add(new("VC004", path, "Element ID is required."));
        else if (!ids.Add(id))
            issues.Add(new("VC004", path, $"Duplicate element ID '{id}'."));
    }
}


