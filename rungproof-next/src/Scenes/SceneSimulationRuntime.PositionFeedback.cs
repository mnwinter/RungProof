using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private void ProjectReferencePositionFeedback(JsonElement motion, float position)
    {
        if (!motion.TryGetProperty("positionFeedback", out var feedback)) return;
        // This descriptor belongs to the standalone reference. It neither
        // commits PLC commands nor replaces the selected controller's image.
        if (UsesExternalClock || _externalPlaybackSelected) return;
        var point = Text(feedback, "point", string.Empty);
        if (_pointOwners.GetValueOrDefault(point) != "SIM" || _pointTypes.GetValueOrDefault(point) != "REAL")
            throw new InvalidOperationException("Reference position feedback requires a declared SIM-owned REAL point.");
        _points[point] = position * Number(feedback, "scale", 1);
        SetNc(Text(feedback, "lowNcPoint", string.Empty), position > 0.0001f);
        SetNc(Text(feedback, "highNcPoint", string.Empty), position < 0.9999f);
        var homePoint = Text(feedback, "homePoint", string.Empty);
        if (homePoint.Length > 0)
        {
            if (_pointOwners.GetValueOrDefault(homePoint) != "PC" || _pointTypes.GetValueOrDefault(homePoint) != "BOOL")
                throw new InvalidOperationException("Reference home feedback requires a declared PC-owned BOOL point.");
            _points[homePoint] = position <= 0.0001f;
        }
        ApplyBindings();
        StateChanged?.Invoke();

        void SetNc(string name, bool value)
        {
            if (name.Length == 0) return;
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException("Reference limit feedback requires a declared PC-owned BOOL point.");
            _points[name] = value;
        }
    }
}
