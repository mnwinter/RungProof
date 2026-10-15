using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static LadderEditorDocument AggregateFixture()
    {
        var doc = new LadderEditorDocument();
        doc.ResetProject("motor-aggregate-qa", "Motor Aggregate QA", TimeSpan.FromMilliseconds(20));
        doc.AddAggregateTag("feedback", PlcVariableRole.Input, PlcVariableType.Struct,
            new([new("valid", PlcVariableType.Bool), new("temperature", PlcVariableType.Real)]),
            new Dictionary<string, object> { ["valid"] = false, ["temperature"] = 0.0 }, "motor_feedback");
        doc.AddAggregateTag("motors", PlcVariableRole.Output, PlcVariableType.Array,
            new(ElementType: PlcVariableType.Bool, LowerBound: 0, Length: 10), new bool[10], "motor_commands");
        doc.AddRung("First motor follows record validity", "motors[0]");
        doc.AddContact(0, 0, "feedback.valid", false);
        doc.AddRung("Last motor follows record validity", "motors[9]");
        doc.AddContact(1, 0, "feedback.valid", false);
        return doc;
    }

    private static void RequireAggregate(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void TestAggregateExecution()
    {
        var doc = AggregateFixture();
        var saved = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(doc));
        RequireAggregate(saved.Document is not null && saved.Issues.Count == 0, "Editor aggregate round-trip failed.");
        RequireAggregate(LadderCompiler.Compile(saved.Document!.BuildProgram()).IsValid, "Saved editor aggregate schema lost.");
        var program = doc.BuildProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(program));
        RequireAggregate(loaded.IsValid, "Program aggregate round-trip failed.");
        var compiled = LadderCompiler.Compile(loaded.Program!);
        RequireAggregate(compiled.IsValid && compiled.Program!.Source.Variables.Count == 2 && compiled.Program.Variables.Count == 12, "Aggregate identity or expansion lost.");
        var runtime = new VirtualControllerRuntime(compiled.Program!);
        runtime.Run();
        var before = runtime.Snapshot;
        var snapshot = runtime.ScanAggregates(new Dictionary<string, object> { ["feedback"] = new Dictionary<string, object> { ["valid"] = true, ["temperature"] = 42.25 } });
        var commands = (IReadOnlyList<object>)snapshot.Aggregates["motors"];
        RequireAggregate((bool)commands[0] && (bool)commands[9] && commands.Skip(1).Take(8).All(v => !(bool)v), "Element writes changed unrelated motors.");
        RequireAggregate(!(bool)((IReadOnlyList<object>)before.Aggregates["motors"])[0], "Old snapshot mutated.");
        RequireAggregate(snapshot.NumericVariables["feedback.temperature"] == 42.25, "REAL member input lost.");
        var io = new[]
        {
            new SceneIoPoint("motor_feedback", "STRUCT", "PC", "input", "Fixture", program.Variables[0].Aggregate),
            new SceneIoPoint("motor_commands", "ARRAY", "PLC", "output", "Fixture", program.Variables[1].Aggregate),
        };
        RequireAggregate(SceneIoBindingValidator.Validate(program, io).Count == 0, "Typed root bindings rejected.");
        RequireAggregate(SceneIoBindingValidator.Validate(program, [io[0], io[1] with { Aggregate = io[1].Aggregate! with { Length = 11 } }]).Count > 0, "Array root shape mismatch accepted.");
        foreach (var malformed in new PlcAggregateSchema?[] { null, new([new("bad.name", PlcVariableType.Bool)]), new([new("x", PlcVariableType.Timer)]), new([new("x", PlcVariableType.Bool), new("x", PlcVariableType.Bool)]) })
            RequireAggregate(SceneIoBindingValidator.Validate(program, [io[0] with { Aggregate = malformed }, io[1]]).Count > 0, "Malformed scene STRUCT schema accepted.");
        var mapped = SceneIoImageMapper.CommitBoolOutputs(program, snapshot.Outputs);
        RequireAggregate(mapped["motor_commands[0]"] && mapped["motor_commands[9]"], "Aggregate leaf bindings lost.");
        RequireAggregate(runtime.Stop().Outputs.Values.All(v => !v), "Stop retained an aggregate output.");
        RequireAggregate(!runtime.Reset().Variables["feedback.valid"], "Reset retained aggregate input.");
        runtime.Run();
        var normal = runtime.Scan(new Dictionary<string, bool> { ["feedback.valid"] = true });
        RequireAggregate((bool)((IReadOnlyList<object>)normal.Aggregates["motors"])[0], "Normal scalar scan failed to reconstruct roots.");
        RequireAggregate(doc.CountTagReferences("motors") == 2, "Array reference lifecycle count lost.");
        doc.UpdateTag("motors", "renamedMotors", PlcVariableType.Array, PlcVariableRole.Output, "motor_commands");
        RequireAggregate(doc.Rungs[0].CoilVariable == "renamedMotors[0]" && LadderCompiler.Compile(doc.BuildProgram()).IsValid, "Array rename failed.");
    }

    private static void TestAggregateRejection()
    {
        var doc = AggregateFixture();
        var program = doc.BuildProgram();
        var wrong = program with { Variables = program.Variables.Select(v => v.Name == "motors" ? v with { InitialValue = new bool[9] } : v).ToArray() };
        RequireAggregate(!LadderCompiler.Compile(wrong).IsValid, "Wrong fixed length accepted.");
        doc.Rungs[0].CoilVariable = "motors[10]";
        RequireAggregate(!LadderCompiler.Compile(doc.BuildProgram()).IsValid, "Out-of-range operand accepted.");
        doc.Rungs[0].CoilVariable = "motors[0]";
        doc.Rungs[0].Branches[0].Contacts[0] = doc.Rungs[0].Branches[0].Contacts[0] with { Variable = "feedback.temperature" };
        RequireAggregate(!LadderCompiler.Compile(doc.BuildProgram()).IsValid, "Numeric member accepted as contact.");
        var overlap = program with { Variables = program.Variables.Append(new PlcVariable("motors[0]", PlcVariableType.Bool, PlcVariableRole.Output, false)).ToArray() };
        RequireAggregate(!LadderCompiler.Compile(overlap).IsValid, "Overlapping leaf declaration accepted.");
        var namespaceOverlap = new PlcVariable[]
        {
            new("a", PlcVariableType.Struct, PlcVariableRole.Memory, new Dictionary<string, object> { ["b"] = false }, Aggregate: new([new("b", PlcVariableType.Bool)])),
            new("a.b", PlcVariableType.Struct, PlcVariableRole.Memory, new Dictionary<string, object> { ["c"] = false }, Aggregate: new([new("c", PlcVariableType.Bool)])),
        };
        RequireAggregate(!LadderCompiler.Compile(program with { Variables = namespaceOverlap }).IsValid, "Aggregate root overlapped another root's member namespace.");
        var renameDoc = AggregateFixture(); var renameSaved = LadderEditorProjectJson.Save(renameDoc); var namespaceRejected = false;
        try { renameDoc.UpdateTag("motors", "feedback.valid", PlcVariableType.Array, PlcVariableRole.Output); }
        catch (ArgumentException) { namespaceRejected = true; }
        RequireAggregate(namespaceRejected && LadderEditorProjectJson.Save(renameDoc) == renameSaved, "Namespace overlap rename modified unrelated references.");
        var editor = AggregateFixture(); var saved = LadderEditorProjectJson.Save(editor); var editRejected = false;
        try { editor.UpdateTag("motors", "renamedInvalid", PlcVariableType.Array, PlcVariableRole.Output, initialValue: new bool[9]); }
        catch (ArgumentException) { editRejected = true; }
        RequireAggregate(editRejected && LadderEditorProjectJson.Save(editor) == saved, "Invalid aggregate edit renamed references or changed the document.");
        var duplicate = program with { Variables = program.Variables.Select(v => v.Name == "feedback" ? v with { Aggregate = new([new("valid", PlcVariableType.Bool), new("valid", PlcVariableType.Bool)]) } : v).ToArray() };
        RequireAggregate(!LadderCompiler.Compile(duplicate).IsValid, "Duplicate STRUCT field accepted.");
        var runtime = new VirtualControllerRuntime(LadderCompiler.Compile(program).Program!);
        runtime.Run();
        var before = runtime.Snapshot;
        var rejected = false;
        try { runtime.ScanAggregates(new Dictionary<string, object> { ["feedback"] = new Dictionary<string, object> { ["valid"] = true, ["temperature"] = "bad" } }); }
        catch (ArgumentException) { rejected = true; }
        RequireAggregate(rejected && runtime.Snapshot.ScanNumber == before.ScanNumber && !runtime.Snapshot.Variables["feedback.valid"], "Malformed aggregate partially changed input image.");
    }
}
