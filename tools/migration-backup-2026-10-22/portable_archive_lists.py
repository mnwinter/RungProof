import json,os
from pathlib import Path
root=Path('C:/Users/matt.winter/Documents/RungProof');base=Path('D:/RungProof-Backup-2026-10-22')
raw=json.loads((base/'reparse-points.json').read_text(encoding='utf-8-sig'));mapping=[]
for row in raw:
 src=Path(row['FullName']);target=Path(row['Target']);a=src.relative_to(root).as_posix();b=target.relative_to(root).as_posix();mapping.append({'path':a,'target':b,'kind':'junction'})
p=root/'skills/rungproof-playtest-handoff/references/junction-map.json';p.write_text(json.dumps(mapping,indent=2)+'\n',encoding='utf8')
(base/'junction-map.json').write_text(json.dumps(mapping,indent=2)+'\n',encoding='utf8')
prefixes=[m['path']+'/'for m in mapping]
for cat in ('working-files','evidence','local-toolchains'):
 p=base/f'archive-{cat}-files.txt';items=p.read_text(encoding='utf8').splitlines();kept=[x for x in items if not any(x.startswith(pre)for pre in prefixes)and not(cat=='evidence'and Path(x).suffix.lower()in('.png','.jpg','.jpeg','.webp','.gif'))]
 if cat=='working-files':kept.append('skills/rungproof-playtest-handoff/references/junction-map.json')
 p.write_text('\n'.join(sorted(set(kept)))+'\n',encoding='utf8');print(json.dumps({'category':cat,'physical_archive_files':len(kept),'omitted_alias_or_raster_duplicates':len(items)-len(kept)}))
print(json.dumps({'junctions':len(mapping),'all_targets_inside_repository':True}))
