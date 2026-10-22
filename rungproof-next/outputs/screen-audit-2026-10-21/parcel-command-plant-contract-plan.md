# Lab2.23 full command-driven parcel plant: proposed contract

No application changes yet. Existing3-parcel preview prescribes which parcel moves, exact queue holds and routes; executing it from START would recreate hidden PLC logic. Keep it as explicit legacy geometry preview only.

## Current evidence and interface gap
Native START now delivers conveyors_runTrue but queue remains stationary. Existing I/O provides one common belt command, one primary angle target, and three upstream classification bits. It lacks centered/clear/aligned transfer feedback and cannot independently index the infeed while holding a loaded primary table. A clean full controller needs these distinctions rather than private elapsed-step assumptions.

## Minimal explicit interface extension
- Retain PLC conveyors_run for transfer/takeaway drives; add PLC infeed_run for the infeed drive. Neither request/action directly mutates an actuator.
- Retain PLC route_position target0/.5/1 for primary0/45/90deg. Model a bounded rotary actuator with explicit speed; it reaches a commanded position but never chooses that position from parcel size. Secondary table remains the authored45deg transfer fixture unless a separate actuator is required by actual geometry.
- Add PC primary_table_loaded (parcel centered and stopped), primary_table_clear (no parcel intersects transfer deck), primary_table_aligned (actual angle reached target), and transfer_inhibited/misroute_detected diagnostics. If learner needs them, add PC per-lane receiver presence/parcel_delivered events from physical geometry. All names/types/initial values must enter scene and help before fixture authoring.
- Preserve current large/medium/small_detected one-hot semantic classification, derived from actual parcel intersection with the three finite height beams (highest interrupted beam), not scheduled metadata. Raw beam state may be separately visualized; no size decides plant routing.

## Mechanics and geometry
1. Derive belt/table support rectangles/disks and parcel hulls from delivered meshes. Integrate each parcel on its currently supporting belt only when its actual actuator command permits. Prevent overlap by bounded contact/queue inhibition; do not advance a parcel merely because a previous timed step completed.
2. Infeed stops only via PLC command/contact inhibition. At centered primary deck and belt off, rotate parcel with actual table motion. Never slide/teleport an unsupported corner across the gap. Loaded/clear feedback follows actual hull/support intersection.
3. Transfer only along the actual table outlet when aligned to a physically contiguous belt. Route0 reaches large straight lane;45deg follows diagonal transfer→secondary table→medium lane;90deg reaches small lane. The geometric outlet, not class, chooses trajectory. Unsupported/mid-angle transfer holds and reports inhibition without changing PLC commands.
4. Receiver occupancy and sorted_count derive completed arrivals once per parcel; wrong-lane arrival reports misroute. CycleComplete is passive all3 received correctly, with no stop command generated. LAD must stop the drives explicitly.
5. AppStop freezes actuator/part/rotation state; Run follows current LAD outputs. Reset restores all3 parcels, table home, zero counts/feedback. Belt visual travel follows actual command even when a parcel is held against a mechanical/contact boundary.

## Controller fixture and verification
Learner LAD handles START latch, infeed indexing, table-loaded stop, class latch, angle target/alignment wait, transfer, primary-clear wait and final drive stop. No sequence in plant. Pure mechanics tests cover support boundaries/contact inhibition, sensor geometry, no motion with outputsOff, invalid angles, no hidden route choice, retained one-event arrivals, Stop/Reset. Built-app UI must exercise each actual receiver route, table-centered pause/rotation, wrong/missing command and active Stop/recovery. Implement only after root accepts/adjusts this explicit interface and B3 compile snapshot finishes; no blanket completion claim from request/motor flags.
