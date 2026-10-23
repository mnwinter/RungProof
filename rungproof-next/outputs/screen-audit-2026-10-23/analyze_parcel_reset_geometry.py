"""Read-only static GLB/bounds and five-contact counterexample; never starts an app."""
from pathlib import Path
import hashlib
import itertools
import json
import math
import struct

ROOT=Path(__file__).resolve().parents[2]
QA=Path(__file__).resolve().parent

def identity():
    return [[1.0 if i==j else 0.0 for j in range(4)] for i in range(4)]
def multiply(a,b):
    return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def transform(m,p):
    return tuple(sum(m[i][j]*([*p,1.0][j]) for j in range(4)) for i in range(3))
def node_matrix(n):
    if 'matrix' in n:
        a=n['matrix'];return [[a[j*4+i] for j in range(4)] for i in range(4)]
    x,y,z,w=n.get('rotation',[0,0,0,1]); sx,sy,sz=n.get('scale',[1,1,1]);tx,ty,tz=n.get('translation',[0,0,0])
    return [[(1-2*y*y-2*z*z)*sx,(2*x*y-2*z*w)*sy,(2*x*z+2*y*w)*sz,tx],
            [(2*x*y+2*z*w)*sx,(1-2*x*x-2*z*z)*sy,(2*y*z-2*x*w)*sz,ty],
            [(2*x*z-2*y*w)*sx,(2*y*z+2*x*w)*sy,(1-2*x*x-2*y*y)*sz,tz],[0,0,0,1]]
def glb_bounds(path):
    raw=path.read_bytes();magic,version,length=struct.unpack_from('<III',raw)
    if magic!=0x46546c67 or version!=2 or length!=len(raw):raise ValueError('Unexpected GLB header')
    size,kind=struct.unpack_from('<II',raw,12)
    if kind!=0x4e4f534a:raise ValueError('Expected GLB JSON chunk')
    data=json.loads(raw[20:20+size].decode('utf-8').rstrip(' \x00'))
    if len(data.get('nodes',[]))>4096:raise ValueError('Analysis limited to4096 static nodes')
    points=[]; details=[]
    def visit(index,parent,trail):
        if index in trail:raise ValueError('Unexpected cyclic GLB node')
        node=data['nodes'][index]; world=multiply(parent,node_matrix(node)); node_points=[]
        if 'mesh' in node:
            if 'skin' in node:raise ValueError('Skinned geometry is outside this static inspection')
            for primitive in data['meshes'][node['mesh']]['primitives']:
                if primitive.get('targets'):raise ValueError('Morph targets are outside this static inspection')
                position=data['accessors'][primitive['attributes']['POSITION']]
                lo,hi=position['min'],position['max']
                node_points.extend(transform(world,p) for p in itertools.product(*zip(lo,hi)))
            points.extend(node_points)
            lo=[min(p[i] for p in node_points) for i in range(3)];hi=[max(p[i] for p in node_points) for i in range(3)]
            details.append({'name':node.get('name',''), 'min':lo,'max':hi,'center':[(a+b)/2 for a,b in zip(lo,hi)]})
        for child in node.get('children',[]):visit(child,world,trail|{index})
    for root in data['scenes'][data.get('scene',0)]['nodes']:visit(root,identity(),set())
    lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    return {'file':str(path.relative_to(ROOT)).replace('\\','/'),'sha256':hashlib.sha256(raw).hexdigest(),
            'static_node_count':len(data['nodes']),'min':lo,'max':hi,'size':[b-a for a,b in zip(lo,hi)],'meshes':details}

catalog=json.loads((ROOT/'assets/catalog/candidates.catalog.json').read_text(encoding='utf-8-sig'))
assets=catalog['assets']
carton=next(a for a in assets if a['id']=='loads.carton.corrugated-rsc.v1')
portal=next(a for a in assets if a['id']=='sensing.dimensioning.parcel-three-height.v1')
carton_glb=glb_bounds(ROOT/carton['model']['deliveryGltf'].removeprefix('res://'))
portal_glb=glb_bounds(ROOT/portal['model']['deliveryGltf'].removeprefix('res://'))
scene=json.loads((ROOT/'scenes/migrated/lab-2-23-parcel-sorter.scene.json').read_text())
eq={e['id']:e for e in scene['equipment']}
meta=carton['bounds']; divisor=[meta['widthM'],meta['heightM'],meta['depthM']]
geometry=[]
for name,yaw in [('large_parcel',0),('medium_parcel',45),('small_parcel',90)]:
    size=eq[name]['config']['size'];scales=[s/d for s,d in zip(size,divisor)]
    delivered=[s*t for s,t in zip(carton_glb['size'],scales)]
    angle=math.radians(yaw);x,y,z=delivered
    rotated=[abs(math.cos(angle))*x+abs(math.sin(angle))*z,y,abs(math.sin(angle))*x+abs(math.cos(angle))*z]
    geometry.append({'id':name,'configured_size_xyz':size,'static_delivered_size_xyz':delivered,
                     'last_cycle_yaw_degrees':yaw,'reset_sampled_world_aabb_size_if_root_not_restored':rotated,
                     'post_projection_visible_size_xyz':delivered,
                     'note':'Static glTF+catalog scaling expectation; actual Godot bounds were not queried.'})

# Exactly the adapter's five footprint contacts projected into the authored
# supporting rectangles/circles, with belt spans from the existing geometry
# verifier. This is a static mathematical counterexample, NOT a native callback.
supports=[e for e in scene['equipment'] if e['id'] in ['sort_infeed','large_lane','medium_lane','small_lane','medium_transfer']]
def contact_report(length,width,distance=3.3):
    angle=math.pi/4;x=math.cos(angle)*distance;z=-math.sin(angle)*distance;out=[]
    for u,v in [(0,0),(-length/2,-width/2),(-length/2,width/2),(length/2,-width/2),(length/2,width/2)]:
        px=x+u*math.cos(angle)+v*math.sin(angle);pz=z-u*math.sin(angle)+v*math.cos(angle);hits=[]
        for e in supports:
            yaw=math.radians(e.get('rotation',[0,0,0])[1]);cx,_,cz=e['position'];dx,dz=px-cx,pz-cz
            localx=dx*math.cos(yaw)-dz*math.sin(yaw);localz=dx*math.sin(yaw)+dz*math.cos(yaw)
            halfx=e['config']['length']*2.935/6;halfz=e['config']['width']/2
            if abs(localx)<=halfx+.002 and abs(localz)<=halfz+.002:hits.append(e['id'])
        for name,radius in [('sort_turntable_a',1.8),('sort_turntable_b',1.5)]:
            cx,_,cz=eq[name]['position']
            if (px-cx)**2+(pz-cz)**2<=radius**2:hits.append(name)
        out.append({'local':[u,v],'world_xz':[px,pz],'supported_by':hits})
    count=sum(bool(p['supported_by']) for p in out)
    return {'length':length,'width':width,'route_degrees':45,'distance':distance,'contacts':out,'supported_contacts':count,'requires':3,'would_accept':count>=3}
medium=next(e for e in geometry if e['id']=='medium_parcel')
fresh=contact_report(medium['static_delivered_size_xyz'][0],medium['static_delivered_size_xyz'][2])
stale=contact_report(medium['reset_sampled_world_aabb_size_if_root_not_restored'][0],medium['reset_sampled_world_aabb_size_if_root_not_restored'][2])
assert fresh['would_accept'] and not stale['would_accept']
result={'kind':'STATIC_GLTF_AND_AUTHORED_SUPPORT_COUNTEREXAMPLE','nativeProof':False,'applicationStateRead':False,
        'scope':'2 static GLBs,3 declared cartons,2 five-contact medium examples; no app/process/API','carton':carton_glb,'sensor_portal':portal_glb,
        'geometry':geometry,'medium_fresh':fresh,'medium_reset_world_aabb_then_zeroed':stale,
        'limitations':['Godot importer/effective node transforms were not queried','Conveyor rectangles use the existing geometry verifier length factor and configured width, not new native mesh measurements','Does not itself prove the exact first native rejected movement tick']}
path=QA/'parcel-reset-geometry-counterexample.json'
if path.exists():raise FileExistsError('Keep this owned geometry result immutable')
path.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({'carton_static_size':carton_glb['size'],'medium':medium,'fresh_contacts':fresh['supported_contacts'],'stale_contacts':stale['supported_contacts'],'result':path.name,'nativeProof':False}))
