using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasMobileTrafficPlant => _definition.TryGetProperty("mobileTrafficPlant", out _);
    public MobileTrafficPlantModel? MobileTrafficPlant { get; private set; }
    private Node3D[] _trafficVehicles = [];
    private void ResetMobileTrafficPlant()
    {
        if (!HasMobileTrafficPlant) return;
        if (Text(_definition.GetProperty("mobileTrafficPlant"), "model", "") != "two-direction-training-v1")
            throw new InvalidOperationException("Unknown mobile traffic training model.");
        foreach (var name in new[] { "controller_ready", "road_a_clear", "road_b_clear", "road_a_occupied", "road_b_occupied", "crossing_clear", "signal_conflict" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException($"Mobile traffic requires PC BOOL '{name}'.");
        foreach (var name in new[] { "road_a_green", "road_b_green", "road_a_amber", "road_b_amber", "road_a_red", "road_b_red" })
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException($"Mobile traffic requires PLC BOOL '{name}'.");
        _trafficVehicles = [_sceneRoot.GetNode<Node3D>("traffic_vehicle_a"), _sceneRoot.GetNode<Node3D>("traffic_vehicle_b")];
        MobileTrafficPlant ??= new(); MobileTrafficPlant.Reset(); ProjectMobileTrafficPlant();
    }
    private bool MobileTrafficActionAvailable(string kind) => kind switch
    {
        "trafficRequestA" => MobileTrafficPlant?.CanRequest(0) == true,
        "trafficRequestB" => MobileTrafficPlant?.CanRequest(1) == true,
        _ => true,
    };
    private bool MobileTrafficAction(string kind)
    {
        var accepted = MobileTrafficPlant?.Request(kind == "trafficRequestA" ? 0 : 1) == true;
        if (accepted) { ProjectMobileTrafficPlant(); ApplyBindings(); StateChanged?.Invoke(); }
        return accepted;
    }
    private void AdvanceMobileTrafficPlant(double seconds)
    {
        MobileTrafficPlant!.Step(seconds, AsBool(_points["controller_ready"]), AsBool(_points["road_a_clear"]), AsBool(_points["road_b_clear"]),
            AsBool(_points["road_a_green"]), AsBool(_points["road_b_green"]));
        ProjectMobileTrafficPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectMobileTrafficPlant()
    {
        var plant = MobileTrafficPlant!;
        for (var i = 0; i < 2; i++)
        {
            _trafficVehicles[i].Visible = plant.Vehicles[i].Visible;
            _trafficVehicles[i].Position = new Vector3((float)plant.X(i), .10f, i == 0 ? (float)MobileTrafficPlantModel.LaneZ : -(float)MobileTrafficPlantModel.LaneZ);
        }
        SetPoint("road_a_occupied", plant.Occupied(0)); SetPoint("road_b_occupied", plant.Occupied(1));
        SetPoint("crossing_clear", plant.CrossingClear); SetPoint("signal_conflict", plant.SignalConflict);
        SetPoint("road_a_position_m", plant.X(0)); SetPoint("road_b_position_m", plant.X(1));
    }
}
