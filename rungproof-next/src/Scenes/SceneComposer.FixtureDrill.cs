using System;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureFixtureDrill(Node3D root)
    {
        var drill = root.GetNode<Node3D>("safe_drill");
        var stock = root.GetNode<Node3D>("drill_workpiece");
        var coupon = drill.FindChild("DRILL_workpiece", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("Fixture drill delivery has no clamped stock mesh.");
        // The stock root is placed at the measured delivered coupon center. Its
        // mesh keeps the original dimensions, material and vise engagement.
        coupon.Owner = null;
        coupon.GetParent().RemoveChild(coupon);
        stock.AddChild(coupon);
        coupon.Transform = Transform3D.Identity;
        var spindle = drill.FindChild("KIN_spindle", true, false) as Node3D
            ?? throw new InvalidOperationException("Fixture drill delivery has no spindle assembly.");
        // Authored bit tip is Y=1.610, stock top=1.695. Raise the home by
        // 235 mm for 150 mm clearance. A 245 mm stroke ends at Y=1.600,
        // inside the stock with 15 mm remaining above its bottom. Quill and
        // bearing retain overlap; the head, guard, vise and table stay fixed.
        spindle.Position += Vector3.Up * 0.235f;
    }
}
