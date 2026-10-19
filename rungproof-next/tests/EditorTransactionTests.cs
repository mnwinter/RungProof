using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterEditorTransactionTests()
    {
        Test("Tag rename updates only resolved symbolic CALL actuals with exact undo and persistence", () =>
        {
            var document = CreateTagCallBindingDocument();
            var before = LadderEditorProjectJson.Save(document);
            var history = new LadderEditorHistory();

            Equal(1, document.CountTagReferences("start_command"));
            Equal(1, document.CountTagReferences("assembly"));
            history.Execute(document, "Rename tag references", () =>
            {
                document.UpdateTag("start_command", "cycle_start", PlcVariableType.Bool,
                    PlcVariableRole.Memory, initialValue: false);
                document.UpdateTag("assembly", "renamed_assembly", PlcVariableType.Struct,
                    PlcVariableRole.Memory,
                    aggregate: new PlcAggregateSchema(Fields: [new("ready", PlcVariableType.Bool)]),
                    initialValue: new Dictionary<string, object> { ["ready"] = false });
            });

            var callBindings = document.Rungs[0].CallBindings;
            Equal("global:cycle_start", callBindings[0].Symbol);
            Equal("global:stable-tag-id", callBindings[1].Symbol);
            Equal("global:renamed_assembly.ready", callBindings[2].Symbol);
            Equal("global:start_command", callBindings[3].Symbol); // Literal payload is opaque.
            Equal("global:missing-tag", callBindings[4].Symbol); // Unresolved symbolic references stay untouched.
            Equal("global:start_command", callBindings[5].Symbol); // Discard payload is opaque.
            Equal("global:assembly.unknown", callBindings[6].Symbol); // Invalid aggregate leaf is not rewritten.
            Equal(0, document.CountTagReferences("start_command"));
            Equal(1, document.CountTagReferences("cycle_start"));
            Equal(1, document.CountTagReferences("renamed_assembly"));

            var renamed = LadderEditorProjectJson.Save(document);
            var reopened = LadderEditorProjectJson.Load(renamed);
            True(reopened.IsReadable);
            Equal(renamed, LadderEditorProjectJson.Save(reopened.Document!));

            True(history.Undo(document, out _));
            Equal(before, LadderEditorProjectJson.Save(document));
            True(history.Redo(document, out _));
            Equal(renamed, LadderEditorProjectJson.Save(document));
        });
        Test("CALL symbolic tag actual blocks deletion but literal and unresolved actuals do not", () =>
        {
            var document = CreateTagCallBindingDocument();
            False(document.TryRemoveTag("start_command", out var references));
            Equal(1, references);
            True(document.Tags.Any(tag => tag.Name == "start_command"));

            document.Rungs[0].CallBindings = document.Rungs[0].CallBindings
                .Where((_, index) => index != 0).ToArray();
            Equal(0, document.CountTagReferences("start_command"));
            True(document.TryRemoveTag("start_command", out references));
            Equal(0, references);
            False(document.Tags.Any(tag => tag.Name == "start_command"));

            False(document.TryRemoveTag("assembly", out references));
            Equal(1, references);
        });

        static LadderEditorDocument CreateTagCallBindingDocument()
        {
            var document = new LadderEditorDocument();
            document.AddBlock("Main", LadderBlockType.OrganizationBlock);
            var worker = document.AddBlock("Worker", LadderBlockType.Function);
            worker.SemanticMode = LadderSemanticMode.Parameterized;
            var formalIds = new[]
            {
                document.AddInterfaceParameter(worker.Id, "fallback", PlcVariableType.Bool, LadderInterfaceSection.Input).Id,
                document.AddInterfaceParameter(worker.Id, "stable", PlcVariableType.Bool, LadderInterfaceSection.Input).Id,
                document.AddInterfaceParameter(worker.Id, "aggregate", PlcVariableType.Bool, LadderInterfaceSection.Input).Id,
                document.AddInterfaceParameter(worker.Id, "literal", PlcVariableType.Bool, LadderInterfaceSection.Input).Id,
                document.AddInterfaceParameter(worker.Id, "unresolved", PlcVariableType.Bool, LadderInterfaceSection.Input).Id,
                document.AddInterfaceParameter(worker.Id, "discard", PlcVariableType.Bool, LadderInterfaceSection.Output).Id,
                document.AddInterfaceParameter(worker.Id, "invalid_leaf", PlcVariableType.Bool, LadderInterfaceSection.Input).Id,
            };
            document.AddTag("start_command", PlcVariableRole.Memory);
            document.AddTag("stable_tag", PlcVariableRole.Memory);
            document.Tags[1] = document.Tags[1] with { Id = "stable-tag-id" };
            document.AddAggregateTag("assembly", PlcVariableRole.Memory, PlcVariableType.Struct,
                new PlcAggregateSchema(Fields: [new("ready", PlcVariableType.Bool)]),
                new Dictionary<string, object> { ["ready"] = false });
            var call = document.AddCallRung("Call worker", worker.Id);
            call.CallSemanticMode = LadderSemanticMode.Parameterized;
            call.CallBindings =
            [
                new(formalIds[0], LadderActualKind.Symbol, "global:start_command"),
                new(formalIds[1], LadderActualKind.Symbol, "global:stable-tag-id"),
                new(formalIds[2], LadderActualKind.Symbol, "global:assembly.ready"),
                new(formalIds[3], LadderActualKind.Literal, "global:start_command", Literal: false, LiteralType: PlcVariableType.Bool),
                new(formalIds[4], LadderActualKind.Symbol, "global:missing-tag"),
                new(formalIds[5], LadderActualKind.Discard, "global:start_command"),
                new(formalIds[6], LadderActualKind.Symbol, "global:assembly.unknown"),
            ];
            return document;
        }

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
