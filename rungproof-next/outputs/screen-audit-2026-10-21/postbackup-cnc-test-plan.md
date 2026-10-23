# Post-backup CNC V3 native test plan

Read-only source derivation for B5. No runtime verification claimed. Use `fixture-new-binding-lab-2-24-v3.rpproj.json`; Reset, Verify/Load, Run, then the actual physical START. Do not modify scene feedback. Reset is needed before another part or after an interrupted cycle.

## Timing boundary

Nominal continuous accepted-scan total is about50seconds: actual pickup feed5.47s, all explicit robot segments29.89s at0.8m/s, independent access/clamp/cut waits7s, final outfeed5.87s, roughly1.3s phase/scan qualification. Quantization and actual IK error add a small margin. Expect completion within55seconds of uninterrupted, successful accepted simulation time. Use120seconds wall-clock as a generous QA wait bound.

There is **no unconditional finite maximum**: a rejected IK pose, physical interlock, Stop, missing command or failed permissive intentionally holds a phase indefinitely. A timeout is evidence of a stall needing diagnosis, not permission to advance state or synthesize completion. Wall time may exceed accepted simulation time.

## Five useful native frames

|Frame|Approximate elapsed from physical START|Required actual evidence|
|---|---|---|
|1|Before/start|One blank on infeed; `start_cnc_request` route accepted; infeed actually begins. Record baseline stock actualXYZ, `blank_at_pickup`, inhibitionFALSE.|
|2|13–15s|Blank held above/from pickup, `robot_grip_confirmed`TRUE; actual joint/tool and stock movement. This covers approach/down/grip/lift, not just a spinning robot.|
|3|24–26s, preferably grounded by actual `cnc_run`TRUE|Same blank seated on vise, robot parked/clear, doors actually closed. `stock_on_vise`, `cnc_clamp_closed`, `cnc_access_is_closed`, `robot_clear_of_enclosure`, `cnc_cut_permitted` TRUE; actual spindle cutting. The cut lasts only2s, so use one fast bounded visible wait if time sampling misses it.|
|4|36–39s|Same machined blank released on supported outfeed, `stock_on_outfeed`TRUE, grip confirmationFALSE, robot withdrawing. Access/robot pose must agree with actual geometry.|
|5|55s or actual exit|Blank near outfeed endX6.2; `part_at_exit`TRUE, `machining_complete`TRUE, `cycle_complete`TRUE; requested infeed/outfeed/robot/cnc/grip/access/clamp outputs allFALSE on following scan. No second blank or teleport.|

These times are sampling hints only. If a phase is visibly later, retain its actual frame and follow measured feedback; do not label a missing intermediate frame a failure. Five scene frames plus bounded visible condition capture are sufficient; avoid a huge full-frame movie/OCR dump.

## Signals that explain a stall

- `robot_actual_x/y/z` versus PLC `robot_target_x/y/z`: actual IK progress; `robot_at_target` must be measured within0.3mm. `stock_actual_x/y/z` proves the carried part follows actual pose; nominal grip offset alone is not evidence.
- `cell_motion_inhibited`TRUE: inspect requested target/actual access and support. Enclosure entry with closed access, clamp while robot inside, unsupported release, invalid workspace/IK, or cut without actual clamped stock/clear robot must inhibit actual forbidden motion.
- `blank_at_pickup`TRUE but no grip: V3 pickup X consumes `stock_actual_x` because the physical leading-edge beam is crossed before blank centre reaches beamX. Check actual targetY1.07/tool-to-stock alignment, not a fixed assumed pickupX.
- `stock_on_vise`TRUE but no cut: inspect clamp closed, access closed, robot clear and `cnc_cut_permitted`; `cnc_ready` is initial cell readiness, **not** cut permission.
- If all scene geometry holds with `cell_motion_inhibited`FALSE, inspect private `qa_step`, `qa_scan_step`, `qa_last_step` in normal Ladder monitor/watch UI. They are learner memory, not scene Signals. Stages10infeed;20access;30–60pickup;70–120load/seat/release;130–160withdraw/clamp/close;170cut;180–220open/unclamp/regrip;230–280unload/release;290–320withdraw/close;330outfeed;0done.

Independent normal-UI unsafe-command tests require a separate temporary learner draft and additional evidence; do not call the normal five-frame cycle proof of every guard or robot/hardware safety. This post-backup note does not change the backed-up application, fixtures or tests.
