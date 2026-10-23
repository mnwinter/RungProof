"""Restore ONE explicitly selected private chained-snapshot tree; preview is default."""
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess


def relative(value):
    if not isinstance(value,str): raise ValueError('Relative path must be text')
    value=value.replace('\\','/')
    parts=value.split('/')
    if not value or any(p in ('','.','..') for p in parts): raise ValueError('Unsafe relative components')
    for p in parts:
        if (p[-1:] in (' ','.') or any(ord(c)<32 or c in '<>:"|?*' for c in p)
            or re.fullmatch(r'(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?',p,re.I)):
            raise ValueError('Unsafe Windows component')
    return '/'.join(parts)


def no_reparse(path):
    path=Path(path).absolute()
    for p in [path,*path.parents]:
        try: info=p.lstat()
        except FileNotFoundError: continue
        if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:
            raise ValueError('Reparse traversal refused: '+str(p))
    return path


def within(root,rel):
    root=no_reparse(root); path=no_reparse(root/relative(rel))
    if not path.resolve().is_relative_to(root.resolve()): raise ValueError('Outside input root')
    return path


def identity(p):
    s=p.stat()
    if not stat.S_ISREG(s.st_mode): raise ValueError('Not a regular file: '+str(p))
    return s.st_size,s.st_mtime_ns,s.st_ino


def hash_file(p):
    p=no_reparse(p); before=identity(p); sha=hashlib.sha256(); count=0
    with p.open('rb') as f:
        for block in iter(lambda:f.read(4*1024*1024),b''):
            sha.update(block); count+=len(block)
    if identity(p)!=before: raise ValueError('File changed while hashing')
    return {'bytes':count,'sha256':sha.hexdigest()}


def equal(row,actual): return row['bytes']==actual['bytes'] and row['sha256']==actual['sha256']


def pinned_json(p,sha):
    if not re.fullmatch('[0-9a-f]{64}',sha): raise ValueError('Invalid pinned SHA256')
    p=no_reparse(p); before=identity(p); raw=p.read_bytes()
    if identity(p)!=before or hashlib.sha256(raw).hexdigest()!=sha: raise ValueError('Receipt pin mismatch')
    value=json.loads(raw.decode('utf-8-sig'))
    if not isinstance(value,dict): raise ValueError('Malformed receipt')
    return value


def rows(receipt):
    if type(receipt.get('schema')) is not int or receipt['schema']!=1 or receipt.get('result')!='PASS' or receipt.get('errors')!=[]:
        raise ValueError('Receipt is not strict schema1 PASS/zero-errors')
    if receipt.get('session_header_errors',[])!=[]: raise ValueError('Session header errors')
    result={}; canonical={}
    if not isinstance(receipt.get('files'),list): raise ValueError('Missing files list')
    for r in receipt['files']:
        tree=relative(r['tree']); rel=relative(r['relative'])
        if '/' in tree: raise ValueError('Tree must be a single name')
        if type(r.get('bytes')) is not int or r['bytes']<0 or not re.fullmatch('[0-9a-f]{64}',r.get('sha256','')):
            raise ValueError('Invalid bytes/hash')
        if any(part.casefold()=='.git' for part in rel.split('/')):
            raise ValueError('Git metadata is separate and may never be overlaid')
        full=tree+'/'+rel
        for i in range(1,len(full.split('/'))+1):
            prefix='/'.join(full.split('/')[:i]); folded=prefix.casefold()
            if folded in canonical and canonical[folded]!=prefix: raise ValueError('Case collision')
            canonical[folded]=prefix
        key=(tree,rel)
        if key in result: raise ValueError('Duplicate file row')
        result[key]=r
    paths={'/'.join(k) for k in result}
    for value in paths:
        parts=value.split('/')
        if any('/'.join(parts[:i]) in paths for i in range(1,len(parts))):
            raise ValueError('File/directory path collision')
    return result


def overlap(a,b):
    a,b=Path(a).resolve(),Path(b).resolve()
    return a.is_relative_to(b) or b.is_relative_to(a)


def target_gate(target,root,head,expected_head):
    target=no_reparse(target)
    if not target.is_absolute() or overlap(target,root): raise ValueError('Target overlaps backup/input root')
    if not target.parent.is_dir(): raise ValueError('Target parent must already exist')
    if target.exists():
        if not target.is_dir() or sorted(p.name for p in target.iterdir())!=['.git']:
            raise ValueError('Existing target must contain ONLY ordinary .git directory')
        gitdir=no_reparse(target/'.git')
        if not gitdir.is_dir(): raise ValueError('.git must be ordinary directory')
        for folder,dirs,files in os.walk(gitdir,followlinks=False):
            for name in dirs+files:no_reparse(Path(folder)/name)
        if not expected_head or expected_head!=head: raise ValueError('Existing clone needs matching expected HEAD')
        actual=subprocess.check_output(['git','-c','safe.directory='+str(target),'-C',str(target),'rev-parse','HEAD'],text=True).strip()
        if actual!=expected_head: raise ValueError('Clone HEAD mismatch')
    return target


def restore(incremental_root,receipt_relative,receipt_sha256,tree,target,apply=False,expected_head=None,report=None,protected_root=None):
    protections=[]
    for value in protected_root or []:
        if not Path(value).is_absolute():raise ValueError('Protected root must be absolute')
        protections.append(no_reparse(value))
    raw_target=Path(target)
    if not raw_target.is_absolute(): raise ValueError('Target must be absolute')
    if not Path(incremental_root).is_absolute(): raise ValueError('Input root must be absolute')
    root=no_reparse(Path(incremental_root).absolute())
    rp=within(root,receipt_relative); chain=pinned_json(rp,receipt_sha256)
    if chain.get('snapshot_mode')!='CHAINED_IMMUTABLE_ADDENDUM': raise ValueError('Wrong chain mode')
    child=rp.parent.relative_to(root).as_posix()
    pp=within(root,chain['parent_receipt_relative']); parent=pinned_json(pp,chain['parent_receipt_sha256'])
    parent_child=pp.parent.relative_to(root).as_posix()
    if parent_child==child: raise ValueError('Parent and child must differ')
    old=rows(parent); current=rows(chain)
    if (parent.get('snapshot_mode')!='COPY_ALL' or type(parent.get('linked_base_file_count')) is not int
        or parent['linked_base_file_count']!=0 or type(parent.get('copied_file_count')) is not int
        or parent['copied_file_count']!=len(old)): raise ValueError('Parent must be complete COPY_ALL without links')
    for (t,r),row in old.items():
        if (row.get('private_storage')!='RECEIPT_COPY' or row.get('verified_private_scope')!='RECEIPT'
            or row.get('verified_private_relative')!='private-snapshot/'+t+'/'+r
            or row.get('paired_D_sha256')!=row['sha256']): raise ValueError('Bad parent storage')
    tree=relative(tree)
    if '/' in tree: raise ValueError('Select exactly one tree')
    selected=[]
    for key,row in current.items():
        t,r=key; ref=relative(row['restore_relative_to_incremental'])
        if row.get('paired_private_sha256')!=row['sha256']: raise ValueError('Unpaired chained row')
        if row.get('storage')=='PARENT_IMMUTABLE_COPY':
            prior=old.get(key)
            if prior is None or not equal(row,prior) or ref!=parent_child+'/private-snapshot/'+t+'/'+r:
                raise ValueError('Parent reference mismatch')
        elif row.get('storage')=='ADDENDUM_OWNED_COPY':
            if ref!=child+'/private-addendum/'+t+'/'+r: raise ValueError('Child reference mismatch')
        else: raise ValueError('Unknown storage')
        source=within(root,ref)
        if t==tree:selected.append((row,source))
    if not selected: raise ValueError('Selected tree has no files')
    if any(overlap(raw_target,p) for p in protections):raise ValueError('Target overlaps protected backup root')
    existed=raw_target.exists()
    target=target_gate(raw_target,root,chain['git_head'],expected_head)
    report=Path(report) if report else target.with_name(target.name+'.restore.json')
    if not report.is_absolute(): raise ValueError('Report must be absolute')
    report=no_reparse(report)
    if (report.exists() or not report.parent.is_dir() or overlap(report,root) or overlap(report,target)
        or any(overlap(report,p) for p in protections)):
        raise ValueError('Report must be NEW outside input and restored tree')
    for row,source in selected:
        if not equal(row,hash_file(source)): raise ValueError('Selected source hash mismatch: '+row['relative'])
        within(target,row['relative']) # validate all destinations before copying anything
    result={'schema':1,'result':'PREVIEW_VERIFIED','private_only':True,'tree':tree,'files':len(selected),
        'bytes':sum(r['bytes'] for r,_ in selected),'target':str(target),'receipt_relative':relative(receipt_relative),
        'receipt_sha256':receipt_sha256,'parent_receipt_relative':chain['parent_receipt_relative'],
        'parent_receipt_sha256':chain['parent_receipt_sha256'],'git_head':chain['git_head'],
        'credential_scan_performed':False,'git_mutated':False,
        'protected_roots':[str(p) for p in protections],
        'restore_tool_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}
    if not apply:return result
    # Recheck lifecycle/input gates immediately before first write.
    target_gate(target,root,chain['git_head'],expected_head)
    pinned_json(rp,receipt_sha256);pinned_json(pp,chain['parent_receipt_sha256'])
    if not existed:target.mkdir(exist_ok=False)
    copied=[]
    for row,source in selected:
        dest=within(target,row['relative']);dest.parent.mkdir(parents=True,exist_ok=True)
        before=identity(no_reparse(source)); sha=hashlib.sha256();count=0
        with source.open('rb') as src,dest.open('xb') as dst:
            for block in iter(lambda:src.read(4*1024*1024),b''):
                dst.write(block);sha.update(block);count+=len(block)
        actual={'bytes':count,'sha256':sha.hexdigest()}
        if identity(no_reparse(source))!=before or not equal(row,actual) or not equal(row,hash_file(dest)):
            raise ValueError('Copy/source/destination verification failed; partial target retained')
        copied.append({'relative':row['relative'],**actual})
    pinned_json(rp,receipt_sha256);pinned_json(pp,chain['parent_receipt_sha256'])
    result.update(result='PASS',report_path=str(report),created_utc=dt.datetime.now(dt.timezone.utc).isoformat(),copied=copied)
    with report.open('x',encoding='utf-8') as f:json.dump(result,f,indent=2);f.write('\n')
    return result


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['incremental-root','receipt-relative','receipt-sha256','tree','target']:
        p.add_argument('--'+name,required=True)
    p.add_argument('--protected-root',action='append',default=[])
    p.add_argument('--apply',action='store_true');p.add_argument('--expected-head');p.add_argument('--report')
    a=p.parse_args()
    try:result=restore(**vars(a))
    except (ValueError,OSError,KeyError,TypeError,subprocess.SubprocessError) as e:
        print(json.dumps({'result':'FAILED','error':str(e),'partial_policy':'Retain partial output; no PASS receipt.'}));return 2
    compact={key:result[key] for key in ['result','tree','files','bytes','target','receipt_sha256','git_head'] if key in result}
    compact['report_path']=result.get('report_path')
    print(json.dumps(compact));return 0
if __name__=='__main__':raise SystemExit(main())
