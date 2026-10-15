using System;

namespace RungProof.Next.Scenes;

/// <summary>One finite teaching load on ten contiguous driven zones. Command
/// truth is supplied by the accepted PLC image, never inferred permissives.</summary>
public sealed class MotorConveyorZonePlantModel
{
    public const int ZoneCount = 10;
    public const double ZoneLength = 1.4, LoadLength = .60, Speed = .45;
    public double Center { get; private set; } = LoadLength / 2;
    public double LeadingEdge => Center + LoadLength / 2;
    public double TrailingEdge => Center - LoadLength / 2;
    public int Zone => Math.Min(9, (int)(Center / ZoneLength));
    public bool AtDischarge => LeadingEdge >= ZoneCount * ZoneLength - 1e-9;
    public void Reset() => Center = LoadLength / 2;
    public bool BeamBlocked(double line)
    {
        if (!double.IsFinite(line) || line < 0 || line > ZoneCount * ZoneLength)
            throw new ArgumentException("Sensing line must lie on the finite conveyor.");
        return TrailingEdge <= line + 1e-9 && LeadingEdge >= line - 1e-9;
    }
    public void Step(double seconds, bool[] commands)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || commands.Length != ZoneCount)
            throw new ArgumentException("Ten-zone step requires finite nonnegative time and ten commands.");
        // Every zone overlapped by the finite load must drive. At a stopped
        // downstream boundary the leading edge holds; running upstream shafts
        // may continue under their command, without transporting through it.
        for (var zone = 0; zone < ZoneCount; zone++)
            if (!commands[zone] && TrailingEdge < (zone + 1) * ZoneLength - 1e-9
                && LeadingEdge > zone * ZoneLength + 1e-9) return;
        var limit = ZoneCount * ZoneLength - LoadLength / 2;
        for (var zone = 0; zone < ZoneCount; zone++)
            if (!commands[zone] && zone * ZoneLength >= LeadingEdge - 1e-9)
                limit = Math.Min(limit, zone * ZoneLength - LoadLength / 2);
        Center = Math.Min(limit, Center + Speed * seconds);
    }
}
