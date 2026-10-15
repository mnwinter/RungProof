using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>Point-of-use operand form. Text changes and blur only stage; Enter/Apply explicitly commits.</summary>
public partial class LadderOperandEditor : PanelContainer
{
    private readonly LineEdit _text = new() { Name = "OperandText", CustomMinimumSize = new Vector2(116, 22) };
    private readonly ItemList _suggestions = new() { Name = "OperandSuggestions", CustomMinimumSize = new Vector2(220, 100), Visible = false };
    private readonly Label _status = new() { Name = "OperandStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Button _create = new() { Name = "CreateAndAssignOperand", Text = "Create global tag and assign" };
    private LadderOperandSymbol[] _symbols = [];
    private Func<string, bool, string?>? _commit;
    private Action? _cancel;
    public string PendingText => _text.Text;
    private bool _inlineMode;
    private bool _createRequested;
    private Vector2 _inlineSize;
    public void PlaceInline(Rect2 bounds)
    {
        _inlineMode = true;
        _inlineSize = bounds.Size;
        Position = bounds.Position;
        CustomMinimumSize = bounds.Size;
        Size = bounds.Size;
        if (GetChild(0) is VBoxContainer body)
        {
            if (body.GetChildCount() > 0 && body.GetChild(0) is Control title) title.Visible = false;
            for (var index = 0; index < body.GetChildCount(); index++)
                if (body.GetChild(index) is HBoxContainer actions) actions.Visible = false;
        }
        _suggestions.Visible = false;
        _status.Visible = false;
        _create.Visible = false;
        _createRequested = false;
        var field = new StyleBoxFlat
        {
            BgColor = new Color("ffffff"), BorderColor = new Color("276fbd"),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 3, ContentMarginRight = 3, ContentMarginTop = 0, ContentMarginBottom = 0
        };
        _text.AddThemeStyleboxOverride("normal", field);
        _text.AddThemeStyleboxOverride("focus", field);
        _text.AddThemeColorOverride("font_color", new Color("17242c"));
        _text.AddThemeColorOverride("caret_color", new Color("17242c"));
        _text.AddThemeFontSizeOverride("font_size", 12);
        AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        AddThemeConstantOverride("h_separation", 0);
        AddThemeConstantOverride("v_separation", 0);
        ResizeInlineForm();
    }

    public LadderOperandEditor()
    {
        Name = "InlineOperandEditor"; ZIndex = 40; CustomMinimumSize = new Vector2(116, 22);
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("14232d"), BorderColor = new Color("6697a8"),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12,
            ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 6
        });
        _status.AddThemeColorOverride("font_color", new Color("e0edf3"));
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 2); AddChild(body);
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
            else if (input is InputEventKey { Pressed: true, CtrlPressed: true, ShiftPressed: true, Keycode: Key.I })
            {
                _createRequested = true;
                _create.Visible = _text.Text.Trim().Length > 0 && !_symbols.Any(s => s.Name == _text.Text.Trim());
                ResizeInlineForm(); _text.AcceptEvent();
            }
            else if (input is InputEventKey { Pressed: true, CtrlPressed: true, Keycode: Key.I })
            { _suggestions.Visible = !_suggestions.Visible; ResizeInlineForm(); _text.AcceptEvent(); }
        };
        _suggestions.ItemActivated += index => { _text.Text = _suggestions.GetItemMetadata((int)index).AsString(); _text.GrabFocus(); _text.CaretColumn = _text.Text.Length; RefreshSuggestions(); };
        apply.Pressed += () => TryApply(false); cancel.Pressed += CancelEdit;
        _create.Pressed += () => TryApply(true);
        Visible = false;
    }

    public void Begin(string current, IEnumerable<LadderOperandSymbol> symbols, Func<string, bool, string?> commit, Action cancel,
        string? initialText = null, bool clearOnOpen = false)
    {
        _inlineMode = false;
        _symbols = symbols.DistinctBy(s => s.Name).OrderBy(s => s.DisplayName, StringComparer.Ordinal).ToArray();
        _commit = commit; _cancel = cancel; _text.Text = clearOnOpen ? "" : initialText ?? current; Show(); RefreshSuggestions();
        _text.GrabFocus();
        if (!clearOnOpen && initialText is null) _text.SelectAll();
        else _text.CaretColumn = _text.Text.Length;
    }
    public bool TryApply(bool create)
    {
        var error = _commit?.Invoke(_text.Text.Trim(), create) ?? "No active operand target.";
        if (error.Length > 0)
        {
            _status.Text = error; _status.Visible = true; _status.Modulate = new Color("a12828");
            TooltipText = error;
            ResizeInlineForm();
            return false;
        }
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
        if (_inlineMode && !_createRequested) _create.Visible = false;
        if (_inlineMode) _status.Visible = false;
        _status.Text = "Pending edit · Enter applies; Escape cancels"; _status.Modulate = Colors.White;
        if (_inlineMode) ResizeInlineForm();
    }

    private void ResizeInlineForm()
    {
        if (!_inlineMode) return;
        var extra = (_suggestions.Visible ? _suggestions.CustomMinimumSize.Y : 0)
            + (_create.Visible ? _create.GetCombinedMinimumSize().Y : 0)
            + (_status.Visible ? _status.GetCombinedMinimumSize().Y : 0);
        Size = new Vector2(_inlineSize.X, _inlineSize.Y + extra);
        CustomMinimumSize = Size;
    }
}
