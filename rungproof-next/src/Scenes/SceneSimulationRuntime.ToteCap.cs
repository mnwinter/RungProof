using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private ToteCapPlantModel? _toteCap;
    private Node3D? _toteCapSpindle;
    private MeshInstance3D? _toteHeldCap;
    private EquipmentMotionController? _toteCapController;
    private Transform3D _toteCapHome;
    private float _toteCapStroke, _toteHeldInnerRadius;

    private void ResetToteCap()
    {
        var station = _sceneRoot.GetNode<Node3D>("capper");
        var spindle = (Node3D)station.FindChild("KIN_capper_spindle", true, false);
        if (_toteCapSpindle != spindle) _toteCapHome = spindle.Transform;
        _toteCapSpindle = spindle;
        spindle.Transform = _toteCapHome;
        _toteHeldCap = (MeshInstance3D)station.FindChild("CAP_UNDER_CHUCK", true, false);
        _toteCapController = station.FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>().Single();
        _toteCap = new ToteCapPlantModel(Number(_definition, "capTravelSeconds", .35),
            Number(_definition, "capSeatingSeconds", .8), Number(_definition, "capSeatingRpm", 180) / 60);
        var held = _toteHeldCap.GlobalTransform * _toteHeldCap.GetAabb();
        var seated = _toteFillCap!.GlobalTransform * _toteFillCap.GetAabb();
        _toteCapStroke = held.End.Y - seated.End.Y;
        if (!float.IsFinite(_toteCapStroke) || _toteCapStroke <= 0)
            throw new InvalidOperationException("Cap installation needs positive measured axial travel.");
        var center = held.GetCenter();
        _toteHeldInnerRadius = _toteHeldCap.Mesh.GetFaces()
            .Select(v => _toteHeldCap.GlobalTransform * v)
            .Where(v => v.Y < held.Position.Y + held.Size.Y * .35f)
            .Min(v => new Vector2(v.X-center.X, v.Z-center.Z).Length());
        _toteHeldCap.Visible = true;
        ProjectToteCap();
    }

    private bool ToteCapEligible()
    {
        if (_toteCap is null || _toteFillCap!.Visible || !_toteFill!.State.Complete
            || AsBool(_points.GetValueOrDefault("conveyor_run"))) return false;
        var held = _toteHeldCap!.GlobalTransform * _toteHeldCap.GetAabb();
        var center = held.GetCenter();
        // Actual radial fit rejects an off-axis tote even inside the broader
        // station-presence window. No requested command is rewritten.
        return _toteFillNeck!.Mesh.GetFaces().All(vertex =>
        {
            var world = _toteFillNeck.GlobalTransform * vertex;
            return new Vector2(world.X-center.X, world.Z-center.Z).Length() < _toteHeldInnerRadius - .001f;
        });
    }

    private void AdvanceToteCap(double seconds)
    {
        _toteCap!.Advance(seconds, AsBool(_points.GetValueOrDefault("capper_run")), ToteCapEligible());
        ProjectToteCap();
    }

    private void ProjectToteCap()
    {
        var state = _toteCap!.State;
        var parent = (Node3D)_toteCapSpindle!.GetParent();
        var displacement = parent.GlobalBasis.Inverse() * (Vector3.Down * _toteCapStroke * (float)state.Extension);
        var axis = (parent.GlobalBasis.Inverse() * Vector3.Up).Normalized();
        var rotation = new Basis(axis, (float)(state.Turns * Math.Tau));
        _toteCapSpindle.Transform = new Transform3D(rotation * _toteCapHome.Basis, _toteCapHome.Origin + displacement);
        _toteHeldCap!.Visible = !state.Applied;
        // Preserve independent visible-cap fault fixtures before application.
        // Once released, the delivered tote cap travels with its tote parent.
        if (state.Applied) _toteFillCap!.Visible = true;
        SetPoint("cap_extension_percent", state.Extension * 100);
        SetPoint("cap_applied", state.Applied);
        SetPoint("cap_busy", state.Stage != ToteCapPlantModel.Phase.Home);
        SetPoint("cap_inhibited", state.Inhibited);
        SetPoint("cap_home", state.Stage == ToteCapPlantModel.Phase.Home);
    }
}
