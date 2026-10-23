import os,json,re,time,collections,subprocess
from pathlib import Path
root=Path('C:/Users/matt.winter/Documents/RungProof');base=Path('D:/RungProof-Backup-2026-10-22')
(base/'archives').mkdir(exist_ok=True)
patterns={
'private_key':re.compile(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
'github_token':re.compile(rb'\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,})\b'),
'openai_key':re.compile(rb'\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{30,}\b'),
'aws_key':re.compile(rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b'),
'local_capability':re.compile(rb'http://(?:127\.0\.0\.1|localhost):\d{2,5}/[A-Za-z0-9_-]{24,}'),
}
binary_ext={'.exe','.dll','.pdb','.png','.jpg','.jpeg','.gif','.webp','.glb','.blend','.zip','.7z','.nupkg','.gz','.mp4','.mp3','.wav','.ttf','.woff','.woff2','.otf','.so','.a','.lib','.bin','.cache','.dat','.pack','.idx','.rdb','.rmeta','.svgz','.pck','.mesh','.ctex','.scn','.res'}
lists={k:[]for k in ('working-files','evidence','local-toolchains')};excluded=[];checked=0;total=0
for parent,dirs,files in os.walk(root):
 dirs[:]=[d for d in dirs if d!='.git']
 for name in files:
  p=Path(parent)/name;rel=p.relative_to(root).as_posix();s=p.stat();total+=1
  if name=='.git':excluded.append({'path':rel,'reason':'nested_git_pointer'});continue
  reasons=[]
  if name=='SESSION-STATE.json' or name.lower().endswith(('.pfx','.p12','.key')) or name.lower()in('.env','.netrc','.npmrc','.pypirc','credentials.json'):reasons.append('private_local_state_or_credential_filename')
  if p.suffix.lower()not in binary_ext and s.st_size<20_000_000:
   raw=p.read_bytes();checked+=1
   for pat,regex in patterns.items():
    if regex.search(raw):reasons.append(pat)
  if reasons:excluded.append({'path':rel,'reason':reasons});continue
  category='evidence' if rel.startswith('rungproof-next/outputs/')else 'local-toolchains' if rel.startswith(('rungproof-next/.tools/','build/.venv-rungproof/','rungproof-next/.godot/'))else 'working-files'
  lists[category].append(rel)
  if total%30000==0:print(json.dumps({'scanned':total,'text':checked}),flush=True)
for cat,items in lists.items():(base/f'archive-{cat}-files.txt').write_text('\n'.join(items)+'\n',encoding='utf8')
report={'total_files':total,'scanned_text':checked,'categories':{k:len(v)for k,v in lists.items()},'excluded_public':excluded,'local_copy':'All originals retained in complete D: mirror; .git retained locally and through repository remote.'}
(base/'public-archive-scan.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({'result':'SCAN_FINISHED','categories':report['categories'],'excluded':len(excluded)}),flush=True)
