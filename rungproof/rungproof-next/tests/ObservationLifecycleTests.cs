using System;
using System.Collections.Generic;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterObservationLifecycleTests()
    {
        Test("ForceHasSeparatePublication", ForceHasSeparatePublication);
        Test("ResetRejectsLatePublication", ResetRejectsLatePublication);
        Test("WatchdogIsAbortedNotCompleted", WatchdogIsAbortedNotCompleted);
        Test("CommitFailureRecordsActualPhases", CommitFailureRecordsActualPhases);
        Test("PlantFailureRecordsActualPhases", PlantFailureRecordsActualPhases);
        Test("LifecycleRevisionNeverResets", LifecycleRevisionNeverResets);
        Test("ObservationHistoryEvictsWholeImmutableEnvelopes", ObservationHistoryEvictsWholeImmutableEnvelopes);
        Test("ForcePublicationPreservesConsumedOrigin", ForcePublicationPreservesConsumedOrigin);
        Test("ObservationCyclePreservesCallbackOrder", ObservationCyclePreservesCallbackOrder);
        Test("InterruptedAttemptsRetainOriginalIdentityAtEachPhase", InterruptedAttemptsRetainOriginalIdentityAtEachPhase);
        Test("PartialOutputCommitFailureHasImagesAndSpecificPhase", PartialOutputCommitFailureHasImagesAndSpecificPhase);
        Test("WatchdogProgressSurvivesMonitoringDisabled", WatchdogProgressSurvivesMonitoringDisabled);
        Test("WatchdogProgressSurvivesObservationCapacityExceeded", WatchdogProgressSurvivesObservationCapacityExceeded);
        Test("EachFailurePhasePreservesAvailableImages", EachFailurePhasePreservesAvailableImages);
        Test("AttemptCallbackLifecycleDoesNotPublishOlderEnvelope", AttemptCallbackLifecycleDoesNotPublishOlderEnvelope);
        Test("FailureAttemptCallbackKeepsFailureEnvelopeIndependent", FailureAttemptCallbackKeepsFailureEnvelopeIndependent);
        Test("ForceCallbacksNeverSupersedeNewerForcePublication", ForceCallbacksNeverSupersedeNewerForcePublication);
    }
    private static void ForceHasSeparatePublication()
    {
        var session = new VirtualControllerSession(Runtime(ObservationProgram()));
        var before = session.Publication;
        session.SetBoolForce("enable", true);
        Equal(ControllerPublicationKind.ForceChanged, session.Publication.Kind);
        True(session.Publication.Revision > before.Revision);
        Equal(ControllerCompletion.NoExecution, session.Publication.Completion);
    }
    private static void ResetRejectsLatePublication()
    {
        var session = new VirtualControllerSession(Runtime(ObservationProgram()));
        session.Run();
        var generation = session.Publication.ExecutionGeneration;
        session.Advance(.01, () => new Dictionary<string, bool>(), _ => session.Reset(), _ => { });
        True(session.Publication.ExecutionGeneration > generation);
        Equal(ControllerPublicationKind.Reset, session.Publication.Kind);
        Equal(0, session.History.Count);
        True(session.Publication.ObservationOrigin is null);
        Equal(1,session.AbandonedAttempts.Count);
        Equal(generation,session.AbandonedAttempts[0].ExecutionGeneration);
        Equal(ControllerCompletion.Aborted,session.AbandonedAttempts[0].Completion);
    }
    private static void WatchdogIsAbortedNotCompleted()
    {
        var runtime = Runtime(ObservationProgram(
            new("label-net", "Label", new("loop1", LadderNodeKind.Contact, "enable", true), LabelInstruction: new("label", "again")),
            new("jump-net", "Jump", new("loop2", LadderNodeKind.Contact, "enable", true), Jump: new("jump", "again"))));
        var session = new VirtualControllerSession(runtime);
        var commits = 0; var plant = 0;
        var attempts=0;var completions=0;
        session.ScanAttempted+=_=>attempts++;session.ScanCompleted+=_=>completions++;
        session.Run();
        Equal(0, session.Advance(.01, () => new Dictionary<string, bool>(), _ => commits++, _ => plant++));
        Equal(1, commits); Equal(1, plant);
        Equal(1,attempts);Equal(0,completions);
        Equal(ControllerCompletion.Aborted, session.Publication.Completion);
        Equal(ControllerPublicationKind.ScanAborted, session.Publication.Kind);
        Equal(0L, session.SuccessfulScanCount);
        Equal(1, session.History.Count);
        True(session.Publication.Observations!.IsPartial);
        Equal(10000,session.Publication.ExecutedNetworkCount);
        True(session.Publication.LastFullyObservedInstructionId is not null);
    }
    private static void CommitFailureRecordsActualPhases()
    {
        var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();
        try {session.Advance(.01,()=>new Dictionary<string,bool>(),_=>throw new InvalidOperationException("commit"),_=>{});}
        catch(InvalidOperationException){}
        Equal(ControllerCompletion.PublicationFailed,session.Publication.Completion);
        True(session.Publication.Phases.HasFlag(ControllerPhaseCompletion.ProgramExecuted));
        False(session.Publication.Phases.HasFlag(ControllerPhaseCompletion.OutputsCommitted));
        Equal(0L,session.SuccessfulScanCount);
    }
    private static void PlantFailureRecordsActualPhases()
    {
        var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();
        try {session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>throw new InvalidOperationException("plant"));}
        catch(InvalidOperationException){}
        Equal(ControllerCompletion.PublicationFailed,session.Publication.Completion);
        True(session.Publication.Phases.HasFlag(ControllerPhaseCompletion.OutputsCommitted));
        False(session.Publication.Phases.HasFlag(ControllerPhaseCompletion.PlantAdvanced));
        True(session.Publication.SampledInputs is not null);
        True(session.Publication.EffectiveInputs is not null);
        True(session.Publication.ProgramEndValues is not null);
        True(session.Publication.PublishedValues is not null);
    }
    private static void LifecycleRevisionNeverResets()
    {
        var runtime=Runtime(ObservationProgram());var session=new VirtualControllerSession(runtime);
        var other=new VirtualControllerSession(Runtime(ObservationProgram()));
        True(session.SessionId!=other.SessionId);True(session.LoadId!=other.LoadId);
        Equal(session.Publication.ProgramContentId,other.Publication.ProgramContentId);
        session.Run();session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>{});
        var old=session.Publication;session.Reset();session.Run();session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>{});
        Equal(old.RuntimeScanNumber,session.Publication.RuntimeScanNumber);
        True(old.ExecutionGeneration!=session.Publication.ExecutionGeneration);
        True(old.Revision<session.Publication.Revision);
    }
    private static void ObservationHistoryEvictsWholeImmutableEnvelopes()
    {
        var session=new VirtualControllerSession(Runtime(ObservationProgram(new LadderNetwork("n","Move",ObservationEnable("c"),
            NumericOperation:new("op",LadderNumericOperationKind.Move,"value","0","target")))));
        session.Run();session.SetInput("enable",true);
        session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>{});
        var retained=session.Publication;
        var count=retained.Observations!.Counts["op"];
        for(var i=0;i<140;i++)session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>{});
        Equal(128,session.History.Count);Equal(13L,session.HistoryDroppedCount);
        True(session.HistoryRetainedBytes<=VirtualControllerSession.HistoryByteCapacity);
        Equal(1L,retained.ScanAttemptNumber);Equal(count,retained.Observations.Counts["op"]);
        True(retained.Observations.GlobalLast["op"].Origin==retained.ObservationOrigin);
        var mutable=new List<InstructionDiagnostic>{new("X","i","A","Program","first")};
        var observation=new InstructionObservation("i","no","b","t",1,1,[],[],true,true){Diagnostics=mutable};
        mutable.Clear();Equal(1,observation.Diagnostics.Count);
    }
    private static void ForcePublicationPreservesConsumedOrigin()
    {
        var session=new VirtualControllerSession(Runtime(ObservationProgram(new LadderNetwork("n","Coil",ObservationEnable("c"),Coil:new("op","output")))));
        session.Run();session.SetInput("enable",true);
        session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>{});
        var execution=session.Publication;
        Equal(true,(bool)execution.Observations!.GlobalLast["op"].StoredAfter!);
        session.SetBoolForce("output",false);
        Equal(ControllerPublicationKind.ForceChanged,session.Publication.Kind);
        True(session.Publication.ObservationOrigin==execution.ObservationOrigin);
        Equal(true,(bool)session.Publication.Observations!.GlobalLast["op"].StoredAfter!);
        Equal(false,session.Publication.PublishedValues!.Bools["output"]);
    }
    private static void ObservationCyclePreservesCallbackOrder()
    {
        var session=new VirtualControllerSession(Runtime(ObservationProgram()));var calls=new List<string>();
        session.ScanCompleted+=_=>calls.Add("event");session.Run();
        session.Advance(.01,()=>{calls.Add("bool-input");return new Dictionary<string,bool>();},
            ()=>{calls.Add("numeric-input");return new Dictionary<string,double>();},
            _=>calls.Add("bool-output"),_=>calls.Add("numeric-output"),_=>calls.Add("plant"),
            ()=>{calls.Add("text-input");return new Dictionary<string,string>();},_=>calls.Add("text-output"),
            ()=>{calls.Add("post-input");return new(new Dictionary<string,bool>(),new Dictionary<string,double>(),new Dictionary<string,string>());});
        Equal("bool-input,numeric-input,text-input,bool-output,numeric-output,text-output,plant,post-input,event",string.Join(",",calls));
        Equal(ControllerPhaseCompletion.InputSampled|ControllerPhaseCompletion.ProgramExecuted|ControllerPhaseCompletion.OutputsCommitted|
            ControllerPhaseCompletion.PlantAdvanced|ControllerPhaseCompletion.PostPlantSampled,session.Publication.Phases);
    }
    private static void InterruptedAttemptsRetainOriginalIdentityAtEachPhase()
    {
        foreach(var stage in new[]{"input","numeric","plant","post"})
        {
            var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();
            var originGeneration=session.ExecutionGeneration;
            session.Advance(.01,()=>{if(stage=="input")session.Reset();return new Dictionary<string,bool>();},
                ()=>new Dictionary<string,double>(),_=>{},_=>{if(stage=="numeric")session.Reset();},
                _=>{if(stage=="plant")session.Reset();},samplePostPlantInputs:()=>{
                    if(stage=="post")session.Reset();return new(new Dictionary<string,bool>(),new Dictionary<string,double>(),new Dictionary<string,string>());});
            Equal(ControllerPublicationKind.Reset,session.Publication.Kind);
            Equal(1,session.AbandonedAttempts.Count);var abandoned=session.AbandonedAttempts[0];
            Equal(originGeneration,abandoned.ExecutionGeneration);
            Equal(ControllerCompletion.Aborted,abandoned.Completion);
            Equal("VC_ATTEMPT_ABANDONED",abandoned.FailureCode!);
            if(stage=="plant"||stage=="post")True(abandoned.Phases.HasFlag(ControllerPhaseCompletion.PlantAdvanced));
            if(stage=="post")True(abandoned.Phases.HasFlag(ControllerPhaseCompletion.PostPlantSampled));
        }
    }
    private static void PartialOutputCommitFailureHasImagesAndSpecificPhase()
    {
        foreach(var stage in new[]{"NumericOutputCommit","TextOutputCommit"})
        {
            var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();var boolCommitted=false;
            try{session.Advance(.01,()=>new Dictionary<string,bool>(),()=>new Dictionary<string,double>(),_=>boolCommitted=true,
                _=>{if(stage=="NumericOutputCommit")throw new InvalidOperationException();},_=>{},
                commitTextOutputs:_=>{if(stage=="TextOutputCommit")throw new InvalidOperationException();});}catch(InvalidOperationException){}
            True(boolCommitted);Equal(stage,session.Publication.FailurePhase!);
            True(session.Publication.CommitProgress.HasFlag(ControllerOutputCommitProgress.Bool));
            False(session.Publication.Phases.HasFlag(ControllerPhaseCompletion.OutputsCommitted));
            True(session.Publication.PublishedValues is not null);
        }
    }
    private static void WatchdogProgressSurvivesMonitoringDisabled()
    {
        var runtime=Runtime(ObservationProgram(new LadderNetwork("label-net","Label",new("c1",LadderNodeKind.Contact,"enable",true),LabelInstruction:new("label","again")),
            new("jump-net","Jump",new("c2",LadderNodeKind.Contact,"enable",true),Jump:new("jump","again"))));
        runtime.MonitoringEnabled=false;runtime.Run();var snapshot=runtime.Scan(new Dictionary<string,bool>());
        Equal(ControllerCompletion.Aborted,snapshot.Completion);Equal(10000,snapshot.ExecutedNetworkCount);
        Equal("jump-net",snapshot.LastFullyObservedInstructionId!);True(snapshot.LastFullyObservedOrdinal>0);
    }
    private static void WatchdogProgressSurvivesObservationCapacityExceeded()
    {
        var networks=new List<LadderNetwork>();
        for(var i=0;i<2500;i++)
        {
            var children=new List<LadderNode>();
            for(var j=0;j<7;j++)children.Add(new($"c-{i}-{j}",LadderNodeKind.Contact,"enable",true));
            networks.Add(new($"n-{i}","Prefix",new($"series-{i}",LadderNodeKind.Series,Children:children),Coil:new($"out-{i}","output")));
        }
        networks.Add(new("label-net","Label",new("loop1",LadderNodeKind.Contact,"enable",true),LabelInstruction:new("label","again")));
        networks.Add(new("jump-net","Jump",new("loop2",LadderNodeKind.Contact,"enable",true),Jump:new("jump","again")));
        var runtime=Runtime(ObservationProgram(networks.ToArray()));True(runtime.MonitoringCapacityExceeded);
        runtime.Run();var snapshot=runtime.Scan(new Dictionary<string,bool>());
        Equal(10000,snapshot.ExecutedNetworkCount);Equal("jump-net",snapshot.LastFullyObservedInstructionId!);
        True(snapshot.Observations!.CapacityExceeded);True(snapshot.Observations.IsPartial);
    }
    private static void EachFailurePhasePreservesAvailableImages()
    {
        foreach(var stage in new[]{"BoolInputSample","NumericInputSample","TextInputSample","Program","BoolOutputCommit","NumericOutputCommit","TextOutputCommit","PlantAdvance","PostPlantSample"})
        {
            var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();
            var attempts=0;var completions=0;session.ScanAttempted+=_=>attempts++;session.ScanCompleted+=_=>completions++;
            void Fault(string current){if(stage==current)throw new InvalidOperationException(current);}
            try{session.Advance(.01,()=>{Fault("BoolInputSample");return Inputs(("enable",true));},
                ()=>{Fault("NumericInputSample");return new Dictionary<string,double>();},
                _=>Fault("BoolOutputCommit"),_=>Fault("NumericOutputCommit"),_=>Fault("PlantAdvance"),
                ()=>{Fault("TextInputSample");return stage=="Program"?new Dictionary<string,string>{["unknown"]="x"}:new Dictionary<string,string>();},
                _=>Fault("TextOutputCommit"),()=>{Fault("PostPlantSample");return new(new Dictionary<string,bool>(),new Dictionary<string,double>(),new Dictionary<string,string>());});}
            catch(Exception exception)when(exception is InvalidOperationException or ArgumentException){}
            Equal(stage,session.Publication.FailurePhase!);Equal(ControllerCompletion.PublicationFailed,session.Publication.Completion);
            Equal(1,attempts);Equal(0,completions);Equal(1,session.History.Count);
            if(stage=="BoolInputSample")True(session.Publication.ImageUnavailableReasons.ContainsKey("SampledInputs"));
            else True(session.Publication.SampledInputs is not null);
            if(stage.EndsWith("Commit")||stage is "PlantAdvance" or "PostPlantSample")
            {True(session.Publication.EffectiveInputs is not null);True(session.Publication.ProgramEndValues is not null);True(session.Publication.PublishedValues is not null);}
        }
    }
    private static void AttemptCallbackLifecycleDoesNotPublishOlderEnvelope()
    {
        foreach(var reset in new[]{true,false})
        {
            var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();
            var publications=new List<ControllerPublication>();var completions=0;
            session.PublicationPublished+=p=>publications.Add(p);session.ScanCompleted+=_=>completions++;
            session.ScanAttempted+=_=>{if(reset)session.Reset();else session.Stop();};
            session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{},_=>{});
            Equal(reset?ControllerPublicationKind.Reset:ControllerPublicationKind.Stop,session.Publication.Kind);
            Equal(1,publications.Count);Equal(0,completions);
            True(publications[0]==session.Publication);
        }
    }
    private static void FailureAttemptCallbackKeepsFailureEnvelopeIndependent()
    {
        var session=new VirtualControllerSession(Runtime(ObservationProgram()));session.Run();
        var publications=new List<ControllerPublication>();ControllerPublication? attempted=null;
        session.PublicationPublished+=p=>publications.Add(p);
        session.ScanAttempted+=p=>{attempted=p;session.Reset();};
        try{session.Advance(.01,()=>new Dictionary<string,bool>(),_=>throw new InvalidOperationException(),_=>{});}catch(InvalidOperationException){}
        Equal(ControllerPublicationKind.PublicationFailed,attempted!.Kind);
        Equal(ControllerPublicationKind.Reset,session.Publication.Kind);Equal(1,publications.Count);
        True(publications[0]==session.Publication);
    }
    private static void ForceCallbacksNeverSupersedeNewerForcePublication()
    {
        foreach(var mutation in new[]{"Set","Remove","Clear"})foreach(var at in new[]{"Commit","Attempt","Publication"})
        {
            var session=new VirtualControllerSession(Runtime(ObservationProgram(new LadderNetwork("n","Coil",ObservationEnable("c"),Coil:new("out","output")))));
            session.Run();session.SetInput("enable",true);
            if(mutation!="Set")session.SetBoolForce("output",false);
            var generation=session.ExecutionGeneration;var once=false;var snapshots=new List<VirtualControllerSnapshot>();var publications=new List<ControllerPublication>();
            void Change()
            {
                if(once)return;once=true;
                if(mutation=="Set")session.SetBoolForce("output",false);
                else if(mutation=="Remove")session.RemoveForce("output");else session.ClearForces();
            }
            if(at=="Attempt")session.ScanAttempted+=_=>Change();
            if(at=="Publication")session.PublicationPublished+=p=>{if(p.Kind==ControllerPublicationKind.ScanCompleted)Change();};
            session.PublicationPublished+=p=>publications.Add(p);session.SnapshotPublished+=s=>snapshots.Add(s);
            session.Advance(.01,()=>new Dictionary<string,bool>(),_=>{if(at=="Commit")Change();},_=>{});
            Equal(ControllerPublicationKind.ForceChanged,session.Publication.Kind);
            Equal(generation,session.ExecutionGeneration);
            True(snapshots.Count>0);Equal(session.Snapshot.Outputs["output"],snapshots[^1].Outputs["output"]);
            Equal(ControllerPublicationKind.ForceChanged,publications[^1].Kind);
            if(at=="Commit"){Equal(1,session.AbandonedAttempts.Count);True(session.AbandonedAttempts[0].CommitProgress.HasFlag(ControllerOutputCommitProgress.Bool));}
        }
    }
}
