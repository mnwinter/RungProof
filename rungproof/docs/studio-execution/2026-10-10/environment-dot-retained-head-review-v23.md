# Dot retained-head source and actual-render review V23

## Current disposition

Bounded ACCEPT for five actual V23 routed-head captures; crossing is OPEN pending its corrected V23b capture. No all77 approval. Dot individually opened the five images below and independently rehashed each PNG against identity sidecar, checked unchanged-during-capture and passed retained-control sidecar. Exact V23 DLL: E7129AFC86BCE69755FE279A43AF68425318E6928C6962A8E2D0585917091C68. Every image is after normal Reset; its event-history accepted action is historical, and the displayed false points correctly represent Reset rather than a failed click.

| Actual image under .tools/studio-verification | SHA256 | Finding |
|---|---|---|
| v23-gallery-switch.scene.png | 86284FE15E97170C6778FFF481F15A4FBA0EEDFA48C6DF76807023BA791C7919 | Unobstructed green head, START clearly readable; supported post/base and nearby exhibition equipment visible. |
| v23-gallery-estop.scene.png | 545698B3EEB912C3465255A314A9C18EA02BB3220DD788644AE7F36F530145FB | Full red/yellow head and EMERGENCY STOP plate readable, no table obstruction of target. Separate actual primer and stop trace: gallery_animation false→true→false, emergency_stop false→true; other safe key industrial_fan_run=false. |
| v23-gallery-selector.scene.png | 5A9DA2057B64BDCE85F90BAE5533B0F823C43DBC90FA5B0345FA9F8BB8820DEE | Actual knob and 0/1/2/3 digits clear; head not occluded by tank. Post extends behind lower HUD. No semantic function caption is present; this gallery illustration deliberately routes toggle-gallery, not detent state. |
| v23-drill-left.scene.png | 8FAB774F055DA79BE260B0D3991C86297B3C06DC29F48F4A10EF5C5B064AF13D | Head and support unobstructed; LEFT HAND decipherable but small (~5–6px ink), not comfortable wording-readability proof. Other hand mostly behind sidebar. |
| v23-drill-right.scene.png | A0068DB255A161924F7CC7EEC008A227C270C6724300B9CF2B5D847B5F1BB254 | Head and support unobstructed; RIGHT HAND similarly small. Does not establish simultaneous visibility of both hands. |

## Source and evidence scope

Read Main.RetainedControlCapture.cs, Main.RetainedControlNavigation.cs, early retained branch in Main.UserReadoutCapture.cs and wrapper guards. QA exact six scene/equipment/action/type/owner/initial contracts are guarded. Gallery safeState three keys are explicit; crossing action is pulse despite toggle-prefixed identifier. Diagnostic UsesExternalClock=true/playback-held is reported, actual commands routed through Viewport.PushInput, not direct ExecuteAction. Normal frames observe transient feedback and authored home; selector correctly has no press travel. Supporting roots/housing, camera/aperture and non-target points are checked; Reset validates every authored initial from a substantive changed target (gallery stop resets emergency_stop=true).

Navigator uses actual ordinary right/middle/wheel gestures, finite event cap, visible head mesh bounds, 48px maximum projected head size and 12px full head aperture margin. No direct camera pose/FOV setters. Rendered occlusion must still be independently assessed because TryPick tests declared control meshes, not arbitrary foreground occluders. Maximum head dimension proves neither its minimum clickable dimension nor face-label ink size. Thus the drill wording limit above remains honest.

The retained early branch skips generic readout navigation's negative HUD check and textual extent/readability gate. These six captures do not independently prove HUD rejection or full identification-text extent; existing separate input gating evidence cannot be mislabeled as this target-specific assertion. Wrapper excludes diagnostic station view for UserReadoutNavigation; retained CLI branch itself precedes its general mixed-camera guard. Treat direct unsupported mixed CLI as outside this qualified isolated wrapper invocation, not evidence of product behavior.

V23 crossing failed before dispatch with exact error `Retained capture: head front faces away`, sidecar passed=false and no PNG. Root diagnosed navigator binding-root +Z versus checker's actual head +Z; corrected navigation normal must use actual head. Failed evidence must remain historical. A previous progress message mistakenly called the present crossing sidecar PASS; corrected after reading it. No crossing visual or route acceptance yet.

## Boundaries

These are five exact scripted routes under an explicit held plant, not gallery motion, traffic timing, real two-hand safety, physical OS interaction, all controls/layouts or general discoverability. Existing current conveyor route remains separate. Human material approval is not held; intermittent Handle root cause remains OPEN. Comfortable drill words may require a closer ordinary gesture capture rather than a product label rewrite.

## V23b actual crossing — visual BLOCKER

Dot individually opened v23b-crossing-request.scene.png and independently verified PNG hash BB77AFFF4A8C74620407FB4BA24E076F5BCB5C3CD03D09C4D6E8C47967802603 against its unchanged DLL536482D561F6C5B2EBDE27F89874CBDBDD014941A201A04CBCB7CCADE0700C3A identity. Retained-control sidecar passed263: actual crossing_request false→true pulse, non-target outputs unchanged, cap motion/home and normal Reset. These bounded command checks are accepted.

Visual acceptance FAIL: the entire scene aperture is an opaque gray surface. No head, housing, CROSS lettering or field station is visible. This is exactly why bound-mesh TryPick/front/aperture math cannot establish rendered occlusion. Camera may be behind/inside another surface, but the cause is not proved by this PNG. Crossing remains OPEN until a different ordinary gesture route produces an unobstructed actual target image. Do not mark this native PASS as visual PASS. Five V23 images retain their own older DLL identity; V23b does not relabel them.

## V23c supersession

Crossing visible target and exact routed pulse/Reset now bounded ACCEPT in environment-dot-crossing-collective-review-v23bc.md. Prior V23b visual FAIL remains historical; do not relabel its gray image passing. New candidate/hash are recorded separately.

