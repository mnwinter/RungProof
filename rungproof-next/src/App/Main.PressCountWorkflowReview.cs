using System;
using System.Collections.Generic;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyPressCountWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-4-01-press-count-lamp", _candidateCatalog!, _mainCamera!, false, false);
        check(_sceneCompositionRoot!.GetNode<Node3D>("switch_0")
            .GetNodeOrNull<Label3D>("OperatorFaceLabel")?.Text == "PULSE",
            "press_count_operator_plate_identifies_input_pulse");
        AuthoredDemoLadderPrograms.TryCreate("lab-4-01-press-count-lamp", out var document);
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var runtime = _sceneRuntime!;
            _virtualController!.Run();
            // Exercise the actual scene action, mapper, fixed scan session and
            // output projection together. A synthetic false/true input loop
            // alone would miss the original scene's persistent toggle defect.
            var oneEdgePerAction = true;
            for (var press = 1; press <= 3; press++)
            {
                runtime.ExecuteAction("toggle-pulse_received");
                _PhysicsProcess(0.040); // one accepted high scan, then a low scan
                oneEdgePerAction &= _virtualController.Snapshot.Counters["press_count"].Accumulated == press
                    && runtime.Points["pulse_received"] is false
                    && Equals(runtime.Points["threshold_lamp"], press == 3);
            }
            check(oneEdgePerAction, "press_count_three_scene_actions_count_three_edges_and_light_only_at_preset");
            _PhysicsProcess(1.0);
            check(_virtualController.Snapshot.Counters["press_count"].Accumulated == 3
                && runtime.Points["threshold_lamp"] is true,
                "press_count_idle_scans_do_not_recount_button_pulses");
            CommitVirtualControllerSnapshot(_virtualController.Stop());
            check(runtime.Points["threshold_lamp"] is false
                && _virtualController.Snapshot.Counters["press_count"].Accumulated == 3,
                "press_count_stop_removes_lamp_command_without_resetting_counter");
            runtime.ResetSimulation();
            CommitVirtualControllerSnapshot(_virtualController.Reset());
            check(runtime.Points["pulse_received"] is false && runtime.Points["threshold_lamp"] is false
                && _virtualController.Snapshot.Counters["press_count"].Accumulated == 0,
                "press_count_reset_clears_input_output_and_counter");
        }
        finally
        {
            DisableVirtualController();
        }
    }
    private void VerifyCounterResetWorkflow(Action<bool, string> check)
    {
        const string sceneId = "lab-4-02-counter-reset-lamp";
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        check(_sceneCompositionRoot!.GetNode<Node3D>("switch_0").GetNode<Label3D>("OperatorFaceLabel").Text == "COUNT"
            && _sceneCompositionRoot.GetNode<Node3D>("switch_2").GetNode<Label3D>("OperatorFaceLabel").Text == "RESET",
            "counter_reset_button_plates_identify_count_and_reset");

        // Explicit QA controller, never loaded automatically into the exercise.
        // Reset inhibits the count rung and executes before the lamp rung, so
        // simultaneous count/reset cannot briefly command a completed batch.
        var document = new LadderEditorDocument();
        document.ResetProject("review-counter-reset", "Counter_Reset_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        document.AddTag("pulse_received", PlcVariableRole.Input, "pulse_received");
        document.AddTag("reset_pressed", PlcVariableRole.Input, "reset_pressed");
        document.AddTag("counter_lamp", PlcVariableRole.Output, "counter_lamp");
        document.AddTag("press_count", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.WatchVariables.AddRange(["pulse_received", "reset_pressed", "press_count", "counter_lamp"]);
        document.AddCounterRung("Count raw pulses unless resetting", "press_count", 3);
        document.AddContact(0, 0, "pulse_received", false);
        document.AddContact(0, 0, "reset_pressed", true);
        document.AddCounterRung("Reset counter with priority", "press_count", 3, reset: true);
        document.AddContact(1, 0, "reset_pressed", false);
        document.AddRung("Lamp follows counter done", "counter_lamp");
        document.AddContact(2, 0, "press_count.DN", false);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-counter-reset.rpproj.json"),
            LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var runtime = _sceneRuntime!;
            long Count() => _virtualController!.Snapshot.Counters.GetValueOrDefault("press_count")?.Accumulated ?? 0;
            bool Action(string id) => ExecuteSelectedControllerAction(id);
            void Pulse(string id) { Action(id); _PhysicsProcess(.04); }
            RunActiveController();
            var correctEdges = true;
            for (var press = 1; press <= 3; press++)
            {
                correctEdges &= Action("pulse-count"); _PhysicsProcess(.04);
                correctEdges &= Count() == press && runtime.Points["pulse_received"] is false
                    && Equals(runtime.Points["counter_lamp"], press == 3);
            }
            check(correctEdges, "counter_reset_three_actual_actions_count_three_edges_at_reference_preset");
            _PhysicsProcess(1);
            check(Count() == 3 && runtime.Points["counter_lamp"] is true,
                "counter_reset_idle_scans_retain_count_without_recounting");
            Pulse("pulse-reset");
            check(Count() == 0 && runtime.Points["reset_pressed"] is false && runtime.Points["counter_lamp"] is false,
                "counter_reset_momentary_reset_clears_counter_and_lamp_in_running_controller");
            Pulse("pulse-count");
            check(Count() == 1 && runtime.Points["counter_lamp"] is false,
                "counter_reset_new_count_after_reset_starts_at_one");
            Action("pulse-count"); Action("pulse-reset"); _PhysicsProcess(.04);
            check(Count() == 0 && runtime.Points["counter_lamp"] is false
                && runtime.Points["pulse_received"] is false && runtime.Points["reset_pressed"] is false,
                "counter_reset_simultaneous_count_and_reset_gives_reset_priority");
            for (var press = 0; press < 3; press++) Pulse("pulse-count");
            StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber;
            _PhysicsProcess(.2);
            check(Count() == 3 && runtime.Points["counter_lamp"] is false && _virtualController.Snapshot.ScanNumber == scan,
                "counter_reset_stop_removes_output_and_holds_count_and_scan");
            RunActiveController(); _PhysicsProcess(.04);
            check(Count() == 3 && runtime.Points["counter_lamp"] is true,
                "counter_reset_run_restores_done_output_without_an_extra_count");
            Action("pulse-count"); Action("pulse-reset"); StopActiveController();
            check(runtime.Points["pulse_received"] is false && runtime.Points["reset_pressed"] is false,
                "counter_reset_stop_discards_both_unscanned_scene_pulses");
            RunActiveController(); _PhysicsProcess(.04);
            check(Count() == 3 && runtime.Points["counter_lamp"] is true,
                "counter_reset_stopped_pending_actions_cannot_fire_on_later_run");
            ResetActiveController();
            check(Count() == 0 && runtime.Points["counter_lamp"] is false && runtime.Points["pulse_received"] is false
                && runtime.Points["reset_pressed"] is false && _virtualController.Snapshot.ScanNumber == 0
                && _virtualController.Snapshot.State == VirtualControllerState.Stopped,
                "counter_reset_application_reset_clears_all_state_and_stays_stopped");
        }
        finally { DisableVirtualController(); }
    }

}
