using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private async void CaptureEditorMonitoringReview(string[] arguments)
    {
        try
        {
            string Option(string name, string fallback) => arguments.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? fallback;
            var fixture = Option("--review-fixture", "counter");
            var style = Option("--review-style", "tia");
            if (fixture is not ("counter" or "timer" or "math" or "text" or "flow" or "parameter-call"))
                throw new ArgumentException("Unknown monitoring fixture: " + fixture);
            if (style is not ("tia" or "logix")) throw new ArgumentException("Unknown monitoring style: " + style);
            var program = new LadderProgram(1, "monitor-review", "Monitoring review", "LD", TimeSpan.FromMilliseconds(10),
                [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false), new("count", PlcVariableType.Counter, PlcVariableRole.Memory, 0),
                 new("timer", PlcVariableType.Timer, PlcVariableRole.Memory, 0), new("value", PlcVariableType.Real, PlcVariableRole.Memory, 3.0),
                 new("result", PlcVariableType.Real, PlcVariableRole.Memory, 0.0), new("zero", PlcVariableType.Real, PlcVariableRole.Memory, 0.0), new("lamp", PlcVariableType.Bool, PlcVariableRole.Output, false)],
                fixture == "counter" ? [new("n1", "CTU observes 1 / 3 before later LOAD overwrites shared state", new("c1", LadderNodeKind.Contact, "enable"), Counter: new("ctu", "count", 3)),
                    new("n2", "LOAD observes 9 / 9 — separate instruction result", new("c2", LadderNodeKind.Contact, "enable"), CounterLoad: new("load", "count", 9))]
                : fixture == "timer" ? [new("n1", "Timer state before later reset", new("c1", LadderNodeKind.Contact, "enable"), Timer: new("ton", "timer", TimeSpan.FromMilliseconds(100))),
                    new("n2", "Reset timer state after TON", new("c2", LadderNodeKind.Contact, "enable"), TimerReset: new("reset", "timer"))]
                : [new("n1", "Compare consumes 3 before later MOV writes 9", new("compare", LadderNodeKind.Compare, "value", CompareOperator: LadderCompareOperator.Equal, RightOperand: "3"), Coil: new("coil", "lamp")),
                   new("n2", "MOV writes 9", new("c2", LadderNodeKind.Contact, "enable"), NumericOperation: new("move", LadderNumericOperationKind.Move, "9", "0", "value")),
                   new("n3", "DIV zero reports retained destination", new("c3", LadderNodeKind.Contact, "enable"), NumericOperation: new("divide", LadderNumericOperationKind.Divide, "value", "zero", "result"))]);
            LadderNode Enabled(string id) => new(id, LadderNodeKind.Contact, "enable");
            LadderNode Wire(string id) => new(id, LadderNodeKind.Series, Children: []);
            if (fixture == "text")
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("empty_text", PlcVariableType.String, PlcVariableRole.Memory, ""),
                        new("formatted_text", PlcVariableType.String, PlcVariableRole.Memory, "old"),
                        new("invalid_template", PlcVariableType.String, PlcVariableRole.Memory, "{broken") }).ToArray(),
                    Networks = [
                        new("text-empty-net", "MOV stores an empty STRING, not an unavailable value", Enabled("text-empty-enable"),
                            NumericOperation: new("text-empty", LadderNumericOperationKind.Move, "\"\"", "0", "empty_text")),
                        new("text-format-net", "FORMAT_TEXT observes successful text before a later failure clears it", Enabled("text-format-enable"),
                            NumericOperation: new("text-format", LadderNumericOperationKind.FormatText, "value", "\"{0:F1}\"", "formatted_text")),
                        new("text-fault-net", "Invalid format reports failure and cleared destination", Enabled("text-fault-enable"),
                            NumericOperation: new("text-fault", LadderNumericOperationKind.FormatText, "value", "invalid_template", "formatted_text"))]
                };
            if (fixture == "flow")
                program = program with { Networks = [
                    new("flow-branch-net", "Nested series/parallel evaluates NO and NC; SET latches", new("flow-series", LadderNodeKind.Series, Children: [
                        Enabled("flow-no"), new("flow-parallel", LadderNodeKind.Parallel, Children: [
                            new("flow-nc", LadderNodeKind.Contact, "enable", true), Enabled("flow-alternative")])]), Coil: new("flow-set", "lamp", LadderCoilMode.Set)),
                    new("flow-edge-net", "Rising edge RESET fires on the second scan", new("flow-edge", LadderNodeKind.Contact, "enable", EdgeMode: LadderEdgeMode.Rising), Coil: new("flow-reset", "lamp", LadderCoilMode.Reset)),
                    new("flow-jump-net", "JMP is taken; next network is not evaluated", Enabled("flow-jump-enable"), Jump: new("flow-jump", "finish")),
                    new("flow-skipped-net", "Skipped by JMP: no observation may be fabricated", Enabled("flow-skipped-enable"), Coil: new("flow-skipped", "lamp")),
                    new("flow-label-net", "Block-local destination LABEL", Wire("flow-label-wire"), LabelInstruction: new("flow-label", "finish")),
                    new("flow-return-net", "RETURN ends this task after the label", Wire("flow-return-wire"), Return: new("flow-return"))] };
            if (fixture == "parameter-call")
            {
                LadderNetwork Call(string suffix, double input) => new("call-net-" + suffix, "Explicit instance " + suffix + " consumes " + input,
                    Enabled("call-enable-" + suffix), Call: new("call-" + suffix, "review-fb", LadderSemanticMode.Parameterized,
                        [new("input", LadderActualKind.Literal, Literal: input, LiteralType: PlcVariableType.Real),
                         new("output", LadderActualKind.Symbol, "global:result-" + suffix),
                         new("shared", LadderActualKind.Symbol, "global:shared")], "instance-" + suffix));
                program = program with { SchemaVersion = 2,
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "enable"),
                        new("result_a", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "result-a"),
                        new("result_b", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "result-b"),
                        new("shared_total", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "shared")],
                    Networks = [], EntryBlock = "main", Instances = [new("instance-a", "Station A", "review-fb"), new("instance-b", "Station B", "review-fb")],
                    Blocks = [new("main", "Main", [Call("a", 3.0), Call("b", 7.0)]),
                        new("review-fb", "Station accumulator", [
                            new("fb-accumulate-net", "Instance-local retained sum", Wire("fb-accumulate-wire"), NumericOperation: new("fb-accumulate", LadderNumericOperationKind.Add, "local:sum", "local:input", "local:sum")),
                            new("fb-publish-net", "Output copies out only on normal return", Wire("fb-publish-wire"), NumericOperation: new("fb-publish", LadderNumericOperationKind.Move, "local:sum", "0", "local:output")),
                            new("fb-shared-net", "InOut aliases the caller shared value", Wire("fb-shared-wire"), NumericOperation: new("fb-shared", LadderNumericOperationKind.Add, "local:shared", "local:input", "local:shared"))],
                            LadderBlockType.FunctionBlock,
                            [new("Input", PlcVariableType.Real, LadderInterfaceSection.Input, 0.0, Id: "input"),
                             new("Output", PlcVariableType.Real, LadderInterfaceSection.Output, 0.0, Id: "output"),
                             new("Shared", PlcVariableType.Real, LadderInterfaceSection.InOut, 0.0, Id: "shared"),
                             new("Sum", PlcVariableType.Real, LadderInterfaceSection.Static, 0.0, Id: "sum")], LadderSemanticMode.Parameterized)] };
            }
            var compiled = LadderCompiler.Compile(program);
            if (!compiled.IsValid) throw new InvalidOperationException("Monitoring render fixture must compile: " + string.Join("; ", compiled.Issues));
            var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
            if (fixture == "flow") runtime.Scan(new Dictionary<string, bool> { ["enable"] = false });
            var snapshot = runtime.Scan(new Dictionary<string, bool> { ["enable"] = true });
            var capture = snapshot.Observations ?? throw new InvalidOperationException("Fixture did not capture actual execution.");
            void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("Fixture execution: " + message); }
            Require(snapshot.Completion == ControllerCompletion.Completed, "scan did not complete");
            if (fixture == "text")
            {
                Require(snapshot.TextVariables["empty_text"] == "" && snapshot.TextVariables["formatted_text"] == "", "empty/failed STRING destinations");
                Require(Equals(capture.GlobalLast["text-format"].StoredAfter, "3.0"), "instruction-time successful formatted text");
                Require(capture.GlobalLast["text-fault"].WriteDisposition == InstructionWriteDisposition.FailedClearedDestination
                    && capture.GlobalLast["text-fault"].Diagnostics.Count > 0, "format failure diagnostics");
            }
            if (fixture == "flow")
            {
                Require(capture.GlobalLast["flow-edge"].LocalResult && !snapshot.Variables["lamp"], "edge reset progression");
                Require(capture.GlobalLast["flow-jump"].ControlOutcome == "Taken" && capture.GlobalLast["flow-return"].ControlOutcome == "Taken", "control transfer");
                Require(!capture.GlobalLast.ContainsKey("flow-skipped"), "skipped network must remain unobserved");
            }
            if (fixture == "parameter-call")
            {
                Require(snapshot.NumericVariables["result_a"] == 3.0 && snapshot.NumericVariables["result_b"] == 7.0
                    && snapshot.NumericVariables["shared_total"] == 10.0, "distinct instance sums and shared alias");
                Require(snapshot.CallInvocations.Count == 2 && snapshot.CallInvocations.All(c => c.Completion == LadderCallCompletion.Completed
                    && c.Transfers.Any(t => t.Phase == "CopyIn") && c.Transfers.Any(t => t.Phase == "CopyOut")
                    && c.Transfers.Any(t => t.Phase == "AliasResolved")), "actual parameter transfer records");
            }
            var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
            var block = Option("--review-block", "");
            if (block.Length > 0)
            {
                var index = document.Blocks.FindIndex(b => b.Id == block || b.Name == block);
                if (index < 0) throw new ArgumentException("Unknown review block: " + block);
                document.SelectBlock(index);
            }
            var overlay = new CanvasLayer { Name = "EditorMonitoringReview", Layer = 100 };
            var background = new PanelContainer(); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            overlay.AddChild(background); AddChild(overlay);
            var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            background.AddChild(scroll);
            var canvas = new LadderEditorCanvas(style == "logix" ? LadderVendorStyle.RockwellLogix : LadderVendorStyle.SiemensTia, document);
            scroll.AddChild(canvas);
            scroll.GetVScrollBar().ValueChanged += _ => canvas.QueueRedraw();
            scroll.GetHScrollBar().ValueChanged += _ => canvas.QueueRedraw();
            canvas.SetZoomPercent(int.Parse(Option("--review-zoom", "100"))); canvas.SetMonitorSnapshot(snapshot);
            var rungIndex = int.Parse(Option("--review-rung", "0"));
            if (rungIndex < 0 || rungIndex >= document.Rungs.Count) throw new ArgumentOutOfRangeException("--review-rung");
            var selectedNode = Option("--review-node", "");
            if (selectedNode.Length > 0)
            {
                Require(EditableLadderTree.Find(document.Rungs[rungIndex].Root, selectedNode) is not null, "selected node exists");
                canvas.SelectTreeNode(rungIndex, selectedNode, notify: false);
            }
            else canvas.SelectElement(rungIndex, -1, -1, true);
            var context = Option("--review-context", "global");
            if (context == "missing") canvas.SelectObservationContext("deliberately-uncaptured-context");
            else if (context != "global")
            {
                var observed = capture.Contexts.Values.FirstOrDefault(o => o.CallPath.Any(p => p.InstanceId == context));
                if (observed is null) throw new ArgumentException("Unobserved instance context: " + context);
                canvas.SelectObservationContext(LadderInstructionPresentation.ContextKey(observed));
            }
            var state = Option("--review-state", "current");
            if (state == "history-evicted") canvas.SetMonitorPublication(null, ObservationUnavailableReason.HistoryEvicted);
            else if (state != "current") throw new ArgumentException("Unknown review state: " + state);
            if (arguments.Contains("--review-detail")) canvas.ShowInstructionInspector();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            scroll.ScrollVertical = Math.Max(0, (int)canvas.GetRungBounds(rungIndex).Position.Y - 12);
            canvas.QueueRedraw();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print($"MONITOR_FIXTURE_EXECUTED fixture={fixture} style={style} block={document.Blocks[document.ActiveBlockIndex].Id} rung={rungIndex} context={context} state={state} scan={snapshot.ScanNumber} observedKeys={string.Join(',', capture.GlobalSummaries.Select(o => o.CatalogKey).Distinct())}");
            GD.Print($"MONITOR_PRESENTATION visibleInstructionBoxes={canvas.LastDrawnInstructionCount} refreshes={canvas.MonitorPresentationRefreshCount}");
            if (_capturePath is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath);
                if (error != Error.Ok) throw new InvalidOperationException("Monitoring capture failed: " + error);
                GD.Print("EDITOR_MONITORING_CAPTURE " + _capturePath); GetTree().Quit(0);
            }
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private async void VerifyEditorMonitoring()
    {
        try
        {
            var program = new LadderProgram(1, "shared-counter-review", "Shared counter review", "LD", TimeSpan.FromMilliseconds(10),
                [new("input", PlcVariableType.Bool, PlcVariableRole.Input, false), new("count", PlcVariableType.Counter, PlcVariableRole.Memory, 0)],
                [new("first", "Count", new("c1", LadderNodeKind.Contact, "input"), Counter: new("ctu", "count", 3)),
                 new("later", "Later load", new("c2", LadderNodeKind.Contact, "input"), CounterLoad: new("load", "count", 9))]);
            var compiled = LadderCompiler.Compile(program);
            if (!compiled.IsValid) throw new InvalidOperationException("Monitoring fixture must compile.");
            var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
            var snapshot = runtime.Scan(new Dictionary<string, bool> { ["input"] = true });
            var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
            foreach (var style in Enum.GetValues<LadderVendorStyle>())
            {
                var canvas = new LadderEditorCanvas(style, document);
                try
                {
                    canvas.SetMonitorSnapshot(snapshot);
                    var lines = canvas.CounterReadoutLines(document.Rungs[0]);
                    if (!lines.Contains("Current count: 1") || !lines.Contains("Preset / setpoint: 3"))
                        throw new InvalidOperationException("CTU must show its actual instruction-time count 1/preset 3, not later LOAD count 9: " + string.Join(" | ", lines));
                }
                finally { canvas.Free(); }
            }
            VerifyMonitorContextIndications();
            if(_simulatorShell is not null && !_simulatorShell.VerifyInstructionMonitoringRoutes(out var routeResult))throw new InvalidOperationException(routeResult);
            await VerifyMonitoringRefreshCadence();
            GD.Print("EDITOR_MONITORING_VERIFY PASS shared counter and exact-context indications, both styles"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError("EDITOR_MONITORING_VERIFY FAIL " + error); GetTree().Quit(1); }
    }
    private async System.Threading.Tasks.Task VerifyMonitoringRefreshCadence()
    {
        var program=new LadderProgram(1,"cadence","Cadence", "LD",TimeSpan.FromMilliseconds(10),
            [new("input",PlcVariableType.Bool,PlcVariableRole.Input,false),new("output",PlcVariableType.Bool,PlcVariableRole.Output,false)],
            [new("n","Output",new("c",LadderNodeKind.Contact,"input"),Coil:new("o","output"))]);
        var runtime=new VirtualControllerRuntime(LadderCompiler.Compile(program).Program!);runtime.Run();
        var document=new LadderEditorDocument();document.ReplaceFromProgram(program);
        var scroll=new ScrollContainer{Position=new Vector2(20,20),Size=new Vector2(640,360)};
        var canvas=new LadderEditorCanvas(LadderVendorStyle.SiemensTia,document);scroll.AddChild(canvas);AddChild(scroll);canvas.RefreshDocument();
        var origin=new ObservationOrigin(Guid.NewGuid(),Guid.NewGuid(),1,0);
        var started=Time.GetTicksUsec();var published=0;
        try
        {
            while(Time.GetTicksUsec()-started<250000)
            {
                for(var i=0;i<10;i++)
                {
                    var snapshot=runtime.Scan(new Dictionary<string,bool>{["input"]=published%2==0});published++;
                    var identity=origin with{ScanAttemptNumber=published};
                    canvas.SetMonitorPublication(new(origin.SessionId,runtime.ProgramContentId,origin.LoadId,1,published,snapshot.ScanNumber,published,
                        ControllerPublicationKind.ScanCompleted,ControllerCompletion.Completed,ControllerPhaseCompletion.ProgramExecuted,identity,snapshot.Observations!.WithOrigin(identity),snapshot));
                }
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            await ToSignal(GetTree().CreateTimer(.04),SceneTreeTimer.SignalName.Timeout);
            var elapsed=Time.GetTicksUsec()-started;
            if(canvas.MonitorPresentationRefreshCount>(long)(elapsed/33334)+2)throw new InvalidOperationException("Presentation exceeded 30Hz bound.");
            if(canvas.MonitorScanNumber!=published || runtime.Snapshot.ScanNumber!=published)throw new InvalidOperationException("Coalescing lost the latest envelope or changed execution count.");
            if(published<=canvas.MonitorPresentationRefreshCount)throw new InvalidOperationException("Cadence fixture did not exercise coalescing.");
            GD.Print($"MONITOR_CADENCE_PASS published={published} refreshed={canvas.MonitorPresentationRefreshCount} elapsedUsec={elapsed}");
            var many=program with{Networks=Enumerable.Range(0,1000).Select(i=>new LadderNetwork("n"+i,"Visible row "+i,new("c"+i,LadderNodeKind.Contact,"input"),Coil:new("o"+i,"output"))).ToArray()};
            document.ReplaceFromProgram(many);canvas.RefreshDocument();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(canvas.LastDrawnInstructionCount>200)throw new InvalidOperationException("Offscreen row virtualization exceeded visible box bound.");
            GD.Print($"MONITOR_CULLING_MEASURE rows=1000 visibleInstructionBoxes={canvas.LastDrawnInstructionCount} viewport=640x360 (zero requires rendered follow-up)");
        }
        finally{scroll.QueueFree();}
    }
    private static void VerifyMonitorContextIndications()
    {
        foreach(var kind in Enum.GetValues<LadderNumericOperationKind>())
        {
            var rung=new EditableRung("arity", "Arity", "") { NumericOperationKind=kind, NumericSourceA="A", NumericSourceB="B", NumericSourceC="C", NumericDestination="D" };
            var rows=LadderEditorCanvas.NumericConfiguredRows(rung);
            if(rows.Length!=2+(LadderNumericOperationRules.RequiresSourceB(kind)?1:0)+(LadderNumericOperationRules.RequiresSourceC(kind)?1:0))
                throw new InvalidOperationException("Numeric unavailable-state arity mismatch: "+kind);
            if(LadderNumericOperationRules.RequiresSourceC(kind)&&(!rows.Contains("MIN: A")||!rows.Contains("VALUE: B")||!rows.Contains("MAX: C")))
                throw new InvalidOperationException("Ternary unavailable-state MIN/VALUE/MAX missing: "+kind);
        }
        var program = new LadderProgram(1, "context-review", "Context review", "LD", TimeSpan.FromMilliseconds(10),
            [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true), new("flag", PlcVariableType.Bool, PlcVariableRole.Memory, false), new("out", PlcVariableType.Bool, PlcVariableRole.Output, false)], [])
        { Blocks = [new("main", "Main", [
            new("n1", "First call sees FALSE", new("c1", LadderNodeKind.Contact, "enable"), Call: new("first-call", "child")),
            new("n2", "Set flag", new("c2", LadderNodeKind.Contact, "enable"), Coil: new("set-flag", "flag")),
            new("n3", "Second call sees TRUE", new("c3", LadderNodeKind.Contact, "enable"), Call: new("second-call", "child"))]),
            new("child", "Child", [new("child-net", "Observe flag", new("child-contact", LadderNodeKind.Contact, "flag"), Coil: new("child-coil", "out"))])], EntryBlock = "main" };
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Context fixture must compile.");
        var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
        var snapshot = runtime.Scan(new Dictionary<string, bool> { ["enable"] = true });
        var first = snapshot.Observations!.Contexts.Values.Single(o => o.InstructionId == "child-contact" && o.CallPath.Any(p => p.CallSiteId == "first-call"));
        var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
        foreach (var style in Enum.GetValues<LadderVendorStyle>())
        {
            var canvas = new LadderEditorCanvas(style, document);
            try
            {
                canvas.SetMonitorSnapshot(snapshot);
                if (!canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Global last context must be TRUE.");
                canvas.SelectObservationContext(LadderInstructionPresentation.ContextKey(first));
                if (canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Selected first context FALSE must not inherit global TRUE highlight.");
                canvas.SelectObservationContext("missing");
                if (canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Missing context must never be green.");
                canvas.SelectObservationContext(null); canvas.SetMonitorSnapshot(runtime.Stop());
                if (canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Stopped lifecycle must not present current green flow.");
            }
            finally { canvas.Free(); }
        }
    }
}


