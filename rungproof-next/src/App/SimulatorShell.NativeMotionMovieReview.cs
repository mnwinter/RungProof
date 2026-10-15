namespace RungProof.Next.App;

public partial class SimulatorShell
{
    // Internal QA caller still uses the exact project-open/load handler.
    internal bool OpenNativeMotionReviewProject(string path, out string message) => TryOpenLadderProject(path, out message);
}
