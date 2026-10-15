# Changelog

All externally delivered RungProof builds must be listed here. Internal
historical ZIP increments are evidence artifacts, not public releases.

## 0.2.0-pilot.2 - Unreleased

### Product scope

- Defines the first supported release as a controlled instructor/controls-lab
  pilot of the native Windows application.
- Supports the Scene 2 - Conveyor Pusher vertical slice. S03-S06 remain
  review-only; the browser player and Scene Editor remain source prototypes.
- Preserves explicit PLC authorization, exact DB14 ownership, read-only
  diagnostics, fail-closed unsupported scenes, and fresh-Run recovery.

### Release engineering

- Adds one authoritative `VERSION` file for application, package, artifact, and
  release metadata.
- Adds a launch plan, supported-configuration matrix, known limitations,
  release checklist, support runbook, and vulnerability-reporting policy.
- Hash-locks every Python build artifact, audits the clean build environment,
  verifies the vendored Siemens-interface source manifest, and emits a
  CycloneDX SBOM plus package integrity manifest.
- Makes the default pilot package fail closed for real PLC writes. A PLC-write
  test candidate requires an explicit build switch and remains unaccepted
  until its hash-bound bench evidence passes.
- Adds hash-bound delta qualification so an accepted PLC/TIA baseline can be
  carried forward only when the profile and target are unchanged and the
  corrected executable passes its affected live behaviors.

### Security corrections

- Rejects partial/extra PC-owned point cycles rather than preserving stale
  writes under a continuing heartbeat.
- Requires an exact PLC echo of the previously written PC heartbeat.
- Bounds imported browser-scene files and repeat-load intervals.
- Restricts the archived development server to its exact loopback Host value.
- Invalidates cached PLC health/status and PLC-owned command samples whenever
  the native session leaves Connected, repaints on connection-state changes,
  and rejects late in-flight cycle publication after Disconnect.
- Closes the non-daemon PLC worker if any later native-window initialization
  step fails, preventing a startup error from stranding the process.

### Product truth and verification

- Hides the in-memory Workspace fixture from the normal product UI and labels
  event evidence as local-only. The fixture remains available only to explicit
  visual-QA captures.
- Runs both the native player and source Scene Editor Qt suites in the locked
  release environment.
- Uses the package-module entry point in the source launcher and documented
  development commands.
- Removes a conflicting versioned ICU DLL collected by PyInstaller 6.21 so
  frozen Qt 6.11 loads the supported Windows ICU compatibility API.

### Unresolved before pilot delivery

- Scene 2 visual approval and target-VM 1920x1080 acceptance.
- The original TIA V17/S7-1500/DB14 candidate has retained live acceptance,
  but the corrected disconnect-telemetry executable needs its focused delta
  recheck before it inherits that baseline.
- Authenticode signing, installer/rollback acceptance, Qt distribution review,
  legal documents, formal name clearance, remote repository, and pilot users.

## 0.2.0-pilot.1 - 2026-08-05 internal baseline

- Completed the first full live TIA V17 Update 9 / S7-1500 / DB14 bench run
  and retained a hash-bound PASS acceptance for its exact executable.
- Verified real ladder execution, exact simulated-I/O scope, normal cycles,
  Stop/Reset/fresh Run, watchdog and network loss, reconnect, Disconnect, and
  Close on the isolated PLC bench.
- Superseded before pilot launch because the application displayed cached
  healthy PLC telemetry after explicit Disconnect. Its acceptance remains the
  unchanged baseline for the focused pilot.2 delta qualification.

## 0.1.18 - 2026-08-04

- Internal engineering package with native Scene 2, setup documentation,
  failure tests, and a passing packaged EXE smoke test.
- Not approved as an externally supported release.
