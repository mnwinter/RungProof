import json,shutil,hashlib,concurrent.futures,time
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');source=Path('C:/Users/matt.winter/Documents/RungProof');store=Path('C:/Users/matt.winter/Documents/RungProof-Migration-Content-Staging-20261022/public-toolchain-content');blobs=store/'blobs';blobs.mkdir(parents=True,exist_ok=True)
wanted=set((base/'archive-local-toolchains-files.txt').read_text(encoding='utf8').splitlines());entries=[];unique={}
for line in (base/'verified-full-sha256.jsonl').read_text(encoding='utf8').splitlines():
 x=json.loads(line)
 if x.get('tree')=='RungProof'and x['path']in wanted:
  assert 'error'not in x;entry={k:x[k]for k in ('path','bytes','sha256')};entries.append(entry);unique.setdefault(x['sha256'],entry)
assert len(entries)==len(wanted)
(store/'toolchain-map.json').write_text(json.dumps({'schema':1,'layout':'sha256-prefix2','files':entries,'original_paths':len(entries),'unique_blobs':len(unique)},separators=(',',':')),encoding='utf8')
def copy_verify(x):
 src=source/x['path'];dest=blobs/x['sha256'][:2]/(x['sha256']+'.blob');dest.parent.mkdir(parents=True,exist_ok=True)
 if not dest.exists():shutil.copyfile(src,dest)
 h=hashlib.sha256()
 with dest.open('rb')as f:
  while b:=f.read(4*1024*1024):h.update(b)
 if dest.stat().st_size!=x['bytes']or h.hexdigest()!=x['sha256']:raise ValueError('Blob verification failed: '+x['path'])
 return x['bytes']
count=total=0
with concurrent.futures.ThreadPoolExecutor(max_workers=6)as pool:
 for size in pool.map(copy_verify,unique.values(),buffersize=36):
  count+=1;total+=size
  if count%5000==0:print(json.dumps({'verified_blobs':count,'GiB':round(total/2**30,2)}),flush=True)
report={'result':'PASS','paths':len(entries),'unique_blobs':len(unique),'unique_bytes':total,'expanded_bytes':sum(x['bytes']for x in entries),'all_blobs_sha256_verified':True}
(base/'verification-content-store.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report),flush=True)
