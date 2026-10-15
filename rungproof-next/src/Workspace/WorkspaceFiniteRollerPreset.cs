using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RungProof.Next.Scenes;

namespace RungProof.Next.Workspace;

public sealed record WorkspaceFiniteRollerPreset(int Version,string RollerId,string PalletId,string SensorId,
    string OperatorId,string StartPoint,string RunPoint,string BeamPoint);

public static class WorkspaceFiniteRollerPolicy
{
    public const double TowerOutwardOffsetM=.12;
    public const string RuntimeType="workspaceFiniteRoller";
    public const string DisplayName="Workspace · Finite Pallet Roller Cell";
    public const string SourceId="scene-1-conveyor-stop";
    public const string RollerAsset="material-handling.pallet-roller-conveyor.1000x4000.v1";
    public const string PalletAsset="loads.pallet.gma-48x40.v1";
    public const string SensorAsset="sensing.photoelectric.through-beam.v1";
    public const string OperatorAsset="controls.operator-station.single-pushbutton.v1";
    public static WorkspaceFiniteRollerPreset Relation()=>new(1,"roller_workspace_conveyor","roller_workspace_pallet","roller_workspace_sensor",
        "roller_workspace_operator","roller_workspace_start","roller_workspace_run","roller_workspace_beam_blocked");
    public static WorkspaceDocument Create()
    {
        var p=Relation();
        return new(8,SourceId,[
            new(p.RollerId,RollerAsset,[0,-.04,0],[0,0,0],[1,1,1]),
            new(p.PalletId,PalletAsset,[1.2625,1.053,0],[0,0,0],[1,1,1]),
            new(p.SensorId,SensorAsset,[-1.20,.398,0],[0,0,0],[1,1,1]),
            new(p.OperatorId,OperatorAsset,[2.9,0,2.2],[0,0,0],[1,1,1])],
            SignalLinks:[new("roller_command",p.RollerId,"run_command",p.RunPoint),
                new("sensor_feedback",p.SensorId,"beam_blocked",p.BeamPoint),
                new("operator_input",p.OperatorId,"pressed",p.StartPoint)],FiniteRollerPreset:p);
    }
    public static void Validate(WorkspaceDocument d)
    {
        if(d.FiniteRollerPreset is not { } p)return;
        if(d.Version!=8||d.SourceSceneId!=SourceId||d.TemplateOverride is not null||d.FiniteBeltPreset is not null||p!=Relation())
            throw new InvalidOperationException("Unsupported finite-roller relation/source/point ownership.");
        if(d.Placements.Any(v=>v.Position is not {Length:3}||v.Rotation is not {Length:3}||v.Scale is not {Length:3}
            ||v.Position.Concat(v.Rotation).Concat(v.Scale).Any(n=>!double.IsFinite(n))))
            throw new InvalidOperationException("Finite roller poses must be finite three-component vectors.");
        if(d.Placements.Select(v=>v.InstanceId).Distinct(StringComparer.Ordinal).Count()!=d.Placements.Count)
            throw new InvalidOperationException("Duplicate finite roller instance IDs.");
        var reference=Create();
        if(d.Placements.Count!=4 || (d.ConnectorLinks?.Count??0)!=0 || (d.Groups?.Count??0)!=0)
            throw new InvalidOperationException("Finite-roller preset requires its four related placements; additional graphs are unsupported.");
        var roller=d.Placements.SingleOrDefault(v=>v.InstanceId==p.RollerId)
            ??throw new InvalidOperationException("Finite-roller placement is missing.");
        var dx=roller.Position[0];var dz=roller.Position[2];
        if(Math.Abs(dx)>10||Math.Abs(dz)>10)throw new InvalidOperationException("Preset translation exceeds the bounded 10m workspace.");
        foreach(var original in reference.Placements)
        {
            var actual=d.Placements.SingleOrDefault(v=>v.InstanceId==original.InstanceId);
            if(actual is null||actual.AssetId!=original.AssetId||actual.Rotation.Any(v=>Math.Abs(v)>1e-6)
                ||actual.Scale.Where((v,i)=>Math.Abs(v-original.Scale[i])>1e-6).Any()
                ||Math.Abs(actual.Position[0]-original.Position[0]-dx)>1e-5
                ||Math.Abs(actual.Position[1]-original.Position[1])>1e-5
                ||Math.Abs(actual.Position[2]-original.Position[2]-dz)>1e-5)
                throw new InvalidOperationException("Move the complete finite-roller preset through its roller conveyor; independent pose/rotation/scale is unsupported.");
        }
        if(d.SignalLinks is null||d.SignalLinks.Count!=3||!reference.SignalLinks!.All(d.SignalLinks.Contains))
            throw new InvalidOperationException("Finite-roller point/signal relations cannot be silently remapped.");
    }
    public static SceneDefinition ReplaceSource(SceneDefinition source,WorkspaceDocument d)
    {
        Validate(d);var p=d.FiniteRollerPreset!;
        var equipment=d.Placements.Select(v=>new SceneEquipment(v.InstanceId,"workspacePresetAsset",v.AssetId,
            v.Position,v.Rotation,v.Scale,JsonSerializer.SerializeToElement(new{assetId=v.AssetId,
                sceneControls=v.InstanceId==p.OperatorId?new[]{new{nodeName="KIN_pushbutton",label="Momentary start",
                    action="roller-workspace-press",feedbackTravelM=0}}:[]}))).ToArray();
        object Point(string name,string owner,string role)=>new{name,type="BOOL",owner,role,initial=false};
        var simulation=JsonSerializer.SerializeToElement(new{type="workspaceFiniteRoller",preset=p,
            points=new[]{Point(p.StartPoint,"PC","input"),Point(p.RunPoint,"PLC","output"),Point(p.BeamPoint,"PC","input")},
            actions=new[]{new{id="roller-workspace-press",label="Press momentary start",type="pulse",point=p.StartPoint}},
            pointBindings=new[]{new{point=p.RunPoint,equipmentId=p.RollerId,mode="running"},
                new{point=p.BeamPoint,equipmentId=p.SensorId,mode="photoeye"},
                new{point=p.StartPoint,equipmentId=p.OperatorId,mode="workspaceMomentary"}}});
        return source with{ Name="Workspace · Finite Pallet Roller Cell",Description="One finite empty pallet; accepted ramp/coast and actual solid-wood beam feedback. Write your own controller.",
            Equipment=equipment,Simulation=simulation,Verification=JsonSerializer.SerializeToElement(new{}),TemplateOverride=null,PlcTestProfile=null};
    }
}
