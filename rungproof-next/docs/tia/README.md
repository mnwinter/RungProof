# TIA evidence registry

`evidence-registry.json` is the machine-readable inventory for the four TIA
evidence states of every `LadderInstructionCatalog` key. It complements the
human evidence matrix; it does not replace an installed-TIA comparison and it
does not assert overall parity.

## Schema version 1

The root contains:

- `schemaVersion`: registry schema version. The current value is `1`.
- `target`: vendor, product, preferred documentation version, CPU families,
  and the claim boundary.
- `entries`: exactly one entry for every catalog key.

Each entry contains:

- `key`: exact `LadderInstructionCatalog` key.
- `supportClass`: `siemens-subset` or `rungproof-local`.
- `source`: zero or more source records with `title`, HTTPS `url`, `version`,
  and an explicit `scope`. Sources are copied only from the current evidence
  matrix; URLs are not inferred from another instruction's naming pattern.
- `gates`: exactly `offline`, `selected`, `edit`, and `monitor`. Every gate has
  a globally unique stable `id`, independent `localStatus` and `vendorStatus`,
  and zero or more `artifactIds`.
- `artifacts`: evidence metadata referenced by gates. Paths are repository-
  relative. The current Siemens-facing gates bind hashed source-tree summaries,
  not volatile `.tools` paths. They remain `ephemeral-local` while this entire
  source tree is untracked; a hash proves identity, not durable availability.
- `limitations`: explicit boundaries that prevent the entry from being read as
  a broader parity claim.

## Status values

`localStatus` describes only RungProof evidence:

- `not-assessed`
- `automated-pass`
- `visual-inspected`
- `automated-and-visual`
- `not-applicable`

`vendorStatus` describes only the installed-TIA comparison:

- `not-assessed`
- `source-only`
- `reference-capture-only`
- `matched`
- `not-required`

A passing local verifier must not automatically change `vendorStatus`.
`matched` requires a same-state installed-TIA comparison with its artifact
metadata recorded. RungProof-local keys use `not-required` and remain excluded
from Siemens parity claims.

Source `scope` is one of:

- `exact-instruction`: the URL identifies this exact instruction. Exact-
  instruction URLs must be unique across keys; a shared family page cannot be
  silently promoted to exact evidence.
- `exact-structure`: the URL defines the exact LAD structure rather than an
  executable instruction. This is limited to `branch` and `rung`.
- `family-reference`: the matrix has an official family page but no per-key
  instruction URL. The entry must retain an explicit provenance limitation.

An empty `source` array is an unresolved provenance gap. It is allowed only for
a deliberately enumerated Siemens-facing key and must be called out in that
entry's `limitations`. It is not equivalent to `source-only` evidence becoming
exact evidence.

## Artifact metadata

Each artifact contains:

- `id`: stable unique identifier used by gate `artifactIds`.
- `path`: relative file or external-manifest path; never an absolute path.
- `kind`: `image`, `log`, or `json`.
- `durability`: `ephemeral-local`, `checked-in`, or `external-immutable`.
- `sha256`: 64 hexadecimal characters when the artifact is immutable, otherwise
  `null`. A null hash is intentionally incomplete provenance, not durable proof.

The controller/editor test suite validates catalog equality, the enum values,
required fields, gate and artifact references, official source hosts for
Siemens-facing entries, and the actual SHA-256 hashes of referenced source-tree
artifacts. It deliberately does not require unavailable installed-TIA captures
or promote `source-only` to `matched`.

## Current local gate bindings

`tia-local-run-evidence.json` is the stable, source-tree summary of the latest
full local run. It binds the exact 50 Siemens-facing keys to all 200 local
renderer, edit, and monitor gates without treating one broad green run as evidence for unrelated
behavior:

- `offline`: `automated-and-visual`; all 50 keys were exercised unselected
  across 75/100/150 percent zoom and three automated display modes. The
  `SymbolicAndAbsolute`/`Show`/`OnOperands` set--150 details and 24 sheets--was
  retained, hash-verified, and human-reviewed;
- `selected`: `automated-and-visual`; the same automated sweep passed, and the
  selected `SymbolicAndAbsolute`/`Show`/`OnOperands` set was retained,
  hash-verified, and human-reviewed. The six CALL-affected artifacts changed by
  the shared layout correction were re-reviewed at all three zooms;
- `edit`: `automated-pass`; all 50 keys passed production workbench open,
  valid Apply, stable identity, compile, file-backed save/load, toolbar
  Undo/Redo, reopen, and cleanup checks. Forty-nine operand/form cases also
  rejected invalid input atomically; the network/rung structural insertion has
  no malformed operand to stage;
- `monitor`: `automated-pass`; all 50 keys passed false, true,
  earlier-cycle-unknown, and never-executed semantic projections (200 unique
  cases), but monitor pixels were not human-reviewed or compared with installed
  TIA Portal.

Every Siemens gate references the hashed local-run summary. Offline gates also
reference `tia-renderer-unselected-visual-review.json`, and selected gates also
reference `tia-renderer-visual-review.json`. Edit and monitor remain bound to
automated local-run evidence only. No Siemens gate points directly at `.tools`.
Vendor status remains independent: all 50 Siemens-facing instructions have
official source provenance and remain `source-only`; none is promoted to a
matched installed-TIA result.

## Renderer visual-review records

`tia-renderer-visual-review.json` and
`tia-renderer-unselected-visual-review.json` are source-tree human-review
verdicts for exact selected and unselected manifests. Each records its manifest
SHA-256 digest, the 50 reviewed Siemens-facing keys, all three zoom levels, 150
detail images, 24 contact sheets, and the offline evidence boundary. Generated
manifests remain `humanReviewStatus=pending`; generation cannot certify its own
pixels.

The manifests and PNG files live under ignored `.tools` storage and therefore
remain `ephemeral-local`. Once this source tree is placed under version control,
the verdict makes the scope and reviewed hash durable; it does not make the
pixels portable or prove an installed-TIA match. A later installed-TIA
comparison must use separately retained, immutable paired captures and update
vendor status independently.

## Editor-control registry

`editor-control-evidence-registry.json` separately inventories seven LAD editor
gates and fourteen project-tree gates outside the 50-key instruction catalog.
The declared `tia-editor-control-gate-sweep` passes 7/7 gates and 36/36
assertions; the current focused `tia-project-tree-gate-sweep` passes 14/14 gates
and 103/103 assertions.
The full-profile runner validates the exact gate-ID sets, required per-gate
identity fields, passing assertion arrays, and fixed `externalTia=false` and
`livePlc=false` boundaries before accepting either artifact. The latest
2026-10-20 interaction profile passed all 15 commands. Its project-tree and
editor-control artifacts are both current-source evidence; they remain
ephemeral local artifacts and do not establish installed-TIA parity. Vendor
status remains independent and requires an official source or matched
installed-TIA evidence as defined by each gate.
