using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace RungProof.Next.Connections;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
}

public interface IGuardedRuntimeClient
{
    ConnectionState State { get; }
    string EndpointDescription { get; }
    string StatusDetail { get; }
    event Action? StateChanged;
}

/// <summary>
/// Safe default used until an explicitly configured guarded PLC runtime exists.
/// This is deliberately not a fake PLC and never invents an endpoint or address.
/// Local scene simulation remains available independently of this connection.
/// </summary>
public sealed class DisconnectedRuntimeClient : IGuardedRuntimeClient
{
    public ConnectionState State => ConnectionState.Disconnected;
    public string EndpointDescription => "No guarded PLC endpoint configured";
    public string StatusDetail => "Local simulation only; physical PLC I/O is disabled.";
    public event Action? StateChanged;

    public void Refresh() => StateChanged?.Invoke();
}

/// <summary>
/// Adapter for the repository's existing guarded Python PLC runtime. The
/// Godot process owns no S7 socket; it exchanges typed JSON commands with the
/// existing tools/plc_live.py controller through plc_bridge.py.
/// </summary>
public sealed class ExternalPlcRuntimeClient : IGuardedRuntimeClient, IDisposable
{
    private readonly Func<ProcessBridgeChannel> _channelFactory;
    private ProcessBridgeChannel? _channel;
    private Task<JsonElement>? _pending;
    private CancellationTokenSource? _requestCancellation;
    private Action<JsonElement>? _completed;
    private Action<Exception>? _failed;
    private string? _sessionId;
    private string? _sceneId;
    private string _endpoint = "No external profile selected";

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public JsonElement? ActiveDescriptor { get; private set; }
    public JsonElement? LatestCycle { get; private set; }
    public bool IsBusy => _pending is not null;
    public int SessionGeneration { get; private set; }
    public string EndpointDescription => _endpoint;
    public string StatusDetail { get; private set; } = "External PLC bridge is disconnected.";
    public event Action? StateChanged;

    public ExternalPlcRuntimeClient(string projectRoot) : this(() => CreateChannel(projectRoot)) { }
    public ExternalPlcRuntimeClient(Func<ProcessBridgeChannel> channelFactory)
    {
        RuntimeBuildPolicy.RequireExternalPlc();
        _channelFactory = channelFactory;
    }

    public void DescribeProfile(string profileId, Action<JsonElement> completed, Action<Exception> failed)
    {
        Request(new { command = "describe", profileId }, response => completed(response.GetProperty("descriptor").Clone()), failed);
    }

    public void RunReadOnlyDiagnostic(string profileId, Action<JsonElement> completed, Action<Exception> failed)
    {
        Request(new { command = "diagnostic", profileId }, response => completed(response.GetProperty("result").Clone()), failed, TimeSpan.FromSeconds(10));
    }

    public void Connect(
        string profileId,
        string sceneId,
        IReadOnlyList<JsonElement> authorizedWriteScope,
        Action<JsonElement> completed,
        Action<Exception> failed,
        int connectTimeoutMs,
        JsonElement expectedDescriptor)
    {
        if (State != ConnectionState.Disconnected || IsBusy)
            throw new InvalidOperationException("Disconnect or finish the current bridge request before connecting.");
        // Own the reviewed JSON before notifying observers or handing it to a
        // worker. Callers may dispose their documents or reuse the input list.
        var scopeSnapshot = new JsonElement[authorizedWriteScope.Count];
        for (var index = 0; index < scopeSnapshot.Length; index++)
            scopeSnapshot[index] = authorizedWriteScope[index].Clone();
        var descriptorSnapshot = expectedDescriptor.Clone();
        SetState(ConnectionState.Connecting, "Opening guarded PLC session…");
        Request(new
        {
            command = "connect",
            profileId,
            sceneId,
            execute = true,
            authorizedWriteScope = scopeSnapshot,
            expectedDescriptor = descriptorSnapshot,
        }, response =>
        {
            var result = response.GetProperty("result").Clone();
            var sessionId = result.GetProperty("sessionId").GetString();
            if (string.IsNullOrWhiteSpace(sessionId) || result.GetProperty("sceneId").GetString() != sceneId
                || result.GetProperty("id").GetString() != profileId || !result.GetProperty("connected").GetBoolean())
                throw new InvalidOperationException("Bridge returned an invalid session identity.");
            _sessionId = sessionId;
            _sceneId = sceneId;
            _endpoint = $"{result.GetProperty("ip").GetString()} rack {result.GetProperty("rack").GetInt32()} slot {result.GetProperty("slot").GetInt32()}";
            ActiveDescriptor = result;
            LatestCycle = null;
            SessionGeneration++;
            SetState(ConnectionState.Connected, "Guarded PLC session connected; waiting for healthy readiness.");
            completed(result);
        }, exception =>
        {
            ClearSession("External PLC connection failed; no active session.");
            failed(exception);
        }, TimeSpan.FromMilliseconds(Math.Clamp(connectTimeoutMs + 2000L, 2000L, 30000L)));
    }

    public void Cycle(string sceneId, IReadOnlyDictionary<string, object?> pcPoints, Action<JsonElement> completed, Action<Exception> failed)
    {
        if (_sessionId is null || _sceneId != sceneId)
            throw new InvalidOperationException("No active external PLC session for the current scene.");
        Request(new
        {
            command = "cycle",
            sessionId = _sessionId,
            sceneId,
            pcPoints,
        }, response =>
        {
            var result = response.GetProperty("result").Clone();
            if (result.GetProperty("sessionId").GetString() != _sessionId || result.GetProperty("sceneId").GetString() != _sceneId)
                throw new InvalidOperationException("Bridge cycle belongs to another session or scene.");
            ValidateCycleMetadata(result);
            LatestCycle = result;
            if (!result.GetProperty("connected").GetBoolean())
                ClearSession("PLC cycle health fault closed the guarded session.", clearCycle: false);
            else
                StatusDetail = $"Guarded cycle {result.GetProperty("cycle")} · health {result.GetProperty("health").GetString()} · heartbeat {result.GetProperty("heartbeat").GetProperty("reason").GetString()}";
            completed(result);
            StateChanged?.Invoke();
        }, failed, TimeSpan.FromMilliseconds(ActiveDescriptor?.GetProperty("heartbeatTimeoutMs").GetInt32() ?? 2000));
    }

    public void Disconnect()
    {
        var sessionId = _sessionId;
        var wasBusy = IsBusy;
        if (wasBusy)
        {
            // Cancels both a queued connect and a pending cycle. A late response
            // is discarded; the next scene cannot inherit its output image.
            _requestCancellation?.Cancel();
            _channel?.Abort();
            var abandoned = _pending!;
            _ = abandoned.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            _pending = null;
            _completed = null;
            _failed = null;
            _requestCancellation?.Dispose();
            _requestCancellation = null;
        }
        ClearSession("External PLC disconnected.");
        if (!wasBusy && sessionId is not null)
            Request(new { command = "disconnect", sessionId }, _ => { }, exception =>
                SetState(ConnectionState.Disconnected, $"Bridge disconnect failed; process closed: {exception.Message}"));
    }

    private void Request(object request, Action<JsonElement> completed, Action<Exception> failed, TimeSpan? timeout = null)
    {
        if (IsBusy) throw new InvalidOperationException("A PLC bridge request is already in progress.");
        _completed = completed;
        _failed = failed;
        _channel ??= _channelFactory();
        _requestCancellation = new CancellationTokenSource();
        var channel = _channel;
        var cancellation = _requestCancellation.Token;
        // Even process startup runs outside Godot's UI/physics thread.
        _pending = Task.Run(() => channel.SendAsync(request, timeout ?? TimeSpan.FromSeconds(3), cancellation));
    }

    /// <summary>Called on the Godot main thread; workers never touch scene/UI objects.</summary>
    public void Poll()
    {
        if (_pending is not { IsCompleted: true } pending) return;
        var completed = _completed;
        var failed = _failed;
        _pending = null;
        _completed = null;
        _failed = null;
        _requestCancellation?.Dispose();
        _requestCancellation = null;
        try { completed?.Invoke(pending.GetAwaiter().GetResult()); }
        catch (Exception exception)
        {
            _channel?.Abort();
            ClearSession($"External PLC bridge fault: {exception.Message}");
            failed?.Invoke(exception);
        }
    }

    private void ClearSession(string detail, bool clearCycle = true)
    {
        _sessionId = null;
        _sceneId = null;
        ActiveDescriptor = null;
        if (clearCycle) LatestCycle = null;
        SessionGeneration++;
        SetState(ConnectionState.Disconnected, detail);
    }

    private static void ValidateCycleMetadata(JsonElement result)
    {
        if (result.GetProperty("cycle").GetInt64() <= 0
            || result.GetProperty("health").GetString() is not ("healthy" or "starting" or "degraded" or "fault"))
            throw new InvalidOperationException("Bridge returned invalid cycle health metadata.");
        var heartbeat = result.GetProperty("heartbeat");
        _ = heartbeat.GetProperty("healthy").GetBoolean();
        _ = heartbeat.GetProperty("reason").GetString();
        foreach (var field in new[] { "last_echo", "age_ms" })
        {
            var value = heartbeat.GetProperty(field);
            if (value.ValueKind != JsonValueKind.Null) _ = value.GetDouble();
        }
        var status = result.GetProperty("plcStatus");
        foreach (var field in new[] { "simulation_enable", "simulation_comm_ok", "simulation_timeout" })
            _ = status.GetProperty(field).GetBoolean();
    }

    private static ProcessBridgeChannel CreateChannel(string projectRoot)
    {
        var python = Path.Combine(projectRoot, "build", ".venv-rungproof", "Scripts", "python.exe");
        var bridge = Path.Combine(projectRoot, "tools", "plc_bridge.py");
        var profiles = Path.Combine(projectRoot, "prototype", "plc-profiles");
        var info = new ProcessStartInfo
        {
            FileName = File.Exists(python) ? python : "py",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        if (File.Exists(python)) info.ArgumentList.Add(bridge);
        else { info.ArgumentList.Add("-3"); info.ArgumentList.Add(bridge); }
        info.ArgumentList.Add(profiles);
        return new ProcessBridgeChannel(info);
    }

    private void SetState(ConnectionState state, string detail)
    {
        State = state;
        StatusDetail = detail;
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        if (_pending is not null)
            _ = _pending.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        _pending = null;
        _completed = null;
        _failed = null;
        _requestCancellation?.Cancel();
        _requestCancellation?.Dispose();
        _requestCancellation = null;
        _channel?.Dispose();
        ClearSession("External PLC bridge closed.");
    }
}
