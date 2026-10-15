using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// Exact, local-only edit qualification for every Siemens-facing catalog key.
/// This drives the production canvas and form controls through viewport input;
/// it does not represent execution in an installed copy of TIA Portal.
/// </summary>
public partial class SimulatorShell
{
    public async Task<(bool Passed, string Detail)> VerifyTiaUniversalEditSweepAsync(string? evidencePath = null)
    {
        var originalDocument = _ladderDocument.CaptureSnapshot();
        var originalHistory = _ladderHistory;
        var originalSaved = _ladderSavedProjectJson;
        var originalPath = _ladderProjectPath;
        var temporaryFiles = new List<string>();
        var results = new List<object>();
        var failures = new List<string>();
        var physicalInputRoutes = new HashSet<string>(StringComparer.Ordinal);

        async Task Frames(int count = 2)
        {
            for (var frame = 0; frame < count; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        void Refresh()
        {
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        async Task<bool> PointerClick(Control control)
        {
            await Frames();
            var rect = control.GetGlobalRect();
            if (control.GetViewport() is Window controlWindow && controlWindow != GetWindow())
                GD.Print($"TIA_EDIT_SUBWINDOW_POINTER control={control.Name} rect={rect} transformed={control.GetGlobalTransformWithCanvas() * (control.Size / 2)} visible={controlWindow.GetVisibleRect()} windowPosition={controlWindow.Position}");
            if (!control.IsVisibleInTree() || control is BaseButton { Disabled: true }
                || rect.Size.X <= 0 || rect.Size.Y <= 0
                || !control.GetViewport().GetVisibleRect().HasPoint(rect.GetCenter()))
                return false;
            var at = rect.GetCenter();
            var viewport = control.GetViewport();
            physicalInputRoutes.Add("Viewport.PushInput");
            viewport.PushInput(new InputEventMouseMotion { Position = at, GlobalPosition = at }, true);
            viewport.PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left,
                Pressed = true, Position = at, GlobalPosition = at,
            }, true);
            viewport.PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left, ButtonMask = (MouseButtonMask)0,
                Pressed = false, Position = at, GlobalPosition = at,
            }, true);
            await Frames(3);
            return true;
        }

        async Task<bool> ActivateButton(Button button)
        {
            if (button.GetViewport() is not Window subwindow || subwindow == GetWindow())
                return await PointerClick(button);
            button.GrabFocus();
            await Frames();
            physicalInputRoutes.Add("Window.PushInput:Enter");
            subwindow.PushInput(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true }, true);
            subwindow.PushInput(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false }, true);
            await Frames(3);
            return true;
        }

        try
        {
            GetWindow().Size = new Vector2I(2200, 1200);
            foreach (var pending in _pendingEditorForms.ToArray()) pending.Discard();
            SetReviewState("ladder", "");
            _ladderEnvironmentTabs.CurrentTab = 0;
            await Frames(3);

            var host = GetNode<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal");
            T Find<T>(string name) where T : Node => host.FindChild(name, true, false) as T
                ?? throw new InvalidOperationException("Missing production edit control '" + name + "'.");
            var canvas = Find<LadderEditorCanvas>("GraphicalLadderCanvas");
            Node? scrollNode = canvas.GetParent();
            while (scrollNode is not null && scrollNode is not ScrollContainer) scrollNode = scrollNode.GetParent();
            var scroll = scrollNode as ScrollContainer
                ?? throw new InvalidOperationException("TIA production canvas has no scroll container.");
            var propertyPopup = Find<Control>("InstructionPropertiesPopup");
            var applyProperties = Find<Button>("ApplyInstructionProperties");
            var cancelProperties = Find<Button>("CancelInstructionProperties");
            var propertyStatus = Find<Label>("InstructionPropertyStatus");
            var undoButton = Find<Button>("LadderUndo");
            var redoButton = Find<Button>("LadderRedo");
            var instructionPalette = Find<TabContainer>("InstructionPalette");

            var catalogKeys = LadderInstructionCatalog.Entries.Select(entry => entry.Key).ToArray();
            Require(catalogKeys.GroupBy(key => key, StringComparer.Ordinal).All(group => group.Count() == 1),
                "Authoritative instruction catalog contains duplicate keys.");
            Require(Main.TiaSweepLocalOnlyKeys.All(key => catalogKeys.Contains(key, StringComparer.Ordinal)),
                "An explicit local-only exclusion is absent from the authoritative catalog.");
            var keys = catalogKeys.Where(key => !Main.TiaSweepLocalOnlyKeys.Contains(key)).ToArray();
            Require(keys.Length == Main.TiaSweepExpectedKeys
                && keys.Distinct(StringComparer.Ordinal).Count() == Main.TiaSweepExpectedKeys,
                $"Expected {Main.TiaSweepExpectedKeys} unique Siemens-facing edit keys; found {keys.Length}.");

            static string TargetIdentity(TiaSweepFixture fixture, LadderEditorDocument document)
            {
                var rung = document.Rungs.FirstOrDefault(item => item.Id == fixture.Document.Rungs[fixture.RungIndex].Id)
                    ?? throw new InvalidOperationException("Target network identity disappeared.");
                return fixture.TargetKind switch
                {
                    TiaSweepTargetKind.Node => EditableLadderTree.Find(rung.Root, fixture.TargetId)?.Id ?? string.Empty,
                    TiaSweepTargetKind.Terminal => rung.TerminalActionId,
                    TiaSweepTargetKind.Label => rung.NetworkLabelId,
                    TiaSweepTargetKind.Network => rung.Id,
                    _ => string.Empty,
                };
            }

            void SelectTarget(TiaSweepFixture fixture)
            {
                var rungId = fixture.Document.Rungs[fixture.RungIndex].Id;
                var rungIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == rungId);
                Require(rungIndex >= 0, "Target network is absent from the live document.");
                switch (fixture.TargetKind)
                {
                    case TiaSweepTargetKind.Node: canvas.SelectTreeNode(rungIndex, fixture.TargetId); break;
                    case TiaSweepTargetKind.Terminal: canvas.SelectElement(rungIndex, -1, -1, output: true); break;
                    case TiaSweepTargetKind.Label: canvas.SelectNetworkLabel(rungIndex); break;
                    case TiaSweepTargetKind.Network: canvas.SelectRung(rungIndex); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            async Task<bool> OpenPhysicalForm(TiaSweepFixture fixture, string key)
            {
                SelectTarget(fixture);
                LadderEditorZoom.RevealSelection(canvas, scroll);
                await Frames(3);
                var offset = key.StartsWith("compare-", StringComparison.Ordinal)
                    ? new Vector2(-60, 0)
                    : key.StartsWith("numeric-", StringComparison.Ordinal) ? new Vector2(-50, 0) : Vector2.Zero;
                var localPoint = canvas.GetSelectionPosition() + offset * canvas.ZoomFactor;
                var viewportPoint = canvas.GetGlobalTransformWithCanvas() * localPoint;
                if (!canvas.GetGlobalRect().HasPoint(viewportPoint)
                    || !scroll.GetGlobalRect().HasPoint(viewportPoint)
                    || !GetViewport().GetVisibleRect().HasPoint(viewportPoint))
                    return false;
                void Mouse(bool pressed, bool doubleClick = false) => GetViewport().PushInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0,
                    Pressed = pressed, DoubleClick = doubleClick,
                    Position = viewportPoint, GlobalPosition = viewportPoint,
                }, true);
                GetViewport().PushInput(new InputEventMouseMotion
                    { Position = viewportPoint, GlobalPosition = viewportPoint }, true);
                Mouse(true); Mouse(false);
                await Frames();
                Mouse(true, doubleClick: true); Mouse(false);
                await Frames(3);
                return key == "call" ? _callAuthoring is { Visible: true } : propertyPopup.Visible;
            }

            static void SelectOption(OptionButton selector, string text)
            {
                var index = Enumerable.Range(0, selector.ItemCount)
                    .FirstOrDefault(item => selector.GetItemText(item) == text, -1);
                if (index < 0) { selector.AddItem(text); index = selector.ItemCount - 1; }
                selector.Select(index);
                selector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
            }

            static void SetText(LineEdit field, string value)
            {
                field.Text = value;
                field.EmitSignal(LineEdit.SignalName.TextChanged, value);
            }

            void StageProperty(string key, bool valid)
            {
                if (key is "no" or "nc" or "edge-rising" or "edge-falling" or "branch")
                    SelectOption(Find<OptionButton>("ContactTagSelector"), valid ? "alternate" : "missing_bool");
                else if (key.StartsWith("compare-", StringComparison.Ordinal))
                    SetText(Find<LineEdit>("CompareRightOperand"), valid ? "1" : "missing_number");
                else if (key is "coil" or "set" or "reset")
                    SelectOption(Find<OptionButton>("CoilTagSelector"), valid ? "bool_memory" : "missing_bool");
                else if (key is "ton" or "tof" or "tp" or "rto")
                    SetText(Find<LineEdit>("TimerPresetOperand"), valid ? "T#2s" : "not_time");
                else if (key == "timer-reset")
                    SelectOption(Find<OptionButton>("TimerTagSelector"), valid ? "timer_alt" : "missing_timer");
                else if (key is "ctu" or "ctd")
                    SelectOption(Find<OptionButton>("CounterTagSelector"), valid ? "counter_alt" : "missing_counter");
                else if (key.StartsWith("numeric-", StringComparison.Ordinal))
                    SetText(Find<LineEdit>("NumericSourceA"), valid ? "1" : "missing_number");
                else if (key == "return")
                {
                    SelectOption(Find<OptionButton>("ReturnValueSelector"), "Value");
                    SetText(Find<LineEdit>("ReturnValueOperand"), valid ? "alternate" : "missing_bool");
                }
                else if (key is "jump" or "label")
                    SetText(Find<LineEdit>("ProgramControlLabel"), valid ? "FinishAlt" : "1bad");
                else throw new InvalidOperationException($"No staged property edit exists for '{key}'.");
            }

            bool AppliedPropertyValue(string key, TiaSweepFixture fixture)
            {
                var rungId = fixture.Document.Rungs[fixture.RungIndex].Id;
                var rung = _ladderDocument.Rungs.Single(item => item.Id == rungId);
                var node = fixture.TargetKind == TiaSweepTargetKind.Node
                    ? EditableLadderTree.Find(rung.Root, fixture.TargetId) : null;
                if (key is "no" or "nc" or "edge-rising" or "edge-falling" or "branch") return node?.Variable == "alternate";
                if (key.StartsWith("compare-", StringComparison.Ordinal)) return node?.RightOperand == "1";
                if (key is "coil" or "set" or "reset") return rung.CoilVariable == "bool_memory";
                if (key is "ton" or "tof" or "tp" or "rto") return rung.TimerPresetOperand == "T#2s";
                if (key == "timer-reset") return rung.TimerVariable == "timer_alt";
                if (key is "ctu" or "ctd") return rung.CounterVariable == "counter_alt";
                if (key.StartsWith("numeric-", StringComparison.Ordinal)) return rung.NumericSourceA == "1";
                if (key == "return") return rung.ReturnValueKind == LadderReturnValueKind.Operand && rung.ReturnValueOperand == "alternate";
                if (key == "jump") return rung.ProgramControlLabel == "FinishAlt";
                if (key == "label") return rung.NetworkLabelName == "FinishAlt" && rung.ProgramControlLabel == "FinishAlt";
                return false;
            }

            async Task<bool> ReopenedPropertyValue(string key)
            {
                if (key is "no" or "nc" or "edge-rising" or "edge-falling" or "branch")
                    return Find<OptionButton>("ContactTagSelector").GetItemText(Find<OptionButton>("ContactTagSelector").Selected) == "alternate";
                if (key.StartsWith("compare-", StringComparison.Ordinal)) return Find<LineEdit>("CompareRightOperand").Text == "1";
                if (key is "coil" or "set" or "reset")
                    return Find<OptionButton>("CoilTagSelector").GetItemText(Find<OptionButton>("CoilTagSelector").Selected) == "bool_memory";
                if (key is "ton" or "tof" or "tp" or "rto") return Find<LineEdit>("TimerPresetOperand").Text == "T#2s";
                if (key == "timer-reset")
                    return Find<OptionButton>("TimerTagSelector").GetItemText(Find<OptionButton>("TimerTagSelector").Selected) == "timer_alt";
                if (key is "ctu" or "ctd")
                    return Find<OptionButton>("CounterTagSelector").GetItemText(Find<OptionButton>("CounterTagSelector").Selected) == "counter_alt";
                if (key.StartsWith("numeric-", StringComparison.Ordinal)) return Find<LineEdit>("NumericSourceA").Text == "1";
                if (key == "return") return Find<LineEdit>("ReturnValueOperand").Text == "alternate";
                if (key is "jump" or "label") return Find<LineEdit>("ProgramControlLabel").Text == "FinishAlt";
                await Task.CompletedTask;
                return false;
            }

            async Task<bool> ClickPalette(string buttonName)
            {
                var button = Find<Button>(buttonName);
                Node? page = button.GetParent();
                while (page?.GetParent() != instructionPalette) page = page?.GetParent();
                if (page is null) return false;
                instructionPalette.CurrentTab = page.GetIndex();
                await Frames(3);
                return await PointerClick(button);
            }

            foreach (var key in keys)
            {
                var fixture = Main.BuildTiaSweepFixture(key);
                var editFixture = fixture;
                if (key == "branch")
                {
                    var branchContact = EditableLadderTree.Visit(fixture.Document.Rungs[fixture.RungIndex].Root)
                        .First(node => node.Kind == LadderNodeKind.Contact);
                    editFixture = new TiaSweepFixture(fixture.Document, fixture.RungIndex,
                        TiaSweepTargetKind.Node, branchContact.Id, []);
                }
                // Alternate declarations are part of the case baseline, not a
                // hidden side effect of the edit under qualification.
                fixture.Document.AddTag("timer_alt", PlcVariableRole.Memory, type: PlcVariableType.Timer);
                fixture.Document.AddTag("counter_alt", PlcVariableRole.Memory, type: PlcVariableType.Counter);
                if (key == "timer-reset")
                    fixture.Document.AddTimerRung("alternate timer owner", "timer_alt", TimeSpan.FromSeconds(2));
                if (key == "jump") fixture.Document.AddLabelRung("alternate destination", "FinishAlt");
                _ladderDocument.RestoreSnapshot(fixture.Document.CaptureSnapshot());
                _ladderHistory = new LadderEditorHistory(150);
                _ladderSavedProjectJson = LadderEditorProjectJson.Save(_ladderDocument);
                _ladderProjectPath = null;
                foreach (var pending in _pendingEditorForms.ToArray()) pending.Discard();
                Refresh();
                await Frames(3);

                var baselineJson = LadderEditorProjectJson.Save(_ladderDocument);
                var baselineSnapshot = _ladderDocument.CaptureSnapshot();
                string CaseIdentity() => key == "branch"
                    ? TargetIdentity(fixture, _ladderDocument) + "|" + TargetIdentity(editFixture, _ladderDocument)
                    : TargetIdentity(fixture, _ladderDocument);
                var baselineIdentity = CaseIdentity();
                var invalidMode = key == "rung" ? "not-representable" : "validated-form";
                var invalidApplicable = key != "rung";
                bool? invalidRejected = invalidApplicable ? false : null;
                var opened = false;
                var applied = false;
                var stableIdentity = false;
                var compiled = false;
                var saveLoadSaveExact = false;
                var undoExact = false;
                var redoExact = false;
                var reopened = false;
                var cleanupExact = false;
                string appliedJson = string.Empty;
                try
                {
                    if (key == "call")
                    {
                        opened = await OpenPhysicalForm(fixture, key);
                        Require(opened && _callAuthoring is not null, "CALL form did not open through the production canvas.");
                        var callForm = _callAuthoring!;
                        var input = _ladderDocument.Blocks.Single(block => block.Name == "ReviewFB").Interface
                            .Single(formal => formal.Name == "EnableIn");
                        var kind = (OptionButton)callForm.FindChild("ActualKind_" + input.Id, true, false);
                        var literal = (LineEdit)callForm.FindChild("ActualLiteral_" + input.Id, true, false);
                        var literalIndex = kind.GetItemIndex((int)LadderActualKind.Literal);
                        kind.Select(literalIndex); kind.EmitSignal(OptionButton.SignalName.ItemSelected, (long)literalIndex);
                        literal.Text = "not_bool";
                        await ActivateButton((Button)callForm.FindChild("ApplyCall", true, false));
                        var invalidStatus = ((Label)callForm.FindChild("CallStatus", true, false)).Text;
                        invalidRejected = callForm.Visible && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                            && !_ladderHistory.CanUndo
                            && invalidStatus.Contains("BOOL", StringComparison.OrdinalIgnoreCase);
                        GD.Print($"TIA_EDIT_CALL_INVALID rejected={invalidRejected} status={invalidStatus}");
                        await ActivateButton((Button)callForm.FindChild("CancelCall", true, false));
                        Require(!callForm.Visible, "CALL invalid form did not cancel through production keyboard input.");
                        Require(await OpenPhysicalForm(fixture, key) && _callAuthoring is not null,
                            "CALL form did not reopen for a valid transaction.");
                        callForm = _callAuthoring!;
                        kind = (OptionButton)callForm.FindChild("ActualKind_" + input.Id, true, false);
                        literal = (LineEdit)callForm.FindChild("ActualLiteral_" + input.Id, true, false);
                        literalIndex = kind.GetItemIndex((int)LadderActualKind.Literal);
                        kind.Select(literalIndex); kind.EmitSignal(OptionButton.SignalName.ItemSelected, (long)literalIndex);
                        literal.Text = "true";
                        ((SpinBox)callForm.FindChild("InstanceDbNumber", true, false)).Value = 77;
                        ((LineEdit)callForm.FindChild("InstanceDbName", true, false)).Text = "ReviewFB_Edit_DB";
                        await ActivateButton((Button)callForm.FindChild("ApplyCall", true, false));
                        var call = _ladderDocument.Rungs.Single(item => item.Id == fixture.Document.Rungs[fixture.RungIndex].Id);
                        var bound = call.CallBindings.Single(binding => binding.ParameterId == input.Id);
                        var instance = _ladderDocument.Instances.Single(item => item.Id == call.CallInstanceId);
                        applied = !callForm.Visible && bound.Kind == LadderActualKind.Literal
                            && Equals(bound.Literal, true) && instance.InstanceDataBlock == new LadderInstanceDataBlock(77, "ReviewFB_Edit_DB")
                            && _ladderHistory.CanUndo;
                    }
                    else if (key == "rung")
                    {
                        SelectTarget(fixture);
                        opened = true;
                        var clicked = await ClickPalette(key == "branch" ? "AddBranch" : "AddRung");
                        applied = clicked && LadderEditorProjectJson.Save(_ladderDocument) != baselineJson && _ladderHistory.CanUndo;
                    }
                    else
                    {
                        opened = await OpenPhysicalForm(editFixture, key);
                        Require(opened, $"{key}: property form did not open through the production canvas.");
                        StageProperty(key, valid: false);
                        await PointerClick(applyProperties);
                        invalidRejected = propertyPopup.Visible && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                            && !_ladderHistory.CanUndo && !string.IsNullOrWhiteSpace(propertyStatus.Text);
                        await PointerClick(cancelProperties);
                        Require(await OpenPhysicalForm(editFixture, key), $"{key}: property form did not reopen for a valid edit.");
                        StageProperty(key, valid: true);
                        await PointerClick(applyProperties);
                        applied = !propertyPopup.Visible && AppliedPropertyValue(key, editFixture) && _ladderHistory.CanUndo;
                    }

                    Require(!invalidApplicable || invalidRejected == true,
                        $"{key}: malformed staged input was not rejected atomically.");
                    Require(applied, $"{key}: valid physical edit did not commit exactly once.");
                    stableIdentity = CaseIdentity() == baselineIdentity;
                    var compile = LadderCompiler.Compile(_ladderDocument.BuildProgram());
                    compiled = compile.IsValid;
                    Require(stableIdentity, $"{key}: target identity changed after Apply.");
                    Require(compiled, $"{key}: applied edit failed compilation: "
                        + string.Join(" | ", compile.Issues.Select(issue => issue.Path + " " + issue.Message)));

                    appliedJson = LadderEditorProjectJson.Save(_ladderDocument);
                    var tempPath = Path.Combine(Path.GetTempPath(), "rungproof-tia-edit-" + key + "-" + Guid.NewGuid().ToString("N") + ".json");
                    temporaryFiles.Add(tempPath);
                    File.WriteAllText(tempPath, appliedJson);
                    var loaded = LadderEditorProjectJson.Load(File.ReadAllText(tempPath));
                    Require(loaded.IsReadable && loaded.Document is not null,
                        $"{key}: Save/Load returned diagnostics: {string.Join(" | ", loaded.Issues.Select(issue => issue.Message))}");
                    var loadedDocument = loaded.Document!;
                    saveLoadSaveExact = LadderEditorProjectJson.Save(loadedDocument) == appliedJson;
                    Require(saveLoadSaveExact, $"{key}: Save/Load/Save was not byte-exact.");

                    Require(await PointerClick(undoButton), $"{key}: physical Undo button was not clickable.");
                    undoExact = LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                        && !_ladderHistory.CanUndo && CaseIdentity() == baselineIdentity;
                    Require(await PointerClick(redoButton), $"{key}: physical Redo button was not clickable.");
                    redoExact = LadderEditorProjectJson.Save(_ladderDocument) == appliedJson
                        && CaseIdentity() == baselineIdentity;
                    Require(undoExact && redoExact, $"{key}: physical Undo/Redo did not restore exact serialized states.");

                    _ladderDocument.RestoreSnapshot(loadedDocument.CaptureSnapshot());
                    _ladderHistory.Clear();
                    Refresh();
                    await Frames(3);
                    if (key == "rung")
                    {
                        SelectTarget(fixture);
                        reopened = TargetIdentity(fixture, _ladderDocument) == baselineIdentity;
                    }
                    else if (key == "call")
                    {
                        reopened = await OpenPhysicalForm(fixture, key) && _callAuthoring is not null;
                        if (reopened)
                        {
                            var input = _ladderDocument.Blocks.Single(block => block.Name == "ReviewFB").Interface
                                .Single(formal => formal.Name == "EnableIn");
                            var reopenedCall = _callAuthoring!;
                            var callKind = (OptionButton)reopenedCall.FindChild("ActualKind_" + input.Id, true, false);
                            var callLiteral = (LineEdit)reopenedCall.FindChild("ActualLiteral_" + input.Id, true, false);
                            reopened = callKind.GetSelectedId() == (int)LadderActualKind.Literal && callLiteral.Text == "True"
                                && ((SpinBox)reopenedCall.FindChild("InstanceDbNumber", true, false)).Value == 77
                                && ((LineEdit)reopenedCall.FindChild("InstanceDbName", true, false)).Text == "ReviewFB_Edit_DB";
                            await ActivateButton((Button)reopenedCall.FindChild("CancelCall", true, false));
                        }
                    }
                    else
                    {
                        var reopenOpened = await OpenPhysicalForm(editFixture, key);
                        var reopenValue = reopenOpened && await ReopenedPropertyValue(key);
                        if (key is "jump" or "label")
                            GD.Print($"TIA_EDIT_PROGRAM_CONTROL_REOPEN key={key} opened={reopenOpened} value={Find<LineEdit>("ProgramControlLabel").Text} matched={reopenValue}");
                        reopened = reopenValue;
                        if (propertyPopup.Visible) await PointerClick(cancelProperties);
                    }
                    Require(reopened, $"{key}: saved edit did not reopen through its production form/selection path.");

                    _ladderDocument.RestoreSnapshot(baselineSnapshot);
                    _ladderHistory.Clear();
                    Refresh();
                    await Frames(2);
                    cleanupExact = LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                        && CaseIdentity() == baselineIdentity
                        && LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
                    Require(cleanupExact, $"{key}: case cleanup did not restore its exact compiled baseline.");

                    results.Add(new
                    {
                        gateId = $"tia.{key}.edit", key, status = "passed", invalidMode, invalidApplicable,
                        invalidRejected, opened, applied, stableIdentity, compiled,
                        saveLoadSaveExact, undoExact, redoExact, reopened, cleanupExact,
                    });
                    GD.Print($"TIA_EDIT_GATE_CASE PASS key={key} gate=tia.{key}.edit invalid={invalidMode} saveLoadSave=true undoRedo=true reopen=true");
                }
                catch (Exception error)
                {
                    failures.Add(key + ": " + error.Message);
                    GD.PushError("TIA_EDIT_GATE_CASE FAIL key=" + key + " " + error.Message);
                }
                finally
                {
                    if (_callAuthoring is { Visible: true }) _callAuthoring.CancelEdit();
                    if (propertyPopup.Visible) cancelProperties.EmitSignal(BaseButton.SignalName.Pressed);
                    _pendingEditorDialog?.Hide();
                    _ladderDocument.RestoreSnapshot(baselineSnapshot);
                    _ladderHistory.Clear();
                    Refresh();
                    await Frames();
                }
            }

            Require(results.Count == Main.TiaSweepExpectedKeys,
                $"Expected {Main.TiaSweepExpectedKeys} edit gate results; recorded {results.Count}.");
            Require(failures.Count == 0, failures.Count == 0 ? string.Empty
                : $"{failures.Count} edit gates failed; first failure: {failures[0]}");

            if (!string.IsNullOrWhiteSpace(evidencePath))
            {
                var output = Path.GetFullPath(evidencePath);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                var artifact = new
                {
                    schemaVersion = 2,
                    resultKind = "tia-edit-gate-sweep",
                    createdUtc = DateTimeOffset.UtcNow.ToString("O"),
                    status = "passed",
                    offlineOnly = true,
                    installedTiaExecuted = false,
                    livePlcExecuted = false,
                    physicalInputRoutes = physicalInputRoutes.OrderBy(route => route, StringComparer.Ordinal).ToArray(),
                    keys,
                    keyCount = keys.Length,
                    caseCount = results.Count,
                    lifecycle = new[] { "invalid-reject-where-representable", "valid-apply", "stable-id", "compile", "save-load-save", "toolbar-undo-redo", "reopen", "cleanup" },
                    cases = results,
                };
                File.WriteAllText(output, JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }));
            }
            GD.Print($"TIA_EDIT_GATE_SWEEP_PASS keys={keys.Length} cases={results.Count} externalTia=false");
            return (true, $"keys={keys.Length} cases={results.Count} physical=Viewport.PushInput externalTia=false");
        }
        catch (Exception error)
        {
            GD.PushError("TIA_EDIT_GATE_SWEEP_FAIL externalTia=false " + error);
            return (false, error.Message + (failures.Count == 0 ? string.Empty : " | " + string.Join(" | ", failures.Take(5))));
        }
        finally
        {
            foreach (var pending in _pendingEditorForms.ToArray()) pending.Discard();
            _callAuthoring?.CancelEdit();
            _pendingEditorDialog?.Hide();
            _ladderDocument.RestoreSnapshot(originalDocument);
            _ladderHistory = originalHistory;
            _ladderSavedProjectJson = originalSaved;
            _ladderProjectPath = originalPath;
            Refresh();
            foreach (var path in temporaryFiles)
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
