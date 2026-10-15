using System;
namespace RungProof.Next.Challenges;
/// <summary>Driver timing only. No correctness predicates or controller output writes.</summary>
public sealed class ConveyorChallengeProtocol
{
    private readonly ChallengeCaseKind _kind;
    private long? _beamStep;
    public ConveyorChallengeProtocol(ChallengeCaseKind kind) => _kind = kind;
    public bool Start(long scan) => scan == 1 && _kind != ChallengeCaseKind.ColdStart
        || _kind == ChallengeCaseKind.BlockedStart && _beamStep.HasValue && scan == _beamStep + 27;
    public bool Stop(long scan) => _kind == ChallengeCaseKind.ProcessStop && scan == 11
        || _kind == ChallengeCaseKind.SimultaneousStartStop && scan == 1;
    public void Accept(long scan, ChallengePlantState state)
    {
        if (!_beamStep.HasValue && state.Feedback.TryGetValue("simulated_photoeye", out var blocked) && blocked)
            _beamStep = scan;
    }
    public bool CycleFinished(long scan) => _kind switch
    {
        ChallengeCaseKind.ColdStart => scan >= 25,
        ChallengeCaseKind.Holding => scan >= 10,
        ChallengeCaseKind.ProcessStop => scan >= 36,
        ChallengeCaseKind.SimultaneousStartStop => scan >= 26,
        ChallengeCaseKind.BlockedStart => _beamStep.HasValue && scan >= _beamStep + 51,
        _ => _beamStep.HasValue && scan >= _beamStep + 26
    };
}
