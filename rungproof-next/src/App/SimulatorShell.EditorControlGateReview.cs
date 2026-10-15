using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// Structured local evidence for the seven non-catalog LAD editor controls.
/// These checks execute RungProof production routes only. They do not execute,
/// inspect, or claim a match with an installed copy of Siemens TIA Portal.
/// </summary>
public partial class SimulatorShell
{
    private const string TiaEditorControlCommandId = "tia-editor-control-gate-sweep";

    public async Task<(bool Passed, string Detail)> VerifyTiaEditorControlGatesAsync(string? evidencePath = null)
    {
        var sweepStarted = DateTimeOffset.UtcNow;
        var sourceRevision = ComputeSourceRevision();
        var assemblyPath = typeof(SimulatorShell).Assembly.Location;
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            assemblyPath = ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/RungProof.Next.dll");
        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException("The executing RungProof assembly could not be resolved for evidence identity.", assemblyPath);
        var assemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assemblyPath)));
        var failures = new List<string>();
        var transactionChecks = new Dictionary<string, bool>(StringComparer.Ordinal);
        var commandChecks = new Dictionary<string, bool>(StringComparer.Ordinal);
        var viewChecks = new Dictionary<string, bool>(StringComparer.Ordinal);
        var emptyChecks = new Dictionary<string, bool>(StringComparer.Ordinal);
        var contactChecks = new Dictionary<string, bool>(StringComparer.Ordinal);

        static bool All(Dictionary<string, bool> checks, params string[] ids)
            => ids.All(id => checks.TryGetValue(id, out var passed) && passed);
        static void Observe(Dictionary<string, bool> checks, string id, bool passed) => checks[id] = passed;

        async Task Capture(string component, Func<Task<bool>> run)
        {
            try
            {
                if (!await run()) failures.Add(component + " returned false");
            }
            catch (Exception error)
            {
                failures.Add(component + ": " + error.Message);
                GD.PushError("TIA_EDITOR_CONTROL_COMPONENT_FAIL component=" + component + " " + error);
            }
        }

        var placementPassed = false;
        await Capture("operand-placement", async () =>
        {
            var result = await VerifyOperandEditorPlacementAsync();
            placementPassed = result.Passed;
            return result.Passed;
        });
        await Capture("contact-shortcuts", () => Task.FromResult(
            VerifyContactShortcutGate((id, passed) => Observe(contactChecks, id, passed))));
        await Capture("editor-transactions", () => Task.FromResult(
            VerifyEditorTransactions(out _, (id, passed) => Observe(transactionChecks, id, passed))));
        await Capture("transaction-resolution", () => Task.FromResult(
            VerifyTransactionResolutionGate((id, passed) => Observe(transactionChecks, id, passed))));
        await Capture("editor-command-routes", () => Task.FromResult(
            VerifyEditorCommandRoutes(out _, (id, passed) => Observe(commandChecks, id, passed))));
        await Capture("block-view-state", async () => await VerifyLadderBlockViewStateAsync(
            (id, passed) => Observe(viewChecks, id, passed)));
        await Capture("empty-placeholder", () => Task.FromResult(
            VerifyEmptyInstructionReplacement((id, passed) => Observe(emptyChecks, id, passed))));

        var deleteRoutes = new[] { "coil_context", "coil_toolbar", "coil_keyboard", "counter_context", "counter_toolbar",
            "counter_keyboard", "call_context", "call_toolbar", "call_keyboard" };
        var deletionPassed = deleteRoutes.All(route => All(commandChecks, "OutputDelete_" + route));
        var routeUndoRedoPassed = deleteRoutes.All(route => All(commandChecks,
            "OutputDeleteUndo_" + route, "OutputDeleteRedo_" + route));

        var gateDefinitions = new[]
        {
            Gate("tia.editor.contact-shortcuts", "contact-shortcuts",
                Assertion("visible-focused-f9-no", "visible focused LAD accepts unmodified F9 as NO insertion",
                    All(contactChecks, "FocusedF9InsertsNormallyOpen"), "FocusedF9InsertsNormallyOpen"),
                Assertion("visible-focused-f10-nc", "visible focused LAD accepts unmodified F10 as NC insertion",
                    All(contactChecks, "FocusedF10InsertsNormallyClosed"), "FocusedF10InsertsNormallyClosed"),
                Assertion("focus-and-visibility-ownership", "unfocused and hidden LAD reject the shortcuts",
                    All(contactChecks, "UnfocusedRejectsContactShortcuts", "HiddenRejectsContactShortcuts"),
                    "UnfocusedRejectsContactShortcuts", "HiddenRejectsContactShortcuts"),
                Assertion("modified-keys-not-contact-shortcuts", "modifier-bearing F9/F10 retain their separate meanings",
                    All(contactChecks, "ModifiedF9F10DoNotEmitContactInsertion"), "ModifiedF9F10DoNotEmitContactInsertion"),
                Assertion("atomic-undo", "each insertion is one undoable edit and Undo restores the exact baseline",
                    All(contactChecks, "F9UndoRestoresExactBaseline", "F10UndoRestoresExactBaseline"),
                    "F9UndoRestoresExactBaseline", "F10UndoRestoresExactBaseline")),

            Gate("tia.editor.operand-shortcuts", "operand-shortcuts",
                Assertion("return-first-operand", "Return opens an empty first-operand editor",
                    All(transactionChecks, "ReturnActivatesFirstOperand"), "ReturnActivatesFirstOperand"),
                Assertion("typing-first-operand", "typing opens the first-operand editor with the typed character",
                    All(transactionChecks, "TypingActivatesFirstOperand"), "TypingActivatesFirstOperand"),
                Assertion("f2-selected-operand", "F2 opens the selected operand",
                    All(transactionChecks, "F2OpensInlineOperand"), "F2OpensInlineOperand"),
                Assertion("enter-and-escape", "Enter applies exactly once and Escape leaves document and history unchanged",
                    All(transactionChecks, "ReturnApplyCommitsExactlyOnce", "ReturnApplySingleUndoExact",
                        "EscapeLeavesDocumentAndHistoryUnchanged"),
                    "ReturnApplyCommitsExactlyOnce", "ReturnApplySingleUndoExact", "EscapeLeavesDocumentAndHistoryUnchanged"),
                Assertion("suggest-and-create", "Ctrl+I opens suggestions and Ctrl+Shift+I exposes create-and-assign",
                    All(transactionChecks, "InlineAutocompleteShortcut", "DefineTagShortcutRevealsCreateAction"),
                    "InlineAutocompleteShortcut", "DefineTagShortcutRevealsCreateAction"),
                Assertion("physical-pointer-routing", "viewport pointer routing selects the exact pin or instruction property form",
                    placementPassed, "VerifyOperandEditorPlacementAsync")),

            Gate("tia.editor.transaction-resolution", "transaction-resolution",
                Assertion("failed-apply", "failed Apply does not run the queued continuation",
                    All(transactionChecks, "SyntheticFailedApplyBlocksContinuation"), "SyntheticFailedApplyBlocksContinuation"),
                Assertion("successful-apply", "successful Apply commits one edit then runs the continuation once",
                    All(transactionChecks, "SyntheticSuccessfulApplyContinuesOnce", "ReturnApplyCommitsExactlyOnce", "ReturnApplySingleUndoExact"),
                    "SyntheticSuccessfulApplyContinuesOnce", "ReturnApplyCommitsExactlyOnce", "ReturnApplySingleUndoExact"),
                Assertion("discard", "Discard runs the continuation without applying the draft",
                    All(transactionChecks, "SyntheticDiscardContinuesWithoutMutation"), "SyntheticDiscardContinuesWithoutMutation"),
                Assertion("cancel", "Cancel runs no continuation and preserves document and history",
                    All(transactionChecks, "SyntheticCancelPreservesState"), "SyntheticCancelPreservesState"),
                Assertion("multiple-forms", "multiple pending forms resolve deterministically without partial commit",
                    All(transactionChecks, "SyntheticMultipleFormsRejectBeforeApply", "MultipleFormsRejectBeforeAnyMutation",
                        "InvalidSecondFormHasNoPartialCommit", "MixedEnvironmentBatchHasNoPartialCommit"),
                    "SyntheticMultipleFormsRejectBeforeApply", "MultipleFormsRejectBeforeAnyMutation",
                    "InvalidSecondFormHasNoPartialCommit", "MixedEnvironmentBatchHasNoPartialCommit")),

            Gate("tia.editor.command-integrity", "command-integrity",
                Assertion("target-identity", "context commands preserve the selected slot or element identity",
                    All(commandChecks, "WirePopupPreservesSlot"), "WirePopupPreservesSlot"),
                Assertion("stale-target", "stale popup targets are rejected without document mutation",
                    All(commandChecks, "StalePopupIsRejected"), "StalePopupIsRejected"),
                Assertion("output-isolation", "output copy, paste, and delete preserve unrelated conditions",
                    All(commandChecks, "OutputCopyIsOutputOnly", "OutputPastePreservesConditions") && deletionPassed,
                    "OutputCopyIsOutputOnly", "OutputPastePreservesConditions", "OutputDelete_*"),
                Assertion("route-convergence", "toolbar, context, and keyboard routes converge on the same bounded mutation",
                    deletionPassed, "OutputDelete_{coil,counter,call}_{context,toolbar,keyboard}"),
                Assertion("undo-redo-exact", "Undo and Redo restore exact serialized states",
                    routeUndoRedoPassed, "OutputDeleteUndo_*", "OutputDeleteRedo_*")),

            Gate("tia.editor.view-controls", "view-controls",
                Assertion("cartesian-18", "all 18 representation by information by position combinations execute",
                    All(commandChecks, "TiaViewSettingsCartesianProduct"), "TiaViewSettingsCartesianProduct"),
                Assertion("literal-and-missing-operands", "numeric literals and ??? remain operands in every combination",
                    All(commandChecks, "TiaViewSettingsCartesianProduct"), "TiaViewSettingsCartesianProduct"),
                Assertion("hidden-removes-information", "Hidden yields no supplementary on-operand or below-network information",
                    All(commandChecks, "TiaViewSettingsCartesianProduct", "TiaHideTagInfoSetting"),
                    "TiaViewSettingsCartesianProduct", "TiaHideTagInfoSetting"),
                Assertion("single-information-location", "Show and Hierarchical place information at exactly one selected location",
                    All(commandChecks, "TiaViewSettingsCartesianProduct", "TiaHierarchicalCommentSetting"),
                    "TiaViewSettingsCartesianProduct", "TiaHierarchicalCommentSetting"),
                Assertion("bounded-comment-hierarchy", "structured leaf and parent comments follow the bounded hierarchy rule",
                    All(commandChecks, "TiaStructuredSimpleCommentUsesLeaf", "TiaStructuredHierarchicalCommentUsesBracketedParent"),
                    "TiaStructuredSimpleCommentUsesLeaf", "TiaStructuredHierarchicalCommentUsesBracketedParent")),

            Gate("tia.editor.block-view-state", "block-view-state",
                Assertion("block-a", "block A restores its exact stable target, scroll, and zoom",
                    All(viewChecks, "TIA PortalAStableTargetScrollZoom"), "TIA PortalAStableTargetScrollZoom"),
                Assertion("block-b", "block B restores its exact stable target, scroll, and zoom",
                    All(viewChecks, "TIA PortalBStableTargetScrollZoom"), "TIA PortalBStableTargetScrollZoom"),
                Assertion("pending-navigation-guard", "pending Apply failure or Cancel prevents navigation and state storage",
                    All(viewChecks, "TIA PortalPendingGuardBeforeStore", "TIA PortalPendingCancelDoesNotStoreOrNavigate"),
                    "TIA PortalPendingGuardBeforeStore", "TIA PortalPendingCancelDoesNotStoreOrNavigate"),
                Assertion("deleted-target-fallback", "a deleted saved target falls back to the header and zero scroll",
                    All(viewChecks, "TIA PortalDeletedTargetFallsBackToHeader"), "TIA PortalDeletedTargetFallsBackToHeader"),
                Assertion("scrolled-render", "the restored scrolled viewport still renders instructions",
                    All(viewChecks, "TIA PortalScrolledViewportDrawsInstructions"), "TIA PortalScrolledViewportDrawsInstructions")),

            Gate("tia.editor.empty-placeholder", "empty-placeholder",
                Assertion("production-chooser-route", "the unresolved placeholder opens its chooser through the production route",
                    All(emptyChecks, "ProductionRouteOpensChooser"), "ProductionRouteOpensChooser"),
                Assertion("declared-choice-set", "the chooser contains exactly the declared supported choices",
                    All(emptyChecks, "ChooserContainsDeclaredChoices"), "ChooserContainsDeclaredChoices"),
                Assertion("choice-operation", "each choice creates the exact expected terminal operation",
                    All(emptyChecks, "EveryChoiceCreatesExpectedTerminal"), "EveryChoiceCreatesExpectedTerminal"),
                Assertion("replacement-persistence", "replacement clears the placeholder and persists through save/reload",
                    All(emptyChecks, "ReplacementPersistsWithoutPlaceholder"), "ReplacementPersistsWithoutPlaceholder"),
                Assertion("placeholder-not-executable", "the unresolved placeholder cannot compile or execute as an instruction",
                    All(emptyChecks, "PlaceholderCannotCompile"), "PlaceholderCannotCompile")),
        };

        var completed = DateTimeOffset.UtcNow;
        var gateResults = gateDefinitions.Select(gate => new
        {
            gateId = gate.GateId,
            status = gate.Assertions.All(assertion => assertion.Passed) ? "passed" : "failed",
            assertions = gate.Assertions.Select(assertion => new
            {
                id = assertion.Id,
                requirement = assertion.Requirement,
                status = assertion.Passed ? "passed" : "failed",
                passed = assertion.Passed,
                evidence = assertion.Evidence,
            }).ToArray(),
            commandId = TiaEditorControlCommandId,
            sourceRevision,
            assemblySha256,
            startedUtc = sweepStarted.ToString("O"),
            completedUtc = completed.ToString("O"),
            externalTia = false,
            livePlc = false,
        }).ToArray();
        var passed = gateResults.Length == 7
            && gateResults.Select(gate => gate.gateId).Distinct(StringComparer.Ordinal).Count() == 7
            && gateResults.All(gate => gate.status == "passed");

        var output = string.IsNullOrWhiteSpace(evidencePath)
            ? ProjectSettings.GlobalizePath("res://.tools/tia-editor-control-gates.json")
            : Path.GetFullPath(evidencePath);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var artifact = new
        {
            schemaVersion = 1,
            resultKind = "tia-editor-control-gate-sweep",
            commandId = TiaEditorControlCommandId,
            status = passed ? "passed" : "failed",
            sourceRevision,
            assemblySha256,
            startedUtc = sweepStarted.ToString("O"),
            completedUtc = completed.ToString("O"),
            externalTia = false,
            livePlc = false,
            gateIds = gateResults.Select(gate => gate.gateId).ToArray(),
            gateCount = gateResults.Length,
            passedGateCount = gateResults.Count(gate => gate.status == "passed"),
            failedGateCount = gateResults.Count(gate => gate.status != "passed"),
            gates = gateResults,
            componentFailures = failures,
        };
        File.WriteAllText(output, JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }));
        var detail = $"TIA_EDITOR_CONTROL_GATE_SWEEP_{(passed ? "PASS" : "FAIL")} gates={gateResults.Count(g => g.status == "passed")}/7 assertions={gateResults.Sum(g => g.assertions.Count(a => a.passed))}/{gateResults.Sum(g => g.assertions.Length)} externalTia=false livePlc=false artifact={output}";
        if (passed) GD.Print(detail); else GD.PushError(detail + " components=" + string.Join(" | ", failures));
        return (passed, detail);
    }

    private bool VerifyContactShortcutGate(Action<string, bool> observe)
    {
        var original = _ladderDocument.CaptureSnapshot();
        var originalHistory = _ladderHistory;
        void Check(string id, bool passed) { observe(id, passed); if (!passed) throw new InvalidOperationException(id); }
        try
        {
            SetReviewState("ladder", "");
            _ladderEnvironmentTabs.CurrentTab = 0;
            _ladderDocument.ReplaceFromProgram(LadderEditorDocument.CreateConveyorExample().BuildProgram());
            _ladderHistory = new LadderEditorHistory();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            // The production shell constructs the TIA canvas before the Studio 5000 canvas.
            var canvas = _ladderCanvases.First();

            var probe = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, new LadderEditorDocument());
            AddChild(probe);
            var requests = new List<string>();
            probe.KeyboardInstructionRequested += requests.Add;
            probe.ReleaseFocus();
            probe._ShortcutInput(new InputEventKey { Keycode = Key.F9, Pressed = true });
            probe._ShortcutInput(new InputEventKey { Keycode = Key.F10, Pressed = true });
            Check("UnfocusedRejectsContactShortcuts", requests.Count == 0);
            probe.GrabFocus();
            probe._ShortcutInput(new InputEventKey { Keycode = Key.F9, CtrlPressed = true, Pressed = true });
            probe._ShortcutInput(new InputEventKey { Keycode = Key.F10, ShiftPressed = true, Pressed = true });
            Check("ModifiedF9F10DoNotEmitContactInsertion", requests.Count == 0);
            probe.Hide();
            probe._ShortcutInput(new InputEventKey { Keycode = Key.F9, Pressed = true });
            probe._ShortcutInput(new InputEventKey { Keycode = Key.F10, Pressed = true });
            Check("HiddenRejectsContactShortcuts", requests.Count == 0);
            probe.Free();

            var baseline = LadderEditorProjectJson.Save(_ladderDocument);
            var count = _ladderDocument.Rungs[0].Branches[0].Contacts.Count;
            canvas.SelectInsertionPoint(0, 0, 1); canvas.GrabFocus();
            canvas._ShortcutInput(new InputEventKey { Keycode = Key.F9, Pressed = true });
            Check("FocusedF9InsertsNormallyOpen", _ladderDocument.Rungs[0].Branches[0].Contacts.Count == count + 1
                && !_ladderDocument.Rungs[0].Branches[0].Contacts[1].NormallyClosed);
            Check("F9UndoRestoresExactBaseline", _ladderHistory.CanUndo && TryUndoLadderEdit(out _)
                && !_ladderHistory.CanUndo && LadderEditorProjectJson.Save(_ladderDocument) == baseline);
            canvas.SelectInsertionPoint(0, 0, 1); canvas.GrabFocus();
            canvas._ShortcutInput(new InputEventKey { Keycode = Key.F10, Pressed = true });
            Check("FocusedF10InsertsNormallyClosed", _ladderDocument.Rungs[0].Branches[0].Contacts.Count == count + 1
                && _ladderDocument.Rungs[0].Branches[0].Contacts[1].NormallyClosed);
            Check("F10UndoRestoresExactBaseline", _ladderHistory.CanUndo && TryUndoLadderEdit(out _)
                && !_ladderHistory.CanUndo && LadderEditorProjectJson.Save(_ladderDocument) == baseline);
            return true;
        }
        finally
        {
            _ladderDocument.RestoreSnapshot(original);
            _ladderHistory = originalHistory;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
    }

    private bool VerifyTransactionResolutionGate(Action<string, bool> observe)
    {
        foreach (var form in _pendingEditorForms.ToArray()) form.Discard();
        var baseline = LadderEditorProjectJson.Save(_ladderDocument);
        var baselineCanUndo = _ladderHistory.CanUndo;
        void Check(string id, bool passed) { observe(id, passed); if (!passed) throw new InvalidOperationException(id); }
        void Cleanup(PendingEditorForm form)
        {
            if (form.HasPending()) form.Discard();
            _pendingEditorForms.Remove(form);
            _pendingEditorDialog?.Hide();
            _pendingEditorContinuation = null;
            _pendingEditorNavigationCancelled = null;
        }

        var pending = true; var applyCalls = 0; var continuationCalls = 0;
        var failed = new PendingEditorForm(() => pending, () => { applyCalls++; return false; }, () => pending = false);
        _pendingEditorForms.Add(failed);
        ResolvePendingEditorForms(() => continuationCalls++);
        _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
        Check("SyntheticFailedApplyBlocksContinuation", applyCalls == 1 && continuationCalls == 0 && pending);
        Cleanup(failed);

        pending = true; applyCalls = 0; continuationCalls = 0;
        var applied = new PendingEditorForm(() => pending, () => { applyCalls++; pending = false; return true; }, () => pending = false);
        _pendingEditorForms.Add(applied);
        ResolvePendingEditorForms(() => continuationCalls++);
        _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
        Check("SyntheticSuccessfulApplyContinuesOnce", applyCalls == 1 && continuationCalls == 1 && !pending);
        Cleanup(applied);

        pending = true; var discardCalls = 0; continuationCalls = 0;
        var discarded = new PendingEditorForm(() => pending, () => false, () => { discardCalls++; pending = false; });
        _pendingEditorForms.Add(discarded);
        ResolvePendingEditorForms(() => continuationCalls++);
        _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
        Check("SyntheticDiscardContinuesWithoutMutation", discardCalls == 1 && continuationCalls == 1 && !pending
            && LadderEditorProjectJson.Save(_ladderDocument) == baseline && _ladderHistory.CanUndo == baselineCanUndo);
        Cleanup(discarded);

        pending = true; continuationCalls = 0; var cancelCalls = 0;
        var cancelled = new PendingEditorForm(() => pending, () => false, () => pending = false);
        _pendingEditorForms.Add(cancelled);
        ResolvePendingEditorForms(() => continuationCalls++, () => cancelCalls++);
        _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled);
        Check("SyntheticCancelPreservesState", continuationCalls == 0 && cancelCalls == 1 && pending
            && LadderEditorProjectJson.Save(_ladderDocument) == baseline && _ladderHistory.CanUndo == baselineCanUndo);
        Cleanup(cancelled);

        var firstPending = true; var secondPending = true; applyCalls = 0; continuationCalls = 0;
        var first = new PendingEditorForm(() => firstPending, () => { applyCalls++; firstPending = false; return true; }, () => firstPending = false);
        var second = new PendingEditorForm(() => secondPending, () => { applyCalls++; secondPending = false; return false; }, () => secondPending = false);
        _pendingEditorForms.Add(first); _pendingEditorForms.Add(second);
        ResolvePendingEditorForms(() => continuationCalls++);
        _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
        Check("SyntheticMultipleFormsRejectBeforeApply", applyCalls == 0 && continuationCalls == 0 && firstPending && secondPending
            && LadderEditorProjectJson.Save(_ladderDocument) == baseline && _ladderHistory.CanUndo == baselineCanUndo);
        Cleanup(first); Cleanup(second);
        return true;
    }

    private static (string GateId, string Key, (string Id, string Requirement, bool Passed, string[] Evidence)[] Assertions) Gate(
        string gateId, string key, params (string Id, string Requirement, bool Passed, string[] Evidence)[] assertions)
        => (gateId, key, assertions);

    private static (string Id, string Requirement, bool Passed, string[] Evidence) Assertion(
        string id, string requirement, bool passed, params string[] evidence)
        => (id, requirement, passed, evidence);

}
