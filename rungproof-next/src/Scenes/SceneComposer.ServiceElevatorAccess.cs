using System;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Original illustrative installation. Stair dimensions, hoist arrangement
    // and open access gate are not a certified lift, load or personnel design.
    private static void AddServiceElevatorAccessAndHoist(Node3D root, Material steel, Material blue, Material dark)
    {
        MeshInstance3D Box(Node3D parent, string name, Vector3 size, Vector3 position, Material material)
        { var part = AddBox(parent, size, position, material); part.Name = name; return part; }
        // A new crossmember bears on the existing top side beams; both drum
        // bearings and motor feet bear on it, rather than floating over the car.
        Box(root, "ELEVATOR_hoist_crossmember", new(2.74f,.16f,.40f), new(0,5.2f,-.45f), steel);
        foreach (var (side,x) in new[] { ("left",-.66f),("right",.66f) })
            Box(root, "ELEVATOR_hoist_bearing_"+side, new(.16f,.26f,.32f), new(x,5.41f,-.45f), blue);
        AddCylinder(root, "ELEVATOR_hoist_drum", new(0,5.48f,-.45f), .14f,1.16f,dark,new(0,0,90));
        Box(root,"ELEVATOR_hoist_motor_foot",new(.4f,.06f,.4f),new(.95f,5.31f,-.45f),steel);
        Box(root,"ELEVATOR_hoist_motor",new(.38f,.28f,.32f),new(.95f,5.48f,-.45f),blue);
        Box(root,"ELEVATOR_hoist_coupling",new(.10f,.08f,.08f),new(.75f,5.48f,-.45f),steel);
        var car = root.GetNode<Node3D>("KIN_service_elevator_car");
        foreach (var (side, x) in new[] { ("left",-.42f), ("right",.42f) }) {
            Box(car,"ELEVATOR_hoist_anchor_"+side,new(.12f,.08f,.12f),new(x,2.58f,-.59f),steel);
            Box(root,"ELEVATOR_hoist_rope_"+side,new(.025f,1,.025f),new(x,4,-.59f),dark);
        }
        // Rear deck opening spans x=-.43..+.43. Fifteen horizontal treads
        // connect its 2.7m upper surface to floor, with continuous stringers.
        const float rise = .18f, run = .28f, topZ = 2.80f;
        for (var i = 0; i < 15; i++)
            Box(root,$"ELEVATOR_access_tread_{i}",new(.82f,.05f,run),new(0,2.7f-i*rise-.025f,topZ+i*run),steel);
        var upper = new Vector3(0,2.69f,2.66f);
        var lower = new Vector3(0,.08f,6.72f);
        var length = upper.DistanceTo(lower);
        var slope = MathF.Atan2(upper.Y-lower.Y,lower.Z-upper.Z);
        foreach (var (side,x) in new[] { ("left",-.37f),("right",.37f) }) {
            var stringer = Box(root,"ELEVATOR_access_stringer_"+side,new(.075f,.14f,length),new(x,(upper.Y+lower.Y)/2,(upper.Z+lower.Z)/2),steel);
            stringer.Rotation = new Vector3(slope,0,0);
            Box(root,"ELEVATOR_access_foot_"+side,new(.20f,.11f,.28f),new(x,.055f,6.72f),steel);
        }
        foreach (var (side,x) in new[] { ("left",-.395f),("right",.395f) }) {
            // Uprights start on their tread and rails follow the same incline.
            foreach (var i in new[] { 0, 5, 10, 14 })
                Box(root,$"ELEVATOR_access_post_{side}_{i}",new(.05f,1.05f,.05f),new(x,2.7f-i*rise+.525f,topZ+i*run),blue);
            foreach (var (level,offset) in new[] { ("middle",.55f),("top",1.025f) }) {
                var rail = Box(root,$"ELEVATOR_access_rail_{side}_{level}",new(.05f,.05f,new Vector3(0,14*rise,14*run).Length()),
                    new(x,2.7f-7*rise+offset,topZ+7*run),blue);
                rail.Rotation = new Vector3(MathF.Atan2(rise,run),0,0);
            }
            Box(root,"ELEVATOR_access_rail_transition_"+side,new(.12f,.05f,.24f),new(MathF.Sign(x)*.4275f,3.725f,2.70f),blue);
        }
        // Fixed-open gate at the existing left rear access post. It projects
        // into the landing, outside the sliding-door and car envelopes. No
        // automatic gate movement or gate-closed feedback is fabricated.
        var gate = new Node3D { Name="ELEVATOR_access_gate_open",Position=new(-.46f,2.7f,2.60f),Rotation=new(0,MathF.PI/2,0) };
        root.AddChild(gate);
        foreach (var (level,y) in new[] { ("middle",.30f),("top",.80f) }) Box(gate,"ELEVATOR_gate_rail_"+level,new(.86f,.045f,.045f),new(.43f,y,0),blue);
        Box(gate,"ELEVATOR_gate_latch_end",new(.045f,.55f,.045f),new(.86f,.55f,0),blue);
        Box(gate,"ELEVATOR_gate_hinge",new(.06f,.65f,.06f),new(0,.55f,0),steel);
        // Independent short access stair bears on floor beside the lower deck.
        for (var i=0;i<3;i++)
            Box(root,$"ELEVATOR_lower_access_step_{i}",new(.30f,.60f-i*.20f,.70f),new(1.45f+i*.30f,(.60f-i*.20f)/2,1.80f),steel);
        ProjectServiceElevatorHoist(root);
    }

    public static void ProjectServiceElevatorHoist(Node3D root)
    {
        var car = root.GetNode<Node3D>("KIN_service_elevator_car");
        // Read the actual modeled car transform, keeping rope endpoints tied
        // to the car rather than echoing an independent position command.
        var bottom = car.Position.Y + 2.62f;
        const float top = 5.48f;
        if (!float.IsFinite(bottom) || bottom >= top) throw new InvalidOperationException("Elevator rope endpoint exceeds illustrative hoist.");
        foreach (var side in new[] { "left", "right" }) {
            var rope = root.GetNode<MeshInstance3D>("ELEVATOR_hoist_rope_"+side);
            ((BoxMesh)rope.Mesh).Size = new Vector3(.025f,top-bottom,.025f);
            rope.Position = new Vector3(rope.Position.X,(top+bottom)/2,rope.Position.Z);
        }
    }
}
