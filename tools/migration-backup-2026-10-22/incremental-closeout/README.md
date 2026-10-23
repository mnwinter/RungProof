# Incremental closeout: root-run only

Prepared offline; no backup, archive creation, upload or Git mutation was executed during preparation. Python standard library; use an already installed Python and `C:\Program Files\7-Zip\7z.exe`. This helper never stages, commits, pushes, tags or uploads. Root owns final publication and remote verification.

The historical baseline is `d0c4566bc956706ab3430e25d4142b03f4431e9c` and `D:\RungProof-Backup-2026-10-22`. The existing D mirror contains later interim changes: path membership is therefore insufficient. The helper reads baseline SHA256 records, hashes the actual current D bytes, compares current files to exact HEAD Git blobs, and optionally proves public historical coverage from actual extracted archive bytes. Public path lists alone never establish coverage.

## Root sequence

Run from canonical `C:\Users\matt.winter\Documents\RungProof`. All C work directories below must be NEW and outside the repository and D backup. They contain private manifests and must never be committed or uploaded. Check available C/D disk space first. Archive proof extraction can require substantial space/time; run it before final cutoff and retain its verified proof.

```powershell
$closeout = 'C:\Users\matt.winter\Documents\RungProof\tools\migration-backup-2026-10-22\incremental-closeout\closeout.py'
$seven = 'C:\Program Files\7-Zip\7z.exe'
python $closeout self-test
# Optional but strongly recommended: exact hashes from all84 historical archive parts (91 release assets total).
python $closeout prove-public --sevenzip $seven --work 'C:\RungProof-Closeout-Proof-20261023'
```

After stopping project testing and settling the final Git commit, freeze application/file writers where practical. Inventory includes **all regular repository files, including ignored new screenshots and build outputs**. Junctions/reparse points and `.git` are reported rather than traversed; Git history is separately bundled below. Add each explicitly approved external project folder with another `--extra LABEL=ABSOLUTE_DIRECTORY`; extras are private-only. Do not add the entire user profile or unrelated Codex context.

```powershell
python $closeout inventory --work 'C:\RungProof-Closeout-Plan-20261023' --public-proof 'C:\RungProof-Closeout-Proof-20261023\HISTORICAL-PUBLIC-HASH-PROOF.json' --extra 'installed-rungproof-skill=C:\Users\matt.winter\.codex\skills\rungproof-playtest-handoff' --extra 'godot-userdata=C:\Users\matt.winter\AppData\Roaming\Godot\app_userdata\RungProof'
```

Review `DRY-RUN-SUMMARY.json` and private `PRIVATE-INVENTORY.json`: zero errors; exclusions justified; all expected new builds/evidence present; selected root/descendant sessions included; private/sensitive HEAD paths explicitly reviewed. Without archive proof, all ignored files lacking exact Git-byte coverage conservatively become public delta, potentially very large. CRLF differences conservatively become delta rather than incorrectly claiming Git-byte coverage.

**Native userdata caution:** historical packaged Python and native PowerShell saw different APPDATA contents. The Python `--extra godot-userdata=...` example does not independently prove coverage of the actual native Godot userdata. Separately enumerate and hash the real native folder using the method in [verify-native-userdata.ps1](../verify-native-userdata.ps1), compare exact relative paths/counts/bytes/hashes against the NEW final destination, and preserve that native proof in a new closeout receipt. Do not run the historical verifier unchanged: it writes historical root receipts; adapt only its destination/output paths to the NEW closeout location. Keep both Python-view and native userdata history. Root's final flat D mirror plus Git fsck is additional restoration convenience/integrity evidence; it does not replace either hash receipt or justify overwriting historical base/interim receipts.

```powershell
python $closeout snapshot --manifest 'C:\RungProof-Closeout-Plan-20261023\PRIVATE-INVENTORY.json' --receipt-name 'final-20261023T110000Z' --bundle-git
```

This creates only a NEW `incremental-2026-10-23\final-20261023T110000Z` folder. It neither overwrites historical receipts nor deletes historical files. Changed/new private bytes are copied with paired source/D SHA256; unchanged actual D bytes are referenced by verified hash. A private `git bundle --all` preserves local refs/history. Missing, changed or unreadable regular files produce `INCOMPLETE`; correct the cause and create a new inventory/receipt name rather than silently skipping. After a failed attempt never reuse its partially created output directory. The receipt is a linked snapshot requiring both original D base and new delta, not a standalone mirror.

```powershell
python $closeout archive --receipt 'D:\RungProof-Backup-2026-10-22\incremental-2026-10-23\final-20261023T110000Z\PRIVATE-CLOSEOUT-RECEIPT.json' --work 'C:\RungProof-Closeout-Public-20261023' --sevenzip $seven
```

Only after private PASS: PUBLIC staging reads verified D bytes, excludes private roots/filenames and credential matches, redacts discovered local capability tokens in UTF8/UTF16 copies, creates/test-checks 700 MB 7z parts and writes part SHA256. Private D originals remain unchanged. Compressed archives/credential containers are private unless their exact SHA256 is explicitly approved using `--allow-container-hash` at inventory time after separate content review. This is conservative pattern scanning, **not universal secret detection**; root must review public manifest/exclusions and credential findings before publishing. Large individual public files currently need memory proportional to their size for redaction. No remote publication or remote integrity is claimed by these receipts.

## Codex/private context and mutable files

Only the first metadata record of each candidate session is inspected for thread IDs and parent relationships. Descendant closure follows `parent_thread_id`, including nested `source.subagent.thread_spawn`, independently of inherited cwd. Only the explicitly selected root and descendants have full content preserved to private D. Unrelated transcript bodies are not read or included. The default root is `01a161f0-5690-7783-a4ed-93dd4dee3195`; additional relevant roots require explicit `--root-thread` arguments. Both active and archived session directories are searched.

Live selected transcripts are captured through their last complete JSONL record at inventory time; later appends are outside that receipt's declared boundary. Other mutable cache files must remain stable across inventory/copy or the operation fails. These receipts certify per-file hash boundaries, not a simultaneous application-consistent database transaction. Stop an active writer before retrying; do not discard unstable paths. Hashing private data does not make it safe for Git/public release.

The current session also has an explicitly owned private launch/capability/log folder at `C:\Users\matt.winter\AppData\Local\Temp\RungProof-QA-20261023-RootOwned`. After its app and bridge writers stop, include that exact folder as a private `--extra` root. Do not enumerate or copy unrelated Temp content. An empty task visualization folder requires no payload copy; verify its contents again at final closeout. Historical process IDs and capability files in the private backup are provenance only, never live handles to reuse on the restored machine.

Root final checks: exact final local/remote commit, bundle verification, D receipt PASS, archive test PASS/part hashes, published asset names+bytes+hash verification, and next-agent handoff links. Update a NEW closeout pointer only after verification; never replace the historical baseline receipt. No network/authentication command is embedded here.
