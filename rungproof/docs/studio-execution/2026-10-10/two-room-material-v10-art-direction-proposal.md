# Exact-two material V10 art-direction proposal — read-only studio review

## Finding and chronology

I personally inspected all four qualified V9 full/split images. V9 resolves the raw-sRGB/exposure defect, but does not convincingly resolve the request for richer color and texture. Classroom cream rear wall, sandy olive floor and gray sage return have similar muted values; dispatch's huge pale floor and pale rear plane dominate, while coated steel reads like the same mottled plaster. Existing wood and cardboard remain useful warm focal accents. Controls remain readable and unobstructed. No grid or moire was seen.

V9 classroom capture began 2026-10-11 13:27:19Z. The human-review document still identifies V6 and retains the original flat/bland quotation. Available records do not establish that the human specifically viewed and rejected V9. This proposal responds to the continuing unresolved criterion, not an invented new V9 rejection.

Historical V9 DLL: FA9A837D4E8F38F01D4398C68E54ACEFB47175CB00ABE6AB98E5BC6B74ED3772. Current app binary includes later unrelated work; V9 images are qualified historical material evidence, not current-binary screenshots.

## Proposed visible hierarchy

| Surface | Classroom Lab 2.1 | Dispatch Lab 4.8 |
|---|---|---|
| Dominant floor | #747F7C cool medium gray resin; smooth sealed surface | #8D9692 neutral mineral concrete; deliberately cooler than kraft/timber |
| Rear wall | #C9C0AE warm plaster | #8CA5AE cool coated-steel gray |
| Side return | #71988D sage painted wall | #76917F sage painted steel |
| Passive trim/support | retain restrained green-gray; keep wood warm | #647978 midtone steel; distinguish from rear cladding |
| Warm focal accent | existing timber worktop and cream notice | existing kraft/cartons/pallet and packing apron; do not add boxes |

These are candidate source-sRGB colors, not promises of exact lit pixels. Preserve blue/red/green/yellow functional heads, optical lenses, controller materials and imported equipment finishes. The floor must stop competing with the task station; cool/warm separation should survive split view. No geometry or room envelope changes are proposed.

## Material identity rather than louder generic noise

Current ReferenceMaterials.cs lines36–45 feeds every role the same 12/48-cell smooth noise and hash. Tiny albedo differences plus roughness/normal changes do not produce recognizable material identities. Replace this opt-in exact-two factory's per-role map recipe, without mutating global StudioSceneMaterials or cached original catalog materials:

- Resin: nearly uniform albedo; fine sparse aggregate flecks in roughness, very low normal relief. Target roughness .40–.52. Remove coarse cloudy roughness that makes it look sandy.
- Plaster: diffuse albedo with at most 2% broad variation; fine irregular stipple relief, roughness .78–.88. No metallic or heavy mineral relief.
- Painted steel: nearly uniform pigment, subtle fine orange-peel roughness/normal; roughness .45–.60 and low metallic contribution because opaque coating covers steel. Do not use directional stripes as brushed grain.
- Concrete: fine nonperiodic aggregate distribution and restrained roughness .75–.90; broad albedo variation <=2%. Rare faint localized wear may modulate roughness, not create brown stains/clouds. Medium neutral base, not sandy yellow.

Keep mipmaps and triplanar mapping. Current tiles span about 2.9m plaster, 4m steel, 4.5m resin and 5.6m concrete; 48-cell detail corresponds roughly 6–12cm, too large to sell microsurface identity. Proposal: fine mineral/stipple features around 1–3cm, with larger 20–40cm variation confined mostly to roughness and very low amplitude. Quantify actual chosen texel/world scales after implementation; inspect both distances for repetition and shimmering. Texture resolution must support the chosen scale rather than merely increasing noise frequency beyond useful texels.

## Lighting proposal and scope

Do not increase total exposure or revert the proven raw-sRGB contract. First redistribute bounded directional energy toward fill: classroom trial key/fill .22/.24 (ambient .20 unchanged), dispatch .24/.22 (ambient .18 unchanged). Keep local fixtures initially at V9 values. This reduces crushed returns/hard shadow contrast while maintaining broad surface headroom. Judge actual renders before changing local fill further. Compatibility ShadowBlur did not remove crisp shadow silhouettes; do not claim a softness fix from that setting alone.

Exact source seams: Catalog.cs lines40–43 floor roles; ArtDirections.cs ArtFinishedRepresentative lines337–343 classroom and372–394 dispatch/recess; ReferenceMaterials.cs private ReferenceRoleSurface; Main.DispatchLook.cs exact incoming ID branches128–140 and restoration175 onward. Root must preserve the existing captured environment/colors/energies/shadow restoration contract. No other75 materials, geometry, controllers or signals change.

## Evidence and acceptance gate

1. Record source diff and pinned DLL after coordinated build. Run existing environment/control verifiers, including lighting restoration, without treating their PASS as art approval.
2. Capture full classroom and dispatch first at matched V9 camera/stopped state. Inspect palette hierarchy and exposure before spending on split renders.
3. If candidate improves, complete both split views under the same DLL and record four PNG/sidecar identities. Personally inspect all four; Dot/root independently review.
4. Visible acceptance: resin reads smooth rather than sand; plaster and coated steel visibly differ; dispatch floor remains neutral with visible relief and warm packing contrast; side return is not nearblack; broad faces retain texture rather than wash nearwhite; no signal obstruction, grid/stripe/repetition/moire. Sparse room composition remains disclosed, not claimed solved by texture.
5. Human aesthetic approval remains open; no other75 propagation until that gate is satisfied.

This document is a proposal only. No production source, engine session or build was changed in this audit.
