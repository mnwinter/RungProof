"""Original optical-profile fixture and static count display for scene 71.

Run with Blender and RUNGPROOF_PROJECT_ROOT. Neither model implements type
classification or reads a live count. Replaces incorrect pallet/shutter copies;
old recognition evidence is retained as historical and invalid for these models.
"""
from __future__ import annotations

import importlib.util
import json
import math
import os
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER'] = '__helpers_only__'
spec = importlib.util.spec_from_file_location('pallet_count_exports', ROOT/'tools/modeling/build_material_flow_assets.py')
flow = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(flow)
flow.BASE = ROOT/'assets/training_accessories'
bpy.context.preferences.filepaths.save_version = 0


def profile_fixture(m):
    # Blender Z is height. Posts also clear the imported drive/control/cable
    # envelope, which is wider than the 1.4 m carrying belt.
    for side in (-1, 1):
        y = side * 1.65
        flow.box(f'PROFILE_foot_{side}', (0, y, .035), (.40, .30, .07), m['zinc'])
        flow.box(f'PROFILE_post_{side}', (0, y, 1.105), (.075, .075, 2.07), m['blue'])
        for row, height in enumerate((1.25, 1.55)):
            flow.box(f'PROFILE_bracket_{side}_{row}', (0, y, height), (.13, .11, .08), m['alum'])
            flow.box(f'PROFILE_head_{side}_{row}', (0, y-side*.07, height), (.16, .10, .12), m['black'])
            flow.cyl(f'PROFILE_lens_{side}_{row}', (0, y-side*.125, height), .035, .016,
                     m['red'], 'Y', 32, False)
    flow.box('PROFILE_crossbeam', (0, 0, 2.105), (.095, 3.375, .075), m['blue'])
    flow.box('PROFILE_legend_plate', (0, -.057, 2.105), (.80, .025, .15), m['white'], collision=False)
    flow.text('PROFILE_static_legend', 'PROFILE FIXTURE', (0, -.073, 2.105), .06,
              m['black'], (math.pi/2, 0, 0))


def count_display(m):
    flow.box('COUNT_DISPLAY_base', (0, 0, .04), (.85, .70, .08), m['zinc'])
    flow.box('COUNT_DISPLAY_mast', (0, .14, .69), (.085, .085, 1.22), m['blue'])
    flow.box('COUNT_DISPLAY_housing', (0, .06, 1.51), (.82, .24, .48), m['steel'])
    flow.box('COUNT_DISPLAY_screen', (0, -.069, 1.51), (.70, .018, .34), m['black'], collision=False)
    # No invented count value: the scene currently has a BOOL batch-valid output.
    flow.text('COUNT_DISPLAY_static_legend', 'COUNT\nNO LIVE VALUE', (0, -.084, 1.51), .07,
              m['white'], (math.pi/2, 0, 0))


catalog_path = ROOT/'assets/catalog/candidates.catalog.json'
register_path = ROOT/'assets/catalog/industrial-reference-register.json'
catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
register = json.loads(register_path.read_text(encoding='utf-8'))
help_spec = importlib.util.spec_from_file_location('pallet_count_help', ROOT/'tools/generate_help_documents.py')
help_module = importlib.util.module_from_spec(help_spec)
assert help_spec.loader is not None
help_spec.loader.exec_module(help_module)
for slug, builder in (('pallet_type_sensor', profile_fixture), ('count_display', count_display)):
    review = flow.BASE/slug/'review'
    archive = review/'historical_invalid_identity_20261004'
    archive.mkdir(parents=True, exist_ok=True)
    for file in review.iterdir():
        if file.is_file() and file.name != '.gdignore' and not file.name.startswith(slug+'_'):
            # Do not overwrite an earlier archived record when the builder reruns.
            if not (archive/file.name).exists():
                file.rename(archive/file.name)
    flow.save(slug, builder)
    graph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and obj.get('rungproof_asset'):
            evaluated = obj.evaluated_get(graph)
            points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    assert abs(min(p.z for p in points)) < .001, f'{slug}: wrong floor datum'
    size = [max(p[a] for p in points)-min(p[a] for p in points) for a in range(3)]
    asset_id = f'training.accessory.{slug}.v1'
    asset = next(a for a in catalog['assets'] if a['id'] == asset_id)
    asset.pop('genericBasisAssetId', None)
    asset['bounds'] = dict(widthM=size[0], heightM=size[2], depthM=size[1])
    asset['kinematics'] = []
    asset['animationTags'] = []
    asset['quality'] = dict(status='candidate', blindReviewId=None, recognitionConfidence=None,
                           topologyReviewed=False, materialReviewed=False,
                           scaleReviewed=False, animationReviewed=False)
    register['entries'] = [e for e in register['entries'] if e['assetId'] != asset_id]
    (review/'repair_scope.json').write_text(json.dumps({
        'assetId': asset_id, 'status': 'candidate-unapproved',
        'scope': 'Original static training geometry; no live numeric display or type-classification algorithm.',
        'historicalEvidence': 'historical_invalid_identity_20261004 is invalid for the replacement geometry.',
    }, indent=2)+'\n', encoding='utf-8')
    help_text = help_module.asset_help(asset, 'candidate', {})
    help_text += ('\n## Replacement scope - 2026-10-04\n\n'
                  'Original static training model. Optical profile heads illustrate mounting only; '
                  'no type classifier is implemented. The count display reads COUNT / NO LIVE VALUE; '
                  'no numeric scene point is bound. Historical pallet/shutter recognition records '
                  'are invalid for the replacement. Independent approval remains pending.\n')
    (ROOT/'docs/help/assets'/f'{asset_id}.md').write_text(help_text, encoding='utf-8')
catalog_path.write_text(json.dumps(catalog, indent=2)+'\n', encoding='utf-8')
register_path.write_text(json.dumps(register, indent=2)+'\n', encoding='utf-8')
print('PALLET_COUNT_PROPS_BUILT original static training geometry; independent approval pending')
