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
        Test("ParallelConnectionWireTargetsExactPath", () =>
        {
            var root = EditableLadderTree.Import(new("root", LadderNodeKind.Parallel, Children: [
                new("upper", LadderNodeKind.Series, Children: [new("a", LadderNodeKind.Contact, "A")]),
                new("lower", LadderNodeKind.Series, Children: [new("b", LadderNodeKind.Contact, "B")]) ]));
            var layout = EditableLadderLayout.Measure(root);
            var connection = layout.Wires.Single(wire => wire.FlowNodeId == "lower"
                && wire.FlowSide == LadderTreeWireFlowSide.Input);
            var hit = layout.HitWire((connection.X1 + connection.X2) / 2, (connection.Y1 + connection.Y2) / 2);
            Equal("lower", hit!.FlowNodeId);
            True(layout.HitWire((connection.X1 + connection.X2) / 2, connection.Y1 + 20) is null);
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
        Test("DeletedTerminalNeverExportsHiddenActionAndLabelRemainsMetadata", () =>
        {
            var wire = new LadderNode("wire", LadderNodeKind.Series, Children: []);
            foreach (var network in RecursiveTerminalFixtures(wire))
            {
                var d = new LadderEditorDocument();
                var p = RecursiveFixture(wire) with { Networks = [network], Blocks = [new("main", "Main", [network])] };
                d.ReplaceFromProgram(p); var h = new LadderEditorHistory(); var template = d.CaptureRung(0);
                var target = EditorTarget.Capture(d, 0, -1, -1, EditorOperandRole.Output);
                var deleted = LadderEditorCommands.Execute(d, h, new(EditorCommandId.Delete, target)).Applied;
                if (network.LabelInstruction is not null)
                {
                    False(deleted);
                    False(d.Rungs[0].HasTerminalAction);
                    Equal(1, RecursiveTerminalCount(d.BuildProgram().Blocks![0].Networks[0]));
                    True(LadderCompiler.Compile(d.BuildProgram()).IsValid);
                }
                else
                {
                    True(deleted);
                    Equal(0, RecursiveTerminalCount(d.BuildProgram().Blocks![0].Networks[0]));
                    False(LadderCompiler.Compile(d.BuildProgram()).IsValid);
                    d.ReplaceOutput(0, template);
                    Equal(1, RecursiveTerminalCount(d.BuildProgram().Blocks![0].Networks[0]));
                    True(d.Rungs[0].HasTerminalAction);
                }
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
        Test("OpenBranchUsesExactSelectedWireSlot", () =>
        {
            var logic = new LadderNode("root", LadderNodeKind.Parallel, Children: [
                new("start-path", LadderNodeKind.Series, Children: [new("start", LadderNodeKind.Contact, "A")]),
                new("seal-path", LadderNodeKind.Series, Children: [
                    new("seal", LadderNodeKind.Contact, "A"),
                    new("stop", LadderNodeKind.Contact, "B", NormallyClosed: true),
                    new("photoeye", LadderNodeKind.Contact, "B", NormallyClosed: true)])]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(logic));
            var history = new LadderEditorHistory();
            var target = new LadderTreeEditorTarget(d.Revision, "main", "network", "seal-path", "seal-path", 1);
            var opened = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.OpenParallelAtSlot, target));
            True(opened.Applied); Equal(opened.SelectionId, opened.SelectionParentId); Equal(0, opened.SelectionSlot);

            var lower = d.Rungs[0].Root.Children![1];
            Equal("seal", lower.Children![0].Id);
            var split = lower.Children[1]; Equal(LadderNodeKind.Parallel, split.Kind);
            var originalLeg = split.Children![0];
            Equal("stop,photoeye", string.Join(",", RecursiveLeaves(EditableLadderTree.Export(originalLeg)).Select(n => n.Id)));
            var newLeg = split.Children[1]; Equal(opened.SelectionId, newLeg.Id); Equal(0, newLeg.Children!.Count);
            Equal("start,seal,stop,photoeye", string.Join(",", RecursiveLeaves(d.BuildProgram().Blocks![0].Networks[0].Logic).Select(n => n.Id)));
            var incomplete = LadderCompiler.Compile(d.BuildProgram());
            False(incomplete.IsValid);
            True(incomplete.Issues.Any(issue => issue.Code == "VC003"
                && issue.Message.Contains("Parallel branch is open", StringComparison.Ordinal)));
            True(history.Undo(d, out _)); Equal(RecursiveShape(logic), RecursiveShape(d.BuildProgram().Blocks![0].Networks[0].Logic));

            target = new(d.Revision, "main", "network", "seal-path", "seal-path", 1);
            opened = LadderTreeEditorCommands.Execute(d, history, new(LadderTreeCommandId.OpenParallelAtSlot, target));
            True(opened.Applied);
            var insertTarget = new LadderTreeEditorTarget(d.Revision, "main", "network", opened.SelectionParentId,
                opened.SelectionParentId, opened.SelectionSlot);
            var inserted = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.InsertAt, insertTarget, new("contact-template", LadderNodeKind.Contact, "A")));
            True(inserted.Applied);
            var newPath = EditableLadderTree.Find(d.Rungs[0].Root, opened.SelectionParentId)!;
            Equal("A", EditableLadderTree.Export(newPath.Children![0]).Variable);
            True(newPath.OpenEnd);
            var stillOpen = LadderCompiler.Compile(d.BuildProgram());
            False(stillOpen.IsValid);
            True(stillOpen.Issues.Any(issue => issue.Code == "VC003"
                && issue.Message.Contains("must be closed", StringComparison.Ordinal)));
            var splitAfterInsert = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Single(node => node.Kind == LadderNodeKind.Parallel
                    && node.Children?.Any(child => child.Id == newPath.Id) == true);
            var mainAfterInsert = splitAfterInsert.Children![0];
            var beforeCloseAtCurrentJoin = LadderEditorProjectJson.Save(d);
            var closeAtCurrentJoin = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", newPath.Id, mainAfterInsert.Id,
                        mainAfterInsert.Children!.Count)));
            True(closeAtCurrentJoin.Applied);
            True(!EditableLadderTree.Find(d.Rungs[0].Root, newPath.Id)!.OpenEnd);
            var compiled = LadderCompiler.Compile(d.BuildProgram());
            if (!compiled.IsValid)
                throw new InvalidOperationException(string.Join(" | ", compiled.Issues.Select(issue =>
                    $"{issue.Code} {issue.Path}: {issue.Message}")));
            True(history.Undo(d, out _)); Equal(beforeCloseAtCurrentJoin, LadderEditorProjectJson.Save(d));
        });
        Test("CloseBranchMovesJoinToExactDownstreamSlot", () =>
        {
            var logic = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("lead", LadderNodeKind.Contact, "A"),
                new("stop", LadderNodeKind.Contact, "B", NormallyClosed: true),
                new("photoeye", LadderNodeKind.Contact, "B", NormallyClosed: true)]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(logic));
            var history = new LadderEditorHistory();
            var openTarget = new LadderTreeEditorTarget(d.Revision, "main", "network", "root", "root", 1);
            var opened = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.OpenParallelAtSlot, openTarget));
            True(opened.Applied);
            var insertTarget = new LadderTreeEditorTarget(d.Revision, "main", "network", opened.SelectionId,
                opened.SelectionParentId, 0);
            True(LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.InsertAt, insertTarget,
                    new("contact-template", LadderNodeKind.Contact, "A"))).Applied);

            var branchPath = EditableLadderTree.Find(d.Rungs[0].Root, opened.SelectionId)!;
            var split = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Single(node => node.Kind == LadderNodeKind.Parallel
                    && node.Children?.Any(child => child.Id == branchPath.Id) == true);
            var mainPath = split.Children![0];
            var beforeClose = LadderEditorProjectJson.Save(d);
            var existingIds = EditableLadderTree.Visit(d.Rungs[0].Root).Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            var rejected = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", branchPath.Id, mainPath.Id, 0)));
            False(rejected.Applied); Equal(beforeClose, LadderEditorProjectJson.Save(d));

            var closed = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", branchPath.Id, mainPath.Id, 1)));
            True(closed.Applied);
            var root = d.Rungs[0].Root;
            Equal("lead", root.Children![0].Id);
            Equal(split.Id, root.Children[1].Id);
            Equal("photoeye", root.Children[2].Id);
            var relocatedSplit = root.Children[1];
            Equal("stop", relocatedSplit.Children![0].Children![0].Id);
            Equal(branchPath.Id, relocatedSplit.Children[1].Id);
            True(existingIds.SetEquals(EditableLadderTree.Visit(root).Select(node => node.Id)));
            True(LadderCompiler.Compile(d.BuildProgram()).IsValid);
            var afterClose = LadderEditorProjectJson.Save(d);
            True(history.Undo(d, out _)); Equal(beforeClose, LadderEditorProjectJson.Save(d));
            True(history.Redo(d, out _)); Equal(afterClose, LadderEditorProjectJson.Save(d));
        });
        Test("CloseOnePathOfMultiPathParallelCreatesExplicitNestedJoin", () =>
        {
            var logic = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("lead", LadderNodeKind.Contact, "A"),
                new("stop", LadderNodeKind.Contact, "B", NormallyClosed: true),
                new("photoeye", LadderNodeKind.Contact, "B", NormallyClosed: true)]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(logic));
            var history = new LadderEditorHistory();

            var firstOpen = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.OpenParallelAtSlot,
                    new(d.Revision, "main", "network", "root", "root", 1)));
            True(firstOpen.Applied);
            True(LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.InsertAt,
                    new(d.Revision, "main", "network", firstOpen.SelectionId,
                        firstOpen.SelectionParentId, 0),
                    new("first-template", LadderNodeKind.Contact, "A"))).Applied);
            var outer = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Single(node => node.Kind == LadderNodeKind.Parallel);
            var mainPath = outer.Children![0];
            True(LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", firstOpen.SelectionId,
                        mainPath.Id, mainPath.Children!.Count))).Applied);

            outer = EditableLadderTree.Find(d.Rungs[0].Root, outer.Id)!;
            mainPath = outer.Children![0];
            var firstLowerPathId = outer.Children[1].Id;
            var secondOpen = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.OpenParallelAtSlot,
                    new(d.Revision, "main", "network", outer.Id, outer.Id, 2)));
            True(secondOpen.Applied);
            True(LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.InsertAt,
                    new(d.Revision, "main", "network", secondOpen.SelectionId,
                        secondOpen.SelectionParentId, 0),
                    new("second-template", LadderNodeKind.Contact, "A"))).Applied);
            var beforeClose = LadderEditorProjectJson.Save(d);
            var existingIds = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            False(LadderCompiler.Compile(d.BuildProgram()).IsValid);

            var closed = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", secondOpen.SelectionId,
                        mainPath.Id, 1)));
            True(closed.Applied);
            var root = d.Rungs[0].Root;
            var closedOuter = EditableLadderTree.Find(root, outer.Id)!;
            Equal(2, closedOuter.Children!.Count);
            Equal(mainPath.Id, closedOuter.Children[0].Id);
            Equal(firstLowerPathId, closedOuter.Children[1].Id);
            var restructuredMain = closedOuter.Children[0];
            Equal(2, restructuredMain.Children!.Count);
            Equal("photoeye", restructuredMain.Children[1].Id);
            var nested = restructuredMain.Children[0];
            Equal(LadderNodeKind.Parallel, nested.Kind);
            Equal(2, nested.Children!.Count);
            Equal("stop", nested.Children[0].Children![0].Id);
            Equal(secondOpen.SelectionId, nested.Children[1].Id);
            False(nested.Children[1].OpenEnd);
            True(existingIds.IsSubsetOf(EditableLadderTree.Visit(root).Select(node => node.Id)));
            Equal(existingIds.Count + 2, EditableLadderTree.Visit(root).Count());
            var multiPathCompiled = LadderCompiler.Compile(d.BuildProgram());
            if (!multiPathCompiled.IsValid)
                throw new InvalidOperationException(string.Join(" | ", multiPathCompiled.Issues.Select(issue =>
                    $"{issue.Code} {issue.Path}: {issue.Message}")));
            var afterClose = LadderEditorProjectJson.Save(d);
            True(history.Undo(d, out _)); Equal(beforeClose, LadderEditorProjectJson.Save(d));
            True(history.Redo(d, out _)); Equal(afterClose, LadderEditorProjectJson.Save(d));
        });
        Test("ClosePowerRailBranchMovesJoinEarlier", () =>
        {
            var openLower = new LadderNode("lower", LadderNodeKind.Series, Children: [
                new("seal", LadderNodeKind.Contact, "A")]) { OpenEnd = true };
            var logic = new LadderNode("power-rail-split", LadderNodeKind.Parallel, Children: [
                new("main-path", LadderNodeKind.Series, Children: [
                    new("stop", LadderNodeKind.Contact, "B", NormallyClosed: true),
                    new("photoeye", LadderNodeKind.Contact, "B", NormallyClosed: true)]),
                openLower]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(logic));
            var history = new LadderEditorHistory();
            var beforeClose = LadderEditorProjectJson.Save(d);
            var existingIds = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);

            var closed = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", "lower", "main-path", 1)));
            True(closed.Applied);
            var root = d.Rungs[0].Root;
            Equal(LadderNodeKind.Series, root.Kind);
            Equal(2, root.Children!.Count);
            Equal("power-rail-split", root.Children[0].Id);
            Equal("photoeye", root.Children[1].Id);
            var split = root.Children[0];
            Equal("stop", split.Children![0].Children![0].Id);
            Equal("lower", split.Children[1].Id);
            False(split.Children[1].OpenEnd);
            True(existingIds.IsSubsetOf(EditableLadderTree.Visit(root).Select(node => node.Id)));
            Equal(existingIds.Count + 1, EditableLadderTree.Visit(root).Count());
            True(LadderCompiler.Compile(d.BuildProgram()).IsValid);
            var afterClose = LadderEditorProjectJson.Save(d);
            True(history.Undo(d, out _)); Equal(beforeClose, LadderEditorProjectJson.Save(d));
            True(history.Redo(d, out _)); Equal(afterClose, LadderEditorProjectJson.Save(d));
        });
        Test("MultipleOpenPathsCloseIndependentlyWithoutConsumingSiblings", () =>
        {
            var logic = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("lead", LadderNodeKind.Contact, "A"),
                new("stop", LadderNodeKind.Contact, "B", NormallyClosed: true),
                new("photoeye", LadderNodeKind.Contact, "B", NormallyClosed: true)]);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(RecursiveFixture(logic));
            var history = new LadderEditorHistory();

            LadderTreeEditorResult OpenAndFill(string parentId, int slot)
            {
                var opened = LadderTreeEditorCommands.Execute(d, history,
                    new(LadderTreeCommandId.OpenParallelAtSlot,
                        new(d.Revision, "main", "network", parentId, parentId, slot)));
                True(opened.Applied);
                True(LadderTreeEditorCommands.Execute(d, history,
                    new(LadderTreeCommandId.InsertAt,
                        new(d.Revision, "main", "network", opened.SelectionId,
                            opened.SelectionParentId, 0),
                        new("contact-template", LadderNodeKind.Contact, "A"))).Applied);
                return opened;
            }

            var first = OpenAndFill("root", 1);
            var outer = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Single(node => node.Kind == LadderNodeKind.Parallel);
            var mainPath = outer.Children![0];
            True(LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", first.SelectionId,
                        mainPath.Id, mainPath.Children!.Count))).Applied);

            outer = EditableLadderTree.Find(d.Rungs[0].Root, outer.Id)!;
            var second = OpenAndFill(outer.Id, outer.Children!.Count);
            outer = EditableLadderTree.Find(d.Rungs[0].Root, outer.Id)!;
            var third = OpenAndFill(outer.Id, outer.Children!.Count);
            var beforeAnyClose = LadderEditorProjectJson.Save(d);
            var originalIds = EditableLadderTree.Visit(d.Rungs[0].Root)
                .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            var twoOpen = LadderCompiler.Compile(d.BuildProgram());
            Equal(2, twoOpen.Issues.Count(issue => issue.Code == "VC003"
                && issue.Path.EndsWith(".openEnd", StringComparison.Ordinal)));

            var closeSecond = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", second.SelectionId, mainPath.Id, 1)));
            True(closeSecond.Applied);
            var afterSecondClose = LadderEditorProjectJson.Save(d);
            var remainingOpen = LadderCompiler.Compile(d.BuildProgram());
            Equal(1, remainingOpen.Issues.Count(issue => issue.Code == "VC003"
                && issue.Path.EndsWith(".openEnd", StringComparison.Ordinal)));
            True(EditableLadderTree.Find(d.Rungs[0].Root, third.SelectionId)!.OpenEnd);
            False(EditableLadderTree.Find(d.Rungs[0].Root, second.SelectionId)!.OpenEnd);

            var closeThird = LadderTreeEditorCommands.Execute(d, history,
                new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(d.Revision, "main", "network", third.SelectionId, mainPath.Id, 1)));
            True(closeThird.Applied);
            var finalRoot = d.Rungs[0].Root;
            var finalOuter = EditableLadderTree.Find(finalRoot, outer.Id)!;
            Equal(2, finalOuter.Children!.Count);
            Equal(mainPath.Id, finalOuter.Children[0].Id);
            var finalMain = finalOuter.Children[0];
            Equal(2, finalMain.Children!.Count);
            Equal("photoeye", finalMain.Children[1].Id);
            var newestNested = finalMain.Children[0];
            Equal(LadderNodeKind.Parallel, newestNested.Kind);
            Equal(third.SelectionId, newestNested.Children![1].Id);
            var earlierNested = newestNested.Children[0].Children![0];
            Equal(LadderNodeKind.Parallel, earlierNested.Kind);
            Equal(second.SelectionId, earlierNested.Children![1].Id);
            True(originalIds.IsSubsetOf(EditableLadderTree.Visit(finalRoot).Select(node => node.Id)));
            Equal(originalIds.Count + 4, EditableLadderTree.Visit(finalRoot).Count());
            True(LadderCompiler.Compile(d.BuildProgram()).IsValid);
            var afterBothClose = LadderEditorProjectJson.Save(d);

            True(history.Undo(d, out _)); Equal(afterSecondClose, LadderEditorProjectJson.Save(d));
            True(history.Undo(d, out _)); Equal(beforeAnyClose, LadderEditorProjectJson.Save(d));
            True(history.Redo(d, out _)); Equal(afterSecondClose, LadderEditorProjectJson.Save(d));
            True(history.Redo(d, out _)); Equal(afterBothClose, LadderEditorProjectJson.Save(d));
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
        Test("LabelMetadataAndReturnJumpActionsRoundTrip", () =>
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
                Equal(n.LabelInstruction is null, d.Rungs[0].HasTerminalAction);
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
        $"{node.Id}:{node.Kind}:{node.Variable}:{node.EdgeMode}:open={node.OpenEnd}[{string.Join(",", (node.Children ?? []).Select(RecursiveShape))}]";
}
