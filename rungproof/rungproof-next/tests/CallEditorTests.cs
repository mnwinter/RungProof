using System;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterCallEditorTests()
    {
        (LadderEditorDocument, string, string, string) Fixture()
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var owner = d.Blocks[0];
            var fb = d.AddBlock("Typed worker", LadderBlockType.FunctionBlock);
            fb.Interface.Add(new("Enable", PlcVariableType.Bool, LadderInterfaceSection.Input, false));
            var r = owner.Rungs[0]; r.IsCall = true; r.CallTarget = fb.Id;
            return (d, owner.Id, r.Id, fb.Id);
        }
        Test("CallConversionPreservesGlobalScopeAndIdentity", () =>
        {
            var (d, owner, rung, target) = Fixture(); var fb = d.Blocks.Single(b => b.Id == target);
            var global = d.Tags.First(t => t.Type == PlcVariableType.Bool);
            fb.Interface.Add(new(global.Name, PlcVariableType.Bool, LadderInterfaceSection.Temp, false));
            var body = new EditableRung("scope-rung", "Scope", global.Name); body.SetRoot(new("scope-root", LadderNodeKind.Contact, Variable: global.Name)); fb.Rungs.Add(body);
            var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized();
            Equal("global:" + global.Name, form.Target!.Rungs[0].Root.Variable);
            Equal("global:" + global.Name, form.Target.Rungs[0].CoilVariable);
            Equal(global.Name, fb.Rungs[0].CoilVariable);
        });
        Test("CallBindingWrongDirectionAndTypeFindExactFormal", () =>
        {
            var (d, owner, rung, _) = Fixture(); var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized(); form.CreateInstance("One");
            var id = form.Target!.Interface[0].Id; form.Bind(new(id, LadderActualKind.Literal, Literal: 1L, LiteralType: PlcVariableType.DInt));
            True(form.Validate().Any(i => i.Field == "binding:" + id && i.Message.Contains("exact formal type")));
            form.Bind(new(id, LadderActualKind.Discard)); True(form.Validate().Any(i => i.Field == "binding:" + id && i.Message.Contains("Only Output")));
            False(form.Commit(new(), false, out _));
        });
        Test("CallInterfaceRejectsUnsupportedDirectionsAndTypes", () =>
        {
            True(LadderCallAuthoring.ValidateFormal(LadderBlockType.Function, new("T", PlcVariableType.Timer, LadderInterfaceSection.Static, TimeSpan.Zero)) is not null);
            True(LadderCallAuthoring.ValidateFormal(LadderBlockType.FunctionBlock, new("T", PlcVariableType.Timer, LadderInterfaceSection.Input, TimeSpan.Zero)) is not null);
            True(LadderCallAuthoring.ValidateFormal(LadderBlockType.FunctionBlock, new("T", PlcVariableType.Timer, LadderInterfaceSection.Static, TimeSpan.Zero)) is null);
            True(LadderCallAuthoring.ValidateFormal(LadderBlockType.Function, new("Text", PlcVariableType.String, LadderInterfaceSection.Output, "")) is null);
            True(LadderCallAuthoring.ValidateFormal(LadderBlockType.Function, new("Mode", PlcVariableType.Enum, LadderInterfaceSection.Input, "Run", EnumMembers: new[] { "Stop" })) is not null);
        });
        Test("CallConversionLeavesOtherSitesExplicitlyUnresolved", () =>
        {
            var (d, owner, rung, target) = Fixture(); var extra = new EditableRung("second-call", "Other", "") { IsCall = true, CallTarget = target }; d.Blocks[0].Rungs.Add(extra);
            var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized();
            Equal(LadderSemanticMode.Legacy, form.Owner.Rungs.Single(r => r.Id == extra.Id).CallSemanticMode);
            True(LadderCompiler.Validate(form.Draft.BuildProgram()).Any(i => i.Code == "VC_CALL_MODE"));
        });
        Test("ReopenedCallPersistsNewGlobalIdentitiesBeforeBinding", () =>
        {
            foreach (var aggregate in new[] { false, true })
            {
                var (d, owner, rung, _) = Fixture(); var first = new LadderCallAuthoring(d, owner, rung); first.ConvertTargetToParameterized(); first.CreateInstance("One");
                first.Bind(new(first.Target!.Interface[0].Id, LadderActualKind.Literal, Literal: true, LiteralType: PlcVariableType.Bool)); True(first.Commit(new(), false, out _));
                if (aggregate) d.Tags.Add(new("new_value", PlcVariableType.Struct, PlcVariableRole.Memory, new System.Collections.Generic.Dictionary<string, object> { ["enabled"] = true }, Aggregate: new(Fields: [new("enabled", PlcVariableType.Bool)])));
                else d.AddTag("new_value", PlcVariableRole.Memory, initialValue: true);
                var baseline = LadderEditorProjectJson.Save(d); var form = new LadderCallAuthoring(d, owner, rung);
                var tag = form.Draft.Tags.Single(t => t.Name == "new_value"); True(tag.Id.Length > 0);
                Equal(baseline, LadderEditorProjectJson.Save(d));
                var reference = "global:" + tag.Id + (aggregate ? ".enabled" : ""); form.Bind(new(form.Target!.Interface[0].Id, LadderActualKind.Symbol, reference));
                var history = new LadderEditorHistory(); True(form.Commit(history, false, out _));
                d.UpdateTag("new_value", "renamed_value", tag.Type, tag.Role);
                var reopened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d)).Document!; var program = reopened.BuildProgram();
                Equal(aggregate ? "renamed_value.enabled" : "renamed_value", LadderCallBindings.ResolveDeclaration(program, program.Blocks!.Single(b => b.Id == owner), reference)!.Name);
                True(history.Undo(d, out _)); Equal(baseline, LadderEditorProjectJson.Save(d));
                var cancel = new LadderCallAuthoring(d, owner, rung); cancel.Cancel(); Equal(baseline, LadderEditorProjectJson.Save(d));
            }
        });
        Test("ReusedGlobalNameAllocatesDistinctIdentity", () =>
        {
            var (d, owner, rung, _) = Fixture(); d.AddTag("A", PlcVariableRole.Memory, initialValue: false);
            var first = new LadderCallAuthoring(d, owner, rung); first.ConvertTargetToParameterized(); first.CreateInstance("One");
            first.Bind(new(first.Target!.Interface[0].Id, LadderActualKind.Literal, Literal: true, LiteralType: PlcVariableType.Bool)); True(first.Commit(new(), false, out _));
            d.UpdateTag("A", "B", PlcVariableType.Bool, PlcVariableRole.Memory); d.AddTag("A", PlcVariableRole.Memory, initialValue: true);
            var form = new LadderCallAuthoring(d, owner, rung); form.SelectTarget(form.Call.CallTarget);
            var newer = form.Draft.Tags.Single(t => t.Name == "A"); var older = form.Draft.Tags.Single(t => t.Name == "B"); True(newer.Id != older.Id);
            form.Bind(new(form.Target!.Interface[0].Id, LadderActualKind.Symbol, "global:" + newer.Id)); True(form.Commit(new(), false, out _));
            var reopened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d)).Document!; var program = reopened.BuildProgram(); var block = program.Blocks!.Single(b => b.Id == owner);
            Equal(true, (bool)LadderCallBindings.ResolveDeclaration(program, block, "global:" + newer.Id)!.InitialValue);
            Equal(false, (bool)LadderCallBindings.ResolveDeclaration(program, block, "global:" + older.Id)!.InitialValue);
        });
        Test("ScalarIdentityCannotCaptureAggregateLeaf", () =>
        {
            var (d, owner, rung, _) = Fixture(); d.Tags.Add(new("old_structure", PlcVariableType.Struct, PlcVariableRole.Memory, new System.Collections.Generic.Dictionary<string, object> { ["enabled"] = false }, Aggregate: new(Fields: [new("enabled", PlcVariableType.Bool)]), Id: "A"));
            d.AddTag("A.enabled", PlcVariableRole.Memory, initialValue: true);
            var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized();
            True(form.Draft.Tags.Single(t => t.Name == "A.enabled").Id != "A.enabled");
        });
        Test("ReopenedCallBindingExecutesAfterNewGlobalRename", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(TypedCallFixture());
            d.AddTag("new_result", PlcVariableRole.Memory, type: PlcVariableType.Real, initialValue: 0.0);
            var form = new LadderCallAuthoring(d, "main", d.Blocks[0].Rungs[0].Id);
            var id = form.Draft.Tags.Single(t => t.Name == "new_result").Id;
            form.Bind(new("arg", LadderActualKind.Symbol, "global:" + id)); True(form.Commit(new(), false, out _));
            d.UpdateTag("new_result", "renamed_result", PlcVariableType.Real, PlcVariableRole.Memory);
            var reopened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d)).Document!;
            var runtime = Runtime(reopened.BuildProgram()); runtime.Run();
            Equal(7.0, runtime.Scan(Inputs()).NumericVariables["renamed_result"]);
        });
        Test("ExistingConflictingGlobalIdentitiesBlockCallApply", () =>
        {
            var (d, owner, rung, _) = Fixture(); var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized(); form.CreateInstance("One");
            form.Bind(new(form.Target!.Interface[0].Id, LadderActualKind.Literal, Literal: true, LiteralType: PlcVariableType.Bool));
            form.Draft.Tags[1] = form.Draft.Tags[1] with { Id = form.Draft.Tags[0].Id };
            False(form.Commit(new(), false, out _)); True(form.Validate().Any(i => i.Message.Contains("identity", StringComparison.OrdinalIgnoreCase)));
        });
        Test("LegacyConversionIsExplicit", () =>
        {
            var (d, owner, rung, target) = Fixture(); var h = new LadderEditorHistory();
            var form = new LadderCallAuthoring(d, owner, rung); form.SelectTarget(target);
            Equal(LadderSemanticMode.Legacy, form.Target!.SemanticMode);
            form.ConvertTargetToParameterized();
            Equal(LadderSemanticMode.Legacy, d.Blocks.Single(b => b.Id == target).SemanticMode);
            True(form.Draft.Tags.All(t => t.Id.Length > 0));
            True(form.Target!.Interface.All(p => p.Id.Length > 0));
            form.CreateInstance("First"); form.Bind(new(form.Target.Interface[0].Id, LadderActualKind.Literal, Literal: true, LiteralType: PlcVariableType.Bool));
            True(form.Commit(h, false, out var error)); True(h.Undo(d, out _));
            Equal(LadderSemanticMode.Legacy, d.Blocks.Single(b => b.Id == target).SemanticMode);
        });
        Test("MissingBindingNavigatesExactField", () =>
        {
            var (d, owner, rung, _) = Fixture(); var form = new LadderCallAuthoring(d, owner, rung);
            form.ConvertTargetToParameterized(); form.CreateInstance("First");
            Equal("binding:" + form.Target!.Interface[0].Id, form.Validate().First().Field);
        });
        Test("CallFormCancelIsAtomic", () =>
        {
            var (d, owner, rung, _) = Fixture(); var before = LadderEditorProjectJson.Save(d);
            var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized(); form.CreateInstance("Pending"); form.Cancel();
            Equal(before, LadderEditorProjectJson.Save(d)); False(form.Commit(new(), false, out _));
        });
        Test("InstanceRenamePreservesBindings", () =>
        {
            var (d, owner, rung, _) = Fixture(); var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized();
            var id = form.CreateInstance("First"); var binding = new LadderCallBinding(form.Target!.Interface[0].Id, LadderActualKind.Literal, Literal: true, LiteralType: PlcVariableType.Bool);
            form.Bind(binding); form.RenameInstance(id, "Renamed"); Equal(id, form.Call.CallInstanceId); Equal(binding, form.Call.CallBindings.Single());
            Equal("Renamed", form.Draft.Instances.Single().Name);
        });
        Test("TwoInstancesRemainDistinctAfterReopen", () =>
        {
            var (d, owner, rung, _) = Fixture(); var form = new LadderCallAuthoring(d, owner, rung); form.ConvertTargetToParameterized();
            var first = form.CreateInstance("First"); var second = form.CreateInstance("Second"); True(first != second);
            form.Bind(new(form.Target!.Interface[0].Id, LadderActualKind.Literal, Literal: false, LiteralType: PlcVariableType.Bool));
            True(form.Commit(new(), false, out var error));
            var reopened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d)).Document!;
            Equal(2, reopened.Instances.Count); Equal(second, reopened.Blocks.Single(b => b.Id == owner).Rungs.Single(r => r.Id == rung).CallInstanceId);
        });
    }
}

