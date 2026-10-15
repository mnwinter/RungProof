# RungProof local MCP

`tools/rungproof_mcp.py` gives an AI agent a bounded local interface to the
RungProof simulator. It is deliberately **simulation-only**:

- There is no physical PLC client, endpoint, driver, address, or I/O tool.
- Ladder projects are written only under `artifacts/mcp-workspace/projects`.
- Runtime commands are named verification lanes, not arbitrary shell commands.
- Project validation compiles the Ladder program and validates only symbolic
  scene I/O ownership/type bindings.

## Codex setup

Add the server for the current Windows user:

```powershell
codex mcp add rungproof -- py "C:\Users\matt.winter\Documents\PLC Visual Simulator\rungproof-next\tools\rungproof_mcp.py"
codex mcp list
```

Restart or open a fresh Codex task after adding it so tool discovery refreshes.
The server locates its project root from its own path. Set `RUNGPROOF_ROOT` only
when deliberately running against a different checkout.

## Typical agent workflow

1. `rungproof_list_scenes` and `rungproof_get_scene` to inspect the declared
   symbolic contract.
2. `rungproof_write_project` to save authored editor JSON in the MCP workspace.
3. `rungproof_verify_integration` to run the real Ladder compiler,
   scene-binding validator, and selected scene's deterministic contract.
4. Use `rungproof_validate_project` or `rungproof_run_scene_contract` when
   isolating a failing side of that integration check.
5. `rungproof_run_checks` for build, virtual-controller, and UI regression lanes.
6. `rungproof_launch` when a human-visible RungProof window is required.

`rungproof_validate_project` needs either a valid `sourceSceneId` in the
project or an explicit `sceneId` argument. A successful result is evidence that
the logic is executable by the offline virtual controller and its declared
bindings agree with the scene; it is not evidence of a physical PLC download or
machine commissioning.

`rungproof_help` exposes bounded in-app engineering documentation to agents:
`ladder`, `schema`, `offline-safety`, `virtual-controller`, and `mcp`.
