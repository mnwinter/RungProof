import subprocess,re,json
from pathlib import Path
patterns={
'private_key':re.compile(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
'github_token':re.compile(rb'\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,})\b'),
'openai_key':re.compile(rb'\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{30,}\b'),
'local_capability':re.compile(rb'http://(?:127\.0\.0\.1|localhost):\d{2,5}/[A-Za-z0-9_-]{24,}'),
}
modified=subprocess.check_output(['git','diff','--name-only','-z']).decode().split('\0')
okay=[];bad=[]
for p in filter(None,modified):
 raw=Path(p).read_bytes();hits=[k for k,v in patterns.items()if v.search(raw)]
 if hits:bad.append({'path':p,'reasons':hits})
 else:okay.append(p)
if okay:subprocess.run(['git','add','--pathspec-from-file=-','--pathspec-file-nul'],input=('\0'.join(okay)+'\0').encode(),check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
paths=list(filter(None,subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')))
print(json.dumps({'staged_files':len(paths),'modified_added':len(okay),'excluded_modified':bad,'application_source_changed':[x for x in paths if x.startswith(('rungproof-next/src/','rungproof-next/tests/','rungproof-next/scenes/'))]}))
