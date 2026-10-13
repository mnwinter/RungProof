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
    private sealed record NumericCycleCaptureContract(string EquipmentId, string Point, string Label, long[] Values)
    {
        public string Action => "cycle-" + Point;
    }

    private NumericCycleCaptureContract? PrepareNumericCycleInput()
    {
        var args = OS.GetCmdlineUserArgs();
        var requested = args.FirstOrDefault(a => a.StartsWith("--capture-numeric-cycle-input=", StringComparison.Ordinal))?
            .Substring("--capture-numeric-cycle-input=".Length);
        if (requested is null) return null;
        void Require(bool ok, string detail) { if (!ok) throw new InvalidOperationException("Numeric cycle: " + detail); }
        Require(args.Contains("--capture-user-readout-navigation"), "navigation flag required");
        Require(!args.Any(a => a.StartsWith("--capture-readout-state=", StringComparison.Ordinal)), "mixed readout fixture");
        Require(_virtualController is null && _virtualProgram is null && _simulatorShell?.IsExternalMode == false,
            "no loaded controller or external execution permitted");
        Require(!_visualSceneReview, "diagnostic scene review must be disabled");
        var ordinary = new long[] { 0, 1, 2, 5, 10 };
        var candidates = _currentSceneId switch
        {
            "lab-9-01-sum-function" or "lab-9-03-sum-and-counter-function" => new[] {
                new NumericCycleCaptureContract("numeric_display_0", "operand_a", "A NEXT", ordinary),
                new NumericCycleCaptureContract("numeric_display_1", "operand_b", "B NEXT", ordinary) },
            "lab-9-02-product-function" => new[] {
                new NumericCycleCaptureContract("numeric_display_0", "factor_a", "A NEXT", ordinary),
                new NumericCycleCaptureContract("numeric_display_1", "factor_b", "B NEXT", ordinary) },
            "lab-9-04-function-selector" => new[] {
                new NumericCycleCaptureContract("numeric_display_0", "operand_a", "A NEXT", ordinary),
                new NumericCycleCaptureContract("numeric_display_1", "operand_b", "B NEXT", ordinary),
                new NumericCycleCaptureContract("numeric_display_2", "function_choice", "FUNC NEXT", new long[] { 0, 1, 2, 99 }) },
            "lab-9-10-box-volume" => new[] {
                new NumericCycleCaptureContract("numeric_display_0", "length_mm", "L mm NEXT", new long[] { 0, 250, 500, 720, 850, 1000 }),
                new NumericCycleCaptureContract("numeric_display_1", "width_mm", "W mm NEXT", new long[] { 0, 250, 500, 720, 850, 1000 }),
                new NumericCycleCaptureContract("numeric_display_2", "height_mm", "H mm NEXT", new long[] { 0, 250, 500, 720, 850, 1000 }) },
            _ => System.Array.Empty<NumericCycleCaptureContract>(),
        };
        var contract = candidates.SingleOrDefault(c => c.EquipmentId == requested)
            ?? throw new InvalidOperationException("Numeric cycle: exact scene/input is not in the twelve-contract inventory");
        var scene = _activeSceneDefinition!;
        var item = scene.Equipment.Single(e => e.Id == requested);
        var action = scene.Simulation.GetProperty("actions").EnumerateArray()
            .Single(a => a.GetProperty("id").GetString() == contract.Action);
        var binding = scene.Simulation.GetProperty("pointBindings").EnumerateArray()
            .Single(b => b.GetProperty("equipmentId").GetString() == requested);
        var point = scene.Simulation.GetProperty("points").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == contract.Point);
        Require(item.Type == "trainingAccessory" && item.Config.GetProperty("catalogAssetId").GetString() == "training.accessory.count_display.v1"
            && item.Config.GetProperty("display").GetString() == "numeric" && item.Config.GetProperty("action").GetString() == contract.Action
            && item.Config.GetProperty("displayLabel").GetString() == contract.Label, "equipment/config drift");
        Require(action.GetProperty("type").GetString() == "cycle" && action.GetProperty("point").GetString() == contract.Point
            && action.GetProperty("values").EnumerateArray().Select(v => v.GetInt64()).SequenceEqual(contract.Values), "action/value-list drift");
        Require(binding.GetProperty("point").GetString() == contract.Point && binding.GetProperty("mode").GetString() == "numericDisplay"
            && binding.GetProperty("label").GetString() == contract.Label && !binding.TryGetProperty("format", out _), "numeric binding/default-G drift");
        Require(point.GetProperty("type").GetString() == "DINT" && point.GetProperty("owner").GetString() == "PC"
            && point.GetProperty("initial").GetInt64() == 0, "PC DINT initial contract drift");
        Require(_sceneRuntime!.UsesExternalClock && !_sceneRuntime.IsRunning, "stopped authored controller-clock policy required");
        ResetActiveController(); // Normal shell Reset callback, before navigation snapshots.
        Require(Convert.ToDouble(_sceneRuntime.Points[contract.Point], CultureInfo.InvariantCulture) == 0, "initial Reset did not restore zero");
        return contract;
    }

    private async Task VerifyNumericCycleInputCapture(NumericCycleCaptureContract contract)
    {
        var trace = new List<object>();
        var checks = 0;
        var path = _capturePath!;
        void Require(bool ok, string detail)
        {
            checks++;
            if (!ok) throw new InvalidOperationException("Numeric cycle: " + contract.EquipmentId + "/" + detail);
        }
        try
        {
            var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!; var camera = _sceneCamera!;
            var shell = _simulatorShell!; var picker = _sceneControlInteractor!;
            var display = root.GetNode<Node3D>(contract.EquipmentId);
            var readout = display.GetNode<Label3D>("NumericReadout");
            var baseline = runtime.Points.ToDictionary(pair => pair.Key, pair => pair.Value);
            var poses = root.FindChildren("*", "", true, false).OfType<Node3D>().Append(root).ToDictionary(n => n, n => n.Transform);
            var aperture = shell.SceneViewportRect(); var cameraPose = camera.Transform; var target = _cameraController!.ViewTarget;
            void Invariants()
            {
                Require(runtime.UsesExternalClock && !runtime.IsRunning && _virtualController is null && _virtualProgram is null
                    && !shell.IsExternalMode, "controller/plant/external execution changed");
                Require(runtime.Points.Count == baseline.Count && baseline.Where(p => p.Key != contract.Point)
                    .All(p => Equals(p.Value, runtime.Points[p.Key])), "non-target points/PLC outputs changed");
                Require(poses.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "field/housing pose changed");
                Require(camera.Transform.IsEqualApprox(cameraPose) && _cameraController.ViewTarget.IsEqualApprox(target)
                    && shell.SceneViewportRect() == aperture, "click/Reset changed navigated camera/aperture");
            }
            void Value(long expected, string stage)
            {
                Require(Convert.ToDouble(runtime.Points[contract.Point], CultureInfo.InvariantCulture) == expected, stage + " exact PC numeric value");
                Require(readout.Text == contract.Label + "\n" + expected.ToString("G", CultureInfo.InvariantCulture), stage + " exact readout/default-G");
                var inset = aperture.Grow(-12);
                var vertices = readout.GenerateTriangleMesh().GetFaces();
                Require(vertices.Length > 0 && vertices.All(vertex => {
                    var world = readout.GlobalTransform * vertex;
                    return !camera.IsPositionBehind(world) && inset.HasPoint(camera.UnprojectPosition(world));
                }), stage + " full changed-text extent inside aperture");
                Invariants();
            }
            async Task Click(long expected, string stage)
            {
                var at = camera.UnprojectPosition(readout.GlobalPosition);
                Require(aperture.Grow(-12).HasPoint(at) && !camera.IsPositionBehind(readout.GlobalPosition), "target outside ordinary aperture");
                Require(picker.TryPick(camera, at, out var picked) && picked?.EquipmentId == contract.EquipmentId
                    && picked.ActionId == contract.Action, "exact pre-click pick identity");
                var before = runtime.Points[contract.Point];
                GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Value(expected, stage + " press");
                var pressed = runtime.Points[contract.Point];
                GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Value(expected, stage + " release-not-second-cycle");
                trace.Add(new { stage, routed = "Viewport.PushInput left press/release", screen = new[] { at.X, at.Y },
                    pickedEquipment = picked!.EquipmentId, pickedAction = picked.ActionId, before, pressed,
                    released = runtime.Points[contract.Point], readout = readout.Text });
            }
            void AuthoredInitials()
            {
                foreach (var point in _activeSceneDefinition!.Simulation.GetProperty("points").EnumerateArray())
                {
                    var name = point.GetProperty("name").GetString()!;
                    var initial = point.GetProperty("initial");
                    var actual = runtime.Points[name];
                    var matches = initial.ValueKind switch {
                        JsonValueKind.Number => Convert.ToDouble(actual, CultureInfo.InvariantCulture) == initial.GetDouble(),
                        JsonValueKind.True or JsonValueKind.False => actual is bool flag && flag == initial.GetBoolean(),
                        JsonValueKind.String => Equals(actual, initial.GetString()),
                        JsonValueKind.Null => actual is null,
                        _ => false
                    };
                    Require(matches, "authored initial mismatch: " + name);
                }
            }
            AuthoredInitials();
            Value(0, "initial");
            foreach (var value in contract.Values.Skip(1).Append(contract.Values[0])) await Click(value, "full-wrap");
            await Click(contract.Values[1], "pre-Reset-nonzero");
            ResetActiveController();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            AuthoredInitials();
            Value(0, "normal Reset from nonzero after full-wrap");
            trace.Add(new { stage = "normal Reset", point = runtime.Points[contract.Point], readout = readout.Text });
            await Click(contract.Values[1], "final-image-state-after-Reset-proof");
            System.IO.File.WriteAllText(path + ".numeric-cycle.json", JsonSerializer.Serialize(new {
                passed = true, sceneId = _currentSceneId, contract.EquipmentId, contract.Action, contract.Point,
                owner = "PC", type = "DINT", values = contract.Values, format = "G", label = contract.Label,
                checks, failures = 0, trace, finalValue = runtime.Points[contract.Point], finalText = readout.Text,
                scope = "Actual routed in-app click/cycle/release/Reset; no PLC program, plant advance or physical OS input",
                imagePhase = "Final nonzero click after full-wrap and Reset proof; not Reset image",
                limitation = "Bound-mesh picking; actual PNG occlusion/readability review remains required"
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("NUMERIC_CYCLE_CAPTURE PASS scene=" + _currentSceneId + " equipment=" + contract.EquipmentId + " checks=" + checks);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(path + ".numeric-cycle.json", JsonSerializer.Serialize(new {
                passed = false, sceneId = _currentSceneId, contract.EquipmentId, checks, error = ex.Message, trace
            }, new JsonSerializerOptions { WriteIndented = true }));
            throw; // Parent navigation handler clears capture and exits nonzero.
        }
    }
}
