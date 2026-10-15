using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasBarrelFillPlant => _definition.TryGetProperty("barrelFillPlant", out _);
    private BarrelFillPlantModel? _barrelPlant;
    private Node3D? _barrelLoad, _barrelLiquid, _barrelSourceLiquid, _barrelStream, _barrelValvePointer;
    private ConveyorController? _barrelConveyor;

    private void ResetBarrelFillPlant()
    {
        if (!HasBarrelFillPlant) return;
        if (Text(_definition.GetProperty("barrelFillPlant"), "model", string.Empty) != "single-barrel-metered-fill-v1")
            throw new InvalidOperationException("Unknown barrel fill plant model.");
        foreach (var point in new[] { "barrel_at_fill", "fill_complete", "downstream_clear", "barrel_parked", "fill_beam_blocked", "exit_beam_blocked", "fill_fault" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Barrel plant requires PC-owned BOOL feedback '{point}'.");
        foreach (var point in new[] { "barrel_litres", "source_litres", "flow_lps" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "REAL")
                throw new InvalidOperationException($"Barrel plant requires PC-owned REAL feedback '{point}'.");
        foreach (var point in new[] { "infeed_run", "fill_valve_open" })
            if (_pointOwners.GetValueOrDefault(point) != "PLC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Barrel plant requires PLC-owned BOOL command '{point}'.");
        _barrelPlant ??= new BarrelFillPlantModel(); _barrelPlant.Reset();
        _barrelLoad = _sceneRoot.GetNode<Node3D>("training_accessory_5");
        _barrelLiquid = _barrelLoad.GetNode<Node3D>("KIN_barrel_liquid");
        _barrelSourceLiquid = _sceneRoot.GetNode<Node3D>("tank_1").GetNode<Node3D>("KIN_supply_liquid");
        _barrelStream = _sceneRoot.GetNode<Node3D>("training_accessory_7").GetNode<Node3D>("KIN_fill_stream");
        _barrelValvePointer = _sceneRoot.GetNode<Node3D>("valve_2").GetNode<Node3D>("KIN_valve_pointer");
        _barrelConveyor = Controllers(_sceneRoot.GetNode<Node3D>("conveyor_0")).OfType<ConveyorController>().Single();
        _barrelConveyor.ResetPlantTravel(); FreezeBarrelAdapters(); ProjectBarrelPlant();
    }

    private void FreezeBarrelAdapters() => _barrelConveyor?.SetPhysicsProcess(false);
    private void PauseBarrelClock()
    {
        _barrelConveyor?.ApplyPlantTravel(0, 0);
        if (_barrelPlant is not null) { _barrelPlant.Pause(); ProjectBarrelPlant(); ApplyBindings(); }
    }

    private void AdvanceBarrelFillPlant(double seconds)
    {
        if (_barrelPlant is null) return;
        var before = _barrelPlant.BeltDistance; var wasFaulted = _barrelPlant.Faulted;
        _barrelPlant.Step(seconds, AsBool(_points["infeed_run"]), AsBool(_points["fill_valve_open"]));
        var travel = (float)(_barrelPlant.BeltDistance - before);
        _barrelConveyor!.ApplyPlantTravel(travel, seconds > 0 ? travel / (float)seconds : 0);
        ProjectBarrelPlant(); ApplyBindings(); StateChanged?.Invoke();
        if (_barrelPlant.Faulted && !wasFaulted) GD.Print($"BARREL_FILL_FAULT {_barrelPlant.FaultReason}");
    }

    private void ProjectBarrelPlant()
    {
        if (_barrelPlant is null) return;
        _barrelLoad!.Position = new((float)_barrelPlant.X, (float)BarrelFillPlantModel.DeckY, 0);
        // Cylinder height is .8 m. Keep the bottom fixed when changing volume.
        void Liquid(Node3D mesh, double height, float bottom)
        {
            mesh.Visible = height > 1e-8;
            mesh.Scale = new(1, (float)Math.Max(height, .0001) / .8f, 1);
            mesh.Position = new(0, bottom + (float)height / 2, 0);
        }
        Liquid(_barrelLiquid!, _barrelPlant.LiquidHeight, .025f);
        Liquid(_barrelSourceLiquid!, _barrelPlant.SourceHeight, (float)BarrelFillPlantModel.SourceBottom);
        _barrelStream!.Visible = _barrelPlant.FlowLitresPerSecond > 0;
        // Stream ends at the actual liquid surface rather than through the floor.
        var surface = (float)(BarrelFillPlantModel.LiquidBottom + _barrelPlant.LiquidHeight);
        _barrelStream.Scale = new(1, (2.1f - surface) / 1.18f, 1);
        _barrelStream.Position = new(0, (2.1f + surface) / 2, 0);
        _barrelValvePointer!.Rotation = new(0, AsBool(_points["fill_valve_open"]) ? 0 : Mathf.Pi / 2, 0);
        SetPoint("barrel_at_fill", _barrelPlant.AtFill); SetPoint("fill_complete", _barrelPlant.FillComplete);
        SetPoint("downstream_clear", _barrelPlant.DownstreamClear); SetPoint("barrel_parked", _barrelPlant.Complete);
        SetPoint("fill_beam_blocked", _barrelPlant.FillBeamBlocked); SetPoint("exit_beam_blocked", _barrelPlant.ExitBeamBlocked);
        SetPoint("fill_fault", _barrelPlant.Faulted); SetPoint("barrel_litres", _barrelPlant.Litres);
        SetPoint("source_litres", _barrelPlant.SourceLitres); SetPoint("flow_lps", _barrelPlant.FlowLitresPerSecond);
    }
}
