using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>Modal detached CALL form. Every visible field is staged until one validated Apply.</summary>
public partial class LadderCallEditor : Window
{
    private readonly LadderCallAuthoring _edit;
    private readonly LadderEditorHistory _history;
    private readonly Func<bool> _locked;
    private readonly Action _committed;
    private readonly VBoxContainer _body = new();
    private readonly ScrollContainer _scroll = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
    private int _focusRequest;
    private readonly Label _status = new() { Name = "CallStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Dictionary<string, Control> _fields = new();
    private readonly List<Func<string?>> _readBindings = [];
    public bool Pending => Visible && _edit.IsPending;
    public LadderCallAuthoring Edit => _edit;
    public LadderCallEditor(LadderCallAuthoring edit, LadderEditorHistory history, Func<bool> locked, Action committed)
    {
        _edit = edit; _history = history; _locked = locked; _committed = committed;
        Name = "CallAuthoring"; Title = "CALL · target, instance and typed actuals"; Size = new Vector2I(790, 640); MinSize = new Vector2I(640, 420);
        Transient = true; Exclusive = true; CloseRequested += CancelEdit; WindowInput += HandleWindowInput;
        var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); AddChild(margin);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 16);
        var outer = new VBoxContainer(); margin.AddChild(outer);
        outer.AddChild(_scroll);
        _body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; _scroll.AddChild(_body);
        outer.AddChild(_status); var actions = new HBoxContainer(); outer.AddChild(actions);
        var apply = new Button { Name = "ApplyCall", Text = "Apply CALL" }; var cancel = new Button { Name = "CancelCall", Text = "Cancel" };
        actions.AddChild(apply); actions.AddChild(cancel); apply.Pressed += () => TryApply(); cancel.Pressed += CancelEdit;
        Rebuild();
    }
    private void HandleWindowInput(InputEvent input)
    {
        if (!Visible || input is not InputEventKey { Pressed: true, Keycode: Key.Escape }) return;
        // A selector popup owns its first Escape; the following key cancels the form.
        if (FindChildren("*", "OptionButton", true, false).OfType<OptionButton>().Any(option => option.GetPopup().Visible)) return;
        CancelEdit(); SetInputAsHandled();
    }
    public void CancelEdit() { _edit.Cancel(); Hide(); }
    private void Run(Action action)
    {
        try { action(); _status.Text = "Pending · Apply commits all changes together; Cancel discards."; }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException) { _status.Text = error.Message; }
    }
    private void Rebuild()
    {
        foreach (var child in _body.GetChildren()) { _body.RemoveChild(child); child.QueueFree(); }
        _fields.Clear(); _readBindings.Clear();
        _body.AddChild(new Label { Text = "Target block" }); var target = new OptionButton { Name = "CallTargetBlock" }; _body.AddChild(target); _fields["target"] = target;
        foreach (var block in _edit.Draft.Blocks.Where(b => b.BlockType is LadderBlockType.Function or LadderBlockType.FunctionBlock))
        { target.AddItem(block.Name + " · " + block.BlockType + " · " + block.SemanticMode); target.SetItemMetadata(target.ItemCount - 1, block.Id); if (block.Id == _edit.Call.CallTarget) target.Select(target.ItemCount - 1); }
        if (_edit.Target is null) target.Select(-1);
        target.ItemSelected += index => Run(() => { _edit.SelectTarget(target.GetItemMetadata((int)index).AsString()); Rebuild(); });
        var blockTarget = _edit.Target;
        _body.AddChild(new Label { Name = "CallSemanticMode", Text = "Mode: " + _edit.Call.CallSemanticMode });
        if (blockTarget is null) return;
        if (_edit.Call.CallSemanticMode != blockTarget.SemanticMode)
        {
            var matchMode = new Button { Name = "MatchCallTargetMode", Text = "Use target mode: " + blockTarget.SemanticMode };
            _body.AddChild(matchMode); _fields["mode"] = matchMode;
            matchMode.Pressed += () => Run(() => { _edit.SelectTarget(blockTarget.Id); Rebuild(); });
        }
        if (blockTarget.SemanticMode == LadderSemanticMode.Legacy)
        {
            _body.AddChild(new Label { Text = "Legacy calls share global tags; interface declarations do not pass values.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            var convert = new Button { Name = "ConvertCallTarget", Text = "Explicitly convert target to parameterized mode" }; _body.AddChild(convert); _fields["mode"] = convert;
            _body.AddChild(new Label { Text = "Conversion keeps existing operands bound to globals. Other call sites require explicit rebinding. Apply is one undoable change.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            convert.Pressed += () => Run(() => { _edit.ConvertTargetToParameterized(); Rebuild(); }); return;
        }
        if (blockTarget.BlockType == LadderBlockType.FunctionBlock)
        {
            _body.AddChild(new Label { Text = "FB instance (identity survives renaming)" }); var instances = new OptionButton { Name = "CallInstance" }; _body.AddChild(instances); _fields["instance"] = instances;
            instances.AddItem("Choose an instance"); instances.SetItemMetadata(0, "");
            foreach (var instance in _edit.Draft.Instances.Where(i => i.BlockId == blockTarget.Id))
            { instances.AddItem(instance.Name); instances.SetItemMetadata(instances.ItemCount - 1, instance.Id); if (instance.Id == _edit.Call.CallInstanceId) instances.Select(instances.ItemCount - 1); }
            instances.ItemSelected += index => _edit.Call.CallInstanceId = instances.GetItemMetadata((int)index).AsString();
            var row = new HBoxContainer(); _body.AddChild(row); var name = new LineEdit { Name = "CallInstanceName", PlaceholderText = "New or renamed instance", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; row.AddChild(name);
            var create = new Button { Name = "CreateCallInstance", Text = "Create" }; var rename = new Button { Name = "RenameCallInstance", Text = "Rename selected" }; row.AddChild(create); row.AddChild(rename);
            create.Pressed += () => Run(() => { if (ReadBindings() is { } error) throw new InvalidOperationException(error); _edit.CreateInstance(name.Text); Rebuild(); });
            rename.Pressed += () => Run(() => { if (ReadBindings() is { } error) throw new InvalidOperationException(error); _edit.RenameInstance(_edit.Call.CallInstanceId, name.Text); Rebuild(); });
        }
        var program = _edit.Draft.BuildProgram(); var owner = program.Blocks!.Single(b => b.Id == _edit.Owner.Id);
        var actuals = new List<(string Reference, string Label, PlcVariable Declaration)>();
        foreach (var global in PlcAggregates.Expand(_edit.Draft.Tags))
        {
            var root = _edit.Draft.Tags.FirstOrDefault(t => t.Name == global.Name || PlcAggregates.IsAggregate(t.Type) && (global.Name.StartsWith(t.Name + ".", StringComparison.Ordinal) || global.Name.StartsWith(t.Name + "[", StringComparison.Ordinal)));
            if (root is null) continue; var reference = "global:" + LadderCallBindings.GlobalId(root) + global.Name[root.Name.Length..];
            if (LadderCallBindings.ResolveDeclaration(program, owner, reference) is { } declaration) actuals.Add((reference, global.Name + " (global)", declaration));
        }
        if (owner.SemanticMode == LadderSemanticMode.Parameterized)
            foreach (var formal in owner.Interface ?? [])
                if (LadderCallBindings.ResolveDeclaration(program, owner, "local:" + formal.Id) is { } declaration) actuals.Add(("local:" + formal.Id, formal.Name + " (local)", declaration));
        foreach (var formal in blockTarget.Interface.Where(p => p.Section is LadderInterfaceSection.Input or LadderInterfaceSection.Output or LadderInterfaceSection.InOut))
        {
            _body.AddChild(new Label { Text = formal.Name + " · " + formal.Section + " · " + formal.Type });
            var row = new HBoxContainer(); _body.AddChild(row); var kind = new OptionButton { Name = "ActualKind_" + formal.Id }; var symbol = new OptionButton { Name = "ActualSymbol_" + formal.Id, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; var literal = new LineEdit { Name = "ActualLiteral_" + formal.Id, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            row.AddChild(kind); row.AddChild(symbol); row.AddChild(literal); _fields["binding:" + formal.Id] = kind;
            kind.AddItem("Symbol", (int)LadderActualKind.Symbol);
            if (formal.Section == LadderInterfaceSection.Input) kind.AddItem("Literal", (int)LadderActualKind.Literal);
            if (formal.Section == LadderInterfaceSection.Output) kind.AddItem("Discard", (int)LadderActualKind.Discard);
            symbol.AddItem("Choose " + formal.Type + " symbol"); symbol.SetItemMetadata(0, "");
            foreach (var actual in actuals.Where(a => a.Declaration.Type == formal.Type && (a.Declaration.EnumMembers ?? []).SequenceEqual(formal.EnumMembers ?? []) && (formal.Section == LadderInterfaceSection.Input || a.Declaration.Role != PlcVariableRole.Input)))
            { symbol.AddItem(actual.Label); symbol.SetItemMetadata(symbol.ItemCount - 1, actual.Reference); }
            var existing = _edit.Call.CallBindings.FirstOrDefault(b => b.ParameterId == formal.Id);
            if (existing is not null)
            {
                var selectedKind = kind.GetItemIndex((int)existing.Kind); if (selectedKind >= 0) kind.Select(selectedKind);
                literal.Text = Convert.ToString(existing.Literal, CultureInfo.InvariantCulture) ?? "";
                var selectedSymbol = Enumerable.Range(0, symbol.ItemCount).FirstOrDefault(i => symbol.GetItemMetadata(i).AsString() == existing.Symbol, -1);
                if (selectedSymbol >= 0) symbol.Select(selectedSymbol);
                else if (existing.Symbol.Length > 0) { symbol.AddItem("Invalid: " + existing.Symbol); symbol.SetItemMetadata(symbol.ItemCount - 1, existing.Symbol); symbol.Select(symbol.ItemCount - 1); }
            }
            void Visibility() { symbol.Visible = kind.GetSelectedId() == (int)LadderActualKind.Symbol; literal.Visible = kind.GetSelectedId() == (int)LadderActualKind.Literal; _fields["binding:" + formal.Id] = literal.Visible ? literal : symbol.Visible ? symbol : kind; }
            kind.ItemSelected += _ => Visibility(); Visibility();
            _readBindings.Add(() =>
            {
                var actualKind = (LadderActualKind)kind.GetSelectedId(); object? value = null;
                if (actualKind == LadderActualKind.Literal && !LadderEditorDocument.TryParseInitialValue(formal.Type, literal.Text, out value, out var error)) { NavigateToField("binding:" + formal.Id); return formal.Name + ": " + error; }
                _edit.Bind(new(formal.Id, actualKind, Symbol: symbol.GetItemMetadata(symbol.Selected).AsString(), Literal: value, LiteralType: formal.Type, EnumMembers: formal.EnumMembers)); return null;
            });
        }
    }
    private string? ReadBindings() { foreach (var read in _readBindings) if (read() is { } error) return error; return null; }
    public async void NavigateToField(string field)
    {
        if (!_fields.TryGetValue(field, out var control)) return;
        var request = ++_focusRequest;
        // Pressed is emitted during pointer dispatch. Reclaim focus after that dispatch
        // and layout settle, otherwise the Apply button can take it back on release.
        var tree = GetTree();
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !Visible || request != _focusRequest) return;
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !Visible || request != _focusRequest || !GodotObject.IsInstanceValid(control) || !control.IsVisibleInTree()) return;
        _scroll.EnsureControlVisible(control); control.GrabFocus();
        if (control is LineEdit text) text.SelectAll();
        GD.Print("CALL_ACTUAL_FOCUS field=" + field + " control=" + control.Name + " focused=" + control.HasFocus());
    }
    public bool TryApply()
    {
        if (ReadBindings() is { } error) { _status.Text = error; return false; }
        var issue = _edit.Validate().FirstOrDefault(); if (issue is not null) { _status.Text = issue.Message; NavigateToField(issue.Field); return false; }
        if (!_edit.Commit(_history, _locked(), out error)) { _status.Text = error; return false; }
        Hide(); _committed(); return true;
    }
}
