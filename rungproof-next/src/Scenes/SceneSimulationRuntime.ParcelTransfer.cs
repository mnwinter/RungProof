using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasParcelTransferPlant => _definition.TryGetProperty("parcelTransferPlant", out var value) && value.ValueKind == JsonValueKind.True;
    private ParcelTransferPlantModel? _parcelTransfer;
    private Node3D[] _parcelLoads = [];
    private float[] _parcelHeights = [];
    private ConveyorController[] _parcelBelts = [];
    private EquipmentMotionController[] _parcelTables = [];
    private MeshInstance3D[] _parcelSizeBeams = [];
    private MeshInstance3D? _parcelEscapement;
    private double _parcelGateX;
    private MeshInstance3D[] _parcelSupportBelts = [];
    private Node3D[] _parcelTableDrives = [];
    private readonly List<(MeshInstance3D Mesh, float HomeX, int Table)> _parcelTableCleats = [];
    private readonly double[] _parcelTablePhases = [0, 0];

    private void ResetParcelTransferPlant()
    {
        if (!HasParcelTransferPlant) return;
        foreach (var name in new[] { "conveyors_run", "infeed_run", "table_transfer_run", "infeed_gate_open" })
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException("Parcel transfer requires PLC BOOL " + name);
        if (_pointOwners.GetValueOrDefault("route_position") != "PLC" || _pointTypes.GetValueOrDefault("route_position") != "REAL")
            throw new InvalidOperationException("Parcel transfer requires PLC REAL route_position.");
        foreach (var name in new[] { "primary_table_loaded", "primary_table_clear", "primary_table_aligned", "infeed_gate_clear",
            "transfer_inhibited", "misroute_detected", "all_parcels_received", "large_detected", "medium_detected", "small_detected" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException("Parcel transfer requires PC BOOL " + name);
        _parcelLoads = new[] { "large_parcel", "medium_parcel", "small_parcel" }.Select(id => _sceneRoot.GetNode<Node3D>(id)).ToArray();
        var loads = _parcelLoads.Select(DeliveredEquipmentBounds).ToArray();
        if (loads.Any(b => Math.Sqrt(b.Size.X * b.Size.X + b.Size.Z * b.Size.Z) / 2 > 1.8))
            throw new InvalidOperationException("Delivered parcel must fit on the primary rotary deck.");
        _parcelHeights = _parcelLoads.Select(p => p.Position.Y).ToArray();
        _parcelBelts = new[] { "sort_infeed", "large_lane", "medium_lane", "small_lane", "medium_transfer" }
            .Select(id => _sceneRoot.GetNode<Node3D>(id).FindChildren("*", "", true, false).OfType<ConveyorController>().Single()).ToArray();
        _parcelSupportBelts = new[] { "sort_infeed", "large_lane", "medium_lane", "small_lane", "medium_transfer" }
            .Select(id => (MeshInstance3D)_sceneRoot.GetNode<Node3D>(id).FindChild("KIN_belt_surface", true, false)).ToArray();
        _parcelTables = new[] { "sort_turntable_a", "sort_turntable_b" }.Select(id => _sceneRoot.GetNode<Node3D>(id)
            .FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single()).ToArray();
        foreach (var table in _parcelTables) table.PositionInputSlewSeconds = 0; // Pure accepted-tick actuator owns slew.
        var sensor = _sceneRoot.GetNode<Node3D>("size_sensor_bank");
        _parcelSizeBeams = Enumerable.Range(1, 3).Select(i => (MeshInstance3D)sensor.FindChild($"KIN_beam_size_{i}", true, false)).ToArray();
        var heights = _parcelSizeBeams.Select(b => (double)(b.GlobalTransform * b.GetAabb().GetCenter()).Y - loads[0].Position.Y).ToArray();
        var receive = new[] { "large_lane", "medium_lane", "small_lane" }.Select(id =>
        {
            var pos = _sceneRoot.GetNode<Node3D>(id).Position;
            return Math.Sqrt(pos.X * pos.X + pos.Z * pos.Z);
        }).ToArray();
        _parcelGateX = Number(_definition, "parcelGateX", -2.7);
        _parcelTransfer = new(_parcelLoads.Select(p => (double)p.Position.X).ToArray(), loads.Select(b => (double)b.Size.X).ToArray(),
            loads.Select(b => (double)b.Size.Z).ToArray(), loads.Select(b => (double)b.Size.Y).ToArray(),
            Number(_definition, "conveyorSpeedMps", .9), _parcelGateX, sensor.Position.X,
            Number(_definition, "parcelTableRadius", 1.8), Number(_definition, "parcelFullTurnSeconds", 1.2), heights, receive);
        _parcelTransfer.Supported = ParcelHasDeliveredSupport;
        foreach (var belt in _parcelBelts) belt.ResetPlantTravel();
        CreateParcelEscapement(); CreateParcelTableDrives();
        _parcelTablePhases[0] = _parcelTablePhases[1] = 0;
        FreezeParcelTransferAdapters(); ProjectParcelTransferPlant();
    }
    private void CreateParcelTableDrives()
    {
        if (_parcelTableDrives.Length > 0) return;
        var rubber = new StandardMaterial3D { AlbedoColor = new Color(.035f, .045f, .05f), Roughness = .9f };
        var stripe = new StandardMaterial3D { AlbedoColor = new Color(.45f, .5f, .52f), Metallic = .6f };
        _parcelTableDrives = new Node3D[2];
        for (var table = 0; table < 2; table++)
        {
            var deck = _sceneRoot.GetNode<Node3D>(table == 0 ? "sort_turntable_a" : "sort_turntable_b");
            var drive = new Node3D { Name = $"ParcelTableTransferDrive{table}", Position = deck.Position + Vector3.Up * 1.056f };
            _sceneRoot.AddChild(drive); _parcelTableDrives[table] = drive;
            var length = table == 0 ? 3.2f : 2.6f;
            foreach (var side in new[] { -.32f, .32f })
            {
                drive.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(length, .006f, .32f) },
                    Position = new Vector3(0, 0, side), MaterialOverride = rubber });
                for (var i = 0; i < 8; i++)
                {
                    var x = -length / 2 + i * length / 8;
                    var mark = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(.025f, .004f, .3f) },
                        Position = new Vector3(x, .005f, side), MaterialOverride = stripe };
                    drive.AddChild(mark); _parcelTableCleats.Add((mark, x, table));
                }
            }
        }
    }
    private bool ParcelHasDeliveredSupport(ParcelTransferPlantModel.Parcel parcel)
    {
        var contacts = 0;
        foreach (var (x, z) in new[] { (0.0, 0.0), (-parcel.HalfLength, -parcel.HalfWidth),
            (-parcel.HalfLength, parcel.HalfWidth), (parcel.HalfLength, -parcel.HalfWidth), (parcel.HalfLength, parcel.HalfWidth) })
        {
            var world = new Vector3((float)(parcel.X + x * Math.Cos(parcel.Angle) + z * Math.Sin(parcel.Angle)),
                _parcelHeights[0], (float)(parcel.Z - x * Math.Sin(parcel.Angle) + z * Math.Cos(parcel.Angle)));
            var supported = _parcelSupportBelts.Any(b =>
            {
                var local = b.GlobalTransform.AffineInverse() * world;
                var box = b.GetAabb();
                return local.X >= box.Position.X - .002f && local.X <= box.End.X + .002f
                    && local.Z >= box.Position.Z - .002f && local.Z <= box.End.Z + .002f;
            });
            foreach (var (id, radius) in new[] { ("sort_turntable_a", 1.8), ("sort_turntable_b", 1.5) })
            {
                var center = _sceneRoot.GetNode<Node3D>(id).GlobalPosition;
                supported |= Math.Pow(world.X - center.X, 2) + Math.Pow(world.Z - center.Z, 2) <= radius * radius;
            }
            if (supported) contacts++;
        }
        // A rigid carton may bridge the small authored belt/table joint with
        // its center or leading edge unsupported; at least three footprint
        // samples must remain on delivered supporting surfaces.
        return contacts >= 3;
    }
    private void CreateParcelEscapement()
    {
        if (_parcelEscapement is not null) return;
        var yellow = new StandardMaterial3D { AlbedoColor = new Color(1, .75f, .03f), Roughness = .6f };
        _parcelEscapement = new MeshInstance3D { Name = "ParcelInfeedEscapementStop",
            Mesh = new BoxMesh { Size = new Vector3(.015f, .15f, 1.08f) }, MaterialOverride = yellow };
        _sceneRoot.AddChild(_parcelEscapement);
        var actuator = new MeshInstance3D { Name = "ParcelEscapementCylinder",
            Mesh = new CylinderMesh { TopRadius = .07f, BottomRadius = .07f, Height = .55f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(.3f, .35f, .4f), Metallic = .8f },
            Position = new Vector3((float)_parcelGateX + .0075f, .65f, 0) };
        _sceneRoot.AddChild(actuator);
    }
    private void FreezeParcelTransferAdapters()
    {
        foreach (var belt in _parcelBelts) belt.SetPhysicsProcess(!UsesExternalClock);
        foreach (var table in _parcelTables) table.SetPhysicsProcess(!UsesExternalClock);
    }
    private void AdvanceParcelTransferPlant(double seconds)
    {
        _parcelTransfer!.Advance(seconds, AsBool(_points["conveyors_run"]), AsBool(_points["infeed_run"]),
            AsBool(_points["table_transfer_run"]), AsBool(_points["infeed_gate_open"]),
            TryAsDouble(_points.GetValueOrDefault("route_position"), out var target) ? target : double.NaN);
        var speed = AsBool(_points["conveyors_run"]) ? Number(_definition, "conveyorSpeedMps", .9) : 0;
        if (AsBool(_points["table_transfer_run"])) _parcelTablePhases[0] += speed * seconds;
        _parcelTablePhases[1] += speed * seconds;
        for (var i = 0; i < _parcelBelts.Length; i++)
        {
            var effective = i == 0 && !AsBool(_points["infeed_run"]) ? 0 : speed;
            _parcelBelts[i].ApplyPlantTravel((float)(effective * seconds), (float)effective);
        }
        ProjectParcelTransferPlant(); ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectParcelTransferPlant()
    {
        if (_parcelTransfer is null || !UsesExternalClock) return;
        var plant = _parcelTransfer;
        for (var i = 0; i < _parcelLoads.Length; i++)
        {
            var parcel = plant.Parcels[i];
            _parcelLoads[i].Position = new Vector3((float)parcel.X, _parcelHeights[i], (float)parcel.Z);
            _parcelLoads[i].Rotation = new Vector3(0, (float)parcel.Angle, 0);
        }
        SetPoint("primary_table_loaded", plant.TableLoaded); SetPoint("primary_table_clear", plant.TableClear);
        SetPoint("primary_table_aligned", plant.TableAligned); SetPoint("infeed_gate_clear", plant.GateClear);
        SetPoint("transfer_inhibited", plant.Inhibited); SetPoint("misroute_detected", plant.Misroute);
        SetPoint("all_parcels_received", plant.Complete);
        SetPoint("infeed_effective", AsBool(_points["conveyors_run"]) && AsBool(_points["infeed_run"]));
        SetPoint("large_detected", plant.DetectedClass == 1); SetPoint("medium_detected", plant.DetectedClass == 2);
        SetPoint("small_detected", plant.DetectedClass == 3);
        SetPoint("sorted_count", plant.SortedCount); SetPoint("cycle_complete", plant.Complete);
        SetPoint("status_color", !PlantPlaybackRunning ? "red" : AsBool(_points["conveyors_run"]) ? "green" : "amber");
        ProjectParcelTransferVisuals();
    }
    private void ProjectParcelTransferVisuals()
    {
        if (_parcelTransfer is null || !UsesExternalClock) return;
        _parcelTables[0].SetPositionNormalized((float)_parcelTransfer.TablePosition);
        _parcelTables[1].SetPositionNormalized(.5f); // Fixed authored diagonal secondary transfer datum.
        if (_parcelTableDrives.Length > 0)
        {
            _parcelTableDrives[0].Rotation = new Vector3(0, (float)(_parcelTransfer.TablePosition * Math.PI / 2), 0);
            _parcelTableDrives[1].Rotation = new Vector3(0, MathF.PI / 4, 0);
            foreach (var (mesh, home, table) in _parcelTableCleats)
            {
                var length = table == 0 ? 3.2 : 2.6;
                mesh.Position = new Vector3((float)((home + length / 2 + _parcelTablePhases[table]) % length - length / 2),
                    mesh.Position.Y, mesh.Position.Z);
            }
        }
        _parcelEscapement!.Position = new Vector3((float)_parcelGateX + .0075f,
            _parcelTransfer.GateOpen ? .88f : 1.135f, 0);
        var detected = _parcelTransfer.DetectedClass;
        for (var i = 0; i < _parcelSizeBeams.Length; i++)
            _parcelSizeBeams[i].Visible = detected == 0 || i >= 4 - detected;
    }
}
