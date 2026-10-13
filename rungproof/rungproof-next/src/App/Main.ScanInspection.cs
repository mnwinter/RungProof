using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    // Pause owns only scheduling and autonomous callbacks. It never projects a
    // Stop image: carried loads, coast speed and pending pulses remain intact.
    private readonly Dictionary<Node, Node.ProcessModeEnum> _scanPauseModes = [];
    private long _scanInspectionEpoch;
    private string? _scanInspectionCaptureState;
    private bool _scanInspectionCapturePending;

    private async void PrepareScanInspectionCapture()
    {
        try
        {
            await PrepareScanInspectionReviewCapture(_scanInspectionCaptureState!);
            var dll = System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), ".godot/mono/temp/bin/Debug/RungProof.Next.dll");
            var identity = new
            {
                state = _scanInspectionCaptureState, scene = _currentSceneId, ephemeralQA = true,
                paused = _virtualController!.IsPaused, scan = _virtualController.Snapshot.ScanNumber,
                periodMs = _virtualController.ScanPeriod.TotalMilliseconds, epoch = _scanInspectionEpoch,
                currentDraftMatches = ScanInspectionCurrentDraftMatches(), historyCount = _virtualController.History.Count,
                droppedRows = _virtualController.HistoryDroppedCount,
                historySelection = _scanInspectionCaptureState == "history" ? "first retained historical row" : "none",
                points = CaptureScanInspectionPlantInputs(), history = _virtualController.History,
                dllSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(dll)))
            };
            System.IO.File.WriteAllText(_capturePath + ".scan-inspection.json", System.Text.Json.JsonSerializer.Serialize(identity));
            _scanInspectionCapturePending = false;
            GD.Print($"SCAN_INSPECTION_CAPTURE_READY state={_scanInspectionCaptureState} scene={_currentSceneId} paused={_virtualController.IsPaused}");
        }
        catch (Exception exception)
        {
            GD.PushError($"SCAN_INSPECTION_CAPTURE_FAILED {exception}");
            GetTree().Quit(1);
        }
    }

    private bool ScanInspectionSupported => _simulatorShell is { IsExternalMode: false, IsChallengeDraftActive: false }
        && WorkspaceDriveScanEligible && _sceneRuntime is not null && (_sceneRuntime.HasWorkspaceBeltPreset || _sceneRuntime.HasWorkspaceRollerPreset
            || AuthoredDemoLadderPrograms.ProjectFiles.Any(item => item.SceneId == _currentSceneId));

    private bool ScanInspectionCurrentDraftMatches()
    {
        if (_simulatorShell is null || _virtualProgram is null
            || !_simulatorShell.TryBuildCurrentLadderProgram(out var draft, out _)) return false;
        return LadderProgramJson.Save(_virtualProgram with { WatchVariables = null })
            == LadderProgramJson.Save(draft with { WatchVariables = null });
    }

    private void PublishScanInspectionState()
    {
        var session = _virtualController;
        var supported = ScanInspectionSupported;
        var running = session?.Snapshot.State == VirtualControllerState.Running;
        var matches = supported && running && ScanInspectionCurrentDraftMatches();
        var ready = supported && running && matches;
        var typedHistory = _simulatorShell is { IsExternalMode: false, IsChallengeDraftActive: false }
            && _currentSceneId is "lab-10-04-motor-enum-state" or "lab-10-02-chicken-label-print" or "lab-10-01-drive-alarm-code-string";
        var repeatCycleHistory = _simulatorShell is { IsExternalMode: false, IsChallengeDraftActive: false }
            && _currentSceneId == RepeatCycleSceneId;
        var reason = !supported ? repeatCycleHistory
            ? "Pause/Step unsupported · retained history is read-only"
            : typedHistory
            ? "Read-only retained typed history · Pause/Step unsupported for this lesson"
            : "Unavailable · supported built-in demos and finite workspaces only"
            : !running ? "Run a valid current draft before Pause or Scan"
            : !matches ? "Current draft differs or is invalid · Run to load; Scan is blocked" : "accepted scans only";
        _simulatorShell?.SetScanInspectionState(ready, session?.IsPaused == true,
            $"{_currentSceneId ?? "No scene"} · controller epoch {_scanInspectionEpoch} · {reason}",
            session?.History ?? System.Array.Empty<VirtualControllerAcceptedScan>(), session?.HistoryDroppedCount ?? 0);
    }

    private void PauseActiveController()
    {
        if (!ScanInspectionSupported || _virtualController?.Snapshot.State != VirtualControllerState.Running
            || !ScanInspectionCurrentDraftMatches())
        {
            _simulatorShell?.SetWorkspaceStatus("PAUSE BLOCKED · Run a valid current draft in a supported built-in scene", true);
            PublishScanInspectionState();
            return;
        }
        _virtualController.Pause();
        _demoEnvironment?.SetAmbientMotionEnabled(false);
        // A root lease alone would not freeze descendants set to Always.
        // Preserve every original mode and restore it before any replacement.
        void Hold(Node node)
        {
            _scanPauseModes.TryAdd(node, node.ProcessMode);
            node.ProcessMode = Node.ProcessModeEnum.Disabled;
            foreach (var child in node.GetChildren()) Hold(child);
        }
        if (_scanPauseModes.Count == 0 && _sceneCompositionRoot is not null) Hold(_sceneCompositionRoot);
        PublishScanInspectionState();
        _simulatorShell?.SetWorkspaceStatus("PAUSED · plant and scan clock held · queued operator inputs wait for STEP or RESUME");
    }

    private void ReleaseScanPauseModes()
    {
        foreach (var (node, mode) in _scanPauseModes)
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion()) node.ProcessMode = mode;
        _scanPauseModes.Clear();
    }

    private void StepActiveController()
    {
        if (!PrepareWorkspaceDriveAcceptedScan())
        {
            _simulatorShell?.SetWorkspaceStatus("STEP BLOCKED · drive controller or measured feedback is unavailable", true);
            PublishScanInspectionState();
            return;
        }
        if (!ScanInspectionSupported || _virtualController?.IsPaused != true || _sceneRuntime is null
            || !ScanInspectionCurrentDraftMatches() || _sceneRuntime.HasLatchedEmergencyStop)
        {
            _simulatorShell?.SetWorkspaceStatus("STEP BLOCKED · pause a valid matching built-in draft first; use RUN to load changed code", true);
            PublishScanInspectionState();
            return;
        }
        var accepted = _virtualController.StepOneAcceptedScan(SampleVirtualControllerInputs,
            SampleVirtualControllerNumericInputs, CommitVirtualControllerOutputs,
            CommitVirtualControllerNumericOutputs, AdvanceAcceptedScenePlant,
            SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs, CaptureScanInspectionPlantInputs);
        PublishScanInspectionState();
        _simulatorShell?.SetWorkspaceStatus(accepted == 1
            ? $"PAUSED · one scan accepted · scan {_virtualController.Snapshot.ScanNumber} · {_virtualController.ScanPeriod.TotalMilliseconds:0} ms"
            : "STEP BLOCKED · no scan accepted", accepted != 1);
    }

    private VirtualControllerSignalImage CaptureScanInspectionPlantInputs()
    {
        var bools = new Dictionary<string, bool>(StringComparer.Ordinal);
        var numbers = new Dictionary<string, double>(StringComparer.Ordinal);
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        if (_sceneRuntime is not null)
            foreach (var (name, value) in _sceneRuntime.Points)
            {
                // Declared PC points are actual post-plant signals. PLC-owned
                // points remain in Applied Commands, never measured feedback.
                if (!_sceneRuntime.IsPcOwnedPoint(name)) continue;
                if (value is bool bit) bools[name] = bit;
                else if (value is string text) texts[name] = text;
                else if (value is byte or short or int or long or float or double or decimal)
                    numbers[name] = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            }
        return new(bools, numbers, texts);
    }

    private void RetirePausedWorkspaceForEdit()
    {
        if (_virtualController?.IsPaused != true) return;
        StopActiveController();
        _virtualController.ClearHistory();
        _scanInspectionEpoch++;
        PublishScanInspectionState();
    }

    private bool BlockPausedForceChange()
    {
        if (_virtualController?.IsPaused != true) return false;
        _simulatorShell?.SetVirtualControllerForceError("Force change blocked while paused · Resume or Stop first");
        return true;
    }

    private bool TryQueuePausedSceneAction(System.Text.Json.JsonElement action, string type, string point)
    {
        if (_virtualController is null || _virtualProgram is null || _sceneRuntime is null) return false;
        string binding = point;
        if (type == "start" || _sceneRuntime.IsPlcOwnedPoint(point))
        {
            var stopping = _sceneRuntime.Points.GetValueOrDefault(point) is true;
            if (!action.TryGetProperty(stopping ? "controllerStopBinding" : "controllerStartBinding", out var bound)) return false;
            binding = bound.GetString() ?? string.Empty;
        }
        else if (type is not ("toggle" or "togglePoint" or "pulse")) return false;
        var variable = _virtualProgram.Variables.FirstOrDefault(item => item.Role == PlcVariableRole.Input
            && item.Type == PlcVariableType.Bool && item.Binding == binding);
        if (variable is null) return false;
        if (type is "start" or "pulse" || _sceneRuntime.IsPlcOwnedPoint(point))
            _virtualController.PulseInput(variable.Name);
        else
        {
            var current = _virtualController.TryGetPendingInput(variable.Name, out var pending)
                ? pending : _sceneRuntime.Points.TryGetValue(binding, out var physical) && physical is bool bit
                    ? bit : _virtualController.Snapshot.Variables.GetValueOrDefault(variable.Name);
            _virtualController.SetInput(variable.Name, !current);
        }
        return true;
    }
}

