using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    // Mirrors SceneSimulationRuntime.ApplyBindings' effective command image;
    // these are commands, not measured motor shaft feedback.
    private static string MotorReviewCommandBits(IReadOnlyDictionary<string, object?>? points) =>
        string.Concat(Enumerable.Range(0, 10).Select(i => points is not null
            && (points.GetValueOrDefault("motor_array_run") is true
                || points.GetValueOrDefault($"motor_{i}_run") is true
                || points.GetValueOrDefault($"motor_commands[{i}]") is true) ? "1" : "0"));

    private VBoxContainer? _gantryReviewClockBar;
    private CheckButton? _gantryReviewHold;
    private Button? _gantryReviewStep;
    private Button? _gantryReviewFineStep;
    private SpinBox? _gantryReviewTickCount;
    private Button? _gantryReviewTickStep;
    private Label? _gantryReviewClockLabel;
    private CheckButton? _receiverReviewCutaway;
    private Node3D? _gantryReviewHeldRoot;
    private SceneSimulationRuntime? _gantryReviewHeldRuntime;
    private ProcessModeEnum _gantryReviewRootMode;
    private ProcessModeEnum _gantryReviewRuntimeMode;
    private bool _gantryReviewStepping;
    private bool _gantryReviewOperatorView = true;

    // Scoped to reviewed offline clock implementations: the
    // single-clock palletizer / conveyor-pusher / chain lift / cookie / barrel / cable / repeat-cycle plants. Other scenes may
    // have separate callbacks; this is not a general external PLC step.
    private bool CanReviewGantryClock => _visualSceneReview && !_visualPlantReview
        && _currentSceneId is "scene-1-conveyor-stop" or "scene-2-conveyor-pusher" or "lab-11-13-xy-palletizing" or "lab-4-09-chain-drive-lift" or "lab-4-10-cookie-packaging" or "lab-4-11-barrel-fill-station" or "lab-4-12-cable-cut-length" or "lab-4-03-repeat-cycle-counter" or "lab-4-07-parking-garage-entry" or "lab-4-08-package-grouping" or "lab-5-01-delayed-lamp" or "lab-5-02-timed-lamp-off" or "lab-5-03-rotary-flasher" or "lab-5-04-alternating-lamps" or "lab-5-05-variable-flash-rate" or "lab-5-06-running-light-tower" or "lab-5-07-pedestrian-crossing" or "lab-5-08-drawbridge-control" or "lab-5-09-bag-indexing-conveyor" or "lab-5-10-coating-line" or "lab-6-08-hand-dryer" or "lab-6-07-luggage-weight-sort" or "lab-11-11-service-elevator" or "lab-2-14-sump-pump" or "lab-2-18-pallet-pickup" or "lab-2-21-tote-finishing" or "lab-11-12-mobile-traffic-lights" or "lab-2-17-pallet-robot" or "lab-10-06-ten-motor-array-startup" or "lab-11-07-multi-conveyor-pallet-route" or "lab-2-22-dual-spindle" or "tank-high-low" or "tank-level" or "lab-11-06-wastewater-collection"
        && _simulatorShell?.IsExternalMode != true;

    private bool GantryReviewClockHeld => _gantryReviewHeldRoot is not null
        && ReferenceEquals(_gantryReviewHeldRoot, _sceneCompositionRoot);

    private void AddGantryReviewClockControls(CanvasLayer layer)
    {
        _gantryReviewClockBar = new VBoxContainer { Position = new Vector2(310, 162) };
        _gantryReviewClockBar.AddThemeFontSizeOverride("font_size", 13);
        layer.AddChild(_gantryReviewClockBar);
        _gantryReviewHold = new CheckButton { Text = "Hold offline plant clock" };
        _gantryReviewStep = new Button { Text = "Step 0.5 s", Disabled = true };
        _gantryReviewFineStep = new Button { Text = "Step 20 ms", Disabled = true, Visible = false };
        // Bounded review convenience: execute the same accepted 20 ms scan
        // route repeatedly. This never sets plant state or bypasses PLC logic.
        _gantryReviewTickCount = new SpinBox
        {
            MinValue = 1, MaxValue = 3000, Step = 1, Value = 25,
            Editable = false, CustomMinimumSize = new Vector2(80, 0),
            TooltipText = "Number of ordinary offline 20 ms scans to execute while held.",
        };
        _gantryReviewTickStep = new Button { Text = "Step ticks", Disabled = true };
        var tickCountLabel = new Label { Text = "QA ticks (20 ms)" };
        tickCountLabel.AddThemeFontSizeOverride("font_size", 13);
        _gantryReviewClockLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        // Diagnostic visibility only: retain bearing rollers, trays, loads,
        // collision shapes and all controller/runtime processing.
        _receiverReviewCutaway = new CheckButton
        {
            Text = "QA receiver cutaway (visual only)",
            Position = new Vector2(310, 198), Visible = false,
        };
        layer.AddChild(_receiverReviewCutaway);
        _receiverReviewCutaway.Toggled += cutaway =>
        {
            if (_currentSceneId != "lab-2-17-pallet-robot"
                || _sceneCompositionRoot?.GetNodeOrNull<Node3D>("process_receiver") is not { } receiver) return;
            // ReviewMeshes excludes invisible parts, so enumerate all meshes
            // to ensure switching off also restores the hidden obstructions.
            foreach (var mesh in receiver.FindChildren("*", "", true, false).OfType<MeshInstance3D>())
            {
                var name = mesh.Name.ToString();
                if (!(name.StartsWith("GUIDE_", StringComparison.Ordinal)
                    || name.StartsWith("FRONT_STOP_", StringComparison.Ordinal)
                    || name.StartsWith("DOCK_FUNNEL_", StringComparison.Ordinal)
                    || name.StartsWith("BACKSTOP", StringComparison.Ordinal)
                    || name is "RECEIVER_NAMEPLATE" or "RECEIVER_NAME")) continue;
                if (cutaway) mesh.SetMeta("qa_cutaway_original_visible", mesh.Visible);
                mesh.Visible = !cutaway && (!mesh.HasMeta("qa_cutaway_original_visible")
                    || mesh.GetMeta("qa_cutaway_original_visible").AsBool());
            }
            GD.Print($"VISUAL_REVIEW_RECEIVER_CUTAWAY enabled={cutaway} visual-only");
        };
        var clockButtons = new HFlowContainer();
        _gantryReviewClockBar.AddChild(clockButtons);
        clockButtons.AddChild(_gantryReviewHold);
        clockButtons.AddChild(_gantryReviewStep);
        clockButtons.AddChild(_gantryReviewFineStep);
        clockButtons.AddChild(tickCountLabel);
        clockButtons.AddChild(_gantryReviewTickCount);
        clockButtons.AddChild(_gantryReviewTickStep);
        _gantryReviewClockBar.AddChild(_gantryReviewClockLabel);
        foreach (var control in new Control[] { _gantryReviewHold, _gantryReviewStep, _gantryReviewFineStep, _gantryReviewTickCount, _gantryReviewTickStep, _gantryReviewClockLabel })
            control.AddThemeFontSizeOverride("font_size", 13);
        _gantryReviewClockBar.Resized += UpdateReviewControlLayout;
        _gantryReviewHold.Toggled += SetGantryReviewClockHeld;
        _gantryReviewStep.Pressed += StepGantryReviewClock;
        _gantryReviewFineStep.Pressed += () => AdvanceGantryReviewClock(1);
        _gantryReviewTickStep.Pressed += () =>
        {
            // Commit typed text before reading Value, including a click made
            // directly from the SpinBox's editor without pressing Enter.
            _gantryReviewTickCount.Apply();
            AdvanceGantryReviewClock((int)_gantryReviewTickCount.Value);
        };
        _simulatorShell!.ProductViewChanged += view =>
        {
            _gantryReviewOperatorView = view == "operator";
            if (!_gantryReviewOperatorView) ReleaseGantryReviewClock();
            RefreshGantryReviewClockControls();
        };
        RefreshGantryReviewClockControls();
    }

    private void UpdateReviewControlLayout()
    {
        if (_visualReviewBar is null) return;
        var aperture = _simulatorShell?.SceneViewportRect() ?? GetViewport().GetVisibleRect();
        if (aperture.Size.X <= 0) return;
        // Standalone preview retains its x18..378 runtime HUD. The shell's
        // operator aperture already excludes inspector/sidebar space.
        var left = _visualPlantReview ? Math.Max(400, aperture.Position.X + 12) : aperture.Position.X + 12;
        var availableWidth = aperture.End.X - left - 12;
        if (availableWidth <= 0) return;
        var width = availableWidth;
        _visualReviewBar.Position = new Vector2(left, _visualPlantReview ? 18 : 126);
        _visualReviewBar.Size = new Vector2(width, _visualReviewBar.Size.Y);
        if (_gantryReviewClockBar is null) return;
        _gantryReviewClockBar.Position = new Vector2(left, _visualReviewBar.Position.Y + _visualReviewBar.Size.Y + 6);
        _gantryReviewClockBar.Size = new Vector2(width, _gantryReviewClockBar.Size.Y);
        if (_receiverReviewCutaway is not null)
            _receiverReviewCutaway.Position = new Vector2(left, _gantryReviewClockBar.Position.Y + _gantryReviewClockBar.Size.Y + 6);
    }

    private void RefreshGantryReviewClockControls()
    {
        if (_receiverReviewCutaway is not null)
        {
            _receiverReviewCutaway.Visible = CanReviewGantryClock && _gantryReviewOperatorView
                && _currentSceneId == "lab-2-17-pallet-robot";
            if (_currentSceneId != "lab-2-17-pallet-robot") _receiverReviewCutaway.SetPressedNoSignal(false);
        }
        if (!CanReviewGantryClock) ReleaseGantryReviewClock();
        if (_gantryReviewClockBar is not null)
            _gantryReviewClockBar.Visible = CanReviewGantryClock && _gantryReviewOperatorView;
        if (_gantryReviewStep is not null)
            _gantryReviewStep.Text = _currentSceneId == "scene-2-conveyor-pusher" ? "Step 0.1 s" : _currentSceneId is "lab-4-09-chain-drive-lift" or "lab-4-10-cookie-packaging" or "lab-4-11-barrel-fill-station" or "lab-2-17-pallet-robot" ? "Step 2.0 s" : "Step 0.5 s";
        if (_gantryReviewFineStep is not null) _gantryReviewFineStep.Visible = _currentSceneId is "scene-1-conveyor-stop" or "scene-2-conveyor-pusher" or "lab-4-03-repeat-cycle-counter" or "lab-4-07-parking-garage-entry" or "lab-4-08-package-grouping" or "lab-5-01-delayed-lamp" or "lab-5-02-timed-lamp-off" or "lab-5-03-rotary-flasher" or "lab-5-04-alternating-lamps" or "lab-5-05-variable-flash-rate" or "lab-5-06-running-light-tower" or "lab-5-07-pedestrian-crossing" or "lab-5-08-drawbridge-control" or "lab-5-09-bag-indexing-conveyor" or "lab-5-10-coating-line" or "lab-6-08-hand-dryer" or "lab-6-07-luggage-weight-sort" or "lab-11-11-service-elevator" or "lab-2-14-sump-pump" or "lab-2-18-pallet-pickup" or "lab-2-21-tote-finishing" or "lab-11-12-mobile-traffic-lights" or "lab-2-17-pallet-robot" or "lab-10-06-ten-motor-array-startup" or "lab-11-07-multi-conveyor-pallet-route" or "lab-2-22-dual-spindle" or "tank-high-low" or "tank-level" or "lab-11-06-wastewater-collection";
        UpdateGantryReviewClockLabel();
    }

    private void SetGantryReviewClockHeld(bool held)
    {
        if (!held) { ReleaseGantryReviewClock(); return; }
        if (!CanReviewGantryClock || _sceneCompositionRoot is null || _sceneRuntime is null)
        {
            _gantryReviewHold?.SetPressedNoSignal(false);
            return;
        }
        if (GantryReviewClockHeld) return;
        _gantryReviewHeldRoot = _sceneCompositionRoot;
        _gantryReviewHeldRuntime = _sceneRuntime;
        _gantryReviewRootMode = _sceneCompositionRoot.ProcessMode;
        _gantryReviewRuntimeMode = _sceneRuntime.ProcessMode;
        _sceneCompositionRoot.ProcessMode = ProcessModeEnum.Disabled;
        _sceneRuntime.ProcessMode = ProcessModeEnum.Disabled;
        GD.Print("GANTRY_REVIEW_CLOCK held=True offline-only");
        UpdateGantryReviewClockLabel();
    }

    private void ReleaseGantryReviewClock()
    {
        if (_gantryReviewHeldRoot is not null && GodotObject.IsInstanceValid(_gantryReviewHeldRoot))
            _gantryReviewHeldRoot.ProcessMode = _gantryReviewRootMode;
        if (_gantryReviewHeldRuntime is not null && GodotObject.IsInstanceValid(_gantryReviewHeldRuntime))
            _gantryReviewHeldRuntime.ProcessMode = _gantryReviewRuntimeMode;
        _gantryReviewHeldRoot = null;
        _gantryReviewHeldRuntime = null;
        _gantryReviewHold?.SetPressedNoSignal(false);
        UpdateGantryReviewClockLabel();
    }

    private void StepGantryReviewClock() => AdvanceGantryReviewClock(
        _currentSceneId == "scene-2-conveyor-pusher" ? 5 :
        _currentSceneId is "scene-1-conveyor-stop" or "lab-4-12-cable-cut-length" or "lab-11-13-xy-palletizing" or "lab-4-03-repeat-cycle-counter" or "lab-4-07-parking-garage-entry" or "lab-4-08-package-grouping" or "lab-5-01-delayed-lamp" or "lab-5-02-timed-lamp-off" or "lab-5-03-rotary-flasher" or "lab-5-04-alternating-lamps" or "lab-5-05-variable-flash-rate" or "lab-5-06-running-light-tower" or "lab-5-07-pedestrian-crossing" or "lab-5-08-drawbridge-control" or "lab-5-09-bag-indexing-conveyor" or "lab-5-10-coating-line" or "lab-6-08-hand-dryer" or "lab-6-07-luggage-weight-sort" or "lab-11-11-service-elevator" or "lab-2-14-sump-pump" or "lab-2-18-pallet-pickup" or "lab-2-21-tote-finishing" or "lab-11-12-mobile-traffic-lights" or "lab-10-06-ten-motor-array-startup" or "lab-11-07-multi-conveyor-pallet-route" or "lab-2-22-dual-spindle" or "tank-high-low" or "tank-level" or "lab-11-06-wastewater-collection" ? 25 : 100);

    private void AdvanceGantryReviewClock(int ticks)
    {
        if (!CanReviewGantryClock || !GantryReviewClockHeld
            || _virtualController?.Snapshot.State != VirtualControllerState.Running) return;
        var chainLift = _currentSceneId == "lab-4-09-chain-drive-lift";
        var motion = chainLift ? _sceneCompositionRoot!.GetNode<Node3D>("liftTable_1").GetNode<EquipmentMotionController>("ChainLiftMotion") : null;
        _gantryReviewStepping = true;
        try
        {
            // Preserve the existing input -> ladder -> output -> plant path,
            // at the authored 20 ms scan cadence. The disabled parent prevents
            // Godot from also advancing autonomous equipment during inspection.
            for (var tick = 0; tick < ticks; tick++)
            {
                _PhysicsProcess(0.02);
                // These motors normally rotate in child physics callbacks.
                // Their parent is disabled while held, so explicitly advance
                // the visuals once after the accepted controller scan.
                if (_currentSceneId == "lab-10-06-ten-motor-array-startup")
                    foreach (var motor in _sceneCompositionRoot!.FindChildren("*", "", true, false)
                        .OfType<EquipmentMotionController>()) motor._PhysicsProcess(.02);
                if (_currentSceneId == "lab-11-07-multi-conveyor-pallet-route")
                    foreach (var receiver in _sceneCompositionRoot!.FindChildren("*", "", true, false)
                        .OfType<RollerConveyorController>()) receiver._PhysicsProcess(.02);
            }
        }
        finally { _gantryReviewStepping = false; }
        // Refit only explicit conveyor-transfer inspection targets after a
        // manual step. A fixed close camera otherwise loses the pallet as it
        // crosses the receiver. Preserve the selected angle and zoom mode.
        if (_currentSceneId == "lab-11-07-multi-conveyor-pallet-route"
            && _visualReviewFocusId is "route_pallet" or "training_accessory_6")
            SetVisualReviewAngle(_sceneCameraDirection, "stepped-transfer-focus");
        UpdateGantryReviewClockLabel();
        GD.Print($"GANTRY_REVIEW_STEP time={_virtualController.Snapshot.SimulatedTime.TotalSeconds:F2} scan={_virtualController.Snapshot.ScanNumber} position={(_currentSceneId == "scene-2-conveyor-pusher" ? Convert.ToDouble(_sceneRuntime?.Points.GetValueOrDefault("pusher_position") ?? 0) : motion?.PositionPercent ?? 0):F3}%");
    }

    private void UpdateGantryReviewClockLabel()
    {
        UpdateReviewControlLayout();
        if (_gantryReviewStep is not null)
            _gantryReviewStep.Disabled = !GantryReviewClockHeld
                || _virtualController?.Snapshot.State != VirtualControllerState.Running;
        if (_gantryReviewFineStep is not null) _gantryReviewFineStep.Disabled = !GantryReviewClockHeld
            || _virtualController?.Snapshot.State != VirtualControllerState.Running;
        var canStepTicks = CanReviewGantryClock && GantryReviewClockHeld
            && _virtualController?.Snapshot.State == VirtualControllerState.Running;
        if (_gantryReviewTickStep is not null) _gantryReviewTickStep.Disabled = !canStepTicks;
        if (_gantryReviewTickCount is not null) _gantryReviewTickCount.Editable = canStepTicks;
        if (_gantryReviewClockLabel is null) return;
        if (_currentSceneId == "lab-11-12-mobile-traffic-lights")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | A X {_sceneRuntime?.Points.GetValueOrDefault("road_a_position_m") ?? 0:0.00} m occ {_sceneRuntime?.Points.GetValueOrDefault("road_a_occupied") ?? false} | B X {_sceneRuntime?.Points.GetValueOrDefault("road_b_position_m") ?? 0:0.00} m occ {_sceneRuntime?.Points.GetValueOrDefault("road_b_occupied") ?? false} | clear {_sceneRuntime?.Points.GetValueOrDefault("crossing_clear") ?? false} | conflict {_sceneRuntime?.Points.GetValueOrDefault("signal_conflict") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-11-06-wastewater-collection")
        {
            var p = _sceneRuntime?.Points;
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | bank {p?.GetValueOrDefault("source_level_percent") ?? 0:0.000}% | {p?.GetValueOrDefault("source_level_ma") ?? 0:0.000} mA | high {p?.GetValueOrDefault("source_level_high") ?? false} | pump {p?.GetValueOrDefault("transfer_pump_run") ?? false} | valve command {p?.GetValueOrDefault("outlet_valve_open") ?? false} | actual {p?.GetValueOrDefault("outlet_valve_position_percent") ?? 0:0.0}%";
            return;
        }
        if (_currentSceneId == "lab-2-21-tote-finishing")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | tote X {_sceneRuntime?.Points.GetValueOrDefault("tote_position") ?? 0:0.00} | tamp {_sceneRuntime?.Points.GetValueOrDefault("label_extension_percent") ?? 0:0.0}% | busy {_sceneRuntime?.Points.GetValueOrDefault("label_busy") ?? false} | home {_sceneRuntime?.Points.GetValueOrDefault("label_home") ?? false} | applied {_sceneRuntime?.Points.GetValueOrDefault("label_applied") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-2-18-pallet-pickup")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | pallet {_sceneRuntime?.Points.GetValueOrDefault("pallet_position") ?? 0:0.00}% | conveyor {_sceneRuntime?.Points.GetValueOrDefault("conveyor_run") ?? false}";
            return;
        }
        if (_currentSceneId == "scene-1-conveyor-stop")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | position {_sceneRuntime?.Points.GetValueOrDefault("object_position") ?? 0:0.000} | eye {_sceneRuntime?.Points.GetValueOrDefault("simulated_photoeye") ?? false}";
            return;
        }
        if (_currentSceneId is "tank-high-low" or "tank-level")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | level {_sceneRuntime?.Points.GetValueOrDefault("tank_level") ?? 0:0.00}% | low {_sceneRuntime?.Points.GetValueOrDefault("low_level_switch") ?? false} | high {_sceneRuntime?.Points.GetValueOrDefault("high_level_switch") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-2-22-dual-spindle")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | A {_sceneRuntime?.Points.GetValueOrDefault("drill_a_position") ?? 0:0.0}% | B {_sceneRuntime?.Points.GetValueOrDefault("drill_b_position") ?? 0:0.0}% | slide {_sceneRuntime?.Points.GetValueOrDefault("transfer_position") ?? 0:0.0}%";
            return;
        }
        if (_currentSceneId == "lab-11-07-multi-conveyor-pallet-route")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | X {_sceneRuntime?.Points.GetValueOrDefault("route_position") ?? 0:0.00} m | complete {_sceneRuntime?.Points.GetValueOrDefault("route_complete") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-10-06-ten-motor-array-startup")
        {
            var commands = MotorReviewCommandBits(_sceneRuntime?.Points);
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | effective M0..9 {commands} (typed OR legacy OR group)";
            return;
        }
        if (_currentSceneId == "lab-2-17-pallet-robot")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | placed {_sceneRuntime?.Points.GetValueOrDefault("placed_count") ?? 0} | park {_sceneRuntime?.Points.GetValueOrDefault("robot_at_park") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-2-14-sump-pump")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | level {_sceneRuntime?.Points.GetValueOrDefault("sump_level") ?? 0:0.0}% | pump {_sceneRuntime?.Points.GetValueOrDefault("pump_run") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-11-11-service-elevator")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | car {_sceneRuntime?.Points.GetValueOrDefault("car_position_pct") ?? 0:0.0}% | lower {_sceneRuntime?.Points.GetValueOrDefault("at_lower_landing") ?? false} | upper {_sceneRuntime?.Points.GetValueOrDefault("at_upper_landing") ?? false}";
            return;
        }
        if (_currentSceneId == "scene-2-conveyor-pusher")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | stroke {_sceneRuntime?.Points.GetValueOrDefault("pusher_position") ?? 0:0.0}% | count {_sceneRuntime?.Points.GetValueOrDefault("parts_completed") ?? 0}";
            return;
        }
        if (_currentSceneId == "lab-6-07-luggage-weight-sort")
        {
            if (_sceneRuntime?.RuntimeType != "luggageWeightSort") return;
            var p=_sceneRuntime.Points;
            _gantryReviewClockLabel.Text=$"QA | {_virtualController?.Snapshot.SimulatedTime.TotalSeconds??0:F2}s | X {p["bag_x"]:0.00} Z {p["bag_z"]:0.00} | kg {p["weight_kg"]:0.00} | class {p["class_result"]} | counts {p["class_1_count"]}/{p["class_2_count"]}/{p["class_3_count"]}";
            return;
        }
        if (_currentSceneId == "lab-6-08-hand-dryer")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | remaining {_sceneRuntime?.Points.GetValueOrDefault("remaining_seconds") ?? 0:0.00} s | HANDS {_sceneRuntime?.Points.GetValueOrDefault("hands_present") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-10-coating-line")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | X {_sceneRuntime?.Points.GetValueOrDefault("workpiece_position") ?? 0:0.00} m | STATION {_sceneRuntime?.Points.GetValueOrDefault("workpiece_at_station") ?? false} | EXIT {_sceneRuntime?.Points.GetValueOrDefault("workpiece_at_exit") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-09-bag-indexing-conveyor")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | bag X {_sceneRuntime?.Points.GetValueOrDefault("bag_position") ?? 0:0.00} m | ENTRY {_sceneRuntime?.Points.GetValueOrDefault("bag_at_entry") ?? false} | EXIT {_sceneRuntime?.Points.GetValueOrDefault("bag_at_exit") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-08-drawbridge-control")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | deck {_sceneRuntime?.Points.GetValueOrDefault("bridge_angle") ?? 0:0.0} deg | gates {_sceneRuntime?.Points.GetValueOrDefault("barrier_angle") ?? 0:0.0} deg | HOME {_sceneRuntime?.Points.GetValueOrDefault("bridge_home") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-07-pedestrian-crossing")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | phase {_virtualController?.Snapshot.NumericVariables.GetValueOrDefault("crossing_phase") ?? 0} | PATH {_sceneRuntime?.Points.GetValueOrDefault("clear_to_finish") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-06-running-light-tower")
        {
            var color = RunningTowerColors.FirstOrDefault(name => _sceneRuntime?.Points.GetValueOrDefault("tower_" + name) is true) ?? "OFF";
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | tower {color}";
            return;
        }
        if (_currentSceneId == "lab-5-04-alternating-lamps")
        {
            var timers = _virtualController?.Snapshot.Timers;
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | A ET {timers?.GetValueOrDefault("a_timer")?.Accumulated.TotalSeconds ?? 0:F2} | B ET {timers?.GetValueOrDefault("b_timer")?.Accumulated.TotalSeconds ?? 0:F2} | A {_sceneRuntime?.Points.GetValueOrDefault("lamp_a") ?? false} | B {_sceneRuntime?.Points.GetValueOrDefault("lamp_b") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-05-variable-flash-rate")
        {
            var enabled = _sceneRuntime?.Points.GetValueOrDefault("flash_enable") is true;
            var fast = _sceneRuntime?.Points.GetValueOrDefault("fast_rate_selected") is true;
            var slow = _sceneRuntime?.Points.GetValueOrDefault("slow_rate_selected") is true;
            var mode = !enabled ? "DISABLED" : fast && slow ? "CONFLICT" : !fast && !slow ? "NO RATE" : fast ? "FAST" : "SLOW";
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | {mode} | lamp {_sceneRuntime?.Points.GetValueOrDefault("rate_lamp") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-5-03-rotary-flasher")
        {
            var timers = _virtualController?.Snapshot.Timers;
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | OFF ET {timers?.GetValueOrDefault("off_timer")?.Accumulated.TotalSeconds ?? 0:F2} | ON ET {timers?.GetValueOrDefault("on_timer")?.Accumulated.TotalSeconds ?? 0:F2} | lamp {_sceneRuntime?.Points.GetValueOrDefault("flash_lamp") ?? false}";
            return;
        }
        if (_currentSceneId is "lab-5-01-delayed-lamp" or "lab-5-02-timed-lamp-off")
        {
            var delayed = _currentSceneId == "lab-5-01-delayed-lamp";
            var timer = _virtualController?.Snapshot.Timers.GetValueOrDefault(delayed ? "delay_timer" : "pulse_timer");
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | ET {timer?.Accumulated.TotalSeconds ?? 0:F2} s | lamp {_sceneRuntime?.Points.GetValueOrDefault(delayed ? "delayed_lamp" : "timed_lamp") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-4-08-package-grouping")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | stop {_sceneRuntime?.Points.GetValueOrDefault("stop_position") ?? 0:0.0}% | count {_sceneRuntime?.Points.GetValueOrDefault("group_count") ?? 0} | done {_sceneRuntime?.Points.GetValueOrDefault("transfer_complete") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-4-07-parking-garage-entry")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | boom {_sceneRuntime?.Points.GetValueOrDefault("barrier_position") ?? 0:0.0}% | count {_sceneRuntime?.Points.GetValueOrDefault("occupancy_count") ?? 0} | {_sceneRuntime?.ParkingEntryPlant?.Phase}";
            return;
        }
        if (_currentSceneId == "lab-4-03-repeat-cycle-counter")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | head {_sceneRuntime?.Points.GetValueOrDefault("head_position") ?? 0:0.0}% | count {_sceneRuntime?.Points.GetValueOrDefault("batch_count") ?? 0} | done {_sceneRuntime?.Points.GetValueOrDefault("cycle_done") ?? false}";
            return;
        }
        var motion = _sceneCompositionRoot?.FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>().FirstOrDefault(item => item is ChainLiftDriveVisual || item.Kind == EquipmentMotionController.MotionKind.CartesianGantry);
        if (_currentSceneId == "lab-11-13-xy-palletizing")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | placed {_sceneRuntime?.Points.GetValueOrDefault("placed_cartons") ?? 0} | home {_sceneRuntime?.Points.GetValueOrDefault("gantry_home") ?? false}";
            return;
        }
        if (_currentSceneId == "lab-4-12-cable-cut-length")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | length {_sceneRuntime?.Points.GetValueOrDefault("measured_length_m") ?? 0:0.00} m | blade {_sceneRuntime?.Points.GetValueOrDefault("cutter_position") ?? 0:0.00}";
            return;
        }
        if (_currentSceneId == "lab-4-11-barrel-fill-station")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | barrel {_sceneRuntime?.Points.GetValueOrDefault("barrel_litres") ?? 0:0.0} L | source {_sceneRuntime?.Points.GetValueOrDefault("source_litres") ?? 0:0.0} L";
            return;
        }
        if (_currentSceneId == "lab-4-10-cookie-packaging")
        {
            _gantryReviewClockLabel.Text = $"QA | {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s | cookies {_sceneRuntime?.Points.GetValueOrDefault("cookie_count") ?? 0} | sealed {_sceneRuntime?.Points.GetValueOrDefault("wrapped_count") ?? 0}";
            return;
        }
        _gantryReviewClockLabel.Text = $"QA · {(_virtualController?.Snapshot.SimulatedTime.TotalSeconds ?? 0):F2} s · stroke {motion?.PositionPercent ?? 0:F1}%";
    }

    private void VerifyGantryReviewClock(Action<bool, string> check)
    {
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var rootMode = root.ProcessMode; var runtimeMode = runtime.ProcessMode;
        SetGantryReviewClockHeld(true); RunActiveController(); StepGantryReviewClock(); runtime.ExecuteAction("start-palletizer");
        var scans = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(5);
        check(GantryReviewClockHeld && _virtualController.Snapshot.ScanNumber == scans, "review_hold_freezes_single_controller_and_plant_clock");
        StepGantryReviewClock();
        check(_virtualController.Snapshot.ScanNumber == scans + 25 && Math.Abs(_virtualController.Snapshot.SimulatedTime.TotalSeconds - 1.0) < 1e-6 && runtime.Points["gantry_home"] is false, "review_step_runs_twenty_five_actual_scans_and_position_feedback");
        StopActiveController(); scans = _virtualController.Snapshot.ScanNumber; StepGantryReviewClock();
        check(_virtualController.Snapshot.ScanNumber == scans, "review_step_cannot_advance_stopped_controller");
        ResetActiveController();
        check(GantryReviewClockHeld && runtime.Points["gantry_home"] is true && _virtualController.Snapshot.ScanNumber == 0, "review_reset_restores_actual_home_while_held");
        ReleaseGantryReviewClock();
        check(root.ProcessMode == rootMode && runtime.ProcessMode == runtimeMode, "review_release_restores_native_process_modes");
    }
    private void VerifyBaseConveyorReviewClock(Action<bool, string> check)
    {
        AddMigratedScene("scene-1-conveyor-stop", _candidateCatalog!, _mainCamera!, false, false);
        var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/demos/03-conveyor-sequence.rpproj.json"));
        if (!loaded.IsReadable || loaded.Document is null) throw new InvalidOperationException("Demo 3 review program cannot be read.");
        EnableVirtualControllerProgram(loaded.Document.BuildProgram());
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var carton = root.GetNode<Node3D>("scene1_product");
        var staged = carton.Transform;
        var deck = FlatConveyorCarryingSurface((MeshInstance3D)root.GetNode<Node3D>("scene1_conveyor").FindChild("KIN_belt_surface", true, false));
        var load = ReviewBounds(carton);
        GD.Print($"BASE_CONVEYOR_FLAT_SUPPORT carton={load} carrying={deck}");
        check(MathF.Abs(load.Position.Y - deck.End.Y) < .001f
            && load.Position.X >= deck.Position.X && load.End.X <= deck.End.X
            && load.Position.Z >= deck.Position.Z && load.End.Z <= deck.End.Z,
            "base_conveyor_native_mesh_initial_full_flat_deck_support");
        var rootMode = root.ProcessMode;
        var runtimeMode = runtime.ProcessMode;
        SetGantryReviewClockHeld(true);
        RunActiveController();
        _PhysicsProcess(5);
        check(GantryReviewClockHeld && _virtualController!.Snapshot.ScanNumber == 0 && carton.Transform == staged,
            "base_conveyor_review_hold_freezes_actual_demo_and_carton");
        StepGantryReviewClock();
        check(_virtualController!.Snapshot.ScanNumber == 25 && carton.Transform == staged,
            "base_conveyor_review_half_second_step_preserves_run_without_start");
        check(ExecuteSelectedControllerAction("scene-toggle"), "base_conveyor_review_operator_start_accepted");
        AdvanceGantryReviewClock(1);
        check(_virtualController.Snapshot.ScanNumber == 26
            && Convert.ToDouble(runtime.Points["object_position"]) is > 0 and < .5
            && runtime.Points["conveyor_running"] is true && carton.Transform != staged,
            "base_conveyor_review_fine_step_exposes_real_mid_travel_pose");
        StopActiveController();
        var stopped = carton.Transform;
        var scans = _virtualController.Snapshot.ScanNumber;
        StepGantryReviewClock();
        AdvanceGantryReviewClock(1);
        check(_virtualController.Snapshot.ScanNumber == scans && carton.Transform == stopped
            && runtime.Points["conveyor_running"] is false,
            "base_conveyor_review_stop_retains_mid_travel_and_blocks_steps");
        ResetActiveController();
        check(GantryReviewClockHeld && _virtualController.Snapshot.ScanNumber == 0 && carton.Transform == staged
            && runtime.Points["simulated_photoeye"] is false,
            "base_conveyor_review_reset_restores_load_end_while_held");
        ReleaseGantryReviewClock();
        check(root.ProcessMode == rootMode && runtime.ProcessMode == runtimeMode,
            "base_conveyor_review_release_restores_process_modes");
    }

    private void VerifyConveyorReviewClock(Action<bool, string> check)
    {
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var rootMode = root.ProcessMode; var runtimeMode = runtime.ProcessMode;
        var carton = root.GetNode<Node3D>("scene2_product");
        ResetActiveController(); var staged = carton.Transform;
        SetGantryReviewClockHeld(true); RunActiveController();
        _PhysicsProcess(5);
        check(GantryReviewClockHeld && _virtualController!.Snapshot.ScanNumber == 0 && carton.Transform == staged,
            "conveyor_review_hold_freezes_controller_and_plant");
        StepGantryReviewClock();
        check(_virtualController!.Snapshot.ScanNumber == 5 && Math.Abs(_virtualController.Snapshot.SimulatedTime.TotalSeconds - .1) < 1e-6,
            "conveyor_review_coarse_step_executes_five_twenty_ms_scans");
        for (var step = 1; step < 10; step++) StepGantryReviewClock();
        AdvanceGantryReviewClock(1);
        check(_virtualController.Snapshot.ScanNumber == 51 && runtime.Points["pusher_extend"] is true
            && Convert.ToDouble(runtime.Points["pusher_position"]) is > 0 and < 10,
            "conveyor_review_fine_step_executes_one_scan_and_starts_actual_stroke");
        var visibleAtRelease = false; var ticks = 0;
        while (runtime.Points["pusher_extended"] is not true && ticks++ < 30)
        {
            AdvanceGantryReviewClock(1);
            if (Convert.ToInt64(runtime.Points["parts_completed"]) == 1) visibleAtRelease |= carton.Visible;
        }
        check(ticks < 30 && visibleAtRelease && Convert.ToInt64(runtime.Points["parts_completed"]) == 1,
            "conveyor_review_fine_steps_cross_transfer_and_full_stroke_without_disappearance");
        var received = carton.Transform;
        ticks = 0;
        while (runtime.Points["pusher_retracted"] is not true && ticks++ < 30) AdvanceGantryReviewClock(1);
        check(ticks < 30 && carton.Visible && carton.Transform.IsEqualApprox(received),
            "conveyor_review_retraction_retains_received_carton");
        StopActiveController(); var scans = _virtualController.Snapshot.ScanNumber;
        StepGantryReviewClock(); AdvanceGantryReviewClock(1);
        check(_virtualController.Snapshot.ScanNumber == scans && runtime.Points["pusher_extend"] is false && runtime.Points["conveyor_running"] is false && carton.Transform == received,
            "conveyor_review_stopped_coarse_and_fine_steps_cannot_advance");
        ResetActiveController();
        check(GantryReviewClockHeld && _virtualController.Snapshot.ScanNumber == 0 && runtime.Points["pusher_retracted"] is true
            && Convert.ToInt64(runtime.Points["parts_completed"]) == 0 && carton.Visible && carton.Transform == staged,
            "conveyor_review_reset_restores_staged_carton_and_zero_count_while_held");
        ReleaseGantryReviewClock();
        check(root.ProcessMode == rootMode && runtime.ProcessMode == runtimeMode,
            "conveyor_review_release_restores_original_process_modes");
    }

}
