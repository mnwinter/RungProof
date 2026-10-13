using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyTemplateProjectWorkflow(Action<bool,string> check)
    {
        foreach(var id in new[]{"lab-2-01-workstation-call","lab-4-01-press-count-lamp","lab-5-01-delayed-lamp"})
        {
            AddMigratedScene(id,_candidateCatalog!,_mainCamera!,false,false);
            check(_simulatorShell!.VerifyActiveScenarioIdentity(),id+"_selected_scenario_exact_id_and_canonical_alias");
            var support=_sceneCompositionRoot!.GetNode<Node3D>("ControlInstallation");
            var original=support.Transform;
            var heads=_activeSceneDefinition!.Equipment.ToDictionary(e=>e.Id,e=>_sceneCompositionRoot!.GetNode<Node3D>(e.Id).Transform);
            check(_simulatorShell!.VerifyTemplateAssemblyUiEdit(.40f,15),id+"_workspace_row_and_inspector_edit");
            var edited=_sceneCompositionRoot!.GetNode<Node3D>("ControlInstallation").Transform;
            var delta=edited*original.AffineInverse();
            check(Mathf.IsZeroApprox(edited.Origin.Y),id+"_assembly_remains_grounded");
            check(heads.All(h=>_sceneCompositionRoot!.GetNode<Node3D>(h.Key).Transform.IsEqualApprox(delta*h.Value)),id+"_original_heads_move_atomically_with_support");
            check(_simulatorShell!.IsWorkspaceDirty,id+"_template_edit_is_unsaved");
            UndoWorkspace();UndoWorkspace();
            check(_sceneCompositionRoot!.GetNode<Node3D>("ControlInstallation").Transform.IsEqualApprox(original),id+"_undo_restores_whole_assembly");
            RedoWorkspace();RedoWorkspace();
            check(_sceneCompositionRoot!.GetNode<Node3D>("ControlInstallation").Transform.IsEqualApprox(edited),id+"_redo_restores_whole_assembly");
            var path=ProjectSettings.GlobalizePath("res://.tools/template-project-"+id+".json");
            SaveWorkspaceToPath(path);
            var bundle=WorkspaceProjectBundleRepository.LoadIfBundle(path,_candidateCatalog!,_sceneCatalog!);
            check(bundle?.Workspace.TemplateOverride is not null,id+"_joint_save_contains_override_and_actual_draft");
            LoadWorkspaceFromPath(path);
            check(_sceneCompositionRoot!.GetNode<Node3D>("ControlInstallation").Transform.IsEqualApprox(edited),id+"_joint_open_restores_assembly");
            check(_simulatorShell!.CaptureIntegratedLadderDraft()==bundle!.LadderProjectJson&&_virtualController is null,id+"_joint_open_restores_exact_draft_stopped");
        }
        // A deliberately compiler-invalid but readable real editor document
        // must round-trip in the same project, never replaced with a solution.
        const string sceneId="lab-4-01-press-count-lamp";
        AddMigratedScene(sceneId,_candidateCatalog!,_mainCamera!,false,false);
        _simulatorShell!.VerifyTemplateAssemblyUiEdit(.25f,0);
        var document=new LadderEditorDocument();
        document.ResetProject("bundle-valid","Bundle_Valid",TimeSpan.FromMilliseconds(20));document.SourceSceneId=sceneId;
        document.AddTag("pulse_received",PlcVariableRole.Input,"pulse_received");
        document.AddTag("threshold_lamp",PlcVariableRole.Output,"threshold_lamp");
        document.AddRung("Lamp follows actual scene input","threshold_lamp");document.AddContact(0,0,"pulse_received",false);
        _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));
        RefreshWorkspaceDirty();check(_simulatorShell!.IsWorkspaceDirty,"actual_draft_change_marks_joint_project_unsaved");
        var customDraft=_simulatorShell!.CaptureIntegratedLadderDraft();
        _simulatorShell.VerifyTemplateAssemblyUiEdit(.10f,5);
        check(_simulatorShell.CaptureIntegratedLadderDraft()==customDraft&&_virtualController is null,"custom_valid_draft_preserved_by_template_edit_stopped");
        UndoWorkspace();UndoWorkspace();RedoWorkspace();RedoWorkspace();
        check(_simulatorShell.CaptureIntegratedLadderDraft()==customDraft&&_virtualController is null,"custom_valid_draft_preserved_by_template_undo_redo");
        var validPath=ProjectSettings.GlobalizePath("res://.tools/template-project-valid.json");
        SaveWorkspaceToPath(validPath);LoadWorkspaceFromPath(validPath);
        check(_simulatorShell!.TryBuildCurrentLadderProgram(out _,out _)&&_virtualController is null,"valid_joint_draft_opened_without_implicit_execution");
        RunEditorDraftController();
        check(_virtualController?.Snapshot.State==VirtualControllerState.Running,"opened_valid_current_draft_can_run");
        _sceneRuntime!.ExecuteAction("toggle-pulse_received");
        _PhysicsProcess(.02);
        GD.Print($"DEVICE_TRACE_ACCEPTED_FIXTURE scan={_virtualController!.Snapshot.ScanNumber} input={_virtualController.Snapshot.Variables["pulse_received"]} output={_virtualController.Snapshot.Variables["threshold_lamp"]} elements={_virtualController.Snapshot.Elements.Count}");
        check(_virtualController!.Snapshot.ScanNumber>0&&_virtualController.Snapshot.Variables["pulse_received"]&&_virtualController.Snapshot.Elements.Count>0&&_virtualController.Snapshot.Variables["threshold_lamp"],"device_trace_actual_accepted_scan_known_output");
        check(_simulatorShell!.VerifyDeviceTraceSelector("indicator_1",true),"device_selector_actual_writer_navigates_current_loaded_rung");
        StopActiveController();
        document.ResetProject("bundle-invalid","Bundle_Invalid",TimeSpan.FromMilliseconds(20));document.SourceSceneId=sceneId;
        _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));
        check(_simulatorShell!.VerifyDeviceTraceSelector("indicator_1",false),"device_trace_invalid_draft_disables_navigation");
        var invalidJson=_simulatorShell!.CaptureIntegratedLadderDraft();
        _simulatorShell.VerifyTemplateAssemblyUiEdit(.10f,5);
        UndoWorkspace();UndoWorkspace();RedoWorkspace();RedoWorkspace();
        check(_simulatorShell.CaptureIntegratedLadderDraft()==invalidJson&&_virtualController is null,"readable_invalid_draft_preserved_by_edit_undo_redo_stopped");
        var invalidPath=ProjectSettings.GlobalizePath("res://.tools/template-project-invalid.json");
        SaveWorkspaceToPath(invalidPath);LoadWorkspaceFromPath(invalidPath);RunEditorDraftController();
        check(_simulatorShell!.CaptureIntegratedLadderDraft()==invalidJson&&_virtualController is null,"invalid_joint_draft_roundtrips_and_cannot_run");
        var before=JsonSerializer.Serialize(CreateWorkspaceDocument());var beforeDraft=_simulatorShell!.CaptureIntegratedLadderDraft();
        var malformedPath=ProjectSettings.GlobalizePath("res://.tools/template-project-malformed.json");
        System.IO.File.WriteAllText(malformedPath,"{\"kind\":\"rungproof-integrated-project\",\"version\":1,\"workspace\":null,\"ladderProjectJson\":\"broken\"}");
        LoadWorkspaceFromPath(malformedPath);
        check(JsonSerializer.Serialize(CreateWorkspaceDocument())==before&&_simulatorShell!.CaptureIntegratedLadderDraft()==beforeDraft,"malformed_joint_project_rejected_before_scene_or_draft_replacement");
        var value=CreateWorkspaceDocument().TemplateOverride!;
        var stale=value with { SourceFingerprint=new string('0',64) };
        var rejected=false;
        try{WorkspaceTemplatePolicy.Validate(_activeSceneDefinition!,stale);}catch(InvalidOperationException){rejected=true;}
        check(rejected,"stale_template_source_rejected");
        foreach(var boundary in new[]{"after-compose","after-draft"})
        foreach(var state in new[]{"valid","invalid"})
        {
            if(state=="valid")
            {
                document.ResetProject("rollback-valid","Rollback_Valid",TimeSpan.FromMilliseconds(20));document.SourceSceneId=sceneId;
                document.AddTag("pulse_received",PlcVariableRole.Input,"pulse_received");document.AddTag("threshold_lamp",PlcVariableRole.Output,"threshold_lamp");
                document.AddRung("Custom rollback draft","threshold_lamp");document.AddContact(0,0,"pulse_received",false);
            }
            else { document.ResetProject("rollback-invalid","Rollback_Invalid",TimeSpan.FromMilliseconds(20));document.SourceSceneId=sceneId; }
            _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));RefreshWorkspaceDirty();
            var historyPath=ProjectSettings.GlobalizePath("res://.tools/rollback-editor-"+state+".json");
            check(_simulatorShell.PrepareRollbackEditorFixture(historyPath),"rollback_real_opened_path_and_undoable_edit_"+state+"_"+boundary);
            RefreshWorkspaceDirty();
            var oldLayout=JsonSerializer.Serialize(CreateWorkspaceDocument());var oldDraft=_simulatorShell.CaptureIntegratedLadderDraft();
            var oldSaved=_workspaceSavedSnapshotJson;var oldSavedDraft=_workspaceSavedLadderJson;var oldDirty=_simulatorShell.IsWorkspaceDirty;
            var oldEditorBaseline=_simulatorShell.CaptureIntegratedLadderSavedBaseline();
            if(state=="valid")RunEditorDraftController();
            _injectWorkspaceReplacementFailure=boundary;_workspaceReplacementAuthorized=true;
            try { LoadWorkspaceFromPath(ProjectSettings.GlobalizePath("res://.tools/template-project-lab-2-01-workstation-call.json")); }
            finally { _workspaceReplacementAuthorized=false; }
            check(_injectWorkspaceReplacementFailure is null&&_currentSceneId==sceneId&&JsonSerializer.Serialize(CreateWorkspaceDocument())==oldLayout
                &&_simulatorShell.CaptureIntegratedLadderDraft()==oldDraft&&_virtualController is null,"actual_"+boundary+"_failure_restores_"+state+"_scene_layout_draft_stopped");
            check(_workspaceSavedSnapshotJson==oldSaved&&_workspaceSavedLadderJson==oldSavedDraft&&_simulatorShell.IsWorkspaceDirty==oldDirty
                &&_simulatorShell.CaptureIntegratedLadderSavedBaseline()==oldEditorBaseline,"rollback_preserves_"+state+"_"+boundary+"_saved_baselines_and_dirty_identity");
            check(_simulatorShell.VerifyRollbackEditorFixture(historyPath,oldDraft),"rollback_exact_open_path_and_functional_undo_redo_"+state+"_"+boundary);
        }
    }
}
