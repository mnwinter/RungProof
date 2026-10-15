# Pneumatic product pusher review

Review date: 2026-09-18

Disposition: **candidate only — not admitted to production**

Reference class: a floor-mounted, guided pneumatic product pusher with a
1.35 m stroke. The reusable assembly contains a fixed pneumatic body, rear and
front caps, valve manifold, air ports/tubes, fixed guide shafts, bearing
blocks, a moving rod/carriage/pusher plate, and a review-only carton witness.

## Defects found and corrected

1. The prior model and scene controller used a short 0.30 m travel despite the
   catalog name and original scene specifying a 1.35 m stroke. The model base,
   guide rails, rod, carriage envelope, product witness, catalog bounds, and
   runtime motion contract were rebuilt around the authored 1.35 m value.
2. The original controller translated the rod as a loose part. The pusher
   controller now holds the rod at the cylinder front cap, grows it along its
   axis, and translates the carriage members together.
3. The first detailed review hero was too distant to inspect because the floor
   size polluted the camera framing. The render pipeline now frames from only
   the mechanical asset bounds.
4. The first runtime capture bound zero motion nodes because Godot used its
   older imported GLB. A forced reimport refreshed the delivery hierarchy;
   runtime now reports nine bound `KIN_pusher_*` nodes.

## Full-resolution inspection

| View | Result | Finding |
| --- | --- | --- |
| `hero.png` | Pass for candidate | The assembly reads as a guided pneumatic pusher, not a conveyor or generic ram. Cylinder, fixed guides, carriage plate, manifold, and transfer rails are visible. |
| `cylinder_and_valve.png` | Pass for candidate | Front/rear caps, cylinder body, two air ports, routed tubes, reed sensors, and two-solenoid valve manifold are mounted and visible. |
| `carriage_and_guide.png` | Pass | The moving plate, cylinder rod, guide shafts, crossmember, brackets, and guide-end locknuts translate through fixed bearing blocks as one constrained carriage. |
| `transfer_alignment.png` | Pass for candidate | End-on view shows the plate square to the carton witness and centered between transfer rails. |
| `underside.png` | Pass for candidate | Base plate, pedestal supports, manifold mounting, rail underside, and carriage clearance are visible. |
| `scale_reference.png` | Pass for candidate | One-meter reference cube establishes the 3.10 m overall base and 1.35 m working envelope. |
| `wireframe.png` | Pass for candidate | Smooth rod/cylinder topology and distinct brackets, supports, rails, and fastener geometry are inspectable without black polygon facet edges. |
| `state_stopped.png` | Pass for candidate | Retracted rod/carriage stops at the cylinder front cap and leaves the transfer path clear. |
| `state_running.png` | Pass for candidate | The rod grows from the front cap and the nine carriage members advance over the full 1.35 m stroke. |
| `blind_review.png` | Pass | A separate context-blind reviewer identified a bench-mounted pneumatic linear pusher with guided carriage at 0.93 confidence and found no major physical or visual defect. |

## Runtime evidence

The migrated `scene-2-conveyor-pusher` runtime was captured at stopped and
running states after reimport. The controller reports `PneumaticPusher bound 9
nodes`; the two captures visibly differ by the full carriage extension.

The prior blind-review round was rejected despite 0.84 recognition because its
tubes, guide constraints, and product support were physically ambiguous. The
source revision terminates both air tubes at modeled manifold fittings, moves
guide shafts with the carriage through fixed bearing blocks, ends the guide rods
at the plate, and supports the review carton on a transfer deck excluded from
delivery. The revised round passed at 0.93.
