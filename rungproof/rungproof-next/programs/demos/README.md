# RungProof Ladder Demonstration Projects

These five `.rpproj.json` files are reloadable offline Ladder projects. Open
them from **File -> Open Ladder Agent Project...**. Each project carries its
associated `sourceSceneId`; RungProof requests that scene before Verify + Load.

| Demo | Project | Focus | Associated scene |
|---|---|---|---|
| 1 | `01-press-count-starter.rpproj.json` | Rising edge, CTU, output coil | Lab 4.1 Press-Count Lamp |
| 2 | `02-delayed-lamp-timer.rpproj.json` | TON timer, `.Q` status, output coil | Lab 5.1 Delayed Lamp |
| 3 | `03-conveyor-sequence.rpproj.json` | Seal-in, NC interlocks, photoeye edge, CTU | Conveyor Stop |
| 4 | `04-batch-process-blocks.rpproj.json` | FB/FC calls, rising detection edge, CTU, type/count permissives, DINT count publication with MOVE | Lab 9.11 Pallet Counting |
| 5 | `05-integrated-cell-multi-fb-fc.rpproj.json` | OB plus two FBs, two FCs and two presentation DBs; calls, TON pickup dwell, CTU completed transfers, rising edges, comparisons and ADD | Lab 11.13 XY Palletizing |

Demo 5 is intentionally an integration exercise. It keeps the FB/FC names
visible in the Project objects tree and combines safety permissives, sequence
control, a 750 ms vacuum pickup dwell, four completed carton transfers, and layer reporting.
The DB members are presentation declarations; runtime values use the matching project tags.
All five remain simulator-only: no physical PLC transport, address mapping, or
live I/O is included.
