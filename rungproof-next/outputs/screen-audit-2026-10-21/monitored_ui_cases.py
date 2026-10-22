"""Assert visible Signals text after grounded physical/UI clicks, no app APIs."""
import json,re,subprocess,sys,time,urllib.request
from PIL import Image
from pathlib import Path
from qa_input_transport import post_input,require_canvas
root=Path(__file__).parent
url,plan_name=sys.argv[1:3]
plan=json.loads((root/plan_name).read_text())
names=[case['name'] for case in plan]
assert len(names)==len(set(names)), 'Evidence case names must be unique before any input'
assert all(not (root/(name+suffix)).exists() for name in names for suffix in ('.png','.ocr.json')), 'Use fresh evidence names; existing captures must be preserved'
require_canvas(url,{'helper':'monitored_ui_cases','plan':plan_name,'stage':'before_any_input'})

def norm(value):return re.sub('[^a-z0-9]','',value.lower())
def visual_rows(ocr,min_x=0,min_y=0,tolerance=5):
    # OCR may split names and values into separate text columns. Rejoin only
    # words occupying the same visible pixel row, never infer a missing value.
    words=[w for line in ocr['lines'] for w in line['words'] if w['x']>=min_x and w['y']>=min_y]
    rows=[]
    for word in sorted(words,key=lambda w:w['y']+w['h']/2):
        center=word['y']+word['h']/2
        if rows and abs(center-rows[-1][0])<=tolerance:rows[-1][1].append(word)
        else:rows.append((center,[word]))
    return [' '.join(w['text'] for w in sorted(row,key=lambda w:w['x'])) for _,row in rows if norm(' '.join(w['text'] for w in row))]
def check_points(lines,expected):
    # Installed OCR reads the displayed zero glyph as O. Restrict that
    # correction to the value immediately following this numeric point name.
    visible={norm(line) for line in lines}
    wrapped={norm(' '.join(lines[i:i+n])) for i in range(len(lines)) for n in range(1,7)}
    return {point:((norm(point+str(value)) in visible) or
                  (isinstance(value,list) and norm(point+str(value)) in wrapped) or
                  # This UI font's lowercase l is sometimes OCR'd as I.
                  # Limit the alias to the known low_ point-name prefix.
                  (point.startswith('low_') and norm('I'+point[1:]+str(value)) in visible) or
                  (type(value) is int and value==0 and norm(point+'O') in visible))
            for point,value in expected.items()}
def send(data):
    raw=json.dumps(data).encode()
    result=post_input(url,raw,context={'helper':'monitored_ui_cases','plan':plan_name})
    with (root/'monitored-actions.jsonl').open('a') as log:log.write(json.dumps({'utc_ns':time.time_ns(),'plan':plan_name,'input':data,'delivery':result.decode()})+'\n')
def click(x,y):send({'x':x,'y':y});time.sleep(.2)
for case_index,case in enumerate(plan,1):
    if 'input' in case:send(case['input'])
    if 'click' in case:click(*case['click'])
    for xy in case.get('clicks',[]):click(*xy)
    time.sleep(case.get('wait',.3))
    # View menu stays anchored; the long Scenario popup can shift its top
    # entries outside the client when a later scene is selected.
    if not case.get('monitor_only'):
        click(422,39);click(485,125);click(1418,101);time.sleep(.25)
    path=root/(case['name']+'.png')
    path.write_bytes(urllib.request.urlopen(url+'/screen?t='+str(time.time_ns()),timeout=30).read())
    out=path.with_suffix('.ocr.json')
    subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(path),'-OutputPath',str(out)],stdout=subprocess.DEVNULL,check=True)
    ocr=json.loads(out.read_text(encoding='utf-8-sig'))
    lines=visual_rows(ocr,1230,140)
    visible=norm(' '.join(lines))
    results=check_points(lines,case.get('expected',{}))
    # OCR sometimes misses small tag labels. Retry only the actual visible
    # symbolic-points region at 2x scale; never infer values from the model.
    if not all(results.values()):
        crop=path.with_name(path.stem+'-points.png')
        with Image.open(path) as shot:
            shot.crop((1235,140,1600,795)).resize((730,1310)).save(crop)
        crop_out=crop.with_suffix('.ocr.json')
        subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(crop),'-OutputPath',str(crop_out)],stdout=subprocess.DEVNULL,check=True)
        retry=json.loads(crop_out.read_text(encoding='utf-8-sig'))
        visible=norm(retry['text'])
        retry_results=check_points(visual_rows(retry,tolerance=8),case.get('expected',{}))
        # Both OCR passes read the same native frame. Retain each successful
        # exact-line reading rather than losing it to a different OCR miss.
        results={point:matched or retry_results[point] for point,matched in results.items()}
        lines.append('Enlarged symbolic-points OCR: '+retry['text'])
    record={'case':case['name'],'image':path.name,'utc_ns':time.time_ns(),'expected':case.get('expected',{}),'visible_right_panel':lines,'checks':results,'scope':'Physical/UI input followed by visible Signals text; no blanket scene PASS'}
    with (root/'monitored-results.jsonl').open('a') as log:log.write(json.dumps(record)+'\n')
    if not all(results.values()) or case_index%5==0:
        print(json.dumps({'case':case['name'],'verified_points':sum(results.values()),'expected_points':len(results),'completed_cases':case_index}),flush=True)
    if not all(results.values()):raise RuntimeError('Visible point mismatch; inspect '+str(path))
    if not case.get('keep_monitor'):
        click(1265,39);time.sleep(.15)
print(json.dumps({'plan':plan_name,'completed_cases':len(plan),'result':'ALL_STATED_VISIBLE_CHECKS_MATCHED; empty expectations are observations only'}),flush=True)
