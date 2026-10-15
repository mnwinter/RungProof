"""Original illustrative EV lesson geometry, without electrical/OEM approval.

Blender Z is vertical; GLB maps it to Godot Y. The bay owns the fixed cable,
while the separate connector is installed at (-.8, 1.05, 1.04) in Godot.
"""
import importlib.util, json, math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER'] = '__helpers_only__'
spec = importlib.util.spec_from_file_location('flow', ROOT/'tools/modeling/build_material_flow_assets.py')
f = importlib.util.module_from_spec(spec); spec.loader.exec_module(f)
f.BASE = ROOT/'assets/training_accessories'
bpy.context.preferences.filepaths.save_version = 0

def bay(m):
    paint = f.mat('Original vehicle blue', (.12,.36,.57), .25,.32)
    glass = f.mat('Opaque illustrated glazing', (.035,.075,.10), .2,.25)
    f.box('EV_BAY_ground_pad',(0,-.4,.015),(4.9,3.3,.03),m['steel'],.01)
    # Four tires touch the pad; a separate hub is visible on each outer face.
    for x in (-1.45,1.45):
        for y in (-.83,.83):
            f.cyl(f'EV_tire_{x}_{y}',(x,y,.345),.315,.22,m['black'],'Y')
            f.cyl(f'EV_hub_{x}_{y}',(x,y+(.12 if y>0 else -.12),.345),.19,.025,m['zinc'],'Y')
    f.box('EV_chassis',(0,0,.40),(3.55,1.50,.22),m['black'],.04)
    body=f.box('EV_BODY',(0,0,.73),(4.25,1.76,.60),paint,.16)
    # Real wheel-arch voids keep the tire envelopes out of the body shell.
    for x in (-1.45,1.45):
        for y in (-.83,.83):
            cutter=f.cyl('ARCH_CUTTER',(x,y,.345),.35,.55,m['black'],'Y')
            modifier=body.modifiers.new('Wheel arch','BOOLEAN')
            modifier.operation='DIFFERENCE';modifier.object=cutter
            bpy.context.view_layer.objects.active=body
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            bpy.data.objects.remove(cutter,do_unlink=True)
    f.box('EV_cabin',(0,0,1.14),(2.20,1.55,.66),glass,.16)
    f.box('EV_roof',(0,0,1.485),(2.0,1.49,.08),paint,.04)
    for x in (-2.13,2.13):
        f.box(f'EV_bumper_{x}',(x,0,.54),(.12,1.65,.17),m['black'],.025)
        for y in (-.60,.60):
            f.box(f'EV_lamp_{x}_{y}',(x,y,.82),(.04,.30,.12),m['white'],.015)
    # Side inlet is outside the body skin and accessible to the separate plug.
    f.box('EV_inlet_mount',(-.8,-.90,1.05),(.26,.055,.22),m['black'],.02)
    f.cyl('EV_INLET',(-.8,-.939,1.05),.082,.025,m['zinc'],'Y')
    f.box('EVSE_base',(-1.40,-1.65,.08),(.70,.60,.10),m['zinc'])
    f.box('EVSE_pedestal',(-1.40,-1.65,.49),(.27,.25,.72),m['blue'])
    f.box('EVSE_housing',(-1.40,-1.65,1.23),(.58,.38,.92),m['white'],.055)
    f.box('EVSE_front_panel',(-1.40,-1.85,1.24),(.46,.025,.64),m['black'],.01)
    f.box('EVSE_status_window',(-1.40,-1.87,1.47),(.30,.018,.09),m['blue'])
    f.text('EVSE_label','EV LESSON',(-1.40,-1.879,1.12),.048,m['white'],(math.pi/2,0,0))
    route=[(-1.12,-1.65,1.12),(-1.0,-1.65,.72),(-.67,-1.57,.55),(-.42,-1.34,.68),(-.58,-1.15,.98),(-.8,-1.18,1.05)]
    for i,(a,b) in enumerate(zip(route,route[1:])):
        f.tube(f'EVSE_CABLE_{i}',a,b,.025,m['black'])

def connector(m):
    # Root is the plug center, not a ground-mounted shutter. Nose faces +Y.
    f.cyl('CONNECTOR_shell',(0,0,0),.082,.20,m['black'],'Y')
    f.cyl('CONNECTOR_nose',(0,.104,0),.067,.035,m['zinc'],'Y')
    f.box('CONNECTOR_grip',(0,-.02,-.07),(.10,.14,.17),m['blue'],.025)
    f.box('CONNECTOR_LATCH',(0,.025,.089),(.042,.13,.028),m['yellow'],.008)
    f.cyl('CONNECTOR_cable_gland',(0,-.125,0),.031,.065,m['black'],'Y')

def meter(m):
    f.box('METER_base',(0,0,.04),(.50,.42,.08),m['zinc'])
    f.box('METER_pedestal',(0,0,.68),(.10,.10,1.20),m['blue'])
    f.box('METER_enclosure',(0,0,1.37),(.48,.22,.40),m['white'],.025)
    f.box('METER_screen',(0,-.12,1.42),(.38,.018,.15),m['black'])
    f.text('METER_static_legend','NO LIVE VALUE',(0,-.132,1.42),.033,m['white'],(math.pi/2,0,0))
    f.text('METER_label','ENERGY kWh',(0,-.132,1.29),.043,m['black'],(math.pi/2,0,0))

def pulse(m):
    f.box('PULSE_module',(0,0,0),(.16,.055,.10),m['black'],.008)
    for x in (-.055,.055):
        f.box(f'PULSE_terminal_{x}',(x,0,-.066),(.034,.038,.030),m['zinc'])
    f.cyl('PULSE_LED',(0,-.032,0),.015,.012,m['yellow'],'Y',24)
    f.text('PULSE_label','PULSE',(0,-.038,.025),.016,m['white'],(math.pi/2,0,0))

def reader(m):
    f.box('READER_base',(0,0,.04),(.38,.34,.08),m['zinc'])
    f.box('READER_post',(0,0,.62),(.08,.08,1.08),m['blue'])
    f.box('READER_housing',(0,0,1.24),(.26,.16,.30),m['black'],.02)
    f.box('READER_scan_face',(0,-.088,1.26),(.20,.015,.18),m['blue'])
    f.text('READER_label','AUTH',(0,-.098,1.26),.043,m['white'],(math.pi/2,0,0))

catalog_path=ROOT/'assets/catalog/candidates.catalog.json'
register_path=ROOT/'assets/catalog/industrial-reference-register.json'
catalog=json.loads(catalog_path.read_text(encoding='utf-8'))
register=json.loads(register_path.read_text(encoding='utf-8'))
for slug,builder in [('ev_charger_bay',bay),('connector_latch',connector),('energy_meter',meter),('pulse_output_meter',pulse),('authorization_reader',reader)]:
    review=f.BASE/slug/'review'; archive=review/'historical_invalid_identity_20261006'
    archive.mkdir(parents=True,exist_ok=True)
    if not (review/'repair_scope.json').exists():
        for file in list(review.iterdir()):
            if file.is_file() and not (archive/file.name).exists(): file.rename(archive/file.name)
    f.save(slug,builder)
    graph=bpy.context.evaluated_depsgraph_get(); points=[]
    for o in bpy.context.scene.objects:
        if o.type=='MESH' and o.get('rungproof_asset'):
            e=o.evaluated_get(graph); points.extend(e.matrix_world@Vector(v) for v in e.bound_box)
    size=[max(p[a] for p in points)-min(p[a] for p in points) for a in range(3)]
    aid='training.accessory.'+slug+'.v1'; asset=next(a for a in catalog['assets'] if a['id']==aid)
    asset.pop('genericBasisAssetId',None)
    asset['bounds']=dict(widthM=size[0],heightM=size[2],depthM=size[1])
    asset['kinematics']=[]; asset['animationTags']=[]
    asset['quality']=dict(status='candidate',blindReviewId=None,recognitionConfidence=None,topologyReviewed=False,materialReviewed=False,scaleReviewed=False,animationReviewed=False)
    register['entries']=[e for e in register['entries'] if e['assetId']!=aid]
    (review/'repair_scope.json').write_text(json.dumps(dict(assetId=aid,status='candidate-unapproved',scope='Original illustrated EV lesson component; connected layout/runtime review pending. No protocol, electrical rating, energy calibration or hardware acceptance.',historicalEvidence='Inherited family recognition is archived and does not approve this model.'),indent=2)+'\n',encoding='utf-8')
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
register_path.write_text(json.dumps(register,indent=2)+'\n',encoding='utf-8')
print('EV_TRAINING_ASSETS_BUILT 5 original unapproved replacements')
