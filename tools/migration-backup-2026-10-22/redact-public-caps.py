import re,shutil,json
from pathlib import Path
root=Path.cwd();base=Path('D:/RungProof-Backup-2026-10-22');kept=[]
for rel in ['rungproof-next/outputs/screen-audit-2026-10-21/backup-session-state-sanitized.json','rungproof-next/outputs/screen-audit-2026-10-21/refresh-backup-manifest.ps1']:
 p=root/rel;out=base/'private-local-state'/rel;out.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,out)
 raw=p.read_bytes();clean,n=re.subn(rb'(http://(?:127\.0\.0\.1|localhost):\d{2,5}/)[A-Za-z0-9_-]{24,}',rb'\1REDACTED',raw);assert n>0
 p.write_bytes(clean);kept.append({'path':rel,'redacted_local_capabilities':n,'original_backup':str(out)})
(base/'public-redactions.json').write_text(json.dumps(kept,indent=2),encoding='utf8')
p=base/'archive-working-files-files.txt';items=p.read_text(encoding='utf8').splitlines()+['skills/rungproof-playtest-handoff/scripts/restore-junctions.ps1','skills/rungproof-playtest-handoff/references/junction-map.json'];p.write_text('\n'.join(sorted(set(items)))+'\n',encoding='utf8')
p=base/'archive-evidence-files.txt';items=p.read_text(encoding='utf8').splitlines()+[x['path']for x in kept];p.write_text('\n'.join(sorted(set(items)))+'\n',encoding='utf8')
print(json.dumps({'redacted_files':len(kept),'raw_originals_preserved_on_D':True}))
