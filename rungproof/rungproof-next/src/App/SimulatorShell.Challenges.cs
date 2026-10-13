using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Challenges;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public enum ChallengeIntent { Enter, Test, Cancel, Hint, ResetStarter, Leave }

public partial class SimulatorShell
{
    private ChallengeDefinition? _challengeDefinition;
    private ChallengePresentationState? _challengeState;
    private ChallengePresenter? _challengePresenter;
    private SceneLadderDraft? _preChallengeDraft;
    private readonly Dictionary<string, SceneLadderDraft> _challengeDraftCache = new(StringComparer.Ordinal);
    private CheckButton? _reducedMotionSetting;
    private readonly List<(Control Control, string Key, int? Previous)> _challengeFontOverrides = [];
    private const float ChallengePanelHeight = 154;
    private string _preChallengeView = "operator";
    private string _preChallengeSceneName = string.Empty;
    private string _preChallengeLadderStatus = string.Empty;
    private readonly Dictionary<Control, (bool Visible, Vector2 Minimum)> _challengeChromeDefaults = [];
    private readonly Dictionary<TabContainer, int> _challengeTabDefaults = [];
    private string ChallengeDisplayName => $"Challenge · {_challengeDefinition?.Title ?? "Conveyor Stop"}";
    public Func<Action, bool>? ChallengeCleanupGuard { get; set; }
    public event Action<ChallengeIntent>? ChallengeIntentRequested;
    public event Action? ChallengeDraftChanged;
    public event Action<bool>? ReducedMotionChanged;
    public bool ReducedMotion { get; private set; } = true;
    private bool ChallengeMutationLocked => _challengeState?.MutationLocked == true;
    public bool IsChallengeDraftActive => _preChallengeDraft is not null;

    private void BuildChallengeInterface()
    {
        _challengePresenter = new ChallengePresenter();
        _challengePresenter.Visible = false;
        ChallengeDraftChanged += ClearChallengeValidationIssues;
        _challengePresenter.IntentRequested += intent => ChallengeIntentRequested?.Invoke(intent);
        _challengePresenter.SaveRequested += ShowLadderSaveDialog;
        _challengePresenter.RunRequested += () => RunRequested?.Invoke();
        _challengePresenter.StopRequested += () => StopRequested?.Invoke();
        _challengePresenter.ResetRequested += () => ResetRequested?.Invoke();
        _challengePresenter.StartRequested += () => VirtualStartRequested?.Invoke();
        _challengePresenter.ProcessStopRequested += () => VirtualStopRequested?.Invoke();
        _challengePresenter.LayoutRequested += mode => SetProductView(mode);
        AddChild(_challengePresenter);
        var sceneMenu = GetNodeOrNull<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/SceneMenu")?.GetPopup();
        if (sceneMenu is not null)
        {
            sceneMenu.AddSeparator();
            sceneMenu.AddItem("Challenge — Conveyor Stop", 5000);
            sceneMenu.IdPressed += id => { if (id == 5000) ChallengeIntentRequested?.Invoke(ChallengeIntent.Enter); };
        }
    }

    public void ConfigureChallenge(ChallengeDefinition definition)
    {
        _challengeDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
        _challengePresenter?.Configure(definition);
    }

    public void SetChallengePresentationState(ChallengePresentationState state)
    {
        _challengeState = state;
        _challengePresenter?.Present(state);
        foreach (var canvas in _ladderCanvases) canvas.MutationLocked = state.MutationLocked;
    }

    public void SetChallengeValidationIssues(IReadOnlyList<string> issues)
        => _challengePresenter?.SetValidationIssues(issues);

    private void ClearChallengeValidationIssues() => SetChallengeValidationIssues(Array.Empty<string>());

    public bool VerifyChallengeValidationVisible(out string diagnostic)
    {
        if (_challengePresenter is null) { diagnostic = "challenge presenter is unavailable"; return false; }
        return _challengePresenter.VerifyValidationVisible(out diagnostic);
    }

    public void SetChallengeProgress(IReadOnlyList<ChallengeProgressRecord> records)
        => _challengePresenter?.SetProgress(records);

    /// <summary>Validate the original editor graph before a serializer can discard metadata.</summary>
    public bool TryBuildChallengeDraft(out LadderProgram program, out IReadOnlyList<string> issues)
    {
        var valid = TryBuildCurrentLadderProgram(out program, out var editorIssues);
        issues = editorIssues.Concat(ChallengeEligibilityValidator.Validate(program))
            .Select(issue => $"{issue.Code} {issue.Path}: {issue.Message}").ToArray();
        return valid && issues.Count == 0;
    }

    /// <summary>Root calls this only after cleanup and the existing unsaved-draft guard.</summary>
    public void BeginChallengeDraft(LadderProgram starter) => BeginChallengeDraftCore(() => _ladderDocument.ReplaceFromProgram(starter));
    public void BeginChallengeDraft(LadderEditorDocument starter) => BeginChallengeDraftCore(() => _ladderDocument.RestoreSnapshot(starter.CaptureSnapshot()));
    private void BeginChallengeDraftCore(Action loadStarter)
    {
        if (ChallengeMutationLocked) throw new InvalidOperationException("Challenge worker still owns the draft.");
        if (_challengeDefinition is null) throw new InvalidOperationException("Configure the trusted challenge first.");
        if (_preChallengeDraft is null)
        {
            _preChallengeDraft = CaptureSceneLadderDraft();
            _preChallengeView = _productView;
            _preChallengeSceneName = _activeSceneName;
            _preChallengeLadderStatus = _ladderWorkspaceStatus.Text;
        }
        ChallengeDraftChanged?.Invoke();
        if (_challengeDraftCache.TryGetValue(_challengeDefinition.Id, out var saved)) RestoreChallengeDraft(saved);
        else
        {
            loadStarter();
            _ladderDocument.SourceSceneId = _challengeDefinition.SceneId;
            _ladderSavedProjectJson = null;
            _ladderProjectPath = null;
            _ladderHistory = new LadderEditorHistory(150);
        }
        _ladderContactClipboard = null;
        _ladderRungClipboard = null;
        foreach (var canvas in _ladderCanvases) canvas.ChallengeReadable = true;
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        RefreshLadderMonitorMatch();
        ApplyChallengeReadableFonts(this);
        if (_challengePresenter is not null) _challengePresenter.Visible = true;
        SetProductView("split");
        RefreshChallengeIdentity();
    }

    public void EndChallengeDraft()
    {
        if (ChallengeMutationLocked) throw new InvalidOperationException("Challenge worker still owns the draft.");
        if (_preChallengeDraft is null) return;
        if (_challengeDefinition is not null) _challengeDraftCache[_challengeDefinition.Id] = CaptureSceneLadderDraft();
        RestoreChallengeDraft(_preChallengeDraft);
        _preChallengeDraft = null;
        _activeSceneName = _preChallengeSceneName;
        foreach (var item in _challengeFontOverrides)
        {
            if (item.Previous.HasValue) item.Control.AddThemeFontSizeOverride(item.Key, item.Previous.Value);
            else item.Control.RemoveThemeFontSizeOverride(item.Key);
        }
        _challengeFontOverrides.Clear();
        _challengeState = null;
        foreach (var canvas in _ladderCanvases) { canvas.MutationLocked = false; canvas.ChallengeReadable = false; }
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        RefreshLadderMonitorMatch();
        if (_challengePresenter is not null) _challengePresenter.Visible = false;
        SetProductView(_preChallengeView);
        foreach (var item in _challengeChromeDefaults) { item.Key.Visible = item.Value.Visible; item.Key.CustomMinimumSize = item.Value.Minimum; }
        foreach (var item in _challengeTabDefaults) item.Key.CurrentTab = item.Value;
        _challengeChromeDefaults.Clear();
        _challengeTabDefaults.Clear();
        _ladderWorkspaceStatus.Text = _preChallengeLadderStatus;
        _sceneTitle.Text = _activeSceneName;
        _sceneTitle.TooltipText = _activeSceneName;
        RefreshOperatorPanels();
    }

    private void RestoreChallengeDraft(SceneLadderDraft draft)
    {
        _ladderDocument.RestoreSnapshot(draft.Document);
        _ladderSavedProjectJson = draft.SavedJson;
        _ladderProjectPath = draft.ProjectPath;
        _ladderHistory = draft.History;
    }

    // These guard and save surfaces let the coordinator keep navigation and cleanup ownership.
    public void GuardChallengeDraftReplacement(Action action) => RequestLadderAction(action);
    public void RevertChallengeStarter(LadderProgram starter) => RevertChallengeStarterCore(() => _ladderDocument.ReplaceFromProgram(starter));
    public void RevertChallengeStarter(LadderEditorDocument starter) => RevertChallengeStarterCore(() => _ladderDocument.RestoreSnapshot(starter.CaptureSnapshot()));
    private void RevertChallengeStarterCore(Action loadStarter)
    {
        if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
        ChallengeDraftChanged?.Invoke();
        loadStarter();
        _ladderDocument.SourceSceneId = _challengeDefinition?.SceneId ?? string.Empty;
        _ladderProjectPath = null;
        _ladderSavedProjectJson = null;
        _ladderHistory = new LadderEditorHistory(150);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        RefreshLadderMonitorMatch();
    }

    private void ShowChallengeLocked()
    {
        if (_ladderWorkspaceStatus is not null)
            _ladderWorkspaceStatus.Text = "TEST CLEANUP PENDING · read, copy and Save Draft remain available";
    }

    private void AddReducedMotionSetting(VBoxContainer body, bool reducedMotion)
    {
        ReducedMotion = reducedMotion;
        _reducedMotionSetting = new CheckButton
        {
            Name = "ReducedMotionSetting", Text = "Reduce decorative motion", ButtonPressed = reducedMotion,
            TooltipText = "Stops decorative ventilation motion; modeled process motion still follows the simulation.",
            FocusMode = Control.FocusModeEnum.All,
        };
        _reducedMotionSetting.AddThemeFontSizeOverride("font_size", 16);
        _reducedMotionSetting.Toggled += value =>
        {
            ReducedMotion = value;
            SaveApplicationSettings();
            ReducedMotionChanged?.Invoke(value);
        };
        body.AddChild(_reducedMotionSetting);
    }

    private static bool FocusFirstChallengeEditorControl(Node node)
    {
        if (node is Control { Visible: true, FocusMode: Control.FocusModeEnum.All } control
            && control is LineEdit or OptionButton or SpinBox)
        {
            control.GrabFocus();
            return true;
        }
        foreach (var child in node.GetChildren())
            if (child is not Control { Visible: false } && FocusFirstChallengeEditorControl(child)) return true;
        return false;
    }

    private void ApplyChallengeReadableFonts(Node node)
    {
        if (node is Control control)
            foreach (var key in new[] { "font_size", "normal_font_size", "bold_font_size", "mono_font_size" })
            {
                if (_challengeFontOverrides.Any(item => item.Control == control && item.Key == key)) continue;
                _challengeFontOverrides.Add((control, key, control.HasThemeFontSizeOverride(key) ? control.GetThemeFontSize(key) : null));
                control.AddThemeFontSizeOverride(key, Math.Max(16, control.GetThemeFontSize(key)));
            }
        foreach (var child in node.GetChildren()) ApplyChallengeReadableFonts(child);
    }

    /// <summary>Opt-in verifier seam; root calls before entry with an isolated evidence save path.</summary>
    public bool VerifyChallengeDraftPreservation(LadderEditorDocument starter, string tempSavePath, out string diagnostic)
    {
        if (IsChallengeDraftActive || ChallengeMutationLocked || _challengeDefinition is null)
        { diagnostic = "requires configured inactive challenge and no outstanding worker"; return false; }
        var original = CaptureSceneLadderDraft();
        var beforeJson = LadderEditorProjectJson.Save(_ladderDocument);
        var beforeUndo = _ladderHistory.UndoDescription;
        var beforeRedo = _ladderHistory.RedoDescription;
        var cached = _challengeDraftCache.ToArray();
        _challengeDraftCache.Clear();
        try
        {
            BeginChallengeDraft(starter);
            var expected = new LadderEditorDocument();
            expected.RestoreSnapshot(starter.CaptureSnapshot());
            expected.SourceSceneId = _challengeDefinition.SceneId;
            var expectedJson = LadderEditorProjectJson.Save(expected);
            var exactStarter = LadderEditorProjectJson.Save(_ladderDocument) == expectedJson;
            var invalid = !LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
            var saved = TrySaveLadderProject(tempSavePath, out var saveDiagnostic);
            var loaded = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(tempSavePath));
            var invalidSaved = saved && loaded.Document is not null
                && LadderEditorProjectJson.Save(loaded.Document) == expectedJson
                && !LadderCompiler.Compile(loaded.Document.BuildProgram()).IsValid;
            EndChallengeDraft();
            var restored = LadderEditorProjectJson.Save(_ladderDocument) == beforeJson
                && _ladderProjectPath == original.ProjectPath && _ladderSavedProjectJson == original.SavedJson
                && ReferenceEquals(_ladderHistory, original.History)
                && _ladderHistory.UndoDescription == beforeUndo && _ladderHistory.RedoDescription == beforeRedo;
            diagnostic = $"exactStarter={exactStarter} invalid={invalid} invalidSaved={invalidSaved} exactDemoRestore={restored}; {saveDiagnostic}";
            return exactStarter && invalid && invalidSaved && restored;
        }
        finally
        {
            if (IsChallengeDraftActive) EndChallengeDraft();
            RestoreChallengeDraft(original);
            _challengeDraftCache.Clear();
            foreach (var entry in cached) _challengeDraftCache.Add(entry.Key, entry.Value);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
    }

    /// <summary>Proves public replacement gates and safe draft serialization under actual published worker ownership.</summary>
    public bool VerifyChallengeLockedDraftSave(LadderEditorDocument replacement, string tempSavePath, out string diagnostic)
    {
        if (!IsChallengeDraftActive || !ChallengeMutationLocked)
        { diagnostic = "requires active challenge and published mutation lock"; return false; }
        var json = LadderEditorProjectJson.Save(_ladderDocument);
        var path = _ladderProjectPath;
        var baseline = _ladderSavedProjectJson;
        try
        {
            RevertChallengeStarter(replacement);
            var revertBlocked = LadderEditorProjectJson.Save(_ladderDocument) == json;
            var openBlocked = !TryOpenLadderProject(tempSavePath, out var openDiagnostic)
                && LadderEditorProjectJson.Save(_ladderDocument) == json;
            var saved = TrySaveLadderProject(tempSavePath, out var saveDiagnostic);
            var unchanged = LadderEditorProjectJson.Save(_ladderDocument) == json;
            var copied = saved && LadderEditorProjectJson.Save(LadderEditorProjectJson.Load(System.IO.File.ReadAllText(tempSavePath)).Document!) == json;
            diagnostic = $"revertBlocked={revertBlocked} openBlocked={openBlocked} save={saved} exactLogic={unchanged} copied={copied}; {openDiagnostic}; {saveDiagnostic}";
            return revertBlocked && openBlocked && saved && unchanged && copied;
        }
        finally
        {
            _ladderProjectPath = path;
            _ladderSavedProjectJson = baseline;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
    }

    private void RefreshChallengeIdentity()
    {
        if (!IsChallengeDraftActive) return;
        _activeSceneName = ChallengeDisplayName;
        _sceneTitle.Text = ChallengeDisplayName;
        _sceneTitle.TooltipText = _challengeDefinition?.Id ?? string.Empty;
        _transportScene.Text = $"{ChallengeDisplayName} · {(ActiveRuntimeRunning ? "RUNNING" : "STOPPED")}";
        _ladderWorkspaceStatus.Text = _ladderMonitorMatchesLoadedProgram
            ? $"CHALLENGE DRAFT · LIVE SCAN {_virtualSnapshot?.ScanNumber ?? 0}"
            : "UNSOLVED CHALLENGE DRAFT · learner-owned logic · not verified";
    }

    private void SetChallengeChrome(Control control, bool visible, Vector2? minimum = null)
    {
        _challengeChromeDefaults.TryAdd(control, (control.Visible, control.CustomMinimumSize));
        control.Visible = visible;
        if (minimum.HasValue) control.CustomMinimumSize = minimum.Value;
    }

    private void ApplyChallengeChrome()
    {
        var basePath = "Workspace/LadderWorkspace/LadderMargin/LadderBody";
        if (GetNodeOrNull<Control>($"{basePath}/LadderHeader") is { } header) SetChallengeChrome(header, false);
        foreach (var environment in new[] { "TIA Portal", "Studio 5000" })
        {
            var env = $"{basePath}/LadderEnvironmentTabs/{environment}";
            if (GetNodeOrNull<Control>($"{env}/VendorChrome") is { } chrome) SetChallengeChrome(chrome, false);
            var work = $"{env}/Workbench/EditorAndTasks/EditorAndInspector";
            if (GetNodeOrNull<Control>($"{work}/BottomDockHost") is { } bottom) SetChallengeChrome(bottom, false);
            var program = $"{work}/ProgramEditor";
            if (GetNodeOrNull<Control>($"{program}/InstructionToolbarChrome") is { } palette) SetChallengeChrome(palette, true, new Vector2(0, 44));
            var instructions = $"{program}/InstructionToolbarChrome/InstructionToolbar";
            foreach (var name in new[] { "Selection", "InstructionCategory" })
                if (GetNodeOrNull<Control>($"{instructions}/{name}") is { } control) SetChallengeChrome(control, false);
            if (GetNodeOrNull<TabContainer>($"{instructions}/InstructionPalette") is { } tabs)
            {
                _challengeTabDefaults.TryAdd(tabs, tabs.CurrentTab);
                tabs.CurrentTab = 0;
                SetChallengeChrome(tabs, true, new Vector2(0, 36));
                if (tabs.GetChild(0) is Control bit)
                    foreach (var child in bit.GetChildren().OfType<Control>())
                        SetChallengeChrome(child, ChallengePresenterModel.IsPaletteInstruction(child.Name.ToString()));
            }
        }
    }

    private void ApplyChallengeLayout()
    {
        if (_challengePresenter is null || !_challengePresenter.Visible) return;
        var top = ToolbarClearanceOffset();
        _challengePresenter.Position = new Vector2(8, top);
        var height = Math.Max(ChallengePanelHeight, _challengePresenter.GetCombinedMinimumSize().Y);
        _challengePresenter.Size = new Vector2(GetViewport().GetVisibleRect().Size.X - 16, height);
        _ladderWorkspace.OffsetTop = top + height + 8;
        _splitDivider.OffsetTop = top + height + 8;
        _ladderWorkspace.OffsetBottom = -58;
        _splitDivider.OffsetBottom = -58;
        _leftDock.Visible = false;
        _rightDock.Visible = false;
        _diagnosticsDock.Visible = false;
        ApplyChallengeChrome();
        RefreshChallengeIdentity();
    }
}
