using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private async void CaptureRecursiveEditorReview(string[] arguments)
    {
        try
        {
            string Option(string name,string fallback)=>arguments.FirstOrDefault(a=>a.StartsWith(name+"=",StringComparison.Ordinal))?.Split('=',2)[1]??fallback;
            ShowRecursiveEditorReview(Option("--review-style","tia"),Option("--review-fixture","nested"),arguments.Contains("--review-terminal"));
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(_capturePath is not null)
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                var error=GetViewport().GetTexture().GetImage().SavePng(_capturePath);
                if(error!=Error.Ok) throw new InvalidOperationException("Review capture failed: "+error);
                GD.Print("RECURSIVE_REVIEW_CAPTURE "+_capturePath); GetTree().Quit(0);
            }
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    /// <summary>Review fixture uses the production editable document and actual canvas.</summary>
    private void ShowRecursiveEditorReview(string style, string fixture, bool revealTerminal = false)
    {
        var document = RecursiveEditorReviewDocument(fixture);
        var overlay = new CanvasLayer { Name = "RecursiveEditorReview", Layer = 100 };
        var background = new PanelContainer { Name = "ReviewBackground" };
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(background); AddChild(overlay);
        var stack = new VBoxContainer(); background.AddChild(stack);
        stack.AddChild(new Label { Text = $"RECURSIVE EDITOR REVIEW · {style} · {fixture} · actual production canvas" });
        var scroll = new ScrollContainer { Name = "ReviewScroll", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        stack.AddChild(scroll);
        var canvas = new LadderEditorCanvas(style == "logix" ? LadderVendorStyle.RockwellLogix : LadderVendorStyle.SiemensTia, document);
        scroll.AddChild(canvas); canvas.RefreshDocument();
        canvas.SelectTreeNode(0, document.Rungs[0].Root.Id);
        canvas.TreeCommandRequested += request =>
        {
            var result = LadderTreeEditorCommands.Execute(document, new LadderEditorHistory(), request);
            if (result.Clipboard is not null) canvas.TreeClipboard = result.Clipboard;
            canvas.RefreshDocument(); canvas.SelectTreeNode(0, result.SelectionId);
            GD.Print($"RECURSIVE_REVIEW_COMMAND command={request.Command} applied={result.Applied} reason={result.Reason}");
        };
        if (revealTerminal) Callable.From(() => scroll.ScrollHorizontal = (int)canvas.CustomMinimumSize.X).CallDeferred();
        GD.Print($"RECURSIVE_REVIEW_READY style={style} fixture={fixture} width={canvas.CustomMinimumSize.X} height={canvas.CustomMinimumSize.Y}");
    }

    private static LadderEditorDocument RecursiveEditorReviewDocument(string fixture)
    {
        LadderNode Contact(string id, string variable) => new(id, LadderNodeKind.Contact, variable);
        var root = new LadderNode("root", LadderNodeKind.Series, Children: [
            new("choices", LadderNodeKind.Parallel, Children: [
                new("upper-path", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
                new("lower-path", LadderNodeKind.Series, Children: [
                    new("nested-choice", LadderNodeKind.Parallel, Children: [
                        new("edge-path", LadderNodeKind.Series, Children: [new("edge", LadderNodeKind.Contact, "Start_request", EdgeMode: LadderEdgeMode.Rising)]),
                        new("empty-wire", LadderNodeKind.Series, Children: [])])])]),
            Contact("permissive", "Process_permissive")]);
        if (fixture is "legacy" or "converted") root = new("root", LadderNodeKind.Parallel, Children: [
            new("active-path", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
            new("ignored-empty-path", LadderNodeKind.Series, Children: [])]);
        if (fixture == "empty") root = new("root", LadderNodeKind.Series, Children: [
            new("wire-true", LadderNodeKind.Series, Children: []), new("parallel-false", LadderNodeKind.Parallel, Children: [])]);
        if (fixture == "long") root = new("root", LadderNodeKind.Series, Children: [
            new("compare", LadderNodeKind.Compare, "Measured_process_value_with_long_qualified_name", CompareOperator: LadderCompareOperator.GreaterOrEqual, RightOperand: "123.456"),
            new("choice", LadderNodeKind.Parallel, Children: [
                new("left", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
                new("right", LadderNodeKind.Series, Children: [Contact("permissive", "Process_permissive")])])]);
        var network = new LadderNetwork("network", "Recursive topology / exact subtree boundaries", root, Coil: new("terminal", "Drive_enable"));
        var program = new LadderProgram(2, "recursive-native-review", "Recursive review", "LD", TimeSpan.FromMilliseconds(20), [
            new("Start_request", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("Process_permissive", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("Drive_enable", PlcVariableType.Bool, PlcVariableRole.Output, false),
            new("Measured_process_value_with_long_qualified_name", PlcVariableType.Real, PlcVariableRole.Memory, 100.0)],
            [network], [new("main", "Recursive review", [network])], "main");
        var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
        document.Rungs[0].LegacyEmptyBranchSemantics = fixture == "legacy";
        if (fixture == "terminal-draft") document.Rungs[0].HasTerminalAction = false;
        return document;
    }
}
