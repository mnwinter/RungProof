using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterParameterizedCallContractRegressionTests()
    {
        Test("FbTimersProcessTrueFalseTrueWithoutSecondQuantum", FbTimersProcessTrueFalseTrueWithoutSecondQuantum);
        Test("AllFbTimerKindsResetWithoutRegrantingQuantum", AllFbTimerKindsResetWithoutRegrantingQuantum);
        Test("AllFbTimerKindsShareTaskQuantumAndSkipPeriodicTime", AllFbTimerKindsShareTaskQuantumAndSkipPeriodicTime);
        Test("FcOutputAndTempInitialValuesResetForEachInvocation", FcOutputAndTempInitialValuesResetForEachInvocation);
        Test("AbortedFbRetainsStaticAndImmediateInOutWithoutCopyOut", AbortedFbRetainsStaticAndImmediateInOutWithoutCopyOut);
    }

    private static LadderProgram ContractTimerProgram(LadderTimerKind kind, bool[] signals, bool[]? resets = null)
    {
        var baseProgram = FbTimerFixture(kind);
        var fb = baseProgram.Blocks![1];
        fb = fb with { Interface = fb.Interface!.Concat(new[] {
            new LadderInterfaceParameter("Signal", PlcVariableType.Bool, LadderInterfaceSection.Input, false, Id: "signal"),
            new LadderInterfaceParameter("Reset", PlcVariableType.Bool, LadderInterfaceSection.Input, false, Id: "reset") }).ToArray(),
            Networks = [new("contract-reset-net", "Explicit reset", new("contract-reset-contact", LadderNodeKind.Contact, "local:reset"), TimerReset: new("contract-reset", "local:clock")),
                fb.Networks[0] with { Logic = new("timer-contact", LadderNodeKind.Contact, "local:signal") }, fb.Networks[1]] };
        var main = baseProgram.Blocks[0] with { Networks = signals.Select((signal, i) => new LadderNetwork("contract-call-net-" + i,
            "Invocation " + i, new("contract-wire-" + i, LadderNodeKind.Series, Children: []),
            Call: new("contract-call-" + i, fb.Id, LadderSemanticMode.Parameterized,
                [new("arg", LadderActualKind.Symbol, "global:result-id"),
                 new("signal", LadderActualKind.Literal, Literal: signal, LiteralType: PlcVariableType.Bool),
                 new("reset", LadderActualKind.Literal, Literal: resets?[i] ?? false, LiteralType: PlcVariableType.Bool)], "one"))).ToArray() };
        return baseProgram with { Blocks = [main, fb] };
    }

    private static void FbTimersProcessTrueFalseTrueWithoutSecondQuantum()
    {
        // Explicit offline policy: transitions still occur, but only the first Advance gets 10 ms.
        foreach (var kind in Enum.GetValues<LadderTimerKind>())
        {
            var runtime = Runtime(ContractTimerProgram(kind, [true, false, true])); runtime.Run();
            var scan = runtime.Scan(Inputs());
            var states = scan.Observations!.Trace.Where(o => o.InstructionId == "timer-action")
                .Select(o => (LadderTimerState)o.StoredAfter!).ToArray();
            Equal(3, states.Length);
            double[] expected = kind switch {
                LadderTimerKind.OnDelay => [10, 0, 0],
                LadderTimerKind.OffDelay => [0, 10, 0],
                _ => [10, 10, 10] };
            for (var i = 0; i < states.Length; i++) Equal(expected[i], states[i].Accumulated.TotalMilliseconds);
            True(states[0].Input); False(states[1].Input); True(states[2].Input);
            Equal(kind is LadderTimerKind.OffDelay or LadderTimerKind.Pulse, states[2].Done);
            Equal(expected[2], scan.NumericVariables["result"]);
        }
    }

    private static void AllFbTimerKindsResetWithoutRegrantingQuantum()
    {
        foreach (var kind in Enum.GetValues<LadderTimerKind>())
        {
            var runtime = Runtime(ContractTimerProgram(kind, [true, true], [false, true])); runtime.Run();
            var scan = runtime.Scan(Inputs());
            var states = scan.Observations!.Trace.Where(o => o.InstructionId == "timer-action")
                .Select(o => (LadderTimerState)o.StoredAfter!).ToArray();
            Equal(2, states.Length);
            Equal(kind == LadderTimerKind.OffDelay ? 0.0 : 10.0, states[0].Accumulated.TotalMilliseconds);
            Equal(0.0, states[1].Accumulated.TotalMilliseconds);
            Equal(0.0, scan.NumericVariables["result"]);
            True(scan.Observations.Trace.Any(o => o.InstructionId == "contract-reset" && o.NetworkEnable == true));
        }
    }

    private static void AllFbTimerKindsShareTaskQuantumAndSkipPeriodicTime()
    {
        foreach (var kind in Enum.GetValues<LadderTimerKind>())
        {
            var p = FbTimerFixture(kind);
            p = p with { Blocks = [p.Blocks![0] with { Networks = [p.Blocks[0].Networks[0]] }, p.Blocks[1]] };
            var twoTasks = p with { Tasks = [
                new("first", "First", LadderTaskKind.Continuous, p.ScanPeriod, 1, "main"),
                new("second", "Second", LadderTaskKind.Continuous, p.ScanPeriod, 2, "main")] };
            var runtime = Runtime(twoTasks); runtime.Run();
            var scan = runtime.Scan(Inputs());
            if (kind == LadderTimerKind.OffDelay) scan = runtime.Scan(new Dictionary<string, bool> { ["timerSignal"] = false });
            Equal(10.0, scan.NumericVariables["result"]);
            var observations = scan.Observations!.Trace.Where(o => o.InstructionId == "timer-action").ToArray();
            Equal(2, observations.Length);
            True(observations.Select(o => o.TaskId).Distinct().Count() == 2);
            True(observations.All(o => ((LadderTimerState)o.StoredAfter!).Accumulated.TotalMilliseconds == 10));

            var periodic = Runtime(p with { Tasks = [new("periodic", "Periodic", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(30), 1, "main")] });
            periodic.Run();
            for (var i = 1; i <= 9; i++)
            {
                var current = periodic.Scan(new Dictionary<string, bool> { ["timerSignal"] = i <= 3 });
                if (i % 3 != 0) False(current.Observations!.GlobalLast.ContainsKey("timer-action"));
                var expected = i < 3 ? 0.0 : kind switch {
                    LadderTimerKind.OnDelay => i < 6 ? 10.0 : 0.0,
                    LadderTimerKind.OffDelay => i < 6 ? 0.0 : i < 9 ? 10.0 : 20.0,
                    LadderTimerKind.Pulse => i < 6 ? 10.0 : i < 9 ? 20.0 : 30.0,
                    _ => 10.0 };
                Equal(expected, current.NumericVariables["result"]);
            }
        }
    }

    private static void FcOutputAndTempInitialValuesResetForEachInvocation()
    {
        var p = TypedCallFixture();
        var fc = p.Blocks![1] with { Interface = [
            new("Input", PlcVariableType.Real, LadderInterfaceSection.Input, 0.0, Id: "in"),
            new("Output", PlcVariableType.Real, LadderInterfaceSection.Output, 2.0, Id: "arg"),
            new("Temp", PlcVariableType.Real, LadderInterfaceSection.Temp, 4.0, Id: "temp")],
            Networks = [TypedMove("mutate-actual-after-sample", "99", "global:result-id"),
                new("sum-initial-net", "Use both declared initial values", new("sum-initial-wire", LadderNodeKind.Series, Children: []),
                    NumericOperation: new("sum-initial", LadderNumericOperationKind.Add, "local:arg", "local:temp", "local:arg")),
                new("add-sample-net", "Use sampled input despite global overwrite", new("add-sample-wire", LadderNodeKind.Series, Children: []),
                    NumericOperation: new("add-sample", LadderNumericOperationKind.Add, "local:arg", "local:in", "local:arg")),
                TypedMove("dirty-temp", "100", "local:temp")] };
        var first = p.Blocks[0].Networks[0];
        var call = first.Call! with { Bindings = [new("in", LadderActualKind.Symbol, "global:result-id"), new("arg", LadderActualKind.Symbol, "global:result-id")] };
        p = p with { Variables = p.Variables.Select(v => v.Id == "result-id" ? v with { InitialValue = 3.0 } : v).ToArray(),
            Blocks = [p.Blocks[0] with { Networks = [first with { Call = call }, first with {
                Id = "second-call-net", Logic = new("second-call-wire", LadderNodeKind.Series, Children: []), Call = call with { Id = "second-call" } }] }, fc] };
        var runtime = Runtime(p); runtime.Run();
        var firstScan = runtime.Scan(Inputs()); Equal(15.0, firstScan.NumericVariables["result"]);
        var capturedInputs = firstScan.CallInvocations.SelectMany(c => c.Transfers).Where(t => t.Phase == "CopyIn").Select(t => Convert.ToDouble(t.Value)).ToArray();
        True(capturedInputs.SequenceEqual(new[] { 3.0, 9.0 }));
        Equal(27.0, runtime.Scan(Inputs()).NumericVariables["result"]);
    }

    private static void AbortedFbRetainsStaticAndImmediateInOutWithoutCopyOut()
    {
        var p = FbFixture();
        var fb = p.Blocks![1] with { Interface = p.Blocks[1].Interface!.Append(
            new LadderInterfaceParameter("Shared", PlcVariableType.Real, LadderInterfaceSection.InOut, 0.0, Id: "shared")).ToArray(),
            Networks = [
                new("inc-output-net", "Output local", new("inc-output-wire", LadderNodeKind.Series, Children: []), NumericOperation: new("inc-output", LadderNumericOperationKind.Add, "local:arg", "1", "local:arg")),
                new("inc-static-net", "Static", new("inc-static-wire", LadderNodeKind.Series, Children: []), NumericOperation: new("inc-static", LadderNumericOperationKind.Add, "local:count", "2", "local:count")),
                new("inc-shared-net", "Immediate alias", new("inc-shared-wire", LadderNodeKind.Series, Children: []), NumericOperation: new("inc-shared", LadderNumericOperationKind.Add, "local:shared", "3", "local:shared")),
                new("loop-label-net", "Loop", new("loop-label-wire", LadderNodeKind.Series, Children: []), LabelInstruction: new("loop-label", "loop")),
                new("loop-jump-net", "Abort request", new("abort-contact", LadderNodeKind.Contact, "global:abort-id"), Jump: new("loop-jump", "loop"))] };
        var first = p.Blocks[0].Networks[0];
        p = p with { Variables = p.Variables.Concat(new PlcVariable[] {
                new("abort", PlcVariableType.Bool, PlcVariableRole.Input, true, Id: "abort-id"),
                new("shared", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "shared-id") }).ToArray(),
            Blocks = [p.Blocks[0] with { Networks = [first with { Call = first.Call! with { Bindings = [
                new("arg", LadderActualKind.Symbol, "global:result-id"), new("shared", LadderActualKind.Symbol, "global:shared-id")] } }] }, fb] };
        var runtime = Runtime(p); runtime.Run(); var aborted = runtime.Scan(Inputs());
        Equal(ControllerCompletion.Aborted, aborted.Completion); Equal(0.0, aborted.NumericVariables["result"]);
        Equal(1.0, aborted.NumericVariables["@instance/one/arg"]); Equal(2.0, aborted.NumericVariables["@instance/one/count"]);
        Equal(3.0, aborted.NumericVariables["shared"]);
        False(aborted.CallInvocations.Single().Transfers.Any(t => t.Phase == "CopyOut"));
        True(aborted.CallInvocations.Single().Transfers.Any(t => t.Phase == "CopyOutSuppressed"));
        Equal(3.0, Convert.ToDouble(aborted.CallInvocations.Single().Transfers.Single(t => t.Phase == "AliasFinal").Value));
        runtime.Run(); var next = runtime.Scan(new Dictionary<string, bool> { ["abort"] = false });
        Equal(ControllerCompletion.Completed, next.Completion); Equal(2.0, next.NumericVariables["result"]);
        Equal(4.0, next.NumericVariables["@instance/one/count"]); Equal(6.0, next.NumericVariables["shared"]);
        Equal(6.0, Convert.ToDouble(next.CallInvocations.Single().Transfers.Single(t => t.Phase == "AliasFinal").Value));
    }
}
