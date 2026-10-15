using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditToteFinishingProcess;
    private void AuditToteFinishingProcess()
    {
        var checks = 0; var failures = 0;
        void Check(bool ok, string name)
        { checks++; if (!ok) failures++; GD.Print($"TOTE_PROCESS_CHECK {name}={ok}"); }
        try
        {
            AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
            var runtime = _sceneRuntime!; var root = _sceneCompositionRoot!;
            runtime.UsesExternalClock = true; runtime.SetControllerPlaybackRunning(true);
            bool On(string name) => Convert.ToBoolean(runtime.Points[name]);
            double Number(string name) => Convert.ToDouble(runtime.Points[name]);
            void Tick(int count = 1, double dt = .02) { for (var i = 0; i < count; i++) runtime.AdvanceSimulation(dt); }
            void Command(bool belt = false, bool fill = false, bool cap = false, bool label = false, bool inspect = false)
                => runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> {
                    ["conveyor_run"] = belt, ["fill_valve_open"] = fill, ["capper_run"] = cap,
                    ["labeler_run"] = label, ["inspection_run"] = inspect });
            void TravelTo(string station)
            {
                var x = root.GetNode<Node3D>(station).Position.X;
                Command(belt: true);
                var limit = 1000;
                while (x - Number("tote_position") > .015000001 && limit-- > 0) Tick();
                var remaining = x - Number("tote_position");
                if (remaining > 1e-8) Tick(1, remaining / .75);
                Command(); Tick();
            }
            var tote = root.GetNode<Node3D>("finishing_tote");
            var retained = (MeshInstance3D)tote.FindChild("FINISHING_applied_label", true, false);
            var tamp = (MeshInstance3D)root.GetNode<Node3D>("labeler").FindChild("LABEL_ON_TAMP", true, false);
            Check(!retained.Visible && !On("label_applied") && !On("inspection_ok"), "initial_unmarked_uninspected");
            Command(label: true, inspect: true); Tick(50);
            Check(!On("label_applied") && On("label_inhibited") && !On("inspection_done") && On("inspection_inhibited"),
                "off_station_commands_cannot_fake_label_or_inspection");
            Command(); Tick();
            TravelTo("finishing_fill_valve"); Command(fill: true); Tick(260);
            Check(On("fill_complete"), "real_normalized_fill_before_label");
            Command(); TravelTo("capper"); Command(cap: true); Tick(90);
            Check(On("cap_applied") && On("cap_home"), "real_cap_application_before_label");
            Command(); TravelTo("labeler");
            var labelerFixture = root.GetNode<Node3D>("labeler");
            var labelerHome = labelerFixture.Position;
            labelerFixture.Position += Vector3.Right * .0225f;
            Command(label: true); Tick();
            Check(On("tote_at_label") && !On("tote_label_aligned") && On("label_inhibited") && !On("label_applied"),
                "coarse_station_window_does_not_authorize_impossible_pad_contact");
            labelerFixture.Position = labelerHome;
            labelerFixture.Position += Vector3.Back * .10f;
            Command(label: true); Tick(100);
            Check(On("tote_at_label") && !On("tote_label_aligned") && On("label_inhibited")
                && !On("label_applied") && Number("label_extension_percent") == 0 && !retained.Visible,
                "station_depth_shift_cannot_fake_contact_with_cached_short_stroke");
            labelerFixture.Position = labelerHome;
            Command(label: true); Tick(10);
            Check(Number("label_extension_percent") > 0 && Number("label_extension_percent") < 100 && !retained.Visible,
                "measured_tamp_moves_before_mark_is_applied");
            var extension = Number("label_extension_percent");
            runtime.SetControllerPlaybackRunning(false); Tick(20);
            Check(Number("label_extension_percent") == extension && !retained.Visible, "stop_retains_partial_stroke_without_mark");
            runtime.SetControllerPlaybackRunning(true);
            var position = Number("tote_position"); Command(belt: true, label: true); Tick();
            Check(On("tote_transfer_inhibited") && Number("tote_position") == position,
                "extended_pad_blocks_belt_without_changing_plc_command");
            for (var i = 0; i < 100 && On("label_busy"); i++) Tick();
            Command(); Tick();
            Check(!On("label_applied") && On("label_home"), "moving_request_aborts_label_and_retracts");
            Command(label: true); Tick(30);
            Check(!retained.Visible && Number("label_extension_percent") == 100
                && Math.Abs(ReviewBounds(tamp).Position.Z - ReviewBounds(retained).End.Z) < .001,
                "actual_tamp_face_contacts_carrier_before_label_release");
            var labeler = root.GetNode<Node3D>("labeler");
            var slide = (MeshInstance3D)labeler.FindChild("TAMP_SLIDE", true, false);
            var cylinder = (MeshInstance3D)labeler.FindChild("TAMP_AIR_CYLINDER", true, false);
            var pad = (MeshInstance3D)labeler.FindChild("APPLICATOR_PAD", true, false);
            Check(ReviewBounds(slide).Intersects(ReviewBounds(cylinder)) && ReviewBounds(slide).Intersects(ReviewBounds(pad)),
                "full_stroke_telescopic_rod_remains_engaged_with_cylinder_and_pad");
            Tick(30);
            Check(retained.Visible && !tamp.Visible && On("label_applied") && On("label_home"), "contact_dwell_retains_visible_label_and_retracts_pad");
            Command(); TravelTo("vision_inspector");
            Check(On("inspection_view_clear"), "outboard_camera_has_actual_unobstructed_label_corner_rays");
            Command(inspect: true); Tick(10);
            Check(On("inspection_busy") && !On("inspection_done") && !On("inspection_ok"), "inspection_waits_for_acquisition");
            runtime.SetControllerPlaybackRunning(false); Tick(40);
            Check(!On("inspection_done") && !On("inspection_busy"), "stop_cannot_complete_acquisition");
            runtime.SetControllerPlaybackRunning(true); Tick(20);
            Check(On("inspection_done") && On("inspection_ok") && Number("inspection_defect_code") == 0,
                "complete_visible_fixture_passes_educational_condition_inspection");
            Command(belt: true); Tick(200); Command(); Tick();
            Check(On("tote_at_exit") && retained.Visible && On("inspection_ok"), "discharged_tote_retains_applied_mark_and_result");
            runtime.ResetSimulation();
            Check(!retained.Visible && !On("label_applied") && !On("inspection_ok") && !On("inspection_done"),
                "reset_removes_mark_and_quality_result");
            TravelTo("vision_inspector"); Command(inspect: true); Tick(30);
            Check(On("inspection_done") && !On("inspection_ok") && Number("inspection_defect_code") == 7,
                "unfilled_uncapped_unlabeled_tote_fails_actual_modeled_conditions");
            VerifyToteFinishingController(Check);
        }
        catch (Exception ex) { failures++; GD.PushError(ex.ToString()); }
        GD.Print($"TOTE_PROCESS_VERIFY {(failures == 0 ? "PASS" : "FAIL")} checks={checks} failures={failures}; contact and modeled condition visibility only, native review pending");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyToteFinishingController(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
        var loaded = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(
            "programs/examples/tote-finishing-process-review.rpproj.json"));
        if (loaded.Document is null) throw new InvalidOperationException("Full finishing PLC reference could not be loaded.");
        EnableVirtualControllerProgram(loaded.Document.BuildProgram());
        try
        {
            RunActiveController(); ExecuteSelectedControllerAction("start-line");
            for (var i = 0; i < 2500; i++) _PhysicsProcess(.02);
            var points = _sceneRuntime!.Points;
            var retained = (MeshInstance3D)_sceneCompositionRoot!.GetNode<Node3D>("finishing_tote")
                .FindChild("FINISHING_applied_label", true, false);
            GD.Print($"TOTE_FULL_REFERENCE position={points["tote_position"]} fill={points["fill_complete"]} cap={points["cap_applied"]} label={points["label_applied"]} inspection={points["inspection_ok"]}");
            check(points["fill_complete"] is true && points["cap_applied"] is true && points["label_applied"] is true
                && points["label_home"] is true && points["inspection_done"] is true && points["inspection_ok"] is true
                && points["tote_at_exit"] is true && retained.Visible,
                "compiled_plc_reference_fills_caps_labels_inspects_and_retains_exit");
            check(new[] { "conveyor_run", "fill_valve_open", "capper_run", "labeler_run", "inspection_run" }
                .All(name => points[name] is false), "full_reference_removes_all_actuator_commands_at_exit");
            StopActiveController();
            check(retained.Visible && _sceneRuntime.Points["inspection_ok"] is true, "controller_stop_retains_mark_and_quality_result");
            ResetActiveController();
            check(!retained.Visible && _sceneRuntime.Points["inspection_ok"] is false
                && _sceneRuntime.Points["label_applied"] is false, "controller_reset_removes_mark_and_result");
        }
        finally { DisableVirtualController(); }
    }
}
