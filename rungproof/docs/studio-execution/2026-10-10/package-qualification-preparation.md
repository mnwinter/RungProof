# T9 internal package qualification preparation

Status: preparation only; fresh exact candidate verification PENDING. This document does not accept an artifact, authorize release/distribution/purchases, or close native accessibility, rights or target-support gates. Root owns final build/export/native capture and source freeze. No build, export, engine launch, network or PLC activity was performed for this preparation.

## Evidence to retain for the fresh candidate

| Field | Required evidence / acceptance boundary |
|---|---|
| Exact artifact | Unique output path; UTC build time; source commit and dirty status; EXE/PCK/assembly/console hashes; full copied-package file inventory; independently retained manifest hash. Hashes detect changes, not publisher authentication. |
| Semantic identity | Fixed bundled semantic manifest; RP-CANON-1 identity; engine/evaluator revision and hashes; normalized generator hash; actual current input membership and hashes compared with generated entries. Current producer enumerates controller/challenge sources plus conveyor model, and three evaluator sources. Root reports actual 33/3 build gate and new-member rejection; retain exact fresh logs rather than promoting this report into independently run evidence. |
| Packet and starter | Exact catalog, challenge packet, starter and presentation JSON hashes; authored scene JSON path/hash for each supported demo and challenge; source-to-export resource correspondence. Preserve unsolved starter semantics. |
| Source freeze | Before/after source fingerprint plus separately retained scene/resource/build/checker-script inventories. Current sourceCodeConfigurationSha256 includes C# and selected configuration/challenge files, but excludes authored scene JSON, meshes/fonts/textures and build/checker scripts. It cannot alone establish unchanged art/resource/tool inputs. |
| Resources | Export.log Storing File inventory; five fixed challenge resource entries; supported scenes and their actual dependency paths; runtime data folder and DLL; console wrapper; README/checker and notices. Reject developer files and unexpected private/config resources. Broad all_resources plus JSON/Markdown inclusion requires inspection of actual inventory. No formal secret/SBOM/dependency audit is claimed. |
| Offline boundary | Exact artifact policy marker and exit code; bridge constructor compile prohibition; shell/challenge verifier evidence on exported assembly. No live PLC, transport installation or actual network activity authorized. |
| Logs | Record command, source snapshot, artifact path, timestamp, numeric exit code, required marker and absence of ERROR / *_VERIFY FAIL / BUILD_POLICY FAIL diagnostics. A trailing PASS cannot override an earlier error. |
| Native captures | Exact fresh artifact/source association; scene marker and PNG dimensions; current five-demo/challenge captures; readable labels, aperture and controls. Keep rejected wrong-scene, placeholder, encoding/startup-error, starter-v2/v3 and patterned-shadow v4 captures clearly rejected. Development captures do not automatically qualify the exported executable. |
| Rights/notices | Actual packaged runtime, engine, mesh, texture and font inventory: path/hash, upstream/local provenance, license/evidence reference, notice attribution requirement and unresolved disposition. Godot/.NET notice copies are a starting point, not asset approval. Unknown rights remain unresolved; no invented license conclusions. |
| Target/support/privacy | Tested Windows version/account/path/renderer/hardware; clean-machine/no-SDK offline startup/relaunch; spaces/non-ASCII/read-only install and writable user data; recovery/rollback/draft-preservation behavior. VM/RDP/minimum GPU/keyboard/DPI/screenreader and participant support/privacy remain unqualified until exact evidence exists. |

## Existing checker scope and pending gaps

`verify_demo_package.ps1` is deliberately read-only and checks schema/policy, three critical artifacts, listed sizes/SHA-256, duplicate/escaping paths and reparse points. Its existing synthetic suite has five cases (complete fixture, missing assembly, damaged PCK, escaping path and empty manifest). No suite was run here.

Source inspection shows it permits unlisted additional files and manifests omitting console wrapper, README, checker and notices. It does not inspect PCK contents, semantic identity, log errors or runtime compatibility. Those are concrete gaps if it is treated as a complete T9 qualification check; its current hash-check promise is narrower. Root should retain a separate complete inventory comparison and challenge/resource evidence or extend the existing checker with meaningful negative fixtures. Do not create a duplicate checker. No source mutation was made here.

## Root-only commands after final freeze and launch clearance

Run from `rungproof/rungproof-next`. The build command launches Godot/exported verification processes and belongs to root's exclusive lease. Use a new output directory; existing candidates are preserved.

```powershell
& .\tools\test_verify_demo_package.ps1
& .\tools\build_offline_demo.ps1 -AllowDirty
```

Expected suite marker: `PACKAGE_CHECKER_TESTS_PASS 5` with exit 0. Expected build terminal marker: `OFFLINE_DEMO_CANDIDATE_PASS <fresh path>` with exit 0; export.log and artifact logs must additionally be clean. The build currently requires policy `failures=0 bridgeFactoryCalls=0`, and exported `OFFLINE_BUILD_POLICY PASS`, `APP_SHELL_VERIFY PASS`, `DEMO_SEQUENCE_VERIFY PASS`, `CONVEYOR_CHALLENGE_VERIFY PASS` markers.

After copying the complete fresh candidate to the agreed isolated test location, replace the example path with that concrete copied folder:

```powershell
& .\tools\verify_demo_package.ps1 -PackageDirectory 'C:\actual\copied-candidate'
```

Expected `PACKAGE_INTEGRITY_PASS files=<count>` with exit 0. This command does not launch Godot. Separately compare full inventory and candidate-manifest hash with retained originals; retain the exact copied-package verification output. Missing or damaged-file failures require restoring the complete candidate. Wrong compiled policy requires a fresh offline build, never enabling transport. None of these markers closes unresolved native/target/rights/distribution gates.
## Updated owner handoff and bounded known limitations

The development build prerequisite on Windows is PowerShell 7 (`pwsh.exe`). The semantic-manifest MSBuild producer uses that executable by default. An existing executable may be selected with the overridable MSBuild `RungProofPowerShell` property; pass an absolute quoted path, for example `/p:RungProofPowerShell="C:\Program Files\PowerShell\7\pwsh.exe"`. Confirm the selected path already exists. This preparation neither installs PowerShell nor changes execution policy. Root owns the concrete application-build invocation and its evidence.

The existing `ReplaceFromProgram` conversion can expand nested parallel branches with shared prefixes into duplicate instruction IDs. Root's native UI Passed + NotSaved fixture uses equivalent top-level parallel branches with unique IDs. That bounded fixture does not prove general nested-program import eligibility or fix this conversion limitation. No product rewrite was authorized in this lane.

Root reports current native UI v7 was produced after a fresh application build with zero warnings/errors and no ERROR diagnostics, with external guards reporting zero mode events and no bridge construction. Retain the exact v7 log/image paths and source association in the final dossier. These are root-reported results; this documentation lane did not rerun them. Accepted nonfocus capture establishes only the observed rendering/fixture behavior under its tested capture conditions. No shared keyboard/mouse input was performed here; native keyboard navigation, DPI scaling and screenreader acceptance remain open. Do not generalize nonfocus capture success into unrestricted safe-launch or input permission.

The checker/inventory gaps described above are observations of the earlier inspected source snapshot. A fresh exclusive owner is now repairing them. Their final disposition requires the owner's current changes, meaningful negative fixtures and fresh independent review; this document does not assert those gaps remain in the final candidate or that fixes have passed.

Root retains exclusive ownership of final application build, export, source freeze and scene/asset/runtime inventory qualification. No documentation addendum substitutes for exact fresh artifact verification, target support or rights disposition.
### Final documentation boundary update

Root reports current wired development Test checks for correct and incorrect programs. Exact exported-artifact checks remain pending separately; development outcomes do not qualify the candidate by association. Package integrity is qualified only by the freshly retained verifier results for the exact candidate; development fixtures alone do not qualify it. Earlier checker observations and pending-repair statements above describe their historical inspection snapshot and are superseded for final disposition by current owner evidence and independent review. This documentation lane does not claim independent checker PASS. Actual input, DPI, screenreader, clean-machine and rights gates remain open. Documentation ownership is returned to root before final freeze.