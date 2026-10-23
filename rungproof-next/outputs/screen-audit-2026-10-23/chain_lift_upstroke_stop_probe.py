"""Root-invoked native loaded-upstroke Stop observer, no setup/state APIs.

Root must physically Start and leave actual App Playback open first.
No menu-opening/Start/Run/Reset inputs. Root owns physical pose acceptance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import signal
import time
import parcel_handoff_probe as base
from cable_knife_stop_probe import menu_context

SIGNALS=(700,128,1020,440)
SCALE=3
EXPECTED={'lift_enable':True,'lift_lower':False,'chain_run':False,'carton_on_lift':True,
          'lift_home':False,'lift_upper':False,'carton_at_receiver':False,
          'transfer_fault':False,'destination_clear':True,'cycle_active':True}
ALL_KEYS=tuple(EXPECTED)+('box_present','carton_on_infeed','cycle_complete','lift_start',
                          'receiver_beam_blocked','receiver_occupied')

def parse_lift(ocr):
    if (ocr.get('width'),ocr.get('height'))!=(960,936):raise base.ProbeError('LIFT_SIGNALS_CROP_DIMENSIONS_CHANGED')
    words=[w for line in ocr.get('lines',[]) for w in line.get('words',[])]
    rows=[]
    for word in sorted(words,key=lambda w:(w['y']+w['h']/2,w['x'])):
        center=word['y']+word['h']/2
        row=next((r for r in rows if abs(r['anchor_y']-center)<=12),None)
        if row is None:row={'anchor_y':center,'words':[]};rows.append(row)
        row['words'].append(word)
    witnesses={key:[] for key in ALL_KEYS}
    for row in rows:
        raw=' '.join(w['text'] for w in sorted(row['words'],key=lambda w:w['x']))
        text=base.canon(raw)
        for key in ALL_KEYS:
            prefix=base.canon(key)
            if not text.startswith(prefix):continue
            value={'true':True,'false':False}.get(text[len(prefix):])
            if value is not None:witnesses[key].append({'text':raw,'value':value,'row_center_y':row['anchor_y']})
    values={key:next(iter(s)) if len(s)==1 else None for key,items in witnesses.items() for s in [{r['value'] for r in items}]}
    return {'values':values,'row_witnesses':witnesses,
            'loaded_upstroke_predicate_matched':all(values[k] is v for k,v in EXPECTED.items())}

def observe(evidence,worker,image,number):
    rows=parse_lift(worker.read(evidence.crop(image,SIGNALS,'signals3x',SCALE)))
    header=worker.read(evidence.crop(image,base.HEADER_BOX,'runtime-header3x',3));text=base.canon(header.get('text',''))
    result={'image':image.name,'rows':rows,'scan':base.parse_scan(header),
            'runtime_running':'running' in text and 'stopped' not in text,'runtime_stopped':'stopped' in text}
    evidence.event('ACTUAL_CHAIN_LIFT_SCREEN_OBSERVATION',number=number,**result)
    return result

def stop_fresh(bridge,evidence,result,image,captured,observation):
    bridge.health();base.guard();age=time.monotonic()-captured
    if age>.5:return False
    result['stop_attempted']=True
    evidence.event('OS_CLICK_ATTEMPT',purpose='SINGLE_APP_STOP_ON_FRESH_LOADED_UPSTROKE',
                   x=base.STOP_CLICK[0],y=base.STOP_CLICK[1],app_pid=bridge.pid,
                   witness={'image':image.name,'age_seconds':age,'observation':observation})
    raw=bridge.request('/input',{'x':base.STOP_CLICK[0],'y':base.STOP_CLICK[1]})
    evidence.event('OS_CLICK_DELIVERED',purpose='SINGLE_APP_STOP_ON_FRESH_LOADED_UPSTROKE',delivery_sha256=hashlib.sha256(raw).hexdigest())
    return True

def main():
    p=argparse.ArgumentParser(description=__doc__)
    mode=p.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session',type=Path);mode.add_argument('--offline-image',type=Path);mode.add_argument('--self-test',action='store_true')
    p.add_argument('--prefix',default='chain-lift-upstroke-stop');p.add_argument('--seconds',type=float,default=25)
    args=p.parse_args();base.guard()
    if not 5<=args.seconds<=25:raise base.ProbeError('LIFT_PROBE_DURATION_OUT_OF_BOUNDS')
    e=base.Evidence(args.prefix);began=time.monotonic();hard_end=began+args.seconds;poll_end=hard_end-3;worker=None
    result={'status':'QA_UPSTROKE_CASE_PENDING','stop_attempted':False,'screen_only':True,
            'native_session':bool(args.session),'physical_pose_acceptance':False,
            'required_precondition':'Root has physically Started and left actual App Playback menu open'}
    try:
        if args.self_test:
            def make(values):return {'width':960,'height':936,'lines':[{'words':[{'text':key+' '+str(value),'x':10,'y':i*54,'w':600,'h':32}]} for i,(key,value) in enumerate(values.items())]}
            assert parse_lift(make(EXPECTED))['loaded_upstroke_predicate_matched']
            for key,value in [('chain_run',True),('lift_home',True),('lift_upper',True),('lift_lower',True),('carton_on_lift',False),('carton_at_receiver',True),('transfer_fault',True),('cycle_active',False),('destination_clear',False),('lift_enable',False)]:
                bad=dict(EXPECTED);bad[key]=value
                assert not parse_lift(make(bad))['loaded_upstroke_predicate_matched']
            for key in EXPECTED:
                bad=dict(EXPECTED);bad.pop(key)
                assert not parse_lift(make(bad))['loaded_upstroke_predicate_matched']
            conflict=make(EXPECTED);conflict['lines'].append({'words':[{'text':'lift_upper True','x':10,'y':800,'w':600,'h':32}]})
            assert not parse_lift(conflict)['loaded_upstroke_predicate_matched']
            try:base.guard(base.DEADLINE)
            except base.ProbeError:pass
            else:raise AssertionError('Cutoff not guarded')
            result.update(status='OFFLINE_UPSTROKE_WRONG_PHASE_MISSING_CONFLICT_CUTOFF_PASS',no_bridge_endpoints=True);return
        worker=base.OcrWorker(e,hard_end)
        if args.offline_image:
            source=args.offline_image.resolve()
            if base.ROOT not in source.parents or not source.is_file():raise base.ProbeError('OFFLINE_LIFT_IMAGE_OUTSIDE_AUDIT')
            image,_=e.save('offline-original.png',source.read_bytes())
            result.update(status='OFFLINE_SAVED_LIFT_IMAGE_OBSERVED',observation=observe(e,worker,image,0),
                          stop_menu_observed=menu_context(e,worker,image),no_bridge_endpoints=True);return
        bridge=base.Bridge(json.loads(args.session.read_text(encoding='utf-8-sig')),e,hard_end)
        number=0;streak=0
        while time.monotonic()<poll_end:
            base.guard();image,captured=bridge.capture('observe-%03d.png'%number)
            context=menu_context(e,worker,image) if number==0 else None
            if number==0 and not context:raise base.ProbeError('ROOT_ARMED_STOP_MENU_UNOBSERVED_NO_INPUT')
            current=observe(e,worker,image,number)
            streak=streak+1 if current['rows']['loaded_upstroke_predicate_matched'] and current['runtime_running'] else 0
            result.update(observations=number+1,last_observation=current,consecutive_upstroke_frames=streak)
            if streak>=2:
                if context is None:context=menu_context(e,worker,image)
                if not context:raise base.ProbeError('FRESH_UPSTROKE_STOP_MENU_UNOBSERVED_NO_INPUT')
                if stop_fresh(bridge,e,result,image,captured,current):
                    saved=[]
                    for suffix,delay in [('immediate',0),('settled',.5),('held',1.0)]:
                        base.guard()
                        if delay:time.sleep(min(delay,max(0,hard_end-time.monotonic())))
                        checkpoint,_=bridge.capture('stop-'+suffix+'.png');saved.append(observe(e,worker,checkpoint,number+len(saved)+1))
                    result.update(status='APP_STOP_SENT_UPSTROKE_HOLD_REVIEW_PENDING',match_image=image.name,stop_checkpoints=saved,
                                  stopped_between_endpoints_observed=all(o['rows']['values']['lift_home'] is False and o['rows']['values']['lift_upper'] is False and o['rows']['values']['carton_on_lift'] is True for o in saved),
                                  stopped_scan_held=saved[0]['scan'] is not None and len({o['scan'] for o in saved})==1 and all(o['runtime_stopped'] for o in saved),
                                  stopped_commands_cleared=all(o['rows']['values'][k] is False for o in saved for k in ['chain_run','lift_enable','lift_lower','cycle_active']))
                    return
                e.event('STALE_UPSTROKE_IMAGE_NO_STOP',image=image.name,age_seconds=time.monotonic()-captured)
            number+=1;time.sleep(min(.025,max(0,poll_end-time.monotonic())))
    except BaseException as exc:
        result.update(status='QA_UPSTROKE_CASE_PENDING_PROBE_ENDED',error_code=str(exc) if isinstance(exc,base.ProbeError) else type(exc).__name__)
    finally:
        if worker is not None:result['owned_ocr_worker_returncode']=worker.close()
        result['elapsed_seconds']=round(time.monotonic()-began,3);e.finish(result)
        print(json.dumps({'status':result['status'],'stop_attempted':result['stop_attempted'],'physical_pose_acceptance':False,
                          'folder':str(e.folder),'error_code':result.get('error_code'),'elapsed_seconds':result['elapsed_seconds']}))

if __name__=='__main__':
    def terminated(signum,frame):raise base.ProbeError('OWN_CHAIN_LIFT_PROBE_TERMINATED')
    if hasattr(signal,'SIGTERM'):signal.signal(signal.SIGTERM,terminated)
    main()
