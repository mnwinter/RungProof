using System;

namespace RungProof.Next.App;

/// <summary>
/// Resolves the desktop workbench scale independently from ladder-diagram zoom.
/// Auto mode only reduces density for constrained windows; it never enlarges
/// the interface and therefore preserves the engineering workspace benefit of
/// larger displays.
/// </summary>
public static class DesktopUiScalePolicy
{
    public static readonly string[] Settings = ["auto", "75", "85", "100", "115"];

    public static string NormalizeSetting(string? setting)
    {
        var normalized = setting?.Trim().ToLowerInvariant() ?? string.Empty;
        return Array.IndexOf(Settings, normalized) >= 0 ? normalized : "auto";
    }

    public static int ResolvePercent(string? setting, int windowWidth, int windowHeight)
    {
        var normalized = NormalizeSetting(setting);
        if (normalized != "auto") return int.Parse(normalized);
        if (windowWidth < 1450 || windowHeight < 800) return 75;
        if (windowWidth < 1800 || windowHeight < 1000) return 85;
        return 100;
    }

    public static string Label(string setting, int resolvedPercent) =>
        NormalizeSetting(setting) == "auto"
            ? $"Auto ({resolvedPercent}%)"
            : $"{resolvedPercent}%";
}
