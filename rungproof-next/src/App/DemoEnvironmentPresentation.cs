using System;
using Godot;

namespace RungProof.Next.App;

/// <summary>
/// Original, decorative context for all authored scenes and training labs.
/// Attach directly beside the process composition, never beneath it: framing,
/// picking, workspace persistence and simulation must see only process nodes.
/// Main owns removal/QueueFree and the reduced-motion/playback policy.
/// </summary>
public partial class DemoEnvironmentPresentation : Node3D
{
    private Node3D? _ventilationRotor;
    private float _rotorAngle;
    private bool _ambientMotionEnabled;
    private bool _ambientMotionInitialized;

    public bool AmbientMotionEnabled => _ambientMotionEnabled;
    public Aabb ProtectedProcessBounds { get; private set; }
    public Aabb? OperatorAccessBounds { get; private set; }

    /// <summary>
    /// Bounds must enclose the complete process travel envelope in the parent's
    /// coordinates. This factory does not discover or modify equipment itself.
    /// Optional floorElevation is a fixed authored floor in those coordinates;
    /// it anchors the slab and grounded backdrop props without shrinking the
    /// supplied travel envelope used for horizontal and upper clearance.
    /// Missing catalog profiles are reported; invalid bounds allocate no nodes.
    /// </summary>
    public static DemoEnvironmentPresentation? Create(string sceneId, Aabb processBounds,
        Vector3 cameraDirection, float? floorElevation = null, Aabb? operatorAccessBounds = null)
    {
        if (!SceneEnvironmentProfiles.TryGet(sceneId, out var profile))
        {
            GD.PushError($"SCENE_ENVIRONMENT_PROFILE_MISSING {sceneId}");
            return null;
        }
        if (!Finite(processBounds.Position) || !Finite(processBounds.Size)
            || processBounds.Size.X <= 0 || processBounds.Size.Y <= 0 || processBounds.Size.Z <= 0
            || (floorElevation.HasValue && !float.IsFinite(floorElevation.Value))) return null;

        // The machinery retains its authored world axes. The room follows those
        // axes too; camera signs choose the open cutaway, never building yaw.
        var center = processBounds.GetCenter();
        var groundY = floorElevation ?? 0.0f;
        var root = new DemoEnvironmentPresentation
        {
            Name = "DemoEnvironmentPresentation",
            Position = new Vector3(center.X, groundY, center.Z),
            Rotation = Vector3.Zero,
            ProtectedProcessBounds = processBounds,
            OperatorAccessBounds = operatorAccessBounds,
        };
        var localBounds = new Aabb(processBounds.Position - root.Position, processBounds.Size);
        var rearX = cameraDirection.X >= 0 ? -1 : 1;
        var rearZ = cameraDirection.Z >= 0 ? -1 : 1;
        var direction = SceneEnvironmentArtDirections.ForScene(sceneId);
        root.BuildCatalogEnvironment(profile, localBounds, rearX, rearZ, direction);
        root.SetAmbientMotionEnabled(false);
        GD.Print($"SCENE_ENVIRONMENT_READY scene={sceneId} family={profile.Family} variant={profile.Variant} yaw=0 protectedBounds={localBounds}");
        return root;
    }

    public void SetAmbientMotionEnabled(bool enabled)
    {
        // Main may refresh policy every frame. Apply transitions once, while
        // allowing the factory's first false call to really disable processing.
        if (_ambientMotionInitialized && _ambientMotionEnabled == enabled) return;
        _ambientMotionInitialized = true;
        _ambientMotionEnabled = enabled;
        if (!enabled)
        {
            _rotorAngle = 0;
            if (_ventilationRotor is not null) _ventilationRotor.Rotation = Vector3.Zero;
        }
        SetProcess(enabled);
    }

    public override void _Process(double delta)
    {
        if (!_ambientMotionEnabled || _ventilationRotor is null || !double.IsFinite(delta) || delta <= 0)
            return;
        // One slow decorative turn per 12 seconds; neither scan time nor machine
        // feedback drives this clock. No node/material allocation during frames.
        _rotorAngle = (_rotorAngle + (float)(Math.Min(delta, 0.25) * Math.Tau / 12)) % MathF.Tau;
        _ventilationRotor.Rotation = new Vector3(0, 0, _rotorAngle);
    }

    private void AddVentilation(Vector3 location, Material steel, Material wall, Material dark)
    {
        var housing = new Node3D { Name = "DecorativeBackgroundVentilation", Position = location };
        AddChild(housing);
        Box(housing, "VentHousing", new Vector3(0.58f, 0.58f, 0.12f), new Vector3(0, 0, 0.08f), steel);
        Box(housing, "VentRecess", new Vector3(0.47f, 0.47f, 0.025f), Vector3.Zero, dark);
        _ventilationRotor = new Node3D { Name = "DecorativeVentilationRotor", Position = new Vector3(0, 0, -0.035f) };
        housing.AddChild(_ventilationRotor);
        for (var blade = 0; blade < 3; blade++)
        {
            var arm = new Node3D { Name = "VentBlade" + blade, Rotation = new Vector3(0, 0, blade * MathF.Tau / 3) };
            _ventilationRotor.AddChild(arm);
            Box(arm, "Blade", new Vector3(0.10f, 0.18f, 0.025f), new Vector3(0, 0.13f, 0), wall);
        }
        var hub = new MeshInstance3D
        {
            Name = "VentHub", Mesh = new CylinderMesh { TopRadius = 0.065f, BottomRadius = 0.065f, Height = 0.055f },
            MaterialOverride = steel, RotationDegrees = new Vector3(90, 0, 0),
        };
        _ventilationRotor.AddChild(hub);
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X)
        && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static StandardMaterial3D Surface(Color color, float metallic, float roughness) => new()
    {
        AlbedoColor = color, Metallic = metallic, Roughness = roughness,
    };

    private static void Cabinet(Node3D parent, Vector3 ground, Material steel, Material trim)
    {
        Box(parent, "DecorativeServiceCabinet", new Vector3(0.55f, 1.35f, 0.36f), ground + new Vector3(0, 0.675f, 0), steel);
        Box(parent, "CabinetDoor", new Vector3(0.46f, 1.16f, 0.025f), ground + new Vector3(0, 0.72f, -0.195f), trim);
        Box(parent, "CabinetHandle", new Vector3(0.025f, 0.16f, 0.04f), ground + new Vector3(0.16f, 0.76f, -0.23f), steel);
    }

    private static void Box(Node3D parent, string name, Vector3 size, Vector3 position, Material material)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = new BoxMesh { Size = size }, Position = position, MaterialOverride = material,
        });
    }
}
