using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyInclinedPhotoeyeGeometry(Action<bool, string> check)
    {
        AddMigratedScene("scene-2-conveyor-pusher", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var sensor = root.GetNode<Node3D>("scene2_photoeye");
        MeshInstance3D Part(string name) => (MeshInstance3D)sensor.FindChild(name, true, false);
        var from = ReviewBounds(Part("RX_lens")).GetCenter();
        var to = ReviewBounds(Part("TX_lens")).GetCenter();
        var line = (to - from).Normalized();
        var aimed = true; var grounded = true; var connected = true;
        foreach (var side in new[] { "RX", "TX" })
        {
            var lens = ReviewBounds(Part(side + "_lens")).GetCenter();
            var bezel = ReviewBounds(Part(side + "_lens_bezel")).GetCenter();
            aimed &= (lens - bezel).Normalized().Dot(side == "RX" ? line : -line) > 0.999f;
            var foot = ReviewBounds(Part(side + "_foot"));
            var post = ReviewBounds(Part(side + "_post"));
            var bracket = ReviewBounds(Part(side + "_adjust_bracket"));
            grounded &= MathF.Abs(foot.Position.Y) < 0.001f && foot.Grow(0.001f).Intersects(post)
                && post.Intersects(bracket);
            var cable = ReviewBounds(Part(side + "_cable"));
            var connector = ReviewBounds(Part(side + "_m12_connector"));
            connected &= cable.Grow(0.001f).Intersects(connector)
                && cable.Grow(0.01f).Intersects(ReviewBounds(Part(side + "_housing")));
        }
        check(aimed, "scene2_inclined_photoeye_lens_axes_face_each_other");
        check(grounded, "scene2_inclined_photoeye_stands_grounded_and_head_brackets_supported");
        check(connected, "scene2_inclined_photoeye_bent_pigtails_join_heads_and_fixed_m12_connectors");
        var dashes = ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        check(dashes.Length == 5 && dashes.All(dash =>
            (ReviewBounds(dash).GetCenter() - from).Cross(line).Length() < 0.001f),
            "scene2_inclined_beam_dashes_follow_continuous_lens_centreline");

        var carton = root.GetNode<Node3D>("scene2_product");
        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_running"] = true });
        for (var sample = 0; sample < 1000 && runtime.Points["part_at_pusher"] is not true; sample++)
            runtime.AdvanceSimulation(0.002);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_running"] = false, ["pusher_extend"] = true });
        var feedbackAligned = true; var releaseSeen = false; var clearAtEnd = false;
        var otherSolids = root.GetChildren().OfType<Node3D>().Where(node => node != sensor && node != carton).SelectMany(ReviewMeshes).ToArray();
        var unobstructed = true;
        for (var sample = 0; sample < 150; sample++)
        {
            runtime.AdvanceSimulation(0.002);
            var load = ReviewBounds(carton);
            var hit = LineHitsBounds(from, to, load.Grow(0.001f));
            var blocked = runtime.Points["part_at_pusher"] is true;
            // Canonical double arithmetic can release either on the exact
            // 80% step or the next step. At that edge, grazing the measured
            // carton top within 2 mm is a geometric boundary, not a fault.
            var heightAtRear = from.Y + (to.Y - from.Y) * (load.Position.Z - from.Z) / (to.Z - from.Z);
            var grazing = MathF.Abs(heightAtRear - load.End.Y) <= 0.002f;
            feedbackAligned &= hit == blocked || grazing;
            if (!blocked)
            {
                releaseSeen = true;
                foreach (var mesh in otherSolids)
                {
                    if (!LineHitsBounds(from, to, ReviewBounds(mesh).Grow(-0.002f))) continue;
                    // A curved cable's enclosing box contains mostly air.
                    // Test its transformed surface before calling it a blocker.
                    if (mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
                        && !LineHitsCableSurface(from, to, mesh)) continue;
                    if (unobstructed) GD.Print($"INCLINED_PHOTOEYE_BLOCKER {mesh.Name}");
                    unobstructed = false;
                }
            }
            if (sample == 149) clearAtEnd = !hit;
        }
        check(feedbackAligned && releaseSeen && clearAtEnd, "scene2_inclined_ray_matches_transfer_feedback_with_two_mm_grazing_boundary");
        check(unobstructed, "scene2_clear_transfer_ray_not_blocked_by_other_equipment");
        GD.Print($"INCLINED_PHOTOEYE_LENSES rx={from} tx={to}");
        runtime.ResetSimulation();
    }

    // Test the continuous optical path between lens centres, including the
    // unpainted gaps between the five visual beam dashes. Slab clipping also
    // handles inclined rays whose enclosing box crosses empty space.
    private static bool LineHitsBounds(Vector3 from, Vector3 to, Aabb bounds)
    {
        var enter = 0f; var leave = 1f; var direction = to - from;
        for (var axis = 0; axis < 3; axis++)
        {
            if (MathF.Abs(direction[axis]) < 1e-8f)
            {
                if (from[axis] < bounds.Position[axis] || from[axis] > bounds.End[axis]) return false;
                continue;
            }
            var near = (bounds.Position[axis] - from[axis]) / direction[axis];
            var far = (bounds.End[axis] - from[axis]) / direction[axis];
            enter = MathF.Max(enter, MathF.Min(near, far));
            leave = MathF.Min(leave, MathF.Max(near, far));
            if (enter > leave) return false;
        }
        return true;
    }

    private static bool LineHitsCableSurface(Vector3 from, Vector3 to, MeshInstance3D cable)
    {
        // Segment/triangle intersection (double-sided). The optical centreline
        // is symbolic; this does not model a vendor's finite beam diameter.
        var inverse = cable.GlobalTransform.AffineInverse();
        var origin = inverse * from; var direction = inverse * to - origin;
        var faces = cable.Mesh.GetFaces();
        for (var index = 0; index < faces.Length; index += 3)
        {
            var edge1 = faces[index + 1] - faces[index];
            var edge2 = faces[index + 2] - faces[index];
            var cross = direction.Cross(edge2); var determinant = edge1.Dot(cross);
            if (MathF.Abs(determinant) < 1e-9f) continue;
            var offset = origin - faces[index];
            var u = offset.Dot(cross) / determinant;
            if (u < 0 || u > 1) continue;
            var q = offset.Cross(edge1); var v = direction.Dot(q) / determinant;
            if (v < 0 || u + v > 1) continue;
            var distance = edge2.Dot(q) / determinant;
            if (distance >= 0 && distance <= 1) return true;
        }
        return false;
    }
}
