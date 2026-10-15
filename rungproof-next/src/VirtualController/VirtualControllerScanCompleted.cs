using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace RungProof.Next.VirtualController;
/// <summary>Private primitive copies; no nested aggregates or live dictionaries.</summary>
public sealed record VirtualControllerSignalImage(IReadOnlyDictionary<string, bool> Bools,
    IReadOnlyDictionary<string, double> Numbers, IReadOnlyDictionary<string, string> Text)
{
    public IReadOnlyDictionary<string, bool> Bools { get; } = Copy(Bools);
    public IReadOnlyDictionary<string, double> Numbers { get; } = Copy(Numbers);
    public IReadOnlyDictionary<string, string> Text { get; } = Copy(Text);
    private static IReadOnlyDictionary<string,T> Copy<T>(IReadOnlyDictionary<string,T> source) =>
        new ReadOnlyDictionary<string,T>(source.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));
}
/// <summary>Raised after an actual scan's output commits, plant step and pulse cleanup.</summary>
public sealed record VirtualControllerScanCompleted(long ScanNumber, TimeSpan SimulatedTime,
    VirtualControllerSignalImage SampledInputs, VirtualControllerSignalImage EffectiveInputs,
    VirtualControllerSignalImage Outputs, IReadOnlyList<string> Diagnostics)
{
    public IReadOnlyList<string> Diagnostics { get; } = Array.AsReadOnly(Diagnostics.ToArray());
    public ControllerCompletion Completion { get; init; } = ControllerCompletion.Completed;
    public ControllerPublication? Publication { get; init; }
}
