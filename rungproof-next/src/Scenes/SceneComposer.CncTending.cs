using System;
using System.Linq;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateCncTendingRobot(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "robotics.robot.six-axis-medium.v1");
        var handling = new PalletRobotMotion
        {
            Name = "PalletRobotMotion", GripHeightM = 0.17f,
            DesiredToolBasis = new Basis(Vector3.Forward, Vector3.Up, Vector3.Right),
            InitialJointAngles = [0, 0.5f, -2, 0, 1.5f, 0],
            ParkOffset = new Vector3(0, 2.8f, -2),
        };
        model.AddChild(handling);
        handling.InitializeModel();
        return model;
    }

    private static void ConfigureCncTendingStock(Node3D root, SceneDefinition scene)
    {
        var machine = root.GetNode<Node3D>("cnc_machine");
        var load = root.GetNode<Node3D>("cnc_workpiece");
        var stock = machine.FindChild("WORK_stock", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("CNC tending installation requires the delivered WORK_stock.");
        var definition = scene.Equipment.Single(equipment => equipment.Id == "cnc_workpiece");
        var size = NumberArray(definition.Config, "size", new[] { 0.4, 0.14, 0.252 });
        // Move the actual billet into the scene's addressable load root. The
        // machine's vise stays installed; it must never travel on the belt.
        var bounds = stock.Transform * stock.GetAabb();
        var basis = Basis.FromScale(new Vector3((float)size[0], (float)size[1], (float)size[2]) / bounds.Size) * stock.Basis;
        stock.Owner = null;
        stock.GetParent().RemoveChild(stock);
        load.AddChild(stock);
        stock.Transform = new Transform3D(basis,
            new Vector3(0, (float)size[1] / 2, 0) - basis * stock.GetAabb().GetCenter());

        // The delivered jaws' inner faces are Z=.489/.741. A seated billet
        // uses their center Z=.615 and Y=1.66 bottom. Fill the visible 195 mm
        // gap from the existing vise base to that bearing plane with a shoe.
        var viseBase = (MeshInstance3D)machine.FindChild("WORK_vise_base", true, false);
        AddBox(machine, new Vector3(0.42f, 0.195f, 0.252f),
            new Vector3(0, 1.5625f, 0.615f), viseBase.GetActiveMaterial(0)).Name = "WORK_stock_bearing_shoe";
        ConfigureCncAccess(machine);
    }

    private static void ConfigureCncAccess(Node3D machine)
    {
        // All three adapters use the scene's explicit access position: 0 is
        // closed/tool down, 1 is open/tool retracted. They never self-advance
        // from cnc_run. The existing spindle still owns tool rotation.
        var parts = machine.FindChildren("*", "", true, false).OfType<Node3D>().ToArray();
        void Mount(Node3D part, Node3D parent)
        {
            var local = part.Transform;
            for (var ancestor = part.GetParent(); ancestor != machine; ancestor = ancestor.GetParent())
                if (ancestor is Node3D spatial) local = spatial.Transform * local;
            part.Owner = null;
            part.Reparent(parent, false);
            part.Transform = local; // new assembly roots have identity transforms
        }
        var steel = ((MeshInstance3D)machine.FindChild("WORK_table", true, false)).GetActiveMaterial(0);
        foreach (var (side, sign) in new[] { ("left", -1f), ("right", 1f) })
        {
            var assembly = new Node3D { Name = $"KIN_cnc_door_{side}" };
            machine.AddChild(assembly);
            foreach (var part in parts.Where(part => part.Name == $"DOOR_glazing_{side}"
                || part.Name.ToString().StartsWith($"DOOR_{side}_", StringComparison.Ordinal)
                || (side == "left" && part.Name == "DOOR_center_interlock"))) Mount(part, assembly);
            // Delivered rollers were at floor height. Seat them on the extended
            // top track and connect them to each moving upper rail.
            var roller = (Node3D)machine.FindChild($"KIN_door_roller_{(sign < 0 ? "-0_82" : "0_82")}", true, false);
            Mount(roller, assembly);
            roller.Position = new Vector3(sign * 0.65f, 2.825f, 0.76f);
            AddBox(assembly, new Vector3(0.04f, 0.15f, 0.04f),
                new Vector3(sign * 0.65f, 2.74f, 0.875f), steel).Name = $"DOOR_{side}_hanger";
            AddBox(assembly, new Vector3(0.04f, 0.04f, 0.14f),
                new Vector3(sign * 0.65f, 2.805f, 0.825f), steel).Name = $"DOOR_{side}_hanger_bracket";
            machine.AddChild(new EquipmentMotionController
            {
                Name = $"CncDoor{side}Motion", Kind = EquipmentMotionController.MotionKind.LinearX,
                TargetPrefix = assembly.Name, TravelM = sign * 0.78f, AutonomousPositionTravel = false,
            });
        }
        var track = (MeshInstance3D)machine.FindChild("DOOR_top_track", true, false);
        track.Scale *= new Vector3(3.9f / 2.02f, 1, 1);
        foreach (var sign in new[] { -1f, 1f })
            AddBox(machine, new Vector3(0.74f, 0.1f, 0.12f),
                new Vector3(sign * 1.48f, 2.80f, 0.76f), steel).Name = $"DOOR_track_support_{sign}";

        // Retract only the nose/tool stack into its fixed head. Moving the
        // entire spindle cartridge would push it through the enclosure roof.
        var tool = new Node3D { Name = "KIN_cnc_tool_clearance" };
        machine.AddChild(tool);
        foreach (var part in parts.Where(part => part.Name == "SPINDLE_nose"
            || part.Name.ToString().StartsWith("TOOL_BT40", StringComparison.Ordinal)
            || part.Name == "TOOL_collet_nut" || part.Name.ToString().StartsWith("TOOL_endmill", StringComparison.Ordinal)
            || part.Name.ToString().StartsWith("TOOL_flute_", StringComparison.Ordinal))) Mount(part, tool);
        // Preserve the existing rotating spindle as the tool assembly's parent.
        tool.Reparent((Node3D)machine.FindChild("KIN_spindle", true, false), false);
        // KIN_spindle has a nonzero authored origin; undo that parent offset.
        var spindle = (Node3D)tool.GetParent();
        tool.Transform = spindle.Transform.AffineInverse() * tool.Transform;
        machine.AddChild(new EquipmentMotionController
        {
            Name = "CncToolClearanceMotion", Kind = EquipmentMotionController.MotionKind.LinearY,
            TargetPrefix = tool.Name, TravelM = 0.30f, AutonomousPositionTravel = false,
        });

        // The original black backdrop and coolant tip intruded into the stock.
        // Keep the backdrop behind the vise and route a connected service line
        // outside the stock/finger corridor using the delivered hose material.
        var backdrop = (Node3D)machine.FindChild("WORK_envelope", true, false);
        backdrop.Position += new Vector3(0, 0, -0.14f);
        // Seat the jaw bolts on the wider jaw body, outside the billet's
        // X footprint; their former high inner edges penetrated the stock.
        foreach (var bolt in parts.Where(part => part.Name.ToString().StartsWith("WORK_vise_clamp_bolt_", StringComparison.Ordinal)))
            bolt.Position += new Vector3(MathF.Sign(bolt.Position.X) * 0.04f, -0.08f, 0);
        foreach (var name in new[] { "COOLANT_nozzle", "COOLANT_nozzle_tip" })
            ((Node3D)machine.FindChild(name, true, false)).Position += new Vector3(-0.20f, 0, 0);
        var hose = (MeshInstance3D)machine.FindChild("COOLANT_hose", true, false);
        hose.Visible = false;
        var route = new[] { new Vector3(-0.48f, 2.34f, -0.20f), new Vector3(-0.46f, 2.20f, 0.04f),
            new Vector3(-0.40f, 2.04f, 0.35f), new Vector3(-0.36f, 1.94f, 0.58f) };
        for (var index = 0; index < route.Length - 1; index++)
        {
            var delta = route[index + 1] - route[index];
            var y = delta.Normalized();
            var x = Vector3.Up.Cross(y).Normalized();
            machine.AddChild(new MeshInstance3D
            {
                Name = $"COOLANT_hose_tending_{index}",
                Mesh = new CylinderMesh { TopRadius = 0.026f, BottomRadius = 0.026f, Height = delta.Length(), RadialSegments = 16 },
                MaterialOverride = hose.GetActiveMaterial(0),
                Transform = new Transform3D(new Basis(x, y, x.Cross(y)), (route[index] + route[index + 1]) / 2),
            });
        }
    }
}
