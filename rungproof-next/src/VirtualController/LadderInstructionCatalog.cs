using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed record LadderInstructionHelp(
    string Key,
    string Category,
    string TiaName,
    string LogixName,
    string Summary,
    string Parameters,
    string Execution,
    string Restrictions,
    string Example);

/// <summary>
/// Authoritative help catalog for instructions that the offline runtime and
/// editor actually implement. Vendor names are presentation aliases over the
/// shared RungProof IR; they never imply vendor project-file compatibility.
/// </summary>
public static class LadderInstructionCatalog
{
    private static readonly IReadOnlyList<LadderInstructionHelp> Catalog = BuildCatalog();
    private static readonly IReadOnlyDictionary<string, LadderInstructionHelp> ByKey =
        Catalog.ToDictionary(item => item.Key, StringComparer.Ordinal);

    public static IReadOnlyList<LadderInstructionHelp> Entries => Catalog;

    public static LadderInstructionHelp Get(string key) =>
        ByKey.TryGetValue(key, out var item)
            ? item
            : throw new KeyNotFoundException($"Unknown Ladder instruction help key '{key}'.");

    public static bool TryGet(string key, out LadderInstructionHelp? item) => ByKey.TryGetValue(key, out item);

    private static IReadOnlyList<LadderInstructionHelp> BuildCatalog()
    {
        var items = new List<LadderInstructionHelp>
        {
            new("no", "Bit logic", "Normally open contact —| |—", "Examine If Closed (XIC) —] [—",
                "Passes rung power when the referenced BOOL or supported instance status member is TRUE.",
                "Operand: BOOL tag, TIMER .Q/.DN/.TT, or COUNTER .Q/.DN.",
                "Read once at this point in deterministic left-to-right scan order.",
                "Read-only. Unknown or non-BOOL operands fail validation.", "—| start_command |—"),
            new("nc", "Bit logic", "Normally closed contact —|/|—", "Examine If Open (XIO) —]/[—",
                "Passes rung power when the referenced BOOL or supported instance status member is FALSE.",
                "Operand: BOOL tag, TIMER .Q/.DN/.TT, or COUNTER .Q/.DN.",
                "Reads the current value and inverts it at this point in scan order.",
                "Read-only. Unknown or non-BOOL operands fail validation.", "—|/ stop_command |—"),
            new("edge-rising", "Bit logic", "Positive edge contact —|P|—", "XIC + ONS rising-edge composite",
                "Passes rung power for one scan when the referenced BOOL changes from FALSE to TRUE.",
                "Operands: BOOL signal above the contact and a separate writable BOOL edge-memory tag below it.",
                "Every execution compares the current signal with the prior state stored in the edge-memory tag, writes the current signal back, and pulses for one scan on 0→1.",
                "TIA P/N editing exposes declared edge memory. Legacy projects without that operand retain baseline-suppressed internal compatibility state; the Logix presentation remains a simulator composite.", "—|P start_command |—"),
            new("edge-falling", "Bit logic", "Negative edge contact —|N|—", "XIO + ONS / OSF falling-edge composite",
                "Passes rung power for one scan when the referenced BOOL changes from TRUE to FALSE.",
                "Operands: BOOL signal above the contact and a separate writable BOOL edge-memory tag below it.",
                "Every execution compares the current signal with the prior state stored in the edge-memory tag, writes the current signal back, and pulses for one scan on 1→0.",
                "TIA P/N editing exposes declared edge memory. Legacy projects without that operand retain baseline-suppressed internal compatibility state; Rockwell OSF/prescan equivalence is not claimed.", "—|N guard_closed |—"),
            new("coil", "Bit logic", "Assignment coil —( )—", "Output Energize (OTE) —( )—",
                "Writes the current rung result to a BOOL memory or output tag.",
                "Destination: writable BOOL memory/output tag.",
                "Writes TRUE on a true rung and FALSE on a false rung every execution.",
                "Inputs are not writable. Later writes in scan order determine the final value.", "—| enable |——( motor_run )—"),
            new("set", "Bit logic", "Set coil —(S)—", "Output Latch (OTL) —(L)—",
                "Latches a BOOL memory or output tag TRUE.", "Destination: writable BOOL memory/output tag.",
                "Writes TRUE only while the rung is true; a false rung preserves the prior value.",
                "Reset with an explicit Reset/OTU instruction. Controller Reset clears simulator memory.", "—| start |——(S seal_in)—"),
            new("reset", "Bit logic", "Reset coil —(R)—", "Output Unlatch (OTU) —(U)—",
                "Clears a latched BOOL memory or output tag.", "Destination: writable BOOL memory/output tag.",
                "Writes FALSE only while the rung is true; a false rung preserves the prior value.",
                "Later Set/Reset writes in the same scan take normal scan-order priority.", "—| stop |——(R seal_in)—"),
            new("branch", "Bit logic", "Parallel branch", "Branch",
                "Adds an OR path to the selected network/rung.", "Each branch contains a series path of conditions.",
                "Branches evaluate top-to-bottom; any true branch passes power.",
                "A parallel group requires at least two non-empty paths before loading.", "start OR seal_in"),
            new("ton", "Timers", "TON (IEC on-delay)", "TON (Timer On Delay)",
                "Non-retentive on-delay timer with live elapsed, timing, and done state.",
                "Instance: TIMER memory tag. Preset: positive milliseconds.",
                "Accumulates one configured base-scan period per true execution; false, Stop, or Reset clears elapsed and done.",
                "Q and DN are aliases in the simulator. This is not vendor timing certification.", "TON start_delay PT=1000 ms"),
            new("tof", "Timers", "TOF (IEC off-delay)", "TOF (Timer Off Delay)",
                "Keeps the timer done output true for the preset after rung power turns off.",
                "Instance: TIMER memory tag. Preset: positive milliseconds.",
                "A true rung sets done immediately. A falling edge starts timing; done clears when elapsed reaches the preset.",
                "Stop or Reset clears elapsed and done. Q and DN are simulator aliases, not vendor certification.", "TOF fan_delay PT=1000 ms"),
            new("tp", "Timers", "TP (IEC pulse)", "TP (simulator pulse timer)",
                "Generates a fixed-duration done pulse on a false-to-true rung transition.",
                "Instance: TIMER memory tag. Preset: positive milliseconds.",
                "The rising edge starts one pulse; input changes do not extend it, and a new pulse requires the input to return false.",
                "Studio 5000 has no direct TP instruction; this is the shared simulator pulse model.", "TP one_shot PT=500 ms"),
            new("rto", "Timers", "TONR (time accumulator)", "RTO (Retentive Timer On)",
                "Accumulates enabled scan time and retains elapsed/done state while the rung is false or the simulator is stopped.",
                "TIA instance: IEC timer DB or static instance. Pins: IN BOOL, R BOOL, PT TIME; Q BOOL, ET TIME. The virtual model stores TIMER state; LTIME is unsupported.",
                "When R is true, reset ET/Q and block timing. Otherwise accumulate one configured base-scan period while IN is true; IN false pauses and retains elapsed/done state.",
                "Use RT for the Siemens timer reset operation. Virtual timing is deterministic and scan-quantized; it does not model Siemens DB layout or certify CPU timing.", "TONR process_time R=reset_request PT=T#1s"),
            new("timer-reset", "Timers", "RT / RESET_TIMER", "RES (timer)",
                "Clears a retentive TIMER instance.",
                "Instance: TIMER memory tag.",
                "A true rung clears accumulated time, timing, input, and done state before later networks execute.",
                "The shared simulator reset does not reproduce vendor structure/status side effects beyond the exposed timer state.", "RT/RES process_time"),
            new("ctu", "Counters", "CTU (count up)", "CTU (Count Up)",
                "Counts positive edges at CU. R resets CV and blocks counting while true; Q is true when CV >= PV.",
                "TIA instance: IEC counter DB or static instance. Type selector: ???, Int, SInt, DInt, USInt, UInt, UDInt, LInt, ULInt. Pins: CU BOOL, R BOOL, PV integer; Q BOOL, CV typed count. The virtual model stores COUNTER state.",
                "With R false, each positive edge at CU increments CV. R has priority: it resets CV and Q and ignores CU while asserted.",
                "RungProof does not reproduce Siemens DB memory layout or CPU scan timing. ULInt shares the virtual controller's signed Int64 ceiling; ??? is an unresolved edit state, not a runnable type.", "CTU parts_int DB=IEC_Counter_0_DB CU=parts_sensor R=reset_request PV=10"),
            new("ctd", "Counters", "CTD (count down)", "CTD (Count Down)",
                "Retentive rising-edge count-down instruction.",
                "Instance: IEC counter DB. Inputs: CD count pulse, LD load BOOL, PV positive preset. Outputs: Q and CV.",
                "LD loads PV into CV while true and suppresses CD. Otherwise CD decrements CV on a rising edge; Q is true when CV <= 0.",
                "The offline controller models the visible Siemens LD/CD/PV pins and retains state in its virtual IEC instance. It is not vendor data-layout or scan-time certification.", "CTD remaining PRE=10 LD=load_request"),
            new("counter-load", "Counters", "Load counter preset (LD)", "Load counter preset (simulator)",
                "Copies the configured preset into a COUNTER instance and clears its done and edge states.",
                "Instance: COUNTER memory tag. Preset: positive integer.",
                "Executes while the rung is true; later networks observe the loaded value in the same scan.",
                "TIA CTD exposes LD as a box input; Studio 5000 normally initializes ACC by other logic. This is an explicit portable simulator instruction.", "LD remaining PV=10"),
            new("counter-reset", "Counters", "Reset counter input", "RES (Reset)",
                "Clears a COUNTER instance's accumulated value, done state, and edge memory.",
                "Instance: COUNTER memory tag.", "Executes only while the rung is true.",
                "This shared simulator operation does not claim identical vendor data-layout behavior.", "RES parts"),
            new("rung", "Program structure", "Insert network", "Insert rung",
                "Adds an ordered Ladder execution unit to the active block/routine.",
                "Title/comment and exactly one supported output instruction.",
                "Networks/rungs execute in document order within their scheduled block.",
                "The new unit must be completed and validated before offline load.", "Network 3 / Rung 2"),
            new("call", "Program control", "LAD block call (FC/FB)", "Specific FC / FB block call",
                "Conditionally executes the selected function (FC) or function block (FB) at this network position.",
                "Target: an existing FC or FB. Bind its declared parameters; an FB call also requires an assigned instance.",
                "A true rung executes the target before the caller's next network/rung; false skips the call.",
                "TIA LAD inserts a named FC/FB call from Program blocks; CALL is not a generic LAD box name. RungProof supports dragging a parameterized FC/FB onto a network terminal, but calls remain terminal actions and use a custom authoring form. The legacy generic call command is hidden in the TIA UI; Siemens multiple-instance and parameter-instance call modes remain unsupported.",
                "FC StationControl / FB StationControl with instance DB"),
            new("return", "Program control", "RETURN from block", "RET (Return from Subroutine)",
                "Conditionally exits the current block/routine execution frame.",
                "TIA return value: incoming RLO, TRUE, FALSE, or a declared BOOL operand. Logix RET has no editable operand in this subset.",
                "A true rung skips the remaining networks/rungs in the current frame and resumes the caller. The selected TIA return value becomes the completed caller ENO; a false rung continues normally.",
                "RungProof ends only the current due task when RETURN is used in a task-entry block. RETURN never stops the controller or another due task. Recursive calls remain prohibited.",
                "—| enable |——(RET, FALSE)—  caller ENO=0"),
            new("jump", "Program control", "JMP", "JMP (Jump to Label)",
                "Conditionally continues execution at a named destination in the current block/routine.",
                "Target: a non-empty block-local LABEL/LBL name.",
                "A true rung skips intervening networks/rungs and resumes at the destination; a false rung falls through normally.",
                "Targets cannot cross block/routine boundaries. A deterministic 10,000-network scan watchdog stops the offline controller and drives outputs safe if backward jumps loop indefinitely.",
                "—| bypass |——[JMP finish]—"),
            new("label", "Program control", "LABEL", "LBL (Label)",
                "Declares a named destination for JMP in the current block/routine.",
                "Name: non-empty and unique within the containing block/routine.",
                "The marker itself does not redirect execution; its rung power is exposed only for offline monitoring.",
                "Duplicate labels and missing JMP targets fail validation. Labels are simulator IR objects and are not vendor project-file compatible.",
                "[LABEL / LBL finish]"),
        };

        var comparisons = new[]
        {
            ("compare-0", "Equal (EQ)", "Equal (EQU)", "=="),
            ("compare-1", "Not equal (NE)", "Not Equal (NEQ)", "<>"),
            ("compare-2", "Greater than (GT)", "Greater Than (GRT)", ">"),
            ("compare-3", "Greater or equal (GE)", "Greater Than or Equal (GEQ)", ">="),
            ("compare-4", "Less than (LT)", "Less Than (LES)", "<"),
            ("compare-5", "Less or equal (LE)", "Less Than or Equal (LEQ)", "<="),
        };
        foreach (var (key, tia, logix, symbol) in comparisons)
            items.Add(new LadderInstructionHelp(key, "Compare", tia, logix,
                $"Passes rung power when source A {symbol} source B.",
                key is "compare-0" or "compare-1"
                    ? "Sources: numeric operands, or matching STRING/ENUM tags and quoted text literals."
                    : "Sources: INT/DINT/REAL tags, numeric literals, COUNTER ACC/CV/PRE/PV, or TIMER ET/PT.",
                "Both operands are resolved at this point in deterministic scan order.",
                "BOOL operands and unknown members fail validation. Text supports equality/inequality only; enum literals must be declared members. REAL comparison uses finite double values.",
                $"measured_value {symbol} setpoint"));

        items.Add(new LadderInstructionHelp("compare-6", "Compare", "CODE MATCH (local)", "CODE MATCH (local)",
            "Passes rung power when a STRING contains a complete case-sensitive alphanumeric code token.",
            "Source A: STRING tag. Source B: STRING tag or quoted nonempty alphanumeric code, for example \"F003\".",
            "Splits source A on non-alphanumeric characters and compares whole tokens using ordinal case-sensitive equality.",
            "F003 matches Drive alarm: F003; reset, but not F0030, AF003 or f003. Local RungProof instruction; no Siemens or Allen-Bradley parity claim.",
            "CODE MATCH alarm_text, \"F003\""));

        var numeric = new[]
        {
            ("numeric-0", "MOVE", "MOV", "Copies source A to the destination.", "MOV source -> destination"),
            ("numeric-1", "ADD", "ADD", "Adds source A and source B.", "ADD a, b -> result"),
            ("numeric-2", "SUB", "SUB", "Subtracts source B from source A.", "SUB a, b -> result"),
            ("numeric-3", "MUL", "MUL", "Multiplies source A by source B.", "MUL a, b -> result"),
            ("numeric-4", "DIV", "DIV", "Divides source A by source B.", "DIV a, b -> result"),
            ("numeric-5", "MOD", "MOD", "Returns the remainder of source A divided by source B.", "MOD a, b -> remainder"),
            ("numeric-6", "ABS", "ABS", "Returns the absolute magnitude of source A.", "ABS source -> magnitude"),
            ("numeric-7", "NEG", "NEG", "Changes the arithmetic sign of source A.", "NEG source -> result"),
            ("numeric-8", "SQRT", "SQRT (SQR before v36)", "Returns the nonnegative square root of source A.", "SQRT source -> result"),
            ("numeric-9", "EXPT", "EXPT (XPY before v36)", "Raises source A to the power of source B.", "EXPT base, exponent -> result"),
            ("numeric-10", "LN", "LN", "Returns the natural logarithm of source A.", "LN source -> result"),
            ("numeric-11", "SIN", "SIN", "Returns the sine of source A in radians.", "SIN radians -> result"),
            ("numeric-12", "COS", "COS", "Returns the cosine of source A in radians.", "COS radians -> result"),
            ("numeric-13", "TAN", "TAN", "Returns the tangent of source A in radians.", "TAN radians -> result"),
            ("numeric-14", "ASIN", "ASIN (ASN before v36)", "Returns the arcsine of source A in radians.", "ASIN source -> radians"),
            ("numeric-15", "ACOS", "ACOS (ACS before v36)", "Returns the arccosine of source A in radians.", "ACOS source -> radians"),
            ("numeric-16", "ATAN", "ATAN (ATN before v36)", "Returns the arctangent of source A in radians.", "ATAN source -> radians"),
            ("numeric-17", "TRUNC", "TRUNC (TRN before v36)", "Removes the fractional part of source A toward zero.", "TRUNC source -> result"),
            ("numeric-18", "NORM_X", "CPT normalization macro", "Maps VALUE from MIN through MAX to a normalized result.", "NORM_X MIN, VALUE, MAX -> result"),
            ("numeric-19", "SCALE_X", "CPT scaling macro", "Maps normalized VALUE to the range MIN through MAX.", "SCALE_X MIN, VALUE, MAX -> result"),
            ("numeric-20", "CONVERT", "MOV destination conversion", "Converts source A according to the destination's declared numeric type.", "CONVERT source -> destination"),
            ("numeric-21", "ROUND", "CPT rounding macro", "Rounds source A to the nearest integer, with exact halves rounded to even.", "ROUND source -> result"),
            ("numeric-22", "CEIL", "CPT ceiling macro", "Returns the nearest integer greater than or equal to source A.", "CEIL source -> result"),
            ("numeric-23", "FLOOR", "CPT floor macro", "Returns the nearest integer less than or equal to source A.", "FLOOR source -> result"),
        };
        foreach (var (key, tia, logix, summary, example) in numeric)
            items.Add(new LadderInstructionHelp(key, "Move and math", tia, logix, summary,
                key == "numeric-0"
                    ? "Source A: numeric tag/member/literal, or STRING/ENUM tag or quoted text literal. Destination: writable tag of compatible type; enum text must be a declared member."
                    : key is "numeric-18" or "numeric-19"
                    ? "Sources A/B/C are MIN, VALUE, and MAX. Destination: writable INT/DINT/REAL tag."
                    : key is not ("numeric-1" or "numeric-2" or "numeric-3" or "numeric-4" or "numeric-5" or "numeric-9")
                    ? "Source: numeric tag/member/literal. Destination: writable INT/DINT/REAL tag."
                    : "Sources A/B: numeric tags/members/literals. Destination: writable INT/DINT/REAL tag.",
                "Executes only on a true rung. Ordinary INT/DINT destination conversion rounds nearest-even and clamps to the simulator's signed range; TRUNC removes the fraction before that conversion. REAL destinations retain finite fractions.",
                key switch
                {
                    "numeric-4" => "For a zero divisor, INT/DINT write 0; REAL writes 2143289344, except REAL 0/0 writes NaN. ENO remains true.",
                    "numeric-5" => "A zero divisor writes 0, drops ENO, and publishes VC_RUNTIME_MOD_ZERO.",
                    "numeric-8" => "Negative source preserves the destination and publishes VC_RUNTIME_DOMAIN.",
                    "numeric-9" => "A negative base with a fractional exponent, or zero with a negative exponent, preserves the destination and publishes VC_RUNTIME_DOMAIN.",
                    "numeric-10" => "A negative or invalid source writes an invalid REAL value and publishes VC_RUNTIME_DOMAIN; zero writes negative infinity.",
                    "numeric-14" or "numeric-15" => "A source outside -1 through 1, or an invalid source, writes an invalid REAL value and publishes VC_RUNTIME_DOMAIN.",
                    "numeric-18" => "MIN must be below MAX. TIA provides NORM_X directly; Logix displays the simulator operation as a CPT-equivalent macro, not a native Logix instruction.",
                    "numeric-19" => "MIN must be below MAX. TIA provides SCALE_X directly; Logix displays the simulator operation as a CPT-equivalent macro, not a native Logix instruction.",
                    "numeric-20" => "TIA exposes CONVERT. Logix performs destination-type conversion through MOV and mixed-type instruction rules; vendor overflow/status flags are not simulated.",
                    "numeric-21" => "TIA exposes ROUND. Logix automatically uses nearest-even for REAL-to-integer destination conversion; the visible CPT label is a simulator macro, not a native ROUND instruction.",
                    "numeric-22" or "numeric-23" => "TIA exposes this instruction directly. The Logix CPT presentation is a simulator macro and does not claim a native Logix operator.",
                    _ => "Non-finite results are rejected and preserve the previous destination.",
                }, example));

        // Keep text formatting separate from numeric help: its destination and failure contract are typed.
        items.Add(new LadderInstructionHelp("numeric-24", "Text", "FORMAT_TEXT (local)", "FORMAT_TEXT (local)",
            "Formats one numeric value into a STRING using invariant culture.",
            "Source A: numeric tag/literal. Source B: STRING tag or quoted template containing exactly one {0:F0} through {0:F6}. Destination: writable STRING.",
            "Executes only on a true rung, resolving both sources in scan order.",
            "Invalid literal templates fail validation. An invalid dynamic template or overlong result clears the destination and reports a runtime diagnostic, preventing stale print text. Local RungProof instruction; no Siemens or Allen-Bradley parity claim.",
            "FORMAT_TEXT mass_kg, \"CHICKEN {0:F3} kg\" -> label_text"));
        return items;
    }
}
