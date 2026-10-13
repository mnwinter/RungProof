using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _userReadoutCapturePending;
    private bool _userReadoutCapturePrepared;

    private async void PrepareUserReadoutCapture()
    {
        if (_userReadoutCapturePrepared) return;
        const float minimumLineBoxPixels = 18;
        _userReadoutCapturePending = true; // Synchronous: hold root countdown before the first await.
        var trace = new List<object>();
        var path = _capturePath;
        var eventCount = 0;
        object? initial = null;
        try
        {
            var camera = _sceneCamera ?? throw new InvalidOperationException("camera missing");
            var controller = _cameraController ?? throw new InvalidOperationException("controller missing");
            var shell = _simulatorShell ?? throw new InvalidOperationException("shell missing");
            var root = _sceneCompositionRoot ?? throw new InvalidOperationException("composition missing");
            var runtime = _sceneRuntime ?? throw new InvalidOperationException("runtime missing");
            void Require(bool ok, string detail) { if (!ok) throw new InvalidOperationException("User readout navigation: " + detail); }
            Require(path is not null, "capture path missing");
            Require(!shell.IsChallengeDraftActive, "ordinary non-challenge scene required");
            var args = OS.GetCmdlineUserArgs();
            Require(!args.Any(arg => arg.StartsWith("--capture-station-view=", StringComparison.Ordinal)
                || arg.StartsWith("--capture-typed-learner-detail", StringComparison.Ordinal)), "mixed diagnostic camera flags");
            Require(_virtualController?.Snapshot.State != VirtualControllerState.Running && !runtime.IsRunning,
                "stopped runtime required; preparation must finish before navigation");
            var aperture = shell.SceneViewportRect();
            Require(aperture.Size.X > 0 && aperture.Size.Y > 0 && aperture == ChallengeSceneAperture(aperture), "aperture mismatch");
            var fov = camera.Fov;
            var poses = root.FindChildren("*", "", true, false).OfType<Node3D>().Append(root)
                .ToDictionary(node => node, node => node.Transform);
            var points = runtime.Points.ToDictionary(pair => pair.Key, pair => pair.Value);
            var labels = new List<Label3D>();
            if (_currentSceneId == "lab-9-12-ev-charging-manager")
                foreach (var id in new[] { "training_accessory_5", "ev_meter_b" })
                {
                    var meter = root.GetNode<Node3D>(id);
                    labels.Add(meter.GetNode<Label3D>("NumericReadout"));
                    var plate = root.GetNode<Node3D>("ControlInstallation")
                        .GetNode<MeshInstance3D>(meter.GetMeta("installationIdentification").AsString());
                    labels.Add(plate.GetParent().GetNode<Label3D>(plate.Name + "_Ink"));
                }
            else if (_currentSceneId == "lab-10-02-chicken-label-print")
                labels.Add(root.GetNode<Label3D>("training_accessory_5/MeasuredWeight"));
            else throw new InvalidOperationException("User navigation supports only exact EV/Chicken scenes");

            float[] V(Vector3 v) => new[] { v.X, v.Y, v.Z };
            object View() => new { position = V(camera.GlobalPosition), target = V(controller.ViewTarget),
                fov = camera.Fov, hOffset = camera.HOffset, vOffset = camera.VOffset };
            initial = View();
            void Send(InputEvent input, string stage)
            {
                Require(++eventCount <= 240, "finite event cap exceeded");
                Require(shell.SceneViewportRect() == aperture && camera.Fov == fov, "aperture/FOV changed");
                var before = View();
                GetViewport().PushInput(input, true);
                trace.Add(new { stage, eventType = input.GetType().Name,
                    position = input is InputEventMouse m ? new[] { m.Position.X, m.Position.Y } : null,
                    relative = input is InputEventMouseMotion motion ? new[] { motion.Relative.X, motion.Relative.Y } : null,
                    button = input is InputEventMouseButton button ? button.ButtonIndex.ToString() : "",
                    pressed = input is InputEventMouseButton pressed && pressed.Pressed, before, after = View() });
            }
            async Task Frame()
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(shell.SceneViewportRect() == aperture && camera.Fov == fov, "layout changed during navigation");
            }
            async Task Drag(MouseButton button, Vector2 relative, string stage)
            {
                var at = aperture.GetCenter();
                Send(new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = at }, stage + " press");
                var chunks = Math.Max(1, (int)MathF.Ceiling(relative.Length() / 80));
                Require(chunks <= 32, "drag exceeded finite chunk cap");
                for (var i = 0; i < chunks; i++)
                    Send(new InputEventMouseMotion { Position = at, Relative = relative / chunks }, stage + " motion");
                Send(new InputEventMouseButton { ButtonIndex = button, Pressed = false, Position = at }, stage + " release");
                await Frame();
            }
            async Task Orbit(float yaw, float pitch, string stage)
            {
                var forward = -camera.GlobalBasis.Z.Normalized();
                var currentYaw = Mathf.Atan2(forward.X, forward.Z);
                var currentPitch = Mathf.Asin(Mathf.Clamp(forward.Y, -.99f, .99f));
                await Drag(MouseButton.Right, new Vector2(-Mathf.Wrap(yaw - currentYaw, -Mathf.Pi, Mathf.Pi) / .006f,
                    -(pitch - currentPitch) / .006f), stage);
            }
            var target = labels.Aggregate(Vector3.Zero, (sum, label) => sum + label.GlobalPosition) / labels.Count;
            async Task Pan(string stage)
            {
                var delta = target - controller.ViewTarget;
                var scale = MathF.Max(camera.Position.DistanceTo(controller.ViewTarget) * .0016f, .002f);
                await Drag(MouseButton.Middle, new Vector2(-delta.Dot(camera.GlobalBasis.X.Normalized()) / scale,
                    delta.Dot(camera.GlobalBasis.Y.Normalized()) / scale), stage);
            }

            // Negative routing probes never touch the OS cursor or field controls.
            var negativePose = camera.Transform;
            var negativeTarget = controller.ViewTarget;
            var hud = aperture.Position - Vector2.One * 4;
            Require(!aperture.HasPoint(hud), "negative point must be outside aperture");
            foreach (var button in new[] { MouseButton.Right, MouseButton.Middle, MouseButton.WheelUp })
            {
                Send(new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = hud }, "HUD reject");
                Send(new InputEventMouseMotion { Position = hud, Relative = new Vector2(24, 8) }, "HUD reject motion");
                Send(new InputEventMouseButton { ButtonIndex = button, Pressed = false, Position = hud }, "HUD release");
            }
            Send(new InputEventMouseMotion { Position = aperture.GetCenter(), Relative = new Vector2(24, 8) }, "uncaptured motion");
            await Frame();
            Require(camera.Transform.IsEqualApprox(negativePose) && controller.ViewTarget.IsEqualApprox(negativeTarget), "HUD/uncaptured input moved camera");

            // Two pan planes span the target's full 3D displacement. These are
            // ordinary routed gestures, not a direct camera/target assignment.
            await Orbit(0, 0, "first pan plane");
            await Pan("first pan");
            await Orbit(Mathf.Pi / 2, 0, "second pan plane");
            await Pan("second pan");
            var normal = labels[0].GlobalBasis.Z.Normalized();
            Require(labels.All(label => label.GlobalBasis.Z.Normalized().Dot(normal) > .95f), "readout faces incompatible");
            var desiredForward = -normal;
            await Orbit(Mathf.Atan2(desiredForward.X, desiredForward.Z),
                Mathf.Asin(Mathf.Clamp(desiredForward.Y, -.99f, .99f)), "readout face orbit");
            await Pan("final center pan");
            float LineBoxPixels(Label3D label)
            {
                var font = label.Font ?? ThemeDB.FallbackFont;
                var height = font.GetStringSize("Mg", HorizontalAlignment.Left, -1, label.FontSize).Y * label.PixelSize;
                var offset = label.GlobalBasis.Y * (height / 2);
                return camera.UnprojectPosition(label.GlobalPosition + offset)
                    .DistanceTo(camera.UnprojectPosition(label.GlobalPosition - offset));
            }
            var wheels = 0;
            while (labels.Min(LineBoxPixels) < minimumLineBoxPixels && wheels++ < 48)
            {
                Send(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true,
                    Position = aperture.GetCenter() }, "readability zoom");
                await Frame();
            }
            Require(labels.All(label => LineBoxPixels(label) >= minimumLineBoxPixels), "18px line-box metric not reached within finite wheel cap");
            var observations = new List<object>();
            foreach (var label in labels)
            {
                var faces = label.GenerateTriangleMesh().GetFaces();
                Require(faces.Length > 0 && label.IsVisibleInTree(), "configured label extent missing/hidden");
                var world = faces.Select(vertex => label.GlobalTransform * vertex).ToArray();
                Require(world.All(point => !camera.IsPositionBehind(point)), "label behind camera");
                Require((camera.GlobalPosition - label.GlobalPosition).Dot(label.GlobalBasis.Z.Normalized()) > 0, "readout back face");
                var projected = world.Select(camera.UnprojectPosition).ToArray();
                var min = new Vector2(projected.Min(p => p.X), projected.Min(p => p.Y));
                var max = new Vector2(projected.Max(p => p.X), projected.Max(p => p.Y));
                var inset = aperture.Grow(-12);
                Require(min.X >= inset.Position.X && min.Y >= inset.Position.Y
                    && max.X <= inset.End.X && max.Y <= inset.End.Y, "full readout rectangle violates aperture margin: " + label.GetPath());
                observations.Add(new { path = label.GetPath().ToString(), label.Text,
                    lineBoxPixels = LineBoxPixels(label), min = new[] { min.X, min.Y }, max = new[] { max.X, max.Y },
                    configuredExtentVertices = faces.Length });
            }
            Require(poses.All(pair => pair.Key.Transform.IsEqualApprox(pair.Value)), "navigation changed field transforms");
            Require(points.Count == runtime.Points.Count && points.All(pair => Equals(pair.Value, runtime.Points[pair.Key])), "navigation changed runtime points");
            Require(!runtime.IsRunning && _virtualController?.Snapshot.State != VirtualControllerState.Running, "runtime started during navigation");
            System.IO.File.WriteAllText(path + ".user-readout.json", JsonSerializer.Serialize(new {
                sceneId = _currentSceneId, scope = "Scripted in-app PushInput ordinary navigation; not physical keyboard/mouse or usability approval",
                preparation = "See separate readout fixture sidecar; navigation does not write outputs or advance plant",
                passed = true, eventCount, minimumLineBoxPixels, initial, final = View(), observations, trace,
                limitations = "Configured label quad extent; actual PNG occlusion/readability review required; no per-glyph certification"
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("USER_READOUT_CAPTURE PASS scene=" + _currentSceneId + " events=" + eventCount);
            _userReadoutCapturePrepared = true;
            _userReadoutCapturePending = false;
            _captureFramesRemaining = 1;
        }
        catch (Exception ex)
        {
            if (path is not null) System.IO.File.WriteAllText(path + ".user-readout.json", JsonSerializer.Serialize(new {
                passed = false, sceneId = _currentSceneId, error = ex.Message, initial, eventCount, trace
            }, new JsonSerializerOptions { WriteIndented = true }));
            _capturePath = null;
            _userReadoutCapturePending = false;
            GD.PushError("USER_READOUT_CAPTURE FAIL " + ex.Message);
            GetTree().Quit(1);
        }
    }
}
