using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static void ConfigureInclinedPhotoeye(Node3D model, float negativeHeight, float positiveHeight,
        float negativeStand, float positiveStand)
    {
        var slope = (positiveHeight - negativeHeight) / (positiveStand - negativeStand);
        var angle = -MathF.Atan(slope);
        var aim = new Basis(Vector3.Right, angle);
        var items = model.FindChildren("*", string.Empty, true, false).OfType<Node3D>().ToArray();
        foreach (var item in items)
        {
            var name = item.Name.ToString();
            if (name.StartsWith("KIN_beam", StringComparison.Ordinal))
            {
                item.Position = new Vector3(item.Position.X,
                    negativeHeight + slope * (item.Position.Z - negativeStand), item.Position.Z);
                item.Basis = aim * Basis.FromScale(new Vector3(1, 1, MathF.Sqrt(1 + slope * slope))) * item.Basis;
                continue;
            }
            var positive = name.StartsWith("TX_", StringComparison.Ordinal);
            if (!positive && !name.StartsWith("RX_", StringComparison.Ordinal)) continue;
            var extraHeight = positive ? positiveHeight - negativeHeight : 0;
            var pivot = new Vector3(0, positive ? positiveHeight : negativeHeight, positive ? positiveStand : negativeStand);
            if (name.EndsWith("_post", StringComparison.Ordinal))
            {
                item.Position += Vector3.Up * (extraHeight / 2);
                item.Scale *= new Vector3(1, (negativeHeight - 0.02f + extraHeight) / (negativeHeight - 0.02f), 1);
            }
            else if (name.EndsWith("_cable", StringComparison.Ordinal) && item is MeshInstance3D cable)
            {
                // Raise the head end while preserving the lower M12 endpoint,
                // then bend the upper tail with the aimed head. Clone the mesh;
                // other scene installations must retain their imported cable.
                var bounds = cable.Transform * cable.GetAabb();
                var stretch = Basis.FromScale(new Vector3(1, (bounds.Size.Y + extraHeight) / bounds.Size.Y, 1));
                cable.Transform = new Transform3D(stretch * cable.Basis,
                    stretch * cable.Position + Vector3.Up * bounds.Position.Y * (1 - (bounds.Size.Y + extraHeight) / bounds.Size.Y));
                BendPhotoeyeCable(cable, pivot, angle);
            }
            else if (!name.EndsWith("_foot", StringComparison.Ordinal)
                && !name.Contains("_m12_", StringComparison.Ordinal))
            {
                item.Position += Vector3.Up * extraHeight;
                item.Transform = new Transform3D(aim * item.Basis, pivot + aim * (item.Position - pivot));
            }
        }
    }

    private static void BendPhotoeyeCable(MeshInstance3D cable, Vector3 pivot, float angle)
    {
        var source = cable.Mesh;
        var bounds = cable.Transform * source.GetAabb();
        var inverse = cable.Transform.AffineInverse();
        var mesh = new ArrayMesh();
        for (var surface = 0; surface < source.GetSurfaceCount(); surface++)
        {
            var arrays = source.SurfaceGetArrays(surface);
            var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
            for (var index = 0; index < vertices.Length; index++)
            {
                var point = cable.Transform * vertices[index];
                var weight = Mathf.Clamp((point.Y - bounds.Position.Y) / bounds.Size.Y, 0, 1);
                var bend = new Basis(Vector3.Right, angle * weight);
                vertices[index] = inverse * (pivot + bend * (point - pivot));
                if (index < normals.Length)
                    normals[index] = (inverse.Basis * bend * cable.Basis * normals[index]).Normalized();
            }
            arrays[(int)Mesh.ArrayType.Vertex] = vertices;
            arrays[(int)Mesh.ArrayType.Normal] = normals;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(surface, source.SurfaceGetMaterial(surface));
        }
        cable.Mesh = mesh;
    }
}
