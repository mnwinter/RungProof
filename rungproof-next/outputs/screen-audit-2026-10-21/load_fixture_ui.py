"""Load a disposable learner project through visible menus/file dialog only."""
import ctypes as c
from ctypes import wintypes as w
import hashlib,json,re,subprocess,sys,time,urllib.request
from pathlib import Path
from qa_input_transport import post_input,require_canvas
from PIL import Image
root=Path(__file__).parent
args=sys.argv[1:]
discard_workspace='--discard-owned-workspace' in args
args=[arg for arg in args if arg!='--discard-owned-workspace']
if len(args)!=3:raise ValueError('Expected URL scene_id filename [--discard-owned-workspace]')
url,scene_id,filename=args
rows=[]
for line in (root/'route-reconciliation.md').read_text(encoding='utf-8').splitlines():
    parts=[x.strip() for x in line.split('|')]
    if len(parts)==7 and parts[1].isdigit():rows.append(parts)
row=next(x for x in rows if x[5]==scene_id)
downs,label=int(row[2]),row[4]
u=c.windll.user32
def title():
    titles=[]
    @c.WINFUNCTYPE(w.BOOL,w.HWND,w.LPARAM)
    def visit(hwnd,_):
        pid=w.DWORD();u.GetWindowThreadProcessId(hwnd,c.byref(pid))
        if pid.value==44956 and u.IsWindowVisible(hwnd):
            text=c.create_unicode_buffer(512);u.GetWindowTextW(hwnd,text,512)
            if text.value.startswith('RungProof'):titles.append(text.value)
        return True
    u.EnumWindows(visit,0);return ' | '.join(titles)
def norm(text):return re.sub('[^a-z0-9]','',text.lower())
def send(data):
    raw=json.dumps(data).encode()
    result=post_input(url,raw,context={'helper':'load_fixture_ui','scene':scene_id})
    with (root/'fixture-ui-actions.jsonl').open('a') as log:log.write(json.dumps({'utc_ns':time.time_ns(),'scene':scene_id,'input':data,'delivery':result.decode()})+'\n')
def click(x,y):send({'x':x,'y':y});time.sleep(.2)
def capture(suffix):
    path=root/('fixture-'+scene_id+'-'+suffix+'.png')
    if path.exists() or path.with_suffix('.ocr.json').exists():
        path=path.with_name(path.stem+'-'+str(time.time_ns())+'.png')
    raw=urllib.request.urlopen(url+'/screen?t='+str(time.time_ns()),timeout=30).read();path.write_bytes(raw)
    out=path.with_suffix('.ocr.json')
    subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(path),'-OutputPath',str(out)],stdout=subprocess.DEVNULL,check=True)
    observed=json.loads(out.read_text(encoding='utf-8-sig'))
    observed['_evidence_image']=path.name
    with Image.open(path) as native:size=list(native.size)
    with (root/'qa-canvas-checks.jsonl').open('a') as log:
        log.write(json.dumps({'utc_ns':time.time_ns(),'context':{'helper':'load_fixture_ui','scene':scene_id,'stage':suffix},
            'image':path.name,'sha256':hashlib.sha256(raw).hexdigest(),
            'native_size':size,'required_size':[1600,900],'matched':size==[1600,900]})+'\n')
    if size!=[1600,900]:raise RuntimeError('Loader native canvas changed; no further input. Inspect '+str(path))
    if 'unsavedsceneworkspace' in norm(observed['text']) and not discard_workspace:
        raise RuntimeError('Unsaved scene workspace semantics not authorized for automatic discard; no further input. Inspect '+str(path))
    return observed
def exact_saved_scene_header(observed_frame):
    header_words=[word for line in observed_frame['lines'] for word in line['words']
                  if word['y']<75 and 800<=word['x']<=1225]
    header=' '.join(word['text'] for word in sorted(header_words,key=lambda word:(word['y']//20,word['x'])))
    header_match=norm(label) in norm(header)
    if norm(label) in norm(title()) and not header_match:
        # OCR fallback uses only the already captured native selected-scene frame.
        # Never take a new image or weaken exact normalized identity matching.
        native_path=root/observed_frame['_evidence_image']
        for scale in (2,3):
            crop=native_path.with_name(native_path.stem+'-header-'+str(scale)+'x-'+str(time.time_ns())+'.png')
            with Image.open(native_path) as shot:
                if shot.width<1225 or shot.height<75:
                    raise RuntimeError('Header crop outside native image; inspect '+str(native_path))
                shot.crop((800,0,1225,75)).resize((425*scale,75*scale)).save(crop)
            crop_out=crop.with_suffix('.ocr.json')
            subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(crop),'-OutputPath',str(crop_out)],stdout=subprocess.DEVNULL,check=True)
            retry=json.loads(crop_out.read_text(encoding='utf-8-sig'))
            header_match=norm(label) in norm(retry['text'])
            with (root/'fixture-ui-identity-checks.jsonl').open('a') as log:
                log.write(json.dumps({'utc_ns':time.time_ns(),'scene':scene_id,
                    'native_image':native_path.name,'native_sha256':hashlib.sha256(native_path.read_bytes()).hexdigest(),
                    'crop':crop.name,'crop_box':[800,0,1225,75],'scale':scale,
                    'crop_sha256':hashlib.sha256(crop.read_bytes()).hexdigest(),
                    'crop_ocr':crop_out.name,'crop_ocr_sha256':hashlib.sha256(crop_out.read_bytes()).hexdigest(),
                    'visible_header_text':retry['text'],'exact_normalized_header_match':header_match})+'\n')
            if header_match:break
    return header_match,header

def discard_owned_qa_dialog(observed,context):
    text=norm(observed['text'])
    if 'unsavedsceneworkspace' in text:
        if not discard_workspace or health['app_pid']!=44956:
            raise RuntimeError('Workspace discard requires explicit flag and isolated QA PID968')
        # This opt-in is solely for root-owned isolated test workspaces.
        # Require the exact visible modal title and its grounded button.
        modal=[line for line in observed['lines'] if norm(line['text'])=='unsavedsceneworkspace']
        buttons=[line for line in observed['lines'] if norm(line['text'])=='discardchanges' and line['words']]
        if len(modal)!=1 or len(buttons)!=1:
            raise RuntimeError('Exact workspace modal/title or Discard changes button ambiguous; no discard')
        words=buttons[0]['words']
        left=min(word['x'] for word in words);right=max(word['x']+word['w'] for word in words)
        top=min(word['y'] for word in words);bottom=max(word['y']+word['h'] for word in words)
        if not (450<=left<right<=1150 and 400<=top<bottom<=650):
            raise RuntimeError('Workspace discard button outside expected centered modal; no discard')
        native=root/observed['_evidence_image']
        with Image.open(native) as shot:
            if shot.size!=(1600,900):raise RuntimeError('Workspace discard canvas mismatch')
        x,y=round((left+right)/2),round((top+bottom)/2)
        with (root/'fixture-ui-dialog-actions.jsonl').open('a',encoding='utf-8') as log:
            log.write(json.dumps({'utc_ns':time.time_ns(),'context':context,'scene':scene_id,
                'pinned_app_pid':44956,'image':native.name,'sha256':hashlib.sha256(native.read_bytes()).hexdigest(),
                'explicit_flag':'--discard-owned-workspace','button_text':'Discard changes',
                'word_bounds':[left,top,right,bottom],'click':[x,y],
                'scope':'root-authorized isolated QA workspace only'})+'\n')
        click(x,y);time.sleep(.6)
        after=capture(context+'-after-workspace-discard')
        if 'unsavedsceneworkspace' in norm(after['text']) or 'unsavedladderwork' in norm(after['text']):
            raise RuntimeError('Workspace modal persisted or another modal appeared; no retry')
        return True
    if 'unsavedladderwork' not in text:return False
    # Authorization is limited to isolated pinned process968 and drafts
    # whose scene IDs are represented by our own prepared inputs/starters.
    if health['app_pid']!=44956:raise RuntimeError('Discard outside isolated QA process refused')
    owned={'lab-10-01-drive-alarm-code-string','lab-10-02-chicken-label-print','lab-10-04-motor-enum-state'}
    for project in (root/'test-projects').glob('*.rpproj.json'):
        try:identity=json.loads(project.read_text(encoding='utf-8-sig')).get('sourceSceneId')
        except (ValueError,OSError):continue
        if identity:owned.add(identity)
    owned_norm={norm(identity) for identity in owned}
    count=re.search(r'(\d+)\s+unsaved\s+ladder\s+project',observed['text'],re.IGNORECASE)
    identified=[line['text'] for line in observed['lines'] if norm(line['text']) in owned_norm]
    if not count or int(count.group(1))!=len(identified):
        raise RuntimeError('Unsaved dialog contains unrecognized/ambiguous draft identities; no discard. Inspect '+observed['_evidence_image'])
    candidates=[line for line in observed['lines'] if norm(line['text'])=='discardchanges']
    if len(candidates)!=1 or not candidates[0]['words']:
        raise RuntimeError('Exact Discard changes button not grounded; no discard. Inspect '+observed['_evidence_image'])
    words=candidates[0]['words'];left=min(w['x'] for w in words);right=max(w['x']+w['w'] for w in words)
    top=min(w['y'] for w in words);bottom=max(w['y']+w['h'] for w in words)
    x,y=round((left+right)/2),round((top+bottom)/2)
    native=root/observed['_evidence_image']
    with (root/'fixture-ui-dialog-actions.jsonl').open('a') as log:
        log.write(json.dumps({'utc_ns':time.time_ns(),'context':context,'scene':scene_id,
            'pinned_app_pid':44956,'recognized_drafts':identified,'image':native.name,
            'sha256':hashlib.sha256(native.read_bytes()).hexdigest(),
            'button_text':'Discard changes','word_bounds':[left,top,right,bottom],
            'click':[x,y],'scope':'root-authorized isolated QA learner drafts only'})+'\n')
    click(x,y);time.sleep(.6)
    after=capture(context+'-after-grounded-discard')
    if 'unsavedladderwork' in norm(after['text']):
        raise RuntimeError('Recognized unsaved dialog persisted after one grounded click; no blind retry. Inspect '+after['_evidence_image'])
    return True

health=json.loads(urllib.request.urlopen(url+'/health').read())
assert health['version'] in (5,6,7,8,9) and health['app_pid']==44956
require_canvas(url,{'helper':'load_fixture_ui','scene':scene_id,'stage':'before_any_input'})
# Confirm the native menu is visibly open before sending navigation keys.
# Focus/PID activation alone cannot prove that the app consumed a click.
for attempt in range(1,4):
    send({'key':'Escape'});time.sleep(.3)
    click(595,39);time.sleep(.6)
    menu=capture('scenario-menu-open-attempt-'+str(attempt))
    menu_lab_rows=[]
    for line in menu['lines']:
        words=[word for word in line['words']
               if 560<=word['x']<=860 and word['y']>=65]
        text=' '.join(word['text'] for word in sorted(words,key=lambda word:word['x']))
        if re.search(r'\blab\s*[0-9]',text,re.IGNORECASE):menu_lab_rows.append(text)
    if len(menu_lab_rows)>=3:break
else:
    raise RuntimeError('Scenario menu not visually confirmed after3 attempts; no navigation keys or project load sent. Inspect '+menu['_evidence_image'])
send({'key':'Home'});time.sleep(.3)
send({'keys':['Down']*downs});time.sleep(.6)
capture('scenario-target-highlight')
send({'key':'Return'});time.sleep(1.5)
# A target title can appear behind the modal; inspect the actual frame always.
selection_modal=capture('selection-modal-check')
discard_owned_qa_dialog(selection_modal,'selection')
if norm(label) not in norm(title()):
    observed=capture('navigation-intermediate')
    discard_owned_qa_dialog(observed,'navigation')
    deadline=time.monotonic()+12
    while norm(label) not in norm(title()):
        if time.monotonic()>deadline:raise RuntimeError('Wrong visible window title: '+title())
        time.sleep(.4)
time.sleep(.6)
selected=capture('selected-scene-identity')
header_match,header=exact_saved_scene_header(selected)
if norm(label) not in norm(title()) or not header_match:
    raise RuntimeError('Scene identity not confirmed by BOTH native window title and visible header after exact2x/3x OCR retry; project open/Run cancelled. title='+title()+' header='+header+' evidence='+selected['_evidence_image'])
if filename=='--viewer':
    if scene_id!='equipment-gallery':raise ValueError('Viewer-only selection is restricted to the equipment gallery')
    click(1265,39);time.sleep(.4)
    observed=capture('viewer-selected')
    print(json.dumps({'scene':scene_id,'result':'UI_SELECTED_VIEWER_ONLY; no controller Run or functional PASS','screen':observed['_evidence_image']}))
    raise SystemExit(0)
if filename!='--starter':
    click(369,39);click(477,129);time.sleep(.3)
    picker=capture('file-open-before-typing')
    if discard_owned_qa_dialog(picker,'file-open'):
        picker=capture('file-open-after-dialog')
    # Do not type a filesystem path into an unverified modal or the editor.
    picker_words=[word for line in picker['lines'] for word in line['words']]
    has_path=any(norm(word['text'])=='path' and 100<=word['y']<=190 for word in picker_words)
    has_file=any(norm(word['text'])=='file' and 660<=word['y']<=750 for word in picker_words)
    has_open=any(norm(word['text'])=='open' and 710<=word['y']<=800 for word in picker_words)
    if not (has_path and has_file and has_open):
        # Exact labels from tightly bounded, same-frame native picker regions.
        # Scaling is OCR assistance only, never a substitute for visible labels.
        native_path=root/picker['_evidence_image']
        checks={'path':has_path,'file':has_file,'open':has_open}
        regions={'path':(290,125,340,165),'file':(350,685,410,735),
                 'open':(545,730,625,780)}
        for key,box in regions.items():
            if checks[key]:continue
            for scale in (2,3):
                crop=native_path.with_name(native_path.stem+'-picker-'+key+'-'+str(scale)+'x-'+str(time.time_ns())+'.png')
                with Image.open(native_path) as shot:
                    if shot.size!=(1600,900):raise RuntimeError('Picker fallback native canvas mismatch; no typing')
                    shot.crop(box).resize(((box[2]-box[0])*scale,(box[3]-box[1])*scale)).save(crop)
                output=crop.with_suffix('.ocr.json')
                subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(crop),'-OutputPath',str(output)],stdout=subprocess.DEVNULL,check=True)
                retry=json.loads(output.read_text(encoding='utf-8-sig'))
                checks[key]=any(norm(word['text'])==key for line in retry['lines'] for word in line['words'])
                with (root/'fixture-ui-picker-checks.jsonl').open('a',encoding='utf-8') as log:
                    log.write(json.dumps({'utc_ns':time.time_ns(),'scene':scene_id,'native_image':native_path.name,
                        'native_sha256':hashlib.sha256(native_path.read_bytes()).hexdigest(),
                        'crop':crop.name,'crop_box':box,'scale':scale,
                        'crop_sha256':hashlib.sha256(crop.read_bytes()).hexdigest(),
                        'ocr':output.name,'ocr_sha256':hashlib.sha256(output.read_bytes()).hexdigest(),
                        'required_exact_label':key,'observed_text':retry['text'],'matched':checks[key]})+'\n')
                if checks[key]:break
        has_path,has_file,has_open=checks['path'],checks['file'],checks['open']
    if not (has_path and has_file and has_open):
        raise RuntimeError('Native file picker exact Path/File/Open not confirmed after same-frame2x/3x bounded OCR; no typing. Inspect '+picker['_evidence_image'])
    click(781,145);send({'key':'Ctrl+A'})
    send({'text':'/Users/matt.winter/Documents/RungProof/rungproof-next/outputs/screen-audit-2026-10-21/test-projects'})
    send({'key':'Return'});time.sleep(.3)
    click(642,711);send({'key':'Ctrl+A'});send({'text':filename})
    click(596,754);time.sleep(.8)
    capture('project-open')
else:
    capture('starter-entry')
click(52,884);time.sleep(.5);click(1265,39);time.sleep(.4)
observed=capture('running')
top_lines=[line['text'] for line in observed['lines'] if any(word['y']<75 for word in line['words'])]
if not any('localruntimerunning' in norm(line) for line in top_lines):
    raise RuntimeError('Run not confirmed by visible header: '+' | '.join(top_lines))
final_header_match,final_header=exact_saved_scene_header(observed)
if norm(label) not in norm(title()) or not final_header_match:
    raise RuntimeError('Scene identity changed during project load; success cancelled. Inspect '+observed['_evidence_image'])
print(json.dumps({'scene':scene_id,'fixture':filename,'result':'UI_LOADED_AND_RUNNING_ONLY','screen':observed['_evidence_image']}))
