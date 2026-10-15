using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureToteFinishingProcess(Node3D scene)
    {
        var tote = scene.GetNode<Node3D>("finishing_tote");
        var labeler = scene.GetNode<Node3D>("labeler");
        var vision = scene.GetNode<Node3D>("vision_inspector");
        Transform3D Pose(Node3D node)
        {
            var pose = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != scene; parent = parent.GetParent() as Node3D)
                pose = parent.Transform * pose;
            return pose;
        }
        Aabb Bounds(MeshInstance3D mesh) => Pose(mesh) * mesh.GetAabb();
        var tampLabel = (MeshInstance3D)labeler.FindChild("LABEL_ON_TAMP", true, false);
        var tamp = Bounds(tampLabel);
        var cage = tote.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(m => m.Name.ToString().StartsWith("IBC_cage_x_", StringComparison.Ordinal) && Bounds(m).GetCenter().Z > 0)
            .OrderBy(m => Math.Abs(Bounds(m).GetCenter().Y - tamp.GetCenter().Y)).Take(2).ToArray();
        if (cage.Length != 2) throw new InvalidOperationException("Label carrier requires two delivered front cage rails.");
        var cageFront = cage.Max(m => Bounds(m).End.Z);
        var relativeX = tamp.GetCenter().X - labeler.Position.X;
        var carrierCenter = new Vector3(tote.Position.X + relativeX, tamp.GetCenter().Y, cageFront + .006f);
        MeshInstance3D Box(string name, Vector3 size, Vector3 center, Material material)
        {
            var mesh = AddBox(tote, size, Vector3.Zero, material);
            mesh.Name = name;
            mesh.Transform = Pose(tote).AffineInverse() * new Transform3D(Basis.Identity, center);
            return mesh;
        }
        var carrierSize = new Vector3(tamp.Size.X + .035f, tamp.Size.Y + .035f, .012f);
        Box("FINISHING_label_carrier", carrierSize, carrierCenter, Material(new Color("a5b3bb"), .7f, .3f));
        // Two brackets physically connect the carrier to actual cage rails.
        foreach (var (rail, index) in cage.Select((rail, index) => (rail, index)))
        {
            var bounds = Bounds(rail);
            var low = MathF.Min(bounds.GetCenter().Y, carrierCenter.Y);
            var high = MathF.Max(bounds.GetCenter().Y, carrierCenter.Y);
            Box($"FINISHING_label_bracket_{index}", new Vector3(.05f, high - low + .025f, .025f),
                new Vector3(carrierCenter.X + (index == 0 ? -.1f : .1f), (low + high) / 2, cageFront),
                Material(new Color("a5b3bb"), .7f, .3f));
        }
        var applied = Box("FINISHING_applied_label", new Vector3(tamp.Size.X, tamp.Size.Y, .003f),
            carrierCenter + Vector3.Back * (carrierSize.Z / 2 + .0015f), Material(new Color("f4f2e8"), 0, .7f));
        applied.Visible = false;

        // Existing silhouette cameras face the opposite side of the tote.
        // Move one complete side-camera assembly to an outboard rear mount;
        // its diagonal view clears the installed backlight's side edge.
        var optic = (MeshInstance3D)vision.FindChild("SIDE_OPTIC_GLASS_0_55", true, false);
        var oldOptic = Bounds(optic).GetCenter();
        var target = new Vector3(vision.Position.X + relativeX, carrierCenter.Y, carrierCenter.Z);
        var desiredOptic = target + new Vector3(1.5f, 0, 1.5f);
        var towards = (target - desiredOptic).Normalized();
        var yaw = MathF.Atan2(towards.X, towards.Z);
        var rotation = new Basis(Vector3.Up, yaw);
        var assembly = new Node3D { Name = "FinishingLabelCamera" };
        vision.AddChild(assembly);
        var moved = vision.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(m => m.Name.ToString().EndsWith("_0_55", StringComparison.Ordinal)
                && (m.Name.ToString().StartsWith("SIDE_CAMERA", StringComparison.Ordinal)
                    || m.Name.ToString().StartsWith("SIDE_LENS", StringComparison.Ordinal)
                    || m.Name.ToString().StartsWith("SIDE_OPTIC", StringComparison.Ordinal)
                    || m.Name.ToString().StartsWith("SIDE_LED", StringComparison.Ordinal))).ToArray();
        foreach (var part in moved)
        {
            var pose = Pose(part);
            var desired = new Transform3D(rotation * pose.Basis, desiredOptic + rotation * (pose.Origin - oldOptic));
            part.Owner = null;
            part.GetParent().RemoveChild(part);
            assembly.AddChild(part);
            part.Transform = Pose(assembly).AffineInverse() * desired;
        }
        var view = new Node3D { Name = "ViewAxis" };
        assembly.AddChild(view);
        view.Transform = Pose(assembly).AffineInverse() * new Transform3D(rotation, desiredOptic);
        // Support to the grounded rear arch, outside the belt/load envelope.
        var mountPoint = desiredOptic - towards * .30f;
        var supportRoot = new Vector3(vision.Position.X, mountPoint.Y, 1.90f);
        var horizontal = mountPoint with { Z = 1.90f };
        AddSumpRoute(vision, "FINISHING_label_camera_mount", new[] {
            supportRoot - vision.Position, horizontal - vision.Position, mountPoint - vision.Position },
            .045f, Material(new Color("a5b3bb"), .7f, .3f));
    }
}
