using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private ToteFinishingProcessModel? _toteFinishingProcess;
    private MeshInstance3D? _toteAppliedLabel, _toteTampLabel, _toteCarrier;
    private Node3D? _toteLabelView;
    private readonly Dictionary<Node3D, Transform3D> _toteTampHomes = [];
    private EquipmentMotionController? _toteLabelController, _toteVisionController;
    private float _toteTampStroke;
    private bool _toteInspectionViewClear;
    private Label3D? _toteInspectionReadout;

    private void ResetToteFinishingProcess()
    {
        if (!HasToteTransfer) return;
        _toteFinishingProcess = new ToteFinishingProcessModel(Number(_definition, "labelTravelSeconds", .35),
            Number(_definition, "labelContactSeconds", .4), Number(_definition, "inspectionSeconds", .6));
        _toteInspectionViewClear = false;
        var labeler = _sceneRoot.GetNode<Node3D>("labeler");
        var vision = _sceneRoot.GetNode<Node3D>("vision_inspector");
        _toteAppliedLabel = (MeshInstance3D)_transferTote!.FindChild("FINISHING_applied_label", true, false);
        _toteCarrier = (MeshInstance3D)_transferTote.FindChild("FINISHING_label_carrier", true, false);
        _toteTampLabel = (MeshInstance3D)labeler.FindChild("LABEL_ON_TAMP", true, false);
        foreach (var name in new[] { "APPLICATOR_PAD", "LABEL_ON_TAMP", "TAMP_SLIDE" })
        {
            var part = (Node3D)labeler.FindChild(name, true, false);
            if (!_toteTampHomes.ContainsKey(part)) _toteTampHomes[part] = part.Transform;
            part.Transform = _toteTampHomes[part];
        }
        var held = _toteTampLabel.GlobalTransform * _toteTampLabel.GetAabb();
        var target = _toteAppliedLabel.GlobalTransform * _toteAppliedLabel.GetAabb();
        _toteTampStroke = held.Position.Z - target.End.Z;
        if (!float.IsFinite(_toteTampStroke) || _toteTampStroke <= 0)
            throw new InvalidOperationException("Tote label installation requires positive measured contact stroke.");
        _toteLabelView = vision.GetNode<Node3D>("FinishingLabelCamera/ViewAxis");
        _toteLabelController = labeler.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        _toteVisionController = vision.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        _toteInspectionReadout = vision.GetNodeOrNull<Label3D>("FinishingInspectionReadout");
        if (_toteInspectionReadout is null)
        {
            _toteInspectionReadout = new Label3D { Name = "FinishingInspectionReadout", Position = new Vector3(0, 3.0f, 0),
                FontSize = 40, PixelSize = .003f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled };
            vision.AddChild(_toteInspectionReadout);
        }
        ProjectToteFinishingProcess();
    }

    private bool ToteLabelEligible()
    {
        if (_toteFinishingProcess is null || !_toteFill!.State.Complete || !_toteCap!.State.Applied
            || _toteCap.State.Stage != ToteCapPlantModel.Phase.Home || AsBool(_points["conveyor_run"])
            || !AsBool(_points["tote_at_label"])) return false;
        return ToteLabelAligned();
    }

    private bool ToteLabelAligned()
    {
        if (_toteTampLabel is null || _toteCarrier is null || !AsBool(_points["tote_at_label"])) return false;
        var held = _toteTampLabel.GlobalTransform * _toteTampLabel.GetAabb();
        var carrier = _toteCarrier!.GlobalTransform * _toteCarrier.GetAabb();
        var retained = _toteAppliedLabel!.GlobalTransform * _toteAppliedLabel.GetAabb();
        var extension = _toteFinishingProcess?.State.Extension ?? 0;
        // The installed stroke is measured at Reset. An altered station pose
        // must still put the actual held label face on the carrier at full
        // extension; XY overlap alone cannot prove that the pad reaches it.
        var predictedContactZ = held.Position.Z - _toteTampStroke * (float)(1 - extension);
        var localSize = _toteTampLabel.GetAabb().Size;
        var thinAxis = localSize.X <= localSize.Y && localSize.X <= localSize.Z ? Vector3.Right
            : localSize.Y <= localSize.Z ? Vector3.Up : Vector3.Back;
        var heldNormal = (_toteTampLabel.GlobalBasis * thinAxis).Normalized();
        var carrierNormal = _toteAppliedLabel.GlobalBasis.Z.Normalized();
        return held.Position.X >= carrier.Position.X && held.End.X <= carrier.End.X
            && held.Position.Y >= carrier.Position.Y && held.End.Y <= carrier.End.Y
            && MathF.Abs(predictedContactZ - retained.End.Z) <= .001f
            && MathF.Abs(heldNormal.Dot(carrierNormal)) >= .999f;
    }

    private bool ToteInspectionViewClear()
    {
        var label = _toteAppliedLabel!.GlobalTransform * _toteAppliedLabel.GetAabb();
        var origin = _toteLabelView!.GlobalPosition;
        var forward = _toteLabelView.GlobalBasis.Z.Normalized();
        var toLabel = label.GetCenter() - origin;
        if (forward.Dot(toLabel.Normalized()) < MathF.Cos(Mathf.DegToRad(30))
            || _toteAppliedLabel.GlobalBasis.Z.Normalized().Dot(-toLabel.Normalized()) < .5f) return false;
        // Screen actual scene triangles along four label-corner rays, after a
        // segment/AABB broad phase. This is modeled visibility, not rendering
        // recognition or a physical camera/lighting calibration.
        var meshes = _sceneRoot.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(m => m != _toteAppliedLabel && m != _toteCarrier
                && !_toteLabelView.GetParent().IsAncestorOf(m)).ToArray();
        foreach (var corner in new[] {
            new Vector3(label.Position.X, label.Position.Y, label.End.Z),
            new Vector3(label.End.X, label.Position.Y, label.End.Z),
            new Vector3(label.Position.X, label.End.Y, label.End.Z),
            new Vector3(label.End.X, label.End.Y, label.End.Z) })
        {
            var direction = corner - origin;
            foreach (var mesh in meshes)
            {
                var bounds = mesh.GlobalTransform * mesh.GetAabb();
                if (!SegmentBox(origin, direction, bounds)) continue;
                var faces = mesh.Mesh.GetFaces();
                for (var i = 0; i + 2 < faces.Length; i += 3)
                    if (SegmentTriangle(origin, direction, mesh.GlobalTransform * faces[i],
                        mesh.GlobalTransform * faces[i + 1], mesh.GlobalTransform * faces[i + 2])) return false;
            }
        }
        return true;
    }

    private static bool SegmentBox(Vector3 origin, Vector3 direction, Aabb bounds)
    {
        var low = 0f; var high = 1f;
        for (var axis = 0; axis < 3; axis++)
        {
            if (MathF.Abs(direction[axis]) < 1e-8f)
            {
                if (origin[axis] < bounds.Position[axis] || origin[axis] > bounds.End[axis]) return false;
                continue;
            }
            var a = (bounds.Position[axis] - origin[axis]) / direction[axis];
            var b = (bounds.End[axis] - origin[axis]) / direction[axis];
            low = MathF.Max(low, MathF.Min(a, b)); high = MathF.Min(high, MathF.Max(a, b));
            if (low > high) return false;
        }
        return true;
    }

    private static bool SegmentTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c)
    {
        var edge = b - a; var other = c - a; var cross = direction.Cross(other);
        var determinant = edge.Dot(cross);
        if (MathF.Abs(determinant) < 1e-8f) return false;
        var inverse = 1 / determinant; var offset = origin - a;
        var u = offset.Dot(cross) * inverse;
        if (u < 0 || u > 1) return false;
        var q = offset.Cross(edge); var v = direction.Dot(q) * inverse;
        if (v < 0 || u + v > 1) return false;
        var t = other.Dot(q) * inverse;
        return t > .001f && t < .999f;
    }

    private void FreezeToteFinishingAdapters()
    {
        _toteLabelController?.SetPhysicsProcess(!UsesExternalClock);
        _toteVisionController?.SetPhysicsProcess(!UsesExternalClock);
    }

    private void AdvanceToteFinishingProcess(double seconds)
    {
        _toteInspectionViewClear = AsBool(_points["tote_at_inspection"]) && ToteInspectionViewClear();
        var inspectionEligible = !AsBool(_points["conveyor_run"]) && _toteInspectionViewClear;
        _toteFinishingProcess!.Advance(seconds, new ToteFinishingProcessModel.Input(
            AsBool(_points["labeler_run"]), ToteLabelEligible(), AsBool(_points["inspection_run"]), inspectionEligible,
            _toteFill!.State.Complete, _toteCap!.State.Applied));
        if (_toteFinishingProcess.State.LabelStage != ToteFinishingProcessModel.LabelPhase.Home)
            _toteLabelController!._PhysicsProcess(seconds);
        if (_toteFinishingProcess.State.InspectionBusy) _toteVisionController!._PhysicsProcess(seconds);
        ProjectToteFinishingProcess();
    }

    private void PauseToteFinishingProcess()
    {
        if (_toteFinishingProcess is null) return;
        _toteFinishingProcess.Pause();
        ProjectToteFinishingProcess();
    }

    private void ProjectToteFinishingProcess()
    {
        var state = _toteFinishingProcess!.State;
        foreach (var (part, home) in _toteTampHomes)
        {
            var parent = part.GetParent<Node3D>();
            if (part.Name == "TAMP_SLIDE" && part is MeshInstance3D slide)
            {
                // Extend the delivered rod from its fixed cylinder engagement
                // while its applicator end advances. Translating the whole
                // original short rod would detach it at full measured stroke.
                var worldHome = parent.GlobalTransform * home;
                var bounds = worldHome * slide.GetAabb();
                var displacement = _toteTampStroke * (float)state.Extension;
                var stretch = Basis.FromScale(new Vector3(1, 1, (bounds.Size.Z + displacement) / bounds.Size.Z));
                var center = bounds.GetCenter() + Vector3.Forward * (displacement / 2);
                var basis = stretch * worldHome.Basis;
                slide.Transform = parent.GlobalTransform.AffineInverse()
                    * new Transform3D(basis, center - basis * slide.GetAabb().GetCenter());
                continue;
            }
            var offset = parent.GlobalBasis.Inverse() * (Vector3.Forward * _toteTampStroke * (float)state.Extension);
            part.Transform = new Transform3D(home.Basis, home.Origin + offset);
        }
        _toteAppliedLabel!.Visible = state.LabelApplied;
        _toteTampLabel!.Visible = !state.LabelApplied;
        SetPoint("label_applied", state.LabelApplied);
        SetPoint("tote_label_aligned", ToteLabelAligned());
        SetPoint("label_extension_percent", state.Extension * 100);
        SetPoint("label_busy", state.LabelStage != ToteFinishingProcessModel.LabelPhase.Home);
        SetPoint("label_home", state.LabelStage == ToteFinishingProcessModel.LabelPhase.Home);
        SetPoint("label_inhibited", state.LabelInhibited);
        SetPoint("inspection_busy", state.InspectionBusy && PlantPlaybackRunning);
        SetPoint("inspection_done", state.InspectionDone);
        SetPoint("inspection_ok", state.InspectionOk);
        SetPoint("inspection_inhibited", state.InspectionInhibited);
        SetPoint("inspection_defect_code", (long)state.Defects);
        SetPoint("inspection_view_clear", _toteInspectionViewClear);
        _toteInspectionReadout!.Text = state.InspectionDone
            ? (state.InspectionOk ? "MODELED CHECK: PASS" : $"MODELED CHECK: FAIL {(int)state.Defects}")
            : state.InspectionBusy ? (PlantPlaybackRunning ? "MODELED CHECK: ACQUIRING" : "MODELED CHECK: PAUSED")
            : "MODELED CHECK: WAITING";
    }
}
