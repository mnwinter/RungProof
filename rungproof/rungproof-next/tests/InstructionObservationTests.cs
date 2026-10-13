using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterInstructionObservationTests()
    {
        Test("LaterMoveDoesNotRewriteEarlierObservation", LaterMoveDoesNotRewriteEarlierObservation);
        Test("SameValueWriteIsStillWrite", SameValueWriteIsStillWrite);
        Test("FalseTimerStillEvaluates", FalseTimerStillEvaluates);
        Test("ObservationCatalogSetEquality", ObservationCatalogSetEquality);
        Test("ObservationContextOverflowKeepsGlobalLast", ObservationContextOverflowKeepsGlobalLast);
        Test("ObservationTraceOverflowKeepsCounts", ObservationTraceOverflowKeepsCounts);
        Test("ObservationTextTruncationPreservesSurrogates", ObservationTextTruncationPreservesSurrogates);
        Test("EveryRuntimeInstructionFamilyProducesObservation", EveryRuntimeInstructionFamilyProducesObservation);
        Test("MonitoringOnOffGoldenResultsMatch", MonitoringOnOffGoldenResultsMatch);
        Test("SharedCounterShowsInstructionAndInstanceValues", SharedCounterShowsInstructionAndInstanceValues);
        Test("RecoverableNumericFaultCompletesWithoutWrite", RecoverableNumericFaultCompletesWithoutWrite);
        Test("ObservationDispositionsNormalizeAndClear", ObservationDispositionsNormalizeAndClear);
        Test("InvocationPathsDoNotSubstituteOtherTasks", InvocationPathsDoNotSubstituteOtherTasks);
        Test("IncomingContinuityIsSeparateFromLocalResult", IncomingContinuityIsSeparateFromLocalResult);
        Test("OriginBindingSharesImmutableEventsWithoutChangingSource", OriginBindingSharesImmutableEventsWithoutChangingSource);
        Test("InternedTaskPathsRemainImmutableAcrossScansAndReset", InternedTaskPathsRemainImmutableAcrossScansAndReset);
        Test("OriginBindingDoesNotEagerlyCloneEveryRetainedEvent", OriginBindingDoesNotEagerlyCloneEveryRetainedEvent);
        Test("FrozenViewsSurviveCollectorReuseWithExactTraceCounts", FrozenViewsSurviveCollectorReuseWithExactTraceCounts);
        Test("DenseFrozenMapsPreserveMissingKeysOrderAndOldCounts", DenseFrozenMapsPreserveMissingKeysOrderAndOldCounts);
        Test("SparseFrozenCaptureChargesOwnedCapacity", SparseFrozenCaptureChargesOwnedCapacity);
    }

    private static void SparseFrozenCaptureChargesOwnedCapacity()
    {
        var ids = Enumerable.Range(0, 20_000).Select(i => "registered-" + i).ToArray();
        var capture = new InstructionObservationCollector(ids).Freeze(false);
        Equal(0, capture.GlobalLast.Count);
        True(capture.EstimatedBytes >= 12L * ids.Length);
        var bound = capture.WithOrigin(new(Guid.NewGuid(), Guid.NewGuid(), 1, 1));
        Equal(capture.EstimatedBytes, bound.EstimatedBytes);
    }

    private static void DenseFrozenMapsPreserveMissingKeysOrderAndOldCounts()
    {
        var collector = new InstructionObservationCollector(["never", "beta", "alpha"]);
        collector.Record(new("beta", "no", "b", "task", 1, 1, [], [], true, false));
        collector.Record(new("beta", "no", "b", "task", 2, 1, [], [], true, true));
        collector.Record(new("alpha", "no", "b", "task", 3, 1, [], [], true, false));
        var first = collector.Freeze(false);
        False(first.GlobalLast.ContainsKey("never")); False(first.Counts.ContainsKey("never"));
        False(first.GlobalLast.TryGetValue("unknown", out _)); False(first.Counts.TryGetValue("never", out _));
        True(first.GlobalLast.Keys.SequenceEqual(new[] { "beta", "alpha" }));
        True(first.Counts.Keys.SequenceEqual(new[] { "beta", "alpha" }));
        True(first.GlobalSummaries.Select(s => s.InstructionId).SequenceEqual(new[] { "beta", "alpha" }));
        Equal(2, first.Counts.Count); Equal(2L, first.Counts["beta"]); Equal(1L, first.Counts["alpha"]);
        var missingThrew = false;
        try { _ = first.Counts["never"]; } catch (KeyNotFoundException) { missingThrew = true; }
        True(missingThrew);
        collector.Reset(); collector.Record(new("never", "no", "b", "task", 1, 1, [], [], true, false));
        var second = collector.Freeze(false);
        Equal(1, second.GlobalLast.Count); Equal(1L, second.Counts["never"]);
        False(second.Counts.ContainsKey("beta")); Equal(2L, first.Counts["beta"]);
        True(first.GlobalLast["beta"].LocalResult); False(first.GlobalLast["alpha"].LocalResult);
    }

    private static void OriginBindingDoesNotEagerlyCloneEveryRetainedEvent()
    {
        var ids = Enumerable.Range(0, 1000).Select(i => "event" + i).ToArray();
        var collector = new InstructionObservationCollector(ids);
        foreach (var id in ids) collector.Record(new(id, "no", "b", "task", 1, 1, [], [], true, true));
        var capture = collector.Freeze(false);
        var origin = new ObservationOrigin(Guid.NewGuid(), Guid.NewGuid(), 1, 1);
        _ = capture.WithOrigin(origin); // Warm the binding path outside measurement.
        var before = GC.GetAllocatedBytesForCurrentThread();
        var bound = capture.WithOrigin(origin);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated >= 16384) throw new InvalidOperationException("Binding an immutable origin must not eagerly duplicate 1000 event objects/indexes; allocated " + allocated);
        Equal(origin, bound.GlobalLast[ids[999]].Origin!);
        True(capture.GlobalLast[ids[999]].Origin is null);
    }
    private static void FrozenViewsSurviveCollectorReuseWithExactTraceCounts()
    {
        var collector = new InstructionObservationCollector(["x"], traceCapacity: 2);
        for (var i = 1; i <= 3; i++) collector.Record(new("x", "no", "b", "task", i, i, [new("site", "b", i)], [], true, i % 2 == 1));
        var first = collector.Freeze(false);
        collector.Reset();
        collector.Record(new("x", "no", "b", "task", 1, 1, [], [], true, false));
        var second = collector.Freeze(false);
        Equal(3L, first.Counts["x"]);
        Equal(3L, first.GlobalLast["x"].ObservationCountInScan);
        Equal(2L, first.Trace[0].ObservationCountInScan);
        Equal(3L, first.Trace[1].ObservationCountInScan);
        Equal(3L, first.Contexts.Values.Single().ContextCountInScan);
        True(first.GlobalLast["x"].LocalResult);
        False(second.GlobalLast["x"].LocalResult);
        Equal(1L, first.TraceDropped);
        Equal(1L, second.Counts["x"]);
    }

    private static void InternedTaskPathsRemainImmutableAcrossScansAndReset()
    {
        var runtime = Runtime(ObservationProgram(new LadderNetwork("n", "Path", ObservationEnable("c"), Coil: new("coil", "output"))));
        runtime.Run();
        var first = runtime.Scan(new Dictionary<string, bool> { ["enable"] = true }).Observations!;
        var contact = first.GlobalLast["c"];
        var coil = first.GlobalLast["coil"];
        True(ReferenceEquals(contact.CallPath, coil.CallPath));
        var task = contact.CallPath[0];
        runtime.Scan(new Dictionary<string, bool> { ["enable"] = false });
        runtime.Reset();
        Equal(task, contact.CallPath[0]);
        True(contact.LocalResult);
        True(coil.LocalResult);
        Equal(1L, contact.ObservationCountInScan);
        Equal(1L, first.Counts["c"]);
    }

    private static void OriginBindingSharesImmutableEventsWithoutChangingSource()
    {
        var collector = new InstructionObservationCollector(["x"]);
        collector.Record(new("x", "no", "block", "task", 1, 1, [], [], true, true));
        var raw = collector.Freeze(false);
        var origin = new ObservationOrigin(Guid.NewGuid(), Guid.NewGuid(), 3, 8);
        var bound = raw.WithOrigin(origin);
        True(raw.GlobalLast["x"].Origin is null);
        Equal(origin, bound.GlobalLast["x"].Origin!);
        True(ReferenceEquals(bound.GlobalLast["x"], bound.Trace[0]));
        True(ReferenceEquals(bound.GlobalLast["x"], bound.Contexts.Values.Single()));
        Equal(raw.EstimatedBytes, bound.EstimatedBytes);
        var nextOrigin = origin with { ScanAttemptNumber = 9 };
        var rebound = bound.WithOrigin(nextOrigin);
        Equal(origin, bound.Trace[0].Origin!);
        Equal(nextOrigin, rebound.Trace[0].Origin!);
    }

    private static LadderProgram ObservationProgram(params LadderNetwork[] networks) => new(1,
        "observations", "Observations", "LD", TimeSpan.FromMilliseconds(10),
        [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
         new("value", PlcVariableType.Real, PlcVariableRole.Memory, 3.0),
         new("target", PlcVariableType.Real, PlcVariableRole.Memory, 3.0),
         new("timer", PlcVariableType.Timer, PlcVariableRole.Memory, 0),
         new("counter", PlcVariableType.Counter, PlcVariableRole.Memory, 0),
         new("output", PlcVariableType.Bool, PlcVariableRole.Output, false),
         new("text", PlcVariableType.String, PlcVariableRole.Memory, "")], networks);
    private static LadderNode ObservationEnable(string id) => new(id, LadderNodeKind.Contact, "enable");

    private static void LaterMoveDoesNotRewriteEarlierObservation()
    {
        var runtime = Runtime(ObservationProgram(
            new("n1", "First", ObservationEnable("c1"), NumericOperation: new("move1", LadderNumericOperationKind.Move, "value", "0", "target")),
            new("n2", "Later", ObservationEnable("c2"), NumericOperation: new("move2", LadderNumericOperationKind.Move, "9", "0", "value"))));
        runtime.Run();
        var snapshot = runtime.Scan(new Dictionary<string, bool> { ["enable"] = true });
        var observation = snapshot.Observations!.GlobalLast["move1"];
        Equal(3.0, (double)observation.Operands[0].Value!);
        Equal(3.0, (double)observation.StoredAfter!);
        Equal(9.0, snapshot.NumericVariables["value"]);
        runtime.Scan(new Dictionary<string, bool> { ["enable"] = true });
        Equal(3.0, (double)observation.Operands[0].Value!);
    }
    private static void SameValueWriteIsStillWrite()
    {
        var runtime = Runtime(ObservationProgram(new LadderNetwork("n", "Same", ObservationEnable("c"),
            NumericOperation: new("move", LadderNumericOperationKind.Move, "3", "0", "target"))));
        runtime.Run();
        Equal(InstructionWriteDisposition.WrittenSameValue,
            runtime.Scan(new Dictionary<string, bool> { ["enable"] = true }).Observations!.GlobalLast["move"].WriteDisposition);
    }
    private static void FalseTimerStillEvaluates()
    {
        var runtime = Runtime(ObservationProgram(new LadderNetwork("n", "Timer", ObservationEnable("c"),
            Timer: new("ton", "timer", TimeSpan.FromMilliseconds(100)))));
        runtime.Run();
        runtime.Scan(new Dictionary<string, bool>());
        var observation = runtime.Scan(new Dictionary<string, bool>()).Observations!.GlobalLast["ton"];
        True(observation.Evaluated);
        Equal(InstructionWriteDisposition.StateUpdatedSameValue, observation.WriteDisposition);
    }
    private static void ObservationCatalogSetEquality()
    {
        Equal(54, InstructionObservationRegistry.CatalogKeys.Count);
        True(LadderInstructionCatalog.Entries.Select(e => e.Key).ToHashSet().SetEquals(InstructionObservationRegistry.CatalogKeys));
    }
    private static void ObservationContextOverflowKeepsGlobalLast()
    {
        var collector = new InstructionObservationCollector(["x"], contextCapacity: 1, traceCapacity: 1);
        collector.Record(new InstructionObservation("x", "no", "block", "task", 1, 1, [], [], true, true));
        collector.Record(new InstructionObservation("x", "no", "block", "other-task", 2, 2, [], [], true, false));
        var capture = collector.Freeze(false);
        Equal(2L, capture.Counts["x"]);
        Equal("other-task", capture.GlobalLast["x"].TaskId);
        Equal(ObservationUnavailableReason.ContextNotCaptured, capture.FindContext("x", "other-task", 2).Reason);
    }
    private static void ObservationTraceOverflowKeepsCounts()
    {
        var collector = new InstructionObservationCollector(["x"], traceCapacity: 1);
        for (var i = 1; i <= 3; i++) collector.Record(new InstructionObservation("x", "no", "b", "t", i, 1, [], [], true, true));
        var capture = collector.Freeze(false);
        Equal(3L, capture.Counts["x"]); Equal(2L, capture.TraceDropped);
        Equal(3L, capture.Trace[0].ExecutionOrdinal);
    }
    private static void ObservationTextTruncationPreservesSurrogates()
    {
        var operand = new InstructionOperand("A", "text", PlcVariableType.String, new string('a', 4095) + "😀");
        Equal(4097, operand.TotalTextLength); Equal(4095, ((string)operand.Value!).Length);
        True(operand.TextTruncated);
    }
    private static void EveryRuntimeInstructionFamilyProducesObservation()
    {
        var covered=new HashSet<string>();
        void Capture(LadderProgram program)
        {
            var runtime=Runtime(program);runtime.Run();
            var snapshot=runtime.Scan(new Dictionary<string,bool>{["enable"]=true});
            foreach(var observation in snapshot.Observations!.GlobalLast.Values)covered.Add(observation.CatalogKey);
        }
        foreach(var kind in Enum.GetValues<LadderNumericOperationKind>())
            Capture(ObservationProgram(new LadderNetwork("n","Math",ObservationEnable("c"),
                NumericOperation:new("op",kind,kind is LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine?"0.5":"2",kind==LadderNumericOperationKind.FormatText?"\"{0:F1}\"":"3",
                    kind==LadderNumericOperationKind.FormatText?"text":"target","10"))));
        foreach(var kind in Enum.GetValues<LadderCoilMode>())
            Capture(ObservationProgram(new LadderNetwork("n","Coil",ObservationEnable("c"),Coil:new("op","output",kind))));
        foreach(var kind in Enum.GetValues<LadderTimerKind>())
            Capture(ObservationProgram(new LadderNetwork("n","Timer",ObservationEnable("c"),Timer:new("op","timer",TimeSpan.FromMilliseconds(100),kind))));
        foreach(var kind in Enum.GetValues<LadderCounterKind>())
            Capture(ObservationProgram(new LadderNetwork("n","Count",ObservationEnable("c"),Counter:new("op","counter",2,kind))));
        foreach(var kind in Enum.GetValues<LadderCompareOperator>())
            Capture(ObservationProgram(new LadderNetwork("n","Compare",new("cmp",LadderNodeKind.Compare,
                kind==LadderCompareOperator.ContainsCode?"\"CODE\"":"2",CompareOperator:kind,
                RightOperand:kind==LadderCompareOperator.ContainsCode?"\"CODE\"":"3"),Coil:new("op","output"))));
        foreach(var edge in Enum.GetValues<LadderEdgeMode>())
            Capture(ObservationProgram(new LadderNetwork("n","Contact",new("c",LadderNodeKind.Contact,"enable",EdgeMode:edge),Coil:new("op","output"))));
        Capture(ObservationProgram(new LadderNetwork("n","Closed",new("c",LadderNodeKind.Contact,"enable",true),Coil:new("op","output"))));
        Capture(ObservationProgram(new LadderNetwork("n","Branch",new("p",LadderNodeKind.Parallel,Children:[ObservationEnable("c1"),ObservationEnable("c2")]),Coil:new("op","output"))));
        Capture(ObservationProgram(
            new("tr","Reset timer",ObservationEnable("c1"),TimerReset:new("op1","timer")),
            new("cr","Reset counter",ObservationEnable("c2"),CounterReset:new("op2","counter")),
            new("cl","Load counter",ObservationEnable("c3"),CounterLoad:new("op3","counter",5)),
            new("l","Label",ObservationEnable("c4"),LabelInstruction:new("op4","label")),
            new("j","Jump",new("c5",LadderNodeKind.Contact,"enable",true),Jump:new("op5","label")),
            new("r","Return",ObservationEnable("c6"),Return:new("op6"))));
        var caller=ObservationProgram();
        Capture(caller with { Blocks=[new("main","Main",[new("n","Call",ObservationEnable("c"),Call:new("call","child"))]),
            new("child","Child",[new("child-n","Return",ObservationEnable("cc"),Return:new("return"))])],EntryBlock="main" });
        True(covered.SetEquals(InstructionObservationRegistry.CatalogKeys));
    }
    private static void SharedCounterShowsInstructionAndInstanceValues()
    {
        var runtime=Runtime(ObservationProgram(
            new("up","Up",ObservationEnable("c1"),Counter:new("up-box","counter",5)),
            new("load","Load",ObservationEnable("c2"),CounterLoad:new("load-box","counter",9))));
        runtime.Run();var snapshot=runtime.Scan(new Dictionary<string,bool>{["enable"]=true});
        var up=snapshot.Observations!.GlobalLast["up-box"];
        Equal(false,up.PreviousInput!.Value);Equal(true,up.CurrentInput!.Value);
        Equal(1L,((LadderCounterState)up.StoredAfter!).Accumulated);
        Equal(9L,snapshot.Counters["counter"].Accumulated);
    }
    private static void RecoverableNumericFaultCompletesWithoutWrite()
    {
        var program=ObservationProgram(new LadderNetwork("n","Div",ObservationEnable("c"),
            NumericOperation:new("div",LadderNumericOperationKind.Divide,"2","zero","target")));
        program=program with {Variables=program.Variables.Append(new PlcVariable("zero",PlcVariableType.Real,PlcVariableRole.Memory,0.0)).ToArray()};
        var runtime=Runtime(program);
        runtime.Run();var snapshot=runtime.Scan(new Dictionary<string,bool>{["enable"]=true});
        Equal(ControllerCompletion.Completed,snapshot.Completion);
        Equal(3.0,snapshot.NumericVariables["target"]);
        Equal(InstructionWriteDisposition.FailedNoWrite,snapshot.Observations!.GlobalLast["div"].WriteDisposition);
    }
    private static void MonitoringOnOffGoldenResultsMatch()
    {
        var program=ObservationProgram(
            new("timer-net","Timer",ObservationEnable("c1"),Timer:new("timer-op","timer",TimeSpan.FromMilliseconds(30))),
            new("counter-net","Counter",new("edge",LadderNodeKind.Contact,"enable",EdgeMode:LadderEdgeMode.Rising),Counter:new("count-op","counter",3)),
            new("math-net","Math",ObservationEnable("c3"),NumericOperation:new("math-op",LadderNumericOperationKind.Add,"value","2","value")));
        var on=Runtime(program);var off=Runtime(program);off.MonitoringEnabled=false;on.Run();off.Run();
        for(var i=0;i<12;i++)
        {
            var input=new Dictionary<string,bool>{["enable"]=i%3!=0};var a=on.Scan(input);var b=off.Scan(input);
            string Result(VirtualControllerSnapshot s)=>System.Text.Json.JsonSerializer.Serialize(new
            {s.ScanNumber,s.SimulatedTime,s.State,s.Variables,s.NumericVariables,s.Outputs,s.NumericOutputs,s.Timers,s.Counters,s.Tasks,s.Forces,s.Diagnostics,s.Elements,s.TextVariables,s.TextOutputs});
            Equal(Result(a),Result(b));
        }
    }
    private static void ObservationDispositionsNormalizeAndClear()
    {
        var program=ObservationProgram(
            new("format","Format",ObservationEnable("cf"),NumericOperation:new("format-op",LadderNumericOperationKind.FormatText,"2","template","text")),
            new("convert","Convert",ObservationEnable("cc"),NumericOperation:new("convert-op",LadderNumericOperationKind.Convert,"40000.5","0","integer")),
            new("set","Set",ObservationEnable("cs"),Coil:new("set-op","output",LadderCoilMode.Set)));
        program=program with {Variables=program.Variables.Append(new PlcVariable("integer",PlcVariableType.Int,PlcVariableRole.Memory,0))
            .Append(new PlcVariable("template",PlcVariableType.String,PlcVariableRole.Memory,"invalid")).ToArray()};
        var runtime=Runtime(program);runtime.Run();var snapshot=runtime.Scan(Inputs(("enable",true)));
        Equal(InstructionWriteDisposition.FailedClearedDestination,snapshot.Observations!.GlobalLast["format-op"].WriteDisposition);
        Equal("",(string)snapshot.Observations.GlobalLast["format-op"].StoredAfter!);
        var convert=snapshot.Observations.GlobalLast["convert-op"];
        True(convert.Rounded);True(convert.Clamped);Equal(32767.0,(double)convert.StoredAfter!);
        var second=runtime.Scan(Inputs(("enable",false)));
        Equal(InstructionWriteDisposition.SkippedDisabled,second.Observations!.GlobalLast["set-op"].WriteDisposition);
        Equal(ObservationUnavailableReason.NotApplicable,second.Observations.GlobalLast["convert-op"].Operands.Single(o=>o.Role=="B").Reason);
        Equal(ObservationUnavailableReason.ActionDisabled,second.Observations.GlobalLast["convert-op"].Operands.Single(o=>o.Role=="A").Reason);
    }
    private static void InvocationPathsDoNotSubstituteOtherTasks()
    {
        var program=ObservationProgram() with {
            Blocks=[new("main","Main",[new("n1","Call1",ObservationEnable("c1"),Call:new("call1","child")),new("n2","Call2",ObservationEnable("c2"),Call:new("call2","child"))]),
                new("child","Child",[new("child-net","Write",ObservationEnable("child-contact"),NumericOperation:new("child-move",LadderNumericOperationKind.Move,"2","0","target"))])],
            EntryBlock="main",Tasks=[new("task1","Task1",LadderTaskKind.Continuous,TimeSpan.FromMilliseconds(10),1,"main"),new("task2","Task2",LadderTaskKind.Continuous,TimeSpan.FromMilliseconds(10),2,"main")]};
        var runtime=Runtime(program);runtime.Run();var capture=runtime.Scan(Inputs(("enable",true))).Observations!;
        Equal(4L,capture.Counts["child-move"]);
        Equal("task2",capture.GlobalLast["child-move"].TaskId);
        var selected=capture.FindPath("child-move","task1",["task1","call1"]);
        Equal(ObservationUnavailableReason.None,selected.Reason);Equal("task1",selected.Observation!.TaskId);
        Equal(ObservationUnavailableReason.ContextNotCaptured,capture.FindPath("child-move","task1",["task1","missing"]).Reason);
    }
    private static void IncomingContinuityIsSeparateFromLocalResult()
    {
        var logic=new LadderNode("series",LadderNodeKind.Series,Children:[ObservationEnable("first"),
            new("compare",LadderNodeKind.Compare,"2",CompareOperator:LadderCompareOperator.Equal,RightOperand:"2"),
            new("parallel",LadderNodeKind.Parallel,Children:[new("branch1",LadderNodeKind.Contact,"enable",true),new("branch2",LadderNodeKind.Contact,"enable",true)])]);
        var runtime=Runtime(ObservationProgram(new LadderNetwork("n","Flow",logic,Coil:new("out","output"))));
        runtime.Run();var capture=runtime.Scan(new Dictionary<string,bool>()).Observations!;
        var compare=capture.GlobalLast["compare"];Equal(true,compare.LocalResult);Equal(false,compare.NetworkEnable!.Value);
        Equal(2.0,(double)compare.Operands[0].Value!);Equal(false,capture.GlobalLast["branch1"].NetworkEnable!.Value);
        Equal(false,capture.GlobalLast["branch2"].NetworkEnable!.Value);Equal(true,capture.GlobalLast["branch1"].LocalResult);
        Equal(1L,capture.Counts["compare"]);Equal(1L,capture.Counts["branch1"]);
    }
}

