using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    // Opt-in development verifier: visits actual composed scenes, checks that
    // decorative presentation stays outside runtime/picking and restores state.
    private async void VerifyCatalogEnvironments()
    {
        try
        {
            RemoveDemoEnvironment();
            var world = GetNode<WorldEnvironment>("WorldEnvironment");
            var baselineEnvironment = world.Environment;
            var key = GetNode<DirectionalLight3D>("KeyLight");
            var fill = GetNode<DirectionalLight3D>("FillLight");
            var baselineKey = (key.LightColor, key.LightEnergy);
            var baselineFill = (fill.LightColor, fill.LightEnergy);
            var floor = GetNode<MeshInstance3D>("Floor");
            var baselineFloor = floor.Visible;
            var baselineFloorMaterial = floor.MaterialOverride;
            var catalog = SceneCatalogLoader.LoadCatalog("res://scenes/catalog/original-scenes.catalog.json");
            var ids = catalog.Scenes.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
            if (!ids.SetEquals(SceneEnvironmentProfiles.Profiles.Keys))
                throw new InvalidOperationException("Environment ledger differs from catalog.");
            if (!ids.SetEquals(SceneEnvironmentArtDirections.Directions.Keys))
                throw new InvalidOperationException("Art-direction ledger differs from catalog.");
            var count = 0;
            var geometryEvidence = new List<object>();
            foreach (var entry in catalog.Scenes)
            {
                AddMigratedScene(entry.Id, _candidateCatalog!, _mainCamera!, false, false);
                var process = _sceneCompositionRoot!;
                var decor = _demoEnvironment ?? throw new InvalidOperationException($"No environment: {entry.Id}");
                var architecture = decor.GetNodeOrNull<Node3D>("LocationArchitecture")
                    ?? throw new InvalidOperationException($"No location architecture: {entry.Id}");
                if (!architecture.HasMeta("purposeLayoutImplemented")
                    || !architecture.GetMeta("purposeLayoutImplemented").AsBool())
                    throw new InvalidOperationException($"Missing exact purpose layout: {entry.Id}");
                if (decor.GetParent() != this || process.GetParent() != this || !decor.Rotation.IsEqualApprox(Vector3.Zero))
                    throw new InvalidOperationException($"Environment hierarchy/axes: {entry.Id}");
                var nodes = decor.FindChildren("*", "", true, false);
                if (nodes.Any(n => n is CollisionObject3D or CollisionShape3D or CollisionPolygon3D
                    or Control or EquipmentMotionController or ConveyorController))
                    throw new InvalidOperationException($"Decoration contains behavior/physics: {entry.Id}");
                if (process.FindChildren("DemoEnvironmentPresentation", "", true, false).Count != 0)
                    throw new InvalidOperationException($"Decoration entered process: {entry.Id}");
                if (decor.AmbientMotionEnabled)
                    throw new InvalidOperationException($"Unexpected decorative motion: {entry.Id}");
                foreach (var mesh in nodes.OfType<MeshInstance3D>())
                {
                    var decorativeBounds = ReviewBounds(mesh);
                    // Slabs may sit below equipment. Every upright furnishing
                    // must stay out of the conservative presentation envelope.
                    if (decorativeBounds.End.Y > 0.05f && decorativeBounds.Intersects(decor.ProtectedProcessBounds))
                        throw new InvalidOperationException($"Decoration enters process envelope: {entry.Id}/{mesh.Name}");
                }
                // Evaluate actual generated meshes against the declared wall
                // aperture, independently of wall-subtraction implementation.
                // This catches a pane, post or foundation spanning a doorway.
                var portalCount = 0;
                foreach (var portal in nodes.OfType<Node3D>().Where(n => n.HasMeta("localOpeningBounds")))
                {
                    var opening = portal.GetMeta("localOpeningBounds").AsAabb();
                    if (opening.Size.X < 1.0f)
                        throw new InvalidOperationException($"Implausibly narrow architectural portal: {entry.Id}/{portal.Name}");
                    var interior = (portal.GlobalTransform * opening).Grow(-.002f);
                    foreach (var mesh in nodes.OfType<MeshInstance3D>())
                        if (ReviewBounds(mesh).Intersects(interior))
                            throw new InvalidOperationException($"Architecture blocks opening: {entry.Id}/{portal.Name}/{mesh.Name}");
                    portalCount++;
                }
                var checkedRoutes = 0;
                foreach (var cabinet in nodes.OfType<Node3D>().Where(n => n.HasMeta("retainedLocalEnvelope")))
                {
                    var retained = (cabinet.GlobalTransform * cabinet.GetMeta("retainedLocalEnvelope").AsAabb()).Grow(.0005f);
                    foreach (var detail in cabinet.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                    {
                        var actual = ReviewBounds(detail);
                        if (!retained.Encloses(actual))
                            throw new InvalidOperationException($"Passive cabinet detail enlarges retained footprint: {entry.Id}/{detail.Name}");
                    }
                    foreach (var label in cabinet.FindChildren("*", "Label3D", true, false).OfType<Label3D>())
                    {
                        var font = label.Font ?? ThemeDB.FallbackFont;
                        var lines = label.Text.Split('\n');
                        var width = lines.Max(line => font.GetStringSize(line, HorizontalAlignment.Left, -1, label.FontSize).X) * label.PixelSize;
                        var height = font.GetStringSize("Mg", HorizontalAlignment.Left, -1, label.FontSize).Y * lines.Length * label.PixelSize;
                        var ink = label.GlobalTransform * new Aabb(new Vector3(-width / 2, -height / 2, -.0005f), new Vector3(width, height, .001f));
                        if (!retained.Encloses(ink))
                            throw new InvalidOperationException($"Passive cabinet caption exceeds retained envelope: {entry.Id}/{label.Name}");
                    }
                }
                void CheckStaticRoute(Aabb routeBounds, string routeName)
                {
                    var obstacles = nodes.OfType<MeshInstance3D>().Concat(ReviewMeshes(process));
                    foreach (var mesh in obstacles.Where(m => m.Mesh is not null && m.IsVisibleInTree()))
                    {
                        var obstacleBounds = ReviewBounds(mesh);
                        if (obstacleBounds.End.Y > .05f && obstacleBounds.Intersects(routeBounds))
                            throw new InvalidOperationException($"Static approach route blocked: {entry.Id}/{routeName}/{mesh.Name}");
                    }
                    checkedRoutes++;
                }
                foreach (var route in nodes.OfType<Node3D>().Where(n => n.HasMeta("localRouteBounds")))
                    CheckStaticRoute(route.GetParent<Node3D>().GlobalTransform * route.GetMeta("localRouteBounds").AsAabb(), route.Name);
                foreach (var route in nodes.OfType<Node3D>().Where(n => n.HasMeta("localApproachPolyline")))
                {
                    var points = route.GetMeta("localApproachPolyline").AsGodotArray<Vector3>();
                    var sweep = route.GetMeta("proposedSweepWidth").AsSingle();
                    if (points.Count < 2 || !float.IsFinite(sweep) || sweep < 1.0f)
                        throw new InvalidOperationException($"Invalid approach route: {entry.Id}/{route.Name}");
                    for (var segment = 1; segment < points.Count; segment++)
                    {
                        var start = points[segment - 1];
                        var end = points[segment];
                        if (MathF.Abs(start.X - end.X) > .001f && MathF.Abs(start.Z - end.Z) > .001f)
                            throw new InvalidOperationException($"Approach segment must use explicit axis-aligned sweep: {entry.Id}/{route.Name}");
                        var localSweep = new Aabb(new Vector3(MathF.Min(start.X, end.X) - sweep / 2, 0, MathF.Min(start.Z, end.Z) - sweep / 2),
                            new Vector3(MathF.Abs(start.X - end.X) + sweep, 2.0f, MathF.Abs(start.Z - end.Z) + sweep));
                        CheckStaticRoute(route.GetParent<Node3D>().GlobalTransform * localSweep, route.Name + "/" + segment);
                    }
                }
                geometryEvidence.Add(new
                {
                    sceneId = entry.Id,
                    layoutVariant = architecture.GetMeta("layoutVariant").AsString(),
                    location = architecture.GetMeta("locationPurpose").AsString(),
                    checkedOpenings = portalCount,
                    checkedStaticRoutes = checkedRoutes,
                    meshes = nodes.OfType<MeshInstance3D>().Select(mesh =>
                    {
                        var actual = ReviewBounds(mesh);
                        return new
                        {
                            path = decor.GetPathTo(mesh).ToString(),
                            position = new[] { actual.Position.X, actual.Position.Y, actual.Position.Z },
                            size = new[] { actual.Size.X, actual.Size.Y, actual.Size.Z },
                        };
                    }).ToArray(),
                });
                var transforms = process.FindChildren("*", "Node3D", true, false).OfType<Node3D>()
                    .Select(n => (Node: n, Transform: n.Transform)).ToArray();
                var cameraTransform = _mainCamera!.Transform;
                RemoveDemoEnvironment();
                if (transforms.Any(p => !p.Node.Transform.IsEqualApprox(p.Transform))
                    || !_mainCamera.Transform.IsEqualApprox(cameraTransform))
                    throw new InvalidOperationException($"Environment removal altered process/camera: {entry.Id}");
                if (world.Environment != baselineEnvironment || key.LightColor != baselineKey.Item1
                    || !Mathf.IsEqualApprox(key.LightEnergy, baselineKey.Item2)
                    || fill.LightColor != baselineFill.Item1 || !Mathf.IsEqualApprox(fill.LightEnergy, baselineFill.Item2)
                    || floor.Visible != baselineFloor || floor.MaterialOverride != baselineFloorMaterial)
                    throw new InvalidOperationException($"Environment state leaked: {entry.Id}");
                GD.Print($"CATALOG_ENVIRONMENT_CASE PASS {entry.Id} decorativeNodes={nodes.Count} checkedOpenings={portalCount}");
                count++;
                // Retire queued nodes between scenes instead of accumulating 77
                // installations and their native render resources until exit.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            var evidenceArgument = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--environment-evidence=", StringComparison.Ordinal));
            if (evidenceArgument is not null)
            {
                var evidencePath = evidenceArgument["--environment-evidence=".Length..];
                using var evidenceFile = FileAccess.Open(evidencePath, FileAccess.ModeFlags.Write)
                    ?? throw new InvalidOperationException($"Cannot write environment evidence: {evidencePath}");
                evidenceFile.StoreString(System.Text.Json.JsonSerializer.Serialize(new
                {
                    scope = "Actual generated decorative world AABBs and opening checks; not visual or physical clearance acceptance",
                    scenes = geometryEvidence,
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            GD.Print($"CATALOG_ENVIRONMENTS_VERIFY PASS scenes={count} ledger=exact hierarchy=sibling axes=world restore=pass runtime=unchanged");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError($"CATALOG_ENVIRONMENTS_VERIFY FAIL {error.Message}");
            GetTree().Quit(1);
        }
    }
}
