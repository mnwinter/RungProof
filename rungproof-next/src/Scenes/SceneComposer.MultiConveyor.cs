using System;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local supports use the delivered carrying endpoints. No load motion
    // or sensor feedback is inferred from the presence of these static parts.
    private static void ConfigureMultiConveyorHandoffs(Node3D root)
    {
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent != null && parent != root; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        MeshInstance3D Part(Node node, string name) => node.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Multi-conveyor handoff requires {node.Name}/{name}.");
        Aabb Bounds(MeshInstance3D mesh) => InScene(mesh) * mesh.GetAabb();
        var steel = Material(new Color("a5b3bb"), .7f, .25f);
        var installation = root.GetNode<Node3D>("zone_handoffs");
        for (var zone = 0; zone < 3; zone++)
        {
            var feed = root.GetNode<Node3D>($"conveyor_{zone}");
            var belt = Bounds(Part(feed, "KIN_belt_surface"));
            var splice = Part(feed, "BELT_vulcanized_splice");
            splice.Position += InScene((Node3D)splice.GetParent()).Basis.Inverse()
                * new Vector3(0, belt.End.Y - Bounds(splice).End.Y, 0);
            // Clear the complete curved belt envelope; the remaining small gap
            // requires load-bearing verification with the actual pallet runners.
            var left = belt.End.X + .005f;
            float right, width;
            if (zone < 2)
            {
                var receiving = root.GetNode<Node3D>($"conveyor_{zone + 1}");
                var nextBelt = Bounds(Part(receiving, "KIN_belt_surface"));
                if (MathF.Abs(nextBelt.End.Y - belt.End.Y) > .001f)
                    throw new InvalidOperationException("Multi-conveyor belt heights differ.");
                right = nextBelt.Position.X - .005f;
                width = MathF.Min(belt.Size.Z, nextBelt.Size.Z);
            }
            else
            {
                var roller = Bounds(Part(root.GetNode<Node3D>("training_accessory_6"), "KIN_roller_00"));
                if (MathF.Abs(roller.End.Y - belt.End.Y) > .001f)
                    throw new InvalidOperationException("Receiving roller and belt carrying heights differ.");
                right = roller.Position.X - .005f;
                width = MathF.Min(belt.Size.Z, roller.Size.Z);
            }
            var span = right - left;
            if (span < .1f || span > 1f)
                throw new InvalidOperationException("Multi-conveyor transfer span is outside the installation range.");
            var center = (left + right) / 2;
            var top = belt.End.Y;
            AddBox(installation, new Vector3(span, .016f, width), new Vector3(center, top - .008f, 0), steel)
                .Name = $"ZONE_{zone}_transfer_deck";
            foreach (var side in new[] { -1, 1 })
            {
                var z = side * width * .4f;
                AddBox(installation, new Vector3(.16f, .06f, .16f), new Vector3(center, .03f, z), steel)
                    .Name = $"ZONE_{zone}_transfer_foot_{side}";
                AddBox(installation, new Vector3(.06f, top - .076f, .06f), new Vector3(center, (top - .016f + .06f) / 2, z), steel)
                    .Name = $"ZONE_{zone}_transfer_post_{side}";
            }
            GD.Print($"MULTI_CONVEYOR_HANDOFF zone={zone} left={left} right={right} top={top} width={width}");
        }
        // The receiving asset has a delivered drive and rollers. It shares
        // zone 3's command; the transfer deck between them remains passive.
        root.GetNode<Node3D>("training_accessory_6").AddChild(new RollerConveyorController
        {
            Name = "RouteReceivingRollers",
            SpeedSetpointMps = .75f,
            // This route uses prescribed velocity, not an inertial drive model.
            AccelerationMps2 = 1000f,
        });
    }
}
