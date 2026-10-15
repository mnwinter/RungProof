using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyServiceDoorPlacement(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-19-service-door", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var door = root.GetNode<Node3D>("service_shutter");
        var displays = new[] { "door_open_button", "door_stop_button", "door_close_button", "door_open_lamp", "door_closed_lamp" }
            .Select(id => root.GetNode<Node3D>(id)).ToArray();
        check(LogBoundsCandidates(root) == 0, "service_door_controls_clear_closed_shutter_and_each_other");
        check(displays.All(node => MathF.Abs(ReviewBounds(node).Position.Y) < 0.001f),
            "service_door_operator_and_signal_stands_rest_on_floor");
        check(new[] { "OPEN", "STOP", "CLOSE" }.Select((text, index) =>
            displays[index].GetNode<Label3D>("OperatorFaceLabel").Text == text).All(value => value),
            "service_door_three_operator_plates_match_actions");
        var motion = door.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var clear = true;
        for (var pose = 0; pose <= 100; pose++)
        {
            motion.SetPositionNormalized(pose / 100.0f);
            clear &= ReviewMeshes(door).All(part => displays.All(display =>
                !ReviewBounds(part).Intersects(ReviewBounds(display))));
        }
        check(clear, "service_door_101_curtain_poses_clear_operator_and_signal_stands");
        _sceneRuntime!.ResetSimulation();
        // These beacons intentionally show the PC-owned raw NC inputs. This
        // layout check does not certify physical limits or cable monitoring.
        check(_sceneRuntime.Points["open_limit_nc"] is true && _sceneRuntime.Points["closed_limit_nc"] is false,
            "service_door_reset_preserves_raw_nc_input_polarity");
        var runtime = _sceneRuntime;
        runtime.UsesExternalClock = false;
        motion.Run();
        motion._PhysicsProcess(1.0);
        check(MathF.Abs(motion.InputPositionNormalized - 1) < 0.001f,
            "service_door_sequence_adapter_does_not_travel_without_position_setpoint");
        runtime.ResetSimulation();
        runtime.ExecuteAction("open-door");
        runtime.AdvanceSimulation(1);
        check(Math.Abs(Convert.ToDouble(runtime.Points["door_position"]) - 60) < 0.001
            && MathF.Abs(motion.InputPositionNormalized - 0.6f) < 0.001f
            && runtime.Points["open_limit_nc"] is true && runtime.Points["closed_limit_nc"] is true,
            "service_door_intermediate_position_and_nc_feedback_match_motion");
        runtime.ExecuteAction("stop-door");
        runtime.AdvanceSimulation(1);
        check(MathF.Abs(motion.InputPositionNormalized - 0.6f) < 0.001f
            && runtime.Points["motor_open"] is false && runtime.Points["motor_close"] is false,
            "service_door_stop_holds_intermediate_position_and_removes_both_commands");
        runtime.ExecuteAction("close-door");
        runtime.AdvanceSimulation(0.5);
        check(MathF.Abs(motion.InputPositionNormalized - 0.68f) < 0.001f
            && Math.Abs(Convert.ToDouble(runtime.Points["door_position"]) - 68) < 0.001,
            "service_door_reverse_closes_from_current_pose_without_opening_jump");
        runtime.ExecuteAction("open-door");
        runtime.AdvanceSimulation(0.5);
        check(MathF.Abs(motion.InputPositionNormalized - 0.544f) < 0.001f,
            "service_door_running_reverse_opens_from_current_pose");
        for (var tick = 0; tick < 360; tick++) runtime.AdvanceSimulation(1.0 / 120.0);
        check(motion.InputPositionNormalized < 0.001f && runtime.Points["open_limit_nc"] is false
            && runtime.Points["closed_limit_nc"] is true && runtime.Points["motor_open"] is false,
            "service_door_open_endpoint_feedback_matches_reference_pose");
        runtime.ResetSimulation();
    }
}
