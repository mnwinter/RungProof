using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

/// <summary>
/// Simulator-side behavior for the first conveyor candidate. These are typed,
/// symbolic simulation points; this class has no PLC transport or addressing.
/// </summary>
public partial class ConveyorController : Node
{
    [Export] public bool RunCommand { get; set; }
    [Export] public bool EstopOk { get; set; } = true;
    [Export(PropertyHint.Range, "-2,2,0.01,suffix:m/s")]
    public float SpeedSetpointMps { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0.05,3,0.05,suffix:m/s²")]
    public float AccelerationMps2 { get; set; } = 0.75f;

    public bool Running => MathF.Abs(ActualSpeedMps) > 0.01f;
    public float ActualSpeedMps { get; private set; }

    private readonly List<(Node3D Node, float RadiusM)> _rotatingParts = [];
    private readonly List<StandardMaterial3D> _beltMaterials = [];
    private readonly Dictionary<Node3D, Transform3D> _rotatingHomes = [];
    private float _beltTravelM;

    public override void _Ready()
    {
        RegisterRotatingParts(GetParent());
        foreach (var (node, _) in _rotatingParts) _rotatingHomes[node] = node.Transform;
        GD.Print(
            $"Conveyor controller bound {_rotatingParts.Count} rotating nodes "
            + $"and {_beltMaterials.Count} belt material surfaces."
        );
    }

    public override void _PhysicsProcess(double delta)
    {
        var target = RunCommand && EstopOk
            ? Math.Clamp(SpeedSetpointMps, -2.0f, 2.0f)
            : 0.0f;
        ActualSpeedMps = Mathf.MoveToward(
            ActualSpeedMps,
            target,
            AccelerationMps2 * (float)delta
        );
        ApplyPlantTravel(ActualSpeedMps * (float)delta, ActualSpeedMps);
    }

    /// <summary>
    /// Projects travel already integrated by a scene plant. That caller must
    /// disable this controller's physics callback to avoid a second clock.
    /// No acceleration/slip model is implied by prescribed scene travel.
    /// </summary>
    public void ApplyPlantTravel(float distanceM, float speedMps)
    {
        ActualSpeedMps = speedMps;
        _beltTravelM += distanceM;

        foreach (var (node, radiusM) in _rotatingParts)
        {
            var radians = distanceM / radiusM;
            node.RotateObjectLocal(Vector3.Up, radians);
        }

        // The authored belt UV uses 0.18 m per U repeat. Offset a duplicated
        // runtime material so this instance can move independently of every
        // other conveyor that shares the imported glTF resource.
        foreach (var material in _beltMaterials)
        {
            material.Uv1Offset = new Vector3(-_beltTravelM / 0.18f, 0.0f, 0.0f);
        }
    }

    // Used by installations with one prescribed plant clock. Restore captured
    // transforms exactly; inverse rotation can retain accumulated float drift.
    public void ResetPlantTravel()
    {
        _beltTravelM = 0; ActualSpeedMps = 0;
        foreach (var (node, home) in _rotatingHomes) node.Transform = home;
        foreach (var material in _beltMaterials) material.Uv1Offset = Vector3.Zero;
    }

    private void RegisterRotatingParts(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Node3D node)
            {
                var name = node.Name.ToString();
                if (name.StartsWith("KIN_belt_surface", StringComparison.Ordinal)
                    && node is MeshInstance3D beltMesh
                    && beltMesh.Mesh is not null)
                {
                    for (var surface = 0; surface < beltMesh.Mesh.GetSurfaceCount(); surface++)
                    {
                        if (beltMesh.Mesh.SurfaceGetMaterial(surface) is not StandardMaterial3D source)
                        {
                            continue;
                        }

                        var instanceMaterial = (StandardMaterial3D)source.Duplicate();
                        beltMesh.SetSurfaceOverrideMaterial(surface, instanceMaterial);
                        _beltMaterials.Add(instanceMaterial);
                    }
                }
                else if (name.StartsWith("KIN_drive_drum", StringComparison.Ordinal)
                    || name.StartsWith("KIN_tail_drum", StringComparison.Ordinal))
                {
                    _rotatingParts.Add((node, 0.059f));
                }
                else if (name.StartsWith("KIN_carry_idler", StringComparison.Ordinal))
                {
                    _rotatingParts.Add((node, 0.032f));
                }
                else if (name.StartsWith("KIN_return_idler", StringComparison.Ordinal))
                {
                    _rotatingParts.Add((node, 0.022f));
                }
            }
            RegisterRotatingParts(child);
        }
    }
}
