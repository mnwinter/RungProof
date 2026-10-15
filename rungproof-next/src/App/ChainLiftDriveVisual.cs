using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

// Chain position is derived from the actual carriage position, not another
// integrated clock. Stop/Reset cannot leave the drive and carriage out of phase.
public partial class ChainLiftDriveVisual : EquipmentMotionController
{
    public const float UpperSprocketCenterY = 3.95f;
    private MeshInstance3D[] _links = [];
    private Transform3D _loadHome;
    private readonly System.Collections.Generic.Dictionary<Node3D, Basis> _sprockets = [];
    public override void _Ready()
    {
        base._Ready();
        if (PositionFollower is not null) _loadHome = PositionFollower.Transform;
        _links = GetParent().FindChildren("LIFT_chain_link_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        foreach (var part in GetParent().FindChildren("LIFT_sprocket_*", "MeshInstance3D", true, false).OfType<Node3D>()) _sprockets[part] = part.Basis;
        ProjectDrive();
    }
    public override void _PhysicsProcess(double delta) { base._PhysicsProcess(delta); ProjectDrive(); }
    public new void ResetMotion() { base.ResetMotion(); ProjectDrive(); }
    public void ProjectDrive()
    {
        const float straight = UpperSprocketCenterY - .35f, radius = .2f;
        var arc = MathF.PI * radius;
        var length = 2 * (straight + arc);
        var travel = PositionPercent / 100 * TravelM;
        if (PositionFollower is not null)
            PositionFollower.Transform = new Transform3D(_loadHome.Basis, _loadHome.Origin + GetParent<Node3D>().Basis.Y * travel);
        foreach (var link in _links)
        {
            var name = link.Name.ToString();
            var ordinal = int.Parse(name[(name.LastIndexOf('_') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            var z = name.Contains("_-", StringComparison.Ordinal) ? -1.05f : 1.05f;
            var distance = (ordinal * length / 96 + travel) % length;
            float x, y, angle;
            if (distance < straight) { x = .45f; y = .35f + distance; angle = 0; }
            else if (distance < straight + arc)
            {
                var a = (distance - straight) / radius;
                x = .65f - radius * MathF.Cos(a); y = UpperSprocketCenterY + radius * MathF.Sin(a); angle = -a;
            }
            else if (distance < 2 * straight + arc) { x = .85f; y = UpperSprocketCenterY - (distance - straight - arc); angle = MathF.PI; }
            else
            {
                var a = (distance - 2 * straight - arc) / radius;
                x = .65f + radius * MathF.Cos(a); y = .35f - radius * MathF.Sin(a); angle = MathF.PI - a;
            }
            link.Position = new Vector3(x, y, z);
            link.Rotation = new Vector3(0, 0, angle);
        }
        foreach (var (sprocket, basis) in _sprockets) sprocket.Basis = basis * new Basis(Vector3.Up, -travel / radius);
    }
}
