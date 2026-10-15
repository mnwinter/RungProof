using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RungProof.Next.Connections;

/// <summary>
/// One bounded JSON exchange at a time. This class has no Godot or S7 dependency;
/// its caller applies completed responses on the application's main thread.
/// A failed exchange destroys the process so a late reply cannot become the
/// response to the next request.
/// </summary>
public sealed class ProcessBridgeChannel : IDisposable
{
    private readonly ProcessStartInfo _startInfo;
    private readonly SemaphoreSlim _exchange = new(1, 1);
    private readonly object _processGate = new();
    private Process? _process;
    private CancellationTokenSource? _processLifetime;
    private bool _disposed;
    private string _stderr = string.Empty;

    public ProcessBridgeChannel(ProcessStartInfo startInfo) => _startInfo = startInfo;

    public async Task<JsonElement> SendAsync(object request, TimeSpan timeout, CancellationToken cancellation = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(timeout);
        await _exchange.WaitAsync(deadline.Token).ConfigureAwait(false);
        try
        {
            var process = EnsureProcess(deadline.Token);
            CancellationToken lifetime;
            lock (_processGate) lifetime = _processLifetime!.Token;
            using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token, lifetime);
            var token = cancelled.Token;
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), token).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(token).ConfigureAwait(false);
            var output = await process.StandardOutput.ReadLineAsync(token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException($"PLC bridge stopped without a response. {StderrDetail()}");
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            if (!root.TryGetProperty("ok", out var ok) || ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidOperationException("PLC bridge response is missing its Boolean ok field.");
            if (!ok.GetBoolean())
                throw new InvalidOperationException(root.GetProperty("error").GetString() ?? "PLC bridge rejected the request.");
            return root.Clone();
        }
        catch (OperationCanceledException exception)
        {
            Abort();
            throw new TimeoutException("PLC bridge exchange timed out or was cancelled; the process was closed.", exception);
        }
        catch
        {
            Abort();
            throw;
        }
        finally { _exchange.Release(); }
    }

    private Process EnsureProcess(CancellationToken token)
    {
        lock (_processGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            token.ThrowIfCancellationRequested();
            if (_process is { HasExited: false }) return _process;
            _process?.Dispose();
            _processLifetime?.Dispose();
            _processLifetime = new CancellationTokenSource();
            _stderr = string.Empty;
            _process = Process.Start(_startInfo) ?? throw new InvalidOperationException("Unable to start the guarded PLC bridge.");
            // Drain continuously: an undrained stderr pipe can deadlock Python
            // before it writes stdout. Keep only the last 2 KiB for diagnostics.
            _ = DrainErrorsAsync(_process, _processLifetime.Token);
            return _process;
        }
    }

    private async Task DrainErrorsAsync(Process process, CancellationToken token)
    {
        try
        {
            var buffer = new char[512];
            while (true)
            {
                var count = await process.StandardError.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
                if (count == 0) return;
                lock (_processGate)
                {
                    if (!ReferenceEquals(_process, process)) return;
                    _stderr += new string(buffer, 0, count);
                    if (_stderr.Length > 2048) _stderr = _stderr[^2048..];
                }
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or System.IO.IOException or InvalidOperationException)
        { /* The process owner cancelled/closed the pipes. */ }
    }

    private string StderrDetail() { lock (_processGate) return _stderr.Trim(); }

    public void Abort()
    {
        lock (_processGate)
        {
            _processLifetime?.Cancel();
            if (_process is { HasExited: false })
            {
                try { _process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { /* Already exited. */ }
            }
            _process?.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        lock (_processGate) _disposed = true;
        Abort();
        // An in-flight exchange still owns the semaphore and lifetime token.
        // Let it finish unwinding instead of disposing them underneath its await.
    }
}
