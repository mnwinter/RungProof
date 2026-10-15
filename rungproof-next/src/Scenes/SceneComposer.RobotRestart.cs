using System;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local installation geometry. The catalog observation-window wall
    // and pallet-fork asset remain available under their existing identities;
    // neither can represent this lesson's fence or controller cabinet.
    private static Node3D CreateRobotRestartGuard()
    {
        var root = new Node3D();
        var frame = Material(new Color("e6b92f"), 0.45f, 0.35f);
        var mesh = Material(new Color("28353b"), 0.55f, 0.38f);
        var steel = Material(new Color("70818a"), 0.7f, 0.3f);
        void Post(float x, float z)
        {
            AddBox(root, new Vector3(0.24f, 0.025f, 0.24f), new Vector3(x, 0.0125f, z), steel).Name = $"FENCE_foot_{x}_{z}";
            AddBox(root, new Vector3(0.1f, 2.05f, 0.1f), new Vector3(x, 1.05f, z), frame).Name = $"FENCE_post_{x}_{z}";
            foreach (var dx in new[] { -0.08f, 0.08f })
            foreach (var dz in new[] { -0.08f, 0.08f })
                AddBox(root, new Vector3(0.024f, 0.02f, 0.024f), new Vector3(x + dx, 0.035f, z + dz), steel);
        }
        void Panel(Node3D parent, float length, Vector3 center, float yaw, string name)
        {
            var panel = new Node3D { Name = name, Position = center, RotationDegrees = new Vector3(0, yaw, 0) };
            parent.AddChild(panel);
            foreach (var y in new[] { 0.18f, 1.95f }) AddBox(panel, new Vector3(length, 0.06f, 0.06f), new Vector3(0, y, 0), frame);
            foreach (var x in new[] { -length / 2 + 0.03f, length / 2 - 0.03f })
                AddBox(panel, new Vector3(0.06f, 1.77f, 0.06f), new Vector3(x, 1.065f, 0), frame);
            // Actual welded-wire infill, with openings through which the
            // robot remains visible. These bars are physical mesh geometry.
            var count = (int)MathF.Ceiling((length - 0.12f) / 0.12f);
            for (var i = 0; i <= count; i++)
                AddBox(panel, new Vector3(0.006f, 1.71f, 0.006f),
                    new Vector3(-length / 2 + 0.06f + (length - 0.12f) * i / count, 1.065f, 0), mesh);
            for (var y = 0.27f; y < 1.94f; y += 0.12f)
                AddBox(panel, new Vector3(length - 0.12f, 0.006f, 0.006f), new Vector3(0, y, 0), mesh);
        }
        foreach (var x in new[] { -4.5f, -2.125f, 0.25f, 2.625f, 5f }) Post(x, -3.3f);
        foreach (var x in new[] { -4.5f, -3.12f, -1.5f, 0.667f, 2.833f, 5f }) Post(x, 3.3f);
        foreach (var z in new[] { -1.1f, 1.1f }) { Post(-4.5f, z); Post(5f, z); }
        for (var i = 0; i < 4; i++) Panel(root, 2.275f, new Vector3(-3.3125f + i * 2.375f, 0, -3.3f), 0, $"FENCE_rear_{i}");
        foreach (var x in new[] { -4.5f, 5f })
        for (var i = 0; i < 3; i++) Panel(root, 2.1f, new Vector3(x, 0, -2.2f + i * 2.2f), 90, $"FENCE_side_{x}_{i}");
        Panel(root, 1.28f, new Vector3(-3.81f, 0, 3.3f), 0, "FENCE_front_left");
        for (var i = 0; i < 3; i++) Panel(root, 2.0667f, new Vector3(-0.4167f + i * 2.1667f, 0, 3.3f), 0, $"FENCE_front_right_{i}");
        var gate = new Node3D { Name = "KIN_restart_gate", Position = new Vector3(-1.5f, 0, 3.3f) };
        root.AddChild(gate);
        Panel(gate, 1.5f, new Vector3(-0.75f, 0, 0), 0, "GATE_leaf");
        foreach (var y in new[] { 0.45f, 1.65f })
            AddBox(root, new Vector3(0.13f, 0.16f, 0.13f), new Vector3(-1.5f, y, 3.3f), steel).Name = $"GATE_hinge_{y}";
        AddBox(gate, new Vector3(0.025f, 0.24f, 0.08f), new Vector3(-1.35f, 1.08f, 0.095f), steel).Name = "GATE_handle";
        root.AddChild(new EquipmentMotionController
        {
            Name = "RestartGateMotion", Kind = EquipmentMotionController.MotionKind.PositionRotation,
            TargetPrefix = "KIN_restart_gate", TravelDegrees = 90, PositionInputInverted = true,
            AutonomousPositionTravel = false,
        });
        return root;
    }

    private static void ConfigureRobotRestartInterlock(Node3D root)
    {
        var interlock = root.GetNode<Node3D>("training_accessory_5");
        var guard = root.GetNode<Node3D>("training_accessory_4");
        var gate = guard.GetNode<Node3D>("KIN_restart_gate");
        foreach (var name in new[] { "FIXED_GUARD_FRAME", "MOVING_GUARD_EDGE", "CLOSED_POSITION_GAP", "SENSOR_cable" })
        {
            var node = interlock.FindChild(name, true, false);
            if (node is null) throw new InvalidOperationException($"Restart interlock is missing {name}.");
            node.Free();
        }
        var steel = Material(new Color("70818a"), 0.7f, 0.3f);
        // Offset brackets connect the sensor/actuator mounting plates to their
        // respective frame members. The actuator is a real gate child and
        // leaves the fixed sensor when gate_closed goes false.
        AddBox(interlock, new Vector3(0.10f, 0.3f, 0.065f), new Vector3(-0.115f, 0.5f, 0.01f), steel).Name = "SENSOR_installation_bracket";
        AddBox(interlock, new Vector3(0.18f, 0.3f, 0.065f), new Vector3(0.10f, 0.5f, 0.01f), steel).Name = "ACTUATOR_installation_bracket";
        foreach (var part in interlock.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(part => part.Name.ToString().Contains("ACTUATOR", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            var relative = part.Transform;
            for (var ancestor = part.GetParent(); ancestor != interlock; ancestor = ancestor.GetParent())
                if (ancestor is Node3D spatial) relative = spatial.Transform * relative;
            var installed = interlock.Transform * relative;
            part.Owner = null;
            part.Reparent(gate, false);
            part.Transform = (guard.Transform * gate.Transform).AffineInverse() * installed;
        }
        // The delivered looping demonstration cable crossed into the gate.
        // Route a fixed lead down the outside of the latch post instead.
        AddBox(interlock, new Vector3(0.012f, 1.24f, 0.012f), new Vector3(-0.15f, -0.14f, 0.12f), steel).Name = "SENSOR_fixed_lead";
        AddBox(interlock, new Vector3(0.075f, 0.012f, 0.012f), new Vector3(-0.1125f, 0.475f, 0.12f), steel).Name = "SENSOR_lead_connector";
    }

    private static Node3D CreateRobotRestartPanel()
    {
        var root = new Node3D();
        var shell = Material(new Color("b4bec4"), 0.6f, 0.35f);
        var dark = Material(new Color("17242b"), 0.15f, 0.35f);
        AddBox(root, new Vector3(0.8f, 0.06f, 0.6f), new Vector3(0, 0.03f, 0), dark).Name = "PANEL_plinth";
        AddBox(root, new Vector3(0.7f, 1.45f, 0.5f), new Vector3(0, 0.785f, 0), shell).Name = "PANEL_cabinet";
        AddBox(root, new Vector3(0.62f, 1.33f, 0.025f), new Vector3(0, 0.785f, 0.263f), dark).Name = "PANEL_door";
        AddBox(root, new Vector3(0.43f, 0.28f, 0.015f), new Vector3(0, 1.18f, 0.283f), shell).Name = "PANEL_screen";
        root.AddChild(new Label3D { Text = "OFFLINE\nCONTROLLER", Position = new Vector3(0, 1.18f, 0.294f),
            FontSize = 32, PixelSize = 0.0007f, OutlineSize = 0, Modulate = Colors.Black });
        root.AddChild(new Label3D { Text = "ROBOT CONTROLLER", Position = new Vector3(0, 1.43f, 0.283f),
            FontSize = 32, PixelSize = 0.00065f, OutlineSize = 0, Modulate = Colors.White });
        AddBox(root, new Vector3(0.018f, 0.2f, 0.025f), new Vector3(0.245f, 0.83f, 0.29f), shell).Name = "PANEL_handle";
        for (var i = 0; i < 5; i++) AddBox(root, new Vector3(0.38f, 0.014f, 0.016f), new Vector3(0, 0.45f + i * 0.055f, 0.283f), shell);
        return root;
    }
}
