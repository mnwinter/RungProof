using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Whole dry-side context around the authored four-vessel lesson. All new
    // objects are passive presentation; no hypothetical treatment stage or
    // process pipe is added. Coordinates below remain in the authored world axes.
    private void ArtWastewaterSite(Node3D architecture)
    {
        var b = ProtectedProcessBounds;
        var wall = StudioSceneMaterials.Create("#879D9A", "paint");
        var roof = StudioSceneMaterials.Create("#53665F", "metal");
        var slab = StudioSceneMaterials.Create("#A29F90", "concrete");
        var trim = StudioSceneMaterials.Create("#687A6D", "paint");
        var cabinet = StudioSceneMaterials.Create("#D7D5C5", "paint");
        Vector3 Local(float x, float y, float z) => new Vector3(x, y, z) - Position;
        void WorldBox(string name, Vector3 size, float x, float y, float z, Material material)
            => Box(architecture, name, size, Local(x, y, z), material);

        var buildingLeft = b.Position.X - 4.25f;
        var buildingRight = b.Position.X - .65f;
        var buildingFront = MathF.Max(5.8f, b.End.Z + .8f);
        var buildingRear = buildingFront - 4.8f;
        var buildingCenterX = (buildingLeft + buildingRight) / 2;
        var buildingCenterZ = (buildingRear + buildingFront) / 2;
        const float wallHeight = 3.0f;
        const float thickness = .18f;
        var doorCenter = buildingLeft + 1.95f;
        const float doorWidth = 1.2f;
        var frontCorridor = MathF.Max(b.End.Z + 1.5f, buildingFront + .8f);
        var apronLeft = buildingLeft - .25f;
        var apronRight = b.End.X + 2.2f;
        var apronRear = MathF.Min(b.Position.Z - 1.8f, buildingRear - .25f);
        var apronFront = frontCorridor + .8f;
        WorldBox("TreatmentContinuousApron", new Vector3(apronRight - apronLeft, .12f, apronFront - apronRear),
            (apronLeft + apronRight) / 2, -.065f, (apronRear + apronFront) / 2, slab);

        Node3D Face(string name, float x, float z, float yaw)
        {
            var node = new Node3D { Name = name, Position = Local(x, 0, z), RotationDegrees = new Vector3(0, yaw, 0) };
            architecture.AddChild(node);
            return node;
        }
        var front = Face("DryControlBuildingFront", buildingCenterX, buildingFront, 0);
        ArtWall(front, buildingRight - buildingLeft, wallHeight, thickness, wall,
            new List<Rect2> { new(doorCenter - buildingCenterX - doorWidth / 2, 0, doorWidth, 2.2f) }, false);
        ArtWall(Face("DryControlBuildingRear", buildingCenterX, buildingRear, 0),
            buildingRight - buildingLeft, wallHeight, thickness, wall, new List<Rect2>(), false);
        foreach (var x in new[] { buildingLeft, buildingRight })
            ArtWall(Face("DryControlBuildingReturn", x, buildingCenterZ, 90),
                buildingFront - buildingRear, wallHeight, thickness, wall, new List<Rect2>(), false);
        WorldBox("ControlBuildingSupportedRoof", new Vector3(buildingRight - buildingLeft + .30f, .16f, buildingFront - buildingRear + .30f),
            buildingCenterX, wallHeight + .08f, buildingCenterZ, roof);
        foreach (var x in new[] { buildingLeft, buildingRight })
            WorldBox("ControlBuildingCornerTrim", new Vector3(.12f, wallHeight, .12f), x, wallHeight / 2, buildingFront, trim);
        WorldBox("ControlBuildingDoorLintel", new Vector3(doorWidth + .12f, .10f, .24f),
            doorCenter, 2.30f, buildingFront, trim);
        WorldBox("PassiveServiceBox", new Vector3(.46f, .64f, .20f),
            buildingLeft + .45f, 1.38f, buildingFront + .19f, cabinet);
        WorldBox("PassiveServiceRaceway", new Vector3(.06f, 1.0f, .06f),
            buildingLeft + .45f, .56f, buildingFront + .14f, trim);
        WorldBox("ServiceDoorLuminaire", new Vector3(.65f, .07f, .22f),
            doorCenter, 2.56f, buildingFront + .18f, cabinet);
        architecture.AddChild(new OmniLight3D
        {
            Name = "DryServiceFrontageLight", Position = Local(doorCenter, 2.44f, buildingFront + .28f),
            LightColor = new Color("FFF4DE"), LightEnergy = .20f, OmniRange = 4.0f, ShadowEnabled = true,
        });
        var sign = new Label3D
        {
            Name = "CollectionSiteContext", Text = "COLLECTION / DRY SERVICE", FontSize = 28,
            PixelSize = .003f, OutlineSize = 0, Modulate = new Color("263A31"),
            Position = Local(buildingCenterX, 2.76f, buildingFront + .10f),
        };
        architecture.AddChild(sign);

        // These quiet broad surfaces show the continuous dry-side route. They
        // are flush, not raised steps, stripes or invented safety markings.
        var outsideRight = b.End.X + .9f;
        var access = OperatorAccessBounds ?? throw new InvalidOperationException("Wastewater site requires the actual operator standing reserve.");
        var terminal = access.GetCenter();
        var rearCurbZ = b.Position.Z - 1.4f;
        WorldBox("YardRearCurb", new Vector3(apronRight - buildingRight, .16f, .18f),
            (apronRight + buildingRight) / 2, .08f, rearCurbZ, trim);
        WorldBox("YardRightCurbReturn", new Vector3(.18f, .16f, 1.8f),
            apronRight, .08f, rearCurbZ + .90f, trim);
        architecture.SetMeta("dryFrontageRouteBounds", new Aabb(
            Local(doorCenter, 0, frontCorridor - .6f), new Vector3(apronRight - doorCenter, 2.0f, 1.2f)));
        architecture.SetMeta("siteRouteScope", "Static dry frontage outside conservative process bounds; terminal operator approach requires actual field/access review.");
        void RouteMarker(string name, Aabb bounds)
        {
            var marker = new Node3D { Name = name };
            marker.SetMeta("localRouteBounds", new Aabb(bounds.Position - Position, bounds.Size));
            architecture.AddChild(marker);
        }
        RouteMarker("DryFrontageRouteReserve", new Aabb(new Vector3(doorCenter - .6f, 0, frontCorridor - .6f),
            new Vector3(apronRight - doorCenter + .6f, 2.0f, 1.2f)));
        RouteMarker("DoorApproachRouteReserve", new Aabb(new Vector3(doorCenter - .6f, 0, buildingFront + .12f),
            new Vector3(1.2f, 2.0f, frontCorridor - buildingFront + .48f)));
        RouteMarker("OperatorOuterRouteReserve", new Aabb(new Vector3(outsideRight - .6f, 0, terminal.Z - .6f),
            new Vector3(1.2f, 2.0f, frontCorridor - terminal.Z + 1.2f)));
        RouteMarker("OperatorTerminalRouteReserve", new Aabb(new Vector3(terminal.X - .6f, 0, terminal.Z - .6f),
            new Vector3(outsideRight - terminal.X + 1.2f, 2.0f, 1.2f)));
    }
}
