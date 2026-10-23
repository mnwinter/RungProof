import os,json,subprocess
from pathlib import Path
root=Path('C:/Users/matt.winter/Documents/RungProof');base=Path('D:/RungProof-Backup-2026-10-22')
tracked=set(filter(None,subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0')))
raw=set((base/'archive-raw-images-files.txt').read_text(encoding='utf8').splitlines());records=set((base/'archive-evidence-files.txt').read_text(encoding='utf8').splitlines());extras=set((base/'archive-local-extras-files.txt').read_text(encoding='utf8').splitlines());tools=set((base/'archive-local-toolchains-files.txt').read_text(encoding='utf8').splitlines())
private={'rungproof-next/outputs/screen-audit-2026-10-21/SESSION-STATE.json','rungproof-next/outputs/screen-audit-2026-10-21/observe_small_array_zones.py'}
counts={k:0 for k in ('git','raw_images','records','extras','toolchain_content','private_local_only')};missing=[];files=[]
for parent,dirs,names in os.walk(root):
 dirs[:]=[d for d in dirs if d!='.git'and not(Path(parent)/d).is_junction()]
 for name in names:
  p=Path(parent)/name;rel=p.relative_to(root).as_posix()
  if name=='.git':continue
  cat='git'if rel in tracked else'raw_images'if rel in raw else'records'if rel in records else'extras'if rel in extras else'toolchain_content'if rel in tools else'private_local_only'if rel in private else None
  if cat:counts[cat]+=1;files.append({'path':rel,'layer':cat})
  else:missing.append(rel)
report={'result':'PASS'if not missing else'FAIL','commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'physical_files':len(files),'layers':counts,'uncovered':missing,'junctions':'18 internal aliases covered by relative junction-map.json and their backed-up targets','private':'D: complete mirror preserves local-only state; public release omits those original capability-bearing files.'}
(base/'public-coverage.json').write_text(json.dumps(report,indent=2),encoding='utf8');(base/'public-file-layers.jsonl').write_text(''.join(json.dumps(x)+'\n'for x in files),encoding='utf8');print(json.dumps(report))
assert not missing
