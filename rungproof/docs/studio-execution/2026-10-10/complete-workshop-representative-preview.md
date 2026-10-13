# Complete workshop representative preview — v4

Human steering supersedes the earlier five-demo visual acceptance: distinct realistic colors, complete spatial environments and understated material detail are required. The current milestone is ONE representative conveyor workshop for human review. Replication to the other four demos is held; their original rendered baselines and read-only scene-specific proposals are preserved.

## Actual render review

All paths below are relative to `rungproof-next/.tools/studio-verification/`:

| Evidence | Path | SHA256 |
|---|---|---|
| Matched before, operator view | `complete-dispatch-before.png` | `9DEF73E5D6140D070EE853EEFD1BDE108DC7BD0A3FFFB7109417D2981B65F8CC` |
| Current v4, operator view | `complete-dispatch-after-v4.png` | `63570060CE5BBCE46C91B27A508381CC87374C0A454FCE5FEB5400618D65B135` |
| Current v4, split view | `complete-dispatch-after-v4-split.png` | `6A4867BE53619AA60E0F9BACE681931D333CB143E4F66DC747B5333BCF41F448` |

These are actual Godot Windows source-project renders, Compatibility renderer, 1600x900, stopped state. Operator before/after share the existing gameplay camera and authored process pose. They are not renders of a newly exported distribution candidate.

The established capture launcher verified native no-focus at startup and reveal, secondary screen 1 (primary 0), mouse passthrough, correct scene identity and saved PNG. It removed its temporary override afterward. No keyboard/mouse interaction or focus takeover was used in this visual revision.

Root, Dot and the art owner inspected v4 operator and split renders. Dot and the art owner both judge v4 READY FOR HUMAN REPRESENTATIVE PREVIEW, not full final acceptance. The bridge independently inspected the representative render and comparison. V1–v3 remain preserved rejected/intermediate evidence.

Visible improvements: broad neutral-warm concrete with quieter grain; side-wall and ceiling returns within the gameplay aperture; a physically recessed, lit loading corridor; connected packing workstation and stocked parcel rack; timber/kraft stock, service utilities and a low trolley. Conveyor, carton, photoeye, station and stacklight remain visible in the sampled views. Remaining style limitations: bright right wall, nearly black overhead trim, peripheral props cropped in split view and simplified procedural artwork.

Bridge comparison file: `C:/Users/mattn/Documents/Codex/2026-10-07/task/rungproof-complete-workshop-before-after.png`. This comparison was inspected by root and uses the matched actual gameplay captures; it is not a concept rendering. Library upload is owned by the bridge; no upload is claimed here.

## Cause and bounded changes

The previous shared environment explicitly used blue-biased concrete, steel, trim and recess colors, together with blue ambient and cyan directional fill. Broad slate floor area dominated the viewport. The revision does not rely on changing small props.

- `DemoEnvironmentPresentation.Dispatch.cs`: original procedural workshop architecture and contextual objects for Demo3 only. Decorative meshes remain siblings of the process composition, with no colliders, I/O, picking targets or contribution to process camera framing. Upright context stays outside the conservative process circle; roof context stays above and behind its envelope.
- `StudioSceneMaterials.cs`: cached original 256px procedural grain, mipmaps and distinct paint/metal/rubber/wood/cardboard/concrete responses. Concrete mapping spans the visible bay to avoid coarse repeated tiles. Palette diffuse values are converted from sRGB to linear; this fixed the initial washed-white material response. No external textures, paid models or new Blender dependency were introduced.
- `Main.DispatchLook.cs`: scene-local duplicated environment, neutral fill and restrained warm key; named imported structural finishes receive local material overrides. Existing signal, control, label, sensor and belt resources retain their semantics. Previous environment and lights restore on departure.
- `Main.DemoEnvironment.cs`: attaches the scene-local presentation treatment and restores it during removal.
- Capture-only performance logging in `Main.cs`: records the sampled renderer metrics when the existing opt-in capture saves its image.

The four other environments were not replicated or restyled. Existing accepted controller/challenge behavior and unrelated WIP are preserved. Original V5 package bytes remain untouched. No new export, commit, push, deployment, public distribution or live PLC operation occurred.

## Verification

Final source build passed with zero errors and warnings. Final affected checks exited zero with their PASS markers and no ERROR/FAIL matches:

- `complete-dispatch-final-demo-sequence.log`: conveyor latch, sensor hold, Stop and Reset integration.
- `complete-dispatch-final-challenge-ui.log`: invalid starter feedback, guards, independent outcomes, progress handling, cancellation/locked save, stale-award rejection and camera restoration. Keyboard and screen-reader acceptance remain false.
- `complete-dispatch-final-shell.log`: all five demo menu selections, scene/editor/program/interface matching, scene-draft isolation and unsaved-work guards.

The v4 captures sampled 30 FPS under the launcher's 30 FPS cap. Operator view: 3.19ms process monitor, 965 draw calls, 90,838,456 bytes reported video memory. Split: 3.16ms, 1122 draw calls, 91,514,882 bytes. These are single capped samples on the current development machine, not a sustained benchmark, minimum-GPU qualification or input responsiveness test.

The earlier Demo4 zero-count report and behavior HOLD are withdrawn: the original scan-1162 screenshot shows count one with validity false, expected below the authored five-count threshold. See the correction and exact originating sequence in `native-input-v5-evidence.md`. First Demo4 entry after challenge departure remains separately unverified, not a demonstrated defect. Native compact layout/DPI/screen reader, clean-machine, asset-rights and distribution gates remain open. Human visual feedback on v4 is the next gate before implementing the other four environments.
