using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    // Imported moving meshes may have a rotated local basis (the button's
    // cylinder axis is not the operator-facing axis). Use the authored legend
    // plane, or the station housing plane for catalog-textured controls.
    private static Vector3 RetainedOperatorFaceNormal(Node3D station) =>
        (station.FindChild("OperatorFaceLabel", true, false) as Label3D)?.GlobalBasis.Z.Normalized()
        ?? station.GlobalBasis.Z.Normalized();
    private readonly List<object> _retainedNavigationTrace = new();

    // QA-only: use the same gestures as the learner, never write camera poses.
    private async Task NavigateRetainedControlCapture(string equipmentId)
    {
        var camera = _sceneCamera!; var controller = _cameraController!;
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var aperture = _simulatorShell!.SceneViewportRect();
        var binding = _sceneControlInteractor!.Bindings.Single(b => b.EquipmentId == equipmentId);
        var head = binding.Node.FindChild("KIN_pushbutton", true, false) as Node3D
            ?? binding.Node.FindChild("KIN_estop", true, false) as Node3D
            ?? binding.Node.FindChild("KIN_selector_handle", true, false) as Node3D
            ?? throw new InvalidOperationException("Retained head missing: " + equipmentId);
        var poses = root.FindChildren("*", "", true, false).OfType<Node3D>().Append(root).ToDictionary(n => n, n => n.Transform);
        var points = runtime.Points.ToDictionary(p => p.Key, p => p.Value);
        var initial = camera.Transform; var fov = camera.Fov; var events = 0;
        void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException("Retained navigation: " + message); }
        async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        void Send(InputEvent input)
        {
            Require(++events <= 240, "finite event cap");
            GetViewport().PushInput(input, true);
        }
        async Task Drag(MouseButton button, Vector2 delta)
        {
            var at = aperture.GetCenter();
            Send(new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = at });
            var chunks = Math.Max(1, (int)MathF.Ceiling(delta.Length() / 80));
            Require(chunks <= 32, "finite drag cap");
            for (var i = 0; i < chunks; i++) Send(new InputEventMouseMotion { Position = at, Relative = delta / chunks });
            Send(new InputEventMouseButton { ButtonIndex = button, Pressed = false, Position = at });
            await Frame();
        }
        async Task Orbit(float yaw, float pitch)
        {
            var forward = -camera.GlobalBasis.Z.Normalized();
            await Drag(MouseButton.Right, new Vector2(-Mathf.Wrap(yaw - Mathf.Atan2(forward.X, forward.Z), -Mathf.Pi, Mathf.Pi) / .006f,
                -(pitch - Mathf.Asin(Mathf.Clamp(forward.Y, -.99f, .99f))) / .006f));
        }
        async Task Pan()
        {
            var delta = head.GlobalPosition - controller.ViewTarget;
            var scale = MathF.Max(camera.Position.DistanceTo(controller.ViewTarget) * .0016f, .002f);
            await Drag(MouseButton.Middle, new Vector2(-delta.Dot(camera.GlobalBasis.X.Normalized()) / scale,
                delta.Dot(camera.GlobalBasis.Y.Normalized()) / scale));
        }
        await Orbit(0, 0); await Pan(); await Orbit(Mathf.Pi / 2, 0); await Pan();
        var normal = RetainedOperatorFaceNormal(binding.Node);
        var desired = -normal;
        await Orbit(Mathf.Atan2(desired.X, desired.Z), Mathf.Asin(Mathf.Clamp(desired.Y, -.99f, .99f))); await Pan();
        var meshes = head.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToList();
        if (head is MeshInstance3D own) meshes.Add(own);
        var corners = meshes.Where(m => m.Mesh is not null && m.IsVisibleInTree()).SelectMany(m => Enumerable.Range(0, 8).Select(i => m.GlobalTransform * m.GetAabb().GetEndpoint(i))).ToArray();
        Require(corners.Length > 0, "head mesh bounds absent");
        Vector2[] Project() => corners.Select(camera.UnprojectPosition).ToArray();
        float Size() { var p = Project(); return MathF.Max(p.Max(v => v.X) - p.Min(v => v.X), p.Max(v => v.Y) - p.Min(v => v.Y)); }
        float NarrowSize() { var p = Project(); return MathF.Min(p.Max(v => v.X) - p.Min(v => v.X), p.Max(v => v.Y) - p.Min(v => v.Y)); }
        for (var wheel = 0; (Size() < 48 || NarrowSize() < 24) && wheel < 48; wheel++)
        {
            Send(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true, Position = aperture.GetCenter() });
            await Frame();
        }
        Require(Size() >= 48, "head below 48px within finite zoom");
        Require(NarrowSize() >= 24, "head narrow extent below 24px");
        Require(corners.All(p => !camera.IsPositionBehind(p)) && Project().All(aperture.Grow(-12).HasPoint), "head bounds outside aperture");
        Require((camera.GlobalPosition - head.GlobalPosition).Dot(normal) > 0, "head viewed from back");
        Require(poses.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "navigation moved field geometry");
        Require(points.Count == runtime.Points.Count && points.All(p => Equals(p.Value, runtime.Points[p.Key])), "navigation changed points");
        Require(camera.Fov == fov && aperture == _simulatorShell.SceneViewportRect(), "layout/FOV changed");
        _retainedNavigationTrace.Add(new { equipmentId, events, initial = initial.ToString(), final = camera.Transform.ToString(),
            headPixels = Size(), route = "Viewport.PushInput right/middle/wheel", aperture = aperture.ToString() });
        System.IO.File.WriteAllText(_capturePath + ".retained-navigation.json", JsonSerializer.Serialize(_retainedNavigationTrace, new JsonSerializerOptions { WriteIndented = true }));
    }
}
