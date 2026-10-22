using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>
/// The five authored demonstrations are paired with real ladder documents.
/// Keeping these documents as code makes the demo selector deterministic and
/// ensures the ladder shown in the editor matches the selected plant contract.
/// </summary>
public static class AuthoredDemoLadderPrograms
{
    // Disk examples and the Demo menu must resolve to the same scene/program.
    // Regenerate with tools/demo-projects after changing an authored demo.
    public static IReadOnlyList<(string SceneId, string RelativePath)> ProjectFiles { get; } =
        Array.AsReadOnly(new[]
        {
            ("lab-4-01-press-count-lamp", "programs/demos/01-press-count-starter.rpproj.json"),
            ("lab-5-01-delayed-lamp", "programs/demos/02-delayed-lamp-timer.rpproj.json"),
            ("scene-2-conveyor-pusher", "programs/demos/03-conveyor-sequence.rpproj.json"),
            ("lab-9-11-pallet-counting", "programs/demos/04-batch-process-blocks.rpproj.json"),
            ("lab-11-13-xy-palletizing", "programs/demos/05-integrated-cell-multi-fb-fc.rpproj.json"),
        });

    public static bool TryCreate(string sceneId, out LadderEditorDocument document)
    {
        document = sceneId switch
        {
            "lab-4-01-press-count-lamp" => CreatePressCount(),
            "lab-5-01-delayed-lamp" => CreateDelayedLamp(),
            "scene-2-conveyor-pusher" => CreateConveyorSequence(),
            "lab-9-11-pallet-counting" => CreateBatchCounter(),
            "lab-11-13-xy-palletizing" => CreateIntegratedPalletizer(),
            _ => null!,
        };
        if (document is not null)
        {
            // Authored demo projects are compared with their checked-in JSON
            // fixtures. Give their watch rows deterministic identities so a
            // schema migration or a new process does not create false drift.
            for (var tableIndex = 0; tableIndex < document.WatchTables.Count; tableIndex++)
            {
                var table = document.WatchTables[tableIndex];
                for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                {
                    var row = table.Rows[rowIndex];
                    table.Rows[rowIndex] = new EditableWatchRow(
                        row.Reference,
                        row.ReferenceKind,
                        row.DisplayFormat,
                        row.ModifyValue,
                        row.Comment,
                        $"{document.Id}-watch-{tableIndex + 1}-row-{rowIndex + 1}");
                }
            }
        }
        return document is not null;
    }

    private static LadderEditorDocument CreatePressCount()
    {
        var document = New("demo-1-press-count", "Press_Count_Main", "lab-4-01-press-count-lamp");
        document.AddTag("pulse_received", PlcVariableRole.Input, "pulse_received",
            comment: "One scan pulse for each learner-operated press event.");
        document.AddTag("reset_pressed", PlcVariableRole.Input, "reset_pressed",
            comment: "Machine-level reset pushbutton input for the PLC counter.");
        document.AddTag("threshold_lamp", PlcVariableRole.Output, "threshold_lamp",
            comment: "PLC output energized when the counter reaches its preset of three.");
        document.AddTag("press_count_value", PlcVariableRole.Output, "press_count_value",
            type: PlcVariableType.DInt,
            comment: "PLC-owned numeric status published from the CTU accumulator for the scene display.");
        document.AddTag("press_count", PlcVariableRole.Memory, type: PlcVariableType.Counter,
            comment: "CTU state: accumulated press count and done status.");
        document.WatchVariables.AddRange(["pulse_received", "reset_pressed", "press_count", "press_count_value", "threshold_lamp"]);

        document.AddCounterRung("Count press pulses", "press_count", 3);
        document.AddContact(0, 0, "pulse_received", false);
        document.AddCounterRung("Machine Reset clears the count", "press_count", 3, reset: true);
        document.AddContact(1, 0, "reset_pressed", false);
        document.AddNumericOperationRung("Publish current press count", LadderNumericOperationKind.Move,
            "press_count.ACC", string.Empty, "press_count_value");
        document.AddRung("Turn on threshold lamp", "threshold_lamp");
        document.AddContact(3, 0, "press_count.DN", false);
        return document;
    }

    private static LadderEditorDocument CreateDelayedLamp()
    {
        var document = New("demo-2-delayed-lamp", "Delayed_Lamp_Main", "lab-5-01-delayed-lamp");
        document.AddTag("timer_request", PlcVariableRole.Input, "timer_request",
            comment: "Maintained learner request that starts the two-second on-delay.");
        document.AddTag("machine_stop", PlcVariableRole.Input, "machine_stop",
            comment: "Machine-level stop pushbutton; clears the timer and output.");
        document.AddTag("reset_pressed", PlcVariableRole.Input, "reset_pressed",
            comment: "Machine-level reset pushbutton; requires REQUEST off before rearming.");
        document.AddTag("delayed_lamp", PlcVariableRole.Output, "delayed_lamp",
            comment: "PLC output energized after REQUEST remains true for two seconds.");
        document.AddTag("delay_elapsed_ms", PlcVariableRole.Output, "delay_elapsed_ms",
            type: PlcVariableType.DInt,
            comment: "PLC-owned elapsed-time status converted from TON.ET for the scene display.");
        document.AddTag("delay_timer", PlcVariableRole.Memory, type: PlcVariableType.Timer,
            comment: "TON state with a two-second preset, elapsed time, and done status.");
        document.AddTag("timer_armed", PlcVariableRole.Memory, initialValue: true,
            comment: "Restart inhibit: Stop or Reset requires a fresh REQUEST off-to-on cycle.");
        document.WatchVariables.AddRange(["timer_request", "machine_stop", "reset_pressed", "timer_armed", "delay_timer", "delay_elapsed_ms", "delayed_lamp"]);

        document.AddTimerRung("Delay lamp request", "delay_timer", TimeSpan.FromSeconds(2));
        document.AddContact(0, 0, "timer_request", false);
        document.AddContact(0, 0, "timer_armed", false);
        document.AddContact(0, 0, "machine_stop", true);
        document.AddContact(0, 0, "reset_pressed", true);
        var rearm = document.AddRung("Re-arm only after request is OFF", "timer_armed");
        rearm.CoilMode = LadderCoilMode.Set;
        document.AddContact(1, 0, "timer_request", true);
        var timerReset = document.AddTimerResetRung("Machine Stop or Reset clears TON", "delay_timer");
        var resetTimerIndex = document.Rungs.IndexOf(timerReset);
        document.AddContact(resetTimerIndex, 0, "machine_stop", false);
        document.AddParallelBranch(resetTimerIndex);
        document.AddContact(resetTimerIndex, 1, "reset_pressed", false);
        var disarm = document.AddRung("Stop or Reset requires a fresh request", "timer_armed");
        disarm.CoilMode = LadderCoilMode.Reset;
        document.AddContact(3, 0, "machine_stop", false);
        document.AddParallelBranch(3);
        document.AddContact(3, 1, "reset_pressed", false);
        document.AddRung("Delayed lamp output", "delayed_lamp");
        document.AddContact(4, 0, "delay_timer.Q", false);
        document.AddContact(4, 0, "timer_armed", false);
        document.AddContact(4, 0, "machine_stop", true);
        document.AddContact(4, 0, "reset_pressed", true);
        document.AddNumericOperationRung("Publish TON elapsed milliseconds", LadderNumericOperationKind.Convert,
            "delay_timer.ET", string.Empty, "delay_elapsed_ms");
        return document;
    }

    private static LadderEditorDocument CreateConveyorSequence()
    {
        var document = New("demo-3-height-sorter", "Height_Sorting_Cell_Main", "scene-2-conveyor-pusher");
        foreach (var input in new[] { "system_enable", "reset_command", "safety_trip", "part_at_pusher",
                     "pusher_extended", "pusher_retracted", "main_line_exit", "side_line_exit", "drive_overload" })
            document.AddTag(input, PlcVariableRole.Input, input);
        document.AddTag("start_command", PlcVariableRole.Input, "start_command");
        document.AddTag("stop_command", PlcVariableRole.Input, "stop_command");
        document.AddTag("carton_height_mm", PlcVariableRole.Input, "carton_height_mm", type: PlcVariableType.DInt);
        document.AddTag("conveyor_running", PlcVariableRole.Output, "conveyor_running");
        document.AddTag("pusher_extend", PlcVariableRole.Output, "pusher_extend");
        document.AddTag("sorter_reset", PlcVariableRole.Output, "sorter_reset");
        foreach (var memory in new[] { "machine_permissive", "machine_run", "sort_active", "divert_latched",
                     "conveyor_request", "pusher_request", "conveyor_fault", "pusher_fault", "always_scan" })
            document.AddTag(memory, PlcVariableRole.Memory, initialValue: memory == "always_scan");
        document.WatchVariables.AddRange(["system_enable", "safety_trip", "machine_permissive", "machine_run",
            "reset_command", "part_at_pusher", "carton_height_mm", "sort_active", "divert_latched",
            "conveyor_running", "pusher_extend", "pusher_extended", "pusher_retracted",
            "main_line_exit", "side_line_exit", "conveyor_fault", "pusher_fault"]);

        var drive = document.AddBlock("FB_ConveyorDrive", LadderBlockType.FunctionBlock);
        foreach (var name in new[] { "Enable", "RunRequest", "OverloadActive" })
            document.AddInterfaceParameter(drive.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Input);
        foreach (var name in new[] { "RunCommand", "Fault" })
            document.AddInterfaceParameter(drive.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Output);
        var pusher = document.AddBlock("FB_SorterPusher", LadderBlockType.FunctionBlock);
        foreach (var name in new[] { "Enable", "ExtendRequest", "ExtendedLimit", "RetractedLimit" })
            document.AddInterfaceParameter(pusher.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Input);
        foreach (var name in new[] { "ExtendCommand", "Fault" })
            document.AddInterfaceParameter(pusher.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Output);
        foreach (var block in new[] { drive, pusher })
            for (var index = 0; index < block.Interface.Count; index++)
                block.Interface[index] = block.Interface[index] with
                { Id = block.Name.ToLowerInvariant() + "." + block.Interface[index].Name.ToLowerInvariant() };
        LadderCallAuthoring.EnsureIdentities(document);
        drive.SemanticMode = LadderSemanticMode.Parameterized;
        pusher.SemanticMode = LadderSemanticMode.Parameterized;
        string Local(EditableBlock block, string name) => "local:" + block.Interface.Single(parameter => parameter.Name == name).Id;
        string Global(string name) => "global:" + document.Tags.Single(tag => tag.Name == name).Id;

        document.SelectBlock(document.Blocks.IndexOf(drive));
        document.AddRung("Conveyor motor command with enable and overload interlock", Local(drive, "RunCommand"));
        document.AddContact(0, 0, Local(drive, "Enable"), false);
        document.AddContact(0, 0, Local(drive, "RunRequest"), false);
        document.AddContact(0, 0, Local(drive, "OverloadActive"), true);
        document.AddRung("Conveyor overload status", Local(drive, "Fault"));
        document.AddContact(1, 0, Local(drive, "OverloadActive"), false);

        document.SelectBlock(document.Blocks.IndexOf(pusher));
        document.AddRung("Pusher extend command until extended limit", Local(pusher, "ExtendCommand"));
        document.AddContact(0, 0, Local(pusher, "Enable"), false);
        document.AddContact(0, 0, Local(pusher, "ExtendRequest"), false);
        document.AddContact(0, 0, Local(pusher, "ExtendedLimit"), true);
        document.AddRung("Contradictory pusher end limits are a fault", Local(pusher, "Fault"));
        document.AddContact(1, 0, Local(pusher, "ExtendedLimit"), false);
        document.AddContact(1, 0, Local(pusher, "RetractedLimit"), false);

        document.SelectBlock(0);
        document.AddRung("Cell permissive: enabled, trip clear, no drive or pusher fault", "machine_permissive");
        document.AddContact(0, 0, "system_enable", false);
        foreach (var fault in new[] { "safety_trip", "conveyor_fault", "pusher_fault" })
            document.AddContact(0, 0, fault, true);

        document.AddRung("Machine Start seal-in; Stop, Reset, trip or fault requires fresh Start", "machine_run");
        document.AddContact(1, 0, "start_command", false);
        document.AddParallelBranch(1);
        document.AddContact(1, 1, "machine_run", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddContact(1, branch, "machine_permissive", false);
            document.AddContact(1, branch, "stop_command", true);
            document.AddContact(1, branch, "reset_command", true);
        }

        document.AddRung("Machine Reset returns the offline carton plant to its start position", "sorter_reset");
        document.AddContact(2, 0, "reset_command", false);
        document.AddContact(2, 0, "safety_trip", true);

        var activeSet = document.AddRung("A carton at the station starts a sort decision", "sort_active");
        activeSet.CoilMode = LadderCoilMode.Set;
        document.AddContact(3, 0, "machine_run", false);
        document.AddContact(3, 0, "part_at_pusher", false);
        var activeReset = document.AddRung("Clear the sort after a measured destination exit or Reset", "sort_active");
        activeReset.CoilMode = LadderCoilMode.Reset;
        document.AddContact(4, 0, "main_line_exit", false);
        document.AddParallelBranch(4);
        document.AddContact(4, 1, "side_line_exit", false);
        document.AddParallelBranch(4);
        document.AddContact(4, 2, "sorter_reset", false);

        var divertSet = document.AddRung("Measured height above 600 mm latches the side-line route", "divert_latched");
        divertSet.CoilMode = LadderCoilMode.Set;
        document.AddContact(5, 0, "machine_run", false);
        document.AddContact(5, 0, "sort_active", false);
        document.AddContact(5, 0, "part_at_pusher", false);
        document.AddComparison(5, 0, "carton_height_mm", LadderCompareOperator.GreaterThan, "600");
        var divertReset = document.AddRung("Clear the diverted route after side exit or Reset", "divert_latched");
        divertReset.CoilMode = LadderCoilMode.Reset;
        document.AddContact(6, 0, "side_line_exit", false);
        document.AddParallelBranch(6);
        document.AddContact(6, 1, "sorter_reset", false);

        document.AddRung("Main conveyor runs unless the tall carton is being diverted", "conveyor_request");
        document.AddContact(7, 0, "machine_run", false);
        document.AddContact(7, 0, "divert_latched", true);
        document.AddContact(7, 0, "pusher_retracted", false);
        document.AddRung("Pusher request follows the PLC-latched tall-carton route", "pusher_request");
        document.AddContact(8, 0, "machine_run", false);
        document.AddContact(8, 0, "divert_latched", false);
        document.AddContact(8, 0, "part_at_pusher", false);

        void AddCall(string label, EditableBlock target, string instanceName, int dbNumber,
            params (string Formal, string Actual)[] bindings)
        {
            var call = document.AddRung(label, string.Empty);
            call.IsCall = true;
            call.CallTarget = target.Id;
            call.CallSemanticMode = LadderSemanticMode.Parameterized;
            call.CallInstanceId = "instance-" + instanceName.ToLowerInvariant();
            call.CallBindings = bindings.Select(binding => new LadderCallBinding(
                target.Interface.Single(parameter => parameter.Name == binding.Formal).Id,
                LadderActualKind.Symbol, Global(binding.Actual))).ToArray();
            document.Instances.Add(new(call.CallInstanceId, instanceName, target.Id,
                new LadderInstanceDataBlock(dbNumber, instanceName + "_DB")));
            document.AddContact(document.Rungs.Count - 1, 0, "always_scan", false);
        }
        AddCall("Conveyor drive FB call", drive, "Sorter_Conveyor", 101,
            ("Enable", "machine_run"), ("RunRequest", "conveyor_request"), ("OverloadActive", "drive_overload"),
            ("RunCommand", "conveyor_running"), ("Fault", "conveyor_fault"));
        AddCall("Pusher actuator FB call", pusher, "Sorter_Pusher", 102,
            ("Enable", "machine_run"), ("ExtendRequest", "pusher_request"), ("ExtendedLimit", "pusher_extended"),
            ("RetractedLimit", "pusher_retracted"), ("ExtendCommand", "pusher_extend"), ("Fault", "pusher_fault"));
        document.EnsureInstanceDataBlocks();
        return document;
    }

    private static LadderEditorDocument CreateBatchCounter()
    {
        var document = New("demo-4-batch-process", "Batch_Process_Main", "lab-9-11-pallet-counting");
        document.AddTag("start_command", PlcVariableRole.Input, "start_command");
        document.AddTag("machine_stop", PlcVariableRole.Input, "machine_stop");
        document.AddTag("reset_pressed", PlcVariableRole.Input, "reset_pressed");
        document.AddTag("pallet_detected", PlcVariableRole.Input, "pallet_detected");
        document.AddTag("pallet_type_valid", PlcVariableRole.Input, "pallet_type_valid");
        document.AddTag("count_request", PlcVariableRole.Input, "count_request");
        document.AddTag("pallet_count_valid", PlcVariableRole.Output, "pallet_count_valid");
        document.AddTag("pallet_count", PlcVariableRole.Output, "pallet_count", type: PlcVariableType.DInt);
        document.AddTag("batch_count_value", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: 0L);
        document.AddTag("batch_done", PlcVariableRole.Memory);
        document.AddTag("batch_ready", PlcVariableRole.Memory);
        // Publish the existing seal-in state to the scene so Start/Stop is
        // visible on the conveyor and inspection spindle, not only in logic.
        document.AddTag("machine_running", PlcVariableRole.Output, "machine_running");
        document.AddTag("always_scan", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.AddRange(["start_command", "machine_stop", "reset_pressed", "machine_running",
            "pallet_detected", "pallet_type_valid", "count_request", "batch_count_value", "batch_done", "pallet_count", "pallet_count_valid"]);

        var counter = document.AddBlock("FB_ValidPalletCounter", LadderBlockType.FunctionBlock);
        var validation = document.AddBlock("FC_BatchValidation", LadderBlockType.Function);
        foreach (var (name, type, section) in new[]
        {
            ("PalletDetected", PlcVariableType.Bool, LadderInterfaceSection.Input),
            ("CountEnable", PlcVariableType.Bool, LadderInterfaceSection.Input),
            ("Reset", PlcVariableType.Bool, LadderInterfaceSection.Input),
            ("BatchDone", PlcVariableType.Bool, LadderInterfaceSection.Output),
            ("Count", PlcVariableType.DInt, LadderInterfaceSection.Output),
            ("CountState", PlcVariableType.Counter, LadderInterfaceSection.Static),
        })
            document.AddInterfaceParameter(counter.Id, name, type, section);
        foreach (var (name, section) in new[]
        {
            ("BatchDone", LadderInterfaceSection.Input),
            ("ValidationEnable", LadderInterfaceSection.Input),
            ("CountValid", LadderInterfaceSection.Output),
        })
            document.AddInterfaceParameter(validation.Id, name, PlcVariableType.Bool, section);

        // Stable formal identities keep the generated demo fixture byte-for-byte
        // deterministic and give the instance counter a predictable DB member.
        foreach (var block in new[] { counter, validation })
            for (var index = 0; index < block.Interface.Count; index++)
                block.Interface[index] = block.Interface[index] with
                { Id = block.Name.ToLowerInvariant() + "." + block.Interface[index].Name.ToLowerInvariant() };
        LadderCallAuthoring.EnsureIdentities(document);
        counter.SemanticMode = LadderSemanticMode.Parameterized;
        validation.SemanticMode = LadderSemanticMode.Parameterized;
        string Local(EditableBlock block, string name) => "local:" + block.Interface.Single(parameter => parameter.Name == name).Id;
        string Global(string name) => "global:" + document.Tags.Single(tag => tag.Name == name).Id;

        document.SelectBlock(document.Blocks.IndexOf(counter));
        document.AddCounterRung("Count valid pallet detection edges", Local(counter, "CountState"), 5);
        // Count the detection edge only when that event has both permissives.
        // Gating a held detection with a newly enabled permissive must not
        // manufacture a new pallet event.
        document.InsertEdgeContact(0, 0, 0, Local(counter, "PalletDetected"), LadderEdgeMode.Rising);
        document.AddContact(0, 0, Local(counter, "CountEnable"), false);
        document.AddContact(0, 0, Local(counter, "Reset"), true);
        document.AddCounterRung("Machine Reset clears the instance count", Local(counter, "CountState"), 5, reset: true);
        document.AddContact(1, 0, Local(counter, "Reset"), false);
        document.AddNumericOperationRung("Publish the instance count", LadderNumericOperationKind.Move,
            Local(counter, "CountState") + ".ACC", string.Empty, Local(counter, "Count"));
        document.AddRung("Publish the instance done state", Local(counter, "BatchDone"));
        document.AddContact(3, 0, Local(counter, "CountState") + ".DN", false);

        document.SelectBlock(document.Blocks.IndexOf(validation));
        document.AddRung("Validate completed batch under current permissives", Local(validation, "CountValid"));
        document.AddContact(0, 0, Local(validation, "BatchDone"), false);
        document.AddContact(0, 0, Local(validation, "ValidationEnable"), false);

        document.SelectBlock(0);
        document.AddRung("Machine Start seal-in; Stop or Reset drops run", "machine_running");
        document.AddContact(0, 0, "start_command", false);
        document.AddParallelBranch(0);
        document.AddContact(0, 1, "machine_running", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddContact(0, branch, "machine_stop", true);
            document.AddContact(0, branch, "reset_pressed", true);
        }
        document.AddRung("Batch event permissive", "batch_ready");
        document.AddContact(1, 0, "pallet_type_valid", false);
        document.AddContact(1, 0, "count_request", false);
        document.AddContact(1, 0, "machine_running", false);
        document.AddContact(1, 0, "reset_pressed", true);

        var counterCall = document.AddRung("Count valid pallet events in DB101", string.Empty);
        counterCall.IsCall = true;
        counterCall.CallTarget = counter.Id;
        counterCall.CallSemanticMode = LadderSemanticMode.Parameterized;
        counterCall.CallInstanceId = "instance-valid-pallet-counter";
        counterCall.CallBindings = new[]
        {
            new LadderCallBinding(counter.Interface.Single(parameter => parameter.Name == "PalletDetected").Id, LadderActualKind.Symbol, Global("pallet_detected")),
            new LadderCallBinding(counter.Interface.Single(parameter => parameter.Name == "CountEnable").Id, LadderActualKind.Symbol, Global("batch_ready")),
            new LadderCallBinding(counter.Interface.Single(parameter => parameter.Name == "Reset").Id, LadderActualKind.Symbol, Global("reset_pressed")),
            new LadderCallBinding(counter.Interface.Single(parameter => parameter.Name == "BatchDone").Id, LadderActualKind.Symbol, Global("batch_done")),
            new LadderCallBinding(counter.Interface.Single(parameter => parameter.Name == "Count").Id, LadderActualKind.Symbol, Global("batch_count_value")),
        };
        document.Instances.Add(new(counterCall.CallInstanceId, "Valid_Pallet_Counter", counter.Id,
            new LadderInstanceDataBlock(101, "Valid_Pallet_Counter_DB")));
        document.AddContact(document.Rungs.Count - 1, 0, "always_scan", false);

        var validationCall = document.AddRung("Validate batch completion in FC", string.Empty);
        validationCall.IsCall = true;
        validationCall.CallTarget = validation.Id;
        validationCall.CallSemanticMode = LadderSemanticMode.Parameterized;
        validationCall.CallBindings = new[]
        {
            new LadderCallBinding(validation.Interface.Single(parameter => parameter.Name == "BatchDone").Id, LadderActualKind.Symbol, Global("batch_done")),
            new LadderCallBinding(validation.Interface.Single(parameter => parameter.Name == "ValidationEnable").Id, LadderActualKind.Symbol, Global("batch_ready")),
            new LadderCallBinding(validation.Interface.Single(parameter => parameter.Name == "CountValid").Id, LadderActualKind.Symbol, Global("pallet_count_valid")),
        };
        document.AddContact(document.Rungs.Count - 1, 0, "always_scan", false);
        // Publish after the counter call so the scene sees this scan's count.
        // Keep the standard Stop policy: output image zero, counter retained.
        document.AddNumericOperationRung("Publish current pallet count", LadderNumericOperationKind.Move,
            "batch_count_value", string.Empty, "pallet_count");
        document.AddContact(document.Rungs.Count - 1, 0, "always_scan", false);
        document.EnsureInstanceDataBlocks();
        return document;
    }

    private static LadderEditorDocument CreateIntegratedPalletizer()
    {
        var document = New("demo-5-integrated-palletizer", "Palletizing_Cell_Main", "lab-11-13-xy-palletizing");
        foreach (var input in new[] { "system_enable", "start_command", "stop_command", "reset_command", "safety_trip",
                     "pallet_position_valid", "carton_at_pick", "gantry_home", "at_pickup", "at_place",
                     "carton_attached", "pick_complete", "cycle_in_progress", "palletizer_fault" })
            document.AddTag(input, PlcVariableRole.Input, input);
        document.AddTag("placed_cartons", PlcVariableRole.Input, "placed_cartons", initialValue: 0L, type: PlcVariableType.DInt);
        foreach (var axisName in new[] { "x", "y", "z" })
            document.AddTag($"gantry_{axisName}_encoder_counts", PlcVariableRole.Input,
                $"gantry_{axisName}_encoder_counts", initialValue: 0L, type: PlcVariableType.DInt);

        foreach (var output in new[] { "gantry_x_positive", "gantry_x_negative", "gantry_y_positive", "gantry_y_negative",
                     "gantry_z_positive", "gantry_z_negative", "vacuum_pick", "palletizer_reset", "layer_complete" })
            document.AddTag(output, PlcVariableRole.Output, output);

        foreach (var memory in new[] { "machine_permissive", "machine_run", "x_positive_request", "x_negative_request",
                     "y_positive_request", "y_negative_request", "z_positive_request", "z_negative_request",
                     "vacuum_request", "x_target_reached", "z_target_reached", "x_home_window", "y_home_window",
                     "z_home_window", "x_axis_fault", "y_axis_fault", "z_axis_fault", "vacuum_fault",
                     "x_encoder_fault", "y_encoder_fault", "z_encoder_fault" })
            document.AddTag(memory, PlcVariableRole.Memory);
        foreach (var axisName in new[] { "x", "y", "z" })
            document.AddTag($"{axisName}_position_counts", PlcVariableRole.Memory, initialValue: 0L, type: PlcVariableType.DInt);
        document.AddTag("cell_active", PlcVariableRole.Output, "cell_active");
        document.AddTag("sequence_step_value", PlcVariableRole.Output, "sequence_step_value",
            initialValue: 0L, type: PlcVariableType.DInt);
        document.AddTag("always_scan", PlcVariableRole.Memory, initialValue: true);
        document.AddTag("sequence_step", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: 0L);
        document.AddTag("layer_count", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: 0L);
        document.AddTag("pick_timer", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.AddTag("cycle_count", PlcVariableRole.Memory, type: PlcVariableType.Counter);

        document.WatchVariables.AddRange(["system_enable", "safety_trip", "machine_run", "sequence_step", "sequence_step_value",
            "pallet_position_valid", "carton_at_pick", "gantry_home", "gantry_x_encoder_counts", "x_position_counts",
            "gantry_y_encoder_counts", "y_position_counts", "gantry_z_encoder_counts", "z_position_counts", "gantry_x_positive", "gantry_x_negative",
            "gantry_y_positive", "gantry_y_negative", "gantry_z_positive", "gantry_z_negative",
            "vacuum_pick", "placed_cartons", "cycle_count", "layer_complete"]);

        var axis = document.AddBlock("FB_AxisDrive", LadderBlockType.FunctionBlock);
        foreach (var name in new[] { "Enable", "PositiveRequest", "NegativeRequest", "PositiveLimit", "NegativeLimit" })
            document.AddInterfaceParameter(axis.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Input);
        foreach (var name in new[] { "PositiveCommand", "NegativeCommand", "Fault" })
            document.AddInterfaceParameter(axis.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Output);

        var vacuum = document.AddBlock("FB_VacuumGripper", LadderBlockType.FunctionBlock);
        foreach (var name in new[] { "Enable", "PickRequest", "AtPickup", "AtPlace", "Attached" })
            document.AddInterfaceParameter(vacuum.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Input);
        foreach (var name in new[] { "VacuumCommand", "Fault" })
            document.AddInterfaceParameter(vacuum.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Output);

        var encoder = document.AddBlock("FB_EncoderChannel", LadderBlockType.FunctionBlock);
        foreach (var name in new[] { "RawCounts", "HomeMinimum", "HomeMaximum", "TravelMinimum", "TravelMaximum" })
            document.AddInterfaceParameter(encoder.Id, name, PlcVariableType.DInt, LadderInterfaceSection.Input);
        document.AddInterfaceParameter(encoder.Id, "PositionCounts", PlcVariableType.DInt, LadderInterfaceSection.Output);
        foreach (var name in new[] { "HomeWindow", "Fault" })
            document.AddInterfaceParameter(encoder.Id, name, PlcVariableType.Bool, LadderInterfaceSection.Output);

        // Authored demo files must regenerate byte-for-byte. Use readable,
        // deterministic formal identities instead of editor-created GUIDs.
        foreach (var block in new[] { axis, vacuum, encoder })
            for (var index = 0; index < block.Interface.Count; index++)
                block.Interface[index] = block.Interface[index] with
                { Id = block.Name.ToLowerInvariant() + "." + block.Interface[index].Name.ToLowerInvariant() };
        LadderCallAuthoring.EnsureIdentities(document);
        axis.SemanticMode = LadderSemanticMode.Parameterized;
        vacuum.SemanticMode = LadderSemanticMode.Parameterized;
        encoder.SemanticMode = LadderSemanticMode.Parameterized;
        string Local(EditableBlock block, string name) => "local:" + block.Interface.Single(parameter => parameter.Name == name).Id;
        string Global(string name) => "global:" + document.Tags.Single(tag => tag.Name == name).Id;

        document.SelectBlock(document.Blocks.IndexOf(axis));
        document.AddRung("Positive direction command with mutual exclusion and end limit", Local(axis, "PositiveCommand"));
        document.AddContact(0, 0, Local(axis, "Enable"), false);
        document.AddContact(0, 0, Local(axis, "PositiveRequest"), false);
        document.AddContact(0, 0, Local(axis, "NegativeRequest"), true);
        document.AddContact(0, 0, Local(axis, "PositiveLimit"), true);
        document.AddRung("Negative direction command with mutual exclusion and end limit", Local(axis, "NegativeCommand"));
        document.AddContact(1, 0, Local(axis, "Enable"), false);
        document.AddContact(1, 0, Local(axis, "NegativeRequest"), false);
        document.AddContact(1, 0, Local(axis, "PositiveRequest"), true);
        document.AddContact(1, 0, Local(axis, "NegativeLimit"), true);
        document.AddRung("Opposed requests are an axis command fault", Local(axis, "Fault"));
        document.AddContact(2, 0, Local(axis, "PositiveRequest"), false);
        document.AddContact(2, 0, Local(axis, "NegativeRequest"), false);

        document.SelectBlock(document.Blocks.IndexOf(vacuum));
        document.AddRung("Vacuum follows the PLC request until the supported place", Local(vacuum, "VacuumCommand"));
        document.AddContact(0, 0, Local(vacuum, "Enable"), false);
        document.AddContact(0, 0, Local(vacuum, "PickRequest"), false);
        document.AddContact(0, 0, Local(vacuum, "AtPlace"), true);
        document.AddRung("Vacuum request must begin at pickup or continue with an attached carton", Local(vacuum, "Fault"));
        document.AddContact(1, 0, Local(vacuum, "PickRequest"), false);
        document.AddContact(1, 0, Local(vacuum, "AtPickup"), true);
        document.AddContact(1, 0, Local(vacuum, "Attached"), true);

        document.SelectBlock(document.Blocks.IndexOf(encoder));
        document.AddNumericOperationRung("Publish measured encoder position", LadderNumericOperationKind.Move,
            Local(encoder, "RawCounts"), string.Empty, Local(encoder, "PositionCounts"));
        document.AddRung("Encoder home window", Local(encoder, "HomeWindow"));
        document.AddComparison(1, 0, Local(encoder, "RawCounts"), LadderCompareOperator.GreaterOrEqual,
            Local(encoder, "HomeMinimum"));
        document.AddComparison(1, 0, Local(encoder, "RawCounts"), LadderCompareOperator.LessOrEqual,
            Local(encoder, "HomeMaximum"));
        document.AddRung("Encoder outside configured travel is a diagnosed fault", Local(encoder, "Fault"));
        document.AddComparison(2, 0, Local(encoder, "RawCounts"), LadderCompareOperator.LessThan,
            Local(encoder, "TravelMinimum"));
        document.AddParallelBranch(2);
        document.AddComparison(2, 1, Local(encoder, "RawCounts"), LadderCompareOperator.GreaterThan,
            Local(encoder, "TravelMaximum"));

        document.SelectBlock(0);
        void AddFbCall(string label, EditableBlock target, string instanceName, int dbNumber,
            params (string Formal, string Actual)[] bindings)
        {
            var call = document.AddRung(label, string.Empty);
            call.IsCall = true;
            call.CallTarget = target.Id;
            call.CallSemanticMode = LadderSemanticMode.Parameterized;
            call.CallInstanceId = "instance-" + instanceName.ToLowerInvariant();
            call.CallBindings = bindings.Select(binding => new LadderCallBinding(
                target.Interface.Single(parameter => parameter.Name == binding.Formal).Id,
                LadderActualKind.Symbol, Global(binding.Actual))).ToArray();
            document.Instances.Add(new(call.CallInstanceId, instanceName, target.Id,
                new LadderInstanceDataBlock(dbNumber, instanceName + "_DB")));
            document.AddContact(document.Rungs.Count - 1, 0, "always_scan", false);
        }
        void AddEncoderCall(string axisName, string instanceName, int dbNumber, long travelMinimum, long travelMaximum)
        {
            var call = document.AddRung($"{axisName.ToUpperInvariant()} encoder FB call", string.Empty);
            call.IsCall = true;
            call.CallTarget = encoder.Id;
            call.CallSemanticMode = LadderSemanticMode.Parameterized;
            call.CallInstanceId = "instance-" + instanceName.ToLowerInvariant();
            LadderCallBinding Literal(string formal, long value) => new(
                encoder.Interface.Single(parameter => parameter.Name == formal).Id,
                LadderActualKind.Literal, Literal: value, LiteralType: PlcVariableType.DInt);
            call.CallBindings = new[]
            {
                new LadderCallBinding(encoder.Interface.Single(parameter => parameter.Name == "RawCounts").Id,
                    LadderActualKind.Symbol, Global($"gantry_{axisName}_encoder_counts")),
                Literal("HomeMinimum", -50L), Literal("HomeMaximum", 50L),
                Literal("TravelMinimum", travelMinimum), Literal("TravelMaximum", travelMaximum),
                new LadderCallBinding(encoder.Interface.Single(parameter => parameter.Name == "PositionCounts").Id,
                    LadderActualKind.Symbol, Global($"{axisName}_position_counts")),
                new LadderCallBinding(encoder.Interface.Single(parameter => parameter.Name == "HomeWindow").Id,
                    LadderActualKind.Symbol, Global($"{axisName}_home_window")),
                new LadderCallBinding(encoder.Interface.Single(parameter => parameter.Name == "Fault").Id,
                    LadderActualKind.Symbol, Global($"{axisName}_encoder_fault")),
            };
            document.Instances.Add(new(call.CallInstanceId, instanceName, encoder.Id,
                new LadderInstanceDataBlock(dbNumber, instanceName + "_DB")));
            document.AddContact(document.Rungs.Count - 1, 0, "always_scan", false);
        }
        AddEncoderCall("x", "X_Encoder", 105, -100L, 13000L);
        AddEncoderCall("y", "Y_Encoder", 106, -9000L, 100L);
        AddEncoderCall("z", "Z_Encoder", 107, -100L, 7000L);

        int Coil(string label, string destination)
        {
            document.AddRung(label, destination);
            return document.Rungs.Count - 1;
        }
        int Move(string label, string value, string destination)
        {
            document.AddNumericOperationRung(label, LadderNumericOperationKind.Move, value, "0", destination);
            return document.Rungs.Count - 1;
        }
        void State(int rung, long value) => document.AddComparison(rung, 0, "sequence_step", LadderCompareOperator.Equal, value.ToString());
        void Running(int rung) => document.AddContact(rung, 0, "machine_run", false);

        var rung = Coil("Cell permission: enabled, pallet valid, no simulated trip or diagnosed fault", "machine_permissive");
        document.AddContact(rung, 0, "system_enable", false);
        document.AddContact(rung, 0, "pallet_position_valid", false);
        foreach (var fault in new[] { "safety_trip", "palletizer_fault", "x_axis_fault", "y_axis_fault", "z_axis_fault", "vacuum_fault",
                     "x_encoder_fault", "y_encoder_fault", "z_encoder_fault" })
            document.AddContact(rung, 0, fault, true);

        rung = Coil("Start seal-in; Stop, Reset, trip, permissive loss or completion requires a fresh Start", "machine_run");
        document.AddContact(rung, 0, "start_command", false);
        document.AddParallelBranch(rung);
        document.AddContact(rung, 1, "machine_run", false);
        foreach (var branch in new[] { 0, 1 })
        {
            document.AddContact(rung, branch, "machine_permissive", false);
            document.AddContact(rung, branch, "stop_command", true);
            document.AddContact(rung, branch, "reset_command", true);
            document.AddContact(rung, branch, "layer_complete", true);
        }

        rung = Coil("Machine Reset command to the kinematic plant", "palletizer_reset");
        document.AddContact(rung, 0, "reset_command", false);

        rung = Coil("X target window selected from the PLC placement index", "x_target_reached");
        foreach (var (branch, placed, target) in new[] { (0, 0, 6900), (1, 1, 12100), (2, 2, 6900), (3, 3, 12100) })
        {
            if (branch > 0) document.AddParallelBranch(rung);
            document.AddComparison(rung, branch, "placed_cartons", LadderCompareOperator.Equal, placed.ToString());
            document.AddComparison(rung, branch, "x_position_counts", LadderCompareOperator.GreaterOrEqual, (target - 50).ToString());
            document.AddComparison(rung, branch, "x_position_counts", LadderCompareOperator.LessOrEqual, (target + 50).ToString());
        }
        rung = Coil("Z target window selected from the PLC placement index", "z_target_reached");
        foreach (var (branch, placed, target) in new[] { (0, 0, 1700), (1, 1, 1700), (2, 2, 6300), (3, 3, 6300) })
        {
            if (branch > 0) document.AddParallelBranch(rung);
            document.AddComparison(rung, branch, "placed_cartons", LadderCompareOperator.Equal, placed.ToString());
            document.AddComparison(rung, branch, "z_position_counts", LadderCompareOperator.GreaterOrEqual, (target - 50).ToString());
            document.AddComparison(rung, branch, "z_position_counts", LadderCompareOperator.LessOrEqual, (target + 50).ToString());
        }

        rung = Move("Start accepted at measured home with a carton available", "10", "sequence_step");
        State(rung, 0); Running(rung); document.AddContact(rung, 0, "gantry_home", false);
        document.AddContact(rung, 0, "carton_at_pick", false);
        document.AddComparison(rung, 0, "placed_cartons", LadderCompareOperator.LessThan, "4");
        foreach (var transition in new[]
        {
            (From: 10L, To: 20L, Feedback: "at_pickup", NormallyClosed: false, Label: "Pickup height reached"),
            (From: 20L, To: 30L, Feedback: "carton_attached", NormallyClosed: false, Label: "Vacuum attachment confirmed"),
            (From: 30L, To: 40L, Feedback: "y_home_window", NormallyClosed: false, Label: "Lift reached carry height"),
            (From: 50L, To: 60L, Feedback: "at_place", NormallyClosed: false, Label: "Supported place height reached"),
            (From: 60L, To: 70L, Feedback: "carton_attached", NormallyClosed: true, Label: "Vacuum release confirmed"),
            (From: 70L, To: 80L, Feedback: "y_home_window", NormallyClosed: false, Label: "Tool cleared pallet at carry height"),
        })
        {
            rung = Move(transition.Label, transition.To.ToString(), "sequence_step");
            State(rung, transition.From); Running(rung);
            document.AddContact(rung, 0, transition.Feedback, transition.NormallyClosed);
        }
        rung = Move("Both horizontal encoders reached the selected pallet slot", "50", "sequence_step");
        State(rung, 40); Running(rung); document.AddContact(rung, 0, "x_target_reached", false);
        document.AddContact(rung, 0, "z_target_reached", false);
        rung = Move("Home return starts the next carton automatically", "10", "sequence_step");
        State(rung, 80); Running(rung); document.AddContact(rung, 0, "gantry_home", false);
        document.AddComparison(rung, 0, "placed_cartons", LadderCompareOperator.LessThan, "4");
        rung = Move("Fourth measured home return completes the layer", "100", "sequence_step");
        State(rung, 80); Running(rung); document.AddContact(rung, 0, "gantry_home", false);
        document.AddComparison(rung, 0, "placed_cartons", LadderCompareOperator.GreaterOrEqual, "4");

        document.AddTimerRung("Pickup vacuum dwell before attachment", "pick_timer", TimeSpan.FromMilliseconds(750));
        rung = document.Rungs.Count - 1; State(rung, 20); Running(rung); document.AddContact(rung, 0, "at_pickup", false);

        rung = Coil("X positive request during pallet transfer", "x_positive_request");
        State(rung, 40); Running(rung); document.AddContact(rung, 0, "x_target_reached", true);
        rung = Coil("X negative request during measured home return", "x_negative_request");
        State(rung, 80); Running(rung); document.AddContact(rung, 0, "x_home_window", true);
        rung = Coil("Z positive request during pallet transfer", "z_positive_request");
        State(rung, 40); Running(rung); document.AddContact(rung, 0, "z_target_reached", true);
        rung = Coil("Z negative request during measured home return", "z_negative_request");
        State(rung, 80); Running(rung); document.AddContact(rung, 0, "z_home_window", true);
        rung = Coil("Y down request for pickup or supported place", "y_negative_request");
        State(rung, 10); Running(rung); document.AddContact(rung, 0, "at_pickup", true);
        document.AddParallelBranch(rung); document.AddComparison(rung, 1, "sequence_step", LadderCompareOperator.Equal, "50");
        document.AddContact(rung, 1, "machine_run", false); document.AddContact(rung, 1, "at_place", true);
        rung = Coil("Y up request after pickup or release", "y_positive_request");
        State(rung, 30); Running(rung); document.AddContact(rung, 0, "y_home_window", true);
        document.AddParallelBranch(rung); document.AddComparison(rung, 1, "sequence_step", LadderCompareOperator.Equal, "70");
        document.AddContact(rung, 1, "machine_run", false); document.AddContact(rung, 1, "y_home_window", true);
        rung = Coil("Vacuum request after dwell and through the supported carry", "vacuum_request");
        State(rung, 20); Running(rung); document.AddContact(rung, 0, "pick_timer.Q", false);
        foreach (var (branch, state) in new[] { (1, 30), (2, 40), (3, 50) })
        {
            document.AddParallelBranch(rung); document.AddComparison(rung, branch, "sequence_step", LadderCompareOperator.Equal, state.ToString());
            document.AddContact(rung, branch, "machine_run", false);
        }

        AddFbCall("X axis FB call", axis, "X_Axis", 101,
            ("Enable", "machine_run"), ("PositiveRequest", "x_positive_request"), ("NegativeRequest", "x_negative_request"),
            ("PositiveLimit", "x_target_reached"), ("NegativeLimit", "x_home_window"),
            ("PositiveCommand", "gantry_x_positive"), ("NegativeCommand", "gantry_x_negative"), ("Fault", "x_axis_fault"));
        AddFbCall("Y axis FB call", axis, "Y_Axis", 102,
            ("Enable", "machine_run"), ("PositiveRequest", "y_positive_request"), ("NegativeRequest", "y_negative_request"),
            ("PositiveLimit", "y_home_window"), ("NegativeLimit", "at_place"),
            ("PositiveCommand", "gantry_y_positive"), ("NegativeCommand", "gantry_y_negative"), ("Fault", "y_axis_fault"));
        AddFbCall("Z axis FB call", axis, "Z_Axis", 103,
            ("Enable", "machine_run"), ("PositiveRequest", "z_positive_request"), ("NegativeRequest", "z_negative_request"),
            ("PositiveLimit", "z_target_reached"), ("NegativeLimit", "z_home_window"),
            ("PositiveCommand", "gantry_z_positive"), ("NegativeCommand", "gantry_z_negative"), ("Fault", "z_axis_fault"));
        AddFbCall("Vacuum gripper FB call", vacuum, "Vacuum_Gripper", 104,
            ("Enable", "machine_run"), ("PickRequest", "vacuum_request"), ("AtPickup", "at_pickup"),
            ("AtPlace", "at_place"), ("Attached", "carton_attached"),
            ("VacuumCommand", "vacuum_pick"), ("Fault", "vacuum_fault"));

        document.AddCounterRung("Count measured placement and home-return edges", "cycle_count", 4);
        rung = document.Rungs.Count - 1; document.InsertEdgeContact(rung, 0, 0, "pick_complete", LadderEdgeMode.Rising);
        document.AddCounterRung("Machine Reset clears completed transfer count", "cycle_count", 4, reset: true);
        rung = document.Rungs.Count - 1; document.AddContact(rung, 0, "reset_command", false);
        rung = Coil("Layer complete follows the fourth physical placement and terminal state", "layer_complete");
        document.AddComparison(rung, 0, "placed_cartons", LadderCompareOperator.GreaterOrEqual, "4");
        document.AddComparison(rung, 0, "sequence_step", LadderCompareOperator.Equal, "100");
        rung = Move("Publish one completed layer", "1", "layer_count");
        document.AddContact(rung, 0, "layer_complete", false);
        rung = Move("Machine Reset clears layer count", "0", "layer_count");
        document.AddContact(rung, 0, "reset_command", false);
        rung = Move("Machine Reset returns the PLC sequence to idle", "0", "sequence_step");
        document.AddContact(rung, 0, "reset_command", false);
        Move("Publish current sequence state to the machine STEP display", "sequence_step", "sequence_step_value");
        rung = Coil("Cell active when an axis or vacuum command is energized", "cell_active");
        var commands = new[] { "gantry_x_positive", "gantry_x_negative", "gantry_y_positive", "gantry_y_negative",
            "gantry_z_positive", "gantry_z_negative", "vacuum_pick" };
        for (var branch = 0; branch < commands.Length; branch++)
        {
            if (branch > 0) document.AddParallelBranch(rung);
            document.AddContact(rung, branch, commands[branch], false);
        }

        document.SelectBlock(0);
        document.EnsureInstanceDataBlocks();
        return document;
    }

    private static void AddAlwaysScannedCall(LadderEditorDocument document, string label, string target)
    {
        var index = document.Rungs.Count;
        document.AddRung(label, string.Empty);
        document.AddContact(index, 0, "always_scan", false);
        document.Rungs[index].IsCall = true;
        document.Rungs[index].CallTarget = target;
    }

    private static LadderEditorDocument New(string id, string name, string sceneId)
    {
        var document = new LadderEditorDocument();
        document.ResetProject(id, name, TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        return document;
    }
}
