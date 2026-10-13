using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RungProof.Next.Scenes;

namespace RungProof.Next.Workspace;

public sealed record WorkspaceFiniteBeltPreset(int Version,string BeltId,string CartonId,string SensorId,
    string OperatorId,string StartPoint,string RunPoint,string BeamPoint);

public static class WorkspaceFiniteBeltPolicy
{
    public const string SourceId="scene-1-conveyor-stop";
    public const string BeltAsset="material-handling.belt-conveyor.600x6000.v1";
    public const string CartonAsset="loads.carton.corrugated-rsc.v1";
    public const string SensorAsset="sensing.photoelectric.through-beam.v1";
    public const string OperatorAsset="controls.operator-station.single-pushbutton.v1";
    public static WorkspaceFiniteBeltPreset Relation()=>new(1,"workspace_belt","workspace_carton","workspace_sensor",
        "workspace_operator","workspace_start","workspace_run","workspace_beam_blocked");
    public static WorkspaceDocument Create()
    {
        var p=Relation();
        return new(7,SourceId,[
            new(p.BeltId,BeltAsset,[0,-.04,0],[0,0,0],[1,1,1]),
            new(p.CartonId,CartonAsset,[-2.67375,1.015,0],[0,0,0],[.65,.65,.65]),
            new(p.SensorId,SensorAsset,[0,.25,0],[0,0,0],[1,1,1]),
            new(p.OperatorId,OperatorAsset,[-2,0,2],[0,0,0],[1,1,1])],
            SignalLinks:[new("belt_command",p.BeltId,"run_command",p.RunPoint),
                new("sensor_feedback",p.SensorId,"beam_blocked",p.BeamPoint),
                new("operator_input",p.OperatorId,"pressed",p.StartPoint)],FiniteBeltPreset:p);
    }
    public static void Validate(WorkspaceDocument d)
    {
        if(d.FiniteBeltPreset is not { } p)return;
        if(d.Version!=7||d.SourceSceneId!=SourceId||d.TemplateOverride is not null||p!=Relation())
            throw new InvalidOperationException("Unsupported finite-belt relation/source/point ownership.");
        var reference=Create();
        if(d.Placements.Count!=4 || (d.ConnectorLinks?.Count??0)!=0 || (d.Groups?.Count??0)!=0)
            throw new InvalidOperationException("Finite-belt preset requires its four related placements; additional graphs are unsupported.");
        var belt=d.Placements.SingleOrDefault(v=>v.InstanceId==p.BeltId)
            ??throw new InvalidOperationException("Finite-belt placement is missing.");
        var dx=belt.Position[0];var dz=belt.Position[2];
        if(Math.Abs(dx)>10||Math.Abs(dz)>10)throw new InvalidOperationException("Preset translation exceeds the bounded 10m workspace.");
        foreach(var original in reference.Placements)
        {
            var actual=d.Placements.SingleOrDefault(v=>v.InstanceId==original.InstanceId);
            if(actual is null||actual.AssetId!=original.AssetId||actual.Rotation.Any(v=>Math.Abs(v)>1e-6)
                ||actual.Scale.Where((v,i)=>Math.Abs(v-original.Scale[i])>1e-6).Any()
                ||Math.Abs(actual.Position[0]-original.Position[0]-dx)>1e-5
                ||Math.Abs(actual.Position[1]-original.Position[1])>1e-5
                ||Math.Abs(actual.Position[2]-original.Position[2]-dz)>1e-5)
                throw new InvalidOperationException("Move the complete finite-belt preset through its belt; independent pose/rotation/scale is unsupported.");
        }
        if(d.SignalLinks is null||d.SignalLinks.Count!=3||!reference.SignalLinks!.All(d.SignalLinks.Contains))
            throw new InvalidOperationException("Finite-belt point/signal relations cannot be silently remapped.");
    }
    public static SceneDefinition ReplaceSource(SceneDefinition source,WorkspaceDocument d)
    {
        Validate(d);var p=d.FiniteBeltPreset!;
        var equipment=d.Placements.Select(v=>new SceneEquipment(v.InstanceId,"workspacePresetAsset",v.AssetId,
            v.Position,v.Rotation,v.Scale,JsonSerializer.SerializeToElement(new{assetId=v.AssetId,
                sceneControls=v.InstanceId==p.OperatorId?new[]{new{nodeName="KIN_pushbutton",label="Momentary start",
                    action="workspace-press",feedbackTravelM=0}}:[]}))).ToArray();
        object Point(string name,string owner,string role)=>new{name,type="BOOL",owner,role,initial=false};
        var simulation=JsonSerializer.SerializeToElement(new{type="workspaceFiniteBelt",preset=p,
            points=new[]{Point(p.StartPoint,"PC","input"),Point(p.RunPoint,"PLC","output"),Point(p.BeamPoint,"PC","input")},
            actions=new[]{new{id="workspace-press",label="Press momentary start",type="pulse",point=p.StartPoint}},
            pointBindings=new[]{new{point=p.RunPoint,equipmentId=p.BeltId,mode="running"},
                new{point=p.BeamPoint,equipmentId=p.SensorId,mode="photoeye"},
                new{point=p.StartPoint,equipmentId=p.OperatorId,mode="workspaceMomentary"}}});
        return source with{ Name="Workspace · Finite Belt Cell",Description="One finite carton; physical momentary start and beam feedback. Write your own controller.",
            Equipment=equipment,Simulation=simulation,Verification=JsonSerializer.SerializeToElement(new{}),TemplateOverride=null,PlcTestProfile=null};
    }
}
