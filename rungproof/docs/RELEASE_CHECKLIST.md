# Release checklist

## Current Godot demo

The checklist below qualifies the historical Qt pilot and its package scripts.
It does not qualify the current Godot/C# application. Use the current
[demo release review](DEMO_RELEASE_REVIEW_2026-10-10.md) to establish the Next
offline artifact, build lane, supported configuration and acceptance record.
Until those gates pass, no external Next demo release is approved.

## Historical Qt pilot checklist

Use this checklist for every externally delivered RungProof artifact. A checked
item requires retained evidence; memory or a prior internal ZIP is insufficient.

## Scope and metadata

- [ ] `VERSION`, About, self-test, `package.json`, build metadata, ZIP/folder
      name, changelog, and Git tag agree.
- [ ] Supported configuration and known limitations match the actual build.
- [ ] Only Scene 2 is labeled production/native live; review scenes fail closed.
- [ ] Browser player and Scene Editor are absent from the supported package.
- [ ] Release commit is clean, reviewed, backed up remotely, and tagged.

## Source and package verification

- [ ] `tools/run_checks.ps1 fast` passes.
- [ ] `tools/run_checks.ps1 full` passes.
- [ ] `tools/run_checks.ps1 release -EnableRealPlc` passes from the release
      commit for a PLC-led pilot candidate. The lane must build and smoke-test
      the same guarded live capability; a read-only build is not equivalent.
- [ ] Locked PySide6 Qt lifecycle tests pass rather than skip.
- [ ] Package self-test reports the expected version, renderer, write addresses,
      no browser/server, and no PLC connection attempt.
- [ ] Window smoke reports one application process, no listener/browser, clean
      close, and no residual process.
- [ ] Package `SHA256.txt`, ZIP SHA-256, SBOM, dependency versions, and logs are
      archived with the release evidence.

## Visual and VM acceptance

- [ ] Product owner approves every Scene 2 asset visible in the pilot.
- [ ] Fresh target VM runs at the declared Windows/VM build and 1920x1080.
- [ ] Views A/B/C, supported orbit range, package motion, photoeye transition,
      pusher extend/retract, resizing, first/repeat launch, and Close pass.
- [ ] No conveyor/photoeye/rail/payload overlap or depth-order defect remains in
      the actual target-VM captures.
- [ ] SmartScreen/UAC and missing/corrupt-file messages are understandable.

## PLC/TIA acceptance

- [ ] Exact TIA V17 update, CPU order number, firmware, rack/slot, and network
      are recorded.
- [ ] Engineer-authored TIA exports import and compile without unresolved
      warning, conflict, namespace, or optimized-access problem.
- [ ] Read-only test is observed to write zero values.
- [ ] TIA watch table proves only the four declared PC-owned DB14 writes.
- [ ] Two cycles, Stop/fresh Run, Reset, readiness bits, heartbeat loss,
      network loss, reconnect/fresh Run, Disconnect, and Close pass.
- [ ] PLC watchdog behavior is observed independently from RungProof.
- [ ] If any PLC/TIA item is not approved, **Connect Real PLC** is absent or
      feature-disabled in the delivered pilot build.

## Security, distribution, and recovery

- [ ] Repository security scan completes with no unresolved reportable High or
      Critical finding and all accepted findings are dispositioned.
- [ ] SBOM/dependency review has no unresolved launch-blocking vulnerability.
- [ ] Qt/PySide LGPL distribution obligations are approved and packaged.
- [ ] Executable and installer are Authenticode signed and signatures verify.
- [ ] Clean install, upgrade, uninstall, and rollback pass on a fresh VM.
- [ ] Previous supported artifact and release source are independently
      recoverable.
- [ ] No secrets, private PLC projects, plant data, or unrelated files are in
      the artifact.

## Commercial and support approval

- [ ] RungProof name/trademark/domain clearance is complete.
- [ ] Ownership, employer IP, moonlighting, and reused-source rights are
      confirmed.
- [ ] Product license/EULA, safety disclaimer, privacy statement, and support
      policy are approved.
- [ ] Security reporting contact and response targets are published.
- [ ] Fresh-user quick-start test passes without developer assistance.
- [ ] Support can collect version/renderer/profile/readiness diagnostics without
      collecting credentials or unrelated machine data.

## Pilot decision

- [ ] 5-10 representative users complete the scripted pilot.
- [ ] Installation/setup time, completion rate, defects, unsafe interpretations,
      and support effort are reviewed.
- [ ] All pilot blockers are fixed and affected gates rerun.
- [ ] Product owner signs the release record and rollback choice.
