using System;
using System.Text.Json;

namespace RungProof.Next.Connections;

/// <summary>
/// Plant playback is independent of the PLC connection and heartbeat exchange.
/// Matches prototype/src/livePlcBinding.js readiness; this sends no PLC command.
/// Recovery and a new session always require an explicit Run.
/// </summary>
public sealed class ExternalScenePlayback
{
    private int _generation = -1;
    public bool IsRunning { get; private set; }
    public bool IsReady { get; private set; }
    public string ReadinessLabel { get; private set; } = "DISCONNECTED";

    public void Update(ConnectionState connection, int generation, JsonElement? cycle)
    {
        if (_generation != generation) Pause();
        _generation = generation;
        ReadinessLabel = connection != ConnectionState.Connected ? "DISCONNECTED" : EvaluateReadiness(cycle);
        IsReady = ReadinessLabel == "HEALTHY";
        if (!IsReady) Pause();
    }

    public bool TryRun()
    {
        if (!IsReady) return false;
        IsRunning = true;
        return true;
    }

    public void Pause() => IsRunning = false;

    private static string EvaluateReadiness(JsonElement? cycle)
    {
        if (cycle is not JsonElement value) return "AWAITING FIRST READBACK";
        if (value.ValueKind != JsonValueKind.Object) return "UNKNOWN HEALTH";
        if (!value.TryGetProperty("health", out var health) || health.ValueKind != JsonValueKind.String)
            return "UNKNOWN HEALTH";
        var label = (health.GetString() ?? "unknown").Replace('_', ' ').ToUpperInvariant();
        if (label != "HEALTHY") return label;
        if (!value.TryGetProperty("plcStatus", out var status) || status.ValueKind != JsonValueKind.Object
            || !TryBool(status, "simulation_enable", out var enable)
            || !TryBool(status, "simulation_comm_ok", out var commOk)
            || !TryBool(status, "simulation_timeout", out var timeout)) return "PLC STATUS INCOMPLETE";
        if (timeout) return "PLC TIMEOUT";
        if (!enable) return "SIMULATION DISABLED";
        if (!commOk) return "PLC COMM NOT OK";
        return "HEALTHY";
    }

    private static bool TryBool(JsonElement value, string name, out bool result)
    {
        result = false;
        if (!value.TryGetProperty(name, out var field) || field.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return false;
        result = field.GetBoolean();
        return true;
    }
}
