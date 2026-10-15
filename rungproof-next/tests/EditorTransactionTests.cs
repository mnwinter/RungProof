using System;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterEditorTransactionTests()
    {
        Test("OperandSuggestionsUseDeclaredTypesAndInstanceMembers", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); d.AddTag("count", PlcVariableRole.Memory, type: PlcVariableType.Counter);
            var symbols = LadderOperandSymbols.ForDocument(d);
            True(System.Linq.Enumerable.Any(symbols, s => s.Name == "count.CV" && s.Type == PlcVariableType.DInt && !s.Writable));
            True(System.Linq.Enumerable.Any(symbols, s => s.Name == "count.Q" && s.Type == PlcVariableType.Bool));
            False(System.Linq.Enumerable.Any(symbols, s => s.Name == "unknown"));
            var first = d.Rungs[0].Branches[0].Contacts[0].Variable;
            True(LadderOperandSymbols.Validate(symbols, first, PlcVariableType.Bool, false, false) is null);
            True(LadderOperandSymbols.Validate(symbols, "count.CV", PlcVariableType.Bool, false, false) is not null);
            True(LadderOperandSymbols.Validate(symbols, "count.CV", PlcVariableType.DInt, true, false) is not null);
        });
        Test("ParameterizedOperandsRequireScopedIdentity", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample();
            var block = d.AddBlock("Scoped"); block.SemanticMode = LadderSemanticMode.Parameterized;
            block.Interface.Add(new("signal", PlcVariableType.Bool, LadderInterfaceSection.Input, false, Id: "formal-signal"));
            d.SelectBlock(d.Blocks.Count - 1);
            var symbols = LadderOperandSymbols.ForDocument(d);
            True(System.Linq.Enumerable.Any(symbols, s => s.Name == "local:formal-signal" && !s.Writable));
            False(System.Linq.Enumerable.Any(symbols, s => s.Name == d.Tags[0].Name));
            True(LadderOperandSymbols.Validate(symbols, "local:formal-signal", PlcVariableType.Bool, false, false) is null);
            True(LadderOperandSymbols.Validate(symbols, "local:formal-signal", PlcVariableType.Bool, true, false) is not null);
            block.SemanticMode = LadderSemanticMode.Legacy;
            False(System.Linq.Enumerable.Any(LadderOperandSymbols.ForDocument(d), s => s.Name == "local:formal-signal"));
        });
        Test("EscapeLeavesDocumentAndHistoryUnchanged", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var before = LadderEditorProjectJson.Save(d); var tx = new LadderEditTransaction(d, "Edit title");
            tx.Draft.Rungs[0].Label = "Pending"; tx.Cancel();
            Equal(before, LadderEditorProjectJson.Save(d)); False(h.CanUndo);
            False(tx.TryCommit(h, false, out _));
        });
        Test("FocusLossKeepsPendingDraft", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var old = d.Rungs[0].Label; var tx = new LadderEditTransaction(d, "Edit title"); tx.Draft.Rungs[0].Label = "Pending";
            tx.FocusLost(); True(tx.IsPending); Equal(old, d.Rungs[0].Label); False(h.CanUndo);
            True(tx.TryCommit(h, false, out _)); Equal("Pending", d.Rungs[0].Label);
            True(h.Undo(d, out _)); Equal(old, d.Rungs[0].Label); False(h.CanUndo);
        });
        Test("CreateTagAndAssignIsAtomic", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory(); var count = d.Tags.Count;
            var tx = new LadderEditTransaction(d, "Create and assign");
            tx.Draft.AddTag("new_input", PlcVariableRole.Input);
            var old = tx.Draft.Rungs[0].Branches[0].Contacts[0];
            tx.Draft.ReplaceContact(0, 0, 0, old with { Variable = "new_input" });
            Equal(count, d.Tags.Count); True(tx.TryCommit(h, false, out _)); Equal(count + 1, d.Tags.Count);
            Equal("new_input", d.Rungs[0].Branches[0].Contacts[0].Variable);
            True(h.Undo(d, out _)); Equal(count, d.Tags.Count); Equal(old.Variable, d.Rungs[0].Branches[0].Contacts[0].Variable); False(h.CanUndo);
        });
        Test("InvalidPropertyHasNoPartialEdit", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory(); var before = LadderEditorProjectJson.Save(d);
            var tx = new LadderEditTransaction(d, "Invalid edit"); tx.Draft.Rungs[0].Label = "Pending";
            False(tx.TryCommit(h, false, out var error, _ => "Preset is invalid")); Equal("Preset is invalid", error);
            Equal(before, LadderEditorProjectJson.Save(d)); False(h.CanUndo); True(tx.IsPending);
        });
        Test("StaleOrLockedTransactionCannotCommit", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var tx = new LadderEditTransaction(d, "Edit"); tx.Draft.Rungs[0].Label = "Pending";
            False(tx.TryCommit(h, true, out _)); False(h.CanUndo);
            h.Execute(d, "Other edit", () => d.Rungs[0].Label = "Other");
            False(tx.TryCommit(h, false, out _)); Equal("Other", d.Rungs[0].Label);
        });
        Test("ArrayResizeAndTypeChangesPreservePendingText", () =>
        {
            var draft = new AggregateArrayDraft(PlcVariableType.Int, -2, new[] { "10", "20", "30" });
            draft.Resize(-1, 3); Equal("20", draft.Values[0]); Equal("30", draft.Values[1]); Equal("0", draft.Values[2]);
            draft.Values[1] = "malformed"; draft.Resize(-2, 4); Equal("malformed", draft.Values[2]);
            False(draft.TryChangeType(PlcVariableType.Bool, false)); Equal(PlcVariableType.Int, draft.Type);
            True(draft.TryChangeType(PlcVariableType.Bool, true)); Equal("10", draft.Values[0]);
            Equal("malformed", draft.Values[2]);
            False(LadderEditorDocument.TryParseInitialValue(draft.Type, draft.Values[2], out _, out _));
            True(draft.TryChangeType(PlcVariableType.Real, true)); Equal("10", draft.Values[0]); Equal("malformed", draft.Values[2]);
            draft.Resize(-1, 2); draft.Resize(-2, 4); Equal("10", draft.Values[0]); Equal("malformed", draft.Values[2]);
        });
    }
}
