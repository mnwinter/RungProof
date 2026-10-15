# Four-position selector-station visual review

Review date: 2026-09-21

Disposition: **independent recognition passed - eligible for strict production gate**

## Source corrections

- Replaced the tall, unanchored generic cabinet with a compact floor-mounted
  operator enclosure, hollow structural pedestal, visible gussets, base plate,
  washers, and four anchors.
- Rebuilt the selector around `KIN_selector_handle`; the handle and its flush
  direction inlay are one hierarchy, with four -55 to +55 degree detents.
- Corrected the Godot selector binding to rotate around the physical front-face
  Y axis instead of swinging the mechanism out of the dial plane around Z.
- Replaced floating labels and the detached-looking red end-piece with flush
  numbered dial indices, matching radial detent ticks, and an in-handle white
  direction inlay.
- Retained the catalog signal as the PC-owned discrete `position` input only;
  this asset invents no PLC-owned command, physical address, or live-I/O path.

## Evidence inspection

| Evidence | Result | Finding |
| --- | --- | --- |
| `hero.png` / `blind_review.png` | Pass locally | Complete enclosure, four-position dial, supported pedestal, and anchored base are in frame. |
| `selector_dial_and_labels.png` | Pass | Flush 0-3 indices and radial ticks identify the four detents; the selected handle's white inlay remains inside the handle silhouette. |
| `pedestal_and_anchors.png` | Pass | Pedestal tube, enclosure flange/gussets, base plate, washers, and four anchors form a continuous support path. |
| `rear_enclosure_and_gland.png` | Pass | Rear enclosure and bottom cable gland are present without a dangling catalog cable. |
| `state_stopped.png` / `state_auto.png` / `state_running.png` | Pass | Position 0 and position 3 captures prove the handle and inlay rotate together from -55 to +55 degrees; `state_running.png` is the contract-named AUTO witness. |
| `scale_reference.png` | Pass | One-metre witness establishes the authored floor-station scale. |
| `wireframe.png` | Pass | Dial, handle, enclosure, supports, and fasteners have distinct, inspectable topology. |

## Independent acceptance

The final separate context-free review identified the unlabeled render as a
pedestal-mounted rotary selector switch/control station at 0.98 confidence.
It returned PASS with no major support, collision, scale, or rendering defect.
It noted only that a large real-world selector handle partially covers the
selected dial area; the flush numbered detents and direction inlay leave this
as a minor readability improvement, not a gate failure.
