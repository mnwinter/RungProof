using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Purpose-specific scene props, rather than relabeling the catalog's meat
    // tray, milling machine and pneumatic pusher as cookie equipment.
    private static Node3D CreateCookiePackagingPart(string installation)
    {
        var root = new Node3D();
        var steel = Material(new Color("aebbc0"), .65f, .3f);
        var dark = Material(new Color("283840"), .25f, .4f);
        var blue = Material(new Color("177fb8"), .2f, .4f);
        MeshInstance3D Box(Node3D parent, string name, Vector3 size, Vector3 at, Material mat)
        { var part = AddBox(parent, size, at, mat); part.Name = name; return part; }
        if (installation == "cookieTray")
        {
            Box(root, "COOKIE_tray", new(.32f, .01f, .32f), new(0, .005f, 0), steel);
            AddCylinder(root, "COOKIE_biscuit", new(0, .03f, 0), .115f, .04f, Material(new Color("dca356"), 0, .9f), Vector3.Zero);
            var chocolate = Material(new Color("4d2b18"), 0, .95f);
            for (var chip = 0; chip < 8; chip++)
            {
                var angle = MathF.Tau * chip / 8;
                AddCylinder(root, $"COOKIE_chip_{chip}", new(.067f * MathF.Cos(angle), .0505f, .067f * MathF.Sin(angle)), .009f, .003f, chocolate, Vector3.Zero);
            }
            var film = new Node3D { Name = "COOKIE_package", Visible = false }; root.AddChild(film);
            Box(film, "COOKIE_film", new(.34f, .075f, .34f), new(0, .0475f, 0), Material(new Color("c8eaf2"), .05f, .3f, true, .22f));
            foreach (var x in new[] { -.17f, .17f })
                Box(film, $"COOKIE_end_seal_{x}", new(.015f, .006f, .34f), new(x, .018f, 0), steel);
        }
        else if (installation == "cookieSealer")
        {
            foreach (var z in new[] { -.83f, .83f })
            {
                Box(root, $"SEAL_foot_{z}", new(.32f, .08f, .34f), new(0, .04f, z), steel);
                Box(root, $"SEAL_post_{z}", new(.1f, 1.45f, .1f), new(0, .805f, z), blue);
            }
            Box(root, "SEAL_header", new(.34f, .14f, 1.76f), new(0, 1.53f, 0), steel);
            Box(root, "SEAL_actuator", new(.13f, .24f, .13f), new(0, 1.35f, 0), blue);
            var head = new Node3D { Name = "KIN_cookie_seal_head" }; root.AddChild(head);
            Box(head, "SEAL_rod", new(.035f, .30f, .035f), new(0, 1.32f, 0), steel);
            Box(head, "SEAL_carrier", new(.43f, .04f, .43f), new(0, 1.22f, 0), dark);
            // Ring jaws surround the biscuit. Their fully-down bearing face
            // is above the conveyor, and no crossbar sweeps through the food.
            foreach (var x in new[] { -.1925f, .1925f })
                Box(head, $"SEAL_jaw_x_{x}", new(.035f, .05f, .42f), new(x, 1.16f, 0), steel);
            foreach (var z in new[] { -.1925f, .1925f })
                Box(head, $"SEAL_jaw_z_{z}", new(.35f, .05f, .035f), new(0, 1.16f, z), steel);
            var label = new Label3D { Text = "COOKIE SEALER", Position = new(0, 1.53f, .9f), FontSize = 40, PixelSize = .0015f, OutlineSize = 0 };
            root.AddChild(label);
        }
        else if (installation == "cookieCounter")
        {
            Box(root, "COUNT_foot", new(.44f, .08f, .42f), new(0, .04f, 0), steel);
            Box(root, "COUNT_mast", new(.08f, 1.10f, .08f), new(0, .63f, 0), dark);
            Box(root, "COUNT_display", new(.64f, .34f, .1f), new(0, 1.35f, 0), blue);
            Box(root, "COUNT_screen", new(.58f, .28f, .008f), new(0, 1.35f, .054f), dark);
            root.AddChild(new Label3D { Name = "NumericReadout", Text = "COUNT\n0", Position = new(0, 1.35f, .06f), FontSize = 42, PixelSize = .0016f, OutlineSize = 0, Modulate = Colors.White });
        }
        else throw new InvalidOperationException($"Unknown cookie installation '{installation}'.");
        return root;
    }

    private static void ConfigureCookieConveyor(Node3D sceneRoot)
    {
        var conveyor = sceneRoot.GetNode<Node3D>("conveyor_0");
        Transform3D InScene(Node3D node)
        {
            var pose = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != sceneRoot; parent = parent.GetParent() as Node3D) pose = parent.Transform * pose;
            return pose;
        }
        var parts = conveyor.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        var belt = parts.Single(mesh => mesh.Name == "KIN_belt_surface");
        var splice = parts.Single(mesh => mesh.Name == "BELT_vulcanized_splice");
        var rise = (InScene(splice) * splice.GetAabb()).End.Y - (InScene(belt) * belt.GetAabb()).End.Y;
        splice.Position -= InScene((Node3D)splice.GetParent()).Basis.Inverse() * (Vector3.Up * rise);
    }
}
