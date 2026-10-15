using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasCoatingPlant => RuntimeType == "coatingLine";
    public CoatingPlantModel? CoatingPlant { get; private set; }
    private Node3D? _coatingPart;
    private MeshInstance3D? _coatingBody, _coatingSpray, _coatingVane;
    private Transform3D _coatingVaneHome;
    private Vector3[] _coatingFaces = [];
    private MeshInstance3D[] _coatingTx = [], _coatingRx = [];
    private ConveyorController? _coatingConveyor;
    private EquipmentMotionController? _coatingFan;
    private void ResetCoatingPlant()
    {
        if (!HasCoatingPlant) return;
        foreach (var n in new[] { "workpiece_at_station", "workpiece_at_exit", "start_request", "spray_ready", "ventilation_ready", "motion_inhibited", "spray_inhibited", "travel_limited" })
            Require(n, "PC", "BOOL");
        foreach (var n in new[] { "workpiece_position", "belt_speed" }) Require(n, "PC", "REAL");
        foreach (var n in new[] { "index_run", "spray_enable", "vent_run" }) Require(n, "PLC", "BOOL");
        void Require(string n, string owner, string type) {
            if (_pointOwners.GetValueOrDefault(n) != owner || _pointTypes.GetValueOrDefault(n) != type)
                throw new InvalidOperationException($"Coating requires {owner} {type} {n}.");
        }
        _coatingPart = _sceneRoot.GetNode<Node3D>("training_accessory_7");
        _coatingBody = (MeshInstance3D)_coatingPart.FindChild("WORKPIECE_BODY", true, false);
        _coatingFaces = _coatingBody.Mesh.GetFaces();
        var sensors = new[] { "photoeye_3", "photoeye_exit" }.Select(id => _sceneRoot.GetNode<Node3D>(id)).ToArray();
        _coatingTx = sensors.Select(n => (MeshInstance3D)n.FindChild("TX_lens", true, false)).ToArray();
        _coatingRx = sensors.Select(n => (MeshInstance3D)n.FindChild("RX_lens", true, false)).ToArray();
        _coatingConveyor = _sceneRoot.GetNode<Node3D>("conveyor_0").FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
        _coatingFan = _sceneRoot.GetNode<Node3D>("fan_2").FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
        _coatingSpray = _sceneRoot.GetNode<MeshInstance3D>("CoatingSpray");
        if (_coatingVane is null) {
            _coatingVane = (MeshInstance3D)_sceneRoot.GetNode<Node3D>("training_accessory_8").FindChild("DAMPER_vane", true, false);
            _coatingVaneHome = _coatingVane.Transform;
        }
        CoatingPlant ??= new(Number(_definition,"minimumX",double.NaN), Number(_definition,"maximumX",double.NaN),
            _coatingPart.Position.X, Number(_definition,"speedMps",double.NaN));
        CoatingPlant.Reset(); FreezeCoatingAdapters(); _coatingConveyor.ResetPlantTravel(); ProjectCoatingPlant();
        ProjectCoatingActuators(false);
    }
    private void FreezeCoatingAdapters() { _coatingConveyor?.SetPhysicsProcess(false); _coatingFan?.SetPhysicsProcess(false); }
    private void PauseCoatingClock()
    {
        if (CoatingPlant is null) return;
        CoatingPlant.Pause(); _coatingConveyor!.ApplyPlantTravel(0,0); _coatingSpray!.Visible = false;
        SetPoint("belt_speed",0d); ProjectCoatingActuators(false); ApplyBindings(); StateChanged?.Invoke();
    }
    private void AdvanceCoatingPlant(double seconds)
    {
        var p=CoatingPlant!; var before=p.Position;
        p.Step(seconds, AsBool(_points["index_run"]), AsBool(_points["spray_enable"]), AsBool(_points["vent_run"]),
            CoatingBlocksBeam(0), AsBool(_points["spray_ready"]), AsBool(_points["ventilation_ready"]));
        ProjectCoatingPlant(); _coatingConveyor!.ApplyPlantTravel((float)(p.Position-before),(float)p.Speed);
        ProjectCoatingActuators(AsBool(_points["vent_run"]));
        _coatingFan!._PhysicsProcess(seconds); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectCoatingActuators(bool vent)
    {
        _coatingFan!.RunCommand=vent;
        // Instant illustrative command projection, not measured damper position.
        _coatingVane!.Transform = _coatingVaneHome;
        if (!vent) _coatingVane.RotateObjectLocal(Vector3.Right, Mathf.Pi/2);
    }
    private void ProjectCoatingPlant()
    {
        var p=CoatingPlant!;
        _coatingPart!.Position=new((float)p.Position,_coatingPart.Position.Y,_coatingPart.Position.Z);
        SetPoint("workpiece_at_station",CoatingBlocksBeam(0)); SetPoint("workpiece_at_exit",CoatingBlocksBeam(1));
        SetPoint("workpiece_position",p.Position); SetPoint("belt_speed",p.Speed);
        SetPoint("motion_inhibited",p.MotionInhibited); SetPoint("spray_inhibited",p.SprayInhibited); SetPoint("travel_limited",p.TravelLimited);
        _coatingSpray!.Visible=p.Spraying;
    }
    private bool CoatingBlocksBeam(int sensor)
    {
        var inverse=_coatingBody!.GlobalTransform.AffineInverse();
        var from=inverse*(_coatingTx[sensor].GlobalTransform*_coatingTx[sensor].GetAabb().GetCenter());
        var to=inverse*(_coatingRx[sensor].GlobalTransform*_coatingRx[sensor].GetAabb().GetCenter()); var direction=to-from;
        for(var i=0;i<_coatingFaces.Length;i+=3) {
            var e1=_coatingFaces[i+1]-_coatingFaces[i]; var e2=_coatingFaces[i+2]-_coatingFaces[i];
            var cross=direction.Cross(e2); var determinant=e1.Dot(cross); if(MathF.Abs(determinant)<1e-8f)continue;
            var offset=from-_coatingFaces[i]; var u=offset.Dot(cross)/determinant; if(u < -1e-6f || u > 1.000001f)continue;
            var q=offset.Cross(e1); var v=direction.Dot(q)/determinant; if(v < -1e-6f || u+v > 1.000001f)continue;
            var t=e2.Dot(q)/determinant; if(t>=0 && t<=1)return true;
        }
        return false;
    }
}
