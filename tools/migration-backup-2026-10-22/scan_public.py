import subprocess,pathlib,re,json,collections
root=pathlib.Path.cwd(); files=subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z']).decode().split('\0')
patterns={
 'private_key':re.compile(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
 'github_token':re.compile(rb'\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,})\b'),
 'openai_key':re.compile(rb'\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{30,}\b'),
 'aws_key':re.compile(rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b'),
 'local_capability':re.compile(rb'http://(?:127\.0\.0\.1|localhost):\d{2,5}/[A-Za-z0-9_-]{24,}'),
}
flagged=[];good=[]
for rel in files:
 if not rel:continue
 p=root/rel
 if p.suffix.lower()in('.png','.jpg','.jpeg','.gif','.zip','.7z','.dll','.exe','.pdb'):continue
 raw=p.read_bytes();hits=[name for name,pat in patterns.items()if pat.search(raw)]
 if p.name=='SESSION-STATE.json':hits.append('session_state')
 if hits:flagged.append({'path':rel,'reasons':hits})
 else:good.append(rel)
out=pathlib.Path('D:/RungProof-Backup-2026-10-22')
(out/'public-stage-candidates.json').write_text(json.dumps(good,indent=2),encoding='utf8')
(out/'public-content-scan.json').write_text(json.dumps({'checked_text':len(good)+len(flagged),'clean':len(good),'flagged':flagged},indent=2),encoding='utf8')
print(json.dumps({'clean_text_files':len(good),'flagged_count':len(flagged),'flagged_names':flagged[:12]}))
