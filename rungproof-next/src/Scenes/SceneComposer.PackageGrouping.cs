using System;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreatePackageGroupingPart(string installation)
    {
        var part = installation == "groupingLine" ? "line" : "stop";
        return ResourceLoader.Load<PackedScene>($"res://assets/scene_installations/package_grouping/{part}/delivery/grouping_{part}.glb")?.Instantiate<Node3D>()
            ?? throw new InvalidOperationException($"Missing package-grouping installation: {part}.");
    }
}
