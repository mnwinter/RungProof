"""Original scoped grouping-line and guided-stop models; metres, no OEM data."""
import os, math, importlib.util
from pathlib import Path
import bpy

ROOT = Path(os.environ['RUNGPROOF_PROJECT_ROOT'])
os.environ['RUNGPROOF_ASSET_FILTER'] = '__helpers_only__'
spec = importlib.util.spec_from_file_location('flow', Path(__file__).with_name('build_material_flow_assets.py'))
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
BASE = ROOT / 'assets/scene_installations/package_grouping'

def line(M):
    # Blender X is travel, Z height; Blender Y becomes Godot -Z.
    for zone, start, end, feet, motor in (
        ('INFEED', -5, 3.2, [-4.5, -1.5, 1.5, 2.8], -4.7),
        ('RECEIVER', 3.2, 7, [3.6, 6.5], 6.5)):
        for side in (-1, 1):
            flow.box(f'{zone}_rail_{side}', ((start+end)/2, side*.78, .74), (end-start,.10,.16), M['blue'], .01)
            for x in feet:
                flow.box(f'{zone}_foot_{side}_{x}', (x,side*.76,.04), (.34,.34,.08), M['steel'], .01)
                flow.box(f'{zone}_leg_{side}_{x}', (x,side*.76,.42), (.10,.10,.68), M['blue'], .01)
        for x in feet:
            flow.box(f'{zone}_brace_{x}', (x,0,.52), (.08,1.52,.08), M['steel'], .01)
        count = math.ceil((end-start)/.18)
        pitch = (end-start)/count
        for i in range(count):
            x = start + pitch*(i+.5)
            flow.cyl(f'KIN_group_roller_{zone}_{i}', (x,0,.82), .08,1.44,M['alum'],'Y')
        flow.cyl(f'{zone}_motor', (motor,1.06,.82), .16,.38,M['blue'],'Y')
        flow.cyl(f'{zone}_drive_shaft', (motor,.86,.82), .045,.28,M['steel'],'Y')
    flow.box('TRANSFER_bridge', (3.2,0,.895), (.07,1.44,.01), M['steel'], .001, False)

def stop(M):
    for side in (-1,1):
        flow.box(f'STOP_foot_{side}', (0,side*1.15,.04), (.30,.24,.08), M['steel'], .01)
        flow.box(f'STOP_guide_{side}', (0,side*1.15,1.32), (.10,.10,2.48), M['blue'], .01)
    flow.box('STOP_header',(0,0,2.60),(.24,2.4,.08),M['steel'],.01)
    flow.box('STOP_actuator',(0,0,2.84),(.20,.20,.40),M['blue'],.015)
    flow.cyl('STOP_telescoping_rod',(0,0,1.905),.035,1.31,M['steel'],'Z')
    flow.box('KIN_group_gate',(0,0,1.10),(.08,2.22,.30),M['yellow'],.01)
    # The rod's upper face stays at 2.56 m while its lower end follows the gate.
    flow.text('STOP_label','GROUP STOP',(0,-1.22,2.6),.10,M['black'],(math.pi/2,0,0))

for slug, builder in (('line',line),('stop',stop)):
    flow.clean(); builder(flow.common()); root=BASE/slug
    (root/'source').mkdir(parents=True,exist_ok=True); (root/'delivery').mkdir(exist_ok=True)
    (root/'source/.gdignore').touch()
    bpy.ops.wm.save_as_mainfile(filepath=str(root/'source'/f'{slug}.blend'))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=str(root/'delivery'/f'grouping_{slug}.glb'),export_format='GLB',use_selection=True,export_apply=True)
print('GROUPING_INSTALLATION_BUILT two original source and delivery assets')
