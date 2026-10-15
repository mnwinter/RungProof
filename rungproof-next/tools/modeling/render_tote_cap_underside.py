"""Render the actual exported cap underside; run from project root."""
import bpy
from pathlib import Path
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(Path('assets/material_flow/tote_finishing_open_ibc/delivery/tote_finishing_open_ibc.glb').resolve()))
for obj in bpy.context.scene.objects:obj.hide_render=obj.name!='IBC_fill_cap'
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
world=bpy.data.worlds.new('Cap review world');scene.world=world;world.color=(.035,.035,.035)
for i,loc in enumerate(((1,-1,.5),(-1,-1,.8),(0,1,.8))):
 d=bpy.data.lights.new('Cap review light '+str(i),'AREA');d.energy=90;d.size=.8;o=bpy.data.objects.new(d.name,d);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.54))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Cap underside camera');cam=bpy.data.objects.new(d.name,d);bpy.context.collection.objects.link(cam);scene.camera=cam;d.lens=58;cam.location=(.5,-.6,1.1);cam.rotation_euler=(Vector((0,0,1.54))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(Path('assets/material_flow/tote_finishing_open_ibc/review/cap_open_underside.png').resolve());bpy.ops.render.render(write_still=True)
print('TOTE_CAP_UNDERSIDE_RENDERED actual exported mesh only')
