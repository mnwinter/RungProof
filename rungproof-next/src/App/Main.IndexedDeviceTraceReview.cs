using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private static LadderEditorDocument CreateIndexedTraceQaDocument()
    {
        const string sceneId="lab-10-06-ten-motor-array-startup";
        var document=new LadderEditorDocument();
        document.ResetProject("indexed-trace-qa","IndexedTrace_QA",TimeSpan.FromMilliseconds(20));
        document.SourceSceneId=sceneId;
        document.AddTag("request",PlcVariableRole.Input,"group_start_request");
        document.AddAggregateTag("zones",PlcVariableRole.Output,PlcVariableType.Array,
            new(ElementType:PlcVariableType.Bool,LowerBound:0,Length:10),new bool[10],"motor_commands");
        document.AddRung("Zone zero writer","zones[0]");document.AddContact(0,0,"request",false);
        document.AddRung("Zone zero reader and nine writer","zones[9]");document.AddContact(1,0,"zones[0]",false);
        return document;
    }
    // One cyclic 20 ms QA program: no indirect-index or root-wide-write claim.
    private async Task VerifyIndexedDeviceTraceWorkflow(Action<bool,string> check)
    {
        const string sceneId="lab-10-06-ten-motor-array-startup";
        AddMigratedScene(sceneId,_candidateCatalog!,_mainCamera!,false,false);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var document=CreateIndexedTraceQaDocument();
        var path=ProjectSettings.GlobalizePath("res://.tools/indexed-device-trace-qa.rpproj.json");
        var original=LadderEditorProjectJson.Save(document);File.WriteAllText(path,original);
        check(_simulatorShell!.OpenNativeMotionReviewProject(path,out var opened),"indexed_trace_real_shell_open");GD.Print(opened);
        RunEditorDraftController();
        void Scan()=>_virtualController!.Advance(.020,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,_sceneRuntime!.AdvanceSimulation,
            SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
        foreach(var expected in new[]{true,false})
        {
            check(ExecuteSelectedControllerAction("toggle-group_start_request"),"indexed_trace_physical_request_action");Scan();
            var accepted=_virtualController!.Snapshot;
            check(accepted.Variables["zones[0]"]==expected&&accepted.Variables["zones[9]"]==expected,"indexed_trace_actual_true_false_image");
            check(_simulatorShell.VerifyIndexedDeviceTrace("motor_0","zones[0]",[LadderReferenceKind.Read,LadderReferenceKind.Write],accepted),"indexed_trace_motor0_exact_reader_writer_navigation");
            check(_simulatorShell.VerifyIndexedDeviceTrace("motor_9","zones[9]",[LadderReferenceKind.Write],accepted),"indexed_trace_motor9_only_own_writer_navigation");
        }
        var lastAccepted=_virtualController!.Snapshot;
        // Change valid semantics without loading; old accepted image cannot describe it.
        document.AddRung("Changed valid unaccepted writer","zones[8]");document.AddContact(2,0,"request",false);
        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document),null,true);
        check(_simulatorShell.VerifyIndexedTraceDraftMismatch("motor_0",lastAccepted),"indexed_trace_changed_valid_disables_navigation_old_image_preserved");
        var invalid=new LadderEditorDocument();invalid.ResetProject("indexed-invalid-qa","Invalid",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=sceneId;
        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(invalid),null,true);
        check(_simulatorShell.VerifyIndexedTraceDraftMismatch("motor_9",lastAccepted),"indexed_trace_invalid_disables_navigation_old_image_preserved");
        StopActiveController();
    }
}