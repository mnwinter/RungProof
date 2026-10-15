using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static LadderEditorDocument TypedLesson(string scene)
    {
        True(AuthoredLessonLadderPrograms.TryCreate(scene, out var d));
        return TypedRoundTrip(d);
    }
    private static Dictionary<string, string> TextInputs(params (string Name, string Value)[] values)
        => values.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);

    private static void TestDriveStringLesson()
    {
        var d = TypedLesson("lab-10-01-drive-alarm-code-string");
        var r = Runtime(d.BuildProgram()); r.Run();
        var valid = Inputs(("drive_alarm_string_valid", true), ("alarm_reset", false));
        r.Scan(valid, null, TextInputs(("drive_alarm_text", "Drive reports F003: undervoltage")));
        True(r.Snapshot.Outputs["alarm_code_found"]); True(r.Snapshot.Outputs["drive_alarm_active"]);
        foreach (var text in new[] { "F0030", "XF003", "F030", "", "F003A" })
        {
            r.Scan(valid, null, TextInputs(("drive_alarm_text", text)));
            False(r.Snapshot.Outputs["alarm_code_found"]);
        }
        r.Scan(Inputs(("drive_alarm_string_valid", false)), null, TextInputs(("drive_alarm_text", "F003")));
        False(r.Snapshot.Outputs["alarm_match_valid"]);
        r.Scan(Inputs(("drive_alarm_string_valid", true), ("alarm_reset", true)), null, TextInputs(("drive_alarm_text", "F003")));
        False(r.Snapshot.Outputs["drive_alarm_active"]); False(r.Snapshot.Outputs["alarm_code_found"]);
        // The editable tag, rather than an implementation constant, chooses the code.
        var tag = d.Tags.FindIndex(v => v.Name == "selected_alarm_code");
        d.Tags[tag] = d.Tags[tag] with { InitialValue = "F030" };
        r = Runtime(TypedRoundTrip(d).BuildProgram()); r.Run();
        r.Scan(valid, null, TextInputs(("drive_alarm_text", "F003 F030")));
        True(r.Snapshot.Outputs["alarm_code_found"]);
        r.Scan(valid, null, TextInputs(("drive_alarm_text", "F003"))); False(r.Snapshot.Outputs["alarm_code_found"]);
        r.Stop(); False(r.Snapshot.Outputs["drive_alarm_active"]); r.Reset(); Equal("F030", r.Snapshot.TextVariables["selected_alarm_code"]);
    }
    private static LadderEditorDocument TypedRoundTrip(LadderEditorDocument d)
        {
        var loaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(d));
        True(loaded.IsReadable); return loaded.Document!;
    }

    private static void TestChickenStringLesson()
    {
        var d = TypedLesson("lab-10-02-chicken-label-print");
        var r = Runtime(d.BuildProgram()); r.Run();
        var ready = Inputs(("product_weighed", true), ("printer_ready", true), ("label_data_valid", true), ("print_complete", false), ("application_complete", false));
        r.Scan(ready, NumericInputs(("weight_kg", 1.23456)));
        Equal("CHICKEN 1.235 kg", r.Snapshot.TextOutputs["label_text"]);
        True(r.Snapshot.Outputs["print_request"]); False(r.Snapshot.Outputs["apply_request"]); False(r.Snapshot.Outputs["label_applied"]);
        r.Scan(ready, NumericInputs(("weight_kg", 1.23456))); True(r.Snapshot.Outputs["print_request"]);
        r.Scan(Inputs(("print_complete", true))); False(r.Snapshot.Outputs["print_request"]); True(r.Snapshot.Outputs["apply_request"]);
        r.Scan(Inputs(("application_complete", true))); False(r.Snapshot.Outputs["apply_request"]); True(r.Snapshot.Outputs["label_applied"]);
        r.Stop(); False(r.Snapshot.Outputs["label_applied"]); r.Reset(); Equal("", r.Snapshot.TextOutputs["label_text"]);
        r.Run(); r.Scan(Inputs(("product_weighed", true), ("label_data_valid", false), ("printer_ready", true)), NumericInputs(("weight_kg", 2)));
        False(r.Snapshot.Outputs["print_request"]); Equal("", r.Snapshot.TextOutputs["label_text"]);
        var template = d.Tags.FindIndex(v => v.Name == "label_template"); d.Tags[template] = d.Tags[template] with { InitialValue = "CHICKEN {0:F2} kg" };
        r = Runtime(TypedRoundTrip(d).BuildProgram()); r.Run(); r.Scan(ready, NumericInputs(("weight_kg", 1.23456)));
        Equal("CHICKEN 1.23 kg", r.Snapshot.TextOutputs["label_text"]);
        d.Tags[template] = d.Tags[template] with { InitialValue = "CHICKEN {0:broken" };
        r = Runtime(TypedRoundTrip(d).BuildProgram()); r.Run(); r.Scan(ready, NumericInputs(("weight_kg", 1.2)));
        Equal("", r.Snapshot.TextOutputs["label_text"]); False(r.Snapshot.Outputs["print_request"]);
    }

    private static void TestMotorEnumLesson()
    {
        var d = TypedLesson("lab-10-04-motor-enum-state");
        var r = Runtime(d.BuildProgram()); r.Run();
        r.Scan(Inputs(("start_request", true))); False(r.Snapshot.Outputs["motor_running"]);
        r.Scan(Inputs(("start_request", false)));
        Equal("Stopped", r.Snapshot.TextOutputs["motor_state"]);
        r.Scan(Inputs(("start_request", true))); True(r.Snapshot.Outputs["motor_running"]); Equal("Running", r.Snapshot.TextOutputs["motor_state"]);
        r.Scan(Inputs(("stop_request", true))); False(r.Snapshot.Outputs["motor_running"]); Equal("Stopped", r.Snapshot.TextOutputs["motor_state"]);
        r.Scan(Inputs(("stop_request", false))); False(r.Snapshot.Outputs["motor_running"]);
        r.Scan(Inputs(("fault_active", true), ("reset_request", true), ("stop_request", true), ("start_request", true))); Equal("Fault", r.Snapshot.TextOutputs["motor_state"]); True(r.Snapshot.Variables["fault_latched"]);
        r.Scan(Inputs(("fault_active", false), ("reset_request", false), ("stop_request", false))); Equal("Fault", r.Snapshot.TextOutputs["motor_state"]);
        r.Scan(Inputs(("reset_request", true))); Equal("Stopped", r.Snapshot.TextOutputs["motor_state"]); False(r.Snapshot.Outputs["motor_running"]);
        r.Scan(Inputs(("start_request", false), ("reset_request", false))); r.Scan(Inputs(("start_request", true))); True(r.Snapshot.Outputs["motor_running"]);
        r.Stop(); Equal("Stopped", r.Snapshot.TextOutputs["motor_state"]); False(r.Snapshot.Outputs["motor_running"]);
        r.Run(); r.Scan(Inputs(("start_request", true))); False(r.Snapshot.Outputs["motor_running"]);
        r.Reset(); Equal("Stopped", r.Snapshot.TextOutputs["motor_state"]); False(r.Snapshot.Variables["fault_latched"]);
        r.Run(); r.Scan(Inputs(("start_request", false)));
        r.Scan(Inputs(("start_request", true), ("stop_request", true), ("fault_active", true), ("reset_request", true)));
        Equal("Fault", r.Snapshot.TextOutputs["motor_state"]); False(r.Snapshot.Outputs["motor_running"]); True(r.Snapshot.Outputs["state_valid"]);
        var move = d.Rungs.Single(v => v.IsNumericOperation && v.NumericSourceA == "\"Running\""); move.NumericSourceA = "\"Stopped\"";
        r = Runtime(TypedRoundTrip(d).BuildProgram()); r.Run(); r.Scan(Inputs(("start_request", false))); r.Scan(Inputs(("start_request", true)));
        False(r.Snapshot.Outputs["motor_running"]); Equal("Stopped", r.Snapshot.TextOutputs["motor_state"]);
    }
}
