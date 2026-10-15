"""Original filled sack for the offline indexing lesson; no OEM/material claim."""
import importlib.util
import json
import os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER'] = '__helpers_only__'
spec = importlib.util.spec_from_file_location('flow', ROOT/'tools/modeling/build_material_flow_assets.py')
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
flow.BASE = ROOT/'assets/training_accessories'


def bag(m):
    # Flat underside bears on the belt. Rounded, tapered rings and end seams
    # distinguish a filled flexible sack from the old carton/pusher copy.
    profile = [(-.22, 0), (.22, 0), (.28, .04), (.28, .30), (.22, .40),
               (0, .45), (-.22, .40), (-.28, .30), (-.28, .04)]
    rings = [(-.425, .65), (-.32, .92), (0, 1), (.32, .92), (.425, .65)]
    vertices = [(x, y*scale, z*scale) for x, scale in rings for y, z in profile]
    n = len(profile)
    faces = [tuple(reversed(range(n))), tuple(range((len(rings)-1)*n, len(rings)*n))]
    faces += [(r*n+j, r*n+(j+1)%n, (r+1)*n+(j+1)%n, (r+1)*n+j)
              for r in range(len(rings)-1) for j in range(n)]
    mesh = bpy.data.meshes.new('SackBodyMesh'); mesh.from_pydata(vertices, [], faces); mesh.update()
    body = bpy.data.objects.new('BAG_BODY', mesh); bpy.context.collection.objects.link(body)
    flow.finish(body, 'BAG_BODY', m['white'], .012, True)
    for x in (-.44, .44):
        flow.box(f'BAG_sealed_end_{x}', (x, 0, .17), (.03, .36, .035), m['carton'], .012)
    flow.box('BAG_top_label', (0, 0, .452), (.34, .20, .004), m['blue'], .008, False)
    flow.text('BAG_label_text', 'BAG', (0, 0, .456), .085, m['white'], rotation=(0, 0, 0))


slug = 'bag_product_load'
review = flow.BASE/slug/'review'
archive = review/'historical_invalid_identity_20261006'; archive.mkdir(parents=True, exist_ok=True)
legacy_names = {'blind_review.png', 'independent_recognition.json', 'visual_review.md',
                'carriage_and_guide.png', 'cylinder_and_valve.png', 'hero.png',
                'scale_reference.png', 'state_running.png', 'state_stopped.png',
                'transfer_alignment.png', 'underside.png', 'wireframe.png'}
legacy_names.update(f'pneumatic_pusher_{i:02d}.png' for i in range(1, 5))
for file in list(review.iterdir()):
    # Rebuilds retain the current sack views and repair record in place.
    if file.is_file() and file.name in legacy_names and not (archive/file.name).exists():
        file.rename(archive/file.name)
flow.save(slug, bag)
points = []
graph = bpy.context.evaluated_depsgraph_get()
for obj in bpy.context.scene.objects:
    if obj.type == 'MESH' and obj.get('rungproof_asset'):
        evaluated = obj.evaluated_get(graph)
        points.extend(evaluated.matrix_world @ Vector(c) for c in evaluated.bound_box)
assert abs(min(p.z for p in points)) < .001
size = [max(p[a] for p in points)-min(p[a] for p in points) for a in range(3)]
catalog_path = ROOT/'assets/catalog/candidates.catalog.json'
catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
asset_id = 'training.accessory.bag_product_load.v1'
asset = next(a for a in catalog['assets'] if a['id'] == asset_id)
asset.pop('genericBasisAssetId', None)
asset['bounds'] = dict(widthM=size[0], heightM=size[2], depthM=size[1])
asset['kinematics'] = []; asset['animationTags'] = []
asset['quality'] = dict(status='candidate', blindReviewId=None, recognitionConfidence=None,
                        topologyReviewed=False, materialReviewed=False, scaleReviewed=False, animationReviewed=False)
catalog_path.write_text(json.dumps(catalog, indent=2)+'\n', encoding='utf-8')
register_path = ROOT/'assets/catalog/industrial-reference-register.json'
register = json.loads(register_path.read_text(encoding='utf-8'))
register['entries'] = [e for e in register['entries'] if e['assetId'] != asset_id]
register_path.write_text(json.dumps(register, indent=2)+'\n', encoding='utf-8')
(review/'repair_scope.json').write_text(json.dumps(dict(assetId=asset_id, status='candidate-unapproved',
    scope='Original illustrated filled sack, flat underside, rounded profile and sealed ends. No flexible-body physics or material claim.',
    historicalEvidence='historical_invalid_identity_20261006 is invalid for this replacement.'), indent=2)+'\n', encoding='utf-8')
print('BAG_INDEX_ASSET_BUILT 1 original unapproved sack')
