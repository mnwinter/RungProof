using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // These exact devices report machine status, not a field measurement or
    // traffic instruction. Separate named heads/outputs remain separate on the
    // common operator enclosure. Teaching towers use their supported bench.
    private static readonly IReadOnlyDictionary<string, string[]> CabinetStatusTowers =
        new Dictionary<string, string[]>
        {
            ["conveyor-cell"] = ["cell_stacklight"],
            ["lab-10-01-drive-alarm-code-string"] = ["indicator_2", "indicator_9"],
            ["lab-10-02-chicken-label-print"] = ["indicator_3", "indicator_11"],
            ["lab-10-03-vision-package-sorter"] = ["indicator_4", "indicator_12"],
            ["lab-10-04-motor-enum-state"] = ["indicator_2", "indicator_5"],
            ["lab-10-05-motor-struct-data"] = ["indicator_2", "indicator_9"],
            ["lab-10-06-ten-motor-array-startup"] = ["indicator_5", "indicator_12"],
            ["lab-11-06-wastewater-collection"] = ["indicator_5", "indicator_13", "training_accessory_9"],
            ["lab-11-07-multi-conveyor-pallet-route"] = ["indicator_12", "indicator_13", "indicator_14"],
            ["lab-11-11-service-elevator"] = ["indicator_3", "indicator_10"],
            ["lab-11-19-powder-batch-mixer"] = ["indicator_5", "indicator_14", "indicator_15"],
            ["lab-2-11-inbound-tote-stop"] = ["inbound_status"],
            ["lab-2-13-coolant-jug-fill"] = ["fill_status"],
            ["lab-2-14-sump-pump"] = ["sump_status"],
            ["lab-2-15-fume-extractor"] = ["speed_display"],
            ["lab-2-16-safe-drill"] = ["drill_status"],
            ["lab-2-17-pallet-robot"] = ["robot_cell_status"],
            ["lab-2-18-pallet-pickup"] = ["pickup_status"],
            ["lab-2-20-bottle-shuttle"] = ["shuttle_status"],
            ["lab-2-21-tote-finishing"] = ["line_status"],
            ["lab-2-22-dual-spindle"] = ["dual_drill_status"],
            ["lab-2-23-parcel-sorter"] = ["sorter_status"],
            ["lab-2-24-robot-cnc"] = ["cnc_cell_status"],
            ["lab-3-01-guarded-pallet-transfer"] = ["indicator_5", "indicator_11"],
            ["lab-3-02-robot-cell-safe-restart"] = ["indicator_3", "indicator_10"],
            ["lab-4-03-repeat-cycle-counter"] = ["indicator_2", "indicator_4"],
            ["lab-4-04-sequence-light-tower"] = ["indicator_2"],
            ["lab-4-07-parking-garage-entry"] = ["available_lamp"],
            ["lab-4-09-chain-drive-lift"] = ["indicator_3", "indicator_11"],
            ["lab-4-10-cookie-packaging"] = ["indicator_3", "indicator_11"],
            ["lab-4-11-barrel-fill-station"] = ["indicator_4", "indicator_11"],
            ["lab-4-12-cable-cut-length"] = ["indicator_3", "indicator_12"],
            ["lab-6-07-luggage-weight-sort"] = ["indicator_3", "indicator_11"],
            ["lab-9-10-box-volume"] = ["indicator_2"],
            // These are charge_enable and energy_session_active, not one head
            // per bay. Preserve their actual meaning at the operator cabinet.
            ["lab-9-12-ev-charging-manager"] = ["indicator_2", "indicator_10"],
            ["scene-1-conveyor-stop"] = ["scene1_stacklight"],
            ["scene-2-conveyor-pusher"] = ["scene2_stacklight"],
            ["tank-high-low"] = ["hl_stacklight"],
            ["tank-level"] = ["tank_stacklight"],
            ["tank-radar"] = ["radar_stacklight"],
        };

    private static void MountCabinetStatusTowers(Node3D root, SceneDefinition scene, Node3D installation)
    {
        if (!CabinetStatusTowers.TryGetValue(scene.Id, out var ids)) return;
        var body = installation.GetNode<MeshInstance3D>("OperatorCabinet_Body");
        var bodyLocal = body.Transform * body.GetAabb();
        var finish = RungProof.Next.App.StudioSceneMaterials.CreateDetailed("#656E68", "paint");
        var mountedIds = new Godot.Collections.Array<string>();
        var widths = ids.Select(id => MathF.Max(.28f, PrepareCabinetStatusHead(root, scene, id).Size.X)).ToArray();
        const float gap = .16f;
        var totalWidth = widths.Sum() + (ids.Length - 1) * gap;
        var cursor = -totalWidth / 2;
        for (var i = 0; i < ids.Length; i++)
        {
            var node = root.GetNode<Node3D>(ids[i].Replace('-', '_'));
            // Keep original scale/orientation, equipment root and controller
            // children. Only translate the retained complete head assembly.
            node.Position = Vector3.Zero;
            var head = InstallationBounds(node);
            var x = cursor + widths[i] / 2;
            cursor += widths[i] + gap;
            var seat = new Vector3(x, bodyLocal.End.Y + .08f, bodyLocal.GetCenter().Z);
            var target = installation.Transform * seat;
            node.Position = target - new Vector3(head.GetCenter().X, head.Position.Y, head.GetCenter().Z);
            AddBox(installation, new Vector3(.28f, .04f, .28f),
                new Vector3(x, bodyLocal.End.Y + .02f, seat.Z), finish).Name = "StatusTowerPlate_" + ids[i];
            AddBox(installation, new Vector3(.07f, .04f, .07f),
                new Vector3(x, bodyLocal.End.Y + .06f, seat.Z), finish).Name = "StatusTowerCoupler_" + ids[i];
            node.SetMeta("installationKind", "cabinetStatusTower");
            node.SetMeta("statusTowerSupport", "StatusTowerCoupler_" + ids[i]);
            mountedIds.Add(ids[i]);
            GD.Print($"STATUS_TOWER_MOUNT_READY scene={scene.Id} equipment={ids[i]} role=machine-status support=operator-cabinet pose={node.Position} authored-head-basis=preserved");
        }
        installation.SetMeta("cabinetStatusTowerIds", mountedIds);
    }

    // Measure the same retained heads before sizing the enclosure roof. The
    // single wastewater beacon is an exact recognized asset exception, never
    // a generic trainingAccessory conversion.
    private static Aabb PrepareCabinetStatusHead(Node3D root, SceneDefinition scene, string id)
    {
        var item = scene.Equipment.Single(e => e.Id == id);
        var recognizedBeacon = scene.Id == "lab-11-06-wastewater-collection"
            && id == "training_accessory_9" && item.Type == "trainingAccessory"
            && Text(item.Config, "catalogAssetId", "") == "training.accessory.alarm_beacon.v1";
        if (!recognizedBeacon && (item.Type != "indicator" || StringArray(item.Config, "colors", []).Length < 2))
            throw new InvalidOperationException("Status mount inventory is not a recognized head: " + id);
        var node = root.GetNode<Node3D>(SafeNodeName(id));
        foreach (var mesh in node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            if (mesh.Name == "POLE" || mesh.Name.ToString().StartsWith("BASE_", StringComparison.Ordinal))
                mesh.Visible = false;
        return InstallationBounds(node);
    }

    private static float CabinetStatusHeadRoofWidth(Node3D root, SceneDefinition scene)
    {
        if (!CabinetStatusTowers.TryGetValue(scene.Id, out var ids)) return 0;
        return ids.Sum(id => MathF.Max(.28f, PrepareCabinetStatusHead(root, scene, id).Size.X))
            + (ids.Length - 1) * .16f + .20f;
    }
}
