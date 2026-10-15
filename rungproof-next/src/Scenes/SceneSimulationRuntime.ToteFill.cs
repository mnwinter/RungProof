using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private ToteFillPlantModel? _toteFill;
    private MeshInstance3D? _toteFillNeck, _toteFillTip, _toteFillCap, _toteFillWitness;
    private Node3D? _toteFillStream;
    private RungProof.Next.App.EquipmentMotionController? _toteFillNozzleController;
    private Transform3D _toteFillWitnessHome;
    private float _toteFillWitnessBottom;

    private void ResetToteFill()
    {
        _toteFill = new ToteFillPlantModel(Number(_definition, "fillRateFractionPerSecond", .2));
        _toteFillNeck = (MeshInstance3D)_transferTote!.FindChild("IBC_open_fill_neck", true, false);
        _toteFillCap = (MeshInstance3D)_transferTote.FindChild("IBC_fill_cap", true, false);
        var witness = (MeshInstance3D)_transferTote.FindChild("IBC_fill_witness", true, false);
        if (_toteFillWitness != witness)
        {
            _toteFillWitnessHome = witness.Transform;
            _toteFillWitnessBottom = (_toteFillWitnessHome * witness.GetAabb()).Position.Y;
        }
        _toteFillWitness = witness;
        var station = _sceneRoot.GetNode<Node3D>("finishing_fill_valve");
        _toteFillTip = (MeshInstance3D)station.FindChild("NOZZLE_TIP", true, false);
        _toteFillStream = station.FindChild("FINISHING_stream", true, false) as Node3D;
        _toteFillNozzleController = station.FindChildren("*", string.Empty, true, false)
            .OfType<RungProof.Next.App.EquipmentMotionController>().Single();
        _toteFillCap.Visible = false;
        ProjectToteFill();
    }

    private bool ToteFillEligible()
    {
        if (_toteFill is null || _toteFillCap!.Visible || AsBool(_points.GetValueOrDefault("conveyor_run"))
            || Math.Abs(_transferTote!.Position.X - _sceneRoot.GetNode<Node3D>("finishing_fill_valve").Position.X) > .025)
            return false;
        var neckBounds = _toteFillNeck!.GlobalTransform * _toteFillNeck.GetAabb();
        var tipBounds = _toteFillTip!.GlobalTransform * _toteFillTip.GetAabb();
        // Require the delivered tip outlet to enter the neck's vertical range.
        if (tipBounds.Position.Y > neckBounds.End.Y - .002f || tipBounds.Position.Y < neckBounds.Position.Y)
            return false;
        var centre = neckBounds.GetCenter();
        var vertices = _toteFillNeck.Mesh.GetFaces().Select(v => _toteFillNeck.GlobalTransform * v).ToArray();
        var x = vertices.Where(v => MathF.Abs(v.Z-centre.Z)<.00001f && MathF.Abs(v.X-centre.X)>.00001f)
            .Select(v => MathF.Abs(v.X-centre.X)).ToArray();
        var z = vertices.Where(v => MathF.Abs(v.X-centre.X)<.00001f && MathF.Abs(v.Z-centre.Z)>.00001f)
            .Select(v => MathF.Abs(v.Z-centre.Z)).ToArray();
        if (x.Length == 0 || z.Length == 0) return false;
        var radiusX = x.Min()-.002f; var radiusZ = z.Min()-.002f;
        return radiusX > 0 && radiusZ > 0 && _toteFillTip.Mesh.GetFaces().All(v =>
        {
            var world = _toteFillTip.GlobalTransform * v;
            var dx = (world.X-centre.X)/radiusX; var dz = (world.Z-centre.Z)/radiusZ;
            return dx*dx+dz*dz < 1;
        });
    }

    private void AdvanceToteFill(double seconds)
    {
        _toteFill!.Advance(seconds, AsBool(_points.GetValueOrDefault("fill_valve_open")), ToteFillEligible());
        ProjectToteFill();
    }

    private void ProjectToteFill()
    {
        var state = _toteFill!.State;
        SetPoint("fill_percent", state.Fraction*100);
        SetPoint("actual_fill_percent_per_second", state.FlowFractionPerSecond*100);
        SetPoint("fill_complete", state.Complete);
        SetPoint("fill_inhibited", state.Inhibited);
        _toteFillWitness!.Visible = state.Fraction > 0;
        // Preserve the authored witness bottom while changing its height.
        // It is a level illustration inside the cavity, not a calibrated volume.
        var fraction = (float)Math.Max(.0001, state.Fraction);
        var scale = Basis.FromScale(new Vector3(1, fraction, 1));
        _toteFillWitness.Transform = new Transform3D(scale * _toteFillWitnessHome.Basis,
            new Vector3(_toteFillWitnessHome.Origin.X,
                _toteFillWitnessBottom + fraction*(_toteFillWitnessHome.Origin.Y-_toteFillWitnessBottom),
                _toteFillWitnessHome.Origin.Z));
        ProjectToteFillVisibility();
    }

    private void ProjectToteFillVisibility()
    {
        if (!HasToteTransfer || !UsesExternalClock || _toteFillStream is null || _toteFill is null) return;
        _toteFillStream.Visible = PlantPlaybackRunning && _toteFill.State.FlowFractionPerSecond > 0
            && AsBool(_points.GetValueOrDefault("fill_valve_open")) && ToteFillEligible();
    }
}
