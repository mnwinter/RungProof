using System;
using RungProof.Next.VirtualController;

namespace RungProof.Next.VirtualController;

/// <summary>Editable offline lesson logic; plants only execute these commands and publish feedback.</summary>
public static class AuthoredLessonLadderPrograms
{
    public static bool TryCreate(string sceneId, out LadderEditorDocument document)
    {
        document = sceneId switch
        {
            "lab-10-01-drive-alarm-code-string" => Drive(),
            "lab-10-02-chicken-label-print" => Chicken(),
            "lab-10-04-motor-enum-state" => Motor(),
            _ => null!,
        };
        return document is not null;
    }

    private static LadderEditorDocument New(string scene, string name)
    {
        var d = new LadderEditorDocument();
        d.ResetProject(scene + "-lesson", name, TimeSpan.FromMilliseconds(20));
        d.SourceSceneId = scene;
        return d;
    }
    private static void Bool(LadderEditorDocument d, string name, PlcVariableRole role)
        => d.AddTag(name, role, role == PlcVariableRole.Memory ? "" : name);
    private static int Coil(LadderEditorDocument d, string label, string output, params string[] contacts)
    {
        var index = d.Rungs.Count;
        d.AddRung(label, output);
        foreach (var c in contacts) d.AddContact(index, 0, c.TrimStart('!'), c.StartsWith('!'));
        return index;
    }

    private static LadderEditorDocument Drive()
    {
        var d = New("lab-10-01-drive-alarm-code-string", "Drive_String_Main");
        d.AddTag("drive_alarm_text", PlcVariableRole.Input, "drive_alarm_text", PlcVariableType.String, "");
        Bool(d, "drive_alarm_string_valid", PlcVariableRole.Input);
        Bool(d, "alarm_reset", PlcVariableRole.Input);
        d.AddTag("selected_alarm_code", PlcVariableRole.Memory, type: PlcVariableType.String, initialValue: "F003");
        foreach (var name in new[] { "alarm_code_found", "drive_alarm_active", "alarm_match_valid" }) Bool(d, name, PlcVariableRole.Output);
        var match = Coil(d, "Whole alarm code token", "alarm_code_found", "drive_alarm_string_valid", "!alarm_reset");
        d.AddComparison(match, 0, "drive_alarm_text", LadderCompareOperator.ContainsCode, "selected_alarm_code");
        Coil(d, "Valid selected code match", "alarm_match_valid", "alarm_code_found", "drive_alarm_string_valid", "!alarm_reset");
        Coil(d, "Drive alarm command", "drive_alarm_active", "alarm_match_valid", "!alarm_reset");
        d.WatchVariables.AddRange(["drive_alarm_text", "selected_alarm_code", "alarm_code_found", "drive_alarm_active"]);
        return d;
    }

    private static LadderEditorDocument Chicken()
    {
        var d = New("lab-10-02-chicken-label-print", "Chicken_Label_Main");
        foreach (var name in new[] { "product_weighed", "printer_ready", "label_data_valid", "print_complete", "application_complete" }) Bool(d, name, PlcVariableRole.Input);
        d.AddTag("weight_kg", PlcVariableRole.Input, "weight_kg", PlcVariableType.Real, 0d);
        d.AddTag("label_template", PlcVariableRole.Memory, type: PlcVariableType.String, initialValue: "CHICKEN {0:F3} kg");
        d.AddTag("label_text", PlcVariableRole.Output, "label_text", PlcVariableType.String, "");
        foreach (var name in new[] { "print_request", "apply_request", "label_applied" }) Bool(d, name, PlcVariableRole.Output);
        var format = d.Rungs.Count;
        d.AddNumericOperationRung("Format weighed product label", LadderNumericOperationKind.FormatText, "weight_kg", "label_template", "label_text");
        d.AddContact(format, 0, "product_weighed", false); d.AddContact(format, 0, "label_data_valid", false);
        var print = Coil(d, "Print until printer acknowledges", "print_request", "product_weighed", "printer_ready", "label_data_valid", "!print_complete", "!application_complete");
        d.AddComparison(print, 0, "label_text", LadderCompareOperator.NotEqual, "\"\"");
        Coil(d, "Apply printed label until application acknowledges", "apply_request", "product_weighed", "label_data_valid", "print_complete", "!application_complete");
        Coil(d, "Application acknowledgement", "label_applied", "application_complete");
        d.WatchVariables.AddRange(["weight_kg", "label_text", "print_request", "print_complete", "apply_request", "application_complete", "label_applied"]);
        return d;
    }

    private static LadderEditorDocument Motor()
    {
        var d = New("lab-10-04-motor-enum-state", "Motor_Enum_Main");
        foreach (var name in new[] { "start_request", "stop_request", "fault_active", "reset_request" }) Bool(d, name, PlcVariableRole.Input);
        foreach (var name in new[] { "fault_latched", "start_seen", "start_edge" }) Bool(d, name, PlcVariableRole.Memory);
        d.AddTag("always_scan", PlcVariableRole.Memory, initialValue: true);
        foreach (var name in new[] { "motor_running", "state_valid" }) Bool(d, name, PlcVariableRole.Output);
        d.AddTag("motor_state", PlcVariableRole.Output, "motor_state", PlcVariableType.Enum, "Stopped", enumMembers: new[] { "Stopped", "Running", "Fault" });
        // Remember every sampled request, including requests blocked by Stop/fault.
        // A held Start cannot restart when a blocking permission disappears.
        var edge = Coil(d, "Fresh Start request", "start_edge", "!start_seen");
        d.InsertEdgeContact(edge, 0, 0, "start_request", LadderEdgeMode.Rising);
        var startEdgeMemory = d.Rungs[edge].Branches[0].Contacts[0].EdgeMemoryOperand!;
        d.UpdateTag(startEdgeMemory, startEdgeMemory, PlcVariableType.Bool, PlcVariableRole.Memory,
            initialValue: true, comment: "Start edge baseline; re-arm after Start is sampled low");
        var fault = Coil(d, "Fault dominates Reset", "fault_latched", "fault_active");
        d.AddParallelBranch(fault); d.AddContact(fault, 1, "fault_latched", false); d.AddContact(fault, 1, "reset_request", true);
        // ENUM itself retains the state. Ordered transitions establish explicit
        // fault > stop > start priority; BOOL output is derived from that state.
        MoveState(d, "Stopped", "reset_request", "!fault_active");
        MoveState(d, "Stopped", "stop_request");
        MoveState(d, "Running", "start_edge", "!stop_request", "!fault_latched", "!fault_active");
        MoveState(d, "Fault", "fault_latched");
        var run = Coil(d, "Motor command from actual enum state", "motor_running");
        d.AddComparison(run, 0, "motor_state", LadderCompareOperator.Equal, "\"Running\"");
        var valid = Coil(d, "Actual enum domain validity", "state_valid");
        d.AddComparison(valid, 0, "motor_state", LadderCompareOperator.Equal, "\"Stopped\"");
        foreach (var state in new[] { "Running", "Fault" })
        {
            var branch = d.Rungs[valid].Branches.Count;
            d.AddParallelBranch(valid);
            d.AddComparison(valid, branch, "motor_state", LadderCompareOperator.Equal, "\"" + state + "\"");
        }
        Coil(d, "Remember sampled Start", "start_seen", "start_request");
        d.WatchVariables.AddRange(["motor_state", "motor_running", "state_valid", "fault_latched", "start_request"]);
        return d;
    }
    private static void MoveState(LadderEditorDocument d, string state, params string[] conditions)
    {
        var index = d.Rungs.Count;
        d.AddNumericOperationRung("State " + state, LadderNumericOperationKind.Move, "\"" + state + "\"", "0", "motor_state");
        foreach (var condition in conditions) d.AddContact(index, 0, condition.TrimStart('!'), condition.StartsWith('!'));
    }
}
