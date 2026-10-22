using System;
using System.Linq;
using System.Text.Json;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    /// <summary>
    /// Adds the continuous room wall crossed by an authored material infeed.
    /// The wall must span the visible room rather than read as a freestanding
    /// portal prop. The scene remains responsible for placing its load behind
    /// the boundary and extending the carrying conveyor through it. This is
    /// visual installation geometry only; it does not sequence product.
    /// </summary>
    private static void ConfigureMaterialEntryPortal(Node3D root, SceneDefinition scene)
    {
        if (!scene.Simulation.TryGetProperty("materialEntryPortal", out var definition)
            || definition.ValueKind != JsonValueKind.Object)
            return;

        var axis = Text(definition, "axis", "x").ToLowerInvariant();
        var coordinate = Number(definition, "coordinate", 0);
        var crossCenter = Number(definition, "crossCenter", 0);
        var wallSpan = Number(definition, "wallSpanM", 4.0);
        var wallHeight = Number(definition, "wallHeightM", 3.5);
        var openingWidth = Number(definition, "openingWidthM", 1.95);
        var openingHeight = Number(definition, "openingHeightM", 2.25);
        var thickness = Number(definition, "wallThicknessM", 0.18);
        var sourceRoomDepth = Number(definition, "sourceRoomDepthM", 6.0);
        var sourceDirection = Number(definition, "sourceDirection", -1.0);
        if (axis is not ("x" or "z")
            || new[] { coordinate, crossCenter, wallSpan, wallHeight, openingWidth, openingHeight,
                    thickness, sourceRoomDepth, sourceDirection }
                .Any(value => !double.IsFinite(value))
            || wallSpan <= openingWidth || wallHeight <= openingHeight
            || openingWidth <= 0 || openingHeight <= 0 || thickness <= 0
            || sourceRoomDepth <= 1.0 || Math.Abs(Math.Abs(sourceDirection) - 1.0) > .001)
            throw new InvalidOperationException($"Scene '{scene.Id}' has an invalid material-entry portal.");

        var portal = new Node3D { Name = "MaterialEntryPortal" };
        root.AddChild(portal);
        // This is part of the room envelope, not a freestanding prop. Match the
        // authored wall finish so the entry reads as one continuous building
        // boundary under the same scene lighting.
        var direction = RungProof.Next.App.SceneEnvironmentArtDirections.ForScene(scene.Id);
        var wall = RungProof.Next.App.StudioSceneMaterials.Create(direction.WallHex, direction.WallStyle);
        var wallBase = RungProof.Next.App.StudioSceneMaterials.Create(direction.AccentHex, "paint");
        var frame = Material(new Color("525b5c"), .72f, .28f);
        // Give the connected material-source room its own believable staging-
        // area finish. Reusing the production-room wall color on every surface
        // made the opening disappear visually: the far wall read as an
        // unbroken panel and cartons still appeared to spawn at the conveyor
        // end. The darker finish creates real depth through the aperture while
        // leaving the production-room wall and steel portal frame unchanged.
        var sourceWall = RungProof.Next.App.StudioSceneMaterials.Create("3E4A4D", "paint");
        var sourceBase = RungProof.Next.App.StudioSceneMaterials.Create("263238", "paint");
        var sideSpan = (float)((wallSpan - openingWidth) * .5);
        var sideOffset = (float)((openingWidth + sideSpan) * .5);
        var c = (float)coordinate;
        var cross = (float)crossCenter;
        var wh = (float)wallHeight;
        var ow = (float)openingWidth;
        var oh = (float)openingHeight;
        var t = (float)thickness;
        var roomDepth = (float)sourceRoomDepth;
        var sourceSign = MathF.Sign((float)sourceDirection);
        var roomSpan = (float)wallSpan;

        Vector3 Size(float along, float y, float across) => axis == "x"
            ? new Vector3(along, y, across)
            : new Vector3(across, y, along);
        Vector3 At(float along, float y, float across) => axis == "x"
            ? new Vector3(along, y, across)
            : new Vector3(across, y, along);

        foreach (var sign in new[] { -1, 1 })
        {
            var panel = AddBox(portal, Size(t, wh, sideSpan),
                At(c, wh * .5f, cross + sign * sideOffset), wall);
            panel.Name = sign < 0 ? "EntryWallNegative" : "EntryWallPositive";
            AddBox(portal, Size(t + .025f, .55f, sideSpan),
                At(c, .275f, cross + sign * sideOffset), wallBase).Name =
                sign < 0 ? "EntryWallBaseNegative" : "EntryWallBasePositive";
            var jamb = AddBox(portal, Size(t + .08f, oh, .10f),
                At(c, oh * .5f, cross + sign * ow * .5f), frame);
            jamb.Name = sign < 0 ? "EntryJambNegative" : "EntryJambPositive";
        }
        var headerHeight = wh - oh;
        AddBox(portal, Size(t, headerHeight, ow),
            At(c, oh + headerHeight * .5f, cross), wall).Name = "EntryWallHeader";
        AddBox(portal, Size(t + .08f, .12f, ow + .20f),
            At(c, oh, cross), frame).Name = "EntrySteelHeader";

        // Continue the jambs and header into the source room. A paper-thin
        // opening can disappear at an oblique camera angle and makes the load
        // look as if it begins on the visible end of the conveyor. These
        // reveals form an actual short material tunnel around (not across) the
        // conveyor, so the belt and carton remain unobstructed.
        var revealDepth = MathF.Min(3.0f, roomDepth * .35f);
        var revealCenter = c + sourceSign * revealDepth * .5f;
        foreach (var sign in new[] { -1, 1 })
            AddBox(portal, Size(revealDepth, oh, .12f),
                At(revealCenter, oh * .5f, cross + sign * ow * .5f), sourceWall).Name =
                sign < 0 ? "MaterialTunnelJambNegative" : "MaterialTunnelJambPositive";
        AddBox(portal, Size(revealDepth, .12f, ow),
            At(revealCenter, oh, cross), sourceWall).Name = "MaterialTunnelCeiling";
        AddBox(portal, Size(revealDepth, .06f, ow),
            At(revealCenter, .03f, cross), sourceBase).Name = "MaterialTunnelFloor";

        // The entry wall is the full-height partition of a connected
        // material-source room, not a freestanding movie-set panel. Its span
        // is authored beyond the production-room envelope, while the connected
        // ceiling and returns close the source volume. An operator camera can
        // therefore see the conveyor aperture but never an exposed wall end,
        // wall top, or empty world behind it.
        var roomCenter = c + sourceSign * roomDepth * .5f;
        AddBox(portal, Size(roomDepth + t, .20f, roomSpan),
            At(roomCenter, wh + .10f, cross), sourceWall).Name = "MaterialSourceRoomCeiling";
        AddBox(portal, Size(t, wh, roomSpan),
            At(c + sourceSign * roomDepth, wh * .5f, cross), sourceWall).Name = "MaterialSourceRoomRearWall";
        foreach (var sign in new[] { -1, 1 })
        {
            AddBox(portal, Size(roomDepth, wh, t),
                At(roomCenter, wh * .5f, cross + sign * roomSpan * .5f), sourceWall).Name =
                sign < 0 ? "MaterialSourceRoomReturnNegative" : "MaterialSourceRoomReturnPositive";
            AddBox(portal, Size(roomDepth, .55f, t + .025f),
                At(roomCenter, .275f, cross + sign * roomSpan * .5f), sourceBase).Name =
                sign < 0 ? "MaterialSourceRoomBaseNegative" : "MaterialSourceRoomBasePositive";
        }
        AddBox(portal, Size(t + .025f, .55f, roomSpan),
            At(c + sourceSign * roomDepth, .275f, cross), sourceBase).Name = "MaterialSourceRoomRearBase";

        portal.AddChild(new OmniLight3D
        {
            Name = "MaterialSourceRoomLight",
            Position = At(c + sourceSign * roomDepth * .42f, wh - .55f, cross),
            LightColor = new Color("F2E3C4"),
            LightEnergy = .42f,
            OmniRange = MathF.Min(roomDepth, 7.5f),
            ShadowEnabled = true,
        });
        portal.SetMeta("materialEntryAxis", axis);
        portal.SetMeta("materialEntryCoordinate", coordinate);
        portal.SetMeta("materialSourceRoomDepth", roomDepth);
    }
}
