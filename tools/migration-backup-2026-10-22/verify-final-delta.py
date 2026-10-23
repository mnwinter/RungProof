import json,subprocess,hashlib,concurrent.futures
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');root=Path('C:/Users/matt.winter/Documents/RungProof');dest=base/'RungProof'
# The bulk119398-file hash pass predates final handoff/script commits. Recheck
# every subsequent changed path, current Git metadata and installed skill.
rels=set(filter(None,subprocess.check_output(['git','diff','--name-only','-z','05a2efac1966953533af00ad782b38930464bb8b','HEAD'],cwd=root).decode().split('\0')))
rels.update(['AGENTS.md','PROJECT_INFORMATION.md','RUNGPROOF-MIGRATION-2026-10-22.md'])
items=[(root/r,dest/r,r)for r in rels if(root/r).is_file()]
items += [(p,dest/p.relative_to(root),p.relative_to(root).as_posix())for p in(root/'.git').rglob('*')if p.is_file()]
skill=Path('C:/Users/matt.winter/.codex/skills/rungproof-playtest-handoff');sd=base/'codex-skills/rungproof-playtest-handoff'
items += [(p,sd/p.relative_to(skill),'installed-skill/'+p.relative_to(skill).as_posix())for p in skill.rglob('*')if p.is_file()]
def digest(p):
 h=hashlib.sha256()
 with p.open('rb')as f:
  while b:=f.read(4*1024*1024):h.update(b)
 return h.hexdigest()
def verify(x):
 a,b,rel=x
 try:
  ha=digest(a);hb=digest(b);assert a.stat().st_size==b.stat().st_size and ha==hb
  return {'path':rel,'bytes':a.stat().st_size,'sha256':ha}
 except Exception as e:return {'path':rel,'error':str(e)or'hash_or_size_mismatch'}
with concurrent.futures.ThreadPoolExecutor(max_workers=6)as pool:rows=list(pool.map(verify,items))
errors=[x for x in rows if'error'in x];report={'result':'PASS'if not errors else'FAIL','files':len(rows),'errors':errors,'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'method':'Final changed-file/Git metadata/installed-skill hash delta following the full regular-file pass'}
(base/'verified-final-delta-sha256.json').write_text(json.dumps(rows,indent=2),encoding='utf8');(base/'verification-final-delta.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report))
if errors:raise SystemExit(1)
