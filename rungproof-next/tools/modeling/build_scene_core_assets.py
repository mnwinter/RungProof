"""Build the first scene-critical industrial asset tranche in Blender 5.2."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Matrix, Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
BASE=ROOT/"assets"/"scene_core"
bpy.context.preferences.filepaths.save_version=0
ASSETS=[]

def clean():
    bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes,bpy.data.curves,bpy.data.cameras,bpy.data.lights,bpy.data.materials):
        for block in list(blocks):
            if block.users==0: blocks.remove(block)

def mat(name,c,metal=.0,rough=.4):
    m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
    b=m.node_tree.nodes.get("Principled BSDF"); b.inputs["Base Color"].default_value=(*c,1); b.inputs["Metallic"].default_value=metal; b.inputs["Roughness"].default_value=rough
    return m

def finish(o,name,m=None,bevel=0,smooth=False,asset=True,collision=True):
    o.name=name
    if m:o.data.materials.append(m)
    if bevel:
        mod=o.modifiers.new("Manufactured edge radius","BEVEL");mod.width=bevel;mod.segments=4;mod.limit_method="ANGLE"
    if smooth and hasattr(o.data,"polygons"):
        for p in o.data.polygons:p.use_smooth=True
    o["rungproof_asset"]=asset
    o["rungproof_collision"]=collision
    return o

def box(n,loc,dims,m,bev=.006,asset=True,collision=True):
    bpy.ops.mesh.primitive_cube_add(location=loc);o=bpy.context.object;o.dimensions=dims;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m,bev,asset=asset,collision=collision)

def cyl(n,loc,r,d,m,axis="Z",verts=96,asset=True,collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d,location=loc,rotation=rot);return finish(bpy.context.object,n,m,.0015,True,asset,collision)

def torus(n,loc,major,minor,m,axis="Z",asset=True,collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0))
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=96,minor_segments=16,location=loc,rotation=rot);return finish(bpy.context.object,n,m,0,True,asset,collision)

def annular_cylinder(n,loc,outer,inner,depth,m,axis="Z",segments=96,asset=True,collision=True):
    """Manufactured hollow ring with planar faces and a real through-bore."""
    vertices=[];faces=[]
    for axial in (-depth/2,depth/2):
        for radius in (outer,inner):
            for i in range(segments):
                angle=2*math.pi*i/segments
                vertices.append((radius*math.cos(angle),radius*math.sin(angle),axial))
    outer_a,inner_a,outer_b,inner_b=0,segments,segments*2,segments*3
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((outer_a+i,outer_a+j,outer_b+j,outer_b+i),
                      (inner_a+j,inner_a+i,inner_b+i,inner_b+j),
                      (outer_a+i,inner_a+i,inner_a+j,outer_a+j),
                      (outer_b+j,inner_b+j,inner_b+i,outer_b+i)))
    mesh=bpy.data.meshes.new(n+"_mesh");mesh.from_pydata(vertices,[],faces);mesh.update()
    o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o)
    # Smooth only the radial walls.  Smoothing planar annular faces creates
    # dark wedges and false faceting around otherwise round process bores.
    for index,polygon in enumerate(mesh.polygons):polygon.use_smooth=index%4 in (0,1)
    if axis=="X":o.rotation_euler.y=math.pi/2
    elif axis=="Y":o.rotation_euler.x=math.pi/2
    o.location=loc
    return finish(o,n,m,.0015,False,asset,collision)

def bolt(n,loc,m,axis="Z"):
    return cyl(n,loc,.009,.012,m,axis,6)

def cable(n,points,r,m):
    c=bpy.data.curves.new(n+"_curve","CURVE");c.dimensions="3D";c.bevel_depth=r;c.bevel_resolution=4
    s=c.splines.new("BEZIER");s.bezier_points.add(len(points)-1)
    for p,v in zip(s.bezier_points,points):p.co=v;p.handle_left_type="AUTO";p.handle_right_type="AUTO"
    o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o);return finish(o,n,m)

def text_label(n,body,loc,size,m,rotation=(math.pi/2,0,0),asset=True):
    bpy.ops.object.text_add(location=loc,rotation=rotation);o=bpy.context.object;o.data.body=body;o.data.align_x="CENTER";o.data.align_y="CENTER";o.data.size=size;o.data.extrude=.003;o.data.bevel_depth=.001
    bpy.ops.object.convert(target="MESH");return finish(o,n,m,0,False,asset)

def common():
    return dict(
      steel=mat("Powder coated steel",(.055,.12,.16),.55,.28),galv=mat("Galvanized steel",(.48,.52,.54),.82,.28),
      black=mat("Black polymer",(.018,.023,.027),.08,.34),blue=mat("Sensor blue",(.025,.24,.52),.18,.30),
      red=mat("Safety red",(.72,.012,.008),.14,.27),yellow=mat("Safety yellow",(.95,.56,.012),.18,.31),
      green=mat("Signal green",(.03,.55,.18),.05,.20),amber=mat("Signal amber",(1,.42,.01),.05,.20),
      lens=mat("Optical lens",(.02,.58,.82),.03,.12),white=mat("Label",(.88,.90,.88),.02,.55),
      aluminum=mat("Machined aluminum",(.42,.45,.47),.88,.20),orange=mat("Pneumatic orange",(1,.20,.015),.05,.36))

def emissive(name,c,strength=3.0):
    m=mat(name,c,.02,.18);b=m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Emission Color"].default_value=(*c,1);b.inputs["Emission Strength"].default_value=strength
    return m

def translucent_emissive(name,c,alpha=.20,strength=1.8):
    m=emissive(name,c,strength);b=m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Alpha"].default_value=alpha;m.diffuse_color=(*c,alpha);m.surface_render_method="BLENDED"
    return m

def translucent(name,c,alpha=.20):
    m=mat(name,c,.08,.18);b=m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Alpha"].default_value=alpha;m.diffuse_color=(*c,alpha);m.surface_render_method="BLENDED"
    return m

def photoeye(M):
    optic=emissive("Photoeye red optic",(1.0,.015,.005),4.0)
    for side,y in (("TX",-.72),("RX",.72)):
        box(f"{side}_foot",(0,y,.025),(.22,.20,.05),M["galv"],.004)
        box(f"{side}_post",(0,y,.49),(.055,.055,.92),M["steel"],.004)
        box(f"{side}_adjust_bracket",(0,y,.88),(.18,.11,.16),M["galv"],.004)
        # Compact rectangular opposing heads with protected lenses, neutral
        # indicators, brackets, and M12 tails; no vendor color or marking.
        box(f"{side}_housing",(0,y,.94),(.18,.20,.28),M["black"],.014)
        face_y=y+(.086 if y<0 else -.086)
        cyl(f"{side}_lens_bezel",(0,face_y,.94),.055,.024,M["black"],"Y")
        cyl(f"{side}_lens",(0,face_y+(.015 if y<0 else -.015),.94),.034,.014,optic,"Y")
        cyl(f"{side}_status_led",(.065,face_y+(.014 if y<0 else -.014),.985),.010,.012,M["green"],"Y",32)
        for dx in (-.055,.055): bolt(f"{side}_mount_bolt_{dx}",(dx,y-.061,.90),M["black"],"Y")
        cable(f"{side}_cable",[(0,y,.87),(.12,y,.72),(.12,y,.18)],.014,M["black"])
        # The pigtail must visibly terminate in a real connector rather than
        # ending as an unexplained cable stub in the review or simulator.
        cyl(f"{side}_m12_connector",(.12,y,.135),.026,.090,M["black"],"Z",48)
        cyl(f"{side}_m12_coupling",(.12,y,.078),.030,.025,M["aluminum"],"Z",48)
    # A photoelectric beam is an optical path, not a structural rod.  Short
    # dashed emissive witnesses keep it legible without creating a solid bar.
    for i,y in enumerate((-.48,-.24,0,.24,.48)):
        box(f"KIN_beam_dash_{i}",(0,y,.94),(.006,.12,.006),optic,.001)

def pushbutton(M):
    # Compact floor-standing operator station.  The enclosure is intentionally
    # smaller than the support structure so it reads as usable industrial
    # hardware instead of a tall toy-like cabinet.
    box("ENCLOSURE_body",(0,0,.98),(.50,.32,.68),M["steel"],.014)
    box("ENCLOSURE_door",(0,-.172,.98),(.44,.025,.59),M["galv"],.006)
    for x in (-.19,.19):
      for z in (.73,1.23): bolt(f"door_screw_{x}_{z}",(x,-.189,z),M["black"],"Y")
    box("NAMEPLATE",(0,-.194,1.20),(.32,.012,.09),M["white"],.004)
    text_label("NAMEPLATE_text","STATION 01",(0,-.203,1.20),.043,M["black"],(math.pi/2,0,0))
    cyl("BUTTON_bezel",(0,-.205,1.02),.082,.040,M["black"],"Y",128)
    cyl("KIN_pushbutton",(0,-.242,1.02),.061,.065,M["green"],"Y",128)
    text_label("BUTTON_start_label","START",(0,-.203,1.115),.048,M["black"],(math.pi/2,0,0))
    cyl("ESTOP_bezel",(0,-.205,.80),.085,.045,M["yellow"],"Y",128)
    cyl("KIN_estop",(0,-.255,.80),.064,.072,M["red"],"Y",128)
    box("ESTOP_nameplate",(0,-.194,.91),(.18,.012,.050),M["white"],.003)
    text_label("ESTOP_label","STOP",(0,-.203,.91),.032,M["black"],(math.pi/2,0,0))
    box("PEDESTAL_mounting_flange",(0,.02,.635),(.42,.30,.055),M["galv"],.006)
    for x in (-.16,.16):
      for y in (-.08,.12):cyl(f"PEDESTAL_mount_bolt_{x}_{y}",(x,y,.668),.012,.025,M["black"],"Z",48)
    box("PEDESTAL_post",(0,.02,.32),(.22,.20,.62),M["steel"],.008)
    box("PEDESTAL_foot",(0,.02,.035),(.48,.42,.07),M["galv"],.008)
    for x in (-.18,.18):
      for y in (-.14,.18):
        cyl(f"PEDESTAL_anchor_washer_{x}_{y}",(x,y,.074),.030,.012,M["black"],"Z",48)
        cyl(f"PEDESTAL_anchor_bolt_{x}_{y}",(x,y,.094),.016,.052,M["black"],"Z",48)
    # Field wiring is internal through the hollow structural pedestal.  This
    # avoids an exposed snag loop and matches common floor-standing stations.

def single_pushbutton(M):
    box("ENCLOSURE_body",(0,0,.76),(.44,.30,.58),M["steel"],.04);box("ENCLOSURE_face",(0,-.164,.76),(.36,.025,.48),M["galv"],.012)
    # A small engraved-style legend sits directly above the one momentary
    # operator.  Keeping it off the fascia fasteners avoids a false impression
    # of a separate display or a detachable control module.
    box("BUTTON_legend_plate",(0,-.184,.90),(.18,.012,.052),M["black"],.003)
    text_label("BUTTON_start_label","START",(0,-.193,.90),.034,M["white"])
    cyl("BUTTON_bezel",(0,-.19,.72),.096,.040,M["black"],"Y")
    cyl("BUTTON_retaining_ring",(0,-.218,.72),.084,.018,M["galv"],"Y",64);cyl("KIN_pushbutton",(0,-.238,.72),.065,.040,M["green"],"Y")
    for x in (-.145,.145):
      for z in (.56,.96):bolt(f"ENCLOSURE_fascia_screw_{x}_{z}",(x,-.186,z),M["black"],"Y")
    # The load path is a square hollow pedestal, a bolted enclosure flange,
    # and a substantially wider floor plate--not decorative angled tabs.
    box("PEDESTAL_mounting_flange",(0,0,.47),(.36,.26,.055),M["galv"],.006)
    for x in (-.13,.13):
      for y in (-.08,.08): bolt(f"PEDESTAL_mount_bolt_{x}_{y}",(x,y,.505),M["black"],"Z")
    box("PEDESTAL",(0,0,.25),(.14,.14,.40),M["steel"],.008);box("FOOT",(0,0,.035),(.42,.36,.07),M["galv"],.008)
    for x in (-.15,.15):
      for y in (-.12,.12): bolt(f"PEDESTAL_anchor_{x}_{y}",(x,y,.077),M["black"],"Z")
    # Rear conduit terminates in a real gland and remains close to the support;
    # the exposed segment is protected rather than a loose hanging cable.
    cyl("ENCLOSURE_rear_cable_gland",(0,.164,.56),.034,.070,M["black"],"Y",48)
    cyl("ENCLOSURE_rear_gland_locknut",(0,.205,.56),.044,.022,M["galv"],"Y",12)
    cable("CTRL_rear_conduit",[(0,.215,.56),(0,.235,.46),(0,.115,.42),(0,.115,.08)],.018,M["black"])

def emergency_stop(M):
    # This is a command-station representation, not a certified safety
    # assembly.  Its physical cues nevertheless need to make the red
    # mushroom, yellow mounting field, and deliberate reset action legible.
    enclosure=mat("E-stop textured enclosure",(.045,.105,.14),.42,.50)
    box("ENCLOSURE_body",(0,0,.76),(.46,.32,.56),enclosure,.025)
    box("ENCLOSURE_door",(0,-.174,.76),(.38,.025,.46),M["galv"],.012)
    cyl("ESTOP_yellow_backplate",(0,-.194,.72),.125,.014,M["yellow"],"Y",128)
    for x in (-.16,.16):
      for z in (.56,.96): bolt(f"ENCLOSURE_fascia_screw_{x}_{z}",(x,-.190,z),M["black"],"Y")
    # Short high-contrast legends remain readable in a catalog overview; the
    # asset category carries the full emergency-stop context without relying
    # on a tiny sentence rendered as geometry.
    box("ESTOP_legend_plate",(0,-.205,.955),(.28,.012,.110),M["black"],.003)
    text_label("ESTOP_legend_emergency","EMERGENCY",(0,-.214,.982),.025,M["white"])
    text_label("ESTOP_legend_stop","STOP",(0,-.214,.936),.042,M["white"])
    cyl("ESTOP_contact_block",(0,-.214,.72),.096,.050,M["yellow"],"Y",96)
    cyl("ESTOP_bezel",(0,-.242,.72),.086,.022,M["black"],"Y",96)
    estop=cyl("KIN_estop",(0,-.276,.72),.078,.080,M["red"],"Y",128)
    # A wider, shallow red skirt produces a true mushroom silhouette.  It is
    # parented to the named motion member so the complete visible actuator
    # follows the catalog's linear press travel.
    cap=cyl("KIN_estop_mushroom_cap",(0,-.326,.72),.091,.032,M["red"],"Y",128)
    cap.parent=estop;cap.matrix_parent_inverse=estop.matrix_world.inverted()
    # A curved arrow is a physical twist-to-release cue on the mushroom cap,
    # not a separate reset control.  It moves with the actuator.
    arrow_points=[]
    for angle in range(210, 31, -30):
      radians=math.radians(angle);arrow_points.append((.060*math.cos(radians),-.348,.72+.060*math.sin(radians)))
    arrow=cable("KIN_estop_twist_arrow",arrow_points,.008,M["white"])
    arrow.parent=estop;arrow.matrix_parent_inverse=estop.matrix_world.inverted()
    bpy.ops.mesh.primitive_cone_add(vertices=3,radius1=.028,depth=.010,location=(.052,-.353,.750),rotation=(math.pi/2,0,math.radians(-35)))
    arrowhead=finish(bpy.context.object,"KIN_estop_twist_arrowhead",M["white"],.001,False)
    arrowhead.parent=estop;arrowhead.matrix_parent_inverse=estop.matrix_world.inverted()
    # Square hollow support, bolted flange, and anchors make the load path
    # readable without implying the station alone fulfils a safety function.
    box("PEDESTAL_mounting_flange",(0,0,.47),(.38,.28,.055),M["galv"],.006)
    for x in (-.14,.14):
      for y in (-.09,.09): bolt(f"PEDESTAL_mount_bolt_{x}_{y}",(x,y,.505),M["black"],"Z")
    box("PEDESTAL",(0,0,.25),(.15,.15,.40),M["steel"],.008)
    box("FOOT",(0,0,.035),(.44,.38,.07),M["galv"],.008)
    for x in (-.16,.16):
      for y in (-.13,.13):
        cyl(f"PEDESTAL_anchor_washer_{x}_{y}",(x,y,.075),.027,.011,M["black"],"Z",48)
        cyl(f"PEDESTAL_anchor_bolt_{x}_{y}",(x,y,.094),.014,.046,M["black"],"Z",48)
    # Field wiring is intentionally routed at the rear and through the hollow
    # pedestal.  A front-visible route at this scale read as a false mechanical
    # lever, which is worse than a concealed, installation-specific connection.
    cyl("ENCLOSURE_rear_cable_gland",(0,.175,.56),.034,.070,M["black"],"Y",48)
    cyl("ENCLOSURE_rear_gland_locknut",(0,.216,.56),.044,.022,M["galv"],"Y",12)

def single_beacon(M):
    # Amber polycarbonate lens with a distinct internal lamp and external
    # Fresnel-like ribs.  The lens must remain visibly amber even when off.
    lens=translucent_emissive("Amber polycarbonate lens",(1.0,.18,.005),.82,1.0)
    core=emissive("Amber LED array",(1.0,.16,.004),3.0)
    base=mat("Beacon powder coat",(.055,.12,.16),.45,.48)
    box("BASE_foot",(0,0,.035),(.42,.40,.07),base,.010)
    for x in (-.15,.15):
      for y in (-.14,.14):
        cyl(f"BASE_anchor_washer_{x}_{y}",(x,y,.075),.030,.011,M["black"],"Z",48)
        cyl(f"BASE_anchor_bolt_{x}_{y}",(x,y,.095),.016,.045,M["black"],"Z",48)
    cyl("BASE_plinth",(0,0,.115),.105,.09,M["black"])
    cyl("BASE_cable_entry",(.215,0,.10),.027,.060,M["black"],"X",48)
    cyl("BASE_cable_gland_nut",(.235,0,.10),.034,.020,M["galv"],"X",12)
    cyl("POLE",(0,0,.60),.040,.88,M["galv"])
    cyl("LENS_single",(0,0,1.12),.155,.235,lens)
    for index,z in enumerate((1.045,1.10,1.155,1.21)):
      torus(f"LENS_fresnel_rib_{index}",(0,0,z),.157,.004,lens)
    torus("BEZEL_lower",(0,0,1.000),.158,.012,M["black"])
    torus("BEZEL_upper",(0,0,1.240),.158,.012,M["black"])
    cyl("CAP",(0,0,1.275),.160,.042,M["black"])
    cyl("LENS_reflector",(0,0,1.06),.095,.025,M["aluminum"],"Z",96)
    for index,z in enumerate((1.075,1.12,1.165)):
      cyl(f"LENS_led_module_{index}",(0,0,z),.045,.024,core,"Z",64)

def stacklight(M):
    box("BASE_foot",(0,0,.03),(.42,.38,.06),M["steel"],.012)
    for x in (-.15,.15):
      for y in (-.13,.13):
        cyl(f"BASE_anchor_washer_{x}_{y}",(x,y,.067),.025,.010,M["black"],"Z",48)
        cyl(f"BASE_anchor_bolt_{x}_{y}",(x,y,.083),.013,.035,M["black"],"Z",48)
    cyl("BASE_plinth",(0,0,.12),.16,.16,M["black"]);cyl("POLE",(0,0,.72),.035,1.08,M["galv"])
    for i,(name,color) in enumerate((("green",M["green"]),("amber",M["amber"]),("red",M["red"]))):
        z=1.30+i*.235
        cyl(f"LENS_{name}",(0,0,z),.14,.19,color)
        torus(f"BEZEL_{name}_lower",(0,0,z-.102),.142,.012,M["black"])
    torus("BEZEL_red_upper",(0,0,1.872),.142,.012,M["black"])
    cyl("CAP",(0,0,1.900),.145,.065,M["black"])
    cyl("BUZZER_housing",(0,0,1.980),.140,.095,M["black"],"Z",128)
    cyl("BUZZER_top_grille",(0,0,2.038),.125,.025,M["galv"],"Z",128)
    for a in range(0,360,45):
        x=.075*math.cos(math.radians(a));y=.075*math.sin(math.radians(a));cyl(f"BUZZER_port_{a}",(x,y,2.055),.010,.010,M["black"],"Z",48)
    cyl("BUZZER_center_port",(0,0,2.055),.012,.010,M["black"],"Z",48)

def pusher(M):
    box("FRAME_base",(.62,0,.07),(3.10,.72,.14),M["steel"],.012)
    for x in (-.48,.28,1.55):box(f"FRAME_pedestal_{x}",(x,0,.36),(.13,.58,.58),M["steel"],.012)
    # Guided pneumatic slide with 1.35 m usable stroke.  The fixed rails span
    # the full carriage envelope; the moving rod/carriage are named for the
    # runtime controller so extension remains mechanically legible.
    # A large-bore CS1-style actuator has a round barrel held between heavy
    # end caps by visible external tie rods.  Do not substitute a box just
    # because the mechanism is mounted in a rectangular machine frame.
    cyl("CYLINDER_barrel",(-.42,.02,.74),.225,.92,M["aluminum"],"X",128)
    cyl("CYLINDER_rear_cap",(-.91,.02,.74),.285,.105,M["black"],"X",96)
    cyl("CYLINDER_front_cap",(.07,.02,.74),.285,.105,M["blue"],"X",96)
    for y in (-.235,.235):
        for z in (.525,.955):
            cyl(f"CYLINDER_tie_rod_{y}_{z}",(-.42,y,z),.026,1.03,M["galv"],"X",32)
            cyl(f"CYLINDER_tie_nut_{y}_{z}",(.105,y,z),.044,.060,M["black"],"X",12)
    cyl("KIN_pusher_rod",(.55,.02,.74),.045,.96,M["galv"],"X")
    box("KIN_pusher_plate",(1.12,.02,.74),(.10,.58,.58),M["yellow"],.018)
    label=text_label("KIN_pusher_plate_label","PUSHER",(1.178,.02,.88),.070,M["black"],(0,0,0))
    label.rotation_euler=Matrix(((0,0,1),(1,0,0),(0,1,0))).to_euler()
    # Two guide shafts and bearing blocks make the plate's constrained linear
    # motion obvious and prevent the assembly reading as a loose ram.
    for y in (-.24,.24):
        cyl(f"KIN_pusher_guide_shaft_{y}",(.58,y,.74),.025,1.10,M["galv"],"X")
        box(f"GUIDE_bearing_{y}",(.82,y,.74),(.16,.13,.14),M["blue"],.018)
        box(f"KIN_pusher_bracket_{y}",(1.03,y,.74),(.20,.12,.22),M["steel"],.012)
    box("KIN_pusher_crossmember",(.97,.02,.74),(.16,.62,.16),M["steel"],.012)
    box("KIN_pusher_clevis",(1.02,.02,.74),(.22,.20,.20),M["steel"],.018)
    box("CYLINDER_rear_mount",(-.99,.02,.74),(.12,.54,.46),M["steel"],.018)
    for y in (-.20,.20):
        for z in (.56,.92):bolt(f"PLATE_bolt_{y}_{z}",(1.178,y,z),M["black"],"X")
    for y in (-.24,.24):cyl(f"KIN_pusher_guide_locknut_{y}",(1.17,y,.74),.050,.065,M["black"],"X",12)
    manifold_ports = {-.70: -.84, -.14: -.68}
    for x in (-.70,-.14):
        cyl(f"AIR_port_{x}",(x,-.10,.95),.025,.08,M["blue"],"Z",48)
        manifold_x = manifold_ports[x]
        cyl(f"AIR_manifold_fitting_{x}",(manifold_x,-.43,.49),.024,.10,M["blue"],"Z",48)
        cable(f"AIR_tube_{x}",[(x,-.10,.99),(x,-.22,.90),(manifold_x,-.36,.62),(manifold_x,-.43,.54)],.014,M["orange"])
    # Wear rails show the carton-transfer path without adding a product to the
    # reusable actuator asset.
    for y in (-.27,.27):box(f"TRANSFER_wear_rail_{y}",(1.70,y,.36),(1.85,.055,.055),M["galv"],.006)
    # Review-only product witness: proves the intended transfer function but
    # is excluded from the delivered reusable pusher GLB.
    review_deck=box("REVIEW_transfer_deck",(2.25,.02,.385),(1.10,.68,.055),M["black"],.006,False)
    review_carton=box("REVIEW_carton",(2.34,.02,.575),(.34,.48,.32),M["amber"],.025,False)
    review_deck["rungproof_review_only"]=True;review_carton["rungproof_review_only"]=True
    box("VALVE_manifold",(-.76,-.31,.33),(.48,.20,.32),M["aluminum"],.022)
    for x in (-.84,-.68):
        cyl(f"VALVE_solenoid_{x}",(x,-.43,.36),.052,.12,M["black"],"Y")
    for x,labelz in ((-.70,.74),(-.14,.74)):
        box(f"SENSOR_reed_{x}",(x,-.15,labelz),(.16,.055,.045),M["blue"],.008)

def motor(M):
    box("MOTOR_foot_L",(0,-.24,.08),(1.10,.18,.16),M["steel"],.008);box("MOTOR_foot_R",(0,.24,.08),(1.10,.18,.16),M["steel"],.008)
    # The external TEFC shell is the stationary stator housing.  Only the
    # shaft and the cooling fan use the KIN prefix at runtime.
    cyl("MOTOR_stator_body",(0,0,.48),.38,1.28,M["blue"],"X",128)
    # TEFC motors use axial cooling fins. Circumferential rings read as a
    # bellows/expansion joint, so model the real longitudinal ribs.
    for i,a in enumerate(range(0,360,30)):
        y=.37*math.cos(math.radians(a));z=.48+.37*math.sin(math.radians(a))
        fin=box(f"MOTOR_axial_fin_{i}",(-.03,y,z),(1.12,.035,.075),M["blue"],.008)
        fin.rotation_euler.x=math.radians(a)
    cyl("MOTOR_drive_end",(.69,0,.48),.31,.13,M["steel"],"X");cyl("MOTOR_fan_cowl",(-.69,0,.48),.41,.16,M["steel"],"X")
    cyl("KIN_motor_shaft",(.88,0,.48),.065,.30,M["galv"],"X")
    # The fan is a real rotating member behind a fixed guard, not a painted
    # disk. It shares the motor kinematic prefix and the shaft's X-axis.
    cyl("KIN_motor_fan_hub",(-.79,0,.48),.070,.075,M["black"],"X",64)
    for a in range(0,360,45):
        y=.19*math.cos(math.radians(a));z=.48+.19*math.sin(math.radians(a))
        blade=box(f"KIN_motor_fan_blade_{a}",(-.83,y/2,.48+(z-.48)/2),(.028,.34,.055),M["black"],.003)
        blade.rotation_euler.x=math.radians(a)
    box("MOTOR_terminal_box",(-.05,0,.94),(.46,.42,.25),M["steel"],.025)
    box("MOTOR_terminal_lid",(-.05,-.218,.94),(.37,.018,.17),M["galv"],.006)
    text_label("MOTOR_terminal_label","3~",(-.05,-.232,1.00),.055,M["black"],(math.pi/2,0,0))
    for x in (-.14,.14):
      for z in (.875,1.005): bolt(f"MOTOR_terminal_screw_{x}_{z}",(x,-.237,z),M["black"],"Y")
    cyl("MOTOR_terminal_cable_gland",(-.05,.255,.94),.048,.090,M["black"],"Y",48)
    torus("MOTOR_fan_grille",(-.78,0,.48),.31,.018,M["black"],"X")
    for a in range(0,360,45):
        y=.29*math.cos(math.radians(a));z=.48+.29*math.sin(math.radians(a));box(f"MOTOR_grille_spoke_{a}",(-.79,y/2,.48+(z-.48)/2),(.025,.58,.025),M["black"],.002).rotation_euler.x=math.radians(a)
    torus("MOTOR_lifting_eye",(-.12,0,1.10),.075,.015,M["galv"],"Y")
    for x in (-.42,.42):
      for y in (-.24,.24):bolt(f"MOTOR_mount_{x}_{y}",(x,y,.17),M["black"])

def pipe_spool(M):
    cyl("PIPE_body",(0,0,.88),.18,3.4,M["galv"],"X",128)
    for x in (-1.72,1.72):
      cyl(f"FLANGE_{x}",(x,0,.88),.31,.09,M["steel"],"X")
      cyl(f"PIPE_open_bore_{x}",(x+(-.051 if x<0 else .051),0,.88),.135,.012,M["black"],"X")
      for a in range(0,360,45):
        y=.245*math.cos(math.radians(a));z=.88+.245*math.sin(math.radians(a));cyl(f"FLANGE_bolt_{x}_{a}",(x+(.05 if x>0 else -.05),y,z),.016,.10,M["black"],"X",12)
    for x in (-1.05,1.05):
      box(f"SUPPORT_post_{x}",(x,0,.39),(.12,.12,.66),M["steel"],.006);box(f"SUPPORT_foot_{x}",(x,0,.035),(.48,.46,.07),M["galv"],.006)
      box(f"SUPPORT_saddle_{x}",(x,0,.72),(.34,.30,.10),M["steel"],.025)
    # Instrumented branch makes the object unambiguously process piping rather
    # than a conveyor roller while remaining a reusable spool assembly.
    cyl("BRANCH_neck",(0,0,1.18),.10,.62,M["galv"],"Z",96)
    cyl("BRANCH_flange",(0,0,1.47),.20,.08,M["steel"],"Z",96)
    # The gauge must be continuously piped to the branch flange.  Keeping its
    # stem visible prevents the dial from reading as a floating decoration.
    cyl("GAUGE_stem",(0,.12,1.62),.075,.25,M["galv"],"Z",64)
    cyl("GAUGE_body",(0,.12,1.78),.18,.12,M["steel"],"Y",96)
    cyl("GAUGE_face",(0,.19,1.78),.145,.025,M["white"],"Y",96)
    box("GAUGE_needle",(.035,.208,1.81),(.012,.012,.12),M["red"],.002).rotation_euler.y=math.radians(-35)

def tank(M):
    cyl("TANK_shell",(0,0,2.90),1.45,4.20,M["galv"],"Z",160)
    cyl("TANK_bottom",(0,0,.78),1.46,.18,M["steel"],"Z",160);cyl("TANK_roof",(0,0,5.02),1.47,.18,M["steel"],"Z",160)
    for a in (45,135,225,315):
      x=1.08*math.cos(math.radians(a));y=1.08*math.sin(math.radians(a));box(f"LEG_{a}",(x,y,.42),(.20,.20,.72),M["steel"],.012);box(f"FOOT_{a}",(x,y,.035),(.44,.44,.07),M["galv"],.006)
      for dx in (-.15,.15):
        for dy in (-.15,.15): bolt(f"FOOT_anchor_{a}_{dx}_{dy}",(x+dx,y+dy,.078),M["black"])
    cyl("MANWAY_neck",(0,0,5.20),.38,.25,M["galv"]);cyl("MANWAY_cover",(0,0,5.35),.43,.07,M["steel"])
    for a in range(0,360,30):
      x=.37*math.cos(math.radians(a));y=.37*math.sin(math.radians(a));bolt(f"MANWAY_bolt_{a}",(x,y,5.39),M["black"])
    box("MANWAY_hinge",(-.44,0,5.37),(.18,.12,.10),M["black"],.018)
    torus("MANWAY_handle",(.18,0,5.47),.12,.018,M["black"],"Y")
    # Ladder with safety cage and top handrail.
    for x in (-1.58,-1.30):box(f"LADDER_rail_{x}",(x,0,2.90),(.045,.045,4.20),M["galv"],.003)
    for i,z in enumerate([.90+i*.29 for i in range(15)]):box(f"LADDER_rung_{i}",(-1.44,0,z),(.32,.035,.035),M["galv"],.002)
    for i,z in enumerate((1.7,2.3,2.9,3.5,4.1,4.7)):
      torus(f"LADDER_cage_{i}",(-1.44,.0,z),.48,.018,M["galv"],"X")
    for a in range(0,360,30):
      x=1.36*math.cos(math.radians(a));y=1.36*math.sin(math.radians(a));cyl(f"RAIL_base_{a}",(x,y,5.13),.055,.035,M["steel"],"Z",48);box(f"RAIL_post_{a}",(x,y,5.39),(.035,.035,.50),M["galv"],.002)
    torus("RAIL_top",(0,0,5.63),1.37,.022,M["galv"])
    # A top rail without a continuous midrail is visually and physically
    # incomplete guarding.  Keep the ring concentric with the roof guard.
    torus("RAIL_mid",(0,0,5.39),1.37,.018,M["galv"])
    # Sight glass and nozzles.
    for z in (.9,4.35):cyl(f"SIGHT_valve_{z}",(1.50,0,z),.09,.22,M["steel"],"X")
    # The tube itself must be optically distinct from the liquid.  An opaque
    # cyan tube concealed every level state in runtime captures.
    sight_glass=translucent_emissive("Sight glass clear tube",(.18,.32,.38),.16,.04)
    cyl("SIGHT_glass",(1.62,0,2.62),.045,3.30,sight_glass,"Z")
    # A level indicator must remain readable from the operational scene
    # camera, not merely in a close-up.  Use a high-opacity illuminated fill
    # so state changes survive the tank's dark process-scene lighting.
    liquid=translucent_emissive("Sight glass process liquid",(.015,.48,.76),.82,2.4)
    cyl("KIN_liquid",(1.62,0,2.62),.031,3.10,liquid,"Z",96,collision=False)
    for i,z in enumerate([1.10+i*.36 for i in range(9)]):box(f"SIGHT_scale_{i}",(1.70,-.01,z),(.13,.025,.018),M["black"],.002)
    cyl("SIGHT_upper_handwheel",(1.70,0,4.35),.13,.04,M["black"],"X",64)
    cyl("SIGHT_lower_handwheel",(1.70,0,.90),.13,.04,M["black"],"X",64)
    cyl("NOZZLE_outlet",(0,-1.60,.95),.18,.42,M["steel"],"Y");cyl("NOZZLE_outlet_flange",(0,-1.83,.95),.30,.08,M["steel"],"Y")
    for a in range(0,360,45):
      x=.245*math.cos(math.radians(a));z=.95+.245*math.sin(math.radians(a));cyl(f"NOZZLE_outlet_bolt_{a}",(x,-1.88,z),.016,.10,M["black"],"Y",12)

def pump(M):
    rotor=bpy.data.objects.new("KIN_pump_shaft",None);rotor.location=(0,0,.50);rotor["rungproof_asset"]=True;bpy.context.collection.objects.link(rotor)
    def rotating(obj):
      obj.parent=rotor;obj.matrix_parent_inverse=rotor.matrix_world.inverted();return obj
    box("SKID",(0,0,.07),(2.6,1.1,.14),M["steel"],.012)
    for x in (-1.00,-.38,.46,.86):
      for y in (-.38,.38): bolt(f"SKID_anchor_{x}_{y}",(x,y,.15),M["black"])
    cyl("MOTOR_body",(-.70,0,.50),.34,1.05,M["blue"],"X",128)
    for x in (-1.05,-.85,-.65,-.45,-.25):torus(f"MOTOR_fin_{x}",(x,0,.50),.345,.015,M["blue"],"X")
    for x in (-.98,-.36):
      box(f"MOTOR_foot_{x}",(x,0,.25),(.20,.56,.13),M["steel"],.012)
      for y in (-.21,.21): bolt(f"MOTOR_mount_{x}_{y}",(x,y,.33),M["black"])
    cyl("MOTOR_fan_cover",(-1.27,0,.50),.30,.12,M["black"],"X",96)
    rotating(cyl("ROTOR_motor_fan_hub",(-1.31,0,.50),.21,.07,M["black"],"X",96))
    for a in range(0,360,45):
      y=.19*math.cos(math.radians(a));z=.50+.19*math.sin(math.radians(a));blade=box(f"ROTOR_motor_fan_{a}",(-1.36,y/2,.50+(z-.50)/2),(.035,.38,.028),M["black"],.002);blade.rotation_euler.x=math.radians(a);rotating(blade)
    # Fixed yellow coupling guard, with visible ventilation slots.  Rotating
    # hubs and shaft are separate so the guard does not appear to spin.
    cyl("COUPLING_guard",(.02,0,.50),.23,.55,M["yellow"],"X")
    for a in range(0,360,60):
      y=.23*math.cos(math.radians(a));z=.50+.23*math.sin(math.radians(a));box(f"COUPLING_guard_slot_{a}",(.02,y,z),(.35,.028,.05),M["black"],.003)
    rotating(cyl("ROTOR_coupling_shaft",(.02,0,.50),.055,.62,M["black"],"X",96))
    rotating(cyl("ROTOR_motor_hub",(-.23,0,.50),.13,.10,M["aluminum"],"X",96))
    rotating(cyl("ROTOR_pump_hub",(.27,0,.50),.13,.10,M["aluminum"],"X",96))
    # Volute casing with a front cover, center eye and tangential discharge.
    cyl("PUMP_volute",(.66,0,.56),.47,.34,M["steel"],"X",160)
    cyl("PUMP_front_cover",(.86,0,.56),.38,.07,M["galv"],"X",160)
    cyl("PUMP_eye",(.91,0,.56),.15,.04,M["black"],"X",128)
    cyl("PUMP_hub",(.66,0,.56),.11,.40,M["aluminum"],"X")
    for x in (.42,.84):
      box(f"PUMP_foot_{x}",(x,0,.24),(.19,.56,.14),M["steel"],.012)
      for y in (-.21,.21): bolt(f"PUMP_mount_{x}_{y}",(x,y,.32),M["black"])
    # True end-suction topology: the inlet is coaxial with the shaft and
    # impeller eye, while the discharge leaves tangentially upward.
    cyl("PUMP_suction",(1.20,0,.56),.18,.52,M["galv"],"X",128);cyl("PUMP_suction_flange",(1.49,0,.56),.29,.08,M["steel"],"X",128)
    cyl("PUMP_suction_bore",(1.535,0,.56),.13,.018,M["black"],"X",128)
    cyl("PUMP_discharge",(.66,0,1.02),.15,.58,M["galv"],"Z");cyl("PUMP_discharge_flange",(.66,0,1.33),.25,.08,M["steel"])
    box("MOTOR_terminal",(-.70,0,.92),(.42,.38,.22),M["steel"],.022)
    for a in range(0,360,45):
        y=.30*math.cos(math.radians(a));z=.56+.30*math.sin(math.radians(a));bolt(f"PUMP_cover_bolt_{a}",(.91,y,z),M["black"],"X")
        y=.235*math.cos(math.radians(a));z=.56+.235*math.sin(math.radians(a));cyl(f"PUMP_suction_bolt_{a}",(1.535,y,z),.015,.10,M["black"],"X",12)
        x=.66+.195*math.cos(math.radians(a));y=.195*math.sin(math.radians(a));cyl(f"PUMP_discharge_bolt_{a}",(x,y,1.38),.015,.10,M["black"],"Z",12)

def valve(M):
    # Installed ANSI-style flanged quarter-turn ball-valve assembly.  Short
    # pipe spools and shoes provide the physical context and load path expected
    # in a plant model while keeping both connection ends reusable.
    bore=.14;center_z=.72
    for side,direction in (("L",-1),("R",1)):
        valve_face=.30*direction;mate_face=.41*direction
        annular_cylinder(f"VALVE_flange_{side}",(valve_face,0,center_z),.34,bore,.10,M["steel"],"X")
        annular_cylinder(f"MATING_flange_{side}",(mate_face,0,center_z),.34,bore,.10,M["galv"],"X")
        annular_cylinder(f"PIPE_spool_{side}",(.77*direction,0,center_z),.18,bore,.62,M["galv"],"X")
        annular_cylinder(f"GASKET_{side}",(.355*direction,0,center_z),.31,bore,.012,M["black"],"X")
        for index,angle in enumerate(range(0,360,45)):
            y=.275*math.cos(math.radians(angle));z=center_z+.275*math.sin(math.radians(angle))
            cyl(f"FLANGE_{side}_stud_{index}",(.355*direction,y,z),.016,.30,M["galv"],"X",48)
            cyl(f"FLANGE_{side}_nut_in_{index}",(.235*direction,y,z),.031,.030,M["black"],"X",6)
            cyl(f"FLANGE_{side}_nut_out_{index}",(.475*direction,y,z),.031,.030,M["black"],"X",6)
        box(f"PIPE_foot_{side}",(.77*direction,0,.285),(.48,.56,.07),M["steel"],.010)
        box(f"PIPE_shoe_post_{side}",(.77*direction,0,.41),(.16,.26,.20),M["galv"],.010)
        saddle_l=box(f"PIPE_saddle_L_{side}",(.77*direction,-.095,.535),(.34,.22,.055),M["galv"],.012);saddle_l.rotation_euler.x=-math.radians(24)
        saddle_r=box(f"PIPE_saddle_R_{side}",(.77*direction,.095,.535),(.34,.22,.055),M["galv"],.012);saddle_r.rotation_euler.x=math.radians(24)
        for dx in (-.15,.15):
            for y in (-.21,.21):
                x=.77*direction+dx
                cyl(f"PIPE_anchor_washer_{side}_{dx}_{y}",(x,y,.327),.034,.010,M["galv"],"Z",48)
                cyl(f"PIPE_anchor_nut_{side}_{dx}_{y}",(x,y,.350),.027,.035,M["black"],"Z",6)
    # Compact split cast body with a real unobstructed bore.
    annular_cylinder("VALVE_body",(0,0,center_z),.35,bore,.52,M["steel"],"X",160)
    # One continuous liner masks segment boundaries and proves an unobstructed
    # process path through both pipe spools and the valve body.
    annular_cylinder("PROCESS_bore_liner",(0,0,center_z),.145,.128,2.22,M["galv"],"X",160)
    for x in (-.20,.20):
        annular_cylinder(f"BODY_joint_{x}",(x,0,center_z),.30,bore,.05,M["galv"],"X")
        for index,angle in enumerate((45,135,225,315)):
            y=.275*math.cos(math.radians(angle));z=center_z+.275*math.sin(math.radians(angle))
            cyl(f"BODY_bolt_{x}_{index}",(x,y,z),.017,.075,M["black"],"X",48)
    # Bonnet and open standoff expose the square-drive coupling; nothing is
    # hidden inside an ambiguous solid bridge.
    cyl("BONNET_lower",(0,0,1.02),.15,.18,M["steel"],"Z",128)
    box("VALVE_ISO_mount_pad",(0,0,1.13),(.42,.32,.07),M["galv"],.010)
    cyl("DRIVE_coupling",(0,0,1.23),.075,.20,M["yellow"],"Z",64)
    cyl("DRIVE_coupling_lower_collar",(0,0,1.135),.090,.030,M["black"],"Z",64)
    cyl("DRIVE_coupling_upper_collar",(0,0,1.325),.090,.030,M["black"],"Z",64)
    box("STEM_square_drive",(0,0,1.275),(.10,.10,.25),M["galv"],.008)
    for x in (-.18,.18):
        for y in (-.11,.11):cyl(f"ACTUATOR_standoff_{x}_{y}",(x,y,1.255),.026,.25,M["galv"],"Z",48)
    box("ACTUATOR_mount_plate",(0,0,1.39),(.50,.34,.06),M["galv"],.010)
    for x in (-.19,.19):
        for y in (-.12,.12):cyl(f"ACTUATOR_mount_bolt_{x}_{y}",(x,y,1.425),.014,.030,M["black"],"Z",48)
    box("ACTUATOR_body",(0,0,1.61),(.72,.44,.38),M["blue"],.060)
    for x in (-.37,.37):cyl(f"ACTUATOR_endcap_{x}",(x,0,1.61),.215,.07,M["blue"],"X",128)
    box("ACTUATOR_nameplate",(0,-.229,1.61),(.30,.012,.12),M["white"],.004)
    text_label("ACTUATOR_label","90 DEG",(0,-.238,1.61),.050,M["black"],(math.pi/2,0,0))
    # Solenoid valve, two push-in fittings, and paired air tubes terminate at
    # explicit actuator ports.
    box("SOLENOID_mount",(.43,0,1.48),(.12,.32,.08),M["galv"],.008)
    box("SOLENOID_body",(.48,-.14,1.62),(.22,.18,.25),M["black"],.016)
    cyl("SOLENOID_coil",(.48,-.26,1.62),.060,.12,M["black"],"Y",64)
    for i,z in enumerate((1.55,1.69)):
        cyl(f"SOLENOID_fitting_{i}",(.48,-.245,z),.024,.06,M["galv"],"Y",48)
        cyl(f"ACTUATOR_port_{i}",(.22,-.235,z),.024,.055,M["yellow"],"Y",48)
        cable(f"AIR_tube_{i}",[(.48,-.28,z),(.36,-.34,z),(.22,-.28,z)],.009,M["orange"])
    cyl("AIR_supply_fitting",(.625,-.14,1.62),.026,.070,M["yellow"],"X",48)
    cyl("AIR_supply_quick_connect",(.685,-.14,1.62),.034,.055,M["galv"],"X",48)
    box("LIMIT_SWITCH_box",(-.20,0,1.92),(.36,.28,.17),M["black"],.020)
    cyl("LIMIT_SWITCH_cable_gland",(-.405,0,1.92),.030,.065,M["black"],"X",48)
    cyl("LIMIT_SWITCH_M12",(-.455,0,1.92),.034,.040,M["galv"],"X",48)
    box("FIELD_JUNCTION_bracket",(-.77,-.26,.47),(.24,.08,.30),M["galv"],.008)
    box("FIELD_JUNCTION_box",(-.77,-.33,.60),(.28,.18,.30),M["black"],.018)
    cyl("FIELD_JUNCTION_gland",(-.77,-.225,.70),.028,.06,M["black"],"Y",48)
    cable("LIMIT_SWITCH_field_cable",[(-.49,0,1.92),(-.62,-.18,1.60),(-.70,-.21,1.10),(-.77,-.21,.70)],.012,M["black"])
    # Shared rotary hierarchy drives stem and position pointer together.
    kin=bpy.data.objects.new("KIN_valve_stem",None);bpy.context.collection.objects.link(kin);kin.location=(0,0,center_z);kin["rungproof_asset"]=True;kin["rungproof_collision"]=False
    stem=cyl("VALVE_stem",(0,0,1.22),.045,.58,M["galv"],"Z",96);stem.parent=kin;stem.matrix_parent_inverse=kin.matrix_world.inverted()
    indicator=cyl("POSITION_indicator_disc",(0,0,2.025),.11,.035,M["yellow"],"Z",96);indicator.parent=kin;indicator.matrix_parent_inverse=kin.matrix_world.inverted()
    pointer=box("POSITION_pointer",(.068,0,2.05),(.21,.042,.032),M["black"],.006);pointer.parent=kin;pointer.matrix_parent_inverse=kin.matrix_world.inverted()

def level_transmitter(M):
    # Flange-mounted continuous level transmitter with a rigid capacitance
    # probe and a field-serviceable dual-compartment electronics head.
    annular_cylinder("PROCESS_flange",(0,0,.10),.32,.055,.12,M["steel"],"Z",128)
    for index,angle in enumerate(range(0,360,60)):
        x=.245*math.cos(math.radians(angle));y=.245*math.sin(math.radians(angle))
        cyl(f"PROCESS_flange_bolt_{index}",(x,y,.17),.024,.055,M["black"],"Z",48)
        cyl(f"PROCESS_flange_washer_{index}",(x,y,.202),.038,.010,M["galv"],"Z",48)
    cyl("PROCESS_neck",(0,0,.30),.095,.34,M["galv"],"Z",96)
    cyl("PROCESS_seal_housing",(0,0,.22),.145,.14,M["steel"],"Z",96)
    cyl("PROBE_insulator",(0,0,.02),.060,.18,M["white"],"Z",96)
    cyl("PROBE_sensing_rod",(0,0,-1.28),.025,2.52,M["galv"],"Z",128)
    cyl("PROBE_tip_weight",(0,0,-2.57),.045,.12,M["galv"],"Z",96)
    # Round enclosure reads as process instrumentation rather than a generic
    # control box.  The front display and rear terminal cover are serviceable.
    cyl("TRANSMITTER_housing",(0,0,.69),.235,.38,M["black"],"Y",160)
    cyl("DISPLAY_bezel",(0,-.245,.69),.225,.055,M["black"],"Y",128)
    cyl("DISPLAY_glass",(0,-.278,.69),.185,.025,M["lens"],"Y",128)
    # A neutral protected window keeps the form recognizable without showing
    # fabricated live values, loop type, or manufacturer display layout.
    box("DISPLAY_window",(0,-.296,.69),(.19,.014,.085),M["lens"],.004)
    cyl("TERMINAL_cover",(0,.255,.69),.245,.065,M["steel"],"Y",128)
    for angle in range(0,360,60):
        x=.19*math.cos(math.radians(angle));z=.69+.19*math.sin(math.radians(angle))
        cyl(f"TERMINAL_cover_bolt_{angle}",(x,.294,z),.010,.020,M["black"],"Y",32)
    # Field entry, protective-earth lug, and bonded neck have explicit support
    # and termination surfaces; no cable is left dangling in catalog geometry.
    cyl("CABLE_gland_body",(.335,0,.71),.065,.16,M["black"],"X",64)
    cyl("CABLE_gland_nut",(.430,0,.71),.078,.045,M["galv"],"X",6)
    box("GROUND_lug",(-.30,0,.55),(.10,.08,.11),M["galv"],.008)
    cyl("GROUND_screw",(-.36,-.045,.55),.018,.030,M["green"],"Y",48)

def level_switch(M):
    # Compact vibronic point-level switch.  The process fitting, sealed stem,
    # symmetric fork, status indication, and field connector remain readable
    # at simulator distance without resorting to a generic control box.
    cyl("PROCESS_hex_fitting",(0,0,.16),.18,.16,M["galv"],"Z",6)
    cyl("PROCESS_thread_core",(0,0,.02),.115,.18,M["galv"],"Z",96)
    for index,z in enumerate((-.055,-.025,.005,.035,.065)):
        torus(f"PROCESS_thread_ring_{index}",(0,0,z),.116,.007,M["steel"],"Z")
    cyl("PROCESS_seal",(0,0,.085),.135,.025,M["black"],"Z",96)
    cyl("SENSOR_neck",(0,0,.33),.105,.26,M["galv"],"Z",96)
    cyl("SWITCH_head",(0,0,.63),.175,.32,M["black"],"Z",128)
    torus("STATUS_ring",(0,0,.475),.166,.010,M["galv"],"Z")
    cyl("STATUS_led",(.15,-.06,.62),.015,.012,M["green"],"Y",24,False)
    cyl("HEAD_top_cap",(0,0,.835),.207,.055,M["steel"],"Z",128)
    # Fixed M12 field connector; external cable belongs to the scene wiring
    # layer and is not left as a dangling catalog pigtail.
    cyl("M12_connector_base",(.0,0,.905),.070,.11,M["black"],"Z",64)
    cyl("M12_coupling_nut",(.0,0,.972),.080,.040,M["galv"],"Z",12)
    cyl("M12_socket",(.0,0,1.005),.052,.025,M["black"],"Z",64)
    for angle in (45,135,225,315):
        x=.027*math.cos(math.radians(angle));y=.027*math.sin(math.radians(angle))
        cyl(f"M12_pin_{angle}",(x,y,1.023),.004,.012,M["galv"],"Z",24)
    # One-piece welded stem and tuning fork with equal supported tines.
    cyl("FORK_stem",(0,0,-.29),.052,.58,M["galv"],"Z",96)
    box("FORK_yoke",(0,0,-.565),(.20,.12,.12),M["galv"],.030)
    for x in (-.065,.065):
        box(f"FORK_tine_{x}",(x,0,-.82),(.055,.105,.46),M["galv"],.024)
        cyl(f"FORK_tip_{x}",(x,0,-1.055),.052,.040,M["galv"],"Z",64)
    box("GROUND_lug",(.19,0,.53),(.08,.07,.10),M["galv"],.008)
    cyl("GROUND_screw",(.235,-.04,.53),.016,.025,M["green"],"Y",48)

def radar(M):
    # Compact vertical electronics body above a downward antenna: this keeps
    # the generic radar sensor in a credible process-instrument orientation.
    cyl("RADAR_head",(0,0,.60),.175,.30,M["black"],"Z",160)
    cyl("DISPLAY_bezel",(0,-.175,.62),.125,.028,M["black"],"Y",96)
    box("DISPLAY_window",(0,-.193,.62),(.14,.010,.065),M["lens"],.004)
    for x in (-.070,.070):cyl(f"DISPLAY_key_{x}",(x,-.202,.52),.012,.010,M["black"],"Y",24)
    cyl("TERMINAL_cover",(0,0,.775),.115,.055,M["steel"],"Z",96)
    # The process neck must overlap both the electronics head and flange.
    # Leaving a visible floating gap falsely suggests a detached transmitter.
    cyl("PROCESS_neck",(0,0,.23),.105,.50,M["galv"],"Z",96)
    annular_cylinder("PROCESS_flange",(0,0,-.08),.34,.105,.10,M["steel"],"Z",128)
    for index,angle in enumerate(range(0,360,45)):
        x=.265*math.cos(math.radians(angle));y=.265*math.sin(math.radians(angle))
        cyl(f"PROCESS_flange_bolt_{index}",(x,y,-.01),.022,.055,M["black"],"Z",48)
        cyl(f"PROCESS_flange_washer_{index}",(x,y,.023),.036,.010,M["galv"],"Z",48)
    # Tapered horn and dielectric lens establish non-contact downward ranging.
    bpy.ops.mesh.primitive_cone_add(vertices=160,radius1=.225,radius2=.12,depth=.38,location=(0,0,-.32));finish(bpy.context.object,"ANTENNA_horn",M["aluminum"],.002,True)
    cyl("ANTENNA_dielectric_lens",(0,0,-.525),.205,.045,M["lens"],"Z",160)
    beam=translucent_emissive("Radar measurement beam",(.02,.35,.95),.08,1.2)
    bpy.ops.mesh.primitive_cone_add(vertices=160,radius1=.55,radius2=.07,depth=1.35,location=(0,0,-1.22));finish(bpy.context.object,"KIN_radar_beam",beam,.0,True,collision=False)
    cyl("CABLE_gland_body",(.205,0,.68),.045,.11,M["black"],"X",64)
    cyl("CABLE_gland_nut",(.270,0,.68),.055,.035,M["galv"],"X",6)
    box("GROUND_lug",(-.20,0,.49),(.07,.06,.08),M["galv"],.006)
    cyl("GROUND_screw",(-.235,-.035,.49),.012,.020,M["green"],"Y",32)

def rotary_switch(M):
    # A selector belongs at a reachable operating height on a braced, anchored
    # pedestal.  The compact enclosure keeps the station credible next to a
    # machine instead of reading as a tall freestanding electrical cabinet.
    box("BASE_plate",(0,0,.025),(.54,.46,.05),M["galv"],.008)
    for x in (-.20,.20):
      for y in (-.16,.16):
        cyl(f"BASE_anchor_{x}_{y}",(x,y,.062),.025,.025,M["black"],"Z",32)
        cyl(f"BASE_washer_{x}_{y}",(x,y,.052),.043,.008,M["galv"],"Z",48)
    box("PEDESTAL_tube",(0,.04,.47),(.18,.18,.82),M["steel"],.009)
    box("PEDESTAL_flange",(0,.04,.86),(.31,.28,.045),M["galv"],.006)
    # Four small gussets make the enclosure-to-pedestal load path visible.
    for x in (-.11,.11):
      box(f"PEDESTAL_gusset_{x}",(x,.03,.80),(.045,.22,.16),M["steel"],.004)
    box("ENCLOSURE_body",(0,.02,1.16),(.60,.38,.62),M["steel"],.022)
    box("ENCLOSURE_door",(0,-.184,1.16),(.54,.022,.54),M["galv"],.006)
    for x in (-.23,.23):
      cyl(f"DOOR_fastener_{x}",(x,-.203,1.39),.014,.012,M["black"],"Y",24)
      cyl(f"DOOR_fastener_lower_{x}",(x,-.203,.93),.014,.012,M["black"],"Y",24)
    # The datum/pivot is the physical center of the operator.  All visibly
    # rotating parts are children, so the runtime never leaves a static red
    # pointer behind while it turns the handle.
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0,-.224,1.15))
    pivot=bpy.context.object;pivot.name="KIN_selector_handle";pivot["rungproof_asset"]=True;pivot["rungproof_collision"]=False
    # The real operator is a 22 mm panel fitting, not a gauge-sized dial.
    # Keep its scale visibly small relative to the enclosure door.
    cyl("SELECTOR_dial_plate",(0,-.202,1.15),.125,.010,M["white"],"Y",96)
    cyl("SELECTOR_bezel",(0,-.219,1.15),.072,.026,M["black"],"Y",96)
    handle=box("SELECTOR_handle",(0,-.244,1.185),(.040,.052,.125),M["black"],.012)
    # Flush pale inlay is intentionally within the handle silhouette.  It
    # communicates the selected direction without looking like a loose red
    # lever bolted to the side of the control.
    inlay=box("SELECTOR_direction_inlay",(0,-.275,1.215),(.012,.006,.047),M["white"],.002)
    for moving in (handle,inlay):
      moving.parent=pivot
      moving.matrix_parent_inverse=pivot.matrix_world.inverted()
    # Position 0 is the source/default-safe detent.  Set this only after
    # parenting so the child geometry follows the pivot rather than preserving
    # its former world transform.
    pivot.rotation_euler.y=-math.radians(55)
    for i,(a,label) in enumerate(zip((-45,0,45),("0","1","2"))):
      # The tick and index share one radial line, keeping detent meaning
      # unambiguous even when the handle covers the selected tick.
      x=.090*math.sin(math.radians(a));z=1.15+.090*math.cos(math.radians(a))
      tick=box(f"POSITION_tick_{i}",(x,-.216,z),(.008,.004,.024),M["black"],.001)
      tick.rotation_euler.y=-math.radians(a)
      # Both elements are set just ahead of the dial's front face, rather than
      # in free space before the enclosure.  That keeps their association with
      # the four detents physically and visually clear.
      tx=.115*math.sin(math.radians(a));tz=1.15+.115*math.cos(math.radians(a))
      text_label(f"POSITION_label_{i}",label,(tx,-.216,tz),.018,M["black"])
    # A real bottom-entry cable gland and a short terminated conduit witness
    # show where field wiring enters; the external cable remains scene-level.
    cyl("CABLE_gland",(.17,.02,.86),.040,.075,M["black"],"Z",48)
    cyl("GROUND_lug",(-.21,.02,.91),.026,.020,M["green"],"Y",32)

def fan(M):
    box("FAN_base",(0,0,.06),(2.10,1.20,.12),M["steel"],.012)
    for x in (-.82,.82):
      box(f"FAN_post_{x}",(x,0,1.05),(.12,.12,1.95),M["steel"],.008)
      for y in (-.12,.12):cyl(f"FAN_anchor_{x}_{y}",(x,y,.135),.027,.030,M["black"],"Z",32)
    # The grille is deliberately smaller than the post spacing.  This leaves
    # a visible bracket-to-guard load path instead of making the frame read as
    # a single, oversized wire ring.
    torus("FAN_guard_outer",(0,-.30,1.45),.76,.040,M["galv"],"Y");torus("FAN_shroud",(0,0,1.45),.72,.09,M["steel"],"Y")
    # Brackets deliberately bridge the housing and structural posts; this is
    # the visible load path for the rotor/shroud, not a ring floating in frame.
    for x in (-.82,.82):box(f"FAN_shroud_bracket_{x}",(x,0,1.45),(.12,.24,.32),M["steel"],.008)
    # Four short, bolted standoffs positively retain the non-rotating safety
    # guard to the fixed shroud.  They intentionally sit inside the ring so
    # the guard's load path and blade clearance are readable in a front view.
    for i,(x,z) in enumerate(((-.60,1.06),(.60,1.06),(-.60,1.84),(.60,1.84))):
      cyl(f"FAN_guard_standoff_{i}",(x,-.15,z),.026,.30,M["galv"],"Y",32)
      cyl(f"FAN_guard_fastener_{i}",(x,-.348,z),.045,.016,M["galv"],"Y",32)
    # A single physical hub hierarchy drives blades and shaft.  The motor,
    # shroud, grille, and structural frame intentionally remain stationary.
    bpy.ops.object.empty_add(type="PLAIN_AXES",location=(0,0,1.45))
    pivot=bpy.context.object;pivot.name="KIN_fan_hub";pivot["rungproof_asset"]=True;pivot["rungproof_collision"]=False
    hub=cyl("FAN_hub",(0,0,1.45),.18,.34,M["blue"],"Y")
    hub.parent=pivot;hub.matrix_parent_inverse=pivot.matrix_world.inverted()
    for i in range(6):
      a=i*math.tau/6;b=box(f"FAN_blade_{i}",(.43*math.cos(a),0,1.45+.43*math.sin(a)),(.58,.06,.20),M["aluminum"],.04);b.rotation_euler.y=-a;b.parent=pivot;b.matrix_parent_inverse=pivot.matrix_world.inverted()
    # Safety-grille rods are round, not rectangular bars.  Their endpoints
    # intentionally overlap the inner/outer rings so no rod can visually
    # protrude beyond the circular guard perimeter.
    for a in range(0,360,22):
      angle=math.radians(a);r0=.20;r1=.75;mid=(r0+r1)/2
      bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.009,depth=r1-r0,location=(mid*math.cos(angle),-.30,1.45+mid*math.sin(angle)))
      rod=bpy.context.object
      rod.rotation_mode="QUATERNION"
      rod.rotation_quaternion=Vector((0,0,1)).rotation_difference(Vector((math.cos(angle),0,math.sin(angle))))
      finish(rod,f"GUARD_rod_{a}",M["galv"],.001,True)
    # Rear drive assembly and front concentric safety grille establish this as
    # a powered axial fan rather than a flywheel.
    cyl("FAN_motor",(0,.48,1.45),.27,.68,M["blue"],"Y",128)
    box("FAN_motor_foot",(0,.48,1.12),(.52,.48,.12),M["steel"],.018)
    # Two forward arms and one crossrail transfer the motor-foot load back to
    # the fixed posts; the motor is not merely shown resting in mid-air.
    for x in (-.82,.82):box(f"FAN_motor_arm_{x}",(x,.24,1.12),(.11,.58,.11),M["steel"],.008)
    box("FAN_motor_crossrail",(0,.48,1.12),(1.76,.11,.11),M["steel"],.008)
    shaft=cyl("FAN_shaft",(0,.10,1.45),.055,.55,M["galv"],"Y");shaft.parent=pivot;shaft.matrix_parent_inverse=pivot.matrix_world.inverted()
    for r in (.20,.37,.54,.70):torus(f"GUARD_ring_{r}",(0,-.30,1.45),r,.010,M["galv"],"Y")

def lift_table(M):
    box("LIFT_base",(0,0,.08),(2.8,1.8,.16),M["steel"],.014);box("KIN_platform",(0,0,1.55),(3.0,1.9,.18),M["yellow"],.012)
    arm_length=2.0
    half_run=.95
    half_rise=.30
    arm_angle=math.atan2(half_rise,half_run)
    for y in (-.65,.65):
      for stage,center_z in ((0,.52),(1,1.12)):
        for sign in (-1,1):
          beam=box(f"KIN_scissor_{stage}_{y}_{sign}",(0,y,center_z),(arm_length,.09,.11),M["galv"],.006);beam.rotation_euler.y=sign*arm_angle
        cyl(f"KIN_stage_pivot_{stage}_{y}",(0,y,center_z),.09,.18,M["black"],"Y")
    # Build every hydraulic member between named pin centres. The prior
    # horizontal cylinder crossed the mechanism with no credible reaction or
    # load point; this inclined cylinder drives the front lower scissor plane.
    def lift_member(name,start,end,radius,material):
      start_v,end_v=Vector(start),Vector(end);delta=end_v-start_v
      bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=radius,depth=delta.length,location=(start_v+end_v)/2)
      link=bpy.context.object;link.rotation_mode="QUATERNION";link.rotation_quaternion=Vector((0,0,1)).rotation_difference(delta.normalized())
      return finish(link,name,material,.001,True)
    cylinder_tail=Vector((-.90,-.65,.24));cylinder_gland=Vector((-.12,-.65,.49));rod_tip=Vector((.40,-.65,.66))
    lift_member("LIFT_cylinder",cylinder_tail,cylinder_gland,.105,M["aluminum"])
    lift_member("KIN_lift_rod",cylinder_gland,rod_tip,.045,M["galv"])
    for n,p in (("LIFT_cylinder_base_clevis",cylinder_tail),("LIFT_rod_end_clevis",rod_tip)):
      box(n,p,(.16,.18,.14),M["steel"],.012)
      cyl(n+"_pin",p,.065,.22,M["black"],"Y",48)
    box("LIFT_drive_lug",rod_tip,(.16,.16,.15),M["steel"],.010)
    for x in (-half_run,half_run):
      for y in (-.65,.65):
        cyl(f"LIFT_top_pivot_{x}_{y}",(x,y,1.42),.075,.16,M["black"],"Y")
        cyl(f"LIFT_bottom_pivot_{x}_{y}",(x,y,.22),.075,.16,M["black"],"Y")
        cyl(f"KIN_mid_pivot_{x}_{y}",(x,y,.82),.075,.16,M["black"],"Y")
    for y in (-.65,.65):
      # These rails are the physical mounting planes at the sliding and fixed
      # arm ends, making the otherwise thin scissor endpoint contacts visible.
      box(f"LIFT_base_mount_rail_{y}",(0,y,.22),(2.20,.22,.12),M["steel"],.008)
      # This name intentionally shares the controller's LIFT_upper_track_
      # prefix, so the platform-side rail follows lift-height motion.
      box(f"LIFT_upper_track_mount_rail_{y}",(0,y,1.42),(2.20,.22,.12),M["steel"],.008)
      box(f"LIFT_lower_track_{y}",(0,y,.20),(2.45,.16,.08),M["galv"],.005)
      box(f"LIFT_upper_track_{y}",(0,y,1.40),(2.45,.16,.08),M["galv"],.005)
      # Roller pairs positively constrain each sliding track/end pivot.
      for x in (-half_run,half_run):
        cyl(f"LIFT_lower_guide_roller_{x}_{y}",(x,y,.25),.060,.18,M["black"],"Y",48)
        cyl(f"LIFT_upper_guide_roller_{x}_{y}",(x,y,1.36),.060,.18,M["black"],"Y",48)
    # Front-side retaining washers distinguish pivot pins from unretained
    # protruding dowels in recognition and inspection views.
    for x,z in ((-half_run,.22),(-half_run,.82),(-half_run,1.42),(0,.52),(0,1.12),(half_run,.22),(half_run,.82),(half_run,1.42)):
      cyl(f"LIFT_retaining_washer_{x}_{z}",(x,-.755,z),.095,.014,M["aluminum"],"Y",48)
    box("HYDRAULIC_power_unit",(-1.02,-.48,.34),(.48,.42,.42),M["blue"],.05)
    cyl("HYDRAULIC_powerpack_motor",(-1.02,-.48,.61),.13,.32,M["steel"],"X",64)
    cyl("HYDRAULIC_pump",(-.81,-.48,.48),.09,.28,M["aluminum"],"X",48)
    cable("HYDRAULIC_hose",[(-.82,-.48,.38),(-.40,-.52,.32),(0,-.40,.45)],.025,M["black"])
    # The referenced stationary-table family exposes its crossed arms and
    # rollers.  Do not hide the entire mechanism behind a generic accordion
    # sleeve: that makes the asset unrecognizable and is not a guarding claim.
    # Any site-specific pinch-point protection belongs in the scene safety
    # design, not as an implied universal feature of this generic lift asset.

def drill_press(M):
    guard_lens=translucent_emissive("Drill guard polycarbonate",(.02,.58,.82),.08,0.8)
    # The full-height column, its cast base collar, and the bolted base make
    # the load path explicit.  The head must not read as a box resting on two
    # unrelated posts in a context-free review.
    box("DRILL_base",(0,0,.10),(1.8,1.5,.20),M["steel"],.018)
    cyl("DRILL_column",(-.50,.25,1.90),.19,3.80,M["galv"],"Z",128)
    cyl("DRILL_column_base",(-.50,.25,.30),.32,.22,M["blue"],"Z",128)
    for x in (-.72,-.28):
      for y in (.03,.47): bolt(f"DRILL_base_anchor_{x}_{y}",(x,y,.215),M["black"])
    box("DRILL_table_support",(-.50,.25,1.12),(.20,.26,.30),M["blue"],.025)
    box("DRILL_table",(0,-.05,1.24),(1.42,1.12,.14),M["steel"],.012)
    for x in (-.40,0,.40):box(f"TABLE_Tslot_{x}",(x,-.05,1.315),(.045,1.08,.018),M["black"],.002)
    cyl("DRILL_table_clamp",(-.50,.00,1.38),.075,.20,M["black"],"Y",64)
    cable("DRILL_table_crank",[(-.50,.02,1.03),(-.50,-.20,.87)],.018,M["galv"]);cyl("DRILL_table_crank_knob",(-.50,-.20,.87),.040,.12,M["black"],"Y",48)
    box("DRILL_head",(-.05,.05,3.22),(1.52,.96,.72),M["blue"],.08)
    # A guarded belt drive makes the motor-to-spindle path visible without
    # exposing a simulated rotating belt.  The motor sits behind the head.
    # Keep the drive above and to the operator-visible side of the head; the
    # external motor, belt cover, and spindle bearing path must read in the
    # acceptance hero rather than only in a rear inspection view.
    cyl("MOTOR_body",(-.53,-.16,3.88),.25,.66,M["blue"],"X",128)
    cyl("MOTOR_fan_cover",(-.89,-.16,3.88),.27,.06,M["black"],"X",96)
    box("DRILL_belt_guard",(.08,-.18,3.71),(1.12,.54,.44),M["blue"],.055)
    for x in (-.30,.43): cyl(f"DRILL_belt_guard_fastener_{x}",(x,-.455,3.71),.022,.025,M["black"],"Y",32)
    text_label("DRILL_nameplate","DRILL PRESS",(-.05,-.442,3.49),.105,M["white"])
    # The exported kinematic node is a non-rendering pivot.  Its spindle,
    # chuck, and cutting tool are children, so run motion cannot leave a
    # stationary bit behind a rotating shell.
    bpy.ops.object.empty_add(type="PLAIN_AXES",location=(.38,-.05,2.55));spindle_pivot=bpy.context.object;spindle_pivot.name="KIN_spindle";spindle_pivot["rungproof_asset"]=True;spindle_pivot["rungproof_collision"]=False
    spindle=cyl("DRILL_quill",(.38,-.05,2.55),.115,.72,M["galv"],"Z",96)
    cyl("DRILL_spindle_bearing_housing",(.38,-.05,2.88),.205,.34,M["blue"],"Z",96)
    box("DRILL_quill_head_bridge",(.20,-.05,2.98),(.48,.50,.25),M["blue"],.040)
    chuck=cyl("DRILL_chuck",(.38,-.05,2.20),.135,.24,M["black"],"Z",96)
    for a in range(0,360,120):
      angle=math.radians(a);box(f"DRILL_chuck_jaw_{a}",(.38+.072*math.cos(angle),-.05+.072*math.sin(angle),2.075),(.040,.040,.13),M["galv"],.006)
    bpy.ops.mesh.primitive_cone_add(vertices=96,radius1=.018,radius2=.060,depth=.46,location=(.38,-.05,1.84));bit=finish(bpy.context.object,"DRILL_bit",M["black"],.001,True)
    for part in (spindle,chuck,bit,*[bpy.data.objects[f"DRILL_chuck_jaw_{a}"] for a in (0,120,240)]):
      part.parent=spindle_pivot;part.matrix_parent_inverse=spindle_pivot.matrix_world.inverted()
    # Fixed transparent polycarbonate shield: it stays out of KIN_spindle and
    # visibly communicates the candidate catalog's guard_closed input.
    for name,loc,dims in (("DRILL_guard_front",(.38,-.275,2.18),(.58,.025,.70)),("DRILL_guard_left",(.08,-.05,2.18),(.025,.43,.70)),("DRILL_guard_right",(.68,-.05,2.18),(.025,.43,.70))):
      box(name,loc,dims,guard_lens,.012)
    # Two pivot lugs and a cross-shaft make the guard a retained hinged shield
    # instead of an unexplained glass box.
    cyl("DRILL_guard_hinge_shaft",(.38,.17,2.66),.030,.68,M["galv"],"X",64)
    for x in (.08,.68):
      box(f"DRILL_guard_hinge_lug_{x}",(x,.17,2.73),(.07,.09,.20),M["blue"],.012)
      cyl(f"DRILL_guard_hinge_pin_{x}",(x,.17,2.66),.042,.11,M["black"],"Y",48)
      cyl(f"DRILL_guard_frame_upright_{x}",(x,.17,2.88),.026,.48,M["galv"],"Z",48)
    cyl("DRILL_guard_frame_crossbar",(.38,.17,3.12),.026,.68,M["galv"],"X",48)
    box("DRILL_guard_frame_mount",(-.04,.17,3.12),(.16,.10,.14),M["blue"],.015)
    cyl("FEED_hub",(.70,.02,2.78),.10,.16,M["black"],"X")
    for a in (0,120,240):
        z=2.78+.30*math.sin(math.radians(a));y=.02+.30*math.cos(math.radians(a))
        cable(f"FEED_handle_{a}",[(.79,.02,2.78),(.79,y,z)],.018,M["galv"])
        cyl(f"FEED_knob_{a}",(.79,y,z),.035,.10,M["black"],"X",48)
    # A real vise rather than a colored workpiece block: fixed and moving jaw,
    # screw, and a clamped test coupon centred below the drill axis.
    box("DRILL_vise_base",(.38,-.05,1.41),(.72,.52,.12),M["blue"],.022)
    # The jaws clamp from left/right, which is clear from the hero camera:
    # yellow stock remains visibly captured between opposed serrated faces.
    box("DRILL_vise_fixed_jaw",(.08,-.05,1.56),(.13,.56,.26),M["blue"],.014)
    box("DRILL_vise_moving_jaw",(.68,-.05,1.56),(.13,.56,.26),M["blue"],.014)
    box("DRILL_workpiece",(.38,-.05,1.64),(.42,.28,.11),M["yellow"],.006)
    for x in (.155,.605): box(f"DRILL_vise_jaw_face_{x}",(x,-.05,1.67),(.018,.36,.15),M["galv"],.003)
    cyl("DRILL_vise_clamp_screw",(.72,-.05,1.55),.040,.44,M["galv"],"X",48)
    cable("DRILL_vise_screw",[(.38,-.49,1.48),(.38,-.26,1.48)],.032,M["galv"]);cyl("DRILL_vise_handle",(.38,-.52,1.48),.045,.28,M["black"],"X",48)
    box("DRILL_control",(-.68,-.43,3.22),(.30,.12,.46),M["steel"],.025)
    cyl("DRILL_start",(-.75,-.505,3.34),.045,.035,M["green"],"Y",48);cyl("DRILL_stop",(-.63,-.505,3.19),.055,.035,M["red"],"Y",48)
    cyl("DRILL_estop",(-.63,-.505,3.04),.070,.045,M["red"],"Y",48)
    torus("DRILL_estop_yellow_collar",(-.63,-.510,3.04),.084,.018,M["yellow"],"Y")
    # Gussets connect the table support to the continuous main column.
    for y in (.03,.47):
      brace=box(f"DRILL_table_gusset_{y}",(-.30,y,1.07),(.42,.05,.28),M["blue"],.008);brace.rotation_euler.y=-math.radians(32)

def robot(M):
    # A six-axis robot needs a real nested joint hierarchy.  Independent
    # `KIN_*` mesh names made the former model look like a robot but caused
    # downstream links to remain behind when a parent joint moved.
    def pivot(name,loc,parent=None):
      bpy.ops.object.empty_add(type="PLAIN_AXES",location=loc);node=bpy.context.object;node.name=name;node["rungproof_asset"]=True;node["rungproof_collision"]=False
      if parent: node.parent=parent;node.matrix_parent_inverse=parent.matrix_world.inverted()
      return node
    def attach(parent,*objects):
      for obj in objects:
        obj.parent=parent;obj.matrix_parent_inverse=parent.matrix_world.inverted()
    def link(name,start,end,width=.42,depth=.48):
      """A rounded load-bearing link centred exactly between named joints."""
      start,end=Vector(start),Vector(end);member=box(name,(start+end)/2,(width,depth,(end-start).length),M["orange"],.10)
      member.rotation_euler=(end-start).to_track_quat("Z","Y").to_euler();return member
    # Anchored pedestal remains fixed; washers, cap screws, and the base flange
    # must be visible at normal review distance instead of pin-prick dots.
    base=cyl("ROBOT_base",(0,0,.15),.72,.30,M["black"]);cyl("ROBOT_base_flange",(0,0,.31),.62,.07,M["galv"])
    for x in (-.43,.43):
      for y in (-.43,.43):
        cyl(f"ROBOT_base_washer_{x}_{y}",(x,y,.355),.055,.012,M["galv"],"Z",48);cyl(f"ROBOT_base_anchor_{x}_{y}",(x,y,.380),.032,.060,M["black"],"Z",6)
    axis1=pivot("KIN_axis_1",(0,0,.54));turntable=cyl("ROBOT_axis1_turntable",(0,0,.54),.52,.43,M["orange"]);attach(axis1,turntable)
    axis2=pivot("KIN_axis_2",(0,0,1.20),axis1)
    shoulder=cyl("ROBOT_axis2_shoulder",(0,0,1.20),.34,.70,M["black"],"Y");shoulder_cover=cyl("ROBOT_axis2_cover",(0,-.38,1.20),.24,.08,M["orange"],"Y");attach(axis2,shoulder,shoulder_cover)
    axis3=pivot("KIN_axis_3",(.52,0,2.42),axis2)
    upper=link("ROBOT_upper_arm",(0,0,1.20),(.52,0,2.42));attach(axis2,upper)
    elbow=cyl("ROBOT_axis3_elbow",(.52,0,2.42),.30,.68,M["black"],"Y");elbow_cover=cyl("ROBOT_axis3_cover",(.52,-.37,2.42),.22,.08,M["orange"],"Y");attach(axis3,elbow,elbow_cover)
    axis4=pivot("KIN_axis_4",(1.55,0,3.05),axis3)
    fore=link("ROBOT_forearm",(.52,0,2.42),(1.55,0,3.05),.44,.44);attach(axis3,fore)
    wrist_roll=cyl("ROBOT_axis4_wrist_roll",(1.55,0,3.05),.20,.42,M["black"],"X");attach(axis4,wrist_roll)
    axis5=pivot("KIN_axis_5",(1.78,0,3.05),axis4)
    wrist_pitch=cyl("ROBOT_axis5_wrist_pitch",(1.78,0,3.05),.17,.32,M["orange"],"X");attach(axis5,wrist_pitch)
    axis6=pivot("KIN_axis_6",(2.00,0,3.05),axis5)
    # A separate black J6 collar, bolt-circle flange and dark tool adaptor
    # make the final roll axis mechanically legible against the gripper body.
    axis6_collar=cyl("ROBOT_axis6_roll_collar",(2.00,0,3.05),.19,.12,M["black"],"X")
    flange=cyl("ROBOT_axis6_tool_flange",(2.09,0,3.05),.22,.075,M["galv"],"X")
    # Square adapter plate leaves the bolt pattern exposed around its smaller
    # central spigot in ordinary three-quarter views.
    adapter_plate=box("ROBOT_tool_adapter_plate",(2.17,0,3.05),(.075,.44,.44),M["galv"],.018)
    adapter=cyl("ROBOT_tool_adapter",(2.25,0,3.05),.125,.10,M["black"],"X");attach(axis6,axis6_collar,flange,adapter_plate,adapter)
    for y in (-.155,.155):
      for z in (2.895,3.205):
        fastener=cyl(f"ROBOT_axis6_bolt_{y}_{z}",(2.215,y,z),.025,.024,M["black"],"X",6);attach(axis6,fastener)
    # Parallel gripper has a tool-flange adaptor, pneumatic body, guide rails,
    # visible finger bolts, and opposed jaws rather than two floating bars.
    actuator=cyl("ROBOT_gripper_actuator",(2.39,0,3.05),.20,.20,M["black"],"X");palm=box("ROBOT_gripper_palm",(2.50,0,3.05),(.18,.56,.34),M["black"],.04);attach(axis6,actuator,palm)
    for y in (-.22,.22):
      rail=cable(f"ROBOT_gripper_guide_{y}",[(2.51,y,3.05),(2.78,y,3.05)],.018,M["galv"]);finger=box(f"ROBOT_gripper_finger_{y}",(2.78,y,3.05),(.42,.09,.16),M["galv"],.022);attach(axis6,rail,finger)
      for z in (2.98,3.12): bolt(f"ROBOT_gripper_finger_bolt_{y}_{z}",(2.63,y,z),M["black"],"Y")
    # The dress pack stays outside the main swept silhouette and ends at named
    # strain-relief housings at the base and wrist.  It is reviewable routing,
    # not a claim of dynamic cable simulation through every robot pose.
    cable("ROBOT_dresspack",[(-.25,.30,.75),(-.28,.34,1.50),(.05,.34,2.15),(.68,.34,2.67),(1.52,.30,3.22)],.035,M["black"])
    for index,loc in enumerate(((-.25,.30,.75),(-.28,.34,1.50),(.05,.34,2.15),(.68,.34,2.67),(1.52,.30,3.22))):
      clip=cyl(f"ROBOT_dresspack_clip_{index}",loc,.060,.075,M["black"],"Y",48);clip["rungproof_collision"]=False
    box("ROBOT_base_junction",(-.25,.30,.72),(.16,.12,.20),M["black"],.015);box("ROBOT_wrist_junction",(1.53,.30,3.22),(.14,.10,.14),M["black"],.012)

def rotary_table(M):
    # Fixed, anchored drive housing below a nested rotating platen.  A catalog
    # rotary axis must move its fixtures and index features together, not only
    # spin a visually separate disc.
    box("TABLE_base",(0,0,.12),(2.35,2.05,.24),M["steel"],.025)
    for x in (-.92,.92):
      for y in (-.77,.77):
        cyl(f"TABLE_base_washer_{x}_{y}",(x,y,.255),.055,.012,M["galv"],"Z",48);cyl(f"TABLE_base_anchor_{x}_{y}",(x,y,.285),.034,.060,M["black"],"Z",6)
    cyl("TABLE_housing",(0,0,.46),1.05,.52,M["blue"],"Z",160)
    cyl("TABLE_bearing_ring",(0,0,.74),1.14,.11,M["black"],"Z",160)
    bpy.ops.object.empty_add(type="PLAIN_AXES",location=(0,0,.85));pivot=bpy.context.object;pivot.name="KIN_table";pivot["rungproof_asset"]=True;pivot["rungproof_collision"]=False
    platter=cyl("TABLE_rotating_platter",(0,0,.85),1.28,.16,M["galv"],"Z",160);platter.parent=pivot;platter.matrix_parent_inverse=pivot.matrix_world.inverted()
    # Radial T-slots and an indexed detent ring make the rotating work surface
    # readable as a fixture table rather than an anonymous turntable disc.
    for index,a in enumerate(range(0,360,45)):
      angle=math.radians(a);x=.66*math.cos(angle);y=.66*math.sin(angle)
      slot=box(f"TABLE_radial_slot_{index}",(x,y,.946),(.58,.070,.030),M["black"],.004);slot.rotation_euler.z=angle;slot.parent=pivot;slot.matrix_parent_inverse=pivot.matrix_world.inverted()
      pin=cyl(f"TABLE_index_marker_{index}",(1.08*math.cos(angle),1.08*math.sin(angle),.962),.034,.025,M["yellow"],"Z",48);pin.parent=pivot;pin.matrix_parent_inverse=pivot.matrix_world.inverted()
    center=cyl("TABLE_center_register",(0,0,.96),.18,.08,M["yellow"],"Z",96);center.parent=pivot;center.matrix_parent_inverse=pivot.matrix_world.inverted()
    # Modular two-jaw fixture is part of the moving platen, while the geared
    # drive, motor, encoder, and cable entry remain safely fixed to the base.
    fixture=box("TABLE_fixture_plate",(.0,0,1.02),(1.05,.72,.10),M["blue"],.020);fixture.parent=pivot;fixture.matrix_parent_inverse=pivot.matrix_world.inverted()
    for x in (-.34,.34):
      jaw=box(f"TABLE_fixture_jaw_{x}",(x,0,1.14),(.14,.52,.22),M["blue"],.014);face=box(f"TABLE_fixture_jaw_face_{x}",(x*0.90,0,1.20),(.018,.38,.12),M["galv"],.003)
      screw=cyl(f"TABLE_fixture_screw_{x}",(x*1.18,0,1.11),.035,.24,M["black"],"X",48)
      for obj in (jaw,face,screw):obj.parent=pivot;obj.matrix_parent_inverse=pivot.matrix_world.inverted()
    # Four strap-clamp/T-nut sets tie the fixture plate to the moving slots;
    # they are deliberately high contrast so they cannot read as a floating jig.
    for x in (-.43,.43):
      for y in (-.27,.27):
        tnut=cyl(f"TABLE_fixture_tnut_{x}_{y}",(x,y,.985),.040,.035,M["black"],"Z",48)
        strap=box(f"TABLE_fixture_strap_{x}_{y}",(x,y,1.095),(.20,.075,.040),M["galv"],.005)
        nut=cyl(f"TABLE_fixture_nut_{x}_{y}",(x,y,1.135),.043,.045,M["black"],"Z",6)
        for obj in (tnut,strap,nut):obj.parent=pivot;obj.matrix_parent_inverse=pivot.matrix_world.inverted()
    # A single gold flag rotates with the platen past a fixed inductive sensor;
    # it visibly represents the indexed/home reference without pretending to
    # model a full position-control system.
    flag=box("TABLE_index_home_flag",(1.10,0,.995),(.11,.12,.08),M["yellow"],.006);flag.parent=pivot;flag.matrix_parent_inverse=pivot.matrix_world.inverted()
    box("TABLE_index_sensor",(1.33,0,.93),(.20,.24,.20),M["black"],.020);cyl("TABLE_index_sensor_face",(1.215,0,.93),.060,.025,M["yellow"],"X",48)
    box("TABLE_drive_gearbox",(0,-1.18,.42),(.62,.58,.48),M["aluminum"],.055)
    cyl("TABLE_drive_motor",(0,-1.70,.42),.23,.62,M["blue"],"Y",128);cyl("TABLE_motor_fan",(0,-2.03,.42),.245,.06,M["black"],"Y",96)
    cyl("TABLE_motor_coupling",(0,-1.42,.42),.105,.20,M["black"],"Y",64)
    cyl("TABLE_gearbox_output_shaft",(0,-.83,.42),.095,.34,M["galv"],"Y",64)
    box("TABLE_drive_guard",(0,-1.13,.56),(.42,.78,.16),M["blue"],.035)
    box("TABLE_encoder",(1.08,0,.49),(.22,.28,.34),M["black"],.025);cyl("TABLE_encoder_cap",(1.23,0,.49),.10,.06,M["galv"],"X",64)
    cyl("TABLE_encoder_gland",(1.08,-.17,.49),.035,.080,M["black"],"Y",48)
    cable("TABLE_encoder_cable",[(1.08,-.22,.49),(1.34,-.40,.49),(1.34,-.66,.34)],.018,M["black"])
    text_label("TABLE_nameplate","INDEX TABLE",(0,-1.477,.50),.080,M["white"],(math.pi/2,0,0))

def shutter(M):
    # Fixed guide jambs, mounting shoes, and a header shroud frame the moving
    # slat curtain.  Each slat retains the existing KIN name used by the Godot
    # shutter controller; the guide and drive hardware must remain fixed.
    for x in (-2.25,2.25):
      box(f"SHUTTER_guide_{x}",(x,0,1.85),(.24,.34,3.70),M["steel"],.012)
      # Deep opposing lips visibly retain each curtain edge in a true channel.
      box(f"SHUTTER_guide_inner_lip_{x}",(x*.93,-.205,1.85),(.085,.12,3.48),M["black"],.006)
      box(f"SHUTTER_guide_outer_lip_{x}",(x,-.19,1.85),(.08,.08,3.45),M["black"],.006)
      for z in (.25,1.85,3.45): bolt(f"SHUTTER_guide_anchor_{x}_{z}",(x,-.19,z),M["galv"],"Y")
    box("SHUTTER_header",(0,0,3.78),(4.75,.55,.55),M["steel"],.025)
    box("SHUTTER_header_access_cover",(0,-.31,3.80),(3.72,.06,.34),M["blue"],.020)
    cyl("KIN_shutter_roll",(0,0,3.76),.22,4.20,M["galv"],"X",128)
    for x in (-2.10,2.10):
      cyl(f"SHUTTER_roll_endplate_{x}",(x,0,3.76),.34,.10,M["black"],"X",96)
      cyl(f"SHUTTER_roll_bearing_{x}",(x*1.015,0,3.76),.15,.12,M["galv"],"X",64)
      box(f"SHUTTER_roll_support_{x}",(x,0,3.64),(.16,.44,.60),M["steel"],.020)
    for i in range(18):
      slat=box(f"KIN_slat_{i}",(0,-.13,3.42-i*.18),(4.20,.10,.155),M["galv"],.012)
      # Recessed alternate slat seam yields a continuous interlocking curtain.
      if i%2==0:
        seam=box(f"SHUTTER_slat_seam_{i}",(0,-.188,3.42-i*.18),(4.10,.018,.028),M["black"],.002)
        seam.parent=slat;seam.matrix_parent_inverse=slat.matrix_world.inverted()
    bottom=box("KIN_bottom_bar",(0,-.13,.28),(4.28,.16,.22),M["yellow"],.014)
    safety_edge=box("SHUTTER_safety_bottom_edge",(0,-.225,.25),(4.05,.045,.11),M["black"],.012);safety_edge.parent=bottom;safety_edge.matrix_parent_inverse=bottom.matrix_world.inverted()
    for x in (-1.92,1.92):
      endcap=cyl(f"SHUTTER_bottom_endcap_{x}",(x,-.13,.28),.075,.18,M["black"],"Y",48);endcap.parent=bottom;endcap.matrix_parent_inverse=bottom.matrix_world.inverted()
      edge=box(f"SHUTTER_safety_edge_{x}",(x,-.225,.25),(.16,.045,.10),M["black"],.010)
      edge.parent=bottom;edge.matrix_parent_inverse=bottom.matrix_world.inverted()
    # Geared motor, coupling, and guarded drive sit beside the roll end plate.
    cyl("SHUTTER_drive_motor",(2.76,.0,3.76),.24,.62,M["blue"],"X",128)
    cyl("SHUTTER_motor_fan",(3.08,0,3.76),.255,.06,M["black"],"X",96)
    cyl("SHUTTER_drive_coupling",(2.34,0,3.76),.12,.30,M["black"],"X",64)
    box("SHUTTER_drive_guard",(2.43,-.16,3.76),(.42,.12,.50),M["blue"],.040)
    box("SHUTTER_motor_mount",(2.63,0,3.53),(.56,.48,.10),M["steel"],.012)
    box("SHUTTER_motor_bracket",(2.38,0,3.55),(.12,.44,.48),M["steel"],.012)
    for y in (-.16,.16): bolt(f"SHUTTER_motor_mount_bolt_{y}",(2.62,y,3.60),M["black"],"Z")
    box("SHUTTER_control_box",(2.63,-.35,3.24),(.34,.16,.46),M["steel"],.025)
    cyl("SHUTTER_control_open",(2.55,-.445,3.34),.042,.030,M["green"],"Y",48);cyl("SHUTTER_control_stop",(2.70,-.445,3.16),.055,.030,M["red"],"Y",48)
    cyl("SHUTTER_control_gland",(2.63,-.445,3.45),.025,.060,M["black"],"Y",48)
    cable("SHUTTER_motor_cable",[(2.76,-.22,3.58),(2.76,-.34,3.47),(2.63,-.34,3.45)],.018,M["black"])

def machine(M):
    # Open-front framed enclosure: side/back panels and sliding doors retain a
    # machining envelope without presenting a solid cabinet to every camera.
    box("MACHINE_base",(0,0,.09),(2.8,2.20,.18),M["steel"],.018)
    box("MACHINE_back_panel",(0,.88,1.68),(2.45,.12,2.98),M["blue"],.05)
    for x in (-1.18,1.18): box(f"MACHINE_side_panel_{x}",(x,.10,1.68),(.16,1.65,2.98),M["blue"],.04)
    box("MACHINE_roof",(0,.10,3.10),(2.50,1.68,.16),M["blue"],.04)
    box("MACHINE_front_header",(0,-.73,2.88),(2.48,.16,.38),M["blue"],.035)
    box("WORK_envelope",(0,-.52,1.76),(1.92,.08,1.72),M["black"],.015)
    # Sliding safety doors need visibly transparent glazing: a solid cyan panel
    # reads as an unfinished cabinet, not as a guarded machining envelope.
    # Low-opacity polycarbonate keeps the stopped enclosure visibly closed
    # while allowing the modeled spindle, vise and stock to be inspected.
    door_glass=translucent("CNC safety glazing",(.04,.32,.46),.20)
    # STOPPED/DISCONNECTED default is a closed, interlocked enclosure.  Each
    # sliding door is a real frame and safety-glazed panel, not a blue slab.
    for side,x in (("left",-.43),("right",.43)):
        box(f"DOOR_glazing_{side}",(x,-.84,1.96),(.58,.025,1.32),door_glass,.010)
        for dx in (-.31,.31):box(f"DOOR_{side}_stile_{dx}",(x+dx,-.875,1.96),(.045,.045,1.42),M["blue"],.010)
        for z in (1.28,2.64):box(f"DOOR_{side}_rail_{z}",(x,-.875,z),(.67,.045,.055),M["blue"],.010)
        # Exterior pull and black interlock tongue are visible safety cues.
        box(f"DOOR_{side}_handle",(x+(-.20 if side=="left" else .20),-.91,1.70),(.07,.04,.36),M["black"],.010)
    for x in (-1.10,1.10):box(f"MACHINE_door_guide_{x}",(x,-.78,1.70),(.06,.12,2.25),M["galv"],.006)
    box("DOOR_top_track",(0,-.76,2.72),(2.02,.12,.10),M["black"],.012)
    box("DOOR_center_interlock",(0,-.91,1.55),(.075,.05,.20),M["black"],.010)
    box("WORK_table",(0,-.60,1.24),(1.55,.66,.14),M["galv"],.010)
    # Make the vertical machining head unmistakable: fixed Z carriage, motor,
    # quill, taper holder and cutter are separate industrial components.
    # Linear Z rails and a rectangular head establish a VMC architecture.
    for x in (-.36,.36):
        box(f"AXIS_Z_rail_{x}",(x,.53,2.25),(.075,.08,1.55),M["galv"],.010)
        box(f"AXIS_Z_truck_{x}",(x,.43,2.28),(.14,.12,.28),M["black"],.020)
    box("AXIS_Z_ballscrew_cover",(0,.58,2.25),(.11,.07,1.62),M["black"],.012)
    box("AXIS_Z_carriage",(0,.20,2.27),(.82,.42,.88),M["blue"],.050)
    box("SPINDLE_head",(0,-.18,2.20),(.62,.42,.64),M["blue"],.055)
    cyl("SPINDLE_motor",(0,.25,2.72),.26,.46,M["steel"],"Z",96)
    box("SPINDLE_drive_cover",(0,-.13,2.58),(.68,.34,.34),M["blue"],.050)
    bpy.ops.object.empty_add(type="PLAIN_AXES",location=(0,-.66,2.70));spindle_pivot=bpy.context.object;spindle_pivot.name="KIN_spindle";spindle_pivot["rungproof_asset"]=True;spindle_pivot["rungproof_collision"]=False
    # Z is up in the authored model: spindle tooling must point downward over
    # the work, not forward out of the enclosure like a horizontal spindle.
    spindle=cyl("MACHINE_spindle",(0,-.66,2.70),.16,.42,M["aluminum"],"Z",96)
    bearing=cyl("SPINDLE_bearing_housing",(0,-.66,2.56),.225,.20,M["blue"],"Z",96)
    # BT40 geometry: spindle nose, flange, tapered holder, collet nut and
    # cutter remain separate so the tooling cannot be read as a dispense cone.
    nose=cyl("SPINDLE_nose",(0,-.66,2.43),.105,.16,M["black"],"Z",64)
    flange=cyl("TOOL_BT40_flange",(0,-.66,2.33),.145,.060,M["black"],"Z",64)
    bpy.ops.mesh.primitive_cone_add(vertices=64,radius1=.070,radius2=.112,depth=.18,location=(0,-.66,2.21));holder=finish(bpy.context.object,"TOOL_BT40_taper",M["steel"],.001,True)
    collet=cyl("TOOL_collet_nut",(0,-.66,2.08),.068,.095,M["black"],"Z",64)
    # A straight shank and a short fluted cutting section read as an end mill.
    # The earlier tapered orange form could be mistaken for a dispense nozzle.
    shank=cyl("TOOL_endmill_shank",(0,-.66,2.03),.034,.30,M["aluminum"],"Z",64)
    cutter=cyl("TOOL_endmill_cutter",(0,-.66,1.90),.050,.18,M["steel"],"Z",64)
    flutes=[]
    for a in range(0,360,90):
        r=math.radians(a);flutes.append(box(f"TOOL_flute_{a}",(math.cos(r)*.042,-.66+math.sin(r)*.042,1.90),(.012,.012,.17),M["black"],.002))
    for part in (spindle,bearing,nose,flange,holder,collet,shank,cutter,*flutes):part.parent=spindle_pivot;part.matrix_parent_inverse=spindle_pivot.matrix_world.inverted()
    box("WORK_vise_base",(0,-.62,1.40),(.78,.44,.13),M["blue"],.020)
    box("WORK_vise_fixed_jaw",(0,-.43,1.53),(.62,.10,.23),M["blue"],.014)
    box("WORK_vise_moving_jaw",(0,-.80,1.53),(.62,.10,.23),M["blue"],.014)
    # Raise the clamped billet above the near jaw so it is visible from the
    # operator view and gives the cutter an unambiguous machining target.
    box("WORK_stock",(0,-.62,1.73),(.40,.22,.14),M["yellow"],.006)
    for y,z in ((-.48,1.67),(-.75,1.67)): box(f"WORK_vise_jaw_face_{y}",(0,y,z),(.45,.018,.14),M["galv"],.003)
    for x in (-.22,.22):cyl(f"WORK_vise_clamp_bolt_{x}",(x,-.48,1.76),.032,.14,M["black"],"Z",48)
    # Power-actuated vise: avoid a large manual handwheel on an automated VMC.
    cyl("WORK_vise_leadscrew",(.42,-.62,1.53),.035,.32,M["aluminum"],"X",48)
    cyl("WORK_vise_actuator",(.57,-.62,1.53),.11,.22,M["blue"],"X",64)
    box("WORK_vise_actuator_mount",(.45,-.62,1.42),(.30,.18,.08),M["steel"],.008)
    for x in (-.56,.20):box(f"AXIS_way_cover_{x}",(x,-.76,1.30),(.30,.52,.08),M["steel"],.008)
    for i,y in enumerate((-.12,.02,.16,.30,.44)):
        box(f"AXIS_Y_bellows_rib_{i}",(0,y,1.38),(1.30,.045,.13),M["black"],.008)
    # A side-mounted rack is legible as tooling without impersonating a rotary
    # fourth axis in the frontal review.
    box("TOOL_rack",(.70,.34,2.02),(.20,.10,.78),M["steel"],.015)
    for z in (1.72,1.94,2.16,2.38):
        cyl(f"TOOL_rack_holder_{z}",(.70,.22,z),.052,.18,M["black"],"Y",48)
        cyl(f"TOOL_rack_tool_{z}",(.70,.10,z),.026,.20,M["aluminum"],"Y",48)
    # A visible side magazine gives the machine a real tool-change context.
    box("TOOL_magazine_housing",(-.82,.55,2.18),(.28,.14,1.20),M["blue"],.035)
    for z in (1.78,2.02,2.26,2.50):
        cyl(f"TOOL_magazine_pocket_{z}",(-.82,.38,z),.070,.18,M["black"],"Y",48)
        cyl(f"TOOL_magazine_holder_{z}",(-.82,.26,z),.040,.20,M["aluminum"],"Y",48)
    # Retained, segmented coolant line: its support and nozzle make it a real
    # process service rather than a visually detached diagonal cable.
    cyl("COOLANT_manifold",(-.48,.28,2.52),.10,.34,M["blue"],"Z",48)
    cable("COOLANT_hose",[(-.48,.20,2.34),(-.40,-.04,2.20),(-.28,-.35,2.04),(-.18,-.58,1.94)],.026,M["blue"])
    cyl("COOLANT_nozzle",(-.16,-.60,1.89),.045,.18,M["blue"],"Z",48)
    cyl("COOLANT_nozzle_tip",(-.16,-.60,1.79),.060,.06,M["blue"],"Z",48)
    for i,(x,z) in enumerate(((-.66,2.55),(-.66,2.30),(-.52,2.12),(-.38,2.00))):
        box(f"CABLE_CHAIN_link_{i}",(x,.50,z),(.14,.16,.10),M["black"],.012)
    cyl("CHIP_auger_tube",(.92,.58,.82),.12,1.18,M["steel"],"X",64)
    cyl("CHIP_auger_outlet",(1.48,.58,.82),.17,.26,M["blue"],"X",64)
    for x in (-.48,-.24,0,.24,.48):box(f"TABLE_Tslot_{x}",(x,-.60,1.317),(.035,.56,.018),M["black"],.002)
    for x in (-.31,.31):
        for y in (-.78,-.46):bolt(f"WORK_vise_hold_down_{x}_{y}",(x,y,1.475),M["steel"],"Z")
    box("MACHINE_control",(1.42,-.45,1.90),(.58,.42,1.05),M["steel"],.04)
    box("HMI_bezel",(1.42,-.684,2.08),(.46,.030,.37),M["black"],.012)
    box("HMI_screen",(1.42,-.704,2.08),(.39,.012,.30),emissive("CNC HMI display",(.008,.040,.045),.04),.004)
    hmi_line=emissive("CNC program text",(.18,.90,.42),1.6)
    for z,w in ((2.16,.25),(2.08,.18),(2.00,.29)):box(f"HMI_program_line_{z}",(1.42,-.715,z),(w,.008,.012),hmi_line,.002)
    for row,z in enumerate((1.92,1.84)):
        for col,x in enumerate((1.28,1.42,1.56)):
            box(f"HMI_key_{row}_{col}",(x,-.716,z),(.075,.010,.048),M["black"],.006)
    for x,c in ((1.29,M["green"]),(1.42,M["amber"]),(1.55,M["white"])):cyl(f"CTRL_button_{x}",(x,-.71,1.85),.035,.026,c,"Y",32)
    cyl("CTRL_estop_collar",(1.42,-.705,1.62),.105,.025,M["yellow"],"Y",48);cyl("CTRL_estop",(1.42,-.74,1.62),.065,.06,M["red"],"Y")
    box("CHIP_sump",(0,.28,.68),(1.86,1.10,.22),M["black"],.020)
    box("CHIP_chute",(.78,.32,.80),(.48,.50,.12),M["steel"],.012);bpy.context.object.rotation_euler.x=math.radians(-17)
    text_label("CTRL_start_label","START",(1.29,-.725,1.72),.042,M["white"])
    text_label("CTRL_estop_label","E-STOP",(1.42,-.725,1.51),.055,M["white"])
    for z,c in ((2.72,M["red"]),(2.88,M["amber"]),(3.04,M["green"])):cyl(f"STATUS_stack_{z}",(1.02,-.40,z),.07,.16,c,"Z",48)
    cyl("STATUS_stack_pole",(1.02,-.40,2.59),.025,.42,M["steel"],"Z",32)
    text_label("MACHINE_nameplate","CNC VMC",(0,-.825,2.92),.13,M["white"])
    for x in (-.82,.82):cyl(f"KIN_door_roller_{x}",(x,-.65,.32),.055,.08,M["black"],"Y")

BUILDERS={"through_beam_photoeye":photoeye,"operator_pushbutton_station":pushbutton,"single_pushbutton_station":single_pushbutton,"emergency_stop_station":emergency_stop,"single_tier_beacon":single_beacon,"stack_light_3_tier":stacklight,"pneumatic_pusher":pusher,
"ac_induction_motor":motor,"flanged_pipe_spool":pipe_spool,"vertical_process_tank":tank,"centrifugal_pump_skid":pump,"actuated_process_valve":valve,
"level_transmitter_4_20ma":level_transmitter,"tuning_fork_level_switch":level_switch,"radar_level_transmitter":radar,"rotary_selector_station":rotary_switch,
"axial_exhaust_fan":fan,"scissor_lift_table":lift_table,"industrial_drill_press":drill_press,"six_axis_robot":robot,"powered_rotary_table":rotary_table,
"motorized_roller_shutter":shutter,"enclosed_machine_center":machine}

def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat("-Z","Y").to_euler()
def save_asset(slug,builder):
    clean();M=common();builder(M)
    root=BASE/slug
    for p in (root/"source",root/"delivery",root/"collision",root/"review"):p.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"))
    objects=[o for o in bpy.context.scene.objects if o.get("rungproof_asset") and not o.get("rungproof_review_only")]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    # Conservative aggregate collision hull; detailed collision is not visual geometry.
    mins=Vector((1e9,1e9,1e9));maxs=Vector((-1e9,-1e9,-1e9))
    collision_objects=[o for o in objects if o.get("rungproof_collision",True)]
    if not collision_objects:
        raise RuntimeError(f"Asset {slug} has no collision-contributing geometry")
    for o in collision_objects:
        for c in o.bound_box:
            w=o.matrix_world@Vector(c);mins.x=min(mins.x,w.x);mins.y=min(mins.y,w.y);mins.z=min(mins.z,w.z);maxs.x=max(maxs.x,w.x);maxs.y=max(maxs.y,w.y);maxs.z=max(maxs.z,w.z)
    bpy.ops.object.select_all(action="DESELECT");bpy.ops.mesh.primitive_cube_add(location=(mins+maxs)/2);col=bpy.context.object;col.name="COLLISION_primary";col.dimensions=maxs-mins;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.export_scene.gltf(filepath=str(root/"collision"/f"{slug}_collision.glb"),export_format="GLB",use_selection=True,export_apply=True)
    bpy.data.objects.remove(col,do_unlink=True)
    # Review setup excluded from delivery.
    floor=box("REVIEW_floor",(0,0,mins.z-.035),(5,5,.05),M["black"],.002,False)
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.025,.035,.04)
    for i,(loc,energy,size) in enumerate((((4,-4,6),1100,4),((-3,-1,3),700,3),((0,4,4),850,3))):
        d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=energy;d.shape="DISK";d.size=size;o=bpy.data.objects.new(d.name,d);o.location=loc;bpy.context.collection.objects.link(o);point(o,(0,0,(mins.z+maxs.z)/2))
    cd=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",cd);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;cd.lens=52
    scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG";scene.render.film_transparent=False
    radius=max((maxs-mins).length*(1.24 if slug=="radar_level_transmitter" else 1.05),2.6);center=(mins+maxs)/2
    elevation={"radar_level_transmitter":.18,"scissor_lift_table":.22}.get(slug,.55)
    for idx,angle in enumerate((35,125,215,305)):
        a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*elevation);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{idx+1:02d}.png");bpy.ops.render.render(write_still=True)
    hero_view={"radar_level_transmitter":4,"vertical_process_tank":4,"rotary_selector_station":3,"axial_exhaust_fan":3,"enclosed_machine_center":4}.get(slug,1)
    primary_review = (root / "review" / f"{slug}_{hero_view:02d}.png").read_bytes()
    (root / "thumbnail.png").write_bytes(primary_review)
    # The source-model gate must inspect current render evidence, not a stale
    # blind review from a prior form of the asset.
    (root / "review" / "blind_review.png").write_bytes(primary_review)
    ASSETS.append((slug,mins,maxs))

asset_filter={item.strip() for item in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if item.strip()}
for slug,builder in BUILDERS.items():
    if not asset_filter or slug in asset_filter:save_asset(slug,builder)
print("SCENE_CORE_ASSETS_BUILT",len(ASSETS))
