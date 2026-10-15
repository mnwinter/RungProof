using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasCableCutPlant => _definition.TryGetProperty("cableCutPlant", out _);
    private CableCutPlantModel? _cablePlant;
    private Node3D? _cutPiece, _cutBlade, _payoff;
    private void ResetCableCutPlant()
    {
        if (!HasCableCutPlant) return;
        if (Text(_definition.GetProperty("cableCutPlant"), "model", "") != "single-three-metre-cut-v1")
            throw new InvalidOperationException("Unknown cable cut plant model.");
        foreach (var point in new[] { "cable_present", "length_reached", "cutter_home", "cut_done", "cut_complete", "cut_fault" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Cable plant requires PC-owned BOOL '{point}'.");
        foreach (var point in new[] { "measured_length_m", "stock_remaining_m", "feed_speed_mps", "cutter_position" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "REAL")
                throw new InvalidOperationException($"Cable plant requires PC-owned REAL '{point}'.");
        foreach (var point in new[] { "feed_run", "cutter_fire", "cycle_active" })
            if (_pointOwners.GetValueOrDefault(point) != "PLC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Cable plant requires PLC-owned BOOL '{point}'.");
        _cablePlant ??= new(); _cablePlant.Reset();
        _cutPiece = _sceneRoot.GetNode<Node3D>("cable_strand/KIN_cable_piece");
        _cutBlade = _sceneRoot.GetNode<Node3D>("training_accessory_7/KIN_cutter_blade");
        _payoff = _sceneRoot.GetNode<Node3D>("training_accessory_4/KIN_payoff");
        ProjectCablePlant();
    }
    private void PauseCableClock()
    { if (_cablePlant is not null) { _cablePlant.Pause(); ProjectCablePlant(); ApplyBindings(); } }
    private void AdvanceCableCutPlant(double seconds)
    {
        if (_cablePlant is null) return;
        var fault = _cablePlant.Faulted;
        _cablePlant.Step(seconds, AsBool(_points["cycle_active"]), AsBool(_points["feed_run"]), AsBool(_points["cutter_fire"]));
        ProjectCablePlant(); ApplyBindings(); StateChanged?.Invoke();
        if (_cablePlant.Faulted && !fault) GD.Print($"CABLE_CUT_FAULT {_cablePlant.FaultReason}");
    }
    private void ProjectCablePlant()
    {
        if (_cablePlant is null) return;
        var length = (float)_cablePlant.LengthM;
        _cutPiece!.Visible = length > 1e-8;
        // Cylinder's local Y is length, rotated onto world X. Keep its
        // trailing end at the cutting plane until a cut has been observed.
        _cutPiece.Scale = new(1, Math.Max(length, .0001f), 1);
        _cutPiece.Position = new(length / 2 + (_cablePlant.CutDone ? .06f : 0), 1.1f, 0);
        _cutBlade!.Position = new(0, (float)(-_cablePlant.Stroke * CableCutPlantModel.StrokeM), 0);
        _payoff!.Rotation = new(0, 0, (float)(-_cablePlant.LengthM / .35));
        foreach (var (id, radius) in new[] { ("training_accessory_5", .12), ("training_accessory_6", .08), ("machine_1", .12) })
        {
            var owner = _sceneRoot.GetNode<Node3D>(id);
            owner.GetNode<Node3D>("KIN_lower_roll").Rotation = new(0, 0, (float)(-_cablePlant.LengthM / radius));
            owner.GetNode<Node3D>("KIN_upper_roll").Rotation = new(0, 0, (float)(_cablePlant.LengthM / radius));
        }
        SetPoint("cable_present", true); SetPoint("length_reached", _cablePlant.LengthReached);
        SetPoint("cutter_home", _cablePlant.Home); SetPoint("cut_done", _cablePlant.CutDone);
        SetPoint("cut_complete", _cablePlant.Complete); SetPoint("cut_fault", _cablePlant.Faulted);
        SetPoint("measured_length_m", _cablePlant.LengthM); SetPoint("stock_remaining_m", _cablePlant.StockM);
        SetPoint("feed_speed_mps", _cablePlant.FeedRate); SetPoint("cutter_position", _cablePlant.Stroke);
    }
}
