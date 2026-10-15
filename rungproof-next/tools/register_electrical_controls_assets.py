"""Register electrical distribution and controls tranche as candidates."""
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"
# slug: id, name, category, bounds, axes, signals
SPECS={
"mcc_withdrawable_bucket":("electrical.mcc.bucket.withdrawable-motor-feeder.v1","Withdrawable MCC Motor-Feeder Bucket","electrical/distribution/mcc",[1.15,.90,1.60],[('disconnect_angle','angular','KIN_DISCONNECT_HANDLE',0,90,'deg',120)],[('disconnect_command','bool','input',None,'disconnect_angle'),('disconnect_closed','bool','output',None,None),('starter_running','bool','output',None,None),('starter_fault','bool','output',None,None)]),
"wall_vfd":("drives.vfd.wall-mount-keypad.v1","Wall-Mount Variable Frequency Drive","electrical/drives/vfd",[.80,.50,1.30],[],[('run_command','bool','input',None,None),('speed_reference','float32','input','Hz',None),('output_frequency','float32','output','Hz',None),('drive_fault','bool','output',None,None)]),
"book_servo_drive":("drives.servo.book-form.v1","Book-Form Servo Drive","electrical/drives/servo",[.75,.50,1.30],[],[('servo_enable','bool','input',None,None),('velocity_command','float32','input','rpm',None),('actual_velocity','float32','output','rpm',None),('drive_fault','bool','output',None,None)]),
"plc_rack":("controls.plc.modular-rack-six-slot.v1","Six-Slot Modular PLC Rack","controls/plc",[1.60,.40,.90],[],[('controller_run','bool','input',None,None),('controller_ok','bool','output',None,None),('io_fault','bool','output',None,None)]),
"industrial_hmi":("controls.hmi.touch-panel-10in.v1","10-Inch Industrial Touch HMI","controls/hmi",[1.20,.28,.95],[],[('screen_active','bool','input',None,None),('operator_touch','bool','output',None,None)]),
"control_transformer":("electrical.transformer.control-open-core.v1","Open-Core Control Transformer","electrical/power/transformers",[1.15,.90,1.15],[],[]),
"fused_disconnect":("electrical.disconnect.enclosed-fused-rotary.v1","Enclosed Fused Disconnect Switch","electrical/distribution/disconnects",[.80,.55,1.30],[('handle_angle','angular','KIN_HANDLE',0,90,'deg',180)],[('close_command','bool','input',None,'handle_angle'),('closed','bool','output',None,None)]),
"molded_case_breaker":("electrical.breaker.molded-case-three-pole.v1","Three-Pole Molded-Case Circuit Breaker","electrical/protection/breakers",[.75,.50,1.15],[('handle_angle','angular','KIN_BREAKER_HANDLE',0,45,'deg',180)],[('close_command','bool','input',None,'handle_angle'),('closed','bool','output',None,None),('tripped','bool','output',None,None)]),
"din_terminal_strip":("electrical.terminal-strip.din-rail-14way.v1","14-Way DIN-Rail Terminal Strip","electrical/panel/terminals",[1.70,.50,.65],[],[]),
"din_24v_power_supply":("electrical.power-supply.din-24vdc-10a.v1","DIN-Rail 24 VDC 10 A Power Supply","electrical/power/dc-supplies",[.60,.45,1.10],[],[('ac_present','bool','input',None,None),('dc_ok','bool','output',None,None),('output_voltage','float32','output','VDC',None)]),
"managed_ethernet_switch":("network.industrial-ethernet.managed-8port.v1","Eight-Port Managed Industrial Ethernet Switch","network/industrial-ethernet",[1.20,.50,.75],[],[('power_ok','bool','input',None,None),('network_fault','bool','output',None,None)]),
"safety_relay":("safety.relay.dual-channel-din.v1","Dual-Channel DIN-Rail Safety Relay","safety/control/relays",[.45,.45,1.05],[],[('channel_a','bool','input',None,None),('channel_b','bool','input',None,None),('reset_command','bool','input',None,None),('safety_output','bool','output',None,None)]),
"contactor_overload_starter":("electrical.starter.contactor-overload-three-pole.v1","Three-Pole Contactor and Overload Starter","electrical/motor-control/starters",[.65,.50,1.10],[],[('coil_command','bool','input',None,None),('auxiliary_closed','bool','output',None,None),('overload_tripped','bool','output',None,None)]),
"soft_starter":("drives.soft-starter.three-phase.v1","Three-Phase Soft Starter","electrical/drives/soft-starters",[.75,.55,1.30],[],[('start_command','bool','input',None,None),('ramp_time','float32','input','s',None),('running','bool','output',None,None),('starter_fault','bool','output',None,None)]),
}
def axis(a):i,k,n,lo,hi,u,r=a;return {"id":i,"kind":k,"nodePath":n,"minimum":lo,"maximum":hi,"unit":u,"maximumRate":r}
def signal(s):i,t,d,u,k=s;return {"id":i,"dataType":t,"direction":d,"unit":u,"kinematicAxis":k,"description":i.replace('_',' ').capitalize()+"."}
def main():
    doc=json.loads(CATALOG.read_text(encoding="utf-8"));by={a["id"]:a for a in doc["assets"]}
    for slug,(aid,name,category,bounds,axes,signals) in SPECS.items():
        by[aid]={"id":aid,"displayName":name,"category":category,"tags":[name.lower(),slug.replace('_',' ')],"model":{"sourceBlend":f"res://assets/electrical_controls/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/electrical_controls/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/electrical_controls/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/electrical_controls/{slug}/thumbnail.png"},"bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],"kinematics":[axis(a) for a in axes],"signals":[signal(s) for s in signals],"quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":True,"materialReviewed":True,"scaleReviewed":True,"animationReviewed":False}}
    ids=[v[0] for v in SPECS.values()];base=[a for a in doc["assets"] if a["id"] not in set(ids)];doc["assets"]=base+[by[i] for i in ids];CATALOG.write_text(json.dumps(doc,indent=2)+"\n",encoding="utf-8");print("REGISTERED_ELECTRICAL_CONTROLS",len(SPECS));print("TOTAL_CANDIDATES",len(doc["assets"]))
if __name__=="__main__":main()
