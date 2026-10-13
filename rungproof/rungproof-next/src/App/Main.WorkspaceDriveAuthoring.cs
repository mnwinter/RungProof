using System;
using System.Linq;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;
public partial class Main
{
    private bool CanConnectWorkspaceDrive(string? instanceId) =>
        _currentSceneId == WorkspaceDriveConnectionPolicy.SourceId
        && _sceneRuntime is not null && _simulatorShell?.IsExternalMode == false
        && _workspaceDriveRelation is null && _templateOverride is null
        && _workspaceFiniteBeltDocument is null && _workspaceFiniteRollerDocument is null
        && instanceId is not null && _workspaceNodes.TryGetValue(instanceId, out var placement)
        && placement.AssetId == WorkspaceDriveConnectionPolicy.BeltAsset
        && placement.Node.RotationDegrees == Godot.Vector3.Zero && placement.Node.Scale == Godot.Vector3.One
        && IsFinite(placement.Node.Position)
        && !_signalLinks.Any(link => link.InstanceId == instanceId)
        && !_workspaceNodes.Keys.Any(id => id != instanceId && WorkspaceDriveConnectionPolicy.NormalizeInstanceId(id) == WorkspaceDriveConnectionPolicy.NormalizeInstanceId(instanceId));

    private void ConnectSelectedWorkspaceDrive(string instanceId)
    {
        if (!CanConnectWorkspaceDrive(instanceId)) return;
        try
        {
            var before = CreateWorkspaceDocument();
            var source = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(s => s.Id == before.SourceSceneId));
            var relation = WorkspaceDriveConnectionPolicy.CreateRelation(source, instanceId);
            // Validate the complete proposal before replacing the live workspace.
            var links = (before.SignalLinks ?? []).Concat(new WorkspaceSignalLink[] {
                new("drive_run_" + instanceId, instanceId, "run_command", relation.RunPoint),
                new("drive_running_" + instanceId, instanceId, "running", relation.RunningPoint),
                new("drive_speed_" + instanceId, instanceId, "actual_speed", relation.ActualSpeedPoint)
            }).ToArray();
            var connected = before with { Version = 9, DriveConnection = relation, SignalLinks = links };
            WorkspaceRepository.ValidateDocument(connected, _candidateCatalog!, _sceneCatalog!);
            ApplyWorkspaceDriveRegistrationTransaction(connected, instanceId);
            _simulatorShell!.SetWorkspaceStatus("Belt drive connected · offline · stopped · Run your current draft");
        }
        catch (InvalidOperationException error)
        { _simulatorShell?.SetWorkspaceStatus("Connection rejected · " + error.Message, true); }
    }
}
