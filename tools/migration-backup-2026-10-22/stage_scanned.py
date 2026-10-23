import json,pathlib,subprocess
root=pathlib.Path.cwd();out=pathlib.Path('D:/RungProof-Backup-2026-10-22')
files=json.loads((out/'public-stage-candidates.json').read_text(encoding='utf8'))
# Include all scanned new text, fixture, diagnostic source, and skill files. Raw raster evidence is archived separately.
extra=['AGENTS.md','PROJECT_INFORMATION.md','RUNGPROOF-MIGRATION-2026-10-22.md']
files=sorted(set(files+extra))
subprocess.run(['git','add','--pathspec-from-file=-','--pathspec-file-nul'],input=('\0'.join(files)+'\0').encode(),check=True)
print(json.dumps({'staged_new_text_and_docs':len(files)}))
