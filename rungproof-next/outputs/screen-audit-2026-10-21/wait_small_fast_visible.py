"""Wait for one exact point/value pair read from visible native Signals pixels."""
import json,sys,time,hashlib,subprocess,urllib.request,re
from pathlib import Path
root=Path(__file__).parent;u,prefix,point,value=sys.argv[1:5];limit=float(sys.argv[5]) if len(sys.argv)>5 else 120
end=time.monotonic()+limit;count=0
while time.monotonic()<end:
 h=json.load(urllib.request.urlopen(u+'/health'));assert h['app_pid']==44956 and h['version']==10 and h['native_client']==[1021,660]
 count+=1;image=root/(prefix+'-'+str(count)+'.png');raw=urllib.request.urlopen(u+'/screen?t='+str(time.time_ns())).read();image.write_bytes(raw)
 out=image.with_suffix('.ocr.json');subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(image),'-OutputPath',str(out)],stdout=subprocess.DEVNULL,check=True)
 words=[w for line in json.loads(out.read_text(encoding='utf-8-sig'))['lines'] for w in line['words'] if w['x']>=780 and w['y']>=90];rows=[]
 for word in sorted(words,key=lambda w:w['y']+w['h']/2):
  y=word['y']+word['h']/2
  if rows and abs(rows[-1][0]-y)<=4: rows[-1][1].append(word)
  else: rows.append((y,[word]))
 norm=lambda s:re.sub('[^a-z0-9]','',s.lower())
 texts=[' '.join(w['text'] for w in sorted(row,key=lambda w:w['x'])) for _,row in rows]
 matched=any(norm(t)==norm(point+value) for t in texts)
 event={'capture':image.name,'sha256':hashlib.sha256(raw).hexdigest(),'point':point,'expected':value,'matched':matched,'visible_rows':texts}
 with (root/(prefix+'-checks.jsonl')).open('a',encoding='utf-8') as log:log.write(json.dumps(event)+'\n')
 if matched: print(json.dumps({'result':'EXACT_VISIBLE_MATCH','capture':image.name,'point':point,'value':value,'checks':count}));break
 time.sleep(.1)
else: print(json.dumps({'result':'NO_VISIBLE_MATCH_WITHIN_WAIT','checks':count}));sys.exit(2)
