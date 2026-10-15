using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasToteTransfer => _definition.TryGetProperty("controllerToteTransfer", out var value) && value.ValueKind == System.Text.Json.JsonValueKind.True;
    private ToteTransferPlantModel? _toteTransfer;
    private Node3D? _transferTote;
    private ConveyorController? _toteTransferDrive;

    private void ResetToteTransfer()
    {
        if (!HasToteTransfer) return;
        _transferTote = _sceneRoot.GetNode<Node3D>("finishing_tote");
        var conveyor = _sceneRoot.GetNode<Node3D>("finishing_conveyor");
        _toteTransferDrive = conveyor.FindChildren("*", string.Empty, true, false).OfType<ConveyorController>().Single();
        var home = _transferTote.Position.X;
        var exit = Number(_definition, "toteExitX", 6.4);
        var surface = (MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false);
        var belt = surface.GlobalTransform * surface.GetAabb();
        var load = DeliveredEquipmentBounds(_transferTote);
        var end = load.Position.X + (float)exit - home + load.Size.X;
        if (load.Position.X < belt.Position.X || end > belt.End.X
            || load.Position.Z < belt.Position.Z || load.End.Z > belt.End.Z
            || MathF.Abs(load.Position.Y - belt.End.Y) > .002f)
            throw new InvalidOperationException("Tote controller route must remain supported by the delivered belt.");
        _toteTransfer = new ToteTransferPlantModel(home, exit, Number(_definition, "conveyorSpeedMps", .75));
        _toteTransferDrive.ResetPlantTravel();
        ResetToteFill();
        ResetToteCap();
        ResetToteFinishingProcess();
        FreezeToteTransferAdapter();
        SetPoint("tote_transfer_inhibited", false);
        ProjectToteTransfer();
    }

    private void FreezeToteTransferAdapter()
    {
        // The legacy standalone sequence remains an explicit geometry preview.
        // Controller operation uses one accepted plant tick for belt and load.
        if (_toteTransferDrive is not null) _toteTransferDrive.SetPhysicsProcess(!UsesExternalClock);
        _toteFillNozzleController?.SetPhysicsProcess(!UsesExternalClock);
        _toteCapController?.SetPhysicsProcess(!UsesExternalClock);
        FreezeToteFinishingAdapters();
    }

    private void PauseToteTransfer()
    {
        if (!HasToteTransfer || !UsesExternalClock || _toteTransfer is null) return;
        _toteTransfer.Pause(); _toteTransferDrive!.ApplyPlantTravel(0, 0);
        if (_toteFill is not null) { _toteFill.Pause(); ProjectToteFill(); }
        if (_toteCap is not null) { _toteCap.Pause(); ProjectToteCap(); }
        PauseToteFinishingProcess();
    }

    private void AdvanceToteTransfer(double seconds)
    {
        var old = _toteTransfer!.State.Position;
        var travelRequested = AsBool(_points.GetValueOrDefault("conveyor_run"));
        // Authored simulator interlock: do not slide the neck under an extended
        // cap chuck. Retain the PLC request and publish the effective inhibition.
        var actuatorExtended = (_toteCap is not null && _toteCap.State.Extension > 0)
            || (_toteFinishingProcess is not null && _toteFinishingProcess.State.Extension > 0);
        SetPoint("tote_transfer_inhibited", travelRequested && actuatorExtended);
        _toteTransfer.Advance(seconds, travelRequested && !actuatorExtended);
        var state = _toteTransfer.State;
        _transferTote!.Position = new Vector3((float)state.Position, _transferTote.Position.Y, _transferTote.Position.Z);
        _toteTransferDrive!.ApplyPlantTravel((float)(state.Position - old), (float)state.Speed);
        ProjectToteTransfer(); ApplyBindings(); AdvanceToteFill(seconds); AdvanceToteCap(seconds);
        AdvanceToteFinishingProcess(seconds); StateChanged?.Invoke();
    }

    private void ProjectToteTransfer()
    {
        SetPoint("tote_position", _toteTransfer!.State.Position);
        SetPoint("tote_at_exit", _toteTransfer.State.AtExit);
        foreach (var (id, point) in new[] {
            ("finishing_fill_valve", "tote_at_fill"), ("capper", "tote_at_cap"),
            ("labeler", "tote_at_label"), ("vision_inspector", "tote_at_inspection") })
            SetPoint(point, Math.Abs(_toteTransfer.State.Position - _sceneRoot.GetNode<Node3D>(id).Position.X) <= .025);
    }
}
