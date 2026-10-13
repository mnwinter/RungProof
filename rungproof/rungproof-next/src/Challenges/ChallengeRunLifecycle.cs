using System;

namespace RungProof.Next.Challenges;

/// <summary>
/// Main-thread worker ownership and award eligibility. Cancelling retires the
/// award epoch immediately but does not release the worker slot. Only actual
/// completion releases it, preventing concurrent trials or stale progress.
/// </summary>
public sealed class ChallengeRunLifecycle
{
    private long? _workerEpoch;
    private long? _awardEpoch;
    private long _lastStartedEpoch;
    private ChallengePendingAction _pendingAction;
    private TimeSpan? _pendingDeadline;
    public static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(2);

    public ChallengeRunLifecycleState State => new(_workerEpoch, _awardEpoch,
        _workerEpoch.HasValue, _workerEpoch.HasValue, _pendingAction, _pendingDeadline);

    public bool TryBegin(long epoch)
    {
        if (_workerEpoch.HasValue || epoch <= _lastStartedEpoch) return false;
        _workerEpoch = _awardEpoch = epoch;
        _lastStartedEpoch = epoch;
        return true;
    }

    public bool Retire(long epoch)
    {
        if (_workerEpoch != epoch || _awardEpoch != epoch) return false;
        _awardEpoch = null;
        return true;
    }

    public bool Complete(long epoch)
    {
        if (_workerEpoch != epoch) return false;
        var eligible = _awardEpoch == epoch;
        _workerEpoch = _awardEpoch = null;
        return eligible;
    }

    public bool QueuePendingAction(ChallengePendingAction action, TimeSpan monotonicNow)
    {
        ValidateTime(monotonicNow);
        if (action is not (ChallengePendingAction.Navigate or ChallengePendingAction.Close or ChallengePendingAction.Reset))
            return false;
        if (!_workerEpoch.HasValue) return false;
        // The first requested action wins; repeated input cannot prolong the
        // deadline or silently replace an already displayed pending action.
        if (_pendingAction != ChallengePendingAction.None) return false;
        _pendingAction = action;
        _pendingDeadline = monotonicNow + CleanupTimeout;
        return true;
    }

    public ChallengePendingAction ExpirePendingAction(TimeSpan monotonicNow)
    {
        ValidateTime(monotonicNow);
        if (!_pendingDeadline.HasValue || monotonicNow < _pendingDeadline.Value) return ChallengePendingAction.None;
        var action = _pendingAction;
        ClearPendingAction();
        return action;
    }

    public ChallengePendingAction TakePendingActionAfterCleanup(TimeSpan monotonicNow)
    {
        ExpirePendingAction(monotonicNow);
        if (_workerEpoch.HasValue) return ChallengePendingAction.None;
        var action = _pendingAction;
        ClearPendingAction();
        return action;
    }

    private void ClearPendingAction()
    {
        _pendingAction = ChallengePendingAction.None;
        _pendingDeadline = null;
    }

    private static void ValidateTime(TimeSpan now)
    {
        if (now < TimeSpan.Zero || now > TimeSpan.MaxValue - CleanupTimeout)
            throw new ArgumentOutOfRangeException(nameof(now));
    }
}
