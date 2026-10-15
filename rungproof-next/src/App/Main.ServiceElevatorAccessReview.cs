using System;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditServiceElevatorAccess;
    private void AuditServiceElevatorAccess()
    {
        var failures=0;
        void Check(bool ok,string name) { if(!ok) failures++; GD.Print($"ELEVATOR_ACCESS_CHECK {name}={ok}"); }
        try { VerifyServiceElevatorAccess(Check); }
        catch(Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"SERVICE_ELEVATOR_ACCESS_VERIFY {(failures==0?"PASS":"FAIL")} sampled illustrative geometry; native pending");
        GetTree().Quit(failures==0?0:1);
    }
    private void VerifyServiceElevatorAccess(Action<bool,string> check)
    {
        AddMigratedScene("lab-11-11-service-elevator",_candidateCatalog!,_mainCamera!,false,false);
        var scene=_sceneCompositionRoot!; var shaft=scene.GetNode<Node3D>("liftTable_0");
        var car=shaft.GetNode<Node3D>("KIN_service_elevator_car");
        var motion=shaft.GetNode<EquipmentMotionController>("ServiceElevatorPosition");
        MeshInstance3D Part(string name)=>shaft.GetNode<MeshInstance3D>(name);
        var bridge=Part("ELEVATOR_hoist_crossmember");
        var drum=Part("ELEVATOR_hoist_drum");
        var motor=Part("ELEVATOR_hoist_motor"); var motorFoot=Part("ELEVATOR_hoist_motor_foot");
        bool TouchY(MeshInstance3D upper,MeshInstance3D lower)=>Math.Abs(ReviewBounds(upper).Position.Y-ReviewBounds(lower).End.Y)<.001;
        check(TouchY(motor,motorFoot)&&TouchY(motorFoot,bridge),"motor_bears_on_foot_and_top_crossmember");
        var motorPose=motor.Position; motor.Position+=Vector3.Up*.05f;
        check(!TouchY(motor,motorFoot),"detached_motor_negative_control_rejected"); motor.Position=motorPose;
        check(new[]{"left","right"}.All(side=>TouchY(Part("ELEVATOR_hoist_bearing_"+side),bridge)),"drum_bearings_bear_on_crossmember");
        var bridgeBounds=ReviewBounds(bridge);
        var topSides=ReviewMeshes(shaft).Where(m=>m.Name.ToString().StartsWith("ELEVATOR_top_side_",StringComparison.Ordinal)).ToArray();
        check(topSides.Length==2&&topSides.All(beam=>{
            var overlap=bridgeBounds.Intersection(ReviewBounds(beam)).Size;
            return overlap.X>.01f&&overlap.Y>.01f&&overlap.Z>.01f;
        }),"crossmember_connects_existing_shaft_top_frame");
        var ropes=new[]{Part("ELEVATOR_hoist_rope_left"),Part("ELEVATOR_hoist_rope_right")};
        var anchors=new[]{car.GetNode<MeshInstance3D>("ELEVATOR_hoist_anchor_left"),car.GetNode<MeshInstance3D>("ELEVATOR_hoist_anchor_right")};
        bool Attached()=>Enumerable.Range(0,2).All(i=>{
            var r=ReviewBounds(ropes[i]);var a=ReviewBounds(anchors[i]);var d=ReviewBounds(drum);
            return Math.Abs(r.Position.Y-a.End.Y)<.001&&Math.Abs(r.End.Y-d.GetCenter().Y)<.001
                &&Math.Abs(r.GetCenter().X-a.GetCenter().X)<.001&&Math.Abs(r.GetCenter().Z-a.GetCenter().Z)<.001
                &&Math.Abs(r.GetCenter().Z-d.Position.Z)<.001;
        });
        check(Attached(),"home_ropes_join_car_anchors_and_drum_tangent");
        var ropePose=ropes[0].Position;ropes[0].Position+=Vector3.Up*.05f;
        check(!Attached(),"detached_rope_negative_control_rejected");ropes[0].Position=ropePose;
        var connected=true;var clear=true;var previous=ReviewBounds(ropes[0]).Size.Y;
        var fixedParts=ReviewMeshes(shaft).Except(ReviewMeshes(car)).ToArray();
        bool Penetrates(MeshInstance3D a,MeshInstance3D b) { var o=ReviewBounds(a).Intersection(ReviewBounds(b)).Size;return o.X>.001&&o.Y>.001&&o.Z>.001; }
        for(var pose=0;pose<=100;pose++) {
            motion.SetPositionNormalized(pose/100f);SceneComposer.ProjectServiceElevatorHoist(shaft);
            connected&=Attached()&&ReviewBounds(ropes[0]).Size.Y<=previous+.001;previous=ReviewBounds(ropes[0]).Size.Y;
            foreach(var moving in ReviewMeshes(car)) foreach(var part in fixedParts) {
                if(!Penetrates(moving,part))continue;
                if(clear)GD.Print($"ELEVATOR_ACCESS_INTERFERENCE pose={pose} moving={moving.Name} fixed={part.Name}");clear=false;
            }
        }
        check(connected,"101_car_poses_keep_ropes_attached_and_shortening");
        check(clear,"101_car_poses_clear_added_hoist_and_access_geometry");
        motion.SetPositionNormalized(0);SceneComposer.ProjectServiceElevatorHoist(shaft);
        var treads=Enumerable.Range(0,15).Select(i=>Part($"ELEVATOR_access_tread_{i}")).ToArray();
        var stringers=new[]{Part("ELEVATOR_access_stringer_left"),Part("ELEVATOR_access_stringer_right")};
        bool TreadSupported(MeshInstance3D tread)=>stringers.All(s=>{
            var b=ReviewBounds(tread);var at=new Vector3(s.GlobalPosition.X,b.Position.Y-.002f,b.GetCenter().Z);
            var local=s.GlobalTransform.AffineInverse()*at;return s.GetAabb().Grow(.002f).HasPoint(local);
        });
        check(treads.All(TreadSupported),"all15_treads_bear_on_both_actual_oriented_stringers");
        var treadPose=treads[7].Position;treads[7].Position+=Vector3.Up*.30f;
        check(!TreadSupported(treads[7]),"floating_tread_negative_control_rejected");treads[7].Position=treadPose;
        check(Math.Abs(ReviewBounds(treads[0]).End.Y-2.7)<.001&&Math.Abs(ReviewBounds(treads[0]).Position.Z-2.66)<.001,
            "upper_tread_meets_upper_landing_surface_and_rear_edge");
        check(new[]{"left","right"}.All(side=>Math.Abs(ReviewBounds(Part("ELEVATOR_access_foot_"+side)).Position.Y)<.001),"upper_access_feet_grounded");
        var lowerSteps=Enumerable.Range(0,3).Select(i=>Part($"ELEVATOR_lower_access_step_{i}")).ToArray();
        check(lowerSteps.All(s=>Math.Abs(ReviewBounds(s).Position.Y)<.001)&&Math.Abs(ReviewBounds(lowerSteps[0]).End.Y-.6)<.001
            &&Math.Abs(ReviewBounds(lowerSteps[0]).Position.X-1.3)<.001,"lower_access_steps_grounded_and_join_lower_deck");
        check(LogBoundsCandidates(scene)==0,"relocated_operator_stations_clear_complete_stair_and_shaft_envelope");
        check(shaft.GetNode<Node3D>("ELEVATOR_access_gate_open").Rotation.Y>1.5,
            "access_gate_explicitly_fixed_open_without_fabricated_feedback");
        _sceneRuntime!.ResetSimulation();check(Attached(),"reset_restores_home_rope_length_and_attachment");
    }
}
