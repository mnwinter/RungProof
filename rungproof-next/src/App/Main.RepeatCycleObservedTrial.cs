using System;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Lessons;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private const string RepeatCycleSceneId = "lab-4-03-repeat-cycle-counter";
    private RepeatCycleObservedTrial? _repeatCycleTrial;
    private RepeatCycleTrialIdentity? _repeatCycleIdentity;
    private RepeatCycleTrialBindings? _repeatCycleBindings;
    private VirtualControllerSession? _repeatCycleSession;
    private Action<VirtualControllerScanCompleted>? _repeatCycleHandler;
    private long _repeatCycleGeneration, _repeatCycleEpoch;
    private bool _repeatCyclePendingReset, _repeatCycleObserverEnabled = true;
    private sealed record RepeatCyclePair(VirtualControllerSession Session, SceneSimulationRuntime Runtime,
        long Generation, long Scan, RepeatCyclePlantImage Before, RepeatCyclePlantImage After,
        VirtualControllerSignalImage PostInputs, VirtualControllerSignalImage ActualOutputs);
    private RepeatCyclePair? _repeatCyclePair;
    // One-use authorization captured only after an actual measured Reset.
    // It names the exact current draft, physical layout and runtime being reset.
    private sealed record RepeatCycleResetHandoff(SceneSimulationRuntime Runtime,
        VirtualControllerSession? PreviousController,long Epoch,string ProgramFingerprint,
        string LayoutFingerprint,RepeatCyclePlantImage Plant);
    private RepeatCycleResetHandoff? _repeatCycleResetHandoff;
    private string? RepeatCycleCurrentDraftFingerprint()
    {
        if(_simulatorShell is null || !_simulatorShell.TryBuildCurrentLadderProgram(out var draft,out _))return null;
        return LadderCompiler.Compile(draft).IsValid
            ? WorkspaceTrialHash(LadderProgramJson.Save(draft with {WatchVariables=null})) : null;
    }
    private bool RepeatCycleResetHandoffMatches(RepeatCycleResetHandoff handoff,string? fingerprint)
        => RepeatCycleVisible && ReferenceEquals(handoff.Runtime,_sceneRuntime) && handoff.Epoch==_repeatCycleEpoch
            && fingerprint is not null && handoff.ProgramFingerprint==fingerprint
            && handoff.LayoutFingerprint==RepeatCycleLayoutFingerprint()
            && _sceneRuntime?.CaptureRepeatCyclePlantState()==handoff.Plant
            && _virtualController?.Snapshot.Forces.Count is null or 0;
    private RepeatCycleResetHandoff? CaptureRepeatCycleResetHandoff(LadderProgram program)
    {
        var handoff=_repeatCycleResetHandoff;
        var fingerprint=WorkspaceTrialHash(LadderProgramJson.Save(program with {WatchVariables=null}));
        return _repeatCyclePendingReset && handoff is not null
            && ReferenceEquals(handoff.PreviousController,_virtualController)
            && RepeatCycleResetHandoffMatches(handoff,fingerprint)
            && RepeatCycleCurrentDraftFingerprint()==fingerprint ? handoff : null;
    }
    private void RestoreRepeatCycleResetHandoff(RepeatCycleResetHandoff? handoff)
    {
        if(handoff is null || !RepeatCycleResetHandoffMatches(handoff,RepeatCycleCurrentDraftFingerprint()))return;
        _repeatCycleResetHandoff=handoff;_repeatCyclePendingReset=true;
    }
    private bool RepeatCycleVisible => _currentSceneId == RepeatCycleSceneId && _simulatorShell?.IsExternalMode != true;
    private void RetireRepeatCycleTrial(string reason)
    {
        ++_repeatCycleGeneration;
        if (_repeatCycleSession is not null && _repeatCycleHandler is not null)
            _repeatCycleSession.ScanCompleted -= _repeatCycleHandler;
        _repeatCycleSession = null; _repeatCycleHandler = null; _repeatCyclePair = null;
        _repeatCyclePendingReset = false; _repeatCycleResetHandoff=null;
        _repeatCycleTrial?.Invalidate(reason);
        PublishRepeatCycleTrial();
    }
    private void ResetRepeatCycleTrial()
    {
        RetireRepeatCycleTrial("Reset replaced the previous accepted epoch.");
        ++_repeatCycleEpoch; _repeatCycleTrial = null; _repeatCycleIdentity = null;
        var plant=_sceneRuntime?.CaptureRepeatCyclePlantState();
        var fingerprint=RepeatCycleCurrentDraftFingerprint();
        _repeatCyclePendingReset = RepeatCycleVisible && _sceneRuntime is not null && fingerprint is not null
            && plant is {Phase:RepeatCyclePlantModel.CyclePhase.Idle,Home:true,Busy:false,Done:false,HomeFraction:1,SpindleRadians:0};
        if(_repeatCyclePendingReset)
            _repeatCycleResetHandoff=new(_sceneRuntime!,_virtualController,_repeatCycleEpoch,fingerprint!,RepeatCycleLayoutFingerprint(),plant!);
        BindRepeatCycleTrial();
    }
    private void BindRepeatCycleTrial()
    {
        if (!_repeatCyclePendingReset || !RepeatCycleVisible || _virtualController is null || _virtualProgram is null
            || !ScanInspectionCurrentDraftMatches()) { PublishRepeatCycleTrial(); return; }
        if(_repeatCycleResetHandoff is not { } handoff
            || !RepeatCycleResetHandoffMatches(handoff,RepeatCycleCurrentDraftFingerprint()))
        {
            RetireRepeatCycleTrial("Measured Reset identity changed before current draft attachment; Reset is required.");return;
        }
        _repeatCyclePendingReset = false; _repeatCycleResetHandoff=null;
        var declarations = PlcAggregates.Expand(_virtualProgram.Variables);
        string Resolve(string point, PlcVariableType type, PlcVariableRole role)
        {
            var declared = _activeSceneDefinition!.Simulation.GetProperty("points").EnumerateArray()
                .Where(p => p.GetProperty("name").GetString() == point).ToArray();
            if (declared.Length != 1 || declared[0].GetProperty("owner").GetString() != (role == PlcVariableRole.Input ? "PC" : "PLC")
                || declared[0].GetProperty("type").GetString() != type.ToString().ToUpperInvariant()) return "";
            var matches = declarations.Where(v => v.Binding == point).ToArray();
            return matches.Length == 1 && matches[0].Type == type && matches[0].Role == role ? matches[0].Name : "";
        }
        var b = new RepeatCycleTrialBindings(Resolve("cycle_request",PlcVariableType.Bool,PlcVariableRole.Input),
            Resolve("machine_enabled",PlcVariableType.Bool,PlcVariableRole.Input),Resolve("machine_home",PlcVariableType.Bool,PlcVariableRole.Input),
            Resolve("machine_busy",PlcVariableType.Bool,PlcVariableRole.Input),Resolve("cycle_done",PlcVariableType.Bool,PlcVariableRole.Input),
            Resolve("head_position",PlcVariableType.Real,PlcVariableRole.Input),Resolve("cycle_active",PlcVariableType.Bool,PlcVariableRole.Output),
            Resolve("cycle_complete",PlcVariableType.Bool,PlcVariableRole.Output),Resolve("batch_count",PlcVariableType.DInt,PlcVariableRole.Output));
        _repeatCycleBindings = b;
        _repeatCycleIdentity = new(_repeatCycleEpoch, RepeatCycleSceneId,"offline-repeat-cycle-v1",
            WorkspaceTrialHash(LadderProgramJson.Save(_virtualProgram with { WatchVariables=null })),
            WorkspaceTrialHash(JsonSerializer.Serialize(b)), RepeatCycleLayoutFingerprint());
        _repeatCycleTrial = new(_repeatCycleIdentity,b);
        var aliases = new[] { b.Request,b.Enabled,b.Home,b.Busy,b.Done,b.HeadPosition,b.Active,b.Complete,b.Count };
        var plant = _sceneRuntime?.CaptureRepeatCyclePlantState();
        if (_virtualProgram.Networks.Count == 0 && (_virtualProgram.Blocks is null || _virtualProgram.Blocks.Count == 0))
            _repeatCycleTrial.Invalidate("Blank learner draft has no executable trial; author your draft and Reset.");
        else if (aliases.Any(string.IsNullOrWhiteSpace) || aliases.Distinct(StringComparer.Ordinal).Count()!=9)
            _repeatCycleTrial.Invalidate("Exact unique scene-owner/type bindings are unavailable; validate the draft and Reset.");
        else if (plant is null || plant.Phase!=RepeatCyclePlantModel.CyclePhase.Idle || !plant.Home || plant.Done || plant.Busy || Math.Abs(plant.HomeFraction-1)>1e-8)
            _repeatCycleTrial.Invalidate("Measured Reset home is unavailable.");
        var session = _virtualController; var generation = ++_repeatCycleGeneration;
        _repeatCycleSession=session;
        _repeatCycleHandler=scan=>
        {
            if (generation!=_repeatCycleGeneration || !ReferenceEquals(session,_virtualController)) return;
            ObserveRepeatCycleScan(scan);
        };
        session.ScanCompleted+=_repeatCycleHandler;
        PublishRepeatCycleTrial();
    }
    private void RepeatCycleDraftChanged()
    {
        if (_repeatCyclePendingReset && _repeatCycleResetHandoff is { } pending
            && !RepeatCycleResetHandoffMatches(pending,RepeatCycleCurrentDraftFingerprint()))
            RetireRepeatCycleTrial("Draft or physical identity changed after Reset; Reset is required.");
        if (_repeatCycleTrial is not null && !ScanInspectionCurrentDraftMatches())
            RetireRepeatCycleTrial("Current draft differs or is invalid; accepted evidence is historical. Reset is required.");
    }
    private void StopRepeatCycleTrial()
    {
        _repeatCyclePendingReset=false;_repeatCycleResetHandoff=null;
        _repeatCyclePair=null; _repeatCycleTrial?.Stop(); PublishRepeatCycleTrial();
    }
    // Called immediately around the existing single plant advance, independent of history recording.
    private RepeatCyclePlantImage? BeforeRepeatCyclePlant()
    {
        _repeatCyclePair=null;
        return _repeatCycleObserverEnabled && RepeatCycleVisible ? _sceneRuntime?.CaptureRepeatCyclePlantState() : null;
    }
    private void AfterRepeatCyclePlant(RepeatCyclePlantImage? before)
    {
        if (before is null || _sceneRuntime is null || _virtualController is null || _virtualProgram is null
            || _sceneRuntime.CaptureRepeatCyclePlantState() is not { } after) return;
        var bools=new System.Collections.Generic.Dictionary<string,bool>(StringComparer.Ordinal);
        var numbers=new System.Collections.Generic.Dictionary<string,double>(StringComparer.Ordinal);
        foreach(var variable in PlcAggregates.Expand(_virtualProgram.Variables).Where(v=>v.Role==PlcVariableRole.Output))
        {
            if (!_sceneRuntime.Points.TryGetValue(variable.Binding,out var value)) continue;
            if(variable.Type==PlcVariableType.Bool && value is bool flag) bools[variable.Name]=flag;
            else if(variable.Type is PlcVariableType.DInt or PlcVariableType.Int or PlcVariableType.Real
                && value is double or int or long or float) numbers[variable.Name]=Convert.ToDouble(value);
        }
        var actualOutputs=new VirtualControllerSignalImage(bools,numbers,new System.Collections.Generic.Dictionary<string,string>());        _repeatCyclePair=new(_virtualController,_sceneRuntime,_repeatCycleGeneration,_virtualController.Snapshot.ScanNumber,before,after,
            new(SampleVirtualControllerInputs(),SampleVirtualControllerNumericInputs(),SampleVirtualControllerTextInputs()),actualOutputs);
    }
    private void ObserveRepeatCycleScan(VirtualControllerScanCompleted scan)
    {
        if (!_repeatCycleObserverEnabled || _repeatCycleTrial is null || _repeatCycleIdentity is null || _repeatCycleBindings is null) return;
        var pair=_repeatCyclePair; _repeatCyclePair=null;
        if(pair is null || pair.Generation!=_repeatCycleGeneration || pair.Scan!=scan.ScanNumber
            || !ReferenceEquals(pair.Session,_virtualController) || !ReferenceEquals(pair.Runtime,_sceneRuntime))
            _repeatCycleTrial.Invalidate("Missing or stale accepted pre/post plant pair; Reset is required.");
        else
        {
            var b=_repeatCycleBindings;
            bool BoolAgrees(string alias)=>scan.Outputs.Bools.TryGetValue(alias,out var expected)
                && pair.ActualOutputs.Bools.TryGetValue(alias,out var actual) && expected==actual;
            bool valid=BoolAgrees(b.Active)&&BoolAgrees(b.Complete)&&scan.Outputs.Numbers.TryGetValue(b.Count,out var count)
                && pair.ActualOutputs.Numbers.TryGetValue(b.Count,out var actualCount)&&count==actualCount;
            var identity=_repeatCycleIdentity with { LayoutFingerprint=RepeatCycleLayoutFingerprint() };
            _repeatCycleTrial.Observe(new(identity,scan.ScanNumber,scan.SimulatedTime,_virtualController!.ScanPeriod,
                scan.SampledInputs,scan.EffectiveInputs,scan.Outputs,pair.PostInputs,pair.Before,pair.After,
                ScanInspectionCurrentDraftMatches(),valid,_virtualController.Snapshot.Forces.Count>0));
        }
        PublishRepeatCycleTrial();
    }
    // Fixed authored equipment roots carry layout. KIN descendants carry modeled
    // motion and deliberately never participate in this execution identity.
    private string RepeatCycleLayoutFingerprint()
    {
        static string Canonical(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject().OrderBy(p=>p.Name,StringComparer.Ordinal)
                .Select(p=>JsonSerializer.Serialize(p.Name)+":"+Canonical(p.Value))) + "}",
            JsonValueKind.Array => "[" + string.Join(",",value.EnumerateArray().Select(Canonical)) + "]",
            JsonValueKind.Undefined => "undefined",
            _ => value.GetRawText()
        };
        static double[] Components(Transform3D pose) => new double[]
        {
            pose.Origin.X,pose.Origin.Y,pose.Origin.Z,
            pose.Basis.X.X,pose.Basis.X.Y,pose.Basis.X.Z,
            pose.Basis.Y.X,pose.Basis.Y.Y,pose.Basis.Y.Z,
            pose.Basis.Z.X,pose.Basis.Z.Y,pose.Basis.Z.Z
        };
        static object FixedRoot(Node3D? root)
        {
            // Explicit missing identity makes deletion retire a formerly valid
            // trial without reading a freed Godot object or fabricating a pose.
            if(root is null || !GodotObject.IsInstanceValid(root) || root.IsQueuedForDeletion())
                return new { Missing=true, Transform=(double[]?)null };
            return new { Missing=false, Transform=(double[]?)Components(root.GlobalTransform) };
        }
        var composition=_sceneCompositionRoot;
        var rootAvailable=composition is not null && GodotObject.IsInstanceValid(composition) && !composition.IsQueuedForDeletion();
        var scene=_activeSceneDefinition;
        var equipment=scene?.Equipment.OrderBy(e=>e.Id,StringComparer.Ordinal).Select(e=>new
        {
            e.Id,e.Type,e.Position,e.Rotation,e.Scale,Config=Canonical(e.Config),
            Root=FixedRoot(rootAvailable?composition!.GetNodeOrNull<Node3D>(SafeNodeName(e.Id)):null)
        }).ToArray();
        return WorkspaceTrialHash(JsonSerializer.Serialize(new
        {
            SceneId=scene?.Id,SourceVersion=scene?.Version,SourceFile=scene?.Migration.SourceFile,
            Workspace=_currentSceneId is null?null:CreateWorkspaceDocument(),
            Composition=FixedRoot(composition),Equipment=equipment
        }));
    }
    private void PublishRepeatCycleTrial()
    {
        var view=_repeatCycleTrial?.View;
        var identityMatches=_repeatCycleIdentity is not null && ScanInspectionCurrentDraftMatches()
            && _repeatCycleIdentity.LayoutFingerprint==RepeatCycleLayoutFingerprint();
        _simulatorShell?.SetRepeatCycleObservedTrial(RepeatCycleVisible, view,
            _repeatCycleIdentity is null ? "No accepted identity; validate the draft and Reset." :
            $"{(view?.Historical==true || !identityMatches?"Historical identity":"Accepted identity matches")} · Reset {_repeatCycleIdentity.Epoch} · program {_repeatCycleIdentity.ProgramFingerprint[..8]} · layout {_repeatCycleIdentity.LayoutFingerprint[..8]}");
    }
}
