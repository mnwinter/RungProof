using System.Diagnostics;
using System.Text.Json;
using RungProof.Next.VirtualController;

// Diagnostic lower bound: retained event objects only, excluding all operands,
// paths, collector dictionaries, snapshots and history. Not a workload pass.
if (args.Contains("--record-size"))
{
    var retained = new InstructionObservation[10000];
    for (var i = 0; i < 100; i++) retained[i] = new("id", "no", "block", "task", i, 1, [], [], true, true);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < retained.Length; i++) retained[i] = new("id", "no", "block", "task", i, 1, [], [], true, true);
    var bytesPerRecord = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)retained.Length;
    GC.KeepAlive(retained);
    Console.WriteLine(JsonSerializer.Serialize(new { diagnostic = "empty-immutable-record-lower-bound", bytesPerRecord,
        w1UniqueEvents = 1750, w1RecordBytesOnly = 1750 * bytesPerRecord, proposedAddedAllocationBudget = 256 * 1024 }));
    return;
}

// Preliminary W1 execution probe. Full qualification also needs the session,
// history, renderer, W2-W4, three runs and the contract's 1000/10000 sample counts.
var samples = args.Length > 0 ? int.Parse(args[0]) : 300;
var warmup = args.Length > 1 ? int.Parse(args[1]) : 100;
var variables = Enumerable.Range(0, 900).Select(i => new PlcVariable("b"+i, PlcVariableType.Bool, PlcVariableRole.Memory, true))
    .Concat(Enumerable.Range(0,100).Select(i=>new PlcVariable("n"+i,PlcVariableType.Real,PlcVariableRole.Memory,1.0)))
    .Concat(Enumerable.Range(0,20).Select(i=>new PlcVariable("t"+i,PlcVariableType.Timer,PlcVariableRole.Memory,0)))
    .Concat(Enumerable.Range(0,20).Select(i=>new PlcVariable("c"+i,PlcVariableType.Counter,PlcVariableRole.Memory,0))).ToArray();
var networks = Enumerable.Range(0,250).Select(i=>
{
    var logic = new LadderNode("root"+i,LadderNodeKind.Series,Children:Enumerable.Range(0,4).Select(j=>new LadderNode($"node{i}-{j}",LadderNodeKind.Contact,"b"+((i*4+j)%900))).ToArray());
    return new LadderNetwork("net"+i,"W1 "+i,logic,
        Coil:i>=90?new("coil"+i,"b"+i):null,
        Timer:i<20?new("timer"+i,"t"+i,TimeSpan.FromMilliseconds(100)):null,
        Counter:i>=20&&i<40?new("counter"+i,"c"+(i-20),3):null,
        NumericOperation:i>=40&&i<90?new("math"+i,LadderNumericOperationKind.Add,"n"+(i-40),"1","n"+(i-40)):null);
}).ToArray();
var program = new LadderProgram(2,"w1","W1 preliminary","LD",TimeSpan.FromMilliseconds(10),variables,networks,
    [new("main","Main",networks)],"main",
    [new("cyclic","Cyclic",LadderTaskKind.Continuous,TimeSpan.Zero,1,"main"),new("periodic","100ms",LadderTaskKind.Periodic,TimeSpan.FromMilliseconds(100),2,"main")]);
var workload = args.Length > 2 ? args[2].ToUpperInvariant() : "W1";
var useSession = args.Length > 3 && args[3].Equals("session", StringComparison.OrdinalIgnoreCase);
program = workload switch { "W1" => program, "W2" => Workloads.Large(), "W3" => Workloads.Repeated(false), "W3-WATCHDOG" => Workloads.Repeated(true), "W4" => Workloads.Text(), _ => throw new ArgumentException("Use W1, W2, W3, W3-watchdog or W4.") };
if (workload == "W4") Workloads.TextRobustness();
var compiled = LadderCompiler.Compile(program);
if(!compiled.IsValid) throw new InvalidOperationException(string.Join(";",compiled.Issues));
var inputs = new Dictionary<string,bool>();
string? baselineState = null;
foreach(var enabled in new[]{false,true})
{
    var runtime = new VirtualControllerRuntime(compiled.Program!){MonitoringEnabled=enabled};
    VirtualControllerSession? session = useSession ? new(runtime) : null;
    var numbers = new Dictionary<string,double>();
    if (session is not null) { session.Run(); session.Pause(); } else runtime.Run();
    void Step()
    {
        // A watchdog abort stops the controller. Restart each independent trial.
        if (runtime.State != VirtualControllerState.Running)
        {
            if (session is not null) { session.Run(); session.Pause(); } else runtime.Run();
        }
        if (session is not null) session.StepOneAcceptedScan(() => inputs, () => numbers, _ => {}, _ => {}, _ => {});
        else runtime.Scan(inputs);
    }
    for(var i=0;i<warmup;i++) Step();
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    var heapBefore = GC.GetTotalMemory(true);
    var collections = Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();
    var pauseBefore = GC.GetTotalPauseDuration();
    var bytes=GC.GetAllocatedBytesForCurrentThread(); var times=new double[samples];
    for(var i=0;i<samples;i++) { var start=Stopwatch.GetTimestamp(); Step(); times[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
    var allocation=(GC.GetAllocatedBytesForCurrentThread()-bytes)/(double)samples;
    var pause = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
    var gcCounts = Enumerable.Range(0,3).Select(i => GC.CollectionCount(i) - collections[i]).ToArray();
    var retainedBytesAfterGc = GC.GetTotalMemory(true);
    var snapshot = runtime.Snapshot;
    if ((args.Contains("--allocation-phases") || args.Contains("--layout")) && snapshot.Observations is {} captured && FrozenObservationTable.From(captured) is {} backing)
    {
        int Length(string name) => ((Array)typeof(FrozenObservationTable).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(backing)!).Length;
        Console.WriteLine(JsonSerializer.Serialize(new { backingLayout = true, rows = Length("_rows"), rowBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<CompactObservationRow>(), booleanOperands = Length("_booleanOperands"), operands = Length("_operands"), operandBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<ObservationOperandValue>(), rich = Length("_rich"), richBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<RichObservationPayload>(), trace = Length("_trace"), firstContext = Length("_firstContext"), nextContext = Length("_nextContext"), globals = captured.GlobalLast.Count, contexts = captured.Contexts.Count, counts = captured.Counts.Count }));
    }
    var state = JsonSerializer.Serialize(new { snapshot.ScanNumber, snapshot.SimulatedTime, snapshot.State, snapshot.Variables, snapshot.NumericVariables,
        snapshot.TextVariables, snapshot.Outputs, snapshot.NumericOutputs, snapshot.TextOutputs, snapshot.Timers, snapshot.Counters, snapshot.Tasks,
        snapshot.Diagnostics, snapshot.Completion, snapshot.ExecutedNetworkCount });
    if (baselineState is null) baselineState = state;
    else if (state != baselineState) throw new InvalidOperationException("Monitoring changed final controller state.");
    if (workload == "W3" && snapshot.ExecutedNetworkCount != 9999) throw new InvalidOperationException("W3 must complete exactly 9999 networks.");
    if (workload == "W3-WATCHDOG" && (snapshot.ExecutedNetworkCount != 10000 || snapshot.Completion != ControllerCompletion.Aborted)) throw new InvalidOperationException("W3 watchdog must reject visit10001.");
    if (workload == "W2" && enabled && Enumerable.Range(0, 100).Sum(i => snapshot.Observations!.Counts.GetValueOrDefault("call" + i)) != 500) throw new InvalidOperationException("W2 must execute 500 call invocations per main scan.");
    Array.Sort(times);
    Console.WriteLine(JsonSerializer.Serialize(new { workload, scope=useSession?"session-history":"runtime",monitoring=enabled,samples,warmup,
        p50Ms=times[samples/2],p95Ms=times[(int)(samples*.95)],p99Ms=times[(int)(samples*.99)],bytesPerScan=allocation,
        gcCollections=gcCounts,gcTotalPauseMs=pause,heapBefore,retainedBytesAfterGc,historyCount=session?.History.Count??0,
        historyRetainedCharge=session?.HistoryRetainedBytes??0,historyDropped=session?.HistoryDroppedCount??0,
        process=Environment.ProcessId,processors=Environment.ProcessorCount,framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        finalStateMatchesBaseline=true,completion=snapshot.Completion.ToString(),snapshot.ExecutedNetworkCount,callInvocations=snapshot.CallInvocations.Count,
        observations=snapshot.Observations?.GlobalLast.Count??0,trace=snapshot.Observations?.Trace.Count??0,traceDropped=snapshot.Observations?.TraceDropped??0,
        contextSlots=snapshot.Observations?.Contexts.Count??0,capacityExceeded=snapshot.Observations?.CapacityExceeded??false}));
    GC.KeepAlive(runtime); GC.KeepAlive(session);
}
