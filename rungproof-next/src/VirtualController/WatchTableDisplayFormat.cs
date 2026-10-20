using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Supported offline watch value formats by declared PLC scalar type.</summary>
public static class WatchTableDisplayFormat
{
    public static IReadOnlyList<string> ForType(PlcVariableType? type) => type switch
    {
        PlcVariableType.Bool => ["Auto", "Bool", "Hex", "Bin", "Dec", "Dec+/-"],
        PlcVariableType.Int or PlcVariableType.DInt => ["Auto", "Dec", "Dec+/-", "Hex", "Bin"],
        PlcVariableType.Real => ["Auto", "Floating-point number", "Hex", "Dec", "Dec+/-"],
        PlcVariableType.Time => ["Auto", "Timer", "Hex", "Bin", "Dec", "Dec+/-"],
        PlcVariableType.String => ["Auto", "Character string"],
        // This offline enum stores names but does not define numeric values.
        PlcVariableType.Enum => ["Auto", "Named value"],
        _ => ["Auto"],
    };

    public static bool IsValid(PlcVariableType? type, string? format)
    {
        var candidate = (format ?? string.Empty).Trim();
        if (candidate.Equals("Binary", StringComparison.OrdinalIgnoreCase)) candidate = "Bin";
        if (candidate.Equals("Signed decimal", StringComparison.OrdinalIgnoreCase)) candidate = "Dec+/-";
        return ForType(type).Contains(candidate, StringComparer.OrdinalIgnoreCase);
    }

    public static string FormatInteger(long value, PlcVariableType type, string format)
    {
        if (type == PlcVariableType.Int)
        {
            var word = unchecked((ushort)(short)value);
            return format.ToUpperInvariant() switch
            {
                "HEX" => $"16#{word:X4}",
                "BIN" => Convert.ToString(word, 2).PadLeft(16, '0'),
                _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
        }
        var dword = unchecked((uint)value);
        return format.ToUpperInvariant() switch
        {
            "HEX" => $"16#{dword:X8}",
            "BIN" => Convert.ToString(dword, 2).PadLeft(32, '0'),
            _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    public static string FormatReal(double value, string format)
    {
        var real = (float)value;
        return format.Equals("Hex", StringComparison.OrdinalIgnoreCase)
            ? $"16#{BitConverter.SingleToInt32Bits(real):X8}"
            : real.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    }
}
