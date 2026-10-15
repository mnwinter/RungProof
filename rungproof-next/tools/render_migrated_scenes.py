"""Render every migrated scene through the Godot runtime for visual QA."""
from __future__ import annotations

import argparse
import json
import os
import subprocess
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "scenes" / "catalog" / "original-scenes.catalog.json"


def render(godot: Path, scene_id: str, state: str, output: Path) -> dict[str, object]:
    image = output / f"{scene_id}.png"
    completed = subprocess.run(
        [str(godot), "--path", str(ROOT), "--", f"--scene-id={scene_id}",
         f"--capture={image}", f"--capture-state={state}"],
        cwd=ROOT, env=os.environ.copy(), text=True, encoding="utf-8", errors="replace",
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=90, check=False,
    )
    log = output / f"{scene_id}.log"
    log.write_text(completed.stdout, encoding="utf-8")
    passed = completed.returncode == 0 and image.exists() and "deferred=0" in completed.stdout
    return {"sceneId": scene_id, "passed": passed, "exitCode": completed.returncode,
            "image": str(image.relative_to(ROOT)).replace("\\", "/"),
            "log": str(log.relative_to(ROOT)).replace("\\", "/")}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--godot", type=Path, required=True)
    parser.add_argument("--state", choices=("stopped", "running"), default="stopped")
    parser.add_argument("--workers", type=int, default=4)
    parser.add_argument("--output", type=Path, default=ROOT / "build" / "migrated-scenes-latest")
    args = parser.parse_args()
    output = args.output.resolve(); output.mkdir(parents=True, exist_ok=True)
    scene_ids = [item["id"] for item in json.loads(CATALOG.read_text(encoding="utf-8"))["scenes"]]
    results: list[dict[str, object]] = []
    with ThreadPoolExecutor(max_workers=max(1, args.workers)) as executor:
        futures = {executor.submit(render, args.godot.resolve(), scene_id, args.state, output): scene_id for scene_id in scene_ids}
        for future in as_completed(futures):
            result = future.result(); results.append(result)
            print(f"SCENE_RENDER {result['sceneId']} {'PASS' if result['passed'] else 'FAIL'}")
    results.sort(key=lambda item: str(item["sceneId"]))
    passed = sum(bool(item["passed"]) for item in results)
    (output / "summary.json").write_text(json.dumps({"state": args.state, "sceneCount": len(results),
        "passed": passed, "failed": len(results)-passed, "results": results}, indent=2)+"\n", encoding="utf-8")
    print(f"SCENE_RENDERS_PASS {passed}"); print(f"SCENE_RENDERS_FAIL {len(results)-passed}")
    return 0 if passed == len(results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
