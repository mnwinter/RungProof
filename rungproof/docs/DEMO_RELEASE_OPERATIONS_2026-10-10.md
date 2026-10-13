# Offline demo release operations

Status: prepared procedure; external delivery is not approved. Release owner, recipient environment, support contact, asset-rights approver and signing identity are unassigned.

## Recommended initial pilot target

Provisional engineering target: Windows 11 x64, a modern four-core CPU, 16 GB RAM, SSD with at least 2 GB free for extraction and local documents, GPU/driver supporting the package's Forward+ renderer, and a 1920×1080 display. Use the 1600×900 editing viewport where practical. These are proposed qualification targets, not measured minimum requirements or a recommendation to buy hardware. Validate a representative lower-spec machine before promising it as supported. Godot documents renderer/API requirements separately from workload-dependent performance in its [renderer overview](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html).

Start with a small invited offline pilot and the five authored demos. Keep the existing full product scope visible in the assessment, including experimental scenes. The user should own initial pilot support internally; select a real contact address and realistic response commitment before distributing the package. This recommendation does not assign or publish contact details.

For rights, reconcile procedural generation scripts, source models and source/reference registers with every packaged resource. Record creator, origin, license or ownership declaration, any third-party inputs and required attribution. Existing generic-OEM comparison notes and quality approval are supporting context, not redistribution permission. The user does not need to guess license names: unresolved entries stay UNVERIFIED until evidence establishes them.

## Freeze and approval record

Record the reviewed source commit plus dirty-source disposition, final package SHA-256, complete acceptance ledger, supported Windows/hardware/display configuration and known limitations. Resolve asset permissions and applicable notices before approval. Freeze a fresh artifact after any product, notice or signing change and rerun affected checks. Preserve earlier artifacts and recipient documents.

The current internal ZIP is unsigned. Signing requires an authorized code-signing identity and available signing tooling. No certificate has been selected, acquired or used. Once supplied, sign the intended executable(s), verify the signature and timestamp, rebuild the file manifest and ZIP, then rehash and retest that signed artifact. The existing hash cannot identify a signed replacement. Do not instruct recipients to disable Windows security protections to launch it.

## Recipient qualification

Use a clean supported Windows machine and standard account, offline, without SDKs or the source checkout. Copy the entire package to a path with spaces. Record Windows build, account type, CPU/GPU/RAM, GPU driver, renderer and display scaling. Test first/repeat launch and closing the app. Verify all five supported demos, complete Demo 5's four-carton layer and extra-load behavior, phase-specific Stop/Run/fresh Start recovery and Reset. Save and reopen a project; reject malformed input while retaining the current work.

Repeat from a read-only installation location with writable normal user data. Test missing runtime/data files using a disposable copy, not the recipient's installation. Confirm errors identify the missing component without modifying user projects. Evaluate keyboard-only operation, focus visibility, supported DPI settings and assistive technologies. Record startup, scene-switch response, frame pacing and memory over a declared soak duration on the proposed minimum hardware.

## Support and recovery

An issue record needs the package hash/build identity, exact steps, expected/observed behavior, selected demo, execution source, renderer, environment and relevant console output. Review logs before sharing; omit credentials, plant data, user project contents and personal paths unless deliberately necessary and authorized. Use a named support contact and response expectation before external distribution.

If files are missing, restore the whole package from the verified archive. If a project fails to open, preserve the original file and report the error; do not overwrite it with a blank project. Stop testing a failing build when it crashes, loses saved work, unexpectedly constructs external transport or contradicts its offline policy. Retain the failing artifact and reproduction evidence.

Rollback uses the previous approved package and its recorded hash. Back up user projects/settings first and check format compatibility before opening them in an older build. The baseline retained during this review is not an approved rollback release. No previous approved external release has been identified.

## Decision table

| Decision | Required owner input |
| --- | --- |
| Supported Windows and minimum hardware | Declare a target configuration, then obtain acceptance evidence |
| Asset rights | Supply creator/source/license and redistribution evidence for catalog and embedded resources |
| Signing | Supply authorized identity or approve a documented unsigned pilot policy |
| Delivery | Select recipients/channel and approve the frozen artifact |
| Support | Name contact, expected response and diagnostic retention policy |

Preparing this procedure does not close those owner decisions or their acceptance gates.

## Local tooling evidence, 2026-10-10

Read-only command discovery did not find `Get-VM`, `VBoxManage.exe`, `vmrun.exe` or `signtool.exe`. A search under the usual Windows Kits 10 bin location did not find a signing tool. The `vmcompute` service is present but stopped; no ready clean Windows test machine is established by that fact. These bounded checks do not prove that no other virtualization or signing facilities exist. No service was started, VM installed, account created or identity acquired.

The inbox Windows PowerShell host refused the package-checker fixture script with `UnauthorizedAccess` because scripts are disabled; its policy scopes were all Undefined. The passing checker evidence uses the existing PowerShell 7 environment. Do not claim that a stock Windows recipient can execute the script unchanged, and do not instruct a policy bypass. Signed-script or launcher qualification remains necessary alongside the unsigned application signing/distribution decision.

The delivered PE inventory records 187 locally Valid signatures and four unsigned files out of 191. Local Microsoft runtime signatures do not sign the application or approve the product. Exact package identity remains the manifest and ZIP hash, particularly because the application assembly version embeds the baseline Git commit despite uncommitted fixes.
