from pathlib import Path
p=Path('D:/RungProof-Backup-2026-10-22/backup-tools/upload_supplemental_archives.py')
s=p.read_text(encoding='utf-8-sig').replace("'rungproof-local-toolchains.7z.'","'rungproof-toolchain-content.7z.'").replace('max_workers=2','max_workers=6')
s=s.replace("return json.loads(subprocess.check_output(['gh','api',f'repos/mnwinter/RungProof/releases/{release_id}'],env=env,text=True))", "pages=json.loads(subprocess.check_output(['gh','api',f'repos/mnwinter/RungProof/releases/{release_id}/assets?per_page=100','--paginate','--slurp'],env=env,text=True));return {'assets':[asset for page in pages for asset in page]}")
p.write_text(s,encoding='utf8')
