"""Root-invoked Q08 native READY loss/restore; agent never invokes live mode.

No state APIs: public saved pixels/OCR plus at most one QUEUE A and two READY
clicks. CLI coordinates and Signals crop must be grounded by root first.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import time
import parcel_handoff_probe as base

KEYS = ('controller_ready', 'road_a_clear', 'road_b_clear', 'road_a_occupied',
        'road_b_occupied', 'crossing_clear', 'signal_conflict',
        'road_a_green', 'road_a_amber', 'road_a_red',
        'road_b_green', 'road_b_amber', 'road_b_red')
TRIGGER = {'controller_ready': True, 'road_a_clear': True, 'road_b_clear': True,
           'road_a_occupied': True, 'road_b_occupied': False,
           'crossing_clear': False, 'signal_conflict': False,
           'road_a_green': True, 'road_a_amber': False, 'road_a_red': False,
           'road_b_green': False, 'road_b_amber': False, 'road_b_red': True}


def parse_public(ocr):
    rows = []
    for w in sorted([w for line in ocr.get('lines', []) for w in line.get('words', [])],
                    key=lambda w: (w['y'] + w['h']/2, w['x'])):
        y = w['y'] + w['h']/2
        row = next((r for r in rows if abs(r[0]-y) <= 12), None)
        if row is None:
            row = [y, []]
            rows.append(row)
        row[1].append(w)
    found = {k: [] for k in KEYS}
    for y, words in rows:
        raw = ' '.join(w['text'] for w in sorted(words, key=lambda w: w['x']))
        text = base.canon(raw)
        for key in KEYS:
            name = base.canon(key)
            if text.startswith(name):
                value = {'true': True, 'false': False}.get(text[len(name):])
                if value is not None:
                    found[key].append({'raw': raw, 'value': value, 'y': y})
    values = {k: next(iter(s)) if len(s) == 1 else None
              for k, v in found.items() for s in [{x['value'] for x in v}]}
    return {'values': values, 'witnesses': found,
            'trigger': all(values[k] is v for k, v in TRIGGER.items())}


def observe(e, worker, image, number, box):
    header = worker.read(e.crop(image, base.HEADER_BOX, 'header3x', 3))
    h = base.canon(header.get('text', ''))
    rows = parse_public(worker.read(e.crop(image, box, 'signals3x', 3)))
    result = {'image': image.name, 'scan': base.parse_scan(header),
              'running': 'running' in h and 'stopped' not in h,
              'cycle20': 'cycle20' in h, **rows}
    e.event('ACTUAL_TRAFFIC_PUBLIC_PIXELS', number=number, **result)
    return result


def qualify_title(e, worker, image):
    title = worker.read(e.crop(image, (0, 0, 700, 65), 'title3x', 3))
    qualified = 'mobiletrafficlights' in base.canon(title.get('text', ''))
    e.event('ACTUAL_TRAFFIC_TITLE_GUARD', image=image.name, qualified=qualified)
    return qualified


def main():
    p = argparse.ArgumentParser(description=__doc__)
    mode = p.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session', type=Path)
    mode.add_argument('--offline-image', type=Path)
    mode.add_argument('--self-test', action='store_true')
    p.add_argument('--ready', nargs=2, type=int)
    p.add_argument('--queue-a', nargs=2, type=int)
    p.add_argument('--send-queue-a', action='store_true')
    p.add_argument('--signals-box', nargs=4, type=int, default=(700, 128, 1020, 440))
    p.add_argument('--prefix', default='traffic-ready-occupied')
    p.add_argument('--seconds', type=float, default=25)
    a = p.parse_args()
    if a.self_test:
        fake = {'lines': [{'words': [{'text': k+' '+str(v), 'x': 0, 'y': n*30, 'w': 100, 'h': 10}]} for n,(k,v) in enumerate(TRIGGER.items())]}
        assert parse_public(fake)['trigger']
        fake['lines'].pop()
        assert not parse_public(fake)['trigger']
        assert not parse_public({'lines': []})['trigger']
        print('PASS pure parser guards; no UI/copy/network')
        return 0
    base.guard()
    if not 5 <= a.seconds <= 25:
        raise base.ProbeError('TRAFFIC_DURATION_OUT_OF_BOUNDS')
    if a.session and (not a.ready or (a.send_queue_a and not a.queue_a)):
        raise base.ProbeError('ROOT_GROUNDED_COORDINATES_REQUIRED')
    for xy in [a.ready, a.queue_a]:
        if xy and not (0 <= xy[0] < 1021 and 0 <= xy[1] < 660):
            raise base.ProbeError('COORDINATE_OUTSIDE_NATIVE_CANVAS')
    box = tuple(a.signals_box)
    if not (0 <= box[0] < box[2] <= 1021 and 0 <= box[1] < box[3] <= 660):
        raise base.ProbeError('INVALID_SIGNALS_CROP')
    e = base.Evidence(a.prefix)
    hard_end = time.monotonic()+a.seconds
    worker = None
    result = {'status': 'PENDING', 'manual_physical_health_review_pending': True,
              'source_sha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              'input_budget': {'queue_a': int(a.send_queue_a), 'ready': 2}, 'observations': []}
    try:
        worker = base.OcrWorker(e, hard_end)
        if a.offline_image:
            source = a.offline_image.resolve()
            if base.ROOT not in source.parents:
                raise base.ProbeError('OFFLINE_IMAGE_OUTSIDE_AUDIT')
            image, _ = e.save('offline-original.png', source.read_bytes())
            result['title_qualified'] = qualify_title(e, worker, image)
            result['offline'] = observe(e, worker, image, 0, box)
            if not result['title_qualified']:
                raise base.ProbeError('OFFLINE_TRAFFIC_TITLE_NOT_QUALIFIED')
            result['status'] = 'OFFLINE_PIXELS_ONLY'
            return 0
        bridge = base.Bridge(json.loads(a.session.read_text(encoding='utf-8-sig')), e, hard_end)
        image, captured = bridge.capture('preflight.png')
        # Scene title and fixed layout are established from saved original pixels.
        result['title_qualified'] = qualify_title(e, worker, image)
        if not result['title_qualified']:
            raise base.ProbeError('TRAFFIC_NATIVE_TITLE_NOT_QUALIFIED_NO_INPUT')
        first = observe(e, worker, image, 0, box)
        if not first['running'] or not first['cycle20']:
            raise base.ProbeError('RUNNING_20MS_PREFLIGHT_REQUIRED_NO_INPUT')
        if a.send_queue_a:
            if not all(first['values'].get(k) is True for k in ('controller_ready','road_a_clear','road_b_clear','crossing_clear')):
                raise base.ProbeError('QUEUE_PREFLIGHT_PERMISSIVES_UNREADABLE_NO_INPUT')
            bridge.click(a.queue_a, 'ONE_ROOT_GROUNDED_QUEUE_A')
        number = 0
        while time.monotonic() < hard_end-5:
            base.guard()
            number += 1
            image, captured = bridge.capture('seek-%03d.png' % number)
            observed = observe(e, worker, image, number, box)
            result['observations'].append(observed)
            if not observed['running'] or not observed['cycle20']:
                raise base.ProbeError('RUNTIME_OR_CYCLE_GUARD_LOST_NO_FURTHER_INPUT')
            if not observed['trigger']:
                continue
            if time.monotonic()-captured > .65:
                e.event('STALE_TRIGGER_REJECTED', image=image.name)
                continue
            bridge.click(a.ready, 'READY_OFF_ON_FRESH_PUBLIC_OCCUPIED_A_GREEN', observed)
            off_at = time.monotonic()
            # No OCR between OFF and restoration: preserve actual immediate/held
            # pixels first, then restore promptly instead of consuming occupancy.
            try:
                immediate, _ = bridge.capture('ready-off-immediate.png')
                remaining = .27-(time.monotonic()-off_at)
                if remaining > 0:
                    time.sleep(remaining)
                settled, _ = bridge.capture('ready-off-settled.png')
            finally:
                # One restoration attempt even if a screenshot failed, provided
                # transport/deadline guards permit; never retry uncertain input.
                bridge.click(a.ready, 'READY_ON_PROMPT_RESTORE_NO_OCR_DELAY')
                result['off_to_restore_ack_wall_seconds'] = time.monotonic()-off_at
            restored, _ = bridge.capture('ready-on-immediate.png')
            for n, frame in enumerate((immediate, settled, restored)):
                result['observations'].append(observe(e, worker, frame, 100+n, box))
            n = 0
            while time.monotonic() < hard_end-2:
                n += 1
                frame, _ = bridge.capture('after-%03d.png' % n)
                result['observations'].append(observe(e, worker, frame, 200+n, box))
            result['status'] = 'CAPTURED_REQUIRES_POSTHOC_NATIVE_REVIEW'
            return 0
        result['status'] = 'NO_FRESH_QUALIFIED_OCCUPIED_TRIGGER_NO_READY_INPUT'
        return 2
    except Exception as exc:
        result['status'] = 'QA_PROBE_INCOMPLETE'
        result['error_code'] = str(exc) if isinstance(exc, base.ProbeError) else type(exc).__name__
        return 2
    finally:
        if worker:
            worker.close()
        e.finish(result)
        print(json.dumps({'status': result['status'], 'folder': str(e.folder)}))


if __name__ == '__main__':
    raise SystemExit(main())
