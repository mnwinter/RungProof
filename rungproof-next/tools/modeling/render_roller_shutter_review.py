"""Render acceptance evidence for the motorized industrial roller shutter."""
from __future__ import annotations
import os, shutil
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);ASSET=ROOT/"assets"/"scene_core"/"motorized_roller_shutter";SOURCE=ASSET/"source"/"motorized_roller_shutter.blend";REVIEW=ASSET/"review"
def aim(obj,target):obj.rotation_euler=(target-obj.location).to_track_quat("-Z","Y").to_euler()
def mat(name,c):item=bpy.data.materials.new(name);item.diffuse_color=(*c,1);item.metallic=.08;item.roughness=.48;return item
def bounds(objs):
 lo=Vector((float("inf"),)*3);hi=Vector((float("-inf"),)*3)
 for obj in objs:
  if obj.type not in {"MESH","CURVE","FONT"}:continue
  for corner in obj.bound_box:
   p=obj.matrix_world@Vector(corner);lo=Vector((min(lo.x,p.x),min(lo.y,p.y),min(lo.z,p.z)));hi=Vector((max(hi.x,p.x),max(hi.y,p.y),max(hi.z,p.z)))
 return lo,hi
def render(scene,camera,path,loc,target):camera.location=loc;aim(camera,target);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
def raised(scene):
 # Mirrors the Godot shutter stack: slats compact under the header and the
 # bottom bar rises below them. This is offline pose evidence, not PLC proof.
 slats=[obj for obj in scene.objects if obj.name.startswith("KIN_slat_")];top=max(obj.location.z for obj in slats)+.10
 for obj in slats:
  index=int(obj.name.removeprefix("KIN_slat_"));obj.location.z=top-index*.025
 bottom=scene.objects["KIN_bottom_bar"];bottom.location.z=top-.50
def main():
 REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
 for obj in list(scene.objects):
  if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
 visual=[obj for obj in scene.objects if obj.get("rungproof_asset")];lo,hi=bounds(visual);center=(lo+hi)/2
 bpy.ops.mesh.primitive_plane_add(size=14,location=(center.x,center.y,lo.z-.03));floor=bpy.context.object;floor.data.materials.append(mat("Review floor",(.06,.08,.10)))
 world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.012,.018,.026)
 for i,(offset,energy,size) in enumerate((((6,-6,6),1600,5),((-5,-3,4),850,3),((0,5,5),950,3))):
  data=bpy.data.lights.new(f"Review light {i}","AREA");data.energy,data.shape,data.size=energy,"DISK",size;light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset);aim(light,center)
 data=bpy.data.cameras.new("Review camera");data.lens=55;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
 scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
 hero=Vector((5.8,-6.0,4.5));target=Vector((0,-.10,2.0));render(scene,camera,REVIEW/"hero.png",hero,target);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
 render(scene,camera,REVIEW/"guide_and_safety_edge.png",Vector((-4.2,-4.7,1.9)),Vector((-2.05,-.13,1.25)))
 render(scene,camera,REVIEW/"roll_and_drive.png",Vector((5.2,-4.5,4.5)),Vector((2.25,0,3.70)))
 render(scene,camera,REVIEW/"header_and_end_supports.png",Vector((-3.8,3.8,4.5)),Vector((0,0,3.72)))
 render(scene,camera,REVIEW/"controls_and_motor_cable.png",Vector((4.5,-3.8,3.5)),Vector((2.63,-.25,3.35)))
 render(scene,camera,REVIEW/"state_closed.png",hero,target);render(scene,camera,REVIEW/"state_stopped.png",hero,target);raised(scene);render(scene,camera,REVIEW/"state_open.png",hero,target);render(scene,camera,REVIEW/"state_running.png",hero,target)
 bpy.ops.mesh.primitive_cube_add(size=1,location=(hi.x+.85,center.y,lo.z+.5));scale=bpy.context.object;scale.data.materials.append(mat("One metre reference",(.9,.18,.02)));render(scene,camera,REVIEW/"scale_reference.png",Vector((5.8,-5.8,4.1)),(center+scale.location)/2);bpy.data.objects.remove(scale,do_unlink=True)
 wire=mat("Review wire",(.02,.85,1));copies=[]
 for obj in visual:
  if obj.type not in {"MESH","CURVE","FONT"}:continue
  obj.hide_render=True;clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone)
  if clone.type=="MESH":mod=clone.modifiers.new("Review wireframe","WIREFRAME");mod.thickness=.0025
  copies.append(clone)
 render(scene,camera,REVIEW/"wireframe.png",hero,target)
 for obj in visual:obj.hide_render=False
 for clone in copies:bpy.data.objects.remove(clone,do_unlink=True)
 shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png");print(f"ROLLER_SHUTTER_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
