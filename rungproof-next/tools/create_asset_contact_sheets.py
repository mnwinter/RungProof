"""Create labeled thumbnail contact sheets for one asset source tree."""
from __future__ import annotations
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

def main() -> None:
    parser=argparse.ArgumentParser();parser.add_argument("--source",type=Path,required=True);parser.add_argument("--output",type=Path,required=True);args=parser.parse_args()
    files=sorted(args.source.resolve().glob("*/thumbnail.png"));output=args.output.resolve();output.mkdir(parents=True,exist_ok=True)
    thumb=(400,400);label_h=44;cols=4;rows=2;font=ImageFont.load_default(size=18)
    for page,start in enumerate(range(0,len(files),cols*rows),1):
        sheet=Image.new("RGB",(cols*thumb[0],rows*(thumb[1]+label_h)),(22,25,29));draw=ImageDraw.Draw(sheet)
        for slot,path in enumerate(files[start:start+cols*rows]):
            im=Image.open(path).convert("RGB");im.thumbnail(thumb,Image.Resampling.LANCZOS);col,row=slot%cols,slot//cols;x=col*thumb[0]+(thumb[0]-im.width)//2;y=row*(thumb[1]+label_h);sheet.paste(im,(x,y));draw.text((col*thumb[0]+8,y+thumb[1]+8),path.parent.name,fill=(235,238,241),font=font)
        target=output/f"assets-{page:02d}.png";sheet.save(target,optimize=True);print(target)
    print("CONTACT_SHEET_ASSETS",len(files))

if __name__=="__main__":main()
