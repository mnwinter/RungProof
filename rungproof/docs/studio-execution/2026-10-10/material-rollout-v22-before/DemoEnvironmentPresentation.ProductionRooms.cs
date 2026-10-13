using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // These three production settings are based on authored task geometry. They
    // remain sibling presentation, with no PLC/controller/collision ownership.
    private bool TryBuildProductionRoom(SceneEnvironmentArtDirection d, Aabb processBounds,
        int sx, int sz, Node3D architecture)
    {
        var diagnostic = d.LayoutVariant == "diagnostics-offset-door";
        var packaging = d.LayoutVariant == "bakery-clean-transfer-bays";
        var cable = d.LayoutVariant == "cable-long-side-light";
        if (!diagnostic && !packaging && !cable) return false;

        var reserve = processBounds;
        if (OperatorAccessBounds is Aabb access)
            reserve = reserve.Merge(new Aabb(access.Position - Position, access.Size));
        var width = MathF.Max(diagnostic ? 8.0f : 9.0f, reserve.Size.X + 4.2f);
        var depth = MathF.Max(6.0f, reserve.Size.Z + 4.2f);
        var center = reserve.GetCenter();
        var rearZ = center.Z + sz * depth / 2;
        var sideX = center.X + sx * width / 2;
        var height = diagnostic ? 3.1f : packaging ? 3.4f : 3.6f;
        Func<string,string,StandardMaterial3D> Surface = packaging ? StudioSceneMaterials.CreateDetailed : StudioSceneMaterials.Create;
        var wall = Surface(diagnostic ? "#DDD8C8" : packaging ? "#D4DCCF" : "#AE785D", cable ? "concrete" : "paint");
        var structure = Surface(diagnostic ? "#9D896A" : packaging ? "#5E7F76" : "#8D9894", "paint");
        var floor = Surface(diagnostic ? "#A69D8B" : packaging ? "#A8B2AA" : "#A5947B", "concrete");
        var pale = Surface(diagnostic ? "#E3DED0" : packaging ? "#C2CCC7" : "#D3C6AB", packaging ? "metal" : "paint");
        var wood = StudioSceneMaterials.Create("#B29165", "wood");
        var name = diagnostic ? "DiagnosticServiceRoom" : packaging ? "CleanPackagingHall" : "CablePreparationWorkshop";
        architecture.SetMeta("productionPurpose", name);
        architecture.SetMeta("resolvedRoomLocalBounds", new Aabb(new Vector3(center.X - width / 2, 0, center.Z - depth / 2), new Vector3(width, height, depth)));

        // Avoid a coincident duplicate of the old generic slab: this finished
        // location owns its large-area floor and outward annex foundation.
        if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } oldFloor) oldFloor.Visible = false;
        Box(architecture, name + "ContinuousFloor", new Vector3(width + 4.0f, .12f, depth + 4.0f),
            new Vector3(center.X, -.065f, center.Z), floor);
        Node3D Face(string faceName, Vector3 at, float yaw)
        {
            var n = new Node3D { Name = faceName, Position = at, RotationDegrees = new Vector3(0, yaw, 0) };
            architecture.AddChild(n); return n;
        }
        var rear = Face(name + "Rear", new Vector3(center.X, 0, rearZ), 0);
        var side = Face(name + "Side", new Vector3(sideX, 0, center.Z), 90);
        // Camera-near return ends in the rear corner; two foreground edges stay
        // cut away rather than burying the machine behind a tall near wall.
        var returnLength = MathF.Min(2.0f, depth * .25f);
        var nearReturn = Face(name + "CutawayReturn", new Vector3(center.X - sx * width / 2, 0, rearZ - sz * returnLength / 2), 90);
        var entry = new Rect2(sz * (depth / 2 - 1.4f) - .6f, 0, 1.2f, 2.2f);
        var rearHoles = new List<Rect2>();
        var sideHoles = new List<Rect2> { entry };
        var annexWidth = diagnostic ? 1.8f : packaging ? 2.8f : 2.4f;
        var annexDepth = diagnostic ? .8f : packaging ? 1.2f : 1.4f;
        var mouthWidth = packaging ? 2.4f : 1.4f;
        var annexOffset = packaging ? -width * .22f : -sz * depth * .20f;
        var mouth = new Rect2(annexOffset - mouthWidth / 2, 0, mouthWidth, packaging ? 2.45f : 2.2f);
        if (packaging)
        {
            rearHoles.Add(mouth);
            rearHoles.Add(new Rect2(width * .18f - 1.5f, 2.30f, 3.0f, .55f));
        }
        else
        {
            sideHoles.Add(mouth);
            rearHoles.Add(new Rect2(diagnostic ? -1.6f : -width * .22f - 1.3f, diagnostic ? 1.35f : 2.35f,
                diagnostic ? 3.2f : 2.6f, diagnostic ? 1.0f : .55f));
        }
        ArtWall(rear, width, height, .18f, wall, rearHoles, true);
        ArtWall(side, depth, height, .18f, wall, sideHoles, false);
        ArtWall(nearReturn, returnLength, height, .18f, wall, new(), false);
        Box(rear, name + "RearWallCap", new Vector3(width + .18f, .20f, .30f), new Vector3(0, height - .10f, 0), structure);
        Box(side, name + "SideWallCap", new Vector3(depth + .18f, .20f, .30f), new Vector3(0, height - .10f, 0), structure);
        Box(nearReturn, name + "CutawayCap", new Vector3(returnLength + .18f, .20f, .30f), new Vector3(0, height - .10f, 0), structure);

        // Annex shares its mouth with the wall hole; it does not add a second
        // coplanar front wall or a fake dark rectangle over an intact wall.
        var edge = packaging ? rear : side;
        var outward = packaging ? sz : sx; // side yaw+90 maps local+Z to world+X.
        var annex = new Node3D { Name = name + "ConnectedServiceAnnex", Position = new Vector3(annexOffset, 0, 0) };
        edge.AddChild(annex);
        annex.SetMeta("resolvedAnnexLocalBounds", new Aabb(new Vector3(-annexWidth / 2, 0, MathF.Min(0, outward * annexDepth)), new Vector3(annexWidth, height, annexDepth)));
        foreach (var sign in new[] { -1, 1 })
            Box(annex, "AnnexConnectedReturn", new Vector3(.18f, height, annexDepth), new Vector3(sign * annexWidth / 2, height / 2, outward * annexDepth / 2), wall);
        Box(annex, "AnnexRearWall", new Vector3(annexWidth + .18f, height, .18f), new Vector3(0, height / 2, outward * annexDepth), wall);
        Box(annex, "AnnexSupportedRoof", new Vector3(annexWidth + .20f, .16f, annexDepth + .20f), new Vector3(0, height, outward * annexDepth / 2), structure);
        // The parent wall already supplies the mouth jambs and lintel;
        // duplicating those faces here would cause coplanar flicker.

        if (diagnostic)
        {
            PassiveClosedCabinet(annex, "ClosedCommunicationsServiceBox", new Vector3(.55f, .75f, .20f),
                new Vector3(.28f, 1.3f, outward * (annexDepth - .20f)), outward, structure, pale, "COMMS\nSERVICE");
            Box(rear, "PassiveDiagnosticWoodBackboard", new Vector3(1.6f, .8f, .07f), new Vector3(width * .29f, 1.65f, -sz * .14f), wood);
            ProductionNotice(rear, "DIAGNOSTIC SERVICE", new Vector3(width * .29f, 1.65f, -sz * .20f), sz, 28, .003f);
        }
        else if (packaging)
        {
            Box(annex, "ClosedBlankPackagingSupplyCupboard", new Vector3(.95f, 1.65f, .35f), new Vector3(.52f, .825f, outward * (annexDepth - .25f)), pale);
            Box(annex, "SupplyCupboardPlainFront", new Vector3(.83f, 1.5f, .025f), new Vector3(.52f, .85f, outward * (annexDepth - .445f)), structure);
            Box(rear, "PackagingMaterialServiceNotice", new Vector3(2.3f, .50f, .06f), new Vector3(annexOffset, 2.90f, -sz * .15f), pale);
            ProductionNotice(rear, "PACKAGING MATERIAL SERVICE", new Vector3(annexOffset, 2.90f, -sz * .20f), sz, 28, .003f);
        }
        else
        {
            Box(annex, "PreparationWoodReferenceBoard", new Vector3(1.8f, .8f, .07f), new Vector3(0, 1.5f, outward * (annexDepth - .14f)), wood);
            ProductionNotice(annex, "PAYOFF PREPARATION", new Vector3(0, 1.5f, outward * (annexDepth - .20f)), outward, 30, .003f);
            Box(rear, "ReceiverSideClosedGuideCase", new Vector3(.70f, .70f, .25f), new Vector3(width * .28f, 1.0f, -sz * .25f), structure);
            // A single wall raceway is infrastructure, never an added length of
            // cable across the authored payoff/cutter/receiver travel path.
            Box(rear, "PreparationServiceRaceway", new Vector3(width * .75f, .14f, .12f), new Vector3(0, .55f, -sz * .14f), pale);
        }
        var fixtureFace = (StandardMaterial3D)pale.Duplicate();
        fixtureFace.EmissionEnabled = true; fixtureFace.Emission = new Color("E4E3D9").SrgbToLinear(); fixtureFace.EmissionEnergyMultiplier = .35f;
        foreach (var x in new[] { -width * .28f, width * .27f })
        {
            Box(rear, "GroundedWallFixtureBracket", new Vector3(.14f, .25f, .28f), new Vector3(x, height - .27f, -sz * .13f), structure);
            Box(rear, "ProductionDiffuserFixture", new Vector3(1.1f, .08f, .30f), new Vector3(x, height - .43f, -sz * .20f), fixtureFace);
            rear.AddChild(new OmniLight3D
            {
                Name = "PassiveProductionRoomLight", Position = new Vector3(x, height - .55f, -sz * .30f),
                LightColor = new Color("E4E3D9").SrgbToLinear(), LightEnergy = .25f, OmniRange = 9.0f, ShadowEnabled = true,
            });
        }
        return true;
    }

    // Closed contextual cabinetry only. All door details fit inside the old
    // cuboid envelope; neither a collider nor an interactive control is added.
    private static void PassiveClosedCabinet(Node3D parent, string name, Vector3 size, Vector3 center,
        int outward, Material shell, Material door, string caption)
    {
        var dark = Surface(new Color("28332F"), .25f, .65f);
        Box(parent, name, new Vector3(size.X, size.Y, size.Z - .025f),
            center + new Vector3(0, 0, outward * .0125f), shell);
        var details = new Node3D { Name = name + "PassiveDoorDetails", Position = center };
        parent.AddChild(details);
        details.SetMeta("retainedLocalEnvelope", new Aabb(-size / 2, size));
        var front = -outward * (size.Z / 2 - .018f);
        Box(details, "InsetDoor", new Vector3(size.X - .035f, size.Y - .045f, .016f), new Vector3(0, 0, front), door);
        // Visible seam, flush hinges and a recessed pull distinguish a door
        // from a solid block without enlarging the existing access footprint.
        foreach (var y in new[] { -size.Y * .30f, size.Y * .30f })
            Box(details, "FlushHinge", new Vector3(.025f, .055f, .008f),
                new Vector3(-size.X / 2 + .022f, y, -outward * (size.Z / 2 - .006f)), dark);
        Box(details, "RecessedPull", new Vector3(.035f, .12f, .008f),
            new Vector3(size.X / 2 - .065f, 0, -outward * (size.Z / 2 - .006f)), dark);
        details.AddChild(new Label3D { Name = "PassiveCabinetIdentity", Text = caption,
            Position = new Vector3(0, size.Y * .24f, -outward * (size.Z / 2 - .0095f)),
            RotationDegrees = new Vector3(0, outward > 0 ? 180 : 0, 0),
            FontSize = 32, PixelSize = .0008f, OutlineSize = 0, Modulate = new Color("28332F") });
    }

    private static void ProductionNotice(Node3D parent, string text, Vector3 position, int rearSign, int fontSize, float pixelSize)
    {
        parent.AddChild(new Label3D
        {
            Name = "PassiveProductionPurposeLabel", Text = text, Position = position, FontSize = fontSize, PixelSize = pixelSize,
            OutlineSize = 0, Modulate = new Color("41514E").SrgbToLinear(), RotationDegrees = new Vector3(0, rearSign > 0 ? 180 : 0, 0),
        });
    }
}



