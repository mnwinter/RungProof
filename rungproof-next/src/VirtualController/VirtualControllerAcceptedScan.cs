using System.Linq;
using System.Collections.Generic;
namespace RungProof.Next.VirtualController;
/// <summary>Accepted pre-plant images and separately sampled post-plant geometry feedback.</summary>
public sealed record VirtualControllerAcceptedScan(VirtualControllerScanCompleted CompletedScan,VirtualControllerSignalImage PostPlantInputs)
{
    /// <summary>Conservative retained payload charge, including envelope/snapshot images, not just optional trace.</summary>
    public long EstimatedPayloadBytes => 1024L+(CompletedScan.Publication?.Observations?.EstimatedBytes??0)
        +ImageBytes(CompletedScan.SampledInputs)+ImageBytes(CompletedScan.EffectiveInputs)+ImageBytes(CompletedScan.Outputs)+ImageBytes(PostPlantInputs)
        +(CompletedScan.Publication is {} p?ImageBytes(p.ProgramEndValues)+ImageBytes(p.PublishedValues)+SnapshotBytes(p.Snapshot):0);
    private static long ImageBytes(VirtualControllerSignalImage? image)=>image is null?0:ScalarBytes(image.Bools,image.Numbers,image.Text);
    private static long ScalarBytes(IReadOnlyDictionary<string,bool> bools,IReadOnlyDictionary<string,double> numbers,IReadOnlyDictionary<string,string> text)
        => bools.Keys.Concat(numbers.Keys).Sum(k=>96L+k.Length*2L)+text.Sum(p=>96L+2L*(p.Key.Length+p.Value.Length));
    private static long SnapshotBytes(VirtualControllerSnapshot s)=>ScalarBytes(s.Variables,s.NumericVariables,s.TextVariables)
        +ScalarBytes(s.Outputs,s.NumericOutputs,s.TextOutputs)
        +(s.Timers.Count+s.Counters.Count+s.Tasks.Count+s.Forces.Count+s.Elements.Count)*256L
        +s.Diagnostics.Sum(d=>64L+d.Length*2L)
        +s.CallInvocations.Sum(CallBytes);
    // Charge each retained representation conservatively, even when strings are shared.
    // Invocation transfers remain available when instruction monitoring is disabled.
    private static long CallBytes(LadderCallInvocation call) => 256L
        +2L*(call.CallId.Length+call.BlockId.Length+call.TaskId.Length+call.InstanceId.Length+call.Failure.Length)
        +call.Transfers.Sum(t=>192L+2L*(t.FormalId.Length+t.StorageIdentity.Length+t.Phase.Length)
            +(t.Value is string text?64L+text.Length*2L:64L));
}
