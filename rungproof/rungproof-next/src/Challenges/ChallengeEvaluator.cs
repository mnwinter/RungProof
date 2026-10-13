using System;
using System.Collections.Generic;
namespace RungProof.Next.Challenges;
/// <summary>Read-only temporal acceptance. Never sets plant state or runtime commands.</summary>
public sealed class ChallengeEvaluator
{
    private const double Epsilon = 1e-9;
    private ChallengeRunIdentity? _identity;
    private ChallengeTestCase? _case;
    private ChallengeCaseResult? _failure;
    private ChallengeObservation? _last;
    private long? _beam;
    private double _stopPosition;
    private int _cycle;
    private int _completedCycles;
    private bool _travel;
    public void BeginCase(ChallengeRunIdentity identity, ChallengeTestCase testCase)
    { _identity=identity; _case=testCase; _failure=null; _last=null; _beam=null; _cycle=0; _completedCycles=0; _travel=false; _stopPosition=0; }
    public ChallengeCaseResult? Observe(ChallengeObservation row)
    {
        if (_case is null) throw new InvalidOperationException("BeginCase is required.");
        if (_failure is not null) return _failure;
        ChallengeCaseResult Fail(string code,string expected) => _failure=new(_case.Id,ChallengeOutcome.Failed,code,expected,
            $"cycle={row.Cycle}, scan={row.ScanNumber}, output={row.AppliedOutputs.GetValueOrDefault("conveyor_running")}, present={row.PlantAfter.ObjectPresent}",row);
        if(row.Identity != _identity || row.CaseId != _case.Id || row.CaseSequence != (_last?.CaseSequence ?? 0)+1
            || row.CaseSequence>250 || row.Cycle < _cycle || row.Cycle > _cycle+1 || row.Cycle>1
            || (row.Cycle!=0 && _case.Kind!=ChallengeCaseKind.ResetRepeat)
            || row.SimulatedTime!=TimeSpan.FromMilliseconds(row.ScanNumber*20)
            || row.ScanNumber != (row.Cycle == _cycle ? (_last?.ScanNumber ?? 0)+1 : 1))
            return Fail("CH_OBSERVATION_SEQUENCE","Contiguous accepted observations and reset cycles.");
        if(row.RuntimeDiagnostics.Count>0) return Fail("CH_RUNTIME_DIAGNOSTIC","No runtime semantic diagnostics.");
        if(row.Cycle != _cycle) { if(!BeamComplete(_last!)) return Fail("CH_RESET_INCOMPLETE","Complete first beam stop before reset."); _completedCycles++; _cycle=row.Cycle; _beam=null; _travel=false; }
        _last=row;
        if(!row.EffectiveInputs.ContainsKey("operator.start") || !row.EffectiveInputs.ContainsKey("operator.stop") || !row.EffectiveInputs.ContainsKey("simulated_photoeye") || !row.AppliedOutputs.ContainsKey("conveyor_running")
            || !row.PlantAfter.Numbers.TryGetValue("object_position",out var edge) || !double.IsFinite(edge) || !row.PlantAfter.Feedback.TryGetValue("simulated_photoeye",out var feedback))
            return Fail("CH_OBSERVATION_IMAGE","Complete finite binding-keyed images.");
        bool run=row.AppliedOutputs["conveyor_running"];
        bool expectedStart=_case.Kind!=ChallengeCaseKind.ColdStart && row.ScanNumber==1
            || _case.Kind==ChallengeCaseKind.BlockedStart && _beam.HasValue && row.ScanNumber==_beam+27;
        bool expectedStop=_case.Kind==ChallengeCaseKind.SimultaneousStartStop && row.ScanNumber==1
            || _case.Kind==ChallengeCaseKind.ProcessStop && row.ScanNumber==11;
        if(row.EffectiveInputs["operator.start"]!=expectedStart || row.EffectiveInputs["operator.stop"]!=expectedStop)
            return Fail("CH_STIMULUS","Published one-accepted-scan operator pulses.");
        if(!row.PlantAfter.ObjectPresent || row.PlantAfter.Completed!=0) return Fail("CH_CARTON_LOST","Carton present and completion zero.");
        if(_case.Kind is ChallengeCaseKind.ColdStart or ChallengeCaseKind.Holding or ChallengeCaseKind.ProcessStop or ChallengeCaseKind.SimultaneousStartStop
            && (feedback || row.EffectiveInputs["simulated_photoeye"])) return Fail("CH_FEEDBACK","Beam clear before the carton reaches the sensor.");
        switch(_case.Kind)
        {
            case ChallengeCaseKind.ColdStart:
            case ChallengeCaseKind.SimultaneousStartStop:
                if(run || Math.Abs(edge)>Epsilon) return Fail("CH_STOP_PRIORITY","Command false and loaded origin."); break;
            case ChallengeCaseKind.Holding:
                if(!run || Math.Abs(edge-row.ScanNumber*.01)>Epsilon) return Fail("CH_HOLDING","Command true after Start release and .01 travel per scan."); break;
            case ChallengeCaseKind.ProcessStop:
                if(run != (row.ScanNumber<11) || Math.Abs(edge-Math.Min(row.ScanNumber,10)*.01)>Epsilon)
                    return Fail("CH_PROCESS_STOP","Process Stop inhibits scan 11 and subsequent travel."); break;
            default:
                if(!_beam.HasValue)
                {
                    if(!run || Math.Abs(edge-row.ScanNumber*.01)>Epsilon) return Fail("CH_TRAVEL","Start initiates uninterrupted modeled travel to the actual beam.");
                    _travel |= edge> Epsilon;
                    if(feedback) { _beam=row.ScanNumber; if(_beam>75 || !_travel) return Fail("CH_BEAM_TIMEOUT","Actual beam transition by scan 75 after travel."); }
                    else if(row.ScanNumber>=75) return Fail("CH_CASE_TIMEOUT","Actual photoeye transition by scan 75.");
                }
                else
                {
                    if(row.ScanNumber==_beam+1) _stopPosition=edge;
                    if(!row.EffectiveInputs["simulated_photoeye"] || run || edge<.5-Epsilon || edge>.51+Epsilon || Math.Abs(edge-_stopPosition)>Epsilon)
                        return Fail("CH_BEAM_STOP","Sampled beam inhibits next scan and holds present carton at .5 through .51.");
                }
                break;
        }
        return null;
    }
    private bool BeamComplete(ChallengeObservation row) => _beam.HasValue && row.ScanNumber >= _beam+26;
    public ChallengeCaseResult FinishCase()
    {
        if(_case is null) throw new InvalidOperationException("BeginCase is required.");
        if(_failure is not null) return _failure;
        bool complete=_last is not null && (_case.Kind switch {
            ChallengeCaseKind.ColdStart => _last.ScanNumber>=25,
            ChallengeCaseKind.SimultaneousStartStop => _last.ScanNumber>=26,
            ChallengeCaseKind.Holding => _last.ScanNumber>=10,
            ChallengeCaseKind.ProcessStop => _last.ScanNumber>=36,
            ChallengeCaseKind.BlockedStart => _beam.HasValue && _last.ScanNumber>=_beam+51,
            ChallengeCaseKind.ResetRepeat => _completedCycles==1 && BeamComplete(_last),
            _ => BeamComplete(_last)});
        return new(_case.Id,complete?ChallengeOutcome.Passed:ChallengeOutcome.Failed,complete?"CH_CASE_PASSED":"CH_CASE_TIMEOUT","Complete published case protocol.",complete?"All required accepted observations satisfied.":"Required observations missing.",complete?null:_last);
    }
}

