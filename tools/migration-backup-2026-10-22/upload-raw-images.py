import hashlib,json,subprocess,sys
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');files=sorted((base/'archives').glob('rungproof-raw-images.7z.*'))
assert files
rows=[]
for p in files:
 h=hashlib.sha256()
 with p.open('rb')as f:
  while b:=f.read(4*1024*1024):h.update(b)
 rows.append({'name':p.name,'bytes':p.stat().st_size,'sha256':h.hexdigest()})
(base/'raw-image-archive-sha256.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
print(json.dumps({'archive_parts':len(files),'bytes':sum(x['bytes']for x in rows)}),flush=True)
subprocess.run([sys.executable,str(base/'backup-tools/gh_mnwinter.py'),'release','upload','backup-2026-10-22-laptop-migration','--repo','mnwinter/RungProof']+[str(x)for x in files],check=True)
print('RAW_IMAGE_UPLOAD_FINISHED',flush=True)
