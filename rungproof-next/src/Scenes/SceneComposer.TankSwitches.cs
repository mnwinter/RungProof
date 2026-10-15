using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureTankSwitchMounts(Node3D sceneRoot, SceneDefinition scene)
    {
        var tank = sceneRoot.GetNode<Node3D>(SafeNodeName(Text(scene.Simulation, "tankId", string.Empty)));
        // Composition precedes tree entry. Accumulate transforms to the scene
        // instead of querying GlobalTransform before the model is ready.
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != sceneRoot; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        Aabb Bounds(string name)
        {
            var mesh = tank.FindChild(name, true, false) as MeshInstance3D
                ?? throw new InvalidOperationException($"Tank switch installation requires '{name}'.");
            return InScene(mesh) * mesh.GetAabb();
        }
        var shell = Bounds("TANK_shell");
        var liquid = Bounds("KIN_liquid");
        var steel = Material(new Color("a5b3bb"), 0.7f, 0.23f);
        foreach (var (idField, thresholdField, defaultThreshold) in new[]
        {
            ("lowSensorId", "lowThreshold", 0.2), ("highSensorId", "highThreshold", 0.8),
        })
        {
            var id = SafeNodeName(Text(scene.Simulation, idField, string.Empty));
            var sensor = sceneRoot.GetNode<Node3D>(id);
            var threshold = Number(scene.Simulation, thresholdField, defaultThreshold);
            if (!double.IsFinite(threshold) || threshold <= 0 || threshold >= 1)
                throw new InvalidOperationException($"Tank switch '{id}' requires a threshold between zero and one.");
            // The tank's authored sight-glass liquid is the runtime's level
            // datum. Use its full range, including configured tank scaling.
            var elevation = liquid.Position.Y + liquid.Size.Y * (float)threshold;
            var mounting = scene.Equipment.Single(equipment => equipment.Id == id);
            var mountSide = Text(mounting.Config, "mountSide", "positiveZ");
            if (mountSide is not "positiveZ" and not "negativeZ" and not "positiveX")
                throw new InvalidOperationException($"Tank switch '{id}' requires a supported mount side.");
            var positiveSide = mountSide != "negativeZ";
            var alongX = mountSide == "positiveX";
            sensor.RotationDegrees = new Vector3(positiveSide ? 90 : -90, alongX ? 90 : 0, 0);
            sensor.Position = alongX
                ? new Vector3(shell.End.X + 0.10f, elevation, shell.GetCenter().Z)
                : new Vector3(shell.GetCenter().X, elevation,
                    positiveSide ? shell.End.Z + 0.10f : shell.Position.Z - 0.10f);
            var seal = sensor.FindChild("PROCESS_seal", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException($"Tank switch '{id}' lacks a process seal.");
            var sealBounds = InScene(seal) * seal.GetAabb();
            // Short hollow socket seats the seal and supports the fitting.
            // Vessel penetration is a visual installation, not a fabricated
            // pressure-vessel opening or rated nozzle design.
            var start = alongX
                ? new Vector3(shell.End.X - 0.02f, elevation, sensor.Position.Z)
                : new Vector3(sensor.Position.X, elevation,
                    positiveSide ? shell.End.Z - 0.02f : shell.Position.Z + 0.02f);
            var end = alongX ? start with { X = sealBounds.Position.X }
                : start with { Z = positiveSide ? sealBounds.Position.Z : sealBounds.End.Z };
            AddSumpRoute(tank, $"TANK_SWITCH_{id}",
                new[] { InScene(tank).AffineInverse() * start, InScene(tank).AffineInverse() * end }, 0.14f, steel);
        }
    }
}
