using System;
using System.Collections.Generic;
using System.Linq;

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

        var variables = new Dictionary<string, PlcVariable>(StringComparer.Ordinal);
        for (var index = 0; index < program.Variables.Count; index++)
        {
            var variable = program.Variables[index];
            var path = $"$.variables[{index}]";
            if (string.IsNullOrWhiteSpace(variable.Name))
                issues.Add(new("VC001", $"{path}.name", "Variable name is required."));
            else if (!variables.TryAdd(variable.Name, variable))
                issues.Add(new("VC004", $"{path}.name", $"Duplicate variable '{variable.Name}'."));
            if (variable.Type is not (PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.Timer or PlcVariableType.Counter or PlcVariableType.String or PlcVariableType.Enum))
                issues.Add(new("VC002", $"{path}.type", $"The current runtime supports BOOL, INT, DINT, REAL, TIMER, and COUNTER; '{variable.Name}' is {variable.Type}."));
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
        var blocks = program.Blocks is { Count: > 0 }
            ? program.Blocks
            : [new LadderBlock("main", program.Name, program.Networks)];
        var blockIds = new HashSet<string>(StringComparer.Ordinal);
        var blockNames = new HashSet<string>(StringComparer.Ordinal);
        var timerInstructionPaths = new Dictionary<string, string>(StringComparer.Ordinal);
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
            else if (!blockNames.Add(block.Name))
                issues.Add(new("VC004", $"{blockPath}.name", $"Duplicate block name '{block.Name}'."));
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
                ValidateNode(network.Logic, $"{path}.logic", variables, ids, issues, program.SchemaVersion);
                var outputCount = (network.Coil is null ? 0 : 1)
                    + (network.Timer is null ? 0 : 1)
                    + (network.Counter is null ? 0 : 1)
                    + (network.CounterReset is null ? 0 : 1)
                    + (network.NumericOperation is null ? 0 : 1)
                    + (network.Call is null ? 0 : 1)
                    + (network.Return is null ? 0 : 1)
                    + (network.CounterLoad is null ? 0 : 1)
                    + (network.TimerReset is null ? 0 : 1)
                    + (network.Jump is null ? 0 : 1)
                    + (network.LabelInstruction is null ? 0 : 1);
                if (outputCount != 1)
                    issues.Add(new("VC006", path, "A network requires exactly one output instruction: coil, timer/reset, counter reset/load, numeric operation, call, return, jump, or label."));
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
                    if (network.Timer.Preset <= TimeSpan.Zero)
                        issues.Add(new("VC007", $"{path}.timer.presetMs", $"{network.Timer.Kind} preset must be greater than zero."));
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
                    if (network.Counter.Preset <= 0)
                        issues.Add(new("VC008", $"{path}.counter.preset", $"{network.Counter.Kind} preset must be greater than zero."));
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
                }
                if (network.NumericOperation is not null)
                {
                    var operation = network.NumericOperation;
                    AddId(operation.Id, $"{path}.numericOperation.id", ids, issues);
                    if (!Enum.IsDefined(operation.Kind))
                        issues.Add(new("VC100", $"{path}.numericOperation.operation", $"Unknown numeric operation '{operation.Kind}'."));
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
                        if (operation.Kind is LadderNumericOperationKind.Divide or LadderNumericOperationKind.Modulo
                            && double.TryParse(operation.SourceB, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var divisor)
                            && divisor == 0)
                            issues.Add(new("VC009", $"{path}.numericOperation.sourceB", $"{operation.Kind.ToString().ToUpperInvariant()} literal divisor cannot be zero."));
                        if (operation.Kind == LadderNumericOperationKind.SquareRoot
                            && double.TryParse(operation.SourceA, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var radicand)
                            && radicand < 0)
                            issues.Add(new("VC009", $"{path}.numericOperation.sourceA", "SQRT literal source cannot be negative."));
                        if (operation.Kind is LadderNumericOperationKind.NaturalLog or LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine
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
                    AddId(network.Return.Id, $"{path}.return.id", ids, issues);
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
        var entryBlock = string.IsNullOrWhiteSpace(program.EntryBlock) ? blocks[0].Id : program.EntryBlock;
        if (!blockIds.Contains(entryBlock))
            issues.Add(new("VC010", "$.entryBlock", $"Unknown entry block '{entryBlock}'."));
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
        ValidateCallGraph(blocks, issues);
        return issues;
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
        ICollection<LadderValidationIssue> issues, int schemaVersion = 1, int depth = 1)
    {
        if (depth > 32)
        {
            issues.Add(new("VC003", path, "Logic nesting exceeds the supported depth of 32."));
            return;
        }
        AddId(node.Id, $"{path}.id", ids, issues);
        if (node.Kind == LadderNodeKind.Contact)
        {
            if (!Enum.IsDefined(node.EdgeMode))
                issues.Add(new("VC100", $"{path}.edge", $"Unknown edge mode '{node.EdgeMode}'."));
            if (node.EdgeMode != LadderEdgeMode.None && node.NormallyClosed)
                issues.Add(new("VC100", $"{path}.contact", "Edge contacts cannot also be normally closed."));
            if (variables.TryGetValue(node.Variable, out var variable))
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
            }
            return;
        }

        var children = node.Children ?? [];
        if (schemaVersion == 1 && children.Count == 0)
            issues.Add(new("VC003", $"{path}.children", $"{node.Kind} requires at least one child."));
        if (schemaVersion == 1 && node.Kind == LadderNodeKind.Parallel && children.Count < 2)
            issues.Add(new("VC003", $"{path}.children", "Parallel logic requires at least two branches."));
        for (var index = 0; index < children.Count; index++)
            ValidateNode(children[index], $"{path}.children[{index}]", variables, ids, issues, schemaVersion, depth + 1);
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


