import json,collections,shutil
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');full=json.loads((base/'public-toolchain-content/toolchain-map.json').read_text(encoding='utf8'));groups=collections.defaultdict(list)
for e in full['files']:
 if e['bytes']<=65536 and (base/'public-toolchain-content/blobs'/(e['sha256']+'.blob')).exists():groups[e['sha256']].append(e)
selected=[]
for sha,items in groups.items():
 if len(items)>=2:
  selected.extend(items[:2])
  if len(selected)==6:break
assert len(selected)==6
sample=base/'restore-content-rehearsal-bundle';(sample/'blobs').mkdir(parents=True,exist_ok=True)
for e in selected:shutil.copyfile(base/'public-toolchain-content/blobs'/(e['sha256']+'.blob'),sample/'blobs'/(e['sha256']+'.blob'))
(sample/'toolchain-map.json').write_text(json.dumps({'schema':1,'files':selected}),encoding='utf8');print(json.dumps({'sample_paths':6,'distinct_blobs':len(set(e['sha256']for e in selected))}))
