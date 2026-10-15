using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Catalog;
using RungProof.Next.Scenes;
using GodotFileAccess = Godot.FileAccess;

namespace RungProof.Next.Workspace;

public static class WorkspaceRepository
{
    public const string LastWorkspacePath = "user://workspaces/last-workspace.json";
    public const string VerificationWorkspacePath = "user://workspaces/verification-workspace.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static string Save(
        WorkspaceDocument document,
        string resourcePath = LastWorkspacePath
    )
    {
        ValidateShape(document);
        WorkspaceFiniteBeltPolicy.Validate(document);
        WorkspaceFiniteRollerPolicy.Validate(document);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var path = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(resourcePath));
        var directory = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Workspace path has no parent directory.");
        System.IO.Directory.CreateDirectory(directory);
        // Finish writing in the destination directory before replacing the
        // saved workspace. A failed write cannot truncate the previous file.
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            System.IO.File.WriteAllText(temporary, json);
            System.IO.File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary);
        }
        return path;
    }

    public static WorkspaceDocument Load(
        AssetCatalogDocument assets,
        SceneCatalogDocument scenes,
        string resourcePath = LastWorkspacePath
    )
    {
        // Repository callers that need only the layout can still read a new
        // integrated file. Main separately restores its actual ladder draft.
        var integrated=WorkspaceProjectBundleRepository.LoadIfBundle(resourcePath,assets,scenes);
        if(integrated is not null)return integrated.Workspace;
        var json = System.IO.Path.IsPathRooted(resourcePath)
            ? System.IO.File.ReadAllText(resourcePath)
            : ReadGodotFile(resourcePath);
        var document = JsonSerializer.Deserialize<WorkspaceDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Workspace JSON produced no document.");
        return ValidateDocument(document,assets,scenes);
    }

    public static WorkspaceDocument ValidateDocument(WorkspaceDocument document,
        AssetCatalogDocument assets,SceneCatalogDocument scenes)
    {
        ValidateShape(document);
        if (!scenes.Scenes.Any(scene => scene.Id == document.SourceSceneId))
            throw new InvalidOperationException($"Workspace scene '{document.SourceSceneId}' is unavailable.");
        if(document.TemplateOverride is not null)
            WorkspaceTemplatePolicy.Validate(SceneCatalogLoader.LoadScene(scenes.Scenes.First(s=>s.Id==document.SourceSceneId)),document.TemplateOverride);
        var assetIds = assets.Assets.Select(asset => asset.Id).ToHashSet(StringComparer.Ordinal);
        var missing = document.Placements.FirstOrDefault(item => !assetIds.Contains(item.AssetId));
        if (missing is not null)
            throw new InvalidOperationException($"Workspace asset '{missing.AssetId}' is unavailable.");
        var duplicate = document.Placements.GroupBy(item => item.InstanceId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate workspace instance ID '{duplicate.Key}'.");
        ValidateConnections(document, assets, scenes);
        return document;
    }

    private static string ReadGodotFile(string resourcePath)
    {
        using var file = GodotFileAccess.Open(resourcePath, GodotFileAccess.ModeFlags.Read)
            ?? throw new InvalidOperationException("No saved workspace is available.");
        return file.GetAsText();
    }

    private static void ValidateShape(WorkspaceDocument document)
    {
        if (document.Version is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9)) throw new InvalidOperationException($"Unsupported workspace version {document.Version}.");
        WorkspaceDriveConnectionPolicy.ValidateShape(document);
        if(new[]{document.TemplateOverride is not null,document.FiniteBeltPreset is not null,document.FiniteRollerPreset is not null,document.DriveConnection is not null}.Count(v=>v)>1)
            throw new InvalidOperationException("Workspace preset relations are mutually exclusive.");
        if(document.Version==8&&document.FiniteRollerPreset is null)throw new InvalidOperationException("Workspace version 8 requires its finite roller relation.");
        WorkspaceFiniteRollerPolicy.Validate(document);
        if(document.TemplateOverride is not null)
        {
            if(document.Version!=6)throw new InvalidOperationException("Template overrides require workspace version 6.");
            WorkspaceTemplatePolicy.ValidateShape(document.SourceSceneId,document.TemplateOverride);
        }
        if (string.IsNullOrWhiteSpace(document.SourceSceneId)) throw new InvalidOperationException("Workspace source scene is blank.");
        foreach (var placement in document.Placements)
        {
            if (string.IsNullOrWhiteSpace(placement.InstanceId) || string.IsNullOrWhiteSpace(placement.AssetId))
                throw new InvalidOperationException("Workspace placement has a blank instance or asset ID.");
            ValidateVector(placement.Position, "position", placement.InstanceId);
            ValidateVector(placement.Rotation, "rotation", placement.InstanceId);
            ValidateVector(placement.Scale, "scale", placement.InstanceId);
            if (placement.Scale.Any(value => value <= 0))
                throw new InvalidOperationException($"Workspace placement '{placement.InstanceId}' has a non-positive scale.");
        }
        ValidateGroups(document);
    }

    private static void ValidateGroups(WorkspaceDocument document)
    {
        var groups = document.Groups ?? [];
        EnsureUniqueIds(groups.Select(group => group.Id), "group");
        var placements = document.Placements.Select(item => item.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var byId = groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
                throw new InvalidOperationException($"Workspace group '{group.Id}' has a blank name.");
            if (group.MemberInstanceIds.Count < 2)
                throw new InvalidOperationException($"Workspace group '{group.Id}' must contain at least two placements.");
            if (group.Pivot is not null)
                ValidateVector(group.Pivot, "pivot", group.Id);
            if (group.ParentGroupId is not null && (!byId.ContainsKey(group.ParentGroupId) || group.ParentGroupId == group.Id))
                throw new InvalidOperationException($"Workspace group '{group.Id}' has an invalid parent.");
            var duplicateMember = group.MemberInstanceIds.GroupBy(id => id, StringComparer.Ordinal)
                .FirstOrDefault(items => items.Count() > 1);
            if (duplicateMember is not null)
                throw new InvalidOperationException($"Workspace group '{group.Id}' repeats member '{duplicateMember.Key}'.");
            foreach (var member in group.MemberInstanceIds)
            {
                if (!placements.Contains(member))
                    throw new InvalidOperationException($"Workspace group '{group.Id}' references missing placement '{member}'.");
            }
        }
        foreach (var group in groups.Where(group => group.ParentGroupId is not null))
        {
            var parent = byId[group.ParentGroupId!];
            if (!group.MemberInstanceIds.All(parent.MemberInstanceIds.Contains))
                throw new InvalidOperationException($"Workspace group '{group.Id}' is not contained by its parent.");
            var cursor = parent;
            // Malformed input may lead into a cycle that does not include the
            // starting group. Track every ancestor, not just the original ID.
            var visited = new HashSet<string>(StringComparer.Ordinal) { group.Id };
            while (cursor.ParentGroupId is not null)
            {
                if (!visited.Add(cursor.Id))
                    throw new InvalidOperationException($"Workspace group '{group.Id}' creates a parent cycle.");
                cursor = byId[cursor.ParentGroupId];
            }
        }
        foreach (var siblings in groups.GroupBy(group => group.ParentGroupId, StringComparer.Ordinal))
        {
            var claimedMembers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sibling in siblings)
            {
                var overlap = sibling.MemberInstanceIds.FirstOrDefault(claimedMembers.Contains);
                if (overlap is not null)
                    throw new InvalidOperationException(
                        $"Workspace sibling group '{sibling.Id}' overlaps member '{overlap}'.");
                claimedMembers.UnionWith(sibling.MemberInstanceIds);
            }
        }
    }

    private static void ValidateConnections(
        WorkspaceDocument document,
        AssetCatalogDocument assets,
        SceneCatalogDocument scenes
    )
    {
        var placements = document.Placements.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        var assetById = assets.Assets.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var connectorLinks = document.ConnectorLinks ?? [];
        var signalLinks = document.SignalLinks ?? [];
        EnsureUniqueIds(connectorLinks.Select(item => item.Id), "connector link");
        EnsureUniqueIds(signalLinks.Select(item => item.Id), "signal link");
        foreach (var link in connectorLinks)
        {
            var from = ResolveConnector(link.FromInstanceId, link.FromConnectorId, placements, assetById);
            var to = ResolveConnector(link.ToInstanceId, link.ToConnectorId, placements, assetById);
            if (link.FromInstanceId == link.ToInstanceId && link.FromConnectorId == link.ToConnectorId)
                throw new InvalidOperationException($"Connector link '{link.Id}' connects an endpoint to itself.");
            var compatible = from.CompatibleKinds.Contains(to.Kind, StringComparer.Ordinal)
                || to.CompatibleKinds.Contains(from.Kind, StringComparer.Ordinal);
            if (!compatible)
                throw new InvalidOperationException($"Connector link '{link.Id}' is incompatible: {from.Kind} to {to.Kind}.");
        }

        var sceneEntry = scenes.Scenes.First(item => item.Id == document.SourceSceneId);
        var scene = SceneCatalogLoader.LoadScene(sceneEntry);
        if(document.FiniteBeltPreset is not null)scene=WorkspaceFiniteBeltPolicy.ReplaceSource(scene,document);
        if(document.FiniteRollerPreset is not null)scene=WorkspaceFiniteRollerPolicy.ReplaceSource(scene,document);
        if(document.DriveConnection is not null)scene=WorkspaceDriveConnectionPolicy.AugmentSource(scene,document,assets);
        var pointTypes = ScenePointTypes(scene.Simulation);
        foreach (var link in signalLinks)
        {
            if (!placements.TryGetValue(link.InstanceId, out var placement))
                throw new InvalidOperationException($"Signal link '{link.Id}' references missing placement '{link.InstanceId}'.");
            var asset = assetById[placement.AssetId];
            var signal = asset.Signals.FirstOrDefault(item => item.Id == link.SignalId)
                ?? throw new InvalidOperationException($"Signal link '{link.Id}' references missing signal '{link.SignalId}'.");
            if (!pointTypes.TryGetValue(link.PointName, out var pointType))
                throw new InvalidOperationException($"Signal link '{link.Id}' references missing point '{link.PointName}'.");
            if (!TypesCompatible(signal.DataType, pointType))
                throw new InvalidOperationException($"Signal link '{link.Id}' type mismatch: {signal.DataType} to {pointType}.");
        }
    }

    private static AssetConnector ResolveConnector(
        string instanceId,
        string connectorId,
        System.Collections.Generic.IReadOnlyDictionary<string, WorkspacePlacement> placements,
        System.Collections.Generic.IReadOnlyDictionary<string, AssetDefinition> assets
    )
    {
        if (!placements.TryGetValue(instanceId, out var placement))
            throw new InvalidOperationException($"Connector references missing placement '{instanceId}'.");
        return assets[placement.AssetId].Connectors.FirstOrDefault(item => item.Id == connectorId)
            ?? throw new InvalidOperationException($"Connector '{connectorId}' is unavailable on '{instanceId}'.");
    }

    private static System.Collections.Generic.Dictionary<string, string> ScenePointTypes(JsonElement simulation)
    {
        var result = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
        if (simulation.ValueKind != JsonValueKind.Object
            || !simulation.TryGetProperty("points", out var points)
            || points.ValueKind != JsonValueKind.Array) return result;
        foreach (var point in points.EnumerateArray())
        {
            if (point.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && point.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String)
                result[name.GetString() ?? string.Empty] = type.GetString() ?? string.Empty;
        }
        return result;
    }

    private static bool TypesCompatible(string signalType, string pointType) =>
        (signalType.ToLowerInvariant(), pointType.ToUpperInvariant()) switch
        {
            ("bool", "BOOL") => true,
            ("float32", "REAL") => true,
            ("int32", "DINT") => true,
            ("int32", "INT") => true,
            _ => false,
        };

    private static void EnsureUniqueIds(System.Collections.Generic.IEnumerable<string> ids, string noun)
    {
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Blank or duplicate {noun} ID '{duplicate.Key}'.");
    }

    private static void ValidateVector(double[] values, string name, string instanceId)
    {
        if (values.Length != 3 || values.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
            throw new InvalidOperationException($"Workspace placement '{instanceId}' has an invalid {name} vector.");
    }
}
