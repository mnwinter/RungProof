"""Render acceptance evidence for the articulated six-axis industrial robot."""
from __future__ import annotations
import os, shutil
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);ASSET=ROOT/"assets"/"scene_core"/"six_axis_robot";SOURCE=ASSET/"source"/"six_axis_robot.blend";REVIEW=ASSET/"review"
def aim(obj,target): obj.rotation_euler=(target-obj.location).to_track_quat("-Z","Y").to_euler()
def mat(name,color):
    item=bpy.data.materials.new(name);item.diffuse_color=(*color,1);item.metallic=.08;item.roughness=.48;return item
def bounds(objects):
    lo=Vector((float("inf"),)*3);hi=Vector((float("-inf"),)*3)
    for obj in objects:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        for corner in obj.bound_box:
            point=obj.matrix_world@Vector(corner);lo=Vector((min(lo.x,point.x),min(lo.y,point.y),min(lo.z,point.z)));hi=Vector((max(hi.x,point.x),max(hi.y,point.y),max(hi.z,point.z)))
    return lo,hi
def render(scene,camera,floor,path,location,target):
    camera.location=location;aim(camera,target);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
def main():
    REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
    visual=[obj for obj in scene.objects if obj.get("rungproof_asset")];lo,hi=bounds(visual);center=(lo+hi)/2
    bpy.ops.mesh.primitive_plane_add(size=12,location=(center.x,center.y,lo.z-.03));floor=bpy.context.object;floor.data.materials.append(mat("Review floor",(.06,.08,.10)))
    world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.012,.018,.026)
    for i,(offset,energy,size) in enumerate((((5,-5,6),1400,4),((-4,-3,4),850,3),((1,5,5),950,3))):
        data=bpy.data.lights.new(f"Review light {i}","AREA");data.energy,data.shape,data.size=energy,"DISK",size;light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset);aim(light,center)
    data=bpy.data.cameras.new("Review camera");data.lens=55;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
    scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    # Tool-side hero deliberately exposes J4/J5/J6 collar/flange/adapter stack.
    hero=Vector((4.8,-5.2,3.6));target=Vector((1.05,0,1.85));render(scene,camera,floor,REVIEW/"hero.png",hero,target);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
    render(scene,camera,floor,REVIEW/"base_and_anchors.png",Vector((3.5,-4.5,1.4)),Vector((0,0,.42)))
    render(scene,camera,floor,REVIEW/"shoulder_elbow_links.png",Vector((-3.8,-4.0,3.0)),Vector((.50,0,2.15)))
    render(scene,camera,floor,REVIEW/"wrist_flange_gripper.png",Vector((5.0,-3.6,3.6)),Vector((2.05,0,3.05)))
    render(scene,camera,floor,REVIEW/"dresspack_routing.png",Vector((1.0,5.0,3.6)),Vector((.65,.20,2.15)))
    for obj in scene.objects:
        if obj.name.startswith("KIN_axis_1"):obj.rotation_euler.z=.55
    render(scene,camera,floor,REVIEW/"state_running.png",hero,target)
    for obj in scene.objects:
        if obj.name.startswith("KIN_axis_1"):obj.rotation_euler.z=0
    render(scene,camera,floor,REVIEW/"state_stopped.png",hero,target)
    bpy.ops.mesh.primitive_cube_add(size=1,location=(hi.x+.85,center.y,lo.z+.5));scale=bpy.context.object;scale.data.materials.append(mat("One metre reference",(.9,.18,.02)))
    render(scene,camera,floor,REVIEW/"scale_reference.png",Vector((4.8,-5.2,3.2)),(center+scale.location)/2);bpy.data.objects.remove(scale,do_unlink=True)
    wire=mat("Review wire",(.02,.85,1));copies=[]
    for obj in visual:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        obj.hide_render=True;clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone)
        if clone.type=="MESH":mod=clone.modifiers.new("Review wireframe","WIREFRAME");mod.thickness=.0025
        copies.append(clone)
    render(scene,camera,floor,REVIEW/"wireframe.png",hero,target)
    for obj in visual:obj.hide_render=False
    for clone in copies:bpy.data.objects.remove(clone,do_unlink=True)
    shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png");print(f"SIX_AXIS_ROBOT_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
