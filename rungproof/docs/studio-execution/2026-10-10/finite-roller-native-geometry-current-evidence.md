# Finite roller native geometry evidence

Status: IN_PROGRESS; proposed optics installation has a newly identified stationary obstruction. Production integration is held for a corrected optical contract and native rejection evidence. Earlier all-pass reports omitted stationary-metal occlusion and do not establish optical feasibility.

Root ran `finite-roller-geometry-build-v2.log` (zero warnings/errors) and `finite-roller-geometry-native-v2.log` (exit 0, terminal failures=0). JSON: `.tools/studio-verification/finite-roller-geometry-native-v2.json`. Exact DLL SHA256: `354B01C50E2E059D5A32F49C8717FE687E9BB84196C8ED1F93EB9173AE18679B`.

Measured 31 delivered rollers, six grounded feet, crown Y=1.053 m, pallet center range [-1.2625,+1.2625] m and guide clearance 0.0675 m. All three runners intersect actual roller crown rays at every 5 mm sample. This is sampled contact, not yet proof of continuous support or physical bearing capacity.

Temporary body/lens yaw correction points toward the original reflector; bracket bounds still contact and the original cable reaches the corrected body. Real wood triangles generate both occupied intervals and gaps. Original delivered asset hashes remain unchanged.

Geometry-only analytic ramp experiments at 20/40/100 ms produce supported settled centers -1.0325/-1.0415/-1.0325 m, leaving 0.230/0.221/0.230 m before the model boundary. The beam is clear at these settled positions. These experiments do not execute a learner controller, prove accepted runtime ordering, or establish observer equivalence.

V1 failed while writing evidence because Godot's loaded assembly Location was empty; the exception handler repeated the same error. Its log is retained. V2 hashes the existence-checked pinned DLL file and guarantees a terminal result even if fallback evidence serialization fails.

Dot found two substantive gaps: absolute axis alignment does not establish rotation sign, and 5 mm samples do not establish continuous runner contact. Technical art is strengthening those checks and preparing isolated geometry renders. No production workspace schema, menu, controller or delivered asset changes are approved by V2 alone. The existing two-room material review and wider color/texture rollout remain separate.

V3 strengthened the sign and continuous centerline support checks and terminated failures=0. Actual flat bottom sections are 0.139 m wide, narrower than their chamfered bounds, and their translation intervals continuously cover the finite center range. Derived local phase sign +1 produces negative-X crown velocity. These are bounded geometric point-contact results, not load capacity or full-width contact.

Root personally viewed actual home/entry/settled renders and two optics attempts. The first optics frame was clipped; the second corrected the framing but exposed a substantive geometry omission. Technical art inspected the frame and source: the guide rails span Y=1.105..1.165 m at Z=+/-0.585 m, while the proposed beam is at Y=1.150 m. The wood-only ray ignores those stationary metal solids. Exact stationary triangle rejection is being added; no raised sensor, invented cutout or transparent guide has been substituted. The original same-height yaw-only installation must not enter production on the earlier pass logs.

Root executed V5: build zero warnings/errors; native process exit 1, terminal failures=1. The failed check is `actual_optical_ray_clear_of_stationary_solids`. Exact imported triangles identify `FRAME_L_guide`, `FRAME_R_guide`, `SENSOR_photoeye_bracket`, and `SENSOR_reflector_bracket`. Native actual guide bounds are Y=1.045..1.165 m (superseding the preliminary source estimate above), and all four intersect the proposed Y=1.150 m ray. JSON/log: `finite-roller-geometry-native-v5.json` / `.log`. This expected rejection is preserved, not relabeled a pass.

Root selected read-only review of an existing standalone through-beam assembly as the next alternative. Raising the original solid brackets alone would retain their obstruction. A proposed separate station and supported height require exact asset facing, stationary clearance, contact/cable review, amended internal contract, and a new native experiment before production.

External-pair V1: build zero warnings/errors, native exit 1/failures=1. Actual opposed-face optical clearance and real wood-gap sensing pass. Geometry-only coast leaves 0.500/0.491/0.500 m of reserve for 20/40/100 ms, with beam clear at rest. Conservative retained-hardware overlap rejects the fixed tower spacing. A full-length solid chain guard intersects the raised transmitter post across every proposed X station, so an X-only search cannot repair this installation.

The next bounded experiment moves each complete transmitter/receiver tower outward by 0.12 m in Z, preserving all named components and their relative geometry. The post's inner surface moves from Z=0.6925 to 0.8125 m, compared with the chain guard's outer surface Z=0.76 m. This proposed clearance is a datum-based hypothesis, not a completed native qualification. Grounded pedestals follow measured shifted feet; actual optical endpoints must be recomputed. Delivered assets and production behavior remain untouched.
