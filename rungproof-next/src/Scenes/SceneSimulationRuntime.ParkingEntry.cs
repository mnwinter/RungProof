using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasParkingEntryPlant => _definition.TryGetProperty("parkingEntryPlant", out _);
    public ParkingEntryPlantModel? ParkingEntryPlant { get; private set; }
    private Node3D[] _parkingVehicles = [];
    private Node3D? _parkingBoom;

    private void ResetParkingEntryPlant()
    {
        if (!HasParkingEntryPlant) return;
        if (Text(_definition.GetProperty("parkingEntryPlant"), "model", "") != "two-bay-parking-v1")
            throw new InvalidOperationException("Unknown parking-entry model.");
        void Require(string name, string owner, string type)
        {
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Parking entry requires {owner}-owned {type} '{name}'.");
        }
        foreach (var name in new[] { "machine_enabled", "exit_clear", "entry_detected", "space_available", "exit_requested", "passage_occupied", "passage_detected", "entry_passed", "exit_passed", "barrier_closed", "barrier_raised" }) Require(name, "PC", "BOOL");
        Require("barrier_position", "PC", "REAL");
        foreach (var name in new[] { "barrier_open", "garage_available", "vehicle_run" }) Require(name, "PLC", "BOOL");
        Require("occupancy_count", "PLC", "DINT");
        _parkingVehicles = [_sceneRoot.GetNode<Node3D>("vehicle_0"), _sceneRoot.GetNode<Node3D>("vehicle_1")];
        _parkingBoom = _sceneRoot.GetNode<Node3D>("barrier").GetNode<Node3D>("BoomPivot");
        ParkingEntryPlant ??= new(); ParkingEntryPlant.Reset(); ProjectParkingEntryPlant();
    }
    private bool ParkingEntryAction(string kind)
    {
        var accepted = kind switch
        {
            "parkingEnter" => ParkingEntryPlant?.RequestEntry() == true,
            "parkingExit" => ParkingEntryPlant?.RequestExit() == true,
            "parkingClearDeparted" => ParkingEntryPlant?.ClearDeparted() == true,
            _ => false,
        };
        if (accepted) { ProjectParkingEntryPlant(); ApplyBindings(); StateChanged?.Invoke(); }
        return accepted;
    }
    private bool ParkingEntryActionAvailable(string kind) => kind switch
    {
        "parkingEnter" => ParkingEntryPlant?.CanEnter == true,
        "parkingExit" => ParkingEntryPlant?.CanExit == true,
        "parkingClearDeparted" => ParkingEntryPlant?.CanClearDeparted == true,
        _ => true,
    };
    private void AdvanceParkingEntryPlant(double seconds)
    {
        ParkingEntryPlant!.Step(seconds, AsBool(_points["machine_enabled"]), AsBool(_points["barrier_open"]), AsBool(_points["vehicle_run"]));
        ProjectParkingEntryPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectParkingEntryPlant()
    {
        var plant = ParkingEntryPlant!;
        for (var i = 0; i < _parkingVehicles.Length; i++)
        {
            var state = plant.Vehicles[i]; var vehicle = _parkingVehicles[i];
            vehicle.Visible = state.Visible;
            vehicle.Position = new Vector3((float)state.X, .04f, (float)state.Z);
            vehicle.Rotation = new Vector3(0, (float)state.Yaw, 0);
        }
        _parkingBoom!.Rotation = new Vector3(0, 0, (float)(plant.BoomFraction * Math.PI / 2));
        SetPoint("entry_detected", plant.EntryDetected); SetPoint("space_available", plant.SpaceAvailable);
        SetPoint("exit_requested", plant.ExitRequested); SetPoint("passage_occupied", plant.PassageOccupied);
        SetPoint("passage_detected", plant.PassageDetected);
        SetPoint("entry_passed", plant.EntryPassed); SetPoint("exit_passed", plant.ExitPassed);
        SetPoint("barrier_closed", plant.BoomFraction <= 1e-9); SetPoint("barrier_raised", plant.BoomFraction >= 1 - 1e-9);
        SetPoint("barrier_position", plant.BoomFraction * 100);
    }
}
