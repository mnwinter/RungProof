using System;
using System.Linq;
using Godot;
using RungProof.Next.Workspace;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;
public partial class Main
{
    private WorkspaceDocument? _workspaceFiniteBeltDocument;
    private void CreateFiniteBeltWorkspace()
    {
        if(!GuardWorkspaceReplacement(CreateFiniteBeltWorkspace))return;
        var rollback=CaptureWorkspaceReplacementRollback();
        try
        {
            var document=WorkspaceFiniteBeltPolicy.Create();
            WorkspaceRepository.ValidateDocument(document,_candidateCatalog!,_sceneCatalog!);
            AddMigratedScene(document.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:document);
            var blank=new LadderEditorDocument();blank.ResetProject("finite-belt-workspace","Main",TimeSpan.FromMilliseconds(20));
            blank.SourceSceneId=document.SourceSceneId;
            blank.AddTag("start",PlcVariableRole.Input,document.FiniteBeltPreset!.StartPoint);
            blank.AddTag("beam_blocked",PlcVariableRole.Input,document.FiniteBeltPreset.BeamPoint);
            blank.AddTag("belt_run",PlcVariableRole.Output,document.FiniteBeltPreset.RunPoint);
            _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(blank),null,true);
            _simulatorShell.SetIntegratedSceneIdentity();
            _simulatorShell.SetWorkspaceStatus("Finite belt workspace created · stopped · write your own ladder · select belt to move the complete cell");
            RefreshWorkspaceDirty();
        }
        catch(Exception error){rollback();_simulatorShell!.SetWorkspaceStatus("Preset creation rejected · "+error.Message,true);}
    }
    private void RegisterFiniteBeltWorkspace(WorkspaceDocument document)
    {
        _workspaceFiniteBeltDocument=document;
        foreach(var placement in document.Placements)
            _workspaceNodes[placement.InstanceId]=(placement.AssetId,_sceneCompositionRoot!.GetNode<Node3D>(placement.InstanceId));
        _signalLinks.AddRange(document.SignalLinks??[]);
    }
    private bool TryTransformFiniteBeltWorkspace(string id,Vector3 position,Vector3 rotation,Vector3 scale)
    {
        if(_workspaceFiniteBeltDocument is not { } document)return false;
        try
        {
            if(id!=document.FiniteBeltPreset!.BeltId||rotation!=Vector3.Zero||scale!=Vector3.One)
                throw new InvalidOperationException("Select belt to translate the complete cell; independent pose, rotation and scale are unsupported.");
            var home=document.Placements.Single(p=>p.InstanceId==id);
            var delta=position-Vector(home.Position);
            var next=document with{Placements=document.Placements.Select(p=>p with{Position=Array(Vector(p.Position)+delta)}).ToArray()};
            WorkspaceRepository.ValidateDocument(next,_candidateCatalog!,_sceneCatalog!);
            RecordUndo();RebuildTemplateWorkspace(next);
            _simulatorShell!.SetWorkspaceStatus("Complete cell translated · actual draft preserved · stopped");
        }
        catch(Exception error){_simulatorShell!.SetWorkspaceStatus("Preset transform rejected · "+error.Message,true);RefreshWorkspaceUi();}
        return true;
    }
    private bool RejectFiniteBeltGraphEdit()
    {
        if(_workspaceFiniteBeltDocument is null)return false;
        _simulatorShell?.SetWorkspaceStatus("Finite belt relation is bounded: translate its belt or create/open a different workspace; independent asset edits are unsupported",true);
        return true;
    }
    private SignalMappingRuntimeStatus? FiniteBeltMappingStatus(string instanceId,string signalId)
    {
        if(_workspaceFiniteBeltDocument?.FiniteBeltPreset is not { } p)return null;
        if(instanceId==p.BeltId&&signalId=="run_command")return new(SignalMappingRuntimeState.LiveInput,"LIVE INPUT","Accepted PLC BOOL drives the single-clock finite belt.");
        if(instanceId==p.SensorId&&signalId=="beam_blocked")return new(SignalMappingRuntimeState.LiveFeedback,"LIVE FEEDBACK","Actual carton intersects the optical line; next accepted scan samples this PC BOOL.");
        if(instanceId==p.OperatorId&&signalId=="pressed")return new(SignalMappingRuntimeState.LiveFeedback,"LIVE INPUT","Actual momentary pushbutton action supplies a PC BOOL pulse.");
        return null;
    }
}
