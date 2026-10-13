using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    private WorkspaceDriveConnection? _workspaceDriveRelation;
    private WorkspaceDrivePlant? _workspaceDrivePlant;
    private ConveyorController? _workspaceDriveAdapter;
    private Node3D? _workspaceDriveNode;
    private SceneSimulationRuntime? _workspaceDriveRuntime;
    private SceneDefinition? _workspaceDriveSource;
    private bool _workspaceDriveFeedbackAvailable;
    private bool _workspaceDriveReconciling;

    private bool WorkspaceDriveIdentityMatches => _workspaceDriveRelation is { } relation
        && _workspaceDrivePlant is not null && _sceneRuntime == _workspaceDriveRuntime
        && _workspaceDriveNode is not null && GodotObject.IsInstanceValid(_workspaceDriveNode)
        && !_workspaceDriveNode.IsQueuedForDeletion() && _workspaceDriveAdapter is not null
        && GodotObject.IsInstanceValid(_workspaceDriveAdapter)
        && !_workspaceDriveAdapter.IsQueuedForDeletion()
        && _workspaceNodes.TryGetValue(relation.InstanceId, out var entry)
        && entry.Node == _workspaceDriveNode && entry.AssetId == WorkspaceDriveConnectionPolicy.BeltAsset;

    private bool WorkspaceDriveScanEligible => _workspaceDriveRelation is null
        || WorkspaceDriveIdentityMatches && _workspaceDriveFeedbackAvailable
            && _simulatorShell?.IsExternalMode == false;

    // Run eligibility can change between UI refreshes (for example, a target
    // queues deletion). Reconcile BEFORE the session samples or accepts a scan,
    // rather than discovering stale ownership after committing learner outputs.
    private bool PrepareWorkspaceDriveAcceptedScan()
    {
        if (_workspaceDriveRelation is null) return true;
        ReconcileWorkspaceDriveMappings();
        return WorkspaceDriveScanEligible;
    }

    private void RegisterWorkspaceDrive(WorkspaceDocument document)
    {
        var source = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(s => s.Id == document.SourceSceneId));
        WorkspaceDriveConnectionPolicy.Validate(document, source, _candidateCatalog!);
        var relation = document.DriveConnection ?? throw new InvalidOperationException("Drive relation is missing.");
        var node = _workspaceNodes[relation.InstanceId].Node;
        var adapter = node.GetNodeOrNull<ConveyorController>("WorkspaceRuntimeAdapter")
            ?? throw new InvalidOperationException("Actual placed belt drive controller is unavailable.");
        _workspaceDriveRelation = relation;
        _workspaceDriveSource = source;
        _workspaceDriveRuntime = _sceneRuntime;
        _workspaceDriveNode = node;
        _workspaceDriveAdapter = adapter;
        _workspaceDrivePlant = new WorkspaceDrivePlant();
        adapter.RunCommand = false;
        adapter.SetPhysicsProcess(false);
        adapter.ResetPlantTravel();
        _workspaceDriveFeedbackAvailable = false;
        ReconcileWorkspaceDriveMappings();
    }

    private void ClearWorkspaceDriveRegistration()
    {
        StopWorkspaceDrive();
        _workspaceDriveRelation = null; _workspaceDrivePlant = null;
        _workspaceDriveAdapter = null; _workspaceDriveNode = null;
        _workspaceDriveRuntime = null; _workspaceDriveSource = null;
        _workspaceDriveFeedbackAvailable = false;
    }

    private void PublishWorkspaceDriveMeasurement(bool available)
    {
        if (_workspaceDriveRelation is not { } relation || _workspaceDriveRuntime is null) return;
        _workspaceDriveRuntime.PublishWorkspaceDriveFeedback(relation.RunningPoint, relation.ActualSpeedPoint,
            available ? _workspaceDrivePlant!.Running : null,
            available ? _workspaceDrivePlant!.ActualSpeedMps : null);
    }

    private void ReconcileWorkspaceDriveMappings()
    {
        if (_workspaceDriveRelation is null || _workspaceDriveReconciling) return;
        _workspaceDriveReconciling = true;
        try
        {
            var available = false;
            if (WorkspaceDriveIdentityMatches && _simulatorShell?.IsExternalMode == false)
            {
                try
                {
                    var document = CreateWorkspaceDocument();
                    WorkspaceDriveConnectionPolicy.Validate(document, _workspaceDriveSource!, _candidateCatalog!);
                    available = WorkspaceDriveConnectionPolicy.Eligibility(document).FeedbackMapped;
                }
                catch (InvalidOperationException) { available = false; }
            }
            if (!available && _workspaceDriveFeedbackAvailable)
            {
                StopActiveController();
                _virtualController?.ClearHistory();
                _scanInspectionEpoch++;
                _simulatorShell?.SetWorkspaceStatus("DRIVE FEEDBACK UNAVAILABLE · stopped · restore the connection and Run the current draft", true);
            }
            var changed = available != _workspaceDriveFeedbackAvailable;
            _workspaceDriveFeedbackAvailable = available;
            if (changed || !available) PublishWorkspaceDriveMeasurement(available);
            if (_workspaceDriveAdapter is { } adapter && GodotObject.IsInstanceValid(adapter)
                && !adapter.IsQueuedForDeletion()) adapter.SetPhysicsProcess(false);
        }
        finally { _workspaceDriveReconciling = false; }
    }

    private bool ValidateWorkspaceDriveRun()
    {
        if (_workspaceDriveRelation is null) return true;
        ReconcileWorkspaceDriveMappings();
        if (!WorkspaceDriveScanEligible)
        {
            StopActiveController();
            _simulatorShell?.SetWorkspaceStatus("RUN BLOCKED · drive controller or measured feedback is unavailable", true);
            return false;
        }
        try
        {
            var source = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(s => s.Id == _currentSceneId));
            WorkspaceDriveConnectionPolicy.Validate(CreateWorkspaceDocument(), source, _candidateCatalog!);
            return true;
        }
        catch (InvalidOperationException error)
        {
            StopActiveController();
            _workspaceDriveFeedbackAvailable = false;
            PublishWorkspaceDriveMeasurement(false);
            _simulatorShell?.SetWorkspaceStatus("RUN BLOCKED · " + error.Message, true);
            return false;
        }
    }

    // Both normal scheduling and paused Step use this exact accepted callback.
    // The original source plant remains intact and advances once before the
    // independently added unloaded drive. Renderer observation never ticks it.
    private void AdvanceAcceptedScenePlant(double seconds)
    {
        // A commit callback may retire a target after the pre-scan guard. Fail
        // before advancing even the original scene plant; never partially tick
        // Scene1 and only then discover that the additional drive is unavailable.
        if (_workspaceDriveRelation is not null && !PrepareWorkspaceDriveAcceptedScan())
            throw new InvalidOperationException("Accepted drive tick lost its connection identity or feedback before plant integration.");
        var repeatCycleBefore = BeforeRepeatCyclePlant();
        _sceneRuntime?.AdvanceSimulation(seconds);
        AfterRepeatCyclePlant(repeatCycleBefore);
        if (_workspaceDriveRelation is not { } relation) return;
        if (!WorkspaceDriveScanEligible) throw new InvalidOperationException("Accepted drive tick lost its connection identity or feedback.");
        var commandMapped = WorkspaceDriveConnectionPolicy.Eligibility(CreateWorkspaceDocument()).CommandMapped;
        var command = commandMapped && _sceneRuntime!.Points.GetValueOrDefault(relation.RunPoint) is true;
        var distance = _workspaceDrivePlant!.Step(seconds, command);
        _workspaceDriveAdapter!.RunCommand = command;
        _workspaceDriveAdapter.SetPhysicsProcess(false);
        _workspaceDriveAdapter.ApplyPlantTravel(distance, _workspaceDrivePlant.ActualSpeedMps);
        PublishWorkspaceDriveMeasurement(true);
    }

    private void StopWorkspaceDrive()
    {
        if (_workspaceDrivePlant is null) return;
        _workspaceDrivePlant.Stop();
        if (_workspaceDriveAdapter is { } adapter && GodotObject.IsInstanceValid(adapter))
        { adapter.RunCommand = false; adapter.SetPhysicsProcess(false); adapter.ApplyPlantTravel(0, 0); }
        if (!_workspaceDriveReconciling && _workspaceDriveFeedbackAvailable) PublishWorkspaceDriveMeasurement(true);
    }

    private void ResetWorkspaceDrive()
    {
        _workspaceDrivePlant?.Reset();
        if (_workspaceDriveAdapter is { } adapter && GodotObject.IsInstanceValid(adapter))
        { adapter.RunCommand = false; adapter.SetPhysicsProcess(false); adapter.ResetPlantTravel(); }
        ReconcileWorkspaceDriveMappings();
        if (_workspaceDriveFeedbackAvailable) PublishWorkspaceDriveMeasurement(true);
    }

    private SignalMappingRuntimeStatus? WorkspaceDriveMappingStatus(string instanceId, string signalId)
    {
        if (_workspaceDriveRelation?.InstanceId != instanceId) return null;
        if (!WorkspaceDriveIdentityMatches || !_workspaceDriveFeedbackAvailable)
            return new(SignalMappingRuntimeState.AuthoredOnly, "UNAVAILABLE", "Drive measured feedback or current controller identity is unavailable; execution is stopped.");
        return signalId == "run_command"
            ? new(SignalMappingRuntimeState.LiveInput, "LIVE INPUT", "Applied once per accepted offline scan; UI refresh does not drive motion.")
            : new(SignalMappingRuntimeState.LiveFeedback, "LIVE FEEDBACK", "Measured after accepted drive integration; sampled on the following scan.");
    }
}
