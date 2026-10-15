# RungProof Ladder Demonstration Projects

These five `.rpproj.json` files are reloadable offline Ladder projects. Open
them from **File -> Open Ladder Agent Project...**. Each project carries its
associated `sourceSceneId`; RungProof requests that scene before Verify + Load.
The descriptions below record the current authored projects, not a claim that
their machine-control architecture has passed the expanded PLC-ownership
acceptance gate in `docs/TIA_PORTAL_PARITY_AUDIT.md` and
`docs/DEMO_FUNCTIONAL_ACCEPTANCE.md`.

| Demo | Project | Focus | Associated scene |
|---|---|---|---|
| 1 | `01-press-count-starter.rpproj.json` | Rising edge, CTU, output coil | Lab 4.1 Press-Count Lamp |
| 2 | `02-delayed-lamp-timer.rpproj.json` | TON timer, `.Q` status, output coil | Lab 5.1 Delayed Lamp |
| 3 | `03-conveyor-sequence.rpproj.json` | Seal-in, NC interlocks, photoeye edge, CTU | Conveyor Stop |
| 4 | `04-batch-process-blocks.rpproj.json` | FB/FC calls, rising detection edge, CTU, type/count permissives, DINT count publication with MOVE | Lab 9.11 Pallet Counting |
| 5 | `05-integrated-cell-multi-fb-fc.rpproj.json` | Main sequence plus parameterized X/Y/Z axis FB instances and a vacuum/gripper FB, each with a distinct instance DB; TON pickup dwell, CTU completed returns, comparisons and MOVE state transitions | Lab 11.13 XY Palletizing |

Demo 5 now combines explicit machine Enable/Start/Stop/Reset and simulated-trip
inputs with a PLC-owned four-carton sequence. Main advances `sequence_step` only
from simulated encoder, position, attachment, release, and home feedback. Three
calls to `FB_AxisDrive` use distinct X/Y/Z instance DBs; `FB_VacuumGripper` has a
fourth instance DB. Six direction outputs and the vacuum output drive a plant
model that only integrates commands and reports measurements; it no longer owns
a palletizing phase enum or accepts a whole-cycle command. This is verified
offline simulation architecture, not live Siemens execution or safety approval.

All five remain simulator-only: no physical PLC transport, address mapping, or
live I/O is included. Any E-stop/safety-trip exercise is instructional
simulation of safety status and restart inhibition; it is not a safety-rated
function.
