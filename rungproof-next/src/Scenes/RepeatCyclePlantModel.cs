using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// One commanded CNC dry stroke: 100 mm feed, 0.5 s dwell, then return home.
/// This is prescribed motion, not material removal, machining dynamics or OEM logic.
/// The PLC owns the batch counter; this plant reports only actual cycle completion.
/// </summary>
public sealed class RepeatCyclePlantModel
{
    public const double TravelM = .1, FeedSeconds = 1, DwellSeconds = .5, ReturnSeconds = 1;
    public const double SpindleRpm = 3200;
    public enum CyclePhase { Idle, Feed, Dwell, Return, Complete }
    public CyclePhase Phase { get; private set; }
    public double HomeFraction { get; private set; } = 1;
    public double SpindleRadians { get; private set; }
    public bool Home => HomeFraction >= 1 - 1e-8;
    public bool Busy => Phase is CyclePhase.Feed or CyclePhase.Dwell or CyclePhase.Return;
    public bool Done => Phase == CyclePhase.Complete;
    private double _dwell;

    public void Reset()
    { Phase = CyclePhase.Idle; HomeFraction = 1; SpindleRadians = _dwell = 0; }

    public void Step(double seconds, bool command)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Repeat-cycle time must be finite and non-negative.");
        if (seconds == 0) return;
        // Stop/permissive loss holds a partial stroke. Completion is retained
        // until a sampled low command, so a held high cannot start a fourth cycle.
        if (!command)
        { if (Done) Phase = CyclePhase.Idle; return; }
        if (Done) return;
        if (Phase == CyclePhase.Idle) Phase = CyclePhase.Feed;
        var remaining = seconds;
        while (remaining > 1e-10 && !Done)
        {
            var tick = Math.Min(.01, remaining);
            SpindleRadians = (SpindleRadians + tick * SpindleRpm * Math.PI / 30) % (2 * Math.PI);
            switch (Phase)
            {
                case CyclePhase.Feed:
                    HomeFraction = Math.Max(0, HomeFraction - tick / FeedSeconds);
                    if (HomeFraction <= 1e-8) { HomeFraction = 0; Phase = CyclePhase.Dwell; }
                    break;
                case CyclePhase.Dwell:
                    _dwell += tick;
                    if (_dwell >= DwellSeconds - 1e-8) { _dwell = 0; Phase = CyclePhase.Return; }
                    break;
                case CyclePhase.Return:
                    HomeFraction = Math.Min(1, HomeFraction + tick / ReturnSeconds);
                    if (Home) { HomeFraction = 1; Phase = CyclePhase.Complete; }
                    break;
            }
            remaining -= tick;
        }
    }
}
