# Single start-pushbutton pedestal station visual review

Review date: 2026-09-21

Disposition: **independent recognition passed - eligible for strict production gate**

## Asset boundary

This is deliberately a start-only, pedestal-mounted command station. It is not
presented as a complete machine-control panel and it does not claim a live PLC
connection, energized circuit, or safety function.

## Source corrections

- Replaced the loose front-side cable/gland that could be mistaken for a second
  control operator with a rear-entry gland and protected conduit close to the
  hollow support.
- Replaced the oversized white plate with a compact black START legend above
  the sole green operator, and separated it from the fascia fasteners.
- Rebuilt the enclosure/support transition as a bolted mounting flange on a
  larger square pedestal and anchored base plate; the former diagonal visual
  artifact is no longer part of the delivered model.
- Retained one exported `KIN_pushbutton` member for the catalog's declared
  linear 18 mm press travel. The travel views are offline visual evidence only.

## Evidence inspection

| Evidence | Result | Finding |
| --- | --- | --- |
| `hero.png` / `blind_review.png` | Pass | The green START operator, enclosure, pedestal, flange, and four floor anchors read as one start-only station. |
| `button_and_legend.png` | Pass | The retained ring, green operator, compact legend, and independent fascia fasteners are separately visible. |
| `pedestal_and_base.png` | Pass | The wider base, anchors, square support, and enclosure mounting flange show a credible load path. |
| `rear_and_entry.png` | Pass | The rear gland and protected conduit enter the hollow pedestal instead of forming an unsupported flying lead. |
| `state_stopped.png` / `state_running.png` | Pass | The evidence pose moves only `KIN_pushbutton` through the declared press travel; it is not a live-machine claim. |
| `scale_reference.png` | Pass | A one-metre reference establishes the authored station scale. |
| `wireframe.png` | Pass | Enclosure, face, legend, pushbutton assembly, flange, support, anchors, and rear-entry hardware remain inspectable. |

## Independent acceptance

A separate source-blind reviewer identified `blind_review.png` as a
pedestal-mounted industrial green START pushbutton station at 0.94 confidence
and returned PASS for professional simulator catalog use. The reviewer noted
that a single static overview cannot prove momentary/maintained or illuminated
behavior and that its framing could be tighter; the catalog's `pressed` signal,
linear kinematic contract, and stopped/running evidence define the intended
simulator behavior without claiming installed live wiring.
