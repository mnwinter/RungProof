using System;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasWastewaterPlant => RuntimeType == "wastewaterCollection";
    private WastewaterPlantModel? _wastewater;
    private Node3D[] _wastewaterTanks = [];
    private EquipmentMotionController? _wastewaterValve;
    private Label3D? _wastewaterReadout;

    private void ResetWastewaterPlant()
    {
        if (!HasWastewaterPlant) return;
        _wastewater = new WastewaterPlantModel(Number(_definition, "initialLevel", .35),
            Number(_definition, "inflowFractionPerSecond", .04), Number(_definition, "transferFractionPerSecond", .08),
            Number(_definition, "highOnFraction", .65), Number(_definition, "highOffFraction", .20));
        _wastewaterTanks = new[] { "tank_0", "tank_1", "tank_2", "training_accessory_6" }
            .Select(id => _sceneRoot.GetNode<Node3D>(id)).ToArray();
        _wastewaterValve = _sceneRoot.GetNode<Node3D>("valve_4").FindChildren("*", string.Empty, true, false)
            .OfType<EquipmentMotionController>().Single();
        _wastewaterReadout = _sceneRoot.GetNode<Node3D>("training_accessory_7").GetNodeOrNull<Label3D>("WastewaterReadout");
        if (_wastewaterReadout is null)
        {
            _wastewaterReadout = new Label3D { Name = "WastewaterReadout",
                FontSize = 40, PixelSize = .003f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled };
            _sceneRoot.GetNode<Node3D>("training_accessory_7").AddChild(_wastewaterReadout);
        }
        // The probe's imported head extends above its installation origin.
        // Place all three text rows above its actual installed mesh envelope,
        // rather than inside the head with an arbitrary local-Y offset.
        var sensor = _sceneRoot.GetNode<Node3D>("training_accessory_7");
        var sensorBounds = DeliveredEquipmentBounds(sensor);
        _wastewaterReadout.Position = sensor.GlobalTransform.AffineInverse() * new Vector3(
            sensorBounds.GetCenter().X, sensorBounds.End.Y + .30f, sensorBounds.GetCenter().Z);
        PublishWastewaterSnapshot();
    }

    private void AdvanceWastewaterPlant(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!PlantPlaybackRunning) return;
        // Read the delivered valve adapter's actual installed opening, rather
        // than declaring the commanded valve position to be instant feedback.
        var remaining = seconds;
        while (remaining > 0)
        {
            var tick = Math.Min(remaining, .02);
            _wastewater!.Advance(tick, AsBool(_points["inflow_enabled"]),
                AsBool(_points["transfer_pump_run"]), _wastewaterValve!.InputPositionNormalized,
                AsBool(_points["treatment_ready"]), AsBool(_points["outlet_clear"]));
            remaining -= tick;
        }
        PublishWastewaterSnapshot();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void PauseWastewaterPlant()
    {
        if (_wastewater is null) return;
        _wastewater.Pause();
        PublishWastewaterSnapshot();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void ProjectWastewaterValveFeedback()
    {
        if (!HasWastewaterPlant || _wastewaterValve is null) return;
        // Absolute bindings may change the installed valve during Reset, Stop
        // or output commit without a plant tick. Publish after those bindings
        // so feedback describes the current adapter, including inversion.
        SetPoint("outlet_valve_position_percent", _wastewaterValve.InputPositionNormalized * 100.0);
    }

    private void PublishWastewaterSnapshot()
    {
        var state = _wastewater!.State;
        SetPoint("source_level_high", state.High);
        SetPoint("source_level_percent", state.LevelPercent);
        SetPoint("source_level_ma", state.TransmitterMa);
        SetPoint("source_empty", state.Empty);
        SetPoint("source_full", state.Full);
        SetPoint("actual_inflow_percent_per_second", state.Inflow * 100);
        SetPoint("actual_transfer_percent_per_second", state.TransferFlow * 100);
        SetPoint("overflow_percent_per_second", state.Overflow * 100);
        SetPoint("transfer_inhibited", state.TransferInhibited);
        ProjectWastewaterValveFeedback();
        foreach (var tank in _wastewaterTanks) ApplyTankLevel(tank, (float)state.Level);
        _wastewaterReadout!.Text = FormattableString.Invariant(
            $"BANK {state.LevelPercent:F1}%  |  {state.TransmitterMa:F2} mA\nTRANSFER {state.TransferFlow * 100:F1}%/s\nOVERFLOW {state.Overflow * 100:F1}%/s");
    }
}
