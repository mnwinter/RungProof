using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

/// <summary>
/// Reusable simulator-side motion adapter for catalog assets.  It exposes only
/// symbolic playback state and never owns PLC transport, addresses, or I/O.
/// Run advances the configured motion, Stop freezes it, and Reset restores the
/// authored transform.
/// </summary>
public partial class EquipmentMotionController : Node
{
    public enum MotionKind
    {
        ContinuousRotation,
        OscillatingRotation,
        PositionRotation,
        LinearX,
        LinearY,
        PneumaticPusher,
        FanRotor,
        ScissorLift,
        RollerShutter,
        CartesianGantry,
        SpindleFeed,
    }

    [Export] public MotionKind Kind { get; set; }
    [Export] public string TargetPrefix { get; set; } = string.Empty;
    [Export] public bool RunCommand { get; set; }
    [Export(PropertyHint.Range, "0.05,5,0.05,suffix:s")]
    public float TravelTimeSeconds { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0,3600,10,suffix:rpm")]
    public float SpeedRpm { get; set; } = 90.0f;
    [Export]
    public Vector3 RotationAxis { get; set; } = Vector3.Up;
    [Export(PropertyHint.Range, "-5,5,0.01,suffix:m")]
    public float TravelM { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "-360,360,1,suffix:deg")]
    public float TravelDegrees { get; set; } = 90.0f;
    [Export] public bool PositionInputInverted { get; set; }
    // Sequence-driven installations supply their own position each tick. They
    // must not also creep toward the adapter's autonomous endpoint.
    [Export] public bool AutonomousPositionTravel { get; set; } = true;
    // Opt-in setpoint slew for indexed parcel tables. Other assets retain
    // their existing immediate position contract when this value is zero.
    [Export] public float PositionInputSlewSeconds { get; set; }
    // Optional visual stream follows valve position; no fabricated PLC feedback.
    [Export] public string PositionVisibilityChildName { get; set; } = string.Empty;
    // Opt-in mounted fixture follows this carriage's actual position. Its
    // scene root stays addressable by the UI/runtime, without a second clock.
    public Node3D? PositionFollower { get; set; }
    private Transform3D _positionFollowerHome;

    public bool Running => RunCommand;
    public float PositionPercent => _position * 100.0f;
    public float InputPositionNormalized => PositionInputInverted ? 1.0f - _position : _position;

    private readonly List<Node3D> _targets = [];
    private readonly Dictionary<Node3D, Transform3D> _authoredTransforms = [];
    private readonly Dictionary<Node3D, Vector3> _authoredScales = [];
    private readonly Dictionary<Node3D, Node3D> _liftPinFollowers = [];
    private readonly List<Node3D> _liftTipAttachments = [];
    private Node3D? _liftDriveArm;
    private Node3D? _liftBasePin;
    private Node3D? _liftTipPin;
    private MeshInstance3D? _liftCylinder;
    private MeshInstance3D? _liftRod;
    private Vector3 _fanCenter;
    private float _position;
    private float _targetPosition;
    private float _angleRadians;
    // Unbound assets keep their authored nominal speed. A numeric scene
    // binding changes only this fraction; the BOOL run command stays separate.
    private float _speedFraction = 1.0f;

    public override void _Ready()
    {
        BindTargets(GetParent());
        if (PositionFollower is not null) _positionFollowerHome = PositionFollower.Transform;
        if (Kind == MotionKind.ScissorLift)
        {
            // The delivered rollers and retaining washers are siblings of
            // their pins. Bind each to the nearest authored pin so the small
            // mounting offset is retained throughout the two-stage motion.
            var pins = _targets.FindAll(node => IsLiftPin(node.Name.ToString()));
            foreach (var follower in _targets.FindAll(node => IsLiftPinFollower(node.Name.ToString())))
            {
                Node3D? nearest = null;
                var distance = float.PositiveInfinity;
                foreach (var pin in pins)
                {
                    var candidate = pin.Position.DistanceSquaredTo(follower.Position);
                    if (candidate >= distance) continue;
                    distance = candidate;
                    nearest = pin;
                }
                if (nearest is not null) _liftPinFollowers[follower] = nearest;
            }
            BindLiftHydraulics();
        }
        if (Kind == MotionKind.FanRotor)
        {
            var hub = _targets.Find(node => node.Name.ToString().StartsWith("KIN_fan_hub", StringComparison.Ordinal));
            _fanCenter = hub is null ? Vector3.Zero : _authoredTransforms[hub].Origin;
        }
        GD.Print($"Equipment motion controller {Kind} bound {_targets.Count} nodes for '{TargetPrefix}'.");
    }

    public override void _PhysicsProcess(double delta)
    {
        var seconds = (float)delta;
        switch (Kind)
        {
            case MotionKind.ContinuousRotation:
                if (RunCommand)
                {
                    RotateContinuously(seconds);
                }
                break;
            case MotionKind.SpindleFeed:
                // Rotation follows the BOOL spindle command; axial feed follows
                // the declared position motion. Running alone cannot drive down.
                if (RunCommand)
                {
                    _angleRadians += RpmToRadiansPerSecond(SpeedRpm * _speedFraction) * seconds;
                    ApplySpindleFeed();
                }
                break;
            case MotionKind.FanRotor:
                if (RunCommand)
                {
                    _angleRadians += RpmToRadiansPerSecond(SpeedRpm * _speedFraction) * seconds;
                    ApplyFanRotation(_angleRadians);
                }
                break;
            case MotionKind.OscillatingRotation:
                if (RunCommand)
                {
                    _angleRadians += seconds * 0.8f;
                    ApplyRotation(MathF.Sin(_angleRadians) * Mathf.DegToRad(TravelDegrees));
                }
                break;
            case MotionKind.CartesianGantry:
                if (RunCommand)
                {
                    // Illustrative command sweep only. No fabricated home or
                    // pick-complete feedback is sent to the controller.
                    _angleRadians = (_angleRadians + seconds * MathF.PI / MathF.Max(TravelTimeSeconds, 0.05f)) % MathF.Tau;
                    _position = (1.0f - MathF.Cos(_angleRadians)) * 0.5f;
                    ApplyGantryPosition();
                }
                break;
            case MotionKind.PositionRotation:
                if (PositionInputSlewSeconds > 0)
                {
                    if (RunCommand)
                    {
                        _position = Mathf.MoveToward(_position, _targetPosition, seconds / PositionInputSlewSeconds);
                        ApplyPosition();
                    }
                    break;
                }
                goto case MotionKind.LinearX;
            case MotionKind.LinearX:
            case MotionKind.LinearY:
            case MotionKind.PneumaticPusher:
            case MotionKind.ScissorLift:
            case MotionKind.RollerShutter:
                if (RunCommand && AutonomousPositionTravel)
                {
                    _position = Mathf.MoveToward(_position, 1.0f, seconds / MathF.Max(TravelTimeSeconds, 0.05f));
                    ApplyPosition();
                }
                break;
        }
    }

    public void Run() => RunCommand = true;

    public void Stop() => RunCommand = false;

    public void SetSpeedPercent(float percent)
    {
        _speedFraction = float.IsFinite(percent) ? Mathf.Clamp(percent, 0.0f, 100.0f) / 100.0f : 0.0f;
    }

    public void SetPositionNormalized(float position)
    {
        _targetPosition = Mathf.Clamp(PositionInputInverted ? 1.0f - position : position, 0.0f, 1.0f);
        if (Kind == MotionKind.PositionRotation && PositionInputSlewSeconds > 0) return;
        _position = _targetPosition;
        if (Kind is MotionKind.PositionRotation
            or MotionKind.LinearX
            or MotionKind.LinearY
            or MotionKind.PneumaticPusher
            or MotionKind.ScissorLift
            or MotionKind.RollerShutter)
        {
            ApplyPosition();
        }
        else if (Kind == MotionKind.SpindleFeed) ApplySpindleFeed();
    }

    public void ResetMotion()
    {
        RunCommand = false;
        _position = 0.0f;
        _targetPosition = 0.0f;
        _angleRadians = 0.0f;
        _speedFraction = 1.0f;
        foreach (var (node, transform) in _authoredTransforms)
        {
            node.Transform = transform;
            node.Scale = _authoredScales[node];
        }
        ApplyPositionVisibility();
        if (PositionFollower is not null) PositionFollower.Transform = _positionFollowerHome;
    }

    private void RotateContinuously(float delta)
    {
        var radians = RpmToRadiansPerSecond(SpeedRpm * _speedFraction) * delta;
        var axis = RotationAxis.LengthSquared() > 0.0001f
            ? RotationAxis.Normalized()
            : Vector3.Up;
        foreach (var target in _targets)
        {
            target.RotateObjectLocal(axis, radians);
        }
    }

    private void ApplySpindleFeed()
    {
        foreach (var target in _targets)
        {
            var authored = _authoredTransforms[target];
            target.Transform = new Transform3D(
                authored.Basis.Rotated(Vector3.Up, _angleRadians),
                authored.Origin + Vector3.Up * (TravelM * _position));
        }
    }

    private void ApplyPosition()
    {
        ApplyPositionVisibility();
        if (Kind == MotionKind.PositionRotation)
        {
            ApplyRotation(Mathf.DegToRad(TravelDegrees) * _position);
            return;
        }

        if (Kind == MotionKind.ScissorLift)
        {
            ApplyScissorLift();
            return;
        }
        if (Kind == MotionKind.RollerShutter)
        {
            ApplyRollerShutter();
            return;
        }
        if (Kind == MotionKind.PneumaticPusher)
        {
            ApplyPneumaticPusher();
            return;
        }

        var offset = Kind == MotionKind.LinearY
            ? Vector3.Up * (TravelM * _position)
            : Vector3.Right * (TravelM * _position);
        foreach (var target in _targets)
        {
            target.Transform = _authoredTransforms[target].TranslatedLocal(offset);
        }
    }

    private void ApplyPositionVisibility()
    {
        if (PositionVisibilityChildName.Length == 0) return;
        foreach (var target in _targets)
            if (target.FindChild(PositionVisibilityChildName, true, false) is Node3D child)
                child.Visible = _position > 0.5f;
    }

    private void ApplyGantryPosition()
    {
        // The delivered gantry's three KIN nodes are siblings, rather than a
        // transform hierarchy. Move carriage and Z axis with the X bridge.
        foreach (var target in _targets)
        {
            var name = target.Name.ToString();
            var offset = Vector3.Right * (TravelM * _position);
            if (name.StartsWith("KIN_Y_CARRIAGE", StringComparison.Ordinal)
                || name.StartsWith("KIN_Z_AXIS", StringComparison.Ordinal)
                || name.StartsWith("GANTRY_GRIPPER", StringComparison.Ordinal))
                offset += Vector3.Back * (0.45f * _position);
            if (name.StartsWith("KIN_Z_AXIS", StringComparison.Ordinal)
                || name.StartsWith("GANTRY_GRIPPER", StringComparison.Ordinal))
                // Keep this delivered solid rod inside the carriage housing.
                // It is not a modeled telescoping actuator.
                offset += Vector3.Down * (0.12f * _position);
            var authored = _authoredTransforms[target];
            target.Transform = new Transform3D(authored.Basis, authored.Origin + offset);
        }
    }

    private void ApplyRotation(float radians)
    {
        foreach (var target in _targets)
        {
            var authored = _authoredTransforms[target];
            target.Transform = new Transform3D(
                authored.Basis.Rotated(Vector3.Up, radians),
                authored.Origin
            );
        }
    }

    private void BindTargets(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Node3D node && IsTarget(node.Name.ToString()))
            {
                _targets.Add(node);
                _authoredTransforms[node] = node.Transform;
                _authoredScales[node] = node.Scale;
            }
            BindTargets(child);
        }
    }

    private bool IsTarget(string name)
    {
        if (Kind == MotionKind.CartesianGantry)
            return name.StartsWith("KIN_X_BRIDGE", StringComparison.Ordinal)
                || name.StartsWith("KIN_Y_CARRIAGE", StringComparison.Ordinal)
                || name.StartsWith("KIN_Z_AXIS", StringComparison.Ordinal)
                || name.StartsWith("GANTRY_GRIPPER", StringComparison.Ordinal);
        if (Kind == MotionKind.ScissorLift)
        {
            return name.StartsWith("KIN_platform", StringComparison.Ordinal)
                || name.StartsWith("KIN_scissor_", StringComparison.Ordinal)
                || name.StartsWith("KIN_stage_pivot_", StringComparison.Ordinal)
                || name.StartsWith("KIN_mid_pivot_", StringComparison.Ordinal)
                || name.StartsWith("KIN_lift_rod", StringComparison.Ordinal)
                || name.StartsWith("KIN_lift_bellows_", StringComparison.Ordinal)
                || name.StartsWith("LIFT_top_pivot_", StringComparison.Ordinal)
                || name.StartsWith("LIFT_bottom_pivot_", StringComparison.Ordinal)
                || name.StartsWith("LIFT_upper_track_", StringComparison.Ordinal)
                || name.StartsWith("LIFT_cylinder", StringComparison.Ordinal)
                || name.StartsWith("LIFT_rod_end_clevis", StringComparison.Ordinal)
                || name == "LIFT_drive_lug"
                || IsLiftPinFollower(name);
        }
        if (Kind == MotionKind.RollerShutter)
        {
            return name.StartsWith("KIN_slat_", StringComparison.Ordinal)
                || name.StartsWith("KIN_bottom_bar", StringComparison.Ordinal);
        }
        return name.StartsWith(TargetPrefix, StringComparison.Ordinal);
    }

    private void ApplyScissorLift()
    {
        const float armLength = 2.0f;
        const float initialStageHeight = 0.60f;
        var totalRise = TravelM * _position;
        var stageHeight = initialStageHeight + totalRise * 0.5f;
        var initialAngle = MathF.Asin(initialStageHeight / armLength);
        var targetAngle = MathF.Asin(Math.Clamp(stageHeight / armLength, -0.999f, 0.999f));
        var armRotation = targetAngle - initialAngle;
        var targetHalfRun = MathF.Sqrt(MathF.Max(0.0f,
            MathF.Pow(armLength * 0.5f, 2) - MathF.Pow(stageHeight * 0.5f, 2)));
        foreach (var target in _targets)
        {
            var authored = _authoredTransforms[target];
            var name = target.Name.ToString();
            if (name.StartsWith("KIN_platform", StringComparison.Ordinal)
                || name.StartsWith("LIFT_top_pivot_", StringComparison.Ordinal)
                || name.StartsWith("LIFT_upper_track_", StringComparison.Ordinal))
            {
                var origin = authored.Origin + Vector3.Up * totalRise;
                if (name.StartsWith("LIFT_top_pivot_", StringComparison.Ordinal))
                    origin.X = MathF.CopySign(targetHalfRun, authored.Origin.X);
                target.Transform = new Transform3D(authored.Basis, origin);
            }
            else if (name.StartsWith("KIN_scissor_", StringComparison.Ordinal))
            {
                // Blender's positive Y rotation imports as negative Godot Z
                // rotation, so the name suffix and runtime delta have opposite signs.
                var sign = name.EndsWith("_-1", StringComparison.Ordinal) ? 1.0f : -1.0f;
                var stage = name.StartsWith("KIN_scissor_1_", StringComparison.Ordinal) ? 1 : 0;
                var centerRise = totalRise * (stage == 0 ? 0.25f : 0.75f);
                target.Transform = new Transform3D(
                    authored.Basis.Rotated(Vector3.Back, sign * armRotation),
                    authored.Origin + Vector3.Up * centerRise
                );
            }
            else if (name.StartsWith("KIN_stage_pivot_", StringComparison.Ordinal))
            {
                var stage = name.StartsWith("KIN_stage_pivot_1_", StringComparison.Ordinal) ? 1 : 0;
                var centerRise = totalRise * (stage == 0 ? 0.25f : 0.75f);
                target.Transform = new Transform3D(authored.Basis, authored.Origin + Vector3.Up * centerRise);
            }
            else if (name.StartsWith("KIN_mid_pivot_", StringComparison.Ordinal))
            {
                var origin = authored.Origin + Vector3.Up * (totalRise * 0.5f);
                origin.X = MathF.CopySign(targetHalfRun, authored.Origin.X);
                target.Transform = new Transform3D(authored.Basis, origin);
            }
            else if (name.StartsWith("LIFT_bottom_pivot_", StringComparison.Ordinal))
            {
                var origin = authored.Origin;
                origin.X = MathF.CopySign(targetHalfRun, authored.Origin.X);
                target.Transform = new Transform3D(authored.Basis, origin);
            }
            else if (name.StartsWith("KIN_lift_bellows_", StringComparison.Ordinal))
            {
                // The bellows spans from the fixed base envelope to the
                // travelling platform.  Scale around its centre and move the
                // centre by half the platform travel so both ends stay seated.
                var scale = _authoredScales[target];
                scale.Y *= 1.0f + totalRise / 1.22f;
                target.Transform = new Transform3D(
                    authored.Basis,
                    authored.Origin + Vector3.Up * (totalRise * 0.5f)
                );
                target.Scale = scale;
            }
        }
        foreach (var (follower, pin) in _liftPinFollowers)
        {
            var authored = _authoredTransforms[follower];
            follower.Transform = new Transform3D(authored.Basis,
                authored.Origin + pin.Position - _authoredTransforms[pin].Origin);
        }
        ApplyLiftHydraulics();
    }

    private void BindLiftHydraulics()
    {
        // These names belong to the delivered two-stage lift master. Bind the
        // entire chain; a lone translating rod leaves its clevis/lug behind.
        _liftDriveArm = _targets.Find(node => node.Name == "KIN_scissor_0_-0_65_-1");
        _liftBasePin = _targets.Find(node => node.Name == "LIFT_cylinder_base_clevis_pin");
        _liftTipPin = _targets.Find(node => node.Name == "LIFT_rod_end_clevis_pin");
        _liftCylinder = _targets.Find(node => node.Name == "LIFT_cylinder") as MeshInstance3D;
        _liftRod = _targets.Find(node => node.Name == "KIN_lift_rod") as MeshInstance3D;
        _liftTipAttachments.AddRange(_targets.FindAll(node =>
            node.Name.ToString().StartsWith("LIFT_rod_end_clevis", StringComparison.Ordinal)
            || node.Name == "LIFT_drive_lug"));
        if (_liftDriveArm is null || _liftBasePin is null || _liftTipPin is null
            || _liftCylinder is null || _liftRod is null)
        {
            GD.PushWarning("Lift hydraulic chain is incomplete; attachment motion is unavailable.");
            return;
        }
        // All delivered parts are siblings in the imported asset coordinate
        // system. Refuse mixed parents rather than applying a wrong local pose.
        var parent = _liftDriveArm.GetParent();
        if (_liftBasePin.GetParent() != parent || _liftTipPin.GetParent() != parent
            || _liftCylinder.GetParent() != parent || _liftRod.GetParent() != parent
            || _liftTipAttachments.Exists(node => node.GetParent() != parent))
        {
            GD.PushWarning("Lift hydraulic chain has mixed parents; attachment motion is unavailable.");
            _liftDriveArm = null;
        }
    }

    private void ApplyLiftHydraulics()
    {
        if (_liftDriveArm is null || _liftBasePin is null || _liftTipPin is null
            || _liftCylinder is null || _liftRod is null) return;
        var armDelta = _liftDriveArm.Transform * _authoredTransforms[_liftDriveArm].AffineInverse();
        foreach (var attachment in _liftTipAttachments)
            attachment.Transform = armDelta * _authoredTransforms[attachment];
        var tail = _liftBasePin.Position;
        var tip = _liftTipPin.Position;
        // Keep the barrel length fixed; only the exposed rod extends. The
        // imported cylinder meshes run along local Y, in the asset's XY plane.
        var barrelRest = _authoredTransforms[_liftCylinder];
        var barrelLength = _liftCylinder.GetAabb().Size.Y * barrelRest.Basis.Y.Length();
        var gland = tail + (tip - tail).Normalized() * barrelLength;
        PoseMember(_liftCylinder, tail, gland);
        PoseMember(_liftRod, gland, tip);

        void PoseMember(MeshInstance3D member, Vector3 start, Vector3 end)
        {
            var authored = _authoredTransforms[member];
            var delta = end - start;
            var authoredAxis = authored.Basis.Y;
            var angle = MathF.Atan2(delta.Y, delta.X) - MathF.Atan2(authoredAxis.Y, authoredAxis.X);
            var ratio = delta.Length() / (member.GetAabb().Size.Y * authoredAxis.Length());
            var basis = authored.Basis * Basis.FromScale(new Vector3(1, ratio, 1));
            member.Transform = new Transform3D(basis.Rotated(Vector3.Back, angle), (start + end) * 0.5f);
        }
    }

    private static bool IsLiftPin(string name) =>
        name.StartsWith("LIFT_top_pivot_", StringComparison.Ordinal)
        || name.StartsWith("LIFT_bottom_pivot_", StringComparison.Ordinal)
        || name.StartsWith("KIN_mid_pivot_", StringComparison.Ordinal)
        || name.StartsWith("KIN_stage_pivot_", StringComparison.Ordinal);

    private static bool IsLiftPinFollower(string name) =>
        name.StartsWith("LIFT_retaining_washer_", StringComparison.Ordinal)
        || name.StartsWith("LIFT_lower_guide_roller_", StringComparison.Ordinal)
        || name.StartsWith("LIFT_upper_guide_roller_", StringComparison.Ordinal);

    private void ApplyRollerShutter()
    {
        var top = float.MinValue;
        foreach (var target in _targets)
        {
            if (target.Name.ToString().StartsWith("KIN_slat_", StringComparison.Ordinal))
            {
                top = MathF.Max(top, _authoredTransforms[target].Origin.Y);
            }
        }
        var stackTop = top + 0.10f;
        foreach (var target in _targets)
        {
            var authored = _authoredTransforms[target];
            var name = target.Name.ToString();
            float targetY;
            if (name.StartsWith("KIN_slat_", StringComparison.Ordinal))
            {
                var suffix = name["KIN_slat_".Length..];
                var index = int.TryParse(suffix, out var parsed) ? parsed : 0;
                targetY = stackTop - index * 0.025f;
            }
            else
            {
                targetY = stackTop - 0.50f;
            }
            var origin = authored.Origin;
            origin.Y = Mathf.Lerp(authored.Origin.Y, targetY, _position);
            target.Transform = new Transform3D(authored.Basis, origin);
        }
    }

    private void ApplyPneumaticPusher()
    {
        var extension = TravelM * _position;
        if (PositionFollower is not null)
        {
            var axis = ((Node3D)GetParent()).Basis.X.Normalized();
            PositionFollower.Transform = new Transform3D(_positionFollowerHome.Basis,
                _positionFollowerHome.Origin + axis * extension);
        }
        foreach (var target in _targets)
        {
            var authored = _authoredTransforms[target];
            if (target.Name.ToString().StartsWith("KIN_pusher_rod", StringComparison.Ordinal)
                && target is MeshInstance3D rod)
            {
                // The rod remains seated in the front cap while its free end
                // follows the carriage. Scaling and shifting half the added
                // length models extension instead of translating a loose rod.
                // The imported rod's local X points vertically. Stretch in
                // parent X instead, using the delivered rod length (not the
                // commanded stroke) to keep its gland end fixed and its free
                // end seated in the travelling clevis. Premultiplication
                // preserves both transverse dimensions and the imported basis.
                var length = MathF.Max((authored * rod.GetAabb()).Size.X, 0.00001f);
                var axialScale = Basis.FromScale(new Vector3((length + extension) / length, 1, 1));
                target.Transform = new Transform3D(
                    axialScale * authored.Basis,
                    authored.Origin + Vector3.Right * (extension * 0.5f));
            }
            else
            {
                // Keep the authored basis and move the part in the asset's
                // parent-space longitudinal direction.
                target.Transform = new Transform3D(
                    authored.Basis,
                    authored.Origin + Vector3.Right * extension);
            }
        }
    }

    private void ApplyFanRotation(float radians)
    {
        var rotation = new Basis(Vector3.Back, radians);
        foreach (var target in _targets)
        {
            var authored = _authoredTransforms[target];
            var offset = authored.Origin - _fanCenter;
            target.Transform = new Transform3D(
                rotation * authored.Basis,
                _fanCenter + rotation * offset
            );
        }
    }

    private static float RpmToRadiansPerSecond(float rpm) => rpm * Mathf.Tau / 60.0f;
}
