using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Workspace;

namespace RungProof.Next.Scenes;
public partial class SceneSimulationRuntime
{
    private WorkspaceBeltPlantModel? _workspaceBeltPlant;
    private WorkspaceFiniteBeltPreset? _workspaceBeltRelation;
    private ConveyorController? _workspaceBeltController;
    private Vector3 _workspaceCartonHome;
    private Node3D? _workspaceOperatorButton;
    private Transform3D? _workspaceOperatorHome;
    public bool HasWorkspaceBeltPreset=>RuntimeType=="workspaceFiniteBelt";
    public (double Center,bool BeamBlocked,bool AtTerminal) CaptureWorkspaceBeltPreset()
        =>_workspaceBeltPlant is { } p?(p.Center,p.BeamBlocked,p.AtTerminal)
            :throw new InvalidOperationException("Finite workspace plant has not initialized.");
    private void FreezeWorkspaceBeltAdapter()
    {
        if(!HasWorkspaceBeltPreset)return;
        _sceneRoot.GetNode<Node3D>(WorkspaceFiniteBeltPolicy.Relation().BeltId)
            .GetNode<ConveyorController>("WorkspaceRuntimeAdapter").SetPhysicsProcess(false);
    }
    private void ResetWorkspaceBeltPreset()
    {
        if(!HasWorkspaceBeltPreset)return;
        _workspaceBeltRelation=_definition.GetProperty("preset").Deserialize<WorkspaceFiniteBeltPreset>()
            ??throw new InvalidOperationException("Finite workspace relation unavailable.");
        var p=_workspaceBeltRelation;
        var geometry=SceneComposer.MeasureWorkspaceBelt(_sceneRoot);
        _workspaceBeltPlant=new(geometry.Belt.Size.X,geometry.Carton.Size.X,
            geometry.Beam.GetCenter().X-geometry.Belt.Position.X,
            geometry.Carton.GetCenter().X-geometry.Belt.Position.X);
        GD.Print($"WORKSPACE_BELT_DATUM measuredCenter={_workspaceBeltPlant.InitialCenter:R} nominalCenter={geometry.Carton.Size.X/2+.05:R} beltOrigin={geometry.Belt.Position.X:R} beam={_workspaceBeltPlant.BeamPosition:R} loadLength={geometry.Carton.Size.X:R}");
        _workspaceCartonHome=_sceneRoot.GetNode<Node3D>(p.CartonId).Position;
        _workspaceOperatorButton=_sceneRoot.GetNode<Node3D>(p.OperatorId).FindChild("KIN_pushbutton",true,false) as Node3D
            ??throw new InvalidOperationException("Physical momentary operator head unavailable.");
        _workspaceOperatorHome??=_workspaceOperatorButton.Transform;
        _workspaceOperatorButton.Transform=_workspaceOperatorHome.Value;
        if(Math.Abs(geometry.Carton.GetCenter().X-geometry.Belt.Position.X-_workspaceBeltPlant.Center)>.015)
            throw new InvalidOperationException("Actual initial carton does not match fully supported model home.");
        _workspaceBeltController=_sceneRoot.GetNode<Node3D>(p.BeltId).GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
        _workspaceBeltController.ResetPlantTravel();_workspaceBeltController.RunCommand=false;
        FreezeWorkspaceBeltAdapter();ProjectWorkspaceBeam();
    }
    private void ProjectWorkspaceBeltCommittedCommand()
    {
        // Main Stop commits cleared outputs without another plant tick. A false
        // accepted drive command must update speed immediately, with zero travel.
        if(HasWorkspaceBeltPreset&&_workspaceBeltRelation is { } relation
            &&!AsBool(_points.GetValueOrDefault(relation.RunPoint)))
            StopWorkspaceBeltPreset();
    }
    private void StopWorkspaceBeltPreset()
    {
        if(!HasWorkspaceBeltPreset||_workspaceBeltController is null)return;
        // This hook does not modify PLC points; the existing session Stop clears outputs.
        // Travel, drum transforms and UV home are untouched until Reset.
        _workspaceBeltController.RunCommand=false;
        _workspaceBeltController.ApplyPlantTravel(0,0);
        FreezeWorkspaceBeltAdapter();
    }
    private void AdvanceWorkspaceBeltPreset(double seconds)
    {
        if(_workspaceBeltPlant is not { } plant||_workspaceBeltRelation is not { } p)
            throw new InvalidOperationException("Finite workspace not ready for an accepted tick.");
        var run=AsBool(_points.GetValueOrDefault(p.RunPoint));
        plant.Step(seconds,run);
        var carton=_sceneRoot.GetNode<Node3D>(p.CartonId);
        carton.Position=_workspaceCartonHome+new Vector3((float)(plant.Center-plant.InitialCenter),0,0);
        _workspaceBeltController!.RunCommand=run;
        _workspaceBeltController.ApplyPlantTravel(run?(float)(seconds*WorkspaceBeltPlantModel.SpeedMps):0,
            run?(float)WorkspaceBeltPlantModel.SpeedMps:0);
        ProjectWorkspaceBeam();StateChanged?.Invoke();
    }
    private void ProjectWorkspaceBeam()
    {
        var p=_workspaceBeltRelation!;var plant=_workspaceBeltPlant!;
        // This readback observes the actual rendered carton interval against
        // the retained optical line, not the PLC command or goal state.
        var carton=_sceneRoot.GetNode<Node3D>(p.CartonId);
        var sensor=_sceneRoot.GetNode<Node3D>(p.SensorId);
        Aabb Bounds(Node3D n,string pattern)=>n.FindChildren(pattern,"MeshInstance3D",true,false)
            .OfType<MeshInstance3D>().Select(m=>_sceneRoot.GlobalTransform.AffineInverse()*m.GlobalTransform*m.GetAabb())
            .Aggregate((a,b)=>a.Merge(b));
        var c=Bounds(carton,"*");var beam=Bounds(sensor,"KIN_beam*");
        var line=beam.GetCenter().X;
        var blocked=c.Position.X<=line+1e-6f&&c.End.X>=line-1e-6f
            &&c.Position.Y<=beam.GetCenter().Y&&c.End.Y>=beam.GetCenter().Y
            &&c.Position.Z<=beam.End.Z&&c.End.Z>=beam.Position.Z;
        if(blocked!=plant.BeamBlocked)throw new InvalidOperationException($"Actual beam and finite model occupancy diverge: actual={blocked} model={plant.BeamBlocked} cartonMinX={c.Position.X:R} cartonMaxX={c.End.X:R} beamX={line:R} modelCenter={plant.Center:R} modelTrailing={plant.TrailingEdge:R} modelLeading={plant.LeadingEdge:R} modelBeam={plant.BeamPosition:R} home={plant.InitialCenter:R}");
        SetExistingPoint(p.BeamPoint,blocked);
        foreach(var mesh in sensor.FindChildren("KIN_beam*","MeshInstance3D",true,false).OfType<MeshInstance3D>())mesh.Visible=!blocked;
    }
    private void ProjectWorkspaceMomentary(Node3D equipment,bool pressed)
    {
        if(!HasWorkspaceBeltPreset||_workspaceOperatorButton is null||_workspaceOperatorHome is null
            ||equipment.Name.ToString()!=_workspaceBeltRelation!.OperatorId)
            throw new InvalidOperationException("Unsupported physical momentary binding.");
        _workspaceOperatorButton.Transform=pressed?_workspaceOperatorHome.Value.TranslatedLocal(Vector3.Forward*.018f)
            :_workspaceOperatorHome.Value;
    }
}
