using System;
using System.Linq;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local installation. The reusable scissor table and the old
    // accessory packages remain unchanged for other lessons.
    private static Node3D CreateGuidedChainLift()
    {
        var root = new Node3D();
        var steel = Material(new Color("52636b"), .6f, .3f);
        var blue = Material(new Color("177fb8"), .25f, .35f);
        var yellow = Material(new Color("d9a900"), .2f, .4f);
        MeshInstance3D Box(Node3D parent, string name, Vector3 size, Vector3 at, Material material)
        { var mesh = AddBox(parent, size, at, material); mesh.Name = name; return mesh; }
        foreach (var x in new[] { -1.15f, 1.15f })
        foreach (var z in new[] { -.94f, .94f })
        {
            Box(root, $"LIFT_foot_{x}_{z}", new(.32f, .08f, .32f), new(x, .04f, z), steel);
            Box(root, $"LIFT_guide_{x}_{z}", new(.12f, 4.02f, .12f), new(x, 2.09f, z), blue);
        }
        foreach (var z in new[] { -.94f, .94f })
        {
            Box(root, $"LIFT_header_{z}", new(2.42f, .16f, .14f), new(0, 4.02f, z), steel);
            Box(root, $"LIFT_base_{z}", new(2.42f, .12f, .14f), new(0, .14f, z), steel);
        }
        var carriage = new Node3D { Name = "KIN_chain_lift_carriage" };
        root.AddChild(carriage);
        Box(carriage, "LIFT_carrying_deck", new(2.5f, .087f, 1.5f), new(0, .921f, 0), yellow);
        // Recess the supporting plate below the belt. Separate yellow margins
        // keep the transfer height flush without coplanar overlapping faces.
        Box(carriage, "KIN_belt_surface", new(2.32f, .003f, 1.3f), new(0, .966f, 0), Material(new Color("202b31"), .05f, .75f));
        foreach (var x in new[] { -1.205f, 1.205f })
            Box(carriage, $"LIFT_deck_margin_end_{x}", new(.09f, .003f, 1.5f), new(x, .966f, 0), yellow);
        foreach (var z in new[] { -.7f, .7f })
            Box(carriage, $"LIFT_deck_margin_side_{z}", new(2.32f, .003f, .1f), new(0, .966f, z), yellow);
        foreach (var x in new[] { -1.16f, 1.16f })
            AddCylinder(carriage, x < 0 ? "KIN_tail_drum" : "KIN_drive_drum", new(x, .91f, 0), .0575f, 1.3f, steel, new(90, 0, 0));
        Box(carriage, "LIFT_belt_drive", new(.25f, .18f, .19f), new(.9f, .61f, .72f), blue);
        Box(carriage, "LIFT_belt_drive_mount", new(.18f, .025f, .21f), new(.9f, .71f, .72f), steel);
        carriage.AddChild(new ConveyorController { Name = "CarryingBelt", EstopOk = true });
        foreach (var x in new[] { -.9f, .9f })
            Box(carriage, $"LIFT_deck_support_{x}", new(.14f, .16f, 2.02f), new(x, .8f, 0), steel);
        foreach (var z in new[] { -.94f, .94f })
        {
            Box(carriage, $"LIFT_carriage_side_{z}", new(2.3f, .16f, .14f), new(0, .8f, z), steel);
            foreach (var x in new[] { -1.15f, 1.15f })
                Box(carriage, $"LIFT_guide_shoe_{x}_{z}", new(.18f, .24f, .18f), new(x, .8f, z), steel);
            Box(carriage, $"LIFT_chain_attachment_{z}", new(.12f, .14f, .2f), new(.45f, .8f, MathF.Sign(z) * .995f), steel);
            // The active strand runs up at X=.45; its return runs down at .85.
            // Both attach to the common moving carriage, outside the load path.
            foreach (var y in new[] { .35f, ChainLiftDriveVisual.UpperSprocketCenterY })
            {
                Box(root, $"LIFT_bearing_mount_{y}_{z}", new(.62f, .08f, .14f), new(.9f, y, z), steel);
                AddCylinder(root, $"LIFT_bearing_{y}_{z}", new(.65f, y, z), .065f, .14f, steel, new(90, 0, 0));
                AddCylinder(root, $"LIFT_sprocket_{y}_{z}", new(.65f, y, MathF.Sign(z) * 1.05f), .2f, .09f, steel, new(90, 0, 0));
                var sprocket = root.GetChildren().OfType<MeshInstance3D>().Last();
                for (var tooth = 0; tooth < 16; tooth++)
                {
                    var angle = tooth * MathF.Tau / 16;
                    var mesh = Box(sprocket, $"LIFT_tooth_{tooth}", new(.04f, .085f, .035f),
                        new(.2f * MathF.Cos(angle), 0, .2f * MathF.Sin(angle)), steel);
                    mesh.Rotation = new Vector3(0, -angle, 0);
                }
            }
            for (var link = 0; link < 96; link++)
                Box(root, $"LIFT_chain_link_{z}_{link}", new(.04f, .055f, .065f), Vector3.Zero, steel);
        }
        AddCylinder(root, "LIFT_drive_shaft", new(.65f, ChainLiftDriveVisual.UpperSprocketCenterY, 0), .04f, 2.12f, steel, new(90, 0, 0));
        Box(root, "LIFT_drive_gearbox", new(.32f, .3f, .28f), new(.65f, ChainLiftDriveVisual.UpperSprocketCenterY, -1.15f), blue);
        AddCylinder(root, "LIFT_drive_motor", new(.65f, ChainLiftDriveVisual.UpperSprocketCenterY, -1.49f), .17f, .4f, blue, new(90, 0, 0));
        Box(root, "LIFT_drive_mount", new(.14f, .5f, .35f), new(.65f, 4.02f, -1.05f), steel);
        var motion = new ChainLiftDriveVisual
        {
            Name = "ChainLiftMotion", Kind = EquipmentMotionController.MotionKind.LinearY,
            TargetPrefix = "KIN_chain_lift_carriage", TravelM = 2.1f, TravelTimeSeconds = 4,
        };
        root.AddChild(motion);
        return root;
    }

    private static void ConfigureChainLiftReceiver(Node3D sceneRoot)
    {
        var receiver = sceneRoot.GetNode<Node3D>("receiving_conveyor");
        MeshInstance3D Part(string name) => receiver.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Chain lift receiver requires '{name}'.");
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != sceneRoot; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        // This delivery's splice witness sits above its belt. Seat it flush
        // in this installation so a resting carton cannot pass through it.
        var splice = Part("BELT_vulcanized_splice");
        var rise = (InScene(splice) * splice.GetAabb()).End.Y
            - (InScene(Part("KIN_belt_surface")) * Part("KIN_belt_surface").GetAabb()).End.Y;
        splice.Position -= InScene((Node3D)splice.GetParent()).Basis.Inverse() * (Vector3.Up * rise);
    }

    private static Node3D CreateChainLiftFeed(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "material-handling.conveyor.pallet-chain-2strand.v1");
        // This delivery contains a demonstration pallet and cube. They are
        // not additional process loads in this installation.
        foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                     .Where(mesh => mesh.Name.ToString().StartsWith("PALLET", StringComparison.Ordinal)).ToArray())
        { mesh.GetParent().RemoveChild(mesh); mesh.QueueFree(); }
        var steel = Material(new Color("aebbc0"), .65f, .3f);
        foreach (var x in new[] { -1.17f, 1.17f })
        {
            var plate = AddBox(model, new(.26f, .025f, .8f), new(x, .955f, 0), steel);
            plate.Name = $"CHAIN_end_wear_plate_{x}";
        }
        model.AddChild(new ChainFeedMotion { Name = "ChainFeedMotion", TargetPrefix = "SPROCKET", Kind = EquipmentMotionController.MotionKind.ContinuousRotation });
        return model;
    }

    private static Node3D CreateChainLiftAccessory(string installation)
    {
        var root = new Node3D();
        var steel = Material(new Color("aebbc0"), .65f, .3f);
        var dark = Material(new Color("283840"), .3f, .4f);
        MeshInstance3D Box(string name, Vector3 size, Vector3 at, Material material)
        { var mesh = AddBox(root, size, at, material); mesh.Name = name; return mesh; }
        if (installation is "chainLiftFeedBridge" or "chainLiftOutfeedBridge")
        {
            // Bridge roots are at the carrying height. The 150 mm plates
            // terminate at, rather than extend inside, the moving platform.
            Box("TRANSFER_bridge", new(.15f, .025f, 1.4f), new(0, -.0125f, 0), steel);
            foreach (var z in new[] { -.31f, .31f })
                Box($"TRANSFER_support_{z}", new(.12f, .18f, .08f), new(0, -.09f, z), dark);
        }
        else if (installation == "chainLiftLimits")
        {
            foreach (var y in new[] { .8f, 2.9f })
            {
                Box($"LIMIT_mount_{y}", new(.12f, .16f, .18f), new(0, y, 0), steel);
                Box($"LIMIT_body_{y}", new(.09f, .1f, .06f), new(-.13f, y, 0), dark);
                Box($"LIMIT_lever_{y}", new(.075f, .025f, .025f), new(-.0475f, y, 0), steel);
                AddCylinder(root, $"LIMIT_roller_{y}", new(-.015f, y, 0), .018f, .05f, steel, new(90, 0, 0));
            }
        }
        else if (installation == "chainLiftStop")
        {
            foreach (var z in new[] { -.82f, .82f })
            {
                Box($"STOP_ground_post_{z}", new(.09f, 3.1675f, .14f), new(.21f, -1.48375f, z), dark);
                Box($"STOP_ground_foot_{z}", new(.28f, .08f, .3f), new(.21f, -3.0275f, z), steel);
            }
            // Keep the ground posts beyond the receiving conveyor's bearing
            // housings. A crossbar and center arm carry the stop plate back
            // over the belt, with a clear carton-bearing surface below it.
            Box("STOP_crossbar", new(.09f, .08f, 1.8f), new(.21f, .055f, 0), steel);
            Box("STOP_center_arm", new(.27f, .08f, .12f), new(.105f, .055f, 0), steel);
            Box("STOP_bearing_plate", new(.075f, .11f, 1.26f), new(0, .055f, 0), steel);
        }
        else throw new InvalidOperationException($"Unknown chain lift scene installation '{installation}'.");
        return root;
    }
}
