using System;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;
public partial class Main
{
    // Qualification only: these documents never replace the learner's editor draft.
    private void VerifyWorkspaceRollerBindings(Action<bool,string> check)
    {
        var preset = _workspaceFiniteRollerDocument!.FiniteRollerPreset!;
        var layout = JsonSerializer.Serialize(CreateWorkspaceDocument());
        var draft = _simulatorShell!.CaptureIntegratedLadderDraft();
        check(_activeSceneDefinition!.PlcTestProfile is null, "finite_preset_has_no_external_profile");
        var guarded = _simulatorShell.VerifyExternalProfileGuards(out var guardResult);
        GD.Print($"WORKSPACE_ROLLER_EXTERNAL_GUARD {guardResult}");
        check(guarded, "finite_external_profile_rejected_before_transport");
        var points = ReadScenePoints(_activeSceneDefinition);
        foreach (var item in new[]
        {
            ("missing", PlcVariableRole.Input, "undeclared_workspace_point", PlcVariableType.Bool, "IO001"),
            ("type", PlcVariableRole.Input, preset.StartPoint, PlcVariableType.DInt, "IO002"),
            ("input_owner", PlcVariableRole.Input, preset.RunPoint, PlcVariableType.Bool, "IO003"),
            ("output_owner", PlcVariableRole.Output, preset.StartPoint, PlcVariableType.Bool, "IO003"),
            ("memory_owner", PlcVariableRole.Memory, preset.BeamPoint, PlcVariableType.Bool, "IO003"),
            ("duplicate_output", PlcVariableRole.Output, preset.RunPoint, PlcVariableType.Bool, "IO004")
        })
        {
            var document = new LadderEditorDocument();
            document.ResetProject("binding-qa", "Binding_QA", TimeSpan.FromMilliseconds(20));
            document.AddTag("run", PlcVariableRole.Output, preset.RunPoint);
            document.AddTag("candidate", item.Item2, item.Item3, item.Item4);
            document.AddRung("Binding fixture", "run");
            var issues = SceneIoBindingValidator.Validate(document.BuildProgram(), points);
            check(issues.Any(issue => issue.Code == item.Item5), $"finite_binding_rejects_{item.Item1}");
        }
        check(layout == JsonSerializer.Serialize(CreateWorkspaceDocument())
            && draft == _simulatorShell.CaptureIntegratedLadderDraft(),
            "finite_binding_and_profile_checks_preserve_layout_and_draft");
    }
}

