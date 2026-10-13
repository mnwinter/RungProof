using System.Text.Json;
using RungProof.Next.VirtualController;

internal static class Workloads
{
    public static LadderProgram Large()
    {
        var vars = Enumerable.Range(0, 9900).Select(i => new PlcVariable("b" + i, PlcVariableType.Bool, PlcVariableRole.Memory, true))
            .Concat(Enumerable.Range(0, 100).Select(i => new PlcVariable("n" + i, PlcVariableType.Real, PlcVariableRole.Memory, 0.0))).ToArray();
        LadderNode Logic(string id, bool compare = false) => new(id, LadderNodeKind.Series, Children: Enumerable.Range(0, 5)
            .Select(j => compare && j == 0 ? new LadderNode(id + "-" + j, LadderNodeKind.Compare, "n99", CompareOperator: LadderCompareOperator.LessThan, RightOperand: "5")
                : new LadderNode(id + "-" + j, LadderNodeKind.Contact, "b" + (j % 9900))).ToArray());
        var main = new List<LadderNetwork> { new("reset", "Reset loop", Logic("reset-root"), NumericOperation: new("reset-op", LadderNumericOperationKind.Move, "0", "0", "n99")) };
        for (var i = 0; i < 1895; i++) main.Add(new("base" + i, "Large " + i, Logic("base-root" + i), Coil: new("base-coil" + i, "b" + i)));
        main.Add(new("label", "Loop", Logic("label-root"), LabelInstruction: new("label-op", "again")));
        for (var i = 0; i < 100; i++) main.Add(new("call-net" + i, "Call " + i, Logic("call-root" + i), Call: new("call" + i, "worker")));
        main.Add(new("increment", "Increment loop", Logic("increment-root"), NumericOperation: new("increment-op", LadderNumericOperationKind.Add, "n99", "1", "n99")));
        main.Add(new("jump", "Repeat five times", Logic("jump-root", true), Jump: new("jump-op", "again")));
        LadderNetwork[] worker = [new("worker-net", "Shared destination", Logic("worker-root"), NumericOperation: new("worker-op", LadderNumericOperationKind.Add, "n0", "1", "n0"))];
        return new(2, "w2", "W2", "LD", TimeSpan.FromMilliseconds(10), vars, main,
            [new("main", "Main", main), new("worker", "Worker", worker)], "main",
            [new("t10", "10ms", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(10), 1, "main"),
             new("t20", "20ms", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(20), 2, "worker"),
             new("t100", "100ms", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(100), 3, "worker"),
             new("t500", "500ms", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(500), 4, "worker")]);
    }
    public static LadderProgram Repeated(bool watchdog)
    {
        LadderNode On(string id) => new(id, LadderNodeKind.Contact, "enable");
        LadderNetwork[] main = [
            new("reset", "Reset", On("reset-c"), NumericOperation: new("reset-op", LadderNumericOperationKind.Move, "0", "0", "loop")),
            new("label", "Loop", On("label-c"), LabelInstruction: new("label-op", "again")),
            new("call", "Shared counter call", On("call-c"), Call: new("call-op", "worker")),
            new("inc", "Increment", On("inc-c"), NumericOperation: new("inc-op", LadderNumericOperationKind.Add, "loop", "1", "loop")),
            new("jump", "Repeat", watchdog ? On("jump-c") : new("jump-c", LadderNodeKind.Compare, "loop", CompareOperator: LadderCompareOperator.LessThan, RightOperand: "1999"), Jump: new("jump-op", "again")),
            new("timer", "Timer", On("timer-c"), Timer: new("timer-op", "timer", TimeSpan.FromMilliseconds(100))),
            new("load", "Shared counter load", On("load-c"), CounterLoad: new("load-op", "counter", 7)),
            new("output", "Output", On("output-c"), Coil: new("output-op", "output"))];
        LadderNetwork[] worker = [new("worker-net", "Counter", On("worker-c"), Counter: new("counter-op", "counter", 3))];
        return new(2, watchdog ? "w3-watchdog" : "w3", "W3", "LD", TimeSpan.FromMilliseconds(10),
            [new("enable", PlcVariableType.Bool, PlcVariableRole.Memory, true), new("output", PlcVariableType.Bool, PlcVariableRole.Output, false),
             new("loop", PlcVariableType.DInt, PlcVariableRole.Memory, 0), new("timer", PlcVariableType.Timer, PlcVariableRole.Memory, 0), new("counter", PlcVariableType.Counter, PlcVariableRole.Memory, 0)], main,
            [new("main", "Main", main), new("worker", "Worker", worker)], "main");
    }
    public static LadderProgram Text()
    {
        var vars = new List<PlcVariable> { new("enable", PlcVariableType.Bool, PlcVariableRole.Memory, true),
            new("empty", PlcVariableType.String, PlcVariableRole.Memory, ""), new("short", PlcVariableType.String, PlcVariableRole.Memory, new string('a', 64)),
            new("limit", PlcVariableType.String, PlcVariableRole.Memory, new string('b', 255)) };
        var networks = new List<LadderNetwork>();
        for (var i = 0; i < 100; i++)
        {
            vars.Add(new("text" + i, PlcVariableType.String, PlcVariableRole.Memory, ""));
            networks.Add(new("net" + i, "Text " + i, new("contact" + i, LadderNodeKind.Contact, "enable"),
                NumericOperation: new("format" + i, LadderNumericOperationKind.FormatText, "1", i % 3 == 0 ? "empty" : i % 3 == 1 ? "short" : "limit", "text" + i)));
        }
        return new(2, "w4", "W4 executable text limit", "LD", TimeSpan.FromMilliseconds(10), vars, networks);
    }
    public static void TextRobustness()
    {
        foreach (var length in new[] { 0, 64, 4096, 65536 })
        {
            var operand = new InstructionOperand("A", "fixture", PlcVariableType.String, new string('x', length));
            if (operand.TotalTextLength != length || ((string)operand.Value!).Length != Math.Min(length, 4096) || operand.TextTruncated != (length > 4096))
                throw new InvalidOperationException("Text capture robustness failed.");
        }
        var textProgram = Text();
        var oversized = textProgram with { Variables = textProgram.Variables.Append(new PlcVariable("huge", PlcVariableType.String, PlcVariableRole.Memory, new string('x', 4096))).ToArray() };
        if (LadderCompiler.Compile(oversized).IsValid) throw new InvalidOperationException("Oversized STRING must not compile.");
        var collector = new InstructionObservationCollector(["x"]);
        for (var i = 0; i <= 40000; i++) collector.Record(new("x", "no", "b", "task", i + 1, i + 1,
            [new("site" + i, "b", i + 1)], [], true, true));
        var capture = collector.Freeze(false);
        if (capture.Contexts.Count != 40000 || capture.Trace.Count != 10000 || capture.TraceDropped != 30001 || capture.Counts["x"] != 40001
            || capture.FindPath("x", "task", ["site40000"]).Reason != ObservationUnavailableReason.ContextNotCaptured)
            throw new InvalidOperationException("Context/trace overflow lost authoritative count or substituted context.");
        Console.WriteLine(JsonSerializer.Serialize(new { diagnostic = "W4-robustness", lengths = new[] { 0, 64, 4096, 65536 }, contexts = capture.Contexts.Count, trace = capture.Trace.Count, capture.TraceDropped, count = capture.Counts["x"], oversizedLanguageRejected = true }));
    }
}
