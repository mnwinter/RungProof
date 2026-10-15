"""Original illustrated luggage and grounded four-cell weighing platform.
No scale calibration, load rating or material/OEM acceptance is implied.
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

def scale(m):
    # Deck top is 0.9 m, matching infeed, diverter crowns and reject receiver.
    # Each corner is borne through a visible cell onto the grounded frame.
    for x in (-.80,.80):
        for y in (-.43,.43):
            f.box(f'SCALE_foot_{x}_{y}',(x,y,.03),(.22,.20,.06),m['zinc'])
            f.box(f'SCALE_leg_{x}_{y}',(x,y,.405),(.065,.065,.75),m['blue'])
    for y in (-.43,.43):
        f.box(f'SCALE_frame_{y}',(0,y,.745),(1.74,.08,.07),m['blue'])
    for x in (-.80,.80):
        f.box(f'SCALE_crossbar_{x}',(x,0,.745),(.08,.90,.07),m['blue'])
    for x in (-.75,.75):
        for y in (-.38,.38):
            f.box(f'LOAD_CELL_body_{x}_{y}',(x,y,.82),(.23,.075,.08),m['alum'],.008)
            for dx in (-.075,.075):
                f.cyl(f'LOAD_CELL_bolt_{x}_{y}_{dx}',(x+dx,y,.855),.015,.015,m['steel'],'Z')
            f.tube(f'LOAD_CELL_cable_{x}_{y}',(x,y+.04,.82),(x,y+.06,.69),.009,m['black'],False)
    f.box('SCALE_DECK_surface',(0,0,.88),(2.0,1.1,.04),m['zinc'],.008)
    f.box('SCALE_junction_box',(0,.485,.59),(.26,.10,.18),m['black'],.012)
    f.tube('SCALE_signal_route',(0,.485,.59),(0,.485,.745),.012,m['black'],False)
    f.box('SCALE_nameplate',(0,-.56,.77),(.58,.025,.14),m['blue'],.003)
    f.text('SCALE_label','WEIGH',(0,-.576,.77),.085,m['white'],(math.pi/2,0,0))

def luggage(m):
    case=f.mat('Illustrative luggage shell',(.12,.30,.52),0,.50)
    f.box('LUGGAGE_base',(0,0,.025),(.66,.50,.05),m['black'],.008)
    f.box('LUGGAGE_BODY',(0,0,.28),(.66,.50,.46),case,.045,False)
    # A seam, corner protectors and joined top/side handles identify a suitcase.
    for y in (-.255,.255):
        f.box(f'LUGGAGE_seam_{y}',(0,y,.28),(.61,.015,.035),m['black'],.004,False)
    for x in (-.305,.305):
        for y in (-.225,.225):
            f.box(f'LUGGAGE_corner_{x}_{y}',(x,y,.455),(.055,.055,.10),m['black'],.01,False)
    for x in (-.12,.12):
        f.box(f'LUGGAGE_handle_anchor_{x}',(x,0,.52),(.065,.075,.03),m['zinc'],.006)
        f.box(f'LUGGAGE_handle_post_{x}',(x,0,.55),(.025,.035,.06),m['black'],.008)
    f.box('LUGGAGE_top_handle',(0,0,.585),(.265,.045,.035),m['black'],.012)
    for y in (-.085,.085):f.box(f'LUGGAGE_side_anchor_{y}',(-.335,y,.28),(.035,.035,.045),m['zinc'],.005)
    f.box('LUGGAGE_side_handle',(-.365,0,.28),(.045,.20,.035),m['black'],.01)
    f.box('LUGGAGE_tag_plate',(.15,-.27,.37),(.16,.015,.095),m['white'],.005,False)
    f.text('LUGGAGE_tag','SIM BAG',(.15,-.281,.37),.023,m['black'],(math.pi/2,0,0))

catalog_path=ROOT/'assets/catalog/candidates.catalog.json';catalog=json.loads(catalog_path.read_text(encoding='utf-8'))
register_path=ROOT/'assets/catalog/industrial-reference-register.json';register=json.loads(register_path.read_text(encoding='utf-8'))
for slug,builder,archive_name in [('scale_load_cell_platform',scale,'historical_incomplete_platform_20261006'),('luggage_load',luggage,'historical_invalid_identity_20261006')]:
    review=f.BASE/slug/'review';archive=review/archive_name;archive.mkdir(parents=True,exist_ok=True)
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
    aid='training.accessory.'+slug+'.v1';a=next(a for a in catalog['assets'] if a['id']==aid)
    a.pop('genericBasisAssetId',None);a['bounds']=dict(widthM=size[0],heightM=size[2],depthM=size[1]);a['kinematics']=[];a['animationTags']=[]
    a['quality']=dict(status='candidate',blindReviewId=None,recognitionConfidence=None,topologyReviewed=False,materialReviewed=False,scaleReviewed=False,animationReviewed=False)
    register['entries']=[e for e in register['entries'] if e['assetId']!=aid]
    marker.write_text(json.dumps(dict(assetId=aid,status='candidate-unapproved',scope='Original illustrated luggage-sort component, pending connected scene and numeric/controller review; no calibration, load rating or physical acceptance.',historicalEvidence='Archived inherited-family review does not approve the new model.'),indent=2)+'\n',encoding='utf-8')
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8');register_path.write_text(json.dumps(register,indent=2)+'\n',encoding='utf-8')
print('LUGGAGE_ASSETS_BUILT 2 original unapproved replacements')
