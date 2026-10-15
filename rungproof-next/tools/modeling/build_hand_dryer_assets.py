"""Original illustrated dryer enclosure/outlet, heater rack and presence station.
No appliance certification, thermal model, protective-function or airflow claim.
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

def outlet(m):
    # A grounded demonstration stand supports the enclosure. Blender +Y maps
    # to Godot -Z; the front nozzle/inspection aperture faces Godot +Z.
    for x in (-.46,.46):
        f.box(f'DRYER_foot_{x}',(x,0,.035),(.24,.70,.07),m['zinc'])
        f.box(f'DRYER_support_{x}',(x,.28,.85),(.06,.08,1.70),m['blue'])
    f.box('DRYER_crossbrace',(0,.28,.52),(.98,.07,.07),m['blue'])
    f.box('DRYER_roof',(0,0,1.84),(1.0,.72,.035),m['white'])
    for x in (-.50,.50):f.box(f'DRYER_side_{x}',(x,0,1.48),(.03,.72,.72),m['white'])
    # Real inlet aperture in rear wall; avoid a solid plate through fan/duct.
    for x in (-.365,.365):f.box(f'DRYER_rear_side_{x}',(x,.36,1.48),(.25,.03,.72),m['white'])
    for z,h in ((1.19,.12),(1.79,.10)):f.box(f'DRYER_rear_strip_{z}',(0,.36,z),(.48,.03,h),m['white'])
    # Open rectangular inlet duct reaches retained scaled fan front at Y=.625.
    for x in (-.235,.235):f.box(f'DRYER_inlet_side_{x}',(x,.49,1.5125),(.018,.27,.47),m['zinc'])
    for z in (1.2775,1.7475):f.box(f'DRYER_inlet_horizontal_{z}',(0,.49,z),(.47,.27,.018),m['zinc'])
    for x in (-.21,.21):f.box(f'DRYER_fan_mount_{x}',(x,.525,1.36),(.09,.35,.07),m['blue'])
    for x in (-.42,.42):
        f.box(f'DRYER_heater_bracket_{x}',(x,0,1.32),(.145,.43,.025),m['zinc'])
    # Bottom air outlet remains hollow. The narrow slot is visibly directed
    # downward toward the hand station, rather than toward the floor off-scene.
    for x in (-.31,.31):f.box(f'NOZZLE_side_{x}',(x,-.18,1.07),(.025,.29,.14),m['zinc'])
    for y in (-.325,-.035):f.box(f'NOZZLE_lip_{y}',(0,y,1.07),(.64,.025,.14),m['zinc'])
    glass=f.mat('Illustrative dryer inspection glazing',(.35,.60,.66),0,.18)
    glass.diffuse_color=(.35,.60,.66,.10);glass.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=.10
    f.box('DRYER_inspection_window',(0,-.36,1.48),(.94,.01,.70),glass,0,False)
    for x in (-.44,.44):f.box(f'DRYER_window_frame_{x}',(x,-.37,1.48),(.025,.03,.69),m['blue'])
    f.text('DRYER_label','HAND DRYER',(0,-.386,1.79),.085,m['black'],(math.pi/2,0,0))

def heater(m):
    # An original open resistive-element rack with terminals and ceramic-like
    # support blocks. Material appearance is illustrative, not a BOM assertion.
    for y in (-.19,.19):f.box(f'HEATER_rail_{y}',(0,y,0),(.72,.025,.04),m['zinc'])
    for x in (-.30,.30):
        for y in (-.19,.19):f.box(f'HEATER_insulator_{x}_{y}',(x,y,.05),(.045,.035,.07),m['white'])
    for i in range(7):
        x=-.27+i*.09
        f.tube(f'HEATER_element_{i}',(x,-.15,.08),(x,.15,.08),.007,m['zinc'])
        if i<6:
            y=.15 if i%2==0 else -.15
            f.tube(f'HEATER_return_{i}',(x,y,.08),(x+.09,y,.08),.007,m['zinc'])
    for x in (-.27,.27):f.cyl(f'HEATER_terminal_{x}',(x,-.19,.08),.015,.055,m['brass'],'Y')
    f.box('HEATER_terminal_cover',(0,-.24,.08),(.72,.08,.06),m['black'])

def presence(m):
    # Grounded tray beneath the outlet. Sensor aperture looks across the real
    # hand zone. Hands are separate named groups for the scene's insertion pose.
    for x in (-.44,.44):
        f.box(f'HAND_STATION_foot_{x}',(x,-.18,.025),(.20,.52,.05),m['zinc'])
        f.box(f'HAND_STATION_post_{x}',(x,-.18,.39),(.04,.04,.75),m['blue'])
    f.box('HAND_STATION_tray',(0,-.18,.78),(.94,.52,.04),m['steel'])
    f.box('HAND_SENSOR_bracket',(.46,-.18,.87),(.025,.08,.18),m['zinc'])
    f.box('HAND_SENSOR_body',(.43,-.18,.87),(.045,.065,.05),m['black'],.008)
    f.box('HAND_SENSOR_lens',(.405,-.18,.87),(.004,.035,.02),m['blue'],.003)
    glove=f.mat('Illustrative hands',(.64,.40,.25),0,.65)
    # Palms, four fingers and thumb make an identifiable pair, rather than a
    # sensor represented by a dispensing can. No anatomical/contact claim.
    for side,x in enumerate((-.14,.14)):
        f.box(f'HAND_{side}_palm',(x,-.18,.87),(.17,.17,.05),glove,.022,False)
        for digit in range(4):
            f.box(f'HAND_{side}_finger_{digit}',(x-.062+digit*.042,-.32,.87),(.035,.17,.035),glove,.015,False)
        f.box(f'HAND_{side}_thumb',(x+(-.10 if side==0 else .10),-.21,.865),(.045,.105,.035),glove,.015,False,rotation=(0,0,(-.4 if side==0 else .4)))

catalog_path=ROOT/'assets/catalog/candidates.catalog.json';catalog=json.loads(catalog_path.read_text(encoding='utf-8'))
register_path=ROOT/'assets/catalog/industrial-reference-register.json';register=json.loads(register_path.read_text(encoding='utf-8'))
for slug,builder in [('air_outlet',outlet),('heating_element',heater),('hand_presence_sensor',presence)]:
    review=f.BASE/slug/'review';archive=review/'historical_invalid_identity_20261006';archive.mkdir(parents=True,exist_ok=True)
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
    marker.write_text(json.dumps(dict(assetId=id,status='candidate-unapproved',scope='Original illustrated dryer component; scene integration/native reference review does not confer asset approval or thermal, electrical, airflow or physical acceptance.',historicalEvidence='Archived copied-family recognition does not approve replacement.'),indent=2)+'\n',encoding='utf-8')
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8');register_path.write_text(json.dumps(register,indent=2)+'\n',encoding='utf-8')
print('HAND_DRYER_ASSETS_BUILT 3 original unapproved replacements')
