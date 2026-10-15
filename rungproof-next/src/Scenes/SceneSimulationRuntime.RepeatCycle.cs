using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    /// <summary>Copied actual state for passive accepted-scan observers; independent of history recording.</summary>
    public RungProof.Next.Lessons.RepeatCyclePlantImage? CaptureRepeatCyclePlantState()
    {
        if (_repeatCycle is null) return null;
        return new(_repeatCycle.Phase, _repeatCycle.HomeFraction, _repeatCycle.SpindleRadians,
            _repeatCycle.Home, _repeatCycle.Busy, _repeatCycle.Done);
    }

    private bool HasRepeatCyclePlant => _definition.TryGetProperty("repeatCyclePlant", out _);
    private RepeatCyclePlantModel? _repeatCycle;
    private EquipmentMotionController? _repeatHeadMotion;
    private Node3D? _repeatSpindle;
    private Basis _repeatSpindleAuthored;

    private void ResetRepeatCyclePlant()
    {
        if (!HasRepeatCyclePlant) return;
        if (Text(_definition.GetProperty("repeatCyclePlant"), "model", "") != "cnc-dry-stroke-v1")
            throw new InvalidOperationException("Unknown repeat-cycle plant model.");
        foreach (var name in new[] { "cycle_request", "machine_enabled", "machine_home", "machine_busy", "cycle_done" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException($"Repeat-cycle plant requires PC-owned BOOL '{name}'.");
        foreach (var name in new[] { "cycle_active", "cycle_complete" })
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException($"Repeat-cycle plant requires PLC-owned BOOL '{name}'.");
        if (_pointOwners.GetValueOrDefault("head_position") != "PC" || _pointTypes.GetValueOrDefault("head_position") != "REAL")
            throw new InvalidOperationException("Repeat-cycle plant requires PC-owned REAL head_position.");
        if (_pointOwners.GetValueOrDefault("batch_count") != "PLC" || _pointTypes.GetValueOrDefault("batch_count") != "DINT")
            throw new InvalidOperationException("Repeat-cycle plant requires PLC-owned DINT batch_count.");
        var machine = _sceneRoot.GetNode<Node3D>("machine_0");
        _repeatHeadMotion = machine.GetNode<EquipmentMotionController>("RepeatCycleHeadMotion");
        _repeatSpindle = (Node3D)machine.FindChild("KIN_spindle", true, false);
        if (_repeatCycle is null) _repeatSpindleAuthored = _repeatSpindle.Basis;
        _repeatCycle ??= new(); _repeatCycle.Reset();
        FreezeRepeatCycleAdapters(); ProjectRepeatCyclePlant();
    }

    private void FreezeRepeatCycleAdapters()
    {
        if (!HasRepeatCyclePlant) return;
        // The accepted controller/plant tick owns both feed and rotation.
        // Imported motion adapters must never also integrate Godot callbacks.
        foreach (var controller in Controllers(_sceneRoot.GetNode<Node3D>("machine_0")))
            controller.SetPhysicsProcess(false);
    }

    private void AdvanceRepeatCyclePlant(double seconds)
    {
        if (_repeatCycle is null) return;
        _repeatCycle.Step(seconds, AsBool(_points["cycle_active"]));
        ProjectRepeatCyclePlant(); ApplyBindings(); StateChanged?.Invoke();
    }

    private void ProjectRepeatCyclePlant()
    {
        if (_repeatCycle is null) return;
        _repeatHeadMotion!.SetPositionNormalized((float)_repeatCycle.HomeFraction);
        _repeatSpindle!.Basis = _repeatSpindleAuthored.Rotated(Vector3.Up, (float)_repeatCycle.SpindleRadians);
        SetPoint("machine_home", _repeatCycle.Home); SetPoint("machine_busy", _repeatCycle.Busy);
        SetPoint("cycle_done", _repeatCycle.Done); SetPoint("head_position", _repeatCycle.HomeFraction * 100);
    }
}
