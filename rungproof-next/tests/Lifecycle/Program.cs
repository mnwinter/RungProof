using System;
using RungProof.Next.Challenges;

internal static class Program
{
    private static int _passed;
    private static void Require(bool value, string detail)
    {
        if (!value) throw new InvalidOperationException(detail);
    }
    private static void Test(string name, Action action)
    {
        action(); _passed++; Console.WriteLine("PASS " + name);
    }
    private static void Main()
    {
        Test("Cancel retires proof while retaining the only worker slot", () =>
        {
            var life = new ChallengeRunLifecycle();
            Require(life.TryBegin(1), "First run starts");
            Require(life.Retire(1), "Cancel retires award");
            Require(life.State.MutationLocked && life.State.AwardEpoch is null, "Ownership retained");
            Require(!life.TryBegin(2), "No overlapping cancelled worker");
            Require(!life.Complete(1), "Cancelled result cannot award");
            Require(life.TryBegin(2), "Actual worker exit releases slot");
        });
        Test("Stale completion and retirement cannot disturb a newer worker", () =>
        {
            var life = new ChallengeRunLifecycle(); life.TryBegin(1); life.Complete(1); life.TryBegin(2);
            Require(!life.Complete(1) && !life.Retire(1), "Stale epoch rejected");
            Require(life.State.WorkerEpoch == 2 && life.State.AwardEpoch == 2, "New ownership intact");
            Require(life.Complete(2), "Current proof remains eligible");
        });
        Test("Epoch reuse cannot make an old result current again", () =>
        {
            var life = new ChallengeRunLifecycle();
            Require(!life.TryBegin(0), "Zero is historical, never current");
            life.TryBegin(1); life.Complete(1);
            Require(!life.TryBegin(1) && life.TryBegin(2), "Only increasing epochs start");
        });
        Test("Navigation waits for actual exit then drains exactly once", () =>
        {
            var life = new ChallengeRunLifecycle(); life.TryBegin(1);
            life.QueuePendingAction(ChallengePendingAction.Navigate, TimeSpan.Zero); life.Retire(1);
            Require(life.TakePendingActionAfterCleanup(TimeSpan.FromMilliseconds(100)) == ChallengePendingAction.None, "No early navigation");
            life.Complete(1);
            Require(life.TakePendingActionAfterCleanup(TimeSpan.FromMilliseconds(200)) == ChallengePendingAction.Navigate, "Pending navigation delivered");
            Require(life.TakePendingActionAfterCleanup(TimeSpan.FromMilliseconds(201)) == ChallengePendingAction.None, "No replay");
        });
        Test("Cleanup timeout drops action without unlocking a stuck worker", () =>
        {
            var life = new ChallengeRunLifecycle(); life.TryBegin(1);
            life.QueuePendingAction(ChallengePendingAction.Close, TimeSpan.Zero); life.Retire(1);
            Require(life.ExpirePendingAction(TimeSpan.FromSeconds(2)) == ChallengePendingAction.Close, "Exact deadline expires");
            Require(life.State.MutationLocked && life.State.PendingAction == ChallengePendingAction.None, "Ownership stays locked");
            life.Complete(1);
            Require(life.TakePendingActionAfterCleanup(TimeSpan.FromSeconds(3)) == ChallengePendingAction.None, "No delayed close replay");
        });
        Test("Late exit itself expires Reset even if no timer tick occurred", () =>
        {
            var life = new ChallengeRunLifecycle(); life.TryBegin(1);
            life.QueuePendingAction(ChallengePendingAction.Reset, TimeSpan.Zero); life.Retire(1); life.Complete(1);
            Require(life.TakePendingActionAfterCleanup(TimeSpan.FromSeconds(2.1)) == ChallengePendingAction.None, "Late Reset discarded");
        });
        Test("Repeated action requests cannot replace or extend the deadline", () =>
        {
            var life = new ChallengeRunLifecycle(); life.TryBegin(1);
            life.QueuePendingAction(ChallengePendingAction.Navigate, TimeSpan.Zero);
            life.QueuePendingAction(ChallengePendingAction.Close, TimeSpan.FromSeconds(1.5));
            Require(life.State.PendingAction == ChallengePendingAction.Navigate && life.State.PendingActionDeadline == TimeSpan.FromSeconds(2), "First intent fixed");
        });
        Test("Published state is a detached immutable snapshot", () =>
        {
            var life = new ChallengeRunLifecycle(); life.TryBegin(1); var captured = life.State; life.Retire(1); life.Complete(1);
            Require(captured.WorkerEpoch == 1 && captured.AwardEpoch == 1 && captured.MutationLocked, "Captured state retained");
        });
        Console.WriteLine($"CHALLENGE_LIFECYCLE_TESTS_PASS {_passed}; no Godot, plant, PLC or worker thread launched.");
    }
}
