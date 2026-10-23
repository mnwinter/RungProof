"""Bounded screen-only predicate observer. Root alone invokes live mode.

No simulator/state/memory APIs. A match does NOT prove ladder phase/class.
Live coordinates come from the freshly reviewed Engineering App Playback menu.
Every original, derivative crop, OCR result and input attempt is retained.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
from pathlib import Path
import queue
import re
import signal
import subprocess
import threading
import time
import urllib.request
from urllib.parse import urlparse
from PIL import Image

ROOT = Path(__file__).resolve().parent
DEADLINE = datetime(2026, 10, 23, 11, tzinfo=timezone.utc)
SIZE = (1021, 660)
IO_BOX = (700, 128, 1018, 445)
HEADER_BOX = (760, 0, 1021, 55)
MENU_BOX = (150, 45, 435, 135)
MENU_CLICK = (203, 34)
STOP_CLICK = (220, 90)

class ProbeError(RuntimeError):
    pass

def guard(now=None):
    if (now or datetime.now(timezone.utc)) >= DEADLINE:
        raise ProbeError('USER_CUTOFF_NO_NEW_UI_OR_OCR')

def canon(s):
    return ''.join(c for c in s.casefold() if c.isalnum())

def parse_rows(ocr):
    """Only exact independently read rows from this single OCR image count."""
    keys = ('sorted_count', 'infeed_run', 'primary_table_loaded', 'table_transfer_run', 'route_position')
    found = {k: [] for k in keys}
    for n, row in enumerate(ocr.get('lines', [])):
        raw = row.get('text', '')
        compact = canon(raw)
        for key in keys:
            prefix = canon(key)
            if not compact.startswith(prefix):
                continue
            value = compact[len(prefix):]
            if key == 'sorted_count' and value.isdigit():
                value = str(int(value))
            elif key == 'route_position' and value in ('o', '0', '00'):
                value = '0'  # OCR letter O only within the numeric value.
            elif value not in ('true', 'false'):
                continue
            found[key].append({'line': n, 'text': raw, 'value': value})
    values = {k: next(iter(v)) if len(v) == 1 else None
              for k in keys for v in [{r['value'] for r in found[k]}]}
    mandatory = (values['sorted_count'] == '1' and values['infeed_run'] == 'false'
                 and values['primary_table_loaded'] == 'false'
                 and values['table_transfer_run'] == 'true')
    return {'values': values, 'row_witnesses': found, 'mandatory_match': mandatory,
            'home_route': values['route_position'] == '0',
            'phase_class_proven': False}

def parse_scan(ocr):
    scans = [int(m) for line in ocr.get('lines', [])
             for m in re.findall(r'scan([0-9]+)(?![0-9])', canon(line.get('text', '')))]
    return max(scans) if scans else None

def safe_url(value):
    parsed = urlparse(value)
    if (parsed.scheme != 'http' or parsed.hostname != '127.0.0.1' or parsed.username
            or parsed.password or parsed.query or parsed.fragment or not parsed.path.startswith('/')
            or parsed.port is None or not 1 <= parsed.port <= 65535):
        raise ProbeError('SESSION_IS_NOT_APPROVED_LOOPBACK_CAPABILITY')
    return value.rstrip('/')

class Evidence:
    def __init__(self, prefix):
        if not re.fullmatch(r'[A-Za-z0-9_-]{1,60}', prefix):
            raise ProbeError('INVALID_EVIDENCE_PREFIX')
        self.folder = ROOT / (prefix + '-' + str(time.time_ns()))
        self.folder.mkdir(exist_ok=False)
        self.log = (self.folder / 'actions.jsonl').open('x', encoding='utf-8')
        self.files = []
    def event(self, kind, **values):
        self.log.write(json.dumps({'utc': datetime.now(timezone.utc).isoformat(), 'kind': kind, **values}) + '\n')
        self.log.flush()
    def save(self, name, raw):
        path = self.folder / name
        with path.open('xb') as handle:
            handle.write(raw)
        entry = {'file': name, 'bytes': len(raw), 'sha256': hashlib.sha256(raw).hexdigest()}
        self.files.append(entry)
        return path, entry
    def crop(self, path, box, label, scale=2):
        with Image.open(path) as source:
            if source.size != SIZE:
                raise ProbeError('NATIVE_CANVAS_NOT_1021x660')
            crop = source.crop(box).resize(((box[2]-box[0])*scale, (box[3]-box[1])*scale), Image.Resampling.LANCZOS)
            buffer = io.BytesIO(); crop.save(buffer, format='PNG')
        name = path.stem + '-' + label + '.png'
        target, entry = self.save(name, buffer.getvalue())
        self.event('DERIVED_CROP', source=path.name, crop=entry, native_box=list(box), scale=scale,
                   alteration='static crop and resample only; original remains intact')
        return target
    def finish(self, result):
        self.save('result.json', json.dumps({**result, 'files': self.files}, indent=2).encode())
        self.log.close()

class OcrWorker:
    def __init__(self, evidence, hard_end):
        self.evidence, self.hard_end = evidence, hard_end
        flags = getattr(subprocess, 'CREATE_NO_WINDOW', 0)
        self.process = subprocess.Popen(['powershell.exe', '-NoProfile', '-File', str(ROOT / 'ocr_worker.ps1')],
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8',
            errors='replace', bufsize=1, creationflags=flags)
        self.messages = queue.Queue()
        threading.Thread(target=self._read, args=(self.process.stdout,), daemon=True).start()
        threading.Thread(target=lambda: self.process.stderr.read(), daemon=True).start()
    def _read(self, stream):
        for line in stream:
            self.messages.put(line)
        self.messages.put(None)
    def read(self, image):
        guard()
        output = image.with_suffix('.ocr.json')
        if output.exists():
            raise ProbeError('OCR_OUTPUT_ALREADY_EXISTS')
        self.process.stdin.write(json.dumps({'image': str(image), 'output': str(output)}) + '\n')
        self.process.stdin.flush()
        remaining = min(6, self.hard_end-time.monotonic(), (DEADLINE-datetime.now(timezone.utc)).total_seconds())
        if remaining <= 0:
            raise ProbeError('OCR_BUDGET_EXPIRED')
        try:
            raw = self.messages.get(timeout=remaining)
        except queue.Empty:
            raise ProbeError('OCR_RESPONSE_TIMEOUT') from None
        if raw is None:
            raise ProbeError('OCR_WORKER_ENDED')
        try:
            result = json.loads(raw)
        except ValueError:
            raise ProbeError('OCR_RESPONSE_NOT_JSON') from None
        if 'error' in result or not output.is_file():
            raise ProbeError('OCR_REJECTED_IMAGE')
        if Path(result.get('image', '')).resolve() != image.resolve():
            raise ProbeError('OCR_RESPONSE_IMAGE_MISMATCH')
        data = output.read_bytes()
        self.evidence.files.append({'file': output.name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
        return result
    def close(self):
        try:
            self.process.stdin.close()
            self.process.wait(timeout=.5)
        except (OSError, subprocess.TimeoutExpired):
            self.process.terminate()
            try:
                self.process.wait(timeout=1)
            except subprocess.TimeoutExpired:
                self.process.kill(); self.process.wait(timeout=1)
        return self.process.returncode

class Bridge:
    def __init__(self, session, evidence, hard_end):
        self.url, self.pid = safe_url(session['url']), session['app_pid']
        if type(self.pid) is not int or self.pid <= 0:
            raise ProbeError('INVALID_PINNED_APP_PID')
        self.evidence, self.hard_end = evidence, hard_end
    def request(self, route, payload=None):
        guard()
        remaining = min(4, self.hard_end-time.monotonic(), (DEADLINE-datetime.now(timezone.utc)).total_seconds())
        if remaining <= 0:
            raise ProbeError('LOCAL_PROBE_BUDGET_EXPIRED')
        req = urllib.request.Request(self.url + route,
            data=None if payload is None else json.dumps(payload).encode(),
            headers={} if payload is None else {'Content-Type': 'application/json'})
        try:
            with urllib.request.urlopen(req, timeout=remaining) as response:
                return response.read()
        except Exception:
            # Exception strings/tracebacks may contain the private URL. Never emit them.
            raise ProbeError('BRIDGE_REQUEST_FAILED_NO_AUTOMATIC_INPUT_RETRY') from None
    def health(self):
        value = json.loads(self.request('/health'))
        if (value.get('version') != 11 or value.get('app_pid') != self.pid
                or value.get('native_client') != list(SIZE)
                or value.get('thread_dpi_awareness') != 2):
            raise ProbeError('PINNED_BRIDGE_PID_CANVAS_GUARD_FAILED')
    def capture(self, name):
        self.health(); guard()
        captured = time.monotonic()
        raw = self.request('/screen?t=' + str(time.time_ns()))
        with Image.open(io.BytesIO(raw)) as image:
            if image.size != SIZE:
                raise ProbeError('NATIVE_CANVAS_CHANGED')
        path, entry = self.evidence.save(name, raw)
        self.evidence.event('NATIVE_CAPTURE', image=entry, app_pid=self.pid, client=list(SIZE))
        return path, captured
    def click(self, xy, purpose, witness=None):
        self.health(); guard()
        self.evidence.event('OS_CLICK_ATTEMPT', purpose=purpose, x=xy[0], y=xy[1],
                            app_pid=self.pid, witness=witness, note='one request; never retry uncertain input')
        raw = self.request('/input', {'x': xy[0], 'y': xy[1]})
        self.evidence.event('OS_CLICK_DELIVERED', purpose=purpose, delivery_sha256=hashlib.sha256(raw).hexdigest())

def evaluate_image(evidence, worker, image, number):
    io_image = evidence.crop(image, IO_BOX, 'io2x')
    rows = parse_rows(worker.read(io_image))
    header = evidence.crop(image, HEADER_BOX, 'header3x', 3)
    scan = parse_scan(worker.read(header))
    evidence.event('SCREEN_OCR_OBSERVATION', image=image.name, observation_number=number, scan=scan, rows=rows)
    return rows, scan

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session', type=Path)
    mode.add_argument('--offline-image', type=Path)
    mode.add_argument('--self-test', action='store_true')
    parser.add_argument('--prefix', default='parcel-handoff-probe')
    parser.add_argument('--seconds', type=float, default=40)
    parser.add_argument('--start-x', type=int); parser.add_argument('--start-y', type=int)
    parser.add_argument('--require-route-home', action='store_true')
    args = parser.parse_args()
    guard()
    if not 1 <= args.seconds <= 40 or ((args.start_x is None) != (args.start_y is None)):
        raise ProbeError('INVALID_DURATION_OR_PARTIAL_START_COORDINATES')
    if args.start_x is not None and not (0 <= args.start_x < SIZE[0] and 0 <= args.start_y < SIZE[1]):
        raise ProbeError('START_COORDINATES_OUT_OF_NATIVE_CLIENT')
    if not args.session and args.start_x is not None:
        raise ProbeError('OFFLINE_MODE_MUST_NOT_CONTAIN_INPUT_COORDINATES')
    evidence = Evidence(args.prefix)
    result = {'status': 'UNOBSERVED', 'stop_attempted': False, 'phase_class_proven': False,
              'screen_only': True, 'native_session': bool(args.session)}
    worker = None
    began = time.monotonic(); poll_end = began + args.seconds; hard_end = poll_end + 4
    try:
        if args.self_test:
            base = ['sorted_count 1', 'infeed_run False', 'primary_table_loaded False', 'table_transfer_run True', 'route_position O']
            make = lambda rows: {'lines': [{'text': row} for row in rows]}
            assert parse_rows(make(base))['mandatory_match']
            assert parse_rows(make(base))['home_route']
            assert not parse_rows(make([base[0].replace(' 1', ' 10')] + base[1:]))['mandatory_match']
            assert not parse_rows(make(base[:-2]))['mandatory_match']
            assert not parse_rows(make(base + ['infeed_run True']))['mandatory_match']
            try: guard(DEADLINE)
            except ProbeError: pass
            else: raise AssertionError('Deadline guard did not reject cutoff')
            try: safe_url('https://example.invalid/private')
            except ProbeError: pass
            else: raise AssertionError('URL guard did not reject nonlocal transport')
            evidence.save('exclusive-write-test.txt', b'owned')
            try: evidence.save('exclusive-write-test.txt', b'overwrite')
            except FileExistsError: pass
            else: raise AssertionError('Evidence overwrite was allowed')
            result['status'] = 'OFFLINE_GUARDS_AND_PARSER_PASS'
            return
        worker = OcrWorker(evidence, hard_end)
        if args.offline_image:
            image = args.offline_image.resolve()
            if ROOT not in image.parents or not image.is_file():
                raise ProbeError('OFFLINE_IMAGE_MUST_BE_EXISTING_PNG_INSIDE_THIS_AUDIT')
            target, entry = evidence.save('offline-original.png', image.read_bytes())
            evidence.event('OFFLINE_ORIGINAL_COPY', source=image.name, image=entry, no_ui_endpoints=True)
            rows, scan = evaluate_image(evidence, worker, target, 0)
            result.update(status='OFFLINE_SAVED_IMAGE_OBSERVED', rows=rows, scan=scan)
            return
        session = json.loads(args.session.read_text(encoding='utf-8-sig'))
        bridge = Bridge(session, evidence, hard_end)
        bridge.capture('before-input.png')
        if args.start_x is not None:
            bridge.click((args.start_x, args.start_y), 'EXPLICIT_GROUNDED_PHYSICAL_START')
        bridge.click(MENU_CLICK, 'ARM_REVIEWED_APP_PLAYBACK_MENU')
        armed, captured = bridge.capture('menu-armed.png')
        menu_ocr = worker.read(evidence.crop(armed, MENU_BOX, 'menu2x'))
        menu_seen = any('stopscan' in canon(r.get('text', '')) or 'stopsimulatorscan' in canon(r.get('text', '')) for r in menu_ocr.get('lines', []))
        if not menu_seen:
            raise ProbeError('REVIEWED_STOP_MENU_TEXT_UNOBSERVED_NO_STOP_SENT')
        first_scan = None; progressed = False; observation = 0
        while time.monotonic() < poll_end:
            guard()
            image, captured = bridge.capture('observe-%03d.png' % observation)
            rows, scan = evaluate_image(evidence, worker, image, observation)
            if scan is not None:
                if first_scan is None: first_scan = scan
                elif scan > first_scan: progressed = True
            result.update(observations=observation+1, menu_seen=menu_seen, menu_open_scan_progress_observed=progressed,
                          last_scan=scan, last_rows=rows)
            matched = rows['mandatory_match'] and (not args.require_route_home or rows['home_route'])
            age = time.monotonic()-captured
            if matched and progressed and age <= 1.5:
                result['stop_attempted'] = True
                bridge.click(STOP_CLICK, 'SINGLE_STOP_ON_SIMULTANEOUS_SCREEN_ROWS',
                             {'image': image.name, 'age_seconds': age, 'rows': rows, 'scan': scan,
                              'phase_class_proven': False})
                immediate, _ = bridge.capture('stop-immediate.png')
                evaluate_image(evidence, worker, immediate, observation+1)
                guard(); time.sleep(min(1.5, max(0, hard_end-time.monotonic())))
                held, _ = bridge.capture('stop-held.png')
                evaluate_image(evidence, worker, held, observation+2)
                result.update(status='STOP_SENT_PUBLIC_ROWS_MATCHED_PHASE_CLASS_UNPROVEN', match_image=image.name,
                              require_route_home=args.require_route_home)
                return
            observation += 1
            time.sleep(min(.08, max(0, poll_end-time.monotonic())))
        result['status'] = 'PREDICATE_OR_MENU_SCAN_PROGRESS_UNOBSERVED_WITHIN_BUDGET'
    except BaseException as exc:
        result.update(status='PROBE_ENDED_WITHOUT_COMPLETION', error_code=str(exc) if isinstance(exc, ProbeError) else type(exc).__name__)
    finally:
        if worker is not None:
            result['owned_ocr_worker_returncode'] = worker.close()
        result['elapsed_seconds'] = round(time.monotonic()-began, 3)
        evidence.finish(result)
        print(json.dumps({'status': result['status'], 'stop_attempted': result['stop_attempted'],
                          'phase_class_proven': False, 'folder': str(evidence.folder),
                          'error_code': result.get('error_code'), 'elapsed_seconds': result['elapsed_seconds']}))

if __name__ == '__main__':
    def terminated(signum, frame):
        raise ProbeError('OWN_PROBE_TERMINATED')
    if hasattr(signal, 'SIGTERM'):
        signal.signal(signal.SIGTERM, terminated)
    main()
