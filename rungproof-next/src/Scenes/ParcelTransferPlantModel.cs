using System;
using System.Linq;

namespace RungProof.Next.Scenes;

/// <summary>Three rigid loads, independent belt drives, escapement and commanded rotary actuator.</summary>
public sealed class ParcelTransferPlantModel
{
    public sealed class Parcel
    {
        public double X, Z, Angle;
        public readonly double HalfLength, HalfWidth, Height;
        public int Route = -1;
        public double Outbound;
        public bool Arrived;
        public Parcel(double x, double halfLength, double halfWidth, double height)
            => (X, HalfLength, HalfWidth, Height) = (x, halfLength, halfWidth, height);
    }
    private readonly double[] _homes;
    private readonly double _speed, _gateX, _sensorX, _tableRadius, _turnSeconds;
    private readonly double[] _classBeamHeights, _receivingDistances;
    private bool _lastGate, _awaitGatePass;
    public Parcel[] Parcels { get; }
    public Func<Parcel, bool>? Supported { get; set; }
    public double TablePosition { get; private set; }
    public bool GateClear { get; private set; }
    public bool GateOpen { get; private set; }
    public bool TableAligned { get; private set; }
    public bool Inhibited { get; private set; }
    public bool TableLoaded => Parcels.Any(p => p.Route < 0 && p.X * p.X + p.Z * p.Z <= .025 * .025);
    public bool TableClear => Parcels.All(p => p.Route < 0 ? p.X + p.HalfLength < -_tableRadius
        : p.Outbound - p.HalfLength > _tableRadius);
    public int SortedCount => Parcels.Count(p => p.Arrived);
    public bool Misroute => Parcels.Where(p => p.Arrived).Any(p => p.Route != ClassFor(p) - 1);
    public bool Complete => SortedCount == Parcels.Length && !Misroute;
    public int DetectedClass => Parcels.Where(p => p.Route < 0 && p.X - p.HalfLength <= _sensorX + 1e-9
        && p.X + p.HalfLength >= _sensorX - 1e-9).Select(ClassFor).FirstOrDefault();
    private int ClassFor(Parcel p) => p.Height >= _classBeamHeights[2] ? 1 : p.Height >= _classBeamHeights[1] ? 2 : p.Height >= _classBeamHeights[0] ? 3 : 0;

    public ParcelTransferPlantModel(double[] homes, double[] lengths, double[] widths, double[] heights,
        double speed, double gateX, double sensorX, double tableRadius, double fullTurnSeconds,
        double[] classBeamHeights, double[] receivingDistances)
    {
        if (classBeamHeights.Length != 3 || receivingDistances.Length != 3 || homes.Length != 3 || lengths.Length != 3 || widths.Length != 3 || heights.Length != 3
            || !homes.Concat(lengths).Concat(widths).Concat(heights).Append(speed).Append(gateX)
                .Append(sensorX).Append(tableRadius).Append(fullTurnSeconds).All(double.IsFinite)
            || !classBeamHeights.Concat(receivingDistances).All(double.IsFinite)
            || receivingDistances.Any(x => x <= tableRadius)
            || classBeamHeights[0] <= 0 || classBeamHeights[1] <= classBeamHeights[0] || classBeamHeights[2] <= classBeamHeights[1]
            || lengths.Any(x => x <= 0) || widths.Any(x => x <= 0) || heights.Any(x => x <= 0)
            || speed <= 0 || tableRadius <= 0 || fullTurnSeconds <= 0 || gateX >= -tableRadius)
            throw new ArgumentException("Invalid parcel transfer geometry.");
        _classBeamHeights = (double[])classBeamHeights.Clone();
        _receivingDistances = (double[])receivingDistances.Clone();
        _homes = (double[])homes.Clone();
        Parcels = homes.Select((x, i) => new Parcel(x, lengths[i] / 2, widths[i] / 2, heights[i])).ToArray();
        (_speed, _gateX, _sensorX, _tableRadius, _turnSeconds) = (speed, gateX, sensorX, tableRadius, fullTurnSeconds);
    }
    public void Reset()
    {
        for (var i = 0; i < Parcels.Length; i++)
        { var p = Parcels[i]; p.X = _homes[i]; p.Z = p.Angle = p.Outbound = 0; p.Route = -1; p.Arrived = false; }
        TablePosition = 0; GateOpen = GateClear = TableAligned = Inhibited = _lastGate = _awaitGatePass = false;
    }
    public void Advance(double seconds, bool enable, bool infeed, bool transfer, bool gateOpen, double routeTarget)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Inhibited = false;
        if (gateOpen && !_lastGate) { GateClear = false; _awaitGatePass = true; }
        _lastGate = gateOpen;
        var gateOccupied = Parcels.Any(p => p.Route < 0 && p.X + p.HalfLength > _gateX
            && p.X - p.HalfLength < _gateX + .015);
        GateOpen = gateOpen || gateOccupied;
        if (!gateOpen && gateOccupied) Inhibited = true;
        var validTarget = double.IsFinite(routeTarget) && routeTarget >= 0 && routeTarget <= 1;
        var canIndex = !transfer && (TableLoaded || TableClear);
        if (enable && validTarget && canIndex)
        {
            var old = TablePosition;
            TablePosition += Math.Clamp(routeTarget - TablePosition, -seconds / _turnSeconds, seconds / _turnSeconds);
            var rotation = (TablePosition - old) * Math.PI / 2;
            foreach (var p in Parcels.Where(p => p.Route < 0 && p.X * p.X + p.Z * p.Z <= .025 * .025))
            {
                var x = p.X; var z = p.Z;
                p.X = x * Math.Cos(rotation) + z * Math.Sin(rotation);
                p.Z = -x * Math.Sin(rotation) + z * Math.Cos(rotation);
                p.Angle += rotation;
            }
        }
        TableAligned = validTarget && Math.Abs(TablePosition - routeTarget) < 1e-8;
        if (!enable) return;
        // Front-to-back contact prevents interpenetration. A closed, visible
        // escapement physically arrests the following load; no class-based queue.
        foreach (var p in Parcels.OrderByDescending(p => p.Route >= 0 ? double.MaxValue : p.X))
        {
            if (p.Arrived) continue;
            var beforeX = p.X; var beforeZ = p.Z; var beforeDistance = p.Outbound;
            if (p.Route >= 0)
            {
                if (p.Outbound - p.HalfLength <= _tableRadius && !transfer) continue;
                p.Outbound += _speed * seconds;
                var end = _receivingDistances[p.Route];
                p.Outbound = Math.Min(end, p.Outbound);
                var angle = p.Route * Math.PI / 4;
                p.X = Math.Cos(angle) * p.Outbound; p.Z = -Math.Sin(angle) * p.Outbound;
                if (Supported?.Invoke(p) == false)
                { p.X = beforeX; p.Z = beforeZ; p.Outbound = beforeDistance; Inhibited = true; }
                p.Arrived = p.Outbound >= end - 1e-9;
                continue;
            }
            var onTable = p.X >= -_tableRadius;
            if (!(onTable ? transfer : infeed)) continue;
            if (onTable && p.X * p.X + p.Z * p.Z <= .025 * .025 && transfer)
            {
                var route = (int)Math.Round(TablePosition * 2);
                if (!TableAligned || Math.Abs(TablePosition * 2 - route) > .001)
                { Inhibited = true; continue; }
                p.Route = route; p.Outbound = p.X * Math.Cos(route * Math.PI / 4) - p.Z * Math.Sin(route * Math.PI / 4); p.Angle = route * Math.PI / 4;
                continue;
            }
            // Infeed enters the primary only at its straight-through datum.
            if (p.X < 0 && Math.Abs(TablePosition) > .001)
            { Inhibited = true; continue; }
            var next = p.X + _speed * seconds;
            if (!GateOpen && p.X + p.HalfLength <= _gateX + 1e-8)
                next = Math.Min(next, _gateX - p.HalfLength);
            var ahead = Parcels.Where(q => q != p && q.Route < 0 && q.X > p.X).OrderBy(q => q.X).FirstOrDefault();
            if (ahead is not null) next = Math.Min(next, ahead.X - ahead.HalfLength - p.HalfLength - .02);
            p.X = next;
            if (Supported?.Invoke(p) == false) { p.X = beforeX; p.Z = beforeZ; Inhibited = true; }
            if (_awaitGatePass && p.X - p.HalfLength > _gateX + .015)
            { GateClear = true; _awaitGatePass = false; }
        }
    }
}
