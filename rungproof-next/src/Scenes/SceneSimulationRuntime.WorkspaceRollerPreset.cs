using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Workspace;
namespace RungProof.Next.Scenes;

public sealed record WorkspaceRollerPlantSnapshot(double Center,double SpeedMps,double DriveSurfaceTravelM,
    double ClampedLoadTravelM,bool BeamBlocked,bool AtTerminal,double HomeCenter,double TerminalCenter);

public partial class SceneSimulationRuntime
{
    private FiniteRollerTransport? _workspaceRollerTransport;
    private FiniteRollerTransportState _workspaceRollerState;
    private WorkspaceFiniteRollerPreset? _workspaceRollerRelation;
    private WorkspaceRollerGeometry? _workspaceRollerGeometry;
    private RollerConveyorController? _workspaceRollerController;
    private Node3D? _workspaceRollerButton;
    private Transform3D? _workspaceRollerButtonHome;
    private Vector3 _workspaceRollerButtonPressDirection;
    private bool _workspaceRollerBeam;
    private readonly Dictionary<MeshInstance3D,Vector3[]> _workspaceRollerWood=[];
    public bool HasWorkspaceRollerPreset=>RuntimeType==WorkspaceFiniteRollerPolicy.RuntimeType;
    public WorkspaceRollerPlantSnapshot CaptureWorkspaceRollerPreset()
    {
        var g=_workspaceRollerGeometry??throw new InvalidOperationException("Finite roller cell is not initialized.");
        var s=_workspaceRollerState;
        return new(g.HomeCenter-s.ClampedLoadTravelM,s.SpeedMps,s.DriveSurfaceTravelM,s.ClampedLoadTravelM,_workspaceRollerBeam,s.AtTerminal,g.HomeCenter,g.TerminalCenter);
    }
    private void FreezeWorkspaceRollerAdapter()
    {
        if(!HasWorkspaceRollerPreset)return;
        _sceneRoot.GetNode<Node3D>(WorkspaceFiniteRollerPolicy.Relation().RollerId)
            .GetNode<RollerConveyorController>("WorkspaceRuntimeAdapter").SetPhysicsProcess(false);
    }
    private void ResetWorkspaceRollerPreset()
    {
        if(!HasWorkspaceRollerPreset)return;
        _workspaceRollerRelation=_definition.GetProperty("preset").Deserialize<WorkspaceFiniteRollerPreset>()
            ??throw new InvalidOperationException("Finite roller relation unavailable.");
        var p=_workspaceRollerRelation;
        _workspaceRollerController=_sceneRoot.GetNode<Node3D>(p.RollerId).GetNode<RollerConveyorController>("WorkspaceRuntimeAdapter");
        // Restore captured roller transforms before measuring radius/crown, never a guessed Euler home.
        _workspaceRollerController.ResetAcceptedTravel();
        // Measurement deliberately validates HOME. A subsequent Reset must first
        // restore this composition's measured (including whole-cell translation) home.
        // The first Reset has no cache and measures the imported policy pose.
        if(_workspaceRollerGeometry is { } previousGeometry)
            _sceneRoot.GetNode<Node3D>(p.PalletId).Position=previousGeometry.PalletHome;
        _workspaceRollerGeometry=SceneComposer.MeasureWorkspaceRoller(_sceneRoot);
        var g=_workspaceRollerGeometry;
        _workspaceRollerTransport=new(g.HomeCenter-g.TerminalCenter);
        _workspaceRollerState=_workspaceRollerTransport.InitialState;
        _workspaceRollerWood.Clear();
        foreach(var mesh in _sceneRoot.GetNode<Node3D>(p.PalletId).FindChildren("PALLET_*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
            .Where(m=>!m.Name.ToString().Contains("nail",StringComparison.OrdinalIgnoreCase)))
            _workspaceRollerWood[mesh]=mesh.Mesh.GetFaces();
        if(_workspaceRollerWood.Count==0)throw new InvalidOperationException("Pallet has no actual wood triangles.");
        var operatorStation=_sceneRoot.GetNode<Node3D>(p.OperatorId);
        _workspaceRollerButton=operatorStation.FindChild("KIN_pushbutton",true,false) as Node3D
            ??throw new InvalidOperationException("Finite roller physical start head is missing.");
        _workspaceRollerButtonPressDirection=SceneControlInteractor.PressDirectionInParent(_workspaceRollerButton,operatorStation);
        _workspaceRollerButtonHome??=_workspaceRollerButton.Transform;
        _workspaceRollerButton.Transform=_workspaceRollerButtonHome.Value;
        FreezeWorkspaceRollerAdapter();ProjectWorkspaceRollerBeam();
    }
    private void StopWorkspaceRollerPreset()
    {
        if(!HasWorkspaceRollerPreset||_workspaceRollerTransport is null||_workspaceRollerController is null)return;
        _workspaceRollerState=_workspaceRollerTransport.Stop(_workspaceRollerState);
        _workspaceRollerController.RunCommand=false;
        _workspaceRollerController.ApplyAcceptedTravel(0,0); // preserve exact phase and load pose
        FreezeWorkspaceRollerAdapter();
    }
    private void AdvanceWorkspaceRollerPreset(double seconds)
    {
        if(_workspaceRollerTransport is null||_workspaceRollerRelation is null||_workspaceRollerGeometry is null||_workspaceRollerController is null)
            throw new InvalidOperationException("Finite roller cell is not ready for an accepted scan.");
        var run=AsBool(_points.GetValueOrDefault(_workspaceRollerRelation.RunPoint));
        var previous=_workspaceRollerState;
        _workspaceRollerState=_workspaceRollerTransport.Advance(previous,seconds,run);
        var state=_workspaceRollerState;
        _sceneRoot.GetNode<Node3D>(_workspaceRollerRelation.PalletId).Position=
            _workspaceRollerGeometry.PalletHome+Vector3.Left*(float)state.ClampedLoadTravelM;
        _workspaceRollerController.RunCommand=run;
        // Powered rollers continue when a wrong program drives against the finite load end.
        _workspaceRollerController.ApplyAcceptedTravel(state.DriveSurfaceTravelM-previous.DriveSurfaceTravelM,state.SpeedMps);
        ProjectWorkspaceRollerBeam();StateChanged?.Invoke();
    }
    private void ProjectWorkspaceRollerBeam()
    {
        var g=_workspaceRollerGeometry!;
        var globalFrom=_sceneRoot.GlobalTransform*g.RayFrom;var globalTo=_sceneRoot.GlobalTransform*g.RayTo;
        var blocked=false;
        foreach(var (mesh,triangles) in _workspaceRollerWood)
        {
            var inverse=mesh.GlobalTransform.AffineInverse();var from=inverse*globalFrom;var to=inverse*globalTo;
            for(var i=0;i+2<triangles.Length;i+=3)
            {
                if(Geometry3D.SegmentIntersectsTriangle(from,to,triangles[i],triangles[i+1],triangles[i+2]).VariantType==Variant.Type.Nil)continue;
                blocked=true;break;
            }
            if(blocked)break;
        }
        _workspaceRollerBeam=blocked;
        SetExistingPoint(_workspaceRollerRelation!.BeamPoint,blocked);
        foreach(var witness in _sceneRoot.GetNode<Node3D>(_workspaceRollerRelation.SensorId).FindChildren("KIN_beam_dash*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
            witness.Visible=!blocked;
    }
    private void ProjectWorkspaceRollerMomentary(Node3D equipment,bool pressed)
    {
        if(_workspaceRollerButton is null||_workspaceRollerButtonHome is null||equipment.Name.ToString()!=_workspaceRollerRelation!.OperatorId)
            throw new InvalidOperationException("Unsupported finite roller operator binding.");
        _workspaceRollerButton.Transform=pressed?_workspaceRollerButtonHome.Value.Translated(_workspaceRollerButtonPressDirection*.018f):_workspaceRollerButtonHome.Value;
    }
}
