"""Inspect normalized level witness at 40/100%; no native scene acceptance."""
import importlib.util
import os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER'] = '__helpers_only__'
spec = importlib.util.spec_from_file_location('flow', ROOT/'tools/modeling/build_material_flow_assets.py')
f = importlib.util.module_from_spec(spec)
spec.loader.exec_module(f)
root = ROOT/'assets/material_flow/tote_finishing_open_ibc'
bpy.ops.wm.open_mainfile(filepath=str(root/'source/tote_finishing_open_ibc.blend'))
bpy.data.objects['IBC_fill_cap'].hide_render = True
witness = bpy.data.objects['IBC_fill_witness']
home = witness.matrix_world.copy()
bottom = min((home @ Vector(corner)).z for corner in witness.bound_box)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
world = scene.world or bpy.data.worlds.new('Review world')
scene.world = world
world.color = (.025, .030, .035)
for index, location in enumerate(((4, -4, 6), (-3, -1, 3), (0, 4, 4))):
    light = bpy.data.lights.new('Fill review light '+str(index), 'AREA')
    light.energy = 1100 if index == 0 else 750
    light.size = 4
    obj = bpy.data.objects.new(light.name, light)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    f.point(obj, Vector((0, 0, .78)))
camera = bpy.data.objects.new('Fill witness review camera', bpy.data.cameras.new('Fill witness review camera'))
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.data.lens = 58
camera.location = (0, 0, 4.2)
f.point(camera, Vector((0, 0, .78)))
for percent in (40, 100):
    fraction = percent/100
    witness.matrix_world = home
    witness.scale.z *= fraction
    witness.location.z = bottom + fraction*(home.translation.z-bottom)
    scene.render.filepath = str(root/'review'/f'fill_witness_{percent}_top.png')
    bpy.ops.render.render(write_still=True)
print('TOTE_FILL_WITNESS_REVIEW_RENDERED 40 100 normalized asset views only')
