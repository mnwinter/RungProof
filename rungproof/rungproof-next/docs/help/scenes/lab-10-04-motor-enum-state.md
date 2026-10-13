# Lab 10.4 - Motor Operating-State Enum help

Scene ID: `lab-10-04-motor-enum-state`

## Operation

Editable loaded ENUM state machine with Stopped, Running and Fault states, Stop/fault priority, latched fault reset and fresh Start requirements.

## Start conditions

- Verify + Load the supplied editable ENUM ladder.
- Release Start, Stop and Fault before Run; Start requires a fresh rising request.

## Sequence

- Start changes motor_state from Stopped to Running; the shaft follows motor_running derived from that named state.
- Stop returns Stopped. A fault enters Fault and remains latched after the fault input clears.
- Clear the fault input and press FAULT RESET, then release and press Start again.

## Expected results

- The live state display and motor_state watch show the actual loaded ENUM value.
- Changing the Running transition MOV to Stopped, then Verify + Load, prevents the motor from running on Start.

## Stop and Reset

- Stop clears motor_running and sets output motor_state to the first declared member Stopped; held Start does not restart.
- Reset clears controller memory, named state, fault latch and scene inputs.

## Expected I/O to operate this scene

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `start_request` | `BOOL` | **PC** | `False` |
| `stop_request` | `BOOL` | **PC** | `False` |
| `fault_active` | `BOOL` | **PC** | `False` |
| `motor_running` | `BOOL` | **PLC** | `False` |
| `state_valid` | `BOOL` | **PLC** | `False` |
| `reset_request` | `BOOL` | **PC** | `False` |
| `motor_state` | `ENUM` | **PLC** | `Stopped` |

## Editor exercise

Select the MOV instruction in the State Running network and change the quoted member `"Running"` to `"Stopped"`. Apply, Verify + load, Run, release and press Start. The named state must stay Stopped and the motor must remain off. Undo, Verify + load and Reset to restore.

The `motor_state` ENUM declares the exact members Stopped, Running and Fault. The loaded ladder retains that named state; `motor_running` is derived from equality to Running. Fault dominates Stop and Start. A fault remains latched until its input is clear and Fault Reset is pressed. A Start held across Stop or fault recovery cannot restart the motor.

## Boundary

All values and clocks are local simulator behavior. No physical PLC transport, vendor STRING encoding/ENUM ABI parity, fieldbus reception, safety function or commissioning approval is claimed.
