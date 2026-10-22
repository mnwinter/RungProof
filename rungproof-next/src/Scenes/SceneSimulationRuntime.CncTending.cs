using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;
using NVector3 = System.Numerics.Vector3;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasCncTendingPlant => _definition.TryGetProperty("cncTendingPlant", out var enabled)
        && enabled.ValueKind == JsonValueKind.True;
    private CncTendingPlantModel? _cncTending;
    private PalletRobotMotion? _cncHandling;
    private Node3D? _cncStock;
    private EquipmentMotionController[] _cncAccess = [];
    private EquipmentMotionController? _cncCutSpindle;
    private readonly List<(Node3D Node, Transform3D Closed)> _cncClampMembers = [];
    private ConveyorController? _cncInfeedDrive, _cncOutfeedDrive;
    private static NVector3 CncNumerics(Vector3 point) => new(point.X, point.Y, point.Z);
    private static Vector3 CncGodot(NVector3 point) => new(point.X, point.Y, point.Z);

    private void ResetCncTendingPlant()
    {
        if (!HasCncTendingPlant) return;
        var outputs = new[] { "infeed_run", "outfeed_run", "robot_run", "robot_grip_close", "cnc_access_open", "cnc_clamp_close", "cnc_run" };
        foreach (var name in outputs)
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException($"CNC tending requires PLC BOOL {name}.");
        foreach (var name in new[] { "robot_target_x", "robot_target_y", "robot_target_z" })
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || _pointTypes.GetValueOrDefault(name) != "REAL")
                throw new InvalidOperationException($"CNC tending requires PLC REAL {name}.");
        foreach (var name in new[] { "blank_at_pickup", "robot_at_target", "robot_grip_confirmed", "cnc_access_is_open",
            "cnc_access_is_closed", "cnc_tool_clear", "cnc_clamp_closed", "cnc_clamp_open", "stock_on_vise",
            "stock_on_outfeed", "robot_clear_of_enclosure", "cnc_cut_permitted", "cell_motion_inhibited",
            "machining_complete", "part_at_exit", "cnc_ready" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException($"CNC tending requires PC BOOL {name}.");
        foreach (var name in new[] { "robot_actual_x", "robot_actual_y", "robot_actual_z", "stock_actual_x", "stock_actual_y", "stock_actual_z" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "REAL")
                throw new InvalidOperationException($"CNC tending requires PC REAL {name}.");
        if (_pointOwners.GetValueOrDefault("cycle_complete") != "SIM" || _pointOwners.GetValueOrDefault("status_color") != "SIM")
            throw new InvalidOperationException("CNC completion/color must remain SIM-owned presentation.");
        _cncStock = _sceneRoot.GetNode<Node3D>("cnc_workpiece");
        _cncHandling = _sceneRoot.GetNode<Node3D>("cnc_robot").FindChild("PalletRobotMotion", true, false) as PalletRobotMotion
            ?? throw new InvalidOperationException("CNC tending requires its delivered joint-driven robot.");
        var machine = _sceneRoot.GetNode<Node3D>("cnc_machine");
        var stockBounds = DeliveredEquipmentBounds(_cncStock);
        var bearing = machine.FindChild("WORK_stock_bearing_shoe", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("CNC tending requires the delivered stock bearing shoe.");
        var bearingBounds = bearing.GlobalTransform * bearing.GetAabb();
        var sensor = _sceneRoot.GetNode<Node3D>("cnc_infeed_sensor");
        var lens = sensor.FindChild("TX_lens", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("CNC pickup requires its actual transmitted photoeye beam.");
        var beam = lens.GlobalTransform * lens.GetAabb().GetCenter();
        if (MathF.Abs(stockBounds.Size.X - .4f) > .002f
            || MathF.Abs(bearingBounds.End.Y - CncTendingPlantModel.Vise.Y) > .002f
            || MathF.Abs(bearingBounds.GetCenter().Z - CncTendingPlantModel.Vise.Z) > .002f
            || MathF.Abs(beam.X - CncTendingPlantModel.Pickup.X) > .002f
            || beam.Y < stockBounds.Position.Y || beam.Y > stockBounds.End.Y)
            throw new InvalidOperationException("CNC stock support/pickup model datums must match actual delivered geometry.");
        var mechanisms = Controllers(machine).OfType<EquipmentMotionController>().ToArray();
        _cncAccess = mechanisms.Where(m => m.Name.ToString().StartsWith("CncDoor", StringComparison.Ordinal)
            || m.Name == "CncToolClearanceMotion").ToArray();
        if (_cncAccess.Length != 3) throw new InvalidOperationException("CNC access requires two delivered doors and tool clearance.");
        _cncCutSpindle = mechanisms.Single(m => m.Kind == EquipmentMotionController.MotionKind.ContinuousRotation);
        if (_cncClampMembers.Count == 0)
        {
            var movingJaw = machine.FindChild("WORK_vise_moving_jaw", true, false) as Node3D
                ?? throw new InvalidOperationException("CNC movable jaw missing.");
            // The delivered moving face is the more positive-Z of the two
            // actual jaw faces; avoid inventing imported decimal-name escaping.
            var faces = machine.FindChildren("WORK_vise_jaw_face_*", "", true, false).OfType<Node3D>().ToArray();
            if (faces.Length != 2) throw new InvalidOperationException("CNC vise requires two delivered jaw faces.");
            var movingFace = faces.OrderBy(face => face.GlobalPosition.Z).Last();
            foreach (var member in new[] { movingJaw, movingFace }) _cncClampMembers.Add((member, member.Transform));
        }
        _cncInfeedDrive = Controllers(_sceneRoot.GetNode<Node3D>("cnc_infeed")).OfType<ConveyorController>().Single();
        _cncOutfeedDrive = Controllers(_sceneRoot.GetNode<Node3D>("cnc_outfeed")).OfType<ConveyorController>().Single();
        _cncInfeedDrive.ResetPlantTravel(); _cncOutfeedDrive.ResetPlantTravel();
        _cncTending = new(CncNumerics(_cncHandling.ParkTarget));
        FreezeCncTendingAdapters(); ProjectCncTendingPlant();
    }
    private void FreezeCncTendingAdapters()
    {
        if (!HasCncTendingPlant) return;
        foreach (var mechanism in _cncAccess) mechanism.SetPhysicsProcess(!UsesExternalClock);
        _cncCutSpindle?.SetPhysicsProcess(!UsesExternalClock);
        _cncInfeedDrive?.SetPhysicsProcess(!UsesExternalClock);
        _cncOutfeedDrive?.SetPhysicsProcess(!UsesExternalClock);
    }
    private double CncNumber(string name) => Convert.ToDouble(_points[name], System.Globalization.CultureInfo.InvariantCulture);
    private void AdvanceCncTendingPlant(double seconds)
    {
        var before = _cncTending!.Stock;
        var command = new CncTendingPlantModel.Commands(AsBool(_points["infeed_run"]), AsBool(_points["outfeed_run"]),
            AsBool(_points["robot_run"]), new((float)CncNumber("robot_target_x"), (float)CncNumber("robot_target_y"), (float)CncNumber("robot_target_z")),
            AsBool(_points["robot_grip_close"]), AsBool(_points["cnc_access_open"]), AsBool(_points["cnc_clamp_close"]), AsBool(_points["cnc_run"]));
        _cncTending.Advance(seconds, command, requested =>
            _cncHandling!.Solve(CncGodot(requested)) ? CncNumerics(_cncHandling.ToolTransform.Origin) : null);
        ProjectCncTendingPlant();
        var delta = _cncTending.Stock.X - before.X;
        var inlet = !_cncTending.Gripped && before.X < CncTendingPlantModel.Pickup.X ? Math.Max(0, delta) : 0;
        var outlet = _cncTending.StockOnOutfeed ? Math.Max(0, delta) : 0;
        _cncInfeedDrive!.ApplyPlantTravel(inlet, seconds > 0 ? inlet / (float)seconds : 0);
        _cncOutfeedDrive!.ApplyPlantTravel(outlet, seconds > 0 ? outlet / (float)seconds : 0);
        _cncCutSpindle!.RunCommand = command.Spindle && _cncTending.CutPermitted;
        _cncCutSpindle._PhysicsProcess(seconds);
        ApplyBindings();
        // Generic command presentation may publish the requested spindle state;
        // its autonomous clock remains frozen. Actual rotation is gated above.
        _cncCutSpindle.RunCommand = command.Spindle && _cncTending.CutPermitted;
        StateChanged?.Invoke();
    }
    private void ProjectCncTendingPlant()
    {
        if (_cncTending is null || !UsesExternalClock) return;
        _cncStock!.GlobalPosition = CncGodot(_cncTending.Stock);
        _cncHandling!.ProjectControllerGripper(_cncStock, AsBool(_points["robot_grip_close"]), _cncTending.Gripped);
        foreach (var mechanism in _cncAccess) mechanism.SetPositionNormalized((float)_cncTending.AccessPosition);
        foreach (var (member, closed) in _cncClampMembers)
            member.Transform = closed.Translated(Vector3.Back * (float)(.12 * (1 - _cncTending.ClampPosition)));
        SetPoint("blank_at_pickup", _cncTending.BlankAtPickup);
        SetPoint("stock_actual_x", (double)_cncTending.Stock.X); SetPoint("stock_actual_y", (double)_cncTending.Stock.Y); SetPoint("stock_actual_z", (double)_cncTending.Stock.Z);
        SetPoint("robot_actual_x", (double)_cncTending.Tool.X); SetPoint("robot_actual_y", (double)_cncTending.Tool.Y); SetPoint("robot_actual_z", (double)_cncTending.Tool.Z);
        SetPoint("robot_at_target", _cncTending.AtTarget); SetPoint("robot_grip_confirmed", _cncTending.Gripped);
        SetPoint("cnc_access_is_open", _cncTending.AccessIsOpen); SetPoint("cnc_access_is_closed", _cncTending.AccessIsClosed);
        SetPoint("cnc_tool_clear", _cncTending.AccessIsOpen); SetPoint("cnc_clamp_closed", _cncTending.ClampIsClosed);
        SetPoint("cnc_clamp_open", _cncTending.ClampPosition <= 0); SetPoint("stock_on_vise", _cncTending.StockOnVise);
        SetPoint("stock_on_outfeed", _cncTending.StockOnOutfeed); SetPoint("robot_clear_of_enclosure", _cncTending.RobotClear);
        SetPoint("cnc_cut_permitted", _cncTending.CutPermitted); SetPoint("cell_motion_inhibited", _cncTending.Inhibited);
        SetPoint("machining_complete", _cncTending.MachiningComplete); SetPoint("part_at_exit", _cncTending.CycleComplete);
        SetPoint("cnc_ready", !_cncTending.MachiningComplete && _cncTending.RobotClear && !_cncTending.Gripped);
        SetPoint("cycle_complete", _cncTending.CycleComplete);
        SetPoint("status_color", !PlantPlaybackRunning ? "red" : _cncTending.Inhibited ? "red"
            : AsBool(_points["robot_run"]) || AsBool(_points["cnc_run"]) || AsBool(_points["infeed_run"]) || AsBool(_points["outfeed_run"]) ? "green" : "amber");
    }
}
