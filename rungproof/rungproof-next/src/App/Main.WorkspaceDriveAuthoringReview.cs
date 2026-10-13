using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyWorkspaceDriveAuthoring;
    private async void VerifyWorkspaceDriveAuthoring()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"WORKSPACE_DRIVE_AUTHORING_CHECK {name}={ok}");if(!ok)failures++;}
        try
        {
            var requests=0;string? requestedId=null;
            void Requested(string id){requests++;requestedId=id;}
            _simulatorShell!.WorkspaceDriveConnectionRequested+=Requested;
            try
            {
                Button Button()=>_simulatorShell.GetNode<Button>("Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/ConnectBeltDrive");
                void Press()=>Button().EmitSignal(Godot.Button.SignalName.Pressed);
                var belt=_candidateCatalog!.Assets.Single(a=>a.Id==WorkspaceDriveConnectionPolicy.BeltAsset);
                StopActiveController();DisableVirtualController();
                AddMigratedScene("lab-5-01-delayed-lamp",_candidateCatalog,_mainCamera!,false,false);
                PlaceAsset(belt);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(!Button().Visible&&Button().Disabled,"disallowed_source_button_hidden_disabled");
                var before=JsonSerializer.Serialize(CreateWorkspaceDocument());Press();
                Check(requests==0&&JsonSerializer.Serialize(CreateWorkspaceDocument())==before,"disallowed_source_signal_emission_cannot_register");
                AddMigratedScene(WorkspaceDriveConnectionPolicy.SourceId,_candidateCatalog,_mainCamera!,false,false);
                PlaceAsset(belt);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var instance=_selectedPlacementId!;var draft=_simulatorShell.CaptureIntegratedLadderDraft();
                foreach(var invalidPose in new[]{"rotation","scale"})
                {
                    var position=_workspaceNodes[instance].Node.Position;
                    TransformPlacement(instance,position,invalidPose=="rotation"?new Vector3(0,90,0):Vector3.Zero,invalidPose=="scale"?new Vector3(1,2,1):Vector3.One);
                    Check(!Button().Visible||Button().Disabled,"actual_"+invalidPose+"_registration_button_unavailable");
                    var layout=JsonSerializer.Serialize(CreateWorkspaceDocument());var undo=JsonSerializer.Serialize(_undoHistory);var redo=JsonSerializer.Serialize(_redoHistory);var count=requests;
                    Press();
                    Check(requests==count&&JsonSerializer.Serialize(CreateWorkspaceDocument())==layout&&JsonSerializer.Serialize(_undoHistory)==undo&&JsonSerializer.Serialize(_redoHistory)==redo&&_simulatorShell.CaptureIntegratedLadderDraft()==draft,"actual_"+invalidPose+"_unavailable_press_no_graph_history_draft_mutation");
                    TransformPlacement(instance,position,Vector3.Zero,Vector3.One);
                }
                CreateSignalLink(instance,"run_command","conveyor_running");
                Check(!Button().Visible||Button().Disabled,"conflicting_generic_map_registration_button_unavailable");
                before=JsonSerializer.Serialize(CreateWorkspaceDocument());var conflictUndo=JsonSerializer.Serialize(_undoHistory);var conflictRedo=JsonSerializer.Serialize(_redoHistory);var conflictRequests=requests;
                Press();
                Check(requests==conflictRequests&&JsonSerializer.Serialize(CreateWorkspaceDocument())==before&&JsonSerializer.Serialize(_undoHistory)==conflictUndo&&JsonSerializer.Serialize(_redoHistory)==conflictRedo&&_simulatorShell.CaptureIntegratedLadderDraft()==draft,"conflicting_registration_press_no_graph_history_draft_mutation");
                DeleteSignalLink(_signalLinks.Single(l=>l.InstanceId==instance&&l.SignalId=="run_command").Id);
                requests=0;requestedId=null;
                Check(Button().Visible&&!Button().Disabled&&CreateWorkspaceDocument().DriveConnection is null,"eligible_selected_added_belt_explicit_button_available");
                Press();
                var document=CreateWorkspaceDocument();var relation=document.DriveConnection;
                Check(requests==1&&requestedId==instance&&relation is not null&&relation.InstanceId==instance&&document.Version==9,"actual_button_press_registers_exact_selected_belt_once");
                Check(_simulatorShell.CaptureIntegratedLadderDraft()==draft,"registration_preserves_current_learner_draft_no_solution_load");
                Check(relation is not null&&_sceneRuntime!.Points.ContainsKey(relation.RunPoint)&&_sceneRuntime.IsPlcOwnedPoint(relation.RunPoint)
                    &&_sceneRuntime.IsPcOwnedPoint(relation.RunningPoint)&&_sceneRuntime.IsPcOwnedPoint(relation.ActualSpeedPoint),"actual_registered_effective_declarations_have_exact_owners");
                Check(document.SignalLinks is not null&&document.SignalLinks.Count(l=>l.InstanceId==instance)==3&&WorkspaceDriveConnectionPolicy.Eligibility(document) is {CommandMapped:true,FeedbackMapped:true},"three_canonical_connections_registered");
                var summary = _simulatorShell.GetNode<Label>("Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/SignalMappingSummary");
                Check(summary.Text == "1 LIVE INPUT · 2 LIVE FEEDBACK · 0 AUTHORED ONLY", "actual_connected_summary_counts_measured_feedback");
                Check(Button().Visible&&Button().Disabled,"connected_button_disabled");Press();
                Check(requests==1&&JsonSerializer.Serialize(CreateWorkspaceDocument())==JsonSerializer.Serialize(document),"connected_press_cannot_duplicate_relation");
                var path=ProjectSettings.GlobalizePath("res://.tools/workspace-drive-authoring-"+Guid.NewGuid().ToString("N")+"/connected.rpworkspace.json");
                SaveWorkspaceToPath(path);
                var saved=WorkspaceProjectBundleRepository.LoadIfBundle(path,_candidateCatalog,_sceneCatalog!);
                Check(File.Exists(path)&&saved is not null&&saved.Workspace.DriveConnection==relation&&saved.LadderProjectJson==draft,"actual_authored_joint_save_preserves_relation_current_draft");
            }
            finally{_simulatorShell.WorkspaceDriveConnectionRequested-=Requested;}
        }
        catch(Exception error){failures++;GD.PushError("WORKSPACE_DRIVE_AUTHORING_EXCEPTION "+error);}
        GD.Print("WORKSPACE_DRIVE_AUTHORING_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
}
