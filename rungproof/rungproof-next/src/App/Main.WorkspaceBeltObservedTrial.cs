using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private FiniteBeltObservedTrial? _workspaceObservedTrial;
    private long _workspaceTrialResetEpoch;
    private double _workspaceTrialInitialCenter;
    private Aabb _workspaceTrialHomeCarton;
    private Aabb _workspaceTrialBeltBounds;
    private Vector3 _workspaceTrialCartonHome;
    private long _workspaceTrialAcceptedScan;
    private FiniteBeltTrialEpoch? _workspaceTrialEpoch;
    private VirtualControllerSession? _workspaceTrialSession;
    private Action<VirtualControllerScanCompleted>? _workspaceTrialScanHandler;
    private long _workspaceTrialSubscriptionEpoch;
    private void AttachWorkspaceTrialObserver(VirtualControllerSession session)
    {
        DetachWorkspaceTrialObserver();
        var token=++_workspaceTrialSubscriptionEpoch;
        _workspaceTrialSession=session;
        _workspaceTrialScanHandler=scan=>
        {
            if(token!=_workspaceTrialSubscriptionEpoch||!ReferenceEquals(session,_virtualController))return;
            ObserveWorkspaceBeltAcceptedScan(scan);
            ObserveWorkspaceRollerAcceptedScan(scan);
        };
        session.ScanCompleted+=_workspaceTrialScanHandler;
    }
    private void DetachWorkspaceTrialObserver()
    {
        ++_workspaceTrialSubscriptionEpoch;
        if(_workspaceTrialSession is not null&&_workspaceTrialScanHandler is not null)_workspaceTrialSession.ScanCompleted-=_workspaceTrialScanHandler;
        _workspaceTrialSession=null;_workspaceTrialScanHandler=null;
    }
    private bool _workspaceObservedTrialEnabled=true; // Native equivalence fixture can disable observation only.
    private string WorkspaceTrialHash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private FiniteBeltTrialEpoch WorkspaceTrialEpoch()=>new(WorkspaceTrialHash(LadderProgramJson.Save(_virtualProgram! with{WatchVariables=null})),WorkspaceTrialHash(JsonSerializer.Serialize(CreateWorkspaceDocument())),_workspaceTrialResetEpoch);
    private void ResetWorkspaceObservedTrial(string reason)
    {
        _workspaceTrialResetEpoch++;_workspaceTrialAcceptedScan=0;
        if(_sceneRuntime?.HasWorkspaceBeltPreset!=true||_workspaceFiniteBeltDocument?.FiniteBeltPreset is not { } p)
        { _workspaceObservedTrial=null;PublishWorkspaceObservedTrial();return; }
        var bounds=RungProof.Next.Scenes.SceneComposer.MeasureWorkspaceBelt(_sceneCompositionRoot!);
        _workspaceTrialHomeCarton=bounds.Carton;_workspaceTrialBeltBounds=bounds.Belt;
        _workspaceTrialCartonHome=_sceneCompositionRoot!.GetNode<Node3D>(p.CartonId).Position;
        _workspaceTrialInitialCenter=_sceneRuntime.CaptureWorkspaceBeltPreset().Center;
        _workspaceObservedTrial=null;
        BeginWorkspaceObservedTrial(reason);
    }
    private void BeginWorkspaceObservedTrial(string reason)
    {
        if(_sceneRuntime?.HasWorkspaceBeltPreset!=true)return;
        if(_virtualProgram is null||_simulatorShell?.LoadedDraftIdentityCurrent!=true)
        { _workspaceObservedTrial=null;PublishWorkspaceObservedTrial();return; }
        var physical=_sceneRuntime.CaptureWorkspaceBeltPreset();
        _workspaceTrialEpoch=WorkspaceTrialEpoch();
        var trial=FiniteBeltObservedTrial.Begin(_workspaceTrialEpoch,_workspaceTrialInitialCenter);
        _workspaceObservedTrial=Math.Abs(physical.Center-_workspaceTrialInitialCenter)<1e-7&&!physical.BeamBlocked?trial:trial.Invalidate("Reset to measured home before starting a new trial.");
        PublishWorkspaceObservedTrial();
    }
    private void WorkspaceTrialDraftChanged()
    {
        if(_workspaceObservedTrial is not null&&_simulatorShell?.LoadedDraftIdentityCurrent!=true)
        { _workspaceObservedTrial=_workspaceObservedTrial.Invalidate("Draft differs from loaded program; previous observations are historical.");PublishWorkspaceObservedTrial(); }
    }
    private void StopWorkspaceObservedTrial()
    {
        if(_workspaceObservedTrial is null||_workspaceObservedTrial.State==FiniteBeltTrialState.Unavailable)return;
        _workspaceObservedTrial=_workspaceObservedTrial.Stop();PublishWorkspaceObservedTrial();
    }
    private void ObserveWorkspaceBeltAcceptedScan(VirtualControllerScanCompleted scan)
    {
        if(!_workspaceObservedTrialEnabled||_workspaceObservedTrial is null||_virtualProgram is null||_virtualController is null
            ||_workspaceFiniteBeltDocument?.FiniteBeltPreset is not { } p||_sceneRuntime?.HasWorkspaceBeltPreset!=true)return;
        var declarations=PlcAggregates.Expand(_virtualProgram.Variables);
        var starts=declarations.Where(v=>v.Binding==p.StartPoint&&v.Type==PlcVariableType.Bool&&v.Role==PlcVariableRole.Input).ToArray();
        var beams=declarations.Where(v=>v.Binding==p.BeamPoint&&v.Type==PlcVariableType.Bool&&v.Role==PlcVariableRole.Input).ToArray();
        var runs=declarations.Where(v=>v.Binding==p.RunPoint&&v.Type==PlcVariableType.Bool&&v.Role==PlcVariableRole.Output).ToArray();
        var valid=starts.Length==1&&beams.Length==1&&runs.Length==1;
        bool Read(VirtualControllerSignalImage image,PlcVariable[] variables)=>variables.Length==1&&image.Bools.TryGetValue(variables[0].Name,out var value)&&value;
        valid=valid&&scan.SampledInputs.Bools.ContainsKey(starts[0].Name)&&scan.EffectiveInputs.Bools.ContainsKey(starts[0].Name)
            &&scan.SampledInputs.Bools.ContainsKey(beams[0].Name)&&scan.EffectiveInputs.Bools.ContainsKey(beams[0].Name)&&scan.Outputs.Bools.ContainsKey(runs[0].Name);
        valid=valid&&scan.Outputs.Bools[runs[0].Name]==(_sceneRuntime.Points[p.RunPoint] is true);
        var physical=_sceneRuntime.CaptureWorkspaceBeltPreset();
        var actual=_workspaceTrialHomeCarton with{Position=_workspaceTrialHomeCarton.Position+_sceneCompositionRoot!.GetNode<Node3D>(p.CartonId).Position-_workspaceTrialCartonHome};
        var supported=actual.Position.X>=_workspaceTrialBeltBounds.Position.X-.002f&&actual.End.X<=_workspaceTrialBeltBounds.End.X+.002f
            &&actual.Position.Z>=_workspaceTrialBeltBounds.Position.Z-.002f&&actual.End.Z<=_workspaceTrialBeltBounds.End.Z+.002f
            &&Math.Abs(actual.Position.Y-_workspaceTrialBeltBounds.End.Y)<.015;
        _workspaceObservedTrial=_workspaceObservedTrial.Observe(new(_workspaceTrialEpoch!,scan.ScanNumber,scan.SimulatedTime.TotalSeconds,_virtualController.ScanPeriod.TotalSeconds,
            Read(scan.SampledInputs,starts),Read(scan.EffectiveInputs,starts),Read(scan.SampledInputs,beams),Read(scan.EffectiveInputs,beams),
            _sceneRuntime.Points[p.RunPoint] is true,physical.Center,physical.BeamBlocked,physical.AtTerminal,_workspaceTrialInitialCenter,supported,
            _simulatorShell!.LoadedDraftIdentityCurrent,_virtualController.Snapshot.Forces.Count>0,valid,_virtualController.Snapshot.State==VirtualControllerState.Running));
        _workspaceTrialAcceptedScan=scan.ScanNumber;PublishWorkspaceObservedTrial();
    }
    private void PublishWorkspaceObservedTrial()
    {
        var visible=_sceneRuntime?.HasWorkspaceBeltPreset==true;
        var trial=_workspaceObservedTrial;
        _simulatorShell?.SetWorkspaceObservedTrial(visible,trial?.State.ToString()??"Unavailable",trial?.Detail??"Validate your own current draft, then Reset and Run from measured home.",
            trial is null?"No current accepted trial.":$"{(_virtualController?.Snapshot.State==VirtualControllerState.Running?"Accepted":"STOPPED · historical accepted")} · scan {_workspaceTrialAcceptedScan} · reset {_workspaceTrialResetEpoch}\nProgram {trial.Epoch.ProgramIdentity[..8]} · layout {trial.Epoch.LayoutIdentity[..8]}");
    }
}
