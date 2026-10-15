using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private static IReadOnlyList<PlcVariable> AggregateEditorLeaves(PlcVariable tag)
    {
        try { return PlcAggregates.Expand([tag]); }
        catch (ArgumentException) { return []; } // Invalid saved work remains editable; Verify reports the error.
    }

    private static IEnumerable<PlcVariable> AggregateWatchVariables(IEnumerable<PlcVariable> tags) => tags.SelectMany(tag =>
        PlcAggregates.IsAggregate(tag.Type) ? new[] { tag }.Concat(AggregateEditorLeaves(tag)) : new[] { tag });

    private static string AggregateWatchValue(PlcVariable variable, VirtualControllerSnapshot snapshot)
    {
        if (!snapshot.Aggregates.TryGetValue(variable.Name, out var value)) return "UNAVAILABLE";
        static string Scalar(object item) => item is bool flag ? flag ? "TRUE" : "FALSE" : Convert.ToDouble(item, CultureInfo.InvariantCulture).ToString("G", CultureInfo.InvariantCulture);
        var entries = value is IReadOnlyDictionary<string, object> fields
            ? fields.Select(field => field.Key + "=" + Scalar(field.Value)).ToArray()
            : value is IReadOnlyList<object> elements ? elements.Select((element, i) => $"[{(long)(variable.Aggregate?.LowerBound ?? 0) + i}]=" + Scalar(element)).ToArray() : [];
        return string.Join("; ", entries.Take(16)) + (entries.Length > 16 ? $"; +{entries.Length - 16} elements (select individual symbols)" : "");
    }
    /// <summary>Exercises real widget signals/persistence. This is not native visual acceptance.</summary>
    public bool VerifyAggregateTagEditor(out string result)
    {
        var original = _ladderDocument.CaptureSnapshot();
        var history = _ladderHistory;
        var originalProgram = _virtualProgram; var originalSnapshot = _virtualSnapshot;
        var failures = 0;
        void Check(bool condition, string name) { if (!condition) failures++; GD.Print($"AGGREGATE_EDITOR_VERIFY {name}={condition}"); }
        try
        {
            // Use Godot's parser, so this detects malformed escape tokens rather
            // than merely comparing two implementations of the same formatter.
            var literal = "motors[9] [color=red]";
            var parserProbe = new RichTextLabel();
            try
            {
                parserProbe.BbcodeEnabled = true;
                parserProbe.Text = Escape(literal);
                Check(parserProbe.GetParsedText() == literal, "bbcode_parser_preserves_literal_array_index_and_markup");
            }
            finally { parserProbe.Free(); }
            _ladderDocument.ResetProject("aggregate-ui-qa", "AggregateUI", TimeSpan.FromMilliseconds(20));
            _ladderHistory = new LadderEditorHistory(150);
            SetProductView("ladder");
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            // Search under the known TIA workbench because tab-host nesting can change independently.
            var workbench = GetNode("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench");
            var environment = workbench.GetParent();
            T Widget<T>(string name) where T : Node => workbench.FindChild(name, true, false) as T ?? throw new InvalidOperationException("Missing aggregate editor widget: " + name);
            var type = Widget<OptionButton>("NewTagType"); var nameField = Widget<LineEdit>("NewTagName");
            var role = Widget<OptionButton>("NewTagRole"); var editor = Widget<AggregateTagEditor>("AggregateTagEditor");
            var add = Widget<Button>("AddTag"); var apply = Widget<Button>("ApplyTagEdit"); var table = Widget<Tree>("TagTable");
            type.Select(6); type.EmitSignal(OptionButton.SignalName.ItemSelected, 6L);
            role.Select((int)PlcVariableRole.Memory); role.EmitSignal(OptionButton.SignalName.ItemSelected, (long)PlcVariableRole.Memory);
            nameField.Text = "qa_struct_ui";
            var row = editor.FindChild("AggregateFieldRow0", true, false)!;
            row.GetNode<LineEdit>("FieldName").Text = "ready"; row.GetNode<LineEdit>("FieldInitial").Text = "TRUE";
            editor.FindChild("AddAggregateField", true, false)!.EmitSignal(Button.SignalName.Pressed);
            var realRow = editor.FindChild("AggregateFieldRow1", true, false)!;
            realRow.GetNode<LineEdit>("FieldName").Text = "value"; realRow.GetNode<OptionButton>("FieldType").Select(3); realRow.GetNode<LineEdit>("FieldInitial").Text = "42.25";
            realRow.GetNode<LineEdit>("FieldComment").Text = "Measured process value";
            add.EmitSignal(Button.SignalName.Pressed);
            Check(_ladderDocument.Tags.Single(v => v.Name == "qa_struct_ui").Aggregate?.Fields?.Select(f => f.Name).SequenceEqual(["ready", "value"]) == true, "struct_schema_created_by_widget_handlers");
            Check(_ladderDocument.Tags.Single(v => v.Name == "qa_struct_ui").Aggregate?.Fields?.Single(f => f.Name == "value").Comment == "Measured process value", "struct_member_comment_created_by_widget_handlers");
            type.Select(7); type.EmitSignal(OptionButton.SignalName.ItemSelected, 7L);
            nameField.Text = "qa_array_ui";
            editor.FindChild("AggregateFieldRow9", true, false)!.GetNode<LineEdit>("FieldInitial").Text = "TRUE";
            add.EmitSignal(Button.SignalName.Pressed);
            Check(_ladderDocument.Tags.Single(v => v.Name == "qa_array_ui").Aggregate?.Length == 10, "array_bounds_created_by_widget_handlers");
            // Instruction properties live in a sibling overlay, outside Workbench.
            var selectors = environment.FindChildren("CoilTagSelector", "", true, false).OfType<OptionButton>().ToArray();
            Check(selectors.Any(s => Enumerable.Range(0, s.ItemCount).Any(i => s.GetItemText(i) == "qa_array_ui[9]")), "array_element_operand_available");
            var contacts = environment.FindChildren("ContactTagSelector", "", true, false).OfType<OptionButton>().ToArray();
            Check(contacts.Any(s => Enumerable.Range(0, s.ItemCount).Any(i => s.GetItemText(i) == "qa_struct_ui.ready")), "struct_member_operand_available");
            SelectIndexedTreeRow(table, _ladderDocument.Tags.FindIndex(v => v.Name == "qa_struct_ui")); table.EmitSignal(Tree.SignalName.ItemSelected);
            nameField.Text = "qa_struct_renamed"; apply.EmitSignal(Button.SignalName.Pressed);
            Check(_ladderDocument.Tags.Any(v => v.Name == "qa_struct_renamed" && v.Aggregate?.Fields?.First().Name == "ready"), "aggregate_apply_retains_schema");
            Check(TryUndoLadderEdit(out _) && _ladderDocument.Tags.Any(v => v.Name == "qa_struct_ui"), "aggregate_edit_undo");
            Check(TryRedoLadderEdit(out _) && _ladderDocument.Tags.Any(v => v.Name == "qa_struct_renamed"), "aggregate_edit_redo");
            var loaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(_ladderDocument));
            Check(loaded.IsReadable && LadderCompiler.Compile(loaded.Document!.BuildProgram()).IsValid
                && loaded.Document.Tags.Single(v => v.Name == "qa_struct_renamed").Aggregate?.Fields?.Single(f => f.Name == "value").Comment == "Measured process value",
                "aggregate_ui_save_open_schema_roundtrip");
            SelectIndexedTreeRow(table, _ladderDocument.Tags.FindIndex(v => v.Name == "qa_array_ui")); table.EmitSignal(Tree.SignalName.ItemSelected);
            var before = LadderEditorProjectJson.Save(_ladderDocument);
            editor.FindChild("AggregateFieldRow9", true, false)!.GetNode<LineEdit>("FieldInitial").Text = "not_a_bool";
            apply.EmitSignal(Button.SignalName.Pressed);
            Check(LadderEditorProjectJson.Save(_ladderDocument) == before, "invalid_typed_element_rejected_without_document_change");
            var watchSymbol = Widget<OptionButton>("WatchSymbol"); var addWatch = Widget<Button>("AddWatchSymbol");
            foreach (var symbol in new[] { "qa_struct_renamed", "qa_struct_renamed.ready", "qa_struct_renamed.value", "qa_array_ui[9]" })
            {
                var index = Enumerable.Range(0, watchSymbol.ItemCount).FirstOrDefault(i => watchSymbol.GetItemText(i) == symbol, -1);
                if (index < 0) throw new InvalidOperationException("Missing typed watch symbol: " + symbol);
                watchSymbol.Select(index); addWatch.EmitSignal(Button.SignalName.Pressed);
            }
            var program = _ladderDocument.BuildProgram(); var runtime = new VirtualControllerRuntime(LadderCompiler.Compile(program).Program!);
            runtime.Run(); var snapshot = runtime.Scan(new Dictionary<string, bool>()); AttachVirtualController(program, snapshot);
            var structDisplay = DisplayPointValue(snapshot.Aggregates["qa_struct_renamed"]);
            var arrayDisplay = DisplayPointValue(snapshot.Aggregates["qa_array_ui"]);
            Check(structDisplay == "{ready=True; value=42.25}" && arrayDisplay == "[False, False, False, False, False, False, False, False, False, True]",
                "io_point_formatter_displays_actual_struct_fields_and_array_elements");
            Check(DisplayPointValue(true) == "True" && DisplayPointValue(42.25) == "42.25" && DisplayPointValue("fixture") == "fixture",
                "io_point_formatter_preserves_scalar_behavior");
            var ioParser = new RichTextLabel();
            try
            {
                ioParser.BbcodeEnabled = true;
                ioParser.Text = BuildPointTable([("qa_struct_renamed", "STRUCT", structDisplay, "PC"), ("qa_array_ui", "ARRAY", arrayDisplay, "PLC")], true, "fixture");
                Check(ioParser.GetParsedText().Contains(structDisplay, StringComparison.Ordinal)
                    && ioParser.GetParsedText().Contains(arrayDisplay, StringComparison.Ordinal), "io_point_table_parser_preserves_aggregate_values");
            }
            finally { ioParser.Free(); }
            var watch = Widget<Tree>("WatchTable");
            string WatchText(string symbol)
            {
                for (var item = watch.GetRoot()?.GetFirstChild(); item is not null; item = item.GetNext()) if (item.GetMetadata(0).AsString() == symbol) return item.GetText(2);
                return "MISSING";
            }
            Check(WatchText("qa_struct_renamed").Contains("ready=TRUE", StringComparison.Ordinal) && WatchText("qa_struct_renamed").Contains("value=42.25", StringComparison.Ordinal), "aggregate_root_watch_shows_actual_members");
            Check(WatchText("qa_struct_renamed.ready") == "TRUE" && WatchText("qa_struct_renamed.value") == "42.25" && WatchText("qa_array_ui[9]") == "TRUE", "typed_bool_numeric_element_watches_show_actual_scan_values");
            Check(_virtualControllerVariables.Text.Contains("value=42.25", StringComparison.Ordinal), "aggregate_monitor_never_falls_through_to_false_boolean");
            SelectIndexedTreeRow(table, _ladderDocument.Tags.FindIndex(v => v.Name == "qa_array_ui")); table.EmitSignal(Tree.SignalName.ItemSelected);
            before = LadderEditorProjectJson.Save(_ladderDocument); var undo = _ladderHistory.UndoDescription; var redo = _ladderHistory.RedoDescription;
            nameField.Text = "qa_struct_renamed.ready"; apply.EmitSignal(Button.SignalName.Pressed);
            Check(LadderEditorProjectJson.Save(_ladderDocument) == before && _ladderHistory.UndoDescription == undo && _ladderHistory.RedoDescription == redo && _ladderMonitorMatchesLoadedProgram, "namespace_collision_apply_preserves_document_history_monitor");
            type.Select(6); type.EmitSignal(OptionButton.SignalName.ItemSelected, 6L); nameField.Text = "qa_struct_renamed.ready";
            add.EmitSignal(Button.SignalName.Pressed);
            Check(LadderEditorProjectJson.Save(_ladderDocument) == before && _ladderHistory.UndoDescription == undo && _ladderMonitorMatchesLoadedProgram && Widget<RichTextLabel>("OutputWindow").Text.Contains("rejected", StringComparison.OrdinalIgnoreCase), "namespace_collision_add_visible_rejection_before_history");
        }
        catch (Exception error) { failures++; GD.PushError($"AGGREGATE_EDITOR_VERIFY_EXCEPTION {error}"); }
        finally
        {
            _ladderDocument.RestoreSnapshot(original); _ladderHistory = history;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            if (originalProgram is not null && originalSnapshot is not null) AttachVirtualController(originalProgram, originalSnapshot); else DetachVirtualController();
        }
        result = $"failures={failures}; actual widget handlers and persistence only, native layout/interaction acceptance pending";
        return failures == 0;
    }
}
