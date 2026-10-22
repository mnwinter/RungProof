using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static readonly (string Scene, string Action, string Request, string Output)[] OperatorCommands =
    [
        ("lab-2-11-inbound-tote-stop", "start-cycle", "start_cycle_request", "conveyor_run"),
        ("lab-2-12-assembly-lift", "raise-lift", "raise_request", "lift_up"),
        ("lab-2-12-assembly-lift", "lower-lift", "lower_request", "lift_down"),
        ("lab-2-13-coolant-jug-fill", "start-fill", "start_fill_request", "conveyor_run"),
        ("lab-2-16-safe-drill", "start-drill", "start_drill_request", "drill_run"),
        ("lab-2-19-service-door", "open-door", "open_request", "motor_open"),
        ("lab-2-19-service-door", "close-door", "close_request", "motor_close"),
        ("lab-2-23-parcel-sorter", "start-sort", "start_sort_request", "conveyors_run"),
        ("lab-2-24-robot-cnc", "start-cnc", "start_cnc_request", "infeed_run")
    ];

    private static void RegisterSceneOperatorCommandBindingTests()
    {
        foreach (var command in OperatorCommands)
            Test("SceneOperatorCommandBinding " + command.Scene + "/" + command.Action,
                () => CheckOperatorCommand(command.Scene, command.Action, command.Request, command.Output));
        Test("SceneOperatorCommandBinding existing draft migration", CheckOperatorCommandMigration);
        Test("TankOperatorRequests maintained inputs and ladder ownership", CheckTankOperatorRequests);
    }

    private static (List<SceneIoPoint> Points, Dictionary<string, object?> Initial) ReadOperatorScene(JsonElement scene)
    {
        var points = new List<SceneIoPoint>();
        var initial = new Dictionary<string, object?>();
        foreach (var point in scene.GetProperty("simulation").GetProperty("points").EnumerateArray())
        {
            var name = point.GetProperty("name").GetString()!;
            points.Add(new(name, point.GetProperty("type").GetString()!, point.GetProperty("owner").GetString()!, "", ""));
            var value = point.GetProperty("initial");
            initial[name] = value.ValueKind switch
            {
                JsonValueKind.True => true, JsonValueKind.False => false,
                JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), _ => null
            };
        }
        return (points, initial);
    }

    private static JsonDocument OpenOperatorScene(string scene) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(FindProjectRoot(), "scenes", "migrated", scene + ".scene.json")));

    private static void CheckOperatorCommand(string sceneId, string actionId, string request, string output)
    {
        using var json = OpenOperatorScene(sceneId);
        var scene = json.RootElement;
        var action = scene.GetProperty("simulation").GetProperty("actions").EnumerateArray().Single(a => a.GetProperty("id").GetString() == actionId);
        Equal("start", action.GetProperty("type").GetString()!);
        Equal(request, action.GetProperty("controllerStartBinding").GetString()!);
        True(scene.GetProperty("equipment").EnumerateArray().Any(e => e.TryGetProperty("config", out var c)
            && c.TryGetProperty("action", out var a) && a.GetString() == actionId));
        var (points, initial) = ReadOperatorScene(scene);
        var point = points.Single(p => p.Name == request);
        Equal("PC", point.Owner); Equal("BOOL", point.Type); True(initial[request] is false);
        Equal("PLC", points.Single(p => p.Name == output).Owner);
        var document = SceneLadderProject.Create(sceneId, "Command binding regression", points, initial, out _);
        True(document.Blocks.All(b => b.Rungs.Count == 0)); // Metadata must never supply the learner's sequence.
        var inputTag = document.Tags.Single(t => t.Binding == request);
        var outputTag = document.Tags.Single(t => t.Binding == output);
        Equal(PlcVariableRole.Input, inputTag.Role); Equal(PlcVariableType.Bool, inputTag.Type);
        document.AddRung("Learner-owned response", outputTag.Name);
        document.AddContact(0, 0, inputTag.Name, false);
        var program = document.BuildProgram();
        True(LadderCompiler.Compile(program).IsValid);
        Equal(0, SceneIoBindingValidator.Validate(program, points).Count);
        var sampled = SceneIoImageMapper.SampleBoolInputs(program, points.Where(p => p.Owner == "PC" && p.Type == "BOOL")
            .ToDictionary(p => p.Name, _ => false));
        var session = new VirtualControllerSession(Runtime(program));
        session.Run();
        void Scan(bool expected) => session.Advance(.020, () => sampled,
            outputs => Equal(expected, outputs[outputTag.Name]), _ => { });
        Scan(false);
        // Resolve the same explicit binding and BOOL Input contract as the UI action seam.
        var resolved = program.Variables.Single(v => v.Binding == action.GetProperty("controllerStartBinding").GetString()
            && v.Role == PlcVariableRole.Input && v.Type == PlcVariableType.Bool);
        session.PulseInput(resolved.Name);
        False(session.Snapshot.Outputs[outputTag.Name]); // No actuator mutation before an accepted ladder scan.
        Scan(true); Scan(false);
        foreach (var other in OperatorCommands.Where(c => c.Scene == sceneId && c.Request != request))
        {
            session.PulseInput(program.Variables.Single(v => v.Binding == other.Request).Name);
            Scan(false); Scan(false); // Directional commands remain independent.
        }
        session.PulseInput(resolved.Name); session.Stop(); session.Run(); Scan(false);
    }

    private static void CheckOperatorCommandMigration()
    {
        foreach (var group in OperatorCommands.GroupBy(c => c.Scene))
        {
            using var json = OpenOperatorScene(group.Key);
            var (points, initial) = ReadOperatorScene(json.RootElement);
            var requests = group.Select(c => c.Request).ToHashSet(StringComparer.Ordinal);
            var old = SceneLadderProject.Create(group.Key, "Existing learner draft", points.Where(p => !requests.Contains(p.Name)).ToArray(), initial, out _);
            var previous = old.Tags.Select(t => (t.Binding, t.Id)).ToArray();
            SceneLadderProject.EnsureSceneIoTable(old, points, initial, out _);
            foreach (var (binding, id) in previous) Equal(id, old.Tags.Single(t => t.Binding == binding).Id);
            foreach (var request in requests) Equal(PlcVariableRole.Input, old.Tags.Single(t => t.Binding == request).Role);
            var count = old.Tags.Count;
            SceneLadderProject.EnsureSceneIoTable(old, points, initial, out _);
            Equal(count, old.Tags.Count);
            True(old.Blocks.All(b => b.Rungs.Count == 0));
        }
    }

    private static void CheckTankOperatorRequests()
    {
        foreach (var sceneId in new[] { "tank-high-low", "tank-level" })
        {
            using var json = OpenOperatorScene(sceneId);
            var scene = json.RootElement;
            var (points, initial) = ReadOperatorScene(scene);
            foreach (var (actionId, request) in new[] { ("toggle-pump", "pump_request"), ("toggle-drain", "drain_request") })
            {
                var action = scene.GetProperty("simulation").GetProperty("actions").EnumerateArray()
                    .Single(a => a.GetProperty("id").GetString() == actionId);
                Equal("toggle", action.GetProperty("type").GetString()!);
                Equal(request, action.GetProperty("point").GetString()!);
                Equal("PC", points.Single(p => p.Name == request).Owner);
                True(initial[request] is false);
                var station = scene.GetProperty("equipment").EnumerateArray().Single(e => e.TryGetProperty("config", out var c)
                    && c.TryGetProperty("action", out var a) && a.GetString() == actionId);
                True(scene.GetProperty("simulation").GetProperty("pointBindings").EnumerateArray().Any(b =>
                    b.GetProperty("point").GetString() == request && b.GetProperty("equipmentId").GetString() == station.GetProperty("id").GetString()
                    && b.GetProperty("mode").GetString() == "switch"));
            }
            var document = SceneLadderProject.Create(sceneId, "Tank request learner regression", points, initial, out _);
            document.AddRung("Fill below high; drain priority", "inlet_pump_run");
            document.AddContact(0, 0, "pump_request", false);
            document.AddContact(0, 0, "drain_request", true);
            document.AddContact(0, 0, "high_level_switch", true);
            document.AddRung("Drain above low", "drain_valve_open");
            document.AddContact(1, 0, "drain_request", false);
            document.AddContact(1, 0, "low_level_switch", true);
            var program = document.BuildProgram();
            True(LadderCompiler.Compile(program).IsValid);
            Equal(0, SceneIoBindingValidator.Validate(program, points).Count);
            foreach (var output in new[] { "inlet_pump_run", "drain_valve_open" })
                Equal(PlcVariableRole.Output, program.Variables.Single(v => v.Binding == output).Role);
            var session = new VirtualControllerSession(Runtime(program)); session.Run();
            void Scan(bool pump, bool drain, bool low, bool high, bool expectedPump, bool expectedDrain)
            {
                var image = SceneIoImageMapper.SampleBoolInputs(program, Inputs(("pump_request", pump), ("drain_request", drain),
                    ("low_level_switch", low), ("high_level_switch", high)));
                session.Advance(.020, () => image, outputs =>
                {
                    Equal(expectedPump, outputs["inlet_pump_run"]);
                    Equal(expectedDrain, outputs["drain_valve_open"]);
                }, _ => { });
            }
            Scan(false, false, false, false, false, false);
            Scan(true, false, false, false, true, false);
            Scan(true, false, false, false, true, false); // Maintained, not a one-scan pulse.
            Scan(true, true, false, false, false, true); // Drain priority.
            Scan(true, true, true, false, false, false); // Actual low indication prevents drain.
            Scan(true, false, false, true, false, false); // Actual high indication prevents fill.
            Scan(false, false, false, false, false, false);
        }
    }
}
