"""Root-invoked exact-label Scenario selection from actual native pixels only.

Start with the Scenario menu CLOSED. Never dismiss a modal, Run, or edit.
Agent validation uses saved PNGs/offline parsers only; root owns live inputs.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import signal
import time
import parcel_handoff_probe as base

POPUP=(252,0,508,660)
SCALE=2
OPEN=(281,34)
WHEEL=(379,253)
MAX_OBSERVATIONS=6
FRESH_SECONDS=1.5

def exact_rows(ocr,label):
    """Join OCR fragments on the same physical row; match the whole row only."""
    width,height=(POPUP[2]-POPUP[0])*SCALE,(POPUP[3]-POPUP[1])*SCALE
    if (ocr.get('width'),ocr.get('height'))!=(width,height):
        raise base.ProbeError('SCENARIO_OCR_CROP_DIMENSIONS_CHANGED')
    words=[w for line in ocr.get('lines',[]) for w in line.get('words',[])]
    rows=[]
    for word in sorted(words,key=lambda w:(w.get('y',-1)+w.get('h',0)/2,w.get('x',-1))):
        values=[word.get(k) for k in ('x','y','w','h')]
        if not all(isinstance(v,(int,float)) and math.isfinite(v) for v in values):
            raise base.ProbeError('SCENARIO_WORD_COORDINATES_INVALID')
        x,y,w,h=values
        if not(0<=x<x+w<=width and 0<=y<y+h<=height):
            raise base.ProbeError('SCENARIO_WORD_OUTSIDE_POPUP')
        center=y+h/2
        row=next((r for r in rows if abs(r['anchor_y']-center)<=8),None)
        if row is None:row={'anchor_y':center,'words':[]};rows.append(row)
        row['words'].append(word)
    matches=[]
    for row in rows:
        ordered=sorted(row['words'],key=lambda w:w['x'])
        text=' '.join(w['text'] for w in ordered)
        if base.canon(text)!=base.canon(label):continue
        left=min(w['x'] for w in ordered);right=max(w['x']+w['w'] for w in ordered)
        top=min(w['y'] for w in ordered);bottom=max(w['y']+w['h'] for w in ordered)
        click=[round(POPUP[0]+(left+right)/2/SCALE),round(POPUP[1]+(top+bottom)/2/SCALE)]
        if not(POPUP[0]<click[0]<POPUP[2] and POPUP[1]<click[1]<POPUP[3]):
            raise base.ProbeError('SCENARIO_ROW_CENTROID_OUTSIDE_POPUP')
        matches.append({'text':text,'words':ordered,'crop_word_union':[left,top,right,bottom],
                        'native_click':click,'native_popup':list(POPUP),'scale':SCALE})
    return matches

def wheel(bridge,evidence,delta):
    bridge.health();base.guard()
    payload={'x':WHEEL[0],'y':WHEEL[1],'phase':'wheel','delta':delta}
    evidence.event('OS_WHEEL_ATTEMPT',purpose='SCENARIO_POPUP_ONLY',input=payload)
    raw=bridge.request('/input',payload)
    evidence.event('OS_WHEEL_DELIVERED',purpose='SCENARIO_POPUP_ONLY',delivery_sha256=hashlib.sha256(raw).hexdigest())

def click_fresh(bridge,evidence,row,image,captured):
    # Check freshness AFTER pinned-session health, immediately before input.
    bridge.health();base.guard();age=time.monotonic()-captured
    if age>FRESH_SECONDS:return False
    x,y=row['native_click']
    evidence.event('OS_CLICK_ATTEMPT',purpose='UNIQUE_EXACT_VISIBLE_SCENARIO_ROW',x=x,y=y,
                   app_pid=bridge.pid,witness={'image':image.name,'age_seconds':age,'row':row},
                   note='one request; no retry of uncertain selection')
    raw=bridge.request('/input',{'x':x,'y':y})
    evidence.event('OS_CLICK_DELIVERED',purpose='UNIQUE_EXACT_VISIBLE_SCENARIO_ROW',delivery_sha256=hashlib.sha256(raw).hexdigest())
    return True

def main():
    p=argparse.ArgumentParser(description=__doc__)
    mode=p.add_mutually_exclusive_group(required=True)
    mode.add_argument('--session',type=Path);mode.add_argument('--offline-image',type=Path);mode.add_argument('--self-test',action='store_true')
    p.add_argument('--prefix',default='scene-menu-select');p.add_argument('--label',required=True)
    args=p.parse_args();base.guard()
    if not base.canon(args.label) or len(args.label)>180:raise base.ProbeError('INVALID_EXACT_SCENARIO_LABEL')
    e=base.Evidence(args.prefix);began=time.monotonic();hard_end=began+22;worker=None
    result={'status':'SCENARIO_LABEL_UNOBSERVED','label':args.label,'selection_sent':False,
            'screen_only':True,'native_session':bool(args.session),'scene_loaded_proven':False,
            'required_precondition':'Scenario menu closed at invocation'}
    try:
        if args.self_test:
            def sample(text,y=140):return {'text':text,'x':20,'y':y,'w':280,'h':24}
            make=lambda words:{'width':512,'height':1320,'lines':[{'words':words}]}
            assert len(exact_rows(make([sample(args.label)]),args.label))==1
            assert not exact_rows(make([sample(args.label+' extra')]),args.label)
            assert len(exact_rows(make([sample(args.label),sample(args.label,180)]),args.label))==2
            try:base.guard(base.DEADLINE)
            except base.ProbeError:pass
            else:raise AssertionError('Cutoff not guarded')
            result.update(status='OFFLINE_EXACT_LABEL_DUPLICATE_AND_CUTOFF_GUARDS_PASS',no_bridge_endpoints=True);return
        worker=base.OcrWorker(e,hard_end)
        if args.offline_image:
            source=args.offline_image.resolve()
            if base.ROOT not in source.parents or not source.is_file():raise base.ProbeError('OFFLINE_IMAGE_OUTSIDE_AUDIT')
            image,_=e.save('offline-original.png',source.read_bytes())
            crop=e.crop(image,POPUP,'scenario-popup2x',SCALE)
            matches=exact_rows(worker.read(crop),args.label)
            result.update(status='OFFLINE_SAVED_SCENARIO_MENU_OBSERVED',matches=matches,unique_exact_match=len(matches)==1,no_bridge_endpoints=True);return
        bridge=base.Bridge(json.loads(args.session.read_text(encoding='utf-8-sig')),e,hard_end)
        bridge.capture('before-menu-input.png')
        bridge.click(OPEN,'OPEN_REVIEWED_SCENARIO_MENU_FROM_CLOSED')
        wheel(bridge,e,1200);wheel(bridge,e,1200)
        for number in range(MAX_OBSERVATIONS):
            base.guard()
            image,captured=bridge.capture('menu-observe-%02d.png'%number)
            matches=exact_rows(worker.read(e.crop(image,POPUP,'scenario-popup2x',SCALE)),args.label)
            e.event('EXACT_VISIBLE_LABEL_OBSERVATION',image=image.name,number=number,matches=matches)
            result.update(observations=number+1,last_matches=matches)
            if len(matches)>1:
                result['status']='DUPLICATE_EXACT_VISIBLE_LABEL_NO_SELECTION';return
            if len(matches)==1 and click_fresh(bridge,e,matches[0],image,captured):
                result.update(status='EXACT_LABEL_CLICK_SENT_SELECTED_OR_MODAL_REVIEW_PENDING',selection_sent=True,match_image=image.name)
                bridge.capture('after-selection-or-modal.png');return
            if number+1<MAX_OBSERVATIONS and time.monotonic()<hard_end:wheel(bridge,e,-600)
    except BaseException as exc:
        result.update(status='SCENARIO_SELECTION_UNOBSERVED_PROBE_ENDED',error_code=str(exc) if isinstance(exc,base.ProbeError) else type(exc).__name__)
    finally:
        if worker is not None:result['owned_ocr_worker_returncode']=worker.close()
        result['elapsed_seconds']=round(time.monotonic()-began,3);e.finish(result)
        print(json.dumps({'status':result['status'],'selection_sent':result['selection_sent'],'scene_loaded_proven':False,
                          'folder':str(e.folder),'error_code':result.get('error_code'),'elapsed_seconds':result['elapsed_seconds']}))

if __name__=='__main__':
    def terminated(signum,frame):raise base.ProbeError('OWN_SCENARIO_PROBE_TERMINATED')
    if hasattr(signal,'SIGTERM'):signal.signal(signal.SIGTERM,terminated)
    main()
