#!/usr/bin/env python3
"""Local, simulation-only MCP server for the RungProof workspace.

This server intentionally exposes a small, named tool surface instead of a
shell.  Every project write is confined to artifacts/mcp-workspace and every
runtime command is a predefined offline verification lane.  No tool creates,
configures, or reaches a physical PLC transport.
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(os.environ.get("RUNGPROOF_ROOT", Path(__file__).resolve().parents[1])).resolve()
WORKSPACE = ROOT / "artifacts" / "mcp-workspace"
PROJECTS = WORKSPACE / "projects"
MAX_PROJECT_BYTES = 1_000_000
SAFE_NAME = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_.-]{0,79}$")


def godot_exe() -> Path:
    candidates = [
        candidate for candidate in (ROOT / ".tools" / "godot").glob("**/Godot*.exe")
        if not candidate.stem.endswith("_console")
    ]
    if len(candidates) != 1:
        raise RuntimeError("Pinned Godot executable is unavailable or ambiguous under .tools/godot.")
    return candidates[0]


def dotnet_exe() -> Path:
    candidate = ROOT / ".tools" / "dotnet" / "dotnet.exe"
    if not candidate.is_file():
        raise RuntimeError("Pinned .NET SDK is unavailable under .tools/dotnet.")
    return candidate


def tool_result(value: Any, is_error: bool = False) -> dict[str, Any]:
    return {
        "content": [{"type": "text", "text": json.dumps(value, indent=2, sort_keys=True)}],
        "isError": is_error,
    }


def safe_project_path(name: str) -> Path:
    if not isinstance(name, str) or not SAFE_NAME.fullmatch(name):
        raise ValueError("Project name must be 1-80 characters of letters, numbers, dot, underscore, or hyphen.")
    PROJECTS.mkdir(parents=True, exist_ok=True)
    candidate = (PROJECTS / f"{name}.rpproj.json").resolve()
    if candidate.parent != PROJECTS.resolve():
        raise ValueError("Project path escapes the MCP workspace.")
    return candidate


def catalog() -> dict[str, Any]:
    path = ROOT / "scenes" / "catalog" / "original-scenes.catalog.json"
    return json.loads(path.read_text(encoding="utf-8"))


def scene_entry(scene_id: str) -> dict[str, Any]:
    for entry in catalog()["scenes"]:
        if entry["id"] == scene_id:
            return entry
    raise ValueError(f"Unknown migrated scene '{scene_id}'.")


def scene_document(scene_id: str) -> dict[str, Any]:
    entry = scene_entry(scene_id)
    path = (ROOT / entry["path"].removeprefix("res://")).resolve()
    if ROOT not in path.parents:
        raise RuntimeError("Catalog scene path escapes the RungProof root.")
    return json.loads(path.read_text(encoding="utf-8"))


def run(command: list[str], timeout: int = 180) -> dict[str, Any]:
    completed = subprocess.run(
        command, cwd=ROOT, text=True, capture_output=True, timeout=timeout, check=False
    )
    return {
        "command": command,
        "exitCode": completed.returncode,
        "passed": completed.returncode == 0,
        "stdout": completed.stdout[-16000:],
        "stderr": completed.stderr[-16000:],
    }


def build_application() -> dict[str, Any]:
    """Ensure Godot loads the current C# source, never a stale assembly."""
    return run([str(dotnet_exe()), "build", "RungProof.Next.csproj", "--no-restore"])


def project_summary(document: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": document.get("id"),
        "name": document.get("name"),
        "sourceSceneId": document.get("sourceSceneId"),
        "tagCount": len(document.get("tags", [])),
        "blockCount": len(document.get("blocks", [])),
        "taskCount": len(document.get("tasks", [])),
    }


def call_tool(name: str, args: dict[str, Any]) -> dict[str, Any]:
    if name == "rungproof_status":
        data = catalog()
        return tool_result({
            "root": str(ROOT), "sceneCount": data["sceneCount"],
            "mcpWorkspace": str(WORKSPACE),
            "offlineSafety": "symbolic virtual controller only; no physical PLC transport is exposed",
            "availableChecks": ["build", "virtual-controller", "app-shell", "ladder-editor", "hud", "ui-density"],
        })
    if name == "rungproof_list_scenes":
        query = str(args.get("query", "")).casefold()
        chapter = str(args.get("chapter", "")).casefold()
        scenes = []
        for entry in catalog()["scenes"]:
            haystack = f"{entry['id']} {entry['name']}".casefold()
            if query and query not in haystack:
                continue
            if chapter and not entry["id"].casefold().startswith(chapter):
                continue
            scenes.append({key: entry[key] for key in ("id", "name", "equipmentCount", "equipmentTypes", "visualStatus")})
        return tool_result({"count": len(scenes), "scenes": scenes})
    if name == "rungproof_get_scene":
        scene = scene_document(str(args["sceneId"]))
        points = scene.get("simulation", {}).get("points", [])
        actions = scene.get("simulation", {}).get("actions", [])
        return tool_result({
            "id": scene["id"], "name": scene["name"], "description": scene.get("description", ""),
            "equipment": [{"id": item["id"], "type": item["type"], "label": item["label"]} for item in scene["equipment"]],
            "points": points, "actions": actions, "verification": scene.get("verification", {}),
        })
    if name == "rungproof_help":
        topics = {
            "ladder": "LADDER_INSTRUCTION_HELP.md",
            "schema": "VIRTUAL_CONTROLLER_PROGRAM_SCHEMA.md",
            "offline-safety": "OFFLINE_LADDER_ACCEPTANCE.md",
            "virtual-controller": "VIRTUAL_CONTROLLER_CONFORMANCE.md",
            "mcp": "RUNGPROOF_MCP.md",
        }
        topic = str(args.get("topic", "ladder"))
        if topic not in topics:
            raise ValueError(f"Unknown help topic '{topic}'. Available: {', '.join(topics)}.")
        text = (ROOT / "docs" / topics[topic]).read_text(encoding="utf-8")
        return tool_result({"topic": topic, "document": topics[topic], "markdown": text[:24000]})
    if name == "rungproof_write_project":
        project = args.get("project")
        if not isinstance(project, dict):
            raise ValueError("project must be a Ladder editor JSON object.")
        encoded = json.dumps(project, indent=2, ensure_ascii=False) + "\n"
        if len(encoded.encode("utf-8")) > MAX_PROJECT_BYTES:
            raise ValueError("Project exceeds the 1 MiB MCP workspace limit.")
        path = safe_project_path(str(args["name"]))
        path.write_text(encoded, encoding="utf-8")
        return tool_result({"written": True, "project": path.name, "summary": project_summary(project)})
    if name == "rungproof_read_project":
        path = safe_project_path(str(args["name"]))
        if not path.is_file():
            raise ValueError(f"MCP project '{path.name}' does not exist.")
        document = json.loads(path.read_text(encoding="utf-8"))
        return tool_result({"project": path.name, "summary": project_summary(document), "document": document})
    if name == "rungproof_validate_project":
        path = safe_project_path(str(args["name"]))
        if not path.is_file():
            raise ValueError(f"MCP project '{path.name}' does not exist.")
        scene_id = args.get("sceneId")
        build = build_application()
        if not build["passed"]:
            return tool_result({"passed": False, "build": build, "validation": None})
        command = [str(godot_exe()), "--headless", "--path", str(ROOT), "--", f"--mcp-project={path}"]
        if scene_id:
            command.append(f"--mcp-scene={scene_id}")
        validation = run(command)
        return tool_result({"passed": validation["passed"], "build": build, "validation": validation})
    if name == "rungproof_run_scene_contract":
        scene_id = str(args["sceneId"])
        scene_entry(scene_id)
        return tool_result(run([
            str(godot_exe()), "--headless", "--path", str(ROOT), "--",
            f"--scene-id={scene_id}", "--verify-scene-contract",
        ]))
    if name == "rungproof_verify_integration":
        path = safe_project_path(str(args["name"]))
        if not path.is_file():
            raise ValueError(f"MCP project '{path.name}' does not exist.")
        scene_id = str(args["sceneId"])
        scene_entry(scene_id)
        build = build_application()
        if not build["passed"]:
            return tool_result({"passed": False, "build": build, "projectValidation": None, "sceneContract": None})
        validation = run([
            str(godot_exe()), "--headless", "--path", str(ROOT), "--",
            f"--mcp-project={path}", f"--mcp-scene={scene_id}",
        ])
        scene_contract = run([
            str(godot_exe()), "--headless", "--path", str(ROOT), "--",
            f"--scene-id={scene_id}", "--verify-scene-contract",
        ])
        return tool_result({
            "passed": build["passed"] and validation["passed"] and scene_contract["passed"],
            "build": build,
            "projectValidation": validation,
            "sceneContract": scene_contract,
            "scope": "offline Ladder compiler + symbolic scene binding + deterministic scene contract",
        })
    if name == "rungproof_run_checks":
        requested = args.get("checks", [])
        if not isinstance(requested, list) or not requested:
            raise ValueError("checks must be a non-empty list of named verification lanes.")
        lanes = {
            "build": [str(dotnet_exe()), "build", "RungProof.Next.csproj", "--no-restore"],
            "virtual-controller": [str(dotnet_exe()), "run", "--project", "tests/RungProof.Next.VirtualController.Tests.csproj"],
            "app-shell": [str(godot_exe()), "--headless", "--path", str(ROOT), "--", "--verify-app-shell"],
            "ladder-editor": [str(godot_exe()), "--headless", "--path", str(ROOT), "--", "--verify-ladder-editor"],
            "hud": [str(godot_exe()), "--headless", "--path", str(ROOT), "--", "--verify-hud"],
            "ui-density": [str(godot_exe()), "--headless", "--path", str(ROOT), "--", "--verify-ui-density"],
        }
        unknown = [lane for lane in requested if lane not in lanes]
        if unknown:
            raise ValueError(f"Unknown check lane(s): {', '.join(map(str, unknown))}.")
        results = {lane: run(lanes[lane]) for lane in requested}
        return tool_result({"passed": all(item["passed"] for item in results.values()), "results": results})
    if name == "rungproof_launch":
        executable = godot_exe()
        subprocess.Popen([str(executable), "--path", str(ROOT)], cwd=ROOT, creationflags=getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0))
        return tool_result({"launched": True, "mode": "local offline RungProof UI", "physicalPlcTransport": "unavailable"})
    raise ValueError(f"Unknown tool '{name}'.")


TOOLS = [
    {"name": "rungproof_status", "description": "Inspect the local RungProof MCP boundary and offline safety contract.", "inputSchema": {"type": "object", "properties": {}}},
    {"name": "rungproof_list_scenes", "description": "List migrated simulator scenes by optional text or chapter-prefix filter.", "inputSchema": {"type": "object", "properties": {"query": {"type": "string"}, "chapter": {"type": "string"}}}},
    {"name": "rungproof_get_scene", "description": "Read a scene's symbolic I/O contract, actions, equipment, and verification cases.", "inputSchema": {"type": "object", "properties": {"sceneId": {"type": "string"}}, "required": ["sceneId"]}},
    {"name": "rungproof_help", "description": "Read bounded RungProof engineering help: ladder, schema, offline-safety, virtual-controller, or mcp.", "inputSchema": {"type": "object", "properties": {"topic": {"type": "string"}}}},
    {"name": "rungproof_write_project", "description": "Write an offline Ladder editor project only inside artifacts/mcp-workspace/projects.", "inputSchema": {"type": "object", "properties": {"name": {"type": "string"}, "project": {"type": "object"}}, "required": ["name", "project"]}},
    {"name": "rungproof_read_project", "description": "Read a Ladder project from the MCP workspace.", "inputSchema": {"type": "object", "properties": {"name": {"type": "string"}}, "required": ["name"]}},
    {"name": "rungproof_validate_project", "description": "Compile a Ladder editor project and validate its symbolic bindings against a scene; never opens PLC transport.", "inputSchema": {"type": "object", "properties": {"name": {"type": "string"}, "sceneId": {"type": "string"}}, "required": ["name"]}},
    {"name": "rungproof_run_scene_contract", "description": "Run a scene's declared deterministic simulation contract headlessly.", "inputSchema": {"type": "object", "properties": {"sceneId": {"type": "string"}}, "required": ["sceneId"]}},
    {"name": "rungproof_verify_integration", "description": "Verify a saved Ladder project against a named scene, then execute that scene's deterministic contract.", "inputSchema": {"type": "object", "properties": {"name": {"type": "string"}, "sceneId": {"type": "string"}}, "required": ["name", "sceneId"]}},
    {"name": "rungproof_run_checks", "description": "Run selected named offline checks. Arbitrary shell commands are intentionally unavailable.", "inputSchema": {"type": "object", "properties": {"checks": {"type": "array", "items": {"type": "string"}}}, "required": ["checks"]}},
    {"name": "rungproof_launch", "description": "Launch the local RungProof UI in offline simulator mode.", "inputSchema": {"type": "object", "properties": {}}},
]


def respond(request: dict[str, Any]) -> None:
    request_id = request.get("id")
    method = request.get("method")
    if method == "notifications/initialized":
        return
    if method == "initialize":
        result: Any = {"protocolVersion": "2025-03-26", "serverInfo": {"name": "rungproof-local", "version": "1.0.0"}, "capabilities": {"tools": {}}}
    elif method == "tools/list":
        result = {"tools": TOOLS}
    elif method == "tools/call":
        try:
            result = call_tool(request["params"]["name"], request["params"].get("arguments", {}))
        except (KeyError, ValueError, RuntimeError, json.JSONDecodeError, subprocess.TimeoutExpired) as error:
            result = tool_result({"error": str(error)}, True)
    elif method == "ping":
        result = {}
    else:
        result = {"code": -32601, "message": f"Method '{method}' not found."}
        if request_id is not None:
            sys.stdout.write(json.dumps({"jsonrpc": "2.0", "id": request_id, "error": result}) + "\n")
            sys.stdout.flush()
        return
    if request_id is not None:
        sys.stdout.write(json.dumps({"jsonrpc": "2.0", "id": request_id, "result": result}) + "\n")
        sys.stdout.flush()


def main() -> None:
    for line in sys.stdin:
        try:
            respond(json.loads(line))
        except json.JSONDecodeError as error:
            sys.stdout.write(json.dumps({"jsonrpc": "2.0", "id": None, "error": {"code": -32700, "message": str(error)}}) + "\n")
            sys.stdout.flush()


if __name__ == "__main__":
    main()
