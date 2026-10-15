using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.App;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterInstructionPresentationTests()
    {
        Test("PresenterEveryCatalogKeyAndDisposition", PresenterEveryCatalogKeyAndDisposition);
        Test("PresenterRealRuntimeCatalogCoverage", PresenterRealRuntimeCatalogCoverage);
        Test("PresenterRealDisabledAndFaultEffects", PresenterRealDisabledAndFaultEffects);
        Test("PresenterParameterizedActualTransfers", PresenterParameterizedActualTransfers);
        Test("PresenterConsumedIsNotFinal", PresenterConsumedIsNotFinal);
        Test("PresenterDoesNotSubstituteContext", PresenterDoesNotSubstituteContext);
        Test("PresenterSharedCounterUsesInstructionState", PresenterSharedCounterUsesInstructionState);
        Test("PresenterLifecycleIdentityAndAbort", PresenterLifecycleIdentityAndAbort);
        Test("PresenterTypedEmptyFalseZeroAndTruncation", PresenterTypedEmptyFalseZeroAndTruncation);
        Test("TiaTimerAndCounterOutputFlowUsesQ", TiaTimerAndCounterOutputFlowUsesQ);
        Test("TiaNumericInstructionPortsMatchSiemensLad", TiaNumericInstructionPortsMatchSiemensLad);
        Test("TiaMonitorValuesOmitRedundantBoolType", TiaMonitorValuesOmitRedundantBoolType);
    }

    private static void TiaMonitorValuesOmitRedundantBoolType()
    {
        Equal("TRUE", TiaMonitorValueFormatter.Format("TRUE (BOOL)", true));
        Equal("FALSE", TiaMonitorValueFormatter.Format("FALSE (BOOL)", true));
        Equal("true", TiaMonitorValueFormatter.Format("true (bool)", true));
        Equal("TRUE (BOOL)", TiaMonitorValueFormatter.Format("TRUE (BOOL)", false));
        Equal("T#100ms", TiaMonitorValueFormatter.Format("T#100ms", true));
        Equal("2.71828175 (REAL)", TiaMonitorValueFormatter.Format("2.7182817459106445 (REAL)", true));
        Equal("0.99999994", TiaMonitorValueFormatter.FormatReal("0.99999994039535522"));
    }

    private static void TiaTimerAndCounterOutputFlowUsesQ()
    {
        static InstructionObservation Observation(string key, object storedAfter, bool? enable = true, bool evaluated = true, bool localResult = true)
            => new("instruction", key, "block", "task", 1, 1, [], [], evaluated, true)
            { NetworkEnable = enable, LocalResult = localResult, StoredAfter = storedAfter };

        True(LadderInstructionPresentation.OutputPowerFlow(Observation("ton", new LadderTimerState(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500), true, false))) == false);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("tof", new LadderTimerState(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500), true, true))) == true);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("tp", new LadderTimerState(TimeSpan.FromSeconds(1), TimeSpan.Zero, false, false))) == false);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("rto", new LadderTimerState(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), false, true))) == true);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("ctu", new LadderCounterState(3, 2, false, false))) == false);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("ctd", new LadderCounterState(0, 0, false, true))) == true);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("call", new object(), true, localResult: false)) == false);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("call", new object(), true, localResult: true)) == true);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("numeric-0", new object(), true)) == true);
        True(LadderInstructionPresentation.OutputPowerFlow(Observation("ton", new LadderTimerState(TimeSpan.Zero, TimeSpan.Zero, false, false), evaluated: false)) is null);
    }

    private static void TiaNumericInstructionPortsMatchSiemensLad()
    {
        foreach (var kind in new[] { LadderNumericOperationKind.Add, LadderNumericOperationKind.Subtract,
                     LadderNumericOperationKind.Multiply, LadderNumericOperationKind.Divide,
                     LadderNumericOperationKind.Modulo, LadderNumericOperationKind.Exponentiate })
        {
            Equal("IN1,IN2", string.Join(',', TiaNumericInstructionPorts.Inputs(kind)));
            Equal("OUT", TiaNumericInstructionPorts.Output(kind));
            True(TiaNumericInstructionPorts.HasEnablePins(kind));
        }

        foreach (var kind in new[] { LadderNumericOperationKind.Move, LadderNumericOperationKind.Absolute,
                     LadderNumericOperationKind.Negate, LadderNumericOperationKind.SquareRoot,
                     LadderNumericOperationKind.NaturalLog, LadderNumericOperationKind.Sine,
                     LadderNumericOperationKind.Cosine, LadderNumericOperationKind.Tangent,
                     LadderNumericOperationKind.ArcSine, LadderNumericOperationKind.ArcCosine,
                     LadderNumericOperationKind.ArcTangent, LadderNumericOperationKind.Truncate,
                     LadderNumericOperationKind.Convert, LadderNumericOperationKind.Round,
                     LadderNumericOperationKind.Ceiling, LadderNumericOperationKind.Floor })
        {
            Equal("IN", string.Join(',', TiaNumericInstructionPorts.Inputs(kind)));
            True(TiaNumericInstructionPorts.HasEnablePins(kind));
        }
        Equal("OUT1", TiaNumericInstructionPorts.Output(LadderNumericOperationKind.Move));
        Equal("MOVE", TiaNumericInstructionPorts.Mnemonic(LadderNumericOperationKind.Move));
        Equal("CONV", TiaNumericInstructionPorts.Mnemonic(LadderNumericOperationKind.Convert));
        Equal(32.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Exponentiate));
        Equal(32.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Add));
        Equal(32.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Subtract));
        Equal(32.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Multiply));
        Equal(32.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Divide));
        Equal(32.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Modulo));
        Equal(20.0f, TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Move));
        foreach (var kind in new[] { LadderNumericOperationKind.Move, LadderNumericOperationKind.Convert, LadderNumericOperationKind.Truncate, LadderNumericOperationKind.Round,
                     LadderNumericOperationKind.Ceiling, LadderNumericOperationKind.Floor })
            True(TiaNumericInstructionPorts.UsesLowerDataRow(kind));

        foreach (var kind in new[] { LadderNumericOperationKind.Normalize, LadderNumericOperationKind.Scale })
        {
            Equal("MIN,VALUE,MAX", string.Join(',', TiaNumericInstructionPorts.Inputs(kind)));
            Equal("OUT", TiaNumericInstructionPorts.Output(kind));
            True(TiaNumericInstructionPorts.HasEnablePins(kind));
        }

        True(!TiaNumericInstructionPorts.IsTiaInstruction(LadderNumericOperationKind.FormatText));
        True(!TiaNumericInstructionPorts.HasEnablePins(LadderNumericOperationKind.FormatText));
    }
    private static VirtualControllerSnapshot PresentationSnapshot(params InstructionObservation[] observations)
    {
        var collector = new InstructionObservationCollector(observations.Select(o => o.InstructionId));
        foreach (var observation in observations) collector.Record(observation);
        return new(1, TimeSpan.FromMilliseconds(10), VirtualControllerState.Running,
            new Dictionary<string, bool>(), new Dictionary<string, double> { ["destination"] = 99 },
            new Dictionary<string, bool>(), new Dictionary<string, double>(), new Dictionary<string, LadderTimerState>(),
            new Dictionary<string, LadderCounterState> { ["counter"] = new(9, 9, false, false) }, new Dictionary<string, LadderTaskState>(),
            new Dictionary<string, LadderForceState>(), [], new Dictionary<string, LadderElementState>())
        { Observations = collector.Freeze(false), Completion = ControllerCompletion.Completed };
    }
    private static InstructionObservation PresentationObservation(string key = "numeric-0", string task = "task", long ordinal = 1) =>
        new("instruction", key, "block", task, ordinal, ordinal, [new("call", "block", ordinal, "instance")],
            [new("A", "source", PlcVariableType.Real, 3.0)], true, true)
        { NetworkEnable = true, DestinationIdentity = "destination", DestinationBefore = 0.0, StoredAfter = 3.0, WriteDisposition = InstructionWriteDisposition.WrittenChanged };
    private static void PresenterEveryCatalogKeyAndDisposition()
    {
        True(LadderInstructionPresentation.CatalogKeys.ToHashSet().SetEquals(LadderInstructionCatalog.Entries.Select(e => e.Key)));
        Equal(54, LadderInstructionPresentation.CatalogKeys.Count);
        foreach (var key in LadderInstructionPresentation.CatalogKeys)
        {
            var view = LadderInstructionPresentation.PresentSnapshot("instruction", key, PresentationSnapshot(PresentationObservation(key)));
            True(view.Rows.Any(r => r.Label == (key is "numeric-18" or "numeric-19" ? "Consumed MIN" : key is "ctu" or "rto" ? "Consumed R" : key == "ctd" ? "Consumed LD" : "Consumed A") && r.Value.Contains("3")));
            Equal(ObservationUnavailableReason.None, view.UnavailableReason);
        }
        foreach (var disposition in Enum.GetValues<InstructionWriteDisposition>())
        {
            var view = LadderInstructionPresentation.PresentSnapshot("instruction", "numeric-0", PresentationSnapshot(PresentationObservation() with { WriteDisposition = disposition }));
            Equal(LadderInstructionPresentation.DispositionText(disposition), view.Rows.Single(r => r.Label == "Effect").Value);
        }
        foreach (var reason in Enum.GetValues<ObservationUnavailableReason>().Where(r => r != ObservationUnavailableReason.None))
            Equal(reason, LadderInstructionPresentation.Present("instruction", "no", null, unavailable: reason).UnavailableReason);
        foreach (var kind in Enum.GetValues<LadderNumericOperationKind>())
            True(LadderInstructionPresentation.CatalogKeys.Contains("numeric-" + (int)kind));
        foreach (var kind in Enum.GetValues<LadderCompareOperator>())
            True(LadderInstructionPresentation.CatalogKeys.Contains("compare-" + (int)kind));
    }
    private static void PresenterConsumedIsNotFinal()
    {
        var view = LadderInstructionPresentation.PresentSnapshot("instruction", "numeric-0", PresentationSnapshot(PresentationObservation()));
        Equal("3", view.Rows.Single(r => r.Label == "Stored after").Value);
        Equal("99", view.Rows.Single(r => r.Label == "Final snapshot").Value);
        True(view.Rows.Single(r => r.Label == "Consumed A").Value.StartsWith("3 (REAL)"));
    }
    private static void PresenterDoesNotSubstituteContext()
    {
        var first = PresentationObservation(task: "first");
        var later = PresentationObservation(task: "later", ordinal: 2) with { StoredAfter = 7.0 };
        var snapshot = PresentationSnapshot(first, later);
        var selected = LadderInstructionPresentation.PresentSnapshot("instruction", "numeric-0", snapshot, LadderInstructionPresentation.ContextKey(first));
        Equal("3", selected.Rows.Single(r => r.Label == "Stored after").Value);
        Equal(3, selected.Contexts.Count);
        Equal(ObservationUnavailableReason.ContextNotCaptured,
            LadderInstructionPresentation.PresentSnapshot("instruction", "numeric-0", snapshot, "missing-instance").UnavailableReason);
    }
    private static void PresenterSharedCounterUsesInstructionState()
    {
        var observation = PresentationObservation("ctu") with { DestinationIdentity = "counter", ConfiguredPreset = 3L,
            StoredAfter = new LadderCounterState(3, 1, true, false), WriteDisposition = InstructionWriteDisposition.StateUpdatedChanged };
        var view = LadderInstructionPresentation.PresentSnapshot("instruction", "ctu", PresentationSnapshot(observation));
        Equal("1", view.Rows.Single(r => r.Label == "Current count").Value);
        Equal("3", view.Rows.Single(r => r.Label == "Configured preset").Value);
        True(view.Rows.Single(r => r.Label == "Final snapshot").Value.StartsWith("CV 9; PV 9"));
    }
    private static void PresenterLifecycleIdentityAndAbort()
    {
        var snapshot = PresentationSnapshot(PresentationObservation());
        var origin = new ObservationOrigin(Guid.NewGuid(), Guid.NewGuid(), 1, 1);
        var publication = new ControllerPublication(origin.SessionId, "hash", origin.LoadId, 1, 1, 1, 4,
            ControllerPublicationKind.Stop, ControllerCompletion.NoExecution, ControllerPhaseCompletion.None,
            origin, snapshot.Observations!.WithOrigin(origin), snapshot);
        var view = LadderInstructionPresentation.Present("instruction", "numeric-0", publication);
        False(view.CurrentExecution); True(view.Status.Contains("LAST EXECUTION"));
        Equal(ObservationUnavailableReason.CaptureFault, LadderInstructionPresentation.Present("instruction", "numeric-0", publication with { LoadId = Guid.NewGuid() }).UnavailableReason);
        Equal(ObservationUnavailableReason.DraftMismatch, LadderInstructionPresentation.Present("instruction", "numeric-0", publication, draftMatches: false).UnavailableReason);
        var partial = publication with { Observations = publication.Observations! with { IsPartial = true }, Kind = ControllerPublicationKind.ScanAborted, Completion = ControllerCompletion.Aborted };
        True(LadderInstructionPresentation.Present("instruction", "numeric-0", partial).Partial);
        Equal(ObservationUnavailableReason.PartialScanNotReached, LadderInstructionPresentation.Present("unreached", "coil", partial).UnavailableReason);
    }
    private static void PresenterTypedEmptyFalseZeroAndTruncation()
    {
        Equal("\"\"", LadderInstructionPresentation.Value("")); Equal("FALSE", LadderInstructionPresentation.Value(false)); Equal("0", LadderInstructionPresentation.Value(0));
        var longOperand = new InstructionOperand("A", "long", PlcVariableType.String, new string('x', 4097));
        var observation = new InstructionObservation("instruction", "numeric-0", "b", "t", 1, 1, [], [longOperand], true, true);
        var view = LadderInstructionPresentation.PresentSnapshot("instruction", "numeric-0", PresentationSnapshot(observation));
        True(view.Rows.Single(r => r.Label == "Consumed A").Value.Contains("1 code units omitted; full consumed text unavailable"));
    }
    private static void PresenterParameterizedActualTransfers()
    {
        var runtime=Runtime(FbFixture());runtime.Run();var snapshot=runtime.Scan(Inputs());
        var view=LadderInstructionPresentation.PresentSnapshot("typed-call","call",snapshot);
        True(view.Rows.Any(r=>r.Label=="Consumed CopyOut:arg" && r.Identity=="result" && r.Value.StartsWith("1 (REAL)",StringComparison.Ordinal)));
        Equal("Completed",view.Rows.Single(r=>r.Label=="Control").Value);
        Equal("one",view.Rows.Single(r=>r.Label=="Called instance").Value);
        True(view.Rows.Any(r=>r.Label=="Called frame"));
    }
    private static void PresenterRealDisabledAndFaultEffects()
    {
        var program=ObservationProgram(
            new("tn","False timer",ObservationEnable("tc"),Timer:new("timer-op","timer",TimeSpan.FromMilliseconds(100))),
            new("cn","False counter",ObservationEnable("cc"),Counter:new("count-op","counter",3)),
            new("mn","Disabled move",ObservationEnable("mc"),NumericOperation:new("move-op",LadderNumericOperationKind.Move,"value","0","target")),
            new("fn","Bad format",new("fc",LadderNodeKind.Contact,"enable",true),NumericOperation:new("format-op",LadderNumericOperationKind.FormatText,"value","template","text")))
            with { Variables=ObservationProgram().Variables.Append(new PlcVariable("template",PlcVariableType.String,PlcVariableRole.Memory,"invalid")).ToArray() };
        var runtime=Runtime(program);runtime.Run();var snapshot=runtime.Scan(new Dictionary<string,bool>());
        foreach(var id in new[]{"timer-op","count-op"})
        {
            var observation=snapshot.Observations!.GlobalLast[id];
            var view=LadderInstructionPresentation.PresentSnapshot(id,observation.CatalogKey,snapshot);
            True(view.Rows.Single(r=>r.Label=="Effect").Value.StartsWith("State evaluated",StringComparison.Ordinal));
            Equal("FALSE",view.Rows.Single(r=>r.Label=="Network enable").Value);
        }
        var move=LadderInstructionPresentation.PresentSnapshot("move-op","numeric-0",snapshot);
        Equal("Skipped — enable FALSE; no write",move.Rows.Single(r=>r.Label=="Effect").Value);
        True(move.Rows.Single(r=>r.Label=="Consumed A").Value.Contains("action disabled",StringComparison.Ordinal));
        var format=LadderInstructionPresentation.PresentSnapshot("format-op","numeric-24",snapshot);
        Equal("Failed — destination cleared",format.Rows.Single(r=>r.Label=="Effect").Value);
        Equal("\"\"",format.Rows.Single(r=>r.Label=="Stored after").Value);
        True(format.Rows.Any(r=>r.Label.StartsWith("Fault ",StringComparison.Ordinal)));
    }
    private static void PresenterRealRuntimeCatalogCoverage()
    {
        var covered=new HashSet<string>();
        void Capture(LadderProgram program)
        {
            var runtime=Runtime(program);runtime.Run();
            var snapshot=runtime.Scan(new Dictionary<string,bool>{["enable"]=true});
                        foreach(var observation in snapshot.Observations!.GlobalLast.Values)
            {
                covered.Add(observation.CatalogKey);
                var view=LadderInstructionPresentation.PresentSnapshot(observation.InstructionId,observation.CatalogKey,snapshot);
                Equal(ObservationUnavailableReason.None,view.UnavailableReason);
                Equal(LadderInstructionPresentation.DispositionText(observation.WriteDisposition),view.Rows.Single(r=>r.Label=="Effect").Value);
                foreach(var operand in observation.Operands.Where(o=>o.Reason==ObservationUnavailableReason.None))
                {
                    var role = observation.CatalogKey switch
                    {
                        "ctu" or "rto" when operand.Role == "A" => "R",
                        "ctd" when operand.Role == "A" => "LD",
                        "numeric-18" or "numeric-19" => operand.Role switch { "A" => "MIN", "B" => "VALUE", "C" => "MAX", _ => operand.Role },
                        _ => operand.Role,
                    };
                    True(view.Rows.Any(r=>r.Label=="Consumed "+role && r.Identity==operand.Identity && r.Value.StartsWith(LadderInstructionPresentation.Value(operand.Value),StringComparison.Ordinal)));
                }
                if(observation.CatalogKey.StartsWith("numeric-",StringComparison.Ordinal))
                {
                    var kind=(LadderNumericOperationKind)int.Parse(observation.CatalogKey[8..],System.Globalization.CultureInfo.InvariantCulture);
                    var actual=view.Rows.Count(r=>r.Label.StartsWith("Consumed ",StringComparison.Ordinal));
                    Equal(1+(LadderNumericOperationRules.RequiresSourceB(kind)?1:0)+(LadderNumericOperationRules.RequiresSourceC(kind)?1:0),actual);
                }
                if(observation.StoredAfter is LadderTimerState timer)Equal(LadderInstructionPresentation.Value(timer.Accumulated),view.Rows.Single(r=>r.Label=="Elapsed").Value);
                if(observation.StoredAfter is LadderCounterState counter)Equal(counter.Accumulated.ToString(System.Globalization.CultureInfo.InvariantCulture),view.Rows.Single(r=>r.Label=="Current count").Value);
            }
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
}


