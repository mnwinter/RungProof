using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _retainedOriginalClock;
    private sealed record RetainedCaptureContract(string Scene, string Equipment, string Action, string Point, string ActionType, string Owner, string Head);

    private RetainedCaptureContract RetainedContract(string id) => (_currentSceneId, id) switch
    {
        ("equipment-gallery", "gallery_switch") => new(_currentSceneId, id, "toggle-gallery", "gallery_animation", "toggle", "SIM", "KIN_pushbutton"),
        ("equipment-gallery", "gallery_estop") => new(_currentSceneId, id, "stop-all", "gallery_animation", "stop", "SIM", "KIN_estop"),
        ("equipment-gallery", "gallery_selector") => new(_currentSceneId, id, "toggle-gallery", "gallery_animation", "toggle", "SIM", "KIN_selector_handle"),
        ("lab-2-16-safe-drill", "left_hand_button") => new(_currentSceneId, id, "toggle-left-hand", "left_hand_request", "togglePoint", "PC", "KIN_pushbutton"),
        ("lab-2-16-safe-drill", "right_hand_button") => new(_currentSceneId, id, "toggle-right-hand", "right_hand_request", "togglePoint", "PC", "KIN_pushbutton"),
        ("lab-5-07-pedestrian-crossing", "switch_2") => new(_currentSceneId, id, "toggle-crossing_request", "crossing_request", "pulse", "PC", "KIN_pushbutton"),
        _ => throw new InvalidOperationException("Retained capture: scene/equipment outside exact six contracts")
    };

    private string? PrepareRetainedControlCapture()
    {
        var args = OS.GetCmdlineUserArgs();
        var flags = args.Where(a => a.StartsWith("--capture-retained-control=", StringComparison.Ordinal)).ToArray();
        if (flags.Length == 0) return null;
        if (flags.Length != 1) throw new InvalidOperationException("Retained capture: exactly one target required");
        var id = flags[0].Substring("--capture-retained-control=".Length);
        var c = RetainedContract(id);
        void Require(bool value, string why) { if (!value) throw new InvalidOperationException("Retained capture: " + why); }
        Require(args.Contains("--capture-user-readout-navigation") && _capturePath is not null, "ordinary navigation/capture required");
        Require(!args.Any(a => a.StartsWith("--capture-numeric-cycle-input=", StringComparison.Ordinal)
            || a.StartsWith("--capture-readout-state=", StringComparison.Ordinal)), "mixed input/readout fixtures");
        Require(!_visualSceneReview && _virtualController is null && _virtualProgram is null
            && _simulatorShell?.IsExternalMode == false && !_simulatorShell.IsChallengeDraftActive, "local unloaded non-challenge capture required");
        var scene = _activeSceneDefinition!;
        var item = scene.Equipment.Single(e => e.Id == id);
        Require(item.Config.GetProperty("action").GetString() == c.Action
            && item.Type == (id == "gallery_selector" ? "rotarySwitch" : "switch"), "equipment action/type drift");
        if (id != "gallery_selector") Require(item.Config.GetProperty("style").GetString()
            == (id == "gallery_estop" ? "emergency" : "pushbutton"), "head style drift");
        else Require(item.Config.GetProperty("positionCount").GetInt32() == 4
            && item.Config.GetProperty("initialPosition").GetInt32() == 2, "selector authored pose drift");
        var action = scene.Simulation.GetProperty("actions").EnumerateArray().Single(a => a.GetProperty("id").GetString() == c.Action);
        Require(action.GetProperty("type").GetString() == c.ActionType, "action semantics drift");
        if (c.ActionType != "stop") Require(action.GetProperty("point").GetString() == c.Point, "action point drift");
        var point = scene.Simulation.GetProperty("points").EnumerateArray().Single(p => p.GetProperty("name").GetString() == c.Point);
        Require(point.GetProperty("type").GetString() == "BOOL" && point.GetProperty("owner").GetString() == c.Owner
            && point.GetProperty("initial").ValueKind == JsonValueKind.False, "point type/owner/initial drift");
        if (id == "gallery_estop")
        {
            var safe = scene.Simulation.GetProperty("safeState");
            Require(safe.EnumerateObject().Count() == 3 && !safe.GetProperty("gallery_animation").GetBoolean()
                && !safe.GetProperty("industrial_fan_run").GetBoolean() && safe.GetProperty("emergency_stop").GetBoolean(), "gallery safe-state drift");
        }
        _retainedOriginalClock = _sceneRuntime!.UsesExternalClock;
        // Explicit QA hold: command acceptance remains real; normal-running motion
        // is covered separately. Never advance the plant or load a controller here.
        _sceneRuntime.UsesExternalClock = true;
        _sceneRuntime.SetControllerPlaybackRunning(false);
        ResetActiveController();
        return id;
    }

    private async Task VerifyRetainedControlCapture(string equipmentId)
    {
        var trace = new List<object>(); var checks = 0;
        var path = _capturePath!; var c = RetainedContract(equipmentId);
        void Require(bool value, string why) { checks++; if (!value) throw new InvalidOperationException("Retained capture: " + why); }
        try
        {
            var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!; var camera = _sceneCamera!;
            var picker = _sceneControlInteractor!; var shell = _simulatorShell!;
            var roots = _activeSceneDefinition!.Equipment.ToDictionary(e => e.Id, e => root.GetNode<Node3D>(e.Id).Transform);
            void Policy()
            {
                Require(runtime.UsesExternalClock && _virtualController is null && _virtualProgram is null && !shell.IsExternalMode,
                    "diagnostic clock/controller policy changed");
                Require(roots.All(p => root.GetNode<Node3D>(p.Key).Transform.IsEqualApprox(p.Value)), "fixed equipment roots moved");
            }
            void Initials()
            {
                foreach (var p in _activeSceneDefinition.Simulation.GetProperty("points").EnumerateArray())
                {
                    var name = p.GetProperty("name").GetString()!; var initial = p.GetProperty("initial"); var actual = runtime.Points[name];
                    Require(initial.ValueKind switch {
                        JsonValueKind.True or JsonValueKind.False => actual is bool b && b == initial.GetBoolean(),
                        JsonValueKind.Number => Convert.ToDouble(actual, CultureInfo.InvariantCulture) == initial.GetDouble(),
                        JsonValueKind.String => Equals(actual, initial.GetString()), JsonValueKind.Null => actual is null, _ => false
                    }, "Reset authored initial: " + name);
                }
            }
            async Task Routed(RetainedCaptureContract contract, bool expected, string stage)
            {
                await NavigateRetainedControlCapture(contract.Equipment);
                var binding = picker.Bindings.Single(b => b.EquipmentId == contract.Equipment && b.ActionId == contract.Action);
                var head = binding.Node.FindChild(contract.Head, true, false) as Node3D
                    ?? throw new InvalidOperationException("Retained capture: exact head missing");
                Require(head.IsVisibleInTree(), "head hidden");
                var meshes = head.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                    .Concat(head is MeshInstance3D own ? new[] { own } : System.Array.Empty<MeshInstance3D>()).Where(m => m.IsVisibleInTree()).ToArray();
                var corners = meshes.SelectMany(m => Enumerable.Range(0, 8).Select(i => {
                    var a = m.GetAabb(); return m.GlobalTransform * (a.Position + new Vector3((i & 1) == 0 ? 0 : a.Size.X,
                        (i & 2) == 0 ? 0 : a.Size.Y, (i & 4) == 0 ? 0 : a.Size.Z));
                })).ToArray();
                Require(corners.Length > 0, "head visible mesh bounds missing");
                var center = corners.Aggregate(Vector3.Zero, (s, v) => s + v) / corners.Length;
                var aperture = shell.SceneViewportRect(); var inset = aperture.Grow(-12);
                Require(corners.All(v => !camera.IsPositionBehind(v) && inset.HasPoint(camera.UnprojectPosition(v))), "full head lacks 12px aperture margin");
                Require((camera.GlobalPosition - center).Dot(RetainedOperatorFaceNormal(binding.Node)) > 0, "operator face points away");
                var at = camera.UnprojectPosition(center);
                Require(picker.TryPick(camera, at, out var pick) && pick?.EquipmentId == contract.Equipment && pick.ActionId == contract.Action, "wrong actual head pick");
                var before = runtime.Points.ToDictionary(p => p.Key, p => p.Value);
                Require(before[contract.Point] is bool prior && prior != expected, "click must cause a real point transition");
                var home = head.Transform; var cameraHome = camera.Transform; var viewTarget = _cameraController!.ViewTarget;
                var fixedNodes = binding.Node.FindChildren("*", "", true, false).OfType<Node3D>().Append(binding.Node)
                    .Where(n => n != head).ToDictionary(n => n, n => n.Transform);
                void Values()
                {
                    Require(runtime.Points[contract.Point] is bool b && b == expected, "accepted point value");
                    foreach (var p in before.Where(p => p.Key != contract.Point))
                    {
                        var allowed = contract.ActionType == "stop" && _activeSceneDefinition.Simulation.GetProperty("safeState").TryGetProperty(p.Key, out var safe);
                        if (allowed) Require(runtime.Points[p.Key] is bool flag && flag == _activeSceneDefinition.Simulation.GetProperty("safeState").GetProperty(p.Key).GetBoolean(), "safe-state value " + p.Key);
                        else Require(Equals(p.Value, runtime.Points[p.Key]), "non-target point changed " + p.Key);
                    }
                    Require(runtime.Points.Count == before.Count, "point inventory changed");
                    Require(fixedNodes.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "control housing/child transforms changed");
                    Require(camera.Transform.IsEqualApprox(cameraHome) && _cameraController.ViewTarget.IsEqualApprox(viewTarget)
                        && shell.SceneViewportRect() == aperture, "click moved navigated camera/aperture"); Policy();
                }
                GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at }, true);
                Values();
                var changed = false;
                // Only the existing feedback clock advances naturally between frames.
                // This does not call AdvanceSimulation/ExecuteAction/AdvanceFeedback.
                var began = Time.GetTicksMsec();
                for (var frames = 0; frames < 180 && Time.GetTicksMsec() - began < 500; frames++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    changed |= !head.Transform.IsEqualApprox(home); Values();
                }
                Require(contract.Head == "KIN_selector_handle" ? !changed : changed, "supported press feedback mismatch");
                GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); Values();
                Require(head.Transform.IsEqualApprox(home), "timed feedback did not return authored home");
                trace.Add(new { stage, contract.Equipment, contract.Action, contract.Point, contract.ActionType,
                    routed = "Viewport.PushInput left press/release", before, after = runtime.Points.ToDictionary(p => p.Key, p => p.Value),
                    screen = new[] { at.X, at.Y }, apertureMarginPixels = 12, changed, restoredHome = true,
                    head = head.GetPath().ToString(), limitation = "Bound-mesh picker; PNG occlusion and readability review required" });
            }
            Initials(); Policy();
            if (equipmentId == "gallery_estop") await Routed(RetainedContract("gallery_switch"), true, "routed primer");
            await Routed(c, c.ActionType != "stop", "target press/release");
            var targetHead = root.GetNode<Node3D>(equipmentId).FindChild(c.Head, true, false) as Node3D;
            var settled = targetHead!.Transform;
            ResetActiveController(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Initials(); Policy(); Require(targetHead.Transform.IsEqualApprox(settled), "Reset changed authored head pose");
            trace.Add(new { stage = "normal Reset", allAuthoredInitials = true });
            System.IO.File.WriteAllText(path + ".retained-control.json", JsonSerializer.Serialize(new {
                passed = true, sceneId = _currentSceneId, equipmentId, c.Action, c.Point, c.ActionType, checks, trace,
                originalUsesExternalClock = _retainedOriginalClock, effectiveUsesExternalClock = runtime.UsesExternalClock,
                diagnosticPlaybackHold = true, imagePhase = "After normal Reset, navigated target unchanged",
                limitation = "Scripted app input and diagnostic held plant; not physical OS input, normal-running motion, safety or all77 discoverability proof"
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("RETAINED_CONTROL_CAPTURE PASS scene=" + _currentSceneId + " equipment=" + equipmentId + " checks=" + checks);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(path + ".retained-control.json", JsonSerializer.Serialize(new {
                passed = false, sceneId = _currentSceneId, equipmentId, checks, error = ex.Message, trace
            }, new JsonSerializerOptions { WriteIndented = true }));
            throw; // Parent navigation handler clears pending capture and exits nonzero.
        }
    }
}
