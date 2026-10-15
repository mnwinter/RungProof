using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local visual installation. Position is deliberately setpoint-owned:
    // a generic Run command must not fabricate a working elevator sequence.
    private static Node3D CreateServiceElevator()
    {
        var root = new Node3D();
        var steel = Material(new Color("677b86"), .6f, .35f);
        var blue = Material(new Color("147eaf"), .3f, .4f);
        var floor = Material(new Color("c5cbd0"), .4f, .55f);
        var dark = Material(new Color("26343d"), .25f, .5f);
        MeshInstance3D Box(Node3D parent, string name, Vector3 size, Vector3 at, Material material)
        { var mesh = AddBox(parent, size, at, material); mesh.Name = name; return mesh; }
        void Label(string name, string text, Vector3 at)
        { root.AddChild(new Label3D { Name = name, Text = text, Position = at,
            // Review annotations face the observer instead of presenting
            // reversed lettering when the front camera sees their back face.
            FontSize = 48, PixelSize = .004f, OutlineSize = 4, Modulate = Colors.White,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true }); }

        // The car envelope is wholly inside four shaft columns. The two
        // landing surfaces meet the car floor at 0.6 and 2.7 metres.
        foreach (var x in new[] { -1.3f, 1.3f })
        foreach (var z in new[] { -1.05f, 1.05f })
        {
            Box(root, $"ELEVATOR_foot_{x}_{z}", new(.34f,.12f,.34f), new(x,.06f,z), steel);
            Box(root, $"ELEVATOR_column_{x}_{z}", new(.14f,5.1f,.14f), new(x,2.67f,z), blue);
        }
        foreach (var z in new[] { -1.05f, 1.05f })
            Box(root, $"ELEVATOR_header_{z}", new(2.74f,.16f,.14f), new(0,5.2f,z), steel);
        foreach (var x in new[] { -1.3f, 1.3f })
        {
            Box(root, $"ELEVATOR_top_side_{x}", new(.14f,.16f,2.1f), new(x,5.2f,0), steel);
            Box(root, $"ELEVATOR_guide_rail_{x}", new(.06f,4.65f,.1f), new(x*.86f,2.65f,-.65f), steel);
        }
        var car = new Node3D { Name = "KIN_service_elevator_car" };
        root.AddChild(car);
        Box(car,"ELEVATOR_car_floor",new(2.0f,.12f,1.8f),new(0,.54f,0),floor);
        Box(car,"ELEVATOR_car_back",new(2.0f,1.84f,.08f),new(0,1.52f,-.86f),blue);
        foreach (var x in new[] { - .96f,.96f })
            Box(car,$"ELEVATOR_car_side_{x}",new(.08f,1.84f,1.72f),new(x,1.52f,0),blue);
        Box(car,"ELEVATOR_car_roof",new(2.0f,.10f,1.8f),new(0,2.49f,0),steel);
        Box(car,"ELEVATOR_car_threshold",new(1.84f,.04f,.16f),new(0,.58f,.98f),steel);

        foreach (var y in new[] { .6f, 2.7f })
        {
            Box(root,$"ELEVATOR_landing_deck_{y}",new(2.6f,.12f,1.6f),new(0,y-.06f,1.86f),floor);
            if (y > 1)
            {
                // Fixed landing edge guards stay outside the car entrance
                // and the sliding-door fixture. Rear centre remains an access
                // opening connected to the scene-local access installation below.
                foreach (var x in new[] { -1.25f, 1.25f })
                {
                    foreach (var z in new[] { 1.35f, 2.60f })
                        Box(root,$"ELEVATOR_guard_post_{x}_{z}",new(.06f,1.1f,.06f),new(x,y+.55f,z),blue);
                    foreach (var height in new[] { .55f, 1.07f })
                        Box(root,$"ELEVATOR_guard_side_{x}_{height}",new(.06f,.06f,1.31f),new(x,y+height,1.975f),blue);
                    Box(root,$"ELEVATOR_guard_toeboard_{x}",new(.04f,.15f,1.31f),new(x,y+.075f,1.975f),steel);
                }
                foreach (var x in new[] { -.46f, .46f })
                    Box(root,$"ELEVATOR_guard_access_post_{x}",new(.06f,1.1f,.06f),new(x,y+.55f,2.60f),blue);
                foreach (var x in new[] { -.855f, .855f })
                foreach (var height in new[] { .55f, 1.07f })
                    Box(root,$"ELEVATOR_guard_rear_{x}_{height}",new(.85f,.06f,.06f),new(x,y+height,2.60f),blue);
            }
            foreach (var x in new[] { -1.23f,1.23f })
            {
                if (y > 1)
                {
                    Box(root,$"ELEVATOR_landing_support_{x}",new(.12f,y-.12f,.12f),new(x,(y-.12f)/2,2.55f),steel);
                    Box(root,$"ELEVATOR_landing_foot_{x}",new(.28f,.08f,.28f),new(x,.04f,2.55f),steel);
                }
                Box(root,$"ELEVATOR_door_jamb_{x}_{y}",new(.12f,1.9f,.12f),new(x,y+.95f,1.07f),steel);
            }
            Box(root,$"ELEVATOR_door_header_{y}",new(2.58f,.12f,.12f),new(0,y+1.96f,1.17f),steel);
            // A continuous overhead track supports the leaves at either pose.
            // Hangers belong to each leaf so fixture changes carry them along.
            Box(root,$"ELEVATOR_door_track_{y}",new(4.65f,.08f,.08f),new(0,y+1.98f,1.27f),steel);
            // Open leaves make the landing/car relationship inspectable.
            // Their pose is visual only; doors_closed remains a manual input.
            foreach (var x in new[] { -1.72f,1.72f })
            {
                var leaf = Box(root,$"ELEVATOR_open_door_leaf_{x}_{y}",new(1.1f,1.8f,.05f),new(x,y+.94f,1.17f),dark);
                foreach (var hangerX in new[] { -.35f,.35f })
                    Box(leaf,$"ELEVATOR_door_hanger_{hangerX}_{x}_{y}",new(.04f,.17f,.1f),new(hangerX,.975f,.05f),steel);
            }
            // Landing-mounted station stays outside the sliding-leaf sweep.
            // The former jamb button penetrated the fully open leaf.
            Box(root,$"ELEVATOR_call_foot_{y}",new(.18f,.04f,.18f),new(.95f,y+.02f,2.42f),steel);
            Box(root,$"ELEVATOR_call_post_{y}",new(.06f,1.2f,.06f),new(.95f,y+.6f,2.42f),steel);
            Box(root,$"ELEVATOR_call_plate_{y}",new(.15f,.3f,.05f),new(.95f,y+1.2f,2.455f),dark);
            AddCylinder(root,$"ELEVATOR_call_button_{y}",new(.95f,y+1.2f,2.49f),.035f,.025f,blue,new(90,0,0));
            Box(root,$"ELEVATOR_position_sensor_{y}",new(.1f,.08f,.12f),new(-1.12f,y+.04f,-.65f),dark);
            Label($"ELEVATOR_landing_label_{y}", y < 1 ? "LOWER LANDING" : "UPPER LANDING",new(-4.5f,y+.3f,y < 1 ? 4.0f : 1.86f));
        }
        // Upper deck support continues to ground; its feet must not duplicate
        // the lower deck feet at the same position.
        AddServiceElevatorAccessAndHoist(root,steel,blue,dark);
        root.AddChild(new EquipmentMotionController { Name = "ServiceElevatorPosition",
            Kind = EquipmentMotionController.MotionKind.LinearY,
            TargetPrefix = "KIN_service_elevator_car", TravelM = 2.1f,
            AutonomousPositionTravel = false });
        return root;
    }
}
