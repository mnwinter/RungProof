using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasChainLiftPlant => _definition.TryGetProperty("chainLiftPlant", out _);
    private ChainLiftPlantModel? _chainLiftPlant;
    private Node3D? _chainLiftLoad;
    private ChainLiftDriveVisual? _chainLiftDrive;
    private ChainFeedMotion? _chainLiftFeed;
    private ConveyorController? _chainLiftReceiver, _chainLiftBelt;

    private void ResetChainLiftPlant()
    {
        if (!HasChainLiftPlant) return;
        var config = _definition.GetProperty("chainLiftPlant");
        if (Text(config, "model", string.Empty) != "single-carton-guided-lift-v1")
            throw new InvalidOperationException("Unknown chain lift plant model.");
        _chainLiftPlant ??= new ChainLiftPlantModel();
        _chainLiftLoad = _sceneRoot.GetNode<Node3D>("box_2");
        _chainLiftDrive = _sceneRoot.GetNode<Node3D>("liftTable_1").GetNode<ChainLiftDriveVisual>("ChainLiftMotion");
        _chainLiftFeed = Controllers(_sceneRoot.GetNode<Node3D>("conveyor_0")).OfType<ChainFeedMotion>().Single();
        _chainLiftReceiver = Controllers(_sceneRoot.GetNode<Node3D>("receiving_conveyor")).OfType<ConveyorController>().Single();
        _chainLiftBelt = Controllers(_sceneRoot.GetNode<Node3D>("liftTable_1")).OfType<ConveyorController>().Single();
        foreach (var point in new[] { "box_present", "lift_home", "destination_clear", "carton_on_infeed", "carton_on_lift", "lift_upper", "carton_at_receiver", "receiver_occupied", "receiver_beam_blocked", "transfer_fault" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Chain lift requires PC-owned BOOL feedback '{point}'.");
        foreach (var point in new[] { "chain_run", "lift_enable", "lift_lower" })
            if (_pointOwners.GetValueOrDefault(point) != "PLC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Chain lift requires PLC-owned BOOL command '{point}'.");
        _chainLiftDrive.PositionFollower = null;
        _chainLiftDrive.AutonomousPositionTravel = false;
        _chainLiftPlant.Reset();
        _chainLiftReceiver.ResetPlantTravel(); _chainLiftBelt.ResetPlantTravel();
        FreezeChainLiftAdapters();
        ProjectChainLiftPlant();
    }

    private void FreezeChainLiftAdapters()
    {
        if (!HasChainLiftPlant) return;
        _chainLiftDrive?.SetPhysicsProcess(false); _chainLiftFeed?.SetPhysicsProcess(false);
        _chainLiftReceiver?.SetPhysicsProcess(false); _chainLiftBelt?.SetPhysicsProcess(false);
    }

    private void ProjectChainLiftCommands()
    {
        if (_chainLiftDrive is null) return;
        // One drive has two distinct direction commands. Keep its requested
        // running status accurate during lowering as well as raising.
        _chainLiftDrive.RunCommand = AsBool(_points["lift_enable"]) || AsBool(_points["lift_lower"]);
    }

    private void PauseChainLiftClock()
    {
        _chainLiftReceiver?.ApplyPlantTravel(0, 0);
        _chainLiftBelt?.ApplyPlantTravel(0, 0);
    }

    private void AdvanceChainLiftPlant(double seconds)
    {
        if (_chainLiftPlant is null) return;
        var oldX = _chainLiftPlant.LoadX;
        var wasFaulted = _chainLiftPlant.TransferFault;
        _chainLiftPlant.Step(seconds, AsBool(_points["chain_run"]), AsBool(_points["lift_enable"]), AsBool(_points["lift_lower"]));
        var distance = (float)(_chainLiftPlant.LoadX - oldX);
        var speed = seconds > 0 ? distance / (float)seconds : 0;
        // The same displacement drives each carrying surface. There is no
        // second independently integrated belt or carriage callback.
        _chainLiftFeed!.RunCommand = AsBool(_points["chain_run"]) && !_chainLiftPlant.TransferFault;
        _chainLiftFeed.ApplyPlantTravel(distance);
        _chainLiftReceiver!.ApplyPlantTravel(distance, speed);
        _chainLiftBelt!.ApplyPlantTravel(distance, speed);
        ProjectChainLiftPlant();
        if (_chainLiftPlant.TransferFault && !wasFaulted) GD.Print($"CHAIN_LIFT_TRANSFER_FAULT {_chainLiftPlant.FaultReason}");
        ApplyBindings(); StateChanged?.Invoke();
    }

    private void ProjectChainLiftPlant()
    {
        if (_chainLiftPlant is null) return;
        _chainLiftDrive!.SetPositionNormalized((float)(_chainLiftPlant.Height / ChainLiftPlantModel.Stroke));
        _chainLiftDrive.ProjectDrive();
        _chainLiftLoad!.Position = new Vector3((float)_chainLiftPlant.LoadX, (float)_chainLiftPlant.LoadY, 0);
        SetPoint("box_present", _chainLiftPlant.BoxPresent); SetPoint("lift_home", _chainLiftPlant.Home);
        SetPoint("carton_on_infeed", _chainLiftPlant.CartonOnInfeed);
        SetPoint("destination_clear", _chainLiftPlant.DestinationClear); SetPoint("carton_on_lift", _chainLiftPlant.CartonOnLift);
        SetPoint("lift_upper", _chainLiftPlant.Upper); SetPoint("carton_at_receiver", _chainLiftPlant.AtReceiver);
        SetPoint("receiver_occupied", _chainLiftPlant.ReceiverOccupied); SetPoint("transfer_fault", _chainLiftPlant.TransferFault);
        SetPoint("receiver_beam_blocked", _chainLiftPlant.ReceiverBeamBlocked);
    }
}
