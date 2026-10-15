using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class LadderEditorCanvas
{
    // A CALL actual can render three stacked text rows when TIA tag information
    // is shown on operands: comment, symbolic name, and absolute address.  A
    // 32 px pitch let the next formal's comment collide with the prior actual's
    // address at every zoom.  Keep one complete annotation stack per formal.
    private const float TiaCallRowPitch = 48.0f;

    // Keep each CALL actual on the side of its pin row that has room for the
    // complete comment/symbol/address stack.  In particular, an InOut formal
    // occupies the center rung row on both sides of the block.  Centering the
    // right-side actual on that row lets the main conductor bisect its text at
    // 150% zoom.  The lower anchor mirrors the already-safe input-side layout.
    private static float TiaCallActualTagY(float pinY, float centerY) =>
        pinY < centerY ? pinY - 41.0f : pinY + 12.0f;
    private ControllerPublication? _monitorPublication;
    private ObservationUnavailableReason _monitorUnavailableReason;
    private string? _selectedObservationContext;
    private Window? _instructionWindow;
    private OptionButton? _instructionContext;
    private TextEdit? _instructionDetails;
    private string[] _instructionContextKeys = [];
    private bool _updatingInstructionContext;
    private readonly Dictionary<string, InstructionPresentation> _presentationCache = new(StringComparer.Ordinal);
    // Siemens can retain a value from an earlier cycle while current power flow is
    // unknown. Keep that value source separate from the current publication so it
    // can never make an unexecuted instruction look energized.
    private readonly Dictionary<string, ControllerPublication> _lastObservedPublications = new(StringComparer.Ordinal);
    private (Guid SessionId, Guid LoadId, long Generation, string ProgramContentId)? _lastObservationIdentity;
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
        RetainLastObservedPublications(publication, reason);
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
        if (CanUseEarlierCycleValue(result)
            && _lastObservedPublications.TryGetValue(ObservationRetentionKey(id, _selectedObservationContext), out var retained))
        {
            var earlier = LadderInstructionPresentation.Present(id, key, retained, selectedContext: _selectedObservationContext);
            if (earlier.UnavailableReason == ObservationUnavailableReason.None)
                result = earlier with
                {
                    Status = "VALUE FROM EARLIER CYCLE — instruction not executed this scan",
                    CurrentExecution = false,
                    EarlierCycle = true
                };
        }
        _presentationCache[cacheKey] = result;
        return result;
    }

    private static string ObservationRetentionKey(string id, string? context) => id + "|" + (context ?? "");

    private void RetainLastObservedPublications(ControllerPublication? publication, ObservationUnavailableReason reason)
    {
        if (reason != ObservationUnavailableReason.None || publication is null)
        {
            _lastObservedPublications.Clear();
            _lastObservationIdentity = null;
            return;
        }
        var identity = (publication.SessionId, publication.LoadId, publication.ExecutionGeneration, publication.ProgramContentId);
        if (_lastObservationIdentity != identity || publication.Kind is ControllerPublicationKind.Loaded or ControllerPublicationKind.Reset)
            _lastObservedPublications.Clear();
        _lastObservationIdentity = identity;
        if (publication.Kind != ControllerPublicationKind.ScanCompleted
            || publication.Completion != ControllerCompletion.Completed
            || publication.Observations is not { } capture)
            return;
        foreach (var summary in capture.GlobalSummaries)
            _lastObservedPublications[ObservationRetentionKey(summary.InstructionId, null)] = publication;
        foreach (var observation in capture.Contexts.Values.Where(value => !value.PathTruncated))
            _lastObservedPublications[ObservationRetentionKey(observation.InstructionId,
                LadderInstructionPresentation.ContextKey(observation))] = publication;
    }

    private bool CanUseEarlierCycleValue(InstructionPresentation current) =>
        _style == LadderVendorStyle.SiemensTia
        && _monitorPublication is { Kind: ControllerPublicationKind.ScanCompleted, Completion: ControllerCompletion.Completed }
        && _monitorSnapshot?.State == VirtualControllerState.Running
        && current.UnavailableReason is ObservationUnavailableReason.NotEvaluatedThisScan
            or ObservationUnavailableReason.ContextNotCaptured;
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
            && observation.WriteDisposition is not (InstructionWriteDisposition.SkippedDisabled or InstructionWriteDisposition.FailedNoWrite
                or InstructionWriteDisposition.FailedClearedDestination or InstructionWriteDisposition.FailedInvalidValueWritten
                or InstructionWriteDisposition.FailedErrorValueWritten);
    }

    private bool IsTiaMonitoringActive => _style == LadderVendorStyle.SiemensTia
        && _monitorSnapshot?.State == VirtualControllerState.Running;

    private InstructionObservation? CurrentFlowObservation(string id)
    {
        if (!IsTiaMonitoringActive) return null;
        var capture = _monitorPublication?.Observations ?? _monitorSnapshot?.Observations;
        if (capture is null) return null;
        var global = capture.Find(id).Observation;
        if (global is null || !InstructionPresentationFor(id, global.CatalogKey).CurrentExecution)
            return null;
        if (string.IsNullOrEmpty(_selectedObservationContext)) return global;
        return capture.ContextsForInstruction(id)
            .Where(o => !o.PathTruncated && LadderInstructionPresentation.ContextKey(o) == _selectedObservationContext)
            .OrderByDescending(o => o.ExecutionOrdinal).FirstOrDefault();
    }

    private sealed record LadderPowerFlow(bool? Input, bool? Output);

    // Conservative three-valued OR for a monitored parallel branch. A known
    // TRUE path proves the join TRUE even when another path is unknown. A
    // known FALSE path cannot decide the join while any other path is unknown.
    private static bool? ParallelOutput(IEnumerable<bool?> outputs)
    {
        var unknown = false;
        foreach (var output in outputs)
        {
            if (output == true) return true;
            if (output is null) unknown = true;
        }
        return unknown ? null : false;
    }

    internal static bool? ReviewParallelOutput(params bool?[] outputs) => ParallelOutput(outputs);

    /// <summary>Projects Siemens online power flow from each instruction's incoming power and local result.</summary>
    private Dictionary<string, LadderPowerFlow> ProjectLadderPowerFlow(EditableLadderLayout layout)
    {
        var projected = new Dictionary<string, LadderPowerFlow>(StringComparer.Ordinal);
        LadderPowerFlow Visit(EditableLadderNode node)
        {
            if (node.IsLeaf)
            {
                var observation = CurrentFlowObservation(node.Id);
                var input = observation is { Evaluated: true } ? observation.NetworkEnable : null;
                var output = input switch
                {
                    false => false,
                    true when observation is { Evaluated: true } => observation.LocalResult,
                    _ => (bool?)null
                };
                return projected[node.Id] = new(input, output);
            }

            var children = node.Children ?? [];
            if (children.Count == 0) return projected[node.Id] = new(null, node.Kind == LadderNodeKind.Series);
            var childFlows = children.Select(Visit).ToArray();
            var groupInput = childFlows[0].Input;
            bool? groupOutput;
            if (node.Kind == LadderNodeKind.Series)
            {
                groupOutput = childFlows[^1].Output;
            }
            else groupOutput = ParallelOutput(childFlows.Select(flow => flow.Output));
            return projected[node.Id] = new(groupInput, groupOutput);
        }

        if (layout.Nodes.Count > 0) Visit(layout.Nodes[0].Node);
        return projected;
    }

    // Read-only native-review hook over the production projection. This keeps
    // true/false/unexecuted acceptance checks from reimplementing ladder flow.
    internal (bool? Input, bool? Output) ReviewPowerFlowForElement(int rungIndex, string elementId)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var flow = ProjectLadderPowerFlow(TreeLayout(_document.Rungs[rungIndex])).GetValueOrDefault(elementId)
            ?? throw new ArgumentException("Unknown ladder element.", nameof(elementId));
        return (flow.Input, flow.Output);
    }

    internal (bool? Input, bool? Output) ReviewTerminalPowerFlow(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var id = OutputElementId(_document.Rungs[rungIndex]);
        return (TerminalNetworkEnableFlow(id), TerminalOutputFlow(id));
    }

    internal string ReviewTiaTerminalInstructionId(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        return OutputElementId(_document.Rungs[rungIndex]);
    }

    private bool? TerminalNetworkEnableFlow(string id)
    {
        var observation = CurrentFlowObservation(id);
        return observation is { Evaluated: true } ? observation.NetworkEnable : null;
    }

    private bool? TerminalOutputFlow(string id)
    {
        var observation = CurrentFlowObservation(id);
        return LadderInstructionPresentation.OutputPowerFlow(observation);
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
        var id = NetworkLabelSelected ? NetworkLabelElementId(rung)
            : OutputSelected ? OutputElementId(rung) : node?.Id ?? rung.Id;
        var key = NetworkLabelSelected ? "label"
            : OutputSelected ? TerminalCatalogKey(rung) : node is null ? "rung" : NodeCatalogKey(node);
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
    private static string TerminalCatalogKey(EditableRung rung) => !rung.HasTerminalAction && rung.IsLabel ? "label" : rung.IsTimer ? rung.TimerKind switch { LadderTimerKind.OffDelay => "tof", LadderTimerKind.Pulse => "tp", LadderTimerKind.RetentiveOnDelay => "rto", _ => "ton" }
        : rung.IsTimerReset ? "timer-reset" : rung.IsCounter ? rung.CounterKind == LadderCounterKind.CountDown ? "ctd" : "ctu"
        : rung.IsCounterReset ? "counter-reset" : rung.IsCounterLoad ? "counter-load" : rung.IsNumericOperation ? "numeric-" + (int)rung.NumericOperationKind
        : rung.IsCall ? "call" : rung.IsReturn ? "return" : rung.IsJump ? "jump"
        : rung.CoilMode == LadderCoilMode.Set ? "set" : rung.CoilMode == LadderCoilMode.Reset ? "reset" : "coil";

    private Rect2 TerminalBounds(int rungIndex)
    {
        var center = TreeOutputPosition(rungIndex);
        var rung = _document.Rungs[rungIndex];
        if (_style == LadderVendorStyle.SiemensTia && rung.HasEmptyInstructionPlaceholder)
            return new Rect2(center.X - 63, center.Y - 28, 126, 56);
        // TIA counter pins are arranged around the CU/CD rung row: CU/CD is
        // on the conductor, with R and PV below it. The taller body therefore
        // frames the three input rows and the two output rows at TIA's spacing.
        if (_style == LadderVendorStyle.SiemensTia && rung.IsCounter)
            // Tag-information comments need their own vertical band above the
            // lower R/LD and PV operands. A 26 px pitch put that comment on the
            // main CU/CD conductor; the taller body supports 40 px pin rows.
            return new Rect2(center.X - 63, center.Y - 64, 126, 160);
        // Siemens' IEC timer input/output rows share the rung conductor at IN/Q.
        // PT/ET are lower rows. TONR adds an R input between IN and PT; give
        // its operand/value rows enough space to remain distinct and keep PT
        // inside the instruction body.
        if (_style == LadderVendorStyle.SiemensTia && rung.IsTimer)
        {
            // The normal IEC timers have IN/Q on the conductor row and PT/ET
            // on the lower row (+24 px). The block must extend below that row;
            // a 78 px body clipped the PT/ET pins at its bottom edge. TONR has
            // a separate R row and a 40 px input pitch, so its PT row is +80 px.
            var timerHeight = rung.TimerKind == LadderTimerKind.RetentiveOnDelay ? 158.0f : 102.0f;
            return new Rect2(center.X - 63, center.Y - 62, 126, timerHeight);
        }
        if (_style == LadderVendorStyle.SiemensTia && rung.IsNumericOperation
            && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind))
        {
            if (TiaNumericInstructionPorts.UsesLowerDataRow(rung.NumericOperationKind))
                return rung.NumericOperationKind == LadderNumericOperationKind.Convert
                    ? new Rect2(center.X - 80, center.Y - 64, 160, 128)
                    : new Rect2(center.X - 66, center.Y - 64, 132, 128);
            var inputCount = TiaNumericInstructionPorts.Inputs(rung.NumericOperationKind).Count;
            if (rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate)
                return new Rect2(center.X - 76, center.Y - 56, 152, 112);
            var numericHeight = inputCount switch { 1 => 84.0f, 2 => 96.0f, _ => 144.0f };
            return new Rect2(center.X - 66, center.Y - numericHeight / 2, 132, numericHeight);
        }
        if (_style == LadderVendorStyle.SiemensTia && rung.IsCall)
        {
            var halfWidth = CallTerminalHalfWidth(rung);
            var inputCount = CallFormals(rung, input: true).Length;
            var outputCount = CallFormals(rung, input: false).Length + 1; // ENO
            var rows = Math.Max(inputCount, outputCount);
            var callHeight = Math.Max(68.0f, 52.0f + Math.Max(0, rows - 1) * TiaCallRowPitch);
            return new Rect2(center.X - halfWidth, center.Y - callHeight / 2, halfWidth * 2, callHeight);
        }
        var height = IsCounterInstruction(rung) ? 82 : rung.IsNumericOperation ? 78 : rung.IsTimer || rung.IsTimerReset ? 88 : 68;
        return new Rect2(center.X - 66, center.Y - height / 2, 132, height);
    }

    private Rect2 TerminalSelectionBounds(int rungIndex)
    {
        var bounds = TerminalBounds(rungIndex);
        if (_style != LadderVendorStyle.SiemensTia) return bounds;
        var rung = _document.Rungs[rungIndex];
        var topExtension = rung.IsTimerReset ? 36.0f
            : TerminalCatalogKey(rung) is "coil" or "set" or "reset" ? 42.0f
            : rung.IsJump || rung.IsReturn ? 22.0f
            : 0.0f;
        return topExtension <= 0 ? bounds
            : new Rect2(bounds.Position - new Vector2(0, topExtension),
                bounds.Size + new Vector2(0, topExtension));
    }
    private Rect2 LabelBounds(int rungIndex)
    {
        var center = TreeOutputPosition(rungIndex);
        return new Rect2(50, center.Y - 72, 62, 24);
    }

    private float CallTerminalHalfWidth(EditableRung rung)
    {
        if (_style != LadderVendorStyle.SiemensTia || !rung.IsCall) return 66.0f;
        var font = ThemeDB.FallbackFont;
        var widest = CallFormals(rung, input: true).Concat(CallFormals(rung, input: false))
            .Select(parameter => font.GetStringSize(parameter.Name, HorizontalAlignment.Left, -1, 11).X)
            .DefaultIfEmpty(0).Max();
        return Math.Clamp(widest + 20.0f, 66.0f, 150.0f);
    }

    private LadderInterfaceParameter[] CallFormals(EditableRung rung, bool input)
    {
        var target = _document.Blocks.FirstOrDefault(block => block.Id == rung.CallTarget);
        return (target?.Interface ?? [])
            .Where(parameter => input
                ? parameter.Section is LadderInterfaceSection.Input or LadderInterfaceSection.InOut
                : parameter.Section is LadderInterfaceSection.Output or LadderInterfaceSection.InOut)
            .Where(parameter => rung.CallBindings.Any(binding => binding.ParameterId == parameter.Id))
            .ToArray();
    }
    public Rect2 GetSelectionBounds()
    {
        if(_document.Rungs.Count==0)return new Rect2(Vector2.Zero,new Vector2(1,1));
        if(OutputSelected)return ToCanvas(NetworkLabelSelected
            ? LabelBounds(SelectedRung) : TerminalSelectionBounds(SelectedRung));
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
        var centerY = TreeOutputPosition(rungIndex).Y;
        var monitored = _monitorSnapshot?.State == VirtualControllerState.Running;
        var monitoring = IsTiaMonitoringActive;
        var energized = IsElementEnergized(OutputElementId(rung));
        var border = view.Partial ? new Color("b65d16")
            : _style == LadderVendorStyle.SiemensTia ? new Color("87949a")
            : monitored ? new Color("00a651") : new Color("87949a");
        var activeFill = _style == LadderVendorStyle.SiemensTia ? new Color("dfe2e8")
            : energized ? new Color("d8f4df") : new Color("e4e7eb");
        var callTarget = rung.IsCall ? _document.Blocks.FirstOrDefault(block => block.Id == rung.CallTarget) : null;
        // In Siemens LAD a code-block call is represented by its selected FC/FB
        // name. The generic CALL mnemonic belongs to legacy S7-300/400 LAD and
        // is not a valid substitute for an unresolved S7-1500 block call.
        var mnemonic = rung.IsCall
            ? callTarget?.Name ?? (_style == LadderVendorStyle.SiemensTia ? "???" : InstructionMnemonic(key, rung))
            : InstructionMnemonic(key, rung);
        var target = rung.IsNumericOperation ? rung.NumericDestination : rung.IsTimer || rung.IsTimerReset ? rung.TimerVariable
            : IsCounterInstruction(rung) ? rung.CounterVariable : rung.IsCall ? rung.CallTarget
            : rung.IsJump || !rung.HasTerminalAction && rung.IsLabel ? rung.ProgramControlLabel : rung.CoilVariable;
        var instanceDb = rung.IsTimer || rung.IsTimerReset ? rung.TimerInstanceDataBlock
            : IsCounterInstruction(rung) ? rung.CounterInstanceDataBlock
            : rung.IsCall && _document.Blocks.FirstOrDefault(block => block.Id == rung.CallTarget)?.BlockType == LadderBlockType.FunctionBlock
                ? _document.Instances.FirstOrDefault(instance => instance.Id == rung.CallInstanceId)?.InstanceDataBlock
                : null;
        var tiaTimerReset = _style == LadderVendorStyle.SiemensTia && rung.IsTimerReset;
        var tiaProgramControlCoil = _style == LadderVendorStyle.SiemensTia && (rung.IsJump || rung.IsReturn);
        if (key is "coil" or "set" or "reset" || tiaTimerReset)
        {
            // Assignment/set/reset coils and Siemens RT are ladder symbols,
            // not boxed instruction blocks. RT resets the assigned IEC timer
            // while passing rung logic through, so its visual form is a coil.
            // Only show the cyan hit outline while the coil is selected.
            var center = TreeOutputPosition(rungIndex);
            var terminalFlow = TerminalOutputFlow(OutputElementId(rung));
            var tiaMonitoredCoil = _style == LadderVendorStyle.SiemensTia && monitoring;
            var coilColor = tiaTimerReset || tiaMonitoredCoil
                ? PowerFlowColor(terminalFlow, new Color("203844"), monitoring)
                : energized ? (_style == LadderVendorStyle.SiemensTia ? new Color("00a651") : new Color("27a745"))
                    : _style == LadderVendorStyle.SiemensTia ? new Color("203844") : new Color("242d69");
            if (tiaTimerReset)
            {
                DrawPowerFlowLine(new Vector2(bounds.Position.X, center.Y), new Vector2(center.X - 28, center.Y),
                    new Color("203844"), TerminalOutputFlow(OutputElementId(rung)), monitoring, 2);
                DrawPowerFlowLine(new Vector2(center.X + 28, center.Y), new Vector2(bounds.End.X, center.Y),
                    new Color("203844"), TerminalOutputFlow(OutputElementId(rung)), monitoring, 2);
                DrawCoil(center.X, center.Y, coilColor, "RT");
                if (instanceDb is not null)
                {
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(center.X - 90, bounds.Position.Y - 25),
                        $"%DB{instanceDb.Number}", HorizontalAlignment.Center, 180, 13, new Color("008e91"));
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(center.X - 112, bounds.Position.Y - 9),
                        $"\"{instanceDb.Name}\"", HorizontalAlignment.Center, 224, 12, new Color("17242c"));
                }
                else
                {
                    DrawEditorString(ThemeDB.FallbackFont, new Vector2(center.X - 42, bounds.Position.Y - 9),
                        "???", HorizontalAlignment.Center, 84, 12, new Color("c0374c"));
                }
            }
            else
            {
                if (tiaMonitoredCoil)
                {
                    DrawPowerFlowLine(new Vector2(bounds.Position.X, center.Y), new Vector2(center.X - 28, center.Y),
                        new Color("203844"), terminalFlow, monitoring, 2);
                    DrawPowerFlowLine(new Vector2(center.X + 28, center.Y), new Vector2(bounds.End.X, center.Y),
                        new Color("203844"), terminalFlow, monitoring, 2);
                }
                else
                {
                    DrawConductor(bounds.Position.X, center.X - 28, center.Y, coilColor, 2);
                    DrawConductor(center.X + 28, bounds.End.X, center.Y, coilColor, 2);
                }
                DrawCoil(center.X, center.Y, coilColor, key == "set" ? _style == LadderVendorStyle.SiemensTia ? "S" : "L" : key == "reset" ? _style == LadderVendorStyle.SiemensTia ? "R" : "U" : "");
                DrawExternalTag(target, ObservationValue(view, "Stored after", "Final snapshot"), center.X, center.Y - 42, coilColor, 190,
                    earlierCycle: view.EarlierCycle);
            }
            if (rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected)
            {
                if (_style == LadderVendorStyle.SiemensTia)
                {
                    // The output operand is part of the selected coil. Include
                    // its comment/symbol/address stack so the dashed top edge
                    // never crosses the operand text.
                    DrawSelectionOutline(TerminalSelectionBounds(rungIndex).Grow(3), new Color("2448bd"), 2);
                }
                else DrawRect(bounds.Grow(3), new Color("0087a8"), false, 3);
            }
            return;
        }
        if (tiaProgramControlCoil)
        {
            // Siemens renders JMP and RET as coil-form terminal instructions,
            // not generic EN/ENO boxes. Their monitored state is the incoming
            // RLO: TRUE takes the transfer and FALSE falls through. RET's
            // selected RLO/TRUE/FALSE/BOOL value is shown above the coil and
            // becomes the completed caller's ENO.
            var center = TreeOutputPosition(rungIndex);
            var terminalFlow = TerminalOutputFlow(OutputElementId(rung));
            var conductor = new Color("203844");
            var symbolColor = PowerFlowColor(terminalFlow, conductor, monitoring);
            const float halfSymbolWidth = 38.0f;
            DrawPowerFlowLine(new Vector2(bounds.Position.X, center.Y),
                new Vector2(center.X - halfSymbolWidth, center.Y), conductor, terminalFlow, monitoring, 2);
            DrawPowerFlowLine(new Vector2(center.X + halfSymbolWidth, center.Y),
                new Vector2(bounds.End.X, center.Y), conductor, terminalFlow, monitoring, 2);
            DrawProgramControlCoil(center.X, center.Y, symbolColor, rung.IsJump ? "JMP" : "RET");
            if (rung.IsJump)
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(center.X - 90, center.Y - 39),
                    string.IsNullOrWhiteSpace(rung.ProgramControlLabel) ? "???" : rung.ProgramControlLabel,
                    HorizontalAlignment.Center, 180, 13,
                    string.IsNullOrWhiteSpace(rung.ProgramControlLabel) ? new Color("c0374c") : new Color("17242c"));
            else
            {
                var returnText = rung.ReturnValueKind switch
                {
                    LadderReturnValueKind.True => "TRUE",
                    LadderReturnValueKind.False => "FALSE",
                    LadderReturnValueKind.Operand => string.IsNullOrWhiteSpace(rung.ReturnValueOperand)
                        ? "???" : rung.ReturnValueOperand,
                    _ => "RLO",
                };
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(center.X - 90, center.Y - 39),
                    returnText, HorizontalAlignment.Center, 180, 13,
                    returnText == "???" ? new Color("c0374c") : new Color("17242c"));
                // Siemens exposes this selector with a small yellow triangle.
                var triangleCenter = new Vector2(center.X + 47, center.Y - 33);
                var triangleColor = new Color("d5a400");
                DrawLine(triangleCenter + new Vector2(-4, -2), triangleCenter + new Vector2(4, -2), triangleColor, 2);
                DrawLine(triangleCenter + new Vector2(4, -2), triangleCenter + new Vector2(0, 3), triangleColor, 2);
                DrawLine(triangleCenter + new Vector2(0, 3), triangleCenter + new Vector2(-4, -2), triangleColor, 2);
            }
            if (rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected)
            {
                // Include the field above JMP/RET in the selected object so
                // the dashed outline does not run through its text.
                DrawSelectionOutline(TerminalSelectionBounds(rungIndex).Grow(3), new Color("2448bd"), 2);
            }
            return;
        }
        DrawRect(bounds, activeFill); DrawRect(bounds, border, false,
            _style == LadderVendorStyle.SiemensTia ? 1 : monitored || energized ? 3 : 2);
        if (_style == LadderVendorStyle.SiemensTia && instanceDb is not null && !rung.IsTimerReset && !rung.IsCounterReset)
        {
            var dbCenterX = bounds.Position.X + bounds.Size.X / 2;
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(dbCenterX - 90, bounds.Position.Y - 25),
                $"%DB{instanceDb.Number}", HorizontalAlignment.Center, 180, 13, new Color("008e91"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(dbCenterX - 112, bounds.Position.Y - 9),
                $"\"{instanceDb.Name}\"", HorizontalAlignment.Center, 224, 12, new Color("17242c"));
        }
        var tiaCounter = _style == LadderVendorStyle.SiemensTia && IsCounterInstruction(rung);
        var tiaTimer = _style == LadderVendorStyle.SiemensTia && rung.IsTimer;
        var tiaNumeric = _style == LadderVendorStyle.SiemensTia && rung.IsNumericOperation
            && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind);
        // In TIA, IEC timer/counter members live in the assigned instance DB.
        // Show the Q/CV/ET pin labels and online values at the block pins; do
        // not invent external tags such as `counter.CV` beside the rail.
        var tiaInstanceMembers = _style == LadderVendorStyle.SiemensTia
            && (rung.IsCounter || rung.IsTimer);
        var tiaCall = _style == LadderVendorStyle.SiemensTia && rung.IsCall;
        DrawEditorString(ThemeDB.FallbackFont,
            tiaCounter ? new Vector2(bounds.Position.X + 3, bounds.Position.Y + 16) : new Vector2(bounds.Position.X + 4, bounds.Position.Y + 17),
            ClipMonitorText(mnemonic, bounds.Size.X - 8, 14), HorizontalAlignment.Center, bounds.Size.X - 8, 14, new Color("17242c"));
        if (tiaCounter && !(rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected))
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 43, bounds.Position.Y + 39),
                LadderCounterValueTypePresentation.TiaLabel(rung.CounterValueType), HorizontalAlignment.Center, 76, 12, new Color("17242c"));
        if (tiaTimer)
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 32, centerY - 24),
                "Time", HorizontalAlignment.Center, 62, 12, new Color("17242c"));
        if (tiaNumeric && LadderNumericOperationRules.HasTiaDualDataTypeSelectors(rung.NumericOperationKind))
        {
            if (!(rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected && _tiaConvertInputTypeSelector?.Visible == true))
            {
                var inputOperand = rung.NumericOperationKind == LadderNumericOperationKind.Scale
                    ? rung.NumericSourceB : rung.NumericSourceA;
                var inferredInputType = rung.NumericOperationKind == LadderNumericOperationKind.Scale
                    ? LadderNumericDataType.Real
                    : LadderNumericDataTypePresentation.FromVariableType(FindTagForOperand(inputOperand)?.Type);
                var inferredOutputType = rung.NumericOperationKind == LadderNumericOperationKind.Normalize
                    ? LadderNumericDataType.Real
                    : LadderNumericDataTypePresentation.FromVariableType(FindTagForOperand(rung.NumericDestination)?.Type);
                var inputType = rung.NumericInputDataType == LadderNumericDataType.Unspecified
                    ? inferredInputType : rung.NumericInputDataType;
                var outputType = rung.NumericOutputDataType == LadderNumericDataType.Unspecified
                    ? inferredOutputType : rung.NumericOutputDataType;
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 4, bounds.Position.Y + 32),
                    LadderNumericDataTypePresentation.TiaLabel(inputType), HorizontalAlignment.Center, 48, 12, new Color("17242c"));
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.End.X - 52, bounds.Position.Y + 32),
                    LadderNumericDataTypePresentation.TiaLabel(outputType), HorizontalAlignment.Center, 48, 12, new Color("17242c"));
            }
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 52, bounds.Position.Y + 31),
                "to", HorizontalAlignment.Center, bounds.Size.X - 104, 11, new Color("17242c"));
        }
        else if (tiaNumeric && rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate)
        {
            var inferredExponentType = LadderNumericDataTypePresentation.InferredTypeFor(
                rung.NumericOperationKind, FindTagForOperand(rung.NumericSourceB)?.Type);
            var exponentType = rung.NumericDataType == LadderNumericDataType.Unspecified
                ? inferredExponentType : rung.NumericDataType;
            var chipY = bounds.Position.Y + 21;
            var leftChip = new Rect2(bounds.Position.X + 9, chipY, 45, 18);
            var rightChip = new Rect2(bounds.End.X - 82, chipY, 74, 18);
            DrawRect(leftChip, new Color("d1d4d8"));
            DrawRect(leftChip, new Color("87949a"), false, 1);
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(leftChip.Position.X, leftChip.Position.Y + 13),
                "Real", HorizontalAlignment.Center, leftChip.Size.X, 11, new Color("17242c"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 54, chipY + 13),
                "**", HorizontalAlignment.Center, bounds.Size.X - 136, 11, new Color("17242c"));
            var selectorOwnsExponent = rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected
                && _tiaNumericTypeSelector?.Visible == true;
            if (!selectorOwnsExponent)
            {
                DrawRect(rightChip, new Color("d1d4d8"));
                DrawRect(rightChip, new Color("87949a"), false, 1);
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(rightChip.Position.X, rightChip.Position.Y + 13),
                    LadderNumericDataTypePresentation.TiaLabel(exponentType, inferredExponentType),
                    HorizontalAlignment.Center, rightChip.Size.X, 11, new Color("17242c"));
            }
        }
        else if (tiaNumeric && LadderNumericOperationRules.HasTiaDataTypeSelector(rung.NumericOperationKind)
            && !(rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected && _tiaNumericTypeSelector?.Visible == true))
        {
            var selectedType = rung.NumericDataType;
            var inferredType = LadderNumericDataTypePresentation.InferredTypeFor(rung.NumericOperationKind,
                FindTagForOperand(rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate
                    ? rung.NumericSourceB : rung.NumericDestination)?.Type);
            if (selectedType == LadderNumericDataType.Unspecified && inferredType != LadderNumericDataType.Unspecified)
                selectedType = inferredType;
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 20, bounds.Position.Y + 31),
                LadderNumericDataTypePresentation.TiaLabel(selectedType, inferredType), HorizontalAlignment.Center,
                bounds.Size.X - 40, 12, new Color("17242c"));
        }
        var inputPins = TerminalInputPins(rung, key, view);
        var outputPins = TerminalOutputPins(rung, key, view);
        // Keep output pins far enough apart for their online value chips while
        // retaining a readable vertical stack inside the instruction body.
        // TIA keeps each IEC pin/value row on its own horizontal band. A
        // 20 px pitch let the second output's value chip overlap the rung's
        // center conductor; 24 px matches the reference spacing and clears it.
        // TONR input operand and value lines need a separate wider pitch because
        // its extra R pin would otherwise overlap the PT operand annotation.
        var rowStep = tiaCounter ? 40.0f : tiaCall ? TiaCallRowPitch : tiaTimer ? 24.0f
            : tiaNumeric ? TiaNumericInstructionPorts.InputRowPitch(rung.NumericOperationKind)
            : 20.0f;
        var timerInputRowStep = tiaTimer && rung.TimerKind == LadderTimerKind.RetentiveOnDelay ? 40.0f : rowStep;
        var firstY = tiaCounter || tiaTimer ? centerY : tiaCall ? centerY - Math.Max(0, inputPins.Length - 1) * rowStep / 2
            : tiaNumeric ? centerY + (TiaNumericInstructionPorts.UsesLowerDataRow(rung.NumericOperationKind) ? 40 : 12)
            : bounds.Position.Y + 34;
        if (tiaNumeric)
        {
            // Siemens numeric boxes have EN/ENO on the rung row and their
            // value operands below it. ENO drops when the evaluated operation
            // reports a controller diagnostic.
            var enableFlow = NumericInstructionEnableFlow(rung, output: false);
            var enoFlow = NumericInstructionEnableFlow(rung, output: true);
            var tiaConductor = new Color("203844");
            var enableColor = PowerFlowColor(enableFlow, tiaConductor, monitoring);
            var enoColor = PowerFlowColor(enoFlow, tiaConductor, monitoring);
            DrawPowerFlowLine(new Vector2(bounds.Position.X, centerY), new Vector2(bounds.Position.X + 25, centerY), tiaConductor,
                enableFlow, monitoring, 2);
            DrawPowerFlowLine(new Vector2(bounds.Position.X + 25, centerY), new Vector2(bounds.End.X - 32, centerY), tiaConductor,
                enoFlow, monitoring, 2);
            DrawPowerFlowLine(new Vector2(bounds.End.X - 32, centerY), new Vector2(bounds.End.X, centerY), tiaConductor,
                enoFlow, monitoring, 2);
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 5, centerY - 2), "EN",
                HorizontalAlignment.Left, 21, 11, enableColor);
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.End.X - 31, centerY - 2), "ENO",
                HorizontalAlignment.Right, 27, 11, enoColor);
        }
        for (var i = 0; i < inputPins.Length; i++)
        {
            var py = firstY + i * timerInputRowStep;
            var pinLabelWidth = tiaCall ? bounds.Size.X / 2 - 15 : 40.0f;
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(bounds.Position.X + 5, py), inputPins[i].Pin,
                tiaCall ? HorizontalAlignment.Right : HorizontalAlignment.Left, pinLabelWidth, 11, new Color("34434a"));
            DrawLine(new Vector2(bounds.Position.X, py - 4), new Vector2(bounds.Position.X + 5, py - 4), border, 2);
            var tagY = tiaTimer && i > 0 && !string.IsNullOrWhiteSpace(inputPins[i].Value) ? py + 1
                : tiaTimer ? py - 10
                : inputPins[i].Name == "???" ? py - 10
                : tiaCounter && !string.IsNullOrWhiteSpace(inputPins[i].Value) ? py + 5
                : tiaCounter ? py - 10
                : tiaNumeric ? py - (string.IsNullOrWhiteSpace(inputPins[i].Value) ? 10 : 14)
                : tiaCall ? TiaCallActualTagY(py, centerY)
                : py < centerY ? py - 25 : py + 12;
            var inputValueX = bounds.Position.X - (tiaTimer ? 22 : 12);
            // Keep an unassigned PT marker clear of its orange terminal lead,
            // as in TIA. The marker remains pinned to the PT row while the
            // conductor starts closer to the instruction body.
            var timerInputWireStartX = tiaTimer && i > 0 && inputPins[i].Pin == "PT" && inputPins[i].Name == "???"
                ? inputValueX + 12
                : inputValueX;
            if (tiaCounter && !string.IsNullOrWhiteSpace(inputPins[i].Value))
                DrawConductor(inputValueX, bounds.Position.X, py - 4, border, 2);
            // Siemens shows a short terminal lead from each lower IEC timer
            // operand (R/PT) into its pin. Keep IN on the rung conductor and
            // keep the operand text/chip outside the body.
            if (tiaTimer && i > 0
                && (!string.IsNullOrWhiteSpace(inputPins[i].Name) || !string.IsNullOrWhiteSpace(inputPins[i].Value)))
            {
                var timerInputColor = inputPins[i].Name == "???" ? new Color("e39a27") : border;
                DrawConductor(timerInputWireStartX, bounds.Position.X, py - 4, timerInputColor, 2);
            }
            if (tiaNumeric && (!string.IsNullOrWhiteSpace(inputPins[i].Name) || !string.IsNullOrWhiteSpace(inputPins[i].Value)))
                DrawConductor(inputValueX, bounds.Position.X, py - 4, new Color("203844"), 2);
            var separateOperandLabel = tiaCounter
                && !string.IsNullOrWhiteSpace(inputPins[i].Name)
                && !string.IsNullOrWhiteSpace(inputPins[i].Value);
            DrawExternalTag(inputPins[i].Name, inputPins[i].Value, inputValueX, tagY, border, 180,
                HorizontalAlignment.Right, separateOperandLabel ? 72 : 0, view.EarlierCycle);
        }
        for (var i = 0; i < outputPins.Length; i++)
        {
            var py = tiaCounter || tiaTimer ? centerY + i * rowStep
                : tiaCall ? centerY - Math.Max(0, outputPins.Length - 1) * rowStep / 2 + i * rowStep
                // The reviewed Siemens V21 binary-arithmetic examples place
                // OUT on the IN2 row, not the IN1 row.
                : tiaNumeric && rung.NumericOperationKind is LadderNumericOperationKind.Add
                    or LadderNumericOperationKind.Subtract or LadderNumericOperationKind.Multiply
                    or LadderNumericOperationKind.Divide or LadderNumericOperationKind.Modulo
                    ? firstY + rowStep
                : firstY + i * rowStep;
            var outputPinLabelWidth = tiaCall ? bounds.Size.X / 2 - 15 : 40.0f;
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(tiaCall ? bounds.Position.X + bounds.Size.X / 2 + 5 : bounds.End.X - 45, py), outputPins[i].Pin,
                HorizontalAlignment.Right, outputPinLabelWidth, 11, new Color("34434a"));
            DrawLine(new Vector2(bounds.End.X - 5, py - 4), new Vector2(bounds.End.X, py - 4), border, 2);
            var tagBaseline = py;
            // Keep online value chips clear of the rung conductor. The first
            // IEC output (Q) shares the rung row; centering its chip on that
            // row paints over the wire and makes a false visual break.
            // Lower IEC outputs (CV/ET) have their own TIA pin row, so align
            // their value text to that row instead of lifting it above the pin.
            var tagY = tiaCall ? TiaCallActualTagY(py, centerY)
                : tiaInstanceMembers && i > 0 && !string.IsNullOrWhiteSpace(outputPins[i].Value)
                ? tagBaseline + 1
                : tiaInstanceMembers && !string.IsNullOrWhiteSpace(outputPins[i].Value)
                    ? tagBaseline - 10
                : tagBaseline - (string.IsNullOrWhiteSpace(outputPins[i].Value) ? 10 : 14);
            var outputValueWidth = tiaInstanceMembers ? TerminalOutputValueWidth(rung) : 205;
            if (tiaNumeric && (!string.IsNullOrWhiteSpace(outputPins[i].Name) || !string.IsNullOrWhiteSpace(outputPins[i].Value)))
                DrawConductor(bounds.End.X, bounds.End.X + 8, py - 4, new Color("203844"), 2);
            // IEC Q/CV/ET are instruction-member pins. The symbolic
            // `instance.member` strings in TerminalOutputPins are internal
            // identifiers for observation lookup, not external LAD operands;
            // TIA shows the pin name and monitored value at the pin itself.
            // Tag-information mode controls tag annotations, not synthetic
            // labels for these instruction outputs.
            DrawExternalTag(tiaInstanceMembers ? string.Empty : outputPins[i].Name,
                outputPins[i].Value, bounds.End.X + 8, tagY, border, outputValueWidth,
                earlierCycle: view.EarlierCycle);
        }
        if (rungIndex == SelectedRung && OutputSelected && !NetworkLabelSelected)
        {
            if (_style == LadderVendorStyle.SiemensTia)
                DrawSelectionOutline(TerminalSelectionBounds(rungIndex).Grow(3), new Color("2448bd"), 2);
            else DrawRect(bounds.Grow(3), new Color("3478c8"), false, 2);
        }
    }

    private void DrawNetworkLabel(int rungIndex, EditableRung rung)
    {
        if (_style != LadderVendorStyle.SiemensTia) return;
        var bounds = LabelBounds(rungIndex);
        // LABEL identifies a destination network but is not part of that
        // network's power-flow conductor.  Do not color the marker from the
        // destination logic's RLO: its topology is explicitly not
        // interconnected, while the contact/terminal conductors below it keep
        // their independent satisfied/not-satisfied/unknown status.
        var conductor = PowerFlowColor(TiaPowerFlowStatus.NotInterconnected,
            new Color("203844"), IsTiaMonitoringActive);
        DrawRect(bounds, new Color("ffffff"), true);
        DrawRect(bounds, conductor, false, 1);
        DrawLine(new Vector2(bounds.Position.X + 2, bounds.End.Y - 4),
            new Vector2(bounds.End.X - 2, bounds.End.Y - 4), conductor, 1);
        var name = string.IsNullOrWhiteSpace(rung.NetworkLabelName)
            ? rung.ProgramControlLabel : rung.NetworkLabelName;
        DrawEditorString(ThemeDB.FallbackFont,
            new Vector2(bounds.Position.X + 4, bounds.Position.Y + 17),
            string.IsNullOrWhiteSpace(name) ? "???" : name,
            HorizontalAlignment.Center, bounds.Size.X - 8, 13,
            string.IsNullOrWhiteSpace(name) ? new Color("c0374c") : new Color("17242c"));
        if (rungIndex == SelectedRung && NetworkLabelSelected)
            DrawSelectionOutline(bounds.Grow(3), new Color("2448bd"), 2);
    }

    internal (string Color, bool Dashed) ReviewNetworkLabelPowerFlowStyle(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        if (!_document.Rungs[rungIndex].IsLabel)
            throw new ArgumentException("Rung does not declare a network LABEL.", nameof(rungIndex));
        return ReviewPowerFlowStyle(TiaPowerFlowStatus.NotInterconnected);
    }

    private static string ObservationValue(InstructionPresentation view, params string[] labels)
    {
        foreach (var label in labels)
            if (view.Rows.FirstOrDefault(row => row.Label == label) is { } row) return row.Value;
        return "";
    }

    private string TiaTimerValue(string value)
    {
        if (_style != LadderVendorStyle.SiemensTia || !value.EndsWith(" ms", StringComparison.Ordinal)) return value;
        var millisecondsText = value[..^3];
        return long.TryParse(millisecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds is >= 0 and <= int.MaxValue
                ? LadderTimeLiteral.Format(TimeSpan.FromMilliseconds(milliseconds))
                : value;
    }

    private (string Pin, string Name, string Value)[] TerminalInputPins(EditableRung rung, string key, InstructionPresentation view)
    {
        if (rung.IsCounter && rung.CounterKind == LadderCounterKind.CountDown)
            return [("CD", "", ""), ("LD", rung.CounterLoadInput, ValueFor(view, "Consumed LD")), ("PV", "", rung.CounterPreset.ToString())];
        if (rung.IsCounter)
        {
            var resetOperand = rung.CounterResetInput;
            var resetValue = ValueFor(view, "Consumed R");
            var renderedResetValue = TiaMonitorValueFormatter.Format(resetValue, tiaPresentation: true);
            // In TIA monitoring, a literal BOOL at R is already represented
            // by its live value at the pin. Drawing the same FALSE/TRUE again
            // as a cyan operand label duplicates what the TIA reference shows
            // once. Keep both name and value for symbolic reset tags.
            if (_style == LadderVendorStyle.SiemensTia && view.UnavailableReason == ObservationUnavailableReason.None
                && bool.TryParse(resetOperand, out _) && resetOperand.Equals(renderedResetValue, StringComparison.OrdinalIgnoreCase))
                resetOperand = "";
            return [("CU", "", ""), ("R", resetOperand, resetValue), ("PV", "", rung.CounterPreset.ToString())];
        }
        if (rung.IsCounterReset) return [("R", "", "")];
        if (rung.IsCounterLoad) return [("PV", "", rung.CounterPreset.ToString())];
        if (rung.IsTimer)
        {
            var pins = new List<(string Pin, string Name, string Value)> { ("IN", "", "") };
            if (rung.TimerKind == LadderTimerKind.RetentiveOnDelay)
                pins.Add(("R", string.IsNullOrWhiteSpace(rung.TimerResetOperand) ? "???" : rung.TimerResetOperand,
                    ValueFor(view, "Consumed R")));
            if (!rung.TimerPresetAssigned)
                pins.Add(("PT", "???", ""));
            else
            {
                var isLiteral = LadderTimeLiteral.TryParse(rung.TimerPresetOperand, out var preset);
                var livePresetText = view.UnavailableReason == ObservationUnavailableReason.None
                    ? view.Rows.FirstOrDefault(row => row.Label == "Instance preset")?.Value ?? ""
                    : "";
                var livePreset = !isLiteral && livePresetText.EndsWith(" ms", StringComparison.Ordinal)
                    && long.TryParse(livePresetText[..^3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var liveMilliseconds)
                    && liveMilliseconds is >= 0 and <= int.MaxValue
                        ? TimeSpan.FromMilliseconds(liveMilliseconds)
                        : isLiteral ? preset : rung.TimerPreset;
                pins.Add(("PT", isLiteral ? "" : rung.TimerPresetOperand, TiaTimerValue(LadderTimeLiteral.Format(livePreset))));
            }
            return pins.ToArray();
        }
        if (rung.IsTimerReset) return [("IN", "", "")];
        if (rung.IsNumericOperation)
        {
            if (_style == LadderVendorStyle.SiemensTia && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind))
            {
                var names = TiaNumericInstructionPorts.Inputs(rung.NumericOperationKind);
                var sources = new[] { rung.NumericSourceA, rung.NumericSourceB, rung.NumericSourceC };
                var values = rung.NumericOperationKind is LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale
                    ? new[] { ValueFor(view, "Consumed MIN"), ValueFor(view, "Consumed VALUE"), ValueFor(view, "Consumed MAX") }
                    : new[] { ValueFor(view, "Consumed A"), ValueFor(view, "Consumed B"), ValueFor(view, "Consumed C") };
                return names.Select((name, index) => (name, sources[index], values[index])).ToArray();
            }
            var ternary = LadderNumericOperationRules.RequiresSourceC(rung.NumericOperationKind);
            var pins = new List<(string Pin, string Name, string Value)> { (ternary ? "MIN" : "IN", rung.NumericSourceA, ValueFor(view, "Consumed " + (ternary ? "MIN" : "A"))) };
            if (LadderNumericOperationRules.RequiresSourceB(rung.NumericOperationKind)) pins.Add((ternary ? "VALUE" : "IN2", rung.NumericSourceB, ValueFor(view, "Consumed " + (ternary ? "VALUE" : "B"))));
            if (ternary) pins.Add(("MAX", rung.NumericSourceC, ValueFor(view, "Consumed MAX")));
            return pins.ToArray();
        }
        if (rung.IsCall)
            return CallFormals(rung, input: true).Select(formal =>
            {
                var binding = rung.CallBindings.First(item => item.ParameterId == formal.Id);
                var actual = binding.Kind == LadderActualKind.Symbol ? binding.Symbol : binding.Literal?.ToString() ?? "";
                var phase = formal.Section == LadderInterfaceSection.InOut ? "AliasResolved:" : "CopyIn:";
                return (formal.Name, actual, ValueFor(view, "Consumed " + phase + binding.ParameterId));
            }).ToArray();
        return [("EN", "", "")];
    }

    private static string ValueFor(InstructionPresentation view, string label) => view.Rows.FirstOrDefault(row => row.Label == label)?.Value ?? "";

    private (string Pin, string Name, string Value)[] TerminalOutputPins(EditableRung rung, string key, InstructionPresentation view)
    {
        if (rung.IsCounter) return [("Q", rung.CounterVariable + ".Q", ObservationValue(view, "Done")), ("CV", rung.CounterVariable + ".CV", ObservationValue(view, "Current count"))];
        if (rung.IsNumericOperation)
        {
            var value = ObservationValue(view, "Stored after", "Final snapshot", "Calculated");
            if (_style == LadderVendorStyle.SiemensTia
                && (rung.NumericOperationKind is LadderNumericOperationKind.SquareRoot or LadderNumericOperationKind.Exponentiate
                    || LadderNumericOperationRules.IsScientificUnaryMath(rung.NumericOperationKind)
                    || rung.NumericOperationKind == LadderNumericOperationKind.Normalize))
                value = TiaMonitorValueFormatter.FormatReal(value);
            return [(_style == LadderVendorStyle.SiemensTia && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind)
                    ? TiaNumericInstructionPorts.Output(rung.NumericOperationKind) : "OUT",
                rung.NumericDestination, value)];
        }
        if (rung.IsTimer) return [("Q", rung.TimerVariable + ".Q", ObservationValue(view, "Done")), ("ET", rung.TimerVariable + ".ET", TiaTimerValue(ObservationValue(view, "Elapsed")))];
        if (rung.IsCall)
        {
            var outputs = CallFormals(rung, input: false).Select(formal =>
            {
                var binding = rung.CallBindings.First(item => item.ParameterId == formal.Id);
                var valueLabel = formal.Section == LadderInterfaceSection.InOut
                    ? "Consumed AliasFinal:" + binding.ParameterId
                    : "Consumed CopyOut:" + binding.ParameterId;
                return (formal.Name, binding.Kind == LadderActualKind.Symbol ? binding.Symbol : "", ValueFor(view, valueLabel));
            });
            return outputs.Append(("ENO", "", "")).ToArray();
        }
        if (rung.IsReturn || rung.IsJump || rung.IsLabel) return [("ENO", "", "")];
        return [("ENO", "", "")];
    }

    // Read-only acceptance seam over the exact production pin projection.
    // The catalog monitor sweep must not duplicate or approximate the pin rules
    // used by DrawTiaInstructionBox.
    internal TiaMonitorPinProjection ReviewTiaTerminalPins(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var rung = _document.Rungs[rungIndex];
        var key = TerminalCatalogKey(rung);
        var view = InstructionPresentationFor(OutputElementId(rung), key);
        return new(
            Array.AsReadOnly(TerminalInputPins(rung, key, view)),
            Array.AsReadOnly(TerminalOutputPins(rung, key, view)));
    }

    // Geometry acceptance seam for the CALL renderer.  It checks the exact
    // output-row anchors used by DrawTiaInstructionBox, including the optional
    // comment and the two symbolic/absolute operand lines.  This specifically
    // prevents a center-row InOut conductor from bisecting its actual text.
    internal bool ReviewTiaCallOutputOperandRowsClearConductors(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var rung = _document.Rungs[rungIndex];
        if (!rung.IsCall) return true;
        var key = TerminalCatalogKey(rung);
        var view = InstructionPresentationFor(OutputElementId(rung), key);
        var pins = TerminalOutputPins(rung, key, view);
        var centerY = TreeOutputPosition(rungIndex).Y;
        var firstY = centerY - Math.Max(0, pins.Length - 1) * TiaCallRowPitch / 2;
        for (var i = 0; i < pins.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(pins[i].Name)) continue;
            var pinY = firstY + i * TiaCallRowPitch;
            var conductorY = pinY - 4.0f;
            var tagY = TiaCallActualTagY(pinY, centerY);
            var rows = new List<(float Top, float Bottom)>();
            if (TagInformationDisplay != TiaTagInformationDisplay.Hidden
                && TagInformationPosition == TiaTagInformationPosition.OnOperands
                && TagInformationText(pins[i].Name).Length > 0)
                rows.Add((tagY - 26.0f, tagY - 17.0f));
            for (var line = 0; line < OperandDisplayLines(pins[i].Name).Length; line++)
                rows.Add((tagY + 1.0f + line * 12.0f, tagY + 10.0f + line * 12.0f));
            if (rows.Any(row => conductorY >= row.Top && conductorY <= row.Bottom)) return false;
        }
        return true;
    }

    private void DrawExternalTag(string name, string value, float x, float y, Color statusColor, float width,
        HorizontalAlignment align = HorizontalAlignment.Left, float nameOffset = 0, bool earlierCycle = false)
    {
        statusColor = EarlierCycleValueColor(statusColor, earlierCycle);
        value = TiaMonitorValueFormatter.Format(value, _style == LadderVendorStyle.SiemensTia);
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(value)) return;
        float AlignedX(float itemWidth) => align switch
        {
            HorizontalAlignment.Right => x - itemWidth - nameOffset,
            HorizontalAlignment.Center => x - itemWidth / 2 + nameOffset,
            _ => x + nameOffset,
        };
        if (_style == LadderVendorStyle.SiemensTia && !string.IsNullOrWhiteSpace(name)
            && TagInformationDisplay != TiaTagInformationDisplay.Hidden
            && TagInformationPosition == TiaTagInformationPosition.OnOperands)
        {
            var info = TagInformationText(name);
            if (info.Length > 0)
            {
                var infoX = AlignedX(width);
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(infoX, y - 17), ClipMonitorText(info, width, 9),
                    align, width, 9, new Color("647780"));
            }
        }
        if (!string.IsNullOrWhiteSpace(value))
        {
            var availableWidth = Math.Min(104, width);
            var measuredWidth = ThemeDB.FallbackFont.GetStringSize(value, HorizontalAlignment.Left, -1, 10).X + 6;
            var valueWidth = Math.Min(availableWidth, Math.Max(20, measuredWidth));
            var valueX = AlignedX(valueWidth);
            var chip = new Rect2(valueX, y - 13, valueWidth, 16);
            DrawRect(chip, new Color("d7d9dc"));
            DrawEditorString(ThemeDB.FallbackFont, new Vector2(chip.Position.X + 3, chip.Position.Y + 12), ClipMonitorText(value, valueWidth - 6, 10), align, valueWidth - 6, 10, statusColor);
            y += 4;
        }
        if (!string.IsNullOrWhiteSpace(name))
        {
            var nameX = AlignedX(width);
            var displayLines = OperandDisplayLines(name);
            for (var i = 0; i < displayLines.Length; i++)
            {
                var missingAbsolute = displayLines[i] == "???";
                DrawEditorString(ThemeDB.FallbackFont, new Vector2(nameX, y + 10 + i * 12),
                    ClipMonitorText(displayLines[i], width, 11), align, width, 11,
                    missingAbsolute ? new Color("e11d48") : new Color("008e91"));
            }
        }
    }

    private bool? NumericInstructionEnableFlow(EditableRung rung, bool output)
    {
        if (!IsTiaMonitoringActive) return null;
        var view = InstructionPresentationFor(OutputElementId(rung), TerminalCatalogKey(rung));
        // Earlier-cycle presentation may supply value badges, but it must never
        // energize current EN/ENO conductors.
        if (!view.CurrentExecution || view.UnavailableReason != ObservationUnavailableReason.None || view.Partial) return null;
        var enable = view.Rows.FirstOrDefault(row => row.Label == "Network enable")?.Value;
        if (enable == "FALSE") return false;
        if (enable != "TRUE") return null;
        if (output && view.Rows.Any(row => row.Label.StartsWith("Fault ", StringComparison.Ordinal))) return false;
        return true;
    }

    internal (bool? Enable, bool? Eno) ReviewNumericInstructionEnableFlow(int rungIndex)
    {
        if (rungIndex < 0 || rungIndex >= _document.Rungs.Count)
            throw new ArgumentOutOfRangeException(nameof(rungIndex));
        var rung = _document.Rungs[rungIndex];
        if (!rung.IsNumericOperation) throw new ArgumentException("Rung is not a numeric instruction.", nameof(rungIndex));
        return (NumericInstructionEnableFlow(rung, output: false), NumericInstructionEnableFlow(rung, output: true));
    }
    private string InstructionMnemonic(string key, EditableRung rung)
    {
        if (rung.IsNumericOperation && _style == LadderVendorStyle.SiemensTia
            && TiaNumericInstructionPorts.IsTiaInstruction(rung.NumericOperationKind))
            return TiaNumericInstructionPorts.Mnemonic(rung.NumericOperationKind);
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
        return key switch { "timer-reset" => _style == LadderVendorStyle.SiemensTia ? "RT" : "RES", "counter-reset" => "RES", "counter-load" => "LOAD", "rto" => _style == LadderVendorStyle.SiemensTia ? "TONR" : "RTO", "coil" => _style == LadderVendorStyle.SiemensTia ? "ASSIGN" : "OTE", "set" => _style == LadderVendorStyle.SiemensTia ? "SET" : "OTL", "reset" => _style == LadderVendorStyle.SiemensTia ? "RESET" : "OTU", "call" => _style == LadderVendorStyle.SiemensTia ? "???" : "JSR", _ => key.ToUpperInvariant() };
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
        if (view.UnavailableReason != ObservationUnavailableReason.None) return;
        var row = view.Rows.FirstOrDefault(r => r.Label.StartsWith("Consumed ", StringComparison.Ordinal));
        if (row is null || row.Value is "—" or "Unavailable") return;
        var color = view.Partial ? new Color("b65d16") : IsElementEnergized(node.Id) ? new Color("008f4c") : new Color("274b5c");
        color = EarlierCycleValueColor(color, view.EarlierCycle);
        var badge = TiaMonitorValueFormatter.Format(row.Value, _style == LadderVendorStyle.SiemensTia);
        var badgeWidth = Math.Min(88, Math.Max(36, ThemeDB.FallbackFont.GetStringSize(badge, HorizontalAlignment.Left, -1, 10).X + 10));
        var observationY = node.EdgeMode != LadderEdgeMode.None ? 70 : 28;
        var rect = new Rect2(x - badgeWidth / 2, y + observationY, badgeWidth, 16);
        DrawRect(rect, new Color("d7d9dc"));
        DrawEditorString(ThemeDB.FallbackFont, new Vector2(rect.Position.X + 3, rect.Position.Y + 12), badge, HorizontalAlignment.Center, badgeWidth - 6, 10, color);
    }

    private static Color EarlierCycleValueColor(Color color, bool earlierCycle) => earlierCycle
        ? Color.FromHsv(color.H, color.S * 0.5f, color.V, color.A)
        : color;

    internal (bool EarlierCycle, string Color) ReviewObservationValueStyle(string id, string key, string baseColor)
    {
        var presentation = InstructionPresentationFor(id, key);
        return (presentation.EarlierCycle,
            EarlierCycleValueColor(new Color(baseColor), presentation.EarlierCycle).ToHtml(false));
    }
}

