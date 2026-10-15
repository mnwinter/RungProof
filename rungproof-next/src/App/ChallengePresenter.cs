using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using RungProof.Next.Challenges;

namespace RungProof.Next.App;

/// <summary>Read-only challenge view. It emits intents and never drives a plant or writes progress.</summary>
public partial class ChallengePresenter : PanelContainer
{
    public event Action<ChallengeIntent>? IntentRequested;
    public event Action? SaveRequested;
    public event Action? RunRequested;
    public event Action? StopRequested;
    public event Action? ResetRequested;
    public event Action? StartRequested;
    public event Action? ProcessStopRequested;
    public event Action<string>? LayoutRequested;
    private readonly Label _status = new();
    private readonly Label _validation = new();
    private readonly RichTextLabel _brief = TextPanel();
    private readonly RichTextLabel _results = TextPanel();
    private readonly RichTextLabel _hints = TextPanel();
    private readonly RichTextLabel _trace = TextPanel();
    private readonly ItemList _traceRows = new();
    private readonly Button _test;
    private readonly Button _cancel;
    private readonly Button _hint;
    private readonly Button _revert;
    private ChallengeDefinition? _definition;
    private ChallengeRunResult? _displayedResult;
    private int _hintCount;
    private IReadOnlyList<ChallengeProgressRecord> _progress = Array.Empty<ChallengeProgressRecord>();

    public ChallengePresenter()
    {
        Name = "ChallengeWorkspace";
        ZIndex = 30;
        AddThemeFontSizeOverride("font_size", 16);
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("101a22"), BorderColor = new Color("365666"), BorderWidthBottom = 1 });
        var body = new VBoxContainer();
        AddChild(body);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.AddThemeFontSizeOverride("font_size", 16);
        var statusRow = new HBoxContainer { Name = "ChallengeStatusRow" };
        _status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _status.SizeFlagsStretchRatio = 1;
        _status.MaxLinesVisible = 2;
        _status.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        statusRow.AddChild(_status);
        _validation.Name = "ChallengeValidationIssues";
        _validation.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _validation.MaxLinesVisible = 2;
        _validation.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _validation.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _validation.SizeFlagsStretchRatio = 3;
        _validation.AddThemeFontSizeOverride("font_size", 16);
        _validation.AddThemeColorOverride("font_color", new Color("ffce85"));
        _validation.Visible = false;
        statusRow.AddChild(_validation);
        body.AddChild(statusRow);
        var controls = new HBoxContainer();
        body.AddChild(controls);
        _test = Button(controls, "Test", () => IntentRequested?.Invoke(ChallengeIntent.Test));
        _cancel = Button(controls, "Cancel Test", () => IntentRequested?.Invoke(ChallengeIntent.Cancel));
        Button(controls, "Save Draft", () => SaveRequested?.Invoke());
        Button(controls, "Run Simulation", () => RunRequested?.Invoke());
        Button(controls, "Stop Simulation", () => StopRequested?.Invoke());
        Button(controls, "Reset Attempt", () => ResetRequested?.Invoke());
        Button(controls, "Process Start", () => StartRequested?.Invoke());
        Button(controls, "Process Stop", () => ProcessStopRequested?.Invoke());
        var tabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        tabs.AddThemeFontSizeOverride("font_size", 16);
        body.AddChild(tabs);
        AddPage(tabs, "Objective & cases", _brief);
        AddPage(tabs, "Results", _results);
        var hintBody = new HBoxContainer();
        _hint = Button(hintBody, "Next hint", RevealHint);
        hintBody.AddChild(_hints);
        AddPage(tabs, "Hints", hintBody);
        var traceBody = new HBoxContainer();
        _traceRows.CustomMinimumSize = new Vector2(260, 0);
        _traceRows.SizeFlagsVertical = SizeFlags.ExpandFill;
        _traceRows.AddThemeFontSizeOverride("font_size", 16);
        _traceRows.ItemSelected += index => ShowTraceRow((int)index);
        traceBody.AddChild(_traceRows);
        traceBody.AddChild(_trace);
        AddPage(tabs, "Historical trace", traceBody);
        var layout = new HBoxContainer();
        _revert = Button(layout, "Revert Starter…", () => IntentRequested?.Invoke(ChallengeIntent.ResetStarter));
        Button(layout, "Scene + ladder", () => LayoutRequested?.Invoke("split"));
        Button(layout, "Ladder focus", () => LayoutRequested?.Invoke("ladder"));
        Button(layout, "Scene focus", () => LayoutRequested?.Invoke("operator"));
        Button(layout, "Leave challenge…", () => IntentRequested?.Invoke(ChallengeIntent.Leave));
        AddPage(tabs, "Layout & draft", layout);
    }

    public void Configure(ChallengeDefinition definition)
    {
        SetValidationIssues(Array.Empty<string>());
        _definition = definition;
        _hintCount = 0;
        _hint.Disabled = definition.Hints.Count == 0;
        _hints.Text = "Hints are optional and sequential. Your draft remains yours.";
        var text = new StringBuilder().AppendLine(definition.Title).AppendLine(definition.Objective);
        text.AppendLine("Seven disclosed situations (one accepted scan Start/Stop pulses):");
        foreach (var test in definition.Cases) text.AppendLine($"{test.Id}: {ChallengePresenterModel.CaseDisclosure(test.Kind)}");
        text.AppendLine("Glossary — commands and feedback are distinct:");
        foreach (var entry in definition.Glossary)
            text.AppendLine($"{entry.DisplayName} [{entry.Binding}] — {entry.Ownership}: {entry.Meaning}");
        text.AppendLine("Palette: BOOL NO/NC contacts, series/parallel branches and ordinary memory/output Assign coils. No timers, counters, edge contacts or Set/Reset coils.");
        text.AppendLine("Keyboard: arrows select contacts/slots; Ctrl+Up/Down selects rungs; Insert adds NO, Shift+Insert branch, Ctrl+Insert rung; Enter properties; F2 tag table; Alt+Left/Right moves; Ctrl+C/V copies/pastes; Ctrl+Z/Y undo/redo; Ctrl+S saves.");
        text.AppendLine("Unsolved starter. Revert Starter replaces logic through the draft guard; Reset Attempt keeps logic. Process Stop is an ordinary modeled input. Offline results do not certify machinery safety.");
        _brief.Text = text.ToString();
        RenderResults();
    }

    /// <summary>Editor diagnostics remain visible until explicitly replaced or a draft mutation clears them.</summary>
    public void SetValidationIssues(IReadOnlyList<string> issues)
    {
        var meaningful = issues.Where(issue => !string.IsNullOrWhiteSpace(issue)).ToArray();
        _validation.Visible = meaningful.Length > 0;
        _validation.Text = meaningful.Length == 0 ? string.Empty : "Draft needs editing: " + meaningful[0]
            + (meaningful.Length > 1 ? $" (+{meaningful.Length - 1} more; hover for details)" : string.Empty);
        _validation.TooltipText = string.Join("\n", meaningful);
        // Two diagnostic lines occupy the existing chrome allowance, not extra graph height.
        foreach (var panel in new[] { _brief, _results, _hints, _trace })
            panel.CustomMinimumSize = new Vector2(0, meaningful.Length == 0 ? 45 : 21);
    }

    public bool VerifyValidationVisible(out string diagnostic)
    {
        var bounds = _validation.GetGlobalRect();
        var visible = _validation.IsVisibleInTree() && !string.IsNullOrWhiteSpace(_validation.Text)
            && bounds.Size.X > 0 && bounds.Size.Y > 0 && GetGlobalRect().Intersects(bounds)
            && _validation.GetThemeFontSize("font_size") >= 16;
        diagnostic = $"validationVisible={visible} text={_validation.Text} labelBounds={bounds} panelBounds={GetGlobalRect()}";
        return visible;
    }

    public void Present(ChallengePresentationState state)
    {
        _status.Text = ChallengePresenterModel.Status(state);
        _test.Disabled = !state.CanTest || state.IsWorkerOutstanding;
        _cancel.Disabled = !state.IsWorkerOutstanding;
        _revert.Disabled = state.MutationLocked;
        if (!ReferenceEquals(_displayedResult, state.LastResult))
        {
            _displayedResult = state.LastResult;
            _traceRows.Clear();
            if (_displayedResult is not null)
                foreach (var row in _displayedResult.Trace)
                    _traceRows.AddItem($"{row.CaseId} cycle {row.Cycle} #{row.CaseSequence} scan {row.ScanNumber}");
            _trace.Text = "Historical evidence only. Selecting a row does not replay the plant or live ladder.";
        }
        RenderResults();
    }

    public void SetProgress(IReadOnlyList<ChallengeProgressRecord> records)
    {
        _progress = records.ToArray();
        RenderResults();
    }

    private void RenderResults()
    {
        if (_definition is null) return;
        _results.Text = ChallengePresenterModel.Ledger(_definition, _displayedResult, _progress);
    }

    private void RevealHint()
    {
        if (_definition is null || _hintCount >= _definition.Hints.Count) return;
        _hintCount++;
        _hints.Text = ChallengePresenterModel.Hints(_definition, _hintCount);
        _hint.Disabled = _hintCount >= _definition.Hints.Count;
        IntentRequested?.Invoke(ChallengeIntent.Hint);
    }

    private void ShowTraceRow(int index)
    {
        if (_displayedResult is null || index < 0 || index >= _displayedResult.Trace.Count) return;
        var row = _displayedResult.Trace[index];
        _trace.Text = ChallengePresenterModel.TraceRow(row);
    }

    private static RichTextLabel TextPanel() => new()
    {
        BbcodeEnabled = false, ScrollActive = true, FitContent = false,
        CustomMinimumSize = new Vector2(0, 45), SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SizeFlagsVertical = SizeFlags.ExpandFill,
    };
    private static void AddPage(TabContainer tabs, string name, Control content)
    {
        var page = new MarginContainer { Name = name };
        page.AddChild(content);
        tabs.AddChild(page);
        SetReadableFonts(content);
    }
    private static void SetReadableFonts(Node node)
    {
        if (node is Control control)
        {
            control.AddThemeFontSizeOverride("font_size", 16);
            control.AddThemeFontSizeOverride("normal_font_size", 16);
        }
        foreach (var child in node.GetChildren()) SetReadableFonts(child);
    }
    private static Button Button(BoxContainer parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.All };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}
