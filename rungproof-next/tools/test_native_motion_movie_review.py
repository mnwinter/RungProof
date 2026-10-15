"""Serialized native capture smoke tests. Run only when the capture slot is free.

No production fixture/settings edits; every case gets a temporary directory.
This proves guard/load/trace behavior, not full-cycle or visual acceptance.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import json
import os
import re
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SCENE = "lab-10-03-vision-package-sorter"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--godot", type=Path)
    parser.add_argument("--project", type=Path, default=ROOT / ".tools/aaa-vision-sorter-native-qa.rpproj.json")
    parser.add_argument("--timeout", type=int, default=90)
    args = parser.parse_args()
    candidates = sorted((ROOT / ".tools/godot").glob("**/*console.exe"))
    godot = args.godot or (candidates[0] if len(candidates) == 1 else None)
    if godot is None or not godot.is_file():
        parser.error("Specify the existing Godot console executable with --godot")
    fixture = json.loads(args.project.read_text(encoding="utf-8-sig"))
    assert fixture["sourceSceneId"] == SCENE and fixture["scanPeriodMs"] == 20
    env = os.environ.copy()
    env["DOTNET_ROOT"] = str(ROOT / ".tools/dotnet")
    env["PATH"] = env["DOTNET_ROOT"] + os.pathsep + env.get("PATH", "")
    results = []
    with tempfile.TemporaryDirectory(prefix="native-motion-smoke-") as temp:
        base = Path(temp)

        def run(name, *, data=None, malformed=False, replace=None,
                visual=True, movie=True, native=True, expected=1,
                headless=False, extension="png", scene=SCENE, extra=()):
            folder = base / name
            folder.mkdir()
            project = folder / "project.rpproj.json"
            project.write_text("{bad json" if malformed else json.dumps(data or fixture), encoding="utf-8")
            trace = folder / "trace.jsonl"
            options = {"project": str(project), "angle": "FR", "route": "1", "trace": str(trace)}
            options.update(replace or {})
            user = ["--app-shell", f"--shell-scene={scene}", *extra]
            if visual:
                user.append("--visual-scene-review")
            if native:
                user.append("--native-motion-review")
                user += [f"--native-motion-{key}={value}" for key, value in options.items() if value is not None]
            command = [str(godot), "--path", str(ROOT), "--rendering-method", "gl_compatibility",
                       "--resolution", "1600x900", "--quit-after", "4"]
            if headless:
                command.append("--headless")
            if movie:
                command += ["--write-movie", str(folder / f"frame.{extension}"), "--fixed-fps", "50"]
            command += ["--", *user]
            completed = subprocess.run(command, cwd=ROOT, env=env, text=True,
                                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                       timeout=args.timeout, encoding="utf-8", errors="replace")
            output = completed.stdout
            if headless:
                assert completed.returncode != 0, f"{name}: unsupported renderer returned success"
                if completed.returncode != 1:
                    print(f"HEADLESS ENGINE LIMITATION: rejection followed by engine exit {completed.returncode}; not clean shutdown")
            else:
                assert completed.returncode == expected, f"{name}: exit {completed.returncode}, expected {expected}\n{output}"
            started = "NATIVE_MOTION_REVIEW_STARTED" in output
            records = [json.loads(line) for line in trace.read_text().splitlines()] if trace.exists() else []
            if expected == 1:
                assert not started, f"{name}: rejected case started capture"
                assert not any(row.get("kind") == "frame" for row in records)
                assert "NATIVE_MOTION_REVIEW_REJECTED" in output or "NATIVE_MOTION_REVIEW_FAILED" in output
            elif not native:
                assert not re.search(r"^ERROR:", output, re.MULTILINE), output
                assert "NATIVE_MOTION_REVIEW_" not in output and not trace.exists()
            else:
                assert not re.search(r"^ERROR:", output, re.MULTILINE), output
                assert started, output
                setup = next(row for row in records if row.get("kind") == "setup")
                frames = [row for row in records if row.get("kind") == "frame"]
                assert setup["transport"] == "none" and setup["scanPeriodMs"] == 20
                assert setup["projectSha256"].lower() == hashlib.sha256(project.read_bytes()).hexdigest()
                assert setup["clock"] == "ordinary PhysicsProcess/controller Advance"
                assert frames and frames[0]["reviewFrame"] == 0
                assert [row["reviewFrame"] for row in frames] == list(range(len(frames)))
                assert all(row["points"]["sort_complete"] is False for row in frames)
                assert all(row["points"]["vision_class"] == 1 for row in frames)
                assert frames[0]["timeSeconds"] == 0, "setup must precede controller advance"
                assert frames[0]["points"]["sort_conveyor_run"] is False
                assert frames[0]["points"]["diverter_enable"] is False
                assert any(row["timeSeconds"] > 0 for row in frames), "ordinary scans must advance"
                assert list(folder.glob("frame*.png")), "native MovieWriter must emit PNGs"
                assert not any(row.get("kind") == "end" and row.get("completed") for row in records), "four-frame smoke is not a complete cycle"
            results.append(name)

        run("missing_movie", movie=False)
        run("missing_visual_review", visual=False)
        run("missing_project", replace={"project": str(base / "absent.json")})
        run("missing_trace", replace={"trace": None})
        run("invalid_angle", replace={"angle": "Rear"})
        run("invalid_route", replace={"route": "5"})
        run("headless_movie", headless=True)
        run("avi_movie", extension="avi")
        run("unsupported_scene", scene="conveyor-cell")
        run("mcp_conflict", extra=(f"--mcp-project={args.project.resolve()}",))
        run("standalone_conflict", extra=(f"--scene-id={SCENE}",))
        mismatch = copy.deepcopy(fixture)
        mismatch["sourceSceneId"] = "conveyor-cell"
        run("mismatched_source", data=mismatch)
        run("malformed_project", malformed=True)
        invalid = copy.deepcopy(fixture)
        invalid["blocks"][0]["rungs"][0]["branches"][0]["contacts"][0]["variable"] = "UNDECLARED_CAPTURE_NEGATIVE"
        run("compile_invalid_project", data=invalid)
        run("ordinary_launch", native=False, visual=False, movie=False, expected=0)
        run("valid_native_movie_smoke", expected=0)
    print(f"NATIVE_MOTION_MOVIE_SMOKE PASS {len(results)} cases: " + ", ".join(results))


if __name__ == "__main__":
    main()
