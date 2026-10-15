using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyToteFinishing;

    private void VerifyToteFinishingOnly()
    {
        var passed = true;
        try
        {
            VerifyToteStartCommand((condition, label) =>
            {
                passed &= condition;
                GD.Print($"TOTE_START_CHECK {label}={condition}");
            });
            if (!passed) { GetTree().Quit(1); return; }
            VerifyToteFill((condition, label) =>
            {
                passed &= condition;
                GD.Print($"TOTE_FILL_CHECK {label}={condition}");
            });
            if (!passed) { GetTree().Quit(1); return; }
            VerifyToteCap((condition, label) =>
            {
                passed &= condition;
                GD.Print($"TOTE_CAP_CHECK {label}={condition}");
            });
            if (!passed) { GetTree().Quit(1); return; }
            VerifyToteFillCapLadder((condition, label) =>
            {
                passed &= condition;
                GD.Print($"TOTE_LADDER_CHECK {label}={condition}");
            });
            if (!passed) { GetTree().Quit(1); return; }
            VerifyToteCameraFollow((condition, label) =>
            {
                passed &= condition;
                GD.Print($"TOTE_CAMERA_CHECK {label}={condition}");
            });
            if (!passed) { GetTree().Quit(1); return; }
            VerifyToteFinishingPlacement((condition, label) =>
            {
                passed &= condition;
                GD.Print($"TOTE_FINISHING_CHECK {label}={condition}");
            });
        }
        catch (Exception error)
        {
            passed = false;
            GD.PushError($"TOTE_FINISHING_EXCEPTION {error}");
        }
        GD.Print($"TOTE_FINISHING_VERIFY {(passed ? "PASS" : "FAIL")} Start, travel, normalized fill, axial/seated cap and standalone preview; label/inspection/native review pending");
        GetTree().Quit(passed ? 0 : 1);
    }

    private void VerifyToteCameraFollow(Action<bool, string> check)
    {
        var previousReview = _visualSceneReview;
        var previousFocus = _visualReviewFocusId;
        try
        {
            _visualSceneReview = true;
            foreach (var direction in new[] { new Vector3(11, 7, 12), new Vector3(-11, 7, 12),
                         new Vector3(-11, 7, -12), new Vector3(11, 7, -12), new Vector3(0, 20, .01f) })
            {
                AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
                _visualReviewFocusId = "finishing_tote";
                SetVisualReviewAngle(direction, "tote-follow-regression");
                var tote = _sceneCompositionRoot!.GetNode<Node3D>("finishing_tote");
                var runtime = _sceneRuntime!;
                runtime.UsesExternalClock = true;
                runtime.SetControllerPlaybackRunning(true);
                runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_run"] = true });
                var follows = true;
                var initialRotation = _mainCamera!.GlobalBasis;
                for (var step = 0; step < 850; step++)
                {
                    var beforeCenter = ReviewBounds(tote).GetCenter();
                    var beforeCamera = _mainCamera.Position;
                    var beforeTarget = _cameraController!.ViewTarget;
                    runtime.AdvanceSimulation(.02);
                    FollowVisualReviewCarton();
                    var travel = ReviewBounds(tote).GetCenter() - beforeCenter;
                    follows &= (_mainCamera.Position - beforeCamera).IsEqualApprox(travel)
                        && (_cameraController.ViewTarget - beforeTarget).IsEqualApprox(travel)
                        && _mainCamera.GlobalBasis.IsEqualApprox(initialRotation);
                }
                check(follows && runtime.Points["tote_at_exit"] is true,
                    $"full_travel_preserves_camera_offset_target_and_rotation_{direction}");
            }
        }
        finally
        {
            _visualSceneReview = previousReview;
            _visualReviewFocusId = previousFocus;
            _visualReviewFollowCenter = null;
        }
    }
    private void VerifyToteFinishingPlacement(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var tote = root.GetNode<Node3D>("finishing_tote");
        var belt = ReviewBounds(ReviewMeshes(root.GetNode<Node3D>("finishing_conveyor"))
            .Single(mesh => mesh.Name.ToString().Contains("BELT_surface", StringComparison.OrdinalIgnoreCase)));
        foreach (var node in root.GetChildren().OfType<Node3D>().Where(node => node.Name != "finishing_conveyor"))
            foreach (var mesh in ReviewMeshes(node))
                GD.Print($"TOTE_FINISHING_MESH {node.Name}/{mesh.Name} {ReviewBounds(mesh)}");
        var solids = root.GetChildren().OfType<Node3D>().Where(node => node != tote
            && node.Name != "finishing_conveyor").SelectMany(ReviewMeshes).ToArray();
        var initial = tote.Transform;
        check(tote.FindChild("IBC_open_fill_neck", true, false) is MeshInstance3D
            && tote.FindChild("IBC_fill_cap", true, false) is MeshInstance3D cap && !cap.Visible,
            "tote_finishing_open_port_candidate_has_separate_initially_hidden_cap");
        // A frame has one transform image. Avoid recomputing the same mesh's
        // eight transformed corners for every possible component pair.
        var boundsCache = new Dictionary<MeshInstance3D, Aabb>();
        Aabb Bounds(MeshInstance3D mesh)
        {
            if (!boundsCache.TryGetValue(mesh, out var bounds))
                boundsCache[mesh] = bounds = ReviewBounds(mesh);
            return bounds;
        }
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = Bounds(a).Intersection(Bounds(b)).Size;
            if (overlap.X <= 0.002f || overlap.Y <= 0.002f || overlap.Z <= 0.002f) return true;
            if (!OrientedBoxesPenetrate(a, b)) return true;
            // A hollow neck's enclosing box includes its empty bore. Accept
            // the fill tool only when every delivered vertex fits within the
            // measured inner meridians with 2 mm radial clearance. All other
            // neck contacts continue through the ordinary collision screen.
            var neck = a.Name == "IBC_open_fill_neck" ? a : b.Name == "IBC_open_fill_neck" ? b : null;
            if (neck is not null)
            {
                var tool = neck == a ? b : a;
                if (tool.Name.ToString() is "NOZZLE_TIP" or "KIN_fill_nozzle")
                {
                    var center = Bounds(neck).GetCenter();
                    var vertices = neck.Mesh.GetFaces().Select(vertex => neck.GlobalTransform * vertex).ToArray();
                    var xMeridian = vertices.Where(vertex => MathF.Abs(vertex.Z - center.Z) < .00001f
                        && MathF.Abs(vertex.X - center.X) > .00001f).Select(vertex => MathF.Abs(vertex.X - center.X)).ToArray();
                    var zMeridian = vertices.Where(vertex => MathF.Abs(vertex.X - center.X) < .00001f
                        && MathF.Abs(vertex.Z - center.Z) > .00001f).Select(vertex => MathF.Abs(vertex.Z - center.Z)).ToArray();
                    if (xMeridian.Length > 0 && zMeridian.Length > 0)
                    {
                        var radiusX = xMeridian.Min() - .002f;
                        var radiusZ = zMeridian.Min() - .002f;
                        if (radiusX > 0 && radiusZ > 0 && tool.Mesh.GetFaces().All(vertex =>
                        {
                            var world = tool.GlobalTransform * vertex;
                            var x = (world.X - center.X) / radiusX;
                            var z = (world.Z - center.Z) / radiusZ;
                            return x*x + z*z < 1;
                        })) return true;
                    }
                }
            }
            // A cable's overall route box includes empty space. Screen its
            // transformed surface triangles rather than treating it as solid.
            var cable = a.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase) ? a
                : b.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase) ? b : null;
            if (cable is not null)
            {
                var other = cable == a ? b : a;
                var transform = other.GlobalTransform.AffineInverse() * cable.GlobalTransform;
                var faces = cable.Mesh.GetFaces();
                var bounds = other.GetAabb();
                var padding = new Vector3(0.002f / other.GlobalTransform.Basis.X.Length(),
                    0.002f / other.GlobalTransform.Basis.Y.Length(), 0.002f / other.GlobalTransform.Basis.Z.Length());
                bounds = new Aabb(bounds.Position + padding, bounds.Size - padding * 2);
                var candidate = false;
                for (var index = 0; index < faces.Length && !candidate; index += 3)
                    candidate = new Aabb(transform * faces[index], Vector3.Zero)
                        .Expand(transform * faces[index + 1]).Expand(transform * faces[index + 2])
                        .Grow(0.0001f).Intersects(bounds);
                if (!candidate) return true;
            }
            GD.Print($"TOTE_FINISHING_CLEARANCE {a.Name}/{b.Name} a={ReviewBounds(a)} b={ReviewBounds(b)}");
            return false;
        }
        var conveyorParts = ReviewMeshes(root.GetNode<Node3D>("finishing_conveyor"));
        var stationsClear = true;
        foreach (var part in solids)
        foreach (var other in conveyorParts) stationsClear &= Clear(part, other);
        check(stationsClear, "tote_finishing_station_solids_clear_conveyor_and_its_cables");
        var supported = true;
        var clear = true;
        var conveyorClear = true;
        for (var sample = 0; sample <= 244; sample++)
        {
            tote.Position = new Vector3(-5.8f + sample * 0.05f, initial.Origin.Y, initial.Origin.Z);
            boundsCache.Clear();
            var bounds = ReviewBounds(tote);
            supported &= MathF.Abs(bounds.Position.Y - belt.End.Y) < 0.002f
                && bounds.Position.X >= belt.Position.X - 0.002f && bounds.End.X <= belt.End.X + 0.002f
                && bounds.Position.Z >= belt.Position.Z - 0.002f && bounds.End.Z <= belt.End.Z + 0.002f;
            clear &= ReviewMeshes(tote).All(part => solids.All(other => Clear(part, other)));
            conveyorClear &= ReviewMeshes(tote).All(part => conveyorParts.All(other => Clear(part, other)));
        }
        tote.Transform = initial;
        check(supported, "tote_finishing_full_route_has_belt_contact_and_full_footprint");
        check(clear, "tote_finishing_full_route_clear_of_station_solids");
        check(conveyorClear, "tote_finishing_full_route_clear_of_conveyor_motor_rails_and_controls");

        var filler = root.GetNode<Node3D>("finishing_fill_valve");
        var capper = root.GetNode<Node3D>("capper");
        var vision = root.GetNode<Node3D>("vision_inspector");
        MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
        var supports = new[]
        {
            (Part(filler, "FILLER_COLUMN"), Part(filler, "FILLER_BASE")),
            (Part(capper, "CAPPER_COLUMN"), Part(capper, "CAPPER_BASE")),
            (Part(root.GetNode<Node3D>("labeler"), "LABELER_PEDESTAL"), Part(root.GetNode<Node3D>("labeler"), "LABELER_BASE")),
            (Part(vision, "VISION_ARCH_POST_-0_72"), Part(vision, "VISION_BASE")),
            (Part(vision, "VISION_ARCH_POST_0_72"), Part(vision, "FINISHING_vision_front_foot")),
        };
        check(supports.All(pair => MathF.Abs(ReviewBounds(pair.Item2).Position.Y) < 0.001f
            && ReviewBounds(pair.Item2).Grow(0.002f).Intersects(ReviewBounds(pair.Item1))),
            "tote_finishing_five_station_columns_have_grounded_bearing_feet");
        check(ReviewBounds(Part(filler, "FILLER_COLUMN")).Grow(0.001f).Intersects(ReviewBounds(Part(filler, "NOZZLE_BOOM")))
            && ReviewBounds(Part(filler, "NOZZLE_BOOM")).Intersects(ReviewBounds(Part(filler, "DOSING_VALVE_BLOCK")))
            && ReviewBounds(Part(capper, "CAPPER_COLUMN")).Intersects(ReviewBounds(Part(capper, "CAPPER_CANTILEVER")))
            && ReviewBounds(Part(capper, "CAPPER_CANTILEVER")).Intersects(ReviewBounds(Part(capper, "CAPPER_HEAD")))
            && supports.Skip(3).All(pair => ReviewBounds(pair.Item1).Intersects(ReviewBounds(Part(vision, "VISION_ARCH_HEADER")))),
            "tote_finishing_overhead_heads_and_arch_connect_to_floor_columns");

        var heldCap = Part(capper, "CAP_UNDER_CHUCK");
        var toteCap = Part(tote, "IBC_fill_cap");
        check(heldCap.Mesh.GetFaces().Length == toteCap.Mesh.GetFaces().Length
            && ReviewBounds(heldCap).Size.IsEqualApprox(ReviewBounds(toteCap).Size),
            "tote_finishing_held_cap_matches_delivered_tote_cap_geometry_and_size");
        check(MathF.Abs(ReviewBounds(heldCap).End.Y - ReviewBounds(Part(capper, "TORQUE_CHUCK")).Position.Y) < .001f,
            "tote_finishing_held_cap_roof_contacts_chuck_without_axial_overlap");

        var neckPart = Part(tote, "IBC_open_fill_neck");
        var neckSize = ReviewBounds(neckPart).Size;
        var capSize = ReviewBounds(toteCap).Size;
        check(MathF.Abs(neckSize.X - neckSize.Z) < .0001f
            && MathF.Abs(capSize.X - capSize.Z) < .0001f,
            "tote_finishing_actual_installed_neck_and_cap_remain_circular");
        // Inspect delivered wall vertices through a quarter-turn, not just the
        // catalog dimensions. This is radial clearance, not a thread/seal test.
        var heldHome = heldCap.GlobalTransform;
        var capBounds = ReviewBounds(heldCap);
        var capCenter = capBounds.GetCenter();
        var neckCenter = ReviewBounds(neckPart).GetCenter();
        float Radius(Vector3 vertex, Vector3 center) => new Vector2(vertex.X-center.X, vertex.Z-center.Z).Length();
        var neckRadius = neckPart.Mesh.GetFaces().Max(vertex => Radius(neckPart.GlobalTransform * vertex, neckCenter));
        var radialClear = true;
        foreach (var angle in new[] { 0f, Mathf.Pi / 2 })
        {
            var turn = new Transform3D(new Basis(Vector3.Up, angle), Vector3.Zero);
            heldCap.GlobalTransform = new Transform3D(Basis.Identity, capCenter) * turn
                * new Transform3D(Basis.Identity, -capCenter) * heldHome;
            var lowerWall = heldCap.Mesh.GetFaces().Select(vertex => heldCap.GlobalTransform * vertex)
                .Where(vertex => vertex.Y < capBounds.Position.Y + capBounds.Size.Y * .35f).ToArray();
            radialClear &= lowerWall.Length > 0 && lowerWall.Min(vertex => Radius(vertex, capCenter)) > neckRadius + .002f;
        }
        heldCap.GlobalTransform = heldHome;
        check(radialClear, "tote_finishing_actual_held_cap_lower_wall_clears_neck_at_home_and_quarter_turn");

        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = false; // Explicit standalone preview, not a controller-owned lesson.
        var controllers = root.FindChildren("*", "", true, false).OfType<EquipmentMotionController>().ToArray();
        var nozzle = Part(filler, "KIN_fill_nozzle");
        var tip = Part(filler, "NOZZLE_TIP");
        var spindle = Part(capper, "KIN_capper_spindle");
        var chuck = Part(capper, "TORQUE_CHUCK");
        var tipLocal = tip.Transform;
        var chuckLocal = chuck.Transform;
        var chuckInitialBasis = chuck.GlobalBasis;
        var nozzleStart = nozzle.Position;
        var sampleSupported = true;
        var sampleClear = true;
        var followers = true;
        var nozzleTravel = false;
        var chuckRotated = false;
        var allActuatorsObserved = new bool[4];
        runtime.ResetSimulation();
        check(runtime.ExecuteAction("start-line"), "tote_finishing_declared_preview_start_is_accepted");
        for (var tick = 0; tick < 5000; tick++)
        {
            runtime.AdvanceSimulation(0.002);
            foreach (var controller in controllers) controller._PhysicsProcess(0.002);
            boundsCache.Clear();
            var bounds = ReviewBounds(tote);
            sampleSupported &= MathF.Abs(bounds.Position.Y - belt.End.Y) < 0.002f
                && bounds.Position.X >= belt.Position.X - 0.002f && bounds.End.X <= belt.End.X + 0.002f
                && bounds.Position.Z >= belt.Position.Z - 0.002f && bounds.End.Z <= belt.End.Z + 0.002f;
            // Transparent liquid is a visualization, not a stationary solid.
            var stationSolids = root.GetChildren().OfType<Node3D>().Where(node => node != tote
                && node.Name != "finishing_conveyor").SelectMany(ReviewMeshes)
                .Where(part => part.Name != "FINISHING_stream").ToArray();
            sampleClear &= ReviewMeshes(tote).All(part => stationSolids.All(other => Clear(part, other)));
            followers &= tip.GetParent() == nozzle && tip.Transform.IsEqualApprox(tipLocal)
                && chuck.GetParent() == spindle && chuck.Transform.IsEqualApprox(chuckLocal);
            nozzleTravel |= MathF.Abs(nozzle.Position.Y - nozzleStart.Y + 0.14f) < 0.001f;
            chuckRotated |= !chuck.GlobalBasis.IsEqualApprox(chuckInitialBasis);
            var commands = new[] { "fill_valve_open", "capper_run", "labeler_run", "inspection_run" };
            for (var index = 0; index < commands.Length; index++) allActuatorsObserved[index] |= runtime.Points[commands[index]] is true;
        }
        check(sampleSupported && MathF.Abs(tote.Position.X - 6.4f) < 0.001f,
            "tote_finishing_actual_preview_has_supported_discharge_through_5000_two_ms_ticks");
        check(sampleClear, "tote_finishing_actual_preview_clear_through_all_station_commands_and_travel");
        check(followers && nozzleTravel && chuckRotated && allActuatorsObserved.All(value => value),
            "tote_finishing_nozzle_tip_and_rotating_chuck_follow_their_actuators");
        check(runtime.Points["cycle_complete"] is true && new[] { "conveyor_run", "fill_valve_open", "capper_run", "labeler_run", "inspection_run" }
            .All(point => runtime.Points[point] is false), "tote_finishing_declared_preview_completes_with_commands_off");
        runtime.ResetSimulation();
        check(tote.Transform.IsEqualApprox(initial) && tip.Transform.IsEqualApprox(tipLocal)
            && chuck.Transform.IsEqualApprox(chuckLocal) && nozzle.Position.IsEqualApprox(nozzleStart),
            "tote_finishing_reset_restores_supported_load_and_attached_tools");
    }
}
