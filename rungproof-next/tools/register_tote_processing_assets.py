"""Register the reusable tote-processing station family."""
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"
SPECS={
"tote_filling_station":("process.packaging.filler.tote-volumetric.v1","Volumetric Tote Filling Station","process/packaging/filling",[1.9,2.8,1.7],("nozzle_position","linear","KIN_fill_nozzle",0,.12,"m",.35),[("fill_valve_open","bool","input",None,"nozzle_position"),("fill_complete","bool","output",None,None)]),
"tote_capping_station":("process.packaging.capper.tote-inline.v1","Inline Tote Capping Station","process/packaging/capping",[1.9,2.8,1.7],("capper_rotation","continuous","KIN_capper_spindle",0,360,"deg",180),[("capper_run","bool","input",None,"capper_rotation"),("cap_present","bool","output",None,None),("capper_fault","bool","output",None,None)]),
"tote_labeling_station":("process.packaging.labeler.tote-pressure-sensitive.v1","Pressure-Sensitive Tote Labeling Station","process/packaging/labeling",[2.1,2.6,1.8],("label_roll_rotation","continuous","KIN_label_roll",0,360,"deg",55),[("labeler_run","bool","input",None,"label_roll_rotation"),("label_low","bool","output",None,None),("labeler_fault","bool","output",None,None)]),
"tote_vision_inspection_station":("inspection.vision.tote-multicamera.v1","Multi-Camera Tote Vision Inspection Station","inspection/vision/packaging",[2.1,2.7,1.9],("camera_scan","rotary","KIN_vision_lens",-4,4,"deg",8),[("inspection_run","bool","input",None,"camera_scan"),("inspection_ok","bool","output",None,None),("vision_fault","bool","output",None,None)]),
}
def main():
    doc=json.loads(CATALOG.read_text(encoding="utf-8"));by={a["id"]:a for a in doc["assets"]}
    for slug,(aid,name,category,bounds,axis,signals) in SPECS.items():
        i,k,n,lo,hi,u,r=axis
        by[aid]={"id":aid,"displayName":name,"category":category,"tags":[name.lower(),slug.replace('_',' ')],"model":{"sourceBlend":f"res://assets/tote_processing/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/tote_processing/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/tote_processing/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/tote_processing/{slug}/thumbnail.png"},"bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],"kinematics":[{"id":i,"kind":k,"nodePath":n,"minimum":lo,"maximum":hi,"unit":u,"maximumRate":r}],"signals":[{"id":x,"dataType":t,"direction":d,"unit":unit,"kinematicAxis":kin,"description":x.replace('_',' ').capitalize()+"."} for x,t,d,unit,kin in signals],"quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":False,"materialReviewed":False,"scaleReviewed":True,"animationReviewed":False}}
    ids=[v[0] for v in SPECS.values()];doc["assets"]=[a for a in doc["assets"] if a["id"] not in ids]+[by[i] for i in ids];CATALOG.write_text(json.dumps(doc,indent=2)+"\n",encoding="utf-8");print("REGISTERED_TOTE_PROCESSING",len(ids));print("TOTAL_CANDIDATES",len(doc["assets"]))
if __name__=="__main__":main()
