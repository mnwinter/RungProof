using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyDrillFeedGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-16-safe-drill", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = false;
        var drill = root.GetNode<Node3D>("safe_drill");
        var stock = root.GetNode<Node3D>("drill_workpiece");
        var motion = drill.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var spindle = (Node3D)drill.FindChild("KIN_spindle", true, false);
        var head = (Node3D)drill.FindChild("DRILL_head", true, false);
        var bit = (MeshInstance3D)drill.FindChild("DRILL_bit", true, false);
        var chuck = (MeshInstance3D)drill.FindChild("DRILL_chuck", true, false);
        var quill = (MeshInstance3D)drill.FindChild("DRILL_quill", true, false);
        var bearing = (MeshInstance3D)drill.FindChild("DRILL_spindle_bearing_housing", true, false);
        var table = (MeshInstance3D)drill.FindChild("DRILL_table", true, false);
        var vise = (MeshInstance3D)drill.FindChild("DRILL_vise_base", true, false);
        var column = (MeshInstance3D)drill.FindChild("DRILL_column", true, false);
        var jaws = ReviewMeshes(drill).Where(mesh => mesh.Name.ToString().StartsWith("DRILL_vise_jaw_face", StringComparison.Ordinal)).ToArray();
        var stockBounds = ReviewBounds(stock);
        var home = spindle.Transform;
        var headHome = head.Transform;
        var bitHome = ReviewBounds(bit);
        var chuckHome = ReviewBounds(chuck);
        check(ReviewMeshes(stock).Length == 1 && ReviewMeshes(drill).All(mesh => mesh.Name != "DRILL_workpiece"),
            "drill_has_one_stock_mesh_in_its_equipment_root");
        check(MathF.Abs(stockBounds.Position.Y - 1.585f) < 0.001f && MathF.Abs(stockBounds.End.Y - 1.695f) < 0.001f
            && !stockBounds.Intersects(ReviewBounds(table)) && !stockBounds.Intersects(ReviewBounds(column)),
            "drill_stock_occupies_delivered_vise_instead_of_under_table");
        check(MathF.Abs(bitHome.Position.Y - stockBounds.End.Y - 0.15f) < 0.001f,
            "drill_home_tip_has_150mm_stock_clearance");
        motion.Run();
        motion._PhysicsProcess(0.013);
        check(spindle.Transform.Origin == home.Origin && spindle.Transform.Basis != home.Basis,
            "drill_run_rotates_without_automatic_axial_feed");
        runtime.ResetSimulation();
        var sweepClear = true;
        for (var sample = 0; sample <= 100; sample++)
        {
            motion.SetPositionNormalized(sample / 100.0f);
            motion.Run();
            motion._PhysicsProcess(0.007);
            var tip = ReviewBounds(bit);
            var body = ReviewBounds(chuck);
            sweepClear &= head.Transform == headHome && !tip.Intersects(ReviewBounds(column))
                && !tip.Intersects(ReviewBounds(table)) && !tip.Intersects(ReviewBounds(vise))
                && jaws.All(jaw => !tip.Intersects(ReviewBounds(jaw)))
                && body.Position.Y > stockBounds.End.Y && ReviewBounds(quill).Intersects(ReviewBounds(bearing));
        }
        var bottom = ReviewBounds(bit);
        check(sweepClear, "drill_101_rotating_feed_poses_clear_fixture_and_keep_quill_in_bearing");
        check(MathF.Abs(bitHome.Position.Y - bottom.Position.Y - 0.245f) < 0.001f
            && MathF.Abs(chuckHome.Position.Y - ReviewBounds(chuck).Position.Y - 0.245f) < 0.001f
            && bottom.Position.Y > stockBounds.Position.Y && bottom.Position.Y < stockBounds.End.Y,
            "drill_bit_and_chuck_feed_together_245mm_into_stock");
        var rotated = spindle.Transform.Basis;
        motion.SetPositionNormalized(0.4f);
        check(spindle.Transform.Basis == rotated, "drill_feed_setpoint_preserves_spindle_rotation");
        runtime.ResetSimulation();
        check(spindle.Transform == home && head.Transform == headHome && ReviewBounds(stock) == stockBounds,
            "drill_reset_restores_home_without_moving_head_or_stock");
        runtime.ExecuteAction("toggle-left-hand");
        runtime.ExecuteAction("toggle-right-hand");
        runtime.RunDefault();
        for (var tick = 0; tick < 228; tick++) runtime.AdvanceSimulation(1.0 / 120.0);
        check(runtime.Points["drill_at_bottom"] is true && runtime.Points["drill_at_top"] is false
            && MathF.Abs(ReviewBounds(bit).Position.Y - 1.600f) < 0.001f,
            "drill_reference_bottom_feedback_matches_visible_tip");
        runtime.ExecuteAction("stop-drill");
        var stopped = spindle.Transform;
        for (var tick = 0; tick < 240; tick++)
        {
            runtime.AdvanceSimulation(1.0 / 120.0);
            motion._PhysicsProcess(1.0 / 120.0);
        }
        check(spindle.Transform == stopped && !motion.Running && runtime.Points["drill_run"] is false
            && runtime.Points["drill_at_bottom"] is true && runtime.Points["drill_at_top"] is false,
            "drill_stop_removes_rotation_and_holds_current_feed");
        check(!runtime.RunDefault(), "drill_restart_at_bottom_requires_reset_to_home");
        runtime.ResetSimulation();
        check(spindle.Transform == home && runtime.Points["drill_at_top"] is true && runtime.Points["drill_at_bottom"] is false,
            "drill_reset_restores_visible_home_and_reference_feedback");
    }
}
