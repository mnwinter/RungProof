"""Build reusable industrial sensing, safety, and operator-control assets."""
from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
BASE = ROOT / "assets" / "controls_sensors"
BUILT: list[str] = []


def clean():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights, bpy.data.materials):
        for block in list(blocks):
            if block.users == 0:
                blocks.remove(block)


def mat(name, color, metal=0.0, rough=.40, alpha=1.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, alpha)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Metallic"].default_value = metal
    shader.inputs["Roughness"].default_value = rough
    if alpha < 1:
        shader.inputs["Alpha"].default_value = alpha
        material.surface_render_method = "DITHERED"
    return material


def finish(obj, name, material=None, bevel=.004, smooth=False, collision=True):
    obj.name = name
    if material:
        obj.data.materials.append(material)
    if bevel:
        modifier = obj.modifiers.new("Manufactured edge radius", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        modifier.limit_method = "ANGLE"
    if smooth and hasattr(obj.data, "polygons"):
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    obj["rungproof_asset"] = True
    obj["rungproof_collision"] = collision
    return obj


def box(name, location, dimensions, material, bevel=.004, collision=True):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bevel, False, collision)


def cyl(name, location, radius, depth, material, axis="Z", vertices=64, collision=True):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    return finish(bpy.context.object, name, material, .002, True, collision)


def sphere(name, location, radius, material, collision=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64, ring_count=32, radius=radius, location=location)
    return finish(bpy.context.object, name, material, .002, True, collision)


def torus(name, location, major, minor, material, axis="Z", collision=True):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=64, minor_segments=12, location=location, rotation=rotation)
    return finish(bpy.context.object, name, material, 0, True, collision)


def tube_between(name, start, end, radius, material, collision=True):
    a, b = Vector(start), Vector(end)
    delta = b - a
    obj = cyl(name, (a + b) / 2, radius, delta.length, material, "Z", 48, collision)
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    return obj


def cut_cyl(target, name, location, radius, depth, axis="Z", vertices=64):
    rotation = (math.pi / 2, 0, 0) if axis == "Y" else ((0, math.pi / 2, 0) if axis == "X" else (0, 0, 0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    cutter = bpy.context.object
    cutter.name = name
    modifier = target.modifiers.new(name, "BOOLEAN")
    modifier.operation = "DIFFERENCE"
    modifier.solver = "EXACT"
    modifier.object = cutter
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def text_label(name, body, location, size, material, rotation=(-math.pi/2,0,0)):
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj=bpy.context.object;obj.data.body=body;obj.data.align_x="CENTER";obj.data.align_y="CENTER";obj.data.size=size;obj.data.extrude=.002;obj.data.bevel_depth=.001
    # Blender's front-face text appears horizontally reversed after the X-axis
    # rotation used on vertical equipment faces. Mirror the local X geometry so
    # nameplates read correctly from the primary +Y inspection camera.
    if rotation == (-math.pi/2,0,0):
        obj.scale.x = -1
    bpy.ops.object.convert(target="MESH")
    return finish(obj,name,material,0,False,False)


def common():
    return {
        "steel": mat("Painted steel", (.07, .14, .18), .68, .27),
        "stainless": mat("Stainless steel", (.56, .60, .61), .88, .22),
        "zinc": mat("Zinc plated", (.44, .48, .48), .76, .30),
        "black": mat("Black polymer", (.012, .018, .022), .08, .34),
        "blue": mat("Industrial blue", (.025, .24, .56), .26, .29),
        "yellow": mat("Safety yellow", (.98, .58, .01), .20, .30),
        "red": mat("Safety red", (.76, .018, .012), .16, .28),
        "green": mat("Indicator green", (.015, .62, .08), .08, .22),
        "amber": mat("Indicator amber", (1.0, .32, .01), .08, .23),
        "white": mat("Industrial white", (.84, .87, .87), .16, .40),
        "brass": mat("Nickel-plated brass", (.64, .59, .42), .76, .24),
        "glass": mat("Optical glass", (.04, .22, .29), .18, .12),
        "rubber": mat("Rubber", (.018, .022, .022), .02, .72),
    }


def mount_plate(M, width=.42, height=.55):
    box("MOUNT_plate", (0, -.10, height / 2), (width, .08, height), M["zinc"], .006)
    for x in (-width * .38, width * .38):
        for z in (height * .18, height * .82):
            cyl(f"MOUNT_hole_{x}_{z}", (x, -.145, z), .025, .012, M["black"], "Y", 32, False)


def m12_connector(M, location, axis="Y", prefix="M12"):
    cyl(f"{prefix}_body", location, .055, .11, M["black"], axis, 48)
    cyl(f"{prefix}_nut", location, .070, .045, M["zinc"], axis, 12)


def inductive_sensor(M):
    # Keep the barrel exposed on a small bracket: a plate over the sensing end
    # makes an inductive sensor indistinguishable from a panel pushbutton.
    box("L_BRACKET", (0, -.16, .34), (.42, .08, .62), M["zinc"], .008)
    cyl("SENSOR_threaded_barrel", (0, .25, .34), .09, .78, M["brass"], "Y", 96)
    for y in (.00, .12):
        cyl(f"LOCKNUT_{y}", (0, y, .34), .125, .055, M["zinc"], "Y", 12)
        torus(f"LOCKWASHER_{y}", (0, y + .032, .34), .105, .008, M["steel"], "Y", False)
    for y in (.20,.27,.34,.41,.48):torus(f"THREAD_{y}",(0,y,.34),.090,.005,M["zinc"],"Y",False)
    # The dark recessed face, flats/nuts, small status window, and rear M12
    # connector are the durable generic cues of an installed M18 inductive
    # sensor.  Do not add a trade dress, part number, sensing range, or label.
    cyl("SENSING_face", (0, .65, .34), .083, .030, M["black"], "Y", 96, False)
    cyl("STATUS_LED", (.070, .52, .41), .014, .014, M["green"], "Y", 24, False)
    m12_connector(M, (0, -.20, .34), "Y", "REAR_M12")
    tube_between("SENSOR_cable",(0,-.27,.34),(0,-.82,.18),.020,M["black"],False)
    box("METAL_TARGET",(0,1.03,.34),(.30,.06,.30),M["zinc"],.012,False)
    box("TARGET_bracket",(0,1.10,.16),(.42,.10,.10),M["steel"],.006,False)


def capacitive_sensor(M):
    # An open bracket deliberately leaves the M30 barrel, thread ridges, and
    # both locknuts in view.  A full plate concealed these identity cues and
    # made the prior candidate look like a generic wired probe.
    box("MOUNT_upright", (-.25, -.10, .38), (.10, .10, .72), M["zinc"], .008)
    box("MOUNT_base", (-.06, -.10, .07), (.48, .34, .11), M["zinc"], .008)
    box("MOUNT_gusset", (-.18, -.10, .18), (.24, .10, .22), M["zinc"], .006)
    cyl("SENSOR_M30_barrel", (0, .23, .39), .120, .82, M["amber"], "Y", 96)
    for y in (.00, .12):
        cyl(f"LOCKNUT_{y}", (0, y, .39), .165, .055, M["zinc"], "Y", 12)
        torus(f"LOCKWASHER_{y}",(0,y+.032,.39),.142,.010,M["steel"],"Y",False)
    # Fine, shallow, metal-coloured rings are external thread crests, not a
    # decorative coil. The dense pitch and separate hex nuts make the M30
    # mounting system readable in a normal inspection render.
    for i in range(14):
        y = .20 + i * .026
        torus(f"THREAD_CREST_{i:02d}", (0, y, .39), .121, .004, M["zinc"], "Y", False)
    # The broad amber collar and recessed black polymer disc make the active
    # capacitive sensing face distinct from the side-mounted green status LED.
    cyl("CAPACITIVE_face_collar", (0, .65, .39), .123, .040, M["yellow"], "Y", 96, False)
    cyl("CAPACITIVE_sensing_face", (0, .674, .39), .094, .010, M["black"], "Y", 96, False)
    cyl("CAP_STATUS_LED", (.076, .57, .49), .027, .018, M["green"], "Y", 32, False)
    # A marked recessed trim screw is a simulator-visible adjustment cue; it
    # does not claim a particular manufacturer's setting method or range.
    cyl("CAP_SENSITIVITY_adjust", (-.078, .50, .50), .030, .014, M["black"], "Y", 32, False)
    box("CAP_SENSITIVITY_slot", (-.078, .509, .50), (.040, .006, .007), M["white"], .001, False)
    text_label("CAPACITIVE_label", "CAPACITIVE", (0, .595, .31), .024, M["white"])
    # A top nameplate stays legible in the standard high three-quarter review
    # view without borrowing a manufacturer mark or implying a part number.
    box("CAP_nameplate", (0, .43, .517), (.24, .22, .014), M["white"], .003, False)
    text_label("CAP_nameplate_text", "CAP", (0, .43, .526), .052, M["black"], (0, 0, 0))
    m12_connector(M, (0, -.22, .39), "Y", "REAR_M12")
    tube_between("SENSOR_cable",(0,-.29,.39),(-.18,-.54,.18),.016,M["black"],False)
    # Use a small open plastic hopper filled with non-metallic pellets, rather
    # than an unexplained flat panel, to make the intended capacitive target
    # material and sensing relationship legible without claiming a range.
    # Offset the hopper slightly so an inspection view can see the flat active
    # face and the non-metal target at the same time.
    target_x = .26
    box("HOPPER_back", (target_x, 1.22, .43), (.76, .10, .72), M["blue"], .025, False)
    for x in (target_x - .33, target_x + .33):
        box(f"HOPPER_side_{x}", (x, 1.10, .43), (.10, .30, .72), M["blue"], .016, False)
    box("HOPPER_bottom", (target_x, 1.10, .10), (.76, .30, .10), M["blue"], .012, False)
    box("HOPPER_rim", (target_x, 1.07, .79), (.84, .08, .07), M["blue"], .012, False)
    box("TARGET_STAND", (target_x, 1.24, .06), (.84, .34, .10), M["steel"], .008, False)
    # Dense, unevenly offset polymer pellets read as bulk material in an open
    # hopper, rather than as isolated buttons on a target panel.
    for row, z in enumerate((.20, .29, .38, .47, .56, .65)):
        offset = .045 if row % 2 else 0
        for column in (-.24, -.12, 0, .12, .24):
            sphere(f"POLYMER_PELLET_{row}_{column}", (target_x + column + offset, 1.14, z), .032, M["white"], False)


def diffuse_photoeye(M):
    # An open L bracket keeps the front optical face readable.  The former
    # full plate dominated the review view and made the sensor resemble a
    # generic pushbutton rather than a diffuse photoeye.
    box("L_BRACKET_upright", (-.25, -.13, .38), (.09, .10, .72), M["zinc"], .008)
    box("L_BRACKET_base", (-.07, -.13, .06), (.46, .32, .10), M["zinc"], .008)
    box("PHOTOEYE_body", (0, .08, .38), (.32, .30, .44), M["black"], .022)
    box("OPTICAL_face", (0, .245, .42), (.30, .030, .30), M["black"], .012, False)
    # Distinct, differently coloured transmitter and receiver apertures,
    # plus a return-light line, make diffuse-reflective operation legible
    # without implying any proprietary sensing range or wiring standard.
    # Keep the optical apertures dark and compact.  Large coloured discs read
    # as pushbuttons in a blind review, whereas a shared smoked window with
    # recessed lenses is the conventional visual cue for a photoelectric head.
    box("OPTICAL_window_inner", (0, .273, .43), (.24, .010, .18), M["glass"], .005, False)
    cyl("EMITTER_lens", (-.070, .281, .43), .042, .012, M["black"], "Y", 64, False)
    cyl("RECEIVER_lens", (.070, .281, .43), .042, .012, M["black"], "Y", 64, False)
    cyl("STATUS_led", (.120, .273, .57), .014, .012, M["green"], "Y", 24, False)
    m12_connector(M, (0, -.14, .18), "Y", "M12")
    tube_between("SENSOR_cable", (0, -.22, .18), (-.14, -.52, .10), .016, M["black"], False)
    beam=mat("Diffuse beam",(1,.01,.005),.01,.12,.30)
    returned=mat("Diffuse return",(.02,.22,.36),.01,.10,.22)
    tube_between("EMITTED_beam",(-.070,.29,.43),(-.070,1.05,.43),.014,beam,False)
    tube_between("RETURNED_light",(-.03,1.05,.43),(.070,.29,.43),.010,returned,False)
    box("TARGET_carton",(-.04,1.18,.42),(.55,.32,.62),mat("Target carton",(.48,.27,.10),.01,.70),.025,False)


def through_beam_pair(M):
    # Opposed sensors are compact heads on adjustment brackets. Tall generic
    # posts made the former scene read as camera equipment rather than a
    # recognizable industrial through-beam pair.
    for x,kind in ((-.62,"EMITTER"),(.62,"RECEIVER")):
      box(f"BRACKET_base_{kind}",(x,-.04,.07),(.36,.28,.10),M["zinc"],.008)
      box(f"BRACKET_upright_{kind}",(x,-.12,.30),(.08,.10,.48),M["zinc"],.006)
      box(f"BRACKET_arm_{kind}",(x,-.01,.50),(.28,.10,.08),M["zinc"],.006)
      box(f"HEAD_{kind}",(x,.10,.50),(.23,.34,.42),M["black"],.024)
      box(f"FACE_{kind}",(x,.285,.50),(.16,.018,.22),M["glass"],.004,False)
      cyl(f"OPTIC_{kind}",(x,.299,.50),.052,.012,M["black"],"Y",48,False)
      cyl(f"STATUS_{kind}",(x+.072,.300,.61),.012,.010,M["green"],"Y",24,False)
      m12_connector(M,(x,-.10,.34),"Y",f"M12_{kind}")
    beam = mat("Photoelectric beam", (1, .02, .01), .01, .12, .22)
    box("THROUGH_BEAM", (0, .305, .50), (1.00, .008, .008), beam, .001, False)


def ultrasonic_sensor(M):
    box("MOUNT_base",(-.13,-.12,.07),(.52,.34,.11),M["zinc"],.008)
    box("MOUNT_upright",(-.25,-.10,.36),(.10,.10,.66),M["zinc"],.008)
    cyl("ULTRASONIC_barrel", (0, .21, .39), .090, .72, M["black"], "Y", 96)
    for y in (.01,.12):cyl(f"LOCKNUT_{y}",(0,y,.39),.125,.055,M["zinc"],"Y",12)
    for i in range(13):torus(f"THREAD_{i}",(0,.20+i*.028,.39),.091,.004,M["zinc"],"Y",False)
    cyl("TRANSDUCER_ring",(0,.585,.39),.094,.040,M["zinc"],"Y",96,False)
    cyl("TRANSDUCER_membrane",(0,.610,.39),.073,.014,M["white"],"Y",96,False)
    cyl("STATUS_LED",(.070,.49,.47),.014,.012,M["green"],"Y",24,False)
    m12_connector(M, (0, -.20, .39), "Y", "M12")
    box("ULTRASONIC_target",(0,1.35,.43),(.55,.04,.55),M["white"],.010,False)


def laser_sensor(M):
    box("MOUNT_bracket",(-.18,-.12,.30),(.10,.12,.58),M["zinc"],.008)
    box("MOUNT_foot",(-.02,-.12,.07),(.46,.32,.10),M["zinc"],.008)
    box("LASER_body", (0, .06, .38), (.30, .40, .52), M["black"], .020)
    box("OPTICS_bezel", (0, .275, .38), (.21, .018, .31), M["glass"], .006, False)
    box("LASER_window", (-.035, .287, .40), (.072,.008,.075), M["red"], .003, False)
    cyl("RECEIVER_lens", (.055, .287, .40), .024, .010, M["black"], "Y", 48, False)
    for i,z in enumerate((.23,.29,.47,.53)):cyl(f"STATUS_{i}",(.105,.287,z),.009,.008,M["green"],"Y",20,False)
    m12_connector(M,(0,-.20,.26),"Y","M12")
    box("TARGET_plate", (0, 1.05, .38), (.58, .035, .58), M["white"], .012, False)
    beam = mat("Laser beam", (1, .01, .005), .01, .10, .28)
    tube_between("LASER_beam", (-.06, .28, .40), (-.06, 1.02, .40), .010, beam, False)
    for i,z in enumerate((.18,.28,.38,.48,.58)):box(f"TARGET_scale_{i}",(.20,1.075,z),(.11,.010,.012),M["black"],.001,False)


def limit_switch(M):
    box("MOUNT_bracket",(-.24,-.12,.31),(.10,.12,.62),M["zinc"],.008)
    box("MOUNT_foot",(-.02,-.12,.06),(.48,.30,.10),M["zinc"],.008)
    box("LIMIT_body", (0, .06, .34), (.34, .34, .44), M["black"], .020)
    box("LIMIT_head", (0, .06, .62), (.26, .29, .16), M["zinc"], .016)
    tube_between("LEVER_arm", (0, .16, .72), (.38, .22, 1.12), .030, M["zinc"])
    cyl("LEVER_roller", (.42, .22, 1.16), .085, .16, M["black"], "Y", 48)
    cyl("LEVER_pivot", (0, .20, .72), .065, .06, M["zinc"], "Y", 48,False)
    m12_connector(M, (0, -.15, .18), "Y", "M12")


def tongue_interlock(M):
    box("SWITCH_body", (-.18, 0, .38), (.34, .30, .70), M["yellow"], .040)
    box("SWITCH_face", (-.18, .17, .38), (.26, .045, .58), M["black"], .018, False)
    box("KEY_slot", (-.18, .20, .58), (.12, .025, .06), M["red"], .004, False)
    box("GUARD_edge", (.25, -.02, .38), (.16, .22, .76), M["zinc"], .006)
    box("ACTUATOR_tongue", (.07, .16, .58), (.36, .05, .10), M["stainless"], .006)
    box("ACTUATOR_fork", (.22, .16, .58), (.08, .05, .24), M["stainless"], .004)
    m12_connector(M, (-.18, -.18, .15), "Y", "M12")


def coded_safety_switch(M):
    # A coded magnetic interlock is a close paired sensor/actuator at a guard
    # seam.  The previous four-post assembly concealed that relationship and
    # was blind-read as generic guarding.  Keep only the fixed frame, moving
    # door edge, visibly separated matched heads, and their cable connection.
    # PSENmag-style switches are small matching heads mounted immediately
    # across a guard seam, rather than large labelled blocks on a mock guard.
    box("FIXED_GUARD_FRAME", (-.20, -.05, .52), (.12, .20, 1.04), M["steel"], .008)
    box("MOVING_GUARD_EDGE", (.20, -.05, .52), (.12, .20, 1.04), M["blue"], .008)
    box("SENSOR_mount", (-.115, -.045, .50), (.10, .055, .34), M["zinc"], .005)
    box("ACTUATOR_mount", (.115, -.045, .50), (.10, .055, .34), M["zinc"], .005)
    box("CODED_SENSOR_body", (-.065, .050, .50), (.095, .145, .235), M["black"], .014)
    box("CODED_ACTUATOR_body", (.065, .050, .50), (.095, .145, .235), M["black"], .014)
    # A subtle molded end cap and one indicator retain the physical cues
    # without pretending to reproduce a vendor label or safety rating.
    box("SENSOR_end_cap", (-.065, .050, .626), (.079, .132, .018), M["zinc"], .004, False)
    box("ACTUATOR_end_cap", (.065, .050, .626), (.079, .132, .018), M["zinc"], .004, False)
    cyl("SAFETY_LED",(-.065,.126,.555),.011,.008,M["green"],"Y",20,False)
    for x, side in ((-.115, "sensor"), (.115, "actuator")):
        for z in (.385, .615):
            cyl(f"{side}_mount_fastener_{z}",(x,-.079,z),.012,.012,M["black"],"Y",20,False)
    # The deliberate narrow gap shows the non-contact paired relationship.
    box("CLOSED_POSITION_GAP", (0, .128, .50), (.025, .020, .205), M["white"], .001, False)
    m12_connector(M, (-.065, -.115, .50), "Y", "SENSOR_M12")
    tube_between("SENSOR_cable",(-.065,-.185,.50),(-.30,-.52,.16),.014,M["black"],False)


def safety_scanner(M):
    # Compact floor scanner family: a low body with a distinct upper optical
    # cover, front display/keypad, rear system interface, and real mounting
    # ears.  The field lines remain explanatory artwork, never a safety design.
    box("SCANNER_mount_plate", (0, -.02, .035), (.64, .46, .07), M["zinc"], .010)
    for x in (-.24,.24):
        cyl(f"SCANNER_mount_{x}",(x,-.13,.075),.032,.045,M["black"],"Z",24,False)
    box("SCANNER_body", (0, 0, .22), (.56, .42, .30), M["yellow"], .042)
    cyl("OPTICS_cover", (0, .105, .36), .17, .055, M["black"], "Y", 96, False)
    box("DISPLAY_bezel", (0, .225, .205), (.28, .020, .115), M["black"], .007, False)
    box("DISPLAY", (-.045, .238, .215), (.13, .008, .050), M["glass"], .004, False)
    for x,z in ((.075,.235),(.120,.235),(.075,.185),(.120,.185)):
        cyl(f"KEY_{x}_{z}",(x,.240,z),.016,.008,M["zinc"],"Y",20,False)
    box("SYSTEM_plug", (0, -.245, .20), (.23, .055, .13), M["black"], .018)
    m12_connector(M, (0, -.315, .20), "Y", "SYSTEM_M12")
    scan = mat("Safety scan field", (1, .015, .005), .01, .12, .16)
    for angle in range(-60, 61, 12):
        a = math.radians(angle)
        tube_between(f"SCAN_ray_{angle}", (0, .16, .36), (1.5 * math.sin(a), .16 + 1.5 * math.cos(a), .08), .004, scan, False)


def rfid_head(M):
    # Compact square RFID head family. Make the scan face, status LEDs, rear
    # connector, and mounting relationship visible rather than presenting it
    # as a generic screen on a block.
    box("READER_mount",(-.55,-.10,.42),(.52,.08,.62),M["zinc"],.008)
    box("RFID_head", (-.55, .06, .42), (.40, .20, .40), M["blue"], .035)
    box("RFID_face", (-.55, .175, .42), (.34, .020, .34), M["black"], .012, False)
    for size in (.10,.15):
        box(f"RFID_loop_top_{size}",(-.55,.267,.42+size/2),(size*2,.010,.012),M["white"],.001,False);box(f"RFID_loop_bottom_{size}",(-.55,.267,.42-size/2),(size*2,.010,.012),M["white"],.001,False)
        box(f"RFID_loop_left_{size}",(-.55-size,.267,.42),(.012,.010,size),M["white"],.001,False);box(f"RFID_loop_right_{size}",(-.55+size,.267,.42),(.012,.010,size),M["white"],.001,False)
    box("TAGGED_TOTE", (.55, .02, .30), (.46, .38, .42), M["yellow"], .035, False)
    box("RFID_tag", (.55, .225, .30), (.22, .025, .24), M["white"], .016, False)
    torus("RFID_tag_coil", (.55, .246, .30), .065, .010, M["zinc"], "Y", False)
    for x, material in ((-.62,M["yellow"]),(-.48,M["green"])):
        cyl(f"RFID_status_{x}",(x,.192,.56),.017,.008,material,"Y",20,False)
    m12_connector(M, (-.55, -.12, .42), "Y", "REAR_M12")
    tube_between("RFID_reader_cable",(-.55,-.19,.42),(-.95,-.65,.16),.020,M["black"],False)


def encoder(M):
    box("ENCODER_mount", (0, -.10, .30), (.55, .10, .55), M["zinc"], .008)
    cyl("ENCODER_housing", (0, .08, .34), .22, .34, M["blue"], "Y", 96)
    cyl("ENCODER_flange", (0, .27, .34), .27, .08, M["zinc"], "Y", 96)
    cyl("ENCODER_shaft", (0, .39, .34), .065, .22, M["stainless"], "Y", 64)
    for a in range(0, 360, 90):
        x=.20*math.cos(math.radians(a));z=.34+.20*math.sin(math.radians(a));cyl(f"FLANGE_bolt_{a}",(x,.32,z),.018,.025,M["black"],"Y",20)
    m12_connector(M, (.18, -.13, .16), "Y", "M12")
    torus("ENCODER_scale_ring",(0,.325,.34),.16,.010,M["black"],"Y",False)
    text_label("ENCODER_label","ENC",(-.10,.335,.52),.050,M["white"])


def pressure_transmitter(M):
    box("MOUNT_bracket",(-.18,-.13,.38),(.10,.12,.72),M["zinc"],.008)
    box("MOUNT_foot",(-.02,-.13,.06),(.46,.34,.10),M["zinc"],.008)
    cyl("PROCESS_thread",(0,.03,.16),.060,.24,M["brass"],"Z",64)
    for z in (.07,.11,.15,.19,.23):torus(f"PROCESS_thread_{z}",(0,.03,z),.061,.004,M["zinc"],"Z",False)
    cyl("HEX_process_fitting",(0,.03,.31),.115,.11,M["stainless"],"Z",6)
    cyl("SENSOR_neck",(0,.03,.41),.060,.12,M["stainless"],"Z",64)
    # A compact cylindrical body and top electrical entry match the documented
    # general-industrial transmitter family without copying a vendor's label,
    # range, connector pinout, or display layout.
    cyl("TRANSMITTER_housing", (0, .03, .63), .135, .34, M["stainless"], "Z", 96)
    torus("HOUSING_collar", (0,.03,.48), .136,.010,M["black"],"Z",False)
    m12_connector(M, (0, .03, .83), "Z", "FIELD_M12")
    tube_between("FIELD_cable",(0,.03,.90),(.15,-.10,1.03),.016,M["black"],False)


def pressure_gauge(M):
    # Neutral bottom-connected dial gauge: a real form needs a case, bezel,
    # face, pointer, and short threaded process fitting—not a pipe spool.
    cyl("PROCESS_thread", (0, 0, .18), .060, .26, M["brass"], "Z", 64)
    for z in (.08,.12,.16,.20,.24):torus(f"THREAD_{z}",(0,0,z),.061,.004,M["zinc"],"Z",False)
    cyl("HEX_process_fitting", (0, 0, .34), .105, .10, M["stainless"], "Z", 6)
    cyl("PROCESS_stem", (0, 0, .45), .055, .15, M["stainless"], "Z", 64)
    cyl("GAUGE_case", (0, 0, .78), .34, .15, M["black"], "Y", 128)
    cyl("GAUGE_bezel", (0,.086,.78), .315,.045,M["stainless"],"Y",128,False)
    cyl("GAUGE_dial", (0, .112, .78), .285, .018, M["white"], "Y", 128, False)
    for a in range(-120, 121, 20):
        angle=math.radians(a);x=.23*math.sin(angle);z=.78+.23*math.cos(angle)
        tick=box(f"TICK_{a}",(x,.128,z),(.016,.008,.050),M["black"],.001,False);tick.rotation_euler.y=-angle
    tube_between("POINTER", (0, .130, .78), (.15, .130, .91), .010, M["red"], False)
    cyl("POINTER_hub", (0, .134, .78), .030, .016, M["black"], "Y", 32, False)


def rtd_probe(M):
    cyl("RTD_probe", (0, 0, .36), .025, .72, M["stainless"], "Z", 48)
    cyl("COMPRESSION_fitting", (0, 0, .73), .10, .16, M["brass"], "Z", 12)
    cyl("HEAD_neck", (0, 0, .88), .06, .18, M["stainless"], "Z", 48)
    cyl("CONNECTION_head", (0, 0, 1.06), .22, .30, M["blue"], "Z", 96)
    cyl("HEAD_cap", (0, 0, 1.23), .23, .06, M["black"], "Z", 96)
    m12_connector(M, (.20, 0, 1.05), "X", "M12")
    cyl("PROCESS_pipe",(0,0,.22),.18,1.10,M["stainless"],"X",96)
    box("TEMP_display",(0,.218,1.06),(.30,.025,.16),M["black"],.008,False)
    text_label("TEMP_label","125 C",(0,.238,1.06),.055,M["red"])
    text_label("PIPE_TEMP_label","TEMPERATURE",(0,.19,.18),.040,M["blue"])


def flow_sensor(M):
    cyl("PIPE_spool", (0, 0, .34), .18, 1.55, M["stainless"], "X", 96)
    # Flat flanges and bolt pattern read as actual inline process connections,
    # rather than the decorative rings used by the rejected first review.
    for x in (-.64,.64):
        cyl(f"PIPE_flange_{x}",(x,0,.34),.27,.055,M["zinc"],"X",64)
        for a in range(0,360,90):
            y=.21*math.cos(math.radians(a)); z=.34+.21*math.sin(math.radians(a))
            cyl(f"FLANGE_bolt_{x}_{a}",(x+.035,y,z),.018,.07,M["black"],"X",20)
    # Short continuation stubs make the flanged spool read as an installed
    # inline component, not a pipe with blank end plates.
    for x in (-.92,.92):cyl(f"MATING_PIPE_{x}",(x,0,.34),.18,.52,M["stainless"],"X",96)
    # A long, plain probe enters through a compression fitting.  The sensing
    # elements are internal to real thermal probes, so external fins would be
    # a made-up visual cue rather than a credible process interface.
    cyl("INSERTION_probe", (0, 0, .67), .035, .52, M["stainless"], "Z", 48)
    cyl("COMPRESSION_fitting", (0, 0, .78), .105, .14, M["brass"], "Z", 12)
    torus("COMPRESSION_nut", (0, 0, .84), .106, .010, M["zinc"], "Z", False)
    cyl("FLOW_body", (0, 0, .98), .20, .34, M["blue"], "Z", 96)
    box("FLOW_display", (0, .195, 1.01), (.32, .025, .16), M["black"], .008, False)
    m12_connector(M, (0, 0, 1.19), "Z", "M12")
    text_label("FLOW_units","THERMAL",(0,.214,1.01),.075,M["green"])
    for x in (-.38,.02):
        box(f"FLOW_arrow_shaft_{x}",(x,.19,.34),(.26,.012,.035),M["blue"],.002,False)
        bpy.ops.mesh.primitive_cone_add(vertices=3, radius1=.09, radius2=0, depth=.18,
            location=(x+.20,.19,.34), rotation=(0,math.pi/2,0))
        finish(bpy.context.object,f"FLOW_arrow_head_{x}",M["blue"],.001,False,False)


def vibration_sensor(M):
    box("BEARING_base", (0, 0, .08), (1.10, .75, .16), M["steel"], .015)
    box("PILLOW_BLOCK",(0,0,.40),(.62,.46,.48),M["zinc"],.08)
    cyl("BEARING_bore",(0,.25,.42),.16,.035,M["black"],"Y",64,False)
    cyl("MACHINE_shaft",(0,0,.42),.105,.90,M["stainless"],"Y",64)
    # Based on the compact cylindrical/stud-mounted transmitter family, not a
    # generic blue cube.  The sensor keeps its physical mounting and connector
    # cues without copying a vendor mark, dimension, performance, or approval.
    cyl("MOUNTING_stud", (.24, 0, .58), .045, .14, M["stainless"], "Z", 48)
    cyl("SENSOR_wrench_base", (.24, 0, .68), .125, .13, M["zinc"], "Z", 6)
    cyl("SENSOR_body", (.24, 0, .82), .105, .22, M["black"], "Z", 96)
    torus("SENSOR_status_ring", (.24, 0, .89), .107, .012, M["green"], "Z", False)
    m12_connector(M, (.24, 0, .98), "Z", "M12")
    text_label("VIB_label","VIB",(.24,.112,.82),.040,M["white"])


def load_cell(M):
    beam=box("LOAD_CELL_beam", (0, 0, .24), (1.30, .34, .30), M["stainless"], .035)
    cut_cyl(beam,"STRAIN_GAUGE_bore",(0,0,.24),.12,.50,"Y",96)
    torus("STRAIN_GAUGE_seal",(0,.20,.24),.12,.014,M["zinc"],"Y",False)
    for y in (-.10,.10):cut_cyl(beam,f"FIXED_mount_hole_{y}",(-.46,y,.24),.055,.44,"Z",48)
    cut_cyl(beam,"LOAD_thread",(.46,0,.24),.065,.44,"Z",64)
    box("LOAD_CELL_nameplate", (.28, .20, .24), (.30, .025, .14), M["blue"], .008, False)
    text_label("LOAD_label","500 kg C3",(.28,.218,.24),.032,M["white"])
    m12_connector(M, (-.68, 0, .24), "X", "CABLE")
    tube_between("SIGNAL_cable",(-.74,0,.24),(-1.20,-.20,.12),.025,M["black"],False)


def io_link_master(M):
    box("IOLINK_body", (0, 0, .45), (.78, .30, .82), M["amber"], .045)
    box("IOLINK_face", (0, .17, .45), (.68, .035, .72), M["black"], .018, False)
    for row,z in enumerate((.25,.43,.61,.79)):
        for col,x in enumerate((-.18,.18)):
            cyl(f"M12_PORT_{row}_{col}",(x,.205,z),.060,.035,M["zinc"],"Y",48,False)
            cyl(f"PORT_core_{row}_{col}",(x,.228,z),.043,.012,M["black"],"Y",48,False)
            for a in range(0,360,90):
                px=x+.020*math.cos(math.radians(a));pz=z+.020*math.sin(math.radians(a))
                cyl(f"PORT_PIN_{row}_{col}_{a}",(px,.239,pz),.005,.008,M["brass"],"Y",12,False)
            text_label(f"PORT_label_{row}_{col}",str(row*2+col+1),(x+.09,.238,z),.025,M["white"])
    for x in (-.20,0,.20):cyl(f"STATUS_{x}",(x,.225,.88),.018,.012,M["green" if x<.2 else "red"],"Y",24,False)
    m12_connector(M, (-.22, 0, .01), "Z", "POWER")
    m12_connector(M, (.22, 0, .01), "Z", "ETHERNET")
    text_label("IOLINK_label","IO-LINK MASTER",(0,.229,.93),.038,M["white"])
    tube_between("ETHERNET_cable",(.22,0,-.05),(.55,-.25,-.30),.030,M["green"],False)
    for x,z,dx in ((-.18,.79,-.55),(.18,.61,.55),(-.18,.43,-.65)):
        cyl(f"PLUG_{x}_{z}",(x,.28,z),.052,.12,M["black"],"Y",32,False)
        tube_between(f"DEVICE_cable_{x}_{z}",(x,.34,z),(dx,.82,z+.10),.025,M["black"],False)


def three_button_station(M):
    box("STATION_enclosure", (0, 0, .45), (.48, .30, .82), M["yellow"], .055)
    box("STATION_face", (0, .17, .45), (.38, .035, .70), M["black"], .018, False)
    for z, material, name in ((.68,M["green"],"START"),(.45,M["amber"],"RESET"),(.22,M["red"],"STOP")):
        cyl(f"{name}_bezel",(0,.205,z),.095,.045,M["zinc"],"Y",64,False)
        cyl(f"{name}_button",(0,.235,z),.070,.055,material,"Y",64,False)
    m12_connector(M, (0, 0, .01), "Z", "CABLE")


def foot_switch(M):
    box("FOOT_base", (0, 0, .08), (.60, .76, .16), M["black"], .050)
    pedal=box("PEDAL",(0,.16,.20),(.38,.48,.10),M["red"],.025);pedal.rotation_euler.x=math.radians(-18)
    for y in (.04,.14,.24,.34):box(f"PEDAL_tread_{y}",(0,y,.27),(.32,.025,.018),M["black"],.002,False)
    # Open U-shaped guard with a visibly recessed toe-operated pedal.
    box("GUARD_top",(0,-.20,.55),(.70,.26,.11),M["yellow"],.045)
    for x in (-.30,.30):box(f"GUARD_side_{x}",(x,-.01,.34),(.10,.62,.52),M["yellow"],.035)
    cyl("PEDAL_hinge",(0,-.10,.22),.050,.45,M["zinc"],"X",48)
    m12_connector(M, (0, -.42, .10), "Y", "CABLE")
    text_label("FOOT_label","FOOT SWITCH",(0,.44,.08),.045,M["yellow"],rotation=(0,0,0))


def rope_pull_estop(M):
    box("ROPE_SWITCH_body", (0, 0, .52), (.62, .40, .64), M["red"], .060)
    box("ROPE_SWITCH_face", (0, .225, .52), (.50, .045, .50), M["black"], .018, False)
    cyl("RESET_mushroom", (0, .275, .56), .10, .08, M["red"], "Y", 64, False)
    for x in (-.42,.42):
        cyl(f"ROPE_eye_{x}",(x,0,.52),.075,.18,M["zinc"],"X",48)
        tube_between(f"PULL_rope_{x}",(x,0,.52),(x*3.3,0,.52),.014,M["red"],False)
        for j in range(1,4):torus(f"ROPE_marker_{x}_{j}",(x*(1+j*.55),0,.52),.035,.010,M["yellow"],"X",False)
    box("MOUNT_bracket",(0,-.24,.35),(.80,.10,.85),M["zinc"],.008)
    text_label("ESTOP_label","E-STOP",(0,.254,.35),.070,M["white"])


def safety_mat(M):
    box("MAT_rubber", (0, 0, .018), (2.0, 1.20, .036), M["rubber"], .018)
    for x in (-.97,.97):box(f"MAT_border_x_{x}",(x,0,.042),(.06,1.20,.018),M["yellow"],.004,False)
    for y in (-.57,.57):box(f"MAT_border_y_{y}",(0,y,.042),(1.94,.06,.018),M["yellow"],.004,False)
    for y in (-.42,-.28,-.14,0,.14,.28,.42):box(f"MAT_rib_{y}",(0,y,.045),(1.80,.018,.012),M["black"],.001,False)
    box("MAT_connector",(1.10,0,.07),(.22,.22,.12),M["black"],.025)
    tube_between("MAT_cable",(1.20,0,.07),(1.70,-.25,.07),.028,M["black"],False)
    text_label("MAT_label","PRESSURE-SENSITIVE SAFETY MAT",(0,.01,.055),.090,M["yellow"],rotation=(0,0,0))


def five_tier_stacklight(M):
    box("LIGHT_base", (0, 0, .08), (.48, .48, .16), M["black"], .035)
    cyl("LIGHT_pole", (0, 0, .68), .045, 1.10, M["zinc"], "Z", 48)
    cyl("LIGHT_housing", (0, 0, 1.42), .18, .22, M["black"], "Z", 96)
    colors=(M["green"],M["white"],M["amber"],M["blue"],M["red"])
    for i,material in enumerate(colors):
        z=1.55+i*.20;cyl(f"LENS_{i}",(0,0,z),.18,.16,material,"Z",96,False);torus(f"SEPARATOR_{i}",(0,0,z-.09),.18,.018,M["black"])
    cyl("SOUNDER", (0, 0, 2.62), .23, .34, M["black"], "Z", 96)
    for i in range(8):
        a=math.radians(i*45);box(f"SOUNDER_slot_{i}",(.18*math.cos(a),.18*math.sin(a),2.62),(.035,.08,.20),M["white"],.002,False)


BUILDERS = {
    "inductive_proximity_sensor_m18": inductive_sensor,
    "capacitive_proximity_sensor_m30": capacitive_sensor,
    "diffuse_photoelectric_sensor": diffuse_photoeye,
    "through_beam_photoelectric_pair": through_beam_pair,
    "ultrasonic_distance_sensor": ultrasonic_sensor,
    "laser_distance_sensor": laser_sensor,
    "roller_lever_limit_switch": limit_switch,
    "tongue_safety_interlock": tongue_interlock,
    "coded_magnetic_safety_switch": coded_safety_switch,
    "safety_laser_scanner": safety_scanner,
    "rfid_read_write_head": rfid_head,
    "incremental_rotary_encoder": encoder,
    "pressure_transmitter": pressure_transmitter,
    "analog_pressure_gauge": pressure_gauge,
    "rtd_temperature_probe": rtd_probe,
    "thermal_flow_sensor": flow_sensor,
    "piezo_vibration_sensor": vibration_sensor,
    "shear_beam_load_cell": load_cell,
    "eight_port_io_link_master": io_link_master,
    "three_button_control_station": three_button_station,
    "guarded_foot_switch": foot_switch,
    "rope_pull_estop_station": rope_pull_estop,
    "industrial_safety_mat": safety_mat,
    "five_tier_stacklight_siren": five_tier_stacklight,
}


def point(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def save(slug, builder):
    clean()
    materials = common()
    builder(materials)
    root = BASE / slug
    for path in (root / "source", root / "delivery", root / "collision", root / "review"):
        path.mkdir(parents=True, exist_ok=True)
    for path in (root / "source", root / "review"):
        (path / ".gdignore").touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root / "source" / f"{slug}.blend"))
    objects = [obj for obj in bpy.context.scene.objects if obj.get("rungproof_asset")]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(filepath=str(root / "delivery" / f"{slug}.glb"), export_format="GLB", use_selection=True, export_apply=True)
    collision = [obj for obj in objects if obj.get("rungproof_collision", True)]
    mins, maxs = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    for obj in collision:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            mins.x, mins.y, mins.z = min(mins.x, world.x), min(mins.y, world.y), min(mins.z, world.z)
            maxs.x, maxs.y, maxs.z = max(maxs.x, world.x), max(maxs.y, world.y), max(maxs.z, world.z)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_cube_add(location=(mins + maxs) / 2)
    proxy = bpy.context.object
    proxy.name = "COLLISION_primary"
    proxy.dimensions = maxs - mins
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.export_scene.gltf(filepath=str(root / "collision" / f"{slug}_collision.glb"), export_format="GLB", use_selection=True, export_apply=True)
    bpy.data.objects.remove(proxy, do_unlink=True)
    visual_mins, visual_maxs = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            visual_mins.x, visual_mins.y, visual_mins.z = min(visual_mins.x, world.x), min(visual_mins.y, world.y), min(visual_mins.z, world.z)
            visual_maxs.x, visual_maxs.y, visual_maxs.z = max(visual_maxs.x, world.x), max(visual_maxs.y, world.y), max(visual_maxs.z, world.z)
    floor = box("REVIEW_floor", (0, 0, mins.z - .035), (max(4, visual_maxs.x-visual_mins.x+1.5), max(4, visual_maxs.y-visual_mins.y+1.5), .05), materials["black"], .002, False)
    floor["rungproof_asset"] = False
    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.color = (.025, .035, .04)
    center, span = (visual_mins + visual_maxs) / 2, visual_maxs - visual_mins
    framing_multiplier = 1.48 if slug == "five_tier_stacklight_siren" else 1.25
    radius = max(span.length * framing_multiplier, 2.1)
    for i, (location, energy, size) in enumerate((((4,-4,6),1150,4),((-3,-1,3),650,3),((0,4,4),800,3))):
        data=bpy.data.lights.new(f"REVIEW_light_{i}","AREA");data.energy=energy;data.shape="DISK";data.size=size
        light=bpy.data.objects.new(data.name,data);light.location=location;bpy.context.collection.objects.link(light);point(light,center)
    camera_data=bpy.data.cameras.new("REVIEW_camera");camera=bpy.data.objects.new("REVIEW_camera",camera_data);bpy.context.collection.objects.link(camera);bpy.context.scene.camera=camera;camera_data.lens=55
    scene=bpy.context.scene;scene.render.engine="BLENDER_EEVEE";scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format="PNG"
    elevation=.62 if slug in {"industrial_safety_mat"} else .48
    for index, angle in enumerate((35,125,215,305)):
        a=math.radians(angle);camera.location=(center.x+radius*math.cos(a),center.y+radius*math.sin(a),center.z+radius*elevation);point(camera,center)
        scene.render.filepath=str(root/"review"/f"{slug}_{index+1:02d}.png");bpy.ops.render.render(write_still=True)
    primary_review = (root / "review" / f"{slug}_01.png").read_bytes()
    (root / "thumbnail.png").write_bytes(primary_review)
    # Keep blind-review evidence synchronized with the delivery GLB.  A stale
    # blind_review.png can otherwise make a remodel look unreviewed or let an
    # old, rejected form be mistaken for current evidence.
    (root / "review" / "blind_review.png").write_bytes(primary_review)
    BUILT.append(slug)


asset_filter={value.strip() for value in os.environ.get("RUNGPROOF_ASSET_FILTER","").split(",") if value.strip()}
for asset_slug, asset_builder in BUILDERS.items():
    if not asset_filter or asset_slug in asset_filter:
        save(asset_slug, asset_builder)
print("CONTROLS_SENSORS_ASSETS_BUILT", len(BUILT))
