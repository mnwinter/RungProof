using System;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateLuggageTransferBridge()
    {
        var root = new Node3D();
        var steel = new StandardMaterial3D { AlbedoColor = new Color(.45f,.49f,.52f), Metallic=.65f, Roughness=.35f };
        var frame = new StandardMaterial3D { AlbedoColor = new Color(.05f,.24f,.40f), Metallic=.45f };
        void Box(string name, Vector3 position, Vector3 size, Material material) => root.AddChild(new MeshInstance3D {
            Name=name, Position=position, Mesh=new BoxMesh { Size=size }, MaterialOverride=material,
        });
        // The narrow X seam leads from the actual weighing deck to the first
        // retained roller. The side deck joins the roller edge to reject infeed.
        Box("LUGGAGE_transition_surface",new(1.0675f,.8825f,0),new(.135f,.035f,.68f),steel);
        Box("LUGGAGE_reject_bridge_surface",new(2.1f,.8825f,-.66f),new(2.2f,.035f,.44f),steel);
        foreach(var x in new[]{1.2f,3.0f}) {
            Box("LUGGAGE_bridge_header_"+x,new(x,.85f,-.66f),new(.07f,.03f,.44f),frame);
            foreach(var z in new[]{-.52f,-.80f}) {
                Box($"LUGGAGE_bridge_foot_{x}_{z}",new(x,.03f,z),new(.19f,.06f,.19f),steel);
                Box($"LUGGAGE_bridge_leg_{x}_{z}",new(x,.4325f,z),new(.065f,.805f,.065f),frame);
            }
        }
        // Crossbars connect the thin transition lip to the adjacent scale frame.
        foreach(var z in new[]{-.25f,.25f})
            Box("LUGGAGE_transition_mount_"+z,new(.98f,.83f,z),new(.25f,.07f,.045f),frame);
        return root;
    }
    private static void ConfigureLuggageInstallation(Node3D root)
    {
        var diverter=root.GetNode<Node3D>("training_accessory_7");
        diverter.AddChild(new EquipmentMotionController {Name="LuggageRollerMotion",Kind=EquipmentMotionController.MotionKind.ContinuousRotation,TargetPrefix="ROLLER_",RotationAxis=Vector3.Up,SpeedRpm=106.1f});
        // Omit only the presentation carton in this installation. Luggage is
        // represented by the independently addressable delivered suitcase.
        ((MeshInstance3D)diverter.FindChild("CARTON",true,false)).Visible=false;
        // This installation uses an illustrative direct drive at the bearing.
        // The delivered pneumatic presentation linkage cannot follow the moved
        // pivot, so omit it explicitly rather than leaving disconnected hardware.
        foreach(var name in new[]{"DIVERTER_ACTUATOR","ACTUATOR_LINK"})
            ((MeshInstance3D)diverter.FindChild(name,true,false)).Visible=false;
        var bearing=(MeshInstance3D)diverter.FindChild("DIVERTER_PIVOT",true,false);
        bearing.Position=new(-.18f,1.09f,.44f);
        var gate=new Node3D { Name="LuggageGatePivot", Position=new(-.18f,1.10f,.44f) };
        diverter.AddChild(gate);
        var arm=(MeshInstance3D)diverter.FindChild("KIN_DIVERTER_ARM",true,false);
        arm.Owner=null;arm.GetParent().RemoveChild(arm);gate.AddChild(arm);
        // Local X is the actual long dimension of the delivered mesh. A hinge
        // at its end leaves the normal lane clear at the parked zero angle.
        arm.Transform=new Transform3D(Basis.Identity,new Vector3(.66125f,0,0));
        arm.Scale=new(1.15f,1,1);
        var steel=new StandardMaterial3D {AlbedoColor=new Color(.40f,.44f,.47f),Metallic=.65f};
        var blue=new StandardMaterial3D {AlbedoColor=new Color(.05f,.24f,.4f),Metallic=.45f};
        diverter.AddChild(new MeshInstance3D {Name="LUGGAGE_drive_mount",Position=new(-.18f,.96f,.44f),
            Mesh=new BoxMesh {Size=new(.28f,.05f,.24f)},MaterialOverride=blue});
        diverter.AddChild(new MeshInstance3D {Name="LUGGAGE_drive_housing",Position=new(-.18f,1.005f,.44f),
            Mesh=new CylinderMesh {TopRadius=.10f,BottomRadius=.10f,Height=.04f},MaterialOverride=steel});
        diverter.AddChild(new MeshInstance3D {Name="LUGGAGE_drive_coupling",Position=new(-.18f,1.0275f,.44f),
            Mesh=new CylinderMesh {TopRadius=.035f,BottomRadius=.035f,Height=.025f},MaterialOverride=steel});
        foreach(var mesh in diverter.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()) {
            // Match crown height to the scale/infeed/receiver deck without
            // lifting the grounded legs. Lower the rear rail below the payload
            // underside so a future reject transfer can cross the side bridge.
            if(mesh.Name.ToString().StartsWith("ROLLER_"))mesh.Position+=Vector3.Down*.045f;
            if(mesh.Name.ToString().StartsWith("SIDE_") && mesh.Position.Z<0)mesh.Position+=Vector3.Down*.055f;
        }
    }
}
