# Lessons that change the next agent's decisions

- **77 identities, not82.** Five named demos alias existing lab/scene IDs.
  Scenario menu, Browser and Viewer are navigation surfaces, not distinct
  implementations requiring invented duplicate inventories.
- **Scope each PASS.** A normal cycle does not prove every phase interruption,
  and a deterministic test does not prove a physical button responds. Preserve
  FAIL, BLOCKED, NOT TESTED and bounded verified behavior separately.
- **Do not manufacture requirements.** Lab10.6 does not ship an automatic
  sequential startup. QA fixture dwell times and restart policies are not
  implicit product specifications. Do not invent unexposed reset/retract/fault
  controls, completion points or physics beyond a finite supported endpoint.
- **LAD owns decisions.** Resolve input ownership and accepted scan ordering
  before editing plant motion. Do not replace executable learner logic with a
  hidden process sequencer. Distinguish command, feedback, permissive and status.
- **Accepted scan time differs from wall time.** Rendering/OCR load slowed
  progress. Fixed wall-time capture names were often premature. Prefer a
  readable actual state; diagnose timing only against a real requirement.
- **OCR is fallible.** Zone8/9 pulses and elevator upper feedback existed in
  native originals despite failed text matches. Inspect those frames before
  declaring a sensor failure. Do not use a filename such as `final` as proof.
- **Wait for deferred UI actions.** Modal dismissal and tab switches can appear
  in a later settled frame. Inspect the real dialog before typing or discarding;
  discard only owned QA drafts. Await one input batch before sending another.
- **Let menu scrolling settle.** A correctly recognized row moved before a
  click because wheel events were still arriving. Require a stable full-popup
  row layout, then verify the loaded title. Exact numeric identity matching can
  tolerate unreadable title letters without conflating Lab2.1 with Lab2.10;
  unreadable IDs and ambiguous rows still mean no selection.
- **Ground coordinates anew.** Desktop/client bounds changed1600x900 to
 1021x660. Old helpers safely rejected wrong size/occlusion. Use per-monitor
  DPI, exact process/window focus, bounds and unobscured-screen guards. At the
  smaller client, interface75percent exposed Browser rows clipped at85percent.
  Old coordinates, PIDs and bridge capabilities must not be restored as live.
- **One input owner.** Root drove UI; agents reviewed originals and fixtures.
  Batch routine real input/OCR locally, retain original SHA hashes and send only
  selected compact comparisons to the model. Do not print base64 image payloads.
- **Animation needs a visual witness.** Changed shadows or command flags do not
  prove mapped motion. CNC flute poses and fan/motor rotation were visible;
  dark empty belt and symmetric rollers were not reliably readable. The belt
  splice geometry is static and cannot serve as a moving marker.
- **Source experiments explain, not certify.** Parcel V4 diagnostic LAD and
  isolated model reproducers separated admission effects; count2 is still a
  failed batch. Keep original fixtures immutable and version diagnostic changes.
- **Reset equivalence needs adapter geometry.** V7 fresh-load success and two
  failed Reset repeats isolated history-dependent initialization. Rebuilding a
  model from a previously rotated world AABB can retain the wrong footprint
  after the rendered root looks home. Pure models that omit the actual delivered
  support callback do not test this seam. Restore only owned transforms; do not
  weaken support/clearance checks or overwrite unrelated workspace edits.
- **Qualify the phase before Stop.** Parcel public rows matched multiple phases;
  actual Watch30/30/class2 disambiguated the successful native handoff case.
  Coating's complete public predicate separates initial index from discharge.
  Group count3 alone precedes staging. Cable authorization is an Output cleared
  by Stop, so its reference requires fresh START; grouping/coating retain phase
  and resume on Run. These are different learner policies.
- **Do not invent stricter proof gates.** A visibly acknowledged physical timer
  retrigger plus expiry before another full interval proves the UI behavior;
  sampling the20ms input peak is not an extra required test. Conversely, absent
  class0 OCR or simulator/sirnulator recognition safely blocked early observers;
  those sent no Stop and were QA misses. Keep exact numerical corrections local.
- **Fresh acknowledgement must be fresh.** A persistent identical ACTION
  ACCEPTED footer can belong to the preceding press. Native Health event
  history supplies new timestamped entries without altering the process. Two
  idle endpoint images can also miss a complete replay between them; bound
  sample gaps against the actual state durations and preserve missed samples.
- **Reset is not universally all-zero.** Marker2.3 deliberately declares its
  initial OutputTrue. Native Reset restores white/True while STOPPED, then NC
  logic maintains it on Run. Preserve each scene's actual initial-state contract.
- **Read maintained defaults before toggling.** Demo5 declares ENABLE and
  PALLET initiallyTrue. The first test clicks turned them off; a later native
  False reading did not prove that machine Reset had cleared them. Distinguish
  an accepted toggle from proof of its resulting Boolean value.
- **Keep camera changes small.** Layout switches reframe the scene; non-demo
  framing includes room geometry. Zoom moves toward that deeper target and can
  pass nearer equipment. Reframe, take small native pan/zoom steps, and re-ground
  controls. Each camera wheel event applies one factor; a large delta is not a
  reliable number of zoom steps. A normal File-picker filename plus Return
  commits selection without relying on changing list-row positions.
- **Frame Process has a demonstrated layout limit.** Demo3's narrow Engineering
  aperture repeatedly hides the process behind a flat wall; Immersive restores
  it. This is QA-UI-003, with seven independently reviewed originals. Source
  suggests a low camera ray fitted farther out without room-occlusion handling;
  actual private camera coordinates were not observed. Keep that visibility
  defect separate from the functioning controller and scene-load result.
- **Preserve honest history.** Later native reviews can supersede ledger cells
  without deleting original failed attempts. Avoid an expanding pile of broad
  chronologies; keep a focused current requirements view with exact references.
- **Use the right backup layer.** Git contains source and reviewable evidence;
  raw captures, ignored toolchains, local runtime workspaces and Git internals
  also need a verified local backup. Public uploads require a content scan;
  local session capabilities and private runtime state stay out of public Git.
- **Verify bytes, not archive membership.** Historical path lists cannot prove
  that a modified ignored file is archived. Exact Git blobs, extracted archive
  hashes and paired current D hashes identify the required incremental delta.
  Find private task descendants by parent thread ID; inherited working folders
  can be obsolete. Verify native Godot userdata with the native PowerShell view
  independently of Python's view, and preserve all historical receipts.
- **Verify the GitHub identity.** This workstation's CLI default was the work
  account while RungProof belongs to `mnwinter`. A per-command environment used
  that existing personal login, without changing the global default or storing
  tokens in files. A release error mentioning workflow scope was misleading
  until the account was checked.
- **Preserve aliases and repeated content deliberately.** exFAT cannot hold
  NTFS junctions. Record internal relative targets and rehearse restoration.
  Toolchain archives can store unique SHA-256 contents with a complete path map;
  restoration must create independent files, verify hashes and preserve any
  existing different file rather than silently overwrite it.
