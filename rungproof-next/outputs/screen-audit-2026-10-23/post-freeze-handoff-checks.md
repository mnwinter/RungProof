# Additional hard-issue and restoration checks

Prepared during the pre-cutoff analysis window. This is source/backup analysis, not a new application repair or native acceptance run. The application change boundary remains `41188c70bb86e3305f7cd7c8759f000f9c396729`; the compiled77-scene findings and `repair-plan.md` remain authoritative.

## Two historical automated failures

The B5 controller log records424 passed and2 failed. `tests/WatchRowTests.cs:80` expects schema9 before its round-trip assertions, while `src/VirtualController/LadderEditorProjectJson.cs:26` and `tests/EditorMigrationTests.cs:160,170` require10. Source strongly supports a stale expectation; it does not prove the assertions after that failure pass. Correct/version the assertion and test name deliberately, then execute the remaining round-trip checks.

The runner supports an actual test-name substring filter (`tests/Program.cs:29,730`) and rejects zero matches. From the confirmed repository root, the existing name is:

```powershell
& 'rungproof-next/.tools/dotnet/dotnet.exe' run --project rungproof-next/tests/RungProof.Next.VirtualController.Tests.csproj --no-restore -- --filter 'watch rows persist schema 9 authoring fields and table identity'
```

The other failure is `TIA local run evidence binds exact local gate coverage` (`Program.cs:237`). Its summary expects `763ae357c136ec7c2bd5195d953acb4fb224c3cd69a1d45a9acef4cf8761550e` for `.tools/tia-project-tree-gates.json`; actual C and D bytes both hash `bebb754872e3721eac920f69581c2e243a8032553da3e4640ce74156d5407b6a`. The independently extracted historical archive proof records that same current hash. None of its110504 proven stored paths has the expected hash; that is not proof those bytes never existed elsewhere.

The artifact reports14 gates/103 assertions and is offline-only. This is a real evidence-provenance inconsistency, not demonstrated editor breakage or installed-TIA/live-PLC proof. Compare intended historical provenance before deliberately superseding the record. `Program.cs:5496` skips raw artifact validation when it is absent: deleting it could misleadingly remove the failure. Preserve it.

```powershell
& 'rungproof-next/.tools/dotnet/dotnet.exe' run --project rungproof-next/tests/RungProof.Next.VirtualController.Tests.csproj --no-restore -- --filter 'TIA local run evidence binds exact local gate coverage'
```

Neither command was executed in this source-frozen window. Keep these validation limitations separate from the77-scene native defect list.

## Bounded neighboring-route check

A read-only pass of77 definitions,230 actions and752 points found three actions directly targeting SIM-owned points: the conveyor emergency control and gallery animation have existing physical routes; radar inspection is the remaining missing visible route. This supplies no additional hidden-presentation-toggle counterexample. Other SIM points can be state rather than controls, and loaded-LAD lifecycle aliases are a separate category. No new interaction PASS is inferred from this inventory.

## Pinned camera documentation

The locally installed GodotSharp4.7.2 primary XML documentation supports KeepAspect.Height preserving vertical framing, FOV in degrees, effective camera transform including H/V offsets, and actual world-to-viewport projection. The offset descriptions alone do not specify a complete units/sign formula. Consequently the horizontal-width divisor concern in the camera analysis remains source algebra, not a newly proven native defect or established Demo3 cause. Keep the actual effective-transform/projection/occlusion discriminator in the repair plan.

## Package/bootstrap boundary

The project uses .NET10 and Godot.NET.Sdk4.7.2. Godot.NET.Sdk, GodotSharp, GodotSharpEditor and Godot.SourceGenerators4.7.2 are bundled inside the canonical Godot toolchain's `GodotSharp/Tools/nupkgs`. The external NuGet cache contains replaceable packages, rather than unique additional project work. Old assets metadata embeds absolute user paths and must be regenerated.

An isolated empty-cache minimal Node compile using only that local package source passed12 explicit gates: exact four packages/source/cache, compile0, HTTP cache0, inherited HOME unchanged, and unchanged canonical source/tool metadata. The corrected rehearsal receipt SHA256 is `7278ce068e06faad545ba999a189556f5255b692c5e0c2570d91e3d874ad392d`. Its complete63-file private D copy was paired-hash verified. This establishes minimal SDK/package bootstrap on this workstation; it does not establish a clean-machine main-app build, asset import or launch. The normal launcher still builds/restores/imports, so follow development setup and establish fresh runtime evidence after migration.

A later isolated **actual main C# build** also passed:427 exact copied inputs, including423 real source files, empty cache with precisely the same four local packages, the actual D PowerShell runtime executing both precompile targets, and0 warnings/errors. All15 gates passed; canonical input hashes/tool metadata and the D runtime metadata remained unchanged. The receipt SHA256 is `fd8933ad5398bccbef53a427efa5e6a532b035187f07796460cea69b9435b62d`. This strengthens source/package recovery evidence, but did not run the engine, asset import, tests or GUI and was not performed after an OS wipe.

## Context and Git restoration boundaries

Metadata-first-line discovery checked1209 active/archived files and found exactly this root and four direct children, each unique, with zero directory/header errors or additional descendants. Unrelated transcript bodies were not read. Final context must still use current complete-record prefixes and preserve the stated time boundary.

The actual all-refs bundle/no-checkout-clone rehearsal restored `db9eb2b293ef97553fa108509ca5d1a38b963fe0` and passed fsck. Its29-file D copy also passed paired hashes and fsck. The exFAT ownership guard initially refused that new checkout; an exact per-command `safe.directory` exception allowed verification without changing global trust settings. This proves Git history/object restoration, not application or userdata restoration. Keep later immutable payloads, native userdata and the latest matching Git/restore receipt together.

## Additional source-build prerequisite: PowerShell

`RungProof.Next.csproj` invokes `pwsh.exe` for UTF-8 source validation and semantic-manifest generation before CoreCompile. The existing `RungProofPowerShell` property accepts an explicit executable path. On this workstation `Get-Command pwsh.exe -All` resolved only Codex's cached PowerShell7.6.5; no executable was found inside the canonical project toolchains. The normal CMD launcher therefore needs PowerShell7 available on PATH after a wipe. This is a replaceable software dependency, but it must not remain an undocumented assumption.

The entire658-file/256592846-byte runtime was copied with direct native paired SHA256 and exact path-set checks to `incremental-2026-10-23/support-runtime-powershell-7.6.5-20261023T091500Z/powershell` on D. The copied executable started successfully as Core7.6.5 with PSHOME in that D folder. Its executable SHA256 is `362a356ce7f0940ec74f73a8fc2c990a2cc24a38a11c90bbd8eca947110ad139`. Preserve the whole directory and license; restoring only pwsh.exe is insufficient. This is package-startup proof on this workstation, not a clean-machine RungProof launch. Update development prerequisites and use the latest D restore index for the actual verified copy.
