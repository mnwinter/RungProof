using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void MountChickenReadoutToMachineFrame(Node3D root, SceneDefinition scene)
    {
        if (scene.Id != "lab-10-02-chicken-label-print") return;
        var scale = root.GetNode<Node3D>("training_accessory_5");
        var parts = scale.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        foreach (var part in parts)
            if (part.Name == "WEIGH_readout_foot" || part.Name == "WEIGH_readout_mast") part.Visible = false;
        // Bounds in the retained scale root's local frame: neither the scale
        // nor its deck, head, product path, labels or controller roots moves.
        Aabb LocalBounds(MeshInstance3D part)
        {
            var pose = part.Transform;
            for (var parent = part.GetParent(); parent != scale; parent = parent.GetParent())
                if (parent is Node3D spatial) pose = spatial.Transform * pose;
            return pose * part.GetAabb();
        }
        var head = LocalBounds(parts.Single(p => p.Name == "WEIGH_readout_housing"));
        var frame = parts.Where(p => p.Name.ToString().StartsWith("WEIGH_FRAME_side_", StringComparison.Ordinal))
            .Select(LocalBounds).OrderBy(b => MathF.Abs(b.GetCenter().Z - head.GetCenter().Z)).First();
        const float thickness = .08f;
        var x = head.GetCenter().X;
        var frameZ = frame.GetCenter().Z;
        var headZ = head.GetCenter().Z;
        var armY = head.Position.Y - thickness / 2;
        var postBottom = frame.End.Y - .02f;
        var postTop = armY + thickness / 2;
        if (postTop <= postBottom) throw new InvalidOperationException("Checkweigher readout frame support has invalid height.");
        var metal = Material(new Color("aebbc0"), .65f, .3f);
        AddBox(scale, new Vector3(thickness, postTop - postBottom, thickness),
            new Vector3(x, (postTop + postBottom) / 2, frameZ), metal).Name = "WEIGH_readout_frame_post";
        AddBox(scale, new Vector3(thickness, thickness, MathF.Abs(headZ - frameZ) + thickness),
            new Vector3(x, armY, (frameZ + headZ) / 2), metal).Name = "WEIGH_readout_frame_arm";
        scale.SetMeta("installationKind", "machineFrameReadout");
        scale.SetMeta("machineReadoutSupport", "WEIGH_readout_frame_arm");
        GD.Print($"MACHINE_READOUT_MOUNT_READY scene={scene.Id} equipment=training_accessory_5 support=scale-frame scale-root-and-head=unchanged");
    }
}
