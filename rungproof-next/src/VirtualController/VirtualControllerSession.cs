using System;
using System.Collections.Generic;

namespace RungProof.Next.VirtualController;

/// <summary>
/// Fixed-time scan coordinator. Each completed scan samples inputs, executes
/// LD, commits outputs, advances the plant by exactly one scan period, and
/// publishes an immutable snapshot in that order.
/// </summary>
public sealed class VirtualControllerSession
{
    public const int MaximumScansPerAdvance = 1000;
    private readonly VirtualControllerRuntime _runtime;
    private readonly Dictionary<string, bool> _operatorInputs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pulseInputs = new(StringComparer.Ordinal);
    private double _accumulatorSeconds;
    public Guid SessionId { get; } = Guid.NewGuid();
    public Guid LoadId { get; } = Guid.NewGuid();
    public long ExecutionGeneration { get; private set; } = 1;
    public long ScanAttemptNumber { get; private set; }
    public long SuccessfulScanCount { get; private set; }
    private long _publicationRevision;
    private long _historyBytes;
    public const long HistoryByteCapacity = 64L * 1024 * 1024;
    public long HistoryRetainedBytes => _historyBytes;
    public bool CurrentHistoryOmitted { get; private set; }
    public ControllerPublication Publication { get; private set; }
    public event Action<ControllerPublication>? PublicationPublished;
    /// <summary>Emitted after an attempt obtains its final disposition, including abort/failure/abandonment.</summary>
    public event Action<ControllerPublication>? ScanAttempted;
    private readonly List<ControllerPublication> _abandonedAttempts=[];
    private readonly List<long> _abandonedCharges=[];
    private long _abandonedBytes;
    public long AbandonedAttemptsDroppedCount { get; private set; }
    public IReadOnlyList<ControllerPublication> AbandonedAttempts=>Array.AsReadOnly(_abandonedAttempts.ToArray());

    public event Action<VirtualControllerSnapshot>? SnapshotPublished;
    public event Action<VirtualControllerScanCompleted>? ScanCompleted;
    private long _lifecycleGeneration;
    private readonly List<VirtualControllerAcceptedScan> _history=[];
    public const int HistoryCapacity=128;
    public bool IsPaused { get; private set; }
    public bool HistoryRecordingEnabled { get; set; }=true;
    public long HistoryDroppedCount { get; private set; }
    public IReadOnlyList<VirtualControllerAcceptedScan> History => Array.AsReadOnly(_history.ToArray());
    public IReadOnlyDictionary<string,bool> PendingInputs => new System.Collections.ObjectModel.ReadOnlyDictionary<string,bool>(new Dictionary<string,bool>(_operatorInputs,StringComparer.Ordinal));
    public bool TryGetPendingInput(string name,out bool value)=>_operatorInputs.TryGetValue(name,out value);
    public void Pause(){IsPaused=true;_lifecycleGeneration++; PublishLifecycle(ControllerPublicationKind.Pause, Snapshot);}
    public void ClearHistory(){_history.Clear();HistoryDroppedCount=0;_historyBytes=0;CurrentHistoryOmitted=false;}
    public VirtualControllerSnapshot Snapshot => _runtime.Snapshot;
    public TimeSpan ScanPeriod => _runtime.ScanPeriod;

    public VirtualControllerSession(VirtualControllerRuntime runtime)
    {
        _runtime = runtime;
        Publication = new(SessionId, runtime.ProgramContentId, LoadId, ExecutionGeneration, 0, runtime.Snapshot.ScanNumber,
            ++_publicationRevision, ControllerPublicationKind.Loaded, ControllerCompletion.NoExecution,
            ControllerPhaseCompletion.None, null, null, runtime.Snapshot);
    }

    private ControllerPublication PublishLifecycle(ControllerPublicationKind kind, VirtualControllerSnapshot snapshot, bool clearObservations=false)
    {
        var prior=Publication;
        Publication=new(SessionId,_runtime.ProgramContentId,LoadId,ExecutionGeneration,ScanAttemptNumber,snapshot.ScanNumber,
            ++_publicationRevision,kind,ControllerCompletion.NoExecution,ControllerPhaseCompletion.None,
            clearObservations?null:prior.ObservationOrigin,clearObservations?null:prior.Observations,snapshot)
            {PublishedValues=new(snapshot.Variables,snapshot.NumericVariables,snapshot.TextVariables)};
        var publication=Publication;
        NotifyPublication(publication);
        return publication;
    }

    // Synchronous subscribers may publish a newer lifecycle envelope. Stop delivering
    // the older active image to later subscribers after that boundary changes.
    private void NotifyPublication(ControllerPublication publication)
    {
        foreach(var callback in PublicationPublished?.GetInvocationList()??Array.Empty<Delegate>())
        {if(!ReferenceEquals(Publication,publication))break;((Action<ControllerPublication>)callback)(publication);}
    }
    private void NotifySnapshot(VirtualControllerSnapshot snapshot,ControllerPublication publication)
    {
        foreach(var callback in SnapshotPublished?.GetInvocationList()??Array.Empty<Delegate>())
        {if(!ReferenceEquals(Publication,publication))break;((Action<VirtualControllerSnapshot>)callback)(snapshot);}
    }
    private void NotifyCompleted(VirtualControllerScanCompleted completed,ControllerPublication publication)
    {
        foreach(var callback in ScanCompleted?.GetInvocationList()??Array.Empty<Delegate>())
        {if(!ReferenceEquals(Publication,publication))break;((Action<VirtualControllerScanCompleted>)callback)(completed);}
    }

    public void Run()
    {
        _lifecycleGeneration++; IsPaused=false;
        _runtime.Run();
        var snapshot=_runtime.Snapshot;
        var publication=PublishLifecycle(ControllerPublicationKind.Run, snapshot);
        NotifySnapshot(snapshot,publication);
    }

    public VirtualControllerSnapshot Stop()
    {
        _lifecycleGeneration++; IsPaused=false;
        // A momentary command that was not scanned before Stop must not fire
        // during a later Run. Held inputs retain their separate semantics.
        foreach (var name in _pulseInputs) _operatorInputs[name] = false;
        _pulseInputs.Clear();
        _accumulatorSeconds = 0.0;
        var snapshot = _runtime.Stop();
        var publication=PublishLifecycle(ControllerPublicationKind.Stop, snapshot);
        NotifySnapshot(snapshot,publication);
        return snapshot;
    }

    public VirtualControllerSnapshot Reset()
    {
        _lifecycleGeneration++; IsPaused=false; ClearHistory();
        ExecutionGeneration++; ScanAttemptNumber=0; SuccessfulScanCount=0;
        _accumulatorSeconds = 0.0;
        _operatorInputs.Clear();
        _pulseInputs.Clear();
        var snapshot = _runtime.Reset();
        var publication=PublishLifecycle(ControllerPublicationKind.Reset, snapshot, clearObservations:true);
        NotifySnapshot(snapshot,publication);
        return snapshot;
    }

    public void SetInput(string name, bool value) => _operatorInputs[name] = value;

    public void PulseInput(string name)
    {
        if (!_operatorInputs.GetValueOrDefault(name, _runtime.Snapshot.Variables.GetValueOrDefault(name)))
            _runtime.PrepareExplicitInputPulse(name);
        _operatorInputs[name] = true;
        _pulseInputs.Add(name);
    }

    public void PrepareExplicitInputPulse(string name) => _runtime.PrepareExplicitInputPulse(name);

    public VirtualControllerSnapshot SetBoolForce(string variableName, bool value)
    {
        var snapshot = _runtime.SetBoolForce(variableName, value);
        _lifecycleGeneration++;
        var publication=PublishLifecycle(ControllerPublicationKind.ForceChanged, snapshot);
        NotifySnapshot(snapshot,publication);
        return snapshot;
    }

    public VirtualControllerSnapshot ModifyVariable(string variableName, object value)
    {
        return ModifyVariables([(variableName, value)]);
    }

    public VirtualControllerSnapshot ModifyVariables(IReadOnlyList<(string VariableName, object Value)> edits)
    {
        var snapshot = _runtime.ModifyVariables(edits);
        _lifecycleGeneration++;
        var publication = PublishLifecycle(ControllerPublicationKind.ValueModified, snapshot);
        NotifySnapshot(snapshot, publication);
        return snapshot;
    }

    public VirtualControllerSnapshot RemoveForce(string variableName)
    {
        var snapshot = _runtime.RemoveForce(variableName);
        _lifecycleGeneration++;
        var publication=PublishLifecycle(ControllerPublicationKind.ForceChanged, snapshot);
        NotifySnapshot(snapshot,publication);
        return snapshot;
    }

    public VirtualControllerSnapshot ClearForces()
    {
        var snapshot = _runtime.ClearForces();
        _lifecycleGeneration++;
        var publication=PublishLifecycle(ControllerPublicationKind.ForceChanged, snapshot);
        NotifySnapshot(snapshot,publication);
        return snapshot;
    }

    public int Advance(
        double elapsedSeconds,
        Func<IReadOnlyDictionary<string, bool>> samplePlantInputs,
        Action<IReadOnlyDictionary<string, bool>> commitOutputs,
        Action<double> advancePlant)
        => Advance(
            elapsedSeconds,
            samplePlantInputs,
            static () => new Dictionary<string, double>(),
            commitOutputs,
            static _ => { },
            advancePlant);

    public int Advance(
        double elapsedSeconds,
        Func<IReadOnlyDictionary<string, bool>> samplePlantInputs,
        Func<IReadOnlyDictionary<string, double>> sampleNumericPlantInputs,
        Action<IReadOnlyDictionary<string, bool>> commitOutputs,
        Action<IReadOnlyDictionary<string, double>> commitNumericOutputs,
        Action<double> advancePlant,
        Func<IReadOnlyDictionary<string, string>>? sampleTextPlantInputs = null,
        Action<IReadOnlyDictionary<string, string>>? commitTextOutputs = null,
        Func<VirtualControllerSignalImage>? samplePostPlantInputs = null)
    {
        if (!double.IsFinite(elapsedSeconds))
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed simulation time must be finite.");
        // Stopped time is not PLC time. In particular, do not sample a
        // momentary input, commit an old image, or accrue future plant ticks.
        if (IsPaused || _runtime.State != VirtualControllerState.Running) return 0;
        _accumulatorSeconds += Math.Max(0.0, elapsedSeconds);
        var cycleSeconds = ScanPeriod.TotalSeconds;
        var scans = 0;
        // Yield after a bounded batch. Keep remaining time for the next call:
        // silently dropping scans would change timers, edges and plant motion.
        // Stop/Reset deliberately clear that backlog with the existing lifecycle.
        while (!IsPaused && _runtime.State == VirtualControllerState.Running
            && _accumulatorSeconds + 1e-12 >= cycleSeconds && scans < MaximumScansPerAdvance)
        {
            _accumulatorSeconds -= cycleSeconds;
            if (!ExecuteAcceptedCycle(samplePlantInputs,sampleNumericPlantInputs,commitOutputs,commitNumericOutputs,
                advancePlant,sampleTextPlantInputs,commitTextOutputs,samplePostPlantInputs,allowPaused:false)) break;
            scans++;
        }
        return scans;
    }
    public int StepOneAcceptedScan(Func<IReadOnlyDictionary<string,bool>> samplePlantInputs,
        Func<IReadOnlyDictionary<string,double>> sampleNumericPlantInputs,
        Action<IReadOnlyDictionary<string,bool>> commitOutputs,Action<IReadOnlyDictionary<string,double>> commitNumericOutputs,
        Action<double> advancePlant,Func<IReadOnlyDictionary<string,string>>? sampleTextPlantInputs=null,
        Action<IReadOnlyDictionary<string,string>>? commitTextOutputs=null,Func<VirtualControllerSignalImage>? samplePostPlantInputs=null)
    {
        if(!IsPaused||_runtime.State!=VirtualControllerState.Running)return 0;
        return ExecuteAcceptedCycle(samplePlantInputs,sampleNumericPlantInputs,commitOutputs,commitNumericOutputs,
            advancePlant,sampleTextPlantInputs,commitTextOutputs,samplePostPlantInputs,true)?1:0;
    }
    private bool ExecuteAcceptedCycle(Func<IReadOnlyDictionary<string,bool>> samplePlantInputs,
        Func<IReadOnlyDictionary<string,double>> sampleNumericPlantInputs,
        Action<IReadOnlyDictionary<string,bool>> commitOutputs,Action<IReadOnlyDictionary<string,double>> commitNumericOutputs,
        Action<double> advancePlant,Func<IReadOnlyDictionary<string,string>>? sampleTextPlantInputs,
        Action<IReadOnlyDictionary<string,string>>? commitTextOutputs,Func<VirtualControllerSignalImage>? samplePostPlantInputs,bool allowPaused)
    {
        var generation=_lifecycleGeneration;
        var executionGeneration=ExecutionGeneration;
        var origin=new ObservationOrigin(SessionId,LoadId,executionGeneration,++ScanAttemptNumber);
        var startingSnapshot=Snapshot;
        var phases=ControllerPhaseCompletion.None;
        var commitProgress=ControllerOutputCommitProgress.None;
        var phase="BoolInputSample";
        VirtualControllerSnapshot? executed=null;
        VirtualControllerSignalImage? sampled=null,effective=null,programEnd=null,published=null,post=null,outputs=null;
        var bools=new Dictionary<string,bool>(StringComparer.Ordinal);
        var numbers=new Dictionary<string,double>(StringComparer.Ordinal);
        var text=new Dictionary<string,string>(StringComparer.Ordinal);
        InstructionObservationCapture? observations=null;
        var finalDisposition=false;
        bool invalidated()=>generation!=_lifecycleGeneration;
        ControllerPublication Envelope(ControllerPublicationKind kind,ControllerCompletion completion,string? failureCode=null)
        {
            var snapshot=executed??startingSnapshot;
            var missing=new Dictionary<string,string>(StringComparer.Ordinal);
            if(sampled is null)missing["SampledInputs"]="Input sampling did not complete; see FailurePhase.";
            else if(!phases.HasFlag(ControllerPhaseCompletion.InputSampled))missing["SampledInputsCompleteness"]="Partial input sample; see FailurePhase. Uncalled input samplers have no captured values.";
            if(effective is null)missing["EffectiveInputs"]="Program did not return an effective input image.";
            if(programEnd is null)missing["ProgramEndValues"]="Program execution did not return an end image.";
            if(published is null)missing["PublishedValues"]="No program publication image was obtained.";
            if(post is null)missing["PostPlantInputs"]=samplePostPlantInputs is null?"No post-plant sampler configured.":"Post-plant sampling did not complete.";
            return new(SessionId,_runtime.ProgramContentId,LoadId,executionGeneration,origin.ScanAttemptNumber,
                snapshot.ScanNumber,++_publicationRevision,kind,completion,phases,origin,observations,snapshot,
                failureCode is null?null:phase,failureCode)
            {
                SampledInputs=sampled,EffectiveInputs=effective,ProgramEndValues=programEnd,
                PublishedValues=published,PostPlantInputs=post,CommitProgress=commitProgress,
                ExecutedNetworkCount=executed?.ExecutedNetworkCount??0,
                LastFullyObservedInstructionId=executed?.LastFullyObservedInstructionId,
                LastFullyObservedOrdinal=executed?.LastFullyObservedOrdinal??0,
                ImageUnavailableReasons=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(missing)
            };
        }
        bool Abandon()
        {
            // The active lifecycle publication stays untouched. The old origin and completed phases survive separately.
            var envelope=Envelope(ControllerPublicationKind.ScanAborted,ControllerCompletion.Aborted,"VC_ATTEMPT_ABANDONED");
            var charge=new VirtualControllerAcceptedScan(new(envelope.RuntimeScanNumber,envelope.Snapshot.SimulatedTime,
                envelope.SampledInputs??EmptyImage(),envelope.EffectiveInputs??EmptyImage(),outputs??EmptyImage(),envelope.Snapshot.Diagnostics)
                {Completion=ControllerCompletion.Aborted,Publication=envelope},post??EmptyImage()).EstimatedPayloadBytes;
            if(charge<=HistoryByteCapacity)
            {
                _abandonedAttempts.Add(envelope);_abandonedCharges.Add(charge);_abandonedBytes+=charge;
                TrimRetainedPayload();
            }
            else AbandonedAttemptsDroppedCount++;
            finalDisposition=true;
            ScanAttempted?.Invoke(envelope);
            return false;
        }
        try
        {
            bools=new(samplePlantInputs(),StringComparer.Ordinal);sampled=new(bools,numbers,text);
            if(invalidated())return Abandon();
            phase="NumericInputSample";
            numbers=new(sampleNumericPlantInputs(),StringComparer.Ordinal);sampled=new(bools,numbers,text);
            if(invalidated())return Abandon();
            foreach(var (name,value) in _operatorInputs)bools[name]=value;
            phase="TextInputSample";
            text=new(sampleTextPlantInputs?.Invoke()??new Dictionary<string,string>(),StringComparer.Ordinal);
            sampled=new(bools,numbers,text);phases|=ControllerPhaseCompletion.InputSampled;
            if(invalidated())return Abandon();
            if(_runtime.State!=VirtualControllerState.Running||(!allowPaused&&IsPaused))return Abandon();
            phase="Program";
            var previous=startingSnapshot.ScanNumber;
            executed=_runtime.Scan(bools,numbers,text);
            if(executed.ScanNumber!=previous+1)return Abandon();
            if(executed.Completion==ControllerCompletion.Completed)phases|=ControllerPhaseCompletion.ProgramExecuted;
            observations=executed.Observations?.WithOrigin(origin);
            effective=_runtime.CaptureEffectiveInputs(executed);
            programEnd=executed.ProgramEndValues;
            published=new(executed.Variables,executed.NumericVariables,executed.TextVariables);
            outputs=new(executed.Outputs,executed.NumericOutputs,executed.TextOutputs);
            phase="BoolOutputCommit";
            commitOutputs(executed.Outputs);commitProgress|=ControllerOutputCommitProgress.Bool;
            if(invalidated())return Abandon();
            phase="NumericOutputCommit";
            commitNumericOutputs(executed.NumericOutputs);commitProgress|=ControllerOutputCommitProgress.Numeric;
            if(commitTextOutputs is null)phases|=ControllerPhaseCompletion.OutputsCommitted;
            if(invalidated())return Abandon();
            if(commitTextOutputs is not null)
            {
                phase="TextOutputCommit";
                commitTextOutputs(executed.TextOutputs);commitProgress|=ControllerOutputCommitProgress.Text;
                phases|=ControllerPhaseCompletion.OutputsCommitted;
                if(invalidated())return Abandon();
            }
            phase="PlantAdvance";
            advancePlant(ScanPeriod.TotalSeconds);phases|=ControllerPhaseCompletion.PlantAdvanced;
            if(invalidated())return Abandon();
            foreach(var name in _pulseInputs)_operatorInputs[name]=false;
            _pulseInputs.Clear();
            phase="PostPlantSample";
            post=samplePostPlantInputs?.Invoke();
            if(post is not null)phases|=ControllerPhaseCompletion.PostPlantSampled;
            if(invalidated())return Abandon();
            var successful=executed.Completion==ControllerCompletion.Completed;
            if(!successful)phase="Program";
            var publication=Envelope(successful?ControllerPublicationKind.ScanCompleted:ControllerPublicationKind.ScanAborted,
                executed.Completion,successful?null:"VC_RUNTIME_JUMP_LIMIT");
            Publication=publication;
            var completed=new VirtualControllerScanCompleted(executed.ScanNumber,executed.SimulatedTime,sampled,effective,outputs,executed.Diagnostics)
                {Completion=executed.Completion,Publication=publication};
            RetainAttempt(completed,post);
            if(successful)SuccessfulScanCount++;
            finalDisposition=true;phase="Publication";
            ScanAttempted?.Invoke(publication);
            if(!invalidated())NotifyPublication(publication);
            if(successful&&!invalidated())NotifyCompleted(completed,publication);
            if(!invalidated())NotifySnapshot(executed,publication);
            return successful;
        }
        catch(Exception exception)
        {
            if(finalDisposition)throw; // A subscriber cannot retroactively change an already recorded execution disposition.
            if(invalidated()) {Abandon();throw;}
            var failure=Envelope(ControllerPublicationKind.PublicationFailed,ControllerCompletion.PublicationFailed,exception.GetType().Name);
            Publication=failure;
            var snapshot=executed??startingSnapshot;
            RetainAttempt(new(snapshot.ScanNumber,snapshot.SimulatedTime,sampled??EmptyImage(),effective??EmptyImage(),outputs??EmptyImage(),snapshot.Diagnostics)
                {Completion=ControllerCompletion.PublicationFailed,Publication=failure},post);
            finalDisposition=true;phase="Publication";
            ScanAttempted?.Invoke(failure);
            if(!invalidated())NotifyPublication(failure);
            throw;
        }
    }
    private static VirtualControllerSignalImage EmptyImage()=>new(new Dictionary<string,bool>(),new Dictionary<string,double>(),new Dictionary<string,string>());
    private void RetainAttempt(VirtualControllerScanCompleted completed,VirtualControllerSignalImage? post)
    {
        if(!HistoryRecordingEnabled)return;
        var entry=new VirtualControllerAcceptedScan(completed,post??EmptyImage());
        var bytes=entry.EstimatedPayloadBytes;
        CurrentHistoryOmitted=bytes>HistoryByteCapacity;
        if(CurrentHistoryOmitted){HistoryDroppedCount++;return;}
        _history.Add(entry);_historyBytes+=bytes;
        TrimRetainedPayload();
    }
    private void TrimRetainedPayload()
    {
        while(_history.Count>HistoryCapacity||_abandonedAttempts.Count>HistoryCapacity||_historyBytes+_abandonedBytes>HistoryByteCapacity)
        {
            var removeHistory=_history.Count>HistoryCapacity||_abandonedAttempts.Count==0
                ||_history.Count>0&&_abandonedAttempts.Count<=HistoryCapacity&&(_history[0].CompletedScan.Publication?.Revision??0)<_abandonedAttempts[0].Revision;
            if(removeHistory){_historyBytes-=_history[0].EstimatedPayloadBytes;_history.RemoveAt(0);HistoryDroppedCount++;}
            else{_abandonedBytes-=_abandonedCharges[0];_abandonedCharges.RemoveAt(0);_abandonedAttempts.RemoveAt(0);AbandonedAttemptsDroppedCount++;}
        }
    }
}
