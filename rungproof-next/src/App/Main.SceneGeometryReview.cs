using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifySceneGeometry;
    private bool _reportSceneGeometry;
    private bool _visualSceneReview;
    private bool _visualPlantReview;
    private Label? _visualReviewLabel;
    private bool _visualReviewClose;
    private OptionButton? _visualReviewFocus;
    private string? _visualReviewFocusId;
    private Vector3? _visualReviewFollowCenter;
    private HFlowContainer? _visualReviewBar;

    // This opt-in inspection bar uses the real shell, scene composer, meshes
    // and camera. Only standalone plant QA exposes explicit scene actions;
    // the controller-owned shell keeps its normal action ownership.
    private void AddVisualSceneReviewControls()
    {
        var layer = new CanvasLayer { Name = "VisualSceneReview", Layer = 20 };
        AddChild(layer);
        var bar = new HFlowContainer { Position = _visualPlantReview ? new Vector2(400, 18) : new Vector2(310, 126) };
        bar.AddThemeFontSizeOverride("font_size", 13);
        _visualReviewBar = bar;
        layer.AddChild(bar);
        if (_simulatorShell is not null)
            _simulatorShell.ProductViewChanged += view => bar.Visible = view == "operator";
        void Button(string text, Action action)
        {
            var button = new Godot.Button { Text = text };
            button.AddThemeFontSizeOverride("font_size", 13);
            button.Pressed += action;
            bar.AddChild(button);
        }
        if (!_visualPlantReview)
        {
            Button("Previous", () => ChangeVisualReviewScene(-1));
            Button("Next", () => ChangeVisualReviewScene(1));
        }
        else bar.AddChild(new Label { Text = "PLANT PREVIEW · NO PLC CONTROLLER" });
        Button("Front right", () => SetVisualReviewAngle(new Vector3(11, 7, 12), "front-right"));
        Button("Front left", () => SetVisualReviewAngle(new Vector3(-11, 7, 12), "front-left"));
        Button("Rear left", () => SetVisualReviewAngle(new Vector3(-11, 7, -12), "rear-left"));
        Button("Rear right", () => SetVisualReviewAngle(new Vector3(11, 7, -12), "rear-right"));
        // Keep the overhead azimuth aligned with the equipment row. Equal X/Z
        // offsets rotated a straight row 45 degrees into the review toolbar.
        Button("Top", () => SetVisualReviewAngle(new Vector3(0, 20, 0.01f), "top"));
        Button("Wide / close", () =>
        {
            _visualReviewClose = !_visualReviewClose;
            SetVisualReviewAngle(_sceneCameraDirection, _visualReviewClose ? "close-up" : "wide");
        });
        _visualReviewLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(140, 0) };
        _visualReviewLabel.AddThemeFontSizeOverride("font_size", 13);
        bar.AddChild(_visualReviewLabel);
        _visualReviewFocus = new OptionButton { CustomMinimumSize = new Vector2(180, 0), FitToLongestItem = false };
        _visualReviewFocus.AddThemeFontSizeOverride("font_size", 13);
        _visualReviewFocus.ItemSelected += index =>
        {
            _visualReviewFocusId = index == 0 ? null : _visualReviewFocus.GetItemText((int)index);
            _visualReviewClose = false;
            SetVisualReviewAngle(_sceneCameraDirection, $"focus-{_visualReviewFocusId ?? "scene"}");
        };
        bar.AddChild(_visualReviewFocus);
        UpdateVisualReviewLabel();
        if (!_visualPlantReview) AddGantryReviewClockControls(layer);
        GetViewport().SizeChanged += UpdateReviewControlLayout;
        bar.Resized += UpdateReviewControlLayout;
        UpdateReviewControlLayout();
        if (_visualPlantReview && _sceneRuntime is not null)
        {
            // Standalone QA must expose both directions/actions, not only the
            // default sequence. Keep this out of the controller-owned shell.
            var runtime = _sceneRuntime;
            var actions = runtime.GetActions();
            // Point values can wrap beyond the original 230 px HUD. Anchor QA
            // actions to the viewport bottom so they never cover Run/Stop/Reset.
            var actionBar = new HBoxContainer
            {
                AnchorTop = 1, AnchorBottom = 1,
                OffsetLeft = 18, OffsetTop = -50, OffsetBottom = -18,
                GrowVertical = Control.GrowDirection.Begin,
            };
            layer.AddChild(actionBar);
            var choice = new OptionButton { CustomMinimumSize = new Vector2(280, 0) };
            foreach (var action in actions) choice.AddItem(action.Label);
            actionBar.AddChild(choice);
            var execute = new Godot.Button { Text = "Preview action", Disabled = actions.Count == 0 };
            execute.Pressed += () => runtime.ExecuteAction(actions[choice.Selected].Id);
            actionBar.AddChild(execute);
            var composition = _sceneCompositionRoot!;
            var runtimeMode = runtime.ProcessMode;
            var compositionMode = composition.ProcessMode;
            var hold = new CheckButton { Text = "Hold preview clock" };
            var step = new Godot.Button { Text = "Step 0.5 s", Disabled = true };
            hold.Toggled += held =>
            {
                // Hold the actual runtime and equipment processing, while the
                // QA camera/UI remain usable. Output ownership is unchanged.
                runtime.ProcessMode = held ? ProcessModeEnum.Disabled : runtimeMode;
                composition.ProcessMode = held ? ProcessModeEnum.Disabled : compositionMode;
                step.Disabled = !held;
                GD.Print($"VISUAL_REVIEW_CLOCK held={held}");
            };
            step.Pressed += () =>
            {
                runtime.AdvanceSimulation(0.5);
                GD.Print("VISUAL_REVIEW_STEP seconds=0.5");
            };
            actionBar.AddChild(hold);
            actionBar.AddChild(step);
            if (_currentSceneId == "lab-2-18-pallet-pickup")
            {
                // The normal elevated views cannot show the strap return
                // inside the fork opening. This camera-only QA view leaves
                // the equipment and controller image untouched.
                var underside = new Godot.Button { Text = "Pallet underside" };
                underside.Pressed += () =>
                {
                    var pallet = composition.GetNode<Node3D>("shipping_pallet");
                    var bounds = ReviewBounds(pallet);
                    var target = bounds.GetCenter();
                    target.Y = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_STRINGER_", StringComparison.Ordinal))
                        .Min(mesh => ReviewBounds(mesh).Position.Y) - 0.025f;
                    _mainCamera!.Position = target + new Vector3(1.6f, 0, 1.6f);
                    _mainCamera.LookAt(target, Vector3.Up);
                    _cameraController?.CaptureCurrentView(target);
                    GD.Print($"VISUAL_REVIEW_ANGLE {_currentSceneId} pallet-underside");
                };
                actionBar.AddChild(underside);
            }
        }
    }

    private void ChangeVisualReviewScene(int step)
    {
        if (_sceneCatalog is null || _candidateCatalog is null || _mainCamera is null) return;
        var scenes = _sceneCatalog.Scenes;
        var index = scenes.ToList().FindIndex(scene => scene.Id == _currentSceneId);
        var next = scenes[(index + step + scenes.Count) % scenes.Count];
        // Use the same request path as the browser, including workspace and
        // ladder replacement guards, rather than bypassing them in QA mode.
        _simulatorShell?.RequestSceneChange(next.Id);
    }

    private void UpdateVisualReviewLabel()
    {
        if (_sceneCatalog is null || _visualReviewLabel is null) return;
        var index = _sceneCatalog.Scenes.ToList().FindIndex(scene => scene.Id == _currentSceneId);
        _visualReviewLabel.Text = $"{index + 1}/{_sceneCatalog.Scenes.Count}";
        _visualReviewFocusId = null;
        _visualReviewFollowCenter = null;
        _visualReviewFocus?.Clear();
        _visualReviewFocus?.AddItem("Full scene");
        if (_sceneCompositionRoot is not null)
            foreach (var node in _sceneCompositionRoot.GetChildren().OfType<Node3D>().Where(node => ReviewMeshes(node).Length > 0))
                _visualReviewFocus?.AddItem(node.Name);
        GD.Print($"VISUAL_REVIEW_SCENE {_currentSceneId} index={index + 1}");
        if (_sceneCompositionRoot is not null) LogBoundsCandidates(_sceneCompositionRoot);
        RefreshGantryReviewClockControls();
    }

    private void ReframeResetReviewFocus()
    {
        // Reset can teleport a moving load back to its infeed. Keep the
        // selected diagnostic focus visible without changing angle or zoom.
        if (_visualSceneReview && _visualReviewFocusId is not null)
            SetVisualReviewAngle(_sceneCameraDirection, "reset-focus");
    }

    private void SetVisualReviewAngle(Vector3 direction, string label)
    {
        if (_mainCamera is null || _sceneCompositionRoot is null) return;
        var focus = _visualReviewFocusId is null ? _sceneCompositionRoot
            : _sceneCompositionRoot.GetNodeOrNull<Node3D>(_visualReviewFocusId) ?? _sceneCompositionRoot;
        // The receiver-only bounds exclude the incoming load. Include its
        // current pose so an overhead transfer inspection cannot clip it.
        var companion = _currentSceneId == "lab-11-07-multi-conveyor-pallet-route"
            && _visualReviewFocusId == "training_accessory_6"
            ? _sceneCompositionRoot.GetNodeOrNull<Node3D>("route_pallet") : null;
        FrameComposition(_mainCamera, focus, direction, companion);
        if (_visualReviewClose && _cameraController is not null)
        {
            var target = _cameraController.ViewTarget;
            _cameraController.FocusOn(target, _mainCamera.Position.DistanceTo(target) * 0.58f / 2.6f);
        }
        _sceneCameraDirection = direction;
        _visualReviewFollowCenter = IsMovingVisualReviewFocus() ? ReviewBounds(focus).GetCenter() : null;
        GD.Print($"VISUAL_REVIEW_ANGLE {_currentSceneId} {label}");
    }

    private bool IsMovingVisualReviewFocus() =>
        (_currentSceneId == "lab-10-03-vision-package-sorter" && _visualReviewFocusId == "box_1")
        || (_currentSceneId == "lab-2-21-tote-finishing" && _visualReviewFocusId == "finishing_tote");

    private void FollowVisualReviewCarton()
    {
        if (!_visualSceneReview || !IsMovingVisualReviewFocus() || _visualReviewFocusId is null
            || _mainCamera is null || _cameraController is null
            || _sceneCompositionRoot?.GetNodeOrNull<Node3D>(_visualReviewFocusId) is not { } carton)
        {
            _visualReviewFollowCenter = null;
            return;
        }
        var center = ReviewBounds(carton).GetCenter();
        if (_visualReviewFollowCenter is { } previous)
        {
            // Translate the existing view: preserve the user's angle, pan and
            // zoom while following the rendered load, including table rotation.
            var travel = center - previous;
            if (travel.LengthSquared() > 0.0000000001f)
            {
                var target = _cameraController.ViewTarget + travel;
                _mainCamera.Position += travel;
                _cameraController.CaptureCurrentView(target);
            }
        }
        _visualReviewFollowCenter = center;
    }

    private static MeshInstance3D[] ReviewMeshes(Node root) =>
        root.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(mesh => mesh.Mesh is not null && mesh.IsVisibleInTree()).ToArray();

    private static Aabb ReviewBounds(MeshInstance3D mesh)
    {
        var local = mesh.GetAabb();
        var minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (var corner = 0; corner < 8; corner++)
        {
            var point = mesh.GlobalTransform * (local.Position + local.Size * new Vector3(
                (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1));
            minimum = new Vector3(MathF.Min(minimum.X, point.X), MathF.Min(minimum.Y, point.Y), MathF.Min(minimum.Z, point.Z));
            maximum = new Vector3(MathF.Max(maximum.X, point.X), MathF.Max(maximum.Y, point.Y), MathF.Max(maximum.Z, point.Z));
        }
        return new Aabb(minimum, maximum - minimum);
    }

    private static Aabb ReviewBounds(Node root) => root is MeshInstance3D mesh
        ? ReviewBounds(mesh)
        : ReviewMeshes(root).Select(ReviewBounds).Aggregate((a, b) => a.Merge(b));

    // Broad-phase candidates guide visual inspection; overlapping bounds on
    // curved/rotated meshes are not proof of solid interference. Intentional
    // machine connections can also intersect and require interpretation.
    private int LogBoundsCandidates(Node3D root)
    {
        var total = 0;
        var equipment = root.GetChildren().OfType<Node3D>()
            .Select(node => (Node: node, Meshes: ReviewMeshes(node)
                .Select(mesh => (Name: mesh.Name.ToString(), Bounds: ReviewBounds(mesh))).ToArray())).ToArray();
        for (var a = 0; a < equipment.Length; a++)
        for (var b = a + 1; b < equipment.Length; b++)
        {
            var candidates = 0;
            var example = string.Empty;
            foreach (var left in equipment[a].Meshes)
            foreach (var right in equipment[b].Meshes)
            {
                var overlap = left.Bounds.Intersection(right.Bounds).Size;
                if (overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f) continue;
                candidates++;
                if (example.Length == 0) example = $"{left.Name}/{right.Name}";
            }
            if (candidates > 0)
            {
                total += candidates;
                GD.Print($"BOUNDS_CANDIDATE {_currentSceneId} {equipment[a].Node.Name}/{equipment[b].Node.Name} count={candidates} example={example}");
            }
        }
        return total;
    }

    // This inventory only identifies candidates for a human camera review.
    // A successful report means the inventory ran, not that scenes are clear.
    private async void ReportSceneGeometry()
    {
        try
        {
            foreach (var scene in _sceneCatalog!.Scenes)
            {
                AddMigratedScene(scene.Id, _candidateCatalog!, _mainCamera!, false, false);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var root = _sceneCompositionRoot!;
                foreach (var equipment in root.GetChildren().OfType<Node3D>())
                {
                    if (ReviewMeshes(equipment).Length > 0)
                        GD.Print($"GEOMETRY_EQUIPMENT {scene.Id} {equipment.Name} {ReviewBounds(equipment)}");
                }
                GD.Print($"GEOMETRY_INVENTORY {scene.Id} candidates={LogBoundsCandidates(root)}");
            }
            GD.Print($"GEOMETRY_INVENTORY_COMPLETE scenes={_sceneCatalog.Scenes.Count} visual acceptance pending");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GEOMETRY_INVENTORY_ERROR {exception}");
            GetTree().Quit(1);
        }
    }

    private void VerifySceneGeometry()
    {
        try
        {
            var passed = true;
            void Check(bool condition, string label)
            { passed &= condition; GD.Print($"SCENE_GEOMETRY_CHECK {label}={condition}"); }
            if (OS.GetCmdlineUserArgs().Contains("--verify-service-elevator-installation", StringComparer.Ordinal))
            {
                VerifyServiceElevatorInstallation(Check);
                GD.Print($"SERVICE_ELEVATOR_INSTALLATION_VERIFY {(passed ? "PASS" : "FAIL")} sampled bounds and offline QA controller; native full-cycle review pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-wastewater-installation", StringComparer.Ordinal))
            {
                VerifyWastewaterProbeGeometry(Check);
                GD.Print($"WASTEWATER_INSTALLATION_VERIFY {(passed ? "PASS" : "FAIL")} bounds and projection only; native/process acceptance pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-tank-switch-mounts", StringComparer.Ordinal))
            {
                VerifyTankSwitchMountGeometry(Check);
                VerifySumpLevelMotionGeometry(Check);
                GD.Print($"TANK_SWITCH_INSTALLATION_VERIFY {(passed ? "PASS" : "FAIL")} geometry and offline sump QA; native/process acceptance pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-powder-mixer", StringComparer.Ordinal))
            {
                VerifyPowderMixerMotion(Check);
                GD.Print($"POWDER_MIXER_VERIFY {(passed ? "PASS" : "FAIL")} PLC-output projection and timed actuator motion; no material-flow simulation");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-pallet-robot-controller", StringComparer.Ordinal))
            {
                VerifyPalletRobotCommandBoundary(Check);
                GD.Print($"PALLET_ROBOT_CONTROLLER_BOUNDARY_VERIFY {(passed ? "PASS" : "FAIL")} virtual command boundary only; native loaded-ladder workflow pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-shipping-pallet", StringComparer.Ordinal))
            {
                VerifyShippingPalletGeometry(Check);
                VerifyShippingPalletControllerWorkflow(Check);
                GD.Print($"SHIPPING_PALLET_VERIFY {(passed ? "PASS" : "FAIL")} offline geometry and loaded-controller workflow; native review pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-multi-conveyor", StringComparer.Ordinal))
            {
                VerifyMultiConveyorControllerWorkflow(Check);
                GD.Print($"MULTI_CONVEYOR_VERIFY {(passed ? "PASS" : "FAIL")} loaded-controller command workflow; native and pallet travel review pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--verify-motor-array", StringComparer.Ordinal))
            {
                VerifyMotorArrayGeometry(Check);
                VerifyMotorArrayControllerWorkflow(Check);
                GD.Print($"MOTOR_ARRAY_VERIFY {(passed ? "PASS" : "FAIL")} independent/group command projection; native timed-controller cycle pending");
                GetTree().Quit(passed ? 0 : 1);
                return;
            }
            VerifyPalletizerWorkflow(Check);
            AddMigratedScene("lab-11-19-powder-batch-mixer", _candidateCatalog!, _mainCamera!, false, false);
            var mixer = _sceneCompositionRoot!;
            Check(LogBoundsCandidates(mixer) == 0, "mixer_static_equipment_clearance_screen");
            var tankShells = ReviewMeshes(mixer).Where(mesh => mesh.Name == "TANK_shell").ToArray();
            Check(tankShells.Length == 3 && tankShells.All(shell =>
            {
                var size = ReviewBounds(shell).Size;
                return MathF.Abs(size.X - 2.6f) < 0.01f && MathF.Abs(size.Y - 2.5f) < 0.01f
                    && MathF.Abs(size.Z - 2.6f) < 0.01f;
            }), "mixer_tank_shells_match_authored_dimensions");
            var chute = mixer.GetNode<Node3D>("training_accessory_10");
            var chuteMeshes = ReviewMeshes(chute);
            Check(chuteMeshes.Any(mesh => mesh.Name == "CHUTE_bottom")
                && chuteMeshes.Count(mesh => mesh.Name.ToString().StartsWith("CHUTE_side_", StringComparison.Ordinal)) == 2
                && !chuteMeshes.Any(mesh => mesh.Name.ToString().Contains("SHUTTER", StringComparison.Ordinal)),
                "mixer_chute_is_open_channel_without_door_geometry");
            Check(MathF.Abs(ReviewBounds(chute).Position.Y) < 0.01f
                && MathF.Abs(ReviewBounds(mixer.GetNode<Node3D>("valve_4")).Position.Y) < 0.01f,
                "mixer_chute_and_valve_supports_rest_on_floor");

            VerifyParcelGeometry(Check);
            VerifyInspectionConveyorGeometry(Check);
            VerifyGalleryGeometry(Check);
            VerifyDriveAlarmGeometry(Check);
            VerifyMotorStateProjection(Check);
            VerifyMotorRecordGeometry(Check);
            VerifyMotorArrayGeometry(Check);
            VerifyWastewaterProbeGeometry(Check);
            VerifyLabelPrintGeometry(Check);
            VerifySelectorProjection(Check);
            VerifyInboundToteGeometry(Check);
            VerifyAssemblyLiftGeometry(Check);
            VerifyCoolantJugGeometry(Check);
            VerifyFumeSpeedProjection(Check);
            VerifySumpInstallation(Check);
            VerifyDrillStartPermissives(Check);
            VerifyDrillFeedGeometry(Check);
            VerifyServiceDoorPlacement(Check);
            VerifyServiceElevatorInstallation(Check);
            VerifyPressCountWorkflow(Check);
            VerifyCounterResetWorkflow(Check);
            VerifyRepeatCycleWorkflow(Check);
            VerifyButtonCounterWorkflows(Check);
            VerifyParkingEntryWorkflow(Check);
            VerifyPackageGroupingWorkflow(Check);
            VerifyTimerLessonWorkflows(Check);
            VerifyRotaryFlasherWorkflow(Check);
            VerifyFlashPairWorkflows(Check);
            VerifyRunningTowerWorkflow(Check);
            VerifyPedestrianCrossingWorkflow(Check);
            VerifyDrawbridgeWorkflow(Check);
            VerifyBagIndexWorkflow(Check);
            VerifyCoatingWorkflow(Check);
            VerifyHandDryerWorkflow(Check);
            VerifyTransferCartonSupport(Check);
            VerifyBaseConveyorCartonSupport(Check);
            VerifyPusherRodGeometry(Check);
            VerifyPusherInstallationGeometry(Check);
            VerifyCartonReceiverGeometry(Check);
            VerifyCartonPusherContactGeometry(Check);
            VerifyInclinedPhotoeyeGeometry(Check);
            VerifyCartonStaticCableRoutes(Check);
            VerifyPalletRobotInstallationGeometry(Check);
            VerifyShippingPalletGeometry(Check);
            VerifyBottleShuttlePlacement(Check);
            VerifyToteFinishingPlacement(Check);
            VerifyTankSwitchMountGeometry(Check);
            VerifyTankAnalogMountGeometry(Check);
            VerifyTankPipingGeometry(Check);
            VerifyPalletCountPropGeometry(Check);
            VerifyCutLengthDisplayGeometry(Check);
            VerifyStaticTrainingReadouts(Check);
            VerifyArithmeticValidityLayouts(Check);
            VerifyArithmeticNumericWorkflows(Check);
            VerifyFunctionSelectorWorkflow(Check);
            VerifySumCounterWorkflow(Check);
            VerifyBoxVolumeFixture(Check);
            VerifyBoxVolumeNumericWorkflow(Check);
            VerifyPalletCountReadoutWorkflow(Check);
            VerifyNumericSceneOutputTypes(Check);
            VerifyRadarMountAndBeam(Check);

            AddMigratedScene("tank-radar", _candidateCatalog!, _mainCamera!, false, false);
            // Exercise an authored parent scale as well as configured sizing.
            var radarTank = _sceneCompositionRoot!.GetNode<Node3D>("water_tank_radar");
            radarTank.Scale = new Vector3(0.7f, 0.6f, 0.7f);
            _sceneRuntime!.ResetSimulation();
            var liquid = radarTank.FindChild("KIN_liquid", true, false) as MeshInstance3D;
            var transmitter = _sceneCompositionRoot.GetNode<Node3D>("radar_transmitter");
            var lens = (MeshInstance3D)transmitter.FindChild("ANTENNA_dielectric_lens", true, false);
            var expectedDistance = Math.Max(0.08, ReviewBounds(lens).Position.Y - ReviewBounds(liquid!).End.Y);
            var actualDistance = Convert.ToDouble(_sceneRuntime.Points["radar_distance"]);
            GD.Print($"RADAR_GEOMETRY expected={expectedDistance} actual={actualDistance}");
            Check(Math.Abs(actualDistance - expectedDistance) < 0.001, "radar_distance_uses_world_scaled_liquid_surface");
            GD.Print($"SCENE_GEOMETRY_VERIFY {(passed ? "PASS" : "FAIL")} bounds screen only; no mechanical or live acceptance");
            GetTree().Quit(passed ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"SCENE_GEOMETRY_VERIFY FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void VerifyPowderMixerMotion(Action<bool, string> check)
    {
        AddMigratedScene("lab-11-19-powder-batch-mixer", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var motor = root.GetNode<Node3D>("motor_3");
        var valve = root.GetNode<Node3D>("valve_4");
        var feeder = root.GetNode<Node3D>("training_accessory_7");
        var motorMotion = motor.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var valveMotion = valve.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var gateMotion = feeder.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var motorShaft = motor.FindChildren("KIN_motor_*", string.Empty, true, false).OfType<Node3D>().First();
        var valveStem = valve.FindChildren("KIN_valve_stem*", string.Empty, true, false).OfType<Node3D>().Single();
        var gateBlade = feeder.FindChildren("KIN_SLIDE_GATE*", string.Empty, true, false).OfType<Node3D>().Single();
        var motorHome = motorShaft.Transform;
        var valveHome = valveStem.Transform;
        var gateHome = gateBlade.Transform;

        _sceneRuntime!.CommitExternalPlcOutputs(new Dictionary<string, object?>
        {
            ["dose_run"] = true,
            ["mixer_run"] = true,
            ["discharge_valve_open"] = true,
        });
        motorMotion._PhysicsProcess(0.05);
        valveMotion._PhysicsProcess(0.6);
        gateMotion._PhysicsProcess(0.575);
        check(motorMotion.Running && !motorShaft.Transform.IsEqualApprox(motorHome),
            "powder_mixer_output_rotates_mixer_motor");
        check(valveMotion.PositionPercent > 40 && valveMotion.PositionPercent < 60
            && !valveStem.Transform.IsEqualApprox(valveHome),
            "powder_mixer_discharge_valve_uses_timed_travel");
        check(gateMotion.PositionPercent > 40 && gateMotion.PositionPercent < 60
            && !gateBlade.Transform.IsEqualApprox(gateHome),
            "powder_mixer_dose_command_moves_feeder_gate");

        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?>
        {
            ["dose_run"] = false,
            ["mixer_run"] = false,
            ["discharge_valve_open"] = false,
        });
        valveMotion._PhysicsProcess(1.2);
        gateMotion._PhysicsProcess(1.15);
        check(!motorMotion.Running && valveMotion.PositionPercent == 0 && gateMotion.PositionPercent == 0
            && valveStem.Transform.IsEqualApprox(valveHome) && gateBlade.Transform.IsEqualApprox(gateHome),
            "powder_mixer_commands_off_stop_motor_and_close_actuators");
    }

    private void VerifyParcelGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-23-parcel-sorter", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var definition = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.First(scene => scene.Id == "lab-2-23-parcel-sorter"));
        var equipment = root.GetChildren().OfType<Node3D>().ToArray();
        var cartons = equipment.Where(node => node.Name.ToString().EndsWith("_parcel", StringComparison.Ordinal)).ToArray();
        var authored = cartons.ToDictionary(node => node, node => node.Transform);
        var tables = equipment.Where(node => node.Name.ToString().StartsWith("sort_turntable_", StringComparison.Ordinal)).ToArray();
        var motions = tables.SelectMany(node => node.FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>()).ToArray();
        check(tables.Length == 2 && motions.Length == 2 && tables.All(table => !ReviewMeshes(table).Any(mesh =>
            mesh.Name.ToString().StartsWith("TABLE_fixture_", StringComparison.Ordinal))), "parcel_tables_have_no_machining_jaws_in_load_path");
        check(equipment.Where(node => definition.Equipment.Any(item => item.Id == node.Name && item.Type == "conveyor"))
            .All(node => MathF.Abs(ReviewBounds(node).Position.Y) < 0.01f), "parcel_conveyor_feet_grounded");
        var deck = root.GetNode<Node3D>("sort_infeed").FindChild("KIN_belt_surface", true, false) as MeshInstance3D;
        var deckBounds = ReviewBounds(deck!);
        check(MathF.Abs(deckBounds.Size.Z - 1.2f) < 0.01f && MathF.Abs(deckBounds.End.Y - 1.055f) < 0.01f,
            "parcel_infeed_honors_belt_width_and_deck_height");
        var portal = root.GetNode<Node3D>("size_sensor_bank");
        var beamHeights = Enumerable.Range(1, 3).Select(channel =>
            ReviewBounds((MeshInstance3D)portal.FindChild($"KIN_beam_size_{channel}", true, false)).GetCenter().Y).ToArray();
        var parcelTops = new[] { "small_parcel", "medium_parcel", "large_parcel" }
            .Select(id => ReviewBounds(root.GetNode<Node3D>(id)).End.Y).ToArray();
        check(beamHeights[0] > deckBounds.End.Y && beamHeights[0] < parcelTops[0]
            && beamHeights[1] > parcelTops[0] && beamHeights[1] < parcelTops[1]
            && beamHeights[2] > parcelTops[1] && beamHeights[2] < parcelTops[2],
            "parcel_optical_beams_clear_deck_and_distinguish_three_carton_heights");

        var cartonSizes = cartons.ToDictionary(node => node, node => definition.Equipment.First(item => item.Id == node.Name)
            .Config.GetProperty("size").EnumerateArray().Select(value => value.GetSingle()).ToArray());
        var supports = definition.Equipment.Where(item => item.Type is "conveyor" or "rotaryTable").Select(item =>
        {
            var node = root.GetNode<Node3D>(item.Id);
            return (Inverse: node.GlobalTransform.AffineInverse(), Height: item.Config.GetProperty("deckHeight").GetSingle(),
                Circle: item.Type == "rotaryTable", HalfLength: item.Type == "conveyor" ? item.Config.GetProperty("length").GetSingle() * 2.935f / 6f : 0,
                HalfWidth: item.Type == "conveyor" ? item.Config.GetProperty("width").GetSingle() / 2f : 0,
                Radius: item.Type == "rotaryTable" ? item.Config.GetProperty("radius").GetSingle() : 0);
        }).ToArray();
        // These supports/frames stay fixed throughout the declared path. The
        // only rotating table surfaces are below the cartons' bottom plane.
        // Cache their bounds rather than making millions of Godot interop calls.
        var solids = equipment.Except(cartons).SelectMany(node => ReviewMeshes(node).Where(mesh =>
            !mesh.Name.ToString().Contains("beam", StringComparison.OrdinalIgnoreCase))
            .Select(mesh => (Equipment: node.Name, Mesh: mesh.Name, Node: mesh, Bounds: ReviewBounds(mesh))))
            .Where(solid => solid.Bounds.End.Y > 1.061f).ToArray();
        var cartonMeshes = cartons.ToDictionary(node => node, ReviewMeshes);

        bool Supported(Node3D carton)
        {
            var size = cartonSizes[carton];
            var transform = carton.GlobalTransform;
            var contacts = new List<Vector2>();
            // Sample actual oriented bottom footprints, including transitions
            // across the small nose gaps. A support hull containing the center
            // checks balanced geometry; it is not a dynamics/load calculation.
            for (var ix = 0; ix < 7; ix++)
            for (var iz = 0; iz < 7; iz++)
            {
                var point = transform * new Vector3(size[0] * (ix / 6f - 0.5f), 0, size[2] * (iz / 6f - 0.5f));
                foreach (var support in supports)
                {
                    var local = support.Inverse * point;
                    if (MathF.Abs(local.Y - support.Height) > 0.015f) continue;
                    var covered = !support.Circle
                        ? MathF.Abs(local.X) <= support.HalfLength && MathF.Abs(local.Z) <= support.HalfWidth
                        : new Vector2(local.X, local.Z).Length() <= support.Radius;
                    if (!covered) continue;
                    contacts.Add(new Vector2(point.X, point.Z));
                    break;
                }
            }
            if (contacts.Count < 18) return false;
            var hull = ContactHull(contacts);
            var center = new Vector2(carton.GlobalPosition.X, carton.GlobalPosition.Z);
            return hull.Count >= 3 && Enumerable.Range(0, hull.Count).All(index =>
                (hull[(index + 1) % hull.Count] - hull[index]).Cross(center - hull[index]) >= -0.001f);
        }

        var supported = cartons.Length == 3 && cartons.All(Supported);
        var clear = true;
        var reported = false;
        runtime.UsesExternalClock = false; // Declared plant preview only, no controller/PLC seam.
        runtime.ExecuteAction("start-sort");
        for (var frame = 0; frame < 2300; frame++)
        {
            runtime.AdvanceSimulation(0.02);
            foreach (var motion in motions) motion._PhysicsProcess(0.02);
            supported &= cartons.All(Supported);
            var cartonBounds = cartons.ToDictionary(node => node, ReviewBounds);
            foreach (var carton in cartons)
            foreach (var solid in solids.Concat(cartons.Where(node => node != carton).SelectMany(node =>
                cartonMeshes[node].Select(mesh => (Equipment: node.Name, Mesh: mesh.Name, Node: mesh, Bounds: cartonBounds[node])))))
            {
                var overlap = cartonBounds[carton].Intersection(solid.Bounds).Size;
                if (overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f) continue;
                if (!cartonMeshes[carton].Any(mesh => OrientedBoxesPenetrate(mesh, solid.Node))) continue;
                clear = false;
                if (!reported)
                {
                    GD.Print($"PARCEL_PATH_INTERFERENCE frame={frame} {carton.Name}/{solid.Equipment} mesh={solid.Mesh} overlap={overlap}");
                    reported = true;
                }
            }
        }
        check(supported, "parcel_full_path_has_balanced_surface_contacts");
        check(clear, "parcel_full_path_clear_of_other_cartons_and_solid_equipment");
        check(Convert.ToInt64(runtime.Points["sorted_count"]) == 3 && runtime.Points["cycle_complete"] is true,
            "parcel_supported_path_finishes_three_way_batch");

        runtime.ResetSimulation();
        runtime.ExecuteAction("start-sort");
        for (var frame = 0; frame < 870; frame++)
        {
            runtime.AdvanceSimulation(0.02);
            foreach (var motion in motions) motion._PhysicsProcess(0.02);
        }
        runtime.StopSimulation();
        var held = cartons.ToDictionary(node => node, node => node.Transform);
        var heldPlates = tables.Select(table => table.FindChild("KIN_table", true, false) as Node3D).ToArray();
        var heldTransforms = heldPlates.Select(plate => plate!.Transform).ToArray();
        runtime.AdvanceSimulation(0.4);
        foreach (var motion in motions) motion._PhysicsProcess(0.4);
        check(cartons.All(node => node.Transform == held[node]) && heldPlates.Select((plate, index) =>
            plate!.Transform == heldTransforms[index]).All(value => value), "parcel_stop_holds_cartons_and_indexing_tables");
        runtime.ResetSimulation();
        check(cartons.All(node => node.Transform == authored[node]) && motions.All(motion => motion.PositionPercent == 0),
            "parcel_reset_restores_cartons_and_table_pose");
    }

    private void VerifyInspectionConveyorGeometry(Action<bool, string> check)
    {
        AddMigratedScene("conveyor-cell", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var conveyor = root.GetNode<Node3D>("main_conveyor");
        var cartons = Enumerable.Range(1, 3).Select(index => root.GetNode<Node3D>($"carton_{index}")).ToArray();
        var authored = cartons.Select(carton => carton.Transform).ToArray();
        var deck = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
        var sensor = root.GetNode<Node3D>("inspection_photoeye");
        var stands = ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString() is "TX_post" or "RX_post").ToArray();
        var feet = ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString() is "TX_foot" or "RX_foot").ToArray();
        var beams = sensor.FindChildren("KIN_beam*", string.Empty, true, false).OfType<MeshInstance3D>().ToArray();
        foreach (var mesh in stands.Concat(feet)) GD.Print($"INSPECTION_SENSOR_GEOMETRY {mesh.Name} position={mesh.GlobalPosition} bounds={ReviewBounds(mesh)}");
        LogBoundsCandidates(root); // Curved cable boxes can overlap diagonal frame boxes.
        var sensorSolids = ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
            && !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var conveyorSolids = ReviewMeshes(conveyor).Where(mesh => !mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var sensorMesh in sensorSolids)
        foreach (var conveyorMesh in conveyorSolids)
            if (OrientedBoxesPenetrate(sensorMesh, conveyorMesh))
                GD.Print($"INSPECTION_SOLID_INTERFERENCE {sensorMesh.Name}/{conveyorMesh.Name}");
        check(sensorSolids.All(sensorMesh => conveyorSolids.All(conveyorMesh => !OrientedBoxesPenetrate(sensorMesh, conveyorMesh))),
            "inspection_sensor_solid_parts_clear_conveyor_frames");
        check(cartons.All(carton => MathF.Abs(ReviewBounds(carton).Position.Y - deck.End.Y) < 0.01f),
            "inspection_cartons_rest_on_actual_belt_surface");
        check(stands.Length == 2 && MathF.Abs(MathF.Abs(stands[0].GlobalPosition.Z - stands[1].GlobalPosition.Z) - 2.6f) < 0.01f
            && feet.Length == 2 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.01f),
            "inspection_photoeye_stands_honor_span_and_grounding");
        check(beams.Length > 0 && beams.All(beam => ReviewBounds(beam).GetCenter().Y > deck.End.Y
            && ReviewBounds(beam).GetCenter().Y < cartons.Min(carton => ReviewBounds(carton).End.Y)),
            "inspection_optical_path_crosses_carton_height_above_deck");
        runtime.UsesExternalClock = false; // Bounded declared plant preview, no PLC transport.
        runtime.ExecuteAction("conveyor_run");
        var support = true;
        var sawBlocked = false;
        var sawClear = false;
        var beamMatches = true;
        for (var frame = 0; frame < 1200; frame++)
        {
            runtime.AdvanceSimulation(0.02);
            support &= cartons.All(carton => MathF.Abs(ReviewBounds(carton).Position.Y - deck.End.Y) < 0.01f
                && carton.GlobalPosition.X > deck.Position.X && carton.GlobalPosition.X < deck.End.X);
            var blocked = runtime.Points["photoeye_blocked"] is true;
            sawBlocked |= blocked; sawClear |= !blocked;
            beamMatches &= beams.All(beam => beam.Visible == !blocked);
        }
        check(support && sawBlocked && sawClear && beamMatches && Convert.ToInt64(runtime.Points["parts_completed"]) > 0,
            "inspection_circulation_holds_height_and_projects_beam_feedback");
        runtime.StopSimulation();
        var held = cartons.Select(carton => carton.Transform).ToArray();
        runtime.AdvanceSimulation(0.4);
        var stopped = cartons.Select((carton, index) => carton.Transform == held[index]).All(value => value);
        runtime.ResetSimulation();
        check(stopped && cartons.Select((carton, index) => carton.Transform == authored[index]).All(value => value),
            "inspection_stop_holds_and_reset_restores_cartons");
    }

    private void VerifyDriveAlarmGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-01-drive-alarm-code-string", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        check(root.GetChildren().OfType<Node3D>().Where(node => ReviewMeshes(node).Length > 0)
            .All(node => ReviewBounds(node).Position.Y >= -0.005f),
            "drive_alarm_equipment_above_finished_floor");
        check(LogBoundsCandidates(root) == 0, "drive_alarm_separate_equipment_clear");
        check(Enumerable.Range(3, 3).All(index =>
        {
            var prop = root.GetNode<Node3D>($"training_accessory_{index}");
            var foot = ReviewBounds((MeshInstance3D)prop.FindChild("STAND_base", true, false));
            var mast = ReviewBounds((MeshInstance3D)prop.FindChild("STAND_mast", true, false));
            return mast.Position.X >= foot.Position.X && mast.End.X <= foot.End.X
                && mast.Position.Z >= foot.Position.Z && mast.End.Z <= foot.End.Z
                && MathF.Abs(mast.Position.Y - foot.End.Y) < 0.005f;
        }), "drive_alarm_masts_land_on_base_plates");
        // Guard the observed identity failures. This checks geometry families,
        // not manufacturer equivalence or independent asset acceptance.
        check(root.GetNode("training_accessory_3").FindChild("VFD_BODY", true, false) is not null
            && root.GetNode("training_accessory_4").FindChild("ALARM_DISPLAY_screen", true, false) is not null
            && root.GetNode("training_accessory_5").FindChild("LENS_red", true, false) is not null
            && !ReviewMeshes(root).Any(mesh => mesh.Name.ToString().StartsWith("SHUTTER_", StringComparison.Ordinal)
                || mesh.Name.ToString().StartsWith("PANEL_edge_post", StringComparison.Ordinal)
                || mesh.Name == "SERVO_DRIVE"), "drive_alarm_correct_prop_families_replace_inherited_copies");
        var labels = new[] { "MESSAGE VALID", "NEXT MESSAGE", "RESET" };
        check(Enumerable.Range(6, 3).All(index =>
            root.GetNode<Node3D>($"switch_{index}").FindChild("OperatorFaceLabel", true, false) is Label3D label
            && label.Text == labels[index - 6]), "drive_alarm_operator_plates_match_input_functions");
        var alarmLens = (MeshInstance3D)root.GetNode("training_accessory_5").FindChild("LENS_red", true, false);
        // Exercise only the symbolic image boundary; this does not connect to a PLC.
        _sceneRuntime!.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["drive_alarm_active"] = true });
        check(alarmLens.MaterialOverride is StandardMaterial3D { EmissionEnabled: true },
            "drive_alarm_bound_status_lens_projects_true");
        _sceneRuntime.ResetSimulation();
        check(alarmLens.MaterialOverride is StandardMaterial3D { EmissionEnabled: false }
            && _sceneRuntime.Points["drive_alarm_active"] is false, "drive_alarm_reset_clears_status_lens");
    }

    private void VerifyLabelPrintGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-02-chicken-label-print", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        check(root.GetChildren().OfType<Node3D>().Where(node => ReviewMeshes(node).Length > 0)
            .All(node => ReviewBounds(node).Position.Y >= -0.005f), "label_print_visible_equipment_above_finished_floor");
        check(LogBoundsCandidates(root) == 0, "label_print_separate_solid_equipment_clear");
        var weigh = root.GetNode<Node3D>("training_accessory_5");
        // Reset now represents an absent product at the entry pose. The
        // loaded-controller typed verifier checks full deck support after travel.
        var food = root.GetNode<Node3D>("training_accessory_4");
        check(_sceneRuntime!.Points["product_present"] is false
            && _sceneRuntime.Points["product_weighed"] is false
            && Convert.ToDouble(_sceneRuntime.Points["weight_kg"]) == 0
            && MathF.Abs(food.Position.X + 2f) < 0.005f,
            "label_print_absent_product_starts_before_weigh_deck");
        var carton = ReviewBounds(root.GetNode<Node3D>("box_2"));
        check(ReviewMeshes(root.GetNode("conveyor_0")).Where(mesh => mesh.Name == "KIN_belt_surface").Any(mesh =>
        {
            var belt = ReviewBounds(mesh);
            return MathF.Abs(carton.Position.Y - belt.End.Y) < 0.005f
                && carton.Position.X >= belt.Position.X && carton.End.X <= belt.End.X
                && carton.Position.Z >= belt.Position.Z && carton.End.Z <= belt.End.Z;
        }), "label_print_carton_supported_by_actual_input_belt");
        check(root.GetNode("training_accessory_4").FindChild("FOOD_TRAY_bottom", true, false) is not null
            && weigh.FindChild("WEIGH_DECK_surface", true, false) is not null
            && root.GetNode("training_accessory_6").FindChild("PRINTER_exit_slot", true, false) is not null
            && root.GetNode("training_accessory_7").FindChild("LABEL_DISPLAY_screen", true, false) is not null
            && !ReviewMeshes(root).Any(mesh => mesh.Name.ToString().StartsWith("SHUTTER_", StringComparison.Ordinal)
                || mesh.Name.ToString().StartsWith("PUSHER_", StringComparison.Ordinal)),
            "label_print_correct_training_prop_families");
        var foot = ReviewBounds((MeshInstance3D)weigh.FindChild("WEIGH_readout_foot", true, false));
        var mast = ReviewBounds((MeshInstance3D)weigh.FindChild("WEIGH_readout_mast", true, false));
        check(MathF.Abs(foot.Position.Y) < 0.005f && MathF.Abs(mast.Position.Y - foot.End.Y) < 0.005f
            && mast.Position.X >= foot.Position.X && mast.End.X <= foot.End.X
            && mast.Position.Z >= foot.Position.Z && mast.End.Z <= foot.End.Z,
            "label_print_readout_mast_supported_on_grounded_foot");
        var printer = root.GetNode<Node3D>("training_accessory_6");
        var paper = ReviewBounds((MeshInstance3D)printer.FindChild("PRINTER_demo_paper", true, false));
        var tray = ReviewBounds((MeshInstance3D)printer.FindChild("PRINTER_output_tray", true, false));
        check(MathF.Abs(paper.Position.Y - tray.End.Y) < 0.002f
            && paper.Position.X >= tray.Position.X && paper.End.X <= tray.End.X
            && paper.Position.Z >= tray.Position.Z && paper.End.Z <= tray.End.Z,
            "label_print_demo_paper_rests_on_output_tray");
    }

    private void VerifyMotorStateProjection(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-04-motor-enum-state", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var labels = new Dictionary<string, string>
        {
            ["switch_1"] = "START REQUEST", ["switch_3"] = "STOP REQUEST", ["switch_4"] = "FAULT ACTIVE",
        };
        check(labels.All(pair => root.GetNode(pair.Key).FindChild("OperatorFaceLabel", true, false)
            is Label3D label && label.Text == pair.Value), "motor_state_operator_plates_identify_distinct_inputs");
        var motor = root.GetNode<Node3D>("motor_0");
        var motion = motor.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var shaft = motor.FindChildren("KIN_motor_*", string.Empty, true, false).OfType<Node3D>().First();
        var authored = shaft.Transform;
        // Supply the declared symbolic output image only. No controller logic,
        // enum state transitions, or physical PLC behavior is inferred here.
        runtime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_running"] = true });
        motion._PhysicsProcess(0.137);
        check(motion.Running && shaft.Transform != authored, "motor_state_true_output_turns_actual_shaft");
        runtime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_running"] = false });
        var held = shaft.Transform;
        motion._PhysicsProcess(0.137);
        check(!motion.Running && shaft.Transform == held, "motor_state_false_output_holds_actual_shaft");
        runtime.ResetSimulation();
        check(!motion.Running && shaft.Transform == authored && runtime.Points["motor_running"] is false,
            "motor_state_reset_restores_shaft_and_output");
    }

    private void VerifyMotorRecordGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-05-motor-struct-data", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        check(LogBoundsCandidates(root) == 0, "motor_record_separate_equipment_bounds_clear");
        foreach (var id in new[] { "training_accessory_3", "training_accessory_4", "training_accessory_5" })
        {
            var display = root.GetNode<Node3D>(id);
            var fixtureReadout = id == "training_accessory_3"
                ? display.FindChild("StaticReadout", true, false) as Label3D
                : display.GetNodeOrNull<Label3D>("NumericReadout");
            check(display.FindChild("COUNT_DISPLAY_screen", true, false) is MeshInstance3D
                && fixtureReadout is not null
                && fixtureReadout.Text.EndsWith(id == "training_accessory_3" ? "\nINVALID" : "\n—", StringComparison.Ordinal)
                && !ReviewMeshes(display).Any(mesh => mesh.Name.ToString().Contains("SHUTTER", StringComparison.Ordinal)),
                $"motor_record_{id}_actual_display_without_shutter_or_fabricated_measurement");
            var foot = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
            var mast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
            var housing = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
            check(MathF.Abs(ReviewBounds(foot).Position.Y) < .001f
                && ReviewBounds(foot).Grow(.001f).Intersects(ReviewBounds(mast))
                && ReviewBounds(mast).Grow(.001f).Intersects(ReviewBounds(housing)), $"motor_record_{id}_grounded_supported_display");
        }
        // Verify projection of an explicit symbolic command, without supplying
        // a lesson controller or claiming STRUCT/temperature simulation.
        var runtime = _sceneRuntime!;
        var motion = root.GetNode<Node3D>("motor_0").FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>().Single();
        runtime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_enable"] = true });
        check(motion.Running, "motor_record_declared_enable_projects_to_motor");
        runtime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_enable"] = false });
        check(!motion.Running, "motor_record_removed_enable_stops_motor");
    }

    private void VerifyMotorArrayGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-06-ten-motor-array-startup", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var motors = root.GetChildren().OfType<Node3D>().Where(node => node.Name.ToString().StartsWith("motor_", StringComparison.Ordinal)).ToArray();
        check(motors.Length == 10 && motors.All(node => node.FindChildren("KIN_motor_*", string.Empty, true, false).Count > 0),
            "motor_array_ten_actual_motors_without_lineup_surrogate");
        check(LogBoundsCandidates(root) == 0, "motor_array_separate_equipment_bounds_clear");
        foreach (var id in new[] { "training_accessory_7", "training_accessory_8" })
        {
            var display = root.GetNode<Node3D>(id);
            check(display.FindChild("COUNT_DISPLAY_screen", true, false) is MeshInstance3D
                && display.FindChild("StaticReadout", true, false) is Label3D label
                && label.Text == (id == "training_accessory_7" ? "COMMANDS ON\n0/10" : "SEQUENCE CMD\nOFF"),
                $"motor_array_{id}_actual_display_without_fabricated_sequence_data");
            var foot = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
            var mast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
            var housing = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
            var installation = root.GetNode<Node3D>("ControlInstallation");
            var fascia = (MeshInstance3D)installation.FindChild("OperatorCabinet_Door", true, false);
            var cabinet = (MeshInstance3D)installation.FindChild("OperatorCabinet_Body", true, false);
            var plinth = (MeshInstance3D)installation.FindChild("CabinetPlinth", true, false);
            var headBounds = ReviewBounds(housing);
            var faceBounds = ReviewBounds(fascia);
            check(display.HasMeta("installationKind") && display.GetMeta("installationKind").AsString() == "operatorPanel"
                && !foot.IsVisibleInTree() && !mast.IsVisibleInTree() && housing.IsVisibleInTree()
                && headBounds.Position.X >= faceBounds.Position.X - .001f && headBounds.End.X <= faceBounds.End.X + .001f
                && headBounds.Position.Y >= faceBounds.Position.Y - .001f && headBounds.End.Y <= faceBounds.End.Y + .001f
                && MathF.Min(MathF.Abs(headBounds.Position.Z - faceBounds.End.Z), MathF.Abs(headBounds.End.Z - faceBounds.Position.Z)) < .002f
                && MathF.Abs(ReviewBounds(plinth).Position.Y) < .001f
                && MathF.Abs(ReviewBounds(plinth).End.Y - ReviewBounds(cabinet).Position.Y) < .001f,
                $"motor_array_{id}_cabinet_supported_display_hidden_original_ground_stand");
        }
        var motions = motors.SelectMany(node => node.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>()).ToArray();
        // Preserve the existing group command alongside independent outputs.
        // Projection checks do not prove a timed loaded-controller sequence.
        _sceneRuntime!.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_array_run"] = true });
        check(motions.Length == 10 && motions.All(motion => motion.Running), "motor_array_group_command_projects_to_all_ten_motors");
        check(root.GetNode<Node3D>("training_accessory_7").FindChild("StaticReadout", true, false) is Label3D groupReadout
            && groupReadout.Text == "COMMANDS ON\n10/10", "motor_array_display_legacy_group_counts_ten");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_array_run"] = false });
        check(motions.All(motion => !motion.Running), "motor_array_group_command_off_stops_all_ten_motors");
        // Exercise independent requests with the legacy group command false.
        // An active individual output must neither start its neighbours nor
        // disappear because the group binding occurs earlier in the JSON.
        for (var index = 0; index < 10; index++)
        {
            _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { [$"motor_{index}_run"] = true });
            check(motors.All(motor => ControllersForReview(motor).Single().Running ==
                (motor.Name.ToString() == $"motor_{index}")), $"motor_array_independent_{index}_only");
            check(root.GetNode<Node3D>("training_accessory_7").FindChild("StaticReadout", true, false) is Label3D individualReadout
                && individualReadout.Text == "COMMANDS ON\n1/10", $"motor_array_display_individual_{index}_counts_one");
            _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { [$"motor_{index}_run"] = false });
        }
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_3_run"] = true, ["motor_array_run"] = true });
        check(motions.All(motion => motion.Running), "motor_array_legacy_group_overrides_individual_off");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_array_run"] = false });
        check(motors.All(motor => ControllersForReview(motor).Single().Running == (motor.Name.ToString() == "motor_3")),
            "motor_array_group_release_preserves_individual_request");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["startup_sequence_active"] = true });
        check(root.GetNode<Node3D>("training_accessory_8").FindChild("StaticReadout", true, false) is Label3D activeReadout
            && activeReadout.Text == "SEQUENCE CMD\nACTIVE", "motor_array_display_sequence_command_active");
        _sceneRuntime.ResetSimulation();
        check(root.GetNode<Node3D>("training_accessory_7").FindChild("StaticReadout", true, false) is Label3D resetCount
            && resetCount.Text == "COMMANDS ON\n0/10"
            && root.GetNode<Node3D>("training_accessory_8").FindChild("StaticReadout", true, false) is Label3D resetSequence
            && resetSequence.Text == "SEQUENCE CMD\nOFF", "motor_array_display_reset_clears_count_and_sequence");
        check(motions.All(motion => !motion.Running)
            && Enumerable.Range(0, 10).All(index => _sceneRuntime.Points[$"motor_{index}_run"] is false),
            "motor_array_reset_clears_all_individual_requests");

        static IEnumerable<EquipmentMotionController> ControllersForReview(Node3D motor) =>
            motor.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>();
    }

    private void VerifyWastewaterProbeGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-11-06-wastewater-collection", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var tank = root.GetNode<Node3D>("tank_0");
        var probe = root.GetNode<Node3D>("training_accessory_7");
        MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
        var roof = ReviewBounds(Part(tank, "TANK_roof"));
        var flange = ReviewBounds(Part(probe, "PROCESS_flange"));
        var socket = ReviewBounds(Part(tank, "TANK_ANALOG_socket"));
        check(socket.Grow(.001f).Intersects(roof) && socket.Grow(.001f).Intersects(flange),
            "wastewater_probe_socket_mates_roof_and_process_flange");
        var obstructions = ReviewMeshes(tank).Where(mesh => mesh.Name.ToString().StartsWith("RAIL_", StringComparison.Ordinal)
            || mesh.Name.ToString().Contains("manway", StringComparison.OrdinalIgnoreCase)).ToArray();
        bool ClearOfRoofPart(MeshInstance3D part, MeshInstance3D obstruction)
        {
            if (!ReviewBounds(part).Intersects(ReviewBounds(obstruction))) return true;
            if (obstruction.Name.ToString() is "RAIL_mid" or "RAIL_top")
            {
                // A torus' OBB fills its hollow centre. Require the whole probe mesh
                // to fit inside the sampled inner ring with 10 mm chord/tolerance margin.
                float Radius(Vector3 point) => new Vector2(point.X - roof.GetCenter().X,
                    point.Z - roof.GetCenter().Z).Length();
                float[] Radii(MeshInstance3D mesh) => Enumerable.Range(0, mesh.Mesh.GetSurfaceCount())
                    .SelectMany(surface => mesh.Mesh.SurfaceGetArrays(surface)[(int)Mesh.ArrayType.Vertex].AsVector3Array())
                    .Select(vertex => Radius(mesh.GlobalTransform * vertex)).ToArray();
                var probeRadii = Radii(part); var railRadii = Radii(obstruction);
                return probeRadii.Length > 0 && railRadii.Length > 0 && probeRadii.Max() < railRadii.Min() - .01f;
            }
            return !OrientedBoxesPenetrate(part, obstruction);
        }
        foreach (var part in ReviewMeshes(probe))
        foreach (var obstruction in obstructions)
            if (!ClearOfRoofPart(part, obstruction)) GD.Print($"WASTEWATER_PROBE_CANDIDATE {part.Name}/{obstruction.Name}");
        check(obstructions.Length > 0 && ReviewMeshes(probe).All(part => obstructions.All(obstruction => ClearOfRoofPart(part, obstruction))),
            "wastewater_probe_clear_of_roof_rail_and_manway_bounds_screen");
        var liquid = ReviewBounds(Part(tank, "KIN_liquid"));
        var tip = ReviewBounds(Part(probe, "PROBE_tip_weight"));
        var rod = ReviewBounds(Part(probe, "PROBE_sensing_rod"));
        check(MathF.Abs(tip.Position.Y - liquid.Position.Y) < .001f && rod.Grow(.001f).Intersects(tip),
            "wastewater_probe_tip_at_liquid_zero_datum_and_attached_to_rod");
        var shells = ReviewMeshes(root).Where(mesh => mesh.Name == "TANK_shell").ToArray();
        check(shells.Length == 4 && !shells.Where((a, i) => shells.Skip(i + 1).Any(b => ReviewBounds(a).Intersects(ReviewBounds(b)))).Any(),
            "wastewater_four_tank_shells_have_separate_bounds");
        var piping = root.GetNode<Node3D>("training_accessory_8");
        check(piping.FindChild("WASTEWATER_collector", true, false) is MeshInstance3D
            && piping.FindChildren("WASTEWATER_branch_?", "MeshInstance3D", true, false).Count == 3
            && piping.FindChild("WASTEWATER_discharge", true, false) is MeshInstance3D
            && piping.FindChild("WASTEWATER_treatment_boundary", true, false) is MeshInstance3D,
            "wastewater_actual_process_routes_replace_pneumatic_manifold");
        Vector3 RingCenter(string name)
        {
            var mesh = (MeshInstance3D)piping.FindChild(name, true, false);
            var vertices = mesh.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            return mesh.GlobalTransform * (vertices.Aggregate(Vector3.Zero, (sum, vertex) => sum + vertex) / vertices.Length);
        }
        Vector3 PortFace(Node equipment, string name, float sign)
        {
            var mesh = Part(equipment, name);
            return mesh.GlobalTransform * (mesh.GetAabb().GetCenter() + Vector3.Up * (sign * mesh.GetAabb().Size.Y / 2));
        }
        var vesselIds = new[] { "tank_0", "tank_1", "tank_2", "training_accessory_6" };
        for (var index = 0; index < vesselIds.Length; index++)
            check(RingCenter(index == 0 ? "WASTEWATER_collector_start_ring" : $"WASTEWATER_branch_{index}_start_ring")
                .DistanceTo(PortFace(root.GetNode<Node3D>(vesselIds[index]), "NOZZLE_outlet_flange", 1)) < .001f,
                $"wastewater_vessel_{index}_branch_starts_at_actual_outlet_face");
        var pumpNode = root.GetNode<Node3D>("pump_3"); var valveNode = root.GetNode<Node3D>("valve_4");
        check(RingCenter("WASTEWATER_collector_end_ring").DistanceTo(PortFace(pumpNode, "PUMP_suction_flange", 1)) < .001f,
            "wastewater_collector_meets_actual_pump_suction");
        check(RingCenter("WASTEWATER_discharge_start_ring").DistanceTo(PortFace(pumpNode, "PUMP_discharge_flange", 1)) < .001f
            && RingCenter("WASTEWATER_discharge_end_ring").DistanceTo(PortFace(valveNode, "PROCESS_bore_liner", -1)) < .001f,
            "wastewater_discharge_meets_pump_and_valve_faces");
        check(RingCenter("WASTEWATER_treatment_boundary_start_ring").DistanceTo(PortFace(valveNode, "PROCESS_bore_liner", 1)) < .001f,
            "wastewater_treatment_outlet_meets_actual_valve_face");
        var feet = ReviewMeshes(root).Where(mesh => mesh.Name.ToString().StartsWith("WASTEWATER_header_foot_", StringComparison.Ordinal)
            || (valveNode.IsAncestorOf(mesh) && mesh.Name.ToString().StartsWith("PIPE_foot", StringComparison.Ordinal))).ToArray();
        foreach (var foot in feet) GD.Print($"WASTEWATER_FOOT {foot.Name} bottom={ReviewBounds(foot).Position.Y}");
        check(feet.Length >= 5 && feet.All(mesh => MathF.Abs(ReviewBounds(mesh).Position.Y) < .001f),
            "wastewater_header_and_raised_valve_feet_rest_on_floor");
        var pump = root.GetNode<Node3D>("pump_3").FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        _sceneRuntime!.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["transfer_pump_run"] = true });
        check(pump.Running, "wastewater_output_command_runs_actual_pump_controller");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["transfer_pump_run"] = false });
        check(!pump.Running, "wastewater_output_command_off_stops_actual_pump_controller");
        var valve = root.GetNode<Node3D>("valve_4").GetNode<EquipmentMotionController>("PositionRotationController");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["outlet_valve_open"] = false });
        check(valve.PositionInputInverted && !valve.AutonomousPositionTravel && valve.PositionPercent == 100,
            "wastewater_false_valve_command_projects_closed_quarter_turn_without_autonomous_motion");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["outlet_valve_open"] = true });
        check(valve.PositionPercent == 0, "wastewater_true_valve_command_projects_open_authored_bore_alignment");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["outlet_valve_open"] = false });
        // Manual BOOL feedback remains manual: mounting geometry does not manufacture analog measurement.
        check(_sceneRuntime.Points["source_level_high"] is false, "wastewater_probe_installation_preserves_manual_feedback");
    }

    private void VerifyDrillStartPermissives(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-16-safe-drill", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        // This checks the standalone reference Run button, not shell playback
        // under a selected controller's scan clock and output ownership.
        runtime.UsesExternalClock = false;
        bool Blocked(SceneSimulationRuntime preview) => !preview.RunDefault()
            && preview.Points["drill_run"] is false && preview.Points["cycle_complete"] is false;
        check(Blocked(runtime), "drill_default_run_rejects_both_hand_requests_missing");
        runtime.ExecuteAction("toggle-left-hand");
        check(Blocked(runtime), "drill_default_run_rejects_left_hand_only");
        runtime.ResetSimulation();
        runtime.ExecuteAction("toggle-right-hand");
        check(Blocked(runtime), "drill_default_run_rejects_right_hand_only");
        runtime.ExecuteAction("toggle-left-hand");
        var started = runtime.RunDefault();
        for (var tick = 0; tick < 480; tick++) runtime.AdvanceSimulation(1.0 / 120.0);
        check(started && runtime.Points["drill_run"] is false && runtime.Points["drill_at_top"] is true
            && runtime.Points["cycle_complete"] is true, "drill_default_run_completes_reference_cycle_with_all_permissives");

        // Use the actual lesson contract with an absent initial workpiece,
        // without mutating its file or fabricating an action to set PC inputs.
        var scene = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.First(item => item.Id == "lab-2-16-safe-drill"));
        var definition = System.Text.Json.Nodes.JsonNode.Parse(scene.Simulation.GetRawText())!;
        foreach (var point in definition["points"]!.AsArray())
            if (point!["name"]!.GetValue<string>() == "workpiece_present") point["initial"] = false;
        using var json = System.Text.Json.JsonDocument.Parse(definition.ToJsonString());
        var noStock = new SceneSimulationRuntime(json.RootElement, _sceneCompositionRoot!);
        try
        {
            noStock.ResetSimulation();
            noStock.ExecuteAction("toggle-left-hand");
            noStock.ExecuteAction("toggle-right-hand");
            check(Blocked(noStock), "drill_default_run_rejects_absent_workpiece_with_both_requests");
        }
        finally { noStock.Free(); }
        AddMigratedScene("lab-2-14-sump-pump", _candidateCatalog!, _mainCamera!, false, false);
        check(_sceneRuntime!.RunDefault(), "sump_unconditional_default_start_remains_available");
        _sceneRuntime.StopSimulation();
    }

    private void VerifySumpInstallation(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-14-sump-pump", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        foreach (var id in new[] { "sump_tank", "sump_pump", "discharge_isolation", "discharge_pipe", "low_float", "high_float" })
        foreach (var mesh in ReviewMeshes(root.GetNode<Node3D>(id)))
        {
            var name = mesh.Name.ToString();
            if (name.Contains("outlet", StringComparison.OrdinalIgnoreCase) || name.Contains("suction", StringComparison.OrdinalIgnoreCase)
                || name.Contains("discharge", StringComparison.OrdinalIgnoreCase) || name.Contains("PIPE", StringComparison.Ordinal)
                || name.StartsWith("SUPPORT", StringComparison.Ordinal) || name.StartsWith("FORK", StringComparison.Ordinal)
                || name.StartsWith("PROCESS", StringComparison.Ordinal) || name.StartsWith("FLANGE", StringComparison.Ordinal))
                GD.Print($"SUMP_DATUM {id}/{name} bounds={ReviewBounds(mesh)} center={mesh.GlobalPosition}");
        }
        var tank = root.GetNode<Node3D>("sump_tank"); var pump = root.GetNode<Node3D>("sump_pump");
        var valve = root.GetNode<Node3D>("discharge_isolation"); var spool = root.GetNode<Node3D>("discharge_pipe");
        MeshInstance3D Part(Node3D equipment, string name) => (MeshInstance3D)equipment.FindChild(name, true, false);
        Vector3? CapCenter(string name)
        {
            if (pump.FindChild(name, true, false) is not MeshInstance3D cap) return null;
            var vertices = cap.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            return vertices.Length == 0 ? null : cap.GlobalTransform * (vertices.Aggregate(Vector3.Zero, (sum, vertex) => sum + vertex) / vertices.Length);
        }
        bool Near(Vector3? actual, Vector3 expected) => actual is { } point && point.DistanceTo(expected) < 0.001f;
        var tankFace = ReviewBounds(Part(tank, "NOZZLE_outlet_flange"));
        var inlet = ReviewBounds(Part(pump, "PUMP_suction_flange"));
        var outlet = ReviewBounds(Part(pump, "PUMP_discharge_flange"));
        var liner = ReviewBounds(Part(valve, "PROCESS_bore_liner"));
        check(Near(CapCenter("SUMP_suction_start_ring"), tankFace.GetCenter() with { X = tankFace.Position.X })
            && Near(CapCenter("SUMP_suction_end_ring"), inlet.GetCenter() with { X = inlet.End.X }),
            "sump_suction_actual_end_rings_meet_tank_and_pump_flange_faces");
        check(Near(CapCenter("SUMP_discharge_start_ring"), outlet.GetCenter() with { Y = outlet.End.Y })
            && Near(CapCenter("SUMP_discharge_end_ring"), liner.GetCenter() with { Z = liner.End.Z }),
            "sump_riser_elbow_actual_end_rings_meet_upward_pump_and_valve_bore");
        var nearSpool = ReviewBounds(Part(spool, "FLANGE_-1_72"));
        check(MathF.Abs(valve.GlobalBasis.X.Dot(Vector3.Forward)) > 0.999f
            && MathF.Abs(spool.GlobalBasis.X.Dot(Vector3.Forward)) > 0.999f
            && MathF.Abs(nearSpool.GetCenter().X - liner.GetCenter().X) < 0.001f
            && MathF.Abs(nearSpool.GetCenter().Y - liner.GetCenter().Y) < 0.001f
            && MathF.Abs(nearSpool.End.Z - liner.Position.Z) < 0.001f,
            "sump_valve_and_instrumented_spool_have_aligned_horizontal_bores");
        var feet = ReviewMeshes(root).Where(mesh => mesh.Name.ToString().StartsWith("SUPPORT_foot", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("PIPE_foot", StringComparison.Ordinal) || mesh.Name == "SUMP_suction_support_foot").ToArray();
        check(feet.Length == 5 && feet.All(mesh => MathF.Abs(ReviewBounds(mesh).Position.Y) < 0.001f),
            "sump_all_five_installed_pipe_shoes_contact_floor");
        var posts = ReviewMeshes(root).Where(mesh => mesh.Name.ToString().StartsWith("SUPPORT_post", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("PIPE_shoe_post", StringComparison.Ordinal) || mesh.Name == "SUMP_suction_support_post").ToArray();
        check(posts.Length == 5 && posts.All(post => feet.Any(foot => ReviewBounds(post).Intersects(ReviewBounds(foot).Grow(0.001f)))),
            "sump_pipe_support_posts_attach_to_grounded_shoes");
        check(ReviewMeshes(valve).Concat(ReviewMeshes(spool)).All(part => ReviewMeshes(tank).All(tankPart =>
            !ReviewBounds(part).Intersects(ReviewBounds(tankPart)) || !OrientedBoxesPenetrate(part, tankPart))),
            "sump_installed_valve_and_spool_clear_actual_tank_ladder_and_shell");
    }

    private void VerifyFumeSpeedProjection(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-15-fume-extractor", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        var fan = _sceneCompositionRoot!.GetNode<Node3D>("extractor_fan");
        var motion = fan.FindChildren("*", "", true, false).OfType<EquipmentMotionController>()
            .Single(controller => controller.Kind == EquipmentMotionController.MotionKind.FanRotor);
        var rotor = fan.FindChildren("KIN_*", "", true, false).OfType<Node3D>().ToArray();
        var hub = rotor.Single(node => node.Name.ToString().StartsWith("KIN_fan_hub", StringComparison.Ordinal));
        var authored = rotor.ToDictionary(node => node, node => node.Transform);
        // The imported hub is a pivot owning the visible rotor meshes. A
        // single bound pivot is valid; verify its descendants in world space.
        var blades = ReviewMeshes(hub).Where(mesh => mesh.Name.ToString().Contains("blade", StringComparison.OrdinalIgnoreCase)).ToArray();
        var bladeOffsets = blades.ToDictionary(mesh => mesh, mesh => hub.GlobalTransform.AffineInverse() * mesh.GlobalTransform);
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        void Command(bool run, double percent)
        {
            runtime.CommitVirtualControllerNumericOutputs(new Dictionary<string, double> { ["fan_speed_percent"] = percent });
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["fan_run"] = run });
        }
        bool SamePose(Dictionary<Node3D, Transform3D> pose) => rotor.All(node => node.Transform.IsEqualApprox(pose[node]));
        bool HasExpectedRotation(float percent, float? command = null)
        {
            motion.ResetMotion();
            Command(true, command ?? percent);
            const float elapsed = 0.02f;
            motion._PhysicsProcess(elapsed);
            var rotation = new Basis(Vector3.Back, motion.SpeedRpm * percent / 100.0f * Mathf.Tau / 60.0f * elapsed);
            var center = authored[hub].Origin;
            var expectedHub = hub.GetParent<Node3D>().GlobalTransform * new Transform3D(
                rotation * authored[hub].Basis, center);
            return blades.Length == 6 && blades.All(mesh => mesh.GlobalTransform.IsEqualApprox(expectedHub * bladeOffsets[mesh]))
                && rotor.All(node => node.Transform.IsEqualApprox(new Transform3D(
                    rotation * authored[node].Basis, center + rotation * (authored[node].Origin - center))));
        }
        foreach (var percent in new[] { 35.0f, 65.0f, 100.0f })
            check(HasExpectedRotation(percent), $"fume_actual_rotor_follows_{percent}_percent_controller_image");
        check(HasExpectedRotation(100, 150) && HasExpectedRotation(0, -5),
            "fume_speed_projection_clamps_numeric_commands_to_zero_through_nominal");
        Command(true, 0);
        var held = rotor.ToDictionary(node => node, node => node.Transform);
        motion._PhysicsProcess(0.03);
        check(SamePose(held), "fume_zero_speed_holds_rotor_even_with_run_true");
        Command(false, 100);
        held = rotor.ToDictionary(node => node, node => node.Transform);
        motion._PhysicsProcess(0.03);
        check(SamePose(held), "fume_speed_command_does_not_bypass_run_false");
        runtime.SetControllerPlaybackRunning(false);
        Command(true, 65);
        check(!motion.IsPhysicsProcessing(), "fume_paused_controller_clock_disables_rotor_callback");
        runtime.ResetSimulation();
        check(SamePose(authored) && runtime.Points["fan_run"] is false
            && Convert.ToDouble(runtime.Points["fan_speed_percent"]) == 0,
            "fume_reset_restores_actual_rotor_and_off_command_image");
        runtime.UsesExternalClock = false;
        runtime.ExecuteAction("next-speed");
        motion._PhysicsProcess(0.02);
        runtime.StopSimulation();
        held = rotor.ToDictionary(node => node, node => node.Transform);
        // Changing a PC selector while stopped must not restart the preview.
        runtime.ExecuteAction("next-speed");
        motion._PhysicsProcess(0.03);
        check(SamePose(held) && runtime.Points["fan_run"] is false
            && Convert.ToDouble(runtime.Points["fan_speed_percent"]) == 0,
            "fume_standalone_stop_holds_outputs_off_across_selector_action");
        runtime.RunDefault();
        motion._PhysicsProcess(0.02);
        check(!SamePose(held) && runtime.Points["fan_run"] is true
            && Convert.ToDouble(runtime.Points["fan_speed_percent"]) == 65,
            "fume_standalone_run_resumes_from_retained_selector");
    }

    private void VerifyCoolantJugGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-13-coolant-jug-fill", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var jug = root.GetNode<Node3D>("coolant_jug");
        var conveyor = root.GetNode<Node3D>("fill_conveyor");
        var filler = root.GetNode<Node3D>("fill_valve");
        var sensor = root.GetNode<Node3D>("jug_present_sensor");
        var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
        bool Supported()
        {
            var load = ReviewBounds(jug);
            return MathF.Abs(load.Position.Y - belt.End.Y) < 0.001f
                && load.Position.X >= belt.Position.X && load.End.X <= belt.End.X
                && load.Position.Z >= belt.Position.Z && load.End.Z <= belt.End.Z;
        }
        check(Supported(),
            "coolant_jug_bottom_contacts_actual_belt");
        var neck = (MeshInstance3D)jug.FindChild("JUG_NECK", true, false);
        var caps = jug.FindChildren("JUG_CAP*", "", true, false).OfType<Node3D>().ToArray();
        var vertices = neck.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var openMouth = caps.Length == 0 && vertices.Length > 0
            && vertices.All(vertex => new Vector2(vertex.X, vertex.Z).Length() >= 0.074f);
        check(openMouth, "coolant_jug_has_uncapped_annular_mouth");
        bool Solid(MeshInstance3D mesh) => !mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
            && !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)
            && mesh.Name != "JUG_FILL_stream" && mesh.Name != "VISIBLE_LIQUID_STREAM";
        bool Clear(MeshInstance3D a, MeshInstance3D b) => !ReviewBounds(a).Intersects(ReviewBounds(b))
            || !OrientedBoxesPenetrate(a, b);
        var sensorSolids = ReviewMeshes(sensor).Where(Solid).ToArray();
        var conveyorSolids = ReviewMeshes(conveyor).Where(Solid).ToArray();
        var fillerSolids = ReviewMeshes(filler).Where(Solid).ToArray();
        check(sensorSolids.All(a => conveyorSolids.All(b => Clear(a, b))),
            "coolant_sensor_solids_clear_conveyor");
        check(fillerSolids.All(a => conveyorSolids.Concat(sensorSolids).All(b => Clear(a, b))),
            "coolant_fill_structure_clear_conveyor_and_sensor");
        var baseMesh = (MeshInstance3D)filler.FindChild("FILLER_BASE", true, false);
        var frontFoot = filler.FindChild("JUG_FILL_front_foot", true, false) as MeshInstance3D;
        var column = (MeshInstance3D)filler.FindChild("FILLER_COLUMN", true, false);
        var frontColumn = filler.FindChild("JUG_FILL_front_column", true, false) as MeshInstance3D;
        var portal = filler.FindChild("JUG_FILL_portal_beam", true, false) as MeshInstance3D;
        check(frontFoot is not null && frontColumn is not null && portal is not null
            && MathF.Abs(ReviewBounds(baseMesh).Position.Y) < 0.001f
            && MathF.Abs(ReviewBounds(frontFoot).Position.Y) < 0.001f
            && ReviewBounds(column).Position.Y <= ReviewBounds(baseMesh).End.Y
            && ReviewBounds(frontColumn).Position.Y <= ReviewBounds(frontFoot).End.Y
            && OrientedBoxesPenetrate(column, portal) && OrientedBoxesPenetrate(frontColumn, portal),
            "coolant_fill_portal_has_grounded_connected_supports");
        var tip = (MeshInstance3D)filler.FindChild("NOZZLE_TIP", true, false);
        var nozzle = (MeshInstance3D)filler.FindChild("KIN_fill_nozzle", true, false);
        var tipOffset = tip.GlobalPosition - nozzle.GlobalPosition;
        var stream = filler.FindChild("JUG_FILL_stream", true, false) as Node3D;
        var beam = (MeshInstance3D)sensor.FindChild("KIN_beam*", true, false);
        var initial = jug.Transform;
        runtime.UsesExternalClock = false;
        runtime.ExecuteAction("start-fill");
        var support = true; var clear = true; var aligned = true; var attachments = true;
        var sawFill = false; var streamState = stream is not null;
        var collisions = new HashSet<string>();
        for (var sample = 0; sample < 320; sample++)
        {
            runtime.AdvanceSimulation(0.02);
            support &= Supported();
            foreach (var loadMesh in ReviewMeshes(jug))
            foreach (var solid in fillerSolids.Concat(sensorSolids))
            {
                if (Clear(loadMesh, solid)) continue;
                clear = false;
                collisions.Add($"{loadMesh.Name}/{solid.Name}");
            }
            var filling = runtime.Points["fill_valve_open"] is true;
            streamState &= stream is not null && stream.Visible == filling;
            attachments &= (tip.GlobalPosition - nozzle.GlobalPosition).DistanceTo(tipOffset) < 0.001f;
            if (!filling) continue;
            sawFill = true;
            var mouth = ReviewBounds(neck); var tipBounds = ReviewBounds(tip);
            var gap = tipBounds.Position.Y - mouth.End.Y;
            var scan = ReviewBounds(beam).GetCenter(); var body = ReviewBounds(jug);
            aligned &= MathF.Abs(tipBounds.GetCenter().X - mouth.GetCenter().X) < 0.001f
                && MathF.Abs(tipBounds.GetCenter().Z - mouth.GetCenter().Z) < 0.001f
                && gap >= 0.02f && gap <= 0.08f
                && scan.X >= body.Position.X && scan.X <= body.End.X
                && scan.Y > body.Position.Y && scan.Y < body.End.Y;
        }
        GD.Print($"COOLANT_GEOMETRY collisions={string.Join(",", collisions)} endpoint={ReviewBounds(jug)} mouth={ReviewBounds(neck)}");
        check(support && runtime.Points["cycle_complete"] is true, "coolant_full_cycle_keeps_jug_supported");
        check(clear, "coolant_jug_sweep_clears_fill_solids_and_sensor");
        check(sawFill && aligned, "coolant_fill_dwell_aligns_nozzle_and_optical_envelope");
        check(attachments && streamState, "coolant_nozzle_tip_follows_and_stream_tracks_valve");
        runtime.ResetSimulation();
        runtime.ExecuteAction("start-fill");
        runtime.AdvanceSimulation(0.7);
        runtime.StopSimulation();
        var held = jug.Transform;
        runtime.AdvanceSimulation(0.4);
        var holds = jug.Transform.IsEqualApprox(held) && (stream is null || !stream.Visible);
        runtime.ResetSimulation();
        check(holds && jug.Transform.IsEqualApprox(initial) && Supported() && stream?.Visible == false,
            "coolant_stop_holds_and_reset_restores_supported_jug");
    }

    private void VerifyAssemblyLiftGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-12-assembly-lift", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var lift = root.GetNode<Node3D>("assembly_lift");
        var fixture = root.GetNode<Node3D>("lift_fixture");
        var deck = (MeshInstance3D)lift.FindChild("KIN_platform", true, false);
        var meshes = ReviewMeshes(lift);
        var driveArm = meshes.Single(mesh => mesh.Name == "KIN_scissor_0_-0_65_-1");
        var basePin = meshes.Single(mesh => mesh.Name == "LIFT_cylinder_base_clevis_pin");
        var tipPin = meshes.Single(mesh => mesh.Name == "LIFT_rod_end_clevis_pin");
        var cylinder = meshes.Single(mesh => mesh.Name == "LIFT_cylinder");
        var rod = meshes.Single(mesh => mesh.Name == "KIN_lift_rod");
        var hydraulicParts = meshes.Where(mesh => mesh == cylinder || mesh == rod
            || mesh.Name.ToString().StartsWith("LIFT_rod_end_clevis", StringComparison.Ordinal)
            || mesh.Name == "LIFT_drive_lug").ToArray();
        var hydraulicRest = hydraulicParts.Select(mesh => mesh.Transform).ToArray();
        var tipOnArm = driveArm.ToLocal(tipPin.GlobalPosition);
        // Check the delivered mesh caps, not a controller-owned state variable.
        // The imported cylinders' longest mesh axis defines their centreline.
        (Vector3 A, Vector3 B) Caps(MeshInstance3D member)
        {
            var bounds = member.GetAabb();
            var axis = (int)bounds.Size.MaxAxisIndex();
            var half = Vector3.Zero;
            half[axis] = bounds.Size[axis] * 0.5f;
            return (member.ToGlobal(bounds.GetCenter() - half), member.ToGlobal(bounds.GetCenter() + half));
        }
        var cylinderCaps = Caps(cylinder);
        var cylinderLength = cylinderCaps.A.DistanceTo(cylinderCaps.B);
        var glandRest = cylinderCaps.A.DistanceTo(basePin.GlobalPosition) > cylinderCaps.B.DistanceTo(basePin.GlobalPosition)
            ? cylinderCaps.A : cylinderCaps.B;
        GD.Print($"LIFT_HYDRAULIC_IMPORT arm={driveArm.Transform} body={cylinder.Transform} bodyBounds={cylinder.GetAabb()} rod={rod.Transform} rodBounds={rod.GetAabb()} base={basePin.GlobalPosition} tip={tipPin.GlobalPosition} gland={glandRest}");
        var pivots = meshes.Where(mesh => mesh.Name.ToString().StartsWith("LIFT_top_pivot_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("LIFT_bottom_pivot_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("KIN_mid_pivot_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("KIN_stage_pivot_", StringComparison.Ordinal)).ToArray();
        var followers = meshes.Where(mesh => mesh.Name.ToString().StartsWith("LIFT_retaining_washer_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("LIFT_lower_guide_roller_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("LIFT_upper_guide_roller_", StringComparison.Ordinal))
            .Select(mesh => (Mesh: mesh, Pivot: pivots.OrderBy(pivot => pivot.GlobalPosition.DistanceSquaredTo(mesh.GlobalPosition)).First()))
            .Select(pair => (pair.Mesh, pair.Pivot, Offset: pair.Mesh.GlobalPosition - pair.Pivot.GlobalPosition)).ToArray();
        bool Supported()
        {
            var load = ReviewBounds(fixture);
            var platform = ReviewBounds(deck);
            return MathF.Abs(load.Position.Y - platform.End.Y) < 0.001f
                && load.Position.X >= platform.Position.X && load.End.X <= platform.End.X
                && load.Position.Z >= platform.Position.Z && load.End.Z <= platform.End.Z;
        }
        GD.Print($"ASSEMBLY_LIFT_GEOMETRY deck={ReviewBounds(deck)} fixture={ReviewBounds(fixture)} followers={followers.Length}");
        check(Supported(), "assembly_fixture_supported_at_initial_deck");
        runtime.UsesExternalClock = false;
        FrameComposition(_mainCamera!, root, new Vector3(-11, 7, 12));
        var framed = true;
        var support = true;
        var attachments = followers.Length == 16;
        var hydraulicAttachment = true;
        var hydraulicCaps = true;
        foreach (var action in new[] { "raise-lift", "lower-lift" })
        {
            runtime.ExecuteAction(action);
            for (var sample = 0; sample < 120; sample++)
            {
                runtime.AdvanceSimulation(0.02);
                support &= Supported();
                var expectedTip = driveArm.ToGlobal(tipOnArm);
                hydraulicAttachment &= hydraulicParts.Where(mesh => mesh != cylinder && mesh != rod)
                    .All(mesh => mesh.GlobalPosition.DistanceTo(expectedTip) < 0.001f);
                var expectedGland = basePin.GlobalPosition
                    + (expectedTip - basePin.GlobalPosition).Normalized() * cylinderLength;
                bool MatchesCaps(MeshInstance3D member, Vector3 a, Vector3 b)
                {
                    var caps = Caps(member);
                    return (caps.A.DistanceTo(a) < 0.001f && caps.B.DistanceTo(b) < 0.001f)
                        || (caps.B.DistanceTo(a) < 0.001f && caps.A.DistanceTo(b) < 0.001f);
                }
                // Require actual barrel/rod cap contact at both named pins and
                // the gland throughout motion, rather than accepting loose rods.
                hydraulicCaps &= MatchesCaps(cylinder, basePin.GlobalPosition, expectedGland)
                    && MatchesCaps(rod, expectedGland, expectedTip);
                foreach (var mesh in ReviewMeshes(root))
                {
                    var bounds = ReviewBounds(mesh);
                    for (var corner = 0; corner < 8; corner++)
                        framed &= _mainCamera!.IsPositionInFrustum(bounds.Position + bounds.Size * new Vector3(
                            (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1));
                }
                attachments &= followers.All(pair => (pair.Mesh.GlobalPosition - pair.Pivot.GlobalPosition)
                    .DistanceTo(pair.Offset) < 0.001f);
            }
        }
        check(support, "assembly_fixture_supported_through_raise_and_lower");
        check(attachments, "assembly_guide_rollers_and_washers_follow_pins");
        check(framed, "assembly_full_travel_fits_initial_camera_frustum");
        check(hydraulicAttachment, "assembly_hydraulic_clevis_and_lug_follow_driven_arm");
        check(hydraulicCaps, "assembly_hydraulic_mesh_caps_stay_seated_through_travel");
        runtime.ResetSimulation();
        runtime.ExecuteAction("raise-lift");
        runtime.AdvanceSimulation(0.7);
        runtime.StopSimulation();
        var stoppedDeck = deck.Transform;
        var stoppedFixture = fixture.Transform;
        var stoppedHydraulics = hydraulicParts.Select(mesh => mesh.Transform).ToArray();
        runtime.AdvanceSimulation(0.5);
        check(deck.Transform.IsEqualApprox(stoppedDeck) && fixture.Transform.IsEqualApprox(stoppedFixture),
            "assembly_stop_holds_deck_and_fixture");
        check(hydraulicParts.Select((mesh, index) => mesh.Transform.IsEqualApprox(stoppedHydraulics[index])).All(held => held),
            "assembly_stop_holds_hydraulic_chain");
        runtime.ResetSimulation();
        check(Supported() && runtime.Points["bottom_limit"] is true && runtime.Points["top_limit"] is false,
            "assembly_reset_restores_supported_bottom_state");
        check(hydraulicParts.Select((mesh, index) => mesh.Transform.IsEqualApprox(hydraulicRest[index])).All(restored => restored),
            "assembly_reset_restores_hydraulic_authored_pose");
    }

    private void VerifyInboundToteGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-11-inbound-tote-stop", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var conveyor = root.GetNode<Node3D>("inbound_conveyor");
        var tote = root.GetNode<Node3D>("inbound_tote");
        var sensor = root.GetNode<Node3D>("scan_photoeye");
        var initial = tote.Transform;
        var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
        bool Supported()
        {
            var load = ReviewBounds(tote);
            return MathF.Abs(load.Position.Y - belt.End.Y) < 0.001f
                && load.Position.X >= belt.Position.X && load.End.X <= belt.End.X
                && load.Position.Z >= belt.Position.Z && load.End.Z <= belt.End.Z;
        }
        check(Supported(), "inbound_tote_rests_on_actual_belt_with_full_footprint");
        var sensorSolids = ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
            && !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var conveyorSolids = ReviewMeshes(conveyor).Where(mesh => !mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)).ToArray();
        check(sensorSolids.All(head => conveyorSolids.All(frame => !OrientedBoxesPenetrate(head, frame))),
            "inbound_sensor_solids_clear_conveyor_frame");
        var feet = ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString() is "TX_foot" or "RX_foot").ToArray();
        check(feet.Length == 2 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f),
            "inbound_photoeye_feet_grounded");
        runtime.UsesExternalClock = false; // Declared plant preview only, no PLC controller.
        runtime.ExecuteAction("start-cycle");
        runtime.AdvanceSimulation(0.7);
        runtime.StopSimulation();
        var stopped = tote.Transform;
        runtime.AdvanceSimulation(0.4);
        var holds = tote.Transform.IsEqualApprox(stopped);
        runtime.ResetSimulation();
        check(holds && tote.Transform.IsEqualApprox(initial), "inbound_stop_holds_and_reset_restores_tote");
        runtime.ExecuteAction("start-cycle");
        var support = true;
        var scanEnvelope = true;
        var sawScan = false;
        var beam = (MeshInstance3D)sensor.FindChild("KIN_beam*", true, false);
        for (var sample = 0; sample < 300; sample++)
        {
            runtime.AdvanceSimulation(0.02);
            support &= Supported();
            if (runtime.Points["tote_at_scanner"] is true)
            {
                sawScan = true;
                var load = ReviewBounds(tote);
                var center = ReviewBounds(beam).GetCenter();
                scanEnvelope &= center.X >= load.Position.X && center.X <= load.End.X
                    && center.Y > load.Position.Y && center.Y < load.End.Y;
            }
        }
        check(support && runtime.Points["cycle_complete"] is true, "inbound_full_cycle_keeps_tote_on_belt");
        check(sawScan && scanEnvelope, "inbound_scan_dwell_places_tote_in_optical_envelope");
        runtime.ResetSimulation();
    }

    private void VerifySelectorProjection(Action<bool, string> check)
    {
        // Inspect actual imported pointer/tick geometry through each declared
        // action, including the BOOL mode selector and a four-position dial.
        foreach (var sceneId in new[] { "lab-2-05-bay-light-selector", "lab-2-09-maintenance-beacon",
            "lab-2-15-fume-extractor", "lab-2-18-pallet-pickup", "lab-5-01-delayed-lamp", "lab-5-03-rotary-flasher", "lab-5-04-alternating-lamps", "lab-5-05-variable-flash-rate", "lab-5-06-running-light-tower", "lab-5-07-pedestrian-crossing", "lab-5-08-drawbridge-control", "lab-5-09-bag-indexing-conveyor" })
        {
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var runtime = _sceneRuntime!;
            var definition = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(scene => scene.Id == sceneId));
            // Match each selector to its own raw input and action. Scenes may
            // have several independent selectors; every dial must be checked.
            foreach (var item in definition.Equipment.Where(item => item.Type == "rotarySwitch"))
            {
                var equipment = _sceneCompositionRoot!.GetNode<Node3D>(item.Id);
                var config = item.Config;
                var selectorId = $"{sceneId}_{item.Id}";
                var count = config.GetProperty("positionCount").GetInt32();
                var action = config.GetProperty("action").GetString()!;
                var binding = definition.Simulation.GetProperty("pointBindings").EnumerateArray()
                    .Single(binding => binding.GetProperty("mode").GetString() == "selector"
                        && binding.GetProperty("equipmentId").GetString() == item.Id);
                var point = binding.GetProperty("point").GetString()!;
                var pivot = (Node3D)equipment.FindChild("KIN_selector_handle", true, false)!;
                var inlay = (Node3D)equipment.FindChild("SELECTOR_direction_inlay", true, false)!;
                var ticks = ReviewMeshes(equipment).Where(mesh => mesh.Name.ToString().StartsWith("POSITION_tick_", StringComparison.Ordinal)).ToArray();
                check(ticks.Length == count, $"{selectorId}_only_configured_detents_visible");
                var dialFace = ReviewBounds((MeshInstance3D)equipment.FindChild("SELECTOR_dial_plate", true, false)!).End.Z;
                check(ticks.All(tick => MathF.Abs(ReviewBounds(tick).Position.Z - dialFace) < 0.001f),
                    $"{selectorId}_tick_solids_rest_on_dial_face");
                var initial = inlay.GlobalTransform;
                var aligned = true;
                var onFace = true;
                var actionsAccepted = true;
                for (var step = 0; step <= count; step++)
                {
                    var ordinal = Convert.ToInt32(runtime.Points[point]);
                    var center = equipment.ToLocal(pivot.GlobalPosition);
                    var tip = equipment.ToLocal(inlay.GlobalPosition);
                    var direction = new Vector2(tip.X - center.X, tip.Y - center.Y).Normalized();
                    var tick = ticks.SingleOrDefault(mesh => mesh.Name == $"POSITION_tick_{ordinal}");
                    if (tick is null) aligned = false;
                    else
                    {
                        var target = equipment.ToLocal(tick.GlobalPosition) - center;
                        aligned &= direction.Dot(new Vector2(target.X, target.Y).Normalized()) > 0.999f;
                    }
                    // Authored inlay lies 51 mm forward of the pivot. The old Y
                    // rotation changed this depth and swung the handle off the face.
                    onFace &= MathF.Abs(tip.Z - center.Z - 0.051f) < 0.001f;
                    actionsAccepted &= runtime.ExecuteAction(action);
                }
                check(actionsAccepted && aligned, $"{selectorId}_pointer_aligns_with_current_input_detent");
                check(onFace, $"{selectorId}_pointer_stays_in_dial_plane");
                runtime.ResetSimulation();
                check(inlay.GlobalTransform.IsEqualApprox(initial), $"{selectorId}_reset_restores_initial_pointer");
            }
        }
    }

    private void VerifyGalleryGeometry(Action<bool, string> check)
    {
        AddMigratedScene("equipment-gallery", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        check(root.GetChildren().OfType<Node3D>().Where(node => ReviewMeshes(node).Length > 0)
            .All(node => ReviewBounds(node).Position.Y >= -0.005f), "gallery_equipment_clear_finished_floor");
        check(LogBoundsCandidates(root) == 0, "gallery_separate_equipment_bounds_clear");
        var instruments = new[] { "gallery_low_sensor", "gallery_transmitter" }.Select(id => root.GetNode<Node3D>(id)).ToArray();
        check(instruments.All(instrument =>
        {
            var standBase = instrument.FindChild("DISPLAY_STAND_base", true, false) as MeshInstance3D;
            var tips = ReviewMeshes(instrument).Where(mesh => mesh.Name.ToString().StartsWith("FORK_tip", StringComparison.Ordinal)
                || mesh.Name == "PROBE_tip_weight").ToArray();
            return standBase is not null && MathF.Abs(ReviewBounds(standBase).Position.Y) < 0.005f
                && tips.Length > 0 && tips.All(tip => ReviewBounds(tip).Position.Y >= 0.13f);
        }), "gallery_display_stands_grounded_and_probe_tips_clear");
        check(new[] { "gallery_pipe", "gallery_valve" }.All(id => MathF.Abs(ReviewBounds(root.GetNode<Node3D>(id)).Position.Y) < 0.005f),
            "gallery_pipe_and_valve_supports_grounded");
    }

    private static List<Vector2> ContactHull(IEnumerable<Vector2> contacts)
    {
        var points = contacts.Distinct().OrderBy(point => point.X).ThenBy(point => point.Y).ToArray();
        var hull = new List<Vector2>();
        foreach (var point in points)
        {
            while (hull.Count >= 2 && (hull[^1] - hull[^2]).Cross(point - hull[^1]) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        var lower = hull.Count;
        foreach (var point in points.Reverse().Skip(1))
        {
            while (hull.Count > lower && (hull[^1] - hull[^2]).Cross(point - hull[^1]) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    // Separating axes for transformed mesh boxes. Unlike world AABBs, this
    // keeps the diagonal belt motor separate from a carton centered on it.
    // It still bounds meshes rather than testing triangles or certifying fit.
    private static bool OrientedBoxesPenetrate(MeshInstance3D left, MeshInstance3D right, float allowance = .005f)
    {
        (Vector3 Center, Vector3[] Edges) Box(MeshInstance3D mesh)
        {
            var bounds = mesh.GetAabb();
            var transform = mesh.GlobalTransform;
            return (transform * bounds.GetCenter(), new[] {
                transform.Basis.X * bounds.Size.X / 2,
                transform.Basis.Y * bounds.Size.Y / 2,
                transform.Basis.Z * bounds.Size.Z / 2 });
        }
        var a = Box(left); var b = Box(right);
        var axes = new List<Vector3>();
        foreach (var box in new[] { a, b })
        {
            axes.Add(box.Edges[0].Cross(box.Edges[1]));
            axes.Add(box.Edges[1].Cross(box.Edges[2]));
            axes.Add(box.Edges[2].Cross(box.Edges[0]));
        }
        foreach (var edgeA in a.Edges)
        foreach (var edgeB in b.Edges) axes.Add(edgeA.Cross(edgeB));
        foreach (var axis in axes.Where(axis => axis.LengthSquared() > 1e-10f).Select(axis => axis.Normalized()))
        {
            var radius = a.Edges.Sum(edge => MathF.Abs(axis.Dot(edge))) + b.Edges.Sum(edge => MathF.Abs(axis.Dot(edge)));
            if (radius - MathF.Abs(axis.Dot(a.Center - b.Center)) <= allowance) return false;
        }
        return true;
    }
}
