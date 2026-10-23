import subprocess,json
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22')
tracked=set(filter(None,subprocess.check_output(['git','ls-files','-z']).decode().split('\0')))
p=base/'archive-working-files-files.txt';items=p.read_text(encoding='utf8').splitlines();extras=[x for x in items if x not in tracked]
(base/'archive-local-extras-files.txt').write_text('\n'.join(extras)+'\n',encoding='utf8')
(base/'git-backed-working-files.txt').write_text('\n'.join(x for x in items if x in tracked)+'\n',encoding='utf8')
print(json.dumps({'working_files_in_git':len(items)-len(extras),'working_files_need_extra_archive':len(extras),'extra_bytes':sum(Path(x).stat().st_size for x in extras)}))
