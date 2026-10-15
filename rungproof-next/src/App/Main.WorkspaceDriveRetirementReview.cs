using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyWorkspaceDriveRetirement;
    private async void VerifyWorkspaceDriveRetirement()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"WORKSPACE_DRIVE_RETIREMENT_CHECK {name}={ok}");if(!ok)failures++;}
        var directory=ProjectSettings.GlobalizePath("res://.tools/workspace-drive-retirement-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        const string instance="placement-001-retirement-belt";
        try
        {
            await VerifyWorkspaceDriveRegistrationRollback(Check,directory);
            var source=SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(s=>s.Id==WorkspaceDriveConnectionPolicy.SourceId));
            var relation=WorkspaceDriveConnectionPolicy.CreateRelation(source,instance);
            var workspace=new WorkspaceDocument(9,source.Id,
                new[]{new WorkspacePlacement(instance,WorkspaceDriveConnectionPolicy.BeltAsset,new double[]{0,0,3},new double[]{0,0,0},new double[]{1,1,1})},
                SignalLinks:new[]{new WorkspaceSignalLink("run",instance,"run_command",relation.RunPoint),new WorkspaceSignalLink("running",instance,"running",relation.RunningPoint),new WorkspaceSignalLink("speed",instance,"actual_speed",relation.ActualSpeedPoint)},DriveConnection:relation);
            foreach(var period in new[]{20,40,100})
            {
                await OpenRunning(period);
                var oldNode=_workspaceNodes[instance].Node;
                var draft=_simulatorShell!.CaptureIntegratedLadderDraft();var oldSession=_virtualController!;
                DeleteSignalLink(_signalLinks.Single(l=>l.InstanceId==instance&&l.SignalId=="run_command").Id);
                UndoWorkspace();
                var replacement=_workspaceNodes[instance].Node;var adapter=replacement.GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                Check(replacement!=oldNode&&_workspaceDriveNode==replacement&&_workspaceDriveAdapter==adapter&&_workspaceDriveRuntime==_sceneRuntime&&WorkspaceDriveIdentityMatches,"actual_undo_rebuild_owns_replacement_node_adapter_runtime_"+period);
                Check(_simulatorShell.CaptureIntegratedLadderDraft()==draft&&CreateWorkspaceDocument().DriveConnection==relation&&WorkspaceDriveConnectionPolicy.Eligibility(CreateWorkspaceDocument()).CommandMapped,"actual_undo_preserves_draft_relation_exact_command_map_"+period);
                Check(oldSession.Snapshot.State==VirtualControllerState.Stopped&&(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running)&&adapter.ActualSpeedMps==0,"undo_replacement_retires_old_execution_new_drive_stopped_"+period);
                RunEditorDraftController();await Scans(4);
                Check(adapter.ActualSpeedMps>0&&!adapter.IsPhysicsProcessing()&&_sceneRuntime!.Points[relation.ActualSpeedPoint] is double,"explicit_current_run_after_undo_drives_new_identity_"+period);

                // QueueFree changes eligibility immediately; no UI refresh or physics
                // frame is allowed between invalidation and the scheduling callback.
                var scan=_virtualController!.Snapshot.ScanNumber;var time=_virtualController.Snapshot.SimulatedTime;
                var pose=WorkspaceDriveRetirementPose();var originalPosition=_sceneRuntime!.Points["object_position"];
                adapter.QueueFree();_PhysicsProcess(period/1000d);
                Check(_virtualController.Snapshot.ScanNumber==scan&&_virtualController.Snapshot.SimulatedTime==time&&_virtualController.Snapshot.State==VirtualControllerState.Stopped,"queued_adapter_rejected_before_any_accepted_scan_without_ui_refresh_"+period);
                Check(WorkspaceDriveRetirementPose()==pose&&Equals(_sceneRuntime.Points["object_position"],originalPosition)&&_sceneRuntime.Points[relation.RunningPoint] is null&&_sceneRuntime.Points[relation.ActualSpeedPoint] is null,"queued_adapter_no_partial_original_or_added_plant_tick_feedback_unavailable_"+period);
                // Compose the next fixture synchronously before a queued adapter is
                // freed, avoiding stale references in the QA itself.
                await OpenRunning(period);
                var deleting=_workspaceNodes[instance].Node;var deletingAdapter=deleting.GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                oldSession=_virtualController!;scan=oldSession.Snapshot.ScanNumber;time=oldSession.Snapshot.SimulatedTime;
                var deletedDraft=_simulatorShell!.CaptureIntegratedLadderDraft();
                DeletePlacement(instance);_PhysicsProcess(period/1000d);
                Check(!_workspaceNodes.ContainsKey(instance)&&oldSession.Snapshot.State==VirtualControllerState.Stopped&&oldSession.Snapshot.ScanNumber==scan&&oldSession.Snapshot.SimulatedTime==time,"actual_target_delete_retires_old_session_before_scan_"+period);
                Check(!WorkspaceDriveIdentityMatches&&(_workspaceDriveRelation is null||!WorkspaceDriveScanEligible)&&deletingAdapter.ActualSpeedMps==0,"actual_target_delete_cannot_leave_running_invalid_relation_"+period);
                var deletedDocument=CreateWorkspaceDocument();var schemaValid=true;
                try{WorkspaceRepository.ValidateDocument(deletedDocument,_candidateCatalog!,_sceneCatalog!);}
                catch(InvalidOperationException error){schemaValid=false;GD.Print("WORKSPACE_DRIVE_DELETE_SCHEMA "+error.Message);}
                Check(schemaValid&&deletedDocument.DriveConnection is null,"target_delete_leaves_valid_generic_layout_without_relation_"+period);
                Check(new[]{relation.RunPoint,relation.RunningPoint,relation.ActualSpeedPoint}.All(p=>!_sceneRuntime!.Points.ContainsKey(p))&&_simulatorShell.CaptureIntegratedLadderDraft()==deletedDraft&&!_simulatorShell.TryBuildCurrentLadderProgram(out _,out _),"target_delete_removes_derived_points_preserves_unresolved_current_draft_"+period);
                UndoWorkspace();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var restored=_workspaceNodes[instance].Node;var restoredAdapter=restored.GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                Check(restored!=deleting&&CreateWorkspaceDocument().DriveConnection==relation&&WorkspaceDriveIdentityMatches&&restoredAdapter.ActualSpeedMps==0&&(_virtualController is null||_virtualController.Snapshot.State==VirtualControllerState.Stopped)&&_simulatorShell.CaptureIntegratedLadderDraft()==deletedDraft,"undo_target_delete_restores_version9_new_identity_stopped_exact_draft_"+period);
                RunEditorDraftController();await Scans(4);
                Check(restoredAdapter.ActualSpeedMps>0&&!restoredAdapter.IsPhysicsProcessing(),"undo_target_delete_explicit_current_run_moves_restored_drive_"+period);
                RedoWorkspace();
                var deletePath=Path.Combine(directory,"deleted-"+period+".rpworkspace.json");SaveWorkspaceToPath(deletePath);
                Check(File.Exists(deletePath),"actual_target_deleted_generic_layout_save_succeeds_"+period);
                if(File.Exists(deletePath))
                {
                    var saved=WorkspaceProjectBundleRepository.LoadIfBundle(deletePath,_candidateCatalog!,_sceneCatalog!);
                    Check(saved is not null&&saved.Workspace.DriveConnection is null&&saved.LadderProjectJson==deletedDraft,"deleted_joint_preserves_invalid_binding_draft_without_relation_"+period);
                    MarkWorkspaceSaved();LoadWorkspaceFromPath(deletePath);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    RunEditorDraftController();
                    Check(CreateWorkspaceDocument().DriveConnection is null&&_simulatorShell.CaptureIntegratedLadderDraft()==deletedDraft&&!_simulatorShell.TryBuildCurrentLadderProgram(out _,out _)&&(_virtualController is null||_virtualController.Snapshot.State==VirtualControllerState.Stopped),"deleted_joint_actual_reopen_unresolved_draft_blocks_stale_fallback_"+period);
                }

                foreach(var invalid in new[]{false,true})
                {
                    await OpenRunning(period);
                    adapter=_workspaceNodes[instance].Node.GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                    var changed=LadderEditorProjectJson.Load(_simulatorShell.CaptureIntegratedLadderDraft()).Document!;
                    if(invalid)changed.AddRung("Unresolved current output","missing_current_output");
                    else changed.AddTag("changed_execution_memory",PlcVariableRole.Memory,initialValue:true);
                    var json=LadderEditorProjectJson.Save(changed);_simulatorShell.RestoreIntegratedLadderDraft(json);
                    Check(_simulatorShell.TryBuildCurrentLadderProgram(out _,out _)!=invalid,"changed_fixture_expected_compile_validity_"+period+"_invalid"+invalid);
                    scan=_virtualController!.Snapshot.ScanNumber;time=_virtualController.Snapshot.SimulatedTime;pose=WorkspaceDriveRetirementPose();
                    DeleteSignalLink(_signalLinks.Single(l=>l.InstanceId==instance&&l.SignalId=="run_command").Id);
                    _PhysicsProcess(period/1000d);
                    Check(_virtualController.Snapshot.State==VirtualControllerState.Stopped&&_virtualController.Snapshot.ScanNumber==scan&&_virtualController.Snapshot.SimulatedTime==time&&adapter.ActualSpeedMps==0&&WorkspaceDriveRetirementPose()==pose,"command_detach_changed_current_identity_never_accepts_coast_tick_"+period+"_invalid"+invalid);
                    Check(_simulatorShell.CaptureIntegratedLadderDraft()==json,"changed_current_draft_not_repaired_or_replaced_"+period+"_invalid"+invalid);
                }
            }

            async Task OpenRunning(int period)
            {
                StopActiveController();DisableVirtualController();MarkWorkspaceSaved();
                var draft=new LadderEditorDocument();draft.ResetProject("retirement-drive-"+period,"Drive_Main",TimeSpan.FromMilliseconds(period));draft.SourceSceneId=source.Id;
                draft.AddTag("enable",PlcVariableRole.Memory,initialValue:true);draft.AddTag("run",PlcVariableRole.Output,relation.RunPoint);
                draft.AddTag("running",PlcVariableRole.Input,relation.RunningPoint);draft.AddTag("speed",PlcVariableRole.Input,relation.ActualSpeedPoint,PlcVariableType.Real);
                draft.AddRung("Actual learner enable","run");draft.AddContact(0,0,"enable",false);
                var path=Path.Combine(directory,"current-"+Guid.NewGuid().ToString("N")+".integrated.json");
                WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,workspace,LadderEditorProjectJson.Save(draft)),path,_candidateCatalog!,_sceneCatalog);
                LoadWorkspaceFromPath(path);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                _simulatorShell!.DismissIntegratedLoadConfirmationForReview();RunEditorDraftController();await Scans(4);
                if(!WorkspaceDriveIdentityMatches||_workspaceDriveAdapter!.ActualSpeedMps<=0)throw new InvalidOperationException("Saved current draft did not establish an actual moving drive.");
            }
            async Task Scans(int count)
            {
                var session=_virtualController??throw new InvalidOperationException("Current draft has no session.");var target=session.Snapshot.ScanNumber+count;var timer=Stopwatch.StartNew();
                while(session.Snapshot.ScanNumber<target)
                {if(timer.Elapsed.TotalSeconds>5)throw new InvalidOperationException("Retirement fixture accepted scan timeout.");await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            }
        }
        catch(Exception error){failures++;GD.PushError("WORKSPACE_DRIVE_RETIREMENT_EXCEPTION "+error);}
        GD.Print("WORKSPACE_DRIVE_RETIREMENT_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
    private async Task VerifyWorkspaceDriveRegistrationRollback(Action<bool,string> check,string directory)
    {
        StopActiveController();DisableVirtualController();
        AddMigratedScene(WorkspaceDriveConnectionPolicy.SourceId,_candidateCatalog!,_mainCamera!,false,false);
        var asset=_candidateCatalog!.Assets.Single(a=>a.Id==WorkspaceDriveConnectionPolicy.BeltAsset);
        PlaceAsset(asset);var instance=_selectedPlacementId!;
        PlaceAsset(asset);UndoWorkspace();SelectPlacement(instance);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var editorPath=Path.Combine(directory,"rollback-current.rpproj.json");
        check(_simulatorShell!.PrepareRollbackEditorFixture(editorPath),"registration_rollback_actual_editor_path_undoable_fixture");
        RefreshWorkspaceDirty();
        var layout=JsonSerializer.Serialize(CreateWorkspaceDocument());var draft=_simulatorShell.CaptureIntegratedLadderDraft();
        var savedLayout=_workspaceSavedSnapshotJson;var savedDraft=_workspaceSavedLadderJson;
        var editorBaseline=_simulatorShell.CaptureIntegratedLadderSavedBaseline();var dirty=_simulatorShell.IsWorkspaceDirty;
        var undo=JsonSerializer.Serialize(_undoHistory);var redo=JsonSerializer.Serialize(_redoHistory);
        var button=_simulatorShell.GetNode<Button>("Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/ConnectBeltDrive");
        check(button.Visible&&!button.Disabled&&_undoHistory.Count>0&&_redoHistory.Count>0,"registration_rollback_actual_button_and_nonempty_undo_redo_ready");
        _injectWorkspaceDriveRegistrationFailure=true;
        try{button.EmitSignal(Button.SignalName.Pressed);}
        finally
        {
            check(!_injectWorkspaceDriveRegistrationFailure,"actual_button_registration_consumes_post_rebuild_failure");
            _injectWorkspaceDriveRegistrationFailure=false;
        }
        check(JsonSerializer.Serialize(CreateWorkspaceDocument())==layout&&_simulatorShell.CaptureIntegratedLadderDraft()==draft&&(_virtualController is null||_virtualController.Snapshot.State==VirtualControllerState.Stopped),"failed_actual_button_registration_restores_exact_layout_draft_stopped");
        check(_workspaceSavedSnapshotJson==savedLayout&&_workspaceSavedLadderJson==savedDraft&&_simulatorShell.CaptureIntegratedLadderSavedBaseline()==editorBaseline&&_simulatorShell.IsWorkspaceDirty==dirty,"failed_registration_restores_saved_baselines_dirty");
        check(JsonSerializer.Serialize(_undoHistory)==undo&&JsonSerializer.Serialize(_redoHistory)==redo,"failed_registration_restores_exact_workspace_undo_redo");
        check(_simulatorShell.VerifyRollbackEditorFixture(editorPath,draft),"failed_registration_exact_editor_path_functional_undo_redo");
    }
    private string WorkspaceDriveRetirementPose()=>string.Join("|",_sceneCompositionRoot!.FindChildren("*","Node3D",true,false).OfType<Node3D>().Select(n=>n.GetPath()+":"+n.Transform));
}
