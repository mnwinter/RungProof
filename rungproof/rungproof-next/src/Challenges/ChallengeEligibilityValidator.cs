using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RungProof.Next.VirtualController;
namespace RungProof.Next.Challenges;
/// <summary>Challenge-only restrictions. The general editor/compiler is unchanged.</summary>
public static class ChallengeEligibilityValidator
{
    private static readonly string[] RequiredInputs = ["operator.start", "operator.stop", "simulated_photoeye"];
    private static readonly SceneIoPoint[] Points = [new("simulated_photoeye", "BOOL", "PC", "feedback", "Modeled photoeye"), new("conveyor_running", "BOOL", "PLC", "command", "Modeled conveyor command")];
    public static IReadOnlyList<LadderValidationIssue> Validate(LadderProgram program)
    {
        var issues = new List<LadderValidationIssue>();
        void Issue(string path, string message) => issues.Add(new("CH_INVALID_PROGRAM", path, message));
        if (program is null)
        {
            Issue("$", "Program is required.");
            return issues.AsReadOnly();
        }
        if (program.SchemaVersion != 1 || program.Language != "LD" || program.ScanPeriod != TimeSpan.FromMilliseconds(20))
            Issue("$", "Requires schema 1, LD and exactly 20 ms.");
        if (program.Variables is null || program.Networks is null)
        {
            Issue("$", "Required collections cannot be null.");
            return issues.AsReadOnly();
        }
        if (program.Variables.Count > 32)
            Issue("$.variables", "At most 32 variables.");
        for (var i = 0; i < program.Variables.Count && i < 33; i++)
        {
            var v = program.Variables[i];
            var path = $"$.variables[{i}]";
            if (v is null)
            {
                Issue(path, "Null variable.");
                continue;
            }
            if (v.Type != PlcVariableType.Bool || v.InitialValue is not false || v.Aggregate is not null || v.EnumMembers is { Count: > 0 })
                Issue(path, "Only BOOL with literal false and no aggregate/enum metadata.");
            if (v.Role == PlcVariableRole.Memory)
            {
                if (v.Binding != "")
                    Issue(path + ".binding", "Memory is unbound.");
            }
            else if (v.Role == PlcVariableRole.Input)
            {
                if (!RequiredInputs.Contains(v.Binding, StringComparer.Ordinal))
                    Issue(path + ".binding", "Input requires a supported binding.");
            }
            else if (v.Role == PlcVariableRole.Output)
            {
                if (v.Binding != "conveyor_running")
                    Issue(path + ".binding", "Output requires conveyor_running.");
            }
            else
                Issue(path + ".role", "Unsupported variable role.");
        }
        foreach (var binding in RequiredInputs.Append("conveyor_running"))
        if (program.Variables.Count(v => v is not null && v.Binding == binding) != 1)
            Issue("$.variables", "Exactly one variable is required for " + binding + ".");
        var blocks = program.Blocks;
        var tasks = program.Tasks;
        if (blocks is { Count: > 1 })
            Issue("$.blocks", "Exactly one entry block is allowed.");
        if (blocks is not { Count: > 0 })
        {
            if (tasks is { Count: > 0 } || program.EntryBlock != "")
                Issue("$.tasks", "Legacy flat program has no blocks, tasks or entry ID.");
        }
        else
        {
            var block = blocks[0];
            if (block is null || block.Networks is null)
            {
                Issue("$.blocks[0]", "Block and networks required.");
                return issues.AsReadOnly();
            }
            if (block.BlockType is not (LadderBlockType.Function or LadderBlockType.OrganizationBlock) || block.Interface is { Count: > 0 })
                Issue("$.blocks[0]", "Only Function or OrganizationBlock without interface parameters.");
            if (program.EntryBlock != block.Id)
                Issue("$.entryBlock", "Entry must match the sole block.");
            if (tasks is { Count: > 1 })
                Issue("$.tasks", "At most one task.");
            if (tasks is { Count: 1 })
            {
                var task = tasks[0];
                if (task is null || task.Kind != LadderTaskKind.Continuous || task.Period != TimeSpan.FromMilliseconds(20) || task.Priority != 10 || task.EntryBlock != block.Id)
                    Issue("$.tasks[0]", "Task must be Continuous, 20 ms, priority 10, sole entry.");
            }
        }
        // Walk original objects before any lossy serializer/compiler traversal. Bounds also stop cycles.
        void CheckNetworks(IReadOnlyList<LadderNetwork> networks, string path)
        {
            if (networks.Count > 64)
            {
                Issue(path, "At most 64 effective networks.");
                return;
            }
            var nodes = 0;
            var ancestors = new HashSet<LadderNode>(ReferenceEqualityComparer.Instance);
            void Walk(LadderNode? node, string location, int depth)
            {
                if (node is null)
                {
                    Issue(location, "Logic required.");
                    return;
                }
                if (++nodes > 256)
                {
                    if (nodes == 257)
                        Issue(location, "At most 256 logic nodes.");
                    return;
                }
                if (depth > 16)
                {
                    Issue(location, "Branch depth exceeds 16.");
                    return;
                }
                if (!ancestors.Add(node))
                {
                    Issue(location, "Cyclic logic graph.");
                    return;
                }
                if (node.EdgeMode != LadderEdgeMode.None || node.CompareOperator != LadderCompareOperator.Equal || node.RightOperand != "")
                    Issue(location, "Unsupported edge/comparison or unused operand.");
                if (node.Kind == LadderNodeKind.Contact)
                {
                    if (node.Children is { Count: > 0 })
                        Issue(location, "Contact cannot hide children.");
                }
                else if (node.Kind is LadderNodeKind.Series or LadderNodeKind.Parallel)
                {
                    if (node.Variable != "" || node.NormallyClosed)
                        Issue(location, "Branch cannot hide contact fields.");
                    if (node.Children is null)
                        Issue(location, "Branch children required.");
                }
                else
                    Issue(location, "Only ordinary contacts, Series and Parallel.");
                if (node.Children is { } children)
                {
                    if (children.Count > 256)
                        Issue(location, "Too many children.");
                    for (var i = 0; i < children.Count && i < 257 && nodes <= 256; i++)
                        Walk(children[i], $"{location}.children[{i}]", depth + 1);
                }
                ancestors.Remove(node);
            }
            for (var i = 0; i < networks.Count; i++)
            {
                var n = networks[i];
                var location = $"{path}[{i}]";
                if (n is null)
                {
                    Issue(location, "Network required.");
                    continue;
                }
                if (n.Timer is not null || n.TimerReset is not null || n.Counter is not null || n.CounterReset is not null || n.CounterLoad is not null || n.NumericOperation is not null || n.Call is not null || n.Return is not null || n.Jump is not null || n.LabelInstruction is not null)
                    Issue(location, "Unsupported instruction, including unused fields.");
                if (n.Coil is null || n.Coil.Mode != LadderCoilMode.Assign)
                    Issue(location + ".coil", "Ordinary Assign coil required.");
                else if (!program.Variables.Any(v => v is not null && v.Name == n.Coil.Variable && v.Role is PlcVariableRole.Memory or PlcVariableRole.Output))
                    Issue(location + ".coil", "Coil must target memory/output.");
                Walk(n.Logic, location + ".logic", 1);
            }
        }
        CheckNetworks(program.Networks, "$.networks");
        if (blocks is { Count: 1 } && blocks[0]?.Networks is { } blockNetworks)
        {
            CheckNetworks(blockNetworks, "$.blocks[0].networks");
            if (issues.Count == 0 && ChallengeProgramIdentity.CanonicalProgramJson(JsonSerializer.Serialize(program.Networks)) != ChallengeProgramIdentity.CanonicalProgramJson(JsonSerializer.Serialize(blockNetworks)))
                Issue("$.networks", "Root networks must match the sole block; hidden shadow instructions rejected.");
        }
        if (issues.Count != 0)
            return issues.AsReadOnly();
        issues.AddRange(LadderCompiler.Validate(program));
        if (issues.Count == 0)
            issues.AddRange(SceneIoBindingValidator.Validate(program, Points));
        return issues.AsReadOnly();
    }
}

