using System;
using Godot;

namespace RungProof.Next.App;

/// <summary>Viewport navigation that stays dormant unless the 3D area receives input.</summary>
public partial class SceneCameraController : Node
{
    private readonly Camera3D _camera;
    private Vector3 _target;
    private float _distance;
    private float _yaw;
    private float _pitch;
    private bool _orbiting;
    private bool _panning;
    /// <summary>Returns the unobstructed 3D aperture in viewport coordinates.</summary>
    public Func<Rect2>? ViewportRectProvider { get; set; }
    public Vector3 ViewTarget => _target;

    public SceneCameraController(Camera3D camera)
    {
        _camera = camera;
        Name = "SceneCameraController";
        CaptureCurrentView();
    }

    public void CaptureCurrentView(Vector3? framedTarget = null)
    {
        var forward = -_camera.GlobalBasis.Z.Normalized();
        _distance = framedTarget.HasValue
            ? _camera.Position.DistanceTo(framedTarget.Value)
            : Mathf.Clamp(_camera.Position.Length() * 0.55f, 2.0f, 80.0f);
        _target = framedTarget ?? _camera.Position + forward * _distance;
        _yaw = Mathf.Atan2(forward.X, forward.Z);
        _pitch = Mathf.Asin(Mathf.Clamp(forward.Y, -0.99f, 0.99f));
        UpdateViewportOffsets();
    }

    private void UpdateViewportOffsets()
    {
        var viewport = _camera.GetViewport().GetVisibleRect();
        var aperture = ViewportRectProvider?.Invoke() ?? viewport;
        if (aperture.Size.X <= 0 || aperture.Size.Y <= 0) aperture = viewport;
        var worldUnitsPerPixel = 2.0f * _distance
            * Mathf.Tan(Mathf.DegToRad(_camera.Fov * 0.5f)) / viewport.Size.Y;
        // Godot camera offsets participate in projection and ray picking. Keep
        // the scene center inside the actual aperture, including after zoom.
        _camera.HOffset = (viewport.GetCenter().X - aperture.GetCenter().X) * worldUnitsPerPixel;
        _camera.VOffset = (aperture.GetCenter().Y - viewport.GetCenter().Y) * worldUnitsPerPixel;
    }

    /// <summary>
    /// Ends any pointer gesture when the scene viewport is hidden or restored.
    /// A view switch can occur without a matching native mouse-button release,
    /// so the controller must not carry a stale orbit/pan capture into the
    /// next scene interaction.
    /// </summary>
    public void CancelPointerCapture()
    {
        _orbiting = false;
        _panning = false;
    }

    public void FocusOn(Vector3 target, float radius)
    {
        _target = target;
        _distance = Mathf.Clamp(Mathf.Max(radius * 2.6f, 1.5f), 0.5f, 200.0f);
        ApplyView();
    }

    // CanvasLayer controls consume pointer events before _UnhandledInput. Use
    // _Input for viewport navigation, then explicitly reject every pointer
    // outside the scene aperture so buttons, tabs, and lists keep their input.
    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Right)
            {
                _orbiting = button.Pressed && IsInSceneViewport(button.Position);
            }
            if (button.ButtonIndex == MouseButton.Middle)
                _panning = button.Pressed && IsInSceneViewport(button.Position);
            if (button.Pressed && IsInSceneViewport(button.Position)
                && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                _distance = Mathf.Clamp(_distance * (button.ButtonIndex == MouseButton.WheelUp ? 0.88f : 1.14f), 0.5f, 200.0f);
                ApplyView();
            }
        }
        if (inputEvent is not InputEventMouseMotion motion) return;
        if (_orbiting)
        {
            _yaw -= motion.Relative.X * 0.006f;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * 0.006f, -1.45f, 1.45f);
            ApplyView();
        }
        else if (_panning)
        {
            var scale = Mathf.Max(_distance * 0.0016f, 0.002f);
            _target += (_camera.GlobalBasis.X * -motion.Relative.X + _camera.GlobalBasis.Y * motion.Relative.Y) * scale;
            ApplyView();
        }
    }

    private bool IsInSceneViewport(Vector2 position) =>
        ViewportRectProvider?.Invoke().HasPoint(position) ?? true;

    private void ApplyView()
    {
        var direction = new Vector3(
            Mathf.Sin(_yaw) * Mathf.Cos(_pitch),
            Mathf.Sin(_pitch),
            Mathf.Cos(_yaw) * Mathf.Cos(_pitch)
        );
        _camera.Position = _target - direction * _distance;
        _camera.LookAt(_target, Vector3.Up);
        UpdateViewportOffsets();
    }
}
