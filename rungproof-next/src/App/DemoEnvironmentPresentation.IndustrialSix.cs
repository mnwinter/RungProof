using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    private bool TryBuildIndustrialSix(SceneEnvironmentArtDirection d,Aabb b,int sx,int sz,Node3D architecture)
    {
        var inspection=d.LayoutVariant=="inspection-side-light";
        var dust=d.LayoutVariant=="dust-side-canopy";
        var assembly=d.LayoutVariant=="lift-open-bay-pilasters";
        var weld=d.LayoutVariant=="weld-dark-side-soffit";
        var sorter=d.LayoutVariant=="parcel-three-destination-hall";
        var cnc=d.LayoutVariant=="cnc-offset-high-bay";
        if(!inspection&&!dust&&!assembly&&!weld&&!sorter&&!cnc)return false;
        var reserve=b;if(OperatorAccessBounds is Aabb a)reserve=reserve.Merge(new Aabb(a.Position-Position,a.Size));
        var center=reserve.GetCenter();var w=MathF.Max(7,reserve.Size.X+4.6f);var dep=MathF.Max(6,reserve.Size.Z+4.6f);
        var rx=center.X+sx*w/2;var rz=center.Z+sz*dep/2;
        var h=inspection?3.3f:dust?3.0f:assembly?3.5f:weld?3.2f:sorter?4f:4.1f;
        var wall=EnvironmentDetailedSurface(inspection?"#C9BA9D":dust?"#B6B6A6":assembly?"#CBC2AC":weld?"#94958B":sorter?"#C5C1AA":"#BDC5C1","paint");
        var sideFinish=EnvironmentDetailedSurface(inspection?"#AB8874":dust?"#A78366":assembly?"#A1AF99":weld?"#8C7765":sorter?"#9DA88E":"#929E9B",dust||weld?"concrete":"paint");
        var trim=EnvironmentDetailedSurface(weld?"#957450":sorter?"#727F60":cnc?"#6D8583":"#6D7D75","paint");
        var floor=EnvironmentDetailedSurface(dust?"#9E9F91":weld?"#A4A59C":"#A0A69D","concrete");
        var timber=EnvironmentDetailedSurface("#B7905A","wood");var paper=EnvironmentDetailedSurface("#E2D8BE","cardboard");
        if(GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is {} old)old.Visible=false;
        // One slab owns the deeper observation bay and complete exterior
        // approach. Extend only its outward edge; no coplanar apron overlay.
        const float inspectionDepth = 2.0f;
        const float inspectionDoorWidth = 1.2f;
        var inspectionApronExtension = inspection ? inspectionDepth + .70f + inspectionDoorWidth / 2 + .10f - 2f : 0;
        Box(architecture,"WholeIndustrialFoundation",new Vector3(w+4+inspectionApronExtension,.12f,dep+4),
            new Vector3(center.X+sx*inspectionApronExtension/2,-.065f,center.Z),floor);
        Node3D Face(string name,Vector3 at,float yaw=0)
        {var n=new Node3D{Name=name,Position=at,RotationDegrees=new Vector3(0,yaw,0)};architecture.AddChild(n);return n;}
        var rear=Face("IndustrialRear",new Vector3(center.X,0,rz));var side=Face("IndustrialServiceSide",new Vector3(rx,0,center.Z),90);
        var rearHoles=new List<Rect2>();var sideHoles=new List<Rect2>();
        var entry=sz*(dep/2-1.5f);sideHoles.Add(new Rect2(entry-.6f,0,1.2f,2.2f));
        var aw=inspection?2.4f:dust?1.8f:assembly?2.4f:weld?2.6f:sorter?3.6f:3f;
        var ad=inspection?inspectionDepth:dust?1.3f:assembly?1.4f:weld?1.3f:sorter?1.8f:1.7f;
        var onRear=assembly||sorter;var offset=onRear?0:-sz*dep*.20f;var outward=onRear?sz:sx;
        var mouth=inspection?2.0f:dust?1.3f:assembly?2f:weld?2.2f:sorter?3.2f:2.6f;
        var holes=onRear?rearHoles:sideHoles;
        holes.Add(new Rect2(offset-mouth/2,inspection?1.1f:0,mouth,inspection?1.1f:2.5f));
        if(inspection||dust||weld)rearHoles.Add(new Rect2(-w*.20f-1.0f,2.25f,2.0f,.55f));
        if(cnc)rearHoles.Add(new Rect2(-w*.18f-1.5f,2.5f,3,.75f));
        if(sorter)foreach(var px in new[]{-w*.28f,w*.28f})rearHoles.Add(new Rect2(px-1.5f,2.8f,3,.6f));
        // Branched distribution uses two true material openings in its rear
        // frontage; they remain environmental service bays outside all lanes.
        if(sorter)rearHoles.Add(new Rect2(-w*.28f-1.2f,0,2.4f,2.5f));
        ArtWall(rear,w,h,.18f,wall,rearHoles,true);
        var sideLength=sorter?MathF.Min(dep,6f):dep;
        // Sorting side wall remains joined to rear but does not close distant
        // outgoing branch ends. Personnel entrance sits in the joined segment.
        if(sorter)
        {
            side.Position=new Vector3(rx,0,rz-sz*sideLength/2);
            sideHoles=new(){new Rect2(-sz*sideLength*.22f-.6f,0,1.2f,2.2f)};
        }
        if(inspection||dust)
        {
            // Keep the rear corner and front wall segment connected, but expose
            // the service bay at the actual camera instead of hiding it behind
            // an opaque upper facade. No glass/frame crosses the dust doorway.
            var leftEnd=offset-aw/2;var rightStart=offset+aw/2;
            void Segment(float lo,float hi)
            {
                var span=hi-lo;var mid=(lo+hi)/2;
                var n=new Node3D{Name="ConnectedServiceWallSegment",Position=new Vector3(mid,0,0)};side.AddChild(n);
                var door=new Rect2(entry-mid-.6f,0,1.2f,2.2f);
                var openings=door.Position.X>=-span/2 && door.End.X<=span/2?new List<Rect2>{door}:new();
                ArtWall(n,span,h,.18f,sideFinish,openings,false);
            }
            Segment(-sideLength/2,leftEnd);Segment(rightStart,sideLength/2);
            var serviceFront=new Node3D{Name="VisibleServiceCutaway",Position=new Vector3(offset,0,0)};side.AddChild(serviceFront);
            if(inspection)
            {
                Box(serviceFront,"ObservationBreastwork",new Vector3(aw,.85f,.18f),new Vector3(0,.425f,0),sideFinish);
                Box(serviceFront,"SupportedObservationSill",new Vector3(aw+.1f,.08f,.32f),new Vector3(0,.89f,0),trim);
            }
            else ArtWall(serviceFront,aw,2.5f,.18f,sideFinish,new(){new Rect2(-.65f,0,1.3f,2.2f)},false);
        }
        else ArtWall(side,sideLength,h,.18f,sideFinish,sideHoles,false);
        Box(rear,"RearContinuousBearing",new Vector3(w+.18f,.22f,.30f),new Vector3(0,h-.11f,0),trim);
        Box(side,"SideContinuousBearing",new Vector3(sideLength+.18f,.22f,.30f),new Vector3(0,h-.11f,0),trim);
        var cut=Face("JoinedCameraCutaway",new Vector3(center.X-sx*w/2,0,rz-sz*.9f),90);
        ArtWall(cut,1.8f,h,.18f,sideFinish,new(),false);
        var edge=onRear?rear:side;
        var annex=new Node3D{Name="ConnectedPurposeServiceBay",Position=new Vector3(offset,0,0)};edge.AddChild(annex);
        if(!inspection) // Expanded main slab exclusively bears this bay.
            Box(annex,"AnnexContinuousFoundation",new Vector3(aw+.3f,.04f,ad+.3f),new Vector3(0,-.025f,outward*ad/2),floor);
        foreach(var end in new[]{-1,1})Box(annex,"AnnexGroundedReturn",new Vector3(.18f,h,ad),new Vector3(end*aw/2,h/2,outward*ad/2),sideFinish);
        var back=new Node3D{Name="AnnexBack",Position=new Vector3(0,0,outward*ad)};annex.AddChild(back);
        ArtWall(back,aw,h,.18f,sideFinish,inspection?new(){new Rect2(-inspectionDoorWidth/2,0,inspectionDoorWidth,2.2f)}:new(),false);
        // The observation room has a real external entry and cutaway roof to
        // make its depth visible. Other bays have wall-supported service roofs.
        var roofDepth=inspection||dust?.40f:ad+.20f;
        Box(annex,"SupportedServiceRoof",new Vector3(aw+.25f,.16f,roofDepth),new Vector3(0,h,outward*(inspection||dust?ad-.20f:ad/2)),trim);
        if(inspection)
        {
            // Passive inspection/reference furniture, not another simulated
            // process station. Counter sits behind the retained observation sill.
            const float counterDepth=.40f;
            var counterCenter=outward*.40f;
            Box(annex,"InspectionReferenceCounter",new Vector3(1.6f,.06f,counterDepth),new Vector3(0,1.02f,counterCenter),timber);
            foreach(var u in new[]{-.70f,.70f}) foreach(var v in new[]{-.13f,.13f})
            {
                var suffix=(u<0?"Left":"Right")+(v<0?"Back":"Front");
                Box(annex,"InspectionCounterFoot"+suffix,new Vector3(.06f,.03f,.06f),new Vector3(u,.010f,counterCenter+v),trim);
                Box(annex,"InspectionCounterLeg"+suffix,new Vector3(.035f,.965f,.035f),new Vector3(u,.5075f,counterCenter+v),trim);
            }
            Box(annex,"PassiveInspectionReferenceTray",new Vector3(.50f,.03f,.30f),new Vector3(0,1.065f,counterCenter),paper);
            // Shared doorway edges define the sweep exactly. Its 1.2m endcaps
            // fit the 1.31m working strip, not the counter or back-wall jambs.
            var innerBack=outward*(ad-.09f);
            var counterRear=counterCenter+outward*counterDepth/2;
            var standing=(innerBack+counterRear)/2;
            var apron=outward*(ad+.70f);
            var route=new Node3D{Name="InspectionCounterCompleteApproach"};
            var lo=MathF.Min(apron,standing)-inspectionDoorWidth/2;
            var hi=MathF.Max(apron,standing)+inspectionDoorWidth/2;
            route.SetMeta("localRouteBounds",new Aabb(new Vector3(-inspectionDoorWidth/2,0,lo),new Vector3(inspectionDoorWidth,2,hi-lo)));
            route.SetMeta("routeScope","Exterior apron through retained rear doorway to passive counter standing region; not physical compliance or main-room control access.");
            route.SetMeta("actualDoorWidth",inspectionDoorWidth);
            route.SetMeta("actualWorkingClearDepth",MathF.Abs(counterRear-innerBack));
            route.SetMeta("localStandingCenter",new Vector3(0,0,standing));
            annex.AddChild(route);
        }
        if(dust||cnc)Box(annex,"ClosedPassiveUtilityCabinet",new Vector3(.60f,1.4f,.25f),new Vector3(.45f,.70f,outward*(ad-.2f)),trim);
        if(weld)Box(annex,"PassiveWeldingReferencePlate",new Vector3(1.6f,.8f,.06f),new Vector3(0,1.5f,outward*(ad-.12f)),timber);
        if(assembly)
        {
            // Reference fixture backing only: omit a worktable until its full
            // pedestrian access to the preparation bay is independently checked.
            Box(annex,"AssemblyFixtureReferenceBacking",new Vector3(1.7f,.9f,.08f),new Vector3(0,1.4f,outward*(ad-.15f)),timber);
        }
        foreach(var px in new[]{-w*.27f,w*.27f})
        {
            Box(rear,"ConnectedTaskLightBracket",new Vector3(.12f,.18f,.30f),new Vector3(px,h-.18f,-sz*.12f),trim);
            var diffuse=(StandardMaterial3D)EnvironmentDetailedSurface("#E0DED2","paint").Duplicate();diffuse.EmissionEnabled=true;diffuse.Emission=new Color("E0DED2").SrgbToLinear();diffuse.EmissionEnergyMultiplier=.3f;
            Box(rear,"MountedTaskLight",new Vector3(1.2f,.08f,.32f),new Vector3(px,h-.33f,-sz*.18f),diffuse);
            rear.AddChild(new OmniLight3D{Name="PassiveIndustrialRoomLight",Position=new Vector3(px,h-.45f,-sz*.3f),LightEnergy=.22f,OmniRange=8,ShadowEnabled=true});
        }
        var boardX=w*.26f;
        Box(rear,"PassivePurposeReferenceBoard",new Vector3(2.2f,.8f,.07f),new Vector3(boardX,1.5f,-sz*.14f),paper);
        ProductionNotice(rear,inspection?"CARTON INSPECTION":dust?"EXTRACTION COMMAND TRAINING":assembly?"ASSEMBLY PREPARATION":weld?"WELD SERVICE":sorter?"PARCEL DISTRIBUTION":"MACHINING SERVICE",new Vector3(boardX,1.5f,-sz*.20f),sz,26,.003f);
        architecture.SetMeta("resolvedIndustrialReserve",reserve);
        annex.SetMeta("resolvedServiceBayBounds",new Aabb(new Vector3(-aw/2,0,MathF.Min(0,outward*ad)),new Vector3(aw,h,ad)));
        return true;
    }
}

