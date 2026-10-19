using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterEditorMigrationTests()
    {
        Test("EditorSchema6WritesAndReadsLegacyDemos", EditorSchema6WritesAndReadsLegacyDemos);
        Test("OpenBranchStatePersistsButCannotExportExecutableProgram", OpenBranchStatePersistsButCannotExportExecutableProgram);
        Test("TagTablesPersistAndLegacyTagsUseDefaultTable", TagTablesPersistAndLegacyTagsUseDefaultTable);
        Test("TagTableOperationsPreserveIdentityAndHistory", TagTableOperationsPreserveIdentityAndHistory);
        Test("TagTableRejectedOperationsAndMalformedDeclarationsAreAtomic", TagTableRejectedOperationsAndMalformedDeclarationsAreAtomic);
        Test("HierarchicalAggregateCommentsPersist", HierarchicalAggregateCommentsPersist);
        Test("UnassignedTimerPresetPersistsAndFailsLoadValidation", UnassignedTimerPresetPersistsAndFailsLoadValidation);
        Test("RetentiveTimerResetOperandPersistsAndValidates", RetentiveTimerResetOperandPersistsAndValidates);
        Test("SiemensTimeLiteralParsingAndRoundTrip", SiemensTimeLiteralParsingAndRoundTrip);
        Test("ComparatorDataTypePersistsAndDefaultsForLegacyNodes", ComparatorDataTypePersistsAndDefaultsForLegacyNodes);
        Test("RecursiveProjectRoundTripPreservesTreeAndActions", RecursiveProjectRoundTripPreservesTreeAndActions);
        Test("FutureEditorVersionRejectedWithoutDocument", FutureEditorVersionRejectedWithoutDocument);
        Test("DuplicateProjectIdsRejected", DuplicateProjectIdsRejected);
        Test("AllocatorRepairDoesNotMakeSavedDocumentDirty", AllocatorRepairDoesNotMakeSavedDocumentDirty);
        Test("Schema2EmptyCompositeSemanticsAndDepth", Schema2EmptyCompositeSemanticsAndDepth);
    }

    private static void UnassignedTimerPresetPersistsAndFailsLoadValidation()
    {
        var document = new LadderEditorDocument();
        document.AddTag("IEC_Timer_0_DB", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        var timer = document.AddTimerRung("Unassigned PT", "IEC_Timer_0_DB", TimeSpan.FromMilliseconds(100));
        timer.TimerPresetAssigned = false;
        timer.TimerPresetOperand = string.Empty;

        var saved = LadderEditorProjectJson.Save(document);
        var loaded = LadderEditorProjectJson.Load(saved);
        True(loaded.IsReadable);
        True(!loaded.Document!.Rungs[0].TimerPresetAssigned);
        True(LadderEditorProjectJson.Save(loaded.Document) == saved);
        var issues = LadderCompiler.Validate(loaded.Document.BuildProgram());
        True(issues.Any(issue => issue.Code == "VC007" && issue.Path.EndsWith("timer.presetMs", StringComparison.Ordinal)));
        var executableLoad = LadderProgramJson.Load(LadderProgramJson.Save(loaded.Document.BuildProgram()));
        True(!executableLoad.IsValid && executableLoad.Issues.Any(issue => issue.Code == "VC007"));

        // Files saved before this field existed retain the historical assigned-preset behavior.
        var legacyJson = JsonNode.Parse(saved)!;
        legacyJson["blocks"]![0]!["rungs"]![0]!.AsObject().Remove("timerPresetAssigned");
        var legacy = LadderEditorProjectJson.Load(legacyJson.ToJsonString());
        True(legacy.IsReadable);
        True(legacy.Document!.Rungs[0].TimerPresetAssigned);
    }

    private static void RetentiveTimerResetOperandPersistsAndValidates()
    {
        var document = new LadderEditorDocument();
        document.AddTag("IEC_Timer_0_DB", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.AddTag("timer_reset", PlcVariableRole.Input, type: PlcVariableType.Bool);
        var timer = document.AddTimerRung("Retentive timer", "IEC_Timer_0_DB", TimeSpan.FromSeconds(1),
            LadderTimerKind.RetentiveOnDelay);
        timer.TimerResetOperand = "timer_reset";

        var saved = LadderEditorProjectJson.Save(document);
        var loaded = LadderEditorProjectJson.Load(saved);
        True(loaded.IsReadable);
        Equal("timer_reset", loaded.Document!.Rungs[0].TimerResetOperand);
        var program = loaded.Document.BuildProgram();
        Equal("timer_reset", program.Networks[0].Timer!.ResetOperand!);
        True(LadderCompiler.Validate(program).All(issue => !issue.Path.EndsWith("timer.resetOperand", StringComparison.Ordinal)));

        var programRoundTrip = LadderProgramJson.Load(LadderProgramJson.Save(program));
        True(programRoundTrip.IsValid);
        Equal("timer_reset", programRoundTrip.Program!.Networks[0].Timer!.ResetOperand!);

        timer.TimerResetOperand = string.Empty;
        var missingReset = LadderCompiler.Validate(document.BuildProgram());
        True(missingReset.Any(issue => issue.Code == "VC007" && issue.Path.EndsWith("timer.resetOperand", StringComparison.Ordinal)));

        // Older TIA timer projects had no explicit reset-operand field and map to FALSE.
        var legacyJson = JsonNode.Parse(saved)!;
        legacyJson["blocks"]![0]!["rungs"]![0]!.AsObject().Remove("timerResetOperand");
        var legacy = LadderEditorProjectJson.Load(legacyJson.ToJsonString());
        True(legacy.IsReadable);
        Equal("FALSE", legacy.Document!.Rungs[0].TimerResetOperand);
    }

    private static void SiemensTimeLiteralParsingAndRoundTrip()
    {
        foreach (var (literal, expected) in new[]
        {
            ("T#750ms", 750), ("T#1s", 1_000), ("T#2s_200ms", 2_200),
            ("TIME#1d2h3m4s5ms", 93_784_005), ("T#25.8s", 25_800),
            ("T#1s500ms", 1_500), ("200", 200),
        })
        {
            True(LadderTimeLiteral.TryParse(literal, out var parsed));
            Equal(TimeSpan.FromMilliseconds(expected), parsed);
        }
        foreach (var invalid in new[] { "", "???", "TIME_TAG", "T#0ms", "T#1us", "T#1.0001ms", "T#1s_", "T#__1s", "T#2147483648ms" })
            True(!LadderTimeLiteral.TryParse(invalid, out _));

        var document = new LadderEditorDocument();
        document.AddTag("IEC_Timer_0_DB", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        var timer = document.AddTimerRung("TIA TIME literal", "IEC_Timer_0_DB", TimeSpan.FromMilliseconds(25_800));
        timer.TimerPresetOperand = "T#25.8s";
        var saved = LadderEditorProjectJson.Save(document);
        var loaded = LadderEditorProjectJson.Load(saved);
        True(loaded.IsReadable);
        Equal("T#25.8s", loaded.Document!.Rungs[0].TimerPresetOperand);
        var executable = loaded.Document.BuildProgram();
        Equal("T#25.8s", executable.Networks[0].Timer!.PresetOperand!);
        True(LadderCompiler.Validate(executable).All(issue => issue.Code != "VC007"));
        var executableRoundTrip = LadderProgramJson.Load(LadderProgramJson.Save(executable));
        True(executableRoundTrip.IsValid);
        Equal("T#25.8s", executableRoundTrip.Program!.Networks[0].Timer!.PresetOperand!);
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

    private static void EditorSchema6WritesAndReadsLegacyDemos()
    {
        Equal(6, LadderEditorProjectJson.SchemaVersion);
        var files = Directory.GetFiles(Path.Combine(FindProjectRoot(), "tests", "fixtures", "editor-migration"), "*.rpproj.json");
        Equal(5, files.Length);
        foreach (var file in files)
        {
            var original = File.ReadAllText(file);
            var first = LadderEditorProjectJson.Load(original);
            if (!first.IsReadable) throw new InvalidOperationException(string.Join("; ", first.Issues.Select(issue => $"{issue.Code} {issue.Path}: {issue.Message}")));
            Equal(File.ReadAllText(file + ".v1-ir.json"), LadderProgramJson.Save(first.Document!.BuildProgram() with { SchemaVersion = 1 }));
            var written = LadderEditorProjectJson.Save(first.Document!);
            Equal(6, JsonNode.Parse(written)!["schemaVersion"]!.GetValue<int>());
            var reopened = LadderEditorProjectJson.Load(written);
            True(reopened.IsReadable);
            Equal(LadderProgramJson.Save(first.Document!.BuildProgram()), LadderProgramJson.Save(reopened.Document!.BuildProgram()));
            Equal(first.Document.SourceSceneId, reopened.Document.SourceSceneId);
            Equal(string.Join(",", first.Document.WatchVariables), string.Join(",", reopened.Document.WatchVariables));
            Equal(original, File.ReadAllText(file));
        }
    }

    private static void OpenBranchStatePersistsButCannotExportExecutableProgram()
    {
        var document = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        var path = document.Rungs[0].Branches[0];
        var opened = LadderTreeEditorCommands.Execute(document, history,
            new(LadderTreeCommandId.OpenParallelAtSlot,
                new(document.Revision, document.Blocks[0].Id, document.Rungs[0].Id, path.Id, path.Id, 1)));
        True(opened.Applied);
        var insert = LadderTreeEditorCommands.Execute(document, history,
            new(LadderTreeCommandId.InsertAt,
                new(document.Revision, document.Blocks[0].Id, document.Rungs[0].Id,
                    opened.SelectionId, opened.SelectionParentId, 0),
                new("template", LadderNodeKind.Contact, "start_command")));
        True(insert.Applied);
        var saved = LadderEditorProjectJson.Save(document);
        Equal(6, JsonNode.Parse(saved)!["schemaVersion"]!.GetValue<int>());
        True(saved.Contains("\"openEnd\": true", StringComparison.Ordinal));
        var loaded = LadderEditorProjectJson.Load(saved);
        True(loaded.IsReadable);
        var loadedPath = EditableLadderTree.Find(loaded.Document!.Rungs[0].Root, opened.SelectionId);
        True(loadedPath?.OpenEnd == true && loadedPath.Children?.Count == 1);
        var compilation = LadderCompiler.Compile(loaded.Document.BuildProgram());
        False(compilation.IsValid);
        True(compilation.Issues.Any(issue => issue.Code == "VC003"
            && issue.Path.EndsWith(".openEnd", StringComparison.Ordinal)));
        var exportRejected = false;
        try { _ = LadderProgramJson.Save(loaded.Document.BuildProgram()); }
        catch (InvalidOperationException error)
        { exportRejected = error.Message.Contains("open parallel branch", StringComparison.OrdinalIgnoreCase); }
        True(exportRejected);
    }

    private static void TagTablesPersistAndLegacyTagsUseDefaultTable()
    {
        var document = new LadderEditorDocument();
        document.AddBlock("Main", LadderBlockType.OrganizationBlock);
        document.AddTag("start", PlcVariableRole.Input, address: "%I0.0", comment: "Machine start request");
        document.AddTagTable("Tag_table_1");
        document.AddTag("counter_enable", PlcVariableRole.Memory, tagTable: "Tag_table_1",
            address: "%M0.1", comment: "Counter permission");
        var saved = LadderEditorProjectJson.Save(document);
        var reopened = LadderEditorProjectJson.Load(saved);
        if (!reopened.IsReadable) throw new InvalidOperationException(string.Join("; ", reopened.Issues.Select(issue => $"{issue.Code} {issue.Path}: {issue.Message}")));
        Equal("Default tag table,Tag_table_1", string.Join(",", reopened.Document!.TagTables));
        Equal("Default tag table", reopened.Document.Tags.Single(tag => tag.Name == "start").TagTable);
        Equal("Tag_table_1", reopened.Document.Tags.Single(tag => tag.Name == "counter_enable").TagTable);
        Equal("%I0.0", reopened.Document.Tags.Single(tag => tag.Name == "start").Address);
        Equal("Machine start request", reopened.Document.Tags.Single(tag => tag.Name == "start").Comment);
        Equal("%M0.1", reopened.Document.Tags.Single(tag => tag.Name == "counter_enable").Address);
        Equal("Counter permission", reopened.Document.Tags.Single(tag => tag.Name == "counter_enable").Comment);
        var programJson = LadderProgramJson.Save(reopened.Document.BuildProgram());
        var programRoundTrip = LadderProgramJson.Load(programJson);
        True(programRoundTrip.IsValid);
        Equal("%I0.0", programRoundTrip.Program!.Variables.Single(tag => tag.Name == "start").Address);
        Equal("Machine start request", programRoundTrip.Program.Variables.Single(tag => tag.Name == "start").Comment);

        var legacy = JsonNode.Parse(saved)!;
        legacy["schemaVersion"] = 3;
        legacy.AsObject().Remove("tagTables");
        foreach (var tag in legacy["tags"]!.AsArray()) tag!.AsObject().Remove("tagTable");
        var migrated = LadderEditorProjectJson.Load(legacy.ToJsonString());
        if (!migrated.IsReadable) throw new InvalidOperationException(string.Join("; ", migrated.Issues.Select(issue => $"{issue.Code} {issue.Path}: {issue.Message}")));
        Equal("Default tag table", migrated.Document!.Tags.Single(tag => tag.Name == "start").TagTable);
        Equal("Default tag table", migrated.Document.Tags.Single(tag => tag.Name == "counter_enable").TagTable);
        Equal("%M0.1", migrated.Document.Tags.Single(tag => tag.Name == "counter_enable").Address);
        Equal("Counter permission", migrated.Document.Tags.Single(tag => tag.Name == "counter_enable").Comment);
    }

    private static void TagTableOperationsPreserveIdentityAndHistory()
    {
        var document = new LadderEditorDocument();
        document.AddBlock("Main", LadderBlockType.OrganizationBlock);
        document.AddTag("legacy_input", PlcVariableRole.Input);
        var stable = document.AddTag("cell_enable", PlcVariableRole.Memory);
        document.Tags[1] = stable with { Id = "tag-stable-cell-enable" };
        document.AddTagTable("Cell");
        var history = new LadderEditorHistory();
        var original = LadderEditorProjectJson.Save(document);

        history.Execute(document, "Move stable tag", () => document.MoveTag("tag-stable-cell-enable", "cell"));
        history.Execute(document, "Move legacy tag", () => document.MoveTag("legacy_input", "Cell"));
        history.Execute(document, "Rename table", () => Equal("CellInputs", document.RenameTagTable("cell", " CellInputs ")));
        Equal("CellInputs", document.Tags.Single(tag => tag.Name == "legacy_input").TagTable);
        Equal("CellInputs", document.Tags.Single(tag => tag.Name == "cell_enable").TagTable);
        Equal("tag-stable-cell-enable", LadderCallBindings.GlobalId(document.Tags.Single(tag => tag.Name == "cell_enable")));
        Equal("legacy_input", LadderCallBindings.GlobalId(document.Tags.Single(tag => tag.Name == "legacy_input")));

        var savedAfterEdits = LadderEditorProjectJson.Save(document);
        var loaded = LadderEditorProjectJson.Load(savedAfterEdits);
        True(loaded.IsReadable);
        Equal(savedAfterEdits, LadderEditorProjectJson.Save(loaded.Document!));
        Equal("CellInputs", loaded.Document!.Tags.Single(tag => tag.Name == "cell_enable").TagTable);

        True(history.Undo(document, out _));
        True(history.Undo(document, out _));
        True(history.Undo(document, out _));
        Equal(original, LadderEditorProjectJson.Save(document));
        True(history.Redo(document, out _));
        True(history.Redo(document, out _));
        True(history.Redo(document, out _));
        Equal(savedAfterEdits, LadderEditorProjectJson.Save(document));

        history.Execute(document, "Add empty table", () => document.AddTagTable("Empty"));
        history.Execute(document, "Delete empty table", () =>
        {
            if (!document.TryRemoveTagTable(" empty ", out var reason))
                throw new InvalidOperationException(reason);
        });
        False(document.TagTables.Contains("Empty", StringComparer.OrdinalIgnoreCase));
        True(history.Undo(document, out _));
        True(document.TagTables.Contains("Empty", StringComparer.OrdinalIgnoreCase));
        True(history.Redo(document, out _));
        False(document.TagTables.Contains("Empty", StringComparer.OrdinalIgnoreCase));
    }

    private static void TagTableRejectedOperationsAndMalformedDeclarationsAreAtomic()
    {
        var document = new LadderEditorDocument();
        document.AddBlock("Main", LadderBlockType.OrganizationBlock);
        document.AddTag("member", PlcVariableRole.Input, tagTable: "Occupied");
        document.AddTagTable("Empty");
        var before = LadderEditorProjectJson.Save(document);

        False(document.TryRemoveTagTable("Default tag table", out var defaultReason));
        True(defaultReason.Contains("default", StringComparison.OrdinalIgnoreCase));
        False(document.TryRemoveTagTable("Occupied", out var occupiedReason));
        True(occupiedReason.Contains("empty", StringComparison.OrdinalIgnoreCase));
        False(document.TryRemoveTagTable("missing", out var missingReason));
        True(missingReason.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
        var defaultRenameRejected = false;
        try { document.RenameTagTable("Default tag table", "DefaultRenamed"); }
        catch (InvalidOperationException exception)
        { defaultRenameRejected = exception.Message.Contains("default", StringComparison.OrdinalIgnoreCase); }
        True(defaultRenameRejected);
        Equal(before, LadderEditorProjectJson.Save(document));

        foreach (var (table, replacement) in new[]
        {
            ("Empty", " "), ("Empty", "Occupied"), ("missing", "Renamed"),
        })
        {
            var rejected = false;
            try { document.RenameTagTable(table, replacement); }
            catch (InvalidOperationException) { rejected = true; }
            True(rejected);
            Equal(before, LadderEditorProjectJson.Save(document));
        }
        foreach (var (identity, target) in new[] { ("missing-tag-id", "Empty"), ("member", "Missing") })
        {
            var rejected = false;
            try { document.MoveTag(identity, target); }
            catch (InvalidOperationException) { rejected = true; }
            True(rejected);
            Equal(before, LadderEditorProjectJson.Save(document));
        }

        var snapshot = document.CaptureSnapshot();
        var whitespaceMembership = new LadderEditorDocument();
        whitespaceMembership.RestoreSnapshot(snapshot with
        {
            TagTables = new[] { "Default tag table", "Cell" },
            Tags = snapshot.Tags.Select(tag => tag with { TagTable = " Cell " }).ToArray(),
        });
        Equal(2, whitespaceMembership.TagTables.Count);
        True(whitespaceMembership.Tags.All(tag => tag.TagTable == "Cell"));

        foreach (var malformedTables in new IReadOnlyList<string>[]
        {
            new string[] { null! }, [""], ["   "], ["Default tag table", " Cell "],
            ["Occupied", "occupied"], ["Only custom table"],
        })
        {
            var rejected = false;
            try { document.RestoreSnapshot(snapshot with { TagTables = malformedTables }); }
            catch (ArgumentException exception)
            {
                rejected = exception.Message.Contains("tag-table", StringComparison.OrdinalIgnoreCase)
                    || exception.Message.Contains("tag table", StringComparison.OrdinalIgnoreCase);
            }
            True(rejected);
            Equal(before, LadderEditorProjectJson.Save(document));
        }

        foreach (var malformedJson in new[]
        {
            "[null]", "[\"\"]", "[\"  \"]", "[\"Default tag table\",\" Cell \"]",
            "[\"Occupied\",\"occupied\"]", "[\"Only custom table\"]",
        })
        {
            var project = JsonNode.Parse(before)!;
            project["tagTables"] = JsonNode.Parse(malformedJson);
            var loaded = LadderEditorProjectJson.Load(project.ToJsonString());
            False(loaded.IsReadable);
            True(loaded.Issues.Any(issue => issue.Message.Contains("tag-table", StringComparison.OrdinalIgnoreCase)
                || issue.Message.Contains("tag table", StringComparison.OrdinalIgnoreCase)));
        }
        var legacy = JsonNode.Parse(before)!;
        legacy.AsObject().Remove("tagTables");
        True(LadderEditorProjectJson.Load(legacy.ToJsonString()).IsReadable);
    }

    private static void ComparatorDataTypePersistsAndDefaultsForLegacyNodes()
    {
        var selectorTypes = new[]
        {
            LadderComparisonValueType.Unspecified,
            LadderComparisonValueType.Int,
            LadderComparisonValueType.DInt,
            LadderComparisonValueType.Real,
        };
        True(LadderComparisonValueTypePresentation.TiaSelectableTypes.SequenceEqual(selectorTypes));
        for (var index = 0; index < selectorTypes.Length; index++)
        {
            Equal(selectorTypes[index], LadderComparisonValueTypePresentation.FromTiaSelector(index));
            Equal(index, LadderComparisonValueTypePresentation.TiaSelectorIndex(selectorTypes[index]));
        }
        Equal("???", LadderComparisonValueTypePresentation.TiaLabel(LadderComparisonValueType.Unspecified));

        var document = new LadderEditorDocument { Id = "compare-type", Name = "Comparator types" };
        document.AddTag("measured", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: 12L);
        document.AddTag("lamp", PlcVariableRole.Output);
        document.AddRung("Typed compare", "lamp");
        document.AddComparison(0, 0, "measured", LadderCompareOperator.GreaterThan, "3",
            LadderComparisonValueType.DInt);

        var programJson = LadderProgramJson.Save(document.BuildProgram());
        var programRoundTrip = LadderProgramJson.Load(programJson);
        True(programRoundTrip.IsValid && programRoundTrip.Program is not null);
        Equal(LadderComparisonValueType.DInt,
            programRoundTrip.Program!.Networks[0].Logic.Children![0].ComparisonValueType);
        var mismatchedLogic = RetypeComparison(programRoundTrip.Program!.Networks[0].Logic);
        var mismatchedNetwork = programRoundTrip.Program.Networks[0] with { Logic = mismatchedLogic };
        var mismatchedBlock = programRoundTrip.Program.Blocks![0] with { Networks = [mismatchedNetwork] };
        var typeMismatch = programRoundTrip.Program with
        {
            Networks = [mismatchedNetwork],
            Blocks = [mismatchedBlock],
        };
        True(LadderCompiler.Validate(typeMismatch).Any(issue => issue.Code == "VC002"
            && issue.Path.EndsWith(".variable", StringComparison.Ordinal)));

        var projectJson = LadderEditorProjectJson.Save(document);
        var projectRoundTrip = LadderEditorProjectJson.Load(projectJson);
        True(projectRoundTrip.IsReadable && projectRoundTrip.Document is not null);
        Equal(projectJson, LadderEditorProjectJson.Save(projectRoundTrip.Document!));
        Equal(LadderComparisonValueType.DInt,
            projectRoundTrip.Document!.Rungs[0].Branches[0].Contacts[0].ComparisonValueType);

        var legacy = JsonNode.Parse(projectJson)!;
        legacy["schemaVersion"] = 3;
        foreach (var rung in legacy["blocks"]![0]!["rungs"]!.AsArray())
        {
            var root = rung!["root"];
            if (root is null) continue;
            foreach (var node in TraverseNodes(root))
                node.AsObject().Remove("comparisonValueType");
        }
        var migrated = LadderEditorProjectJson.Load(legacy.ToJsonString());
        True(migrated.IsReadable && migrated.Document is not null);
        Equal(LadderComparisonValueType.Unspecified,
            migrated.Document!.Rungs[0].Branches[0].Contacts[0].ComparisonValueType);

        var history = new LadderEditorHistory();
        var editable = document.Rungs[0];
        var compareNode = EditableLadderTree.Visit(editable.Root).Single(node => node.Kind == LadderNodeKind.Compare);
        history.Execute(document, "Change comparator data type", () => editable.SetRoot(
            EditableLadderTree.Replace(editable.Root, compareNode.Id,
                compareNode with { ComparisonValueType = LadderComparisonValueType.Real })));
        Equal(LadderComparisonValueType.Real, editable.Branches[0].Contacts[0].ComparisonValueType);
        True(history.Undo(document, out _));
        Equal(LadderComparisonValueType.DInt, document.Rungs[0].Branches[0].Contacts[0].ComparisonValueType);
        True(history.Redo(document, out _));
        Equal(LadderComparisonValueType.Real, document.Rungs[0].Branches[0].Contacts[0].ComparisonValueType);

        static LadderNode RetypeComparison(LadderNode node) => node.Kind == LadderNodeKind.Compare
            ? node with { ComparisonValueType = LadderComparisonValueType.Int }
            : node with { Children = node.Children?.Select(RetypeComparison).ToArray() };

        static IEnumerable<JsonNode> TraverseNodes(JsonNode node)
        {
            yield return node;
            if (node["children"] is JsonArray children)
                foreach (var child in children)
                    if (child is not null)
                        foreach (var descendant in TraverseNodes(child)) yield return descendant;
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

    private static void HierarchicalAggregateCommentsPersist()
    {
        var document = new LadderEditorDocument();
        document.AddBlock("Main", LadderBlockType.OrganizationBlock);
        document.AddAggregateTag("motor_state", PlcVariableRole.Memory, PlcVariableType.Struct,
            new PlcAggregateSchema([new PlcAggregateField("ready", PlcVariableType.Bool, "Motor permission is satisfied")]),
            new System.Collections.Generic.Dictionary<string, object> { ["ready"] = true },
            comment: "Motor control state");
        var saved = LadderEditorProjectJson.Save(document);
        var reopened = LadderEditorProjectJson.Load(saved);
        True(reopened.IsReadable);
        var aggregate = reopened.Document!.Tags.Single(tag => tag.Name == "motor_state");
        Equal("Motor control state", aggregate.Comment);
        Equal("Motor permission is satisfied", aggregate.Aggregate!.Fields!.Single().Comment);
        var program = LadderProgramJson.Load(LadderProgramJson.Save(reopened.Document.BuildProgram()));
        True(program.IsValid);
        Equal("Motor permission is satisfied", program.Program!.Variables.Single(tag => tag.Name == "motor_state").Aggregate!.Fields!.Single().Comment);
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
