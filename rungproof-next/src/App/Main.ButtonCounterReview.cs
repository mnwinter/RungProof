using System;
using System.Collections.Generic;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditButtonCounters;

    private void AuditButtonCounters()
    {
        var failures = 0;
        void Check(bool value, string label)
        { if (!value) failures++; GD.Print($"BUTTON_COUNTER_CHECK {label}={value}"); }
        try { VerifyButtonCounterWorkflows(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"BUTTON_COUNTER_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyButtonCounterWorkflows(Action<bool, string> check)
    {
        VerifyButtonCounterWorkflow(check, exact: false);
        VerifyButtonCounterWorkflow(check, exact: true);
    }

    private void VerifyButtonCounterWorkflow(Action<bool, string> check, bool exact)
    {
        var sceneId = exact ? "lab-4-06-multi-press-confirmation" : "lab-4-05-dual-input-count-window";
        var prefix = exact ? "multi_press" : "dual_count";
        void Check(bool value, string label) => check(value, $"{prefix}_{label}");
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var a = exact ? "button_a_pressed" : "channel_a_pulse";
        var b = exact ? "button_b_pressed" : "channel_b_pulse";
        var output = exact ? "confirmation_valid" : "window_ready";
        var countA = exact ? "button_a_count" : "channel_a_count";
        var countB = exact ? "button_b_count" : "channel_b_count";
        Check(root.GetNode<Node3D>("switch_0").GetNode<Label3D>("OperatorFaceLabel").Text == "A"
            && root.GetNode<Node3D>("switch_1").GetNode<Label3D>("OperatorFaceLabel").Text == "B"
            && root.GetNode<Node3D>("reset_button").GetNode<Label3D>("OperatorFaceLabel").Text == "RESET",
            "physical_buttons_identify_a_b_and_reset");
        Check(!runtime.Points.ContainsKey(exact ? "button_a_pattern_ok" : "channel_a_ready")
            && !runtime.Points.ContainsKey(exact ? "button_b_pattern_ok" : "channel_b_ready")
            && runtime.Points[a] is false && runtime.Points[b] is false,
            "raw_presses_replace_precomputed_results");

        // Original offline references, explicitly opened by the reviewer. The
        // source lessons supply no numeric limits, elapsed time or press order.
        // Never silently install either reference into the blank exercise editor.
        var document = new LadderEditorDocument();
        document.ResetProject($"review-{prefix}", exact ? "Multi_Press_Reference" : "Dual_Count_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        foreach (var point in new[] { a, b, "reset_pressed" }) document.AddTag(point, PlcVariableRole.Input, point);
        document.AddTag(output, PlcVariableRole.Output, output);
        foreach (var point in new[] { countA, countB }) document.AddTag(point, PlcVariableRole.Output, point, PlcVariableType.DInt);
        foreach (var counter in new[] { "a_counter", "b_counter" }) document.AddTag(counter, PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("publish_counts", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.AddRange([a, b, "reset_pressed", "a_counter", "b_counter", countA, countB, output]);
        for (var channel = 0; channel < 2; channel++)
        {
            var counter = channel == 0 ? "a_counter" : "b_counter";
            document.AddCounterRung($"Count raw {(channel == 0 ? "A" : "B")} presses unless resetting", counter, channel + 2);
            document.AddContact(channel, 0, channel == 0 ? a : b, false);
            document.AddContact(channel, 0, "reset_pressed", true);
        }
        for (var channel = 0; channel < 2; channel++)
        {
            document.AddCounterRung("Reset with priority", channel == 0 ? "a_counter" : "b_counter", channel + 2, reset: true);
            document.AddContact(channel + 2, 0, "reset_pressed", false);
        }
        document.AddRung(exact ? "Confirm exactly A=2 and B=3" : "Ready inside inclusive A=2..3 and B=3..4 windows", output);
        document.AddComparison(4, 0, "a_counter.ACC", exact ? LadderCompareOperator.Equal : LadderCompareOperator.GreaterOrEqual, "2");
        document.AddComparison(4, 0, "b_counter.ACC", exact ? LadderCompareOperator.Equal : LadderCompareOperator.GreaterOrEqual, "3");
        if (!exact)
        {
            document.AddComparison(4, 0, "a_counter.ACC", LadderCompareOperator.LessOrEqual, "3");
            document.AddComparison(4, 0, "b_counter.ACC", LadderCompareOperator.LessOrEqual, "4");
        }
        for (var channel = 0; channel < 2; channel++)
        {
            document.AddNumericOperationRung("Publish actual PLC accumulated count", LadderNumericOperationKind.Move,
                channel == 0 ? "a_counter.ACC" : "b_counter.ACC", string.Empty, channel == 0 ? countA : countB);
            document.AddContact(channel + 5, 0, "publish_counts", false);
        }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://.tools/plant-review-{prefix.Replace('_', '-')}.rpproj.json"), LadderEditorProjectJson.Save(document));
        var program = document.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Button-counter reference does not compile: " + string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        try
        {
            long Count(string name) => _virtualController!.Snapshot.Counters.GetValueOrDefault(name)?.Accumulated ?? 0;
            bool State(long ca, long cb, bool on) => Count("a_counter") == ca && Count("b_counter") == cb
                && Convert.ToInt64(runtime.Points[countA]) == ca && Convert.ToInt64(runtime.Points[countB]) == cb
                && Equals(runtime.Points[output], on) && runtime.Points[a] is false && runtime.Points[b] is false && runtime.Points["reset_pressed"] is false;
            void Action(string id) { if (!ExecuteSelectedControllerAction(id)) throw new InvalidOperationException($"Scene action rejected: {id}"); }
            void Pulse(string id) { Action(id); _PhysicsProcess(.04); }
            RunActiveController(); _PhysicsProcess(.04);
            Check(State(0, 0, false), "initial_counts_and_indication_zero");
            Pulse("pulse-a"); Check(State(1, 0, false), "a_press_does_not_count_b");
            Pulse("pulse-a"); Check(State(2, 0, false), "a_condition_alone_cannot_enable_indication");
            Pulse("pulse-b"); Pulse("pulse-b"); Check(State(2, 2, false), "b_below_lower_bound_remains_off");
            Pulse("pulse-b"); Check(State(2, 3, true), "both_lower_bounds_enable_indication");
            _PhysicsProcess(1); Check(State(2, 3, true), "idle_scans_do_not_recount");
            Pulse("pulse-a"); Check(State(3, 3, !exact), "a_extra_press_obeys_exact_or_inclusive_upper_limit");
            Pulse("pulse-a"); Check(State(4, 3, false), "a_over_limit_removes_indication");
            Pulse("pulse-reset"); Check(State(0, 0, false), "operator_reset_clears_both_counters");
            for (var press = 0; press < 3; press++) Pulse("pulse-b");
            Check(State(0, 3, false), "b_condition_alone_cannot_enable_indication");
            Pulse("pulse-a"); Pulse("pulse-a"); Check(State(2, 3, true), "reverse_channel_order_is_accepted");
            Pulse("pulse-b"); Check(State(2, 4, !exact), "b_extra_press_obeys_exact_or_inclusive_upper_limit");
            Pulse("pulse-b"); Check(State(2, 5, false), "b_over_limit_removes_indication");
            Action("pulse-a"); Action("pulse-b"); Action("pulse-reset"); _PhysicsProcess(.04);
            Check(State(0, 0, false), "simultaneous_reset_has_priority_over_both_presses");
            Action("pulse-a"); Action("pulse-a"); Action("pulse-b"); _PhysicsProcess(.04);
            Check(State(1, 1, false), "unscanned_same_channel_clicks_coalesce_but_channels_remain_independent");
            Pulse("pulse-a"); Pulse("pulse-b"); Pulse("pulse-b"); Check(State(2, 3, true), "valid_counts_can_be_reestablished");
            StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(.2);
            Check(Count("a_counter") == 2 && Count("b_counter") == 3 && runtime.Points[output] is false
                && Convert.ToInt64(runtime.Points[countA]) == 0 && Convert.ToInt64(runtime.Points[countB]) == 0
                && _virtualController.Snapshot.ScanNumber == scan, "stop_clears_command_outputs_but_holds_counters_and_scan");
            RunActiveController(); _PhysicsProcess(.04); Check(State(2, 3, true), "run_republishes_retained_counts_without_extra_edges");
            Action("pulse-a"); Action("pulse-b"); Action("pulse-reset"); StopActiveController();
            Check(runtime.Points[a] is false && runtime.Points[b] is false && runtime.Points["reset_pressed"] is false, "stop_discards_all_unscanned_pulses");
            RunActiveController(); _PhysicsProcess(.04); Check(State(2, 3, true), "discarded_pulses_cannot_execute_after_run");
            ResetActiveController();
            Check(State(0, 0, false) && _virtualController!.Snapshot.ScanNumber == 0
                && _virtualController.Snapshot.State == VirtualControllerState.Stopped, "application_reset_clears_all_state_and_stays_stopped");
        }
        finally { DisableVirtualController(); }
    }
}
