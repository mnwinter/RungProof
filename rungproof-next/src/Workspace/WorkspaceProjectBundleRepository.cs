using System;
using System.IO;
using System.Text.Json;
using Godot;
using RungProof.Next.Catalog;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.Workspace;

public sealed record WorkspaceProjectBundle(int Version,string Kind,
    WorkspaceDocument Workspace,string LadderProjectJson);

public static class WorkspaceProjectBundleRepository
{
    public const string Kind="rungproof-integrated-project";
    private static readonly JsonSerializerOptions Options=new()
    {PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true,WriteIndented=true};

    // Readable invalid ladder work is preserved. Compilation is deliberately
    // not part of saving/opening an engineering project or a solution loader.
    public static WorkspaceProjectBundle Validate(WorkspaceProjectBundle bundle,
        AssetCatalogDocument assets,SceneCatalogDocument scenes)
    {
        if(bundle.Version!=1||bundle.Kind!=Kind||bundle.Workspace is null||string.IsNullOrWhiteSpace(bundle.LadderProjectJson))
            throw new InvalidOperationException("Unsupported or incomplete integrated project.");
        WorkspaceRepository.ValidateDocument(bundle.Workspace,assets,scenes);
        var ladder=LadderEditorProjectJson.Load(bundle.LadderProjectJson);
        if(!ladder.IsReadable||ladder.Document is null)
            throw new InvalidOperationException("Integrated ladder draft is malformed: "+string.Join("; ",ladder.Issues));
        if(ladder.Document.SourceSceneId!=bundle.Workspace.SourceSceneId)
            throw new InvalidOperationException("Integrated workspace and ladder source scene identities differ.");
        return bundle;
    }
    public static WorkspaceProjectBundle? LoadIfBundle(string path,AssetCatalogDocument assets,SceneCatalogDocument scenes)
    {
        var json=File.ReadAllText(ProjectSettings.GlobalizePath(path));
        using var parsed=JsonDocument.Parse(json);
        if(!parsed.RootElement.TryGetProperty("kind",out var kind)||kind.GetString()!=Kind)return null;
        var bundle=JsonSerializer.Deserialize<WorkspaceProjectBundle>(json,Options)
            ?? throw new InvalidOperationException("Integrated project is empty.");
        return Validate(bundle,assets,scenes);
    }
    public static string Save(WorkspaceProjectBundle bundle,string path,
        AssetCatalogDocument assets,SceneCatalogDocument scenes)
    {
        Validate(bundle,assets,scenes);
        var absolute=Path.GetFullPath(ProjectSettings.GlobalizePath(path));
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        var preserveOriginal=false;
        if(File.Exists(absolute))
        {
            var existing=LoadIfBundle(absolute,assets,scenes)
                ?? throw new InvalidOperationException("Existing file is not a supported integrated project; choose a new Save As path.");
            preserveOriginal=LadderEditorProjectJson.Load(existing.LadderProjectJson).SourceSchemaVersion
                < LadderEditorProjectJson.SchemaVersion;
        }
        LadderProjectFileStore.SaveValidated(absolute,JsonSerializer.Serialize(bundle,Options),content=>
        {
            var reread=JsonSerializer.Deserialize<WorkspaceProjectBundle>(content,Options)
                ?? throw new InvalidOperationException("Integrated project is empty.");
            Validate(reread,assets,scenes);
        },preserveOriginal);
        return absolute;
    }
}
