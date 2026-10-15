using Godot;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreatePalletHandlingRobot(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "robotics.robot.six-axis-medium.v1");
        var steel = Material(new Color("52636b"), 0.5f, 0.3f);
        model.AddChild(new MeshInstance3D { Name = "ROBOT_installation_foot", Position = new Vector3(0, -0.97f, 0),
            Mesh = new CylinderMesh { TopRadius = 0.45f, BottomRadius = 0.45f, Height = 0.06f, RadialSegments = 48 }, MaterialOverride = steel });
        model.AddChild(new MeshInstance3D { Name = "ROBOT_installation_pedestal", Position = new Vector3(0, -0.47f, 0),
            Mesh = new CylinderMesh { TopRadius = 0.40f, BottomRadius = 0.40f, Height = 0.94f, RadialSegments = 48 }, MaterialOverride = steel });
        model.AddChild(new MeshInstance3D { Name = "ROBOT_installation_top_plate", Position = new Vector3(0, -0.025f, 0),
            Mesh = new CylinderMesh { TopRadius = 0.74f, BottomRadius = 0.74f, Height = 0.05f, RadialSegments = 48 }, MaterialOverride = steel });
        var controller = new PalletRobotMotion { Name = "PalletRobotMotion" };
        model.AddChild(controller);
        controller.InitializeModel();
        return model;
    }

    private static Node3D CreateRaisedPalletReceiver(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "material-handling.receiver.container-two-position.v1");
        // Mount identification on the backstop's inward face instead of
        // projecting into the descending wrist at the receiving edge.
        foreach (var name in new[] { "RECEIVER_NAMEPLATE", "RECEIVER_NAME" })
            if (model.FindChild(name, true, false) is Node3D label)
                label.Position += new Vector3(0, 0.65f, -1.2375f);
        const float elevation = 0.80f;
        var steel = Material(new Color("52636b"), 0.5f, 0.3f);
        foreach (var x in new[] { -1.05f, 1.05f })
        foreach (var z in new[] { -0.55f, 0.55f })
        {
            AddBox(model, new Vector3(0.24f, 0.04f, 0.24f), new Vector3(x, -elevation + 0.02f, z), steel)
                .Name = $"RECEIVER_grounded_foot_{x}_{z}";
            AddBox(model, new Vector3(0.12f, elevation - 0.04f, 0.12f), new Vector3(x, (-elevation + 0.04f) / 2, z), steel)
                .Name = $"RECEIVER_support_post_{x}_{z}";
        }
        return model;
    }
}
