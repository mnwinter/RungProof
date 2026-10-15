# Vertical Process Tank — candidate visual review

## Identity and disposition

Candidate only. The unlabelled blind image is recognizable as a vertical
process tank with a roof manway, guarded roof, external ladder, sight glass,
bottom outlet, and four supported feet. No independent review record has been
submitted, so this asset is not eligible for production promotion.

## Inspected views

- `hero.png`: coherent vertical-tank silhouette; manway, roof guard, sight
  glass, outlet, and four-foot support arrangement are legible.
- `roof_manway.png`: continuous top and midrails, posts with base mounts,
  bolted manway cover, hinge, and handle are present.
- `ladder_cage.png`: ladder rails, rungs, and cage hoops are continuous.
- `sight_glass.png`: external tube, scale, upper/lower isolation fittings,
  and the liquid path are visibly connected.
- `outlet_nozzle.png`: flanged outlet has a visible bolt circle and does not
  terminate in free space.
- `legs_and_underside.png`: four structural legs, base plates, and anchor
  bolts are visible; no unsupported parts were found.
- `scale_reference.png`: one-metre cube used for the 3 m x 5 m tank scale
  check.
- `wireframe.png`: curved shell, roof, and flanges use smooth high-density
  topology; no visible facet-edge artifact.
- `state_stopped.png` and `state_running.png`: the bright liquid column is
  bottom-anchored and grows from 42% to 56%, matching the runtime level rule.

## Corrections made during review

1. Added the missing continuous roof midrail.
2. Added outlet bolt circle and foot anchor bolts.
3. Replaced the opaque cyan sight-glass tube with a transparent tube and an
   opaque, illuminated liquid column. The prior tube hid level changes in the
   normal simulator camera.
4. Restored continuous tank behavior in the migrated runtime: symbolic inlet
   and drain commands update level, pump rotation, switches, transmitter,
   indicator, and `KIN_liquid`. The runtime contains no PLC address or transport
   write path.

## Runtime evidence

`tank-level` contract verifies a one-second inlet-pump rise from 42% to
49.05833333333316% and 10.72 to 11.849333333333306 mA, then verifies Stop holds
that level and clears both symbolic commands. Live captures are held in the
ignored build output and were manually inspected after Godot reimport.

## Remaining blocker

This remains a candidate until a real independent, context-free reviewer
provides `independent_recognition.json`; no self-generated file is substituted.
