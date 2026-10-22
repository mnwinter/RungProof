using System;
using System.Collections.Generic;
using System.Globalization;

namespace RungProof.Next.Scenes;

/// <summary>Command indication only; never writes the controller-owned motor image.</summary>
public static class FumeExtractorFeedback
{
    public static void Project(IDictionary<string, object?> points)
    {
        var running = points.TryGetValue("fan_run", out var run) && run is true;
        var speed = points.TryGetValue("fan_speed_percent", out var value)
            && value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
                ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : 0;
        // The three discrete indications describe the commanded preset, not the
        // selector input or measured shaft speed. An unsupported command is dark.
        points["low_speed"] = running && speed == 35;
        points["medium_speed"] = running && speed == 65;
        points["high_speed"] = running && speed == 100;
    }
}
