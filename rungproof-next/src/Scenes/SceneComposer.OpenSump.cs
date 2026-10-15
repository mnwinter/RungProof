using System;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Above-floor illustrative collection basin. Four actual walls and a floor
    // leave the top open; a solid primitive must not stand in for its cavity.
    // Dimensions are scene geometry, not civil or pump-installation engineering.
    private static Node3D CreateOpenSump()
    {
        var root = new Node3D();
        var concrete = Material(new Color("879399"), 0, 0.9f);
        var steel = Material(new Color("a5b3bb"), 0.7f, 0.23f);
        AddBox(root, new Vector3(3.2f, .12f, 3.2f), new Vector3(0, .06f, 0), concrete).Name = "SUMP_floor";
        // TANK_shell remains the bounds datum expected by the geometry audit.
        // Join four wall boxes into a single mesh, leaving the centre empty.
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var (size, position) in new[] {
            (new Vector3(.14f,1.8f,3.2f),new Vector3(-1.53f,1.02f,0)),
            (new Vector3(.14f,1.8f,3.2f),new Vector3(1.53f,1.02f,0)),
            (new Vector3(2.92f,1.8f,.14f),new Vector3(0,1.02f,-1.53f)),
            (new Vector3(2.92f,1.8f,.14f),new Vector3(0,1.02f,1.53f)),
        })
        {
            var box = new BoxMesh { Size = size };
            surface.AppendFrom(box, 0, new Transform3D(Basis.Identity, position));
        }
        root.AddChild(new MeshInstance3D { Name="TANK_shell", Mesh=surface.Commit(), MaterialOverride=concrete });
        // Full-height authored liquid is centred about its midpoint so the
        // existing runtime can scale it from its fixed bottom at y=.12.
        AddBox(root, new Vector3(2.90f,1.65f,2.90f), new Vector3(0,.945f,0),
            Material(new Color("1597ad"),0,.25f)).Name="KIN_liquid";
        // The scene rotates this local +Z outlet onto its pump-facing -X wall.
        AddCylinder(root,"SUMP_outlet_neck",new Vector3(0,.6f,1.61f),.18f,.30f,steel,new Vector3(90,0,0));
        AddCylinder(root,"NOZZLE_outlet_flange",new Vector3(0,.6f,1.78f),.25f,.08f,steel,new Vector3(90,0,0));
        return root;
    }

    private static void ConfigureSumpFloats(Node3D sceneRoot, SceneDefinition scene)
    {
        var steel=Material(new Color("a5b3bb"),.7f,.23f);
        foreach(var (id,field,defaultThreshold,z) in new[] {
            ("low_float","lowThreshold",.2,-.55f),
            ("high_float","highThreshold",.78,.55f),
        })
        {
            var sensor=sceneRoot.GetNode<Node3D>(id);
            // Replace the fork model only in this sump installation. The root
            // stays intact so the existing levelSensor binding remains valid.
            foreach(var child in sensor.GetChildren()) { sensor.RemoveChild(child); child.Free(); }
            sensor.RotationDegrees=Vector3.Zero;
            var threshold=(float)Number(scene.Simulation,field,defaultThreshold);
            if(!float.IsFinite(threshold)||threshold<=0||threshold>=1)
                throw new InvalidOperationException($"Sump float {id} requires a normalized threshold.");
            var elevation=.12f+1.65f*threshold;
            sensor.Position=new Vector3(1.30f,elevation,z);
            AddBox(sensor,new Vector3(.44f,.08f,.20f),new Vector3(.20f,1.92f-elevation,0),steel).Name="SUMP_FLOAT_bracket";
            AddBox(sensor,new Vector3(.18f,.18f,.16f),new Vector3(.40f,2.01f-elevation,0),steel).Name="SUMP_FLOAT_head";
            var stemHeight=1.92f-elevation+.14f;
            AddCylinder(sensor,"SUMP_FLOAT_stem",new Vector3(0,(1.92f-elevation-.14f)/2,0),.025f,stemHeight,steel);
            // Illustrative captive float: +/-80 mm about the switch datum.
            // Runtime projects water level onto this bounded stem travel.
            sensor.SetMeta("sump_float_visual_travel_m", .08f);
            sensor.AddChild(new MeshInstance3D { Name="SUMP_FLOAT_body",
                Mesh=new SphereMesh { Radius=.11f, Height=.22f },
                MaterialOverride=Material(new Color("e4b442"),.15f,.4f) });
        }
    }
}
