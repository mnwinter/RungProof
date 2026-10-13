using System;
using System.Collections.Generic;

namespace RungProof.Next.VirtualController;

public enum PlcVariableType
{
    Bool,
    Int,
    DInt,
    Real,
    Timer,
    Counter,
    Struct,
    Array,
    String,
    Enum,
}

public enum PlcVariableRole
{
    Input,
    Memory,
    Output,
}

public sealed record PlcVariable(
    string Name,
    PlcVariableType Type,
    PlcVariableRole Role,
    object InitialValue,
    string Binding = "",
    PlcAggregateSchema? Aggregate = null,
    IReadOnlyList<string>? EnumMembers = null,
    string Id = ""
);

public enum LadderNodeKind
{
    Contact,
    Compare,
    Series,
    Parallel,
}

public enum LadderCompareOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterOrEqual,
    LessThan,
    LessOrEqual,
    ContainsCode,
}

public enum LadderEdgeMode
{
    None,
    Rising,
    Falling,
}

/// <summary>
/// Renderer-neutral Ladder Diagram intermediate representation. A contact
/// references one symbolic BOOL. Series children are evaluated left-to-right;
/// parallel children are branches evaluated top-to-bottom.
/// </summary>
public sealed record LadderNode(
    string Id,
    LadderNodeKind Kind,
    string Variable = "",
    bool NormallyClosed = false,
    IReadOnlyList<LadderNode>? Children = null,
    LadderCompareOperator CompareOperator = LadderCompareOperator.Equal,
    string RightOperand = "",
    LadderEdgeMode EdgeMode = LadderEdgeMode.None
);

public enum LadderCoilMode
{
    Assign,
    Set,
    Reset,
}

public sealed record LadderCoil(string Id, string Variable, LadderCoilMode Mode = LadderCoilMode.Assign);
public enum LadderTimerKind
{
    OnDelay,
    OffDelay,
    Pulse,
    RetentiveOnDelay,
}

public sealed record LadderTimer(
    string Id,
    string Variable,
    TimeSpan Preset,
    LadderTimerKind Kind = LadderTimerKind.OnDelay);
public sealed record LadderTimerReset(string Id, string Variable);
public enum LadderCounterKind
{
    CountUp,
    CountDown,
}

public sealed record LadderCounter(
    string Id,
    string Variable,
    long Preset,
    LadderCounterKind Kind = LadderCounterKind.CountUp);
public sealed record LadderCounterReset(string Id, string Variable);
public sealed record LadderCounterLoad(string Id, string Variable, long Preset);

public enum LadderNumericOperationKind
{
    Move,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Absolute,
    Negate,
    SquareRoot,
    Exponentiate,
    NaturalLog,
    Sine,
    Cosine,
    Tangent,
    ArcSine,
    ArcCosine,
    ArcTangent,
    Truncate,
    Normalize,
    Scale,
    Convert,
    Round,
    Ceiling,
    Floor,
    FormatText,
}

public static class LadderNumericOperationRules
{
    public static bool RequiresSourceB(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Add or
        LadderNumericOperationKind.Subtract or
        LadderNumericOperationKind.Multiply or
        LadderNumericOperationKind.Divide or
        LadderNumericOperationKind.Modulo or
        LadderNumericOperationKind.Exponentiate or
        LadderNumericOperationKind.Normalize or
        LadderNumericOperationKind.Scale or
        LadderNumericOperationKind.FormatText;

    public static bool RequiresSourceC(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Normalize or
        LadderNumericOperationKind.Scale;

    public static bool IsDomainValid(LadderNumericOperationKind kind, double sourceA, double sourceB = 0, double sourceC = 0) => kind switch
    {
        LadderNumericOperationKind.SquareRoot => sourceA >= 0,
        LadderNumericOperationKind.NaturalLog => sourceA > 0,
        LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine => sourceA is >= -1 and <= 1,
        LadderNumericOperationKind.Exponentiate => !(sourceA < 0 && sourceB != Math.Truncate(sourceB))
            && !(sourceA == 0 && sourceB < 0),
        LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale => sourceA < sourceC,
        _ => true,
    };
}

public sealed record LadderNumericOperation(
    string Id,
    LadderNumericOperationKind Kind,
    string SourceA,
    string SourceB,
    string Destination,
    string SourceC = "0");

public enum LadderSemanticMode { Legacy, Parameterized }
public enum LadderActualKind { Symbol, Literal, Discard }
public sealed record LadderCallBinding(string ParameterId, LadderActualKind Kind,
    string Symbol = "", object? Literal = null, PlcVariableType? LiteralType = null,
    IReadOnlyList<string>? EnumMembers = null);
public sealed record LadderInstance(string Id, string Name, string BlockId);
public sealed record LadderCall(string Id, string TargetBlock,
    LadderSemanticMode SemanticMode = LadderSemanticMode.Legacy,
    IReadOnlyList<LadderCallBinding>? Bindings = null, string InstanceId = "");
public sealed record LadderReturn(string Id);
public sealed record LadderJump(string Id, string TargetLabel);
public sealed record LadderLabel(string Id, string Name);

public enum LadderBlockType
{
    OrganizationBlock,
    FunctionBlock,
    Function,
    DataBlock,
}

public enum LadderInterfaceSection
{
    Input,
    Output,
    InOut,
    Static,
    Temp,
}

public sealed record LadderInterfaceParameter(
    string Name,
    PlcVariableType Type,
    LadderInterfaceSection Section,
    object InitialValue,
    string DataType = "", string Id = "", IReadOnlyList<string>? EnumMembers = null);

public sealed record LadderNetwork(
    string Id,
    string Label,
    LadderNode Logic,
    LadderCoil? Coil = null,
    LadderTimer? Timer = null,
    LadderCounter? Counter = null,
    LadderCounterReset? CounterReset = null,
    LadderNumericOperation? NumericOperation = null,
    LadderCall? Call = null,
    LadderReturn? Return = null,
    LadderCounterLoad? CounterLoad = null,
    LadderTimerReset? TimerReset = null,
    LadderJump? Jump = null,
    LadderLabel? LabelInstruction = null
);

public sealed record LadderBlock(
    string Id,
    string Name,
    IReadOnlyList<LadderNetwork> Networks,
    LadderBlockType BlockType = LadderBlockType.Function,
    IReadOnlyList<LadderInterfaceParameter>? Interface = null,
    LadderSemanticMode SemanticMode = LadderSemanticMode.Legacy);

public enum LadderTaskKind
{
    Continuous,
    Periodic,
}

public sealed record LadderTask(
    string Id,
    string Name,
    LadderTaskKind Kind,
    TimeSpan Period,
    int Priority,
    string EntryBlock);

public sealed record LadderProgram(
    int SchemaVersion,
    string Id,
    string Name,
    string Language,
    TimeSpan ScanPeriod,
    IReadOnlyList<PlcVariable> Variables,
    IReadOnlyList<LadderNetwork> Networks,
    IReadOnlyList<LadderBlock>? Blocks = null,
    string EntryBlock = "",
    IReadOnlyList<LadderTask>? Tasks = null,
    IReadOnlyList<string>? WatchVariables = null,
    IReadOnlyList<LadderInstance>? Instances = null
);

public sealed record LadderValidationIssue(string Code, string Path, string Message);
