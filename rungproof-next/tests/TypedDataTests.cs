using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void TestSceneTextBoundary()
    {
        System.Text.Json.JsonElement Definition(string type, object? initial, object? members, string owner = "PC") =>
            System.Text.Json.JsonSerializer.SerializeToElement(new { points = new[] {
                new Dictionary<string, object?> { ["name"] = "text", ["type"] = type, ["owner"] = owner, ["initial"] = initial, ["enumMembers"] = members }
            }});
        foreach (var bad in new (string Type, object? Initial, object? Members)[] {
            ("STRING", false, null), ("STRING", new string('x',256), null), ("STRING", null, null),
            ("ENUM", "Stopped", null), ("ENUM", "Other", new[] { "Stopped", "Running" }),
            ("ENUM", "Stopped", new[] { "Stopped", "Stopped" }), ("ENUM", "Stopped", new object[] { "Stopped", 123 })
        })
        {
            try { SceneAggregateContract.Validate(Definition(bad.Type, bad.Initial, bad.Members)); throw new InvalidOperationException("Malformed scene text accepted"); }
            catch (ArgumentException) { }
        }
        SceneAggregateContract.Validate(Definition("STRING", new string('x',255), null));
        SceneAggregateContract.Validate(Definition("STRING", "  preserved  ", null));
        SceneAggregateContract.Validate(Definition("STRING", "amber", null, "SIM"));
        SceneAggregateContract.Validate(Definition("ENUM", "Stopped", new[] { "Stopped", "Running" }));
    }

    private static void TestEnumMalformedJson()
    {
        var domain = new[] { "Stopped", "Running", "Fault" };
        var p = TextProgram(new("state_in", PlcVariableType.Enum, PlcVariableRole.Input, "Stopped", EnumMembers: domain),
            new("state", PlcVariableType.Enum, PlcVariableRole.Output, "Stopped", EnumMembers: domain),
            new("move", LadderNumericOperationKind.Move, "state_in", "0", "state"));
        foreach (var malformed in new[] { "[\"Stopped\",123]", "[\"Stopped\",null]", "[\"Stopped\",true]", "[\"Stopped\",{}]", "\"Stopped\"" })
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(LadderProgramJson.Save(p))!;
            root["variables"]![0]!["enumMembers"] = System.Text.Json.Nodes.JsonNode.Parse(malformed);
            var result = LadderProgramJson.Load(root.ToJsonString());
            False(result.IsValid);
            True(result.Issues.Any(issue => issue.Code == "VC100" && issue.Path.EndsWith("enumMembers", StringComparison.Ordinal)));
        }
    }

    private static void TestTextTypeSupport()
    {
        if (!Enum.TryParse<PlcVariableType>("String", out _)) throw new InvalidOperationException("STRING must be an actual declared controller type");
        if (!Enum.TryParse<PlcVariableType>("Enum", out _)) throw new InvalidOperationException("ENUM must be an actual declared controller type");
    }

    private static LadderProgram TextProgram(PlcVariable input, PlcVariable output, LadderNumericOperation operation) =>
        new(1, "typed-text", "Typed text", "LD", TimeSpan.FromMilliseconds(20),
            [input, output, new("enable", PlcVariableType.Bool, PlcVariableRole.Memory, true)],
            [new("network", "Text operation", Contact("enable-contact", "enable"), NumericOperation: operation)]);

    private static void TestTextMoveAndLifecycle()
    {
        var p = TextProgram(new("message", PlcVariableType.String, PlcVariableRole.Input, "  initial  ", "rx"),
            new("copy", PlcVariableType.String, PlcVariableRole.Output, "", "tx"),
            new("move", LadderNumericOperationKind.Move, "message", "0", "copy"));
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(p));
        True(loaded.IsValid);
        var r = Runtime(loaded.Program!); r.Run();
        r.Scan(Inputs(), null, new Dictionary<string,string> { ["message"] = "  F003  " });
        Equal("  F003  ", r.Snapshot.TextOutputs["copy"]);
        Equal("  F003  ", SceneIoImageMapper.CommitTextOutputs(p, r.Snapshot.TextOutputs)["tx"]);
        Equal("raw", SceneIoImageMapper.SampleTextInputs(p, new Dictionary<string,string> { ["rx"] = "raw" })["message"]);
        r.Stop(); Equal("", r.Snapshot.TextOutputs["copy"]);
        r.Reset(); Equal("  initial  ", r.Snapshot.TextVariables["message"]);
        True(LadderEditorDocument.TryParseInitialValue(PlcVariableType.String, "  preserve  ", out var value, out _));
        Equal("  preserve  ", (string)value);
    }

    private static void TestTextInputValidation()
    {
        var p = TextProgram(new("message", PlcVariableType.String, PlcVariableRole.Input, "old"),
            new("copy", PlcVariableType.String, PlcVariableRole.Output, ""),
            new("move", LadderNumericOperationKind.Move, "message", "0", "copy"));
        var r = Runtime(p); r.Run();
        try { r.Scan(Inputs(), null, new Dictionary<string,string> { ["message"] = "changed", ["unknown"] = "bad" }); throw new InvalidOperationException("Invalid input accepted"); }
        catch (ArgumentException) { Equal("old", r.Snapshot.TextVariables["message"]); }
        try { r.Scan(Inputs(), null, new Dictionary<string,string> { ["message"] = new string('x',256) }); throw new InvalidOperationException("Oversized input accepted"); }
        catch (ArgumentException) { Equal("old", r.Snapshot.TextVariables["message"]); }
        r.Scan(Inputs(), null, new Dictionary<string,string> { ["message"] = new string('x',255) });
        Equal(255, r.Snapshot.TextOutputs["copy"].Length);
        HasIssue(p with { Variables = p.Variables.Select(v => v.Name == "message" ? v with { InitialValue = new string('x',256) } : v).ToArray() }, "VC002");
    }

    private static void TestTextFormattingFailures()
    {
        var p = TextProgram(new("template", PlcVariableType.String, PlcVariableRole.Input, "CHICKEN {0:F3} kg"),
            new("label", PlcVariableType.String, PlcVariableRole.Output, ""),
            new("format", LadderNumericOperationKind.FormatText, "1.237", "template", "label"));
        var r = Runtime(p); r.Run(); r.Scan(Inputs()); Equal("CHICKEN 1.237 kg", r.Snapshot.TextOutputs["label"]);
        r.Scan(Inputs(), null, new Dictionary<string,string> { ["template"] = "CHICKEN {1:F3} kg" });
        Equal("", r.Snapshot.TextOutputs["label"]); True(r.Snapshot.Diagnostics.Any(d => d.StartsWith("VC_RUNTIME_TEXT")));
        foreach(var template in new[] { "{0:F3}{0:F2}", "{0:F7}", "{0,99:F3}", "{{0:F3}}" }) False(PlcTextValues.TryFormat(1.237,template,out _));
        False(PlcTextValues.TryFormat(1e30, new string('x',249) + "{0:F3}", out _));
        False(PlcTextValues.TryFormat(double.NaN,"{0:F3}",out _));
    }

    private static void TestEnumDomains()
    {
        var domain = new[] { "Stopped", "Running", "Fault" };
        var p = TextProgram(new("state_in", PlcVariableType.Enum, PlcVariableRole.Input, "Stopped", EnumMembers: domain),
            new("state", PlcVariableType.Enum, PlcVariableRole.Output, "Stopped", EnumMembers: domain),
            new("move", LadderNumericOperationKind.Move, "state_in", "0", "state"));
        var r = Runtime(p); r.Run(); r.Scan(Inputs(), null, new Dictionary<string,string> { ["state_in"] = "Running" });
        Equal("Running", r.Snapshot.TextOutputs["state"]); r.Stop(); Equal("Stopped", r.Snapshot.TextOutputs["state"]);
        try { r.Scan(Inputs(), null, new Dictionary<string,string> { ["state_in"] = "Bogus" }); throw new InvalidOperationException("Invalid enum accepted"); }
        catch (ArgumentException) { Equal("Running",r.Snapshot.TextVariables["state_in"]); }
        HasIssue(p with { Variables = p.Variables.Select(v=>v.Name=="state_in" ? v with { EnumMembers = new[] { "Stopped", "Other" } } : v).ToArray() },"VC002");
        HasIssue(p with { Variables = p.Variables.Select(v=>v.Name=="state_in" ? v with { EnumMembers = new[] { "Stopped", "Stopped" } } : v).ToArray() },"VC002");
        HasIssue(p with { Networks = [p.Networks[0] with { NumericOperation = p.Networks[0].NumericOperation! with { SourceA = "\"Bogus\"" } }] },"VC002");
        True(LadderProgramJson.Load(LadderProgramJson.Save(p)).IsValid);
        var points = new[] { new SceneIoPoint("state_binding","ENUM","PLC","output","state",EnumMembers: new[]{"Stopped","Other"}) };
        True(SceneIoBindingValidator.Validate(p with { Variables = [p.Variables[1] with { Binding = "state_binding" }] },points).Any(i=>i.Code=="IO002"));
    }
}
