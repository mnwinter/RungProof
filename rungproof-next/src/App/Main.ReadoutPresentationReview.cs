using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    // Explicit capture-only fixtures: never learner assessment or live PLC evidence.
    private void PrepareReadoutPresentationCapture()
    {
        var argument = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture-readout-state=", StringComparison.Ordinal));
        if (argument is null) return;
        try
        {
            var state = argument["--capture-readout-state=".Length..];
            var runtime = _sceneRuntime ?? throw new InvalidOperationException("Readout capture requires runtime");
            var root = _sceneCompositionRoot ?? throw new InvalidOperationException("Readout capture requires composition");
            void Require(bool ok, string detail) { if (!ok) throw new InvalidOperationException("Readout capture: " + detail); }
            object evidence;
            if (state == "ev-unequal")
            {
                Require(_currentSceneId == "lab-9-12-ev-charging-manager", "EV exact scene required");
                runtime.ResetSimulation();
                Require(Convert.ToDouble(runtime.Points["energy_kwh"]) == 0 && Convert.ToDouble(runtime.Points["bay_b_energy_kwh"]) == 0, "EV reset must clear both PLC display values");
                var feedbackA = runtime.Points["meter_energy_kwh"];
                var feedbackB = runtime.Points["bay_b_meter_energy_kwh"];
                runtime.CommitVirtualControllerNumericOutputs(new Dictionary<string, double>
                    { ["energy_kwh"] = 1.234, ["bay_b_energy_kwh"] = 5.678 });
                var observations = new List<object>();
                foreach (var fixture in new[] { (Id: "training_accessory_5", Point: "energy_kwh", Bay: "A", Value: "1.234"),
                    (Id: "ev_meter_b", Point: "bay_b_energy_kwh", Bay: "B", Value: "5.678") })
                {
                    var meter = root.GetNode<Node3D>(fixture.Id);
                    var numeric = meter.GetNode<Label3D>("NumericReadout");
                    Require(numeric.Text.Contains("kWh", StringComparison.Ordinal) && numeric.Text.Contains(fixture.Value, StringComparison.Ordinal), fixture.Id + " unequal projection");
                    var plate = root.GetNode<Node3D>("ControlInstallation").GetNode<MeshInstance3D>(meter.GetMeta("installationIdentification").AsString());
                    Require(plate.GetMeta("sourcePoint").AsString() == fixture.Point && plate.GetMeta("sourcePointOwner").AsString() == "PLC", fixture.Id + " exact identity provenance");
                    var ink = plate.GetParent().GetNode<Label3D>(plate.Name + "_Ink");
                    Require(ink.Text == "BAY " + fixture.Bay + "\nPLC kWh", fixture.Id + " bay caption");
                    observations.Add(new { fixture.Id, fixture.Point, caption = ink.Text, numeric = numeric.Text });
                }
                Require(Equals(feedbackA, runtime.Points["meter_energy_kwh"]) && Equals(feedbackB, runtime.Points["bay_b_meter_energy_kwh"]), "QA outputs must not alter measured feedback");
                evidence = new { scope = "Explicit unequal PLC-output projection fixture, not learner/controller result", observations };
            }
            else
            {
                Require(_currentSceneId == "lab-10-02-chicken-label-print", "Chicken exact scene required");
                Require(new[] { "load", "weighing", "stable", "changed-mass", "reset" }.Contains(state), "unknown state");
                runtime.ResetSimulation();
                runtime.UsesExternalClock = true;
                if (state is "weighing" or "stable" or "changed-mass")
                {
                    Require(runtime.ExecuteAction("toggle-product_weighed"), "normal load action rejected");
                    if (state == "changed-mass")
                    {
                        // Existing model API is deliberately accessed only in this QA fixture.
                        var field = typeof(SceneSimulationRuntime).GetField("_chicken", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException("Missing existing Chicken plant field");
                        var plant = field.GetValue(runtime) as ChickenLabelPlantModel
                            ?? throw new InvalidOperationException("Missing existing Chicken plant");
                        plant.LoadProduct(2.468);
                    }
                    runtime.SetControllerPlaybackRunning(true);
                    runtime.AdvanceSimulation(state == "weighing" ? .25 : .5);
                }
                if (state == "reset")
                {
                    Require(runtime.ExecuteAction("toggle-product_weighed"), "reset fixture load rejected");
                    runtime.SetControllerPlaybackRunning(true);
                    runtime.AdvanceSimulation(.5);
                    runtime.ResetSimulation();
                }
                runtime.SetControllerPlaybackRunning(false);
                var scale = root.GetNode<Node3D>("training_accessory_5");
                var label = scale.GetNode<Label3D>("MeasuredWeight");
                var expected = state == "weighing" ? "WEIGHING..." : state == "stable" ? "MEASURED 1.237 kg" : state == "changed-mass" ? "MEASURED 2.468 kg" : "LOAD PRODUCT";
                var expectedKg = state == "stable" ? 1.237 : state == "changed-mass" ? 2.468 : 0;
                Require(label.Text == expected && Math.Abs(Convert.ToDouble(runtime.Points["weight_kg"]) - expectedKg) < 1e-9, "plant-owned text/value mismatch");
                Require(label.Billboard == BaseMaterial3D.BillboardModeEnum.Disabled, "screen text must not billboard");
                var screen = scale.FindChild("WEIGH_readout_screen", true, false) as MeshInstance3D
                    ?? throw new InvalidOperationException("Missing retained screen");
                var bounds = screen.GetAabb();
                var vertices = label.GenerateTriangleMesh().GetFaces().Select(v => screen.GlobalTransform.AffineInverse() * (label.GlobalTransform * v)).ToArray();
                Require(vertices.Length > 0 && vertices.All(v => v.X >= bounds.Position.X + .01f && v.X <= bounds.End.X - .01f
                    && v.Y >= bounds.Position.Y + .01f && v.Y <= bounds.End.Y - .01f
                    && v.Z > bounds.End.Z && v.Z < bounds.End.Z + .004f), "actual glyph mesh outside screen inset/outward face");
                Require(!scale.FindChildren("WEIGH_readout_static_text", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Any(m => m.Visible), "static imported text visible");
                evidence = new { scope = "Normal load/advance/reset APIs; changed mass uses explicit existing-model QA fixture, not learner result", text = label.Text, weightKg = expectedKg, glyphVertices = vertices.Length, screenInsetM = .01, outwardDepthM = .0015 };
            }
            runtime.SetControllerPlaybackRunning(false);
            using var file = FileAccess.Open(_capturePath + ".readout.json", FileAccess.ModeFlags.Write)
                ?? throw new InvalidOperationException("Cannot write readout fixture evidence");
            file.StoreString(System.Text.Json.JsonSerializer.Serialize(new { state, sceneId = _currentSceneId, evidence }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            GD.Print("READOUT_PRESENTATION_CAPTURE PASS state=" + state);
        }
        catch (Exception error)
        {
            _capturePath = null;
            GD.PushError("READOUT_PRESENTATION_CAPTURE FAIL " + error);
            GetTree().Quit(1);
        }
    }
}
