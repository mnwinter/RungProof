"""Serial native Windows movie captures. Completion is recording, not visual acceptance.

Uses the opt-in ordinary-controller review path and preserves all source movies.
The caller prepares .tools/native-movie-project: same project and resource junctions,
with only viewport/window dimensions changed to 2400x1350. No scene changes.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import os
import re
from pathlib import Path
import subprocess
import time
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ANGLES = ("FR", "FL", "RL", "RR", "Top")
SCENES = {"vision": "lab-10-03-vision-package-sorter", "robot": "lab-2-17-pallet-robot"}


def validate_sequence(directory):
    """Recording integrity only; every-frame image interpretation is separate."""
    records = [json.loads(line) for line in (directory / "trace.jsonl").read_text().splitlines()]
    frames = [row for row in records if row.get("kind") == "frame"]
    if not records or records[-1].get("completed") is not True:
        raise ValueError("Trace lacks successful completion")
    if [row["reviewFrame"] for row in frames] != list(range(len(frames))) or [row["renderedFrame"] for row in frames] != list(range(len(frames))):
        raise ValueError("Trace frame IDs are not consecutive from zero")
    files = sorted(directory.glob("frame*.png"))
    expected = [f"frame{number:08d}.png" for number in range(len(frames) + 1)]
    if [path.name for path in files] != expected:
        raise ValueError("PNG sequence must include every traced frame plus exactly one final rendered frame")
    log = (directory / "capture.log").read_text(errors="replace")
    if re.search(r"^ERROR:", log, re.MULTILINE):
        raise ValueError("Godot logged a runtime error")
    counts = re.findall(r"^(\d+) frames at 50 FPS", log, re.MULTILINE)
    if counts != [str(len(files))]:
        raise ValueError("MovieWriter frame count disagrees with source PNG sequence")
    identities = []
    for path in files:
        with Image.open(path) as picture:
            if picture.size != (2400, 1350) or picture.format != "PNG":
                raise ValueError(f"Unexpected frame format/dimensions: {path}")
            picture.verify()
        identities.append({"frame": path.name, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    integrity = {"status": "recording verified; every-frame visual review pending", "accepted": False,
                 "count": len(files), "traceCount": len(frames), "fps": 50, "dimensions": [2400, 1350],
                 "traceMapping": "renderedFrame N maps to frameNNNNNNNN.png; final PNG has no post-draw trace callback",
                 "finalUntracedFrame": files[-1].name, "frames": identities,
                 "traceSha256": hashlib.sha256((directory / "trace.jsonl").read_bytes()).hexdigest()}
    (directory / "sequence-integrity.json").write_text(json.dumps(integrity, indent=2))
    return integrity


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / ".tools/native-motion-png")
    parser.add_argument("--case", help="One case, e.g. vision-r1-FR; default records all 25 serially")
    parser.add_argument("--workers", type=int, choices=(1, 2), default=1)
    args = parser.parse_args()
    # Godot resolves movie paths against --path, whereas the coordinator is
    # launched from ROOT. Normalize before passing paths to either process.
    args.output = args.output.resolve()
    godot = ROOT / ".tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"
    project = ROOT / ".tools/native-movie-project"
    if not (project / "project.godot").is_file():
        raise SystemExit("Prepare the documented native-movie-project first.")
    env = dict(os.environ, DOTNET_ROOT=str(ROOT / ".tools/dotnet"))
    env["PATH"] = env["DOTNET_ROOT"] + os.pathsep + env.get("PATH", "")
    cases = [(scene, route, angle) for scene in SCENES for route in (range(1, 5) if scene == "vision" else (1,)) for angle in ANGLES]
    if args.case:
        cases = [case for case in cases if f"{case[0]}-r{case[1]}-{case[2]}" == args.case]
        if not cases:
            raise SystemExit("Unsupported case")
    args.output.mkdir(parents=True, exist_ok=True)
    def record_case(case_tuple):
        scene, route, angle = case_tuple
        case = f"{scene}-r{route}-{angle}"
        directory = args.output / case
        fixture = ROOT / "tools/native-motion-fixtures" / ("vision-sorter.rpproj.json" if scene == "vision" else "pallet-robot.rpproj.json")
        fixture_sha = hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
        source_sha = hashlib.sha256((ROOT / "src/App/Main.NativeMotionMovieReview.cs").read_bytes()).hexdigest()
        assembly_sha = hashlib.sha256((ROOT / ".godot/mono/temp/bin/Debug/RungProof.Next.dll").read_bytes()).hexdigest()
        project_sha = hashlib.sha256((project / "project.godot").read_bytes()).hexdigest()
        bundle = hashlib.sha256()
        for source in sorted((ROOT / "src").rglob("*.cs")):
            bundle.update(str(source.relative_to(ROOT)).encode())
            bundle.update(hashlib.sha256(source.read_bytes()).digest())
        bundle_sha = bundle.hexdigest()
        trace = directory / "trace.jsonl"
        if trace.exists():
            records = [json.loads(line) for line in trace.read_text().splitlines()]
            saved = json.loads((directory / "capture-manifest.json").read_text()) if (directory / "capture-manifest.json").exists() else {}
            if (records[0].get("projectSha256") == fixture_sha and records[0].get("scene") == SCENES[scene]
                and records[0].get("angle") == angle and records[0].get("route") == route
                and saved.get("reviewHelperSha256") == source_sha and saved.get("assemblySha256") == assembly_sha
                and saved.get("captureProjectSha256") == project_sha and saved.get("sourceBundleSha256") == bundle_sha):
                validate_sequence(directory)
                print(f"PRESERVED {case}: recorded, visual review pending", flush=True)
                return
            raise SystemExit(f"Incomplete existing capture {directory}; preserve it and use a fresh --output directory.")
        directory.mkdir(parents=True, exist_ok=False)
        command = [str(godot), "--path", str(project), "--rendering-method", "gl_compatibility", "--disable-vsync",
                   "--write-movie", str(directory / "frame.png"), "--fixed-fps", "50", "--", "--app-shell", "--visual-scene-review",
                   "--shell-scene=" + SCENES[scene], "--native-motion-review", "--native-motion-project=" + str(fixture),
                   "--native-motion-angle=" + angle, "--native-motion-route=" + str(route), "--native-motion-trace=" + str(trace)]
        metadata = {"case": case, "command": command, "projectSha256": fixture_sha,
                    "reviewHelperSha256": source_sha, "assemblySha256": assembly_sha,
                    "captureProjectSha256": project_sha, "sourceBundleSha256": bundle_sha,
                    "status": "recording"}
        (directory / "capture-manifest.json").write_text(json.dumps(metadata, indent=2))
        print(f"START {case}", flush=True)
        started = time.monotonic()
        with (directory / "capture.log").open("w") as log:
            result = subprocess.run(command, cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=240)
        records = [json.loads(line) for line in trace.read_text().splitlines()] if trace.exists() else []
        completed = result.returncode == 0 and bool(records) and records[-1].get("completed") is True
        if completed:
            try:
                validate_sequence(directory)
            except ValueError as error:
                completed = False
                metadata["integrityFailure"] = str(error)
        metadata.update(status="recorded; every-frame visual review pending" if completed else "failed",
                        exitCode=result.returncode, elapsedSeconds=round(time.monotonic()-started, 2))
        (directory / "capture-manifest.json").write_text(json.dumps(metadata, indent=2))
        print(f"{'RECORDED' if completed else 'FAILED'} {case}: {metadata['elapsedSeconds']} seconds", flush=True)
        if not completed:
            raise SystemExit(f"Capture failed; inspect {directory / 'capture.log'}")

    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        # Submit only a bounded pair. A failed batch prevents later captures.
        for start in range(0, len(cases), args.workers):
            jobs = [pool.submit(record_case, case) for case in cases[start:start + args.workers]]
            for job in jobs:
                job.result()


if __name__ == "__main__":
    main()
