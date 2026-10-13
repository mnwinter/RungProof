using Godot;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private VBoxContainer _workspaceRollerTrialCard = null!;
    private Label _workspaceRollerTrialResult = null!;
    private Label _workspaceRollerTrialIdentity = null!;
    private Label _workspaceRollerTrialMotion = null!;
    private Label _workspaceRollerTrialHint = null!;
    private Button _workspaceRollerTrialHintButton = null!;
    private string _workspaceRollerTrialRawState="Unavailable", _workspaceRollerTrialRawDetail="validate your draft, then Reset and Run.";

    private Control BuildWorkspaceRollerObservedTrialCard()
    {
        _workspaceRollerTrialCard = new VBoxContainer { Name = "FiniteRollerObservedTrial", Visible = false };
        _workspaceRollerTrialCard.AddChild(SectionLabel("FINITE PALLET ROLLER · OBSERVED TRIAL"));
        Label Text(string text) => new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _workspaceRollerTrialResult = Text("Unavailable · validate your draft, then Reset and Run.");
        _workspaceRollerTrialResult.Name = "ObservedResult";
        _workspaceRollerTrialIdentity = Text("No accepted trial yet.");
        _workspaceRollerTrialIdentity.Name = "AcceptedIdentity";
        _workspaceRollerTrialMotion = Text("Drive stopped · current beam clear.");
        _workspaceRollerTrialCard.AddChild(_workspaceRollerTrialResult);
        _workspaceRollerTrialCard.AddChild(_workspaceRollerTrialIdentity);
        _workspaceRollerTrialCard.AddChild(_workspaceRollerTrialMotion);
        _workspaceRollerTrialCard.AddChild(Text("Objective: retain momentary Start; command OFF on the next sampled beam scan; coast to rest and hold 0.5 accepted seconds."));
        _workspaceRollerTrialCard.AddChild(Text("Pallet gaps may clear the beam during coast. Trigger history differs from the current beam. Read-only; timeout 60 accepted seconds."));
        _workspaceRollerTrialHintButton = new Button { Name = "ShowConceptualHint", Text = "Show conceptual hint" };
        _workspaceRollerTrialHint = Text("Remember the request after release. Remove the drive command when sampled beam feedback arrives; allow braking to finish. No ladder is supplied or repaired.");
        _workspaceRollerTrialHint.Visible = false;
        _workspaceRollerTrialHintButton.Pressed += () =>
        {
            _workspaceRollerTrialHint.Visible = !_workspaceRollerTrialHint.Visible;
            _workspaceRollerTrialHintButton.Text = _workspaceRollerTrialHint.Visible ? "Hide conceptual hint" : "Show conceptual hint";
        };
        _workspaceRollerTrialCard.AddChild(_workspaceRollerTrialHintButton);
        _workspaceRollerTrialCard.AddChild(_workspaceRollerTrialHint);
        return _workspaceRollerTrialCard;
    }

    public void ResetWorkspaceRollerTrialHint()
    {
        if (_workspaceRollerTrialHint is null) return;
        _workspaceRollerTrialHint.Visible = false;
        _workspaceRollerTrialHintButton.Text = "Show conceptual hint";
    }

    public void SetWorkspaceRollerObservedTrial(bool visible, string state, string detail, string identity, string motion)
    {
        if (_workspaceRollerTrialCard is null) return;
        _workspaceRollerTrialCard.Visible = visible;
        _workspaceRollerTrialRawState=state;_workspaceRollerTrialRawDetail=detail;
        _workspaceRollerTrialResult.Text = ObservedTrialDisplay(state,detail);
        _workspaceRollerTrialIdentity.Text = identity;
        _workspaceRollerTrialMotion.Text = motion;
    }

    public void ScrollWorkspaceRollerTrialForReview()
        => GetNode<ScrollContainer>("Workspace/LeftDock/LeftTabs/Operator").EnsureControlVisible(_workspaceRollerTrialCard);

    public bool WorkspaceRollerTrialCardFitsForReview()
    {
        var viewport = GetNode<ScrollContainer>("Workspace/LeftDock/LeftTabs/Operator").GetGlobalRect();
        var card = _workspaceRollerTrialCard.GetGlobalRect();
        return card.Position.Y >= viewport.Position.Y - 2 && card.End.Y <= viewport.End.Y + 2;
    }
}
