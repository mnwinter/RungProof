from pathlib import Path
p=Path('D:/RungProof-Backup-2026-10-22/backup-tools/verify-remote-assets.py');s=p.read_text(encoding='utf-8-sig').replace("env['GH_TOKEN']=token","env['GH_TOKEN']=token;env['GH_HOST']='github.com'");p.write_text(s,encoding='utf8')
