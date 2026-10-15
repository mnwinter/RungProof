using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureHandDryerInstallation(Node3D root)
    {
        var fan = root.GetNode<Node3D>("fan_0");
        foreach (var mesh in fan.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            if (mesh.Name == "FAN_base" || mesh.Name.ToString().StartsWith("FAN_post_")
                || mesh.Name.ToString().StartsWith("FAN_anchor_")) mesh.Visible = false;
        // Keep the delivered hands as a distinct actor; the sensor station and
        // grounded tray remain fixed when hands are withdrawn.
        var station = root.GetNode<Node3D>("training_accessory_3");
        foreach (var foot in station.FindChildren("HAND_STATION_foot_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            foot.Visible = false;
        var hands = new Node3D { Name = "HandPair" };
        station.AddChild(hands);
        foreach (var mesh in station.FindChildren("HAND_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray())
            if (mesh.Name.ToString().StartsWith("HAND_0_") || mesh.Name.ToString().StartsWith("HAND_1_"))
            {
                // Composition runs before the tree is attached; global transforms
                // are unavailable here. Preserve the transform relative to station.
                var transform = mesh.Transform;
                for (var parent = mesh.GetParent() as Node3D; parent is not null && parent != station; parent = parent.GetParent() as Node3D)
                    transform = parent.Transform * transform;
                mesh.Owner = null;
                mesh.GetParent().RemoveChild(mesh);
                hands.AddChild(mesh);
                mesh.Transform = transform;
            }
        hands.Visible = false;
        root.AddChild(new MeshInstance3D {
            Name = "DryerAirCommand", Position = new Vector3(0, .965f, .18f), Visible = false,
            Mesh = new CylinderMesh { TopRadius = .24f, BottomRadius = .28f, Height = .07f },
            MaterialOverride = new StandardMaterial3D {
                AlbedoColor = new Color(.25f, .7f, 1, .2f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        });
    }
}
