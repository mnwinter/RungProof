using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Architecture lives beyond the protected equipment/motion rectangle. The
    // two camera-facing sides remain cut away. Openings are actual wall holes,
    // not dark rectangles pasted onto the repeated original beige room.
    private void BuildArtDirectionArchitecture(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, float rearX, float rearZ, float width, float depth)
    {
        var wall = StudioSceneMaterials.Create(d.WallHex, d.WallStyle);
        var finish = StudioSceneMaterials.Create(d.AccentHex, "paint");
        var structure = StudioSceneMaterials.Create("#555B59", "metal");
        var brightMetal = StudioSceneMaterials.Create("#A2AAA7", "metal");
        var timber = StudioSceneMaterials.Create("#AE8D60", "wood");
        var center = b.GetCenter();
        var architecture = new Node3D { Name = "LocationArchitecture" };
        architecture.SetMeta("locationPurpose", d.LocationLabel);
        architecture.SetMeta("layoutVariant", d.LayoutVariant);
        architecture.SetMeta("architectureFamily", d.Architecture.ToString());
        AddChild(architecture);
        if (TryBuildProductionRoom(d, b, sx, sz, architecture) || TryBuildProcessLocation(d, b, sx, sz, architecture) || TryBuildNextProcessRoom(d, b, sx, sz, architecture) || TryBuildNextProcessRoom2(d, b, sx, sz, architecture) || TryBuildProductionLocations3(d, b, sx, sz, architecture) || TryBuildIndustrialSix(d, b, sx, sz, architecture) || TryBuildRemainingProductionNine(d, b, sx, sz, architecture))
        {
            architecture.SetMeta("purposeLayoutImplemented", true);
            return;
        }        if (TryBuildTeachingSuite(d, b, sx, sz, architecture))
        {
            architecture.SetMeta("purposeLayoutImplemented", true);
            return;
        }
        var height = MathF.Max(2.7f, d.WallHeight);
        var rear = new Node3D { Name = "RearArchitecture", Position = new Vector3(center.X, 0, rearZ) };
        var sideLength = d.Architecture switch
        {
            SceneEnvironmentArchitecture.TrainingLab => depth * .62f,
            SceneEnvironmentArchitecture.MaintenanceBay => depth * .80f,
            SceneEnvironmentArchitecture.WarehouseHall => depth * .72f,
            SceneEnvironmentArchitecture.WashdownHall => depth * .85f,
            SceneEnvironmentArchitecture.MachineHall => depth * .55f,
            SceneEnvironmentArchitecture.UtilityAnnex => depth * .48f,
            SceneEnvironmentArchitecture.OutdoorYard => MathF.Min(4.4f, depth * .38f),
            SceneEnvironmentArchitecture.RoadCorridor => MathF.Min(depth * .45f, 5.0f),
            SceneEnvironmentArchitecture.Waterfront => MathF.Min(depth * .28f, 3.0f),
            SceneEnvironmentArchitecture.PublicService => depth * .60f,
            SceneEnvironmentArchitecture.Gallery => depth * .50f,
            _ => depth,
        };
        var side = new Node3D
        {
            // A shortened return meets the rear building edge; it is not an
            // isolated wall floating halfway down the classroom/site apron.
            Name = "SideArchitecture", Position = new Vector3(rearX, 0, center.Z),
            RotationDegrees = new Vector3(0, 90, 0),
        };
        architecture.AddChild(rear);
        architecture.AddChild(side);

        if (d.LayoutVariant == "dryer-washroom-side-return")
        {
            ArtWashroom(architecture, rear, side, b, sx, sz);
            architecture.SetMeta("purposeLayoutImplemented", true);
            return;
        }
        if (!ArtPurposeLayout(d, rear, side, sx, sz, width, depth, height, wall, structure, finish))
            throw new InvalidOperationException("No purpose layout implementation: " + d.LayoutVariant);
        architecture.SetMeta("purposeLayoutImplemented", true);
    }
    // Roof columns sit on the outward edge, so door/window apertures stay open.
    // Every floating roof member receives a continuous ground-to-roof support.
    private static void ArtSupportedRoof(Node3D rear, Node3D side, int arrangement,
        int sx, int sz, float width, float depth, float h, Material steel, Material finish)
    {
        void Frame(Node3D parent, float length, float outward, string name, bool slab)
        {
            Box(parent, name + "GroundBase", new Vector3(length + .5f, .04f, .90f), new Vector3(0, -.025f, outward), StudioSceneMaterials.Create("#A6A7A1", "concrete"));
            foreach (var x in new[] { -length / 2, length / 2 })
            {
                Box(parent, name + "GroundPost", new Vector3(.16f, h, .16f), new Vector3(x, h / 2, outward), steel);
                Box(parent, name + "CrossHeader", new Vector3(.16f, .18f, .90f), new Vector3(x, h - .09f, 0), steel);
            }
            Box(parent, name + "LongHeader", new Vector3(length + .16f, .18f, .18f), new Vector3(0, h - .09f, outward), steel);
            Box(parent, name + "Roof", new Vector3(length + .16f, .14f, slab ? .90f : .25f), new Vector3(0, h, 0), finish);
        }
        if (arrangement == 1) { Frame(rear, width * .68f, sz * .36f, "RearCanopy", true); }
        else if (arrangement == 2) { Frame(side, depth, sx * .36f, "SideSoffit", true); }
        else if (arrangement == 3)
        {
            Frame(rear, width * .64f, sz * .36f, "Clerestory", false);
            foreach (var x in new[] { -width * .32f, width * .32f })
                Box(rear, "ClerestoryUpperPost", new Vector3(.16f, .55f, .25f), new Vector3(x, h + .25f, 0), steel);
            Box(rear, "ClerestoryUpperBeam", new Vector3(width * .64f + .16f, .15f, .25f), new Vector3(0, h + .48f, 0), steel);
            Box(rear, "ClerestoryPane", new Vector3(width * .64f - .16f, .38f, .025f), new Vector3(0, h + .25f, 0), ArtGlass());
        }
        else if (arrangement == 4)
        {
            Frame(rear, width, sz * .36f, "RearCornerFrame", false);
            Frame(side, depth, sx * .36f, "SideCornerFrame", false);
        }
    }
    // Exact purpose layouts change enclosure footprint, not only finish/trim.
    private bool ArtPurposeLayout(SceneEnvironmentArtDirection d, Node3D rear, Node3D side,
        int sx, int sz, float width, float depth, float h, Material wall, Material steel, Material finish)
    {
        void Wing(Node3D parent, string name, float length, float outward)
        {
            length = MathF.Max(2.2f, length);
            var wing = new Node3D { Name = name };
            parent.AddChild(wing);
            Box(wing, "WingGroundBase", new Vector3(length + .3f, .04f, 2.5f), new Vector3(0, -.025f, outward * 1.1f), StudioSceneMaterials.Create("#A6A7A1", "concrete"));
            ArtWall(wing, length, h, .20f, wall, ArtOpenings(length, h, true, false, true, 1), false);

            foreach (var x in new[] { -length / 2, length / 2 })
                Box(wing, "WingReturn", new Vector3(.20f, h, 2.2f), new Vector3(x, h / 2, outward * 1.1f), wall);
            Box(wing, "WingRear", new Vector3(length, h, .20f), new Vector3(0, h / 2, outward * 2.2f), wall);
            Box(wing, "WingRoof", new Vector3(length + .2f, .16f, 2.4f), new Vector3(0, h, outward * 1.1f), finish);
        }
        if (d.LayoutVariant is "call-open-front-alcove" or "grouping-clear-span-loading")
        {
            ArtFinishedRepresentative(d, rear, side, sx, sz, width, depth, wall, steel, finish);
            return true;
        }
        switch (d.LayoutVariant)
        {
            case "call-open-front-alcove":
                ArtWall(rear, width * .48f, h * .76f, .12f, wall, new(), false);
                foreach (var endX in new[] { -width * .24f, width * .24f })
                    Box(rear, "ApprenticeConnectedReturn", new Vector3(.12f, h * .76f, 1.2f), new Vector3(endX, h * .38f, sz * .6f), wall);
                return true;
            case "inspection-paired-recesses":
                ArtInspectionSuite(rear, side, sx, sz);
                return true;            case "temporary-road-cutback":
                Wing(side, "TemporarySiteOffice", MathF.Min(depth, 3.2f), sx);
                ArtWall(rear, width * .40f, .6f, .25f, wall, new(), false);
                return true;
            case "xy-pallet-open-frame":
                ArtWall(rear, width * .72f, 1.1f, .22f, wall, new(), false);
                ArtSupportedRoof(rear, side, 1, sx, sz, width, depth, h, steel, finish);
                return true;
            case "container-open-loading-shed":
                foreach (var sign in new[] { -1, 1 })
                {
                    var bay = new Node3D { Name = "LoadingWing", Position = new Vector3(sign * width * .32f, 0, 0) };
                    rear.AddChild(bay);
                    Wing(bay, "RecessedLoadingWing", width * .25f, sz);
                }
                return true;
            case "parking-low-garage-facade":
                var holes = new List<Rect2>();
                foreach (var sign in new[] { -1, 1 }) holes.Add(new Rect2(sign * width * .25f - 1.35f, 0, 2.7f, 2.45f));
                ArtWall(rear, width, h, .24f, wall, holes, false);
                Box(side, "GarageDeepReturn", new Vector3(depth, h, .24f), new Vector3(0, h / 2, 0), wall);
                return true;
            case "charging-open-service-island":
                ArtSupportedRoof(rear, side, 2, sx, sz, width, depth * .55f, h, steel, finish);
                return true;
            case "dryer-washroom-side-return":
                ArtWall(rear, width * .66f, h, .16f, wall, new(), false);
                ArtWall(side, depth, h, .16f, wall, ArtOpenings(depth, h, true, false, true, 2), true);
                return true;
            case "shutter-service-return":
                Wing(side, "BuildingServiceVestibule", MathF.Min(depth, 3.8f), sx);
                ArtWall(rear, width * .45f, h * .65f, .22f, wall, new(), false);
                return true;
            case "point-level-low-plant-room":
                Wing(side, "MasonryPumpHouse", MathF.Min(depth, 3.4f), sx);
                ArtWall(rear, width * .50f, .85f, .25f, wall, new(), false);
                return true;
            case "analog-tank-clerestory-room":
                Wing(rear, "InstrumentationAnnex", MathF.Min(width * .40f, 4.0f), sz);
                ArtWall(side, depth * .42f, 1.2f, .16f, wall, new(), false);
                return true;
            case "open-exhibit-pavilion":
                ArtSupportedRoof(rear, side, 4, sx, sz, width, depth, h, steel, finish);
                return true;
            case "volume-measurement-lightwell":
                ArtWall(rear, width * .82f, h, .16f, wall, ArtOpenings(width * .82f, h, true, false, true, 4), true);
                ArtWall(side, depth, h * .82f, .16f, wall, ArtOpenings(depth, h * .82f, false, false, true, 1), true);
                return true;
            default: return ArtRemainingPurposeLayout(d, rear, side, sx, sz, width, depth, h, wall, steel, finish);
        }
    }
    private void ArtWashroom(Node3D architecture, Node3D rear, Node3D side, Aabb b, int sx, int sz)
    {
        // Contract world coordinates are translated into this process-centered
        // root. Only enlarge an edge if actual field bounds need the margin.
        var x0 = MathF.Min(-2.0f - Position.X, b.Position.X - .65f);
        var x1 = MathF.Max(3.4f - Position.X, b.End.X + .65f);
        var z0 = MathF.Min(-2.0f - Position.Z, b.Position.Z - .65f);
        var z1 = MathF.Max(2.8f - Position.Z, b.End.Z + .65f);
        var width = x1 - x0; var depth = z1 - z0; const float h = 2.7f;
        var rearZ = sz < 0 ? z0 : z1;
        var sideX = sx < 0 ? x0 : x1;
        var centerX = (x0 + x1) / 2; var centerZ = (z0 + z1) / 2;
        rear.Position = new Vector3(centerX, 0, rearZ);
        side.Position = new Vector3(sideX, 0, centerZ);
        var painted = StudioSceneMaterials.Create("#D8D9CE", "paint");
        var lower = StudioSceneMaterials.Create("#9BACA7", "paint");
        var satin = StudioSceneMaterials.Create("#B7C7CB", "paint");
        var white = StudioSceneMaterials.Create("#DCDDD1", "paint");
        var windows = new List<Rect2> { new(width * .22f - .5f, 1.9f, 1.0f, .45f) };
        var entryX = sz * depth * .29f;
        var entry = new List<Rect2> { new(entryX - .575f, 0, 1.15f, 2.2f) };
        ArtWall(rear, width, h, .12f, painted, windows, true);
        ArtWall(side, depth, h, .12f, painted, entry, false);
        ArtWall(rear, width, .14f, .16f, lower, new(), false);
        ArtWall(side, depth, .14f, .16f, lower, entry, false);
        var other = new Node3D
        {
            Name = "WashroomConnectedRearReturn", Position = new Vector3(sx < 0 ? x1 : x0, 0, rearZ - sz * .50f),
            RotationDegrees = new Vector3(0, 90, 0),
        };
        architecture.AddChild(other);
        ArtWall(other, 1.0f, h, .12f, painted, new(), false);
        Box(rear, "WashroomRearCornice", new Vector3(width, .14f, .20f), new Vector3(0, h - .07f, 0), lower);
        Box(side, "WashroomSideCornice", new Vector3(depth, .14f, .20f), new Vector3(0, h - .07f, 0), lower);
        Box(other, "WashroomCutawayCap", new Vector3(1.0f, .14f, .20f), new Vector3(0, h - .07f, 0), lower);
        // Wet context is entirely on the dry-side wall reserve. It does not
        // connect to the authored fan/heater, sensor or hand-motion envelope.
        var basinWorldX = MathF.Min(-1.30f, b.Position.X + Position.X - 1.0f);
        var basinWorldZ = -1.37f;
        var basin = new Node3D
        {
            Name = "PassiveWashroomBasin", Position = new Vector3(basinWorldX - Position.X, 0, basinWorldZ - Position.Z),
        };
        architecture.AddChild(basin);
        Box(basin, "GroundedBasinPedestal", new Vector3(.30f, .78f, .30f), new Vector3(0, .39f, 0), white);
        Box(basin, "BasinInsetBottom", new Vector3(.59f, .07f, .42f), new Vector3(0, .795f, 0), satin);
        foreach (var sign in new[] { -1, 1 })
        {
            Box(basin, "BasinSideRim", new Vector3(.08f, .16f, .54f), new Vector3(sign * .31f, .875f, 0), white);
            Box(basin, "BasinFrontRearRim", new Vector3(.54f, .16f, .08f), new Vector3(0, .875f, sign * .23f), white);
        }
        CatalogCylinder(basin, "PassiveTapStem", new Vector3(.18f, 1.03f, -.19f), .025f, .22f, satin, Vector3.Zero);
        Box(basin, "PassiveTapSpout", new Vector3(.05f, .05f, .18f), new Vector3(.18f, 1.14f, -.12f), satin);
        CatalogCylinder(basin, "PassiveWallSupplyPipe", new Vector3(0, .48f, -.25f), .018f, .75f, satin, Vector3.Zero);
        var basinBehind = (basin.Position.Z - rearZ) / 2;
        Box(architecture, "BasinWallMountReturn", new Vector3(.70f, .68f, MathF.Abs(basin.Position.Z - rearZ)),
            new Vector3(basin.Position.X, .34f, rearZ + basinBehind), lower);
        var lessonBacking = StudioSceneMaterials.Create("#DED4BC", "paint");
        var lessonX = -Position.X - rear.Position.X;
        Box(rear, "DryerTeachingReferencePlate", new Vector3(1.70f, .60f, .055f), new Vector3(lessonX, 2.25f, -sz * .10f), lower);
        Box(rear, "DryerTeachingReferencePaper", new Vector3(1.60f, .50f, .015f), new Vector3(lessonX, 2.25f, -sz * .145f), lessonBacking);
        rear.AddChild(new Label3D
        {
            Name = "PassiveDryerCutawayExplanation", Text = "HAND-DRYER\nTEACHING CUTAWAY", FontSize = 32, PixelSize = .003f,
            OutlineSize = 0, Modulate = new Color("3F565A").SrgbToLinear(),
            Position = new Vector3(lessonX, 2.25f, -sz * .165f), RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0),
        });        // Quiet material whole floor plus foundation beneath the joined shell.
        Box(architecture, "WashroomContinuousResinFoundation", new Vector3(width + .4f, .04f, depth + .4f),
            new Vector3(centerX, -.025f, centerZ), StudioSceneMaterials.Create("#AAA99B", "concrete"));
        var diffuser = (StandardMaterial3D)white.Duplicate();
        diffuser.EmissionEnabled = true; diffuser.Emission = new Color("E1E2D5").SrgbToLinear(); diffuser.EmissionEnergyMultiplier = .25f;
        Box(rear, "WashroomSupportedWallFixture", new Vector3(.85f, .12f, .24f), new Vector3(-width * .15f, h - .16f, -sz * .12f), lower);
        Box(rear, "WashroomFixtureDiffuser", new Vector3(.76f, .025f, .20f), new Vector3(-width * .15f, h - .235f, -sz * .12f), diffuser);
        rear.AddChild(new OmniLight3D
        {
            Name = "PassiveWashroomFixtureLight", Position = new Vector3(-width * .15f, h - .35f, -sz * .25f),
            LightColor = new Color("E2E2D8").SrgbToLinear(), LightEnergy = .12f, OmniRange = 5.0f, ShadowEnabled = true,
        });
    }
    private static void ArtInspectionSuite(Node3D rear, Node3D side, int sx, int sz)
    {
        const float h = 2.8f, width = 6.8f, depth = 6.0f;
        // Exact proposed whole-room footprint; signs select world-axis rear
        // edges. Rotation stays0/90; it never follows arbitrary camera yaw.
        rear.Position = new Vector3(rear.Position.X, 0, sz * 3.2f);
        side.Position = new Vector3(sx * 3.4f, 0, sz * .2f);
        var painted = StudioSceneMaterials.Create("#CDD9DB", "paint");
        var frames = StudioSceneMaterials.Create("#667982", "paint");
        var paper = StudioSceneMaterials.Create("#E3DFD0", "paint");
        var windows = new List<Rect2> { new(-2.5f, 1.25f, 1.4f, 1.0f), new(1.1f, 1.25f, 1.4f, 1.0f) };
        var entry = new List<Rect2> { new(sz * 1.7f - .6f, 0, 1.2f, 2.2f) };
        ArtWall(rear, width, h, .16f, painted, windows, true);
        ArtWall(side, depth, h, .16f, StudioSceneMaterials.CreateDetailed("#829D94", "paint"), entry, false);
        var opposite = new Node3D
        {
            Name = "InspectionRearConnectedReturn", Position = new Vector3(-sx * 3.4f, 0, sz * 2.1f),
            RotationDegrees = new Vector3(0, 90, 0),
        };
        rear.GetParent().AddChild(opposite);
        ArtWall(opposite, 2.2f, h, .16f, painted, new(), false);
        Box(rear, "InspectionRoomCornice", new Vector3(width, .16f, .24f), new Vector3(0, h - .08f, 0), frames);
        Box(side, "InspectionSideCornice", new Vector3(depth, .16f, .24f), new Vector3(0, h - .08f, 0), frames);
        Box(opposite, "InspectionReturnCornice", new Vector3(2.2f, .16f, .24f), new Vector3(0, h - .08f, 0), frames);
        foreach (var sign in new[] { -1, 1 })
        {
            var centerX = sign * 2.05f;
            var taskFinish = StudioSceneMaterials.Create(sign < 0 ? "#8CA8AC" : "#B89E79", "paint");
            var tray = StudioSceneMaterials.Create("#AEBAB7", "paint");
            var sample = StudioSceneMaterials.Create(sign < 0 ? "#BDA17E" : "#778B8F", sign < 0 ? "cardboard" : "rubber");
            Box(rear, "InspectionTaskSurface", new Vector3(1.48f, .08f, .70f), new Vector3(centerX, 1.08f, -sz * .54f), taskFinish);
            Box(rear, "PassiveSampleTray", new Vector3(.58f, .035f, .40f), new Vector3(centerX, 1.14f, -sz * .54f), tray);
            Box(rear, "PassiveInspectionSample", new Vector3(.24f, .13f, .23f), new Vector3(centerX, 1.22f, -sz * .54f), sample);
            Box(rear, "InspectorBayLabelBacking", new Vector3(.65f, .34f, .035f), new Vector3(centerX, 1.06f, -sz * .12f), paper);
            rear.AddChild(new Label3D
            {
                Name = sign < 0 ? "PassiveInspectorABay" : "PassiveInspectorBBay", Text = sign < 0 ? "A" : "B",
                FontSize = 72, PixelSize = .005f, OutlineSize = 0, Modulate = new Color("405861").SrgbToLinear(),
                Position = new Vector3(centerX, 1.06f, -sz * .15f), RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0),
            });            // Low inspection surround is a passive architectural recess, not
            // a second workstation or a personnel doorway. All ends join rear.
            Box(rear, "InspectionLowFront", new Vector3(1.5f, 1.05f, .14f), new Vector3(centerX, .525f, -sz * .9f), frames);
            foreach (var endX in new[] { centerX - .75f, centerX + .75f })
                Box(rear, "InspectionConnectedLowReturn", new Vector3(.14f, 1.05f, .90f), new Vector3(endX, .525f, -sz * .45f), frames);
        }
        Box(rear, "InspectionPassiveRaceway", new Vector3(width - .30f, .12f, .10f), new Vector3(0, .55f, -sz * .12f), frames);
        Box(side, "InspectionTeachingNoticeFrame", new Vector3(1.0f, .75f, .08f), new Vector3(-sz * 1.25f, 1.55f, -sx * .12f), frames);
        Box(side, "InspectionTeachingNoticeFace", new Vector3(.90f, .65f, .02f), new Vector3(-sz * 1.25f, 1.55f, -sx * .17f), paper);
        side.AddChild(new Label3D
        {
            Name = "PassiveInspectionNotice", Text = "INSPECTION\nTWO INDEPENDENT VOTES", FontSize = 20, PixelSize = .002f,
            OutlineSize = 0, Modulate = new Color("344C57").SrgbToLinear(),
            Position = new Vector3(-sz * 1.25f, 1.55f, -sx * .19f), RotationDegrees = new Vector3(0, sx > 0 ? 180 : 0, 0),
        });
        var diffuser = (StandardMaterial3D)StudioSceneMaterials.Create("#E2E7DE", "paint").Duplicate();
        diffuser.EmissionEnabled = true; diffuser.Emission = new Color("D3DFD7").SrgbToLinear(); diffuser.EmissionEnergyMultiplier = .25f;
        Box(rear, "InspectionEdgeFixture", new Vector3(1.2f, .10f, .30f), new Vector3(0, h - .16f, -sz * .12f), frames);
        Box(rear, "InspectionEdgeDiffuser", new Vector3(1.08f, .03f, .26f), new Vector3(0, h - .225f, -sz * .12f), diffuser);
        rear.AddChild(new OmniLight3D
        {
            Name = "PassiveInspectionRoomLight", Position = new Vector3(0, h - .35f, -sz * .30f),
            LightColor = new Color("DFE7DF").SrgbToLinear(), LightEnergy = .16f, OmniRange = 6.0f, ShadowEnabled = true,
        });
    }
    // These two complete locations are the human quality gate. Both camera-
    // facing sides are cut away; rear and side returns meet the exact corner.
    private static void ArtFinishedRepresentative(SceneEnvironmentArtDirection d, Node3D rear, Node3D side,
        int sx, int sz, float w, float depth, Material wall, Material steel, Material finish)
    {
        if (d.LayoutVariant == "call-open-front-alcove")
        {
            const float h = 2.8f;
            var painted = ReferenceRoleSurface("#DED0B4", "plaster");
            var skirting = ReferenceRoleSurface("#354F51", "paint");
            steel = RepresentativeSurface("#9D805F", "wood", .72f, 1.2f, .08f);
            var rearWindows = new List<Rect2> { new(-.9f, 1.3f, 1.8f, .95f) };
            var entry = new List<Rect2> { new(-sz * depth * .22f - .575f, 0, 1.15f, 2.2f) };
            ArtWall(rear, w, h, .16f, painted, rearWindows, true, skirting);
            ArtWall(side, depth, h, .16f, ReferenceRoleSurface("#527D78", "paint"), entry, false);
            ArtWall(rear, w, .14f, .20f, skirting, new(), false);
            ArtWall(side, depth, .14f, .20f, skirting, entry, false);
            // Single real service raceway. This does not connect to PLC heads.
            Box(rear, "WallUtilityRaceway", new Vector3(w - .3f, .12f, .11f), new Vector3(0, .62f, -sz * .11f), skirting);
            var notice = StudioSceneMaterials.CreateDetailed("#E8DFBF", "cardboard");
            Box(side, "FramedTeachingNotice", new Vector3(.9f, .72f, .08f), new Vector3(sz * depth * .18f, 1.50f, -sx * .12f), steel);
            Box(side, "NeutralNoticeFace", new Vector3(.80f, .62f, .02f), new Vector3(sz * depth * .18f, 1.50f, -sx * .17f), notice);
            var classroomNotice = new Label3D
            {
                Name = "PassiveTrainingNotice", Text = "CONTROLS TRAINING\nREQUEST / ACKNOWLEDGE",
                FontSize = 22, PixelSize = .002f, OutlineSize = 0, Modulate = new Color("364744").SrgbToLinear(),
                Position = new Vector3(sz * depth * .18f, 1.50f, -sx * .19f),
                RotationDegrees = new Vector3(0, sx > 0 ? 180 : 0, 0),
            };
            side.AddChild(classroomNotice);            // Roof cutaway: connected corner trim rests directly on walls.
            Box(rear, "ClassroomRearCornice", new Vector3(w, .16f, .28f), new Vector3(0, h - .08f, 0), painted);
            Box(side, "ClassroomSideCornice", new Vector3(depth, .16f, .28f), new Vector3(0, h - .08f, 0), painted);
            Box(rear, "HighBayDiffuser", new Vector3(1.10f, .08f, .30f), new Vector3(w * .22f, h - .16f, -sz * .14f), StudioSceneMaterials.Create("#D9DEDA", "paint"));
            rear.AddChild(new OmniLight3D
            {
                Name="PassiveClassroomDiffuserFill", Position=new Vector3(w*.22f,h-.32f,-sz*.24f),
                // Broad local diffuse fill retains plaster/underside detail;
                // the global key still supplies the contact-shadow direction.
                LightColor=new Color("#E5E8E4").SrgbToLinear(),LightEnergy=.14f,OmniRange=5.5f,ShadowEnabled=false,
            });
            return;
        }
        const float hallH = 3.8f;
        var coatedCladding = ReferenceRoleSurface("#C0CDD0", "coated-steel");
        wall = coatedCladding;
        steel = ReferenceRoleSurface("#3C5564", "paint");
        var holes = new List<Rect2> { new(-1.6f, 0, 3.2f, 2.7f), new(-w * .30f - 1.3f, 2.85f, 2.6f, .50f) };
        ArtWall(rear, w, hallH, .18f, wall, holes, true, steel);
        ArtWall(side, depth, hallH, .18f, ReferenceRoleSurface("#405D70", "paint"), new(), false);
        // Structural bays have unequal spans; they are construction, not a
        // repeated panel-line overlay or striped material texture.
        foreach (var x in new[] { -w / 2, -w * .20f, w / 2 })
            Box(rear, "HallGroundedSteelColumn", new Vector3(.26f, hallH, .32f), new Vector3(x, hallH / 2, -sz * .08f), steel);
        foreach (var x in new[] { -depth / 2, depth / 2 })
            Box(side, "HallSideSteelColumn", new Vector3(.26f, hallH, .32f), new Vector3(x, hallH / 2, -sx * .08f), steel);
        Box(rear, "HallRearGirder", new Vector3(w + .2f, .28f, .34f), new Vector3(0, hallH - .14f, 0), steel);
        Box(side, "HallSideGirder", new Vector3(depth + .2f, .28f, .34f), new Vector3(0, hallH - .14f, 0), steel);
        foreach (var x in new[] { -1.74f, 1.74f })
        {
            Box(rear, "DockSteelJamb", new Vector3(.26f, 2.85f, .34f), new Vector3(x, 1.425f, 0), steel);
            Box(rear, "LoadingBayReveal", new Vector3(.18f, 2.85f, 2.2f), new Vector3(x, 1.425f, sz * 1.1f), wall);
        }
        Box(rear, "DockLoadingHeader", new Vector3(3.75f, .22f, .38f), new Vector3(0, 2.82f, 0), steel);
        var recessPaint = ReferenceRoleSurface("#7A8580", "concrete");
        Box(rear, "LoadingBayBackWall", new Vector3(3.5f, 2.85f, .18f), new Vector3(0, 1.425f, sz * 2.2f), recessPaint);
        Box(rear, "LoadingBayGroundContinuation", new Vector3(3.7f, .04f, 2.5f), new Vector3(0, -.025f, sz * 1.1f), ReferenceRoleSurface("#8D9692", "concrete"));
        Box(rear, "LoadingBaySoffit", new Vector3(3.7f, .16f, 2.4f), new Vector3(0, 2.9f, sz * 1.1f), finish);
        ArtSupportedRoof(rear, side, 1, sx, sz, w, depth, hallH, steel, finish);
        // Fixtures are mounted directly beneath the girders, rather than a
        // suspended glowing prop without any construction attachment.
        var diffuser = (StandardMaterial3D)StudioSceneMaterials.Create("#E5E9E2", "paint").Duplicate();
        diffuser.EmissionEnabled = true; diffuser.Emission = new Color("D8E1D8").SrgbToLinear(); diffuser.EmissionEnergyMultiplier = .6f;
        foreach (var lightX in new[] { -w * .30f, w * .27f })
        {
            Box(rear, "LightingBracket", new Vector3(.12f, .20f, .25f), new Vector3(lightX, hallH - .29f, -sz * .13f), steel);
            Box(rear, "HallOverheadFixture", new Vector3(1.3f, .12f, .38f), new Vector3(lightX, hallH - .40f, -sz * .20f), finish);
            Box(rear, "HallQuietDiffuser", new Vector3(1.18f, .025f, .32f), new Vector3(lightX, hallH - .472f, -sz * .20f), diffuser);
            rear.AddChild(new OmniLight3D
            {
                Name = "PassiveHallFixtureLight", Position = new Vector3(lightX, hallH - .55f, -sz * .36f),
                LightColor = new Color("E4E7DD").SrgbToLinear(), LightEnergy = .16f,
                OmniRange = 10.0f, ShadowEnabled = false,
            });
        }
        rear.AddChild(new OmniLight3D
        {
            Name = "PassiveLoadingRecessLight", Position = new Vector3(0, 2.35f, sz * 1.10f),
            LightColor = new Color("E8E2D4").SrgbToLinear(), LightEnergy = .16f,
            OmniRange = 4.0f, ShadowEnabled = true,
        });        // Small packing-material staging sits against the other side-wall
        // segment. Its .55m inward reach leaves approximately1m to the process
        // reserve; no shelf/bench behind the teaching plant is reintroduced.
        var stagingX = -sz * depth * .25f;
        var wood = RepresentativeSurface("#AE865B", "wood", .78f, 1.2f, .08f);
        var carton = RepresentativeSurface("#BD955F", "cardboard", .94f, 1f, .025f);
        var floorZone = ReferenceRoleSurface("#857968", "concrete");
        Box(side, "PackingMaterialsFloorZone", new Vector3(4.0f, .012f, .90f), new Vector3(stagingX, -.002f, -sx * .20f), floorZone);
        foreach (var x in new[] { -.75f, 0, .75f })
            Box(side, "PalletTimberRunner", new Vector3(.15f, .13f, .52f), new Vector3(stagingX + x, .065f, -sx * .28f), wood);
        Box(side, "PalletTimberDeck", new Vector3(2.0f, .08f, .58f), new Vector3(stagingX, .17f, -sx * .28f), wood);
        foreach (var x in new[] { -.63f, 0, .63f })
            Box(side, "ClosedPackingCarton", new Vector3(.59f, .47f, .48f), new Vector3(stagingX + x, .445f, -sx * .28f), carton);
        foreach (var x in new[] { -.32f, .32f })
            Box(side, "StackedPackingCarton", new Vector3(.59f, .47f, .48f), new Vector3(stagingX + x, .915f, -sx * .28f), carton);
        var cream = StudioSceneMaterials.Create("#DFD8C2", "paint");
        Box(side, "PackingNoticeBackplate", new Vector3(2.2f, .90f, .05f), new Vector3(stagingX, 1.85f, -sx * .12f), cream);
        var packingSign = new Label3D
        {
            Name = "PassivePackingMaterialsLabel", Text = "DISPATCH\nPACKING",
            FontSize = 64, PixelSize = .005f, OutlineSize = 0, Modulate = new Color("344B55").SrgbToLinear(),
            Position = new Vector3(stagingX, 1.85f, -sx * .16f),
            RotationDegrees = new Vector3(0, sx > 0 ? 180 : 0, 0),
        };
        side.AddChild(packingSign);        // A single closed passive service cabinet against the side wall. The
        // 1.05m clear band to protected process remains a continuous approach.
        var utility = StudioSceneMaterials.Create("#748891", "paint");
        Box(side, "HallClosedUtilityCabinet", new Vector3(.85f, 1.55f, .4f), new Vector3(sz * depth * .26f, .775f, -sx * .30f), utility);
        Box(side, "HallUtilityDoor", new Vector3(.75f, 1.40f, .025f), new Vector3(sz * depth * .26f, .78f, -sx * .515f), wall);
    }
    // Detailed textures are opt-in to this dispatch candidate; cached originals
    // remain untouched. Duplicate before changing its material response.
    private static StandardMaterial3D RepresentativeSurface(string hex, string family, float roughness,
        float textureScale, float normalScale)
    {
        // Scoped reference finishes: preserve cached maps and calibrate their
        // roughness around the requested mean, rather than multiply that mean.
        var material = (StandardMaterial3D)StudioSceneMaterials.CreateDetailed(hex, family).Duplicate();
        var originalMean=family switch { "metal"=>.42f,"paint"=>.58f,"wood"=>.72f,_=>.92f };
        var map=material.RoughnessTexture.GetImage();
        for(var y=0;y<map.GetHeight();y++)for(var x=0;x<map.GetWidth();x++)
        {
            var value=Mathf.Clamp(roughness+map.GetPixel(x,y).R-originalMean,.2f,1);
            map.SetPixel(x,y,new Color(value,value,value));
        }
        map.GenerateMipmaps();
        material.Roughness = 1;
        material.RoughnessTexture=ImageTexture.CreateFromImage(map);
        material.Uv1Scale=Vector3.One*textureScale;
        material.NormalScale=normalScale;
        return material;
    }
    // Every ledger key has an explicit footprint composition. Local outward Z
    // maps directly to sz on rear walls and sx on the exactly-90-degree side.
    private bool ArtRemainingPurposeLayout(SceneEnvironmentArtDirection d, Node3D rear, Node3D side,
        int sx, int sz, float w, float dep, float h, Material wall, Material steel, Material finish)
    {
        void Panel(Node3D edge, string name, float len, float ht, float offset = 0, int openings = 0)
        {
            var n = new Node3D { Name = name, Position = new Vector3(offset, 0, 0) };
            edge.AddChild(n);
            ArtWall(n, len, ht, .20f, wall, ArtOpenings(len, ht, openings is 1 or 2 && len >= 1.6f,
                openings == 2 && len >= 3.2f, openings >= 3, openings == 4 ? 4 : 1), openings >= 3);
        }
        void Niche(Node3D edge, string name, float len, float ht, float offset, float projection,
            int outward, bool enclosed = false, int opening = 0, float secondProjection = 0)
        {
            len = MathF.Max(enclosed ? 2.2f : 1.5f, len);
            var n = new Node3D { Name = name, Position = new Vector3(offset, 0, 0) };
            edge.AddChild(n);
            Box(n, "GroundedAnnexBase", new Vector3(len + .3f, .04f, MathF.Max(projection, secondProjection) + .3f),
                new Vector3(0, -.025f, outward * MathF.Max(projection, secondProjection) / 2), StudioSceneMaterials.Create("#A6A7A1", "concrete"));
            ArtWall(n, len, ht, .20f, wall, ArtOpenings(len, ht, opening is 1 or 2, opening == 2,
                opening >= 3, opening == 4 ? 4 : 1), opening >= 3);
            foreach (var sign in new[] { -1, 1 })
            {
                var reach = sign > 0 && secondProjection > 0 ? secondProjection : projection;
                Box(n, "ConnectedCheekReturn", new Vector3(.20f, ht, reach), new Vector3(sign * len / 2, ht / 2, outward * reach / 2), wall);
            }
            if (enclosed)
            {
                Box(n, "AnnexBack", new Vector3(len, ht, .20f), new Vector3(0, ht / 2, outward * projection), wall);
                Box(n, "SupportedAnnexRoof", new Vector3(len + .20f, .16f, projection + .2f), new Vector3(0, ht, outward * projection / 2), finish);
            }
        }
        void Rear(float length, float ht, float offset = 0, int openings = 0) => Panel(rear, "PurposeRearPartition", length, ht, offset, openings);
        void Side(float length, float ht, float offset = 0, int openings = 0) => Panel(side, "PurposeSidePartition", length, ht, offset, openings);
        void R(float length, float ht, float offset, float projection, bool enclosed = false, int opening = 0, float other = 0)
            => Niche(rear, "PurposeRearAnnex", length, ht, offset, projection, sz, enclosed, opening, other);
        void S(float length, float ht, float offset, float projection, bool enclosed = false, int opening = 0)
            => Niche(side, "PurposeSideAnnex", length, ht, offset, projection, sx, enclosed, opening);
        void Roof(int arrangement, float width, float depth, float height) => ArtSupportedRoof(rear, side, arrangement, sx, sz, width, depth, height, steel, finish);
        switch (d.LayoutVariant)
        {
            // Instructional benches: illustrative teaching architecture only.
            case "handoff-split-window": Rear(w * .28f, h * .58f, -w * .30f); Rear(w * .20f, h * .72f, w * .27f); Side(dep * .20f, h * .58f); break;
            case "service-marker-recess": R(w * .35f, h, -w * .15f, 1.2f, true, 1); Side(dep * .20f, .8f); break;
            case "two-station-wide-return": R(w * .25f, 1.1f, -w * .32f, .8f); R(w * .25f, 1.1f, w * .32f, 1.4f); break;
            case "lighting-high-side-slot": S(dep * .55f, h, 0, 1.3f, true, 2); Rear(w * .25f, .9f, w * .25f); break;
            case "attention-service-recess": R(w * .30f, h * .85f, -w * .22f, .9f, true, 1); Side(dep * .22f, 1.0f); break;
            case "contacts-corner-portal": Rear(w * .40f, h, sx * w * .30f); Side(dep * .35f, h, sz * dep * .20f, 1); break;
            case "beacon-tall-single-recess": R(w * .30f, h, 0, .8f); break;
            case "inspection-low-side-window": Side(dep * .45f, h * .82f, 0, 3); break;
            case "pulse-count-wide-rear-slot": R(w * .60f, h * .70f, 0, .9f, false, 1, .1f); break;
            case "counter-reset-side-recess": S(dep * .44f, h, 0, 1.1f, true, 1); Rear(w * .30f, .85f, w * .25f); break;
            case "dual-count-paired-bays": R(w * .24f, h * .80f, -w * .33f, 1.4f); R(w * .24f, h * .80f, w * .33f, .8f); break;
            case "confirmation-offset-lightwell": R(w * .45f, h * .85f, 0, 1.0f, false, 0, 1.5f); Side(dep * .22f, .7f); break;
            case "delay-on-overhead-recess": S(dep * .40f, h, 0, 1.1f, true, 1); Rear(w * .22f, .7f, w * .26f); break;
            case "timed-service-low-canopy": R(w * .30f, h * .70f, -w * .18f, 1.2f, true, 1); break;
            case "flasher-side-service-bay": Side(dep * .50f, .85f); Roof(2, w, dep * .50f, h); break;
            case "alternation-two-window-room": R(w * .22f, h * .70f, -w * .31f, .9f); R(w * .22f, h * .70f, w * .31f, .9f); Side(dep * .20f, .65f); break;
            case "flash-rate-corner-lightwell": Side(dep * .60f, h, 0, 3); R(w * .25f, h, w * .25f, 1.2f, true, 1); break;
            case "running-tower-open-canopy": R(w * .36f, 1.25f, 0, .9f, false, 1); Roof(1, w * .60f, dep, h); break;
            case "sum-horizontal-light-slot": R(w * .70f, 1.3f, 0, .7f, false, 1, 1.2f); break;
            case "product-deep-side-window": Side(dep * .65f, h, 0, 3); R(w * .25f, h, w * .27f, 1.1f); break;
            case "sum-count-paired-height-bays": R(w * .24f, h * .55f, -w * .30f, .9f); R(w * .24f, h * .85f, w * .30f, 1.4f); Side(dep * .25f, .75f); break;
            case "function-corner-service-recess": Rear(w * .35f, h, sx * w * .325f); S(dep * .45f, h, sz * dep * .22f, 1.2f, false, 1); break;
            // Machine/process adjacency is passive and has no field connection.
            case "inspection-side-light": Rear(w * .55f, h, -w * .12f, 3); R(w * .24f, h, w * .32f, 1.3f, true, 1); Side(dep * .25f, h * .55f); break;
            case "diagnostics-offset-door": R(w * .42f, h, -w * .18f, 1.4f, true, 1); Side(dep * .30f, .9f); break;
            case "label-room-transfer-hatch": Rear(w * .55f, h, -w * .20f, 3); R(w * .28f, h * .85f, w * .29f, 1.1f, true, 1); Side(dep * .35f, h); break;
            case "vision-high-span": R(w * .24f, h, -w * .36f, 1.5f, true, 1); R(w * .24f, h, w * .36f, 1.2f, true, 1); Side(dep * .25f, .8f); break;
            case "motor-state-single-bay": Rear(w * .45f, h); S(dep * .22f, h * .82f, sz * dep * .25f, 1.0f, true, 1); break;
            case "motor-data-window-return": R(w * .70f, h, 0, 1.5f, false, 4); break;
            case "ten-drive-long-hall": foreach (var x in new[] { -w * .32f, 0, w * .32f }) R(w * .28f, h, x, 1.0f, false, 3); S(dep * .35f, h, sz * dep * .20f, 1.5f, true, 1); break;
            case "wastewater-open-header-yard": ArtWastewaterSite(rear.GetParent() as Node3D ?? throw new InvalidOperationException("Missing wastewater architecture")); break;
            case "receiving-staggered-bays": R(w * .30f, h, -w * .32f, 1.8f, true, 2); R(w * .22f, h * .82f, w * .34f, 1.3f, true, 1); Side(dep * .20f, .75f); break;
            case "service-lift-two-landings": R(w * .55f, MathF.Max(h, 4.5f), -w * .20f, 1.5f, true, 3); R(w * .25f, 2.4f, w * .30f, 1.2f, true, 1); break;
            case "powder-high-pitched-bay": R(w * .25f, h * .80f, -w * .33f, 1.2f, true, 1); R(w * .20f, h, w * .34f, 1.8f, true, 3); Rear(w * .35f, h); break;
            case "dust-side-canopy": S(dep * .40f, h * .75f, 0, 1.3f, true, 1); Rear(w * .40f, .85f); break;
            case "tote-receiving-side-door": S(dep * .45f, h, 0, 1.5f, true, 2); Rear(w * .35f, 1.1f); break;
            case "lift-open-bay-pilasters": R(w * .25f, 1.2f, -w * .32f, 1.0f); R(w * .25f, 1.2f, w * .32f, .7f); S(dep * .25f, 2.6f, 0, 1.1f, true, 1); break;
            case "coolant-low-washdown-recess": R(w * .35f, h * .80f, -w * .18f, 1.3f, true, 1); Side(dep * .30f, .7f); break;
            case "sump-open-pit-annex": S(dep * .40f, h * .75f, 0, 1.2f, true, 1); Rear(w * .26f, .6f, -w * .30f); Rear(w * .20f, .6f, w * .32f); break;
            case "weld-dark-side-soffit": Side(dep * .45f, h, 0, 3); Rear(w * .35f, h * .65f, w * .25f); Roof(2, w, dep * .45f, h + .2f); break;
            case "drill-paired-side-posts": R(w * .45f, h, 0, 1.1f); break;
            case "pickup-rear-dock-canopy": S(dep * .50f, h, 0, 1.6f, true, 2); Rear(w * .25f, .8f, w * .26f); break;
            case "bottle-narrow-clean-bay": R(w * .65f, h, 0, .9f, false, 3); break;
            case "tote-long-service-gallery": R(w * .22f, h, -w * .34f, 1.0f, true, 1); R(w * .22f, h * .85f, 0, 1.4f, true, 3); R(w * .22f, h, w * .34f, 1.8f, true, 1); break;
            case "dual-head-twin-rear-bays": R(w * .28f, h, -w * .32f, 1.2f, true, 1); R(w * .28f, h, w * .32f, 1.2f, true, 1); Side(dep * .22f, .8f); break;
            case "parcel-three-destination-hall": R(w * .28f, h, 0, 1.2f, true, 2); S(dep * .22f, h, -dep * .34f, 1.4f, true, 1); S(dep * .22f, h * .80f, dep * .34f, 1.2f, true, 1); break;
            case "cnc-offset-high-bay": R(w * .48f, h, w * .24f, 1.7f, true, 3); Side(dep * .30f, .8f, -dep * .15f); break;
            case "guard-transfer-wide-side-open": Side(dep * .60f, h, 0, 3); Rear(w * .25f, .85f, w * .27f); break;
            case "restart-high-corner-frame": R(w * .25f, h, -w * .32f, 1.0f, true, 3); R(w * .25f, h, w * .32f, 1.0f, true, 3); Rear(w * .40f, .8f); break;
            case "cycle-counter-deep-service-bay": R(w * .45f, h, 0, 1.8f, true, 1); Side(dep * .25f, .8f); break;
            case "sequence-four-color-open-return": Rear(w * .22f, .75f, -w * .32f); Rear(w * .22f, 1.0f, w * .32f); Roof(1, w * .40f, dep, h); break;
            case "grouping-clear-span-loading":
                ArtWall(rear, w, h, .18f, wall, new List<Rect2> { new(-1.35f, 0, 2.7f, 2.45f) }, false);
                foreach (var x in new[] { -1.47f, 1.47f })
                {
                    Box(rear, "LoadingSteelJamb", new Vector3(.24f, 2.65f, .30f), new Vector3(x, 1.325f, 0), steel);
                    Box(rear, "DeepLoadingReveal", new Vector3(.18f, 2.65f, 2.2f), new Vector3(x, 1.325f, sz * 1.1f), wall);
                }
                Box(rear, "LoadingSteelHeader", new Vector3(3.2f, .24f, .34f), new Vector3(0, 2.57f, 0), steel);
                Side(dep * .35f, h * .72f); Roof(1, w, dep, h); break;
            case "chain-lift-high-low-wall": R(w * .40f, MathF.Max(h, 4.2f), -w * .24f, 1.6f, true, 3); R(w * .25f, 2.5f, w * .30f, .9f, true, 1); Side(dep * .20f, .7f); break;
            case "bakery-clean-transfer-bays": R(w * .26f, h, -w * .32f, 1.1f, true, 1); R(w * .26f, h, w * .32f, 1.1f, true, 1); Rear(w * .40f, 1.1f); break;
            case "barrel-open-utility-canopy": Rear(w * .50f, .85f); Roof(1, w, dep, h); break;
            case "cable-long-side-light": R(w * .70f, h, -w * .05f, 1.2f, false, 3, .1f); break;
            case "crossing-sidewalk-edge": R(w * .32f, h, -w * .17f, 1.1f, true, 1); R(w * .32f, h * .86f, w * .17f, 1.1f, true, 1); Side(dep * .30f, .65f); break;
            case "bridge-canal-quayside": Rear(w * .20f, .6f, -w * .36f); Rear(w * .20f, .6f, w * .36f); S(MathF.Min(dep * .30f, 3.0f), 2.6f, 0, 1.2f, true, 1); break;
            case "bag-index-deep-side-opening": S(dep * .45f, h, 0, 1.5f, true, 2); Rear(w * .22f, .8f, -w * .33f); Rear(w * .22f, .8f, w * .33f); break;
            case "coating-exhaust-clerestory": R(w * .50f, h, -w * .18f, 1.4f, true, 3); Side(dep * .30f, .9f); break;
            case "baggage-wide-terminal-wall":
                var terminalHoles = new List<Rect2> { new(-w * .24f - .575f, 0, 1.15f, 2.45f), new(w * .24f - .575f, 0, 1.15f, 2.45f) };
                ArtWall(rear, w * .80f, h, .20f, wall, terminalHoles, false);
                S(dep * .25f, h, sz * dep * .28f, 1.2f, true, 1); break;
            case "pallet-count-open-dock-header": R(w * .35f, h, -w * .22f, 1.3f, true, 2); Side(dep * .30f, .85f); break;
            case "stop-conveyor-short-dock": Rear(w * .60f, h, 0, 2); Side(dep * .20f, .85f); break;
            case "pusher-open-takeaway-side": R(w * .45f, h, -w * .18f, 1.4f, true, 2); break;
            case "radar-open-roof-sky": Rear(w * .22f, .6f, -w * .32f); Rear(w * .22f, .6f, w * .32f); S(dep * .30f, 2.7f, 0, 1.2f, true, 1); break;
            default: return false;
        }
        return true;
    }
    // Opening arrangements are deliberate architectural types: high daylight
    // slot, side light, paired panes, or corner clerestory. No random decoration.
    private static List<Rect2> ArtOpenings(float length, float height, bool portal,
        bool widePortal, bool windows, int windowArrangement)
    {
        var holes = new List<Rect2>();
        if (portal)
        {
            var w = MathF.Min(widePortal ? 2.7f : 1.15f, MathF.Max(.30f, length - .40f));
            var x = widePortal ? 0 : MathF.Max(-length * .23f, -length / 2 + w / 2 + .22f);
            holes.Add(new Rect2(x - w / 2, 0, w, MathF.Min(2.45f, height - .22f)));
        }
        if (windows && height >= 2.4f)
        {
            var y = windowArrangement is 1 or 4 ? height - .85f : 1.25f;
            var h = windowArrangement is 1 or 4 ? .48f : MathF.Min(1.10f, height - y - .20f);
            var w = MathF.Min(length * .22f, 1.85f);
            if (windowArrangement is 3 or 4)
                foreach (var sign in new[] { -1, 1 }) holes.Add(new Rect2(sign * length * .23f - w / 2, y, w, h));
            else
                holes.Add(new Rect2(length * .24f - w / 2, y, w, h));
        }
        // A glazed observation pane must never cross a ground-level portal.
        var doors = holes.FindAll(r => r.Position.Y < .1f);
        holes.RemoveAll(r => r.Position.Y > .1f && doors.Exists(door => r.Intersects(door.Grow(.06f))));        return holes;
    }

    private static void ArtWall(Node3D parent, float length, float height, float thickness,
        Material wall, List<Rect2> openings, bool glaze, Material? windowFrame = null)
    {
        var pieces = new List<Rect2> { new(-length / 2, 0, length, height) };
        foreach (var hole in openings)
        {
            if (hole.Position.Y < .1f && hole.End.Y <= height && hole.Position.X >= -length / 2 && hole.End.X <= length / 2)
            {
                var portal = new Node3D { Name = "ArchitecturalPortal" };
                portal.SetMeta("localOpeningBounds", new Aabb(new Vector3(hole.Position.X, 0, -thickness / 2),
                    new Vector3(hole.Size.X, hole.Size.Y, thickness)));
                parent.AddChild(portal);
            }
        }        foreach (var hole in openings)
        {
            var next = new List<Rect2>();
            foreach (var p in pieces)
            {
                var cut = p.Intersection(hole);
                if (cut.Size.X <= .001f || cut.Size.Y <= .001f) { next.Add(p); continue; }
                if (cut.Position.X > p.Position.X) next.Add(new Rect2(p.Position, new Vector2(cut.Position.X - p.Position.X, p.Size.Y)));
                if (cut.End.X < p.End.X) next.Add(new Rect2(cut.End.X, p.Position.Y, p.End.X - cut.End.X, p.Size.Y));
                if (cut.Position.Y > p.Position.Y) next.Add(new Rect2(cut.Position.X, p.Position.Y, cut.Size.X, cut.Position.Y - p.Position.Y));
                if (cut.End.Y < p.End.Y) next.Add(new Rect2(cut.Position.X, cut.End.Y, cut.Size.X, p.End.Y - cut.End.Y));
            }
            pieces = next;
            // Door portals remain empty. Windows use four separate jamb/sill
            // members and optional translucent glazing; wall is absent behind.
            if (hole.Position.Y > .1f && hole.End.Y <= height)
            {
                var frame = windowFrame ?? StudioSceneMaterials.Create("#626F71", "metal");
                foreach (var x in new[] { hole.Position.X, hole.End.X })
                    Box(parent, "WindowJamb", new Vector3(.075f, hole.Size.Y + .08f, thickness + .05f), new Vector3(x, hole.GetCenter().Y, 0), frame);
                foreach (var y in new[] { hole.Position.Y, hole.End.Y })
                    Box(parent, "WindowSillLintel", new Vector3(hole.Size.X + .08f, .075f, thickness + .05f), new Vector3(hole.GetCenter().X, y, 0), frame);
                if (glaze) Box(parent, "WindowDaylightGlazing", new Vector3(hole.Size.X, hole.Size.Y, .025f),
                    new Vector3(hole.GetCenter().X, hole.GetCenter().Y, 0), ArtGlass());
            }
        }
        foreach (var p in pieces)
            Box(parent, "ArchitecturalWallSegment", new Vector3(p.Size.X, p.Size.Y, thickness), new Vector3(p.GetCenter().X, p.GetCenter().Y, 0), wall);
    }

    private static StandardMaterial3D ArtGlass() => new()
    {
        AlbedoColor = new Color(.21f, .36f, .40f, .24f),
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        Roughness = .30f, Metallic = .05f,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };

    private static void ArtHeaderFrame(Node3D rear, float width, float height, Material steel)
    {
        foreach (var x in new[] { -width * .46f, width * .46f })
            Box(rear, "SteelPortalColumn", new Vector3(.24f, height, .30f), new Vector3(x, height / 2, 0), steel);
        Box(rear, "SteelPortalHeader", new Vector3(width * .96f, .28f, .34f), new Vector3(0, height - .14f, 0), steel);
    }

    private static bool ArtColumnBlocksOpening(float x, List<Rect2> openings) => openings.Exists(h => x + .25f > h.Position.X && x - .25f < h.End.X);

    private static void ArtSupports(Node3D rear, Node3D side, int arrangement,
        float width, float depth, float height, Material steel, Material masonry,
        List<Rect2> rearOpenings, List<Rect2> sideOpenings)
    {
        if (arrangement == 1)
        {
            foreach (var columnX in new[] { -width * .345f, width * .345f })
            {
                if (!ArtColumnBlocksOpening(columnX, rearOpenings))
                {
                    Box(rear, "RearFrameColumn", new Vector3(.24f, height, .30f), new Vector3(columnX, height / 2, 0), steel);
                }
            }
            Box(rear, "RearFrameHeader", new Vector3(width * .72f, .28f, .34f), new Vector3(0, height - .14f, 0), steel);
        }
        else if (arrangement == 2)
        {
            foreach (var columnX in new[] { -depth * .25f, depth * .25f })
            {
                if (!ArtColumnBlocksOpening(columnX, sideOpenings))
                {
                    Box(side, "SideCanopyColumn", new Vector3(.18f, height, .24f), new Vector3(columnX, height / 2, 0), steel);
                }
            }
        }
        else if (arrangement == 3)
        {
            if (!ArtColumnBlocksOpening(-width * .42f, rearOpenings))
            {
                Box(rear, "RearMasonryPilaster", new Vector3(.42f, height, .40f), new Vector3(-width * .42f, height / 2, 0), masonry);
            }
            if (!ArtColumnBlocksOpening(depth * .25f, sideOpenings))
            {
                Box(side, "SideMasonryPilaster", new Vector3(.42f, height, .40f), new Vector3(depth * .25f, height / 2, 0), masonry);
            }
        }
        else if (arrangement == 4)
        {
            foreach (var columnX in new[] { -width * .36f, width * .06f, width * .36f })
            {
                if (!ArtColumnBlocksOpening(columnX, rearOpenings))
                {
                    Box(rear, "StructuralBayColumn", new Vector3(.18f, height, .28f), new Vector3(columnX, height / 2, 0), steel);
                }
            }
        }
    }
}









