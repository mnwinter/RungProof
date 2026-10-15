using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    private bool TryBuildRemainingProductionNine(SceneEnvironmentArtDirection d,Aabb b,int sx,int sz,Node3D architecture)
    {
        var kind=d.LayoutVariant switch
        {
            "label-room-transfer-hatch"=>1,"receiving-staggered-bays"=>2,
            "xy-pallet-open-frame"=>3,"powder-high-pitched-bay"=>4,
            "guard-transfer-wide-side-open"=>5,"restart-high-corner-frame"=>6,
            "bag-index-deep-side-opening"=>7,"stop-conveyor-short-dock"=>8,
            "pusher-open-takeaway-side"=>9,_=>0,
        };
        if(kind==0)return false;
        var reserve=b;if(OperatorAccessBounds is Aabb a)reserve=reserve.Merge(new Aabb(a.Position-Position,a.Size));
        var c=reserve.GetCenter();var w=MathF.Max(8,reserve.Size.X+4.6f);var dep=MathF.Max(6,reserve.Size.Z+4.6f);
        var h=kind switch {1=>3.2f,2=>4.1f,3=>4.0f,4=>3.8f,5=>3.5f,6=>3.8f,7=>3.2f,8=>3.3f,_=>3.6f};
        var x=c.X+sx*w/2;var z=c.Z+sz*dep/2;
        string wallHex=kind switch {1=>"#D5D5C3",2=>"#BDB7A4",3=>"#AAB4A8",4=>"#CBBFA5",5=>"#CACDC4",6=>"#C1B6A1",7=>"#CBBDAA",8=>"#CDC5AE",_=>"#BAA391"};
        string frameHex=kind switch {1=>"#6E9084",2=>"#778A8C",3=>"#647267",4=>"#957657",5=>"#668583",6=>"#778571",7=>"#A37A64",8=>"#7F8867",_=>"#6B8389"};
        var wall=EnvironmentDetailedSurface(wallHex,"paint");var frame=EnvironmentDetailedSurface(frameHex,"paint");
        var floor=EnvironmentDetailedSurface(kind==1?"#A5AEA2":kind==7?"#AEA58F":"#9FA39C",kind==1||kind==7?"paint":"concrete");
        var timber=EnvironmentDetailedSurface("#B18F60","wood");var stainless=EnvironmentDetailedSurface("#BEC5C0","metal");
        var paper=EnvironmentDetailedSurface("#E3D9BE","cardboard");
        if(GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is {} old)old.Visible=false;
        Box(architecture,"WholeLocationFoundation",new Vector3(w+4,.12f,dep+4),new Vector3(c.X,-.065f,c.Z),floor);
        Node3D Face(string name,Vector3 at,float yaw=0)
        {var n=new Node3D{Name=name,Position=at,RotationDegrees=new Vector3(0,yaw,0)};architecture.AddChild(n);return n;}
        var rear=Face("PurposeConnectedRear",new Vector3(c.X,0,z));
        var sideLen=kind is 1 or 4?MathF.Min(dep,4.8f):kind is 5 or 6 or 9?MathF.Min(dep,4.5f):dep;
        var side=Face("PurposeConnectedSide",new Vector3(x,0,z-sz*sideLen/2),90);
        void Wall(Node3D n,float len,float height,List<Rect2> holes,bool glass=false)=>ArtWall(n,len,height,.18f,wall,holes,glass);
        void Cap(Node3D n,float len,float height)=>Box(n,"WallBearingCap",new Vector3(len+.18f,.22f,.30f),new Vector3(0,height-.11f,0),frame);
        Node3D Annex(Node3D edge,string name,float offset,float aw,float ad,float ah,int outward,bool rearDoor=false,bool cutawayRoof=false)
        {
            var n=new Node3D{Name=name,Position=new Vector3(offset,0,0)};edge.AddChild(n);
            Box(n,"AnnexConnectedFoundation",new Vector3(aw+.3f,.04f,ad+.3f),new Vector3(0,-.025f,outward*ad/2),floor);
            foreach(var end in new[]{-1,1})Box(n,"AnnexWallReturn",new Vector3(.18f,ah,ad),new Vector3(end*aw/2,ah/2,outward*ad/2),wall);
            var back=new Node3D{Name="AnnexRearFacade",Position=new Vector3(0,0,outward*ad)};n.AddChild(back);
            Wall(back,aw,ah,rearDoor?new(){new Rect2(-.6f,0,1.2f,2.2f)}:new());
            Box(n,"WallSupportedAnnexRoof",new Vector3(aw+.25f,.16f,cutawayRoof?.40f:ad+.20f),new Vector3(0,ah,outward*(cutawayRoof?ad-.20f:ad/2)),frame);
            n.SetMeta("actualConnectedAnnexBounds",new Aabb(new Vector3(-aw/2,0,MathF.Min(0,outward*ad)),new Vector3(aw,ah,ad)));
            return n;
        }
        void Fixture(Node3D n,float px,float height,int outward)
        {
            Box(n,"FixtureBearingBracket",new Vector3(.14f,.18f,.30f),new Vector3(px,height-.20f,-outward*.12f),frame);
            var glow=(StandardMaterial3D)EnvironmentDetailedSurface("#E4DED0","paint").Duplicate();glow.EmissionEnabled=true;glow.Emission=new Color("E4DED0").SrgbToLinear();glow.EmissionEnergyMultiplier=.3f;
            Box(n,"SupportedEdgeLight",new Vector3(1.2f,.08f,.32f),new Vector3(px,height-.35f,-outward*.18f),glow);
            n.AddChild(new OmniLight3D{Name="PassiveLocationLight",Position=new Vector3(px,height-.45f,-outward*.30f),LightEnergy=.22f,OmniRange=8,ShadowEnabled=true});
        }
        var rh=new List<Rect2>();var sh=new List<Rect2>();
        if(kind==3)
        {
            // XY layer assembly has an open shed, with roof only above the
            // external service edge. Gantry and pallet travel stay open to sky.
            Wall(rear,w,1.2f,new()); // Service facade alone owns this side; no low wall across its doorway.
            Box(rear,"ExternalShedRoof",new Vector3(w,.18f,1.5f),new Vector3(0,h,sz*.35f),frame);
            Box(rear,"ContinuousRoofBearing",new Vector3(w,.32f,.32f),new Vector3(0,h-.16f,sz*.35f),frame);
            foreach(var px in new[]{-w/2,0,w/2})Box(rear,"GroundedLoadingPost",new Vector3(.26f,h,.26f),new Vector3(px,h/2,sz*.35f),frame);
            var bay=Face("ConnectedLayerServiceFacade",new Vector3(x,0,z-sz*1.7f),90);
            Wall(bay,3.4f,2.9f,new(){new Rect2(-1.3f,0,2.6f,2.5f)});
            Annex(bay,"LayerMaterialServiceBay",0,3,1.5f,2.9f,sx);Fixture(bay,0,2.9f,sx);
        }
        else
        {
            // Each location resolves its own opening and service adjacency,
            // rather than inherited family props or random window fractions.
            var entry=-sz*sideLen*.24f;
            if(kind is 1 or 4 or 7)rh.Add(new Rect2(w*.32f-.6f,0,1.2f,2.2f));
            else sh.Add(new Rect2(entry-.6f,0,1.2f,2.2f));
            if(kind==1)
            {
                sh.Add(new Rect2(-1.25f,.85f,2.5f,h-.85f));
                var supplies=Annex(side,"VisibleLabelSupplyServiceBay",0,3.0f,1.4f,h,sx,true,true);
                PassiveClosedCabinet(supplies,"ClosedPackagingSupplyCupboard",new Vector3(.60f,1.4f,.30f),
                    new Vector3(.95f,.70f,sx*1.15f),sx,stainless,paper,"LABEL\nSUPPLIES");
            }
            else if(kind==2)
            {
                foreach(var px in new[]{-w*.27f,w*.27f})
                {rh.Add(new Rect2(px-1.5f,0,3,2.7f));Annex(rear,"OffsetReceivingDock",px,3.4f,1.7f,h,sz);}
                rh.Add(new Rect2(-1.7f,2.8f,3.4f,.7f));
            }
            else if(kind==4)
            {
                sh.Add(new Rect2(-1.4f,0,2.8f,2.5f));
                var bay=Annex(side,"VisibleDryMaterialServiceHouse",0,3.2f,1.8f,h,sx,false,true);
                Box(bay,"ClosedDryServiceCupboard",new Vector3(.7f,1.6f,.30f),new Vector3(.95f,.8f,sx*1.55f),frame);
                rh.Add(new Rect2(-w*.23f-.8f,2.4f,1.6f,.7f));
                // The visible service house uses its actual wall-supported flat roof.
            }
            else if(kind==5)
            {
                rh.Add(new Rect2(-w*.22f-1.4f,1.2f,2.8f,1.1f));
                rh.Add(new Rect2(w*.22f-.7f,0,1.4f,2.2f));
                Annex(rear,"TransferMaintenanceServiceBay",w*.22f,2.4f,1.2f,h,sz);
                // Environmental observation glazing adds no guard, sensor or
                // claim about the retained training safety devices.
            }
            else if(kind==6)
            {
                rh.Add(new Rect2(-1.5f,1.2f,3,1.15f));
                var offset=sz*sideLen*.22f;sh.Add(new Rect2(offset-.7f,0,1.4f,2.2f));
                Annex(side,"RobotCommissioningServiceRecess",offset,2.5f,1.4f,h,sx);
            }
            else if(kind==7)
            {
                var offset=sz*sideLen*.20f;sh.Add(new Rect2(offset-1.4f,1.05f,2.8f,1.15f));
                Annex(side,"ReversibleBagObservationGallery",offset,3.2f,1.3f,h,sx,true);
                rh.Add(new Rect2(-1.5f,2.2f,3,.6f));
            }
            else if(kind==8)
            {
                rh.Add(new Rect2(-1.4f,0,2.8f,2.6f));
                Annex(rear,"HeldParcelReceivingRecess",0,3.2f,1.6f,h,sz);
                rh.Add(new Rect2(-w*.27f-.75f,2.35f,1.5f,.55f));
            }
            else
            {
                rh.Add(new Rect2(-1.3f,0,2.6f,2.5f));
                Annex(rear,"LateralTransferServiceWing",0,3.0f,1.5f,h,sz);
                sh.Add(new Rect2(sz*sideLen*.22f-.8f,1.3f,1.6f,1.1f));
                // The near takeaway side is open beyond the short service
                // return, preserving the pusher and receiver teaching view.
            }
            if(kind is 2 or 4)
                IndustrialMaterialWall(rear,w,h,rh,.95f+(kind==4?.25f:0),
                    EnvironmentDetailedSurface(kind==2?"#84918D":"#82998E","paint"),
                    EnvironmentDetailedSurface(kind==2?"#BEC8C7":"#CFD2C7",kind==2?"metal":"paint"));
            else Wall(rear,w,h,rh,true);
            Wall(side,sideLen,h,sh,kind!=1);Cap(rear,w,h);Cap(side,sideLen,h);
            if(kind==4)
                foreach(var pierX in new[]{-w*.08f,w*.12f})
                    if(!ArtColumnBlocksOpening(pierX,rh))
                        Box(rear,"GroundedDryServicePier",new Vector3(.22f,h,.30f),new Vector3(pierX,h/2,-sz*.13f),EnvironmentDetailedSurface("#75877F","paint"));
            if(kind is 1 or 4 or 7 or 8)
            {var cut=Face("JoinedShortCameraReturn",new Vector3(c.X-sx*w/2,0,z-sz*.8f),90);Wall(cut,1.6f,h,new());Cap(cut,1.6f,h);}
            if(kind==2)
            {
                // Visible intermediate girder bearings divide the long hall
                // into irregular construction bays, without graphic stripes.
                foreach(var px in new[]{-w/2,-w*.12f,w*.12f,w/2})Box(rear,"ReceivingGirderPost",new Vector3(.24f,h,.28f),new Vector3(px,h/2,-sz*.13f),frame);
            }
            if(kind==2)
            {
                var loadingSteel=EnvironmentDetailedSurface("#7E8986","paint");
                foreach(var dockX in new[]{-w*.27f,w*.27f})
                {
                    foreach(var end in new[]{-1,1})Box(rear,"VisibleLoadingSteelJamb",new Vector3(.20f,2.9f,.34f),new Vector3(dockX+end*1.61f,1.45f,-sz*.13f),loadingSteel);
                    Box(rear,"VisibleLoadingSteelHeader",new Vector3(3.45f,.22f,.34f),new Vector3(dockX,2.82f,-sz*.13f),loadingSteel);
                    rear.AddChild(new OmniLight3D{Name="PassiveLoadingRevealLight",Position=new Vector3(dockX,2.35f,sz*.80f),LightEnergy=.25f,OmniRange=4,ShadowEnabled=true});
                }
            }
            Fixture(rear,-w*.32f,h,sz);Fixture(rear,w*.32f,h,sz);
            var boardX=kind==2?0:kind==5?-w*.38f:kind is 1 or 4 or 7?-w*.30f:w*.27f;
            Box(rear,"PurposeServiceReferenceBoard",new Vector3(2.0f,.75f,.07f),new Vector3(boardX,1.5f,-sz*.14f),kind==1?stainless:paper);
            var label=kind switch {1=>"WEIGH / LABEL",2=>"RECEIVING TRANSFER",4=>"BATCH TRAINING",5=>"TRANSFER SERVICE",6=>"ROBOT COMMISSIONING",7=>"BAG TRANSFER",8=>"PARCEL HOLD",_=>"SIDE TRANSFER"};
            ProductionNotice(rear,label,new Vector3(boardX,1.5f,-sz*.20f),sz,25,.003f);
        }
        architecture.SetMeta("resolvedProductionReserve",reserve);
        architecture.SetMeta("resolvedPurposeLayout",d.LayoutVariant);
        return true;
    }

    // Split the actual already-subtracted wall pieces, retaining the exact
    // full-height portal markers and window frames. No coplanar overlays,
    // truncated doorway markers, lower thresholds, or graphic panel seams.
    private static void IndustrialMaterialWall(Node3D parent,float width,float height,
        List<Rect2> holes,float lowerHeight,Material lower,Material upper)
    {
        var construction=new Node3D();
        ArtWall(construction,width,height,.18f,upper,holes,true);
        foreach(var child in construction.GetChildren())
        {
            if(child is MeshInstance3D mesh && mesh.Name.ToString().StartsWith("ArchitecturalWallSegment") && mesh.Mesh is BoxMesh box)
            {
                var bottom=mesh.Position.Y-box.Size.Y/2;
                var top=mesh.Position.Y+box.Size.Y/2;
                var split=Mathf.Clamp(lowerHeight,bottom,top);
                if(split-bottom>.001f)
                    Box(parent,"MineralLowerWallPiece",new Vector3(box.Size.X,split-bottom,box.Size.Z),new Vector3(mesh.Position.X,(bottom+split)/2,mesh.Position.Z),lower);
                if(top-split>.001f)
                    Box(parent,"UpperCladdingWallPiece",new Vector3(box.Size.X,top-split,box.Size.Z),new Vector3(mesh.Position.X,(split+top)/2,mesh.Position.Z),upper);
            }
            else
            {
                construction.RemoveChild(child);
                parent.AddChild(child);
            }
        }
        construction.Free();
    }
}



