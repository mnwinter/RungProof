# Offline candidate dependency assessment

Candidate: `RungProof-OfflineDemo-20261010-callgraph-fixed.zip`. Review date: 2026-10-10. This is a bounded dependency assessment, not a complete software bill of materials or security clearance.

## Verified inventory

| Component | Evidence | Result |
| --- | --- | --- |
| Godot SDK / engine | Project SDK and export identity | 4.7.2, engine hash ed1daf0bf |
| Godot.SourceGenerators | Fresh resolved NuGet inventory | 4.7.2, auto referenced |
| GodotSharp | Fresh resolved NuGet inventory | 4.7.2, auto referenced |
| GodotSharpEditor | Fresh resolved NuGet inventory | 4.7.2, auto referenced |
| Microsoft.NETCore.App | Exported runtimeconfig.json | Included framework 10.0.12 |
| Unsafe BinaryFormatter serialization | Exported runtimeconfig.json | Disabled |

Fresh `dotnet package list --project RungProof.Next.csproj --include-transitive --vulnerable --format json` exited 0, consulted NuGet's configured public source and returned no vulnerable package entries. The separate full inventory resolved the three Godot packages above. Logs are under `rungproof-next/.tools/demo-release-review-20261009/dependency-advisories-current.json` and `dependency-inventory-current.json`.

Microsoft's [.NET 10 release index](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md), checked on the review date, lists 10.0.12 as its latest release. The [10.0.12 notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) identify the September 8, 2026 release, SDK 10.0.401, and security and non-security fixes. This supports the installed runtime's currency against that index; it does not independently validate every CVE's applicability.

The [Godot maintainer advisory page](https://github.com/godotengine/godot/security/advisories), checked on the review date, displayed no published advisories. An empty advisory page does not prove that Godot or its bundled native libraries have no vulnerabilities.

## Open evidence

Notice-copy verification on the preflight repack: all four delivered engine/runtime notice files matched the local build prerequisites byte-for-byte (SHA-256). `GODOT_LICENSE.txt`: `B0435E3B3E4E55238F05F4B306F30524A1B2E20147810D436EAA554FA6855C80`; `GODOT_COPYRIGHT.txt`: `CB1980C88089573BCACD7221D777C689BB8BBD778799F24C27FCA0FE5F774D6D`; `DOTNET_LICENSE.txt`: `7F6839A61CE892B79C6549E2DC5A81FDBD240A0B260F8881216B45B7FDA8B45D`; `DOTNET_ThirdPartyNotices.txt`: `DEB4427A295E1ED474B0D81C5A0D972C1B550B9A715CDA939CDFA9236B1B418F`. This establishes faithful copying, not complete rights clearance. The export-resource log lists zero TTF/OTF/WOFF font paths; engine-embedded fonts are outside that log's scope and remain covered only by the existing notice inventory pending full component reconciliation.

Delivered PE inventory follow-up: `DEMO_DELIVERED_BINARY_INVENTORY_2026-10-10.json` covers all 191 EXE/DLL files in the preflight repack (package SHA-256 `970E4653EB879D6DCDC41E679E94C25101807067D207D0D123AE322D2F3785DB`). It records byte hashes, version resources and host Authenticode status. Windows reported 187 Valid and four NotSigned; the two Godot executables, GodotSharp and application assembly are unsigned. Included `msquic.dll` identifies product version `2.5.10.154561281-official`; `Microsoft.DiaSymReader.Native.amd64.dll` identifies `14.50.35738.0`. Version resources identify components but do not establish full build provenance or advisory applicability. No new dependency query or engine launch was performed for this inventory.

The NuGet query does not enumerate Godot's bundled native libraries, embedded codecs/importers, platform DLL dependencies or assets. Derive a complete native inventory from the matching export template/source build and map versions to relevant advisories before describing the whole package as cleared. Preserve engine and runtime notices already included in the candidate. Asset ownership, license and redistribution evidence remain separate gates.

The formal source assessment is an unresolved external gate. The user explicitly stopped the blocked security workflow; do not retry it, invoke its tools or work around the app-level restriction. No current scan status is asserted here, and no completed report establishes clearance. Record ordinary dependency inventory and packaged-artifact evidence separately.

Refresh advisories at release freeze and after any runtime, engine or package update. Any changed binary creates a new artifact identity and requires affected acceptance checks; never silently replace a previously recorded ZIP.
