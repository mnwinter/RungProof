using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.Scenes;

/// <summary>
/// Symbolic plant ported from Python components.ConveyorPhotoeye/ConveyorPusher
/// and native_runtime reload policy. Commands come from the selected controller.
/// </summary>
public sealed class ConveyorPlantModel
{
    private readonly double _length, _speed, _objectLength, _photoeyePosition, _minimumPhotoeyeOn, _strokeTime, _transferPosition;
    private readonly double? _reloadDelay;
    private readonly double[] _cartonHeightSequenceM;
    private int _cartonHeightIndex;
    private double _photoeyeHold, _reloadElapsed;
    public double? LeadingEdge { get; private set; }
    public bool ObjectPresent => LeadingEdge.HasValue;
    public bool PhotoeyeBlocked { get; private set; }
    public double PusherPosition { get; private set; }
    public bool PusherExtended => PusherPosition >= 1 - 1e-9;
    public bool PusherRetracted => PusherPosition <= 1e-9;
    public long Completed { get; private set; }
    public bool ObjectTransferred { get; private set; }
    public bool ObjectPassedMainLine { get; private set; }
    public bool ObjectLoaded { get; private set; }
    public double CurrentCartonHeightM { get; private set; }
    public string State { get; private set; } = "stopped_loaded";

    public ConveyorPlantModel(double length, double speed, double objectLength, double photoeyePosition,
        double minimumPhotoeyeOn = 0.1, double strokeTime = 0.3, double transferPosition = 0.8,
        double? reloadDelay = null, IReadOnlyList<double>? cartonHeightSequenceM = null)
    {
        foreach (var number in new[] { length, speed, objectLength, photoeyePosition, minimumPhotoeyeOn, strokeTime, transferPosition })
            if (!double.IsFinite(number)) throw new ArgumentException("Conveyor plant parameters must be finite.");
        if (length <= 0 || speed <= 0 || objectLength <= 0 || objectLength > length || photoeyePosition < 0
            || photoeyePosition > length || minimumPhotoeyeOn < 0 || strokeTime <= 0 || transferPosition <= 0
            || transferPosition > 1 || (reloadDelay.HasValue && (!double.IsFinite(reloadDelay.Value) || reloadDelay < 0.05 || reloadDelay > 3600)))
            throw new ArgumentException("Conveyor plant parameters are outside their declared ranges.");
        _cartonHeightSequenceM = cartonHeightSequenceM?.ToArray() ?? [];
        if (_cartonHeightSequenceM.Any(height => !double.IsFinite(height) || height <= 0 || height > 10))
            throw new ArgumentException("Carton height sequence must contain finite heights from 0 to 10 m.", nameof(cartonHeightSequenceM));
        (_length, _speed, _objectLength, _photoeyePosition, _minimumPhotoeyeOn, _strokeTime, _transferPosition, _reloadDelay) =
            (length, speed, objectLength, photoeyePosition, minimumPhotoeyeOn, strokeTime, transferPosition, reloadDelay);
        Reset();
    }

    public void Reset()
    {
        LeadingEdge = 0;
        _cartonHeightIndex = 0;
        CurrentCartonHeightM = _cartonHeightSequenceM.Length == 0 ? 0.72 : _cartonHeightSequenceM[0];
        PusherPosition = 0;
        Completed = 0;
        ObjectTransferred = false;
        ObjectPassedMainLine = false;
        ObjectLoaded = true;
        _photoeyeHold = _reloadElapsed = 0;
        RefreshPhotoeye(0, false);
        State = "stopped_loaded";
    }

    public void Step(double seconds, bool conveyorRun, bool pusherExtend = false, bool hasPusher = false)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentException("Plant step must be finite and non-negative.");
        ObjectTransferred = false;
        ObjectPassedMainLine = false;
        ObjectLoaded = false;
        var previousStroke = PusherPosition;
        if (hasPusher) PusherPosition = Math.Clamp(PusherPosition + (pusherExtend ? 1 : -1) * seconds / _strokeTime, 0, 1);
        var crossedPhotoeye = false;
        if (conveyorRun && LeadingEdge is double previous)
        {
            var current = previous + _speed * seconds;
            crossedPhotoeye = current >= _photoeyePosition && previous - _objectLength <= _photoeyePosition;
            if (current - _objectLength >= _length)
            {
                LeadingEdge = null;
                Completed++;
                ObjectPassedMainLine = true;
            }
            else LeadingEdge = current;
        }
        RefreshPhotoeye(seconds, crossedPhotoeye);
        if (hasPusher && pusherExtend && previousStroke < _transferPosition && PusherPosition >= _transferPosition
            && ObjectPresent && PhotoeyeBlocked)
        {
            LeadingEdge = null;
            _photoeyeHold = 0;
            PhotoeyeBlocked = false;
            ObjectTransferred = true;
            Completed++;
        }
        // Native reload waits for transfer/discharge, delay and retracted limit.
        if (ObjectTransferred) _reloadElapsed = 0;
        else if (!ObjectPresent) _reloadElapsed += seconds;
        if (_reloadDelay.HasValue && !ObjectPresent && PusherRetracted && _reloadElapsed + 1e-9 >= _reloadDelay.Value)
        {
            LeadingEdge = 0;
            if (_cartonHeightSequenceM.Length > 0)
            {
                _cartonHeightIndex = (_cartonHeightIndex + 1) % _cartonHeightSequenceM.Length;
                CurrentCartonHeightM = _cartonHeightSequenceM[_cartonHeightIndex];
            }
            ObjectLoaded = true;
            _reloadElapsed = 0;
            RefreshPhotoeye(0, false);
        }
        State = hasPusher && pusherExtend ? (PusherExtended ? "extended" : "pushing")
            : hasPusher && !PusherRetracted ? "retracting"
            : conveyorRun ? (ObjectPresent ? "running_loaded" : "running_empty")
            : ObjectPresent ? "stopped_loaded" : "stopped_empty";
    }

    private void RefreshPhotoeye(double seconds, bool crossed)
    {
        var blocked = LeadingEdge is double position && position - _objectLength <= _photoeyePosition && _photoeyePosition <= position;
        _photoeyeHold = blocked || crossed ? Math.Max(_photoeyeHold, _minimumPhotoeyeOn) : Math.Max(0, _photoeyeHold - seconds);
        PhotoeyeBlocked = blocked || _photoeyeHold > 0;
    }
}
