# V14b bounded identity review

Read-only verification, 2026-10-11. No source changes/build/engine. Full input/capture hash records: industrial-mount-evidence-identity-review-v14b.json.

Current425/425 frozen source hashes match; both immutable inventory inputs match. Frozen/current Debug DLL is **4D0A91BC3E0D7C170813EED5B9D8D5CC5F90133E30F0E03F47318AC5A5D1966A**. Four fresh PNGs match image hashes, expected assembly identity, engine hashes and unchanged-during-capture sidecars. Capture logs contain zero ERROR/Exception/FAIL, with expected TAA renderer warnings. Root reported capture batch exit0; this audit did not launch or independently measure process exit.

Fresh images: v14b-pickup-initial.operator.png, v14b-pickup-toggled.operator.png, v14b-barrel-meter.field.png and v14b-barrel-source.field.png.

## Exact V14→V14b frozen source deltas

| Input | V14 SHA256 | V14b SHA256 |
|---|---|---|
| src/Scenes/SceneComposer.InstallationLegends.cs |FE6527E63F79FBE7BF41967BDBDD15EA82D483B24A7ED6D0976287B904E9D801 |D6DF7385DFFD91A21F85129CEDEBE677FAF17BF2808EBDD610B81EA09114CFEA |
| src/App/Main.ControlInstallationCapture.cs |03DA15EAAFCCF3FE9B8FAB5A5D5FA2D08D32407E74A74A05E0F39AB8B1DA4C60 |6512BB8D71C18788292B2B2E4CFD162341DD082AC597D09357309FE6A95EDA34 |
| tools/capture_demo_nonfocus.ps1 |007A8A1D5048C1EDC4C2CA717F5352228489D1DDD907C392CC46E3ED63E1A642 |0AC149B555CC18CCB78C639AB926DF25038B1A95DA8BD5C2D581812731F70F92 |

No other frozen source changes or removals. The preserved installation-legends-before-pickup-repair-v14.cs matches the V14 helper hash; direct diff against current helper contains only the exact pickup_mode branch's retained dial/handle/tick/ordinal-envelope measurement. Thirteen other V14 operator frames therefore remain valid **historical V14 images with unchanged scene-specific label source**, not new V14b captures. Their sidecars must retain A17D152B…686A12. Pickup V14 image is superseded for repaired MODE placement by the two fresh V14b frames. Capture-camera/tool source also changed; this audit does not claim identical diagnostic framing from hash equivalence alone.

## Native terminal evidence

Actual control-installations-v14b.log terminal reports PASS77,71 authored case scenes,163 functional picks,138 accepted presses,17 selectors and zero ERROR/Exception/FAIL. Actual industrial-mounts-v14b.log terminal reports cases2205/passed2205/failed0,77 scenes,121 light candidates and54 display candidates; zero ERROR/Exception/FAIL. The JSON's initial generic terminal recognizer did not match the mount gate's `INDUSTRIAL_MOUNT_RESULT` spelling: its null terminal is a parser limitation, not a failed or pending native gate. This Markdown records the directly read actual terminal. Neither terminal establishes other sequential gates still running at this snapshot.

No identity mismatches found. Visual readability is separate from identity; no PNG visual acceptance is asserted by this report. Handle OPEN; material75 HELD.
