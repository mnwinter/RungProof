using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local visual installation. Port continuity is not hydraulic sizing or flow simulation.
    private static void ConfigureWastewaterPiping(Node3D scene)
    {
        var manifold = scene.GetNode<Node3D>("training_accessory_8");
        manifold.Position = Vector3.Zero;
        var pump = scene.GetNode<Node3D>("pump_3");
        var valve = scene.GetNode<Node3D>("valve_4");
        MeshInstance3D Part(Node node, string name) => node.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Wastewater installation requires {node.Name}/{name}.");
        Transform3D Transform(Node3D node)
        {
            var result = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != scene; parent = parent.GetParent() as Node3D)
                result = parent.Transform * result;
            return result;
        }
        Vector3 Face(MeshInstance3D part, float sign) => Transform(part) *
            (part.GetAabb().GetCenter() + Vector3.Up * (sign * part.GetAabb().Size.Y / 2));
        Aabb Bounds(MeshInstance3D part) => Transform(part) * part.GetAabb();
        var steel = Material(new Color("a5b3bb"), .7f, .23f);
        void Route(string name, IReadOnlyList<Vector3> path, float radius) => AddSumpRoute(manifold, name, path, radius, steel);
        pump.RotationDegrees = new Vector3(0, 180, 0);
        pump.Position = new Vector3(10, 0, 3);
        var suction = Face(Part(pump, "PUMP_suction_flange"), 1);
        var outlets = new[] { "tank_0", "tank_1", "tank_2", "training_accessory_6" }
            .Select(id => Face(Part(scene.GetNode<Node3D>(id), "NOZZLE_outlet_flange"), 1)).ToArray();
        const float radius = .45f;
        // First branch turns into the common header; the other branches terminate at its tees.
        var first = outlets[0];
        var bend = new Vector3(first.X, suction.Y, 3 - radius);
        var firstPath = new List<Vector3> { first, first + Vector3.Back * .25f, bend };
        for (var step = 1; step <= 24; step++)
        {
            var angle = step * Mathf.Pi / 48;
            firstPath.Add(bend + Vector3.Right * (radius * (1 - MathF.Cos(angle))) + Vector3.Back * (radius * MathF.Sin(angle)));
        }
        firstPath.Add(suction);
        Route("WASTEWATER_collector", firstPath, .15f);
        for (var index = 1; index < outlets.Length; index++)
        {
            var outlet = outlets[index];
            Route($"WASTEWATER_branch_{index}", new[] { outlet, outlet + Vector3.Back * .25f,
                new Vector3(outlet.X, suction.Y, 3) }, .15f);
        }
        // Preserve each real port's tangent: pump discharge UP, installed valve bore +Z.
        var discharge = Face(Part(pump, "PUMP_discharge_flange"), 1);
        valve.RotationDegrees = new Vector3(0, -90, 0);
        valve.Position = new Vector3(discharge.X, 1.30f, 5.9f);
        var inlet = Face(Part(valve, "PROCESS_bore_liner"), -1);
        var elbowStart = discharge with { Y = inlet.Y - radius };
        var dischargePath = new List<Vector3> { discharge, elbowStart };
        for (var step = 1; step <= 24; step++)
        {
            var angle = step * Mathf.Pi / 48;
            dischargePath.Add(elbowStart + Vector3.Up * (radius * MathF.Sin(angle)) + Vector3.Back * (radius * (1 - MathF.Cos(angle))));
        }
        dischargePath.Add(inlet);
        Route("WASTEWATER_discharge", dischargePath, .145f);
        var outletFace = Face(Part(valve, "PROCESS_bore_liner"), 1);
        Route("WASTEWATER_treatment_boundary", new[] { outletFace, outletFace + Vector3.Back * 1.2f }, .145f);
        manifold.AddChild(new Label3D { Name = "TreatmentBoundary", Text = "TO TREATMENT", Position = outletFace + new Vector3(0, .4f, 1.2f),
            FontSize = 48, PixelSize = .003f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled });
        // Extend existing valve shoe posts; feet and anchors stay on the floor.
        var valveFeet = valve.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(mesh => mesh.Name.ToString().StartsWith("PIPE_foot", StringComparison.Ordinal)).ToArray();
        var valveFootDrop = valveFeet.Min(foot => Bounds(foot).Position.Y);
        foreach (var mesh in valve.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            var name = mesh.Name.ToString(); var bounds = Bounds(mesh);
            if (name.StartsWith("PIPE_foot", StringComparison.Ordinal) || name.StartsWith("PIPE_anchor", StringComparison.Ordinal))
                mesh.Position -= Vector3.Up * valveFootDrop;
            if (name.StartsWith("PIPE_shoe_post", StringComparison.Ordinal))
            {
                var bottom = .06f;
                mesh.Scale = mesh.Scale with { Y = mesh.Scale.Y * (bounds.End.Y - bottom) / bounds.Size.Y };
                mesh.Position -= Vector3.Up * (bounds.Position.Y - bottom) / 2;
            }
        }
        foreach (var x in new[] { -5f, -1f, 3f, 7f })
        {
            var height = suction.Y - .15f;
            AddBox(manifold, new Vector3(.4f, .07f, .4f), new Vector3(x, .035f, 3), steel).Name = $"WASTEWATER_header_foot_{x}";
            AddBox(manifold, new Vector3(.10f, height - .07f, .10f), new Vector3(x, (height + .07f) / 2, 3), steel).Name = $"WASTEWATER_header_post_{x}";
        }
    }
}
