# Guarded hydraulic scissor-lift table visual review

Review date: 2026-09-21

Disposition: **independent recognition passed - eligible for strict production gate**

## Source corrections

- Replaced the visually disconnected horizontal actuator with an inclined,
  pin-centred cylinder, rod, clevises, and drive lug on the front lower
  scissor plane.
- Added base rails, travelling upper rails, rollers, retained pivots, and a
  detailed hydraulic power pack so the support and drive paths are explicit.
- Replaced partial warning rails and a failed fixed cage with a four-sided
  expandable bellows skirt. The skirt is a continuous pinch-zone barrier from
  base envelope to travelling deck, with reinforcing bands parented to it.
- Bound both the upper mounting rails and `KIN_lift_bellows_*` to the existing
  scissor-lift motion controller; the controller translates/scales the skirt
  about its centre as the platform rises.

## Evidence inspection

| Evidence | Result | Finding |
| --- | --- | --- |
| `hero.png` / `blind_review.png` | Pass locally | Continuous four-sided black/yellow bellows, deck, and base read as an enclosed lift platform. |
| `guard_and_platform.png` | Pass | Deck-to-skirt and skirt-to-base interfaces are complete without a visible side opening. |
| `hydraulic_and_guides.png` / `drive_side.png` | Pass | Hydraulic power pack, inclined actuator, pin centres, scissor arms, rails, and guide rollers are inspectable. |
| `internal_cutaway.png` | Pass | Evidence-only cutaway exposes the two-stage arm, pivot, actuator, and rail arrangement; it is not production geometry. |
| `state_stopped.png` / `state_running.png` | Pass | The platform rises through the two-stage geometry while the four bellows sections expand to retain coverage. |
| `scale_reference.png` | Pass | One-metre reference establishes authored industrial table scale. |
| `wireframe.png` | Pass | Deck, rails, scissor arms, actuator, power pack, and guarded envelope remain separately inspectable. |

## Independent acceptance

The final separate context-free review identified the exterior as an enclosed
scissor-lift table at 0.95 confidence. It returned PASS with no visible major
guard discontinuity, unsupported platform, exposed pinch mechanism, or major
geometry defect. It noted only minor chunky/puckered corner transitions on
the bellows reinforcement bands.
