using System;
using System.Collections.Generic;
using RungProof.Next.Scenes;
namespace RungProof.Next.Challenges;
/// <summary>Offline binding-keyed plant boundary. Tag translation belongs to SceneIoImageMapper.</summary>
public sealed class ConveyorChallengePlantAdapter : IChallengePlantAdapter
{
    private readonly ConveyorPlantModel _plant;
    private bool _run;
    public ConveyorChallengePlantAdapter(ConveyorPlantParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _plant = new(parameters.Length, parameters.Speed, parameters.ObjectLength, parameters.PhotoeyePosition, parameters.MinimumPhotoeyeOn);
    }
    public void Reset() { _run = false; _plant.Reset(); }
    public IReadOnlyDictionary<string,bool> SampleBoolInputs() => CaptureState().Feedback;
    public IReadOnlyDictionary<string,double> SampleNumericInputs() => ChallengeCopies.Map(new Dictionary<string,double>());
    public IReadOnlyDictionary<string,string> SampleTextInputs() => ChallengeCopies.Map(new Dictionary<string,string>());
    public void CommitBoolOutputs(IReadOnlyDictionary<string,bool> outputs) => _run = outputs.TryGetValue("conveyor_running", out var run) && run;
    public void CommitNumericOutputs(IReadOnlyDictionary<string,double> outputs) { }
    public void CommitTextOutputs(IReadOnlyDictionary<string,string> outputs) { }
    public void Advance(TimeSpan period) => _plant.Step(period.TotalSeconds, _run);
    public ChallengePlantState CaptureState() => new(
        new Dictionary<string,bool> { ["simulated_photoeye"] = _plant.PhotoeyeBlocked },
        new Dictionary<string,double> { ["object_position"] = _plant.LeadingEdge ?? 0 },
        new Dictionary<string,string> { ["component_state"] = _plant.State },
        _plant.ObjectPresent, _plant.Completed);
}
