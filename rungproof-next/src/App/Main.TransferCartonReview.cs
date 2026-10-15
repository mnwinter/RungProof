using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyNumericSceneOutputTypes(Action<bool, string> check)
    {
        using var definition = JsonDocument.Parse("""
            {"type":"booleanPanel","points":[
              {"name":"integer","type":"INT","owner":"PLC","initial":0,"role":"output"},
              {"name":"double_integer","type":"DINT","owner":"PLC","initial":0,"role":"output"},
              {"name":"real","type":"REAL","owner":"PLC","initial":0.0,"role":"output"}]}
            """);
        var root = new Node3D();
        var runtime = new SceneSimulationRuntime(definition.RootElement, root);
        try
        {
            runtime.ResetSimulation();
            runtime.CommitVirtualControllerNumericOutputs(new Dictionary<string, double>
            { ["integer"] = double.MaxValue, ["double_integer"] = double.MinValue, ["real"] = 1.25 });
            var upperInt = Equals(runtime.Points["integer"], (long)short.MaxValue);
            var lowerDInt = Equals(runtime.Points["double_integer"], (long)int.MinValue);
            check(runtime.Points["real"] is double number && number == 1.25,
                "scene_real_output_remains_double_without_integer_truncation");
            runtime.CommitVirtualControllerNumericOutputs(new Dictionary<string, double>
            { ["integer"] = double.MinValue, ["double_integer"] = double.MaxValue });
            var lowerInt = Equals(runtime.Points["integer"], (long)short.MinValue);
            var upperDInt = Equals(runtime.Points["double_integer"], (long)int.MaxValue);
            runtime.CommitVirtualControllerNumericOutputs(new Dictionary<string, double>
            { ["integer"] = 1.9, ["double_integer"] = -1.9 });
            check(upperInt && lowerInt && Equals(runtime.Points["integer"], 1L),
                "scene_int_output_is_boxed_integer_truncated_and_range_clamped");
            check(lowerDInt && upperDInt && Equals(runtime.Points["double_integer"], -1L),
                "scene_dint_output_is_boxed_integer_truncated_and_range_clamped");
        }
        finally { runtime.Free(); root.Free(); }
    }

    private void VerifyPalletCountReadoutWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-9-11-pallet-counting", _candidateCatalog!, _mainCamera!, false, false);
        var display = _sceneCompositionRoot!.GetNode<Node3D>("training_accessory_6");
        var readout = display.GetNodeOrNull<Label3D>("NumericReadout");
        check(readout?.Text == "COUNT\n0"
            && display.FindChild("COUNT_DISPLAY_static_legend", true, false) is MeshInstance3D { Visible: false },
            "pallet_count_live_readout_replaces_static_legend_at_initial_zero");
        AuthoredDemoLadderPrograms.TryCreate("lab-9-11-pallet-counting", out var document);
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var runtime = _sceneRuntime!;
            _virtualController!.Run();
            runtime.ExecuteAction("toggle-count_request");
            _PhysicsProcess(0.04);
            runtime.ExecuteAction("toggle-pallet_detected");
            _PhysicsProcess(0.04);
            GD.Print($"PALLET_COUNT_READOUT_PROBE type={runtime.Points["pallet_count"]?.GetType().Name} value={runtime.Points["pallet_count"]} text={readout?.Text.Replace('\n', '|')}");
            check(Equals(runtime.Points["pallet_count"], 0L) && readout?.Text == "COUNT\n0",
                "pallet_count_invalid_type_event_is_not_published_as_a_count");
            runtime.ExecuteAction("toggle-pallet_type_valid");
            _PhysicsProcess(0.04);
            check(Equals(runtime.Points["pallet_count"], 0L) && readout?.Text == "COUNT\n0",
                "pallet_count_later_permissive_does_not_recount_held_detection");
            var correct = true;
            for (var count = 1; count <= 5; count++)
            {
                runtime.ExecuteAction("toggle-pallet_detected"); // release old detection
                _PhysicsProcess(0.04);
                runtime.ExecuteAction("toggle-pallet_detected"); // next detection edge
                _PhysicsProcess(0.04);
                correct &= Equals(runtime.Points["pallet_count"], (long)count)
                    && readout?.Text == $"COUNT\n{count}"
                    && Equals(runtime.Points["pallet_count_valid"], count == 5);
            }
            check(correct, "pallet_count_five_valid_scene_edges_publish_same_scan_count_and_readout");
            _PhysicsProcess(1.0);
            check(Equals(runtime.Points["pallet_count"], 5L) && readout?.Text == "COUNT\n5",
                "pallet_count_held_detection_keeps_five_on_display");
            runtime.ExecuteAction("toggle-pallet_type_valid");
            _PhysicsProcess(0.04);
            check(runtime.Points["pallet_count_valid"] is false
                && Equals(runtime.Points["pallet_count"], 5L) && readout?.Text == "COUNT\n5",
                "pallet_count_permissive_loss_clears_validity_without_erasing_count");
            CommitVirtualControllerSnapshot(_virtualController.Stop());
            check(Equals(runtime.Points["pallet_count"], 0L) && readout?.Text == "COUNT\n0"
                && _virtualController.Snapshot.Counters["batch_count"].Accumulated == 5,
                "pallet_count_stop_clears_numeric_output_image_and_retains_counter_memory");
            _virtualController.Run();
            _PhysicsProcess(0.04);
            check(Equals(runtime.Points["pallet_count"], 5L) && readout?.Text == "COUNT\n5",
                "pallet_count_run_republishes_retained_counter_without_new_event");
            runtime.ResetSimulation();
            CommitVirtualControllerSnapshot(_virtualController.Reset());
            check(Equals(runtime.Points["pallet_count"], 0L) && readout?.Text == "COUNT\n0"
                && runtime.Points["pallet_detected"] is false && runtime.Points["pallet_count_valid"] is false
                && _virtualController.Snapshot.Counters["batch_count"].Accumulated == 0,
                "pallet_count_reset_clears_readout_inputs_outputs_and_memory");
        }
        finally { DisableVirtualController(); }
    }

    private void VerifyPalletCountPropGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-9-11-pallet-counting", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var fixture = root.GetNode<Node3D>("training_accessory_5");
        var display = root.GetNode<Node3D>("training_accessory_6");
        var fixtureMeshes = ReviewMeshes(fixture);
        var displayMeshes = ReviewMeshes(display);
        check(fixtureMeshes.Count(mesh => mesh.Name.ToString().StartsWith("PROFILE_head_", StringComparison.Ordinal)) == 4
            && fixture.FindChild("PROFILE_crossbeam", true, false) is not null
            && !fixtureMeshes.Any(mesh => mesh.Name.ToString().Contains("PALLET", StringComparison.OrdinalIgnoreCase)),
            "pallet_count_fixture_is_optical_profile_geometry_not_pallet");
        check(display.FindChild("COUNT_DISPLAY_screen", true, false) is not null
            && display.FindChild("COUNT_DISPLAY_static_legend", true, false) is not null
            && display.FindChild("KIN_bottom_bar", true, false) is null,
            "pallet_count_display_is_static_readout_not_shutter");
        var feet = fixtureMeshes.Where(mesh => mesh.Name.ToString().StartsWith("PROFILE_foot_", StringComparison.Ordinal)).ToArray();
        var posts = fixtureMeshes.Where(mesh => mesh.Name.ToString().StartsWith("PROFILE_post_", StringComparison.Ordinal)).ToArray();
        check(feet.Length == 2 && posts.Length == 2
            && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f)
            && posts.All(post => feet.Any(foot => ReviewBounds(post).Intersects(ReviewBounds(foot).Grow(0.001f)))),
            "pallet_count_profile_posts_seated_on_grounded_feet");
        var baseMesh = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
        var mast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
        var housing = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
        check(MathF.Abs(ReviewBounds(baseMesh).Position.Y) < 0.001f
            && ReviewBounds(mast).Intersects(ReviewBounds(baseMesh).Grow(0.001f))
            && ReviewBounds(mast).Intersects(ReviewBounds(housing)),
            "pallet_count_readout_mast_seated_on_base_and_housing");
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(a, b);
        }
        var otherSolids = root.GetChildren().OfType<Node3D>().Where(node => node != fixture && node != display)
            .SelectMany(ReviewMeshes).ToArray();
        foreach (var part in fixtureMeshes)
            foreach (var other in otherSolids.Where(other => !Clear(part, other)))
                GD.Print($"PALLET_PROFILE_COLLISION {part.Name} {other.GetParent().Name}/{other.Name} {ReviewBounds(part)} {ReviewBounds(other)}");
        check(fixtureMeshes.All(part => otherSolids.All(other => Clear(part, other))),
            "pallet_count_profile_fixture_clear_of_conveyor_carton_and_other_equipment");
        check(displayMeshes.All(part => otherSolids.Concat(fixtureMeshes).All(other => Clear(part, other))),
            "pallet_count_readout_clear_of_separate_equipment");
    }

    private void VerifyCutLengthDisplayGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-4-12-cable-cut-length", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var display = root.GetNode<Node3D>("training_accessory_8");
        check(display.FindChild("LENGTH_DISPLAY_screen", true, false) is not null
            && display.FindChild("LENGTH_DISPLAY_static_legend", true, false) is not null
            && display.FindChild("KIN_bottom_bar", true, false) is null,
            "cut_length_display_is_readout_not_shutter");
        var baseMesh = (MeshInstance3D)display.FindChild("LENGTH_DISPLAY_base", true, false);
        var mast = (MeshInstance3D)display.FindChild("LENGTH_DISPLAY_mast", true, false);
        var housing = (MeshInstance3D)display.FindChild("LENGTH_DISPLAY_housing", true, false);
        check(MathF.Abs(ReviewBounds(baseMesh).Position.Y) < 0.001f
            && ReviewBounds(mast).Intersects(ReviewBounds(baseMesh).Grow(0.001f))
            && ReviewBounds(mast).Intersects(ReviewBounds(housing)),
            "cut_length_display_grounded_and_housing_supported");
        var others = root.GetChildren().OfType<Node3D>().Where(node => node != display).SelectMany(ReviewMeshes).ToArray();
        bool Clear(MeshInstance3D part, MeshInstance3D other)
        {
            var overlap = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(part, other);
        }
        check(ReviewMeshes(display).All(part => others.All(other => Clear(part, other))),
            "cut_length_display_clear_of_separate_equipment");
        check(root.GetNode<Node3D>("switch_9").FindChild("OperatorFaceLabel", true, false)
                is Label3D plate && plate.Text == "START"
            && root.GetNodeOrNull<Node3D>("switch_10") is null && root.GetNodeOrNull<Node3D>("switch_11") is null,
            "cut_length_start_plate_replaces_manual_feedback_switches");
    }

    private void VerifyStaticTrainingReadouts(Action<bool, string> check)
    {
        foreach (var (sceneId, displayId) in new[]
        {
            ("lab-6-08-hand-dryer", "training_accessory_6"),
            ("lab-6-07-luggage-weight-sort", "training_accessory_6")
        })
        {
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var display = root.GetNode<Node3D>(displayId);
            var parts = ReviewMeshes(display);
            check(display.FindChild("STATIC_READOUT_screen", true, false) is not null
                && display.FindChild("STATIC_READOUT_static_legend", true, false) is not null
                && display.FindChild("KIN_bottom_bar", true, false) is null
                && display.FindChild("KIN_selector_handle", true, false) is null,
                $"{sceneId}_static_readout_identity_without_shutter_or_selector");
            var baseMesh = (MeshInstance3D)display.FindChild("STATIC_READOUT_base", true, false);
            var mast = (MeshInstance3D)display.FindChild("STATIC_READOUT_mast", true, false);
            var housing = (MeshInstance3D)display.FindChild("STATIC_READOUT_housing", true, false);
            check(MathF.Abs(ReviewBounds(baseMesh).Position.Y) < 0.001f
                && ReviewBounds(mast).Intersects(ReviewBounds(baseMesh).Grow(0.001f))
                && ReviewBounds(mast).Intersects(ReviewBounds(housing)),
                $"{sceneId}_static_readout_grounded_and_supported");
            var others = root.GetChildren().OfType<Node3D>().Where(node => node != display).SelectMany(ReviewMeshes).ToArray();
            bool Clear(MeshInstance3D part, MeshInstance3D other)
            {
                var overlap = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
                return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(part, other);
            }
            foreach (var part in parts)
                foreach (var other in others.Where(other => !Clear(part, other)))
                    GD.Print($"STATIC_READOUT_COLLISION {sceneId} {part.Name} {other.GetParent().Name}/{other.Name} {ReviewBounds(part)} {ReviewBounds(other)}");
            check(parts.All(part => others.All(other => Clear(part, other))),
                $"{sceneId}_static_readout_clear_of_separate_equipment");
        }
    }

    private void VerifyArithmeticValidityLayouts(Action<bool, string> check)
    {
        foreach (var (sceneId, firstId, firstLabel, secondLabel) in new[]
        {
            ("lab-9-01-sum-function", "switch_1", "A VALID", "B VALID"),
            ("lab-9-02-product-function", "switch_1", "A VALID", "B VALID"),
            ("lab-9-04-function-selector", "switch_0", "OPERANDS", "FUNC VALID")
        })
        {
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            // These scenes retain Boolean validity inputs. Neither a CNC nor
            // a bearing mislabeled as a calculation panel participates in them.
            check(root.GetNodeOrNull<Node3D>("machine_0") is null
                && root.GetNodeOrNull<Node3D>("machine_1") is null
                && root.GetNodeOrNull<Node3D>("training_accessory_3") is null,
                $"{sceneId}_unrelated_cnc_and_bearing_props_removed");
            check(new[] { (firstId, firstLabel), ("switch_5", secondLabel), ("switch_6", "CALCULATE") }
                .All(item => root.GetNode<Node3D>(item.Item1).FindChild("OperatorFaceLabel", true, false)
                    is Label3D label && label.Text == item.Item2),
                $"{sceneId}_manual_input_plates_match_validity_and_request");
        }
    }

    private void VerifyArithmeticNumericWorkflows(Action<bool, string> check)
    {
        foreach (var (sceneId, referenceName, inputPrefix, result, label, expected) in new[]
        {
            ("lab-9-01-sum-function", "sum", "operand", "sum_result", "SUM", 7L),
            ("lab-9-02-product-function", "product", "factor", "product_result", "PRODUCT", 10L)
        })
        {
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var runtime = _sceneRuntime!;
            Label3D Readout(string id) => root.GetNode<Node3D>(id).GetNode<Label3D>("NumericReadout");
            var displays = new[] { "numeric_display_0", "numeric_display_1", "training_accessory_4" };
            check(displays.All(id => Readout(id).Text.EndsWith("\n0", StringComparison.Ordinal))
                && root.GetNode<Node3D>("training_accessory_4").FindChild("STATIC_READOUT_static_legend", true, false) is null,
                $"{sceneId}_initial_live_numeric_readouts");
            bool Clear(MeshInstance3D a, MeshInstance3D b)
            {
                var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
                return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(a, b);
            }
            var equipment = root.GetChildren().OfType<Node3D>().ToArray();
            check(equipment.All(a => equipment.Where(b => b != a)
                .All(b => ReviewMeshes(a).All(am => ReviewMeshes(b).All(bm => Clear(am, bm))))),
                $"{sceneId}_separate_equipment_clearance");
            var reference = LadderEditorProjectJson.Load(FileAccess.GetFileAsString(
                $"res://programs/examples/09-{referenceName}-function-reference.rpproj.json"));
            check(reference.IsReadable, $"{sceneId}_reference_readable");
            if (reference.Document is null) throw new InvalidOperationException($"Unreadable arithmetic reference: {sceneId}");
            EnableVirtualControllerProgram(reference.Document.BuildProgram());
            try
            {
                _virtualController!.Run();
                void Scan() => _PhysicsProcess(0.04);
                void Toggle(string point) { runtime.ExecuteAction("toggle-" + point); Scan(); }
                var a = inputPrefix + "_a";
                var b = inputPrefix + "_b";
                runtime.ExecuteAction("cycle-" + a); runtime.ExecuteAction("cycle-" + a);
                for (var i = 0; i < 3; i++) runtime.ExecuteAction("cycle-" + b);
                Scan();
                check(Readout(displays[0]).Text == "A NEXT\n2" && Readout(displays[1]).Text == "B NEXT\n5",
                    $"{sceneId}_pc_numeric_cycles_update_readouts");
                Toggle("calculate_request"); Toggle(a + "_valid");
                check(runtime.Points[result + "_valid"] is false && Equals(runtime.Points[result], 0L),
                    $"{sceneId}_missing_b_valid_blocks_calculation");
                Toggle(b + "_valid");
                check(runtime.Points[result + "_valid"] is true && Equals(runtime.Points[result], expected)
                    && Readout(displays[2]).Text == $"{label}\n{expected}",
                    $"{sceneId}_valid_calculation_publishes_same_scan_result");
                runtime.ExecuteAction("cycle-" + a); Scan(); // A: 2 -> 5
                var updated = referenceName == "sum" ? 10L : 25L;
                check(Equals(runtime.Points[result], updated) && Readout(displays[2]).Text == $"{label}\n{updated}",
                    $"{sceneId}_changed_operand_recalculates_live_result");
                Toggle(a + "_valid");
                check(runtime.Points[result + "_valid"] is false && Equals(runtime.Points[result], updated),
                    $"{sceneId}_permissive_loss_clears_validity_retains_result");
                CommitVirtualControllerSnapshot(_virtualController.Stop());
                check(Readout(displays[2]).Text == $"{label}\n0" && runtime.Points[result + "_valid"] is false,
                    $"{sceneId}_stop_clears_output_image");
                _virtualController.Run(); Scan();
                check(Equals(runtime.Points[result], 0L) && runtime.Points[result + "_valid"] is false,
                    $"{sceneId}_restart_with_missing_permissive_keeps_result_zero");
                Toggle(a + "_valid");
                check(Equals(runtime.Points[result], updated) && runtime.Points[result + "_valid"] is true,
                    $"{sceneId}_restored_permissive_recalculates");
                runtime.ResetSimulation(); CommitVirtualControllerSnapshot(_virtualController.Reset());
                check(displays.All(id => Readout(id).Text.EndsWith("\n0", StringComparison.Ordinal))
                    && runtime.Points["calculate_request"] is false && runtime.Points[result + "_valid"] is false,
                    $"{sceneId}_reset_clears_manual_inputs_and_numeric_values");
            }
            finally { DisableVirtualController(); }
        }
    }

    private void VerifyFunctionSelectorWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-9-04-function-selector", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        Label3D Readout(int i) => root.GetNode<Node3D>($"numeric_display_{i}").GetNode<Label3D>("NumericReadout");
        check(Enumerable.Range(0, 4).All(i => Readout(i).Text.EndsWith("\n0", StringComparison.Ordinal))
            && root.GetNodeOrNull<Node3D>("training_accessory_4") is null,
            "function_selector_initial_live_readouts_replace_static_legend");
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(a, b);
        }
        var equipment = root.GetChildren().OfType<Node3D>().ToArray();
        check(equipment.All(a => equipment.Where(b => b != a)
            .All(b => ReviewMeshes(a).All(am => ReviewMeshes(b).All(bm => Clear(am, bm))))),
            "function_selector_separate_equipment_clearance");
        check(Enumerable.Range(0, 4).All(i =>
        {
            var display = root.GetNode<Node3D>($"numeric_display_{i}");
            var foot = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
            var mast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
            var housing = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
            return MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f
                && ReviewBounds(mast).Intersects(ReviewBounds(foot).Grow(0.001f))
                && ReviewBounds(mast).Intersects(ReviewBounds(housing));
        }), "function_selector_readout_masts_grounded_and_supported");
        var reference = LadderEditorProjectJson.Load(FileAccess.GetFileAsString(
            "res://programs/examples/09-function-selector-reference.rpproj.json"));
        check(reference.IsReadable, "function_selector_opt_in_reference_readable");
        if (reference.Document is null) throw new InvalidOperationException("Function selector reference is unreadable.");
        EnableVirtualControllerProgram(reference.Document.BuildProgram());
        try
        {
            _virtualController!.Run();
            void Scan() => _PhysicsProcess(0.04);
            void Toggle(string point) { runtime.ExecuteAction("toggle-" + point); Scan(); }
            void NextChoice() { runtime.ExecuteAction("cycle-function_choice"); Scan(); }
            bool Result(long value, bool valid) => Equals(runtime.Points["selected_result"], value)
                && Equals(runtime.Points["selected_result_valid"], valid) && Readout(3).Text == $"RESULT\n{value}";
            runtime.ExecuteAction("cycle-operand_a"); runtime.ExecuteAction("cycle-operand_a");
            for (var i = 0; i < 3; i++) runtime.ExecuteAction("cycle-operand_b");
            Scan();
            check(Readout(0).Text == "A NEXT\n2" && Readout(1).Text == "B NEXT\n5",
                "function_selector_manual_operands_publish_live_values");
            Toggle("operand_set_valid"); Toggle("calculate_request"); NextChoice();
            check(Result(0L, false), "function_selector_missing_manual_function_valid_blocks_call");
            Toggle("function_select_valid");
            check(Result(7L, true) && Readout(2).Text == "FUNC NEXT\n1",
                "function_selector_choice_one_calls_sum_only");
            NextChoice();
            check(Result(10L, true) && Readout(2).Text == "FUNC NEXT\n2",
                "function_selector_choice_two_calls_product_only");
            NextChoice(); // 99 is deliberately unsupported, even with FUNC VALID true.
            check(Result(10L, false) && Readout(2).Text == "FUNC NEXT\n99",
                "function_selector_invalid_choice_clears_validity_retains_last_result");
            NextChoice();
            check(Result(10L, false) && Readout(2).Text == "FUNC NEXT\n0",
                "function_selector_zero_choice_is_not_authorized_by_manual_validity");
            NextChoice(); runtime.ExecuteAction("cycle-operand_a"); Scan(); // A=5, B=5, SUM=10.
            check(Result(10L, true), "function_selector_changed_operand_recalculates_selected_path");
            Toggle("calculate_request");
            check(Result(10L, false), "function_selector_request_loss_clears_validity");
            Toggle("calculate_request"); Toggle("operand_set_valid");
            check(Result(10L, false), "function_selector_operand_permissive_loss_clears_validity");
            CommitVirtualControllerSnapshot(_virtualController.Stop());
            check(Result(0L, false), "function_selector_stop_clears_numeric_output_image");
            _virtualController.Run(); Scan();
            check(Result(0L, false), "function_selector_restart_missing_permissive_keeps_result_zero");
            Toggle("operand_set_valid");
            check(Result(10L, true), "function_selector_restored_permissive_recalculates");
            runtime.ResetSimulation(); CommitVirtualControllerSnapshot(_virtualController.Reset());
            check(Enumerable.Range(0, 4).All(i => Readout(i).Text.EndsWith("\n0", StringComparison.Ordinal))
                && runtime.Points["operand_set_valid"] is false && runtime.Points["function_select_valid"] is false
                && runtime.Points["calculate_request"] is false && Result(0L, false),
                "function_selector_reset_clears_values_and_manual_inputs");
        }
        finally { DisableVirtualController(); }
    }

    private void VerifySumCounterWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-9-03-sum-and-counter-function", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        Label3D Readout(int index) => root.GetNode<Node3D>($"numeric_display_{index}").GetNode<Label3D>("NumericReadout");
        check(root.GetNodeOrNull<Node3D>("machine_0") is null
            && Enumerable.Range(0, 4).All(i => Readout(i).Text.EndsWith("\n0", StringComparison.Ordinal)),
            "sum_counter_initial_live_readouts_and_no_unbound_cnc");
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(a, b);
        }
        var equipment = root.GetChildren().OfType<Node3D>().ToArray();
        check(equipment.All(a => equipment.Where(b => b != a)
            .All(b => ReviewMeshes(a).All(am => ReviewMeshes(b).All(bm => Clear(am, bm))))),
            "sum_counter_separate_equipment_clearance");
        var reference = LadderEditorProjectJson.Load(FileAccess.GetFileAsString(
            "res://programs/examples/09-sum-counter-reference.rpproj.json"));
        check(reference.IsReadable, "sum_counter_opt_in_reference_project_readable");
        if (reference.Document is null) throw new InvalidOperationException("Sum/counter reference is unreadable.");
        EnableVirtualControllerProgram(reference.Document.BuildProgram());
        try
        {
            _virtualController!.Run();
            void Scan() => _PhysicsProcess(0.04);
            void Toggle(string point) { runtime.ExecuteAction("toggle-" + point); Scan(); }
            runtime.ExecuteAction("cycle-operand_a"); runtime.ExecuteAction("cycle-operand_a");
            for (var i = 0; i < 3; i++) runtime.ExecuteAction("cycle-operand_b");
            Scan();
            check(Readout(0).Text == "A NEXT\n2" && Readout(1).Text == "B NEXT\n5",
                "sum_counter_pc_operand_cycles_update_live_readouts");
            Toggle("call_complete"); // Invalid event; no permissives yet.
            Toggle("inputs_valid"); Toggle("calculate_request");
            check(Equals(runtime.Points["event_count"], 0L),
                "sum_counter_later_permissive_does_not_count_held_invalid_completion");
            Toggle("call_complete"); Toggle("call_complete");
            check(Equals(runtime.Points["sum_result"], 7L) && Readout(2).Text == "SUM\n7"
                && Equals(runtime.Points["event_count"], 1L) && Readout(3).Text == "COUNT\n1",
                "sum_counter_valid_edge_publishes_sum_and_same_scan_count");
            _PhysicsProcess(1.0);
            check(Equals(runtime.Points["event_count"], 1L), "sum_counter_held_completion_does_not_recount");
            Toggle("inputs_valid");
            check(runtime.Points["result_valid"] is false && Equals(runtime.Points["event_count"], 1L),
                "sum_counter_permissive_loss_clears_validity_preserves_count");
            CommitVirtualControllerSnapshot(_virtualController.Stop());
            check(Readout(2).Text == "SUM\n0" && Readout(3).Text == "COUNT\n0"
                && _virtualController.Snapshot.Counters["completed_calls"].Accumulated == 1,
                "sum_counter_stop_zeroes_output_image_preserves_counter_memory");
            _virtualController.Run(); Scan();
            check(Readout(3).Text == "COUNT\n1", "sum_counter_run_republishes_retained_count");
            runtime.ResetSimulation(); CommitVirtualControllerSnapshot(_virtualController.Reset());
            check(Enumerable.Range(0, 4).All(i => Readout(i).Text.EndsWith("\n0", StringComparison.Ordinal))
                && runtime.Points["call_complete"] is false
                && _virtualController.Snapshot.Counters["completed_calls"].Accumulated == 0,
                "sum_counter_reset_clears_operands_inputs_outputs_and_memory");
        }
        finally { DisableVirtualController(); }
    }

    private void VerifyRadarMountAndBeam(Action<bool, string> check)
    {
        AddMigratedScene("tank-radar", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var tank = root.GetNode<Node3D>("water_tank_radar");
        var transmitter = root.GetNode<Node3D>("radar_transmitter");
        var roof = (MeshInstance3D)tank.FindChild("TANK_roof", true, false);
        var flange = (MeshInstance3D)transmitter.FindChild("PROCESS_flange", true, false);
        var lens = (MeshInstance3D)transmitter.FindChild("ANTENNA_dielectric_lens", true, false);
        var head = (MeshInstance3D)transmitter.FindChild("RADAR_head", true, false);
        var beam = (MeshInstance3D)transmitter.FindChild("KIN_radar_beam", true, false);
        var liquid = (MeshInstance3D)tank.FindChild("KIN_liquid", true, false);
        var shell = (MeshInstance3D)tank.FindChild("TANK_shell", true, false);
        GD.Print($"RADAR_MOUNT roof={ReviewBounds(roof)} flange={ReviewBounds(flange)} head={ReviewBounds(head)}");
        check(MathF.Abs(ReviewBounds(flange).Position.Y - ReviewBounds(roof).End.Y) < 0.001f,
            "radar_process_flange_seats_on_delivered_roof");
        var electronics = ReviewMeshes(transmitter).Where(mesh => !mesh.Name.ToString().StartsWith("PROCESS_", StringComparison.Ordinal)
            && !mesh.Name.ToString().StartsWith("ANTENNA_", StringComparison.Ordinal)
            && mesh != beam).ToArray();
        bool Clear(MeshInstance3D part, MeshInstance3D other)
        {
            if (!OrientedBoxesPenetrate(part, other)) return true;
            if (other.Name != "RAIL_top") return false;
            // The circular rail's bounding box fills its empty center. Check
            // the imported ring's inner radius instead of treating it as a disk.
            var center = ReviewBounds(other).GetCenter();
            float Radius(Vector3 point) => new Vector2(point.X - center.X, point.Z - center.Z).Length();
            var innerRadius = Enumerable.Range(0, other.Mesh.GetSurfaceCount())
                .SelectMany(surface => other.Mesh.SurfaceGetArrays(surface)[(int)Mesh.ArrayType.Vertex].AsVector3Array())
                .Select(vertex => Radius(other.GlobalTransform * vertex)).Min();
            var bounds = ReviewBounds(part);
            var maxRadius = Enumerable.Range(0, 8).Select(corner => Radius(bounds.Position + bounds.Size * new Vector3(
                (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1))).Max();
            GD.Print($"RADAR_RAIL_CLEARANCE {part.Name} innerRadius={innerRadius} outerPartRadius={maxRadius}");
            return maxRadius + 0.005f < innerRadius;
        }
        foreach (var part in electronics)
            foreach (var other in ReviewMeshes(tank).Where(other => !Clear(part, other)))
                GD.Print($"RADAR_MOUNT_CANDIDATE {part.Name}/{other.Name}");
        check(ReviewBounds(head).Position.Y > ReviewBounds(roof).End.Y
            && electronics.All(part => ReviewMeshes(tank).All(other => Clear(part, other))),
            "radar_electronics_above_roof_and_clear_of_manway_and_guard");
        bool BeamMatches()
        {
            var actual = Convert.ToDouble(_sceneRuntime!.Points["radar_distance"]);
            var expected = ReviewBounds(lens).Position.Y - ReviewBounds(liquid).End.Y;
            var shellBounds = ReviewBounds(shell);
            var center = shellBounds.GetCenter();
            var shellRadius = shellBounds.Size.X * 0.5f;
            var beamInside = Enumerable.Range(0, beam.Mesh.GetSurfaceCount())
                .SelectMany(surface => beam.Mesh.SurfaceGetArrays(surface)[(int)Mesh.ArrayType.Vertex].AsVector3Array())
                .Select(vertex => beam.GlobalTransform * vertex)
                .All(point => new Vector2(point.X - center.X, point.Z - center.Z).Length() < shellRadius - 0.005f);
            return Math.Abs(actual - expected) < 0.001
                && beamInside
                && MathF.Abs(ReviewBounds(beam).End.Y - ReviewBounds(lens).Position.Y) < 0.001f
                && MathF.Abs(ReviewBounds(beam).Position.Y - ReviewBounds(liquid).End.Y) < 0.001f;
        }
        var runtime = _sceneRuntime!;
        GD.Print($"RADAR_INITIAL_DISTANCE {runtime.Points["radar_distance"]}");
        check(BeamMatches(), "radar_initial_beam_and_distance_share_lens_and_surface_datums");
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["inlet_pump_run"] = true, ["drain_valve_open"] = false });
        var initialDistance = Convert.ToDouble(runtime.Points["radar_distance"]);
        var consistent = true;
        for (var sample = 0; sample < 200; sample++) { runtime.AdvanceSimulation(0.02); consistent &= BeamMatches(); }
        var filledDistance = Convert.ToDouble(runtime.Points["radar_distance"]);
        check(consistent && filledDistance < initialDistance, "radar_sampled_fill_shortens_beam_and_distance_together");
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["inlet_pump_run"] = false, ["drain_valve_open"] = true });
        for (var sample = 0; sample < 200; sample++) { runtime.AdvanceSimulation(0.02); consistent &= BeamMatches(); }
        var drainedDistance = Convert.ToDouble(runtime.Points["radar_distance"]);
        check(consistent && drainedDistance > filledDistance, "radar_sampled_drain_lengthens_beam_and_distance_together");
        runtime.SetControllerPlaybackRunning(false);
        runtime.AdvanceSimulation(1);
        check(BeamMatches() && Math.Abs(Convert.ToDouble(runtime.Points["radar_distance"]) - drainedDistance) < 0.001,
            "radar_stopped_clock_holds_surface_range_and_beam");
        runtime.ResetSimulation();
        check(BeamMatches() && Math.Abs(Convert.ToDouble(runtime.Points["radar_distance"]) - initialDistance) < 0.001,
            "radar_reset_restores_initial_surface_range_and_beam");
    }

    private void VerifyBoxVolumeFixture(Action<bool, string> check)
    {
        AddMigratedScene("lab-9-10-box-volume", _candidateCatalog!, _mainCamera!, false, false);
        var carton = _sceneCompositionRoot!.GetNode<Node3D>("box_0");
        var fixture = _sceneCompositionRoot.GetNode<Node3D>("training_accessory_3");
        var floor = (MeshInstance3D)fixture.FindChild("DIMENSION_tabletop", true, false);
        var bounds = ReviewBounds(carton);
        var floorBounds = ReviewBounds(floor);
        GD.Print($"BOX_VOLUME_BENCH_CONTACT carton={bounds} bench={floorBounds}");
        // The formerly floor-supported carton now belongs on the carrying bench.
        check(MathF.Abs(bounds.Position.Y - floorBounds.End.Y) < 0.001f
            && bounds.Position.X >= floorBounds.Position.X && bounds.End.X <= floorBounds.End.X
            && bounds.Position.Z >= floorBounds.Position.Z && bounds.End.Z <= floorBounds.End.Z,
            "box_volume_carton_contacts_bench_with_full_footprint");
        var heads = ReviewMeshes(fixture).Where(mesh => mesh.Name.ToString().StartsWith("DIMENSION_head_", StringComparison.Ordinal)).ToArray();
        check(heads.Length == 3 && fixture.FindChild("KIN_bottom_bar", true, false) is null
            && _sceneCompositionRoot.GetNodeOrNull<Node3D>("machine_1") is null,
            "box_volume_fixture_has_three_heads_without_shutter_or_cnc");
        var fixtureParts = ReviewMeshes(fixture);
        var feet = fixtureParts.Where(mesh => mesh.Name.ToString().Contains("foot", StringComparison.Ordinal)).ToArray();
        var supports = fixtureParts.Where(mesh => mesh.Name.ToString().Contains("leg", StringComparison.Ordinal)
            || mesh.Name.ToString().Contains("post", StringComparison.Ordinal)).ToArray();
        check(feet.Length == 7 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f)
            && supports.All(support => feet.Any(foot => ReviewBounds(support).Intersects(ReviewBounds(foot)))),
            "box_volume_table_and_sensor_supports_seat_on_grounded_feet");
        var crossbeam = (MeshInstance3D)fixture.FindChild("DIMENSION_crossbeam", true, false);
        check(supports.Where(mesh => mesh.Name.ToString().Contains("table_leg", StringComparison.Ordinal))
                .All(leg => ReviewBounds(leg).Intersects(floorBounds))
            && supports.Where(mesh => mesh.Name.ToString().Contains("portal_post", StringComparison.Ordinal))
                .All(post => ReviewBounds(post).Intersects(ReviewBounds(crossbeam)))
            && heads.All(head => supports.Append(crossbeam).Any(support => ReviewBounds(head).Intersects(ReviewBounds(support)))),
            "box_volume_bench_portal_and_sensor_heads_have_connected_supports");
        check(new[] { ("switch_5", "LENGTH"), ("switch_6", "WIDTH"), ("switch_7", "HEIGHT") }
            .All(item => _sceneCompositionRoot.GetNode<Node3D>(item.Item1).FindChild("OperatorFaceLabel", true, false)
                is Label3D label && label.Text == item.Item2), "box_volume_manual_input_plates_match_dimensions");
        var others = _sceneCompositionRoot.GetChildren().OfType<Node3D>()
            .Where(node => node != carton).SelectMany(ReviewMeshes).ToArray();
        check(ReviewMeshes(carton).All(part => others.All(other =>
        {
            var overlap = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f
                || !OrientedBoxesPenetrate(part, other);
        })), "box_volume_carton_clear_of_separate_equipment");
        var separate = _sceneCompositionRoot.GetChildren().OfType<Node3D>()
            .Where(node => node != carton && node != fixture).SelectMany(ReviewMeshes).ToArray();
        check(fixtureParts.All(part => separate.All(other =>
        {
            var overlap = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f
                || !OrientedBoxesPenetrate(part, other);
        })), "box_volume_fixture_clear_of_controls_and_readout");
    }

    private void VerifyBoxVolumeNumericWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-9-10-box-volume", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var displayIds = new[] { "numeric_display_0", "numeric_display_1", "numeric_display_2", "training_accessory_4" };
        Label3D? Readout(int index) => root.GetNodeOrNull<Node3D>(displayIds[index])?.GetNodeOrNull<Label3D>("NumericReadout");
        var live = Enumerable.Range(0, 4).All(i => Readout(i)?.Text.EndsWith("\n0", StringComparison.Ordinal) == true);
        check(live, "box_volume_four_live_numeric_readouts_initially_zero");
        if (!live) return; // Report the missing contract without obscuring other scene checks.
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f || !OrientedBoxesPenetrate(a, b);
        }
        var displays = displayIds.Select(id => root.GetNode<Node3D>(id)).ToArray();
        check(displays.All(display => ReviewMeshes(display).All(part => root.GetChildren().OfType<Node3D>()
            .Where(other => other != display).SelectMany(ReviewMeshes).All(other => Clear(part, other)))),
            "box_volume_all_readouts_clear_of_fixture_carton_and_controls");
        check(displays.All(display =>
        {
            var foot = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
            var mast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
            var head = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
            return MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f
                && ReviewBounds(mast).Intersects(ReviewBounds(foot).Grow(0.001f))
                && ReviewBounds(mast).Intersects(ReviewBounds(head));
        }), "box_volume_numeric_stands_grounded_and_supported");
        var reference = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/09-box-volume-reference.rpproj.json"));
        check(reference.IsReadable, "box_volume_reference_readable");
        if (reference.Document is null) throw new InvalidOperationException("Box-volume reference is unreadable.");
        EnableVirtualControllerProgram(reference.Document.BuildProgram());
        try
        {
            _virtualController!.Run();
            void Scan() => _PhysicsProcess(0.04);
            void Toggle(string point) { runtime.ExecuteAction("toggle-" + point); Scan(); }
            void Next(string point, int times = 1) { for (var i = 0; i < times; i++) runtime.ExecuteAction("cycle-" + point); Scan(); }
            bool Result(long value, bool valid) => Equals(runtime.Points["volume_mm3"], value)
                && Equals(runtime.Points["volume_result_valid"], valid) && Readout(3)!.Text == $"VOLUME mm3\n{value}";
            Toggle("length_valid"); Toggle("width_valid"); Toggle("height_valid");
            check(Result(0, false), "box_volume_zero_dimensions_invalid_despite_manual_validity");
            Next("length_mm", 4); Next("width_mm", 3); Next("height_mm", 3);
            check(Readout(0)!.Text == "L mm NEXT\n850" && Readout(1)!.Text == "W mm NEXT\n720"
                && Readout(2)!.Text == "H mm NEXT\n720", "box_volume_manual_mm_cycles_update_readouts");
            check(Result(440640000, true), "box_volume_850_by_720_by_720_publishes_same_scan_mm3");
            Next("height_mm");
            check(Result(520200000, true), "box_volume_changed_height_recalculates");
            foreach (var point in new[] { "length_valid", "width_valid", "height_valid" })
            {
                Toggle(point);
                check(Result(520200000, false), $"box_volume_{point}_loss_invalidates_retains_last_value");
                Toggle(point);
            }
            // An in-memory sampled-input fixture exercises the reference guard
            // beyond bounded UI cycles; it never contacts or writes a PLC.
            bool Guard(double value)
            {
                var inputs = new Dictionary<string, double>(SampleVirtualControllerNumericInputs()) { ["height_mm"] = value };
                _virtualController.Advance(0.02, SampleVirtualControllerInputs, () => inputs,
                    CommitVirtualControllerOutputs, CommitVirtualControllerNumericOutputs, runtime.AdvanceSimulation);
                return Result(520200000, false);
            }
            check(Guard(-1), "box_volume_negative_dimension_invalid");
            check(Guard(1001), "box_volume_above_1000_mm_invalid_before_dint_multiply");
            Next("length_mm"); Next("width_mm", 2); Next("height_mm");
            check(Result(1000000000, true), "box_volume_upper_bound_product_fits_dint");
            CommitVirtualControllerSnapshot(_virtualController.Stop());
            check(Result(0, false), "box_volume_stop_clears_output_image");
            _virtualController.Run(); Scan();
            check(Result(1000000000, true), "box_volume_restart_recalculates_valid_dimensions");
            runtime.ResetSimulation(); CommitVirtualControllerSnapshot(_virtualController.Reset());
            check(Result(0, false) && Enumerable.Range(0, 3).All(i => Readout(i)!.Text.EndsWith("\n0", StringComparison.Ordinal))
                && new[] { "length_valid", "width_valid", "height_valid" }.All(point => runtime.Points[point] is false),
                "box_volume_reset_clears_numeric_and_boolean_inputs");
        }
        finally { DisableVirtualController(); }
    }

    private void VerifyPusherRodGeometry(Action<bool, string> check)
    {
        AddMigratedScene("scene-2-conveyor-pusher", _candidateCatalog!, _mainCamera!, false, false);
        var pusher = _sceneCompositionRoot!.GetNode<Node3D>("scene2_pusher");
        var rod = (MeshInstance3D)pusher.FindChild("KIN_pusher_rod", true, false);
        var clevis = (MeshInstance3D)pusher.FindChild("KIN_pusher_clevis", true, false);
        var motion = pusher.FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>().Single();
        var authored = rod.Transform;
        // Measure the delivered mesh in its controller's parent coordinates.
        // The imported cylinder's local axes differ from its travel direction.
        Aabb ParentBounds(MeshInstance3D mesh) => mesh.Transform * mesh.GetAabb();
        var initialClevis = ParentBounds(clevis);
        GD.Print($"PUSHER_ROD_INITIAL bounds={ParentBounds(rod)} basis={rod.Basis} clevis={initialClevis}");
        foreach (var rootRotation in new[] { -90.0f, 0.0f })
        {
            pusher.RotationDegrees = new Vector3(0, rootRotation, 0);
            motion.SetPositionNormalized(0);
            var initial = ReviewBounds(rod);
            var travelAxis = rootRotation == -90.0f ? 2 : 0;
            var seated = true;
            var follows = true;
            var diameter = true;
            for (var sample = 0; sample <= 100; sample++)
            {
                var extension = motion.TravelM * sample / 100;
                motion.SetPositionNormalized(sample / 100.0f);
                var bounds = ReviewBounds(rod);
                var carriage = ReviewBounds(clevis);
                seated &= MathF.Abs(bounds.Position[travelAxis] - initial.Position[travelAxis]) < 0.001f;
                follows &= MathF.Abs(bounds.End[travelAxis] - initial.End[travelAxis] - extension) < 0.001f
                    && bounds.Intersects(carriage);
                diameter &= MathF.Abs(bounds.Size.Y - initial.Size.Y) < 0.001f
                    && MathF.Abs(bounds.Size[travelAxis == 2 ? 0 : 2] - initial.Size[travelAxis == 2 ? 0 : 2]) < 0.001f;
            }
            GD.Print($"PUSHER_ROD_ENDPOINT bounds={ParentBounds(rod)} clevis={ParentBounds(clevis)}");
            check(seated, $"pusher_rod_gland_end_fixed_through_101_samples_rotation_{rootRotation}");
            check(follows, $"pusher_rod_free_end_follows_seated_clevis_rotation_{rootRotation}");
            check(diameter, $"pusher_rod_diameter_preserved_rotation_{rootRotation}");
        }
        motion.Stop();
        var held = rod.Transform;
        motion._PhysicsProcess(0.4);
        check(rod.Transform.IsEqualApprox(held), "pusher_rod_stop_holds_pose");
        motion.ResetMotion();
        check(rod.Transform.IsEqualApprox(authored), "pusher_rod_reset_restores_exact_authored_mesh_transform");
    }

    private void VerifyPusherInstallationGeometry(Action<bool, string> check)
    {
        AddMigratedScene("scene-2-conveyor-pusher", _candidateCatalog!, _mainCamera!, false, false);
        var pusher = _sceneCompositionRoot!.GetNode<Node3D>("scene2_pusher");
        var meshes = ReviewMeshes(pusher);
        MeshInstance3D Part(string name) => meshes.Single(mesh => mesh.Name.ToString() == name);
        Aabb ParentBounds(MeshInstance3D mesh) => mesh.Transform * mesh.GetAabb();
        var plate = Part("KIN_pusher_plate");
        check(MathF.Abs(ReviewBounds(plate).GetCenter().Y - 1.38f) < 0.001f,
            "scene2_pusher_honors_configured_plate_center_height");
        var baseBounds = ParentBounds(Part("FRAME_base"));
        var supports = meshes.Where(mesh => mesh.Name.ToString().StartsWith("FRAME_pedestal", StringComparison.Ordinal)).ToArray();
        var barrel = ParentBounds(Part("CYLINDER_barrel"));
        var bearingMeshes = meshes.Where(mesh => mesh.Name.ToString().StartsWith("GUIDE_bearing", StringComparison.Ordinal)).ToArray();
        check(MathF.Abs(ReviewBounds(Part("FRAME_base")).Position.Y) < 0.001f
            && supports.All(mesh => MathF.Abs(ParentBounds(mesh).Position.Y - baseBounds.End.Y) < 0.001f),
            "scene2_pusher_floor_base_and_column_bottoms_grounded");
        check(bearingMeshes.All(bearing => supports.Any(support =>
        {
            var seat = ParentBounds(support); var mounted = ParentBounds(bearing);
            return MathF.Abs(seat.End.Y - mounted.Position.Y) < 0.001f
                && seat.Position.X <= mounted.Position.X && seat.End.X >= mounted.End.X
                && seat.Position.Z <= mounted.Position.Z && seat.End.Z >= mounted.End.Z;
        })), "scene2_pusher_fixed_guide_bearings_have_supported_bottoms");
        check(supports.Count(support => MathF.Abs(ParentBounds(support).End.Y - barrel.Position.Y) < 0.001f
            && ParentBounds(support).Position.X >= barrel.Position.X && ParentBounds(support).End.X <= barrel.End.X) == 2,
            "scene2_pusher_barrel_has_two_floor_supported_seats");
        var manifoldPost = meshes.FirstOrDefault(mesh => mesh.Name.ToString() == "FRAME_manifold_post");
        var manifold = ParentBounds(Part("VALVE_manifold"));
        check(manifoldPost is not null && MathF.Abs(ParentBounds(manifoldPost).Position.Y - baseBounds.End.Y) < 0.001f
            && MathF.Abs(ParentBounds(manifoldPost).End.Y - manifold.Position.Y) < 0.001f,
            "scene2_pusher_raised_air_manifold_has_floor_supported_mount");
        var motion = pusher.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var shafts = meshes.Where(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_guide_shaft", StringComparison.Ordinal)).ToArray();
        var caps = meshes.Where(mesh => mesh.Name.ToString().StartsWith("CYLINDER_", StringComparison.Ordinal)).ToArray();
        var engagement = true; var capClearance = true; var bracketSeat = true;
        var sensor = _sceneCompositionRoot!.GetNode<Node3D>("scene2_photoeye");
        var sensorSolids = ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var sensorClearance = true;
        var fasteners = meshes.Where(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_PLATE_bolt", StringComparison.Ordinal)).ToArray();
        var fastenerOffsets = fasteners.ToDictionary(mesh => mesh, mesh => mesh.Position - plate.Position);
        var fastenersFollow = fasteners.Length == 4;
        var initialShaftSizes = shafts.ToDictionary(mesh => mesh, mesh => ParentBounds(mesh).Size);
        for (var sample = 0; sample <= 100; sample++)
        {
            motion.SetPositionNormalized(sample / 100f);
            foreach (var shaft in shafts)
            {
                var rod = ParentBounds(shaft);
                var bearing = bearingMeshes.Single(mesh => MathF.Sign(ParentBounds(mesh).GetCenter().Z) == MathF.Sign(rod.GetCenter().Z));
                var seat = ParentBounds(bearing);
                engagement &= rod.Position.X < seat.Position.X && rod.End.X > seat.End.X
                    && rod.Size.IsEqualApprox(initialShaftSizes[shaft]);
                capClearance &= caps.All(cap => !rod.Intersects(ParentBounds(cap)));
                var bracket = meshes.Single(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_bracket", StringComparison.Ordinal)
                    && MathF.Sign(ParentBounds(mesh).GetCenter().Z) == MathF.Sign(rod.GetCenter().Z));
                bracketSeat &= rod.Intersects(ParentBounds(bracket))
                    && ParentBounds(bracket).Intersects(ParentBounds(Part("KIN_pusher_crossmember")));
            }
            sensorClearance &= meshes.Where(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_", StringComparison.Ordinal))
                .All(mesh => sensorSolids.All(solid =>
                {
                    var overlap = ReviewBounds(mesh).Intersection(ReviewBounds(solid)).Size;
                    if (overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f) return true;
                    if (solid.Name.ToString().EndsWith("_cable", StringComparison.Ordinal))
                    {
                        // A raised curved pigtail's enclosing box includes
                        // empty space beside the guide bracket. Screen its
                        // actual transformed triangles, as for the receiver.
                        var faces = solid.Mesh.GetFaces();
                        var movingBounds = ReviewBounds(mesh);
                        for (var index = 0; index < faces.Length; index += 3)
                        {
                            var triangle = new Aabb(solid.GlobalTransform * faces[index], Vector3.Zero)
                                .Expand(solid.GlobalTransform * faces[index + 1]).Expand(solid.GlobalTransform * faces[index + 2]);
                            var intersection = movingBounds.Intersection(triangle).Size;
                            if (intersection.X > 0.005f && intersection.Y > 0.005f && intersection.Z > 0.005f) return false;
                        }
                        return true;
                    }
                    GD.Print($"PUSHER_SENSOR_CONFLICT sample={sample} {mesh.Name}/{solid.Name}");
                    return false;
                }));
            fastenersFollow &= fasteners.All(mesh => (mesh.Position - plate.Position).IsEqualApprox(fastenerOffsets[mesh]));
        }
        check(engagement, "scene2_pusher_rigid_guide_shafts_engaged_through_101_stroke_samples");
        check(capClearance, "scene2_pusher_guide_shafts_clear_round_cylinder_through_stroke");
        check(bracketSeat, "scene2_pusher_both_guides_remain_seated_in_attached_carriage_brackets");
        check(sensorClearance, "scene2_pusher_moving_members_clear_photoeye_solids_through_101_samples");
        check(fastenersFollow, "scene2_pusher_all_four_plate_fasteners_follow_plate_through_stroke");
        motion.ResetMotion();
        GD.Print($"PUSHER_INSTALLATION_BOUNDS plate={ReviewBounds(plate)} supports={string.Join(';', supports.Select(mesh => ParentBounds(mesh).ToString()))}");
    }

    private void VerifyCartonReceiverGeometry(Action<bool, string> check)
    {
        AddMigratedScene("scene-2-conveyor-pusher", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var receiver = root.GetNode<Node3D>("scene2_receiver");
        var parts = ReviewMeshes(receiver);
        var deck = ReviewBounds(parts.Single(mesh => mesh.Name == "BENCH_top"));
        var belt = ReviewBounds((MeshInstance3D)root.GetNode("scene2_conveyor").FindChild("KIN_belt_surface", true, false));
        check(MathF.Abs(deck.End.Y - belt.End.Y) < 0.001f
            && MathF.Abs(deck.Position.Z - belt.End.Z) < 0.001f,
            "scene2_receiver_deck_flush_with_belt_height_and_edge");
        var legs = parts.Where(mesh => mesh.Name.ToString().StartsWith("BENCH_leg", StringComparison.Ordinal)).ToArray();
        var beams = parts.Where(mesh => mesh.Name.ToString().StartsWith("RECEIVER_", StringComparison.Ordinal)).ToArray();
        check(legs.Length == 4 && legs.All(leg => MathF.Abs(ReviewBounds(leg).Position.Y) < 0.001f
                && beams.Any(beam => ReviewBounds(beam).Grow(0.001f).Intersects(ReviewBounds(leg))))
            && beams.Length == 4 && beams.All(beam => MathF.Abs(ReviewBounds(beam).End.Y - deck.Position.Y) < 0.001f),
            "scene2_receiver_four_grounded_legs_and_frame_seated_under_deck");
        // Geometric footprint witness only; actual contact and visibility
        // have separate runtime and actual-ladder checks.
        var load = ReviewBounds(root.GetNode("scene2_product"));
        var covered = true;
        for (var sample = 0; sample <= 100; sample++)
        {
            var footprint = new Aabb(new Vector3(-0.2f - load.Size.X / 2, belt.End.Y,
                load.Position.Z + 1.35f * sample / 100), load.Size);
            covered &= footprint.Position.X >= deck.Position.X && footprint.End.X <= deck.End.X
                && footprint.Position.Z >= belt.Position.Z && footprint.End.Z <= deck.End.Z;
        }
        check(covered, "scene2_carton_footprint_covered_by_belt_and_receiver_through_101_offsets");
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            if (overlap.X <= 0.001f || overlap.Y <= 0.001f || overlap.Z <= 0.001f || !OrientedBoxesPenetrate(a, b)) return true;
            if (!b.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)) return false;
            // Routed cables have large empty bounds. Screen actual triangle
            // bounds against the receiver, rather than treating the whole
            // route's enclosing box as solid material.
            var faces = b.Mesh.GetFaces();
            var receiverBounds = ReviewBounds(a);
            for (var index = 0; index < faces.Length; index += 3)
            {
                var triangle = new Aabb(b.GlobalTransform * faces[index], Vector3.Zero);
                triangle = triangle.Expand(b.GlobalTransform * faces[index + 1]).Expand(b.GlobalTransform * faces[index + 2]);
                var intersection = receiverBounds.Intersection(triangle).Size;
                if (intersection.X > 0.001f && intersection.Y > 0.001f && intersection.Z > 0.001f) return false;
            }
            return true;
        }
        var others = root.GetChildren().OfType<Node3D>().Where(node => node != receiver && node.Name != "scene2_product")
            .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var clear = true;
        var motion = root.GetNode("scene2_pusher").FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>().Single();
        for (var sample = 0; sample <= 100; sample++)
        {
            motion.SetPositionNormalized(sample / 100f);
            foreach (var part in parts)
                foreach (var other in others.Where(other => !Clear(part, other)))
                {
                    clear = false;
                    if (sample == 0) GD.Print($"RECEIVER_COLLISION {part.Name} {ReviewBounds(part)} {other.GetParent().Name}/{other.Name} {ReviewBounds(other)}");
                }
        }
        check(clear, "scene2_receiver_clear_of_other_equipment_through_101_pusher_samples");
        motion.ResetMotion();
        GD.Print($"CARTON_RECEIVER_BOUNDS deck={deck} belt={belt}");
    }

    private void VerifyCartonPusherContactGeometry(Action<bool, string> check)
    {
        AddMigratedScene("scene-2-conveyor-pusher", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var pusher = root.GetNode<Node3D>("scene2_pusher");
        var carton = root.GetNode<Node3D>("scene2_product");
        var runtime = _sceneRuntime!;
        var plate = (MeshInstance3D)pusher.FindChild("KIN_pusher_plate", true, false);
        var crossmember = (MeshInstance3D)pusher.FindChild("KIN_pusher_crossmember", true, false);
        var moving = ReviewMeshes(pusher).Where(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_", StringComparison.Ordinal)).ToArray();
        var yokes = moving.Where(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_plate_yoke", StringComparison.Ordinal)).ToArray();
        var belt = ReviewBounds((MeshInstance3D)root.GetNode("scene2_conveyor").FindChild("KIN_belt_surface", true, false));
        var deck = ReviewBounds((MeshInstance3D)root.GetNode("scene2_receiver").FindChild("BENCH_top", true, false));
        var otherSolids = root.GetChildren().OfType<Node3D>().Where(node => node != pusher && node != carton)
            .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        bool Clear(MeshInstance3D part, MeshInstance3D other)
        {
            var overlap = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
            if (overlap.X <= 0.002f || overlap.Y <= 0.002f || overlap.Z <= 0.002f || !OrientedBoxesPenetrate(part, other)) return true;
            if (!other.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)) return false;
            var bounds = ReviewBounds(part); var faces = other.Mesh.GetFaces();
            for (var index = 0; index < faces.Length; index += 3)
            {
                var triangle = new Aabb(other.GlobalTransform * faces[index], Vector3.Zero)
                    .Expand(other.GlobalTransform * faces[index + 1]).Expand(other.GlobalTransform * faces[index + 2]);
                var size = bounds.Intersection(triangle).Size;
                if (size.X > 0.002f && size.Y > 0.002f && size.Z > 0.002f) return false;
            }
            return true;
        }
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_running"] = true });
        for (var sample = 0; sample < 200 && runtime.Points["part_at_pusher"] is not true; sample++)
            runtime.AdvanceSimulation(0.01);
        check(runtime.Points["part_at_pusher"] is true, "scene2_contact_sweep_reaches_actual_station");
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_running"] = false, ["pusher_extend"] = true });
        var connected = yokes.Length == 2; var contact = true; var clear = true; var supported = true;
        for (var sample = 0; sample <= 150; sample++)
        {
            if (sample > 0) runtime.AdvanceSimulation(0.002);
            var face = ReviewBounds(plate); var load = ReviewBounds(carton);
            connected &= yokes.All(yoke => ReviewBounds(yoke).Intersects(ReviewBounds(crossmember))
                && ReviewBounds(yoke).Intersects(face));
            contact &= carton.Visible && MathF.Abs(face.End.Z - load.Position.Z) < 0.002f
                && face.Position.X < load.End.X && face.End.X > load.Position.X;
            supported &= MathF.Abs(load.Position.Y - belt.End.Y) < 0.001f
                && load.Position.X >= deck.Position.X && load.End.X <= deck.End.X
                && load.Position.Z >= belt.Position.Z && load.End.Z <= deck.End.Z;
            foreach (var part in moving.Concat(ReviewMeshes(carton)))
                foreach (var other in otherSolids.Where(other => !Clear(part, other)))
                {
                    clear = false;
                    if (sample == 0 || sample == 150) GD.Print($"CARTON_CONTACT_CONFLICT sample={sample} {part.Name}/{other.Name}");
                }
            // Contact may touch; the plate, hardware and yoke must not
            // penetrate the carton beyond the 2 mm visual tolerance.
            clear &= ReviewMeshes(carton).All(part => moving.All(other => Clear(part, other)));
        }
        check(connected, "scene2_plate_yoke_connected_to_carriage_and_plate_through_151_samples");
        check(contact, "scene2_plate_and_visible_carton_contact_through_151_two_ms_samples");
        check(clear, "scene2_carton_and_moving_pusher_clear_solids_through_full_transfer");
        check(supported && runtime.Points["pusher_extended"] is true, "scene2_carton_supported_across_contiguous_belt_and_table_through_transfer");
        GD.Print($"CARTON_CONTACT_LANDING plate={ReviewBounds(plate)} load={ReviewBounds(carton)}");
        runtime.ResetSimulation();
    }

    // The continuous belt wraps around its drums. Its overall AABB includes
    // curved ends below the carrying plane and cannot prove full load support.
    // These scenes use an unrotated rectangular deck: derive that plane from
    // actual delivered mesh vertices rather than hard-coded conveyor lengths.
    private static Aabb FlatConveyorCarryingSurface(MeshInstance3D mesh)
    {
        var vertices = mesh.Mesh.GetFaces().Select(vertex => mesh.GlobalTransform * vertex).ToArray();
        var top = vertices.Max(vertex => vertex.Y);
        var carrying = vertices.Where(vertex => MathF.Abs(vertex.Y - top) < 0.00001f).ToArray();
        if (carrying.Length < 4) throw new InvalidOperationException("Conveyor has no rectangular carrying plane.");
        var bounds = new Aabb(carrying[0], Vector3.Zero);
        foreach (var vertex in carrying) bounds = bounds.Expand(vertex);
        return bounds;
    }
    private void VerifyBaseConveyorCartonSupport(Action<bool, string> check)
    {
        foreach (var number in new[] { 1, 2 })
        {
            var sceneId = number == 1 ? "scene-1-conveyor-stop" : "scene-2-conveyor-pusher";
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var runtime = _sceneRuntime!;
            var carton = root.GetNode<Node3D>($"scene{number}_product");
            var initial = carton.Transform;
            var conveyor = root.GetNode<Node3D>($"scene{number}_conveyor");
            var beltMesh = (MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false);
            var belt = FlatConveyorCarryingSurface(beltMesh);
            bool Supported()
            {
                var load = ReviewBounds(carton);
                return MathF.Abs(load.Position.Y - belt.End.Y) < 0.001f
                    && load.Position.X >= belt.Position.X && load.End.X <= belt.End.X
                    && load.Position.Z >= belt.Position.Z && load.End.Z <= belt.End.Z;
            }
            GD.Print($"BASE_CONVEYOR_SUPPORT {sceneId} carton={ReviewBounds(carton)} belt={belt}");
            check(Supported(), $"{sceneId}_initial_carton_contact_and_full_belt_footprint");
            // Symbolic plant probe only: command through the same typed output
            // boundary and clock as the controller. No ladder or transport proof.
            runtime.UsesExternalClock = true;
            runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_running"] = true });
            var feedback = number == 1 ? "simulated_photoeye" : "part_at_pusher";
            var support = true;
            for (var sample = 0; sample < 100 && runtime.Points[feedback] is not true; sample++)
            {
                runtime.AdvanceSimulation(0.02);
                support &= Supported();
            }
            check(support && runtime.Points[feedback] is true,
                $"{sceneId}_carton_supported_through_first_photoeye_detection");
            var sensor = root.GetNode<Node3D>($"scene{number}_photoeye");
            var txLens = (MeshInstance3D)sensor.FindChild("TX_lens", true, false);
            var rxLens = (MeshInstance3D)sensor.FindChild("RX_lens", true, false);
            var detectedLoad = ReviewBounds(carton);
            check(runtime.Points[feedback] is true
                && LineHitsBounds(ReviewBounds(txLens).GetCenter(), ReviewBounds(rxLens).GetCenter(), detectedLoad.Grow(0.001f)),
                $"{sceneId}_first_detection_has_carton_in_optical_envelope");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_running"] = false });
            runtime.SetControllerPlaybackRunning(false);
            var stopped = carton.Transform;
            runtime.AdvanceSimulation(0.5);
            check(carton.Transform.IsEqualApprox(stopped) && Supported(), $"{sceneId}_stopped_clock_holds_supported_carton");
            runtime.ResetSimulation();
            check(carton.Transform.IsEqualApprox(initial) && Supported() && runtime.Points[feedback] is false,
                $"{sceneId}_reset_restores_supported_load_end");
            if (number == 2)
            {
                runtime.SetControllerPlaybackRunning(true);
                runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool>
                    { ["conveyor_running"] = false, ["pusher_extend"] = true });
                var offStationHeld = true;
                for (var sample = 0; sample < 30; sample++)
                {
                    runtime.AdvanceSimulation(0.01);
                    offStationHeld &= carton.Transform.IsEqualApprox(initial) && carton.Visible && Supported();
                }
                check(offStationHeld && runtime.Points[feedback] is false
                    && runtime.Points["pusher_extended"] is true && Convert.ToInt64(runtime.Points["parts_completed"]) == 0,
                    "scene2_off_station_extension_does_not_drag_or_transfer_infeed_carton");
                runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["pusher_extend"] = false });
                for (var sample = 0; sample < 30; sample++)
                {
                    runtime.AdvanceSimulation(0.01);
                    offStationHeld &= carton.Transform.IsEqualApprox(initial) && carton.Visible && Supported();
                }
                check(offStationHeld && runtime.Points["pusher_retracted"] is true,
                    "scene2_off_station_retraction_keeps_infeed_carton_supported");
                runtime.ResetSimulation();
            }
        }
    }

    private bool _auditGuardedLayout;

    private void AuditGuardedLayout()
    {
        var failures = 0;
        void Check(bool ok, string name)
        {
            if (!ok) failures++;
            GD.Print($"GUARDED_LAYOUT_CHECK {name}={ok}");
        }
        try
        {
            VerifyTransferCartonSupport(Check, "lab-3-01-guarded-pallet-transfer");
            var runtime = _sceneRuntime!;
            runtime.UsesExternalClock = true;
            runtime.SetControllerPlaybackRunning(true);
            var drive = _sceneCompositionRoot!.GetNode<Node3D>("conveyor_0")
                .FindChildren("*", string.Empty, true, false).OfType<ConveyorController>().Single();
            Check(!drive.RunCommand, "reset_drive_command_is_off");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["transfer_run"] = true });
            Check(drive.RunCommand && runtime.Points["transfer_run"] is true,
                "plc_transfer_run_commands_conveyor_drive");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["transfer_run"] = false });
            Check(!drive.RunCommand, "withdrawing_transfer_run_removes_drive_command");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["transfer_run"] = true });
            runtime.ResetSimulation();
            Check(!drive.RunCommand && runtime.Points["transfer_run"] is false,
                "reset_removes_transfer_command_and_drive_command");
            var carton = _sceneCompositionRoot.GetNode<Node3D>("box_1");
            var home = carton.Transform;
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["transfer_run"] = true });
            var sawEntry = false;
            var sawExit = false;
            var supported = true;
            var belt = ReviewBounds((MeshInstance3D)_sceneCompositionRoot.GetNode<Node3D>("conveyor_0").FindChild("KIN_belt_surface", true, false));
            for (var tick = 0; tick < 700; tick++)
            {
                runtime.AdvanceSimulation(.02);
                sawEntry |= runtime.Points["entry_carton_present"] is true;
                sawExit |= runtime.Points["exit_carton_present"] is true;
                var load = ReviewBounds(carton);
                supported &= carton.Visible && MathF.Abs(load.Position.Y - belt.End.Y) < .001f
                    && load.Position.X >= belt.Position.X && load.End.X <= belt.End.X
                    && load.Position.Z >= belt.Position.Z && load.End.Z <= belt.End.Z;
                if (tick == 100)
                {
                    var commandHeld = carton.Transform;
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["transfer_run"] = false });
                    for (var held = 0; held < 25; held++) runtime.AdvanceSimulation(.02);
                    Check(carton.Transform == commandHeld && drive.ActualSpeedMps == 0,
                        "withdrawn_plc_run_holds_carton_during_transfer");
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["transfer_run"] = true });
                    var paused = carton.Transform;
                    runtime.SetControllerPlaybackRunning(false);
                    for (var held = 0; held < 25; held++) runtime.AdvanceSimulation(.02);
                    Check(carton.Transform == paused && drive.ActualSpeedMps == 0, "stop_retains_supported_carton_and_zero_drive_speed");
                    runtime.SetControllerPlaybackRunning(true);
                }
            }
            Check(supported, "entire_transfer_retains_carton_on_delivered_belt");
            Check(sawEntry && sawExit, "moving_carton_crosses_both_installed_optical_routes");
            Check(runtime.Points["transfer_complete"] is true && carton.Visible, "exit_completion_retains_visible_carton");
            var exit = carton.Transform;
            for (var tick = 0; tick < 100; tick++) runtime.AdvanceSimulation(.02);
            Check(carton.Transform == exit && drive.ActualSpeedMps == 0, "held_run_at_exit_does_not_wrap_or_disappear");
            runtime.ResetSimulation();
            Check(carton.Transform == home && runtime.Points["transfer_complete"] is false,
                "reset_restores_supported_infeed_and_clears_completion");
            runtime.SetControllerPlaybackRunning(false);
            AuditGuardedTransferReference(Check);
        }
        catch (Exception exception) { failures++; GD.PushError(exception.ToString()); }
        GD.Print($"GUARDED_LAYOUT_VERIFY {(failures == 0 ? "PASS" : "FAIL")} geometry, retained carton travel and offline ladder reference; native views pending");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyTransferCartonSupport(Action<bool, string> check, string? onlySceneId = null)
    {
        foreach (var sceneId in new[]
        {
            "lab-3-01-guarded-pallet-transfer",
            "lab-5-09-bag-indexing-conveyor",
            "lab-6-07-luggage-weight-sort",
            "lab-9-11-pallet-counting"
        })
        {
            if (onlySceneId is not null && sceneId != onlySceneId) continue;
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            // Luggage now uses its delivered suitcase actor, not a duplicate carton.
            var carton = root.GetNode<Node3D>(sceneId == "lab-6-07-luggage-weight-sort" ? "training_accessory_5" : "box_1");
            var load = ReviewBounds(carton);
            var conveyor = root.GetNode<Node3D>("conveyor_0");
            var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
            GD.Print($"TRANSFER_CARTON_SUPPORT {sceneId} carton={load} belt={belt}");
            // Use delivered, transformed meshes: equipment origin and catalog
            // nominal height alone did not catch cartons buried below the bed.
            check(MathF.Abs(load.Position.Y - belt.End.Y) < 0.001f
                && load.Position.X >= belt.Position.X && load.End.X <= belt.End.X
                && load.Position.Z >= belt.Position.Z && load.End.Z <= belt.End.Z,
                $"{sceneId}_carton_resting_within_carrying_belt");
            if (sceneId == "lab-3-01-guarded-pallet-transfer")
            {
                // Prove placement from delivered meshes, rather than equipment
                // origins. This does not establish protective-field behavior.
                foreach (var sensorId in new[] { "photoeye_2", "photoeye_3" })
                {
                    var sensor = root.GetNode<Node3D>(sensorId);
                    var beams = ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString().StartsWith("KIN_beam")).ToArray();
                    // Imported witness beams are dashed; their combined bounds
                    // describe the lens-to-lens route, not each individual dash.
                    var beam = beams.Length > 0 ? ReviewBounds(beams[0]) : new Aabb();
                    foreach (var dash in beams.Skip(1)) beam = beam.Merge(ReviewBounds(dash));
                    GD.Print($"GUARDED_SENSOR_ROUTE {sensorId} beam={beam}");
                    check(beams.Length > 0 && beam.Position.X >= belt.Position.X && beam.End.X <= belt.End.X
                        && beam.Position.Z <= belt.Position.Z && beam.End.Z >= belt.End.Z
                        && beam.Position.Y > belt.End.Y && beam.End.Y < load.End.Y,
                        $"{sceneId}_{sensorId}_beam_crosses_carrying_route_at_carton_height");
                }
                var curtain = root.GetNode<Node3D>("training_accessory_6");
                var field = ReviewMeshes(curtain).Where(mesh => mesh.Name.ToString().StartsWith("BEAM_")).ToArray();
                check(field.Length > 0 && field.All(mesh =>
                {
                    var beam = ReviewBounds(mesh);
                    return beam.Position.X >= belt.Position.X && beam.End.X <= belt.End.X
                        && beam.Position.Z <= belt.Position.Z && beam.End.Z >= belt.End.Z;
                }), $"{sceneId}_curtain_field_spans_conveyor_width");
                var gate = root.GetNode<Node3D>("training_accessory_7");
                check(ReviewMeshes(gate).Any(mesh => mesh.Name.ToString().StartsWith("GATE_frame"))
                    && ReviewBounds(gate).Position.Z > belt.End.Z,
                    $"{sceneId}_mesh_gate_is_beside_carrying_route");
            }
            var cartonParts = ReviewMeshes(carton);
            var solids = root.GetChildren().OfType<Node3D>().Where(node => node != carton)
                .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam")).ToArray();
            check(cartonParts.All(part => solids.All(solid =>
            {
                var overlap = ReviewBounds(part).Intersection(ReviewBounds(solid)).Size;
                return overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f
                    || !OrientedBoxesPenetrate(part, solid);
            })), $"{sceneId}_carton_clear_of_separate_equipment_solids");
        }
    }
}
