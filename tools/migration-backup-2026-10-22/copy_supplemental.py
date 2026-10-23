import shutil,json
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');sources={
'codex-skills/rungproof-playtest-handoff':Path('C:/Users/matt.winter/.codex/skills/rungproof-playtest-handoff'),
'codex-skills/rungproof-development':Path('C:/Users/matt.winter/Documents/RungProof/skills/rungproof-development'),
'codex-skills/plc-tia-wincc':Path('C:/Users/matt.winter/.codex/skills/plc-tia-wincc'),
'codex-skills/handoff':Path('C:/Users/matt.winter/.codex/skills/handoff'),
'codex-skills/skill-creator':Path('C:/Users/matt.winter/.codex/skills/.system/skill-creator'),
}
for rel,src in sources.items():shutil.copytree(src,base/rel,dirs_exist_ok=True)
(base/'private-thread-context').mkdir(exist_ok=True)
src=Path('C:/Users/matt.winter/.codex/sessions/2026/10/21/rollout-2026-10-21T21-48-23-01a161f0-5690-7783-a4ed-93dd4dee3195.jsonl');shutil.copy2(src,base/'private-thread-context'/src.name)
(base/'supplemental-sources.json').write_text(json.dumps({k:str(v)for k,v in sources.items()}|{'private-thread-context':str(src)},indent=2),encoding='utf8')
print(json.dumps({'skills':len(sources),'thread_context_bytes':src.stat().st_size}))
