from pathlib import Path
p=Path('D:/RungProof-Backup-2026-10-22/backup-tools/build-final-metadata.py');s=p.read_text(encoding='utf-8-sig').replace("encoding='utf8-sig'","encoding='utf-8-sig'");p.write_text(s,encoding='utf8')
