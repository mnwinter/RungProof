using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.Scenes;

internal static partial class Program
{
    private static void RegisterScenePlantFeedbackRegressionTests()
    {
        Test("Jug fill obeys physical eligibility and independent actuator commands", () =>
        {
            var jug = new JugFillPlantModel(-3.2, 3.5, .75, .4, -.4, .4, 1 / 2.2);
            jug.Advance(1, false, true, true, true); Equal(0.0, jug.Fraction); // No jug at station.
            jug.Advance(3.2 / .75, true, false, false, false); True(jug.Present);
            jug.Advance(.02, true, true, true, true); Equal(0.0, jug.Fraction); // Moving under nozzle cannot fill.
            jug.Advance(1, false, true, false, true); Equal(0.0, jug.Fraction);
            jug.Advance(1, false, false, true, true); Equal(0.0, jug.Fraction);
            jug.Advance(1, false, true, true, false); Equal(0.0, jug.Fraction);
            jug.Advance(1.1, false, true, true, true); True(Math.Abs(jug.Fraction - .5) < 1e-9);
            var position = jug.Position; jug.Advance(2, false, false, false, true);
            Equal(position, jug.Position); True(Math.Abs(jug.Fraction - .5) < 1e-9);
            jug.Advance(1.2, false, true, true, true); True(jug.High);
            jug.Advance(20, true, true, true, true); True(jug.AtExit); False(jug.Present); Equal(1.0, jug.Fraction);
            jug.Reset(); Equal(-3.2, jug.Position); Equal(0.0, jug.Fraction); False(jug.High);
        });
        Test("Assembly lift obeys separate commands and hard limits", () =>
        {
            var lift = new BidirectionalLiftPlantModel(2);
            True(lift.AtBottom); False(lift.AtTop); False(lift.CycleComplete);
            lift.Advance(.5, true, false); Equal(.25, lift.Position);
            lift.Advance(5, false, false); Equal(.25, lift.Position);
            lift.Advance(5, true, true); Equal(.25, lift.Position); // Conflicting actuator commands hold.
            lift.Advance(5, true, false); Equal(1.0, lift.Position); True(lift.AtTop); False(lift.AtBottom);
            False(lift.CycleComplete); // No plant-driven automatic return.
            lift.Advance(1, false, true); Equal(.5, lift.Position);
            lift.Advance(5, false, true); Equal(0.0, lift.Position); True(lift.CycleComplete);
            lift.Reset(); True(lift.AtBottom); False(lift.CycleComplete);
        });
        Test("Inbound scanner mechanics follow command and stopped identification", () =>
        {
            var plant = new InboundScanPlantModel(-3.2, 3.4, .75, .8, .5, .8);
            plant.Advance(2, false); Equal(-3.2, plant.Position); False(plant.ScanComplete);
            plant.Advance(4.8, true); True(plant.AtScanner); False(plant.ScanComplete);
            var stopped = plant.Position;
            plant.Advance(.4, false); Equal(stopped, plant.Position); False(plant.ScanComplete);
            plant.Advance(.02, true); False(plant.ScanComplete); // Movement cancels unfinished read.
            plant.Advance(.79, false); False(plant.ScanComplete);
            plant.Advance(.01, false); True(plant.ScanComplete);
            plant.Advance(10, true); Equal(3.4, plant.Position); True(plant.AtExit); False(plant.AtScanner);
            True(plant.ScanComplete); // Scanner result retained until physical reset.
            plant.Reset(); Equal(-3.2, plant.Position); False(plant.ScanComplete); False(plant.AtExit);
            plant.Advance(20, true); True(plant.AtExit); False(plant.ScanComplete); // No automatic stop/scan sequence.
        });
        Test("Vision class readout exposes the PC class cycle action", () =>
        {
            using var json = OpenOperatorScene("lab-10-03-vision-package-sorter");
            var scene = json.RootElement;
            var display = scene.GetProperty("equipment").EnumerateArray()
                .Single(e => e.GetProperty("id").GetString() == "training_accessory_6");
            var config = display.GetProperty("config");
            Equal("cycle-vision_class", config.GetProperty("action").GetString()!);
            Equal("CLASS NEXT", config.GetProperty("displayLabel").GetString()!);
            var simulation = scene.GetProperty("simulation");
            var action = simulation.GetProperty("actions").EnumerateArray()
                .Single(a => a.GetProperty("id").GetString() == config.GetProperty("action").GetString());
            Equal("cycle", action.GetProperty("type").GetString()!);
            Equal("vision_class", action.GetProperty("point").GetString()!);
            True(action.GetProperty("values").EnumerateArray().Select(v => v.GetInt32()).SequenceEqual(new[] { 1, 2, 3, 4 }));
            var binding = simulation.GetProperty("pointBindings").EnumerateArray()
                .Single(b => b.GetProperty("point").GetString() == "vision_class");
            Equal("training_accessory_6", binding.GetProperty("equipmentId").GetString()!);
            Equal("CLASS NEXT", binding.GetProperty("label").GetString()!);
            Equal("vision_result_valid", binding.GetProperty("validPoint").GetString()!);
            var (points, initial) = ReadOperatorScene(scene);
            Equal("PC", points.Single(p => p.Name == "vision_class").Owner);
            Equal(1.0, (double)initial["vision_class"]!);
        });
        Test("Scene common stop has a physical input route", () =>
        {
            using var json = OpenOperatorScene("lab-11-07-multi-conveyor-pallet-route");
            var scene = json.RootElement;
            var station = scene.GetProperty("equipment").EnumerateArray()
                .Single(e => e.GetProperty("id").GetString() == "training_accessory_8");
            var id = station.GetProperty("config").GetProperty("action").GetString();
            var action = scene.GetProperty("simulation").GetProperty("actions").EnumerateArray()
                .Single(a => a.GetProperty("id").GetString() == id);
            Equal("toggle", action.GetProperty("type").GetString()!);
            Equal("common_stop_request", action.GetProperty("point").GetString()!);
            var (points, initial) = ReadOperatorScene(scene);
            Equal("PC", points.Single(p => p.Name == "common_stop_request").Owner);
            True(initial["common_stop_request"] is false);
        });
        Test("Fume command indication follows output image and clears stale lamps", () =>
        {
            using var json = OpenOperatorScene("lab-2-15-fume-extractor");
            True(json.RootElement.GetProperty("simulation").GetProperty("fumeSpeedFeedback").GetBoolean());
            var (schema, _) = ReadOperatorScene(json.RootElement);
            foreach (var name in new[] { "low_speed", "medium_speed", "high_speed" })
                Equal("SIM", schema.Single(p => p.Name == name).Owner);
            var image = new Dictionary<string, object?>
            {
                ["fan_run"] = true, ["fan_speed_percent"] = 35,
                ["fan_selector_position"] = 3, ["hood_light_on"] = false
            };
            void Check(bool low, bool medium, bool high)
            {
                var run = image["fan_run"]; var speed = image["fan_speed_percent"];
                FumeExtractorFeedback.Project(image);
                Equal(low, (bool)image["low_speed"]!);
                Equal(medium, (bool)image["medium_speed"]!);
                Equal(high, (bool)image["high_speed"]!);
                True(Equals(run, image["fan_run"]) && Equals(speed, image["fan_speed_percent"]));
            }
            Check(true, false, false); // Output wins even when selector disagrees.
            image["fan_speed_percent"] = 65; image["hood_light_on"] = true; Check(false, true, false);
            image["fan_speed_percent"] = 100; Check(false, false, true);
            image["fan_run"] = false; Check(false, false, false);
            image["fan_run"] = true; image["fan_speed_percent"] = 0; Check(false, false, false);
            image["fan_speed_percent"] = 50; Check(false, false, false);
            image["fan_speed_percent"] = 35; Check(true, false, false);
        });
    }
}
