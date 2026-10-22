using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasServiceDoorPlant => _definition.TryGetProperty("serviceDoorPlant", out var enabled)
        && enabled.ValueKind == JsonValueKind.True;
    private ServiceDoorPlantModel? _serviceDoorPlant;
    private EquipmentMotionController? _serviceDoorMechanism;

    private void ResetServiceDoorPlant()
    {
        if (!HasServiceDoorPlant) return;
        foreach (var (name, owner, type) in new[] { ("motor_open", "PLC", "BOOL"), ("motor_close", "PLC", "BOOL"),
            ("open_limit_nc", "PC", "BOOL"), ("closed_limit_nc", "PC", "BOOL"),
            ("door_position", "SIM", "REAL"), ("cycle_complete", "SIM", "BOOL") })
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Service door requires {owner} {type} {name}.");
        _serviceDoorMechanism = _sceneRoot.GetNode<Node3D>("service_shutter").FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single(m => m.Kind == EquipmentMotionController.MotionKind.RollerShutter);
        // The delivered shutter uses inverted position input: authored scene
        // position 1 is physically closed. Keep this existing convention.
        if (!_serviceDoorMechanism.PositionInputInverted)
            throw new InvalidOperationException("Service shutter requires the delivered inverted position input.");
        _serviceDoorPlant = new(Number(_definition, "doorFullTravelSeconds", 3));
        FreezeServiceDoorAdapter(); ProjectServiceDoorPlant();
    }

    private void FreezeServiceDoorAdapter() => _serviceDoorMechanism?.SetPhysicsProcess(!UsesExternalClock);

    private void AdvanceServiceDoorPlant(double seconds)
    {
        _serviceDoorPlant!.Advance(seconds, AsBool(_points["motor_open"]), AsBool(_points["motor_close"]));
        ProjectServiceDoorPlant(); ApplyBindings(); StateChanged?.Invoke();
    }

    private void ProjectServiceDoorPlant()
    {
        if (_serviceDoorPlant is null || !UsesExternalClock) return;
        _serviceDoorMechanism!.SetPositionNormalized((float)_serviceDoorPlant.Position);
        SetPoint("open_limit_nc", _serviceDoorPlant.OpenLimitNc);
        SetPoint("closed_limit_nc", _serviceDoorPlant.ClosedLimitNc);
        SetPoint("door_position", _serviceDoorPlant.Position * 100);
        SetPoint("cycle_complete", _serviceDoorPlant.CycleComplete);
    }
}
