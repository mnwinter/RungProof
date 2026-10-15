using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterParameterizedCallAcceptanceTests()
    {
        Test("TypedScalarLiteralAndOutputBoundaries", () =>
        {
            foreach (var sample in new (PlcVariableType Type, object Value, string[]? Domain)[]
            {
                (PlcVariableType.Bool, false, null), (PlcVariableType.Int, -32768, null),
                (PlcVariableType.DInt, 2147483647, null), (PlcVariableType.Real, -1.25, null),
                (PlcVariableType.String, "", null), (PlcVariableType.String, new string('x', 255), null),
                (PlcVariableType.Enum, "Idle", new[] { "Idle", "Run" })
            })
            {
                var p = TypedCallFixture();
                var formal = new LadderInterfaceParameter("Sample", sample.Type, LadderInterfaceSection.Input,
                    sample.Value, Id: "input", EnumMembers: sample.Domain);
                var output = formal with { Name = "Output", Id = "output", Section = LadderInterfaceSection.Output };
                p = p with
                {
                    Variables = [new("result", sample.Type, PlcVariableRole.Memory, sample.Value, Id: "result-id", EnumMembers: sample.Domain), p.Variables[1]],
                    Blocks = [p.Blocks![0] with { Networks = [p.Blocks[0].Networks[0] with { Call = p.Blocks[0].Networks[0].Call! with
                    { Bindings = [new("input", LadderActualKind.Literal, Literal: sample.Value, LiteralType: sample.Type, EnumMembers: sample.Domain),
                        new("output", LadderActualKind.Symbol, "global:result-id")] } }] },
                    p.Blocks[1] with { Interface = [formal, output], Networks = [] }]
                };
                var runtime = Runtime(p); runtime.Run(); var scan = runtime.Scan(Inputs());
                Equal(ControllerCompletion.Completed, scan.Completion);
                var transfer = scan.CallInvocations.Single().Transfers.Single(t => t.FormalId == "input" && t.Phase == "CopyIn");
                Equal(sample.Type, transfer.Type);
                True(Equals(sample.Value, transfer.Value) || sample.Value is int && Convert.ToDouble(sample.Value) == Convert.ToDouble(transfer.Value));
                var copy = scan.CallInvocations.Single().Transfers.Single(t => t.Phase == "CopyOut");
                Equal(sample.Type, copy.Type);
                var wrong = p.Blocks[0].Networks[0].Call! with { Bindings = [new("input", LadderActualKind.Literal, Literal: 1.0, LiteralType: sample.Type == PlcVariableType.Real ? PlcVariableType.Int : PlcVariableType.Real), new("output", LadderActualKind.Symbol, "global:result-id")] };
                False(LadderCompiler.Compile(ReplaceTypedCall(p, wrong)).IsValid);
                if (sample.Type == PlcVariableType.Enum)
                    False(LadderCompiler.Compile(ReplaceTypedCall(p, p.Blocks[0].Networks[0].Call! with
                    { Bindings = [new("input", LadderActualKind.Literal, Literal: "Idle", LiteralType: PlcVariableType.Enum, EnumMembers: ["Idle", "Fault"]), new("output", LadderActualKind.Symbol, "global:result-id")] })).IsValid);
            }
        });
        Test("ParameterizedMonitoringDoesNotChangeInstanceExecution", () =>
        {
            var p = FbFixture(); var off = new VirtualControllerRuntime(LadderCompiler.Compile(p).Program!) { MonitoringEnabled = false };
            var on = new VirtualControllerRuntime(LadderCompiler.Compile(p).Program!) { MonitoringEnabled = true };
            off.Run(); on.Run();
            for (var i = 0; i < 12; i++)
            {
                var input = new Dictionary<string, bool> { ["enable"] = i % 3 != 0 };
                var a = off.Scan(input); var b = on.Scan(input);
                Equal(a.Completion, b.Completion); Equal(a.NumericVariables["result"], b.NumericVariables["result"]);
                Equal(a.NumericVariables["other"], b.NumericVariables["other"]);
                True(a.CallInvocations.Select(x => (x.CallId, x.InstanceId, x.Completion)).SequenceEqual(b.CallInvocations.Select(x => (x.CallId, x.InstanceId, x.Completion))));
            }
        });
    }
}
