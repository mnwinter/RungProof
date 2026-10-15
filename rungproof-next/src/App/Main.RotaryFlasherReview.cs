using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditRotaryFlasher;
    private void AuditRotaryFlasher()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"ROTARY_FLASHER_CHECK {name}={ok}"); }
        try { VerifyRotaryFlasherWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"ROTARY_FLASHER_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyRotaryFlasherWorkflow(Action<bool, string> check)
    {
        void Check(bool ok, string name) => check(ok, "rotary_flasher_" + name);
        const string sceneId = "lab-5-03-rotary-flasher";
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var selector = root.GetNode<Node3D>("switch_0"); var initialPose = selector.Transform;
        Check(LogBoundsCandidates(root) == 0 && root.GetChildren().OfType<Node3D>().Where(n => ReviewMeshes(n).Length > 0)
            .All(n => ReviewBounds(n).Position.Y >= -.005f), "separated_and_grounded");
        Check(selector.FindChild("KIN_selector_handle", true, false) is Node3D
            && selector.GetNode<Label3D>("OperatorFaceLabel").Text == "MODE"
            && selector.GetNode<Label3D>("SelectorOrdinal_0").Text == "OFF"
            && selector.GetNode<Label3D>("SelectorOrdinal_1").Text == "FLASH", "rotary_mode_off_flash_identified");
        Check(runtime.Points.Count == 2 && !runtime.Points.ContainsKey("flash_tick") && root.GetNodeOrNull<Node3D>("switch_2") is null,
            "only_raw_mode_and_plc_lamp_no_manual_tick");

        // Original offline reference. The source supplies no frequency. Each
        // half-period uses 25 accepted scans, with both TONs evaluated before
        // the phase coil. Do not silently install this in the exercise editor.
        var document = new LadderEditorDocument();
        document.ResetProject("review-rotary-flasher", "Rotary_Flasher_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        document.AddTag("flash_mode_selected", PlcVariableRole.Input, "flash_mode_selected");
        document.AddTag("flash_lamp", PlcVariableRole.Output, "flash_lamp");
        document.AddTag("phase_on", PlcVariableRole.Memory);
        foreach (var name in new[] { "off_timer", "on_timer" }) document.AddTag(name, PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.WatchVariables.AddRange(["flash_mode_selected", "off_timer", "on_timer", "phase_on", "flash_lamp"]);
        document.AddTimerRung("Half-second OFF phase", "off_timer", TimeSpan.FromMilliseconds(500));
        document.AddContact(0, 0, "flash_mode_selected", false); document.AddContact(0, 0, "phase_on", true);
        document.AddTimerRung("Half-second ON phase", "on_timer", TimeSpan.FromMilliseconds(500));
        document.AddContact(1, 0, "flash_mode_selected", false); document.AddContact(1, 0, "phase_on", false);
        document.AddRung("Enter ON after OFF time; hold until ON time completes", "phase_on");
        document.AddContact(2, 0, "flash_mode_selected", false); document.AddContact(2, 0, "off_timer.Q", false);
        document.AddParallelBranch(2);
        document.AddContact(2, 1, "flash_mode_selected", false); document.AddContact(2, 1, "phase_on", false);
        document.AddContact(2, 1, "on_timer.Q", true);
        document.AddRung("Lamp follows selected PLC phase", "flash_lamp");
        document.AddContact(3, 0, "flash_mode_selected", false); document.AddContact(3, 0, "phase_on", false);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-rotary-flasher.rpproj.json"), LadderEditorProjectJson.Save(document));
        var program = document.BuildProgram(); var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        try
        {
            void Tick(int n = 1) { for (var i = 0; i < n; i++) _PhysicsProcess(.02); }
            void Toggle() { if (!ExecuteSelectedControllerAction("toggle-flash_mode_selected")) throw new InvalidOperationException("Mode input rejected."); }
            bool Lamp() => runtime.Points["flash_lamp"] is true;
            double Et(string name) => _virtualController!.Snapshot.Timers[name].Accumulated.TotalSeconds;
            bool Time(string name, double value) => Math.Abs(Et(name) - value) < 1e-9;
            RunActiveController(); Tick(30);
            Check(!Lamp() && Time("off_timer", 0) && Time("on_timer", 0), "idle_off_cannot_flash");
            Toggle(); Tick(24); Check(!Lamp() && Time("off_timer", .48), "off_half_on_boundary_minus_one_scan");
            Tick(); Check(Lamp() && Time("off_timer", .5) && Time("on_timer", 0), "on_at_25th_selected_scan");
            Tick(24); Check(Lamp() && Time("on_timer", .48) && Time("off_timer", 0), "on_half_before_expiry");
            Tick(); Check(!Lamp() && Time("on_timer", .5), "off_at_50th_selected_scan");
            Tick(25); Check(Lamp() && Time("off_timer", .5), "next_on_at_75th_scan");
            var periodic = true;
            for (var phase = 0; phase < 6; phase++) { Tick(25); periodic &= Lamp() == (phase % 2 == 1); }
            Check(periodic, "six_more_half_periods_have_no_scan_drift");
            Toggle(); Tick(); Check(!Lamp() && Time("off_timer", 0) && Time("on_timer", 0), "off_clears_lamp_phase_and_timers");
            Tick(100); Check(!Lamp() && Time("off_timer", 0), "off_stays_dark_after_wall_interval");
            Toggle(); Tick(24); Check(!Lamp() && Time("off_timer", .48), "new_flash_starts_full_off_half");
            Tick(); Tick(10); Check(Lamp() && Time("on_timer", .2), "on_phase_before_stop");
            StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1);
            Check(!Lamp() && Time("off_timer", 0) && Time("on_timer", 0) && runtime.Points["flash_mode_selected"] is true
                && _virtualController.Snapshot.ScanNumber == scan, "stop_dark_resets_timers_retains_mode_freezes_scan");
            RunActiveController(); Tick(24); Check(Lamp() && Time("on_timer", .48), "run_resumes_retained_on_with_fresh_half_period");
            Tick(); Check(!Lamp() && Time("on_timer", .5), "resumed_on_expires_after_25_fresh_scans");
            StopActiveController(); RunActiveController(); Tick(24); Check(!Lamp() && Time("off_timer", .48), "off_phase_restart_requires_fresh_half_period");
            Tick(); Check(Lamp(), "off_phase_restart_completes_at_25");
            Toggle(); Tick(); Check(!Lamp() && Time("off_timer", 0) && Time("on_timer", 0), "operator_off_cancels_restarted_flashing");
            Check(selector.Transform == initialPose, "station_does_not_move_with_selector_projection");
            ResetActiveController();
            Check(!Lamp() && runtime.Points["flash_mode_selected"] is false && Time("off_timer", 0) && Time("on_timer", 0)
                && _virtualController!.Snapshot.ScanNumber == 0 && _virtualController.Snapshot.State == VirtualControllerState.Stopped,
                "reset_off_zero_timers_scan_and_stopped");
        }
        finally { DisableVirtualController(); }
    }
}
