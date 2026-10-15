using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyMotorArrayControllerWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-06-ten-motor-array-startup", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        var document = new LadderEditorDocument();
        document.ResetProject("review-motor-array", "Motor_Array_QA", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-10-06-ten-motor-array-startup";
        foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" })
            document.AddTag(input, PlcVariableRole.Input, input);
        document.AddTag("motor_array_run", PlcVariableRole.Output, "motor_array_run");
        document.AddTag("startup_sequence_active", PlcVariableRole.Output, "startup_sequence_active");
        // Explicit QA choice: ten parallel nonretentive TONs, .5 seconds apart.
        // No automatic solution is installed in the exercise. Group command
        // has no rung and stays false, so only individual commands drive motors.
        for (var motor = 0; motor < 10; motor++)
        {
            var timer = $"qa_start_{motor}";
            var output = $"motor_{motor}_run";
            document.AddTag(timer, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            document.AddTag(output, PlcVariableRole.Output, output);
            var rung = document.Rungs.Count;
            document.AddTimerRung($"Motor {motor + 1} startup delay", timer, TimeSpan.FromSeconds((motor + 1) * .5), LadderTimerKind.OnDelay);
            foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" })
                document.AddContact(rung, 0, input, false);
            document.AddRung($"Motor {motor + 1} follows timer", output);
            document.AddContact(rung + 1, 0, timer + ".Q", false);
        }
        var activeRung = document.Rungs.Count;
        document.AddRung("Startup active until last timer completes", "startup_sequence_active");
        foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" })
            document.AddContact(activeRung, 0, input, false);
        document.AddContact(activeRung, 0, "qa_start_9.Q", true);
        var program = document.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        check(compiled.IsValid, "motor_array_qa_compiles");
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-motor-array-native-qa.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(program);
        try
        {
            void Tick(int count) { for (var scan = 0; scan < count; scan++) _PhysicsProcess(.02); }
            bool Motors(int count) => Enumerable.Range(0, 10).All(i => Equals(runtime.Points[$"motor_{i}_run"], i < count));
            void Toggle(string point)
            {
                if (!ExecuteSelectedControllerAction("toggle-" + point)) throw new InvalidOperationException("Motor array input rejected: " + point);
            }
            RunActiveController(); Tick(30);
            check(Motors(0), "motor_array_idle_run_stays_off");
            Toggle("group_start_request"); Toggle("all_motors_ready"); Tick(30);
            check(Motors(0), "motor_array_missing_alarm_permissive_stays_off");
            Toggle("group_alarm_clear"); Tick(24);
            check(Motors(0) && runtime.Points["startup_sequence_active"] is true, "motor_array_before_first_delay_off");
            var reviewWasEnabled = _visualSceneReview;
            _visualSceneReview = true;
            try
            {
                SetGantryReviewClockHeld(true);
                var shaftNodes = _sceneCompositionRoot!.GetNode<Node3D>("motor_0")
                    .FindChildren("KIN_motor_*", "", true, false).OfType<Node3D>().ToArray();
                var before = shaftNodes.Select(node => node.Transform).ToArray();
                var scans = _virtualController!.Snapshot.ScanNumber;
                AdvanceGantryReviewClock(1);
                check(_virtualController.Snapshot.ScanNumber == scans + 1
                    && shaftNodes.Where((node, i) => node.Transform != before[i]).Any(),
                    "motor_array_held_step_accepts_one_scan_and_advances_motor_visual");
            }
            finally { ReleaseGantryReviewClock(); _visualSceneReview = reviewWasEnabled; }
            check(Motors(1), "motor_array_first_motor_at_500ms");
            for (var count = 2; count <= 10; count++)
            {
                Tick(25);
                check(Motors(count), $"motor_array_timed_prefix_{count}");
            }
            check(runtime.Points["startup_sequence_active"] is false && runtime.Points["motor_array_run"] is false,
                "motor_array_completed_individual_sequence_group_off");
            Toggle("group_alarm_clear"); Tick(1);
            check(Motors(0), "motor_array_alarm_loss_clears_all_outputs_next_scan");
            Toggle("group_alarm_clear"); Tick(25);
            check(Motors(1), "motor_array_alarm_restore_restarts_nonretentive_delays");
            StopActiveController();
            check(Motors(0) && runtime.Points["startup_sequence_active"] is false, "motor_array_stop_clears_commands");
            ResetActiveController();
            check(Motors(0) && runtime.Points["group_start_request"] is false && _virtualController!.Snapshot.ScanNumber == 0,
                "motor_array_reset_clears_requests_and_scan");
        }
        finally { DisableVirtualController(); }
    }
}
