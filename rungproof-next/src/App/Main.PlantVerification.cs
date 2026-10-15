using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyPlantMotion()
    {
        var passed = true;
        void Check(bool condition, string label)
        {
            passed &= condition;
            GD.Print($"PLANT_MOTION_CHECK {label}={condition}");
        }
        void Advance(int scans)
        {
            for (var scan = 0; scan < scans; scan++)
                _virtualController!.Advance(0.02, SampleVirtualControllerInputs, SampleVirtualControllerNumericInputs,
                    CommitVirtualControllerOutputs, CommitVirtualControllerNumericOutputs, _sceneRuntime!.AdvanceSimulation);
        }
        var contactId = 0;
        void SaveReviewProject(LadderProgram project, string sceneId, string name)
        {
            var document = new LadderEditorDocument();
            document.ReplaceFromProgram(project);
            document.SourceSceneId = sceneId;
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://.tools/plant-review-{name}.rpproj.json"), LadderEditorProjectJson.Save(document));
        }
        LadderNode Contact(string name, bool nc = false) => new($"contact-{++contactId}", LadderNodeKind.Contact, name, nc);
        LadderNode Series(string id, params LadderNode[] nodes) => new(id, LadderNodeKind.Series, Children: nodes);
        try
        {
            if (_visualSceneReview) VerifyBaseConveyorReviewClock(Check);
            AddMigratedScene("conveyor-cell", _candidateCatalog!, _mainCamera!, false, false);
            var resetDrive = _sceneCompositionRoot!.FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
            var resetDrum = (Node3D)_sceneCompositionRoot.FindChild("KIN_drive_drum", true, false);
            var resetDrumHome = resetDrum.Transform;
            var resetBelt = (MeshInstance3D)_sceneCompositionRoot.FindChild("KIN_belt_surface", true, false);
            var resetMaterial = (StandardMaterial3D)resetBelt.GetSurfaceOverrideMaterial(0);
            resetDrive.RunCommand = true;
            resetDrive.ApplyPlantTravel(1.3f, .65f);
            Check(resetDrive.Running && !resetDrum.Transform.IsEqualApprox(resetDrumHome)
                && resetMaterial.Uv1Offset.Length() > .1f, "conveyor_reset_fixture_has_actual_integrated_travel");
            _sceneRuntime!.ResetSimulation();
            Check(!resetDrive.RunCommand && !resetDrive.Running && resetDrive.ActualSpeedMps == 0
                && resetDrum.Transform.IsEqualApprox(resetDrumHome) && resetMaterial.Uv1Offset == Vector3.Zero,
                "conveyor_reset_restores_speed_drum_transform_and_belt_travel_immediately");
            AddMigratedScene("scene-2-conveyor-pusher", _candidateCatalog!, _mainCamera!, false, false);
            PlcVariable Tag(string name, PlcVariableRole role, bool initial = false) =>
                new(name, PlcVariableType.Bool, role, initial, name);
            var program = new LadderProgram(1, "review-scene2", "Scene 2 reference sequence", "LD", TimeSpan.FromMilliseconds(20),
                [Tag("part_at_pusher", PlcVariableRole.Input), Tag("pusher_extended", PlcVariableRole.Input),
                    Tag("pusher_retracted", PlcVariableRole.Input, true), Tag("pusher_extend", PlcVariableRole.Output),
                    Tag("conveyor_running", PlcVariableRole.Output)],
                [new("set-extend", "Set at photoeye", Series("extend-permit", Contact("part_at_pusher"), Contact("pusher_retracted")),
                    new("set", "pusher_extend", LadderCoilMode.Set)),
                 new("reset-extend", "Reset wins at extended limit", Contact("pusher_extended"), new("reset", "pusher_extend", LadderCoilMode.Reset)),
                 new("conveyor", "Retracted and photoeye clear", Series("conveyor-permit", Contact("pusher_retracted"), Contact("part_at_pusher", true), Contact("pusher_extend", true)),
                     new("motor", "conveyor_running"))]);
            EnableVirtualControllerProgram(program);
            if (_virtualController is null) throw new InvalidOperationException(string.Join("; ", LadderCompiler.Validate(program).Select(issue => issue.Message)));
            SaveReviewProject(program, "scene-2-conveyor-pusher", "scene2");
            RunActiveController();
            var photoeyeSeen = false;
            var extensionSeen = false;
            var extendedSeen = false;
            var carton = _sceneCompositionRoot!.GetNode<Node3D>("scene2_product");
            var stagedCarton = carton.Transform;
            var receiverDeck = ReviewBounds((MeshInstance3D)_sceneCompositionRoot.GetNode("scene2_receiver").FindChild("BENCH_top", true, false));
            var transferSeen = false; var transferVisible = true; var receivedSeen = false;
            var plate = (MeshInstance3D)_sceneCompositionRoot.GetNode("scene2_pusher").FindChild("KIN_pusher_plate", true, false);
            var contactSeen = false; var plateContact = true;
            var receivedHeld = true; var reloadSeen = false; var lastCompleted = 0L;
            var sensor = _sceneCompositionRoot.GetNode<Node3D>("scene2_photoeye");
            var txLens = (MeshInstance3D)sensor.FindChild("TX_lens", true, false);
            var rxLens = (MeshInstance3D)sensor.FindChild("RX_lens", true, false);
            var opticalMatches = true;
            Vector3? releasedPosition = null;
            for (var scan = 0; scan < 650; scan++)
            {
                Advance(1);
                photoeyeSeen |= _sceneRuntime!.Points["part_at_pusher"] is true;
                extensionSeen |= Convert.ToDouble(_sceneRuntime.Points["pusher_position"]) > 0;
                extendedSeen |= _sceneRuntime.Points["pusher_extended"] is true;
                var opticalHit = LineHitsBounds(ReviewBounds(txLens).GetCenter(), ReviewBounds(rxLens).GetCenter(), ReviewBounds(carton).Grow(0.001f));
                var opticalMatch = opticalHit == (_sceneRuntime.Points["part_at_pusher"] is true);
                opticalMatches &= opticalMatch;
                var completed = Convert.ToInt64(_sceneRuntime.Points["parts_completed"]);
                if (completed > lastCompleted)
                {
                    transferSeen = true;
                    transferVisible &= carton.Visible;
                    releasedPosition = carton.Position;
                }
                if (releasedPosition is { } released)
                {
                    // The one rendered carton is recycled only at the plant's
                    // actual reload boundary, never at its transfer threshold.
                    if (carton.Transform.IsEqualApprox(stagedCarton))
                    {
                        reloadSeen |= carton.Visible;
                        releasedPosition = null;
                    }
                    else
                    {
                        receivedHeld &= carton.Visible && MathF.Abs(carton.Position.X - released.X) < 0.001f
                            && carton.Position.Z >= released.Z - 0.001f;
                        releasedPosition = carton.Position;
                        if (_sceneRuntime.Points["pusher_extended"] is true)
                        {
                            var load = ReviewBounds(carton);
                            receivedSeen |= MathF.Abs(load.Position.Y - receiverDeck.End.Y) < 0.001f
                                && load.Position.X >= receiverDeck.Position.X && load.End.X <= receiverDeck.End.X
                                && load.Position.Z >= receiverDeck.Position.Z && load.End.Z <= receiverDeck.End.Z;
                        }
                    }
                }
                lastCompleted = completed;
                if (_sceneRuntime.Points["pusher_extend"] is true
                    && (_sceneRuntime.Points["part_at_pusher"] is true || releasedPosition.HasValue))
                {
                    var face = ReviewBounds(plate); var load = ReviewBounds(carton);
                    contactSeen = true;
                    plateContact &= MathF.Abs(face.End.Z - load.Position.Z) < 0.002f
                        && face.Position.X < load.End.X && face.End.X > load.Position.X
                        && face.Position.Y < load.End.Y && face.End.Y > load.Position.Y;
                }
            }
            Check(contactSeen && plateContact, "scene2_plate_contacts_carton_through_actual_ladder_extension");
            Check(opticalMatches, "scene2_actual_ladder_photoeye_matches_carton_optical_path_through_repeat_cycles");
            Check(transferSeen && transferVisible, "scene2_carton_remains_visible_at_canonical_transfer_threshold");
            Check(receivedSeen && receivedHeld, "scene2_full_stroke_carton_seated_on_receiver_and_not_dragged_back");
            Check(reloadSeen, "scene2_visual_carton_recycled_only_when_plant_reloads");
            Check(photoeyeSeen, "scene2_photoeye_from_package_motion");
            Check(extensionSeen && extendedSeen, "scene2_stroke_and_limit_feedback");
            Check(Convert.ToInt64(_sceneRuntime!.Points["parts_completed"]) >= 2, "scene2_repeat_transfer_with_actual_ladder_scans");
            for (var scan = 0; scan < 250 && (Convert.ToDouble(_sceneRuntime.Points["pusher_position"]) <= 0
                || Convert.ToDouble(_sceneRuntime.Points["pusher_position"]) >= 75); scan++) Advance(1);
            Check(Convert.ToDouble(_sceneRuntime.Points["pusher_position"]) is > 0 and < 75, "scene2_stop_fixture_is_mid_stroke");
            StopActiveController();
            var stoppedStroke = _sceneRuntime.Points["pusher_position"];
            Advance(40);
            Check(Equals(stoppedStroke, _sceneRuntime.Points["pusher_position"]), "stopped_controller_holds_plant");
            ResetActiveController();
            Check(_sceneRuntime.Points["pusher_retracted"] is true && _sceneRuntime.Points["part_at_pusher"] is false
                && Convert.ToInt64(_sceneRuntime.Points["parts_completed"]) == 0, "scene2_reset_restores_initial_feedback");
            Check(carton.Visible && carton.Transform.IsEqualApprox(stagedCarton), "scene2_reset_restores_visible_staged_carton");

            // Native inspection fixture: same actual ladder path, but hold the
            // solenoid after transfer so endpoint views do not race the reload.
            // It is an ignored QA project, not an authored production solution.
            SaveReviewProject(program with { Id = "review-scene2-held-transfer", Name = "Scene 2 held transfer inspection",
                Networks = program.Networks.Where(network => network.Id != "reset-extend").ToArray() },
                "scene-2-conveyor-pusher", "scene2-held-transfer");

            if (_visualSceneReview) VerifyConveyorReviewClock(Check);

            AddMigratedScene("tank-level", _candidateCatalog!, _mainCamera!, false, false);
            var tankProgram = new LadderProgram(1, "review-tank", "Tank plant exercise", "LD", TimeSpan.FromMilliseconds(20),
                [new("permit", PlcVariableType.Bool, PlcVariableRole.Memory, true), Tag("inlet_pump_run", PlcVariableRole.Output),
                 Tag("drain_valve_open", PlcVariableRole.Output),
                 new("level_transmitter", PlcVariableType.Real, PlcVariableRole.Input, 10.72, "level_transmitter")],
                [new("pump", "Inlet enabled", Contact("permit"), new("pump-coil", "inlet_pump_run"))]);
            EnableVirtualControllerProgram(tankProgram);
            SaveReviewProject(tankProgram, "tank-level", "tank");
            var initial = Convert.ToDouble(_sceneRuntime!.Points["tank_level"]);
            RunActiveController();
            Advance(100);
            var filled = Convert.ToDouble(_sceneRuntime.Points["tank_level"]);
            Check(Math.Abs(filled - (initial + 14)) < 1e-6, "tank_fills_from_ladder_output_without_hidden_local_run");
            Check(Math.Abs(Convert.ToDouble(_sceneRuntime.Points["level_transmitter"]) - (4 + 0.16 * filled)) < 1e-6,
                "tank_analog_feedback_tracks_actual_level");
            ApplyVirtualForce("inlet_pump_run", false);
            ApplyVirtualForce("drain_valve_open", true);
            Advance(100);
            var drained = Convert.ToDouble(_sceneRuntime.Points["tank_level"]);
            Check(Math.Abs(drained - (filled - 9)) < 1e-6, "tank_drain_output_changes_plant");
            StopActiveController();
            Advance(100);
            Check(Math.Abs(Convert.ToDouble(_sceneRuntime.Points["tank_level"]) - drained) < 1e-6,
                "tank_stopped_controller_holds_level");
            RemoveVirtualForce("inlet_pump_run");
            RemoveVirtualForce("drain_valve_open");
            RunActiveController();
            Advance(1000);
            Check(Convert.ToDouble(_sceneRuntime.Points["tank_level"]) == 100 && _sceneRuntime.Points["high_level_switch"] is true,
                "tank_high_limit_and_fill_saturation");
            ApplyVirtualForce("inlet_pump_run", false);
            ApplyVirtualForce("drain_valve_open", true);
            Advance(1500);
            Check(Convert.ToDouble(_sceneRuntime.Points["tank_level"]) == 0 && _sceneRuntime.Points["low_level_switch"] is true,
                "tank_low_limit_and_drain_saturation");
            ResetActiveController();
            Check(Math.Abs(Convert.ToDouble(_sceneRuntime.Points["tank_level"]) - initial) < 1e-6, "tank_reset_restores_level");

            VerifyPalletizerWorkflow(Check);
            GD.Print($"PLANT_MOTION_VERIFY {(passed ? "PASS" : "FAIL")} offline-only; no PLC transport");
            GetTree().Quit(passed ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"PLANT_MOTION_VERIFY FAIL {exception}");
            GetTree().Quit(1);
        }
    }
}
