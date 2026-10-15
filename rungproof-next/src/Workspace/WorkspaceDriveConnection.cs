namespace RungProof.Next.Workspace;

// Explicit registration of one generic asset's drive I/O, not a load/preset graph.
public sealed record WorkspaceDriveConnection(int Version,string InstanceId,string SourceFingerprint,
    string RunPoint,string RunningPoint,string ActualSpeedPoint);

// Mapping completeness alone never grants runtime execution. Main must also
// validate the current program, connection identity, controller and lifecycle.
public sealed record WorkspaceDriveConnectionEligibility(bool CommandMapped,bool FeedbackMapped)
{
    public bool CanRun => FeedbackMapped;
    public bool CommandDetachCanCoast => FeedbackMapped&&!CommandMapped;
}
