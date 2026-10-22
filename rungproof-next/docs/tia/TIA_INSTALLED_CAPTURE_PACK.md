# Installed TIA capture pack

Updated: 2026-10-21

This checklist collects the external evidence that local RungProof automation
cannot create. It is not a commissioning procedure and does not require a live
machine. Use an isolated test project or PLCSIM where the required state can be
produced safely. Do not connect, download, force, or modify a production PLC to
obtain parity screenshots.

The authoritative instruction requirements and official Siemens links remain in
`../TIA_INSTRUCTION_EVIDENCE_MATRIX.md`. This pack standardizes the matching
installed-TIA side of each comparison.

## Session record

Record these values once for every capture session:

| Field | Required value |
| --- | --- |
| TIA Portal version and full build | Exact installed version/build, not only “V17” |
| CPU target | Exact CPU family and order number used by the project |
| CPU firmware | Exact configured firmware |
| Execution source | PLCSIM, test PLC, or offline editor only |
| Windows display scaling | Percentage |
| TIA theme | Light/dark and any custom theme |
| LAD editor zoom | Exact displayed percentage |
| Operand representation | Symbolic, absolute, or both |
| Tag information | Hidden, shown, or hierarchical |
| Tag-information position | On operand or below network |
| Project archive identity | Read-only archive name plus SHA-256 |
| Operator | Person collecting the evidence |
| Date/time | Local time and time zone |

If TIA does not expose an exact value, record `not exposed`; do not infer it.

### 2026-10-21 installed V17 offline session

| Field | Recorded value |
| --- | --- |
| TIA executable file/product version | `1700.0.4302.1` |
| Installed product | `Siemens Totally Integrated Automation Portal V17` |
| Project | `Winters_TIA_Bench.ap17` |
| CPU shown in project tree | `CPU 1512SP-1 PN`; order number and configured firmware not exposed in these captures |
| Execution source | Offline editor only |
| Safety boundary | No Save, download, connection, force, PLCSIM, or PLC action |
| Captured objects | Project View; expanded PLC project tree; `Pulse_Cycle_DB [DB6]`; Default tag table; Watch table 1 |

Accepted capture hashes:

| File | SHA-256 |
| --- | --- |
| `tia-v17-project-view.png` | `AA99F7A33CF338DC7DBD448581A6ABD45C0988A2926D1B9244AE7E5732893FE5` |
| `tia-v17-instance-db-properties.png` | `990A09BB6A283A1EEE31189F4EC3240C061CA29FE38F8A3AAE39CFB13DB18D14` |
| `tia-v17-plc-tag-table-properties.png` | `0AC9E4E2B69E60BA843D90D596869F762FA5423DC88CD4E8E7A8E713A66E3EA4` |
| `tia-v17-watch-table.png` | `26A9C00E86608E25A67CB523A86E2E7B49293254BDAE9F2D719BCFC913DFA7A9` |

These files are stored in
`C:\Users\matt.winter\Documents\Codex\2026-10-20\rungproof\outputs`.
They establish the installed V17 side only; same-object/same-state pairing to
RungProof remains open.

## Required fixture rules

- Use one declared symbolic operand per visible pin so placement is unambiguous.
- Use deterministic names shared with the paired RungProof fixture.
- Record declared types and addresses before capturing the instruction.
- Give IEC timers/counters explicit instance DB identities visible in the project
  tree and instruction call.
- Keep the entire instruction, its left and right conductors, network header,
  and relevant project-tree selection visible.
- Do not crop away a pin, type selector, instance identity, diagnostic marker,
  or online-status legend needed to interpret the state.
- Capture offline-unselected before selection or online highlighting changes the
  presentation.
- Capture edit states only after opening the exact production editor gesture
  named in the matrix. Record the gesture separately from the screenshot.
- For monitoring, record the input image, prior internal state, current internal
  state, destination before/after, diagnostic, and expected ENO/Q/RLO result.

## File naming and manifest entry

Use:

```text
tia-<version>-<cpu>-<key>-<state>-z<zoom>-<tagmode>-<sequence>.png
```

Examples:

```text
tia-v17-1512-project-view-offline-z100-symbolic-01.png
tia-v17-1512-db-declarations-offline-z100-symbolic-01.png
tia-v17-1512-watch-table-offline-z100-symbolic-01.png
```

For every accepted file, add its SHA-256, session record, matrix key, gate,
fixture identity, and paired RungProof artifact to the evidence registry. Never
replace a file in place; reject or supersede it explicitly.

## Capture session A — bit logic and topology

Capture these keys together at 100% zoom in offline-unselected, selected, edit,
and applicable monitored states:

- `no`, `nc` — FALSE and TRUE operands plus skipped/unknown network;
- `edge-rising`, `edge-falling` — signal and dedicated memory operands, primed,
  pulse, held, and skipped/unknown states;
- `coil`, `set`, `reset` — false/true RLO and skipped/unknown states;
- `branch`, `rung` — flat, root-parallel, nested-parallel, open branch before
  closure, selected connection, and monitored true/false/unknown flow.

Repeat one representative contact and one representative box at 75% and 150%
to establish scale behavior. Do not extrapolate those scale results to all
families until RungProof's universal renderer sweep passes.

## Capture session B — timers and counters

Capture:

- `ton` — released, accumulating, done, held, and skipped/unknown;
- `tof` — input true, falling/off-delay, expired, and skipped/unknown;
- `tp` — rising pulse, input falling during active pulse, held input, expired,
  and skipped/unknown;
- `rto` — accumulating, paused, resumed, done, reset, and skipped/unknown;
- `timer-reset` — unpowered, powered reset, selected, edit, and
  skipped/unknown;
- `ctu` — reset-dominant, rising count, held input, done, and skipped/unknown;
- `ctd` — load-dominant, rising decrement, held input, zero/done, and
  skipped/unknown.

For each IEC instruction also capture the associated instance DB in the project
tree and the instruction's instance/type edit state. Record whether observed
Q/CV/ET behavior comes from PLCSIM or a test CPU.

## Capture session C — comparisons and numeric boxes

Use the exact supported RungProof subset only. Capture all six numeric comparison
operators and every native numeric key `numeric-0` through `numeric-23`:

- offline-unselected with every formal pin visible;
- selected without obscuring pins, operands, type selectors, or conductors;
- type/operand edit state;
- enabled normal result;
- disabled result;
- skipped/unknown result;
- each documented local fault/domain/overflow state named in the matrix.

For `TRUNC`, `CONVERT`, `NORM_X`, `SCALE_X`, `ROUND`, `CEIL`, and `FLOOR`, show
both type selectors. For `EXPT`, show its fixed REAL base and selected exponent
type. Do not capture unsupported Siemens types as evidence of RungProof support;
record them as limitations instead.

## Capture session D — program control and project tree

Capture:

- `call` — named FC, parameterized FC, FB instance, explicit instance DB,
  formal/actual binding edit, enabled, disabled, and skipped/unknown;
- `return` — no-value and BOOL-value edit plus taken/not-taken monitoring;
- `jump` and `label` — edit, taken/not-taken/skipped flow, destination network,
  and LABEL not-interconnected presentation;
- empty instruction box — empty, chooser open, populated instruction;
- project tree — single-click selection versus double-click activation for OB,
  FC, FB, supported Global DB, generated instance DB, tag table, and watch table;
- creation, rename, delete, and undo/redo states for every supported object type.

Record unsupported TIA object types, access modes, columns, multi-instance forms,
and parameter-instance behavior without implying that RungProof implements them.

## Acceptance review

Each capture is accepted only when:

1. the session metadata is complete;
2. the exact instruction/control key and state are unambiguous;
3. the source project/archive identity is recorded;
4. the file hash is recorded;
5. the paired RungProof fixture uses the same operands, types, state, zoom, and
   display settings;
6. differences are documented rather than explained away;
7. a reviewer marks the comparison accepted, rejected, or blocked.

Installed-TIA capture completion does not establish live-machine suitability,
safety validation, or commissioning acceptance.
