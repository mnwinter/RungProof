# Virtual controller program schema v1

Programs are UTF-8 JSON. The canonical example is
`programs/demo-conveyor.ld.json`. Parsing is strict and compilation must
succeed before execution.

This `.ld.json` schema is the validated executable-program interchange, not
the mutable engineering project. The workbench's **Save project** and
**Open project** commands use `.rpproj.json` with
`kind: "rungproof-ladder-project"`. That editor format preserves stable
element IDs, active block context, blocks/routines, tasks/OBs, typed tag
definitions, scene bindings, and watch symbols even while the program contains
compiler errors. Opening an editor project does not load or run it. The user
must explicitly invoke **Verify + Load**, which builds this executable form and
passes it through all compiler and scene-binding validation before runtime
replacement.

## Root object

```json
{
  "schemaVersion": 1,
  "id": "stable-program-id",
  "name": "Display name",
  "language": "LD",
  "scanPeriodMs": 20,
  "variables": [],
  "watchVariables": [],
  "networks": []
}
```

- `schemaVersion` must be `1`.
- `language` must be `LD`.
- `scanPeriodMs` must be greater than zero.
- IDs and variable names are case-sensitive ordinal strings.

New editor documents also declare `entryBlock`, `blocks`, and `tasks`. The root
`networks` array remains the first block's compatibility projection for older
readers; when `blocks` is present, it is authoritative.

```json
{
  "entryBlock": "main",
  "blocks": [
    { "id": "main", "name": "Main", "networks": [] },
    { "id": "sequence", "name": "Sequence", "networks": [] }
  ],
  "tasks": [
    {
      "id": "main-task",
      "name": "MainTask",
      "kind": "continuous",
      "periodMs": 20,
      "priority": 10,
      "entryBlock": "main"
    }
  ]
}
```

Tasks assign block entry points to the deterministic simulator scheduler.
`kind` is `continuous` or `periodic`. A continuous task is due every base scan.
A periodic task requires a positive `periodMs` that is at least, and an integer
multiple of, the root `scanPeriodMs`. `priority` must be nonnegative. When
multiple tasks are due on the same base scan, lower numeric priority executes
first and document order breaks ties. Each task's block executes in network
order, including inline CALL/JSR behavior.

This is scan-boundary scheduling. It does not model asynchronous OB interrupts,
Rockwell event tasks, execution-time budgets, watchdogs, or preemption. TIA OB
and Studio 5000 task labels are vendor-familiar presentation over this shared
simulator-native model, not vendor-project compatibility.

The editor treats block and task `id` values as stable reference identities.
Renaming changes only the human-readable `name`, so CALL/JSR targets and task
entry assignments remain intact. A block cannot be deleted while it is the
controller entry, a task entry, or a CALL/JSR target, and the editor retains at
least one block and one task. These are authoring safeguards; direct JSON still
passes through compiler validation before it can load.

`watchVariables` is an optional ordered array of declared tag names. It is
engineering metadata used by the persistent TIA-style Watch table and
Logix-style Watch List. Missing and duplicate names fail validation. Changing
this list does not alter executable scan semantics or require an offline
controller reload.

## Variables

```json
{
  "name": "simulated_photoeye",
  "type": "BOOL",
  "role": "input",
  "initial": false,
  "binding": "simulated_photoeye"
}
```

The current slice executes `BOOL`, `INT`, `DINT`, `REAL`, `TIMER`, and `COUNTER`.
BOOL/INT/DINT/REAL roles are `input`, `memory`, and `output`. TIMER and COUNTER
instances must use the `memory` role.
`binding` is optional; when present it is a symbolic plant point, never a PLC
address. Input bindings may sample only declared PC-owned BOOL points. Output
bindings may write only declared PLC-owned BOOL points.

## Networks and nodes

Each network has a globally unique stable `id`, a display `label`, one `logic`
node, and exactly one supported output instruction.

```json
{
  "id": "network-output",
  "label": "Conveyor output",
  "logic": {
    "id": "series-output",
    "kind": "series",
    "children": [
      {
        "id": "contact-seal",
        "kind": "contact",
        "variable": "seal_in",
        "contact": "normallyOpen"
      }
    ]
  },
  "coil": {
    "id": "coil-conveyor",
    "variable": "conveyor_running",
    "mode": "assign"
  }
}
```

Supported node forms:

- `contact`: requires `variable` and `contact` equal to `normallyOpen` or
  `normallyClosed`; it may optionally declare `edge` equal to `rising` or
  `falling`;
- `series`: requires one or more child nodes;
- `parallel`: requires at least two child nodes.

Coil `mode` is `assign`, `set`, or `reset`. Missing mode is read as `assign`
for compatibility with earlier saved programs. An assign coil writes the rung
result each scan. Set writes TRUE only on a true rung; reset writes FALSE only
on a true rung. A false set/reset rung preserves the prior value. If multiple
coils target the same BOOL, normal document order determines the final value.

All network, node, and output-instruction IDs share one uniqueness scope. This makes live
energization state stable and unambiguous in the UI.

A newly authored TIA P/N contact declares `edgeMemory`, a unique writable BOOL
memory tag shown below the contact. Each execution compares the current signal
with that stored value, writes the current signal back, and passes power for
exactly one scan on the selected transition. Its authored initial value therefore
controls the first evaluation after Reset; Stop preserves it. This follows the
explicit Siemens edge-memory operand model.

Legacy documents without `edgeMemory` remain readable. Only that compatibility
path stores prior state against the stable node ID and establishes a no-pulse
baseline on first execution after Stop or Reset. The Logix workbench presents
the operation as portable `XIC + ONS` and `XIO + ONS / OSF` composites and does
not claim a vendor controller storage-bit data layout.

## Timer output instructions

```json
{
  "id": "network-delay",
  "label": "Delay conveyor start",
  "logic": {
    "id": "contact-start",
    "kind": "contact",
    "variable": "start_command",
    "contact": "normallyOpen"
  },
  "timer": {
    "id": "ton-start-delay",
    "variable": "start_delay",
    "presetMs": 1000,
    "kind": "OnDelay"
  }
}
```

`kind` is `OnDelay` (TON), `OffDelay` (TOF), `Pulse` (TP), or
`RetentiveOnDelay` (TIA TONR / Logix RTO). Missing
`kind` loads as `OnDelay` for compatibility with earlier saved programs.
`start_delay` must be a TIMER memory tag and `presetMs` must be greater than
zero. TON accumulates while true and resets while false. TOF sets done
immediately while true, then holds it until the preset expires after a falling
edge. TP starts one fixed-duration pulse on a rising edge and requires a false
input before it can retrigger. RetentiveOnDelay accumulates while true and
pauses without clearing elapsed/done state while false or during simulator
Stop. A separate timer reset output is represented as:

```json
"timerReset": { "id": "reset-start-delay", "variable": "start_delay" }
```

When its rung is true, it clears accumulated, timing, input, and done state.
The TIA view presents TONR plus RT/RESET_TIMER terminology. The Logix view
presents Ladder RTO plus RES. One timer instruction owns each TIMER instance;
reusing an instance for a second TON/TOF/TP/TONR/RTO fails validation with
`VC013`, while any number of explicit timer-reset references may target that
owned instance. Contacts may reference `start_delay.Q` (TIA done), `start_delay.DN`
(Logix done), or `start_delay.TT` (timing). Q and DN are aliases over the same
simulator state; this does not claim vendor project-file compatibility.

## CTU and counter reset instructions

```json
{
  "id": "network-count",
  "label": "Count parts",
  "logic": {
    "id": "contact-photoeye",
    "kind": "contact",
    "variable": "photoeye",
    "contact": "normallyOpen"
  },
  "counter": {
    "id": "ctu-parts",
    "variable": "parts",
    "preset": 10
  }
}
```

`parts` must be a COUNTER memory tag and `preset` must be a positive integer.
CTU increments only on a false-to-true transition. A continuously true rung
does not count repeatedly. Accumulated count remains when the rung is false and
across Stop. `parts.Q` (TIA) and `parts.DN` (Logix) are Boolean aliases for
done. CV/ACC and PV/PRE are valid numeric expression operands.

A separate counter-reset output is represented as:

```json
"counterReset": { "id": "reset-parts", "variable": "parts" }
```

When its rung is true it clears accumulated count, done, and CTU edge memory.
The Logix view presents this as RES; the TIA view presents it as the reset
input workflow over the shared simulator instruction model.

## Numeric comparison nodes

Comparison nodes are rung conditions alongside contacts. They support INT/DINT/REAL
tags, invariant-culture numeric literals, counter `ACC/CV/PRE/PV`, and timer
`ET/PT` in milliseconds.

```json
{
  "id": "compare-speed",
  "kind": "compare",
  "left": "line_speed",
  "operator": "GreaterOrEqual",
  "right": "speed_setpoint"
}
```

Operators are `Equal`, `NotEqual`, `GreaterThan`, `GreaterOrEqual`, `LessThan`,
and `LessOrEqual`. The result is Boolean rung continuity. Unknown operands,
BOOL operands, and instance members without numeric meaning are rejected before
load.

## MOV and numeric output instructions

Numeric output instructions execute only when their rung is true and otherwise
preserve the destination. Destinations must be writable INT, DINT, or REAL tags.

```json
"numericOperation": {
  "id": "calculate-speed",
  "operation": "Multiply",
  "sourceA": "command_percent",
  "sourceB": "max_speed",
  "destination": "speed_reference",
  "sourceC": "0"
}
```

Binary operations are `Add`, `Subtract`, `Multiply`, `Divide`, `Modulo`, and
`Exponentiate`. Unary operations are `Move`, `Absolute`, `Negate`, `SquareRoot`,
`NaturalLog`, `Sine`, `Cosine`, `Tangent`, `ArcSine`, `ArcCosine`, `ArcTangent`,
`Truncate`, `Convert`, `Round`, `Ceiling`, and `Floor`; they ignore `sourceB`.
Trigonometric input and output angles are
radians. `Normalize` and `Scale` use three operands: `sourceA` is MIN,
`sourceB` is VALUE, and `sourceC` is MAX. Older JSON without `sourceC` loads it
as `"0"`. Source operands follow the comparison operand rules and can be
entered in the editor as a tag, timer/counter numeric member, or invariant-
culture numeric literal. Ordinary INT and DINT destination conversion rounds a
finite REAL value to nearest-even before clamping to the signed 16-bit or
signed 32-bit range. REAL destinations retain the finite double-precision
result. `Truncate` is intentionally different: it discards the fractional part
toward zero before the declared destination conversion is applied. `Convert`
uses the destination's declared numeric type; `Round`, `Ceiling`, and `Floor`
produce integral-valued results using nearest-even, toward-positive-infinity,
and toward-negative-infinity rules respectively.

A negative SQRT source, LN at or below zero, ASIN/ACOS outside -1 through 1,
a negative EXPT base with a fractional exponent, or zero raised to a negative
exponent are rejected by validation with `VC009`. DIV follows the STEP 7 V21
S7-1200/S7-1500 zero-divisor result: INT
and DINT write zero, while REAL writes `2143289344`, except REAL zero divided
by zero writes NaN; ENO remains true. MOD writes the defined error value zero,
drops ENO, and publishes `VC_RUNTIME_MOD_ZERO` when its divisor is zero; this
applies to both literal and runtime-resolved zero divisors. A negative runtime
square-root source similarly
preserves the destination and publishes `VC_RUNTIME_DOMAIN`. The same domain
rule applies to LN, ASIN/ACOS, and EXPT. Non-finite results publish
`VC_RUNTIME_NUMERIC` and also preserve the destination.

`Normalize` computes `(VALUE - MIN) / (MAX - MIN)`. `Scale` computes
`VALUE * (MAX - MIN) + MIN`. Both require MIN below MAX. Literal invalid ranges
fail validation with `VC009`; dynamic invalid ranges preserve the destination
and publish `VC_RUNTIME_RANGE`. The TIA workbench presents these as NORM_X and
SCALE_X. The Studio 5000 workbench presents a CPT-equivalent simulator macro
and does not claim a native Logix scaling instruction or vendor status flags.

## Block calls

CALL/JSR is a network output instruction referencing a stable block ID.

```json
"call": { "id": "call-sequence", "targetBlock": "sequence" }
```

When rung power is true, the target block's networks execute immediately in
document order before execution returns to the caller's next network. A false
call rung skips the target. Missing entry/target blocks and direct or indirect
recursive call cycles are compile errors. This deterministic inline model does
not imply Siemens instance-DB behavior or Rockwell parameter semantics.

## Conditional return

RETURN/RET is a network output instruction with a stable element ID.

```json
"return": { "id": "return-sequence" }
```

When rung power is true, execution exits the current call frame and resumes at
the caller's next network. When rung power is false, execution continues with
the next network in the same block. If RETURN executes in a task entry block
without a caller, it ends only that task's current invocation; later due tasks
still execute in deterministic priority order. The runtime uses explicit
execution-frame boundaries, so RETURN cannot accidentally discard a caller or
another task. This simulator behavior does not claim Siemens block-return
parameters or Rockwell controller/routine compatibility. In particular, it
does not model TIA RET's selectable return value (`RLO`, `TRUE`, `FALSE`, or a
BOOL operand) that is supplied to the caller through ENO.

## Block-local jump and label

JMP and LABEL/LBL are mutually exclusive network output instructions in the
current shared schema. Both carry stable element IDs; their symbolic name is
local to the containing block/routine. The Siemens canvas presents LABEL in
its reference-backed position as a compact marker above the destination
network, not as an EN/ENO box. The schema still cannot combine that marker
with a separate coil or box action in the same network, even though TIA can;
that is a known structural limitation rather than claimed parity.

```json
"jump": { "id": "jump-finish", "targetLabel": "finish" }
"label": { "id": "label-finish", "name": "finish" }
```

A true JMP discards the remaining networks in its current execution frame and
continues at the matching destination. A false JMP falls through. Labels do
not redirect execution by themselves. Empty names, duplicate declarations, and
targets absent from the same block/routine fail validation with `VC014`.
The Siemens-facing editor additionally applies STEP 7's LABEL grammar: one or
more ASCII letters, optionally followed by digits. It inserts `Label1` by
default and rejects number-first, underscore, whitespace, and other
special-character forms atomically. This check is intentionally an editor
rule; the shared JSON/runtime model preserves existing Logix and legacy names.
Cross-block jumps are not supported. A 10,000-network-per-scan watchdog stops
the offline controller, drives BOOL and numeric outputs safe, and publishes
`VC_RUNTIME_JUMP_LIMIT` if backward jumps form a non-terminating loop. This
shared simulator behavior does not imply vendor project compatibility.

## Task and scan semantics

At each base scan boundary, the runtime determines which tasks are due and
orders them by priority and then document order. Each due task begins at its
declared entry block. Networks execute in document order, CALL/JSR targets
execute inline, and a true JMP may advance or rewind within its current
block-local frame. RETURN/RET observes the execution-frame rules above. A coil
write is immediately visible to
later nodes/networks in the same scan. Series children evaluate left-to-right;
parallel children evaluate top-to-bottom. The engine still evaluates every
child so every element receives an observable state.

Every timer advances in exact configured base-scan increments and clamps at its
preset. TON done becomes true on the scan that reaches the preset. TOF done
clears on the off-delay scan that reaches the preset. TP done is true for its
scan-quantized pulse duration and does not extend while its input remains true.
Stop clears elapsed, timing, input-edge memory, and done state for TON, TOF,
and TP. TONR/RTO instead preserves elapsed and done across Stop while clearing
its active timing/input indications. Controller Reset clears every timer.

The schema contains no scripts, expressions, assemblies, physical addresses,
or transport configuration. Unknown kinds and malformed values fail parsing or
validation rather than being ignored.

Simulator force state is runtime-only and is never serialized into this
program schema. Only declared BOOL input and output tags are eligible. An input
force overrides the sampled simulator input image before Ladder execution; an
output force overrides the calculated output image after Ladder execution.
Stop always de-energizes outputs even when an output force remains visibly
armed, and Reset clears every force. Memory, TIMER, COUNTER, INT, and REAL force
requests are rejected. These semantics do not represent, connect to, or write
a physical PLC force table.
