"""Render production evidence for the vibronic tuning-fork level switch."""
from __future__ import annotations
import os, shutil
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET=ROOT/"assets"/"scene_core"/"tuning_fork_level_switch"
SOURCE=ASSET/"source"/"tuning_fork_level_switch.blend"
REVIEW=ASSET/"review"
def aim(obj,target):obj.rotation_euler=(target-obj.location).to_track_quat("-Z","Y").to_euler()
def material(name,color,roughness=.55):
    value=bpy.data.materials.new(name);value.diffuse_color=(*color,1);value.metallic=.05;value.roughness=roughness;return value
def bounds(objects):
    low,high=Vector((1e9,)*3),Vector((-1e9,)*3)
    for obj in objects:
        for corner in obj.bound_box:
            point=obj.matrix_world@Vector(corner)
            low=Vector((min(low.x,point.x),min(low.y,point.y),min(low.z,point.z)))
            high=Vector((max(high.x,point.x),max(high.y,point.y),max(high.z,point.z)))
    return low,high
def render(scene,camera,path,location,target,floor,show_floor=True):
    floor.hide_render=not show_floor;camera.location=location;aim(camera,target);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
def main():
    REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
    visual=[obj for obj in scene.objects if obj.type in {"MESH","CURVE"} and obj.get("rungproof_asset")]
    low,high=bounds(visual);center=(low+high)/2;radius=max((high-low).length*1.35,2.8)
    bpy.ops.mesh.primitive_plane_add(size=7,location=(center.x,center.y,low.z-.04));floor=bpy.context.object;floor.data.materials.append(material("Review floor",(.07,.09,.11),.72))
    world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.015,.022,.032)
    for index,(offset,energy,size) in enumerate((((4,-5,6),1100,4),((-4,-1,3),650,3),((1,5,4),780,3))):
        data=bpy.data.lights.new(f"Review light {index}","AREA");data.energy,data.shape,data.size=energy,"DISK",size
        light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset);aim(light,center)
    data=bpy.data.cameras.new("Review camera");data.lens=60;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
    scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    hero=center+Vector((.82,-1.30,.40))*radius
    render(scene,camera,REVIEW/"hero.png",hero,center,floor);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
    render(scene,camera,REVIEW/"fork_and_yoke.png",Vector((.78,-1.35,-.78)),Vector((0,0,-.78)),floor,False)
    render(scene,camera,REVIEW/"process_thread_and_hex.png",Vector((.88,-1.32,.18)),Vector((0,0,.10)),floor,False)
    render(scene,camera,REVIEW/"status_and_nameplate.png",Vector((.50,-1.55,.67)),Vector((0,0,.65)),floor,False)
    render(scene,camera,REVIEW/"m12_connector.png",Vector((.72,-.70,1.45)),Vector((0,0,.98)),floor,False)
    render(scene,camera,REVIEW/"rear_and_ground.png",Vector((.95,1.35,.72)),Vector((0,0,.63)),floor,False)
    bpy.ops.mesh.primitive_cube_add(size=1,location=(.58,0,low.z+.5));scale=bpy.context.object;scale.data.materials.append(material("One metre reference",(.9,.18,.02),.38))
    render(scene,camera,REVIEW/"scale_reference.png",hero,(center+scale.location)/2,floor);bpy.data.objects.remove(scale,do_unlink=True)
    wire=material("Review wire",(.02,.85,1.0),.35);clones=[]
    for obj in visual:
        obj.hide_render=True
        if obj.type!="MESH":continue
        clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone);clone.modifiers.new("Review wireframe","WIREFRAME").thickness=.0025;clones.append(clone)
    render(scene,camera,REVIEW/"wireframe.png",hero,center,floor)
    for obj in visual:obj.hide_render=False
    for clone in clones:bpy.data.objects.remove(clone,do_unlink=True)
    shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png")
    print(f"TUNING_FORK_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
