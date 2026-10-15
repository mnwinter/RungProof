using System;
using System.Collections.Generic;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    /// <summary>
    /// Publishes measured offline drive feedback into its declared PC domain.
    /// Null means unavailable, never a fabricated stopped/zero measurement.
    /// Validate the entire pair before committing either point.
    /// </summary>
    public void PublishWorkspaceDriveFeedback(string runningPoint, string speedPoint,
        bool? running, double? speedMps)
    {
        if (_pointOwners.GetValueOrDefault(runningPoint) != "PC"
            || _pointTypes.GetValueOrDefault(runningPoint) != "BOOL"
            || _pointOwners.GetValueOrDefault(speedPoint) != "PC"
            || _pointTypes.GetValueOrDefault(speedPoint) != "REAL"
            || running.HasValue != speedMps.HasValue
            || speedMps is { } speed && !double.IsFinite(speed))
            throw new InvalidOperationException("Drive feedback requires declared PC BOOL/REAL points and a finite measured pair, or an unavailable pair.");
        _points[runningPoint] = running;
        _points[speedPoint] = speedMps;
        StateChanged?.Invoke();
    }
}
