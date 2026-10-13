using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum VirtualControllerState
{
    Stopped,
    Running,
}

public sealed record LadderElementState(string Id, bool Energized);
public sealed record LadderTimerState(
    TimeSpan Preset,
    TimeSpan Accumulated,
    bool Timing,
    bool Done,
    bool Input = false);
public sealed record LadderCounterState(long Preset, long Accumulated, bool CountInput, bool Done);
public sealed record LadderTaskState(
    string Id,
    string Name,
    LadderTaskKind Kind,
    TimeSpan Period,
    int Priority,
    string EntryBlock,
    bool Due,
    long ExecutionCount);
public sealed record LadderForceState(string Variable, bool Value, PlcVariableRole Role);

public sealed record VirtualControllerSnapshot(
    long ScanNumber,
    TimeSpan SimulatedTime,
    VirtualControllerState State,
    IReadOnlyDictionary<string, bool> Variables,
    IReadOnlyDictionary<string, double> NumericVariables,
    IReadOnlyDictionary<string, bool> Outputs,
    IReadOnlyDictionary<string, double> NumericOutputs,
    IReadOnlyDictionary<string, LadderTimerState> Timers,
    IReadOnlyDictionary<string, LadderCounterState> Counters,
    IReadOnlyDictionary<string, LadderTaskState> Tasks,
    IReadOnlyDictionary<string, LadderForceState> Forces,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyDictionary<string, LadderElementState> Elements
)
{
    public IReadOnlyDictionary<string, string> TextVariables { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> TextOutputs { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, object> Aggregates { get; init; } = new Dictionary<string, object>();
    public InstructionObservationCapture? Observations { get; init; }
    public ControllerCompletion Completion { get; init; }
    public VirtualControllerSignalImage? ProgramEndValues { get; init; }
    public int ExecutedNetworkCount { get; init; }
    public string? LastFullyObservedInstructionId { get; init; }
    public long LastFullyObservedOrdinal { get; init; }
    public IReadOnlyList<LadderCallInvocation> CallInvocations { get; init; } = [];
}

/// <summary>
/// Deterministic scan executor for the validated Phase 1 LD subset. It owns
/// program memory only; plant physics and physical PLC transport are outside
/// this module.
/// </summary>
public sealed partial class VirtualControllerRuntime
{
    private sealed record ExecutionItem(LadderNetwork? Network, string BlockId = "", int NetworkIndex = -1,
        string TaskId = "", long FrameId = 0, IReadOnlyList<ObservationCallSite>? Path = null);

    private readonly CompiledLadderProgram _program;
    private readonly IReadOnlyDictionary<string, LadderBlock> _blocks;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> _blockLabels;
    private readonly HashSet<string> _retentiveTimerVariables;
    private readonly string _entryBlock;
    private readonly IReadOnlyList<LadderTask> _tasks;
    private readonly Dictionary<string, bool> _values = new(StringComparer.Ordinal);
    // Raw input image stays independent of force overrides, including inputs
    // absent from later partial samples (for example momentary operator commands).
    private readonly Dictionary<string, bool> _unforcedBoolInputs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _textValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _numericValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LadderTimerState> _timers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LadderCounterState> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<InstructionMemoryIdentity, bool> _counterInputs = [];
    private readonly Dictionary<InstructionMemoryIdentity, bool> _edgeInputs = [];
    private readonly Dictionary<string, LadderElementState> _elements = new(StringComparer.Ordinal);
    private readonly List<string> _diagnostics = [];
    private readonly Dictionary<string, long> _taskExecutions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dueTasks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LadderForceState> _forces = new(StringComparer.Ordinal);
    private long _scanNumber;
    private TimeSpan _simulatedTime;

    public VirtualControllerState State { get; private set; } = VirtualControllerState.Stopped;
    public TimeSpan ScanPeriod => _program.Source.ScanPeriod;
    public VirtualControllerSnapshot Snapshot => CreateSnapshot();

    public VirtualControllerRuntime(CompiledLadderProgram program)
    {
        _program = program;
        var blocks = program.Source.Blocks is { Count: > 0 }
            ? program.Source.Blocks
            : [new LadderBlock("main", program.Source.Name, program.Source.Networks)];
        _blocks = blocks.ToDictionary(block => block.Id, StringComparer.Ordinal);
        _blockLabels = blocks.ToDictionary(
            block => block.Id,
            block => (IReadOnlyDictionary<string, int>)block.Networks
                .Select((network, index) => (network.LabelInstruction, index))
                .Where(item => item.LabelInstruction is not null)
                .ToDictionary(item => item.LabelInstruction!.Name, item => item.index, StringComparer.Ordinal),
            StringComparer.Ordinal);
        _retentiveTimerVariables = blocks
            .SelectMany(block => block.Networks)
            .Where(network => network.Timer?.Kind == LadderTimerKind.RetentiveOnDelay)
            .Select(network => network.Timer!.Variable)
            .ToHashSet(StringComparer.Ordinal);
        _entryBlock = string.IsNullOrWhiteSpace(program.Source.EntryBlock)
            ? blocks[0].Id
            : program.Source.EntryBlock;
        _tasks = program.Source.Tasks is { Count: > 0 }
            ? program.Source.Tasks
            : [new LadderTask("main-task", "MainTask", LadderTaskKind.Continuous,
                program.Source.ScanPeriod, 10, _entryBlock)];
        Reset();
    }

    public void Run() => State = VirtualControllerState.Running;

    // A deliberate momentary press has an observed released state before it.
    // Seed only unobserved edge contacts for that BOOL input. Held startup
    // inputs, existing edge memory, and forced inputs retain their semantics.
    public void PrepareExplicitInputPulse(string name)
    {
        if (State != VirtualControllerState.Running || _forces.ContainsKey(name)
            || !_program.Variables.TryGetValue(name, out var variable)
            || variable.Role != PlcVariableRole.Input || variable.Type != PlcVariableType.Bool) return;
        void Visit(LadderNode node)
        {
            if (node.Kind == LadderNodeKind.Contact && node.EdgeMode != LadderEdgeMode.None && node.Variable == name)
                _edgeInputs.TryAdd(new("", node.Id), false);
            foreach (var child in node.Children ?? []) Visit(child);
        }
        foreach (var network in _blocks.Values.SelectMany(block => block.Networks)) Visit(network.Logic);
    }

    /// <summary>
    /// Applies an offline-simulator force to a declared BOOL input or output.
    /// Memory tags and non-BOOL values are deliberately excluded so forcing
    /// remains an explicit I/O-image override rather than hidden program logic.
    /// </summary>
    public VirtualControllerSnapshot SetBoolForce(string variableName, bool value)
    {
        if (!_program.Variables.TryGetValue(variableName, out var variable))
            throw new ArgumentException($"Unknown force target '{variableName}'.", nameof(variableName));
        if (variable.Type != PlcVariableType.Bool
            || variable.Role is not (PlcVariableRole.Input or PlcVariableRole.Output))
            throw new InvalidOperationException(
                $"Force target '{variableName}' must be a declared BOOL input or output.");

        _forces[variableName] = new LadderForceState(variableName, value, variable.Role);
        if (variable.Role == PlcVariableRole.Input || State == VirtualControllerState.Running)
            _values[variableName] = value;
        return CreateSnapshot();
    }

    public VirtualControllerSnapshot RemoveForce(string variableName)
    {
        if (_forces.Remove(variableName, out var force) && force.Role == PlcVariableRole.Input)
            _values[variableName] = _unforcedBoolInputs[variableName];
        return CreateSnapshot();
    }

    public VirtualControllerSnapshot ClearForces()
    {
        foreach (var force in _forces.Values.Where(item => item.Role == PlcVariableRole.Input))
            _values[force.Variable] = _unforcedBoolInputs[force.Variable];
        _forces.Clear();
        return CreateSnapshot();
    }

    public VirtualControllerSnapshot Stop()
    {
        State = VirtualControllerState.Stopped;
        _dueTasks.Clear();
        _edgeInputs.Clear();
        foreach (var variable in _program.Variables.Values.Where(item =>
                     item.Role == PlcVariableRole.Output && item.Type == PlcVariableType.Bool))
            _values[variable.Name] = false;
        foreach (var variable in _program.Variables.Values.Where(item =>
                     item.Role == PlcVariableRole.Output
                     && item.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real))
            _numericValues[variable.Name] = 0.0;
        foreach (var variable in _program.Variables.Values.Where(item => item.Role == PlcVariableRole.Output && PlcTextValues.IsText(item.Type)))
            _textValues[variable.Name] = variable.Type == PlcVariableType.Enum ? variable.EnumMembers![0] : string.Empty;
        foreach (var name in _timers.Keys.ToArray())
            _timers[name] = _retentiveTimerVariables.Contains(name)
                ? _timers[name] with { Timing = false, Input = false }
                : _timers[name] with
                {
                    Accumulated = TimeSpan.Zero,
                    Timing = false,
                    Done = false,
                    Input = false,
                };
        return CreateSnapshot();
    }

    public VirtualControllerSnapshot Reset()
    {
        _observations = null; _collector = null; _activeObservation = null; _reusableCollector?.Reset(); _observedPaths.Clear();
        _completion = ControllerCompletion.NoExecution; _programEndValues = null;
        _completedNetworks=0;_lastFullyObservedId=null;_progressOrdinal=0;
        State = VirtualControllerState.Stopped;
        _scanNumber = 0;
        _simulatedTime = TimeSpan.Zero;
        _elements.Clear();
        _diagnostics.Clear();
        _values.Clear();
        _unforcedBoolInputs.Clear();
        _numericValues.Clear();
        _textValues.Clear();
        _timers.Clear();
        _counters.Clear();
        _counterInputs.Clear();
        _edgeInputs.Clear();
        _taskExecutions.Clear();
        _dueTasks.Clear();
        _forces.Clear();
        foreach (var task in _tasks) _taskExecutions[task.Id] = 0;
        foreach (var variable in _program.Variables.Values)
        {
            if (variable.Type == PlcVariableType.Bool)
            {
                _values[variable.Name] = variable.InitialValue is true;
                if (variable.Role == PlcVariableRole.Input)
                    _unforcedBoolInputs[variable.Name] = variable.InitialValue is true;
            }
            else if (variable.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
                _numericValues[variable.Name] = Convert.ToDouble(variable.InitialValue, CultureInfo.InvariantCulture);
            else if (PlcTextValues.IsText(variable.Type))
                _textValues[variable.Name] = (string)variable.InitialValue;
            else if (variable.Type == PlcVariableType.Timer)
                _timers[variable.Name] = new LadderTimerState(TimeSpan.Zero, TimeSpan.Zero, false, false);
            else if (variable.Type == PlcVariableType.Counter)
                _counters[variable.Name] = new LadderCounterState(0, 0, false, false);
        }
        foreach (var network in _blocks.Values.Where(block => block.SemanticMode == LadderSemanticMode.Legacy).SelectMany(block => block.Networks).Where(item => item.Counter is not null))
        {
            var counter = network.Counter!;
            _counters[counter.Variable] = new LadderCounterState(counter.Preset, 0, false, false);
            _counterInputs[new("", counter.Id)] = false;
        }
        foreach (var load in _blocks.Values.Where(block => block.SemanticMode == LadderSemanticMode.Legacy).SelectMany(block => block.Networks)
                     .Where(item => item.CounterLoad is not null).Select(item => item.CounterLoad!))
            _counters[load.Variable] = new LadderCounterState(load.Preset, 0, false, false);
        ResetCallStorage();
        _counterEdgeStorage.Clear(); _instanceTimerQuantumUsed.Clear(); RegisterInstanceRetentiveTimers();
        return CreateSnapshot();
    }

    /// <summary>Validates complete aggregate inputs before changing the scan image.</summary>
    public VirtualControllerSnapshot ScanAggregates(IReadOnlyDictionary<string, object> inputs)
    {
        var declarations = _program.Source.Variables.Where(v => v.Role == PlcVariableRole.Input && PlcAggregates.IsAggregate(v.Type)).ToArray();
        if (inputs.Keys.Any(name => !declarations.Any(v => v.Name == name)))
            throw new ArgumentException("Aggregate input is not a declared input root.");
        var leaves = PlcAggregates.Expand(declarations.Where(v => inputs.ContainsKey(v.Name)).Select(v => v with { InitialValue = inputs[v.Name] }).ToArray());
        foreach (var leaf in leaves)
        {
            if (leaf.Type == PlcVariableType.Int && ((long)leaf.InitialValue < short.MinValue || (long)leaf.InitialValue > short.MaxValue)
                || leaf.Type == PlcVariableType.DInt && ((long)leaf.InitialValue < int.MinValue || (long)leaf.InitialValue > int.MaxValue))
                throw new ArgumentException($"Aggregate input '{leaf.Name}' is outside its declared numeric range.");
        }
        return Scan(leaves.Where(v => v.Type == PlcVariableType.Bool).ToDictionary(v => v.Name, v => (bool)v.InitialValue),
            leaves.Where(v => v.Type != PlcVariableType.Bool).ToDictionary(v => v.Name, v => Convert.ToDouble(v.InitialValue, CultureInfo.InvariantCulture)));
    }

    public VirtualControllerSnapshot Scan(
        IReadOnlyDictionary<string, bool> sampledInputs,
        IReadOnlyDictionary<string, double>? sampledNumericInputs = null,
        IReadOnlyDictionary<string, string>? sampledTextInputs = null)
    {
        if (sampledTextInputs is not null)
        {
            foreach (var (name, value) in sampledTextInputs)
                if (!_program.Variables.TryGetValue(name, out var declaration) || declaration.Role != PlcVariableRole.Input
                    || !PlcTextValues.IsText(declaration.Type) || !PlcTextValues.IsValid(declaration, value))
                    throw new ArgumentException($"Invalid typed text input '{name}'.");
            foreach (var (name, value) in sampledTextInputs) _textValues[name] = value;
        }
        foreach (var variable in _program.Variables.Values.Where(item =>
                     item.Role == PlcVariableRole.Input && item.Type == PlcVariableType.Bool))
        {
            if (sampledInputs.TryGetValue(variable.Name, out var value))
            {
                _unforcedBoolInputs[variable.Name] = value;
                _values[variable.Name] = value;
            }
        }
        if (sampledNumericInputs is not null)
        {
            foreach (var variable in _program.Variables.Values.Where(item =>
                         item.Role == PlcVariableRole.Input
                         && item.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real))
            {
                if (!sampledNumericInputs.TryGetValue(variable.Name, out var value)) continue;
                _numericValues[variable.Name] = NormalizeNumeric(variable.Type, value);
            }
        }
        foreach (var force in _forces.Values.Where(item => item.Role == PlcVariableRole.Input))
            _values[force.Variable] = force.Value;
        if (State != VirtualControllerState.Running) return CreateSnapshot();

        _elements.Clear();
        _diagnostics.Clear();
        _dueTasks.Clear();
        BeginObservationAttempt();
        _callInvocations.Clear();
        _instanceTimerQuantumUsed.Clear();
        var dueTasks = _tasks
            .Select((task, index) => (Task: task, Index: index))
            .Where(item => item.Task.Kind == LadderTaskKind.Continuous
                || (_scanNumber + 1) % Math.Max(1L,
                    (long)Math.Round(item.Task.Period.TotalMilliseconds / ScanPeriod.TotalMilliseconds)) == 0)
            .OrderBy(item => item.Task.Priority)
            .ThenBy(item => item.Index)
            .Select(item => item.Task)
            .ToArray();
        var executionQueue = new LinkedList<ExecutionItem>();
        foreach (var task in dueTasks)
        {
            var frame = ++_nextFrame;
            IReadOnlyList<ObservationCallSite> path = Array.AsReadOnly(new[] { new ObservationCallSite(task.Id, task.EntryBlock, frame) });
            _dueTasks.Add(task.Id);
            _taskExecutions[task.Id]++;
            _elements[task.Id] = new LadderElementState(task.Id, true);
            var entryNetworks = _blocks[task.EntryBlock].Networks;
            for (var index = 0; index < entryNetworks.Count; index++)
                executionQueue.AddLast(new ExecutionItem(entryNetworks[index], task.EntryBlock, index, task.Id, frame, path));
            executionQueue.AddLast(new ExecutionItem(null));
        }
        var executedNetworkCount = 0;
        var executionFault = false;
        while (executionQueue.First is not null)
        {
            var executionItem = executionQueue.First.Value;
            var network = executionItem.Network;
            executionQueue.RemoveFirst();
            if (network is null) { FinishCall(executionItem.FrameId, true); continue; }
            if (_callFrames.ContainsKey(executionItem.FrameId))
                network = LadderCallBindings.MapNetwork(network, operand => ResolveCallStorage(operand, executionItem.FrameId));
            executedNetworkCount++;
            if (executedNetworkCount > 10000)
            {
                _diagnostics.Add("VC_RUNTIME_JUMP_LIMIT: scan aborted after 10000 network executions; outputs were driven safe.");
                executionQueue.Clear();
                executionFault = true;
                State = VirtualControllerState.Stopped;
                break;
            }
            _observationContext = executionItem;
            var energized = Evaluate(network.Logic);
            var actionObservation = BeginActionObservation(network, energized);
            if (network.Coil is not null)
            {
                switch (network.Coil.Mode)
                {
                    case LadderCoilMode.Assign:
                        _values[network.Coil.Variable] = energized;
                        break;
                    case LadderCoilMode.Set when energized:
                        _values[network.Coil.Variable] = true;
                        break;
                    case LadderCoilMode.Reset when energized:
                        _values[network.Coil.Variable] = false;
                        break;
                }
                _elements[network.Coil.Id] = new LadderElementState(network.Coil.Id, energized);
            }
            else if (network.Timer is not null)
            {
                var previous = _timers.GetValueOrDefault(network.Timer.Variable)
                    ?? new LadderTimerState(network.Timer.Preset, TimeSpan.Zero, false, false);
                _timers[network.Timer.Variable] = EvaluateTimer(network.Timer, previous, energized);
                _elements[network.Timer.Id] = new LadderElementState(network.Timer.Id, energized);
            }
            else if (network.TimerReset is not null)
            {
                var previous = _timers.GetValueOrDefault(network.TimerReset.Variable)
                    ?? new LadderTimerState(TimeSpan.Zero, TimeSpan.Zero, false, false);
                if (energized)
                    _timers[network.TimerReset.Variable] = previous with
                    {
                        Accumulated = TimeSpan.Zero,
                        Timing = false,
                        Done = false,
                        Input = false,
                    };
                _elements[network.TimerReset.Id] = new LadderElementState(network.TimerReset.Id, energized);
            }
            else if (network.Counter is not null)
            {
                var previous = _counters.GetValueOrDefault(network.Counter.Variable)
                    ?? new LadderCounterState(network.Counter.Preset, 0, false, false);
                var counterMemoryKey = CounterMemoryKey(network.Counter);
                var priorInput = _counterInputs.GetValueOrDefault(counterMemoryKey);
                var risingEdge = energized && !priorInput;
                var accumulated = previous.Accumulated;
                if (risingEdge)
                {
                    accumulated = network.Counter.Kind switch
                    {
                        LadderCounterKind.CountUp when accumulated < long.MaxValue => accumulated + 1,
                        LadderCounterKind.CountDown when accumulated > long.MinValue => accumulated - 1,
                        _ => accumulated,
                    };
                }
                var done = risingEdge
                    ? network.Counter.Kind == LadderCounterKind.CountUp
                        ? accumulated >= network.Counter.Preset
                        : accumulated <= 0
                    : previous.Done;
                _counterInputs[counterMemoryKey] = energized;
                _counters[network.Counter.Variable] = new LadderCounterState(
                    network.Counter.Preset,
                    accumulated,
                    energized,
                    done);
                _elements[network.Counter.Id] = new LadderElementState(network.Counter.Id, energized);
            }
            else if (network.CounterReset is not null)
            {
                var previous = _counters.GetValueOrDefault(network.CounterReset.Variable)
                    ?? new LadderCounterState(0, 0, false, false);
                if (energized)
                {
                    _counters[network.CounterReset.Variable] = previous with
                    {
                        Accumulated = 0,
                        CountInput = false,
                        Done = false,
                    };
                    ClearCounterEdges(network.CounterReset.Variable);
                }
                _elements[network.CounterReset.Id] = new LadderElementState(network.CounterReset.Id, energized);
            }
            else if (network.CounterLoad is not null)
            {
                var previous = _counters.GetValueOrDefault(network.CounterLoad.Variable)
                    ?? new LadderCounterState(network.CounterLoad.Preset, 0, false, false);
                if (energized)
                {
                    _counters[network.CounterLoad.Variable] = previous with
                    {
                        Preset = network.CounterLoad.Preset,
                        Accumulated = network.CounterLoad.Preset,
                        CountInput = false,
                        Done = false,
                    };
                    ClearCounterEdges(network.CounterLoad.Variable);
                }
                _elements[network.CounterLoad.Id] = new LadderElementState(network.CounterLoad.Id, energized);
            }
            else if (network.NumericOperation is not null)
            {
                var operation = network.NumericOperation;
                if (energized)
                {
                    if (PlcTextValues.IsText(_storageDeclarations[operation.Destination].Type))
                    {
                        if (operation.Kind == LadderNumericOperationKind.Move)
                        {
                            var consumed = PlcTextValues.Resolve(operation.SourceA, _textValues);
                            CaptureResolved(operation.SourceA, consumed);
                            _textValues[operation.Destination] = consumed;
                            if (actionObservation is not null) actionObservation.Calculated = consumed;
                        }
                        else if (PlcTextValues.TryFormat(ResolveNumeric(operation.SourceA),
                                     ResolveObservedText(operation.SourceB), out var formatted))
                        {
                            _textValues[operation.Destination] = formatted;
                            if (actionObservation is not null) actionObservation.Calculated = formatted;
                        }
                        else
                        {
                            _textValues[operation.Destination] = string.Empty;
                            _diagnostics.Add($"VC_RUNTIME_TEXT {operation.Id}: invalid template/result; destination cleared.");
                        }
                    }
                    else
                    {
                        var sourceA = ResolveNumeric(operation.SourceA);
                        var sourceB = LadderNumericOperationRules.RequiresSourceB(operation.Kind)
                            ? ResolveNumeric(operation.SourceB)
                            : 0;
                        var sourceC = LadderNumericOperationRules.RequiresSourceC(operation.Kind)
                            ? ResolveNumeric(operation.SourceC)
                            : 0;
                        string? numericFault = null;
                        double? result = operation.Kind switch
                        {
                            LadderNumericOperationKind.Move => sourceA,
                            LadderNumericOperationKind.Add => sourceA + sourceB,
                            LadderNumericOperationKind.Subtract => sourceA - sourceB,
                            LadderNumericOperationKind.Multiply => sourceA * sourceB,
                            LadderNumericOperationKind.Divide when sourceB != 0 => sourceA / sourceB,
                            LadderNumericOperationKind.Divide => Fault("VC_RUNTIME_DIV_ZERO", "divisor resolved to zero"),
                            LadderNumericOperationKind.Modulo when sourceB != 0 => sourceA % sourceB,
                            LadderNumericOperationKind.Modulo => Fault("VC_RUNTIME_MOD_ZERO", "divisor resolved to zero"),
                            LadderNumericOperationKind.Absolute => Math.Abs(sourceA),
                            LadderNumericOperationKind.Negate => -sourceA,
                            LadderNumericOperationKind.SquareRoot when sourceA >= 0 => Math.Sqrt(sourceA),
                            LadderNumericOperationKind.SquareRoot => Fault("VC_RUNTIME_DOMAIN", "square-root source resolved below zero"),
                            LadderNumericOperationKind.Exponentiate when LadderNumericOperationRules.IsDomainValid(operation.Kind, sourceA, sourceB) => Math.Pow(sourceA, sourceB),
                            LadderNumericOperationKind.Exponentiate => Fault("VC_RUNTIME_DOMAIN", "power operands are outside the real-number domain"),
                            LadderNumericOperationKind.NaturalLog when sourceA > 0 => Math.Log(sourceA),
                            LadderNumericOperationKind.NaturalLog => Fault("VC_RUNTIME_DOMAIN", "natural-log source resolved at or below zero"),
                            LadderNumericOperationKind.Sine => Math.Sin(sourceA),
                            LadderNumericOperationKind.Cosine => Math.Cos(sourceA),
                            LadderNumericOperationKind.Tangent => Math.Tan(sourceA),
                            LadderNumericOperationKind.ArcSine when sourceA is >= -1 and <= 1 => Math.Asin(sourceA),
                            LadderNumericOperationKind.ArcSine => Fault("VC_RUNTIME_DOMAIN", "arcsine source resolved outside -1 through 1"),
                            LadderNumericOperationKind.ArcCosine when sourceA is >= -1 and <= 1 => Math.Acos(sourceA),
                            LadderNumericOperationKind.ArcCosine => Fault("VC_RUNTIME_DOMAIN", "arccosine source resolved outside -1 through 1"),
                            LadderNumericOperationKind.ArcTangent => Math.Atan(sourceA),
                            LadderNumericOperationKind.Truncate => Math.Truncate(sourceA),
                            LadderNumericOperationKind.Normalize when sourceA < sourceC => (sourceB - sourceA) / (sourceC - sourceA),
                            LadderNumericOperationKind.Normalize => Fault("VC_RUNTIME_RANGE", "NORM_X/CPT normalization requires MIN below MAX"),
                            LadderNumericOperationKind.Scale when sourceA < sourceC => sourceB * (sourceC - sourceA) + sourceA,
                            LadderNumericOperationKind.Scale => Fault("VC_RUNTIME_RANGE", "SCALE_X/CPT scaling requires MIN below MAX"),
                            LadderNumericOperationKind.Convert => sourceA,
                            LadderNumericOperationKind.Round => Math.Round(sourceA, MidpointRounding.ToEven),
                            LadderNumericOperationKind.Ceiling => Math.Ceiling(sourceA),
                            LadderNumericOperationKind.Floor => Math.Floor(sourceA),
                            _ => null,
                        };
                        if (actionObservation is not null) actionObservation.Calculated = result;
                        if (numericFault is not null)
                            _diagnostics.Add($"{numericFault} {operation.Id}: '{operation.Destination}' was not written.");
                        else if (result is null)
                            _diagnostics.Add($"VC_RUNTIME_NUMERIC {operation.Id}: unsupported numeric operation; '{operation.Destination}' was not written.");
                        else if (double.IsNaN(result.Value) || double.IsInfinity(result.Value))
                            _diagnostics.Add($"VC_RUNTIME_NUMERIC {operation.Id}: result is not finite; '{operation.Destination}' was not written.");
                        else
                            StoreNumeric(operation.Destination, result.Value);

                        double? Fault(string code, string detail)
                        {
                            numericFault = $"{code}: {detail};";
                            return null;
                        }
                    }
                }
                _elements[operation.Id] = new LadderElementState(operation.Id, energized);
            }
            else if (network.Call is not null)
            {
                if (energized)
                {
                    var targetNetworks = _blocks[network.Call.TargetBlock].Networks;
                    var frame = ++_nextFrame;
                    if (actionObservation is not null && network.Call.SemanticMode == LadderSemanticMode.Parameterized) actionObservation.CalledFrameId = frame;
                    if (!TryEnterCall(network.Call, executionItem, frame, out var callFailure))
                    {
                        _diagnostics.Add(callFailure); executionFault = true; State = VirtualControllerState.Stopped;
                        if (network.Call.SemanticMode == LadderSemanticMode.Parameterized)
                            _callInvocations.Add(new(frame, network.Call.Id, network.Call.TargetBlock, executionItem.TaskId,
                                network.Call.InstanceId, LadderCallCompletion.Aborted, [], callFailure));
                        executionQueue.Clear();
                    }
                    else
                    {
                    var path = Array.AsReadOnly((executionItem.Path ?? []).Append(new ObservationCallSite(network.Call.Id, network.Call.TargetBlock, frame,
                        network.Call.SemanticMode == LadderSemanticMode.Parameterized && network.Call.InstanceId.Length > 0 ? network.Call.InstanceId : null)).ToArray());
                    executionQueue.AddFirst(new ExecutionItem(null, FrameId: frame));
                    for (var index = targetNetworks.Count - 1; index >= 0; index--)
                        executionQueue.AddFirst(new ExecutionItem(targetNetworks[index], network.Call.TargetBlock, index, executionItem.TaskId, frame, path));
                    }
                }
                _elements[network.Call.Id] = new LadderElementState(network.Call.Id, energized);
            }
            else if (network.Return is not null)
            {
                if (energized)
                {
                    while (executionQueue.First is not null)
                    {
                        var item = executionQueue.First.Value;
                        executionQueue.RemoveFirst();
                        if (item.Network is null) { FinishCall(item.FrameId, true); break; }
                    }
                }
                _elements[network.Return.Id] = new LadderElementState(network.Return.Id, energized);
            }
            else if (network.Jump is not null)
            {
                if (energized)
                {
                    while (executionQueue.First is not null && executionQueue.First.Value.Network is not null)
                        executionQueue.RemoveFirst();
                    var targetIndex = _blockLabels[executionItem.BlockId][network.Jump.TargetLabel];
                    var targetNetworks = _blocks[executionItem.BlockId].Networks;
                    for (var index = targetNetworks.Count - 1; index >= targetIndex; index--)
                        executionQueue.AddFirst(new ExecutionItem(targetNetworks[index], executionItem.BlockId, index, executionItem.TaskId, executionItem.FrameId, executionItem.Path));
                }
                _elements[network.Jump.Id] = new LadderElementState(network.Jump.Id, energized);
            }
            else if (network.LabelInstruction is not null)
            {
                _elements[network.LabelInstruction.Id] = new LadderElementState(
                    network.LabelInstruction.Id, energized);
            }
            _elements[network.Id] = new LadderElementState(network.Id, energized);
            EndActionObservation(actionObservation, network, energized);
            ObserveNetwork(network, energized);
            _completedNetworks++;
        }
        if (executionFault)
            foreach (var frameId in _callFrames.Keys.ToArray()) FinishCall(frameId, false, _diagnostics.LastOrDefault() ?? "Scan aborted");
        _programEndValues = new VirtualControllerSignalImage(_values, _numericValues, _textValues);
        if (executionFault)
        {
            foreach (var variable in _program.Variables.Values.Where(item =>
                         item.Role == PlcVariableRole.Output && item.Type == PlcVariableType.Bool))
                _values[variable.Name] = false;
            foreach (var variable in _program.Variables.Values.Where(item =>
                         item.Role == PlcVariableRole.Output
                         && item.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real))
                _numericValues[variable.Name] = 0.0;
        }
        else
        {
            foreach (var force in _forces.Values.Where(item => item.Role == PlcVariableRole.Output))
                _values[force.Variable] = force.Value;
        }
        _scanNumber++;
        _simulatedTime += ScanPeriod;
        _completion = executionFault ? ControllerCompletion.Aborted : ControllerCompletion.Completed;
        _observations = AttachCallObservations(_collector?.Freeze(executionFault));
        _collector = null;
        return CreateSnapshot();
    }

    private void ClearCounterEdges(string variable)
    {
        foreach (var key in _counterEdgeStorage.Where(p => p.Value == variable).Select(p => p.Key)) _counterInputs[key] = false;
        foreach (var instruction in _blocks.Values
                     .SelectMany(block => block.Networks)
                     .Where(network => network.Counter?.Variable == variable)
                     .Select(network => network.Counter!))
            _counterInputs[new("", instruction.Id)] = false;
    }

    private LadderTimerState EvaluateTimer(LadderTimer instruction, LadderTimerState previous, bool input)
    {
        TimeSpan Advance(TimeSpan current)
        {
            var next = current + TimerQuantum(instruction.Variable);
            return next > instruction.Preset ? instruction.Preset : next;
        }

        return instruction.Kind switch
        {
            LadderTimerKind.OnDelay => EvaluateOnDelay(),
            LadderTimerKind.OffDelay => EvaluateOffDelay(),
            LadderTimerKind.Pulse => EvaluatePulse(),
            LadderTimerKind.RetentiveOnDelay => EvaluateRetentiveOnDelay(),
            _ => throw new InvalidOperationException($"Unsupported timer kind {instruction.Kind}."),
        };

        LadderTimerState EvaluateOnDelay()
        {
            var accumulated = input ? Advance(previous.Accumulated) : TimeSpan.Zero;
            var done = input && accumulated >= instruction.Preset;
            return new(instruction.Preset, accumulated, input && !done, done, input);
        }

        LadderTimerState EvaluateRetentiveOnDelay()
        {
            var accumulated = input && !previous.Done
                ? Advance(previous.Accumulated)
                : previous.Accumulated;
            var done = previous.Done || accumulated >= instruction.Preset;
            return new(instruction.Preset, accumulated, input && !done, done, input);
        }

        LadderTimerState EvaluateOffDelay()
        {
            if (input)
                return new(instruction.Preset, TimeSpan.Zero, false, true, true);
            if (!previous.Input && !previous.Done && !previous.Timing)
                return new(instruction.Preset, TimeSpan.Zero, false, false, false);
            var accumulated = Advance(previous.Accumulated);
            var done = accumulated < instruction.Preset;
            return new(instruction.Preset, accumulated, done, done, false);
        }

        LadderTimerState EvaluatePulse()
        {
            if (previous.Timing)
            {
                var accumulated = Advance(previous.Accumulated);
                var active = accumulated < instruction.Preset;
                return new(instruction.Preset, accumulated, active, active, input);
            }
            if (previous.Done)
                return new(instruction.Preset, previous.Accumulated, false, false, input);
            if (input && !previous.Input)
            {
                var accumulated = Advance(TimeSpan.Zero);
                return new(instruction.Preset, accumulated,
                    accumulated < instruction.Preset, true, true);
            }
            return new(instruction.Preset, TimeSpan.Zero, false, false, input);
        }
    }

    private bool Evaluate(LadderNode node, bool incomingContinuity = true)
    {
        var nodeObservation = BeginNodeObservation(node);
        bool energized;
        switch (node.Kind)
        {
            case LadderNodeKind.Contact:
                var value = ResolveBool(node.Variable);
                if (node.EdgeMode == LadderEdgeMode.None)
                {
                    energized = node.NormallyClosed ? !value : value;
                }
                else if (!_edgeInputs.TryGetValue(InstructionMemoryKey(node.Id), out var previous))
                {
                    // First observation after Reset/Stop establishes the baseline
                    // without creating a startup pulse.
                    _edgeInputs[InstructionMemoryKey(node.Id)] = value;
                    energized = false;
                }
                else
                {
                    energized = node.EdgeMode == LadderEdgeMode.Rising
                        ? value && !previous
                        : !value && previous;
                    _edgeInputs[InstructionMemoryKey(node.Id)] = value;
                }
                break;
            case LadderNodeKind.Compare:
                if (PlcTextValues.IsOperand(node.Variable, _storageDeclarations) || PlcTextValues.IsOperand(node.RightOperand, _storageDeclarations))
                {
                    var textLeft = PlcTextValues.Resolve(node.Variable, _textValues);
                    var textRight = PlcTextValues.Resolve(node.RightOperand, _textValues);
                    CaptureResolved(node.Variable, textLeft);
                    CaptureResolved(node.RightOperand, textRight);
                    if (node.CompareOperator == LadderCompareOperator.ContainsCode
                        && (textRight.Length == 0 || textRight.Any(character => !char.IsLetterOrDigit(character))))
                        _diagnostics.Add($"VC_RUNTIME_TEXT {node.Id}: CODE MATCH selector must be a nonempty alphanumeric code.");
                    energized = node.CompareOperator switch
                    {
                        LadderCompareOperator.Equal => textLeft == textRight,
                        LadderCompareOperator.NotEqual => textLeft != textRight,
                        LadderCompareOperator.ContainsCode => PlcTextValues.ContainsCode(textLeft, textRight),
                        _ => false,
                    };
                    break;
                }
                var left = ResolveNumeric(node.Variable);
                var right = ResolveNumeric(node.RightOperand);
                energized = node.CompareOperator switch
                {
                    LadderCompareOperator.Equal => left == right,
                    LadderCompareOperator.NotEqual => left != right,
                    LadderCompareOperator.GreaterThan => left > right,
                    LadderCompareOperator.GreaterOrEqual => left >= right,
                    LadderCompareOperator.LessThan => left < right,
                    LadderCompareOperator.LessOrEqual => left <= right,
                    _ => false,
                };
                break;
            case LadderNodeKind.Series:
                energized = true;
                foreach (var child in node.Children ?? [])
                {
                    var childState = Evaluate(child, incomingContinuity && energized);
                    energized = energized && childState;
                }
                break;
            case LadderNodeKind.Parallel:
                energized = false;
                foreach (var child in node.Children ?? [])
                {
                    var childState = Evaluate(child, incomingContinuity);
                    energized = energized || childState;
                }
                break;
            default:
                throw new InvalidOperationException($"Unsupported node kind {node.Kind}.");
        }
        _elements[node.Id] = new LadderElementState(node.Id, energized);
        EndNodeObservation(nodeObservation, node, energized, incomingContinuity);
        return energized;
    }

    private bool ResolveBool(string name)
    {
        var result = ResolveBoolRaw(name); CaptureResolved(name, result); return result;
    }
    private bool ResolveBoolRaw(string name)
    {
        if (_values.TryGetValue(name, out var value)) return value;
        var dot = name.LastIndexOf('.');
        if (dot <= 0) return false;
        var instance = name[..dot];
        var member = name[(dot + 1)..];
        if (_timers.TryGetValue(instance, out var timer)) return member switch
        {
            "DN" or "Q" => timer.Done,
            "TT" => timer.Timing,
            _ => false,
        };
        if (_counters.TryGetValue(instance, out var counter)) return member switch
        {
            "DN" or "Q" => counter.Done,
            _ => false,
        };
        return false;
    }

    private double ResolveNumeric(string operand)
    {
        var result = ResolveNumericRaw(operand); CaptureResolved(operand, result); return result;
    }
    private double ResolveNumericRaw(string operand)
    {
        if (double.TryParse(operand, NumberStyles.Float, CultureInfo.InvariantCulture, out var literal))
            return literal;
        if (_numericValues.TryGetValue(operand, out var value)) return value;
        var dot = operand.LastIndexOf('.');
        if (dot <= 0) return 0;
        var instance = operand[..dot];
        var member = operand[(dot + 1)..];
        if (_counters.TryGetValue(instance, out var counter)) return member switch
        {
            "ACC" or "CV" => counter.Accumulated,
            "PRE" or "PV" => counter.Preset,
            _ => 0,
        };
        if (_timers.TryGetValue(instance, out var timer)) return member switch
        {
            "ET" => timer.Accumulated.TotalMilliseconds,
            "PT" => timer.Preset.TotalMilliseconds,
            _ => 0,
        };
        return 0;
    }

    private void StoreNumeric(string destination, double value)
    {
        var variable = _storageDeclarations[destination];
        _numericValues[destination] = NormalizeNumeric(variable.Type, value);
    }

    private static double NormalizeNumeric(PlcVariableType type, double value) => type switch
    {
        PlcVariableType.Int => Math.Clamp(Math.Round(value, MidpointRounding.ToEven), short.MinValue, short.MaxValue),
        PlcVariableType.DInt => Math.Clamp(Math.Round(value, MidpointRounding.ToEven), int.MinValue, int.MaxValue),
        _ => value,
    };

    // Compiled variables already contain expanded aggregate leaves. Select only
    // Input-role primitives from the accepted snapshot, after force application.
    internal VirtualControllerSignalImage CaptureEffectiveInputs(VirtualControllerSnapshot snapshot)
    {
        var inputs = _program.Variables.Values.Where(v => v.Role == PlcVariableRole.Input).ToArray();
        return new(
            inputs.Where(v => v.Type == PlcVariableType.Bool).ToDictionary(v => v.Name, v => snapshot.Variables[v.Name], StringComparer.Ordinal),
            inputs.Where(v => v.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real).ToDictionary(v => v.Name, v => snapshot.NumericVariables[v.Name], StringComparer.Ordinal),
            inputs.Where(v => PlcTextValues.IsText(v.Type)).ToDictionary(v => v.Name, v => snapshot.TextVariables[v.Name], StringComparer.Ordinal));
    }

    private VirtualControllerSnapshot CreateSnapshot()
    {
        var values = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(_values, StringComparer.Ordinal));
        var numericValues = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(_numericValues, StringComparer.Ordinal));
        var outputs = new ReadOnlyDictionary<string, bool>(_program.Variables.Values
            .Where(item => item.Role == PlcVariableRole.Output && item.Type == PlcVariableType.Bool)
            .ToDictionary(item => item.Name, item => _values[item.Name], StringComparer.Ordinal));
        var numericOutputs = new ReadOnlyDictionary<string, double>(_program.Variables.Values
            .Where(item => item.Role == PlcVariableRole.Output
                && item.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
            .ToDictionary(item => item.Name, item => _numericValues[item.Name], StringComparer.Ordinal));
        var elements = new ReadOnlyDictionary<string, LadderElementState>(
            new Dictionary<string, LadderElementState>(_elements, StringComparer.Ordinal));
        var timers = new ReadOnlyDictionary<string, LadderTimerState>(
            new Dictionary<string, LadderTimerState>(_timers, StringComparer.Ordinal));
        var counters = new ReadOnlyDictionary<string, LadderCounterState>(
            new Dictionary<string, LadderCounterState>(_counters, StringComparer.Ordinal));
        var taskStates = new ReadOnlyDictionary<string, LadderTaskState>(_tasks.ToDictionary(
            task => task.Id,
            task => new LadderTaskState(
                task.Id, task.Name, task.Kind, task.Period, task.Priority, task.EntryBlock,
                _dueTasks.Contains(task.Id), _taskExecutions.GetValueOrDefault(task.Id)),
            StringComparer.Ordinal));
        var forces = new ReadOnlyDictionary<string, LadderForceState>(
            new Dictionary<string, LadderForceState>(_forces, StringComparer.Ordinal));
        return new VirtualControllerSnapshot(_scanNumber, _simulatedTime, State, values, numericValues, outputs, numericOutputs, timers, counters, taskStates, forces, _diagnostics.ToArray(), elements)
        {
            Aggregates = PlcAggregates.Reconstruct(_program.Source.Variables, values, numericValues),
            Observations = _observations,
            Completion = _completion,
            ProgramEndValues = _programEndValues,
            ExecutedNetworkCount = _completedNetworks,
            LastFullyObservedInstructionId = _lastFullyObservedId,
            LastFullyObservedOrdinal = _progressOrdinal,
            CallInvocations = Array.AsReadOnly(_callInvocations.ToArray()),
            TextVariables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(_textValues, StringComparer.Ordinal)),
            TextOutputs = new ReadOnlyDictionary<string, string>(_program.Variables.Values
                .Where(item => item.Role == PlcVariableRole.Output && PlcTextValues.IsText(item.Type))
                .ToDictionary(item => item.Name, item => _textValues[item.Name], StringComparer.Ordinal))
        };
    }
}

