using System.Collections.Generic;

namespace RungProof.Next.Workspace;

public enum WorkspaceArrangeOperation
{
    AlignX,
    AlignY,
    AlignZ,
    DistributeX,
    DistributeY,
    DistributeZ,
}

public sealed record WorkspaceDocument(
    int Version,
    string SourceSceneId,
    IReadOnlyList<WorkspacePlacement> Placements,
    IReadOnlyList<WorkspaceConnectorLink>? ConnectorLinks = null,
    IReadOnlyList<WorkspaceSignalLink>? SignalLinks = null,
    IReadOnlyList<WorkspaceGroup>? Groups = null,
    WorkspaceTemplateOverride? TemplateOverride = null,
    WorkspaceFiniteBeltPreset? FiniteBeltPreset = null,
    WorkspaceFiniteRollerPreset? FiniteRollerPreset = null,
    WorkspaceDriveConnection? DriveConnection = null
);

// First editable-template slice moves a grounded operator assembly atomically.
// Individual heads and process equipment are intentionally outside this schema.
public sealed record WorkspaceTemplateOverride(int Version,string TargetId,
    string SourceFingerprint,double[] Position,double YawDegrees);

public sealed record WorkspacePlacement(
    string InstanceId,
    string AssetId,
    double[] Position,
    double[] Rotation,
    double[] Scale
);

public sealed record WorkspaceConnectorLink(
    string Id,
    string FromInstanceId,
    string FromConnectorId,
    string ToInstanceId,
    string ToConnectorId
);

public sealed record WorkspaceSignalLink(
    string Id,
    string InstanceId,
    string SignalId,
    string PointName
);

public sealed record WorkspaceGroup(
    string Id,
    string Name,
    IReadOnlyList<string> MemberInstanceIds,
    double[]? Pivot = null,
    string? ParentGroupId = null
);
