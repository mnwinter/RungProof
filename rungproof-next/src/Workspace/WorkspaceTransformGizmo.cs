using System;
using Godot;

namespace RungProof.Next.Workspace;

public enum WorkspaceTransformMode
{
    Move,
    Rotate,
    Scale,
}

public enum WorkspaceTransformSpace
{
    World,
    Local,
}

/// <summary>
/// Runtime editor gizmo with screen-space hit testing. It owns only the visual
/// handles and drag measurement; Main owns undo, validation, and transforms.
/// </summary>
public partial class WorkspaceTransformGizmo : Node3D
{
    private static readonly Vector3[] Axes = [Vector3.Right, Vector3.Up, Vector3.Back];
    private static readonly Color[] Colors = [new("ef5b5b"), new("65d49a"), new("4aa5ff")];
    private const float Length = 0.85f;
    private int _activeAxis = -1;
    private Vector2 _dragStart;
    private float _startAngle;

    public WorkspaceTransformMode Mode { get; private set; } = WorkspaceTransformMode.Move;
    public WorkspaceTransformSpace Space { get; private set; } = WorkspaceTransformSpace.World;
    public bool IsDragging => _activeAxis >= 0;
    public int ActiveAxis => _activeAxis;

    public WorkspaceTransformGizmo()
    {
        Name = "WorkspaceTransformGizmo";
        Visible = false;
    }

    public override void _Ready() => Rebuild();

    public void SetMode(WorkspaceTransformMode mode)
    {
        Mode = mode;
        Rebuild();
    }

    public void SetSpace(WorkspaceTransformSpace space) => Space = space;

    public void AttachTo(Vector3 globalPosition, Basis orientation)
    {
        GlobalPosition = globalPosition;
        GlobalBasis = orientation.Orthonormalized();
        Visible = true;
    }

    public void Detach()
    {
        Visible = false;
        _activeAxis = -1;
    }

    public bool BeginDrag(Camera3D camera, Vector2 pointer)
    {
        if (!Visible) return false;
        _activeAxis = HitTest(camera, pointer);
        if (_activeAxis < 0) return false;
        _dragStart = pointer;
        var center = camera.UnprojectPosition(GlobalPosition);
        _startAngle = MathF.Atan2(pointer.Y - center.Y, pointer.X - center.X);
        return true;
    }

    public float DragAmount(Camera3D camera, Vector2 pointer)
    {
        if (_activeAxis < 0) return 0.0f;
        if (Mode == WorkspaceTransformMode.Rotate)
        {
            var center = camera.UnprojectPosition(GlobalPosition);
            var angle = MathF.Atan2(pointer.Y - center.Y, pointer.X - center.X);
            return Mathf.RadToDeg(Mathf.Wrap(angle - _startAngle, -Mathf.Pi, Mathf.Pi));
        }

        var origin = camera.UnprojectPosition(GlobalPosition);
        var endpoint = camera.UnprojectPosition(GlobalPosition + ActiveAxisWorld() * Length);
        var projected = endpoint - origin;
        var pixels = projected.Length();
        if (pixels < 4.0f) return 0.0f;
        var pixelDelta = (pointer - _dragStart).Dot(projected / pixels);
        var worldDelta = pixelDelta / pixels * Length;
        return Mode == WorkspaceTransformMode.Scale ? worldDelta / Length : worldDelta;
    }

    public void EndDrag() => _activeAxis = -1;

    public static Vector3 Axis(int index) => index >= 0 && index < Axes.Length
        ? Axes[index]
        : Vector3.Zero;

    public Vector3 ActiveAxisWorld() => _activeAxis >= 0 && _activeAxis < Axes.Length
        ? (GlobalBasis * Axes[_activeAxis]).Normalized()
        : Vector3.Zero;

    private int HitTest(Camera3D camera, Vector2 pointer)
    {
        var bestAxis = -1;
        var bestDistance = 14.0f;
        if (Mode == WorkspaceTransformMode.Rotate)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                var previous = camera.UnprojectPosition(GlobalPosition + GlobalBasis * RingPoint(axis, 0));
                for (var segment = 1; segment <= 48; segment++)
                {
                    var current = camera.UnprojectPosition(GlobalPosition + GlobalBasis * RingPoint(axis, segment));
                    var distance = DistanceToSegment(pointer, previous, current);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestAxis = axis;
                    }
                    previous = current;
                }
            }
            return bestAxis;
        }

        var origin = camera.UnprojectPosition(GlobalPosition);
        for (var axis = 0; axis < 3; axis++)
        {
            var endpoint = camera.UnprojectPosition(GlobalPosition + (GlobalBasis * Axes[axis]).Normalized() * Length);
            var distance = DistanceToSegment(pointer, origin, endpoint);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestAxis = axis;
            }
        }
        return bestAxis;
    }

    private void Rebuild()
    {
        foreach (Node child in GetChildren()) child.QueueFree();
        for (var axis = 0; axis < 3; axis++)
        {
            var material = new StandardMaterial3D
            {
                AlbedoColor = Colors[axis],
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                NoDepthTest = true,
                EmissionEnabled = true,
                Emission = Colors[axis],
                EmissionEnergyMultiplier = 2.2f,
            };
            var lines = new ImmediateMesh();
            if (Mode == WorkspaceTransformMode.Rotate)
            {
                lines.SurfaceBegin(Mesh.PrimitiveType.LineStrip, material);
                for (var segment = 0; segment <= 48; segment++)
                    lines.SurfaceAddVertex(RingPoint(axis, segment));
            }
            else
            {
                lines.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
                lines.SurfaceAddVertex(Vector3.Zero);
                lines.SurfaceAddVertex(Axes[axis] * Length);
            }
            lines.SurfaceEnd();
            AddChild(new MeshInstance3D
            {
                Name = $"Axis{axis}",
                Mesh = lines,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
            AddChild(new Label3D
            {
                Name = $"AxisLabel{axis}",
                Text = axis switch { 0 => "X", 1 => "Y", _ => "Z" },
                Position = Axes[axis] * (Length + 0.12f),
                Modulate = Colors[axis],
                FontSize = 48,
                PixelSize = 0.0045f,
                NoDepthTest = true,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            });
            if (Mode != WorkspaceTransformMode.Rotate)
            {
                AddChild(new MeshInstance3D
                {
                    Name = $"Handle{axis}",
                    Position = Axes[axis] * Length,
                    Mesh = Mode == WorkspaceTransformMode.Move
                        ? new SphereMesh { Radius = 0.07f, Height = 0.14f, Material = material }
                        : new BoxMesh { Size = Vector3.One * 0.13f, Material = material },
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                });
            }
        }
    }

    private static Vector3 RingPoint(int axis, int segment)
    {
        var angle = Mathf.Tau * segment / 48.0f;
        var cosine = MathF.Cos(angle) * Length;
        var sine = MathF.Sin(angle) * Length;
        return axis switch
        {
            0 => new Vector3(0, cosine, sine),
            1 => new Vector3(cosine, 0, sine),
            _ => new Vector3(cosine, sine, 0),
        };
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 1e-6f) return point.DistanceTo(start);
        var amount = Mathf.Clamp((point - start).Dot(segment) / lengthSquared, 0.0f, 1.0f);
        return point.DistanceTo(start + segment * amount);
    }
}
