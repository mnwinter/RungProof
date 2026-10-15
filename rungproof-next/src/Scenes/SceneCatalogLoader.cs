using System;
using System.Linq;
using System.Text.Json;
using Godot;
using GodotFileAccess = Godot.FileAccess;

namespace RungProof.Next.Scenes;

public static class SceneCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static SceneCatalogDocument LoadCatalog(string resourcePath)
    {
        var document = Load<SceneCatalogDocument>(resourcePath);
        if (document.Version != 1 || document.SceneCount != document.Scenes.Count)
        {
            throw new InvalidOperationException("Migrated scene catalog count/version is invalid.");
        }
        var duplicate = document.Scenes.GroupBy(scene => scene.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate migrated scene ID: {duplicate.Key}");
        }
        return document;
    }

    public static SceneDefinition LoadScene(SceneCatalogEntry entry)
    {
        var scene = Load<SceneDefinition>(entry.Path);
        if (!string.Equals(scene.Id, entry.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Scene catalog ID mismatch for {entry.Path}.");
        }
        return scene;
    }

    private static T Load<T>(string resourcePath)
    {
        using var file = GodotFileAccess.Open(resourcePath, GodotFileAccess.ModeFlags.Read);
        if (file is null)
        {
            throw new InvalidOperationException($"Scene resource is unavailable: {resourcePath}");
        }
        return JsonSerializer.Deserialize<T>(file.GetAsText(), JsonOptions)
            ?? throw new InvalidOperationException($"Scene JSON produced no document: {resourcePath}");
    }
}
