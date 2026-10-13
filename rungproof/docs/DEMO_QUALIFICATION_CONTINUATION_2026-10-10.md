# Offline demo qualification continuation

This supplements `DEMO_RELEASE_REVIEW_2026-10-10.md`. Release approval remains open.

## Confirmed repair and regression evidence

A flat imported project containing a 20,000-block CALL chain reproduced a process stack overflow during compilation. Compilation also occurs when opening a project; running the controller was not required. Recursive call-graph traversal was replaced with an explicit traversal stack. The isolated reproducer now completes and still rejects a cycle with diagnostic VC011. The full controller suite reports **164 PASS, 0 FAIL**. Its log reports no real PLC transport construction or connection attempt.

The formal scan draft now records this baseline defect as `availability.unbounded-callgraph-recursion` (CWE-674), low severity with high confidence. Low severity reflects operator-mediated local availability impact; no privilege escalation, code execution, remote listener or PLC access is established. The record separates original baseline source from the unpublished working-tree repair and repaired candidate. Scan coverage remains partial; the finding is not a completed whole-application security report.

Evidence: `rungproof-next/.tools/demo-release-review-20261009/deep-call-red-stderr.log` and `controller-qualification.log`. These are local generated evidence, not distributed product files.

The earlier ZIP is an immutable baseline and does not include this repair. The first repaired export in `rungproof-next/artifacts/offline-demo/local-review-20261010-callgraph-fixed` completed resource packing but left its headless editor running. That owned exporter was stopped; this output is diagnostic evidence, not an accepted build. Adding explicit `--quit` to the export invocation resolved the observed stall on the fresh export.

The repaired candidate is now `rungproof-next/artifacts/offline-demo/local-review-20261010-callgraph-fixed-quit`. Export exited 0 with no ERROR diagnostics. Its resource inventory contains 2,548 packed entries and rejects developer/test/authoring paths using the build lane's exclusion check. The candidate manifest records 199 runtime/document files and their SHA-256 hashes; all 199 were independently rehashed successfully. The actual exported offline-policy guard passed with zero bridge-factory calls. Its headless conveyor integration check also passed, including Start, sensor stop, early Stop and Reset. Shell/project guards passed, including all five demo selections, cross-scene save/open and retained drafts. The exported palletizer audit passed with zero failures across 2,576 samples, including complete layers and phase-specific interruption recovery. These checks do not establish visual acceptance or continuous mechanical clearance. The executable remains unsigned. Native workflow acceptance on this repaired artifact remains open.

`DEMO_ASSET_RIGHTS_INVENTORY_2026-10-10.json` records 295 production/candidate catalog entries, resource paths and explicit UNVERIFIED rights status with fields for ownership, license, provenance, permission and notices. This is a review inventory, not a rights determination or a complete embedded-resource inventory.

Provenance follow-up: all 295 entries have local source-model files and hashable delivery models. Exactly 256 match entries in the OEM-reference register. The inventory now includes those records, delivery hashes and possible modeling-script links based on asset-directory tokens. Token matches identify leads for inspection, not proof that a script generated the exact binary. All rights statuses remain UNVERIFIED. Validation confirmed 295 entries with no missing delivery hash/source file or accidentally promoted rights status.

Replacement internal ZIP: `rungproof-next/artifacts/offline-demo/RungProof-OfflineDemo-20261010-callgraph-fixed.zip`, 400,093,779 bytes. SHA-256: `D1BEEEB16491B8B66F4459A334AC0111C9F8F5986C481722E3A209253262D8DF`. All 199 manifest-listed files were independently rehashed directly from the archive and matched. The archive also includes local build-evidence logs; it remains an internal unsigned candidate, not an approved external release.

## Preflight repack qualification, 2026-10-10

Fresh candidate: `rungproof-next/artifacts/offline-demo/local-review-20261010-preflight-repack`. This copies the already-tested runtime from `local-review-20261010-callgraph-fixed-quit`; it is **not a new source export**. All 198 previously manifested files other than README retain their exact hashes. README was updated and `Verify-Package.ps1` added. The existing root `export.log` is now also manifested, yielding 201 listed files. Build-evidence logs remain outside manifest coverage.

ZIP: `RungProof-OfflineDemo-20261010-preflight-repack.zip`, **400,078,344 bytes**, SHA-256 **970E4653EB879D6DCDC41E679E94C25101807067D207D0D123AE322D2F3785DB**. Independent archive-stream hashing matched all 201 entries; all 207 ZIP entry names were unique. The read-only checker passed on the candidate and again after ZIP extraction into `.tools/demo-release-review-20261009/Extracted package with spaces preflight 20261010`. Its five fixture regressions passed. This verifies extraction/integrity with spaces, not executable launch with spaces, standard-account behavior or read-only installation.

The preceding repaired ZIP still hashes to `D1BEEEB16491B8B66F4459A334AC0111C9F8F5986C481722E3A209253262D8DF`. Both previous candidate directories and ZIPs are preserved. The new candidate remains unsigned and `distributionApproved=false`. No engine, GUI, screen capture, application input or PLC connection was used during repack qualification. Native acceptance remains deferred while the screen is in use.

`DEMO_ASSET_METADATA_EVIDENCE_2026-10-10.json` records read-only GLB metadata and matching delivery hashes for all 295 catalog models. All contain generator metadata `Khronos glTF Blender I/O v5.2.40`; none contains copyright metadata. The initial console summary incorrectly grouped temporary dictionaries and reported no generator; regrouping the saved JSON records corrected that summary. Three contain images; none declares an external image/buffer URI. Embedded image origin remains unverified. Exporter metadata does not establish creator identity, ownership or permission. All 295 rights statuses remain UNVERIFIED.

The three image-bearing models are the belt conveyor, destination conveyor bank and indexing conveyor. Each embeds the same three named PNG image records, with two distinct byte hashes. Six of nine embedded image records match local conveyor texture bytes; three roughness image records have no exact match in that local texture directory. `tools/modeling/build_belt_conveyor.py:30-64` contains procedural texture generation using local arithmetic, but this source inspection does not prove that the current binary was produced by that script or establish creator rights. Image names alone are insufficient provenance. Per-image hashes and local matches are retained in the metadata evidence.

Ruling: preserve the existing dirty canonical checkout and runtime artifacts, perform reversible packaging only, and retain evidence rather than creating commits or replacing exports. The transferred user instruction overrides the older plan's security-scan task: the blocked workflow is stopped and must not be invoked. Security assessment remains an unresolved external gate.

## Native package acceptance so far

The baseline package demonstrated a mid-cycle Demo 5 Stop, Run holding the stopped pose, and fresh Start completing recovery to home with one carton. A subsequent carton completed to home. The four-carton layer, rejection of an extra load, and full layer Reset have not yet completed native acceptance. Previously recorded deterministic/model checks remain separate evidence.

The user is using the screen. Native testing is deferred without taking input, capturing the screen, closing their app, or changing display settings. Background package checks that write shared preferences or drafts also wait until they can run without interfering with that session.

## Remaining qualification and distribution gates

Windows PowerShell portability check: the host's inbox `powershell.exe -NoLogo -NoProfile -NonInteractive -File tools/test_verify_demo_package.ps1` exited 1 before fixtures ran, with `UnauthorizedAccess` / "running scripts is disabled on this system." Its execution-policy query reported all scopes Undefined (effective Windows default restriction); the PowerShell 7 host used for previous passing checks has a different Process-scope policy. No policy was changed and no bypass used. **Default Windows PowerShell execution of the script preflight is not qualified.** Resolve recipient policy compatibility through an approved signing/distribution approach or a separately qualified launcher; do not tell recipients to weaken their policy. This is separate from the checker logic passing under the existing PowerShell 7 environment.

`DEMO_DELIVERED_BINARY_INVENTORY_2026-10-10.json` inventories all 191 delivered EXE/DLL files with SHA-256, Windows version metadata and local Authenticode status. Windows reported 187 Valid and four NotSigned (`RungProof.exe`, `RungProof.console.exe`, `GodotSharp.dll`, `RungProof.Next.dll`). This is current host verification, not clean-machine trust, signing approval or complete engine-native-component inventory. The application DLL's version contains the baseline commit despite dirty-source repairs; the manifest and exact package hash remain the authoritative candidate identity.

Package preflight remediation: `rungproof-next/tools/verify_demo_package.ps1` checks critical files, schema/policy, contained non-redirected paths, file sizes and SHA-256 hashes without launching Godot. Five disposable-fixture regressions passed. Future build outputs copy it as `Verify-Package.ps1` and invoke it after manifest generation. The preflight repack above now includes this checker and passed all 201 manifest entries; older immutable ZIPs remain unchanged. Using the checker detects incomplete copies before launch; an automatic primary launcher and direct-EXE graceful recovery remain open. The missing-PCK test did display a native desktop alert despite `--headless`, as confirmed by the user's attached screenshot; subsequent work avoids such executable-failure probes while the screen is in use.

Incomplete-package probes used disposable copies under `.tools/demo-release-review-20261009/package-failures`, leaving the candidate intact. With `RungProof.Next.dll` withheld, the headless executable emitted hostfxr initialization/null plugin-pointer errors and exited with Windows status -1073741819 (native access violation). With the PCK absent, it emitted an explicit missing-project/PCK error but did not terminate during observation; only the verified owned probe engine/launcher processes were stopped. This gate **FAILS** graceful recovery: console evidence exists, but the missing-assembly message is not an actionable recipient explanation and the missing-pack probe did not self-exit. A distribution preflight/launcher or upstream engine correction must address this before describing incomplete-copy handling as qualified. No current application-window input or screen capture was used.

Background follow-up: `DEMO_DEPENDENCY_REVIEW_2026-10-10.md` records the refreshed resolved-package and advisory queries, embedded runtime version, primary-source release/advisory checks and their limits. `DEMO_RELEASE_OPERATIONS_2026-10-10.md` provides recipient qualification, support/recovery, signing and rollback procedures with explicit unassigned owner decisions. Neither document constitutes acceptance or release approval.

| Gate | Current evidence | Required acceptance |
| --- | --- | --- |
| Fixed package | 164 source regressions passed; export, resource exclusions, artifact policy, shell, conveyor and sampled palletizer passed | Native affected paths and external environment qualification |
| Clean Windows / standard account | No ready authorized clean VM identified | Complete copied package starts offline without SDK or developer checkout, on a standard account |
| Demo 5 | Native recovery and two cartons observed | Four-carton layer, extra-load behavior, Stop/Run/fresh Start and Reset on final package |
| Hardware | RTX 5070 Ti host only; roughly 1 GB working set observed | Declared minimum GPU, startup/response/frame pacing and bounded soak |
| Accessibility | Earlier minimum-window inspection; no assistive-technology qualification | Keyboard-only completion, focus visibility, DPI and screen-reader evaluation |
| Rights | Runtime notices available; catalog approval does not establish licenses | Asset-by-asset provenance and distribution permission, applicable notices and ownership review |
| Security | Source review and reproduced availability defect; formal scan incomplete | Preserve actual coverage and unresolved findings; do not describe interrupted scan as clearance |
| Signing/support | Baseline executable unsigned | Approved release identity, signing decision, distribution channel, support contact and rollback artifact |

## Recipient acceptance record

Keyboard source assessment: the ladder canvas explicitly accepts focus and gates its copy/paste/delete/move handlers on visibility and focus. Text editors and unsaved-work dialogs explicitly request focus in selected flows. Main handles save/open/undo/redo and help shortcuts. Toolbar buttons rely on standard Button focus/theme behavior. These observations establish implementation paths only; keyboard-only completion, traversal order, visible focus, text-entry shortcut isolation, DPI readability and assistive-technology behavior still require native acceptance. No accessibility pass is claimed.

Record package SHA-256, build identity, Windows version, account type, CPU/GPU/RAM, renderer and display scaling before testing. Retain console logs and the exact reproduction steps for failures. Do not include PLC credentials or plant data in support material.

Accept only after offline first/repeat launch, all five supported demos, project save/reopen, malformed-import recovery, Stop/Reset behavior and the complete Demo 5 layer succeed on the declared supported environment. Record untested configurations explicitly. Missing runtime files should produce an actionable failure; resolve an incomplete copy by restoring the whole package.

Keep the previous approved artifact and its hash for rollback. Preserve recipient projects and settings before any upgrade or rollback. Name a release owner and support contact before external delivery; neither has been invented by this review.
