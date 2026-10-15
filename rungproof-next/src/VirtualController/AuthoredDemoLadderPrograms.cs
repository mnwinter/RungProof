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
            ("scene-1-conveyor-stop", "programs/demos/03-conveyor-sequence.rpproj.json"),
            ("lab-9-11-pallet-counting", "programs/demos/04-batch-process-blocks.rpproj.json"),
            ("lab-11-13-xy-palletizing", "programs/demos/05-integrated-cell-multi-fb-fc.rpproj.json"),
        });

    public static bool TryCreate(string sceneId, out LadderEditorDocument document)
    {
        document = sceneId switch
        {
            "lab-4-01-press-count-lamp" => CreatePressCount(),
            "lab-5-01-delayed-lamp" => CreateDelayedLamp(),
            "scene-1-conveyor-stop" => CreateConveyorSequence(),
            "lab-9-11-pallet-counting" => CreateBatchCounter(),
            "lab-11-13-xy-palletizing" => CreateIntegratedPalletizer(),
            _ => null!,
        };
        return document is not null;
    }

    private static LadderEditorDocument CreatePressCount()
    {
        var document = New("demo-1-press-count", "Press_Count_Main", "lab-4-01-press-count-lamp");
        document.AddTag("pulse_received", PlcVariableRole.Input, "pulse_received");
        document.AddTag("threshold_lamp", PlcVariableRole.Output, "threshold_lamp");
        document.AddTag("press_count", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("lamp_enable", PlcVariableRole.Memory);
        document.WatchVariables.AddRange(["pulse_received", "press_count", "threshold_lamp"]);

        document.AddCounterRung("Count press pulses", "press_count", 3);
        document.AddContact(0, 0, "pulse_received", false);
        document.AddRung("Turn on threshold lamp", "threshold_lamp");
        document.AddContact(1, 0, "press_count.DN", false);
        return document;
    }

    private static LadderEditorDocument CreateDelayedLamp()
    {
        var document = New("demo-2-delayed-lamp", "Delayed_Lamp_Main", "lab-5-01-delayed-lamp");
        document.AddTag("timer_request", PlcVariableRole.Input, "timer_request");
        document.AddTag("delayed_lamp", PlcVariableRole.Output, "delayed_lamp");
        document.AddTag("delay_timer", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.WatchVariables.AddRange(["timer_request", "delay_timer", "delayed_lamp"]);

        document.AddTimerRung("Delay lamp request", "delay_timer", TimeSpan.FromSeconds(2));
        document.AddContact(0, 0, "timer_request", false);
        document.AddRung("Delayed lamp output", "delayed_lamp");
        document.AddContact(1, 0, "delay_timer.Q", false);
        return document;
    }

    private static LadderEditorDocument CreateConveyorSequence()
    {
        var document = New("demo-3-conveyor-sequence", "Conveyor_Sequence_Main", "scene-1-conveyor-stop");
        document.AddTag("start_command", PlcVariableRole.Input, "operator.start");
        document.AddTag("stop_command", PlcVariableRole.Input, "operator.stop");
        document.AddTag("simulated_photoeye", PlcVariableRole.Input, "simulated_photoeye");
        document.AddTag("seal_in", PlcVariableRole.Memory);
        document.AddTag("conveyor_running", PlcVariableRole.Output, "conveyor_running");
        document.WatchVariables.AddRange(["start_command", "stop_command", "simulated_photoeye", "seal_in", "conveyor_running"]);

        document.AddRung("Start/stop conveyor sequence", "seal_in");
        document.AddContact(0, 0, "start_command", false);
        document.AddContact(0, 0, "stop_command", true);
        document.AddContact(0, 0, "simulated_photoeye", true);
        document.AddParallelBranch(0);
        document.AddContact(0, 1, "seal_in", false);
        document.AddContact(0, 1, "stop_command", true);
        document.AddContact(0, 1, "simulated_photoeye", true);
        document.AddRung("Conveyor output command", "conveyor_running");
        document.AddContact(1, 0, "seal_in", false);
        document.AddContact(1, 0, "simulated_photoeye", true);
        return document;
    }

    private static LadderEditorDocument CreateBatchCounter()
    {
        var document = New("demo-4-batch-process", "Batch_Process_Main", "lab-9-11-pallet-counting");
        document.AddTag("pallet_detected", PlcVariableRole.Input, "pallet_detected");
        document.AddTag("pallet_type_valid", PlcVariableRole.Input, "pallet_type_valid");
        document.AddTag("count_request", PlcVariableRole.Input, "count_request");
        document.AddTag("pallet_count_valid", PlcVariableRole.Output, "pallet_count_valid");
        document.AddTag("pallet_count", PlcVariableRole.Output, "pallet_count", type: PlcVariableType.DInt);
        document.AddTag("batch_count", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("batch_ready", PlcVariableRole.Memory);
        document.AddTag("always_scan", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.AddRange(["pallet_detected", "pallet_type_valid", "count_request", "batch_count", "pallet_count", "pallet_count_valid"]);

        var counter = document.AddBlock("FB_ValidPalletCounter", LadderBlockType.FunctionBlock);
        var validation = document.AddBlock("FC_BatchValidation", LadderBlockType.Function);
        document.SelectBlock(1);
        document.AddCounterRung("Count valid pallets", "batch_count", 5);
        // Count the detection edge only when that event has both permissives.
        // Gating a held detection with a newly enabled permissive must not
        // manufacture a new pallet event.
        document.InsertEdgeContact(0, 0, 0, "pallet_detected", LadderEdgeMode.Rising);
        document.AddContact(0, 0, "pallet_type_valid", false);
        document.AddContact(0, 0, "count_request", false);
        document.SelectBlock(2);
        document.AddRung("Batch ready permissive", "batch_ready");
        document.AddContact(0, 0, "pallet_type_valid", false);
        document.AddContact(0, 0, "count_request", false);
        document.AddRung("Validate pallet count", "pallet_count_valid");
        document.AddContact(1, 0, "batch_count.DN", false);
        document.AddContact(1, 0, "batch_ready", false);
        document.SelectBlock(0);
        AddAlwaysScannedCall(document, "Count valid pallet events", counter.Id);
        AddAlwaysScannedCall(document, "Validate batch completion", validation.Id);
        // Publish after the counter call so the scene sees this scan's count.
        // Keep the standard Stop policy: output image zero, counter retained.
        document.AddNumericOperationRung("Publish current pallet count", LadderNumericOperationKind.Move,
            "batch_count.ACC", string.Empty, "pallet_count");
        document.AddContact(2, 0, "always_scan", false);
        return document;
    }

    private static LadderEditorDocument CreateIntegratedPalletizer()
    {
        var document = New("demo-5-integrated-palletizer", "Palletizing_Cell_Main", "lab-11-13-xy-palletizing");
        foreach (var input in new[] { "system_enable", "start_command", "stop_command", "reset_command", "safety_trip",
                     "pallet_position_valid", "carton_at_pick", "gantry_home", "at_pickup", "at_place",
                     "carton_attached", "pick_complete", "cycle_in_progress", "palletizer_fault" })
            document.AddTag(input, PlcVariableRole.Input, input);
        document.AddTag("placed_cartons", PlcVariableRole.Input, "placed_cartons", type: PlcVariableType.DInt);
        foreach (var axisName in new[] { "x", "y", "z" })
            document.AddTag($"gantry_{axisName}_encoder_counts", PlcVariableRole.Input,
                $"gantry_{axisName}_encoder_counts", type: PlcVariableType.DInt);

        foreach (var output in new[] { "gantry_x_positive", "gantry_x_negative", "gantry_y_positive", "gantry_y_negative",
                     "gantry_z_positive", "gantry_z_negative", "vacuum_pick", "palletizer_reset", "layer_complete" })
            document.AddTag(output, PlcVariableRole.Output, output);

        foreach (var memory in new[] { "machine_permissive", "machine_run", "x_positive_request", "x_negative_request",
                     "y_positive_request", "y_negative_request", "z_positive_request", "z_negative_request",
                     "vacuum_request", "x_target_reached", "z_target_reached", "x_home_window", "y_home_window",
                     "z_home_window", "x_axis_fault", "y_axis_fault", "z_axis_fault", "vacuum_fault" })
            document.AddTag(memory, PlcVariableRole.Memory);
        document.AddTag("cell_active", PlcVariableRole.Output, "cell_active");
        document.AddTag("always_scan", PlcVariableRole.Memory, initialValue: true);
        document.AddTag("sequence_step", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: 0L);
        document.AddTag("layer_count", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: 0L);
        document.AddTag("pick_timer", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.AddTag("cycle_count", PlcVariableRole.Memory, type: PlcVariableType.Counter);

        document.WatchVariables.AddRange(["system_enable", "safety_trip", "machine_run", "sequence_step",
            "pallet_position_valid", "carton_at_pick", "gantry_home", "gantry_x_encoder_counts",
            "gantry_y_encoder_counts", "gantry_z_encoder_counts", "gantry_x_positive", "gantry_x_negative",
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

        // Authored demo files must regenerate byte-for-byte. Use readable,
        // deterministic formal identities instead of editor-created GUIDs.
        foreach (var block in new[] { axis, vacuum })
            for (var index = 0; index < block.Interface.Count; index++)
                block.Interface[index] = block.Interface[index] with
                { Id = block.Name.ToLowerInvariant() + "." + block.Interface[index].Name.ToLowerInvariant() };
        LadderCallAuthoring.EnsureIdentities(document);
        axis.SemanticMode = LadderSemanticMode.Parameterized;
        vacuum.SemanticMode = LadderSemanticMode.Parameterized;
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

        document.SelectBlock(0);
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
        foreach (var fault in new[] { "safety_trip", "palletizer_fault", "x_axis_fault", "y_axis_fault", "z_axis_fault", "vacuum_fault" })
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

        foreach (var (label, axisName) in new[] { ("X", "x"), ("Y", "y"), ("Z", "z") })
        {
            rung = Coil($"{label} encoder home window (±5 mm)", $"{axisName}_home_window");
            document.AddComparison(rung, 0, $"gantry_{axisName}_encoder_counts", LadderCompareOperator.GreaterOrEqual, "-50");
            document.AddComparison(rung, 0, $"gantry_{axisName}_encoder_counts", LadderCompareOperator.LessOrEqual, "50");
        }

        rung = Coil("X target window selected from the PLC placement index", "x_target_reached");
        foreach (var (branch, placed, target) in new[] { (0, 0, 6900), (1, 1, 12100), (2, 2, 6900), (3, 3, 12100) })
        {
            if (branch > 0) document.AddParallelBranch(rung);
            document.AddComparison(rung, branch, "placed_cartons", LadderCompareOperator.Equal, placed.ToString());
            document.AddComparison(rung, branch, "gantry_x_encoder_counts", LadderCompareOperator.GreaterOrEqual, (target - 50).ToString());
            document.AddComparison(rung, branch, "gantry_x_encoder_counts", LadderCompareOperator.LessOrEqual, (target + 50).ToString());
        }
        rung = Coil("Z target window selected from the PLC placement index", "z_target_reached");
        foreach (var (branch, placed, target) in new[] { (0, 0, 1700), (1, 1, 1700), (2, 2, 6300), (3, 3, 6300) })
        {
            if (branch > 0) document.AddParallelBranch(rung);
            document.AddComparison(rung, branch, "placed_cartons", LadderCompareOperator.Equal, placed.ToString());
            document.AddComparison(rung, branch, "gantry_z_encoder_counts", LadderCompareOperator.GreaterOrEqual, (target - 50).ToString());
            document.AddComparison(rung, branch, "gantry_z_encoder_counts", LadderCompareOperator.LessOrEqual, (target + 50).ToString());
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
