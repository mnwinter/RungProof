using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private async void CaptureEditorMonitoringReview(string[] arguments)
    {
        try
        {
            string Option(string name, string fallback) => arguments.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? fallback;
            var fixture = Option("--review-fixture", "counter");
            var unknownTimerKind = fixture switch
            {
                "timer-unknown" => (LadderTimerKind?)LadderTimerKind.OnDelay,
                "timer-tof-unknown" => LadderTimerKind.OffDelay,
                "timer-tp-unknown" => LadderTimerKind.Pulse,
                "timer-tonr-unknown" => LadderTimerKind.RetentiveOnDelay,
                _ => null,
            };
            var unknownTimerMnemonic = unknownTimerKind switch
            {
                LadderTimerKind.OffDelay => "TOF",
                LadderTimerKind.Pulse => "TP",
                LadderTimerKind.RetentiveOnDelay => "TONR",
                LadderTimerKind.OnDelay => "TON",
                _ => "",
            };
            var unknownCounterKind = fixture switch
            {
                "counter-unknown" => (LadderCounterKind?)LadderCounterKind.CountUp,
                "counter-down-unknown" => LadderCounterKind.CountDown,
                _ => null,
            };
            var unknownComparison = fixture == "compare-unknown";
            var unknownContact = fixture == "contact-unknown";
            var unknownEdge = fixture == "edge-unknown";
            var unknownCoil = fixture == "coil-unknown";
            var unknownProgramControl = fixture == "program-control-unknown";
            var unknownCall = fixture == "call-unknown";
            var numericSkipped = Option("--review-skipped", "false") switch
            {
                "true" => true,
                "false" => false,
                _ => throw new ArgumentException("Skipped-state review must be 'true' or 'false'."),
            };
            var numericSkippedFixture = fixture is "numeric-move-tia" or "numeric-add-tia" or "numeric-sub-tia"
                or "numeric-mul-tia" or "numeric-div-tia" or "numeric-mod-tia" or "numeric-abs-tia" or "numeric-neg-tia"
                or "numeric-sqrt-tia" or "numeric-expt-tia" or "numeric-scientific-tia" or "numeric-trunc-tia"
                or "numeric-convert-tia" or "numeric-round-tia" or "numeric-ceil-tia" or "numeric-floor-tia"
                or "numeric-norm-tia" or "numeric-scale-tia";
            LadderNumericOperation? skippedNumericOperation = null;
            string? skippedNumericEnableId = null;
            var style = Option("--review-style", "tia");
            var reviewMonitor = Option("--review-monitor", "online");
            if (reviewMonitor is not ("online" or "offline"))
                throw new ArgumentException("Review monitor must be 'online' or 'offline'.");
            var enable = Option("--review-enable", "true") switch
            {
                "true" => true,
                "false" => false,
                _ => throw new ArgumentException("Review enable must be 'true' or 'false'."),
            };
            var compareResult = Option("--review-compare-result", "true") switch
            {
                "true" => true,
                "false" => false,
                _ => throw new ArgumentException("Compare result must be 'true' or 'false'."),
            };
            var scientificDomainFault = Option("--review-scientific-domain", "false") switch
            {
                "true" => true,
                "false" => false,
                _ => throw new ArgumentException("Scientific domain review must be 'true' or 'false'."),
            };
            if (numericSkipped && (!numericSkippedFixture || scientificDomainFault))
                throw new ArgumentException("Skipped-state review is available only for normal native TIA numeric fixtures.");
            if (numericSkipped && !enable)
                throw new ArgumentException("Skipped-state review requires the earlier numeric execution to be enabled.");
            var reviewCompareOperator = Option("--review-compare-operator", "not-equal") switch
            {
                "equal" => LadderCompareOperator.Equal,
                "not-equal" => LadderCompareOperator.NotEqual,
                "greater" => LadderCompareOperator.GreaterThan,
                "greater-or-equal" => LadderCompareOperator.GreaterOrEqual,
                "less" => LadderCompareOperator.LessThan,
                "less-or-equal" => LadderCompareOperator.LessOrEqual,
                _ => throw new ArgumentException("Compare operator must be equal, not-equal, greater, greater-or-equal, less, or less-or-equal."),
            };
            if (fixture is not ("counter" or "counter-tia" or "counter-flow" or "counter-untyped-tia" or "counter-down-tia" or "counter-reset-tia" or "counter-unknown" or "counter-down-unknown" or "timer" or "timer-flow" or "timer-unknown" or "timer-tof-unknown" or "timer-tp-unknown" or "timer-tonr-unknown" or "timer-reset-state-tia" or "timer-symbolic-time" or "timer-unassigned-tia" or "timer-tof" or "timer-tp" or "timer-tonr" or "math" or "numeric-move-tia" or "numeric-add-tia" or "numeric-add-overflow-tia" or "numeric-sub-tia" or "numeric-sub-overflow-tia" or "numeric-mul-tia" or "numeric-mul-overflow-tia" or "numeric-div-tia" or "numeric-div-zero-tia" or "numeric-mod-tia" or "numeric-mod-zero-tia" or "numeric-abs-tia" or "numeric-neg-tia" or "numeric-abs-overflow-tia" or "numeric-neg-overflow-tia" or "numeric-sqrt-tia" or "numeric-sqrt-domain-tia" or "numeric-expt-tia" or "numeric-expt-domain-tia" or "numeric-expt-overflow-tia" or "numeric-scientific-tia" or "numeric-trunc-tia" or "numeric-trunc-overflow-tia" or "numeric-convert-tia" or "numeric-convert-overflow-tia" or "numeric-round-tia" or "numeric-round-overflow-tia" or "numeric-ceil-tia" or "numeric-ceil-overflow-tia" or "numeric-floor-tia" or "numeric-floor-overflow-tia" or "numeric-norm-tia" or "numeric-norm-range-tia" or "numeric-norm-invalid-tia" or "numeric-scale-tia" or "numeric-scale-range-tia" or "numeric-scale-overflow-tia" or "numeric-scale-invalid-tia" or "compare-tia" or "compare-tia-inferred" or "compare-unknown" or "contact-tia" or "contact-unknown" or "edge-tia" or "edge-unknown" or "assignment-coil-tia" or "coil-tia" or "coil-unknown" or "program-control-tia" or "program-control-unknown" or "return-value-tia" or "text" or "flow" or "branch-start" or "branch-nested" or "empty-box-tia" or "parameter-call" or "call-unknown"))
                throw new ArgumentException("Unknown monitoring fixture: " + fixture);
            if (style is not ("tia" or "logix")) throw new ArgumentException("Unknown monitoring style: " + style);
            var reviewTimerKind = fixture switch
            {
                "timer-tof" => LadderTimerKind.OffDelay,
                "timer-tp" => LadderTimerKind.Pulse,
                "timer-tonr" => LadderTimerKind.RetentiveOnDelay,
                _ => LadderTimerKind.OnDelay,
            };
            var program = new LadderProgram(1, "monitor-review", "Monitoring review", "LD", TimeSpan.FromMilliseconds(10),
                [fixture.StartsWith("timer", StringComparison.Ordinal)
                    ? new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "Timer enable")
                    : new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
                 new("load", PlcVariableType.Bool, PlcVariableRole.Input, false),
                 new("reset", PlcVariableType.Bool, PlcVariableRole.Input, false),
                 new("count", PlcVariableType.Counter, PlcVariableRole.Memory, 0),
                 new("timer", PlcVariableType.Timer, PlcVariableRole.Memory, 0), new("time_preset", PlcVariableType.Time, PlcVariableRole.Input, TimeSpan.FromMilliseconds(100)),
                 new("value", PlcVariableType.Real, PlcVariableRole.Memory, 3.0),
                 new("result", PlcVariableType.Real, PlcVariableRole.Memory, 0.0), new("zero", PlcVariableType.Real, PlcVariableRole.Memory, 0.0), new("lamp", PlcVariableType.Bool, PlcVariableRole.Output, false)],
                fixture == "counter" ? [new("n1", "CTU observes 1 / 3 before later LOAD overwrites shared state", new("c1", LadderNodeKind.Contact, "enable"), Counter: new("ctu", "count", 3)),
                    new("n2", "LOAD observes 9 / 9 — separate instruction result", new("c2", LadderNodeKind.Contact, "enable"), CounterLoad: new("load", "count", 9))]
                : fixture is "counter-tia" or "counter-untyped-tia" ? [new("n1", "Count press pulses", new("c1", LadderNodeKind.Contact, "enable"), Counter: new("ctu", "count", 3))]
                : fixture == "counter-flow" ? [new("n1", "CTU Q controls outgoing power flow", new("c1", LadderNodeKind.Contact, "enable"), Counter: new("ctu", "count", 3))]
                : fixture == "counter-reset-tia" ? [new("n1", "CTU count and reset inputs", new("c1", LadderNodeKind.Contact, "enable"),
                    Counter: new("ctu", "count", 3, ResetInput: "reset"))]
                : fixture == "counter-down-tia" ? [new("n1", "Load preset and count down", new("c1", LadderNodeKind.Contact, "enable"),
                    Counter: new("ctd", "count", 3, LadderCounterKind.CountDown, LoadInput: "load"))]
                : fixture == "timer-reset-state-tia" ? [
                    new("n1", "TON accumulates before the RT network", new("timer-enable", LadderNodeKind.Contact, "enable"),
                        Timer: new("timer", "timer", TimeSpan.FromMilliseconds(100), LadderTimerKind.OnDelay,
                            new LadderInstanceDataBlock(1, "IEC_Timer_0_DB"))),
                    new("n2", "RT resets the same IEC timer instance", new("reset-enable", LadderNodeKind.Contact, "reset"),
                        TimerReset: new("reset", "timer", new LadderInstanceDataBlock(1, "IEC_Timer_0_DB")))]
                : fixture == "timer-flow" ? [new("n1", "TON Q controls outgoing power flow", new("c1", LadderNodeKind.Contact, "enable"),
                    Timer: new("timer", "timer", TimeSpan.FromMilliseconds(100), LadderTimerKind.OnDelay,
                        new LadderInstanceDataBlock(1, "IEC_Timer_0_DB")))]
                : fixture is "timer" or "timer-symbolic-time" or "timer-unassigned-tia" or "timer-tof" or "timer-tp" or "timer-tonr" ? [new("n1", fixture switch
                    {
                        "timer-tof" => "TOF off-delay active",
                        "timer-tp" => "TP pulse active",
                        "timer-tonr" => "TONR accumulation active",
                        "timer-flow" => "TON Q controls outgoing power flow",
                        _ => "TON on-delay active",
                    }, new("c1", LadderNodeKind.Contact, "enable"), Timer: new("timer", "timer", TimeSpan.FromMilliseconds(100),
                        reviewTimerKind, new LadderInstanceDataBlock(1, "IEC_Timer_0_DB"),
                        PresetOperand: fixture == "timer-symbolic-time" ? "time_preset" : null,
                        ResetOperand: fixture == "timer-tonr" ? "reset" : "FALSE")),
                    new("n2", "Reset timer instance after first call", new("c2", LadderNodeKind.Contact, "enable"), TimerReset: new("reset", "timer"))]
                : [new("n1", "Compare consumes 3 before later MOV writes 9", new("compare", LadderNodeKind.Compare, "value", CompareOperator: LadderCompareOperator.Equal, RightOperand: "3"), Coil: new("coil", "lamp")),
                   new("n2", "MOV writes 9", new("c2", LadderNodeKind.Contact, "enable"), NumericOperation: new("move", LadderNumericOperationKind.Move, "9", "0", "value")),
                   new("n3", "DIV zero reports retained destination", new("c3", LadderNodeKind.Contact, "enable"), NumericOperation: new("divide", LadderNumericOperationKind.Divide, "value", "zero", "result"))]);
            if (fixture is "compare-tia" or "compare-tia-inferred")
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("pc_heartbeat", PlcVariableType.DInt, PlcVariableRole.Input, 0L),
                        new("last_heartbeat", PlcVariableType.DInt, PlcVariableRole.Memory, 0L) }).ToArray(),
                    Networks = [new("compare-inline-net", "Inline DInt comparator", new("compare-inline", LadderNodeKind.Compare,
                        "pc_heartbeat", CompareOperator: reviewCompareOperator, RightOperand: "last_heartbeat",
                        ComparisonValueType: fixture == "compare-tia" ? LadderComparisonValueType.DInt : LadderComparisonValueType.Unspecified),
                        Coil: new("compare-inline-coil", "lamp"))]
                };
            if (unknownComparison)
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("skip_comparison", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("pc_heartbeat", PlcVariableType.DInt, PlcVariableRole.Input, 0L),
                        new("last_heartbeat", PlcVariableType.DInt, PlcVariableRole.Memory, 0L),
                        new("comparison_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false) }).ToArray(),
                    Networks = [
                        new("compare-jump-net", "JMP skips the comparator in the current scan",
                            new("compare-jump-enable", LadderNodeKind.Contact, "skip_comparison"),
                            Jump: new("compare-jump", "done")),
                        new("compare-unknown-net", "Comparator not executed this scan; values come from the earlier cycle",
                            new("compare-unknown", LadderNodeKind.Compare, "pc_heartbeat",
                                CompareOperator: reviewCompareOperator, RightOperand: "last_heartbeat",
                                ComparisonValueType: LadderComparisonValueType.DInt),
                            Coil: new("compare-unknown-coil", "lamp")),
                        new("compare-label-net", "Block-local destination LABEL",
                            new("compare-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("compare-label-coil", "comparison_label_output"),
                            LabelInstruction: new("compare-label", "done"))]
                };
            if (fixture == "contact-tia")
                program = program with
                {
                    Variables = [
                        new("no_false", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "NO false state"),
                        new("no_true", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.1", Comment: "NO true state"),
                        new("nc_false", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.2", Comment: "NC false state"),
                        new("nc_true", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.3", Comment: "NC true state"),
                        new("no_false_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("no_true_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("nc_false_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("nc_true_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("contact-no-false-net", "NO operand FALSE", new("contact-no-false", LadderNodeKind.Contact, "no_false"), Coil: new("contact-no-false-coil", "no_false_out")),
                        new("contact-no-true-net", "NO operand TRUE", new("contact-no-true", LadderNodeKind.Contact, "no_true"), Coil: new("contact-no-true-coil", "no_true_out")),
                        new("contact-nc-false-net", "NC operand FALSE", new("contact-nc-false", LadderNodeKind.Contact, "nc_false", NormallyClosed: true), Coil: new("contact-nc-false-coil", "nc_false_out")),
                        new("contact-nc-true-net", "NC operand TRUE", new("contact-nc-true", LadderNodeKind.Contact, "nc_true", NormallyClosed: true), Coil: new("contact-nc-true-coil", "nc_true_out")),
                    ],
                };
            if (unknownContact)
                program = program with
                {
                    Variables = [
                        new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("skip_contacts", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("no_signal", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.0", Comment: "NO earlier-cycle TRUE"),
                        new("nc_signal", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.1", Comment: "NC earlier-cycle FALSE"),
                        new("no_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("nc_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("contact_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("contact-gate-net", "Current scan skips the NO and NC review networks",
                            new("contact-gate-enable", LadderNodeKind.Contact, "skip_contacts"),
                            Jump: new("contact-gate-jump", "done")),
                        new("contact-no-unknown-net", "NO not executed this scan; value comes from the earlier cycle",
                            new("contact-no-unknown", LadderNodeKind.Contact, "no_signal"),
                            Coil: new("contact-no-unknown-coil", "no_unknown_out")),
                        new("contact-nc-unknown-net", "NC not executed this scan; value comes from the earlier cycle",
                            new("contact-nc-unknown", LadderNodeKind.Contact, "nc_signal", NormallyClosed: true),
                            Coil: new("contact-nc-unknown-coil", "nc_unknown_out")),
                        new("contact-label-net", "Block-local destination LABEL",
                            new("contact-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("contact-label-coil", "contact_label_output"),
                            LabelInstruction: new("contact-label", "done")),
                    ],
                };
            if (fixture == "edge-tia")
                program = program with
                {
                    Variables = [
                        new("rising_signal", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "Positive-edge signal"),
                        new("falling_signal", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.1", Comment: "Negative-edge signal"),
                        new("rising_memory", PlcVariableType.Bool, PlcVariableRole.Memory, false, Address: "%M0.0", Comment: "P edge memory"),
                        new("falling_memory", PlcVariableType.Bool, PlcVariableRole.Memory, false, Address: "%M0.1", Comment: "N edge memory"),
                        new("rising_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("falling_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("edge-rising-net", "Positive-edge contact", new LadderNode("edge-rising", LadderNodeKind.Contact,
                            "rising_signal", EdgeMode: LadderEdgeMode.Rising) { EdgeMemoryOperand = "rising_memory" },
                            Coil: new("edge-rising-coil", "rising_out")),
                        new("edge-falling-net", "Negative-edge contact", new LadderNode("edge-falling", LadderNodeKind.Contact,
                            "falling_signal", EdgeMode: LadderEdgeMode.Falling) { EdgeMemoryOperand = "falling_memory" },
                            Coil: new("edge-falling-coil", "falling_out")),
                    ],
                };
            if (unknownEdge)
                program = program with
                {
                    Variables = [
                        new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("skip_edges", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("rising_signal", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.0", Comment: "P earlier-cycle transition"),
                        new("falling_signal", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.1", Comment: "N earlier-cycle transition"),
                        new("rising_memory", PlcVariableType.Bool, PlcVariableRole.Memory, false, Address: "%M0.0", Comment: "P edge memory"),
                        new("falling_memory", PlcVariableType.Bool, PlcVariableRole.Memory, true, Address: "%M0.1", Comment: "N edge memory"),
                        new("rising_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("falling_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("edge_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("edge-gate-net", "Current scan skips the P and N review networks",
                            new("edge-gate-enable", LadderNodeKind.Contact, "skip_edges"),
                            Jump: new("edge-gate-jump", "done")),
                        new("edge-rising-unknown-net", "P not executed this scan; state comes from the earlier cycle",
                            new LadderNode("edge-rising-unknown", LadderNodeKind.Contact, "rising_signal", EdgeMode: LadderEdgeMode.Rising)
                                { EdgeMemoryOperand = "rising_memory" },
                            Coil: new("edge-rising-unknown-coil", "rising_unknown_out")),
                        new("edge-falling-unknown-net", "N not executed this scan; state comes from the earlier cycle",
                            new LadderNode("edge-falling-unknown", LadderNodeKind.Contact, "falling_signal", EdgeMode: LadderEdgeMode.Falling)
                                { EdgeMemoryOperand = "falling_memory" },
                            Coil: new("edge-falling-unknown-coil", "falling_unknown_out")),
                        new("edge-label-net", "Block-local destination LABEL",
                            new("edge-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("edge-label-coil", "edge_label_output"),
                            LabelInstruction: new("edge-label", "done")),
                    ],
                };
            if (unknownCoil)
                program = program with
                {
                    Variables = [
                        new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("skip_coils", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("coil_input", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.0", Comment: "Earlier-cycle TRUE RLO"),
                        new("assign_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, false, Address: "%Q0.0", Comment: "Assignment earlier-cycle write"),
                        new("set_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, false, Address: "%Q0.1", Comment: "Set earlier-cycle write"),
                        new("reset_unknown_out", PlcVariableType.Bool, PlcVariableRole.Output, true, Address: "%Q0.2", Comment: "Reset earlier-cycle write"),
                        new("coil_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("coil-gate-net", "Current scan skips assignment, Set, and Reset review networks",
                            new("coil-gate-enable", LadderNodeKind.Contact, "skip_coils"),
                            Jump: new("coil-gate-jump", "done")),
                        new("assign-unknown-net", "Assignment not executed this scan; state comes from the earlier cycle",
                            new("assign-unknown-contact", LadderNodeKind.Contact, "coil_input"),
                            Coil: new("assign-unknown-coil", "assign_unknown_out")),
                        new("set-unknown-net", "Set not executed this scan; state comes from the earlier cycle",
                            new("set-unknown-contact", LadderNodeKind.Contact, "coil_input"),
                            Coil: new("set-unknown-coil", "set_unknown_out", LadderCoilMode.Set)),
                        new("reset-unknown-net", "Reset not executed this scan; state comes from the earlier cycle",
                            new("reset-unknown-contact", LadderNodeKind.Contact, "coil_input"),
                            Coil: new("reset-unknown-coil", "reset_unknown_out", LadderCoilMode.Reset)),
                        new("coil-label-net", "Block-local destination LABEL",
                            new("coil-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("coil-label-coil", "coil_label_output"),
                            LabelInstruction: new("coil-label", "done")),
                    ],
                };
            if (fixture == "assignment-coil-tia")
                program = program with
                {
                    Variables = [
                        new("rlo_false", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "False RLO"),
                        new("rlo_true", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.1", Comment: "True RLO"),
                        new("assign_false_writes", PlcVariableType.Bool, PlcVariableRole.Output, true, Address: "%Q0.0", Comment: "Assignment written false"),
                        new("assign_true_writes", PlcVariableType.Bool, PlcVariableRole.Output, false, Address: "%Q0.1", Comment: "Assignment written true"),
                    ],
                    Networks = [
                        new("assign-false-net", "Assignment with FALSE RLO writes FALSE", new("assign-false-contact", LadderNodeKind.Contact, "rlo_false"),
                            Coil: new("assign-false-coil", "assign_false_writes")),
                        new("assign-true-net", "Assignment with TRUE RLO writes TRUE", new("assign-true-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("assign-true-coil", "assign_true_writes")),
                    ],
                };
            if (fixture == "coil-tia")
                program = program with
                {
                    Variables = [
                        new("rlo_false", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "False RLO"),
                        new("rlo_true", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.1", Comment: "True RLO"),
                        new("set_false_holds", PlcVariableType.Bool, PlcVariableRole.Output, true, Address: "%Q0.0", Comment: "Set preserved"),
                        new("set_true_writes", PlcVariableType.Bool, PlcVariableRole.Output, false, Address: "%Q0.1", Comment: "Set written"),
                        new("reset_false_holds", PlcVariableType.Bool, PlcVariableRole.Output, false, Address: "%Q0.2", Comment: "Reset preserved"),
                        new("reset_true_writes", PlcVariableType.Bool, PlcVariableRole.Output, true, Address: "%Q0.3", Comment: "Reset written"),
                    ],
                    Networks = [
                        new("set-false-net", "S with FALSE RLO preserves TRUE", new("set-false-contact", LadderNodeKind.Contact, "rlo_false"),
                            Coil: new("set-false-coil", "set_false_holds", LadderCoilMode.Set)),
                        new("set-true-net", "S with TRUE RLO writes TRUE", new("set-true-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("set-true-coil", "set_true_writes", LadderCoilMode.Set)),
                        new("reset-false-net", "R with FALSE RLO preserves FALSE", new("reset-false-contact", LadderNodeKind.Contact, "rlo_false"),
                            Coil: new("reset-false-coil", "reset_false_holds", LadderCoilMode.Reset)),
                        new("reset-true-net", "R with TRUE RLO writes FALSE", new("reset-true-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("reset-true-coil", "reset_true_writes", LadderCoilMode.Reset)),
                    ],
                };
            if (fixture == "program-control-tia")
                program = program with
                {
                    Variables = [
                        new("rlo_false", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "False RLO"),
                        new("rlo_true", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.1", Comment: "True RLO"),
                        new("after_false_jump", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("skipped_by_jump", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("label_destination_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("after_false_return", PlcVariableType.Bool, PlcVariableRole.Output, false),
                        new("skipped_by_return", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("jmp-false-net", "FALSE JMP falls through", new("jmp-false-contact", LadderNodeKind.Contact, "rlo_false"),
                            Jump: new("jmp-false", "finish")),
                        new("after-false-jmp-net", "Executed after FALSE JMP", new("after-false-jmp-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("after-false-jmp-coil", "after_false_jump")),
                        new("jmp-true-net", "TRUE JMP transfers to LABEL", new("jmp-true-contact", LadderNodeKind.Contact, "rlo_true"),
                            Jump: new("jmp-true", "finish")),
                        new("jump-skipped-net", "Skipped by taken JMP", new("jump-skipped-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("jump-skipped-coil", "skipped_by_jump")),
                        new("finish-label-net", "Block-local destination", new("finish-label-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("finish-label-coil", "label_destination_output"),
                            LabelInstruction: new("finish-label", "finish")),
                        new("ret-false-net", "FALSE RET falls through", new("ret-false-contact", LadderNodeKind.Contact, "rlo_false"),
                            Return: new("ret-false")),
                        new("after-false-ret-net", "Executed after FALSE RET", new("after-false-ret-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("after-false-ret-coil", "after_false_return")),
                        new("ret-true-net", "TRUE RET exits the current block", new("ret-true-contact", LadderNodeKind.Contact, "rlo_true"),
                            Return: new("ret-true")),
                        new("return-skipped-net", "Skipped by taken RET", new("return-skipped-contact", LadderNodeKind.Contact, "rlo_true"),
                            Coil: new("return-skipped-coil", "skipped_by_return")),
                    ],
                };
            if (unknownProgramControl)
                program = program with
                {
                    Variables = [
                        new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("skip_program_control", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("control_false", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("control_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [
                        new("control-gate-net", "Current scan skips the JMP and RET review networks",
                            new("control-gate-enable", LadderNodeKind.Contact, "skip_program_control"),
                            Jump: new("control-gate-jump", "done")),
                        new("jump-unknown-net", "JMP not executed this scan; state comes from the earlier cycle",
                            new("jump-unknown-enable", LadderNodeKind.Contact, "control_false"),
                            Jump: new("jump-unknown", "done")),
                        new("return-unknown-net", "RET not executed this scan; state comes from the earlier cycle",
                            new("return-unknown-enable", LadderNodeKind.Contact, "control_false"),
                            Return: new("return-unknown")),
                        new("control-label-net", "Block-local destination LABEL",
                            new("control-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("control-label-coil", "control_label_output"),
                            LabelInstruction: new("control-label", "done")),
                    ],
                };
            if (fixture == "return-value-tia")
            {
                var mainNetworks = new LadderNetwork[]
                {
                    new("return-value-call-net", "RET FALSE clears the caller ENO", new("return-value-call-contact", LadderNodeKind.Contact, "enable"),
                        Call: new LadderCall("return-value-call", "return-value-sub")),
                };
                var subNetworks = new LadderNetwork[]
                {
                    new("return-value-ret-net", "Return FALSE to the calling block", new("return-value-ret-contact", LadderNodeKind.Contact, "enable"),
                        Return: new LadderReturn("return-value-ret", LadderReturnValueKind.False)),
                };
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.0", Comment: "RET enable")],
                    Networks = mainNetworks,
                    Blocks = [new("return-value-main", "Main", mainNetworks), new("return-value-sub", "ReturnFalse", subNetworks)],
                    EntryBlock = "return-value-main",
                    Tasks = [new LadderTask("return-value-task", "MainTask", LadderTaskKind.Continuous,
                        TimeSpan.FromMilliseconds(10), 10, "return-value-main")],
                };
            }
            if (fixture == "numeric-move-tia")
                program = program with
                {
                    Variables = [
                        new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("source", PlcVariableType.DInt, PlcVariableRole.Memory, 42L),
                        new("destination", PlcVariableType.DInt, PlcVariableRole.Memory, 7L),
                    ],
                    Networks = [new("numeric-move-net", "MOVE DINT value to matching DINT destination",
                        new("numeric-move-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new("numeric-move", LadderNumericOperationKind.Move,
                            "source", "0", "destination"))],
                };
            if (fixture is "numeric-add-tia" or "numeric-add-overflow-tia"
                or "numeric-sub-tia" or "numeric-sub-overflow-tia"
                or "numeric-mul-tia" or "numeric-mul-overflow-tia"
                or "numeric-div-tia" or "numeric-div-zero-tia" or "numeric-mod-tia" or "numeric-mod-zero-tia")
            {
                var overflow = fixture.Contains("overflow", StringComparison.Ordinal);
                var divideByZero = fixture == "numeric-div-zero-tia";
                var moduloByZero = fixture == "numeric-mod-zero-tia";
                var zeroDivisor = divideByZero || moduloByZero;
                var arithmeticKind = fixture switch
                {
                    "numeric-sub-tia" or "numeric-sub-overflow-tia" => LadderNumericOperationKind.Subtract,
                    "numeric-mul-tia" or "numeric-mul-overflow-tia" => LadderNumericOperationKind.Multiply,
                    "numeric-div-tia" or "numeric-div-zero-tia" => LadderNumericOperationKind.Divide,
                    "numeric-mod-tia" or "numeric-mod-zero-tia" => LadderNumericOperationKind.Modulo,
                    _ => LadderNumericOperationKind.Add,
                };
                var initialValue = overflow
                    ? arithmeticKind == LadderNumericOperationKind.Subtract ? -32768 : 32767
                    : zeroDivisor ? 7
                    : fixture is "numeric-div-tia" or "numeric-mod-tia" ? 8 : 3;
                var initialAddend = overflow
                    ? arithmeticKind == LadderNumericOperationKind.Multiply ? 2 : 1
                    : fixture switch
                {
                    "numeric-div-zero-tia" or "numeric-mod-zero-tia" => 0,
                    "numeric-mul-tia" or "numeric-div-tia" => 2,
                    "numeric-mod-tia" => 3,
                    _ => 1,
                };
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("value", PlcVariableType.Int, PlcVariableRole.Memory, initialValue),
                        new("operand_b", PlcVariableType.Int, PlcVariableRole.Memory, initialAddend),
                        new("result", PlcVariableType.Int, PlcVariableRole.Memory, overflow || zeroDivisor ? 12 : 0)],
                    Networks = [new("numeric-add-net", overflow
                            ? TiaNumericInstructionPorts.Mnemonic(arithmeticKind) + " INT overflow"
                            : zeroDivisor ? TiaNumericInstructionPorts.Mnemonic(arithmeticKind) + " INT zero divisor"
                            : "Typed integer arithmetic",
                        new("numeric-add-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-add", arithmeticKind, "value", "operand_b", "result")
                            { DataType = LadderNumericDataType.Int })]
                };
            }
            if (fixture is "numeric-abs-tia" or "numeric-neg-tia" or "numeric-abs-overflow-tia" or "numeric-neg-overflow-tia")
            {
                var isAbsolute = fixture is "numeric-abs-tia" or "numeric-abs-overflow-tia";
                var overflowInput = fixture.Contains("overflow", StringComparison.Ordinal);
                var unaryKind = isAbsolute
                    ? LadderNumericOperationKind.Absolute
                    : LadderNumericOperationKind.Negate;
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("value", PlcVariableType.Int, PlcVariableRole.Memory, overflowInput ? -32768L : -8L),
                        new("result", PlcVariableType.Int, PlcVariableRole.Memory, 0L)],
                    Networks = [new("numeric-unary-net", isAbsolute ? "ABS signed input" : "NEG signed input",
                        new("numeric-unary-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-unary", unaryKind, "value", "0", "result")
                            { DataType = LadderNumericDataType.Int })],
                };
            }
            if (fixture is "numeric-sqrt-tia" or "numeric-sqrt-domain-tia")
            {
                var radicand = fixture == "numeric-sqrt-domain-tia" ? -1.0 : 25.0;
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("radicand", PlcVariableType.Real, PlcVariableRole.Input, radicand),
                        new("result", PlcVariableType.Real, PlcVariableRole.Memory, 0.0)],
                    Networks = [new("numeric-sqrt-net", "SQRT floating-point input", new("numeric-sqrt-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-sqrt", LadderNumericOperationKind.SquareRoot, "radicand", "0", "result")
                            { DataType = LadderNumericDataType.Real })],
                };
            }
            if (fixture is "numeric-expt-tia" or "numeric-expt-domain-tia" or "numeric-expt-overflow-tia")
            {
                var domainFault = fixture == "numeric-expt-domain-tia";
                var overflow = fixture == "numeric-expt-overflow-tia";
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("base_value", PlcVariableType.Real, PlcVariableRole.Input, domainFault ? -2.0 : overflow ? 10.0 : 2.0),
                        new("exponent_value", PlcVariableType.Real, PlcVariableRole.Input, domainFault ? 0.5 : overflow ? 50.0 : 8.0),
                        new("result", PlcVariableType.Real, PlcVariableRole.Memory, 7.0)],
                    Networks = [new("numeric-expt-net", domainFault
                            ? "EXPT invalid real-number domain"
                            : overflow ? "EXPT REAL overflow" : "EXPT floating-point power",
                        new("numeric-expt-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-expt", LadderNumericOperationKind.Exponentiate,
                            "base_value", "exponent_value", "result") { DataType = LadderNumericDataType.Real })],
                };
            }
            if (fixture == "numeric-scientific-tia")
            {
                var scientificName = Option("--review-scientific", "sin").ToLowerInvariant();
                var scientificKind = scientificName switch
                {
                    "ln" => LadderNumericOperationKind.NaturalLog,
                    "sin" => LadderNumericOperationKind.Sine,
                    "cos" => LadderNumericOperationKind.Cosine,
                    "tan" => LadderNumericOperationKind.Tangent,
                    "asin" => LadderNumericOperationKind.ArcSine,
                    "acos" => LadderNumericOperationKind.ArcCosine,
                    "atan" => LadderNumericOperationKind.ArcTangent,
                    _ => throw new ArgumentException("Scientific review must be ln, sin, cos, tan, asin, acos, or atan."),
                };
                if (scientificDomainFault && scientificKind is not (LadderNumericOperationKind.NaturalLog
                        or LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine))
                    throw new ArgumentException("A domain-fault review is available only for LN, ASIN, or ACOS.");
                var scientificInput = (scientificKind, scientificDomainFault) switch
                {
                    (LadderNumericOperationKind.NaturalLog, true) => -1.0,
                    (LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine, true) => 2.0,
                    (LadderNumericOperationKind.NaturalLog, false) => Math.E,
                    (LadderNumericOperationKind.Sine, false) => Math.PI / 2,
                    (LadderNumericOperationKind.Cosine, false) => Math.PI,
                    (LadderNumericOperationKind.Tangent, false) => Math.PI / 4,
                    (LadderNumericOperationKind.ArcSine, false) => 1.0,
                    (LadderNumericOperationKind.ArcCosine, false) => -1.0,
                    _ => 1.0,
                };
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("source_value", PlcVariableType.Real, PlcVariableRole.Input, scientificInput),
                        new("result", PlcVariableType.Real, PlcVariableRole.Memory, 7.0)],
                    Networks = [new("numeric-scientific-net",
                        scientificDomainFault ? scientificName.ToUpperInvariant() + " invalid input" : scientificName.ToUpperInvariant() + " floating-point function",
                        new("numeric-scientific-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-scientific", scientificKind,
                            "source_value", "0", "result") { DataType = LadderNumericDataType.Real })],
                };
            }
            if (fixture is "numeric-trunc-tia" or "numeric-trunc-overflow-tia")
            {
                var overflow = fixture == "numeric-trunc-overflow-tia";
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("source_value", PlcVariableType.Real, PlcVariableRole.Input, overflow ? 40000.9 : -7.9),
                        new("result", PlcVariableType.Int, PlcVariableRole.Memory, overflow ? 12L : 7L)],
                    Networks = [new("numeric-trunc-net", overflow ? "TRUNC INT overflow" : "TRUNC REAL to INT",
                        new("numeric-trunc-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-trunc", LadderNumericOperationKind.Truncate,
                            "source_value", "0", "result")
                        { InputDataType = LadderNumericDataType.Real, OutputDataType = LadderNumericDataType.Int })],
                };
            }
            if (fixture is "numeric-convert-tia" or "numeric-convert-overflow-tia")
            {
                var overflow = fixture == "numeric-convert-overflow-tia";
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("source", overflow ? PlcVariableType.Real : PlcVariableType.Int,
                            PlcVariableRole.Input, overflow ? (object)40000.0 : 42L),
                        new("destination", overflow ? PlcVariableType.Int : PlcVariableType.DInt,
                            PlcVariableRole.Memory, overflow ? 12L : 7L)],
                    Networks = [new("numeric-convert-net", overflow ? "REAL to INT overflow" : "INT input converted to DINT",
                        new("numeric-convert-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-convert", LadderNumericOperationKind.Convert,
                            "source", "0", "destination")
                        {
                            InputDataType = overflow ? LadderNumericDataType.Real : LadderNumericDataType.Int,
                            OutputDataType = overflow ? LadderNumericDataType.Int : LadderNumericDataType.DInt,
                        })],
                };
            }
            if (fixture is "numeric-round-tia" or "numeric-round-overflow-tia"
                or "numeric-ceil-tia" or "numeric-ceil-overflow-tia"
                or "numeric-floor-tia" or "numeric-floor-overflow-tia")
            {
                var overflow = fixture is "numeric-round-overflow-tia" or "numeric-ceil-overflow-tia" or "numeric-floor-overflow-tia";
                var kind = fixture switch
                {
                    "numeric-ceil-tia" or "numeric-ceil-overflow-tia" => LadderNumericOperationKind.Ceiling,
                    "numeric-floor-tia" or "numeric-floor-overflow-tia" => LadderNumericOperationKind.Floor,
                    _ => LadderNumericOperationKind.Round,
                };
                var sourceValue = overflow ? 3_000_000_000.0
                    : kind == LadderNumericOperationKind.Round ? 1.5 : -0.5;
                var expectedText = kind switch
                {
                    LadderNumericOperationKind.Round => "ROUND REAL to DINT",
                    LadderNumericOperationKind.Ceiling => "CEIL REAL to DINT",
                    _ => "FLOOR REAL to DINT",
                };
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("source", PlcVariableType.Real, PlcVariableRole.Input, sourceValue),
                        new("destination", PlcVariableType.DInt, PlcVariableRole.Memory, overflow ? 12L : 7L)],
                    Networks = [new("numeric-rounding-net", overflow ? kind + " DINT overflow" : expectedText,
                        new("numeric-rounding-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-rounding", kind,
                            "source", "0", "destination")
                        {
                            InputDataType = LadderNumericDataType.Real,
                            OutputDataType = LadderNumericDataType.DInt,
                        })],
                };
            }
            if (fixture is "numeric-norm-tia" or "numeric-norm-range-tia" or "numeric-norm-invalid-tia")
            {
                var rangeFault = fixture == "numeric-norm-range-tia";
                var invalid = fixture == "numeric-norm-invalid-tia";
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("minimum", invalid ? PlcVariableType.Real : PlcVariableType.DInt, PlcVariableRole.Input,
                            invalid ? (object)10.0 : rangeFault ? 30L : 10L),
                        new("value", invalid ? PlcVariableType.Real : PlcVariableType.DInt, PlcVariableRole.Input,
                            invalid ? (object)20.0 : 20L),
                        new("maximum", invalid ? PlcVariableType.Real : PlcVariableType.DInt, PlcVariableRole.Input,
                            invalid ? (object)30.0 : rangeFault ? 10L : 30L),
                        new("result", PlcVariableType.Real, PlcVariableRole.Memory, 7.0)],
                    Networks = [new("numeric-norm-net", rangeFault
                            ? "NORM_X invalid MIN/MAX range"
                            : invalid ? "NORM_X invalid REAL value" : "DINT range normalized to REAL",
                        new("numeric-norm-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-norm", LadderNumericOperationKind.Normalize,
                            "minimum", "value", "result", "maximum")
                        { InputDataType = invalid ? LadderNumericDataType.Real : LadderNumericDataType.DInt,
                            OutputDataType = LadderNumericDataType.Real })],
                };
            }
            if (fixture is "numeric-scale-tia" or "numeric-scale-range-tia" or "numeric-scale-overflow-tia" or "numeric-scale-invalid-tia")
            {
                var rangeFault = fixture == "numeric-scale-range-tia";
                var overflow = fixture == "numeric-scale-overflow-tia";
                var invalid = fixture == "numeric-scale-invalid-tia";
                program = program with
                {
                    Variables = [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("minimum", PlcVariableType.DInt, PlcVariableRole.Input, rangeFault ? 30L : overflow ? 0L : 10L),
                        new("value", PlcVariableType.Real, PlcVariableRole.Input, overflow ? 2.0 : 0.5),
                        new("maximum", PlcVariableType.DInt, PlcVariableRole.Input, rangeFault ? 10L : overflow ? 2_000_000_000L : 30L),
                        new("result", PlcVariableType.DInt, PlcVariableRole.Memory, overflow ? 12L : 7L)],
                    Networks = [new("numeric-scale-net", rangeFault
                            ? "SCALE_X invalid MIN/MAX range"
                            : overflow ? "SCALE_X DINT overflow"
                            : invalid ? "SCALE_X invalid REAL value" : "REAL value scaled to DINT range",
                        new("numeric-scale-enable", LadderNodeKind.Contact, "enable"),
                        NumericOperation: new LadderNumericOperation("numeric-scale", LadderNumericOperationKind.Scale,
                            "minimum", "value", "result", "maximum")
                        { InputDataType = LadderNumericDataType.Real, OutputDataType = LadderNumericDataType.DInt })],
                };
            }
            LadderNode Enabled(string id) => new(id, LadderNodeKind.Contact, "enable");
            // Review programs must use the same non-empty executable logic
            // topology as authored user programs. A one-contact wire preserves
            // the always-visible rung conductor without relying on the legacy
            // empty-series-as-TRUE behavior (VC003 rejects that in new files).
            // Parameterized FB logic must qualify a global tag explicitly;
            // this fixture's child-block contacts consume the caller's shared
            // global enable rather than an unbound block-local operand.
            LadderNode Wire(string id) => new(id, LadderNodeKind.Series, Children: [new LadderNode(id + "-enable", LadderNodeKind.Contact, "global:enable")]);
            if (fixture == "text")
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("empty_text", PlcVariableType.String, PlcVariableRole.Memory, ""),
                        new("formatted_text", PlcVariableType.String, PlcVariableRole.Memory, "old"),
                        new("invalid_template", PlcVariableType.String, PlcVariableRole.Memory, "{broken") }).ToArray(),
                    Networks = [
                        new("text-empty-net", "MOV stores an empty STRING, not an unavailable value", Enabled("text-empty-enable"),
                            NumericOperation: new("text-empty", LadderNumericOperationKind.Move, "\"\"", "0", "empty_text")),
                        new("text-format-net", "FORMAT_TEXT observes successful text before a later failure clears it", Enabled("text-format-enable"),
                            NumericOperation: new("text-format", LadderNumericOperationKind.FormatText, "value", "\"{0:F1}\"", "formatted_text")),
                        new("text-fault-net", "Invalid format reports failure and cleared destination", Enabled("text-fault-enable"),
                            NumericOperation: new("text-fault", LadderNumericOperationKind.FormatText, "value", "invalid_template", "formatted_text"))]
                };
            if (fixture == "flow")
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("branch_a", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("branch_b", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("flow_edge_memory", PlcVariableType.Bool, PlcVariableRole.Memory, false),
                        new("label_output", PlcVariableType.Bool, PlcVariableRole.Output, false) }).ToArray(),
                    Networks = [
                    new("flow-branch-net", "Independent NO branches: mixed and all-false power flow", new("flow-series", LadderNodeKind.Series, Children: [
                        Enabled("flow-no"), new("flow-parallel", LadderNodeKind.Parallel, Children: [
                            new("flow-branch-a-series", LadderNodeKind.Series, Children: [new("flow-branch-a", LadderNodeKind.Contact, "branch_a")]),
                            new("flow-branch-b-series", LadderNodeKind.Series, Children: [new("flow-branch-b", LadderNodeKind.Contact, "branch_b")])])]),
                        Coil: new("flow-set", "lamp", LadderCoilMode.Set)),
                    new("flow-edge-net", "Rising edge RESET fires on the second scan",
                        new LadderNode("flow-edge", LadderNodeKind.Contact, "enable", EdgeMode: LadderEdgeMode.Rising)
                            { EdgeMemoryOperand = "flow_edge_memory" },
                        Coil: new("flow-reset", "lamp", LadderCoilMode.Reset)),
                    new("flow-jump-net", "JMP is taken; next network is not evaluated", Enabled("flow-jump-enable"), Jump: new("flow-jump", "finish")),
                    new("flow-skipped-net", "Skipped parallel network: power flow is unknown", new("flow-skipped-series", LadderNodeKind.Series, Children: [
                        Enabled("flow-skipped-enable"), new("flow-skipped-parallel", LadderNodeKind.Parallel, Children: [
                            new("flow-skipped-a-series", LadderNodeKind.Series, Children: [new("flow-skipped-a", LadderNodeKind.Contact, "branch_a")]),
                            new("flow-skipped-b-series", LadderNodeKind.Series, Children: [new("flow-skipped-b", LadderNodeKind.Contact, "branch_b")])])]),
                        Coil: new("flow-skipped", "lamp")),
                    new("flow-label-net", "Block-local destination LABEL", Enabled("flow-label-wire"),
                        Coil: new("flow-label-coil", "label_output"), LabelInstruction: new("flow-label", "finish")),
                    new("flow-return-net", "RETURN ends this task after the label", Enabled("flow-return-wire"), Return: new("flow-return"))]
                };
            if (unknownTimerKind is { } skippedTimerKind)
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("skip_timer", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("timer_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false) }).ToArray(),
                    Networks = [
                        new("timer-jump-net", "JMP skips the " + unknownTimerMnemonic + " in the current scan", new("timer-jump-enable", LadderNodeKind.Contact, "skip_timer"),
                            Jump: new("timer-jump", "done")),
                        new("timer-unknown-net", unknownTimerMnemonic + " not executed this scan; values come from the earlier cycle", new("timer-unknown-enable", LadderNodeKind.Contact, "enable"),
                            Timer: new("timer-unknown", "timer", TimeSpan.FromMilliseconds(100), skippedTimerKind,
                                new LadderInstanceDataBlock(1, "IEC_Timer_0_DB"),
                                ResetOperand: skippedTimerKind == LadderTimerKind.RetentiveOnDelay ? "reset" : "FALSE")),
                        new("timer-label-net", "Block-local destination LABEL", new("timer-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("timer-label-coil", "timer_label_output"), LabelInstruction: new("timer-label", "done"))]
                };
            if (unknownCounterKind is { } skippedCounterKind)
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("skip_counter", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("counter_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false) }).ToArray(),
                    Networks = [
                        new("counter-jump-net", "JMP skips the counter in the current scan", new("counter-jump-enable", LadderNodeKind.Contact, "skip_counter"),
                            Jump: new("counter-jump", "done")),
                        new("counter-unknown-net", (skippedCounterKind == LadderCounterKind.CountDown ? "CTD" : "CTU") + " not executed this scan; values come from the earlier cycle",
                            new("counter-unknown-enable", LadderNodeKind.Contact, "enable"),
                            Counter: new("counter-unknown", "count", 3, skippedCounterKind,
                                LoadInput: skippedCounterKind == LadderCounterKind.CountDown ? "load" : "FALSE",
                                ResetInput: skippedCounterKind == LadderCounterKind.CountUp ? "reset" : "FALSE")),
                        new("counter-label-net", "Block-local destination LABEL", new("counter-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("counter-label-coil", "counter_label_output"), LabelInstruction: new("counter-label", "done"))]
                };
            if (fixture == "branch-start")
            {
                LadderNode ContactPath(string id, params LadderNode[] contacts) =>
                    new(id, LadderNodeKind.Series, Children: contacts);
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("start_permissive", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("cell_permissive", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("layer_complete", PlcVariableType.Bool, PlcVariableRole.Input, false) }).ToArray(),
                    Networks = [new("branch-start-net", "Seal-in branch starts at the left power rail",
                        new("branch-start-parallel", LadderNodeKind.Parallel, Children: [
                            ContactPath("branch-start-top", new("start-permissive", LadderNodeKind.Contact, "start_permissive"),
                                new("top-cell-permissive", LadderNodeKind.Contact, "cell_permissive"),
                                new("top-layer-incomplete", LadderNodeKind.Contact, "layer_complete", NormallyClosed: true)),
                            ContactPath("branch-start-seal", new("seal-in-contact", LadderNodeKind.Contact, "lamp"),
                                new("seal-cell-permissive", LadderNodeKind.Contact, "cell_permissive"),
                                new("seal-layer-incomplete", LadderNodeKind.Contact, "layer_complete", NormallyClosed: true))]),
                        Coil: new("branch-start-coil", "lamp"))]
                };
            }
            if (fixture == "branch-nested")
            {
                LadderNode Path(string id, params LadderNode[] children) =>
                    new(id, LadderNodeKind.Series, Children: children);
                program = program with
                {
                    Variables = [
                        new("lead", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("upper", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("nested_a", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("nested_b", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("lower", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("tail", PlcVariableType.Bool, PlcVariableRole.Input, true),
                        new("nested_out", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    ],
                    Networks = [new("branch-nested-net", "Non-root branch with nested parallel path",
                        new("branch-nested-root", LadderNodeKind.Series, Children: [
                            new("branch-nested-lead", LadderNodeKind.Contact, "lead"),
                            new("branch-outer-parallel", LadderNodeKind.Parallel, Children: [
                                Path("branch-outer-upper", new LadderNode("branch-upper", LadderNodeKind.Contact, "upper"),
                                    new("branch-inner-parallel", LadderNodeKind.Parallel, Children: [
                                        Path("branch-inner-a-path", new LadderNode("branch-inner-a", LadderNodeKind.Contact, "nested_a")),
                                        Path("branch-inner-b-path", new LadderNode("branch-inner-b", LadderNodeKind.Contact, "nested_b")),
                                    ])),
                                Path("branch-outer-lower", new LadderNode("branch-lower", LadderNodeKind.Contact, "lower")),
                            ]),
                            new("branch-nested-tail", LadderNodeKind.Contact, "tail"),
                        ]), Coil: new("branch-nested-coil", "nested_out"))],
                };
            }
            if (fixture == "empty-box-tia")
            {
                LadderNode ContactPath(string id, params LadderNode[] contacts) =>
                    new(id, LadderNodeKind.Series, Children: contacts);
                program = program with
                {
                    Variables = [new("start_command", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("seal_in", PlcVariableType.Bool, PlcVariableRole.Memory, false),
                        new("stop_command", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("simulated_photoeye", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("lamp", PlcVariableType.Bool, PlcVariableRole.Output, false)],
                    Networks = [new("empty-box-net", "", new("empty-box-branches", LadderNodeKind.Parallel, Children: [
                        ContactPath("empty-box-top", new("empty-box-start", LadderNodeKind.Contact, "start_command"),
                            new("empty-box-top-stop", LadderNodeKind.Contact, "stop_command", NormallyClosed: true),
                            new("empty-box-top-photoeye", LadderNodeKind.Contact, "simulated_photoeye", NormallyClosed: true)),
                        ContactPath("empty-box-seal", new("empty-box-seal-in", LadderNodeKind.Contact, "seal_in"),
                            new("empty-box-seal-stop", LadderNodeKind.Contact, "stop_command", NormallyClosed: true),
                            new("empty-box-seal-photoeye", LadderNodeKind.Contact, "simulated_photoeye", NormallyClosed: true))]),
                        Coil: new("empty-box-coil", "lamp"))]
                };
            }
            if (fixture is "parameter-call" or "call-unknown")
            {
                LadderNetwork Call(string suffix, double input) => new("call-net-" + suffix, "Explicit instance " + suffix + " consumes " + input,
                    Enabled("call-enable-" + suffix), Call: new("call-" + suffix, "review-fb", LadderSemanticMode.Parameterized,
                        [new("input", LadderActualKind.Literal, Literal: input, LiteralType: PlcVariableType.Real),
                         new("output", LadderActualKind.Symbol, "global:result-" + suffix),
                         new("shared", LadderActualKind.Symbol, "global:shared")], "instance-" + suffix));
                var mainNetworks = unknownCall
                    ? new LadderNetwork[] {
                        new("call-gate-net", "Current scan skips the FB call",
                            new("call-gate-enable", LadderNodeKind.Contact, "skip_call"),
                            Jump: new("call-gate-jump", "done")),
                        Call("a", 3.0),
                        new("call-label-net", "Block-local destination LABEL",
                            new("call-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("call-label-coil", "call_label_output"),
                            LabelInstruction: new("call-label", "done")),
                    }
                    : [Call("a", 3.0), Call("b", 7.0)];
                var callVariables = new List<PlcVariable> {
                    new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "enable"),
                    new("result_a", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "result-a"),
                    new("shared_total", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "shared") };
                if (unknownCall)
                {
                    callVariables.Add(new("skip_call", PlcVariableType.Bool, PlcVariableRole.Input, false, Id: "skip-call"));
                    callVariables.Add(new("call_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false, Id: "call-label-output"));
                }
                else callVariables.Add(new("result_b", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Id: "result-b"));
                program = program with { SchemaVersion = 2,
                    Variables = callVariables.ToArray(),
                    Networks = [], EntryBlock = "main", Instances = unknownCall
                        ? [new("instance-a", "Station A", "review-fb")]
                        : [new("instance-a", "Station A", "review-fb"), new("instance-b", "Station B", "review-fb")],
                    Blocks = [new("main", "Main", mainNetworks),
                        new("review-fb", "Station counter", [
                            new("fb-accumulate-net", "Instance-local retained sum", Wire("fb-accumulate-wire"), NumericOperation: new("fb-accumulate", LadderNumericOperationKind.Add, "local:sum", "local:input", "local:sum")),
                            new("fb-publish-net", "Output copies out only on normal return", Wire("fb-publish-wire"), NumericOperation: new("fb-publish", LadderNumericOperationKind.Move, "local:sum", "0", "local:output")),
                            new("fb-shared-net", "InOut aliases the caller shared value", Wire("fb-shared-wire"), NumericOperation: new("fb-shared", LadderNumericOperationKind.Add, "local:shared", "local:input", "local:shared"))],
                            LadderBlockType.FunctionBlock,
                            [new("Input", PlcVariableType.Real, LadderInterfaceSection.Input, 0.0, Id: "input"),
                             new("Output", PlcVariableType.Real, LadderInterfaceSection.Output, 0.0, Id: "output"),
                             new("Shared", PlcVariableType.Real, LadderInterfaceSection.InOut, 0.0, Id: "shared"),
                             new("Sum", PlcVariableType.Real, LadderInterfaceSection.Static, 0.0, Id: "sum")], LadderSemanticMode.Parameterized)] };
            }
            if (fixture == "timer-unassigned-tia")
                program = program with { Networks = [program.Networks[0]] };
            if (fixture is "timer-tof" or "timer-tp" or "timer-tonr")
                program = program with { Networks = [program.Networks[0]] };
            if (numericSkipped)
            {
                var reviewedNetwork = program.Networks.Single();
                skippedNumericOperation = reviewedNetwork.NumericOperation
                    ?? throw new InvalidOperationException("Skipped numeric fixture must contain one numeric operation.");
                skippedNumericEnableId = reviewedNetwork.Logic.Id;
                program = program with
                {
                    Variables = program.Variables.Concat(new PlcVariable[] {
                        new("skip_numeric", PlcVariableType.Bool, PlcVariableRole.Input, false),
                        new("numeric_label_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
                    }).ToArray(),
                    Networks = [
                        new("numeric-skip-gate-net", "Current scan skips the reviewed numeric instruction",
                            new("numeric-skip-gate-enable", LadderNodeKind.Contact, "skip_numeric"),
                            Jump: new("numeric-skip-jump", "done")),
                        reviewedNetwork with { Label = reviewedNetwork.Label + " — not executed in the current scan" },
                        new("numeric-skip-label-net", "Block-local destination LABEL",
                            new("numeric-skip-label-enable", LadderNodeKind.Contact, "enable"),
                            Coil: new("numeric-skip-label-coil", "numeric_label_output"),
                            LabelInstruction: new("numeric-skip-label", "done")),
                    ],
                };
            }
            (double Left, double Right) compareOperands = (reviewCompareOperator, compareResult) switch
            {
                (LadderCompareOperator.Equal, true) => (0.0, 0.0),
                (LadderCompareOperator.Equal, false) => (1.0, 0.0),
                (LadderCompareOperator.NotEqual, true) => (1.0, 0.0),
                (LadderCompareOperator.NotEqual, false) => (0.0, 0.0),
                (LadderCompareOperator.GreaterThan, true) => (1.0, 0.0),
                (LadderCompareOperator.GreaterThan, false) => (0.0, 0.0),
                (LadderCompareOperator.GreaterOrEqual, true) => (0.0, 0.0),
                (LadderCompareOperator.GreaterOrEqual, false) => (-1.0, 0.0),
                (LadderCompareOperator.LessThan, true) => (-1.0, 0.0),
                (LadderCompareOperator.LessThan, false) => (0.0, 0.0),
                (LadderCompareOperator.LessOrEqual, true) => (0.0, 0.0),
                (LadderCompareOperator.LessOrEqual, false) => (1.0, 0.0),
                _ => throw new InvalidOperationException("Unsupported comparison review case."),
            };
            var comparisonNumericInputs = new Dictionary<string, double> {
                ["pc_heartbeat"] = compareOperands.Left,
                ["last_heartbeat"] = compareOperands.Right,
            };
            var compiled = LadderCompiler.Compile(program);
            if (!compiled.IsValid) throw new InvalidOperationException("Monitoring render fixture must compile: " + string.Join("; ", compiled.Issues));
            var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
            VirtualControllerSnapshot? earlierCycleSnapshot = null;
            if (fixture == "flow")
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> { ["enable"] = false, ["branch_a"] = false, ["branch_b"] = false });
            if (unknownTimerKind is not null)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> { ["enable"] = true, ["skip_timer"] = false });
            if (unknownCounterKind is { } earlierCounterKind)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = earlierCounterKind == LadderCounterKind.CountUp,
                    ["load"] = earlierCounterKind == LadderCounterKind.CountDown,
                    ["reset"] = false,
                    ["skip_counter"] = false });
            if (unknownComparison)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_comparison"] = false }, comparisonNumericInputs);
            if (unknownContact)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_contacts"] = false,
                    ["no_signal"] = true,
                    ["nc_signal"] = false });
            if (unknownEdge)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_edges"] = false,
                    ["rising_signal"] = true,
                    ["falling_signal"] = false });
            if (unknownCoil)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_coils"] = false,
                    ["coil_input"] = true });
            if (unknownProgramControl)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_program_control"] = false,
                    ["control_false"] = false });
            if (unknownCall)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_call"] = false });
            if (numericSkipped)
                earlierCycleSnapshot = runtime.Scan(new Dictionary<string, bool> {
                    ["enable"] = true,
                    ["skip_numeric"] = false });
            var flowState = Option("--review-flow-state", "mixed");
            if (fixture == "flow" && flowState is not ("mixed" or "false"))
                throw new ArgumentException("Flow review state must be 'mixed' or 'false'; use review rung 3 for JMP-skipped unknown flow.");
            var counterDownState = Option("--review-counter-down-state", "load");
            if (fixture == "counter-down-tia" && counterDownState is not ("load" or "count" or "held" or "zero"))
                throw new ArgumentException("CTD review state must be 'load', 'count', 'held', or 'zero'.");
            var counterUpState = Option("--review-counter-up-state", "requested");
            if (fixture == "counter-reset-tia" && counterUpState is not ("requested" or "reset" or "count" or "held" or "done"))
                throw new ArgumentException("CTU review state must be 'reset', 'count', 'held', or 'done'.");
            var timerState = Option("--review-timer-state", "requested");
            if (fixture == "timer-tof" && timerState is not ("requested" or "input" or "falling" or "expired"))
                throw new ArgumentException("TOF review state must be 'input', 'falling', or 'expired'.");
            if (fixture == "timer-tp" && timerState is not ("requested" or "pulse" or "held" or "expired"))
                throw new ArgumentException("TP review state must be 'pulse', 'held', or 'expired'.");
            if (fixture == "timer-tonr" && timerState is not ("requested" or "accumulating" or "paused" or "reset" or "released" or "done"))
                throw new ArgumentException("TONR review state must be 'accumulating', 'paused', 'reset', 'released', or 'done'.");
            if (fixture == "timer-flow" && timerState is not ("requested" or "accumulating" or "held" or "done" or "released"))
                throw new ArgumentException("TON review state must be 'accumulating', 'held', 'done', or 'released'.");
            var timerResetState = Option("--review-timer-reset-state", "unpowered");
            if (fixture == "timer-reset-state-tia" && timerResetState is not ("unpowered" or "powered"))
                throw new ArgumentException("RT review state must be 'unpowered' or 'powered'.");
            var scanInputs = new Dictionary<string, bool> { ["enable"] = enable, ["load"] = fixture == "counter-down-tia", ["reset"] = fixture == "counter-reset-tia" };
            if (fixture == "counter-reset-tia" && counterUpState != "requested")
            {
                scanInputs["enable"] = true;
                scanInputs["reset"] = counterUpState == "reset";
            }
            if (fixture == "timer-reset-state-tia")
            {
                scanInputs["enable"] = true;
                scanInputs["reset"] = false;
            }
            if (fixture is "timer-flow" or "timer-tof" or "timer-tp" or "timer-tonr" && timerState != "requested")
            {
                scanInputs["enable"] = true;
                scanInputs["reset"] = false;
            }
            if (fixture == "program-control-tia")
            {
                scanInputs["rlo_false"] = false;
                scanInputs["rlo_true"] = true;
            }
            if (fixture == "contact-tia")
            {
                scanInputs["no_false"] = false;
                scanInputs["no_true"] = true;
                scanInputs["nc_false"] = false;
                scanInputs["nc_true"] = true;
            }
            var edgeState = Option("--review-edge-state", "baseline");
            if (fixture == "edge-tia")
            {
                if (edgeState is not ("baseline" or "pulse" or "held"))
                    throw new ArgumentException("Edge review state must be 'baseline', 'pulse', or 'held'.");
                scanInputs["rising_signal"] = false;
                scanInputs["falling_signal"] = true;
            }
            if (fixture == "branch-start")
            {
                scanInputs["start_permissive"] = true;
                scanInputs["cell_permissive"] = true;
                scanInputs["layer_complete"] = false;
            }
            if (fixture == "branch-nested")
            {
                scanInputs.Clear();
                scanInputs["lead"] = true;
                scanInputs["upper"] = true;
                scanInputs["nested_a"] = false;
                scanInputs["nested_b"] = true;
                scanInputs["lower"] = false;
                scanInputs["tail"] = true;
            }
            if (fixture == "flow")
            {
                scanInputs["branch_a"] = flowState == "mixed";
                scanInputs["branch_b"] = false;
            }
            if (unknownTimerKind is not null) scanInputs["skip_timer"] = true;
            if (unknownCounterKind is not null)
            {
                scanInputs["skip_counter"] = true;
                scanInputs["load"] = false;
                scanInputs["reset"] = false;
            }
            if (unknownComparison) scanInputs["skip_comparison"] = true;
            if (unknownContact)
            {
                scanInputs["enable"] = true;
                scanInputs["skip_contacts"] = true;
                scanInputs["no_signal"] = true;
                scanInputs["nc_signal"] = false;
            }
            if (unknownEdge)
            {
                scanInputs["enable"] = true;
                scanInputs["skip_edges"] = true;
                scanInputs["rising_signal"] = true;
                scanInputs["falling_signal"] = false;
            }
            if (unknownCoil)
            {
                scanInputs["enable"] = true;
                scanInputs["skip_coils"] = true;
                scanInputs["coil_input"] = true;
            }
            if (unknownProgramControl)
            {
                scanInputs["enable"] = true;
                scanInputs["skip_program_control"] = true;
                scanInputs["control_false"] = false;
            }
            if (unknownCall)
            {
                scanInputs["enable"] = true;
                scanInputs["skip_call"] = true;
            }
            if (numericSkipped)
            {
                scanInputs["enable"] = true;
                scanInputs["skip_numeric"] = true;
            }
            Dictionary<string, double>? numericInputs = fixture switch
            {
                "timer-symbolic-time" => new Dictionary<string, double> { ["time_preset"] = 60 },
                "compare-tia" or "compare-unknown" => comparisonNumericInputs,
                "numeric-norm-invalid-tia" or "numeric-scale-invalid-tia" =>
                    new Dictionary<string, double> { ["value"] = double.NaN },
                _ => null,
            };
            var snapshot = runtime.Scan(scanInputs, numericInputs);
            if (fixture == "timer-reset-state-tia" && timerResetState == "powered")
            {
                scanInputs["reset"] = true;
                snapshot = runtime.Scan(scanInputs, numericInputs);
            }
            if (fixture == "counter-down-tia" && counterDownState != "load")
            {
                // LD dominates CD in the load scan. Drop both inputs to re-arm
                // the positive-edge detector, then apply explicit CD pulses.
                scanInputs["load"] = false;
                scanInputs["enable"] = false;
                snapshot = runtime.Scan(scanInputs, numericInputs);
                scanInputs["enable"] = true;
                snapshot = runtime.Scan(scanInputs, numericInputs);
                if (counterDownState == "held")
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                if (counterDownState == "zero")
                {
                    for (var pulse = 0; pulse < 2; pulse++)
                    {
                        scanInputs["enable"] = false;
                        runtime.Scan(scanInputs, numericInputs);
                        scanInputs["enable"] = true;
                        snapshot = runtime.Scan(scanInputs, numericInputs);
                    }
                }
            }
            if (fixture == "counter-reset-tia" && counterUpState is "held" or "done")
            {
                if (counterUpState == "held")
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                else
                {
                    for (var pulse = 0; pulse < 2; pulse++)
                    {
                        scanInputs["enable"] = false;
                        runtime.Scan(scanInputs, numericInputs);
                        scanInputs["enable"] = true;
                        snapshot = runtime.Scan(scanInputs, numericInputs);
                    }
                }
            }
            if (fixture == "timer-tof" && timerState is "falling" or "expired")
            {
                scanInputs["enable"] = false;
                snapshot = runtime.Scan(scanInputs, numericInputs);
                for (var scan = 1; timerState == "expired" && scan < 10; scan++)
                    snapshot = runtime.Scan(scanInputs, numericInputs);
            }
            if (fixture == "timer-tp" && timerState is "held" or "expired")
            {
                var targetScans = timerState == "held" ? 2 : 10;
                for (var scan = 1; scan < targetScans; scan++)
                    snapshot = runtime.Scan(scanInputs, numericInputs);
            }
            if (fixture == "timer-tonr")
            {
                if (timerState == "paused")
                {
                    scanInputs["enable"] = false;
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                }
                else if (timerState == "reset")
                {
                    scanInputs["reset"] = true;
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                }
                else if (timerState == "released")
                {
                    scanInputs["reset"] = true;
                    runtime.Scan(scanInputs, numericInputs);
                    scanInputs["reset"] = false;
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                }
                else if (timerState == "done")
                {
                    for (var scan = 1; scan < 10; scan++)
                        snapshot = runtime.Scan(scanInputs, numericInputs);
                }
            }
            if (fixture == "timer-flow")
            {
                if (timerState == "held")
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                else if (timerState is "done" or "released")
                {
                    for (var scan = 1; scan < 10; scan++)
                        snapshot = runtime.Scan(scanInputs, numericInputs);
                    if (timerState == "released")
                    {
                        scanInputs["enable"] = false;
                        snapshot = runtime.Scan(scanInputs, numericInputs);
                    }
                }
            }
            if (fixture == "edge-tia" && edgeState is "pulse" or "held")
            {
                scanInputs["rising_signal"] = true;
                scanInputs["falling_signal"] = false;
                snapshot = runtime.Scan(scanInputs, numericInputs);
                if (edgeState == "held")
                    snapshot = runtime.Scan(scanInputs, numericInputs);
            }
            if (fixture == "timer-flow" && timerState == "requested")
            {
                var qMode = Option("--review-timer-q", "false");
                if (qMode is not ("false" or "true")) throw new ArgumentException("Timer flow state must be 'false' or 'true'.");
                for (var scan = 1; qMode == "true" && scan < 10; scan++)
                    snapshot = runtime.Scan(scanInputs, numericInputs);
                Require(snapshot.Timers["timer"].Done == (qMode == "true"), "TON Q matches the requested outgoing-flow state");
            }
            if (fixture == "counter-flow")
            {
                var qMode = Option("--review-counter-q", "false");
                if (qMode is not ("false" or "true")) throw new ArgumentException("Counter flow state must be 'false' or 'true'.");
                for (var pulse = 0; qMode == "true" && pulse < 2; pulse++)
                {
                    scanInputs["enable"] = false;
                    runtime.Scan(scanInputs);
                    scanInputs["enable"] = true;
                    snapshot = runtime.Scan(scanInputs);
                }
                Require(snapshot.Counters["count"].Done == (qMode == "true"), "CTU Q matches the requested outgoing-flow state");
            }
            var capture = snapshot.Observations ?? throw new InvalidOperationException("Fixture did not capture actual execution.");
            var numericEvidenceSnapshot = numericSkipped
                ? earlierCycleSnapshot ?? throw new InvalidOperationException("Skipped numeric fixture lost its earlier execution snapshot.")
                : snapshot;
            var numericEvidenceOperation = skippedNumericOperation
                ?? program.Networks.Select(network => network.NumericOperation).FirstOrDefault(operation => operation is not null);
            void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("Fixture execution: " + message); }
            Require(snapshot.Completion == ControllerCompletion.Completed, "scan did not complete");
            if (fixture == "counter-reset-tia")
            {
                var observed = capture.GlobalLast["ctu"];
                var counter = snapshot.Counters["count"];
                if (counterUpState == "requested")
                {
                    Require(counter.Accumulated == 0, "CTU R overrides CU while reset is asserted");
                    Require(observed.Operands.Any(operand => operand.Value is true), "CTU records the live R operand");
                }
                else
                {
                    var expected = counterUpState switch
                    {
                        "reset" => (BeforeCv: 0L, BeforeInput: false, BeforeQ: false, Cv: 0L, Q: false,
                            PreviousInput: false, Reset: true, Disposition: InstructionWriteDisposition.StateUpdatedChanged),
                        "count" => (BeforeCv: 0L, BeforeInput: false, BeforeQ: false, Cv: 1L, Q: false,
                            PreviousInput: false, Reset: false, Disposition: InstructionWriteDisposition.StateUpdatedChanged),
                        "held" => (BeforeCv: 1L, BeforeInput: true, BeforeQ: false, Cv: 1L, Q: false,
                            PreviousInput: true, Reset: false, Disposition: InstructionWriteDisposition.StateUpdatedSameValue),
                        "done" => (BeforeCv: 2L, BeforeInput: false, BeforeQ: false, Cv: 3L, Q: true,
                            PreviousInput: false, Reset: false, Disposition: InstructionWriteDisposition.StateUpdatedChanged),
                        _ => throw new InvalidOperationException("Unsupported CTU review state."),
                    };
                    var before = observed.DestinationBefore as LadderCounterState
                        ?? throw new InvalidOperationException("CTU observation lacks the pre-execution state.");
                    var after = observed.StoredAfter as LadderCounterState
                        ?? throw new InvalidOperationException("CTU observation lacks the post-execution state.");
                    Require(counter.Accumulated == expected.Cv && counter.CountInput && counter.Done == expected.Q,
                        "CTU CV, CU, and Q match the requested reset, edge, held-high, or done state");
                    Require(observed.Evaluated && observed.NetworkEnable == true && observed.LocalResult
                        && observed.DestinationIdentity == "count"
                        && before == new LadderCounterState(3, expected.BeforeCv, expected.BeforeInput, expected.BeforeQ)
                        && after == counter
                        && Equals(observed.ConfiguredPreset, 3L)
                        && observed.PreviousKnown && observed.PreviousInput == expected.PreviousInput
                        && observed.CurrentInput == true
                        && observed.WriteDisposition == expected.Disposition
                        && observed.Diagnostics.Count == 0,
                        "CTU observation records exact CU, edge history, before/after state, PV, disposition, and diagnostics");
                    Require(observed.Operands.Any(operand => operand.Identity == "reset"
                            && operand.DeclaredType == PlcVariableType.Bool
                            && operand.Reason == ObservationUnavailableReason.None
                            && Equals(operand.Value, expected.Reset)),
                        "CTU observation records the declared BOOL R input");
                    GD.Print($"TIA_CTU_STATE_PASS state={counterUpState} cu=1 r={(expected.Reset ? 1 : 0)} beforeCv={before.Accumulated} cv={counter.Accumulated} q={(counter.Done ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics=0 scan={snapshot.ScanNumber}");
                }
            }
            if (fixture == "counter-down-tia")
            {
                var observed = capture.GlobalLast["ctd"];
                var counter = snapshot.Counters["count"];
                var expectedCd = counterDownState == "load" ? enable : true;
                var expected = counterDownState switch
                {
                    "load" => (BeforeCv: 0L, BeforeInput: false, BeforeQ: false, Cv: 3L, Q: false,
                        PreviousInput: false, Load: true, Disposition: InstructionWriteDisposition.StateUpdatedChanged),
                    "count" => (BeforeCv: 3L, BeforeInput: false, BeforeQ: false, Cv: 2L, Q: false,
                        PreviousInput: false, Load: false, Disposition: InstructionWriteDisposition.StateUpdatedChanged),
                    "held" => (BeforeCv: 2L, BeforeInput: true, BeforeQ: false, Cv: 2L, Q: false,
                        PreviousInput: true, Load: false, Disposition: InstructionWriteDisposition.StateUpdatedSameValue),
                    "zero" => (BeforeCv: 1L, BeforeInput: false, BeforeQ: false, Cv: 0L, Q: true,
                        PreviousInput: false, Load: false, Disposition: InstructionWriteDisposition.StateUpdatedChanged),
                    _ => throw new InvalidOperationException("Unsupported CTD review state."),
                };
                var before = observed.DestinationBefore as LadderCounterState
                    ?? throw new InvalidOperationException("CTD observation lacks the pre-execution state.");
                var after = observed.StoredAfter as LadderCounterState
                    ?? throw new InvalidOperationException("CTD observation lacks the post-execution state.");
                Require(counter.Accumulated == expected.Cv && counter.CountInput == expectedCd && counter.Done == expected.Q,
                    "CTD CV, CD, and Q match the requested load, edge, held-high, or zero state");
                Require(observed.Evaluated && observed.NetworkEnable == expectedCd && observed.LocalResult == expectedCd
                    && observed.DestinationIdentity == "count"
                    && before == new LadderCounterState(3, expected.BeforeCv, expected.BeforeInput, expected.BeforeQ)
                    && after == counter
                    && Equals(observed.ConfiguredPreset, 3L)
                    && observed.PreviousKnown && observed.PreviousInput == expected.PreviousInput
                    && observed.CurrentInput == expectedCd
                    && observed.WriteDisposition == expected.Disposition
                    && observed.Diagnostics.Count == 0,
                    "CTD observation records exact CD, edge history, before/after state, PV, disposition, and diagnostics");
                Require(observed.Operands.Any(operand => operand.Identity == "load"
                        && operand.DeclaredType == PlcVariableType.Bool
                        && operand.Reason == ObservationUnavailableReason.None
                        && Equals(operand.Value, expected.Load)),
                    "CTD observation records the declared BOOL LD input");
                GD.Print($"TIA_CTD_STATE_PASS state={counterDownState} cd={(expectedCd ? 1 : 0)} ld={(expected.Load ? 1 : 0)} beforeCv={before.Accumulated} cv={counter.Accumulated} q={(counter.Done ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics=0 scan={snapshot.ScanNumber}");
            }
            if (fixture == "timer")
            {
                var reset = capture.GlobalLast.GetValueOrDefault("reset");
                Require(reset is { Evaluated: true } && reset.LocalResult == enable,
                    "RT terminal flow follows the requested rung enable state");
            }
            if (fixture == "timer-reset-state-tia")
            {
                var powered = timerResetState == "powered";
                var observed = capture.GlobalLast["reset"];
                var before = observed.DestinationBefore as LadderTimerState
                    ?? throw new InvalidOperationException("RT observation lacks the pre-reset timer state.");
                var after = observed.StoredAfter as LadderTimerState
                    ?? throw new InvalidOperationException("RT observation lacks the post-reset timer state.");
                var expectedBeforeMs = powered ? 20.0 : 10.0;
                var expectedAfterMs = powered ? 0.0 : 10.0;
                Require(observed.LocalResult == powered && observed.NetworkEnable == powered,
                    "RT observation follows its independent reset input");
                Require(before.Accumulated.TotalMilliseconds == expectedBeforeMs
                    && after.Accumulated.TotalMilliseconds == expectedAfterMs
                    && snapshot.Timers["timer"].Accumulated.TotalMilliseconds == expectedAfterMs,
                    "powered RT clears the previously accumulating IEC timer while unpowered RT preserves it");
                Require(powered
                        ? observed.WriteDisposition == InstructionWriteDisposition.StateUpdatedChanged
                        : observed.WriteDisposition == InstructionWriteDisposition.SkippedDisabled,
                    "RT observation records whether the reset state changed or was skipped");
                Require(!powered || (!after.Done && !after.Timing && !after.Input),
                    "powered RT clears Q, timing, and stored input state");
                GD.Print($"TIA_RT_STATE_PASS state={timerResetState} beforeEtMs={before.Accumulated.TotalMilliseconds:0} afterEtMs={after.Accumulated.TotalMilliseconds:0} q={(after.Done ? 1 : 0)} scan={snapshot.ScanNumber}");
            }
            if (fixture == "timer-symbolic-time")
                Require(snapshot.Timers["timer"].Preset == TimeSpan.FromMilliseconds(60),
                    "symbolic TIME PT resolves from the accepted input image during this scan");
            if (fixture is "timer-flow" or "timer-tof" or "timer-tp" or "timer-tonr" && timerState != "requested")
            {
                var observed = capture.GlobalLast["timer"];
                var timer = snapshot.Timers["timer"];
                var expected = (fixture, timerState) switch
                {
                    ("timer-flow", "accumulating") => (BeforeEt: 0.0, Et: 10.0, Input: true, Q: false, Timing: true, Reset: false),
                    ("timer-flow", "held") => (BeforeEt: 10.0, Et: 20.0, Input: true, Q: false, Timing: true, Reset: false),
                    ("timer-flow", "done") => (BeforeEt: 90.0, Et: 100.0, Input: true, Q: true, Timing: false, Reset: false),
                    ("timer-flow", "released") => (BeforeEt: 100.0, Et: 0.0, Input: false, Q: false, Timing: false, Reset: false),
                    ("timer-tof", "input") => (BeforeEt: 0.0, Et: 0.0, Input: true, Q: true, Timing: false, Reset: false),
                    ("timer-tof", "falling") => (BeforeEt: 0.0, Et: 10.0, Input: false, Q: true, Timing: true, Reset: false),
                    ("timer-tof", "expired") => (BeforeEt: 90.0, Et: 100.0, Input: false, Q: false, Timing: false, Reset: false),
                    ("timer-tp", "pulse") => (BeforeEt: 0.0, Et: 10.0, Input: true, Q: true, Timing: true, Reset: false),
                    ("timer-tp", "held") => (BeforeEt: 10.0, Et: 20.0, Input: true, Q: true, Timing: true, Reset: false),
                    ("timer-tp", "expired") => (BeforeEt: 90.0, Et: 100.0, Input: true, Q: false, Timing: false, Reset: false),
                    ("timer-tonr", "accumulating") => (BeforeEt: 0.0, Et: 10.0, Input: true, Q: false, Timing: true, Reset: false),
                    ("timer-tonr", "paused") => (BeforeEt: 10.0, Et: 10.0, Input: false, Q: false, Timing: false, Reset: false),
                    ("timer-tonr", "reset") => (BeforeEt: 10.0, Et: 0.0, Input: true, Q: false, Timing: false, Reset: true),
                    ("timer-tonr", "released") => (BeforeEt: 0.0, Et: 10.0, Input: true, Q: false, Timing: true, Reset: false),
                    ("timer-tonr", "done") => (BeforeEt: 90.0, Et: 100.0, Input: true, Q: true, Timing: false, Reset: false),
                    _ => throw new InvalidOperationException("Unsupported timer review state."),
                };
                var before = observed.DestinationBefore as LadderTimerState
                    ?? throw new InvalidOperationException("Timer observation lacks the pre-execution state.");
                var after = observed.StoredAfter as LadderTimerState
                    ?? throw new InvalidOperationException("Timer observation lacks the post-execution state.");
                Require(timer.Accumulated.TotalMilliseconds == expected.Et
                    && timer.Input == expected.Input
                    && timer.Done == expected.Q
                    && timer.Timing == expected.Timing,
                    "timer ET, IN, Q, and timing state match the requested transition");
                Require(observed.Evaluated && observed.NetworkEnable == expected.Input
                    && observed.LocalResult == expected.Input
                    && observed.DestinationIdentity == "timer"
                    && before.Accumulated.TotalMilliseconds == expected.BeforeEt
                    && after == timer
                    && Equals(observed.ConfiguredPreset, TimeSpan.FromMilliseconds(100))
                    && observed.WriteDisposition == InstructionWriteDisposition.StateUpdatedChanged
                    && observed.Diagnostics.Count == 0,
                    "timer observation records exact IN, before/after state, PT, state disposition, and diagnostics");
                if (fixture == "timer-tonr")
                    Require(observed.Operands.Any(operand => operand.Identity == "reset"
                        && operand.DeclaredType == PlcVariableType.Bool
                        && operand.Reason == ObservationUnavailableReason.None
                        && Equals(operand.Value, expected.Reset)),
                        "TONR observation records the live R input");
                GD.Print($"TIA_TIMER_STATE_PASS fixture={fixture} state={timerState} in={(timer.Input ? 1 : 0)} r={(expected.Reset ? 1 : 0)} beforeEtMs={before.Accumulated.TotalMilliseconds:0} etMs={timer.Accumulated.TotalMilliseconds:0} q={(timer.Done ? 1 : 0)} timing={(timer.Timing ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics=0 scan={snapshot.ScanNumber}");
            }
            if (fixture == "math")
            {
                var divide = capture.GlobalLast["divide"];
                Require(divide.NetworkEnable == enable && divide.Diagnostics.Count > 0,
                    "DIV-by-zero fixture reaches EN and produces an instruction fault for ENO");
            }
            if (fixture == "compare-tia")
            {
                var comparison = capture.GlobalLast["compare-inline"];
                Require(comparison.LocalResult == compareResult,
                    "inline DInt comparison result matches the requested true/false display state");
                Require(comparison.NetworkEnable == true && comparison.Evaluated,
                    "inline comparison receives true incoming continuity and executes");
                GD.Print($"TIA_COMPARE_STATE_PASS op={reviewCompareOperator} expected={(compareResult ? 1 : 0)} left={compareOperands.Left:R} right={compareOperands.Right:R} result={(comparison.LocalResult ? 1 : 0)}");
            }
            if (fixture == "contact-tia")
            {
                foreach (var (id, identity, sampled, result) in new[]
                {
                    ("contact-no-false", "no_false", false, false),
                    ("contact-no-true", "no_true", true, true),
                    ("contact-nc-false", "nc_false", false, true),
                    ("contact-nc-true", "nc_true", true, false),
                })
                {
                    var observed = capture.GlobalLast[id];
                    var operand = observed.Operands.SingleOrDefault(item => item.Role == "A");
                    Require(observed.Evaluated && observed.NetworkEnable == true
                        && observed.LocalResult == result
                        && observed.DestinationIdentity.Length == 0
                        && observed.WriteDisposition == InstructionWriteDisposition.NotApplicable
                        && observed.Diagnostics.Count == 0
                        && operand is not null
                        && operand.Identity == identity
                        && operand.DeclaredType == PlcVariableType.Bool
                        && operand.Reason == ObservationUnavailableReason.None
                        && Equals(operand.Value, sampled),
                        $"{id} observes the exact BOOL operand and produces the expected NO/NC result from powered incoming continuity");
                }
                foreach (var (id, identity, result, disposition) in new[]
                {
                    ("contact-no-false-coil", "no_false_out", false, InstructionWriteDisposition.WrittenSameValue),
                    ("contact-no-true-coil", "no_true_out", true, InstructionWriteDisposition.WrittenChanged),
                    ("contact-nc-false-coil", "nc_false_out", true, InstructionWriteDisposition.WrittenChanged),
                    ("contact-nc-true-coil", "nc_true_out", false, InstructionWriteDisposition.WrittenSameValue),
                })
                {
                    var observed = capture.GlobalLast[id];
                    Require(observed.NetworkEnable == result && observed.LocalResult == result
                        && observed.DestinationIdentity == identity
                        && Equals(observed.DestinationBefore, false)
                        && Equals(observed.StoredAfter, result)
                        && observed.WriteDisposition == disposition
                        && observed.Diagnostics.Count == 0
                        && snapshot.Variables[identity] == result,
                        $"{id} receives the exact contact result and publishes the expected assignment-coil write disposition");
                }
                GD.Print("TIA_CONTACT_STATE_PASS noFalse=0/WrittenSameValue noTrue=1/WrittenChanged ncFalse=1/WrittenChanged ncTrue=0/WrittenSameValue operands=Bool diagnostics=0");
            }
            if (unknownContact)
            {
                var earlierCapture = earlierCycleSnapshot?.Observations?.GlobalLast;
                Require(earlierCapture is not null
                    && earlierCapture.TryGetValue("contact-no-unknown", out var earlierNo)
                    && earlierNo.NetworkEnable == true && earlierNo.LocalResult
                    && earlierNo.Operands.Single(operand => operand.Role == "A") is { Identity: "no_signal", DeclaredType: PlcVariableType.Bool, Value: true }
                    && earlierCapture.TryGetValue("contact-nc-unknown", out var earlierNc)
                    && earlierNc.NetworkEnable == true && earlierNc.LocalResult
                    && earlierNc.Operands.Single(operand => operand.Role == "A") is { Identity: "nc_signal", DeclaredType: PlcVariableType.Bool, Value: false },
                    "earlier scan evaluated exact BOOL operands through both NO and NC contacts before the gate jump became true");
                Require(!capture.GlobalLast.ContainsKey("contact-no-unknown")
                    && !capture.GlobalLast.ContainsKey("contact-nc-unknown")
                    && !capture.GlobalLast.ContainsKey("contact-no-unknown-coil")
                    && !capture.GlobalLast.ContainsKey("contact-nc-unknown-coil")
                    && capture.GlobalLast["contact-gate-jump"].ControlOutcome == "Taken"
                    && capture.GlobalLast.ContainsKey("contact-label-coil")
                    && snapshot.Variables["no_unknown_out"]
                    && snapshot.Variables["nc_unknown_out"],
                    "current scan skipped both contact networks, visited the LABEL destination, and preserved earlier coil writes");
            }
            if (unknownEdge)
            {
                var earlierCapture = earlierCycleSnapshot?.Observations?.GlobalLast;
                Require(earlierCapture is not null
                    && earlierCapture.TryGetValue("edge-rising-unknown", out var earlierRising)
                    && earlierRising.NetworkEnable == true && earlierRising.LocalResult
                    && earlierRising.PreviousKnown && earlierRising.PreviousInput == false && earlierRising.CurrentInput == true
                    && earlierRising.WriteDisposition == InstructionWriteDisposition.StateUpdatedChanged
                    && earlierRising.Operands.Single(operand => operand.Role == "A") is { Identity: "rising_signal", DeclaredType: PlcVariableType.Bool, Value: true }
                    && earlierCapture.TryGetValue("edge-falling-unknown", out var earlierFalling)
                    && earlierFalling.NetworkEnable == true && earlierFalling.LocalResult
                    && earlierFalling.PreviousKnown && earlierFalling.PreviousInput == true && earlierFalling.CurrentInput == false
                    && earlierFalling.WriteDisposition == InstructionWriteDisposition.StateUpdatedChanged
                    && earlierFalling.Operands.Single(operand => operand.Role == "A") is { Identity: "falling_signal", DeclaredType: PlcVariableType.Bool, Value: false },
                    "earlier scan detected exact 0-to-1 and 1-to-0 transitions with explicit edge-memory state");
                Require(!capture.GlobalLast.ContainsKey("edge-rising-unknown")
                    && !capture.GlobalLast.ContainsKey("edge-falling-unknown")
                    && !capture.GlobalLast.ContainsKey("edge-rising-unknown-coil")
                    && !capture.GlobalLast.ContainsKey("edge-falling-unknown-coil")
                    && capture.GlobalLast["edge-gate-jump"].ControlOutcome == "Taken"
                    && capture.GlobalLast.ContainsKey("edge-label-coil")
                    && snapshot.Variables["rising_unknown_out"]
                    && snapshot.Variables["falling_unknown_out"]
                    && snapshot.Variables["rising_memory"]
                    && !snapshot.Variables["falling_memory"],
                    "current scan skipped both edge networks and preserved their earlier outputs and edge-memory bits");
            }
            if (unknownCoil)
            {
                var earlierCapture = earlierCycleSnapshot?.Observations?.GlobalLast;
                Require(earlierCapture is not null
                    && earlierCapture.TryGetValue("assign-unknown-coil", out var earlierAssign)
                    && earlierAssign.NetworkEnable == true && earlierAssign.LocalResult
                    && earlierAssign.DestinationIdentity == "assign_unknown_out"
                    && Equals(earlierAssign.DestinationBefore, false) && Equals(earlierAssign.StoredAfter, true)
                    && earlierAssign.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && earlierCapture.TryGetValue("set-unknown-coil", out var earlierSet)
                    && earlierSet.NetworkEnable == true && earlierSet.LocalResult
                    && earlierSet.DestinationIdentity == "set_unknown_out"
                    && Equals(earlierSet.DestinationBefore, false) && Equals(earlierSet.StoredAfter, true)
                    && earlierSet.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && earlierCapture.TryGetValue("reset-unknown-coil", out var earlierReset)
                    && earlierReset.NetworkEnable == true && earlierReset.LocalResult
                    && earlierReset.DestinationIdentity == "reset_unknown_out"
                    && Equals(earlierReset.DestinationBefore, true) && Equals(earlierReset.StoredAfter, false)
                    && earlierReset.WriteDisposition == InstructionWriteDisposition.WrittenChanged,
                    "earlier scan executed exact assignment, Set, and Reset writes from TRUE incoming RLO");
                Require(!capture.GlobalLast.ContainsKey("assign-unknown-contact")
                    && !capture.GlobalLast.ContainsKey("assign-unknown-coil")
                    && !capture.GlobalLast.ContainsKey("set-unknown-contact")
                    && !capture.GlobalLast.ContainsKey("set-unknown-coil")
                    && !capture.GlobalLast.ContainsKey("reset-unknown-contact")
                    && !capture.GlobalLast.ContainsKey("reset-unknown-coil")
                    && capture.GlobalLast["coil-gate-jump"].ControlOutcome == "Taken"
                    && capture.GlobalLast.ContainsKey("coil-label-coil")
                    && snapshot.Variables["assign_unknown_out"]
                    && snapshot.Variables["set_unknown_out"]
                    && !snapshot.Variables["reset_unknown_out"]
                    && snapshot.Variables["coil_label_output"],
                    "current scan skipped all three coil networks, visited the LABEL destination, and preserved earlier output states");
            }
            if (fixture == "edge-tia")
            {
                var rising = capture.GlobalLast["edge-rising"];
                var falling = capture.GlobalLast["edge-falling"];
                var risingCoil = capture.GlobalLast["edge-rising-coil"];
                var fallingCoil = capture.GlobalLast["edge-falling-coil"];
                var expected = edgeState == "pulse";
                var risingCurrent = edgeState != "baseline";
                var fallingCurrent = edgeState == "baseline";
                var edgeDisposition = edgeState == "held"
                    ? InstructionWriteDisposition.StateUpdatedSameValue
                    : InstructionWriteDisposition.StateUpdatedChanged;
                if (edgeState == "baseline")
                {
                    Require(rising.WriteDisposition == InstructionWriteDisposition.StateUpdatedSameValue
                        && falling.WriteDisposition == InstructionWriteDisposition.StateUpdatedChanged,
                        "baseline P retains FALSE memory while N changes memory from FALSE to TRUE");
                }
                else Require(rising.WriteDisposition == edgeDisposition && falling.WriteDisposition == edgeDisposition,
                    "pulse and held P/N observations classify exact edge-memory updates");
                foreach (var (observed, identity, current) in new[]
                {
                    (rising, "rising_signal", risingCurrent),
                    (falling, "falling_signal", fallingCurrent),
                })
                {
                    var operand = observed.Operands.SingleOrDefault(item => item.Role == "A");
                    Require(observed.Evaluated && observed.NetworkEnable == true
                        && observed.PreviousKnown && observed.LocalResult == expected
                        && observed.DestinationIdentity.Length == 0
                        && observed.Diagnostics.Count == 0
                        && operand is not null
                        && operand.Identity == identity
                        && operand.DeclaredType == PlcVariableType.Bool
                        && operand.Reason == ObservationUnavailableReason.None
                        && Equals(operand.Value, current),
                        $"{observed.InstructionId} samples the exact BOOL signal with powered continuity and no diagnostic");
                }
                Require(edgeState switch
                {
                    "baseline" => rising.PreviousInput == false && rising.CurrentInput == false
                        && falling.PreviousInput == false && falling.CurrentInput == true,
                    "pulse" => rising.PreviousInput == false && rising.CurrentInput == true
                        && falling.PreviousInput == true && falling.CurrentInput == false,
                    _ => rising.PreviousInput == true && rising.CurrentInput == true
                        && falling.PreviousInput == false && falling.CurrentInput == false,
                }, "P/N observations retain the exact previous/current edge-memory transition");
                var outputBefore = edgeState == "held";
                var outputDisposition = edgeState == "baseline"
                    ? InstructionWriteDisposition.WrittenSameValue
                    : InstructionWriteDisposition.WrittenChanged;
                Require(risingCoil.NetworkEnable == expected && risingCoil.LocalResult == expected
                    && risingCoil.DestinationIdentity == "rising_out"
                    && Equals(risingCoil.DestinationBefore, outputBefore)
                    && Equals(risingCoil.StoredAfter, expected)
                    && risingCoil.WriteDisposition == outputDisposition
                    && risingCoil.Diagnostics.Count == 0
                    && fallingCoil.NetworkEnable == expected && fallingCoil.LocalResult == expected
                    && fallingCoil.DestinationIdentity == "falling_out"
                    && Equals(fallingCoil.DestinationBefore, outputBefore)
                    && Equals(fallingCoil.StoredAfter, expected)
                    && fallingCoil.WriteDisposition == outputDisposition
                    && fallingCoil.Diagnostics.Count == 0
                    && snapshot.Variables["rising_out"] == expected
                    && snapshot.Variables["falling_out"] == expected
                    && snapshot.Variables["rising_memory"] == risingCurrent
                    && snapshot.Variables["falling_memory"] == fallingCurrent,
                    "P/N outputs and explicit edge-memory operands match the requested baseline, one-cycle pulse, or held state");
                GD.Print($"TIA_EDGE_STATE_PASS state={edgeState} rising={(rising.LocalResult ? 1 : 0)}/{rising.WriteDisposition} falling={(falling.LocalResult ? 1 : 0)}/{falling.WriteDisposition} outputs={outputDisposition} diagnostics=0 scan={snapshot.ScanNumber}");
            }
            if (fixture == "assignment-coil-tia")
            {
                var falseWrite = capture.GlobalLast["assign-false-coil"];
                var trueWrite = capture.GlobalLast["assign-true-coil"];
                Require(!snapshot.Variables["assign_false_writes"]
                    && snapshot.Variables["assign_true_writes"],
                    "assignment outputs overwrite inverse initial states with the incoming RLO");
                Require(falseWrite.NetworkEnable == false && !falseWrite.LocalResult
                    && falseWrite.DestinationIdentity == "assign_false_writes"
                    && Equals(falseWrite.DestinationBefore, true)
                    && Equals(falseWrite.StoredAfter, false)
                    && falseWrite.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && falseWrite.Diagnostics.Count == 0
                    && trueWrite.NetworkEnable == true && trueWrite.LocalResult
                    && trueWrite.DestinationIdentity == "assign_true_writes"
                    && Equals(trueWrite.DestinationBefore, false)
                    && Equals(trueWrite.StoredAfter, true)
                    && trueWrite.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && trueWrite.Diagnostics.Count == 0,
                    "assignment observations prove FALSE and TRUE RLO are both written and passed through with exact before/after state");
                GD.Print($"TIA_ASSIGNMENT_COIL_STATE_PASS falseWrite=0 falseDisposition={falseWrite.WriteDisposition} trueWrite=1 trueDisposition={trueWrite.WriteDisposition} diagnostics=0");
            }
            if (fixture == "coil-tia")
            {
                var setFalse = capture.GlobalLast["set-false-coil"];
                var setTrue = capture.GlobalLast["set-true-coil"];
                var resetFalse = capture.GlobalLast["reset-false-coil"];
                var resetTrue = capture.GlobalLast["reset-true-coil"];
                Require(snapshot.Variables["set_false_holds"]
                    && snapshot.Variables["set_true_writes"]
                    && !snapshot.Variables["reset_false_holds"]
                    && !snapshot.Variables["reset_true_writes"],
                    "S/R outputs preserve on false RLO and write on true RLO");
                Require(setFalse.NetworkEnable == false && !setFalse.LocalResult
                    && setFalse.DestinationIdentity == "set_false_holds"
                    && Equals(setFalse.DestinationBefore, true) && Equals(setFalse.StoredAfter, true)
                    && setFalse.WriteDisposition == InstructionWriteDisposition.SkippedDisabled
                    && setFalse.Diagnostics.Count == 0
                    && setTrue.NetworkEnable == true && setTrue.LocalResult
                    && setTrue.DestinationIdentity == "set_true_writes"
                    && Equals(setTrue.DestinationBefore, false) && Equals(setTrue.StoredAfter, true)
                    && setTrue.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && setTrue.Diagnostics.Count == 0
                    && resetFalse.NetworkEnable == false && !resetFalse.LocalResult
                    && resetFalse.DestinationIdentity == "reset_false_holds"
                    && Equals(resetFalse.DestinationBefore, false) && Equals(resetFalse.StoredAfter, false)
                    && resetFalse.WriteDisposition == InstructionWriteDisposition.SkippedDisabled
                    && resetFalse.Diagnostics.Count == 0
                    && resetTrue.NetworkEnable == true && resetTrue.LocalResult
                    && resetTrue.DestinationIdentity == "reset_true_writes"
                    && Equals(resetTrue.DestinationBefore, true) && Equals(resetTrue.StoredAfter, false)
                    && resetTrue.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && resetTrue.Diagnostics.Count == 0,
                    "S/R observations prove false RLO skips and preserves while true RLO writes, with exact destination before/after state");
                GD.Print($"TIA_SET_RESET_STATE_PASS setFalseHold=1 setFalseDisposition={setFalse.WriteDisposition} setTrueWrite=1 setTrueDisposition={setTrue.WriteDisposition} resetFalseHold=0 resetFalseDisposition={resetFalse.WriteDisposition} resetTrueWrite=0 resetTrueDisposition={resetTrue.WriteDisposition} diagnostics=0");
            }
            if (fixture == "program-control-tia")
            {
                var falseJump = capture.GlobalLast["jmp-false"];
                var trueJump = capture.GlobalLast["jmp-true"];
                var afterFalseJump = capture.GlobalLast["after-false-jmp-coil"];
                var labelDestination = capture.GlobalLast["finish-label-coil"];
                Require(snapshot.Variables["after_false_jump"]
                    && !snapshot.Variables["skipped_by_jump"]
                    && snapshot.Variables["label_destination_output"]
                    && snapshot.Variables["after_false_return"]
                    && !snapshot.Variables["skipped_by_return"],
                    "taken and not-taken JMP/RET paths execute only their intended networks");
                Require(falseJump.NetworkEnable == false && !falseJump.LocalResult
                    && falseJump.ControlOutcome == "NotTaken"
                    && falseJump.DestinationIdentity.Length == 0
                    && falseJump.WriteDisposition == InstructionWriteDisposition.NotApplicable
                    && falseJump.Diagnostics.Count == 0
                    && trueJump.NetworkEnable == true && trueJump.LocalResult
                    && trueJump.ControlOutcome == "Taken"
                    && trueJump.DestinationIdentity.Length == 0
                    && trueJump.WriteDisposition == InstructionWriteDisposition.NotApplicable
                    && trueJump.Diagnostics.Count == 0
                    && capture.GlobalLast["ret-false"].NetworkEnable == false
                    && capture.GlobalLast["ret-false"].ControlOutcome == "NotTaken"
                    && capture.GlobalLast["ret-true"].NetworkEnable == true
                    && capture.GlobalLast["ret-true"].ControlOutcome == "Taken",
                    "program-control observations distinguish exact incoming RLO, transfer outcome, disposition, and diagnostics");
                Require(!capture.GlobalLast.ContainsKey("jump-skipped-coil")
                    && !capture.GlobalLast.ContainsKey("return-skipped-coil"),
                    "taken transfers leave skipped instructions unobserved");
                Require(afterFalseJump.NetworkEnable == true && afterFalseJump.LocalResult
                    && afterFalseJump.DestinationIdentity == "after_false_jump"
                    && Equals(afterFalseJump.DestinationBefore, false) && Equals(afterFalseJump.StoredAfter, true)
                    && afterFalseJump.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && afterFalseJump.Diagnostics.Count == 0
                    && labelDestination.NetworkEnable == true && labelDestination.LocalResult
                    && labelDestination.DestinationIdentity == "label_destination_output"
                    && Equals(labelDestination.DestinationBefore, false) && Equals(labelDestination.StoredAfter, true)
                    && labelDestination.WriteDisposition == InstructionWriteDisposition.WrittenChanged
                    && labelDestination.Diagnostics.Count == 0
                    && snapshot.Elements["finish-label"].Energized,
                    "false JMP executes its following write while taken JMP skips the intervening write and visits the labeled destination network");
                GD.Print($"TIA_PROGRAM_CONTROL_STATE_PASS jmpFalse={falseJump.ControlOutcome}/{falseJump.WriteDisposition} jmpTrue={trueJump.ControlOutcome}/{trueJump.WriteDisposition} afterFalse={afterFalseJump.WriteDisposition} skippedAbsent=1 labelVisited=1 labelWrite={labelDestination.WriteDisposition} diagnostics=0");
            }
            if (fixture == "return-value-tia")
            {
                GD.Print($"TIA_RET_RETURN_VALUE_DETAIL retEnable={capture.GlobalLast["return-value-ret"].NetworkEnable} retOutcome={capture.GlobalLast["return-value-ret"].ControlOutcome} callLocal={capture.GlobalLast["return-value-call"].LocalResult} callElement={snapshot.Elements["return-value-call"].Energized}");
                Require(snapshot.CallInvocations.Count == 1
                    && snapshot.CallInvocations[0].Completion == LadderCallCompletion.Completed
                    && !snapshot.CallInvocations[0].EnableOutput,
                    "RET FALSE completes the call with caller ENO false");
                Require(capture.GlobalLast["return-value-ret"].NetworkEnable == true
                    && capture.GlobalLast["return-value-ret"].ControlOutcome == "Taken"
                    && capture.GlobalLast["return-value-call"].LocalResult == false,
                    "RET observes true incoming RLO while the calling block observes false ENO");
                GD.Print("TIA_RET_RETURN_VALUE_STATE_PASS incomingRlo=1 selectedValue=FALSE callerEno=0");
            }
            if (fixture == "branch-start")
                Require(snapshot.Variables["lamp"]
                    && capture.GlobalLast["start-permissive"].LocalResult
                    && capture.GlobalLast["top-cell-permissive"].LocalResult
                    && capture.GlobalLast["top-layer-incomplete"].LocalResult
                    && !capture.GlobalLast["seal-in-contact"].LocalResult,
                    "first rung path powers the output while the seal-in branch is false");
            if (fixture == "branch-nested")
            {
                Require(snapshot.Variables["nested_out"]
                    && capture.GlobalLast["branch-nested-lead"].LocalResult
                    && capture.GlobalLast["branch-upper"].LocalResult
                    && !capture.GlobalLast["branch-inner-a"].LocalResult
                    && capture.GlobalLast["branch-inner-b"].LocalResult
                    && !capture.GlobalLast["branch-lower"].LocalResult
                    && capture.GlobalLast["branch-nested-tail"].LocalResult
                    && capture.GlobalLast["branch-nested-coil"].NetworkEnable == true,
                    "non-root outer branch and nested inner branch preserve exact TRUE/FALSE paths and power the output");
                GD.Print("TIA_NESTED_BRANCH_STATE_PASS lead=1 upper=1 innerA=0 innerB=1 innerJoin=1 lower=0 outerJoin=1 tail=1 output=1");
            }
            if (fixture is "numeric-add-tia" or "numeric-add-overflow-tia"
                or "numeric-sub-tia" or "numeric-sub-overflow-tia"
                or "numeric-mul-tia" or "numeric-mul-overflow-tia"
                or "numeric-div-tia" or "numeric-div-zero-tia" or "numeric-mod-tia" or "numeric-mod-zero-tia")
            {
                var kind = numericEvidenceOperation!.Kind;
                var overflow = fixture.Contains("overflow", StringComparison.Ordinal);
                var divideByZero = fixture == "numeric-div-zero-tia";
                var moduloByZero = fixture == "numeric-mod-zero-tia";
                var zeroDivisor = divideByZero || moduloByZero;
                var faulted = overflow || moduloByZero;
                var expected = kind switch
                {
                    LadderNumericOperationKind.Subtract => 2,
                    LadderNumericOperationKind.Multiply => 6,
                    LadderNumericOperationKind.Divide => divideByZero ? 0 : 4,
                    LadderNumericOperationKind.Modulo => moduloByZero ? 0 : 2,
                    _ => 4,
                };
                var expectedBefore = overflow || zeroDivisor ? 12.0 : 0.0;
                Require(numericEvidenceSnapshot.NumericVariables["result"] == (enable && !overflow ? expected : expectedBefore),
                    "explicitly typed integer arithmetic stores its in-range result and preserves the simulator destination on overflow");
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-add"];
                var sourceA = observed.Operands.Single(operand => operand.Role == "A");
                var sourceB = observed.Operands.Single(operand => operand.Role == "B");
                var expectedA = overflow
                    ? kind == LadderNumericOperationKind.Subtract ? -32768.0 : 32767.0
                    : zeroDivisor ? 7.0
                    : fixture is "numeric-div-tia" or "numeric-mod-tia" ? 8.0 : 3.0;
                var expectedB = overflow
                    ? kind == LadderNumericOperationKind.Multiply ? 2.0 : 1.0
                    : fixture switch
                {
                    "numeric-div-zero-tia" or "numeric-mod-zero-tia" => 0.0,
                    "numeric-mul-tia" or "numeric-div-tia" => 2.0,
                    "numeric-mod-tia" => 3.0,
                    _ => 1.0,
                };
                var diagnosticCode = kind switch
                {
                    LadderNumericOperationKind.Add => "VC_RUNTIME_ADD_OVERFLOW",
                    LadderNumericOperationKind.Subtract => "VC_RUNTIME_SUB_OVERFLOW",
                    LadderNumericOperationKind.Multiply => "VC_RUNTIME_MUL_OVERFLOW",
                    LadderNumericOperationKind.Modulo when moduloByZero => "VC_RUNTIME_MOD_ZERO",
                    _ => string.Empty,
                };
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, expectedBefore)
                    && Equals(observed.StoredAfter, enable && !overflow ? (double)expected : expectedBefore)
                    && Equals(observed.CalculatedResult, enable && !overflow ? (double)expected : null)
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : moduloByZero
                            ? InstructionWriteDisposition.FailedErrorValueWritten
                        : overflow
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && faulted ? 1 : 0)
                    && (!enable || !faulted || observed.Diagnostics.Single().Code.StartsWith(
                        diagnosticCode, StringComparison.Ordinal)),
                    "typed arithmetic records exact EN/ENO, destination transition, disposition, and overflow diagnostic");
                Require(enable
                        ? sourceA.Identity == "value"
                            && sourceA.DeclaredType == PlcVariableType.Int
                            && sourceA.Reason == ObservationUnavailableReason.None
                            && Equals(sourceA.Value, expectedA)
                            && sourceB.Identity == "operand_b"
                            && sourceB.DeclaredType == PlcVariableType.Int
                            && sourceB.Reason == ObservationUnavailableReason.None
                            && Equals(sourceB.Value, expectedB)
                        : sourceA.Identity.Length == 0
                            && sourceA.DeclaredType is null
                            && sourceA.Reason == ObservationUnavailableReason.ActionDisabled
                            && sourceA.Value is null
                            && sourceB.Identity.Length == 0
                            && sourceB.DeclaredType is null
                            && sourceB.Reason == ObservationUnavailableReason.ActionDisabled
                            && sourceB.Value is null,
                    "typed arithmetic records exact enabled operands or disabled not-consumed states");
                GD.Print($"TIA_ARITHMETIC_STATE_PASS kind={kind} enable={(enable ? 1 : 0)} overflow={(overflow ? 1 : 0)} zeroDivisor={(zeroDivisor ? 1 : 0)} a={(enable ? expectedA : double.NaN):R} b={(enable ? expectedB : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={numericEvidenceSnapshot.NumericVariables["result"]:R} eno={(observed.NetworkEnable == true && observed.Diagnostics.Count == 0 ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture == "numeric-move-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-move"];
                var result = numericEvidenceSnapshot.NumericVariables["destination"];
                var source = observed.Operands.Single(operand => operand.Role == "A");
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "destination"
                    && Equals(observed.DestinationBefore, 7.0)
                    && Equals(observed.StoredAfter, enable ? 42.0 : 7.0)
                    && Equals(observed.CalculatedResult, enable ? 42.0 : null)
                    && observed.Diagnostics.Count == 0
                    && observed.WriteDisposition == (enable
                        ? InstructionWriteDisposition.WrittenChanged
                        : InstructionWriteDisposition.SkippedDisabled)
                    && !observed.Rounded && !observed.Clamped,
                    "MOVE records exact EN/ENO, before/calculated/after result, disposition, and diagnostics");
                Require(enable
                        ? result == 42
                            && source.Identity == "source"
                            && source.DeclaredType == PlcVariableType.DInt
                            && source.Reason == ObservationUnavailableReason.None
                            && source.Value is double sourceValue
                            && sourceValue == 42.0
                        : result == 7
                            && source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "MOVE records the exact enabled DINT source or disabled not-consumed state");
                var eno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_MOVE_STATE_PASS enable={(enable ? 1 : 0)} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(eno ? 1 : 0)} diagnostics=0 disposition={observed.WriteDisposition}");
            }
            if (fixture is "numeric-abs-tia" or "numeric-neg-tia" or "numeric-abs-overflow-tia" or "numeric-neg-overflow-tia")
            {
                var overflowInput = fixture.Contains("overflow", StringComparison.Ordinal);
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-unary"];
                var source = observed.Operands.Single(operand => operand.Role == "A");
                var expectedSource = overflowInput ? -32768.0 : -8.0;
                var expectedDiagnostics = enable && overflowInput ? 1 : 0;
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, 0.0)
                    && Equals(observed.CalculatedResult, enable && !overflowInput ? 8.0 : null)
                    && Equals(observed.StoredAfter, enable && !overflowInput ? 8.0 : 0.0)
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : overflowInput
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == expectedDiagnostics
                    && (!overflowInput || !enable || observed.Diagnostics.Single().Code.StartsWith(
                        numericEvidenceOperation!.Kind == LadderNumericOperationKind.Absolute
                            ? "VC_RUNTIME_ABS_OVERFLOW"
                            : "VC_RUNTIME_NEG_OVERFLOW", StringComparison.Ordinal)),
                    "typed unary records exact EN, destination transition, disposition, overflow diagnostic, and stored result");
                Require(enable
                        ? source.Identity == "value"
                            && source.DeclaredType == PlcVariableType.Int
                            && source.Reason == ObservationUnavailableReason.None
                            && Equals(source.Value, expectedSource)
                        : source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "typed unary records the exact enabled INT source or disabled not-consumed state");
                var unaryEno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_UNARY_STATE_PASS kind={numericEvidenceOperation!.Kind} enable={(enable ? 1 : 0)} source={(enable ? expectedSource : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={numericEvidenceSnapshot.NumericVariables["result"]:R} eno={(unaryEno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture is "numeric-sqrt-tia" or "numeric-sqrt-domain-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-sqrt"];
                var domainFault = fixture == "numeric-sqrt-domain-tia";
                var source = observed.Operands.Single(operand => operand.Role == "A");
                var expectedSource = domainFault ? -1.0 : 25.0;
                var storedIsExpected = enable && domainFault
                    ? observed.StoredAfter is double stored && double.IsNaN(stored)
                    : Equals(observed.StoredAfter, enable ? 5.0 : 0.0);
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, 0.0)
                    && Equals(observed.CalculatedResult, enable && !domainFault ? 5.0 : null)
                    && storedIsExpected
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : domainFault
                            ? InstructionWriteDisposition.FailedInvalidValueWritten
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && domainFault ? 1 : 0)
                    && (!enable || !domainFault || observed.Diagnostics.Single().Code.StartsWith(
                        "VC_RUNTIME_SQRT_DOMAIN", StringComparison.Ordinal)),
                    "SQRT records exact EN, destination transition, invalid-value write disposition, and domain diagnostic");
                Require(enable
                        ? source.Identity == "radicand"
                            && source.DeclaredType == PlcVariableType.Real
                            && source.Reason == ObservationUnavailableReason.None
                            && Equals(source.Value, expectedSource)
                        : source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "SQRT records the exact enabled REAL source or disabled not-consumed state");
                var sqrtEno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_SQRT_STATE_PASS enable={(enable ? 1 : 0)} source={(enable ? expectedSource : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={numericEvidenceSnapshot.NumericVariables["result"]:R} eno={(sqrtEno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture is "numeric-expt-tia" or "numeric-expt-domain-tia" or "numeric-expt-overflow-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-expt"];
                var domainFault = fixture == "numeric-expt-domain-tia";
                var overflow = fixture == "numeric-expt-overflow-tia";
                var faulted = domainFault || overflow;
                var baseOperand = observed.Operands.Single(operand => operand.Role == "A");
                var exponentOperand = observed.Operands.Single(operand => operand.Role == "B");
                var expectedBase = domainFault ? -2.0 : overflow ? 10.0 : 2.0;
                var expectedExponent = domainFault ? 0.5 : overflow ? 50.0 : 8.0;
                var diagnosticCode = domainFault ? "VC_RUNTIME_DOMAIN" : "VC_RUNTIME_EXPT_OVERFLOW";
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, 7.0)
                    && Equals(observed.CalculatedResult, enable && !faulted ? 256.0 : null)
                    && Equals(observed.StoredAfter, enable && !faulted ? 256.0 : 7.0)
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : faulted
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && faulted ? 1 : 0)
                    && (!enable || !faulted || observed.Diagnostics.Single().Code.StartsWith(
                        diagnosticCode, StringComparison.Ordinal)),
                    "EXPT records exact EN, destination transition, disposition, domain/overflow diagnostic, and stored result");
                Require(enable
                        ? baseOperand.Identity == "base_value"
                            && baseOperand.DeclaredType == PlcVariableType.Real
                            && baseOperand.Reason == ObservationUnavailableReason.None
                            && Equals(baseOperand.Value, expectedBase)
                            && exponentOperand.Identity == "exponent_value"
                            && exponentOperand.DeclaredType == PlcVariableType.Real
                            && exponentOperand.Reason == ObservationUnavailableReason.None
                            && Equals(exponentOperand.Value, expectedExponent)
                        : baseOperand.Identity.Length == 0
                            && baseOperand.DeclaredType is null
                            && baseOperand.Reason == ObservationUnavailableReason.ActionDisabled
                            && baseOperand.Value is null
                            && exponentOperand.Identity.Length == 0
                            && exponentOperand.DeclaredType is null
                            && exponentOperand.Reason == ObservationUnavailableReason.ActionDisabled
                            && exponentOperand.Value is null,
                    "EXPT records the exact enabled REAL operands or disabled not-consumed states");
                var exptEno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_EXPT_STATE_PASS enable={(enable ? 1 : 0)} domainFault={(domainFault ? 1 : 0)} overflow={(overflow ? 1 : 0)} base={(enable ? expectedBase : double.NaN):R} exponent={(enable ? expectedExponent : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={numericEvidenceSnapshot.NumericVariables["result"]:R} eno={(exptEno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture == "numeric-scientific-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-scientific"];
                var scientificKind = numericEvidenceOperation!.Kind;
                var expectedSource = (scientificKind, scientificDomainFault) switch
                {
                    (LadderNumericOperationKind.NaturalLog, true) => -1.0,
                    (LadderNumericOperationKind.ArcSine or LadderNumericOperationKind.ArcCosine, true) => 2.0,
                    (LadderNumericOperationKind.NaturalLog, false) => Math.E,
                    (LadderNumericOperationKind.Sine, false) => Math.PI / 2,
                    (LadderNumericOperationKind.Cosine, false) => Math.PI,
                    (LadderNumericOperationKind.Tangent, false) => Math.PI / 4,
                    (LadderNumericOperationKind.ArcSine, false) => 1.0,
                    (LadderNumericOperationKind.ArcCosine, false) => -1.0,
                    (LadderNumericOperationKind.ArcTangent, false) => 1.0,
                    _ => throw new InvalidOperationException("Unsupported scientific review operation."),
                };
                expectedSource = (double)(float)expectedSource;
                var realSource = (float)expectedSource;
                var expected = scientificKind switch
                {
                    LadderNumericOperationKind.NaturalLog => (double)(float)Math.Log(realSource),
                    LadderNumericOperationKind.Sine => (double)(float)Math.Sin(realSource),
                    LadderNumericOperationKind.Cosine => (double)(float)Math.Cos(realSource),
                    LadderNumericOperationKind.Tangent => (double)(float)Math.Tan(realSource),
                    LadderNumericOperationKind.ArcSine => (double)(float)Math.Asin(realSource),
                    LadderNumericOperationKind.ArcCosine => (double)(float)Math.Acos(realSource),
                    LadderNumericOperationKind.ArcTangent => (double)(float)Math.Atan(realSource),
                    _ => throw new InvalidOperationException("Unsupported scientific review operation."),
                };
                var result = numericEvidenceSnapshot.NumericVariables["result"];
                var source = observed.Operands.Single(operand => operand.Role == "A");
                var storedIsExpected = enable && scientificDomainFault
                    ? observed.StoredAfter is double stored && double.IsNaN(stored)
                    : Equals(observed.StoredAfter, enable ? expected : 7.0);
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, 7.0)
                    && Equals(observed.CalculatedResult, enable && !scientificDomainFault ? expected : null)
                    && storedIsExpected
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : scientificDomainFault
                            ? InstructionWriteDisposition.FailedInvalidValueWritten
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && scientificDomainFault ? 1 : 0)
                    && (!enable || !scientificDomainFault || observed.Diagnostics.Single().Code.StartsWith(
                        "VC_RUNTIME_DOMAIN", StringComparison.Ordinal)),
                    "scientific unary records exact EN, destination transition, invalid-value write disposition, and domain diagnostic");
                Require(enable
                        ? source.Identity == "source_value"
                            && source.DeclaredType == PlcVariableType.Real
                            && source.Reason == ObservationUnavailableReason.None
                            && Equals(source.Value, expectedSource)
                        : source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "scientific unary records the exact enabled REAL source or disabled not-consumed state");
                var scientificEno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_SCIENTIFIC_STATE_PASS kind={scientificKind} enable={(enable ? 1 : 0)} domainFault={(scientificDomainFault ? 1 : 0)} source={(enable ? expectedSource : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(scientificEno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            void RequireScalingObservation(InstructionObservation observed, bool isEnabled, string? faultCode,
                double expectedResult, PlcVariableType rangeType, PlcVariableType valueType,
                double minimum, double value, double maximum,
                string mnemonic, double expectedBefore = 7.0)
            {
                var faulted = faultCode is not null;
                static bool ExactNumeric(object? actual, double expected) => actual is double numeric
                    && (double.IsNaN(expected) ? double.IsNaN(numeric) : numeric.Equals(expected));
                var sourceA = observed.Operands.Single(operand => operand.Role == "A");
                var sourceB = observed.Operands.Single(operand => operand.Role == "B");
                var sourceC = observed.Operands.Single(operand => operand.Role == "C");
                Require(observed.Evaluated
                    && observed.NetworkEnable == isEnabled
                    && observed.LocalResult == isEnabled
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, expectedBefore)
                    && Equals(observed.CalculatedResult, isEnabled && !faulted ? expectedResult : null)
                    && Equals(observed.StoredAfter, isEnabled && !faulted ? expectedResult : expectedBefore)
                    && observed.WriteDisposition == (!isEnabled
                        ? InstructionWriteDisposition.SkippedDisabled
                        : faulted
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (isEnabled && faulted ? 1 : 0)
                    && (!isEnabled || !faulted || observed.Diagnostics.Single().Code.StartsWith(
                        faultCode!, StringComparison.Ordinal)),
                    $"{mnemonic} records exact EN, destination transition, disposition, fault diagnostic, and stored result");
                Require(isEnabled
                        ? sourceA.Identity == "minimum"
                            && sourceA.DeclaredType == rangeType
                            && sourceA.Reason == ObservationUnavailableReason.None
                            && ExactNumeric(sourceA.Value, minimum)
                            && sourceB.Identity == "value"
                            && sourceB.DeclaredType == valueType
                            && sourceB.Reason == ObservationUnavailableReason.None
                            && ExactNumeric(sourceB.Value, value)
                            && sourceC.Identity == "maximum"
                            && sourceC.DeclaredType == rangeType
                            && sourceC.Reason == ObservationUnavailableReason.None
                            && ExactNumeric(sourceC.Value, maximum)
                        : new[] { sourceA, sourceB, sourceC }.All(source => source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null),
                    $"{mnemonic} records exact sampled MIN/VALUE/MAX operands or disabled not-consumed states");
            }
            if (fixture is "numeric-trunc-tia" or "numeric-trunc-overflow-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-trunc"];
                var overflow = fixture == "numeric-trunc-overflow-tia";
                var result = numericEvidenceSnapshot.NumericVariables["result"];
                var expectedSource = (double)(float)(overflow ? 40000.9 : -7.9);
                var expectedBefore = overflow ? 12.0 : 7.0;
                var source = observed.Operands.Single(operand => operand.Role == "A");
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "result"
                    && Equals(observed.DestinationBefore, expectedBefore)
                    && Equals(observed.CalculatedResult, enable && !overflow ? -7.0 : null)
                    && Equals(observed.StoredAfter, enable && !overflow ? -7.0 : expectedBefore)
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : overflow
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && overflow ? 1 : 0)
                    && (!enable || !overflow || observed.Diagnostics.Single().Code.StartsWith(
                        "VC_RUNTIME_TRUNC_OVERFLOW", StringComparison.Ordinal)),
                    "TRUNC records exact EN, destination transition, disposition, overflow diagnostic, and stored result");
                Require(enable
                        ? source.Identity == "source_value"
                            && source.DeclaredType == PlcVariableType.Real
                            && source.Reason == ObservationUnavailableReason.None
                            && Equals(source.Value, expectedSource)
                        : source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "TRUNC records the exact sampled REAL source or disabled not-consumed state");
                Require(result == (enable && !overflow ? -7.0 : expectedBefore),
                    "TRUNC stores the exact enabled, disabled, or integer-overflow result");
                var eno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_TRUNC_STATE_PASS enable={(enable ? 1 : 0)} overflow={(overflow ? 1 : 0)} source={(enable ? expectedSource : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(eno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture is "numeric-convert-tia" or "numeric-convert-overflow-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-convert"];
                var overflow = fixture == "numeric-convert-overflow-tia";
                var result = numericEvidenceSnapshot.NumericVariables["destination"];
                var expectedSource = overflow ? 40000.0 : 42.0;
                var expectedBefore = overflow ? 12.0 : 7.0;
                var source = observed.Operands.Single(operand => operand.Role == "A");
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "destination"
                    && Equals(observed.DestinationBefore, expectedBefore)
                    && Equals(observed.CalculatedResult, enable && !overflow ? 42.0 : null)
                    && Equals(observed.StoredAfter, enable && !overflow ? 42.0 : expectedBefore)
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : overflow
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && overflow ? 1 : 0)
                    && (!enable || !overflow || observed.Diagnostics.Single().Code.StartsWith(
                        "VC_RUNTIME_CONVERT_OVERFLOW", StringComparison.Ordinal)),
                    "CONV records exact EN, destination transition, disposition, overflow diagnostic, and stored result");
                Require(enable
                        ? source.Identity == "source"
                            && source.DeclaredType == (overflow ? PlcVariableType.Real : PlcVariableType.Int)
                            && source.Reason == ObservationUnavailableReason.None
                            && Equals(source.Value, expectedSource)
                        : source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "CONV records the exact sampled source or disabled not-consumed state");
                Require(result == (enable && !overflow ? 42.0 : expectedBefore),
                    "CONV stores the exact enabled, disabled, or overflow-retained result");
                var eno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_CONVERT_STATE_PASS enable={(enable ? 1 : 0)} overflow={(overflow ? 1 : 0)} source={(enable ? expectedSource : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(eno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture is "numeric-round-tia" or "numeric-round-overflow-tia"
                or "numeric-ceil-tia" or "numeric-ceil-overflow-tia"
                or "numeric-floor-tia" or "numeric-floor-overflow-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-rounding"];
                var kind = numericEvidenceOperation!.Kind;
                var overflow = fixture is "numeric-round-overflow-tia" or "numeric-ceil-overflow-tia" or "numeric-floor-overflow-tia";
                var expectedSource = (double)(float)(overflow ? 3_000_000_000.0
                    : kind == LadderNumericOperationKind.Round ? 1.5 : -0.5);
                var expectedBefore = overflow ? 12.0 : 7.0;
                var expected = kind switch
                {
                    LadderNumericOperationKind.Ceiling => 0.0,
                    LadderNumericOperationKind.Floor => -1.0,
                    _ => 2.0,
                };
                var result = numericEvidenceSnapshot.NumericVariables["destination"];
                var source = observed.Operands.Single(operand => operand.Role == "A");
                var diagnosticCode = $"VC_RUNTIME_{LadderNumericOperationRules.RoundingMnemonic(kind)}_OVERFLOW";
                Require(observed.Evaluated
                    && observed.NetworkEnable == enable
                    && observed.LocalResult == enable
                    && observed.DestinationIdentity == "destination"
                    && Equals(observed.DestinationBefore, expectedBefore)
                    && Equals(observed.CalculatedResult, enable && !overflow ? expected : null)
                    && Equals(observed.StoredAfter, enable && !overflow ? expected : expectedBefore)
                    && observed.WriteDisposition == (!enable
                        ? InstructionWriteDisposition.SkippedDisabled
                        : overflow
                            ? InstructionWriteDisposition.FailedNoWrite
                            : InstructionWriteDisposition.WrittenChanged)
                    && !observed.Rounded && !observed.Clamped
                    && observed.Diagnostics.Count == (enable && overflow ? 1 : 0)
                    && (!enable || !overflow || observed.Diagnostics.Single().Code.StartsWith(
                        diagnosticCode, StringComparison.Ordinal)),
                    "ROUND/CEIL/FLOOR records exact EN, destination transition, disposition, overflow diagnostic, and stored result");
                Require(enable
                        ? source.Identity == "source"
                            && source.DeclaredType == PlcVariableType.Real
                            && source.Reason == ObservationUnavailableReason.None
                            && Equals(source.Value, expectedSource)
                        : source.Identity.Length == 0
                            && source.DeclaredType is null
                            && source.Reason == ObservationUnavailableReason.ActionDisabled
                            && source.Value is null,
                    "ROUND/CEIL/FLOOR records the exact sampled REAL source or disabled not-consumed state");
                Require(result == (enable && !overflow ? expected : expectedBefore),
                    "ROUND/CEIL/FLOOR stores the exact enabled, disabled, or overflow-retained result");
                var eno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_ROUNDING_STATE_PASS kind={kind} enable={(enable ? 1 : 0)} overflow={(overflow ? 1 : 0)} source={(enable ? expectedSource : double.NaN):R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(eno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture is "numeric-norm-tia" or "numeric-norm-range-tia" or "numeric-norm-invalid-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-norm"];
                var rangeFault = fixture == "numeric-norm-range-tia";
                var invalid = fixture == "numeric-norm-invalid-tia";
                var faulted = rangeFault || invalid;
                var result = numericEvidenceSnapshot.NumericVariables["result"];
                var expectedMinimum = rangeFault ? 30.0 : 10.0;
                var expectedMaximum = rangeFault ? 10.0 : 30.0;
                var expectedValue = invalid ? double.NaN : 20.0;
                var rangeType = invalid ? PlcVariableType.Real : PlcVariableType.DInt;
                var faultCode = rangeFault ? "VC_RUNTIME_RANGE" : invalid ? "VC_RUNTIME_NORM_X_INVALID" : null;
                RequireScalingObservation(observed, enable, faultCode, 0.5, rangeType, rangeType,
                    expectedMinimum, expectedValue, expectedMaximum, "NORM_X");
                Require(result == (enable && !faulted ? 0.5 : 7.0),
                    "NORM_X stores the exact enabled, disabled, range-fault, or invalid-input-retained result");
                var eno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_NORMALIZE_STATE_PASS enable={(enable ? 1 : 0)} rangeFault={(rangeFault ? 1 : 0)} invalid={(invalid ? 1 : 0)} min={expectedMinimum:R} value={expectedValue:R} max={expectedMaximum:R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(eno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture is "numeric-scale-tia" or "numeric-scale-range-tia" or "numeric-scale-overflow-tia" or "numeric-scale-invalid-tia")
            {
                var observed = numericEvidenceSnapshot.Observations!.GlobalLast["numeric-scale"];
                var rangeFault = fixture == "numeric-scale-range-tia";
                var overflow = fixture == "numeric-scale-overflow-tia";
                var invalid = fixture == "numeric-scale-invalid-tia";
                var faulted = rangeFault || overflow || invalid;
                var result = numericEvidenceSnapshot.NumericVariables["result"];
                var expectedMinimum = rangeFault ? 30.0 : overflow ? 0.0 : 10.0;
                var expectedValue = invalid ? double.NaN : overflow ? 2.0 : 0.5;
                var expectedMaximum = rangeFault ? 10.0 : overflow ? 2_000_000_000.0 : 30.0;
                var expectedBefore = overflow ? 12.0 : 7.0;
                var faultCode = rangeFault ? "VC_RUNTIME_RANGE"
                    : overflow ? "VC_RUNTIME_SCALE_X_OVERFLOW"
                    : invalid ? "VC_RUNTIME_SCALE_X_INVALID" : null;
                RequireScalingObservation(observed, enable, faultCode, 20.0, PlcVariableType.DInt, PlcVariableType.Real,
                    expectedMinimum, expectedValue, expectedMaximum, "SCALE_X", expectedBefore);
                Require(result == (enable && !faulted ? 20.0 : expectedBefore),
                    "SCALE_X stores the exact enabled, disabled, range-fault, or overflow-retained result");
                var eno = observed.NetworkEnable == true && observed.Diagnostics.Count == 0;
                GD.Print($"TIA_SCALE_STATE_PASS enable={(enable ? 1 : 0)} rangeFault={(rangeFault ? 1 : 0)} overflow={(overflow ? 1 : 0)} min={expectedMinimum:R} value={expectedValue:R} max={expectedMaximum:R} before={observed.DestinationBefore} calculated={observed.CalculatedResult ?? "not-consumed"} result={result:R} eno={(eno ? 1 : 0)} disposition={observed.WriteDisposition} diagnostics={observed.Diagnostics.Count}");
            }
            if (fixture == "text")
            {
                Require(snapshot.TextVariables["empty_text"] == "" && snapshot.TextVariables["formatted_text"] == "", "empty/failed STRING destinations");
                Require(Equals(capture.GlobalLast["text-format"].StoredAfter, "3.0"), "instruction-time successful formatted text");
                Require(capture.GlobalLast["text-fault"].WriteDisposition == InstructionWriteDisposition.FailedClearedDestination
                    && capture.GlobalLast["text-fault"].Diagnostics.Count > 0, "format failure diagnostics");
            }
            if (fixture == "flow")
            {
                Require(capture.GlobalLast["flow-branch-a"].LocalResult == (flowState == "mixed")
                    && !capture.GlobalLast["flow-branch-b"].LocalResult, "independent branch inputs match requested flow state");
                Require(capture.GlobalLast["flow-set"].NetworkEnable == (flowState == "mixed"),
                    "assignment-coil input flow matches the mixed or all-false rung state");
                Require(capture.GlobalLast["flow-edge"].LocalResult && !snapshot.Variables["lamp"], "edge reset progression");
                Require(capture.GlobalLast["flow-jump"].ControlOutcome == "Taken" && capture.GlobalLast["flow-return"].ControlOutcome == "Taken", "control transfer");
                Require(!capture.GlobalLast.ContainsKey("flow-skipped"), "skipped network must remain unobserved");
            }
            if (unknownTimerKind is not null)
            {
                Require(earlierCycleSnapshot?.Observations?.GlobalLast.ContainsKey("timer-unknown") == true,
                    "earlier scan evaluated the timer before the jump became true");
                Require(!capture.GlobalLast.ContainsKey("timer-unknown")
                    && capture.GlobalLast["timer-jump"].ControlOutcome == "Taken",
                    "current scan took JMP and did not evaluate the timer");
            }
            if (unknownCounterKind is not null)
            {
                Require(earlierCycleSnapshot?.Observations?.GlobalLast.ContainsKey("counter-unknown") == true,
                    "earlier scan evaluated the counter before the jump became true");
                Require(!capture.GlobalLast.ContainsKey("counter-unknown")
                    && capture.GlobalLast["counter-jump"].ControlOutcome == "Taken",
                    "current scan took JMP and did not evaluate the counter");
            }
            if (unknownComparison)
            {
                Require(earlierCycleSnapshot?.Observations?.GlobalLast.TryGetValue("compare-unknown", out var earlierComparison) == true
                    && earlierComparison.LocalResult == compareResult,
                    "earlier scan evaluated the comparator with the requested result before the jump became true");
                Require(!capture.GlobalLast.ContainsKey("compare-unknown")
                    && capture.GlobalLast["compare-jump"].ControlOutcome == "Taken",
                    "current scan took JMP and did not evaluate the comparator");
            }
            if (unknownProgramControl)
            {
                var earlierCapture = earlierCycleSnapshot?.Observations?.GlobalLast;
                Require(earlierCapture is not null
                    && earlierCapture.TryGetValue("jump-unknown", out var earlierJump)
                    && earlierJump.NetworkEnable == false
                    && earlierJump.ControlOutcome == "NotTaken"
                    && earlierCapture.TryGetValue("return-unknown", out var earlierReturn)
                    && earlierReturn.NetworkEnable == false
                    && earlierReturn.ControlOutcome == "NotTaken",
                    "earlier scan evaluated false JMP and RET as NotTaken before the gate jump became true");
                Require(!capture.GlobalLast.ContainsKey("jump-unknown")
                    && !capture.GlobalLast.ContainsKey("return-unknown")
                    && capture.GlobalLast["control-gate-jump"].ControlOutcome == "Taken"
                    && capture.GlobalLast.ContainsKey("control-label-coil"),
                    "current scan took the gate JMP, skipped both reviewed control instructions, and visited the LABEL destination");
            }
            if (unknownCall)
            {
                var earlierCapture = earlierCycleSnapshot?.Observations?.GlobalLast;
                Require(earlierCapture is not null
                    && earlierCapture.TryGetValue("call-a", out var earlierCall)
                    && earlierCall.NetworkEnable == true
                    && earlierCall.LocalResult
                    && earlierCall.CalledInstanceId == "instance-a"
                    && earlierCycleSnapshot!.CallInvocations.Count == 1
                    && earlierCycleSnapshot.CallInvocations[0].Completion == LadderCallCompletion.Completed
                    && earlierCycleSnapshot.CallInvocations[0].Transfers.Any(transfer => transfer.Phase == "CopyIn")
                    && earlierCycleSnapshot.CallInvocations[0].Transfers.Any(transfer => transfer.Phase == "CopyOut")
                    && earlierCycleSnapshot.CallInvocations[0].Transfers.Any(transfer => transfer.Phase == "AliasResolved")
                    && earlierCycleSnapshot.CallInvocations[0].Transfers.Any(transfer => transfer.Phase == "AliasFinal" && Equals(transfer.Value, 3.0)),
                    "earlier scan completed the exact FB instance with CopyIn, CopyOut, and InOut alias transfer evidence");
                Require(!capture.GlobalLast.ContainsKey("call-a")
                    && snapshot.CallInvocations.Count == 0
                    && capture.GlobalLast["call-gate-jump"].ControlOutcome == "Taken"
                    && capture.GlobalLast.ContainsKey("call-label-coil")
                    && snapshot.NumericVariables["result_a"] == 3.0
                    && snapshot.NumericVariables["shared_total"] == 3.0,
                    "current scan took the gate JMP, performed no call, visited the LABEL destination, and preserved prior call results");
            }
            if (numericSkipped)
            {
                var operation = skippedNumericOperation!;
                var earlierCapture = earlierCycleSnapshot?.Observations?.GlobalLast;
                Require(earlierCapture is not null && earlierCapture.ContainsKey(operation.Id),
                    "earlier scan captured the exact normal numeric instruction");
                var earlierNumeric = earlierCapture![operation.Id];
                Require(earlierNumeric.Evaluated
                    && earlierNumeric.NetworkEnable == true
                    && earlierNumeric.Diagnostics.Count == 0,
                    "earlier scan evaluated the exact normal numeric instruction without diagnostics");
                Require(!capture.GlobalLast.ContainsKey(operation.Id)
                    && capture.GlobalLast["numeric-skip-jump"].ControlOutcome == "Taken"
                    && capture.GlobalLast.ContainsKey("numeric-skip-label-coil"),
                    "current scan took JMP, omitted the numeric instruction, and visited the LABEL destination");
                Require(snapshot.NumericVariables.TryGetValue(earlierNumeric!.DestinationIdentity, out var retained)
                    && Equals(retained, Convert.ToDouble(earlierNumeric.StoredAfter, System.Globalization.CultureInfo.InvariantCulture)),
                    "skipped scan preserved the exact destination written by the earlier numeric execution");
            }
            if (fixture == "parameter-call")
            {
                if (enable)
                {
                    Require(snapshot.NumericVariables["result_a"] == 3.0 && snapshot.NumericVariables["result_b"] == 7.0
                        && snapshot.NumericVariables["shared_total"] == 10.0, "distinct instance sums and shared alias");
                    Require(snapshot.CallInvocations.Count == 2 && snapshot.CallInvocations.All(c => c.Completion == LadderCallCompletion.Completed
                        && c.Transfers.Any(t => t.Phase == "CopyIn") && c.Transfers.Any(t => t.Phase == "CopyOut")
                        && c.Transfers.Any(t => t.Phase == "AliasResolved") && c.Transfers.Any(t => t.Phase == "AliasFinal"))
                        && snapshot.CallInvocations.Single(c => c.CallId == "call-a").Transfers.Any(t => t.Phase == "AliasFinal" && Equals(t.Value, 3.0))
                        && snapshot.CallInvocations.Single(c => c.CallId == "call-b").Transfers.Any(t => t.Phase == "AliasFinal" && Equals(t.Value, 10.0)),
                        "actual phase-qualified parameter transfer records");
                }
                else
                {
                    Require(snapshot.NumericVariables["result_a"] == 0.0 && snapshot.NumericVariables["result_b"] == 0.0
                        && snapshot.NumericVariables["shared_total"] == 0.0,
                        "disabled CALL networks retain all caller destinations");
                    Require(snapshot.CallInvocations.Count == 0,
                        "disabled CALL networks do not perform parameter transfer or enter either instance");
                }
                GD.Print($"TIA_PARAMETER_CALL_STATE_PASS enable={(enable ? 1 : 0)} invocations={snapshot.CallInvocations.Count} resultA={snapshot.NumericVariables["result_a"]:R} resultB={snapshot.NumericVariables["result_b"]:R} shared={snapshot.NumericVariables["shared_total"]:R}");
            }
            var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
            if (fixture == "empty-box-tia")
            {
                EditableLadderNode HideOperands(EditableLadderNode node) => node with
                {
                    Variable = node.IsLeaf ? "???" : node.Variable,
                    Children = node.Children?.Select(HideOperands).ToArray(),
                };
                document.Rungs[0].Label = string.Empty;
                document.Rungs[0].HasTerminalAction = false;
                document.Rungs[0].HasEmptyInstructionPlaceholder = true;
                document.Rungs[0].SetRoot(HideOperands(document.Rungs[0].Root));
            }
            // These review drafts are intentionally not executable. Compile a
            // valid fixture for controller setup, then draw the editor's actual
            // untyped/missing-operand state without leaking typed scan values.
            var untypedCounterReview = fixture == "counter-untyped-tia";
            if (untypedCounterReview) document.Rungs[0].CounterValueType = LadderCounterValueType.Unspecified;
            var unassignedTimerReview = fixture == "timer-unassigned-tia";
            if (unassignedTimerReview)
            {
                document.Rungs[0].TimerPresetAssigned = false;
                document.Rungs[0].TimerPresetOperand = string.Empty;
            }
            var block = Option("--review-block", "");
            if (block.Length > 0)
            {
                var index = document.Blocks.FindIndex(b => b.Id == block || b.Name == block);
                if (index < 0) throw new ArgumentException("Unknown review block: " + block);
                document.SelectBlock(index);
            }
            var overlay = new CanvasLayer { Name = "EditorMonitoringReview", Layer = 100 };
            var background = new PanelContainer(); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            overlay.AddChild(background); AddChild(overlay);
            var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            background.AddChild(scroll);
            var canvas = new LadderEditorCanvas(style == "logix" ? LadderVendorStyle.RockwellLogix : LadderVendorStyle.SiemensTia, document);
            T EnumOption<T>(string name, T fallback) where T : struct, Enum =>
                Enum.TryParse<T>(Option(name, string.Empty), true, out var parsed) && Enum.IsDefined(parsed) ? parsed : fallback;
            canvas.SetTiaDisplaySettings(
                EnumOption("--review-operand", TiaOperandRepresentation.Symbolic),
                EnumOption("--review-tag-info", TiaTagInformationDisplay.Hidden),
                EnumOption("--review-tag-position", TiaTagInformationPosition.OnOperands));
            var rungIndex = int.Parse(Option("--review-rung", "0"));
            if (rungIndex < 0 || rungIndex >= document.Rungs.Count) throw new ArgumentOutOfRangeException("--review-rung");
            scroll.AddChild(canvas);
            scroll.GetVScrollBar().ValueChanged += _ => canvas.QueueRedraw();
            scroll.GetHScrollBar().ValueChanged += _ => canvas.QueueRedraw();
            canvas.SetZoomPercent(int.Parse(Option("--review-zoom", "100")));
            if (fixture == "empty-box-tia")
            {
                var lead = canvas.GetTerminalInputLeadLength(rungIndex);
                Require(Math.Abs(lead - 80.0f) < 0.1,
                    "empty TIA instruction has an 80 logical px straight conductor after the branch merge");
                GD.Print($"TIA_EMPTY_BOX_INPUT_LEAD_PASS length={lead:0.0} logical px");
            }
            if (style == "tia" && (document.Rungs[rungIndex].IsCounter || document.Rungs[rungIndex].IsTimer))
            {
                var wireClearance = canvas.GetTerminalConductorClearance(rungIndex);
                Require(Math.Abs(wireClearance.X) < 0.1 && Math.Abs(wireClearance.Y) < 0.1,
                    "IEC timer/counter rung conductors meet both instruction body edges without gaps or overlap");
                GD.Print($"TIA_TERMINAL_WIRE_CONTINUITY_PASS left={wireClearance.X:0.0} right={wireClearance.Y:0.0}");
            }
            if (fixture is "counter-tia" or "counter-reset-tia" or "counter-down-tia" && style == "tia" && int.Parse(Option("--review-zoom", "100")) == 100)
            {
                canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);
                var counterBounds = canvas.GetSelectionBounds();
                Require(Math.Abs(counterBounds.Size.X - 126) < 0.1 && Math.Abs(counterBounds.Size.Y - 128) < 0.1,
                    "TIA counter body matches the supplied 126x128 reference crop at 100% zoom");
                GD.Print(fixture == "counter-down-tia"
                    ? $"TIA_CTD_GEOMETRY_PASS width={counterBounds.Size.X:0} height={counterBounds.Size.Y:0}"
                    : $"TIA_CTU_GEOMETRY_PASS width={counterBounds.Size.X:0} height={counterBounds.Size.Y:0}");
                canvas.ClearElementSelection();
            }
            if (fixture == "timer-tonr" && style == "tia" && int.Parse(Option("--review-zoom", "100")) == 100)
            {
                canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);
                var terminalBounds = canvas.GetSelectionBounds();
                Require(terminalBounds.Size.Y >= 158,
                    "TONR instruction body and input rows contain separate R and PT operands");
                canvas.ClearElementSelection();
                GD.Print($"TIA_TIMER_GEOMETRY_PASS fixture=timer-tonr height={terminalBounds.Size.Y:0}");
            }
            if (fixture == "numeric-move-tia" && style == "tia" && int.Parse(Option("--review-zoom", "100")) == 100)
            {
                canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);
                var moveBounds = canvas.GetSelectionBounds();
                Require(Math.Abs(moveBounds.Size.X - 132) < 0.1 && Math.Abs(moveBounds.Size.Y - 128) < 0.1,
                    "TIA MOVE body contains separate EN/ENO and IN/OUT1 rows at the reviewed 100% scale");
                canvas.ClearElementSelection();
                GD.Print($"TIA_MOVE_GEOMETRY_PASS width={moveBounds.Size.X:0} height={moveBounds.Size.Y:0}");
            }
            if (fixture is "numeric-add-tia" or "numeric-add-overflow-tia"
                    or "numeric-sub-tia" or "numeric-sub-overflow-tia"
                    or "numeric-mul-tia" or "numeric-mul-overflow-tia"
                    or "numeric-div-tia" or "numeric-div-zero-tia" or "numeric-mod-tia" or "numeric-mod-zero-tia"
                && style == "tia" && int.Parse(Option("--review-zoom", "100")) == 100)
            {
                canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);
                var arithmeticBounds = canvas.GetSelectionBounds();
                Require(Math.Abs(arithmeticBounds.Size.X - 132) < 0.1 && Math.Abs(arithmeticBounds.Size.Y - 96) < 0.1,
                    "TIA ADD/SUB body fits the title/type, EN/ENO, and two operand rows at the reviewed scale");
                canvas.ClearElementSelection();
                GD.Print($"TIA_ARITHMETIC_GEOMETRY_PASS fixture={fixture} width={arithmeticBounds.Size.X:0} height={arithmeticBounds.Size.Y:0} inputPitch={TiaNumericInstructionPorts.InputRowPitch(document.Rungs[rungIndex].NumericOperationKind):0}");
            }
            if (fixture is "numeric-expt-tia" or "numeric-expt-domain-tia" or "numeric-expt-overflow-tia"
                && style == "tia" && int.Parse(Option("--review-zoom", "100")) == 100)
            {
                canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);
                var exptBounds = canvas.GetSelectionBounds();
                Require(Math.Abs(exptBounds.Size.X - 152) < 0.1 && Math.Abs(exptBounds.Size.Y - 112) < 0.1,
                    "TIA EXPT body separates two monitored operand rows at the reviewed 100% scale");
                canvas.ClearElementSelection();
                GD.Print($"TIA_EXPT_GEOMETRY_PASS width={exptBounds.Size.X:0} height={exptBounds.Size.Y:0} inputPitch={TiaNumericInstructionPorts.InputRowPitch(LadderNumericOperationKind.Exponentiate):0}");
            }
            if (fixture == "program-control-tia" && style == "tia" && document.Rungs[rungIndex].IsLabel
                && int.Parse(Option("--review-zoom", "100")) == 100)
            {
                canvas.SelectNetworkLabel(rungIndex, notify: false);
                var labelBounds = canvas.GetSelectionBounds();
                Require(Math.Abs(labelBounds.Position.X - 50) < 0.1
                    && Math.Abs(labelBounds.Size.X - 62) < 0.1
                    && Math.Abs(labelBounds.Size.Y - 24) < 0.1,
                    "TIA LABEL is a compact marker next to the left rail, not a terminal EN/ENO box");
                var executable = document.BuildProgram().Blocks![document.ActiveBlockIndex].Networks[rungIndex];
                Require(executable.LabelInstruction is not null && executable.Coil is not null,
                    "TIA LABEL marker coexists with the destination network's terminal action");
                GD.Print($"TIA_LABEL_MARKER_GEOMETRY_PASS bounds={labelBounds}");
                canvas.ClearElementSelection();
            }
            if (!untypedCounterReview && !unassignedTimerReview && reviewMonitor == "online")
            {
                if (earlierCycleSnapshot is not null)
                {
                    var reviewSession = Guid.NewGuid();
                    var reviewLoad = Guid.NewGuid();
                    ControllerPublication ReviewPublication(VirtualControllerSnapshot value, long attempt)
                    {
                        var origin = new ObservationOrigin(reviewSession, reviewLoad, 1, attempt);
                        return new(reviewSession, runtime.ProgramContentId, reviewLoad, 1, attempt, value.ScanNumber, attempt,
                            ControllerPublicationKind.ScanCompleted, ControllerCompletion.Completed, ControllerPhaseCompletion.ProgramExecuted,
                            origin, value.Observations!.WithOrigin(origin), value);
                    }
                    canvas.SetMonitorPublication(ReviewPublication(earlierCycleSnapshot, 1));
                    canvas.SetMonitorPublication(ReviewPublication(snapshot, 2));
                    canvas.FlushPendingMonitoringForReview();
                }
                else canvas.SetMonitorSnapshot(snapshot);
            }
            if (fixture == "flow" && style == "tia" && reviewMonitor == "online")
            {
                var satisfiedStyle = canvas.ReviewPowerFlowStyle(true);
                var unsatisfiedStyle = canvas.ReviewPowerFlowStyle(false);
                var unknownStyle = canvas.ReviewPowerFlowStyle(null);
                var notInterconnectedStyle = canvas.ReviewPowerFlowStyle(TiaPowerFlowStatus.NotInterconnected);
                Require(satisfiedStyle == ("00a651", false)
                    && unsatisfiedStyle == ("233db8", true)
                    && unknownStyle == ("87949a", false)
                    && notInterconnectedStyle == ("000000", false),
                    "TIA power-flow styles distinguish satisfied, unsatisfied, unknown, and not-interconnected states");
                if (rungIndex == 0)
                {
                    var branchA = canvas.ReviewPowerFlowForElement(rungIndex, "flow-branch-a");
                    var branchB = canvas.ReviewPowerFlowForElement(rungIndex, "flow-branch-b");
                    Require(branchA == (true, flowState == "mixed") && branchB == (true, false),
                        "evaluated parallel legs retain independent satisfied and unsatisfied states");
                }
                if (rungIndex == 3)
                {
                    foreach (var id in new[] { "flow-skipped-enable", "flow-skipped-a", "flow-skipped-b" })
                        Require(canvas.ReviewPowerFlowForElement(rungIndex, id) == (null, null),
                            "JMP-skipped network element remains unknown rather than false: " + id);
                    var earlierValue = canvas.ReviewObservationValueStyle("flow-skipped-a", "no", "008f4c");
                    Require(earlierValue == (true, "488f6d"),
                        "JMP-skipped contact retains its earlier-cycle value at deterministic 50% saturation");
                    GD.Print($"TIA_EARLIER_CYCLE_VALUE_STYLE_PASS id=flow-skipped-a color={earlierValue.Color} saturation=50% flow=gray/unknown");
                }
                GD.Print($"TIA_POWER_FLOW_STYLE_PASS rung={rungIndex} state={(rungIndex == 3 ? "unknown" : flowState)} satisfied={satisfiedStyle.Color}/solid unsatisfied={unsatisfiedStyle.Color}/dashed unknown={unknownStyle.Color}/solid notInterconnected={notInterconnectedStyle.Color}/solid");
            }
            if (fixture == "branch-nested" && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex == 0, "nested branch review has one network");
                Require(canvas.ReviewPowerFlowForElement(0, "branch-nested-lead") == (true, true)
                    && canvas.ReviewPowerFlowForElement(0, "branch-upper") == (true, true)
                    && canvas.ReviewPowerFlowForElement(0, "branch-inner-a") == (true, false)
                    && canvas.ReviewPowerFlowForElement(0, "branch-inner-b") == (true, true)
                    && canvas.ReviewPowerFlowForElement(0, "branch-inner-parallel") == (true, true)
                    && canvas.ReviewPowerFlowForElement(0, "branch-lower") == (true, false)
                    && canvas.ReviewPowerFlowForElement(0, "branch-outer-parallel") == (true, true)
                    && canvas.ReviewPowerFlowForElement(0, "branch-nested-tail") == (true, true)
                    && canvas.ReviewTerminalPowerFlow(0) == (true, true),
                    "nested branch projection preserves each leg, both OR joins, trailing series continuity, and terminal flow");
                GD.Print("TIA_NESTED_BRANCH_FLOW_PASS inner=false|true->true outer=true|false->true terminal=true");
            }
            if (unknownTimerKind is { } unknownKind && style == "tia" && reviewMonitor == "online")
            {
                var unknownCatalogKey = unknownKind switch
                {
                    LadderTimerKind.OffDelay => "tof",
                    LadderTimerKind.Pulse => "tp",
                    LadderTimerKind.RetentiveOnDelay => "rto",
                    _ => "ton",
                };
                Require(rungIndex == 1, "timer unknown-state review must capture the skipped timer network");
                Require(canvas.ReviewPowerFlowForElement(rungIndex, "timer-unknown-enable") == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped timer contact, input, and Q output all remain unknown rather than false");
                var view = canvas.InstructionPresentationFor("timer-unknown", unknownCatalogKey);
                var expectedElapsed = unknownKind == LadderTimerKind.OffDelay ? "0 ms" : "10 ms";
                var expectedDone = unknownKind is LadderTimerKind.OffDelay or LadderTimerKind.Pulse ? "TRUE" : "FALSE";
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Elapsed" && row.Value == expectedElapsed)
                    && view.Rows.Any(row => row.Label == "Done" && row.Value == expectedDone)
                    && (unknownKind != LadderTimerKind.RetentiveOnDelay
                        || view.Rows.Any(row => row.Label == "Consumed R" && row.Value.StartsWith("FALSE", StringComparison.Ordinal))),
                    "skipped timer retains its earlier ET/Q/R state without claiming current execution");
                GD.Print($"TIA_TIMER_UNKNOWN_STATE_PASS kind={unknownTimerMnemonic} flow=gray/unknown value=earlier-cycle/50% elapsed={expectedElapsed.Replace(" ", "")} q={expectedDone} currentExecution=false");
            }
            if (unknownCounterKind is { } unknownCounter && style == "tia" && reviewMonitor == "online")
            {
                var catalogKey = unknownCounter == LadderCounterKind.CountDown ? "ctd" : "ctu";
                var mnemonic = unknownCounter == LadderCounterKind.CountDown ? "CTD" : "CTU";
                var expectedCount = unknownCounter == LadderCounterKind.CountDown ? "3" : "1";
                Require(rungIndex == 1, "counter unknown-state review must capture the skipped counter network");
                Require(canvas.ReviewPowerFlowForElement(rungIndex, "counter-unknown-enable") == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped counter contact, input, and Q output all remain unknown rather than false");
                var view = canvas.InstructionPresentationFor("counter-unknown", catalogKey);
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Current count" && row.Value == expectedCount)
                    && view.Rows.Any(row => row.Label == "Done" && row.Value == "FALSE")
                    && (unknownCounter != LadderCounterKind.CountDown
                        || view.Rows.Any(row => row.Label == "Consumed LD" && row.Value.StartsWith("TRUE", StringComparison.Ordinal))),
                    "skipped counter retains its earlier CV/Q/LD state without claiming current execution");
                GD.Print($"TIA_COUNTER_UNKNOWN_STATE_PASS kind={mnemonic} flow=gray/unknown value=earlier-cycle/50% cv={expectedCount} q=FALSE currentExecution=false");
            }
            if (unknownContact && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex is 1 or 2,
                    "contact unknown-state review must capture the skipped NO or NC network");
                var normallyClosed = rungIndex == 2;
                var instructionId = normallyClosed ? "contact-nc-unknown" : "contact-no-unknown";
                var catalogKey = normallyClosed ? "nc" : "no";
                var identity = normallyClosed ? "nc_signal" : "no_signal";
                var expectedOperand = normallyClosed ? "FALSE (BOOL)" : "TRUE (BOOL)";
                Require(canvas.ReviewPowerFlowForElement(rungIndex, instructionId) == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped contact input/output and terminal coil all remain unknown rather than false");
                var view = canvas.InstructionPresentationFor(instructionId, catalogKey);
                var valueStyle = canvas.ReviewObservationValueStyle(instructionId, catalogKey, "008f4c");
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Consumed A" && row.Identity == identity && row.Value == expectedOperand)
                    && view.Rows.Any(row => row.Label == "Local result" && row.Value == "TRUE")
                    && valueStyle == (true, "488f6d"),
                    "skipped contact retains its exact earlier BOOL operand and result at half saturation without claiming current execution");
                GD.Print($"TIA_CONTACT_UNKNOWN_STATE_PASS kind={(normallyClosed ? "NC" : "NO")} flow=gray/unknown value=earlier-cycle/50% operand={(normallyClosed ? 0 : 1)} result=1 currentExecution=false labelVisited=true");
            }
            if (unknownEdge && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex is 1 or 2,
                    "edge unknown-state review must capture the skipped P or N network");
                var falling = rungIndex == 2;
                var instructionId = falling ? "edge-falling-unknown" : "edge-rising-unknown";
                var catalogKey = falling ? "edge-falling" : "edge-rising";
                var signalIdentity = falling ? "falling_signal" : "rising_signal";
                var memoryIdentity = falling ? "falling_memory" : "rising_memory";
                var expectedSignal = falling ? "FALSE (BOOL)" : "TRUE (BOOL)";
                var expectedPrevious = falling ? "TRUE" : "FALSE";
                var expectedCurrent = falling ? "FALSE" : "TRUE";
                var node = EditableLadderTree.Find(document.Rungs[rungIndex].Root, instructionId);
                Require(node?.EdgeMemoryOperand == memoryIdentity,
                    "skipped edge contact retains the exact dedicated edge-memory operand below the symbol");
                Require(canvas.ReviewPowerFlowForElement(rungIndex, instructionId) == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped edge input/output and terminal coil all remain unknown rather than false");
                var view = canvas.InstructionPresentationFor(instructionId, catalogKey);
                var valueStyle = canvas.ReviewObservationValueStyle(instructionId, catalogKey, "008f4c");
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Consumed A" && row.Identity == signalIdentity && row.Value == expectedSignal)
                    && view.Rows.Any(row => row.Label == "Previous box input" && row.Value == expectedPrevious)
                    && view.Rows.Any(row => row.Label == "Box input" && row.Value == expectedCurrent)
                    && view.Rows.Any(row => row.Label == "Local result" && row.Value == "TRUE")
                    && view.Rows.Any(row => row.Label == "Effect" && row.Value == "State evaluated — changed")
                    && valueStyle == (true, "488f6d"),
                    "skipped edge contact retains its exact earlier transition and pulse at half saturation without claiming current execution");
                GD.Print($"TIA_EDGE_UNKNOWN_STATE_PASS kind={(falling ? "N" : "P")} flow=gray/unknown value=earlier-cycle/50% previous={(falling ? 1 : 0)} current={(falling ? 0 : 1)} result=1 memory={memoryIdentity} currentExecution=false labelVisited=true");
            }
            if (unknownCoil && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex is 1 or 2 or 3,
                    "coil unknown-state review must capture the skipped assignment, Set, or Reset network");
                var (contactId, coilId, catalogKey, destination, before, after, mnemonic) = rungIndex switch
                {
                    1 => ("assign-unknown-contact", "assign-unknown-coil", "coil", "assign_unknown_out", "FALSE", "TRUE", "Assign"),
                    2 => ("set-unknown-contact", "set-unknown-coil", "set", "set_unknown_out", "FALSE", "TRUE", "Set"),
                    _ => ("reset-unknown-contact", "reset-unknown-coil", "reset", "reset_unknown_out", "TRUE", "FALSE", "Reset"),
                };
                Require(canvas.ReviewPowerFlowForElement(rungIndex, contactId) == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped coil-network contact and terminal flow remain unknown rather than false");
                var view = canvas.InstructionPresentationFor(coilId, catalogKey);
                var valueStyle = canvas.ReviewObservationValueStyle(coilId, catalogKey, "008f4c");
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Network enable" && row.Value == "TRUE")
                    && view.Rows.Any(row => row.Label == "Before" && row.Identity == destination && row.Value == before)
                    && view.Rows.Any(row => row.Label == "Stored after" && row.Identity == destination && row.Value == after)
                    && view.Rows.Any(row => row.Label == "Effect" && row.Value == "Written — value changed")
                    && valueStyle == (true, "488f6d"),
                    "skipped coil retains its exact earlier destination transition at half saturation without claiming current execution");
                GD.Print($"TIA_COIL_UNKNOWN_STATE_PASS kind={mnemonic} flow=gray/unknown value=earlier-cycle/50% before={before} after={after} destination={destination} currentExecution=false labelVisited=true");
            }
            if (unknownComparison && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex == 1, "comparison unknown-state review must capture the skipped comparator network");
                Require(canvas.ReviewPowerFlowForElement(rungIndex, "compare-unknown") == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped comparator input, output, and terminal coil all remain unknown rather than false");
                var catalogKey = "compare-" + (int)reviewCompareOperator;
                var view = canvas.InstructionPresentationFor("compare-unknown", catalogKey);
                var expectedLeft = ((long)compareOperands.Left).ToString(System.Globalization.CultureInfo.InvariantCulture) + " (DINT)";
                var expectedRight = ((long)compareOperands.Right).ToString(System.Globalization.CultureInfo.InvariantCulture) + " (DINT)";
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Consumed A" && row.Identity == "pc_heartbeat" && row.Value == expectedLeft)
                    && view.Rows.Any(row => row.Label == "Consumed B" && row.Identity == "last_heartbeat" && row.Value == expectedRight)
                    && view.Rows.Any(row => row.Label == "Local result" && row.Value == (compareResult ? "TRUE" : "FALSE")),
                    "skipped comparator retains its exact earlier operands and result without claiming current execution");
                GD.Print($"TIA_COMPARE_UNKNOWN_STATE_PASS op={reviewCompareOperator} flow=gray/unknown value=earlier-cycle/50% left={compareOperands.Left:R} right={compareOperands.Right:R} result={(compareResult ? 1 : 0)} currentExecution=false");
            }
            if (numericSkipped && style == "tia" && reviewMonitor == "online")
            {
                var operation = skippedNumericOperation!;
                var catalogKey = "numeric-" + (int)operation.Kind;
                var earlierNumeric = earlierCycleSnapshot!.Observations!.GlobalLast[operation.Id];
                Require(rungIndex == 1, "numeric unknown-state review must capture the JMP-skipped instruction network");
                Require(canvas.ReviewPowerFlowForElement(rungIndex, skippedNumericEnableId!) == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null)
                    && canvas.ReviewNumericInstructionEnableFlow(rungIndex) == (null, null),
                    "JMP-skipped numeric contact, EN, and ENO flow remain unknown rather than false");
                var view = canvas.InstructionPresentationFor(operation.Id, catalogKey);
                bool RetainsOperand(InstructionOperand operand)
                {
                    if (operand.Reason != ObservationUnavailableReason.None) return false;
                    var role = operation.Kind is LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale
                        ? operand.Role switch { "A" => "MIN", "B" => "VALUE", "C" => "MAX", _ => operand.Role }
                        : operand.Role;
                    var expected = LadderInstructionPresentation.Value(operand.Value)
                        + (operand.DeclaredType is { } type ? " (" + type.ToString().ToUpperInvariant() + ")" : "");
                    return view.Rows.Any(row => row.Label == "Consumed " + role
                        && row.Value == expected && row.Identity == operand.Identity);
                }
                var earlierValue = canvas.ReviewObservationValueStyle(operation.Id, catalogKey, "008f4c");
                Require(view.EarlierCycle && !view.CurrentExecution
                    && earlierNumeric.Operands.Where(operand => operand.Reason != ObservationUnavailableReason.NotApplicable).All(RetainsOperand)
                    && view.Rows.Any(row => row.Label == "Before"
                        && row.Value == LadderInstructionPresentation.Value(earlierNumeric.DestinationBefore)
                        && row.Identity == earlierNumeric.DestinationIdentity)
                    && view.Rows.Any(row => row.Label == "Stored after"
                        && row.Value == LadderInstructionPresentation.Value(earlierNumeric.StoredAfter)
                        && row.Identity == earlierNumeric.DestinationIdentity)
                    && earlierValue == (true, "488f6d"),
                    "skipped numeric instruction retains exact earlier operands and destination transition at half saturation without claiming current execution");
                GD.Print($"TIA_NUMERIC_UNKNOWN_STATE_PASS key={catalogKey} kind={operation.Kind} flow=gray/unknown value=earlier-cycle/50% operands={earlierNumeric.Operands.Count(operand => operand.Reason != ObservationUnavailableReason.NotApplicable)} destination={earlierNumeric.DestinationIdentity} currentExecution=false labelVisited=true");
            }
            if (unknownProgramControl && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex is 1 or 2,
                    "program-control unknown-state review must capture the skipped JMP or RET network");
                var instructionId = rungIndex == 1 ? "jump-unknown" : "return-unknown";
                var contactId = rungIndex == 1 ? "jump-unknown-enable" : "return-unknown-enable";
                var catalogKey = rungIndex == 1 ? "jump" : "return";
                Require(canvas.ReviewPowerFlowForElement(rungIndex, contactId) == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped program-control contact and terminal flow remain unknown rather than false");
                var view = canvas.InstructionPresentationFor(instructionId, catalogKey);
                var earlierContactValue = canvas.ReviewObservationValueStyle(contactId, "no", "008f4c");
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Network enable" && row.Value == "FALSE")
                    && view.Rows.Any(row => row.Label == "Control" && row.Value == "NotTaken")
                    && earlierContactValue == (true, "488f6d"),
                    "skipped program-control instruction retains exact earlier NotTaken state and half-saturation input value without claiming current execution");
                GD.Print($"TIA_PROGRAM_CONTROL_UNKNOWN_STATE_PASS kind={(rungIndex == 1 ? "JMP" : "RET")} flow=gray/unknown value=earlier-cycle/50% earlierRlo=FALSE earlierOutcome=NotTaken currentExecution=false labelVisited=true");
            }
            if (unknownCall && style == "tia" && reviewMonitor == "online")
            {
                Require(rungIndex == 1, "CALL unknown-state review must capture the skipped FB call network");
                Require(canvas.ReviewPowerFlowForElement(rungIndex, "call-enable-a") == (null, null)
                    && canvas.ReviewTerminalPowerFlow(rungIndex) == (null, null),
                    "JMP-skipped CALL contact, EN, and ENO flow remain unknown rather than false");
                var view = canvas.InstructionPresentationFor("call-a", "call");
                var earlierCallValue = canvas.ReviewObservationValueStyle("call-a", "call", "008f4c");
                Require(view.EarlierCycle && !view.CurrentExecution
                    && view.Rows.Any(row => row.Label == "Network enable" && row.Value == "TRUE")
                    && view.Rows.Any(row => row.Label == "Consumed CopyIn:input" && row.Value == "3 (REAL)" && row.Identity == "literal")
                    && view.Rows.Any(row => row.Label == "Consumed CopyOut:output" && row.Value == "3 (REAL)" && row.Identity == "result_a")
                    && view.Rows.Any(row => row.Label == "Consumed AliasResolved:shared" && row.Value == "0 (REAL)" && row.Identity == "shared_total")
                    && view.Rows.Any(row => row.Label == "Consumed AliasFinal:shared" && row.Value == "3 (REAL)" && row.Identity == "shared_total")
                    && view.Rows.Any(row => row.Label == "Called instance" && row.Value == "instance-a")
                    && earlierCallValue == (true, "488f6d"),
                    "skipped CALL retains the exact earlier enabled instance presentation at half saturation without claiming current execution");
                GD.Print("TIA_CALL_UNKNOWN_STATE_PASS instance=instance-a flow=gray/unknown value=earlier-cycle/50% earlierEn=TRUE earlierCompletion=Completed transfers=CopyIn,CopyOut,AliasResolved,AliasFinal currentInvocations=0 currentExecution=false labelVisited=true");
            }
            var selectedNode = Option("--review-node", "");
            var reviewSelection = Option("--review-select", "none");
            if (selectedNode.Length > 0)
            {
                Require(EditableLadderTree.Find(document.Rungs[rungIndex].Root, selectedNode) is not null, "selected node exists");
                canvas.SelectTreeNode(rungIndex, selectedNode, notify: false);
                if (fixture == "branch-nested" && selectedNode == "branch-inner-parallel")
                {
                    var nestedBounds = canvas.GetSelectionBounds();
                    Require(canvas.SelectedNodeId == selectedNode && !canvas.NetworkSelected
                        && !canvas.OutputSelected && !canvas.NetworkLabelSelected,
                        "nested branch selection targets only the inner parallel topology");
                    Require(nestedBounds.Size.X > 120 && nestedBounds.Size.Y > 70,
                        "nested branch selection contains both inner legs rather than one contact");
                    GD.Print($"TIA_NESTED_BRANCH_SELECTION_PASS id={selectedNode} bounds={nestedBounds}");
                }
                if (fixture is "compare-tia" or "compare-tia-inferred")
                {
                    var selector = canvas.GetNode<OptionButton>("TiaComparisonValueTypeSelector");
                    Require(selector.Visible, "selected comparator shows the TIA type selector");
                    Require(selector.ItemCount == 4
                        && selector.GetItemText(0) == "???"
                        && selector.GetItemText(1) == "Int"
                        && selector.GetItemText(2) == "DInt"
                        && selector.GetItemText(3) == "Real",
                        "comparator selector exposes only the documented simulator-supported choices in TIA order");
                    Require(selector.Selected == 2, "explicit or tag-inferred DInt is reflected in the selected comparator type field");
                    GD.Print("COMPARATOR_TYPE_SELECTOR_PASS selected=" + selector.GetItemText(selector.Selected)
                        + " choices=" + string.Join(',', Enumerable.Range(0, selector.ItemCount).Select(selector.GetItemText)));
                }
            }
            else if (reviewSelection == "output") canvas.SelectElement(rungIndex, -1, -1, true);
            else if (reviewSelection == "label") canvas.SelectNetworkLabel(rungIndex);
            else if (reviewSelection == "rung") canvas.SelectRung(rungIndex);
            else if (reviewSelection == "branch")
            {
                var root = document.Rungs[rungIndex].Root;
                Require(root.Kind == LadderNodeKind.Parallel,
                    "branch review selection requires a parallel root node");
                canvas.SelectTreeNode(rungIndex, root.Id);
            }
            else if (reviewSelection != "none")
                throw new ArgumentException("Review selection must be 'none', 'output', 'label', 'rung', or 'branch'.");
            if (reviewSelection == "none" && selectedNode.Length == 0)
                Require(!canvas.NetworkSelected, "an unselected review fixture must not inherit network selection from geometry checks");
            if (reviewSelection == "branch")
            {
                var root = document.Rungs[rungIndex].Root;
                var branchBounds = canvas.GetSelectionBounds();
                var outputCenter = canvas.GetElementPosition(rungIndex, -1, -1, output: true);
                Require(canvas.SelectedNodeId == root.Id && !canvas.NetworkSelected
                    && !canvas.OutputSelected && !canvas.NetworkLabelSelected,
                    "branch selection targets only the root parallel topology");
                Require(branchBounds.Size.X > 240 && branchBounds.Size.Y > 136,
                    "branch selection contains multiple legs rather than one child instruction");
                Require(branchBounds.Position.X <= 49 && branchBounds.End.X < outputCenter.X,
                    "root branch selection begins at the TIA left rail and stops before the terminal action");
                GD.Print($"TIA_BRANCH_SELECTION_PASS id={root.Id} bounds={branchBounds} outputX={outputCenter.X:0.0}");
            }
            if (reviewSelection == "rung")
            {
                var networkBounds = canvas.GetTiaNetworkSelectionBounds(rungIndex);
                Require(canvas.NetworkSelected && canvas.SelectedRung == rungIndex
                    && string.IsNullOrEmpty(canvas.SelectedNodeId)
                    && !canvas.OutputSelected && !canvas.NetworkLabelSelected,
                    "network selection does not also select an instruction or label");
                Require(networkBounds.Position.X >= 21 && networkBounds.Position.Y >= 89
                    && networkBounds.Size.X > 700 && networkBounds.Size.Y >= 320,
                    "network selection border contains the header, comment row, rail, and ladder body");
                GD.Print($"TIA_NETWORK_SELECTION_PASS rung={rungIndex + 1} bounds={networkBounds}");
            }
            if (fixture == "numeric-move-tia" && selectedNode.Length == 0
                && reviewSelection == "output")
            {
                Require(!canvas.GetNode<OptionButton>("TiaNumericDataTypeSelector").Visible
                    && !canvas.GetNode<OptionButton>("TiaConvertInputTypeSelector").Visible
                    && !canvas.GetNode<OptionButton>("TiaConvertOutputTypeSelector").Visible,
                    "selected MOVE does not invent a type selector absent from the official LAD example");
                GD.Print("TIA_MOVE_SELECTION_PASS typeSelectors=hidden");
            }
            if ((fixture is "numeric-convert-tia" or "numeric-trunc-tia" or "numeric-trunc-overflow-tia"
                    or "numeric-norm-tia" or "numeric-scale-tia"
                    or "numeric-round-tia" or "numeric-round-overflow-tia"
                    or "numeric-ceil-tia" or "numeric-ceil-overflow-tia"
                    or "numeric-floor-tia" or "numeric-floor-overflow-tia")
                && selectedNode.Length == 0 && reviewSelection == "output")
            {
                var inputSelector = canvas.GetNode<OptionButton>("TiaConvertInputTypeSelector");
                var outputSelector = canvas.GetNode<OptionButton>("TiaConvertOutputTypeSelector");
                var instructionBounds = canvas.GetSelectionBounds();
                Require(inputSelector.Visible && outputSelector.Visible,
                    "selected dual-type instruction exposes independent input and output type controls");
                var normalize = fixture == "numeric-norm-tia";
                var scale = fixture == "numeric-scale-tia";
                var truncate = fixture is "numeric-trunc-tia" or "numeric-trunc-overflow-tia";
                var rounding = fixture is "numeric-round-tia" or "numeric-round-overflow-tia"
                    or "numeric-ceil-tia" or "numeric-ceil-overflow-tia"
                    or "numeric-floor-tia" or "numeric-floor-overflow-tia";
                Require(normalize
                        ? inputSelector.ItemCount == 4 && outputSelector.ItemCount == 2
                            && inputSelector.Selected == 2 && outputSelector.Selected == 1
                        : scale || rounding
                        ? inputSelector.ItemCount == 2 && outputSelector.ItemCount == 4
                            && inputSelector.Selected == 1 && outputSelector.Selected == 2
                        : truncate
                        ? inputSelector.ItemCount == 2 && outputSelector.ItemCount == 4
                            && inputSelector.Selected == 1 && outputSelector.Selected == 1
                        : inputSelector.ItemCount == 4 && outputSelector.ItemCount == 4
                            && inputSelector.Selected == 1 && outputSelector.Selected == 2,
                    "dual type controls expose and select the instruction-specific supported subset");
                GD.Print($"DUAL_TYPE_CONTROL_GEOMETRY body={instructionBounds} input={new Rect2(inputSelector.Position, inputSelector.Size)} output={new Rect2(outputSelector.Position, outputSelector.Size)}");
                Require(instructionBounds.HasPoint(inputSelector.Position + Vector2.One)
                    && instructionBounds.HasPoint(inputSelector.Position + inputSelector.Size - Vector2.One)
                    && instructionBounds.HasPoint(outputSelector.Position + Vector2.One)
                    && instructionBounds.HasPoint(outputSelector.Position + outputSelector.Size - Vector2.One),
                    "both selected type controls remain inside their instruction body");
                Require(inputSelector.Position.X + inputSelector.Size.X <= outputSelector.Position.X,
                    "dual type controls do not overlap and preserve the Siemens 'to' separator gap");
                Require(inputSelector.Position.Y + inputSelector.Size.Y < instructionBounds.GetCenter().Y
                    && outputSelector.Position.Y + outputSelector.Size.Y < instructionBounds.GetCenter().Y,
                    "dual type controls stay above the EN/ENO conductor row");
                GD.Print("DUAL_TYPE_SELECTORS_PASS input=" + inputSelector.GetItemText(inputSelector.Selected)
                    + " output=" + outputSelector.GetItemText(outputSelector.Selected));
                var requested = new List<(bool Input, LadderNumericDataType Type)>();
                canvas.NumericConvertDataTypeRequested += (_, input, type) => requested.Add((input, type));
                var requestedInputIndex = normalize ? 1 : scale || rounding || truncate ? 0 : 2;
                var requestedOutputIndex = normalize ? 0 : scale ? 2 : 3;
                inputSelector.Select(requestedInputIndex);
                inputSelector.EmitSignal(OptionButton.SignalName.ItemSelected, requestedInputIndex);
                outputSelector.Select(requestedOutputIndex);
                outputSelector.EmitSignal(OptionButton.SignalName.ItemSelected, requestedOutputIndex);
                Require(requested.SequenceEqual(new[]
                {
                    (true, normalize ? LadderNumericDataType.Int : scale || rounding || truncate ? LadderNumericDataType.Unspecified : LadderNumericDataType.DInt),
                    (false, normalize ? LadderNumericDataType.Unspecified : scale ? LadderNumericDataType.DInt : LadderNumericDataType.Real),
                }), "each dual type control emits its own side and instruction-specific type request");
                GD.Print("DUAL_TYPE_INTERACTION_PASS fixture=" + fixture);
                var restoreInputIndex = normalize ? 2 : scale || rounding || truncate ? 1 : 1;
                var restoreOutputIndex = normalize ? 1 : truncate ? 1 : 2;
                inputSelector.Select(restoreInputIndex);
                inputSelector.EmitSignal(OptionButton.SignalName.ItemSelected, restoreInputIndex);
                outputSelector.Select(restoreOutputIndex);
                outputSelector.EmitSignal(OptionButton.SignalName.ItemSelected, restoreOutputIndex);
            }
            if ((fixture is "numeric-add-tia" or "numeric-add-overflow-tia"
                    or "numeric-sub-tia" or "numeric-sub-overflow-tia"
                    or "numeric-mul-tia" or "numeric-mul-overflow-tia"
                    or "numeric-div-tia" or "numeric-div-zero-tia" or "numeric-mod-tia" or "numeric-mod-zero-tia" or "numeric-abs-tia" or "numeric-neg-tia" or "numeric-abs-overflow-tia" or "numeric-neg-overflow-tia" or "numeric-sqrt-tia" or "numeric-expt-tia" or "numeric-scientific-tia")
                && selectedNode.Length == 0 && reviewSelection == "output")
            {
                var selector = canvas.GetNode<OptionButton>("TiaNumericDataTypeSelector");
                Require(selector.Visible, "selected arithmetic instruction shows the TIA data type selector");
                var selectorBounds = new Rect2(selector.Position, selector.Size);
                var instructionBounds = canvas.GetSelectionBounds();
                Require(instructionBounds.HasPoint(selectorBounds.Position + Vector2.One)
                    && instructionBounds.HasPoint(selectorBounds.End - Vector2.One),
                    "selected arithmetic type control remains inside the selected instruction bounds");
                var isModulo = fixture == "numeric-mod-tia";
                var isUnary = fixture is "numeric-abs-tia" or "numeric-neg-tia" or "numeric-abs-overflow-tia" or "numeric-neg-overflow-tia";
                var isSquareRoot = fixture == "numeric-sqrt-tia";
                var isExponentiate = fixture == "numeric-expt-tia";
                var isScientific = fixture == "numeric-scientific-tia";
                Require(!isExponentiate
                        || selectorBounds.Position.X >= instructionBounds.Position.X + 70.0f,
                    "selected EXPT exponent control preserves the fixed REAL chip and visible ** separator gap");
                Require(isExponentiate
                        ? selector.ItemCount == 4
                            && selector.GetItemText(0) == "???"
                            && selector.GetItemText(1) == "Int"
                            && selector.GetItemText(2) == "DInt"
                            && selector.GetItemText(3) == "Real"
                        : isSquareRoot || isScientific
                        ? selector.ItemCount == 2
                            && selector.GetItemText(0) == "???"
                            && selector.GetItemText(1) == "Real"
                        : isUnary
                        ? selector.ItemCount == 4
                            && selector.GetItemText(0) == "???"
                            && selector.GetItemText(1) == "Int"
                            && selector.GetItemText(2) == "DInt"
                            && selector.GetItemText(3) == "Real"
                        : isModulo
                        ? selector.ItemCount == 4
                            && selector.GetItemText(0) == "???"
                            && selector.GetItemText(1) == "Int"
                            && selector.GetItemText(2) == "DInt"
                            && selector.GetItemText(3) == "Auto (Int)"
                        : selector.ItemCount == 5
                            && selector.GetItemText(0) == "???"
                            && selector.GetItemText(1) == "Int"
                            && selector.GetItemText(2) == "DInt"
                            && selector.GetItemText(3) == "Real"
                            && selector.GetItemText(4) == "Auto (Int)",
                    isExponentiate
                        ? "selected EXPT instruction exposes a REAL base/result and the supported exponent choices"
                        : isSquareRoot || isScientific
                        ? "selected floating unary instruction exposes the unresolved and supported REAL choices only"
                        : isUnary
                        ? "selected unary instruction exposes unresolved, INT, DINT, REAL choices without local Auto"
                        : "selected arithmetic instruction exposes only its supported type choices plus Auto");
                Require(selector.Selected == (isExponentiate ? 3 : 1), "explicit selected type is reflected in the arithmetic block selector");
                GD.Print((isExponentiate ? "EXPT" : isScientific ? "SCIENTIFIC" : isSquareRoot ? "SQRT" : isUnary ? "UNARY_MATH" : "ARITHMETIC") + "_TYPE_SELECTOR_PASS selected=" + selector.GetItemText(selector.Selected)
                    + " choices=" + string.Join(',', Enumerable.Range(0, selector.ItemCount).Select(selector.GetItemText)));
                var interactionTypeName = Option("--review-interact-type", "");
                if (interactionTypeName.Length > 0)
                {
                    if (!Enum.TryParse<LadderNumericDataType>(interactionTypeName, true, out var interactionType)
                        || !Enum.IsDefined(interactionType))
                        throw new ArgumentException("Unknown review arithmetic type: " + interactionTypeName);
                    var interactionIndex = LadderNumericDataTypePresentation.TiaSelectorIndex(
                        document.Rungs[rungIndex].NumericOperationKind, interactionType);
                    LadderNumericDataType? requestedType = null;
                    canvas.NumericDataTypeRequested += (_, requested) => requestedType = requested;
                    selector.Select(interactionIndex);
                    selector.EmitSignal(OptionButton.SignalName.ItemSelected, interactionIndex);
                    Require(requestedType == interactionType,
                        "selecting an arithmetic data-type option emits the matching editor request");
                    selector.Select(1);
                    GD.Print("ARITHMETIC_TYPE_INTERACTION_PASS requested=" + interactionType);
                }
            }
            var context = Option("--review-context", "global");
            if (context == "missing") canvas.SelectObservationContext("deliberately-uncaptured-context");
            else if (context != "global")
            {
                var observed = capture.Contexts.Values.FirstOrDefault(o => o.CallPath.Any(p => p.InstanceId == context));
                if (observed is null) throw new ArgumentException("Unobserved instance context: " + context);
                canvas.SelectObservationContext(LadderInstructionPresentation.ContextKey(observed));
            }
            var state = Option("--review-state", "current");
            if (state == "history-evicted") canvas.SetMonitorPublication(null, ObservationUnavailableReason.HistoryEvicted);
            else if (state != "current") throw new ArgumentException("Unknown review state: " + state);
            if (arguments.Contains("--review-detail")) canvas.ShowInstructionInspector();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            scroll.ScrollVertical = Math.Max(0, (int)canvas.GetRungBounds(rungIndex).Position.Y - 12);
            var reviewScrollX = int.Parse(Option("--review-scroll-x", "0"));
            if (reviewScrollX < 0) throw new ArgumentOutOfRangeException("--review-scroll-x");
            scroll.ScrollHorizontal = reviewScrollX;
            canvas.QueueRedraw();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print($"MONITOR_FIXTURE_EXECUTED fixture={fixture} style={style} block={document.Blocks[document.ActiveBlockIndex].Id} rung={rungIndex} context={context} state={state} scan={snapshot.ScanNumber} observedKeys={string.Join(',', capture.GlobalSummaries.Select(o => o.CatalogKey).Distinct())}");
            GD.Print($"MONITOR_PRESENTATION visibleInstructionBoxes={canvas.LastDrawnInstructionCount} refreshes={canvas.MonitorPresentationRefreshCount}");
            if (_capturePath is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath);
                if (error != Error.Ok) throw new InvalidOperationException("Monitoring capture failed: " + error);
                GD.Print("EDITOR_MONITORING_CAPTURE " + _capturePath); GetTree().Quit(0);
            }
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private async void VerifyEditorMonitoring()
    {
        try
        {
            var program = new LadderProgram(1, "shared-counter-review", "Shared counter review", "LD", TimeSpan.FromMilliseconds(10),
                [new("input", PlcVariableType.Bool, PlcVariableRole.Input, false), new("count", PlcVariableType.Counter, PlcVariableRole.Memory, 0)],
                [new("first", "Count", new("c1", LadderNodeKind.Contact, "input"), Counter: new("ctu", "count", 3)),
                 new("later", "Later load", new("c2", LadderNodeKind.Contact, "input"), CounterLoad: new("load", "count", 9))]);
            var compiled = LadderCompiler.Compile(program);
            if (!compiled.IsValid) throw new InvalidOperationException("Monitoring fixture must compile.");
            var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
            var snapshot = runtime.Scan(new Dictionary<string, bool> { ["input"] = true });
            var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
            foreach (var style in Enum.GetValues<LadderVendorStyle>())
            {
                var canvas = new LadderEditorCanvas(style, document);
                try
                {
                    canvas.SetMonitorSnapshot(snapshot);
                    var lines = canvas.CounterReadoutLines(document.Rungs[0]);
                    if (!lines.Contains("Current count: 1") || !lines.Contains("Preset / setpoint: 3"))
                        throw new InvalidOperationException("CTU must show its actual instruction-time count 1/preset 3, not later LOAD count 9: " + string.Join(" | ", lines));
                    if (style == LadderVendorStyle.SiemensTia)
                    {
                        if (canvas.ReviewPowerFlowStyle(true) != ("00a651", false)
                            || canvas.ReviewPowerFlowStyle(false) != ("233db8", true)
                            || canvas.ReviewPowerFlowStyle(null) != ("87949a", false)
                            || canvas.ReviewPowerFlowStyle(TiaPowerFlowStatus.NotInterconnected) != ("000000", false))
                            throw new InvalidOperationException("TIA satisfied, unsatisfied, unknown, and not-interconnected power-flow styles collapsed together.");
                        GD.Print("TIA_POWER_FLOW_STYLE_VERIFY PASS satisfied=green/solid unsatisfied=blue/dashed unknown=gray/solid notInterconnected=black/solid");
                        if (LadderEditorCanvas.ReviewParallelOutput(true, null) != true
                            || LadderEditorCanvas.ReviewParallelOutput(false, null) is not null
                            || LadderEditorCanvas.ReviewParallelOutput(null, null) is not null
                            || LadderEditorCanvas.ReviewParallelOutput(false, false) != false)
                            throw new InvalidOperationException("TIA parallel-flow projection does not preserve conservative TRUE/FALSE/unknown OR semantics.");
                        GD.Print("TIA_PARALLEL_TRISTATE_PROJECTION_PASS true+unknown=true false+unknown=unknown unknown+unknown=unknown false+false=false");
                    }
                }
                finally { canvas.Free(); }
            }
            VerifyEarlierCycleValuePresentation();
            VerifyEarlierCycleNumericFlow();
            VerifyNetworkLabelNotInterconnected();
            VerifyMonitorContextIndications();
            if(_simulatorShell is not null && !_simulatorShell.VerifyInstructionMonitoringRoutes(out var routeResult))throw new InvalidOperationException(routeResult);
            await VerifyMonitoringRefreshCadence();
            GD.Print("EDITOR_MONITORING_VERIFY PASS shared counter and exact-context indications, both styles"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError("EDITOR_MONITORING_VERIFY FAIL " + error); GetTree().Quit(1); }
    }
    private static void VerifyEarlierCycleValuePresentation()
    {
        var program = new LadderProgram(1, "earlier-cycle-values", "Earlier-cycle value review", "LD", TimeSpan.FromMilliseconds(10),
            [new("skip", PlcVariableType.Bool, PlcVariableRole.Input, false),
             new("observed", PlcVariableType.Bool, PlcVariableRole.Input, true),
             new("target", PlcVariableType.Bool, PlcVariableRole.Output, false),
             new("label_target", PlcVariableType.Bool, PlcVariableRole.Output, false)],
            [new("jump-net", "Skip the observed network on demand", new("jump-enable", LadderNodeKind.Contact, "skip"), Jump: new("jump", "finish")),
             new("observed-net", "Value is retained when a later scan skips this network", new("observed-contact", LadderNodeKind.Contact, "observed"), Coil: new("observed-coil", "target")),
             new("label-net", "Jump destination", new("label-enable", LadderNodeKind.Contact, "observed"),
                 Coil: new("label-coil", "label_target"), LabelInstruction: new("label", "finish"))]);
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Earlier-cycle monitoring fixture must compile: " + string.Join("; ", compiled.Issues));
        var runtime = new VirtualControllerRuntime(compiled.Program!);
        runtime.Run();
        var sessionId = Guid.NewGuid();
        var loadId = Guid.NewGuid();
        ControllerPublication Publication(VirtualControllerSnapshot snapshot, long attempt)
        {
            var origin = new ObservationOrigin(sessionId, loadId, 1, attempt);
            return new(sessionId, runtime.ProgramContentId, loadId, 1, attempt, snapshot.ScanNumber, attempt,
                ControllerPublicationKind.ScanCompleted, ControllerCompletion.Completed, ControllerPhaseCompletion.ProgramExecuted,
                origin, snapshot.Observations!.WithOrigin(origin), snapshot);
        }
        var first = runtime.Scan(new Dictionary<string, bool> { ["skip"] = false, ["observed"] = true });
        if (!first.Observations!.GlobalLast.ContainsKey("observed-contact"))
            throw new InvalidOperationException("Earlier-cycle fixture did not observe the contact on its first scan.");
        var document = new LadderEditorDocument();
        document.ReplaceFromProgram(program);
        var canvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, document);
        try
        {
            canvas.SetMonitorPublication(Publication(first, 1));
            var current = canvas.ReviewObservationValueStyle("observed-contact", "no", "008f4c");
            if (current != (false, "008f4c"))
                throw new InvalidOperationException("Current-cycle value must retain 100% status-color saturation: " + current);
            var second = runtime.Scan(new Dictionary<string, bool> { ["skip"] = true, ["observed"] = true });
            if (second.Observations!.GlobalLast.ContainsKey("observed-contact"))
                throw new InvalidOperationException("Earlier-cycle fixture did not skip the observed contact on its second scan.");
            canvas.SetMonitorPublication(Publication(second, 2));
            var earlier = canvas.ReviewObservationValueStyle("observed-contact", "no", "008f4c");
            if (!earlier.EarlierCycle || earlier.Color == "008f4c"
                || canvas.ReviewPowerFlowForElement(1, "observed-contact") != (null, null))
                throw new InvalidOperationException("Skipped instruction did not retain a reduced-saturation value with unknown current flow: " + earlier);
            var foreignLoad = Publication(second, 3) with { LoadId = Guid.NewGuid() };
            canvas.SetMonitorPublication(foreignLoad);
            if (canvas.InstructionPresentationFor("observed-contact", "no").EarlierCycle)
                throw new InvalidOperationException("Earlier-cycle value leaked across a controller load identity boundary.");
            GD.Print($"TIA_EARLIER_CYCLE_VALUE_VERIFY PASS current={current.Color}/100% earlier={earlier.Color}/50% flow=gray/unknown identityBoundary=cleared");
        }
        finally { canvas.Free(); }
    }
    private static void VerifyEarlierCycleNumericFlow()
    {
        var program = new LadderProgram(1, "earlier-cycle-numeric-flow", "Earlier-cycle numeric flow review", "LD", TimeSpan.FromMilliseconds(10),
            [new("skip", PlcVariableType.Bool, PlcVariableRole.Input, false),
             new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true),
             new("value", PlcVariableType.Int, PlcVariableRole.Memory, 3L),
             new("operand", PlcVariableType.Int, PlcVariableRole.Memory, 1L),
             new("result", PlcVariableType.Int, PlcVariableRole.Memory, 0L),
             new("label_target", PlcVariableType.Bool, PlcVariableRole.Output, false)],
            [new("jump-net", "Skip the numeric network on demand", new("jump-enable", LadderNodeKind.Contact, "skip"), Jump: new("jump", "finish")),
             new("numeric-net", "Retained values must not energize current EN/ENO", new("numeric-enable", LadderNodeKind.Contact, "enable"),
                 NumericOperation: new LadderNumericOperation("numeric", LadderNumericOperationKind.Add, "value", "operand", "result")
                    { DataType = LadderNumericDataType.Int }),
             new("label-net", "Jump destination", new("label-enable", LadderNodeKind.Contact, "enable"),
                 Coil: new("label-coil", "label_target"), LabelInstruction: new("label", "finish"))]);
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Earlier-cycle numeric-flow fixture must compile: " + string.Join("; ", compiled.Issues));
        var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
        var sessionId = Guid.NewGuid(); var loadId = Guid.NewGuid();
        ControllerPublication Publication(VirtualControllerSnapshot snapshot, long attempt)
        {
            var origin = new ObservationOrigin(sessionId, loadId, 1, attempt);
            return new(sessionId, runtime.ProgramContentId, loadId, 1, attempt, snapshot.ScanNumber, attempt,
                ControllerPublicationKind.ScanCompleted, ControllerCompletion.Completed, ControllerPhaseCompletion.ProgramExecuted,
                origin, snapshot.Observations!.WithOrigin(origin), snapshot);
        }
        var first = runtime.Scan(new Dictionary<string, bool> { ["skip"] = false, ["enable"] = true });
        var second = runtime.Scan(new Dictionary<string, bool> { ["skip"] = true, ["enable"] = true });
        if (!first.Observations!.GlobalLast.ContainsKey("numeric") || second.Observations!.GlobalLast.ContainsKey("numeric"))
            throw new InvalidOperationException("Earlier-cycle numeric-flow fixture did not execute then skip the numeric instruction.");
        var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
        var canvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, document);
        try
        {
            canvas.SetMonitorPublication(Publication(first, 1));
            canvas.SetMonitorPublication(Publication(second, 2));
            var view = canvas.InstructionPresentationFor("numeric", "numeric-1");
            if (!view.EarlierCycle || view.CurrentExecution
                || canvas.ReviewPowerFlowForElement(1, "numeric-enable") != (null, null)
                || canvas.ReviewTerminalPowerFlow(1) != (null, null)
                || canvas.ReviewNumericInstructionEnableFlow(1) != (null, null))
                throw new InvalidOperationException("Earlier numeric values energized current contact, EN, ENO, or terminal flow.");
            GD.Print("TIA_NUMERIC_EARLIER_CYCLE_FLOW_VERIFY PASS contact=unknown en=unknown eno=unknown terminal=unknown values=earlier-cycle");
        }
        finally { canvas.Free(); }
    }
    private static void VerifyNetworkLabelNotInterconnected()
    {
        var program = new LadderProgram(1, "label-status", "LABEL topology review", "LD", TimeSpan.FromMilliseconds(10),
            [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
             new("target", PlcVariableType.Bool, PlcVariableRole.Output, false)],
            [new("label-net", "Destination network", new("label-contact", LadderNodeKind.Contact, "enable"),
                Coil: new("label-coil", "target"), LabelInstruction: new("label-marker", "finish"))]);
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("LABEL topology fixture must compile: " + string.Join("; ", compiled.Issues));
        var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
        var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
        var canvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, document);
        try
        {
            canvas.SetMonitorSnapshot(runtime.Scan(new Dictionary<string, bool> { ["enable"] = false }));
            if (canvas.ReviewNetworkLabelPowerFlowStyle(0) != ("000000", false)
                || canvas.ReviewPowerFlowForElement(0, "label-contact") != (true, false)
                || canvas.ReviewTerminalPowerFlow(0) != (false, false))
                throw new InvalidOperationException("FALSE destination logic did not remain separate from the black LABEL marker.");
            canvas.SetMonitorSnapshot(runtime.Scan(new Dictionary<string, bool> { ["enable"] = true }));
            if (canvas.ReviewNetworkLabelPowerFlowStyle(0) != ("000000", false)
                || canvas.ReviewPowerFlowForElement(0, "label-contact") != (true, true)
                || canvas.ReviewTerminalPowerFlow(0) != (true, true))
                throw new InvalidOperationException("TRUE destination logic leaked into the not-interconnected LABEL marker.");
            GD.Print("TIA_LABEL_NOT_INTERCONNECTED_VERIFY PASS marker=black/solid falseNetwork=blue/dashed trueNetwork=green/solid");
        }
        finally { canvas.Free(); }
    }
    private async System.Threading.Tasks.Task VerifyMonitoringRefreshCadence()
    {
        var program=new LadderProgram(1,"cadence","Cadence", "LD",TimeSpan.FromMilliseconds(10),
            [new("input",PlcVariableType.Bool,PlcVariableRole.Input,false),new("output",PlcVariableType.Bool,PlcVariableRole.Output,false)],
            [new("n","Output",new("c",LadderNodeKind.Contact,"input"),Coil:new("o","output"))]);
        var runtime=new VirtualControllerRuntime(LadderCompiler.Compile(program).Program!);runtime.Run();
        var document=new LadderEditorDocument();document.ReplaceFromProgram(program);
        var scroll=new ScrollContainer{Position=new Vector2(20,20),Size=new Vector2(640,360)};
        var canvas=new LadderEditorCanvas(LadderVendorStyle.SiemensTia,document);scroll.AddChild(canvas);AddChild(scroll);canvas.RefreshDocument();
        var origin=new ObservationOrigin(Guid.NewGuid(),Guid.NewGuid(),1,0);
        var started=Time.GetTicksUsec();var published=0;
        try
        {
            while(Time.GetTicksUsec()-started<250000)
            {
                for(var i=0;i<10;i++)
                {
                    var snapshot=runtime.Scan(new Dictionary<string,bool>{["input"]=published%2==0});published++;
                    var identity=origin with{ScanAttemptNumber=published};
                    canvas.SetMonitorPublication(new(origin.SessionId,runtime.ProgramContentId,origin.LoadId,1,published,snapshot.ScanNumber,published,
                        ControllerPublicationKind.ScanCompleted,ControllerCompletion.Completed,ControllerPhaseCompletion.ProgramExecuted,identity,snapshot.Observations!.WithOrigin(identity),snapshot));
                }
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            await ToSignal(GetTree().CreateTimer(.04),SceneTreeTimer.SignalName.Timeout);
            var elapsed=Time.GetTicksUsec()-started;
            if(canvas.MonitorPresentationRefreshCount>(long)(elapsed/33334)+2)throw new InvalidOperationException("Presentation exceeded 30Hz bound.");
            if(canvas.MonitorScanNumber!=published || runtime.Snapshot.ScanNumber!=published)throw new InvalidOperationException("Coalescing lost the latest envelope or changed execution count.");
            if(published<=canvas.MonitorPresentationRefreshCount)throw new InvalidOperationException("Cadence fixture did not exercise coalescing.");
            GD.Print($"MONITOR_CADENCE_PASS published={published} refreshed={canvas.MonitorPresentationRefreshCount} elapsedUsec={elapsed}");
            var many=program with{Networks=Enumerable.Range(0,1000).Select(i=>new LadderNetwork("n"+i,"Visible row "+i,new("c"+i,LadderNodeKind.Contact,"input"),Coil:new("o"+i,"output"))).ToArray()};
            document.ReplaceFromProgram(many);canvas.RefreshDocument();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(canvas.LastDrawnInstructionCount>200)throw new InvalidOperationException("Offscreen row virtualization exceeded visible box bound.");
            GD.Print($"MONITOR_CULLING_MEASURE rows=1000 visibleInstructionBoxes={canvas.LastDrawnInstructionCount} viewport=640x360 (zero requires rendered follow-up)");
        }
        finally{scroll.QueueFree();}
    }
    private static void VerifyMonitorContextIndications()
    {
        foreach(var kind in Enum.GetValues<LadderNumericOperationKind>())
        {
            var rung=new EditableRung("arity", "Arity", "") { NumericOperationKind=kind, NumericSourceA="A", NumericSourceB="B", NumericSourceC="C", NumericDestination="D" };
            var rows=LadderEditorCanvas.NumericConfiguredRows(rung);
            if(rows.Length!=2+(LadderNumericOperationRules.RequiresSourceB(kind)?1:0)+(LadderNumericOperationRules.RequiresSourceC(kind)?1:0))
                throw new InvalidOperationException("Numeric unavailable-state arity mismatch: "+kind);
            if(LadderNumericOperationRules.RequiresSourceC(kind)&&(!rows.Contains("MIN: A")||!rows.Contains("VALUE: B")||!rows.Contains("MAX: C")))
                throw new InvalidOperationException("Ternary unavailable-state MIN/VALUE/MAX missing: "+kind);
        }
        var program = new LadderProgram(1, "context-review", "Context review", "LD", TimeSpan.FromMilliseconds(10),
            [new("enable", PlcVariableType.Bool, PlcVariableRole.Input, true), new("flag", PlcVariableType.Bool, PlcVariableRole.Memory, false), new("out", PlcVariableType.Bool, PlcVariableRole.Output, false)], [])
        { Blocks = [new("main", "Main", [
            new("n1", "First call sees FALSE", new("c1", LadderNodeKind.Contact, "enable"), Call: new("first-call", "child")),
            new("n2", "Set flag", new("c2", LadderNodeKind.Contact, "enable"), Coil: new("set-flag", "flag")),
            new("n3", "Second call sees TRUE", new("c3", LadderNodeKind.Contact, "enable"), Call: new("second-call", "child"))]),
            new("child", "Child", [new("child-net", "Observe flag", new("child-contact", LadderNodeKind.Contact, "flag"), Coil: new("child-coil", "out"))])], EntryBlock = "main" };
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Context fixture must compile.");
        var runtime = new VirtualControllerRuntime(compiled.Program!); runtime.Run();
        var snapshot = runtime.Scan(new Dictionary<string, bool> { ["enable"] = true });
        var first = snapshot.Observations!.Contexts.Values.Single(o => o.InstructionId == "child-contact" && o.CallPath.Any(p => p.CallSiteId == "first-call"));
        var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
        foreach (var style in Enum.GetValues<LadderVendorStyle>())
        {
            var canvas = new LadderEditorCanvas(style, document);
            try
            {
                canvas.SetMonitorSnapshot(snapshot);
                if (!canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Global last context must be TRUE.");
                canvas.SelectObservationContext(LadderInstructionPresentation.ContextKey(first));
                if (canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Selected first context FALSE must not inherit global TRUE highlight.");
                canvas.SelectObservationContext("missing");
                if (canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Missing context must never be green.");
                canvas.SelectObservationContext(null); canvas.SetMonitorSnapshot(runtime.Stop());
                if (canvas.IsElementEnergized("child-contact")) throw new InvalidOperationException("Stopped lifecycle must not present current green flow.");
            }
            finally { canvas.Free(); }
        }
    }
}


