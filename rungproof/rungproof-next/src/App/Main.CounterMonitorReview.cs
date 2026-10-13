using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    // Explicit offline QA only: run the actual editor draft and plant input,
    // holding automatic callbacks so every count has a reproducible snapshot.
    private async void VerifyCounterMonitor()
    {
        var checks = 0;
        void Check(bool ok, string label)
        {
            checks++;
            if (!ok) throw new InvalidOperationException(label);
        }
        try
        {
            SetPhysicsProcess(false);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _simulatorShell!.GetNode<AcceptDialog>("SimulatorModeDialog").Hide();
            AddMigratedScene("lab-4-01-press-count-lamp", _candidateCatalog!, _mainCamera!, false, false);
            AuthoredDemoLadderPrograms.TryCreate("lab-4-01-press-count-lamp", out var document);
            _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));
            var rung = document.Rungs.Single(r => r.IsCounter);
            var canvases = _simulatorShell.FindChildren("*", "Control", true, false).OfType<LadderEditorCanvas>().ToArray();
            Check(canvases.Length == 2, "both vendor canvases present");
            var readout = typeof(LadderEditorCanvas).GetMethod("CounterReadoutLines", BindingFlags.Instance | BindingFlags.Public);
            Check(readout is not null, "counter monitoring presentation is missing");
            string[] Lines(LadderEditorCanvas canvas) => (string[])readout!.Invoke(canvas, new object[] { rung })!;
            foreach (var canvas in canvases)
                Check(Lines(canvas).Contains("Current count: —"), "offline count must be unknown, not fabricated zero");
            RunEditorDraftController();
            _PhysicsProcess(.02);
            await ToSignal(GetTree().CreateTimer(.08), SceneTreeTimer.SignalName.Timeout);
            _simulatorShell.SetReviewState("ladder", string.Empty);
            // Use the existing Scene I/O collapse button to give the ladder the
            // same usable height at a 900px capture viewport as on a taller desktop.
            if (_simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables").Visible)
                _simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle")
                    .EmitSignal(Button.SignalName.Pressed);
            var tabs = _simulatorShell.GetNode<TabContainer>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs");
            var output = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--counter-review-output=", StringComparison.Ordinal))?.Split('=', 2)[1];
            if (output is not null) Directory.CreateDirectory(output);
            for (var count = 0; count <= 3; count++)
            {
                if (count > 0)
                {
                    Check(ExecuteSelectedControllerAction("toggle-pulse_received"), "actual count input action accepted");
                    _PhysicsProcess(.02);
                    await ToSignal(GetTree().CreateTimer(.08), SceneTreeTimer.SignalName.Timeout);
                    foreach (var canvas in canvases)
                        Check(Lines(canvas).Contains("Count input: TRUE"), "sampled pulse is displayed while active");
                    _PhysicsProcess(.02);
                    await ToSignal(GetTree().CreateTimer(.08), SceneTreeTimer.SignalName.Timeout);
                }
                var state = _virtualController!.Snapshot.Counters["press_count"];
                Check(state.Accumulated == count && state.Done == (count == 3), "actual counter progression");
                foreach (var canvas in canvases)
                {
                    var lines = Lines(canvas);
                    GD.Print($"COUNTER_READOUT_DETAIL expectedCount={count} instanceCount={state.Accumulated} instanceInput={state.CountInput} runtimeScan={_virtualController.Snapshot.ScanNumber} canvasScan={canvas.MonitorScanNumber} path={canvas.GetPath()} status={canvas.MonitorStatusText} sessionPublication={_virtualController.Publication.Kind}/{_virtualController.Publication.Revision} monitor={canvas.MonitoringDiagnostic} rows={string.Join(" | ",lines)}");
                    Check(lines.Contains($"Current count: {count}"), "display current count matches executing counter");
                    Check(lines.Contains("Preset / setpoint: 3"), "display preset matches executing counter");
                    Check(lines.Contains("Done: " + (state.Done ? "TRUE" : "FALSE")), "done status survives deenergized count input");
                    Check(lines.Contains("Count input: " + (state.CountInput ? "TRUE" : "FALSE")), "display uses sampled input");
                }
                if (output is not null && count is 0 or 2 or 3)
                    for (var vendor = 0; vendor < 2; vendor++)
                    {
                        tabs.CurrentTab = vendor;
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                        GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output, $"counter-{vendor}-{count}.png"));
                    }
            }
            ResetActiveController();
            foreach (var canvas in canvases)
                Check(Lines(canvas).Contains("Current count: —") && Lines(canvas).Contains("Done: —"), "Reset clears prior instruction observation");
            Check(_virtualController!.Snapshot.Counters["press_count"].Accumulated == 0,
                "Reset final instance state remains zero separately from unavailable instruction observation");
            document.Rungs.Single(r => r.IsCounter).CounterPreset = 5;
            _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));
            foreach (var canvas in canvases)
                Check(!canvas.MonitorActive && Lines(canvas).Contains("Current count: —") && Lines(canvas).Contains("Preset / setpoint: 5"), "changed draft suppresses stale runtime values");
            GD.Print($"COUNTER_MONITOR_RESULT failures=0 checks={checks}");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("COUNTER_MONITOR_RESULT failures=1 " + error);
            GetTree().Quit(1);
        }
    }
}



