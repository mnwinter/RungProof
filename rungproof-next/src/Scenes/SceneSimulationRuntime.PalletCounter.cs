using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private JsonElement _palletCounterVisualDefinition;
    private Node3D? _palletCounterVisualCarton;
    private Vector3 _palletCounterVisualStart;
    private Vector3 _palletCounterVisualEnd;
    private double _palletCounterVisualTravelSeconds;
    private double _palletCounterVisualElapsed;
    private int _palletCounterVisualQueue;
    private bool _palletCounterVisualActive;

    private bool HasPalletCounterVisual =>
        _palletCounterVisualDefinition.ValueKind == JsonValueKind.Object;

    private void InitializePalletCounterVisual()
    {
        if (!_definition.TryGetProperty("palletCounterVisual", out var visual)
            || visual.ValueKind != JsonValueKind.Object)
            return;

        var equipmentId = SafeNodeName(Text(visual, "equipmentId", string.Empty));
        _palletCounterVisualCarton = _sceneRoot.GetNodeOrNull<Node3D>(equipmentId)
            ?? throw new InvalidOperationException(
                $"Pallet-counter visual requires equipment '{equipmentId}'.");
        var authored = _palletCounterVisualCarton.Position;
        _palletCounterVisualStart = new Vector3(
            (float)Number(visual, "startX", authored.X), authored.Y, authored.Z);
        _palletCounterVisualEnd = new Vector3(
            (float)Number(visual, "endX", authored.X), authored.Y, authored.Z);
        _palletCounterVisualTravelSeconds = Number(visual, "travelSeconds", 3.2);
        if (!double.IsFinite(_palletCounterVisualTravelSeconds)
            || _palletCounterVisualTravelSeconds <= 0
            || _palletCounterVisualEnd.X <= _palletCounterVisualStart.X)
            throw new InvalidOperationException(
                "Pallet-counter visual requires positive travel time and increasing X travel.");
        _palletCounterVisualDefinition = visual.Clone();
    }

    private void AcceptPalletCounterVisualPulse(string point)
    {
        if (!HasPalletCounterVisual) return;
        if (point == Text(_palletCounterVisualDefinition, "resetPoint", "reset_pressed"))
        {
            ResetPalletCounterVisual();
            return;
        }
        if (point != Text(_palletCounterVisualDefinition, "eventPoint", "pallet_detected")) return;

        // The pulse represents a physical pallet arriving at the infeed. Queue
        // every event so repeated learner presses never make cartons vanish;
        // the PLC-owned run output determines when material can actually move.
        _palletCounterVisualQueue++;
        if (!_palletCounterVisualActive)
        {
            _palletCounterVisualElapsed = 0;
            _palletCounterVisualCarton!.Position = _palletCounterVisualStart;
        }
    }

    private void AdvancePalletCounterVisual(double seconds)
    {
        if (!HasPalletCounterVisual || _palletCounterVisualCarton is null
            || !double.IsFinite(seconds) || seconds <= 0)
            return;

        var runPoint = Text(_palletCounterVisualDefinition, "runPoint", "machine_running");
        if (!AsBool(_points.GetValueOrDefault(runPoint)) || _palletCounterVisualQueue == 0)
            return;

        _palletCounterVisualActive = true;
        _palletCounterVisualElapsed = Math.Min(
            _palletCounterVisualTravelSeconds,
            _palletCounterVisualElapsed + seconds);
        var progress = (float)(_palletCounterVisualElapsed / _palletCounterVisualTravelSeconds);
        _palletCounterVisualCarton.Position = _palletCounterVisualStart.Lerp(
            _palletCounterVisualEnd, Mathf.SmoothStep(0, 1, progress));

        if (_palletCounterVisualElapsed + 1e-9 < _palletCounterVisualTravelSeconds) return;

        _palletCounterVisualQueue--;
        _palletCounterVisualActive = false;
        _palletCounterVisualElapsed = 0;
        // Recycle the teaching carton at the off-screen infeed. A queued event
        // begins on the next accepted scan, producing discrete pallet passes.
        _palletCounterVisualCarton.Position = _palletCounterVisualStart;
    }

    private void ResetPalletCounterVisual()
    {
        if (!HasPalletCounterVisual || _palletCounterVisualCarton is null) return;
        _palletCounterVisualQueue = 0;
        _palletCounterVisualActive = false;
        _palletCounterVisualElapsed = 0;
        _palletCounterVisualCarton.Position = _palletCounterVisualStart;
    }
}
