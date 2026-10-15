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
    private bool _auditMotorAggregates;
    private void AuditMotorAggregates()
    {
        var failures = 0;
        void Check(bool condition, string label)
        {
            if (!condition) failures++;
            GD.Print($"MOTOR_AGGREGATE_AUDIT {label}={condition}");
        }
        try
        {
            VerifyMotorAggregateControllerWorkflow(Check);
            var editorPassed = _simulatorShell!.VerifyAggregateTagEditor(out var editorResult);
            Check(editorPassed, "structured_tag_editor_handlers");
            GD.Print($"MOTOR_AGGREGATE_EDITOR {editorResult}");
        }
        catch (Exception error) { failures++; GD.PushError($"MOTOR_AGGREGATE_AUDIT_EXCEPTION {error}"); }
        finally { DisableVirtualController(); }
        GD.Print($"MOTOR_AGGREGATE_AUDIT_RESULT failures={failures}; offline typed controller, fixture-value and command projection only; native workflow pending");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
    private void VerifyMotorAggregateControllerWorkflow(Action<bool, string> check)
    {
        JsonElement BoundaryFixture(string aliasTarget = "feedback.valid", string sourceType = "bOoL", object? sourceInitial = null, bool hasInitial = true, string sourceName = "source")
        {
            var source = new Dictionary<string, object> { ["name"] = sourceName, ["type"] = sourceType, ["owner"] = "pc" };
            if (hasInitial) source["initial"] = sourceInitial ?? (sourceType.Equals("BOOL", StringComparison.OrdinalIgnoreCase) ? (object)false : 0.0);
            return JsonSerializer.SerializeToElement(new
            {
            type = "booleanPanel",
            points = new object[]
            {
                new { name = "feedback", type = "sTrUcT", owner = "pC", initial = new { valid = false }, aggregate = new { fields = new[] { new { name = "valid", type = "Bool" } } } },
                new { name = "commands", type = "aRrAy", owner = "pLc", initial = new bool[2], aggregate = new { elementType = "Bool", lowerBound = 0, length = 2 } },
                source,
                new { name = "legacy_command", type = "bOoL", owner = "pLc", role = "output", initial = false },
            },
            aggregateFeedbackAliases = new Dictionary<string, string> { [aliasTarget] = sourceName },
            });
        }
        var fixtureRoot = new Node3D();
        var fixtureRuntime = new SceneSimulationRuntime(BoundaryFixture(), fixtureRoot);
        fixtureRuntime.ResetSimulation();
        fixtureRuntime.SetAggregateSceneInput("feedback", new Dictionary<string, object> { ["valid"] = true });
        fixtureRuntime.CommitAggregateSceneOutputs(new Dictionary<string, object> { ["commands"] = new[] { true, false } });
        fixtureRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["legacy_command"] = true });
        check(fixtureRuntime.Points["feedback.valid"] is true && fixtureRuntime.Points["source"] is true && fixtureRuntime.Points["commands[0]"] is true,
            "aggregate_scene_mixed_case_types_owners_share_strict_boundary");
        check(fixtureRuntime.SampleVirtualControllerInputs()["source"] && fixtureRuntime.Points["legacy_command"] is true,
            "mixed_case_scalar_pc_sampling_and_plc_bool_commit_use_same_boundary");
        fixtureRuntime.Free(); fixtureRoot.Free();
        foreach (var fixture in new[]
        {
            ("wrong_type", BoundaryFixture(sourceType: "REAL")),
            ("missing_target", BoundaryFixture(aliasTarget: "feedback.missing")),
            ("missing_source_initial", BoundaryFixture(hasInitial: false)),
            ("wrong_source_initial_type", BoundaryFixture(sourceInitial: 17)),
            ("empty_source_name", BoundaryFixture(sourceName: "")),
        })
        {
            var rejected = false;
            try { SceneAggregateContract.Validate(fixture.Item2); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "aggregate_feedback_alias_" + fixture.Item1 + "_rejected_before_node_allocation");
        }
        AddMigratedScene("lab-10-05-motor-struct-data", _candidateCatalog!, _mainCamera!, false, false);
        var scene = _sceneRuntime!;
        var doc = new LadderEditorDocument();
        doc.ResetProject("motor-struct-qa", "Motor_STRUCT_QA", TimeSpan.FromMilliseconds(20));
        doc.SourceSceneId = "lab-10-05-motor-struct-data";
        var feedbackSchema = new PlcAggregateSchema([new("record_valid", PlcVariableType.Bool), new("temperature_valid", PlcVariableType.Bool), new("alarm_clear", PlcVariableType.Bool), new("power_valid", PlcVariableType.Bool), new("power", PlcVariableType.Real), new("temperature", PlcVariableType.Real)]);
        Dictionary<string, object> Feedback(bool valid) => new() { ["record_valid"] = valid, ["temperature_valid"] = valid, ["alarm_clear"] = valid, ["power_valid"] = valid, ["power"] = 17.5, ["temperature"] = 42.25 };
        doc.AddAggregateTag("feedback", PlcVariableRole.Input, PlcVariableType.Struct, feedbackSchema, Feedback(false), "motor_feedback");
        doc.AddAggregateTag("command", PlcVariableRole.Output, PlcVariableType.Struct, new([new("enable", PlcVariableType.Bool), new("record_ready", PlcVariableType.Bool)]), new Dictionary<string, object> { ["enable"] = false, ["record_ready"] = false }, "motor_command");
        foreach (var target in new[] { "enable", "record_ready" })
        {
            var rung = doc.Rungs.Count;
            doc.AddRung("Record permissives", "command." + target);
            foreach (var field in new[] { "record_valid", "temperature_valid", "alarm_clear" }) doc.AddContact(rung, 0, "feedback." + field, false);
        }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-motor-struct-aggregate-native-qa.rpproj.json"), LadderEditorProjectJson.Save(doc));
        EnableVirtualControllerProgram(doc.BuildProgram());
        var structMotion = CaptureMotorProgramMotion(check);
        try
        {
            RunActiveController(); _PhysicsProcess(.02);
            check(scene.Points["motor_command.enable"] is false, "motor_struct_aggregate_idle_off");
            scene.SetAggregateSceneInput("motor_feedback", Feedback(true)); _PhysicsProcess(.02);
            check(scene.Points["motor_command.enable"] is true && scene.Points["motor_command.record_ready"] is true && scene.Points["motor_enable"] is false, "motor_struct_aggregate_owns_outputs_without_legacy_commands");
            structMotion.Step(1, "motor_struct_accepted_QA_scan");
            structMotion.StopReset("motor_struct_running_QA_program");
            RunActiveController();
            scene.SetAggregateSceneInput("motor_feedback", Feedback(true)); _PhysicsProcess(.02);
            check(Convert.ToDouble(scene.Points["motor_feedback.temperature"]) == 42.25, "motor_struct_explicit_fixture_numeric_value_preserved");
            var readout = _sceneCompositionRoot!.GetNode<Node3D>("training_accessory_4").GetNodeOrNull<Label3D>("NumericReadout");
            check(readout?.Text.Contains("42.25", StringComparison.Ordinal) == true, "motor_struct_actual_readout_projects_fixture_value");
            foreach (var field in new[] { "record_valid", "temperature_valid", "alarm_clear" })
            {
                var input = Feedback(true); input[field] = false; scene.SetAggregateSceneInput("motor_feedback", input); _PhysicsProcess(.02);
                check(scene.Points["motor_command.enable"] is false, "motor_struct_loss_" + field);
                if (field == "temperature_valid") check(readout?.Text.Contains("42.25", StringComparison.Ordinal) == false, "motor_struct_invalid_temperature_hides_fixture_value");
            }
            StopActiveController();
            check(scene.Points["motor_command.enable"] is false, "motor_struct_aggregate_stop_off");
            ResetActiveController();
            check(scene.Points["motor_feedback.record_valid"] is false && scene.Points["motor_command.enable"] is false, "motor_struct_aggregate_reset_defaults");
        }
        finally { DisableVirtualController(); }

        AddMigratedScene("lab-10-06-ten-motor-array-startup", _candidateCatalog!, _mainCamera!, false, false);
        scene = _sceneRuntime!;
        doc = new LadderEditorDocument(); doc.ResetProject("motor-array-aggregate-qa", "Motor_ARRAY_QA", TimeSpan.FromMilliseconds(20));
        doc.SourceSceneId = "lab-10-06-ten-motor-array-startup";
        foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" }) doc.AddTag(input, PlcVariableRole.Input, input);
        doc.AddAggregateTag("motors", PlcVariableRole.Output, PlcVariableType.Array, new(ElementType: PlcVariableType.Bool, LowerBound: 0, Length: 10), new bool[10], "motor_commands");
        for (var i = 0; i < 10; i++)
        {
            var timer = "startup_" + i; doc.AddTag(timer, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var rung = doc.Rungs.Count; doc.AddTimerRung("Motor delay", timer, TimeSpan.FromSeconds((i + 1) * .5), LadderTimerKind.OnDelay);
            foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" }) doc.AddContact(rung, 0, input, false);
            doc.AddRung("Motor ARRAY element", $"motors[{i}]"); doc.AddContact(rung + 1, 0, timer + ".Q", false);
        }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-motor-array-aggregate-native-qa.rpproj.json"), LadderEditorProjectJson.Save(doc));
        EnableVirtualControllerProgram(doc.BuildProgram());
        var arrayMotion = CaptureMotorProgramMotion(check);
        try
        {
            RunActiveController();
            foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" }) ExecuteSelectedControllerAction("toggle-" + input);
            for (var count = 1; count <= 10; count++)
            {
                for (var scan = 0; scan < 25; scan++) _PhysicsProcess(.02);
                check(Enumerable.Range(0, 10).All(i => Equals(scene.Points[$"motor_commands[{i}]"], i < count)) && scene.Points["motor_array_run"] is false,
                    "motor_array_typed_prefix_" + count);
                check(MotorReviewCommandBits(scene.Points) == new string('1', count) + new string('0', 10 - count),
                    "motor_array_review_effective_typed_prefix_" + count);
                arrayMotion.Step(count, "motor_array_accepted_QA_prefix_" + count);
            }
            arrayMotion.StopReset("motor_array_running_QA_program");
            RunActiveController();
            foreach (var input in new[] { "group_start_request", "all_motors_ready", "group_alarm_clear" }) ExecuteSelectedControllerAction("toggle-" + input);
            for (var scan = 0; scan < 250; scan++) _PhysicsProcess(.02);
            check(Enumerable.Range(0, 10).All(i => scene.Points[$"motor_commands[{i}]"] is true), "motor_array_QA_restart_all_before_alarm_loss");
            ExecuteSelectedControllerAction("toggle-group_alarm_clear"); _PhysicsProcess(.02);
            check(Enumerable.Range(0, 10).All(i => scene.Points[$"motor_commands[{i}]"] is false), "motor_array_typed_alarm_loss");
            var rejected = false;
            try { scene.CommitAggregateSceneOutputs(new Dictionary<string, object> { ["motor_commands"] = new bool[9] }); }
            catch (ArgumentException) { rejected = true; }
            check(rejected && Enumerable.Range(0, 10).All(i => scene.Points[$"motor_commands[{i}]"] is false), "motor_array_bad_shape_commit_atomic_rejection");
            StopActiveController(); ResetActiveController();
            check(Enumerable.Range(0, 10).All(i => scene.Points[$"motor_commands[{i}]"] is false), "motor_array_typed_stop_reset");
        }
        finally { DisableVirtualController(); }
        scene.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_array_run"] = true });
        var motions = _sceneCompositionRoot!.GetChildren().OfType<Node3D>().Where(n => n.Name.ToString().StartsWith("motor_", StringComparison.Ordinal))
            .SelectMany(n => n.FindChildren("*", "", true, false).OfType<EquipmentMotionController>()).ToArray();
        check(motions.Length == 10 && motions.All(m => m.Running), "motor_array_legacy_group_still_projects_to_all_motors");
        check(MotorReviewCommandBits(scene.Points) == "1111111111", "motor_array_review_effective_legacy_group");
        scene.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_array_run"] = false });
        var lastOnly = new bool[10]; lastOnly[9] = true;
        scene.CommitAggregateSceneOutputs(new Dictionary<string, object> { ["motor_commands"] = lastOnly });
        check(motions.Count(m => m.Running) == 1, "motor_array_typed_single_element_projects_independently");
        scene.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["motor_0_run"] = true });
        check(motions.Count(m => m.Running) == 2 && scene.Points["motor_commands[0]"] is false, "motor_array_legacy_or_does_not_overwrite_typed_root");
        check(MotorReviewCommandBits(scene.Points) == "1000000001", "motor_array_review_effective_legacy_and_typed_arbitration");
        scene.ResetSimulation();
    }
}
