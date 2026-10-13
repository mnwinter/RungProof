using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    private bool TryBuildProductionLocations3(SceneEnvironmentArtDirection d,Aabb b,int sx,int sz,Node3D architecture)
    {
        var inbound=d.LayoutVariant=="tote-receiving-side-door";
        var coolant=d.LayoutVariant=="coolant-low-washdown-recess";
        var pickup=d.LayoutVariant=="pickup-rear-dock-canopy";
        var bottle=d.LayoutVariant=="bottle-narrow-clean-bay";
        var finishing=d.LayoutVariant=="tote-long-service-gallery";
        var spindle=d.LayoutVariant=="dual-head-twin-rear-bays";
        if(!inbound&&!coolant&&!pickup&&!bottle&&!finishing&&!spindle)return false;
        var reserve=b;
        if(OperatorAccessBounds is Aabb a)reserve=reserve.Merge(new Aabb(a.Position-Position,a.Size));
        var c=reserve.GetCenter();var w=reserve.Size.X+4.4f;var dep=reserve.Size.Z+4.4f;
        var h=inbound?3.3f:coolant?3.1f:pickup?3.6f:bottle?3.2f:finishing?3.8f:3.7f;
        var rearZ=c.Z+sz*dep/2;var sideX=c.X+sx*w/2;
        var wall=StudioSceneMaterials.CreateDetailed(inbound?"#CEC4AE":coolant?"#CBD3C7":bottle?"#D9DCD0":finishing?"#BCBDA8":spindle?"#B49B80":"#B6BCAD","paint");
        var frame=StudioSceneMaterials.CreateDetailed(inbound?"#637A83":coolant?"#708F85":bottle?"#73948D":finishing?"#547B73":spindle?"#79857D":"#AA8856","paint");
        var floor=StudioSceneMaterials.CreateDetailed(spindle?"#90978E":coolant?"#A2AAA2":bottle?"#A8B7AD":"#A4A59A",coolant||bottle?"paint":"concrete");
        var timber=StudioSceneMaterials.CreateDetailed("#B69261","wood");
        var stainless=StudioSceneMaterials.CreateDetailed("#B9C2BE","metal");
        var paper=StudioSceneMaterials.CreateDetailed("#DDD5BE","cardboard");
        architecture.SetMeta("productionReserveLocal",reserve);
        architecture.SetMeta("productionLocationName",d.LocationLabel);
        if(GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is {} old)old.Visible=false;
        Box(architecture,"ContinuousProductionFoundation",new Vector3(w+4,.12f,dep+4),new Vector3(c.X,-.065f,c.Z),floor);
        Node3D Face(string name,Vector3 at,float yaw=0)
        {var n=new Node3D{Name=name,Position=at,RotationDegrees=new Vector3(0,yaw,0)};architecture.AddChild(n);return n;}
        var rear=Face("ProductionRear",new Vector3(c.X,0,rearZ));
        var side=Face("ProductionSide",new Vector3(sideX,0,c.Z),90);
        void Wall(Node3D n,float len,float height,List<Rect2> holes,bool glaze=false)=>ArtWall(n,len,height,.18f,wall,holes,glaze);
        void Cap(Node3D n,float len,float height)=>Box(n,"ContinuousSupportedCap",new Vector3(len+.18f,.20f,.30f),new Vector3(0,height-.10f,0),frame);
        Node3D Annex(Node3D parent,string name,float offset,float aw,float ad,int outward,bool cutawayRoof=false,bool separateRearEntry=false)
        {
            var n=new Node3D{Name=name,Position=new Vector3(offset,0,0)};parent.AddChild(n);
            Box(n,"ConnectedAnnexFoundation",new Vector3(aw+.3f,.04f,ad+.3f),new Vector3(0,-.025f,outward*ad/2),floor);
            foreach(var end in new[]{-1,1})Box(n,"GroundedConnectedReturn",new Vector3(.18f,h,ad),new Vector3(end*aw/2,h/2,outward*ad/2),wall);
            if(!separateRearEntry)Box(n,"AnnexBackWall",new Vector3(aw+.18f,h,.18f),new Vector3(0,h/2,outward*ad),wall);
            if(!cutawayRoof)Box(n,"SupportedAnnexRoof",new Vector3(aw+.25f,.16f,ad+.20f),new Vector3(0,h,outward*ad/2),frame);
            n.SetMeta("resolvedAnnexBounds",new Aabb(new Vector3(-aw/2,0,MathF.Min(0,outward*ad)),new Vector3(aw,h,ad)));
            return n;
        }
        void Fixture(Node3D n,float px,int outward)
        {
            Box(n,"WallFixtureSupport",new Vector3(.12f,.18f,.32f),new Vector3(px,h-.20f,-outward*.13f),frame);
            var diffuser=(StandardMaterial3D)StudioSceneMaterials.CreateDetailed("#E2DED0","paint").Duplicate();diffuser.EmissionEnabled=true;diffuser.Emission=new Color("E2DED0").SrgbToLinear();diffuser.EmissionEnergyMultiplier=.3f;
            Box(n,"DiffuseTaskFixture",new Vector3(1.2f,.08f,.32f),new Vector3(px,h-.35f,-outward*.18f),diffuser);
            n.AddChild(new OmniLight3D{Name="PassiveProductionLight",Position=new Vector3(px,h-.46f,-outward*.30f),LightEnergy=.20f,OmniRange=8,ShadowEnabled=true});
        }
        if(pickup)
        {
            // Open shipping apron differs from enclosed receiving: short side
            // building and a rear canopy, leaving both route ends exposed.
            Wall(rear,w,1.15f,new());
            var sideLength=3.2f;
            var dock=Face("ConnectedShippingDock",new Vector3(sideX,0,rearZ-sz*sideLength/2),90);
            Wall(dock,sideLength,h,new(){new Rect2(-1.25f,0,2.5f,2.7f)});
            var annex=Annex(dock,"ShippingServiceBay",0,2.8f,1.6f,sx);
            Box(annex,"ClosedDockServiceCupboard",new Vector3(.8f,1.3f,.30f),new Vector3(.6f,.65f,sx*1.35f),timber);
            Box(rear,"ShippingCanopy",new Vector3(w,.18f,1.1f),new Vector3(0,h,sz*.25f),frame);
            Box(rear,"CanopyBearingGirder",new Vector3(w,.30f,.30f),new Vector3(0,h-.15f,sz*.25f),frame);
            foreach(var px in new[]{-w/2,0,w/2})Box(rear,"GroundedCanopyColumn",new Vector3(.24f,h,.24f),new Vector3(px,h/2,sz*.25f),frame);
            Fixture(dock,0,sx);
        }
        else
        {
            var entry=sz*(dep/2-1.5f);var service=-sz*dep*.20f;
            var rh=new List<Rect2>();var sh=new List<Rect2>();
            if(!inbound&&!coolant&&!bottle&&!finishing)sh.Add(new Rect2(entry-.6f,0,1.2f,2.2f));
            else rh.Add(new Rect2(w*.30f-.6f,0,1.2f,2.2f));
            Node3D annex;var outward=sx;float ad;
            if(spindle)
            {
                // Two tool-reference wings flank one wide rear fixture bay.
                rh.Add(new Rect2(-1.1f,0,2.2f,2.5f));
                foreach(var px in new[]{-w*.28f,w*.28f})rh.Add(new Rect2(px-.8f,2.55f,1.6f,.60f));
                annex=Annex(rear,"TwinFixturePreparationBay",0,2.6f,1.1f,sz);outward=sz;ad=1.1f;
                foreach(var px in new[]{-.52f,.52f})Box(annex,"PassiveToolReferenceBoard",new Vector3(.85f,.75f,.06f),new Vector3(px,1.5f,sz*.95f),timber);
            }
            else if(inbound)
            {
                sh.Add(new Rect2(service-1.6f,.85f,3.2f,h-.85f));
                annex=Annex(side,"IdentificationObservationBooth",service,3.6f,1.4f,sx,true,true);ad=1.4f;
                var observationBack=new Node3D{Name="ObservationRoomExternalEntry",Position=new Vector3(0,0,sx*1.4f)};annex.AddChild(observationBack);
                ArtWall(observationBack,3.6f,h,.18f,wall,new(){new Rect2(-.6f,0,1.2f,2.2f)},false);
                Box(annex,"ObservationCutawayRoof",new Vector3(3.85f,.16f,.40f),new Vector3(0,h,sx*1.2f),frame);
                Box(annex,"PassiveIdentificationReferenceTop",new Vector3(.70f,.08f,.45f),new Vector3(1.15f,.90f,sx*.90f),timber);
                foreach(var leg in new[]{.85f,1.45f})Box(annex,"ReferenceTopBearing",new Vector3(.10f,.86f,.35f),new Vector3(leg,.43f,sx*.90f),frame);
                rh.Add(new Rect2(-w*.20f-1.2f,2.3f,2.4f,.60f));
                // Personnel entry is separately retained toward the open front;
                // observation glazing never occupies that doorway.
            }
            else if(bottle)
            {
                rh.Add(new Rect2(-1.3f,1.0f,2.6f,1.2f));
                annex=Annex(rear,"BottleObservationGallery",0,3.2f,1.2f,sz);ad=1.2f;outward=sz;
                sh.Add(new Rect2(-1.4f,2.25f,2.8f,.55f));
                Box(annex,"ClosedCleaningServiceCupboard",new Vector3(.60f,1.4f,.25f),new Vector3(.95f,.7f,sz*1.0f),stainless);
            }
            else if(coolant)
            {
                rh.Add(new Rect2(-1.0f,.75f,2.0f,h-.75f));
                annex=Annex(rear,"WetServiceRecess",0,2.2f,1.2f,sz,true);
                Box(annex,"WetServiceRoofRearBearing",new Vector3(2.45f,.16f,.40f),new Vector3(0,h,sz*1.0f),frame);ad=1.2f;outward=sz;
                rh.Add(new Rect2(-w*.20f-1,2.3f,2,.5f));
                Box(annex,"PassiveSplashResistantBoard",new Vector3(1.1f,.8f,.06f),new Vector3(0,1.45f,sz*1.05f),stainless);
            }
            else
            {
                rh.Add(new Rect2(-1.5f,0,3,2.5f));
                rh.Add(new Rect2(-w*.25f-1.5f,2.65f,3,.6f));
                rh.Add(new Rect2(w*.25f-1.5f,2.65f,3,.6f));
                annex=Annex(rear,"LabelInspectionServiceAnnex",0,3.4f,1.4f,sz,true);outward=sz;ad=1.4f;
                Box(annex,"VisibleServiceGalleryRoofCutaway",new Vector3(3.65f,.16f,.45f),new Vector3(0,h,sz*1.20f),frame);
                foreach(var px in new[]{-.95f,.95f})Box(annex,"ClosedStationServiceCupboard",new Vector3(.65f,1.5f,.30f),new Vector3(px,.75f,sz*1.15f),stainless);
            }
            ArtWall(rear,w,h,.18f,coolant?StudioSceneMaterials.CreateDetailed("#869F98","paint"):wall,rh,!coolant);
            var sideSpan=coolant?3.4f:bottle?4.4f:finishing?5.5f:dep;
            if(coolant||bottle||finishing)side.Position=new Vector3(sideX,0,rearZ-sz*sideSpan/2);
            ArtWall(side,sideSpan,h,.18f,finishing?StudioSceneMaterials.CreateDetailed("#73958B","paint"):inbound?StudioSceneMaterials.CreateDetailed("#A98670","paint"):wall,sh,bottle);
            Cap(rear,w,h);Cap(side,sideSpan,h);
            var cut=Face("ConnectedShortFrontCutaway",new Vector3(c.X-sx*w/2,0,rearZ-sz*.9f),90);Wall(cut,1.8f,h,new());Cap(cut,1.8f,h);
            Fixture(rear,-w*.28f,sz);if(finishing||spindle)Fixture(rear,w*.28f,sz);
            var boardX=spindle?w*.25f:-w*.28f;
            Box(rear,"PurposeInformationBackplate",new Vector3(2.2f,.75f,.06f),new Vector3(boardX,1.55f,-sz*.14f),paper);
            ProductionNotice(rear,inbound?"TOTE IDENTIFICATION":coolant?"COOLANT SERVICE":bottle?"BOTTLE TRANSFER":finishing?"FINISH / INSPECT":"TWIN FIXTURE PREPARATION",new Vector3(boardX,1.55f,-sz*.20f),sz,24,.003f);
            // Metadata states passive context dimensions, never commissioned
            // clearance or a new functional material-transfer connection.
            annex.SetMeta("passiveContextDepth",ad);annex.SetMeta("outwardAxisSign",outward);
        }
        return true;
    }
}






