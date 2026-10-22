# Final backup readiness — root owns Git operations

Deadline22:04:11UTC. Snapshot21:49UTC; source+curated-evidence backup is ready for root review/staging. No staging, commit or push was performed by coverage support.

## Exact files and refresh command

- `rungproof-next/outputs/screen-audit-2026-10-21/backup-proposed-staging-paths.txt`: current explicit1305path proposal, approximately97.2MB. All140 current dirty/untracked source/doc/config paths are included.
- `rungproof-next/outputs/screen-audit-2026-10-21/backup-final-readiness.json`: machine-readable counts/size, log-copy coverage and scan result.
- `rungproof-next/outputs/screen-audit-2026-10-21/backup-capability-redaction-manifest.json`: selected-text localhost capability scan.
- `rungproof-next/outputs/screen-audit-2026-10-21/backup-evidence-logs/manifest.json`: ignored build/test logs preserved as committable text copies with original/copy hashes.

After B5 and final report edits complete, run from the repository root:

```powershell
& './rungproof-next/outputs/screen-audit-2026-10-21/refresh-backup-manifest.ps1'
```

This output-only script refreshes current source paths, reports, plans/manifests/fixtures, compact review images, native new-plant213/216/219/223/224 key captures, and every existing remediation log (including B5 as soon as present). It never stages/commits/pushes or edits raw history. At this snapshot B4 logs are included; B5 logs do not yet exist. Root should rerun once near final staging so new readiness/final-report files and B5 logs are captured.

## Backup scope and sensitive data

Canonical repo verified `C:\Users\matt.winter\Documents\RungProof`, branchmain, origin `https://github.com/mnwinter/RungProof.git`. Cached HEAD/origin matched3b67ed1534ca11c468acd55558ced9096421ccd1 at initial audit; root verifies current remote before push. Mixed earlier dirty work is deliberately visible, not misattributed to this audit; user authorized the current-work backup.

Current proposal scans1035selected text files for active or old capability URLs at arbitrary localhost/127.0.0.1 ports. Zero unredacted capabilities remain. Original SESSION-STATE.json is excluded; sanitized snapshot is included. If a future selected file contains a capability, the refresh script writes a sanitized Git copy and excludes the raw original, recording provenance; raw local history remains intact. Earlier14credential-pattern hits were embedded task-UUIDs, no unexplained matches or sensitive filename candidates. This is a bounded screen, not a formal secret-audit guarantee.

No toolchains, generated .godot/bin/obj/build caches, or bulk raw capture archive is proposed. Raw audit output was approximately2.275GB at the initial inventory; most raw screenshots remain local. The proposed backup is source+curated evidence, not a complete raw-artifact backup. Never claim all raw captures were pushed unless separately backed up. All selected files are below2MB; no GitHub100MBfile concern in this set.

## Quality grade and honest remaining limits

| Area | Current grade |
|---|---|
| Catalog breadth |77/77inventoried and entry-reviewed. |
| Functional audit | INCOMPLETE; saved ledger67bounded normal/5remaining legacy routes+5demoPARTIAL. No full-scenePASS; latest native closures must be reconciled at final handoff. |
| Source/build | B4 clean build0warnings/0errors;422PASS/3FAIL, including confirmed CNCsupport offset being corrected forB5 plus existing watch-schema/TIA hash failures. B5 outcome pending; do not predict success. |
| Native evidence | New door normal/NC/Stop/freshClose/Reset/repeat and drill complete/hand-loss captures are selected. Jug flow remains in progress at root's last report; review credit is separate. |
| Git backup | Proposal ready; root must inspect stagedstat/secret screen, create authorized backup commit and push before deadline. No commit/push success is claimed here. |

Current functional plan: [SCREEN_AUDIT_REMEDIATION_PLAN](../../docs/SCREEN_AUDIT_REMEDIATION_PLAN.md). Audit evidence: [coverage-ledger](coverage-ledger.json), [scene issue index](scene-issue-index.md), [deduplicated findings](provisional-issues.md), [remediation evidence](remediation-evidence.md). Final report should identify actual commit/push result, remaining failures and raw-artifact backup limit.
