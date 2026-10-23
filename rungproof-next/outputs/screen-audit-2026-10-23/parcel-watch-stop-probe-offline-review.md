# Watch-qualified native Stop probe

`parcel_watch_stop_probe.py` is a root-invoked, bounded UI observer. This agent tested saved images and parser/error guards only; it did not contact the screen bridge or operate the application.

The current r3 helper SHA256 is `a412c324b26740ab641be9f83e9dd9a8b970f84c7da308fe6d8325fe85b3e9c8`. The first live-attempt source is preserved as `parcel_watch_stop_probe.r1.py.txt`, SHA256 `fd84492fc149e77f22040d4d705a39ac5f9cf9254b657af265f7011ebdcd257e`. The r2 source is preserved as `parcel_watch_stop_probe.r2.py.txt`, SHA256 `11e2a512b6db01144d555e133fadfa010b459f3471210b47db70be82b88092ed`.

## Preconditions and operation

Root must freshly verify the exact native geometry: 1021 by 660 client, current Engineering Start coordinates, Logic Editor button, bottom Watch tree row, and top Playback menu. The private session supplies the pinned app PID and bridge URL; the URL is never included in public evidence. The helper never selects Run, Reset, or Modify Value.

With optional explicit Start coordinates it sends that physical click once, opens Logic Editor, proves the actual Logic view header, double-clicks the reviewed Watch row, and scrolls to the reviewed bottom rows. It proves the MONITOR VALUE header and all three exact row names. Initial `qa_step` and `qa_scan_step` values must be readable; an unreadable initial `qa_class` stays null and is allowed only at this preflight.

It opens the reviewed Playback menu, verifies its Stop text, and requires native header scans to advance while the menu is open. Stop is sent once only if the same fresh native screenshot yields `qa_step=30`, `qa_scan_step=30`, and `qa_class=2`, with image age at the qualification check no greater than one second. The target requires all three numeric monitor values. It then saves immediate and held Watch images and exits, terminating its own OCR worker.

The crop includes only the Name and Monitor Value fields. It excludes Modify Value, addresses and comments. Names are matched exactly after punctuation normalization. Numeric OCR corrections apply only inside the monitor field. Values join a named row only within 18 pixels of vertical center in the 3x crop; competing unequal values make that row unproven.

Every original PNG, derived crop, OCR result and action witness is retained with hashes. Polling is at most 40 seconds, cleanup is bounded, and each capture/input checks the 2026-10-23T11:00:00Z cutoff. Missing context, expired freshness or timeout means the QA case remains pending; it does not establish an application failure.

## Offline evidence

- `watch-probe-offline-stall-1792730972333001500/result.json`: actual saved private Watch image has MONITOR VALUE context, scan4732 and values50/50/2; target does not match. OCR worker exits0.
- `watch-probe-offline-logic-guard-1792731018999382500/result.json`: actual saved header proves LADDER LOGIC PROGRAMMING. OCR worker exits0.
- `watch-probe-offline-row-association-1792731215088699800.json`: ten parser checks, including a synthetic positive30/30/2, missing each target value, shifted values, wrong-column values and conflicting values. Synthetic values are parser evidence, not native phase evidence.
- `watch-probe-offline-r2-preflight-1792731261941604100.json`: actual first live-attempt preflight OCR has all names and10/10/null. The r2 preflight accepts that context without inferring class0; missing a required name or either step value rejects. Actual50/50/2 remains negative.
- `watch-probe-offline-r3-menu-1792731362516110800.json`: actual r2 saved menu OCR reads the Stop row as `Stop sirnulator scan`. The r3 helper accepts that exact observed row alias alongside the two existing Stop aliases. Exact normalized row matching rejects unrelated menu actions, near spellings and prose containing a Stop phrase. Target and other guards are unchanged.

## Limits requiring root review

The first live attempt `parcel-v7-watch-handoff-r1-1792731173678113800` exited before opening the Playback menu because OCR omitted the initial class zero. It sent no Stop and is a QA false block. That evidence remains unchanged.

The r2 live attempt `parcel-v7-watch-handoff-r2-1792731284452001600` likewise sent no Stop: its menu OCR used the observed `sirnulator` spelling and the helper rejected the menu context. This is another QA false block, not an application issue. Its original evidence remains unchanged.

A target image can precede a transition before the OS Stop lands. Root must inspect immediate and held actual Watch values; a stopped later phase does not satisfy the phase30 interruption case. Watch qualification also does not prove the physical parcel pose or successful hold/resume: root must return to the scene, inspect actual pose/held state, then operate Run and verify completion. Native outcomes belong to the corresponding root-owned run evidence; this document makes no native Stop success claim.
