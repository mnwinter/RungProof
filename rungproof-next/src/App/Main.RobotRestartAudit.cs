using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditRobotRestart;

    // Offline projection and installation checks. These supplied output
    // images exercise the renderer; they do not validate ladder safety logic.
    private void AuditRobotRestart()
    {
        var failures = 0;
        void Check(bool condition, string name)
        {
            if (!condition) failures++;
            GD.Print($"ROBOT_RESTART_AUDIT {name}={condition}");
        }
        try
        {
            AddMigratedScene("lab-3-02-robot-cell-safe-restart", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var runtime = _sceneRuntime!;
            runtime.UsesExternalClock = false;
            var robot = root.GetNode<Node3D>("robotArm_0");
            var guard = root.GetNode<Node3D>("training_accessory_4");
            var interlock = root.GetNode<Node3D>("training_accessory_5");
            var panel = root.GetNode<Node3D>("training_accessory_7");
            var gate = guard.GetNode<Node3D>("KIN_restart_gate");
            var motion = robot.FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
            var gateMotion = guard.FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
            MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
            var wrist = Part(robot, "ROBOT_axis6_bolt_0_155_2_895");
            var home = wrist.GlobalTransform;
            Check(ReviewMeshes(guard).Any(mesh => mesh.Name.ToString().StartsWith("FENCE_post", StringComparison.Ordinal))
                && guard.GetNodeOrNull<Node3D>("FENCE_rear_0") is not null
                && guard.GetNodeOrNull<Node3D>("FENCE_side_5_0") is not null,
                "fence_has_front_rear_and_both_side_runs");
            Check(ReviewMeshes(panel).All(mesh => !mesh.Name.ToString().Contains("FORK", StringComparison.OrdinalIgnoreCase))
                && panel.FindChild("PANEL_cabinet", true, false) is not null, "controller_panel_is_a_cabinet");
            Check(ReviewBounds(robot).Position.Y >= -0.001f && ReviewBounds(root.GetNode<Node3D>("machine_1")).Position.Y >= -0.001f
                && ReviewBounds(guard).Position.Y >= -0.001f && ReviewBounds(interlock).Position.Y >= -0.001f,
                "robot_machine_guard_and_interlock_clear_floor");
            var sensor = Part(interlock, "CODED_SENSOR_body");
            var actuator = Part(gate, "CODED_ACTUATOR_body");
            var fixedSensor = sensor.GlobalTransform;
            var openGate = gate.Transform;
            Check(runtime.Points["gate_closed"] is false && gateMotion.PositionPercent > 99,
                "initial_false_gate_feedback_renders_open_gate");
            Check(sensor.GlobalPosition.DistanceTo(actuator.GlobalPosition) > 1, "open_gate_separates_actuator_from_sensor");
            runtime.ExecuteAction("toggle-gate_closed");
            Check(gateMotion.PositionPercent < 0.001f && sensor.GlobalTransform.IsEqualApprox(fixedSensor)
                && MathF.Abs(sensor.GlobalPosition.Y - actuator.GlobalPosition.Y) < 0.001f
                && MathF.Abs(sensor.GlobalPosition.Z - actuator.GlobalPosition.Z) < 0.001f
                && sensor.GlobalPosition.DistanceTo(actuator.GlobalPosition) < 0.14f,
                "closed_gate_aligns_moving_actuator_with_fixed_sensor");
            Check(!motion.RunCommand && runtime.Points["robot_enable"] is false,
                "closing_gate_does_not_invent_a_plc_enable");
            var actuatorBracket = Part(gate, "ACTUATOR_installation_bracket");
            Check(ReviewMeshes(gate.GetNode<Node3D>("GATE_leaf")).Any(part => ReviewBounds(part).Intersects(ReviewBounds(actuatorBracket))),
                "actuator_mounting_bracket_bears_on_gate_frame");
            var fixedGuardParts = ReviewMeshes(guard).Where(part => !gate.IsAncestorOf(part)
                && !part.Name.ToString().StartsWith("GATE_hinge", StringComparison.Ordinal)
                && part.Name != "FENCE_post_-1_5_3_3" && part.Name != "FENCE_foot_-1_5_3_3").ToArray();
            var gateObstacles = root.GetChildren().OfType<Node3D>().Where(node => node != guard && node != interlock)
                .SelectMany(node => ReviewMeshes(node)).Concat(fixedGuardParts).Select(part => (Mesh: part, Bounds: ReviewBounds(part))).ToArray();
            var gateParts = ReviewMeshes(gate);
            var swingClear = true;
            var gateContacts = new HashSet<string>();
            for (var sample = 0; sample <= 90; sample++)
            {
                gateMotion.SetPositionNormalized(sample / 90f);
                foreach (var part in gateParts)
                foreach (var other in gateObstacles)
                {
                    var overlap = ReviewBounds(part).Intersection(other.Bounds).Size;
                    if (overlap.X <= 0.002f || overlap.Y <= 0.002f || overlap.Z <= 0.002f
                        || !OrientedBoxesPenetrate(part, other.Mesh)) continue;
                    swingClear = false;
                    if (gateContacts.Count < 12 && gateContacts.Add($"{part.Name}/{other.Mesh.Name}"))
                        GD.Print($"ROBOT_RESTART_GATE_CONTACT {part.Name}/{other.Mesh.Name} sample={sample}");
                }
            }
            gateMotion.SetPositionNormalized(1);
            Check(swingClear, "full_gate_swing_clears_other_equipment_and_non_hinge_fence_members_at_91_samples");
            runtime.ExecuteAction("toggle-reset_complete");
            Check(runtime.Points["reset_complete"] is true && runtime.SampleVirtualControllerInputs()["reset_complete"],
                "reset_action_presents_a_momentary_input_pulse");
            runtime.AdvanceSimulation(0.002);
            Check(runtime.Points["reset_complete"] is false, "reset_pulse_clears_on_next_simulator_tick");
            runtime.ExecuteAction("toggle-robot_ready");
            Check(runtime.Points["robot_enable"] is false && !motion.RunCommand,
                "pc_ready_feedback_does_not_write_plc_commands");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["robot_enable"] = true, ["cell_ready"] = true });
            Check(motion.RunCommand, "plc_robot_enable_commands_actual_robot_adapter");
            var obstacles = root.GetChildren().OfType<Node3D>().Where(node => node != robot)
                .SelectMany(node => ReviewMeshes(node)).Select(mesh => (Mesh: mesh, Bounds: ReviewBounds(mesh))).ToArray();
            var robotParts = ReviewMeshes(robot);
            var clear = true;
            var contained = true;
            var moved = false;
            var contacts = new HashSet<string>();
            for (var tick = 0; tick < 800; tick++)
            {
                motion._PhysicsProcess(0.01); // 8 s covers one complete 7.854 s sine cycle.
                moved |= !wrist.GlobalTransform.IsEqualApprox(home);
                foreach (var part in robotParts)
                {
                    var bounds = ReviewBounds(part);
                    contained &= bounds.Position.X > -4.4f && bounds.End.X < 4.9f
                        && bounds.Position.Z > -3.2f && bounds.End.Z < 3.2f;
                    foreach (var other in obstacles)
                    {
                        var overlap = bounds.Intersection(other.Bounds).Size;
                        if (overlap.X <= 0.002f || overlap.Y <= 0.002f || overlap.Z <= 0.002f) continue;
                        clear = false;
                        if (contacts.Count < 12 && contacts.Add($"{part.Name}/{other.Mesh.Name}"))
                            GD.Print($"ROBOT_RESTART_CONTACT {part.Name}/{other.Mesh.Name} tick={tick}");
                    }
                }
            }
            Check(moved, "enabled_robot_changes_its_actual_wrist_pose");
            Check(clear, "full_commanded_sweep_clears_other_equipment_at_800_samples");
            Check(contained, "full_commanded_sweep_remains_inside_fence_footprint");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["robot_enable"] = false });
            var stopped = wrist.GlobalTransform;
            motion._PhysicsProcess(1);
            Check(!motion.RunCommand && wrist.GlobalTransform.IsEqualApprox(stopped), "removed_enable_freezes_robot_pose");
            runtime.ExecuteAction("toggle-gate_closed");
            Check(gate.Transform.IsEqualApprox(openGate) && sensor.GlobalTransform.IsEqualApprox(fixedSensor),
                "gate_feedback_reopens_leaf_and_preserves_fixed_sensor");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["robot_enable"] = true });
            runtime.StopSimulation();
            motion._PhysicsProcess(1);
            GD.Print($"ROBOT_RESTART_STOP running={motion.RunCommand} command={runtime.Points["robot_enable"]} held={wrist.GlobalTransform.IsEqualApprox(stopped)} pose={wrist.GlobalTransform} expected={stopped}");
            Check(!motion.RunCommand && runtime.Points["robot_enable"] is false
                && wrist.GlobalTransform.IsEqualApprox(stopped), "normal_stop_removes_command_and_holds_pose");
            runtime.ResetSimulation();
            Check(wrist.GlobalTransform.IsEqualApprox(home) && gate.Transform.IsEqualApprox(openGate)
                && runtime.Points["reset_complete"] is false && runtime.Points["cell_ready"] is false,
                "reset_restores_robot_open_gate_and_initial_points");
            VerifyRobotRestartController(Check);
        }
        catch (Exception error)
        {
            failures++;
            GD.PushError($"ROBOT_RESTART_AUDIT_EXCEPTION {error}");
        }
        GD.Print($"ROBOT_RESTART_AUDIT_RESULT failures={failures}; offline geometry/output projection only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyRobotRestartController(Action<bool, string> check)
    {
        var document = new LadderEditorDocument();
        document.ResetProject("qa-robot-restart", "Robot_Restart_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-3-02-robot-cell-safe-restart";
        foreach (var point in new[] { "gate_closed", "reset_complete", "robot_ready", "motion_request" }) document.AddTag(point, PlcVariableRole.Input, point);
        document.AddTag("stop_command", PlcVariableRole.Input, "operator.stop");
        foreach (var point in new[] { "robot_enable", "cell_ready" }) document.AddTag(point, PlcVariableRole.Output, point);
        document.AddRung("Fresh reset authorizes the cell without starting motion", "cell_ready");
        // Edge detection occurs before the permissives, so pressing reset
        // while open cannot become a delayed restart when the gate closes.
        document.InsertEdgeContact(0, 0, 0, "reset_complete", LadderEdgeMode.Rising);
        document.AddParallelBranch(0);
        document.AddContact(0, 1, "cell_ready", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddContact(0, branch, "gate_closed", false);
            document.AddContact(0, branch, "robot_ready", false);
            document.AddContact(0, branch, "stop_command", true);
        }
        document.AddRung("Separate Start edge enables authorized robot motion", "robot_enable");
        document.InsertEdgeContact(1, 0, 0, "motion_request", LadderEdgeMode.Rising);
        document.AddParallelBranch(1);
        document.AddContact(1, 1, "robot_enable", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddContact(1, branch, "cell_ready", false);
            document.AddContact(1, branch, "gate_closed", false);
            document.AddContact(1, branch, "robot_ready", false);
            document.AddContact(1, branch, "stop_command", true);
        }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/robot-restart-reference.rpproj.json"), LadderEditorProjectJson.Save(document));
        var reference = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/robot-cell-restart-reference.rpproj.json"));
        check(reference.Document is not null && reference.IsReadable
            && LadderEditorProjectJson.Save(reference.Document) == LadderEditorProjectJson.Save(document),
            "saved_reference_matches_the_reviewed_reset_edge_and_seal_in_program");
        if (reference.Document is null || !reference.IsReadable) throw new InvalidOperationException("Unreadable robot restart reference.");
        EnableVirtualControllerProgram(reference.Document.BuildProgram());
        if (_virtualController is null) throw new InvalidOperationException("Robot restart reference did not load.");
        var runtime = _sceneRuntime!;
        var motion = _sceneCompositionRoot!.GetNode<Node3D>("robotArm_0").FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single();
        void Advance() => _PhysicsProcess(0.04); // high scan followed by cleared pulse scan
        bool Enabled() => runtime.Points["robot_enable"] is true && motion.RunCommand;
        try
        {
            RunActiveController(); Advance();
            runtime.ExecuteAction("toggle-robot_ready");
            runtime.ExecuteAction("toggle-reset_complete"); Advance();
            runtime.ExecuteAction("toggle-gate_closed"); Advance();
            check(!Enabled() && runtime.Points["cell_ready"] is false,
                "actual_scans_discard_reset_while_open_and_do_not_enable_when_gate_later_closes");
            runtime.ExecuteAction("start-robot-motion"); Advance();
            check(!Enabled(), "actual_scans_reject_start_without_reset_authorization");
            runtime.ExecuteAction("toggle-reset_complete"); Advance();
            check(!Enabled() && runtime.Points["cell_ready"] is true && runtime.Points["reset_complete"] is false,
                "actual_scans_accept_fresh_reset_without_starting_robot_motion");
            runtime.ExecuteAction("start-robot-motion"); Advance();
            check(Enabled() && runtime.Points["motion_request"] is false, "actual_scans_accept_separate_start_and_seal_enable_after_pulse_clears");
            runtime.ExecuteAction("toggle-gate_closed"); Advance();
            check(!Enabled() && runtime.Points["cell_ready"] is false, "actual_scans_remove_enable_when_gate_opens");
            runtime.ExecuteAction("toggle-gate_closed"); Advance();
            check(!Enabled(), "actual_scans_require_new_reset_after_gate_reclosure");
            runtime.ExecuteAction("toggle-reset_complete"); Advance();
            runtime.ExecuteAction("start-robot-motion"); Advance();
            runtime.ExecuteAction("toggle-robot_ready"); Advance();
            check(!Enabled(), "actual_scans_remove_enable_when_robot_ready_is_lost");
            runtime.ExecuteAction("toggle-robot_ready"); Advance();
            check(!Enabled(), "actual_scans_require_new_reset_after_ready_returns");
            runtime.ExecuteAction("toggle-reset_complete"); Advance();
            runtime.ExecuteAction("start-robot-motion"); Advance();
            StopActiveController();
            check(!Enabled(), "actual_controller_stop_removes_robot_command");
            RunActiveController(); Advance();
            check(!Enabled(), "actual_controller_run_after_stop_requires_fresh_reset");
            runtime.ExecuteAction("start-robot-motion"); Advance();
            check(!Enabled(), "actual_controller_start_after_stop_cannot_reuse_old_reset_authorization");
            runtime.ExecuteAction("toggle-reset_complete"); Advance();
            check(!Enabled() && runtime.Points["cell_ready"] is true, "actual_controller_restart_reset_authorizes_without_motion");
            runtime.ExecuteAction("start-robot-motion"); Advance();
            check(Enabled(), "actual_controller_restart_requires_both_new_reset_and_separate_start");
            ResetActiveController();
            check(!Enabled() && runtime.Points["gate_closed"] is false && runtime.Points["robot_ready"] is false
                && runtime.Points["reset_complete"] is false && runtime.Points["motion_request"] is false,
                "actual_controller_reset_restores_initial_inputs_outputs_and_robot");
        }
        finally { DisableVirtualController(); }
    }
}
