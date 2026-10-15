"""Original illustrative spray tunnel, supported load and duct damper.
Replaces invalid copied identities. No airflow, coating quality or equipment approval.
"""
import importlib.util,json,os,math
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER']='__helpers_only__'
spec=importlib.util.spec_from_file_location('flow',ROOT/'tools/modeling/build_material_flow_assets.py')
f=importlib.util.module_from_spec(spec);spec.loader.exec_module(f)
f.BASE=ROOT/'assets/training_accessories';bpy.context.preferences.filepaths.save_version=0

def tunnel(m):
    # X-axis tunnel. Leg/side-frame Z clear the complete scaled conveyor envelope.
    for x in (-1.12,1.12):
        for y in (-1.32,1.32):
            f.box(f'TUNNEL_foot_{x}_{y}',(x,y,.035),(.24,.22,.07),m['zinc'])
            f.box(f'TUNNEL_column_{x}_{y}',(x,y,1.14),(.09,.09,2.2),m['blue'])
        f.box(f'TUNNEL_end_header_{x}',(x,0,2.26),(.09,2.73,.12),m['blue'])
    for y in (-1.32,1.32):f.box(f'TUNNEL_side_header_{y}',(0,y,2.26),(2.33,.09,.12),m['blue'])
    f.box('TUNNEL_roof',(0,0,2.34),(2.33,2.73,.04),m['white'])
    # Front inspection window; rear wall has a genuine outlet opening.
    window=f.mat('Illustrative inspection glazing',(.35,.65,.75),0,.15)
    window.diffuse_color=(.35,.65,.75,.12);window.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=.12
    f.box('TUNNEL_inspection_glazing',(0,-1.32,1.65),(2.15,.012,1.10),window,0,False)
    for y in (-1.32,1.32):
        f.box(f'TUNNEL_sill_{y}',(0,y,1.02),(2.15,.04,.06),m['zinc'])
    for x in (-.775,.775):f.box(f'TUNNEL_rear_side_{x}',(x,1.32,1.66),(.61,.04,1.16),m['white'])
    for z,h in ((1.145,.13),(2.19,.10)):f.box(f'TUNNEL_rear_strip_{z}',(0,1.32,z),(.94,.04,h),m['white'])
    # Gun mounting stem contacts retained robot flange after scene rotation/scale.
    f.box('GUN_roof_mount',(0,0,2.165),(.18,.20,.35),m['zinc'])
    f.box('GUN_mount_plate',(0,0,1.99),(.18,.22,.035),m['blue'])
    for y,color in ((-.11,m['black']),(.11,m['blue'])):
        f.tube(f'GUN_service_drop_{y}',(-.10,y,2.10),(-.10,y,2.21),.022,color,False)
        f.tube(f'GUN_service_route_{y}',(-.10,y,2.21),(-.10,1.25,2.21),.022,color,False)
    f.text('TUNNEL_label','SPRAY STATION',(0,-1.37,2.25),.11,m['white'],(math.pi/2,0,0))

def workpiece(m):
    # Flat fixture underside bears on the conveyor at scene Y=.9 m.
    f.box('WORKPIECE_fixture',(0,0,.025),(.72,.56,.05),m['steel'],.012)
    f.box('WORKPIECE_BODY',(0,0,.225),(.54,.38,.35),m['alum'],.018)
    for x in (-.30,.30):f.box(f'WORKPIECE_stop_{x}',(x,0,.07),(.025,.43,.04),m['yellow'])
    f.text('WORKPIECE_label','PART',(0,-.196,.23),.08,m['black'],(math.pi/2,0,0))

def damper(m):
    # Hollow rectangular duct spans the actual rear-wall aperture to fan inlet.
    # Blender +Y is Godot -Z. Fan inlet at Y=2.035, centre height=1.6975.
    z=1.6975
    for x in (-.465,.465):f.box(f'DUCT_side_{x}',(x,1.68,z),(.025,.715,.955),m['zinc'])
    for h in (-.465,.465):f.box(f'DUCT_horizontal_{h}',(0,1.68,z+h),(.955,.715,.025),m['zinc'])
    for y in (1.35,2.026):
        for x in (-.49,.49):f.box(f'DUCT_flange_vertical_{x}_{y}',(x,y,z),(.05,.028,1.01),m['steel'])
        for h in (-.49,.49):f.box(f'DUCT_flange_horizontal_{h}_{y}',(0,y,z+h),(1.03,.028,.05),m['steel'])
    # Vane starts open. Renderer rotates around its real centre only.
    f.box('DAMPER_vane',(0,1.68,z),(.86,.30,.015),m['alum'],.003)
    f.cyl('DAMPER_shaft',(0,1.68,z),.016,1.16,m['zinc'],'X')
    f.box('DAMPER_actuator',(.60,1.68,z),(.16,.20,.16),m['blue'])
    # Stand for retained axial fan, without its original pedestal/base copy.
    for x in (-.451,.451):
        for y in (2.20,2.43):
            f.box(f'EXHAUST_foot_{x}_{y}',(x,y,.035),(.20,.19,.07),m['zinc'])
            f.box(f'EXHAUST_stand_{x}_{y}',(x,y,.76),(.07,.07,1.45),m['blue'])
        f.box(f'EXHAUST_side_mount_{x}',(x,2.315,1.50),(.12,.38,.09),m['zinc'])
    f.box('EXHAUST_crossbar',(0,2.315,.72),(.90,.07,.07),m['blue'])
    f.text('DUCT_label','EXHAUST',(0,2.055,z+.52),.075,m['black'],(math.pi/2,0,0))

catalog_path=ROOT/'assets/catalog/candidates.catalog.json';catalog=json.loads(catalog_path.read_text())
register_path=ROOT/'assets/catalog/industrial-reference-register.json';register=json.loads(register_path.read_text())
for slug,builder in [('coating_enclosure',tunnel),('workpiece_load',workpiece),('ventilation_damper',damper)]:
    review=f.BASE/slug/'review'; archive=review/'historical_invalid_identity_20261006';archive.mkdir(parents=True,exist_ok=True)
    # Archive old records/renders once. Rebuilds must retain current views.
    marker=review/'repair_scope.json'
    if not marker.exists():
        for file in list(review.iterdir()):
            if file.is_file() and not (archive/file.name).exists():file.rename(archive/file.name)
    f.save(slug,builder)
    points=[];graph=bpy.context.evaluated_depsgraph_get()
    for o in bpy.context.scene.objects:
        if o.type=='MESH' and o.get('rungproof_asset'):
            e=o.evaluated_get(graph);points.extend(e.matrix_world@Vector(v) for v in e.bound_box)
    size=[max(p[a] for p in points)-min(p[a] for p in points) for a in range(3)]
    id='training.accessory.'+slug+'.v1';a=next(a for a in catalog['assets'] if a['id']==id)
    a.pop('genericBasisAssetId',None);a['bounds']=dict(widthM=size[0],heightM=size[2],depthM=size[1]);a['kinematics']=[];a['animationTags']=[]
    a['quality']=dict(status='candidate',blindReviewId=None,recognitionConfidence=None,topologyReviewed=False,materialReviewed=False,scaleReviewed=False,animationReviewed=False)
    register['entries']=[e for e in register['entries'] if e['assetId']!=id]
    marker.write_text(json.dumps(dict(assetId=id,status='candidate-unapproved',scope='Original illustrated spray-line installation; symbolic process only. No airflow/coating/physical acceptance.',historicalEvidence='Archived copied-asset recognition does not approve replacement.'),indent=2)+'\n')
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n');register_path.write_text(json.dumps(register,indent=2)+'\n')
print('COATING_ASSETS_BUILT 3 original unapproved replacements')
