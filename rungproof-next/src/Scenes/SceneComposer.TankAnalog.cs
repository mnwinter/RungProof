using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureTankAnalogMount(Node3D sceneRoot, SceneDefinition scene)
    {
        var tank = sceneRoot.GetNode<Node3D>(SafeNodeName(Text(scene.Simulation, "tankId", string.Empty)));
        var sensor = sceneRoot.GetNode<Node3D>(SafeNodeName(Text(scene.Simulation, "transmitterId", string.Empty)));
        MeshInstance3D Part(Node node, string name) => node.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Tank analog mounting requires '{node.Name}/{name}'.");
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != sceneRoot; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        Aabb Bounds(MeshInstance3D mesh) => InScene(mesh) * mesh.GetAabb();
        var roof = Bounds(Part(tank, "TANK_roof"));
        var liquid = Bounds(Part(tank, "KIN_liquid"));
        var flange = Part(sensor, "PROCESS_flange");
        sensor.RotationDegrees = Vector3.Zero;
        // Keep the central manway and outer guardrail clear. This is an
        // explicit scene installation; the reusable transmitter stays intact.
        var config = scene.Equipment.Single(equipment => equipment.Id == Text(scene.Simulation, "transmitterId", string.Empty)).Config;
        sensor.Position = new Vector3(roof.GetCenter().X + (float)Number(config, "roofOffsetX", 0.35),
            roof.End.Y + 0.15f, roof.GetCenter().Z + (float)Number(config, "roofOffsetZ", -0.85));
        var socketBottom = new Vector3(sensor.Position.X, roof.End.Y - 0.02f, sensor.Position.Z);
        var socketTop = socketBottom with { Y = Bounds(flange).Position.Y };
        AddSumpRoute(tank, "TANK_ANALOG_socket", new[] { socketBottom - tank.Position, socketTop - tank.Position },
            0.18f, flange.GetActiveMaterial(0));

        // The delivered 2.6 m probe is too short for this installed tank.
        // Extend only the sensing rod, retaining its diameter, fitting/head
        // and tip weight. Match the modeled zero-level datum; no purchased
        // probe compatibility or hardware calibration is implied.
        var tip = Part(sensor, "PROBE_tip_weight");
        var tipCenter = new Vector3(sensor.Position.X, liquid.Position.Y + Bounds(tip).Size.Y / 2, sensor.Position.Z);
        var parent = tip.GetParent<Node3D>();
        tip.Position += InScene(parent).AffineInverse() * tipCenter - (tip.Transform * tip.GetAabb()).GetCenter();
        var rod = Part(sensor, "PROBE_sensing_rod");
        var rodBounds = rod.Transform * rod.GetAabb();
        var tipBounds = tip.Transform * tip.GetAabb();
        var bottom = tipBounds.End.Y - 0.025f;
        var size = rodBounds.Size with { Y = rodBounds.End.Y - bottom };
        if (size.Y <= 0) throw new InvalidOperationException("Tank analog probe has no positive installed length.");
        var center = rodBounds.GetCenter() with { Y = (rodBounds.End.Y + bottom) / 2 };
        var basis = Basis.FromScale(size / rodBounds.Size) * rod.Basis;
        rod.Transform = new Transform3D(basis, center - basis * rod.GetAabb().GetCenter());
    }
}
