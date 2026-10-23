from pathlib import Path
from PIL import Image
import json,subprocess,re
root=Path('.')
found=[]
for line in Path('small-elevator-r1-upper-checks.jsonl').read_text(encoding='utf-8').splitlines():
 e=json.loads(line);p=Path(e['capture']);crop=p.with_name(p.stem+'-symbols-3x.png')
 with Image.open(p) as im: im.crop((780,112,1021,250)).resize((723,414)).save(crop)
 out=crop.with_suffix('.ocr.json');subprocess.run(['powershell.exe','-NoProfile','-File','ocr_screenshot.ps1','-ImagePath',str(crop.resolve()),'-OutputPath',str(out.resolve())],stdout=subprocess.DEVNULL,check=True)
 data=json.loads(out.read_text(encoding='utf-8-sig'));t=data['text'];n=re.sub('[^a-z0-9]','',t.lower())
 if 'atupperlandingtrue' in n:found.append({'original':str(p),'text':t,'sha256':e['sha256']})
Path('small-elevator-r1-upper-crop-review.json').write_text(json.dumps({'found':found,'original_frames':35},indent=2),encoding='utf-8')
print(json.dumps({'found_count':len(found),'originals':[x['original'] for x in found]}))
