using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Stationary installation witnesses only. These meshes carry no equipment
    // identity, actions, controllers, KIN members or physical clearance claim.
    private static void ConfigureContextInstallation(Node3D root, SceneDefinition scene)
    {
        string? anchorId = scene.Id switch
        {
            "lab-2-19-service-door" => "service_shutter",
            "lab-2-15-fume-extractor" => "extractor_fan",
            "lab-5-07-pedestrian-crossing" => "training_accessory_5",
            "lab-11-12-mobile-traffic-lights" => "training_accessory_4",
            _ => null,
        };
        if (anchorId is null) return;
        var anchor = root.GetNode<Node3D>(SafeNodeName(anchorId));
        var context = new Node3D { Name = "ProcessContextInstallation", Transform = anchor.Transform };
        context.SetMeta("installationKind", "stationaryProcessContext");
        root.AddChild(context);
        // Palette hex values describe display sRGB, while this renderer uses
        // linear albedo values. Convert once; authored materials below remain
        // shared unchanged so adjoining road/sidewalk surfaces match exactly.
        var plaster = Material(new Color("c8c6bc").SrgbToLinear(), .02f, .85f);
        var concrete = Material(new Color("a6a49c").SrgbToLinear(), .02f, .92f);
        var steel = Material(new Color("888e8c").SrgbToLinear(), .55f, .65f);
        var dark = Material(new Color("535653").SrgbToLinear(), .05f, .90f);

        if (scene.Id == "lab-2-19-service-door")
        {
            // Use actual fixed guides, not the scene config's nominal width.
            // GLB front faces +Z; wings abut behind fixed jambs. Nothing spans
            // the curtain aperture or roll travel, including the open pose.
            var guides = ContextMemberBounds(anchor, "SHUTTER_guide_");
            var header = ContextMemberBounds(anchor, "SHUTTER_header");
            const float wing = 1.65f;
            var height = header.End.Y + .55f;
            foreach (var sign in new[] { -1, 1 })
            {
                var edge = sign < 0 ? guides.Position.X : guides.End.X;
                AddBox(context, new Vector3(wing, height, .30f),
                    new Vector3(edge + sign * wing / 2, height / 2, -.20f), plaster).Name = "BuildingWallWing";
                AddBox(context, new Vector3(.18f, height, 1.20f),
                    new Vector3(edge + sign * .09f, height / 2, -.85f), plaster).Name = "DoorRevealReturn";
            }
            var opening = guides.Size.X;
            AddBox(context, new Vector3(opening, .42f, 1.20f),
                new Vector3(guides.GetCenter().X, header.End.Y + .24f, -.85f), plaster).Name = "DoorLintelSoffit";
            AddBox(context, new Vector3(opening, .06f, 1.45f),
                new Vector3(guides.GetCenter().X, -.035f, -.75f), concrete).Name = "LoadingThresholdContinuation";
        }
        else if (scene.Id == "lab-2-15-fume-extractor")
        {
            // Rear plenum seats on the fixed shroud. Thin sides leave the fan
            // bore open, preserving the visible propeller and rotating hub.
            var shroud = ContextMemberBounds(anchor, "FAN_shroud");
            var center = shroud.GetCenter();
            var w = shroud.Size.X;
            var h = shroud.Size.Y;
            var start = shroud.Position.Z;
            const float run = 1.25f;
            var z = start - run / 2;
            foreach (var sign in new[] { -1, 1 })
            {
                AddBox(context, new Vector3(.045f, h, run),
                    new Vector3(center.X + sign * (w / 2 - .0225f), center.Y, z), steel).Name = "ExtractionPlenumSide";
                AddBox(context, new Vector3(w, .045f, run),
                    new Vector3(center.X, center.Y + sign * (h / 2 - .0225f), z), steel).Name = "ExtractionPlenumTopBottom";
            }
            // A closed rear riser is beyond the motor; the transition is a
            // rectangular duct enclosure rather than an invented second fan.
            var rear = start - run - .30f;
            AddBox(context, new Vector3(w, 2.25f, .60f),
                new Vector3(center.X, center.Y + .45f, rear), steel).Name = "ExtractionVerticalDuct";
            AddBox(context, new Vector3(w + .14f, .08f, .72f),
                new Vector3(center.X, center.Y + 1.615f, rear), dark).Name = "ExtractionDuctTerminal";
            foreach (var sign in new[] { -1, 1 })
                AddBox(context, new Vector3(.09f, MathF.Max(.1f, center.Y - h / 2), .09f),
                    new Vector3(center.X + sign * (w / 2 - .12f), (center.Y - h / 2) / 2, rear), steel).Name = "DuctSupportLeg";
        }
        else
        {
            var road = ContextMemberBounds(anchor, "ROAD_horizontal_surface");
            var roadMesh = anchor.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                .First(m => m.Name.ToString().StartsWith("ROAD_horizontal_surface", StringComparison.Ordinal));
            var roadMaterial = roadMesh.MaterialOverride ?? roadMesh.GetActiveMaterial(0) ?? dark;
            var walks = anchor.FindChildren("SIDEWALK_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
            // Extend only the authored road axis and existing sidewalk bands.
            // No duplicate crossing stripes, stop lines, signals or controls.
            foreach (var sign in new[] { -1, 1 })
            {
                const float length = 5.0f;
                var edge = sign < 0 ? road.Position.X : road.End.X;
                AddBox(context, new Vector3(length, road.Size.Y, road.Size.Z),
                    new Vector3(edge + sign * length / 2, road.GetCenter().Y, road.GetCenter().Z), roadMaterial).Name = "StreetApproachContinuation";
                foreach (var walk in walks)
                {
                    var b = ContextMeshBounds(anchor, walk);
                    AddBox(context, new Vector3(length, b.Size.Y, b.Size.Z),
                        new Vector3(edge + sign * length / 2, b.GetCenter().Y, b.GetCenter().Z),
                        walk.MaterialOverride ?? walk.GetActiveMaterial(0) ?? concrete).Name = "SidewalkContinuation";
                }
            }
        }
        GD.Print($"CONTEXT_INSTALLATION_READY scene={scene.Id} anchor={anchorId}");
    }

    private static Aabb ContextMemberBounds(Node3D anchor, string prefix)
    {
        Aabb? result = null;
        foreach (var mesh in anchor.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            if (mesh.Mesh is null || !mesh.Name.ToString().StartsWith(prefix, StringComparison.Ordinal)) continue;
            var b = ContextMeshBounds(anchor, mesh);
            result = result is { } previous ? previous.Merge(b) : b;
        }
        return result ?? throw new InvalidOperationException($"Context anchor missing {prefix} in {anchor.Name}");
    }

    private static Aabb ContextMeshBounds(Node3D anchor, MeshInstance3D mesh)
    {
        var transform = mesh.Transform;
        for (var parent = mesh.GetParent(); parent != anchor; parent = parent.GetParent())
            if (parent is Node3D spatial) transform = spatial.Transform * transform;
        return transform * mesh.GetAabb();
    }
}
