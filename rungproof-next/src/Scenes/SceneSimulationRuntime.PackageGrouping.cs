using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasPackageGroupingPlant => _definition.TryGetProperty("packageGroupingPlant", out _);
    public PackageGroupingPlantModel? PackageGroupingPlant { get; private set; }
    private Node3D[] _groupCartons = [], _groupRollers = [];
    private Node3D? _groupGate, _groupRod;
    private Transform3D _groupGateHome, _groupRodHome;
    private Transform3D[] _groupRollerHomes = [];

    private void ResetPackageGroupingPlant()
    {
        if (!HasPackageGroupingPlant) return;
        if (Text(_definition.GetProperty("packageGroupingPlant"), "model", "") != "three-carton-accumulation-v1")
            throw new InvalidOperationException("Unknown package-grouping plant.");
        void Require(string name, string owner, string type)
        {
            if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                throw new InvalidOperationException($"Grouping requires {owner}-owned {type} '{name}'.");
        }
        foreach (var name in new[] { "machine_enabled", "release_clear", "package_detected", "group_staged", "stop_raised", "stop_lowered", "receiver_occupied", "receiver_detected", "transfer_complete" }) Require(name, "PC", "BOOL");
        Require("stop_position", "PC", "REAL");
        foreach (var name in new[] { "group_conveyor_run", "group_release", "group_ready" }) Require(name, "PLC", "BOOL");
        Require("group_count", "PLC", "DINT");
        // Capture authored homes once: subsequent application Reset restores them
        // rather than capturing a partially moved gate or accumulated roller angle.
        if (_groupCartons.Length == 0)
        {
            _groupCartons = Enumerable.Range(0, 3).Select(i => _sceneRoot.GetNode<Node3D>($"carton_{i}")).ToArray();
            var stop = _sceneRoot.GetNode<Node3D>("group_stop");
            _groupGate = (Node3D)stop.FindChild("KIN_group_gate", true, false);
            _groupRod = (Node3D)stop.FindChild("STOP_telescoping_rod", true, false);
            _groupGateHome = _groupGate.Transform; _groupRodHome = _groupRod.Transform;
            _groupRollers = _sceneRoot.GetNode<Node3D>("group_line").FindChildren("KIN_group_roller*", "Node3D", true, false).OfType<Node3D>().ToArray();
            _groupRollerHomes = _groupRollers.Select(r => r.Transform).ToArray();
        }
        PackageGroupingPlant ??= new(); PackageGroupingPlant.Reset(); ProjectPackageGroupingPlant();
    }
    private bool LoadGroupingCarton()
    {
        if (PackageGroupingPlant?.Load() != true) return false;
        ProjectPackageGroupingPlant(); ApplyBindings(); StateChanged?.Invoke(); return true;
    }
    private void AdvancePackageGroupingPlant(double seconds)
    {
        PackageGroupingPlant!.Step(seconds, AsBool(_points["machine_enabled"]), AsBool(_points["release_clear"]),
            AsBool(_points["group_conveyor_run"]), AsBool(_points["group_release"]));
        ProjectPackageGroupingPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectPackageGroupingPlant()
    {
        var p = PackageGroupingPlant!;
        for (var i = 0; i < _groupCartons.Length; i++)
        {
            _groupCartons[i].Visible = i < p.Loaded;
            _groupCartons[i].Position = new((float)p.X[i], .9f, 0);
        }
        _groupGate!.Transform = _groupGateHome.Translated(Vector3.Up * (float)p.GateFraction);
        var rod = _groupRodHome;
        rod.Basis = Basis.FromScale(new Vector3(1, (float)((1.31 - p.GateFraction) / 1.31), 1)) * rod.Basis;
        rod.Origin += Vector3.Up * (float)(p.GateFraction / 2); _groupRod!.Transform = rod;
        for (var i = 0; i < _groupRollers.Length; i++)
        {
            _groupRollers[i].Transform = _groupRollerHomes[i];
            _groupRollers[i].Rotate(Vector3.Back, (float)(-p.RollerTravel / .08));
        }
        SetPoint("package_detected", p.PackageDetected); SetPoint("group_staged", p.GroupStaged);
        SetPoint("stop_raised", p.GateFraction >= 1 - 1e-9); SetPoint("stop_lowered", p.GateFraction <= 1e-9);
        SetPoint("stop_position", p.GateFraction * 100); SetPoint("receiver_occupied", p.ReceiverOccupied);
        SetPoint("receiver_detected", p.ReceiverDetected);
        SetPoint("transfer_complete", p.Complete);
    }
}
