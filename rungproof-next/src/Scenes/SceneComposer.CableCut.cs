using System;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateCableCutPart(string installation)
    {
        var root = new Node3D();
        var steel = Material(new Color("aebbc0"), .65f, .3f);
        var blue = Material(new Color("177fb8"), .2f, .4f);
        var dark = Material(new Color("283840"), .1f, .6f);
        var cable = Material(new Color("df9b27"), .05f, .65f);
        void Box(Node3D parent, string name, Vector3 size, Vector3 at, Material mat)
        { AddBox(parent, size, at, mat).Name = name; }
        void Label(string text, Vector3 at) => root.AddChild(new Label3D
        { Text = text, Position = at, FontSize = 42, PixelSize = .0015f, OutlineSize = 0 });
        void Stand(float width, float depth, float top)
        {
            foreach (var x in new[] { -width / 2, width / 2 })
            foreach (var z in new[] { -depth / 2, depth / 2 })
            {
                Box(root, $"SUPPORT_foot_{x}_{z}", new(.18f, .08f, .18f), new(x, .04f, z), steel);
                Box(root, $"SUPPORT_leg_{x}_{z}", new(.05f, top - .08f, .05f), new(x, (top + .08f) / 2, z), blue);
            }
        }
        void Roller(Node3D parent, string name, Vector3 at, float radius, float width)
        {
            var axis = new Node3D { Name = name, Position = at }; parent.AddChild(axis);
            AddCylinder(axis, name + "_body", Vector3.Zero, radius, width, dark, new Vector3(90, 0, 0));
            Box(axis, name + "_witness", new(.018f, radius, .01f), new(0, radius / 2, width / 2 + .005f), cable);
        }
        if (installation == "cablePayoff")
        {
            // Prethreaded cable leaves the constant-radius winding tangentially
            // at Y=1.1. Two bearings support the Z-oriented reel shaft.
            Stand(.65f, .85f, .75f);
            foreach (var z in new[] { -.425f, .425f })
                Box(root, $"REEL_bearing_{z}", new(.22f, .18f, .10f), new(0, .75f, z), steel);
            var reel = new Node3D { Name = "KIN_payoff", Position = new(0, .75f, 0) }; root.AddChild(reel);
            AddCylinder(reel, "REEL_winding", Vector3.Zero, .336f, .58f, cable, new Vector3(90, 0, 0));
            foreach (var z in new[] { -.31f, .31f })
            {
                AddCylinder(reel, $"REEL_flange_{z}", new(0, 0, z), .45f, .04f, blue, new Vector3(90, 0, 0));
                Box(reel, $"REEL_witness_{z}", new(.025f, .34f, .01f), new(0, .17f, z + MathF.Sign(z) * .025f), steel);
            }
            AddCylinder(root, "REEL_shaft", new(0, .75f, 0), .04f, .95f, steel, new Vector3(90, 0, 0));
            Label("PAYOFF\nFIXED RADIUS", new(0, .75f, .52f));
        }
        else if (installation is "cableGuide" or "cableEncoder" or "cableFeed")
        {
            Stand(.4f, .5f, .83f);
            foreach (var z in new[] { -.29f, .29f })
                Box(root, $"ROLLER_bed_{z}", new(.55f, .04f, .08f), new(0, .85f, z), steel);
            var radius = installation == "cableEncoder" ? .08f : .12f;
            foreach (var z in new[] { -.29f, .29f })
                Box(root, $"ROLLER_pillar_{z}", new(.08f, .48f, .08f), new(0, 1.11f, z), blue);
            foreach (var y in new[] { 1.086f - radius, 1.114f + radius })
            {
                foreach (var z in new[] { -.24f, .24f })
                    Box(root, $"ROLLER_bearing_{y}_{z}", new(.08f, .08f, .08f), new(0, y, z), steel);
                AddCylinder(root, $"ROLLER_shaft_{y}", new(0, y, 0), .025f, .56f, steel, new Vector3(90, 0, 0));
            }
            Roller(root, "KIN_lower_roll", new(0, 1.086f - radius, 0), radius, .36f);
            Roller(root, "KIN_upper_roll", new(0, 1.114f + radius, 0), radius, .36f);
            if (installation == "cableEncoder")
            {
                Box(root, "ENCODER_housing", new(.16f, .16f, .12f), new(0, 1.006f, .34f), blue);
                AddCylinder(root, "ENCODER_shaft", new(0, 1.006f, .25f), .025f, .22f, steel, new Vector3(90, 0, 0));
            }
            if (installation == "cableFeed")
            {
                Box(root, "FEED_motor_mount", new(.24f, .08f, .25f), new(0, .91f, .41f), steel);
                Box(root, "FEED_motor", new(.2f, .2f, .28f), new(0, 1.05f, .44f), blue);
                AddCylinder(root, "FEED_coupling", new(0, .966f, .29f), .04f, .10f, steel, new Vector3(90, 0, 0));
                Box(root, "FEED_motor_bracket", new(.08f, .06f, .22f), new(0, .91f, .32f), steel);
            }
            Label(installation == "cableEncoder" ? "LENGTH ENCODER" : installation == "cableFeed" ? "FEED ROLLS" : "PASSIVE GUIDE", new(0, .85f, .33f));
        }
        else if (installation == "cableCutter")
        {
            foreach (var z in new[] { -.45f, .45f })
            {
                Box(root, $"KNIFE_foot_{z}", new(.34f, .08f, .24f), new(0, .04f, z), steel);
                Box(root, $"KNIFE_post_{z}", new(.1f, 1.82f, .1f), new(0, .99f, z), blue);
            }
            Box(root, "KNIFE_header", new(.32f, .12f, 1), new(0, 1.94f, 0), steel);
            Box(root, "KNIFE_actuator", new(.15f, .60f, .15f), new(0, 1.95f, 0), blue);
            foreach (var x in new[] { -.215f, .215f })
                Box(root, $"KNIFE_slotted_anvil_{x}", new(.37f, .05f, .70f), new(x, 1.061f, 0), steel);
            foreach (var z in new[] { -.45f, .45f })
                Box(root, $"KNIFE_support_bracket_{z}", new(.35f, .06f, .1f), new(.125f, 1.006f, z), blue);
            Box(root, "KNIFE_anvil_support", new(.10f, .06f, 1), new(.25f, 1.006f, 0), blue);
            var blade = new Node3D { Name = "KIN_cutter_blade" }; root.AddChild(blade);
            Box(blade, "KNIFE_blade", new(.04f, .22f, .60f), new(0, 1.53f, 0), steel);
            Box(blade, "KNIFE_rod", new(.03f, .60f, .03f), new(0, 1.905f, 0), steel);
            Label("CUTTER\n3.00 m TARGET", new(0, 1.94f, .53f));
        }
        else if (installation == "cableReceiver")
        {
            Stand(3.15f, .65f, 1.036f);
            // Bearing surface tangent to the cable, with no raised witness
            // through the received piece. Rails sit beside the cable.
            Box(root, "RECEIVER_surface", new(3.3f, .05f, .55f), new(0, 1.061f, 0), steel);
            foreach (var z in new[] { -.30f, .30f })
                Box(root, $"RECEIVER_rail_{z}", new(3.3f, .12f, .04f), new(0, 1.096f, z), blue);
            Label("RETAINED CUT PIECE", new(0, .95f, .34f));
        }
        else if (installation == "cableStrand")
        {
            // Cylinders along X. Runtime stretches the received length and
            // reveals a 60 mm separation after the prescribed completed cut.
            AddCylinder(root, "CABLE_upstream", new(-2.75f, 1.1f, 0), .014f, 5.5f, cable, new Vector3(0, 0, 90));
            AddCylinder(root, "KIN_cable_piece", new(.5f, 1.1f, 0), .014f, 1, cable, new Vector3(0, 0, 90));
        }
        else throw new InvalidOperationException($"Unknown cable installation '{installation}'.");
        return root;
    }
}
