"""Screen-only QA at the current fully visible small native client; no app state APIs."""
import json,sys,time,hashlib,urllib.request
from pathlib import Path
root=Path(__file__).parent
url=sys.argv[1];plan=json.loads((root/sys.argv[2]).read_text(encoding='utf-8-sig'))
h=json.load(urllib.request.urlopen(url+'/health'));assert h['version']==10 and h['app_pid']==44956 and h['native_client']==[1021,660]
# Actual unoccluded native capture must succeed before any plan input.
urllib.request.urlopen(url+'/screen?t='+str(time.time_ns())).read()
for step in plan:
 event={'utc_ns':time.time_ns(),'plan':sys.argv[2],'step':step,'client':[1021,660]}
 if 'input' in step:
  h=json.load(urllib.request.urlopen(url+'/health'));assert h['app_pid']==44956 and h['native_client']==[1021,660]
  event['delivery']=urllib.request.urlopen(urllib.request.Request(url+'/input',data=json.dumps(step['input']).encode(),headers={'Content-Type':'application/json'})).read().decode()
 time.sleep(step.get('wait',.3))
 if 'capture' in step:
  path=root/(step['capture']+'.png');assert not path.exists(),'Never overwrite evidence'
  raw=urllib.request.urlopen(url+'/screen?t='+str(time.time_ns())).read();path.write_bytes(raw);event['sha256']=hashlib.sha256(raw).hexdigest()
 with (root/'postbackup-small-ui-actions.jsonl').open('a',encoding='utf-8') as log:log.write(json.dumps(event)+'\n')
print(json.dumps({'plan':sys.argv[2],'steps':len(plan),'result':'OS_INPUT_AND_NATIVE_CAPTURE_ONLY'}))
