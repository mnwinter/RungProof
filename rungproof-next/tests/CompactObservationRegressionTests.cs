using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterCompactObservationRegressionTests()
    {
        Test("UnmaterializedCompactRuntimeCaptureSurvivesScanAndReset", UnmaterializedCompactRuntimeCaptureSurvivesScanAndReset);
        Test("MixedCompactAndRichSlotsSurviveEvictionAndReset", MixedCompactAndRichSlotsSurviveEvictionAndReset);
        Test("CompactNullableFieldsRoundTripIndependently", CompactNullableFieldsRoundTripIndependently);
        Test("ConcurrentCompactReadersKeepOriginsIsolated", ConcurrentCompactReadersKeepOriginsIsolated);
        Test("VisibleInstructionDoesNotMaterializeWholeCapture", VisibleInstructionDoesNotMaterializeWholeCapture);
        Test("LazyRichRuntimePayloadSurvivesScanAndReset", LazyRichRuntimePayloadSurvivesScanAndReset);
        Test("LazyRichSlotsRetainPayloadAcrossRecycling", LazyRichSlotsRetainPayloadAcrossRecycling);
        Test("DenseObservationMapsPreserveSparseFirstObservationOrder", DenseObservationMapsPreserveSparseFirstObservationOrder);
        Test("ObservationRegistryOwnsItsInputArray", ObservationRegistryOwnsItsInputArray);
    }

    private static void DenseObservationMapsPreserveSparseFirstObservationOrder()
    {
        var collector = new InstructionObservationCollector(["a", "never", "b", "c", "A"], traceCapacity: 2);
        RecordCompactRegression(collector, "c", "task", 1, true, null, true);
        RecordCompactRegression(collector, "a", "task", 2, false, true, false);
        collector.Record(new("unknown", "no", "block", "task", 3, 1, [], [], true, true));
        RecordCompactRegression(collector, "c", "task", 4, false, true, false);
        collector.Record(new("b", "numeric-0", "block", "task", 5, 1, [], [], true, true) { StoredAfter = 7.0 });
        var retained = collector.Freeze(false);
        collector.Reset();
        var empty = collector.Freeze(false);
        RecordCompactRegression(collector, "A", "task", 1, true, false, true);
        var next = collector.Freeze(false);

        void VerifyOrder(InstructionObservationCapture capture, string[] expected)
        {
            Equal(expected.Length, capture.GlobalLast.Count); Equal(expected.Length, capture.Counts.Count);
            True(capture.GlobalLast.Keys.SequenceEqual(expected)); True(capture.Counts.Keys.SequenceEqual(expected));
            True(capture.GlobalLast.Select(p => p.Key).SequenceEqual(expected));
            True(capture.Counts.Select(p => p.Key).SequenceEqual(expected));
            True(capture.GlobalLast.Values.Select(o => o.InstructionId).SequenceEqual(expected));
            True(capture.GlobalSummaries.Select(o => o.InstructionId).SequenceEqual(expected));
            True(capture.Counts.Values.SequenceEqual(expected.Select(id => capture.Counts[id])));
            foreach (var pair in capture.GlobalLast)
            {
                True(capture.GlobalLast.ContainsKey(pair.Key)); True(capture.Counts.ContainsKey(pair.Key));
                True(capture.GlobalLast.TryGetValue(pair.Key, out var observation));
                True(ReferenceEquals(pair.Value, observation));
                True(capture.Counts.TryGetValue(pair.Key, out var count));
                Equal(count, pair.Value.ObservationCountInScan);
            }
        }
        void VerifyAbsent(InstructionObservationCapture capture, string id)
        {
            False(capture.GlobalLast.ContainsKey(id)); False(capture.Counts.ContainsKey(id));
            False(capture.GlobalLast.TryGetValue(id, out var observation)); True(observation is null);
            False(capture.Counts.TryGetValue(id, out var count)); Equal(0L, count);
            var globalThrew = false; var countThrew = false;
            try { _ = capture.GlobalLast[id]; } catch (KeyNotFoundException) { globalThrew = true; }
            try { _ = capture.Counts[id]; } catch (KeyNotFoundException) { countThrew = true; }
            True(globalThrew); True(countThrew);
        }
        VerifyOrder(retained, ["c", "a", "b"]);
        Equal(2L, retained.Counts["c"]); Equal(4L, retained.GlobalLast["c"].ExecutionOrdinal);
        Equal(1L, retained.Counts["a"]); Equal(1L, retained.Counts["b"]);
        foreach (var id in new[] { "never", "unknown", "A" }) VerifyAbsent(retained, id);
        VerifyOrder(empty, []);
        foreach (var id in new[] { "a", "b", "c", "never", "unknown", "A" }) VerifyAbsent(empty, id);
        VerifyOrder(next, ["A"]); VerifyAbsent(next, "a"); VerifyAbsent(next, "c");
        // Rebinding retains original ordering and sparse membership, independently of collector reuse.
        VerifyOrder(retained.WithOrigin(new(Guid.NewGuid(), Guid.NewGuid(), 1, 1)), ["c", "a", "b"]);
    }

    private static void ObservationRegistryOwnsItsInputArray()
    {
        string[] source = ["a", "b", "c"];
        var registry = new ObservationIdRegistry(source);
        var collector = new InstructionObservationCollector(source);
        source[0] = "replacement"; source[1] = "a"; source[2] = "other";
        Equal("a", registry.Id(0)); Equal("b", registry.Id(1)); Equal("c", registry.Id(2));
        True(registry.TryIndex("a", out var index)); Equal(0, index);
        False(registry.TryIndex("replacement", out _));
        RecordCompactRegression(collector, "b", "task", 1, true, null, true);
        RecordCompactRegression(collector, "a", "task", 2, false, true, false);
        RecordCompactRegression(collector, "replacement", "task", 3, true, null, true);
        var retained = collector.Freeze(false);
        collector.Reset();
        True(retained.Counts.Keys.SequenceEqual(new[] { "b", "a" }));
        True(retained.GlobalLast.Keys.SequenceEqual(new[] { "b", "a" }));
        False(retained.Counts.ContainsKey("replacement"));
        Equal(1L, retained.Counts["a"]); Equal("b", retained.GlobalLast["b"].InstructionId);
    }

    private static void LazyRichRuntimePayloadSurvivesScanAndReset()
    {
        var program = ObservationProgram(
            new("convert-net", "Convert", ObservationEnable("convert-contact"),
                NumericOperation: new LadderNumericOperation("convert", LadderNumericOperationKind.Convert,
                    "40000.5", "0", "integer")
                {
                    InputDataType = LadderNumericDataType.Real,
                    OutputDataType = LadderNumericDataType.Int,
                }),
            new("divide-net", "Modulo fault", ObservationEnable("divide-contact"),
                NumericOperation: new("divide", LadderNumericOperationKind.Modulo, "value", "zero", "target")),
            new("timer-net", "Timer", ObservationEnable("timer-contact"),
                Timer: new("timer-action", "timer", TimeSpan.FromMilliseconds(100))),
            new("return-net", "Return", ObservationEnable("return-contact"), Return: new("return-action")))
            with { Variables = ObservationProgram().Variables
                .Append(new PlcVariable("integer", PlcVariableType.Int, PlcVariableRole.Memory, 0))
                .Append(new PlcVariable("zero", PlcVariableType.Real, PlcVariableRole.Memory, 0.0)).ToArray() };
        var runtime = Runtime(program); runtime.Run();
        var capture = runtime.Scan(Inputs(("enable", true))).Observations!;
        // Keep rich backing untouched until its mutable collector and draft pool have been reused.
        runtime.Scan(Inputs(("enable", false)));
        runtime.Reset();
        var converted = capture.GlobalLast["convert"];
        Equal(0.0, (double)converted.DestinationBefore!); Equal(0.0, (double)converted.StoredAfter!);
        False(converted.Rounded); False(converted.Clamped);
        Equal(InstructionWriteDisposition.FailedNoWrite, converted.WriteDisposition);
        True(converted.Diagnostics.Count > 0);
        var fault = capture.GlobalLast["divide"];
        Equal(InstructionWriteDisposition.FailedErrorValueWritten, fault.WriteDisposition);
        Equal(3.0, (double)fault.DestinationBefore!); Equal(0.0, (double)fault.CalculatedResult!);
        Equal(0.0, (double)fault.StoredAfter!);
        True(fault.Diagnostics.Count > 0); Equal("divide", fault.Diagnostics[0].InstructionId);
        True(fault.Diagnostics[0].Message.Length > 0);
        Equal(0.0, (double)fault.Operands.Single(o => o.Role == "B").Value!);
        var timer = capture.GlobalLast["timer-action"];
        Equal(TimeSpan.FromMilliseconds(100), (TimeSpan)timer.ConfiguredPreset!);
        Equal(TimeSpan.Zero, ((LadderTimerState)timer.DestinationBefore!).Accumulated);
        Equal(TimeSpan.FromMilliseconds(10), ((LadderTimerState)timer.StoredAfter!).Accumulated);
        Equal("Taken", capture.GlobalLast["return-action"].ControlOutcome);
        True(capture.EstimatedBytes > 0);
    }

    private static void LazyRichSlotsRetainPayloadAcrossRecycling()
    {
        var collector = new InstructionObservationCollector(["x"], contextCapacity: 1, traceCapacity: 2);
        for (var i = 1; i <= 9; i++)
        {
            var path = new ObservationCallPath([new("site", "block", i)]);
            var prepared = collector.Prepare("x", "task", path, false);
            collector.RecordRich(new("x", "call", "block", "task", "destination"), path, i, i,
                1u | 8192u | 16384u, InstructionWriteDisposition.WrittenChanged,
                [new(new("A", "value", PlcVariableType.Real, ObservationUnavailableReason.None, null), (double)i)],
                new((double)i - 1, (double)i + 0.5, (double)i, TimeSpan.FromMilliseconds(i),
                    "Entered " + i, i + 10, "instance-" + i, [new("CODE", "x", "A", "Program", "diagnostic " + i)]), prepared);
        }
        var capture = collector.Freeze(false);
        collector.Reset();
        for (var i = 1; i <= 20; i++) RecordCompactRegression(collector, "x", "task", i, false, null, false);
        foreach (var item in capture.Trace)
        {
            var ordinal = item.ExecutionOrdinal;
            Equal((double)ordinal - 1, (double)item.DestinationBefore!);
            Equal((double)ordinal + 0.5, (double)item.CalculatedResult!);
            Equal((double)ordinal, (double)item.StoredAfter!);
            Equal(TimeSpan.FromMilliseconds(ordinal), (TimeSpan)item.ConfiguredPreset!);
            Equal("Entered " + ordinal, item.ControlOutcome);
            Equal(ordinal + 10, item.CalledFrameInvocationId!.Value);
            Equal("instance-" + ordinal, item.CalledInstanceId!);
            Equal("diagnostic " + ordinal, item.Diagnostics.Single().Message);
            True(item.Rounded); True(item.Clamped);
        }
        Equal(8L, capture.Trace[0].ExecutionOrdinal); Equal(9L, capture.Trace[1].ExecutionOrdinal);
        Equal(9L, capture.GlobalLast["x"].ObservationCountInScan);
        Equal(9L, capture.Contexts.Values.Single().ContextCountInScan);
    }

    private static void UnmaterializedCompactRuntimeCaptureSurvivesScanAndReset()
    {
        var runtime = Runtime(ObservationProgram(new LadderNetwork("n", "Edge", new("edge", LadderNodeKind.Contact,
            "enable", EdgeMode: LadderEdgeMode.Rising), Coil: new("coil", "output"))));
        runtime.Run();
        runtime.Scan(Inputs(("enable", false)));
        // Retain only the capture. Materializing a record here would mask stale backing-buffer bugs.
        var retained = runtime.Scan(Inputs(("enable", true))).Observations!;
        runtime.Scan(Inputs(("enable", false)));
        runtime.Reset();
        var edge = retained.GlobalLast["edge"];
        True(edge.Evaluated); True(edge.LocalResult); True(edge.PreviousKnown);
        Equal(false, edge.PreviousInput!.Value); Equal(true, edge.CurrentInput!.Value);
        Equal(true, edge.NetworkEnable!.Value);
        Equal("enable", edge.Operands.Single().Identity);
        Equal(true, (bool)edge.Operands.Single().Value!);
        Equal(InstructionWriteDisposition.StateUpdatedChanged, edge.WriteDisposition);
        True(edge.DestinationBefore is null); True(edge.StoredAfter is null);
        var coil = retained.GlobalLast["coil"];
        Equal(false, (bool)coil.DestinationBefore!); Equal(true, (bool)coil.StoredAfter!);
        Equal("output", coil.DestinationIdentity);
        Equal(InstructionWriteDisposition.WrittenChanged, coil.WriteDisposition);
        False(coil.PreviousKnown); True(coil.PreviousInput is null); True(coil.CurrentInput is null);
        Equal(1L, edge.ObservationCountInScan); Equal(1L, edge.ContextCountInScan);
        True(edge.CallPath.Count > 0); Equal(edge.CallPath[0], coil.CallPath[0]);
    }

    private static void MixedCompactAndRichSlotsSurviveEvictionAndReset()
    {
        foreach (var traceCapacity in new[] { 0, 1, 2 })
        {
            var collector = new InstructionObservationCollector(["x"], contextCapacity: 1, traceCapacity: traceCapacity);
            for (var i = 1; i <= 12; i++)
            {
                var task = i % 3 == 1 ? "kept" : "overflow" + i;
                if (i % 2 == 1) RecordCompactRegression(collector, "x", task, i, true, false, true);
                else collector.Record(new("x", "numeric-0", "block", task, i, i, [new("site", "block", i)],
                    [new("A", "number", PlcVariableType.Real, (double)i)], true, false)
                    { StoredAfter = (double)i, CalculatedResult = (double)i + 0.5,
                        Diagnostics = [new("TEST", "x", "A", "Program", "retained " + i)] });
            }
            var retained = collector.Freeze(false);
            collector.Reset();
            // Force formerly rich and compact slots to be reused before any old record is read.
            for (var i = 1; i <= 20; i++) RecordCompactRegression(collector, "x", "new", i, false, null, false);
            var current = collector.Freeze(false);
            Equal(12L, retained.Counts["x"]); Equal(20L, current.Counts["x"]);
            Equal(12.0, (double)retained.GlobalLast["x"].StoredAfter!);
            Equal(12.5, (double)retained.GlobalLast["x"].CalculatedResult!);
            Equal("retained 12", retained.GlobalLast["x"].Diagnostics.Single().Message);
            var context = retained.Contexts.Values.Single();
            Equal(10L, context.ExecutionOrdinal); Equal(4L, context.ContextCountInScan);
            Equal(12L, context.ObservationCountInScan);
            Equal(traceCapacity, retained.Trace.Count); Equal(12L - traceCapacity, retained.TraceDropped);
            for (var j = 0; j < retained.Trace.Count; j++)
            {
                var expectedOrdinal = 13 - traceCapacity + j;
                var value = retained.Trace[j];
                Equal((long)expectedOrdinal, value.ExecutionOrdinal);
                Equal((long)expectedOrdinal, value.ObservationCountInScan);
                if (expectedOrdinal % 2 == 1)
                {
                    Equal(true, (bool)value.Operands.Single().Value!);
                    Equal(false, value.PreviousInput!.Value); Equal(true, value.CurrentInput!.Value);
                    Equal(0, value.Diagnostics.Count); True(value.CalculatedResult is null);
                }
                else Equal((double)expectedOrdinal, (double)value.Operands.Single().Value!);
            }
        }
    }

    private static void CompactNullableFieldsRoundTripIndependently()
    {
        bool?[] values = [null, false, true];
        // Different fields use shifted permutations, detecting overlap and absent/false confusion.
        for (var i = 0; i < values.Length; i++)
        {
            var collector = new InstructionObservationCollector(["x"]);
            var path = new ObservationCallPath([]);
            var prepared = collector.Prepare("x", "task", path, false);
            var enable = values[i]; var before = values[(i + 1) % 3]; var after = values[(i + 2) % 3];
            var flags = 1u | CompactObservationRow.NullableFlag(enable, 1)
                | CompactObservationRow.NullableFlag(before, 3) | CompactObservationRow.NullableFlag(after, 5)
                | 128u | CompactObservationRow.NullableFlag(after, 8) | CompactObservationRow.NullableFlag(before, 10) | 4096u;
            collector.RecordBoolean(new("x", "coil", "block", "task", "output"), path, 7, 9, flags,
                InstructionWriteDisposition.WrittenChanged, [], prepared);
            var result = collector.Freeze(false).GlobalLast["x"];
            True(result.NetworkEnable == enable); True(Equals(result.DestinationBefore, before));
            True(Equals(result.StoredAfter, after)); True(result.PreviousInput == after); True(result.CurrentInput == before);
            True(result.PreviousKnown); True(result.PathTruncated); True(result.LocalResult);
            Equal(7L, result.ExecutionOrdinal); Equal(9L, result.FrameInvocationId);
        }
    }

    private static void ConcurrentCompactReadersKeepOriginsIsolated()
    {
        var collector = new InstructionObservationCollector(["x"]);
        RecordCompactRegression(collector, "x", "task", 1, true, false, true);
        var raw = collector.Freeze(false);
        var firstOrigin = new ObservationOrigin(Guid.NewGuid(), Guid.NewGuid(), 3, 7);
        var secondOrigin = firstOrigin with { ScanAttemptNumber = 8 };
        var first = raw.WithOrigin(firstOrigin); var second = first.WithOrigin(secondOrigin);
        collector.Reset();
        Parallel.For(0, 256, i =>
        {
            var capture = i % 2 == 0 ? first : second;
            var expected = i % 2 == 0 ? firstOrigin : secondOrigin;
            var value = capture.GlobalLast["x"];
            Equal(expected, value.Origin!);
            True(ReferenceEquals(value, capture.Trace[0]));
            True(ReferenceEquals(value, capture.Contexts.Values.Single()));
            Equal(true, (bool)value.Operands.Single().Value!);
            True(raw.GlobalLast["x"].Origin is null);
            Equal(raw.EstimatedBytes, capture.EstimatedBytes);
        });
    }

    private static void VisibleInstructionDoesNotMaterializeWholeCapture()
    {
        var ids = Enumerable.Range(0, 5000).Select(i => "visible-test-" + i).ToArray();
        var collector = new InstructionObservationCollector(ids);
        foreach (var id in ids) RecordCompactRegression(collector, id, "task", 1, true, false, true);
        var snapshot = PresentationSnapshot() with { Observations = collector.Freeze(false) };
        // Warm presenter machinery using another capture; the measured capture remains untouched.
        _ = RungProof.Next.App.LadderInstructionPresentation.PresentSnapshot("instruction", "no",
            PresentationSnapshot(PresentationObservation("no")));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var view = RungProof.Next.App.LadderInstructionPresentation.PresentSnapshot(ids[0], "edge-rising", snapshot);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Equal(ObservationUnavailableReason.None, view.UnavailableReason);
        Equal(2, view.Contexts.Count);
        if (allocated >= 131072)
            throw new InvalidOperationException("Presenting one visible instruction must not allocate records for all 5000 instructions; bytes=" + allocated);
    }

    private static void RecordCompactRegression(InstructionObservationCollector collector, string id, string task,
        long ordinal, bool local, bool? previous, bool current)
    {
        var path = new ObservationCallPath([new("site", "block", ordinal)]);
        var prepared = collector.Prepare(id, task, path, false);
        var flags = (local ? 1u : 0) | CompactObservationRow.NullableFlag(true, 1)
            | (previous.HasValue ? 128u : 0) | CompactObservationRow.NullableFlag(previous, 8)
            | CompactObservationRow.NullableFlag(current, 10);
        collector.RecordBoolean(new(id, "edge-rising", "block", task, ""), path, ordinal, ordinal, flags,
            InstructionWriteDisposition.StateUpdatedChanged,
            [new(new("A", "input", PlcVariableType.Bool, ObservationUnavailableReason.None, null), current)], prepared);
    }
}

