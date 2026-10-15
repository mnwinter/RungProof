using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasHandDryer => RuntimeType == "handDryer";
    private Node3D? _dryerHands;
    private MeshInstance3D? _dryerLens, _dryerAir;
    private MeshInstance3D[] _dryerHandMeshes = [], _dryerElements = [];
    private EquipmentMotionController? _dryerFan;
    private StandardMaterial3D? _dryerHot, _dryerCold;

    private void ResetHandDryer()
    {
        if (!HasHandDryer) return;
        foreach (var (n, owner, type) in new[] {
            ("hands_inserted", "PC", "BOOL"), ("hands_present", "PC", "BOOL"),
            ("heater_inhibited", "PC", "BOOL"), ("blower_run", "PLC", "BOOL"),
            ("heater_enable", "PLC", "BOOL"), ("remaining_seconds", "PLC", "REAL") })
            if (_pointOwners.GetValueOrDefault(n) != owner || _pointTypes.GetValueOrDefault(n) != type)
                throw new InvalidOperationException($"Hand dryer requires {owner} {type} {n}.");
        var station = _sceneRoot.GetNode<Node3D>("training_accessory_3");
        _dryerHands = station.GetNode<Node3D>("HandPair");
        _dryerHandMeshes = _dryerHands.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        _dryerLens = (MeshInstance3D)station.FindChild("HAND_SENSOR_lens", true, false);
        _dryerAir = _sceneRoot.GetNode<MeshInstance3D>("DryerAirCommand");
        _dryerFan = _sceneRoot.GetNode<Node3D>("fan_0").FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
        _dryerElements = _sceneRoot.GetNode<Node3D>("training_accessory_5").FindChildren("*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>().Where(m => m.Name.ToString().StartsWith("HEATER_element_") || m.Name.ToString().StartsWith("HEATER_return_")).ToArray();
        _dryerCold ??= new StandardMaterial3D { AlbedoColor = new Color(.55f,.58f,.6f), Metallic = .7f, Roughness = .35f };
        _dryerHot ??= new StandardMaterial3D { AlbedoColor = new Color(.9f,.3f,.06f), EmissionEnabled = true, Emission = new Color(.7f,.12f,.01f) };
        FreezeHandDryerAdapter(); ProjectDryerHands(); ProjectDryerCommands(false);
    }
    private void FreezeHandDryerAdapter() => _dryerFan?.SetPhysicsProcess(false);
    private void PauseHandDryer()
    {
        if (_dryerFan is null) return;
        ProjectDryerCommands(false); ApplyBindings(); StateChanged?.Invoke();
    }
    private void AdvanceHandDryer(double seconds)
    {
        ProjectDryerHands(); ProjectDryerCommands(true);
        _dryerFan!._PhysicsProcess(seconds); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectDryerCommands(bool eligibleClock)
    {
        var hands = AsBool(_points["hands_present"]);
        var blower = eligibleClock && hands && AsBool(_points["blower_run"]);
        var heater = blower && AsBool(_points["heater_enable"]);
        // Display actuator eligibility without modifying the PLC-owned image.
        // No measured temperature or airflow feedback is fabricated here.
        _dryerFan!.RunCommand = blower; _dryerAir!.Visible = blower;
        foreach (var mesh in _dryerElements) mesh.MaterialOverride = heater ? _dryerHot : _dryerCold;
        SetPoint("heater_inhibited", AsBool(_points["heater_enable"]) && !heater);
    }
    private void ProjectDryerHands()
    {
        if (_dryerHands is null) return;
        _dryerHands.Visible = AsBool(_points["hands_inserted"]);
        var from = _dryerLens!.GlobalTransform * _dryerLens.GetAabb().GetCenter();
        var to = from + _sceneRoot.GetNode<Node3D>("training_accessory_3").GlobalBasis * new Vector3(-.8f,0,0);
        SetPoint("hands_present", _dryerHands.Visible && _dryerHandMeshes.Any(mesh => DryerBeamHits(mesh, from, to)));
    }
    private static bool DryerBeamHits(MeshInstance3D mesh, Vector3 from, Vector3 to)
    {
        var inverse = mesh.GlobalTransform.AffineInverse(); from = inverse * from; to = inverse * to;
        var direction = to-from; var faces = mesh.Mesh.GetFaces();
        for (var i=0;i<faces.Length;i+=3) {
            var e1=faces[i+1]-faces[i]; var e2=faces[i+2]-faces[i];
            var cross=direction.Cross(e2); var determinant=e1.Dot(cross); if(MathF.Abs(determinant)<1e-8f)continue;
            var offset=from-faces[i]; var u=offset.Dot(cross)/determinant; if(u < -1e-6f || u > 1.000001f)continue;
            var q=offset.Cross(e1); var v=direction.Dot(q)/determinant; if(v < -1e-6f || u+v > 1.000001f)continue;
            var t=e2.Dot(q)/determinant; if(t>=0 && t<=1)return true;
        }
        return false;
    }
}
