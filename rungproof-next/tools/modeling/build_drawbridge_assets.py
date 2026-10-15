"""Original illustrative bascule bridge, barriers and cam limits; no OEM or road-design claim.
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


def deck(m):
    road = flow.mat('Bridge road surface', (.08, .09, .10), 0, .9)
    concrete = flow.mat('Bridge concrete', (.39, .42, .43), 0, .9)
    water = flow.mat('Illustrative water', (.025, .16, .23), 0, .3)
    flow.box('CHANNEL_water', (0, 0, .02), (4.0, 7.4, .04), water, collision=False)
    for side in (-1, 1):
        center = side*5.005
        flow.box(f'APPROACH_{side}', (center, 0, 1.28), (5.99, 4.2, .24), road)
        for y in (-1.6, 1.6):
            flow.box(f'APPROACH_pier_{side}_{y}', (center, y, .58), (2.2, .45, 1.16), concrete)
        flow.box(f'APPROACH_centerline_{side}', (center, 0, 1.404), (5.7, .06, .008), m['yellow'], collision=False)
        # Leave hinge-side rail clearance for the swept raised deck railing.
        for y in (-2.02, 2.02):
            for x in (side*4.0, side*6.0, side*7.5):
                flow.tube(f'APPROACH_post_{side}_{x}_{y}', (x, y, 1.4), (x, y, 2.3), .035, m['zinc'])
            flow.tube(f'APPROACH_rail_{side}_{y}', (side*3.2, y, 2.3), (side*7.8, y, 2.3), .035, m['zinc'])
    for y in (-2.35, 2.35):
        flow.box(f'HINGE_pier_{y}', (-2, y, .58), (.65, .55, 1.16), concrete)
        flow.box(f'HINGE_bearing_{y}', (-2, y, 1.275), (.38, .30, .23), m['steel'])
    for side in (-1, 1):
        flow.box(f'GATE_support_platform_{side}', (side*3.6, side*2.7, .7), (.9, .9, 1.4), concrete)
    flow.cyl('HINGE_shaft', (-2, 0, 1.4), .09, 6.05, m['zinc'], 'Y')
    # Top-surface pivot prevents the underside sweeping into the left approach.
    flow.box('KIN_deck_surface', (0, 0, 1.3), (4, 3.8, .2), road)
    flow.box('KIN_deck_centerline', (0, 0, 1.404), (3.8, .06, .008), m['yellow'], collision=False)
    for y in (-1.5, 1.5):
        flow.box(f'LANDING_seat_{y}', (1.82, y, .58), (.32, .4, 1.16), concrete)
        flow.box(f'LANDING_pad_{y}', (1.82, y, 1.18), (.32, .4, .04), m['black'])
    for y in (-1.85, 1.85):
        for x in (-1.65, -.5, .5, 1.65):
            flow.tube(f'KIN_deck_post_{x}_{y}', (x, y, 1.4), (x, y, 2.3), .035, m['zinc'])
        flow.tube(f'KIN_deck_rail_{y}', (-1.65, y, 2.3), (1.65, y, 2.3), .035, m['zinc'])


def barrier(m):
    flow.box('GATE_base', (0, 0, .04), (.60, .6, .08), m['zinc'])
    flow.box('GATE_cabinet', (0, .10, .58), (.42, .38, 1.0), m['steel'], .02)
    flow.cyl('GATE_hinge', (0, 0, 1.2), .16, .38, m['zinc'], 'Y')
    flow.box('KIN_gate_arm', (2.4, -.05, 1.2), (4.8, .12, .10), m['white'])
    # Sparse red identification blocks are on the boom, not a UI background.
    for i in range(5):
        flow.box(f'KIN_gate_red_mark_{i}', (.55+i*.85, -.116, 1.2), (.30, .012, .092), m['red'], collision=False)
    flow.box('KIN_gate_end', (4.76, -.05, 1.2), (.08, .14, .14), m['red'])


def limits(m):
    flow.box('LIMIT_ground_base', (-.65, 0, .035), (.40, .35, .07), m['zinc'])
    flow.box('LIMIT_mast', (-.65, .08, .735), (.07, .07, 1.33), m['steel'])
    flow.box('LIMIT_mount_plate', (-.10, .12, 1.4), (1.55, .07, .95), m['steel'])
    flow.cyl('KIN_cam_disc', (0, -.04, 1.4), .24, .065, m['zinc'], 'Y')
    flow.cyl('KIN_cam_lobe', (.26, -.04, 1.4), .06, .07, m['brass'], 'Y')
    for label, angle, color in [('home', 0, 'green'), ('raised', 70, 'amber')]:
        a = math.radians(angle)
        x, z = .54*math.cos(a), 1.4+.54*math.sin(a)
        material = m['green'] if color == 'green' else m['yellow']
        flow.box('LIMIT_housing_'+label, (x, -.04, z), (.17, .16, .20), m['blue'])
        flow.cyl('LENS_'+color+'_'+label, (x, -.132, z), .026, .018, material, 'Y', collision=False)
        # Fixed roller is reached only by the cam lobe at its actual limit angle.
        rx, rz = .34*math.cos(a), 1.4+.34*math.sin(a)
        flow.tube('LIMIT_lever_'+label, (x, -.04, z), (rx, -.04, rz), .014, m['zinc'])
        flow.cyl('LIMIT_roller_'+label, (rx, -.04, rz), .025, .085, m['black'], 'Y')


catalog_path = ROOT/'assets/catalog/candidates.catalog.json'
register_path = ROOT/'assets/catalog/industrial-reference-register.json'
catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
register = json.loads(register_path.read_text(encoding='utf-8'))
for slug, builder in (('drawbridge_deck', deck), ('road_barrier', barrier), ('bridge_limit_switches', limits)):
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
        scope='Original bascule bridge, boom and cam-limit geometry. No load, road engineering, hydraulic design or OEM claim.',
        historicalEvidence='historical_invalid_identity_20261006 is invalid for the replacement geometry.'), indent=2)+'\n', encoding='utf-8')
catalog_path.write_text(json.dumps(catalog, indent=2)+'\n', encoding='utf-8')
register_path.write_text(json.dumps(register, indent=2)+'\n', encoding='utf-8')
print('DRAWBRIDGE_ASSETS_BUILT 3 original unapproved models')
