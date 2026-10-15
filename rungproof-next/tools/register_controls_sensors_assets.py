"""Register the controls/sensors tranche as candidate-only catalog assets."""
from __future__ import annotations

import json
from pathlib import Path


ROOT=Path(__file__).resolve().parents[1]
CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"

# slug: id, display name, category, width/height/depth, signal tuples
SPECS={
"inductive_proximity_sensor_m18":("sensing.proximity.inductive-m18.v1","M18 Inductive Proximity Sensor","sensing/proximity",[.55,.65,.75],["detected"]),
"capacitive_proximity_sensor_m30":("sensing.proximity.capacitive-m30.v1","M30 Capacitive Proximity Sensor","sensing/proximity",[.65,.75,.85],["detected"]),
"diffuse_photoelectric_sensor":("sensing.photoelectric.diffuse.v1","Diffuse Photoelectric Sensor","sensing/photoelectric",[.55,.70,.60],["detected"]),
"through_beam_photoelectric_pair":("sensing.photoelectric.through-beam-pair.v1","Through-Beam Photoelectric Sensor Pair","sensing/photoelectric",[1.45,1.30,.45],["beam_clear","detected"]),
"ultrasonic_distance_sensor":("sensing.distance.ultrasonic-single-transducer.v1","Single-Transducer Ultrasonic Distance Sensor","sensing/distance",[.65,.75,.65],["distance_mm","in_range"]),
"laser_distance_sensor":("sensing.distance.laser.v1","Industrial Laser Distance Sensor","sensing/distance",[.65,.75,1.45],["distance_mm","in_range"]),
"roller_lever_limit_switch":("sensing.position.roller-lever-limit-switch.v1","Roller-Lever Limit Switch","sensing/position",[.75,1.05,.65],["actuated"]),
"tongue_safety_interlock":("safety.interlock.tongue-switch.v1","Tongue-Actuated Safety Interlock Switch","safety/interlocks",[.85,.90,.55],["guard_closed","safety_ok"]),
"coded_magnetic_safety_switch":("safety.interlock.coded-magnetic-pair.v1","Coded Magnetic Safety Switch Pair","safety/interlocks",[.65,.95,.50],["guard_closed","safety_ok"]),
"safety_laser_scanner":("safety.scanner.floor-area.v1","Industrial Safety Laser Scanner","safety/presence-sensing",[3.2,.90,3.2],["field_clear","warning_field_clear"]),
"rfid_read_write_head":("sensing.identification.rfid-read-write-head.v1","Industrial RFID Read/Write Head","sensing/identification",[1.10,.80,.65],["read_trigger","tag_present","tag_code"]),
"incremental_rotary_encoder":("sensing.position.incremental-encoder.v1","Incremental Rotary Encoder","sensing/position",[.75,.75,.85],["position_counts","speed_rpm"]),
"pressure_transmitter":("sensing.pressure.smart-transmitter.v1","Smart Industrial Pressure Transmitter","sensing/pressure",[.55,1.15,.55],["pressure_bar","healthy"]),
"analog_pressure_gauge":("instrumentation.pressure.analog-gauge.v1","Analog Pressure Gauge","instrumentation/pressure",[.85,1.15,.55],[]),
"rtd_temperature_probe":("sensing.temperature.rtd-probe.v1","Industrial RTD Temperature Probe","sensing/temperature",[.70,1.40,.70],["temperature_c","healthy"]),
"thermal_flow_sensor":("sensing.flow.insertion-thermal.v1","Insertion Thermal Flow Sensor on Pipe Spool","sensing/flow",[1.65,1.30,.80],["flow_m3h","flow_switch"]),
"piezo_vibration_sensor":("sensing.condition.vibration.v1","Stud-Mount Industrial Vibration Sensor","sensing/condition",[.80,.80,.70],["velocity_mm_s","healthy"]),
"shear_beam_load_cell":("sensing.force.shear-beam-load-cell.v1","Shear-Beam Load Cell","sensing/force",[1.45,.55,.55],["force_n","overload"]),
"eight_port_io_link_master":("controls.remote-io.io-link-master-8port.v1","Eight-Port IO-Link Master","controls/remote-io",[.90,1.00,.55],["connected","faulted"]),
"three_button_control_station":("controls.operator-station.three-button.v1","Three-Button Start Reset Stop Station","controls/operator-stations",[.60,1.00,.55],["start","reset","stop"]),
"guarded_foot_switch":("controls.operator-station.guarded-foot-switch.v1","Guarded Industrial Foot Switch","controls/operator-stations",[.85,.70,1.00],["pressed"]),
"rope_pull_estop_station":("safety.estop.rope-pull-switch.v1","Rope-Pull Emergency-Stop Switch","safety/emergency-stop",[3.2,.95,.65],["estop_ok","rope_tripped"]),
"industrial_safety_mat":("safety.mat.pressure-sensitive-2x1m.v1","Pressure-Sensitive Industrial Safety Mat","safety/presence-sensing",[2.35,.25,1.30],["field_clear"]),
"five_tier_stacklight_siren":("controls.stack-light.five-tier-sounder.v1","Five-Tier Stack Light with Sounder","controls/indication",[.60,2.90,.60],["green_on","white_on","amber_on","blue_on","red_on","sounder_on"]),
}

INPUTS={"read_trigger","start","reset","stop","green_on","white_on","amber_on","blue_on","red_on","sounder_on"}
FLOATS={"distance_mm":"mm","speed_rpm":"rpm","pressure_bar":"bar","temperature_c":"degC","flow_m3h":"m3/h","velocity_mm_s":"mm/s","force_n":"N"}
INTS={"position_counts","tag_code"}

def signal(name):
    dtype="float32" if name in FLOATS else ("int32" if name in INTS else "bool")
    return {"id":name,"dataType":dtype,"direction":"input" if name in INPUTS else "output","unit":FLOATS.get(name),"kinematicAxis":None,"description":name.replace("_"," ").capitalize()+"."}

def main():
    document=json.loads(CATALOG.read_text(encoding="utf-8"));by_id={asset["id"]:asset for asset in document["assets"]}
    for superseded in ("sensing.photoelectric.retroreflective-pair.v1","sensing.distance.ultrasonic-dual-transducer.v1"):
        by_id.pop(superseded,None)
    for slug,(asset_id,name,category,bounds,signals) in SPECS.items():
        by_id[asset_id]={
            "id":asset_id,"displayName":name,"category":category,"tags":[name.lower(),slug.replace("_"," ")],
            "model":{"sourceBlend":f"res://assets/controls_sensors/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/controls_sensors/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/controls_sensors/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/controls_sensors/{slug}/thumbnail.png"},
            "bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],"kinematics":[],"signals":[signal(value) for value in signals],
            "quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":True,"materialReviewed":True,"scaleReviewed":True,"animationReviewed":False},
        }
    ordered_ids=[spec[0] for spec in SPECS.values()]
    tranche_ids=set(ordered_ids)|{"sensing.photoelectric.retroreflective-pair.v1","sensing.distance.ultrasonic-dual-transducer.v1"}
    base=[asset for asset in document["assets"] if asset["id"] not in tranche_ids]
    document["assets"]=base+[by_id[asset_id] for asset_id in ordered_ids]
    CATALOG.write_text(json.dumps(document,indent=2)+"\n",encoding="utf-8")
    print("REGISTERED_CONTROLS_SENSORS",len(SPECS));print("TOTAL_CANDIDATES",len(document["assets"]))
if __name__=="__main__":main()
