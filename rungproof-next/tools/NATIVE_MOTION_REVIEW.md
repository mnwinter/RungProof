# Native continuous motion review

This is local offline QA. A completed recording is not visual acceptance and is
not a physical collision, contact-force, between-frame clearance or PLC test.
The helper supports only the vision sorter and pallet robot, with FR/FL/RL/RR/Top
cameras. Sorter routes 1–4 use the existing simulated input actions. Both fixtures
use the ordinary project-open, verify/load and controller scan path at 20 ms.
No plant positions, derived feedback or PLC commands are assigned by the helper.

## Prepare the isolated capture project

Build the ordinary project first. Create `.tools/native-movie-project/project.godot`
from the root configuration, changing only the four viewport/window dimensions
from 1600×900 to 2400×1350. Its `assets`, `scenes`, `src`, `programs`, `.godot` and
`artifacts` directories are Windows junctions to the corresponding root directories.
This preserves the ordinary app configuration and the compiled assembly/resources.
Do not recursively delete the junction targets.

The bundled Python runtime requires Pillow. Set `DOTNET_ROOT` to `.tools/dotnet`;
the coordinator does this for each child. Run from the repository root:

```powershell
python tools/test_native_motion_movie_review.py
python -m unittest tools.test_native_motion_sequence tools.test_build_native_motion_review_sheets
python tools/capture_native_motion_review.py --output .tools/native-motion-png-fresh --workers 2
```

The smoke check distinguishes correct rejection of unsupported headless PNG
capture from a known Godot dummy-renderer shutdown crash after rejection. It must
not be reported as a clean supported headless recording. Ordinary/native positive
cases require exit zero and no runtime ERROR logs.

## Recording provenance and coverage

The coordinator records all 25 cases in bounded batches of at most two native
processes. Each writes to its own directory. It preserves failed runs and refuses
to overwrite or reuse captures with a different fixture, camera, route, source
bundle, helper, assembly or capture-project identity. Use a fresh output directory
after changing the build. Every source PNG is retained.

`capture-manifest.json` identifies the command, fixture SHA256, source bundle,
helper source, compiled assembly and capture configuration. `sequence-integrity.json`
lists every consecutive PNG and its hash after decoding/format validation. It
requires 2400×1350 native PNGs, the exact MovieWriter frame count, consecutive trace
IDs and no runtime ERROR logs.

`trace.jsonl` begins with setup, then one callback per traced rendered frame.
`renderedFrame N` maps directly to `frameNNNNNNNN.png` for these captures. Godot
writes one additional final PNG after the completion callback disconnects; it is
explicitly recorded as untraced and still requires visual inspection. A partial
trace or missing PNG invalidates the sequence. Trace screen coordinates are
diagnostic image-review coordinates; they never drive plant motion.

## Visual inspection

Inspect native frame zero to choose a fixed rectangle containing all relevant
moving geometry for that camera. Do not reuse an angle's rectangle blindly for
another angle. Generate readable native-pixel consecutive load crops:

```powershell
python tools/build_native_motion_review_sheets.py .tools/native-motion-png-fresh/vision-r1-FR --roi 650,400,1880,950 --loads --tile-width 300 --tile-height 240 --columns 5 --rows 5
```

The builder verifies every source and trace boundary. Its manifest starts with
`GENERATED_NOT_YET_VISUALLY_REVIEWED` and `accepted: false`. Generation, decoding,
hashing and tests do not constitute visual inspection. Each sheet must actually
be viewed at readable resolution. Retain per-frame sheet/tile/crop mappings,
reviewed ranges and observed clipping/occlusion. Exact RGB-byte equality can map
identical geometry crops to an inspected representative; hash equality alone
does not establish that proof. Preserve complete originals for all mappings.

Record occluded contacts explicitly. All-angle native visual review and analytic
swept/between-frame clearance are separate evidence categories.
