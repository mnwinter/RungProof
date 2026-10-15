using System.Collections.Generic;

namespace RungProof.Next.Catalog;

public sealed record AssetCatalogDocument(
    int Version,
    string CatalogId,
    IReadOnlyList<AssetDefinition> Assets
);

public sealed record AssetDefinition(
    string Id,
    string DisplayName,
    string Category,
    IReadOnlyList<string> Tags,
    AssetModel Model,
    AssetBounds Bounds,
    IReadOnlyList<AssetConnector> Connectors,
    IReadOnlyList<KinematicAxis> Kinematics,
    IReadOnlyList<SignalDefinition> Signals,
    AssetQuality Quality
);

public sealed record AssetModel(
    string SourceBlend,
    string DeliveryGltf,
    IReadOnlyList<string> LodFiles,
    string CollisionFile,
    string ThumbnailFile
);

public sealed record AssetBounds(double WidthM, double HeightM, double DepthM);

public sealed record AssetConnector(
    string Id,
    string Kind,
    double[] PositionM,
    double[] RotationDeg,
    IReadOnlyList<string> CompatibleKinds
);

public sealed record KinematicAxis(
    string Id,
    string Kind,
    string NodePath,
    double Minimum,
    double Maximum,
    string Unit,
    double MaximumRate
);

public sealed record SignalDefinition(
    string Id,
    string DataType,
    string Direction,
    string? Unit,
    string? KinematicAxis,
    string? Description
);

public sealed record AssetQuality(
    string Status,
    string? BlindReviewId,
    double? RecognitionConfidence,
    bool TopologyReviewed,
    bool MaterialReviewed,
    bool ScaleReviewed,
    bool AnimationReviewed
);
