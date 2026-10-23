import os,json,hashlib,time,concurrent.futures,sys
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22')
roots={
 'RungProof':Path('C:/Users/matt.winter/Documents/RungProof'),
 'legacy-PLC-Visual-Simulator':Path('C:/Users/matt.winter/Documents/PLC Visual Simulator'),
 'Godot-userdata-Python-view-RungProof':Path('C:/Users/matt.winter/AppData/Roaming/Godot/app_userdata/RungProof'),
 'thread-visualizations':Path('C:/Users/matt.winter/.codex/visualizations/2026/10/22/01a161f0-5690-7783-a4ed-93dd4dee3195'),
}
roots.update({k:Path(v)for k,v in json.loads((base/'supplemental-sources.json').read_text(encoding='utf8')).items()if k!='private-thread-context'})
if len(sys.argv)>1 and sys.argv[1]=='supplemental':roots.pop('RungProof')
def digest(p):
 h=hashlib.sha256()
 with p.open('rb')as f:
  while b:=f.read(4*1024*1024):h.update(b)
 return h.hexdigest()
def check(item):
 label,src,rel=item;dst=base/label/rel
 try:
  sb=src.stat();db=dst.stat()
  if sb.st_size!=db.st_size:return {'tree':label,'path':rel,'error':'size_mismatch','source_bytes':sb.st_size,'backup_bytes':db.st_size}
  a=digest(src);b=digest(dst)
  if a!=b:return {'tree':label,'path':rel,'error':'sha256_mismatch'}
  return {'tree':label,'path':rel,'bytes':sb.st_size,'sha256':a}
 except Exception as e:return {'tree':label,'path':rel,'error':str(e)}
items=[]
for label,root in roots.items():
 if not root.exists():continue
 for parent,dirs,files in os.walk(root):
  dirs[:]=[d for d in dirs if not (Path(parent)/d).is_junction()]
  for name in files:
   p=Path(parent)/name;items.append((label,p,p.relative_to(root).as_posix()))
name='supplemental'if len(sys.argv)>1 and sys.argv[1]=='supplemental'else'full'
errors=[];count=total=0;start=time.time()
with (base/f'verified-{name}-sha256.jsonl').open('w',encoding='utf8')as f,concurrent.futures.ThreadPoolExecutor(max_workers=6)as pool:
 for result in pool.map(check,items,buffersize=36):
  f.write(json.dumps(result)+'\n');count+=1;total+=result.get('bytes',0)
  if 'error'in result:errors.append(result)
  if count%10000==0:print(json.dumps({'checked':count,'GiB':round(total/2**30,2),'errors':len(errors)}),flush=True)
report={'result':'PASS'if not errors else'FAIL','files':count,'bytes':total,'seconds':round(time.time()-start,2),'errors':errors,'trees':list(roots),'method':'Every unique regular source file compared with D: copy by size and SHA-256; 18 internal junction aliases preserved separately in junction-map.json; no source deletion.'}
(base/f'verification-{name}.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report),flush=True)
if errors:sys.exit(1)
