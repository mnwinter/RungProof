"""Build reusable scene-support equipment for metering, receiving, and parcel sizing."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"scene_support";BUILT=[]
bpy.context.preferences.filepaths.save_version=0
def clean():
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes,bpy.data.curves,bpy.data.cameras,bpy.data.lights,bpy.data.materials):
        for block in list(blocks):
            if block.users==0:blocks.remove(block)
def mat(n,c,metal=0,rough=.4,alpha=1):
    m=bpy.data.materials.new(n);m.diffuse_color=(*c,alpha);m.use_nodes=True;p=m.node_tree.nodes.get("Principled BSDF");p.inputs["Base Color"].default_value=(*c,1);p.inputs["Metallic"].default_value=metal;p.inputs["Roughness"].default_value=rough
    if alpha<1:p.inputs["Alpha"].default_value=alpha;m.surface_render_method="DITHERED"
    return m
def finish(o,n,m=None,b=.006,s=False,c=True):
    o.name=n
    if m:o.data.materials.append(m)
    if b:q=o.modifiers.new("Manufactured edge radius","BEVEL");q.width=b;q.segments=3;q.limit_method="ANGLE"
    if s and hasattr(o.data,"polygons"):
        for p in o.data.polygons:p.use_smooth=True
    o["rungproof_asset"]=True;o["rungproof_collision"]=c;return o
def box(n,l,d,m,b=.006,c=True,r=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=l,rotation=r);o=bpy.context.object;o.dimensions=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m,b,False,c)
def cyl(n,l,rad,depth,m,axis="Z",c=True,verts=64):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=rad,depth=depth,location=l,rotation=rot);return finish(bpy.context.object,n,m,.002,True,c)
def tube(n,a,b,rad,m,c=True):
    a,b=Vector(a),Vector(b);v=b-a;o=cyl(n,(a+b)/2,rad,v.length,m,"Z",c,40);o.rotation_euler=v.to_track_quat("Z","Y").to_euler();return o
def text_mesh(n,text_value,l,size,m,r=(math.pi/2,0,0)):
    d=bpy.data.curves.new(n,"FONT");d.body=text_value;d.align_x="CENTER";d.align_y="CENTER";d.size=size;d.extrude=.006;d.bevel_depth=.001;o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);o.location=l;o.rotation_euler=r;bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target="MESH");return finish(o,n,m,0,False,False)
def mats():return {"blue":mat("Industrial blue",(.025,.22,.43),.42,.30),"steel":mat("Stainless steel",(.58,.64,.66),.85,.17),"dark":mat("Dark frame",(.018,.028,.035),.32,.50),"yellow":mat("Safety yellow",(.96,.57,.01),.15,.30),"white":mat("White polymer",(.92,.93,.89),.02,.64),"red":mat("Optical red",(.90,.01,.01),.04,.20),"glass":mat("Polycarbonate",(.05,.38,.45),.08,.18,.28)}
def metering(M):
    box("SKID_BASE",(0,0,.08),(2.20,1.35,.16),M["dark"],.018)
    for x in (-.88,.88):
        for y in (-.48,.48):box(f"ANCHOR_{x}_{y}",(x,y,.03),(.20,.20,.06),M["steel"],.008)
    cyl("DRIVE_MOTOR",(-.48,0,.58),.25,.72,M["blue"],"X");box("GEAR_REDUCER",(-.05,0,.58),(.36,.48,.48),M["steel"],.035);cyl("KIN_metering_pump_shaft",(.23,0,.58),.13,.30,M["dark"],"X")
    box("METERING_PUMP_HEAD",(.48,0,.58),(.34,.48,.50),M["blue"],.035)
    cyl("DIAPHRAGM_COVER",(.67,-.26,.58),.19,.07,M["steel"],"Y",False,64)
    for a in range(0,360,60):
        q=math.radians(a);cyl(f"DIAPHRAGM_BOLT_{a}",(.67+.14*math.cos(q),-.305,.58+.14*math.sin(q)),.018,.025,M["dark"],"Y",False,24)
    cyl("PULSATION_DAMPENER",(.62,.28,1.03),.13,.62,M["steel"]);cyl("CALIBRATION_COLUMN",(-.70,-.42,1.12),.10,1.08,M["glass"],"Z",False,48)
    # A continuous, readable suction-to-discharge process path with flanged
    # external connections and inline check valves identifies this as a dosing
    # skid rather than a generic motor/gearbox assembly.
    tube("SUCTION_INLET",(-1.02,-.44,.42),(.48,-.44,.42),.055,M["steel"],False)
    tube("SUCTION_DROP",(.48,-.44,.42),(.48,-.24,.48),.055,M["steel"],False)
    cyl("SUCTION_FLANGE",(-1.02,-.44,.42),.12,.05,M["steel"],"X",False,48)
    cyl("SUCTION_CHECK",(-.05,-.44,.42),.11,.20,M["yellow"],"X",False,48)
    tube("DISCHARGE_RISER",(.48,-.24,.72),(.48,-.24,1.22),.055,M["steel"],False)
    tube("DISCHARGE_HEADER",(.48,-.24,1.22),(1.02,-.24,1.22),.055,M["steel"],False)
    cyl("DISCHARGE_CHECK",(.48,-.24,.96),.11,.20,M["yellow"],"Z",False,48)
    cyl("DISCHARGE_FLANGE",(1.02,-.24,1.22),.12,.05,M["steel"],"X",False,48)
    cyl("PRESSURE_GAUGE",(.48,-.29,1.40),.13,.06,M["white"],"Y",False,48);box("GAUGE_NEEDLE",(.48,-.325,1.40),(.015,.015,.16),M["red"],.001,False,r=(0,0,-.45))
    box("PANEL_POST",(-.82,.45,.72),(.10,.10,1.28),M["steel"],.010);box("PANEL_ARM",(-.82,.32,1.00),(.10,.34,.10),M["steel"],.010);box("CONTROL_PANEL",(-.82,.12,1.22),(.42,.28,.72),M["blue"],.035);box("HMI",(-.82,-.035,1.34),(.27,.025,.20),M["glass"],.004,False);cyl("ESTOP",(-.82,-.055,1.08),.05,.04,M["red"],"Y",False,32)
    for z in (.72,1.38):box(f"COLUMN_CLAMP_{z}",(-.70,-.38,z),(.28,.12,.08),M["steel"],.008,False)
    box("METER_NAMEPLATE",(-.20,-.30,.96),(.62,.025,.16),M["white"],.004,False);text_mesh("METER_NAME","METERING SKID",(-.20,-.316,.96),.09,M["dark"])
def receiver(M):
    box("RECEIVER_BASE",(0,0,.08),(2.45,1.70,.16),M["dark"],.018);box("BACKSTOP",(0,.68,.88),(2.30,.12,1.60),M["blue"],.018)
    for x in (-.57,.57):
        box(f"TRAY_{x}",(x,0,.24),(.92,1.18,.10),M["steel"],.018);box(f"FRONT_STOP_{x}",(x,-.55,.48),(.92,.10,.40),M["yellow"],.012)
        for side in (-.43,.43):box(f"GUIDE_{x}_{side}",(x+side,0,.62),(.08,1.16,.64),M["steel"],.010)
        side="left" if x<0 else "right"
        for i,y in enumerate((-.38,-.18,.02,.22,.42)):
            name=f"KIN_receiver_roller_{side}" if i==0 else f"RECEIVER_ROLLER_{side}_{i}"
            cyl(name,(x,y,.34),.055,.78,M["dark"],"X",False,48)
        # Docking funnel and presence photoeye make each bay's receive function explicit.
        box(f"DOCK_FUNNEL_L_{side}",(x-.34,-.49,.53),(.10,.28,.18),M["yellow"],.015,False,r=(0,0,-.28))
        box(f"DOCK_FUNNEL_R_{side}",(x+.34,-.49,.53),(.10,.28,.18),M["yellow"],.015,False,r=(0,0,.28))
        box(f"PRESENCE_SENSOR_{side}",(x+.36,.47,.62),(.12,.18,.13),M["blue"],.018,False)
        cyl(f"PRESENCE_LENS_{side}",(x+.36,.37,.62),.035,.025,M["red"],"Y",False,32)
    for x in (-1.05,1.05):box(f"BACKSTOP_POST_{x}",(x,.68,.86),(.12,.12,1.56),M["steel"],.012)
    box("RECEIVER_NAMEPLATE",(0,-.625,.78),(1.10,.025,.18),M["white"],.004,False);text_mesh("RECEIVER_NAME","CONTAINER RECEIVER",(0,-.641,.78),.09,M["dark"])
    before=set(bpy.data.objects)
    for x,color in ((-.57,M["white"]),(.57,M["yellow"])):
        box(f"REFERENCE_CONTAINER_{x}",(x,.03,.76),(.62,.72,.78),color,.045,False);box(f"REFERENCE_CONTAINER_RIM_{x}",(x,.03,1.12),(.68,.78,.10),M["dark"],.012,False)
    for o in set(bpy.data.objects)-before:o["rungproof_review_only"]=True
def sensor_bank(M):
    box("PORTAL_BASE",(0,0,.06),(.52,2.55,.12),M["dark"],.012)
    for z in (-1.08,1.08):box(f"PORTAL_POST_{z}",(0,z,1.12),(.16,.16,2.12),M["blue"],.018)
    box("PORTAL_HEADER",(0,0,2.18),(.18,2.32,.18),M["blue"],.018)
    for i,h in enumerate((.95,1.30,1.65),1):
        for z in (-1.08,1.08):
            box(f"SENSOR_HOUSING_{i}_{z}",(-.12,z,h),(.32,.24,.22),M["dark"],.025);cyl(f"LENS_{i}_{z}",(-.30,z,h),.055,.035,M["red"],"X",False,40)
        box(f"KIN_beam_size_{i}",(-.32,0,h),(.025,2.05,.025),M["red"],.001,False)
    box("JUNCTION_BOX",(.18,1.08,1.86),(.38,.34,.48),M["steel"],.025);tube("CABLE_TRUNK",(.10,1.08,.30),(.10,1.08,1.68),.030,M["dark"],False)
    # A mounted parcel-height readout differentiates this sparse three-channel
    # dimensioning bank from a personnel-protection light curtain.
    box("HEIGHT_READOUT",(0,-1.20,2.18),(.92,.08,.34),M["dark"],.025,False)
    box("HEIGHT_SCREEN",(0,-1.245,2.20),(.70,.025,.16),M["glass"],.006,False)
    text_mesh("HEIGHT_LABEL","PARCEL HEIGHT",(0,-1.262,2.20),.085,M["white"])
    for i,x in enumerate((-.22,0,.22),1):cyl(f"HEIGHT_CHANNEL_{i}",(x,-1.265,2.08),.025,.02,M["yellow"],"Y",False,24)
    before=set(bpy.data.objects);box("REFERENCE_BELT",(0,0,.48),(.62,2.40,.12),M["dark"],.008,False);box("REFERENCE_PARCEL",(-.06,0,.88),(.46,.70,.68),M["white"],.025,False)
    for o in set(bpy.data.objects)-before:o["rungproof_review_only"]=True
BUILDERS={"liquid_metering_skid":metering,"two_position_container_receiver":receiver,"three_height_parcel_sensor_bank":sensor_bank}
def point(o,t):o.rotation_euler=(Vector(t)-o.location).to_track_quat("-Z","Y").to_euler()
def save(slug,builder):
    clean();M=mats();builder(M);root=BASE/slug
    for p in ("source","delivery","collision","review"):(root/p).mkdir(parents=True,exist_ok=True)
    for p in (root/"source",root/"review"):(p/".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"));objects=[o for o in bpy.context.scene.objects if o.type=="MESH" and o.get("rungproof_asset") and not o.get("rungproof_review_only")]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    mins=Vector((1e9,1e9,1e9));maxs=Vector((-1e9,-1e9,-1e9))
    for o in objects:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c);mins.x,mins.y,mins.z=min(mins.x,w.x),min(mins.y,w.y),min(mins.z,w.z);maxs.x,maxs.y,maxs.z=max(maxs.x,w.x),max(maxs.y,w.y),max(maxs.z,w.z)
    bpy.ops.object.select_all(action="DESELECT");bpy.ops.mesh.primitive_cube_add(location=(mins+maxs)/2);proxy=bpy.context.object;proxy.name="COLLISION_primary";proxy.dimensions=maxs-mins;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bpy.ops.export_scene.gltf(filepath=str(root/"collision"/f"{slug}_collision.glb"),export_format="GLB",use_selection=True,export_apply=True);bpy.data.objects.remove(proxy,do_unlink=True)
    center=(mins+maxs)/2;span=maxs-mins;radius=max(span.length*1.42,4.0);floor=box("REVIEW_floor",(0,0,-.035),(5.2,5.2,.05),M["dark"],.002,False);floor["rungproof_asset"]=False
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.018,.024,.028)
    for i,(loc,e,s) in enumerate((((4,-5,6),1300,4),((-4,-1,3),850,3),((0,4,5),950,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;s=bpy.context.scene;s.render.engine="BLENDER_EEVEE";s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.image_settings.file_format="PNG"
    for i,a in enumerate((305,215,35,125)):
        q=math.radians(a);cam.location=(center.x+radius*math.cos(q),center.y+radius*math.sin(q),center.z+radius*.38);point(cam,center);s.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    (root/"thumbnail.png").write_bytes((root/"review"/f"{slug}_01.png").read_bytes());BUILT.append(slug)
flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("SCENE_SUPPORT_ASSETS_BUILT",len(BUILT))
