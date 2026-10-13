using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum LadderCallCompletion { Completed, Aborted }
public sealed record LadderCallTransfer(string FormalId, LadderInterfaceSection Section, string StorageIdentity,
    PlcVariableType Type, object? Value, string Phase);
public sealed record LadderCallInvocation(long FrameId, string CallId, string BlockId, string TaskId, string InstanceId,
    LadderCallCompletion Completion, IReadOnlyList<LadderCallTransfer> Transfers, string Failure = "");

public sealed partial class VirtualControllerRuntime
{
    private sealed class CallFrame
    {
        public required long Id;
        public required LadderCall Call;
        public required LadderBlock Block;
        public required string TaskId;
        public readonly Dictionary<string, string> Symbols = new(StringComparer.Ordinal);
        public readonly List<string> Owned = [];
        public readonly List<(LadderInterfaceParameter Formal, string Source, string Destination)> CopyOut = [];
        public readonly List<LadderCallTransfer> Transfers = [];
    }
    private readonly Dictionary<long, CallFrame> _callFrames = [];
    private readonly HashSet<string> _activeInstances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PlcVariable> _storageDeclarations = new(StringComparer.Ordinal);
    private readonly List<LadderCallInvocation> _callInvocations = [];
    private readonly Dictionary<(string Instance, string Formal), string> _instanceStorageKeys = [];
    private readonly HashSet<string> _instanceStorageIdentities = new(StringComparer.Ordinal);
    private readonly HashSet<string> _occupiedStorageNames = new(StringComparer.Ordinal);

    private string AvailableStorageKey(string preferred, PlcVariableType type) => LadderStorageNamespace.Reserve(_occupiedStorageNames, preferred, type);

    private string InstanceStorage(string instance, string formal, PlcVariableType type)
    {
        if (_instanceStorageKeys.TryGetValue((instance, formal), out var existing)) return existing;
        var key = AvailableStorageKey("@instance/" + Uri.EscapeDataString(instance) + "/" + Uri.EscapeDataString(formal), type);
        _instanceStorageKeys.Add((instance, formal), key); _instanceStorageIdentities.Add(key);
        return key;
    }

    private void ResetCallStorage()
    {
        _callFrames.Clear(); _activeInstances.Clear(); _callInvocations.Clear(); _storageDeclarations.Clear();
        _instanceStorageKeys.Clear(); _instanceStorageIdentities.Clear();
        _occupiedStorageNames.Clear();
        foreach (var v in _program.Variables.Values) _storageDeclarations.Add(v.Name, v);
        foreach (var v in _program.Variables.Values) _occupiedStorageNames.UnionWith(LadderStorageNamespace.Names(v.Name, v.Type));
        foreach (var instance in _program.Source.Instances ?? [])
        foreach (var formal in _blocks[instance.BlockId].Interface ?? [])
            if (formal.Section is LadderInterfaceSection.Output or LadderInterfaceSection.Static)
                InitializeStorage(InstanceStorage(instance.Id, formal.Id, formal.Type), formal);
    }

    private void InitializeStorage(string identity, LadderInterfaceParameter formal)
    {
        var declaration = LadderCallBindings.Declaration("", formal) with { Name = identity, Role = PlcVariableRole.Memory };
        _storageDeclarations[identity] = declaration;
        if (formal.Type == PlcVariableType.Timer) _timers[identity] = new(TimeSpan.Zero, TimeSpan.Zero, false, false);
        else if (formal.Type == PlcVariableType.Counter) _counters[identity] = new(0, 0, false, false);
        else WriteCallScalar(identity, formal.InitialValue);
    }

    private void WriteCallScalar(string identity, object value)
    {
        var declaration = _storageDeclarations[identity];
        if (declaration.Type == PlcVariableType.Bool) _values[identity] = (bool)value;
        else if (PlcTextValues.IsText(declaration.Type)) _textValues[identity] = (string)value;
        else _numericValues[identity] = NormalizeNumeric(declaration.Type, Convert.ToDouble(value, CultureInfo.InvariantCulture));
    }

    private string ResolveCallStorage(string reference, long frameId)
    {
        if (reference.StartsWith("global:", StringComparison.Ordinal))
        {
            var owner = _callFrames.TryGetValue(frameId, out var current) ? current.Block : _blocks.Values.First();
            if (LadderCallBindings.ResolveDeclaration(_program.Source, owner, reference) is { } global) return global.Name;
            var dot = reference.LastIndexOf('.');
            if (dot > 7 && LadderCallBindings.ResolveDeclaration(_program.Source, owner, reference[..dot]) is { } instance)
                return instance.Name + reference[dot..];
        }
        if (reference.StartsWith("local:", StringComparison.Ordinal) && _callFrames.TryGetValue(frameId, out var frame))
        {
            if (frame.Symbols.TryGetValue(reference[6..], out var exact)) return exact;
            var dot = reference.LastIndexOf('.');
            if (dot > 6 && frame.Symbols.TryGetValue(reference[6..dot], out var parent)) return parent + reference[dot..];
        }
        return reference;
    }

    private bool TryEnterCall(LadderCall call, ExecutionItem parent, long frameId, out string failure)
    {
        failure = "";
        if ((parent.Path?.Count ?? 1) >= 32 && (call.SemanticMode == LadderSemanticMode.Parameterized || _callFrames.Count > 0))
        { failure = "VC_RUNTIME_CALL_DEPTH: maximum call depth 32 exceeded."; return false; }
        if (call.SemanticMode == LadderSemanticMode.Legacy) return true;
        var block = _blocks[call.TargetBlock];
        if (call.InstanceId.Length > 0 && _activeInstances.Contains(call.InstanceId))
        { failure = "VC_RUNTIME_CALL_INSTANCE: nested invocation of active instance '" + call.InstanceId + "'."; return false; }
        // Resolve every ultimate writable identity before creating storage or sampling a body.
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        var writable = new HashSet<string>(StringComparer.Ordinal);
        foreach (var formal in block.Interface ?? [])
        {
            if (formal.Section is not (LadderInterfaceSection.Input or LadderInterfaceSection.Output or LadderInterfaceSection.InOut)) continue;
            var binding = call.Bindings!.Single(b => b.ParameterId == formal.Id);
            if (binding.Kind != LadderActualKind.Symbol) continue;
            var key = ResolveCallStorage(binding.Symbol, parent.FrameId);
            if (!_storageDeclarations.ContainsKey(key)) { failure = "VC_RUNTIME_CALL_BINDING: actual storage is unavailable."; return false; }
            resolved.Add(formal.Id, key);
            if (formal.Section != LadderInterfaceSection.Input && !writable.Add(key))
            { failure = "VC_RUNTIME_CALL_ALIAS: forwarded writable actuals resolve to the same storage."; return false; }
        }
        var frame = new CallFrame { Id = frameId, Call = call, Block = block, TaskId = parent.TaskId };
        foreach (var formal in block.Interface ?? [])
        {
            var binding = call.Bindings?.FirstOrDefault(b => b.ParameterId == formal.Id);
            if (formal.Section == LadderInterfaceSection.InOut)
            {
                var alias = resolved[formal.Id]; frame.Symbols.Add(formal.Id, alias);
                frame.Transfers.Add(new(formal.Id, formal.Section, alias, formal.Type, StorageValue(alias), "AliasResolved"));
                continue;
            }
            var persistent = block.BlockType == LadderBlockType.FunctionBlock && formal.Section is LadderInterfaceSection.Output or LadderInterfaceSection.Static;
            var storage = persistent ? InstanceStorage(call.InstanceId, formal.Id, formal.Type) : AvailableStorageKey("@frame/" + frameId + "/" + Uri.EscapeDataString(formal.Id), formal.Type);
            frame.Symbols.Add(formal.Id, storage);
            if (!persistent) { InitializeStorage(storage, formal); frame.Owned.Add(storage); }
            if (formal.Section == LadderInterfaceSection.Input)
            {
                var value = binding!.Kind == LadderActualKind.Literal ? binding.Literal! : StorageValue(resolved[formal.Id])!;
                WriteCallScalar(storage, value);
                frame.Transfers.Add(new(formal.Id, formal.Section, binding.Kind == LadderActualKind.Literal ? "literal" : resolved[formal.Id], formal.Type, value, "CopyIn"));
            }
            else if (formal.Section == LadderInterfaceSection.Output && binding!.Kind != LadderActualKind.Discard)
                frame.CopyOut.Add((formal, storage, resolved[formal.Id]));
        }
        _callFrames.Add(frameId, frame);
        if (call.InstanceId.Length > 0) _activeInstances.Add(call.InstanceId);
        return true;
    }

    private void FinishCall(long frameId, bool completed, string failure = "")
    {
        if (!_callFrames.Remove(frameId, out var frame)) return;
        if (completed)
            foreach (var (formal, source, destination) in frame.CopyOut)
            {
                var value = StorageValue(source)!;
                WriteCallScalar(destination, value);
                frame.Transfers.Add(new(formal.Id, formal.Section, destination, formal.Type, value, "CopyOut"));
            }
        else
            foreach (var (formal, source, destination) in frame.CopyOut)
                frame.Transfers.Add(new(formal.Id, formal.Section, destination, formal.Type, StorageValue(source), "CopyOutSuppressed"));
        foreach (var key in frame.Owned)
        {
            _occupiedStorageNames.ExceptWith(LadderStorageNamespace.Names(key, _storageDeclarations[key].Type));
            _storageDeclarations.Remove(key); _values.Remove(key); _numericValues.Remove(key); _textValues.Remove(key);
        }
        if (frame.Call.InstanceId.Length > 0) _activeInstances.Remove(frame.Call.InstanceId);
        _callInvocations.Add(new(frame.Id, frame.Call.Id, frame.Block.Id, frame.TaskId, frame.Call.InstanceId,
            completed ? LadderCallCompletion.Completed : LadderCallCompletion.Aborted, Array.AsReadOnly(frame.Transfers.ToArray()), failure));
    }

    private InstructionObservationCapture? AttachCallObservations(InstructionObservationCapture? capture)
    {
        if (capture is null || _callInvocations.Count == 0) return capture;
        var invocations = _callInvocations.ToDictionary(i => i.FrameId);
        InstructionObservation Attach(InstructionObservation observation)
        {
            if (observation.CalledFrameInvocationId is not { } id || !invocations.TryGetValue(id, out var call)) return observation;
            return observation with {
                CalledInstanceId = call.InstanceId.Length == 0 ? null : call.InstanceId,
                ControlOutcome = call.Completion.ToString(),
                Operands = call.Transfers.Select(t => new InstructionOperand(t.Phase + ":" + t.FormalId, t.StorageIdentity, t.Type, t.Value)).ToArray(),
                Diagnostics = call.Failure.Length == 0 ? observation.Diagnostics : observation.Diagnostics.Append(
                    new InstructionDiagnostic("VC_RUNTIME_CALL_ABORT", call.CallId, "", "CallExit", call.Failure)).ToArray()
            };
        }
        return capture with {
            GlobalLast = new ReadOnlyDictionary<string, InstructionObservation>(capture.GlobalLast.ToDictionary(p => p.Key, p => Attach(p.Value), StringComparer.Ordinal)),
            Contexts = new ReadOnlyDictionary<string, InstructionObservation>(capture.Contexts.ToDictionary(p => p.Key, p => Attach(p.Value), StringComparer.Ordinal)),
            Trace = Array.AsReadOnly(capture.Trace.Select(Attach).ToArray())
        };
    }
}
