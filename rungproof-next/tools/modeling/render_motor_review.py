"""Render detailed source-model review evidence for the TEFC induction motor."""
from __future__ import annotations
import math, os, shutil
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET = ROOT / "assets" / "scene_core" / "ac_induction_motor"
SOURCE, REVIEW = ASSET / "source" / "ac_induction_motor.blend", ASSET / "review"

def aim(obj, point): obj.rotation_euler = (point - obj.location).to_track_quat("-Z", "Y").to_euler()
def material(name, color, roughness):
    value=bpy.data.materials.new(name); value.use_nodes=True
    shader=value.node_tree.nodes.get("Principled BSDF"); shader.inputs["Base Color"].default_value=(*color,1); shader.inputs["Roughness"].default_value=roughness
    return value
def bounds(objects):
    low,high=Vector((float("inf"),)*3),Vector((float("-inf"),)*3)
    for obj in objects:
        for corner in obj.bound_box:
            point=obj.matrix_world @ Vector(corner)
            low=Vector((min(low.x,point.x),min(low.y,point.y),min(low.z,point.z))); high=Vector((max(high.x,point.x),max(high.y,point.y),max(high.z,point.z)))
    return low,high
def render(scene,camera,path,location,target,floor,show_floor=True):
    floor.hide_render=not show_floor; camera.location=location; aim(camera,target); scene.render.filepath=str(path); bpy.ops.render.render(write_still=True)

def main():
    REVIEW.mkdir(parents=True,exist_ok=True); bpy.ops.wm.open_mainfile(filepath=str(SOURCE)); scene=bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}: bpy.data.objects.remove(obj,do_unlink=True)
    visual=[obj for obj in scene.objects if obj.type=="MESH"]; low,high=bounds(visual); center=(low+high)/2; radius=max((high-low).length*.85,2.8)
    bpy.ops.mesh.primitive_plane_add(size=radius*4,location=(center.x,center.y,low.z-.025)); floor=bpy.context.object; floor.data.materials.append(material("Review floor",(.055,.07,.09),.72))
    world=scene.world or bpy.data.worlds.new("World"); scene.world=world; world.color=(.012,.018,.028)
    for index,(offset,energy,size) in enumerate((((2.1,-2.4,2.8),1100,3.4),((-2.1,-1.4,1.8),750,2.6),((.4,2.0,2.2),800,3.0))):
        data=bpy.data.lights.new(f"REVIEW_light_{index}","AREA"); data.energy,data.shape,data.size=energy,"DISK",size
        lamp=bpy.data.objects.new(data.name,data); scene.collection.objects.link(lamp); lamp.location=center+Vector(offset); aim(lamp,center)
    data=bpy.data.cameras.new("REVIEW_camera"); data.lens=58; camera=bpy.data.objects.new("REVIEW_camera",data); scene.collection.objects.link(camera); scene.camera=camera
    scene.render.engine="BLENDER_EEVEE"; scene.render.resolution_x=scene.render.resolution_y=1050; scene.render.resolution_percentage=100; scene.render.image_settings.file_format="PNG"
    views={
      "hero.png":(Vector((1.15,-1.25,.75)),center,True),
      "drive_end.png":(Vector((1.45,-.68,.20)),Vector((.73,0,.49)),True),
      "fan_guard.png":(Vector((-1.35,-.62,.30)),Vector((-.77,0,.49)),True),
      "terminal_box.png":(Vector((-.15,-1.10,1.05)),Vector((-.05,0,.95)),True),
      "underside.png":(Vector((.70,-.82,-1.0)),Vector((0,0,.18)),False),
      "end_alignment.png":(Vector((1.65,.0,.15)),Vector((.72,0,.49)),True),
    }
    for name,(direction,target,show_floor) in views.items(): render(scene,camera,REVIEW/name,center+direction*radius,target,floor,show_floor)
    render(scene,camera,REVIEW/"state_stopped.png",center+Vector((1.15,-1.25,.75))*radius,center,floor); shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
    bpy.ops.mesh.primitive_cube_add(size=1.0,location=(high.x+.72,center.y,low.z+.5)); cube=bpy.context.object; cube.data.materials.append(material("One metre reference",(.92,.23,.02),.32))
    render(scene,camera,REVIEW/"scale_reference.png",center+Vector((1.25,-1.1,.72))*radius,(center+cube.location)/2,floor); bpy.data.objects.remove(cube,do_unlink=True)
    wire=material("Review wireframe",(.03,.82,.98),.30); copies=[]
    for obj in visual:
        obj.hide_render=True; clone=obj.copy(); clone.data=obj.data.copy(); scene.collection.objects.link(clone); clone.hide_render=False; clone.data.materials.clear(); clone.data.materials.append(wire); modifier=clone.modifiers.new("Review wireframe","WIREFRAME"); modifier.thickness=.003; copies.append(clone)
    render(scene,camera,REVIEW/"wireframe.png",center+Vector((1.15,-1.25,.75))*radius,center,floor)
    for clone in copies: bpy.data.objects.remove(clone,do_unlink=True)
    for obj in visual: obj.hide_render=False
    for obj in scene.objects:
        if obj.name.startswith("KIN_motor_"): obj.rotation_euler.x += math.radians(38)
    render(scene,camera,REVIEW/"state_running.png",center+Vector((1.15,-1.25,.75))*radius,center,floor)
    shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png"); print(f"MOTOR_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__": main()
