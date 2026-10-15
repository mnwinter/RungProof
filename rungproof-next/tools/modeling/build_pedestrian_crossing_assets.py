"""Original illustrative crossing models, not public-road design or certified hardware.
Blender Z becomes Godot Y; signal faces point toward Godot +Z.
"""
import importlib.util
import json
import math
import os
from pathlib import Path
import bpy
from mathutils import Vector
ROOT = Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER'] = '__helpers_only__'
spec = importlib.util.spec_from_file_location('crossing_exports', ROOT/'tools/modeling/build_material_flow_assets.py')
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
flow.BASE = ROOT/'assets/training_accessories'
bpy.context.preferences.filepaths.save_version = 0


def road(m):
    asphalt = flow.mat('Asphalt', (.075, .082, .087), 0, .95)
    concrete = flow.mat('Sidewalk concrete', (.43, .45, .44), 0, .9)
    flow.box('ROAD_horizontal_surface', (0, 0, .05), (10, 6, .10), asphalt)
    for side in (-1, 1):
        flow.box(f'SIDEWALK_{side}', (0, side*3.8, .075), (10, 1.6, .15), concrete)
        # Stop lines sit outside the pedestrian crossing in their approach lanes.
        flow.box(f'STOP_LINE_{side}', (side*1.65, side*1.5, .103), (.12, 2.85, .006), m['white'], collision=False)
    for i in range(9):
        flow.box(f'CROSSWALK_mark_{i}', (0, -2.7+i*.675, .103), (2.4, .36, .006), m['white'], collision=False)
    for side in (-1, 1):
        flow.box(f'CENTER_LINE_{side}', (side*3.35, 0, .103), (3.3, .07, .006), m['yellow'], collision=False)


def mast(m, height):
    flow.box('MAST_base', (0, .13, .035), (.42, .38, .07), m['zinc'])
    flow.cyl('MAST_support', (0, .13, height/2), .065, height, m['zinc'])


def vehicle(m):
    mast(m, 3.55)
    flow.box('VEHICLE_backplate', (0, 0, 2.96), (.70, .10, 1.58), m['black'], .035)
    for color, z in (('red', 3.43), ('amber', 2.96), ('green', 2.49)):
        flow.cyl('HOUSING_'+color, (0, -.11, z), .225, .19, m['black'], 'Y')
        material = m['red'] if color == 'red' else m['yellow'] if color == 'amber' else m['green']
        flow.cyl('LENS_'+color, (0, -.215, z), .175, .025, material, 'Y', collision=False)
        # Upper visor leaves the forward lens unobstructed.
        flow.box('VISOR_'+color, (0, -.25, z+.225), (.44, .28, .04), m['black'])


def join_symbol(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj['rungproof_asset'] = True
    obj['rungproof_collision'] = False


def pedestrian(m):
    mast(m, 3.35)
    flow.box('PEDESTRIAN_two_section_housing', (0, 0, 2.86), (.70, .25, 1.15), m['black'], .035)
    for z in (3.14, 2.58):
        flow.box('PEDESTRIAN_screen_'+str(z), (0, -.137, z), (.58, .018, .48), m['black'], collision=False)
        flow.box('PEDESTRIAN_visor_'+str(z), (0, -.23, z+.26), (.68, .22, .035), m['black'])
    orange = flow.mat('Pedestrian orange', (.94, .25, .012), 0, .45)
    hand = [flow.box('hand_palm', (0, -.154, 3.12), (.20, .015, .18), orange, collision=False)]
    for i, length in enumerate((.14, .19, .20, .17)):
        hand.append(flow.box('hand_finger_'+str(i), (-.075+i*.05, -.154, 3.20+length/2), (.037, .015, length), orange, collision=False))
    hand.append(flow.box('hand_thumb', (-.14, -.154, 3.12), (.11, .015, .045), orange, collision=False, rotation=(0, -.65, 0)))
    join_symbol(hand, 'LENS_orange_hand')
    walk = [flow.cyl('walk_head', (0, -.155, 2.75), .046, .016, m['white'], 'Y', collision=False)]
    def limb(name, a, b, radius=.026):
        walk.append(flow.tube(name, (a[0], -.155, a[1]), (b[0], -.155, b[1]), radius, m['white'], False))
    limb('walk_body', (0, 2.68), (-.03, 2.56), .033)
    limb('walk_arm_1', (-.008, 2.64), (-.12, 2.60))
    limb('walk_arm_2', (-.008, 2.64), (.12, 2.57))
    limb('walk_leg_1', (-.03, 2.56), (-.13, 2.39))
    limb('walk_leg_2', (-.03, 2.56), (.09, 2.43))
    join_symbol(walk, 'LENS_white_walk')


catalog_path = ROOT/'assets/catalog/candidates.catalog.json'
register_path = ROOT/'assets/catalog/industrial-reference-register.json'
catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
register = json.loads(register_path.read_text(encoding='utf-8'))
for slug, builder in (('crosswalk_road_module', road), ('traffic_signal_head', vehicle), ('pedestrian_signal_head', pedestrian)):
    review = flow.BASE/slug/'review'
    archive = review/'historical_invalid_identity_20261006'
    archive.mkdir(parents=True, exist_ok=True)
    for file in list(review.iterdir()):
        if file.is_file() and file.name != '.gdignore' and not (archive/file.name).exists():
            file.rename(archive/file.name)
    flow.save(slug, builder)
    graph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and obj.get('rungproof_asset'):
            evaluated = obj.evaluated_get(graph)
            points.extend(evaluated.matrix_world @ Vector(c) for c in evaluated.bound_box)
    assert abs(min(p.z for p in points)) < .001
    size = [max(p[a] for p in points)-min(p[a] for p in points) for a in range(3)]
    asset_id = f'training.accessory.{slug}.v1'
    asset = next(a for a in catalog['assets'] if a['id'] == asset_id)
    asset.pop('genericBasisAssetId', None)
    asset['bounds'] = dict(widthM=size[0], heightM=size[2], depthM=size[1])
    asset['kinematics'] = []
    asset['animationTags'] = []
    asset['quality'] = dict(status='candidate', blindReviewId=None, recognitionConfidence=None,
                           topologyReviewed=False, materialReviewed=False, scaleReviewed=False, animationReviewed=False)
    register['entries'] = [e for e in register['entries'] if e['assetId'] != asset_id]
    (review/'repair_scope.json').write_text(json.dumps(dict(assetId=asset_id, status='candidate-unapproved',
        scope='Original illustrative geometry. Signal symbol/color reference only; no public-road timing, accessibility, certification or OEM claim.',
        visualReference='https://mutcd.fhwa.dot.gov/htm/2009r1r2/part4/part4e.htm',
        historicalEvidence='historical_invalid_identity_20261006 is invalid for the replacement geometry.'), indent=2)+'\n', encoding='utf-8')
catalog_path.write_text(json.dumps(catalog, indent=2)+'\n', encoding='utf-8')
register_path.write_text(json.dumps(register, indent=2)+'\n', encoding='utf-8')
print('PEDESTRIAN_CROSSING_ASSETS_BUILT 3 original unapproved models')
