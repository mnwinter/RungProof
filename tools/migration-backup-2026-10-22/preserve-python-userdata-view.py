from pathlib import Path
import shutil
p=Path('D:/RungProof-Backup-2026-10-22/backup-tools/verify_copies.py')
s=p.read_text(encoding='utf-8-sig').replace("'Godot-userdata-RungProof':Path(","'Godot-userdata-Python-view-RungProof':Path(")
p.write_text(s,encoding='utf8')
shutil.copytree('C:/Users/matt.winter/AppData/Roaming/Godot/app_userdata/RungProof','D:/RungProof-Backup-2026-10-22/Godot-userdata-Python-view-RungProof',dirs_exist_ok=True)
