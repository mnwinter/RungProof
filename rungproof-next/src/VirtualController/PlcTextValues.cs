using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RungProof.Next.VirtualController;

/// <summary>Local simulator scalar text rules; no vendor encoding or enum ABI is implied.</summary>
public static class PlcTextValues
{
    public const int MaximumLength = 255;
    public static readonly IReadOnlyList<string> MotorStates = Array.AsReadOnly(new[] { "Stopped", "Running", "Fault" });
    public static bool IsText(PlcVariableType type) => type is PlcVariableType.String or PlcVariableType.Enum;

    public static bool IsValid(PlcVariable declaration, object? value) => value is string text
        && text.Length <= MaximumLength
        && (declaration.Type == PlcVariableType.String
            || declaration.Type == PlcVariableType.Enum && IsValidDomain(declaration.EnumMembers)
                && declaration.EnumMembers!.Contains(text, StringComparer.Ordinal));

    public static bool IsValidDomain(IReadOnlyList<string>? members) => members is { Count: >= 1 and <= 32 }
        && members.All(member => member is not null && Regex.IsMatch(member, @"^[A-Za-z_][A-Za-z0-9_]{0,63}$"))
        && members.Distinct(StringComparer.Ordinal).Count() == members.Count;

    public static bool SameDomain(PlcVariable left, PlcVariable right) => left.Type == right.Type
        && (left.Type == PlcVariableType.String
            || (left.EnumMembers ?? []).SequenceEqual(right.EnumMembers ?? [], StringComparer.Ordinal));

    public static bool TryLiteral(string operand, out string value)
    {
        value = string.Empty;
        if (!operand.StartsWith('"') || !operand.EndsWith('"')) return false;
        try { value = JsonSerializer.Deserialize<string>(operand) ?? string.Empty; return value.Length <= MaximumLength; }
        catch (JsonException) { return false; }
    }

    public static bool IsOperand(string operand, IReadOnlyDictionary<string, PlcVariable> variables) =>
        variables.TryGetValue(operand, out var variable) && IsText(variable.Type) || operand.StartsWith('"');

    public static bool OperandCompatible(string operand, PlcVariable target, IReadOnlyDictionary<string, PlcVariable> variables) =>
        variables.TryGetValue(operand, out var source) ? SameDomain(source, target)
            : TryLiteral(operand, out var literal) && IsValid(target, literal);

    public static string Resolve(string operand, IReadOnlyDictionary<string, string> values) =>
        values.TryGetValue(operand, out var value) ? value
            : TryLiteral(operand, out var literal) ? literal
            : throw new InvalidOperationException($"Invalid text operand '{operand}'.");

    public static bool ContainsCode(string message, string selectedCode)
    {
        if (selectedCode.Length == 0 || selectedCode.Any(character => !char.IsLetterOrDigit(character))) return false;
        var start = 0;
        for (var index = 0; index <= message.Length; index++)
        {
            if (index < message.Length && char.IsLetterOrDigit(message[index])) continue;
            if (index > start && message.AsSpan(start, index - start).SequenceEqual(selectedCode.AsSpan())) return true;
            start = index + 1;
        }
        return false;
    }

    // A deliberately bounded local instruction: one fixed-point placeholder,
    // no alignment, other argument indexes, arbitrary formats, or culture dependence.
    public static bool TryFormat(double number, string template, out string result)
    {
        result = string.Empty;
        if (!double.IsFinite(number) || template.Length > MaximumLength) return false;
        var match = Regex.Match(template, @"\{0:F([0-6])\}", RegexOptions.CultureInvariant);
        if (!match.Success || Regex.Matches(template, @"\{0:F[0-6]\}").Count != 1) return false;
        var remainder = template.Remove(match.Index, match.Length);
        if (remainder.Contains('{') || remainder.Contains('}')) return false;
        result = template[..match.Index] + number.ToString("F" + match.Groups[1].Value, CultureInfo.InvariantCulture)
            + template[(match.Index + match.Length)..];
        if (result.Length <= MaximumLength) return true;
        result = string.Empty;
        return false;
    }
}
