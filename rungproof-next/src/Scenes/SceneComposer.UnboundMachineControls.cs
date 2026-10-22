using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    /// <summary>
    /// The shared machining-center model contains a presentation-only START /
    /// E-STOP cluster. It has no scene action binding and must not compete with
    /// the actual functional operator station. Scenes may explicitly retain it
    /// only after supplying their own interaction contract.
    /// </summary>
    private static void HideUnboundDecorativeMachineControls(Node3D root, SceneDefinition scene)
    {
        foreach (var equipment in scene.Equipment.Where(item => item.Type == "machine"))
        {
            if (equipment.Config.TryGetProperty("retainDecorativeControls", out var retain)
                && retain.ValueKind == System.Text.Json.JsonValueKind.True)
                continue;
            var machine = root.GetNodeOrNull<Node3D>(SafeNodeName(equipment.Id));
            if (machine is null) continue;
            var hidden = 0;
            foreach (var control in machine.FindChildren("CTRL_*", "", true, false).OfType<Node3D>())
            {
                control.Visible = false;
                hidden++;
            }
            if (hidden > 0)
            {
                machine.SetMeta("unboundDecorativeControlsHidden", hidden);
                GD.Print($"UNBOUND_MACHINE_CONTROLS_HIDDEN scene={scene.Id} equipment={equipment.Id} count={hidden}");
            }
        }
    }
}
