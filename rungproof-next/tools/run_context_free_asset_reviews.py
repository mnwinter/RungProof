"""Run one isolated Codex vision-review process per unreviewed candidate image."""
from __future__ import annotations

import argparse
import concurrent.futures
import json
import shutil
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "assets" / "catalog" / "candidates.catalog.json"
OUTPUT = ROOT / "build" / "blind-review-assets"
PROMPT = (
    "Inspect the attached image without using its filename or any outside project context. "
    "Identify the industrial object or equipment family shown. Be specific enough to distinguish "
    "it from adjacent catalog families. Return exactly three lines:\n"
    "Identification: <plain-language identity>\n"
    "Confidence: <number from 0.00 to 1.00>\n"
    "Concerns: <visible ambiguity, geometry, proportion, or realism concerns, or none>"
)


def review(item: tuple[int, dict], force: bool, suffix: str, model: str, effort: str) -> tuple[int, str, int]:
    index, asset = item
    source = ROOT / asset["model"]["thumbnailFile"].removeprefix("res://")
    fresh = OUTPUT / f"fresh_{index:03d}_{suffix}"
    fresh.mkdir(parents=True, exist_ok=True)
    image = fresh / "image.png"
    shutil.copy2(source, image)
    result = OUTPUT / f"image_{index:03d}.{suffix}.review.md"
    log = OUTPUT / f"image_{index:03d}.{suffix}.agent.log"
    if result.exists() and not force:
        return index, asset["id"], 0
    command = [
        "codex", "exec", "--ephemeral", "-s", "read-only", "-C", str(fresh),
        "-m", model, "-c", f"model_reasoning_effort={effort}",
        "-i", str(image), "-o", str(result), PROMPT,
    ]
    completed = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log.write_text(completed.stdout + "\n--- STDERR ---\n" + completed.stderr, encoding="utf-8")
    return index, asset["id"], completed.returncode


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--start", type=int, default=26)
    parser.add_argument("--end", type=int, default=53)
    parser.add_argument("--jobs", type=int, default=7)
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--indices", help="Comma-separated one-based catalog indices; overrides start/end.")
    parser.add_argument("--suffix", default="factory")
    parser.add_argument("--model", default="gpt-5.6-luna")
    parser.add_argument("--effort", default="low")
    args = parser.parse_args()
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    indices = ([int(value.strip()) for value in args.indices.split(",") if value.strip()]
               if args.indices else list(range(args.start, min(args.end, len(assets)) + 1)))
    selected = [(i, assets[i - 1]) for i in indices]
    failures = 0
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.jobs) as pool:
        futures = [pool.submit(review, item, args.force, args.suffix, args.model, args.effort) for item in selected]
        for future in concurrent.futures.as_completed(futures):
            index, asset_id, code = future.result()
            print(f"REVIEW {index:03d} {code} {asset_id}", flush=True)
            failures += code != 0
    print("REVIEW_PROCESSES", len(selected))
    print("REVIEW_PROCESS_FAILURES", failures)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
