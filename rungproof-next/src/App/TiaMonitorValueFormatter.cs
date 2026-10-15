using System;
using System.Globalization;

namespace RungProof.Next.App;

/// <summary>Formats online value chips without changing the underlying operand.</summary>
public static class TiaMonitorValueFormatter
{
    public static string Format(string value, bool tiaPresentation)
    {
        if (!tiaPresentation) return value;
        if (value.EndsWith(" (BOOL)", StringComparison.OrdinalIgnoreCase)) return value[..^7];
        const string realSuffix = " (REAL)";
        if (value.EndsWith(realSuffix, StringComparison.OrdinalIgnoreCase))
            return FormatReal(value[..^realSuffix.Length]) + value[^realSuffix.Length..];
        return value;
    }

    public static string FormatReal(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return value;
        return ((float)parsed).ToString("G9", CultureInfo.InvariantCulture);
    }
}
