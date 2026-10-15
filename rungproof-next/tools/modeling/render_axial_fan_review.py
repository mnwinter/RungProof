"""Render production evidence for the anchored axial exhaust fan."""
from __future__ import annotations
import os, shutil
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
ASSET=ROOT/"assets"/"scene_core"/"axial_exhaust_fan"
SOURCE=ASSET/"source"/"axial_exhaust_fan.blend"
REVIEW=ASSET/"review"

def aim(obj,target): obj.rotation_euler=(target-obj.location).to_track_quat("-Z","Y").to_euler()
def mat(name,color,rough=.55):
    value=bpy.data.materials.new(name);value.diffuse_color=(*color,1);value.metallic=.05;value.roughness=rough;return value
def bounds(objects):
    lo=Vector((float("inf"),)*3);hi=Vector((float("-inf"),)*3)
    for obj in objects:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        for corner in obj.bound_box:
            point=obj.matrix_world@Vector(corner);lo=Vector((min(lo.x,point.x),min(lo.y,point.y),min(lo.z,point.z)));hi=Vector((max(hi.x,point.x),max(hi.y,point.y),max(hi.z,point.z)))
    return lo,hi
def render(scene,camera,floor,path,location,target,show_floor=True):
    floor.hide_render=not show_floor;camera.location=location;aim(camera,target);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)

def main():
    REVIEW.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
    for obj in list(scene.objects):
        if obj.type in {"CAMERA","LIGHT"}:bpy.data.objects.remove(obj,do_unlink=True)
    visual=[obj for obj in scene.objects if obj.get("rungproof_asset")];lo,hi=bounds(visual);center=(lo+hi)/2
    bpy.ops.mesh.primitive_plane_add(size=9,location=(center.x,center.y,lo.z-.025));floor=bpy.context.object;floor.data.materials.append(mat("Review floor",(.07,.09,.11),.72))
    world=scene.world or bpy.data.worlds.new("Review world");scene.world=world;world.color=(.015,.022,.032)
    for index,(offset,energy,size) in enumerate((((4,-4,5),1100,3.2),((-3,-2,3),600,2.5),((1,4,4),750,3))):
        data=bpy.data.lights.new(f"Review light {index}","AREA");data.energy,data.shape,data.size=energy,"DISK",size;light=bpy.data.objects.new(data.name,data);scene.collection.objects.link(light);light.location=center+Vector(offset);aim(light,center)
    # Keep the entire guard perimeter and both posts in the hero frame.  The
    # evidence image is an inspection view, so edge cropping must not make a
    # positively retained grille appear to have open or protruding ends.
    data=bpy.data.cameras.new("Review camera");data.lens=50;camera=bpy.data.objects.new("Review camera",data);scene.collection.objects.link(camera);scene.camera=camera
    scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    front=center+Vector((1.15,-2.25,.52))*1.22
    render(scene,camera,floor,REVIEW/"hero.png",front,center);shutil.copyfile(REVIEW/"hero.png",REVIEW/"blind_review.png")
    render(scene,camera,floor,REVIEW/"guard_and_blades.png",center+Vector((.20,-1.8,.15)),Vector((0,-.15,1.45)))
    render(scene,camera,floor,REVIEW/"motor_and_shaft.png",center+Vector((1.2,1.5,.35)),Vector((0,.35,1.45)))
    render(scene,camera,floor,REVIEW/"frame_and_anchors.png",center+Vector((1.35,-1.15,-.55)),Vector((0,0,.35)))
    render(scene,camera,floor,REVIEW/"rear_drive.png",center+Vector((-.85,1.65,.35)),center)
    render(scene,camera,floor,REVIEW/"state_stopped.png",front,center)
    pivot=scene.objects["KIN_fan_hub"];pivot.rotation_euler.y=.78
    render(scene,camera,floor,REVIEW/"state_running.png",front,center);pivot.rotation_euler.y=0
    bpy.ops.mesh.primitive_cube_add(size=1,location=(hi.x+.72,center.y,lo.z+.5));scale=bpy.context.object;scale.data.materials.append(mat("One metre reference",(.9,.18,.02),.38));render(scene,camera,floor,REVIEW/"scale_reference.png",center+Vector((1.2,-1.7,.55)),(center+scale.location)/2);bpy.data.objects.remove(scale,do_unlink=True)
    wire=mat("Review wire",(.02,.85,1),.35);copies=[]
    for obj in visual:
        if obj.type not in {"MESH","CURVE","FONT"}:continue
        obj.hide_render=True;clone=obj.copy();clone.data=obj.data.copy();clone.data.materials.clear();clone.data.materials.append(wire);scene.collection.objects.link(clone)
        if clone.type=="MESH":modifier=clone.modifiers.new("Review wireframe","WIREFRAME");modifier.thickness=.0025
        copies.append(clone)
    render(scene,camera,floor,REVIEW/"wireframe.png",front,center)
    for obj in visual:obj.hide_render=False
    for clone in copies:bpy.data.objects.remove(clone,do_unlink=True)
    shutil.copyfile(REVIEW/"hero.png",ASSET/"thumbnail.png");print(f"AXIAL_FAN_REVIEW_RENDERED {REVIEW}")
if __name__=="__main__":main()
