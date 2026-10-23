"""Breadth pass using OS UI navigation and pixel evidence only. No app APIs.

This records visits and visible Run behavior, not full functional passes.
Run bounded batches by zero-based route offsets, e.g. 0 10.
"""
import ctypes as c
from ctypes import wintypes as w
import hashlib,json,re,subprocess,sys,time,urllib.request
from pathlib import Path
root=Path(__file__).parent
url=sys.argv[1]
start,count=map(int,sys.argv[2:4])
routes=[]
for line in (root/'route-reconciliation.md').read_text(encoding='utf-8').splitlines():
    parts=[x.strip() for x in line.split('|')]
    if len(parts)==7 and parts[1].isdigit():
        routes.append({'ordinal':int(parts[1]),'downs':int(parts[2]),'group':parts[3],'label':parts[4],'id':parts[5]})
u=c.windll.user32
def title():
    result=[]
    @c.WINFUNCTYPE(w.BOOL,w.HWND,w.LPARAM)
    def visit(hwnd,_):
        pid=w.DWORD();u.GetWindowThreadProcessId(hwnd,c.byref(pid))
        if pid.value==36352 and u.IsWindowVisible(hwnd):
            text=c.create_unicode_buffer(512);u.GetWindowTextW(hwnd,text,512)
            if text.value.startswith('RungProof'):result.append(text.value)
        return True
    u.EnumWindows(visit,0)
    return ' | '.join(result)
def send(data):
    raw=json.dumps(data).encode()
    response=urllib.request.urlopen(urllib.request.Request(url+'/input',data=raw,headers={'Content-Type':'application/json'}),timeout=30).read()
    with (root/'breadth-actions.jsonl').open('a',encoding='utf-8') as log:
        log.write(json.dumps({'utc_ns':time.time_ns(),'input':data,'delivery':response.decode()})+'\n')
def click(x,y):send({'x':x,'y':y});time.sleep(.25)
def capture(name):
    raw=urllib.request.urlopen(url+'/screen?t='+str(time.time_ns()),timeout=30).read()
    path=root/(name+'.png');path.write_bytes(raw)
    out=root/(name+'.ocr.json')
    subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(path),'-OutputPath',str(out)],stdout=subprocess.DEVNULL,check=True)
    ocr=json.loads(out.read_text(encoding='utf-8-sig'))
    return {'file':path.name,'sha256':hashlib.sha256(raw).hexdigest(),'text':ocr['text']}
def norm(text):return re.sub('[^a-z0-9]','',text.lower())
health=json.loads(urllib.request.urlopen(url+'/health',timeout=5).read())
assert health['version']==4 and health['app_pid']==36352,health
for route in routes[start:start+count]:
    print('OPEN '+route['id'],flush=True)
    send({'key':'Escape'})
    click(595,39)
    send({'keys':['Home']+['Down']*route['downs']+['Return']})
    deadline=time.monotonic()+15
    while norm(route['label']) not in norm(title()):
        if time.monotonic()>deadline:
            capture('breadth-navigation-unexpected-'+route['id'])
            raise RuntimeError('Navigation did not reach '+route['label']+'; actual '+title())
        time.sleep(.4)
    time.sleep(.8)
    entry=capture('breadth-'+route['id']+'-entry')
    click(52,884)
    time.sleep(.7)
    run=capture('breadth-'+route['id']+'-run')
    # Return to a known product view before testing app playback controls.
    click(1265,39)
    click(131,884)
    click(215,884)
    time.sleep(.4)
    reset=capture('breadth-'+route['id']+'-reset')
    record={**route,'utc_ns':time.time_ns(),'status':'VISITED_REQUIRES_REVIEW','scope':'Menu entry, visible Run attempt, app Stop and app Reset only; no blanket functional pass','entry':entry,'run':run,'reset':reset,'native_window_title':title()}
    with (root/'breadth-results.jsonl').open('a',encoding='utf-8') as log:log.write(json.dumps(record)+'\n')
    print('CAPTURED '+route['id']+'; '+run['text'][:110],flush=True)
