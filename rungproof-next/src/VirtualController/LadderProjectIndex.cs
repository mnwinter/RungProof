using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum LadderReferenceKind
{
    Declaration,
    BlockDeclaration,
    NetworkDeclaration,
    TaskDeclaration,
    Read,
    Write,
    Call,
    ProgramControl,
    ScheduledEntry,
}

public sealed record LadderProjectReference(
    string Symbol,
    string RootSymbol,
    LadderReferenceKind Kind,
    string BlockId,
    string BlockName,
    int NetworkIndex,
    string NetworkId,
    string NetworkLabel,
    string ElementId,
    string Detail)
{
    public bool HasNetwork => NetworkIndex >= 0 && NetworkId.Length > 0;
}

/// <summary>
/// Project-wide semantic index used by Find All and cross-reference. It walks
/// the same immutable Ladder IR that is compiler-validated and executed, so a
/// result identifies an actual declaration or instruction operand rather than
/// a coincidental UI text match.
/// </summary>
public sealed class LadderProjectIndex
{
    private readonly IReadOnlyList<LadderProjectReference> _references;

    public IReadOnlyList<LadderProjectReference> References => _references;

    private LadderProjectIndex(IReadOnlyList<LadderProjectReference> references) =>
        _references = references;

    public static LadderProjectIndex Build(LadderProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        var references = new List<LadderProjectReference>();
        var variableNames = new HashSet<string>(program.Variables.Select(item => item.Name), StringComparer.Ordinal);
        var blocks = program.Blocks is { Count: > 0 }
            ? program.Blocks
            : new[] { new LadderBlock("main", program.Name, program.Networks) };
        var blockNames = blocks.ToDictionary(block => block.Id, block => block.Name, StringComparer.Ordinal);

        foreach (var variable in program.Variables)
        {
            references.Add(new LadderProjectReference(
                variable.Name,
                variable.Name,
                LadderReferenceKind.Declaration,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{variable.Type.ToString().ToUpperInvariant()} {variable.Role.ToString().ToUpperInvariant()}"
                    + (string.IsNullOrWhiteSpace(variable.Binding) ? string.Empty : $" · binding {variable.Binding}")));
        }

        foreach (var task in program.Tasks ?? [])
        {
            var name = blockNames.GetValueOrDefault(task.EntryBlock, task.EntryBlock);
            references.Add(new LadderProjectReference(
                task.Name,
                task.Id,
                LadderReferenceKind.TaskDeclaration,
                task.EntryBlock,
                name,
                -1,
                string.Empty,
                string.Empty,
                task.Id,
                $"{task.Kind} · {task.Period.TotalMilliseconds:0} ms · priority {task.Priority}"));
            references.Add(new LadderProjectReference(
                name,
                task.EntryBlock,
                LadderReferenceKind.ScheduledEntry,
                task.EntryBlock,
                name,
                -1,
                string.Empty,
                string.Empty,
                task.Id,
                $"{task.Name} · {task.Kind} · priority {task.Priority}"));
        }

        foreach (var block in blocks)
        {
            references.Add(new LadderProjectReference(
                block.Name,
                block.Id,
                LadderReferenceKind.BlockDeclaration,
                block.Id,
                block.Name,
                -1,
                string.Empty,
                string.Empty,
                block.Id,
                "Program block / routine"));
            for (var networkIndex = 0; networkIndex < block.Networks.Count; networkIndex++)
            {
                var network = block.Networks[networkIndex];
                references.Add(new LadderProjectReference(
                    network.Label,
                    network.Id,
                    LadderReferenceKind.NetworkDeclaration,
                    block.Id,
                    block.Name,
                    networkIndex,
                    network.Id,
                    network.Label,
                    network.Id,
                    "Network / rung"));
                void AddOperand(string operand, LadderReferenceKind kind, string elementId, string detail)
                {
                    if (string.IsNullOrWhiteSpace(operand)
                        || double.TryParse(operand, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return;
                    var root = RootSymbol(operand, variableNames);
                    references.Add(new LadderProjectReference(
                        operand,
                        root,
                        kind,
                        block.Id,
                        block.Name,
                        networkIndex,
                        network.Id,
                        network.Label,
                        elementId,
                        detail));
                }

                void Walk(LadderNode node)
                {
                    if (node.Kind == LadderNodeKind.Contact)
                    {
                        AddOperand(node.Variable, LadderReferenceKind.Read, node.Id,
                            node.EdgeMode switch
                            {
                                LadderEdgeMode.Rising => "P edge / XIC+ONS rising-edge read",
                                LadderEdgeMode.Falling => "N edge / XIO+ONS/OSF falling-edge read",
                                _ => node.NormallyClosed ? "NC / XIO contact" : "NO / XIC contact",
                            });
                        if (node.EdgeMemoryOperand is { } edgeMemory)
                        {
                            AddOperand(edgeMemory, LadderReferenceKind.Read, node.Id, "P/N edge memory: previous signal state");
                            AddOperand(edgeMemory, LadderReferenceKind.Write, node.Id, "P/N edge memory: updated signal state");
                        }
                    }
                    else if (node.Kind == LadderNodeKind.Compare)
                    {
                        AddOperand(node.Variable, LadderReferenceKind.Read, node.Id,
                            $"{node.CompareOperator} left operand");
                        AddOperand(node.RightOperand, LadderReferenceKind.Read, node.Id,
                            $"{node.CompareOperator} right operand");
                    }
                    foreach (var child in node.Children ?? []) Walk(child);
                }

                Walk(network.Logic);
                if (network.Coil is not null)
                    AddOperand(network.Coil.Variable, LadderReferenceKind.Write, network.Coil.Id,
                        $"{network.Coil.Mode} coil");
                else if (network.Timer is not null)
                    AddOperand(network.Timer.Variable, LadderReferenceKind.Write, network.Timer.Id,
                        $"{network.Timer.Kind} timer instance");
                else if (network.TimerReset is not null)
                    AddOperand(network.TimerReset.Variable, LadderReferenceKind.Write, network.TimerReset.Id,
                        "Timer reset / RT / RES");
                else if (network.Counter is not null)
                {
                    AddOperand(network.Counter.Variable, LadderReferenceKind.Write, network.Counter.Id,
                        network.Counter.Kind == LadderCounterKind.CountDown ? "CTD instance" : "CTU instance");
                    if (network.Counter.Kind == LadderCounterKind.CountDown
                        && !network.Counter.LoadInput.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                        && !network.Counter.LoadInput.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                        AddOperand(network.Counter.LoadInput, LadderReferenceKind.Read, network.Counter.Id, "CTD LD input");
                    if (network.Counter.Kind == LadderCounterKind.CountUp
                        && !network.Counter.ResetInput.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                        && !network.Counter.ResetInput.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                        AddOperand(network.Counter.ResetInput, LadderReferenceKind.Read, network.Counter.Id, "CTU R input");
                }
                else if (network.CounterReset is not null)
                    AddOperand(network.CounterReset.Variable, LadderReferenceKind.Write, network.CounterReset.Id, "Counter reset / RES");
                else if (network.CounterLoad is not null)
                    AddOperand(network.CounterLoad.Variable, LadderReferenceKind.Write, network.CounterLoad.Id, "Counter preset load");
                else if (network.NumericOperation is not null)
                {
                    AddOperand(network.NumericOperation.SourceA, LadderReferenceKind.Read, network.NumericOperation.Id,
                        $"{network.NumericOperation.Kind} source A");
                    if (LadderNumericOperationRules.RequiresSourceB(network.NumericOperation.Kind))
                        AddOperand(network.NumericOperation.SourceB, LadderReferenceKind.Read, network.NumericOperation.Id,
                            $"{network.NumericOperation.Kind} source B");
                    if (LadderNumericOperationRules.RequiresSourceC(network.NumericOperation.Kind))
                        AddOperand(network.NumericOperation.SourceC, LadderReferenceKind.Read, network.NumericOperation.Id,
                            $"{network.NumericOperation.Kind} source C");
                    AddOperand(network.NumericOperation.Destination, LadderReferenceKind.Write, network.NumericOperation.Id,
                        $"{network.NumericOperation.Kind} destination");
                }
                else if (network.Call is not null)
                {
                    var targetName = blockNames.GetValueOrDefault(network.Call.TargetBlock, network.Call.TargetBlock);
                    references.Add(new LadderProjectReference(
                        targetName,
                        network.Call.TargetBlock,
                        LadderReferenceKind.Call,
                        block.Id,
                        block.Name,
                        networkIndex,
                        network.Id,
                        network.Label,
                        network.Call.Id,
                        $"CALL / JSR {targetName}"));
                }
                else if (network.Return is not null)
                {
                    references.Add(new LadderProjectReference(
                        "RETURN",
                        "RETURN",
                        LadderReferenceKind.ProgramControl,
                        block.Id,
                        block.Name,
                        networkIndex,
                        network.Id,
                        network.Label,
                        network.Return.Id,
                        "Conditional RETURN / RET"));
                }
                else if (network.Jump is not null)
                {
                    references.Add(new LadderProjectReference(
                        network.Jump.TargetLabel,
                        network.Jump.TargetLabel,
                        LadderReferenceKind.ProgramControl,
                        block.Id,
                        block.Name,
                        networkIndex,
                        network.Id,
                        network.Label,
                        network.Jump.Id,
                        $"JMP target {network.Jump.TargetLabel}"));
                }
                if (network.LabelInstruction is not null)
                {
                    references.Add(new LadderProjectReference(
                        network.LabelInstruction.Name,
                        network.LabelInstruction.Name,
                        LadderReferenceKind.ProgramControl,
                        block.Id,
                        block.Name,
                        networkIndex,
                        network.Id,
                        network.Label,
                        network.LabelInstruction.Id,
                        $"LBL / LABEL declaration {network.LabelInstruction.Name}"));
                }
            }
        }

        return new LadderProjectIndex(references);
    }

    public IReadOnlyList<LadderProjectReference> CrossReference(string symbol)
    {
        var query = symbol.Trim();
        if (query.Length == 0) return [];
        return _references.Where(reference =>
                reference.Symbol.Equals(query, StringComparison.OrdinalIgnoreCase)
                || reference.RootSymbol.Equals(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ReferenceOrder)
            .ThenBy(reference => reference.NetworkIndex)
            .ThenBy(reference => reference.ElementId, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<LadderProjectReference> Search(string text)
    {
        var query = text.Trim();
        if (query.Length == 0) return [];
        return _references.Where(reference =>
                Contains(reference.Symbol, query)
                || Contains(reference.RootSymbol, query)
                || Contains(reference.BlockName, query)
                || Contains(reference.NetworkLabel, query)
                || Contains(reference.Detail, query)
                || Contains(reference.Kind.ToString(), query))
            .OrderBy(ReferenceOrder)
            .ThenBy(reference => reference.NetworkIndex)
            .ThenBy(reference => reference.ElementId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string RootSymbol(string operand, IReadOnlySet<string> variables)
    {
        if (variables.Contains(operand)) return operand;
        var dot = operand.LastIndexOf('.');
        if (dot > 0 && variables.Contains(operand[..dot])) return operand[..dot];
        return operand;
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static string ReferenceOrder(LadderProjectReference reference) =>
        $"{reference.RootSymbol}\u001f{(int)reference.Kind:D2}\u001f{reference.BlockName}";
}
