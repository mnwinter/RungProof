using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    // This opt-in reference uses the actual loaded-case meshes. It does not
    // produce commands when a virtual/external PLC owns the output image.
    private bool HasShippingPalletReference => _definition.TryGetProperty("shippingPalletReference", out _);
    private bool _shippingPalletReferenceActive;
    private bool _shippingPalletReferenceAutomatic;
    private Node3D? _shippingPallet;
    private MeshInstance3D? _shippingPalletTx, _shippingPalletRx;
    private (MeshInstance3D Mesh, Vector3[] Faces)[] _shippingPalletCases = [];
    private double _shippingPalletStartX, _shippingPalletPickupX, _shippingPalletX, _shippingPalletTargetX, _shippingPalletSpeed;
    private double _shippingPalletJogFraction;

    private void ResetShippingPalletReference()
    {
        _shippingPalletReferenceActive = false;
        if (!HasShippingPalletReference) return;
        var config = _definition.GetProperty("shippingPalletReference");
        _shippingPallet = _sceneRoot.GetNode<Node3D>(SafeNodeName(Text(config, "productId", string.Empty)));
        var sensor = _sceneRoot.GetNode<Node3D>(SafeNodeName(Text(config, "photoeyeId", string.Empty)));
        _shippingPalletTx = (MeshInstance3D)sensor.FindChild("TX_lens", true, false);
        _shippingPalletRx = (MeshInstance3D)sensor.FindChild("RX_lens", true, false);
        _shippingPalletCases = _shippingPallet.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(mesh => mesh.Mesh is not null && mesh.Name.ToString().StartsWith("CASE_", StringComparison.Ordinal)
                && mesh.Name.ToString().Length > 5 && char.IsDigit(mesh.Name.ToString()[5]))
            .Select(mesh => (mesh, mesh.Mesh.GetFaces())).ToArray();
        _shippingPalletSpeed = Number(config, "speedMps", 0);
        _shippingPalletJogFraction = Number(config, "jogFraction", 0);
        _shippingPalletStartX = _shippingPallet.Position.X;
        var maximumX = Number(config, "maximumX", double.NaN);
        if (_shippingPalletCases.Length == 0 || !double.IsFinite(maximumX) || maximumX <= _shippingPalletStartX
            || !double.IsFinite(_shippingPalletSpeed) || _shippingPalletSpeed <= 0 || _shippingPalletSpeed > 2
            || !double.IsFinite(_shippingPalletJogFraction) || _shippingPalletJogFraction <= 0 || _shippingPalletJogFraction > 1)
            throw new InvalidOperationException("Shipping pallet reference requires cases, a forward finite bound, positive speed and a bounded jog fraction.");
        var conveyor = _sceneRoot.GetNode<Node3D>(SafeNodeName(Text(config, "conveyorId", string.Empty)))
            .FindChildren("*", string.Empty, true, false).OfType<ConveyorController>().Single();
        conveyor.SpeedSetpointMps = (float)_shippingPalletSpeed;
        foreach (var (point, owner, type) in new[] { ("pickup_sensor", "PC", "BOOL"), ("pallet_position", "SIM", "REAL"),
            ("conveyor_run", "PLC", "BOOL"), ("auto_mode", "PC", "BOOL"), ("cycle_complete", "SIM", "BOOL"), ("status_color", "SIM", "STRING") })
            if (_pointOwners.GetValueOrDefault(point) != owner || _pointTypes.GetValueOrDefault(point) != type)
                throw new InvalidOperationException($"Shipping pallet reference has an invalid {point} ownership/type contract.");

        // Intersect the real triangle surfaces with the horizontal optical
        // plane. A center coordinate can lie in a gap between case columns;
        // an enclosing load box or a monotonic endpoint test misses that gap.
        var tx = _shippingPalletTx.GlobalTransform * _shippingPalletTx.GetAabb().GetCenter();
        var rx = _shippingPalletRx.GlobalTransform * _shippingPalletRx.GetAabb().GetCenter();
        if (MathF.Abs(tx.X - rx.X) > 0.00001f || MathF.Abs(tx.Y - rx.Y) > 0.00001f)
            throw new InvalidOperationException("Shipping pallet reference requires a horizontal cross-conveyor beam.");
        var minZ = MathF.Min(tx.Z, rx.Z); var maxZ = MathF.Max(tx.Z, rx.Z);
        var leadingX = double.NegativeInfinity;
        foreach (var (mesh, faces) in _shippingPalletCases)
        for (var index = 0; index < faces.Length; index += 3)
        {
            var points = new List<Vector3>();
            for (var edge = 0; edge < 3; edge++)
            {
                var a = mesh.GlobalTransform * faces[index + edge];
                var b = mesh.GlobalTransform * faces[index + (edge + 1) % 3];
                if (MathF.Abs(a.Y - tx.Y) < 1e-7f) points.Add(a);
                if ((a.Y < tx.Y && b.Y > tx.Y) || (a.Y > tx.Y && b.Y < tx.Y))
                    points.Add(a.Lerp(b, (tx.Y - a.Y) / (b.Y - a.Y)));
            }
            foreach (var point in points)
                if (point.Z >= minZ && point.Z <= maxZ) leadingX = Math.Max(leadingX, point.X);
            foreach (var a in points)
            foreach (var b in points)
            foreach (var z in new[] { minZ, maxZ })
                if ((a.Z < z && b.Z > z) || (a.Z > z && b.Z < z))
                    leadingX = Math.Max(leadingX, a.Lerp(b, (z - a.Z) / (b.Z - a.Z)).X);
        }
        _shippingPalletPickupX = _shippingPalletStartX + tx.X - leadingX + 0.0001;
        if (!double.IsFinite(_shippingPalletPickupX) || _shippingPalletPickupX <= _shippingPalletStartX
            || _shippingPalletPickupX > maximumX || ShippingPalletBlocksBeam())
            throw new InvalidOperationException("Shipping pallet reference requires a clear start and a case crossing inside the forward bound.");
        var home = _shippingPallet.Position;
        try
        {
            MoveShippingPallet(_shippingPalletPickupX);
            if (!ShippingPalletBlocksBeam()) throw new InvalidOperationException("Shipping pallet calculated endpoint does not block the actual beam.");
        }
        finally { _shippingPallet.Position = home; }
        _shippingPalletX = _shippingPalletStartX;
        ProjectShippingPalletReference();
        GD.Print($"SHIPPING_PALLET_REFERENCE_DATUM start={_shippingPalletStartX} pickup={_shippingPalletPickupX} speed={_shippingPalletSpeed}");
    }

    private bool StartShippingPalletReference(string sequence)
    {
        if (UsesExternalClock || _externalPlaybackSelected || _shippingPalletReferenceActive || _shippingPallet is null
            || sequence is not ("automatic" or "manual")) return false;
        var automatic = sequence == "automatic";
        if (automatic != AsBool(_points["auto_mode"]) || ShippingPalletBlocksBeam()) return false;
        // Read the held pose when restarting; never reapply the original start.
        var currentX = (double)_shippingPallet.Position.X;
        if (currentX < _shippingPalletStartX - 0.001 || currentX > _shippingPalletPickupX + 0.001) return false;
        if (Math.Abs(currentX - _shippingPalletX) > 0.00001) _shippingPalletX = currentX;
        _shippingPalletTargetX = automatic ? _shippingPalletPickupX
            : Math.Min(_shippingPalletPickupX, _shippingPalletX + (_shippingPalletPickupX - _shippingPalletStartX) * _shippingPalletJogFraction);
        _shippingPalletReferenceAutomatic = automatic;
        _shippingPalletReferenceActive = true;
        SetPoint("conveyor_run", true); SetPoint("cycle_complete", false);
        SetPoint("status_color", automatic ? "green" : "amber");
        ApplyBindings(); StateChanged?.Invoke();
        GD.Print($"SHIPPING_PALLET_REFERENCE_START {sequence} from={_shippingPalletX} to={_shippingPalletTargetX}");
        return true;
    }

    private void AdvanceShippingPalletReference(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentException("Shipping pallet step must be finite and non-negative.");
        if (_shippingPalletReferenceActive)
        {
            if (_shippingPalletReferenceAutomatic != AsBool(_points["auto_mode"])) { StopSimulation(); return; }
            _shippingPalletX = Math.Min(_shippingPalletTargetX, _shippingPalletX + _shippingPalletSpeed * seconds);
            MoveShippingPallet(_shippingPalletX);
            if (_shippingPalletX >= _shippingPalletTargetX)
            {
                _shippingPalletReferenceActive = false;
                SetPoint("conveyor_run", false); SetPoint("cycle_complete", true); SetPoint("status_color", "amber");
                GD.Print("SHIPPING_PALLET_REFERENCE_COMPLETE");
            }
        }
        ProjectShippingPalletReference();
        ApplyBindings(); StateChanged?.Invoke();
    }

    private void AdvanceControllerShippingPallet(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Shipping pallet step must be finite and non-negative.");
        // The PLC owns run/stop and mode logic. Do not reuse the autonomous
        // reference: it writes conveyor_run and stops itself at the beam.
        var commanded = AsBool(_points.GetValueOrDefault("conveyor_run"));
        if (commanded)
        {
            var maximumX = Number(_definition.GetProperty("shippingPalletReference"), "maximumX", _shippingPalletPickupX);
            _shippingPalletX = Math.Min(maximumX, _shippingPalletX + _shippingPalletSpeed * seconds);
            MoveShippingPallet(_shippingPalletX);
        }
        ProjectShippingPalletReference();
        SetPoint("cycle_complete", !commanded && ShippingPalletBlocksBeam());
        SetPoint("status_color", commanded ? "green" : "amber");
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void MoveShippingPallet(double x)
    {
        var position = _shippingPallet!.Position;
        position.X = (float)x;
        _shippingPallet.Position = position;
    }

    private void ProjectShippingPalletReference()
    {
        SetPoint("pallet_position", Math.Clamp((_shippingPalletX - _shippingPalletStartX) / (_shippingPalletPickupX - _shippingPalletStartX) * 100, 0, 100));
        SetPoint("pickup_sensor", ShippingPalletBlocksBeam());
    }

    private bool ShippingPalletBlocksBeam()
    {
        Vector3 Center(MeshInstance3D mesh) => mesh.GlobalTransform * mesh.GetAabb().GetCenter();
        var from = Center(_shippingPalletTx!); var to = Center(_shippingPalletRx!);
        return LoadedCasesBlockBeam(_shippingPalletCases, from, to);
    }

    private static bool LoadedCasesBlockBeam((MeshInstance3D Mesh, Vector3[] Faces)[] cases, Vector3 from, Vector3 to)
    {
        foreach (var (mesh, faces) in cases)
        {
            var inverse = mesh.GlobalTransform.AffineInverse();
            var a = inverse * from; var b = inverse * to;
            var direction = b - a;
            // Slab broad phase in mesh-local space. Fork openings and carton
            // bevels are decided by actual triangles in the narrow phase.
            var bounds = mesh.GetAabb(); var near = 0f; var far = 1f;
            for (var axis = 0; axis < 3; axis++)
            {
                if (MathF.Abs(direction[axis]) < 1e-8f)
                {
                    if (a[axis] < bounds.Position[axis] || a[axis] > bounds.End[axis]) { far = -1; break; }
                }
                else
                {
                    var enter = (bounds.Position[axis] - a[axis]) / direction[axis];
                    var leave = (bounds.End[axis] - a[axis]) / direction[axis];
                    near = MathF.Max(near, MathF.Min(enter, leave)); far = MathF.Min(far, MathF.Max(enter, leave));
                }
            }
            if (near > far) continue;
            for (var index = 0; index < faces.Length; index += 3)
            {
                var edge1 = faces[index + 1] - faces[index]; var edge2 = faces[index + 2] - faces[index];
                var cross = direction.Cross(edge2); var determinant = edge1.Dot(cross);
                if (MathF.Abs(determinant) < 1e-8f) continue;
                var offset = a - faces[index]; var u = offset.Dot(cross) / determinant;
                if (u < -1e-6f || u > 1.000001f) continue;
                var q = offset.Cross(edge1); var v = direction.Dot(q) / determinant;
                if (v < -1e-6f || u + v > 1.000001f) continue;
                var t = edge2.Dot(q) / determinant;
                if (t >= 0 && t <= 1) return true;
            }
        }
        return false;
    }
}
