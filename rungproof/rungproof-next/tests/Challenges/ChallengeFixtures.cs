using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;
using System.Text.Json;
namespace RungProof.Next.ChallengeTests;

public static class ChallengeFixtures
{
    public static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    public static LadderProgram OutputSealIn(bool rename = false)
    {
        var s = rename ? "Begin" : "Start";
        var t = rename ? "Halt" : "Stop";
        var e = rename ? "Occupied" : "Eye";
        var o = rename ? "Motor" : "Run";
        var variables = new PlcVariable[] { new(s, PlcVariableType.Bool, PlcVariableRole.Input, false, "operator.start"), new(t, PlcVariableType.Bool, PlcVariableRole.Input, false, "operator.stop"), new(e, PlcVariableType.Bool, PlcVariableRole.Input, false, "simulated_photoeye"), new(o, PlcVariableType.Bool, PlcVariableRole.Output, false, "conveyor_running") };
        var n = new LadderNetwork("r1", "Request", new("series", LadderNodeKind.Series, Children: [new("stop", LadderNodeKind.Contact, t, true), new("eye", LadderNodeKind.Contact, e, true), new("parallel", LadderNodeKind.Parallel, Children: [new("start", LadderNodeKind.Contact, s), new("hold", LadderNodeKind.Contact, o)])]), new("coil", o));
        return new(1, "fixture", "Fixture", "LD", TimeSpan.FromMilliseconds(20), variables, [n]);
    }
    public static LadderProgram InternalSealIn(bool rename = false)
    {
        var p = OutputSealIn(rename);
        var output = p.Variables.Single(v => v.Role == PlcVariableRole.Output).Name;
        var memory = rename ? "RememberedRequest" : "Request";
        var n = p.Networks[0];
        LadderNode Replace(LadderNode node) => node with
        {
            Variable = node.Variable == output ? memory : node.Variable,
            Children = node.Children?.Select(Replace).ToArray()
        };
        var first = n with
        {
            Logic = Replace(n.Logic),
            Coil = n.Coil! with
            {
                Variable = memory
            }
        };
        var second = new LadderNetwork("r2", "Apply remembered request", new("memory-contact", LadderNodeKind.Contact, memory), new("output-coil", output));
        return p with
        {
            Variables = p.Variables.Append(new(memory, PlcVariableType.Bool, PlcVariableRole.Memory, false)).ToArray(),
            Networks = [first, second]
        };
    }
    public static string DefinitionJson => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        id = "scene-1-conveyor-stop",
        revision = 1,
        sceneId = "scene-1-conveyor-stop",
        protocolRevision = "conveyor-v1",
        scanPeriodMs = 20,
        title = "Stop the carton",
        objective = "Start, hold after release, stop on feedback or Process Stop.",
        hints = new[] { "Observe release." },
        plant = new
        {
            length = 1.0,
            speed = 0.5,
            objectLength = 0.2,
            photoeyePosition = 0.5,
            minimumPhotoeyeOn = 0.1
        },
        cases = new[] { "ColdStart", "Holding", "BeamStop", "ProcessStop", "SimultaneousStartStop", "BlockedStart", "ResetRepeat" }.Select((kind, i) => new { id = $"CV{i + 1:00}", kind, maximumScans = 250 }),
        glossary = new[] { "operator.start", "operator.stop", "simulated_photoeye", "conveyor_running" }.Select(b => new { binding = b, displayName = b, ownership = b == "conveyor_running" ? "Output" : "Input", meaning = "Modeled symbolic signal." }),
        eligibilityPolicy = "conveyor-bool-v1",
        starter = new
        {
            resourceId = "res://challenges/starters/conveyor-stop.rpproj.json",
            sha256 = new string('0', 64),
            byteLength = 100
        }
    });
}


