"""Render acceptance evidence for the guarded industrial pedestal drill press."""
from __future__ import annotations

import os, shutil
from pathlib import Path

import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET=ROOT/"assets"/"scene_core"/"industrial_drill_press"
SOURCE=ASSET/"source"/"industrial_drill_press.blend"
REVIEW=ASSET/"review"

def aim(obj,target): obj.rotation_euler=(target-obj.location).to_track_quat("-Z","Y").to_euler()
def material(name,color,rough=.55):
    item=bpy.data.materials.new(name);item.diffuse_color=(*color,1);item.metallic=.05;item.roughness=rough;return item
def bounds(objects):
    lo=Vector((float("inf"),)*3);hi=Vector((float("-inf"),)*3)
    for obj in objects:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        for corner in obj.bound_box:
            point=obj.matrix_world@Vector(corner)
            lo=Vector((min(lo.x,point.x),min(lo.y,point.y),min(lo.z,point.z)))
            hi=Vector((max(hi.x,point.x),max(hi.y,point.y),max(hi.z,point.z)))
    return lo,hi
def render(scene,camera,floor,path,location,target,show_floor=True):
    floor.hide_render=not show_floor;camera.location=location;aim(camera,target);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
def main():
    REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
    visual=[obj for obj in scene.objects if obj.get("rungproof_asset")];lo,hi=bounds(visual);center=(lo+hi)/2
    bpy.ops.mesh.primitive_plane_add(size=12,location=(center.x,center.y,lo.z-.025));floor=bpy.context.object;floor.data.materials.append(material("Review floor",(.07,.09,.11),.72))
    world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.015,.022,.032)
    for index,(offset,energy,size) in enumerate((((5,-5,6),1450,4),((-4,-3,4),900,3),((1,5,5),1050,3))):
        data=bpy.data.lights.new(f"Review light {index}","AREA");data.energy,data.shape,data.size=energy,"DISK",size;light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset);aim(light,center)
    data=bpy.data.cameras.new("Review camera");data.lens=55;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
    scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    hero=center+Vector((4.6,-5.4,2.7));target=center+Vector((0,0,.28))
    render(scene,camera,floor,REVIEW/"hero.png",hero,target);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
    render(scene,camera,floor,REVIEW/"drive_and_column.png",center+Vector((-4.1,4.2,2.5)),Vector((-.22,.28,3.05)))
    render(scene,camera,floor,REVIEW/"guard_chuck_and_vise.png",center+Vector((2.7,-4.5,.55)),Vector((.38,-.05,1.92)))
    render(scene,camera,floor,REVIEW/"controls_and_feed.png",center+Vector((3.6,-4.8,1.9)),Vector((-.18,-.18,2.82)))
    # The generated pose is not a claim of live operation: it only verifies
    # that the complete rotating spindle hierarchy remains coherent.
    pivot=scene.objects.get("KIN_spindle");pivot.rotation_euler.z=.72
    render(scene,camera,floor,REVIEW/"state_running.png",hero,target)
    pivot.rotation_euler.z=0
    render(scene,camera,floor,REVIEW/"state_stopped.png",hero,target)
    bpy.ops.mesh.primitive_cube_add(size=1,location=(hi.x+.82,center.y,lo.z+.5));scale=bpy.context.object;scale.data.materials.append(material("One metre reference",(.9,.18,.02),.38))
    render(scene,camera,floor,REVIEW/"scale_reference.png",center+Vector((4.7,-5.2,2.5)),(center+scale.location)/2);bpy.data.objects.remove(scale,do_unlink=True)
    wire=material("Review wire",(.02,.85,1),.35);copies=[]
    for obj in visual:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        obj.hide_render=True;clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone)
        if clone.type=="MESH":modifier=clone.modifiers.new("Review wireframe","WIREFRAME");modifier.thickness=.0025
        copies.append(clone)
    render(scene,camera,floor,REVIEW/"wireframe.png",hero,target)
    for obj in visual:obj.hide_render=False
    for clone in copies:bpy.data.objects.remove(clone,do_unlink=True)
    shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png");print(f"DRILL_PRESS_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
