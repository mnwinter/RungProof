# Native userdata staging helper review

2026-10-23 08:08 UTC. Scope: inspectable backup tool and three-file synthetic rehearsal only. **The actual Godot userdata folder was not read, copied or changed by this rehearsal.**

`tools/migration-backup-2026-10-22/incremental-closeout/prepare-native-userdata-snapshot.ps1` is ready for root review. Frozen SHA256: `4999a56fd8e7a5511dce5695adfb0776b01f0c1639353fc38d8fbbb8724d462b`.

The helper must run directly through native PowerShell 7 after Godot and bridge writers stop. A PowerShell process launched by packaged Python inherits its different filesystem view. The native preflight observed 144 userdata files; Python observed 97. The helper does not identify namespaces automatically: its explicit expected count is a fail-closed precondition, not an instruction to accept 97.

The source is pinned to the actual approved Godot userdata path. The destination must be one new `native-userdata-*` child of the exact approved RootOwned directory. The helper checks reparse points through the roots' ancestors, uses `CreateNew` for copies and receipt, denies concurrent writers while each source is copied, compares every paired copy SHA256 and size, then independently enumerates and hashes both entire trees again. A PASS requires nonempty equal source-before/source-after/copy/destination relative pathsets, sizes and SHA256, with no errors or exclusions. Detailed filenames and errors go only into a private receipt; standard output contains aggregate counts and that receipt's path. No overwrite, deletion, D write, Git or application action is present.

The synthetic rehearsal used only three tiny files in a new approved private child, including an empty file and nested UTF-8 content. An inspectable private script copy substituted only the source-root literal. The production guards and actual copy/hash/full-set code ran unchanged. All **21 checks passed**: actual copies, complete sets, paired hashes, reordered sets, missing/duplicate/size/hash negatives, outside-root and traversal rejection, existing-destination rejection, immutable copy retention after a later source mutation, and unchanged production helper. AST parsing also passed under native PowerShell 7.6.5.

Private evidence: `RootOwned/native-staging-helper-rehearsal-20261023T081000Z/PRIVATE-REHEARSAL-RESULT.json`; the actual synthetic native receipt is in `RootOwned/native-userdata-SYNTHETIC-20261023T081000Z/PRIVATE-NATIVE-USERDATA-RECEIPT.json`. These are synthetic private evidence, not production userdata coverage.

After root reviews the source and stops writers, the intended direct-native command is:

```powershell
& 'C:\Users\matt.winter\Documents\RungProof\tools\migration-backup-2026-10-22\incremental-closeout\prepare-native-userdata-snapshot.ps1' `
  -DestinationName 'native-userdata-final-20261023T110000Z' `
  -ExpectedFileCount 144
```

If the freshly grounded native count changes, reconcile it first and pass that explicitly reviewed count. Do not lower the guard to match the redirected view. After native PASS, packaged Python must independently enumerate **the staged payload**, match every relative path/size/SHA256 against the native receipt, and reject missing or extra files. The fresh final inventory must include the entire exact RootOwned root; `snapshot --copy-all` must preserve this staging payload in its immutable private tree. Finally verify the D payload against these native hashes. The synthetic rehearsal does not establish those production checks, a Git bundle, public release or native app acceptance.
