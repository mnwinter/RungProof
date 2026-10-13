using RungProof.Next.VirtualController;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private ControllerPublication? _instructionPublication;
    private ObservationUnavailableReason _instructionUnavailableReason = ObservationUnavailableReason.NoProgramLoaded;
    public void UpdateInstructionPublication(ControllerPublication? publication)
    {
        _instructionPublication = publication;
        foreach (var canvas in _ladderCanvases)
            canvas.SetMonitorPublication(publication, publication is null ? ObservationUnavailableReason.NoProgramLoaded : _instructionUnavailableReason);
    }
    private void ApplyInstructionMonitoring(LadderEditorCanvas canvas, VirtualControllerSnapshot? snapshot)
    {
        if(_instructionPublication is not null)canvas.SetMonitorPublication(_instructionPublication,_instructionUnavailableReason);
        else canvas.SetMonitorSnapshot(snapshot,_instructionUnavailableReason);
    }
    private void RefreshInstructionMonitorEligibility()
    {
        _instructionUnavailableReason = _virtualProgram is null ? ObservationUnavailableReason.NoProgramLoaded
            : _ladderMonitorMatchesLoadedProgram ? ObservationUnavailableReason.None : ObservationUnavailableReason.DraftMismatch;
        if(_instructionUnavailableReason == ObservationUnavailableReason.DraftMismatch)
        {
            try
            {
                if(!LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid)_instructionUnavailableReason=ObservationUnavailableReason.InvalidDraft;
            }
            catch(System.Exception error) when(error is System.ArgumentException or System.InvalidOperationException)
            { _instructionUnavailableReason=ObservationUnavailableReason.InvalidDraft; }
        }
        foreach(var canvas in _ladderCanvases)ApplyInstructionMonitoring(canvas,_virtualSnapshot);
    }
    public bool VerifyInstructionMonitoringRoutes(out string result)
    {
        var saved=LadderEditorProjectJson.Save(_ladderDocument);
        var originalProgram=_virtualProgram;var originalSnapshot=_virtualSnapshot;var originalPublication=_instructionPublication;
        var oldHistory=_scanHistory;var oldSelected=_scanSelectedHistory;var oldEvicted=_scanHistorySelectionEvicted;
        var checks=0;
        void Check(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
        try
        {
            var source=new LadderProgram(1,"monitor-routes","Monitor routes","LD",TimeSpan.FromMilliseconds(10),
                [new("input",PlcVariableType.Bool,PlcVariableRole.Input,false),new("output",PlcVariableType.Bool,PlcVariableRole.Output,false)],
                [new("network","Copy input",new("contact",LadderNodeKind.Contact,"input"),Coil:new("coil","output"))]);
            var document=new LadderEditorDocument();document.ReplaceFromProgram(source);document.SourceSceneId=_ladderDocument.SourceSceneId;
            var draft=LadderEditorProjectJson.Save(document);
            RestoreIntegratedLadderDraft(draft);var program=_ladderDocument.BuildProgram();
            var runtime=new VirtualControllerRuntime(LadderCompiler.Compile(program).Program!);runtime.Run();
            var first=runtime.Scan(new Dictionary<string,bool>{["input"]=false});
            AttachVirtualController(program,first);
            var origin=new ObservationOrigin(Guid.NewGuid(),Guid.NewGuid(),1,1);
            ControllerPublication Envelope(VirtualControllerSnapshot snapshot,long attempt)=>new(origin.SessionId,runtime.ProgramContentId,origin.LoadId,1,attempt,snapshot.ScanNumber,attempt,
                ControllerPublicationKind.ScanCompleted,ControllerCompletion.Completed,ControllerPhaseCompletion.ProgramExecuted,
                origin with{ScanAttemptNumber=attempt},snapshot.Observations!.WithOrigin(origin with{ScanAttemptNumber=attempt}),snapshot);
            var firstEnvelope=Envelope(first,1);UpdateInstructionPublication(firstEnvelope);RefreshLadderMonitorMatch();
            void Reason(ObservationUnavailableReason expected)
            {
                foreach(var canvas in _ladderCanvases)canvas.FlushPendingMonitoringForReview();
                Check(_ladderCanvases.All(c=>c.InstructionPresentationFor("contact","no").UnavailableReason==expected),"monitor eligibility "+expected);
            }
            Reason(ObservationUnavailableReason.None);
            _ladderDocument.Rungs[0].CoilMode=LadderCoilMode.Set;RefreshLadderMonitorMatch();Reason(ObservationUnavailableReason.DraftMismatch);
            _ladderDocument.Rungs[0].CoilVariable="missing";RefreshLadderMonitorMatch();Reason(ObservationUnavailableReason.InvalidDraft);
            RestoreIntegratedLadderDraft(draft);RefreshLadderMonitorMatch();Reason(ObservationUnavailableReason.None);
            _ladderDocument.WatchVariables.Add("input");RefreshLadderMonitorMatch();Reason(ObservationUnavailableReason.None);
            var reset=runtime.Reset();UpdateInstructionPublication(firstEnvelope with{Kind=ControllerPublicationKind.Reset,ExecutionGeneration=2,ObservationOrigin=null,Observations=null,Snapshot=reset});
            Reason(ObservationUnavailableReason.NoExecutionYet);
            UpdateInstructionPublication(firstEnvelope with{LoadId=Guid.NewGuid()});Reason(ObservationUnavailableReason.CaptureFault);
            UpdateInstructionPublication(firstEnvelope);Reason(ObservationUnavailableReason.None);

            var empty=new VirtualControllerSignalImage(new Dictionary<string,bool>(),new Dictionary<string,double>(),new Dictionary<string,string>());
            VirtualControllerAcceptedScan Record(ControllerPublication publication)=>new(new(publication.RuntimeScanNumber,publication.Snapshot.SimulatedTime,empty,empty,empty,[]){Publication=publication},empty);
            var firstRecord=Record(firstEnvelope);
            _scanHistorySelectionEvicted=false;_scanSelectedHistory=null;_historyInstructionId="contact";
            SetScanInspectionState(true,true,"Monitoring route review",[firstRecord]);
            VerifyScanInspectionHistorySelection(0,out _);
            Check(_scanHistoryText.Text.Contains("Consumed A: FALSE",StringComparison.Ordinal),"history reads actual first consumed FALSE");
            runtime.Run();var second=runtime.Scan(new Dictionary<string,bool>{["input"]=true});
            var secondEnvelope=Envelope(second,2);var secondRecord=Record(secondEnvelope);
            UpdateInstructionPublication(secondEnvelope);
            SetScanInspectionState(true,true,"Monitoring route review",[firstRecord,secondRecord]);
            Check(_scanHistoryText.Text.Contains("Consumed A: FALSE",StringComparison.Ordinal),"selected historical values stay stable after later TRUE scan");
            SetScanInspectionState(true,true,"Monitoring route review",[secondRecord],1);
            Check(_scanHistoryText.Text.StartsWith("Selected history evicted",StringComparison.Ordinal),"eviction does not substitute newest scan");
            result=$"Instruction monitoring routes PASS checks={checks}";return true;
        }
        catch(Exception error){result=$"Instruction monitoring routes FAIL checks={checks}: {error}";return false;}
        finally
        {
            RestoreIntegratedLadderDraft(saved);_virtualProgram=originalProgram;_virtualSnapshot=originalSnapshot;_instructionPublication=originalPublication;
            _scanHistory=oldHistory;_scanSelectedHistory=oldSelected;_scanHistorySelectionEvicted=oldEvicted;
            _scanHistoryWindow.Hide();RefreshLadderMonitorMatch();
        }
    }
}



