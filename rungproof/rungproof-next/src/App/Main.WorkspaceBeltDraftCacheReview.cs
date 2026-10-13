using System;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    // Reproducer first: no schema/cache fixes are made by this fixture.
    private async Task VerifyWorkspaceBeltDraftCache(Action<bool,string> check)
    {
        var preset=WorkspaceFiniteBeltPolicy.Create();var p=preset.FiniteBeltPreset!;
        foreach(var invalid in new[]{false,true})
        {
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            AuthoredDemoLadderPrograms.TryCreate(preset.SourceSceneId,out var original);
            original.AddTag("original_cache_marker",PlcVariableRole.Memory);
            var originalBaseline=LadderEditorProjectJson.Save(original);
            if(invalid)original.AddRung("Original invalid marker","undeclared_original_output");
            var originalJson=LadderEditorProjectJson.Save(original);
            _simulatorShell!.RestoreIntegratedLadderDraft(originalJson,originalBaseline,true);
            var originalPath=ProjectSettings.GlobalizePath("res://.tools/original-draft-cache-"+invalid+".rpproj.json");
            check(_simulatorShell.PrepareRollbackEditorFixture(originalPath),"original_cache_real_path_history_prepared_"+invalid);
            originalJson=_simulatorShell.CaptureIntegratedLadderDraft();originalBaseline=_simulatorShell.CaptureIntegratedLadderSavedBaseline()!;
            var originalPointDomain=string.Join("|",_sceneRuntime!.Points.Keys);
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:preset);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var fresh=LadderEditorProjectJson.Load(_simulatorShell.CaptureIntegratedLadderDraft()).Document!;
            if(!invalid)
            {
            check(fresh.Rungs.Count==0&&_sceneRuntime!.Points.Count==3
                &&_sceneRuntime.Points.ContainsKey(p.StartPoint)&&_sceneRuntime.Points.ContainsKey(p.RunPoint)
                &&_sceneRuntime.Points.ContainsKey(p.BeamPoint),"finite_cache_namespace_has_blank_declared_point_draft_"+invalid);
            RunEditorDraftController();
            check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"empty_finite_cached_draft_cannot_run_"+invalid);
            }
            var finite=new LadderEditorDocument();finite.ResetProject("finite-cache-marker","Finite_Cache",TimeSpan.FromMilliseconds(20));finite.SourceSceneId=preset.SourceSceneId;
            finite.AddTag("start",PlcVariableRole.Input,p.StartPoint);finite.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);finite.AddTag("run",PlcVariableRole.Output,p.RunPoint);
            finite.AddRung("Finite custom marker","run");finite.AddContact(0,0,"start",false);
            var finiteBaseline=LadderEditorProjectJson.Save(finite);
            if(invalid)finite.AddRung("Finite invalid marker","undeclared_finite_output");
            var finiteJson=LadderEditorProjectJson.Save(finite);
            _simulatorShell.RestoreIntegratedLadderDraft(finiteJson,finiteBaseline,true);
            var finitePath=ProjectSettings.GlobalizePath("res://.tools/finite-draft-cache-"+invalid+".rpproj.json");
            check(_simulatorShell.PrepareRollbackEditorFixture(finitePath),"finite_cache_real_path_history_prepared_"+invalid);
            finiteJson=_simulatorShell.CaptureIntegratedLadderDraft();finiteBaseline=_simulatorShell.CaptureIntegratedLadderSavedBaseline()!;
            AddMigratedScene("lab-2-01-workstation-call",_candidateCatalog!,_mainCamera!,false,false);
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false);
            check(_simulatorShell.CaptureIntegratedLadderDraft()==originalJson
                &&_simulatorShell.CaptureIntegratedLadderSavedBaseline()==originalBaseline
                &&string.Join("|",_sceneRuntime!.Points.Keys)==originalPointDomain,
                "original_demo_exact_custom_draft_baseline_point_domain_restored_"+invalid);
            check(_simulatorShell.VerifyRollbackEditorFixture(originalPath,originalJson),"original_cache_path_history_undo_redo_restored_"+invalid);
            check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"original_navigation_does_not_implicitly_load_"+invalid);
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:preset);
            check(_simulatorShell.CaptureIntegratedLadderDraft()==finiteJson
                &&_simulatorShell.CaptureIntegratedLadderSavedBaseline()==finiteBaseline
                &&_sceneRuntime!.Points.Count==3,"finite_exact_custom_draft_baseline_namespace_restored_"+invalid);
            check(_simulatorShell.VerifyRollbackEditorFixture(finitePath,finiteJson),"finite_cache_path_history_undo_redo_restored_"+invalid);
            check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"finite_navigation_does_not_implicitly_load_"+invalid);
            if(invalid){RunEditorDraftController();check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"invalid_restored_finite_has_no_old_logic_fallback");}
        }
        GD.Print("FINITE_CACHE_SCOPE exactJSON_savedbaseline_pointdomain_navigation; actualpath_historyUndoRedo via existing editor fixtures");
    }
}