"""Scene-specific hollow IBC candidate, derived from the generic closed master.

The exported cap remains a separate mesh. The finishing installation hides it
until cap application is implemented. No inherited approval or volume rating.
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
spec = importlib.util.spec_from_file_location('flow', ROOT/'tools/modeling/build_material_flow_assets.py')
f = importlib.util.module_from_spec(spec)
spec.loader.exec_module(f)
SLUG = 'tote_finishing_open_ibc'


def apply_difference(body, cutter):
    modifier = body.modifiers.new('Actual fill cavity', 'BOOLEAN')
    modifier.operation = 'DIFFERENCE'
    modifier.solver = 'EXACT'
    modifier.object = cutter
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def build(materials):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/factory_kit/ibc_tote/source/ibc_tote.blend'))
    # Opening a Blend replaces datablocks, including the helper's materials.
    materials.clear()
    materials.update(f.common())
    for obj in list(bpy.data.objects):
        if obj.type != 'MESH' or not obj.name.startswith('IBC_'):
            bpy.data.objects.remove(obj, do_unlink=True)
        else:
            obj['rungproof_asset'] = True
            obj['rungproof_collision'] = True
    body = bpy.data.objects['IBC_tank']
    bpy.context.view_layer.objects.active = body
    for modifier in list(body.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    # Cavity leaves the bottom and outer rounded shell intact; the separate
    # cylindrical cut connects that cavity through the roof to the fill neck.
    cutter = f.box('CAVITY_CUTTER', (0, 0, .78), (.99, .94, 1.34), None, .10)
    bpy.context.view_layer.objects.active = cutter
    for modifier in list(cutter.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    apply_difference(body, cutter)
    apply_difference(body, f.cyl('PORT_CUTTER', (0, 0, 1.51), .11, .30, None, 'Z', 96))
    neck = f.cyl('IBC_open_fill_neck', (0, 0, 1.5125), .14, .095, body.data.materials[0], 'Z', 96)
    apply_difference(neck, f.cyl('NECK_BORE_CUTTER', (0, 0, 1.5125), .11, .14, None, 'Z', 96))
    original_cap = bpy.data.objects['IBC_fill_cap']
    cap_material = original_cap.data.materials[0]
    bpy.data.objects.remove(original_cap, do_unlink=True)
    cap = f.cyl('IBC_fill_cap', (0, 0, 1.54), .16, .08, cap_material, 'Z', 96)
    # The open underside clears the .14 m neck radius by 5 mm. Its inner
    # roof seats at Z 1.56; the .02 m roof remains above the neck, not in it.
    apply_difference(cap, f.cyl('CAP_CAVITY_CUTTER', (0, 0, 1.50), .145, .12, None, 'Z', 96))
    f.box('IBC_fill_witness', (0, 0, .75), (.90, .85, 1.20),
        f.mat('Illustrative fill witness', (.03, .22, .58), 0, .20), 0, False)
    body['candidate_scope'] = 'Illustrative hollow fill geometry; no seal, capacity, chemical or mechanical approval.'


f.save(SLUG, build)
root = f.BASE/SLUG
# Additional open-port view corresponds to the scene installation, which hides
# only the separate cap. Delivery/source retain it for future cap application.
bpy.data.objects['IBC_fill_cap'].hide_render = True
bpy.data.objects['IBC_fill_witness'].hide_render = True
camera = bpy.context.scene.camera
camera.location = (0, 0, 4.2)
f.point(camera, Vector((0, 0, .78)))
bpy.context.scene.render.filepath = str(root/'review'/'open_port_top.png')
bpy.ops.render.render(write_still=True)
graph = bpy.context.evaluated_depsgraph_get()
points = []
for obj in bpy.context.scene.objects:
    if obj.type == 'MESH' and obj.get('rungproof_asset'):
        evaluated = obj.evaluated_get(graph)
        points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
size = [max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
centre = Vector(tuple((max(p[i] for p in points)+min(p[i] for p in points))/2 for i in range(3)))
radius = max(Vector(size).length*1.2, 2.3)
for index, angle in enumerate((35, 125, 215, 305), start=1):
    radians = math.radians(angle)
    camera.location = (centre.x+radius*math.cos(radians), centre.y+radius*math.sin(radians), centre.z+radius*.48)
    f.point(camera, centre)
    bpy.context.scene.render.filepath = str(root/'review'/f'open_port_{index:02d}.png')
    bpy.ops.render.render(write_still=True)
catalog_path = ROOT/'assets/catalog/candidates.catalog.json'
catalog = json.loads(catalog_path.read_text())
asset_id = 'loads.ibc.open-finishing.v1'
asset = dict(id=asset_id, displayName='Open-fill IBC - finishing installation candidate',
    category='loads/containers', tags=['ibc tote', 'open fill port', 'finishing installation'],
    model=dict(sourceBlend=f'res://assets/material_flow/{SLUG}/source/{SLUG}.blend',
        deliveryGltf=f'res://assets/material_flow/{SLUG}/delivery/{SLUG}.glb', lodFiles=[],
        collisionFile=f'res://assets/material_flow/{SLUG}/collision/{SLUG}_collision.glb',
        thumbnailFile=f'res://assets/material_flow/{SLUG}/thumbnail.png'),
    bounds=dict(widthM=size[0], heightM=size[2], depthM=size[1]), connectors=[], kinematics=[], animationTags=[], signals=[],
    quality=dict(status='candidate', blindReviewId=None, recognitionConfidence=None, topologyReviewed=False,
        materialReviewed=False, scaleReviewed=False, animationReviewed=False))
catalog['assets'] = [a for a in catalog['assets'] if a['id'] != asset_id] + [asset]
catalog_path.write_text(json.dumps(catalog, indent=2)+'\n')
(root/'review'/'repair_scope.json').write_text(json.dumps(dict(assetId=asset_id,
    status='candidate-unapproved', scope='Scene-specific cavity and fill aperture; separate cap retained in delivery.',
    sourceBasis='Generic closed IBC master remains unchanged.',
    nativeSceneReview='pending', fillVolume='uncalibrated; normalized percentage only', capApplication='not implemented'), indent=2)+'\n')
print('TOTE_OPEN_IBC_CANDIDATE_BUILT', asset_id, size)
