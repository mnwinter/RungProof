import os,json,collections,time
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22'); (base/'backup-tools').mkdir()
roots={
 'RungProof':Path('C:/Users/matt.winter/Documents/RungProof'),
 'legacy-PLC-Visual-Simulator':Path('C:/Users/matt.winter/Documents/PLC Visual Simulator'),
 'Godot-userdata-RungProof':Path('C:/Users/matt.winter/AppData/Roaming/Godot/app_userdata/RungProof'),
 'thread-visualizations':Path('C:/Users/matt.winter/.codex/visualizations/2026/10/22/01a161f0-5690-7783-a4ed-93dd4dee3195'),
}
summary=[]; errors=[]; entries=[]
for label,root in roots.items():
 n=total=0; cats=collections.defaultdict(lambda:[0,0]); largest=[]
 if not root.exists(): errors.append({'root':str(root),'error':'MISSING'});continue
 for parent,dirs,files in os.walk(root,followlinks=False):
  for name in files:
   p=Path(parent)/name
   try:
    s=p.stat(); rel=p.relative_to(root).as_posix(); n+=1;total+=s.st_size
    cat=('/'.join(rel.split('/')[:2]) if label=='RungProof' else rel.split('/')[0]);cats[cat][0]+=1;cats[cat][1]+=s.st_size
    entries.append({'tree':label,'relative':rel,'bytes':s.st_size,'mtime_ns':s.st_mtime_ns,'symlink':p.is_symlink()})
    if s.st_size>50_000_000:largest.append({'relative':rel,'bytes':s.st_size})
   except Exception as e:errors.append({'path':str(p),'error':str(e)})
 summary.append({'tree':label,'source':str(root),'files':n,'bytes':total,'categories':dict(cats),'large_files':largest})
(base/'inventory-before.json').write_text(json.dumps({'roots':summary,'errors':errors},indent=2),encoding='utf8')
with (base/'inventory-files.jsonl').open('w',encoding='utf8') as f:
 for e in entries:f.write(json.dumps(e)+'\n')
print(json.dumps({'trees':[{'tree':s['tree'],'files':s['files'],'GiB':round(s['bytes']/2**30,3)}for s in summary],'errors':errors,'symlinks':sum(x['symlink']for x in entries)}))
