from pathlib import Path
import json,struct
ids=['training.accessory.package_class_result_display.v1','training.accessory.four_lane_diverter.v1','training.accessory.destination_conveyor_bank.v1','training.accessory.count_display.v1']
cat=json.loads(Path('assets/catalog/candidates.catalog.json').read_text());report=[]
for aid in ids:
 a=next(a for a in cat['assets'] if a['id']==aid);p=Path(a['model']['deliveryGltf'].removeprefix('res://'));b=p.read_bytes();length,kind=struct.unpack_from('<II',b,12);assert kind==0x4e4f534a;d=json.loads(b[20:20+length]);names=[n.get('name','') for n in d.get('nodes',[]) if 'mesh' in n]
 r=dict(assetId=aid,delivery=str(p),meshNames=names);report.append(r);print(aid+': '+', '.join(names[:14])+f' ({len(names)} meshes)')
Path('../docs/VISION_SORTER_ASSET_IDENTITY.json').write_text(json.dumps(report,indent=2)+'\n')
