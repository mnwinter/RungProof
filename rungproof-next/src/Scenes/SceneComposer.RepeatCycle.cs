using System.Linq;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateRepeatCycleMachine(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var machine = CreateControlledAsset(equipment, candidates, "machining.machine.enclosed-center.v1", false,
            EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_spindle", speedRpm: 3200);
        // Reuse the reviewed enclosed-machine access/workholding corrections.
        // Doors stay closed and the tool-clearance adapter stays at zero;
        // this cycle moves the complete Z head, including its connected motor.
        ConfigureCncAccess(machine);
        // The delivered static prop puts its spindle against the closed door
        // and its motor only 70 mm below the roof. Install a deeper front bay
        // and a 100 mm taller enclosure for the declared moving Z stroke.
        var roof = (MeshInstance3D)machine.FindChild("MACHINE_roof", true, false);
        roof.Position += Vector3.Up * .1f;
        foreach (var panel in machine.FindChildren("MACHINE_*panel*", "", true, false).OfType<Node3D>())
        {
            panel.Position += Vector3.Up * .05f;
            panel.Scale *= new Vector3(1, 3.08f / 2.98f, 1);
            if (panel.Name.ToString().StartsWith("MACHINE_side_panel_"))
            {
                panel.Position += Vector3.Back * .175f;
                panel.Scale *= new Vector3(1, 1, 2.0f / 1.65f);
            }
        }
        var header = (Node3D)machine.FindChild("MACHINE_front_header", true, false);
        header.Position += new Vector3(0, .05f, .35f);
        header.Scale *= new Vector3(1, .48f / .38f, 1);
        foreach (var part in machine.FindChildren("*", "", true, false).OfType<Node3D>().Where(part =>
            part.Name.ToString() is "KIN_cnc_door_left" or "KIN_cnc_door_right" or "DOOR_top_track" or "MACHINE_nameplate"
            || part.Name.ToString().StartsWith("DOOR_track_support_") || part.Name.ToString().StartsWith("MACHINE_door_guide_")))
            part.Position += Vector3.Back * .35f;
        // This decorative black backdrop is a solid slab through the Z head,
        // not a cavity. The actual rear enclosure panel remains installed.
        ((Node3D)machine.FindChild("WORK_envelope", true, false)).Visible = false;
        ((Node3D)machine.FindChild("COOLANT_manifold", true, false)).Position += Vector3.Left * .2f;
        var hosePoints = new[] { new Vector3(-.68f, 2.42f, -.28f), new Vector3(-.64f, 2.20f, .04f),
            new Vector3(-.40f, 2.04f, .35f), new Vector3(-.36f, 1.94f, .58f) };
        for (var index = 0; index < hosePoints.Length - 1; index++)
        {
            var hose = (MeshInstance3D)machine.FindChild($"COOLANT_hose_tending_{index}", true, false);
            var delta = hosePoints[index + 1] - hosePoints[index];
            var y = delta.Normalized(); var x = Vector3.Up.Cross(y).Normalized();
            ((CylinderMesh)hose.Mesh).Height = delta.Length();
            hose.Transform = new Transform3D(new Basis(x, y, x.Cross(y)), (hosePoints[index] + hosePoints[index + 1]) / 2);
        }
        var stock = (MeshInstance3D)machine.FindChild("WORK_stock", true, false);
        stock.Position = new Vector3(0, 1.73f, .615f);
        stock.Scale = new Vector3(stock.Scale.X, stock.Scale.Y, stock.Scale.Z * .252f / .22f);
        var vise = (MeshInstance3D)machine.FindChild("WORK_vise_base", true, false);
        AddBox(machine, new Vector3(.42f, .195f, .252f), new Vector3(0, 1.5625f, .615f),
            vise.GetActiveMaterial(0)).Name = "WORK_stock_bearing_shoe";

        var assembly = new Node3D { Name = "KIN_repeat_cycle_head" };
        machine.AddChild(assembly);
        var parts = machine.FindChildren("*", "", true, false).OfType<Node3D>().Where(part =>
            part.Name.ToString().StartsWith("AXIS_Z_truck_") || part.Name == "AXIS_Z_carriage"
            || part.Name == "SPINDLE_head" || part.Name == "SPINDLE_motor"
            || part.Name == "SPINDLE_drive_cover" || part.Name == "KIN_spindle").ToArray();
        foreach (var part in parts)
        {
            var local = part.Transform;
            for (var ancestor = part.GetParent(); ancestor != machine; ancestor = ancestor.GetParent())
                if (ancestor is Node3D spatial) local = spatial.Transform * local;
            part.Owner = null; part.Reparent(assembly, false); part.Transform = local;
        }
        machine.AddChild(new EquipmentMotionController
        {
            Name = "RepeatCycleHeadMotion", Kind = EquipmentMotionController.MotionKind.LinearY,
            TargetPrefix = assembly.Name, TravelM = (float)RepeatCyclePlantModel.TravelM,
            AutonomousPositionTravel = false,
        });
        return machine;
    }
}
