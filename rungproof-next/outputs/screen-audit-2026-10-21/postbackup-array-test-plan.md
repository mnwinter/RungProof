# Lab10.6 ARRAY / ten-zone native test plan

Read-only source/fixture/evidence assessment. Exact scene: `lab-10-06-ten-motor-array-startup`. No separate sequential-start scene or shipped sequential controller was found. Existing valid QA input is `fixture-lab-10-06-ten-motor-array-startup-v2.rpproj.json`; invalid-v1 is a preserved QA input error and must not be reused.

## Scope correction

The current scene/help explicitly says any timed sequence or alarm response must be authored in learner ladder, and **no automatic delay, alarm interlock or grader is implied** (`scene.json:633,644`). Existing V2 writes all10 ARRAY elements together from the three permissives; startup_sequence_active is merely the same BOOL conjunction. Therefore native full traversal is genuinely pending, but a claimed built-in sequential-start requirement is not supported by the current published contract. Sequential element gating is a useful additional learner exercise only after appropriate explicit ladder is provided; its absence is not an app defect or an extra unexposed control to test. Do not call simultaneous V2 commands sequential.

## Already evidenced versus remaining

`evidence-review-lab1006.md` records all8request/ready/alarm combinations; only complete permissives produce10TRUE ARRAY elements while motor_array_run and all10legacy motor_i_run remainFALSE. It records material through zones0..3, alarm-clear loss/restore, Stop/Run freeze/recovery, and Reset upstream. It explicitly does not establish all10zones/end or individual shaft animation. Reuse those truth-table/lifecycle observations; do not redo them as though absent.

Existing fixture gates every motor_commands[i] by group_start_request AND all_motors_ready AND group_alarm_clear. It contains no timers or phase state. No hidden sequence exists in the plant. Effective zone command is OR(group,legacy_i,array[i]); preserve group/legacyFALSE to establish ARRAY-only transport.

## Physical controls and minimal route

Use actual station face labels, never guessed screen order: switch_9 GROUP START ->toggle-group_start_request; switch_10 ALL READY ->toggle-all_motors_ready; switch_11 ALARM CLEAR ->toggle-group_alarm_clear. Reset/VerifyLoad/Run, then make all3TRUE by actual clicks. Ground each actual state; all10array commandsTRUE, group/legacyFALSE.

One uninterrupted route takes29.78seconds of accepted running time from Reset to the finite supported end. The0.60m carton begins model center0.30/worldX-6.70; all10zones are1.40m; speed0.45m/s. Final center13.70/worldX6.70, leading edge at worldX7.00. The model stops at the supported end; it does not drive the carton fully off the conveyor. There is no declared PLC-facing completion/exit point, so do not invent one. Allow35seconds accepted time or a generous60seconds wall-clock before diagnosing a stall. Pauses/permissive loss add to wall time.

## Ten physical sensing-line checkpoints

The zone_occupied_i fields are beam observations, not whole-zone presence or motor readiness. Sensor line is modelX0.18+1.40*i. Expected intervals below assume all commands remainTRUE; actual rendered lens offset/float rounding may shift timing slightly. Each pulse lastsabout1.33s, so use a small native capture/contact-sheet stream around these windows, or bounded visible waits if the needed row is actually visible. A final screenshot cannot prove all transient beam pulses.

|Zone|Expected TRUE interval(s)|Suggested single frame(s)|
|---|---|---|
|0|0..0.40|baseline already evidenced|
|1|2.18..3.51|already evidenced|
|2|5.29..6.62|already evidenced|
|3|8.40..9.73|already evidenced|
|4|11.51..12.84|12.2|
|5|14.62..15.96|15.3|
|6|17.73..19.07|18.4|
|7|20.84..22.18|21.5|
|8|23.96..25.29|24.6|
|9|27.07..28.40|27.7|

Lean remaining capture set: one ARRAY-only command baseline, six phase-aligned zone4..9frames, supported-end frameabout31s, and a later end-hold frameabout35s. Keep native originals, review a compact contact sheet once; no need for full-frame OCR on every tick. If rows4..9 require normal UI scrolling, keep route time referenced to actual command start; do not count dock scrolling as evidence of unseen pulses. Actual material progression along each physical zone is separate from command TRUE. For individual shaft/drum response, one short native clip or few spaced poses of visible drives is useful; command panel count alone is not motion proof.

A running upstream zone cannot push through a stopped overlapping/downstream zone: the finite-load model requires every overlapped zone to drive. V2 cannot isolate one element with existing3station toggles because it intentionally writes all10together. Test that independent command behavior only through normal learner editor changes/new QA fixture, never simulator state injection. No new fixture is created by this plan.

## Sequential learner demonstration boundary

If root elects the additional exercise, define its ladder timing/order explicitly and label them QA parameters, keep group/legacyFALSE, and observe motor_commands ARRAY partial prefixes plus corresponding physical shafts before reaching10/10. A downstream motor delayed too long can legitimately stop the carton at its boundary; this is documented plant consequence, not failed sequencing by the app. Reuse the existing group-start/ready/alarm controls as inputs; no new hidden operator route is needed. This plan does not assert an existing sequential program or a mandatory startup delay.
