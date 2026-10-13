using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateBarrelFillPart(string installation)
    {
        var root = new Node3D();
        var steel = Material(new Color("aebbc0"), .65f, .3f);
        var blue = Material(new Color("177fb8"), .2f, .4f);
        var clear = Material(new Color("a7c7d4"), .1f, .35f, true, .25f);
        var water = Material(new Color("32a6d1"), .05f, .3f);
        void Box(string name, Vector3 size, Vector3 at, Material mat) => AddBox(root, size, at, mat).Name = name;
        void Pipe(string name, Vector3 from, Vector3 to, float radius = .045f) => AddSumpRoute(root, name, new[] { from, to }, radius, steel, .007f);
        void Label(string name, string text, Vector3 at) => root.AddChild(new Label3D
        { Name = name, Text = text, Position = at, FontSize = 42, PixelSize = .0018f, OutlineSize = 0 });
        if (installation == "barrelLoad")
        {
            // Open-top cutaway barrel: walls are a tube, not a solid cylinder
            // through which the liquid or nozzle would falsely pass.
            AddSumpRoute(root, "BARREL_wall", new[] { new Vector3(0, .025f, 0), new Vector3(0, .9f, 0) }, .3f, clear, .02f);
            AddCylinder(root, "BARREL_bottom", new(0, .0125f, 0), .3f, .025f, steel);
            foreach (var y in new[] { .035f, .25f, .65f, .89f })
                AddSumpRoute(root, $"BARREL_band_{y}", new[] { new Vector3(0, y - .008f, 0), new Vector3(0, y + .008f, 0) }, .305f, steel, .018f);
            AddCylinder(root, "KIN_barrel_liquid", new(0, .425f, 0), .28f, .8f, water);
            Label("BARREL_label", "150 L BATCH\nCUTAWAY", new(0, .5f, .312f));
        }
        else if (installation == "barrelSupply")
        {
            AddSumpRoute(root, "SUPPLY_wall", new[] { new Vector3(0, 2.5f, 0), new Vector3(0, 3.4f, 0) }, .55f, clear, .05f);
            AddCylinder(root, "SUPPLY_bottom", new(0, 2.475f, 0), .55f, .05f, steel);
            AddCylinder(root, "KIN_supply_liquid", new(0, 2.9f, 0), .5f, .8f, water);
            foreach (var x in new[] { -.48f, .48f })
            foreach (var z in new[] { -.48f, .48f })
            {
                Box($"SUPPLY_foot_{x}_{z}", new(.22f, .08f, .22f), new(x, .04f, z), steel);
                Box($"SUPPLY_leg_{x}_{z}", new(.07f, 2.395f, .07f), new(x, 1.2775f, z), blue);
            }
            var outlet = new System.Collections.Generic.List<Vector3> { new(0, 2.55f, .5f), new(0, 2.55f, .6f) };
            for (var step = 1; step <= 16; step++)
            {
                var u = step / 16f;
                outlet.Add(new(0, 2.55f - .1f * u * u * (3 - 2 * u), .6f + .35f * u));
            }
            outlet.Add(new(0, 2.45f, 1.05f));
            AddSumpRoute(root, "SUPPLY_outlet", outlet, .045f, steel, .007f);
            Label("SUPPLY_label", "SOURCE\n200 L INITIAL", new(0, 3.1f, .56f));
        }
        else if (installation == "barrelValve")
        {
            Pipe("VALVE_bore", new(0, 2.45f, -.2f), new(0, 2.45f, .2f), .075f);
            // Thick, through-bored sockets bridge the retained unequal pipe
            // annuli. A 1mm enlarged inner step avoids coincident inner faces;
            // this changes presentation only, never plant flow or valve state.
            foreach(var (name, z) in new[]{("VALVE_inlet_reducing_socket",-.2f),("VALVE_outlet_reducing_socket",.2f)})
                AddSumpRoute(root,name,new[]{new Vector3(0,2.45f,z-.04f),new Vector3(0,2.45f,z+.04f)},.083f,steel,.083f-.039f);
            Box("VALVE_actuator", new(.22f, .14f, .22f), new(0, 2.62f, 0), blue);
            // Seat on the curved outer wall above the .068m fluid lumen;
            // retain actuator and pointer poses while connecting both bearings.
            AddCylinder(root,"VALVE_actuator_stem",new(0,2.620f,0),.018f,.200f,steel);
            var pointer = new Node3D { Name = "KIN_valve_pointer", Position = new(0, 2.72f, 0) }; root.AddChild(pointer);
            AddBox(pointer, new(.04f, .03f, .30f), Vector3.Zero, Material(new Color("e8bc39"), .1f, .4f)).Name = "VALVE_pointer";
            // Offset the ground support from the fill photoeye's X=0 ray.
            Box("VALVE_foot", new(.32f, .08f, .32f), new(.45f, .04f, 0), steel);
            Box("VALVE_post", new(.08f, 2.31f, .08f), new(.45f, 1.235f, 0), blue);
            Box("VALVE_saddle", new(.52f, .06f, .12f), new(.225f, 2.37f, 0), steel);
        }
        else if (installation == "barrelMeter")
        {
            Pipe("METER_inlet", new(0, 2.45f, -.4f), new(0, 2.45f, -.15f));
            Pipe("METER_bore", new(0, 2.45f, -.15f), new(0, 2.45f, .15f), .08f);
            Pipe("METER_outlet", new(0, 2.45f, .15f), new(0, 2.45f, .3f));
            foreach(var (name, z) in new[]{("METER_inlet_reducing_socket",-.15f),("METER_outlet_reducing_socket",.15f)})
                AddSumpRoute(root,name,new[]{new Vector3(0,2.45f,z-.04f),new Vector3(0,2.45f,z+.04f)},.088f,steel,.088f-.039f);
            Box("METER_display", new(.55f, .32f, .09f), new(0, 2.7f, 0), blue);
            AddCylinder(root, "METER_stem", new(0, 2.54f, 0), .025f, .18f, steel);
            Label("NumericReadout", "LITRES\n0", new(0, 2.7f, .05f));
        }
        else if (installation == "barrelNozzle")
        {
            // A supported high pipe stays clear of the moving barrel rim.
            foreach (var z in new[] { -1.75f, 1.75f })
            {
                Box($"FILL_foot_{z}", new(.3f, .08f, .3f), new(.85f, .04f, z), steel);
                Box($"FILL_post_{z}", new(.08f, 2.68f, .08f), new(.85f, 1.42f, z), blue);
            }
            Box("FILL_header", new(.12f, .12f, 3.6f), new(.85f, 2.82f, 0), steel);
            Box("FILL_pipe_clamp", new(.9f, .08f, .10f), new(.41f, 2.39f, -.2f), blue);
            Box("FILL_clamp_hanger", new(.05f, .33f, .05f), new(.85f, 2.595f, -.2f), steel);
            var curve = new System.Collections.Generic.List<Vector3> { new(0, 2.45f, -.6f), new(0, 2.45f, -.2f) };
            for (var step = 1; step <= 16; step++)
            {
                var angle = step * MathF.PI / 32;
                curve.Add(new(0, 2.25f + .2f * MathF.Cos(angle), -.2f + .2f * MathF.Sin(angle)));
            }
            curve.Add(new(0, 2.1f, 0));
            AddSumpRoute(root, "FILL_nozzle", curve, .045f, steel, .007f);
            AddCylinder(root, "KIN_fill_stream", new(0, 1.51f, 0), .018f, 1.18f, water);
            Label("FILL_label", "BARREL FILL", new(.85f, 2.83f, 1.82f));
        }
        else throw new InvalidOperationException($"Unknown barrel installation '{installation}'.");
        return root;
    }

    private static void ConfigureBarrelConveyor(Node3D sceneRoot)
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
