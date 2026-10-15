"""Build reusable tote filling, capping, labeling, and vision stations."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]);BASE=ROOT/"assets"/"tote_processing";BUILT=[]
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
def cone(n,l,r1,r2,depth,m,c=True,verts=64):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=l);return finish(bpy.context.object,n,m,.002,True,c)
def tube(n,a,b,rad,m,c=True):
    a,b=Vector(a),Vector(b);v=b-a;o=cyl(n,(a+b)/2,rad,v.length,m,"Z",c,40);o.rotation_euler=v.to_track_quat("Z","Y").to_euler();return o
def torus(n,l,major,minor,m,axis="Z",c=False):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=64,minor_segments=12,location=l,rotation=rot);return finish(bpy.context.object,n,m,0,True,c)
def text_mesh(n,text_value,l,size,m,r=(math.pi/2,0,0)):
    d=bpy.data.curves.new(n,"FONT");d.body=text_value;d.align_x="CENTER";d.align_y="CENTER";d.size=size;d.extrude=.006;d.bevel_depth=.001;o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);o.location=l;o.rotation_euler=r;bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target="MESH");return finish(o,n,m,0,False,False)
def materials():return {"blue":mat("Painted blue",(.025,.22,.43),.42,.30),"steel":mat("Stainless steel",(.58,.64,.66),.85,.17),"dark":mat("Dark frame",(.018,.028,.035),.32,.50),"yellow":mat("Safety yellow",(.96,.57,.01),.15,.30),"white":mat("Label stock",(.92,.93,.89),.02,.64),"black":mat("Elastomer",(.006,.009,.012),.02,.72),"red":mat("Indicator red",(.72,.01,.008),.12,.28),"green":mat("Indicator green",(.02,.55,.15),.08,.28),"glass":mat("Guard polycarbonate",(.05,.38,.45),.08,.18,.28),"plastic":mat("Molded tote plastic",(.10,.32,.48),.03,.48),"liquid":mat("Process liquid",(.02,.55,.82),.02,.16,.55)}
def frame(M,w=1.65,h=2.55,d=1.35):
    box("BASE_PLATE",(0,0,.06),(w,d,.12),M["dark"],.012)
    for x in (-w*.44,w*.44):
        for y in (-d*.38,d*.38):box(f"FRAME_POST_{x}_{y}",(x,y,h*.48),(.12,.12,h*.92),M["steel"],.012)
    for y in (-d*.38,d*.38):box(f"FRAME_HEADER_{y}",(0,y,h-.15),(w,.14,.16),M["steel"],.012)
    box("TOP_CROSSBEAM",(0,0,h-.15),(.16,d,.16),M["steel"],.012)
    box("CONTROL_ENCLOSURE",(w*.55,d*.43,1.55),(.42,.30,.72),M["blue"],.025)
    box("HMI_SCREEN",(w*.55,d*.265,1.66),(.28,.02,.22),M["glass"],.004,False)
    cyl("ESTOP",(w*.55,d*.24,1.42),.052,.035,M["red"],"Y",False,32)
def tote(M,cap=True):
    before=set(bpy.data.objects)
    box("REFERENCE_TOTE",(0,0,.72),(1.02,.82,1.18),M["white"],.055,False)
    box("TOTE_TOP_RIM",(0,0,1.16),(1.12,.92,.10),M["steel"],.012,False)
    box("TOTE_BOTTOM_RIM",(0,0,.20),(1.12,.92,.10),M["steel"],.012,False)
    cyl("TOTE_NECK",(0,0,1.30),.16,.20,M["white"],"Z",False,48)
    if cap:cyl("TOTE_CAP",(0,0,1.42),.18,.07,M["dark"],"Z",False,48)
    for x in (-.48,-.24,0,.24,.48):box(f"TOTE_CAGE_FRONT_V_{x}",(x,-.425,.69),(.025,.025,.92),M["steel"],.003,False)
    for z in (.32,.54,.76,.98):box(f"TOTE_CAGE_FRONT_H_{z}",(0,-.425,z),(1.02,.025,.025),M["steel"],.003,False)
    for y in (-.36,-.12,.12,.36):box(f"TOTE_CAGE_SIDE_V_{y}",(.515,y,.69),(.025,.025,.92),M["steel"],.003,False)
    for z in (.32,.54,.76,.98):box(f"TOTE_CAGE_SIDE_H_{z}",(.515,0,z),(.025,.82,.025),M["steel"],.003,False)
    for x in (-.40,.40):box(f"TOTE_PALLET_FOOT_{x}",(x,0,.10),(.18,.82,.16),M["dark"],.012,False)
    for o in set(bpy.data.objects)-before:o["rungproof_review_only"]=True
def filler(M):
    box("FILLER_BASE",(0,0,.06),(1.9,1.5,.12),M["dark"],.012);box("FILLER_COLUMN",(.66,.40,1.30),(.28,.30,2.45),M["blue"],.035);box("NOZZLE_BOOM",(.28,.10,2.34),(1.10,.30,.26),M["blue"],.035);tote(M,False);box("DOSING_VALVE_BLOCK",(0,0,2.15),(.40,.34,.30),M["blue"],.035);cyl("PRODUCT_INLET",(.28,.10,2.48),.09,.46,M["steel"],"Z")
    for x in (-.14,.14):cyl(f"NOZZLE_GUIDE_{x}",(x,.08,1.88),.025,.55,M["steel"],"Z",False,32)
    cyl("KIN_fill_nozzle",(0,0,1.82),.060,.46,M["steel"],"Z");cone("NOZZLE_TIP",(0,0,1.53),.025,.060,.14,M["steel"],False,40);cyl("VISIBLE_LIQUID_STREAM",(0,0,1.39),.020,.20,M["liquid"],"Z",False,32);tube("PRODUCT_HOSE",(-.48,0,2.68),(-.48,.44,2.40),.055,M["black"],False);tube("PRODUCT_HOSE_DROP",(-.48,.44,2.40),(0,.30,2.18),.055,M["black"],False)
    cyl("SUPPLY_RESERVOIR",(-.62,.36,.78),.28,1.25,M["steel"],"Z",False);cyl("SUPPLY_SIGHT_GLASS",(-.86,.16,.78),.055,.78,M["liquid"],"Z",False,32);cyl("FLOW_METER",(-.48,.16,2.52),.13,.20,M["glass"],"Y",False,48);tube("SUPPLY_PIPE_RISER",(-.62,.36,1.40),(-.62,.36,2.45),.045,M["steel"],False);tube("SUPPLY_PIPE_HEADER",(-.62,.36,2.45),(-.15,.12,2.45),.045,M["steel"],False)
    box("FILLER_CONTROL_PANEL",(.78,-.10,1.60),(.32,.22,.55),M["blue"],.025);box("FILLER_HMI",(.78,-.225,1.68),(.22,.025,.18),M["glass"],.004,False);cyl("FILLER_ESTOP",(.78,-.245,1.48),.045,.035,M["red"],"Y",False,32)
    for x in (-.28,.28):cyl(f"DRIP_TRAY_DRAIN_{x}",(x,0,.28),.035,.35,M["steel"],"Z",False,32)
    box("DRIP_TRAY",(0,0,.20),(1.28,.96,.12),M["steel"],.025)
def capper(M):
    box("CAPPER_BASE",(0,0,.06),(1.9,1.5,.12),M["dark"],.012);box("CAPPER_COLUMN",(.68,.38,1.28),(.30,.34,2.40),M["blue"],.04);box("CAPPER_CANTILEVER",(.30,.10,2.22),(1.10,.38,.30),M["blue"],.04);tote(M);box("CAPPER_HEAD",(0,0,2.04),(.62,.54,.36),M["blue"],.045);cyl("DRIVE_MOTOR",(0,0,2.38),.22,.44,M["blue"],"Z");cyl("KIN_capper_spindle",(0,0,1.70),.085,.55,M["steel"],"Z");cyl("TORQUE_CHUCK",(0,0,1.37),.18,.18,M["yellow"],"Z")
    cyl("CAP_HOPPER",(-.52,.34,2.18),.34,.34,M["steel"],"Z");cone("CAP_HOPPER_FUNNEL",(-.52,.34,1.91),.10,.34,.30,M["steel"],False);tube("CAP_CHUTE",(-.52,.18,1.82),(-.20,-.05,1.47),.085,M["steel"],False)
    box("CAP_FEED_RAIL",(-.38,-.28,1.55),(.70,.22,.06),M["steel"],.008,False)
    for i in range(4):
        x=-.62+i*.16;cyl(f"CAP_ON_FEED_RAIL_{i}",(x,-.28,1.62),.080,.065,M["blue"],"Z",False,48);torus(f"CAP_RIB_{i}",(x,-.28,1.64),.072,.008,M["dark"],"Z",False)
    box("CAP_ESCAPEMENT",(-.12,-.28,1.55),(.22,.28,.16),M["yellow"],.012,False);cyl("CAP_UNDER_CHUCK",(0,0,1.43),.145,.09,M["yellow"],"Z",False,48);cyl("TORQUE_CHUCK_BODY",(0,0,1.54),.19,.14,M["dark"],"Z",False,48);torus("CHUCK_GRIP_RING",(0,0,1.48),.15,.025,M["black"],"Z",False);box("CAPPER_CONTROL_PANEL",(.76,-.12,1.62),(.30,.22,.52),M["blue"],.025);box("CAPPER_HMI",(.76,-.245,1.70),(.20,.025,.16),M["glass"],.004,False);box("CAPPER_NAMEPLATE",(.25,-.272,2.22),(.56,.025,.15),M["white"],.004,False);text_mesh("CAPPER_NAME","CAP TORQUE",(.25,-.288,2.22),.105,M["dark"]);box("GUARD_FRONT",(0,-.52,1.72),(1.18,.035,1.05),M["glass"],.008,False)
def labeler(M):
    box("LABELER_BASE",(0,0,.06),(1.9,1.5,.12),M["dark"],.012);tote(M);box("LABELER_PEDESTAL",(-.68,.25,1.15),(.38,.42,2.08),M["blue"],.045);box("PRINT_APPLY_ENCLOSURE",(-.48,-.16,1.72),(.70,.48,.72),M["blue"],.05);box("LABEL_ROLL_WINDOW",(-.48,-.415,1.82),(.48,.025,.38),M["glass"],.004,False);cyl("KIN_label_roll",(-.48,-.44,1.84),.15,.07,M["white"],"Y",False);cyl("ROLL_CORE",(-.48,-.48,1.84),.048,.08,M["dark"],"Y",False)
    box("LABEL_EXIT_SLOT",(-.16,-.42,1.58),(.22,.035,.08),M["dark"],.004,False);cyl("TAMP_AIR_CYLINDER",(-.05,-.18,1.42),.075,.48,M["steel"],"X",False,40);box("TAMP_SLIDE",(.20,-.18,1.42),(.42,.10,.10),M["steel"],.008,False);box("APPLICATOR_PAD",(.40,-.18,1.42),(.12,.40,.46),M["yellow"],.025);box("LABEL_ON_TAMP",(.466,-.18,1.42),(.018,.30,.34),M["white"],.002,False)
    for z in (1.31,1.37,1.45,1.53):box(f"TAMP_BAR_{z}",(.478,-.18,z),(.010,.22,.018),M["black"],.001,False)
    applied=box("APPLIED_LABEL",(0,-.455,.78),(.62,.025,.34),M["white"],.004,False);applied["rungproof_review_only"]=True
    for x in (-.22,-.15,-.08,.02,.10,.18):
        bar=box(f"LABEL_BARCODE_{x}",(x,-.472,.78),(.025,.012,.22),M["black"],.001,False);bar["rungproof_review_only"]=True
    box("LABELER_NAMEPLATE",(-.48,-.438,2.06),(.54,.025,.14),M["white"],.004,False);text_mesh("LABELER_NAME","PRINT + APPLY",(-.48,-.454,2.06),.085,M["dark"])
def vision(M):
    box("VISION_BASE",(0,0,.06),(1.9,1.55,.12),M["dark"],.012);tote(M)
    for x in (-.72,.72):box(f"VISION_ARCH_POST_{x}",(x,.15,1.25),(.18,.22,2.35),M["steel"],.018)
    box("VISION_ARCH_HEADER",(0,.15,2.35),(1.62,.22,.20),M["steel"],.018);box("CAMERA_BODY",(0,-.16,2.06),(.42,.42,.30),M["dark"],.025);box("MAIN_CAMERA_BEZEL",(0,-.385,2.04),(.52,.045,.42),M["dark"],.018,False);cyl("KIN_vision_lens",(0,-.44,2.04),.12,.17,M["black"],"Y",False,48);cyl("MAIN_OPTIC_GLASS",(0,-.53,2.04),.078,.018,M["glass"],"Y",False,48)
    for x,z in ((-.18,2.04),(.18,2.04),(0,1.86),(0,2.22)):box(f"MAIN_LED_{x}_{z}",(x,-.420,z),(.10,.07,.055),M["white"],.008,False)
    for x in (-.55,.55):
        box(f"SIDE_CAMERA_BRACKET_{x}",((x+(.72 if x>0 else -.72))/2,-.01,1.62),(.20,.18,.07),M["steel"],.008,False);box(f"SIDE_CAMERA_{x}",(x,-.18,1.62),(.28,.34,.24),M["dark"],.020);box(f"SIDE_CAMERA_BEZEL_{x}",(x,-.37,1.62),(.35,.04,.32),M["dark"],.014,False);cyl(f"SIDE_LENS_{x}",(x,-.415,1.62),.075,.13,M["black"],"Y",False,40);cyl(f"SIDE_OPTIC_GLASS_{x}",(x,-.485,1.62),.048,.015,M["glass"],"Y",False,40);box(f"SIDE_LED_{x}",(x,-.405,1.75),(.18,.06,.055),M["white"],.008,False)
    box("LINE_LIGHT",(0,-.48,1.32),(.92,.045,.10),M["white"],.015,False);box("BACKLIGHT_PANEL",(0,.48,1.18),(1.20,.05,1.08),M["white"],.02,False);tube("CAMERA_CABLE",(0,.12,2.18),(.62,.42,1.92),.026,M["black"],False)
BUILDERS={"tote_filling_station":filler,"tote_capping_station":capper,"tote_labeling_station":labeler,"tote_vision_inspection_station":vision}
def point(o,t):o.rotation_euler=(Vector(t)-o.location).to_track_quat("-Z","Y").to_euler()
def save(slug,builder):
    clean();M=materials();builder(M);root=BASE/slug
    for p in ("source","delivery","collision","review"):(root/p).mkdir(parents=True,exist_ok=True)
    for p in (root/"source",root/"review"):(p/".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"));objects=[o for o in bpy.context.scene.objects if o.type=="MESH" and o.get("rungproof_asset") and not o.get("rungproof_review_only")]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    mins=Vector((1e9,1e9,1e9));maxs=Vector((-1e9,-1e9,-1e9))
    for o in [x for x in objects if x.get("rungproof_collision")]:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c);mins.x,mins.y,mins.z=min(mins.x,w.x),min(mins.y,w.y),min(mins.z,w.z);maxs.x,maxs.y,maxs.z=max(maxs.x,w.x),max(maxs.y,w.y),max(maxs.z,w.z)
    bpy.ops.object.select_all(action="DESELECT");bpy.ops.mesh.primitive_cube_add(location=(mins+maxs)/2);proxy=bpy.context.object;proxy.name="COLLISION_primary";proxy.dimensions=maxs-mins;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bpy.ops.export_scene.gltf(filepath=str(root/"collision"/f"{slug}_collision.glb"),export_format="GLB",use_selection=True,export_apply=True);bpy.data.objects.remove(proxy,do_unlink=True)
    vmin=Vector((1e9,1e9,1e9));vmax=Vector((-1e9,-1e9,-1e9))
    for o in objects:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c);vmin.x,vmin.y,vmin.z=min(vmin.x,w.x),min(vmin.y,w.y),min(vmin.z,w.z);vmax.x,vmax.y,vmax.z=max(vmax.x,w.x),max(vmax.y,w.y),max(vmax.z,w.z)
    center=(vmin+vmax)/2;span=vmax-vmin;radius=max(span.length*1.42,4.0);floor=box("REVIEW_floor",(0,0,-.035),(5.2,5.2,.05),M["dark"],.002,False);floor["rungproof_asset"]=False
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.018,.024,.028)
    for i,(loc,e,s) in enumerate((((4,-5,6),1300,4),((-4,-1,3),850,3),((0,4,5),950,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=e;d.shape="DISK";d.size=s;l=bpy.data.objects.new(d.name,d);l.location=loc;bpy.context.collection.objects.link(l);point(l,center)
    d=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",d);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;d.lens=58;s=bpy.context.scene;s.render.engine="BLENDER_EEVEE";s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.image_settings.file_format="PNG"
    review_angles=(215,305,35,125) if slug=="tote_vision_inspection_station" else (305,215,35,125)
    for i,a in enumerate(review_angles):
        q=math.radians(a);cam.location=(center.x+radius*math.cos(q),center.y+radius*math.sin(q),center.z+radius*.38);point(cam,center);s.render.filepath=str(root/"review"/f"{slug}_{i+1:02d}.png");bpy.ops.render.render(write_still=True)
    (root/"thumbnail.png").write_bytes((root/"review"/f"{slug}_01.png").read_bytes());BUILT.append(slug)
flt={v.strip() for v in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if v.strip()}
for slug,builder in BUILDERS.items():
    if not flt or slug in flt:save(slug,builder)
print("TOTE_PROCESSING_ASSETS_BUILT",len(BUILT))
