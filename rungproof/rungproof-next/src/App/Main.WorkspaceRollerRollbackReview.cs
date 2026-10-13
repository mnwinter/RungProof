using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private async Task VerifyWorkspaceRollerRollback(Action<bool,string> check)
    {
        var preset=WorkspaceFiniteRollerPolicy.Create();var p=preset.FiniteRollerPreset!;
        foreach(var boundary in new[]{"after-compose","after-draft"})
        foreach(var state in new[]{"valid","invalid"})
        {
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteRollerWorkspace:preset);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var document=new LadderEditorDocument();document.ResetProject("roller-rollback-"+state,"Finite_Rollback_"+state,TimeSpan.FromMilliseconds(20));document.SourceSceneId=preset.SourceSceneId;
            if(state=="valid")
            {
                document.AddTag("start",PlcVariableRole.Input,p.StartPoint);document.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);document.AddTag("run",PlcVariableRole.Output,p.RunPoint);
                document.AddRung("Custom rollback rung","run");document.AddContact(0,0,"start",false);document.AddContact(0,0,"beam",true);
            }
            _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));
            var incoming=ProjectSettings.GlobalizePath("res://.tools/roller-rollback-incoming-"+state+".rpworkspace.json");
            SaveWorkspaceToPath(incoming);
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();
            var oldRoller=_workspaceNodes[p.RollerId].Item2.Position;
            TryTransformFiniteRollerWorkspace(p.RollerId,oldRoller+new Vector3(.4f,0,.3f),Vector3.Zero,Vector3.One);
            var historyPath=ProjectSettings.GlobalizePath("res://.tools/roller-rollback-editor-"+state+".rpproj.json");
            check(_simulatorShell.PrepareRollbackEditorFixture(historyPath),"finite_rollback_actual_opened_path_undoable_edit_"+state+"_"+boundary);
            RefreshWorkspaceDirty();
            var layout=JsonSerializer.Serialize(CreateWorkspaceDocument());var draft=_simulatorShell.CaptureIntegratedLadderDraft();
            var savedLayout=_workspaceSavedSnapshotJson;var savedDraft=_workspaceSavedLadderJson;
            var editorBaseline=_simulatorShell.CaptureIntegratedLadderSavedBaseline();var dirty=_simulatorShell.IsWorkspaceDirty;
            if(state=="valid")RunEditorDraftController();
            _injectWorkspaceReplacementFailure=boundary;_workspaceReplacementAuthorized=true;
            try { LoadWorkspaceFromPath(incoming); }
            finally { _workspaceReplacementAuthorized=false; }
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            check(_injectWorkspaceReplacementFailure is null&&_sceneRuntime!.HasWorkspaceRollerPreset
                &&_currentSceneId==preset.SourceSceneId&&JsonSerializer.Serialize(CreateWorkspaceDocument())==layout
                &&_workspaceNodes.Count==4&&new[]{p.RollerId,p.PalletId,p.SensorId,p.OperatorId}.All(id=>_workspaceNodes.ContainsKey(id))
                &&CreateWorkspaceDocument().FiniteRollerPreset==p&&CreateWorkspaceDocument().SignalLinks!.Count==3,
                "finite_rollback_actual_"+boundary+"_preserves_four_ids_relation_links_"+state);
            check(_simulatorShell.CaptureIntegratedLadderDraft()==draft&&_virtualController is null,
                "finite_rollback_preserves_"+state+"_custom_draft_stopped_"+boundary);
            check(_workspaceSavedSnapshotJson==savedLayout&&_workspaceSavedLadderJson==savedDraft
                &&_simulatorShell.CaptureIntegratedLadderSavedBaseline()==editorBaseline&&_simulatorShell.IsWorkspaceDirty==dirty,
                "finite_rollback_saved_baselines_dirty_"+state+"_"+boundary);
            check(_simulatorShell.VerifyRollbackEditorFixture(historyPath,draft),"finite_rollback_exact_path_functional_undo_redo_"+state+"_"+boundary);
            var adapter=_sceneCompositionRoot!.GetNode<Node3D>(p.RollerId).GetNode<RollerConveyorController>("WorkspaceRuntimeAdapter");
            var before=_sceneCompositionRoot.GetNode<Node3D>(p.PalletId).Position;
            _sceneRuntime!.AdvanceSimulation(.5);
            check(!adapter.IsPhysicsProcessing()&&_sceneCompositionRoot.GetNode<Node3D>(p.PalletId).Position==before,
                "finite_rollback_no_old_autonomy_or_implicit_resume_"+state+"_"+boundary);
        }
    }
}

