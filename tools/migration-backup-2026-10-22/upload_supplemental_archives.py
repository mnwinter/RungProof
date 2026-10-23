import os,subprocess,json,sys,time,hashlib,concurrent.futures
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');tag='backup-2026-10-22-laptop-migration';release_id=421391212
raw=subprocess.check_output(['gh','auth','token','--hostname','github.com','--user','mnwinter'],text=True).strip();env=os.environ.copy();env['GH_TOKEN']=raw;env['GH_HOST']='github.com'
assert subprocess.check_output(['gh','api','user','--jq','.login'],env=env,text=True).strip()=='mnwinter'
files=[]
for prefix in ('rungproof-toolchain-content.7z.','rungproof-evidence-records.7z.','rungproof-local-extras.7z.'):
 files += [p for p in sorted((base/'archives').glob(prefix+'*'))if p.suffix[1:].isdigit()]
assert files
rows=[]
for p in files:
 h=hashlib.sha256()
 with p.open('rb')as f:
  while b:=f.read(4*1024*1024):h.update(b)
 rows.append({'name':p.name,'bytes':p.stat().st_size,'sha256':h.hexdigest()})
(base/'supplemental-archive-sha256.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
def remote():
 pages=json.loads(subprocess.check_output(['gh','api',f'repos/mnwinter/RungProof/releases/{release_id}/assets?per_page=100','--paginate','--slurp'],env=env,text=True));return {'assets':[asset for page in pages for asset in page]}
def upload(p):
 for attempt in range(1,4):
  prior=next((a for a in remote()['assets']if a['name']==p.name),None)
  if prior and prior['state']=='uploaded'and prior['size']==p.stat().st_size:return {'name':p.name,'result':'ALREADY_UPLOADED'}
  if prior:raise RuntimeError('Existing incomplete or different asset requires explicit inspection: '+p.name)
  print(json.dumps({'upload_begin':p.name,'attempt':attempt,'bytes':p.stat().st_size}),flush=True)
  run=subprocess.run(['gh','release','upload',tag,str(p),'--repo','mnwinter/RungProof'],env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
  if run.returncode==0:return {'name':p.name,'result':'UPLOADED'}
  print(json.dumps({'upload_attempt_failed':p.name,'attempt':attempt,'error':run.stderr[-500:]}),flush=True)
  time.sleep(attempt*5)
 raise RuntimeError('Upload retries exhausted: '+p.name)
with concurrent.futures.ThreadPoolExecutor(max_workers=6)as pool:
 for result in pool.map(upload,files):print(json.dumps(result),flush=True)
(base/'remote-release-after-supplemental.json').write_text(json.dumps(remote(),indent=2),encoding='utf8')
print('SUPPLEMENTAL_ARCHIVE_UPLOAD_FINISHED',flush=True)
