using System;
using System.Collections.Generic;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// Siemens TIA LAD operand pins for numeric operations represented by the
/// editor. This is presentation metadata, not controller execution semantics.
/// FORMAT_TEXT is deliberately excluded because it is a local RungProof helper.
/// </summary>
public static class TiaNumericInstructionPorts
{
    private static readonly IReadOnlyList<string> UnaryInputs = Array.AsReadOnly(new[] { "IN" });
    private static readonly IReadOnlyList<string> BinaryInputs = Array.AsReadOnly(new[] { "IN1", "IN2" });
    private static readonly IReadOnlyList<string> RangeInputs = Array.AsReadOnly(new[] { "MIN", "VALUE", "MAX" });

    public static bool IsTiaInstruction(LadderNumericOperationKind kind) => kind != LadderNumericOperationKind.FormatText;

    public static IReadOnlyList<string> Inputs(LadderNumericOperationKind kind) => kind switch
    {
        LadderNumericOperationKind.FormatText => throw new ArgumentException("FORMAT_TEXT is a local instruction, not a Siemens TIA instruction.", nameof(kind)),
        LadderNumericOperationKind.Add or LadderNumericOperationKind.Subtract or LadderNumericOperationKind.Multiply
            or LadderNumericOperationKind.Divide or LadderNumericOperationKind.Modulo or LadderNumericOperationKind.Exponentiate => BinaryInputs,
        LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale => RangeInputs,
        _ => UnaryInputs,
    };

    public static string Output(LadderNumericOperationKind kind) => kind switch
    {
        LadderNumericOperationKind.FormatText => throw new ArgumentException("FORMAT_TEXT is a local instruction, not a Siemens TIA instruction.", nameof(kind)),
        LadderNumericOperationKind.Move => "OUT1",
        _ => "OUT",
    };

    public static string Mnemonic(LadderNumericOperationKind kind) => kind switch
    {
        LadderNumericOperationKind.FormatText => throw new ArgumentException("FORMAT_TEXT is a local instruction, not a Siemens TIA instruction.", nameof(kind)),
        LadderNumericOperationKind.Move => "MOVE",
        LadderNumericOperationKind.Add => "ADD",
        LadderNumericOperationKind.Subtract => "SUB",
        LadderNumericOperationKind.Multiply => "MUL",
        LadderNumericOperationKind.Divide => "DIV",
        LadderNumericOperationKind.Modulo => "MOD",
        LadderNumericOperationKind.Absolute => "ABS",
        LadderNumericOperationKind.Negate => "NEG",
        LadderNumericOperationKind.SquareRoot => "SQRT",
        LadderNumericOperationKind.Exponentiate => "EXPT",
        LadderNumericOperationKind.NaturalLog => "LN",
        LadderNumericOperationKind.Sine => "SIN",
        LadderNumericOperationKind.Cosine => "COS",
        LadderNumericOperationKind.Tangent => "TAN",
        LadderNumericOperationKind.ArcSine => "ASIN",
        LadderNumericOperationKind.ArcCosine => "ACOS",
        LadderNumericOperationKind.ArcTangent => "ATAN",
        LadderNumericOperationKind.Truncate => "TRUNC",
        LadderNumericOperationKind.Normalize => "NORM_X",
        LadderNumericOperationKind.Scale => "SCALE_X",
        // STEP 7 names the catalog instruction CONVERT, but the LAD box
        // mnemonic shown in the official example is the compact "CONV".
        LadderNumericOperationKind.Convert => "CONV",
        LadderNumericOperationKind.Round => "ROUND",
        LadderNumericOperationKind.Ceiling => "CEIL",
        LadderNumericOperationKind.Floor => "FLOOR",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static bool HasEnablePins(LadderNumericOperationKind kind) => IsTiaInstruction(kind);

    /// <summary>Vertical spacing between data-input rows in the TIA LAD box.
    /// Multi-input blocks need extra separation because every input can show a
    /// monitored-value chip plus a symbolic operand name. A 20 px pitch lets
    /// the next row's chip paint over the preceding row's symbol.</summary>
    public static float InputRowPitch(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Add or LadderNumericOperationKind.Subtract
        or LadderNumericOperationKind.Multiply or LadderNumericOperationKind.Divide
        or LadderNumericOperationKind.Modulo or LadderNumericOperationKind.Exponentiate
            ? 32.0f
            : kind is LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale
                ? 28.0f
            : 20.0f;

    /// <summary>Conversion boxes whose official LAD examples place IN/OUT on
    /// a distinct lower row beneath the EN/ENO conductor.</summary>
    public static bool UsesLowerDataRow(LadderNumericOperationKind kind) => kind is
        LadderNumericOperationKind.Move or LadderNumericOperationKind.Convert or LadderNumericOperationKind.Truncate or LadderNumericOperationKind.Round
        or LadderNumericOperationKind.Ceiling or LadderNumericOperationKind.Floor;
}
