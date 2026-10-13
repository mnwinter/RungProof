using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Data/drive training and dry utility surroundings. Numeric fixture values
    // do not acquire new units; no new pumping/level/controller logic lives here.
    private bool TryBuildUtilityTeachingSite(SceneEnvironmentArtDirection d,Aabb b,
        int sx,int sz,Node3D architecture)
    {
        var state=d.LayoutVariant=="motor-state-single-bay";
        var data=d.LayoutVariant=="motor-data-window-return";
        var array=d.LayoutVariant=="ten-drive-long-hall";
        var pallet=d.LayoutVariant=="pallet-count-open-dock-header";
        var discrete=d.LayoutVariant=="point-level-low-plant-room";
        var analog=d.LayoutVariant=="analog-tank-clerestory-room";
        if(!state&&!data&&!array&&!pallet&&!discrete&&!analog)return false;
        var bounds=b; Aabb? access=null;
        if(OperatorAccessBounds is { } a){access=new Aabb(a.Position-Position,a.Size);bounds=bounds.Merge(access.Value);}
        var center=bounds.GetCenter();var cu=sx*center.X;var cv=sz*center.Z;
        var w=MathF.Max(array?8:pallet?12:discrete||analog?11:data?9:7.6f,bounds.Size.X+3.6f);
        var dep=MathF.Max(array?6.5f:pallet?8:discrete||analog?9:data?8:7,bounds.Size.Z+3.6f);
        var l=cu-w/2;var r=cu+w/2;var f=cv-dep/2;var v=cv+dep/2;
        Vector3 At(float u,float y,float z)=>new(sx*u,y,sz*z);
        var wall=EnvironmentDetailedSurface(state?"#B3C0BF":data?"#D3CBBE":array?"#CBCDBF":pallet?"#C5B9A4":discrete?"#A9ADA2":"#C1D0CE",array?"metal":"plaster");
        var feature=EnvironmentDetailedSurface(state?"#667F85":data?"#A48B6D":array?"#758D83":pallet?"#7F939C":discrete?"#819085":"#738F96",data?"wood":"paint");
        var floor=EnvironmentDetailedSurface(state?"#7D8C8B":data?"#A59784":array?"#A6AAA2":pallet?"#A19E92":discrete?"#A8ADA5":"#A6B0AE",state?"paint":data?"wood":"concrete");
        var metal=EnvironmentDetailedSurface("#A0AAA6","metal");
        var ivory=EnvironmentDetailedSurface("#E5E2D8","paint");
        if(GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } ground)ground.MaterialOverride=floor;
        var ex=data?1.5f:analog?1.6f:0;
        var ez=state?1.0f:pallet?2:discrete?1.6f:0;
        Box(architecture,"UtilityTeachingContinuousFoundation",new Vector3(w+ex+.4f,.12f,dep+ez+.4f),At(cu+ex/2,-.07f,cv+ez/2),floor);
        void Wall(string name, float u0, float z0, float u1, float z1, float h,
            StandardMaterial3D material, float? door = null, float? window = null,
            float ww = 1.4f, float wy = 1.25f, float wh = 1)
        {
            var alongX = MathF.Abs(z1 - z0) < .001f;
            var len = alongX ? MathF.Abs(u1 - u0) : MathF.Abs(z1 - z0);
            var middle = alongX ? (u0 + u1) / 2 : (z0 + z1) / 2;
            var sign = alongX ? sx : -sz;
            var node = new Node3D { Name = name, Position = At((u0 + u1) / 2, 0, (z0 + z1) / 2), RotationDegrees = new Vector3(0, alongX ? 0 : 90, 0) };
            architecture.AddChild(node);
            var holes = new List<Rect2>();
            float? dx = door.HasValue ? sign * (door.Value - middle) : null;
            if (dx.HasValue)
            {
                if (MathF.Abs(dx.Value) + .68f > len / 2) throw new InvalidOperationException("Timing suite doorway outside wall: " + name);
                holes.Add(new Rect2(dx.Value - .6f, 0, 1.2f, 2.2f));
            }
            if (window.HasValue)
            {
                var wx = sign * (window.Value - middle);
                if (MathF.Abs(wx) + ww / 2 + .06f > len / 2 || wy + wh > h)
                    throw new InvalidOperationException("Timing suite glazing outside wall: " + name);
                holes.Add(new Rect2(wx - ww / 2, wy, ww, wh));
            }
            ArtWall(node, len, h, .16f, material, holes, true);
            Box(node, "BearingCornice", new Vector3(len - .02f, .08f, .20f), new Vector3(0, h - .04f, 0), metal);
            void Strip(float lo, float hi)
            {
                if (hi - lo > .01f) Box(node, "SetbackSkirting", new Vector3(hi - lo, .10f, .18f), new Vector3((lo + hi) / 2, .05f, 0), metal);
            }
            if (dx.HasValue) { Strip(-len / 2 + .10f, dx.Value - .6f); Strip(dx.Value + .6f, len / 2 - .10f); }
            else Strip(-len / 2 + .10f, len / 2 - .10f);
        }
        void Board(string name, float u, float z, string text, float width = 2)
        {
            Box(architecture, name + "BearingPlate", new Vector3(width, .8f, .06f), At(u, 1.55f, z - .115f), ivory);
            architecture.AddChild(new Label3D { Name = name + "StaticLegend", Text = text, FontSize = 32, PixelSize = .0018f, OutlineSize = 0,
                Modulate = new Color("#39403B"), Position = At(u, 1.55f, z - .151f), RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0) });
        }
        var doorV=f+1.1f;
        if(state)
        {
            const float h=3.2f;
            Wall("MotorStateRearLeft",l,v,cu-1.3f,v,h,wall,window:(l+cu-1.3f)/2,ww:1.6f,wy:1.1f,wh:1.55f);
            Wall("MotorStateDiagnosticLeft",cu-1.3f,v,cu-1.3f,v+1,h,feature);
            Wall("MotorStateDiagnosticBack",cu-1.3f,v+1,cu+1.3f,v+1,h,feature);
            Wall("MotorStateDiagnosticRight",cu+1.3f,v,cu+1.3f,v+1,h,feature);
            Wall("MotorStateRearRight",cu+1.3f,v,r,v,h,wall);
            Wall("MotorStateEntry",r,f,r,v,h,wall,doorV);
            Board("EnumStateReference",cu,v+1,"STOPPED / RUNNING / FAULT\nLatched fault / fresh Start\nLoaded ENUM logic owns state",2.2f);
            Box(architecture,"MotorStateSupportedReviewSill",new Vector3(2.2f,.08f,.3f),At(cu,.95f,v+.8f),feature);
            foreach(var u in new[]{cu-.9f,cu+.9f})
                Box(architecture,"ReviewSillBearing",new Vector3(.12f,.91f,.28f),At(u,.455f,v+.8f),metal);
        }
        else if(data)
        {
            var h=MathF.Max(3.8f,bounds.End.Y+.65f);
            var mouth=cv-.2f;
            Wall("StructMainRear",l,v,r+1.5f,v,h,wall);
            Wall("StructEntrySide",r,f,r,mouth,h,wall,doorV);
            Wall("StructObservationFront",r,mouth,r+1.5f,mouth,h,feature);
            Wall("StructObservationOuter",r+1.5f,mouth,r+1.5f,v,h,feature,window:(mouth+v)/2,ww:MathF.Min(3.2f,v-mouth-.3f),wy:.9f,wh:1.8f);
            Board("StructFixtureProvenance",cu,v,"MOTOR RECORD / COMMAND\nFixture values: units unspecified\nNo physical temperature model",2.7f);
            Box(architecture,"StructGalleryRearCeilingBearing",new Vector3(1.5f,.15f,.18f),At(r+.75f,h-.08f,v-.12f),metal);
        }
        else if(array)
        {
            // A linked ten-zone conveyor test cell, sized from the complete
            // authored field and operator approach rather than ten display rigs.
            var h=MathF.Max(3.6f,bounds.End.Y+.8f);
            var rear=new Node3D{Name="ConveyorArrayDispatchRear",Position=At(cu,0,v)};
            architecture.AddChild(rear);
            var holes=new List<Rect2>{
                new(-w*.25f-1.0f,h-1.05f,2.0f,.65f),
                new(w*.25f-1.0f,h-1.05f,2.0f,.65f)};
            IndustrialMaterialWall(rear,w,h,holes,1.15f,
                EnvironmentDetailedSurface("#849C90","paint"),
                EnvironmentDetailedSurface("#CBCDBF","paint"));
            Wall("ConveyorArrayPersonnelReturn",r,f,r,v,h,wall,doorV);
            foreach(var u in new[]{l+.15f,cu,r-.15f})
                Box(architecture,"DispatchRearGroundedBearing",new Vector3(.22f,h,.28f),At(u,h/2,v-.10f),feature);
            Box(architecture,"DispatchRearBearingHeader",new Vector3(w,.24f,.30f),At(cu,h-.12f,v-.10f),feature);
            Board("ConveyorZoneCommandReference",cu,v,"TEN CONVEYOR ZONES / BOOL [0..9]\nAggregate / legacy / group commands: OR\nCount is commanded zones; no sensor feedback",3.5f);
        }
        else if(pallet)
        {
            var h=MathF.Max(3.8f,bounds.End.Y+.8f);
            // Actual outward receiving recess, with loading aperture wholly
            // independent of the manual source pallet-detection inputs.
            Wall("PalletReceivingRearLeft",l,v,cu-2,v,h,wall);
            Wall("PalletReceivingLeftReturn",cu-2,v,cu-2,v+2,h,feature);
            Wall("PalletReceivingBack",cu-2,v+2,cu+2,v+2,h,feature,window:cu,ww:2.8f,wy:1.1f,wh:1.8f);
            Wall("PalletReceivingRightReturn",cu+2,v,cu+2,v+2,h,feature);
            Wall("PalletReceivingRearRight",cu+2,v,r,v,h,wall);
            Wall("PalletPersonnelSide",r,f,r,v,h,wall,doorV);
            Box(architecture,"PalletReceivingBearingHeader",new Vector3(4.18f,.22f,.26f),At(cu,h-.11f,v+.05f),metal);
            Box(architecture,"PalletReceivingRearRoof",new Vector3(4.18f,.12f,2.18f),At(cu,h+.06f,v+1),metal);
            Board("PalletManualValidation",(l+cu-2)/2,v,"PALLET INPUTS\nDetection / type: manual\nLoaded block owns count",MathF.Min(2.5f,cu-2-l-.2f));
        }
        else if(discrete)
        {
            // Low connected yard boundary keeps both actual switch elevations
            // and the five-metre fluid vessel visible, with no repeated hut.
            Wall("DiscreteDryYardRear",l,v,r,v,1.1f,wall);
            Wall("DiscreteDryYardSide",r,cv,r,v,1.1f,wall);
            var roofH=2.8f;
            var canopy0=l+.4f;var canopy1=l+3.6f;
            foreach(var u in new[]{canopy0,canopy1})
            {
                Box(architecture,"DiscreteDryServiceGroundPost",new Vector3(.18f,roofH,.18f),At(u,roofH/2,v+1.2f),metal);
                Box(architecture,"DiscreteDryServiceFrontPost",new Vector3(.18f,roofH,.18f),At(u,roofH/2,v+.1f),metal);
            }
            foreach(var z in new[]{v+.1f,v+1.2f})
                Box(architecture,"DiscreteDryServiceBearingHeader",new Vector3(3.4f,.18f,.20f),At((canopy0+canopy1)/2,roofH-.09f,z),metal);
            Box(architecture,"DiscreteDryServiceRoof",new Vector3(3.5f,.12f,1.35f),At((canopy0+canopy1)/2,roofH+.06f,v+.65f),feature);
            // Bearing wall beyond tank envelope, not a dummy field transmitter.
            Wall("DiscreteServiceLegendBacking",canopy0,v+1.35f,canopy1,v+1.35f,2.4f,feature);
            Board("DiscreteSwitchReference",(canopy0+canopy1)/2,v+1.35f,"LSL / LSH\nSeparate point-switch elevations\nPump / drain remain authored",2.6f);
        }
        else
        {
            // Taller daylight instrument-service facade and side dry gallery;
            // there is deliberately NO roof over the analog roof probe or tank.
            const float h=3.6f;
            var mouth=cv+.2f;
            Wall("AnalogDryRearFacade",l,v,r+1.6f,v,h,wall,window:cu,ww:w-1.4f,wy:1.15f,wh:1.9f);
            Wall("AnalogPersonnelEntry",r,f,r,mouth,h,wall,doorV);
            Wall("AnalogDryGalleryFront",r,mouth,r+1.6f,mouth,h,feature);
            Wall("AnalogDryGalleryOuter",r+1.6f,mouth,r+1.6f,v,h,feature,window:(mouth+v)/2,ww:MathF.Min(2.8f,v-mouth-.3f),wy:.8f,wh:2.3f);
            // A quiet low supported interpretation strip below rear glazing.
            Box(architecture,"AnalogServiceSupportedLegendPlate",new Vector3(2.6f,.65f,.06f),At(cu,.7f,v-.115f),ivory);
            architecture.AddChild(new Label3D{Name="AnalogStaticInterpretation",Text="LINEAR 4–20 mA / 0–100%\nOriginal simulated transmitter",FontSize=30,PixelSize=.0018f,OutlineSize=0,
                Modulate=new Color("#38403C"),Position=At(cu,.7f,v-.151f),RotationDegrees=new Vector3(0,sz>0?180:0,0)});
        }
        if(access is { } standing)
        {
            var target=standing.GetCenter();target.Y=0;target.Z=sz>0?standing.Position.Z:standing.End.Z;
            var points=new Godot.Collections.Array<Vector3>();
            if(discrete)points.Add(At(r-.8f,0,f+.8f));
            else{points.Add(At(r-.8f,0,doorV));points.Add(At(r-.8f,0,f+.8f));}
            points.Add(new Vector3(target.X,0,sz*(f+.8f)));points.Add(target);
            var route=new Node3D{Name="UtilityTeachingActualDryApproach"};route.SetMeta("localApproachPolyline",points);
            route.SetMeta("proposedSweepWidth",1.2f);route.SetMeta("localTerminalStandingRegion",standing);architecture.AddChild(route);
        }
        architecture.SetMeta("purposeLayoutImplemented",true);
        architecture.SetMeta("utilityTeachingExactLayout",d.LayoutVariant);
        architecture.SetMeta("teachingSuiteContextOnly",true);
        return true;
    }
}