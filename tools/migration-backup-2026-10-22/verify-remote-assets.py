import json,os,subprocess,hashlib,sys
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');rid=421391212
token=subprocess.check_output(['gh','auth','token','--hostname','github.com','--user','mnwinter'],text=True).strip();env=os.environ.copy();env['GH_TOKEN']=token;env['GH_HOST']='github.com'
pages=json.loads(subprocess.check_output(['gh','api',f'repos/mnwinter/RungProof/releases/{rid}/assets?per_page=100','--paginate','--slurp'],env=env,text=True));assets={a['name']:a for page in pages for a in page}
rows=json.loads((base/'raw-image-archive-sha256.json').read_text(encoding='utf8'))+json.loads((base/'supplemental-archive-sha256.json').read_text(encoding='utf8'))
if '--metadata'in sys.argv:
 for name in ['SHA256SUMS','ARCHIVE-INDEX.json','RESTORE-PUBLIC.md','BACKUP-VERIFICATION-SUMMARY.json','public-coverage.json','public-archive-scan.json','public-redactions.json']:
  p=base/name;rows.append({'name':name,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
problems=[];matched=[]
for x in rows:
 a=assets.get(x['name'])
 if not a:problems.append({'name':x['name'],'error':'missing'});continue
 if a['state']!='uploaded'or a['size']!=x['bytes']or a.get('digest')!='sha256:'+x['sha256']:problems.append({'name':x['name'],'error':'server_size_state_or_digest_mismatch','state':a['state'],'bytes':a['size'],'digest':a.get('digest')})
 else:matched.append(x['name'])
report={'result':'PASS'if not problems else'FAIL','expected_assets':len(rows),'verified_assets':len(matched),'bytes':sum(x['bytes']for x in rows),'problems':problems,'method':'GitHub uploaded state, byte length and server-provided SHA256 digest matched against local files','release_id':rid}
(base/('verification-remote-all.json'if '--metadata'in sys.argv else'verification-remote-archives.json')).write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report),flush=True)
if problems:sys.exit(1)
