# Lab 10.1 - Drive Alarm-Code String help

Scene ID: `lab-10-01-drive-alarm-code-string`

## Operation

Editable loaded STRING ladder matches complete drive alarm codes in received text; validity and Reset gate the alarm outputs. Offline symbolic message fixtures.

## Start conditions

- Verify + Load the supplied editable STRING ladder.
- Run, enable MESSAGE VALID, then cycle received messages.

## Sequence

- F003 matches the default selected_alarm_code; F030, F0030, prefixes, lowercase and empty messages do not.
- Edit selected_alarm_code in the tag table, Apply then Verify + Load to select a different code.

## Expected results

- The live display shows received STRING text; alarm_code_found and both alarm lamps are derived by the loaded program.

## Stop and Reset

- Stop removes BOOL commands and freezes equipment. Reset restores empty received text and the declared selected code.

## Expected I/O to operate this scene

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `drive_alarm_string_valid` | `BOOL` | **PC** | `False` |
| `alarm_code_found` | `BOOL` | **PLC** | `False` |
| `alarm_reset` | `BOOL` | **PC** | `False` |
| `drive_alarm_active` | `BOOL` | **PLC** | `False` |
| `alarm_match_valid` | `BOOL` | **PLC** | `False` |
| `drive_alarm_text` | `STRING` | **PC** | `` |

## Editor exercise

Select `selected_alarm_code` in PLC tags. Change its initial STRING value from F003 to F030, Apply, then Online > Verify + load offline. Run and repeat the messages: F003 must no longer match; F030 must match. Undo, Verify + load and Reset to restore the original.

`CODE MATCH` is a local RungProof instruction. It compares case-sensitive complete alphanumeric tokens, not substrings: F0030, XF003, f003 and an empty message do not match F003. Invalid selectors diagnose and return false. `alarm_code_found` is now a PLC-owned derived output; it is no longer a manual Boolean stand-in.

## Boundary

All values and clocks are local simulator behavior. No physical PLC transport, vendor STRING encoding/ENUM ABI parity, fieldbus reception, safety function or commissioning approval is claimed.
