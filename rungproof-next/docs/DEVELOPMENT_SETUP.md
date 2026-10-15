# Development setup

Pin the toolchain before authoring production assets:

1. Use Godot 4.7.2 .NET for Windows.
2. Use the x64 .NET 10 SDK. Godot 4.7.2 requires its .NET 10 host runtime.
3. Use Blender 5.2.2 LTS for the pinned asset-authoring pipeline.
4. Keep downloaded tools outside source control; `.tools/` is ignored. The
   verified workstation toolchain is portable under `.tools/`.
5. Extract the full Godot .NET bundle, including its console executable and
   `GodotSharp` folder, into the Godot directory below. Install/extract the
   x64 .NET SDK into `.tools/dotnet`; a runtime-only installation cannot build.
6. Double-click `RUN-RUNGPROOF-NEXT.cmd`. It restores NuGet packages, builds
   the C# assembly, imports runtime assets, then opens the application. The
   first launch requires package access and can take several minutes to import
   the full catalog. Later launches reuse unchanged imports. Blender is not
   required to run the delivered GLBs.

The launcher reports the exact missing executable or stops on build/import
failure. Run it from an existing terminal to retain that output:

```powershell
& .\RUN-RUNGPROOF-NEXT.cmd
```

Use the matching pinned .NET Godot bundle from the
[official release archive](https://godotengine.org/download/archive/) and the
[Microsoft .NET SDK downloads](https://dotnet.microsoft.com/download/dotnet/10.0).
Do not mix a non-.NET Godot bundle with this C# project.

This is a source-run entry point. A separate unsigned Windows offline candidate
preset and checked build lane are documented in
[OFFLINE_DEMO_CANDIDATE.md](OFFLINE_DEMO_CANDIDATE.md); no installer or approved
external release is supplied. The guarded external-PLC path additionally
requires the parent `rungproof` Python tools and configured profiles; copying
only `rungproof-next` provides offline operation.

Do not migrate primitive geometry into the production catalog. The first
accepted asset must pass `ASSET_QUALITY_STANDARD.md` from Blender source through
Godot render and blind review.

Verified local entry points:

- `.tools/dotnet/dotnet.exe`
- `.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe`
- `.tools/blender/blender-5.2.2-windows-x64/blender.exe`

The Godot project must retain `[dotnet] project/assembly_name="RungProof.Next"`;
without it Godot searches for an assembly derived from the display name and
cannot bind the C# scene scripts.

Run `python .\tools\validate_asset_evidence.py` before any catalog promotion.
It is intentionally advisory for unfinished candidates and mandatory for every
entry admitted to `production.catalog.json`.
