using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public static class LadderEditorZoom
{
    public static HBoxContainer CreateControls(LadderEditorCanvas canvas, ScrollContainer scroll)
    {
        var row = new LadderEditorZoomRow { Name = "LadderZoomControls" };
        var label = new Label { Text = "  Ladder zoom", VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeColorOverride("font_color", new Color("263943")); label.AddThemeFontSizeOverride("font_size", 12); row.AddChild(label);
        var selector = new OptionButton { Name = "LadderZoomSelector", CustomMinimumSize = new Vector2(92, 32),
            TooltipText = "Zoom the ladder view. Use scrollbars to reach offscreen instructions." };
        foreach (var percent in new[] { 75, 100, 150 }) selector.AddItem(percent + "%", percent);
        selector.AddThemeColorOverride("font_color", new Color("263943")); selector.AddThemeFontSizeOverride("font_size", 12);
        var style = new StyleBoxFlat { BgColor = new Color("e9edef"), BorderColor = new Color("aab6bc"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 10, ContentMarginRight = 24, ContentMarginTop = 4, ContentMarginBottom = 4 };
        selector.AddThemeStyleboxOverride("normal", style); selector.AddThemeStyleboxOverride("hover", style); selector.AddThemeStyleboxOverride("pressed", style);
        selector.Select(1); row.AddChild(selector);
        canvas.ZoomPercentChanged += percent => selector.Select(percent == 75 ? 0 : percent == 100 ? 1 : 2);
        selector.ItemSelected += index =>
        {
            canvas.SetZoomPercent(selector.GetItemId((int)index));
            // Minimum-size changes schedule Container layout before this reveal.
            Callable.From(() => RevealSelection(canvas, scroll)).CallDeferred();
        };
        canvas.KeyboardSelectionChanged += () => RevealSelection(canvas, scroll);
        // Culling commands are cached by Godot; scrolling must redraw the newly visible instructions.
        scroll.GetHScrollBar().ValueChanged += _ => canvas.QueueRedraw();
        scroll.GetVScrollBar().ValueChanged += _ => canvas.QueueRedraw();
        scroll.Resized += canvas.QueueRedraw;
        return row;
    }
    public static void RevealSelection(LadderEditorCanvas canvas, ScrollContainer scroll)
    {
        var bounds = canvas.GetSelectionBounds();
        scroll.ScrollHorizontal = LadderEditorZoomGeometry.Reveal(scroll.ScrollHorizontal, scroll.Size.X, bounds.Position.X, bounds.End.X);
        scroll.ScrollVertical = LadderEditorZoomGeometry.Reveal(scroll.ScrollVertical, scroll.Size.Y, bounds.Position.Y, bounds.End.Y);
    }
}

public partial class LadderEditorZoomRow : HBoxContainer
{
    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), new Color("dfe4e7"));
}

public partial class LadderEditorCanvas
{
    private LadderEditorZoomGeometry _zoom = new(100);
    public int ZoomPercent { get; private set; } = 100;
    public float ZoomFactor => _zoom.Factor;
    public event System.Action<int>? ZoomPercentChanged;
    private Vector2 LogicalViewportSize => Size / ZoomFactor;
    private Vector2 ToCanvas(Vector2 logical) => logical * ZoomFactor;
    private Vector2 ToLogical(Vector2 physical) => physical / ZoomFactor;
    private Rect2 ToCanvas(Rect2 logical) => new(ToCanvas(logical.Position), ToCanvas(logical.Size));
    public void SetZoomPercent(int percent)
    {
        var transform = new LadderEditorZoomGeometry(percent);
        ZoomPercent = percent; _zoom = transform;
        RefreshDocument();
        ZoomPercentChanged?.Invoke(percent);
    }
    public LadderEditorViewState CaptureViewState(int horizontalScroll, int verticalScroll) => new(
        _document.Rungs.Count == 0 ? "" : _document.Rungs[SelectedRung].Id,
        SelectedNodeId, OutputSelected, _selectedTreeSlot?.ParentId ?? "", _selectedTreeSlot?.ChildIndex ?? -1,
        horizontalScroll, verticalScroll, ZoomPercent);
    public bool RestoreViewState(LadderEditorViewState state)
    {
        SetZoomPercent(state.ZoomPercent);
        var rung = _document.Rungs.FindIndex(r => r.Id == state.NetworkId);
        if (rung < 0) { SelectRung(0); return false; }
        SelectRung(rung);
        if (state.Output) { SelectElement(rung, -1, -1, true, notify: false); return true; }
        if (string.IsNullOrEmpty(state.NodeId)) return true;
        if (EditableLadderTree.Find(_document.Rungs[rung].Root, state.NodeId) is null) return false;
        SelectTreeNode(rung, state.NodeId, notify: false);
        if (state.Slot >= 0)
        {
            _selectedTreeSlot = System.Linq.Enumerable.FirstOrDefault(TreeLayout(_document.Rungs[rung]).Slots,
                s => s.ParentId == state.ParentId && s.ChildIndex == state.Slot);
            if (_selectedTreeSlot is null) { SelectRung(rung); return false; }
            var branch = _document.Rungs[rung].Branches.FindIndex(b => b.Id == state.ParentId);
            if (_document.Rungs[rung].IsFlat && branch >= 0) SelectInsertionPoint(rung, branch, state.Slot, notify: false);
        }
        QueueRedraw(); return true;
    }
}
