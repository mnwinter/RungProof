using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditSequenceTower;
    private static readonly string[] SequenceColors = ["red", "amber", "green", "blue"];

    private void AuditSequenceTower()
    {
        var failures = 0;
        void Check(bool condition, string name)
        {
            if (!condition) failures++;
            GD.Print($"SEQUENCE_TOWER_AUDIT {name}={condition}");
        }
        try
        {
            AddMigratedScene("lab-4-04-sequence-light-tower", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var runtime = _sceneRuntime!;
            var tower = root.GetNode<Node3D>("indicator_2");
            var lenses = ReviewMeshes(tower).Where(part => part.Name.ToString().StartsWith("LENS_", StringComparison.Ordinal)).ToArray();
            bool Lit(string color) => lenses.Single(part => part.Name == $"LENS_{color}").MaterialOverride is StandardMaterial3D { EmissionEnabled: true };
            bool DisplayIs(string? color) => SequenceColors.All(name => Lit(name) == (name == color));
            Check(lenses.Length == 4 && SequenceColors.All(color => lenses.Any(part => part.Name == $"LENS_{color}")), "four_named_tower_lenses_are_present");
            Check(DisplayIs(null), "initial_tower_has_no_energized_lenses");
            var redBounds = ReviewBounds(lenses.Single(part => part.Name == "LENS_red"));
            var blueBounds = ReviewBounds(lenses.Single(part => part.Name == "LENS_blue"));
            var cap = ReviewBounds(ReviewMeshes(tower).Single(part => part.Name == "CAP"));
            Check(MathF.Abs(blueBounds.Position.Y - redBounds.Position.Y - 0.235f) < 0.001f
                && blueBounds.Position.Y > redBounds.End.Y && cap.Position.Y > blueBounds.End.Y,
                "blue_tier_and_raised_cap_do_not_overlap_adjacent_lenses");
            Check(root.GetNode<Node3D>("switch_1").GetNode<Label3D>("OperatorFaceLabel").Text == "START"
                && root.GetNode<Node3D>("switch_3").GetNode<Label3D>("OperatorFaceLabel").Text == "STEP", "operator_plates_identify_momentary_requests");
            Check(ReviewBounds(root).Position.Y >= -0.001f, "equipment_is_above_floor");
            // Deliberately supply conflicting output images: the renderer must
            // show every commanded channel, not silently pick a winner.
            runtime.CommitVirtualControllerOutputs(SequenceColors.ToDictionary(color => $"tower_{color}", _ => true));
            Check(SequenceColors.All(Lit), "simultaneous_commands_remain_visible_on_all_four_channels");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["tower_red"] = false });
            Check(!Lit("red") && SequenceColors.Skip(1).All(Lit), "turning_one_channel_off_preserves_the_other_channels");
            runtime.ResetSimulation();

            var expected = CreateSequenceTowerReference();
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/sequence-tower-reference.rpproj.json"), LadderEditorProjectJson.Save(expected));
            var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/sequence-light-tower-reference.rpproj.json"));
            Check(loaded.IsReadable && loaded.Document is not null
                && LadderEditorProjectJson.Save(loaded.Document) == LadderEditorProjectJson.Save(expected), "saved_reference_matches_reviewed_program");
            if (loaded.Document is null || !loaded.IsReadable) throw new InvalidOperationException("Unreadable sequence tower reference.");
            EnableVirtualControllerProgram(loaded.Document.BuildProgram());
            void Scan() => _PhysicsProcess(0.04);
            void Press(string point) { runtime.ExecuteAction($"toggle-{point}"); Scan(); }
            bool StateIs(string? color, bool active, bool complete) => DisplayIs(color)
                && Equals(runtime.Points["tower_active"], active) && Equals(runtime.Points["sequence_complete"], complete)
                && SequenceColors.All(name => Equals(runtime.Points[$"tower_{name}"], name == color));
            RunActiveController(); Scan();
            Press("sequence_step_due");
            Check(StateIs(null, false, false), "step_before_start_is_discarded");
            Press("sequence_start");
            Check(StateIs("red", true, false) && runtime.Points["sequence_start"] is false, "start_pulse_selects_red_and_clears_input");
            for (var step = 0; step < 4; step++)
            {
                _PhysicsProcess(1);
                Check(StateIs(SequenceColors[step], true, false), $"idle_scans_do_not_advance_{SequenceColors[step]}");
                Press("sequence_start");
                Check(StateIs(SequenceColors[step], true, false), $"start_while_active_does_not_reset_{SequenceColors[step]}");
                Press("sequence_step_due");
                Check(StateIs(step == 3 ? null : SequenceColors[step + 1], step != 3, step == 3)
                    && runtime.Points["sequence_step_due"] is false, $"step_{step + 1}_advances_once_and_clears_pulse");
            }
            Press("sequence_step_due");
            Check(StateIs(null, false, true) && _virtualController!.Snapshot.Counters["sequence_steps"].Accumulated == 4,
                "extra_step_after_completion_is_rejected");
            StopActiveController();
            RunActiveController(); Scan();
            Press("sequence_step_due");
            Check(StateIs(null, false, false), "stop_after_completion_clears_status_and_rejects_idle_step_on_run");
            Press("sequence_start");
            Check(StateIs("red", true, false), "fresh_start_after_completion_restarts_at_red");
            Press("sequence_step_due");
            StopActiveController();
            Check(StateIs(null, false, false), "stop_removes_all_color_and_status_commands");
            RunActiveController(); Scan();
            Check(StateIs(null, false, false), "run_after_stop_cannot_resume_previous_color");
            Press("sequence_step_due");
            Check(StateIs(null, false, false), "step_after_stop_cannot_resume_previous_state");
            Press("sequence_start");
            Check(StateIs("red", true, false) && _virtualController!.Snapshot.Counters["sequence_steps"].Accumulated == 0,
                "fresh_start_after_stop_discards_previous_count");
            ResetActiveController();
            Check(StateIs(null, false, false) && runtime.Points["sequence_start"] is false
                && runtime.Points["sequence_step_due"] is false && _virtualController!.Snapshot.Counters["sequence_steps"].Accumulated == 0,
                "global_reset_restores_initial_points_counter_and_lenses");
        }
        catch (Exception error) { failures++; GD.PushError($"SEQUENCE_TOWER_AUDIT_EXCEPTION {error}"); }
        finally { DisableVirtualController(); }
        GD.Print($"SEQUENCE_TOWER_AUDIT_RESULT failures={failures}; offline symbolic controller and rendered-material checks only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private static LadderEditorDocument CreateSequenceTowerReference()
    {
        var document = new LadderEditorDocument();
        document.ResetProject("reference-sequence-tower", "Sequence_Tower_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-4-04-sequence-light-tower";
        foreach (var point in new[] { "sequence_start", "sequence_step_due" }) document.AddTag(point, PlcVariableRole.Input, point);
        document.AddTag("stop_command", PlcVariableRole.Input, "operator.stop");
        foreach (var point in new[] { "tower_active", "sequence_complete" }.Concat(SequenceColors.Select(color => $"tower_{color}")))
            document.AddTag(point, PlcVariableRole.Output, point);
        foreach (var point in new[] { "start_edge", "step_edge", "start_accepted" }) document.AddTag(point, PlcVariableRole.Memory);
        document.AddTag("sequence_steps", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        // Sample both edges before applying state permissives. An invalid
        // request cannot be retained and accepted later in a different state.
        document.AddRung("Sample Start request edge", "start_edge");
        document.InsertEdgeContact(0, 0, 0, "sequence_start", LadderEdgeMode.Rising);
        document.AddRung("Sample Step request edge", "step_edge");
        document.InsertEdgeContact(1, 0, 0, "sequence_step_due", LadderEdgeMode.Rising);
        document.AddRung("Accept Start only when inactive", "start_accepted");
        document.AddContact(2, 0, "start_edge", false);
        document.AddContact(2, 0, "tower_active", true);
        document.AddContact(2, 0, "stop_command", true);
        document.AddCounterRung("New Start resets the old sequence", "sequence_steps", 4, reset: true);
        document.AddContact(3, 0, "start_accepted", false);
        document.AddCounterRung("Count separate active Step requests", "sequence_steps", 4);
        document.AddContact(4, 0, "step_edge", false);
        document.AddContact(4, 0, "tower_active", false);
        document.AddContact(4, 0, "start_accepted", true);
        document.AddContact(4, 0, "stop_command", true);
        // Completion is latched only by an active sequence finishing. Stop
        // clears this output; retained counter memory alone must not relight it.
        document.AddRung("Completed sequence status seal-in", "sequence_complete");
        document.AddContact(5, 0, "tower_active", false);
        document.AddComparison(5, 0, "sequence_steps.ACC", LadderCompareOperator.GreaterOrEqual, "4");
        document.AddParallelBranch(5);
        document.AddContact(5, 1, "sequence_complete", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddContact(5, branch, "start_accepted", true);
            document.AddContact(5, branch, "stop_command", true);
        }
        document.AddRung("Sequence active seal-in", "tower_active");
        document.AddContact(6, 0, "start_accepted", false);
        document.AddParallelBranch(6);
        document.AddContact(6, 1, "tower_active", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddComparison(6, branch, "sequence_steps.ACC", LadderCompareOperator.LessThan, "4");
            document.AddContact(6, branch, "stop_command", true);
        }
        for (var state = 0; state < 4; state++)
        {
            document.AddRung($"State {state}: {SequenceColors[state]} only", $"tower_{SequenceColors[state]}");
            document.AddContact(7 + state, 0, "tower_active", false);
            document.AddComparison(7 + state, 0, "sequence_steps.ACC", LadderCompareOperator.Equal, state.ToString());
        }
        return document;
    }
}
