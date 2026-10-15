"""Check installed round-cap/neck envelopes after anisotropic tote scaling.
Run from rungproof-next. Analytic nominal radii, not native acceptance.
"""
import json, math
from pathlib import Path
scene=json.loads(Path('scenes/migrated/lab-2-21-tote-finishing.scene.json').read_text())
cat=json.loads(Path('assets/catalog/candidates.catalog.json').read_text())
asset=next(a for a in cat['assets'] if a['id']=='loads.ibc.open-finishing.v1')
config=next(e['config'] for e in scene['equipment'] if e['id']=='finishing_tote')
size=config['size']
b=asset['bounds'];sx=size[0]/b['widthM'];sz=size[2]/b['depthM']
if config.get('preserveCircularFillPort'): sx=sz=max(sx,sz)
# Exported ray evidence gives nominal inner cap and outer neck radii.
fit=json.loads(Path('assets/material_flow/tote_finishing_open_ibc/review/exported_cap_fit_checks.json').read_text())
cap=fit['inner_wall_hit'][0];neck=fit['neck_outer_wall_hit'][0]
# At 90 degrees the rotating cap exchanges its major and minor axes.
report={'scope':'Nominal installed ellipse envelopes at 0 and 90 degrees; threads/seal/native view unverified','scaleX':sx,'scaleZ':sz,'homeClearanceX_M':(cap-neck)*sx,'homeClearanceZ_M':(cap-neck)*sz,'quarterTurnClearanceX_M':cap*sz-neck*sx,'quarterTurnClearanceZ_M':cap*sx-neck*sz}
report['quarterTurnFits']=min(report['quarterTurnClearanceX_M'],report['quarterTurnClearanceZ_M'])>=0
print(json.dumps(report,indent=2))
Path('assets/material_flow/tote_finishing_open_ibc/review/installed_cap_rotation_fit.json').write_text(json.dumps(report,indent=2)+'\n')

assert report['quarterTurnFits'], 'Installed nominal cap envelope does not fit after rotation'
