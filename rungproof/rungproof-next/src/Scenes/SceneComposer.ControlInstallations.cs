using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // This is installation presentation, not electrical design or a wiring
    // schematic. Existing equipment roots, KIN members and tags remain intact.
    private static void ConfigureControlInstallation(Node3D root, SceneDefinition scene)
    {
        if (!ControlInstallationPlans.TryGetValue(scene.Id, out var plan))
            throw new InvalidOperationException($"No installation inventory for {scene.Id}");
        var equipment = plan.EquipmentIds.Select(id => scene.Equipment.Single(e => e.Id == id)).ToArray();
        if (equipment.Length == 0) return;
        // A tall teaching tower must not pass through an upper display shelf.
        // Put it on the last tier; keep paired count displays together below.
        if (plan.Bench && equipment.Length > 5)
            equipment = equipment.OrderBy(e => e.Type == "indicator"
                && e.Config.TryGetProperty("colors", out var colors)
                && colors.ValueKind == System.Text.Json.JsonValueKind.Array
                && colors.GetArrayLength() > 1 ? 1 : 0).ToArray();
        var installation = new Node3D { Name = "ControlInstallation" };
        root.AddChild(installation);
        // Only passive supports receive the location finishes. Functional heads
        // keep their authored materials and signal meanings.
        var direction = RungProof.Next.App.SceneEnvironmentArtDirections.ForScene(scene.Id);
        var frame = RungProof.Next.App.StudioSceneMaterials.Create(direction.SupportHex, "paint");
        var top = RungProof.Next.App.StudioSceneMaterials.Create(direction.BenchTopHex, direction.BenchTopStyle);
        var panel = RungProof.Next.App.StudioSceneMaterials.Create(direction.CabinetHex, "paint");
        var door = RungProof.Next.App.StudioSceneMaterials.Create(direction.CabinetHex, "paint");
        if (scene.Id == "lab-2-01-workstation-call")
        {
            // Candidate finish revision only: the bench remains the same
            // geometry, and equipment heads retain their authored materials.
            frame = RungProof.Next.App.StudioSceneMaterials.CreateDetailed("#394746", "paint");
            top = (StandardMaterial3D)RungProof.Next.App.StudioSceneMaterials.CreateDetailed("#9D805F", "wood").Duplicate();
            top.Uv1Scale=Vector3.One*1.2f;
            top.NormalScale=.08f;
            panel = RungProof.Next.App.StudioSceneMaterials.CreateDetailed("#D3D6D1", "paint");
        }
        var wiring = Material(new Color("363c36").SrgbToLinear(), .1f, .7f);
        var compactDryerService = scene.Id == "lab-6-08-hand-dryer";
        var columns = Math.Min(plan.Bench ? 5 : compactDryerService ? 2 : 4, equipment.Length);
        var rows = (equipment.Length + columns - 1) / columns;
        var pitch = plan.Bench || equipment.Any(e => e.Type == "trainingAccessory") ? .70f : .38f;
        var compactColumnWidths = Vector2.Zero;
        if (compactDryerService)
        {
            // Measure the delivered device bodies after removing only the same
            // mounting components hidden below. Keep their original scale/basis;
            // the normal placement loop applies the existing .70 presentation scale.
            var largestBodyWidth = 0f;
            for (var measuredIndex = 0; measuredIndex < equipment.Length; measuredIndex++)
            {
                var item = equipment[measuredIndex];
                var device = root.GetNode<Node3D>(SafeNodeName(item.Id));
                foreach (var mesh in device.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                    if (IsOriginalControlMount(item, mesh.Name.ToString())
                        || IsPanelControlShell(item, mesh.Name.ToString())) mesh.Visible = false;
                var deviceWidth = InstallationBounds(device).Size.X * .70f;
                largestBodyWidth = MathF.Max(largestBodyWidth, deviceWidth);
                if (measuredIndex % 2 == 0) compactColumnWidths.X = MathF.Max(compactColumnWidths.X, deviceWidth);
                else compactColumnWidths.Y = MathF.Max(compactColumnWidths.Y, deviceWidth);
            }
            pitch = MathF.Max(.38f, (compactColumnWidths.X + compactColumnWidths.Y) / 2 + .12f);
            installation.SetMeta("compactServiceMeasuredDeviceWidth", largestBodyWidth);
            installation.SetMeta("compactServiceColumnWidths", compactColumnWidths);
        }
        var rowPitch = compactDryerService ? .38f : .52f;
        // New cabinet heads include long STRING panels, dual meters and a
        // drive body. Measure their retained visible geometry, rather than
        // assuming every accessory fits the old button-sized grid.
        var measuredMountRevision = scene.Id is "lab-10-01-drive-alarm-code-string"
            or "lab-10-02-chicken-label-print" or "lab-10-03-vision-package-sorter"
            or "lab-4-07-parking-garage-entry" or "lab-4-08-package-grouping"
            or "lab-4-09-chain-drive-lift" or "lab-4-10-cookie-packaging"
            or "lab-4-12-cable-cut-length" or "lab-6-07-luggage-weight-sort"
            or "lab-9-11-pallet-counting" or "lab-9-12-ev-charging-manager";
        if (measuredMountRevision)
        {
            foreach (var item in equipment)
            {
                var device = root.GetNode<Node3D>(SafeNodeName(item.Id));
                foreach (var mesh in device.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                    if (IsOriginalControlMount(item, mesh.Name.ToString())
                        || IsPanelControlShell(item, mesh.Name.ToString())) mesh.Visible = false;
                var originalTransform = device.Transform;
                device.Scale *= .70f;
                device.RotationDegrees = new Vector3(item.Type == "indicator" ? 90 : 0, 0, 0);
                device.Position = Vector3.Zero;
                var retained = InstallationBounds(device);
                pitch = MathF.Max(pitch, retained.Size.X + .18f);
                rowPitch = MathF.Max(rowPitch, retained.Size.Y + .15f);
                device.Transform = originalTransform;
            }
            installation.SetMeta("measuredHeadPitch", pitch);
            installation.SetMeta("measuredHeadRowPitch", rowPitch);
        }
        var width = MathF.Max(plan.Bench ? 2.4f : 1.1f, columns * pitch + .30f);
        if (!plan.Bench) width = MathF.Max(width, CabinetStatusHeadRoofWidth(root, scene));
        if (compactDryerService)
            // Centred columns need clearance for the wider of the two bodies on
            // either outer edge; do not force the readout into a guessed box.
            width = MathF.Max(1.1f, pitch + MathF.Max(compactColumnWidths.X, compactColumnWidths.Y) + .20f);
        const float topHeight = .95f;
        var origin = Vector3.Zero;
        if (scene.Id == "equipment-gallery")
            origin = root.GetNode<Node3D>(SafeNodeName("gallery_indicator")).Position;
        var frontSign = scene.Camera.Position[2] >= scene.Camera.Target[2] ? 1f : -1f;
        if (!plan.Bench)
        {
            var field = scene.Equipment.Where(e => !plan.EquipmentIds.Contains(e.Id))
                .Select(e => root.GetNodeOrNull<Node3D>(SafeNodeName(e.Id)))
                .Where(n => n is not null && n.FindChildren("*", "MeshInstance3D", true, false).Count > 0)
                .Select(n => InstallationBounds(n!)).ToArray();
            var bounds = field.Length == 0 ? new Aabb(new Vector3(-.5f, 0, -.5f), Vector3.One)
                : field.Aggregate((a, b) => a.Merge(b));
            var side = scene.Camera.Position[0] >= scene.Camera.Target[0] ? 1f : -1f;
            if (scene.Id is "lab-2-14-sump-pump" or "lab-10-03-vision-package-sorter") side = -1;
            var margin = scene.Id is "lab-11-13-xy-palletizing" or "lab-2-17-pallet-robot"
                or "lab-2-24-robot-cnc" or "lab-3-02-robot-cell-safe-restart" ? 1.75f : .75f;
            // Place the cabinet beyond the field installation, on the visible
            // side. It must not sit in a conveyor lane or swept machine body.
            origin = new Vector3(side > 0 ? bounds.End.X + width / 2 + margin
                : bounds.Position.X - width / 2 - margin, 0, bounds.GetCenter().Z);
            if (scene.Id is "lab-11-07-multi-conveyor-pallet-route" or "lab-6-07-luggage-weight-sort")
                origin = new Vector3(bounds.GetCenter().X, 0, bounds.End.Z + 1.35f);
            if (scene.Id == "lab-6-07-luggage-weight-sort")
                origin = new Vector3(bounds.Position.X - width / 2 - 1.1f, 0, bounds.End.Z + .90f);
            if (scene.Id == "lab-5-08-drawbridge-control")
                origin = new Vector3(bounds.Position.X - width / 2 - 1.0f, 0, bounds.End.Z + 1.35f);
            if (scene.Id is "lab-4-07-parking-garage-entry" or "lab-11-12-mobile-traffic-lights"
                or "lab-9-12-ev-charging-manager" or "lab-5-07-pedestrian-crossing")
                origin = new Vector3(bounds.GetCenter().X, 0, bounds.End.Z + 1.60f);
            if (scene.Id == "lab-9-12-ev-charging-manager")
                origin = new Vector3(bounds.Position.X - width / 2 - 1.1f, 0, bounds.End.Z + 1.60f);
            if (scene.Id == "lab-11-11-service-elevator")
                origin = new Vector3(bounds.End.X + width / 2 + 1.0f, 0, bounds.End.Z + .75f);
            if (scene.Id == "lab-10-06-ten-motor-array-startup")
                // Keep both transport endpoints clear. The operator station
                // belongs in the open service aisle, away from the drive row;
                // its existing measured front-access footprint is retained.
                origin = new Vector3(bounds.GetCenter().X - width, 0, bounds.End.Z + 1.35f);
        }
        installation.Position = origin;
        installation.RotationDegrees = new Vector3(0, frontSign < 0 ? 180 : 0, 0);
        // Explicit illustrative standing/access footprint. This is checked
        // separately from mesh overlap; a disjoint shelf can still block access.
        var accessNear = plan.Bench ? .60f : .10f;
        var accessOrigin = origin + new Vector3(-width / 2, 0,
            frontSign > 0 ? accessNear : -accessNear - 1.0f);
        installation.SetMeta("operatorAccessBounds", new Aabb(accessOrigin, new Vector3(width, 2.0f, 1.0f)));
        GD.Print($"CONTROL_INSTALLATION_ACCESS scene={scene.Id} illustrativeBounds={accessOrigin} size=({width},2,1) compliance=not-assessed");
        if (plan.Bench)
        {
            AddBox(installation, new Vector3(width, .08f, 1.15f), new Vector3(0, topHeight - .04f, 0), top).Name = "StudentBench_Worktop";
            foreach (var x in new[] { -width / 2 + .16f, width / 2 - .16f })
            {
                foreach (var z in new[] { -.43f, .43f })
                {
                    AddBox(installation, new Vector3(.07f, .87f, .07f), new Vector3(x, .435f, z), frame).Name = "BenchLeg";
                    AddBox(installation, new Vector3(.13f, .04f, .13f), new Vector3(x, .02f, z), wiring).Name = "BenchFoot";
                }
                AddBox(installation, new Vector3(.06f, .06f, .90f), new Vector3(x, .22f, 0), frame).Name = "BenchCrossbrace";
            }
            AddBox(installation, new Vector3(width - .28f, .08f, .08f), new Vector3(0, .26f, -.40f), frame).Name = "BenchRail";
            var boardHeight = .67f + (rows - 1) * .52f;
            AddBox(installation, new Vector3(width - .20f, boardHeight, .06f), new Vector3(0, topHeight + boardHeight / 2, -.39f), panel).Name = "TrainingBackboard";
            for (var row = 0; row < rows; row++)
            {
                var level = topHeight + row * .52f;
                if (row > 0) AddBox(installation, new Vector3(width - .22f, .065f, .50f), new Vector3(0, level - .0325f, -.12f), frame).Name = "InstrumentTier";
                AddBox(installation, new Vector3(width - .28f, .065f, .075f), new Vector3(0, level + .05f, -.28f), wiring).Name = "ProtectedWireDuct";
            }
        }
        else if (compactDryerService)
        {
            // A compact teaching service box on a grounded narrow support,
            // separate from the user's hand station. It is not a full-height
            // industrial enclosure or a depiction of certified appliance wiring.
            const float boxBottom = .90f;
            const float boxTop = 1.85f;
            AddBox(installation, new Vector3(width, boxTop - boxBottom, .30f), new Vector3(0, (boxTop + boxBottom) / 2, -.31f), panel).Name = "OperatorCabinet_Body";
            AddBox(installation, new Vector3(width - .08f, boxTop - boxBottom - .08f, .035f), new Vector3(0, (boxTop + boxBottom) / 2, -.1425f), door).Name = "OperatorCabinet_Door";
            AddBox(installation, new Vector3(.12f, boxBottom, .12f), new Vector3(0, boxBottom / 2, -.31f), frame).Name = "CabinetPlinth";
            AddBox(installation, new Vector3(.42f, .035f, .36f), new Vector3(0, .0175f, -.31f), frame).Name = "CompactService_Foot";
            AddBox(installation, new Vector3(.025f, .10f, .04f), new Vector3(-width / 2 + .09f, 1.12f, -.105f), wiring).Name = "DoorHandle";
            foreach (var y in new[] { 1.04f, 1.69f })
                AddBox(installation, new Vector3(.03f, .08f, .04f), new Vector3(width / 2 - .02f, y, -.15f), frame).Name = "DoorHinge";
            installation.SetMeta("compactServiceHousing", true);
        }
        else
        {
            var height = MathF.Max(1.85f, 1.10f + rows * rowPitch + .12f);
            AddBox(installation, new Vector3(width, height - .14f, .46f), new Vector3(0, (height + .14f) / 2, -.39f), panel).Name = "OperatorCabinet_Body";
            AddBox(installation, new Vector3(width - .10f, height - .28f, .035f), new Vector3(0, (height + .14f) / 2, -.1425f), door).Name = "OperatorCabinet_Door";
            AddBox(installation, new Vector3(width - .10f, .14f, .40f), new Vector3(0, .07f, -.39f), frame).Name = "CabinetPlinth";
            AddBox(installation, new Vector3(.035f, .22f, .06f), new Vector3(-width / 2 + .13f, .70f, -.095f), wiring).Name = "DoorHandle";
            foreach (var y in new[] { .35f, height - .28f })
                AddBox(installation, new Vector3(.04f, .13f, .04f), new Vector3(width / 2 - .025f, y, -.15f), frame).Name = "DoorHinge";
            // Protected wiring is inside the enclosure, rather than a long
            // external ledge whose grazing shadow obscures the lower door.
            AddBox(installation, new Vector3(width - .14f, .08f, .10f), new Vector3(0, .38f, -.39f), wiring).Name = "ProtectedPanelCableRoute";
            AddBox(installation, new Vector3(.09f, .34f, .09f), new Vector3(width / 2 - .18f, .18f, -.50f), wiring).Name = "CabinetCableEntry";
        }
        for (var i = 0; i < equipment.Length; i++)
        {
            var item = equipment[i];
            var node = root.GetNode<Node3D>(SafeNodeName(item.Id));
            if (!plan.Bench && item.Type == "indicator"
                && node.FindChild("LENS_single", true, false) is MeshInstance3D pilot)
            {
                // Keep the named output lens for its existing controller, but
                // replace the selected pole-beacon surface with a panel pilot.
                pilot.Mesh = new CylinderMesh { TopRadius = .065f, BottomRadius = .065f, Height = .035f };
                foreach (var detail in node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                    if (detail.Name.ToString().StartsWith("LENS_fresnel_rib_", StringComparison.Ordinal)
                        || detail.Name == "BEZEL_upper") detail.Visible = false;
                if (node.FindChild("BEZEL_lower", true, false) is MeshInstance3D bezel)
                {
                    bezel.Mesh = new CylinderMesh { TopRadius = .075f, BottomRadius = .075f, Height = .012f };
                    bezel.Position = pilot.Position + new Vector3(0, -.023f, 0);
                }
            }
            if (!plan.Bench && node.FindChild("OperatorFaceLabel", true, false) is Label3D legend)
            {
                legend.PixelSize = .0018f;
                if (node.FindChild("BUTTON_legend_plate", true, false) is MeshInstance3D plate)
                {
                    plate.Scale = new Vector3(2.0f, 1.65f, 1);
                    // Imported Blender text uses a left-side origin. The live
                    // Label3D is centred, so use the actual plate centre in the
                    // label parent's coordinate frame instead of that origin.
                    Transform3D InEquipment(Node3D part)
                    {
                        var pose = part.Transform;
                        for (var parent = part.GetParent(); parent != node; parent = parent.GetParent())
                            if (parent is Node3D spatial) pose = spatial.Transform * pose;
                        return pose;
                    }
                    var legendParent = (Node3D)legend.GetParent();
                    var parentPose = legendParent == node ? Transform3D.Identity : InEquipment(legendParent);
                    var plateBounds = parentPose.AffineInverse() * InEquipment(plate) * plate.GetAabb();
                    legend.HorizontalAlignment = HorizontalAlignment.Center;
                    legend.VerticalAlignment = VerticalAlignment.Center;
                    legend.Position = new Vector3(plateBounds.GetCenter().X, plateBounds.GetCenter().Y,
                        plateBounds.End.Z + .003f);
                    var font = legend.Font ?? ThemeDB.FallbackFont;
                    var lines = legend.Text.Split('\n');
                    var textSizes = lines.Select(line => font.GetStringSize(line,
                        HorizontalAlignment.Left, -1, legend.FontSize)).ToArray();
                    var textWidth = MathF.Max(1, textSizes.Max(size => size.X));
                    var lineHeight = MathF.Max(1, font.GetStringSize("Mg", HorizontalAlignment.Left,
                        -1, legend.FontSize).Y);
                    var textHeight = textSizes.Sum(size => MathF.Max(lineHeight, size.Y));
                    // The retained plate supplies the contrast area. Fit the
                    // actual configured text with inset margins; never shorten
                    // its operator function or rewrite its configuration.
                    legend.PixelSize = MathF.Min(.0018f, MathF.Min(
                        MathF.Max(.001f, plateBounds.Size.X - .020f) / textWidth,
                        MathF.Max(.001f, plateBounds.Size.Y - .010f) / textHeight));
                }
            }
            foreach (var mesh in node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                if (IsOriginalControlMount(item, mesh.Name.ToString())
                    || !plan.Bench && IsPanelControlShell(item, mesh.Name.ToString())) mesh.Visible = false;
            node.Scale *= .70f;
            node.RotationDegrees = new Vector3(!plan.Bench && item.Type == "indicator" ? 90 : 0,
                frontSign < 0 ? 180 : 0, 0);
            node.Position = Vector3.Zero;
            var bounds = InstallationBounds(node);
            var row = i / columns;
            var inRow = Math.Min(columns, equipment.Length - row * columns);
            var localX = (i % columns - (inRow - 1) / 2f) * pitch;
            var desiredBottom = (plan.Bench ? topHeight : 1.10f) + row * rowPitch;
            // Cabinet door is behind retained device heads. Their rear faces
            // contact its fascia; a head is never suspended in front of it.
            var desiredZ = plan.Bench ? (row == 0 ? 0 : -.12f)
                : frontSign > 0 ? -.125f - bounds.Position.Z : .125f - bounds.End.Z;
            node.Position = origin + new Vector3(frontSign * localX,
                desiredBottom - bounds.Position.Y, plan.Bench ? frontSign * desiredZ : desiredZ);
            if (plan.Bench)
            {
                AddBox(installation, new Vector3(.16f, .08f, .14f), new Vector3(localX, desiredBottom + .04f, -.13f), wiring).Name = "RearTerminalCover";
                AddBox(installation, new Vector3(.035f, .06f, .20f), new Vector3(localX, desiredBottom + .03f, -.20f), wiring).Name = "ComponentWireRoute";
            }
            node.SetMeta("installationKind", plan.Bench ? "studentBench" : "operatorPanel");
            if (scene.Id == "lab-4-09-chain-drive-lift" && (item.Id is "switch_8" or "switch_9" or "switch_10"))
            {
                var (point, text) = item.Id switch
                {
                    "switch_8" => ("box_present", "BOX PRESENT"),
                    "switch_9" => ("lift_home", "LIFT HOME"),
                    _ => ("destination_clear", "DEST CLEAR"),
                };
                if (!scene.Simulation.GetProperty("pointBindings").EnumerateArray().Any(binding =>
                    Text(binding, "equipmentId", "") == item.Id && Text(binding, "point", "") == point
                    && Text(binding, "mode", "") == "indicator"))
                    throw new InvalidOperationException("Chain feedback legend binding no longer matches: " + item.Id);
                // The support owns this passive physical legend. Its exact
                // feedback pilot and green/off controller stay untouched.
                // Use the available door area below the pilot for readable
                // ink, rather than limiting all legends to tiny 5px glyphs.
                // The plate top stays clear of the retained pilot underside.
                var legendAt = new Vector3(localX, desiredBottom - .110f, -.110f);
                var plateWidth = MathF.Min(.42f, pitch - .04f);
                const float plateHeight = .125f;
                AddBox(installation, new Vector3(plateWidth, plateHeight, .012f), legendAt, wiring)
                    .Name = "FeedbackLegendPlate_" + item.Id;
                var label = new Label3D
                {
                    Name = "FeedbackLegend_" + item.Id, Text = text,
                    Position = legendAt + new Vector3(0, 0, .009f),
                    FontSize = 64, PixelSize = .0014f, OutlineSize = 0,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center, Modulate = Colors.White,
                };
                var ink = ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, label.FontSize);
                label.PixelSize = MathF.Min(label.PixelSize,
                    MathF.Min((plateWidth - .02f) / MathF.Max(1, ink.X),
                        (plateHeight - .02f) / MathF.Max(1, ink.Y)));
                label.SetMeta("sourcePoint", point);
                node.SetMeta("feedbackLegend", label.Name.ToString());
                installation.AddChild(label);
            }
            GD.Print($"CONTROL_INSTALLATION_COMPONENT scene={scene.Id} equipment={item.Id} kind={(plan.Bench ? "studentBench" : "operatorPanel")} pose={node.Position} scale={node.Scale}");
        }
        if (!plan.Bench) MountCabinetStatusTowers(root, scene, installation);
        MountChickenReadoutToMachineFrame(root, scene);
        var associated = new Godot.Collections.Array<string>();
        foreach (var id in plan.EquipmentIds) associated.Add(id);
        if (!plan.Bench && CabinetStatusTowers.TryGetValue(scene.Id, out var towerIds))
            foreach (var id in towerIds)
                if (!associated.Contains(id)) associated.Add(id);
        installation.SetMeta("installationEquipmentIds", associated);
        installation.SetMeta("installationKind", plan.Bench ? "studentBench" : "operatorPanel");
        ConfigureInstallationIdentification(root, scene, installation);
        GD.Print($"CONTROL_INSTALLATION_READY scene={scene.Id} kind={(plan.Bench ? "studentBench" : "operatorPanel")} components={equipment.Length}");
    }

    private static bool IsOriginalControlMount(SceneEquipment item, string name)
    {
        // Only verified mounting namespaces of the selected assets are hidden.
        // Functional lenses, operators, housings, KIN nodes and labels survive.
        if (item.Type == "switch") return name == "FOOT" || name.StartsWith("PEDESTAL", StringComparison.Ordinal)
            || name.StartsWith("CTRL_rear_conduit", StringComparison.Ordinal);
        if (item.Type == "rotarySwitch") return name.StartsWith("BASE_", StringComparison.Ordinal)
            || name.StartsWith("PEDESTAL", StringComparison.Ordinal);
        if (item.Type == "indicator") return name == "POLE" || name.StartsWith("BASE_", StringComparison.Ordinal);
        if (Text(item.Config, "installation", "") == "cookieCounter")
            return name is "COUNT_foot" or "COUNT_mast";
        if (Text(item.Config, "catalogAssetId", "") == "training.accessory.energy_meter.v1"
            && (name is "METER_base" or "METER_pedestal")) return true;
        return name is "COUNT_DISPLAY_base" or "COUNT_DISPLAY_mast"
            or "STATIC_READOUT_base" or "STATIC_READOUT_mast"
            or "LENGTH_DISPLAY_base" or "LENGTH_DISPLAY_mast"
            or "LABEL_DISPLAY_base" or "LABEL_DISPLAY_mast"
            or "STAND_base" or "STAND_mast";
    }

    private static bool IsPanelControlShell(SceneEquipment item, string name)
    {
        if (item.Type is "switch" or "rotarySwitch")
            return name is "ENCLOSURE_body" or "ENCLOSURE_face" or "ENCLOSURE_door"
                || name.StartsWith("ENCLOSURE_fascia_screw", StringComparison.Ordinal)
                || name.StartsWith("ENCLOSURE_rear_", StringComparison.Ordinal)
                || name.StartsWith("DOOR_fastener", StringComparison.Ordinal);
        // Explicitly selected single pilot heads become front-facing panel
        // lenses. The field tower assets are never selected for this mounting.
        return item.Type == "indicator" && (name is "CAP" or "LENS_reflector"
            || name.StartsWith("LENS_led_module_", StringComparison.Ordinal));
    }

    // Compose has not entered the tree, so use accumulated local transforms;
    // GlobalTransform is inappropriate until the hierarchy is attached.
    private static Aabb InstallationBounds(Node3D equipment)
    {
        Aabb? bounds = null;
        foreach (var mesh in equipment.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            if (!mesh.Visible || mesh.Mesh is null) continue;
            var transform = mesh.Transform;
            for (var parent = mesh.GetParent(); parent != equipment; parent = parent.GetParent())
                if (parent is Node3D spatial) transform = spatial.Transform * transform;
            var aabb = equipment.Transform * transform * mesh.GetAabb();
            bounds = bounds is { } value ? value.Merge(aabb) : aabb;
        }
        return bounds ?? throw new InvalidOperationException($"Installation component has no visible mesh: {equipment.Name}");
    }
}
