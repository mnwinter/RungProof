using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasMultiConveyorRoute => _definition.TryGetProperty("multiConveyorRoute", out _);
    private Node3D? _routePallet;
    private double _routeHomeX, _routeMaximumX, _routeSpeed;
    private double _routeHalfLength;
    private readonly double[] _routeBeltLeft = new double[3], _routeBeltRight = new double[3];
    private (MeshInstance3D Mesh, Vector3[] Faces)[] _routeCases = [];
    private (string Point, MeshInstance3D Tx, MeshInstance3D Rx)[] _routeSensors = [];

    private void ResetMultiConveyorRoute()
    {
        if (!HasMultiConveyorRoute) return;
        var config = _definition.GetProperty("multiConveyorRoute");
        _routePallet = _sceneRoot.GetNode<Node3D>(SafeNodeName(Text(config, "productId", "")));
        _routeHomeX = _routePallet.Position.X;
        _routeMaximumX = Number(config, "maximumX", double.NaN);
        _routeSpeed = Number(config, "speedMps", double.NaN);
        _routeCases = _routePallet.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(mesh => mesh.Mesh is not null && mesh.Name.ToString().StartsWith("CASE_", StringComparison.Ordinal)
                && mesh.Name.ToString().Length > 5 && char.IsDigit(mesh.Name.ToString()[5]))
            .Select(mesh => (mesh, mesh.Mesh.GetFaces())).ToArray();
        _routeSensors = new[] { ("infeed_blocked", "photoeye_4"), ("handoff_1_blocked", "training_accessory_7"),
            ("zone_2_blocked", "photoeye_5"), ("receiver_entry_blocked", "photoeye_3") }
            .Select(item =>
            {
                var sensor = _sceneRoot.GetNode<Node3D>(item.Item2);
                if (_pointOwners.GetValueOrDefault(item.Item1) != "PC" || _pointTypes.GetValueOrDefault(item.Item1) != "BOOL")
                    throw new InvalidOperationException("Route photoeye feedback must be a declared PC BOOL.");
                return (item.Item1, (MeshInstance3D)sensor.FindChild("TX_lens", true, false),
                    (MeshInstance3D)sensor.FindChild("RX_lens", true, false));
            }).ToArray();
        Aabb Bounds(MeshInstance3D mesh) => _sceneRoot.GlobalTransform.AffineInverse() * mesh.GlobalTransform * mesh.GetAabb();
        var runners = _routePallet.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(mesh => mesh.Name.ToString().StartsWith("PALLET_BOTTOM_", StringComparison.Ordinal)).ToArray();
        if (runners.Length != 3) throw new InvalidOperationException("Route pallet requires three bottom runners.");
        _routeHalfLength = runners.Max(mesh => Bounds(mesh).Size.X) / 2;
        for (var zone = 0; zone < 3; zone++)
        {
            var feed = _sceneRoot.GetNode<Node3D>($"conveyor_{zone}");
            var belt = Bounds((MeshInstance3D)feed.FindChild("KIN_belt_surface", true, false));
            _routeBeltLeft[zone] = belt.Position.X;
            _routeBeltRight[zone] = belt.End.X;
        }
        if (!double.IsFinite(_routeMaximumX) || _routeMaximumX <= _routeHomeX
            || !double.IsFinite(_routeSpeed) || _routeSpeed <= 0 || _routeSpeed > 2)
            throw new InvalidOperationException("Multi-conveyor route requires a forward bound and positive bounded speed.");
        for (var zone = 1; zone <= 3; zone++)
            if (_pointOwners.GetValueOrDefault($"zone_{zone}_run") != "PLC"
                || _pointTypes.GetValueOrDefault($"zone_{zone}_run") != "BOOL")
                throw new InvalidOperationException("Multi-conveyor route commands must be PLC BOOL points.");
        ProjectMultiConveyorRoute();
    }

    private void AdvanceMultiConveyorRoute(double seconds)
    {
        if (!UsesExternalClock || seconds <= 0 || !double.IsFinite(seconds)) return;
        // Small spatial steps prevent a large elapsed interval from skipping a
        // stopped receiving zone. Zone 3 includes the powered receiving rollers.
        var remaining = _routeSpeed * seconds;
        while (remaining > 1e-8 && _routePallet!.Position.X < _routeMaximumX - .00001)
        {
            var x = _routePallet.Position.X;
            var step = Math.Min(remaining, .005);
            var proposed = Math.Min(_routeMaximumX, x + step);
            var enabled = true;
            for (var zone = 0; zone < 3; zone++)
            {
                // Require both belts while the pallet spans their carrying
                // surfaces; the supported static bridge is not a drive.
                var left = _routeBeltLeft[zone];
                var right = zone == 2 ? _routeMaximumX + _routeHalfLength : _routeBeltRight[zone];
                if (proposed + _routeHalfLength > left && x - _routeHalfLength < right
                    && !AsBool(_points.GetValueOrDefault($"zone_{zone + 1}_run"))) enabled = false;
            }
            if (!enabled) break;
            var position = _routePallet.Position;
            position.X = (float)proposed;
            _routePallet.Position = position;
            remaining -= step;
        }
        ProjectMultiConveyorRoute();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void ProjectMultiConveyorRoute()
    {
        SetPoint("route_position", (double)_routePallet!.Position.X);
        SetPoint("route_complete", _routePallet.Position.X >= _routeMaximumX - .0001);
        foreach (var (point, tx, rx) in _routeSensors)
            SetPoint(point, LoadedCasesBlockBeam(_routeCases,
                tx.GlobalTransform * tx.GetAabb().GetCenter(), rx.GlobalTransform * rx.GetAabb().GetCenter()));
    }
}
