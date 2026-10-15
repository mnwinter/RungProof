using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditRobotCnc;

    // Diagnose delivered installation and actual reference travel separately
    // from the accepted regression suite. Red process checks must stay visible
    // until the robot, load and enclosure satisfy them together.
    private void AuditRobotCnc()
    {
        var failures = 0;
        void Check(bool condition, string name)
        {
            if (!condition) failures++;
            GD.Print($"ROBOT_CNC_AUDIT {name}={condition}");
        }
        try
        {
            AddMigratedScene("lab-2-24-robot-cnc", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var runtime = _sceneRuntime!;
            runtime.UsesExternalClock = false;
            var load = root.GetNode<Node3D>("cnc_workpiece");
            var robot = root.GetNode<Node3D>("cnc_robot");
            var machine = root.GetNode<Node3D>("cnc_machine");
            var infeed = root.GetNode<Node3D>("cnc_infeed");
            var outfeed = root.GetNode<Node3D>("cnc_outfeed");
            var sensor = root.GetNode<Node3D>("cnc_infeed_sensor");
            var home = load.Transform;
            MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
            foreach (var equipment in root.GetChildren().OfType<Node3D>())
            foreach (var part in ReviewMeshes(equipment)) GD.Print($"ROBOT_CNC_MESH {equipment.Name}/{part.Name} {ReviewBounds(part)}");
            bool OnBelt(Node3D belt)
            {
                var item = ReviewBounds(load);
                return ReviewMeshes(belt).Where(mesh => mesh.Name.ToString().Contains("belt_surface", StringComparison.OrdinalIgnoreCase))
                    .Any(mesh =>
                    {
                        var deck = ReviewBounds(mesh);
                        return MathF.Abs(item.Position.Y - deck.End.Y) < 0.002f
                            && item.Position.X >= deck.Position.X - 0.001f && item.End.X <= deck.End.X + 0.001f
                            && item.Position.Z >= deck.Position.Z - 0.001f && item.End.Z <= deck.End.Z + 0.001f;
                    });
            }
            Check(OnBelt(infeed), "initial_blank_bears_on_infeed_belt");
            Check(ReviewMeshes(load).All(mesh => !mesh.Name.ToString().Contains("VISE", StringComparison.OrdinalIgnoreCase)),
                "moving_blank_does_not_include_the_machine_vise");
            Check(machine.FindChild("WORK_stock", true, false) is not MeshInstance3D coupon || !coupon.IsVisibleInTree(),
                "machine_has_no_duplicate_stock");
            var door = Part(machine, "DOOR_glazing_left");
            var closedDoorPlane = ReviewBounds(door).GetCenter().Z;
            var localRobot = machine.GlobalTransform.AffineInverse() * robot.GlobalPosition;
            var localDoor = machine.GlobalTransform.AffineInverse() * door.GlobalPosition;
            Check(localRobot.Z > localDoor.Z, "robot_base_is_on_machine_front_access_side");
            Check(MathF.Abs(ReviewBounds(Part(robot, "ROBOT_base")).Position.Y) < 0.002f
                && MathF.Abs(ReviewBounds(Part(machine, "MACHINE_base")).Position.Y) < 0.002f,
                "robot_and_machine_bases_are_grounded");
            // Optical contact is intended; opaque beam geometry is excluded.
            var cache = new Dictionary<MeshInstance3D, Aabb>();
            Aabb Bounds(MeshInstance3D part)
            {
                if (!cache.TryGetValue(part, out var value)) cache[part] = value = ReviewBounds(part);
                return value;
            }
            bool Solid(MeshInstance3D part) => !part.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal);
            bool Clear(MeshInstance3D a, MeshInstance3D b)
            {
                var overlap = Bounds(a).Intersection(Bounds(b)).Size;
                if (overlap.X <= 0.002f || overlap.Y <= 0.002f || overlap.Z <= 0.002f || !OrientedBoxesPenetrate(a, b)) return true;
                bool Cable(MeshInstance3D part) => part.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase)
                    || part.Name.ToString().Contains("dresspack", StringComparison.OrdinalIgnoreCase);
                var cable = Cable(a) ? a : Cable(b) ? b : null;
                if (cable is not null)
                {
                    var other = cable == a ? b : a;
                    var transform = other.GlobalTransform.AffineInverse() * cable.GlobalTransform;
                    var faces = cable.Mesh.GetFaces();
                    var candidate = false;
                    for (var i = 0; i < faces.Length && !candidate; i += 3)
                        candidate = new Aabb(transform * faces[i], Vector3.Zero).Expand(transform * faces[i + 1])
                            .Expand(transform * faces[i + 2]).Grow(0.0001f).Intersects(other.GetAabb());
                    if (!candidate) return true;
                }
                return false;
            }
            var groups = root.GetChildren().OfType<Node3D>().ToArray();
            var staticClear = true;
            for (var a = 0; a < groups.Length; a++)
            for (var b = a + 1; b < groups.Length; b++)
            foreach (var left in ReviewMeshes(groups[a]).Where(Solid))
            foreach (var right in ReviewMeshes(groups[b]).Where(Solid))
                if (!Clear(left, right)) { staticClear = false; GD.Print($"ROBOT_CNC_STATIC {groups[a].Name}/{left.Name} {groups[b].Name}/{right.Name}"); }
            Check(staticClear, "separate_equipment_clear_at_home");
            var beamBounds = ReviewBounds(ReviewMeshes(sensor).First(part => part.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)));
            var pickup = ReviewBounds(load);
            Check(beamBounds.Position.Y > 0.9f && beamBounds.End.Y < 0.9f + pickup.Size.Y,
                "pickup_beam_height_intersects_seated_blank");
            var infeedSupported = true;
            var outfeedSupported = true;
            var stationSupported = true;
            var loadClear = true;
            var gripped = true;
            var sawTransfer = false;
            var doorsClear = true;
            var robotClear = true;
            var transferAttached = true;
            var machiningGuarded = true;
            var sawMachining = false;
            var handling = robot.GetNode<RungProof.Next.Scenes.PalletRobotMotion>("PalletRobotMotion");
            var access = machine.GetNode<EquipmentMotionController>("CncDoorleftMotion");
            var rightAccess = machine.GetNode<EquipmentMotionController>("CncDoorrightMotion");
            var toolAccess = machine.GetNode<EquipmentMotionController>("CncToolClearanceMotion");
            var homeAngles = handling.JointAngles;
            var sawLoadEntry = false;
            var sawUnloadExit = false;
            var accessOpenForCrossing = true;
            var doorBearings = true;
            var track = ReviewBounds(Part(machine, "DOOR_top_track"));
            var rollers = ReviewMeshes(machine).Where(part => part.Name.ToString().StartsWith("KIN_door_roller_", StringComparison.Ordinal)).ToArray();
            var robotParts = ReviewMeshes(robot).Where(Solid).ToArray();
            var fixedSurroundings = groups.Where(group => group != load && group != robot)
                .SelectMany(ReviewMeshes).Where(Solid).ToArray();
            var previousLoadPosition = load.GlobalPosition;
            var previousBeltRunning = false;
            var reportedContacts = new HashSet<string>(StringComparer.Ordinal);
            var surrounding = groups.Where(group => group != load).SelectMany(ReviewMeshes).Where(Solid).ToArray();
            var fingers = ReviewMeshes(robot).Where(part => part.Name.ToString().StartsWith("ROBOT_gripper_finger", StringComparison.Ordinal)
                && !part.Name.ToString().Contains("bolt", StringComparison.Ordinal)).ToArray();
            var doors = ReviewMeshes(machine).Where(part => part.Name.ToString().StartsWith("DOOR_", StringComparison.Ordinal)).ToArray();
            var controllers = root.FindChildren("*", "", true, false).OfType<EquipmentMotionController>().ToArray();
            runtime.ExecuteAction("start-cnc");
            for (var tick = 0; tick < 22500; tick++)
            {
                runtime.AdvanceSimulation(0.002);
                foreach (var controller in controllers) controller._PhysicsProcess(0.002);
                if (runtime.Points["infeed_run"] is true) infeedSupported &= OnBelt(infeed);
                if (runtime.Points["outfeed_run"] is true) outfeedSupported &= OnBelt(outfeed);
                if (runtime.Points["cnc_run"] is true)
                {
                    sawMachining = true;
                    machiningGuarded &= access.PositionPercent < 0.001f && rightAccess.PositionPercent < 0.001f
                        && toolAccess.PositionPercent < 0.001f && handling.GrippedLoad is null
                        && runtime.Points["robot_run"] is false;
                    var item = ReviewBounds(load);
                    stationSupported &= ReviewMeshes(machine).Any(part =>
                    {
                        var bearing = ReviewBounds(part);
                        return MathF.Abs(bearing.End.Y - item.Position.Y) < 0.002f
                            && bearing.Position.X <= item.Position.X + 0.001f && bearing.End.X >= item.End.X - 0.001f
                            && bearing.Position.Z <= item.Position.Z + 0.001f && bearing.End.Z >= item.End.Z - 0.001f;
                    });
                }
                var stockBounds = ReviewBounds(load);
                if (stockBounds.Position.Z < closedDoorPlane && stockBounds.End.Z > closedDoorPlane)
                {
                    if (runtime.Points["machining_complete"] is true) sawUnloadExit = true; else sawLoadEntry = true;
                    accessOpenForCrossing &= access.PositionPercent > 99.999f && rightAccess.PositionPercent > 99.999f
                        && toolAccess.PositionPercent > 99.999f && handling.GrippedLoad == load;
                }
                doorBearings &= rollers.Length == 2 && rollers.All(part =>
                {
                    var bearing = ReviewBounds(part);
                    return MathF.Abs(bearing.Position.Y - track.End.Y) < 0.002f
                        && bearing.Position.X >= track.Position.X && bearing.End.X <= track.End.X;
                });
                // Approach/release/withdraw intentionally have no held stock.
                // Every non-belt stock movement, however, must be tool-driven.
                var beltRunning = runtime.Points["infeed_run"] is true || runtime.Points["outfeed_run"] is true;
                if (!beltRunning && !previousBeltRunning
                    && load.GlobalPosition.DistanceTo(previousLoadPosition) > 0.00001f)
                {
                    sawTransfer = true;
                    transferAttached &= handling.GrippedLoad == load
                        && (handling.ToolTransform.Origin - load.GlobalPosition - Vector3.Up * handling.GripHeightM).Length() < 0.001f;
                    var item = ReviewBounds(load);
                    gripped &= fingers.Length == 2 && fingers.All(part => ReviewBounds(part).Grow(0.003f).Intersects(item));
                    doorsClear &= doors.All(part => !OrientedBoxesPenetrate(ReviewMeshes(load).First(), part));
                }
                previousLoadPosition = load.GlobalPosition;
                previousBeltRunning = beltRunning;
                if (tick % 10 != 0) continue;
                cache.Clear();
                foreach (var part in ReviewMeshes(load))
                foreach (var other in surrounding)
                {
                    if (Clear(part, other)) continue;
                    loadClear = false;
                    // Retain concrete failing pairs for the next transfer repair,
                    // without flooding the diagnostic with every sampled tick.
                    var pair = $"{part.Name}/{other.Name}";
                    if (reportedContacts.Count < 24 && reportedContacts.Add(pair))
                        GD.Print($"ROBOT_CNC_MOTION_CONTACT {pair} tick={tick}");
                }
                foreach (var part in robotParts)
                foreach (var other in fixedSurroundings)
                {
                    if (Clear(part, other)) continue;
                    robotClear = false;
                    var pair = $"{part.Name}/{other.Name}";
                    if (reportedContacts.Count < 24 && reportedContacts.Add(pair))
                        GD.Print($"ROBOT_CNC_ROBOT_CONTACT {pair} tick={tick} part={Bounds(part)} angles={string.Join(',', handling.JointAngles.Select(Mathf.RadToDeg))}");
                }
            }
            Check(infeedSupported && outfeedSupported && OnBelt(outfeed), "blank_supported_through_infeed_and_outfeed_routes");
            Check(sawMachining && stationSupported, "machining_blank_has_full_bearing_surface");
            Check(loadClear, "moving_blank_clears_separate_equipment_through_2250_samples");
            Check(sawTransfer && gripped, "both_robot_fingers_contact_blank_through_transfers");
            Check(sawTransfer && transferAttached, "moving_blank_follows_actual_tool_without_detached_translation");
            Check(sawTransfer && doorsClear, "transferred_blank_clears_enclosure_doors");
            Check(robotClear, "robot_clears_other_equipment_through_2250_samples");
            Check(sawMachining && machiningGuarded, "machining_reference_has_closed_access_and_ungripped_stopped_robot");
            Check(sawLoadEntry && sawUnloadExit && accessOpenForCrossing, "both_actual_aperture_crossings_have_open_doors_retracted_tool_and_attached_load");
            Check(doorBearings, "both_moving_door_rollers_bear_on_track_through_full_cycle");
            Check(runtime.Points["cycle_complete"] is true, "timed_reference_reports_completion");
            var completedLoad = load.Transform;
            Check(!runtime.ExecuteAction("start-cnc") && load.Transform.IsEqualApprox(completedLoad), "completed_restart_rejected_without_teleport");
            runtime.ResetSimulation();
            Check(load.Transform.IsEqualApprox(home), "reset_restores_blank_home");
            bool RestoredRobot() => handling.JointAngles.Zip(homeAngles).All(pair => MathF.Abs(pair.First - pair.Second) < 0.0001f)
                && handling.GrippedLoad is null && access.PositionPercent < 0.001f
                && rightAccess.PositionPercent < 0.001f && toolAccess.PositionPercent < 0.001f;
            Check(RestoredRobot(), "reset_restores_robot_access_and_tool_home");
            runtime.ExecuteAction("start-cnc");
            for (var tick = 0; tick < 4000; tick++) runtime.AdvanceSimulation(0.002);
            var heldLoad = load.Transform;
            var heldAngles = handling.JointAngles;
            var wasHeld = handling.GrippedLoad == load;
            runtime.StopSimulation();
            runtime.AdvanceSimulation(1);
            Check(wasHeld && load.Transform.IsEqualApprox(heldLoad)
                && handling.JointAngles.Zip(heldAngles).All(pair => MathF.Abs(pair.First - pair.Second) < 0.0001f)
                && runtime.Points["robot_run"] is false && runtime.Points["cnc_run"] is false
                && !runtime.ExecuteAction("start-cnc"), "stop_holds_attached_load_and_robot_until_reset");
            runtime.ResetSimulation();
            Check(RestoredRobot() && load.Transform.IsEqualApprox(home), "reset_recovers_interrupted_transfer");
        }
        catch (Exception error)
        {
            failures++;
            GD.PushError($"ROBOT_CNC_AUDIT_EXCEPTION {error}");
        }
        GD.Print($"ROBOT_CNC_AUDIT_RESULT failures={failures}; sampled geometric/reference diagnostic only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
