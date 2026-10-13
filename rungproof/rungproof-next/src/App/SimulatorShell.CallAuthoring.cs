using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private LadderCallEditor? _callAuthoring;
    private PendingEditorForm? _callPendingForm;
    private void OpenCallAuthoring(LadderEditorDocument document, string ownerId, string rungId, Action committed)
    {
        if (ChallengeMutationLocked) return;
        if (_callPendingForm is not null) _pendingEditorForms.Remove(_callPendingForm);
        _callAuthoring?.QueueFree();
        _callAuthoring = new(new LadderCallAuthoring(document, ownerId, rungId), _ladderHistory, () => ChallengeMutationLocked, committed);
        AddChild(_callAuthoring); _callAuthoring.PopupCentered();
        var form = _callAuthoring;
        _callPendingForm = new(() => GodotObject.IsInstanceValid(form) && form.Pending, form.TryApply, form.CancelEdit);
        _pendingEditorForms.Add(_callPendingForm);
    }
    private Control CreateBlockSemanticControls(LadderEditorDocument document, Action committed)
    {
        var body = new VBoxContainer { Name = "BlockSemanticControls" };
        var status = new Label { Name = "BlockSemanticMode", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        var convert = new Button { Name = "ConvertBlockSemantics", Text = "Convert to parameterized FC/FB" };
        body.AddChild(status); body.AddChild(convert);
        void Refresh()
        {
            if (document.Blocks.Count == 0) return;
            var block = document.Blocks[document.ActiveBlockIndex];
            status.Text = block.SemanticMode == LadderSemanticMode.Legacy ? "Legacy: calls share global tags; interface is descriptive." : "Parameterized: typed actuals, invocation locals and explicit FB instances.";
            convert.Disabled = ChallengeMutationLocked || block.SemanticMode == LadderSemanticMode.Parameterized || block.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock);
        }
        _ladderEditorRefreshers.Add(Refresh); Refresh();
        convert.Pressed += () => ResolvePendingEditorForms(() =>
        {
            var block = document.Blocks[document.ActiveBlockIndex];
            var tx = new LadderEditTransaction(document, "Convert block to parameterized mode");
            var dialog = new ConfirmationDialog { Name = "ConfirmBlockConversion", Title = "Explicit semantic conversion", DialogText = "Convert " + block.Name + " to parameterized mode? Existing operands remain explicitly global. Other calls require typed actuals and FB instances. Unresolved operands remain diagnostics. This is one undoable edit.", OkButtonText = "Convert" };
            AddChild(dialog);
            dialog.Confirmed += () =>
            {
                try
                {
                    LadderCallAuthoring.ConvertBlock(tx.Draft, block.Id);
                    var converted = tx.Draft.Blocks.Single(b => b.Id == block.Id);
                    foreach (var formal in converted.Interface)
                        if (LadderCallAuthoring.ValidateFormal(converted.BlockType, formal) is { } invalid) throw new InvalidOperationException(invalid);
                    if (!tx.TryCommit(_ladderHistory, ChallengeMutationLocked, out var error)) throw new InvalidOperationException(error);
                    committed(); Refresh(); dialog.QueueFree();
                }
                catch (InvalidOperationException error) { status.Text = error.Message; dialog.QueueFree(); }
            };
            dialog.Canceled += () => { tx.Cancel(); dialog.QueueFree(); }; dialog.PopupCentered();
        });
        return body;
    }
    public bool VerifyCallAuthoring()
    {
        var d = LadderEditorDocument.CreateConveyorExample(); var owner = d.Blocks[0]; var target = d.AddBlock("Call review", LadderBlockType.FunctionBlock);
        target.Interface.Add(new("Enable", PlcVariableType.Bool, LadderInterfaceSection.Input, false));
        target.Interface.Add(new("Result", PlcVariableType.Bool, LadderInterfaceSection.Output, false));
        var rung = owner.Rungs[0]; rung.IsCall = true; rung.CallTarget = target.Id;
        var history = new LadderEditorHistory(); var committed = false; var before = LadderEditorProjectJson.Save(d);
        var form = new LadderCallEditor(new(d, owner.Id, rung.Id), history, () => false, () => committed = true); AddChild(form); form.PopupCentered();
        T Field<T>(string name) where T : Node => form.FindChild(name, true, false) as T ?? throw new InvalidOperationException("Missing CALL control: " + name);
        void Press(string name) => Field<Button>(name).EmitSignal(Button.SignalName.Pressed);
        void Choose(OptionButton field, int index) { field.Select(index); field.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index); }
        Press("ConvertCallTarget");
        var inputId = form.Edit.Target!.Interface[0].Id; var outputId = form.Edit.Target.Interface[1].Id;
        var staged = LadderEditorProjectJson.Save(d) == before && !history.CanUndo;
        Field<LineEdit>("CallInstanceName").Text = "First instance"; Press("CreateCallInstance");
        var instanceId = form.Edit.Call.CallInstanceId;
        Choose(Field<OptionButton>("ActualKind_" + inputId), 1); Field<LineEdit>("ActualLiteral_" + inputId).Text = "not_bool";
        Choose(Field<OptionButton>("ActualKind_" + outputId), 1);
        Press("ApplyCall"); var rejected = !committed && !history.CanUndo && Field<Label>("CallStatus").Text.Contains("BOOL", StringComparison.Ordinal);
        Field<LineEdit>("ActualLiteral_" + inputId).Text = "true";
        Field<LineEdit>("CallInstanceName").Text = "Renamed instance"; Press("RenameCallInstance");
        Press("ApplyCall");
        var applied = committed && d.Instances.Single().Id == instanceId && d.Instances.Single().Name == "Renamed instance" && owner.Id == d.Blocks[0].Id;
        var reopened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d)).Document!;
        var persisted = reopened.Instances.Single().Id == instanceId && reopened.Blocks[0].Rungs[0].CallBindings.Count == 2;
        var undone = history.Undo(d, out _) && LadderEditorProjectJson.Save(d) == before && !history.CanUndo;
        form.QueueFree();
        var cancel = new LadderCallEditor(new(d, owner.Id, rung.Id), history, () => false, () => { }); AddChild(cancel); cancel.PopupCentered();
        cancel.Edit.ConvertTargetToParameterized(); cancel.CancelEdit(); var cancelled = LadderEditorProjectJson.Save(d) == before && !history.CanUndo; cancel.QueueFree();
        GD.Print($"CALL_AUTHORING staged={staged} invalidRejected={rejected} applied={applied} persisted={persisted} undo={undone} cancelled={cancelled}");
        return staged && rejected && applied && persisted && undone && cancelled;
    }    /// <summary>Production-shell fixture for rendered and physical-input qualification; leaves the editor open.</summary>
    public void SetupCallAuthoringReview(bool openForm = true)
    {
        foreach (var pending in _pendingEditorForms.ToArray()) pending.Discard();
        var d = LadderEditorDocument.CreateConveyorExample(); var owner = d.Blocks[0];
        var target = d.AddBlock("Station counter", LadderBlockType.FunctionBlock);
        target.Interface.Add(new("Pulse", PlcVariableType.Bool, LadderInterfaceSection.Input, false));
        target.Interface.Add(new("Threshold reached", PlcVariableType.Bool, LadderInterfaceSection.Output, false));
        target.Interface.Add(new("Pieces", PlcVariableType.Counter, LadderInterfaceSection.Static, 0L));
        var call = owner.Rungs[0]; call.IsCall = true; call.CallTarget = target.Id; call.Label = "Call station with independent instance storage";
        var edit = new LadderCallAuthoring(d, owner.Id, call.Id); edit.ConvertTargetToParameterized(); edit.CreateInstance("Station A"); edit.CreateInstance("Station B");
        var parameters = edit.Target!.Interface;
        var count = new EditableRung("review-fb-counter", "Count rising pulses", "") { IsCounter = true, CounterVariable = "local:" + parameters[2].Id, CounterPreset = 3 };
        count.SetRoot(new("review-count-enable", LadderNodeKind.Contact, Variable: "local:" + parameters[0].Id)); edit.Target.Rungs.Add(count);
        var output = new EditableRung("review-fb-output", "Expose threshold", "local:" + parameters[1].Id);
        output.SetRoot(new("review-count-status", LadderNodeKind.Contact, Variable: "local:" + parameters[2].Id + ".Q")); edit.Target.Rungs.Add(output);
        edit.Bind(new(parameters[0].Id, LadderActualKind.Literal, Literal: false, LiteralType: PlcVariableType.Bool));
        edit.Bind(new(parameters[1].Id, LadderActualKind.Discard));
        if (!edit.Commit(new LadderEditorHistory(), false, out var error)) throw new InvalidOperationException(error);
        _ladderDocument.RestoreSnapshot(d.CaptureSnapshot()); _ladderHistory.Clear();
        SetReviewState("ladder", ""); _ladderEnvironmentTabs.CurrentTab = 0;
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        _ladderCanvases.First().SelectElement(0, -1, -1, true);
        Callable.From(() =>
        {
            foreach (var canvas in _ladderCanvases)
            {
                canvas.RefreshDocument();
                for (Node? parent = canvas.GetParent(); parent is not null && parent != this; parent = parent.GetParent())
                    if (parent is ScrollContainer scroll) { scroll.ScrollHorizontal = 0; scroll.ScrollVertical = 0; break; }
                GD.Print($"CALL_REVIEW_CANVAS visible={canvas.IsVisibleInTree()} rect={canvas.GetGlobalRect()} minimum={canvas.CustomMinimumSize} block={_ladderDocument.ActiveBlockIndex} rungs={_ladderDocument.Rungs.Count}");
            }
        }).CallDeferred();
        if (openForm) OpenCallAuthoring(_ladderDocument, owner.Id, call.Id, () => { foreach (var refresh in _ladderEditorRefreshers) refresh(); });
    }}
