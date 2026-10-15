using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

// Animated upper chain run only. This adapter does not transport a carton or
// infer PLC permissives. The installation's load model is a separate concern.
public partial class ChainFeedMotion : EquipmentMotionController
{
    private readonly Dictionary<Node3D, Transform3D> _slats = [];
    private readonly Dictionary<Node3D, Basis> _sprockets = [];
    private float _travel;
    public override void _Ready()
    {
        base._Ready();
        foreach (var part in GetParent().FindChildren("SPROCKET*", "MeshInstance3D", true, false).OfType<Node3D>()) _sprockets[part] = part.Basis;
        foreach (var slat in GetParent().FindChildren("KIN_CHAIN_SLAT_*", "MeshInstance3D", true, false).OfType<Node3D>())
            _slats[slat] = slat.Transform;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!RunCommand) return;
        ApplyPlantTravel((float)delta * .3f);
    }
    public void ApplyPlantTravel(float distanceM)
    {
        _travel += distanceM;
        foreach (var (slat, home) in _slats)
        {
            // Each authored strand has thirteen links on a 160 mm pitch.
            var offset = (_travel + home.Origin.X + 1.04f) % 2.08f;
            slat.Position = new Vector3(offset - 1.04f, home.Origin.Y, home.Origin.Z);
        }
        foreach (var (sprocket, basis) in _sprockets) sprocket.Basis = basis * new Basis(Vector3.Up, -_travel / .1f);
    }
    public new void ResetMotion()
    {
        base.ResetMotion(); _travel = 0;
        foreach (var (slat, home) in _slats) slat.Transform = home;
        foreach (var (sprocket, basis) in _sprockets) sprocket.Basis = basis;
    }
}
