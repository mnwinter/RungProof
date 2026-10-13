using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Six source-grounded locations. All furnishings remain passive siblings,
    // outside the merged authored motion and operator approach reserve.
    private bool TryBuildNextProcessRoom(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        var drill = d.LayoutVariant == "drill-paired-side-posts";
        var sump = d.LayoutVariant == "sump-open-pit-annex";
        var garage = d.LayoutVariant == "parking-low-garage-facade";
        var lift = d.LayoutVariant == "service-lift-two-landings";
        var baggage = d.LayoutVariant == "baggage-wide-terminal-wall";
        var robot = d.LayoutVariant == "container-open-loading-shed";
        if (!drill && !sump && !garage && !lift && !baggage && !robot) return false;
        var reserve = b;
        if (OperatorAccessBounds is Aabb a)
            reserve = reserve.Merge(new Aabb(a.Position - Position, a.Size));
        var c = reserve.GetCenter();
        var w = MathF.Max(8, reserve.Size.X + 4.4f);
        var dep = MathF.Max(6, reserve.Size.Z + 4.4f);
        var rearZ = c.Z + sz * dep / 2;
        var sideX = c.X + sx * w / 2;
        Node3D Face(string name, Vector3 at, float yaw = 0)
        {
            var face = new Node3D { Name = name, Position = at,
                RotationDegrees = new Vector3(0, yaw, 0) };
            architecture.AddChild(face); return face;
        }
        var rear = Face("PurposeRearBoundary", new Vector3(c.X, 0, rearZ));
        var side = Face("PurposeSideBoundary", new Vector3(sideX, 0, c.Z), 90);
        var plaster = StudioSceneMaterials.CreateDetailed(drill ? "#B5A18A" : baggage ? "#D1D3C7" : lift ? "#C5BCAA" : "#B9BEB0", "paint");
        var frame = StudioSceneMaterials.CreateDetailed(drill ? "#687867" : baggage ? "#637C7B" : lift ? "#8B7864" : "#7B8580", "paint");
        var mineral = StudioSceneMaterials.CreateDetailed(sump ? "#9EA89A" : garage ? "#9FA39C" : "#A5A69A", "concrete");
        var wood = StudioSceneMaterials.CreateDetailed("#B28C58", "wood");
        var paper = StudioSceneMaterials.CreateDetailed("#E3DCC6", "cardboard");
        architecture.SetMeta("resolvedPurposeReserve", reserve);
        architecture.SetMeta("resolvedPurposeFootprint", new Aabb(new Vector3(c.X-w/2,0,c.Z-dep/2), new Vector3(w,4,dep)));
        // Pit and lift profiles retain authored zero/pit/water surfaces. Their
        // new foundation lies wholly outside B; no opaque slab crosses the pit.
        if (!sump && !lift)
        {
            if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } floor) floor.Visible = false;
            Box(architecture,"PurposeContinuousFloor",new Vector3(w+4,.12f,dep+4),new Vector3(c.X,-.065f,c.Z),mineral);
        }
        void Panel(Node3D parent,float length,float height,List<Rect2> holes,bool glaze=true)
            => ArtWall(parent,length,height,.18f,plaster,holes,glaze);
        void Cap(Node3D parent,float length,float height)
            => Box(parent,"ConnectedWallCap",new Vector3(length+.18f,.18f,.30f),new Vector3(0,height-.09f,0),frame);
        void Fixture(Node3D parent,float x,float height,int outward)
        {
            Box(parent,"WallFixtureSupport",new Vector3(.14f,.18f,.30f),new Vector3(x,height-.15f,-outward*.10f),frame);
            var glow=(StandardMaterial3D)StudioSceneMaterials.CreateDetailed("#E5E1D4","paint").Duplicate();
            glow.EmissionEnabled=true; glow.Emission=new Color("E5E1D4").SrgbToLinear(); glow.EmissionEnergyMultiplier=.3f;
            Box(parent,"SupportedDiffuseFixture",new Vector3(1.2f,.08f,.32f),new Vector3(x,height-.28f,-outward*.18f),glow);
            parent.AddChild(new OmniLight3D { Name="PassivePurposeLight",Position=new Vector3(x,height-.4f,-outward*.32f),LightColor=new Color("E5E1D4").SrgbToLinear(),LightEnergy=.22f,OmniRange=8,ShadowEnabled=true });
        }
        // A connected room beyond a real parent opening. No front-plane filler
        // is added: ArtWall alone owns the hole, jambs and supported lintel.
        void Annex(Node3D edge,string name,float offset,float width,float depth,float height,int outward)
        {
            var n=new Node3D { Name=name,Position=new Vector3(offset,0,0) }; edge.AddChild(n);
            Box(n,"AnnexFoundation",new Vector3(width+.3f,.04f,depth+.3f),new Vector3(0,-.025f,outward*depth/2),mineral);
            foreach(var sign in new[]{-1,1})
                Box(n,"ConnectedAnnexReturn",new Vector3(.18f,height,depth),new Vector3(sign*width/2,height/2,outward*depth/2),plaster);
            Box(n,"AnnexBackWall",new Vector3(width+.18f,height,.18f),new Vector3(0,height/2,outward*depth),plaster);
            Box(n,"SupportedAnnexRoof",new Vector3(width+.25f,.16f,depth+.20f),new Vector3(0,height,outward*depth/2),frame);
            n.SetMeta("passiveAnnexBounds",new Aabb(new Vector3(-width/2,0,MathF.Min(0,outward*depth)),new Vector3(width,height,depth)));
        }
        if (sump)
        {
            // Open dewatering yard: a dry-side pump-service building, not a
            // room enclosing the tank or a decorative cover over the sump.
            var hut=Face("DryPumpServiceBuilding",new Vector3(sideX,0,c.Z));
            hut.RotationDegrees=new Vector3(0,90,0);
            const float hw=2.6f,hd=2.0f,hh=2.8f;
            Panel(hut,hw,hh,new(){new Rect2(-.6f,0,1.2f,2.2f)},false);
            Annex(hut,"ConnectedDryServiceHouse",0,hw,hd,hh,sx);
            Box(side,"DryServiceApron",new Vector3(dep+1,.04f,1.8f),new Vector3(0,-.025f,-sx*.60f),mineral);
            Box(rear,"LowYardBoundary",new Vector3(w,.65f,.20f),new Vector3(0,.325f,0),mineral);
            Fixture(hut,.85f,hh,sx);
            ProductionNotice(hut,"DEWATERING SERVICE",new Vector3(0,2.5f,-sx*.12f),sx,24,.003f);
        }
        else if (garage)
        {
            // Two full vehicle-size openings in a low garage frontage. The
            // authored barrier, road and vehicle trajectory remain unchanged.
            const float h=3.4f;
            Panel(rear,w,h,new(){new Rect2(-w*.22f-1.6f,0,3.2f,2.8f),new Rect2(w*.22f-1.6f,0,3.2f,2.8f)},false);
            Cap(rear,w,h);
            foreach(var x in new[]{-w*.22f,w*.22f}) Annex(rear,"GarageVehicleRecess",x,3.6f,1.5f,h,sz);
            // A short lobby frontage attaches at the existing rear corner;
            // the open foreground remains the vehicle forecourt.
            var lobbyLength=3.0f;
            var lobby=Face("ConnectedGarageLobby",new Vector3(sideX,0,rearZ-sz*lobbyLength/2),90);
            Panel(lobby,lobbyLength,h,new(){new Rect2(-.6f,0,1.2f,2.2f)},false); Cap(lobby,lobbyLength,h);
            Annex(lobby,"LobbyServiceRecess",0,2.2f,2,h,sx);
            Fixture(rear,-w*.22f,h,sz); Fixture(rear,w*.22f,h,sz);
        }
        else if (robot)
        {
            // Open loading shed: a low connected rear service wall with
            // outboard steel supports. Nothing roofs over the robot sweep.
            const float h=3.8f;
            Panel(rear,w,2.9f,new(){new Rect2(-1.5f,0,3,2.5f)},false);
            Annex(rear,"ContainerMaterialServiceAnnex",0,4,1.8f,2.9f,sz);
            Box(rear,"RearServiceCanopy",new Vector3(w+.25f,.18f,1.15f),new Vector3(0,h,sz*.25f),frame);
            Box(rear,"ContinuousCanopyBearingGirder",new Vector3(w+.25f,.30f,.32f),new Vector3(0,h-.15f,sz*.25f),frame);
            foreach(var x in new[]{-w/2,-w*.24f,w*.24f,w/2})
            {
                Box(rear,"GroundedShedColumn",new Vector3(.26f,h,.26f),new Vector3(x,h/2,sz*.25f),frame);
                Box(rear,"GroundedColumnBase",new Vector3(.48f,.08f,.48f),new Vector3(x,.04f,sz*.25f),mineral);
            }
            var shortSide=Face("ConnectedLoadingSideReturn",new Vector3(sideX,0,rearZ-sz*1.5f),90);
            Panel(shortSide,3,2.9f,new(),false); Cap(shortSide,3,2.9f);
            Fixture(rear,-w*.27f,2.9f,sz);
            Box(rear,"PassiveContainerMaterialBoard",new Vector3(2.2f,.8f,.08f),new Vector3(w*.25f,1.8f,-sz*.13f),wood);
            ProductionNotice(rear,"CONTAINER TRANSFER",new Vector3(w*.25f,1.8f,-sz*.19f),sz,28,.003f);
        }
        else
        {
            var h=lift?4.1f:baggage?3.3f:3.2f;
            var entryX=sz*(dep/2-1.5f);
            var serviceX=-sz*dep*.20f;
            var sideHoles=new List<Rect2>{new(entryX-.6f,0,1.2f,2.2f)};
            var rearHoles=new List<Rect2>();
            if(drill)
            {
                sideHoles.Add(new Rect2(serviceX-.65f,0,1.3f,2.2f));
                rearHoles.Add(new Rect2(-w*.20f-1.1f,2.1f,2.2f,.65f));
                Annex(side,"FixturePreparationNook",serviceX,1.8f,.9f,h,sx);
            }
            else if(baggage)
            {
                sideHoles.Add(new Rect2(serviceX-1.3f,0,2.6f,2.4f));
                rearHoles.Add(new Rect2(-2,1.4f,4,.95f));
                Annex(side,"BaggageInspectionCorridor",serviceX,3,1.3f,h,sx);
            }
            else
            {
                // This building adjacency never invents another lift/car or
                // extends the illustrative authored landing/stairs geometry.
                rearHoles.Add(new Rect2(-w*.22f-1.25f,2.5f,2.5f,.9f));
                Annex(side,"GroundFloorServiceLobby",serviceX,2.6f,1.6f,2.8f,sx);
                sideHoles.Add(new Rect2(serviceX-.6f,0,1.2f,2.2f));
                Box(rear,"ExteriorUpperServicePanel",new Vector3(w*.42f,.9f,.08f),new Vector3(w*.18f,3,-sz*.13f),frame);
                Box(rear,"ExternalDryLobbyFoundation",new Vector3(w+.5f,.04f,1.5f),new Vector3(0,-.025f,-sz*.55f),mineral);
                Box(side,"ExternalDryApproachFoundation",new Vector3(dep+.5f,.04f,1.5f),new Vector3(0,-.025f,-sx*.55f),mineral);
            }
            Panel(rear,w,h,rearHoles); Panel(side,dep,h,sideHoles,false); Cap(rear,w,h); Cap(side,dep,h);
            var cut=Face("ConnectedShortCutawayReturn",new Vector3(c.X-sx*w/2,0,rearZ-sz*.9f),90);
            Panel(cut,1.8f,h,new(),false); Cap(cut,1.8f,h);
            Fixture(rear,w*.28f,h,sz);
            var boardX=drill?w*.25f:-w*.28f;
            Box(rear,"PurposeReferenceBoard",new Vector3(2.0f,.75f,.07f),new Vector3(boardX,1.55f,-sz*.14f),drill?wood:paper);
            ProductionNotice(rear,drill?"FIXTURE PREPARATION":baggage?"BAGGAGE CLASSIFICATION":"LIFT SERVICE / TRAINING",new Vector3(boardX,1.55f,-sz*.20f),sz,26,.003f);
        }
        return true;
    }
}

