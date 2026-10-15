# Virtual controller research and provenance

Reviewed 2026-09-30. Phase 1 incorporates no source code, binaries, packages,
or generated artifacts from the candidates below. The implementation is local
C# code using the existing .NET/Godot toolchain, so this research adds no new
third-party redistribution obligation.

| Project / standard | Upstream evidence | License / constraint | Use in Phase 1 |
|---|---|---|---|
| PLCopen XML | https://www.plcopen.org/standards/xml-echange/ | PLCopen specification; current exchange format is published as IEC 61131-10 | Architectural exchange candidate only |
| Beremiz | https://github.com/beremiz/beremiz | IDE/CLI GPL-2.0+, Python runtime LGPL-2.0+; repository also describes a separate GPL-3.0 C++ runtime | Evaluated only |
| MatIEC | https://github.com/beremiz/matiec and `COPYING` in that repository | GPL-3.0; IEC source to C/C++ compiler | Evaluated only; license boundary requires review before integration |
| OpenPLC Runtime v4 | https://github.com/Autonomy-Logic/openplc-runtime | MIT; Linux-oriented C/C++ runtime plus REST/debug/compile services | Preferred external-runtime spike candidate, not incorporated |
| IronPLC | https://github.com/ironplc/ironplc | MIT; upstream README calls it a prototype | Watch/spike candidate, not incorporated |
| RuSTy | https://github.com/PLC-lang/rusty | LGPL/GPL repository licensing; ST/LLVM focus | Evaluated only |
| Eclipse 4diac FORTE | https://eclipse.dev/4diac/4diac_forte/ | Eclipse ecosystem; IEC 61499 runtime | Not a primary fit for IEC 61131-3 LD |

Before incorporating any candidate, record the exact version/commit, bundled
files, license text, distribution mode, process boundary, supported-language
evidence, determinism evidence, and security review. A tool being open source
does not establish vendor-equivalent PLC behavior or suitability for physical
control.
