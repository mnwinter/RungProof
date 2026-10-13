using System.Collections.Generic;
using System.Text.Json;

namespace RungProof.Next.Scenes;

public sealed record SceneCatalogDocument(
    int Version,
    string CatalogId,
    string SourceRoot,
    int SceneCount,
    int EquipmentInstanceCount,
    IReadOnlyDictionary<string, int> EquipmentTypeCounts,
    IReadOnlyList<SceneCatalogEntry> Scenes
);

public sealed record SceneCatalogEntry(
    string Id,
    string Name,
    string Path,
    string SourceFile,
    int EquipmentCount,
    IReadOnlyList<string> EquipmentTypes,
    IReadOnlyList<string> UnresolvedAssetTypes,
    string VisualStatus
);

public sealed record SceneDefinition(
    int Version,
    string Id,
    string Name,
    string Description,
    SceneCamera Camera,
    IReadOnlyList<SceneEquipment> Equipment,
    JsonElement Simulation,
    JsonElement Training,
    JsonElement Verification,
    SceneMigration Migration,
    string? PlcTestProfile = null,
    RungProof.Next.Workspace.WorkspaceTemplateOverride? TemplateOverride = null
);

public sealed record SceneCamera(double[] Position, double[] Target, double Fov);

public sealed record SceneEquipment(
    string Id,
    string Type,
    string Label,
    double[] Position,
    double[]? Rotation,
    double[]? Scale,
    JsonElement Config
);

public sealed record SceneMigration(
    string SourceFile,
    string SourceFormat,
    IReadOnlyDictionary<string, string> AssetMappings,
    IReadOnlyList<string> ScenePropTypes,
    IReadOnlyList<string> UnresolvedAssetTypes,
    string VisualStatus,
    bool ProductionApproved,
    string Reason
);
