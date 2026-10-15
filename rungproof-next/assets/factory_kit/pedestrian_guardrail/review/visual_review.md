# Pedestrian guardrail visual review

Review date: 2026-09-22

Disposition: **independent recognition passed - eligible for strict production gate**

## Asset boundary

This is a static, surface-mounted pedestrian-separation guardrail. It models
the visible rails, toe board, posts, base plates, and anchors only. It does not
claim a design load, vehicle-impact rating, code compliance, or site-specific
anchorage performance.

## Source corrections

- Rebuilt the former linework-only assembly with distinct galvanized base
  plates, four washered anchors per post, base gussets, rail-to-post joints,
  rounded caps, and manufactured edge radii.
- Corrected the evidence renderer to scale its camera from source bounds after
  the initial fixed-distance frame cropped the full four-metre assembly.

## Evidence inspection

| Evidence | Result | Finding |
| --- | --- | --- |
| `hero.png` / `blind_review.png` | Pass | Both posts, two rails, toe board, caps, feet, and anchors are visible as one complete barrier. |
| `base_and_anchors.png` | Pass | Surface plates, gussets, washers, and four anchors per post are separately inspectable. |
| `upper_rail_and_joints.png` | Pass | The upper rail, post caps, and restrained rail terminations are visible. |
| `opposite_side.png` | Pass | The complete assembly remains coherent from the reverse view. |
| `scale_reference.png` | Pass | The one-metre reference establishes the authored four-metre class. |
| `wireframe.png` | Pass | Rails, posts, toe board, bases, gussets, caps, washers, and anchors remain independently inspectable. |

## Independent acceptance

A separate source-blind reviewer identified the asset as a floor-anchored
pedestrian safety guardrail/walkway barrier at 0.93 confidence and accepted it
for industrial-simulator use. The reviewer noted that it can also read as a
low-impact vehicle guide without contextual signage and that the contrasting
black caps are slightly stylized; neither issue prevents correct recognition.
