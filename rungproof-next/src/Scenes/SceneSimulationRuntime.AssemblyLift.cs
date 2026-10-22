using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasAssemblyLiftPlant => _definition.TryGetProperty("assemblyLiftPlant", out var enabled)
        && enabled.ValueKind == JsonValueKind.True;
    private BidirectionalLiftPlantModel? _assemblyLiftPlant;
    private EquipmentMotionController? _assemblyLiftMechanism;
    private Node3D? _assemblyFixture;
    private float _assemblyFixtureHome;

    private void ResetAssemblyLiftPlant()
    {
        if (!HasAssemblyLiftPlant) return;
        foreach (var (name, owner, type) in new[] { ("lift_up", "PLC", "BOOL"), ("lift_down", "PLC", "BOOL"),
            ("top_limit", "PC", "BOOL"), ("bottom_limit", "PC", "BOOL"),
            ("lift_position", "SIM", "REAL"), ("cycle_complete", "SIM", "BOOL") })
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Assembly lift requires {owner} {type} {name}.");
        _assemblyLiftMechanism = _sceneRoot.GetNode<Node3D>("assembly_lift").FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single();
        _assemblyFixture = _sceneRoot.GetNode<Node3D>("lift_fixture");
        _assemblyFixtureHome = _assemblyFixture.Position.Y;
        _assemblyLiftPlant = new(Number(_definition, "liftFullTravelSeconds", 2));
        FreezeAssemblyLiftAdapter(); ProjectAssemblyLiftPlant();
    }
    private void FreezeAssemblyLiftAdapter() => _assemblyLiftMechanism?.SetPhysicsProcess(!UsesExternalClock);
    private void AdvanceAssemblyLiftPlant(double seconds)
    {
        _assemblyLiftPlant!.Advance(seconds, AsBool(_points["lift_up"]), AsBool(_points["lift_down"]));
        ProjectAssemblyLiftPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectAssemblyLiftPlant()
    {
        if (_assemblyLiftPlant is null || !UsesExternalClock) return;
        var position = _assemblyLiftPlant.Position;
        _assemblyLiftMechanism!.SetPositionNormalized((float)position);
        _assemblyFixture!.Position = new Vector3(_assemblyFixture.Position.X,
            _assemblyFixtureHome + (float)(position * Number(_definition, "liftTravelM", 2.2)), _assemblyFixture.Position.Z);
        SetPoint("top_limit", _assemblyLiftPlant.AtTop); SetPoint("bottom_limit", _assemblyLiftPlant.AtBottom);
        SetPoint("lift_position", position * 100); SetPoint("cycle_complete", _assemblyLiftPlant.CycleComplete);
    }
}
