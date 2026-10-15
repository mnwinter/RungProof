"""Register reusable metering, receiving, and parcel-sizing equipment."""
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"
SPECS={
"liquid_metering_skid":("process.dosing.skid.liquid-metering.v1","Liquid Metering Pump Skid","process/dosing/metering",[2.3,1.9,1.5],[{"id":"pump_rotation","kind":"continuous","nodePath":"KIN_metering_pump_shaft","minimum":0,"maximum":360,"unit":"deg","maximumRate":1750}],[('metering_pump_run','bool','input','pump_rotation'),('flow_proven','bool','output',None),('metering_fault','bool','output',None)]),
"two_position_container_receiver":("material-handling.receiver.container-two-position.v1","Two-Position Container Receiving Fixture","material-handling/receivers",[2.5,1.8,1.8],[{"id":"receiver_rollers","kind":"continuous","nodePath":"KIN_receiver_roller_left","minimum":0,"maximum":360,"unit":"deg","maximumRate":45}],[('receiver_run','bool','input','receiver_rollers'),('left_present','bool','output',None),('right_present','bool','output',None)]),
"three_height_parcel_sensor_bank":("sensing.dimensioning.parcel-three-height.v1","Three-Height Parcel Sensor Bank","sensing/dimensioning",[.6,2.4,2.6],[],[('size_beam_low','bool','output',None),('size_beam_mid','bool','output',None),('size_beam_high','bool','output',None)]),
}
def main():
    doc=json.loads(CATALOG.read_text(encoding="utf-8"));by={a["id"]:a for a in doc["assets"]}
    for slug,(aid,name,category,bounds,kinematics,signals) in SPECS.items():
        by[aid]={"id":aid,"displayName":name,"category":category,"tags":[name.lower(),slug.replace('_',' ')],"model":{"sourceBlend":f"res://assets/scene_support/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/scene_support/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/scene_support/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/scene_support/{slug}/thumbnail.png"},"bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],"kinematics":kinematics,"signals":[{"id":sid,"dataType":typ,"direction":direction,"unit":None,"kinematicAxis":axis,"description":sid.replace('_',' ').capitalize()+'.'} for sid,typ,direction,axis in signals],"quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":False,"materialReviewed":False,"scaleReviewed":True,"animationReviewed":False}}
    ids=[spec[0] for spec in SPECS.values()];doc["assets"]=[a for a in doc["assets"] if a["id"] not in ids]+[by[i] for i in ids];CATALOG.write_text(json.dumps(doc,indent=2)+"\n",encoding="utf-8");print("REGISTERED_SCENE_SUPPORT",len(ids));print("TOTAL_CANDIDATES",len(doc["assets"]))
if __name__=="__main__":main()
