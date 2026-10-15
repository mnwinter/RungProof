using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditRunningTower;
    private static readonly string[] RunningTowerColors = ["red", "amber", "green"];

    private void AuditRunningTower()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"RUNNING_TOWER_CHECK {name}={ok}"); }
        try { VerifyRunningTowerWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"RUNNING_TOWER_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private static LadderEditorDocument CreateRunningTowerReference()
    {
        // Original explicit reference, never installed in the blank exercise.
        // The source names a Step input but supplies no timer or wrap behavior.
        var document = new LadderEditorDocument();
        document.ResetProject("review-running-tower", "Running_Tower_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-5-06-running-light-tower";
        foreach (var input in new[] { "tower_enable", "step_pulse" }) document.AddTag(input, PlcVariableRole.Input, input);
        foreach (var color in RunningTowerColors) document.AddTag("tower_" + color, PlcVariableRole.Output, "tower_" + color);
        document.AddTag("step_edge", PlcVariableRole.Memory);
        document.AddTag("was_enabled", PlcVariableRole.Memory);
        document.AddTag("tower_phase", PlcVariableRole.Memory, type: PlcVariableType.DInt);
        // Sample before permissives so a disabled press cannot be deferred.
        // Stop/Reset establish a new edge baseline without a startup pulse.
        document.AddRung("Sample fresh Step edge independently of Enable", "step_edge");
        document.InsertEdgeContact(0, 0, 0, "step_pulse", LadderEdgeMode.Rising);
        document.AddNumericOperationRung("Advance once on a fresh enabled Step", LadderNumericOperationKind.Add, "tower_phase", "1", "tower_phase");
        foreach (var contact in new[] { "tower_enable", "was_enabled", "step_edge" }) document.AddContact(1, 0, contact, false);
        // Reset after increment to wrap green -> red in the same scan.
        // First enable selects red even if Step arrives simultaneously.
        document.AddNumericOperationRung("OFF, first enable or wrap selects phase zero", LadderNumericOperationKind.Move, "0", string.Empty, "tower_phase");
        document.AddContact(2, 0, "tower_enable", true);
        document.AddParallelBranch(2);
        document.AddContact(2, 1, "tower_enable", false); document.AddContact(2, 1, "was_enabled", true);
        document.AddParallelBranch(2);
        document.AddComparison(2, 2, "tower_phase", LadderCompareOperator.GreaterOrEqual, "3");
        document.AddRung("Remember accepted enable for next scan", "was_enabled");
        document.AddContact(3, 0, "tower_enable", false);
        for (var phase = 0; phase < RunningTowerColors.Length; phase++)
        {
            document.AddRung("Select only " + RunningTowerColors[phase], "tower_" + RunningTowerColors[phase]);
            document.AddContact(4 + phase, 0, "tower_enable", false);
            document.AddComparison(4 + phase, 0, "tower_phase", LadderCompareOperator.Equal, phase.ToString());
        }
        document.WatchVariables.AddRange(document.Tags.Select(tag => tag.Name));
        return document;
    }

    private void VerifyRunningTowerWorkflow(Action<bool, string> check)
    {
        void Check(bool ok, string name) => check(ok, "running_tower_" + name);
        AddMigratedScene("lab-5-06-running-light-tower", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var tower = root.GetNode<Node3D>("indicator_1");
        var lenses = ReviewMeshes(tower).Where(mesh => mesh.Name.ToString().StartsWith("LENS_", StringComparison.Ordinal)).ToArray();
        bool Lit(string color) => lenses.Single(mesh => mesh.Name == "LENS_" + color).MaterialOverride is StandardMaterial3D { EmissionEnabled: true };
        bool Display(string? color) => RunningTowerColors.All(name => Lit(name) == (name == color));
        Check(lenses.Length == 3 && RunningTowerColors.All(color => lenses.Any(mesh => mesh.Name == "LENS_" + color)), "three_independent_lenses_present");
        Check(Display(null), "initial_lenses_dark");
        Check(LogBoundsCandidates(root) == 0 && root.GetChildren().OfType<Node3D>().Where(node => ReviewMeshes(node).Length > 0)
            .All(node => ReviewBounds(node).Position.Y >= -.005f), "equipment_separated_and_grounded");
        var selector = root.GetNode<Node3D>("switch_0"); var pointer = (Node3D)selector.FindChild("SELECTOR_direction_inlay", true, false)!;
        var selectorPose = selector.Transform; var pointerPose = pointer.Transform;
        Check(selector.FindChild("KIN_selector_handle", true, false) is Node3D
            && root.GetNode<Node3D>("switch_2").GetNode<Label3D>("OperatorFaceLabel").Text == "STEP", "actual_enable_selector_and_named_momentary_step");
        Check(runtime.Points.Count == 5 && !runtime.Points.ContainsKey("tower_step_active") && runtime.Points["step_pulse"] is false,
            "raw_requests_and_separate_plc_color_outputs");
        runtime.CommitVirtualControllerOutputs(RunningTowerColors.ToDictionary(color => "tower_" + color, _ => true));
        Check(RunningTowerColors.All(Lit), "renderer_does_not_hide_conflicting_commands");
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["tower_red"] = false });
        Check(!Lit("red") && Lit("amber") && Lit("green"), "one_channel_clear_preserves_others");
        runtime.ResetSimulation();
        var document = CreateRunningTowerReference();
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-running-light-tower.rpproj.json"), LadderEditorProjectJson.Save(document));
        var compiled = LadderCompiler.Compile(document.BuildProgram());
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            void Tick(int count = 1) { for (var i = 0; i < count; i++) _PhysicsProcess(.02); }
            void Action(string name) { if (!ExecuteSelectedControllerAction(name)) throw new InvalidOperationException("Rejected tower action: " + name); }
            // Two accepted scans per physical press: assertion then release.
            // Multiple unsampled clicks are one BOOL pulse, not an event queue.
            void Press() { Action("toggle-step_pulse"); Tick(2); }
            double Phase() => _virtualController!.Snapshot.NumericVariables["tower_phase"];
            bool State(string? color, double phase) => Phase() == phase && Display(color)
                && RunningTowerColors.All(name => Equals(runtime.Points["tower_" + name], name == color)) && runtime.Points["step_pulse"] is false;
            RunActiveController(); Tick(2); Check(State(null, 0), "run_with_enable_off_stays_dark");
            Press(); Check(State(null, 0), "disabled_step_discarded");
            Action("toggle-tower_enable"); Action("toggle-step_pulse"); Tick(2);
            Check(State("red", 0), "first_enable_wins_over_simultaneous_step");
            Tick(100); Check(State("red", 0), "idle_scans_do_not_advance");
            Press(); Check(State("amber", 1), "first_step_selects_amber");
            Press(); Check(State("green", 2), "second_step_selects_green");
            Press(); Check(State("red", 0), "third_step_wraps_to_red");
            var exclusive = true;
            for (var step = 0; step < 30; step++) { Press(); exclusive &= State(RunningTowerColors[(step + 1) % 3], (step + 1) % 3); }
            Check(exclusive, "thirty_separate_presses_cycle_with_exactly_one_color");
            Action("toggle-step_pulse"); Action("toggle-step_pulse"); Action("toggle-step_pulse"); Tick(2);
            Check(State("amber", 1), "unsampled_repeated_clicks_coalesce_to_one_step");
            _virtualController!.SetBoolForce("step_pulse", true); Tick(100);
            Check(State("green", 2), "held_raw_step_counts_only_one_edge");
            _virtualController.RemoveForce("step_pulse"); Tick(2);
            Check(State("green", 2), "release_does_not_advance");
            Action("toggle-tower_enable"); Tick(); Check(State(null, 0), "off_clears_outputs_and_phase");
            Press(); Action("toggle-tower_enable"); Tick(2); Check(State("red", 0), "discarded_disabled_step_cannot_execute_on_enable");
            Press(); StopActiveController(); var scan = _virtualController.Snapshot.ScanNumber; Tick(100);
            Check(State(null, 1) && runtime.Points["tower_enable"] is true && _virtualController.Snapshot.ScanNumber == scan,
                "stop_clears_lamps_retains_enable_phase_and_freezes_scan");
            RunActiveController(); Tick(2); Check(State("amber", 1), "run_republishes_retained_phase_without_step");
            Action("toggle-step_pulse"); StopActiveController(); Check(State(null, 1), "stop_discards_unscanned_step");
            RunActiveController(); Tick(2); Check(State("amber", 1), "discarded_step_cannot_execute_after_run");
            Press(); Check(State("green", 2), "fresh_step_after_restart_advances_once");
            StopActiveController(); Action("toggle-step_pulse"); RunActiveController(); Tick(2);
            Check(State("green", 2), "first_run_scan_establishes_edge_baseline_without_startup_step");
            Action("toggle-tower_enable"); Tick(); Action("toggle-tower_enable"); Tick(); Check(State("red", 0), "reenable_returns_to_first_color");
            Check(selector.Transform == selectorPose, "selector_root_does_not_move");
            ResetActiveController();
            Check(State(null, 0) && runtime.Points["tower_enable"] is false && pointer.Transform.IsEqualApprox(pointerPose)
                && _virtualController.Snapshot.ScanNumber == 0 && _virtualController.Snapshot.State == VirtualControllerState.Stopped,
                "reset_restores_off_detent_all_state_zero_and_stopped");
        }
        finally { DisableVirtualController(); }
    }
}
