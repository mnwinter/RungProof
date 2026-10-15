using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local installation geometry only. Keep the reusable tank, valve,
    // pump and instrumented spool unchanged in other lessons. These mounting
    // datums do not specify pressure ratings, pump sizing or pipe stress.
    private static void ConfigureSumpPiping(Node3D scene)
    {
        var tank = scene.GetNode<Node3D>("sump_tank");
        var pump = scene.GetNode<Node3D>("sump_pump");
        var valve = scene.GetNode<Node3D>("discharge_isolation");
        var spool = scene.GetNode<Node3D>("discharge_pipe");
        MeshInstance3D Part(Node3D equipment, string name) => equipment.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Sump installation is missing '{equipment.Name}/{name}'.");
        // Compose runs before entering the tree. Accumulate authored transforms
        // explicitly rather than reading invalid not-yet-ready GlobalTransform.
        Transform3D InScene(Node3D node)
        {
            var transform = node.Transform;
            for (var parent = node.GetParent() as Node3D; parent is not null && parent != scene; parent = parent.GetParent() as Node3D)
                transform = parent.Transform * transform;
            return transform;
        }
        Aabb Bounds(MeshInstance3D mesh)
        {
            var local = mesh.GetAabb(); var transform = InScene(mesh);
            var minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var maximum = -minimum;
            for (var corner = 0; corner < 8; corner++)
            {
                var point = transform * (local.Position + local.Size * new Vector3(
                    (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1));
                minimum = minimum.Min(point); maximum = maximum.Max(point);
            }
            return new Aabb(minimum, maximum - minimum);
        }
        var tankFlange = Bounds(Part(tank, "NOZZLE_outlet_flange"));
        var suctionFlange = Bounds(Part(pump, "PUMP_suction_flange"));
        var dischargeFlange = Bounds(Part(pump, "PUMP_discharge_flange"));
        var suctionStart = tankFlange.GetCenter() with { X = tankFlange.Position.X };
        var suctionEnd = suctionFlange.GetCenter() with { X = suctionFlange.End.X };
        var dischargeStart = dischargeFlange.GetCenter() with { Y = dischargeFlange.End.Y };
        var liner = Bounds(Part(valve, "PROCESS_bore_liner"));
        var dischargeEnd = liner.GetCenter() with { Z = liner.End.Z };

        var steel = Material(new Color("a5b3bb"), 0.7f, 0.23f);
        // Two 45-degree bends provide the small height offset without folding
        // a 180 mm-radius tube around a tighter 150 mm centerline radius.
        const float suctionRadius = 0.35f;
        const float bendX = -2.38f;
        var suction = new List<Vector3> { suctionStart, new(bendX, suctionStart.Y, 0) };
        for (var step = 1; step <= 12; step++)
        {
            var angle = step * Mathf.Pi / 48.0f;
            suction.Add(new Vector3(bendX - suctionRadius * MathF.Sin(angle),
                suctionStart.Y - suctionRadius + suctionRadius * MathF.Cos(angle), 0));
        }
        var radial45 = suctionRadius / MathF.Sqrt(2);
        var lowerStartY = suctionEnd.Y + suctionRadius - radial45;
        var diagonal = suction[^1].Y - lowerStartY;
        var lowerStart = suction[^1] + new Vector3(-diagonal, -diagonal, 0);
        suction.Add(lowerStart);
        var lowerCenter = lowerStart + new Vector3(-radial45, radial45, 0);
        for (var step = 13; step <= 24; step++)
        {
            var angle = step * Mathf.Pi / 48.0f;
            suction.Add(lowerCenter + new Vector3(suctionRadius * MathF.Cos(angle),
                -suctionRadius * MathF.Sin(angle), 0));
        }
        suction.Add(suctionEnd);
        AddSumpRoute(pump, "SUMP_suction", suction.Select(point => point - pump.Position).ToArray(), 0.18f, steel);

        const float elbowRadius = 0.35f;
        var bendBaseY = dischargeEnd.Y - elbowRadius;
        var discharge = new List<Vector3> { dischargeStart, new(dischargeStart.X, bendBaseY, 0) };
        for (var step = 1; step <= 24; step++)
        {
            var angle = step * Mathf.Pi / 48.0f;
            discharge.Add(new Vector3(dischargeStart.X, bendBaseY + elbowRadius * MathF.Sin(angle),
                -elbowRadius + elbowRadius * MathF.Cos(angle)));
        }
        discharge.Add(dischargeEnd);
        AddSumpRoute(pump, "SUMP_discharge", discharge.Select(point => point - pump.Position).ToArray(), 0.15f, steel);

        // Match the near spool flange face to the valve liner's far end. The
        // source spool is X-oriented; both installed runs are now Z-oriented.
        var nearSpoolFace = Bounds(Part(spool, "FLANGE_-1_72")).End.Z;
        spool.Position += Vector3.Back * (liner.Position.Z - nearSpoolFace);
        GroundSupports(valve, "PIPE_foot", "PIPE_shoe_post", "PIPE_anchor");
        GroundSupports(spool, "SUPPORT_foot", "SUPPORT_post", null);
        var standX = -3.20f - pump.Position.X;
        AddBox(pump, new Vector3(0.48f, 0.08f, 0.48f), new Vector3(standX, 0.04f, 0), steel).Name = "SUMP_suction_support_foot";
        AddBox(pump, new Vector3(0.12f, 0.32f, 0.12f), new Vector3(standX, 0.24f, 0), steel).Name = "SUMP_suction_support_post";
        AddBox(pump, new Vector3(0.32f, 0.05f, 0.28f), new Vector3(standX, 0.395f, 0), steel).Name = "SUMP_suction_support_saddle";

        void GroundSupports(Node3D equipment, string footPrefix, string postPrefix, string? anchorPrefix)
        {
            var meshes = equipment.FindChildren("*", "", true, false).OfType<MeshInstance3D>().ToArray();
            var feet = meshes.Where(mesh => mesh.Name.ToString().StartsWith(footPrefix, StringComparison.Ordinal)).ToArray();
            foreach (var foot in feet)
            {
                var drop = Bounds(foot).Position.Y;
                foot.Position -= Vector3.Up * drop;
                if (anchorPrefix is not null)
                {
                    // Match each ground fastener to its nearest authored shoe.
                    foreach (var anchor in meshes.Where(mesh => mesh.Name.ToString().StartsWith(anchorPrefix, StringComparison.Ordinal)))
                        if (feet.OrderBy(other => new Vector2(other.Position.X - anchor.Position.X,
                            other.Position.Z - anchor.Position.Z).LengthSquared()).First() == foot)
                            anchor.Position -= Vector3.Up * drop;
                }
            }
            foreach (var post in meshes.Where(mesh => mesh.Name.ToString().StartsWith(postPrefix, StringComparison.Ordinal)))
            {
                var bounds = Bounds(post);
                var newBottom = 0.06f; // 10 mm overlap with the grounded shoe.
                var newHeight = bounds.End.Y - newBottom;
                post.Scale = new Vector3(post.Scale.X, post.Scale.Y * newHeight / bounds.Size.Y, post.Scale.Z);
                post.Position -= Vector3.Up * (bounds.Position.Y - newBottom) / 2.0f;
            }
        }
    }

    private static void AddSumpRoute(Node3D parent, string name, IReadOnlyList<Vector3> centers, float outerRadius, Material material,
        float wall = 0.022f)
    {
        const int sides = 48;
        if (wall <= 0 || wall >= outerRadius)
            throw new InvalidOperationException($"Pipe route '{name}' requires a positive wall thinner than its radius.");
        var outer = new Vector3[centers.Count, sides]; var inner = new Vector3[centers.Count, sides];
        var normal = Vector3.Zero;
        for (var ring = 0; ring < centers.Count; ring++)
        {
            var tangent = (centers[Math.Min(ring + 1, centers.Count - 1)] - centers[Math.Max(ring - 1, 0)]).Normalized();
            if (ring == 0)
            {
                var reference = MathF.Abs(tangent.Dot(Vector3.Right)) < 0.9f ? Vector3.Right : Vector3.Up;
                normal = tangent.Cross(reference).Normalized();
            }
            // Transport the same radial frame instead of abruptly switching
            // reference axes mid-bend and twisting adjacent polygon rings.
            normal = (normal - tangent * normal.Dot(tangent)).Normalized();
            var binormal = tangent.Cross(normal).Normalized();
            if (ring > 0 && ring < centers.Count - 1)
            {
                var before = centers[ring] - centers[ring - 1]; var after = centers[ring + 1] - centers[ring];
                var cross = before.Cross(after).Length();
                if (cross > 0.000001f)
                {
                    var radius = before.Length() * after.Length() * (before + after).Length() / (2 * cross);
                    if (radius <= outerRadius)
                        throw new InvalidOperationException($"Sump route '{name}' folds its tube around an undersized bend.");
                }
            }
            for (var side = 0; side < sides; side++)
            {
                var angle = side * Mathf.Tau / sides;
                var radial = normal * MathF.Cos(angle) + binormal * MathF.Sin(angle);
                outer[ring, side] = centers[ring] + radial * outerRadius;
                inner[ring, side] = centers[ring] + radial * (outerRadius - wall);
            }
        }
        using var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            foreach (var vertex in new[] { a, b, c, a, c, d }) surface.AddVertex(vertex);
        }
        for (var ring = 0; ring < centers.Count - 1; ring++)
        for (var side = 0; side < sides; side++)
        {
            var next = (side + 1) % sides;
            Quad(outer[ring, side], outer[ring + 1, side], outer[ring + 1, next], outer[ring, next]);
            Quad(inner[ring, side], inner[ring, next], inner[ring + 1, next], inner[ring + 1, side]);
        }
        surface.GenerateNormals();
        parent.AddChild(new MeshInstance3D { Name = name, Mesh = surface.Commit(), MaterialOverride = material });
        foreach (var ring in new[] { 0, centers.Count - 1 })
        {
            using var cap = new SurfaceTool(); cap.Begin(Mesh.PrimitiveType.Triangles);
            for (var side = 0; side < sides; side++)
            {
                var next = (side + 1) % sides;
                foreach (var vertex in new[] { outer[ring, side], outer[ring, next], inner[ring, next],
                    outer[ring, side], inner[ring, next], inner[ring, side] }) cap.AddVertex(vertex);
            }
            cap.GenerateNormals();
            parent.AddChild(new MeshInstance3D { Name = $"{name}_{(ring == 0 ? "start" : "end")}_ring",
                Mesh = cap.Commit(), MaterialOverride = material });
        }
    }
}
