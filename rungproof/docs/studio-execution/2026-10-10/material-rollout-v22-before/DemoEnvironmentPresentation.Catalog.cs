using System;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    /// <summary>
    /// Builds presentation-only context in a world-axis-aligned frame. Root supplies
    /// the complete conservative process envelope, including declared motion. Rear
    /// signs select two building sides to close; they do not rotate the building.
    /// No process node, transform, material, controller or pick target is modified.
    /// </summary>
    public void BuildCatalogEnvironment(SceneEnvironmentProfile profile, Aabb protectedBoundsLocal,
        int rearXSign, int rearZSign, SceneEnvironmentArtDirection? artDirection = null)
    {
        var bounds = protectedBoundsLocal;
        var sx = rearXSign < 0 ? -1 : 1;
        var sz = rearZSign < 0 ? -1 : 1;
        var cx = bounds.GetCenter().X;
        var cz = bounds.GetCenter().Z;
        var width = MathF.Max(profile.Family == SceneEnvironmentFamily.DispatchWarehouse ? 8.0f : 4.8f, bounds.Size.X + 3.10f);
        var depth = MathF.Max(4.8f, bounds.Size.Z + 3.10f);
        if (artDirection is not null)
        {
            width *= MathF.Max(1, artDirection.WidthScale);
            depth *= MathF.Max(1, artDirection.DepthScale);
        }
        // Wall coordinates use final room dimensions, including minimum widths;
        // otherwise compact lessons put the side wall through their furniture.
        var backX = cx + sx * width * 0.5f;
        var backZ = cz + sz * depth * 0.5f;
        var accent = StudioSceneMaterials.Create(artDirection?.AccentHex ?? profile.AccentHex, "paint");
        var steel = StudioSceneMaterials.Create("#666A68", "metal");
        var metal = StudioSceneMaterials.Create("#B7BAB8", "metal");
        var timber = StudioSceneMaterials.Create("#B79562", "wood");
        var kraft = StudioSceneMaterials.Create("#B68B58", "cardboard");
        var rubber = StudioSceneMaterials.Create("#302F2B", "rubber");
        var concrete = StudioSceneMaterials.Create(artDirection?.FloorHex ?? "#AAA79F", artDirection?.FloorStyle ?? "concrete");
        if (artDirection?.LayoutVariant == "call-open-front-alcove")
            concrete = ReferenceRoleSurface("#747F7C", "resin");
        if (artDirection?.LayoutVariant == "grouping-clear-span-loading")
            concrete = ReferenceRoleSurface("#8D9692", "concrete");        if (artDirection?.LayoutVariant == "inspection-paired-recesses")
            concrete = StudioSceneMaterials.Create("#85999D", "concrete");        if (artDirection?.LayoutVariant == "dryer-washroom-side-return")
            concrete = StudioSceneMaterials.Create("#AAA99B", "concrete");        var white = StudioSceneMaterials.Create("#D9D8CF", "paint");
        var interior = profile.Family is not (SceneEnvironmentFamily.OutdoorTraffic or SceneEnvironmentFamily.OutdoorUtility);

        if (profile.ReplaceFloor)
        {
            var ground = artDirection is not null || interior ? concrete : StudioSceneMaterials.Create("#99978C", "concrete");
            // A generous continuous slab reaches beyond context, never a tiny
            // pedestal. Its top is below the authored zero floor datum.
            Box(this, "CatalogContinuousGround", new Vector3(width + 4, 0.12f, depth + 4),
                new Vector3(cx, -0.065f, cz), ground);
        }
        else
        {
            // Preserve authored roads, channels and pits. Peripheral aprons never
            // cover the protected rectangle, including its below-ground contents.
            foreach (var sign in new[] { -1, 1 })
            {
                var x = (sign > 0 ? bounds.End.X : bounds.Position.X) + sign * 1.8f;
                Box(this, "PeripheralGroundX" + sign, new Vector3(2.5f, 0.10f, depth + 4),
                    new Vector3(x, -0.06f, cz), concrete);
                var z = (sign > 0 ? bounds.End.Z : bounds.Position.Z) + sign * 1.8f;
                Box(this, "PeripheralGroundZ" + sign, new Vector3(bounds.Size.X + 1.1f, 0.10f, 2.5f),
                    new Vector3(cx, -0.06f, z), concrete);
            }
        }

        if (artDirection is not null)
        {
            // Exact layout composers own their passive context. Old generic
            // furniture is not automatically a sensible access destination.
            BuildArtDirectionArchitecture(artDirection, bounds, sx, sz, backX, backZ, width, depth);
            SetAmbientMotionEnabled(false);
            return;
        }
        if (interior)
        {
            if (artDirection is not null)
                BuildArtDirectionArchitecture(artDirection, bounds, sx, sz, backX, backZ, width, depth);
            else
                BuildCatalogRoom(profile, bounds, sx, sz, backX, backZ, width, depth, white, steel, accent);
            // Identity selects this exact ledger entry: other packaging scenes
            // share the variant, so variant matching would broaden the pilot.
            var groupingRepresentative = ReferenceEquals(profile,
                SceneEnvironmentProfiles.Profiles["lab-4-08-package-grouping"]);
            if (profile.Family == SceneEnvironmentFamily.TrainingWorkshop || groupingRepresentative)
            {
                // The functional student station is composed separately. Its
                // work and standing/reach area must not become an aisle to
                // decorative stock. Retain the open-front classroom shell,
                // with no second bench, rear table, rack or stocked shelving.
                // Grouping's representative also omits rear packing/storage:
                // do not create an undefined access destination behind the
                // machinery. Its existing loading architecture remains intact.
                // No storage-access claim is inferred from process clearance.
                SetAmbientMotionEnabled(false);
                return;
            }
            var work = CatalogRearFixture("ContextWorkstation", bounds, sz,
                cx - (profile.Family == SceneEnvironmentFamily.DispatchWarehouse ? 2.55f : width * 0.20f));
            var stock = CatalogRearFixture("ContextStorage", bounds, sz,
                cx + (profile.Family == SceneEnvironmentFamily.DispatchWarehouse ? 2.45f : width * 0.20f));
            switch (profile.Family)
            {
                case SceneEnvironmentFamily.DispatchWarehouse:
                    if (profile.Variant == "food")
                    {
                        CatalogBench(work, 2.4f, metal, white, metal);
                        CatalogShelf(work, 2.4f, 2.20f, metal, white);
                        CatalogInstrumentCase(work, new Vector3(-0.42f, 1.02f, 0), metal, rubber);
                        CatalogClosedTote(work, new Vector3(0.53f, 1.02f, 0), white, metal);
                        CatalogServiceCabinet(stock, white, metal);
                        CatalogClosedTote(stock, new Vector3(0.85f, 0.05f, 0), white, metal);
                    }
                    else if (profile.Variant == "chemical")
                    {
                        CatalogUtilitySkid(work, accent, metal, rubber);
                        CatalogServiceCabinet(stock, accent, metal);
                        CatalogCylinder(stock, "ClosedProcessStorageDrum", new Vector3(0.82f, 0.5f, 0), 0.25f, 1.0f, metal, Vector3.Zero);
                    }
                    else if (profile.Variant == "palletizing")
                    {
                        CatalogRollStand(work, accent, metal, white);
                        CatalogPallet(stock, timber);
                        CatalogCarton(stock, "PalletStockLower", new Vector3(-0.25f, 0.18f, 0), new Vector3(0.52f, 0.45f, 0.58f), kraft, white);
                        CatalogCarton(stock, "PalletStockUpper", new Vector3(-0.25f, 0.63f, 0), new Vector3(0.44f, 0.34f, 0.50f), kraft, white);
                        CatalogCarton(stock, "PalletStockSide", new Vector3(0.32f, 0.18f, 0), new Vector3(0.50f, 0.36f, 0.57f), kraft, white);
                    }
                    else
                    {
                        CatalogBench(work, 2.4f, timber, accent, steel);
                        CatalogShelf(work, 2.4f, 2.20f, metal, accent);
                        CatalogCarton(work, "PackingParcel", new Vector3(-0.45f, 1.02f, 0), new Vector3(0.52f, 0.40f, 0.46f), kraft, white);
                        CatalogCylinder(work, "PackingPaperRoll", new Vector3(0.55f, 1.16f, 0), 0.14f, 0.38f, timber, new Vector3(0, 0, 90));
                        CatalogRack(stock, 2.2f, accent, metal, kraft, white);
                    }
                    break;
                case SceneEnvironmentFamily.MachineShop:
                    if (profile.Variant == "drives")
                    {
                        CatalogServiceCabinet(work, accent, metal);
                        CatalogServiceCabinet(stock, metal, accent);
                    }
                    else if (profile.Variant == "extraction")
                    {
                        CatalogUtilitySkid(work, accent, metal, rubber);
                        CatalogCylinder(stock, "ClosedFilterStorage", new Vector3(0, 1.1f, 0), 0.30f, 2.1f, metal, Vector3.Zero);
                        Box(stock, "FilterReceiverFoot", new Vector3(0.72f, 0.10f, 0.66f), new Vector3(0, 0.05f, 0), accent);
                    }
                    else
                    {
                        CatalogBench(work, 2.3f, profile.Variant == "assembly" ? timber : metal, accent, steel);
                        if (profile.Variant == "machining")
                        {
                            CatalogToolChest(work, accent, metal, rubber);
                            CatalogStockStand(stock, metal, timber, accent);
                        }
                        else
                        {
                            CatalogPartsBin(work, new Vector3(-0.45f, 1.02f, 0), accent);
                            CatalogServiceCabinet(stock, profile.Variant == "coating" ? white : accent, metal);
                        }
                        CatalogInstrumentCase(work, new Vector3(0.55f, 1.04f, 0), metal, rubber);
                    }
                    break;
                case SceneEnvironmentFamily.ProcessUtility:
                    CatalogUtilitySkid(work, accent, metal, rubber);
                    CatalogServiceCabinet(stock, accent, metal);
                    if (profile.Variant == "powder")
                        CatalogCylinder(stock, "ClosedPowderStorageDrum", new Vector3(0.80f, 0.50f, 0), 0.25f, 1.0f, white, Vector3.Zero);
                    break;
                case SceneEnvironmentFamily.PublicInterior:
                    CatalogPublicContext(work, stock, profile.Variant, timber, accent, metal, rubber, white);
                    break;
                case SceneEnvironmentFamily.EquipmentGallery:
                    CatalogBench(work, 2.2f, white, accent, metal);
                    CatalogInstrumentCase(work, new Vector3(0, 1.03f, 0), metal, rubber);
                    CatalogRack(stock, 1.6f, accent, metal, kraft, white);
                    break;
            }
            if (profile.Family is not SceneEnvironmentFamily.PublicInterior)
            {
                // The existing slow fan retains owner's Stop/reduced-motion policy.
                var fanHeight = MathF.Max(2.7f, bounds.End.Y + 0.7f);
                AddVentilation(new Vector3(cx + width * 0.30f, fanHeight, backZ - sz * 0.16f), metal, white, rubber);
                if (sz < 0) GetNode<Node3D>("DecorativeBackgroundVentilation").RotationDegrees = new Vector3(0, 180, 0);
            }
        }
        else
        {
            if (artDirection is not null)
                BuildArtDirectionArchitecture(artDirection, bounds, sx, sz, backX, backZ, width, depth);
            BuildCatalogOutdoor(profile, bounds, sx, sz, backX, backZ, accent, metal, timber, rubber, concrete);
        }
        SetAmbientMotionEnabled(false);
    }

    private void BuildCatalogRoom(SceneEnvironmentProfile profile, Aabb bounds, int sx, int sz,
        float backX, float backZ, float width, float depth, Material wall, Material steel, Material accent)
    {
        // An elevator's conservative travel box is not a building-height demand.
        // All these architectural faces already lie outside its protected XZ box.
        var height = profile.Variant == "elevator" ? 5.0f : MathF.Max(3.6f, bounds.End.Y + 1.25f);
        // Room surfaces share texture resources but use their own shadow setting,
        // avoiding accidental mutation of cached materials used by other objects.
        var plaster = (StandardMaterial3D)StudioSceneMaterials.Create("#D0CDC3", "concrete").Duplicate();
        plaster.DisableReceiveShadows = true;
        var lowerWall = StudioSceneMaterials.Create(profile.Family == SceneEnvironmentFamily.MachineShop ? "#85847A" : "#A49A83", "paint");
        var cx = bounds.GetCenter().X;
        var cz = bounds.GetCenter().Z;
        var loading = profile.Family == SceneEnvironmentFamily.DispatchWarehouse;
        if (loading)
        {
            const float opening = 2.2f;
            var wing = (width - opening) * 0.5f;
            foreach (var sign in new[] { -1, 1 })
                Box(this, "WarehouseRearWing" + sign, new Vector3(wing, height, 0.18f),
                    new Vector3(cx + sign * (opening * 0.5f + wing * 0.5f), height * 0.5f, backZ), plaster);
            Box(this, "WarehouseDoorHeader", new Vector3(opening, MathF.Max(0.2f, height - 2.8f), 0.18f),
                new Vector3(cx, 2.8f + MathF.Max(0.2f, height - 2.8f) * 0.5f, backZ), plaster);
            Box(this, "WarehouseRecessFloor", new Vector3(opening, 0.10f, 1.25f),
                new Vector3(cx, -0.055f, backZ + sz * 0.60f), StudioSceneMaterials.Create("#AAA79F", "concrete"));
            Box(this, "WarehouseRecessBack", new Vector3(opening, 2.8f, 0.12f),
                new Vector3(cx, 1.4f, backZ + sz * 1.25f), plaster);
            foreach (var sign in new[] { -1, 1 })
                Box(this, "WarehousePortalReveal" + sign, new Vector3(0.14f, 2.8f, 1.25f),
                    new Vector3(cx + sign * opening * 0.5f, 1.4f, backZ + sz * 0.60f), lowerWall);
            Box(this, "WarehousePortalSoffit", new Vector3(opening, 0.12f, 1.25f),
                new Vector3(cx, 2.74f, backZ + sz * 0.60f), plaster);
            Box(this, "WarehouseInsetDoor", new Vector3(0.85f, 2.25f, 0.04f),
                new Vector3(cx - 0.45f, 1.125f, backZ + sz * 1.17f), accent);
            foreach (var sign in new[] { -1, 1 })
                Box(this, "WarehouseRearWallBase" + sign, new Vector3(wing, 0.60f, 0.20f),
                    new Vector3(cx + sign * (opening * 0.5f + wing * 0.5f), 0.30f, backZ - sz * 0.025f), lowerWall);
        }
        else
        {
            Box(this, "ContextRearWall", new Vector3(width, height, 0.18f), new Vector3(cx, height * 0.5f, backZ), plaster);
            Box(this, "ContextRearWallBase", new Vector3(width, 0.60f, 0.20f), new Vector3(cx, 0.30f, backZ - sz * 0.025f), lowerWall);
        }
        // The second wall establishes an asymmetric world-aligned room corner;
        // its nearer end stops short of the camera-facing process boundary.
        Box(this, "ContextSideWall", new Vector3(0.18f, height, depth), new Vector3(backX, height * 0.5f, cz), plaster);
        Box(this, "ContextSideWallBase", new Vector3(0.20f, 0.60f, depth), new Vector3(backX - sx * 0.025f, 0.30f, cz), lowerWall);
        Box(this, "ContextRearStructuralBeam", new Vector3(width, 0.18f, 0.22f), new Vector3(cx, height - 0.10f, backZ), steel);
        Box(this, "ContextSideStructuralBeam", new Vector3(0.22f, 0.18f, depth), new Vector3(backX, height - 0.10f, cz), steel);
        Box(this, "ContextRearCeilingUndersurface", new Vector3(width, 0.10f, 0.75f),
            new Vector3(cx, height - 0.05f, backZ - sz * 0.25f), plaster);
        if (profile.Variant == "elevator")
        {
            // Closed service-building facade at a human scale; this is neither
            // another active lift nor a fabricated accessible/safe landing.
            var facade = new Node3D
            {
                Name = "ElevatorServiceBuildingFacade",
                Position = new Vector3(cx, 0, backZ - sz * 0.16f),
                RotationDegrees = sz < 0 ? new Vector3(0, 180, 0) : Vector3.Zero,
            };
            AddChild(facade);
            Box(facade, "ClosedFacilitiesDoor", new Vector3(1.15f, 2.45f, 0.065f), new Vector3(-1.13f, 1.225f, -0.015f), accent);
            Box(facade, "ClosedDoorFrameLeft", new Vector3(0.075f, 2.53f, 0.09f), new Vector3(-1.74f, 1.265f, -0.05f), steel);
            Box(facade, "ClosedDoorFrameRight", new Vector3(0.075f, 2.53f, 0.09f), new Vector3(-0.52f, 1.265f, -0.05f), steel);
            Box(facade, "ClosedDoorFrameHeader", new Vector3(1.30f, 0.08f, 0.09f), new Vector3(-1.13f, 2.49f, -0.05f), steel);
            Box(facade, "FacilitiesWindowFrame", new Vector3(2.05f, 1.15f, 0.07f), new Vector3(0.80f, 2.05f, -0.025f), accent);
            Box(facade, "FacilitiesClosedWindow", new Vector3(1.90f, 1.00f, 0.025f), new Vector3(0.80f, 2.05f, -0.073f), StudioSceneMaterials.Create("#89958C", "metal"));
            Box(facade, "ServiceMezzanineFacadeBeam", new Vector3(4.30f, 0.20f, 0.42f), new Vector3(0, 3.0f, -0.20f), steel);
            foreach (var x in new[] { -2.05f, 2.05f })
                Box(facade, "ServiceFacadeSupport", new Vector3(0.12f, 3.0f, 0.18f), new Vector3(x, 1.50f, -0.22f), steel);
        }
        foreach (var fraction in new[] { -0.23f, 0.23f })
        {
            var p = new Vector3(cx + width * fraction, height - 0.25f, backZ - sz * 0.30f);
            Box(this, "ContextHighBayFixture", new Vector3(0.80f, 0.12f, 0.30f), p, steel);
            Box(this, "ContextHighBayWhiteFace", new Vector3(0.66f, 0.018f, 0.24f), p + new Vector3(0, -0.07f, 0), wall);
        }
    }

    private Node3D CatalogRearFixture(string name, Aabb bounds, int sz, float x)
    {
        // Furniture's maximum depth is 0.8m; 1.0m center offset leaves a minimum
        // 0.60m clearance from the protected process rectangle.
        var z = (sz > 0 ? bounds.End.Z : bounds.Position.Z) + sz * 1.0f;
        var node = new Node3D { Name = name, Position = new Vector3(x, 0, z), RotationDegrees = sz < 0 ? new Vector3(0, 180, 0) : Vector3.Zero };
        AddChild(node);
        return node;
    }

    private static void CatalogBench(Node3D parent, float width, Material top, Material paint, Material steel)
    {
        Box(parent, "BenchWorktop", new Vector3(width, 0.08f, 0.72f), new Vector3(0, 0.98f, 0), top);
        Box(parent, "BenchLowerShelf", new Vector3(width - 0.12f, 0.05f, 0.62f), new Vector3(0, 0.28f, 0), steel);
        foreach (var x in new[] { -width * 0.43f, width * 0.43f })
        foreach (var z in new[] { -0.28f, 0.28f })
            Box(parent, "BenchConnectedLeg", new Vector3(0.06f, 0.94f, 0.06f), new Vector3(x, 0.47f, z), paint);
    }

    private static void CatalogShelf(Node3D parent, float width, float y, Material metal, Material paint)
    {
        Box(parent, "SupplyShelf", new Vector3(width, 0.06f, 0.30f), new Vector3(0, y, 0.21f), metal);
        foreach (var x in new[] { -width * 0.40f, width * 0.40f })
            Box(parent, "SupplyShelfSupport", new Vector3(0.055f, 0.65f, 0.055f), new Vector3(x, y - 0.30f, 0.30f), paint);
    }

    private static void CatalogRack(Node3D parent, float width, Material frame, Material metal, Material kraft, Material label)
    {
        foreach (var x in new[] { -width * 0.47f, width * 0.47f })
        foreach (var z in new[] { -0.28f, 0.28f })
            Box(parent, "RackUpright", new Vector3(0.06f, 2.7f, 0.06f), new Vector3(x, 1.35f, z), frame);
        foreach (var y in new[] { 0.25f, 1.25f, 2.25f })
        {
            Box(parent, "RackShelf", new Vector3(width, 0.07f, 0.65f), new Vector3(0, y, 0), metal);
            CatalogCarton(parent, "StoredParcel", new Vector3(-width * 0.22f, y + 0.035f, 0), new Vector3(0.50f, 0.34f, 0.46f), kraft, label);
            CatalogCarton(parent, "StoredSmallParcel", new Vector3(width * 0.22f, y + 0.035f, 0), new Vector3(0.42f, 0.28f, 0.40f), kraft, label);
        }
    }

    private static void CatalogCarton(Node3D parent, string name, Vector3 bottom, Vector3 size, Material kraft, Material label)
    {
        Box(parent, name, size, bottom + new Vector3(0, size.Y * 0.5f, 0), kraft);
        Box(parent, name + "ShippingLabel", new Vector3(size.X * 0.30f, size.Y * 0.22f, 0.004f),
            bottom + new Vector3(0.02f, size.Y * 0.6f, -size.Z * 0.5f - 0.003f), label);
    }

    private static void CatalogPartsBin(Node3D parent, Vector3 p, Material paint)
    {
        Box(parent, "PartsBinBase", new Vector3(0.42f, 0.04f, 0.30f), p, paint);
        foreach (var x in new[] { -0.19f, 0.19f }) Box(parent, "PartsBinSide", new Vector3(0.035f, 0.15f, 0.30f), p + new Vector3(x, 0.075f, 0), paint);
        Box(parent, "PartsBinBack", new Vector3(0.42f, 0.15f, 0.035f), p + new Vector3(0, 0.075f, 0.13f), paint);
    }

    private static void CatalogInstrumentCase(Node3D parent, Vector3 p, Material body, Material rubber)
    {
        Box(parent, "ClosedInstrumentCase", new Vector3(0.48f, 0.20f, 0.34f), p + new Vector3(0, 0.10f, 0), body);
        Box(parent, "InstrumentCaseHandle", new Vector3(0.16f, 0.045f, 0.035f), p + new Vector3(0, 0.11f, -0.19f), rubber);
    }

    private static void CatalogToolChest(Node3D parent, Material paint, Material metal, Material rubber)
    {
        Box(parent, "ToolChestBody", new Vector3(0.85f, 0.72f, 0.54f), new Vector3(-0.52f, 0.62f, 0), paint);
        foreach (var y in new[] { 0.44f, 0.64f, 0.84f })
            Box(parent, "ToolDrawerHandle", new Vector3(0.58f, 0.035f, 0.04f), new Vector3(-0.52f, y, -0.29f), metal);
        Box(parent, "ToolChestRubberTop", new Vector3(0.86f, 0.045f, 0.55f), new Vector3(-0.52f, 1.005f, 0), rubber);
    }

    private static void CatalogStockStand(Node3D parent, Material metal, Material timber, Material paint)
    {
        Box(parent, "StockStandBase", new Vector3(1.60f, 0.08f, 0.70f), new Vector3(0, 0.10f, 0), paint);
        foreach (var x in new[] { -0.60f, 0.60f }) Box(parent, "StockStandUpright", new Vector3(0.09f, 1.40f, 0.09f), new Vector3(x, 0.80f, 0.22f), paint);
        Box(parent, "StockStandCrossMember", new Vector3(1.4f, 0.08f, 0.48f), new Vector3(0, 1.28f, 0), metal);
        Box(parent, "StoredTimberBlank", new Vector3(1.45f, 0.12f, 0.20f), new Vector3(0, 1.38f, 0), timber);
        Box(parent, "StoredMetalBlank", new Vector3(1.20f, 0.08f, 0.18f), new Vector3(0, 0.30f, -0.13f), metal);
    }

    private static void CatalogServiceCabinet(Node3D parent, Material paint, Material metal)
    {
        Box(parent, "ServiceCupboard", new Vector3(0.95f, 1.90f, 0.60f), new Vector3(0, 0.95f, 0), paint);
        Box(parent, "ServiceCupboardDoor", new Vector3(0.83f, 1.73f, 0.035f), new Vector3(0, 0.95f, -0.32f), metal);
        Box(parent, "ServiceCupboardHandle", new Vector3(0.035f, 0.19f, 0.045f), new Vector3(0.29f, 0.99f, -0.35f), paint);
    }

    private static void CatalogUtilitySkid(Node3D parent, Material paint, Material metal, Material rubber)
    {
        Box(parent, "UtilityReceiverSkid", new Vector3(1.65f, 0.10f, 0.70f), new Vector3(0, 0.05f, 0), metal);
        CatalogCylinder(parent, "UtilityVerticalReceiver", new Vector3(-0.42f, 0.88f, 0), 0.25f, 1.5f, paint, Vector3.Zero);
        Box(parent, "UtilityClosedFilterHousing", new Vector3(0.44f, 0.62f, 0.42f), new Vector3(0.47f, 0.43f, 0), paint);
        CatalogCylinder(parent, "UtilityConnectedPipe", new Vector3(0.10f, 0.75f, 0), 0.04f, 0.65f, metal, new Vector3(0, 0, 90));
        Box(parent, "UtilityRubberIsolationPad", new Vector3(0.55f, 0.055f, 0.52f), new Vector3(0.47f, 0.12f, 0), rubber);
    }

    private static void CatalogClosedTote(Node3D parent, Vector3 bottom, Material body, Material lid)
    {
        Box(parent, "ClosedSupplyTote", new Vector3(0.58f, 0.35f, 0.45f), bottom + new Vector3(0, 0.175f, 0), body);
        Box(parent, "ClosedSupplyToteLid", new Vector3(0.60f, 0.035f, 0.47f), bottom + new Vector3(0, 0.3675f, 0), lid);
    }

    private static void CatalogPallet(Node3D parent, Material wood)
    {
        foreach (var x in new[] { -0.42f, 0, 0.42f })
            Box(parent, "PalletTimberBearer", new Vector3(0.12f, 0.13f, 0.74f), new Vector3(x, 0.065f, 0), wood);
        foreach (var z in new[] { -0.26f, 0, 0.26f })
            Box(parent, "PalletTimberDeck", new Vector3(1.15f, 0.05f, 0.21f), new Vector3(0, 0.155f, z), wood);
    }

    private static void CatalogRollStand(Node3D parent, Material paint, Material metal, Material roll)
    {
        Box(parent, "PackagingRollStandBase", new Vector3(1.2f, 0.08f, 0.60f), new Vector3(0, 0.04f, 0), paint);
        foreach (var x in new[] { -0.48f, 0.48f })
            Box(parent, "PackagingRollStandUpright", new Vector3(0.07f, 1.25f, 0.07f), new Vector3(x, 0.625f, 0), paint);
        CatalogCylinder(parent, "PackagingRollStandSpindle", new Vector3(0, 1.05f, 0), 0.045f, 1.1f, metal, new Vector3(0, 0, 90));
        CatalogCylinder(parent, "PackagingFilmStock", new Vector3(0, 1.05f, 0), 0.25f, 0.80f, roll, new Vector3(0, 0, 90));
    }

    private static void CatalogPublicContext(Node3D work, Node3D stock, string variant, Material timber,
        Material paint, Material metal, Material rubber, Material white)
    {
        if (variant == "washroom")
        {
            Box(work, "WashroomVanityCabinet", new Vector3(1.55f, 0.90f, 0.62f), new Vector3(0, 0.45f, 0), paint);
            Box(work, "WashroomCounter", new Vector3(1.62f, 0.08f, 0.67f), new Vector3(0, 0.94f, 0), white);
            Box(work, "WashroomBasinRecess", new Vector3(0.55f, 0.015f, 0.37f), new Vector3(0, 0.99f, 0), metal);
            Box(work, "WashroomMirrorFrame", new Vector3(1.05f, 0.83f, 0.05f), new Vector3(0, 1.64f, 0.28f), paint);
            Box(work, "WashroomMirrorFace", new Vector3(0.96f, 0.74f, 0.015f), new Vector3(0, 1.64f, 0.245f), metal);
            CatalogServiceCabinet(stock, white, paint);
            return;
        }
        if (variant is "elevator" or "access")
        {
            CatalogServiceCabinet(work, paint, metal);
            CatalogBench(stock, 1.6f, metal, paint, metal);
            CatalogInstrumentCase(stock, new Vector3(0, 1.02f, 0), paint, rubber);
            return;
        }
        // A waiting bench and closed storage provide architectural context without
        // introducing pretend public controls or additional lesson status signals.
        Box(work, "PublicBenchSeat", new Vector3(1.80f, 0.10f, 0.52f), new Vector3(0, 0.49f, 0), timber);
        Box(work, "PublicBenchBack", new Vector3(1.80f, 0.48f, 0.075f), new Vector3(0, 0.82f, 0.23f), timber);
        foreach (var x in new[] { -0.70f, 0.70f }) Box(work, "PublicBenchLeg", new Vector3(0.075f, 0.46f, 0.44f), new Vector3(x, 0.23f, 0), metal);
        CatalogServiceCabinet(stock, paint, white);
        if (variant.Contains("baggage", StringComparison.Ordinal)) CatalogInstrumentCase(work, new Vector3(0.55f, 0.55f, 0), paint, rubber);
    }

    private void BuildCatalogOutdoor(SceneEnvironmentProfile profile, Aabb bounds, int sx, int sz,
        float backX, float backZ, Material accent, Material metal, Material timber, Material rubber, Material concrete)
    {
        var center = bounds.GetCenter();
        var fixture = CatalogRearFixture("OutdoorServiceContext", bounds, sz, center.X - MathF.Min(3.0f, bounds.Size.X * 0.25f));
        if (profile.Variant == "parking")
        {
            Box(fixture, "ParkingServiceKioskBody", new Vector3(1.40f, 2.45f, 0.70f), new Vector3(0, 1.225f, 0), accent);
            Box(fixture, "ParkingServiceKioskWindow", new Vector3(1.12f, 0.73f, 0.03f), new Vector3(0, 1.65f, -0.365f), metal);
            Box(fixture, "ParkingServiceKioskRoof", new Vector3(1.55f, 0.10f, 0.78f), new Vector3(0, 2.50f, 0), metal);
        }
        else
            CatalogServiceCabinet(fixture, accent, metal);
        if (profile.Variant == "charging")
        {
            // A service canopy is context outside the lesson's vehicle paths,
            // not an invented charger or electrically functional cabinet.
            foreach (var x in new[] { -1.20f, 1.20f })
            foreach (var z in new[] { -0.28f, 0.28f })
                Box(fixture, "ChargingSiteCanopyPost", new Vector3(0.075f, 3.1f, 0.075f), new Vector3(x, 1.55f, z), metal);
            Box(fixture, "ChargingSiteServiceCanopy", new Vector3(2.60f, 0.14f, 0.78f), new Vector3(0, 3.12f, 0), accent);
        }
        var second = CatalogRearFixture("OutdoorSiteContext", bounds, sz, center.X + MathF.Min(3.0f, bounds.Size.X * 0.25f));
        if (profile.Family == SceneEnvironmentFamily.OutdoorUtility || profile.Variant == "waterfront")
            CatalogUtilitySkid(second, accent, metal, rubber);
        else
        {
            Box(second, "RoadsideMaintenanceBench", new Vector3(1.65f, 0.09f, 0.55f), new Vector3(0, 0.48f, 0), timber);
            foreach (var x in new[] { -0.63f, 0.63f }) Box(second, "RoadsideBenchFoot", new Vector3(0.16f, 0.45f, 0.48f), new Vector3(x, 0.225f, 0), concrete);
        }
        // Low site architecture follows world axes outside process travel. A
        // single retaining wall avoids a generic factory room for outdoor lessons.
        Box(this, "SiteRearRetainingWall", new Vector3(MathF.Max(5.0f, bounds.Size.X + 3.1f), 0.72f, 0.25f),
            new Vector3(center.X, 0.36f, backZ + sz * (profile.Family == SceneEnvironmentFamily.OutdoorUtility || profile.Variant == "waterfront" ? 1.50f : 0.45f)), concrete);
        // Treatment-plant shelter occupies this side boundary; keep its frontage
        // clear rather than planting a decorative object through the building.
        if (profile.Family != SceneEnvironmentFamily.OutdoorUtility)
        {
        var planter = new Vector3(backX, 0, center.Z);
        Box(this, "SiteLandscapePlanter", new Vector3(0.80f, 0.45f, 2.20f), planter + new Vector3(0, 0.225f, 0), concrete);
        Box(this, "SiteLandscapeSoil", new Vector3(0.67f, 0.035f, 2.06f), planter + new Vector3(0, 0.46f, 0), StudioSceneMaterials.Create("#574B36", "concrete"));
        var foliage = StudioSceneMaterials.Create("#66784B", "paint");
        foreach (var z in new[] { -0.60f, 0.10f, 0.67f })
        {
            CatalogCylinder(this, "LandscapeShrubStem", planter + new Vector3(0, 0.73f, z), 0.035f, 0.50f, timber, Vector3.Zero);
            AddChild(new MeshInstance3D
            {
                Name = "LandscapeShrubCrown", Position = planter + new Vector3(0, 0.99f, z), MaterialOverride = foliage,
                Mesh = new SphereMesh { Radius = 0.27f, Height = 0.54f, RadialSegments = 12, Rings = 6 },
            });
        }
        }
        if (profile.Family == SceneEnvironmentFamily.OutdoorUtility || profile.Variant == "waterfront")
        {
            // A connected service shelter establishes an industrial site at the
            // scale of distributed plants. Its shallow footprint remains wholly
            // beyond the process envelope; no water/pit or roadway is covered.
            var z = (sz > 0 ? bounds.End.Z : bounds.Position.Z) + sz * 1.85f;
            var treatmentPlant = profile.Family == SceneEnvironmentFamily.OutdoorUtility;
            var shelter = new Node3D
            {
                Name = profile.Variant == "waterfront" ? "QuayServiceShelter" : "TreatmentPlantServiceShelter",
                Position = treatmentPlant
                    ? new Vector3((sx > 0 ? bounds.End.X : bounds.Position.X) + sx * 1.85f, 0, center.Z)
                    : new Vector3(center.X, 0, z),
                RotationDegrees = treatmentPlant ? new Vector3(0, sx * 90, 0)
                    : sz < 0 ? new Vector3(0, 180, 0) : Vector3.Zero,
            };
            AddChild(shelter);
            Box(shelter, "ServiceShelterExternalApron", new Vector3(4.2f, 0.12f, 1.85f), new Vector3(0, -0.065f, 0), concrete);
            Box(shelter, "ServiceShelterRearWall", new Vector3(3.6f, 2.75f, 0.16f), new Vector3(0, 1.375f, 0.47f), concrete);
            foreach (var x in new[] { -1.72f, 1.72f })
                Box(shelter, "ServiceShelterReturn", new Vector3(0.16f, 2.75f, 1.02f), new Vector3(x, 1.375f, 0), concrete);
            Box(shelter, "ServiceShelterRoof", new Vector3(3.9f, 0.14f, 1.18f), new Vector3(0, 2.80f, 0), accent);
            var cupboard = new Node3D { Name = "ShelteredFacilitiesCupboard", Position = new Vector3(0.85f, 0, -0.04f) };
            shelter.AddChild(cupboard);
            CatalogServiceCabinet(cupboard, accent, metal);
            var service = new Node3D { Name = "ShelteredMaintenanceSupplies", Position = new Vector3(-0.78f, 0, -0.04f) };
            shelter.AddChild(service);
            if (profile.Family == SceneEnvironmentFamily.OutdoorUtility)
                CatalogUtilitySkid(service, accent, metal, rubber);
            else
            {
                CatalogBench(service, 1.25f, timber, accent, metal);
                CatalogInstrumentCase(service, new Vector3(0, 1.02f, 0), metal, rubber);
            }
        }
    }

    private static void CatalogCylinder(Node3D parent, string name, Vector3 position, float radius,
        float height, Material material, Vector3 rotation)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Position = position, RotationDegrees = rotation, MaterialOverride = material,
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 16 },
        });
    }
}



