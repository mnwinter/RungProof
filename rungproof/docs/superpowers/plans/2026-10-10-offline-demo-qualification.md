# Offline Demo Qualification Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to execute these gates directly. User explicitly requested adding the outstanding gates to the goal and continuing.

**Goal:** Qualify the exact offline candidate for demo release and repair concrete failures without conflating local evidence with external acceptance.

**Architecture:** Preserve the current package as an immutable baseline. Record qualification evidence separately; rebuild only when a verified product defect requires source changes. Keep offline transport prohibition and existing working behavior.

**Tech Stack:** Windows, Godot 4.7.2 .NET, .NET 10, PowerShell, C#.

**Spec:** ../../DEMO_RELEASE_REVIEW_2026-10-10.md and ../../../rungproof-next/docs/OFFLINE_DEMO_CANDIDATE.md

## Global Constraints

- Preserve all uncommitted work and generated UID files; no commit/push/publication without authorization.
- No real PLC connection, network writes, commissioning or physical-safety claims.
- No security-setting changes, new system accounts, virtualization installs or certificate purchases inferred from testing authorization.
- Visual acceptance requires actual rendered package evidence. Synthetic/model checks are labeled separately.

## Review Focus

- Imported corrupt/null/oversized documents must fail readably and preserve the current document.
- Interrupted/failed save must preserve an earlier valid file.
- Stop/Run must not restart machine motion without fresh Start; Reset restores initial plant/count.
- Missing runtime files, read-only installation and paths with spaces require explicit acceptance results.
- Licensing, dependency advisories and missing accessibility support must remain visible release gates.

## Tasks

- [ ] Inspect available clean-machine/standard-account environments; qualify only existing authorized facilities and document unavailable gates.
- [ ] Exact package native Demo 5: four-carton layer, Stop/Run/fresh Start recovery, Reset. Archive screenshots and log outcomes; investigate actual failures only.
- [ ] Persistence: exercise existing package guards plus malformed/missing file paths; inspect atomic save and practical import budgets. Add regression and smallest fix for reproduced defects, then rebuild/retest affected package behavior.
- [ ] Ordinary dependency inventory/advisory lookup and packaged asset/font rights inventory. The user stopped the blocked formal security workflow; do not invoke its tools, retry it or work around the restriction. Preserve security assessment as an unresolved external gate.
- [ ] Measure local process startup/memory/response and bounded soak; inspect alternate renderer/minimum-hardware evidence without pretending local RTX results qualify minimum hardware.
- [ ] Inspect keyboard navigation/readability in the rendered package. Record assistive-technology and DPI gates that require separate settings/environment.
- [ ] Prepare recipient acceptance, release notes, support/recovery and signing/distribution checklists with concrete missing owner/environment requirements.
- [ ] Update release assessment with PASS/PARTIAL/OPEN/BLOCKED evidence, package identity and changed-file scope. Keep goal active while required release gates remain.
