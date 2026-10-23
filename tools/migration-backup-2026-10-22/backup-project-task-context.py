import json,shutil,hashlib,datetime
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');source=Path('C:/Users/matt.winter/.codex/sessions');dest=base/'private-rungproof-task-context';dest.mkdir(exist_ok=True)
rows=[]
for p in source.rglob('*.jsonl'):
 try:
  with p.open(encoding='utf8')as f:meta=json.loads(f.readline())
  payload=meta.get('payload',{});cwd=payload.get('cwd','').replace('\\','/').lower();sid=payload.get('id','')
  if cwd!='c:/users/matt.winter/documents/rungproof'and sid!='01a161f0-5690-7783-a4ed-93dd4dee3195':continue
  target=dest/p.name
  # Copy exactly the bytes present when opened; a live transcript may append later.
  h=hashlib.sha256();n=0
  with p.open('rb')as src,target.open('wb')as out:
   limit=p.stat().st_size
   while n<limit:
    b=src.read(min(4*1024*1024,limit-n))
    if not b:break
    out.write(b);h.update(b);n+=len(b)
  check=hashlib.sha256(target.read_bytes()).hexdigest();assert check==h.hexdigest()and n==limit
  rows.append({'source':str(p),'backup':str(target.relative_to(base)),'bytes':n,'sha256':check,'session_id':sid,'snapshot_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()})
 except Exception as e:
  if 'payload' in locals()and payload.get('cwd','').lower().replace('\\','/')=='c:/users/matt.winter/documents/rungproof':raise
(base/'private-task-context-manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
print(json.dumps({'private_project_transcripts':len(rows),'bytes':sum(x['bytes']for x in rows),'all_sha256_verified':True}))
