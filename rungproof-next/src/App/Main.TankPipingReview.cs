using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyTankPipingGeometry(Action<bool, string> check, string? onlySceneId = null)
    {
        foreach (var (sceneId, tankId, pumpId, inletId, outletId) in new[]
        {
            ("tank-high-low", "water_tank_hl", "hl_inlet_pump", "hl_inlet_pipe", "hl_outlet_pipe"),
            ("tank-level", "process_tank", "inlet_pump", "inlet_pipe", "outlet_pipe"),
            ("tank-radar", "water_tank_radar", "radar_inlet_pump", "radar_inlet_pipe", "radar_outlet_pipe"),
        })
        {
            if (onlySceneId is not null && sceneId != onlySceneId) continue;
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var tank = root.GetNode<Node3D>(tankId);
            var pump = root.GetNode<Node3D>(pumpId);
            var inlet = root.GetNode<Node3D>(inletId);
            var outlet = root.GetNode<Node3D>(outletId);
            MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
            Vector3? Ring(Node node, string name)
            {
                if (node.FindChild(name, true, false) is not MeshInstance3D mesh) return null;
                var vertices = mesh.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                return mesh.GlobalTransform * (vertices.Aggregate(Vector3.Zero, (sum, vertex) => sum + vertex) / vertices.Length);
            }
            bool Near(Vector3? actual, Vector3 expected) => actual is { } value && value.DistanceTo(expected) < 0.001f;
            Vector3 Face(MeshInstance3D mesh, float sign) => mesh.GlobalTransform *
                (mesh.GetAabb().GetCenter() + Vector3.Up * (sign * mesh.GetAabb().Size.Y / 2));
            // Delivered cylinders use local Y along their bores. Face centres,
            // rather than world AABB extrema, also work for diagonal installs.
            check(Near(Ring(pump, "TANK_FILL_start_ring"), Face(Part(pump, "PUMP_discharge_flange"), 1))
                && Near(Ring(pump, "TANK_FILL_end_ring"), Face(Part(inlet, "FLANGE_-1_72"), -1)),
                $"{sceneId}_pump_upward_discharge_route_meets_inlet_spool_face");
            check(Near(Ring(tank, "TANK_INLET_flange_end_ring"), Face(Part(inlet, "FLANGE_1_72"), 1))
                && Ring(tank, "TANK_INLET_neck_start_ring") is { } start
                && new Vector2(start.X, start.Z).Length() < 1.5f,
                $"{sceneId}_radial_inlet_nozzle_meets_spool_and_enters_shell");
            check(Face(Part(outlet, "FLANGE_-1_72"), -1).DistanceTo(Face(Part(tank, "NOZZLE_outlet_flange"), 1)) < 0.001f,
                $"{sceneId}_outlet_spool_meets_actual_tank_flange");
            var feet = ReviewMeshes(inlet).Concat(ReviewMeshes(outlet)).Where(mesh => mesh.Name.ToString().StartsWith("SUPPORT_foot", StringComparison.Ordinal)).ToArray();
            var posts = ReviewMeshes(inlet).Concat(ReviewMeshes(outlet)).Where(mesh => mesh.Name.ToString().StartsWith("SUPPORT_post", StringComparison.Ordinal)).ToArray();
            check(feet.Length == 4 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f)
                && posts.Length == 4 && posts.All(post => feet.Any(foot => ReviewBounds(post).Intersects(ReviewBounds(foot).Grow(0.001f)))),
                $"{sceneId}_four_pipe_supports_grounded_and_attached");
            bool Penetrates(MeshInstance3D part, MeshInstance3D other)
            {
                if (!ReviewBounds(part).Intersects(ReviewBounds(other)) || !OrientedBoxesPenetrate(part, other)) return false;
                if (other.Name != "TANK_shell") return true;
                // A cylinder's box has empty diagonal corners. Screen the
                // complete projected part box against the actual shell radius,
                // including edge interiors rather than checking vertices only.
                var local = part.GetAabb();
                var points = Enumerable.Range(0, 8).Select(corner =>
                {
                    var p = part.GlobalTransform * (local.Position + local.Size * new Vector3(
                        (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1));
                    return new Vector2(p.X, p.Z);
                }).ToArray();
                var hull = Geometry2D.ConvexHull(points);
                var centre3 = ReviewBounds(other).GetCenter();
                var centre2 = new Vector2(centre3.X, centre3.Z);
                if (Geometry2D.IsPointInPolygon(centre2, hull)) return true;
                var radius = ReviewBounds(other).Size.X / 2;
                return Enumerable.Range(0, hull.Length - 1).Any(index =>
                    Geometry2D.GetClosestPointToSegment(centre2, hull[index], hull[index + 1]).DistanceTo(centre2) < radius + 0.001f);
            }
            var conflicts = ReviewMeshes(inlet).Concat(ReviewMeshes(outlet)).SelectMany(part => ReviewMeshes(tank)
                .Where(mesh => !mesh.Name.ToString().StartsWith("NOZZLE_outlet", StringComparison.Ordinal)
                    && !mesh.Name.ToString().StartsWith("TANK_INLET", StringComparison.Ordinal))
                .Where(other => Penetrates(part, other))
                .Select(other => $"{part.Name}/{other.Name}")).ToArray();
            GD.Print($"TANK_PIPE_CLEARANCE {sceneId} {string.Join(';', conflicts)}");
            check(conflicts.Length == 0,
                $"{sceneId}_installed_spools_clear_ladder_shell_and_instruments");
            check(Near(Ring(pump, "TANK_SUPPLY_start_ring"), Face(Part(pump, "PUMP_suction_flange"), 1))
                && root.FindChild("TANK_supply_boundary", true, false) is Label3D
                && root.FindChild("TANK_drain_boundary", true, false) is Label3D,
                $"{sceneId}_pump_suction_connected_and_external_boundaries_labeled");
            var supply = pump.FindChild("TANK_SUPPLY", true, false) as MeshInstance3D;
            bool ClearSupply(MeshInstance3D post)
            {
                if (supply is null) return false;
                // Check actual route faces in the post's local box, avoiding
                // the empty interior of a bent route's large bounding box.
                var transform = post.GlobalTransform.AffineInverse() * supply.GlobalTransform;
                var faces = supply.Mesh.GetFaces();
                for (var index = 0; index < faces.Length; index += 3)
                {
                    var triangle = new Aabb(transform * faces[index], Vector3.Zero)
                        .Expand(transform * faces[index + 1]).Expand(transform * faces[index + 2]).Grow(0.001f);
                    if (triangle.Intersects(post.GetAabb())) return false;
                }
                return true;
            }
            check(posts.All(ClearSupply), $"{sceneId}_suction_route_clear_of_all_spool_support_posts");
            var supplyFoot = root.FindChild("TANK_supply_foot", true, false) as MeshInstance3D;
            var supplyPost = root.FindChild("TANK_supply_post", true, false) as MeshInstance3D;
            var supplySaddle = root.FindChild("TANK_supply_saddle", true, false) as MeshInstance3D;
            check(supplyFoot is not null && supplyPost is not null && supplySaddle is not null
                && MathF.Abs(ReviewBounds(supplyFoot).Position.Y) < 0.001f
                && ReviewBounds(supplyPost).Intersects(ReviewBounds(supplyFoot))
                && ReviewBounds(supplyPost).Intersects(ReviewBounds(supplySaddle))
                && ClearSupply(supplyPost)
                && Ring(pump, "TANK_SUPPLY_end_ring") is { } supplyEnd
                && MathF.Abs(ReviewBounds(supplySaddle).End.Y - (supplyEnd.Y - 0.18f)) < 0.001f,
                $"{sceneId}_supply_shoe_grounded_and_saddle_supported");
            GD.Print($"TANK_PIPING_DATUM {sceneId} pump={pump.Position} inlet={inlet.Position} outlet={outlet.Position}");
            VerifyTankDrainValve(sceneId, root, outlet, check);
        }
    }
}
