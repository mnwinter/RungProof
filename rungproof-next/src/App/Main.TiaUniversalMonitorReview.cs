using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private async void VerifyTiaUniversalMonitorSweep()
    {
        var layer = new CanvasLayer { Name = "TiaUniversalMonitorSweep", Layer = 100 };
        AddChild(layer);
        var exitCode = 0;
        try
        {
            static void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            async Task Frames()
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            var keys = LadderInstructionCatalog.Entries.Select(entry => entry.Key)
                .Where(key => !TiaSweepLocalOnlyKeys.Contains(key)).ToArray();
            Check(keys.Length == TiaSweepExpectedKeys && keys.Distinct(StringComparer.Ordinal).Count() == TiaSweepExpectedKeys,
                $"Expected {TiaSweepExpectedKeys} unique Siemens-facing monitor keys; found {keys.Length}.");

            var results = new List<object>();
            foreach (var key in keys)
            {
                var fixture = PrepareTiaMonitorFixture(BuildTiaSweepFixture(key), key);
                var sourceProgram = fixture.Document.BuildProgram();
                var compile = LadderCompiler.Compile(sourceProgram);
                Check(compile.IsValid && compile.Program is not null,
                    $"{key}: monitor fixture is invalid: " + string.Join(" | ", compile.Issues.Select(issue => issue.Path + " " + issue.Message)));
                var program = compile.Program!;
                if (key == "label")
                {
                    var targetRungId = fixture.Document.Rungs[fixture.RungIndex].Id;
                    var compiledTarget = sourceProgram.Blocks!.SelectMany(block => block.Networks)
                        .Single(network => network.Id == targetRungId);
                    Check(compiledTarget.LabelInstruction?.Id == fixture.TargetId,
                        $"label: compiled target marker mismatch expected={fixture.TargetId} actual={compiledTarget.LabelInstruction?.Id ?? "missing"}.");
                }
                var runtime = new VirtualControllerRuntime(program);
                runtime.Run();
                var sessionId = Guid.NewGuid();
                var loadId = Guid.NewGuid();
                var falseSnapshot = runtime.Scan(TiaMonitorInputs(gate: false, skip: false));
                var trueSnapshot = RunTiaMonitorTrueState(runtime, key);
                var unknownSnapshot = runtime.Scan(TiaMonitorInputs(gate: true, skip: true));
                var neverRuntime = new VirtualControllerRuntime(program);
                neverRuntime.Run();
                var neverSnapshot = neverRuntime.Scan(TiaMonitorInputs(gate: true, skip: true));

                ControllerPublication Publication(VirtualControllerSnapshot snapshot, long attempt, Guid publicationLoad)
                {
                    var origin = new ObservationOrigin(sessionId, publicationLoad, 1, attempt);
                    return new(sessionId, runtime.ProgramContentId, publicationLoad, 1, attempt,
                        snapshot.ScanNumber, attempt, ControllerPublicationKind.ScanCompleted,
                        ControllerCompletion.Completed, ControllerPhaseCompletion.ProgramExecuted,
                        origin, snapshot.Observations!.WithOrigin(origin), snapshot);
                }

                var falsePublication = Publication(falseSnapshot, 1, loadId);
                var truePublication = Publication(trueSnapshot, 2, loadId);
                var unknownPublication = Publication(unknownSnapshot, 3, loadId);
                var neverPublication = Publication(neverSnapshot, 1, loadId);
                var canvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, fixture.Document)
                {
                    Size = new Vector2(1800, 1100),
                };
                layer.AddChild(canvas);
                canvas.RefreshDocument();
                await Frames();
                var target = TiaMonitorTarget(canvas, fixture, key);
                try
                {
                    Check(canvas.ReviewPowerFlowStyle(true) == ("00a651", false)
                        && canvas.ReviewPowerFlowStyle(false) == ("233db8", true)
                        && canvas.ReviewPowerFlowStyle(null) == ("87949a", false)
                        && canvas.ReviewPowerFlowStyle(TiaPowerFlowStatus.NotInterconnected) == ("000000", false),
                        $"{key}: TIA status-color contract changed.");

                    canvas.SetMonitorPublication(falsePublication);
                    canvas.FlushPendingMonitoringForReview();
                    VerifyTiaMonitorCurrentCase(canvas, fixture, key, target, falsePublication, expectInput: false);
                    results.Add(MonitorCase(key, "evaluated-false"));
                    GD.Print($"TIA_MONITOR_GATE_CASE PASS key={key} state=evaluated-false gate=tia.{key}.monitor");

                    canvas.SetMonitorPublication(truePublication);
                    canvas.FlushPendingMonitoringForReview();
                    VerifyTiaMonitorCurrentCase(canvas, fixture, key, target, truePublication, expectInput: true);
                    VerifyTiaMonitorPins(canvas, fixture, key);
                    results.Add(MonitorCase(key, "evaluated-true"));
                    GD.Print($"TIA_MONITOR_GATE_CASE PASS key={key} state=evaluated-true gate=tia.{key}.monitor");

                    canvas.SetMonitorPublication(unknownPublication);
                    canvas.FlushPendingMonitoringForReview();
                    var earlier = canvas.InstructionPresentationFor(target.Id, key);
                    Check(earlier.EarlierCycle && !earlier.CurrentExecution
                        && earlier.UnavailableReason == ObservationUnavailableReason.None,
                        $"{key}: skipped current scan did not retain an earlier-cycle value.");
                    VerifyTiaMonitorUnknownFlow(canvas, fixture, key);
                    var earlierStyle = canvas.ReviewObservationValueStyle(target.Id, key, "008f4c");
                    Check(earlierStyle.EarlierCycle && earlierStyle.Color == "488f6d",
                        $"{key}: earlier-cycle value is not shown at deterministic 50% saturation.");
                    results.Add(MonitorCase(key, "unknown-earlier-cycle"));
                    GD.Print($"TIA_MONITOR_GATE_CASE PASS key={key} state=unknown-earlier-cycle gate=tia.{key}.monitor");

                    // A new load identity must clear retained values immediately.
                    canvas.SetMonitorPublication(Publication(unknownSnapshot, 4, Guid.NewGuid()));
                    canvas.FlushPendingMonitoringForReview();
                    var cleared = canvas.InstructionPresentationFor(target.Id, key);
                    Check(!cleared.EarlierCycle && !cleared.CurrentExecution
                        && cleared.UnavailableReason == ObservationUnavailableReason.NotEvaluatedThisScan,
                        $"{key}: earlier-cycle value crossed a load identity boundary.");
                }
                finally
                {
                    canvas.QueueFree();
                    await Frames();
                }

                var neverCanvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, fixture.Document)
                {
                    Size = new Vector2(1800, 1100),
                };
                layer.AddChild(neverCanvas);
                neverCanvas.RefreshDocument();
                await Frames();
                try
                {
                    neverCanvas.SetMonitorPublication(neverPublication);
                    neverCanvas.FlushPendingMonitoringForReview();
                    var never = neverCanvas.InstructionPresentationFor(target.Id, key);
                    Check(!never.CurrentExecution && !never.EarlierCycle
                        && never.UnavailableReason == ObservationUnavailableReason.NotEvaluatedThisScan,
                        $"{key}: never-executed instruction did not remain unavailable.");
                    VerifyTiaMonitorUnknownFlow(neverCanvas, fixture, key);
                    results.Add(MonitorCase(key, "never-executed"));
                    GD.Print($"TIA_MONITOR_GATE_CASE PASS key={key} state=never-executed gate=tia.{key}.monitor");
                }
                finally
                {
                    neverCanvas.QueueFree();
                    await Frames();
                }
            }

            Check(results.Count == TiaSweepExpectedKeys * 4,
                $"Expected {TiaSweepExpectedKeys * 4} monitor cases; found {results.Count}.");
            var outputArgument = OS.GetCmdlineUserArgs().FirstOrDefault(argument =>
                argument.StartsWith("--monitor-evidence=", StringComparison.Ordinal));
            if (outputArgument is not null)
            {
                var outputPath = Path.GetFullPath(outputArgument["--monitor-evidence=".Length..]);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                var artifact = new
                {
                    schemaVersion = 1,
                    resultKind = "tia-monitor-gate-sweep",
                    createdUtc = DateTimeOffset.UtcNow.ToString("O"),
                    status = "passed",
                    offlineOnly = true,
                    installedTiaExecuted = false,
                    livePlcExecuted = false,
                    keys,
                    states = new[] { "evaluated-false", "evaluated-true", "unknown-earlier-cycle", "never-executed" },
                    caseCount = results.Count,
                    cases = results,
                };
                File.WriteAllText(outputPath, JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }));
            }
            GD.Print($"TIA_MONITOR_GATE_SWEEP_PASS keys={keys.Length} cases={results.Count} externalTia=false");
        }
        catch (Exception error)
        {
            GD.PushError("TIA_MONITOR_GATE_SWEEP_FAIL externalTia=false " + error);
            exitCode = 1;
        }
        finally
        {
            // Let the scene tree release the final Godot-owned canvas wrapper
            // before native shutdown. Quitting first can leave its managed
            // finalizer dereferencing an already-destroyed native object table.
            layer.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().Quit(exitCode);
        }
    }

    private static object MonitorCase(string key, string state) => new
    {
        gateId = $"tia.{key}.monitor",
        key,
        state,
        status = "passed",
    };

    private static TiaSweepFixture PrepareTiaMonitorFixture(TiaSweepFixture fixture, string key)
    {
        var document = fixture.Document;
        if (key is "numeric-14" or "numeric-15")
            document.UpdateTag("real_a", "real_a", PlcVariableType.Real, PlcVariableRole.Memory,
                initialValue: 0.5, address: "%MD12", comment: "Representative real input");
        if (key == "numeric-19")
            document.UpdateTag("real_b", "real_b", PlcVariableType.Real, PlcVariableRole.Memory,
                initialValue: 0.5, address: "%MD16", comment: "Representative normalized input");
        document.AddTag("monitor_gate", PlcVariableRole.Input, address: "%I1.0", comment: "Monitor sweep network gate");
        document.AddTag("monitor_skip", PlcVariableRole.Input, address: "%I1.1", comment: "Monitor sweep skip gate");

        var targetRung = document.Rungs[fixture.RungIndex];
        var originalRoot = targetRung.Root;
        targetRung.SetRoot(new EditableLadderNode("monitor-series-" + key, LadderNodeKind.Series,
            Children: System.Array.AsReadOnly(new[]
            {
                new EditableLadderNode("monitor-gate-" + key, LadderNodeKind.Contact, "monitor_gate"),
                originalRoot,
            })));

        var jump = document.AddJumpRung("Monitor skip " + key, "MonitorAfter");
        var jumpIndex = document.Rungs.IndexOf(jump);
        document.AddContact(jumpIndex, 0, "monitor_skip", false);
        var label = document.AddLabelRung("Monitor destination " + key, "MonitorAfter");
        document.Rungs.Remove(jump);
        document.Rungs.Remove(label);
        document.Rungs.Insert(fixture.RungIndex, jump);
        document.Rungs.Insert(fixture.RungIndex + 2, label);
        return fixture with { RungIndex = fixture.RungIndex + 1 };
    }

    private static Dictionary<string, bool> TiaMonitorInputs(bool gate, bool skip, bool load = false) => new(StringComparer.Ordinal)
    {
        ["enable"] = true,
        ["alternate"] = true,
        ["reset_request"] = false,
        ["load_request"] = load,
        ["monitor_gate"] = gate,
        ["monitor_skip"] = skip,
    };

    private static VirtualControllerSnapshot RunTiaMonitorTrueState(VirtualControllerRuntime runtime, string key)
    {
        VirtualControllerSnapshot snapshot;
        if (key is "ton" or "rto")
        {
            snapshot = runtime.Scan(TiaMonitorInputs(gate: true, skip: false));
            for (var scan = 1; scan < 55; scan++)
                snapshot = runtime.Scan(TiaMonitorInputs(gate: true, skip: false));
            return snapshot;
        }
        if (key == "ctu")
        {
            snapshot = runtime.Scan(TiaMonitorInputs(gate: true, skip: false));
            for (var pulse = 1; pulse < 10; pulse++)
            {
                runtime.Scan(TiaMonitorInputs(gate: false, skip: false));
                snapshot = runtime.Scan(TiaMonitorInputs(gate: true, skip: false));
            }
            return snapshot;
        }
        if (key == "ctd")
        {
            runtime.Scan(TiaMonitorInputs(gate: true, skip: false, load: true));
            snapshot = runtime.Scan(TiaMonitorInputs(gate: false, skip: false));
            for (var pulse = 0; pulse < 10; pulse++)
            {
                snapshot = runtime.Scan(TiaMonitorInputs(gate: true, skip: false));
                if (pulse < 9) runtime.Scan(TiaMonitorInputs(gate: false, skip: false));
            }
            return snapshot;
        }
        return runtime.Scan(TiaMonitorInputs(gate: true, skip: false));
    }

    private static (string Id, string RungId) TiaMonitorTarget(
        LadderEditorCanvas canvas, TiaSweepFixture fixture, string key)
    {
        var rung = fixture.Document.Rungs[fixture.RungIndex];
        var id = fixture.TargetKind switch
        {
            TiaSweepTargetKind.Node => fixture.TargetId,
            TiaSweepTargetKind.Label => fixture.TargetId,
            TiaSweepTargetKind.Network => rung.Id,
            _ => canvas.ReviewTiaTerminalInstructionId(fixture.RungIndex),
        };
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException($"{key}: monitor target has no stable instruction ID.");
        return (id, rung.Id);
    }

    private static void VerifyTiaMonitorCurrentCase(
        LadderEditorCanvas canvas,
        TiaSweepFixture fixture,
        string key,
        (string Id, string RungId) target,
        ControllerPublication publication,
        bool expectInput)
    {
        var observation = publication.Observations!.Find(target.Id).Observation
            ?? throw new InvalidOperationException($"{key}: current monitor publication omitted target '{target.Id}'; observed="
                + string.Join(',', publication.Observations.GlobalLast.Values.Select(item => item.InstructionId + ":" + item.CatalogKey)));
        if (observation.InstructionId != target.Id || observation.CatalogKey != key
            || observation.Origin != publication.ObservationOrigin
            || observation.Origin?.SessionId != publication.SessionId
            || observation.Origin?.LoadId != publication.LoadId
            || observation.Origin?.ExecutionGeneration != publication.ExecutionGeneration
            || observation.Origin?.ScanAttemptNumber != publication.ScanAttemptNumber)
            throw new InvalidOperationException($"{key}: target observation identity does not match its publication.");
        var view = canvas.InstructionPresentationFor(target.Id, key);
        if (!view.CurrentExecution || view.EarlierCycle || view.UnavailableReason != ObservationUnavailableReason.None)
            throw new InvalidOperationException($"{key}: current execution was not presented as current.");

        var flow = fixture.TargetKind == TiaSweepTargetKind.Node
            ? canvas.ReviewPowerFlowForElement(fixture.RungIndex, fixture.TargetId)
            : canvas.ReviewTerminalPowerFlow(fixture.RungIndex);
        if (flow.Input != expectInput || flow.Output is null)
            throw new InvalidOperationException($"{key}: expected current flow input={expectInput}; actual={flow}.");
        var inputStyle = canvas.ReviewPowerFlowStyle(flow.Input);
        if (inputStyle != (expectInput ? ("00a651", false) : ("233db8", true)))
            throw new InvalidOperationException($"{key}: current input flow style does not match Siemens state colors.");
        if (fixture.TargetKind == TiaSweepTargetKind.Label
            && canvas.ReviewNetworkLabelPowerFlowStyle(fixture.RungIndex) != ("000000", false))
            throw new InvalidOperationException("label: marker must remain black/solid and not interconnected.");
    }

    private static void VerifyTiaMonitorUnknownFlow(LadderEditorCanvas canvas, TiaSweepFixture fixture, string key)
    {
        var flow = fixture.TargetKind == TiaSweepTargetKind.Node
            ? canvas.ReviewPowerFlowForElement(fixture.RungIndex, fixture.TargetId)
            : canvas.ReviewTerminalPowerFlow(fixture.RungIndex);
        if (flow != (null, null))
            throw new InvalidOperationException($"{key}: unexecuted current flow must be unknown; actual={flow}.");
        if (canvas.ReviewPowerFlowStyle(flow.Input) != ("87949a", false)
            || canvas.ReviewPowerFlowStyle(flow.Output) != ("87949a", false))
            throw new InvalidOperationException($"{key}: unknown current flow is not gray/solid.");
        if (fixture.TargetKind == TiaSweepTargetKind.Label
            && canvas.ReviewNetworkLabelPowerFlowStyle(fixture.RungIndex) != ("000000", false))
            throw new InvalidOperationException("label: unknown network execution must not energize its marker.");
    }

    private static void VerifyTiaMonitorPins(LadderEditorCanvas canvas, TiaSweepFixture fixture, string key)
    {
        if (fixture.TargetKind != TiaSweepTargetKind.Terminal) return;
        var pins = canvas.ReviewTiaTerminalPins(fixture.RungIndex);
        if (pins.Inputs.Count == 0 || pins.Outputs.Count == 0)
            throw new InvalidOperationException($"{key}: terminal projection omitted input or output pins.");
        var inputNames = pins.Inputs.Select(pin => pin.Pin).ToArray();
        var outputNames = pins.Outputs.Select(pin => pin.Pin).ToArray();
        string[] expectedInputs;
        string[] expectedOutputs;
        if (key is "ton" or "tof" or "tp")
        {
            expectedInputs = ["IN", "PT"];
            expectedOutputs = ["Q", "ET"];
        }
        else if (key == "rto")
        {
            expectedInputs = ["IN", "R", "PT"];
            expectedOutputs = ["Q", "ET"];
        }
        else if (key == "timer-reset")
        {
            expectedInputs = ["IN"];
            expectedOutputs = ["ENO"];
        }
        else if (key == "ctu")
        {
            expectedInputs = ["CU", "R", "PV"];
            expectedOutputs = ["Q", "CV"];
        }
        else if (key == "ctd")
        {
            expectedInputs = ["CD", "LD", "PV"];
            expectedOutputs = ["Q", "CV"];
        }
        else if (key.StartsWith("numeric-", StringComparison.Ordinal))
        {
            var kind = (LadderNumericOperationKind)int.Parse(key[8..], System.Globalization.CultureInfo.InvariantCulture);
            expectedInputs = TiaNumericInstructionPorts.Inputs(kind).ToArray();
            expectedOutputs = [TiaNumericInstructionPorts.Output(kind)];
        }
        else if (key == "call")
        {
            expectedInputs = ["EnableIn", "PermitInOut"];
            expectedOutputs = ["EnableOut", "PermitInOut", "ENO"];
        }
        else
        {
            expectedInputs = ["EN"];
            expectedOutputs = ["ENO"];
        }
        if (!inputNames.SequenceEqual(expectedInputs, StringComparer.Ordinal)
            || !outputNames.SequenceEqual(expectedOutputs, StringComparer.Ordinal))
            throw new InvalidOperationException($"{key}: pin projection mismatch. inputs={string.Join(',', inputNames)} outputs={string.Join(',', outputNames)}");
    }
}
