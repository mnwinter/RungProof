using System;
using System.Globalization;

namespace RungProof.Next.VirtualController;

/// <summary>Parses the positive S7 TIME literals accepted by the offline TON PT editor.</summary>
public static class LadderTimeLiteral
{
    private const long MaximumMilliseconds = int.MaxValue;

    public static string Format(TimeSpan value)
    {
        var milliseconds = checked((long)Math.Round(value.TotalMilliseconds, MidpointRounding.AwayFromZero));
        return "T#" + milliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
    }

    public static bool TryParse(string? text, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var input = text.Trim();
        if (input.Length > 0 && IsDigits(input))
        {
            if (!long.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)) return false;
            return TryFromMilliseconds(milliseconds, out value);
        }
        var body = input.StartsWith("TIME#", StringComparison.OrdinalIgnoreCase) ? input[5..]
            : input.StartsWith("T#", StringComparison.OrdinalIgnoreCase) ? input[2..]
            : string.Empty;
        if (body.Length == 0) return false;

        decimal totalMilliseconds = 0;
        var index = 0;
        while (index < body.Length)
        {
            var numberStart = index;
            var decimalPointSeen = false;
            while (index < body.Length && (char.IsAsciiDigit(body[index]) || body[index] == '.' && !decimalPointSeen))
            {
                if (body[index] == '.') decimalPointSeen = true;
                index++;
            }
            if (numberStart == index || body[index - 1] == '.') return false;
            if (!decimal.TryParse(body[numberStart..index], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                return false;

            var unitStart = index;
            while (index < body.Length && char.IsAsciiLetter(body[index])) index++;
            if (unitStart == index) return false;
            var unit = body[unitStart..index];
            var multiplier = unit.ToLowerInvariant() switch
            {
                "ms" => 1L,
                "s" => 1_000L,
                "m" => 60_000L,
                "h" => 3_600_000L,
                "d" => 86_400_000L,
                _ => 0L,
            };
            if (multiplier == 0) return false;
            try { totalMilliseconds = checked(totalMilliseconds + checked(amount * multiplier)); }
            catch (OverflowException) { return false; }
            if (totalMilliseconds > MaximumMilliseconds) return false;

            if (index == body.Length) break;
            if (body[index] == '_')
            {
                index++;
                if (index == body.Length || body[index] == '_') return false;
            }
            else if (!char.IsAsciiDigit(body[index])) return false;
        }
        if (totalMilliseconds != decimal.Truncate(totalMilliseconds)) return false;
        return TryFromMilliseconds((long)totalMilliseconds, out value);
    }

    private static bool IsDigits(string value)
    {
        foreach (var character in value)
            if (!char.IsAsciiDigit(character)) return false;
        return true;
    }

    private static bool TryFromMilliseconds(long milliseconds, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        if (milliseconds <= 0 || milliseconds > MaximumMilliseconds) return false;
        value = TimeSpan.FromMilliseconds(milliseconds);
        return true;
    }
}
