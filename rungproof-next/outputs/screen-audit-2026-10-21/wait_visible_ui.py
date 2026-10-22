"""Wait on actual native-window pixels only; never query simulator state."""
import hashlib,json,re,subprocess,sys,time,urllib.request
from pathlib import Path
from decimal import Decimal
from PIL import Image
root=Path(__file__).parent
url,prefix,point,value=sys.argv[1:5]
deadline=time.monotonic()+float(sys.argv[5] if len(sys.argv)>5 else 180)
poll_seconds=float(sys.argv[6] if len(sys.argv)>6 else 2)
if not .15<=poll_seconds<=10:raise ValueError('Poll seconds must be between0.15 and10')
normalize=lambda s:re.sub('[^a-z0-9]','',s.lower())
target=normalize(point+value)
numeric_request=re.fullmatch(r'(>=|<=)([+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+))',value)
if value.startswith(('>=','<=')) and not numeric_request:
    raise ValueError('Numeric comparator requires >=number or <=number without spaces')
comparison_operator=numeric_request.group(1) if numeric_request else None
comparison_threshold=Decimal(numeric_request.group(2)) if numeric_request else None
numeric_observation=None
def visual_rows(ocr,min_x=0,min_y=0,tolerance=5):
    # Rejoin only words on the same actual visible pixel row. Missing text
    # remains missing; never fill a name/value from model state or context.
    words=[w for line in ocr['lines'] for w in line['words']
           if w['x']>=min_x and w['y']>=min_y]
    rows=[]
    for word in sorted(words,key=lambda w:w['y']+w['h']/2):
        center=word['y']+word['h']/2
        if rows and abs(center-rows[-1][0])<=tolerance:rows[-1][1].append(word)
        else:rows.append((center,[word]))
    return [' '.join(w['text'] for w in sorted(row,key=lambda w:w['x']))
            for _,row in rows]

def exact_opposite_bool_visible(lines):
    # Only skip enlarged OCR when the complete exact requested point-name and
    # the opposite BOOL value occupy one native visual row. No substring/name
    # aliases, numeric inference, or partially recognized name is accepted.
    wanted=normalize(value)
    if wanted not in ('true','false'):return False
    opposite='false' if wanted=='true' else 'true'
    return normalize(point+opposite) in {normalize(line) for line in lines}

def exact_visible_match(lines):
    # No generic I/l/1 or value substitution. An OCR miss is not an app fail.
    global numeric_observation
    numeric_observation=None
    if numeric_request:
        candidates=[]
        for line in lines:
            # Entire final whitespace-separated token must be numeric; its
            # preceding complete OCR row must be exactly this point name.
            match=re.fullmatch(r'(.+?)\s+([+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+))\s*',line)
            if match and normalize(match.group(1))==normalize(point):
                candidates.append((match.group(2),line))
        if len(candidates)!=1:return False  # ambiguous/missing rows stay unproven
        actual,row=candidates[0]
        numeric_observation={'operator':comparison_operator,'threshold':str(comparison_threshold),
                             'actual_value':actual,'actual_row':row}
        parsed=Decimal(actual)
        return parsed>=comparison_threshold if comparison_operator=='>=' else parsed<=comparison_threshold
    return target in {normalize(line) for line in lines}

index=0
while True:
    index+=1
    path=root/(prefix+'-'+str(time.time_ns())+'.png')
    raw=urllib.request.urlopen(url+'/screen?t='+str(time.time_ns()),timeout=30).read()
    with path.open('xb') as capture_file:capture_file.write(raw)
    out=path.with_suffix('.ocr.json')
    subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(path),'-OutputPath',str(out)],stdout=subprocess.DEVNULL,check=True)
    observed=json.loads(out.read_text(encoding='utf-8-sig'))
    lines=visual_rows(observed,1230,140)
    matched=exact_visible_match(lines)
    record=dict(utc_ns=time.time_ns(),image=path.name,
                sha256=hashlib.sha256(raw).hexdigest(),point=point,value=value,
                raw_ocr=out.name,raw_ocr_sha256=hashlib.sha256(out.read_bytes()).hexdigest(),
                matched=matched,matched_pass='raw' if matched else None,
                visible_rows=lines,source='Native-window visible Signals pixels only')
    record['numeric_comparison']=numeric_observation
    opposite_visible=not matched and exact_opposite_bool_visible(lines)
    record.update(opposite_bool_visible=opposite_visible,poll_seconds=poll_seconds)
    if not matched and not opposite_visible:
        # Retry only the same captured native pixels, enlarged 2x. Never
        # refresh between passes or query the simulator for a missing label.
        crop=path.with_name(path.stem+'-points-2x.png')
        if crop.exists():raise FileExistsError(crop)
        with Image.open(path) as shot:
            if shot.width<1600 or shot.height<795:
                raise RuntimeError('Signals crop requires native frame at least1600x795; inspect '+str(path))
            shot.crop((1235,140,1600,795)).resize((730,1310)).save(crop)
        crop_out=crop.with_suffix('.ocr.json')
        subprocess.run(['powershell.exe','-NoProfile','-File',str(root/'ocr_screenshot.ps1'),'-ImagePath',str(crop),'-OutputPath',str(crop_out)],stdout=subprocess.DEVNULL,check=True)
        retry=json.loads(crop_out.read_text(encoding='utf-8-sig'))
        retry_lines=visual_rows(retry,tolerance=8)
        matched=exact_visible_match(retry_lines)
        record.update(crop=crop.name,crop_box=[1235,140,1600,795],crop_scale=2,
                      crop_sha256=hashlib.sha256(crop.read_bytes()).hexdigest(),
                      crop_ocr=crop_out.name,crop_ocr_sha256=hashlib.sha256(crop_out.read_bytes()).hexdigest(),
                      crop_visible_rows=retry_lines,matched=matched,
                      matched_pass='crop2x' if matched else None,
                      numeric_comparison=numeric_observation)
    with (root/'visible-waits.jsonl').open('a') as log:
        log.write(json.dumps(record)+'\n')
    if matched:
        print(json.dumps(dict(result='VISIBLE_CONDITION_MATCHED',image=path.name,polls=index,point=point,value=value,numeric_comparison=numeric_observation)),flush=True)
        break
    if time.monotonic()>deadline:
        raise RuntimeError('Visible condition not observed before timeout; inspect '+str(path))
    if index%5==0:print(json.dumps(dict(result='WAITING_ON_VISIBLE_CONDITION',polls=index,point=point)),flush=True)
    time.sleep(poll_seconds)
