using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasDrillPlant => _definition.TryGetProperty("guardedDrillPlant", out var enabled)
        && enabled.ValueKind == JsonValueKind.True;
    private GuardedDrillPlantModel? _drillPlant;
    private EquipmentMotionController? _drillMechanism;
    private void ResetDrillPlant()
    {
        if (!HasDrillPlant) return;
        foreach (var (name, owner, type) in new[] { ("drill_run", "PLC", "BOOL"),
            ("drill_feed_down", "PLC", "BOOL"), ("drill_retract_up", "PLC", "BOOL"),
            ("left_hand_request", "PC", "BOOL"), ("right_hand_request", "PC", "BOOL"), ("workpiece_present", "PC", "BOOL"),
            ("drill_at_top", "PC", "BOOL"), ("drill_at_bottom", "PC", "BOOL"), ("drill_motion_inhibited", "PC", "BOOL") })
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Guarded drill requires {owner} {type} {name}.");
        _drillMechanism = _sceneRoot.GetNode<Node3D>("safe_drill").FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single(m => m.Kind == EquipmentMotionController.MotionKind.SpindleFeed);
        _drillPlant = new(Number(_definition, "drillFullTravelSeconds", 1.4));
        FreezeDrillAdapter(); ProjectDrillPlant();
    }
    private void FreezeDrillAdapter() => _drillMechanism?.SetPhysicsProcess(!UsesExternalClock);
    private void AdvanceDrillPlant(double seconds)
    {
        _drillPlant!.Advance(seconds, AsBool(_points["drill_run"]), AsBool(_points["drill_feed_down"]),
            AsBool(_points["drill_retract_up"]), AsBool(_points["left_hand_request"]),
            AsBool(_points["right_hand_request"]), AsBool(_points["workpiece_present"]));
        ProjectDrillPlant();
        _drillMechanism!.RunCommand = AsBool(_points["drill_run"]);
        _drillMechanism._PhysicsProcess(seconds);
        ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectDrillPlant()
    {
        if (_drillPlant is null || !UsesExternalClock) return;
        _drillMechanism!.SetPositionNormalized((float)_drillPlant.Position);
        SetPoint("drill_at_top", _drillPlant.AtTop); SetPoint("drill_at_bottom", _drillPlant.AtBottom);
        SetPoint("drill_motion_inhibited", _drillPlant.MotionInhibited);
        SetPoint("cycle_complete", _drillPlant.CycleComplete);
        SetPoint("cycle_color", !PlantPlaybackRunning ? "red" : AsBool(_points["drill_run"]) ? "green" : "amber");
    }
}
