using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    // Opt-in evidence camera only: neither normal framing nor saved workspace
    // cameras change. The real no-focus renderer captures the actual geometry.
    private void ApplyControlInstallationCaptureView()
    {
        var view = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture-station-view=", StringComparison.Ordinal))?
            .Substring("--capture-station-view=".Length);
        if (view is null || _sceneCamera is null || _sceneCompositionRoot is null) return;
        if (view == "field-face")
        {
            var id = _currentSceneId switch
            {
                "tank-level" => "level_transmitter",
                "tank-radar" => "radar_transmitter",
                _ => throw new InvalidOperationException("Face evidence requires an inspected tank instrument."),
            };
            var device = _sceneCompositionRoot.GetNode<Node3D>(id);
            var face = device.FindChild("DISPLAY_window", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException("Retained instrument display window missing.");
            var meshes = device.FindChildren("*", "MeshInstance3D", true, false)
                .OfType<MeshInstance3D>().Where(mesh => mesh.Name.ToString().StartsWith("DISPLAY_", StringComparison.Ordinal)).ToArray();
            FitIndustrialMountCapture(meshes, device.GlobalBasis * Vector3.Back, view);
            GD.Print($"FIELD_FACE_WITNESS scene={_currentSceneId} device={id} face={face.GetPath()} labels={device.FindChildren("*", "Label3D", true, false).Count}");
            return;
        }
        if (view is "purpose-detail" or "purpose-exterior")
        {
            if (view == "purpose-exterior" && _currentSceneId != "conveyor-cell")
                throw new InvalidOperationException("Exterior purpose view requires the inspection annex.");
            var environment = _demoEnvironment ?? throw new InvalidOperationException("Purpose view requires the actual environment.");
            MeshInstance3D RequiredMesh(string name) => environment.FindChild(name, true, false) as MeshInstance3D
                ?? throw new InvalidOperationException("Missing purpose witness: " + name);
            Label3D RequiredLabel(string name) => environment.FindChild(name, true, false) as Label3D
                ?? throw new InvalidOperationException("Missing purpose legend: " + name);
            MeshInstance3D[] meshes;
            Vector3 direction;
            if (_currentSceneId is "lab-10-01-drive-alarm-code-string" or "lab-10-02-chicken-label-print")
            {
                var furnishing = RequiredMesh(_currentSceneId == "lab-10-01-drive-alarm-code-string"
                    ? "ClosedCommunicationsServiceBox" : "ClosedPackagingSupplyCupboard");
                meshes = new[] { furnishing };
                // Inspect through the existing mouth from the process side.
                // No walls are hidden or modified for these diagnostic views.
                var annex = (Node3D)furnishing.GetParent();
                direction = annex.GlobalBasis * new Vector3(0, .55f, -MathF.Sign(furnishing.Position.Z));
            }
            else if (_currentSceneId == "lab-10-04-motor-enum-state")
            {
                meshes = new[] { RequiredMesh("EnumStateReferenceBearingPlate"), RequiredMesh("MotorStateSupportedReviewSill") };
                direction = RequiredLabel("EnumStateReferenceStaticLegend").GlobalBasis * Vector3.Back + Vector3.Up * .25f;
            }
            else if (_currentSceneId == "lab-2-04-two-station-call")
            {
                meshes = new[] { RequiredMesh("NorthCallBoardBackplate"), RequiredMesh("SouthCallBoardBackplate") };
                direction = RequiredLabel("NorthCallBoardInstruction").GlobalBasis * Vector3.Back + Vector3.Up * .25f;
            }
            else if (_currentSceneId is "conveyor-cell" or "lab-10-03-vision-package-sorter")
            {
                var annex = environment.FindChild(_currentSceneId == "conveyor-cell"
                    ? "ConnectedPurposeServiceBay" : "OutgoingDockServiceExtension", true, false) as Node3D
                    ?? throw new InvalidOperationException("Missing measured annex.");
                meshes = annex.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
                var inward = environment.ProtectedProcessBounds.GetCenter() - annex.GlobalPosition;
                direction = new Vector3(inward.X, 2, inward.Z).Normalized();
                if (_currentSceneId == "lab-10-03-vision-package-sorter")
                {
                    meshes = meshes.Where(mesh => mesh.Name.ToString().StartsWith("ReferenceStorage", StringComparison.Ordinal)).ToArray();
                    // Look down through the real open upper frontage; retain
                    // the sill and all walls while exposing lower shelf bearings.
                    direction = new Vector3(inward.X, 0, inward.Z).Normalized() + Vector3.Up * .85f;
                }
                else if (view == "purpose-exterior")
                    direction = -new Vector3(inward.X, 0, inward.Z).Normalized() + Vector3.Up * .4f;
            }
            else throw new InvalidOperationException("Purpose detail requires an explicitly inspected scene.");
            FitIndustrialMountCapture(meshes, direction, view);
            if (_capturePath is not null)
            {
                float[] V(Vector3 value) => new[] { value.X, value.Y, value.Z };
                object B(Aabb value) => new { position = V(value.Position), size = V(value.Size) };
                object[] Geometry(Node3D owner) => owner.FindChildren("*", "MeshInstance3D", true, false)
                    .OfType<MeshInstance3D>().Where(mesh => mesh.Mesh is not null && mesh.IsVisibleInTree())
                    .Select(mesh => (object)new { path = owner.GetPathTo(mesh).ToString(),
                        worldBounds = B(mesh.GlobalTransform * mesh.GetAabb()) }).ToArray();
                var witness = new { sceneId = _currentSceneId, scope = "actual-visible-world-AABBs-not-continuous-clearance",
                    protectedProcess = B(environment.ProtectedProcessBounds),
                    operatorStanding = environment.OperatorAccessBounds is Aabb access ? B(access) : null,
                    cameraEye = V(_sceneCamera.GlobalPosition), cameraForward = V(-_sceneCamera.GlobalBasis.Z),
                    environmentMeshes = Geometry(environment), plantMeshes = Geometry(_sceneCompositionRoot) };
                System.IO.File.WriteAllText(_capturePath + ".purpose.json", System.Text.Json.JsonSerializer.Serialize(witness,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            return;
        }
        if (view == "field-joints")
        {
            if (_currentSceneId != "lab-4-11-barrel-fill-station")
                throw new InvalidOperationException("Joint evidence requires the barrel installation.");
            if (OS.GetCmdlineUserArgs().Contains("--capture-barrel-valve-open", StringComparer.Ordinal))
            {
                // Explicit output-projection fixture; no plant time or flow.
                _sceneRuntime!.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool>
                    { ["fill_valve_open"] = true });
                _sceneRuntime.SetControllerPlaybackRunning(true);
                _sceneRuntime.SetControllerPlaybackRunning(false);
                GD.Print("BARREL_POINTER_QA open-command projection only; no learner or flow result");
            }
            WriteBarrelInterfaceWitness();
            var meshes = new[] { "valve_2", "training_accessory_6" }
                .SelectMany(id => _sceneCompositionRoot.GetNode<Node3D>(id).FindChildren("*", "MeshInstance3D", true, false))
                .OfType<MeshInstance3D>().Where(mesh => mesh.Name.ToString().Contains("bore", StringComparison.Ordinal)
                    || mesh.Name.ToString().Contains("socket", StringComparison.OrdinalIgnoreCase)
                    || mesh.Name.ToString().StartsWith("METER_inlet", StringComparison.Ordinal)
                    || mesh.Name.ToString().StartsWith("METER_outlet", StringComparison.Ordinal)).ToArray();
            FitIndustrialMountCapture(meshes, new Vector3(3, .35f, 1), view);
            return;
        }
        if (view is "field-detail" or "field-reverse" or "field-meter" or "field-source")
        {
            if (_currentSceneId == "lab-4-11-barrel-fill-station") WriteBarrelInterfaceWitness();
            if ((view is "field-meter" or "field-source") && _currentSceneId != "lab-4-11-barrel-fill-station")
                throw new InvalidOperationException("Meter/source detail requires the barrel fill scene.");
            if (view == "field-reverse" && _currentSceneId != "lab-11-12-mobile-traffic-lights")
                throw new InvalidOperationException("Reverse field witness is restricted to road B's opposite-facing signal.");
            var ids = _currentSceneId switch
            {
                "lab-11-12-mobile-traffic-lights" => new[] { view == "field-reverse" ? "indicator_1" : "indicator_0" },
                "lab-2-16-safe-drill" => new[] { "left_hand_button", "right_hand_button" },
                "lab-2-21-tote-finishing" => new[] { "vision_inspector" },
                "lab-4-11-barrel-fill-station" => view == "field-meter" ? new[] { "training_accessory_6" }
                    : view == "field-source" ? new[] { "tank_1" }
                    : new[] { "tank_1", "training_accessory_6", "training_accessory_7" },
                "tank-high-low" => new[] { "hl_low_switch", "hl_high_switch" },
                "tank-level" => new[] { "low_level_switch", "high_level_switch", "level_transmitter" },
                "tank-radar" => new[] { "radar_transmitter" },
                _ => throw new InvalidOperationException("Field evidence view requires an explicitly reviewed scene."),
            };
            var devices = ids.Select(id => _sceneCompositionRoot.GetNode<Node3D>(id)).ToArray();
            var meshes = devices.SelectMany(device => device.FindChildren("*", "MeshInstance3D", true, false))
                .OfType<MeshInstance3D>()
                // The separate whole installation witness retains the feet.
                // This source detail frames the vessel and outlet ink/interfaces.
                .Where(mesh => view != "field-source" || (!mesh.Name.ToString().StartsWith("SUPPLY_leg_", StringComparison.Ordinal)
                    && !mesh.Name.ToString().StartsWith("SUPPLY_foot_", StringComparison.Ordinal))).ToArray();
            var direction = _currentSceneId switch
            {
                "lab-11-12-mobile-traffic-lights" => devices[0].GlobalBasis * Vector3.Back + Vector3.Up * .12f,
                "tank-high-low" or "tank-level" => new Vector3(2.5f, .6f, 2.5f),
                "tank-radar" => new Vector3(1.2f, 1.0f, 2.5f),
                _ => new Vector3(.4f, .35f, 2.5f),
            };
            // The existing tote annotation is a billboard above the mesh.
            // Include its measured ink envelope in this diagnostic framing.
            var extra = devices.SelectMany(device => device.FindChildren("*", "Label3D", true, false))
                .OfType<Label3D>().Where(label => label.IsVisibleInTree()).SelectMany(label =>
                {
                    var lines = label.Text.Split('\n');
                    var font = label.Font ?? ThemeDB.FallbackFont;
                    var width = lines.Max(line => font.GetStringSize(line, HorizontalAlignment.Left, -1, label.FontSize).X) * label.PixelSize;
                    var height = font.GetStringSize("Mg", HorizontalAlignment.Left, -1, label.FontSize).Y * lines.Length * label.PixelSize;
                    return new[] { label.GlobalPosition - new Vector3(width / 2, height / 2, width / 2),
                        label.GlobalPosition + new Vector3(width / 2, height / 2, width / 2) };
                }).ToArray();
            FitIndustrialMountCapture(meshes, direction, view, extra);
            return;
        }
        if (view == "accessory-support")
        {
            if (_currentSceneId is not ("lab-3-01-guarded-pallet-transfer" or "lab-11-19-powder-batch-mixer"))
                throw new InvalidOperationException("Accessory evidence view requires one of the two static exhibit repairs.");
            var retained = _sceneCompositionRoot.GetNode<Node3D>("training_accessory_8");
            var exhibit = _sceneCompositionRoot.GetNode<Node3D>(retained.GetMeta("accessorySupportNode").AsString());
            var meshes = retained.FindChildren("*", "MeshInstance3D", true, false)
                .Concat(exhibit.FindChildren("*", "MeshInstance3D", true, false))
                .OfType<MeshInstance3D>().ToArray();
            FitIndustrialMountCapture(meshes, new Vector3(1.2f, .7f, 2.5f), view);
            return;
        }
        if (view == "machine-support")
        {
            if (_currentSceneId != "lab-10-02-chicken-label-print")
                throw new InvalidOperationException("Machine-support evidence view is restricted to Chicken scale.");
            var scale = _sceneCompositionRoot.GetNode<Node3D>("training_accessory_5");
            var meshes = scale.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                .Where(m => m.Name.ToString().StartsWith("WEIGH_", StringComparison.Ordinal)).ToArray();
            if (!meshes.Any(m => m.Name == "WEIGH_readout_frame_arm")
                || !meshes.Any(m => m.Name == "WEIGH_readout_frame_post"))
                throw new InvalidOperationException("Machine-support evidence view requires the actual repaired frame supports.");
            // Oblique from the room's open +Z side: retain the full scale,
            // side frame, connected arm/post and original head in one witness.
            FitIndustrialMountCapture(meshes, new Vector3(2.2f, 1.0f, 2.6f), view);
            return;
        }
        if (view == "ev-readouts")
        {
            if (_currentSceneId != "lab-9-12-ev-charging-manager")
                throw new InvalidOperationException("EV readout evidence view requires the exact EV scene.");
            var station = _sceneCompositionRoot.GetNode<Node3D>("ControlInstallation");
            var meters = new[] { "training_accessory_5", "ev_meter_b" }
                .Select(id => _sceneCompositionRoot.GetNode<Node3D>(id));
            FitIndustrialMountCapture(meters.SelectMany(m => m.FindChildren("*", "MeshInstance3D", true, false))
                .OfType<MeshInstance3D>().Concat(station.FindChildren("InstallationIdentification_*", "MeshInstance3D", true, false)
                    .OfType<MeshInstance3D>().Where(mesh => mesh.HasMeta("installationLegendKind")
                        && mesh.GetMeta("installationLegendKind").AsString() == "evPlcEnergyIdentity"))
                .ToArray(), station.GlobalBasis * Vector3.Back, view);
            return;
        }
        if (view == "mount-detail")
        {
            var station = _sceneCompositionRoot.GetNode<Node3D>("ControlInstallation");
            var heads = station.GetMeta("installationEquipmentIds").AsGodotArray()
                .Select(id => _sceneCompositionRoot.GetNode<Node3D>(id.AsString()));
            var meshes = station.FindChildren("*", "MeshInstance3D", true, false)
                .Concat(heads.SelectMany(h => h.FindChildren("*", "MeshInstance3D", true, false)))
                .OfType<MeshInstance3D>().ToArray();
            // Gallery foreground exhibits obstruct the straight frontal sightline.
            // Keep the same measured framing, but inspect its bench from above/right.
            var direction = _currentSceneId == "equipment-gallery"
                ? station.GlobalBasis * new Vector3(1.8f, 1.2f, 1.0f)
                : station.GlobalBasis * Vector3.Back + Vector3.Up * .08f;
            FitIndustrialMountCapture(meshes, direction, view);
            return;
        }
        if (view == "motor")
        {
            // A close diagnostic witness of original motor feet/shaft and the
            // new support. This does not change the normal teaching camera.
            var motor = _sceneCompositionRoot.GetChildren().OfType<Node3D>()
                .Where(n => n.HasMeta("motorTestMounted"))
                // Choose the open foreground end of the original array; a
                // close view of the first rear motor looks through its peers.
                .OrderByDescending(n => n.GlobalPosition.Z).ThenByDescending(n => n.GlobalPosition.X)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Motor evidence view requires a mounted motor.");
            var motorTarget = motor.GlobalPosition + new Vector3(0, .10f, 0);
            // Stay on the room's open +Z side, rather than outside its rear
            // wall. STRUCT's narrow X offset also avoids the adjoining CNC.
            var motorEye = motor.GlobalPosition + (_currentSceneId == "lab-10-05-motor-struct-data"
                ? new Vector3(.4f, 2.1f, 5.4f) : new Vector3(3.4f, 2.1f, 4.5f));
            _sceneCamera.GlobalPosition = motorEye;
            _sceneCamera.Fov = 35;
            _sceneCamera.LookAt(motorTarget, Vector3.Up);
            _cameraController?.CaptureCurrentView(motorTarget);
            GD.Print($"MOTOR_TEST_CAPTURE_VIEW scene={_currentSceneId} equipment={motor.Name} eye={motorEye} target={motorTarget} scope=diagnostic-camera-only");
            return;
        }
        if (view is not ("operator" or "side")) throw new InvalidOperationException("Unknown station evidence view.");
        var support = _sceneCompositionRoot.GetNodeOrNull<Node3D>("ControlInstallation")
            ?? throw new InvalidOperationException("Station evidence camera requires a mounted installation.");
        var front = support.GlobalBasis * Vector3.Back;
        var right = support.GlobalBasis * Vector3.Right;
        var target = support.GlobalPosition + new Vector3(0, 1.10f, 0);
        var position = support.GlobalPosition + front * 5.5f + new Vector3(0, 1.60f, 0);
        if (view == "side") position += right * 4.0f;
        _sceneCamera.GlobalPosition = position;
        _sceneCamera.Fov = 43;
        _sceneCamera.LookAt(target, Vector3.Up);
        _cameraController?.CaptureCurrentView(target);
        GD.Print($"CONTROL_INSTALLATION_CAPTURE_VIEW scene={_currentSceneId} view={view} eye={position} target={target} scope=diagnostic-camera-only");
    }

    private void FitIndustrialMountCapture(MeshInstance3D[] meshes, Vector3 viewDirection, string view, Vector3[]? extraWorldPoints = null)
    {
        var camera = _sceneCamera ?? throw new InvalidOperationException("Mount capture requires scene camera.");
        // Transform every actual visible mesh corner. Hidden old floor stands
        // cannot enlarge or falsify the repaired support evidence framing.
        var points = meshes.Where(m => m.Mesh is not null && m.IsVisibleInTree()).SelectMany(mesh =>
        {
            var bounds = mesh.GetAabb();
            return Enumerable.Range(0, 8).Select(mask => mesh.GlobalTransform * (bounds.Position + new Vector3(
                (mask & 1) == 0 ? 0 : bounds.Size.X,
                (mask & 2) == 0 ? 0 : bounds.Size.Y,
                (mask & 4) == 0 ? 0 : bounds.Size.Z)));
        }).Concat(extraWorldPoints ?? System.Array.Empty<Vector3>()).ToArray();
        if (points.Length == 0) throw new InvalidOperationException("Mount capture has no actual visible geometry.");
        var world = new Aabb(points[0], Vector3.Zero);
        foreach (var point in points.Skip(1)) world = world.Expand(point);
        var target = world.GetCenter();
        var direction = viewDirection.Normalized();
        var right = direction.Cross(Vector3.Up).Normalized();
        var up = right.Cross(direction).Normalized();
        var halfWidth = points.Max(p => MathF.Abs((p - target).Dot(right)));
        var halfHeight = points.Max(p => MathF.Abs((p - target).Dot(up)));
        var halfDepth = points.Max(p => MathF.Abs((p - target).Dot(direction)));
        camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
        camera.Fov = 35;
        var viewport = camera.GetViewport().GetVisibleRect();
        var aperture = ChallengeSceneAperture(_simulatorShell?.SceneViewportRect() ?? viewport);
        if (aperture.Size.X <= 0 || aperture.Size.Y <= 0) aperture = viewport;
        var tangent = MathF.Tan(Mathf.DegToRad(camera.Fov / 2));
        var distance = MathF.Max(halfHeight * viewport.Size.Y / (tangent * aperture.Size.Y),
            halfWidth * viewport.Size.Y / (tangent * aperture.Size.X));
        distance = MathF.Max(1.2f, (distance + halfDepth) * 1.16f);
        camera.GlobalPosition = target + direction * distance;
        camera.LookAt(target, Vector3.Up);
        _cameraController?.CaptureCurrentView(target);
        GD.Print($"INDUSTRIAL_MOUNT_CAPTURE_VIEW scene={_currentSceneId} view={view} measuredPoints={points.Length} eye={camera.GlobalPosition} target={target} aperture={aperture} scope=diagnostic-camera-only");
    }
}
