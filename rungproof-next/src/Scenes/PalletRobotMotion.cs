using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

/// <summary>
/// Scene-specific geometric handling adapter. The imported joint hierarchy
/// drives the tool, and a gripped tote follows that tool. This is an offline
/// visual model, not manufacturer joint limits, payload or safety validation.
/// There is no autonomous clock: the existing reference/runtime advances it.
/// </summary>
public partial class PalletRobotMotion : Node
{
    public bool RunCommand { get; set; }
    public bool AtPark { get; private set; }
    public Node3D? GrippedLoad { get; private set; }
    public float PositionErrorM { get; private set; }
    public float OrientationError { get; private set; }
    public Transform3D ToolTransform => new(_joints[5].GlobalBasis,
        _joints[5].GlobalTransform * new Vector3(0.98f, 0, 0));
    public float[] JointAngles => (float[])_angles.Clone();
    public bool ReferenceNeedsReset => _motionKey.Length > 0;
    // Installation-specific grip datum/orientation. Existing tote handling
    // keeps its original values; a thin CNC billet needs a higher side grip
    // to clear the vise jaws and a tool pointing into the front opening.
    public float GripHeightM { get; set; } = 0.53f;
    public Basis DesiredToolBasis { get; set; } = new(Vector3.Back, Vector3.Up, Vector3.Left);
    public float[]? InitialJointAngles { get; set; }
    public Vector3 ParkOffset { get; set; } = new(0, 3.25f, 0);

    private static readonly Vector3[] Axes = [Vector3.Up, Vector3.Back, Vector3.Back,
        Vector3.Right, Vector3.Back, Vector3.Right];
    private readonly Node3D[] _joints = new Node3D[6];
    private readonly Transform3D[] _rest = new Transform3D[6];
    private float[] _angles = [2.7f, -0.35f, -0.5f, Mathf.Pi / 2, -Mathf.Pi / 2, -1.6f];
    private float[] _home = [];
    private Vector3 _approachFrom;
    private Vector3 _approachTo;
    private readonly List<(Node3D Part, Vector3 Origin, float Side)> _jawParts = [];
    private readonly List<(MeshInstance3D Mesh, float Side)> _guides = [];
    private readonly List<(MeshInstance3D Mesh, float Side)> _jawMounts = [];
    private readonly List<Node3D> _cableAnchors = [];
    private readonly List<MeshInstance3D> _cableSegments = [];
    private string _motionKey = string.Empty;
    private Vector3 _loadOffset;
    private float _closedHalfSpan;
    private float _jawHalfSpan;
    private Node3D Robot => (Node3D)GetParent();

    public void InitializeModel()
    {
        for (var index = 0; index < 6; index++)
        {
            _joints[index] = Robot.FindChild($"KIN_axis_{index + 1}", true, false) as Node3D
                ?? throw new InvalidOperationException($"Missing robot joint {index + 1}.");
            _rest[index] = _joints[index].Transform;
        }
        ConfigureJawsAndCable();
    }

    public override void _Ready()
    {
        if (InitialJointAngles is { Length: 6 }) _angles = (float[])InitialJointAngles.Clone();
        if (!Solve(ParkTarget)) throw new InvalidOperationException("Pallet robot park pose is unreachable.");
        _home = (float[])_angles.Clone();
        ResetMotion();
    }

    public Vector3 ParkTarget => Robot.GlobalPosition + ParkOffset;

    public void ResetMotion()
    {
        RunCommand = false;
        GrippedLoad = null;
        _motionKey = string.Empty;
        if (_home.Length != 6) return;
        AtPark = true;
        _angles = (float[])_home.Clone();
        ApplyAngles();
        SetJawSpan(0.36f);
    }

    public bool ApplyReference(string key, string kind, Node3D? load,
        Vector3 from, Vector3 to, float progress)
    {
        if (!RunCommand) return false;
        AtPark = false;
        if (_motionKey != key)
        {
            _motionKey = key;
            if (load is not null)
            {
                var bounds = Bounds(load);
                _loadOffset = Vector3.Up * (bounds.Position.Y - load.GlobalPosition.Y + GripHeightM);
                // Tool Z spans the tote's world X sides. Thin fingers fit the
                // measured bay guides while retaining a visible side contact.
                _closedHalfSpan = bounds.Size.X / 2 + 0.035f;
            }
            if (kind == "robotApproach")
            {
                GrippedLoad = null;
                _approachFrom = ToolTransform.Origin;
                _approachTo = load is null ? ParkTarget : (to == Vector3.Zero ? load.GlobalPosition : to) + _loadOffset;
            }
        }
        switch (kind)
        {
            case "robotApproach":
                // Follow the declared tool route with constant orientation.
                // Joint interpolation alone can sweep the open gripper through
                // a staged load even when both endpoint poses are clear.
                var eased = progress * progress * (3 - 2 * progress);
                if (!Solve(_approachFrom.Lerp(_approachTo, eased))) return false;
                // Publish a completed park, not proximity during its final scan.
                AtPark = load is null && progress >= 0.99999f
                    && ToolTransform.Origin.DistanceTo(ParkTarget) < 0.0005f;
                SetJawSpan(load is null ? _jawHalfSpan : _closedHalfSpan + 0.025f);
                return true;
            case "robotGrip":
                if (load is null || !Solve(load.GlobalPosition + _loadOffset)) return false;
                SetJawSpan(_closedHalfSpan + 0.025f * (1 - progress));
                if (progress >= 0.99999f) GrippedLoad = load;
                return true;
            case "robotTransfer":
                if (load is null || GrippedLoad != load) return false;
                if (!Solve(from.Lerp(to, progress) + _loadOffset)) return false;
                load.GlobalPosition = ToolTransform.Origin - _loadOffset;
                SetJawSpan(_closedHalfSpan);
                return true;
            case "robotRelease":
                GrippedLoad = null;
                SetJawSpan(_closedHalfSpan + 0.008f * progress);
                return true;
            case "robotWithdraw":
                GrippedLoad = null;
                return Solve(from.Lerp(to, progress) + _loadOffset);
            default: return false;
        }
    }

    // Damped least squares using the actual imported pivot origins and axes.
    // A failed solve leaves the last valid pose/load in place. The runtime then
    // stops the reference instead of counting an unreachable placement.
    public bool Solve(Vector3 target)
    {
        var previous = (float[])_angles.Clone();
        for (var iteration = 0; iteration < 180; iteration++)
        {
            ApplyAngles(updateAttachments: false);
            var tool = ToolTransform;
            var position = target - tool.Origin;
            var rotation = (tool.Basis.X.Cross(DesiredToolBasis.X) + tool.Basis.Y.Cross(DesiredToolBasis.Y)
                + tool.Basis.Z.Cross(DesiredToolBasis.Z)) * 0.5f;
            PositionErrorM = position.Length();
            OrientationError = (tool.Basis.X - DesiredToolBasis.X).Length()
                + (tool.Basis.Y - DesiredToolBasis.Y).Length() + (tool.Basis.Z - DesiredToolBasis.Z).Length();
            if (PositionErrorM < 0.0002f && OrientationError < 0.003f)
            { UpdateCable(); return true; }
            var jacobian = new double[6, 6];
            for (var joint = 0; joint < 6; joint++)
            {
                var axis = (_joints[joint].GlobalBasis * Axes[joint]).Normalized();
                var translation = axis.Cross(tool.Origin - _joints[joint].GlobalPosition);
                for (var coordinate = 0; coordinate < 3; coordinate++)
                {
                    jacobian[coordinate, joint] = translation[coordinate];
                    jacobian[coordinate + 3, joint] = axis[coordinate] * 0.5;
                }
            }
            var normal = new double[6, 7];
            for (var row = 0; row < 6; row++)
            {
                for (var column = 0; column < 6; column++)
                {
                    for (var joint = 0; joint < 6; joint++) normal[row, column] += jacobian[row, joint] * jacobian[column, joint];
                    if (row == column) normal[row, column] += 0.0025;
                }
                normal[row, 6] = row < 3 ? position[row] : rotation[row - 3] * 0.5;
            }
            if (!Eliminate(normal)) break;
            for (var joint = 0; joint < 6; joint++)
            {
                var delta = 0.0;
                for (var row = 0; row < 6; row++) delta += jacobian[row, joint] * normal[row, 6];
                _angles[joint] += (float)Math.Clamp(delta, -0.16, 0.16);
                _angles[joint] = Mathf.Wrap(_angles[joint], -Mathf.Pi, Mathf.Pi);
                if (joint == 0) _angles[joint] = Mathf.Clamp(_angles[joint], Mathf.DegToRad(-170), Mathf.DegToRad(170));
            }
        }
        _angles = previous;
        ApplyAngles();
        GD.Print($"PALLET_ROBOT_IK_REJECT target={target} positionErrorM={PositionErrorM} orientationError={OrientationError} angles={string.Join(',', _angles.Select(Mathf.RadToDeg))}");
        return false;
    }

    private static bool Eliminate(double[,] augmented)
    {
        for (var pivot = 0; pivot < 6; pivot++)
        {
            var best = pivot;
            for (var row = pivot + 1; row < 6; row++)
                if (Math.Abs(augmented[row, pivot]) > Math.Abs(augmented[best, pivot])) best = row;
            if (Math.Abs(augmented[best, pivot]) < 1e-12) return false;
            for (var column = pivot; column < 7; column++)
                (augmented[pivot, column], augmented[best, column]) = (augmented[best, column], augmented[pivot, column]);
            var divisor = augmented[pivot, pivot];
            for (var column = pivot; column < 7; column++) augmented[pivot, column] /= divisor;
            for (var row = 0; row < 6; row++)
            {
                if (row == pivot) continue;
                var factor = augmented[row, pivot];
                for (var column = pivot; column < 7; column++) augmented[row, column] -= factor * augmented[pivot, column];
            }
        }
        return true;
    }

    private void ApplyAngles(bool updateAttachments = true)
    {
        for (var index = 0; index < 6; index++)
            _joints[index].Transform = new Transform3D(_rest[index].Basis * new Basis(Axes[index], _angles[index]), _rest[index].Origin);
        if (updateAttachments) UpdateCable();
    }

    private void ConfigureJawsAndCable()
    {
        var parts = Robot.FindChildren("*", string.Empty, true, false).OfType<MeshInstance3D>().ToArray();
        foreach (var part in parts.Where(part => part.Name.ToString().StartsWith("ROBOT_gripper_finger", StringComparison.Ordinal)))
        {
            if (part.GetParent() != _joints[5]) ReparentAuthored(part, _joints[5]);
            var origin = part.Position;
            _jawParts.Add((part, origin, MathF.Sign(origin.Z)));
            if (!part.Name.ToString().Contains("bolt", StringComparison.Ordinal)) part.Scale *= new Vector3(1, 1, 0.07f / 0.09f);
        }
        foreach (var guide in parts.Where(part => part.Name.ToString().StartsWith("ROBOT_gripper_guide", StringComparison.Ordinal))) guide.Visible = false;
        var steel = new StandardMaterial3D { AlbedoColor = new Color("a6b0b7"), Metallic = 0.7f, Roughness = 0.25f };
        foreach (var side in new[] { -1f, 1f })
        {
            var guide = Segment($"ROBOT_jaw_guide_{side}", 0.018f, steel);
            _joints[5].AddChild(guide);
            _guides.Add((guide, side));
            var mount = Segment($"ROBOT_jaw_mount_{side}", 0.018f, steel);
            _joints[5].AddChild(mount);
            _jawMounts.Add((mount, side));
        }
        var oldCable = parts.Single(part => part.Name == "ROBOT_dresspack");
        oldCable.Visible = false;
        for (var index = 0; index < 5; index++)
        {
            var anchor = parts.Single(part => part.Name == $"ROBOT_dresspack_clip_{index}");
            // The last strain relief sits on the forearm before the wrist
            // roll. Rotating that fixed clip with J4 swept its cable sideways
            // through the tote even though the arm itself stayed clear.
            if (index > 0) ReparentAuthored(anchor, _joints[index < 3 ? 1 : 2]);
            if (index >= 3)
            {
                // Put forearm clips on the opposite outer face; short connected
                // brackets keep the cable away from the tote without spanning
                // through the arm with a long unsupported extension.
                anchor.Position = anchor.Position with { Z = -anchor.Position.Z };
                var mountingFace = anchor.Position with { Z = MathF.Sign(anchor.Position.Z) * 0.22f };
                var support = Segment($"ROBOT_dresspack_standoff_{index}", 0.018f, steel);
                anchor.GetParent().AddChild(support);
                FitSegment(support, mountingFace, anchor.Position, false);
            }
            _cableAnchors.Add(anchor);
        }
        var wristJunction = parts.Single(part => part.Name == "ROBOT_wrist_junction");
        ReparentAuthored(wristJunction, _joints[2]);
        wristJunction.Position = wristJunction.Position with { Z = -wristJunction.Position.Z };
        for (var index = 0; index < 4; index++)
        {
            var segment = Segment($"ROBOT_following_dresspack_{index}", 0.035f, oldCable.GetActiveMaterial(0));
            Robot.AddChild(segment);
            _cableSegments.Add(segment);
        }
    }

    private void ReparentAuthored(Node3D part, Node3D joint)
    {
        // Composition happens before entering the SceneTree. Resolve imported
        // transforms locally instead of requiring an unavailable global pose.
        Transform3D InModel(Node3D node)
        {
            var result = node.Transform;
            for (var parent = node.GetParent(); parent != Robot; parent = parent.GetParent())
                if (parent is Node3D spatial) result = spatial.Transform * result;
            return result;
        }
        var local = InModel(joint).AffineInverse() * InModel(part);
        part.Owner = null;
        part.Reparent(joint, false);
        part.Transform = local;
    }

    private static MeshInstance3D Segment(string name, float radius, Material material) => new()
    {
        Name = name, Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = 1, RadialSegments = 12 },
        MaterialOverride = material,
    };

    private static void FitSegment(MeshInstance3D mesh, Vector3 start, Vector3 end, bool global)
    {
        var direction = end - start;
        var y = direction.Normalized();
        var x = (MathF.Abs(y.Dot(Vector3.Up)) < 0.95f ? Vector3.Up : Vector3.Right).Cross(y).Normalized();
        var transform = new Transform3D(new Basis(x, y * direction.Length(), x.Cross(y)), (start + end) / 2);
        if (global) mesh.GlobalTransform = transform; else mesh.Transform = transform;
    }

    private void SetJawSpan(float halfSpan)
    {
        _jawHalfSpan = halfSpan;
        foreach (var (part, origin, side) in _jawParts) part.Position = origin with { Z = side * halfSpan };
        foreach (var (guide, side) in _guides)
            FitSegment(guide, new Vector3(0.50f, 0, side * 0.22f), new Vector3(0.50f, 0, side * halfSpan), false);
        foreach (var (mount, side) in _jawMounts)
            FitSegment(mount, new Vector3(0.50f, 0, side * halfSpan), new Vector3(0.58f, 0, side * halfSpan), false);
    }

    private void UpdateCable()
    {
        for (var index = 0; index < _cableSegments.Count; index++)
            FitSegment(_cableSegments[index], _cableAnchors[index].GlobalPosition, _cableAnchors[index + 1].GlobalPosition, true);
    }

    private static Aabb Bounds(Node3D root)
    {
        var meshes = root.FindChildren("*", string.Empty, true, false).OfType<MeshInstance3D>().Where(mesh => mesh.IsVisibleInTree()).ToArray();
        var bounds = meshes[0].GlobalTransform * meshes[0].GetAabb();
        foreach (var mesh in meshes.Skip(1)) bounds = bounds.Merge(mesh.GlobalTransform * mesh.GetAabb());
        return bounds;
    }
}
