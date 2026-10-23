# Demo3 constrained fit: feasibility and finite solver

Source/mathematical analysis only; no engine, model-state, UI, application/fixture or Git/D operation. Read with [camera decision note](camera-framing-decision-note.md) and [native failure](demo3-frame-process-visibility-review.md). An adaptive lens is a **conditional candidate**, not yet a demonstrated viable repair.

## What the current implementation actually does

`Main.FrameComposition`4611–4747 collects each visible selected mesh AABB’s eight **GlobalTransform-transformed** corners, chooses the world AABB center, profile direction/FOV and an aperture-scaled symmetric fit. Depth is discounted by Demo3’s `.22` allowance. It assigns `camera.Position`, calls `LookAt`, then `CaptureCurrentView(center)`. That last call immediately invokes `SceneCameraController.UpdateViewportOffsets`28–56. Consequently validating the unoffset assigned position is insufficient.

Let `C` be the actual selected world center, `n` the normalized center→eye direction, `R/U` the actual post-LookAt camera right/up basis, `d` distance, and `t=tan(FOV/2)`. Keep the orientation fixed. With current controller distance bookkeeping, the literal source offsets are

```
kx = 2*(viewport.Center.X - aperture.Center.X)/viewport.Width
ky = 2*(viewport.Center.Y - aperture.Center.Y)/viewport.Height
h = d*t*kx;  v = d*t*ky
E = C + d*n + h*R + v*U
```

`E` is the proposed effective projection eye, conditional on the offset convention being reproduced correctly. Camera-parent/local versus global handling must be explicit: Main currently assigns local `Position` using world points, while `CaptureCurrentView` mixes `GlobalBasis` and local `Position`. Current scene identity ancestry may make this work; an implementation must either assert that supported ancestry or transform consistently. Do not silently claim arbitrary transformed camera parents are supported.

**Important discriminator:** with ordinary vertical-FOV/KeepHeight pinhole math, horizontal pixels per world unit are viewport.Height/(2*d*t), whereas the literal HOffset formula above divides by viewport.Width. Therefore the usual “offset places target exactly at aperture center” identity does not follow algebraically from this source at non-square aspect. That is not a newly proven native bug: actual Godot offset/projection behavior must be checked using existing `UnprojectPosition(C)` after offsets settle. Replacing the shared formula from a textbook assumption would exceed this narrow repair. The solver must consume the actual same offsets/projection convention, then validate against the real camera. Existing target-centering verifier cannot establish non-occlusion or all-corner fit.

## Smallest exact search once conventions are established

The circularity is avoidable: **fix a trial lens first**, solve only distance, then vary the trial lens using a finite interval partition. No iterative “change FOV then recompute distance then hope offsets converge.”

For world corner `P_i`, calculate `x_i=(P_i-C)·R`, `y_i=(P_i-C)·U`, `z_i=(P_i-C)·n`. Each point has its own positive depth `q_i=d-z_i`. In the standard vertical-FOV convention, with full viewport center `(cx,cy)` and height `H`,

```
screenX_i = cx + H*(x_i - d*t*kx)/(2*t*(d-z_i))
screenY_i = cy - H*(y_i - d*t*ky)/(2*t*(d-z_i))
```

These expressions must be adapted if the actual projection discriminator contradicts that convention; do not redefine Godot semantics. Preserve nonzero viewport origin and actual settled logical aperture, not native pixel guesses.

1. Shrink the aperture by a declared visual margin. For a fixed `t>0`, impose left≤screenX≤right and top≤screenY≤bottom for **every** transformed corner. Also impose `d-z_i ≥ near+clearance` and the far depth bound. Multiplying by positive `t*(d-z_i)` produces linear inequalities in `d`; there is no mean-depth or `.22*halfDepth` approximation.
2. Transform the *candidate eye* into the environment’s published local frame, or transform each plane into the eye’s world frame correctly. Plane normals require the inverse-transpose rule under general affine transforms; transform points directly into room local space if that is simpler. Reusing `reserve.Position-Position` as a general transformed-room contract would be incorrect.
3. For each convex room interior plane, impose containment of `E` with clearance. At fixed `t`, `E=C+d*(n+t*kx*R+t*ky*U)`, so this is again linear in `d`. Include the near-plane rectangle corners (with their actual aspect/FOV offsets), not just an infinitesimal eye point, to keep the near plane out of a wall/ceiling. Finite wall inner faces and thickness—not their mesh center planes—define these limits.
4. Intersect all resulting distance half-lines with the supported distance range. A nonempty interval gives a feasible distance for this lens; choose an interior value with numerical slack, not a boundary grazed by floating-point error. Re-evaluate all inequalities in double precision and report worst slack/point/plane. An empty interval explicitly rejects this lens.

For a **minimal practical finite implementation**, a small deterministic set of acceptable lens candidates, ordered from35° upward, plus exact per-lens interval intersection is reliable **for the candidate set**. It cannot honestly report “no feasible lens exists” between samples. If that limitation is acceptable, label failure `NO_FEASIBLE_APPROVED_LENS_CANDIDATE`, not geometric impossibility. An acceptable maximum FOV is a product/readability decision; no source supports declaring65°,75° or90° automatically acceptable.

If continuous feasibility within a bounded approved lens interval is required, a small exact partition is available without a general optimizer. Every distance inequality has form

```
(a0 + a1*t)*d <= b0 + b1*t
```

The projected corner, room/near-plane and distance constraints all have this form with fixed orientation and aperture. Bound functions are linear-fractional in `t`. Partition the approved `t` interval at coefficient-zero roots and at pairwise lower-bound/upper-bound equality roots; cross multiplication gives at most quadratic polynomials. Within each open partition, coefficient signs and every lower-versus-upper ordering are fixed. Evaluate one interior trial there, plus admissible endpoints with tolerance. Handle zero/identical polynomials explicitly; reject inconsistent zero coefficients. Select the lowest feasible lens with robust interior slack, then exact distance. This finite sign/ordering partition avoids an unjustified monotonic-FOV binary search: offsets can make room containment worse while projection improves. Complexity is quadratic in constraint count, potentially large for thousands of redundant mesh corners; prefer the finite approved-candidate variant unless continuous minimality is actually needed. Deduplicate equivalent constraints without dropping real perspective-depth extrema.

## Why a distance clamp or wider lens alone can still fail

- Illustrative arithmetic only: a point at lateral5m and near-side depth offset8m needs `q=d−8`. At35° and d10, it projects with ratio5/(2*t), far outside a narrow aperture; the center-plane approximation uses5/(10*t) and understates the extent fivefold. A wall-safe d10 clamp does not fit it.
- Off-center aperture means nonzero `kx/ky`. At fixed d, widening the lens increases the **physical** lateral/vertical offset `d*t*k`, potentially moving the effective eye through a sidewall or ceiling. Thus feasibility need not improve monotonically with FOV.
- All points can fit in2D while the eye sits outside the room. Conversely, eye/near-plane inside the outer convex shell does not prove the internal material-entry portal or other opaque architecture leaves a useful sightline. Inner walls/returns are not removed by satisfying six outer planes.
- An acceptable complete-corner fit can be unusably wide, with tiny cabinet labels or severe edge distortion. “Mathematically feasible” is not “usable process framing.” If the minimum acceptable solution exceeds the approved lens/readability bound, report that failure and reconsider the candidate rather than widening indefinitely.

## Minimal future discriminating evidence and ownership

Before selecting the algorithm, record one settled actual Engineering layout’s viewport/aperture, chosen transformed corners, actual camera parent transform, unoffset pose/FOV, final H/V offsets, effective eye and target/corner projections. Compare `UnprojectPosition` and `IsPositionBehind` with the proposed analytic checks. Record relation to actual kind9 inner wall/ceiling planes and identify whether an opaque room/material-entry surface lies in the useful sightline. These would be new presentation diagnostics, not existing native proof or a reason to query private state during the screen audit.

The environment builder owns any proposed room-plane metadata: derive it from the same `reserve,c,w,dep,h`, `.18` wall thickness and actual transforms as meshes. It must not leak a plant point, collision/safety claim or persist into project data. Main owns the Demo3 Process fit result; camera controller retains manual navigation/offset bookkeeping. Candidate evaluation should not repeatedly call `CaptureCurrentView`: compute candidates without mutating live camera, commit final pose/lens once, call Capture once, then validate actual resulting projection. If it disagrees, fail verification and report residuals rather than adding a hidden convergence loop.

Explicitly restore nominal lens at entry to WholeScene, other scenes, asset preview and visual-review paths; Whole currently skips the demo-profile FOV assignment. An aperture resize reuses the explicit fit path after settled layout; manual orbit/pan/wheel must not trigger continuous auto-snap. Preserve the existing camera parent and direction in this narrow repair. Retain latest good pose on an infeasible fit with a visible verification diagnostic; do not silently hide room meshes, crop required equipment or pretend a fallback met all constraints.

## Conclusion

Adaptive lens remains plausible, but feasibility is conditional on three independent results: **correct actual projection/offset convention**, **joint corner/near-plane/room interval feasibility within an approved lens range**, and **useful unoccluded native process visibility**. The fixed-lens exact distance interval is the smallest dependable solver. A finite approved-lens list is likely sufficient for this single-scene repair if its honest failure semantics are accepted. Continuous minimum-FOV solving is available through finite constraint partitions, but should not become a general camera framework. No algorithm here has been run against actual engine state or accepted as a repair.
