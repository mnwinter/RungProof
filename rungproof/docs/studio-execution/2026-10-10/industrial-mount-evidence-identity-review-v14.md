# V14 bounded identity audit

Read-only verification against industrial-mount-source-freeze-v14.json, 2026-10-11. No build/engine/source changes.

| Check | Actual result |
|---|---|
| Current source against freeze |425/425 hashes match; no missing or changed inputs |
| Immutable light/display inventory inputs |2/2 hashes match |
| Current Debug DLL and frozen DLL | Both A17D152BC856EAAB7EDB8E1FD68582339BE30C024F530820AFCFE62422686A12 |
| Capture image/assembly/engine identity |22/22 matching; each sidecar reports assembly unchanged during capture |
| Capture views |14 operator,7 field-detail,1 field-reverse |
| Capture log diagnostics |0 ERROR/Exception/FAIL;22 warnings, one per capture: TAA only available using Forward+ renderer |
| Control-installations log | Terminal PASS77;0 ERROR/Exception/FAIL/WARNING matches |

Control terminal additionally records71 authored case scenes,163 functional picks,138 accepted presses,17 selectors,8 prerequisite-blocked actions,12 known non-head bindings,7 retained field bindings,36 projected indicators,4 complex indicator bindings,7 non-output indicators and21 bench lens/shelf checks. Its scope explicitly excludes keyboard and clearance. Exit status was not independently measured by this log-only audit.

Full per-input/per-image hashes, identities and capture times are preserved in industrial-mount-evidence-identity-review-v14.json; diagnostic classification is separately preserved in industrial-mount-evidence-log-detail-v14.json. The initial JSON's captureLogIssueCount22 includes warnings, not errors. No identity mismatches found.

Identity does not establish visual readability, continuous clearance, live PLC behavior or closure of the intermittent wrapper fault. Pickup MODE overlap remains a separate actual-image review finding. Handle OPEN; material75 HELD.
