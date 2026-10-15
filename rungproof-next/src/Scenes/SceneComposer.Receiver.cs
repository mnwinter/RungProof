using System;
using System.Linq;
using Godot;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateContainerReceiver(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        if (Text(equipment.Config, "installation", string.Empty) == "dualSpindleBed")
            return CreateDualSpindleBed(equipment, candidates);
        if (Text(equipment.Config, "installation", string.Empty) == "palletHandlingReceiver")
            return CreateRaisedPalletReceiver(equipment, candidates);
        if (Text(equipment.Config, "installation", string.Empty) != "flatCartonReceiver")
            return CreateMappedAsset(equipment, candidates, "material-handling.receiver.container-two-position.v1");

        // Reuse the authored workbench's top, four legs and shelf. Its vise
        // and drawer do not belong in a carton landing path. The composed
        // steel top extends to the belt edge above the lower conveyor rail.
        var model = CreateMappedAsset(equipment, candidates, "facility.workbench.2400mm.v1");
        var meshes = model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        var top = meshes.Single(mesh => mesh.Name.ToString() == "BENCH_top");
        var legs = meshes.Where(mesh => mesh.Name.ToString().StartsWith("BENCH_leg", StringComparison.Ordinal)).ToArray();
        if (legs.Length != 4) throw new InvalidOperationException("Carton receiver requires four workbench legs.");
        var steel = legs[0].GetActiveMaterial(0);
        void Place(MeshInstance3D mesh, Vector3 size, Vector3 center)
        {
            var bounds = mesh.Transform * mesh.GetAabb();
            var basis = Basis.FromScale(size / bounds.Size) * mesh.Basis;
            mesh.Transform = new Transform3D(basis, center - basis * mesh.GetAabb().GetCenter());
        }
        foreach (var mesh in meshes)
            mesh.Visible = mesh == top || legs.Contains(mesh) || mesh.Name.ToString() == "BENCH_shelf";
        Place(top, new Vector3(1.40f, 0.016f, 1.805f), new Vector3(0, 0.892f, -0.1025f));
        top.MaterialOverride = steel;
        foreach (var leg in legs)
        {
            // Keep the front supports beyond the conveyor's floor bracing;
            // the deck extends over its lower side rail to meet the belt.
            var center = (leg.Transform * leg.GetAabb()).GetCenter();
            Place(leg, new Vector3(0.10f, 0.804f, 0.10f),
                new Vector3(MathF.CopySign(0.53f, center.X), 0.402f, center.Z < 0 ? -0.30f : 0.68f));
        }
        Place(meshes.Single(mesh => mesh.Name.ToString() == "BENCH_shelf"),
            new Vector3(1.16f, 0.08f, 1.08f), new Vector3(0, 0.25f, 0.19f));
        void Beam(string name, Vector3 size, Vector3 center) => model.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = new BoxMesh { Size = size }, Position = center, MaterialOverride = steel,
        });
        foreach (var side in new[] { -1, 1 })
        {
            Beam($"RECEIVER_side_beam_{side}", new Vector3(0.08f, 0.08f, 1.06f), new Vector3(side * 0.53f, 0.844f, 0.19f));
            Beam($"RECEIVER_end_beam_{side}", new Vector3(1.14f, 0.08f, 0.08f), new Vector3(0, 0.844f, side < 0 ? -0.30f : 0.68f));
        }
        return model;
    }
}
