using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditFlashPair;
    private void AuditFlashPair()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"FLASH_PAIR_CHECK {name}={ok}"); }
        try { VerifyFlashPairWorkflows(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"FLASH_PAIR_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyFlashPairWorkflows(Action<bool, string> check)
    {
        VerifyFlashPairWorkflow(check, alternating: true);
        VerifyFlashPairWorkflow(check, alternating: false);
    }

    private static LadderEditorDocument CreateFlashPairReference(bool alternating)
    {
        // Original explicitly opened offline references. The source lessons
        // supply no preset. Do not populate the user's blank exercise editor.
        var sceneId = alternating ? "lab-5-04-alternating-lamps" : "lab-5-05-variable-flash-rate";
        var document = new LadderEditorDocument();
        document.ResetProject("review-" + sceneId, alternating ? "Alternating_Lamps_Reference" : "Variable_Flash_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        foreach (var input in alternating ? new[] { "alternate_enable" } : new[] { "flash_enable", "fast_rate_selected", "slow_rate_selected" })
            document.AddTag(input, PlcVariableRole.Input, input);
        foreach (var output in alternating ? new[] { "lamp_a", "lamp_b" } : new[] { "rate_lamp" })
            document.AddTag(output, PlcVariableRole.Output, output);
        document.AddTag("phase_on", PlcVariableRole.Memory);
        if (!alternating)
        {
            foreach (var mode in new[] { "fast_valid", "slow_valid", "rate_valid" }) document.AddTag(mode, PlcVariableRole.Memory);
            document.AddRung("Enable FAST only without SLOW", "fast_valid");
            document.AddContact(0, 0, "flash_enable", false); document.AddContact(0, 0, "fast_rate_selected", false);
            document.AddContact(0, 0, "slow_rate_selected", true);
            document.AddRung("Enable SLOW only without FAST", "slow_valid");
            document.AddContact(1, 0, "flash_enable", false); document.AddContact(1, 0, "slow_rate_selected", false);
            document.AddContact(1, 0, "fast_rate_selected", true);
            document.AddRung("Exactly one requested rate is valid", "rate_valid");
            document.AddContact(2, 0, "fast_valid", false); document.AddParallelBranch(2); document.AddContact(2, 1, "slow_valid", false);
        }
        var modes = alternating ? new[] { "alternate_enable" } : new[] { "fast_valid", "slow_valid" };
        var prefixes = alternating ? new[] { "" } : new[] { "fast_", "slow_" };
        var timerA = alternating ? "a_timer" : "off_timer";
        var timerB = alternating ? "b_timer" : "on_timer";
        for (var mode = 0; mode < modes.Length; mode++)
        {
            var preset = TimeSpan.FromMilliseconds(!alternating && mode == 0 ? 200 : 500);
            foreach (var suffix in new[] { timerA, timerB }) document.AddTag(prefixes[mode] + suffix, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var index = document.Rungs.Count;
            document.AddTimerRung("Time first phase on accepted scans", prefixes[mode] + timerA, preset);
            document.AddContact(index, 0, modes[mode], false); document.AddContact(index, 0, "phase_on", true);
            document.AddTimerRung("Time second phase on accepted scans", prefixes[mode] + timerB, preset);
            document.AddContact(index + 1, 0, modes[mode], false); document.AddContact(index + 1, 0, "phase_on", false);
        }
        // All timers precede the phase coil. Each timer starts on the scan
        // after phase transition, avoiding an extra transition scan per period.
        var enabled = alternating ? "alternate_enable" : "rate_valid";
        var phaseRung = document.Rungs.Count;
        document.AddRung("Advance phase at first timer done; hold until second timer done", "phase_on");
        for (var mode = 0; mode < modes.Length; mode++)
        {
            if (mode > 0) document.AddParallelBranch(phaseRung);
            document.AddContact(phaseRung, mode, modes[mode], false);
            document.AddContact(phaseRung, mode, prefixes[mode] + timerA + ".Q", false);
        }
        document.AddParallelBranch(phaseRung);
        document.AddContact(phaseRung, modes.Length, enabled, false);
        document.AddContact(phaseRung, modes.Length, "phase_on", false);
        foreach (var prefix in prefixes) document.AddContact(phaseRung, modes.Length, prefix + timerB + ".Q", true);
        var lampRung = document.Rungs.Count;
        document.AddRung(alternating ? "Lamp A amber during first PLC phase" : "Rate lamp follows selected PLC phase", alternating ? "lamp_a" : "rate_lamp");
        document.AddContact(lampRung, 0, enabled, false); document.AddContact(lampRung, 0, "phase_on", alternating);
        if (alternating)
        {
            document.AddRung("Lamp B green during second PLC phase", "lamp_b");
            document.AddContact(lampRung + 1, 0, enabled, false); document.AddContact(lampRung + 1, 0, "phase_on", false);
        }
        document.WatchVariables.AddRange(document.Tags.Select(t => t.Name));
        return document;
    }

    private void VerifyFlashPairWorkflow(Action<bool, string> check, bool alternating)
    {
        var sceneId = alternating ? "lab-5-04-alternating-lamps" : "lab-5-05-variable-flash-rate";
        void Check(bool ok, string name) => check(ok, (alternating ? "alternating_" : "variable_rate_") + name);
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        Check(LogBoundsCandidates(root) == 0 && root.GetChildren().OfType<Node3D>().Where(n => ReviewMeshes(n).Length > 0)
            .All(n => ReviewBounds(n).Position.Y >= -.005f), "equipment_separated_and_grounded");
        var selectors = alternating ? new[] { "switch_0" } : new[] { "switch_0", "switch_1", "switch_3" };
        var poses = selectors.ToDictionary(id => id, id => root.GetNode<Node3D>(id).Transform);
        var pointerPoses = selectors.ToDictionary(id => id,
            id => ((Node3D)root.GetNode<Node3D>(id).FindChild("SELECTOR_direction_inlay", true, false)!).Transform);
        Check(selectors.All(id => root.GetNode<Node3D>(id).FindChild("KIN_selector_handle", true, false) is Node3D), "all_held_inputs_are_actual_selectors");
        Check(alternating ? runtime.Points.Count == 3 && !runtime.Points.ContainsKey("alternate_phase") && root.GetNodeOrNull<Node3D>("switch_3") is null
            : runtime.Points.Count == 4 && runtime.Points["fast_rate_selected"] is false && runtime.Points["slow_rate_selected"] is false,
            "raw_inputs_only_no_precomputed_phase");
        Check(root.GetNode<Node3D>(alternating ? "indicator_1" : "indicator_2").FindChild("LENS_single", true, false) is MeshInstance3D
            && (!alternating || root.GetNode<Node3D>("indicator_2").FindChild("LENS_single", true, false) is MeshInstance3D), "single_lamps_match_outputs");
        var document = CreateFlashPairReference(alternating);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-" + (alternating ? "alternating-lamps" : "variable-flash-rate") + ".rpproj.json"), LadderEditorProjectJson.Save(document));
        var program = document.BuildProgram(); var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        try
        {
            void Tick(int n = 1) { for (var i = 0; i < n; i++) _PhysicsProcess(.02); }
            void Toggle(string point) { if (!ExecuteSelectedControllerAction("toggle-" + point)) throw new InvalidOperationException("Selector action rejected: " + point); }
            bool On(string point) => runtime.Points[point] is true;
            bool Time(string name, double value) => Math.Abs(_virtualController!.Snapshot.Timers[name].Accumulated.TotalSeconds - value) < 1e-9;
            bool TimersZero() => _virtualController!.Snapshot.Timers.Values.All(t => t.Accumulated == TimeSpan.Zero);
            bool Dark() => alternating ? !On("lamp_a") && !On("lamp_b") : !On("rate_lamp");
            RunActiveController(); Tick(10); Check(Dark() && TimersZero(), "initial_run_is_dark_and_untimed");
            if (alternating)
            {
                Toggle("alternate_enable"); Tick(); Check(On("lamp_a") && !On("lamp_b") && Time("a_timer", .02), "first_enabled_scan_lights_only_a");
                Tick(23); Check(On("lamp_a") && !On("lamp_b") && Time("a_timer", .48), "a_at_24th_scan");
                Tick(); Check(!On("lamp_a") && On("lamp_b") && Time("a_timer", .5), "b_at_25th_scan");
                Tick(24); Check(!On("lamp_a") && On("lamp_b") && Time("b_timer", .48), "b_before_expiry");
                Tick(); Check(On("lamp_a") && !On("lamp_b") && Time("b_timer", .5), "a_at_50th_scan");
                var exclusive = true; var periodic = true;
                for (var i = 1; i <= 200; i++)
                {
                    Tick(); exclusive &= On("lamp_a") != On("lamp_b");
                    if (i % 25 == 0) periodic &= On("lamp_b") == (i / 25 % 2 == 1);
                }
                Check(exclusive && periodic, "200_scans_exactly_one_lamp_and_no_phase_drift");
                Toggle("alternate_enable"); Tick(); Check(Dark() && TimersZero(), "disable_clears_both_lamps_and_timers");
                Tick(100); Check(Dark() && TimersZero(), "disabled_stays_dark");
                Toggle("alternate_enable"); Tick(25); Check(!On("lamp_a") && On("lamp_b"), "reenable_starts_a_then_b_after_full_phase");
                StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1);
                Check(Dark() && TimersZero() && On("alternate_enable") && _virtualController.Snapshot.ScanNumber == scan,
                    "stop_clears_lamps_and_timers_retains_enable_freezes_scan");
                RunActiveController(); Tick(24); Check(!On("lamp_a") && On("lamp_b") && Time("b_timer", .48), "run_retains_b_with_fresh_interval");
                Tick(); Check(On("lamp_a") && !On("lamp_b"), "restarted_b_expires_after_25_fresh_scans");
                Toggle("alternate_enable"); Tick(); Check(Dark() && TimersZero(), "disable_after_restart_clears_all");
            }
            else
            {
                Toggle("flash_enable"); Tick(30); Check(Dark() && TimersZero(), "enabled_without_rate_is_inhibited");
                Toggle("fast_rate_selected"); Tick(9); Check(Dark() && Time("fast_off_timer", .18), "fast_off_at_nine_scans");
                Tick(); Check(On("rate_lamp") && Time("fast_off_timer", .2), "fast_on_at_tenth_scan");
                Tick(9); Check(On("rate_lamp") && Time("fast_on_timer", .18), "fast_on_before_expiry");
                Tick(); Check(Dark() && Time("fast_on_timer", .2), "fast_off_at_twentieth_scan");
                var periodic = true;
                for (var half = 1; half <= 8; half++) { Tick(10); periodic &= On("rate_lamp") == (half % 2 == 1); }
                Check(periodic, "eight_fast_half_periods_without_drift");
                Toggle("slow_rate_selected"); Tick(); Check(Dark() && TimersZero(), "simultaneous_rates_inhibit_and_clear_timing");
                Toggle("fast_rate_selected"); Tick(24); Check(Dark() && Time("slow_off_timer", .48), "corrected_slow_starts_full_off_interval");
                Tick(); Check(On("rate_lamp") && Time("slow_off_timer", .5), "slow_on_at_25th_scan");
                Tick(24); Check(On("rate_lamp") && Time("slow_on_timer", .48), "slow_on_before_expiry");
                Tick(); Check(Dark() && Time("slow_on_timer", .5), "slow_off_at_50th_scan");
                Tick(25); Check(On("rate_lamp"), "slow_next_on_after_full_off_half");
                Toggle("slow_rate_selected"); Toggle("fast_rate_selected"); Tick();
                Check(On("rate_lamp") && Time("slow_on_timer", 0) && Time("fast_on_timer", .02), "direct_rate_change_retains_phase_starts_new_rate_time");
                Tick(8); Check(On("rate_lamp") && Time("fast_on_timer", .18), "changed_fast_phase_before_expiry");
                Tick(); Check(Dark() && Time("fast_on_timer", .2), "changed_fast_phase_expires_at_tenth_new_scan");
                Toggle("fast_rate_selected"); Tick(); Check(Dark() && TimersZero(), "neither_rate_clears_timing_again");
                Toggle("flash_enable"); Toggle("slow_rate_selected"); Tick(100); Check(Dark() && TimersZero(), "selected_rate_cannot_override_disabled_enable");
                Toggle("flash_enable"); Tick(25); Check(On("rate_lamp"), "enable_with_retained_slow_starts_fresh_off_interval");
                StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1);
                Check(Dark() && TimersZero() && On("flash_enable") && On("slow_rate_selected") && _virtualController.Snapshot.ScanNumber == scan,
                    "stop_resets_timers_retains_requests_and_freezes_scan");
                RunActiveController(); Tick(24); Check(On("rate_lamp") && Time("slow_on_timer", .48), "run_resumes_retained_on_with_full_slow_interval");
                Tick(); Check(Dark() && Time("slow_on_timer", .5), "resumed_slow_on_expires_after_25_fresh_scans");
                Toggle("flash_enable"); Tick(); Check(Dark() && TimersZero(), "enable_off_cancels_after_restart");
            }
            Check(selectors.All(id => root.GetNode<Node3D>(id).Transform == poses[id]), "station_roots_do_not_move_with_selection");
            ResetActiveController();
            Check(Dark() && TimersZero() && selectors.All(id =>
                ((Node3D)root.GetNode<Node3D>(id).FindChild("SELECTOR_direction_inlay", true, false)!).Transform.IsEqualApprox(pointerPoses[id]))
                && runtime.Points.Where(p => !runtime.IsPlcOwnedPoint(p.Key)).All(p => p.Value is false)
                && _virtualController!.Snapshot.ScanNumber == 0 && _virtualController.Snapshot.State == VirtualControllerState.Stopped,
                "reset_all_inputs_lamps_timers_zero_and_stopped");
        }
        finally { DisableVirtualController(); }
    }
}
