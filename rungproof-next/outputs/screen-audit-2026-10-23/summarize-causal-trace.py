import json
from pathlib import Path
q=Path(__file__).resolve().parent
out={}
for name in ['v3-20ms-authored','v4-20ms-authored']:
    data=[json.loads(s) for s in (q/(name+'.jsonl')).read_text(encoding='utf-8').splitlines()]
    witnesses=[]
    for parcel in range(3):
        changes=[i for i,r in enumerate(data) if r['parcels'][parcel]['Route']>=0]
        if changes:
            i=changes[0]
            witnesses.append({'kind':'first-route-commit','parcelIndex':parcel,'window':data[max(i-3,0):i+3]})
    gate=[]
    for i,r in enumerate(data):
        c=r['klass']
        # Follower means parcel AFTER the active size-class parcel, not the
        # currently admitted medium that legitimately occupies an open gate.
        if c in [1,2] and not r['commands']['infeed_gate_open'] and r['GateOpen']:
            following=r['parcels'][int(c)]
            if following['Route']<0 and following['front']>-2.7:
                gate.append(i)
    if gate:
        i=gate[0]
        witnesses.append({'kind':'following-active-parcel-already-past-closed-gate','window':data[max(i-3,0):i+3]})
    out[name]={'witnesses':witnesses,'final':data[-1]}
(q/'parcel-causal-witnesses.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
for name,r in out.items():
    print(name)
    for w in r['witnesses']:
        print(' ',w['kind'],w.get('parcelIndex',''))
        for s in w['window']:
            print('  ',s['scan'],s['phase'],s['snapshotPhase'],s['klass'],s['commands'],s['TableLoaded'],s['TableClear'],s['TableAligned'],s['GateOpen'],[(round(p['X'],5),p['Route']) for p in s['parcels']])
