using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main
{
    // Opt-in source-worktree regression. No learner solution, controller output
    // fixture, external PLC, physical installation or rendered-readability claim.
    private async void VerifyIndustrialMounts()
    {
        var checks = 0; var failures = new List<string>(); var evidence = new List<object>();
        void Check(bool condition, string name)
        {
            checks++;
            if (!condition) { failures.Add(name); GD.Print("INDUSTRIAL_MOUNT FAIL " + name); }
        }
        try
        {
            var docs = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../docs/studio-execution/2026-10-10"));
            using var lights = JsonDocument.Parse(File.ReadAllText(Path.Combine(docs, "industrial-light-mount-inventory-v1.json")));
            using var displays = JsonDocument.Parse(File.ReadAllText(Path.Combine(docs, "industrial-display-mount-inventory-v1.json")));
            using var frozen = JsonDocument.Parse(File.ReadAllText(Path.Combine(docs, "repeat-cycle-source-freeze-v7.json")));
            var frozenSources = frozen.RootElement.GetProperty("source").EnumerateArray().ToArray();
            var lightScenes = lights.RootElement.GetProperty("scenes").EnumerateArray().ToArray();
            var displayScenes = displays.RootElement.GetProperty("scenes").EnumerateArray().ToArray();
            Check(lightScenes.Length == 77 && displayScenes.Length == 77, "baseline_all_77_scenes");
            var lightCount = lightScenes.Sum(s => s.GetProperty("lights").EnumerateArray().Count(e =>
                !new[] { "optical-sensor-not-indicator", "relay-not-beacon-name-mismatch" }.Contains(e.GetProperty("classification").GetString())));
            Check(lightCount == 121, "baseline_121_actual_lights_including_8_roadway");
            Check(displayScenes.Sum(s => s.GetProperty("displays").GetArrayLength()) == 54, "baseline_54_display_candidate_records");
            var catalog = SceneCatalogLoader.LoadCatalog("res://scenes/catalog/original-scenes.catalog.json");
            Check(catalog.Scenes.Count() == 77, "catalog_77");
            foreach (var entry in catalog.Scenes)
            {
                var authored = SceneCatalogLoader.LoadScene(entry);
                var sceneSource = lightScenes.Single(s => s.GetProperty("sceneId").GetString() == entry.Id).GetProperty("sourcePath").GetString()!.Replace('\\', '/');
                var frozenSource = frozenSources.Single(s => s.GetProperty("path").GetString()!.Replace('\\', '/') == sceneSource);
                var sceneHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(ProjectSettings.GlobalizePath("res://" + sceneSource))));
                Check(sceneHash == frozenSource.GetProperty("sha256").GetString(), entry.Id + "/immutable_v7_authored_source_sha256");
                AddMigratedScene(entry.Id, _candidateCatalog!, _mainCamera!, false, false);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var root = _sceneCompositionRoot!; var definition = _activeSceneDefinition!;
                Check(definition.Simulation.GetRawText() == authored.Simulation.GetRawText(), entry.Id + "/source_point_binding_contract_unchanged");
                var records = lightScenes.Single(s => s.GetProperty("sceneId").GetString() == entry.Id).GetProperty("lights").EnumerateArray()
                    .Concat(displayScenes.Single(s => s.GetProperty("sceneId").GetString() == entry.Id).GetProperty("displays").EnumerateArray()).ToArray();
                foreach (var record in records)
                {
                    var id = record.GetProperty("equipmentId").GetString()!;
                    var classification = record.GetProperty("classification").GetString()!;
                    var node = root.GetNodeOrNull<Node3D>(id.Replace('-', '_'));
                    Check(node is not null, entry.Id + "/" + id + "/inventory_root_exists");
                    if (node is null) continue;
                    var item = definition.Equipment.Single(e => e.Id == id);
                    if (record.TryGetProperty("config", out var frozenConfig))
                        Check(IndustrialJsonIdentity(item.Config) == IndustrialJsonIdentity(frozenConfig), entry.Id + "/" + id + "/frozen_inventory_config");
                    var actualBindings = definition.Simulation.GetProperty("pointBindings").EnumerateArray().Where(b => b.TryGetProperty("equipmentId", out var eq) && eq.GetString() == id).ToArray();
                    var baselineBindings = record.GetProperty("pointBindings").EnumerateArray().Select(b => b.TryGetProperty("binding", out var wrapped) ? wrapped : b).ToArray();
                    // The two annotation-only records have no binding snapshot;
                    // their source semantics are protected by the V7 scene hash.
                    if (classification != "runtime-floating-observation-annotation-needs-explicit-design")
                        Check(baselineBindings.Select(IndustrialJsonIdentity).OrderBy(v => v).SequenceEqual(actualBindings.Select(IndustrialJsonIdentity).OrderBy(v => v)), entry.Id + "/" + id + "/frozen_inventory_point_bindings");
                    Check(item.Config.GetRawText() == authored.Equipment.Single(e => e.Id == id).Config.GetRawText(), entry.Id + "/" + id + "/config_action_retained");
                    var meshes = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Where(m => m.Mesh is not null).ToArray();
                    foreach (var binding in definition.Simulation.GetProperty("pointBindings").EnumerateArray().Where(b => b.TryGetProperty("equipmentId", out var eq) && eq.GetString() == id))
                    {
                        var mode = binding.TryGetProperty("mode", out var md) ? md.GetString() : "";
                        if (mode is "numericDisplay" or "textDisplay")
                            Check(node.FindChild(mode == "numericDisplay" ? "NumericReadout" : "StaticReadout", true, false) is Label3D,
                                entry.Id + "/" + id + "/retained_typed_" + mode + "_label_path");
                    }
                    if (item.Type == "indicator")
                    {
                        var colors = item.Config.TryGetProperty("colors", out var c) ? c.EnumerateArray().Select(v => v.GetString()!).ToArray() : new[] { "green" };
                        foreach (var color in colors)
                            Check(meshes.Any(m => m.Name == (colors.Length == 1 ? "LENS_single" : "LENS_" + color) && m.IsVisibleInTree()), entry.Id + "/" + id + "/retained_lens_" + color);
                    }
                    var needsMount = classification is "cabinet-panel-mounted" or "cabinet-status-tower" or "training-bench-mounted"
                        or "unmounted-industrial-needs-fix" or "unmounted-floor-counter-needs-fix" or "unmounted-floor-display-needs-fix"
                        or "checkweigher-separate-floor-readout-needs-machine-support" or "stand-backed-drive-screen-station-review";
                    if (!needsMount) { evidence.Add(new { scene = entry.Id, id, classification, scope = "retained_non_panel_or_annotation_class" }); continue; }
                    Check(node.HasMeta("installationKind"), entry.Id + "/" + id + "/explicit_mount_required");
                    var kind = node.HasMeta("installationKind") ? node.GetMeta("installationKind").AsString() : "missing";
                    Check(new[] { "studentBench", "operatorPanel", "cabinetStatusTower", "machineFrameReadout" }.Contains(kind), entry.Id + "/" + id + "/recognized_mount_kind");
                    bool OriginalBase(string name) => name == "POLE" || name.StartsWith("BASE_", StringComparison.Ordinal)
                        || new[] { "COUNT_DISPLAY_base", "COUNT_DISPLAY_mast", "STATIC_READOUT_base", "STATIC_READOUT_mast", "LENGTH_DISPLAY_base", "LENGTH_DISPLAY_mast", "LABEL_DISPLAY_base", "LABEL_DISPLAY_mast", "STAND_base", "STAND_mast", "METER_base", "METER_pedestal", "COUNT_foot", "COUNT_mast", "WEIGH_readout_foot", "WEIGH_readout_mast" }.Contains(name);
                    Check(meshes.Where(m => OriginalBase(m.Name.ToString())).All(m => !m.IsVisibleInTree()), entry.Id + "/" + id + "/original_floor_support_hidden");
                    var support = root.GetNodeOrNull<Node3D>("ControlInstallation");
                    var supportMeshes = kind == "machineFrameReadout"
                        ? meshes.Where(m => m.Name == "WEIGH_readout_frame_arm").ToArray()
                        : support?.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                            .Where(m => m.IsVisibleInTree() && m.Mesh is not null && (m.Name == "OperatorCabinet_Door" || m.Name == "OperatorCabinet_Body"
                                || m.Name == "StudentBench_Worktop" || m.Name == "InstrumentTier" || m.Name == "TrainingBackboard"
                                || m.Name == "StatusTowerCoupler_" + id)).ToArray() ?? System.Array.Empty<MeshInstance3D>();
                    if (kind == "cabinetStatusTower")
                        supportMeshes = supportMeshes.Where(m => m.Name == "StatusTowerCoupler_" + id).ToArray();
                    var visible = meshes.Where(m => m.IsVisibleInTree() && !m.Name.ToString().StartsWith("WEIGH_readout_frame_", StringComparison.Ordinal)).ToArray();
                    if (kind == "machineFrameReadout")
                        visible = visible.Where(m => m.Name == "WEIGH_readout_housing" || m.Name == "WEIGH_readout_screen").ToArray();
                    Check(visible.Length > 0 && supportMeshes.Length > 0, entry.Id + "/" + id + "/visible_head_and_support");
                    // Contact must be real retained-head mesh contact, not merely metadata.
                    Check(visible.Any(h => supportMeshes.Any(s => IndustrialBoundsGap(ControlInstallationWorldBounds(h), ControlInstallationWorldBounds(s)) <= .012f)), entry.Id + "/" + id + "/head_support_contact_12mm");
                    if (kind == "machineFrameReadout")
                    {
                        var arms = meshes.Where(m => m.IsVisibleInTree() && m.Name == "WEIGH_readout_frame_arm").ToArray();
                        var posts = meshes.Where(m => m.IsVisibleInTree() && m.Name == "WEIGH_readout_frame_post").ToArray();
                        var frames = meshes.Where(m => m.IsVisibleInTree() && m.Name.ToString().StartsWith("WEIGH_FRAME_side_", StringComparison.Ordinal)).ToArray();
                        Check(arms.Length == 1 && posts.Length == 1 && frames.Length > 0, entry.Id + "/" + id + "/exact_machine_support_chain_exists");
                        Check(arms.Any(a => posts.Any(p => IndustrialBoundsGap(ControlInstallationWorldBounds(a), ControlInstallationWorldBounds(p)) <= .003f)), entry.Id + "/" + id + "/arm_to_post_contact");
                        Check(posts.Any(p => frames.Any(f => IndustrialBoundsGap(ControlInstallationWorldBounds(p), ControlInstallationWorldBounds(f)) <= .003f)), entry.Id + "/" + id + "/post_to_machine_frame_contact");
                    }
                    if (kind == "cabinetStatusTower")
                    {
                        var body = support?.GetNodeOrNull<MeshInstance3D>("OperatorCabinet_Body");
                        Check(body is not null && supportMeshes.Length == 1 && visible.Length > 0, entry.Id + "/" + id + "/exact_roof_coupler_body_exists");
                        if (body is not null && supportMeshes.Length == 1 && visible.Length > 0)
                        {
                            var headBounds = visible.Select(ControlInstallationWorldBounds).Aggregate((a, b) => a.Merge(b));
                            var couplerBounds = ControlInstallationWorldBounds(supportMeshes[0]);
                            Check(MathF.Abs(headBounds.Position.Y - couplerBounds.End.Y) <= .003f, entry.Id + "/" + id + "/head_bottom_exact_coupler_top_bearing");
                            // Roof packing is measured in the cabinet's own frame,
                            // so a yawed cabinet cannot pass through a loose world AABB.
                            var inverse = support!.GlobalTransform.AffineInverse();
                            var localHead = visible.Select(m => inverse * m.GlobalTransform * m.GetAabb()).Aggregate((a, b) => a.Merge(b));
                            var localBody = body.Transform * body.GetAabb();
                            Check(localHead.Position.X >= localBody.Position.X - .003f && localHead.End.X <= localBody.End.X + .003f
                                && localHead.Position.Z >= localBody.Position.Z - .003f && localHead.End.Z <= localBody.End.Z + .003f,
                                entry.Id + "/" + id + "/full_head_xz_envelope_within_cabinet_roof");
                        }
                    }
                    var pose = node.Transform;
                    var paths = node.FindChildren("*", "", true, false).Select(n => node.GetPathTo(n).ToString()).OrderBy(p => p).ToArray();
                    _sceneRuntime!.ResetSimulation();
                    Check(node.Transform.IsEqualApprox(pose) && paths.SequenceEqual(node.FindChildren("*", "", true, false).Select(n => node.GetPathTo(n).ToString()).OrderBy(p => p)), entry.Id + "/" + id + "/reset_preserves_pose_controller_lens_readout_paths");
                    evidence.Add(new { scene = entry.Id, id, classification, actualMount = kind, retainedPaths = paths });
                }
                if (WorkspaceTemplatePolicy.Supports(entry.Id))
                {
                    var support = root.GetNode<Node3D>("ControlInstallation"); var original = support.Transform;
                    var heads = root.GetChildren().OfType<Node3D>().Where(n => n != support && n.HasMeta("installationKind")).ToDictionary(n => n, n => n.Transform);
                    SceneComposer.ApplyTemplateOverride(root, definition, new WorkspaceTemplateOverride(1, "ControlInstallation", WorkspaceTemplatePolicy.Fingerprint(definition), new[] { .4, 0.0, .3 }, 15));
                    var delta = support.Transform * original.AffineInverse();
                    Check(heads.Count > 0 && heads.All(h => h.Key.Transform.IsEqualApprox(delta * h.Value)), entry.Id + "/supported_template_sibling_heads_follow_support");
                    SceneComposer.ApplyTemplateOverride(root, definition, null);
                    Check(support.Transform.IsEqualApprox(original) && heads.All(h => h.Key.Transform.IsEqualApprox(h.Value)), entry.Id + "/supported_template_restore_exact");
                }
            }
            var path = ProjectSettings.GlobalizePath("res://.tools/industrial-mount-review-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(new { checks, failures, evidence, scope = "native_geometry_source_regression_not_rendered_readability_or_physical_acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"INDUSTRIAL_MOUNT_RESULT cases={checks} passed={checks - failures.Count} failed={failures.Count} scenes=77 lights=121 displayCandidates=54 evidence={path}");
            GetTree().Quit(failures.Count == 0 ? 0 : 1);
        }
        catch (Exception ex) { GD.PrintErr("INDUSTRIAL_MOUNT_EXCEPTION " + ex); GetTree().Quit(1); }
    }

    private static float IndustrialBoundsGap(Aabb a, Aabb b)
    {
        var x = MathF.Max(0, MathF.Max(a.Position.X - b.End.X, b.Position.X - a.End.X));
        var y = MathF.Max(0, MathF.Max(a.Position.Y - b.End.Y, b.Position.Y - a.End.Y));
        var z = MathF.Max(0, MathF.Max(a.Position.Z - b.End.Z, b.Position.Z - a.End.Z));
        return MathF.Sqrt(x * x + y * y + z * z);
    }

    private static string IndustrialJsonIdentity(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + IndustrialJsonIdentity(p.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(IndustrialJsonIdentity)) + "]",
        JsonValueKind.Number => value.GetDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.GetRawText()
    };
}

