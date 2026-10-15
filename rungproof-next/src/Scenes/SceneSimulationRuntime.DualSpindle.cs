using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasDualSpindlePlant => _definition.TryGetProperty("controllerDualSpindle", out var value)
        && value.ValueKind == System.Text.Json.JsonValueKind.True;
    private EquipmentMotionController[]? _dualHeads;
    private EquipmentMotionController? _dualSlide;
    private readonly double[] _dualHeadPositions = new double[2];
    private double _dualSlidePosition;

    private void ResetDualSpindlePlant()
    {
        if (!HasDualSpindlePlant) return;
        _dualHeads = new[] { "drill_a", "drill_b" }.Select(id =>
            _sceneRoot.GetNode<Node3D>(id).FindChildren("*", "", true, false)
                .OfType<EquipmentMotionController>().Single()).ToArray();
        _dualSlide = _sceneRoot.GetNode<Node3D>("plate_transfer").FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single();
        Array.Clear(_dualHeadPositions);
        _dualSlidePosition = 0;
        FreezeDualSpindleAdapters();
        SetPoint("transfer_inhibited", false);
        ProjectDualSpindlePlant();
    }

    private void FreezeDualSpindleAdapters()
    {
        // The accepted controller tick owns rotation and travel together.
        // Standalone reference playback retains its existing animation clock.
        if (_dualHeads is null) return;
        foreach (var head in _dualHeads) head.SetPhysicsProcess(!UsesExternalClock);
        _dualSlide!.SetPhysicsProcess(!UsesExternalClock);
    }

    private void AdvanceDualSpindlePlant(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return;
        var names = new[] { "drill_a", "drill_b" };
        for (var i = 0; i < 2; i++)
        {
            var run = AsBool(_points.GetValueOrDefault(names[i] + "_run"));
            var feed = AsBool(_points.GetValueOrDefault(names[i] + "_feed"));
            var retract = AsBool(_points.GetValueOrDefault(names[i] + "_retract"));
            // Contradictory requests hold; retract does not require rotation.
            // A displaced fixture prevents a head from feeding into empty space.
            if (feed != retract && (!feed || (run && _dualSlidePosition == 0)))
            {
                var duration = feed ? 1.7 : 1.6;
                _dualHeadPositions[i] = MoveDualAxis(_dualHeadPositions[i], feed ? 1 : 0, seconds / duration);
            }
            _dualHeads![i].RunCommand = run;
            _dualHeads[i].SetPositionNormalized((float)_dualHeadPositions[i]);
            _dualHeads[i]._PhysicsProcess(seconds);
        }
        var extend = AsBool(_points.GetValueOrDefault("transfer_extend"));
        var back = AsBool(_points.GetValueOrDefault("transfer_retract"));
        var clear = _dualHeadPositions.All(position => position == 0)
            && names.All(name => !AsBool(_points.GetValueOrDefault(name + "_run")));
        SetPoint("transfer_inhibited", (extend || back) && (!clear || extend == back));
        if (clear && extend != back)
            _dualSlidePosition = MoveDualAxis(_dualSlidePosition, extend ? 1 : 0, seconds / 1.2);
        _dualSlide!.SetPositionNormalized((float)_dualSlidePosition);
        ProjectDualSpindlePlant();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private static double MoveDualAxis(double position, double target, double distance)
    {
        // Snap only at the requested endpoint; avoid a floating-point residue
        // preventing a measured limit from ever becoming active.
        if (Math.Abs(target - position) <= distance + 1e-12) return target;
        return position + Math.CopySign(distance, target - position);
    }

    private void ProjectDualSpindlePlant()
    {
        for (var i = 0; i < 2; i++)
        {
            var prefix = i == 0 ? "drill_a" : "drill_b";
            SetPoint(prefix + "_position", _dualHeadPositions[i] * 100);
            SetPoint(prefix + "_home", _dualHeadPositions[i] == 0);
            SetPoint(prefix + "_at_depth", _dualHeadPositions[i] == 1);
        }
        SetPoint("transfer_position", _dualSlidePosition * 100);
        SetPoint("transfer_home", _dualSlidePosition == 0);
        SetPoint("transfer_at_end", _dualSlidePosition == 1);
    }
}
