# Two-room material revision V7 — plan and evidence

Human feedback: spend more time on color and texture; the scenes still look flat and bland. This revision is limited to lab-2-01-workstation-call and lab-4-08-package-grouping. No geometry, functional head colors, controllers, assets or other75 material profiles were changed. Human stylistic approval remains open.

## Material plan and implementation

New ReferenceRoleSurface is an opt-in cache separate from all global StudioSceneMaterials caches. Original deterministic256-square albedo, roughness and normal maps combine smooth coarse/midrange variation with fine grain. Low-amplitude albedo avoids painted clouds; roughness and normal relief supply the principal surface distinction. There are no grids, stripes or drawn seam overlays.

| Role | Base roughness | Normal scale | Metallic | Triplanar world tile |
|---|---:|---:|---:|---:|
| Plaster | .82 | .22 | 0 | 2.86m |
| Resin | .48 | .08 | 0 | 4.55m |
| Coated steel | .46 | .15 | .16 | 4m |
| Paint | .66 | .08 | 0 | 3.33m |
| Concrete | .83 | .30 | 0 | 5.56m |

Classroom: cream rear plaster D2C6AE, muted green side paint8BA59B, restrained trim566761, warm greige resin A39D8B. Existing timber remains tactile and unchanged. Dispatch: cool cladding B3BEC2, sage side708A7D, structure566A6C, warm mineral concrete AAA79A, darker recess7A8580 and existing staging patch857968. These are intended input hex colors, not a claim of screen-pixel fidelity.

Lighting is gated by the two exact incoming IDs: classroom key.42/fill.30/ambient.30; dispatch key.50/fill.25/ambient.29. Key ShadowBlur1.6 is supported by pinned GodotSharp4.7.2. Previous environment, key/fill colors/energies and key/fill ShadowBlur are captured and restored. Existing dispatch local fixture shadows disabled, retaining positions/energy. Compatibility does not provide Forward+ PCSS angular softness, so large directional shadow edges remain a visual limitation.

## Frozen source and binary

DLL SHA256: 99F2805075A58453A5C45F15DF285D621229779F81E7DDFB6DCE2BEB672EB173

| Source | SHA256 |
|---|---|
| src/App/DemoEnvironmentPresentation.ReferenceMaterials.cs | F0527AC4497154F09DCC1BFC7BE6983BB8FFC0958DA7002E96509183612D46EA |
| src/App/DemoEnvironmentPresentation.ArtDirections.cs | 1E74BF2FDEB8AB12D5F9C69FE6094D8AB3E63CEB24BDE99F98D3B26F5457429B |
| src/App/DemoEnvironmentPresentation.Catalog.cs | 1C826D412C935CB8DA649038D7821CBC362877A048801ADA37FF7158BFD8F81B |
| src/App/Main.DispatchLook.cs | E74A600B4079046CA81DDB1B29EF3A0913C4650A57A873B94C075BE3681AB77E |

## Verification

Logs under rungproof-next/.tools/studio-verification:

- two-room-material-v7-build.log: pinned SDK build PASS, zero warnings/errors.
- two-room-material-v7-environments.log: exit0; exact77 environment hierarchy/axes/restore/runtime checks PASS.
- two-room-material-v7-controls.log: exit0;77 scene scripted-native mounted-control checks PASS. This is not keyboard, commissioning or continuous-clearance qualification.

Four1600x900 stopped captures under .tools/studio-verification/two-room-material-revision-v7 all have terminal NONFOCUS_SCENE_CAPTURE_PASS and matching .identity.json sidecars. Each sidecar records the exact DLL above and assemblyUnchangedDuringCapture=true.

| Actual image personally inspected | PNG SHA256 |
|---|---|
| 01-classroom.scene.png | 9E68AFE536CC33A5BC4EE5669C078284584D5CB2089EE8DF9B1D379AFB4024F1 |
| 01-classroom.split.png | 7C8A0DE363D0A34ACE2758C5CB90D0B7C7F7B97C73D3FF259F8F201E1A36A2F5 |
| 02-dispatch.scene.png | DA59B87172199D2300B1B965C55F0801F7E664D8BBC72F88E80495978AB8CF57 |
| 02-dispatch.split.png | 2970D47904854AD7AC610EA4802683732BB18292D14F9FD9F7345293DF9CDBDB |

## Actual art review and remaining limits

All four actual frames were personally viewed. Classroom cream/timber/green separation is stronger; floor and plaster grain are visible. Dispatch sage/cladding/mineral-floor separation is clearer, with visible quiet surface variation. Equipment, cabinets and control heads remain unobstructed; no new overlapping text, grid or moire was observed. Split views retain material contrast at the narrower viewport.

This is a bounded improvement, not closure of human flatness feedback. Dispatch still has substantial empty floor and sparse staging; its sidewall remains dark and coated steel reads fairly matte at the preserved camera. Classroom directional bench shadows remain hard and the unchanged underbench frame stays dark. No new furniture or enclosure changes were authorized in this pass.

### Color-space follow-up — read-only investigation, not resolved

ReferenceRoleSurface currently supplies new Color(hex).SrgbToLinear to StandardMaterial3D.AlbedoColor, matching the old helper convention. Parent observed intended green8BA59B appearing much darker. Official upstream4.6 material set_albedo forwards Color unchanged; generated shader declares albedo:source_color. However upstream GLES material storage TYPE_VEC4 uploads values unchanged, and Compatibility uses an sRGB output pipeline. Consequently an explicit linear conversion may itself darken Compatibility material output; automatic double-conversion cannot be asserted merely from the source_color declaration. Pinned4.7.2 executable behavior has not yet been isolated.

Proposed next bounded proof: an isolated pinned-engine unshaded A/B swatch comparing raw hex against explicit SrgbToLinear, without production source changes. Only if it establishes the color-input contract should the exact-two helper be corrected and all four recaptured under a fresh DLL. V7 is frozen pending that decision; palettes are intended source values, not certified rendered colors.

Sources: https://raw.githubusercontent.com/godotengine/godot/4.6/scene/resources/material.cpp ; https://raw.githubusercontent.com/godotengine/godot/4.6/drivers/gles3/storage/material_storage.cpp ; https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/shading_language.html#using-source-color

## V8 correction and evidence (supersedes V7 color interpretation)

An isolated pinned Godot4.7.2 Compatibility unshaded material experiment confirmed raw8BA59B rendered8BA59B; explicit SrgbToLinear rendered416054. Actual swatches were viewed. Probe source/project/log/image retained in rungproof-next/.tools/studio-verification/color-input-probe and color-input-probe.log. No production source was used for the probe. Only ReferenceRoleSurface AlbedoColor was corrected to raw hex; all global material helpers remain unchanged.

Current V8 DLL SHA256: BD7D0BB8345C6647A69ABA8FBEE8C38D1FC6C2E71313062170DB65C23B9705BA

Fresh two-room-material-v8-build.log PASS0warnings/0errors; two-room-material-v8-environments.log exact77 PASS/restore=pass; two-room-material-v8-controls.log scripted-native77 PASS. Four matched captures in two-room-material-revision-v8 are terminal qualified and identity-checked; verified-identity-ledger.json records their hashes. All four actual images personally viewed.

Actual finding: green sidewalls now read lighter and closer to intended palette, proving V7's darkness was partly input conversion. However corrected colors expose excessive brightness on dispatch's broad floor and rear cladding; fine grain weakens especially in split. Classroom cream/plaster and floor also brighten, while directional shadows remain hard. Controls remain clear and original functional colors untouched. This is a necessary technical correction but not final richness/style acceptance. Recommend a bounded exact-two exposure/palette retune under the correct color contract, preserving historical V7/V8 evidence.

## Final V9 bounded exposure correction

V8's color-input defect is fixed, and V9 retunes illumination under the correct raw-sRGB contract. Palettes/maps/geometry remain unchanged from V8. Classroom key/fill/ambient=.26/.20/.20; local diffuser=.14. Dispatch key/fill/ambient=.28/.18/.18; hall fixtures=.16 each and recess=.16. Previous scene environment/energies/colors/ShadowBlur are still restored on leave. Only exact two reference branches use these changes.

Final DLL SHA256: FA9A837D4E8F38F01D4398C68E54ACEFB47175CB00ABE6AB98E5BC6B74ED3772

V9 build PASS zero warnings/errors; native environment77 PASS with restore=pass. Final control result is recorded in two-room-material-v9-controls.log. Source and four image SHA identities are captured in .tools/studio-verification/two-room-material-revision-v9/verified-identity-ledger.json. All image sidecars match the final DLL and assemblyUnchangedDuringCapture=true.

| Final actual image personally inspected | SHA256 |
|---|---|
| 01-classroom.scene.png | 228D25AC39F81EE0EEE247CE889C841566C17F2321067DFDE5AEEB96B1DF2CF2 |
| 01-classroom.split.png | 3E185B08AD831A76C4F40FA0C37297ADEE6F4376B741F2BA16D5BCE17C92DD42 |
| 02-dispatch.scene.png | 058DCDDECB8C1A4E82B26C021E37256B0557A0A4280784EA614793F15098B38F |
| 02-dispatch.split.png | 61B708EE0D1C6ACA9F4CF8789B3F4327D821A2968131E406DCC504E102B560F1 |

Actual V9 review: plaster, concrete and sage remain visible instead of washing nearwhite; timber/cream/green classroom distinction and blue machine/cool cladding/warm mineral dispatch distinction are clearer in full and split. No control obstruction or new text overlap was observed. Floor texture remains a little sandy and broad, but no grid/stripe/moire. Existing hard directional shadows, dark original bench legs and sparse dispatch surroundings remain genuine limitations. Surface exposure is corrected; final artistic satisfaction belongs to human review, not these native tests. No other75 propagation is authorized by this checkpoint.
Final V9 controls terminal exit0: CONTROL_INSTALLATIONS_VERIFY PASS scenes=77 (scripted-native mounted controls, not keyboard/clearance).
