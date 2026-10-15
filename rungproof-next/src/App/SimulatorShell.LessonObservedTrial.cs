using Godot;
using System.Globalization;
using RungProof.Next.Lessons;
namespace RungProof.Next.App;
public partial class SimulatorShell
{
    private VBoxContainer _repeatCycleCard=null!;
    private Label _repeatCycleResult=null!, _repeatCycleCounts=null!, _repeatCycleProvenance=null!, _repeatCycleHint=null!;
    private Button _repeatCycleHintButton=null!;
    private Control BuildRepeatCycleObservedTrialCard()
    {
        _repeatCycleCard=new VBoxContainer { Name="RepeatCycleObservedTrial", Visible=false };
        Label Text(string name,string text)=>new(){Name=name,Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart};
        _repeatCycleCard.AddChild(SectionLabel("REPEAT CYCLE · OBSERVED TRIAL"));
        _repeatCycleResult=Text("ObservedResult","Unavailable · validate the draft and Reset.");
        _repeatCycleCounts=Text("AcceptedCounts","Assessed actual returns: unavailable · accepted PLC count: unavailable");
        _repeatCycleProvenance=Text("AcceptedIdentity","No accepted identity.");
        _repeatCycleCard.AddChild(_repeatCycleResult);_repeatCycleCard.AddChild(_repeatCycleCounts);_repeatCycleCard.AddChild(_repeatCycleProvenance);
        _repeatCycleCard.AddChild(Text("Protocol","One enabled momentary START; three actual returns, then stop and hold home 0.5 accepted seconds. Deadline: 20 accepted seconds."));
        _repeatCycleCard.AddChild(Text("ReadOnlyBoundary","Read-only offline observation. Pause/Step unsupported. Retained history is read-only. Reset starts a new trial."));
        _repeatCycleHintButton=new Button {Name="ShowConceptualHint",Text="Show conceptual hint"};
        _repeatCycleHint=Text("ConceptualHint","Retain the request after release. Count actual completion edges; sampled Done arrives on the next scan. A low command acknowledges Done before another cycle. Stop at the target count.");
        _repeatCycleHint.Visible=false;
        _repeatCycleHintButton.Pressed+=()=>{_repeatCycleHint.Visible=!_repeatCycleHint.Visible;_repeatCycleHintButton.Text=_repeatCycleHint.Visible?"Hide conceptual hint":"Show conceptual hint";};
        _repeatCycleCard.AddChild(_repeatCycleHintButton);_repeatCycleCard.AddChild(_repeatCycleHint);
        return _repeatCycleCard;
    }
    public void SetRepeatCycleObservedTrial(bool visible,RepeatCycleTrialView? view,string identity)
    {
        if(_repeatCycleCard is null)return;
        if(!visible){_repeatCycleHint.Visible=false;_repeatCycleHintButton.Text="Show conceptual hint";}
        _repeatCycleCard.Visible=visible;
        _repeatCycleResult.Text=view is null?"Unavailable · validate the current draft and Reset.":$"{view.Status} · {view.Detail}";
        var frozen=view is null || view.Historical || view.Status is RepeatCycleTrialStatus.NeedsAttention
            or RepeatCycleTrialStatus.Incomplete or RepeatCycleTrialStatus.Unavailable;
        _repeatCycleCounts.Text=$"Assessed actual returns: {view?.ActualReturns.ToString(CultureInfo.InvariantCulture)??"unavailable"} · accepted PLC count: {view?.ClaimedCount?.ToString(CultureInfo.InvariantCulture)??"unavailable"}\n"
            +$"Accepted elapsed: {view?.AcceptedElapsed.TotalSeconds.ToString("F2",CultureInfo.InvariantCulture)??"unavailable"} s · {(frozen?"frozen accepted assessment":"updating accepted assessment")}";
        _repeatCycleProvenance.Text=identity;
    }
    public void ScrollRepeatCycleTrialForReview()=>GetNode<ScrollContainer>("Workspace/LeftDock/LeftTabs/Operator").EnsureControlVisible(_repeatCycleHint.Visible ? _repeatCycleHint : _repeatCycleCard.GetChild<Control>(0));
    public string RepeatCycleTrialLabelsForReview()=>_repeatCycleResult.Text+"\n"+_repeatCycleCounts.Text+"\n"+_repeatCycleProvenance.Text;
    public void ShowRepeatCycleHintForReview(){if(!_repeatCycleHint.Visible)_repeatCycleHintButton.EmitSignal(Button.SignalName.Pressed);}
}
