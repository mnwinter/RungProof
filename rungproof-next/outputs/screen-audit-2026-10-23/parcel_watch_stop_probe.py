"""Root-invoked bounded actual Watch-row Stop observer, no backend state APIs.

Requires current grounded Engineering controls. Never Run/Reset or Modify.
Agent validation is offline only; live execution belongs to root's UI owner.
"""
import argparse
from datetime import datetime, timezone
import io
import json
from pathlib import Path
import re
import signal
import time
from PIL import Image
import parcel_handoff_probe as base

NAME_BOX=(225,300,320,535)
VALUE_BOX=(505,300,623,535)
WATCH_HEADER_BOX=(210,275,780,310)
SCALE=3
VALUE_START=((NAME_BOX[2]-NAME_BOX[0])+8)*SCALE
NAME_WIDTH=(NAME_BOX[2]-NAME_BOX[0])*SCALE
EXPECTED={'qastep':'qa_step','qascanstep':'qa_scan_step','qaclass':'qa_class'}

def numeric_field(text):
    # OCR corrections apply ONLY to words inside MONITOR VALUE, never names,
    # comments, Modify Value, another table or any application memory image.
    if not re.fullmatch(r'[0-9IlioO]{1,6}',text):return None
    text=text.translate(str.maketrans({'I':'1','l':'1','i':'1','O':'0','o':'0'}))
    return int(text) if text.isdigit() else None

def parse_watch(ocr):
    words=[w for line in ocr.get('lines',[]) for w in line.get('words',[])]
    name_words=[w for w in words if w['x']+w['w']/2<NAME_WIDTH]
    value_words=[w for w in words if w['x']+w['w']/2>=VALUE_START]
    buckets=[]
    for w in sorted(name_words,key=lambda w:(w['y']+w['h']/2,w['x'])):
        y=w['y']+w['h']/2
        found=next((b for b in buckets if abs(b['y']-y)<=18),None)
        if found is None:buckets.append({'y':y,'words':[w]})
        else:found['words'].append(w)
    result={k:[] for k in EXPECTED.values()}
    names_seen=set()
    for bucket in buckets:
        label=' '.join(w['text'] for w in sorted(bucket['words'],key=lambda w:w['x']))
        key=EXPECTED.get(base.canon(label))
        if key is None:continue
        names_seen.add(key)
        candidates=[w for w in value_words if abs((w['y']+w['h']/2)-bucket['y'])<=18 and numeric_field(w['text']) is not None]
        for w in candidates:
            result[key].append({'name_text':label,'monitor_text':w['text'],'value':numeric_field(w['text']),
                                'name_center_y':bucket['y'],'monitor_word':w})
    values={k:next(iter(s)) if len(s)==1 else None for k,rows in result.items() for s in [{r['value'] for r in rows}]}
    matched=(values['qa_step']==30 and values['qa_scan_step']==30 and values['qa_class']==2)
    return {'values':values,'row_witnesses':result,'names_seen':sorted(names_seen),'target_rows_matched':matched,
            'screen_watch_only':True,'physical_pose_acceptance':False}

def initial_watch_qualified(rows):
    # Native OCR may omit the initial zero class. Require its actual row name,
    # but keep its value unproven; the later Stop target still requires class2.
    return (set(EXPECTED.values()).issubset(rows['names_seen']) and
            rows['values']['qa_step'] is not None and
            rows['values']['qa_scan_step'] is not None)

def watch_strip(evidence,image):
    with Image.open(image) as original:
        if original.size!=base.SIZE:raise base.ProbeError('NATIVE_WATCH_CANVAS_CHANGED')
        names=original.crop(NAME_BOX).resize((NAME_WIDTH,(NAME_BOX[3]-NAME_BOX[1])*SCALE),Image.Resampling.LANCZOS)
        values=original.crop(VALUE_BOX).resize(((VALUE_BOX[2]-VALUE_BOX[0])*SCALE,(VALUE_BOX[3]-VALUE_BOX[1])*SCALE),Image.Resampling.LANCZOS)
        joined=Image.new('RGB',(VALUE_START+values.width,names.height),'white')
        joined.paste(names,(0,0));joined.paste(values,(VALUE_START,0))
        out=io.BytesIO();joined.save(out,format='PNG')
    path,entry=evidence.save(image.stem+'-name-and-monitor3x.png',out.getvalue())
    evidence.event('DERIVED_WATCH_FIELDS',source=image.name,image=entry,name_native_box=list(NAME_BOX),
                   monitor_native_box=list(VALUE_BOX),scale=SCALE,value_start_x=VALUE_START,
                   excludes='Address, Display Format, Modify Value, write buttons and comments')
    return path

def observe(evidence,worker,image,number):
    rows=parse_watch(worker.read(watch_strip(evidence,image)))
    scan=base.parse_scan(worker.read(evidence.crop(image,base.HEADER_BOX,'runtime-header3x',3)))
    evidence.event('ACTUAL_SCREEN_WATCH_OBSERVATION',image=image.name,number=number,scan=scan,watch=rows)
    return rows,scan

def context(evidence,worker,image):
    ocr=worker.read(evidence.crop(image,WATCH_HEADER_BOX,'watch-header2x',2))
    return 'monitorvalue' in base.canon(ocr.get('text',''))

def stop_menu_text_observed(ocr):
    # Exact row aliases from the reviewed Playback menu, not fuzzy matching.
    # WinRT read native "simulator" as "sirnulator" in the saved r2 image.
    accepted={'stopscan','stopsimulatorscan','stopsirnulatorscan'}
    return any(base.canon(row.get('text','')) in accepted for row in ocr.get('lines',[]))

def wheel(bridge,evidence):
    bridge.health();base.guard()
    data={'x':578,'y':460,'phase':'wheel','delta':-1200}
    evidence.event('OS_WHEEL_ATTEMPT',purpose='EXPLICIT_GROUNDED_WATCH_BOTTOM_SCROLL',input=data)
    bridge.request('/input',data)
    evidence.event('OS_WHEEL_DELIVERED',purpose='EXPLICIT_GROUNDED_WATCH_BOTTOM_SCROLL')

def main():
    p=argparse.ArgumentParser(description=__doc__)
    mode=p.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session',type=Path);mode.add_argument('--offline-image',type=Path);mode.add_argument('--self-test',action='store_true')
    p.add_argument('--prefix',default='parcel-watch-stop');p.add_argument('--seconds',type=float,default=40)
    p.add_argument('--start-x',type=int);p.add_argument('--start-y',type=int)
    args=p.parse_args();base.guard()
    if not 1<=args.seconds<=40 or ((args.start_x is None)!=(args.start_y is None)):raise base.ProbeError('INVALID_WATCH_PROBE_ARGUMENTS')
    if args.start_x is not None and not(0<=args.start_x<1021 and 0<=args.start_y<660):raise base.ProbeError('INVALID_GROUNDED_START')
    if not args.session and args.start_x is not None:raise base.ProbeError('NO_INPUT_COORDINATES_IN_OFFLINE_MODE')
    e=base.Evidence(args.prefix);began=time.monotonic();poll_end=began+args.seconds;hard_end=poll_end+4;worker=None
    result={'status':'QA_PHASE_CASE_PENDING','stop_attempted':False,'screen_watch_only':True,'physical_pose_acceptance':False,'native_session':bool(args.session)}
    try:
        if args.self_test:
            def example(a,b,c):
                words=[]
                for n,(name,value) in enumerate([('qa_step',a),('qa_scan_step',b),('qa_class',c)]):
                    words += [{'text':name,'x':8,'y':n*70,'w':180,'h':30},{'text':str(value),'x':VALUE_START+8,'y':n*70,'w':45,'h':30}]
                return {'lines':[{'words':words}]}
            assert parse_watch(example(30,30,2))['target_rows_matched']
            assert not parse_watch(example(50,50,2))['target_rows_matched']
            assert not parse_watch(example(30,30,3))['target_rows_matched']
            assert numeric_field('I')==1 and numeric_field('3O')==30 and numeric_field('Modify') is None
            assert base.canon('qa_I_step') not in EXPECTED
            bad=example(30,30,2);bad['lines'][0]['words'][1]['x']=100
            assert not parse_watch(bad)['target_rows_matched']
            try:base.guard(base.DEADLINE)
            except base.ProbeError:pass
            else:raise AssertionError('Cutoff not guarded')
            result['status']='OFFLINE_WATCH_PARSER_AND_CUTOFF_GUARDS_PASS';return
        worker=base.OcrWorker(e,hard_end)
        if args.offline_image:
            image=args.offline_image.resolve()
            if base.ROOT not in image.parents:raise base.ProbeError('OFFLINE_WATCH_IMAGE_OUTSIDE_AUDIT')
            original,_=e.save('offline-original.png',image.read_bytes())
            found=context(e,worker,original);rows,scan=observe(e,worker,original,0)
            result.update(status='OFFLINE_SAVED_WATCH_OBSERVED',watch_context=found,watch=rows,scan=scan,no_bridge_endpoints=True);return
        bridge=base.Bridge(json.loads(args.session.read_text(encoding='utf-8-sig')),e,hard_end)
        bridge.capture('before-input.png')
        if args.start_x is not None:bridge.click((args.start_x,args.start_y),'EXPLICIT_GROUNDED_PHYSICAL_START')
        bridge.click((809,34),'OPEN_REVIEWED_LOGIC_EDITOR')
        base.guard();time.sleep(.12)
        logic,_=bridge.capture('logic-open-before-watch-click.png')
        logic_text=worker.read(e.crop(logic,(0,70,650,130),'logic-context2x',2))
        if 'ladderlogicprogramming' not in base.canon(logic_text.get('text','')):
            raise base.ProbeError('ACTUAL_LOGIC_VIEW_CONTEXT_UNOBSERVED_NO_WATCH_CLICK')
        bridge.click((131,538),'REVIEWED_WATCH_TREE_DOUBLE_CLICK_1')
        bridge.click((131,538),'REVIEWED_WATCH_TREE_DOUBLE_CLICK_2')
        wheel(bridge,e)
        before,_=bridge.capture('watch-before-menu.png')
        if not context(e,worker,before):raise base.ProbeError('ACTUAL_MONITOR_VALUE_CONTEXT_UNOBSERVED_NO_STOP')
        rows,scan=observe(e,worker,before,0)
        if not initial_watch_qualified(rows):raise base.ProbeError('REQUIRED_WATCH_NAMES_OR_STEP_VALUES_UNOBSERVED_NO_STOP')
        bridge.click(base.MENU_CLICK,'ARM_REVIEWED_APP_PLAYBACK_MENU')
        armed,_=bridge.capture('watch-menu-armed.png')
        menu=worker.read(e.crop(armed,base.MENU_BOX,'menu2x'))
        if not stop_menu_text_observed(menu):
            raise base.ProbeError('STOP_MENU_TEXT_UNOBSERVED_NO_STOP')
        first_scan=None;progressed=False;number=1
        while time.monotonic()<poll_end:
            base.guard();image,captured=bridge.capture('observe-%03d.png'%number)
            rows,scan=observe(e,worker,image,number)
            if scan is not None:
                if first_scan is None:first_scan=scan
                elif scan>first_scan:progressed=True
            result.update(observations=number,menu_open_scan_progress_observed=progressed,last_watch=rows,last_scan=scan)
            age=time.monotonic()-captured
            if rows['target_rows_matched'] and progressed and age<=1:
                result['stop_attempted']=True
                bridge.click(base.STOP_CLICK,'SINGLE_STOP_ON_VISIBLE_QA_STEP30_QA_SCAN_STEP30_CLASS2',{'image':image.name,'age_seconds':age,'watch':rows,'scan':scan})
                immediate,_=bridge.capture('stop-immediate-watch.png');observe(e,worker,immediate,number+1)
                base.guard();time.sleep(min(1.5,max(0,hard_end-time.monotonic())))
                held,_=bridge.capture('stop-held-watch.png');observe(e,worker,held,number+2)
                result.update(status='TARGET_SCREEN_WATCH_ROWS_MATCHED_STOP_SENT_PHYSICAL_REVIEW_PENDING',match_image=image.name,target_watch=rows)
                return
            number+=1;time.sleep(min(.06,max(0,poll_end-time.monotonic())))
        result['status']='QA_PHASE_CASE_PENDING_TARGET_OR_PROGRESS_UNOBSERVED'
    except BaseException as exc:
        result.update(status='QA_PHASE_CASE_PENDING_PROBE_ENDED',error_code=str(exc) if isinstance(exc,base.ProbeError) else type(exc).__name__)
    finally:
        if worker is not None:result['owned_ocr_worker_returncode']=worker.close()
        result['elapsed_seconds']=round(time.monotonic()-began,3);e.finish(result)
        print(json.dumps({'status':result['status'],'stop_attempted':result['stop_attempted'],'physical_pose_acceptance':False,'folder':str(e.folder),'error_code':result.get('error_code'),'elapsed_seconds':result['elapsed_seconds']}))

if __name__=='__main__':
    def terminated(signum,frame):raise base.ProbeError('OWN_WATCH_PROBE_TERMINATED')
    if hasattr(signal,'SIGTERM'):signal.signal(signal.SIGTERM,terminated)
    main()
