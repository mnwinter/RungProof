"""Root-invoked native knife-phase Stop observer; root arms Playback first.

No Start/Run/Reset/setup and no state APIs. Root owns physical pose acceptance.
One qualifying fresh outbound image is enough; the stroke is only0.8 seconds.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import signal
import time
import parcel_handoff_probe as base

SIGNALS=(700,128,1020,440)
BOOL_EXPECTED={'cutter_fire':True,'cut_done':False,'cutter_home':False,
               'length_reached':True,'feed_run':False,'cut_fault':False}
BOOL_KEYS=tuple(BOOL_EXPECTED)+('cycle_active','cycle_complete','cut_complete','batch_start','cable_present')
NUMBER_KEYS=('cutter_position','measured_length_m','stock_remaining_m','feed_speed_mps')
STOP_ALIASES={'stopscan','stopsimulatorscan','stopsirnulatorscan'}

def numeric_field(raw):
    # Corrections are confined to numeric value fields; names are never fuzzy.
    raw=raw.strip().replace('\u2212','-')
    if not re.fullmatch(r'[+-]?(?:[0-9IlioO]+(?:[.,][0-9IlioO]*)?|[.,][0-9IlioO]+)',raw):return None
    value=float(raw.translate(str.maketrans({'I':'1','l':'1','i':'1','O':'0','o':'0',',':'.'})))
    return value if math.isfinite(value) else None

def value_tail(raw,key):
    letters=[(char.casefold(),i) for i,char in enumerate(raw) if char.isalnum()]
    compact=''.join(char for char,_ in letters);prefix=base.canon(key)
    if not compact.startswith(prefix):return None
    return raw[letters[len(prefix)-1][1]+1:].strip(' \t:=')

def parse_cable(ocr):
    if (ocr.get('width'),ocr.get('height'))!=(640,624):raise base.ProbeError('CABLE_SIGNALS_CROP_DIMENSIONS_CHANGED')
    words=[w for line in ocr.get('lines',[]) for w in line.get('words',[])]
    rows=[]
    for word in sorted(words,key=lambda w:(w['y']+w['h']/2,w['x'])):
        center=word['y']+word['h']/2
        row=next((r for r in rows if abs(r['anchor_y']-center)<=8),None)
        if row is None:row={'anchor_y':center,'words':[]};rows.append(row)
        row['words'].append(word)
    witnesses={key:[] for key in BOOL_KEYS+NUMBER_KEYS}
    for row in rows:
        raw=' '.join(w['text'] for w in sorted(row['words'],key=lambda w:w['x']))
        for key in witnesses:
            tail=value_tail(raw,key)
            if tail is None:continue
            if key in NUMBER_KEYS:value=numeric_field(tail)
            else:value={'true':True,'false':False}.get(base.canon(tail))
            if value is not None:witnesses[key].append({'text':raw,'value':value,'row_center_y':row['anchor_y']})
    values={key:next(iter(s)) if len(s)==1 else None for key,items in witnesses.items() for s in [{r['value'] for r in items}]}
    position=values['cutter_position']
    matched=all(values[k] is v for k,v in BOOL_EXPECTED.items()) and position is not None and 0<position<1
    return {'values':values,'row_witnesses':witnesses,'outbound_knife_predicate_matched':matched}

def observe(evidence,worker,image,number):
    rows=parse_cable(worker.read(evidence.crop(image,SIGNALS,'signals2x',2)))
    header=worker.read(evidence.crop(image,base.HEADER_BOX,'runtime-header3x',3));text=base.canon(header.get('text',''))
    result={'image':image.name,'rows':rows,'scan':base.parse_scan(header),
            'runtime_running':'running' in text and 'stopped' not in text,'runtime_stopped':'stopped' in text}
    evidence.event('ACTUAL_CABLE_SCREEN_OBSERVATION',number=number,**result)
    return result

def menu_context(evidence,worker,image):
    ocr=worker.read(evidence.crop(image,base.MENU_BOX,'menu2x',2))
    return any(base.canon(row.get('text','')) in STOP_ALIASES for row in ocr.get('lines',[]))

def stop_fresh(bridge,evidence,result,image,captured,observation):
    bridge.health();base.guard();age=time.monotonic()-captured
    if age>.5:return False
    result['stop_attempted']=True
    evidence.event('OS_CLICK_ATTEMPT',purpose='SINGLE_APP_STOP_ON_FRESH_OUTBOUND_KNIFE',
                   x=base.STOP_CLICK[0],y=base.STOP_CLICK[1],app_pid=bridge.pid,
                   witness={'image':image.name,'age_seconds':age,'observation':observation})
    raw=bridge.request('/input',{'x':base.STOP_CLICK[0],'y':base.STOP_CLICK[1]})
    evidence.event('OS_CLICK_DELIVERED',purpose='SINGLE_APP_STOP_ON_FRESH_OUTBOUND_KNIFE',delivery_sha256=hashlib.sha256(raw).hexdigest())
    return True

def main():
    p=argparse.ArgumentParser(description=__doc__)
    mode=p.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session',type=Path);mode.add_argument('--offline-image',type=Path);mode.add_argument('--self-test',action='store_true')
    p.add_argument('--prefix',default='cable-knife-stop');p.add_argument('--seconds',type=float,default=25)
    args=p.parse_args();base.guard()
    if not 5<=args.seconds<=25:raise base.ProbeError('CABLE_PROBE_DURATION_OUT_OF_BOUNDS')
    e=base.Evidence(args.prefix);began=time.monotonic();hard_end=began+args.seconds;poll_end=hard_end-3;worker=None
    result={'status':'QA_KNIFE_CASE_PENDING','stop_attempted':False,'screen_only':True,
            'native_session':bool(args.session),'physical_pose_acceptance':False,
            'required_precondition':'Root has started feed and left actual App Playback menu open'}
    try:
        if args.self_test:
            good={**BOOL_EXPECTED,'cutter_position':.4}
            def make(values):return {'width':640,'height':624,'lines':[{'words':[{'text':key+' '+str(value),'x':10,'y':i*36,'w':420,'h':22}]} for i,(key,value) in enumerate(values.items())]}
            assert parse_cable(make(good))['outbound_knife_predicate_matched']
            for key,value in [('cutter_fire',False),('cut_done',True),('cutter_home',True),('length_reached',False),('feed_run',True),('cut_fault',True),('cutter_position',0),('cutter_position',1),('cutter_position',-.4)]:
                bad=dict(good);bad[key]=value
                assert not parse_cable(make(bad))['outbound_knife_predicate_matched']
            for key in good:
                bad=dict(good);bad.pop(key)
                assert not parse_cable(make(bad))['outbound_knife_predicate_matched']
            conflict=make(good);conflict['lines'].append({'words':[{'text':'cutter_position 0.8','x':10,'y':500,'w':420,'h':22}]})
            assert not parse_cable(conflict)['outbound_knife_predicate_matched']
            assert numeric_field('O.4')==.4 and numeric_field('I')==1 and numeric_field('NaN') is None
            assert value_tail('cutter_pOsition 0.4','cutter_position')=='0.4'
            try:base.guard(base.DEADLINE)
            except base.ProbeError:pass
            else:raise AssertionError('Cutoff not guarded')
            result.update(status='OFFLINE_CABLE_PREDICATE_COUNTEREXAMPLES_AND_CUTOFF_PASS',no_bridge_endpoints=True);return
        worker=base.OcrWorker(e,hard_end)
        if args.offline_image:
            source=args.offline_image.resolve()
            if base.ROOT not in source.parents or not source.is_file():raise base.ProbeError('OFFLINE_CABLE_IMAGE_OUTSIDE_AUDIT')
            image,_=e.save('offline-original.png',source.read_bytes())
            result.update(status='OFFLINE_SAVED_CABLE_IMAGE_OBSERVED',observation=observe(e,worker,image,0),
                          stop_menu_observed=menu_context(e,worker,image),no_bridge_endpoints=True);return
        bridge=base.Bridge(json.loads(args.session.read_text(encoding='utf-8-sig')),e,hard_end)
        number=0
        while time.monotonic()<poll_end:
            base.guard();image,captured=bridge.capture('observe-%03d.png'%number)
            context=menu_context(e,worker,image) if number==0 else None
            if number==0 and not context:raise base.ProbeError('ROOT_ARMED_STOP_MENU_UNOBSERVED_NO_INPUT')
            current=observe(e,worker,image,number)
            result.update(observations=number+1,last_observation=current)
            if current['rows']['outbound_knife_predicate_matched'] and current['runtime_running']:
                if context is None:context=menu_context(e,worker,image)
                if not context:raise base.ProbeError('FRESH_TARGET_STOP_MENU_UNOBSERVED_NO_INPUT')
                if stop_fresh(bridge,e,result,image,captured,current):
                    saved=[]
                    for suffix,delay in [('immediate',0),('settled',.5),('held',1.0)]:
                        base.guard()
                        if delay:time.sleep(min(delay,max(0,hard_end-time.monotonic())))
                        checkpoint,_=bridge.capture('stop-'+suffix+'.png');saved.append(observe(e,worker,checkpoint,number+len(saved)+1))
                    result.update(status='APP_STOP_SENT_KNIFE_HOLD_REVIEW_PENDING',match_image=image.name,stop_checkpoints=saved,
                                  stopped_before_cut_done_observed=all(o['rows']['values']['cut_done'] is False for o in saved),
                                  stopped_scan_held=saved[0]['scan'] is not None and len({o['scan'] for o in saved})==1 and all(o['runtime_stopped'] for o in saved))
                    return
                e.event('STALE_KNIFE_IMAGE_NO_STOP',image=image.name,age_seconds=time.monotonic()-captured)
            number+=1;time.sleep(min(.025,max(0,poll_end-time.monotonic())))
    except BaseException as exc:
        result.update(status='QA_KNIFE_CASE_PENDING_PROBE_ENDED',error_code=str(exc) if isinstance(exc,base.ProbeError) else type(exc).__name__)
    finally:
        if worker is not None:result['owned_ocr_worker_returncode']=worker.close()
        result['elapsed_seconds']=round(time.monotonic()-began,3);e.finish(result)
        print(json.dumps({'status':result['status'],'stop_attempted':result['stop_attempted'],'physical_pose_acceptance':False,
                          'folder':str(e.folder),'error_code':result.get('error_code'),'elapsed_seconds':result['elapsed_seconds']}))

if __name__=='__main__':
    def terminated(signum,frame):raise base.ProbeError('OWN_CABLE_PROBE_TERMINATED')
    if hasattr(signal,'SIGTERM'):signal.signal(signal.SIGTERM,terminated)
    main()
