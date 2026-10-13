using System;
using Godot;
namespace RungProof.Next.App;
public partial class SimulatorShell
{
    private VBoxContainer _workspaceTrialCard=null!;
    private Label _workspaceTrialResult=null!;
    private Label _workspaceTrialIdentity=null!;
    private Label _workspaceTrialHint=null!;
    private Button _workspaceTrialHintButton=null!;
    private string _workspaceTrialRawState="Unavailable", _workspaceTrialRawDetail="validate your draft, then Reset and Run.";
    private string ObservedTrialDisplay(string state,string detail)=>_scanInspectionPaused
        ? $"CLOCK HELD · trial phase {state} · STEP or RESUME continues observation.\nRetained observation: {detail}"
        : state+" · "+detail;
    private void RefreshWorkspaceTrialSchedulingDisplay()
    {
        if(_workspaceTrialResult is not null)_workspaceTrialResult.Text=ObservedTrialDisplay(_workspaceTrialRawState,_workspaceTrialRawDetail);
        if(_workspaceRollerTrialResult is not null)_workspaceRollerTrialResult.Text=ObservedTrialDisplay(_workspaceRollerTrialRawState,_workspaceRollerTrialRawDetail);
    }
    public bool LoadedDraftIdentityCurrent { get { RefreshLadderMonitorMatch(); return _ladderMonitorMatchesLoadedProgram; } }
    private Control BuildWorkspaceObservedTrialCard()
    {
        _workspaceTrialCard=new VBoxContainer { Name="FiniteBeltObservedTrial",Visible=false };
        _workspaceTrialCard.AddChild(SectionLabel("FINITE BELT · OBSERVED TRIAL"));
        Label Text(string text)=>new(){Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart};
        // Show the outcome before explanatory text, even when the operator rail is short.
        _workspaceTrialResult=Text("Unavailable · validate your draft, then Reset and Run.");_workspaceTrialResult.Name="ObservedResult";
        _workspaceTrialIdentity=Text("No accepted trial yet.");_workspaceTrialIdentity.Name="AcceptedIdentity";
        _workspaceTrialCard.AddChild(_workspaceTrialResult);_workspaceTrialCard.AddChild(_workspaceTrialIdentity);
        _workspaceTrialCard.AddChild(Text("Objective: momentary Start, retain Run after release, stop on sampled beam feedback, then hold for 0.5 accepted seconds."));
        _workspaceTrialCard.AddChild(Text("Reset first. Read-only observation. Timeout: 60 accepted seconds after Start; timeout means not observed."));
        _workspaceTrialHintButton=new Button { Name="ShowConceptualHint",Text="Show conceptual hint" };
        _workspaceTrialHint=Text("Remember the request after release; use physical beam feedback to remove the drive command. No ladder is supplied or repaired.");
        _workspaceTrialHint.Visible=false;
        _workspaceTrialHintButton.Pressed+=()=>{_workspaceTrialHint.Visible=!_workspaceTrialHint.Visible;_workspaceTrialHintButton.Text=_workspaceTrialHint.Visible?"Hide conceptual hint":"Show conceptual hint";};
        _workspaceTrialCard.AddChild(_workspaceTrialHintButton);_workspaceTrialCard.AddChild(_workspaceTrialHint);
        return _workspaceTrialCard;
    }
    public void ScrollWorkspaceTrialForReview()
    {
        GetNode<ScrollContainer>("Workspace/LeftDock/LeftTabs/Operator").EnsureControlVisible(_workspaceTrialCard);
    }
    public bool WorkspaceTrialCardFitsForReview()
    {
        var scroll=GetNode<ScrollContainer>("Workspace/LeftDock/LeftTabs/Operator");
        var frame=scroll.GetGlobalRect();var card=_workspaceTrialCard.GetGlobalRect();
        return _workspaceTrialCard.IsVisibleInTree()&&card.Position.Y>=frame.Position.Y-2&&card.End.Y<=frame.End.Y+2;
    }
    public void ResetWorkspaceTrialHint()
    {
        if(_workspaceTrialHint is null)return;
        _workspaceTrialHint.Visible=false;_workspaceTrialHintButton.Text="Show conceptual hint";
    }
    public bool VerifyWorkspaceTrialHintHidden()=>_workspaceTrialHint is not null&&!_workspaceTrialHint.Visible;
    public bool ShowWorkspaceTrialHintForReview()
    {
        if(!VerifyWorkspaceTrialHintHidden())return false;
        _workspaceTrialHintButton.EmitSignal(Button.SignalName.Pressed);
        return _workspaceTrialHint.Visible;
    }
    public void SetWorkspaceObservedTrial(bool visible,string state,string detail,string identity)
    {
        if(_workspaceTrialCard is null)return;
        _workspaceTrialCard.Visible=visible;
        _workspaceTrialRawState=state;_workspaceTrialRawDetail=detail;
        _workspaceTrialResult.Text=ObservedTrialDisplay(state,detail);
        _workspaceTrialIdentity.Text=identity;
    }
}
