"""Root-invoked Q21 screen-only active-WALK CROSS and bounded idle observer.

Root supplies explicit grounded CROSS coordinate and performs the FIRST idle
CROSS immediately before invoking. The helper sends at most one further CROSS
while exact current public WALK qualifies. No setup, menus, Health, Run or Reset.
Physical lamp and fresh Health-event acknowledgement remain root review.
"""
import argparse
import hashlib
import json
from pathlib import Path
import signal
import time
import parcel_handoff_probe as base

EXPECTED = {
    'clear_to_finish': True,
    'pedestrian_walk': True,
    'pedestrian_stop': False,
    'vehicle_ready': False,
    'vehicle_amber': False,
    'vehicle_stop': True,
}
KEYS = tuple(EXPECTED) + ('crossing_request', 'sequence_running')
NATIVE_LAYOUT_QUALIFIED = True  # Current saved baseline qualified offline; camera/layout must remain fixed.
IDLE = {'clear_to_finish': True, 'pedestrian_walk': False,
        'pedestrian_stop': True, 'vehicle_ready': True,
        'vehicle_amber': False, 'vehicle_stop': False}
ACCEPTED_SCAN_MS = 20  # Frozen current fixture, line 7; not measured wall time.
REQUIRED_IDLE_SCANS = 400
MAX_GAP_EXCLUSIVE = 40


def parse_exact_bool_rows(ocr, row_tolerance):
    """Join only same-height words, then accept exact field name and BOOL suffix.

    Tolerance must be grounded for the actual static crop scale. Unknown,
    conflicting, or missing fields remain None. No semantic/OCR aliases.
    """
    rows = []
    words = [word for line in ocr.get('lines', []) for word in line.get('words', [])]
    for word in sorted(words, key=lambda w: (w['y'] + w['h']/2, w['x'])):
        center = word['y'] + word['h']/2
        row = next((r for r in rows if abs(r['anchor_y'] - center) <= row_tolerance), None)
        if row is None:
            row = {'anchor_y': center, 'words': []}
            rows.append(row)
        row['words'].append(word)
    found = {key: [] for key in KEYS}
    for row in rows:
        raw = ' '.join(word['text'] for word in sorted(row['words'], key=lambda w: w['x']))
        text = base.canon(raw)
        for key in KEYS:
            name = base.canon(key)
            if text.startswith(name):
                value = {'true': True, 'false': False}.get(text[len(name):])
                if value is not None:
                    found[key].append({'raw': raw, 'value': value, 'row_y': row['anchor_y']})
    values = {key: next(iter(s)) if len(s) == 1 else None
              for key, items in found.items() for s in [{item['value'] for item in items}]}
    return {'values': values, 'row_witnesses': found,
            'public_walk_predicate': all(values[key] is value for key, value in EXPECTED.items())}


def require_native_qualification():
    if not NATIVE_LAYOUT_QUALIFIED:
        raise base.ProbeError('CURRENT_NATIVE_LAYOUT_AND_CONTROL_QUALIFICATION_PENDING_NO_INPUT')


class IdleCoverage:
    """Passive observation bookkeeping only; never requests state or UI input.

    Input records come from actual full native images in live mode.
    Synthetic records exercise bookkeeping only. Once coverage is broken
    it is not silently restarted, and any readable non-idle field is surfaced.
    """
    def __init__(self):
        self.first_idle_scan = None
        self.last_scan = None
        self.last_complete_scan = None
        self.last_complete_wall = None
        self.max_scan_gap = 0
        self.max_wall_gap = 0
        self.complete_samples = 0
        self.parse_misses = []
        self.uncovered_observations = []
        self.active_after_idle = []
        self.invalid_reasons = []
        self.clearance_scan = None
        self.possible_replay_after_clearance = []
        self.post_last_scan = None
        self.post_last_complete_scan = None
        self.post_max_scan_gap = 0
        self.post_parse_misses = []

    def add(self, observation):
        values = observation['values']
        scan = observation.get('scan')
        scan_valid = type(scan) is int and scan >= 0
        running = observation.get('runtime_running') is True
        complete = all(type(values.get(key)) is bool for key in IDLE)
        idle = complete and all(values[key] is expected for key, expected in IDLE.items()) and values.get('sequence_running') is not True
        # Track the full post-click path, not just the portion after sampled
        # idle. A queued cycle could pass through one unsampled idle scan.
        clearance = (complete and values['clear_to_finish'] is True
            and values['pedestrian_walk'] is False and values['pedestrian_stop'] is True
            and values['vehicle_stop'] is True and values['vehicle_amber'] is False
            and values['vehicle_ready'] is False)
        if self.clearance_scan is None and scan_valid and running and clearance:
            self.clearance_scan = scan
        if self.clearance_scan is not None:
            replay_fields = {key: True for key in ('vehicle_amber','pedestrian_walk') if values.get(key) is True}
            if replay_fields:
                self.possible_replay_after_clearance.append({'image':observation['image'],'scan':scan,'fields':replay_fields})
        if not running:
            self.invalid_reasons.append('POST_CLICK_RUNTIME_NOT_RUNNING')
        if scan_valid and self.post_last_scan is not None and scan < self.post_last_scan:
            self.invalid_reasons.append('POST_CLICK_NON_MONOTONIC_SCAN')
        if scan_valid:
            self.post_last_scan = scan
            if self.post_last_complete_scan is not None:
                gap = scan-self.post_last_complete_scan
                if gap >= MAX_GAP_EXCLUSIVE:
                    self.invalid_reasons.append('POST_CLICK_TRANSITION_SAMPLE_GAP_TOO_LARGE')
                if complete and running:
                    self.post_max_scan_gap = max(self.post_max_scan_gap,gap)
        if complete and running and scan_valid:
            self.post_last_complete_scan = scan
        else:
            self.post_parse_misses.append({'image':observation['image'],'scan':scan,
                'complete_fields':complete,'runtime_running':running})
        if self.first_idle_scan is None:
            if not (scan_valid and running and idle):
                return self.snapshot()
            self.first_idle_scan = scan
        else:
            wrong = {key: values.get(key) for key, expected in IDLE.items()
                     if type(values.get(key)) is bool and values[key] is not expected}
            # A true sequence_running is another active-output witness even if
            # some other required row is unreadable. It is not required to start.
            if values.get('sequence_running') is True:
                wrong['sequence_running'] = True
            if wrong:
                self.active_after_idle.append({'image': observation['image'], 'scan': scan, 'fields': wrong})
        if not running:
            self.invalid_reasons.append('RUNTIME_NOT_RUNNING_AFTER_FIRST_IDLE')
        if scan_valid and self.last_scan is not None and scan < self.last_scan:
            self.invalid_reasons.append('NON_MONOTONIC_SCAN_AFTER_FIRST_IDLE')
        if scan_valid:
            self.last_scan = scan
        if not (scan_valid and running and idle):
            miss = {'image': observation['image'], 'scan': scan,
                    'complete_fields': complete, 'runtime_running': running}
            self.uncovered_observations.append(miss)
            if not complete or not scan_valid:
                self.parse_misses.append(miss)
            if scan_valid and self.last_complete_scan is not None and scan-self.last_complete_scan >= MAX_GAP_EXCLUSIVE:
                self.invalid_reasons.append('IDLE_SAMPLE_SCAN_GAP_TOO_LARGE')
            return self.snapshot()
        if self.last_complete_scan is not None:
            gap = scan-self.last_complete_scan
            self.max_scan_gap = max(self.max_scan_gap, gap)
            self.max_wall_gap = max(self.max_wall_gap, observation['wall_seconds']-self.last_complete_wall)
            if gap >= MAX_GAP_EXCLUSIVE:
                self.invalid_reasons.append('IDLE_SAMPLE_SCAN_GAP_TOO_LARGE')
        self.last_complete_scan = scan
        self.last_complete_wall = observation['wall_seconds']
        self.complete_samples += 1
        return self.snapshot()

    def snapshot(self):
        span = (self.last_complete_scan-self.first_idle_scan
                if self.first_idle_scan is not None and self.last_complete_scan is not None else 0)
        return {'first_idle_scan': self.first_idle_scan, 'last_complete_idle_scan': self.last_complete_scan,
                'accepted_idle_span_scans': span, 'accepted_idle_span_seconds': span*ACCEPTED_SCAN_MS/1000,
                'maximum_complete_sample_gap_scans': self.max_scan_gap,
                'maximum_complete_sample_gap_wall_seconds': self.max_wall_gap,
                'complete_idle_samples': self.complete_samples,
                'parse_misses': self.parse_misses, 'uncovered_observations': self.uncovered_observations,
                'active_after_idle': self.active_after_idle,
                'first_observed_clearance_scan':self.clearance_scan,
                'possible_replay_after_clearance':self.possible_replay_after_clearance,
                'post_click_max_complete_sample_gap_scans':self.post_max_scan_gap,
                'post_click_parse_misses':self.post_parse_misses,
                'partial_reasons': sorted(set(self.invalid_reasons)),
                'bounded_no_replay_coverage_observed': span >= REQUIRED_IDLE_SCANS
                    and self.clearance_scan is not None and not self.invalid_reasons
                    and not self.active_after_idle and not self.possible_replay_after_clearance,
                'physical_lamp_acceptance': False, 'fresh_action_acknowledgement_proven': False}


SIGNALS = (700,155,1020,305)
GROUNDED_CROSS = (441,381)
BASELINE = 'pedestrian-r1-path-clear.png'


def parse_pedestrian(ocr):
    if (ocr.get('width'),ocr.get('height')) != (960,450):
        raise base.ProbeError('PEDESTRIAN_NATIVE_SIGNALS_CROP_CHANGED')
    return parse_exact_bool_rows(ocr,12)


def observe(e, worker, image, captured, began, number):
    rows = parse_pedestrian(worker.read(e.crop(image,SIGNALS,'signals3x',3)))
    header = worker.read(e.crop(image,base.HEADER_BOX,'runtime-header3x',3))
    header_lines = [base.canon(row.get('text','')) for row in header.get('lines',[])]
    result = {'image':image.name, **rows, 'scan':base.parse_scan(header),
              'wall_seconds':captured-began,
              'runtime_running':'localruntimerunning' in header_lines,
              'physical_lamp_acceptance':False,'fresh_action_acknowledgement_proven':False}
    e.event('ACTUAL_PEDESTRIAN_SCREEN_OBSERVATION',number=number,**result)
    return result


def click_cross_fresh(bridge,e,result,image,captured,observation,xy):
    bridge.health(); base.guard()
    age=time.monotonic()-captured
    if age>.5:
        e.event('STALE_WALK_IMAGE_NO_INPUT',image=image.name,age_seconds=age)
        return False
    result['cross_attempted']=True
    e.event('OS_CLICK_ATTEMPT',purpose='SINGLE_CROSS_DURING_PUBLIC_WALK',x=xy[0],y=xy[1],
            app_pid=bridge.pid,witness={'image':image.name,'age_seconds':age,'observation':observation})
    raw=bridge.request('/input',{'x':xy[0],'y':xy[1]})
    e.event('OS_CLICK_DELIVERED',purpose='SINGLE_CROSS_DURING_PUBLIC_WALK',delivery_sha256=hashlib.sha256(raw).hexdigest())
    return True


def self_test():
    def make(values):
        return {'width':960,'height':450,'lines':[{'words':[{'text':key+' '+str(value),
            'x':10,'y':i*50,'w':700,'h':28}]} for i,(key,value) in enumerate(values.items())]}
    good={**EXPECTED,'crossing_request':False,'sequence_running':True}
    assert parse_pedestrian(make(good))['public_walk_predicate']
    for key in EXPECTED:
        wrong=dict(good);wrong[key]=not wrong[key]
        assert not parse_pedestrian(make(wrong))['public_walk_predicate']
        missing=dict(good);missing.pop(key)
        assert not parse_pedestrian(make(missing))['public_walk_predicate']
    conflict=make(good);conflict['lines'].append({'words':[{'text':'pedestrian_walk False','x':10,'y':420,'w':700,'h':28}]})
    assert not parse_pedestrian(conflict)['public_walk_predicate']
    assert not parse_pedestrian(make(IDLE))['public_walk_predicate']
    def sample(scan,values=None,running=True):
        return {'image':'synthetic-%d.png'%scan,'values':dict(IDLE) if values is None else values,
                'scan':scan,'runtime_running':running,'wall_seconds':scan*.02}
    clearance={**IDLE,'vehicle_ready':False,'vehicle_stop':True,'sequence_running':True}
    def prime():
        c=IdleCoverage();c.add(sample(0,good));c.add(sample(10,clearance));return c
    def run(scans):
        c=prime()
        for scan in scans:c.add(sample(scan+20))
        return c
    assert run(list(range(0,401,20))).snapshot()['bounded_no_replay_coverage_observed']
    assert not run(list(range(0,381,20))+[399]).snapshot()['bounded_no_replay_coverage_observed']
    assert run(list(range(0,391,39))+[400]).snapshot()['bounded_no_replay_coverage_observed']
    assert not run(list(range(0,401,40))).snapshot()['bounded_no_replay_coverage_observed']
    c=run([0]);missing=dict(IDLE);missing.pop('pedestrian_walk');c.add(sample(30,missing));c.add(sample(40))
    assert len(c.parse_misses)==1 and c.max_scan_gap==20
    c.add(sample(80,missing));c.add(sample(100));assert 'IDLE_SAMPLE_SCAN_GAP_TOO_LARGE' in c.invalid_reasons
    c=run([0]);bad=dict(IDLE);bad['vehicle_amber']=True;bad.pop('pedestrian_walk');c.add(sample(25,bad))
    for scan in range(40,421,20):c.add(sample(scan))
    assert c.active_after_idle and not c.snapshot()['bounded_no_replay_coverage_observed']
    c=run([0,20]);c.add(sample(30));assert 'NON_MONOTONIC_SCAN_AFTER_FIRST_IDLE' in c.invalid_reasons
    c=run([0]);c.add(sample(25,running=False));assert 'RUNTIME_NOT_RUNNING_AFTER_FIRST_IDLE' in c.invalid_reasons
    # Replay beginning after a one-scan idle that the rendering never samples.
    c=prime();c.add(sample(20,{**clearance,'vehicle_amber':True}))
    assert c.first_idle_scan is None and c.possible_replay_after_clearance
    c=prime();c.add(sample(20,good));assert c.possible_replay_after_clearance
    c=prime();c.add(sample(50,clearance))
    assert 'POST_CLICK_TRANSITION_SAMPLE_GAP_TOO_LARGE' in c.invalid_reasons
    c=IdleCoverage();c.add(sample(0,good));c.add(sample(20))
    for scan in range(40,421,20):c.add(sample(scan))
    assert not c.snapshot()['bounded_no_replay_coverage_observed']  # Clearance unobserved.
    try:base.guard(base.DEADLINE)
    except base.ProbeError:pass
    else:raise AssertionError('Cutoff guard absent')


def main():
    p=argparse.ArgumentParser(description=__doc__)
    modes=p.add_mutually_exclusive_group(required=True)
    modes.add_argument('--session',type=Path);modes.add_argument('--offline-image',type=Path);modes.add_argument('--self-test',action='store_true')
    p.add_argument('--cross-x',type=int);p.add_argument('--cross-y',type=int)
    p.add_argument('--prefix',default='pedestrian-active-walk');p.add_argument('--seconds',type=float,default=30)
    args=p.parse_args();base.guard()
    if not 15<=args.seconds<=30:raise base.ProbeError('PEDESTRIAN_DURATION_MUST_BE_15_TO_30_SECONDS')
    if args.session and (args.cross_x,args.cross_y)!=GROUNDED_CROSS:
        raise base.ProbeError('EXPLICIT_CURRENT_NATIVE_CROSS_COORDINATE_REQUIRED')
    e=base.Evidence(args.prefix);began=time.monotonic();hard_end=began+args.seconds
    worker=None;coverage=IdleCoverage()
    result={'status':'QA_ACTIVE_WALK_CASE_PENDING','cross_attempted':False,'native_session':bool(args.session),
            'screen_only':True,'physical_lamp_acceptance':False,'fresh_action_acknowledgement_proven':False,
            'native_layout_baseline':BASELINE,'source_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
            'accepted_scan_period_ms':ACCEPTED_SCAN_MS,'no_setup_or_further_requests':True}
    try:
        if args.self_test:
            self_test();result.update(status='OFFLINE_PEDESTRIAN_PARSER_COVERAGE_GUARDS_PASS',no_bridge_endpoints=True);return
        worker=base.OcrWorker(e,hard_end)
        if args.offline_image:
            source=args.offline_image.resolve()
            if base.ROOT not in source.parents or not source.is_file():raise base.ProbeError('OFFLINE_PEDESTRIAN_IMAGE_OUTSIDE_AUDIT')
            image,_=e.save('offline-original.png',source.read_bytes())
            result.update(status='OFFLINE_SAVED_PEDESTRIAN_IMAGE_OBSERVED',
                          observation=observe(e,worker,image,began,began,0),no_bridge_endpoints=True);return
        require_native_qualification()
        bridge=base.Bridge(json.loads(args.session.read_text(encoding='utf-8-sig')),e,hard_end)
        number=0;seek_end=began+min(10,args.seconds-15)
        while time.monotonic()<seek_end:
            base.guard();image,captured=bridge.capture('seek-%03d.png'%number)
            current=observe(e,worker,image,captured,began,number);number+=1
            result.update(last_observation=current,observations=number)
            if current['public_walk_predicate'] and current['runtime_running']:
                if click_cross_fresh(bridge,e,result,image,captured,current,(args.cross_x,args.cross_y)):
                    result['match_image']=image.name
                    coverage.add(current)  # Bridge the matched WALK to post-click frames.
                    # Preserve physical feedback before any OCR work delays it.
                    delivered=time.monotonic()
                    immediate,at_immediate=bridge.capture('cross-immediate.png')
                    base.guard();delay=max(0,.3-(time.monotonic()-delivered))
                    if delay:time.sleep(delay)
                    settled,at_settled=bridge.capture('cross-settled-300ms.png')
                    checkpoints=[]
                    for checkpoint,at in [(immediate,at_immediate),(settled,at_settled)]:
                        observation=observe(e,worker,checkpoint,at,began,number);number+=1
                        checkpoints.append(observation);coverage.add(observation)
                    result.update(post_click_checkpoints=checkpoints,
                        active_walk_public_scope_still_observed=all(o['public_walk_predicate'] and o['runtime_running'] for o in checkpoints),
                        actual_settled_delay_seconds=at_settled-delivered)
                    break
            time.sleep(min(.025,max(0,seek_end-time.monotonic())))
        if not result['cross_attempted']:
            result['status']='PUBLIC_WALK_UNOBSERVED_OR_STALE_NO_INPUT';return
        while time.monotonic()<hard_end-.5:
            base.guard();image,captured=bridge.capture('after-cross-%03d.png'%number)
            current=observe(e,worker,image,captured,began,number);number+=1
            summary=coverage.add(current)
            result.update(last_observation=current,observations=number,idle_coverage=summary)
            if summary['possible_replay_after_clearance']:
                result['status']='POSSIBLE_REPLAY_AFTER_CLEARANCE_OBSERVED_REVIEW_PENDING';return
            if summary['active_after_idle']:
                result['status']='ACTIVE_OUTPUT_AFTER_FIRST_IDLE_OBSERVED_REVIEW_PENDING';return
            if summary['accepted_idle_span_scans']>=REQUIRED_IDLE_SCANS:
                result['status']=('BOUNDED_IDLE_COVERAGE_OBSERVED_ACK_AND_PHYSICAL_REVIEW_PENDING'
                                  if summary['bounded_no_replay_coverage_observed']
                                  else 'BOUNDED_IDLE_COVERAGE_PARTIAL_ACK_AND_PHYSICAL_REVIEW_PENDING')
                return
            time.sleep(min(.025,max(0,hard_end-.5-time.monotonic())))
        result['status']='BOUNDED_OBSERVATION_ENDED_WITH_PARTIAL_IDLE_COVERAGE'
    except BaseException as exc:
        result.update(status='QA_ACTIVE_WALK_CASE_PENDING_PROBE_ENDED',
                      error_code=str(exc) if isinstance(exc,base.ProbeError) else type(exc).__name__)
    finally:
        result['idle_coverage']=coverage.snapshot()
        if worker is not None:result['owned_ocr_worker_returncode']=worker.close()
        result['elapsed_seconds']=round(time.monotonic()-began,3);e.finish(result)
        print(json.dumps({'status':result['status'],'cross_attempted':result['cross_attempted'],
            'physical_lamp_acceptance':False,'fresh_action_acknowledgement_proven':False,
            'folder':str(e.folder),'error_code':result.get('error_code'),'elapsed_seconds':result['elapsed_seconds']}))


if __name__=='__main__':
    def terminated(signum,frame):raise base.ProbeError('OWN_PEDESTRIAN_PROBE_TERMINATED')
    if hasattr(signal,'SIGTERM'):signal.signal(signal.SIGTERM,terminated)
    main()
