using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasGuardedTransfer => RuntimeType == "guardedTransfer";
    private Node3D? _guardedCarton;
    private ConveyorController? _guardedDrive;
    private float _guardedExitX;

    private void ResetGuardedTransfer()
    {
        if (!HasGuardedTransfer) return;
        _guardedCarton = _sceneRoot.GetNode<Node3D>("box_1");
        var conveyor = _sceneRoot.GetNode<Node3D>("conveyor_0");
        _guardedDrive = conveyor.FindChildren("*", string.Empty, true, false).OfType<ConveyorController>().Single();
        var surface = (MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false);
        var belt = surface.GlobalTransform * surface.GetAabb();
        var load = DeliveredEquipmentBounds(_guardedCarton);
        // Keep the complete delivered carton footprint on the actual belt,
        // including a 10 mm endpoint margin. No wraparound or disappearance.
        _guardedExitX = _guardedCarton.Position.X + belt.End.X - load.End.X - .01f;
        if (load.Position.X < belt.Position.X || load.End.X > belt.End.X
            || load.Position.Z < belt.Position.Z || load.End.Z > belt.End.Z
            || MathF.Abs(load.Position.Y - belt.End.Y) > .001f)
            throw new InvalidOperationException("Guarded transfer home carton is not supported by the delivered belt.");
        _guardedDrive.SetPhysicsProcess(false);
        _guardedDrive.ResetPlantTravel();
        _guardedCarton.Visible = true;
        ProjectGuardedTransfer();
    }

    private void PauseGuardedTransfer()
    {
        if (!HasGuardedTransfer || _guardedDrive is null) return;
        _guardedDrive.ApplyPlantTravel(0, 0);
    }

    private void AdvanceGuardedTransfer(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Guarded transfer requires accepted ticks up to 20 ms.");
        var carton = _guardedCarton!;
        var oldX = carton.Position.X;
        var speed = Number(_definition, "speedMps", .45);
        if (!double.IsFinite(speed) || speed <= 0) throw new InvalidOperationException("Invalid guarded transfer speed.");
        var newX = AsBool(_points.GetValueOrDefault("transfer_run"))
            ? MathF.Min(_guardedExitX, oldX + (float)(speed * seconds)) : oldX;
        carton.Position = new Vector3(newX, carton.Position.Y, carton.Position.Z);
        _guardedDrive!.ApplyPlantTravel(newX - oldX, (newX - oldX) / (float)seconds);
        ProjectGuardedTransfer();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void ProjectGuardedTransfer()
    {
        var load = DeliveredEquipmentBounds(_guardedCarton!);
        foreach (var (sensorId, point) in new[] { ("photoeye_2", "entry_carton_present"), ("photoeye_3", "exit_carton_present") })
        {
            // The imported dashed witness route is straight and transverse.
            // Merge its bounds for finite route/body overlap; manual path-clear
            // fixtures remain independent and do not assert protective sensing.
            var sensor = _sceneRoot.GetNode<Node3D>(sensorId);
            var beams = sensor.FindChildren("KIN_beam*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
            if (beams.Length == 0) throw new InvalidOperationException($"Missing optical route for {sensorId}.");
            var route = beams[0].GlobalTransform * beams[0].GetAabb();
            foreach (var beam in beams.Skip(1)) route = route.Merge(beam.GlobalTransform * beam.GetAabb());
            SetPoint(point, load.Intersects(route));
        }
        SetPoint("carton_position", (double)_guardedCarton!.Position.X);
        SetPoint("transfer_complete", _guardedCarton.Position.X >= _guardedExitX - .00001f);
    }
}
