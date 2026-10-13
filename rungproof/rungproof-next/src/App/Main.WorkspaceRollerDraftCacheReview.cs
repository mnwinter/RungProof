using System;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private async Task VerifyWorkspaceRollerDraftCache(Action<bool,string> check)
    {
        var roller=WorkspaceFiniteRollerPolicy.Create();var belt=WorkspaceFiniteBeltPolicy.Create();
        foreach(var invalid in new[]{false,true})
        {
            var drafts=new string[3];var baselines=new string?[3];var paths=new string[3];var domains=new string[3];
            void Open(int kind)=>AddMigratedScene(roller.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:kind==1?belt:null,finiteRollerWorkspace:kind==2?roller:null);
            for(var kind=0;kind<3;kind++)
            {
                Open(kind);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var doc=new LadderEditorDocument();doc.ResetProject($"three-cache-{kind}",$"Cache_{kind}",TimeSpan.FromMilliseconds(20));doc.SourceSceneId=roller.SourceSceneId;
                if(kind==0){AuthoredDemoLadderPrograms.TryCreate(roller.SourceSceneId,out var original);doc=original;}
                doc.AddTag("marker_"+kind,PlcVariableRole.Memory);
                if(kind!=0){var output=kind==1?belt.FiniteBeltPreset!.RunPoint:roller.FiniteRollerPreset!.RunPoint;doc.AddTag("run",PlcVariableRole.Output,output);doc.AddRung("Cache custom marker "+kind,"run");}
                var baseline=LadderEditorProjectJson.Save(doc);if(invalid)doc.AddRung("Invalid marker "+kind,"undeclared_cache_output");
                _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(doc),baseline,true);
                paths[kind]=ProjectSettings.GlobalizePath($"res://.tools/roller-three-cache-{kind}-{invalid}.rpproj.json");
                check(_simulatorShell.PrepareRollbackEditorFixture(paths[kind]),$"three_way_cache_history_fixture_{kind}_{invalid}");
                drafts[kind]=_simulatorShell.CaptureIntegratedLadderDraft();baselines[kind]=_simulatorShell.CaptureIntegratedLadderSavedBaseline();domains[kind]=string.Join("|",_sceneRuntime!.Points.Keys);
            }
            foreach(var kind in new[]{0,1,2,1,0,2})
            {
                Open(kind);
                check(_simulatorShell!.CaptureIntegratedLadderDraft()==drafts[kind]&&_simulatorShell.CaptureIntegratedLadderSavedBaseline()==baselines[kind]&&string.Join("|",_sceneRuntime!.Points.Keys)==domains[kind],$"three_way_same_source_exact_draft_baseline_domain_{kind}_{invalid}");
                check(_simulatorShell.VerifyRollbackEditorFixture(paths[kind],drafts[kind]),$"three_way_same_source_path_undo_redo_{kind}_{invalid}");
                check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,$"three_way_navigation_stopped_{kind}_{invalid}");
            }
        }
    }
}
