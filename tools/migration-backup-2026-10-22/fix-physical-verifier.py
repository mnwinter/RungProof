from pathlib import Path
p=Path('D:/RungProof-Backup-2026-10-22/backup-tools/verify_copies.py')
s=p.read_text(encoding='utf8').replace(" for parent,dirs,files in os.walk(root):\n  for name in files:"," for parent,dirs,files in os.walk(root):\n  dirs[:]=[d for d in dirs if not (Path(parent)/d).is_junction()]\n  for name in files:")
s=s.replace("'method':'Every source file compared against D: copy by size and SHA-256; no source deletion.'", "'method':'Every unique regular source file compared with D: copy by size and SHA-256; 18 internal junction aliases preserved separately in junction-map.json; no source deletion.'")
p.write_text(s,encoding='utf8')
