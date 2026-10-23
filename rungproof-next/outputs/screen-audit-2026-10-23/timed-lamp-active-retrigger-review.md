# Timed Lamp Lab5.2 active retrigger review

Independent original pixels/action log; no reviewer input. PID12768/client1021×660, local runtime RUNNING, PLC disconnected. All four original SHA256 values match log. Existing frozen `../screen-audit-2026-10-21/test-projects/lab-5-02-timed-lamp-off.rpproj.json` declares start_pulse and TP timerPresetMs3000 (QA parameter), not a manually fabricated time-active input. Static declaration is not pulse-acceptance evidence.

| Original | Actual native state |
|---|---|
| [first pulse](timed-lamp-r1-first-pulse-on.png) | After physical START(527,265), green lamp ON; scan2174. |
| [second active click](timed-lamp-r1-active-second-pulse.png) | Same physical START coordinate, green lamp stillON; scan2231. |
| [expired](timed-lamp-r1-expired-before-second-interval.png) | No subsequent input; green lamp dark; scan2343. |
| [fresh after expiry](timed-lamp-r1-fresh-after-expiry-on.png) | Same START click, green lampON again; scan2362. |

The native log records one delivered START for each first/second/fresh click, with waits0.25s/0.1s/0.15s, and no inlineOCR. The second input is issued while the first pulse is visibly active. Expired scan2343 is112 scans after second-click capture2231, equal2240ms at displayed20ms. First capture→expired is169scans=3380ms. These actual scan differences are consistent with expiry of the original3000ms interval despite the active click, then valid fresh retrigger after expiry.

**Important qualification:** second-click capture is not the exact accepted-input scan. The four floor-view originals do not expose a momentary start_pulseTrue, accepted-action footer, Health event or TP private elapsed/edge state for that second click. Thus2240ms is proven from the second-click **capture**, not from an independently witnessed accepted pulse. There is760ms margin to3000; the log's0.1s post-click wait supports but does not replace a scan-level acceptance witness. No exact second accepted-pulse time is claimed.

Bounded result: visible active-click/nonextended-expiry/fresh-click behavior is observed; first and fresh clicks demonstrably work on that physical button. A fully strict claim that an **accepted** in-progress TP edge was ignored remains qualified by absent direct pulse acceptance evidence. No product failure demonstrated and no broader timing/curriculum acceptance inferred.

## R2 acknowledged physical route — Q20 bounded closure

Four additional originals were independently inspected and rehashed against `timed-lamp-r2-active-acknowledged-trigger.json` entries; all match. Same app/native client. The plan clicks the same visible START(471,305) three times, with no forces or simulator-state writes.

- [First acknowledged pulse](timed-lamp-r2-first-pulse-ack-on.png): scan9404, timed_lampTrue and actual green segmentON. Outer status explicitly ACTION ACCEPTED · PRESS START (ONE ACCEPTED SCAN) · MOMENTARY · start_pulse=TRUE.
- [Active acknowledged pulse](timed-lamp-r2-active-pulse-ack-on.png): scan9461, lamp stillTrue/green; same explicit accepted physical action status. Thus the active second press is acknowledged through the real UI route, rather than only transport delivery.
- [Off before retrigger deadline](timed-lamp-r2-off-before-retrigger-deadline.png): scan9572, timed_lampFalse and actual green segmentdark. This is **111 scans=2220ms** after the active acknowledgement capture (not2200ms), still below3000ms. First acknowledgement capture→OFF168scans=3360ms.
- [Fresh acknowledged pulse](timed-lamp-r2-fresh-pulse-ack-on.png): scan9590, timed_lampTrue and actual green segmentON, same accepted physical Start status.

Q20 closes for this normal UI-function scope: acknowledged START during an active TP does not extend the visible interval, and fresh START after expiry operates again. The sampled start_pulse row is alreadyFalse in all captures, consistent with a one-scan momentary action; its exact consumption scan/private TP memory is not directly sampled. That limitation does not require capturing a20ms peak to credit the actual acknowledged control path. No arbitrary timing permutation, exact wall-clock edge timestamp or wider lesson acceptance is claimed.
