using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private static Aabb DeliveredEquipmentBounds(Node3D node)
    {
        var meshes = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        if (meshes.Length == 0) throw new InvalidOperationException($"No delivered meshes for {node.Name}.");
        var bounds = meshes[0].GlobalTransform * meshes[0].GetAabb();
        foreach (var mesh in meshes.Skip(1)) bounds = bounds.Merge(mesh.GlobalTransform * mesh.GetAabb());
        return bounds;
    }

}
