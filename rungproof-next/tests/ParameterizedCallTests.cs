using System;
using System.Linq;
using System.Text.Json.Nodes;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterParameterizedCallTests()
    {
        RegisterParameterizedReviewTests();
        Test("TypedOutputClipboardPreservesExecutableCallMetadata", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(FbFixture());
            var action = d.CaptureRung(0); d.Rungs[1].CallSemanticMode = LadderSemanticMode.Legacy;
            d.Rungs[1].CallBindings = []; d.Rungs[1].CallInstanceId = "";
            d.ReplaceOutput(1, action);
            Equal(LadderSemanticMode.Parameterized, d.Rungs[1].CallSemanticMode);
            Equal("one", d.Rungs[1].CallInstanceId); Equal("arg", d.Rungs[1].CallBindings.Single().ParameterId);
        });
        Test("ParameterizedBodyRejectsBareAggregateLeaf", () =>
        {
            var p = TypedCallFixture(); p = p with { Variables = p.Variables.Append(new PlcVariable("assembly", PlcVariableType.Struct, PlcVariableRole.Memory,
                new System.Collections.Generic.Dictionary<string, object> { ["value"] = 5.0 }, Aggregate: new(Fields: [new("value", PlcVariableType.Real)]), Id: "assembly-id")).ToArray(),
                Blocks = [p.Blocks![0], p.Blocks[1] with { Networks = [TypedMove("bad-bare", "assembly.value", "local:arg")] }] };
            False(LadderCompiler.Compile(p).IsValid);
        });
        Test("MalformedTypedJsonFailsClosedWithoutNullDereference", () =>
        {
            var json = JsonNode.Parse(LadderProgramJson.Save(TypedCallFixture()))!;
            json["blocks"]![0]!["networks"]![0]!["call"]!["bindings"]![0]!["Symbol"] = null;
            False(LadderProgramJson.Load(json.ToJsonString()).IsValid);
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(FbFixture());
            var editor = JsonNode.Parse(LadderEditorProjectJson.Save(d))!;
            editor["instances"]![0]!["id"] = null;
            False(LadderEditorProjectJson.Load(editor.ToJsonString()).IsReadable);
        });
        Test("FbTimerResetCannotGrantAnotherSameScanQuantum", () =>
        {
            var p = FbTimerFixture(LadderTimerKind.OnDelay); var fb = p.Blocks![1];
            fb = fb with { Networks = [new("reset-net", "Reset", new("reset-wire", LadderNodeKind.Series, Children: []),
                TimerReset: new("reset-clock", "local:clock")), .. fb.Networks] };
            var runtime = Runtime(p with { Blocks = [p.Blocks[0], fb] }); runtime.Run(); var scan = runtime.Scan(Inputs());
            Equal(0.0, scan.NumericVariables["result"]); Equal(10.0, scan.NumericVariables["other"]);
        });
        Test("FbTimerTimeIsSharedAcrossTasksAndNotCaughtUpAfterSkippedScans", () =>
        {
            var p = FbTimerFixture(LadderTimerKind.OnDelay); p = p with { Blocks = [p.Blocks![0] with { Networks = [p.Blocks[0].Networks[0]] }, p.Blocks[1]],
                Tasks = [new("one-task", "One", LadderTaskKind.Continuous, p.ScanPeriod, 1, "main"), new("two-task", "Two", LadderTaskKind.Continuous, p.ScanPeriod, 2, "main")] };
            var runtime = Runtime(p); runtime.Run(); Equal(10.0, runtime.Scan(Inputs()).NumericVariables["result"]);
            var periodic = Runtime(p with { Tasks = [new("periodic", "Periodic", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(30), 1, "main")] });
            periodic.Run(); Equal(0.0, periodic.Scan(Inputs()).NumericVariables["result"]);
            Equal(0.0, periodic.Scan(Inputs()).NumericVariables["result"]); Equal(10.0, periodic.Scan(Inputs()).NumericVariables["result"]);
            Equal(10.0, periodic.Scan(Inputs()).NumericVariables["result"]);
        });
        Test("RuntimeDefendsAgainstAlreadyActiveFbInstance", () =>
        {
            var p = FbFixture(); var fb = p.Blocks![1] with { Networks = [new("recursive-net", "Defensive", new("recursive-wire", LadderNodeKind.Series, Children: []),
                Call: new("recursive-call", "fc", LadderSemanticMode.Parameterized, [new("arg", LadderActualKind.Discard)], "one"))] };
            p = p with { Blocks = [p.Blocks[0] with { Networks = [p.Blocks[0].Networks[0]] }, fb] };
            False(LadderCompiler.Compile(p).IsValid);
            // Exercise runtime's defense against a stale/corrupt compiled input, bypassing the ordinary compiler gate deliberately.
            var runtime = new VirtualControllerRuntime(new(p, PlcAggregates.Expand(p.Variables).ToDictionary(v => v.Name)));
            runtime.Run(); var scan = runtime.Scan(Inputs()); Equal(ControllerCompletion.Aborted, scan.Completion);
            True(scan.Diagnostics.Any(d => d.StartsWith("VC_RUNTIME_CALL_INSTANCE", StringComparison.Ordinal)));
            Equal(0.0, scan.NumericVariables["result"]);
        });
        Test("QualifiedInstanceStorageCannotCollideAcrossIdSegments", () =>
        {
            var p = FbFixture(); var fb = p.Blocks![1];
            fb = fb with { Interface = [fb.Interface![0], fb.Interface[1] with { Id = "x/arg" }],
                Networks = [new("increment-net", "Increment", new("increment-wire", LadderNodeKind.Series, Children: []),
                    NumericOperation: new("increment", LadderNumericOperationKind.Add, "local:x/arg", "1", "local:x/arg")),
                    TypedMove("publish", "local:x/arg", "local:arg")] };
            var main = p.Blocks[0] with { Networks = p.Blocks[0].Networks.Select(n => n.Call!.InstanceId == "two"
                ? n with { Call = n.Call with { InstanceId = "one/x" } } : n).ToArray() };
            p = p with { Blocks = [main, fb], Instances = [new("one", "One", "fc"), new("one/x", "Other", "fc")] };
            var runtime = Runtime(p); runtime.Run(); var first = runtime.Scan(Inputs());
            Equal(2.0, first.NumericVariables["result"]); Equal(1.0, first.NumericVariables["other"]);
            var second = runtime.Scan(Inputs()); Equal(4.0, second.NumericVariables["result"]); Equal(2.0, second.NumericVariables["other"]);
        });
        Test("NestedFcEdgesShareOwningInstanceAcrossOuterCallSites", () =>
        {
            var p = FbFixture(); var fb = p.Blocks![1] with { Networks = [
                new("inner-call-net", "Inner", new("inner-call-wire", LadderNodeKind.Series, Children: []), Call: new("inner-call", "inner", LadderSemanticMode.Parameterized,
                    [new("count", LadderActualKind.Symbol, "local:count")])), TypedMove("publish", "local:count", "local:arg")] };
            var inner = new LadderBlock("inner", "Inner", [new("edge-net", "Edge", new("inner-edge", LadderNodeKind.Contact, "global:edge-id", EdgeMode: LadderEdgeMode.Rising),
                NumericOperation: new("edge-add", LadderNumericOperationKind.Add, "local:count", "1", "local:count"))],
                Interface: [new("Count", PlcVariableType.Real, LadderInterfaceSection.InOut, 0.0, Id: "count")], SemanticMode: LadderSemanticMode.Parameterized);
            p = p with { Blocks = [p.Blocks[0], fb, inner], Variables = p.Variables.Append(new PlcVariable("edgeInput", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "edge-id")).ToArray() };
            var runtime = Runtime(p); runtime.Run(); runtime.Scan(Inputs());
            var edge = runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["edgeInput"] = true });
            Equal(1.0, edge.NumericVariables["result"]); Equal(1.0, edge.NumericVariables["other"]);
        });
        Test("FbResetDoesNotCreateUnqualifiedGhostStorage", () =>
        {
            var p = FbFixture(); var fb = p.Blocks![1] with { Interface = [new("Output", PlcVariableType.Real, LadderInterfaceSection.Output, 0.0, Id: "arg"),
                new("Counter", PlcVariableType.Counter, LadderInterfaceSection.Static, 0, Id: "count")],
                Networks = [new("count-net", "Count", new("count-wire", LadderNodeKind.Series, Children: []), Counter: new("count-action", "local:count", 2))] };
            var runtime = Runtime(p with { Blocks = [p.Blocks[0], fb] });
            False(runtime.Snapshot.Counters.Keys.Any(k => k.StartsWith("local:", StringComparison.Ordinal)));
        });
        Test("FcStaticCallPathSharesEdgeMemoryAcrossSerialTasks", () =>
        {
            var p = TypedCallFixture(); var fc = p.Blocks![1] with { Interface = [],
                Networks = [new("fc-edge-net", "Count edge", new("fc-edge", LadderNodeKind.Contact, "global:edge-id", EdgeMode: LadderEdgeMode.Rising),
                    NumericOperation: new("fc-add", LadderNumericOperationKind.Add, "global:result-id", "1", "global:result-id"))] };
            p = ReplaceTypedCall(p, p.Blocks[0].Networks[0].Call! with { Bindings = [] });
            p = p with { Blocks = [p.Blocks![0], fc], Variables = p.Variables.Append(new PlcVariable("edgeInput", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "edge-id")).ToArray(),
                Tasks = [new("task1", "One", LadderTaskKind.Continuous, p.ScanPeriod, 1, "main"), new("task2", "Two", LadderTaskKind.Continuous, p.ScanPeriod, 2, "main")] };
            var runtime = Runtime(p); runtime.Run(); runtime.Scan(Inputs());
            Equal(1.0, runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["edgeInput"] = true }).NumericVariables["result"]);
        });
        Test("ParameterizedBoolOutputRespectsForcePublication", () =>
        {
            var p = TypedCallFixture(); var fc = p.Blocks![1] with { Interface = [new("Out", PlcVariableType.Bool, LadderInterfaceSection.Output, false, Id: "arg")],
                Networks = [new("coil-net", "Coil", new("coil-wire", LadderNodeKind.Series, Children: []), Coil: new("coil", "local:arg"))] };
            p = p with { Variables = [p.Variables[0] with { Type = PlcVariableType.Bool, Role = PlcVariableRole.Output, InitialValue = false }, p.Variables[1]], Blocks = [p.Blocks[0], fc] };
            var runtime = Runtime(p); runtime.SetBoolForce("result", false); runtime.Run(); var scan = runtime.Scan(Inputs());
            False(scan.Variables["result"]); True(scan.CallInvocations.Single().Transfers.Single(t => t.Phase == "CopyOut").Value is true);
        });
        Test("CallObservationShowsActualCopyOutAndInstance", () =>
        {
            var runtime = Runtime(FbFixture()); runtime.Run(); var scan = runtime.Scan(Inputs());
            var call = scan.Observations!.GlobalLast["typed-call"];
            Equal("Completed", call.ControlOutcome);
            True(call.Operands.Any(o => o.Role == "CopyOut:arg" && o.Identity == "result" && o.Value is double v && v == 1.0));
            Equal("Completed", scan.Observations.GlobalLast["call3"].ControlOutcome);
            True(scan.Observations.GlobalLast["publish"].CallPath.Any(p => p.InstanceId == "one"));
        });
        Test("GlobalInstanceMemberReadsRemainQualifiedInsideFc", () =>
        {
            var p = TypedCallFixture(PlcVariableType.DInt); var main = p.Blocks![0]; var fc = p.Blocks[1];
            p = p with { Variables = p.Variables.Append(new PlcVariable("clock", PlcVariableType.Timer, PlcVariableRole.Memory, 0, Id: "clock-id")).ToArray(),
                Blocks = [main with { Networks = [new("clock-net", "Timer", new("clock-wire", LadderNodeKind.Series, Children: []), Timer: new("clock-action", "clock", TimeSpan.FromMilliseconds(100))), .. main.Networks] },
                    fc with { Networks = [TypedConvert("read-clock", "global:clock-id.ET", "local:arg")] }] };
            var runtime = Runtime(p); runtime.Run(); Equal(10.0, runtime.Scan(Inputs()).NumericVariables["result"]);
        });
        Test("AggregateLeafActualUsesStableRootIdentity", () =>
        {
            var p = TypedCallFixture();
            p = p with { Variables = p.Variables.Append(new PlcVariable("assembly", PlcVariableType.Struct, PlcVariableRole.Memory,
                new System.Collections.Generic.Dictionary<string, object> { ["value"] = 0.0 }, Aggregate: new(Fields: [new("value", PlcVariableType.Real)]), Id: "assembly-id")).ToArray() };
            p = ReplaceTypedCall(p, p.Blocks![0].Networks[0].Call! with { Bindings = [new("arg", LadderActualKind.Symbol, "global:assembly-id.value")] });
            var runtime = Runtime(p); runtime.Run(); Equal(7.0, runtime.Scan(Inputs()).NumericVariables["assembly.value"]);
        });
        Test("ForwardedInOutOverlapAbortsBeforeNestedBody", () =>
        {
            var p = TypedCallFixture(); var outer = p.Blocks![1] with {
                Interface = [new("Shared", PlcVariableType.Real, LadderInterfaceSection.InOut, 0.0, Id: "arg")],
                Networks = [TypedMove("outer-write", "1", "local:arg"),
                    new("nested-net", "Nested", new("nested-wire", LadderNodeKind.Series, Children: []),
                        Call: new("nested", "inner", LadderSemanticMode.Parameterized,
                            [new("a", LadderActualKind.Symbol, "local:arg"), new("b", LadderActualKind.Symbol, "global:result-id")]))] };
            var inner = new LadderBlock("inner", "Inner", [TypedMove("bad-write", "99", "local:a")],
                Interface: [new("A", PlcVariableType.Real, LadderInterfaceSection.InOut, 0.0, Id: "a"),
                    new("B", PlcVariableType.Real, LadderInterfaceSection.Output, 0.0, Id: "b")], SemanticMode: LadderSemanticMode.Parameterized);
            var runtime = Runtime(p with { Blocks = [p.Blocks[0], outer, inner] }); runtime.Run();
            var scan = runtime.Scan(Inputs()); Equal(ControllerCompletion.Aborted, scan.Completion);
            Equal(1.0, scan.NumericVariables["result"]); False(scan.Elements.ContainsKey("bad-write"));
            True(scan.Diagnostics.Any(d => d.StartsWith("VC_RUNTIME_CALL_ALIAS", StringComparison.Ordinal)));
        });
        Test("AbortedFbRetainsInternalOutputButSuppressesCopyOut", () =>
        {
            var p = FbFixture(); var main = p.Blocks![0] with { Networks = [p.Blocks[0].Networks[0]] };
            var fb = p.Blocks[1] with { Networks = [new("output-net", "Increment output", new("output-wire", LadderNodeKind.Series, Children: []),
                NumericOperation: new("output-inc", LadderNumericOperationKind.Add, "local:arg", "1", "local:arg")),
                new("label-net", "Loop", new("label-wire", LadderNodeKind.Series, Children: []), LabelInstruction: new("label", "loop")),
                new("jump-net", "Jump", new("jump-contact", LadderNodeKind.Contact, "global:abort-id"), Jump: new("jump", "loop"))] };
            p = p with { Blocks = [main, fb], Variables = p.Variables.Append(new PlcVariable("abort", PlcVariableType.Bool, PlcVariableRole.Input, true, Id: "abort-id")).ToArray() };
            var runtime = Runtime(p); runtime.Run(); var aborted = runtime.Scan(Inputs());
            Equal(ControllerCompletion.Aborted, aborted.Completion); Equal(0.0, aborted.NumericVariables["result"]);
            Equal(1.0, aborted.NumericVariables["@instance/one/arg"]);
            True(aborted.CallInvocations.Single().Transfers.Any(t => t.Phase == "CopyOutSuppressed"));
            runtime.Run(); var next = runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["abort"] = false });
            Equal(2.0, next.NumericVariables["result"]); Equal(LadderCallCompletion.Completed, next.CallInvocations.Single().Completion);
        });
        Test("ParameterizedEditorPersistenceNeverDropsBindings", () =>
        {
            var d = new LadderEditorDocument(); d.ReplaceFromProgram(FbFixture());
            var saved = LadderEditorProjectJson.Save(d); var loaded = LadderEditorProjectJson.Load(saved);
            True(loaded.IsReadable); var p = loaded.Document!.BuildProgram();
            Equal(LadderSemanticMode.Parameterized, p.Blocks![1].SemanticMode);
            Equal("one", p.Blocks[0].Networks[0].Call!.InstanceId);
            Equal("global:result-id", p.Blocks[0].Networks[0].Call!.Bindings![0].Symbol);
            Equal("result-id", p.Variables[0].Id); Equal(2, p.Instances!.Count);
        });
        Test("FbStaticTimersConsumeOneQuantumPerScan", () =>
        {
            foreach (var kind in Enum.GetValues<LadderTimerKind>())
            {
                var runtime = Runtime(FbTimerFixture(kind)); runtime.Run();
                var first = runtime.Scan(Inputs());
                if (kind == LadderTimerKind.OffDelay)
                {
                    first = runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["timerSignal"] = false });
                }
                Equal(10.0, first.NumericVariables["result"]); Equal(10.0, first.NumericVariables["other"]);
                runtime.Stop();
                var stored = runtime.Snapshot.Timers["@instance/one/clock"];
                Equal(kind == LadderTimerKind.RetentiveOnDelay ? 10.0 : 0.0, stored.Accumulated.TotalMilliseconds);
            }
        });
        Test("FbCounterEdgesAreInstanceScoped", () =>
        {
            var p = FbFixture(PlcVariableType.DInt); var fb = p.Blocks![1] with {
                Interface = [new("Output", PlcVariableType.DInt, LadderInterfaceSection.Output, 0L, Id: "arg"),
                    new("Count", PlcVariableType.Counter, LadderInterfaceSection.Static, 0, Id: "count")],
                Networks = [new("counter-net", "Count", new("counter-wire", LadderNodeKind.Series, Children: []),
                    Counter: new("counter-action", "local:count", 5)), TypedMove("publish", "local:count.ACC", "local:arg")] };
            var runtime = Runtime(p with { Blocks = [p.Blocks[0], fb] }); runtime.Run();
            var scan = runtime.Scan(Inputs()); Equal(1.0, scan.NumericVariables["result"]); Equal(1.0, scan.NumericVariables["other"]);
        });
        Test("FbEdgesAreInstanceScopedAndStopPrimes", () =>
        {
            var p = FbFixture(); var fb = p.Blocks![1];
            fb = fb with { Networks = [fb.Networks[0] with { Logic = new("edge", LadderNodeKind.Contact, "global:edge-id", EdgeMode: LadderEdgeMode.Rising) }, fb.Networks[1]] };
            p = p with { Variables = p.Variables.Append(new PlcVariable("edgeInput", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "edge-id")).ToArray(), Blocks = [p.Blocks[0], fb] };
            var runtime = Runtime(p); runtime.Run(); runtime.Scan(Inputs());
            var scan = runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["edgeInput"] = true });
            Equal(1.0, scan.NumericVariables["result"]); Equal(1.0, scan.NumericVariables["other"]);
            runtime.Stop(); runtime.Run(); scan = runtime.Scan(Inputs());
            Equal(1.0, scan.NumericVariables["result"]); Equal(1.0, scan.NumericVariables["other"]);
        });
        Test("CallDepthBoundary32And33", () =>
        {
            LadderProgram Chain(int count) => new(2, "depth", "Depth", "LD", TimeSpan.FromMilliseconds(10), [], [],
                Enumerable.Range(0, count).Select(i => new LadderBlock("b" + i, "B" + i,
                    [new("n" + i, "N", new("wire" + i, LadderNodeKind.Series, Children: []),
                        Call: i < count - 1 ? new("call" + i, "b" + (i + 1), LadderSemanticMode.Parameterized, []) : null,
                        Return: i == count - 1 ? new("ret") : null)],
                        SemanticMode: i == 0 ? LadderSemanticMode.Legacy : LadderSemanticMode.Parameterized)).ToArray(), "b0");
            True(LadderCompiler.Compile(Chain(32)).IsValid);
            False(LadderCompiler.Compile(Chain(33)).IsValid);
        });
        Test("TypedFcExecutesAndCopiesOutputOnlyAtReturn", () =>
        {
            var runtime = Runtime(TypedCallFixture()); runtime.Run();
            Equal(7.0, runtime.Scan(Inputs()).NumericVariables["result"]);
        });
        Test("FbInstancesRetainSeparatelyAndReset", () =>
        {
            var p = FbFixture(); var runtime = Runtime(p); runtime.Run();
            var one = runtime.Scan(Inputs()); Equal(2.0, one.NumericVariables["result"]); Equal(1.0, one.NumericVariables["other"]);
            var two = runtime.Scan(Inputs()); Equal(4.0, two.NumericVariables["result"]); Equal(2.0, two.NumericVariables["other"]);
            runtime.Stop(); runtime.Run(); Equal(6.0, runtime.Scan(Inputs()).NumericVariables["result"]);
            runtime.Reset(); runtime.Run(); Equal(2.0, runtime.Scan(Inputs()).NumericVariables["result"]);
        });
        Test("FalseParameterizedCallHasNoStorageEffect", () =>
        {
            var runtime = Runtime(FbFixture()); runtime.Run();
            Equal(0.0, runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["enable"] = false }).NumericVariables["result"]);
            Equal(2.0, runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["enable"] = true }).NumericVariables["result"]);
        });
        Test("FcInputIsSampledBeforeOutputAndTempInitializes", () =>
        {
            var p = TypedCallFixture(); var f = p.Blocks![1];
            f = f with { Interface = [new("Input", PlcVariableType.Real, LadderInterfaceSection.Input, 0.0, Id: "in"),
                new("Result", PlcVariableType.Real, LadderInterfaceSection.Output, 2.0, Id: "arg"),
                new("Temp", PlcVariableType.Real, LadderInterfaceSection.Temp, 4.0, Id: "temp")],
                Networks = [TypedMove("initialize", "local:temp", "local:arg"),
                    new("sum-net", "sum", new("sum-wire", LadderNodeKind.Series, Children: []),
                        NumericOperation: new("sum", LadderNumericOperationKind.Add, "local:in", "local:arg", "local:arg")),
                    TypedMove("clear", "0", "local:temp"),
                    new("return-net", "return", new("return-wire", LadderNodeKind.Series, Children: []), Return: new("return-action")),
                    TypedMove("unreached", "99", "local:arg")] };
            var call = p.Blocks[0].Networks[0].Call! with { Bindings = [new("in", LadderActualKind.Symbol, "global:result-id"), new("arg", LadderActualKind.Symbol, "global:result-id")] };
            p = ReplaceTypedCall(p, call) with { Blocks = [p.Blocks[0] with { Networks = [p.Blocks[0].Networks[0] with { Call = call }] }, f] };
            var runtime = Runtime(p); runtime.Run(); Equal(4.0, runtime.Scan(Inputs()).NumericVariables["result"]);
            Equal(8.0, runtime.Scan(Inputs()).NumericVariables["result"]);
        });
        Test("TypedCallCompilesAndRoundTripsStableScope", () =>
        {
            var p = TypedCallFixture();
            True(LadderCompiler.Compile(p).IsValid);
            var loaded = LadderProgramJson.Load(LadderProgramJson.Save(p));
            True(loaded.IsValid);
            Equal("arg", loaded.Program!.Blocks![1].Interface![0].Id);
            Equal("global:result-id", loaded.Program.Blocks[0].Networks[0].Call!.Bindings![0].Symbol);
        });
        Test("TypedCallBindingCompletenessAndExactTypes", () =>
        {
            var p = TypedCallFixture();
            foreach (var bindings in new LadderCallBinding[][] {
                [], [new("bad", LadderActualKind.Discard)],
                [new("arg", LadderActualKind.Symbol, "global:result-id"), new("arg", LadderActualKind.Discard)],
                [new("arg", LadderActualKind.Literal, Literal: 1.0, LiteralType: PlcVariableType.Real)],
                [new("arg", LadderActualKind.Symbol, "global:bool-id")] })
                False(LadderCompiler.Compile(ReplaceTypedCall(p, p.Blocks![0].Networks[0].Call! with { Bindings = bindings })).IsValid);
        });
        Test("TypedCallRejectsInvalidSectionsAndInstance", () =>
        {
            var p = TypedCallFixture(); var fc = p.Blocks![1];
            foreach (var formal in new[] { fc.Interface![0] with { Section = LadderInterfaceSection.Static },
                fc.Interface[0] with { Type = PlcVariableType.Counter }, fc.Interface[0] with { Type = PlcVariableType.Array } })
                False(LadderCompiler.Compile(p with { Blocks = [p.Blocks[0], fc with { Interface = [formal] }] }).IsValid);
            False(LadderCompiler.Compile(p with { Blocks = [p.Blocks[0], fc with { BlockType = LadderBlockType.FunctionBlock }] }).IsValid);
            False(LadderCompiler.Compile(p with { SchemaVersion = 1 }).IsValid);
        });
        Test("ParameterizedModeNeverSilentlyFallsBackToLegacy", () =>
        {
            var p = new LadderProgram(2, "calls", "Calls", "LD", TimeSpan.FromMilliseconds(10),
                [new("run", PlcVariableType.Bool, PlcVariableRole.Input, true)], [],
                [new("main", "Main", [new("n", "Call", new("c", LadderNodeKind.Contact, "run"), Call: new("call", "fc"))]),
                 new("fc", "FC", [new("ret", "Return", new("rc", LadderNodeKind.Contact, "run"), Return: new("r"))])], "main");
            var json = JsonNode.Parse(LadderProgramJson.Save(p))!;
            json["blocks"]![0]!["networks"]![0]!["call"]!["semanticMode"] = "Parameterized";
            var loaded = LadderProgramJson.Load(json.ToJsonString());
            False(loaded.IsValid); // A parameterized CALL cannot target an explicitly legacy FC.
            True(loaded.Issues.Any(i => i.Path.Contains("call") && i.Message.Contains("mode", StringComparison.OrdinalIgnoreCase)));
        });
    }

    private static LadderProgram ReplaceTypedCall(LadderProgram p, LadderCall call) => p with
    { Blocks = [p.Blocks![0] with { Networks = [p.Blocks[0].Networks[0] with { Call = call }] }, p.Blocks[1]] };

    private static object TypedZero(PlcVariableType type) => type switch
    {
        PlcVariableType.Real => 0.0,
        PlcVariableType.Int or PlcVariableType.DInt => 0L,
        PlcVariableType.Time => TimeSpan.Zero,
        _ => 0,
    };

    private static LadderProgram TypedCallFixture(PlcVariableType resultType = PlcVariableType.Real) => new(2, "typed", "Typed", "LD", TimeSpan.FromMilliseconds(10),
        [new("result", resultType, PlcVariableRole.Memory, TypedZero(resultType), Id: "result-id"),
         new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true, Id: "bool-id")], [],
        [new("main", "Main", [new("call-net", "Call", new("enable-contact", LadderNodeKind.Contact, "enable"),
            Call: new("typed-call", "fc", LadderSemanticMode.Parameterized, [new("arg", LadderActualKind.Symbol, "global:result-id")]))]),
         new("fc", "FC", [new("body", "Body", new("wire", LadderNodeKind.Series, Children: []),
            NumericOperation: new("move", LadderNumericOperationKind.Move, "7", "0", "local:arg"))],
            LadderBlockType.Function, [new("Result", resultType, LadderInterfaceSection.Output, TypedZero(resultType), Id: "arg")],
            LadderSemanticMode.Parameterized)], "main");

    private static LadderNetwork TypedMove(string id, string source, string destination) => new(id + "-n", id,
        new(id + "-wire", LadderNodeKind.Series, Children: []), NumericOperation: new(id, LadderNumericOperationKind.Move, source, "0", destination));

    private static LadderNetwork TypedConvert(string id, string source, string destination) => new(id + "-n", id,
        new(id + "-wire", LadderNodeKind.Series, Children: []), NumericOperation: new(id, LadderNumericOperationKind.Convert, source, "0", destination));

    private static LadderProgram FbFixture(PlcVariableType resultType = PlcVariableType.Real)
    {
        var p = TypedCallFixture(resultType); var call = p.Blocks![0].Networks[0];
        var fb = p.Blocks[1] with { BlockType = LadderBlockType.FunctionBlock,
            Interface = [new("Result", resultType, LadderInterfaceSection.Output, TypedZero(resultType), Id: "arg"),
                new("Count", PlcVariableType.Real, LadderInterfaceSection.Static, 0.0, Id: "count")],
            Networks = [new("increment-net", "Increment", new("increment-wire", LadderNodeKind.Series, Children: []),
                NumericOperation: new("increment", LadderNumericOperationKind.Add, "local:count", "1", "local:count")),
                TypedMove("publish", "local:count", "local:arg")] };
        return p with { Variables = p.Variables.Append(new PlcVariable("other", resultType, PlcVariableRole.Memory, TypedZero(resultType), Id: "other-id")).ToArray(),
            Instances = [new("one", "One", "fc"), new("two", "Two", "fc")],
            Blocks = [p.Blocks[0] with { Networks = [call with { Call = call.Call! with { InstanceId = "one" } },
                call with { Id = "call2-net", Logic = call.Logic with { Id = "call2-contact" }, Call = call.Call! with { Id = "call2", InstanceId = "two", Bindings = [new("arg", LadderActualKind.Symbol, "global:other-id")] } },
                call with { Id = "call3-net", Logic = call.Logic with { Id = "call3-contact" }, Call = call.Call! with { Id = "call3", InstanceId = "one" } }] }, fb] };
    }

    private static LadderProgram FbTimerFixture(LadderTimerKind kind)
    {
        var p = FbFixture(PlcVariableType.DInt); var fb = p.Blocks![1] with {
            Interface = [new("Result", PlcVariableType.DInt, LadderInterfaceSection.Output, 0L, Id: "arg"),
                new("Clock", PlcVariableType.Timer, LadderInterfaceSection.Static, 0, Id: "clock")],
            Networks = [new("timer-net", "Timer", new("timer-contact", LadderNodeKind.Contact, "global:timer-signal"),
                Timer: new("timer-action", "local:clock", TimeSpan.FromMilliseconds(100), kind)),
                TypedConvert("timer-value", "local:clock.ET", "local:arg")] };
        return p with { Variables = p.Variables.Append(new PlcVariable("timerSignal", PlcVariableType.Bool, PlcVariableRole.Input, true, Id: "timer-signal")).ToArray(), Blocks = [p.Blocks[0], fb] };
    }

    private static void RegisterParameterizedReviewTests()
    {
        Test("PrivateTimerMembersCannotAliasScalarGlobals", () =>
        {
            var p = FbTimerFixture(LadderTimerKind.OnDelay);
            p = p with { Blocks = [p.Blocks![0], p.Blocks[1] with { Networks = [.. p.Blocks[1].Networks,
                new("q-net", "Q", new("q-contact", LadderNodeKind.Contact, "local:clock.Q"), Coil: new("q-coil", "global:flag"))] }] };
            p = p with { Variables = p.Variables.Concat(new PlcVariable[] {
                new("@instance/one/clock.ET", PlcVariableType.Real, PlcVariableRole.Memory, 777.0),
                new("@instance/one/clock.Q", PlcVariableType.Bool, PlcVariableRole.Memory, true),
                new("@local/fc/clock.ET", PlcVariableType.Bool, PlcVariableRole.Memory, true),
                new("flag", PlcVariableType.Bool, PlcVariableRole.Memory, false, Id: "flag") }).ToArray() };
            var runtime = Runtime(p); runtime.Run(); var scan = runtime.Scan(Inputs());
            Equal(10.0, scan.NumericVariables["result"]); Equal(777.0, scan.NumericVariables["@instance/one/clock.ET"]);
            True(scan.Variables["@instance/one/clock.Q"]);
            False(scan.Variables["flag"]);
            var timer = scan.Observations!.GlobalLast["timer-action"].StoredAfter as LadderTimerState;
            True(timer is not null); False(timer!.Done);
            runtime.Reset(); runtime.Run(); Equal(10.0, runtime.Scan(Inputs()).NumericVariables["result"]);
        });
        Test("PrivateCounterMemberReadsCannotAliasScalarGlobals", () =>
        {
            foreach (var member in new[] { "ACC", "CV" })
            {
                var p = FbFixture(PlcVariableType.DInt); var fb = p.Blocks![1] with { Interface = [p.Blocks[1].Interface![0], new("Counter", PlcVariableType.Counter, LadderInterfaceSection.Static, 0, Id: "count")],
                    Networks = [new("counter-net", "Counter", new("counter-wire", LadderNodeKind.Series, Children: []), Counter: new("count", "local:count", 5)),
                        TypedMove("publish", "local:count." + member, "local:arg")] };
                p = p with { Blocks = [p.Blocks[0], fb], Variables = p.Variables.Append(new PlcVariable("@instance/one/count." + member, PlcVariableType.Real, PlcVariableRole.Memory, 888.0)).ToArray() };
                var runtime = Runtime(p); runtime.Run(); var scan = runtime.Scan(Inputs());
                Equal(1.0, scan.NumericVariables["result"]); Equal(888.0, scan.NumericVariables["@instance/one/count." + member]);
            }
        });
        Test("BareScopeIsRejectedWithoutResolvableSentinels", () =>
        {
            foreach (var type in new[] { PlcVariableType.Timer, PlcVariableType.Counter })
            foreach (var member in type == PlcVariableType.Timer ? new[] { "ET", "Q" } : new[] { "ACC", "DN" })
            {
                var p = TypedCallFixture(); var boolean = member is "Q" or "DN";
                var network = boolean ? new LadderNetwork("n-member", "Member", new("member", LadderNodeKind.Contact, "clock." + member), Return: new("ret-member"))
                    : TypedMove("move-member", "clock." + member, "local:arg");
                p = p with { Variables = p.Variables.Concat(new PlcVariable[] { new("clock", type, PlcVariableRole.Memory, 0), new("@unqualified/clock", type, PlcVariableRole.Memory, 0) }).ToArray(),
                    Blocks = [p.Blocks![0], p.Blocks[1] with { Networks = [network] }] };
                var compiled = LadderCompiler.Compile(p); False(compiled.IsValid);
                True(compiled.Issues.Any(i => i.Code == "VC_CALL_SCOPE"));
            }
        });
        Test("LegacyAndInstanceEdgeMemoryAreDisjointIdentities", () =>
        {
            var p = FbFixture(); var fb = p.Blocks![1] with { Networks = [p.Blocks[1].Networks[0] with {
                Logic = new("edge", LadderNodeKind.Contact, "global:typed-input", EdgeMode: LadderEdgeMode.Rising) }, p.Blocks[1].Networks[1]] };
            var legacy = new LadderNetwork("legacy-net", "Legacy", new("@edge/instance/one/edge", LadderNodeKind.Contact, "legacyInput", EdgeMode: LadderEdgeMode.Rising),
                NumericOperation: new("legacy-add", LadderNumericOperationKind.Add, "legacyCount", "1", "legacyCount"));
            p = p with { Blocks = [p.Blocks[0] with { Networks = [legacy, p.Blocks[0].Networks[0]] }, fb], Variables = p.Variables.Concat(new PlcVariable[] {
                new("legacyInput", PlcVariableType.Bool, PlcVariableRole.Input, false), new("typedInput", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "typed-input"),
                new("legacyCount", PlcVariableType.Real, PlcVariableRole.Memory, 0.0) }).ToArray() };
            var runtime = Runtime(p); runtime.Run(); runtime.Scan(Inputs());
            var first = runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["legacyInput"] = true });
            Equal(1.0, first.NumericVariables["legacyCount"]); Equal(0.0, first.NumericVariables["result"]);
            var held = runtime.Scan(Inputs()); Equal(1.0, held.NumericVariables["legacyCount"]);
            var typed = runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["typedInput"] = true });
            Equal(1.0, typed.NumericVariables["result"]); Equal(1.0, typed.NumericVariables["legacyCount"]);
        });
        Test("LegacyAndInstanceCounterEdgeMemoryAreDisjoint", () =>
        {
            var p = FbFixture(PlcVariableType.DInt); var fb = p.Blocks![1] with { Interface = [p.Blocks[1].Interface![0], new("Counter", PlcVariableType.Counter, LadderInterfaceSection.Static, 0, Id: "count")],
                Networks = [new("typed-counter-net", "Count", new("typed-counter-contact", LadderNodeKind.Contact, "global:typed-input"), Counter: new("counter", "local:count", 5)),
                    TypedMove("publish", "local:count.ACC", "local:arg")] };
            var legacy = new LadderNetwork("legacy-net", "Legacy", new("legacy-contact", LadderNodeKind.Contact, "legacyInput"), Counter: new("@instance/one/count/counter", "legacyCounter", 5));
            p = p with { Blocks = [p.Blocks[0] with { Networks = [legacy, p.Blocks[0].Networks[0]] }, fb], Variables = p.Variables.Concat(new PlcVariable[] {
                new("legacyInput", PlcVariableType.Bool, PlcVariableRole.Input, false), new("typedInput", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "typed-input"),
                new("legacyCounter", PlcVariableType.Counter, PlcVariableRole.Memory, 0) }).ToArray() };
            var runtime = Runtime(p); runtime.Run(); runtime.Scan(Inputs());
            runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["legacyInput"] = true });
            Equal(1L, runtime.Scan(Inputs()).Counters["legacyCounter"].Accumulated);
            Equal(1.0, runtime.Scan(new System.Collections.Generic.Dictionary<string, bool> { ["typedInput"] = true }).NumericVariables["result"]);
        });
        Test("TypedValidationPrivateNamespacePreservesLegalGlobals", () =>
        {
            var p = TypedCallFixture();
            p = p with { Variables = p.Variables.Append(new PlcVariable("@local/fc/arg", PlcVariableType.Bool, PlcVariableRole.Memory, true)).ToArray() };
            True(LadderCompiler.Compile(p).IsValid);
            var runtime = Runtime(p); runtime.Run(); var scan = runtime.Scan(Inputs());
            True(scan.Variables["@local/fc/arg"]); Equal(7.0, scan.NumericVariables["result"]);
        });
        Test("CallPrivateStorageNeverOverwritesLegalGlobalNames", () =>
        {
            var fc = TypedCallFixture();
            fc = fc with { Variables = fc.Variables.Append(new PlcVariable("@frame/2/arg", PlcVariableType.Bool, PlcVariableRole.Memory, true)).ToArray() };
            var runtime = Runtime(fc); runtime.Run(); var scan = runtime.Scan(Inputs());
            True(scan.Variables.TryGetValue("@frame/2/arg", out var preserved) && preserved);
            runtime.Reset(); runtime.Run(); True(runtime.Scan(Inputs()).Variables["@frame/2/arg"]);
            var fb = FbFixture();
            fb = fb with { Variables = fb.Variables.Append(new PlcVariable("@instance/one/arg", PlcVariableType.Real, PlcVariableRole.Memory, 91.0)).ToArray() };
            runtime = Runtime(fb); Equal(91.0, runtime.Snapshot.NumericVariables["@instance/one/arg"]);
            runtime.Run(); Equal(91.0, runtime.Scan(Inputs()).NumericVariables["@instance/one/arg"]);
            runtime.Reset(); Equal(91.0, runtime.Snapshot.NumericVariables["@instance/one/arg"]);
        });
        Test("ImplicitTypedEntryIsRejectedBeforeExecution", () =>
        {
            foreach (var p in new[] { TypedCallFixture(), FbFixture() })
            foreach (var entry in new[] { "", "   " })
                False(LadderCompiler.Compile(p with { EntryBlock = entry, Tasks = null, Blocks = [p.Blocks![1], p.Blocks[0]] }).IsValid);
        });
        Test("QualifiedGlobalRtoRetainsOnStopInFcAndFb", () =>
        {
            foreach (var original in new[] { TypedCallFixture(), FbFixture() })
            {
                var body = original.Blocks![1] with { Networks = [new("global-rto-net", "Global RTO", new("rto-wire", LadderNodeKind.Series, Children: []),
                    Timer: new("global-rto", "global:clock-id", TimeSpan.FromMilliseconds(100), LadderTimerKind.RetentiveOnDelay))] };
                var p = original with { Variables = original.Variables.Append(new PlcVariable("ClockDisplay", PlcVariableType.Timer, PlcVariableRole.Memory, 0, Id: "clock-id")).ToArray(),
                    Blocks = [original.Blocks[0] with { Networks = [original.Blocks[0].Networks[0]] }, body] };
                var runtime = Runtime(p); runtime.Run(); Equal(10.0, runtime.Scan(Inputs()).Timers["ClockDisplay"].Accumulated.TotalMilliseconds);
                Equal(10.0, runtime.Stop().Timers["ClockDisplay"].Accumulated.TotalMilliseconds);
                runtime.Run(); Equal(20.0, runtime.Scan(Inputs()).Timers["ClockDisplay"].Accumulated.TotalMilliseconds);
                Equal(0.0, runtime.Reset().Timers["ClockDisplay"].Accumulated.TotalMilliseconds);
            }
        });
        Test("TypedBlocksRejectBareTimerAndCounterMembers", () =>
        {
            foreach (var (source, resultType, convert) in new[] {
                ("clock.ET", PlcVariableType.DInt, true), ("counter.ACC", PlcVariableType.DInt, false) })
            {
                var p = TypedCallFixture(resultType);
                p = p with { Variables = p.Variables.Concat(new PlcVariable[] {
                    new("clock", PlcVariableType.Timer, PlcVariableRole.Memory, 0, Id: "clock-id"),
                    new("counter", PlcVariableType.Counter, PlcVariableRole.Memory, 0, Id: "counter-id") }).ToArray() };
                var network = convert ? TypedConvert("member", source, "local:arg") : TypedMove("member", source, "local:arg");
                var fc = p.Blocks![1] with { Networks = [network] };
                False(LadderCompiler.Compile(p with { Blocks = [p.Blocks[0], fc] }).IsValid);
                var qualified = source.StartsWith("clock", StringComparison.Ordinal) ? "global:clock-id.ET" : "global:counter-id.ACC";
                var qualifiedNetwork = convert ? TypedConvert("member", qualified, "local:arg") : TypedMove("member", qualified, "local:arg");
                True(LadderCompiler.Compile(p with { Blocks = [p.Blocks[0], fc with { Networks = [qualifiedNetwork] }] }).IsValid);
            }
            var booleanMemberProgram = TypedCallFixture();
            booleanMemberProgram = booleanMemberProgram with
            {
                Variables = booleanMemberProgram.Variables.Concat(new PlcVariable[]
                {
                    new("clock", PlcVariableType.Timer, PlcVariableRole.Memory, 0),
                    new("counter", PlcVariableType.Counter, PlcVariableRole.Memory, 0),
                }).ToArray(),
            };
            foreach (var source in new[] { "clock.Q", "counter.DN" })
            {
                var fc = booleanMemberProgram.Blocks![1] with { Networks = [new("member-net", "Member", new("member-contact", LadderNodeKind.Contact, source), Return: new("member-return"))] };
                False(LadderCompiler.Compile(booleanMemberProgram with { Blocks = [booleanMemberProgram.Blocks[0], fc] }).IsValid);
            }
        });
        Test("ScopeRejectionCannotAccidentallyBindToLegalDiagnosticLikeGlobal", () =>
        {
            var p = TypedCallFixture(); p = p with { Variables = p.Variables.Concat(new PlcVariable[] {
                new("clock", PlcVariableType.Timer, PlcVariableRole.Memory, 0),
                new("@unqualified/clock.ET", PlcVariableType.Real, PlcVariableRole.Memory, 99.0) }).ToArray(),
                Blocks = [p.Blocks![0], p.Blocks[1] with { Networks = [TypedMove("bare", "clock.ET", "local:arg")] }] };
            False(LadderCompiler.Compile(p).IsValid);
        });
        Test("CallTransferHistoryChargeIncludesMonitoringOffPayload", () =>
        {
            var parameters = Enumerable.Range(0, 256).Select(i => new LadderInterfaceParameter("Input" + i, PlcVariableType.String, LadderInterfaceSection.Input, "", Id: "p" + i)).ToArray();
            var text = new string('x', 255);
            var bindings = parameters.Select(p => new LadderCallBinding(p.Id, LadderActualKind.Literal, Literal: text, LiteralType: PlcVariableType.String)).ToArray();
            var networks = Enumerable.Range(0, 20).Select(i => new LadderNetwork("n" + i, "Call", new("w" + i, LadderNodeKind.Series, Children: []),
                Call: new("c" + i, "fc", LadderSemanticMode.Parameterized, bindings))).ToArray();
            var p = new LadderProgram(2, "history-calls", "History calls", "LD", TimeSpan.FromMilliseconds(10), [], [],
                [new("main", "Main", networks), new("fc", "FC", [new("ret-net", "Return", new("ret-wire", LadderNodeKind.Series, Children: []), Return: new("ret"))],
                    Interface: parameters, SemanticMode: LadderSemanticMode.Parameterized)], "main");
            foreach (var monitoring in new[] { false, true })
            {
                var runtime = Runtime(p); runtime.MonitoringEnabled = monitoring;
                var session = new VirtualControllerSession(runtime); session.Run(); session.Advance(.01, () => Inputs(), _ => { }, _ => { });
                var first = session.History.Single(); var snapshot = first.CompletedScan.Publication!.Snapshot;
                var minimumTextPayload = 20L * 256 * 255 * 2;
                True(first.EstimatedPayloadBytes >= minimumTextPayload);
                Equal(20, snapshot.CallInvocations.Count); Equal(256, snapshot.CallInvocations[0].Transfers.Count);
                var firstValue = snapshot.CallInvocations[0].Transfers[0].Value;
                for (var scan = 0; scan < 26; scan++) session.Advance(.01, () => Inputs(), _ => { }, _ => { });
                True(session.HistoryDroppedCount > 0); True(session.HistoryRetainedBytes <= VirtualControllerSession.HistoryByteCapacity);
                Equal((string)firstValue!, (string)snapshot.CallInvocations[0].Transfers[0].Value!);
                if (monitoring) True(first.CompletedScan.Publication.Observations!.EstimatedBytes >= minimumTextPayload);
            }
        });
    }
}



