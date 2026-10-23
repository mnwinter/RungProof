# Bounded native handoff observer: offline review

Helper: parcel_handoff_probe.py. SHA25621302c39214ebd1b0a973149d5a90f22cd68ac9bd9d75b56adfa8150c4bc2486.

This agent tested saved PNGs/parser/deadline/error guards only. No bridge endpoint was called and no native input was sent. Root alone may invoke live mode while retaining sole UI ownership.

Live invocation from repository root:

    python rungproof-next/outputs/screen-audit-2026-10-23/parcel_handoff_probe.py --session <PRIVATE_SESSION_JSON> --prefix parcel-v7-medium-stop-r1 --seconds 40 --start-x <GROUNDED_X> --start-y <GROUNDED_Y> --require-route-home

Root must first visibly reset/run through the current App Playback menu, confirm count0, expose the required lower Signals rows and leave the menu CLOSED. The optional Start coordinates must come from current native pixels. Omit both Start arguments only when Start has already been performed and root deliberately wants observation-only arming.

Fresh Engineering layout uses AppPlayback(203,34), then Stop simulator scan(220,90). The old bottom AppStop(113,646) is clipped in this layout and must not be reused. The helper always arms the reviewed menu after optional Start, captures it, requires OCR-visible Stop menu text, then requires advancing actual header scan numbers while that menu stays open. It does not operate Run, Reset, navigation, scrolling or the viewport.

Each polling observation retains the unedited1021x660 original, static IO(700,128,1018,445)2x crop, static header(760,0,1021,55)3x crop, OCR outputs and SHA256 values. The initial menu crop also has explicit native coordinates/provenance. A single persistent hidden PowerShell OCR worker handles only already saved images inside this audit tree and is always closed/terminated by the helper. Native bridge11/PID/canvas/DPI guards and the2026-10-23T11:00Z cutoff are checked before every UI request/capture. Input requests are never retried after uncertain delivery. The session URL is never printed or written to public evidence.

AppStop is sent ONCE only after a single OCR image simultaneously supplies exact rows sorted_count1, infeed_runFalse, primary_table_loadedFalse and table_transfer_runTrue, plus route_position0 when the recommended flag is supplied. Count10, missing rows and contradictory duplicate rows do not match. A match older than1.5seconds is rejected. After Stop the helper captures immediate and1.5s-held native states, then leaves the app stopped. Root must inspect those images and directly confirm retained qa_step/qa_class through LAD/Watch before crediting a medium-phase test.

CRITICAL: public-row match is not phase/class proof. The actual-source V7 trace contains qualifying route0 windows at phase30/class2 scans535-575 AND phase30/class3 scans814-908. Exact windows are preserved in parcel-v7-public-predicate-ambiguity.json. Starting observation before physical Start favors the first window, but missing it cannot establish medium acceptance. Result status explicitly retains PHASE_CLASS_UNPROVEN. Timeout is UNOBSERVED, not an application failure.

Offline verification performed:

- parser/deadline/nonlocal-session/strict-no-overwrite/count10/missing-row/contradictory-row guards PASS;
- actual saved complete-lower-Signals original parsed count3, infeedFalse, loadedFalse, transferFalse, route0 and header scan12119; no stop predicate;
- actual saved outbound frame parsed count1 but missing other cropped rows, so it did not match; no claim of a successful positive native predicate test;
- actual saved menu2x crop recognized Run simulator scan / Stop simulator scan / Reset simulator scene;
- every owned OCR worker exited0; no UI bridge call occurred.

Owned offline evidence folders have prefixes parcel-probe-offline-guards, parcel-probe-offline-complete, parcel-probe-offline-outbound and parcel-probe-offline-menu. Live runs create a unique new folder per invocation with actions.jsonl and result.json. Review originals before concluding Stop held actual physical motion; the helper deliberately makes no automatic motion/phase PASS claim.
