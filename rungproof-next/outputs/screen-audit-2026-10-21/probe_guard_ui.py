"""Read native Health history after grounded UI clicks; no simulator APIs."""
import hashlib,json,re,subprocess,sys,time,urllib.request
from pathlib import Path
from PIL import Image
root=Path(__file__).parent
url,prefix,plan_name=sys.argv[1:4]
plan=json.loads((root/plan_name).read_text())
assert all(not (root/(prefix+'-'+s['name']+'-health.png')).exists() for s in plan)
def click(x,y):
    data=json.dumps({'x':x,'y':y}).encode()
    reply=urllib.request.urlopen(urllib.request.Request(url+'/input',data=data,headers={'Content-Type':'application/json'}),timeout=30).read()
    with (root/'guard-ui-actions.jsonl').open('a') as f:f.write(json.dumps({'utc_ns':time.time_ns(),'prefix':prefix,'input':[x,y],'delivery':reply.decode()})+'\n')
    time.sleep(.2)
for step in plan:
    click(*step['click'])
    click(422,39);click(485,125);click(1280,101)
    path=root/(prefix+'-'+step['name']+'-health.png')
    raw=urllib.request.urlopen(url+'/screen?t='+str(time.time_ns()),timeout=30).read();path.write_bytes(raw)
    crop=path.with_name(path.stem+'-history.png')
    with Image.open(path) as im:im.crop((1240,174,1595,255)).resize((710,162)).save(crop)
    out=crop.with_suffix('.ocr.json')
    subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(crop),'-OutputPath',str(out)],stdout=subprocess.DEVNULL,check=True)
    text=json.loads(out.read_text(encoding='utf-8-sig'))['text']
    normalized=re.sub('[^a-z0-9]','',text.lower())
    observed='nocommandbinding' in normalized and 'actionblocked' in normalized
    record={'prefix':prefix,'action':step['name'],'utc_ns':time.time_ns(),'screen':path.name,'sha256':hashlib.sha256(raw).hexdigest(),'visible_history':text,'ocr_guard_phrase_match':observed,'visual_review_required':True,'scope':'UI guard diagnostic only; OCR miss is not an absent event; false probe does not test normal process sequence'}
    with (root/'guard-ui-results.jsonl').open('a') as f:f.write(json.dumps(record)+'\n')
    print(json.dumps({'action':step['name'],'ocr_guard_phrase_match':observed,'visual_review_required':True}),flush=True)
    click(1265,39)
