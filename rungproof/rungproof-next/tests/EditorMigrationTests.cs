using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterEditorMigrationTests()
    {
        Test("EditorSchema2WritesAndReadsLegacyDemos", EditorSchema2WritesAndReadsLegacyDemos);
        Test("RecursiveProjectRoundTripPreservesTreeAndActions", RecursiveProjectRoundTripPreservesTreeAndActions);
        Test("FutureEditorVersionRejectedWithoutDocument", FutureEditorVersionRejectedWithoutDocument);
        Test("DuplicateProjectIdsRejected", DuplicateProjectIdsRejected);
        Test("AllocatorRepairDoesNotMakeSavedDocumentDirty", AllocatorRepairDoesNotMakeSavedDocumentDirty);
        Test("Schema2EmptyCompositeSemanticsAndDepth", Schema2EmptyCompositeSemanticsAndDepth);
    }

    private static void Schema2EmptyCompositeSemanticsAndDepth()
    {
        foreach (var kind in new[] { LadderNodeKind.Series, LadderNodeKind.Parallel })
        {
            var program = new LadderProgram(2, "empty", "Empty", "LD", TimeSpan.FromMilliseconds(20),
                [new("Q", PlcVariableType.Bool, PlcVariableRole.Output, false)],
                [new("network", "Empty", new("root", kind, Children: []), Coil: new("coil", "Q"))]);
            True(!LadderCompiler.Compile(program with { SchemaVersion = 1 }).IsValid);
            var runtime = Runtime(program); runtime.Run();
            Equal(kind == LadderNodeKind.Series, runtime.Scan(Inputs()).Outputs["Q"]);
            True(LadderProgramJson.Load(LadderProgramJson.Save(program)).IsValid);
            var root = program.Networks[0].Logic;
            for (var depth = 2; depth <= 32; depth++) root = new("depth-"+depth, LadderNodeKind.Series, Children: [root]);
            var deep = program with { Networks = [program.Networks[0] with { Logic = root }] };
            True(LadderProgramJson.Load(LadderProgramJson.Save(deep)).IsValid);
            root = new("depth-33", LadderNodeKind.Series, Children: [root]);
            True(!LadderCompiler.Compile(deep with { Networks = [deep.Networks[0] with { Logic = root }] }).IsValid);
        }
    }

    private static void AllocatorRepairDoesNotMakeSavedDocumentDirty()
    {
        var document = new LadderEditorDocument();
        document.ReplaceFromProgram(LadderEditorDocument.CreateConveyorExample().BuildProgram());
        var saved = LadderEditorProjectJson.Save(document);
        True(!LadderEditorProjectJson.HasUnsavedChanges(document, saved));
        var loaded = LadderEditorProjectJson.Load(saved);
        True(loaded.IsReadable);
        True(!LadderEditorProjectJson.HasUnsavedChanges(loaded.Document!, saved));
        document.Rungs[0].Label += " edited";
        True(LadderEditorProjectJson.HasUnsavedChanges(document, saved));
    }

    private static void EditorSchema2WritesAndReadsLegacyDemos()
    {
        Equal(2, LadderEditorProjectJson.SchemaVersion);
        var files = Directory.GetFiles(Path.Combine(FindProjectRoot(), "tests", "fixtures", "editor-migration"), "*.rpproj.json");
        Equal(5, files.Length);
        foreach (var file in files)
        {
            var original = File.ReadAllText(file);
            var first = LadderEditorProjectJson.Load(original);
            True(first.IsReadable);
            Equal(File.ReadAllText(file + ".v1-ir.json"), LadderProgramJson.Save(first.Document!.BuildProgram() with { SchemaVersion = 1 }));
            var written = LadderEditorProjectJson.Save(first.Document!);
            Equal(2, JsonNode.Parse(written)!["schemaVersion"]!.GetValue<int>());
            var reopened = LadderEditorProjectJson.Load(written);
            True(reopened.IsReadable);
            Equal(LadderProgramJson.Save(first.Document!.BuildProgram()), LadderProgramJson.Save(reopened.Document!.BuildProgram()));
            Equal(first.Document.SourceSceneId, reopened.Document.SourceSceneId);
            Equal(string.Join(",", first.Document.WatchVariables), string.Join(",", reopened.Document.WatchVariables));
            Equal(original, File.ReadAllText(file));
        }
    }

    private static void RecursiveProjectRoundTripPreservesTreeAndActions()
    {
        var tree = new LadderNode("root", LadderNodeKind.Series, Children:
        [new("fork", LadderNodeKind.Parallel, Children: [new("a", LadderNodeKind.Contact, "A"), new("b", LadderNodeKind.Contact, "B")]),
         new("edge", LadderNodeKind.Contact, "A", EdgeMode: LadderEdgeMode.Rising)]);
        var source = new LadderProgram(1, "migration", "Migration", "LD", TimeSpan.FromMilliseconds(20),
            [new("A", PlcVariableType.Bool, PlcVariableRole.Input, false), new("B", PlcVariableType.Bool, PlcVariableRole.Input, false),
             new("Q", PlcVariableType.Bool, PlcVariableRole.Output, false)],
            [new("network", "Nested", tree, Coil: new("actual-output", "Q"))]);
        var document = new LadderEditorDocument();
        document.ReplaceFromProgram(source);
        var before = LadderProgramJson.Save(document.BuildProgram());
        var loaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(document));
        True(loaded.IsReadable);
        Equal(before, LadderProgramJson.Save(loaded.Document!.BuildProgram()));
        Equal("actual-output", loaded.Document.BuildProgram().Networks[0].Coil!.Id);
    }

    private static void FutureEditorVersionRejectedWithoutDocument()
    {
        var doc = new LadderEditorDocument(); doc.AddRung("Draft", "Q");
        var node = JsonNode.Parse(LadderEditorProjectJson.Save(doc))!;
        node["schemaVersion"] = 999;
        var result = LadderEditorProjectJson.Load(node.ToJsonString());
        True(!result.IsReadable && result.Document is null);
        True(result.Issues.Any(i => i.Message.Contains("999", StringComparison.Ordinal)));
    }

    private static void DuplicateProjectIdsRejected()
    {
        var doc = new LadderEditorDocument(); doc.AddRung("Draft", "Q"); doc.AddRung("Draft", "Q");
        var node = JsonNode.Parse(LadderEditorProjectJson.Save(doc))!;
        var rungs = node["blocks"]![0]!["rungs"]!;
        rungs[1]!["id"] = rungs[0]!["id"]!.GetValue<string>();
        var result = LadderEditorProjectJson.Load(node.ToJsonString());
        True(!result.IsReadable && result.Document is null);
        True(result.Issues.Any(i => i.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)));
    }
}
