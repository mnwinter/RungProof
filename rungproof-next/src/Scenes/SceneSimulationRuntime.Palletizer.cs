using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasPalletizerPlant => _definition.TryGetProperty("palletizerPlant", out _);
    private PalletizerPlantModel? _palletizer;
    private Node3D? _palletizerBox, _palletizerBridge, _palletizerCarriage, _palletizerRod, _palletizerTool;
    private readonly Dictionary<Node3D, Transform3D> _palletizerAuthored = new();
    private Node3D[] _placedCartons = Array.Empty<Node3D>();
    private void ResetPalletizerPlant()
    {
        if (!HasPalletizerPlant) return;
        if (Text(_definition.GetProperty("palletizerPlant"), "model", "") != "four-carton-layer-v1")
            throw new InvalidOperationException("Unknown palletizer plant model.");
        foreach (var point in new[] { "system_enable", "start_command", "stop_command", "reset_command", "safety_trip",
                     "pallet_position_valid", "carton_at_pick", "gantry_home", "at_pickup", "at_place",
                     "carton_attached", "pick_complete", "cycle_in_progress", "palletizer_fault" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Palletizer requires PC-owned BOOL '{point}'.");
        foreach (var point in new[] { "gantry_x_positive", "gantry_x_negative", "gantry_y_positive", "gantry_y_negative",
                     "gantry_z_positive", "gantry_z_negative", "vacuum_pick", "palletizer_reset", "cell_active", "layer_complete" })
            if (_pointOwners.GetValueOrDefault(point) != "PLC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Palletizer requires PLC-owned BOOL '{point}'.");
        foreach (var point in new[] { "placed_cartons", "gantry_x_encoder_counts", "gantry_y_encoder_counts", "gantry_z_encoder_counts" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "DINT")
                throw new InvalidOperationException($"Palletizer requires PC-owned DINT '{point}'.");
        _palletizer ??= new(); _palletizer.Reset();
        _palletizerBox = _sceneRoot.GetNode<Node3D>("box_2");
        var gantry = _sceneRoot.GetNode<Node3D>("training_accessory_4");
        Node3D Part(string prefix) => gantry.FindChildren(prefix + "*", "", true, false).OfType<Node3D>().Single();
        _palletizerBridge = Part("KIN_X_BRIDGE"); _palletizerCarriage = Part("KIN_Y_CARRIAGE");
        _palletizerRod = Part("KIN_Z_AXIS"); _palletizerTool = Part("GANTRY_GRIPPER");
        if (_palletizerAuthored.Count == 0)
            foreach (var part in new[] { _palletizerBridge, _palletizerCarriage, _palletizerRod, _palletizerTool }) _palletizerAuthored[part] = part.Transform;
        if (_placedCartons.Length == 0)
        {
            _placedCartons = Enumerable.Range(0, 4).Select(index =>
            {
                var copy = (Node3D)_palletizerBox.Duplicate(); copy.Name = $"PlacedCarton_{index}";
                _sceneRoot.AddChild(copy); return copy;
            }).ToArray();
        }
        ProjectPalletizer();
    }
    private void PausePalletizerClock()
    {
        if (_palletizer is null) return;
        // A Start pressed before Stop but not yet sampled must not survive
        // playback pause and fire on a later Run.
        SetPoint("start_command", false); _pulsePoints.Remove("start_command");
        ProjectPalletizer(); ApplyBindings();
    }
    private bool LoadPalletizerCarton()
    {
        if (_palletizer?.LoadCarton() != true) return false;
        ProjectPalletizer(); ApplyBindings(); StateChanged?.Invoke(); return true;
    }
    private void AdvancePalletizerPlant(double seconds)
    {
        if (_palletizer is null) return;
        var fault = _palletizer.Faulted;
        _palletizer.Step(seconds,
            AsBool(_points["gantry_x_positive"]), AsBool(_points["gantry_x_negative"]),
            AsBool(_points["gantry_y_positive"]), AsBool(_points["gantry_y_negative"]),
            AsBool(_points["gantry_z_positive"]), AsBool(_points["gantry_z_negative"]),
            AsBool(_points["vacuum_pick"]), AsBool(_points["pallet_position_valid"]),
            AsBool(_points["palletizer_reset"]));
        ProjectPalletizer(); ApplyBindings(); StateChanged?.Invoke();
        if (!fault && _palletizer.Faulted) GD.Print($"PALLETIZER_FAULT {_palletizer.FaultReason}");
    }
    private void ProjectPalletizer()
    {
        if (_palletizer is null) return;
        var x = (float)_palletizer.X; var z = (float)_palletizer.Z; var y = (float)_palletizer.ToolY;
        var delta = new Vector3(x, 0, z - .1f);
        foreach (var part in new[] { _palletizerBridge!, _palletizerCarriage!, _palletizerTool! })
        {
            var authored = _palletizerAuthored[part];
            var offset = part == _palletizerBridge ? new Vector3(x, 0, 0) : delta;
            if (part == _palletizerTool) offset.Y = y - .67f;
            part.Transform = new(authored.Basis, authored.Origin + offset);
        }
        // This scene opts into a telescoping rod, fixed in the carriage and
        // seated 25 mm into the tool. Never modify the shared delivered asset.
        var rod = _palletizerAuthored[_palletizerRod!];
        var bottom = y + .175f; var height = 2.395f - bottom;
        _palletizerRod!.Transform = new(Basis.FromScale(new(1, height / 1.55f, 1)) * rod.Basis,
            rod.Origin + delta + new Vector3(0, (bottom + 2.395f) / 2 - 1.62f, 0));
        _palletizerBox!.Visible = _palletizer.CartonAtPick || _palletizer.CartonFeeding || _palletizer.Attached;
        _palletizerBox.Position = _palletizer.Attached
            ? new(x, y - .4f, z)
            : new((float)(_palletizer.CartonFeeding ? _palletizer.CartonFeedX : PalletizerPlantModel.PickX),
                1.055f, -.4f);
        for (var index = 0; index < 4; index++)
        {
            var slot = PalletizerPlantModel.Slot(index); _placedCartons[index].Visible = index < _palletizer.Placed;
            _placedCartons[index].Position = new((float)slot.X, .4505f, (float)slot.Z);
        }
        SetPoint("carton_at_pick", _palletizer.CartonAtPick); SetPoint("gantry_home", _palletizer.Home);
        SetPoint("at_pickup", _palletizer.AtPickup); SetPoint("at_place", _palletizer.AtPlace);
        SetPoint("carton_attached", _palletizer.Attached); SetPoint("pick_complete", _palletizer.PickComplete);
        SetPoint("cycle_in_progress", _palletizer.InProgress); SetPoint("palletizer_fault", _palletizer.Faulted);
        SetPoint("placed_cartons", _palletizer.Placed);
        // Simulated incremental feedback at 10 counts/mm, zeroed at the
        // pickup/home pose. These counts observe plant position; they do not
        // command motion or model a physical encoder/servo drive.
        static int Counts(double metres) => checked((int)Math.Round(metres * 10_000, MidpointRounding.AwayFromZero));
        SetPoint("gantry_x_encoder_counts", Counts(_palletizer.X - PalletizerPlantModel.PickX));
        SetPoint("gantry_y_encoder_counts", Counts(_palletizer.ToolY - PalletizerPlantModel.CarryY));
        SetPoint("gantry_z_encoder_counts", Counts(_palletizer.Z - PalletizerPlantModel.PickZ));
    }
}
