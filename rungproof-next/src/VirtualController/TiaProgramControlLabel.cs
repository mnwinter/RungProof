using System.Text.RegularExpressions;

namespace RungProof.Next.VirtualController;

/// <summary>Siemens STEP 7 V21 grammar for LAD JMP/LABEL names.</summary>
public static class TiaProgramControlLabel
{
    private static readonly Regex ValidName = new(
        @"^[A-Za-z]+[0-9]*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public const string ValidationMessage =
        "TIA jump labels must start with letters and may end with digits; spaces, underscores, other special characters, and number-first names are not allowed.";

    public static bool IsValid(string? name) =>
        !string.IsNullOrEmpty(name) && ValidName.IsMatch(name);
}
