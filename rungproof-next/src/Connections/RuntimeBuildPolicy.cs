using System;

namespace RungProof.Next.Connections;

/// <summary>Compile-time delivery policy; command-line flags cannot enable transport.</summary>
public static class RuntimeBuildPolicy
{
    public static bool IsOfflineDemo
    {
        get
        {
#if RUNGPROOF_OFFLINE_DEMO
            return true;
#else
            return false;
#endif
        }
    }

    public static void RequireExternalPlc()
    {
#if RUNGPROOF_OFFLINE_DEMO
        throw new NotSupportedException("External PLC transport is disabled in this offline demo build.");
#endif
    }
}
