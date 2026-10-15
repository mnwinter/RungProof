using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Static teaching hardware: no new controller, pick target, signal or load path.
    private static void ConfigureAccessorySupports(Node3D root, SceneDefinition scene)
    {
        if (scene.Id != "lab-3-01-guarded-pallet-transfer"
            && scene.Id != "lab-11-19-powder-batch-mixer") return;
        var item = scene.Equipment.Single(e => e.Id == "training_accessory_8");
        var expectedAsset = scene.Id == "lab-3-01-guarded-pallet-transfer"
            ? "training.accessory.safety_relay_status_beacon.v1" : "training.accessory.load_cell.v1";
        if (!item.Config.TryGetProperty("catalogAssetId", out var asset) || asset.GetString() != expectedAsset)
            throw new InvalidOperationException($"Static accessory contract changed: {scene.Id}/{item.Id}");
        var retained = root.GetNode<Node3D>(SafeNodeName(item.Id));
        var original = retained.Transform;
        var metal = Material(new Color("505b61").SrgbToLinear(), .35f, .65f);
        var back = Material(new Color("aeb9b5").SrgbToLinear(), .25f, .65f);
        var dark = Material(new Color("263035").SrgbToLinear(), .1f, .7f);
        var support = new Node3D { Name = scene.Id == "lab-3-01-guarded-pallet-transfer"
            ? "AccessorySupport_RelayTeachingBay" : "AccessorySupport_LoadCellExhibit" };
        root.AddChild(support);

        // The helper uses the existing composer measurement convention: bounds
        // include every parent transform through the retained equipment root.
        Aabb Part(string name)
        {
            var mesh = retained.FindChildren("*", "MeshInstance3D", true, false)
                .OfType<MeshInstance3D>().Single(m => m.Name == name && m.Visible && m.Mesh is not null);
            var pose = mesh.Transform;
            for (var parent = mesh.GetParent(); parent != retained; parent = parent.GetParent())
                if (parent is Node3D spatial) pose = spatial.Transform * pose;
            return retained.Transform * pose * mesh.GetAabb();
        }
        void Box(string name, Vector3 size, Vector3 center, Material finish)
            => AddBox(support, size, center, finish).Name = name;
        void Caption(string text, float width, Vector3 center)
        {
            const float height = .15f;
            Box("StaticExhibitCaptionPlate", new Vector3(width, height, .015f), center, dark);
            var label = new Label3D { Name = "StaticExhibitCaption", Text = text,
                Position = center + Vector3.Back * .010f, FontSize = 48, PixelSize = .0015f,
                OutlineSize = 0, Modulate = Colors.White,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var ink = ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, label.FontSize);
            label.PixelSize = MathF.Min(label.PixelSize, MathF.Min((width - .04f) / MathF.Max(1, ink.X),
                (height - .03f) / MathF.Max(1, ink.Y)));
            support.AddChild(label);
        }

        if (scene.Id == "lab-3-01-guarded-pallet-transfer")
        {
            var installation = root.GetNode<Node3D>("ControlInstallation");
            var cabinet = installation.GetNode<MeshInstance3D>("OperatorCabinet_Body");
            var cabinetBounds = installation.Transform * cabinet.Transform * cabinet.GetAabb();
            var full = InstallationBounds(retained);
            var rail = Part("DIN_RAIL");
            const float padding = .10f;
            var width = full.Size.X + 2 * padding;
            // Adjacent open bay keeps the existing closed operator face clear.
            var x = cabinetBounds.Position.X - .22f - width / 2;
            var delta = new Vector3(x - full.GetCenter().X, .75f - full.Position.Y,
                cabinetBounds.End.Z - rail.Position.Z);
            retained.Position += delta;
            full = InstallationBounds(retained);
            rail = Part("DIN_RAIL");
            // The retained GLB's thick relay base extends behind its rail.
            // Put the plate behind the complete asset, with two rigid spacers
            // at exposed rail ends; never pass a plate through the relay body.
            var plateFront = full.Position.Z;
            var plateBack = plateFront - .035f;
            var front = full.End.Z + .06f;
            var bottom = full.Position.Y - padding;
            var top = full.End.Y + padding;
            var depth = front - plateBack;
            if (depth <= .04f || bottom <= .10f) throw new InvalidOperationException("Invalid relay bay envelope");
            Box("RelayBay_Backplate", new Vector3(width, top - bottom, .035f),
                new Vector3(x, (top + bottom) / 2, plateFront - .0175f), back);
            var spacerDepth = rail.Position.Z - plateFront;
            if (spacerDepth <= .001f) throw new InvalidOperationException("Relay spacer depth contract changed");
            var relayBase = Part("RELAY_BASE");
            foreach (var side in new[] { -1f, 1f })
            {
                const float spacerWidth = .08f;
                var spacerX = side < 0 ? rail.Position.X + spacerWidth : rail.End.X - spacerWidth;
                if (spacerX + spacerWidth / 2 > relayBase.Position.X
                    && spacerX - spacerWidth / 2 < relayBase.End.X)
                    throw new InvalidOperationException("Rail spacer overlaps retained relay base");
                Box("RelayBay_RailSpacer_" + (side < 0 ? "Left" : "Right"),
                    new Vector3(spacerWidth, MathF.Min(.04f, rail.Size.Y), spacerDepth),
                    new Vector3(spacerX, rail.GetCenter().Y, plateFront + spacerDepth / 2), metal);
            }
            foreach (var side in new[] { -1f, 1f })
                Box("RelayBay_Side_" + (side < 0 ? "Left" : "Right"), new Vector3(.035f, top - bottom, depth),
                    new Vector3(x + side * (width / 2 + .0175f), (top + bottom) / 2, (plateBack + front) / 2), metal);
            Box("RelayBay_Bottom", new Vector3(width + .07f, .035f, depth),
                new Vector3(x, bottom - .0175f, (plateBack + front) / 2), metal);
            Box("RelayBay_Top", new Vector3(width + .07f, .035f, depth),
                new Vector3(x, top + .0175f, (plateBack + front) / 2), metal);
            // Full-width grounded plinth bears both side walls and bottom shelf.
            Box("RelayBay_GroundedPlinth", new Vector3(width + .07f, bottom - .035f, depth),
                new Vector3(x, (bottom - .035f) / 2, (plateBack + front) / 2), metal);
            Caption("STATIC RELAY EXHIBIT", width - .04f, new Vector3(x, bottom - .12f, front + .012f));
            retained.SetMeta("accessoryContactMesh", "DIN_RAIL");
            retained.SetMeta("accessoryBearingMesh", "RelayBay_RailSpacer_Left");
            retained.SetMeta("accessorySupportKind", "staticRelayTeachingBay");
            var associated = installation.GetMeta("installationEquipmentIds").AsGodotArray();
            foreach (var name in new[] { item.Id, support.Name.ToString() })
                if (!associated.Any(v => v.AsString() == name)) associated.Add(name);
            installation.SetMeta("installationEquipmentIds", associated);
        }
        else
        {
            var beam = Part("LOAD_CELL_beam");
            // Keep authored X/Z and basis/scale; only lift the static exhibit.
            const float worktop = .90f;
            const float padHeight = .04f;
            retained.Position += Vector3.Up * (worktop + padHeight - beam.Position.Y);
            beam = Part("LOAD_CELL_beam");
            var full = InstallationBounds(retained);
            var width = full.Size.X + .18f;
            var depth = MathF.Max(.60f, full.Size.Z + .18f);
            var center = new Vector3(full.GetCenter().X, 0, full.GetCenter().Z);
            Box("LoadCellExhibit_Worktop", new Vector3(width, .06f, depth), center + Vector3.Up * (worktop - .03f), back);
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
            {
                var at = center + new Vector3(sx * (width / 2 - .09f), 0, sz * (depth / 2 - .09f));
                Box($"LoadCellExhibit_Leg_{sx}_{sz}", new Vector3(.06f, worktop - .06f, .06f), at + Vector3.Up * ((worktop - .06f) / 2), metal);
                Box($"LoadCellExhibit_Foot_{sx}_{sz}", new Vector3(.12f, .03f, .12f), at + Vector3.Up * .015f, metal);
            }
            // A nonfunctional two-pad cradle: no inferred fixed-end clamp or load.
            foreach (var side in new[] { -1f, 1f })
                Box("LoadCellExhibit_Cradle_" + (side < 0 ? "Left" : "Right"),
                    new Vector3(MathF.Min(.12f, beam.Size.X * .20f), padHeight, beam.Size.Z * .70f),
                    new Vector3(beam.GetCenter().X + side * beam.Size.X * .30f, worktop + padHeight / 2, beam.GetCenter().Z), dark);
            Caption("STATIC LOAD-CELL EXHIBIT", width - .10f,
                new Vector3(center.X, worktop - .14f, center.Z + depth / 2 + .012f));
            retained.SetMeta("accessoryContactMesh", "LOAD_CELL_beam");
            retained.SetMeta("accessoryBearingMesh", "LoadCellExhibit_Cradle_Left");
            retained.SetMeta("accessorySupportKind", "staticLoadCellCradle");
        }
        retained.SetMeta("accessoryOriginalTransform", original);
        retained.SetMeta("accessorySupportNode", support.Name.ToString());
        retained.SetMeta("accessoryStaticUnbound", true);
        support.SetMeta("retainedEquipmentId", item.Id);
        support.SetMeta("accessoryEnvelope", InstallationBounds(retained));
        support.SetMeta("passiveSupport", true);
        GD.Print($"ACCESSORY_SUPPORT_READY scene={scene.Id} equipment={item.Id} support={support.Name} role=static-unbound");
    }
}
