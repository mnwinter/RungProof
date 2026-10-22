using System;
using System.Numerics;

namespace RungProof.Next.Scenes;

/// <summary>Single-target robot/actuator mechanics. No phase table and no PLC command generation.</summary>
public sealed class CncTendingPlantModel
{
    public sealed record Commands(bool Infeed, bool Outfeed, bool RobotEnable, Vector3 RobotTarget,
        bool GripClose, bool AccessOpen, bool ClampClose, bool Spindle);
    public static readonly Vector3 Pickup = new(-1.8f, .9f, .55f);
    public static readonly Vector3 Vise = new(0, 1.66f, -1.985f);
    public static readonly Vector3 Outfeed = new(1.8f, .9f, .55f);
    public static readonly Vector3 GripOffset = new(0, .17f, 0);
    private readonly Vector3 _park;
    private Vector3 _gripTool, _gripStock;
    // The delivered IK solver accepts <0.2mm position error. Feedback must
    // not call a pose complete several millimetres before that actual target.
    private const float PoseTolerance = .0003f, ContactTolerance = .0006f;
    public Vector3 Tool { get; private set; }
    public Vector3 Stock { get; private set; } = new(-6.1f, .9f, .55f);
    public double AccessPosition { get; private set; }
    public double ClampPosition { get; private set; }
    public bool Gripped { get; private set; }
    public bool AtTarget { get; private set; }
    public bool Inhibited { get; private set; }
    public double CuttingSeconds { get; private set; }
    public bool MachiningComplete => CuttingSeconds >= 2;
    public bool AccessIsOpen => AccessPosition >= 1;
    public bool AccessIsClosed => AccessPosition <= 0;
    public bool ClampIsClosed => ClampPosition >= 1;
    public bool StockOnVise => !Gripped && Vector3.Distance(Stock, Vise) < .006f;
    public bool StockOnOutfeed => !Gripped && Math.Abs(Stock.Z - Outfeed.Z) < .006 && Math.Abs(Stock.Y - Outfeed.Y) < .006 && Stock.X >= Outfeed.X - .006;
    public bool BlankAtPickup => !Gripped && Math.Abs(Stock.X - Pickup.X) <= .2f
        && Math.Abs(Stock.Y - Pickup.Y) < .006f && Math.Abs(Stock.Z - Pickup.Z) < .006f;
    public bool RobotClear => (Tool.Z >= -1.3f || (Tool.Y >= 2.5f && Tool.Z >= -1.45f))
        && (!Gripped || Stock.Z >= -1.3f);
    public bool CutPermitted => StockOnVise && ClampIsClosed && AccessIsClosed && RobotClear;
    public bool CycleComplete => MachiningComplete && StockOnOutfeed && Stock.X >= 6.2f;
    public CncTendingPlantModel(Vector3 park) { _park = park; Reset(); }
    public void Reset()
    {
        Tool = _park; Stock = new(-6.1f, .9f, .55f); AccessPosition = ClampPosition = CuttingSeconds = 0;
        _gripTool = _gripStock = Vector3.Zero;
        Gripped = AtTarget = Inhibited = false;
    }
    private static double Move(double current, double target, double maximum) => current + Math.Clamp(target - current, -maximum, maximum);
    public void Advance(double seconds, Commands command, Func<Vector3, Vector3?> acceptToolPose)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Inhibited = false;
        if (!float.IsFinite(command.RobotTarget.X) || !float.IsFinite(command.RobotTarget.Y) || !float.IsFinite(command.RobotTarget.Z))
            throw new ArgumentException("Robot target must be finite.");
        if (seconds == 0) return;
        // Access and clamp are independent commanded mechanisms. A spinning
        // request cannot authorize opening access or releasing held stock.
        var spinning = command.Spindle && CutPermitted;
        if (command.AccessOpen && spinning) Inhibited = true;
        else if (!command.AccessOpen && !RobotClear) Inhibited = true;
        else AccessPosition = Move(AccessPosition, command.AccessOpen ? 1 : 0, seconds);
        if (spinning || (command.ClampClose && (!StockOnVise || !RobotClear)))
        { if (command.ClampClose != ClampIsClosed) Inhibited = true; }
        else ClampPosition = Move(ClampPosition, command.ClampClose ? 1 : 0, seconds / .5);
        if (!Gripped && !StockOnVise && !StockOnOutfeed && command.Infeed && Stock.X < Pickup.X)
            Stock = new((float)Math.Min(Pickup.X, Stock.X + .75 * seconds), .9f, .55f);
        if (command.RobotEnable)
        {
            var delta = command.RobotTarget - Tool;
            var next = Tool + (delta.Length() <= .8 * seconds ? delta : Vector3.Normalize(delta) * (float)(.8 * seconds));
            var entersEnclosure = next.Z < -1.45f || (Gripped && next.Z < -1.45f + GripOffset.Z);
            var workspace = Math.Abs(next.X) <= 2.2 && next.Y >= 1.04 && next.Y <= 3.2 && next.Z >= -2.2 && next.Z <= 1;
            if (!workspace || (entersEnclosure && (!AccessIsOpen || spinning))) Inhibited = true;
            else
            {
                var actual = acceptToolPose(next);
                if (actual is null || Vector3.Distance(actual.Value, next) > PoseTolerance) Inhibited = true;
                else
                {
                    Tool = actual.Value;
                    // Preserve the measured attachment transform. Replacing
                    // it with nominal GripOffset on the next scan snapped a
                    // near-target stock upward even without tool movement.
                    if (Gripped) Stock = _gripStock + (Tool - _gripTool);
                }
            }
            if (command.GripClose && !Gripped)
            {
                if (Vector3.Distance(Tool, Stock + GripOffset) < ContactTolerance && !(StockOnVise && ClampPosition > 0))
                {
                    _gripTool = Tool; _gripStock = Stock; Gripped = true;
                }
                else Inhibited = true;
            }
            else if (!command.GripClose && Gripped)
            {
                if (Vector3.Distance(Stock, Vise) < ContactTolerance || Vector3.Distance(Stock, Outfeed) < ContactTolerance)
                {
                    // Resolve only submillimetre support contact onto the
                    // measured bearing plane; never permit a free-space drop.
                    Stock = Vector3.Distance(Stock, Vise) < ContactTolerance ? Vise : Outfeed;
                    Gripped = false;
                }
                else Inhibited = true; // Unsupported releases never teleport the load.
            }
        }
        AtTarget = Vector3.Distance(Tool, command.RobotTarget) < PoseTolerance;
        if (command.Spindle)
        {
            if (CutPermitted) CuttingSeconds = Math.Min(2, CuttingSeconds + seconds);
            else Inhibited = true;
        }
        if (command.Outfeed && StockOnOutfeed) Stock = new((float)Math.Min(6.2, Stock.X + .75 * seconds), .9f, .55f);
    }
}
