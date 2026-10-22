using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasJugFillPlant => _definition.TryGetProperty("jugFillPlant", out var enabled) && enabled.ValueKind == JsonValueKind.True;
    private JugFillPlantModel? _jugFillPlant;
    private Node3D? _coolantJug, _jugStream;
    private MeshInstance3D? _jugNeck, _jugTip;
    private ConveyorController? _jugDrive;
    private EquipmentMotionController? _jugNozzle;

    private void ResetJugFillPlant()
    {
        if (!HasJugFillPlant) return;
        foreach (var (name, owner, type) in new[] { ("conveyor_run", "PLC", "BOOL"), ("fill_valve_open", "PLC", "BOOL"),
            ("fill_skid_run", "PLC", "BOOL"), ("jug_present", "PC", "BOOL"), ("high_level_probe", "PC", "BOOL"),
            ("fill_percent", "SIM", "REAL"), ("cycle_complete", "SIM", "BOOL"), ("cell_color", "SIM", "STRING") })
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Jug fill requires {owner} {type} {name}.");
        _coolantJug = _sceneRoot.GetNode<Node3D>("coolant_jug");
        var conveyor = _sceneRoot.GetNode<Node3D>("fill_conveyor");
        _jugDrive = conveyor.FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
        var station = _sceneRoot.GetNode<Node3D>("fill_valve");
        _jugNozzle = station.FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
        _jugNeck = (MeshInstance3D)_coolantJug.FindChild("JUG_NECK", true, false);
        _jugTip = (MeshInstance3D)station.FindChild("NOZZLE_TIP", true, false);
        _jugStream = (Node3D)station.FindChild("JUG_FILL_stream", true, false);
        var load = DeliveredEquipmentBounds(_coolantJug);
        var home = _coolantJug.Position.X;
        var exit = Number(_definition, "jugExitX", 3.5);
        var beltNode = (MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false);
        var belt = beltNode.GlobalTransform * beltNode.GetAabb();
        if (load.Position.X < belt.Position.X || load.End.X + exit - home > belt.End.X
            || load.Position.Z < belt.Position.Z || load.End.Z > belt.End.Z
            || MathF.Abs(load.Position.Y - belt.End.Y) > .002f)
            throw new InvalidOperationException("Jug route must remain supported by the delivered belt.");
        var neck = _jugNeck.GlobalTransform * _jugNeck.GetAabb();
        var tip = _jugTip.GlobalTransform * _jugTip.GetAabb();
        var aligned = home + tip.GetCenter().X - neck.GetCenter().X;
        // Leading-edge sensor detects the indexed neck position, rather than
        // stopping the jug one body half-length before the nozzle.
        var sensor = _sceneRoot.GetNode<Node3D>("jug_present_sensor");
        var tx = (MeshInstance3D)sensor.FindChild("TX_lens", true, false);
        var oldBeam = tx.GlobalTransform * tx.GetAabb().GetCenter();
        var beamX = aligned + load.End.X - home;
        sensor.Position += Vector3.Right * (beamX - oldBeam.X);
        _jugFillPlant = new(home, exit, Number(_definition, "conveyorSpeedMps", .75), beamX,
            load.Position.X - home, load.End.X - home, Number(_definition, "jugFillFractionPerSecond", 1 / 2.2));
        _jugDrive.ResetPlantTravel(); FreezeJugFillAdapter(); ProjectJugFillPlant();
    }
    private void FreezeJugFillAdapter()
    {
        _jugDrive?.SetPhysicsProcess(!UsesExternalClock);
        _jugNozzle?.SetPhysicsProcess(!UsesExternalClock);
    }
    private bool JugNozzleInsideNeck()
    {
        var neck = _jugNeck!.GlobalTransform * _jugNeck.GetAabb();
        var tip = _jugTip!.GlobalTransform * _jugTip.GetAabb();
        if (tip.Position.Y < neck.Position.Y || tip.Position.Y > neck.End.Y - .002f) return false;
        var center = neck.GetCenter();
        // OpenJugForFilling builds the inner neck radius at .075 local units.
        var scale = _jugNeck.GlobalTransform.Basis.Scale;
        var radiusX = .075f * MathF.Abs(scale.X) - .002f;
        var radiusZ = .075f * MathF.Abs(scale.Z) - .002f;
        return radiusX > 0 && radiusZ > 0 && _jugTip.Mesh.GetFaces().All(v =>
        {
            var world = _jugTip.GlobalTransform * v;
            var dx = (world.X - center.X) / radiusX; var dz = (world.Z - center.Z) / radiusZ;
            return dx * dx + dz * dz < 1;
        });
    }
    private void AdvanceJugFillPlant(double seconds)
    {
        var running = AsBool(_points["conveyor_run"]);
        _jugFillPlant!.Advance(seconds, running, AsBool(_points["fill_valve_open"]),
            AsBool(_points["fill_skid_run"]), JugNozzleInsideNeck());
        _coolantJug!.Position = new Vector3((float)_jugFillPlant.Position, _coolantJug.Position.Y, _coolantJug.Position.Z);
        var speed = running ? Number(_definition, "conveyorSpeedMps", .75) : 0;
        _jugDrive!.ApplyPlantTravel((float)(speed * seconds), (float)speed);
        ProjectJugFillPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectJugFillPlant()
    {
        if (_jugFillPlant is null || !UsesExternalClock) return;
        SetPoint("jug_present", _jugFillPlant.Present); SetPoint("high_level_probe", _jugFillPlant.High);
        SetPoint("fill_percent", _jugFillPlant.Fraction * 100);
        SetPoint("cycle_complete", _jugFillPlant.AtExit && _jugFillPlant.High);
        SetPoint("cell_color", !PlantPlaybackRunning ? "red" : AsBool(_points["conveyor_run"]) ? "green" : "amber");
        ProjectJugFillVisibility();
    }
    private void ProjectJugFillVisibility()
    {
        if (_jugFillPlant is null || !UsesExternalClock) return;
        _jugStream!.Visible = PlantPlaybackRunning && !AsBool(_points["conveyor_run"])
            && AsBool(_points["fill_valve_open"]) && AsBool(_points["fill_skid_run"])
            && !_jugFillPlant.High && JugNozzleInsideNeck();
    }
}
