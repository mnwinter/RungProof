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
- **Preserve honest history.** Later native reviews can supersede ledger cells
  without deleting original failed attempts. Avoid an expanding pile of broad
  chronologies; keep a focused current requirements view with exact references.
- **Use the right backup layer.** Git contains source and reviewable evidence;
  raw captures, ignored toolchains, local runtime workspaces and Git internals
  also need a verified local backup. Public uploads require a content scan;
  local session capabilities and private runtime state stay out of public Git.
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
