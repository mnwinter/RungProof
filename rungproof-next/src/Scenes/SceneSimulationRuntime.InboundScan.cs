using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasInboundScanPlant => _definition.TryGetProperty("inboundScanPlant", out var enabled)
        && enabled.ValueKind == JsonValueKind.True;
    private InboundScanPlantModel? _inboundScan;
    private Node3D? _inboundTote;
    private ConveyorController? _inboundDrive;

    private void ResetInboundScanPlant()
    {
        if (!HasInboundScanPlant) return;
        foreach (var (name, owner, type) in new[] { ("conveyor_run", "PLC", "BOOL"),
            ("tote_at_scanner", "PC", "BOOL"), ("scan_complete", "PC", "BOOL"),
            ("cycle_complete", "SIM", "BOOL"), ("cell_color", "SIM", "STRING") })
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Inbound scanner requires {owner} {type} {name}.");
        _inboundTote = _sceneRoot.GetNode<Node3D>("inbound_tote");
        var conveyor = _sceneRoot.GetNode<Node3D>("inbound_conveyor");
        _inboundDrive = conveyor.FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
        var load = DeliveredEquipmentBounds(_inboundTote);
        var beltNode = (MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false);
        var belt = beltNode.GlobalTransform * beltNode.GetAabb();
        var home = _inboundTote.Position.X;
        var exit = Number(_definition, "inboundExitX", 3.4);
        if (load.Position.X < belt.Position.X || load.End.X + exit - home > belt.End.X
            || load.Position.Z < belt.Position.Z || load.End.Z > belt.End.Z
            || MathF.Abs(load.Position.Y - belt.End.Y) > .002f)
            throw new InvalidOperationException("Inbound tote route must remain supported by the delivered belt.");
        var scanner = _sceneRoot.GetNode<Node3D>("scan_photoeye");
        var tx = (MeshInstance3D)scanner.FindChild("TX_lens", true, false);
        var beam = tx.GlobalTransform * tx.GetAabb().GetCenter();
        if (beam.Y < load.Position.Y || beam.Y > load.End.Y)
            throw new InvalidOperationException("Inbound scanner beam must intersect the delivered tote body.");
        _inboundScan = new(home, exit, Number(_definition, "conveyorSpeedMps", .75),
            beam.X, load.Size.X / 2, Number(_definition, "scannerIdentificationSeconds", .8));
        _inboundDrive.ResetPlantTravel(); FreezeInboundScanAdapter(); ProjectInboundScanPlant();
    }
    private void FreezeInboundScanAdapter() => _inboundDrive?.SetPhysicsProcess(!UsesExternalClock);
    private void AdvanceInboundScanPlant(double seconds)
    {
        var running = AsBool(_points["conveyor_run"]);
        _inboundScan!.Advance(seconds, running);
        _inboundTote!.Position = new Vector3((float)_inboundScan.Position, _inboundTote.Position.Y, _inboundTote.Position.Z);
        // The bounded tote display must not silently stop the commanded belt.
        var speed = running ? Number(_definition, "conveyorSpeedMps", .75) : 0;
        _inboundDrive!.ApplyPlantTravel((float)(speed * seconds), (float)speed);
        ProjectInboundScanPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectInboundScanPlant()
    {
        if (_inboundScan is null || !UsesExternalClock) return;
        SetPoint("tote_at_scanner", _inboundScan.AtScanner);
        SetPoint("scan_complete", _inboundScan.ScanComplete);
        SetPoint("cycle_complete", _inboundScan.AtExit && _inboundScan.ScanComplete);
        SetPoint("cell_color", !PlantPlaybackRunning ? "red"
            : AsBool(_points["conveyor_run"]) ? "green" : "amber");
    }
}
