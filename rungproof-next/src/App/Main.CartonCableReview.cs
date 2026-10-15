using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyCartonStaticRoutes;

    private async void VerifyCartonStaticRoutesOnly()
    {
        var passed = true;
        void Check(bool condition, string label)
        {
            passed &= condition;
            GD.Print($"CARTON_STATIC_CHECK {label}={condition}");
        }
        try
        {
            // Headless windows start at a tiny dummy size. Screen-space
            // assertions require the same usable shell layout as native QA.
            GetWindow().Size = new Vector2I(1600, 900);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            VerifyCartonStaticCableRoutes(Check);
            VerifyInclinedPhotoeyeGeometry(Check);
            VerifyVisionSorterCartonSupport(Check);
            VerifyVisionSorterRuntime(Check);
            VerifyMobileTrafficLampChannels(Check);
        }
        catch (Exception error)
        {
            passed = false;
            GD.PushError($"CARTON_STATIC_CHECK exception: {error}");
        }
        GetTree().Quit(passed ? 0 : 1);
    }

    private void VerifyMobileTrafficLampChannels(Action<bool, string> check)
    {
        AddMigratedScene("lab-11-12-mobile-traffic-lights", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        var colors = new[] { "red", "amber", "green" };
        var heads = new[] { _sceneCompositionRoot!.GetNode<Node3D>("indicator_0"),
            _sceneCompositionRoot.GetNode<Node3D>("indicator_1") };
        var lamps = heads.SelectMany(head => colors.Select(color => ReviewMeshes(head)
            .Where(mesh => mesh.Name.ToString().StartsWith("LENS_" + color, StringComparison.OrdinalIgnoreCase)).ToArray())).ToArray();
        bool Lit(int channel) => lamps[channel].Length > 0 && lamps[channel].All(mesh =>
            mesh.MaterialOverride is StandardMaterial3D { EmissionEnabled: true });
        var names = new[] { "road_a_red", "road_a_amber", "road_a_green", "road_b_red", "road_b_amber", "road_b_green" };
        check(lamps.All(group => group.Length > 0) && names.All(name => runtime.Points[name] is false)
            && Enumerable.Range(0, 6).All(channel => !Lit(channel)), "traffic_six_present_channels_initially_dark");
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        // Exhaust every command combination, including invalid simultaneous
        // colors. The renderer must expose, not silently correct, ladder bugs.
        var accurate = true;
        for (var mask = 0; mask < 64; mask++)
        {
            var outputs = names.Select((name, channel) => (name, active: (mask & (1 << channel)) != 0))
                .ToDictionary(item => item.name, item => item.active);
            runtime.CommitVirtualControllerOutputs(outputs);
            accurate &= Enumerable.Range(0, 6).All(channel => Lit(channel) == outputs[names[channel]]);
        }
        check(accurate, "traffic_all_64_command_images_match_six_lenses_including_conflicts");
        runtime.SetControllerPlaybackRunning(false);
        runtime.CommitVirtualControllerOutputs(names.ToDictionary(name => name, _ => false));
        check(Enumerable.Range(0, 6).All(channel => !Lit(channel)), "traffic_stopped_zero_commands_dark");
        runtime.ResetSimulation();
        check(names.All(name => runtime.Points[name] is false) && Enumerable.Range(0, 6).All(channel => !Lit(channel)),
            "traffic_reset_commands_and_lenses_dark");
    }

    private void VerifyVisionSorterRuntime(Action<bool, string> check)
    {
        for (var lane = 1; lane <= 4; lane++)
        {
            AddMigratedScene("lab-10-03-vision-package-sorter", _candidateCatalog!, _mainCamera!, false, false);
            var runtime = _sceneRuntime!;
            var carton = _sceneCompositionRoot!.GetNode<Node3D>("box_1");
            var previousReviewMode = _visualSceneReview;
            _visualSceneReview = true;
            _visualReviewFocusId = "box_1";
            SetVisualReviewAngle(new Vector3(0, 20, .01f), "sorter-top-follow-regression");
            var follows = true;
            var cartonMeshes = ReviewMeshes(carton);
            var topFrameFits = true;
            void ScreenTopFrame()
            {
                var viewport = _mainCamera!.GetViewport().GetVisibleRect();
                var aperture = _simulatorShell?.SceneViewportRect() ?? viewport;
                // The review bar overlays the top of the scene aperture.
                // Require the entire rendered carton below it, not just its center.
                var safeTop = aperture.Position.Y + 64;
                foreach (var mesh in cartonMeshes)
                for (var corner = 0; corner < 8; corner++)
                {
                    var box = mesh.GetAabb();
                    var world = mesh.GlobalTransform * (box.Position + box.Size * new Vector3(
                        (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1));
                    var screen = _mainCamera.UnprojectPosition(world);
                    var fits = !_mainCamera.IsPositionBehind(world)
                        && screen.X >= aperture.Position.X + 12 && screen.X <= aperture.End.X - 12
                        && screen.Y >= safeTop && screen.Y <= aperture.End.Y - 12;
                    if (topFrameFits && !fits)
                        GD.Print($"SORTER_TOP_FRAME_OUTSIDE lane={lane} screen={screen} aperture={aperture} camera={_mainCamera.Position} carton={carton.Position}");
                    topFrameFits &= fits;
                }
            }
            ScreenTopFrame();
            // Dashed photoeye rays are optical annotations, not solids. All
            // housings, drums, rails, decks and cable meshes remain included.
            var obstacles = ReviewMeshes(_sceneCompositionRoot).Where(mesh => !carton.IsAncestorOf(mesh)
                && !mesh.Name.ToString().StartsWith("KIN_beam_dash_", StringComparison.Ordinal)).ToArray();
            var motionClear = cartonMeshes.Length > 0 && obstacles.Length > 0;
            var interferenceCount = 0;
            // Bounds enclose the rendered meshes: this is a conservative pose
            // screen, not a swept-volume/contact-force simulation. Each scan
            // moves at most 9 mm; the index advances at most 0.9 degrees.
            void ScreenCarton()
            {
                foreach (var part in cartonMeshes)
                foreach (var obstacle in obstacles)
                {
                    var overlap = ReviewBounds(part).Intersection(ReviewBounds(obstacle)).Size;
                    if (overlap.X <= .001f || overlap.Y <= .001f || overlap.Z <= .001f
                        || !OrientedBoxesPenetrate(part, obstacle, .001f)) continue;
                    // Refine cylinder/curved-cable empty bounds against actual
                    // faces in the carton mesh's local box. Convert the world
                    // allowance per axis so scaled carton meshes remain valid.
                    var basis = part.GlobalBasis;
                    var padding = new Vector3(.001f / basis.X.Length(), .001f / basis.Y.Length(), .001f / basis.Z.Length());
                    var box = part.GetAabb();
                    var interior = new Aabb(box.Position + padding, box.Size - padding * 2);
                    if (!RouteTriangleEntersBox(obstacle, interior, part.GlobalTransform.AffineInverse())) continue;
                    motionClear = false;
                    if (interferenceCount++ < 8)
                        GD.Print($"VISION_SORTER_MOVING_INTERFERENCE lane={lane} carton={part.Name} obstacle={obstacle.GetPath()} position={carton.Position}");
                }
            }
            ScreenCarton();
            for (var selection = 1; selection < lane; selection++) runtime.ExecuteAction("cycle-vision_class");
            runtime.ExecuteAction("toggle-package_present");
            runtime.ExecuteAction("toggle-vision_result_valid");
            runtime.ExecuteAction("toggle-destination_clear");
            runtime.UsesExternalClock = true;
            runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool>
            { ["sort_conveyor_run"] = true, ["diverter_enable"] = true });
            runtime.AdvanceSimulation(.02);
            FollowVisualReviewCarton();
            ScreenTopFrame();
            ScreenCarton();
            var latched = Convert.ToInt32(runtime.Points["sort_latched_class"]);
            runtime.ExecuteAction("cycle-vision_class");
            var continuous = true;
            var scans = 0;
            while (runtime.Points["sort_complete"] is not true && scans++ < 2000)
            {
                var before = carton.Position;
                var beforeCenter = ReviewBounds(carton).GetCenter();
                var beforeCamera = _mainCamera!.Position;
                var beforeTarget = _cameraController!.ViewTarget;
                runtime.AdvanceSimulation(.02);
                FollowVisualReviewCarton();
                ScreenTopFrame();
                var travel = ReviewBounds(carton).GetCenter() - beforeCenter;
                follows &= (_mainCamera.Position - beforeCamera).IsEqualApprox(travel)
                    && (_cameraController.ViewTarget - beforeTarget).IsEqualApprox(travel);
                ScreenCarton();
                continuous &= carton.Position.DistanceTo(before) <= .00901f;
            }
            var target = _sceneCompositionRoot.GetNode<Node3D>($"destination_lane_{lane}");
            check(follows, $"vision_sorter_lane_{lane}_camera_follows_rendered_center_without_zoom_change");
            check(topFrameFits, $"vision_sorter_lane_{lane}_top_carton_corners_clear_toolbar_and_aperture_each_scan");
            var belt = (MeshInstance3D)target.FindChild("KIN_belt_surface", true, false);
            var load = ReviewBounds(carton);
            var support = ReviewBounds(belt);
            check(motionClear, $"vision_sorter_runtime_lane_{lane}_carton_triangle_pose_screen_one_mm_allowance");
            check(runtime.Points["sort_complete"] is true && latched == lane
                && Convert.ToInt32(runtime.Points["sort_latched_class"]) == lane && continuous,
                $"vision_sorter_runtime_lane_{lane}_continuous_complete_retains_latched_class");
            check(MathF.Abs(load.Position.Y-support.End.Y) < .001f && load.Intersects(support.Grow(.001f)),
                $"vision_sorter_runtime_lane_{lane}_endpoint_bearing_height_and_broad_overlap");
            var endpoint = carton.Transform;
            runtime.SetControllerPlaybackRunning(false);
            runtime.AdvanceSimulation(.02);
            check(carton.Transform == endpoint && carton.Visible,
                $"vision_sorter_runtime_lane_{lane}_stopped_retains_visible_endpoint");
            runtime.ResetSimulation();
            ReframeResetReviewFocus();
            check(_cameraController!.ViewTarget.IsEqualApprox(ReviewBounds(carton).GetCenter()),
                $"vision_sorter_lane_{lane}_reset_refocuses_home_carton");
            _visualSceneReview = previousReviewMode;
            _visualReviewFocusId = null;
            _visualReviewFollowCenter = null;
            check(MathF.Abs(carton.Position.X + 1.1f) < .00001f && carton.Position.Z == 0
                && runtime.Points["sort_complete"] is false && Convert.ToInt32(runtime.Points["sort_latched_class"]) == 0,
                $"vision_sorter_runtime_lane_{lane}_reset_home_and_feedback");
        }
    }

    private void VerifyVisionSorterCartonSupport(Action<bool, string> check)
    {
        AddMigratedScene("lab-10-03-vision-package-sorter", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var display = root.GetNode<Node3D>("training_accessory_6");
        check(display.FindChild("COUNT_DISPLAY_screen", true, false) is MeshInstance3D
            && display.FindChild("COUNT_DISPLAY_base", true, false) is MeshInstance3D,
            "vision_sorter_class_display_has_actual_supported_display_mesh_identity");
        var foot = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
        var mast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
        var housing = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
        check(MathF.Abs(ReviewBounds(foot).Position.Y) < .001f
            && ReviewBounds(foot).Grow(.001f).Intersects(ReviewBounds(mast))
            && ReviewBounds(mast).Grow(.001f).Intersects(ReviewBounds(housing)),
            "vision_sorter_display_has_grounded_base_and_attached_mast_housing");
        check(display.FindChild("NumericReadout", true, false) is Label3D label && label.Text == "SIM CLASS\n—"
            && display.FindChild("COUNT_DISPLAY_static_legend", true, false) is MeshInstance3D legend && !legend.Visible,
            "vision_sorter_display_states_no_result_and_hides_stale_count_legend");
        var carton = ReviewBounds(root.GetNode<Node3D>("box_1"));
        var surface = (MeshInstance3D)root.GetNode<Node3D>("conveyor_0").FindChild("KIN_belt_surface", true, false);
        var belt = ReviewBounds(surface);
        var table = root.GetNode<Node3D>("rotaryTable_3");
        var platter = (MeshInstance3D)table.FindChild("TABLE_rotating_platter", true, false);
        check(!ReviewMeshes(table).Any(mesh => mesh.Name.ToString().StartsWith("TABLE_fixture_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("TABLE_index_", StringComparison.Ordinal) || mesh.Name == "TABLE_center_register"),
            "vision_sorter_parcel_platter_has_no_visible_machining_fixture_or_raised_index_markers");
        check(MathF.Abs(ReviewBounds(platter).End.Y - belt.End.Y) < .001f,
            "vision_sorter_parcel_platter_matches_actual_infeed_bearing_plane");
        var lanes=root.GetChildren().OfType<Node3D>().Where(n=>n.Name.ToString().StartsWith("destination_lane_",StringComparison.Ordinal)).ToArray();
        check(lanes.Length==4 && lanes.All(l=>l.FindChild("KIN_belt_surface",true,false) is MeshInstance3D),
            "vision_sorter_has_four_separate_actual_receiving_conveyor_decks");
        check(lanes.Length==4 && lanes.All(l=>MathF.Abs(ReviewBounds((MeshInstance3D)l.FindChild("KIN_belt_surface",true,false)).End.Y-belt.End.Y)<.001f),
            "vision_sorter_four_receiving_decks_match_infeed_and_platter_height");
        var laneParts=lanes.Select(l=>ReviewMeshes(l).Select(m=>(Mesh:m,Bounds:ReviewBounds(m))).ToArray()).ToArray();
        var lanesClear=true;
        for(var left=0;left<laneParts.Length;left++)
        for(var right=left+1;right<laneParts.Length;right++)
        foreach(var aPart in laneParts[left])
        foreach(var bPart in laneParts[right])
        {
            var overlap=aPart.Bounds.Intersection(bPart.Bounds).Size;
            if(overlap.X<=.001f || overlap.Y<=.001f || overlap.Z<=.001f || !OrientedBoxesPenetrate(aPart.Mesh,bPart.Mesh))continue;
            GD.Print($"VISION_SORTER_LANE_INTERFERENCE {lanes[left].Name}/{aPart.Mesh.Name} {lanes[right].Name}/{bPart.Mesh.Name}");
            lanesClear=false;
        }
        check(lanes.Length==4 && lanesClear,"vision_sorter_four_receiving_conveyors_clear_each_other_at_one_mm_obb_screen");
        var controlsClear=true;
        foreach(var control in root.GetChildren().OfType<Node3D>().Where(n=>n.Name.ToString().StartsWith("switch_",StringComparison.Ordinal)))
        foreach(var part in ReviewMeshes(control))
        foreach(var lane in lanes)
        foreach(var other in ReviewMeshes(lane))
        {
            var overlap=ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
            if(overlap.X<=.001f || overlap.Y<=.001f || overlap.Z<=.001f || !OrientedBoxesPenetrate(part,other))continue;
            GD.Print($"VISION_SORTER_OPERATOR_INTERFERENCE {control.Name}/{part.Name} {lane.Name}/{other.Name} overlap={overlap}");
            controlsClear=false;
        }
        check(controlsClear,"vision_sorter_operator_controls_clear_all_four_receiving_conveyors_at_one_mm_obb_screen");
        foreach(var lane in lanes)
        {
            var laneDeck=(MeshInstance3D)lane.FindChild("KIN_belt_surface",true,false);
            var laneTop=ReviewBounds(laneDeck).End.Y;
            var localBearingStart=laneDeck.Mesh.GetFaces().Select(v=>laneDeck.GlobalTransform*v)
                .Where(v=>MathF.Abs(v.Y-laneTop)<.002f).Min(v=>lane.ToLocal(v).X);
            var localNoseStart=laneDeck.Mesh.GetFaces().Select(v=>lane.ToLocal(laneDeck.GlobalTransform*v)).Min(v=>v.X);
            GD.Print($"VISION_SORTER_RECEIVING_LANE {lane.Name} {ReviewBounds(laneDeck)} localBearingStart={localBearingStart} localNoseStart={localNoseStart}");
        }
        var receiver = root.GetNodeOrNull<Node3D>("destination_lane_1") ?? root.GetNode<Node3D>("training_accessory_8");
        var receivingBelt = (MeshInstance3D)receiver.FindChild("KIN_belt_surface", true, false);
        GD.Print($"VISION_SORTER_RECEIVING infeed={belt} platter={ReviewBounds(platter)} bank={ReviewBounds(receivingBelt)}");
        GD.Print($"VISION_SORTER_RECEIVING_GAP x={ReviewBounds(platter).Position.X-belt.End.X} deckDelta={ReviewBounds(platter).End.Y-belt.End.Y} bankBeltCount={receiver.FindChildren("KIN_belt_surface", "MeshInstance3D", true, false).Count}");
        var bridge = root.GetNode<Node3D>("sorter_handoff_bridge");
        var deck = (MeshInstance3D)bridge.FindChild("SORTER_handoff_deck", true, false);
        check(MathF.Abs(ReviewBounds(deck).End.Y-belt.End.Y)<.001f,
            "vision_sorter_handoff_bridge_matches_infeed_and_platter_height");
        var feet=ReviewMeshes(bridge).Where(m=>m.Name.ToString().StartsWith("SORTER_bridge_foot_",StringComparison.Ordinal)).ToArray();
        var legs=ReviewMeshes(bridge).Where(m=>m.Name.ToString().StartsWith("SORTER_bridge_leg_",StringComparison.Ordinal)).ToArray();
        check(feet.Length==4 && legs.Length==4 && feet.All(f=>MathF.Abs(ReviewBounds(f).Position.Y)<.001f)
            && legs.All(l=>feet.Any(f=>ReviewBounds(f).Grow(.001f).Intersects(ReviewBounds(l)))
                && ReviewBounds(l).Grow(.001f).Intersects(ReviewBounds(deck))),
            "vision_sorter_handoff_has_four_grounded_attached_supports");
        var bridgeClear=true;var candidates=0;
        foreach(var part in ReviewMeshes(bridge))
        foreach(var peer in root.GetChildren().OfType<Node3D>().Where(node=>node!=bridge))
        foreach(var other in ReviewMeshes(peer))
        {
            // These two interfaces need a separate shaped-nose/contact test;
            // their enclosing boxes deliberately overlap the handoff boundary.
            if(part==deck && (other==surface || other==platter))continue;
            var overlap=ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
            if(overlap.X<=.001f || overlap.Y<=.001f || overlap.Z<=.001f || !OrientedBoxesPenetrate(part,other))continue;
            if(part==deck && other.Name=="KIN_drive_drum" && VisionSorterContactProfileClear(deck,other))continue;
            candidates++;
            if(candidates<=8) GD.Print($"VISION_SORTER_BRIDGE_INTERFERENCE {part.Name}/{peer.Name}/{other.Name} overlap={overlap}");
            bridgeClear=false;
        }
        check(bridgeClear,"vision_sorter_bridge_other_equipment_clear_at_one_mm_obb_screen_excluding_two_mating_surfaces");
        check(VisionSorterContactProfileClear(deck,surface),
            "vision_sorter_bridge_clears_actual_belt_nose_profile_with_one_mm_allowance");
        check(VisionSorterContactProfileClear(deck,platter,true),
            "vision_sorter_curved_bridge_nose_clears_actual_platter_profile_with_one_mm_allowance");
        var drum=(MeshInstance3D)root.GetNode<Node3D>("conveyor_0").FindChild("KIN_drive_drum",true,false);
        var deckHome=deck.Transform;
        var detected=false;
        GD.Print("VISION_SORTER_DRUM_PROFILE_NEGATIVE_CONTROL_BEGIN lowered_deck_mm=50");
        try { deck.Position+=Vector3.Down*.05f;detected=!VisionSorterContactProfileClear(deck,drum); }
        finally { deck.Transform=deckHome; }
        GD.Print("VISION_SORTER_DRUM_PROFILE_NEGATIVE_CONTROL_END restored=True");
        check(detected,"vision_sorter_drum_profile_screen_detects_deliberately_lowered_deck");
        var beltProbe=false;var platterProbe=false;
        GD.Print("VISION_SORTER_MATING_NEGATIVE_CONTROLS_BEGIN");
        try
        {
            deck.Position+=Vector3.Down*.05f;
            beltProbe=!VisionSorterContactProfileClear(deck,surface);
            deck.Transform=deckHome;deck.Position+=Vector3.Right*.05f;
            platterProbe=!VisionSorterContactProfileClear(deck,platter,true);
        }
        finally { deck.Transform=deckHome; }
        GD.Print("VISION_SORTER_MATING_NEGATIVE_CONTROLS_END restored=True");
        check(beltProbe,"vision_sorter_belt_profile_screen_detects_deliberately_lowered_deck");
        check(platterProbe,"vision_sorter_platter_profile_screen_detects_deliberately_shifted_nose");
        var beltBearingEnd=surface.Mesh.GetFaces().Select(v=>surface.GlobalTransform*v)
            .Where(v=>MathF.Abs(v.Y-belt.End.Y)<.002f).Max(v=>v.X);
        GD.Print($"VISION_SORTER_BELT_BEARING_END {beltBearingEnd}");
        var topTriangles = new[] {surface,deck,platter}.SelectMany(mesh=>
        {
            var vertices=mesh.Mesh.GetFaces().Select(v=>mesh.GlobalTransform*v).ToArray();
            return Enumerable.Range(0,vertices.Length/3).Select(i=>new[]{vertices[i*3],vertices[i*3+1],vertices[i*3+2]})
                .Where(t=>t.All(v=>MathF.Abs(v.Y-belt.End.Y)<.002f));
        }).ToArray();
        bool Supported(Vector2 p)
        {
            foreach(var t in topTriangles)
            {
                var x0=t[0].X;var z0=t[0].Z;var x1=t[1].X;var z1=t[1].Z;var x2=t[2].X;var z2=t[2].Z;
                var denominator=(z1-z2)*(x0-x2)+(x2-x1)*(z0-z2);
                if(MathF.Abs(denominator)<1e-9f)continue;
                var u=((z1-z2)*(p.X-x2)+(x2-x1)*(p.Y-z2))/denominator;
                var v=((z2-z0)*(p.X-x2)+(x0-x2)*(p.Y-z2))/denominator;
                if(u>=-.00001f && v>=-.00001f && u+v<=1.00001f)return true;
            }
            return false;
        }
        var routeSupported=true;var samples=0;
        for(var step=0;step<=88;step++)
        for(var along=0;along<=4;along++)
        for(var across=0;across<=4;across++)
        {
            var x=-1.1f+step*.05f-carton.Size.X/2+carton.Size.X*along/4;
            var z=carton.Position.Z+carton.Size.Z*across/4;
            if(!Supported(new Vector2(x,z)))
            {
                if(routeSupported) GD.Print($"VISION_SORTER_UNSUPPORTED x={x} z={z}");
                routeSupported=false;
            }
            samples++;
        }
        GD.Print($"VISION_SORTER_SUPPORT_SAMPLES {samples}");
        check(routeSupported,"vision_sorter_infeed_bridge_and_platter_support_sampled_full_carton_footprint");
        var handoffs=root.GetChildren().OfType<Node3D>().Where(n=>n.Name.ToString().StartsWith("sorter_outgoing_bridge_",StringComparison.Ordinal)).ToArray();
        check(handoffs.Length==4 && root.GetNodeOrNull<Node3D>("training_accessory_7") is null,
            "vision_sorter_four_outgoing_handoffs_replace_cabinet_diverter_surrogate");
        var outgoingClear=handoffs.Length==4;var outgoingMatingClear=handoffs.Length==4;
        var peerParts=root.GetChildren().OfType<Node3D>().SelectMany(n=>ReviewMeshes(n).Select(m=>(Root:n,Mesh:m,Bounds:ReviewBounds(m)))).ToArray();
        foreach(var handoff in handoffs)
        {
            var laneId=handoff.Name.ToString().Replace("sorter_outgoing_bridge_","destination_lane_",StringComparison.Ordinal);
            var lane=root.GetNode<Node3D>(laneId);
            var outgoingDeck=(MeshInstance3D)handoff.FindChild("SORTER_handoff_deck",true,false);
            var receivingDeck=(MeshInstance3D)lane.FindChild("KIN_belt_surface",true,false);
            var receivingDrum=(MeshInstance3D)lane.FindChild("KIN_drive_drum",true,false);
            var receivingTail=(MeshInstance3D)lane.FindChild("KIN_tail_drum",true,false);
            outgoingMatingClear &= VisionSorterContactProfileClear(outgoingDeck,receivingDeck,false,lane)
                && VisionSorterContactProfileClear(outgoingDeck,receivingDrum,false,lane)
                && VisionSorterContactProfileClear(outgoingDeck,receivingTail,false,lane)
                && VisionSorterContactProfileClear(outgoingDeck,platter,true);
            foreach(var part in ReviewMeshes(handoff))
            foreach(var peer in peerParts.Where(p=>p.Root!=handoff))
            {
                if(part==outgoingDeck && (peer.Mesh==receivingDeck || peer.Mesh==receivingDrum || peer.Mesh==receivingTail || peer.Mesh==platter))continue;
                var overlap=ReviewBounds(part).Intersection(peer.Bounds).Size;
                if(overlap.X<=.001f || overlap.Y<=.001f || overlap.Z<=.001f || !OrientedBoxesPenetrate(part,peer.Mesh))continue;
                if(peer.Mesh==platter && VisionSorterContactProfileClear(part,platter,true))continue;
                GD.Print($"VISION_SORTER_OUTGOING_BOUNDS part={ReviewBounds(part)} peer={peer.Bounds}");
                GD.Print($"VISION_SORTER_OUTGOING_INTERFERENCE {handoff.Name}/{part.Name} {peer.Root.Name}/{peer.Mesh.Name}");
                outgoingClear=false;
            }
        }
        check(outgoingClear,"vision_sorter_all_four_outgoing_bridges_clear_other_equipment_at_one_mm_obb_screen");
        check(outgoingMatingClear,"vision_sorter_four_outgoing_belt_drum_platter_profiles_clear_in_lane_frames");
        var infeedTriangles=topTriangles;
        var outgoingSupported=handoffs.Length==4;var outgoingSamples=0;
        foreach(var lane in lanes)
        {
            var id=lane.Name.ToString().Replace("destination_lane_","sorter_outgoing_bridge_",StringComparison.Ordinal);
            var handoff=root.GetNodeOrNull<Node3D>(id);
            if(handoff is null){outgoingSupported=false;continue;}
            var handoffDeck=(MeshInstance3D)handoff.FindChild("SORTER_handoff_deck",true,false);
            var laneDeck=(MeshInstance3D)lane.FindChild("KIN_belt_surface",true,false);
            topTriangles=new[]{platter,handoffDeck,laneDeck}.SelectMany(mesh=>
            {
                var vertices=mesh.Mesh.GetFaces().Select(v=>mesh.GlobalTransform*v).ToArray();
                return Enumerable.Range(0,vertices.Length/3).Select(i=>new[]{vertices[i*3],vertices[i*3+1],vertices[i*3+2]})
                    .Where(t=>t.All(v=>MathF.Abs(v.Y-belt.End.Y)<.002f));
            }).ToArray();
            var alongAxis=lane.GlobalBasis.X.Normalized();var acrossAxis=lane.GlobalBasis.Z.Normalized();
            for(var step=0;step<=122;step++)
            for(var along=0;along<=4;along++)
            for(var across=0;across<=4;across++)
            {
                var distance=step*.05f-carton.Size.X/2+carton.Size.X*along/4;
                var offset=carton.Position.Z+carton.Size.Z*across/4;
                var world=table.GlobalPosition+alongAxis*distance+acrossAxis*offset;
                if(!Supported(new Vector2(world.X,world.Z)))
                {
                    if(outgoingSupported)GD.Print($"VISION_SORTER_OUTGOING_UNSUPPORTED {lane.Name} step={step} world={world}");
                    outgoingSupported=false;
                }
                outgoingSamples++;
            }
        }
        topTriangles=infeedTriangles;
        GD.Print($"VISION_SORTER_OUTGOING_SUPPORT_SAMPLES {outgoingSamples}");
        check(outgoingSupported,"vision_sorter_all_four_outgoing_routes_support_sampled_full_carton_footprint");
        GD.Print($"VISION_SORTER_CARTON_SUPPORT carton={carton} belt={belt}");
        check(MathF.Abs(carton.Position.Y - belt.End.Y) < .001f,
            "vision_sorter_carton_bottom_contacts_actual_delivered_belt_top");
        check(carton.Position.X >= belt.Position.X && carton.End.X <= belt.End.X
            && carton.Position.Z >= belt.Position.Z && carton.End.Z <= belt.End.Z,
            "vision_sorter_initial_carton_full_footprint_is_supported");
    }

    // Conservative contact silhouette test: clip actual bridge triangles
    // against an obstacle vertex hull (XY for belt/drum, XZ for platter),
    // eroded by a declared 1 mm allowance. These projections are bounded
    // geometry evidence, not general concave-mesh or swept-volume proof.
    private static bool VisionSorterContactProfileClear(MeshInstance3D deck, MeshInstance3D drum, bool horizontal = false, Node3D? referenceFrame = null)
    {
        Vector2 Project(Vector3 v)=>horizontal ? new Vector2(v.X,v.Z) : new Vector2(v.X,v.Y);
        float Depth(Vector3 v)=>horizontal ? v.Y : v.Z;
        var toFrame=referenceFrame?.GlobalTransform.AffineInverse() ?? Transform3D.Identity;
        var obstacleVertices=drum.Mesh.GetFaces().Select(v=>toFrame*drum.GlobalTransform*v).ToArray();
        var points=obstacleVertices.Select(Project).Distinct().OrderBy(v=>v.X).ThenBy(v=>v.Y).ToArray();
        float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
        var hull=new System.Collections.Generic.List<Vector2>();
        foreach(var p in points)
        {
            while(hull.Count>=2 && Cross(hull[^1]-hull[^2],p-hull[^1])<=0)hull.RemoveAt(hull.Count-1);
            hull.Add(p);
        }
        var lower=hull.Count;
        for(var i=points.Length-2;i>=0;i--)
        {
            var p=points[i];
            while(hull.Count>lower && Cross(hull[^1]-hull[^2],p-hull[^1])<=0)hull.RemoveAt(hull.Count-1);
            hull.Add(p);
        }
        if(hull.Count>1)hull.RemoveAt(hull.Count-1);
        if(hull.Count<3)return false;
        var faces=deck.Mesh.GetFaces();
        var depthStart=obstacleVertices.Min(Depth);var depthEnd=obstacleVertices.Max(Depth);
        for(var i=0;i<faces.Length;i+=3)
        {
            var triangle=new[]{toFrame*deck.GlobalTransform*faces[i],toFrame*deck.GlobalTransform*faces[i+1],toFrame*deck.GlobalTransform*faces[i+2]};
            if(triangle.All(v=>Depth(v)<depthStart) || triangle.All(v=>Depth(v)>depthEnd))continue;
            var clipped=triangle.Select(Project).ToList();
            for(var edge=0;edge<hull.Count && clipped.Count>0;edge++)
            {
                var origin=hull[edge];var direction=hull[(edge+1)%hull.Count]-origin;
                float Distance(Vector2 v)=>Cross(direction,v-origin)-.001f*direction.Length();
                var next=new System.Collections.Generic.List<Vector2>();
                var previous=clipped[^1];var before=Distance(previous);
                foreach(var current in clipped)
                {
                    var after=Distance(current);
                    if((before>=0)!=(after>=0))next.Add(previous+(current-previous)*(before/(before-after)));
                    if(after>=0)next.Add(current);
                    previous=current;before=after;
                }
                clipped=next;
            }
            if(clipped.Count>0)
            {
                GD.Print($"VISION_SORTER_CONTACT_PROFILE_INTERFERENCE obstacle={drum.Name} triangle={i/3}");return false;
            }
        }
        GD.Print($"VISION_SORTER_CONTACT_PROFILE_CLEAR obstacle={drum.Name} declared_allowance_mm=1");
        return true;
    }

    private void VerifyCartonStaticCableRoutes(Action<bool, string> check,
        string sceneId = "scene-2-conveyor-pusher", string checkPrefix = "scene2")
    {
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var equipment = _sceneCompositionRoot!.GetChildren().OfType<Node3D>()
            .Select(node => (Node: node, Meshes: ReviewMeshes(node))).ToArray();
        var candidates = 0; var unresolved = 0;
        bool Cable(MeshInstance3D mesh) => mesh.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase);
        for (var a = 0; a < equipment.Length; a++)
        for (var b = a + 1; b < equipment.Length; b++)
        foreach (var left in equipment[a].Meshes)
        foreach (var right in equipment[b].Meshes)
        {
            var overlap = ReviewBounds(left).Intersection(ReviewBounds(right)).Size;
            if (overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f) continue;
            candidates++;
            var cable = Cable(left) ? left : Cable(right) ? right : null;
            var other = cable == left ? right : left;
            var surfaceCandidate = cable is null;
            if (cable is not null)
            {
                // Screen each actual cable triangle in the other component's
                // local frame. This excludes empty route bounds. A remaining
                // candidate still needs inspection: the other mesh's local box
                // can contain empty space. Cable pairs also screen against
                // individual triangles rather than the entire second route.
                var transform = other.GlobalTransform.AffineInverse() * cable.GlobalTransform;
                // Convert a 1 mm world allowance into each local axis; authored
                // equipment scales must not silently change the tolerance.
                var basis = other.GlobalTransform.Basis;
                var padding = new Vector3(0.001f / basis.X.Length(),
                    0.001f / basis.Y.Length(), 0.001f / basis.Z.Length());
                var authored = other.GetAabb();
                var bounds = new Aabb(authored.Position + padding, authored.Size - padding * 2);
                var otherFaces = Cable(other) ? other.Mesh.GetFaces() : System.Array.Empty<Vector3>();
                var otherTriangles = Enumerable.Range(0, otherFaces.Length / 3).Select(index =>
                    new Aabb(otherFaces[index * 3], Vector3.Zero)
                        .Expand(otherFaces[index * 3 + 1]).Expand(otherFaces[index * 3 + 2])).ToArray();
                var faces = cable.Mesh.GetFaces();
                for (var index = 0; index < faces.Length && !surfaceCandidate; index += 3)
                {
                    var triangle = new Aabb(transform * faces[index], Vector3.Zero)
                        .Expand(transform * faces[index + 1]).Expand(transform * faces[index + 2]);
                    surfaceCandidate = triangle.Grow(0.0001f).Intersects(bounds)
                        && (otherTriangles.Length == 0 || otherTriangles.Any(otherTriangle =>
                            triangle.Grow(0.0001f).Intersects(otherTriangle)));
                }
            }
            if (surfaceCandidate)
            {
                unresolved++;
                GD.Print($"CARTON_STATIC_ROUTE_BOUNDS left={ReviewBounds(left)} right={ReviewBounds(right)}");
            }
            GD.Print($"CARTON_STATIC_ROUTE {equipment[a].Node.Name}/{left.Name} {equipment[b].Node.Name}/{right.Name} surfaceCandidate={surfaceCandidate}");
        }
        GD.Print($"CARTON_STATIC_ROUTE_TOTAL broad={candidates} unresolved={unresolved}");
        check(unresolved == 0, $"{checkPrefix}_static_routes_clear_other_equipment_local_bounds_at_one_mm_tolerance");
    }
}
