"""Create a read-only review sheet from saved native UI evidence.

Inspect a full frame first; supply a physical ROI grounded in that frame.
This does not operate the app or evaluate simulator state.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw

def rectangle(value):
    coords = tuple(int(part) for part in value.split(','))
    if len(coords) != 4 or coords[2] <= coords[0] or coords[3] <= coords[1]:
        raise argparse.ArgumentTypeError('Use left,top,right,bottom')
    return coords

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('plan', type=Path, help='Saved action plan with name or capture keys')
parser.add_argument('output', type=Path, help='New JPG/PNG sheet; will not overwrite')
parser.add_argument('--physical', type=rectangle, required=True)
parser.add_argument('--points', type=rectangle, default=(1235,145,1595,245))
parser.add_argument('--runtime', type=rectangle, default=(1420,10,1595,65))
args = parser.parse_args()
if args.output.exists() or args.output.with_suffix('.manifest.json').exists():
    raise SystemExit('Output already exists; choose a new evidence name')
plan = json.loads(args.plan.read_text(encoding='utf-8-sig'))
names = [step.get('name',step.get('capture')) for step in plan]
names = [name for name in names if name]
if not names:
    raise SystemExit('Plan contains no named captures')
points_height = args.points[3]-args.points[1]
cell_height = max(230,points_height+110)
sheet = Image.new('RGB',(1600,cell_height*math.ceil(len(names)/2)),'#ddd')
manifest = []
for index,name in enumerate(names):
    source = args.plan.parent/(name+'.png')
    raw = source.read_bytes()
    with Image.open(source) as image:
        image = image.convert('RGB')
        for roi in (args.physical,args.points,args.runtime):
            if not (0<=roi[0]<roi[2]<=image.width and 0<=roi[1]<roi[3]<=image.height):
                raise SystemExit(f'ROI outside {source.name}: {roi}')
        x,y = (index%2)*800,(index//2)*cell_height
        ImageDraw.Draw(sheet).text((x+8,y+8),name,fill='black')
        for roi,position,limit in ((args.physical,(x+8,y+35),(325,175)),
                                   (args.points,(x+350,y+35),(440,points_height)),
                                   (args.runtime,(x+350,y+points_height+50),(440,65))):
            crop = image.crop(roi)
            crop.thumbnail(limit)
            sheet.paste(crop,position)
        manifest.append({'case':name,'source':source.name,'sha256':hashlib.sha256(raw).hexdigest(),
                         'image_size':list(image.size)})
args.output.parent.mkdir(parents=True,exist_ok=True)
sheet.save(args.output,quality=92)
args.output.with_suffix('.manifest.json').write_text(json.dumps({
    'scope':'Derived image pixels only; no app input or pass assertions',
    'physical_roi':args.physical,'points_roi':args.points,'runtime_roi':args.runtime,
    'cases':manifest},indent=2),encoding='utf-8')
print(args.output)
