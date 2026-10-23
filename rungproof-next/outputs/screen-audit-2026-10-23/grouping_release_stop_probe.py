"""Root-invoked native grouping-release Stop observer; no setup or state APIs.

Root stages the existing fixture through physical ENABLE/CLEAR/LOAD first.
No Start/Run/Reset, no input modification, no automatic click retry.
"""
import argparse
import hashlib
import json
from pathlib import Path
import signal
import time
import parcel_handoff_probe as base

SIGNALS=(700,128,1020,430)
EXPECTED={'group_count':'3','group_release':'true','group_conveyor_run':'true',
          'machine_enabled':'true','release_clear':'true','stop_position':'100',
          'stop_raised':'true','transfer_complete':'false'}
ALL_KEYS=tuple(EXPECTED)+('stop_lowered','group_ready','group_staged')

def parse_grouping(ocr):
    # WinRT can split a label and value into different OCR lines. Join only
    # words on the same physical row in the unchanged 2x Signals crop.
    words=[w for line in ocr.get('lines',[]) for w in line.get('words',[])]
    rows=[]
    for word in sorted(words,key=lambda w:(w['y']+w['h']/2,w['x'])):
        center=word['y']+word['h']/2
        row=next((r for r in rows if abs(r['anchor_y']-center)<=8),None)
        if row is None:row={'anchor_y':center,'words':[]};rows.append(row)
        row['words'].append(word)
    witnesses={key:[] for key in ALL_KEYS}
    for row in rows:
        raw=' '.join(w['text'] for w in sorted(row['words'],key=lambda w:w['x']))
        text=base.canon(raw)
        for key in ALL_KEYS:
            prefix=base.canon(key)
            if not text.startswith(prefix):continue
            value=text[len(prefix):]
            if key in ('group_count','stop_position'):
                if value in ('o','0'):value='0'
                if not value.isdecimal():continue
                value=str(int(value))
            elif value not in ('true','false'):continue
            witnesses[key].append({'text':raw,'value':value,'row_center_y':row['anchor_y']})
    values={key:next(iter(s)) if len(s)==1 else None for key,items in witnesses.items() for s in [{r['value'] for r in items}]}
    return {'values':values,'row_witnesses':witnesses,
            'release_predicate_matched':all(values[k]==v for k,v in EXPECTED.items())}

def observe(evidence,worker,image,number):
    rows=parse_grouping(worker.read(evidence.crop(image,SIGNALS,'signals2x',2)))
    header=worker.read(evidence.crop(image,base.HEADER_BOX,'runtime-header3x',3))
    text=base.canon(header.get('text',''));scan=base.parse_scan(header)
    result={'rows':rows,'scan':scan,'runtime_running':'running' in text and 'stopped' not in text,
            'runtime_stopped':'stopped' in text,'image':image.name}
    evidence.event('ACTUAL_GROUPING_SCREEN_OBSERVATION',number=number,**result)
    return result

def stop_fresh(bridge,evidence,result,image,captured,observation):
    bridge.health();base.guard();age=time.monotonic()-captured
    if age>1.0:return False
    result['stop_attempted']=True
    evidence.event('OS_CLICK_ATTEMPT',purpose='SINGLE_APP_STOP_ON_FRESH_GROUPING_RELEASE',
                   x=base.STOP_CLICK[0],y=base.STOP_CLICK[1],app_pid=bridge.pid,
                   witness={'image':image.name,'age_seconds':age,'observation':observation})
    raw=bridge.request('/input',{'x':base.STOP_CLICK[0],'y':base.STOP_CLICK[1]})
    evidence.event('OS_CLICK_DELIVERED',purpose='SINGLE_APP_STOP_ON_FRESH_GROUPING_RELEASE',delivery_sha256=hashlib.sha256(raw).hexdigest())
    return True

def main():
    p=argparse.ArgumentParser(description=__doc__)
    mode=p.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session',type=Path);mode.add_argument('--offline-image',type=Path);mode.add_argument('--self-test',action='store_true')
    p.add_argument('--prefix',default='grouping-release-stop');p.add_argument('--seconds',type=float,default=35)
    args=p.parse_args();base.guard()
    if not 5<=args.seconds<=35:raise base.ProbeError('GROUPING_PROBE_DURATION_OUT_OF_BOUNDS')
    e=base.Evidence(args.prefix);began=time.monotonic();hard_end=began+args.seconds;poll_end=hard_end-3;worker=None
    result={'status':'QA_RELEASE_CASE_PENDING','stop_attempted':False,'screen_only':True,
            'native_session':bool(args.session),'physical_pose_acceptance':False}
    try:
        if args.self_test:
            def make(values):
                return {'lines':[{'words':[{'text':key+' '+value,'x':10,'y':i*36,'w':350,'h':22}]} for i,(key,value) in enumerate(values.items())]}
            assert parse_grouping(make(EXPECTED))['release_predicate_matched']
            for key,value in [('group_count','2'),('group_release','false'),('stop_position','0'),('transfer_complete','true')]:
                wrong=dict(EXPECTED);wrong[key]=value
                assert not parse_grouping(make(wrong))['release_predicate_matched']
            missing=dict(EXPECTED);missing.pop('group_conveyor_run')
            assert not parse_grouping(make(missing))['release_predicate_matched']
            conflict=make(EXPECTED);conflict['lines'].append({'words':[{'text':'transfer_complete true','x':10,'y':500,'w':350,'h':22}]})
            assert not parse_grouping(conflict)['release_predicate_matched']
            try:base.guard(base.DEADLINE)
            except base.ProbeError:pass
            else:raise AssertionError('Cutoff not guarded')
            result.update(status='OFFLINE_GROUPING_PREDICATE_AND_CUTOFF_GUARDS_PASS',no_bridge_endpoints=True);return
        worker=base.OcrWorker(e,hard_end)
        if args.offline_image:
            source=args.offline_image.resolve()
            if base.ROOT not in source.parents or not source.is_file():raise base.ProbeError('OFFLINE_GROUPING_IMAGE_OUTSIDE_AUDIT')
            image,_=e.save('offline-original.png',source.read_bytes())
            result.update(status='OFFLINE_SAVED_GROUPING_IMAGE_OBSERVED',observation=observe(e,worker,image,0),no_bridge_endpoints=True);return
        bridge=base.Bridge(json.loads(args.session.read_text(encoding='utf-8-sig')),e,hard_end)
        number=0;streak=0
        while time.monotonic()<poll_end:
            base.guard();image,captured=bridge.capture('observe-%03d.png'%number)
            current=observe(e,worker,image,number)
            qualifying=current['rows']['release_predicate_matched'] and current['runtime_running']
            streak=streak+1 if qualifying else 0
            result.update(observations=number+1,last_observation=current,consecutive_release_frames=streak)
            if streak>=2:
                bridge.click(base.MENU_CLICK,'OPEN_REVIEWED_PLAYBACK_FOR_GROUPING_STOP')
                base.guard();time.sleep(min(.3,max(0,hard_end-time.monotonic())))
                armed,captured=bridge.capture('menu-armed-release-recheck.png')
                menu=worker.read(e.crop(armed,base.MENU_BOX,'menu2x',2))
                aliases={'stopscan','stopsimulatorscan','stopsirnulatorscan'}
                if not any(base.canon(r.get('text','')) in aliases for r in menu.get('lines',[])):
                    raise base.ProbeError('ACTUAL_STOP_MENU_CONTEXT_UNOBSERVED_NO_STOP')
                recheck=observe(e,worker,armed,number+1)
                result['menu_armed_recheck']=recheck
                if not(recheck['rows']['release_predicate_matched'] and recheck['runtime_running']):
                    result['status']='QA_RELEASE_CASE_PENDING_PHASE_ADVANCED_BEFORE_STOP';return
                if not stop_fresh(bridge,e,result,armed,captured,recheck):
                    result['status']='QA_RELEASE_CASE_PENDING_FRESHNESS_UNOBSERVED_NO_STOP';return
                immediate,_=bridge.capture('stop-immediate.png');first=observe(e,worker,immediate,number+2)
                base.guard();time.sleep(min(1.2,max(0,hard_end-time.monotonic())))
                held,_=bridge.capture('stop-held.png');last=observe(e,worker,held,number+3)
                result.update(status='APP_STOP_SENT_ACTUAL_RELEASE_HOLD_REVIEW_PENDING',stop_immediate=first,stop_held=last,
                              stopped_before_complete_observed=all(o['rows']['values']['transfer_complete']=='false' for o in [first,last]),
                              stopped_scan_held=first['scan'] is not None and first['scan']==last['scan'] and first['runtime_stopped'] and last['runtime_stopped'])
                return
            number+=1;time.sleep(min(.06,max(0,poll_end-time.monotonic())))
    except BaseException as exc:
        result.update(status='QA_RELEASE_CASE_PENDING_PROBE_ENDED',error_code=str(exc) if isinstance(exc,base.ProbeError) else type(exc).__name__)
    finally:
        if worker is not None:result['owned_ocr_worker_returncode']=worker.close()
        result['elapsed_seconds']=round(time.monotonic()-began,3);e.finish(result)
        print(json.dumps({'status':result['status'],'stop_attempted':result['stop_attempted'],
                          'physical_pose_acceptance':False,'folder':str(e.folder),'error_code':result.get('error_code'),'elapsed_seconds':result['elapsed_seconds']}))

if __name__=='__main__':
    def terminated(signum,frame):raise base.ProbeError('OWN_GROUPING_PROBE_TERMINATED')
    if hasattr(signal,'SIGTERM'):signal.signal(signal.SIGTERM,terminated)
    main()
