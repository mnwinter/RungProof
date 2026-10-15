# RungProof security policy

## Supported security scope

Only the version named in `VERSION` is being prepared for the controlled pilot.
No older internal ZIP should be treated as a supported or patched release.

The supported product surface is the native Windows application and its exact
Scene 2 profile. The browser player, local HTTP server, Scene Editor, review-
only scenes, and generic/custom PLC profiles are development surfaces.

## Security invariants

- RungProof starts disconnected and never connects or writes automatically.
- A persistent PLC session requires explicit operator authorization after the
  exact endpoint and DB write scope are displayed.
- The supported profile may write only the declared PC-owned DB14 points. It
  must not write physical `%I` or `%Q` addresses.
- Read-only PLC diagnostics must contain no transport write call and must not
  overlap the persistent writer.
- Unsupported scenes, profile mismatches, readiness loss, stale cycles,
  transport failure, and malformed input must fail closed.
- Recovery or reconnection requires a fresh operator Run.
- The PLC watchdog is authoritative. RungProof may claim only that local scene
  commands are safe; it cannot prove unobserved physical outputs safe.
- The native package must not contain a browser/server runtime, start a
  listener, or leave a PLC-capable process running after Close.
- Scene files remain symbolic data and must not contain executable code, PLC
  endpoints, absolute DB mappings, or physical I/O addresses.

## Reporting a vulnerability

Until a public security address is approved, report suspected vulnerabilities
privately to the product owner. Do not include PLC credentials, plant network
details, proprietary TIA projects, or production data in a public issue.

Include the RungProof version, Windows environment, affected file or workflow,
reproduction steps, observed versus expected behavior, and whether any PLC
connection or write was attempted. The permanent reporting address and response
targets are pilot-launch approval items.

## Safety boundary

RungProof is a training and controls-bench tool, not a safety system. Security
testing must use offline fixtures or an authorized isolated bench with no
production field I/O. Do not bypass machine safety, plant procedure, lockout/
tagout, network controls, or PLC watchdog logic to reproduce a finding.
