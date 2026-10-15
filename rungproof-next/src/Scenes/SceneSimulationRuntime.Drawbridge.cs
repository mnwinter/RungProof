using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasDrawbridgePlant => RuntimeType == "drawbridge";
    public DrawbridgePlantModel? DrawbridgePlant { get; private set; }
    private Node3D? _drawbridgeDeck, _drawbridgeCam;
    private Node3D[] _drawbridgeGates = [];

    private void ResetDrawbridgePlant()
    {
        if (!HasDrawbridgePlant) return;
        void Require(string point, string owner, string type)
        {
            if (_pointOwners.GetValueOrDefault(point) != owner || _pointTypes.GetValueOrDefault(point) != type)
                throw new InvalidOperationException($"Drawbridge requires {owner} {type} {point}.");
        }
        foreach (var input in new[] { "traffic_stopped", "bridge_request", "bridge_home", "bridge_raised", "barriers_closed", "barriers_open", "motion_inhibited" }) Require(input, "PC", "BOOL");
        foreach (var input in new[] { "bridge_angle", "barrier_angle" }) Require(input, "PC", "REAL");
        foreach (var output in new[] { "bridge_raise", "bridge_lower", "barrier_close", "barrier_open", "traffic_release", "traffic_stop" }) Require(output, "PLC", "BOOL");
        _drawbridgeDeck = _sceneRoot.GetNode<Node3D>("training_accessory_3").GetNode<Node3D>("BridgeMotionPivot");
        _drawbridgeCam = _sceneRoot.GetNode<Node3D>("training_accessory_5").GetNode<Node3D>("BridgeMotionPivot");
        _drawbridgeGates = [_sceneRoot.GetNode<Node3D>("training_accessory_4").GetNode<Node3D>("BridgeMotionPivot"),
            _sceneRoot.GetNode<Node3D>("barrier_east").GetNode<Node3D>("BridgeMotionPivot")];
        DrawbridgePlant ??= new(); DrawbridgePlant.Reset(); ProjectDrawbridgePlant();
    }

    private void AdvanceDrawbridgePlant(double seconds)
    {
        DrawbridgePlant!.Step(seconds, AsBool(_points["traffic_stopped"]), AsBool(_points["bridge_raise"]), AsBool(_points["bridge_lower"]),
            AsBool(_points["barrier_close"]), AsBool(_points["barrier_open"]), AsBool(_points["traffic_release"]));
        ProjectDrawbridgePlant(); ApplyBindings(); StateChanged?.Invoke();
    }

    private void ProjectDrawbridgePlant()
    {
        var plant = DrawbridgePlant!;
        _drawbridgeDeck!.Rotation = _drawbridgeCam!.Rotation = new Vector3(0, 0, Mathf.DegToRad((float)plant.BridgeDegrees));
        foreach (var gate in _drawbridgeGates) gate.Rotation = new Vector3(0, 0, Mathf.DegToRad((float)plant.BarrierDegrees));
        SetPoint("bridge_home", plant.Home); SetPoint("bridge_raised", plant.Raised);
        SetPoint("barriers_closed", plant.GatesClosed); SetPoint("barriers_open", plant.GatesOpen);
        SetPoint("bridge_angle", plant.BridgeDegrees); SetPoint("barrier_angle", plant.BarrierDegrees);
        SetPoint("motion_inhibited", plant.MotionInhibited);
    }
}
