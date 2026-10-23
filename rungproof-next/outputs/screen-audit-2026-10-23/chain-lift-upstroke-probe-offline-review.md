# Chain-lift upstroke Stop observer: offline review

This is QA-tool validation, not native application proof. The child agent did not contact the screen bridge or send UI input. Root owns the actual run and physical pose acceptance.

- Helper: `chain_lift_upstroke_stop_probe.py`, SHA256 `875f95ec5836f5d6e6a67c1255500e32f7dd8a60c9de136705a39fa05768bf9b`.
- Actual saved baseline: `chainlift-r1-engineering-home.png`; original SHA256 `96af1fd2c8321cdf1a89996923c5e82e9f6b717b20c780f158ee97a63b8ee338`.
- Offline evidence: `chainlift-probe-offline-native-home-1792736336891026900/result.json` and `chainlift-probe-offline-guards-1792736336891591800/result.json`.

The actual home image parsed all ten required fields and all six other visible signal rows. Runtime was RUNNING at scan 204; lift_home=True, carton_on_lift=False, cycle_active=False, and lift_enable=False correctly rejected the upstroke predicate. The saved image had no Playback menu, so menu=False was expected. The owned persistent OCR worker exited with code 0.

Synthetic tests accepted the complete upstroke predicate and rejected each wrong-phase field, each missing required field, and a conflicting lift_upper value. The hard cutoff guard rejected the deadline. Synthetic rows are parser tests only.

The live helper makes no setup, Start, Run, Reset, or menu-opening inputs. Root must physically Start and leave the actual App Playback menu open. It requires two consecutive images with all ten exact signal values and RUNNING, then proves the Stop menu on that final image and checks its age is at most 0.5 seconds after pinned process/client health validation. It sends one Stop at (220,90), with no retry on uncertain delivery, and retains immediate, settled, and held screenshots/OCR/hashes. Operations are bounded to 25 seconds, including a three-second hold-capture reserve; worker cleanup is separately bounded by the reviewed base helper. The 11:00 UTC cutoff applies before actions and captures.

Unreadable, conflicting, stale, missing-menu, or unobserved-phase results leave the QA case pending. They do not establish an application failure. The helper reports endpoint signals and stopped scans/commands but cannot measure private carriage height or certify physical pose; root must inspect the original pixels and independent reviewer evidence. Fresh Start recovery is a separate native case, because this fixture clears cycle_active on App Stop and Run alone must hold.
