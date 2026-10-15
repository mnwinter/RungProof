using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasBagIndexPlant => RuntimeType == "bagIndex";
    public BagIndexPlantModel? BagIndexPlant { get; private set; }
    private Node3D? _indexedBag;
    private MeshInstance3D? _indexedBagBody;
    private Vector3[] _indexedBagFaces = [];
    private MeshInstance3D[] _bagIndexTx = [], _bagIndexRx = [];
    private ConveyorController? _bagIndexConveyor;

    private void ResetBagIndexPlant()
    {
        if (!HasBagIndexPlant) return;
        foreach (var name in new[] { "bag_at_entry", "bag_at_exit", "start_request", "pause_clear", "motion_inhibited", "travel_limited" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException("Bag index requires PC BOOL " + name);
        foreach (var name in new[] { "bag_position", "belt_speed" })
            if (_pointOwners.GetValueOrDefault(name) != "PC" || _pointTypes.GetValueOrDefault(name) != "REAL")
                throw new InvalidOperationException("Bag index requires PC REAL " + name);
        foreach (var name in new[] { "conveyor_run", "conveyor_reverse" })
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || _pointTypes.GetValueOrDefault(name) != "BOOL")
                throw new InvalidOperationException("Bag index requires PLC BOOL " + name);
        _indexedBag = _sceneRoot.GetNode<Node3D>("box_1");
        _indexedBagBody = (MeshInstance3D)_indexedBag.FindChild("BAG_BODY", true, false);
        _indexedBagFaces = _indexedBagBody.Mesh.GetFaces();
        var sensors = new[] { "photoeye_2", "photoeye_3" }.Select(id => _sceneRoot.GetNode<Node3D>(id)).ToArray();
        _bagIndexTx = sensors.Select(n => (MeshInstance3D)n.FindChild("TX_lens", true, false)).ToArray();
        _bagIndexRx = sensors.Select(n => (MeshInstance3D)n.FindChild("RX_lens", true, false)).ToArray();
        _bagIndexConveyor = _sceneRoot.GetNode<Node3D>("conveyor_0").FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
        BagIndexPlant ??= new(Number(_definition, "minimumX", double.NaN), Number(_definition, "maximumX", double.NaN),
            _indexedBag.Position.X, Number(_definition, "speedMps", double.NaN));
        BagIndexPlant.Reset(); FreezeBagIndexAdapter(); _bagIndexConveyor.ResetPlantTravel(); ProjectBagIndexPlant();
        if (_indexedBagFaces.Length == 0 || !BagIndexBlocksBeam(0) || BagIndexBlocksBeam(1))
            throw new InvalidOperationException("Indexed bag must initially block only ENTRY with actual body triangles.");
    }
    private void FreezeBagIndexAdapter() => _bagIndexConveyor?.SetPhysicsProcess(false);
    private void PauseBagIndexClock()
    {
        if (BagIndexPlant is null) return;
        BagIndexPlant.Pause(); _bagIndexConveyor!.ApplyPlantTravel(0, 0);
        SetPoint("belt_speed", 0d); ApplyBindings(); StateChanged?.Invoke();
    }
    private void AdvanceBagIndexPlant(double seconds)
    {
        var plant = BagIndexPlant!; var before = plant.Position;
        plant.Step(seconds, AsBool(_points["conveyor_run"]), AsBool(_points["conveyor_reverse"]), AsBool(_points["pause_clear"]));
        ProjectBagIndexPlant();
        _bagIndexConveyor!.ApplyPlantTravel((float)(plant.Position - before), (float)plant.Speed);
        ApplyBindings(); StateChanged?.Invoke();
    }
    private void ProjectBagIndexPlant()
    {
        var plant = BagIndexPlant!;
        _indexedBag!.Position = new Vector3((float)plant.Position, _indexedBag.Position.Y, _indexedBag.Position.Z);
        SetPoint("bag_at_entry", BagIndexBlocksBeam(0)); SetPoint("bag_at_exit", BagIndexBlocksBeam(1));
        SetPoint("bag_position", plant.Position); SetPoint("belt_speed", plant.Speed);
        SetPoint("motion_inhibited", plant.MotionInhibited); SetPoint("travel_limited", plant.TravelLimited);
    }
    private bool BagIndexBlocksBeam(int sensor)
    {
        // Test the delivered body triangles against the finite optical segment,
        // not the broad box around the tapered sack or its separate label.
        var inverse = _indexedBagBody!.GlobalTransform.AffineInverse();
        var tx = _bagIndexTx[sensor]; var rx = _bagIndexRx[sensor];
        var from = inverse * (tx.GlobalTransform * tx.GetAabb().GetCenter());
        var to = inverse * (rx.GlobalTransform * rx.GetAabb().GetCenter()); var direction = to - from;
        for (var i = 0; i < _indexedBagFaces.Length; i += 3)
        {
            var e1 = _indexedBagFaces[i + 1] - _indexedBagFaces[i]; var e2 = _indexedBagFaces[i + 2] - _indexedBagFaces[i];
            var cross = direction.Cross(e2); var determinant = e1.Dot(cross);
            if (MathF.Abs(determinant) < 1e-8f) continue;
            var offset = from - _indexedBagFaces[i]; var u = offset.Dot(cross) / determinant;
            if (u < -1e-6f || u > 1.000001f) continue;
            var q = offset.Cross(e1); var v = direction.Dot(q) / determinant;
            if (v < -1e-6f || u + v > 1.000001f) continue;
            var t = e2.Dot(q) / determinant;
            if (t >= 0 && t <= 1) return true;
        }
        return false;
    }
}
