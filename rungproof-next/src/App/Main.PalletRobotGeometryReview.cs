using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyShippingPalletGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-18-pallet-pickup", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var conveyor = root.GetNode<Node3D>("pickup_conveyor");
        var pallet = root.GetNode<Node3D>("shipping_pallet");
        var sensor = root.GetNode<Node3D>("pickup_end_sensor");
        var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
        var tailX = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_tail_drum", true, false)).GetCenter().X;
        var driveX = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_drive_drum", true, false)).GetCenter().X;
        var runners = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_BOTTOM_", StringComparison.Ordinal)).ToArray();
        GD.Print($"SHIPPING_PALLET_DATUM belt={belt} flatX={tailX}..{driveX} pallet={ReviewBounds(pallet)}");
        check(runners.Length == 3 && runners.All(mesh => MathF.Abs(ReviewBounds(mesh).Position.Y - belt.End.Y) < 0.001f),
            "shipping_pallet_all_three_runners_seated_on_belt");
        // Inspect each bearing interface independently. A supported bottom
        // runner alone does not prove that the deck or cases are supported.
        var meshes = ReviewMeshes(pallet);
        var blocks = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PALLET_BLOCK_", StringComparison.Ordinal)).ToArray();
        var stringers = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PALLET_STRINGER_", StringComparison.Ordinal)).ToArray();
        var decks = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PALLET_TOP_DECK_", StringComparison.Ordinal)).ToArray();
        bool BearsOn(MeshInstance3D upper, MeshInstance3D lower)
        {
            var a = ReviewBounds(upper); var b = ReviewBounds(lower);
            return MathF.Abs(a.Position.Y - b.End.Y) < 0.0001f
                && MathF.Min(a.End.X, b.End.X) - MathF.Max(a.Position.X, b.Position.X) > 0.02f
                && MathF.Min(a.End.Z, b.End.Z) - MathF.Max(a.Position.Z, b.Position.Z) > 0.02f;
        }
        check(blocks.Length == 9 && blocks.All(block => runners.Any(runner => BearsOn(block, runner))),
            "shipping_pallet_nine_blocks_bear_on_bottom_boards_without_penetration");
        check(stringers.Length == 3 && stringers.All(board => blocks.Count(block => BearsOn(board, block)) == 3),
            "shipping_pallet_stringers_bear_on_all_nine_blocks_without_gap");
        check(decks.Length == 7 && decks.All(deck => stringers.Count(board => BearsOn(deck, board)) == 3),
            "shipping_pallet_all_deck_boards_bear_on_three_stringers_without_gap");
        var lowerCases = meshes.Where(mesh => mesh.Name.ToString().StartsWith("CASE_0_", StringComparison.Ordinal)).ToArray();
        var upperCases = meshes.Where(mesh => mesh.Name.ToString().StartsWith("CASE_1_", StringComparison.Ordinal)).ToArray();
        var lowerTape = meshes.Where(mesh => mesh.Name.ToString().StartsWith("CASE_TAPE_0_", StringComparison.Ordinal)).ToArray();
        check(lowerCases.Length == 4 && lowerCases.All(box => decks.Count(deck => BearsOn(box, deck)) >= 2),
            "shipping_pallet_four_lower_cases_contact_multiple_deck_boards");
        check(upperCases.Length == 4 && upperCases.All(box => lowerTape.Any(tape => BearsOn(box, tape)))
            && lowerTape.Length == 4 && lowerTape.All(tape => lowerCases.Any(box => BearsOn(tape, box))),
            "shipping_pallet_upper_cases_contact_seated_lower_sealing_tape");
        GD.Print($"SHIPPING_PALLET_INTERNAL_DATUM bottomTop={ReviewBounds(runners[0]).End.Y} blockTop={ReviewBounds(blocks[0]).End.Y} stringerTop={ReviewBounds(stringers[0]).End.Y} deckTop={ReviewBounds(decks[0]).End.Y} lowerCaseBottom={ReviewBounds(lowerCases[0]).Position.Y}");
        VerifyShippingLoadStraps(check, pallet, belt, stringers);
        var supported = true;
        // Inspect actual reference transforms, including both endpoints. The
        // flat carrying span ends at the drum axes, not at the curved wraps.
        _sceneRuntime!.UsesExternalClock = false; // Offline reference, not a loaded controller lesson.
        _sceneRuntime.ExecuteAction("start-auto");
        for (var sample = 0; sample <= 1200; sample++)
        {
            supported &= runners.All(mesh =>
            {
                var bounds = ReviewBounds(mesh);
                return MathF.Abs(bounds.Position.Y - belt.End.Y) < 0.001f
                    && bounds.Position.X >= tailX && bounds.End.X <= driveX
                    && bounds.Position.Z >= belt.Position.Z && bounds.End.Z <= belt.End.Z;
            });
            _sceneRuntime.AdvanceSimulation(0.01);
        }
        check(supported, "shipping_pallet_entire_automatic_route_has_full_flat_runner_support");
        var lens = ReviewBounds((MeshInstance3D)sensor.FindChild("TX_lens", true, false)).GetCenter();
        var leadingOffset = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("CASE_", StringComparison.Ordinal)
                && char.IsDigit(mesh.Name.ToString()[5])).Select(ReviewBounds)
            .Where(bounds => bounds.Position.Y < lens.Y && bounds.End.Y > lens.Y).Max(bounds => bounds.End.X - pallet.Position.X);
        var pickupX = lens.X - leadingOffset;
        check(MathF.Abs(pallet.Position.X - pickupX) < 0.001f
            && _sceneRuntime.Points["pickup_sensor"] is true
            && _sceneRuntime.Points["conveyor_run"] is false,
            "shipping_pallet_reference_reaches_pickup_endpoint");
        _sceneRuntime.ResetSimulation();
        var sensorSolids = ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var conveyorSolids = ReviewMeshes(conveyor);
        check(sensorSolids.All(a => conveyorSolids.All(b =>
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            if (overlap.X <= 0.001f || overlap.Y <= 0.001f || overlap.Z <= 0.001f || !OrientedBoxesPenetrate(a, b)) return true;
            GD.Print($"SHIPPING_PALLET_SENSOR_CONFLICT {a.Name}/{b.Name} a={ReviewBounds(a)} b={ReviewBounds(b)}");
            return false;
        })), "shipping_pallet_sensor_hardware_clear_of_conveyor_bounds");
        check(new[] { "TX", "RX" }.All(side => MathF.Abs(ReviewBounds((MeshInstance3D)sensor.FindChild($"{side}_foot", true, false)).Position.Y) < 0.001f),
            "shipping_pallet_both_sensor_feet_grounded");
        VerifyShippingPalletReference(check, pallet, sensor, pickupX);
    }

    private static void VerifyShippingLoadStraps(Action<bool, string> check, Node3D pallet, Aabb belt, MeshInstance3D[] stringers)
    {
        var straps = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("LOAD_STRAP_LOOP_", StringComparison.Ordinal)).ToArray();
        check(straps.Length == 2, "shipping_pallet_has_two_routed_strap_loops");
        string Key(Vector3 point) => $"{MathF.Round(point.X * 1000000)}:{MathF.Round(point.Y * 1000000)}:{MathF.Round(point.Z * 1000000)}";
        bool ClosedLoop(MeshInstance3D mesh)
        {
            var faces = mesh.Mesh.GetFaces();
            var vertices = new System.Collections.Generic.HashSet<string>();
            var edges = new System.Collections.Generic.Dictionary<string, int>();
            for (var index = 0; index < faces.Length; index += 3)
            for (var edge = 0; edge < 3; edge++)
            {
                var a = Key(faces[index + edge]); var b = Key(faces[index + (edge + 1) % 3]);
                vertices.Add(a); vertices.Add(b);
                var key = string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
                edges.TryGetValue(key, out var count);
                edges[key] = count + 1;
            }
            // A closed band has a hole (Euler characteristic zero), unlike
            // the old three separate bars. Every welded edge has two faces.
            return faces.Length > 0 && edges.Values.All(count => count == 2)
                && vertices.Count - edges.Count + faces.Length / 3 == 0;
        }
        check(straps.Length == 2 && straps.All(ClosedLoop), "shipping_pallet_strap_meshes_are_closed_bands_with_return_path");
        var underside = stringers.Min(mesh => ReviewBounds(mesh).Position.Y);
        check(straps.Length == 2 && straps.All(mesh =>
            MathF.Abs(ReviewBounds(mesh).Position.Y - (underside - 0.0012f)) < 0.0002f
            && ReviewBounds(mesh).Position.Y > belt.End.Y + 0.1f),
            "shipping_pallet_strap_returns_pass_under_stringers_clear_of_belt");
        var solids = ReviewMeshes(pallet).Where(mesh => !mesh.Name.ToString().StartsWith("LOAD_STRAP_", StringComparison.Ordinal)).ToArray();
        bool InsideConvexSolid(MeshInstance3D mesh, Vector3 worldPoint)
        {
            var point = mesh.GlobalTransform.AffineInverse() * worldPoint;
            if (!mesh.GetAabb().Grow(-0.0002f).HasPoint(point)) return false;
            var center = mesh.GetAabb().GetCenter(); var faces = mesh.Mesh.GetFaces();
            for (var index = 0; index < faces.Length; index += 3)
            {
                var a = faces[index]; var normal = (faces[index + 1] - a).Cross(faces[index + 2] - a);
                if (normal.LengthSquared() < 1e-12f) continue;
                normal = normal.Normalized();
                if (normal.Dot(center - a) > 0) normal = -normal;
                if (normal.Dot(point - a) >= -0.0002f) return false;
            }
            return true;
        }
        var clear = straps.Length == 2;
        foreach (var strap in straps)
        {
            var faces = strap.Mesh.GetFaces();
            for (var index = 0; index < faces.Length; index += 3)
            {
                var a = strap.GlobalTransform * faces[index]; var b = strap.GlobalTransform * faces[index + 1]; var c = strap.GlobalTransform * faces[index + 2];
                foreach (var point in new[] { a, b, c, (a + b) / 2, (b + c) / 2, (a + c) / 2, (a + b + c) / 3 })
                    clear &= !solids.Any(mesh => InsideConvexSolid(mesh, point));
            }
        }
        check(clear, "shipping_pallet_strap_triangle_samples_do_not_penetrate_convex_load_parts");
    }

    private void VerifyShippingPalletReference(Action<bool, string> check, Node3D pallet, Node3D sensor, float pickupX)
    {
        var runtime = _sceneRuntime!;
        var home = pallet.Position;
        var travel = pickupX - home.X;
        var opticalAll = true;
        void Advance(double seconds)
        {
            for (var remaining = seconds; remaining > 1e-9; remaining -= 0.01)
            {
                runtime.AdvanceSimulation(Math.Min(0.01, remaining));
                opticalAll &= OpticalMatch();
            }
        }
        double Progress() => Convert.ToDouble(runtime.Points["pallet_position"]);
        bool OpticalMatch()
        {
            var tx = ReviewBounds((MeshInstance3D)sensor.FindChild("TX_lens", true, false)).GetCenter();
            var rx = ReviewBounds((MeshInstance3D)sensor.FindChild("RX_lens", true, false)).GetCenter();
            var blocked = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("CASE_", StringComparison.Ordinal)
                && char.IsDigit(mesh.Name.ToString()[5])).Any(mesh => LineHitsBounds(tx, rx, ReviewBounds(mesh)));
            return blocked == (runtime.Points["pickup_sensor"] is true);
        }
        check(!runtime.ExecuteAction("manual-jog") && pallet.Position.IsEqualApprox(home), "shipping_pallet_jog_blocked_in_auto");
        runtime.ResetSimulation();
        runtime.ExecuteAction("set-auto");
        check(!runtime.RunDefault(), "shipping_pallet_auto_run_blocked_in_manual");
        var progressive = true; var optical = true;
        for (var jog = 1; jog <= 4; jog++)
        {
            var before = pallet.Position;
            progressive &= runtime.ExecuteAction("manual-jog") && pallet.Position.IsEqualApprox(before);
            Advance(3);
            progressive &= MathF.Abs(pallet.Position.X - (home.X + travel * jog / 4)) < 0.001f
                && Math.Abs(Progress() - jog * 25) < 0.02;
            optical &= OpticalMatch();
        }
        check(progressive, "shipping_pallet_four_jogs_advance_from_current_pose_by_quarters");
        var atPickup = pallet.Position;
        check(!runtime.ExecuteAction("manual-jog") && pallet.Position.IsEqualApprox(atPickup), "shipping_pallet_fifth_jog_blocked_at_pickup");
        check(optical && opticalAll, "shipping_pallet_jog_feedback_matches_case_optical_path");
        runtime.ResetSimulation(); runtime.RunDefault(); Advance(1);
        check(MathF.Abs(pallet.Position.X - home.X - 0.75f) < 0.001f
            && Math.Abs(Progress() - 0.75 / travel * 100) < 0.02, "shipping_pallet_speed_and_progress_follow_actual_travel");
        check(!runtime.ExecuteAction("start-auto"), "shipping_pallet_active_start_does_not_replace_motion");
        runtime.StopSimulation(); var stopped = pallet.Position; var stoppedProgress = Progress(); Advance(1);
        check(pallet.Position.IsEqualApprox(stopped) && Math.Abs(Progress() - stoppedProgress) < 0.001
            && runtime.Points["conveyor_run"] is false, "shipping_pallet_stop_holds_pose_and_progress");
        check(runtime.RunDefault() && pallet.Position.IsEqualApprox(stopped), "shipping_pallet_restart_continues_without_teleport");
        Advance(1);
        check(MathF.Abs(pallet.Position.X - stopped.X - 0.75f) < 0.001f, "shipping_pallet_resumed_motion_keeps_configured_speed");
        runtime.ExecuteAction("set-auto"); Advance(0.02);
        check(runtime.Points["conveyor_run"] is false && !runtime.IsRunning, "shipping_pallet_mode_change_stops_reference");
        runtime.ResetSimulation(); runtime.RunDefault(); runtime.AdvanceSimulation(30);
        check(MathF.Abs(pallet.Position.X - pickupX) < 0.001f && OpticalMatch()
            && runtime.Points["pickup_sensor"] is true && runtime.Points["conveyor_run"] is false,
            "shipping_pallet_large_clock_step_stops_at_first_optical_crossing");
        runtime.ResetSimulation();
        check(pallet.Position.IsEqualApprox(home) && Progress() == 0 && runtime.Points["pickup_sensor"] is false,
            "shipping_pallet_reset_restores_pose_and_actual_feedback");
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["conveyor_run"] = true });
        var controllerOwnsCommand = !runtime.RunDefault();
        runtime.AdvanceSimulation(1);
        check(controllerOwnsCommand && runtime.Points["conveyor_run"] is true
            && MathF.Abs(pallet.Position.X - home.X - .75f) < .001f,
            "shipping_pallet_controller_command_drives_motion_without_reference_output_writes");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["conveyor_run"] = false });
        var held = pallet.Position; runtime.AdvanceSimulation(1);
        check(pallet.Position.IsEqualApprox(held), "shipping_pallet_controller_command_loss_holds_pose");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["conveyor_run"] = true });
        runtime.AdvanceSimulation(30);
        check(runtime.Points["conveyor_run"] is true && OpticalMatch()
            && pallet.Position.X > pickupX && pallet.Position.X <= 3.1001f,
            "shipping_pallet_missing_plc_stop_remains_visible_and_bounded");
        runtime.UsesExternalClock = false;
        runtime.ResetSimulation();
    }

    private void VerifyPalletRobotInstallationGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-17-pallet-robot", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var conveyor = root.GetNode<Node3D>("robot_pallet_conveyor");
        var pallet = root.GetNode<Node3D>("robot_pallet");
        var receiver = root.GetNode<Node3D>("process_receiver");
        var sensor = root.GetNode<Node3D>("pallet_ready_sensor");
        var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
        var palletBounds = ReviewBounds(pallet);

        var runners = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_bottom_runner", StringComparison.Ordinal)).ToArray();
        check(runners.Length == 3 && runners.All(mesh => MathF.Abs(ReviewBounds(mesh).Position.Y - belt.End.Y) < 0.001f)
            && palletBounds.Position.X >= belt.Position.X && palletBounds.End.X <= belt.End.X
            && palletBounds.Position.Z >= belt.Position.Z && palletBounds.End.Z <= belt.End.Z,
            "pallet_robot_all_three_bottom_runners_seated_on_belt");
        var boards = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_top_board", StringComparison.Ordinal)).ToArray();
        var deckY = boards.Max(mesh => ReviewBounds(mesh).End.Y);
        check(new[] { "container_a", "container_b" }.All(id =>
        {
            var load = ReviewBounds(root.GetNode(id));
            return MathF.Abs(load.Position.Y - deckY) < 0.001f
                && load.Position.X >= palletBounds.Position.X && load.End.X <= palletBounds.End.X
                && load.Position.Z >= palletBounds.Position.Z && load.End.Z <= palletBounds.End.Z;
        }), "pallet_robot_both_containers_seated_within_pallet_deck");
        check(ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_top_nail", StringComparison.Ordinal))
            .All(mesh => ReviewBounds(mesh).End.Y <= deckY + 0.001f), "pallet_robot_top_nail_heads_do_not_protrude_into_staged_loads");

        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            if (overlap.X <= 0.001f || overlap.Y <= 0.001f || overlap.Z <= 0.001f || !OrientedBoxesPenetrate(a, b)) return true;
            // Routed cable bounds enclose empty space. Check the actual cable
            // triangles before reporting a solid installation conflict.
            if (b.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase))
            {
                var faces = b.Mesh.GetFaces();
                var bounds = ReviewBounds(a);
                for (var index = 0; index < faces.Length; index += 3)
                {
                    var triangle = new Aabb(b.GlobalTransform * faces[index], Vector3.Zero)
                        .Expand(b.GlobalTransform * faces[index + 1]).Expand(b.GlobalTransform * faces[index + 2]);
                    var size = bounds.Intersection(triangle).Size;
                    if (size.X > 0.001f && size.Y > 0.001f && size.Z > 0.001f) return false;
                }
                return true;
            }
            GD.Print($"PALLET_ROBOT_INSTALLATION_CONFLICT {a.Name}/{b.Name} a={ReviewBounds(a)} b={ReviewBounds(b)}");
            return false;
        }
        var others = root.GetChildren().OfType<Node3D>().Where(node => node != receiver)
            .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        check(ReviewMeshes(receiver).All(a => others.All(b => Clear(a, b))),
            "pallet_robot_receiver_clear_of_all_separate_equipment_at_home");
        check(ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal))
                .All(a => ReviewMeshes(conveyor).All(b => Clear(a, b))),
            "pallet_robot_sensor_hardware_clear_of_conveyor");
        check(new[] { "TX", "RX" }.All(side =>
        {
            var cable = ReviewBounds((MeshInstance3D)sensor.FindChild($"{side}_cable", true, false));
            var post = ReviewBounds((MeshInstance3D)sensor.FindChild($"{side}_post", true, false));
            var housing = ReviewBounds((MeshInstance3D)sensor.FindChild($"{side}_housing", true, false));
            var connector = ReviewBounds((MeshInstance3D)sensor.FindChild($"{side}_m12_connector", true, false));
            return MathF.Abs(cable.GetCenter().Z - post.GetCenter().Z) < 0.001f
                && cable.Grow(0.002f).Intersects(housing)
                && MathF.Abs(cable.Position.Y - connector.End.Y) < 0.002f;
        }), "pallet_robot_each_sensor_pigtail_follows_its_stand_and_connects_at_both_ends");
        GD.Print($"PALLET_ROBOT_DATUM belt={belt} pallet={palletBounds} deckY={deckY} receiver={ReviewBounds(receiver)}");
        VerifyPalletOutboundGeometry(check);
    }

    private void VerifyPalletOutboundGeometry(Action<bool, string> check)
    {
        var root = _sceneCompositionRoot!;
        var outbound = root.GetNodeOrNull<Node3D>("robot_pallet_outbound");
        var bridge = root.GetNodeOrNull<Node3D>("robot_pallet_transfer_bridge");
        check(outbound is not null && bridge is not null, "pallet_robot_outbound_conveyor_and_transfer_bridge_present");
        if (outbound is null || bridge is null) return;
        var inbound = root.GetNode<Node3D>("robot_pallet_conveyor");
        var pallet = root.GetNode<Node3D>("robot_pallet");
        Aabb FlatBelt(Node3D conveyor)
        {
            var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
            var tailX = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_tail_drum", true, false)).GetCenter().X;
            var driveX = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_drive_drum", true, false)).GetCenter().X;
            return new Aabb(new Vector3(tailX, belt.End.Y, belt.Position.Z), new Vector3(driveX - tailX, 0, belt.Size.Z));
        }
        var staging = FlatBelt(inbound); var receiving = FlatBelt(outbound);
        var deck = ReviewBounds((MeshInstance3D)bridge.FindChild("PALLET_TRANSFER_deck", true, false));
        check(MathF.Abs(staging.Position.Y - receiving.Position.Y) < 0.001f
            && MathF.Abs(deck.End.Y - staging.Position.Y) < 0.001f
            && deck.Position.X > staging.End.X && deck.End.X < receiving.Position.X,
            "pallet_robot_both_belts_and_bridge_share_carrying_height");

        var feet = ReviewMeshes(bridge).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_TRANSFER_foot", StringComparison.Ordinal)).ToArray();
        var posts = ReviewMeshes(bridge).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_TRANSFER_post", StringComparison.Ordinal)).ToArray();
        var bearer = ReviewBounds((MeshInstance3D)bridge.FindChild("PALLET_TRANSFER_bearer", true, false));
        check(feet.Length == 2 && posts.Length == 2 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f)
            && posts.All(post => feet.Any(foot => MathF.Abs(ReviewBounds(post).Position.Y - ReviewBounds(foot).End.Y) < 0.001f
                && ReviewBounds(foot).Grow(0.001f).Intersects(ReviewBounds(post)))
                && MathF.Abs(ReviewBounds(post).End.Y - bearer.Position.Y) < 0.001f)
            && MathF.Abs(bearer.End.Y - deck.Position.Y) < 0.001f,
            "pallet_robot_transfer_bridge_has_grounded_connected_supports");
        var obstacles = root.GetChildren().OfType<Node3D>().Where(node => node != pallet)
            .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        check(new[] { outbound, bridge }.All(equipment => ReviewMeshes(equipment).All(part =>
            root.GetChildren().OfType<Node3D>().Where(node => node != equipment).SelectMany(ReviewMeshes)
                .Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal))
                .All(other => PalletTransferPartsClear(part, other)))),
            "pallet_robot_outbound_and_bridge_clear_of_separate_equipment");
        check(new[] { inbound, outbound }.All(conveyor =>
            MathF.Abs(ReviewBounds((MeshInstance3D)conveyor.FindChild("BELT_vulcanized_splice", true, false)).End.Y - staging.Position.Y) < 0.001f),
            "pallet_robot_both_visual_splice_witnesses_flush_with_belt");

        var runtime = _sceneRuntime!;
        // Exercise the same standalone reference clock as --visual-plant-review.
        // The normal controller-owned shell must keep its separate clock/outputs.
        var originalClock = runtime.UsesExternalClock;
        var runners = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_bottom_runner", StringComparison.Ordinal)).ToArray();
        var surfaces = new[] { staging, receiving, new Aabb(deck.Position with { Y = deck.End.Y }, deck.Size with { Y = 0 }) };
        var supported = true; var moved = false; var commandsTogether = true; var sweepClear = true;
        var receiver = root.GetNode<Node3D>("process_receiver");
        var loads = new[] { root.GetNode<Node3D>("container_a"), root.GetNode<Node3D>("container_b") };
        var robotRoot = root.GetNode<Node3D>("pallet_robot");
        var robot = robotRoot.FindChild("PalletRobotMotion", true, false) as Scenes.PalletRobotMotion;
        check(robot is not null && !robotRoot.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Any(),
            "pallet_robot_uses_imported_joint_handler_without_generic_base_oscillator");
        var robotFoot = ReviewBounds((MeshInstance3D)robotRoot.FindChild("ROBOT_installation_foot", true, false));
        var robotPost = ReviewBounds((MeshInstance3D)robotRoot.FindChild("ROBOT_installation_pedestal", true, false));
        check(MathF.Abs(robotFoot.Position.Y) < 0.001f && MathF.Abs(robotFoot.End.Y - robotPost.Position.Y) < 0.001f
            && MathF.Abs(robotPost.End.Y - ReviewBounds((MeshInstance3D)robotRoot.FindChild("ROBOT_base", true, false)).Position.Y) < 0.001f,
            "pallet_robot_pedestal_connects_grounded_foot_to_imported_base");
        var receiverFeet = ReviewMeshes(receiver).Where(mesh => mesh.Name.ToString().StartsWith("RECEIVER_grounded_foot", StringComparison.Ordinal)).ToArray();
        var receiverPosts = ReviewMeshes(receiver).Where(mesh => mesh.Name.ToString().StartsWith("RECEIVER_support_post", StringComparison.Ordinal)).ToArray();
        check(receiverFeet.Length == 4 && receiverPosts.Length == 4 && receiverFeet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f)
            && receiverPosts.All(post => receiverFeet.Any(foot => ReviewBounds(foot).Grow(0.001f).Intersects(ReviewBounds(post))
                && MathF.Abs(ReviewBounds(foot).End.Y - ReviewBounds(post).Position.Y) < 0.001f)
                && MathF.Abs(ReviewBounds(post).End.Y - ReviewBounds((MeshInstance3D)receiver.FindChild("RECEIVER_BASE", true, false)).Position.Y) < 0.001f),
            "pallet_robot_raised_receiver_has_four_grounded_connected_supports");
        check(ReviewMeshes(robotRoot).Where(mesh => mesh.Name.ToString().StartsWith("ROBOT_gripper_finger", StringComparison.Ordinal))
            .All(mesh => mesh.GetParent().Name == "KIN_axis_6"), "pallet_robot_fingers_and_fasteners_follow_imported_wrist_joint");
        var homeAngles = robot?.JointAngles;
        var attached = true; var hadAttachment = false; var jointsInRange = true; var robotSweepClear = true;
        var robotObstacles = root.GetChildren().OfType<Node3D>().Where(node => node != robotRoot && !loads.Contains(node))
            .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var loadObstacles = root.GetChildren().OfType<Node3D>().Where(node => !loads.Contains(node))
            .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        var robotParts = ReviewMeshes(robotRoot);
        var loadParts = loads.Select(ReviewMeshes).ToArray();
        var palletParts = ReviewMeshes(pallet);
        var movingParts = robotParts.Concat(loadParts.SelectMany(parts => parts)).Concat(palletParts).ToArray();
        // Refresh moving bounds once per sample; repeated cross-equipment
        // pairs must not repeat thousands of Godot transform calls. The same
        // narrow-phase test still runs for every broad-phase candidate.
        var sampledBounds = ReviewMeshes(root).ToDictionary(part => part, ReviewBounds);
        bool SampledClear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = sampledBounds[a].Intersection(sampledBounds[b]).Size;
            return overlap.X <= 0.001f || overlap.Y <= 0.001f || overlap.Z <= 0.001f
                || PalletTransferPartsClear(a, b);
        }
        var loadSweepClear = true; var countRequiresLanding = true;
        bool Landed(int index)
        {
            var side = index == 0 ? "left" : "right";
            var rollers = ReviewMeshes(receiver).Where(mesh => mesh.Name.ToString() == $"KIN_receiver_roller_{side}"
                || mesh.Name.ToString().StartsWith($"RECEIVER_ROLLER_{side}_", StringComparison.Ordinal)).ToArray();
            if (rollers.Length != 5) return false;
            var carrying = ReviewBounds(rollers[0]);
            foreach (var roller in rollers.Skip(1)) carrying = carrying.Merge(ReviewBounds(roller));
            var load = ReviewBounds(loads[index]);
            return MathF.Abs(load.Position.Y - carrying.End.Y) < 0.001f
                && load.Position.X >= carrying.Position.X && load.End.X <= carrying.End.X
                && load.Position.Z >= carrying.Position.Z && load.End.Z <= carrying.End.Z;
        }
        var startX = pallet.Position.X;
        var drives = new[] { inbound, outbound }.Select(node => node.FindChildren("*", string.Empty, true, false)
            .OfType<ConveyorController>().Single()).ToArray();
        try
        {
            runtime.UsesExternalClock = false;
            runtime.ResetSimulation();
            runtime.RunDefault();
            for (var tick = 0; tick < 5000; tick++)
            {
                runtime.AdvanceSimulation(0.01);
                foreach (var part in movingParts) sampledBounds[part] = ReviewBounds(part);
                if (robot is not null)
                {
                    jointsInRange &= MathF.Abs(robot.JointAngles[0]) <= Mathf.DegToRad(170) + 0.0001f;
                    if (robot.GrippedLoad is { } held)
                    {
                        hadAttachment = true;
                        var bounds = ReviewBounds(held);
                        var expected = held.GlobalPosition with { Y = bounds.Position.Y + 0.53f };
                        attached &= robot.ToolTransform.Origin.DistanceTo(expected) < 0.0003f
                            && robot.ToolTransform.Basis.X.DistanceTo(Vector3.Back) < 0.003f
                            && robot.ToolTransform.Basis.Y.DistanceTo(Vector3.Up) < 0.003f;
                    }
                    if (robotSweepClear)
                        robotSweepClear = robotParts.All(part => robotObstacles.All(other => SampledClear(part, other)));
                }
                // Test the actual timed reference, including the transfer path,
                // rather than accepting placed_count as evidence of placement.
                if (loadSweepClear)
                {
                    loadSweepClear = loadParts.All(parts => parts.All(part =>
                        loadObstacles.All(other => SampledClear(part, other))))
                        && loadParts[0].All(part => loadParts[1].All(other => SampledClear(part, other)));
                    if (!loadSweepClear) GD.Print($"PALLET_LOAD_SWEEP_SAMPLE timeS={(tick + 1) * 0.01} tool={robot?.ToolTransform.Origin}");
                }
                var placed = Convert.ToInt32(runtime.Points["placed_count"]);
                if (placed >= 1) countRequiresLanding &= Landed(0);
                if (placed >= 2) countRequiresLanding &= Landed(1);
                moved |= pallet.Position.X > startX + 0.001f;
                commandsTogether &= drives.All(drive => drive.RunCommand == (runtime.Points["conveyor_run"] is true));
                foreach (var runner in runners)
                {
                    var bounds = ReviewBounds(runner);
                    var contactLength = surfaces.Where(surface => bounds.Position.Z >= surface.Position.Z - 0.001f
                            && bounds.End.Z <= surface.End.Z + 0.001f && MathF.Abs(bounds.Position.Y - surface.Position.Y) < 0.001f)
                        .Sum(surface => MathF.Max(0, MathF.Min(bounds.End.X, surface.End.X) - MathF.Max(bounds.Position.X, surface.Position.X)));
                    supported &= contactLength >= 0.10f;
                }
                if (moved) sweepClear &= palletParts.All(part => obstacles.All(other => SampledClear(part, other)));
                if (runtime.Points["cycle_complete"] is true) break;
            }
            var final = ReviewBounds(pallet);
            check(moved && supported, "pallet_robot_all_three_runners_supported_through_actual_reference_release");
            check(moved && sweepClear, "pallet_robot_complete_pallet_mesh_clear_through_actual_reference_release");
            check(runtime.Points["cycle_complete"] is true && final.Position.X >= receiving.Position.X
                && final.End.X <= receiving.End.X, "pallet_robot_empty_pallet_finishes_fully_on_outbound_flat_belt");
            check(commandsTogether && drives.All(drive => !drive.RunCommand),
                "pallet_robot_one_existing_command_drives_both_conveyors_and_stops_at_completion");
            check(Landed(0) && Landed(1), "pallet_robot_both_transferred_containers_land_within_actual_receiver_rollers");
            check(countRequiresLanding, "pallet_robot_placed_count_requires_each_container_already_seated");
            check(loadSweepClear, "pallet_robot_container_meshes_clear_separate_equipment_through_reference_transfer");
            check(hadAttachment && attached, "pallet_robot_gripped_load_follows_actual_tool_position_and_orientation");
            check(jointsInRange, "pallet_robot_base_joint_remains_inside_catalog_170_degree_range");
            check(robotSweepClear, "pallet_robot_actual_joint_sweep_clears_separate_equipment");
            GD.Print($"PALLET_RECEIVER_LANDING left={ReviewBounds(loads[0])} right={ReviewBounds(loads[1])}");
            GD.Print($"PALLET_OUTBOUND_DATUM staging={staging} bridge={deck} receiving={receiving} final={final}");
        }
        finally
        {
            runtime.ResetSimulation();
            runtime.UsesExternalClock = originalClock;
        }
        check(MathF.Abs(pallet.Position.X - startX) < 0.001f && drives.All(drive => !drive.RunCommand),
            "pallet_robot_reset_restores_staged_pallet_and_stops_both_conveyors");
        check(robot is not null && robot.GrippedLoad is null && !robot.RunCommand
            && robot.JointAngles.Zip(homeAngles!).All(pair => MathF.Abs(pair.First - pair.Second) < 0.0001f),
            "pallet_robot_reset_releases_load_and_restores_every_joint");
        if (robot is null) return;
        try
        {
            runtime.UsesExternalClock = false;
            runtime.RunDefault();
            for (var tick = 0; tick < 402; tick++) runtime.AdvanceSimulation(0.01);
            var held = robot.GrippedLoad;
            var heldPosition = held?.GlobalPosition;
            var stoppedAngles = robot.JointAngles;
            runtime.StopSimulation();
            runtime.AdvanceSimulation(2.0);
            var restarted = runtime.RunDefault();
            check(held is not null && held.GlobalPosition == heldPosition && !robot.RunCommand
                && !restarted
                && robot.JointAngles.Zip(stoppedAngles).All(pair => pair.First == pair.Second),
                "pallet_robot_stop_holds_gripped_load_and_all_joint_poses_and_requires_reset_before_restart");
            runtime.ResetSimulation();
            var installed = robotRoot.GlobalPosition;
            try
            {
                robotRoot.GlobalPosition += Vector3.Right * 100;
                runtime.RunDefault();
                runtime.AdvanceSimulation(2.5);
                check(!robot.RunCommand && runtime.Points["robot_run"] is false
                    && Convert.ToInt32(runtime.Points["placed_count"]) == 0 && runtime.Points["cycle_complete"] is false,
                    "pallet_robot_unreachable_motion_stops_before_placement_or_completion");
            }
            finally { robotRoot.GlobalPosition = installed; }
        }
        finally
        {
            runtime.ResetSimulation();
            runtime.UsesExternalClock = originalClock;
        }
    }

    private void VerifyPalletRobotCommandBoundary(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-17-pallet-robot", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        var pallet = _sceneCompositionRoot!.GetNode<Node3D>("robot_pallet");
        runtime.UsesExternalClock = false;
        runtime.ResetSimulation();
        check(runtime.SampleVirtualControllerInputs()["robot_at_park"]
            && !runtime.SampleVirtualControllerInputs()["cycle_complete"]
            && runtime.SampleVirtualControllerNumericInputs()["placed_count"] == 0,
            "pallet_robot_feedback_is_readable_at_reset");
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.AdvanceSimulation(.02);
        check(runtime.SampleVirtualControllerInputs()["robot_at_park"], "pallet_robot_no_sequence_before_command");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool>
            { ["robot_run"] = true, ["conveyor_run"] = false });
        for (var tick = 0; tick < 30; tick++) runtime.AdvanceSimulation(.02);
        check(!runtime.SampleVirtualControllerInputs()["robot_at_park"]
            && runtime.Points["robot_run"] is true && runtime.Points["conveyor_run"] is false,
            "pallet_robot_plc_command_starts_sequence_without_replacing_output_image");
        runtime.StopSimulation();
        runtime.TryGetAction("start-robot", out var startAction);
        check(!runtime.CanExecuteAction(startAction), "pallet_robot_stopped_carry_rejects_operator_restart_before_reset");
        runtime.ResetSimulation();
        check(runtime.CanExecuteAction(startAction), "pallet_robot_reset_restores_operator_start_permissive");
        runtime.UsesExternalClock = false;
        runtime.RunDefault();
        var leftPark = false;
        for (var tick = 0; tick < 3000; tick++)
        {
            runtime.AdvanceSimulation(.02);
            var inputs = runtime.SampleVirtualControllerInputs();
            leftPark |= !inputs["robot_at_park"];
            if (runtime.SampleVirtualControllerNumericInputs()["placed_count"] == 2 && inputs["robot_at_park"]) break;
        }
        check(leftPark && runtime.SampleVirtualControllerNumericInputs()["placed_count"] == 2
            && runtime.SampleVirtualControllerInputs()["robot_at_park"], "pallet_robot_feedback_requires_completed_second_placement_and_park");
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool>
            { ["robot_run"] = false, ["conveyor_run"] = false });
        var held = pallet.Position;
        runtime.AdvanceSimulation(1);
        runtime.AdvanceSimulation(1);
        check(pallet.Position == held && runtime.Points["cycle_complete"] is false,
            "pallet_robot_outbound_waits_for_plc_conveyor_command");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["conveyor_run"] = true });
        runtime.AdvanceSimulation(.2);
        check(pallet.Position.X > held.X + .12f, "pallet_robot_outbound_moves_with_plc_command");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["conveyor_run"] = false });
        held = pallet.Position;
        runtime.AdvanceSimulation(3);
        check(pallet.Position == held, "pallet_robot_command_withdrawal_holds_outbound_pose_and_elapsed_time");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["conveyor_run"] = true });
        for (var tick = 0; tick < 600 && runtime.Points["cycle_complete"] is false; tick++) runtime.AdvanceSimulation(.02);
        check(runtime.SampleVirtualControllerInputs()["cycle_complete"] && MathF.Abs(pallet.Position.X - 5.2f) < .001f,
            "pallet_robot_resumed_outbound_completes_at_authored_endpoint");
        runtime.ResetSimulation();
    }

    private static bool PalletTransferPartsClear(MeshInstance3D a, MeshInstance3D b)
    {
        var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
        if (overlap.X <= 0.001f || overlap.Y <= 0.001f || overlap.Z <= 0.001f || !OrientedBoxesPenetrate(a, b)) return true;
        // Normalize once. Recursive swaps can bounce forever when the curved
        // member is also the cylinder selected as the solid test volume.
        if (b.Mesh is CylinderMesh && a.Mesh is not CylinderMesh) (a, b) = (b, a);
        bool Curved(MeshInstance3D mesh) => mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
            || mesh.Name.ToString().StartsWith("KIN_belt_surface", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("KIN_drive_drum", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("KIN_tail_drum", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("ROBOT_axis", StringComparison.Ordinal)
            || mesh.Name.ToString() is "ROBOT_base" or "ROBOT_axis1_turntable" or "ROBOT_installation_top_plate" or "ROBOT_installation_foot" or "ROBOT_installation_pedestal";
        if (a.Mesh is not CylinderMesh && !Curved(b) && Curved(a)) (a, b) = (b, a);
        if (Curved(b) || a.Mesh is CylinderMesh)
        {
            // Curved belt wraps, round drums and cable routes enclose empty space. Test
            // actual transformed triangles against the other part's bounds.
            var faces = b.Mesh.GetFaces();
            // Screen in the other part's own frame: a rotated pedestal's
            // world box includes empty corners that are not solid material.
            var transform = a.GlobalTransform.AffineInverse() * b.GlobalTransform;
            var bounds = a.GetAabb();
            var basis = a.GlobalBasis;
            var tolerance = new Vector3(0.001f / basis.X.Length(), 0.001f / basis.Y.Length(), 0.001f / basis.Z.Length());
            for (var index = 0; index < faces.Length; index += 3)
            {
                var triangle = new Aabb(transform * faces[index], Vector3.Zero)
                    .Expand(transform * faces[index + 1]).Expand(transform * faces[index + 2]);
                var size = bounds.Intersection(triangle).Size;
                if (size.X > tolerance.X && size.Y > tolerance.Y && size.Z > tolerance.Z)
                {
                    var vertices = new[] { transform * faces[index], transform * faces[index + 1], transform * faces[index + 2] };
                    // Triangle bounds still contain empty corners. Test the
                    // actual face against the shrunken local box using its
                    // face normal and edge/box separating axes.
                    var centered = vertices.Select(point => point - bounds.GetCenter()).ToArray();
                    var half = bounds.Size / 2 - tolerance;
                    var axes = new System.Collections.Generic.List<Vector3> { Vector3.Right, Vector3.Up, Vector3.Back,
                        (centered[1] - centered[0]).Cross(centered[2] - centered[0]) };
                    for (var edge = 0; edge < 3; edge++)
                    foreach (var boxAxis in new[] { Vector3.Right, Vector3.Up, Vector3.Back })
                        axes.Add((centered[(edge + 1) % 3] - centered[edge]).Cross(boxAxis));
                    if (axes.Where(axis => axis.LengthSquared() > 1e-16f).Any(axis =>
                    {
                        var radius = half.X * MathF.Abs(axis.X) + half.Y * MathF.Abs(axis.Y) + half.Z * MathF.Abs(axis.Z);
                        var projections = centered.Select(point => point.Dot(axis)).ToArray();
                        return projections.Min() >= radius || projections.Max() <= -radius;
                    })) continue;
                    if (a.Mesh is CylinderMesh cylinder && Mathf.IsEqualApprox(cylinder.TopRadius, cylinder.BottomRadius))
                    {
                        // A round installation column is not its square local
                        // box. Reject cable triangles outside its actual radius.
                        // Clip the triangle to the cylinder's end planes before
                        // projecting. Otherwise a long slanted triangle can
                        // cross the radius outside the cylinder's actual length.
                        var clipped = new System.Collections.Generic.List<Vector3>
                            { transform * faces[index], transform * faces[index + 1], transform * faces[index + 2] };
                        foreach (var upper in new[] { false, true })
                        {
                            var plane = (upper ? 1 : -1) * (cylinder.Height / 2 - tolerance.Y);
                            var output = new System.Collections.Generic.List<Vector3>();
                            for (var edge = 0; edge < clipped.Count; edge++)
                            {
                                var start = clipped[edge]; var end = clipped[(edge + 1) % clipped.Count];
                                var startIn = upper ? start.Y <= plane : start.Y >= plane;
                                var endIn = upper ? end.Y <= plane : end.Y >= plane;
                                if (startIn) output.Add(start);
                                if (startIn != endIn) output.Add(start.Lerp(end, (plane - start.Y) / (end.Y - start.Y)));
                            }
                            clipped = output;
                        }
                        if (clipped.Count < 3) continue;
                        var points = clipped.Select(point => new Vector2(point.X, point.Z)).ToArray();
                        float EdgeDistance(Vector2 start, Vector2 end)
                        {
                            var direction = end - start;
                            var t = direction.LengthSquared() < 1e-12f ? 0 : Mathf.Clamp(-start.Dot(direction) / direction.LengthSquared(), 0, 1);
                            return (start + direction * t).Length();
                        }
                        var crosses = Enumerable.Range(0, points.Length).Select(edge => points[edge].Cross(points[(edge + 1) % points.Length])).ToArray();
                        var area = crosses.Sum();
                        var inside = MathF.Abs(area) > 1e-8f && (crosses.All(value => value >= 0) || crosses.All(value => value <= 0));
                        var radialDistance = inside ? 0 : Enumerable.Range(0, points.Length).Min(edge => EdgeDistance(points[edge], points[(edge + 1) % points.Length]));
                        if (radialDistance >= cylinder.TopRadius - MathF.Min(tolerance.X, tolerance.Z)) continue;
                    }
                    GD.Print($"PALLET_TRANSFER_CONFLICT {a.Name}/{b.Name} part={bounds} triangle={triangle}"); return false;
                }
            }
            return true;
        }
        GD.Print($"PALLET_TRANSFER_CONFLICT {a.Name}/{b.Name} a={ReviewBounds(a)} b={ReviewBounds(b)}");
        return false;
    }
}
