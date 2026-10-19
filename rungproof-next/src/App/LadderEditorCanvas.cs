using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public enum LadderVendorStyle { SiemensTia, RockwellLogix }
public enum TiaOperandRepresentation { Symbolic, Absolute, SymbolicAndAbsolute }
public enum TiaTagInformationDisplay { Hidden, Show, HierarchicalComments }
public enum TiaTagInformationPosition { OnOperands, BelowNetwork }
internal enum TiaPowerFlowStatus { Satisfied, NotSatisfied, UnknownOrNotExecuted, NotInterconnected }
internal sealed record TiaMonitorPinProjection(
    IReadOnlyList<(string Pin, string Name, string Value)> Inputs,
    IReadOnlyList<(string Pin, string Name, string Value)> Outputs);

/// <summary>Graphical editor surface for a simulator-owned Ladder document.</summary>
public partial class LadderEditorCanvas : Control
{
    private const float FirstRungTop = 90.0f;
    private const float BaseRungHeight = 205.0f;
    private const float BaseRungAdvance = 220.0f;
    private const float MultiBranchGap = 92.0f;
    // The conductor must terminate on the contact bars. The earlier 20 px
    // half-gap left a visible break before bars drawn at x +/- 13. Keep the
    // wider legacy spacing for the Logix skin, but use the actual TIA bar
    // position for Siemens LAD.
    private float ContactConductorHalfGap => _style == LadderVendorStyle.SiemensTia ? 13.0f : 20.0f;
    private readonly LadderVendorStyle _style;
    private readonly LadderEditorDocument _document;
    public TiaOperandRepresentation OperandRepresentation { get; private set; } = TiaOperandRepresentation.Symbolic;
    public TiaTagInformationDisplay TagInformationDisplay { get; private set; } = TiaTagInformationDisplay.Hidden;
    public TiaTagInformationPosition TagInformationPosition { get; private set; } = TiaTagInformationPosition.OnOperands;
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
    public bool NetworkLabelSelected { get; private set; }
    public bool NetworkSelected { get; private set; }
    public bool MonitorActive => _monitorSnapshot is not null;
    public long MonitorScanNumber => _monitorSnapshot?.ScanNumber ?? 0;
    public event Action<int>? RungSelected;
    public event Action<int, int, int, bool>? ElementSelected;
    public event Action<int, int, int>? InsertionPointSelected;
    public event Action<int, int, int, bool>? PropertiesRequested;
    public event Action<int, int, int, bool>? ContextMenuRequested;
    public event Action<int>? EmptyInstructionChooserRequested;
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
    public event Action<string?, bool, string?>? InlineOperandRequested;
    public event Action? SaveAsRequested;
    public event Action? CutRequested;
    public event Action? CopyRequested;
    public event Action? PasteRequested;
    public event Action? DeleteRequested;
    public event Action<int>? MoveRequested;
    public event Action<string, int, int, int>? InstructionDropRequested;
    public event Action<int, int>? BlockCallDropRequested;
    public event Action<string, string>? TagOperandDropRequested;
    internal int ReviewBlockDropChecks { get; private set; }
    internal int ReviewBlockDrops { get; private set; }
    internal string ReviewBlockDropRejection { get; private set; } = string.Empty;
    internal int ReviewTagDropChecks { get; private set; }
    internal string ReviewTagDropRejection { get; private set; } = string.Empty;
    public event Action<LadderTreeEditorRequest>? TreeCommandRequested;
    public EditableLadderNode? TreeClipboard { get; set; }
    public string SelectedNodeId { get; private set; } = string.Empty;
    private LadderTreeSlot? _selectedTreeSlot;
    private PopupMenu? _treePopup;
    private LadderTreeEditorTarget? _treePopupTarget;
    private sealed record BranchCloseDrag(int RungIndex, string BranchPathId, string MainPathId,
        Vector2 Start, Vector2 Current, int CandidateSlot = -1);
    private BranchCloseDrag? _branchCloseDrag;

    private sealed class TreeLayoutCache { public EditableLadderNode? Root; public EditableLadderLayout? Layout; public bool Legacy; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EditableRung, TreeLayoutCache> TreeLayouts = new();
    private static EditableLadderLayout TreeLayout(EditableRung rung)
    {
        var cache = TreeLayouts.GetOrCreateValue(rung);
        if (!ReferenceEquals(cache.Root, rung.Root) || cache.Layout is null || cache.Legacy != rung.LegacyEmptyBranchSemantics)
        {
            cache.Layout = EditableLadderLayout.Measure(rung.Root,
                contactWidth: Math.Max(240, EditableLadderTree.Visit(rung.Root).Where(n => n.IsLeaf)
                    .Select(n => Math.Max(n.Kind == LadderNodeKind.Compare ? n.Variable.Length + n.RightOperand.Length + 3 : n.Variable.Length,
                        n.EdgeMemoryOperand?.Length ?? 0) * 7 + 36).DefaultIfEmpty(144).Max()),
                leafHeight: 136, legacyEmptyBranchSemantics: rung.LegacyEmptyBranchSemantics);
            cache.Root = rung.Root;
            cache.Legacy = rung.LegacyEmptyBranchSemantics;
        }
        return cache.Layout;
    }
    private float TreeLeft(EditableRung rung) => _style == LadderVendorStyle.SiemensTia
        ? rung.Root.Kind == LadderNodeKind.Parallel ? 48 : 64
        : 78;
    private Vector2 TreeOrigin(int rungIndex) => new(TreeLeft(_document.Rungs[rungIndex]),
        RungTop(rungIndex) + (_style == LadderVendorStyle.SiemensTia ? 132 : 60));
    private static bool IsBitCoil(EditableRung rung) => TerminalCatalogKey(rung) is "coil" or "set" or "reset";
    // Boxed TIA instructions sit directly on the rung conductor. For an empty
    // instruction after parallel branches, preserve a visible straight lead
    // from the branch merge to the box input; matching only the body's half
    // width made the box appear attached to the merge.
    private float TerminalLead(EditableRung rung) => _style == LadderVendorStyle.SiemensTia
        // Leave 80 logical px of conductor after the branch merge before the
        // empty box's IN pin. A shorter lead became difficult to distinguish
        // from the junction in the user's reduced-scale close-up.
        ? rung.HasEmptyInstructionPlaceholder ? 143.0f
            : rung.IsNumericOperation && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind) ? 300.0f
            : rung.IsCall ? CallTerminalHalfWidth(rung) + 40.0f : 106.0f
        : IsBitCoil(rung) ? 106.0f : 190.0f;
    private float TerminalGap(EditableRung rung) => _style == LadderVendorStyle.SiemensTia
        ? rung.HasEmptyInstructionPlaceholder ? 63.0f
            : rung.IsTimer || rung.IsCounter ? 63.0f
            // The outer rung conductor must stop at the actual instruction
            // body edge. EXPT and CONVERT are wider than the standard 132 px
            // numeric body, so the generic 66 px half-gap would paint the wire
            // 10/14 px inside those boxes.
            : rung.IsNumericOperation && rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate ? 76.0f
            : rung.IsNumericOperation && rung.NumericOperationKind == LadderNumericOperationKind.Convert ? 80.0f
            : rung.IsCall ? CallTerminalHalfWidth(rung) : 66.0f
        : IsBitCoil(rung) ? 66.0f : 78.0f;
    private float TerminalOutputTail(EditableRung rung) =>
        _style == LadderVendorStyle.SiemensTia && (rung.IsCounter || rung.IsTimer) ? 112.0f
        : _style == LadderVendorStyle.SiemensTia && rung.IsCall ? 220.0f
        : _style == LadderVendorStyle.SiemensTia && rung.IsNumericOperation && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind) ? 210.0f
        : 80.0f;
    private float TerminalOutputValueWidth(EditableRung rung) =>
        _style == LadderVendorStyle.SiemensTia && (rung.IsCounter || rung.IsTimer) ? 96.0f : TerminalGap(rung) - 2.0f;
    internal Vector2 GetTerminalConductorClearance(int rungIndex)
    {
        var rung = _document.Rungs[rungIndex];
        var centerX = TreeOutputPosition(rungIndex).X;
        var bounds = TerminalBounds(rungIndex);
        var halfGap = TerminalGap(rung);
        // Zero means the rung wire meets the instruction body. Positive values
        // mean the wire endpoint is inside the body (overlap); negative values
        // mean an open gap remains before the body.
        return new(centerX - halfGap - bounds.Position.X, bounds.End.X - (centerX + halfGap));
    }
    internal float GetTerminalInputLeadLength(int rungIndex)
    {
        var rung = _document.Rungs[rungIndex];
        if (_style != LadderVendorStyle.SiemensTia || !rung.HasEmptyInstructionPlaceholder) return 0;
        var branchMergeX = TreeOrigin(rungIndex).X + TreeLayout(rung).Width;
        return TerminalBounds(rungIndex).Position.X - branchMergeX;
    }
    private Vector2 TreeOutputPosition(int rungIndex)
    {
        var layout = TreeLayout(_document.Rungs[rungIndex]); var origin = TreeOrigin(rungIndex);
        var rung = _document.Rungs[rungIndex];
        var outputY = layout.Height / 2;
        if (_style == LadderVendorStyle.SiemensTia && rung.HasEmptyInstructionPlaceholder
            && rung.Root.Kind == LadderNodeKind.Parallel && rung.Root.Children is { Count: > 0 } children)
        {
            // TIA places an instruction after a parallel section on the upper
            // path. The lower path rises into the shared junction before IN.
            outputY = layout.Nodes.First(node => node.Node.Id == children[0].Id).ContinuityY;
        }
        return new(origin.X + layout.Width + TerminalLead(rung), origin.Y + outputY);
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
        else
        {
            SelectedRung = rungIndex;
            ClearElementSelection();
            SelectedNodeId = nodeId;
            if (notify) RungSelected?.Invoke(rungIndex);
            QueueRedraw();
            RefreshTiaCounterTypeSelector();
            RefreshTiaComparisonTypeSelector();
            RefreshTiaNumericTypeSelector();
        }
    }
    public bool SelectTreeInsertionPoint(int rungIndex, string parentId, int childIndex, bool notify = true)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count) return false;
        var slot = TreeLayout(_document.Rungs[rungIndex]).Slots
            .FirstOrDefault(item => item.ParentId == parentId && item.ChildIndex == childIndex);
        if (slot is null) return false;
        SelectedRung = rungIndex;
        SelectedBranch = -1;
        SelectedContact = -1;
        SelectedInsertionIndex = -1;
        OutputSelected = false;
        NetworkLabelSelected = false;
        NetworkSelected = false;
        SelectedNodeId = parentId;
        _selectedTreeSlot = slot;
        if (notify) RungSelected?.Invoke(rungIndex);
        QueueRedraw();
        RefreshTiaCounterTypeSelector();
        RefreshTiaComparisonTypeSelector();
        RefreshTiaNumericTypeSelector();
        return true;
    }
    private LadderTreeEditorTarget TreeTarget() => new(_document.Revision,
        _document.Blocks[_document.ActiveBlockIndex].Id, _document.Rungs[SelectedRung].Id,
        string.IsNullOrEmpty(SelectedNodeId) ? _document.Rungs[SelectedRung].Root.Id : SelectedNodeId,
        _selectedTreeSlot?.ParentId ?? "", _selectedTreeSlot?.ChildIndex ?? -1);
    public LadderTreeEditorTarget CaptureTreeTarget() => TreeTarget();
    public Vector2 GetSelectionPosition()
    {
        if (_document.Rungs.Count == 0) return Vector2.Zero;
        if (OutputSelected)
        {
            var rung = _document.Rungs[SelectedRung];
            if (NetworkLabelSelected && rung.IsLabel)
                return ToCanvas(LabelBounds(SelectedRung).GetCenter());
            return ToCanvas(TreeOutputPosition(SelectedRung));
        }
        if (_selectedTreeSlot is not null) return ToCanvas(TreeOrigin(SelectedRung) + new Vector2(_selectedTreeSlot.X, _selectedTreeSlot.Y));
        var node = TreeLayout(_document.Rungs[SelectedRung]).Nodes.FirstOrDefault(n => n.Node.Id == SelectedNodeId);
        return node is null ? GetRungBounds(SelectedRung).Position
            : ToCanvas(TreeOrigin(SelectedRung) + new Vector2(node.Bounds.X + node.Bounds.Width / 2, node.ContinuityY));
    }

    /// <summary>Screen-space canvas bounds for the selected TIA operand text field.</summary>
    public Rect2 GetOperandEditorBounds(string operand = "")
    {
        if (_document.Rungs.Count == 0) return new Rect2(Vector2.Zero, new Vector2(116, 22));
        var rung = _document.Rungs[SelectedRung];
        var font = ThemeDB.FallbackFont;
        float Width(float minimum) => Math.Clamp(font.GetStringSize(operand, HorizontalAlignment.Left, -1, 12).X + 12, minimum, 250);
        if (OutputSelected)
        {
            var terminal = TerminalBounds(SelectedRung);
            var center = TreeOutputPosition(SelectedRung);
            if (NetworkLabelSelected && rung.IsLabel)
            {
                var width = Width(116);
                var label = LabelBounds(SelectedRung);
                return ToCanvas(new Rect2(label.Position.X, label.Position.Y - 29, width, 22));
            }
            if (rung.IsNumericOperation && _style == LadderVendorStyle.SiemensTia
                && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind))
            {
                var outputRow = center.Y + (TiaNumericInstructionPorts.UsesLowerDataRow(rung.NumericOperationKind) ? 40 : 12);
                var width = Width(166);
                return ToCanvas(new Rect2(terminal.End.X + 8, outputRow - 30, width, 22));
            }
            if (rung.IsTimer || rung.IsCounter || rung.IsTimerReset || rung.IsCounterReset || rung.IsCounterLoad)
            {
                var width = Width(140);
                return ToCanvas(new Rect2(terminal.Position.X + terminal.Size.X / 2 - width / 2, terminal.Position.Y - 51, width, 22));
            }
            var coilWidth = Width(156);
            return ToCanvas(new Rect2(center.X - coilWidth / 2, center.Y - 55, coilWidth, 22));
        }

        var placed = TreeLayout(rung).Nodes.FirstOrDefault(node => node.Node.Id == SelectedNodeId);
        if (placed is null) return new Rect2(GetSelectionPosition() - new Vector2(58, 44), new Vector2(116, 22));
        var origin = TreeOrigin(SelectedRung);
        var centerX = origin.X + placed.Bounds.X + placed.Bounds.Width / 2;
        var centerY = origin.Y + placed.ContinuityY;
        if (placed.Node.Kind == LadderNodeKind.Compare)
        {
            var width = Width(116);
            return ToCanvas(new Rect2(centerX - width / 2, centerY - 66, width, 22));
        }
        var contactWidth = Width(116);
        return ToCanvas(new Rect2(centerX - contactWidth / 2, centerY - 45, contactWidth, 22));
    }

    /// <summary>Screen-space inline field for the specific operand pin selected in TIA mode.</summary>
    public Rect2 GetOperandEditorBounds(string operand, string target)
    {
        if (_style != LadderVendorStyle.SiemensTia || string.IsNullOrWhiteSpace(target))
            return GetOperandEditorBounds(operand);
        if (_document.Rungs.Count == 0) return new Rect2(Vector2.Zero, new Vector2(116, 22));
        var rung = _document.Rungs[SelectedRung];
        var font = ThemeDB.FallbackFont;
        var width = Math.Clamp(font.GetStringSize(operand, HorizontalAlignment.Left, -1, 12).X + 12, 116, 220);
        if (target == "compare-right" && !string.IsNullOrEmpty(SelectedNodeId))
        {
            var node = TreeLayout(rung).Nodes.FirstOrDefault(item => item.Node.Id == SelectedNodeId);
            if (node is not null)
            {
                var origin = TreeOrigin(SelectedRung);
                var center = new Vector2(origin.X + node.Bounds.X + node.Bounds.Width / 2, origin.Y + node.ContinuityY);
                return ToCanvas(new Rect2(center.X - width / 2, center.Y + 33, width, 22));
            }
        }
        if (TryTerminalInputPinY(SelectedRung, rung, target, out var pinY))
        {
            var body = TerminalBounds(SelectedRung);
            return ToCanvas(new Rect2(body.Position.X - width - 10, pinY - 13, width, 22));
        }
        return GetOperandEditorBounds(operand);
    }

    private bool TryTerminalInputPinY(int rungIndex, EditableRung rung, string target, out float pinY)
    {
        pinY = 0;
        var centerY = TreeOutputPosition(rungIndex).Y;
        if (rung.IsCounter)
        {
            var row = target switch
            {
                "terminal-counter-r" when rung.CounterKind == LadderCounterKind.CountUp => 1,
                "terminal-counter-load" when rung.CounterKind == LadderCounterKind.CountDown => 1,
                "terminal-counter-pv" => 2,
                _ => -1,
            };
            if (row < 0) return false;
            pinY = centerY + row * 26.0f;
            return true;
        }
        if (rung.IsTimer)
        {
            var resetRow = rung.TimerKind == LadderTimerKind.RetentiveOnDelay;
            if (target == "terminal-timer-reset" && resetRow) { pinY = centerY + 40.0f; return true; }
            if (target == "terminal-timer-pt")
            {
                pinY = centerY + (resetRow ? 80.0f : 24.0f);
                return true;
            }
            return false;
        }
        if (rung.IsNumericOperation && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind))
        {
            var row = target switch
            {
                "terminal-numeric-a" => 0,
                "terminal-numeric-b" => 1,
                "terminal-numeric-c" => 2,
                _ => -1,
            };
            if (row < 0 || row >= TiaNumericInstructionPorts.Inputs(rung.NumericOperationKind).Count) return false;
            pinY = centerY + (TiaNumericInstructionPorts.UsesLowerDataRow(rung.NumericOperationKind) ? 40.0f : 12.0f)
                + row * TiaNumericInstructionPorts.InputRowPitch(rung.NumericOperationKind);
            return true;
        }
        return false;
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
        var selected = EditableLadderTree.Find(_document.Rungs[SelectedRung].Root, SelectedNodeId);
        var parent = EditableLadderTree.Visit(_document.Rungs[SelectedRung].Root)
            .FirstOrDefault(node => node.Children?.Any(child => child.Id == selected?.Id) == true);
        var deleteLabel = _style != LadderVendorStyle.SiemensTia ? "Remove selected subtree"
            : parent?.Kind == LadderNodeKind.Parallel ? "Delete branch" : "Delete LAD element";
        _treePopup.AddItem(deleteLabel, (int)LadderTreeCommandId.Delete);
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
    internal IReadOnlyList<string> ReviewTreePopupItems() => _treePopup is null
        ? [] : Enumerable.Range(0, _treePopup.ItemCount).Select(_treePopup.GetItemText).ToArray();
    internal bool ReviewActivateTreePopup(LadderTreeCommandId command)
    {
        if (_treePopup is null || Enumerable.Range(0, _treePopup.ItemCount)
            .All(index => _treePopup.GetItemId(index) != (int)command)) return false;
        _treePopup.EmitSignal(PopupMenu.SignalName.IdPressed, (long)(int)command);
        _treePopup.Hide();
        return true;
    }

    private void DrawRecursiveConditions(int rungIndex, EditableRung rung, EditableLadderLayout layout, Vector2 origin, Color wire, Color live)
    {
        var monitoring = IsTiaMonitoringActive;
        var power = ProjectLadderPowerFlow(layout);
        foreach (var line in layout.Wires)
            if (MonitorBoundsVisible(new Rect2(origin + new Vector2(Math.Min(line.X1,line.X2),Math.Min(line.Y1,line.Y2)), new Vector2(Math.Max(1,Math.Abs(line.X2-line.X1)),Math.Max(1,Math.Abs(line.Y2-line.Y1))))))
            {
                power.TryGetValue(line.FlowNodeId, out var flow);
                var state = line.FlowSide switch
                {
                    LadderTreeWireFlowSide.Input => flow?.Input,
                    LadderTreeWireFlowSide.Output => flow?.Output,
                    _ => (bool?)null
                };
                DrawPowerFlowLine(origin + new Vector2(line.X1, line.Y1), origin + new Vector2(line.X2, line.Y2), wire, state, monitoring, 2);
            }
        foreach (var placed in layout.Nodes)
        {
            var node = placed.Node;
            if (!MonitorBoundsVisible(new Rect2(origin + new Vector2(placed.Bounds.X, placed.Bounds.Y), new Vector2(placed.Bounds.Width, placed.Bounds.Height)))) continue;
            if (node.IsLeaf) { if (LastDrawnInstructionCount >= 200) continue; LastDrawnInstructionCount++; }
            var x = origin.X + placed.Bounds.X + placed.Bounds.Width / 2;
            var y = origin.Y + placed.ContinuityY;
            var tiaComparison = _style == LadderVendorStyle.SiemensTia
                && node.Kind == LadderNodeKind.Compare && node.CompareOperator != LadderCompareOperator.ContainsCode;
            if (node.IsLeaf)
            {
                power.TryGetValue(node.Id, out var flow);
                var color = PowerFlowColor(flow?.Output, wire, monitoring);
                var comparisonWidth = node.Kind == LadderNodeKind.Compare ? tiaComparison ? 68 : 84 : 0;
                var gap = node.Kind == LadderNodeKind.Compare ? comparisonWidth / 2 : ContactConductorHalfGap;
                DrawPowerFlowLine(new Vector2(origin.X + placed.Bounds.X, y), new Vector2(x - gap, y), wire, flow?.Input, monitoring, 2);
                DrawPowerFlowLine(new Vector2(x + gap, y), new Vector2(origin.X + placed.Bounds.X + placed.Bounds.Width, y), wire, flow?.Output, monitoring, 2);
                if (node.Kind == LadderNodeKind.Compare)
                {
                    var compareView = InstructionPresentationFor(node.Id, NodeCatalogKey(node));
                    var first = compareView.Rows.FirstOrDefault(r => r.Label == "Consumed A");
                    var second = compareView.Rows.FirstOrDefault(r => r.Label == "Consumed B");
                    var firstValue = tiaComparison ? ComparisonValue(first) : first?.Value ?? "";
                    var secondValue = tiaComparison ? ComparisonValue(second) : second?.Value ?? "";
                    if (tiaComparison)
                    {
                        DrawExternalTag(node.Variable, firstValue, x, y - 44,
                            PowerFlowColor(flow?.Input, wire, monitoring), 190, HorizontalAlignment.Center, 0, compareView.EarlierCycle);
                        DrawExternalTag(node.RightOperand, secondValue, x, y + 36,
                            PowerFlowColor(flow?.Output, wire, monitoring), 190, HorizontalAlignment.Center, 0, compareView.EarlierCycle);
                        DrawLine(new Vector2(x - 34, y - 27), new Vector2(x - 34, y + 27), color, 2);
                        DrawLine(new Vector2(x + 34, y - 27), new Vector2(x + 34, y + 27), color, 2);
                        DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 30, y - 10), TiaComparisonOperator(node.CompareOperator),
                            HorizontalAlignment.Center, 60, 14, color);
                        var selectorOwnsTypeLabel = _tiaCompareTypeSelector?.Visible == true
                            && rungIndex == SelectedRung && SelectedNodeId == node.Id;
                        if (!selectorOwnsTypeLabel)
                        {
                            var typeBox = new Rect2(x - 25, y - 7, 50, 20);
                            DrawRect(typeBox, new Color("dfe2e8"));
                            DrawRect(typeBox, new Color("87949a"), false, 1);
                            DrawEditorString(ThemeDB.FallbackFont, new Vector2(typeBox.Position.X, typeBox.Position.Y + 14),
                                TiaComparisonDataType(node, first, second), HorizontalAlignment.Center, typeBox.Size.X, 11, new Color("17242c"));
                        }
                    }
                    else
                    {
                        DrawExternalTag(node.Variable, firstValue, x - 50, y - 35,
                            PowerFlowColor(flow?.Input, wire, monitoring), 150, HorizontalAlignment.Right, 0, compareView.EarlierCycle);
                        DrawExternalTag(node.RightOperand, secondValue, x + 50, y + 20,
                            PowerFlowColor(flow?.Output, wire, monitoring), 150, earlierCycle: compareView.EarlierCycle);
                        var compareBody = new Rect2(x - comparisonWidth / 2, y - 25, comparisonWidth, 50);
                        DrawRect(compareBody, new Color("e3e6eb"));
                        DrawRect(compareBody, _monitorSnapshot?.State == VirtualControllerState.Running ? live : new Color("87949a"), false, 2);
                        var label = node.CompareOperator == LadderCompareOperator.ContainsCode ? "LOCAL CODE MATCH" : node.CompareOperator.ToString();
                        DrawEditorString(ThemeDB.FallbackFont, new Vector2(compareBody.Position.X + 4, y + 6), label,
                            HorizontalAlignment.Center, comparisonWidth - 8, 9, new Color("17242c"));
                    }
                }
                else
                {
                    DrawContact(x, y, node.NormallyClosed, color);
                    if (node.EdgeMode != LadderEdgeMode.None)
                    {
                        DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 24, y + 7), node.EdgeMode == LadderEdgeMode.Rising ? "P" : "N", HorizontalAlignment.Center, 48, 11, color);
                        DrawEdgeMemoryOperand(x, y + 32, node.EdgeMemoryOperand ?? "???", placed.Bounds.Width - 8);
                    }
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
                else if (_style != LadderVendorStyle.SiemensTia)
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 65, y - 12),
                        node.Kind == LadderNodeKind.Series ? "EMPTY SERIES · TRUE" : "EMPTY PARALLEL · FALSE",
                        HorizontalAlignment.Center, 130, 10, wire);
            }
            if (node.Kind == LadderNodeKind.Contact) DrawConditionObservation(node, x, y, placed.Bounds.Width - 12);
            if (rungIndex == SelectedRung && SelectedNodeId == node.Id)
            {
                var selection = tiaComparison
                    ? new Rect2(x - 37, y - 30, 74, 60)
                    : new Rect2(origin + new Vector2(placed.Bounds.X, placed.Bounds.Y), new(placed.Bounds.Width, placed.Bounds.Height));
                if (_style == LadderVendorStyle.SiemensTia) DrawSelectionOutline(selection.Grow(2), new Color("2448bd"), 2);
                else DrawRect(selection, new Color("0087a8"), false, 2);
            }
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
        InitializeTiaCounterTypeSelector();
    }

    public void SetTiaDisplaySettings(TiaOperandRepresentation representation,
        TiaTagInformationDisplay information, TiaTagInformationPosition position)
    {
        if (_style != LadderVendorStyle.SiemensTia) return;
        OperandRepresentation = representation;
        TagInformationDisplay = information;
        TagInformationPosition = position;
        RefreshDocument();
    }

    public void RefreshDocument()
    {
        // Any document/view rebuild invalidates drag-time geometry and targets.
        _branchCloseDrag = null;
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
        if (_style == LadderVendorStyle.SiemensTia && TagInformationDisplay != TiaTagInformationDisplay.Hidden
            && TagInformationPosition == TiaTagInformationPosition.BelowNetwork)
        {
            var font = ThemeDB.FallbackFont;
            minimumWidth = Math.Max(minimumWidth, _document.Rungs.SelectMany(NetworkTagInformationEntries)
                .Select(entry => font.GetStringSize(NetworkTagInformationText(entry), HorizontalAlignment.Left, -1, 11).X + 120)
                .DefaultIfEmpty(760).Max());
        }
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
        RefreshTiaCounterTypeSelector();
        RefreshTiaComparisonTypeSelector();
        RefreshTiaNumericTypeSelector();
    }

    public void SetMonitorSnapshot(VirtualControllerSnapshot? snapshot, ObservationUnavailableReason reason = ObservationUnavailableReason.None)
    {
        _monitorSnapshot = reason == ObservationUnavailableReason.None ? snapshot : null;
        _monitorUnavailableReason = reason;
        _monitorPublication = null;
        _pendingMonitorPublication = null;
        _lastObservedPublications.Clear();
        _lastObservationIdentity = null;
        _presentationCache.Clear();
        if (_instructionWindow?.Visible == true) RefreshInstructionInspector();
        QueueRedraw();
        RefreshTiaCounterTypeSelector();
        RefreshTiaComparisonTypeSelector();
        RefreshTiaNumericTypeSelector();
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
        if (output)
        {
            var terminalRung = _document.Rungs[rungIndex];
            if (_style == LadderVendorStyle.SiemensTia && terminalRung.IsLabel)
                return ToCanvas(LabelBounds(rungIndex).GetCenter());
            return ToCanvas(TreeOutputPosition(rungIndex));
        }
        var rung = _document.Rungs[rungIndex];
        var id = rung.Branches[branchIndex].Contacts[contactIndex].Id;
        var placed = TreeLayout(rung).Nodes.First(n => n.Node.Id == id);
        return ToCanvas(TreeOrigin(rungIndex) + new Vector2(placed.Bounds.X + placed.Bounds.Width / 2, placed.ContinuityY));
    }

    public Vector2 GetTreeInputWirePosition(int rungIndex, string nodeId)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var wire = TreeLayout(_document.Rungs[rungIndex]).Wires.FirstOrDefault(item =>
            item.FlowNodeId == nodeId && item.FlowSide == LadderTreeWireFlowSide.Input)
            ?? throw new InvalidOperationException($"Node '{nodeId}' does not have a selectable input connection.");
        return ToCanvas(TreeOrigin(rungIndex) + new Vector2((wire.X1 + wire.X2) / 2, (wire.Y1 + wire.Y2) / 2));
    }

    public Vector2 GetTreeOutputWirePosition(int rungIndex, string nodeId)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var wire = TreeLayout(_document.Rungs[rungIndex]).Wires.FirstOrDefault(item =>
            item.FlowNodeId == nodeId && item.FlowSide == LadderTreeWireFlowSide.Output)
            ?? throw new InvalidOperationException($"Node '{nodeId}' does not have a selectable output connection.");
        return ToCanvas(TreeOrigin(rungIndex) + new Vector2((wire.X1 + wire.X2) / 2, (wire.Y1 + wire.Y2) / 2));
    }

    public Vector2 GetTreeSlotPosition(int rungIndex, string parentId, int slotIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var slot = TreeLayout(_document.Rungs[rungIndex]).Slots.FirstOrDefault(item =>
            item.ParentId == parentId && item.ChildIndex == slotIndex)
            ?? throw new InvalidOperationException($"Node '{parentId}' does not have slot {slotIndex}.");
        return ToCanvas(TreeOrigin(rungIndex) + new Vector2(slot.X, slot.Y));
    }

    public bool ReviewBranchCloseDragActive => _branchCloseDrag is not null;
    public int ReviewBranchCloseCandidateSlot => _branchCloseDrag?.CandidateSlot ?? -1;

    public void SelectRung(int index)
    {
        SelectedRung = Math.Clamp(index, 0, Math.Max(0, _document.Rungs.Count - 1));
        ClearElementSelection();
        NetworkSelected = true;
        QueueRedraw();
        RefreshTiaCounterTypeSelector();
        RefreshTiaComparisonTypeSelector();
        RefreshTiaNumericTypeSelector();
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
        NetworkLabelSelected = false;
        NetworkSelected = false;
        SelectedNodeId = output ? string.Empty : _document.Rungs[rungIndex].Branches[branchIndex].Contacts[contactIndex].Id;
        _selectedTreeSlot = null;
        if (notify)
        {
            RungSelected?.Invoke(rungIndex);
            ElementSelected?.Invoke(rungIndex, SelectedBranch, SelectedContact, output);
        }
        QueueRedraw();
        RefreshTiaCounterTypeSelector();
        RefreshTiaComparisonTypeSelector();
        RefreshTiaNumericTypeSelector();
    }

    public void SelectNetworkLabel(int rungIndex, bool notify = true)
    {
        SelectElement(rungIndex, -1, -1, output: true, notify: false);
        NetworkLabelSelected = true;
        if (notify)
        {
            RungSelected?.Invoke(rungIndex);
            ElementSelected?.Invoke(rungIndex, -1, -1, true);
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
        NetworkLabelSelected = false;
        NetworkSelected = false;
        SelectedNodeId = rung.Branches[branchIndex].Id;
        _selectedTreeSlot = TreeLayout(rung).Slots.FirstOrDefault(s => s.ParentId == SelectedNodeId && s.ChildIndex == insertionIndex);
        if (notify)
        {
            RungSelected?.Invoke(rungIndex);
            InsertionPointSelected?.Invoke(rungIndex, branchIndex, insertionIndex);
        }
        QueueRedraw();
        RefreshTiaCounterTypeSelector();
        RefreshTiaComparisonTypeSelector();
        RefreshTiaNumericTypeSelector();
    }

    public void ClearElementSelection()
    {
        SelectedBranch = -1;
        SelectedContact = -1;
        SelectedInsertionIndex = -1;
        OutputSelected = false;
        NetworkLabelSelected = false;
        NetworkSelected = false;
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
            if (slot is not null) return SelectTreeInsertionPoint(rungIndex, slot.ParentId, slot.ChildIndex, notify: false);
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
        var rung = _document.Rungs[rungIndex];
        var point = position - TreeOrigin(rungIndex);
        var slot = TreeLayout(rung).HitSlot(point.X, point.Y);
        // Branch placement is structural even while the current network still
        // qualifies for the legacy flat editing path. The toolbar command
        // already opens at CaptureTreeTarget(); drag/drop must honor the same
        // exact wire slot instead of degrading to "add another root path".
        if (slot is not null && (kind == "branch" || !rung.IsFlat))
        {
            if (kind == "branch")
            {
                SelectTreeInsertionPoint(rungIndex, slot.ParentId, slot.ChildIndex, notify: false);
                TreeCommandRequested?.Invoke(new(LadderTreeCommandId.OpenParallelAtSlot, TreeTarget()));
                return true;
            }
            var isComparison = kind.StartsWith("compare-", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan(8), out var comparisonValue) && Enum.IsDefined((LadderCompareOperator)comparisonValue);
            if (kind is not ("no" or "nc" or "edge-rising" or "edge-falling") && !isComparison) return false;
            SelectTreeNode(rungIndex, slot.ParentId); _selectedTreeSlot = slot;
            var parent = EditableLadderTree.Find(rung.Root, slot.ParentId)!;
            var template = isComparison ? NewComparisonTemplate((LadderCompareOperator)int.Parse(kind.AsSpan(8))) : NewContactTemplate() with { NormallyClosed = kind == "nc",
                EdgeMode = kind == "edge-rising" ? LadderEdgeMode.Rising : kind == "edge-falling" ? LadderEdgeMode.Falling : LadderEdgeMode.None };
            EditableLadderNode payload = parent.Kind == LadderNodeKind.Parallel
                ? new("clipboard-series", LadderNodeKind.Series, Children: Array.AsReadOnly(new[] { template })) : template;
            TreeCommandRequested?.Invoke(new(LadderTreeCommandId.InsertAt, TreeTarget(), payload)); return true;
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

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        ReviewBlockDropChecks++;
        var payload = data.AsString();
        if (ProgramBlockDragPayload.TryGetBlockIndex(payload, out var blockIndex))
            return CanDropProgramBlock(atPosition, blockIndex);
        ReviewTagDropChecks++;
        if (!LadderTagOperandDragPayload.TryGetIdentity(payload, out var tagIdentity))
        { ReviewBlockDropRejection = "payload"; ReviewTagDropRejection = "payload"; return false; }
        if (MutationLocked) { ReviewTagDropRejection = "locked"; return false; }
        if (!LadderTagOperandDragPayload.TryResolveScalarTag(_document, tagIdentity, out var tag, out var operand))
        { ReviewTagDropRejection = "tag-identity"; return false; }
        if (!TryTagDropTarget(atPosition, out var rungIndex, out var nodeId, out var target, out var requiredType, out var writable))
        { ReviewTagDropRejection = "target"; return false; }
        if (LadderOperandSymbols.Validate(LadderOperandSymbols.ForDocument(_document), operand, requiredType, writable, allowLiteral: false) is { } error)
        { ReviewTagDropRejection = error; return false; }
        ReviewTagDropRejection = string.Empty;
        ReviewBlockDropRejection = string.Empty;
        return true;

        bool CanDropProgramBlock(Vector2 position, int blockIndex)
        {
            if (MutationLocked) { ReviewBlockDropRejection = "locked"; return false; }
            if (blockIndex < 0 || blockIndex >= _document.Blocks.Count)
            { ReviewBlockDropRejection = "block-index"; return false; }
            if (_document.Blocks[blockIndex].BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock))
            { ReviewBlockDropRejection = "block-type"; return false; }
            if (_document.Blocks[_document.ActiveBlockIndex].Id == _document.Blocks[blockIndex].Id
                || _document.Blocks[blockIndex].SemanticMode != LadderSemanticMode.Parameterized)
            { ReviewBlockDropRejection = "self-or-mode"; return false; }

            var logical = ToLogical(position);
            var blockRungIndex = RungIndexAt(logical.Y);
            if (blockRungIndex < 0) { ReviewBlockDropRejection = $"rung:{position}->{logical}"; return false; }
            var accepted = TryHitElement(blockRungIndex, logical, out _, out _, out var output) && output;
            ReviewBlockDropRejection = accepted ? string.Empty : $"terminal:{position}->{logical}:rung={blockRungIndex}";
            return accepted;
        }
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        ReviewBlockDrops++;
        var payload = data.AsString();
        if (!_CanDropData(atPosition, data)) return;
        if (LadderTagOperandDragPayload.TryGetIdentity(payload, out var tagIdentity))
        {
            if (!TryTagDropTarget(atPosition, out var tagRungIndex, out var nodeId, out _, out _, out _)) return;
            var logical = ToLogical(atPosition);
            // InlineOperandRequested resolves the selected network, so instruction-pin
            // targets (which do not hit a selectable node) must select their rung too.
            SelectRung(tagRungIndex);
            if (nodeId.Length > 0) SelectTreeNode(tagRungIndex, nodeId);
            else if (TryHitElement(tagRungIndex, logical, out var branchIndex, out var contactIndex, out var tagOutput))
                SelectElement(tagRungIndex, branchIndex, contactIndex, tagOutput);
            if (TryTagDropTarget(atPosition, out _, out _, out var target, out _, out _))
                TagOperandDropRequested?.Invoke(tagIdentity, target);
            return;
        }
        if (!ProgramBlockDragPayload.TryGetBlockIndex(payload, out var blockIndex))
            return;

        var position = ToLogical(atPosition);
        var rungIndex = RungIndexAt(position.Y);
        if (rungIndex < 0 || !TryHitElement(rungIndex, position, out _, out _, out var output) || !output)
            return;
        BlockCallDropRequested?.Invoke(rungIndex, blockIndex);
    }

    private bool TryTagDropTarget(Vector2 atPosition, out int rungIndex, out string nodeId,
        out string target, out PlcVariableType requiredType, out bool writable)
    {
        rungIndex = -1;
        nodeId = target = string.Empty;
        requiredType = PlcVariableType.Bool;
        writable = false;
        var position = ToLogical(atPosition);
        rungIndex = RungIndexAt(position.Y);
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count) return false;
        var rung = _document.Rungs[rungIndex];

        if (TryHitTiaComparisonOperand(rungIndex, position, out nodeId, out target))
        {
            var node = EditableLadderTree.Find(rung.Root, nodeId);
            if (node is null) return false;
            requiredType = node.ComparisonValueType switch
            {
                LadderComparisonValueType.Int => PlcVariableType.Int,
                LadderComparisonValueType.Real => PlcVariableType.Real,
                LadderComparisonValueType.DInt => PlcVariableType.DInt,
                _ => target == "compare-right" ? PlcVariableType.Int
                    : LadderOperandSymbols.ForDocument(_document).FirstOrDefault(symbol => symbol.Name == node.Variable)?.Type ?? PlcVariableType.DInt,
            };
            return true;
        }

        // Existing rendered instruction input/output operands use the same
        // inline editor and validation path as double-click editing.
        var currentSymbols = LadderOperandSymbols.ForDocument(_document);
        var currentOperand = string.Empty;
        if (TryHitTiaInstructionOperand(rungIndex, position, out target))
        {
            // Counter PV is authored as the instruction's numeric literal in
            // the current editor/runtime model; a tag drag cannot be applied.
            if (target == "terminal-counter-pv") return false;
            currentOperand = target switch
            {
                "terminal-counter-r" => rung.CounterResetInput,
                "terminal-counter-load" => rung.CounterLoadInput,
                "terminal-counter-pv" => rung.CounterPreset.ToString(CultureInfo.InvariantCulture),
                "terminal-timer-reset" => rung.TimerResetOperand,
                "terminal-timer-pt" => rung.TimerPresetOperand,
                "terminal-numeric-a" => rung.NumericSourceA,
                "terminal-numeric-b" => rung.NumericSourceB,
                "terminal-numeric-c" => rung.NumericSourceC,
                "terminal-output" => rung.IsNumericOperation ? rung.NumericDestination : rung.CoilVariable,
                _ => string.Empty,
            };
            requiredType = target switch
            {
                "terminal-counter-r" or "terminal-counter-load" or "terminal-timer-reset" => PlcVariableType.Bool,
                "terminal-counter-pv" => rung.CounterValueType is LadderCounterValueType.DInt or LadderCounterValueType.UDInt ? PlcVariableType.DInt : PlcVariableType.Int,
                "terminal-timer-pt" => PlcVariableType.Time,
                "terminal-numeric-a" => NumericSourceType(0),
                "terminal-numeric-b" => NumericSourceType(1),
                "terminal-numeric-c" => NumericSourceType(2),
                _ => currentSymbols.FirstOrDefault(symbol => symbol.Name == currentOperand)?.Type ?? PlcVariableType.DInt,
            };
            writable = target == "terminal-output";
            return true;
        }

        if (!TryHitElement(rungIndex, position, out _, out _, out var output)) return false;
        if (output && (IsBitCoil(rung) || rung.IsTimer || rung.IsTimerReset || rung.IsCounter || rung.IsCounterLoad || rung.IsCounterReset))
        {
            target = "terminal-output";
            requiredType = rung.IsTimer || rung.IsTimerReset ? PlcVariableType.Timer
                : rung.IsCounter || rung.IsCounterLoad || rung.IsCounterReset ? PlcVariableType.Counter
                : PlcVariableType.Bool;
            writable = true;
            return true;
        }

        var hit = TreeLayout(rung).HitNode(position.X - TreeOrigin(rungIndex).X, position.Y - TreeOrigin(rungIndex).Y);
        if (hit?.Node.Kind != LadderNodeKind.Contact) return false;
        nodeId = hit.Node.Id;
        target = "contact";
        requiredType = PlcVariableType.Bool;
        return true;

        PlcVariableType NumericSourceType(int sourceIndex)
        {
            var selected = LadderNumericOperationRules.SelectedDataTypeForSource(
                rung.NumericOperationKind, sourceIndex, rung.NumericDataType,
                rung.NumericInputDataType, rung.NumericOutputDataType);
            if (selected == LadderNumericDataType.Unspecified)
            {
                var choices = LadderNumericOperationRules.HasTiaDualDataTypeSelectors(rung.NumericOperationKind)
                    ? rung.NumericOperationKind == LadderNumericOperationKind.Scale && sourceIndex != 1
                        ? LadderNumericDataTypePresentation.TiaOutputTypesFor(rung.NumericOperationKind)
                        : LadderNumericDataTypePresentation.TiaInputTypesFor(rung.NumericOperationKind)
                    : LadderNumericDataTypePresentation.TiaSelectableTypesFor(rung.NumericOperationKind);
                var fixedTypes = choices.Where(item => item != LadderNumericDataType.Unspecified).Distinct().ToArray();
                if (fixedTypes.Length == 1) selected = fixedTypes[0];
                var inferenceOperand = rung.NumericOperationKind switch
                {
                    LadderNumericOperationKind.Scale when sourceIndex == 1 => null,
                    LadderNumericOperationKind.Scale => rung.NumericDestination,
                    LadderNumericOperationKind.Normalize => rung.NumericSourceA,
                    LadderNumericOperationKind.Move => rung.NumericDestination,
                    _ => currentSymbols.FirstOrDefault(symbol => symbol.Name == currentOperand)?.Name,
                };
                if (selected == LadderNumericDataType.Unspecified)
                    selected = LadderNumericDataTypePresentation.FromVariableType(
                        currentSymbols.FirstOrDefault(symbol => symbol.Name == inferenceOperand)?.Type);
            }
            return selected switch
            {
                LadderNumericDataType.Int => PlcVariableType.Int,
                LadderNumericDataType.Real => PlcVariableType.Real,
                _ => PlcVariableType.DInt,
            };
        }
    }

    /// <summary>Open the same transactional inline operand editor used by double-click/F2.</summary>
    public void RequestInlineOperandEdit(string? initialText, bool clearOnOpen, string? target) =>
        InlineOperandRequested?.Invoke(initialText, clearOnOpen, target);

    public override void _GuiInput(InputEvent input)
    {
        // Consume edit keys before Control's directional focus navigation.
        if (input is InputEventKey)
        {
            if (_branchCloseDrag is not null) { _branchCloseDrag = null; QueueRedraw(); }
            _ShortcutInput(input);
            return;
        }
        if (_branchCloseDrag is not null && input is InputEventMouseMotion motion)
        {
            var motionPosition = ToLogical(motion.Position);
            var drag = _branchCloseDrag;
            var candidate = -1;
            if (RungIndexAt(motionPosition.Y) == drag.RungIndex)
            {
                var rung = _document.Rungs[drag.RungIndex];
                var local = motionPosition - TreeOrigin(drag.RungIndex);
                var slot = TreeLayout(rung).HitSlot(local.X, local.Y, 24);
                var main = EditableLadderTree.Find(rung.Root, drag.MainPathId);
                if (slot?.ParentId == drag.MainPathId && main?.Children is { Count: > 1 }
                    && slot.ChildIndex > 0 && slot.ChildIndex <= main.Children.Count)
                    candidate = slot.ChildIndex;
            }
            _branchCloseDrag = drag with { Current = motionPosition, CandidateSlot = candidate };
            QueueRedraw(); AcceptEvent(); return;
        }
        if (_branchCloseDrag is not null
            && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            var drag = _branchCloseDrag;
            _branchCloseDrag = null;
            if (drag.CandidateSlot >= 0 && !MutationLocked)
            {
                SelectTreeNode(drag.RungIndex, drag.BranchPathId, notify: false);
                var target = TreeTarget() with { ParentId = drag.MainPathId, Slot = drag.CandidateSlot };
                TreeCommandRequested?.Invoke(new(LadderTreeCommandId.CloseParallelAtSlot, target));
            }
            QueueRedraw(); AcceptEvent(); return;
        }
        if (input is not InputEventMouseButton { Pressed: true } click
            || click.ButtonIndex is not (MouseButton.Left or MouseButton.Right)) return;
        GrabFocus();
        var logicalPosition = ToLogical(click.Position);
        var index = RungIndexAt(logicalPosition.Y);
        if (index < 0 || index >= _document.Rungs.Count) return;
        if (_document.Rungs[index].IsLabel && LabelBounds(index).HasPoint(logicalPosition))
        {
            SelectNetworkLabel(index);
            if (click.ButtonIndex == MouseButton.Right)
                ContextMenuRequested?.Invoke(index, -1, -1, true);
            else if (click.DoubleClick)
                PropertiesRequested?.Invoke(index, -1, -1, true);
            AcceptEvent();
            return;
        }
        if (click.ButtonIndex == MouseButton.Left && click.DoubleClick
            && TryHitTiaInstructionOperand(index, logicalPosition, out var instructionOperand))
        {
            SelectElement(index, -1, -1, output: true);
            InlineOperandRequested?.Invoke(null, false, instructionOperand);
            AcceptEvent();
            return;
        }
        if (click.ButtonIndex == MouseButton.Left && click.DoubleClick
            && TryHitTiaComparisonOperand(index, logicalPosition, out var comparisonNode, out var comparisonTarget))
        {
            SelectTreeNode(index, comparisonNode);
            InlineOperandRequested?.Invoke(null, false, comparisonTarget);
            AcceptEvent();
            return;
        }
        if (TryHitElement(index, logicalPosition, out var branchIndex, out var contactIndex, out var output))
        {
            SelectElement(index, branchIndex, contactIndex, output);
            var terminalBounds = output ? TerminalBounds(index) : default;
            if (click.ButtonIndex == MouseButton.Left && output
                && _document.Rungs[index].HasEmptyInstructionPlaceholder
                && terminalBounds.HasPoint(logicalPosition)
                && logicalPosition.X >= terminalBounds.End.X - 17
                && logicalPosition.Y <= terminalBounds.Position.Y + 17)
            {
                EmptyInstructionChooserRequested?.Invoke(index);
                AcceptEvent();
                return;
            }
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
        var treeWire = treeLayout.HitWire(treePoint.X, treePoint.Y);
        if (_style == LadderVendorStyle.SiemensTia && !MutationLocked
            && click.ButtonIndex == MouseButton.Left
            && treeWire is { FlowSide: LadderTreeWireFlowSide.Output })
        {
            var rung = _document.Rungs[index];
            var branchPath = EditableLadderTree.Find(rung.Root, treeWire.FlowNodeId);
            var parallel = branchPath?.Kind == LadderNodeKind.Series
                ? EditableLadderTree.Visit(rung.Root).FirstOrDefault(node => node.Kind == LadderNodeKind.Parallel
                    && node.Children?.Any(child => child.Id == branchPath.Id) == true)
                : null;
            if (branchPath is { OpenEnd: true }
                && parallel?.Children is { Count: >= 2 } paths
                && paths.Skip(1).Any(path => path.Id == branchPath.Id)
                && paths[0].Kind == LadderNodeKind.Series
                && paths[0].Children is { Count: > 1 })
            {
                SelectTreeNode(index, branchPath.Id);
                var start = TreeOrigin(index) + new Vector2(
                    (treeWire.X1 + treeWire.X2) / 2, (treeWire.Y1 + treeWire.Y2) / 2);
                _branchCloseDrag = new(index, branchPath.Id, paths[0].Id, start, start);
                QueueRedraw(); AcceptEvent(); return;
            }
        }
        if (treeSlot is not null || treeWire is not null || treeNode is not null)
        {
            if (treeSlot is not null) SelectTreeInsertionPoint(index, treeSlot.ParentId, treeSlot.ChildIndex);
            else SelectTreeNode(index, treeWire?.FlowNodeId ?? treeNode!.Node.Id);
            if (click.ButtonIndex == MouseButton.Right) ShowTreePopup();
            QueueRedraw(); AcceptEvent(); return;
        }
        SelectRung(index);
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
        else if (!key.CtrlPressed && !key.ShiftPressed && !key.AltPressed && !key.MetaPressed && key.Keycode == Key.F9)
        {
            if (!MutationLocked) KeyboardInstructionRequested?.Invoke("no");
        }
        else if (!key.CtrlPressed && !key.ShiftPressed && !key.AltPressed && !key.MetaPressed && key.Keycode == Key.F8)
        {
            if (!MutationLocked && _style == LadderVendorStyle.SiemensTia) KeyboardInstructionRequested?.Invoke("empty-box");
        }
        else if (!key.CtrlPressed && !key.ShiftPressed && !key.AltPressed && !key.MetaPressed && key.Keycode == Key.F10)
        {
            if (!MutationLocked) KeyboardInstructionRequested?.Invoke("nc");
        }
        else if (key.Keycode == Key.F2) InlineOperandRequested?.Invoke(null, false, null);
        else if (key.CtrlPressed && key.Keycode == Key.X) { if (!MutationLocked) CutRequested?.Invoke(); }
        else if (key.CtrlPressed && key.Keycode == Key.C) CopyRequested?.Invoke();
        else if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter)
        {
            if (OutputSelected || !string.IsNullOrEmpty(SelectedNodeId)) InlineOperandRequested?.Invoke(null, true, null);
            else PropertiesRequested?.Invoke(SelectedRung, SelectedBranch, SelectedContact, OutputSelected);
        }
        else if (!key.CtrlPressed && !key.AltPressed && !key.MetaPressed && !MutationLocked
            && (OutputSelected || !string.IsNullOrEmpty(SelectedNodeId))
            && key.Unicode is > 0 and <= 0x10FFFF
            && char.IsLetterOrDigit(char.ConvertFromUtf32((int)key.Unicode), 0))
            InlineOperandRequested?.Invoke(char.ConvertFromUtf32((int)key.Unicode), false, null);
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

    private bool TryHitTiaInstructionOperand(int rungIndex, Vector2 position, out string target)
    {
        target = string.Empty;
        if (_style != LadderVendorStyle.SiemensTia) return false;
        var rung = _document.Rungs[rungIndex];
        var bounds = TerminalBounds(rungIndex);
        var center = TreeOutputPosition(rungIndex);
        var inputs = new List<string>();
        if (rung.IsCounter)
        {
            if (rung.CounterKind == LadderCounterKind.CountUp) inputs.Add("terminal-counter-r");
            else inputs.Add("terminal-counter-load");
            inputs.Add("terminal-counter-pv");
        }
        else if (rung.IsTimer)
        {
            if (rung.TimerKind == LadderTimerKind.RetentiveOnDelay) inputs.Add("terminal-timer-reset");
            inputs.Add("terminal-timer-pt");
        }
        else if (rung.IsNumericOperation && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind))
        {
            inputs.Add("terminal-numeric-a");
            if (TiaNumericInstructionPorts.Inputs(rung.NumericOperationKind).Count > 1) inputs.Add("terminal-numeric-b");
            if (TiaNumericInstructionPorts.Inputs(rung.NumericOperationKind).Count > 2) inputs.Add("terminal-numeric-c");
        }

        var nearestInput = inputs
            .Select((candidate, order) =>
            {
                var found = TryTerminalInputPinY(rungIndex, rung, candidate, out var pinY);
                return (candidate, order, found, pinY);
            })
            .Where(item => item.found && new Rect2(bounds.Position.X - 205, item.pinY - 21, 205, 42).HasPoint(position))
            .OrderBy(item => Math.Abs(item.pinY - position.Y))
            .ThenBy(item => item.order)
            .FirstOrDefault();
        if (nearestInput.found)
        {
            target = nearestInput.candidate;
            return true;
        }

        // Cover the complete rendered OUT operand field. Its normal-row field
        // begins 18 px above the conductor; the previous -6 px hit region
        // missed the upper half even though that text was visibly clickable.
        // The lower-data-row variants still fit within this shared region.
        if (rung.IsNumericOperation && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind)
            && new Rect2(bounds.End.X + 5, center.Y - 20, 190, 58).HasPoint(position))
        { target = "terminal-output"; return true; }
        if (IsBitCoil(rung) && new Rect2(center.X - 105, center.Y - 70, 210, 42).HasPoint(position))
        { target = "terminal-output"; return true; }
        return false;
    }

    private bool TryHitTiaComparisonOperand(int rungIndex, Vector2 position, out string nodeId, out string target)
    {
        nodeId = target = string.Empty;
        if (_style != LadderVendorStyle.SiemensTia) return false;
        var origin = TreeOrigin(rungIndex);
        foreach (var placed in TreeLayout(_document.Rungs[rungIndex]).Nodes.Where(item => item.Node.Kind == LadderNodeKind.Compare))
        {
            var centerX = origin.X + placed.Bounds.X + placed.Bounds.Width / 2;
            var centerY = origin.Y + placed.ContinuityY;
            var left = new Rect2(centerX - 95, centerY - 68, 190, 30);
            var right = new Rect2(centerX - 95, centerY + 30, 190, 30);
            if (left.HasPoint(position)) { nodeId = placed.Node.Id; target = "compare-left"; return true; }
            if (right.HasPoint(position)) { nodeId = placed.Node.Id; target = "compare-right"; return true; }
        }
        return false;
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

    /// <summary>The visible TIA network surface, including its header and comment row.</summary>
    public Rect2 GetTiaNetworkSelectionBounds(int rungIndex)
    {
        if (_style != LadderVendorStyle.SiemensTia)
            throw new InvalidOperationException("TIA network selection bounds require the Siemens TIA style.");
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        return ToCanvas(new Rect2(22, RungTop(rungIndex), LogicalViewportSize.X - 42,
            RungHeight(_document.Rungs[rungIndex])));
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
    private float NetworkBaseHeight(EditableRung rung) => _style == LadderVendorStyle.SiemensTia
        ? Math.Max(320, TreeLayout(rung).Height + 184)
        : Math.Max(250, TreeLayout(rung).Height + 110);
    private float RungHeight(EditableRung rung)
    {
        var baseHeight = NetworkBaseHeight(rung);
        if (_style != LadderVendorStyle.SiemensTia || TagInformationDisplay == TiaTagInformationDisplay.Hidden
            || TagInformationPosition != TiaTagInformationPosition.BelowNetwork) return baseHeight;
        var rows = NetworkTagInformationEntries(rung).Length;
        return rows == 0 ? baseHeight : baseHeight + rows * 15 + 8;
    }
    private float RungAdvance(EditableRung rung) =>
        RungHeight(rung) + 15;

    public override void _Draw()
    {
        BeginMonitoringDraw();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * ZoomFactor);
        DrawRect(new Rect2(Vector2.Zero, LogicalViewportSize), new Color("f5f6f7"));
        if (_style == LadderVendorStyle.SiemensTia) DrawTia();
        else DrawLogix();
        if (_style == LadderVendorStyle.SiemensTia) DrawBranchClosePreview();
        if (HasFocus()) DrawRect(new Rect2(Vector2.One, LogicalViewportSize - Vector2.One * 2), new Color("075985"), false, 3);
    }

    private void DrawBranchClosePreview()
    {
        if (_branchCloseDrag is not { } drag || drag.RungIndex < 0 || drag.RungIndex >= _document.Rungs.Count) return;
        var rung = _document.Rungs[drag.RungIndex];
        var main = EditableLadderTree.Find(rung.Root, drag.MainPathId);
        if (main?.Children is not { Count: > 1 }) return;
        var origin = TreeOrigin(drag.RungIndex);
        var slots = TreeLayout(rung).Slots.Where(slot => slot.ParentId == drag.MainPathId
            && slot.ChildIndex > 0 && slot.ChildIndex <= main.Children.Count).ToArray();
        var green = new Color("00a651");
        foreach (var slot in slots)
        {
            var point = origin + new Vector2(slot.X, slot.Y);
            if (slot.ChildIndex == drag.CandidateSlot) DrawCircle(point, 8, green);
            else DrawCircle(point, 6, green, false, 2);
        }
        var vector = drag.Current - drag.Start;
        var length = vector.Length();
        if (length <= 0.5f) return;
        var direction = vector / length;
        const float dash = 8;
        const float gap = 5;
        for (var offset = 0.0f; offset < length; offset += dash + gap)
            DrawLine(drag.Start + direction * offset,
                drag.Start + direction * Math.Min(offset + dash, length), green, 2);
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
            // TIA's network area uses a white work surface, a neutral gray
            // header, and its own comment row above the ladder work area.
            DrawRect(new Rect2(22, y, LogicalViewportSize.X - 42, rungHeight), new Color("ffffff"));
            DrawRect(new Rect2(22, y, LogicalViewportSize.X - 42, 30), new Color("d9d9d9"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(36, y + 21), $"Network {index + 1}", HorizontalAlignment.Left, -1, 13, new Color("17242c"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(130, y + 21), _document.Rungs[index].Label, HorizontalAlignment.Left, -1, 12, new Color("5b6a72"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(36, y + 48), "Comment", HorizontalAlignment.Left, 180, 11, new Color("829099"));
            DrawLine(new Vector2(36, y + 53), new Vector2(LogicalViewportSize.X - 36, y + 53), new Color("cbd3d8"), 1);
            DrawRungLogic(index, _document.Rungs[index], 48, y + 174, LogicalViewportSize.X - 94, new Color("111111"));
            if (TagInformationDisplay != TiaTagInformationDisplay.Hidden
                && TagInformationPosition == TiaTagInformationPosition.BelowNetwork)
                DrawNetworkTagInformation(_document.Rungs[index], y + Math.Max(250, TreeLayout(_document.Rungs[index]).Height + 110) + 18);
            if (index == SelectedRung && NetworkSelected)
            {
                // Network selection is intentionally border-only: the header,
                // comment row, rail, and every contained instruction remain
                // unobscured. Exact installed-TIA styling remains an evidence
                // gap, so do not infer it from the instruction-selection box.
                var selection = new Rect2(22, y, LogicalViewportSize.X - 42, rungHeight);
                DrawRect(selection, new Color("2448bd"), false, 2);
                DrawRect(new Rect2(selection.Position, new Vector2(4, 30)), new Color("2448bd"));
            }
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
        var outputGap = TerminalGap(rung);
        var outputId = OutputElementId(rung);
        var outputPowered = IsElementEnergized(outputId);
        var measured = TreeLayout(rung);
        // A TIA network whose first element is a parallel branch uses the
        // left power rail as that branch's split. Do not insert the 16 px
        // lead-in used for an ordinary series root: it creates a second,
        // detached-looking inner rail connected by a mid-rung stub.
        var treeOrigin = new Vector2(TreeLeft(rung), y - 42);
        var outputOffsetY = measured.Height / 2;
        if (_style == LadderVendorStyle.SiemensTia && rung.HasEmptyInstructionPlaceholder
            && rung.Root.Kind == LadderNodeKind.Parallel && rung.Root.Children is { Count: > 0 } children)
            outputOffsetY = measured.Nodes.First(node => node.Node.Id == children[0].Id).ContinuityY;
        y = treeOrigin.Y + outputOffsetY;
        outputX = treeOrigin.X + measured.Width + TerminalLead(rung);
        right = outputX + outputGap + TerminalOutputTail(rung);
        var monitoring = IsTiaMonitoringActive;
        var flow = ProjectLadderPowerFlow(measured);
        LadderPowerFlow? rootFlow = null;
        if (measured.Nodes.Count > 0) flow.TryGetValue(measured.Nodes[0].Node.Id, out rootFlow);
        var entering = rootFlow?.Input;
        var terminalInputFlow = rung.HasTerminalAction ? TerminalNetworkEnableFlow(outputId) : rootFlow?.Output;
        var terminalOutputFlow = rung.HasTerminalAction ? TerminalOutputFlow(outputId) : rootFlow?.Output;
        if (terminalInputFlow is null && !rung.HasTerminalAction) terminalInputFlow = rootFlow?.Output;
        if (terminalOutputFlow is null && !rung.HasTerminalAction) terminalOutputFlow = rootFlow?.Output;
        var tiaNumericEnablePins = _style == LadderVendorStyle.SiemensTia && rung.IsNumericOperation
            && TiaNumericInstructionPorts.HasEnablePins(rung.NumericOperationKind);
        var instructionInputFlow = tiaNumericEnablePins
            ? NumericInstructionEnableFlow(rung, output: false)
            : terminalInputFlow;
        if (tiaNumericEnablePins)
            terminalOutputFlow = NumericInstructionEnableFlow(rung, output: true);
        DrawRecursiveConditions(rungIndex, rung, measured, treeOrigin, wire, live);
        DrawPowerFlowLine(new Vector2(left, y), new Vector2(treeOrigin.X, y), wire, entering, monitoring, 2);
        DrawPowerFlowLine(new Vector2(treeOrigin.X + measured.Width, y), new Vector2(outputX - outputGap, y), wire, instructionInputFlow, monitoring, 2);
        DrawPowerFlowLine(new Vector2(outputX + outputGap, y), new Vector2(right, y), wire, terminalOutputFlow, monitoring, 2);
        var outputColor = PowerFlowColor(terminalOutputFlow, outputPowered ? live : wire, monitoring);
        // Siemens networks share one left power rail across their rung/branch
        // work area. A root parallel split coincides with that rail. Rockwell
        // keeps its separate short rung-origin rail treatment.
        const float railHalfHeight = 34.0f;
        if (_style == LadderVendorStyle.SiemensTia)
        {
            var networkTop = RungTop(rungIndex);
            DrawPowerFlowLine(new Vector2(left, networkTop + 58),
                new Vector2(left, networkTop + NetworkBaseHeight(rung) - 4), wire, entering, monitoring, 3);
        }
        else
            DrawPowerFlowLine(new Vector2(left, y - railHalfHeight), new Vector2(left, y + railHalfHeight), wire, entering, monitoring, 3);
        if (_style == LadderVendorStyle.SiemensTia && rung.HasEmptyInstructionPlaceholder)
        {
            // An unconfigured TIA box is an incomplete instruction on an
            // otherwise open network. TIA continues the conductor with its
            // insertion chevron; the right power rail belongs to a completed
            // network and must not be drawn here.
            var tip = new Vector2(right + 8, y);
            DrawLine(new Vector2(right - 1, y - 7), tip, outputColor, 2);
            DrawLine(new Vector2(right - 1, y + 7), tip, outputColor, 2);
            DrawLine(new Vector2(right + 5, y - 7), new Vector2(right + 14, y), outputColor, 2);
            DrawLine(new Vector2(right + 5, y + 7), new Vector2(right + 14, y), outputColor, 2);
        }
        else
            DrawPowerFlowLine(new Vector2(right, y - 34), new Vector2(right, y + 34), wire, terminalOutputFlow, monitoring, 3);
        if (rung.IsLabel) DrawNetworkLabel(rungIndex, rung);
        if (!rung.HasTerminalAction)
        {
            if (rung.HasEmptyInstructionPlaceholder)
                DrawEmptyInstructionPlaceholder(rungIndex, new Vector2(outputX, y), outputColor);
            return;
        }
        DrawTerminalObservation(rungIndex, rung);
    }

    private void DrawEmptyInstructionPlaceholder(int rungIndex, Vector2 center, Color wire)
    {
        var bounds = TerminalBounds(rungIndex);
        var selected = rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected;
        var header = new Rect2(bounds.Position, new Vector2(bounds.Size.X, 22));
        DrawRect(bounds, new Color("d9dce4"), true);
        DrawRect(header, new Color("c9ccd7"), true);
        DrawRect(bounds, selected ? new Color("005a9e") : new Color("7c8790"), false, selected ? 2.0f : 1.0f);
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(center.X, header.Position.Y + 16), "??",
            HorizontalAlignment.Center, 52, 13, new Color("e51b23"));
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 8, center.Y + 4), "IN",
            HorizontalAlignment.Left, 24, 12, new Color("25333a"));
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.End.X - 31, center.Y + 4), "OUT",
            HorizontalAlignment.Left, 30, 12, new Color("25333a"));
        if (selected)
            DrawColoredPolygon(
                [new Vector2(bounds.End.X - 13, bounds.Position.Y + 5), new Vector2(bounds.End.X - 5, bounds.Position.Y + 5), new Vector2(bounds.End.X - 9, bounds.Position.Y + 10)],
                new Color("59636a"));
    }

    private static TiaPowerFlowStatus TiaPowerFlowStatusFor(bool? flow) => flow switch
    {
        true => TiaPowerFlowStatus.Satisfied,
        false => TiaPowerFlowStatus.NotSatisfied,
        _ => TiaPowerFlowStatus.UnknownOrNotExecuted,
    };

    private Color PowerFlowColor(TiaPowerFlowStatus status, Color fallback, bool monitoring)
    {
        if (!monitoring) return fallback;
        return status switch
        {
            TiaPowerFlowStatus.Satisfied => new Color("00a651"),
            TiaPowerFlowStatus.NotSatisfied => new Color("233db8"),
            TiaPowerFlowStatus.UnknownOrNotExecuted => new Color("87949a"),
            TiaPowerFlowStatus.NotInterconnected => new Color("000000"),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
    }

    private Color PowerFlowColor(bool? flow, Color fallback, bool monitoring) =>
        PowerFlowColor(TiaPowerFlowStatusFor(flow), fallback, monitoring);

    internal (string Color, bool Dashed) ReviewPowerFlowStyle(bool? flow)
        => ReviewPowerFlowStyle(TiaPowerFlowStatusFor(flow));

    internal (string Color, bool Dashed) ReviewPowerFlowStyle(TiaPowerFlowStatus status)
    {
        var color = PowerFlowColor(status, new Color("111111"), monitoring: true);
        return (color.ToHtml(false), status == TiaPowerFlowStatus.NotSatisfied);
    }

    private void DrawPowerFlowLine(Vector2 start, Vector2 end, Color fallback, bool? flow, bool monitoring, float width)
        => DrawPowerFlowLine(start, end, fallback, TiaPowerFlowStatusFor(flow), monitoring, width);

    private void DrawPowerFlowLine(Vector2 start, Vector2 end, Color fallback, TiaPowerFlowStatus status, bool monitoring, float width)
    {
        if (!monitoring)
        {
            DrawLine(start, end, fallback, width);
            return;
        }
        var color = PowerFlowColor(status, fallback, true);
        if (status != TiaPowerFlowStatus.NotSatisfied)
        {
            DrawLine(start, end, color, width);
            return;
        }

        // TIA shows an evaluated FALSE path as blue dashed power flow.
        var delta = end - start;
        var length = delta.Length();
        if (length <= 0.1f) return;
        var direction = delta / length;
        const float dashLength = 6;
        const float pitch = 10;
        for (var position = 0.0f; position < length; position += pitch)
            DrawLine(start + direction * position, start + direction * Math.Min(position + dashLength, length), color, width);
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

    private static string NetworkLabelElementId(EditableRung rung) => string.IsNullOrWhiteSpace(rung.NetworkLabelId)
        ? $"{rung.Id}-label" : rung.NetworkLabelId;
    private static string OutputElementId(EditableRung rung) => !rung.HasTerminalAction && rung.IsLabel
        ? NetworkLabelElementId(rung)
        : !string.IsNullOrEmpty(rung.TerminalActionId) ? rung.TerminalActionId : rung.IsTimer
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
        rung.HasTerminalAction ? 78.0f : 32.0f;

    private void DrawConductor(float startX, float endX, float y, Color color, float width)
    {
        if (endX <= startX) return;
        DrawLine(new Vector2(startX, y), new Vector2(endX, y), color, width);
    }

    private void DrawSelectionOutline(Rect2 bounds, Color color, float width = 2)
    {
        var topLeft = bounds.Position;
        var topRight = new Vector2(bounds.End.X, bounds.Position.Y);
        var bottomRight = bounds.End;
        var bottomLeft = new Vector2(bounds.Position.X, bounds.End.Y);
        DrawDashedSelectionEdge(topLeft, topRight, color, width);
        DrawDashedSelectionEdge(topRight, bottomRight, color, width);
        DrawDashedSelectionEdge(bottomRight, bottomLeft, color, width);
        DrawDashedSelectionEdge(bottomLeft, topLeft, color, width);
    }

    private void DrawDashedSelectionEdge(Vector2 start, Vector2 end, Color color, float width)
    {
        var delta = end - start;
        var length = delta.Length();
        if (length <= 0.1f) return;
        var direction = delta / length;
        const float dash = 4;
        const float pitch = 7;
        for (var position = 0.0f; position < length; position += pitch)
            DrawLine(start + direction * position, start + direction * Math.Min(position + dash, length), color, width);
    }

    private void DrawContactOperand(float x, float y, string operand, float width)
    {
        width = Math.Max(1, width);
        var font = ThemeDB.FallbackFont;
        var displayLines = OperandDisplayLines(operand);
        if (ChallengeReadable)
        {
            // RefreshDocument reserves each full operand at 14 logical units.
            for (var index = 0; index < displayLines.Length; index++)
                DrawEditorString(font, new Vector2(x - width / 2, y - 27 - (displayLines.Length - 1 - index) * 14), displayLines[index],
                    HorizontalAlignment.Center, width, 14, displayLines[index] == "???" ? new Color("e11d48") : new Color("17242c"));
            DrawOperandTagInformation(operand, x - width / 2, y - 27 - displayLines.Length * 14, width, HorizontalAlignment.Center);
            return;
        }
        var lines = displayLines;
        if (lines.Length == 1 && font.GetStringSize(lines[0], HorizontalAlignment.Left, -1, 12).X > width)
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
                HorizontalAlignment.Center, width, fontSize, text == "???" ? new Color("e11d48") : new Color("17242c"));
        }
        DrawOperandTagInformation(operand, x - width / 2, y - 27 - lines.Length * 14, width, HorizontalAlignment.Center);
    }

    private void DrawEdgeMemoryOperand(float x, float y, string operand, float width)
    {
        width = Math.Max(1, width);
        var lines = OperandDisplayLines(operand);
        for (var index = 0; index < lines.Length; index++)
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - width / 2, y + index * 12), lines[index],
                HorizontalAlignment.Center, width, 10, lines[index] == "???" ? new Color("e11d48") : new Color("17242c"));
        DrawOperandTagInformation(operand, x - width / 2, y + lines.Length * 12, width, HorizontalAlignment.Center);
    }

    private string[] OperandDisplayLines(string operand)
    {
        // An unassigned pin has no symbolic or absolute representation to
        // expand. TIA shows one missing-operand placeholder in every mode.
        if (operand == "???") return ["???"];
        var presentedOperand = PresentedOperand(operand);
        if (_style != LadderVendorStyle.SiemensTia || OperandRepresentation == TiaOperandRepresentation.Symbolic)
            return [presentedOperand];
        var tag = FindTagForOperand(operand);
        // Constants are not tags and therefore have no alternate symbolic or
        // absolute address. TIA keeps the literal itself in every operand
        // representation instead of replacing a valid value with "???".
        if (tag is null && IsLiteralOperand(operand)) return [operand];
        var exactTag = tag?.Name.Equals(presentedOperand, StringComparison.Ordinal) == true;
        var address = exactTag ? tag!.Address.Trim() : string.Empty;
        if (OperandRepresentation == TiaOperandRepresentation.Absolute)
            return [address.Length > 0 ? address : "???"];
        return [presentedOperand, address.Length > 0 ? address : "???"];
    }

    private static bool IsLiteralOperand(string operand) =>
        double.TryParse(operand, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
        || LadderTimeLiteral.TryParse(operand, out _)
        || operand.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
        || operand.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
        || PlcTextValues.TryLiteral(operand, out _);

    private static string TiaComparisonOperator(LadderCompareOperator comparison) => comparison switch
    {
        LadderCompareOperator.Equal => "==",
        LadderCompareOperator.NotEqual => "<>",
        LadderCompareOperator.GreaterThan => ">",
        LadderCompareOperator.GreaterOrEqual => ">=",
        LadderCompareOperator.LessThan => "<",
        LadderCompareOperator.LessOrEqual => "<=",
        _ => "???",
    };

    private static string ComparisonValue(InstructionPresentationRow? row)
    {
        var value = row?.Value ?? "";
        var typeStart = value.LastIndexOf(" (", StringComparison.Ordinal);
        return typeStart >= 0 && value.EndsWith(')') ? value[..typeStart] : value;
    }

    private string TiaComparisonDataType(EditableLadderNode node, InstructionPresentationRow? first, InstructionPresentationRow? second)
    {
        if (node.ComparisonValueType != LadderComparisonValueType.Unspecified)
            return LadderComparisonValueTypePresentation.TiaLabel(node.ComparisonValueType);
        static string? ConsumedType(InstructionPresentationRow? row)
        {
            var value = row?.Value ?? "";
            var start = value.LastIndexOf(" (", StringComparison.Ordinal);
            return start >= 0 && value.EndsWith(')') ? value[(start + 2)..^1] : null;
        }
        static string Label(string? type) => type?.ToUpperInvariant() switch
        {
            "BOOL" => "Bool", "INT" => "Int", "DINT" => "DInt", "REAL" => "Real", "TIME" => "Time",
            "STRING" => "String", "ENUM" => "Enum", _ => "???",
        };
        var observed = Label(ConsumedType(first) ?? ConsumedType(second));
        if (observed != "???") return observed;
        var declared = FindTagForOperand(node.Variable)?.Type ?? FindTagForOperand(node.RightOperand)?.Type;
        if (declared is null && node.Variable.StartsWith("local:", StringComparison.Ordinal))
        {
            var name = node.Variable["local:".Length..];
            declared = _document.Blocks[_document.ActiveBlockIndex].Interface.FirstOrDefault(parameter => parameter.Name == name)?.Type;
        }
        return declared switch
        {
            PlcVariableType.Bool => "Bool", PlcVariableType.Int => "Int", PlcVariableType.DInt => "DInt",
            PlcVariableType.Real => "Real", PlcVariableType.Time => "Time", PlcVariableType.String => "String",
            _ => "???",
        };
    }

    private string PresentedOperand(string operand)
    {
        if (_style != LadderVendorStyle.SiemensTia) return operand;
        if (operand.StartsWith("global:", StringComparison.Ordinal))
        {
            var reference = operand["global:".Length..];
            var tag = _document.Tags
                .OrderByDescending(candidate => LadderCallBindings.GlobalId(candidate).Length)
                .FirstOrDefault(candidate =>
                {
                    var id = LadderCallBindings.GlobalId(candidate);
                    return reference.Equals(id, StringComparison.Ordinal)
                        || reference.StartsWith(id + ".", StringComparison.Ordinal)
                        || reference.StartsWith(id + "[", StringComparison.Ordinal);
                });
            if (tag is not null)
            {
                var id = LadderCallBindings.GlobalId(tag);
                return tag.Name + reference[id.Length..];
            }
        }
        if (operand.StartsWith("local:", StringComparison.Ordinal) && _document.Blocks.Count > 0)
        {
            var reference = operand["local:".Length..];
            var formal = _document.Blocks[_document.ActiveBlockIndex].Interface
                .OrderByDescending(candidate => candidate.Id.Length)
                .FirstOrDefault(candidate => reference.Equals(candidate.Id, StringComparison.Ordinal)
                    || reference.StartsWith(candidate.Id + ".", StringComparison.Ordinal)
                    || reference.StartsWith(candidate.Id + "[", StringComparison.Ordinal));
            if (formal is not null) return formal.Name + reference[formal.Id.Length..];
        }
        return operand;
    }

    private PlcVariable? FindTagForOperand(string operand)
    {
        if (operand.StartsWith("global:", StringComparison.Ordinal))
        {
            var reference = operand["global:".Length..];
            return _document.Tags
                .OrderByDescending(candidate => LadderCallBindings.GlobalId(candidate).Length)
                .FirstOrDefault(candidate =>
                {
                    var id = LadderCallBindings.GlobalId(candidate);
                    return reference.Equals(id, StringComparison.Ordinal)
                        || reference.StartsWith(id + ".", StringComparison.Ordinal)
                        || reference.StartsWith(id + "[", StringComparison.Ordinal);
                });
        }
        if (operand.StartsWith("local:", StringComparison.Ordinal) && _document.Blocks.Count > 0)
        {
            var reference = operand["local:".Length..];
            var owner = _document.Blocks[_document.ActiveBlockIndex];
            var formal = owner.Interface
                .OrderByDescending(candidate => candidate.Id.Length)
                .FirstOrDefault(candidate => reference.Equals(candidate.Id, StringComparison.Ordinal)
                    || reference.StartsWith(candidate.Id + ".", StringComparison.Ordinal)
                    || reference.StartsWith(candidate.Id + "[", StringComparison.Ordinal));
            if (formal is not null)
                return new PlcVariable(formal.Name, formal.Type,
                    formal.Section == LadderInterfaceSection.Input ? PlcVariableRole.Input : PlcVariableRole.Memory,
                    formal.InitialValue, EnumMembers: formal.EnumMembers, Id: formal.Id, Comment: formal.Comment);
        }
        var presentedOperand = PresentedOperand(operand);
        var root = presentedOperand;
        var separator = presentedOperand.IndexOfAny(['.', '[']);
        if (separator >= 0) root = presentedOperand[..separator];
        return _document.Tags.FirstOrDefault(tag => tag.Name.Equals(root, StringComparison.Ordinal));
    }

    private string TagInformationText(string operand)
    {
        var presentedOperand = PresentedOperand(operand);
        var tag = FindTagForOperand(operand);
        if (tag is null) return string.Empty;
        var parts = new List<string>();
        var exactTag = tag.Name.Equals(presentedOperand, StringComparison.Ordinal);
        if (OperandRepresentation == TiaOperandRepresentation.Symbolic && exactTag && !string.IsNullOrWhiteSpace(tag.Address))
            parts.Add(tag.Address.Trim());
        else if (OperandRepresentation == TiaOperandRepresentation.Absolute)
            parts.Add(presentedOperand);

        var comment = tag.Comment.Trim();
        if (tag.Type == PlcVariableType.Struct && tag.Aggregate?.Fields is { } fields
            && presentedOperand.StartsWith(tag.Name + ".", StringComparison.Ordinal))
        {
            var member = presentedOperand[(tag.Name.Length + 1)..];
            var field = fields.FirstOrDefault(candidate => candidate.Name.Equals(member, StringComparison.Ordinal));
            comment = field?.Comment?.Trim() ?? string.Empty;
            // Siemens documents higher-level structure comments in brackets
            // after the member's own comment. RungProof supports one-level,
            // scalar STRUCT fields, so there is exactly one possible parent.
            if (TagInformationDisplay == TiaTagInformationDisplay.HierarchicalComments
                && !string.IsNullOrWhiteSpace(tag.Comment))
                comment = comment.Length > 0 ? $"{comment} [{tag.Comment.Trim()}]" : $"[{tag.Comment.Trim()}]";
        }
        if (comment.Length > 0) parts.Add(comment);
        return string.Join(" · ", parts);
    }

    // Native acceptance checks use the same presentation functions as the
    // production draw path. These are intentionally read-only review hooks;
    // they do not create a second formatting implementation.
    internal string[] ReviewOperandDisplayLines(string operand) => OperandDisplayLines(operand);
    internal string ReviewOnOperandTagInformation(string operand) =>
        _style == LadderVendorStyle.SiemensTia
        && TagInformationDisplay != TiaTagInformationDisplay.Hidden
        && TagInformationPosition == TiaTagInformationPosition.OnOperands
            ? TagInformationText(operand) : string.Empty;
    internal (string Operand, string Info)[] ReviewBelowNetworkTagInformation(EditableRung rung) =>
        _style == LadderVendorStyle.SiemensTia
        && TagInformationDisplay != TiaTagInformationDisplay.Hidden
        && TagInformationPosition == TiaTagInformationPosition.BelowNetwork
            ? NetworkTagInformationEntries(rung) : [];

    private void DrawOperandTagInformation(string operand, float x, float y, float width, HorizontalAlignment align)
    {
        if (_style != LadderVendorStyle.SiemensTia || TagInformationDisplay == TiaTagInformationDisplay.Hidden
            || TagInformationPosition != TiaTagInformationPosition.OnOperands) return;
        var info = ReviewOnOperandTagInformation(operand);
        if (info.Length == 0) return;
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(x, y), ClipMonitorText(info, width, 9),
            align, width, 9, new Color("647780"));
    }

    private (string Operand, string Info)[] NetworkTagInformationEntries(EditableRung rung)
    {
        var operands = new List<string>();
        foreach (var node in EditableLadderTree.Visit(rung.Root).Where(node => node.IsLeaf))
        {
            if (!string.IsNullOrWhiteSpace(node.Variable)) operands.Add(node.Variable);
            if (node.Kind == LadderNodeKind.Compare && !string.IsNullOrWhiteSpace(node.RightOperand)) operands.Add(node.RightOperand);
        }
        operands.AddRange([rung.CoilVariable, rung.TimerVariable, rung.CounterVariable, rung.NumericSourceA,
            rung.NumericSourceB, rung.NumericSourceC, rung.NumericDestination]);
        if (rung.IsCall) operands.AddRange(rung.CallBindings.Where(binding => binding.Kind == LadderActualKind.Symbol).Select(binding => binding.Symbol));
        return operands.Where(operand => !string.IsNullOrWhiteSpace(operand))
            .Select(operand => (Operand: operand, Info: TagInformationText(operand)))
            .Where(entry => entry.Info.Length > 0)
            .GroupBy(entry => entry.Operand, StringComparer.Ordinal).Select(group => group.First()).ToArray();
    }

    private string NetworkTagInformationText((string Operand, string Info) entry) =>
        string.Join(" / ", OperandDisplayLines(entry.Operand)) + " — " + entry.Info;

    private void DrawNetworkTagInformation(EditableRung rung, float firstBaselineY)
    {
        var entries = NetworkTagInformationEntries(rung);
        for (var i = 0; i < entries.Length; i++)
        {
            var text = NetworkTagInformationText(entries[i]);
            // Keep the information table clear of the 48 px left power rail.
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(64, firstBaselineY + i * 15), text,
                HorizontalAlignment.Left, -1, 11, new Color("647780"));
        }
    }

    private void DrawContact(float x, float y, bool normallyClosed, Color color)
    {
        if (_style == LadderVendorStyle.SiemensTia)
        {
            // The supplied TIA captures show a compact contact: its bar
            // height is about 1.16 times the bar spacing, the wire touches both
            // bars, and the NC slash stays within their extents. At the user's
            // captured 100% scale this 26 x 30 logical geometry renders within
            // rounding of the 19 x 22 px reference.
            DrawLine(new Vector2(x - 13, y - 15), new Vector2(x - 13, y + 15), color, 3);
            DrawLine(new Vector2(x + 13, y - 15), new Vector2(x + 13, y + 15), color, 3);
            if (normallyClosed) DrawLine(new Vector2(x - 12, y + 15), new Vector2(x + 12, y - 15), color, 2);
            return;
        }
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
    private void DrawProgramControlCoil(float x, float y, Color color, string marker)
    {
        // JMP/RET need the same open-parenthesis IEC silhouette as a coil,
        // with a wider center field for the three-letter instruction name.
        DrawArc(new Vector2(x - 19, y), 20, MathF.PI / 2, MathF.PI * 1.5f, 20, color, 3);
        DrawArc(new Vector2(x + 19, y), 20, -MathF.PI / 2, MathF.PI / 2, 20, color, 3);
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - 24, y + 6), marker,
            HorizontalAlignment.Center, 48, 13, color);
    }
    private void DrawEditorString(Font font, Vector2 position, string text, HorizontalAlignment alignment,
        float width, int fontSize, Color color)
    {
        DrawString(font, position, text, alignment, width, ChallengeReadable ? Math.Max(14, fontSize) : fontSize, color);
    }

}







