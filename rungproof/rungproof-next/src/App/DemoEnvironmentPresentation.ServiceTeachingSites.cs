using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Complete passive locations for service, calculation and field lessons.
    // Actual shutter limits, CROSS head, numeric actions and fixture stay put.
    private bool TryBuildServiceTeachingSite(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        var shutter = d.LayoutVariant == "shutter-service-return";
        var inspection = d.LayoutVariant == "inspection-low-side-window";
        var crossing = d.LayoutVariant == "crossing-sidewalk-edge";
        var sumCount = d.LayoutVariant == "sum-count-paired-height-bays";
        var function = d.LayoutVariant == "function-corner-service-recess";
        var volume = d.LayoutVariant == "volume-measurement-lightwell";
        if (!shutter && !inspection && !crossing && !sumCount && !function && !volume) return false;
        var bounds = b;
        Aabb? access = null;
        if (OperatorAccessBounds is { } a)
        {
            access = new Aabb(a.Position - Position, a.Size);
            bounds = bounds.Merge(access.Value);
        }
        var center = bounds.GetCenter();var cu = sx * center.X;var cv = sz * center.Z;
        var w = MathF.Max(shutter || volume ? 9 : sumCount ? 8.8f : function ? 8 : crossing ? 10 : 5.8f, bounds.Size.X + 3.6f);
        var dep = MathF.Max(shutter || volume ? 7 : function ? 6.8f : inspection ? 6.6f : crossing ? 10 : 6.2f, bounds.Size.Z + 3.6f);
        var l = cu - w / 2;var r = cu + w / 2;var f = cv - dep / 2;var v = cv + dep / 2;
        Vector3 At(float u,float y,float z) => new(sx*u,y,sz*z);
        var wall=EnvironmentDetailedSurface(shutter ? "#B8BAB3" : inspection ? "#DDD8CA" : crossing ? "#AA8777" : sumCount ? "#DAD6CA" : function ? "#D3DDD9" : "#AE9874",volume ? "wood" : "plaster");
        var feature=EnvironmentDetailedSurface(shutter ? "#6F8C88" : inspection ? "#BDA784" : crossing ? "#BAC3BF" : sumCount ? "#918892" : function ? "#7E9F9A" : "#667A77",inspection ? "wood" : crossing || volume ? "metal" : "paint");
        var floor=EnvironmentDetailedSurface(shutter ? "#8B9190" : inspection ? "#788991" : crossing ? "#ADB4B1" : sumCount ? "#A7947A" : function ? "#929D99" : "#B6B8AC",sumCount ? "wood" : inspection || function ? "paint" : "concrete");
        var metal=EnvironmentDetailedSurface("#A0ACA7","metal");
        var ivory=EnvironmentDetailedSurface("#E7E2D6","paint");
        Material visibleFloor=floor;
        if(crossing)
        {
            // One finish shader, no overlapping asphalt/concrete floor planes.
            // Regional travel is passive context; authored road marks remain.
            // Authored road/sidewalk continuation already supplies the actual
            // measured road datum. A generic shader band invented two black
            // disconnected road ends; keep the surrounding precinct mineral.
            visibleFloor=floor;
        }
        if(GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } ground) ground.MaterialOverride=visibleFloor;
        var extraU=inspection ? 1.2f : function ? 1.6f : crossing ? 2 : 0;
        var extraV=shutter ? 1.8f : sumCount ? 1.4f : function ? 1.1f : crossing ? 2.4f : 0;
        Box(architecture,"ServiceSiteContinuousFoundation",new Vector3(w+extraU+.4f,.12f,dep+extraV+.4f),At(cu+extraU/2,-.07f,cv+extraV/2),visibleFloor);
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
        var h=shutter ? MathF.Max(4.2f,bounds.End.Y+.8f) : sumCount ? 3.2f : function ? 3 : inspection ? 2.9f : 3.3f;
        if(shutter)
        {
            Wall("ShutterServiceAnnexBack",l,v+1.8f,l+2.6f,v+1.8f,h,feature);
            Wall("ShutterServiceAnnexLeft",l,v,l,v+1.8f,h,feature);
            Wall("ShutterServiceAnnexRight",l+2.6f,v,l+2.6f,v+1.8f,h,feature);
            Wall("ShutterMainRear",l+2.6f,v,r,v,h,wall,window:(l+2.6f+r)/2,ww:2.3f,wy:1.2f,wh:1.5f);
            Wall("ShutterPersonnelSide",r,f,r,v,h,wall,doorV);
            Box(architecture,"ShutterAnnexBearingRoof",new Vector3(2.76f,.12f,1.96f),At(l+1.3f,h+.06f,v+.9f),metal);
            Board("ShutterCommandReference",l+1.3f,v+1.8f,"OPEN / STOP / CLOSE\nNC limits remain simulated\nIndependent service doorway",2.2f);
        }
        else if(inspection)
        {
            var mouth0=cv-.8f;var mouth1=cv+1.2f;
            Wall("InspectionRear",l,v,r,v,h,wall);
            Wall("InspectionEntrySide",r,f,r,mouth0,h,wall,doorV);
            Wall("InspectionDaylightFront",r,mouth0,r+1.2f,mouth0,h,feature);
            Wall("InspectionDaylightOuter",r+1.2f,mouth0,r+1.2f,mouth1,h,feature,window:(mouth0+mouth1)/2,ww:1.6f,wy:.7f,wh:1.8f);
            Wall("InspectionDaylightBack",r,mouth1,r+1.2f,mouth1,h,feature);
            Wall("InspectionSideToCorner",r,mouth1,r,v,h,wall);
            Board("InspectionToggleReference",cu,v,"SPRING-RETURN TOGGLE\ntoggle_memory\ninspection_light_on",2.1f);
            Box(architecture,"PassiveMatteInspectionSample",new Vector3(1.3f,.75f,.05f),At(l+1,1.5f,v-.115f),ivory);
            Box(architecture,"InspectionSampleTimberBearing",new Vector3(1.5f,.10f,.22f),At(l+1,1.05f,v-.14f),feature);
        }
        else if(crossing)
        {
            // A permanent connected street corner, not another roadwork tray.
            // The field CROSS fixture and poles are untouched inside B.
            var rearH=3.4f;var facade0=r-8;var facade1=r;var facadeCenter=r-4;
            Wall("PedestrianStreetFrontage",facade0,v,facade1,v,rearH,wall,door:facadeCenter-1.8f,window:facadeCenter+1.2f,ww:2.0f,wy:.65f,wh:1.8f);
            // Complete shallow precinct building, connected corners and a
            // bearing roof behind the facade. Entire occupied volume is on
            // the outward rear side of protected field + standing bounds.
            Wall("PedestrianFrontageLeftReturn",facade0,v,facade0,v+2.2f,rearH,wall);
            Wall("PedestrianFrontageRightReturn",facade1,v,facade1,v+2.2f,rearH,wall);
            Wall("PedestrianFrontageRear",facade0,v+2.2f,facade1,v+2.2f,rearH,wall);
            Box(architecture,"StreetFrontageBearingRoof",new Vector3(8.16f,.12f,2.36f),At(facadeCenter,rearH+.06f,v+1.1f),feature);
            // A continuous peripheral raised pavement beyond the field/motion
            // envelope; no new curb or ramp crosses existing road/poles.
            Box(architecture,"FrontagePeripheralPavement",new Vector3(8.16f,.08f,1.6f),At(facadeCenter,-.005f,v-.88f),floor);
            // Butt join without coplanar overlap; one continuous side return
            // leads around the building rather than a detached pavement tile.
            Box(architecture,"FrontageConnectedCornerPavement",new Vector3(1.4f,.08f,2.44f),At(facade1+.78f,-.005f,v+1.14f),floor);
            architecture.SetMeta("pedestrianFieldApproachScope","Authored CROSS remains field-mounted. No unmeasured route from cabinet A to CROSS is asserted; native actual fixture bounds required.");
        }
        else if(sumCount)
        {
            var lo=l+.6f;var hi=r-.6f;
            Wall("ComputationRearLeftPier",l,v,lo,v,h,wall);
            Wall("ComputationGalleryLeft",lo,v,lo,v+1.4f,h,feature);
            Wall("ComputationGalleryRearLeft",lo,v+1.4f,cu-.65f,v+1.4f,h,wall,window:(lo+cu-.65f)/2,ww:1.6f,wy:1.1f,wh:1.6f);
            Wall("ComputationGalleryRearPier",cu-.65f,v+1.4f,cu+.65f,v+1.4f,h,feature);
            Wall("ComputationGalleryRearRight",cu+.65f,v+1.4f,hi,v+1.4f,h,wall,window:(cu+.65f+hi)/2,ww:1.6f,wy:1.1f,wh:1.6f);
            Wall("ComputationGalleryRight",hi,v,hi,v+1.4f,h,feature);
            Wall("ComputationRearRightPier",hi,v,r,v,h,wall);
            Wall("ComputationEntrySide",r,f,r,v,h,wall,doorV);
            Wall("ComputationInternalReviewDivider",cu,v+.4f,cu,v+1.4f,2.2f,feature);
            // The divider is the role witness; do not cover either glazed bay
            // with fake numeric results or an overlapping broad backboard.
            Board("ComputationOwnership",cu,v+.4f,"SUM / COUNT\nPLC outputs",1.05f);
            Box(architecture,"ComputationDividerPlateBracket",new Vector3(.15f,.35f,.20f),At(cu,1.55f,v+.415f),metal);
        }
        else if(function)
        {
            var mouth=cv+.3f;
            Wall("FunctionMainRear",l,v,r,v,h,wall);
            Wall("FunctionEntrySide",r,f,r,mouth,h,wall,doorV);
            Wall("FunctionCornerFront",r,mouth,r+1.6f,mouth,h,feature);
            Wall("FunctionOuterDaylight",r+1.6f,mouth,r+1.6f,v+1.1f,h,feature,window:(mouth+v+1.1f)/2,ww:MathF.Min(2.8f,v+1.1f-mouth-.3f),wy:.6f,wh:2);
            Wall("FunctionCornerBack",r,v+1.1f,r+1.6f,v+1.1f,h,feature,
                window:r+.8f,ww:1.1f,wy:.6f,wh:2);
            Wall("FunctionBackJoin",r,v,r,v+1.1f,h,wall);
            Board("FunctionChoiceReference",cu,v,"ONE SELECTED FUNCTION\n1 SUM / 2 PRODUCT\nLoaded ladder owns RESULT",2.2f);
        }
        else
        {
            Wall("MetrologyGlazedRear",l,v,r,v,3.3f,wall,window:cu,ww:w-1.2f,wy:1,wh:1.9f);
            Wall("MetrologyGlazedCorner",r,v-2.4f,r,v,3.3f,wall,window:v-1.2f,ww:1.9f,wy:1,wh:1.9f);
            var roofH=MathF.Max(3.5f,bounds.End.Y+.8f);
            foreach(var u in new[]{l+.3f,r-.3f})
            {
                Box(architecture,"MetrologyRoofPost",new Vector3(.18f,roofH,.18f),At(u,roofH/2,v-.9f),feature);
                Box(architecture,"MetrologyGroundFoot",new Vector3(.34f,.04f,.34f),At(u,.02f,v-.9f),feature);
                Box(architecture,"MetrologyCrossBearing",new Vector3(.18f,.18f,1.2f),At(u,roofH-.09f,v-.6f),feature);
            }
            Box(architecture,"MetrologyRearBearingHeader",new Vector3(w,.18f,.20f),At(cu,roofH-.09f,v-.9f),feature);
            Box(architecture,"MetrologyRearDaylightCanopy",new Vector3(w,.10f,1.2f),At(cu,roofH+.05f,v-.6f),metal);
            // Solid timber sill carries a grid-free dimension outline. The
            // existing physical box and readouts provide all numeric values.
            Box(architecture,"StaticDimensionOutlineHorizontal",new Vector3(1.6f,.03f,.03f),At(cu,.62f,v-.115f),feature);
            foreach(var u in new[]{cu-.8f,cu+.8f})
                Box(architecture,"StaticDimensionOutlineVertical",new Vector3(.03f,.4f,.03f),At(u,.42f,v-.115f),feature);
            architecture.SetMeta("metrologyScope","Static carrying fixture/manual dimensions retained. No automated or calibrated measurement claimed.");
        }
        if(!crossing && access is { } standing)
        {
            var target=standing.GetCenter();target.Y=0;target.Z=sz>0?standing.Position.Z:standing.End.Z;
            var points=new Godot.Collections.Array<Vector3>();
            if(volume) points.Add(At(r-.8f,0,f+.8f));
            else {points.Add(At(r-.8f,0,doorV));points.Add(At(r-.8f,0,f+.8f));}
            points.Add(new Vector3(target.X,0,sz*(f+.8f)));points.Add(target);
            var route=new Node3D{Name="ServiceSiteActualStandingApproach"};
            route.SetMeta("localApproachPolyline",points);route.SetMeta("proposedSweepWidth",1.2f);
            route.SetMeta("localTerminalStandingRegion",standing);architecture.AddChild(route);
        }
        architecture.SetMeta("purposeLayoutImplemented",true);
        architecture.SetMeta("serviceTeachingExactLayout",d.LayoutVariant);
        architecture.SetMeta("teachingSuiteContextOnly",true);
        return true;
    }
}
