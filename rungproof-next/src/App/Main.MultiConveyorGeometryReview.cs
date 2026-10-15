using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyMultiConveyorBearing(Action<bool, string> check, Node3D root, Node3D pallet)
    {
        // A geometric bearing audit: carrying planes and roller top contacts,
        // not a load-capacity or deflection calculation. Curved belt wraps do
        // not count as flat supports. Roller contacts are lines, not boxes.
        var supports = new List<(double Left, double Right, double Near, double Far, double Y)>();
        for (var zone = 0; zone < 3; zone++)
        {
            var conveyor = root.GetNode<Node3D>($"conveyor_{zone}");
            var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
            var tail = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_tail_drum", true, false)).GetCenter().X;
            var drive = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_drive_drum", true, false)).GetCenter().X;
            supports.Add((tail, drive, belt.Position.Z, belt.End.Z, belt.End.Y));
            var deck = ReviewBounds((MeshInstance3D)root.GetNode<Node3D>("zone_handoffs").FindChild($"ZONE_{zone}_transfer_deck", true, false));
            supports.Add((deck.Position.X, deck.End.X, deck.Position.Z, deck.End.Z, deck.End.Y));
        }
        var rollers = ReviewMeshes(root.GetNode<Node3D>("training_accessory_6"))
            .Where(mesh => mesh.Name.ToString().StartsWith("KIN_roller_", StringComparison.Ordinal)).ToArray();
        GD.Print($"MULTI_CONVEYOR_ROLLER_AXIS local_bounds={rollers[0].GetAabb()} basis={rollers[0].GlobalBasis}");
        var rollerDrive = root.GetNode<Node3D>("training_accessory_6").FindChildren("*", "", true, false)
            .OfType<RollerConveyorController>().Single();
        var rollerHomes = rollers.ToDictionary(mesh => mesh, mesh => mesh.Transform);
        var rollerFaces = rollers.ToDictionary(mesh => mesh, mesh => mesh.Mesh.GetFaces());
        Aabb TightRollerBounds(MeshInstance3D mesh)
        {
            // Rotating the local bounding box expands its empty square corners.
            // Use delivered vertices to measure the rotating cylinder itself.
            var faces = rollerFaces[mesh];
            var bounds = new Aabb(mesh.GlobalTransform * faces[0], Vector3.Zero);
            foreach (var vertex in faces) bounds = bounds.Expand(mesh.GlobalTransform * vertex);
            return bounds;
        }
        var rollerBounds = rollers.ToDictionary(mesh => mesh, TightRollerBounds);
        var rollerAxes = rollers.ToDictionary(mesh => mesh, mesh => mesh.GlobalBasis.Y.Normalized());
        var phasesClear = true;
        var rotates = false;
        try
        {
            rollerDrive.RunCommand = true;
            // Exercise the delivered controller for a full rotation. This is
            // deterministic adapter evidence, separate from native imagery.
            for (var phase = 0; phase < 360; phase++)
            {
                rollerDrive._PhysicsProcess(Math.PI / 180 * .038 / .75);
                foreach (var roller in rollers)
                {
                    var bounds = TightRollerBounds(roller); var baseline = rollerBounds[roller];
                    phasesClear &= roller.GlobalBasis.Y.Normalized().Dot(rollerAxes[roller]) > .99999f
                        && MathF.Abs(bounds.End.Y - baseline.End.Y) < .001f
                        && bounds.GetCenter().DistanceTo(baseline.GetCenter()) < .00001f
                        && MathF.Abs(bounds.Size.Z - baseline.Size.Z) < .00001f;
                }
                if (phase == 89) rotates = !rollers[0].Basis.IsEqualApprox(rollerHomes[rollers[0]].Basis);
            }
            rollerDrive.RunCommand = false; rollerDrive._PhysicsProcess(.02);
            check(rotates && !rollerDrive.Running, "multi_conveyor_receiving_adapter_rotates_and_stops");
            check(phasesClear, "multi_conveyor_all_roller_phase_axes_and_carrying_envelopes_stable");
        }
        finally
        {
            rollerDrive.RunCommand = false;
            foreach (var (roller, transform) in rollerHomes) roller.Transform = transform;
        }
        foreach (var roller in rollers)
        {
            var bounds = ReviewBounds(roller);
            var x = bounds.GetCenter().X;
            supports.Add((x, x, bounds.Position.Z, bounds.End.Z, bounds.End.Y));
        }
        var runners = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_BOTTOM_", StringComparison.Ordinal)).ToArray();
        check(rollers.Length == 31 && runners.Length == 3, "multi_conveyor_bearing_uses_31_rollers_and_three_runners");
        var home = pallet.Position;
        var supported = true;
        var worstGap = 0d;
        var minimumReceiverContacts = int.MaxValue;
        var samples = 0;
        try
        {
            // Sweep the authored straight route including both endpoints.
            // Five-millimetre spacing is sampled geometry, not continuous proof.
            var count = (int)Math.Ceiling((12.3 - home.X) / .005);
            for (var sample = 0; sample <= count; sample++)
            {
                pallet.Position = new Vector3((float)(home.X + (12.3 - home.X) * sample / count), home.Y, home.Z);
                foreach (var runner in runners)
                {
                    var bounds = ReviewBounds(runner);
                    var contacts = supports.Where(surface => Math.Abs(surface.Y - bounds.Position.Y) < .001
                        && surface.Near <= bounds.Position.Z + .0001 && surface.Far >= bounds.End.Z - .0001
                        && surface.Right >= bounds.Position.X && surface.Left <= bounds.End.X)
                        .Select(surface => (Left: Math.Max(surface.Left, bounds.Position.X), Right: Math.Min(surface.Right, bounds.End.X)))
                        .OrderBy(surface => surface.Left).ToArray();
                    if (contacts.Length == 0) { supported = false; continue; }
                    // Contact hull must bracket the runner center. This rejects
                    // hanging the load entirely off the last bearing surface.
                    var center = bounds.GetCenter().X;
                    supported &= contacts[0].Left <= center && contacts.Max(contact => contact.Right) >= center;
                    var coveredTo = (double)bounds.Position.X;
                    foreach (var contact in contacts)
                    {
                        worstGap = Math.Max(worstGap, Math.Max(0, contact.Left - coveredTo));
                        coveredTo = Math.Max(coveredTo, contact.Right);
                    }
                    worstGap = Math.Max(worstGap, Math.Max(0, bounds.End.X - coveredTo));
                    if (sample == count) minimumReceiverContacts = Math.Min(minimumReceiverContacts, contacts.Length);
                }
                samples++;
            }
        }
        finally { pallet.Position = home; }
        check(supported, "multi_conveyor_sampled_runner_contact_hulls_bracket_centers");
        check(minimumReceiverContacts >= 2, "multi_conveyor_endpoint_each_runner_bears_on_multiple_rollers");
        // For this route the load only translates along X. Sweeping each mesh
        // AABB through the whole segment is conservative: empty corners can
        // produce a candidate, but a clear envelope cannot skip a collision
        // between samples. Optical beam visualization is intentionally not a
        // solid; the sensor bodies/lenses remain in the stationary set.
        var movingMeshes = ReviewMeshes(pallet);
        var stationary = ReviewMeshes(root).Except(movingMeshes)
            .Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal))
            .Select(mesh => (Mesh: mesh, Bounds: ReviewBounds(mesh))).ToArray();
        var candidates = new List<string>();
        foreach (var moving in movingMeshes)
        {
            var bounds = ReviewBounds(moving);
            var swept = new Aabb(bounds.Position, bounds.Size + new Vector3((float)(12.3 - home.X), 0, 0));
            foreach (var fixedMesh in stationary)
            {
                var overlap = swept.Intersection(fixedMesh.Bounds).Size;
                // Intended carrying contact is a zero-thickness intersection.
                // State the tolerance explicitly rather than claiming exact
                // triangle collision or including physical deformation.
                if (overlap.X > .001f && overlap.Y > .001f && overlap.Z > .001f
                    && (!fixedMesh.Mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
                        || RouteTriangleEntersBox(fixedMesh.Mesh, swept.Grow(-.001f))))
                    candidates.Add($"{moving.Name}/{fixedMesh.Mesh.Name}");
            }
        }
        check(candidates.Count == 0, "multi_conveyor_continuous_translation_envelope_clear_at_one_mm_tolerance");
        if (candidates.Count > 0) GD.Print("MULTI_CONVEYOR_SWEEP_CANDIDATES " + string.Join(",", candidates.Take(20)));
        // Report the measured gap, without inventing a structural acceptance
        // limit for this delivered pallet or claiming physical commissioning.
        GD.Print($"MULTI_CONVEYOR_BEARING samples={samples} spacing_m=.005 worst_uncovered_runner_span_m={worstGap:F6} endpoint_min_contacts={minimumReceiverContacts} geometric_only=True");
    }

    private static bool RouteTriangleEntersBox(MeshInstance3D mesh, Aabb box, Transform3D? referenceInverse = null)
    {
        // Clip every transformed face to the conservative swept box. This
        // rejects empty corners of bent cable bounds without excluding cables.
        var faces = mesh.Mesh.GetFaces();
        var transform = (referenceInverse ?? Transform3D.Identity) * mesh.GlobalTransform;
        for (var index = 0; index < faces.Length; index += 3)
        {
            var polygon = new List<Vector3> { transform * faces[index],
                transform * faces[index + 1], transform * faces[index + 2] };
            for (var axis = 0; axis < 3 && polygon.Count > 0; axis++)
            foreach (var upper in new[] { false, true })
            {
                var plane = upper ? box.End[axis] : box.Position[axis];
                var clipped = new List<Vector3>();
                for (var edge = 0; edge < polygon.Count; edge++)
                {
                    var start = polygon[edge]; var end = polygon[(edge + 1) % polygon.Count];
                    var startInside = upper ? start[axis] <= plane : start[axis] >= plane;
                    var endInside = upper ? end[axis] <= plane : end[axis] >= plane;
                    if (startInside) clipped.Add(start);
                    if (startInside != endInside) clipped.Add(start.Lerp(end, (plane - start[axis]) / (end[axis] - start[axis])));
                }
                polygon = clipped;
            }
            if (polygon.Count >= 3) return true;
        }
        return false;
    }
}
