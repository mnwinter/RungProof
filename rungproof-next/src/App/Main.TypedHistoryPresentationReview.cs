using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyTypedHistoryPresentation;
    private async void VerifyTypedHistoryPresentation()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"TYPED_HISTORY_PRESENTATION_CHECK {name}={ok}");if(!ok)failures++;}
        try
        {
            var directory=ProjectSettings.GlobalizePath("res://.tools/typed-history-presentation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            foreach(var scene in new[]{"lab-10-04-motor-enum-state","lab-10-02-chicken-label-print","lab-10-01-drive-alarm-code-string"})
            foreach(var period in new[]{20,40,100})
            {
                StopActiveController();DisableVirtualController();
                AddMigratedScene(scene,_candidateCatalog!,_mainCamera!,false,false);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var path=Path.Combine(directory,scene+"-"+period+".rpproj.json");
                File.WriteAllText(path,LadderEditorProjectJson.Save(TypedLearnerDocument(scene,period,"intended")));
                Check(_simulatorShell!.OpenNativeMotionReviewProject(path,out var opened),"actual_disk_open_"+scene+"_"+period);GD.Print(opened);
                RunEditorDraftController();var timer=Stopwatch.StartNew();
                while(_virtualController is null||_virtualController.History.Count<2)
                {
                    if(timer.Elapsed.TotalSeconds>3)throw new InvalidOperationException("Typed history accepted scans timed out: "+scene);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                }
                var session=_virtualController!;var snapshot=JsonSerializer.Serialize(session.Snapshot);var points=JsonSerializer.Serialize(_sceneRuntime!.Points);var pose=FiveDemoPose();
                PauseActiveController();StepActiveController();
                Check(!ScanInspectionSupported&&!session.IsPaused&&JsonSerializer.Serialize(session.Snapshot)==snapshot&&JsonSerializer.Serialize(_sceneRuntime.Points)==points&&FiveDemoPose()==pose,"typed_pause_step_still_unsupported_no_mutation_"+scene+"_"+period);
                PublishScanInspectionState();
                Check(_simulatorShell.VerifyScanInspectionHistorySelection(0,out var selected),"actual_history_dialog_retained_row_selected_"+scene+"_"+period);
                var window=_simulatorShell.GetNode<Window>("AcceptedScanHistory");
                var body=window.GetChildren().OfType<VBoxContainer>().Single();var header=body.GetChildren().OfType<Label>().Single().Text;
                var editors=window.FindChildren("*","TextEdit",true,false).OfType<TextEdit>().ToArray();
                var buttons=_simulatorShell.FindChildren("*","Button",true,false).OfType<Button>().Where(b=>b.Name.ToString() is "PauseButton" or "StepScanButton").ToArray();
                Check(header.Contains("Read-only retained typed history",StringComparison.Ordinal)&&header.Contains("Pause/Step unsupported",StringComparison.Ordinal),"actual_header_distinguishes_readonly_typed_history_from_unsupported_pause_step_"+scene+"_"+period);
                Check(window.Visible&&editors.Length==1&&!editors[0].Editable&&selected.Contains("HISTORICAL ACCEPTED SCAN",StringComparison.Ordinal)&&buttons.Length==2&&buttons.All(b=>b.Disabled),"actual_readonly_history_visible_with_pause_step_disabled_"+scene+"_"+period);
                Check(JsonSerializer.Serialize(session.Snapshot)==snapshot&&JsonSerializer.Serialize(_sceneRuntime.Points)==points&&FiveDemoPose()==pose,"history_selection_no_live_snapshot_plant_pose_change_"+scene+"_"+period);
                GD.Print("TYPED_HISTORY_ACTUAL_HEADER "+scene+" "+period+"ms "+header.Replace('\n',' '));window.Hide();
                StopActiveController();
            }
        }
        catch(Exception error){failures++;GD.PushError("TYPED_HISTORY_PRESENTATION_EXCEPTION "+error);}
        GD.Print("TYPED_HISTORY_PRESENTATION_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
}
