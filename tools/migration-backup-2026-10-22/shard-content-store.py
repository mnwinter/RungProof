from pathlib import Path
p=Path('D:/RungProof-Backup-2026-10-22/backup-tools/build-content-store.py');s=p.read_text(encoding='utf-8-sig')
s=s.replace("source=base/'RungProof';store=base/'public-toolchain-content'", "source=Path('C:/Users/matt.winter/Documents/RungProof');store=Path('C:/Users/matt.winter/Documents/RungProof-Migration-Content-Staging-20261022/public-toolchain-content')")
s=s.replace("'schema':1,'files':entries", "'schema':1,'layout':'sha256-prefix2','files':entries")
s=s.replace("dest=blobs/(x['sha256']+'.blob')", "dest=blobs/x['sha256'][:2]/(x['sha256']+'.blob');dest.parent.mkdir(parents=True,exist_ok=True)")
p.write_text(s,encoding='utf8')
