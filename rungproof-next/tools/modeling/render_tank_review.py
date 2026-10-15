"""Render source-model quality evidence for the vertical process tank."""
from __future__ import annotations
import os, shutil
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]); ASSET=ROOT/"assets"/"scene_core"/"vertical_process_tank"; SOURCE,REVIEW=ASSET/"source"/"vertical_process_tank.blend",ASSET/"review"
def aim(obj,point): obj.rotation_euler=(point-obj.location).to_track_quat("-Z","Y").to_euler()
def material(name,color,roughness):
 value=bpy.data.materials.new(name); value.use_nodes=True; shader=value.node_tree.nodes.get("Principled BSDF"); shader.inputs["Base Color"].default_value=(*color,1); shader.inputs["Roughness"].default_value=roughness; return value
def bounds(objects):
 low,high=Vector((float("inf"),)*3),Vector((float("-inf"),)*3)
 for obj in objects:
  for corner in obj.bound_box:
   point=obj.matrix_world@Vector(corner); low=Vector((min(low.x,point.x),min(low.y,point.y),min(low.z,point.z))); high=Vector((max(high.x,point.x),max(high.y,point.y),max(high.z,point.z)))
 return low,high
def render(scene,camera,path,location,target,floor,show_floor=True):
 floor.hide_render=not show_floor; camera.location=location; aim(camera,target); scene.render.filepath=str(path); bpy.ops.render.render(write_still=True)
def set_liquid_fraction(liquid,fraction):
 fraction=max(.001,min(1.0,fraction)); authored_height=liquid.dimensions.z
 bottom=liquid.location.z-authored_height*.5
 liquid.scale.z=fraction
 liquid.location.z=bottom+authored_height*fraction*.5
def main():
 REVIEW.mkdir(parents=True,exist_ok=True); bpy.ops.wm.open_mainfile(filepath=str(SOURCE)); scene=bpy.context.scene
 for obj in list(scene.objects):
  if obj.type in {"CAMERA","LIGHT"}: bpy.data.objects.remove(obj,do_unlink=True)
 visual=[obj for obj in scene.objects if obj.type=="MESH"]; low,high=bounds(visual); center=(low+high)/2; radius=max((high-low).length*.82,7.2)
 bpy.ops.mesh.primitive_plane_add(size=radius*4,location=(center.x,center.y,low.z-.025)); floor=bpy.context.object; floor.data.materials.append(material("Review floor",(.055,.07,.09),.72))
 world=scene.world or bpy.data.worlds.new("World"); scene.world=world; world.color=(.012,.018,.028)
 for index,(offset,energy,size) in enumerate((((4,-4,6),1800,5),((-4,-2,3),1100,4),((1,4,5),1500,4))):
  data=bpy.data.lights.new(f"REVIEW_light_{index}","AREA"); data.energy,data.shape,data.size=energy,"DISK",size; lamp=bpy.data.objects.new(data.name,data); scene.collection.objects.link(lamp); lamp.location=center+Vector(offset); aim(lamp,center)
 data=bpy.data.cameras.new("REVIEW_camera"); data.lens=62; camera=bpy.data.objects.new("REVIEW_camera",data); scene.collection.objects.link(camera); scene.camera=camera
 scene.render.engine="BLENDER_EEVEE"; scene.render.resolution_x=scene.render.resolution_y=1100; scene.render.resolution_percentage=100; scene.render.image_settings.file_format="PNG"
 views={"hero.png":(Vector((1.15,-1.25,.62)),center,True),"roof_manway.png":(Vector((.70,-.85,1.45)),Vector((0,0,5.18)),True),"ladder_cage.png":(Vector((-1.25,-.70,.22)),Vector((-1.42,0,2.9)),True),"sight_glass.png":(Vector((1.28,-.70,.22)),Vector((1.52,0,2.55)),True),"outlet_nozzle.png":(Vector((.10,-1.35,-.05)),Vector((0,-1.60,.94)),True),"legs_and_underside.png":(Vector((.90,-.90,-1.05)),Vector((0,0,.42)),False)}
 for name,(direction,target,show_floor) in views.items(): render(scene,camera,REVIEW/name,center+direction*radius,target,floor,show_floor)
 liquid=bpy.data.objects.get("KIN_liquid")
 if liquid:
  set_liquid_fraction(liquid,.42); render(scene,camera,REVIEW/"state_stopped.png",center+Vector((1.15,-1.25,.62))*radius,center,floor)
  set_liquid_fraction(liquid,.56); render(scene,camera,REVIEW/"state_running.png",center+Vector((1.15,-1.25,.62))*radius,center,floor)
 shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
 bpy.ops.mesh.primitive_cube_add(size=1.0,location=(high.x+.9,center.y,low.z+.5)); cube=bpy.context.object; cube.data.materials.append(material("One metre reference",(.92,.23,.02),.32)); render(scene,camera,REVIEW/"scale_reference.png",center+Vector((1.15,-1.15,.60))*radius,(center+cube.location)/2,floor); bpy.data.objects.remove(cube,do_unlink=True)
 wire=material("Review wireframe",(.03,.82,.98),.30); copies=[]
 for obj in visual:
  obj.hide_render=True; clone=obj.copy(); clone.data=obj.data.copy(); scene.collection.objects.link(clone); clone.hide_render=False; clone.data.materials.clear(); clone.data.materials.append(wire); modifier=clone.modifiers.new("Review wireframe","WIREFRAME"); modifier.thickness=.003; copies.append(clone)
 render(scene,camera,REVIEW/"wireframe.png",center+Vector((1.15,-1.25,.62))*radius,center,floor)
 for clone in copies: bpy.data.objects.remove(clone,do_unlink=True)
 shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png"); print(f"TANK_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__": main()
