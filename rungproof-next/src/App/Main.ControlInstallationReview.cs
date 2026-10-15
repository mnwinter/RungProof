using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    // Opt-in native scene audit. Scripted ray picking is not keyboard/mouse
    // acceptance, electrical verification, or a continuous swept-clearance proof.
    private async void VerifyControlInstallations()
    {
        try
        {
            var filter = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--installation-scene-id=", StringComparison.Ordinal))?
                .Substring("--installation-scene-id=".Length);
            var catalog = SceneCatalogLoader.LoadCatalog("res://scenes/catalog/original-scenes.catalog.json");
            var entries = catalog.Scenes.Where(e => filter is null || e.Id == filter).ToArray();
            if (entries.Length == 0) throw new InvalidOperationException("Unknown installation scene filter.");
            var scenes = 0; var cases = 0; var picks = 0; var presses = 0; var selectors = 0; var blocked = 0; var knownNonHeadBindings = 0; var retainedFieldBindings = 0;
            var measuredEvidence = new List<object>();
            var projectedIndicators = 0; var complexIndicatorBindings = 0; var nonOutputIndicators = 0;
            var benchLensShelfChecks = 0;
            foreach (var entry in entries)
            {
                var authored = SceneCatalogLoader.LoadScene(entry);
                AddMigratedScene(entry.Id, _candidateCatalog!, _mainCamera!, false, false);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var definition = _activeSceneDefinition!;
                var root = _sceneCompositionRoot!;
                var runtime = _sceneRuntime!;
                var authoredClockMode = runtime.UsesExternalClock;
                // Authored cases exercise the local preview rules. The app's
                // stopped controller clock deliberately suppresses those rules.
                runtime.UsesExternalClock = false;
                var interactor = _sceneControlInteractor!;
                void Require(bool value, string detail)
                {
                    if (!value) throw new InvalidOperationException($"{entry.Id}: {detail}");
                }
                Require(definition.Simulation.GetRawText() == authored.Simulation.GetRawText(), "simulation contract changed");
                Require(definition.Equipment.Select(e => e.Id).SequenceEqual(authored.Equipment.Select(e => e.Id)), "equipment identities changed");
                var declared = new List<(string Equipment, string Action)>();
                foreach (var equipment in definition.Equipment)
                {
                    Require(equipment.Config.GetRawText() == authored.Equipment.Single(e => e.Id == equipment.Id).Config.GetRawText(), $"config changed: {equipment.Id}");
                    if (equipment.Config.TryGetProperty("action", out var action) && action.GetString() is { Length: > 0 } actionId)
                        declared.Add((equipment.Id, actionId));
                    if (equipment.Config.TryGetProperty("sceneControls", out var controls))
                        foreach (var control in controls.EnumerateArray())
                            if (control.TryGetProperty("action", out var nested) && nested.GetString() is { Length: > 0 } nestedId)
                                declared.Add((equipment.Id, nestedId));
                }
                Require(declared.OrderBy(p => p.Equipment).ThenBy(p => p.Action).SequenceEqual(
                    interactor.Bindings.Select(b => (b.EquipmentId, b.ActionId)).OrderBy(p => p.EquipmentId).ThenBy(p => p.ActionId)), "declared binding coverage differs");
                var mounted = root.GetChildren().OfType<Node3D>().Where(n => n.Name != "ControlInstallation"
                    && n.HasMeta("installationKind") && (n.GetMeta("installationKind").AsString() is "studentBench" or "operatorPanel")).ToArray();
                var support = root.GetNodeOrNull<Node3D>("ControlInstallation");
                if (support is not null)
                {
                    object VectorEvidence(Vector3 value) => new { x = value.X, y = value.Y, z = value.Z };
                    object BoundsEvidence(Aabb value) => new
                    {
                        position = VectorEvidence(value.Position), size = VectorEvidence(value.Size), end = VectorEvidence(value.End)
                    };
                    var cabinet = support.FindChild("OperatorCabinet_Body", true, false) as MeshInstance3D;
                    var heads = mounted.Select(node => new
                    {
                        equipmentId = node.Name.ToString(),
                        kind = node.GetMeta("installationKind").AsString(),
                        worldPosition = VectorEvidence(node.GlobalPosition),
                        worldBasis = new { x = VectorEvidence(node.GlobalBasis.X), y = VectorEvidence(node.GlobalBasis.Y), z = VectorEvidence(node.GlobalBasis.Z) },
                        functionalHeads = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(mesh => mesh.Mesh is not null && mesh.IsVisibleInTree()
                                && (mesh.Name.ToString().StartsWith("KIN_", StringComparison.Ordinal) || mesh.Name.ToString().StartsWith("LENS_", StringComparison.Ordinal)))
                            .Select(mesh => new { name = mesh.Name.ToString(), worldCentroid = VectorEvidence(mesh.GlobalTransform * mesh.GetAabb().GetCenter()), worldBounds = BoundsEvidence(ControlInstallationWorldBounds(mesh)) }).ToArray()
                    }).ToArray();
                    var conveyors = definition.Equipment.Where(e => e.Type == "conveyor").Select(e =>
                    {
                        var node = root.GetNode<Node3D>(e.Id.Replace('-', '_'));
                        var meshes = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.Mesh is not null && m.IsVisibleInTree()).ToArray();
                        var belts = meshes.Where(m => m.Name.ToString().StartsWith("KIN_belt_surface", StringComparison.Ordinal)).ToArray();
                        return new { equipmentId = e.Id, renderedAssemblyTopY = meshes.Length == 0 ? (float?)null : meshes.Max(m => ControlInstallationWorldBounds(m).End.Y),
                            actualBeltSurfaceTopY = belts.Length == 0 ? (float?)null : belts.Max(m => ControlInstallationWorldBounds(m).End.Y),
                            worldBasis = new { x = VectorEvidence(node.GlobalBasis.X), y = VectorEvidence(node.GlobalBasis.Y), z = VectorEvidence(node.GlobalBasis.Z) } };
                    }).ToArray();
                    var rollerInstallations = definition.Equipment.Where(e => e.Config.TryGetProperty("installation", out var value)
                        && value.GetString() == "groupingLine").Select(e =>
                    {
                        var node = root.GetNode<Node3D>(e.Id.Replace('-', '_'));
                        var rollers = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.Mesh is not null && m.IsVisibleInTree()
                                && m.Name.ToString().StartsWith("KIN_group_roller_", StringComparison.Ordinal)).ToArray();
                        Require(rollers.Length > 0, $"grouping line has no identified visible rollers: {e.Id}");
                        return new
                        {
                            equipmentId = e.Id, installation = "groupingLine", rollerCount = rollers.Length,
                            rollerCrownTopY = rollers.Max(m => ControlInstallationWorldBounds(m).End.Y),
                            rollerCrownMinY = rollers.Min(m => ControlInstallationWorldBounds(m).End.Y),
                            lineLongAxisWorld = VectorEvidence(node.GlobalBasis.X.Normalized()),
                            worldBasis = new { x = VectorEvidence(node.GlobalBasis.X), y = VectorEvidence(node.GlobalBasis.Y), z = VectorEvidence(node.GlobalBasis.Z) },
                            rollerNames = rollers.Select(m => m.Name.ToString()).ToArray()
                        };
                    }).ToArray();
                    var measurement = new
                    {
                        sceneId = entry.Id,
                        provenance = "Actual composed native node world transforms and visible mesh AABBs; render assembly top is not assumed belt surface.",
                        cabinetBodyWorldBounds = cabinet is null ? null : BoundsEvidence(ControlInstallationWorldBounds(cabinet)),
                        supportWorldYawDegrees = support.GlobalRotationDegrees.Y,
                        environmentWorldYawDegrees = _demoEnvironment?.GlobalRotationDegrees.Y,
                        operatorAccessWorldBounds = support.HasMeta("operatorAccessBounds") ? BoundsEvidence(support.GetMeta("operatorAccessBounds").AsAabb()) : null,
                        mountedHeads = heads, conveyors, rollerInstallations,
                        limitation = "Static geometry evidence; operator reach, perception, swept clearance and release approval remain separate."
                    };
                    measuredEvidence.Add(measurement);
                    GD.Print("CONTROL_INSTALLATION_MEASURED " + JsonSerializer.Serialize(measurement));
                }
                if (mounted.Length > 0)
                {
                    Require(support is not null && support.GetParent() == root, "missing sibling mounting support");
                    Require(!interactor.Bindings.Any(b => b.Node == support || b.Node.IsAncestorOf(support!)), "support entered a bound clickable root");
                    Require(!support!.FindChildren("*", "", true, false).Any(n => n is CollisionObject3D or CollisionShape3D or EquipmentMotionController or ConveyorController), "support has physics/runtime behavior");
                    var supportMeshes = support.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
                    Require(supportMeshes.Length > 0, "support contains no geometry");
                    var supportBounds = supportMeshes.Select(ControlInstallationWorldBounds).Aggregate((a, b) => a.Merge(b));
                    if (entry.Id == "lab-6-08-hand-dryer")
                    {
                        // This checks the measured teaching-service installation,
                        // not retail appliance dimensions. Sensor/hand geometry and
                        // timer behavior remain covered by the unchanged dryer audit.
                        Require(support.HasMeta("compactServiceMeasuredDeviceWidth"), "compact dryer missing measured device-width metadata");
                        var measuredWidth = (float)support.GetMeta("compactServiceMeasuredDeviceWidth").AsDouble();
                        Require(float.IsFinite(measuredWidth) && measuredWidth > 0,
                            "compact dryer measured device width is invalid");
                        Require(support.HasMeta("compactServiceColumnWidths")
                            && support.HasMeta("compactServiceHousing") && support.GetMeta("compactServiceHousing").AsBool(),
                            "compact dryer lacks measured-column service-housing metadata");
                        var columnWidths = support.GetMeta("compactServiceColumnWidths").AsVector2();
                        Require(float.IsFinite(columnWidths.X) && float.IsFinite(columnWidths.Y)
                            && columnWidths.X > 0 && columnWidths.Y > 0
                            && Math.Abs(Math.Max(columnWidths.X, columnWidths.Y) - measuredWidth) < .002f,
                            "compact dryer measured column widths are invalid");
                        var expectedPitch = Math.Max(.38f, (columnWidths.X + columnWidths.Y) / 2 + .12f);
                        var expectedWidth = Math.Max(1.1f, expectedPitch + Math.Max(columnWidths.X, columnWidths.Y) + .20f);
                        var body = supportMeshes.SingleOrDefault(m => m.Name == "OperatorCabinet_Body");
                        var fascia = supportMeshes.SingleOrDefault(m => m.Name == "OperatorCabinet_Door");
                        var plinth = supportMeshes.SingleOrDefault(m => m.Name == "CabinetPlinth");
                        Require(body is not null && fascia is not null && plinth is not null,
                            "compact dryer lacks cabinet body, fascia or grounded plinth");
                        var bodyBounds = ControlInstallationWorldBounds(body!);
                        var fasciaBounds = ControlInstallationWorldBounds(fascia!);
                        var plinthBounds = ControlInstallationWorldBounds(plinth!);
                        Require(Math.Abs(bodyBounds.Position.Y - .90f) < .002f
                            && Math.Abs(bodyBounds.End.Y - 1.85f) < .002f
                            && Math.Abs(bodyBounds.Size.Y - .95f) < .002f
                            && Math.Abs(bodyBounds.Size.Z - .30f) < .002f,
                            "compact dryer housing reverted to a full-height/deep floor cabinet");
                        Require(Math.Abs(plinthBounds.Size.X - .12f) < .002f
                            && Math.Abs(plinthBounds.Size.Y - .90f) < .002f
                            && Math.Abs(plinthBounds.Size.Z - .12f) < .002f,
                            "compact dryer service housing lacks its narrow bearing post");
                        var foot = supportMeshes.SingleOrDefault(m => m.Name == "CompactService_Foot");
                        Require(foot is not null && foot.IsVisibleInTree(), "compact dryer service post lacks a visible grounded foot");
                        var footBounds = ControlInstallationWorldBounds(foot!);
                        Require(Math.Abs(footBounds.Position.Y) < .002f
                            && Math.Abs(footBounds.Size.X - .42f) < .002f
                            && Math.Abs(footBounds.Size.Y - .035f) < .002f
                            && Math.Abs(footBounds.Size.Z - .36f) < .002f
                            && footBounds.Position.X <= plinthBounds.Position.X && footBounds.End.X >= plinthBounds.End.X
                            && footBounds.Position.Z <= plinthBounds.Position.Z && footBounds.End.Z >= plinthBounds.End.Z,
                            "compact dryer ground foot does not bear the service post");
                        Require(Math.Abs(bodyBounds.Size.X - expectedWidth) < .002f,
                            "compact dryer body width does not follow measured two-column pitch");
                        Require(Math.Abs(plinthBounds.Position.Y) < .002f
                            && Math.Abs(plinthBounds.End.Y - bodyBounds.Position.Y) < .002f,
                            "compact dryer cabinet body is not borne by its grounded plinth");
                        var expectedIds = new[] { "indicator_2", "training_accessory_6", "switch_7", "indicator_9" };
                        Require(mounted.Length == 4 && mounted.Select(n => n.Name.ToString()).OrderBy(n => n)
                            .SequenceEqual(expectedIds.OrderBy(n => n)), "compact dryer selected-device coverage differs");
                        for (var i = 0; i < expectedIds.Length; i++)
                        {
                            var node = mounted.Single(n => n.Name == expectedIds[i]);
                            Require(Math.Abs(node.GlobalBasis.X.Length() - .70f) < .002f
                                && Math.Abs(node.GlobalBasis.Y.Length() - .70f) < .002f
                                && Math.Abs(node.GlobalBasis.Z.Length() - .70f) < .002f,
                                $"compact dryer changed retained presentation basis: {node.Name}");
                            var devices = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                                .Where(m => m.Mesh is not null && m.IsVisibleInTree()).ToArray();
                            Require(devices.Length > 0, $"compact dryer device is invisible: {node.Name}");
                            var deviceBounds = devices.Select(ControlInstallationWorldBounds).Aggregate((a, b) => a.Merge(b));
                            var expectedX = support.GlobalPosition.X + (i % 2 == 0 ? -.5f : .5f) * expectedPitch;
                            Require(Math.Abs(node.GlobalPosition.X - expectedX) < .002f,
                                $"compact dryer does not retain two measured columns: {node.Name}");
                            Require(Math.Abs(deviceBounds.Position.Y - (1.10f + i / 2 * .38f)) < .002f,
                                $"compact dryer does not retain two supported rows: {node.Name}");
                            Require(deviceBounds.Position.X >= bodyBounds.Position.X - .002f
                                && deviceBounds.End.X <= bodyBounds.End.X + .002f,
                                $"compact dryer device extends beyond measured cabinet width: {node.Name}");
                            // The retained rear face bears against the front fascia.
                            // Exact face contact is intentional, not an obstruction.
                            Require(Math.Abs(deviceBounds.Position.Z - fasciaBounds.End.Z) < .003f,
                                $"compact dryer device rear is detached from fascia: {node.Name}");
                        }
                        var display = mounted.Single(n => n.Name == "training_accessory_6")
                            .FindChild("NumericReadout", true, false) as Label3D;
                        Require(display is not null && display.IsVisibleInTree()
                            && display.Text.Contains("REMAINING", StringComparison.Ordinal),
                            "compact dryer lost its live remaining-seconds readout");
                        Require(display!.GlobalPosition.X >= bodyBounds.Position.X
                            && display.GlobalPosition.X <= bodyBounds.End.X,
                            "compact dryer readout anchor is outside the retained support");
                        GD.Print($"CONTROL_INSTALLATION_DRYER_COMPACT measuredDeviceWidth={measuredWidth} columns=2 rows=2 pitch={expectedPitch} bodyWidth={bodyBounds.Size.X} groundedPlinth=true fasciaContact=true displayRetained=true scope=static-service-geometry-not-hand-sensor-or-physical-compliance");
                    }
                    // Check functional lamp lenses against the raised instrument
                    // shelf only. Mounting plates and the worktop intentionally
                    // contact component bases and are outside this assertion.
                    var instrumentTiers = supportMeshes.Where(m => m.Mesh is not null && m.IsVisibleInTree()
                        && m.Name.ToString() == "InstrumentTier").ToArray();
                    foreach (var benchComponent in mounted.Where(n => n.GetMeta("installationKind").AsString() == "studentBench"))
                        foreach (var lens in benchComponent.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.Mesh is not null && m.IsVisibleInTree()
                                && m.Name.ToString().StartsWith("LENS_", StringComparison.Ordinal)))
                            foreach (var tier in instrumentTiers)
                            {
                                var lensBounds = ControlInstallationWorldBounds(lens);
                                var tierBounds = ControlInstallationWorldBounds(tier);
                                var overlap = new Vector3(
                                    Math.Min(lensBounds.End.X, tierBounds.End.X) - Math.Max(lensBounds.Position.X, tierBounds.Position.X),
                                    Math.Min(lensBounds.End.Y, tierBounds.End.Y) - Math.Max(lensBounds.Position.Y, tierBounds.Position.Y),
                                    Math.Min(lensBounds.End.Z, tierBounds.End.Z) - Math.Max(lensBounds.Position.Z, tierBounds.Position.Z));
                                // Ignore exact face contact and sub-millimetre
                                // transform rounding; require actual volume.
                                Require(overlap.X <= .001f || overlap.Y <= .001f || overlap.Z <= .001f,
                                    $"instrument tier intersects functional lens: {benchComponent.Name}/{lens.Name}");
                                benchLensShelfChecks++;
                            }
                    Require(support.HasMeta("operatorAccessBounds"), "missing standing-access zone metadata");
                    var access = support.GetMeta("operatorAccessBounds").AsAabb();
                    foreach (var fieldRoot in root.GetChildren().OfType<Node3D>().Where(n => n != support && !mounted.Contains(n)))
                        foreach (var mesh in fieldRoot.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.Mesh is not null && m.IsVisibleInTree()))
                        {
                            var bounds = ControlInstallationWorldBounds(mesh);
                            if (bounds.End.Y > .05f)
                                Require(!bounds.Intersects(access), $"field geometry blocks standing-access zone: {fieldRoot.Name}/{mesh.Name}");
                        }
                    if (_demoEnvironment is not null)
                        foreach (var mesh in _demoEnvironment.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.Mesh is not null && m.IsVisibleInTree()))
                        {
                            var bounds = ControlInstallationWorldBounds(mesh);
                            if (bounds.End.Y > .05f)
                                Require(!bounds.Intersects(access), $"environment blocks standing-access zone: {mesh.Name}");
                        }
                    foreach (var node in mounted)
                    {
                        var meshes = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.Mesh is not null && m.IsVisibleInTree()).ToArray();
                        Require(meshes.Length > 0, $"mounted component invisible: {node.Name}");
                        var bounds = meshes.Select(ControlInstallationWorldBounds).Aggregate((a, b) => a.Merge(b));
                        Require(bounds.Position.Y >= .85f && bounds.Position.Y <= supportBounds.End.Y + .05f,
                            $"component outside supported vertical range: {node.Name}");
                        Require(bounds.GetCenter().X >= supportBounds.Position.X - .10f && bounds.GetCenter().X <= supportBounds.End.X + .10f,
                            $"component outside mounting width: {node.Name}");
                    }
                }
                VerifyCabinetStatusTowers(definition, root, support, runtime);
                var poses = mounted.ToDictionary(n => n, n => n.Transform);
                if (definition.Verification.ValueKind == JsonValueKind.Object
                    && definition.Verification.TryGetProperty("cases", out var authoredCases) && authoredCases.GetArrayLength() > 0)
                {
                    Require(runtime.VerifyDeclaredCases(definition.Verification), "authored runtime cases failed");
                    cases++;
                }
                runtime.ResetSimulation();
                Require(poses.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "reset lost mounted root poses");
                VerifyMotorTestFixtures(definition, root, runtime);
                foreach (var equipment in definition.Equipment.Where(e => e.Type == "indicator"
                    && mounted.Any(n => n.Name.ToString() == e.Id.Replace('-', '_'))))
                {
                    var indicatorBindings = definition.Simulation.TryGetProperty("pointBindings", out var pointBindings)
                        ? pointBindings.EnumerateArray().Where(b => b.TryGetProperty("equipmentId", out var id)
                            && id.GetString() == equipment.Id).ToArray() : System.Array.Empty<JsonElement>();
                    if (indicatorBindings.Length != 1 || !indicatorBindings[0].TryGetProperty("mode", out var mode)
                        || mode.GetString() != "indicator")
                    {
                        complexIndicatorBindings++;
                        GD.Print($"CONTROL_INSTALLATION_INDICATOR_SCOPE scene={entry.Id} equipment={equipment.Id} projection=not-forced reason=multiple-or-channel-binding authoredCases=retained");
                        continue;
                    }
                    var point = indicatorBindings[0].GetProperty("point").GetString()!;
                    var declaredPoint = definition.Simulation.GetProperty("points").EnumerateArray()
                        .Single(p => p.GetProperty("name").GetString() == point);
                    if (declaredPoint.GetProperty("type").GetString() != "BOOL"
                        || declaredPoint.GetProperty("owner").GetString() != "PLC")
                    {
                        nonOutputIndicators++;
                        GD.Print($"CONTROL_INSTALLATION_INDICATOR_SCOPE scene={entry.Id} equipment={equipment.Id} point={point} projection=not-forced reason=non-PLC-BOOL-output authoredCases=retained");
                        continue;
                    }
                    var node = root.GetNode<Node3D>(equipment.Id.Replace('-', '_'));
                    var color = indicatorBindings[0].GetProperty("activeColor").GetString()!;
                    var lens = node.FindChild("LENS_single", true, false) as MeshInstance3D
                        ?? node.FindChild("LENS_" + color, true, false) as MeshInstance3D;
                    Require(lens is not null && lens.IsVisibleInTree(), $"mounted indicator bound lens missing: {equipment.Id}");
                    bool Emitting() => lens!.MaterialOverride is StandardMaterial3D material
                        && material.EmissionEnabled && material.EmissionEnergyMultiplier > .1f;
                    var initial = runtime.Points[point];
                    foreach (var value in new[] { false, true, false })
                    {
                        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = value });
                        Require(Equals(runtime.Points[point], value) && Emitting() == value,
                            $"mounted indicator output projection mismatch: {equipment.Id}/{point}/{value}");
                    }
                    runtime.ResetSimulation();
                    Require(Equals(runtime.Points[point], initial) && Emitting() == (initial is true),
                        $"mounted indicator reset projection mismatch: {equipment.Id}/{point}");
                    projectedIndicators++;
                    GD.Print($"CONTROL_INSTALLATION_INDICATOR_PROJECTION PASS scene={entry.Id} equipment={equipment.Id} point={point} lens={lens!.Name} scope=explicit-output-projection-fixture false=off true=on reset=initial");
                }
                if (entry.Id == "lab-4-08-package-grouping")
                {
                    var lampBinding = definition.Simulation.GetProperty("pointBindings").EnumerateArray()
                        .Single(b => b.GetProperty("equipmentId").GetString() == "group_ready_lamp"
                            && b.GetProperty("mode").GetString() == "indicator");
                    var point = lampBinding.GetProperty("point").GetString()!;
                    var lens = (MeshInstance3D)root.GetNode<Node3D>("group_ready_lamp").FindChild("LENS_single", true, false);
                    bool Emitting() => lens.MaterialOverride is StandardMaterial3D material
                        && material.EmissionEnabled && material.EmissionEnergyMultiplier > .1f;
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = false });
                    Require(runtime.Points[point] is false && !Emitting(), "group-ready false output/lens projection failed");
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = true });
                    Require(runtime.Points[point] is true && Emitting(), "group-ready true output/lens projection failed");
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = false });
                    Require(runtime.Points[point] is false && !Emitting(), "group-ready lens failed to extinguish");
                    runtime.ResetSimulation();
                    Require(runtime.Points[point] is false && !Emitting(), "group-ready reset projection failed");
                    GD.Print($"CONTROL_INSTALLATION_GROUP_LAMP PASS point={point} scope=explicit-output-projection-fixture false=off true=on reset=off");
                    var access = support!.GetMeta("operatorAccessBounds").AsAabb();
                    var cabinetDoor = support.FindChild("OperatorCabinet_Door", true, false) as MeshInstance3D;
                    Require(cabinetDoor is not null, "grouping cabinet door missing");
                    var doorBounds = ControlInstallationWorldBounds(cabinetDoor!);
                    var doorFrontSign = definition.Camera.Position[2] >= definition.Camera.Target[2] ? 1f : -1f;
                    // Conservative rectangle covering a hypothetical 90-degree
                    // swing of the visible right-hinged door. No animated hinge
                    // or mechanical door clearance is represented by this check.
                    var doorSwing = new Aabb(new Vector3(doorBounds.Position.X, doorBounds.Position.Y,
                        doorFrontSign > 0 ? doorBounds.End.Z : doorBounds.Position.Z - doorBounds.Size.X),
                        new Vector3(doorBounds.Size.X, doorBounds.Size.Y, doorBounds.Size.X));
                    var obstacles = root.GetChildren().OfType<Node3D>().Where(n => n != support && !mounted.Contains(n))
                        .SelectMany(n => n.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>());
                    if (_demoEnvironment is not null)
                        obstacles = obstacles.Concat(_demoEnvironment.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>());
                    foreach (var mesh in obstacles.Where(m => m.Mesh is not null && m.IsVisibleInTree()))
                    {
                        var bounds = ControlInstallationWorldBounds(mesh);
                        if (bounds.End.Y > .05f)
                            Require(!bounds.Intersects(doorSwing), $"illustrative cabinet-door reserve intersects {mesh.Name}");
                    }
                    GD.Print($"CONTROL_INSTALLATION_GROUP_DOOR doorWorldBounds={doorBounds} illustrativeNinetyDegreeReserve={doorSwing} staticFieldAndEnvironmentClear=true scope=conservative-rectangle-not-animated-door-not-physical-certification");
                    var carton = root.GetNode<Node3D>("carton_0");
                    var cartonMeshes = carton.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                        .Where(m => m.Mesh is not null).ToArray();
                    var cartonBounds = cartonMeshes.Select(ControlInstallationWorldBounds).Aggregate((a, b) => a.Merge(b));
                    var route = cartonBounds;
                    var delta = (float)(PackageGroupingPlantModel.FinalFront - PackageGroupingPlantModel.EntryX);
                    route.Size += new Vector3(delta, 0, 0);
                    Require(!route.Intersects(access), "declared grouping carton-route envelope crosses standing-access zone");
                    GD.Print($"CONTROL_INSTALLATION_GROUP_ROUTE access={access} declaredCartonRoute={route} entryX={PackageGroupingPlantModel.EntryX} finalFrontX={PackageGroupingPlantModel.FinalFront} staticEnvelopeClear=true scope=authored-translation-envelope-not-dynamics doorSwing=unmodeled");
                }
                if (entry.Id == "lab-2-01-workstation-call")
                {
                    var lamp = root.GetNode<Node3D>("material_call_lamp").FindChild("LENS_single", true, false) as MeshInstance3D;
                    Require(lamp is not null, "representative lamp lens missing");
                    bool LampEmits() => lamp!.MaterialOverride is StandardMaterial3D material
                        && material.EmissionEnabled && material.EmissionEnergyMultiplier > .1f;
                    Require(runtime.Points.GetValueOrDefault("material_call_on") is false && !LampEmits(), "released lamp visual/output not off");
                    Require(runtime.ExecuteAction("toggle-material-call"), "representative call rejected");
                    Require(runtime.Points.GetValueOrDefault("material_call_on") is true && LampEmits(), "pressed lamp visual/output not on");
                    Require(runtime.ExecuteAction("toggle-material-call"), "representative release rejected");
                    Require(runtime.Points.GetValueOrDefault("material_call_on") is false && !LampEmits(), "released lamp visual/output did not turn off");
                    runtime.ResetSimulation();
                    Require(!LampEmits(), "reset left representative lamp illuminated");
                    GD.Print("CONTROL_INSTALLATION_LAMP_PREVIEW PASS pressed=on released=off reset=off");
                }
                foreach (var binding in interactor.Bindings)
                {
                    // Strict mounting/feedback assertions cover adapted controls.
                    // Retained gallery/field controls keep their authored contract:
                    // gallery_selector deliberately toggles motion, not its detent.
                    if (!mounted.Any(n => n == binding.Node || n.IsAncestorOf(binding.Node)))
                    {
                        retainedFieldBindings++;
                        GD.Print($"CONTROL_INSTALLATION_RETAINED_BINDING scene={entry.Id} equipment={binding.EquipmentId} action={binding.ActionId} scope=authored-contract-only");
                        continue;
                    }
                    runtime.ResetSimulation();
                    var functional = binding.Node.FindChild("KIN_pushbutton", true, false) as Node3D
                        ?? binding.Node.FindChild("KIN_estop", true, false) as Node3D
                        ?? binding.Node.FindChild("KIN_selector_handle", true, false) as Node3D
                        ?? binding.Node as MeshInstance3D;
                    var boundDefinition = definition.Equipment.Single(e => e.Id == binding.EquipmentId);
                    var requiresFunctionalHead = mounted.Any(n => n == binding.Node || n.IsAncestorOf(binding.Node))
                        && (boundDefinition.Type is "switch" or "rotarySwitch");
                    if (functional is null || !functional.IsVisibleInTree())
                    {
                        Require(!requiresFunctionalHead, $"mounted operator lacks visible functional head: {binding.EquipmentId}/{binding.ActionId}");
                        knownNonHeadBindings++;
                        GD.Print($"CONTROL_INSTALLATION_NONHEAD_BINDING scene={entry.Id} equipment={binding.EquipmentId} action={binding.ActionId} type={boundDefinition.Type}");
                        continue;
                    }
                    var face = functional as MeshInstance3D ?? functional.FindChildren("*", "MeshInstance3D", true, false)
                        .OfType<MeshInstance3D>().FirstOrDefault(m => m.Mesh is not null && m.IsVisibleInTree());
                    Require(face?.Mesh is not null, $"functional head lacks visible mesh: {binding.EquipmentId}");
                    var centroid = face!.GlobalTransform * face.GetAabb().GetCenter();
                    Require(interactor.TryPick(_mainCamera!, _mainCamera!.UnprojectPosition(centroid), out var picked)
                        && picked?.EquipmentId == binding.EquipmentId && picked.ActionId == binding.ActionId,
                        $"functional centroid ray did not select exact control: {binding.EquipmentId}/{binding.ActionId}");
                    picks++;
                    var localHome = functional.Transform;
                    var rootHome = binding.Node.Transform;
                    string? selectorPoint = null;
                    object? selectorHomePoint = null;
                    if (functional.Name == "KIN_selector_handle")
                    {
                        Require(definition.Simulation.TryGetProperty("actions", out var selectorActions), "selector has no action definitions");
                        var selectorAction = selectorActions.EnumerateArray().Single(a => a.GetProperty("id").GetString() == binding.ActionId);
                        Require(selectorAction.TryGetProperty("point", out var pointElement), $"selector action lacks point: {binding.ActionId}");
                        selectorPoint = pointElement.GetString();
                        Require(selectorPoint is not null && runtime.Points.TryGetValue(selectorPoint, out selectorHomePoint), "selector point missing from runtime");
                    }
                    if (!runtime.ExecuteAction(binding.ActionId))
                    {
                        interactor.AdvanceFeedback(.08);
                        Require(functional.Transform.IsEqualApprox(localHome) && binding.Node.Transform.IsEqualApprox(rootHome),
                            $"rejected action animated control: {binding.EquipmentId}");
                        blocked++; continue;
                    }
                    if (functional.Name == "KIN_selector_handle")
                    {
                        Require(binding.Node.Transform.IsEqualApprox(rootHome), $"selector moved mounting root: {binding.EquipmentId}");
                        Require(float.IsFinite(functional.RotationDegrees.Z), "selector rotation invalid");
                        Require(!Equals(runtime.Points[selectorPoint!], selectorHomePoint), $"accepted selector action did not change point: {binding.EquipmentId}");
                        Require(!functional.Transform.IsEqualApprox(localHome), $"selector point changed without handle detent change: {binding.EquipmentId}");
                        selectors++;
                    }
                    else
                    {
                        interactor.PlayAcceptedFeedback(binding);
                        interactor.AdvanceFeedback(.08);
                        Require(functional.Position.DistanceTo(localHome.Origin) > .0001f, $"accepted button did not depress: {binding.EquipmentId}");
                        Require(binding.Node == functional || binding.Node.Transform.IsEqualApprox(rootHome), "button feedback moved mounting root");
                        interactor.AdvanceFeedback(.30);
                        if (binding.ReleaseWhenPoint.Length > 0)
                            interactor.SynchronizeFeedback(new Dictionary<string, object?> { [binding.ReleaseWhenPoint] = true });
                        Require(functional.Transform.IsEqualApprox(localHome), $"button did not restore: {binding.EquipmentId}");
                        presses++;
                    }
                    runtime.ResetSimulation();
                    if (selectorPoint is not null)
                    {
                        Require(Equals(runtime.Points[selectorPoint], selectorHomePoint), $"selector reset lost initial point: {binding.EquipmentId}");
                        Require(functional.Transform.IsEqualApprox(localHome), $"selector reset lost initial detent: {binding.EquipmentId}");
                    }
                    Require(poses.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "action/reset changed mounted poses");
                }
                runtime.UsesExternalClock = authoredClockMode;
                if (entry.Id == "lab-2-01-workstation-call")
                {
                    var lamp = (MeshInstance3D)root.GetNode<Node3D>("material_call_lamp").FindChild("LENS_single", true, false);
                    bool LampEmits() => lamp.MaterialOverride is StandardMaterial3D material
                        && material.EmissionEnabled && material.EmissionEnergyMultiplier > .1f;
                    // This lesson intentionally has a blank learner ladder.
                    // Supply an explicit ephemeral audit fixture, never save it
                    // or install it as the lesson's starter solution.
                    var document = new LadderEditorDocument();
                    document.ResetProject("review-workstation-call", "Workstation_Call_Review", TimeSpan.FromMilliseconds(20));
                    document.SourceSceneId = entry.Id;
                    document.AddTag("material_call_pressed", PlcVariableRole.Input, "material_call_pressed");
                    document.AddTag("material_call_on", PlcVariableRole.Output, "material_call_on");
                    document.AddRung("Call lamp follows material call", "material_call_on");
                    document.AddContact(0, 0, "material_call_pressed", false);
                    GD.Print("CONTROL_INSTALLATION_REFERENCE scene=" + entry.Id + " provenance=explicit-ephemeral-verifier-only sha256="
                        + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(LadderEditorProjectJson.Save(document)))));
                    EnableVirtualControllerProgram(document.BuildProgram(), resetScene: false);
                    ResetActiveController();
                    RunActiveController();
                    Require(_virtualController is not null, "representative built-in controller unavailable");
                    void ScanOnce() => _virtualController!.Advance(.020,
                        SampleVirtualControllerInputs, SampleVirtualControllerNumericInputs,
                        CommitVirtualControllerOutputs, CommitVirtualControllerNumericOutputs,
                        runtime.AdvanceSimulation, SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs);
                    void Trace(string step) => GD.Print("CONTROL_INSTALLATION_SCAN " + JsonSerializer.Serialize(new
                    { sceneId = entry.Id, provenance = "explicit-ephemeral-verifier-only", step,
                        scanNumber = _virtualController!.Snapshot.ScanNumber,
                        input = runtime.Points["material_call_pressed"], output = runtime.Points["material_call_on"], actualLensEmitting = LampEmits() }));
                    Require(runtime.ExecuteAction("toggle-material-call"), "controller call rejected");
                    ScanOnce();
                    Trace("pressed-after-scan");
                    Require(runtime.Points.GetValueOrDefault("material_call_on") is true && LampEmits(), "controller scan did not light call lamp");
                    Require(runtime.ExecuteAction("toggle-material-call"), "controller release rejected");
                    ScanOnce();
                    Trace("released-after-scan");
                    Require(runtime.Points.GetValueOrDefault("material_call_on") is false && !LampEmits(), "controller scan did not extinguish call lamp");
                    StopActiveController();
                    ResetActiveController();
                    Trace("stopped-reset");
                    Require(runtime.Points.GetValueOrDefault("material_call_on") is false && !LampEmits(), "controller reset left lamp on");
                    Require(poses.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "controller reset lost mounted poses");
                    GD.Print("CONTROL_INSTALLATION_LAMP_CONTROLLER PASS referenceFixture=explicit-verifier-only scan=actual pressed=on released=off stoppedReset=off");
                }
                if (entry.Id == "lab-4-08-package-grouping") VerifyInstalledGroupingController(root, runtime);
                if (entry.Id is "lab-4-01-press-count-lamp" or "lab-9-11-pallet-counting")
                    VerifyInstalledAuthoredCounter(entry.Id, root, runtime);
                GD.Print($"CONTROL_INSTALLATION_CASE PASS {entry.Id} mounted={mounted.Length} bindings={interactor.Count}");
                scenes++;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            var evidencePath = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--installation-evidence=", StringComparison.Ordinal))?
                .Substring("--installation-evidence=".Length);
            if (!string.IsNullOrWhiteSpace(evidencePath))
                System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(evidencePath), JsonSerializer.Serialize(measuredEvidence, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"CONTROL_INSTALLATIONS_VERIFY PASS scenes={scenes} authoredCaseScenes={cases} functionalPicks={picks} acceptedPresses={presses} selectors={selectors} prerequisiteBlockedActions={blocked} knownNonHeadBindings={knownNonHeadBindings} retainedFieldBindings={retainedFieldBindings} projectedIndicators={projectedIndicators} complexIndicatorBindings={complexIndicatorBindings} nonOutputIndicators={nonOutputIndicators} benchLensShelfChecks={benchLensShelfChecks} scope=scripted-native-mounted-controls-not-keyboard-not-clearance");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError($"CONTROL_INSTALLATIONS_VERIFY FAIL {error}");
            GetTree().Quit(1);
        }
    }

    private static Aabb ControlInstallationWorldBounds(MeshInstance3D mesh)
        => mesh.GlobalTransform * mesh.GetAabb();

    private void VerifyInstalledAuthoredCounter(string sceneId, Node3D root, SceneSimulationRuntime runtime)
    {
        void Require(bool result, string reason) { if (!result) throw new InvalidOperationException(sceneId + " authored installation seam: " + reason); }
        Require(AuthoredDemoLadderPrograms.TryCreate(sceneId, out var document), "authored document missing");
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(LadderEditorProjectJson.Save(document))));
        var program = document.BuildProgram();
        Require(LadderCompiler.Compile(program).IsValid, "authored program failed compile");
        EnableVirtualControllerProgram(program, resetScene: false);
        Require(_virtualController is not null, "authored program failed application attach");
        ResetActiveController(); RunActiveController();
        var bench = sceneId == "lab-4-01-press-count-lamp";
        var lamp = root.GetNode<Node3D>(bench ? "indicator_1" : "indicator_3");
        var lens = lamp.FindChild("LENS_single", true, false) as MeshInstance3D
            ?? (MeshInstance3D)lamp.FindChild("LENS_green", true, false);
        bool Emits() => lens.MaterialOverride is StandardMaterial3D material && material.EmissionEnabled && material.EmissionEnergyMultiplier > .1f;
        var output = bench ? "threshold_lamp" : "pallet_count_valid";
        var counter = bench ? "press_count" : "batch_count";
        void Tick() => _PhysicsProcess(.020);
        void Trace(string step) => GD.Print("CONTROL_INSTALLATION_SCAN " + JsonSerializer.Serialize(new
        { sceneId, provenance = "AuthoredDemoLadderPrograms", step, scanNumber = _virtualController!.Snapshot.ScanNumber,
            inputs = runtime.SampleVirtualControllerInputs(), output = runtime.Points[output], counterValue = _virtualController.Snapshot.Counters[counter].Accumulated,
            numericCount = bench ? (object?)null : runtime.Points["pallet_count"], actualLensEmitting = Emits() }));
        void Action(string id)
        {
            Require(ExecuteSelectedControllerAction(id), "rejected authored input " + id);
            Trace("accepted-before-scan:" + id);
            var binding = _sceneControlInteractor!.Bindings.Single(b => b.ActionId == id);
            var cap = binding.Node.FindChild("KIN_pushbutton", true, false) as Node3D;
            if (cap is not null)
            {
                var home = cap.Transform;
                _sceneControlInteractor.PlayAcceptedFeedback(binding); _sceneControlInteractor.AdvanceFeedback(.08);
                Require(!cap.Transform.IsEqualApprox(home), "accepted input lacks actual cap feedback");
                _sceneControlInteractor.AdvanceFeedback(.30);
                Require(cap.Transform.IsEqualApprox(home), "accepted cap failed release");
            }
        }
        Require(runtime.Points[output] is false && !Emits(), "initial output/lens not off");
        if (!bench) { Action("toggle-pallet_type_valid"); Action("toggle-count_request"); Tick(); }
        var target = bench ? 3 : 5;
        for (var count = 1; count <= target; count++)
        {
            Action(bench ? "toggle-pulse_received" : "toggle-pallet_detected"); Tick(); Tick();
            Trace("event-after-scan-" + count);
            Require(_virtualController!.Snapshot.Counters[counter].Accumulated == count, "actual scan counter differs at " + count);
            if (!bench)
            {
                Require(Convert.ToInt64(runtime.Points["pallet_count"]) == count, "mapped numeric output differs");
                var label = root.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout");
                Require(label.Text.Contains(count.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal), "actual display does not publish count");
                Action("toggle-pallet_detected"); Tick();
            }
            Require(Equals(runtime.Points[output], count == target) && Emits() == (count == target), "authored threshold output/lens mismatch");
            Trace("accepted-event-" + count);
        }
        StopActiveController();
        Trace("stopped");
        Require(runtime.Points[output] is false && !Emits(), "stop output/lens not off");
        ResetActiveController();
        Trace("reset");
        Require(runtime.Points[output] is false && !Emits() && _virtualController!.Snapshot.Counters[counter].Accumulated == 0,
            "authored reset did not clear counter/output/lens");
        if (!bench) Require(Convert.ToInt64(runtime.Points["pallet_count"]) == 0,
            "authored reset did not clear numeric display point");
        GD.Print($"CONTROL_INSTALLATION_AUTHORED_CONTROLLER PASS scene={sceneId} provenance=AuthoredDemoLadderPrograms sha256={fingerprint} compile=actual appAttach=actual acceptedInputs={target} scanCounter={target} actualLens=true numericDisplay={!bench} feedback=true stop=off reset=zero scope=offline-not-physical-plc");
        DisableVirtualController();
    }

    private void VerifyInstalledGroupingController(Node3D root, SceneSimulationRuntime runtime)
    {
        // This learner lesson has no authored starter program. Reuse the
        // established review logic as an explicit ephemeral reference fixture;
        // acceptance follows input sampling -> controller scan -> real plant
        // movement/output projection, never injected output values.
        var document = new LadderEditorDocument();
        document.ResetProject("review-installed-package-grouping", "Installed_Grouping_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-4-08-package-grouping";
        foreach (var name in new[] { "machine_enabled", "release_clear", "package_detected", "group_staged", "stop_raised", "receiver_occupied", "transfer_complete" })
            document.AddTag(name, PlcVariableRole.Input, name);
        foreach (var name in new[] { "group_conveyor_run", "group_release", "group_ready" }) document.AddTag(name, PlcVariableRole.Output, name);
        document.AddTag("group_count", PlcVariableRole.Output, "group_count", PlcVariableType.DInt);
        document.AddTag("arrivals", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("release_active", PlcVariableRole.Memory);
        document.AddTag("publish", PlcVariableRole.Memory, initialValue: true);
        document.AddCounterRung("Count actual incoming beam rising edges", "arrivals", 3);
        document.AddContact(0, 0, "package_detected", false);
        document.AddNumericOperationRung("Publish PLC count", LadderNumericOperationKind.Move, "arrivals.ACC", "", "group_count");
        document.AddContact(1, 0, "publish", false);
        document.AddRung("Latch release after physical accumulation", "release_active").CoilMode = LadderCoilMode.Set;
        foreach (var name in new[] { "machine_enabled", "release_clear", "arrivals.DN", "group_staged" }) document.AddContact(2, 0, name, false);
        document.AddRung("Completed transfer clears release", "release_active").CoilMode = LadderCoilMode.Reset;
        document.AddContact(3, 0, "transfer_complete", false);
        document.AddRung("Enable rollers with clear path", "group_conveyor_run");
        document.AddContact(4, 0, "machine_enabled", false); document.AddContact(4, 0, "release_clear", false); document.AddContact(4, 0, "transfer_complete", true);
        document.AddRung("Maintain permissive release", "group_release");
        foreach (var name in new[] { "machine_enabled", "release_clear", "release_active" }) document.AddContact(5, 0, name, false);
        document.AddRung("Count reached lamp", "group_ready");
        document.AddContact(6, 0, "arrivals.DN", false); document.AddContact(6, 0, "transfer_complete", true);
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(LadderEditorProjectJson.Save(document))));
        EnableVirtualControllerProgram(document.BuildProgram(), resetScene: false);
        void Require(bool result, string reason) { if (!result) throw new InvalidOperationException("grouping controller seam: " + reason); }
        Require(_virtualController is not null, "reference did not compile/attach");
        var plant = runtime.PackageGroupingPlant!;
        var carton = root.GetNode<Node3D>("carton_0");
        var roller = root.GetNode<Node3D>("group_line").FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .First(m => m.Name.ToString().StartsWith("KIN_group_roller_", StringComparison.Ordinal));
        var lens = (MeshInstance3D)root.GetNode<Node3D>("group_ready_lamp").FindChild("LENS_single", true, false);
        bool Emits() => lens.MaterialOverride is StandardMaterial3D m && m.EmissionEnabled && m.EmissionEnergyMultiplier > .1f;
        void Tick() => _virtualController!.Advance(.020, SampleVirtualControllerInputs, SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs, CommitVirtualControllerNumericOutputs, runtime.AdvanceSimulation,
            SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs);
        void Action(string id)
        {
            Require(ExecuteSelectedControllerAction(id), "rejected input " + id);
            Trace("accepted-before-scan:" + id);
        }
        void Trace(string step) => GD.Print("CONTROL_INSTALLATION_SCAN " + JsonSerializer.Serialize(new
        { sceneId = "lab-4-08-package-grouping", provenance = "explicit-ephemeral-verifier-only", step,
            scanNumber = _virtualController!.Snapshot.ScanNumber, inputs = runtime.SampleVirtualControllerInputs(),
            conveyorOutput = runtime.Points["group_conveyor_run"], releaseOutput = runtime.Points["group_release"], readyOutput = runtime.Points["group_ready"],
            countOutput = runtime.Points["group_count"], actualCartonX = carton.Position.X, actualRollerPose = roller.RotationDegrees.ToString(), actualLensEmitting = Emits() }));
        ResetActiveController(); RunActiveController();
        Action("toggle-machine_enabled"); Action("toggle-release_clear"); Action("load-carton");
        var initialCarton = carton.Transform; var initialRoller = roller.Transform;
        for (var i = 0; i < 25; i++) Tick();
        Trace("accepted-permissives-and-load-moving");
        Require(runtime.Points["group_conveyor_run"] is true && carton.Visible
            && !carton.Transform.IsEqualApprox(initialCarton) && !roller.Transform.IsEqualApprox(initialRoller), "scan command did not move actual carton and roller");
        var holdCarton = carton.Transform; var holdRoller = roller.Transform;
        StopActiveController(); var holdScan = _virtualController!.Snapshot.ScanNumber;
        Tick(); runtime.AdvanceSimulation(.1);
        Trace("stopped-hold");
        Require(carton.Transform.IsEqualApprox(holdCarton) && roller.Transform.IsEqualApprox(holdRoller)
            && _virtualController.Snapshot.ScanNumber == holdScan, "stop did not hold actual plant/scan");
        RunActiveController();
        for (var batch = 0; batch < 3; batch++)
        {
            if (batch > 0) Action("load-carton");
            var ticks = 0;
            while (plant.ActiveFeed >= 0 && ticks++ < 1100) Tick();
            Require(plant.ActiveFeed < 0, "feed timed out"); Tick();
            Trace("actual-arrival-" + (batch + 1));
        }
        Require(Convert.ToInt64(runtime.Points["group_count"]) == 3 && runtime.Points["group_ready"] is true && Emits(), "real beam arrivals did not drive count/ready lens");
        ResetActiveController();
        Trace("reset");
        Require(Convert.ToInt64(runtime.Points["group_count"]) == 0 && runtime.Points["group_ready"] is false && !Emits()
            && !carton.Visible && _virtualController!.Snapshot.ScanNumber == 0, "reset did not clear count/lens/actual plant");
        GD.Print($"CONTROL_INSTALLATION_GROUP_CONTROLLER PASS provenance=explicit-ephemeral-verifier-only sha256={fingerprint} actualScans=true actualCartonAndRollerMotion=true beamCount=3 readyLens=on stopHold=true reset=off");
        DisableVirtualController();
    }
}
