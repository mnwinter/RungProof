using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureCoatingInstallation(Node3D root)
    {
        // Retain the delivered gun and fan; omit their presentation pedestals
        // only in this explicitly mounted installation.
        var gun = root.GetNode<Node3D>("training_accessory_6");
        foreach (var name in new[] { "BASE", "NAMEPLATE", "LABEL" })
            ((MeshInstance3D)gun.FindChild(name, true, false)).Visible = false;
        var fan = root.GetNode<Node3D>("fan_2");
        foreach (var mesh in fan.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            if (mesh.Name == "FAN_base" || mesh.Name.ToString().StartsWith("FAN_post_")
                || mesh.Name.ToString().StartsWith("FAN_anchor_")) mesh.Visible = false;
        // A cone represents the enabled command. It is not fluid particles,
        // measured coverage, or a fabricated coating-complete sensor.
        root.AddChild(new MeshInstance3D {
            Name = "CoatingSpray", Position = new Vector3(0, 1.45f, 0), Visible = false,
            Mesh = new CylinderMesh { TopRadius = .018f, BottomRadius = .14f, Height = .30f },
            MaterialOverride = new StandardMaterial3D {
                AlbedoColor = new Color(.20f, .65f, .95f, .30f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        });
    }
}
