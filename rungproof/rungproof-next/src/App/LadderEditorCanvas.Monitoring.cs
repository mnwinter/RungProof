using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class LadderEditorCanvas
{
    private ControllerPublication? _monitorPublication;
    private ObservationUnavailableReason _monitorUnavailableReason;
    private string? _selectedObservationContext;
    private Window? _instructionWindow;
    private OptionButton? _instructionContext;
    private TextEdit? _instructionDetails;
    private string[] _instructionContextKeys = [];
    private bool _updatingInstructionContext;
    private readonly Dictionary<string, InstructionPresentation> _presentationCache = new(StringComparer.Ordinal);
    private ControllerPublication? _pendingMonitorPublication;
    private ObservationUnavailableReason _pendingMonitorReason;
    private ulong _lastMonitorRefreshUsec;
    public long MonitorPresentationRefreshCount { get; private set; }
    public int LastDrawnInstructionCount { get; private set; }
    private Rect2 _visibleMonitorBounds;
    private long _monitorProcessTicks;
    public string MonitoringDiagnostic => $"publication={_monitorPublication?.Kind}/{_monitorPublication?.Revision} pending={_pendingMonitorPublication?.Kind}/{_pendingMonitorPublication?.Revision} processTicks={_monitorProcessTicks} processing={IsProcessing()} canProcess={CanProcess()} reason={_monitorUnavailableReason}";

    public void SetMonitorPublication(ControllerPublication? publication, ObservationUnavailableReason reason = ObservationUnavailableReason.None)
    {
        if(ReferenceEquals(publication,_monitorPublication)&&reason==_monitorUnavailableReason)return;
        var now=Time.GetTicksUsec();
        if(publication?.Kind==ControllerPublicationKind.ScanCompleted && reason==ObservationUnavailableReason.None
            && _monitorPublication is not null && IsInsideTree() && now-_lastMonitorRefreshUsec<33334)
        { _pendingMonitorPublication=publication;_pendingMonitorReason=reason;return; }
        ApplyMonitorPublication(publication,reason);
    }
    private void ApplyMonitorPublication(ControllerPublication? publication, ObservationUnavailableReason reason)
    {
        _pendingMonitorPublication=null;
        _lastMonitorRefreshUsec=Time.GetTicksUsec();MonitorPresentationRefreshCount++;
        _monitorPublication = publication;
        _monitorUnavailableReason = reason;
        _monitorSnapshot = reason == ObservationUnavailableReason.None ? publication?.Snapshot : null;
        _presentationCache.Clear();
        if (_instructionWindow?.Visible == true) RefreshInstructionInspector();
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        _monitorProcessTicks++;
        if(_pendingMonitorPublication is { } pending && Time.GetTicksUsec()-_lastMonitorRefreshUsec>=33334)
            ApplyMonitorPublication(pending,_pendingMonitorReason);
    }
    public void FlushPendingMonitoringForReview()
    {if(_pendingMonitorPublication is { } pending)ApplyMonitorPublication(pending,_pendingMonitorReason);}
    private Rect2 VisibleMonitoringBounds()
    {
        for(Node? parent=GetParent();parent is not null;parent=parent.GetParent())
            if(parent is ScrollContainer scroll)
            {
                var inverse=GetGlobalTransform().AffineInverse();var rect=scroll.GetGlobalRect();
                return new Rect2(ToLogical(inverse*rect.Position),ToLogical((inverse*rect.End)-(inverse*rect.Position)));
            }
        return new Rect2(Vector2.Zero,LogicalViewportSize);
    }
    private void BeginMonitoringDraw()
    {LastDrawnInstructionCount=0;_visibleMonitorBounds=VisibleMonitoringBounds();}
    private bool MonitorBoundsVisible(Rect2 bounds)=>bounds.Intersects(_visibleMonitorBounds);

    public InstructionPresentation InstructionPresentationFor(string id, string key)
    {
        var cacheKey = id + "|" + key;
        if (_presentationCache.TryGetValue(cacheKey, out var cached)) return cached;
        var result = _monitorUnavailableReason != ObservationUnavailableReason.None
            ? LadderInstructionPresentation.Present(id, key, _monitorPublication, unavailable: _monitorUnavailableReason)
            : _monitorPublication is null
            ? LadderInstructionPresentation.PresentSnapshot(id, key, _monitorSnapshot, _selectedObservationContext)
            : LadderInstructionPresentation.Present(id, key, _monitorPublication, selectedContext: _selectedObservationContext);
        _presentationCache[cacheKey] = result;
        return result;
    }
    private bool ObservationIsEnergized(string id)
    {
        var capture = _monitorPublication?.Observations ?? _monitorSnapshot?.Observations;
        if(capture?.Find(id).Observation is not { } global)return false;
        var view = InstructionPresentationFor(id, global.CatalogKey);
        if(!view.CurrentExecution || _monitorSnapshot?.State != VirtualControllerState.Running)return false;
        var observation = string.IsNullOrEmpty(_selectedObservationContext) ? global : capture.ContextsForInstruction(id)
            .Where(o=>!o.PathTruncated&&LadderInstructionPresentation.ContextKey(o)==_selectedObservationContext)
            .OrderByDescending(o=>o.ExecutionOrdinal).FirstOrDefault();
        return observation is { LocalResult:true } && observation.NetworkEnable != false
            && observation.WriteDisposition is not (InstructionWriteDisposition.SkippedDisabled or InstructionWriteDisposition.FailedNoWrite or InstructionWriteDisposition.FailedClearedDestination);
    }

    public void SelectObservationContext(string? key)
    {
        _selectedObservationContext = string.IsNullOrEmpty(key) ? null : key;
        _presentationCache.Clear(); RefreshInstructionInspector(); QueueRedraw();
    }

    private void InitializeMonitoringInspector()
    {
        var button = new Button { Name = "InstructionObservationButton", Text = "Instruction values…", Position = new Vector2(14, 38), Size = new Vector2(180, 30),
            TooltipText = "Inspect the selected instruction's consumed values, effects and exact task/call/instance context." };
        AddChild(button); button.Pressed += ShowInstructionInspector;
        ElementSelected += (_, _, _, _) => { if (_instructionWindow?.Visible == true) RefreshInstructionInspector(); };
        RungSelected += _ => { if (_instructionWindow?.Visible == true) RefreshInstructionInspector(); };
        KeyboardSelectionChanged += () => { if (_instructionWindow?.Visible == true) RefreshInstructionInspector(); };
    }
    public void ShowInstructionInspector()
    {
        if (_instructionWindow is null)
        {
            _instructionWindow = new Window { Name = "InstructionObservationInspector", Title = "Instruction-time values · read only", MinSize = new Vector2I(600, 360), Size = new Vector2I(900, 600), Visible = false };
            AddChild(_instructionWindow); _instructionWindow.CloseRequested += _instructionWindow.Hide;
            var body = new VBoxContainer { AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 12, OffsetTop = 12, OffsetRight = -12, OffsetBottom = -12 };
            _instructionWindow.AddChild(body);
            body.AddChild(new Label { Text = "Task / call path / instance — counts apply to this scan only" });
            _instructionContext = new OptionButton { Name = "InstructionContextSelector", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            body.AddChild(_instructionContext);
            _instructionContext.ItemSelected += selected => { if (!_updatingInstructionContext && selected >= 0 && selected < _instructionContextKeys.Length) SelectObservationContext(_instructionContextKeys[(int)selected]); };
            _instructionDetails = new TextEdit { Name = "InstructionObservationDetails", Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, SizeFlagsVertical = SizeFlags.ExpandFill };
            _instructionDetails.AddThemeFontSizeOverride("font_size", 15);
            _instructionDetails.AddThemeColorOverride("font_readonly_color", new Color("eef5f9"));
            _instructionDetails.AddThemeStyleboxOverride("read_only", new StyleBoxFlat { BgColor = new Color("15232c") });
            body.AddChild(_instructionDetails);
        }
        RefreshInstructionInspector(); _instructionWindow.PopupCenteredRatio(.8f);
    }
    private void RefreshInstructionInspector()
    {
        if (_instructionContext is null || _instructionDetails is null) return;
        if (_document.Rungs.Count == 0) { _instructionDetails.Text = "No instruction selected."; _instructionContext.Clear(); return; }
        var rung = _document.Rungs[Math.Clamp(SelectedRung, 0, _document.Rungs.Count - 1)];
        var node = EditableLadderTree.Find(rung.Root, SelectedNodeId);
        var id = OutputSelected ? OutputElementId(rung) : node?.Id ?? rung.Id;
        var key = OutputSelected ? TerminalCatalogKey(rung) : node is null ? "rung" : NodeCatalogKey(node);
        var view = InstructionPresentationFor(id, key);
        _updatingInstructionContext = true;
        _instructionContext.Clear(); _instructionContextKeys = view.Contexts.Select(c => c.Key).ToArray();
        foreach (var choice in view.Contexts) _instructionContext.AddItem(choice.Label);
        var selected = Array.IndexOf(_instructionContextKeys, _selectedObservationContext ?? "");
        if (selected >= 0) _instructionContext.Select(selected);
        // Retain a missing explicit choice visibly rather than silently changing to another instance.
        else if (_selectedObservationContext is not null)
        {
            _instructionContext.AddItem("Selected context not captured");
            _instructionContext.Select(_instructionContext.ItemCount - 1);
        }
        _instructionContext.Disabled = _instructionContext.ItemCount == 0;
        _updatingInstructionContext = false;
        _instructionDetails.Text = id + " · " + key + "\n" + view.Detail;
    }
    private static string NodeCatalogKey(EditableLadderNode node) => node.Kind switch
    {
        LadderNodeKind.Contact => node.EdgeMode == LadderEdgeMode.Rising ? "edge-rising" : node.EdgeMode == LadderEdgeMode.Falling ? "edge-falling" : node.NormallyClosed ? "nc" : "no",
        LadderNodeKind.Compare => "compare-" + (int)node.CompareOperator,
        LadderNodeKind.Parallel => "branch", _ => "rung",
    };
    private static string TerminalCatalogKey(EditableRung rung) => rung.IsTimer ? rung.TimerKind switch { LadderTimerKind.OffDelay => "tof", LadderTimerKind.Pulse => "tp", LadderTimerKind.RetentiveOnDelay => "rto", _ => "ton" }
        : rung.IsTimerReset ? "timer-reset" : rung.IsCounter ? rung.CounterKind == LadderCounterKind.CountDown ? "ctd" : "ctu"
        : rung.IsCounterReset ? "counter-reset" : rung.IsCounterLoad ? "counter-load" : rung.IsNumericOperation ? "numeric-" + (int)rung.NumericOperationKind
        : rung.IsCall ? "call" : rung.IsReturn ? "return" : rung.IsJump ? "jump" : rung.IsLabel ? "label"
        : rung.CoilMode == LadderCoilMode.Set ? "set" : rung.CoilMode == LadderCoilMode.Reset ? "reset" : "coil";

    private Rect2 TerminalBounds(int rungIndex)
    {
        var center = TreeOutputPosition(rungIndex);
        return new Rect2(center.X - 160, center.Y - 54, 320, IsCounterInstruction(_document.Rungs[rungIndex]) ? 126 : 146);
    }
    public Rect2 GetSelectionBounds()
    {
        if(_document.Rungs.Count==0)return new Rect2(Vector2.Zero,new Vector2(1,1));
        if(OutputSelected)return ToCanvas(TerminalBounds(SelectedRung));
        var placed=TreeLayout(_document.Rungs[SelectedRung]).Nodes.FirstOrDefault(n=>n.Node.Id==SelectedNodeId);
        return placed is null ? new Rect2(GetSelectionPosition(),new Vector2(1,1))
            : ToCanvas(new Rect2(TreeOrigin(SelectedRung)+new Vector2(placed.Bounds.X,placed.Bounds.Y),new Vector2(placed.Bounds.Width,placed.Bounds.Height)));
    }
    private string ClipMonitorText(string text, float width, int fontSize = 12)
    {
        var font = ThemeDB.FallbackFont;
        if (font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X <= width) return text;
        var length = text.Length;
        while (length > 0 && font.GetStringSize(text[..length] + "…", HorizontalAlignment.Left, -1, fontSize).X > width) length--;
        if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
        return text[..length] + "…";
    }
    private void DrawTerminalObservation(int rungIndex, EditableRung rung)
    {
        if(!MonitorBoundsVisible(TerminalBounds(rungIndex))||LastDrawnInstructionCount>=200)return;
        LastDrawnInstructionCount++;
        var key = TerminalCatalogKey(rung); var view = InstructionPresentationFor(OutputElementId(rung), key);
        var bounds = TerminalBounds(rungIndex);
        var border = view.Partial ? new Color("b65d16") : IsElementEnergized(OutputElementId(rung)) ? new Color("008f4c") : new Color("40616e");
        DrawRect(bounds, new Color("e4edf1")); DrawRect(bounds, border, false, 2);
        var title = InstructionMnemonic(key, rung) + " · " + (rung.IsNumericOperation ? rung.NumericDestination : rung.IsTimer || rung.IsTimerReset ? rung.TimerVariable
            : IsCounterInstruction(rung) ? rung.CounterVariable : rung.IsCall ? rung.CallTarget : rung.IsJump || rung.IsLabel ? rung.ProgramControlLabel : rung.CoilVariable);
        var rows = CompactRows(view, rung);
        const float textWidth=300;
        if (key is "coil" or "set" or "reset")
        {
            // Keep conventional coil symbols; data rows are annotations below the conductor.
            DrawRect(bounds, new Color("f5fafc"));
            var center = TreeOutputPosition(rungIndex);
            DrawConductor(bounds.Position.X, center.X - 28, center.Y, border, 2);
            DrawConductor(center.X + 28, bounds.End.X, center.Y, border, 2);
            DrawCoil(center.X, center.Y, border, key == "set" ? _style == LadderVendorStyle.SiemensTia ? "S" : "L" : key == "reset" ? _style == LadderVendorStyle.SiemensTia ? "R" : "U" : "");
            DrawEditorString(ThemeDB.FallbackFont, bounds.Position + new Vector2(10, 18), ClipMonitorText(title, textWidth, 14), HorizontalAlignment.Center, textWidth, 14, new Color("17242c"));
            DrawEditorString(ThemeDB.FallbackFont, bounds.Position + new Vector2(10, 36), ClipMonitorText(view.Status, textWidth), HorizontalAlignment.Center, textWidth, 12, border);
            var coilRows = new[] { "Network enable", "Stored after", "Effect" }.Select(label => view.Rows.FirstOrDefault(r => r.Label == label)).Where(r => r is not null).ToArray();
            for (var i = 0; i < coilRows.Length; i++) DrawEditorString(ThemeDB.FallbackFont, bounds.Position + new Vector2(10, 90 + i * 18), ClipMonitorText(coilRows[i]!.Text, textWidth), HorizontalAlignment.Left, textWidth, 12, new Color("17242c"));
            if (rungIndex == SelectedRung && OutputSelected) DrawRect(bounds.Grow(3), new Color("0087a8"), false, 3);
            return;
        }
        DrawEditorString(ThemeDB.FallbackFont, bounds.Position + new Vector2(10, 20), ClipMonitorText(title, textWidth, 14), HorizontalAlignment.Left, textWidth, 14, new Color("17242c"));
        DrawEditorString(ThemeDB.FallbackFont, bounds.Position + new Vector2(10, 40), ClipMonitorText(view.Status, textWidth), HorizontalAlignment.Left, textWidth, 12, border);
        for (var i = 0; i < rows.Length; i++)
            DrawEditorString(ThemeDB.FallbackFont, bounds.Position + new Vector2(10, 61 + i * 18), ClipMonitorText(rows[i], textWidth), HorizontalAlignment.Left, textWidth, 12, new Color("17242c"));
        if (rungIndex == SelectedRung && OutputSelected) DrawRect(bounds.Grow(3), new Color("0087a8"), false, 3);
    }
    private string InstructionMnemonic(string key, EditableRung rung)
    {
        if (rung.IsNumericOperation) return rung.NumericOperationKind switch
        {
            LadderNumericOperationKind.Move => "MOV", LadderNumericOperationKind.Add => "ADD", LadderNumericOperationKind.Subtract => "SUB",
            LadderNumericOperationKind.Multiply => "MUL", LadderNumericOperationKind.Divide => "DIV", LadderNumericOperationKind.Modulo => "MOD",
            LadderNumericOperationKind.Absolute => "ABS", LadderNumericOperationKind.Negate => "NEG", LadderNumericOperationKind.SquareRoot => "SQRT",
            LadderNumericOperationKind.Exponentiate => "EXPT", LadderNumericOperationKind.NaturalLog => "LN", LadderNumericOperationKind.Sine => "SIN",
            LadderNumericOperationKind.Cosine => "COS", LadderNumericOperationKind.Tangent => "TAN", LadderNumericOperationKind.ArcSine => "ASIN",
            LadderNumericOperationKind.ArcCosine => "ACOS", LadderNumericOperationKind.ArcTangent => "ATAN", LadderNumericOperationKind.Truncate => "TRUNC",
            LadderNumericOperationKind.Normalize => _style == LadderVendorStyle.SiemensTia ? "NORM_X" : "CPT NORM",
            LadderNumericOperationKind.Scale => _style == LadderVendorStyle.SiemensTia ? "SCALE_X" : "CPT SCALE",
            LadderNumericOperationKind.Convert => _style == LadderVendorStyle.SiemensTia ? "CONVERT" : "MOV CONV",
            LadderNumericOperationKind.Round => _style == LadderVendorStyle.SiemensTia ? "ROUND" : "CPT ROUND",
            LadderNumericOperationKind.Ceiling => _style == LadderVendorStyle.SiemensTia ? "CEIL" : "CPT CEIL",
            LadderNumericOperationKind.Floor => _style == LadderVendorStyle.SiemensTia ? "FLOOR" : "CPT FLOOR", _ => "FORMAT_TEXT",
        };
        return key switch { "timer-reset" => _style == LadderVendorStyle.SiemensTia ? "RT" : "RES", "counter-reset" => "RES", "counter-load" => "LOAD", "rto" => _style == LadderVendorStyle.SiemensTia ? "TONR" : "RTO", "coil" => _style == LadderVendorStyle.SiemensTia ? "ASSIGN" : "OTE", "set" => _style == LadderVendorStyle.SiemensTia ? "SET" : "OTL", "reset" => _style == LadderVendorStyle.SiemensTia ? "RESET" : "OTU", "call" => _style == LadderVendorStyle.SiemensTia ? "CALL" : "JSR", _ => key.ToUpperInvariant() };
    }
    private static string[] CompactRows(InstructionPresentation view, EditableRung rung)
    {
        string Row(string label)=>view.Rows.FirstOrDefault(r=>r.Label==label)?.Value??"—";
        if(IsCounterInstruction(rung))return ["CV / current: "+Row("Current count")+"  ·  configured PV: "+(Row("Configured preset")=="—" ? rung.IsCounterReset?"—":rung.CounterPreset.ToString():Row("Configured preset")),
            (rung.IsCounter?"Box input: "+Row("Box input"):"Enable: "+Row("Network enable"))+"  ·  Done: "+Row("Done"),"Instance PV: "+Row("Instance preset")+"  ·  values… for effect/context"];
        if (view.UnavailableReason != ObservationUnavailableReason.None)
            return rung.IsTimer ? ["Configured preset: " + LadderInstructionPresentation.Value(rung.TimerPreset), "Elapsed: —", "Input / timing / done: —"]
                : rung.IsCounter || rung.IsCounterLoad ? ["Configured preset: " + rung.CounterPreset, "Current count: —", "Input / done: —"]
                : rung.IsNumericOperation ? NumericConfiguredRows(rung)
                : ["No instruction-time values available", "Select instruction → Instruction values…"];
        var labels = IsCounterInstruction(rung) ? new[] { "Configured preset", "Current count", "Instance preset", "Box input", "Done", "Effect" }
            : rung.IsTimer || rung.IsTimerReset ? new[] { "Configured preset", "Elapsed", "Instance input", "Timing", "Done", "Effect" }
            : new[] { "Consumed A", "Consumed B", "Consumed C", "Consumed MIN", "Consumed VALUE", "Consumed MAX", "Calculated", "Stored after", "Control", "Network enable", "Effect" };
        return labels.Select(label => view.Rows.FirstOrDefault(r => r.Label == label)).Where(r => r is not null).Take(5).Select(r => r!.Text).ToArray();
    }
    public static string[] NumericConfiguredRows(EditableRung rung)
    {
        var ternary=LadderNumericOperationRules.RequiresSourceC(rung.NumericOperationKind);
        var rows=new List<string>{(ternary?"MIN: ":"Source A: ")+rung.NumericSourceA};
        if(LadderNumericOperationRules.RequiresSourceB(rung.NumericOperationKind))rows.Add((ternary?"VALUE: ":"Source B: ")+rung.NumericSourceB);
        if(ternary)rows.Add("MAX: "+rung.NumericSourceC);
        rows.Add("Destination: "+rung.NumericDestination);return rows.ToArray();
    }
    private void DrawConditionObservation(EditableLadderNode node, float x, float y, float width)
    {
        var view = InstructionPresentationFor(node.Id, NodeCatalogKey(node));
        var lines = view.UnavailableReason != ObservationUnavailableReason.None ? new[] { view.Status }
            : new[]{string.Join(" · ",view.Rows.Where(r=>r.Label.StartsWith("Consumed ",StringComparison.Ordinal)).Select(r=>r.Label.Replace("Consumed ","")+": "+r.Value)),
                "Local: "+view.Rows.FirstOrDefault(r=>r.Label=="Local result")?.Value+" · Enable: "+view.Rows.FirstOrDefault(r=>r.Label=="Network enable")?.Value};
        for (var i = 0; i < lines.Length; i++) DrawEditorString(ThemeDB.FallbackFont, new Vector2(x - width / 2, y + 47 + i * 16),
            ClipMonitorText(lines[i], width), HorizontalAlignment.Center, width, 12, view.Partial ? new Color("b65d16") : new Color("274b5c"));
    }
}

