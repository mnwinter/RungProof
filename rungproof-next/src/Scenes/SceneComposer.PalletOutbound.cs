using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigurePalletOutbound(Node3D root)
    {
        var staging = root.GetNode<Node3D>("robot_pallet_conveyor");
        var outbound = root.GetNode<Node3D>("robot_pallet_outbound");
        var bridge = root.GetNode<Node3D>("robot_pallet_transfer_bridge");
        MeshInstance3D Part(Node equipment, string name) => equipment.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Pallet transfer requires '{equipment.Name}/{name}'.");
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != root; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        Aabb Bounds(MeshInstance3D mesh) => InScene(mesh) * mesh.GetAabb();
        var stagingBelt = Bounds(Part(staging, "KIN_belt_surface"));
        var outboundBelt = Bounds(Part(outbound, "KIN_belt_surface"));
        if (MathF.Abs(stagingBelt.End.Y - outboundBelt.End.Y) > 0.001f)
            throw new InvalidOperationException("Pallet transfer belts must share their carrying height.");

        // The belt bounds include the curved drum wrap. Start/end the plate
        // beyond that wrap at deck height, leaving 70/35 mm transfer gaps from
        // the two flat tangent points. Those gaps are narrower than a runner.
        var left = Bounds(Part(staging, "KIN_drive_drum")).GetCenter().X + 0.070f;
        var right = Bounds(Part(outbound, "KIN_tail_drum")).GetCenter().X - 0.035f;
        var length = right - left;
        if (length < 0.20f) throw new InvalidOperationException("Pallet transfer bridge has insufficient installation space.");
        var width = MathF.Min(stagingBelt.Size.Z, outboundBelt.Size.Z);
        var height = stagingBelt.End.Y;
        bridge.Position = new Vector3((left + right) / 2, 0, stagingBelt.GetCenter().Z);
        var steel = Material(new Color("a5b3bb"), 0.7f, 0.23f);
        AddBox(bridge, new Vector3(length, 0.016f, width), new Vector3(0, height - 0.008f, 0), steel).Name = "PALLET_TRANSFER_deck";
        // The deeper bearer must stay between the curved belt wraps. The
        // thin deck spans the remaining distance to the transfer gaps.
        AddBox(bridge, new Vector3(0.20f, 0.080f, width), new Vector3(0, height - 0.056f, 0), steel).Name = "PALLET_TRANSFER_bearer";
        var postTop = height - 0.096f;
        foreach (var side in new[] { -1, 1 })
        {
            AddBox(bridge, new Vector3(0.40f, 0.060f, 0.34f), new Vector3(0, 0.030f, side * width * 0.40f), steel)
                .Name = $"PALLET_TRANSFER_foot_{side}";
            AddBox(bridge, new Vector3(0.080f, postTop - 0.060f, 0.080f),
                new Vector3(0, (postTop + 0.060f) / 2, side * width * 0.40f), steel).Name = $"PALLET_TRANSFER_post_{side}";
        }
        foreach (var conveyor in new[] { staging, outbound })
        {
            // The visual splice witness must not become a raised obstruction
            // through a pallet runner. Keep its existing size/material flush.
            var splice = Part(conveyor, "BELT_vulcanized_splice");
            var rise = Bounds(splice).End.Y - Bounds(Part(conveyor, "KIN_belt_surface")).End.Y;
            splice.Position -= InScene((Node3D)splice.GetParent()).Basis.Inverse() * (Vector3.Up * rise);
        }
        // These imported nail heads protrude through a tote resting on the
        // wooden carrying surface. Seat them flush in this pallet installation.
        var pallet = root.GetNode<Node3D>("robot_pallet");
        var palletMeshes = pallet.FindChildren("*", string.Empty, true, false).OfType<MeshInstance3D>().ToArray();
        var deckY = palletMeshes.Where(mesh => mesh.Name.ToString().StartsWith("PALLET_top_board", StringComparison.Ordinal))
            .Max(mesh => Bounds(mesh).End.Y);
        foreach (var nail in palletMeshes.Where(mesh => mesh.Name.ToString().StartsWith("PALLET_top_nail", StringComparison.Ordinal)))
        {
            var rise = MathF.Max(0, Bounds(nail).End.Y - deckY);
            nail.Position -= InScene((Node3D)nail.GetParent()).Basis.Inverse() * (Vector3.Up * rise);
        }
    }
}
