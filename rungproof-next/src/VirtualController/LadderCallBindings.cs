using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Stable, explicitly qualified references. Display names are never used to guess a local binding.</summary>
public static class LadderCallBindings
{
    public static string GlobalId(PlcVariable variable) => string.IsNullOrWhiteSpace(variable.Id) ? variable.Name : variable.Id;
    private static IReadOnlyList<PlcVariable> GlobalDeclarations(LadderProgram program) => PlcAggregates.Expand(program.Variables).Select(leaf =>
    {
        var root = program.Variables.FirstOrDefault(v => PlcAggregates.IsAggregate(v.Type)
            && (leaf.Name.StartsWith(v.Name + ".", StringComparison.Ordinal) || leaf.Name.StartsWith(v.Name + "[", StringComparison.Ordinal)));
        return root is null ? leaf : leaf with { Id = GlobalId(root) + leaf.Name[root.Name.Length..] };
    }).ToArray();
    public static string LocalKey(string blockId, string formalId) => "@local/" + Uri.EscapeDataString(blockId) + "/" + Uri.EscapeDataString(formalId);

    internal static IReadOnlyList<LadderValidationIssue> Validate(LadderProgram program)
    {
        var issues = new List<LadderValidationIssue>();
        var blocks = program.Blocks ?? [];
        var globals = GlobalDeclarations(program);
        foreach (var duplicate in globals.GroupBy(GlobalId).Where(g => g.Count() > 1))
            issues.Add(new("VC_CALL_ID", "$.variables", $"Duplicate stable symbol identity '{duplicate.Key}'."));
        foreach (var duplicate in (program.Instances ?? []).GroupBy(i => i.Id).Where(g => g.Count() > 1))
            issues.Add(new("VC_CALL_INSTANCE", "$.instances", $"Duplicate instance identity '{duplicate.Key}'."));
        foreach (var instance in program.Instances ?? [])
        {
            var target = blocks.FirstOrDefault(b => b.Id == instance.BlockId);
            if (string.IsNullOrWhiteSpace(instance.Id) || string.IsNullOrWhiteSpace(instance.Name)
                || target is not { SemanticMode: LadderSemanticMode.Parameterized, BlockType: LadderBlockType.FunctionBlock })
                issues.Add(new("VC_CALL_INSTANCE", "$.instances", "Instance requires a stable identity, name and parameterized FB type."));
        }
        for (var bi = 0; bi < blocks.Count; bi++)
        {
            var block = blocks[bi]; var bp = $"$.blocks[{bi}]";
            if (!Enum.IsDefined(block.SemanticMode)) issues.Add(new("VC_CALL_MODE", bp, "Unknown block semantic mode."));
            if (block.BlockType == LadderBlockType.DataBlock)
            {
                if (block.Networks.Count != 0)
                    issues.Add(new("VC_DB_STRUCTURE", bp + ".networks", "A data block contains declarations and cannot contain LAD networks."));
                if ((block.Interface ?? []).Any(parameter => parameter.Section != LadderInterfaceSection.Static))
                    issues.Add(new("VC_DB_STRUCTURE", bp + ".interface", "Global DB declarations must use static storage, not code-block interface sections."));
            }
            if (block.SemanticMode == LadderSemanticMode.Parameterized)
            {
                if (program.SchemaVersion != 2) issues.Add(new("VC_CALL_VERSION", bp, "Parameterized blocks require schema 2."));
                if (block.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock))
                    issues.Add(new("VC_CALL_TARGET", bp + ".blockType", "Parameterized bodies must be FC or FB."));
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (var pi = 0; pi < (block.Interface?.Count ?? 0); pi++)
                {
                    var p = block.Interface![pi]; var pp = bp + $".interface[{pi}]";
                    if (string.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id)) issues.Add(new("VC_CALL_ID", pp, "Formal requires a unique stable identity."));
                    if (string.IsNullOrWhiteSpace(p.Name)) issues.Add(new("VC_CALL_ID", pp + ".name", "Formal name is required."));
                    if (!Enum.IsDefined(p.Section)) issues.Add(new("VC_CALL_SECTION", pp, "Unknown formal section."));
                    if (p.Section == LadderInterfaceSection.Static && block.BlockType != LadderBlockType.FunctionBlock)
                        issues.Add(new("VC_CALL_SECTION", pp, "Static storage is supported only in FB."));
                    var scalar = p.Type is PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.String or PlcVariableType.Enum;
                    if (!scalar && !(p.Section == LadderInterfaceSection.Static && block.BlockType == LadderBlockType.FunctionBlock
                        && p.Type is PlcVariableType.Timer or PlcVariableType.Counter))
                        issues.Add(new("VC_CALL_TYPE", pp, $"{p.Type} is unsupported in {p.Section}. Only scalar parameters and FB Static TIMER/COUNTER are supported."));
                }
            }
            for (var ni = 0; ni < block.Networks.Count; ni++)
            {
                if (block.SemanticMode == LadderSemanticMode.Parameterized)
                    ValidateScopedOperands(program, block, block.Networks[ni], bp + $".networks[{ni}]", issues);
                var call = block.Networks[ni].Call; if (call is null) continue;
                var cp = bp + $".networks[{ni}].call";
                var target = blocks.FirstOrDefault(b => b.Id == call.TargetBlock);
                if (!Enum.IsDefined(call.SemanticMode)) issues.Add(new("VC_CALL_MODE", cp, "Unknown CALL semantic mode."));
                if (target is null) continue; // Existing exact target diagnostic handles missing targets.
                if (target.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock))
                {
                    issues.Add(new("VC_CALL_TARGET", cp + ".targetBlock", "CALL target must be FC or FB; OB and DB are not callable."));
                    continue;
                }
                if (call.SemanticMode != target.SemanticMode) issues.Add(new("VC_CALL_MODE", cp + ".semanticMode", "CALL mode must match target block mode."));
                if (call.SemanticMode == LadderSemanticMode.Legacy)
                {
                    if ((call.Bindings?.Count ?? 0) != 0 || call.InstanceId.Length != 0)
                        issues.Add(new("VC_CALL_MODE", cp, "Legacy shared-global CALL cannot carry executable bindings or an instance."));
                    continue;
                }
                if (program.SchemaVersion != 2) issues.Add(new("VC_CALL_VERSION", cp, "Parameterized CALL requires schema 2."));
                if (target.BlockType == LadderBlockType.FunctionBlock)
                {
                    if (!(program.Instances ?? []).Any(i => i.Id == call.InstanceId && i.BlockId == target.Id))
                        issues.Add(new("VC_CALL_INSTANCE", cp + ".instanceId", "FB CALL requires a declared instance of this FB type."));
                }
                else if (call.InstanceId.Length != 0) issues.Add(new("VC_CALL_INSTANCE", cp + ".instanceId", "FC CALL does not accept an instance."));
                var required = (target.Interface ?? []).Where(p => p.Section is LadderInterfaceSection.Input or LadderInterfaceSection.Output or LadderInterfaceSection.InOut).ToArray();
                var writable = new HashSet<string>(StringComparer.Ordinal);
                foreach (var formal in required)
                    if ((call.Bindings ?? []).Count(b => b.ParameterId == formal.Id) != 1)
                        issues.Add(new("VC_CALL_BINDING", cp + ".bindings", $"Exactly one binding is required for '{formal.Name}'."));
                for (var i = 0; i < (call.Bindings?.Count ?? 0); i++)
                {
                    var binding = call.Bindings![i]; var path = cp + $".bindings[{i}]";
                    var formal = required.FirstOrDefault(p => p.Id == binding.ParameterId);
                    if (formal is null) { issues.Add(new("VC_CALL_BINDING", path, "Unknown or non-bindable formal identity.")); continue; }
                    if (!Enum.IsDefined(binding.Kind)) { issues.Add(new("VC_CALL_BINDING", path, "Unknown actual kind.")); continue; }
                    if (binding.Kind == LadderActualKind.Discard)
                    { if (formal.Section != LadderInterfaceSection.Output) issues.Add(new("VC_CALL_BINDING", path, "Only Output may be explicitly discarded.")); continue; }
                    if (binding.Kind == LadderActualKind.Literal)
                    {
                        if (formal.Section != LadderInterfaceSection.Input) issues.Add(new("VC_CALL_BINDING", path, "Output/InOut requires writable storage."));
                        if (binding.LiteralType != formal.Type || !DomainsEqual(formal.EnumMembers, binding.EnumMembers))
                            issues.Add(new("VC_CALL_TYPE", path, "Actual literal must have the exact formal type and enum domain."));
                        if (binding.Literal is null) issues.Add(new("VC_CALL_TYPE", path, "Input literal value is required."));
                        else ValidateInitial(new("literal", formal.Type, PlcVariableRole.Memory, binding.Literal, EnumMembers: binding.EnumMembers), path, issues);
                        continue;
                    }
                    var actual = ResolveDeclaration(program, block, binding.Symbol);
                    if (actual is null) { issues.Add(new("VC_CALL_SCOPE", path + ".symbol", "Actual requires an existing global:<id> or local:<id> reference.")); continue; }
                    if (actual.Type != formal.Type || !DomainsEqual(actual.EnumMembers, formal.EnumMembers))
                        issues.Add(new("VC_CALL_TYPE", path, "Actual and formal require exact types and enum domains; use an explicit conversion."));
                    if (formal.Section != LadderInterfaceSection.Input)
                    {
                        if (actual.Role == PlcVariableRole.Input) issues.Add(new("VC_CALL_WRITE", path, "Input-role storage is not writable."));
                        var identity = binding.Symbol.StartsWith("global:", StringComparison.Ordinal) ? "global:" + actual.Name : binding.Symbol;
                        if (!writable.Add(identity)) issues.Add(new("VC_CALL_ALIAS", path, "Writable actuals overlap in this CALL."));
                    }
                }
            }
        }
        foreach (var task in program.Tasks ?? [])
            if (blocks.Any(b => b.Id == task.EntryBlock && b.SemanticMode == LadderSemanticMode.Parameterized))
                issues.Add(new("VC_CALL_TARGET", "$.tasks", "A parameterized FC/FB requires CALL bindings and cannot be a task entry."));
        var effectiveEntry = string.IsNullOrWhiteSpace(program.EntryBlock) ? blocks.FirstOrDefault()?.Id : program.EntryBlock;
        if (blocks.Any(b => b.Id == effectiveEntry && b.SemanticMode == LadderSemanticMode.Parameterized))
            issues.Add(new("VC_CALL_TARGET", "$.entryBlock", "A parameterized FC/FB cannot be the program entry."));
        return issues;
    }

    private static bool DomainsEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b) => (a ?? []).SequenceEqual(b ?? [], StringComparer.Ordinal);

    private static void ValidateScopedOperands(LadderProgram program, LadderBlock block, LadderNetwork network,
        string path, ICollection<LadderValidationIssue> issues)
    {
        void Operand(string operand, string role, bool literalAllowed = false)
        {
            if (literalAllowed && (double.TryParse(operand, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                || PlcTextValues.TryLiteral(operand, out _))) return;
            if (operand.StartsWith("global:", StringComparison.Ordinal) || operand.StartsWith("local:", StringComparison.Ordinal))
            {
                if (ResolveDeclaration(program, block, operand) is not null) return;
                var dot = operand.LastIndexOf('.');
                if (dot > 0 && ResolveDeclaration(program, block, operand[..dot]) is { } declaration
                    && LadderStorageNamespace.Members(declaration.Type).Contains(operand[(dot + 1)..], StringComparer.Ordinal)) return;
            }
            issues.Add(new("VC_CALL_SCOPE", role, "Parameterized operand must use an existing explicit global:<id> or local:<id> reference, or an applicable literal."));
        }
        void Node(LadderNode node, string nodePath, int depth = 1)
        {
            if (depth > 32) return; // The ordinary tree validator diagnoses the depth limit.
            if (node.Kind == LadderNodeKind.Contact) Operand(node.Variable, nodePath + ".variable");
            else if (node.Kind == LadderNodeKind.Compare)
            { Operand(node.Variable, nodePath + ".left", true); Operand(node.RightOperand, nodePath + ".right", true); }
            for (var index = 0; index < (node.Children?.Count ?? 0); index++) Node(node.Children![index], nodePath + $".children[{index}]", depth + 1);
        }
        Node(network.Logic, path + ".logic");
        if (network.Coil is { } coil) Operand(coil.Variable, path + ".coil.variable");
        if (network.Timer is { } timer) Operand(timer.Variable, path + ".timer.variable");
        if (network.TimerReset is { } timerReset) Operand(timerReset.Variable, path + ".timerReset.variable");
        if (network.Counter is { } counter) Operand(counter.Variable, path + ".counter.variable");
        if (network.CounterReset is { } counterReset) Operand(counterReset.Variable, path + ".counterReset.variable");
        if (network.CounterLoad is { } counterLoad) Operand(counterLoad.Variable, path + ".counterLoad.variable");
        if (network.NumericOperation is { } operation)
        {
            Operand(operation.SourceA, path + ".numeric.sourceA", true);
            if (LadderNumericOperationRules.RequiresSourceB(operation.Kind)) Operand(operation.SourceB, path + ".numeric.sourceB", true);
            if (LadderNumericOperationRules.RequiresSourceC(operation.Kind)) Operand(operation.SourceC, path + ".numeric.sourceC", true);
            Operand(operation.Destination, path + ".numeric.destination");
        }
    }

    private static void ValidateInitial(PlcVariable variable, string path, ICollection<LadderValidationIssue> issues)
    {
        // Reuse the ordinary declared-value rules without inventing an implicit conversion.
        var probe = new LadderProgram(2, "initial", "Initial", "LD", TimeSpan.FromMilliseconds(1), [variable],
            [new("n", "n", new("wire", LadderNodeKind.Series, Children: []), Return: new("ret"))]);
        foreach (var issue in LadderCompiler.Validate(probe).Where(i => i.Path.StartsWith("$.variables", StringComparison.Ordinal)))
            issues.Add(issue with { Path = path + ".initial" });
    }

    public static PlcVariable? ResolveDeclaration(LadderProgram program, LadderBlock owner, string reference)
    {
        if (reference.StartsWith("global:", StringComparison.Ordinal))
            return GlobalDeclarations(program).FirstOrDefault(v => GlobalId(v) == reference[7..]);
        if (reference.StartsWith("local:", StringComparison.Ordinal) && owner.SemanticMode == LadderSemanticMode.Parameterized)
        {
            var p = (owner.Interface ?? []).FirstOrDefault(p => p.Id == reference[6..]);
            if (p is not null) return Declaration(owner.Id, p);
        }
        return null;
    }

    internal static PlcVariable Declaration(string blockId, LadderInterfaceParameter parameter) => new(LocalKey(blockId, parameter.Id),
        parameter.Type, parameter.Section == LadderInterfaceSection.Input ? PlcVariableRole.Input : PlcVariableRole.Memory,
        parameter.InitialValue, EnumMembers: parameter.EnumMembers);

    internal static LadderProgram LowerForValidation(LadderProgram program)
    {
        if (!(program.Blocks ?? []).Any(b => b.SemanticMode == LadderSemanticMode.Parameterized)) return program;
        var variables = program.Variables.ToList();
        var occupied = GlobalDeclarations(program).SelectMany(v => LadderStorageNamespace.Names(v.Name, v.Type)).ToHashSet(StringComparer.Ordinal);
        var blocks = (program.Blocks ?? []).Select(block =>
        {
            if (block.SemanticMode != LadderSemanticMode.Parameterized) return block;
            var locals = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var formal in block.Interface ?? [])
            {
                var candidate = LadderStorageNamespace.Reserve(occupied, LocalKey(block.Id, formal.Id), formal.Type);
                locals.Add(formal.Id, candidate); variables.Add(Declaration(block.Id, formal) with { Name = candidate });
            }
            string Map(string operand)
            {
                var exact = ResolveDeclaration(program, block, operand);
                if (exact is not null) return operand.StartsWith("local:", StringComparison.Ordinal) ? locals[operand[6..]] : exact.Name;
                var dot = operand.LastIndexOf('.');
                if (dot > 0 && ResolveDeclaration(program, block, operand[..dot]) is { } member)
                    return (operand.StartsWith("local:", StringComparison.Ordinal) ? locals[operand[6..dot]] : member.Name) + operand[dot..];
                // Scope validity is diagnosed on the original operand before lowering.
                return operand;
            }
            return block with { Networks = block.Networks.Select(n => MapNetwork(n, Map)).ToArray() };
        }).ToArray();
        return program with { Variables = variables, Blocks = blocks };
    }

    internal static LadderNetwork MapNetwork(LadderNetwork n, Func<string, string> map)
    {
        LadderNode Node(LadderNode node) => node with { Variable = map(node.Variable), RightOperand = map(node.RightOperand),
            EdgeMemoryOperand = node.EdgeMemoryOperand is null ? null : map(node.EdgeMemoryOperand), Children = node.Children?.Select(Node).ToArray() };
        return n with {
            Logic = Node(n.Logic), Coil = n.Coil is { } c ? c with { Variable = map(c.Variable) } : null,
            Timer = n.Timer is { } t ? t with { Variable = map(t.Variable) } : null,
            TimerReset = n.TimerReset is { } tr ? tr with { Variable = map(tr.Variable) } : null,
            Counter = n.Counter is { } ct ? ct with { Variable = map(ct.Variable) } : null,
            CounterReset = n.CounterReset is { } cr ? cr with { Variable = map(cr.Variable) } : null,
            CounterLoad = n.CounterLoad is { } cl ? cl with { Variable = map(cl.Variable) } : null,
            NumericOperation = n.NumericOperation is { } op ? op with { SourceA = map(op.SourceA), SourceB = map(op.SourceB), SourceC = map(op.SourceC), Destination = map(op.Destination) } : null,
            Return = n.Return is { ValueKind: LadderReturnValueKind.Operand } ret
                ? ret with { Operand = map(ret.Operand) } : n.Return,
        };
    }
}
