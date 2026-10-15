using System;
using System.Linq;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateDualSpindleHead(SceneEquipment equipment, AssetCatalogDocument catalog, bool run)
    {
        var model = CreateMappedAsset(equipment, catalog, "machining.drill-press.pedestal.v1");
        // Retain the delivered column, head, guard, feed assembly and spindle.
        // The common bed replaces both separate tables/vises/test coupons.
        foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray())
            if (mesh.Name.ToString().StartsWith("DRILL_table", StringComparison.Ordinal)
                || mesh.Name.ToString().StartsWith("DRILL_vise", StringComparison.Ordinal)
                || mesh.Name == "DRILL_workpiece")
            {
                mesh.GetParent().RemoveChild(mesh);
                mesh.Free();
            }
        // Rotate each top-level assembly exactly once, including the nested
        // spindle. Columns stand behind the common stock rather than inside it.
        var turn = new Transform3D(new Basis(Vector3.Up, -Mathf.Pi / 2), Vector3.Zero);
        foreach (var child in model.GetChildren().OfType<Node3D>()) child.Transform = turn * child.Transform;
        // Retracted tips clear the passing slide plate by 50 mm. The 330 mm
        // feed then enters stock by 25 mm while retaining quill/bearing overlap.
        var spindle = (Node3D)model.FindChild("KIN_spindle", true, false);
        spindle.Position += Vector3.Up * 0.25f;
        model.AddChild(new EquipmentMotionController
        {
            Name = "DualSpindleFeed", Kind = EquipmentMotionController.MotionKind.SpindleFeed,
            TargetPrefix = "KIN_spindle", SpeedRpm = 900, TravelM = -0.33f, RunCommand = run,
        });
        return model;
    }

    private static void PlaceDualPart(MeshInstance3D mesh, Vector3 size, Vector3 center)
    {
        var bounds = mesh.Transform * mesh.GetAabb();
        var basis = Basis.FromScale(size / bounds.Size) * mesh.Basis;
        mesh.Transform = new Transform3D(basis, center - basis * mesh.GetAabb().GetCenter());
    }

    private static Node3D CreateDualSpindleBed(SceneEquipment equipment, AssetCatalogDocument catalog)
    {
        var bed = CreateMappedAsset(equipment, catalog, "facility.workbench.2400mm.v1");
        var parts = bed.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        var legs = parts.Where(part => part.Name.ToString().StartsWith("BENCH_leg", StringComparison.Ordinal)).ToArray();
        var steel = legs[0].GetActiveMaterial(0);
        foreach (var part in parts)
            part.Visible = legs.Contains(part) || part.Name == "BENCH_top";
        var top = parts.Single(part => part.Name == "BENCH_top");
        PlaceDualPart(top, new Vector3(6.28f, 0.10f, 1.35f), new Vector3(1.16f, 1.40f, 0));
        top.MaterialOverride = steel;
        foreach (var leg in legs)
        {
            var center = (leg.Transform * leg.GetAabb()).GetCenter();
            PlaceDualPart(leg, new Vector3(0.10f, 1.25f, 0.10f),
                new Vector3(center.X < 0 ? -1.86f : 4.06f, 0.625f, MathF.CopySign(0.55f, center.Z)));
        }
        foreach (var side in new[] { -1, 1 })
            AddBox(bed, new Vector3(6.12f, 0.10f, 0.10f), new Vector3(1.1f, 1.30f, side * 0.55f), steel);
        foreach (var x in new[] { -1.86f, 4.06f })
            AddBox(bed, new Vector3(0.10f, 0.10f, 1.20f), new Vector3(x, 1.30f, 0), steel);
        return bed;
    }

    private static void ConfigureDualSpindleSlide(Node3D model)
    {
        var parts = model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        var plate = parts.Single(part => part.Name == "KIN_pusher_plate");
        var bounds = plate.Transform * plate.GetAabb();
        // Its lower edge bears against the fixture subplate and clears the
        // bed; the retained carriage/yoke/guide shafts remain above the bed.
        PlaceDualPart(plate, new Vector3(bounds.Size.X, 0.36f, bounds.Size.Z),
            new Vector3(bounds.GetCenter().X, 1.63f, bounds.GetCenter().Z));
        foreach (var part in parts.Where(part => part.Name.ToString().StartsWith("KIN_pusher_plate_label", StringComparison.Ordinal)))
            part.Visible = false;
        foreach (var part in parts.Where(part => part.Name.ToString().StartsWith("KIN_pusher_PLATE_bolt", StringComparison.Ordinal)))
            part.Position += Vector3.Up * (1.65f - (part.Transform * part.GetAabb()).GetCenter().Y);
    }
}
