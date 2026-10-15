using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyCabinetStatusTowers(SceneDefinition definition, Node3D root,
        Node3D? support, SceneSimulationRuntime runtime)
    {
        if (support is null || !support.HasMeta("cabinetStatusTowerIds")) return;
        var ids = support.GetMeta("cabinetStatusTowerIds").AsGodotArray<string>();
        var body = support.GetNode<MeshInstance3D>("OperatorCabinet_Body");
        var bodyBounds = ControlInstallationWorldBounds(body);
        foreach (var id in ids)
        {
            var head = root.GetNode<Node3D>(id.Replace('-', '_'));
            var pose = head.Transform;
            var basis = head.Transform.Basis;
            var visible = head.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                .Where(m => m.Mesh is not null && m.IsVisibleInTree()).ToArray();
            var bounds = visible.Select(ControlInstallationWorldBounds).Aggregate((a, b) => a.Merge(b));
            var coupler = support.GetNode<MeshInstance3D>("StatusTowerCoupler_" + id);
            var couplerBounds = ControlInstallationWorldBounds(coupler);
            void Check(bool condition, string reason)
            {
                if (!condition) throw new InvalidOperationException($"Status tower {definition.Id}/{id}: {reason}");
            }
            Check(Math.Abs(bounds.Position.Y - couplerBounds.End.Y) < .003f, "head lacks bearing contact");
            Check(bounds.GetCenter().X >= bodyBounds.Position.X && bounds.GetCenter().X <= bodyBounds.End.X,
                "head outside enclosure width");
            Check(bounds.GetCenter().Z >= bodyBounds.Position.Z && bounds.GetCenter().Z <= bodyBounds.End.Z,
                "head outside enclosure depth");
            Check(head.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                .Where(m => m.Name == "POLE" || m.Name.ToString().StartsWith("BASE_", StringComparison.Ordinal))
                .All(m => !m.Visible), "original floor stand is still visible");
            var bindings = definition.Simulation.GetProperty("pointBindings").EnumerateArray()
                .Where(b => b.GetProperty("equipmentId").GetString() == id).ToArray();
            foreach (var binding in bindings)
            {
                if (!binding.TryGetProperty("point", out var pointElement)
                    || !binding.TryGetProperty("activeColor", out var colorElement)) continue;
                var point = pointElement.GetString()!;
                var declared = definition.Simulation.GetProperty("points").EnumerateArray()
                    .Single(p => p.GetProperty("name").GetString() == point);
                if (declared.GetProperty("type").GetString() != "BOOL"
                    || declared.GetProperty("owner").GetString() != "PLC") continue;
                var lens = head.FindChild("LENS_" + colorElement.GetString(), true, false) as MeshInstance3D;
                Check(lens is not null && lens.IsVisibleInTree(), "named output lens lost");
                bool Emitting() => lens!.MaterialOverride is StandardMaterial3D material
                    && material.EmissionEnabled && material.EmissionEnergyMultiplier > .1f;
                var initial = runtime.Points[point];
                foreach (var value in new[] { false, true, false })
                {
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = value });
                    Check(Equals(runtime.Points[point], value) && Emitting() == value,
                        "output/lens projection failed: " + point);
                }
                runtime.ResetSimulation();
                Check(Equals(runtime.Points[point], initial) && Emitting() == (initial is true),
                    "reset output projection failed: " + point);
                GD.Print($"STATUS_TOWER_OUTPUT_PROJECTION PASS scene={definition.Id} equipment={id} point={point} scope=explicit-output-fixture-not-program-scan");
            }
            runtime.ResetSimulation();
            Check(head.Transform.IsEqualApprox(pose) && head.Transform.Basis.IsEqualApprox(basis),
                "reset moved or rotated mounted head");
            GD.Print($"STATUS_TOWER_INSTALLATION PASS scene={definition.Id} equipment={id} bearing=contact basis=retained reset=retained authored-bindings={bindings.Length}");
        }
    }
}
