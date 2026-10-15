"""Render acceptance evidence for the enclosed machining center."""
from __future__ import annotations
import os, shutil
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);ASSET=ROOT/"assets"/"scene_core"/"enclosed_machine_center";SOURCE=ASSET/"source"/"enclosed_machine_center.blend";REVIEW=ASSET/"review"
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
def main():
 REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
 for obj in list(scene.objects):
  if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
 visual=[obj for obj in scene.objects if obj.get("rungproof_asset")];lo,hi=bounds(visual);center=(lo+hi)/2
 bpy.ops.mesh.primitive_plane_add(size=12,location=(center.x,center.y,lo.z-.03));floor=bpy.context.object;floor.data.materials.append(mat("Review floor",(.06,.08,.10)))
 world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.012,.018,.026)
 for i,(off,energy,size) in enumerate((((5,-5,6),850,4),((-4,-3,4),520,3),((1,5,5),620,3))):
  data=bpy.data.lights.new(f"Review light {i}","AREA");data.energy,data.shape,data.size=energy,"DISK",size;light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(off);aim(light,center)
 # Dedicated work-zone light prevents the enclosure cavity hiding the cutter,
 # vise, stock and coolant witness in the acceptance view.
 data=bpy.data.lights.new("Review work-zone light","AREA");data.energy,data.shape,data.size=340,"RECTANGLE",2.0
 light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=(0,-2.1,2.15);aim(light,Vector((0,-.56,1.72)))
 data=bpy.data.cameras.new("Review camera");data.lens=55;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
 scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
 hero=Vector((4.3,-5.8,3.1));target=Vector((0,-.30,1.72));render(scene,camera,REVIEW/"hero.png",hero,target);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
 render(scene,camera,REVIEW/"doors_and_windows.png",Vector((0.6,-4.1,2.25)),Vector((0,-.78,1.90)))
 # Maintenance inspection only: hide the closed front-door assembly so the
 # reviewer can assess the actual spindle, tool and vise geometry.  It does
 # not alter the source blend, delivery GLB, or stopped-state hero image.
 door_parts=[obj for obj in scene.objects if obj.name.startswith(("DOOR_","MACHINE_door_"))]
 for obj in door_parts: obj.hide_render=True
 render(scene,camera,REVIEW/"spindle_vise_tool.png",Vector((.85,-2.65,1.90)),Vector((0,-.63,1.64)))
 for obj in door_parts: obj.hide_render=False
 render(scene,camera,REVIEW/"hmi_and_estop.png",Vector((2.35,-2.35,2.00)),Vector((1.42,-.62,1.87)))
 render(scene,camera,REVIEW/"tool_carousel_and_coolant.png",Vector((-3.2,-4.0,2.3)),Vector((.38,-.72,1.95)))
 pivot=scene.objects.get("KIN_spindle");pivot.rotation_euler.y=.72;render(scene,camera,REVIEW/"state_running.png",hero,target);pivot.rotation_euler.y=0;render(scene,camera,REVIEW/"state_stopped.png",hero,target)
 bpy.ops.mesh.primitive_cube_add(size=1,location=(hi.x+.85,center.y,lo.z+.5));scale=bpy.context.object;scale.data.materials.append(mat("One metre reference",(.9,.18,.02)));render(scene,camera,REVIEW/"scale_reference.png",Vector((4.5,-5.5,2.9)),(center+scale.location)/2);bpy.data.objects.remove(scale,do_unlink=True)
 wire=mat("Review wire",(.02,.85,1));copies=[]
 for obj in visual:
  if obj.type not in {"MESH","CURVE","FONT"}:continue
  obj.hide_render=True;clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone)
  if clone.type=="MESH":mod=clone.modifiers.new("Review wireframe","WIREFRAME");mod.thickness=.0025
  copies.append(clone)
 render(scene,camera,REVIEW/"wireframe.png",hero,target)
 for obj in visual:obj.hide_render=False
 for clone in copies:bpy.data.objects.remove(clone,do_unlink=True)
 shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png");print(f"MACHINE_CENTER_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
