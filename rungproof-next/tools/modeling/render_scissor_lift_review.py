"""Render acceptance evidence for the guarded hydraulic scissor-lift table."""
from __future__ import annotations
import os, shutil, math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET=ROOT/"assets"/"scene_core"/"scissor_lift_table"
SOURCE=ASSET/"source"/"scissor_lift_table.blend"
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
def raised_pose(scene,rise=.60):
    """Mirror the two-stage lift geometry used by EquipmentMotionController."""
    arm_length=2.0;initial_height=.60;initial_angle=math.asin(initial_height/arm_length)
    stage_height=initial_height+rise*.5;angle=math.asin(stage_height/arm_length);half_run=math.sqrt((arm_length*.5)**2-(stage_height*.5)**2)
    for obj in scene.objects:
        name=obj.name
        if name.startswith("KIN_platform") or name.startswith("LIFT_top_pivot_") or name.startswith("LIFT_upper_track_"):
            obj.location.z+=rise
        elif name.startswith("KIN_lift_bellows_"):
            obj.location.z+=rise*.5;obj.scale.z*=1.0+rise/1.22
        elif name.startswith("KIN_scissor_"):
            stage=1 if name.startswith("KIN_scissor_1_") else 0
            sign=-1 if name.endswith("_-1") else 1
            obj.rotation_euler.y=sign*angle
            obj.location.z+=rise*(.25 if stage==0 else .75)
        elif name.startswith("KIN_stage_pivot_"):
            stage=1 if name.startswith("KIN_stage_pivot_1_") else 0
            obj.location.z+=rise*(.25 if stage==0 else .75)
        elif name.startswith("KIN_mid_pivot_"):
            obj.location.z+=rise*.5;obj.location.x=math.copysign(half_run,obj.location.x)
        elif name.startswith("LIFT_bottom_pivot_"):
            obj.location.x=math.copysign(half_run,obj.location.x)
    return initial_angle,angle
def main():
    REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
    visual=[obj for obj in scene.objects if obj.get("rungproof_asset")];lo,hi=bounds(visual);center=(lo+hi)/2
    bpy.ops.mesh.primitive_plane_add(size=10,location=(center.x,center.y,lo.z-.025));floor=bpy.context.object;floor.data.materials.append(material("Review floor",(.07,.09,.11),.72))
    world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.015,.022,.032)
    for index,(offset,energy,size) in enumerate((((5,-5,6),1300,4),((-4,-3,4),850,3),((1,5,5),950,3))):
        data=bpy.data.lights.new(f"Review light {index}","AREA");data.energy,data.shape,data.size=energy,"DISK",size;light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset);aim(light,center)
    data=bpy.data.cameras.new("Review camera");data.lens=52;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
    scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    hero=center+Vector((4.2,-5.0,2.7));target=center+Vector((0,0,.15))
    render(scene,camera,floor,REVIEW/"hero.png",hero,target);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
    render(scene,camera,floor,REVIEW/"guard_and_platform.png",center+Vector((.2,-5.2,2.0)),center+Vector((0,0,1.25)))
    render(scene,camera,floor,REVIEW/"hydraulic_and_guides.png",center+Vector((-4.0,3.8,.5)),Vector((0,-.25,.72)))
    render(scene,camera,floor,REVIEW/"drive_side.png",center+Vector((3.6,-3.8,.25)),Vector((-.20,-.45,.55)))
    # Inspection cutaway only: temporarily hide the continuous bellows skins
    # while retaining all mechanism geometry for a readable drive/guide view.
    bellows=[obj for obj in scene.objects if obj.name.startswith("KIN_lift_bellows_")]
    for obj in bellows:obj.hide_render=True
    render(scene,camera,floor,REVIEW/"internal_cutaway.png",center+Vector((3.8,-4.0,.7)),Vector((0,0,.78)))
    for obj in bellows:obj.hide_render=False
    render(scene,camera,floor,REVIEW/"state_stopped.png",hero,target)
    raised_pose(scene)
    render(scene,camera,floor,REVIEW/"state_running.png",hero,target+Vector((0,0,.30)))
    bpy.ops.mesh.primitive_cube_add(size=1,location=(hi.x+.85,center.y,lo.z+.5));scale=bpy.context.object;scale.data.materials.append(material("One metre reference",(.9,.18,.02),.38))
    render(scene,camera,floor,REVIEW/"scale_reference.png",center+Vector((4.2,-4.8,2.3)),(center+scale.location)/2);bpy.data.objects.remove(scale,do_unlink=True)
    wire=material("Review wire",(.02,.85,1),.35);copies=[]
    for obj in visual:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        obj.hide_render=True;clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone)
        if clone.type=="MESH":modifier=clone.modifiers.new("Review wireframe","WIREFRAME");modifier.thickness=.0025
        copies.append(clone)
    render(scene,camera,floor,REVIEW/"wireframe.png",hero,target)
    for obj in visual:obj.hide_render=False
    for clone in copies:bpy.data.objects.remove(clone,do_unlink=True)
    shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png");print(f"SCISSOR_LIFT_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
