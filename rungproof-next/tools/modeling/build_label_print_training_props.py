"""Original food tray, weigh deck, desktop printer and static label preview.

Run with Blender and RUNGPROOF_PROJECT_ROOT. These are illustrative training
geometry, not an OEM model, weighing simulation, label formatter or printer I/O.
Only the four named packages are rebuilt; inherited approval is not retained.
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
spec = importlib.util.spec_from_file_location('label_prop_exports', ROOT/'tools/modeling/build_material_flow_assets.py')
flow = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(flow)
flow.BASE = ROOT/'assets/training_accessories'
bpy.context.preferences.filepaths.save_version = 0


def food_tray(m):
    # Datum is the tray bottom, to be placed on the scene's actual weigh deck.
    cream = flow.mat('Food tray', (.78, .75, .62), 0, .6)
    food = flow.mat('Illustrative packaged food', (.72, .42, .28), 0, .7)
    flow.box('FOOD_TRAY_bottom', (0, 0, .015), (.52, .36, .03), cream, .01)
    for x in (-.25, .25):
        flow.box(f'FOOD_TRAY_end_{x}', (x, 0, .055), (.02, .36, .08), cream, .005)
    for y in (-.17, .17):
        flow.box(f'FOOD_TRAY_side_{y}', (0, y, .055), (.48, .02, .08), cream, .005)
    for x in (-.13, .13):
        flow.box(f'FOOD_PORTION_{x}', (x, 0, .069), (.21, .26, .075), food, .035)
    # Raised neutral paper label; no invented measured weight or real batch data.
    flow.box('FOOD_TRAY_demo_label', (0, .07, .11), (.28, .13, .008), m['white'], .004, False)
    flow.text('FOOD_TRAY_demo_text', 'TRAINING', (0, .07, .116), .027, m['black'], (0, 0, 0))


def checkweigher(m):
    # Blender Z is height. The carrying surface is exactly 0.900 m.
    flow.box('WEIGH_DECK_surface', (0, 0, .88), (1.8, 1.0, .04), m['black'], .005)
    flow.box('WEIGH_DECK_pan', (0, 0, .825), (1.65, .94, .07), m['alum'], .006)
    for y in (-.56, .56):
        flow.box(f'WEIGH_FRAME_side_{y}', (0, y, .785), (1.8, .09, .16), m['alum'])
    for x in (-.65, .65):
        flow.box(f'WEIGH_FRAME_cross_{x}', (x, 0, .72), (.08, 1.18, .08), m['alum'])
        for y in (-.54, .54):
            flow.box(f'WEIGH_FOOT_{x}_{y}', (x, y, .035), (.22, .22, .07), m['zinc'])
            flow.box(f'WEIGH_LEG_{x}_{y}', (x, y, .385), (.065, .065, .63), m['alum'])
    for x in (-.45, .45):
        flow.box(f'WEIGH_load_cell_{x}', (x, 0, .773), (.22, .10, .034), m['zinc'])
    # A stand-mounted readout behind the deck, outside the product envelope.
    flow.box('WEIGH_readout_mast', (0, .79, .725), (.07, .07, 1.31), m['alum'])
    flow.box('WEIGH_readout_foot', (0, .79, .035), (.32, .32, .07), m['zinc'])
    flow.box('WEIGH_readout_housing', (0, .79, 1.39), (.58, .16, .31), m['steel'])
    flow.box('WEIGH_readout_screen', (0, .702, 1.41), (.49, .018, .17), m['black'], collision=False)
    flow.text('WEIGH_readout_static_text', 'DEMO', (0, .687, 1.41), .08, m['white'], (math.pi/2, 0, 0))
    flow.text('WEIGH_station_label', 'WEIGH DECK', (0, -.611, .80), .07, m['black'], (math.pi/2, 0, 0))


def printer(m):
    # An original desktop label-printer silhouette on a grounded workbench.
    flow.box('PRINTER_BENCH_top', (0, 0, 1.025), (1.5, 1.1, .05), m['alum'])
    for x in (-.55, .55):
        for y in (-.40, .40):
            flow.box(f'PRINTER_BENCH_foot_{x}_{y}', (x, y, .03), (.20, .20, .06), m['zinc'])
            flow.box(f'PRINTER_BENCH_leg_{x}_{y}', (x, y, .53), (.065, .065, .94), m['steel'])
        flow.box(f'PRINTER_BENCH_cross_{x}', (x, 0, .30), (.065, .865, .065), m['steel'])
    flow.box('PRINTER_body', (0, .10, 1.30), (.70, .50, .50), m['steel'], .025)
    flow.box('PRINTER_lid', (0, .10, 1.61), (.66, .46, .12), m['blue'], .018)
    flow.box('PRINTER_control_screen', (0, -.159, 1.44), (.35, .015, .10), m['black'], collision=False)
    flow.text('PRINTER_static_legend', 'DEMO PRINTER', (0, -.171, 1.44), .035, m['white'], (math.pi/2, 0, 0))
    flow.box('PRINTER_exit_slot', (0, -.156, 1.15), (.55, .014, .055), m['black'])
    flow.box('PRINTER_output_tray', (0, -.34, 1.10), (.63, .38, .04), m['alum'])
    # Static sample emerging from the front slot; not an actual print operation.
    flow.box('PRINTER_demo_paper', (0, -.33, 1.124), (.44, .34, .008), m['white'], .002, False)
    flow.text('PRINTER_demo_paper_text', 'DEMO LABEL', (0, -.35, 1.130), .038, m['black'], (0, 0, 0))


def label_display(m):
    flow.box('LABEL_DISPLAY_base', (0, .08, .04), (1.55, .95, .08), m['zinc'])
    flow.box('LABEL_DISPLAY_mast', (0, .16, .98), (.08, .08, 1.8), m['steel'])
    flow.box('LABEL_DISPLAY_backboard', (0, .11, 1.47), (1.30, .04, .70), m['alum'])
    flow.box('LABEL_DISPLAY_enclosure', (0, -.015, 1.47), (1.30, .21, .70), m['steel'])
    flow.box('LABEL_DISPLAY_screen', (0, -.128, 1.47), (1.10, .015, .50), m['white'], collision=False)
    flow.text('LABEL_DISPLAY_static_preview', 'DEMO LABEL\nNO LIVE DATA', (0, -.145, 1.47), .092,
              m['black'], (math.pi/2, 0, 0))


catalog_path = ROOT/'assets/catalog/candidates.catalog.json'
register_path = ROOT/'assets/catalog/industrial-reference-register.json'
catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
register = json.loads(register_path.read_text(encoding='utf-8'))
for slug, builder in (('food_product_load', food_tray), ('checkweigher', checkweigher),
                      ('label_printer', printer), ('formatted_label_display', label_display)):
    flow.save(slug, builder)
    graph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and obj.get('rungproof_asset'):
            evaluated = obj.evaluated_get(graph)
            points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    assert abs(min(p.z for p in points)) < .001, f'{slug}: wrong bottom datum'
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
catalog_path.write_text(json.dumps(catalog, indent=2)+'\n', encoding='utf-8')
register_path.write_text(json.dumps(register, indent=2)+'\n', encoding='utf-8')
print('LABEL_PRINT_PROPS_BUILT original static training geometry; independent approval pending')
