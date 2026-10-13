# T7 source evidence — challenge labels

Source scope: new `rungproof-next/src/App/Main.ChallengeLabels.cs` and fixed trusted
`challenges/presentation/conveyor-stop.presentation.json`. No machine geometry,
material, asset, camera implementation, PLC transport, or grading trace was edited.
The root integration owner supplies entry/process/leave lifecycle calls and build
and native qualification evidence.

## Observation and presentation boundary

`InitializeChallengeLabels()`, `UpdateChallengeLabels()`, and
`ClearChallengeLabels()` are private void hooks in Main. Update consumes the
main-thread `CaptureConveyorChallengeState()` private-copy record and current
virtual snapshot. Start/Stop are mapped by BOOL input role and symbolic binding;
output uses `SceneIoImageMapper.CommitBoolOutputs`, so renamed tags retain their
meaning. Missing snapshot/binding displays unavailable rather than FALSE.
Photoeye is simulated feedback; conveyor value is an offline controller command,
not shaft-motion feedback. Object position is the internal leading edge in model
meters, not a measured dimension or visual world-coordinate value. Object
presence and exits come from canonical capture, not position inference.

Metadata schema 1 declares sceneId, camera focusEquipment/direction/fov, and three
anchors with id/equipmentId/offset. Only this fixed resource path is read. Label
anchors accept exact known equipment IDs and finite bounded 3-vectors; arbitrary
NodePaths are not consumed. Camera fields are intent for root integration; labels
do not change camera state. The direction `[10.5,5.6,9.5]`, FOV 43 preserve authored
perspective intent relative to its target `[0,.9,0]`.

Invalid or missing anchors produce relevant missing I/O text beside the station,
outside the belt corridor. Labels billboard with depth testing and white text
with dark outline; textual TRUE/FALSE/unavailable carries meaning independently
of color. Native readability, depth occlusion, overlap, fallback fit and process
aperture at supported DPI remain required review gates. Source coordinates alone
do not pass those gates.

## Reused asset evidence and separate rights gate

SceneComposer resolves the existing conveyor and carton catalog assets. Current
delivery SHA256 observed on 2026-10-10:

| Asset | SHA256 |
|---|---|
| `assets/material_handling/belt_conveyor_600x6000/delivery/belt_conveyor_600x6000.glb` | `025FA017779CCF716A1E8A89B06A9C71C95BF160D3A437DE63DBB25B8EDD85CB` |
| `assets/scene_loads/corrugated_shipping_carton/delivery/corrugated_shipping_carton.glb` | `6834B43C26E3E12B976839C3F9BE44EF81BD45072FBA04E7B4CD1580D7BACA84` |

Existing conveyor independent recognition records pass, confidence .98.
The carton candidates catalog still says candidate, topology/material unreviewed;
its independent recognition .99 notes stylized rounded edges/strap and embossed
lettering. These are historical recognition evidence, not approval of this new
assembled challenge. The existing procedural generation sources are
`tools/modeling/build_belt_conveyor.py` and `build_scene_load_assets.py`.
Their presence does not establish complete historical asset rights, notice,
label-mark or distribution clearance. Human distribution rights approval remains
a separate open gate; no external delivery is authorized by this evidence.

## Verification limits

Exact equipment IDs/transforms and model capture/mapping APIs were inspected.
JSON finite-offset/equipment consistency checks and root compiler verification
are requested. No app launch, native input, screenshot capture, plant connection,
physical measurement, or release acceptance was performed by this source owner.
Actual rendered idle/travel/beam/Stop/blocked/Reset qualification belongs to the
root native review owner, including results/hints aperture and DPI checks.
