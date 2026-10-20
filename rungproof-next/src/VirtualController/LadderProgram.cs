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
    Time,
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
    string Id = "",
    string TagTable = "Default tag table",
    string Address = "",
    string Comment = ""
);

/// <summary>
/// A PLC-wide symbolic constant declared in a tag table. Constants are editor
/// metadata only; they are never mutable runtime tags or PLC I/O points.
/// </summary>
public sealed record PlcUserConstant(
    string Id,
    string Name,
    PlcVariableType Type,
    string Value,
    string TagTable,
    string Comment = "");

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
    LadderEdgeMode EdgeMode = LadderEdgeMode.None,
    LadderComparisonValueType ComparisonValueType = LadderComparisonValueType.Unspecified
)
{
    /// <summary>Declared BOOL storage holding the previous signal state for TIA P/N contacts.</summary>
    public string? EdgeMemoryOperand { get; init; }
    /// <summary>Editor-authored branch leg that has not yet been connected to a downstream join.</summary>
    public bool OpenEnd { get; init; }
}

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

/// <summary>
/// Symbolic identity of an IEC instruction's assigned TIA instance data
/// block. The offline controller models the block association and state, not
/// the vendor-specific byte layout of an S7 DB.
/// </summary>
public sealed record LadderInstanceDataBlock(int Number, string Name);

public sealed record LadderTimer(
    string Id,
    string Variable,
    TimeSpan Preset,
    LadderTimerKind Kind = LadderTimerKind.OnDelay,
    LadderInstanceDataBlock? InstanceDataBlock = null,
    bool PresetAssigned = true,
    string? PresetOperand = null,
    string? ResetOperand = null);
public sealed record LadderTimerReset(string Id, string Variable, LadderInstanceDataBlock? InstanceDataBlock = null);
public enum LadderCounterKind
{
    CountUp,
    CountDown,
}

/// <summary>IEC counter CV/PV integer type shown by the TIA LAD block selector.
/// ULInt is capped at Int64.MaxValue by the offline simulator's current storage model.</summary>
public enum LadderCounterValueType
{
    Unspecified,
    Int,
    SInt,
    DInt,
    USInt,
    UInt,
    UDInt,
    LInt,
    ULInt,
}

public static class LadderCounterValueTypePresentation
{
    /// <summary>Label used by the TIA counter type field, including its untyped state.</summary>
    public static string TiaLabel(LadderCounterValueType valueType) => valueType switch
    {
        LadderCounterValueType.Unspecified => "???",
        _ when Enum.IsDefined(valueType) => valueType.ToString(),
        _ => "???",
    };
}

/// <summary>
/// Data type selector for LAD compare instructions. Numeric values 0-8 match
/// the legacy counter-type enum so existing project data keeps its meaning.
/// </summary>
public enum LadderComparisonValueType
{
    Unspecified = 0,
    Int = 1,
    SInt = 2,
    DInt = 3,
    USInt = 4,
    UInt = 5,
    UDInt = 6,
    LInt = 7,
    ULInt = 8,
    Real = 9,
}

public static class LadderComparisonValueTypePresentation
{
    public static readonly LadderComparisonValueType[] TiaSelectableTypes =
    [
        LadderComparisonValueType.Unspecified,
        LadderComparisonValueType.Int,
        LadderComparisonValueType.DInt,
        LadderComparisonValueType.Real,
    ];

    public static string TiaLabel(LadderComparisonValueType valueType) => valueType switch
    {
        LadderComparisonValueType.Unspecified => "???",
        _ when Enum.IsDefined(valueType) => valueType.ToString(),
        _ => "???",
    };

    public static LadderComparisonValueType FromTiaSelector(int selectedIndex) =>
        selectedIndex >= 0 && selectedIndex < TiaSelectableTypes.Length
            ? TiaSelectableTypes[selectedIndex]
            : LadderComparisonValueType.Unspecified;

    public static int TiaSelectorIndex(LadderComparisonValueType valueType) =>
        Array.IndexOf(TiaSelectableTypes, valueType);
}

public static class LadderCounterValueTypeRange
{
    // The virtual controller stores CV as Int64. ULInt therefore shares the
    // simulator's Int64 ceiling rather than pretending to model UInt64 values.
    public static long Minimum(LadderCounterValueType type) => type switch
    {
        LadderCounterValueType.SInt => sbyte.MinValue,
        LadderCounterValueType.Int => short.MinValue,
        LadderCounterValueType.DInt => int.MinValue,
        LadderCounterValueType.USInt or LadderCounterValueType.UInt or LadderCounterValueType.UDInt or LadderCounterValueType.ULInt => 0,
        LadderCounterValueType.LInt => long.MinValue,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static long Maximum(LadderCounterValueType type) => type switch
    {
        LadderCounterValueType.SInt => sbyte.MaxValue,
        LadderCounterValueType.Int => short.MaxValue,
        LadderCounterValueType.DInt => int.MaxValue,
        LadderCounterValueType.USInt => byte.MaxValue,
        LadderCounterValueType.UInt => ushort.MaxValue,
        LadderCounterValueType.UDInt => uint.MaxValue,
        LadderCounterValueType.LInt or LadderCounterValueType.ULInt => long.MaxValue,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}

public sealed record LadderCounter(
    string Id,
    string Variable,
    long Preset,
    LadderCounterKind Kind = LadderCounterKind.CountUp,
    LadderCounterValueType ValueType = LadderCounterValueType.Int,
    LadderInstanceDataBlock? InstanceDataBlock = null,
    string LoadInput = "FALSE",
    string ResetInput = "FALSE");
public sealed record LadderCounterReset(string Id, string Variable, LadderInstanceDataBlock? InstanceDataBlock = null);
public sealed record LadderCounterLoad(string Id, string Variable, long Preset,
    LadderCounterValueType ValueType = LadderCounterValueType.Int,
    LadderInstanceDataBlock? InstanceDataBlock = null);

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

/// <summary>Supported S7-1500 numeric instruction type choices for the
/// currently modeled INT, DINT, and REAL value domain. Auto follows the
/// connected destination type; other Siemens types remain unsupported.</summary>
public enum LadderNumericDataType
{
    Unspecified,
    Int,
    DInt,
    Real,
    Auto,
}

public static class LadderNumericDataTypePresentation
{
    public static readonly LadderNumericDataType[] TiaSelectableTypes =
    [
        LadderNumericDataType.Unspecified,
        LadderNumericDataType.Int,
        LadderNumericDataType.DInt,
        LadderNumericDataType.Real,
        LadderNumericDataType.Auto,
    ];

    public static string TiaLabel(LadderNumericDataType valueType, LadderNumericDataType inferred = LadderNumericDataType.Unspecified) => valueType switch
    {
        LadderNumericDataType.Unspecified => "???",
        LadderNumericDataType.Auto when inferred != LadderNumericDataType.Unspecified => $"Auto ({inferred})",
        _ when Enum.IsDefined(valueType) => valueType.ToString(),
        _ => "???",
    };

    public static LadderNumericDataType FromTiaSelector(int selectedIndex) =>
        selectedIndex >= 0 && selectedIndex < TiaSelectableTypes.Length
            ? TiaSelectableTypes[selectedIndex]
            : LadderNumericDataType.Unspecified;

    public static int TiaSelectorIndex(LadderNumericDataType valueType) =>
        Array.IndexOf(TiaSelectableTypes, valueType);

    // MOD is integer-only in Siemens LAD. The simulator currently models INT
    // and DINT exactly; other Siemens integer widths remain an explicit
    // limitation until the PLC value model can represent them without loss.
    private static readonly LadderNumericDataType[] ModuloTypes =
    [
        LadderNumericDataType.Unspecified,
        LadderNumericDataType.Int,
        LadderNumericDataType.DInt,
        LadderNumericDataType.Auto,
    ];

    // Siemens ABS/NEG accept signed integers and floating-point operands. The
    // current virtual controller can represent INT, DINT, and REAL only, so
    // keep the dropdown to that verified subset and preserve the unresolved
    // ??? state. Auto is a local arithmetic convenience, not a Siemens type.
    private static readonly LadderNumericDataType[] UnaryMathTypes =
    [
        LadderNumericDataType.Unspecified,
        LadderNumericDataType.Int,
        LadderNumericDataType.DInt,
        LadderNumericDataType.Real,
    ];

    // Siemens SQRT accepts floating-point operands. REAL is the only one of
    // those data types represented by this offline controller; LREAL remains
    // unsupported rather than being shown as a selectable but lossy type.
    private static readonly LadderNumericDataType[] FloatingUnaryTypes =
    [
        LadderNumericDataType.Unspecified,
        LadderNumericDataType.Real,
    ];

    // Siemens EXPT keeps the base and result floating-point while the
    // exponent may be an integer or floating-point value. DataType records
    // the exponent type for this instruction; REAL is the only modeled
    // floating width, so LREAL remains unsupported.
    private static readonly LadderNumericDataType[] ExponentTypes =
    [
        LadderNumericDataType.Unspecified,
        LadderNumericDataType.Int,
        LadderNumericDataType.DInt,
        LadderNumericDataType.Real,
    ];

    // CONVERT exposes independent input and output type lists in TIA. This
    // simulator currently represents only these three numeric widths; the
    // wider Siemens conversion table remains explicitly unsupported. The
    // rounding conversions use the same supported output subset.
    private static readonly LadderNumericDataType[] ConvertTypes =
    [
        LadderNumericDataType.Unspecified,
        LadderNumericDataType.Int,
        LadderNumericDataType.DInt,
        LadderNumericDataType.Real,
    ];

    public static IReadOnlyList<LadderNumericDataType> TiaSelectableTypesFor(LadderNumericOperationKind kind) =>
        kind switch
        {
            LadderNumericOperationKind.Modulo => ModuloTypes,
            LadderNumericOperationKind.Absolute or LadderNumericOperationKind.Negate => UnaryMathTypes,
            LadderNumericOperationKind.SquareRoot or LadderNumericOperationKind.NaturalLog
                or LadderNumericOperationKind.Sine or LadderNumericOperationKind.Cosine
                or LadderNumericOperationKind.Tangent or LadderNumericOperationKind.ArcSine
                or LadderNumericOperationKind.ArcCosine or LadderNumericOperationKind.ArcTangent => FloatingUnaryTypes,
            LadderNumericOperationKind.Exponentiate => ExponentTypes,
            LadderNumericOperationKind.Truncate => ConvertTypes,
            LadderNumericOperationKind.Convert => ConvertTypes,
            _ => TiaSelectableTypes,
        };

    public static IReadOnlyList<LadderNumericDataType> TiaInputTypesFor(LadderNumericOperationKind kind) => kind switch
    {
        LadderNumericOperationKind.Normalize => ConvertTypes,
        LadderNumericOperationKind.Scale or LadderNumericOperationKind.Truncate or LadderNumericOperationKind.Round
            or LadderNumericOperationKind.Ceiling or LadderNumericOperationKind.Floor => FloatingUnaryTypes,
        _ => ConvertTypes,
    };

    public static IReadOnlyList<LadderNumericDataType> TiaOutputTypesFor(LadderNumericOperationKind kind) => kind switch
    {
        LadderNumericOperationKind.Normalize => FloatingUnaryTypes,
        LadderNumericOperationKind.Scale => ConvertTypes,
        _ => ConvertTypes,
    };

    public static int TiaSideSelectorIndex(LadderNumericOperationKind kind, bool input, LadderNumericDataType valueType)
    {
        var choices = input ? TiaInputTypesFor(kind) : TiaOutputTypesFor(kind);
        for (var i = 0; i < choices.Count; i++)
            if (choices[i] == valueType) return i;
        return -1;
    }

    public static LadderNumericDataType FromTiaSideSelector(LadderNumericOperationKind kind, bool input, int selectedIndex)
    {
        var choices = input ? TiaInputTypesFor(kind) : TiaOutputTypesFor(kind);
        return selectedIndex >= 0 && selectedIndex < choices.Count
            ? choices[selectedIndex]
            : LadderNumericDataType.Unspecified;
    }

    public static int TiaSelectorIndex(LadderNumericOperationKind kind, LadderNumericDataType valueType)
    {
        var choices = TiaSelectableTypesFor(kind);
        for (var i = 0; i < choices.Count; i++)
            if (choices[i] == valueType) return i;
        return -1;
    }

    public static LadderNumericDataType FromTiaSelector(LadderNumericOperationKind kind, int selectedIndex)
    {
        var choices = TiaSelectableTypesFor(kind);
        return selectedIndex >= 0 && selectedIndex < choices.Count
            ? choices[selectedIndex]
            : LadderNumericDataType.Unspecified;
    }

    public static bool IsSelectableType(LadderNumericOperationKind kind, LadderNumericDataType valueType) =>
        TiaSelectorIndex(kind, valueType) >= 0;

    public static LadderNumericDataType InferredTypeFor(LadderNumericOperationKind kind, PlcVariableType? valueType)
    {
        var inferred = FromVariableType(valueType);
        return IsSelectableType(kind, inferred) ? inferred : LadderNumericDataType.Unspecified;
    }

    public static LadderNumericDataType FromVariableType(PlcVariableType? valueType) => valueType switch
    {
        PlcVariableType.Int => LadderNumericDataType.Int,
        PlcVariableType.DInt => LadderNumericDataType.DInt,
        PlcVariableType.Real => LadderNumericDataType.Real,
        _ => LadderNumericDataType.Unspecified,
    };
}

public static class LadderNumericOperationRules
{
    /// <summary>
    /// TIA presents a selectable data type for these supported arithmetic
    /// boxes, SQRT, and integer MOD. The offline runtime currently implements only
    /// INT, DINT, REAL, and destination-based Auto; MOD is integer-only.
    /// </summary>
    public static bool HasTiaDataTypeSelector(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Add or
        LadderNumericOperationKind.Subtract or
        LadderNumericOperationKind.Multiply or
        LadderNumericOperationKind.Divide or
        LadderNumericOperationKind.Modulo or
        LadderNumericOperationKind.Absolute or
        LadderNumericOperationKind.Negate or
        LadderNumericOperationKind.SquareRoot or
        LadderNumericOperationKind.NaturalLog or
        LadderNumericOperationKind.Sine or
        LadderNumericOperationKind.Cosine or
        LadderNumericOperationKind.Tangent or
        LadderNumericOperationKind.ArcSine or
        LadderNumericOperationKind.ArcCosine or
        LadderNumericOperationKind.ArcTangent or
        LadderNumericOperationKind.Exponentiate or
        LadderNumericOperationKind.Truncate or
        LadderNumericOperationKind.Normalize or
        LadderNumericOperationKind.Scale or
        LadderNumericOperationKind.Convert or
        LadderNumericOperationKind.Round or
        LadderNumericOperationKind.Ceiling or
        LadderNumericOperationKind.Floor;

    public static bool HasTiaDualDataTypeSelectors(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Convert or LadderNumericOperationKind.Truncate
        or LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale
        or LadderNumericOperationKind.Round or LadderNumericOperationKind.Ceiling or LadderNumericOperationKind.Floor;

    /// <summary>
    /// Resolves the selected Siemens type side for an instruction source pin.
    /// NORM_X applies its input type to MIN, VALUE, and MAX. SCALE_X applies
    /// its REAL input type only to VALUE; MIN and MAX use the output/range
    /// type. EXPT fixes IN1 (the base) to REAL while its selector describes
    /// IN2 (the exponent). Other dual-selector instructions have one source
    /// on the input side. Remaining single-selector instructions use their
    /// ordinary data type.
    /// </summary>
    public static LadderNumericDataType SelectedDataTypeForSource(
        LadderNumericOperationKind kind,
        int sourceIndex,
        LadderNumericDataType dataType,
        LadderNumericDataType inputDataType,
        LadderNumericDataType outputDataType)
    {
        if (sourceIndex is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(sourceIndex));
        if (kind == LadderNumericOperationKind.Exponentiate && sourceIndex == 0)
            return LadderNumericDataType.Real;
        if (!HasTiaDualDataTypeSelectors(kind)) return dataType;
        return kind == LadderNumericOperationKind.Scale && sourceIndex != 1
            ? outputDataType
            : inputDataType;
    }

    public static bool IsRoundingConversion(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Round or LadderNumericOperationKind.Ceiling or LadderNumericOperationKind.Floor;

    public static string RoundingMnemonic(LadderNumericOperationKind kind) => kind switch
    {
        LadderNumericOperationKind.Round => "ROUND",
        LadderNumericOperationKind.Ceiling => "CEIL",
        LadderNumericOperationKind.Floor => "FLOOR",
        _ => throw new ArgumentException("Operation is not a rounding conversion.", nameof(kind)),
    };

    public static bool SupportsTiaDataType(LadderNumericOperationKind kind, LadderNumericDataType dataType) => kind switch
    {
        LadderNumericOperationKind.Modulo => dataType is LadderNumericDataType.Unspecified
            or LadderNumericDataType.Int or LadderNumericDataType.DInt or LadderNumericDataType.Auto,
        LadderNumericOperationKind.Add or LadderNumericOperationKind.Subtract
            or LadderNumericOperationKind.Multiply or LadderNumericOperationKind.Divide =>
            dataType is LadderNumericDataType.Unspecified or LadderNumericDataType.Int
                or LadderNumericDataType.DInt or LadderNumericDataType.Real or LadderNumericDataType.Auto,
        LadderNumericOperationKind.Absolute or LadderNumericOperationKind.Negate =>
            dataType is LadderNumericDataType.Unspecified or LadderNumericDataType.Int
                or LadderNumericDataType.DInt or LadderNumericDataType.Real,
        LadderNumericOperationKind.SquareRoot or LadderNumericOperationKind.NaturalLog
            or LadderNumericOperationKind.Sine or LadderNumericOperationKind.Cosine
            or LadderNumericOperationKind.Tangent or LadderNumericOperationKind.ArcSine
            or LadderNumericOperationKind.ArcCosine or LadderNumericOperationKind.ArcTangent =>
            dataType is LadderNumericDataType.Unspecified or LadderNumericDataType.Real,
        LadderNumericOperationKind.Exponentiate => dataType is LadderNumericDataType.Unspecified
            or LadderNumericDataType.Int or LadderNumericDataType.DInt or LadderNumericDataType.Real,
        LadderNumericOperationKind.Truncate => dataType is LadderNumericDataType.Unspecified
            or LadderNumericDataType.Int or LadderNumericDataType.DInt or LadderNumericDataType.Real,
        LadderNumericOperationKind.Convert or LadderNumericOperationKind.Round
            or LadderNumericOperationKind.Ceiling or LadderNumericOperationKind.Floor => dataType is LadderNumericDataType.Unspecified
            or LadderNumericDataType.Int or LadderNumericDataType.DInt or LadderNumericDataType.Real,
        _ => false,
    };

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

    public static bool IsFloatingUnaryMath(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.SquareRoot or
        LadderNumericOperationKind.NaturalLog or
        LadderNumericOperationKind.Sine or
        LadderNumericOperationKind.Cosine or
        LadderNumericOperationKind.Tangent or
        LadderNumericOperationKind.ArcSine or
        LadderNumericOperationKind.ArcCosine or
        LadderNumericOperationKind.ArcTangent;

    public static bool IsScientificUnaryMath(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.NaturalLog or
        LadderNumericOperationKind.Sine or
        LadderNumericOperationKind.Cosine or
        LadderNumericOperationKind.Tangent or
        LadderNumericOperationKind.ArcSine or
        LadderNumericOperationKind.ArcCosine or
        LadderNumericOperationKind.ArcTangent;

    public static bool IsDomainValid(LadderNumericOperationKind kind, double sourceA, double sourceB = 0, double sourceC = 0) => kind switch
    {
        LadderNumericOperationKind.SquareRoot => sourceA >= 0,
        LadderNumericOperationKind.NaturalLog => double.IsFinite(sourceA) && sourceA >= 0,
        LadderNumericOperationKind.Sine or LadderNumericOperationKind.Cosine
            or LadderNumericOperationKind.Tangent or LadderNumericOperationKind.ArcTangent => double.IsFinite(sourceA),
        LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine =>
            double.IsFinite(sourceA) && sourceA is >= -1 and <= 1,
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
    string SourceC = "0")
{
    public LadderNumericDataType DataType { get; init; } = LadderNumericDataType.Unspecified;
    public LadderNumericDataType InputDataType { get; init; } = LadderNumericDataType.Unspecified;
    public LadderNumericDataType OutputDataType { get; init; } = LadderNumericDataType.Unspecified;
}

public enum LadderSemanticMode { Legacy, Parameterized }
public enum LadderActualKind { Symbol, Literal, Discard }
public sealed record LadderCallBinding(string ParameterId, LadderActualKind Kind,
    string Symbol = "", object? Literal = null, PlcVariableType? LiteralType = null,
    IReadOnlyList<string>? EnumMembers = null);
public sealed record LadderInstance(string Id, string Name, string BlockId,
    LadderInstanceDataBlock? InstanceDataBlock = null);
public sealed record LadderCall(string Id, string TargetBlock,
    LadderSemanticMode SemanticMode = LadderSemanticMode.Legacy,
    IReadOnlyList<LadderCallBinding>? Bindings = null, string InstanceId = "");
public enum LadderReturnValueKind { Rlo, True, False, Operand }
public sealed record LadderReturn(
    string Id,
    LadderReturnValueKind ValueKind = LadderReturnValueKind.Rlo,
    string Operand = "");
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
    string DataType = "", string Id = "", IReadOnlyList<string>? EnumMembers = null,
    bool Retain = false, string Comment = "");

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
