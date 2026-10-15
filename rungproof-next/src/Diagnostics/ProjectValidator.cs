using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Catalog;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.Diagnostics;

public static class ProjectValidator
{
    private static readonly HashSet<string> SignalTypes = new(StringComparer.Ordinal)
    {
        "bool", "float32", "int32",
    };

    private static readonly HashSet<string> SignalDirections = new(StringComparer.Ordinal)
    {
        "input", "output",
    };

    private static readonly HashSet<string> BindingModes = new(StringComparer.Ordinal)
    {
        "running", "speedPercent", "estopPermissive", "switch", "selector", "numericDisplay", "textDisplay", "indicator", "indicatorChannel", "photoeye", "position", "levelSensor",
    };

    public static IReadOnlyList<DiagnosticIssue> Validate(
        AssetCatalogDocument assets,
        SceneCatalogDocument catalog,
        IReadOnlyList<SceneDefinition> scenes
    )
    {
        var issues = new List<DiagnosticIssue>();
        ValidateAssets(assets, issues);
        ValidateScenes(catalog, scenes, assets, issues);
        if (!issues.Any(issue => issue.Severity == DiagnosticSeverity.Error))
        {
            issues.Add(new DiagnosticIssue(
                "SYS-READY",
                DiagnosticSeverity.Information,
                "Project",
                $"Catalog data checks passed: {assets.Assets.Count} assets, {scenes.Count} scenes.",
                "Scene appearance, motion, control logic and PLC operation require separate verification."
            ));
        }
        return issues;
    }

    private static void ValidateAssets(
        AssetCatalogDocument document,
        ICollection<DiagnosticIssue> issues
    )
    {
        foreach (var asset in document.Assets)
        {
            var scope = $"Asset: {asset.DisplayName}";
            if (string.IsNullOrWhiteSpace(asset.DisplayName) || string.IsNullOrWhiteSpace(asset.Category))
            {
                AddError(issues, "AST-METADATA", scope,
                    "Display name or category is missing.",
                    "Complete the catalog metadata before approving this asset.");
            }
            if (!ResourceLoader.Exists(asset.Model.DeliveryGltf))
            {
                AddError(issues, "AST-MODEL-MISSING", scope,
                    $"Delivery model is unavailable: {asset.Model.DeliveryGltf}",
                    "Rebuild or restore the referenced delivery GLB.");
            }
            if (asset.Bounds.WidthM <= 0 || asset.Bounds.HeightM <= 0 || asset.Bounds.DepthM <= 0)
            {
                AddError(issues, "AST-BOUNDS", scope,
                    "One or more authored bounds are not positive.",
                    "Recalculate and register physical asset bounds in metres.");
            }
            AddDuplicateErrors(asset.Connectors.Select(item => item.Id), issues,
                "AST-CONNECTOR-DUPLICATE", scope, "connector");
            AddDuplicateErrors(asset.Signals.Select(item => item.Id), issues,
                "AST-SIGNAL-DUPLICATE", scope, "signal");
            AddDuplicateErrors(asset.Kinematics.Select(item => item.Id), issues,
                "AST-AXIS-DUPLICATE", scope, "kinematic axis");

            var axisIds = asset.Kinematics.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var connector in asset.Connectors)
            {
                if (connector.PositionM.Length != 3 || connector.RotationDeg.Length != 3)
                {
                    AddError(issues, "AST-CONNECTOR-POSE", scope,
                        $"Connector '{connector.Id}' does not have a three-axis pose.",
                        "Author exactly three position and three rotation components.");
                }
                if (connector.CompatibleKinds.Count == 0)
                {
                    AddWarning(issues, "AST-CONNECTOR-COMPAT", scope,
                        $"Connector '{connector.Id}' has no compatible connection kinds.",
                        "Declare at least one compatible connection kind or remove the connector.");
                }
            }
            foreach (var signal in asset.Signals)
            {
                if (!SignalTypes.Contains(signal.DataType))
                {
                    AddError(issues, "AST-SIGNAL-TYPE", scope,
                        $"Signal '{signal.Id}' has unsupported type '{signal.DataType}'.",
                        "Use a supported typed signal or extend the runtime type system deliberately.");
                }
                if (!SignalDirections.Contains(signal.Direction))
                {
                    AddError(issues, "AST-SIGNAL-DIRECTION", scope,
                        $"Signal '{signal.Id}' has unsupported direction '{signal.Direction}'.",
                        "Use input or output and define direction from the asset perspective.");
                }
                if (signal.KinematicAxis is not null && !axisIds.Contains(signal.KinematicAxis))
                {
                    AddError(issues, "AST-SIGNAL-AXIS", scope,
                        $"Signal '{signal.Id}' references missing axis '{signal.KinematicAxis}'.",
                        "Correct the axis ID or add the missing kinematic axis.");
                }
            }
        }
    }

    private static void ValidateScenes(
        SceneCatalogDocument catalog,
        IReadOnlyList<SceneDefinition> scenes,
        AssetCatalogDocument assets,
        ICollection<DiagnosticIssue> issues
    )
    {
        if (catalog.SceneCount != scenes.Count)
        {
            AddError(issues, "SCN-COUNT", "Scene catalog",
                $"Catalog declares {catalog.SceneCount} scenes but {scenes.Count} loaded.",
                "Regenerate the scene catalog and resolve missing scene resources.");
        }
        var assetIds = assets.Assets.Select(asset => asset.Id).ToHashSet(StringComparer.Ordinal);
        bool AccessoryResolved(SceneEquipment item, SceneDefinition scene)
        {
            var configuredAsset = Text(item.Config, "catalogAssetId");
            return configuredAsset.Length > 0 ? assetIds.Contains(configuredAsset)
                : SceneComposer.IsProceduralTrainingAccessory(item)
                    || (Text(item.Config, "installation") == "palletTransferBridge"
                        && item.Id == "robot_pallet_transfer_bridge"
                        && scene.Equipment.Any(part => part.Id == "robot_pallet_outbound"
                            && Text(part.Config, "installation") == "palletOutbound")
                        && scene.Equipment.Any(part => part.Id == "robot_pallet_conveyor"));
        }
        foreach (var scene in scenes)
        {
            var scope = $"Scene: {scene.Name}";
            AddDuplicateErrors(scene.Equipment.Select(item => item.Id), issues,
                "SCN-EQUIPMENT-DUPLICATE", scope, "equipment ID");
            // Migration metadata predates the configured accessory catalog.
            // Resolve each configured asset against the current catalog rather
            // than reporting a stale type-level failure for mapped equipment.
            var unresolvedTypes = scene.Migration.UnresolvedAssetTypes.Where(type =>
                type != "trainingAccessory" || scene.Equipment.Where(item => item.Type == type)
                    .Any(item => !AccessoryResolved(item, scene)))
                .ToArray();
            if (unresolvedTypes.Length > 0)
            {
                AddError(issues, "SCN-ASSET-UNRESOLVED", scope,
                    $"Unresolved equipment types: {string.Join(", ", unresolvedTypes)}.",
                    "Map each source type to a catalog family before release.");
            }
            foreach (var equipment in scene.Equipment.Where(item => item.Type == "trainingAccessory"))
            {
                var assetId = Text(equipment.Config, "catalogAssetId");
                if (!AccessoryResolved(equipment, scene))
                    AddError(issues, "SCN-ACCESSORY-MISSING", scope,
                        $"Equipment '{equipment.Id}' references unavailable accessory '{assetId}'.",
                        "Configure an existing catalog asset ID for this equipment.");
            }
            foreach (var mapping in scene.Migration.AssetMappings)
            {
                if (!assetIds.Contains(mapping.Value))
                {
                    AddError(issues, "SCN-ASSET-MISSING", scope,
                        $"Source type '{mapping.Key}' maps to missing asset '{mapping.Value}'.",
                        "Register the asset or correct the migration mapping.");
                }
            }
            ValidateSimulation(scene, scope, issues);
        }
    }

    private static void ValidateSimulation(
        SceneDefinition scene,
        string scope,
        ICollection<DiagnosticIssue> issues
    )
    {
        if (scene.Simulation.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        var equipmentIds = scene.Equipment.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var points = new HashSet<string>(StringComparer.Ordinal);
        if (scene.Simulation.TryGetProperty("points", out var pointArray)
            && pointArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var point in pointArray.EnumerateArray())
            {
                var name = Text(point, "name");
                if (name.Length == 0 || !points.Add(name))
                {
                    AddError(issues, "SCN-POINT-DUPLICATE", scope,
                        $"Simulation point '{name}' is blank or duplicated.",
                        "Give every symbolic point a unique non-empty name.");
                }
                var type = Text(point, "type");
                try { SceneAggregateContract.ValidateTextPoint(point); }
                catch (ArgumentException exception)
                {
                    AddError(issues, "SCN-TEXT", scope, exception.Message,
                        "Provide a bounded STRING initial value or a valid ENUM domain and initial member.");
                }
                if (type is "STRUCT" or "ARRAY")
                {
                    try
                    {
                        var schema = point.TryGetProperty("aggregate", out var metadata)
                            ? PlcAggregates.ParseSchema(metadata) : null;
                        if (!point.TryGetProperty("initial", out var initial))
                            throw new ArgumentException("Aggregate requires an initial value.");
                        var root = new PlcVariable(name,
                            type == "STRUCT" ? PlcVariableType.Struct : PlcVariableType.Array,
                            PlcVariableRole.Memory, initial, Aggregate: schema);
                        foreach (var leaf in PlcAggregates.Expand([root]))
                            if (!points.Add(leaf.Name))
                                throw new ArgumentException($"Aggregate leaf '{leaf.Name}' overlaps another point.");
                    }
                    catch (Exception exception) when (exception is ArgumentException or JsonException)
                    {
                        AddError(issues, "SCN-AGGREGATE", scope, exception.Message,
                            "Provide a valid fixed aggregate schema and matching initial values.");
                    }
                }
            }
        }
        if (!scene.Simulation.TryGetProperty("pointBindings", out var bindings)
            || bindings.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        foreach (var binding in bindings.EnumerateArray())
        {
            var point = Text(binding, "point");
            var equipment = Text(binding, "equipmentId");
            var mode = Text(binding, "mode");
            if (!points.Contains(point))
            {
                AddError(issues, "SCN-BINDING-POINT", scope,
                    $"Binding references unknown symbolic point '{point}'.",
                    "Create the point or correct the binding name.");
            }
            if (!equipmentIds.Contains(equipment))
            {
                AddError(issues, "SCN-BINDING-EQUIPMENT", scope,
                    $"Binding references unknown equipment '{equipment}'.",
                    "Correct the equipment ID or restore the missing instance.");
            }
            if (!BindingModes.Contains(mode))
            {
                AddError(issues, "SCN-BINDING-MODE", scope,
                    $"Binding uses unsupported mode '{mode}'.",
                    "Use a supported renderer binding mode or implement it explicitly.");
            }
        }
    }

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static void AddDuplicateErrors(
        IEnumerable<string> values,
        ICollection<DiagnosticIssue> issues,
        string code,
        string scope,
        string noun
    )
    {
        foreach (var duplicate in values.GroupBy(value => value, StringComparer.Ordinal)
                     .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
        {
            AddError(issues, code, scope,
                $"Blank or duplicate {noun}: '{duplicate.Key}'.",
                $"Give every {noun} a unique non-empty identifier.");
        }
    }

    private static void AddError(ICollection<DiagnosticIssue> issues, string code,
        string scope, string message, string resolution) =>
        issues.Add(new DiagnosticIssue(code, DiagnosticSeverity.Error, scope, message, resolution));

    private static void AddWarning(ICollection<DiagnosticIssue> issues, string code,
        string scope, string message, string resolution) =>
        issues.Add(new DiagnosticIssue(code, DiagnosticSeverity.Warning, scope, message, resolution));
}
