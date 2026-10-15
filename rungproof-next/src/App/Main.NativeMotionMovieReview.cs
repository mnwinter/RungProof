using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _nativeMotionMovie;
    private string _nativeMotionProject = "", _nativeMotionAngle = "", _nativeMotionTracePath = "";
    private int _nativeMotionRoute = 1, _nativeMotionFrames, _nativeMotionCompletedFrames;
    private StreamWriter? _nativeMotionTrace;
    private bool _nativeMotionTraceConnected;

    // Explicit local QA only. The movie writer owns capture; this helper owns
    // setup and normal UI commands, never plant state or derived feedback.
    private bool ConfigureNativeMotionMovie(string[] arguments)
    {
        _nativeMotionMovie = arguments.Contains("--native-motion-review", StringComparer.Ordinal);
        if (!_nativeMotionMovie) return true;
        string Option(string key) => arguments.FirstOrDefault(a => a.StartsWith(key + "=", StringComparison.Ordinal))?
            .Substring(key.Length + 1) ?? "";
        _nativeMotionProject = Option("--native-motion-project");
        _nativeMotionAngle = Option("--native-motion-angle");
        _nativeMotionTracePath = Option("--native-motion-trace");
        var route = Option("--native-motion-route");
        if (route.Length > 0 && !int.TryParse(route, NumberStyles.None, CultureInfo.InvariantCulture, out _nativeMotionRoute))
            _nativeMotionRoute = 0;
        if (!_visualSceneReview || _visualPlantReview || !_appShellRequested
            || !Path.GetExtension(Engine.GetWriteMoviePath()).Equals(".png", StringComparison.OrdinalIgnoreCase)
            || DisplayServer.GetName() == "headless" || _sceneId is not null || _mcpProjectPath is not null
            || _shellScene is not ("lab-10-03-vision-package-sorter" or "lab-2-17-pallet-robot")
            || _nativeMotionAngle is not ("FR" or "FL" or "RL" or "RR" or "Top")
            || _nativeMotionRoute is < 1 or > 4
            || (_shellScene == "lab-2-17-pallet-robot" && _nativeMotionRoute != 1)
            || !File.Exists(_nativeMotionProject) || _nativeMotionTracePath.Length == 0)
        {
            GD.PushError("NATIVE_MOTION_REVIEW_REJECTED: requires native shell visual review, lossless PNG capture, supported scene/angle/route, existing project and trace path.");
            GetTree().Quit(1);
            return false;
        }
        return true;
    }

    private void StartNativeMotionMovie()
    {
        try
        {
            if (_simulatorShell is null || _sceneRuntime is null || _simulatorShell.IsExternalMode)
                throw new InvalidOperationException("Only the disconnected built-in controller is supported.");
            var loaded = LadderEditorProjectJson.Load(File.ReadAllText(_nativeMotionProject));
            if (!loaded.IsReadable || loaded.Document is null || loaded.Document.SourceSceneId != _currentSceneId
                || loaded.Document.BuildProgram().ScanPeriod != TimeSpan.FromMilliseconds(20))
                throw new InvalidOperationException("Project must match the selected scene and use 20 ms scans.");
            if (!LadderCompiler.Compile(loaded.Document.BuildProgram()).IsValid)
                throw new InvalidOperationException("Project compilation rejected; capture will not retain an older controller.");
            var previousController = _virtualController;
            if (!_simulatorShell.OpenNativeMotionReviewProject(_nativeMotionProject, out var message)
                || _virtualController is null || ReferenceEquals(previousController, _virtualController))
                throw new InvalidOperationException("Ordinary project verification/load failed: " + message);
            _nativeMotionTrace = new StreamWriter(_nativeMotionTracePath, false) { AutoFlush = true };
            _nativeMotionTrace.WriteLine(JsonSerializer.Serialize(new {
                kind = "setup", scene = _currentSceneId, angle = _nativeMotionAngle, route = _nativeMotionRoute,
                projectSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(_nativeMotionProject))),
                transport = "none", clock = "ordinary PhysicsProcess/controller Advance", scanPeriodMs = 20
            }));
            SetVisualReviewAngle(_nativeMotionAngle switch {
                "FR" => new Vector3(11,7,12), "FL" => new Vector3(-11,7,12),
                "RL" => new Vector3(-11,7,-12), "RR" => new Vector3(11,7,-12),
                _ => new Vector3(0,20,.01f)
            }, "native-movie-" + _nativeMotionAngle);
            if (_currentSceneId == "lab-10-03-vision-package-sorter")
            {
                for (var selection = 0; selection < 4 && Convert.ToInt32(_sceneRuntime.Points["vision_class"], CultureInfo.InvariantCulture) != _nativeMotionRoute; selection++)
                    if (!ExecuteSelectedControllerAction("cycle-vision_class")) throw new InvalidOperationException("Class selection rejected.");
                if (Convert.ToInt32(_sceneRuntime.Points["vision_class"], CultureInfo.InvariantCulture) != _nativeMotionRoute)
                    throw new InvalidOperationException("Requested vision class was not selected.");
                foreach (var action in new[] { "toggle-package_present", "toggle-vision_result_valid", "toggle-destination_clear" })
                    if (!ExecuteSelectedControllerAction(action)) throw new InvalidOperationException("Fixture action rejected: " + action);
            }
            RunActiveController();
            if (_virtualController.Snapshot.State != VirtualControllerState.Running)
                throw new InvalidOperationException("Ordinary Run did not reach Running.");
            if (_currentSceneId == "lab-2-17-pallet-robot" && !ExecuteSelectedControllerAction("start-robot"))
                throw new InvalidOperationException("Robot operator start rejected.");
            RenderingServer.FramePostDraw += TraceNativeMotionFrame;
            _nativeMotionTraceConnected = true;
            GD.Print("NATIVE_MOTION_REVIEW_STARTED scene=" + _currentSceneId + " angle=" + _nativeMotionAngle + " route=" + _nativeMotionRoute);
        }
        catch (Exception exception)
        {
            GD.PushError("NATIVE_MOTION_REVIEW_FAILED: " + exception.Message);
            _nativeMotionTrace?.Dispose();
            GetTree().Quit(1);
        }
    }

    private void TraceNativeMotionFrame()
    {
        if (_nativeMotionTrace is null || _sceneRuntime is null || _virtualController is null) return;
        var snapshot = _virtualController.Snapshot;
        _nativeMotionTrace.WriteLine(JsonSerializer.Serialize(new {
            kind = "frame", renderedFrame = Engine.GetFramesDrawn(), reviewFrame = _nativeMotionFrames++,
            timeSeconds = snapshot.SimulatedTime.TotalSeconds, scan = snapshot.ScanNumber,
            camera = _mainCamera!.Position.ToString(), points = _sceneRuntime.Points,
            // Review coordinates only: preserve native pixels around moving
            // loads without estimating screen positions from world motion.
            loadScreens = _sceneCompositionRoot!.GetChildren().OfType<Node3D>()
                .Where(n => n.Name.ToString() is "box_1" or "robot_pallet" or "container_a" or "container_b")
                .Select(n => {
                    var bounds = ReviewBounds(n);
                    var center = _mainCamera.UnprojectPosition(bounds.GetCenter());
                    return new { id = n.Name.ToString(), x = center.X, y = center.Y };
                }).ToArray(),
            equipment = _sceneCompositionRoot!.GetChildren().OfType<Node3D>().ToDictionary(n => n.Name.ToString(), n => new {
                position = n.Position.ToString(), rotation = n.RotationDegrees.ToString()
            })
        }));
        var donePoint = _currentSceneId == "lab-10-03-vision-package-sorter" ? "sort_complete" : "cycle_complete";
        if (_sceneRuntime.Points.GetValueOrDefault(donePoint) is true) _nativeMotionCompletedFrames++;
        else _nativeMotionCompletedFrames = 0;
        if (_nativeMotionCompletedFrames >= 10 || snapshot.SimulatedTime.TotalSeconds > 65 || _nativeMotionFrames >= 4000)
        {
            var passed = _nativeMotionCompletedFrames >= 10;
            RenderingServer.FramePostDraw -= TraceNativeMotionFrame;
            _nativeMotionTraceConnected = false;
            _nativeMotionTrace.WriteLine(JsonSerializer.Serialize(new { kind = "end", completed = passed, frames = _nativeMotionFrames }));
            _nativeMotionTrace.Dispose(); _nativeMotionTrace = null;
            GD.Print("NATIVE_MOTION_REVIEW_" + (passed ? "COMPLETE" : "TIMEOUT") + " frames=" + _nativeMotionFrames);
            GetTree().Quit(passed ? 0 : 1);
        }
    }
}
