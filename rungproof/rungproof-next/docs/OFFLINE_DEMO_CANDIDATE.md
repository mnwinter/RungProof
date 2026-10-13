# Windows offline demo candidate

This is an internal, unsigned release candidate lane. It is not distribution
approval or clean-machine acceptance. The supported preview scope is the five
authored demos, the local ladder editor and the internal Stop the Carton challenge. Other catalog scenes are experimental.
No real PLC connection or commissioning is included.

## Build

Use the pinned Godot 4.7.2 .NET bundle and x64 .NET 10 SDK under `.tools/`. Windows development builds also require an existing PowerShell 7 (`pwsh.exe`) installation for the semantic-manifest MSBuild producer. Its `RungProofPowerShell` property is overridable with an absolute quoted executable path, for example `/p:RungProofPowerShell="C:\Program Files\PowerShell\7\pwsh.exe"`. Confirm that path exists; do not change execution policy to satisfy this prerequisite.
Download the matching official .NET export templates, verify their published
SHA-512, and extract the four Windows x64 release/debug template files under
`.tools/export-templates/windows/`. Preserve `GODOT_LICENSE.txt` and
`GODOT_COPYRIGHT.txt` from the matching official Godot source tag under
`.tools/export-templates/`. Generated binaries remain ignored.

```powershell
# Clean source is the default. -AllowDirty explicitly labels local WIP.
& .\tools\build_offline_demo.ps1 -AllowDirty
```

The script exports to a fresh versioned `artifacts/offline-demo/` directory,
requires the C# runtime assembly, rejects export error diagnostics even if
Godot returns zero, checks the packed resource inventory, and runs checks
against the exported executable. It restores its environment settings afterward.

`RungProofOfflineDemo=true` adds a compile-time transport prohibition. Both
external-client constructors reject construction. Menus and direct UI handlers
also keep the execution source local. An export carrying the `offline_demo`
feature without the compiled policy fails startup. Use the script rather than
exporting this preset with an ordinary development assembly.

The export keeps its own `RungProof Offline Demo` user-data directory, separate
from the development application's drafts and settings. The renderer remains
Forward+; minimum GPU, VM and RDP compatibility are unqualified.

## Try the candidate

Copy the complete output folder to a separate location. Run `RungProof.exe`;
do not separate it from `RungProof.pck` or `data_RungProof.Next_windows_x86_64/`.
No build/restore step should occur at recipient launch. A clean Windows machine
without SDKs/developer tools is still a mandatory acceptance gate.

New builds include `Verify-Package.ps1`. Before launch, run this read-only
integrity check from the copied package directory:

```powershell
& .\Verify-Package.ps1 -PackageDirectory $PWD
```

Expect `PACKAGE_INTEGRITY_PASS`. If it fails, restore the complete verified
archive before running the EXE. This checks recorded sizes/hashes and critical
files without starting Godot. It is not signing, provenance authentication or
release approval. Follow normal local PowerShell policy; do not weaken security
settings. Earlier candidate ZIPs lack this checker and remain immutable.

Directly launching an incomplete package can crash or display a native error
dialog: missing-assembly and missing-PCK probes reproduced these failures.
Graceful direct-EXE recovery remains unqualified.

1. Select **START DEMO 1**, then **Run**.
2. Press the count control three times; the lamp should turn green.
3. Select **Stop**, then **Reset**; outputs/count should return to their initial state.
4. Use **Scenario** to select Demos 2–5, which now appear first.
5. Demo 5 begins with a carton. **Run**, then **Start** processes one carton;
   after home return, **Load** then **Start** processes the next. Reset starts a new layer.
6. **View → Frame Process / Frame Whole Scene** changes camera framing without
   changing plant pose. Inspect the live ladder in split view.

For editing, use the tested 1600×900 viewport. The 1200×675 minimum window is
visually inspected, but dense engineering panes require scrolling and the
instruction palette is hidden at short heights. High DPI, keyboard-only and
assistive-technology acceptance remain open.

If startup fails, run `RungProof.console.exe` from a terminal and retain its
output. Missing C# assembly/data-folder errors indicate an incomplete copy.
An offline-policy failure indicates the wrong assembly/build configuration.
Do not resolve either by installing or enabling PLC transport tools.

## Acceptance and remaining gates

- Exported constructor-policy, shell/menu/project persistence and conveyor
  feedback integration checks must pass on the exact candidate.
- Native startup, demo entry, editor styles, process/whole framing, Stop/Reset
  and offline controls must be observed on the exact candidate.
- Clean-machine offline launch/relaunch, standard-account/read-only installation,
  corrupt-file recovery, update/rollback, minimum hardware, DPI/accessibility,
  representative-user pilot and full five-demo artifact workflows remain required.
- Runtime/engine notice copies are a starting point. Complete asset/font rights,
  product ownership/name, privacy/support and signing/distribution review before
  external release. Candidate assets are not approved by inclusion in this package.

Build metadata, file hashes and exact logs live beside the candidate. No push,
merge, publication, live PLC connection or scheduled automation is part of this lane.

Export options follow the [official Godot export documentation](https://docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html)
and the [4.7.2 C# export implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs).

## Current internal challenge evidence and limitations

The internal Stop the Carton challenge uses the bundled authored packet, unsolved starter, conveyor model and generated semantic manifest. Qualify the challenge/policy checks on the exact exported executable; development verification cannot qualify another candidate by association.

Root reports current native UI evidence after a fresh application build with zero warnings/errors and no ERROR diagnostics; external guards reported zero mode events and no bridge construction. This is bounded native fixture/render evidence, with exact retained logs/captures required in the current candidate dossier. Root also reports wired development Test checks for correct and incorrect programs. Those development results require separate exact-artifact checks before qualifying the exported candidate. Nonfocus capture does not establish actual keyboard or mouse interaction. Keyboard-only navigation, DPI scaling, screenreader use, clean Windows launch without developer tools, minimum GPU, VM/RDP support and complete exact-artifact workflows remain open.

Known limitation: existing `ReplaceFromProgram` conversion of nested parallel branches with shared prefixes can produce duplicate instruction IDs. The native Passed + NotSaved fixture uses equivalent top-level parallel branches with unique IDs. It does not establish general nested-program import eligibility or repair that conversion. Use the authored starter for this bounded challenge; retain invalid-draft diagnostics and do not infer broader import compatibility from the fixture.

Full asset/font/runtime rights and required notices remain unresolved until the packaged inventory has an explicit disposition. Signing, recruitment and public distribution require separate approval. Package integrity is qualified only by the freshly retained verifier results for the exact candidate; development fixtures alone do not qualify it. Final build/export, source freeze and scene/asset inventory remain root-owned.