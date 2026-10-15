using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;

public partial class SimulatorShell
{
    public event Action? PauseRequested;
    public event Action? StepScanRequested;
    private bool _scanInspectionPaused;
    private Button _scanPause = null!, _scanStep = null!, _scanHistoryButton = null!;
    private Window _scanHistoryWindow = null!;
    private ItemList _scanHistoryRows = null!;
    private TextEdit _scanHistoryText = null!;
    private Label _scanHistoryScope = null!;
    private IReadOnlyList<VirtualControllerAcceptedScan> _scanHistory = Array.Empty<VirtualControllerAcceptedScan>();
    private VirtualControllerAcceptedScan? _scanSelectedHistory;
    private bool _scanHistorySelectionEvicted;
    private OptionButton _historyInstruction = null!, _historyContext = null!;
    private string[] _historyInstructionIds = [], _historyContextKeys = [];
    private string? _historyInstructionId, _historyContextKey;
    private bool _refreshingHistorySelectors;
    private Button? _scanRunButton;
    private string ScanInspectionRuntimeLabel => _scanInspectionPaused ? "PAUSED" : ActiveRuntimeRunning ? "RUNNING" : "STOPPED";

    public void AppendScanInspectionControls(HBoxContainer transportRow)
    {
        _scanRunButton = transportRow.GetNode<Button>("RunButton");
        _scanPause = ToolbarButton("PauseButton", "PAUSE", new Color("355d73"), 64);
        _scanStep = ToolbarButton("StepScanButton", "SCAN", new Color("355d73"), 58);
        _scanHistoryButton = ToolbarButton("ScanHistoryButton", "HISTORY", new Color("355d73"), 76);
        _scanPause.TooltipText = "Hold accepted clock and plant state; commands remain retained.";
        _scanStep.TooltipText = "Accept exactly one PLC scan and plant period while paused.";
        _scanPause.Disabled = _scanStep.Disabled = true;
        _scanPause.Pressed += () => PauseRequested?.Invoke();
        _scanStep.Pressed += () => StepScanRequested?.Invoke();
        _scanHistoryButton.Pressed += () => { RefreshScanHistory(); _scanHistoryWindow.PopupCenteredRatio(.82f); };
        transportRow.AddChild(_scanPause); transportRow.AddChild(_scanStep); transportRow.AddChild(_scanHistoryButton);
        _scanHistoryWindow = new Window { Name="AcceptedScanHistory", Title="Scan attempt history · read-only", Visible=false, MinSize=new Vector2I(640,360), Size=new Vector2I(980,580) };
        _scanHistoryWindow.CloseRequested += _scanHistoryWindow.Hide;
        AddChild(_scanHistoryWindow);
        var body = new VBoxContainer { AnchorRight=1,AnchorBottom=1,OffsetLeft=12,OffsetTop=12,OffsetRight=-12,OffsetBottom=-12 };
        _scanHistoryWindow.AddChild(body);
        _scanHistoryScope = new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart };
        body.AddChild(_scanHistoryScope);
        var split = new HSplitContainer { SizeFlagsVertical=Control.SizeFlags.ExpandFill };
        body.AddChild(split);
        _scanHistoryRows = new ItemList { CustomMinimumSize=new Vector2(190,0), SizeFlagsVertical=Control.SizeFlags.ExpandFill };
        _scanHistoryText = new TextEdit { Editable=false,WrapMode=TextEdit.LineWrappingMode.Boundary,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        // Read-only TextEdit uses font_readonly_color, not its editable font color.
        _scanHistoryText.AddThemeColorOverride("font_readonly_color", new Color("e6eef2"));
        _scanHistoryText.AddThemeColorOverride("font_color", new Color("e6eef2"));
        _scanHistoryText.AddThemeStyleboxOverride("read_only", new StyleBoxFlat { BgColor=new Color("15232c") });
        _scanHistoryRows.AddThemeColorOverride("font_color", new Color("e6eef2"));
        var detail = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _historyInstruction = new OptionButton { Name = "HistoryInstructionSelector", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _historyContext = new OptionButton { Name = "HistoryInstructionContext", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        detail.AddChild(_historyInstruction); detail.AddChild(_historyContext);
        _scanHistoryText.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        detail.AddChild(_scanHistoryText);
        split.AddChild(_scanHistoryRows); split.AddChild(detail);
        _scanHistoryRows.ItemSelected += row => { _scanHistorySelectionEvicted=false; _scanSelectedHistory=_scanHistory[(int)row]; _historyContextKey=null; ShowScanHistoryRow((int)row); };
        _historyInstruction.ItemSelected += row =>
        {
            if(_refreshingHistorySelectors || row<0 || row>=_historyInstructionIds.Length)return;
            _historyInstructionId=_historyInstructionIds[(int)row]; _historyContextKey=null; RefreshScanHistory();
        };
        _historyContext.ItemSelected += row =>
        {
            if(_refreshingHistorySelectors || row<0 || row>=_historyContextKeys.Length)return;
            _historyContextKey=_historyContextKeys[(int)row]; RefreshScanHistory();
        };
    }

    public void SetScanInspectionState(bool available,bool paused,string scope,IReadOnlyList<VirtualControllerAcceptedScan> history,long droppedCount=0)
    {
        _scanInspectionPaused=paused;
        RefreshWorkspaceTrialSchedulingDisplay();
        foreach(var canvas in _ladderCanvases) canvas.SetMonitorPaused(paused);
        if (_scanPause is null) return;
        _scanPause.Disabled=!available||paused;
        _scanStep.Disabled=!available||!paused;
        _scanPause.TooltipText=available?"Hold accepted clock and plant state.":scope;
        _scanStep.TooltipText=available?"One accepted scan; remains paused.":scope;
        _scanRunButton!.Text=paused&&available?"RESUME":"RUN";
        _scanHistory=history.ToArray(); // Records already own immutable primitive copies.
        var retained=_scanHistory.Count==0?"No retained scans":$"Retained scans {_scanHistory[0].CompletedScan.ScanNumber}–{_scanHistory[^1].CompletedScan.ScanNumber}";
        _scanHistoryScope.Text=scope+$"\n{retained} · dropped {droppedCount} · capacity 128. Post-plant feedback is sampled on a following scan, not this row. Selection never replays or changes live state.";
        if (_scanSelectedHistory is { } selected&&!_scanHistory.Any(r=>ReferenceEquals(r,selected)))
        { _scanSelectedHistory=null; _scanHistorySelectionEvicted=true; }
        if (_scanHistoryWindow.Visible) RefreshScanHistory();
        RefreshRuntimeState();
        if(paused&&_virtualSnapshot is { } snapshot)
        {
            _ladderWorkspaceStatus.Text=$"PAUSED · ACCEPTED SCAN {snapshot.ScanNumber} · NO PHYSICAL PLC";
            _virtualControllerStatus.Text=$"VIRTUAL CONTROLLER · PAUSED · ACCEPTED SCAN {snapshot.ScanNumber}";
        }
    }
    private void RefreshScanHistory()
    {
        _scanHistoryRows.Clear();
        for(var i=0;i<_scanHistory.Count;i++) _scanHistoryRows.AddItem($"Scan {_scanHistory[i].CompletedScan.ScanNumber} · {_scanHistory[i].CompletedScan.Completion} · {_scanHistory[i].CompletedScan.SimulatedTime.TotalMilliseconds:0.###} ms");
        if(_scanHistorySelectionEvicted)
        {
            _scanHistoryText.Text="Selected history evicted. Choose another retained attempt explicitly; live values have not been substituted.";
            _historyInstruction.Clear(); _historyContext.Clear(); _historyInstruction.Disabled=_historyContext.Disabled=true; return;
        }
        if(_scanHistory.Count==0){_scanHistoryText.Text="No accepted scans in this epoch. Run or Scan a valid current built-in program.";return;}
        var row=_scanSelectedHistory is { } selected?Enumerable.Range(0,_scanHistory.Count).FirstOrDefault(i=>ReferenceEquals(_scanHistory[i],selected),_scanHistory.Count-1):_scanHistory.Count-1;
        _scanHistoryRows.Select(row); ShowScanHistoryRow(row);
    }
    private void ShowScanHistoryRow(int row)
    {
        var record=_scanHistory[row]; var scan=record.CompletedScan; var text=new StringBuilder();
        _scanSelectedHistory=record;
        text.AppendLine($"HISTORICAL {(scan.Completion==ControllerCompletion.Completed?"ACCEPTED SCAN":"SCAN ATTEMPT")} {scan.ScanNumber} · {scan.Completion} · {scan.SimulatedTime.TotalMilliseconds:0.###} ms");
        if(scan.Publication is { } publication)
        {
            text.AppendLine($"Load {publication.LoadId} · generation {publication.ExecutionGeneration} · attempt {publication.ScanAttemptNumber}");
            text.AppendLine($"Phases: {publication.Phases} · output commits: {publication.CommitProgress}");
            if(publication.FailurePhase is { } phase) text.AppendLine($"Failure: {phase} · {publication.FailureCode}");
        }
        text.AppendLine("Read-only image. Current monitor and plant are unchanged.\n");
        AppendHistoricalInstruction(text, scan.Publication);
        void Image(string title,VirtualControllerSignalImage image)
        {
            text.AppendLine(title);
            foreach(var p in image.Bools.OrderBy(p=>p.Key,StringComparer.Ordinal))text.AppendLine($"  {p.Key}: BOOL {p.Value}");
            foreach(var p in image.Numbers.OrderBy(p=>p.Key,StringComparer.Ordinal))text.AppendLine($"  {p.Key}: {p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            foreach(var p in image.Text.OrderBy(p=>p.Key,StringComparer.Ordinal))text.AppendLine($"  {p.Key}: STRING {p.Value}");
            text.AppendLine();
        }
        void AvailableImage(string title, VirtualControllerSignalImage? image)
        {
            if(image is not null) Image(title,image);
            else text.AppendLine(title+" · unavailable for this attempt\n");
        }
        AvailableImage("SAMPLED INPUTS · operator-overlay image before forcing",scan.Publication is { } sampledPublication?sampledPublication.SampledInputs:scan.SampledInputs);
        AvailableImage("EFFECTIVE INPUTS · force-applied image",scan.Publication is { } effectivePublication?effectivePublication.EffectiveInputs:scan.EffectiveInputs);
        AvailableImage("OUTPUT IMAGE · see completion and commit phases above",scan.Publication is { } outputPublication?outputPublication.PublishedValues:scan.Outputs);
        if((scan.Publication is { } postPublication?postPublication.PostPlantInputs:record.PostPlantInputs) is { } post) Image("POST-PLANT PC INPUTS · generated after this scan",post);
        else text.AppendLine("POST-PLANT PC INPUTS · unavailable for this record");
        foreach(var diagnostic in scan.Diagnostics)text.AppendLine("Diagnostic: "+diagnostic);
        _scanHistoryText.Text=text.ToString();
    }
    private void AppendHistoricalInstruction(StringBuilder text, ControllerPublication? publication)
    {
        _refreshingHistorySelectors=true;
        try
        {
            var observations=publication?.Observations?.GlobalSummaries.OrderBy(o=>o.ExecutionOrdinal).ToArray()??[];
            _historyInstruction.Clear(); _historyInstructionIds=observations.Select(o=>o.InstructionId).ToArray();
            foreach(var observedItem in observations)_historyInstruction.AddItem(observedItem.CatalogKey+" · "+observedItem.InstructionId);
            if(_historyInstructionId is null || !_historyInstructionIds.Contains(_historyInstructionId))_historyInstructionId=_historyInstructionIds.FirstOrDefault();
            var index=Array.IndexOf(_historyInstructionIds,_historyInstructionId);
            _historyInstruction.Disabled=index<0;
            _historyContext.Clear();
            if(index<0){_historyContext.Disabled=true;text.AppendLine("No retained instruction observations for this attempt.\n");return;}
            _historyInstruction.Select(index);
            var observation=observations[index];
            var view=LadderInstructionPresentation.Present(observation.InstructionId,observation.CatalogKey,publication,selectedContext:_historyContextKey);
            if(view.UnavailableReason==ObservationUnavailableReason.None)
                view=view with{Status=view.Partial?"HISTORICAL PARTIAL / ABORTED EXECUTION":"HISTORICAL EXECUTION — selected retained attempt",CurrentExecution=false};
            _historyContextKeys=view.Contexts.Select(c=>c.Key).ToArray();
            foreach(var context in view.Contexts)_historyContext.AddItem(context.Label);
            var contextIndex=Array.IndexOf(_historyContextKeys,_historyContextKey??"");
            if(contextIndex>=0)_historyContext.Select(contextIndex);
            else{_historyContext.AddItem("Selected context not captured");_historyContext.Select(_historyContext.ItemCount-1);}
            _historyContext.Disabled=false;
            text.AppendLine("HISTORICAL INSTRUCTION · "+observation.InstructionId+" · read only");
            text.AppendLine(view.Detail);text.AppendLine();
        }
        finally{_refreshingHistorySelectors=false;}
    }
    public bool VerifyScanInspectionHistorySelection(int index,out string diagnostic)
    {
        if(index<0||index>=_scanHistory.Count){diagnostic="Requested historical row is unavailable.";return false;}
        RefreshScanHistory(); _scanHistoryWindow.PopupCenteredRatio(.82f);
        _scanHistoryRows.Select(index);
        _scanHistoryRows.EmitSignal(ItemList.SignalName.ItemSelected,index);
        diagnostic=_scanHistoryText.Text;
        return _scanHistoryWindow.Visible&&diagnostic.Contains($"HISTORICAL ACCEPTED SCAN {_scanHistory[index].CompletedScan.ScanNumber}",StringComparison.Ordinal);
    }
    public bool VerifyScanInspectionControls(bool paused,out string diagnostic)
    {
        var canvasesMatch=_ladderCanvases.Count>0&&_ladderCanvases.All(c=>c.MonitorPaused==paused
            &&(!c.MonitorActive||c.MonitorStatusText.Contains(paused?"PAUSED":"MONITOR",StringComparison.Ordinal)));
        bool CardMatches(Control card,Label result)=>card is null||!card.Visible||result.Text.StartsWith("CLOCK HELD",StringComparison.Ordinal)==paused;
        var cardsMatch=CardMatches(_workspaceTrialCard,_workspaceTrialResult)&&CardMatches(_workspaceRollerTrialCard,_workspaceRollerTrialResult);
        diagnostic=$"pauseDisabled={_scanPause.Disabled} stepDisabled={_scanStep.Disabled} run={_scanRunButton!.Text} state={_runtimeStatus.Text} canvasesMatch={canvasesMatch}";
        return _scanInspectionPaused==paused&&_scanRunButton.Text==(paused?"RESUME":"RUN")
            &&canvasesMatch&&cardsMatch
            &&(!paused||(!_scanStep.Disabled&&_runtimeStatus.Text.Contains("PAUSED",StringComparison.Ordinal)));
    }
    public bool VerifyScanInspectionHistoryInitiallyHidden(out string diagnostic)
    {
        diagnostic=$"historyVisible={_scanHistoryWindow.Visible}";
        return !_scanHistoryWindow.Visible;
    }
}

