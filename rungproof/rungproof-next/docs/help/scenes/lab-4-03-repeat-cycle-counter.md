# Lab 4.3 - Repeat-Cycle Counter help

Scene ID: `lab-4-03-repeat-cycle-counter`  
Migrated source: `prototype/scenes/lab-4-03-repeat-cycle-counter.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-03-repeat-cycle-counter.scene.json`

## Purpose

Count actual completed CNC dry strokes in a PLC counter, then stop a bounded
reference batch. The old manually toggled `cycle_count_complete` input is
removed. `cycle_done` now reports modeled machine completion; PLC logic owns
batch accumulated count and batch completion. This exercise starts with an
empty ladder editor. Author or explicitly open a compatible controller.

## Expected I/O to operate this scene

All points are symbolic; no physical addresses are declared here.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `cycle_request` | `BOOL` | **PC** | `False` |
| `machine_enabled` | `BOOL` | **PC** | `False` |
| `machine_home` | `BOOL` | **PC** | `True` |
| `machine_busy` | `BOOL` | **PC** | `False` |
| `cycle_done` | `BOOL` | **PC** | `False` |
| `head_position` | `REAL` | **PC** | `100.0` |
| `cycle_active` | `BOOL` | **PLC** | `False` |
| `cycle_complete` | `BOOL` | **PLC** | `False` |
| `batch_count` | `DINT` | **PLC** | `0` |

Authored I/O notes (retained from the prior table):

- `cycle_request` — Meaning: One accepted-scan START request
- `machine_enabled` — Meaning: Held manual process enable
- `machine_home` — Meaning: Z head at its raised home endpoint
- `machine_busy` — Meaning: Feed/dwell/return in progress, including a held partial stroke
- `cycle_done` — Meaning: Actual full feed/dwell/return complete; cleared by sampled low command
- `head_position` — Meaning: Head lift percent: home 100, work endpoint 0
- `cycle_active` — Meaning: Dry-cycle command; low holds a partial stroke
- `cycle_complete` — Meaning: PLC batch counter done indication
- `batch_count` — Meaning: PLC accumulated count published to COUNT display

## Operator actions

| Action | Type | Bound point |
| --- | --- | --- |
| `Start batch` | `pulse` | `cycle_request` |
| `Toggle machine enable` | `toggle` | `machine_enabled` |

START returns false after an accepted local input scan. Presses before that
scan coalesce into one high input. ENABLE is held until toggled again.
The former manual count-complete station is now labeled ENABLE.

## Equipment bindings

| Point | Equipment | Mode |
| --- | --- | --- |
| `cycle_request` | `switch_1` | `switch` |
| `machine_enabled` | `switch_3` | `switch` |
| `cycle_active` | `indicator_2` | `indicator` (amber) |
| `cycle_complete` | `indicator_4` | `indicator` (green) |
| `batch_count` | `batch_count_display` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `machine_0` | `machine` | Enclosed CNC dry-stroke station |
| `switch_1` | `switch` | Batch request button |
| `switch_3` | `switch` | Machine enable selector |
| `indicator_2` | `indicator` | PLC cycle command indication |
| `indicator_4` | `indicator` | PLC batch complete indication |
| `batch_count_display` | `trainingAccessory` | PLC completed-cycle count |

## Reference cycle and ladder

The chosen offline model feeds the complete connected Z head down 100 mm
in 1 s, dwells for 0.5 s, and returns up in 1 s. The prescribed spindle
rotation uses the asset's existing 3200 rpm value. These reference values are
not OEM specifications; this is a dry stroke with no stock removal, force,
tool wear or spindle acceleration model. The vise stock remains supported.
The scene-specific enclosure and coolant route clear this moving installation.

The focused verifier (`-- --audit-repeat-cycle --visual-scene-review`) writes
`.tools/plant-review-repeat-cycle.rpproj.json`. Open it through File -> Open
Ladder Agent Project to use the explicit five-rung QA reference; it is ignored
verification evidence, not a sixth demo or automatically loaded lab solution.

1. The CTU counts `cycle_done` rising edges with a reference preset of three,
   continuing the preceding counter lessons.
2. A START pulse seals the batch while ENABLE is true and the counter is not done.
3. `cycle_active` is commanded while the batch is active and `cycle_done` is false.
   One sampled low command acknowledges each return before another stroke.
4. The counter done state drives `cycle_complete`; MOV publishes its accumulated
   value to `batch_count`. The reference `publish_count` memory bit starts true.
5. Enable, Run and press START once. COUNT advances after actual returns to home:
   1, 2, 3. The reference then removes the cycle command and lights batch completion.
   A new START cannot add a fourth cycle; application Reset starts a new batch.

## Stop and safety boundary

Local Stop clears PLC output image and holds actual head/rotation; counter and
batch memory remain retained. Run republishes the count and resumes a retained
partial batch if ENABLE remains true. ENABLE loss clears the reference batch
latch and holds a partial stroke; restoring it alone cannot restart. A fresh
START resumes the held stroke. Application Reset clears the controller and
plant, restores raised home, disables ENABLE, and stays stopped at scan zero.
The numeric output display is zero during Stop even though CTU memory is retained.

The opt-in inspection bar supports Hold, Step 0.5 s and Step 20 ms, using the
same accepted controller/plant clock. It cannot step a stopped controller.
These checks do not prove real CNC behavior, a safety function, external PLC
transport, interlock certification or commissioning readiness.
