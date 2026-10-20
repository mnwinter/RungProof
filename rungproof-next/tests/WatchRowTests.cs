using System;
using System.Linq;
using System.Text.Json.Nodes;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterWatchRowTests()
    {
        Test("watch rows retain identity through symbolic compatibility operations", WatchRowCompatibilityOperations);
        Test("watch rows persist schema 9 authoring fields and table identity", WatchRowSchema9RoundTrip);
        Test("watch rows migrate schema 8 and legacy watchVariables", WatchRowLegacyMigration);
        Test("watch row history deep-clones and reorders authored rows", WatchRowHistoryRoundTrip);
        Test("watch rows reject duplicate aliases and invalid absolute addresses", WatchRowInvalidMetadata);
        Test("watch display formats validate by type and preserve numeric width", WatchDisplayFormatsByType);
        Test("simulator watch modify updates typed runtime values without force", WatchRowRuntimeModify);
    }

    private static void WatchRowCompatibilityOperations()
    {
        var table = new EditableWatchTable("Commissioning");
        table.Variables.AddRange(["first", "second"]);
        var first = table.Rows[0];
        var second = table.Rows[1];
        var absolute = new EditableWatchRow("%I0.0", WatchReferenceKind.AbsoluteAddress);
        table.Rows.Insert(1, absolute);

        Equal(2, table.Variables.Count);
        Equal("second", table.Variables[1]);
        table.Variables[1] = "second_renamed";
        Equal(second.Id, table.Rows.Single(row => row.Reference == "second_renamed").Id);
        table.Variables.Insert(1, "inserted");
        Equal("inserted", table.Variables[1]);
        Equal(WatchReferenceKind.AbsoluteAddress, table.Rows.Single(row => row.Id == absolute.Id).ReferenceKind);

        var removed = table.Variables.RemoveAll(name => name == "first" || name == "second_renamed");
        Equal(2, removed);
        Equal("inserted", table.Variables.Single());
        True(table.Rows.Any(row => row.Id == absolute.Id), "Symbolic removal must preserve address rows.");
        table.Variables.Clear();
        Equal(0, table.Variables.Count);
        Equal(1, table.Rows.Count);
        Equal(absolute.Id, table.Rows[0].Id);

        table.Rows.Add(first);
        table.Rows.Add(new EditableWatchRow("second"));
        var oldOrder = table.Rows.Select(row => row.Id).ToArray();
        var moved = table.Rows[0];
        table.Rows.RemoveAt(0);
        table.Rows.Add(moved);
        Equal(oldOrder[1], table.Rows[0].Id);
        Equal(oldOrder[0], table.Rows[^1].Id);
        Equal(absolute.Id, table.Rows[^1].Id);
        Equal("second", table.Variables[^1]);
    }

    private static void WatchRowSchema9RoundTrip()
    {
        var document = LadderEditorDocument.CreateConveyorExample();
        var startIndex = document.Tags.FindIndex(tag => tag.Name == "start_command");
        document.Tags[startIndex] = document.Tags[startIndex] with { Address = "%I0.0" };
        var table = document.ActiveWatchTable;
        var tableId = table.Id;
        table.Variables.Clear();
        var symbolic = new EditableWatchRow("start_command", WatchReferenceKind.Symbolic,
            "Binary", "TRUE", "Start request row");
        var address = new EditableWatchRow("%I0.0", WatchReferenceKind.AbsoluteAddress,
            "Hex", null, "Absolute input reference");
        table.Rows.Add(symbolic);
        table.Rows.Add(address);

        var secondTable = document.AddWatchTable("Diagnostics");
        var secondTableId = secondTable.Id;
        secondTable.Rows.Add(new EditableWatchRow("%MW12", WatchReferenceKind.AbsoluteAddress,
            "Auto", "42", "Offline desired value"));

        var json = LadderEditorProjectJson.Save(document);
        var root = JsonNode.Parse(json)!;
        Equal(9, root["schemaVersion"]!.GetValue<int>());
        var serializedRows = root["watchTables"]![0]!["rows"]!.AsArray();
        Equal(2, serializedRows.Count);
        False(serializedRows[0]!.AsObject().ContainsKey("monitorValue"));
        False(serializedRows[0]!.AsObject().ContainsKey("quality"));
        False(serializedRows[0]!.AsObject().ContainsKey("force"));

        var loaded = LadderEditorProjectJson.Load(json);
        True(loaded.IsReadable && loaded.Document is not null);
        var restored = loaded.Document!;
        Equal(tableId, restored.WatchTables[0].Id);
        Equal(secondTableId, restored.WatchTables[1].Id);
        Equal(2, restored.WatchTables[0].Rows.Count);
        AssertSameRow(symbolic, restored.WatchTables[0].Rows[0]);
        AssertSameRow(address, restored.WatchTables[0].Rows[1]);
        Equal("%MW12", restored.WatchTables[1].Rows[0].Reference);
        Equal("42", restored.WatchTables[1].Rows[0].ModifyValue!);
        Equal(json, LadderEditorProjectJson.Save(restored));
    }

    private static void WatchRowLegacyMigration()
    {
        var document = LadderEditorDocument.CreateConveyorExample();
        document.WatchVariables.Clear();
        document.WatchVariables.AddRange(["start_command", "stop_command"]);
        var second = document.AddWatchTable("Second");
        second.Variables.AddRange(["seal_in", "conveyor_running"]);
        var tableIds = document.WatchTables.Select(table => table.Id).ToArray();

        var schema8 = JsonNode.Parse(LadderEditorProjectJson.Save(document))!;
        schema8["schemaVersion"] = 8;
        foreach (var table in schema8["watchTables"]!.AsArray())
            table!.AsObject().Remove("rows");
        var migrated = LadderEditorProjectJson.Load(schema8.ToJsonString());
        True(migrated.IsReadable && migrated.Document is not null);
        Equal(8, migrated.SourceSchemaVersion);
        Equal(string.Join(',', tableIds), string.Join(',', migrated.Document!.WatchTables.Select(table => table.Id)));
        Equal("start_command,stop_command", string.Join(',', migrated.Document.WatchTables[0].Variables));
        Equal("seal_in,conveyor_running", string.Join(',', migrated.Document.WatchTables[1].Variables));
        True(migrated.Document.WatchTables.SelectMany(table => table.Rows)
            .All(row => row.ReferenceKind == WatchReferenceKind.Symbolic && row.Id.Length > 0));

        var upgraded = LadderEditorProjectJson.Save(migrated.Document);
        var reopened = LadderEditorProjectJson.Load(upgraded);
        True(reopened.IsReadable && reopened.Document is not null);
        Equal(upgraded, LadderEditorProjectJson.Save(reopened.Document!));

        var schema4 = JsonNode.Parse(upgraded)!;
        schema4["schemaVersion"] = 4;
        schema4.AsObject().Remove("watchTables");
        schema4.AsObject().Remove("userConstants");
        schema4.AsObject().Remove("tagTables");
        var migratedLegacyList = LadderEditorProjectJson.Load(schema4.ToJsonString());
        True(migratedLegacyList.IsReadable && migratedLegacyList.Document is not null);
        Equal("start_command,stop_command", string.Join(',', migratedLegacyList.Document!.WatchVariables));
        True(migratedLegacyList.Document.ActiveWatchTable.Rows
            .All(row => row.ReferenceKind == WatchReferenceKind.Symbolic));

        var legacyCaseVariants = JsonNode.Parse(LadderEditorProjectJson.Save(document))!;
        legacyCaseVariants["schemaVersion"] = 8;
        foreach (var tableNode in legacyCaseVariants["watchTables"]!.AsArray())
            tableNode!.AsObject().Remove("rows");
        legacyCaseVariants["watchTables"]![0]!["variables"] = new JsonArray("start_command", "START_COMMAND");
        var caseVariantMigration = LadderEditorProjectJson.Load(legacyCaseVariants.ToJsonString());
        True(caseVariantMigration.IsReadable && caseVariantMigration.Document is not null,
            "Schema-8 case-distinct symbolic rows were previously accepted and must still migrate.");
        Equal("start_command,START_COMMAND", string.Join(',', caseVariantMigration.Document!.WatchTables[0].Variables));
    }

    private static void WatchRowHistoryRoundTrip()
    {
        var document = LadderEditorDocument.CreateConveyorExample();
        document.WatchVariables.Clear();
        var table = document.ActiveWatchTable;
        var symbolic = new EditableWatchRow("start_command", WatchReferenceKind.Symbolic,
            "Auto", null, "before");
        var absolute = new EditableWatchRow("%I0.0", WatchReferenceKind.AbsoluteAddress,
            "Auto", "TRUE", "address row");
        table.Rows.Add(symbolic);
        table.Rows.Add(absolute);
        var history = new LadderEditorHistory();

        history.Execute(document, "Reorder and edit watch rows", () =>
        {
            table.Rows.RemoveAt(0);
            table.Rows.Add(symbolic);
            table.Rows[1].Comment = "after";
        });
        Equal(absolute.Id, document.ActiveWatchTable.Rows[0].Id);
        Equal("after", document.ActiveWatchTable.Rows[1].Comment);

        True(history.Undo(document, out _));
        var undone = document.ActiveWatchTable;
        Equal(symbolic.Id, undone.Rows[0].Id);
        Equal("before", undone.Rows[0].Comment);
        Equal(absolute.Id, undone.Rows[1].Id);
        False(ReferenceEquals(symbolic, undone.Rows[0]), "Undo must restore detached row snapshots.");

        True(history.Redo(document, out _));
        var redone = document.ActiveWatchTable;
        Equal(absolute.Id, redone.Rows[0].Id);
        Equal(symbolic.Id, redone.Rows[1].Id);
        Equal("after", redone.Rows[1].Comment);
        False(ReferenceEquals(symbolic, redone.Rows[1]), "Redo must restore detached row snapshots.");
    }

    private static void WatchRowInvalidMetadata()
    {
        var document = LadderEditorDocument.CreateConveyorExample();
        document.ActiveWatchTable.Rows.Clear();
        document.ActiveWatchTable.Rows.Add(new EditableWatchRow("start_command"));
        var validJson = LadderEditorProjectJson.Save(document);
        var duplicate = JsonNode.Parse(validJson)!;
        duplicate["watchTables"]![0]!["rows"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "duplicate-case-id",
            ["referenceKind"] = "symbolic",
            ["reference"] = "START_COMMAND",
            ["displayFormat"] = "Auto",
        });
        var distinctCaseRows = LadderEditorProjectJson.Load(duplicate.ToJsonString());
        True(distinctCaseRows.IsReadable && distinctCaseRows.Document is not null,
            "Case-distinct symbolic references remain valid because project identifiers are case-sensitive.");
        Equal(2, distinctCaseRows.Document!.ActiveWatchTable.Rows.Count);

        var invalidAddress = JsonNode.Parse(validJson)!;
        invalidAddress["watchTables"]![0]!["rows"]!.AsArray()[0]!["referenceKind"] = "AbsoluteAddress";
        invalidAddress["watchTables"]![0]!["rows"]!.AsArray()[0]!["reference"] = "%DB1.DBX1.8";
        False(LadderEditorProjectJson.Load(invalidAddress.ToJsonString()).IsReadable,
            "Invalid absolute bit offsets must fail project loading.");

        var invalidFormat = JsonNode.Parse(validJson)!;
        invalidFormat["watchTables"]![0]!["rows"]![0]!["displayFormat"] = "Character string";
        False(LadderEditorProjectJson.Load(invalidFormat.ToJsonString()).IsReadable,
            "A persisted display format that does not match the resolved BOOL type must be rejected.");

        document.ActiveWatchTable.Rows.Add(new EditableWatchRow("%I0.0", WatchReferenceKind.AbsoluteAddress));
        document.ActiveWatchTable.Rows[1].Reference = "%I0.8";
        var captureRejected = false;
        try { _ = document.CaptureSnapshot(); }
        catch (ArgumentException) { captureRejected = true; }
        True(captureRejected, "In-memory invalid rows must not enter history or persistence snapshots.");
    }

    private static void WatchDisplayFormatsByType()
    {
        True(WatchTableDisplayFormat.IsValid(PlcVariableType.Bool, "Hex"));
        False(WatchTableDisplayFormat.IsValid(PlcVariableType.String, "Hex"));
        False(WatchTableDisplayFormat.IsValid(PlcVariableType.Enum, "Dec"));
        True(WatchTableDisplayFormat.IsValid(PlcVariableType.Bool, "Binary"), "Legacy display aliases normalize for validation.");
        Equal("16#FFFF", WatchTableDisplayFormat.FormatInteger(-1, PlcVariableType.Int, "Hex"));
        Equal("16#FFFFFFFF", WatchTableDisplayFormat.FormatInteger(-1, PlcVariableType.DInt, "Hex"));
        Equal("0000000000000001", WatchTableDisplayFormat.FormatInteger(1, PlcVariableType.Int, "Bin"));
        Equal("0.0001", WatchTableDisplayFormat.FormatReal(0.0001, "Auto"));
        Equal("1.23456", WatchTableDisplayFormat.FormatReal(1.23456, "Floating-point number"));
    }

    private static void WatchRowRuntimeModify()
    {
        var document = LadderEditorDocument.CreateConveyorExample();
        document.AddTag("speed_setpoint", PlcVariableRole.Memory, type: PlcVariableType.Int, initialValue: 0L);
        document.AddTag("target_position", PlcVariableRole.Memory, type: PlcVariableType.Real, initialValue: 0.0);
        document.AddTag("operator_note", PlcVariableRole.Memory, type: PlcVariableType.String, initialValue: "idle");
        var compiled = LadderCompiler.Compile(document.BuildProgram());
        True(compiled.IsValid, string.Join("; ", compiled.Issues.Select(issue => issue.Message)));
        var runtime = new VirtualControllerRuntime(compiled.Program!);

        var boolSnapshot = runtime.ModifyVariable("start_command", true);
        True(boolSnapshot.Variables["start_command"]);
        False(boolSnapshot.Forces.ContainsKey("start_command"), "SIM MODIFY must not create a simulator force.");
        var intSnapshot = runtime.ModifyVariable("speed_setpoint", 125);
        Equal(125.0, intSnapshot.NumericVariables["speed_setpoint"]);
        var realSnapshot = runtime.ModifyVariable("target_position", 12.75);
        Equal(12.75, realSnapshot.NumericVariables["target_position"]);
        var textSnapshot = runtime.ModifyVariable("operator_note", "ready");
        Equal("ready", textSnapshot.TextVariables["operator_note"]);

        var rejected = false;
        try { _ = runtime.ModifyVariable("speed_setpoint", 32768); }
        catch (InvalidOperationException) { rejected = true; }
        True(rejected, "INT values outside the signed 16-bit range must be rejected.");
        Equal(125.0, runtime.Snapshot.NumericVariables["speed_setpoint"]);

        rejected = false;
        try
        {
            _ = runtime.ModifyVariables([
                ("start_command", (object)false),
                ("conveyor_running", true),
            ]);
        }
        catch (InvalidOperationException) { rejected = true; }
        True(rejected, "Stopped output edits must reject a whole batch before changing any row.");
        True(runtime.Snapshot.Variables["start_command"], "A rejected batch must not partially apply an earlier input edit.");
        False(runtime.Snapshot.Outputs["conveyor_running"], "STOPPED outputs must remain de-energized after a rejected batch.");

        rejected = false;
        try { _ = runtime.ModifyVariable("operator_note", new string('x', PlcTextValues.MaximumLength + 1)); }
        catch (InvalidOperationException) { rejected = true; }
        True(rejected, "STRING values beyond the declared offline size limit must be rejected.");
        Equal("ready", runtime.Snapshot.TextVariables["operator_note"]);
    }

    private static void AssertSameRow(EditableWatchRow expected, EditableWatchRow actual)
    {
        Equal(expected.Id, actual.Id);
        Equal(expected.ReferenceKind, actual.ReferenceKind);
        Equal(expected.Reference, actual.Reference);
        Equal(expected.DisplayFormat, actual.DisplayFormat);
        Equal(expected.ModifyValue ?? string.Empty, actual.ModifyValue ?? string.Empty);
        Equal(expected.Comment, actual.Comment);
    }
}
