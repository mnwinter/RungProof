"""Extract the documented native-review queue without claiming visual acceptance."""
import json,re
from pathlib import Path
source=Path('../docs/MULTI_ANGLE_SCENE_REVIEW.md')
raw=source.read_bytes()
try:text=raw.decode('utf-8')
except UnicodeDecodeError:text=raw.decode('cp1252')
rows=[]
# These changed layouts, displays or processes lack fresh native inspection.
# Historical home inspection does not certify current motion or acceptance.
latest_pending=set()
implemented_pending={
}
# C1-C6 closed by recorded offline native evidence; this is not physical/live certification.
accepted_bounded={
 'tank-high-low': 'C9 high-low sub-scope accepted: native exact threshold frames/operator Stop/resume/Reset; prior five-angle geometry retained',
 'tank-level': 'C9 analog sub-scope accepted: native exact threshold/4-20mA frames/operator Stop/resume/Reset; prior five-angle geometry retained',
 'lab-2-14-sump-pump': 'C9 sump sub-scope accepted: sampled native float actuation/Stop/Reset; opaque-water visibility and physical buoyancy limits retained',
 'lab-2-18-pallet-pickup': 'C9 shipping sub-scope accepted: native last-active99/expiry100/retention101/Stop/Reset; prior sampled poses retained',
 'lab-2-21-tote-finishing': 'C6 closed: offline native fill/cap/opposing label contact/dwell/retraction/retention/inspection/Stop/Reset evidence accepted',
 'lab-11-12-mobile-traffic-lights': 'C5 closed: offline native views/queued crossings/READY loss/occupied all-red/conflicting entries/Stop/Reset evidence accepted',
 'lab-11-11-service-elevator': 'C4 closed: offline native hoist/access/door gating/moving rope/roundtrip/upper/Stop/Reset evidence accepted',
 'lab-10-05-motor-struct-data': 'C1 closed: offline native STRUCT views/typed gates/watch/operand layout/power flow/Stop/Reset evidence accepted',
 'lab-10-06-ten-motor-array-startup': 'C2 closed: offline native ARRAY views/effective stages/shaft/inhibit/Stop/Reset evidence accepted',
 'lab-11-06-wastewater-collection': 'C3 closed: offline native geometry/readout/threshold/independent actuator/Stop/Reset evidence accepted',
}
# Evidence triage of the literal FAIL/open rows in the source matrix. A
# verification limitation must not be presented as a confirmed collision or
# missing implementation. Preserve the original result text below for review.
implementation_gaps={
}
verification_gaps={
 'lab-10-03-vision-package-sorter': 'Required uninterrupted full-route native observation in five angles pending; continuous solid clearance between sampled poses also unverified',
 'lab-2-17-pallet-robot': 'Required uninterrupted full-cycle native observation in five angles pending; continuous solid clearance between sampled poses also unverified',
}
for line in text.splitlines():
 if not re.match(r'^\| \d+ \|',line):continue
 cells=[c.strip() for c in line.split('|')[1:-1]]
 if len(cells)!=5:raise RuntimeError('Unexpected matrix row: '+line)
 index,scene,bounds,views,result=cells;scene=scene.strip('`')
 angles={name:bool(re.search(r'\b'+pattern+r'\b',views)) for name,pattern in [('FR','FR'),('FL','FL'),('RL','RL'),('RR','RR'),('Top','(?:Top|T)')]}
 explicit_failure=bool(re.search(r'\bFAIL\b',result,re.I))
 current_pending=scene in latest_pending
 # Unknown future FAIL records need triage, not an inferred repair order.
 gap_kind=('NATIVE_REVIEW_ACCEPTED_BOUNDED' if scene in accepted_bounded else
           'IMPLEMENTED_NATIVE_PENDING' if scene in implemented_pending else
           'IMPLEMENTATION_GAP' if scene in implementation_gaps else
           'VERIFICATION_GAP' if scene in verification_gaps else
           'UNTRIAGED_RECORD' if explicit_failure else 'NOT_TRIAGED')
 disposition=('RETAIN_BOUNDED_NATIVE_EVIDENCE' if scene in accepted_bounded else
              'REVIEW_CHANGED_SCENE' if gap_kind=='IMPLEMENTED_NATIVE_PENDING' else
              'IMPLEMENT_RECORDED_GAP' if gap_kind=='IMPLEMENTATION_GAP' else
              'VERIFY_RECORDED_GAP' if gap_kind=='VERIFICATION_GAP' else
              'TRIAGE_RECORDED_FAILURE' if explicit_failure else
              'REVIEW_CHANGED_SCENE' if current_pending else 'RECONCILE_CURRENT_EVIDENCE')
 rows.append(dict(index=int(index),sceneId=scene,recordedNativeViews=views,recordedAngles=angles,recordedResult=result,
  latestNativeReviewPending=current_pending,explicitFailureRecorded=explicit_failure,
  gapKind=gap_kind,gapSummary=accepted_bounded.get(scene,implemented_pending.get(scene,implementation_gaps.get(scene,verification_gaps.get(scene)))),
  disposition=disposition,
  acceptance='BOUNDED_OFFLINE_NATIVE_ONLY' if scene in accepted_bounded else 'NOT_CERTIFIED_BY_THIS_INVENTORY'))
assert len(rows)==77 and len({r['sceneId'] for r in rows})==77
report={'source':'MULTI_ANGLE_SCENE_REVIEW.md','scope':'Inventory of documented observations only; views may predate current geometry or displays. Pending flags refer to changed layouts, displays or processes. Runtime, collision and visual acceptance remain separate.',
 'sceneCount':len(rows),'allFiveAnglesRecordedCount':sum(all(r['recordedAngles'].values()) for r in rows),
 'explicitFailureCount':sum(r['explicitFailureRecorded'] for r in rows),
 'triagedImplementationGapCount':sum(r['gapKind']=='IMPLEMENTATION_GAP' for r in rows),
 'implementedNativePendingCount':sum(r['gapKind']=='IMPLEMENTED_NATIVE_PENDING' for r in rows),
 'triagedVerificationGapCount':sum(r['gapKind']=='VERIFICATION_GAP' for r in rows),
 'knownChangedSceneCount':sum(r['latestNativeReviewPending'] for r in rows),'scenes':rows}
Path('../docs/SCENE_NATIVE_REVIEW_QUEUE.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='scenes'},indent=2))
for r in rows:
 if r['explicitFailureRecorded'] or r['latestNativeReviewPending']:print(r['sceneId']+' '+r['disposition'])
