using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterRecursiveEditorTests()
    {
        Test("BlockViewStateRetainsStableSelectionAndScroll", () =>
        {
            var memory = new LadderEditorViewMemory();
            var first = new LadderEditorViewState("network-a", "leaf-a", false, "series-a", 2, 318, 412, 75);
            var second = new LadderEditorViewState("network-b", "", true, "", -1, 802, 16, 150);
            memory.Remember("project", "block-a", first); memory.Remember("project", "block-b", second);
            True(memory.TryRestore("project", "block-a", out var restored)); Equal(first, restored!);
            True(memory.TryRestore("project", "block-b", out restored)); Equal(second, restored!);
            False(memory.TryRestore("different-project", "block-a", out _));
        });
        Test("LadderZoomGeometryPreservesLogicalTargets", () =>
        {
            foreach (var percent in new[] { 75, 100, 150 })
            {
                var zoom = new LadderEditorZoomGeometry(percent);
                Equal(192.0f, zoom.ToLogical(zoom.ToCanvas(192)));
                Equal(512.0f, zoom.ToLogical(zoom.ToCanvas(512)));
                True(zoom.ToCanvas(1000) == percent * 10);
                var start = zoom.ToCanvas(800); var end = zoom.ToCanvas(1100);
                var scroll = LadderEditorZoomGeometry.Reveal(0, 500, start, end);
                True(start >= scroll && end <= scroll + 500);
            }
            foreach (var invalid in new[] { 0, 50, 101, 200 })
            {
                try { _ = new LadderEditorZoomGeometry(invalid); throw new Exception("Unsupported zoom accepted"); }
                catch (ArgumentOutOfRangeException) { }
            }
        });
        Test("RecursiveNavigationIncludesTerminalAndSlots", () =>
        {
            var root = EditableLadderTree.Import(new("root", LadderNodeKind.Series, Children: [
                new("choice", LadderNodeKind.Parallel, Children: [new("leaf", LadderNodeKind.Contact, "A")])]));
            var layout = EditableLadderLayout.Measure(root); var order = layout.Navigation();
            True(order[^1].IsTerminal); Equal(1, order.Count(n => n.IsTerminal));
            Equal("root", order[0].NodeId); True(order.Any(n => n.NodeId == "leaf" && !n.IsInsertion));
            foreach (var slot in layout.Slots)
                True(order.Any(n => n.IsInsertion && n.ParentId == slot.ParentId && n.Slot == slot.ChildIndex));
            False(layout.Navigation(false).Any(n => n.IsTerminal));
        });
        Test("LegacyIgnoredEmptyPathsHaveExplicitLayoutDisposition", () =>
        {
            var root = EditableLadderTree.Import(new("root", LadderNodeKind.Parallel, Children: [
                new("active", LadderNodeKind.Series, Children: [new("leaf", LadderNodeKind.Contact, "A")]),
                new("draft", LadderNodeKind.Series, Children: [])]));
            var legacy = EditableLadderLayout.Measure(root, legacyEmptyBranchSemantics: true);
            var ignored = legacy.Nodes.Single(n => n.Node.Id == "draft"); True(ignored.IgnoredLegacy);
            False(legacy.Wires.Any(w => w.Y1 == ignored.ContinuityY && w.Y2 == ignored.ContinuityY
                && w.X1 == ignored.Bounds.X && w.X2 == ignored.Bounds.X + ignored.Bounds.Width));
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(EditableLadderTree.Export(root)));
            d.Rungs[0].LegacyEmptyBranchSemantics = true; var h = new LadderEditorHistory();
            var target = new LadderTreeEditorTarget(d.Revision, "main", "network", "draft");
            True(LadderTreeEditorCommands.Execute(d, h, new(LadderTreeCommandId.ConvertLegacy, target)).Applied);
            False(EditableLadderLayout.Measure(d.Rungs[0].Root, legacyEmptyBranchSemantics: d.Rungs[0].LegacyEmptyBranchSemantics).Nodes.Single(n => n.Node.Id == "draft").IgnoredLegacy);
            True(h.Undo(d, out _)); True(d.Rungs[0].LegacyEmptyBranchSemantics);
            True(EditableLadderLayout.Measure(d.Rungs[0].Root, legacyEmptyBranchSemantics: d.Rungs[0].LegacyEmptyBranchSemantics).Nodes.Single(n => n.Node.Id == "draft").IgnoredLegacy);
        });
        Test("DeletedTerminalNeverExportsHiddenAction", () =>
        {
            var wire = new LadderNode("wire", LadderNodeKind.Series, Children: []);
            foreach (var network in RecursiveTerminalFixtures(wire))
            {
                var d = new LadderEditorDocument();
                var p = RecursiveFixture(wire) with { Networks = [network], Blocks = [new("main", "Main", [network])] };
                d.ReplaceFromProgram(p); var h = new LadderEditorHistory(); var template = d.CaptureRung(0);
                var target = EditorTarget.Capture(d, 0, -1, -1, EditorOperandRole.Output);
                True(LadderEditorCommands.Execute(d, h, new(EditorCommandId.Delete, target)).Applied);
                Equal(0, RecursiveTerminalCount(d.BuildProgram().Blocks![0].Networks[0]));
                False(LadderCompiler.Compile(d.BuildProgram()).IsValid);
                d.ReplaceOutput(0, template);
                Equal(1, RecursiveTerminalCount(d.BuildProgram().Blocks![0].Networks[0]));
                True(d.Rungs[0].HasTerminalAction);
            }
        });
        Test("NestedLegacyContactCommandsUseAtomicTreePath", () =>
        {
            var root = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("a", LadderNodeKind.Contact, "A"),
                new("choice", LadderNodeKind.Parallel, Children: [new("path", LadderNodeKind.Series, Children: [new("b", LadderNodeKind.Contact, "B")])])]);
            foreach (var command in new[] { EditorCommandId.Delete, EditorCommandId.MoveRight, EditorCommandId.Paste })
            {
                var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(root)); var h = new LadderEditorHistory();
                var branch = d.Rungs[0].Branches.FindIndex(b => b.Id == "root");
                var target = EditorTarget.Capture(d, 0, branch, 0, EditorOperandRole.Contact);
                var request = new EditorCommandRequest(command, target, new EditorContactClipboard(new("copied", "B", false)));
                var result = LadderEditorCommands.Execute(d, h, request);
                True(result.Applied); True(h.CanUndo); Equal("terminal", d.Rungs[0].TerminalActionId);
                True(EditableLadderTree.Find(d.Rungs[0].Root, "b") is not null);
                True(h.Undo(d, out _)); Equal(RecursiveShape(root), RecursiveShape(EditableLadderTree.Export(d.Rungs[0].Root)));
                False(h.CanUndo);
            }
        });
        Test("UnsupportedRecursiveImportNamesExactPath", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(new("valid", LadderNodeKind.Contact, "A")));
            var before = d.CaptureSnapshot();
            var malformed = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("bad-leaf", LadderNodeKind.Contact, "A", Children: [new("hidden", LadderNodeKind.Contact, "B")])]);
            try { d.ReplaceFromProgram(RecursiveFixture(malformed)); throw new Exception("Unsupported graph accepted"); }
            catch (ArgumentException error) { True(error.Message.Contains("blocks[main].networks[0].logic.children[0]", StringComparison.Ordinal)); }
            Equal(RecursiveShape(EditableLadderTree.Export(before.Blocks[0].Rungs[0].Root)), RecursiveShape(EditableLadderTree.Export(d.Rungs[0].Root)));
        });
        Test("RecursiveCommandsRejectStaleAndLockedTargets", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(new("leaf", LadderNodeKind.Contact, "A")));
            var history = new LadderEditorHistory();
            var target = new LadderTreeEditorTarget(d.Revision, "main", "network", "leaf");
            False(LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.Delete, target), true).Applied);
            False(history.CanUndo);
            True(LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.WrapParallel, target)).Applied);
            False(LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.Delete, target)).Applied);
            Equal(1, EditableLadderTree.Visit(d.Rungs[0].Root).Count(n => n.Id == "leaf"));
        });
        Test("RecursiveMoveAndRootPruneAreAtomic", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(new("root", LadderNodeKind.Series, Children: [
                new("a", LadderNodeKind.Contact, "A"), new("b", LadderNodeKind.Contact, "B")])));
            var history = new LadderEditorHistory();
            LadderTreeEditorTarget Target(string id) => new(d.Revision, "main", "network", id);
            True(LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.MoveRight, Target("a"))).Applied);
            Equal("b,a", string.Join(",", d.Rungs[0].Root.Children!.Select(n => n.Id)));
            var before = d.CaptureSnapshot();
            False(LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.PasteAt,
                Target("a") with { ParentId = "a", Slot = 0 }, d.Rungs[0].Root)).Applied);
            Equal(RecursiveShape(EditableLadderTree.Export(before.Blocks[0].Rungs[0].Root)), RecursiveShape(EditableLadderTree.Export(d.Rungs[0].Root)));
            True(LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.Delete, Target("root"))).Applied);
            Equal(LadderNodeKind.Series, d.Rungs[0].Root.Kind); Equal(0, d.Rungs[0].Root.Children!.Count);
            True(history.Undo(d, out _)); Equal("b,a", string.Join(",", d.Rungs[0].Root.Children!.Select(n => n.Id)));
        });
        Test("PasteRegeneratesAllIds", () =>
        {
            var d = new LadderEditorDocument();
            d.ReplaceFromProgram(RecursiveFixture(new("root", LadderNodeKind.Series, Children: [new("contact", LadderNodeKind.Contact, "A")])));
            var template = d.CaptureRung(0); var pasted = d.PasteRung(template, 1);
            var oldIds = EditableLadderTree.Visit(template.Root).Select(n => n.Id).Append(template.Id).Append(template.TerminalActionId).ToHashSet();
            var newIds = EditableLadderTree.Visit(pasted.Root).Select(n => n.Id).Append(pasted.Id).Append(pasted.TerminalActionId).ToArray();
            False(newIds.Any(oldIds.Contains)); Equal(newIds.Length, newIds.Distinct().Count());
        });
        Test("UnrelatedIdsSurviveEdit", () =>
        {
            var root = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("a", LadderNodeKind.Contact, "A"), new("b", LadderNodeKind.Contact, "B")]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(root));
            var history = new LadderEditorHistory();
            history.Execute(d, "Nested branch", () => d.WrapParallel(0, "a"));
            Equal("root", d.Rungs[0].Root.Id); True(EditableLadderTree.Find(d.Rungs[0].Root, "b") is not null);
            Equal(1, EditableLadderTree.Visit(d.Rungs[0].Root).Count(n => n.Id == "a"));
            True(history.Undo(d, out _)); Equal(RecursiveShape(root), RecursiveShape(d.BuildProgram().Blocks![0].Networks[0].Logic));
            True(history.Redo(d, out _)); Equal(1, EditableLadderTree.Visit(d.Rungs[0].Root).Count(n => n.Id == "a"));
        });
        Test("NestedLegacyMutationRejectsWithoutFlattening", () =>
        {
            var root = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("choice", LadderNodeKind.Parallel, Children: [new("a", LadderNodeKind.Contact, "A")]),
                new("b", LadderNodeKind.Contact, "B")]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(root));
            try { d.Rungs[0].Branches.Add(new("extra")); throw new Exception("Nested flat mutation accepted"); }
            catch (InvalidOperationException error) { True(error.Message.Contains("subtree", StringComparison.Ordinal)); }
            Equal(RecursiveShape(root), RecursiveShape(d.BuildProgram().Blocks![0].Networks[0].Logic));
        });
        Test("NestedEdgeVisitedOnce", () =>
        {
            var logic = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("choice", LadderNodeKind.Parallel, Children: [
                    new("a", LadderNodeKind.Contact, "A"), new("b", LadderNodeKind.Contact, "B")]),
                new("edge", LadderNodeKind.Contact, "A", EdgeMode: LadderEdgeMode.Rising)]);
            var before = RecursiveFixture(logic);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(before);
            var after = d.BuildProgram();
            Equal("a,b,edge", string.Join(",", RecursiveLeaves(after.Blocks![0].Networks[0].Logic).Select(n => n.Id)));
            Equal(1, RecursiveLeaves(after.Blocks[0].Networks[0].Logic).Count(n => n.Id == "edge"));
            Equal(RecursiveShape(logic), RecursiveShape(after.Blocks[0].Networks[0].Logic));
            var originalRuntime = Runtime(before); var editedRuntime = Runtime(after);
            originalRuntime.Run(); editedRuntime.Run();
            foreach (var values in new[] { (false, false), (true, true), (true, false), (false, true), (true, true) })
            {
                var input = Inputs(("A", values.Item1), ("B", values.Item2));
                Equal(originalRuntime.Scan(input).Outputs["Q"], editedRuntime.Scan(input).Outputs["Q"]);
            }
        });
        Test("NestedEmptyShapesPreserved", () =>
        {
            var root = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("wire", LadderNodeKind.Series, Children: []),
                new("false", LadderNodeKind.Parallel, Children: [])]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(root));
            Equal(RecursiveShape(root), RecursiveShape(d.BuildProgram().Blocks![0].Networks[0].Logic));
        });
        Test("RecursiveImportPreservesAllInstructionIds", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(new("leaf", LadderNodeKind.Contact, "A")));
            var n = d.BuildProgram().Blocks![0].Networks[0];
            Equal("network", n.Id); Equal("terminal", n.Coil!.Id); Equal("leaf", n.Logic.Id);
        });
        Test("Depth32Accepted33Rejected", () =>
        {
            LadderNode Deep(int depth)
            {
                LadderNode node = new("deep-leaf", LadderNodeKind.Contact, "A");
                for (var i = 1; i < depth; i++) node = new("depth-" + i, LadderNodeKind.Series, Children: [node]);
                return node;
            }
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(Deep(32)));
            Equal(RecursiveShape(Deep(32)), RecursiveShape(d.BuildProgram().Blocks![0].Networks[0].Logic));
            var before = d.CaptureSnapshot();
            try { d.ReplaceFromProgram(RecursiveFixture(Deep(33))); throw new Exception("Depth 33 accepted"); }
            catch (ArgumentException error) { True(error.Message.Contains("32", StringComparison.Ordinal)); }
            Equal(before.Id, d.Id);
            Equal(RecursiveShape(Deep(32)), RecursiveShape(d.BuildProgram().Blocks![0].Networks[0].Logic));
        });
        Test("LabelReturnJumpRemainTerminalActions", () =>
        {
            var root = new LadderNode("wire", LadderNodeKind.Series, Children: []);
            foreach (var n in new[] {
                new LadderNetwork("label-network", "Label", root, LabelInstruction: new("label-terminal", "L")),
                new LadderNetwork("return-network", "Return", root, Return: new("return-terminal")),
                new LadderNetwork("jump-network", "Jump", root, Jump: new("jump-terminal", "L")) })
            {
                var p = RecursiveFixture(root) with { Networks = [n], Blocks = [new("main", "Main", [n])] };
                var d = new LadderEditorDocument(); d.ReplaceFromProgram(p);
                var actual = d.BuildProgram().Blocks![0].Networks[0];
                Equal(n.Id, actual.Id); Equal(n.Return?.Id ?? string.Empty, actual.Return?.Id ?? string.Empty);
                Equal(n.Jump?.Id ?? string.Empty, actual.Jump?.Id ?? string.Empty); Equal(n.LabelInstruction?.Id ?? string.Empty, actual.LabelInstruction?.Id ?? string.Empty);
                Equal(RecursiveShape(root), RecursiveShape(actual.Logic));
            }
        });
    }

    private static LadderProgram RecursiveFixture(LadderNode root)
    {
        var network = new LadderNetwork("network", "Nested", root, new("terminal", "Q"));
        return new(1, "recursive-fixture", "Recursive", "LD", TimeSpan.FromMilliseconds(20), [
            new("A", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("B", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("Q", PlcVariableType.Bool, PlcVariableRole.Output, false)], [network],
            [new("main", "Main", [network])], "main");
    }
    private static int RecursiveTerminalCount(LadderNetwork n) => new object?[] { n.Coil, n.Timer, n.TimerReset,
        n.Counter, n.CounterReset, n.CounterLoad, n.NumericOperation, n.Call, n.Return, n.Jump, n.LabelInstruction }.Count(a => a is not null);
    private static IEnumerable<LadderNetwork> RecursiveTerminalFixtures(LadderNode logic)
    {
        yield return new("network", "Coil", logic, Coil: new("action", "Q"));
        yield return new("network", "Timer", logic, Timer: new("action", "T", TimeSpan.FromSeconds(1)));
        yield return new("network", "TimerReset", logic, TimerReset: new("action", "T"));
        yield return new("network", "Counter", logic, Counter: new("action", "C", 10));
        yield return new("network", "CounterReset", logic, CounterReset: new("action", "C"));
        yield return new("network", "CounterLoad", logic, CounterLoad: new("action", "C", 10));
        yield return new("network", "Math", logic, NumericOperation: new("action", LadderNumericOperationKind.Move, "0", "0", "N"));
        yield return new("network", "Call", logic, Call: new("action", "helper"));
        yield return new("network", "Return", logic, Return: new("action"));
        yield return new("network", "Jump", logic, Jump: new("action", "L"));
        yield return new("network", "Label", logic, LabelInstruction: new("action", "L"));
    }
    private static IEnumerable<LadderNode> RecursiveLeaves(LadderNode node) =>
        node.Kind is LadderNodeKind.Contact or LadderNodeKind.Compare ? [node]
        : (node.Children ?? []).SelectMany(RecursiveLeaves);
    private static string RecursiveShape(LadderNode node) =>
        $"{node.Id}:{node.Kind}:{node.Variable}:{node.EdgeMode}[{string.Join(",", (node.Children ?? []).Select(RecursiveShape))}]";
}
