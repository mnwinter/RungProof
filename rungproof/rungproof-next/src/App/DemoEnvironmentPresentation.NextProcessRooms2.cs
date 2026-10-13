using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Second six production contexts: open infrastructure and enclosed process
    // rooms share construction helpers, not one repeated room composition.
    private bool TryBuildNextProcessRoom2(SceneEnvironmentArtDirection d,Aabb b,int sx,int sz,Node3D architecture)
    {
        var barrel=d.LayoutVariant=="barrel-open-utility-canopy";
        var coating=d.LayoutVariant=="coating-exhaust-clerestory";
        var chain=d.LayoutVariant=="chain-lift-high-low-wall";
        var vision=d.LayoutVariant=="vision-high-span";
        var radar=d.LayoutVariant=="radar-open-roof-sky";
        var bridge=d.LayoutVariant=="bridge-canal-quayside";
        if(!barrel&&!coating&&!chain&&!vision&&!radar&&!bridge)return false;
        var reserve=b;
        if(OperatorAccessBounds is Aabb access)reserve=reserve.Merge(new Aabb(access.Position-Position,access.Size));
        var c=reserve.GetCenter(); var w=reserve.Size.X+4.4f; var dep=reserve.Size.Z+4.4f;
        var z=c.Z+sz*dep/2; var x=c.X+sx*w/2;
        Node3D Face(string name,Vector3 at,float yaw=0)
        {var n=new Node3D{Name=name,Position=at,RotationDegrees=new Vector3(0,yaw,0)};architecture.AddChild(n);return n;}
        var rear=Face("ProcessRearContext",new Vector3(c.X,0,z));
        var side=Face("ProcessSideContext",new Vector3(x,0,c.Z),90);
        var wall=EnvironmentDetailedSurface(coating?"#C6CFBD":vision?"#D3CEC0":chain?"#BBAF97":bridge?"#B5AF9F":"#B7BEAD","paint");
        var trim=EnvironmentDetailedSurface(vision?"#916758":coating?"#778B7C":bridge?"#658080":barrel?"#91815E":"#697C71","paint");
        var concrete=EnvironmentDetailedSurface("#9FA39C","concrete");
        var paper=EnvironmentDetailedSurface("#E0D8C0","cardboard");
        architecture.SetMeta("processContextReserve",reserve);
        var noProcessFloor=chain||bridge;
        if(!noProcessFloor)
        {
            if(GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is {} old)old.Visible=false;
            Box(architecture,"ProcessContinuousFoundation",new Vector3(w+4,.12f,dep+4),new Vector3(c.X,-.065f,c.Z),concrete);
        }
        void Wall(Node3D n,float len,float h,List<Rect2> holes,bool glass=false)=>ArtWall(n,len,h,.18f,wall,holes,glass);
        void Beam(Node3D n,float len,float h)=>Box(n,"SupportedBoundaryGirder",new Vector3(len+.18f,.22f,.30f),new Vector3(0,h-.11f,0),trim);
        void Light(Node3D n,float px,float h,int outward)
        {
            Box(n,"ConnectedLightBracket",new Vector3(.12f,.20f,.32f),new Vector3(px,h-.2f,-outward*.12f),trim);
            var lamp=(StandardMaterial3D)EnvironmentDetailedSurface("#E1DED0","paint").Duplicate();lamp.EmissionEnabled=true;lamp.Emission=new Color("E1DED0").SrgbToLinear();lamp.EmissionEnergyMultiplier=.3f;
            Box(n,"ServiceDiffuser",new Vector3(1.2f,.08f,.30f),new Vector3(px,h-.35f,-outward*.18f),lamp);
            n.AddChild(new OmniLight3D{Name="PassiveContextLight",Position=new Vector3(px,h-.45f,-outward*.3f),LightEnergy=.22f,OmniRange=8,ShadowEnabled=true});
        }
        Node3D Annex(Node3D edge,string name,float offset,float aw,float ad,float ah,int outward,bool cutawayRoof=false)
        {
            var n=new Node3D{Name=name,Position=new Vector3(offset,0,0)};edge.AddChild(n);
            Box(n,"ContinuousAnnexFoundation",new Vector3(aw+.3f,.04f,ad+.3f),new Vector3(0,-.025f,outward*ad/2),concrete);
            foreach(var sign in new[]{-1,1})Box(n,"ConnectedAnnexReturn",new Vector3(.18f,ah,ad),new Vector3(sign*aw/2,ah/2,outward*ad/2),wall);
            Box(n,"AnnexBackWall",new Vector3(aw+.18f,ah,.18f),new Vector3(0,ah/2,outward*ad),wall);
            Box(n,"WallSupportedRoof",new Vector3(aw+.25f,.16f,cutawayRoof?.40f:ad+.20f),new Vector3(0,ah,outward*(cutawayRoof?ad-.20f:ad/2)),trim);
            n.SetMeta("actualAnnexLocalBounds",new Aabb(new Vector3(-aw/2,0,MathF.Min(0,outward*ad)),new Vector3(aw,ah,ad)));
            return n;
        }
        if(barrel||radar||bridge)
        {
            // Buildings sit along the X-side reserve so tanks/bridge do not
            // hide the only visible context behind their rear silhouettes.
            var hw=barrel?2.8f:radar?3f:3.4f;var hd=barrel?2.2f:radar?2f:2.4f;
            var house=Face(barrel?"DrumUtilityHouse":radar?"RadarServiceHouse":"BridgekeeperHouse",new Vector3(x,0,c.Z),90);
            Wall(house,hw,2.8f,new(){new Rect2(-.6f,0,1.2f,2.2f)});
            Annex(house,"ConnectedSiteBuilding",0,hw,hd,2.8f,sx);
            Box(side,"ConnectedDrySiteApron",new Vector3(dep+1,.04f,1.8f),new Vector3(0,-.025f,-sx*.6f),bridge?EnvironmentDetailedSurface("#B8B5A3","concrete"):concrete);
            if(!bridge)Box(rear,"LowInfrastructureBoundary",new Vector3(w,.55f,.18f),new Vector3(0,.275f,0),concrete);
            else
            {
                // Dry-bank edges only; the authored channel and bridge sweep
                // stay intact. No new water surface covers the physical field.
                // Leave the service-house doorway and its dry approach open;
                // the quay edge cannot form a low barrier across a real portal.
                var edgeLength=dep/2-.75f;
                foreach(var end in new[]{-1,1})
                    Box(side,"DryQuayEdge",new Vector3(edgeLength,.45f,.22f),new Vector3(end*(dep/4+.375f),.225f,sx*.15f),concrete);
                var bank=EnvironmentDetailedSurface("#A59A80","concrete");
                Box(side,"OuterMineralBankFoundation",new Vector3(dep+4,.04f,3.0f),new Vector3(0,-.065f,sx*1.8f),bank);

            }
            Light(house,.9f,2.8f,sx);
            ProductionNotice(house,barrel?"DRUM SERVICE":radar?"RADAR SERVICE":"BRIDGE CONTROL SERVICE",new Vector3(0,2.5f,-sx*.13f),sx,24,.003f);
            if(barrel)
            {
                // Supported service canopy stays entirely behind process B.
                const float roofH=3.4f;
                Box(rear,"BulkServiceCanopy",new Vector3(w,.18f,1.1f),new Vector3(0,roofH,sz*.25f),trim);
                Box(rear,"CanopyBearingGirder",new Vector3(w,.28f,.30f),new Vector3(0,roofH-.14f,sz*.25f),trim);
                foreach(var px in new[]{-w/2,0,w/2})Box(rear,"GroundedCanopyPost",new Vector3(.24f,roofH,.24f),new Vector3(px,roofH/2,sz*.25f),trim);
            }
        }
        else
        {
            var h=coating?3.4f:vision?4f:4.5f;
            var sideSpan=coating?4.8f:vision?4.5f:dep;
            if(coating||vision)side.Position=new Vector3(x,0,z-sz*sideSpan/2);
            var entry=sz*(sideSpan/2-1.5f); var offset=coating||vision?0f:-sz*dep*.20f;
            var rearHoles=new List<Rect2>();var sideHoles=new List<Rect2>{new(entry-.6f,0,1.2f,2.2f)};
            if(coating)
            {
                sideHoles.Clear();
                sideHoles.Add(new Rect2(-1.4f,.85f,2.8f,h-.85f));
                rearHoles.Add(new Rect2(w*.30f-.6f,0,1.2f,2.2f));
                rearHoles.Add(new Rect2(-w*.18f-1.8f,2.35f,3.6f,.6f));
                Annex(side,"ClosedProcessUtilitiesAnnex",offset,3,1.5f,h,sx,true);
            }
            else if(vision)
            {
                sideHoles.Clear();
                sideHoles.Add(new Rect2(-1.3f,.80f,2.6f,h-.80f));
                rearHoles.Add(new Rect2(w*.30f-.6f,0,1.2f,2.2f));
                rearHoles.Add(new Rect2(-w*.24f-1.4f,2.3f,2.8f,.75f));
                rearHoles.Add(new Rect2(w*.24f-1.4f,2.3f,2.8f,.75f));
                var storage=Annex(side,"OutgoingDockServiceExtension",offset,3,1.6f,h,sx,true);
                // Open reference storage is passive context. No scanner target,
                // extra product load, control, or calibration behavior is added.
                var storageDepth=sx*1.28f;
                foreach(var u in new[]{-.85f,.85f}) foreach(var v in new[]{-.15f,.15f})
                {
                    var suffix=(u<0?"Left":"Right")+(v<0?"Back":"Front");
                    Box(storage,"ReferenceStorageFoot"+suffix,new Vector3(.08f,.03f,.08f),new Vector3(u,.010f,storageDepth+v),trim);
                    Box(storage,"ReferenceStorageUpright"+suffix,new Vector3(.04f,1.465f,.04f),new Vector3(u,.7575f,storageDepth+v),trim);
                }
                var tier=0;
                foreach(var top in new[]{.45f,.95f,1.45f})
                {
                    Box(storage,"PassiveReferenceShelf"+tier,new Vector3(1.8f,.04f,.40f),new Vector3(0,top-.02f,storageDepth),trim);
                    Box(storage,"PassiveReferenceTray"+tier,new Vector3(.40f,.06f,.30f),new Vector3((tier-1)*.50f,top+.03f,storageDepth),paper);
                    tier++;
                }
                // Caption has its own supported plate inside the frame's
                // footprint, on the top shelf front. It carries no live value.
                var captionAt=new Vector3(0,1.36f,storageDepth-sx*.18f);
                Box(storage,"ReferenceStorageCaptionBearing",new Vector3(1.65f,.10f,.02f),captionAt,paper);
                ProductionNotice(storage,"PASSIVE REFERENCE STORAGE",captionAt-new Vector3(0,0,sx*.012f),sx,24,.0017f);
            }
            else
            {
                sideHoles.Add(new Rect2(offset-.6f,0,1.2f,2.2f));
                Annex(side,"LowerTransferServiceVolume",offset,3,1.4f,2.7f,sx);
                rearHoles.Add(new Rect2(-w*.22f-1.2f,2.7f,2.4f,.8f));
                Box(rear,"TransferElevationFacade",new Vector3(w*.40f,1,.08f),new Vector3(w*.18f,3.3f,-sz*.13f),trim);
                Box(rear,"DryTransferBuildingFoundation",new Vector3(w+.5f,.04f,1.5f),new Vector3(0,-.025f,-sz*.55f),concrete);
                Box(side,"DryServiceFoundation",new Vector3(dep+.5f,.04f,1.5f),new Vector3(0,-.025f,-sx*.55f),concrete);
            }
            if(vision)
            {
                IndustrialMaterialWall(rear,w,h,rearHoles,1.05f,
                    EnvironmentDetailedSurface("#9DADA7","paint"),
                    EnvironmentDetailedSurface("#BBC6C8","metal"));
                foreach(var pierX in new[]{-w*.04f,w*.46f})
                    if(!ArtColumnBlocksOpening(pierX,rearHoles))
                        Box(rear,"GroundedOpticalHallBearing",new Vector3(.22f,h,.30f),new Vector3(pierX,h/2,-sz*.13f),EnvironmentDetailedSurface("#778C8E","paint"));
            }
            else Wall(rear,w,h,rearHoles,true);
            ArtWall(side,sideSpan,h,.18f,EnvironmentDetailedSurface(coating?"#779187":vision?"#8E9B9B":"#BBAF97","paint"),sideHoles,false);
            Beam(rear,w,h);Beam(side,sideSpan,h);
            var shortReturn=Face("ConnectedCameraCutawayReturn",new Vector3(c.X-sx*w/2,0,z-sz*.9f),90);Wall(shortReturn,1.8f,h,new());Beam(shortReturn,1.8f,h);
            Light(rear,-w*.28f,h,sz);Light(rear,w*.28f,h,sz);
            Box(rear,"PassivePurposeBoard",new Vector3(2.4f,.8f,.07f),new Vector3((coating||vision?-1:1)*w*.28f,1.55f,-sz*.14f),paper);
            ProductionNotice(rear,coating?"COATING / SERVICE":vision?"OPTICAL ROUTING":"CARTON TRANSFER",new Vector3((coating||vision?-1:1)*w*.28f,1.55f,-sz*.20f),sz,28,.003f);
        }
        return true;
    }
}



