using System;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    /// <summary>
    /// A complete small dispatch workshop, authored in the camera-oriented local
    /// coordinates established by Create. All upright context stays outside the
    /// conservative process circle; the floor alone extends beneath equipment.
    /// These meshes have no physics, pick controls, simulation tags or I/O.
    /// </summary>
    private void BuildDispatchWorkshop(float radius, float clearance, float processTop)
    {
        var rear = radius + clearance + 0.35f;
        var side = radius + clearance + 0.60f;
        var wallZ = rear + 1.70f;
        var height = MathF.Max(3.8f, processTop + 1.5f);
        var floorHalf = MathF.Max(side + 1.5f, 7.0f);
        var concrete = StudioSceneMaterials.Create("#AAA79F", "concrete");
        var plaster = StudioSceneMaterials.Create("#D0CDC3", "concrete");
        var brick = StudioSceneMaterials.Create("#967353", "concrete");
        var steel = StudioSceneMaterials.Create("#55534D", "metal");
        var galvanized = StudioSceneMaterials.Create("#A6AAA6", "metal");
        var olive = StudioSceneMaterials.Create("#69734B", "paint");
        var benchPaint = StudioSceneMaterials.Create("#985541", "paint");
        var timber = StudioSceneMaterials.Create("#BD925B", "wood");
        var kraft = StudioSceneMaterials.Create("#B98A50", "cardboard");
        var lightKraft = StudioSceneMaterials.Create("#D3AF76", "cardboard");
        var rubber = StudioSceneMaterials.Create("#282724", "rubber");
        var cream = StudioSceneMaterials.Create("#ECE5D3", "paint");
        var loadingDark = StudioSceneMaterials.Create("#535A4C", "paint");
        // Large backdrop faces avoid directional shadow sampling bands. Object
        // geometry retains shadows, making the separated work areas read in depth.
        plaster.DisableReceiveShadows = true;
        brick.DisableReceiveShadows = true;

        Box(this, "DispatchWarmConcreteFloor", new Vector3(floorHalf * 2, 0.12f, floorHalf * 2),
            new Vector3(0, -0.065f, 0), concrete);

        // Open cutaway warehouse: a rear delivery opening and shallow side walls
        // provide an actual room, while the camera-facing sides remain open.
        const float doorway = 3.0f;
        var wingWidth = side - doorway * 0.5f;
        foreach (var sign in new[] { -1, 1 })
        {
            var x = sign * (doorway * 0.5f + wingWidth * 0.5f);
            Box(this, "DispatchRearWall" + sign, new Vector3(wingWidth, height, 0.20f),
                new Vector3(x, height * 0.5f, wallZ), plaster);
            Box(this, "DispatchRearMasonry" + sign, new Vector3(wingWidth, 0.75f, 0.23f),
                new Vector3(x, 0.375f, wallZ - 0.025f), brick);
            Box(this, "DispatchSideReturn" + sign, new Vector3(0.20f, height, 4.6f),
                new Vector3(sign * side, height * 0.5f, wallZ - 2.10f), plaster);
            Box(this, "DispatchSideMasonryReturn" + sign, new Vector3(0.23f, 0.75f, 4.6f),
                new Vector3(sign * side, 0.375f, wallZ - 2.10f), brick);
            Box(this, "DispatchStructuralColumn" + sign, new Vector3(0.23f, height, 0.23f),
                new Vector3(sign * side, height * 0.5f, rear - 0.20f), steel);
            Box(this, "DispatchColumnFoot" + sign, new Vector3(0.48f, 0.15f, 0.48f),
                new Vector3(sign * side, 0.075f, rear - 0.20f), concrete);
        }
        Box(this, "DispatchDoorHeader", new Vector3(doorway, height - 3.2f, 0.20f),
            new Vector3(0, 3.2f + (height - 3.2f) * 0.5f, wallZ), plaster);
        // An open loading recess has depth and a warm sunlit yard beyond it. A
        // solid recessed face ends the set; no repeated shutter slats are used.
        Box(this, "LoadingRecessFloor", new Vector3(doorway, 0.08f, 2.0f),
            new Vector3(0, -0.01f, wallZ + 0.95f), concrete);
        Box(this, "LoadingYardBackdrop", new Vector3(doorway, 3.2f, 0.10f),
            new Vector3(0, 1.6f, wallZ + 1.95f), plaster);
        Box(this, "LoadingRearServiceDoor", new Vector3(1.20f, 2.65f, 0.04f),
            new Vector3(-0.58f, 1.325f, wallZ + 1.88f), loadingDark);
        // An offset interior return exposes a side face, providing a readable
        // second depth plane behind the outside portal instead of a black void.
        Box(this, "LoadingInteriorReturn", new Vector3(0.16f, 2.85f, 0.80f),
            new Vector3(1.0f, 1.425f, wallZ + 1.55f), brick);
        foreach (var sign in new[] { -1, 1 })
            Box(this, "LoadingDoorDeepReveal" + sign, new Vector3(0.16f, 3.2f, 2.0f),
                new Vector3(sign * doorway * 0.5f, 1.6f, wallZ + 0.95f), plaster);
        Box(this, "LoadingOpeningSoffit", new Vector3(doorway, 0.12f, 2.0f),
            new Vector3(0, 3.15f, wallZ + 0.95f), plaster);
        // Stock visible in the recessed loading area gives the opening a scale
        // cue and depth rather than another blank rectangular wall face.
        DispatchPallet("RecessedDeliveryPallet", new Vector3(0.36f, 0, wallZ + 1.23f), timber);
        DispatchCarton("RecessedDeliveryCrate", new Vector3(0.36f, 0.18f, wallZ + 1.23f),
            new Vector3(0.70f, 0.56f, 0.55f), kraft, cream);
        AddChild(new OmniLight3D
        {
            Name = "LoadingCorridorNeutralFill", Position = new Vector3(0.35f, 2.6f, wallZ + 0.90f),
            LightColor = new Color(1.0f, 0.98f, 0.94f), LightEnergy = 0.18f,
            OmniRange = 3.5f, ShadowEnabled = false,
        });
        foreach (var sign in new[] { -1, 1 })
            Box(this, "LoadingDoorJamb" + sign, new Vector3(0.10f, 3.25f, 0.35f),
                new Vector3(sign * (doorway * 0.5f + 0.02f), 1.625f, wallZ - 0.05f), galvanized);

        // Just two connected roof members convey building structure rather than
        // a repeating-line background or an opaque ceiling hiding the equipment.
        Box(this, "DispatchRearRoofGirder", new Vector3(side * 2 + 0.23f, 0.28f, 0.24f),
            new Vector3(0, height - 0.10f, rear - 0.20f), steel);
        foreach (var sign in new[] { -1, 1 })
            Box(this, "DispatchRoofReturn" + sign, new Vector3(0.23f, 0.23f, 4.6f),
                new Vector3(sign * side, height - 0.10f, wallZ - 2.10f), steel);
        // A narrow solid rear canopy shows the underside of the building. Its
        // front edge remains beyond the process circle and it does not roof over
        // the teaching machine or force an obstructed view of the carton.
        Box(this, "DispatchRearCeilingCanopy", new Vector3(side * 2, 0.12f, 1.15f),
            new Vector3(0, height - 0.05f, wallZ - 0.48f), plaster);

        // Shipping stock occupies a real timber pallet in the rear-left work area.
        var palletPosition = new Vector3(-side * 0.53f, 0, rear + 0.10f);
        DispatchPallet("ShippingStockPallet", palletPosition, timber);
        DispatchCarton("ShippingStockA", palletPosition + new Vector3(-0.32f, 0.18f, -0.12f),
            new Vector3(0.58f, 0.52f, 0.62f), kraft, cream);
        DispatchCarton("ShippingStockB", palletPosition + new Vector3(0.30f, 0.18f, -0.10f),
            new Vector3(0.55f, 0.42f, 0.64f), lightKraft, cream);
        DispatchCarton("ShippingStockC", palletPosition + new Vector3(-0.27f, 0.70f, -0.12f),
            new Vector3(0.48f, 0.35f, 0.52f), lightKraft, cream);

        // A packing bench has an unmistakable hardwood work surface, painted
        // frame, lower shelf and paper roll, but no pretend electrical controls.
        var bench = new Vector3(side * 0.55f, 0, rear + 0.10f);
        Box(this, "DispatchPackingBenchTop", new Vector3(2.5f, 0.09f, 0.76f),
            bench + new Vector3(0, 0.97f, 0), timber);
        Box(this, "DispatchPackingBenchShelf", new Vector3(2.30f, 0.05f, 0.65f),
            bench + new Vector3(0, 0.25f, 0), benchPaint);
        foreach (var x in new[] { -1.10f, 1.10f })
            foreach (var z in new[] { -0.28f, 0.28f })
                Box(this, "PackingBenchLeg", new Vector3(0.065f, 0.93f, 0.065f),
                    bench + new Vector3(x, 0.465f, z), benchPaint);
        DispatchCarton("PackingBenchCarton", bench + new Vector3(-0.47f, 1.015f, 0),
            new Vector3(0.46f, 0.32f, 0.42f), kraft, cream);
        Cylinder(this, "PackingPaperRoll", bench + new Vector3(0.47f, 1.15f, 0),
            0.13f, 0.38f, lightKraft, new Vector3(0, 0, 90));
        Box(this, "PackingBenchLowerTray", new Vector3(0.45f, 0.18f, 0.42f),
            bench + new Vector3(0.45f, 0.365f, 0), olive);
        // The packing station includes useful vertical storage and a shipping
        // noticeboard, so it reads as a work area rather than an isolated table.
        Box(this, "PackingStationNoticeboard", new Vector3(1.30f, 0.70f, 0.06f),
            bench + new Vector3(0.12f, 1.85f, 0.57f), timber);
        Box(this, "PackingStationNotice", new Vector3(0.38f, 0.48f, 0.012f),
            bench + new Vector3(-0.19f, 1.85f, 0.525f), cream);
        Box(this, "PackingStationDispatchFolder", new Vector3(0.26f, 0.34f, 0.015f),
            bench + new Vector3(0.37f, 1.87f, 0.515f), olive);
        Box(this, "PackingStationSupplyShelf", new Vector3(2.40f, 0.07f, 0.45f),
            bench + new Vector3(0, 2.50f, 0.55f), benchPaint);
        DispatchCarton("PackingStationShelfSupplies", bench + new Vector3(-0.66f, 2.535f, 0.53f),
            new Vector3(0.62f, 0.35f, 0.40f), lightKraft, cream);
        DispatchCarton("PackingStationShelfSuppliesSmall", bench + new Vector3(0.25f, 2.535f, 0.53f),
            new Vector3(0.45f, 0.28f, 0.37f), kraft, cream);

        // A dedicated parcel rack behind the stocked pallet occupies midground
        // height, with physically connected uprights and three useful shelves.
        var rack = new Vector3(-side * 0.53f, 0, rear + 0.93f);
        foreach (var x in new[] { -1.08f, 1.08f })
            foreach (var z in new[] { -0.30f, 0.30f })
                Box(this, "DispatchStockRackUpright", new Vector3(0.065f, 2.80f, 0.065f),
                    rack + new Vector3(x, 1.40f, z), benchPaint);
        foreach (var shelfY in new[] { 0.30f, 1.35f, 2.40f })
        {
            Box(this, "DispatchStockRackShelf", new Vector3(2.23f, 0.075f, 0.69f),
                rack + new Vector3(0, shelfY, 0), galvanized);
            DispatchCarton("RackShippingStock", rack + new Vector3(-0.60f, shelfY + 0.0375f, 0),
                new Vector3(0.60f, 0.43f, 0.52f), kraft, cream);
            DispatchCarton("RackShippingStockSmall", rack + new Vector3(0.22f, shelfY + 0.0375f, 0.01f),
                new Vector3(0.55f, 0.35f, 0.50f), lightKraft, cream);
        }

        // Low side staging provides midground/foreground depth. Its nearest
        // inside edge stays outside the process circle regardless of rotation.
        var sideStock = new Vector3(-radius - clearance - 0.80f, 0, -0.65f);
        DispatchPallet("EmptySidePallet", sideStock, timber);
        DispatchCarton("SideDispatchCarton", sideStock + new Vector3(0, 0.18f, 0),
            new Vector3(0.70f, 0.48f, 0.60f), kraft, cream);
        var trolley = new Vector3(radius + clearance + 0.78f, 0, 0.65f);
        Box(this, "DispatchUtilityTrolleyDeck", new Vector3(0.85f, 0.09f, 0.60f),
            trolley + new Vector3(0, 0.19f, 0), olive);
        foreach (var x in new[] { -0.31f, 0.31f })
            foreach (var z in new[] { -0.21f, 0.21f })
                Cylinder(this, "TrolleyRubberWheel", trolley + new Vector3(x, 0.10f, z),
                    0.10f, 0.065f, rubber, new Vector3(0, 0, 90));
        Box(this, "UtilityTrolleyHandleLeft", new Vector3(0.04f, 0.75f, 0.04f),
            trolley + new Vector3(-0.33f, 0.56f, 0.26f), galvanized);
        Box(this, "UtilityTrolleyHandleRight", new Vector3(0.04f, 0.75f, 0.04f),
            trolley + new Vector3(0.33f, 0.56f, 0.26f), galvanized);
        Box(this, "UtilityTrolleyGrip", new Vector3(0.70f, 0.04f, 0.04f),
            trolley + new Vector3(0, 0.92f, 0.26f), rubber);

        Cabinet(this, new Vector3(side - 0.48f, 0, rear + 1.15f), galvanized, olive);
        // Simple single pipe and air receiver make this a serviced industrial
        // workspace without exposing dangerous wiring or claiming safety hardware.
        Cylinder(this, "DispatchAirReceiver", new Vector3(-side + 0.43f, 0.48f, rear + 1.02f),
            0.21f, 0.88f, olive, Vector3.Zero);
        Box(this, "DispatchUtilityPipe", new Vector3(0.045f, 2.45f, 0.045f),
            new Vector3(side - 0.30f, 1.225f, wallZ - 0.16f), galvanized);

        var fixture = Surface(new Color(0.94f, 0.93f, 0.88f), 0, 0.6f);
        fixture.EmissionEnabled = true;
        fixture.Emission = new Color(0.74f, 0.73f, 0.68f);
        foreach (var sign in new[] { -1, 1 })
        {
            var at = new Vector3(sign * side * 0.47f, height - 0.30f, rear - 0.15f);
            Box(this, "DispatchWhiteFixtureHousing" + sign, new Vector3(0.85f, 0.12f, 0.36f), at, galvanized);
            Box(this, "DispatchWhiteFixtureFace" + sign, new Vector3(0.72f, 0.02f, 0.27f),
                at + new Vector3(0, -0.07f, 0), fixture);
            AddChild(new OmniLight3D
            {
                Name = "DispatchNeutralHighBayFill" + sign, Position = at + new Vector3(0, -0.22f, 0),
                LightColor = new Color(1.0f, 0.97f, 0.90f), LightEnergy = 0.08f,
                OmniRange = MathF.Max(7.0f, radius * 2.0f), ShadowEnabled = false,
            });
        }
        AddVentilation(new Vector3(-side * 0.63f, 2.65f, wallZ - 0.12f), galvanized, cream, rubber);
        Box(this, "DispatchBayIdentificationPlate", new Vector3(1.45f, 0.32f, 0.035f),
            new Vector3(0, 3.55f, wallZ - 0.13f), olive);
        AddChild(new Label3D
        {
            Name = "DispatchWorkshopIdentity", Text = "DISPATCH / 03",
            Position = new Vector3(0, 3.55f, wallZ - 0.155f), RotationDegrees = new Vector3(0, 180, 0),
            FontSize = 40, PixelSize = 0.003f, OutlineSize = 0, Modulate = new Color(0.96f, 0.94f, 0.86f),
        });
    }

    private void DispatchPallet(string name, Vector3 position, Material timber)
    {
        var pallet = new Node3D { Name = name, Position = position };
        AddChild(pallet);
        foreach (var x in new[] { -0.42f, 0, 0.42f })
            Box(pallet, "TimberBearer", new Vector3(0.12f, 0.13f, 0.78f), new Vector3(x, 0.065f, 0), timber);
        // A few physical pallet planks are object geometry, not background stripes.
        foreach (var z in new[] { -0.28f, 0, 0.28f })
            Box(pallet, "TimberDeckBoard", new Vector3(1.15f, 0.05f, 0.23f), new Vector3(0, 0.155f, z), timber);
    }

    private void DispatchCarton(string name, Vector3 bottom, Vector3 size, Material cardboard, Material label)
    {
        Box(this, name, size, bottom + new Vector3(0, size.Y * 0.5f, 0), cardboard);
        Box(this, name + "ShippingLabel", new Vector3(size.X * 0.32f, size.Y * 0.20f, 0.004f),
            bottom + new Vector3(0.05f, size.Y * 0.57f, -size.Z * 0.5f - 0.003f), label);
    }

    private static void Cylinder(Node3D parent, string name, Vector3 position, float radius,
        float height, Material material, Vector3 rotationDegrees)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Position = position, RotationDegrees = rotationDegrees, MaterialOverride = material,
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 16 },
        });
    }
}
