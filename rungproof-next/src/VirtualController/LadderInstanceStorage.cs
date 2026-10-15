using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed partial class VirtualControllerRuntime
{
    // Scope and source ID occupy different fields: no legal legacy ID can equal
    // a generated instance/path identity merely by copying its displayed text.
    private readonly record struct InstructionMemoryIdentity(string Scope, string Id);
    private readonly HashSet<string> _instanceTimerQuantumUsed = new(StringComparer.Ordinal);
    private readonly Dictionary<InstructionMemoryIdentity, string> _counterEdgeStorage = [];

    private InstructionMemoryIdentity InstructionMemoryKey(string nodeId)
    {
        if (_observationContext is not { } context || !_callFrames.TryGetValue(context.FrameId, out var frame)) return new("", nodeId);
        if (frame.Block.BlockType == LadderBlockType.FunctionBlock) return new("FB:" + frame.Call.InstanceId, nodeId);
        // Static call-site identity excludes task roots and per-scan invocation ordinals.
        // Include owning FB ancestry so the same FC body inside different FBs is isolated.
        var sites = (context.Path ?? []).Skip(1).ToArray();
        var lastInstance = Array.FindLastIndex(sites, p => p.InstanceId is not null);
        var ancestry = string.Join("/", sites.Where(p => p.InstanceId is not null).Select(p => Uri.EscapeDataString(p.InstanceId!)));
        var path = sites.Skip(lastInstance + 1).Select(p => Uri.EscapeDataString(p.CallSiteId));
        return new("FC:" + ancestry + "//" + string.Join("/", path), nodeId);
    }

    private InstructionMemoryIdentity CounterMemoryKey(LadderCounter instruction)
    {
        var key = new InstructionMemoryIdentity(_instanceStorageIdentities.Contains(instruction.Variable) ? instruction.Variable : "", instruction.Id);
        _counterEdgeStorage[key] = instruction.Variable;
        return key;
    }

    private TimeSpan TimerQuantum(string variable)
    {
        if (!_instanceStorageIdentities.Contains(variable)) return ScanPeriod;
        return _instanceTimerQuantumUsed.Add(variable) ? ScanPeriod : TimeSpan.Zero;
    }

    private void RegisterInstanceRetentiveTimers()
    {
        _retentiveTimerVariables.Clear();
        foreach (var block in _blocks.Values)
        foreach (var network in block.Networks)
        {
            if (network.Timer is not { Kind: LadderTimerKind.RetentiveOnDelay } timer) continue;
            if (block.SemanticMode == LadderSemanticMode.Legacy) _retentiveTimerVariables.Add(timer.Variable);
            else if (timer.Variable.StartsWith("global:", StringComparison.Ordinal)
                && LadderCallBindings.ResolveDeclaration(_program.Source, block, timer.Variable) is { } global)
                _retentiveTimerVariables.Add(global.Name);
        }
        foreach (var instance in _program.Source.Instances ?? [])
        foreach (var network in _blocks[instance.BlockId].Networks)
            if (network.Timer is { Kind: LadderTimerKind.RetentiveOnDelay } timer && timer.Variable.StartsWith("local:", StringComparison.Ordinal))
                _retentiveTimerVariables.Add(InstanceStorage(instance.Id, timer.Variable[6..], PlcVariableType.Timer));
    }
}
