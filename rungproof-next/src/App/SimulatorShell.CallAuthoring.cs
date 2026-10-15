using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    // Only the terminal box forms supported by the current simulator belong
    // in TIA's empty-box chooser. Coils and CTD's LD pin are separate elements.
    private static readonly (string Label, string Kind)[] TiaEmptyBoxChoices =
    [
        ("ABS", "numeric-6"), ("ACOS", "numeric-15"), ("ADD", "numeric-1"),
        ("ASIN", "numeric-14"), ("ATAN", "numeric-16"), ("CEIL", "numeric-22"),
        ("CONVERT", "numeric-20"), ("COS", "numeric-12"), ("CTD", "ctd"),
        ("CTU", "ctu"), ("DIV", "numeric-4"), ("EXPT", "numeric-9"),
        ("FLOOR", "numeric-23"), ("LN", "numeric-10"), ("MOD", "numeric-5"),
        ("MOVE", "numeric-0"), ("MUL", "numeric-3"), ("NEG", "numeric-7"),
        ("NORM_X", "numeric-18"), ("ROUND", "numeric-21"), ("SCALE_X", "numeric-19"),
        ("SIN", "numeric-11"), ("SQRT", "numeric-8"), ("SUB", "numeric-2"),
        ("TAN", "numeric-13"), ("TON", "ton"), ("TONR", "rto"),
        ("TOF", "tof"), ("TP", "tp"), ("TRUNC", "numeric-17"),
    ];

    /// <summary>Visual fixture for the TIA empty-instruction-box authoring state.</summary>
    public void SetupEmptyInstructionReview(bool openChooser = false)
    {
        GetWindow().Size = new Vector2I(2200, 1200);
        foreach (var pending in _pendingEditorForms.ToArray()) pending.Discard();
        var document = LadderEditorDocument.CreateConveyorExample();
        var referenceRung = document.Rungs[0];
        referenceRung.Label = string.Empty;
        referenceRung.HasTerminalAction = false;
        referenceRung.HasEmptyInstructionPlaceholder = true;
        // Keep the review fixture in the same unresolved-operand state as the
        // user's TIA screenshot. Named conveyor tags made earlier captures an
        // invalid side-by-side comparison even when the topology matched.
        for (var branchIndex = 0; branchIndex < referenceRung.Branches.Count; branchIndex++)
        for (var contactIndex = 0; contactIndex < referenceRung.Branches[branchIndex].Contacts.Count; contactIndex++)
            referenceRung.Branches[branchIndex].Contacts[contactIndex] =
                referenceRung.Branches[branchIndex].Contacts[contactIndex] with { Variable = "???" };
        _ladderDocument.RestoreSnapshot(document.CaptureSnapshot());
        _ladderHistory.Clear();
        _simulatorModeDialog?.Hide();
        SetReviewState("ladder", "");
        _ladderEnvironmentTabs.CurrentTab = 0;
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var reviewCanvas = _ladderCanvases.First();
        reviewCanvas.SetZoomPercent(100);
        var inputLead = reviewCanvas.GetTerminalInputLeadLength(0);
        if (inputLead < 80.0f)
            throw new InvalidOperationException($"TIA empty-box input lead is too short: {inputLead:0.0} logical px");
        GD.Print($"TIA_EMPTY_BOX_INPUT_LEAD_PASS length={inputLead:0.0} logical px");
        if (openChooser) reviewCanvas.SelectElement(0, -1, -1, true);
        else reviewCanvas.SelectRung(0);
        if (openChooser)
            reviewCanvas._GuiInput(new InputEventMouseButton
            {
                Pressed = true,
                ButtonIndex = MouseButton.Left,
                Position = reviewCanvas.GetElementPosition(0, -1, -1, output: true)
                    + new Vector2(54, -21) * reviewCanvas.ZoomFactor,
            });
        Callable.From(() =>
        {
            foreach (var canvas in _ladderCanvases)
            {
                canvas.RefreshDocument();
                GD.Print($"EMPTY_BOX_REVIEW visible={canvas.IsVisibleInTree()} rect={canvas.GetGlobalRect()} networks={_ladderDocument.Rungs.Count}");
            }
        }).CallDeferred();
    }

    public bool VerifyEmptyInstructionReplacement(Action<string, bool>? assertionObserver = null)
    {
        SetupEmptyInstructionReview(openChooser: true);
        var chooser = GetNodeOrNull<PopupMenu>(
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/EmptyInstructionChooser");
        void Check(string id, bool passed)
        {
            assertionObserver?.Invoke(id, passed);
            if (!passed) throw new InvalidOperationException(id);
        }
        Check("ProductionRouteOpensChooser", chooser is not null && chooser.Visible);
        Check("ChooserContainsDeclaredChoices", chooser is not null && chooser.ItemCount == TiaEmptyBoxChoices.Length);
        if (chooser is null) return false;
        var reviewCanvas = _ladderCanvases.First();
        var placeholderSnapshot = _ladderDocument.CaptureSnapshot();
        Check("PlaceholderCannotCompile", !LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid);
        var everyChoiceMatched = true;
        var everyChoicePersisted = true;
        foreach (var (choice, index) in TiaEmptyBoxChoices.Select((choice, index) => (choice, index)))
        {
            _ladderDocument.RestoreSnapshot(placeholderSnapshot);
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            reviewCanvas.SelectElement(0, -1, -1, output: true);
            chooser.EmitSignal(PopupMenu.SignalName.IdPressed, (long)index);

            var rung = _ladderDocument.Rungs[0];
            var matches = choice.Kind switch
            {
                "ton" => rung.IsTimer && rung.TimerKind == LadderTimerKind.OnDelay,
                "tof" => rung.IsTimer && rung.TimerKind == LadderTimerKind.OffDelay,
                "tp" => rung.IsTimer && rung.TimerKind == LadderTimerKind.Pulse,
                "rto" => rung.IsTimer && rung.TimerKind == LadderTimerKind.RetentiveOnDelay,
                "ctu" => rung.IsCounter && rung.CounterKind == LadderCounterKind.CountUp,
                "ctd" => rung.IsCounter && rung.CounterKind == LadderCounterKind.CountDown,
                _ when choice.Kind.StartsWith("numeric-", StringComparison.Ordinal)
                    && int.TryParse(choice.Kind.AsSpan("numeric-".Length), out var numericIndex)
                    => rung.IsNumericOperation && (int)rung.NumericOperationKind == numericIndex,
                _ => false,
            };
            everyChoiceMatched &= matches && rung.HasTerminalAction && !rung.HasEmptyInstructionPlaceholder;

            var reloaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(_ladderDocument)).Document;
            everyChoicePersisted &= reloaded is not null && !reloaded.Rungs[0].HasEmptyInstructionPlaceholder
                && reloaded.Rungs[0].HasTerminalAction;
        }
        Check("EveryChoiceCreatesExpectedTerminal", everyChoiceMatched);
        Check("ReplacementPersistsWithoutPlaceholder", everyChoicePersisted);

        _ladderDocument.RestoreSnapshot(placeholderSnapshot);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        reviewCanvas.SelectElement(0, -1, -1, output: true);
        return true;
    }

    private LadderCallEditor? _callAuthoring;
    private PendingEditorForm? _callPendingForm;
    private void OpenCallAuthoring(LadderEditorDocument document, string ownerId, string rungId, Action committed)
        => OpenCallAuthoring(new LadderCallAuthoring(document, ownerId, rungId), committed);
    private void OpenCallAuthoring(LadderCallAuthoring edit, Action committed)
    {
        if (ChallengeMutationLocked) return;
        if (_callPendingForm is not null) _pendingEditorForms.Remove(_callPendingForm);
        _callAuthoring?.QueueFree();
        _callAuthoring = new(edit, _ladderHistory, () => ChallengeMutationLocked, committed);
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
        var dbNumber = Field<SpinBox>("InstanceDbNumber"); var dbName = Field<LineEdit>("InstanceDbName");
        dbNumber.Value = 42; dbName.Text = "StationCounter_DB";
        Choose(Field<OptionButton>("ActualKind_" + inputId), 1); Field<LineEdit>("ActualLiteral_" + inputId).Text = "not_bool";
        Choose(Field<OptionButton>("ActualKind_" + outputId), 1);
        Press("ApplyCall"); var rejected = !committed && !history.CanUndo && Field<Label>("CallStatus").Text.Contains("BOOL", StringComparison.Ordinal);
        Field<LineEdit>("ActualLiteral_" + inputId).Text = "true";
        Field<LineEdit>("CallInstanceName").Text = "Renamed instance"; Press("RenameCallInstance");
        Press("ApplyCall");
        var applied = committed && d.Instances.Single().Id == instanceId && d.Instances.Single().Name == "Renamed instance"
            && d.Instances.Single().InstanceDataBlock == new LadderInstanceDataBlock(42, "StationCounter_DB") && owner.Id == d.Blocks[0].Id;
        var reopened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d)).Document!;
        var persisted = reopened.Instances.Single().Id == instanceId && reopened.Blocks[0].Rungs[0].CallBindings.Count == 2
            && reopened.Instances.Single().InstanceDataBlock == new LadderInstanceDataBlock(42, "StationCounter_DB");
        var undone = history.Undo(d, out _) && LadderEditorProjectJson.Save(d) == before && !history.CanUndo;
        form.QueueFree();
        var cancel = new LadderCallEditor(new(d, owner.Id, rung.Id), history, () => false, () => { }); AddChild(cancel); cancel.PopupCentered();
        cancel.Edit.ConvertTargetToParameterized(); cancel.CancelEdit(); var cancelled = LadderEditorProjectJson.Save(d) == before && !history.CanUndo; cancel.QueueFree();
        var dropDocument = LadderEditorDocument.CreateConveyorExample();
        var dropOwner = dropDocument.Blocks[0];
        var dropTarget = dropDocument.AddBlock("Atomic drop FC", LadderBlockType.Function);
        dropTarget.SemanticMode = LadderSemanticMode.Parameterized;
        var dropRung = dropOwner.Rungs[0];
        var dropRungId = dropRung.Id;
        var dropTerminalId = dropRung.TerminalActionId;
        var dropBefore = LadderEditorProjectJson.Save(dropDocument);
        var dropHistory = new LadderEditorHistory();
        var cancelledDrop = LadderCallAuthoring.CreateForDroppedBlock(
            dropDocument, dropOwner.Id, dropRungId, dropTarget.Id);
        var dropStaged = LadderEditorProjectJson.Save(dropDocument) == dropBefore && !dropHistory.CanUndo
            && cancelledDrop.Call.IsCall && cancelledDrop.Call.CallTarget == dropTarget.Id
            && cancelledDrop.Call.TerminalActionId == dropTerminalId;
        cancelledDrop.Cancel();
        var dropCancelExact = LadderEditorProjectJson.Save(dropDocument) == dropBefore && !dropHistory.CanUndo;

        var appliedDrop = LadderCallAuthoring.CreateForDroppedBlock(
            dropDocument, dropOwner.Id, dropRungId, dropTarget.Id);
        var dropCommitted = appliedDrop.Commit(dropHistory, false, out var dropError);
        var dropAppliedJson = LadderEditorProjectJson.Save(dropDocument);
        var dropApplied = dropCommitted && dropDocument.Rungs[0].IsCall
            && dropDocument.Rungs[0].CallTarget == dropTarget.Id
            && dropDocument.Rungs[0].Id == dropRungId
            && dropDocument.Rungs[0].TerminalActionId == dropTerminalId
            && dropHistory.CanUndo;
        var dropUndoExact = dropHistory.Undo(dropDocument, out _)
            && LadderEditorProjectJson.Save(dropDocument) == dropBefore && !dropHistory.CanUndo;
        var dropRedoExact = dropHistory.Redo(dropDocument, out _)
            && LadderEditorProjectJson.Save(dropDocument) == dropAppliedJson;

        GD.Print($"CALL_AUTHORING staged={staged} invalidRejected={rejected} applied={applied} persisted={persisted} undo={undone} cancelled={cancelled} "
            + $"dropStaged={dropStaged} dropCancelExact={dropCancelExact} dropApplied={dropApplied} dropUndoExact={dropUndoExact} dropRedoExact={dropRedoExact} dropError={dropError}");
        return staged && rejected && applied && persisted && undone && cancelled
            && dropStaged && dropCancelExact && dropApplied && dropUndoExact && dropRedoExact;
    }    /// <summary>Production-shell fixture for rendered and physical-input qualification; leaves the editor open.</summary>
    public void SetupCallAuthoringReview(bool openForm = true)
    {
        // Give the reviewer enough viewport to inspect both the left rail and
        // the FB instance DB header without horizontal or diagnostic-panel crop.
        GetWindow().Size = new Vector2I(2200, 1200);
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
