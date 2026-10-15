using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RungProof.Next.Connections;

internal static class Program
{
    private static readonly JsonElement OfflineDescriptor = JsonSerializer.SerializeToElement(new { id = "offline.json" });
    private static string _python = "";
    private static int _passed;

    private static async Task Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0])) throw new ArgumentException("Pass the local Python executable.");
        _python = args[0];
        await Test("stderr flood is drained without blocking JSON", async () =>
        {
            using var channel = Channel();
            var result = await channel.SendAsync(new { command = "flood", value = 7 }, TimeSpan.FromSeconds(3));
            Require(result.GetProperty("result").GetProperty("value").GetInt32() == 7);
        });
        await Test("missing response times out, kills process, and recovers on a fresh process", async () =>
        {
            using var channel = Channel();
            var pid = Pid(await channel.SendAsync(new { command = "echo" }, TimeSpan.FromSeconds(3)));
            var clock = Stopwatch.StartNew();
            await Reject<TimeoutException>(() => channel.SendAsync(new { command = "hang" }, TimeSpan.FromMilliseconds(120)));
            Require(clock.Elapsed < TimeSpan.FromSeconds(2));
            RequireExited(pid);
            var recovered = await channel.SendAsync(new { command = "echo", value = 9 }, TimeSpan.FromSeconds(3));
            Require(Pid(recovered) != pid && recovered.GetProperty("result").GetProperty("value").GetInt32() == 9);
        });
        await Test("malformed, rejected, and EOF replies close the channel", async () =>
        {
            foreach (var command in new[] { "malformed", "reject", "close" })
            {
                using var channel = Channel();
                var pid = Pid(await channel.SendAsync(new { command = "echo" }, TimeSpan.FromSeconds(3)));
                await Reject<Exception>(() => channel.SendAsync(new { command }, TimeSpan.FromSeconds(3)));
                RequireExited(pid);
            }
        });
        await Test("queued requests are serialized and preserve reply identity", async () =>
        {
            using var channel = Channel();
            var first = channel.SendAsync(new { command = "delay", value = 1 }, TimeSpan.FromSeconds(3));
            var second = channel.SendAsync(new { command = "echo", value = 2 }, TimeSpan.FromSeconds(3));
            Require((await first).GetProperty("result").GetProperty("value").GetInt32() == 1);
            Require((await second).GetProperty("result").GetProperty("value").GetInt32() == 2);
        });
        await Test("cancel before startup never creates a process", async () =>
        {
            using var channel = Channel();
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Reject<OperationCanceledException>(() => channel.SendAsync(new { command = "connect" }, TimeSpan.FromSeconds(3), cancelled.Token));
        });
        await Test("Dispose cancels a pending read and refuses restart", async () =>
        {
            var channel = Channel();
            var pid = Pid(await channel.SendAsync(new { command = "echo" }, TimeSpan.FromSeconds(3)));
            var pending = channel.SendAsync(new { command = "hang" }, TimeSpan.FromSeconds(3));
            channel.Dispose();
            await Reject<Exception>(() => pending);
            RequireExited(pid);
            await Reject<ObjectDisposedException>(() => channel.SendAsync(new { command = "echo" }, TimeSpan.FromSeconds(3)));
        });
        await Test("controller completion runs only on the polling caller", async () =>
        {
            using var client = Client();
            var completed = false;
            var callbackThread = 0;
            client.Connect("offline.json", "offline-scene", [], _ => { completed = true; callbackThread = Environment.CurrentManagedThreadId; }, Fail, 100, OfflineDescriptor);
            await Task.Delay(300);
            Require(!completed && client.State == ConnectionState.Connecting);
            var pollThread = Environment.CurrentManagedThreadId;
            client.Poll();
            Require(completed && callbackThread == pollThread && client.State == ConnectionState.Connected);
        });
        await Test("connect snapshots typed scope and descriptor before observers and async serialization", async () =>
        {
            using var client = Client();
            using var scopeDocument = JsonDocument.Parse("""[{"name":"permit","dataType":"BOOL","safeValue":false},{"name":"count","dataType":"INT","safeValue":0},{"name":"speed","dataType":"REAL","safeValue":0.25}]""");
            using var descriptorDocument = JsonDocument.Parse("""{"id":"typed-scope-fixture.json","review":{"revision":7}}""");
            var scope = new List<JsonElement>();
            foreach (var entry in scopeDocument.RootElement.EnumerateArray()) scope.Add(entry);
            var invalidatedBeforeRequest = false;
            client.StateChanged += () =>
            {
                if (client.State != ConnectionState.Connecting) return;
                // SetState precedes Request: disposal here deterministically tests
                // ownership rather than racing a fast worker serialization.
                scopeDocument.Dispose();
                descriptorDocument.Dispose();
                scope.Clear();
                scope.Add(JsonSerializer.SerializeToElement(new { safeValue = "changed" }));
                invalidatedBeforeRequest = true;
            };
            JsonElement observed = default;
            client.Connect("typed-scope-fixture.json", "offline-scene", scope,
                result => observed = result, Fail, 100, descriptorDocument.RootElement);
            await Pump(client);
            Require(invalidatedBeforeRequest && client.State == ConnectionState.Connected);
            var wireScope = observed.GetProperty("observedAuthorizedWriteScope");
            Require(wireScope.GetArrayLength() == 3);
            Require(wireScope[0].GetProperty("name").GetString() == "permit"
                && wireScope[0].GetProperty("safeValue").ValueKind == JsonValueKind.False);
            Require(wireScope[1].GetProperty("dataType").GetString() == "INT"
                && wireScope[1].GetProperty("safeValue").ValueKind == JsonValueKind.Number
                && wireScope[1].GetProperty("safeValue").GetInt32() == 0);
            Require(wireScope[2].GetProperty("dataType").GetString() == "REAL"
                && wireScope[2].GetProperty("safeValue").ValueKind == JsonValueKind.Number
                && wireScope[2].GetProperty("safeValue").GetDouble() == 0.25);
            Require(observed.GetProperty("observedExpectedDescriptor").GetProperty("review").GetProperty("revision").GetInt32() == 7);
        });
        await Test("disconnect invalidates an in-flight connect and permits a fresh session", async () =>
        {
            using var client = Client();
            var staleApplied = false;
            client.Connect("offline.json", "old-scene", [], _ => staleApplied = true, Fail, 100, OfflineDescriptor);
            client.Disconnect();
            await Task.Delay(100);
            client.Poll();
            Require(!staleApplied && client.State == ConnectionState.Disconnected && client.ActiveDescriptor is null);
            client.Connect("offline.json", "new-scene", [], _ => { }, Fail, 100, OfflineDescriptor);
            await Pump(client);
            Require(client.State == ConnectionState.Connected && client.ActiveDescriptor!.Value.GetProperty("sceneId").GetString() == "new-scene");
        });
        foreach (var mode in new[] { "stale", "bad_status", "hang" })
        {
            await Test($"cycle {mode} cannot publish and closes session", async () =>
            {
                using var client = Client();
                client.Connect("offline.json", "offline-scene", [], _ => { }, Fail, 100, OfflineDescriptor);
                await Pump(client);
                var published = false;
                var failed = false;
                client.Cycle("offline-scene", new Dictionary<string, object?> { ["mode"] = mode }, _ => published = true, _ => failed = true);
                await Pump(client);
                Require(!published && failed && client.State == ConnectionState.Disconnected && client.ActiveDescriptor is null);
            });
        }
        await Test("healthy and fault cycles retain actual guarded health metadata", async () =>
        {
            using var client = Client();
            client.Connect("offline.json", "offline-scene", [], _ => { }, Fail, 100, OfflineDescriptor);
            await Pump(client);
            client.Cycle("offline-scene", new Dictionary<string, object?>(), _ => { }, Fail);
            await Pump(client);
            Require(client.LatestCycle!.Value.GetProperty("health").GetString() == "healthy");
            client.Cycle("offline-scene", new Dictionary<string, object?> { ["mode"] = "fault" }, _ => { }, Fail);
            await Pump(client);
            Require(client.State == ConnectionState.Disconnected && client.LatestCycle!.Value.GetProperty("health").GetString() == "fault");
        });
        await Test("symbolic ownership, exact scope, types and numeric bounds are enforced", () =>
        {
            using var simulation = JsonDocument.Parse("""{"points":[{"name":"sensor","owner":"PC","type":"BOOL"},{"name":"motor","owner":"PLC","type":"BOOL"}]}""");
            using var descriptor = JsonDocument.Parse("""{"cycleMs":20,"heartbeatTimeoutMs":1000,"pcPointScope":[{"name":"sensor","dataType":"BOOL"}],"plcPointScope":[{"name":"motor","dataType":"BOOL"}]}""");
            ExternalSceneContract.Validate(simulation.RootElement, descriptor.RootElement);
            Require(ExternalSceneContract.SamplePcPoints(descriptor.RootElement, new Dictionary<string, object?> { ["sensor"] = true })["sensor"] is true);
            foreach (var json in new[] { "{}", "{\"motor\":1}", "{\"motor\":null}", "{\"motor\":true,\"extra\":false}" })
            {
                using var outputs = JsonDocument.Parse(json);
                ExpectRejected(() => ExternalSceneContract.ReadPlcPoints(descriptor.RootElement, outputs.RootElement));
            }
            using var wrongOwner = JsonDocument.Parse("""{"points":[{"name":"sensor","owner":"PLC","type":"BOOL"},{"name":"motor","owner":"PLC","type":"BOOL"}]}""");
            ExpectRejected(() => ExternalSceneContract.Validate(wrongOwner.RootElement, descriptor.RootElement));
            ExpectRejected(() => ExternalSceneContract.TypedValue("BOOL", 1, "x"));
            ExpectRejected(() => ExternalSceneContract.TypedValue("INT", 32768, "x"));
            ExpectRejected(() => ExternalSceneContract.TypedValue("DINT", 1.5, "x"));
            ExpectRejected(() => ExternalSceneContract.TypedValue("REAL", double.NaN, "x"));
            Require((long)ExternalSceneContract.TypedValue("INT", -32768, "x") == -32768);
            Require((long)ExternalSceneContract.TypedValue("DINT", int.MaxValue, "x") == int.MaxValue);
            return Task.CompletedTask;
        });
        await Test("playback requires healthy exchange and every existing PLC readiness bit", () =>
        {
            for (var bits = 0; bits < 8; bits++)
            {
                var playback = new ExternalScenePlayback();
                var cycle = JsonSerializer.SerializeToElement(new { health = "healthy", plcStatus = new
                {
                    simulation_enable = (bits & 1) != 0, simulation_comm_ok = (bits & 2) != 0,
                    simulation_timeout = (bits & 4) != 0,
                } });
                playback.Update(ConnectionState.Connected, 1, cycle);
                Require(playback.IsReady == (bits == 3) && playback.TryRun() == (bits == 3));
                foreach (var health in new[] { "starting", "degraded", "fault" })
                {
                    playback.Update(ConnectionState.Connected, 1, JsonSerializer.SerializeToElement(new
                    { health, plcStatus = new { simulation_enable = true, simulation_comm_ok = true, simulation_timeout = false } }));
                    Require(!playback.IsReady && !playback.IsRunning && !playback.TryRun());
                }
            }
            return Task.CompletedTask;
        });
        await Test("readiness recovery, Stop, and fresh sessions require a new Run", () =>
        {
            var playback = new ExternalScenePlayback();
            var good = JsonSerializer.SerializeToElement(new { health = "healthy", plcStatus = new
            { simulation_enable = true, simulation_comm_ok = true, simulation_timeout = false } });
            playback.Update(ConnectionState.Connected, 1, null);
            Require(!playback.TryRun());
            playback.Update(ConnectionState.Connected, 1, good);
            Require(!playback.IsRunning && playback.TryRun());
            playback.Pause();
            playback.Update(ConnectionState.Connected, 1, good);
            Require(!playback.IsRunning && playback.TryRun());
            playback.Update(ConnectionState.Connected, 1, JsonSerializer.SerializeToElement(new { health = "starting" }));
            Require(!playback.IsRunning);
            playback.Update(ConnectionState.Connected, 1, good);
            Require(!playback.IsRunning && playback.TryRun());
            playback.Update(ConnectionState.Connected, 2, good);
            Require(!playback.IsRunning && playback.TryRun());
            playback.Update(ConnectionState.Disconnected, 3, good);
            Require(!playback.IsRunning && !playback.IsReady && !playback.TryRun());
            return Task.CompletedTask;
        });
        await Test("incomplete or non-boolean PLC status cannot authorize playback", () =>
        {
            foreach (var json in new[] { "null", "1", "{}", "{\"health\":\"healthy\"}",
                "{\"health\":\"healthy\",\"plcStatus\":{\"simulation_enable\":\"true\",\"simulation_comm_ok\":true,\"simulation_timeout\":false}}" })
            {
                var playback = new ExternalScenePlayback();
                using var cycle = JsonDocument.Parse(json);
                playback.Update(ConnectionState.Connected, 1, cycle.RootElement);
                Require(!playback.IsReady && !playback.TryRun());
            }
            return Task.CompletedTask;
        });
        await Test("profile presentation preserves typed configuration, escapes names and states verification limits", () =>
        {
            using var profile = JsonDocument.Parse("""{"cpuFamily":"S7-1200","ip":"192.0.2.10","rack":0,"slot":1,"cycleMs":20,"connectTimeoutMs":750,"heartbeatPcTag":"pc_beat","heartbeatEchoTag":"plc_echo","heartbeatTimeoutMs":1000,"writeScope":[{"name":"[permit]","symbol":"DB.[permit]","dataType":"BOOL","address":"DB1.DBX0.0","safeValue":false},{"name":"count","symbol":"DB.count","dataType":"INT","address":"DB1.DBW2","safeValue":0}],"readScope":[{"name":"ready","symbol":"DB.ready","dataType":"BOOL","address":"DB1.DBX4.0"}],"pcPointScope":[{"name":"sensor","tag":"permit","dataType":"BOOL","address":"DB1.DBX0.0","inverted":true}],"plcPointScope":[{"name":"motor","tag":"ready","dataType":"BOOL","address":"DB1.DBX4.0"}]}""");
            var formatted = ExternalProfilePresentation.Format(profile.RootElement);
            foreach (var expected in new[] { "S7-1200", "192.0.2.10", "rack 0", "slot 1", "Cycle: 20 ms",
                "connect timeout: 750 ms", "pc_beat → plc_echo", "timeout: 1000 ms", "BOOL", "INT", "DB1.DBW2",
                "configured safe value: false", "configured safe value: 0", "sensor → permit", "motor → ready",
                "inverted: true", "[lb]permit[rb]", "DB.[lb]permit[rb]", "LOCAL PROFILE VALID",
                "does not verify CPU access permissions", "live handshake or watchdog operation",
                "does not prove they were written on Stop or Disconnect" })
                Require(formatted.Contains(expected, StringComparison.Ordinal));
            using var missing = JsonDocument.Parse("""{"writeScope":[{"name":"unknown","dataType":"BOOL"}],"readScope":[],"pcPointScope":[],"plcPointScope":[]}""");
            var unknown = ExternalProfilePresentation.Format(missing.RootElement);
            Require(unknown.Contains("CPU: not provided", StringComparison.Ordinal)
                && unknown.Contains("configured safe value: not provided", StringComparison.Ordinal)
                && !unknown.Contains("configured safe value: false", StringComparison.Ordinal));
            return Task.CompletedTask;
        });
        Console.WriteLine($"CONNECTION_TESTS_PASS {_passed}; offline child processes only, no PLC transport or network.");
    }

    private static ProcessBridgeChannel Channel()
    {
        var info = new ProcessStartInfo(_python) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(Path.GetFullPath("tests/Connections/fake_bridge.py"));
        return new ProcessBridgeChannel(info);
    }
    private static ExternalPlcRuntimeClient Client() => new(Channel);
    private static int Pid(JsonElement response) => response.GetProperty("result").GetProperty("pid").GetInt32();
    private static void Fail(Exception exception) => throw new Exception("Unexpected bridge error", exception);
    private static void Require(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
    private static void RequireExited(int pid)
    {
        try { using var process = Process.GetProcessById(pid); Require(process.WaitForExit(1000)); }
        catch (ArgumentException) { /* Already reaped. */ }
    }
    private static async Task Pump(ExternalPlcRuntimeClient client)
    {
        var deadline = Stopwatch.StartNew();
        while (client.IsBusy && deadline.Elapsed < TimeSpan.FromSeconds(5)) { client.Poll(); await Task.Delay(5); }
        Require(!client.IsBusy);
    }
    private static async Task Reject<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
    private static void ExpectRejected(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Invalid symbolic contract was accepted.");
    }
    private static async Task Test(string name, Func<Task> action)
    {
        await action(); _passed++; Console.WriteLine($"PASS {name}");
    }
}
