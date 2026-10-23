import json,collections,shutil
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');store=Path('C:/Users/matt.winter/Documents/RungProof-Migration-Content-Staging-20261022/public-toolchain-content');full=json.loads((store/'toolchain-map.json').read_text(encoding='utf8'));groups=collections.defaultdict(list)
for e in full['files']:
 if e['bytes']<=65536:groups[e['sha256']].append(e)
selected=[]
for sha,items in groups.items():
 if len(items)>=2 and (store/'blobs'/sha[:2]/(sha+'.blob')).exists():
  selected.extend(items[:2])
  if len(selected)==6:break
assert len(selected)==6
sample=base/'restore-content-sharded-rehearsal-bundle';sample.mkdir(exist_ok=True)
for e in selected:
 sha=e['sha256'];out=sample/'blobs'/sha[:2]/(sha+'.blob');out.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(store/'blobs'/sha[:2]/(sha+'.blob'),out)
(sample/'toolchain-map.json').write_text(json.dumps({'schema':1,'layout':'sha256-prefix2','files':selected}),encoding='utf8');print(json.dumps({'sample_paths':6,'distinct_blobs':len(set(e['sha256']for e in selected))}))
