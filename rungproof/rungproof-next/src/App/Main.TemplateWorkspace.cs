using System;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    private WorkspaceTemplateOverride? _templateOverride;
    private string? _workspaceSavedLadderJson;
    private WorkspacePlacement[] TemplateWorkspaceRows()
    {
        if(_activeSceneDefinition is null||!WorkspaceTemplatePolicy.Supports(_activeSceneDefinition.Id)
            ||_sceneCompositionRoot?.GetNodeOrNull<Node3D>("ControlInstallation") is not { } support)return [];
        return [new(WorkspaceTemplatePolicy.SelectionId,"Template · operator bench and mounted controls",
            Array(support.Position),Array(support.RotationDegrees),Array(Vector3.One))];
    }
    private bool SelectTemplateAssembly(string? id)
    {
        if(id!=WorkspaceTemplatePolicy.SelectionId||TemplateWorkspaceRows().Length==0)return false;
        _selectedPlacementIds.Clear();_selectedPlacementIds.Add(id);_selectedPlacementId=id;
        _transformGizmo?.Detach();RefreshWorkspaceUi();return true;
    }
    private bool TransformTemplateAssembly(string id,Vector3 position,Vector3 rotation,Vector3 scale)
    {
        if(id!=WorkspaceTemplatePolicy.SelectionId)return false;
        try
        {
            if(_activeSceneDefinition is null||!WorkspaceTemplatePolicy.Supports(_activeSceneDefinition.Id))
                throw new InvalidOperationException("This template is locked.");
            if(position.Y!=0||rotation.X!=0||rotation.Z!=0||scale!=Vector3.One)
                throw new InvalidOperationException("Edit X/Z position and yaw only; support height, tilt and scale are locked.");
            var value=new WorkspaceTemplateOverride(1,"ControlInstallation",WorkspaceTemplatePolicy.Fingerprint(_activeSceneDefinition),Array(position),rotation.Y);
            WorkspaceTemplatePolicy.Validate(_activeSceneDefinition,value);
            var support=_sceneCompositionRoot!.GetNode<Node3D>("ControlInstallation");
            if(support.Position.IsEqualApprox(position)&&support.RotationDegrees.IsEqualApprox(rotation))return true;
            RecordUndo();
            RebuildTemplateWorkspace(CreateWorkspaceDocument() with { Version=6,TemplateOverride=value });
            _simulatorShell?.SetWorkspaceStatus("Template assembly updated · simulation rebuilt and stopped · original I/O retained");
        }
        catch(Exception error)
        {
            _simulatorShell?.SetWorkspaceStatus("Template edit rejected · "+error.Message,true);RefreshWorkspaceUi();
        }
        return true;
    }
    private void RebuildTemplateWorkspace(WorkspaceDocument snapshot)
    {
        var undo=_undoHistory.ToArray();var redo=_redoHistory.ToArray();
        var saved=_workspaceSavedSnapshotJson;
        var savedLadder=_workspaceSavedLadderJson;
        var restoreEditor=_simulatorShell!.CaptureIntegratedEditorRollback();
        StopActiveController();
        AddMigratedScene(snapshot.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,snapshot.TemplateOverride,
            snapshot.FiniteBeltPreset is null?null:snapshot,
            snapshot.FiniteRollerPreset is null?null:snapshot,
            snapshot.DriveConnection is null?null:snapshot);
        restoreEditor();
        foreach(var placement in snapshot.Placements)
            if(snapshot.FiniteBeltPreset is null && snapshot.FiniteRollerPreset is null)PlaceAsset(_candidateCatalog!.Assets.First(a=>a.Id==placement.AssetId),placement,false);
        _connectorLinks.AddRange(snapshot.ConnectorLinks??[]);
        if(snapshot.FiniteBeltPreset is null && snapshot.FiniteRollerPreset is null)_signalLinks.AddRange(snapshot.SignalLinks??[]);
        _workspaceGroups.AddRange(snapshot.Groups??[]);
        if(snapshot.DriveConnection is not null)RegisterWorkspaceDrive(snapshot);
        _undoHistory.AddRange(undo);_redoHistory.AddRange(redo);
        _workspaceSavedSnapshotJson=saved;
        _workspaceSavedLadderJson=savedLadder;
        SelectTemplateAssembly(WorkspaceTemplatePolicy.SelectionId);
        RefreshWorkspaceDirty();
    }
}
