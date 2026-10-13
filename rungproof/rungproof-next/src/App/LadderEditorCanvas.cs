using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public enum LadderVendorStyle { SiemensTia, RockwellLogix }

/// <summary>Graphical editor surface for a simulator-owned Ladder document.</summary>
public partial class LadderEditorCanvas : Control
{
    private const float FirstRungTop = 90.0f;
    private const float BaseRungHeight = 205.0f;
    private const float BaseRungAdvance = 220.0f;
    private const float MultiBranchGap = 92.0f;
    private const float ContactConductorHalfGap = 20.0f;
    private readonly LadderVendorStyle _style;
    private readonly LadderEditorDocument _document;
    private VirtualControllerSnapshot? _monitorSnapshot;
    private bool _monitorPaused;
    // Scheduling is held independently of the accepted PLC image's Running state.
    public void SetMonitorPaused(bool paused) { _monitorPaused=paused; QueueRedraw(); }
    public bool MonitorPaused => _monitorPaused;
    public string MonitorStatusText => _monitorSnapshot is null ? "OFFLINE EDIT"
        : $"MONITOR · {(_monitorPaused ? "PAUSED" : _monitorSnapshot.State.ToString().ToUpperInvariant())} · SCAN {_monitorSnapshot.ScanNumber}";
    public int SelectedRung { get; private set; }
    public int SelectedBranch { get; private set; } = -1;
    public int SelectedContact { get; private set; } = -1;
    public int SelectedInsertionIndex { get; private set; } = -1;
    public bool OutputSelected { get; private set; }
    public bool MonitorActive => _monitorSnapshot is not null;
    public long MonitorScanNumber => _monitorSnapshot?.ScanNumber ?? 0;
    public event Action<int>? RungSelected;
    public event Action<int, int, int, bool>? ElementSelected;
    public event Action<int, int, int>? InsertionPointSelected;
    public event Action<int, int, int, bool>? PropertiesRequested;
    public event Action<int, int, int, bool>? ContextMenuRequested;
    public bool MutationLocked { get; set; }
    private bool _challengeReadable;
    public bool ChallengeReadable
    {
        get => _challengeReadable;
        set { _challengeReadable = value; RefreshDocument(); }
    }
    public event Action<string>? KeyboardInstructionRequested;
    public event Action? KeyboardSelectionChanged;
    public event Action? UndoEditRequested;
    public event Action? RedoEditRequested;
    public event Action? SaveDraftRequested;
    public event Action? InlineOperandRequested;
    public event Action? SaveAsRequested;
    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action? DeleteRequested;
    public event Action<int>? MoveRequested;
    public event Action<string, int, int, int>? InstructionDropRequested;
    public event Action<LadderTreeEditorRequest>? TreeCommandRequested;
    public EditableLadderNode? TreeClipboard { get; set; }
    public string SelectedNodeId { get; private set; } = string.Empty;
    private LadderTreeSlot? _selectedTreeSlot;
    private PopupMenu? _treePopup;
    private LadderTreeEditorTarget? _treePopupTarget;

    private sealed class TreeLayoutCache { public EditableLadderNode? Root; public EditableLadderLayout? Layout; public bool Legacy; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EditableRung, TreeLayoutCache> TreeLayouts = new();
    private static EditableLadderLayout TreeLayout(EditableRung rung)
    {
        var cache = TreeLayouts.GetOrCreateValue(rung);
        if (!ReferenceEquals(cache.Root, rung.Root) || cache.Layout is null || cache.Legacy != rung.LegacyEmptyBranchSemantics)
        {
            cache.Layout = EditableLadderLayout.Measure(rung.Root,
                contactWidth: Math.Max(240, EditableLadderTree.Visit(rung.Root).Where(n => n.IsLeaf)
                    .Select(n => (n.Kind == LadderNodeKind.Compare ? n.Variable.Length + n.RightOperand.Length + 3 : n.Variable.Length) * 7 + 36).DefaultIfEmpty(144).Max()),
                leafHeight: 136, legacyEmptyBranchSemantics: rung.LegacyEmptyBranchSemantics);
            cache.Root = rung.Root;
            cache.Legacy = rung.LegacyEmptyBranchSemantics;
        }
        return cache.Layout;
    }
    private Vector2 TreeOrigin(int rungIndex) => new(_style == LadderVendorStyle.SiemensTia ? 64 : 78, RungTop(rungIndex) + 60);
    private Vector2 TreeOutputPosition(int rungIndex)
    {
        var layout = TreeLayout(_document.Rungs[rungIndex]); var origin = TreeOrigin(rungIndex);
        return new(origin.X + layout.Width + 190, origin.Y + layout.Height / 2);
    }
    private bool LeafCoordinates(EditableRung rung, string id, out int branch, out int contact)
    {
        for (branch = 0; branch < rung.Branches.Count; branch++)
        { contact = rung.Branches[branch].Contacts.FindIndex(n => n.Id == id); if (contact >= 0) return true; }
        branch = contact = -1; return false;
    }
    public void SelectTreeNode(int rungIndex, string nodeId, bool notify = true)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count) return;
        var rung = _document.Rungs[rungIndex];
        if (EditableLadderTree.Find(rung.Root, nodeId) is null) return;
        if (LeafCoordinates(rung, nodeId, out var branch, out var contact)) SelectElement(rungIndex, branch, contact, notify: notify);
        else { SelectedRung = rungIndex; ClearElementSelection(); SelectedNodeId = nodeId; if (notify) RungSelected?.Invoke(rungIndex); QueueRedraw(); }
    }
    private LadderTreeEditorTarget TreeTarget() => new(_document.Revision,
        _document.Blocks[_document.ActiveBlockIndex].Id, _document.Rungs[SelectedRung].Id,
        string.IsNullOrEmpty(SelectedNodeId) ? _document.Rungs[SelectedRung].Root.Id : SelectedNodeId,
        _selectedTreeSlot?.ParentId ?? "", _selectedTreeSlot?.ChildIndex ?? -1);
    public LadderTreeEditorTarget CaptureTreeTarget() => TreeTarget();
    public Vector2 GetSelectionPosition()
    {
        if (_document.Rungs.Count == 0) return Vector2.Zero;
        if (OutputSelected) return ToCanvas(TreeOutputPosition(SelectedRung));
        if (_selectedTreeSlot is not null) return ToCanvas(TreeOrigin(SelectedRung) + new Vector2(_selectedTreeSlot.X, _selectedTreeSlot.Y));
        var node = TreeLayout(_document.Rungs[SelectedRung]).Nodes.FirstOrDefault(n => n.Node.Id == SelectedNodeId);
        return node is null ? GetRungBounds(SelectedRung).Position
            : ToCanvas(TreeOrigin(SelectedRung) + new Vector2(node.Bounds.X + node.Bounds.Width / 2, node.ContinuityY));
    }
    private EditableLadderNode NewContactTemplate() => new("clipboard-contact", LadderNodeKind.Contact,
        _document.Tags.FirstOrDefault(t => t.Type == PlcVariableType.Bool)?.Name ?? "");
    private EditableLadderNode NewComparisonTemplate(LadderCompareOperator comparison = LadderCompareOperator.Equal) =>
        new("clipboard-comparison", LadderNodeKind.Compare,
            _document.Tags.FirstOrDefault(t => t.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)?.Name ?? "0",
            CompareOperator: comparison, RightOperand: "0");
    private void ShowTreePopup()
    {
        if (_treePopup is null)
        {
            _treePopup = new PopupMenu(); AddChild(_treePopup);
            _treePopup.IdPressed += id =>
            {
                if (_treePopupTarget is null) return;
                var comparison = id >= 1000;
                var command = (LadderTreeCommandId)(comparison ? id - 1000 : id);
                var payload = command is LadderTreeCommandId.PasteAt or LadderTreeCommandId.PasteAfter or LadderTreeCommandId.PasteBefore ? TreeClipboard
                    : command is LadderTreeCommandId.InsertAt or LadderTreeCommandId.InsertAfter or LadderTreeCommandId.InsertBefore ? comparison ? NewComparisonTemplate() : NewContactTemplate() : null;
                TreeCommandRequested?.Invoke(new(command, _treePopupTarget, payload));
            };
        }
        _treePopupTarget = TreeTarget(); _treePopup.Clear();
        _treePopup.AddItem("Add parallel path around subtree", (int)LadderTreeCommandId.WrapParallel);
        _treePopup.AddItem("Insert contact before subtree", (int)LadderTreeCommandId.InsertBefore);
        _treePopup.AddItem("Insert comparison before subtree", 1000 + (int)LadderTreeCommandId.InsertBefore);
        _treePopup.AddItem(_selectedTreeSlot is null ? "Insert contact after subtree" : "Insert contact at wire slot",
            (int)(_selectedTreeSlot is null ? LadderTreeCommandId.InsertAfter : LadderTreeCommandId.InsertAt));
        _treePopup.AddItem(_selectedTreeSlot is null ? "Insert comparison after subtree" : "Insert comparison at wire slot",
            1000 + (int)(_selectedTreeSlot is null ? LadderTreeCommandId.InsertAfter : LadderTreeCommandId.InsertAt));
        _treePopup.AddItem("Copy subtree", (int)LadderTreeCommandId.Copy);
        _treePopup.AddItem("Cut subtree", (int)LadderTreeCommandId.Cut);
        _treePopup.AddItem(_selectedTreeSlot is null ? "Paste subtree after" : "Paste subtree at slot",
            (int)(_selectedTreeSlot is null ? LadderTreeCommandId.PasteAfter : LadderTreeCommandId.PasteAt));
        _treePopup.AddItem("Paste subtree before", (int)LadderTreeCommandId.PasteBefore);
        _treePopup.AddItem("Remove selected subtree", (int)LadderTreeCommandId.Delete);
        _treePopup.AddItem("Move subtree left / path up", (int)LadderTreeCommandId.MoveLeft);
        _treePopup.AddItem("Move subtree right / path down", (int)LadderTreeCommandId.MoveRight);
        if (_document.Rungs[SelectedRung].LegacyEmptyBranchSemantics)
            _treePopup.AddItem("Convert legacy semantics (empty wire = TRUE)", (int)LadderTreeCommandId.ConvertLegacy);
        _treePopup.Position = (Vector2I)GetGlobalMousePosition();
        _treePopup.Popup();
        var viewport = GetViewport().GetVisibleRect().Size;
        var popupSize = _treePopup.Size;
        _treePopup.Position = new Vector2I(
            Math.Clamp(_treePopup.Position.X, 0, Math.Max(0, (int)viewport.X - popupSize.X - 8)),
            Math.Clamp(_treePopup.Position.Y, 0, Math.Max(0, (int)viewport.Y - popupSize.Y - 8)));
    }

    private void DrawRecursiveConditions(int rungIndex, EditableRung rung, EditableLadderLayout layout, Vector2 origin, Color wire, Color live)
    {
        foreach (var line in layout.Wires)
            if (MonitorBoundsVisible(new Rect2(origin + new Vector2(Math.Min(line.X1,line.X2),Math.Min(line.Y1,line.Y2)), new Vector2(Math.Max(1,Math.Abs(line.X2-line.X1)),Math.Max(1,Math.Abs(line.Y2-line.Y1))))))
                DrawLine(origin + new Vector2(line.X1, line.Y1), origin + new Vector2(line.X2, line.Y2), wire, 2);
        foreach (var placed in layout.Nodes)
        {
            var node = placed.Node;
            if (!MonitorBoundsVisible(new Rect2(origin + new Vector2(placed.Bounds.X, placed.Bounds.Y), new Vector2(placed.Bounds.Width, placed.Bounds.Height)))) continue;
            if (node.IsLeaf) { if (LastDrawnInstructionCount >= 200) continue; LastDrawnInstructionCount++; }
            var x = origin.X + placed.Bounds.X + placed.Bounds.Width / 2;
            var y = origin.Y + placed.ContinuityY;
            if (node.IsLeaf)
            {
                var color = IsElementEnergized(node.Id) ? live : wire;
                var comparisonWidth = Math.Max(116, placed.Bounds.Width - 24);
                var gap = node.Kind == LadderNodeKind.Compare ? comparisonWidth / 2 : ContactConductorHalfGap;
                DrawConductor(origin.X + placed.Bounds.X, x - gap, y, wire, 2);
                DrawConductor(x + gap, origin.X + placed.Bounds.X + placed.Bounds.Width, y, wire, 2);
                if (node.Kind == LadderNodeKind.Compare)
                {
                    DrawRect(new Rect2(x - comparisonWidth / 2, y - 32, comparisonWidth, 64), new Color("e3e6eb"));
                    DrawRect(new Rect2(x - comparisonWidth / 2, y - 32, comparisonWidth, 64), color, false, 2);
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - comparisonWidth / 2 + 6, y - 8), node.CompareOperator.ToString(), HorizontalAlignment.Left, comparisonWidth - 12, 10, color);
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - comparisonWidth / 2 + 6, y + 12), node.Variable + ", " + node.RightOperand, HorizontalAlignment.Left, comparisonWidth - 12, 10, color);
                }
                else
                {
                    DrawContact(x, y, node.NormallyClosed, color);
                    if (node.EdgeMode != LadderEdgeMode.None)
                        DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 24, y + 7), node.EdgeMode == LadderEdgeMode.Rising ? "P" : "N", HorizontalAlignment.Center, 48, 11, color);
                    DrawContactOperand(x, y, node.Variable, placed.Bounds.Width - 8);
                }
            }
            else if ((node.Children?.Count ?? 0) == 0)
            {
                if (placed.IgnoredLegacy)
                {
                    DrawRect(new Rect2(x - 68, y - 8, 136, 26), new Color("fff2da"));
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 65, y + 10), "LEGACY DRAFT · IGNORED", HorizontalAlignment.Center, 130, 9, new Color("9a5800"));
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 70, y + 34), "Shift+F10 → Convert", HorizontalAlignment.Center, 140, 9, new Color("9a5800"));
                }
                else DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 65, y - 12), node.Kind == LadderNodeKind.Series ? "EMPTY SERIES · TRUE" : "EMPTY PARALLEL · FALSE", HorizontalAlignment.Center, 130, 10, wire);
            }
            if (node.IsLeaf) DrawConditionObservation(node, x, y, placed.Bounds.Width - 12);
            if (rungIndex == SelectedRung && SelectedNodeId == node.Id)
                DrawRect(new Rect2(origin + new Vector2(placed.Bounds.X, placed.Bounds.Y), new(placed.Bounds.Width, placed.Bounds.Height)), new Color("0087a8"), false, 2);
        }
        if (rungIndex == SelectedRung && _selectedTreeSlot is not null)
            DrawCircle(origin + new Vector2(_selectedTreeSlot.X, _selectedTreeSlot.Y), 9, new Color("0087a8"), false, 2);
    }

    public LadderEditorCanvas(LadderVendorStyle style, LadderEditorDocument document)
    {
        _style = style;
        _document = document;
        Name = "GraphicalLadderCanvas";
        CustomMinimumSize = new Vector2(760, 500);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        ClipContents = true;
        FocusMode = FocusModeEnum.All;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        InitializeMonitoringInspector();
    }

    public void RefreshDocument()
    {
        // Wider logical canvas is scrolled, never reduced to unreadable operand text.
        // Reserve readable counter operands and the wire on either side. Narrow
        // editor docks scroll the logical canvas instead of shrinking the text.
        var minimumWidth = _document.Rungs.Any(IsCounterInstruction) ? 1100.0f : 760.0f;
        if (ChallengeReadable)
        {
            var font = ThemeDB.FallbackFont;
            foreach (var rung in _document.Rungs)
            {
                minimumWidth = Math.Max(minimumWidth, font.GetStringSize(rung.CoilVariable, HorizontalAlignment.Left, -1, 14).X / 0.24f + 110);
                foreach (var branch in rung.Branches)
                {
                    var stride = branch.Contacts.Select(contact => font.GetStringSize(contact.Variable,
                        HorizontalAlignment.Left, -1, 14).X + 28).DefaultIfEmpty(140).Max();
                    minimumWidth = Math.Max(minimumWidth, (branch.Contacts.Count + 1) * Math.Max(140, stride) / 0.68f + 110);
                }
            }
        }
        minimumWidth = Math.Max(minimumWidth, _document.Rungs.Select(r => TreeLayout(r).Width + 620).DefaultIfEmpty(760).Max());
        CustomMinimumSize = ToCanvas(new Vector2(minimumWidth, Math.Max(500, FirstRungTop / ZoomFactor + TotalRungAdvance() + 30 / ZoomFactor)));
        SelectedRung = Math.Clamp(SelectedRung, 0, Math.Max(0, _document.Rungs.Count - 1));
        if (_document.Rungs.Count == 0)
        {
            ClearElementSelection();
        }
        else if (SelectedContact >= 0)
        {
            var rung = _document.Rungs[SelectedRung];
            if (SelectedBranch < 0 || SelectedBranch >= rung.Branches.Count
                || SelectedContact >= rung.Branches[SelectedBranch].Contacts.Count)
                ClearElementSelection();
        }
        else if (SelectedInsertionIndex >= 0)
        {
            var rung = _document.Rungs[SelectedRung];
            if (SelectedBranch < 0 || SelectedBranch >= rung.Branches.Count)
                ClearElementSelection();
            else
                SelectedInsertionIndex = Math.Clamp(
                    SelectedInsertionIndex,
                    0,
                    rung.Branches[SelectedBranch].Contacts.Count);
        }
        QueueRedraw();
    }

    public void SetMonitorSnapshot(VirtualControllerSnapshot? snapshot, ObservationUnavailableReason reason = ObservationUnavailableReason.None)
    {
        _monitorSnapshot = reason == ObservationUnavailableReason.None ? snapshot : null;
        _monitorUnavailableReason = reason;
        _monitorPublication = null;
        _pendingMonitorPublication = null;
        _presentationCache.Clear();
        if (_instructionWindow?.Visible == true) RefreshInstructionInspector();
        QueueRedraw();
    }

    public bool IsElementEnergized(string id) => ObservationIsEnergized(id);

    public Vector2 GetInsertionPointPosition(int rungIndex, int branchIndex, int insertionIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var rung = _document.Rungs[rungIndex];
        if (branchIndex < 0 || branchIndex >= rung.Branches.Count)
            throw new ArgumentOutOfRangeException(nameof(branchIndex));
        var contactCount = rung.Branches[branchIndex].Contacts.Count;
        if (insertionIndex < 0 || insertionIndex > contactCount)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));
        var branch = rung.Branches[branchIndex];
        var slot = TreeLayout(rung).Slots.FirstOrDefault(s => s.ParentId == branch.Id && s.ChildIndex == insertionIndex)
            ?? throw new InvalidOperationException("Nested insertion requires an exact tree slot.");
        return ToCanvas(TreeOrigin(rungIndex) + new Vector2(slot.X, slot.Y));
    }

    public Vector2 GetElementPosition(int rungIndex, int branchIndex, int contactIndex, bool output = false)
    {
        if (output) return ToCanvas(TreeOutputPosition(rungIndex));
        var rung = _document.Rungs[rungIndex];
        var id = rung.Branches[branchIndex].Contacts[contactIndex].Id;
        var placed = TreeLayout(rung).Nodes.First(n => n.Node.Id == id);
        return ToCanvas(TreeOrigin(rungIndex) + new Vector2(placed.Bounds.X + placed.Bounds.Width / 2, placed.ContinuityY));
    }

    public void SelectRung(int index)
    {
        SelectedRung = Math.Clamp(index, 0, Math.Max(0, _document.Rungs.Count - 1));
        ClearElementSelection();
        QueueRedraw();
    }

    public void SelectElement(int rungIndex, int branchIndex, int contactIndex, bool output = false, bool notify = true)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count) return;
        if (!output)
        {
            if (branchIndex < 0 || branchIndex >= _document.Rungs[rungIndex].Branches.Count) return;
            if (contactIndex < 0 || contactIndex >= _document.Rungs[rungIndex].Branches[branchIndex].Contacts.Count) return;
        }
        SelectedRung = rungIndex;
        SelectedBranch = output ? -1 : branchIndex;
        SelectedContact = output ? -1 : contactIndex;
        SelectedInsertionIndex = -1;
        OutputSelected = output;
        SelectedNodeId = output ? string.Empty : _document.Rungs[rungIndex].Branches[branchIndex].Contacts[contactIndex].Id;
        _selectedTreeSlot = null;
        if (notify)
        {
            RungSelected?.Invoke(rungIndex);
            ElementSelected?.Invoke(rungIndex, SelectedBranch, SelectedContact, output);
        }
        QueueRedraw();
    }

    public void SelectInsertionPoint(
        int rungIndex,
        int branchIndex,
        int insertionIndex,
        bool notify = true)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count) return;
        var rung = _document.Rungs[rungIndex];
        if (branchIndex < 0 || branchIndex >= rung.Branches.Count) return;
        if (insertionIndex < 0 || insertionIndex > rung.Branches[branchIndex].Contacts.Count) return;
        SelectedRung = rungIndex;
        SelectedBranch = branchIndex;
        SelectedContact = -1;
        SelectedInsertionIndex = insertionIndex;
        OutputSelected = false;
        SelectedNodeId = rung.Branches[branchIndex].Id;
        _selectedTreeSlot = TreeLayout(rung).Slots.FirstOrDefault(s => s.ParentId == SelectedNodeId && s.ChildIndex == insertionIndex);
        if (notify)
        {
            RungSelected?.Invoke(rungIndex);
            InsertionPointSelected?.Invoke(rungIndex, branchIndex, insertionIndex);
        }
        QueueRedraw();
    }

    public void ClearElementSelection()
    {
        SelectedBranch = -1;
        SelectedContact = -1;
        SelectedInsertionIndex = -1;
        OutputSelected = false;
        SelectedNodeId = string.Empty;
        _selectedTreeSlot = null;
    }

    public bool PreviewInsertionAt(Vector2 position)
    {
        position = ToLogical(position);
        var rungIndex = RungIndexAt(position.Y);
        if (rungIndex < 0) return false;
        if (!_document.Rungs[rungIndex].IsFlat)
        {
            var point = position - TreeOrigin(rungIndex); var slot = TreeLayout(_document.Rungs[rungIndex]).HitSlot(point.X, point.Y);
            if (slot is not null) { SelectTreeNode(rungIndex, slot.ParentId); _selectedTreeSlot = slot; QueueRedraw(); return true; }
        }
        if (TryHitElement(rungIndex, position, out var outputBranch, out var outputContact, out var output)
            && output)
        {
            SelectElement(rungIndex, outputBranch, outputContact, output, notify: false);
            return true;
        }
        if (!TryHitInsertionPoint(rungIndex, position, out var branchIndex, out var insertionIndex))
            return false;
        SelectInsertionPoint(rungIndex, branchIndex, insertionIndex, notify: false);
        return true;
    }

    public bool DropInstructionAt(string kind, Vector2 position)
    {
        position = ToLogical(position);
        if (MutationLocked) return false;
        var rungIndex = RungIndexAt(position.Y);
        if (rungIndex < 0) return false;
        if (!_document.Rungs[rungIndex].IsFlat)
        {
            var point = position - TreeOrigin(rungIndex); var slot = TreeLayout(_document.Rungs[rungIndex]).HitSlot(point.X, point.Y);
            if (slot is not null)
            {
                var isComparison = kind.StartsWith("compare-", StringComparison.Ordinal)
                    && int.TryParse(kind.AsSpan(8), out var comparisonValue) && Enum.IsDefined((LadderCompareOperator)comparisonValue);
                if (kind is not ("no" or "nc" or "edge-rising" or "edge-falling" or "branch") && !isComparison) return false;
                SelectTreeNode(rungIndex, slot.ParentId); _selectedTreeSlot = slot;
                var parent = EditableLadderTree.Find(_document.Rungs[rungIndex].Root, slot.ParentId)!;
                var template = isComparison ? NewComparisonTemplate((LadderCompareOperator)int.Parse(kind.AsSpan(8))) : NewContactTemplate() with { NormallyClosed = kind == "nc",
                    EdgeMode = kind == "edge-rising" ? LadderEdgeMode.Rising : kind == "edge-falling" ? LadderEdgeMode.Falling : LadderEdgeMode.None };
                EditableLadderNode payload = parent.Kind == LadderNodeKind.Parallel || kind == "branch"
                    ? new("clipboard-series", LadderNodeKind.Series, Children: kind == "branch" ? Array.Empty<EditableLadderNode>() : Array.AsReadOnly(new[] { template })) : template;
                TreeCommandRequested?.Invoke(new(LadderTreeCommandId.InsertAt, TreeTarget(), payload)); return true;
            }
        }
        if (TryHitElement(rungIndex, position, out var outputBranch, out var outputContact, out var output)
            && output)
        {
            SelectElement(rungIndex, outputBranch, outputContact, output);
            InstructionDropRequested?.Invoke(kind, rungIndex, outputBranch, -1);
            return true;
        }
        if (!TryHitInsertionPoint(rungIndex, position, out var branchIndex, out var insertionIndex))
            return false;
        SelectInsertionPoint(rungIndex, branchIndex, insertionIndex);
        InstructionDropRequested?.Invoke(kind, rungIndex, branchIndex, insertionIndex);
        return true;
    }

    public override void _GuiInput(InputEvent input)
    {
        // Consume edit keys before Control's directional focus navigation.
        if (input is InputEventKey)
        {
            _ShortcutInput(input);
            return;
        }
        if (input is not InputEventMouseButton { Pressed: true } click
            || click.ButtonIndex is not (MouseButton.Left or MouseButton.Right)) return;
        GrabFocus();
        var logicalPosition = ToLogical(click.Position);
        var index = RungIndexAt(logicalPosition.Y);
        if (index < 0 || index >= _document.Rungs.Count) return;
        if (TryHitElement(index, logicalPosition, out var branchIndex, out var contactIndex, out var output))
        {
            SelectElement(index, branchIndex, contactIndex, output);
            if (click.ButtonIndex == MouseButton.Right)
            {
                if (!output && !_document.Rungs[index].IsFlat) ShowTreePopup();
                else ContextMenuRequested?.Invoke(index, branchIndex, contactIndex, output);
            }
            else if (click.DoubleClick)
                PropertiesRequested?.Invoke(index, branchIndex, contactIndex, output);
            AcceptEvent();
            return;
        }
        if (TryHitInsertionPoint(index, logicalPosition, out branchIndex, out var insertionIndex))
        {
            SelectInsertionPoint(index, branchIndex, insertionIndex);
            if (click.ButtonIndex == MouseButton.Right)
            {
                if (_document.Rungs[index].LegacyEmptyBranchSemantics && TreeLayout(_document.Rungs[index]).Nodes.Any(n => n.Node.Id == SelectedNodeId && n.IgnoredLegacy)) ShowTreePopup();
                else ContextMenuRequested?.Invoke(index, branchIndex, -1, false);
            }
            AcceptEvent();
            return;
        }
        var treePoint = logicalPosition - TreeOrigin(index);
        var treeLayout = TreeLayout(_document.Rungs[index]);
        var treeSlot = treeLayout.HitSlot(treePoint.X, treePoint.Y);
        var treeNode = treeLayout.HitNode(treePoint.X, treePoint.Y);
        if (treeSlot is not null || treeNode is not null)
        {
            SelectTreeNode(index, treeSlot?.ParentId ?? treeNode!.Node.Id);
            _selectedTreeSlot = treeSlot;
            if (click.ButtonIndex == MouseButton.Right) ShowTreePopup();
            QueueRedraw(); AcceptEvent(); return;
        }
        SelectedRung = index;
        ClearElementSelection();
        RungSelected?.Invoke(index);
        if (click.ButtonIndex == MouseButton.Right)
            ContextMenuRequested?.Invoke(index, -1, -1, false);
        QueueRedraw();
        AcceptEvent();
    }

    public override void _ShortcutInput(InputEvent input)
    {
        if (!IsVisibleInTree() || !HasFocus()) return;
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        if ((key.Keycode == Key.Menu || key.ShiftPressed && key.Keycode == Key.F10) && _document.Rungs.Count > 0)
        {
            if (OutputSelected || string.IsNullOrEmpty(SelectedNodeId)) ContextMenuRequested?.Invoke(SelectedRung, SelectedBranch, SelectedContact, OutputSelected);
            else ShowTreePopup();
            AcceptEvent(); return;
        }
        if (key.AltPressed && key.Keycode is Key.Left or Key.Right && !string.IsNullOrEmpty(SelectedNodeId))
        {
            TreeCommandRequested?.Invoke(new(key.Keycode == Key.Left ? LadderTreeCommandId.MoveLeft : LadderTreeCommandId.MoveRight, TreeTarget()));
            AcceptEvent(); return;
        }
        if (!string.IsNullOrEmpty(SelectedNodeId) && (key.CtrlPressed && key.ShiftPressed && key.Keycode == Key.B
            || !_document.Rungs[SelectedRung].IsFlat && (key.Keycode is Key.Delete or Key.Backspace || key.CtrlPressed && key.Keycode is Key.C or Key.X or Key.V)))
        {
            var command = key.Keycode == Key.B ? LadderTreeCommandId.WrapParallel : key.Keycode == Key.C ? LadderTreeCommandId.Copy
                : key.Keycode == Key.X ? LadderTreeCommandId.Cut : key.Keycode == Key.V ? _selectedTreeSlot is null ? LadderTreeCommandId.PasteAfter : LadderTreeCommandId.PasteAt : LadderTreeCommandId.Delete;
            TreeCommandRequested?.Invoke(new(command, TreeTarget(), command is LadderTreeCommandId.PasteAfter or LadderTreeCommandId.PasteAt ? TreeClipboard : null));
            AcceptEvent(); return;
        }
        if (key.CtrlPressed && key.ShiftPressed && key.Keycode == Key.S) SaveAsRequested?.Invoke();
        else if (key.CtrlPressed && key.Keycode == Key.S) SaveDraftRequested?.Invoke();
        else if (key.CtrlPressed && key.Keycode == Key.Z) { if (!MutationLocked) UndoEditRequested?.Invoke(); }
        else if (key.CtrlPressed && key.Keycode == Key.Y) { if (!MutationLocked) RedoEditRequested?.Invoke(); }
        else if (key.Keycode == Key.F2) InlineOperandRequested?.Invoke();
        else if (key.CtrlPressed && key.Keycode == Key.X) { if (!MutationLocked) CutRequested?.Invoke(); }
        else if (key.CtrlPressed && key.Keycode == Key.C) CopyRequested?.Invoke();
        else if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter)
            PropertiesRequested?.Invoke(SelectedRung, SelectedBranch, SelectedContact, OutputSelected);
        else if (key.AltPressed && (key.Keycode == Key.Left || key.Keycode == Key.Right))
        {
            if (!MutationLocked) MoveRequested?.Invoke(key.Keycode == Key.Left ? -1 : 1);
        }
        else if (key.Keycode is Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End)
            NavigateKeyboard(key.Keycode, key.CtrlPressed);
        else if (key.CtrlPressed && key.Keycode == Key.V) { if (!MutationLocked) PasteRequested?.Invoke(); }
        else if (key.Keycode is Key.Delete or Key.Backspace) { if (!MutationLocked) DeleteRequested?.Invoke(); }
        else if (key.Keycode == Key.Insert)
        {
            if (!MutationLocked && !key.CtrlPressed && !string.IsNullOrEmpty(SelectedNodeId))
                TreeCommandRequested?.Invoke(new(key.ShiftPressed ? LadderTreeCommandId.WrapParallel
                    : _selectedTreeSlot is null ? LadderTreeCommandId.InsertAfter : LadderTreeCommandId.InsertAt,
                    TreeTarget(), key.ShiftPressed ? null : NewContactTemplate()));
            else if (!MutationLocked) KeyboardInstructionRequested?.Invoke(key.CtrlPressed ? "rung" : key.ShiftPressed ? "branch" : "no");
        }
        else return;
        KeyboardSelectionChanged?.Invoke();
        AcceptEvent();
    }

    private void NavigateKeyboard(Key key, bool control)
    {
        if (_document.Rungs.Count == 0) return;
        if (control && key is Key.Up or Key.Down)
        {
            SelectRung(Math.Clamp(SelectedRung + (key == Key.Up ? -1 : 1), 0, _document.Rungs.Count - 1));
            RungSelected?.Invoke(SelectedRung);
            return;
        }
        var rung = _document.Rungs[Math.Clamp(SelectedRung, 0, _document.Rungs.Count - 1)];
        if (!rung.IsFlat)
        {
            var layout = TreeLayout(rung); var ordered = layout.Navigation();
            var current = OutputSelected ? ordered.Count - 1 : ordered.ToList().FindIndex(n => n.NodeId == SelectedNodeId
                && (_selectedTreeSlot is null ? !n.IsInsertion : n.ParentId == _selectedTreeSlot.ParentId && n.Slot == _selectedTreeSlot.ChildIndex));
            var next = key == Key.Home ? 0 : key == Key.End ? ordered.Count - 1
                : Math.Clamp(current + (key is Key.Left or Key.Up ? -1 : 1), 0, ordered.Count - 1);
            var treeNavigationTarget = ordered[next];
            if (treeNavigationTarget.IsTerminal) SelectElement(SelectedRung, -1, -1, output: true);
            else
            {
                SelectTreeNode(SelectedRung, treeNavigationTarget.NodeId);
                if (treeNavigationTarget.IsInsertion) _selectedTreeSlot = layout.Slots.First(s => s.ParentId == treeNavigationTarget.ParentId && s.ChildIndex == treeNavigationTarget.Slot);
                QueueRedraw();
            }
            return;
        }
        if (rung.Branches.Count == 0)
        {
            if (key is Key.End or Key.Right) SelectElement(SelectedRung, -1, -1, true);
            else RungSelected?.Invoke(SelectedRung);
            return;
        }
        var branch = Math.Clamp(SelectedBranch, 0, rung.Branches.Count - 1);
        if (key is Key.Up or Key.Down)
        {
            var next = branch + (key == Key.Up ? -1 : 1);
            if (next < 0 || next >= rung.Branches.Count)
            {
                SelectRung(Math.Clamp(SelectedRung + (key == Key.Up ? -1 : 1), 0, _document.Rungs.Count - 1));
                RungSelected?.Invoke(SelectedRung);
            }
            else SelectInsertionPoint(SelectedRung, next, Math.Clamp(SelectedContact, 0, rung.Branches[next].Contacts.Count));
            return;
        }
        var count = rung.Branches[branch].Contacts.Count;
        // Alternating insertion slots and contacts, then the output. Empty branches remain editable.
        var direction = key switch
        {
            Key.Home => ChallengeHorizontalNavigation.Home, Key.End => ChallengeHorizontalNavigation.End,
            Key.Left => ChallengeHorizontalNavigation.Left, _ => ChallengeHorizontalNavigation.Right,
        };
        var target = ChallengePresenterModel.Navigate(count, new(SelectedContact, SelectedInsertionIndex, OutputSelected), direction);
        if (target.Output) SelectElement(SelectedRung, branch, -1, true);
        else if (target.Insertion >= 0) SelectInsertionPoint(SelectedRung, branch, target.Insertion);
        else SelectElement(SelectedRung, branch, target.Contact, false);
        TooltipText = OutputSelected ? $"Output: {rung.CoilVariable}"
            : SelectedContact >= 0 ? $"Contact: {rung.Branches[branch].Contacts[SelectedContact].Variable}"
            : $"Insertion: rung {SelectedRung + 1}, branch {branch + 1}, slot {SelectedInsertionIndex + 1}";
    }

    private bool TryHitElement(int rungIndex, Vector2 position, out int branchIndex, out int contactIndex, out bool output)
    {
        branchIndex = -1;
        contactIndex = -1;
        output = false;
        var rung = _document.Rungs[rungIndex];
        var outputPosition = TreeOutputPosition(rungIndex);
        var outputBounds = TerminalBounds(rungIndex);
        if (outputBounds.HasPoint(position)) { output = true; return true; }
        var local = position - TreeOrigin(rungIndex);
        var hit = TreeLayout(rung).HitNode(local.X, local.Y);
        return hit?.Node.IsLeaf == true && LeafCoordinates(rung, hit.Node.Id, out branchIndex, out contactIndex);
    }

    private bool TryHitInsertionPoint(int rungIndex, Vector2 position, out int branchIndex, out int insertionIndex)
    {
        branchIndex = insertionIndex = -1;
        var rung = _document.Rungs[rungIndex]; var local = position - TreeOrigin(rungIndex);
        var slot = TreeLayout(rung).HitSlot(local.X, local.Y);
        if (slot is null || !rung.IsFlat) return false;
        branchIndex = rung.Branches.FindIndex(b => b.Id == slot.ParentId);
        if (branchIndex < 0) return false;
        insertionIndex = slot.ChildIndex; return true;
    }

    public Rect2 GetRungBounds(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        return ToCanvas(new Rect2(0, RungTop(rungIndex), LogicalViewportSize.X, RungHeight(_document.Rungs[rungIndex])));
    }

    private float TotalRungAdvance()
    {
        var total = 0.0f;
        foreach (var rung in _document.Rungs) total += RungAdvance(rung);
        return total;
    }

    private float RungTop(int rungIndex)
    {
        // Child UI controls retain readable unscaled chrome; first rung stays below them.
        var top = FirstRungTop / ZoomFactor;
        for (var index = 0; index < rungIndex && index < _document.Rungs.Count; index++)
            top += RungAdvance(_document.Rungs[index]);
        return top;
    }

    private int RungIndexAt(float y)
    {
        for (var index = 0; index < _document.Rungs.Count; index++)
        {
            var top = RungTop(index);
            if (y >= top && y < top + RungAdvance(_document.Rungs[index])) return index;
        }
        return -1;
    }

    private static float BranchGap(EditableRung rung) => rung.Branches.Count > 1 ? MultiBranchGap : 0.0f;
    private static float RungHeight(EditableRung rung) =>
        Math.Max(250, TreeLayout(rung).Height + 110);
    private static float RungAdvance(EditableRung rung) =>
        RungHeight(rung) + 15;

    public override void _Draw()
    {
        BeginMonitoringDraw();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * ZoomFactor);
        DrawRect(new Rect2(Vector2.Zero, LogicalViewportSize), new Color("f5f6f7"));
        if (_style == LadderVendorStyle.SiemensTia) DrawTia();
        else DrawLogix();
        if (HasFocus()) DrawRect(new Rect2(Vector2.One, LogicalViewportSize - Vector2.One * 2), new Color("075985"), false, 3);
    }

    private void DrawTia()
    {
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawRect(new Rect2(0, 0, Size.X, 36), new Color("dfe4e7"));
        var blockName = _document.Blocks.Count > 0 ? _document.Blocks[_document.ActiveBlockIndex].Name : _document.Name;
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(14, 25), $"{blockName}  |  LAD", HorizontalAlignment.Left, -1, 13, new Color("263943"));
        DrawMonitorStatus();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * ZoomFactor);
        var drawY = FirstRungTop / ZoomFactor;
        for (var index = 0; index < _document.Rungs.Count; index++)
        {
            var y = drawY;
            var rungHeight = RungHeight(_document.Rungs[index]);
            drawY += rungHeight + 15;
            if (y + rungHeight < _visibleMonitorBounds.Position.Y) continue;
            if (y > _visibleMonitorBounds.End.Y) break;
            var selected = index == SelectedRung;
            DrawRect(new Rect2(22, y, LogicalViewportSize.X - 42, rungHeight), selected ? new Color("eef9fb") : new Color("ffffff"));
            DrawRect(new Rect2(22, y, LogicalViewportSize.X - 42, 30), selected ? new Color("ccecf2") : new Color("e9edef"));
            DrawRect(new Rect2(22, y, 5, rungHeight), selected ? new Color("0087a8") : new Color("aab6bc"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(36, y + 21), $"Network {index + 1}", HorizontalAlignment.Left, -1, 13, new Color("17242c"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(130, y + 21), _document.Rungs[index].Label, HorizontalAlignment.Left, -1, 12, new Color("5b6a72"));
            DrawRungLogic(index, _document.Rungs[index], 48, y + 102, LogicalViewportSize.X - 94, new Color("203844"));
        }
    }

    private void DrawLogix()
    {
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawRect(new Rect2(0, 0, Size.X, 36), new Color("d5d8dd"));
        var blockName = _document.Blocks.Count > 0 ? _document.Blocks[_document.ActiveBlockIndex].Name : _document.Name;
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(14, 25), $"MainProgram — {blockName}  [LAD]", HorizontalAlignment.Left, -1, 13, new Color("20252b"));
        DrawMonitorStatus();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * ZoomFactor);
        DrawRect(new Rect2(0, 36, 44, LogicalViewportSize.Y - 36), new Color("d9dce1"));
        var drawY = FirstRungTop / ZoomFactor;
        for (var index = 0; index < _document.Rungs.Count; index++)
        {
            var y = drawY;
            var rungHeight = RungHeight(_document.Rungs[index]);
            drawY += rungHeight + 15;
            if (y + rungHeight < _visibleMonitorBounds.Position.Y) continue;
            if (y > _visibleMonitorBounds.End.Y) break;
            var selected = index == SelectedRung;
            DrawRect(new Rect2(44, y, LogicalViewportSize.X - 44, rungHeight), selected ? new Color("f1f4fb") : new Color("ffffff"));
            DrawRect(new Rect2(44, y, LogicalViewportSize.X - 44, 32), selected ? new Color("dce5f7") : new Color("eceef1"));
            DrawRect(new Rect2(40, y, 4, rungHeight), selected ? new Color("335ea8") : new Color("808996"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(14, y + 22), index.ToString(), HorizontalAlignment.Left, -1, 13, new Color("26344a"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(56, y + 22), _document.Rungs[index].Label, HorizontalAlignment.Left, -1, 12, new Color("4b5664"));
            DrawRungLogic(index, _document.Rungs[index], 62, y + 102, LogicalViewportSize.X - 84, new Color("242d69"));
        }
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(8, FirstRungTop / ZoomFactor + TotalRungAdvance() + 13), "(End)", HorizontalAlignment.Left, -1, 12, new Color("304f9e"));
    }

    private void DrawRungLogic(int rungIndex, EditableRung rung, float left, float y, float span, Color wire)
    {
        var live = _style == LadderVendorStyle.SiemensTia ? new Color("00a651") : new Color("27a745");

        var right = left + span;
        var outputX = left + span * 0.86f;
        var outputGap = 160.0f;
        var outputId = OutputElementId(rung);
        var outputPowered = IsElementEnergized(outputId);
        var measured = TreeLayout(rung);
        var treeOrigin = new Vector2(left + 16, y - 42);
        y = treeOrigin.Y + measured.Height / 2;
        outputX = treeOrigin.X + measured.Width + 190;
        right = outputX + outputGap + 80;
        DrawRecursiveConditions(rungIndex, rung, measured, treeOrigin, wire, live);
        DrawConductor(left, treeOrigin.X, y, wire, 2);
        DrawConductor(treeOrigin.X + measured.Width, outputX - outputGap, y, wire, 2);
        DrawConductor(outputX + outputGap, right, y, wire, 2);
        DrawLine(new Vector2(left, y - 34), new Vector2(left, y + 34), wire, 3);
        DrawLine(new Vector2(right, y - 34), new Vector2(right, y + 34), wire, 3);
        var outputColor = outputPowered ? live : wire;
        if (!rung.HasTerminalAction)
        {
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(outputX - 80, y), "DRAFT · NO TERMINAL", HorizontalAlignment.Center, 160, 11, outputColor);
            return;
        }
        if (outputPowered)
            DrawLine(new Vector2(right, y - 34), new Vector2(right, y + 34), live, 4);
        DrawTerminalObservation(rungIndex, rung);
    }

    private void DrawMonitorStatus()
    {
        var text = MonitorStatusText;
        var color = _monitorSnapshot is null
            ? new Color("6a7d86")
            : _monitorSnapshot.State == VirtualControllerState.Running
                ? new Color("18864b")
                : new Color("b36c16");
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(Math.Max(360, Size.X - 255), 25), text,
            HorizontalAlignment.Right, 240, 11, color);
    }

    private static string OutputElementId(EditableRung rung) => !string.IsNullOrEmpty(rung.TerminalActionId) ? rung.TerminalActionId : rung.IsTimer
        ? $"{rung.Id}-timer"
        : rung.IsTimerReset
            ? $"{rung.Id}-timer-reset"
        : rung.IsCounter
            ? $"{rung.Id}-counter"
            : rung.IsCounterReset
                ? $"{rung.Id}-counter-reset"
                : rung.IsCounterLoad
                    ? $"{rung.Id}-counter-load"
                    : rung.IsNumericOperation
                        ? $"{rung.Id}-numeric"
                        : rung.IsCall
                            ? $"{rung.Id}-call"
                            : rung.IsReturn
                                ? $"{rung.Id}-return"
                                : rung.IsJump
                                    ? $"{rung.Id}-jump"
                                    : rung.IsLabel
                                        ? $"{rung.Id}-label"
                                : $"{rung.Id}-coil";

    private static float InsertionSlotX(float left, float usable, int contactCount, int insertionIndex) =>
        left + 36 + usable * (insertionIndex + 0.5f) / (contactCount + 1.0f);

    private static bool IsCounterInstruction(EditableRung rung) => rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad;

    /// <summary>Read-only presentation of the accepted scan, shared by both editor styles.
    /// The shell only supplies a snapshot when the current draft matches the loaded program.
    /// Never use rung power as done status: a counter retains its count between pulses.</summary>
    public string[] CounterReadoutLines(EditableRung rung)
    {
        var view = InstructionPresentationFor(OutputElementId(rung), TerminalCatalogKey(rung));
        var observation = _monitorPublication?.Observations?.Find(OutputElementId(rung)).Observation
            ?? _monitorSnapshot?.Observations?.Find(OutputElementId(rung)).Observation;
        if (_selectedObservationContext is not null)
            observation = (_monitorPublication?.Observations ?? _monitorSnapshot?.Observations)?.ContextsForInstruction(OutputElementId(rung))
                .Where(o => LadderInstructionPresentation.ContextKey(o) == _selectedObservationContext)
                .OrderByDescending(o => o.ExecutionOrdinal).FirstOrDefault();
        var state = view.UnavailableReason == ObservationUnavailableReason.None ? observation?.StoredAfter as LadderCounterState : null;
        var instruction = rung.IsCounterReset ? "RES" : rung.IsCounterLoad ? "LOAD" : rung.CounterKind == LadderCounterKind.CountDown ? "CTD" : "CTU";
        var preset = observation?.ConfiguredPreset is { } configured ? LadderInstructionPresentation.Value(configured)
            : rung.IsCounterReset ? "—" : rung.CounterPreset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new[]
        {
            instruction + (state is null ? " · NOT MONITORED" : ""), rung.CounterVariable,
            "Preset / setpoint: " + preset,
            "Current count: " + (state?.Accumulated.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"),
            "Count input: " + (observation?.CurrentInput is { } input && state is not null ? input ? "TRUE" : "FALSE" : "—"),
            "Done: " + (state is null ? "—" : state.Done ? "TRUE" : "FALSE"),
        };
    }
    private static float OutputConductorHalfGap(EditableRung rung) =>
        IsCounterInstruction(rung) ? 130.0f
        : rung.IsNumericOperation || rung.IsCall || rung.IsReturn || rung.IsJump || rung.IsLabel ? 66.0f
        : rung.IsTimer || rung.IsTimerReset || rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad ? 58.0f
        : 32.0f;

    private void DrawConductor(float startX, float endX, float y, Color color, float width)
    {
        if (endX <= startX) return;
        DrawLine(new Vector2(startX, y), new Vector2(endX, y), color, width);
    }

    private void DrawContactOperand(float x, float y, string operand, float width)
    {
        width = Math.Max(1, width);
        var font = ThemeDB.FallbackFont;
        if (ChallengeReadable)
        {
            // RefreshDocument reserves each full operand at 14 logical units.
            DrawEditorString(font, new Vector2(x - width / 2, y - 27), operand,
                HorizontalAlignment.Center, width, 14, new Color("17242c"));
            return;
        }
        string[] lines = [operand];
        if (font.GetStringSize(operand, HorizontalAlignment.Left, -1, 12).X > width)
        {
            // Keep member identity intact: clipping "temperature_valid" to
            // "temperature" would display a different declared symbol.
            var separator = operand.LastIndexOf('.');
            if (separator > 0 && separator < operand.Length - 1)
                lines = [operand[..(separator + 1)], operand[(separator + 1)..]];
            // At narrow contact strides a member alone may exceed the cell.
            // Prefer two complete balanced lines at a readable 9pt minimum.
            if (lines.Length == 1 || lines.Any(line => font.GetStringSize(line, HorizontalAlignment.Left, -1, 9).X > width))
            {
                var split = Enumerable.Range(1, Math.Max(0, operand.Length - 1))
                    .OrderBy(index => Math.Max(font.GetStringSize(operand[..index], HorizontalAlignment.Left, -1, 9).X,
                        font.GetStringSize(operand[index..], HorizontalAlignment.Left, -1, 9).X)).FirstOrDefault();
                if (split > 0) lines = [operand[..split], operand[split..]];
            }
        }
        for (var index = 0; index < lines.Length; index++)
        {
            var fontSize = 12;
            while (fontSize > 9 && font.GetStringSize(lines[index], HorizontalAlignment.Left, -1, fontSize).X > width)
                fontSize--;
            var text = lines[index];
            if (font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X > width)
            {
                // Explicit omission is preferable to displaying a clipped,
                // different symbol or silently shrinking it to one pixel.
                while (text.Length > 0 && font.GetStringSize(text + "…", HorizontalAlignment.Left, -1, fontSize).X > width)
                    text = text[..^1];
                text += "…";
            }
            DrawEditorString(font, new Vector2(x - width / 2, y - 27 - (lines.Length - 1 - index) * 14), text,
                HorizontalAlignment.Center, width, fontSize, new Color("17242c"));
        }
    }

    private void DrawContact(float x, float y, bool normallyClosed, Color color)
    {
        DrawLine(new Vector2(x - 13, y - 18), new Vector2(x - 13, y + 18), color, 3);
        DrawLine(new Vector2(x + 13, y - 18), new Vector2(x + 13, y + 18), color, 3);
        if (normallyClosed) DrawLine(new Vector2(x - 18, y + 22), new Vector2(x + 18, y - 22), color, 2);
    }

    private void DrawCoil(float x, float y, Color color, string marker)
    {
        // IEC/TIA coil notation is two open parentheses, not one closed oval.
        // The left arc bulges left and the right arc bulges right, leaving a
        // visible gap for the assignment/set/reset marker.
        DrawArc(new Vector2(x - 9, y), 18, MathF.PI / 2, MathF.PI * 1.5f, 18, color, 3);
        DrawArc(new Vector2(x + 9, y), 18, -MathF.PI / 2, MathF.PI / 2, 18, color, 3);
        if (!string.IsNullOrEmpty(marker))
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 12, y + 6), marker,
                HorizontalAlignment.Center, 24, 13, color);
    }
    private void DrawEditorString(Font font, Vector2 position, string text, HorizontalAlignment alignment,
        float width, int fontSize, Color color)
    {
        DrawString(font, position, text, alignment, width, ChallengeReadable ? Math.Max(14, fontSize) : fontSize, color);
    }

}







