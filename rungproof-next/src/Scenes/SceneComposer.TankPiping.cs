using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Installation geometry for the two tank lessons. Reuse the delivered
    // pump/spools at their original sizes; do not stretch flange diameters or
    // alter level/command ownership. Ports define layout, not hydraulic ratings.
    private static void ConfigureTankPiping(Node3D sceneRoot, SceneDefinition scene)
    {
        var tank = sceneRoot.GetNode<Node3D>(SafeNodeName(Text(scene.Simulation, "tankId", string.Empty)));
        var pump = sceneRoot.GetNode<Node3D>(SafeNodeName(Text(scene.Simulation, "pumpId", string.Empty)));
        Node3D Spool(string role) => sceneRoot.GetNode<Node3D>(SafeNodeName(scene.Equipment.Single(equipment =>
            Text(equipment.Config, "installation", string.Empty) == "tankPiping"
            && Text(equipment.Config, "role", string.Empty) == role).Id));
        var inlet = Spool("inlet"); var outlet = Spool("outlet");
        MeshInstance3D Part(Node node, string name) => node.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Tank piping requires '{node.Name}/{name}'.");
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != sceneRoot; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        Aabb Bounds(MeshInstance3D mesh) => InScene(mesh) * mesh.GetAabb();
        Vector3 Face(MeshInstance3D mesh, float sign) => InScene(mesh) *
            (mesh.GetAabb().GetCenter() + Vector3.Up * (sign * mesh.GetAabb().Size.Y / 2));
        var steel = Material(new Color("a5b3bb"), 0.7f, 0.23f);
        void Route(Node3D parent, string name, IEnumerable<Vector3> centers, float radius, float wall = 0.022f) =>
            AddSumpRoute(parent, name, centers.Select(point => InScene(parent).AffineInverse() * point).ToArray(), radius, steel, wall);
        var centre = Bounds(Part(tank, "TANK_shell")).GetCenter() with { Y = 0 };
        var inward = new Vector3(1, 0, 1).Normalized();
        const float inletY = 4.6f;
        // The diagonal inlet avoids the ladder on -X and sight glass on +X.
        pump.RotationDegrees = new Vector3(0, -45, 0);
        pump.Position = centre - inward * 7.56f;
        inlet.RotationDegrees = new Vector3(0, -45, 0);
        inlet.Position = centre;
        var tankInletFace = centre - inward * 1.93f + Vector3.Up * inletY;
        inlet.Position += tankInletFace - Face(Part(inlet, "FLANGE_1_72"), 1);
        Route(tank, "TANK_INLET_neck", new[] { centre - inward * 1.48f + Vector3.Up * inletY,
            centre - inward * 1.85f + Vector3.Up * inletY }, 0.18f);
        Route(tank, "TANK_INLET_flange", new[] { centre - inward * 1.85f + Vector3.Up * inletY,
            tankInletFace }, 0.31f, 0.152f);

        var discharge = Face(Part(pump, "PUMP_discharge_flange"), 1);
        var inletStart = Face(Part(inlet, "FLANGE_-1_72"), -1);
        const float bendRadius = 0.45f;
        var bendStart = discharge with { Y = inletStart.Y - bendRadius };
        var fill = new List<Vector3> { discharge, bendStart };
        for (var step = 1; step <= 24; step++)
        {
            var angle = step * Mathf.Pi / 48;
            fill.Add(bendStart + Vector3.Up * (bendRadius * MathF.Sin(angle))
                + inward * (bendRadius * (1 - MathF.Cos(angle))));
        }
        fill.Add(inletStart);
        Route(pump, "TANK_FILL", fill, 0.15f);
        // Make the scene's supply boundary explicit rather than leaving the
        // pump's suction port visibly disconnected from any line.
        var suction = Face(Part(pump, "PUMP_suction_flange"), 1);
        var side = new Vector3(inward.Z, 0, -inward.X);
        var supplyBend = suction + inward * 0.2f;
        var supply = new List<Vector3> { suction, supplyBend };
        for (var step = 1; step <= 24; step++)
        {
            var angle = step * Mathf.Pi / 48;
            supply.Add(supplyBend + inward * (bendRadius * MathF.Sin(angle))
                + side * (bendRadius * (1 - MathF.Cos(angle))));
        }
        var supplyEnd = supply[^1] + side * 0.8f;
        supply.Add(supplyEnd);
        Route(pump, "TANK_SUPPLY", supply, 0.18f);
        var supplySaddle = supplyEnd - side * 0.25f;
        var support = new Node3D { Name = "TANK_supply_support", Position = supplySaddle with { Y = 0 } };
        sceneRoot.AddChild(support);
        AddBox(support, new Vector3(0.36f, 0.07f, 0.36f), new Vector3(0, 0.035f, 0), steel).Name = "TANK_supply_foot";
        AddBox(support, new Vector3(0.10f, supplySaddle.Y - 0.29f, 0.10f),
            new Vector3(0, (supplySaddle.Y - 0.29f) / 2 + 0.06f, 0), steel).Name = "TANK_supply_post";
        AddBox(support, new Vector3(0.30f, 0.06f, 0.30f), new Vector3(0, supplySaddle.Y - 0.21f, 0), steel).Name = "TANK_supply_saddle";

        // The existing tank outlet is +Z. Rotate the source X spool to that
        // bore and mate its near flange to the measured delivered flange face.
        outlet.RotationDegrees = new Vector3(0, -90, 0);
        outlet.Position = centre;
        outlet.Position += Face(Part(tank, "NOZZLE_outlet_flange"), 1) - Face(Part(outlet, "FLANGE_-1_72"), -1);
        GroundSupports(inlet); GroundSupports(outlet);
        var drainEnd = Face(Part(outlet, "FLANGE_1_72"), 1);
        var valveEquipment = scene.Equipment.SingleOrDefault(equipment =>
            Text(equipment.Config, "installation", string.Empty) == "tankDrainValve");
        if (valveEquipment is not null)
        {
            var valve = sceneRoot.GetNode<Node3D>(SafeNodeName(valveEquipment.Id));
            valve.RotationDegrees = new Vector3(0, -90, 0);
            valve.Position = centre;
            var liner = Part(valve, "PROCESS_bore_liner");
            // Keep the verified tank/spool joint. Append the full delivered
            // valve, measuring its continuous bore rather than guessing a root.
            valve.Position += drainEnd - Face(liner, -1);
            Route(valve, "TANK_DRAIN_mating_flange", new[] { drainEnd, drainEnd + Vector3.Back * 0.09f }, 0.31f, 0.17f);
            GroundSupports(valve, "PIPE_foot", "PIPE_shoe_post", "PIPE_anchor");
            var controller = valve.GetNode<EquipmentMotionController>("PositionRotationController");
            // Authored pointer parallel to bore = OPEN. Existing tank physics
            // applies the BOOL command immediately; project that same state.
            controller.PositionInputInverted = true;
            controller.AutonomousPositionTravel = false;
            drainEnd = Face(liner, 1);
        }
        Boundary("TANK_supply_boundary", "FROM SUPPLY", supplyEnd + Vector3.Up * 0.38f);
        Boundary("TANK_drain_boundary", "TO DRAIN", drainEnd + Vector3.Up * 0.40f);

        void GroundSupports(Node3D spool, string footPrefix = "SUPPORT_foot", string postPrefix = "SUPPORT_post", string? anchorPrefix = null)
        {
            var meshes = spool.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
            var feet = meshes.Where(mesh => mesh.Name.ToString().StartsWith(footPrefix, StringComparison.Ordinal)).ToArray();
            foreach (var foot in feet)
            {
                var drop = Bounds(foot).Position.Y;
                foot.Position -= Vector3.Up * drop;
                if (anchorPrefix is not null)
                    foreach (var anchor in meshes.Where(mesh => mesh.Name.ToString().StartsWith(anchorPrefix, StringComparison.Ordinal)))
                        if (feet.OrderBy(other => new Vector2(other.Position.X - anchor.Position.X,
                            other.Position.Z - anchor.Position.Z).LengthSquared()).First() == foot)
                            anchor.Position -= Vector3.Up * drop;
            }
            foreach (var post in meshes.Where(mesh => mesh.Name.ToString().StartsWith(postPrefix, StringComparison.Ordinal)))
            {
                var bounds = Bounds(post);
                var bottom = 0.06f; // 10 mm overlap with the 70 mm ground shoe.
                post.Scale = post.Scale with { Y = post.Scale.Y * (bounds.End.Y - bottom) / bounds.Size.Y };
                post.Position -= Vector3.Up * (bounds.Position.Y - bottom) / 2;
            }
        }
        void Boundary(string name, string text, Vector3 position) => sceneRoot.AddChild(new Label3D
        {
            Name = name, Text = text, Position = position, FontSize = 48, PixelSize = 0.004f,
            OutlineSize = 6, Modulate = Colors.White, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        });
    }
}
