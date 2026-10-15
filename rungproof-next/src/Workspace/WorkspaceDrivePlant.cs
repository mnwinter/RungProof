using System;

namespace RungProof.Next.Workspace;

/// <summary>
/// Renderer-neutral conveyor drive state, advanced only by its caller. Mirrors
/// the existing conveyor's float acceleration and end-speed travel integration.
/// It owns no timer, PLC transport, sensor, hidden feedback or autonomous tick.
/// </summary>
public sealed class WorkspaceDrivePlant
{
    public float ActualSpeedMps { get; private set; }
    public float TravelM { get; private set; }
    public bool Running => MathF.Abs(ActualSpeedMps) > .01f;

    /// <returns>Distance for the renderer to project exactly once.</returns>
    public float Step(double seconds, bool runCommand, bool estopOk = true,
        double speedSetpointMps = .65, double accelerationMps2 = .75)
    {
        // Validate before changing state. Negative acceleration would move away
        // from the requested target, and float overflow cannot represent travel.
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > float.MaxValue
            || !double.IsFinite(speedSetpointMps) || !double.IsFinite(accelerationMps2)
            || accelerationMps2 < 0 || accelerationMps2 > float.MaxValue)
            throw new ArgumentException("Drive step requires finite non-negative representable time/acceleration and finite speed.");
        if (seconds == 0) return 0;
        var dt = (float)seconds;
        var target = runCommand && estopOk ? Math.Clamp((float)Math.Clamp(speedSetpointMps, -2d, 2d), -2f, 2f) : 0f;
        var acceleration = (float)accelerationMps2 * dt;
        if (!float.IsFinite(acceleration)) throw new ArgumentException("Drive acceleration interval is not representable.");
        var difference = target - ActualSpeedMps;
        var speed = MathF.Abs(difference) <= acceleration
            ? target : ActualSpeedMps + MathF.Sign(difference) * acceleration;
        var distance = speed * dt;
        var travel = TravelM + distance;
        if (!float.IsFinite(distance) || !float.IsFinite(travel))
            throw new ArgumentException("Drive travel is not representable.");
        ActualSpeedMps = speed;
        TravelM = travel;
        return distance;
    }

    /// <summary>Explicit application Stop; unlike removing Run, this does not coast.</summary>
    public void Stop() => ActualSpeedMps = 0;

    /// <summary>Restores the model origin; the renderer must restore its captured homes.</summary>
    public void Reset() { ActualSpeedMps = 0; TravelM = 0; }
}
