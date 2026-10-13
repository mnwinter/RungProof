# Constrained drive connection prerequisites — implemented, runtime pending

This is the ratified one-belt connection prerequisite slice under existing authorization. It does not implement a new preset, source scene, material transfer graph or physical PLC connection. Material V10 human approval and propagation to the other 75 scenes remain a separate held checkpoint.

## Implemented

- Optional explicit version-1 DriveConnection relation in workspace version 9, with a source fingerprint and three legal derived point names. Original versions and their preset/template contracts remain distinct.
- Pure policy validates the existing belt's exact command/feedback directions, BOOL/float32 types and m/s units, source identity, pose, normalized-name collisions and conflicting mappings. Detached maps remain readable engineering work; mapping eligibility alone never grants runtime execution.
- Source augmentation copies the original scene and appends exactly three owned typed declarations. Its derived scene removes the old external PLC profile because the added I/O has no authorized physical mapping; the original scene/profile is unchanged.
- Pure explicit-clock float drive model matches existing acceleration, end-speed travel integration, measured Running threshold, command/permissive coast, immediate application Stop and Reset. It owns no timer or renderer callback.
- Actual native repository save/load and integrated valid/invalid draft preservation pass. The app explicitly rejects version-9 drive loading before replacing current scene/runtime/draft/layout until the accepted-clock adapter is implemented; it cannot silently downgrade the saved relation.

## Evidence

Final DLL B0ED980FDE51A58397B3D446208055C69FCE03469C656496E30F4B93280AC609; build zero warnings/errors. Root reran policy 48 checks and pure model 72 checks successfully. Six native repository checks and final app-shell verification exit zero. Virtual-controller regression exits zero and constructs/attempts no physical PLC transport. Logs are in `.tools/studio-verification/workspace-drive-*`; frozen hashes are in drive-connection-prerequisite-identity-v1.json.

TDD evidence preserves missing-policy/model functional RED and native unsupported-version RED. A second actual-load RED proved silent relation downgrade before the temporary runtime guard was added. The final native guard probe intentionally emits an unsupported-load WARNING; it contains no engine ERROR.

The broader `--verify-workspace` headless run passed the new repository assertions but failed mouse-dependent gizmo/group-gizmo checks. That original failing log is preserved; it is not a green overall workspace result. The dedicated `--verify-workspace-drive-repository` flag isolates the bounded checks.

## Required next implementation

Wire the same effective source declarations through composition, editor bindings and current-draft validation; register the actual placed controller; disable only its autonomous physics callback; advance/project once per accepted scan; publish validated PC-owned feedback after the plant and sample it on the next scan. Prove 20/40/100 ms actual motion and history, intended/wrong/invalid current drafts, exact joint reopen, Stop/Reset/Pause/Step, observer equivalence and detach/rebind identity retirement. Remove the temporary load guard only once this adapter can preserve and execute the relation correctly.

Missing feedback must Stop/freeze and display unavailable, rather than show declared initial false/zero as measured feedback. Command detach may coast only with valid remaining feedback, current relation identity and eligible current program. No runtime or visual acceptance is claimed for this prerequisite slice.

Typed lesson closure remains independently frozen on B892 (456 typed checks, 768 scan checks, 18 root/Dot-reviewed native images, PASS_WITH_LIMITS). Those tests are not relabeled as the newer prerequisite binary. Whole-product acceptance remains IN_PROGRESS: 8 authored workflows have bounded qualification; 69 tag-only exercises still need functional work. The intermittent Godot Handle finding and typed caption/history presentation limits remain open.
