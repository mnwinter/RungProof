using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>Point-of-use operand form. Text changes and blur only stage; Enter/Apply explicitly commits.</summary>
public partial class LadderOperandEditor : PanelContainer
{
    private readonly LineEdit _text = new() { Name = "OperandText", CustomMinimumSize = new Vector2(260, 32) };
    private readonly ItemList _suggestions = new() { Name = "OperandSuggestions", CustomMinimumSize = new Vector2(260, 100) };
    private readonly Label _status = new() { Name = "OperandStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Button _create = new() { Name = "CreateAndAssignOperand", Text = "Create global tag and assign" };
    private LadderOperandSymbol[] _symbols = [];
    private Func<string, bool, string?>? _commit;
    private Action? _cancel;
    public string PendingText => _text.Text;
    private Vector2 _anchor;

    public void PlaceNear(Vector2 viewportAnchor)
    {
        _anchor = viewportAnchor;
        Reposition();
        Callable.From(Reposition).CallDeferred();
    }

    private void Reposition()
    {
        if (!Visible || !IsInsideTree()) return;
        var viewport = GetViewport().GetVisibleRect();
        var extent = GetCombinedMinimumSize().Max(new Vector2(380, 0));
        Size = extent;
        var x = Mathf.Clamp(_anchor.X - 80, viewport.Position.X + 8, Math.Max(viewport.Position.X + 8, viewport.End.X - extent.X - 8));
        var y = _anchor.Y + 28;
        if (y + extent.Y > viewport.End.Y - 8) y = _anchor.Y - extent.Y - 16;
        y = Mathf.Clamp(y, viewport.Position.Y + 8, Math.Max(viewport.Position.Y + 8, viewport.End.Y - extent.Y - 8));
        Position = new Vector2(x, y);
    }

    public LadderOperandEditor()
    {
        Name = "InlineOperandEditor"; ZIndex = 40; CustomMinimumSize = new Vector2(380, 0);
        Resized += () => { if (Visible) Callable.From(Reposition).CallDeferred(); };
        MinimumSizeChanged += () => { if (Visible) Callable.From(Reposition).CallDeferred(); };
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("14232d"), BorderColor = new Color("6697a8"),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12,
            ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 6
        });
        _status.AddThemeColorOverride("font_color", new Color("e0edf3"));
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 8); AddChild(body);
        body.AddChild(new Label { Text = "OPERAND · staged until Apply" });
        body.AddChild(_text); body.AddChild(_suggestions); body.AddChild(_status); body.AddChild(_create);
        var actions = new HBoxContainer(); body.AddChild(actions);
        var apply = new Button { Name = "ApplyOperand", Text = "Apply" };
        var cancel = new Button { Name = "CancelOperand", Text = "Cancel" };
        actions.AddChild(apply); actions.AddChild(cancel);
        _text.TextChanged += _ => RefreshSuggestions();
        _text.TextSubmitted += _ => TryApply(false);
        _text.FocusExited += () => { if (Visible) _status.Text = "Pending edit · Apply or Cancel"; };
        _text.GuiInput += input =>
        {
            if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }) { CancelEdit(); _text.AcceptEvent(); }
        };
        _suggestions.ItemActivated += index => { _text.Text = _suggestions.GetItemMetadata((int)index).AsString(); _text.GrabFocus(); _text.CaretColumn = _text.Text.Length; RefreshSuggestions(); };
        apply.Pressed += () => TryApply(false); cancel.Pressed += CancelEdit;
        _create.Pressed += () => TryApply(true);
        Visible = false;
    }

    public void Begin(string current, IEnumerable<LadderOperandSymbol> symbols, Func<string, bool, string?> commit, Action cancel)
    {
        _symbols = symbols.DistinctBy(s => s.Name).OrderBy(s => s.DisplayName, StringComparer.Ordinal).ToArray();
        _commit = commit; _cancel = cancel; _text.Text = current; Show(); RefreshSuggestions();
        _text.GrabFocus(); _text.SelectAll();
    }
    public bool TryApply(bool create)
    {
        var error = _commit?.Invoke(_text.Text.Trim(), create) ?? "No active operand target.";
        if (error.Length > 0) { _status.Text = error; _status.Modulate = new Color("ffb5a9"); return false; }
        Hide(); _commit = null; _cancel = null; return true;
    }
    public void CancelEdit() { _cancel?.Invoke(); _commit = null; _cancel = null; Hide(); }
    private void RefreshSuggestions()
    {
        _suggestions.Clear();
        foreach (var symbol in _symbols.Where(s => s.Name.Contains(_text.Text, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Contains(_text.Text, StringComparison.OrdinalIgnoreCase)).Take(40))
        {
            var label = symbol.DisplayName == symbol.Name ? symbol.Name : symbol.DisplayName + " · " + symbol.Name;
            _suggestions.AddItem(label + " · " + symbol.Type);
            _suggestions.SetItemMetadata(_suggestions.ItemCount - 1, symbol.Name);
        }
        _create.Visible = _text.Text.Trim().Length > 0 && !_symbols.Any(s => s.Name == _text.Text.Trim());
        _status.Text = "Pending edit · Enter applies; Escape cancels"; _status.Modulate = Colors.White;
    }
}
