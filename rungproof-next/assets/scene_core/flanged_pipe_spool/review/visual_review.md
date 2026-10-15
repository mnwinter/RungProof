# Flanged process pipe spool review

Review date: 2026-09-18

Disposition: **candidate only — not admitted to production**

Reference class: a supported, flanged horizontal process-pipe spool with open
end bores, bolted flanges, two saddles, a top instrument branch, branch flange,
gauge stem, and analog pressure gauge.

## Defects found and corrected

1. The original gauge body sat above the branch flange without a visible
   connecting member. A dedicated stainless gauge stem now joins the gauge to
   the flanged branch continuously; source, delivery GLB, collision output,
   thumbnail, and review images were regenerated.
2. Generic four-angle renders could hide either flange face or the unsupported
   gauge gap. Dedicated flange, branch/gauge, saddle, underside, scale, and
   wireframe views were added.

## Full-resolution inspection

| View | Result | Finding |
| --- | --- | --- |
| `hero.png` | Pass for candidate | The assembly reads as supported process piping, not a roller or conveyor component. |
| `left_flange.png` | Pass for candidate | Open bore, bolt circle, flange thickness, pipe-to-flange joint, and nearby saddle are visible. |
| `right_flange.png` | Pass for candidate | Opposite-end bore, bolt circle, pipe joint, and support path are visible. |
| `branch_and_gauge.png` | Pass for candidate | Branch neck, flange, new gauge stem, gauge body, face, and needle form a continuous instrument path. |
| `saddle_support.png` | Pass for candidate | Pipe rests on a formed saddle above a post and foot; no camera-hidden floating support is present. |
| `underside.png` | Pass for candidate | Both feet, posts, saddle undersides, and branch-to-pipe attachment are inspectable. |
| `scale_reference.png` | Pass for candidate | One-meter reference establishes the approximately 3.6 m spool scale. |
| `wireframe.png` | Pass for candidate | Smooth pipe and branch topology plus discrete flanges, bores, bolts, supports, and gauge construction are visible. |
| `blind_review.png` | Pending independent review | Context-free image is prepared; no author self-score is used as independent recognition. |

## Production blockers

- Independent context-free identification of the asset family at confidence
  0.80 or higher must be saved in `review/independent_recognition.json`.
- Reinspect after any branch, flange, support, or gauge geometry change.
