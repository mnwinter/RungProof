"""Verify nominal exported cap/neck seat geometry; no mechanical approval."""
import bpy,json
from pathlib import Path
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(Path('assets/material_flow/tote_finishing_open_ibc/delivery/tote_finishing_open_ibc.glb').resolve()))
cap=bpy.data.objects['IBC_fill_cap'];neck=bpy.data.objects['IBC_open_fill_neck']
def hit(obj,origin,direction):
 inv=obj.matrix_world.inverted();ok,p,n,index=obj.ray_cast(inv@Vector(origin),(inv.to_3x3()@Vector(direction)).normalized())
 return list(obj.matrix_world@p) if ok else None
roof=hit(cap,(0,0,1.4),(0,0,1));inner=hit(cap,(0,0,1.53),(1,0,0));outer=hit(neck,(.2,0,1.53),(-1,0,0));neck_top=max((neck.matrix_world@Vector(c)).z for c in neck.bound_box)
report=dict(inner_roof_hit=roof,inner_wall_hit=inner,neck_outer_wall_hit=outer,seat_gap_m=roof[2]-neck_top,radial_gap_m=inner[0]-outer[0])
checks=dict(open_underside_reaches_roof=roof[2]>1.55,inner_roof_seats_without_axial_penetration=abs(report['seat_gap_m'])<.00001,cap_wall_clears_neck=report['radial_gap_m']>.0049,roof_remains_above_neck=max((cap.matrix_world@Vector(c)).z for c in cap.bound_box)>neck_top+.019)
report['checks']=checks
Path('assets/material_flow/tote_finishing_open_ibc/review/exported_cap_fit_checks.json').write_text(json.dumps(report,indent=2)+'\n')
print('TOTE_EXPORTED_CAP_FIT '+json.dumps(report))
if not all(checks.values()):raise RuntimeError('Exported cap fit failed')
