"""Batch only real OS inputs and native pixels through the authorized bridge.

Session capabilities live in a private temporary file, never in this output
folder or the public action log. Coordinates must come from current pixels.
This transport has no simulator, controller, or saved-project state API.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
from pathlib import Path
import subprocess
import time
import urllib.request
from urllib.parse import urlparse

from PIL import Image

ROOT = Path(__file__).resolve().parent
DEADLINE = datetime(2026, 10, 23, 11, 0, tzinfo=timezone.utc)


def guard_deadline():
    if datetime.now(timezone.utc) >= DEADLINE:
        raise RuntimeError('User cutoff reached: no more project UI work')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('plan', type=Path)
    parser.add_argument('--session', type=Path, required=True)
    args = parser.parse_args()
    guard_deadline()
    session = json.loads(args.session.read_text(encoding='utf-8-sig'))
    url = session['url']
    parsed = urlparse(url)
    if parsed.scheme != 'http' or parsed.hostname != '127.0.0.1':
        raise ValueError('Only the approved local loopback transport is allowed')
    plan = json.loads(args.plan.read_text(encoding='utf-8-sig'))
    if not isinstance(plan, list) or not 1 <= len(plan) <= 40:
        raise ValueError('Plan must contain 1 to 40 serial observation/input steps')
    if sum(float(step.get('wait', 0.25)) for step in plan) > 40:
        raise ValueError('Bounded plans must leave time to observe and report progress')

    def health():
        guard_deadline()
        value = json.load(urllib.request.urlopen(url + '/health', timeout=10))
        if (value['version'] != 11 or value['app_pid'] != session['app_pid']
                or value['native_client'] != [1021, 660]):
            raise RuntimeError('Fresh pinned process/client guard failed; no input')
        return value

    def pixels():
        guard_deadline()
        raw = urllib.request.urlopen(url + '/screen?t=' + str(time.time_ns()), timeout=15).read()
        with Image.open(io.BytesIO(raw)) as shot:
            if shot.size != (1021, 660):
                raise RuntimeError('Native canvas changed; no further input')
        return raw

    health()
    pixels()  # Require an actual unoccluded capture before any input.
    for number, step in enumerate(plan):
        guard_deadline()
        health()
        event = {'utc': datetime.now(timezone.utc).isoformat(), 'plan': args.plan.name,
                 'step_number': number, 'step': step, 'app_pid': session['app_pid'],
                 'client': [1021, 660]}
        if 'input' in step:
            guard_deadline()
            request = urllib.request.Request(url + '/input', data=json.dumps(step['input']).encode(),
                                             headers={'Content-Type': 'application/json'})
            event['delivery'] = urllib.request.urlopen(request, timeout=15).read().decode()
        wait = float(step.get('wait', 0.25))
        if not 0 <= wait <= 5:
            raise ValueError('Each step wait must be between zero and five seconds')
        time.sleep(min(wait, max(0, (DEADLINE - datetime.now(timezone.utc)).total_seconds())))
        if 'capture' in step:
            name = step['capture']
            if not isinstance(name, str) or Path(name).name != name or not name:
                raise ValueError('Evidence must use a plain new filename')
            image_path = ROOT / (name + '.png')
            if image_path.exists():
                raise FileExistsError('Never overwrite original native evidence: ' + image_path.name)
            raw = pixels()
            image_path.write_bytes(raw)
            event.update({'image': image_path.name, 'sha256': hashlib.sha256(raw).hexdigest()})
            if step.get('ocr', False):
                guard_deadline()
                ocr_path = image_path.with_suffix('.ocr.json')
                subprocess.run(['powershell.exe', '-NoProfile', '-File',
                                str(ROOT.parent / 'screen-audit-2026-10-21/ocr_screenshot.ps1'),
                                '-ImagePath', str(image_path), '-OutputPath', str(ocr_path)],
                               check=True, stdout=subprocess.DEVNULL)
                event['ocr'] = ocr_path.name
        with (ROOT / 'native-ui-actions.jsonl').open('a', encoding='utf-8') as log:
            log.write(json.dumps(event) + '\n')
    print(json.dumps({'plan': args.plan.name, 'steps': len(plan),
                      'result': 'OS_INPUT_AND_ACTUAL_NATIVE_PIXELS_ONLY'}))


if __name__ == '__main__':
    main()
