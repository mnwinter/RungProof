"""Build the first reusable factory-construction and passive-load asset kit."""
from __future__ import annotations
import math, os
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(os.environ["RUNGPROOF_PROJECT_ROOT"]); BASE=ROOT/"assets"/"factory_kit"; BUILT=[]

def clean():
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes,bpy.data.curves,bpy.data.cameras,bpy.data.lights,bpy.data.materials):
        for block in list(blocks):
            if block.users==0:blocks.remove(block)

def mat(name,c,metal=0,rough=.42,alpha=1):
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,alpha);m.use_nodes=True
    b=m.node_tree.nodes.get("Principled BSDF");b.inputs["Base Color"].default_value=(*c,1);b.inputs["Metallic"].default_value=metal;b.inputs["Roughness"].default_value=rough
    if alpha<1:b.inputs["Alpha"].default_value=alpha;m.surface_render_method="DITHERED"
    return m

def finish(o,n,m=None,bev=.004,smooth=False,collision=True):
    o.name=n
    if m:o.data.materials.append(m)
    if bev:
        mod=o.modifiers.new("Manufactured edge radius","BEVEL");mod.width=bev;mod.segments=3;mod.limit_method="ANGLE"
    if smooth and hasattr(o.data,"polygons"):
        for p in o.data.polygons:p.use_smooth=True
    o["rungproof_asset"]=True;o["rungproof_collision"]=collision;return o

def box(n,loc,d,m,bev=.004,collision=True):
    bpy.ops.mesh.primitive_cube_add(location=loc);o=bpy.context.object;o.dimensions=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,m,bev,False,collision)

def cyl(n,loc,r,d,m,axis="Z",verts=64,collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d,location=loc,rotation=rot);return finish(bpy.context.object,n,m,.002,True,collision)

def torus(n,loc,major,minor,m,axis="Z",collision=True):
    rot=(math.pi/2,0,0) if axis=="Y" else ((0,math.pi/2,0) if axis=="X" else (0,0,0));bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=64,minor_segments=12,location=loc,rotation=rot);return finish(bpy.context.object,n,m,0,True,collision)

def sphere(n,loc,r,m,collision=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=32,radius=r,location=loc)
    return finish(bpy.context.object,n,m,.002,True,collision)

def tube_between(n,start,end,r,m,collision=True):
    """Create a round member whose ends land exactly on two design points."""
    a=Vector(start);b=Vector(end);delta=b-a
    o=cyl(n,(a+b)/2,r,delta.length,m,"Z",48,collision)
    o.rotation_euler=delta.to_track_quat("Z","Y").to_euler()
    return o

def common():return dict(steel=mat("Painted steel",(.07,.15,.20),.65,.28),galv=mat("Galvanized steel",(.48,.53,.55),.78,.30),yellow=mat("Safety yellow",(.95,.57,.015),.25,.32),black=mat("Black polymer",(.018,.023,.026),.08,.36),red=mat("Safety red",(.70,.018,.012),.18,.30),white=mat("Industrial white",(.82,.85,.85),.18,.43),blue=mat("Industrial blue",(.025,.25,.55),.28,.30),wood=mat("Pallet hardwood",(.42,.22,.075),.02,.74),concrete=mat("Sealed concrete",(.34,.36,.36),.02,.82),glass=mat("Safety glass",(.09,.34,.42),.10,.16,.30),orange=mat("Safety orange",(.95,.20,.01),.15,.32))

def bolt(n,loc,M,axis="Z"):return cyl(n,loc,.018,.018,M["black"],axis,12)

def concrete_mat():
    # A sealed slab still has concrete texture, but its lower roughness gives
    # the broad, soft light response that distinguishes a coated/sealed finish
    # from bare matte concrete or a generic grey board.
    m=mat("Sealed concrete floor",(.18,.20,.20),.01,.48);nodes=m.node_tree.nodes;links=m.node_tree.links;b=nodes.get("Principled BSDF")
    noise=nodes.new("ShaderNodeTexNoise");noise.inputs["Scale"].default_value=7.0;noise.inputs["Detail"].default_value=5.0;noise.inputs["Roughness"].default_value=.72
    ramp=nodes.new("ShaderNodeValToRGB");ramp.color_ramp.elements[0].color=(.10,.115,.12,1);ramp.color_ramp.elements[1].color=(.31,.33,.34,1)
    bump=nodes.new("ShaderNodeBump");bump.inputs["Strength"].default_value=.22;bump.inputs["Distance"].default_value=.025
    links.new(noise.outputs["Fac"],ramp.inputs["Fac"]);links.new(ramp.outputs["Color"],b.inputs["Base Color"]);links.new(noise.outputs["Fac"],bump.inputs["Height"]);links.new(bump.outputs["Normal"],b.inputs["Normal"])
    return m

def floor_slab(M):
    slab_mat=concrete_mat()
    box("CONCRETE_slab",(0,0,.04),(6,6,.08),slab_mat,.006)
    # Saw-cut control joints, aisle stripes, anchors and a grated drain make the
    # object read as plant floor rather than as an equipment access cover.
    for x in (-1.5,0,1.5):box(f"CONTROL_joint_x_{x}",(x,0,.085),(.018,5.75,.008),M["black"],.001,False)
    for y in (-1.5,0,1.5):box(f"CONTROL_joint_y_{y}",(0,y,.086),(5.75,.018,.008),M["black"],.001,False)
    box("SAFETY_aisle_long",(-2.55,0,.095),(.10,5.5,.012),M["yellow"],.002,False)
    box("DRAIN_frame",(1.9,1.9,.098),(.62,.62,.022),M["galv"],.004,False)
    for i in range(7):box(f"DRAIN_grate_{i}",(1.66+i*.08,1.9,.112),(.030,.52,.010),M["black"],.001,False)

def wall_panel(M):
    box("WALL_core",(0,0,1.5),(4,.16,3),M["white"],.012)
    for x in (-1.5,-1.0,-.5,0,.5,1.0,1.5):box(f"WALL_rib_{x}",(x,.095,1.5),(.018,.028,2.92),M["galv"],.002)
    for x in (-2,-1,0,1,2):box(f"WALL_panel_joint_{x}",(x,.112,1.5),(.025,.018,2.96),M["steel"],.001,False)
    box("BASE_flashing",(0,.12,.10),(4.08,.12,.20),M["steel"],.006);box("TOP_cap",(0,.02,3.04),(4.08,.22,.08),M["steel"],.006)
    foam=mat("Insulation core",(.86,.67,.20),.02,.76)
    box("INSULATION_exposed_edge",(2.015,0,1.55),(.035,.145,2.72),foam,.002,False)

def insulated_panel(M):
    wall_panel(M)
    # This tranche's solid panel is a machine-guard/acoustic partition; make
    # its freestanding guarding function explicit with posts and anchor feet.
    for x in (-2.08,2.08):
      box(f"GUARD_post_{x}",(x,0,1.55),(.12,.12,3.10),M["yellow"],.006)
      box(f"GUARD_foot_{x}",(x,0,.035),(.42,.38,.07),M["galv"],.005)

def door_module(M):
    # Compose a real infill-panel opening; a painted rectangle on a continuous
    # wall cannot read as a modular equipment-enclosure personnel door.
    box("WALL_left",(-1.52,0,1.5),(.96,.16,3.0),M["white"],.012)
    box("WALL_right",(1.52,0,1.5),(.96,.16,3.0),M["white"],.012)
    box("WALL_header",(0,0,2.75),(2.12,.16,.50),M["white"],.012)
    box("BASE_flashing",(0,.12,.10),(4.08,.12,.20),M["steel"],.006)
    box("TOP_cap",(0,.02,3.04),(4.08,.22,.08),M["steel"],.006)
    box("DOOR_leaf",(0,.13,1.36),(1.12,.07,2.48),M["blue"],.016)
    box("DOOR_frame_top",(0,.18,2.62),(1.34,.13,.12),M["steel"],.006)
    box("DOOR_threshold",(0,.22,.13),(1.30,.30,.10),M["galv"],.006)
    for x in (-.64,.64):
        box(f"DOOR_frame_{x}",(x,.18,1.36),(.12,.13,2.60),M["steel"],.006)
    # Three exposed barrel hinges and a right-hand latch/escutcheon establish
    # opening direction and hardware without asserting lock or egress ratings.
    for z in (.65,1.36,2.05):
        cyl(f"DOOR_hinge_{z}",(-.60,.235,z),.045,.13,M["black"],"Y",48)
    box("DOOR_latch_escutcheon",(.43,.185,1.35),(.16,.025,.32),M["steel"],.006,False)
    cyl("DOOR_handle",(.43,.235,1.36),.032,.17,M["black"],"Y",48)
    box("DOOR_kickplate",(0,.185,.38),(.92,.025,.42),M["galv"],.004,False)
    for x in (-1.96,-1.35,1.35,1.96):
        box(f"WALL_seam_{x}",(x,.10,1.50),(.018,.025,2.88),M["steel"],.001,False)
    for x,z in ((-.64,.16),(.64,.16),(-.64,2.62),(.64,2.62)):
        cyl(f"FRAME_fastener_{x}_{z}",(x,.258,z),.018,.010,M["black"],"Y",24,False)

def window_module(M):
    # Build around an actual opening instead of laying glass over a solid wall.
    box("WALL_left",(-1.58,0,1.5),(.84,.16,3.0),M["white"],.012);box("WALL_right",(1.58,0,1.5),(.84,.16,3.0),M["white"],.012)
    box("WALL_bottom",(0,0,.50),(2.32,.16,1.0),M["white"],.012);box("WALL_top",(0,0,2.67),(2.32,.16,.66),M["white"],.012)
    box("BASE_flashing",(0,.12,.10),(4.08,.12,.20),M["steel"],.006);box("TOP_cap",(0,.02,3.04),(4.08,.22,.08),M["steel"],.006)
    box("WINDOW_recess",(0,.115,1.65),(2.23,.045,1.29),M["black"],.004,False)
    # A separate blue-tinted pane, black gasket reveal, and two restrained
    # diagonal reflections make this read as glazing rather than a dark screen
    # or empty opening in a wall.
    glass=mat("Observation safety glazing",(.12,.46,.60),.12,.10,.46)
    box("WINDOW_glass",(0,.155,1.65),(2.12,.025,1.18),glass,.008,False)
    for i,x in enumerate((-.48,.38)):
      glare=box(f"WINDOW_reflection_{i}",(x,.172,1.72),(.030,.010,.58),M["white"],.001,False)
      glare.rotation_euler=(0,math.radians(36 if i == 0 else -36),0)
    for x in (-1.12,1.12):box(f"WINDOW_jamb_{x}",(x,.18,1.65),(.10,.12,1.38),M["steel"],.005)
    for z in (1.00,2.30):box(f"WINDOW_rail_{z}",(0,.18,z),(2.34,.12,.10),M["steel"],.005)
    box("WINDOW_mullion",(0,.19,1.65),(.08,.13,1.22),M["steel"],.004)
    box("WINDOW_sill",(0,.28,.96),(2.48,.36,.10),M["galv"],.006)
    # Retention screws make this a removable industrial observation frame,
    # not domestic glazing set directly into wall sheet.
    for x in (-1.10,1.10):
      for z in (1.00,2.30):
        cyl(f"WINDOW_retainer_{x}_{z}",(x,.255,z),.020,.012,M["black"],"Y",24,False)
    for x in (-2.04,2.04):
      box(f"PANEL_edge_post_{x}",(x,.08,1.50),(.10,.14,3.06),M["steel"],.006)
      box(f"PANEL_foot_{x}",(x,.02,.04),(.34,.30,.08),M["galv"],.005)
    for x in (-1.78,-1.38,1.38,1.78):box(f"WALL_seam_{x}",(x,.10,1.50),(.018,.025,2.88),M["steel"],.001,False)

def column(M):
    # Keep the thin web visible between two wide, shallow flanges.  The prior
    # arrangement read as a box/built-up post from the review camera.
    box("COLUMN_web",(0,0,2.0),(.12,.38,4),M["steel"],.005)
    box("COLUMN_flange_front",(0,-.24,2.0),(.66,.10,4),M["steel"],.005)
    box("COLUMN_flange_rear",(0,.24,2.0),(.66,.10,4),M["steel"],.005)
    box("BASE_plate",(0,0,.04),(.92,.92,.08),M["galv"],.008);box("CAP_plate",(0,0,4.04),(.82,.78,.08),M["galv"],.008)
    for x in (-.34,.34):
      for y in (-.34,.34):bolt(f"ANCHOR_{x}_{y}",(x,y,.095),M)

def ibeam(M):
    box("BEAM_web",(0,0,0),(5,.12,.56),M["steel"],.004);box("BEAM_top",(0,0,.34),(5,.55,.12),M["steel"],.004);box("BEAM_bottom",(0,0,-.34),(5,.55,.12),M["steel"],.004)
    for x in (-2.15,2.15):
      box(f"CONNECTION_plate_{x}",(x,-.12,0),(.40,.08,.52),M["galv"],.004)
      for z in (-.16,.16):bolt(f"CONNECTION_bolt_{x}_{z}",(x,-.18,z),M,"Y")

def guardrail_parts(M,length=4,z0=0):
    # A pedestrian guardrail is a surface-mounted barrier: retain the open
    # handrail geometry, but make its base fixing, post caps, and rail-to-post
    # joints visible instead of leaving it as generic yellow linework.
    for x in (-length/2,length/2):
      box(f"RAIL_post_{x}",(x,0,z0+.56),(.07,.07,1.12),M["yellow"],.006)
      box(f"RAIL_foot_{x}",(x,0,z0+.025),(.30,.24,.05),M["galv"],.006)
      box(f"RAIL_base_gusset_{x}",(x,0,z0+.145),(.16,.055,.20),M["yellow"],.003)
      sphere(f"RAIL_post_cap_{x}",(x,0,z0+1.125),.043,M["black"],False)
      for dx in (-.095,.095):
        for dy in (-.065,.065):
          cyl(f"RAIL_anchor_washer_{x}_{dx}_{dy}",(x+dx,dy,z0+.060),.026,.008,M["black"],"Z",24)
          bolt(f"RAIL_anchor_{x}_{dx}_{dy}",(x+dx,dy,z0+.071),M)
    for z in (z0+.58,z0+1.10):cyl(f"RAIL_tube_{z}",(0,0,z),.035,length,M["yellow"],"X",48)
    for x in (-length/2,length/2):
      for z in (z0+.58,z0+1.10):torus(f"RAIL_joint_{x}_{z}",(x,0,z),.042,.008,M["black"],"X",False)
    box("RAIL_toeboard",(0,0,z0+.10),(length,.06,.20),M["yellow"],.004)

def guardrail(M):guardrail_parts(M)

def catwalk(M):
    box("CATWALK_deck",(0,0,1.25),(4,1.35,.14),M["galv"],.008)
    # Cross bars make the walking surface read as an industrial grating deck
    # at normal review distance without claiming a particular load rating.
    for x in (-1.75,-1.40,-1.05,-.70,-.35,0,.35,.70,1.05,1.40,1.75):
      box(f"CATWALK_grating_{x}",(x,0,1.335),(.035,1.24,.025),M["steel"],.001,False)
    for x in (-1.7,0,1.7):
      for y in (-.52,.52):
        box(f"CATWALK_leg_{x}_{y}",(x,y,.62),(.12,.12,1.24),M["steel"],.005)
        box(f"CATWALK_base_plate_{x}_{y}",(x,y,.04),(.36,.36,.08),M["galv"],.005)
        for dx in (-.10,.10):
          for dy in (-.10,.10):bolt(f"CATWALK_anchor_{x}_{y}_{dx}_{dy}",(x+dx,y+dy,.095),M)
    # X braces on both long faces give the platform a credible lateral load
    # path, while remaining a reusable modular platform asset.
    for y in (-.58,.58):
      for start,end in (((-1.7,y,.16),(0,y,1.15)),((0,y,.16),(1.7,y,1.15))):
        tube_between(f"CATWALK_brace_{y}_{start[0]}_{end[0]}",start,end,.035,M["steel"],False)
      for start,end in (((-1.7,y,1.15),(0,y,.16)),((0,y,1.15),(1.7,y,.16))):
        tube_between(f"CATWALK_brace_reverse_{y}_{start[0]}_{end[0]}",start,end,.035,M["steel"],False)
    for y in (-.66,.66):
      for x in (-1.95,0,1.95):box(f"CATWALK_post_{x}_{y}",(x,y,1.82),(.06,.06,1.08),M["yellow"],.003)
      for z in (1.75,2.32):cyl(f"CATWALK_rail_{y}_{z}",(0,y,z),.03,3.9,M["yellow"],"X",48)
      box(f"CATWALK_toe_{y}",(0,y,1.38),(3.9,.05,.20),M["yellow"],.003)
    # Both deck ends are protected.  Access is intentionally a separate
    # modular stair/ladder asset, rather than an unprotected edge.
    for x in (-1.95,1.95):
      for z in (1.75,2.32):cyl(f"CATWALK_end_rail_{x}_{z}",(x,0,z),.03,1.32,M["yellow"],"Y",48)
      box(f"CATWALK_end_toe_{x}",(x,0,1.38),(.05,1.30,.20),M["yellow"],.003)

def stairs(M):
    steps=10;run=2.8;rise=2.0
    for i in range(steps):
      x=-run/2+(i+.5)*run/steps;z=(i+.5)*rise/steps;box(f"STAIR_tread_{i}",(x,0,z),(run/steps+.03,1.15,.08),M["galv"],.004)
    for y in (-.52,.52):
      s=box(f"STAIR_stringer_{y}",(0,y,rise/2),(3.48,.10,.14),M["steel"],.005);s.rotation_euler.y=-math.atan2(rise,run)
      for i in range(0,steps+1,2):
        x=-run/2+i*run/steps;tread_z=i*rise/steps;rail_z=tread_z+1.02
        box(f"STAIR_post_{y}_{i}",(x,y,(tread_z+rail_z)/2),(.055,.055,rail_z-tread_z),M["yellow"],.003)
      tube_between(f"STAIR_handrail_{y}",(-run/2,y,1.02),(run/2,y,rise+1.02),.032,M["yellow"])
      tube_between(f"STAIR_midrail_{y}",(-run/2,y,.56),(run/2,y,rise+.56),.026,M["yellow"])
    # Close the elevated landing edge with a transverse guardrail; the access
    # side remains a deliberate modular interface for a platform or gate.
    for y in (-.52,.52):
      box(f"STAIR_landing_post_{y}",(run/2,y,rise+.55),(.055,.055,1.10),M["yellow"],.003)
    for z in (rise+1.10,rise+.62):cyl(f"STAIR_landing_rail_{z}",(run/2,0,z),.032,1.10,M["yellow"],"Y",48)
    box("STAIR_landing_toeboard",(run/2,0,rise+.12),(.06,1.10,.20),M["yellow"],.003)

def caged_ladder(M):
    # A slim backplane is deliberately part of this fixed-access module so
    # mounting is visible in isolated catalog previews rather than implied.
    box("LADDER_mounting_backplane",(0,.68,2.0),(1.08,.10,4.0),M["steel"],.004)
    for x in (-.24,.24):box(f"LADDER_rail_{x}",(x,0,2.0),(.05,.05,4),M["galv"],.003)
    for i in range(14):box(f"LADDER_rung_{i}",(0,0,.18+i*.28),(.56,.05,.045),M["galv"],.003)
    # Use a continuous rectangular cage rather than decorative torus hoops:
    # it remains legible from the review camera, has no clipped intersections,
    # and is appropriate for a reusable generic access-ladder asset.
    cage_y_front=-.40;cage_y_back=.30;cage_x=.43
    for x in (-cage_x,cage_x):
      for y in (cage_y_front,cage_y_back):
        box(f"CAGE_longitudinal_{x}_{y}",(x,y,3.05),(.040,.040,1.80),M["yellow"],.002)
    # Leave the lower climbing zone open. The cage begins above the entry and
    # its first band flares outward, matching a credible fixed-ladder cage
    # rather than enclosing the operator at floor level.
    for i,z in enumerate((2.15,2.55,3.00,3.45,3.80)):
      cyl(f"CAGE_band_front_{i}",(0,cage_y_front,z),.026,cage_x*2,M["yellow"],"X",32)
      cyl(f"CAGE_band_back_{i}",(0,cage_y_back,z),.026,cage_x*2,M["yellow"],"X",32)
      cyl(f"CAGE_band_left_{i}",(-cage_x,(cage_y_front+cage_y_back)/2,z),.026,cage_y_back-cage_y_front,M["yellow"],"Y",32)
      cyl(f"CAGE_band_right_{i}",(cage_x,(cage_y_front+cage_y_back)/2,z),.026,cage_y_back-cage_y_front,M["yellow"],"Y",32)
    for x in (-cage_x,cage_x):
      tube_between(f"CAGE_flare_{x}",(x,cage_y_front,2.15),(x*1.18,cage_y_front-.08,1.92),.026,M["yellow"])
    # Rear stand-offs and vertical mounting rails make the fixed-structure
    # connection explicit without inventing a particular wall construction.
    for x in (-.30,.30):box(f"LADDER_rear_mount_rail_{x}",(x,.53,2.0),(.07,.08,3.90),M["steel"],.004)
    for z in (.45,1.65,2.85,3.80):
      for x in (-.30,.30):
        cyl(f"LADDER_mount_standoff_{z}_{x}",(x,.28,z),.035,.48,M["steel"],"Y",32)
        bolt(f"LADDER_wall_anchor_{z}_{x}",(x,.59,z),M,"Y")
    for x in (-.24,.24):
      box(f"LADDER_base_plate_{x}",(x,0,.035),(.26,.28,.07),M["galv"],.004)
      for dx in (-.07,.07):
        for dy in (-.07,.07):bolt(f"LADDER_base_anchor_{x}_{dx}_{dy}",(x+dx,dy,.090),M)
    # A short top landing with side guards provides an identifiable walk-off
    # transition; the final connection to a scene platform is intentionally modular.
    box("LADDER_top_landing",(0,0,4.06),(.90,.82,.10),M["galv"],.004)
    # Three-sided guard boundary at the walk-off: the ladder exit side is the
    # intentional opening and includes a visible self-closing-style gate bar.
    for x in (-.40,.40):
      for y in (-.32,.28):box(f"LADDER_top_guard_post_{x}_{y}",(x,y,4.58),(.05,.05,1.04),M["yellow"],.003)
    for z in (4.52,4.98):
      cyl(f"LADDER_top_guard_rear_{z}",(0,.28,z),.030,.86,M["yellow"],"X",48)
      cyl(f"LADDER_top_guard_left_{z}",(-.40,-.02,z),.030,.62,M["yellow"],"Y",48)
      cyl(f"LADDER_top_guard_right_{z}",(.40,-.02,z),.030,.62,M["yellow"],"Y",48)
    cyl("LADDER_top_gate_bar",(0,-.32,4.76),.028,.72,M["yellow"],"X",48)

def fence(M):
    for x in (-1.55,1.55):
      box(f"FENCE_post_{x}",(x,0,1.05),(.10,.10,2.10),M["yellow"],.006)
      box(f"FENCE_foot_{x}",(x,0,.035),(.38,.32,.07),M["galv"],.006)
      box(f"FENCE_base_gusset_{x}",(x,0,.17),(.18,.060,.25),M["yellow"],.003)
      sphere(f"FENCE_post_cap_{x}",(x,0,2.115),.060,M["black"],False)
      for dx in (-.12,.12):
        for dy in (-.09,.09):
          cyl(f"FENCE_anchor_washer_{x}_{dx}_{dy}",(x+dx,dy,.076),.027,.009,M["black"],"Z",24)
          bolt(f"FENCE_anchor_{x}_{dx}_{dy}",(x+dx,dy,.087),M)
    for z in (.18,1.95):box(f"FENCE_rail_{z}",(0,0,z),(3.0,.08,.08),M["yellow"],.005)
    # Thin dark mesh remains visibly open, avoiding a misleading solid-panel guard.
    # Capture each vertical inside the upper/lower frame instead of leaving
    # wire-like tails exposed beneath the lower rail.
    for x in [-1.4+i*.14 for i in range(21)]:box(f"MESH_v_{x}",(x,0,1.07),(.012,.025,1.65),M["black"],.001,False)
    for z in [.28+i*.14 for i in range(12)]:box(f"MESH_h_{z}",(0,0,z),(2.82,.025,.012),M["black"],.001,False)
    for x in (-1.55,1.55):
      for z in (.18,1.95):torus(f"FENCE_joint_{x}_{z}",(x,0,z),.055,.009,M["black"],"X",False)

def gate(M):
    for x in (-1.15,1.15):box(f"GATE_post_{x}",(x,0,1.05),(.12,.12,2.10),M["yellow"],.006);box(f"GATE_foot_{x}",(x,0,.035),(.38,.34,.07),M["galv"],.005)
    box("GATE_frame_top",(0,-.08,1.86),(2.1,.08,.08),M["yellow"],.004);box("GATE_frame_bottom",(0,-.08,.30),(2.1,.08,.08),M["yellow"],.004)
    for x in (-1.0,1.0):box(f"GATE_frame_side_{x}",(x,-.08,1.08),(.08,.08,1.64),M["yellow"],.004)
    for x in [-.9+i*.15 for i in range(13)]:box(f"GATE_mesh_{x}",(x,-.08,1.08),(.012,.025,1.42),M["black"],.001,False)
    for z in [.4+i*.15 for i in range(10)]:box(f"GATE_mesh_h_{z}",(0,-.08,z),(1.82,.025,.012),M["black"],.001,False)
    for z in (.42,1.70):cyl(f"GATE_hinge_{z}",(-1.08,.10,z),.055,.18,M["black"],"Z",48)
    box("GATE_latch",(.95,.17,1.08),(.18,.16,.28),M["red"],.025)
    brace=box("GATE_diagonal_brace",(0,.10,1.08),(2.30,.055,.055),M["yellow"],.003);brace.rotation_euler.y=-math.atan2(1.45,1.9)
    cyl("GATE_handle",(.82,.18,1.20),.028,.24,M["black"],"Z",40)
    cyl("GATE_caster",(.82,0,.20),.10,.06,M["black"],"Y",48)

def bollard(M):
    # Surface-mounted impact bollard: continuous painted post, a full
    # circumferential reflective sleeve, and a dome cap.  A flat one-sided
    # reflector reads as an accidental tab from all other views.
    cyl("BOLLARD_post",(0,0,.58),.12,1.16,M["yellow"],"Z",96)
    cyl("BOLLARD_reflective_sleeve",(0,0,.86),.123,.135,M["white"],"Z",96)
    torus("BOLLARD_reflective_lower_edge",(0,0,.792),.123,.008,M["black"])
    torus("BOLLARD_reflective_upper_edge",(0,0,.928),.123,.008,M["black"])
    sphere("BOLLARD_dome_cap",(0,0,1.155),.122,M["black"])
    cyl("BOLLARD_base_shroud",(0,0,.105),.155,.090,M["yellow"],"Z",96)
    box("BOLLARD_base",(0,0,.035),(.48,.48,.07),M["galv"],.008)
    for x in (-.17,.17):
      for y in (-.17,.17):
        cyl(f"ANCHOR_washer_{x}_{y}",(x,y,.075),.032,.011,M["black"],"Z",48)
        cyl(f"ANCHOR_{x}_{y}",(x,y,.095),.017,.044,M["black"],"Z",12)

def light_curtain(M):
    for x,label in ((-1,"TX"),(1,"RX")):
      box(f"LIGHT_CURTAIN_{label}",(x,0,1.15),(.12,.14,1.85),M["yellow"],.025)
      box(f"STAND_{label}",(x,0,.20),(.07,.07,.34),M["steel"],.004)
      box(f"FOOT_{label}",(x,0,.035),(.36,.30,.07),M["galv"],.005)
      # Visibly differentiate emitter and receiver without asserting any
      # manufacturer-specific safety rating or protocol.
      face = M["orange"] if label == "TX" else M["blue"]
      box(f"OPTIC_WINDOW_{label}",(x,-.077,1.15),(.082,.012,1.60),face,.004,False)
      for i in range(14):cyl(f"OPTIC_{label}_{i}",(x,-.088,.27+i*.125),.014,.013,M["red"],"Y",24)
      box(f"STATUS_{label}",(x,-.090,2.00),(.054,.012,.055),M["green"] if "green" in M else M["white"],.003,False)
      cyl(f"CABLE_ENTRY_{label}",(x,.082,.30),.032,.08,M["black"],"Y",32)
    # Reviewable simulator witness geometry: keep every beam equally visible
    # from emitter to receiver instead of using transparency that makes a
    # continuous protective field appear intermittent or misaligned.
    beam=mat("Infrared beam",(1,.02,.01),.02,.15,1.0)
    for i in range(14):box(f"BEAM_{i}",(0,-.095,.27+i*.125),(2.00,.012,.008),beam,.001,False)

def workbench(M):
    box("BENCH_top",(0,0,1.0),(2.4,.9,.12),M["wood"],.012)
    for x in (-1.0,1.0):
      for y in (-.32,.32):box(f"BENCH_leg_{x}_{y}",(x,y,.48),(.10,.10,.96),M["steel"],.005)
    box("BENCH_shelf",(0,0,.25),(2.05,.70,.08),M["galv"],.006);box("BENCH_drawer",(.65,.43,.78),(.70,.34,.32),M["blue"],.02);box("DRAWER_handle",(.65,.62,.80),(.35,.06,.04),M["black"],.01)
    box("VISE_base",(-.70,-.05,1.12),(.42,.34,.12),M["blue"],.025);box("VISE_jaw_fixed",(-.83,-.05,1.28),(.10,.38,.22),M["galv"],.005);box("VISE_jaw_moving",(-.55,-.05,1.28),(.10,.38,.22),M["galv"],.005);cyl("VISE_handle",(-.38,-.05,1.17),.018,.55,M["black"],"Y",32)

def tool_cabinet(M):
    box("CABINET_body",(0,0,.65),(1.25,.65,1.30),M["blue"],.045)
    for i in range(6):
      z=.23+i*.17;box(f"DRAWER_{i}",(0,.34,z),(1.10,.06,.145),M["steel"],.006);box(f"HANDLE_{i}",(0,.39,z),(.45,.04,.035),M["black"],.008)
    for x in (-.48,.48):
      for y in (-.24,.24):cyl(f"CASTER_{x}_{y}",(x,y,.04),.07,.05,M["black"],"Y",48)
    # Front casters and a side push handle are intentionally exposed so the
    # asset reads as a mobile cabinet rather than a fixed drawer pedestal.
    for x in (-.48,.48):
      cyl(f"CASTER_FORK_{x}",(x,.355,.12),.042,.12,M["galv"],"Z",24)
      cyl(f"CASTER_FRONT_{x}",(x,.355,.045),.075,.055,M["black"],"Y",48)
    tube_between("CABINET_PUSH_HANDLE",(.69,-.22,.45),(.69,-.22,1.02),.026,M["steel"],False)
    box("CABINET_top",(0,0,1.34),(1.30,.70,.10),M["black"],.018)

def enclosure(M):
    box("ENCLOSURE_body",(0,0,1.05),(1.20,.38,1.85),M["white"],.045);box("ENCLOSURE_door",(0,.215,1.08),(1.10,.07,1.72),M["galv"],.025)
    for z in (.42,1.72):cyl(f"HINGE_{z}",(-.51,.27,z),.035,.18,M["black"],"Z",48)
    box("HANDLE",(.43,.29,1.08),(.10,.08,.34),M["black"],.028);box("GLAND_plate",(0,0,.10),(.78,.44,.08),M["steel"],.008)
    for x in (-.32,0,.32):cyl(f"CABLE_gland_{x}",(x,0,.035),.055,.12,M["black"],"Z",48)
    box("DANGER_label",(0,.26,1.48),(.34,.025,.24),M["yellow"],.004,False)
    box("FLOOR_plinth",(0,0,.11),(1.28,.46,.22),M["steel"],.012)

def junction_box(M):
    box("JBOX_body",(0,0,.45),(.70,.36,.72),M["white"],.045);box("JBOX_cover",(0,.205,.45),(.62,.06,.64),M["galv"],.018)
    for x in (-.24,.24):
      for z in (.22,.68):bolt(f"COVER_bolt_{x}_{z}",(x,.245,z),M,"Y")
    for x in (-.22,0,.22):cyl(f"GLAND_{x}",(x,0,.04),.055,.14,M["black"],"Z",48)
    for x in (-.43,.43):
      for z in (.16,.74):box(f"MOUNTING_lug_{x}_{z}",(x,.02,z),(.16,.08,.12),M["galv"],.008)
    box("TERMINAL_label",(0,.245,.46),(.30,.012,.10),M["white"],.003,False);box("WARNING_label",(0,.252,.60),(.30,.012,.13),M["yellow"],.003,False)

def cable_tray(M):
    # Open rails plus regular exposed transverse rungs read as a ladder tray
    # rather than a shallow solid-sided trough. Keep the cable bundle inside
    # those rails; previous black cross-bars/slots looked disconnected from the
    # tray and were mistaken for geometry intersecting the cables.
    for y in (-.34,.34):box(f"TRAY_rail_{y}",(0,y,.82),(4,.065,.08),M["galv"],.004)
    for i,x in enumerate([-1.8+j*.30 for j in range(13)]):box(f"TRAY_rung_{i}",(x,0,.80),(.05,.70,.055),M["galv"],.003)
    cable_colors=(M["black"],M["blue"],M["orange"],M["red"])
    for i,y in enumerate((-.24,-.16,-.08,0,.08,.16,.24)):
      # Equal, inset ends keep all conductors contained by the tray rather
      # than looking like differently cut loose lengths.
      cyl(f"CABLE_{i}",(0,y,.88),.018,3.30,cable_colors[i%len(cable_colors)],"X",24,False)
    for x in (-1.55,1.55):
      # Standalone catalogue evidence cannot imply an unexplained ceiling.
      # Use two visibly connected under-tray support yokes instead of detached
      # rods and floating ceiling plates.
      box(f"TRAY_support_crossbar_{x}",(x,0,.70),(.10,.82,.10),M["steel"],.004)
      for y in (-.34,.34):
        box(f"TRAY_support_yoke_{x}_{y}",(x,y,.755),(.10,.06,.15),M["steel"],.003)

def pipe_support(M):
    # A floor-anchored rack gives one unambiguous load path for this reusable
    # multi-pipe support: pipe -> clamp/saddle -> crossbar -> legs -> anchors.
    support_z=1.04
    station_x=(-.72,.72)
    pipe_specs=((-.72,.11),(0,.13),(.72,.11))
    for x in station_x:
      box(f"TRAPEZE_crossbar_{x}",(x,0,support_z),(.16,2.25,.14),M["steel"],.004)
      for y in (-1.0,1.0):
        box(f"RACK_leg_{x}_{y}",(x,y,support_z/2),(.12,.12,support_z),M["steel"],.004)
        box(f"RACK_baseplate_{x}_{y}",(x,y,.04),(.36,.36,.08),M["galv"],.004)
        for dx in (-.10,.10):
          for dy in (-.10,.10):bolt(f"RACK_anchor_{x}_{y}_{dx}_{dy}",(x+dx,y+dy,.09),M)
    for y in (-1.0,1.0):
      tube_between(f"RACK_brace_a_{y}",(-.72,y,.16),(.72,y,support_z-.10),.035,M["steel"],False)
      tube_between(f"RACK_brace_b_{y}",(-.72,y,support_z-.10),(.72,y,.16),.035,M["steel"],False)
    for i,(y,r) in enumerate(pipe_specs):
      pipe_z=support_z+.16+r
      cyl(f"PROCESS_pipe_{i}",(0,y,pipe_z),r,3.25,M["galv"],"X",96)
      for x in (-1.63,1.63):
        cyl(f"PIPE_open_end_{i}_{x}",(x,y,pipe_z),r*.72,.012,M["black"],"X",48,False)
      for sx in station_x:
        box(f"PIPE_saddle_{i}_{sx}",(sx,y,support_z+.12),(.26,r*2.35,.09),M["galv"],.008)
        torus(f"PIPE_clamp_{i}_{sx}",(sx,y,pipe_z),r+.020,.012,M["galv"],"X")
        for dx in (-.11,.11):
          cyl(f"CLAMP_bolt_{i}_{sx}_{dx}",(sx+dx,y,support_z+.20),.026,.11,M["black"],"Z",6,False)

def pallet(M):
    # A GMA-style pallet needs separately readable boards, blocks, and open
    # fork entries.  Keeping small gaps between the boards is deliberate: a
    # smooth single slab looks like molded plastic in review renders.
    wood = mat("Rough pallet hardwood", (.37, .16, .045), .0, .82)
    nodes = wood.node_tree.nodes; links = wood.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    grain = nodes.new("ShaderNodeTexNoise"); grain.inputs["Scale"].default_value = 5.5
    grain.inputs["Detail"].default_value = 3.0; grain.inputs["Roughness"].default_value = .72
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = .30; ramp.color_ramp.elements[0].color = (.09, .026, .006, 1)
    ramp.color_ramp.elements[1].position = .72; ramp.color_ramp.elements[1].color = (.42, .19, .050, 1)
    bump = nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = .25; bump.inputs["Distance"].default_value = .035
    links.new(grain.outputs["Fac"], ramp.inputs["Fac"]); links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(grain.outputs["Fac"], bump.inputs["Height"]); links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    # Seven top boards span the 40 in direction. Three lower runners and nine
    # blocks leave four-way fork/pallet-jack openings visible from either side.
    for x in (-.54, -.36, -.18, 0, .18, .36, .54):
      box(f"PALLET_top_board_{x}", (x, 0, .285), (.135, 1.00, .075), wood, .003)
      for y in (-.40, .40):
        cyl(f"PALLET_top_nail_{x}_{y}", (x, y, .327), .011, .007, M["black"], "Z", 16, False)
    for x in (-.48, 0, .48):
      box(f"PALLET_bottom_runner_{x}", (x, 0, .035), (.145, 1.00, .070), wood, .003)
    for x in (-.48, 0, .48):
      for y in (-.39, 0, .39):
        box(f"PALLET_block_{x}_{y}", (x, y, .145), (.155, .145, .205), wood, .003)

def tote(M):
    # Model the open shell rather than placing a dark rectangle on a solid box:
    # the interior, rim, and handholds must remain inspectable in review views.
    tote_blue = mat("Reusable tote HDPE", (.018, .20, .52), .05, .36)
    tote_dark = mat("Tote recessed shadow", (.008, .018, .035), .0, .55)
    box("TOTE_bottom", (0, 0, .10), (1.00, .70, .18), tote_blue, .035)
    for x in (-.46, .46): box(f"TOTE_side_{x}", (x, 0, .39), (.085, .70, .52), tote_blue, .028)
    for y in (-.31, .31): box(f"TOTE_end_{y}", (0, y, .39), (.84, .085, .52), tote_blue, .028)
    for x in (-.50, .50): box(f"TOTE_rim_x_{x}", (x, 0, .67), (.09, .78, .095), tote_blue, .026)
    for y in (-.36, .36): box(f"TOTE_rim_y_{y}", (0, y, .67), (1.04, .09, .095), tote_blue, .026)
    # Dark inset panels mark through-handholds without claiming a particular
    # manufacturer's molded geometry.
    for x in (-.506, .506):
      box(f"TOTE_handle_recess_{x}", (x, 0, .50), (.010, .27, .135), tote_dark, .022, False)
      box(f"TOTE_handle_top_{x}", (x, 0, .575), (.025, .31, .035), tote_blue, .012)
    for x in (-.35, -.18, 0, .18, .35):
      box(f"TOTE_front_rib_{x}", (x, -.356, .37), (.034, .025, .43), tote_blue, .006)
      box(f"TOTE_back_rib_{x}", (x, .356, .37), (.034, .025, .43), tote_blue, .006)

def drum(M):
    # Closed-head shipping drums have reinforced top/bottom chimes and two
    # shallow rolling hoops.  Four bright torus bands read as a pressure vessel
    # in review renders, so these features stay proportionate and body-coloured.
    drum_blue = mat("Drum blue steel", (.018, .18, .43), .62, .32)
    label_white = mat("Drum label", (.82, .84, .82), .02, .48)
    cyl("DRUM_shell", (0, 0, .56), .34, 1.10, drum_blue, "Z", 128)
    for z in (.055, 1.065): torus(f"DRUM_chime_{z}", (0, 0, z), .347, .027, drum_blue)
    for z in (.38, .75): torus(f"DRUM_rolling_hoop_{z}", (0, 0, z), .342, .012, drum_blue)
    cyl("DRUM_top_head", (0, 0, 1.118), .326, .038, drum_blue, "Z", 128)
    torus("DRUM_top_seam", (0, 0, 1.138), .324, .008, M["steel"])
    # Hexagonal plugs, retaining collars, and a small seal recess communicate
    # closed-head bungs without assigning a product, fluid, or hazard class.
    for name, loc, radius in (("BUNG_large", (.14, .07, 1.155), .052), ("BUNG_small", (-.14, -.08, 1.155), .033)):
      cyl(name, loc, radius, .040, M["black"], "Z", 6)
      torus(f"{name}_collar", (loc[0], loc[1], 1.145), radius + .010, .006, M["steel"])
      cyl(f"{name}_recess", (loc[0], loc[1], 1.178), radius * .48, .006, M["steel"], "Z", 6, False)
    box("DRUM_label_panel", (0, -.342, .61), (.31, .010, .24), label_white, .006, False)
    box("DRUM_label_rule", (0, -.349, .67), (.24, .004, .012), M["steel"], .001, False)

def ibc(M):
    plastic=mat("Translucent HDPE",(.76,.82,.78),.02,.32,.88);box("IBC_tank",(0,0,.78),(1.05,1.0,1.42),plastic,.12)
    for x in (-.56,.56):
      for y in (-.54,.54):box(f"IBC_cage_post_{x}_{y}",(x,y,.80),(.045,.045,1.52),M["galv"],.002)
    for z in (.18,.48,.78,1.08,1.38):
      for y in (-.54,.54):box(f"IBC_cage_x_{z}_{y}",(0,y,z),(1.16,.04,.04),M["galv"],.002)
      for x in (-.56,.56):box(f"IBC_cage_y_{z}_{x}",(x,0,z),(.04,1.12,.04),M["galv"],.002)
    # A slotted base makes the pallet/fork-entry interface visible instead of
    # reducing it to a featureless black plinth.
    box("IBC_pallet_deck", (0, 0, .155), (1.15, 1.10, .08), M["black"], .012)
    for x in (-.43, 0, .43): box(f"IBC_pallet_runner_{x}", (x, 0, .065), (.19, 1.15, .13), M["black"], .012)
    for y in (-.48, .48): box(f"IBC_pallet_crossmember_{y}", (0, y, .105), (1.14, .10, .10), M["black"], .010)
    # A flange, short nozzle, valve body, pivot, and lever make the gravity
    # outlet recognizable without asserting a thread, seal, or flow rating.
    cyl("IBC_outlet_flange", (0, .535, .285), .145, .070, M["black"], "Y", 64)
    box("IBC_valve_body", (0, .650, .285), (.25, .16, .20), M["black"], .018)
    cyl("IBC_outlet_nozzle", (0, .790, .285), .082, .22, M["black"], "Y", 64)
    cyl("IBC_outlet_cap", (0, .910, .285), .094, .038, M["steel"], "Y", 48)
    cyl("IBC_handle_pivot", (0, .748, .395), .035, .055, M["steel"], "Y", 32)
    box("IBC_handle", (.13, .750, .435), (.30, .040, .055), M["red"], .010)
    cyl("IBC_fill_cap", (0, 0, 1.53), .16, .06, M["black"], "Z", 64)

def coil(M):
    cyl("STEEL_coil", (0, 0, .78), .75, .85, M["galv"], "Y", 160)
    cyl("COIL_eye", (0, 0, .78), .28, .90, M["black"], "Y", 128, False)
    # Fine outer wraps and two circumferential shipping bands keep the object
    # legible as coiled strip rather than a generic solid roll.
    for y in (-.34, -.24, -.14, -.04, .06, .16, .26, .36): torus(f"COIL_wrap_{y}", (0, y, .78), .748, .006, M["steel"], "Y", False)
    for y in (-.30, .30): torus(f"COIL_shipping_band_{y}", (0, y, .78), .762, .016, M["black"], "Y", False)
    # Opposed V-chocks support the lower quadrant of the coil; small end stops
    # identify the tall members as retention hardware rather than random posts.
    for x, angle in ((-.39, -32), (.39, 32)):
      s=box(f"COIL_saddle_chock_{x}", (x, 0, .19), (.20, 1.12, .20), M["wood"], .010); s.rotation_euler.y=math.radians(angle)
    for x in (-.64, .64):
      box(f"COIL_retention_post_{x}", (x, 0, .45), (.075, .14, .75), M["steel"], .006)
      box(f"COIL_retention_foot_{x}", (x, 0, .05), (.26, .34, .10), M["wood"], .008)

def sheet_stack(M):
    for i in range(18):box(f"SHEET_{i}",(0,0,.24+i*.025),(2.4,1.2,.018),M["galv"],.001)
    for y in (-.42,.42):box(f"DUNNAGE_{y}",(0,y,.10),(2.55,.14,.20),M["wood"],.006)
    for x in (-.72,.72):box(f"BAND_{x}",(x,-.61,.46),(.06,.02,.58),M["black"],.002,False);box(f"BAND_top_{x}",(x,0,.76),(.06,1.24,.02),M["black"],.002,False)

def floor_scale(M):
    box("SCALE_base",(0,0,.055),(1.62,1.62,.11),M["black"],.012)
    box("SCALE_deck",(0,0,.15),(1.48,1.48,.18),M["galv"],.012)
    # A bounded weighing deck and four externally readable corner supports are
    # essential visual cues; they do not assert calibrated load-cell behavior.
    for x in (-.72,.72):
      for y in (-.72,.72):
        cyl(f"SCALE_foot_{x}_{y}",(x,y,.025),.075,.05,M["black"],"Z",48)
        box(f"SCALE_corner_guard_{x}_{y}",(x,y,.115),(.14,.14,.08),M["steel"],.006)
    for y in (-.76,.76):box(f"SCALE_deck_border_{y}",(0,y,.255),(1.36,.025,.020),M["black"],.002,False)
    for x in (-.76,.76):box(f"SCALE_deck_border_{x}",(x,0,.255),(.025,1.36,.020),M["black"],.002,False)
    box("SCALE_ramp_in",(-1.05,0,.06),(.60,1.5,.12),M["steel"],.004);box("SCALE_ramp_out",(1.05,0,.06),(.60,1.5,.12),M["steel"],.004)
    for x in (-.60,.60):
      for y in (-.60,.60):cyl(f"LOAD_CELL_{x}_{y}",(x,y,.03),.06,.05,M["black"],"Z",48)
    box("DISPLAY_post",(1.15,.75,.75),(.08,.08,1.25),M["steel"],.004);box("DISPLAY_head",(1.15,.75,1.40),(.50,.24,.34),M["blue"],.035);box("DISPLAY_screen",(1.15,.885,1.42),(.32,.025,.15),M["black"],.004,False)
    led=mat("Display LED",(.05,1.0,.12),.03,.18)
    for i,x in enumerate((1.04,1.12,1.20)): 
      for z in (1.38,1.46):box(f"DISPLAY_digit_h_{i}_{z}",(x,.902,z),(.055,.008,.012),led,.001,False)
      for dx in (-.026,.026):box(f"DISPLAY_digit_v_{i}_{dx}",(x+dx,.902,1.42),(.010,.008,.075),led,.001,False)
    box("DISPLAY_kg_badge",(1.31,.902,1.37),(.05,.008,.04),M["white"],.001,False)
    box("DISPLAY_face_border",(1.15,.905,1.42),(.40,.008,.22),M["white"],.003,False)
    tube_between("DISPLAY_cable",(1.15,.71,.15),(.78,.68,.08),.018,M["black"],False)

def scanner(M):
    # Compact fixed-mount reader: protected scan window, two neutral optical
    # elements, status indicator, connector and bracket—not an invented panel.
    box("SCANNER_frame",(0,0,1.55),(.34,.30,.30),M["black"],.025)
    box("SCANNER_window",(0,.165,1.56),(.24,.018,.16),M["glass"],.005,False)
    cyl("SCANNER_lens",(-.060,.180,1.56),.040,.014,M["black"],"Y",64,False)
    cyl("SCANNER_illuminator",(.060,.180,1.56),.026,.014,M["white"],"Y",48,False)
    cyl("STATUS_green",(.105,.181,1.66),.012,.010,mat("Status green",(.02,.80,.10),.05,.22),"Y",24,False)
    box("SCANNER_post",(-.55,0,.72),(.09,.09,1.45),M["steel"],.004);box("SCANNER_foot",(-.55,0,.035),(.40,.35,.07),M["galv"],.005)
    box("SCANNER_mount_bracket",(-.28,.02,1.48),(.62,.12,.10),M["galv"],.006);cyl("SCANNER_mount_pivot",(-.28,.10,1.48),.070,.035,M["black"],"Y",48)
    bolt("SCANNER_mount_bolt",(-.28,.125,1.48),M,"Y")
    beam=mat("Scanner beam",(1,.015,.005),.02,.12,.30)
    # A parcel with a visible barcode makes the scanner purpose legible without text.
    box("PARCEL",(.48,.05,.32),(.72,.60,.52),M["wood"],.025,False);box("BARCODE_target",(.48,.36,.40),(.54,.035,.30),M["white"],.008,False)
    for i,w in enumerate((.022,.040,.018,.055,.026,.034,.018,.050,.024)):
      box(f"BARCODE_bar_{i}",(.27+i*.052,.382,.40),(w,.008,.20),M["black"],.001,False)
    tube_between("SCAN_beam",(-.07,.245,1.56),(.48,.385,.40),.012,beam,False)
    cyl("SCANNER_connector",(0,-.185,1.48),.045,.09,M["black"],"Y",48)
    tube_between("SCANNER_cable",(0,-.24,1.48),(-.42,-.06,.20),.016,M["black"],False)

BUILDERS={"sealed_concrete_floor_slab":floor_slab,"insulated_wall_panel":insulated_panel,"personnel_door_wall_module":door_module,"observation_window_wall_module":window_module,"structural_w_column":column,"structural_w_beam":ibeam,"elevated_catwalk_platform":catwalk,"industrial_stair_flight":stairs,"fixed_caged_ladder":caged_ladder,"pedestrian_guardrail":guardrail,"machine_safety_fence_panel":fence,"machine_safety_swing_gate":gate,"safety_bollard":bollard,"safety_light_curtain_pair":light_curtain,"industrial_workbench":workbench,"mobile_tool_cabinet":tool_cabinet,"floorstanding_electrical_enclosure":enclosure,"industrial_junction_box":junction_box,"ladder_cable_tray":cable_tray,"trapeze_pipe_support":pipe_support,"gma_wood_pallet":pallet,"reusable_plastic_tote":tote,"steel_shipping_drum":drum,"ibc_tote":ibc,"steel_coil_on_saddles":coil,"banded_sheet_metal_stack":sheet_stack,"industrial_floor_scale":floor_scale,"fixed_barcode_scanner":scanner}

def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat("-Z","Y").to_euler()
def save(slug,builder):
    clean();M=common();builder(M);root=BASE/slug
    for p in (root/"source",root/"delivery",root/"collision",root/"review"):p.mkdir(parents=True,exist_ok=True)
    for p in (root/"source",root/"review"):(p/".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/"source"/f"{slug}.blend"))
    objects=[o for o in bpy.context.scene.objects if o.get("rungproof_asset")]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.export_scene.gltf(filepath=str(root/"delivery"/f"{slug}.glb"),export_format="GLB",use_selection=True,export_apply=True)
    collision=[o for o in objects if o.get("rungproof_collision",True)];mins=Vector((1e9,1e9,1e9));maxs=Vector((-1e9,-1e9,-1e9))
    for o in collision:
      for c in o.bound_box:
        w=o.matrix_world@Vector(c);mins.x=min(mins.x,w.x);mins.y=min(mins.y,w.y);mins.z=min(mins.z,w.z);maxs.x=max(maxs.x,w.x);maxs.y=max(maxs.y,w.y);maxs.z=max(maxs.z,w.z)
    bpy.ops.object.select_all(action="DESELECT");bpy.ops.mesh.primitive_cube_add(location=(mins+maxs)/2);col=bpy.context.object;col.name="COLLISION_primary";col.dimensions=maxs-mins;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bpy.ops.export_scene.gltf(filepath=str(root/"collision"/f"{slug}_collision.glb"),export_format="GLB",use_selection=True,export_apply=True);bpy.data.objects.remove(col,do_unlink=True)
    floor_z=(0.0 if slug in {"ladder_cable_tray","trapeze_pipe_support"} else mins.z-.035)
    floor=box("REVIEW_floor",(0,0,floor_z),(max(5,maxs.x-mins.x+2),max(5,maxs.y-mins.y+2),.05),M["black"],.002,False);floor["rungproof_asset"]=False
    world=bpy.context.scene.world or bpy.data.worlds.new("World");bpy.context.scene.world=world;world.color=(.025,.035,.04)
    visual_mins=Vector((1e9,1e9,1e9));visual_maxs=Vector((-1e9,-1e9,-1e9))
    for o in objects:
      for c in o.bound_box:
        w=o.matrix_world@Vector(c);visual_mins.x=min(visual_mins.x,w.x);visual_mins.y=min(visual_mins.y,w.y);visual_mins.z=min(visual_mins.z,w.z);visual_maxs.x=max(visual_maxs.x,w.x);visual_maxs.y=max(visual_maxs.y,w.y);visual_maxs.z=max(visual_maxs.z,w.z)
    center=(visual_mins+visual_maxs)/2;span=visual_maxs-visual_mins;radius=max(span.length*1.20,2.4)
    for i,(loc,energy,size) in enumerate((((4,-4,6),1150,4),((-3,-1,3),650,3),((0,4,4),800,3))):
      d=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");d.energy=energy;d.shape="DISK";d.size=size;o=bpy.data.objects.new(d.name,d);o.location=loc;bpy.context.collection.objects.link(o);point(o,center)
    cd=bpy.data.cameras.new("REVIEW_camera");cam=bpy.data.objects.new("REVIEW_camera",cd);bpy.context.collection.objects.link(cam);bpy.context.scene.camera=cam;cd.lens=52
    scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    elevation=.78 if slug=="sealed_concrete_floor_slab" else (.36 if slug in {"structural_w_beam","gma_wood_pallet","banded_sheet_metal_stack"} else .52)
    angles=((125,35,215,305) if slug=="trapeze_pipe_support" else (35,125,215,305))
    for idx,angle in enumerate(angles):
      a=math.radians(angle);cam.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*elevation);point(cam,center);scene.render.filepath=str(root/"review"/f"{slug}_{idx+1:02d}.png");bpy.ops.render.render(write_still=True)
    primary_review=(root/"review"/f"{slug}_01.png").read_bytes()
    (root/"thumbnail.png").write_bytes(primary_review)
    (root/"review"/"blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)

asset_filter={x.strip() for x in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if x.strip()}
for slug,builder in BUILDERS.items():
    if not asset_filter or slug in asset_filter:save(slug,builder)
print("FACTORY_KIT_ASSETS_BUILT",len(BUILT))
