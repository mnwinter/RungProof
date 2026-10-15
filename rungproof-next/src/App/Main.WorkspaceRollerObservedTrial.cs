using System;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    private FiniteRollerObservedTrial? _workspaceRollerObservedTrial;
    private FiniteRollerTrialEpoch? _workspaceRollerTrialEpoch;
    private long _workspaceRollerResetEpoch;
    private Vector3 _workspaceRollerMeasuredHome;
    private double _workspaceRollerSupportedLimit;
    private bool _workspaceRollerObservedTrialEnabled = true; // Native trace-equivalence fixture only.

    private FiniteRollerTrialEpoch RollerTrialEpoch() => new(
        WorkspaceTrialHash(LadderProgramJson.Save(_virtualProgram! with { WatchVariables = null })),
        WorkspaceTrialHash(JsonSerializer.Serialize(CreateWorkspaceDocument())), _workspaceRollerResetEpoch);

    private void ResetWorkspaceRollerObservedTrial(string reason)
    {
        _workspaceRollerResetEpoch++;
        _workspaceRollerObservedTrial = null;
        _workspaceRollerTrialEpoch = null;
        if (_sceneRuntime?.HasWorkspaceRollerPreset == true)
        {
            var geometry = SceneComposer.MeasureWorkspaceRoller(_sceneCompositionRoot!);
            _workspaceRollerMeasuredHome = geometry.PalletHome;
            _workspaceRollerSupportedLimit = geometry.HomeCenter - geometry.TerminalCenter;
            BeginWorkspaceRollerObservedTrial(reason);
        }
        PublishWorkspaceRollerObservedTrial();
    }

    private void BeginWorkspaceRollerObservedTrial(string reason)
    {
        if (_sceneRuntime?.HasWorkspaceRollerPreset != true) return;
        if (_virtualProgram is null || _simulatorShell?.LoadedDraftIdentityCurrent != true)
        {
            _workspaceRollerObservedTrial = null;
            PublishWorkspaceRollerObservedTrial();
            return;
        }
        var physical = _sceneRuntime.CaptureWorkspaceRollerPreset();
        _workspaceRollerTrialEpoch = RollerTrialEpoch();
        var trial = FiniteRollerObservedTrial.Begin(_workspaceRollerTrialEpoch, _workspaceRollerSupportedLimit);
        _workspaceRollerObservedTrial = physical.ClampedLoadTravelM < 1e-7
            && physical.DriveSurfaceTravelM < 1e-7 && physical.SpeedMps == 0 && !physical.BeamBlocked
            ? trial : trial.Invalidate("Reset to measured home before starting a new trial.");
        PublishWorkspaceRollerObservedTrial();
    }

    private void WorkspaceRollerTrialDraftChanged()
    {
        if (_workspaceRollerObservedTrial is not null && _simulatorShell?.LoadedDraftIdentityCurrent != true)
        {
            _workspaceRollerObservedTrial = _workspaceRollerObservedTrial.Invalidate("Draft differs from loaded program; previous observations are historical.");
            PublishWorkspaceRollerObservedTrial();
        }
    }

    private void StopWorkspaceRollerObservedTrial()
    {
        if (_workspaceRollerObservedTrial is null || _workspaceRollerObservedTrial.State == FiniteRollerTrialState.Unavailable) return;
        _workspaceRollerObservedTrial = _workspaceRollerObservedTrial.Stop();
        PublishWorkspaceRollerObservedTrial();
    }

    private void ObserveWorkspaceRollerAcceptedScan(VirtualControllerScanCompleted scan)
    {
        if (!_workspaceRollerObservedTrialEnabled || _workspaceRollerObservedTrial is null
            || _virtualProgram is null || _virtualController is null
            || _workspaceFiniteRollerDocument?.FiniteRollerPreset is not { } relation
            || _sceneRuntime?.HasWorkspaceRollerPreset != true) return;
        var declarations = PlcAggregates.Expand(_virtualProgram.Variables);
        var starts = declarations.Where(variable => variable.Binding == relation.StartPoint
            && variable.Type == PlcVariableType.Bool && variable.Role == PlcVariableRole.Input).ToArray();
        var beams = declarations.Where(variable => variable.Binding == relation.BeamPoint
            && variable.Type == PlcVariableType.Bool && variable.Role == PlcVariableRole.Input).ToArray();
        var runs = declarations.Where(variable => variable.Binding == relation.RunPoint
            && variable.Type == PlcVariableType.Bool && variable.Role == PlcVariableRole.Output).ToArray();
        bool Read(VirtualControllerSignalImage image, PlcVariable[] variables)
            => variables.Length == 1 && image.Bools.TryGetValue(variables[0].Name, out var value) && value;
        var valid = starts.Length == 1 && beams.Length == 1 && runs.Length == 1;
        valid = valid && scan.SampledInputs.Bools.ContainsKey(starts[0].Name)
            && scan.EffectiveInputs.Bools.ContainsKey(starts[0].Name)
            && scan.SampledInputs.Bools.ContainsKey(beams[0].Name)
            && scan.EffectiveInputs.Bools.ContainsKey(beams[0].Name)
            && scan.Outputs.Bools.ContainsKey(runs[0].Name);
        valid = valid && scan.Outputs.Bools[runs[0].Name] == (_sceneRuntime.Points[relation.RunPoint] is true);
        var physical = _sceneRuntime.CaptureWorkspaceRollerPreset();
        var expectedPose = _workspaceRollerMeasuredHome + Vector3.Left * (float)physical.ClampedLoadTravelM;
        var actualPose = _sceneCompositionRoot!.GetNode<Node3D>(relation.PalletId).Position;
        // The locked unit-scale layout has qualified continuous runner contact.
        // Check the actual rendered pose against that support interval each scan.
        var supported = actualPose.IsEqualApprox(expectedPose)
            && physical.Center <= physical.HomeCenter + 1e-5
            && physical.Center >= physical.TerminalCenter - 1e-5;
        _workspaceRollerObservedTrial = _workspaceRollerObservedTrial.Observe(new(
            _workspaceRollerTrialEpoch!, scan.ScanNumber, scan.SimulatedTime.TotalSeconds,
            _virtualController.ScanPeriod.TotalSeconds,
            Read(scan.SampledInputs, starts), Read(scan.EffectiveInputs, starts),
            Read(scan.SampledInputs, beams), Read(scan.EffectiveInputs, beams),
            _sceneRuntime.Points[relation.RunPoint] is true,
            new(physical.SpeedMps, physical.DriveSurfaceTravelM, physical.ClampedLoadTravelM, physical.AtTerminal),
            physical.BeamBlocked, supported, _simulatorShell!.LoadedDraftIdentityCurrent,
            _virtualController.Snapshot.Forces.Count > 0, valid,
            _virtualController.Snapshot.State == VirtualControllerState.Running));
        PublishWorkspaceRollerObservedTrial();
    }

    private void PublishWorkspaceRollerObservedTrial()
    {
        var visible = _sceneRuntime?.HasWorkspaceRollerPreset == true;
        var trial = _workspaceRollerObservedTrial;
        var physical = visible ? _sceneRuntime!.CaptureWorkspaceRollerPreset() : null;
        var identity = trial is null ? "No current accepted trial."
            : $"{(_virtualController?.Snapshot.State == VirtualControllerState.Running ? "Accepted" : "STOPPED · historical accepted")} · scan {trial.LastScanNumber ?? 0} · reset {_workspaceRollerResetEpoch}\nProgram {trial.Epoch.ProgramIdentity[..8]} · layout {trial.Epoch.LayoutIdentity[..8]}";
        var motion = physical is null ? ""
            : $"Command {(_sceneRuntime!.Points[_workspaceFiniteRollerDocument!.FiniteRollerPreset!.RunPoint] is true ? "ON" : "OFF")} · drive {physical.SpeedMps:F3} m/s\nCurrent beam {(physical.BeamBlocked ? "blocked" : "clear")} · pallet center {physical.Center:F3} m\n"
                + (trial?.BeamTriggerScanNumber is { } trigger ? $"Trigger recorded at scan {trigger}." : "No triggering beam event recorded.")
                + (physical.AtTerminal ? " Load clamped at model boundary; this is not a controller stop." : "");
        _simulatorShell?.SetWorkspaceRollerObservedTrial(visible, trial?.State.ToString() ?? "Unavailable",
            trial?.Detail ?? "Validate your own current draft, then Reset and Run from measured home.", identity, motion);
    }
}
