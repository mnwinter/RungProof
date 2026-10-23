from pathlib import Path
import json
base=Path('D:/RungProof-Backup-2026-10-22');wanted=set((base/'archive-local-toolchains-files.txt').read_text(encoding='utf8').splitlines());rows=[];unique={}
with (base/'verified-full-sha256.jsonl').open(encoding='utf8')as f:
 for line in f:
  try:x=json.loads(line)
  except json.JSONDecodeError:break
  if x.get('tree')=='RungProof'and x['path']in wanted and 'sha256'in x:
   rows.append(x);unique.setdefault(x['sha256'],x)
print(json.dumps({'toolchain_rows_hashed_so_far':len(rows),'expected':len(wanted),'logical_GiB':round(sum(x['bytes']for x in rows)/2**30,3),'unique_GiB':round(sum(x['bytes']for x in unique.values())/2**30,3),'unique_files':len(unique)}))
