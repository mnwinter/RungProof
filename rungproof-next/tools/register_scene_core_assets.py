"""Register generated scene-core assets without promoting them to production."""
from __future__ import annotations
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"

SPECS={
"single_pushbutton_station":("controls.operator-station.single-pushbutton.v1","Single Pushbutton Pedestal Station","controls/operator-stations",[.5,1.1,.4],["pressed"],["button_travel","linear","KIN_pushbutton","m",0,.018,.20]),
"emergency_stop_station":("controls.operator-station.emergency-stop.v1","Emergency-Stop Pedestal Station","controls/operator-stations",[.5,1.1,.4],["estop_ok"],["estop_travel","linear","KIN_estop","m",0,.018,.20]),
"single_tier_beacon":("controls.beacon.single-tier.v1","Single-Tier Signal Beacon","controls/indication",[.4,1.4,.4],["lamp_on"],None),
"pneumatic_pusher":("actuation.pneumatic-pusher.1350mm.v1","Pneumatic Product Pusher - 1350 mm Stroke","actuation/pneumatic/linear",[1.7,1.1,.9],["extend_command","retract_command","extended","retracted"],["pusher_position","linear","KIN_push_plate","m",0,1.35,.65]),
"ac_induction_motor":("drives.motor.ac-induction.v1","AC Induction Motor","drives/motors",[1.8,1.1,.9],["run_command","running"],["motor_rotation","rotary_continuous","KIN_motor_shaft","rpm",0,3600,720]),
"flanged_pipe_spool":("process.pipe.flanged-spool.v1","Flanged Process Pipe Spool","process/piping",[3.6,1.2,.8],[],None),
"vertical_process_tank":("process.tank.vertical-3000x5000.v1","Vertical Process Tank - 3 m x 5 m","process/tanks",[3.4,5.8,3.4],["level_percent","high_level","low_level"],None),
"centrifugal_pump_skid":("process.pump.centrifugal-skid.v1","Centrifugal Pump and Motor Skid","process/pumps",[2.7,1.5,1.2],["run_command","running","flow_estimate"],["pump_rotation","rotary_continuous","KIN_pump_shaft","rpm",0,3600,720]),
"actuated_process_valve":("process.valve.actuated-ball.v1","Actuated Ball Valve","process/valves",[2.22,1.816,.77],["open_command","close_command","open_limit","closed_limit"],["valve_position","rotary","KIN_valve_stem","deg",0,90,90]),
"level_transmitter_4_20ma":("sensing.level.analog-4-20ma.v1","4-20 mA Level Transmitter","sensing/level",[.8305,3.605,.64],["level_percent","signal_ma"],None),
"tuning_fork_level_switch":("sensing.level.tuning-fork.v1","Tuning-Fork Point Level Switch","sensing/level",[.46,2.104,.435],["covered"],None),
"radar_level_transmitter":("sensing.level.radar.v1","Non-Contact Radar Level Transmitter","sensing/level",[.771,1.422,.68],["level_percent","signal_ma","echo_ok"],None),
"rotary_selector_station":("controls.operator-station.selector.v1","Four-Position Selector Station","controls/operator-stations",[.7,1.7,.6],["position"],["selector_position","rotary","KIN_selector_handle","deg",-55,55,180]),
"axial_exhaust_fan":("air-handling.fan.axial-1900.v1","Industrial Axial Exhaust Fan","air-handling/fans",[2.2,2.4,1.3],["run_command","running","speed_percent"],["fan_rotation","rotary_continuous","KIN_fan_hub","rpm",0,1800,720]),
"scissor_lift_table":("material-handling.lift.scissor-table.v1","Hydraulic Scissor Lift Table","material-handling/lifts",[3.1,2.4,2.0],["raise_command","lower_command","top_limit","bottom_limit"],["lift_height","linear","KIN_platform","m",0,2.2,1.1]),
"industrial_drill_press":("machining.drill-press.pedestal.v1","Industrial Pedestal Drill Press","machining/drilling",[1.9,4.1,1.6],["run_command","guard_closed","spindle_position"],["spindle_speed","rotary_continuous","KIN_spindle","rpm",0,3000,800]),
"six_axis_robot":("robotics.robot.six-axis-medium.v1","Six-Axis Industrial Robot","robotics/arms",[4.3,3.8,2.0],["run_command","running","at_home"],["axis_1","rotary","KIN_axis_1","deg",-170,170,120]),
"powered_rotary_table":("material-handling.table.powered-rotary.v1","Powered Indexing Rotary Table","material-handling/tables",[2.7,1.2,2.7],["position_command","at_position"],["table_angle","rotary","KIN_table","deg",-360,360,90]),
"motorized_roller_shutter":("access-control.door.roller-shutter.v1","Motorized Industrial Roller Shutter","access-control/doors",[5.3,4.2,.8],["open_command","close_command","open_limit","closed_limit"],["door_position","linear","KIN_bottom_bar","m",0,3.2,1.2]),
"enclosed_machine_center":("machining.machine.enclosed-center.v1","Enclosed Machining Center","machining/machine-tools",[3.6,3.3,2.3],["run_command","running","door_closed","faulted"],["spindle_speed","rotary_continuous","KIN_spindle","rpm",0,12000,3000]),
}

def signal(name):
    output=name in {"pressed","estop_ok","position","running","extended","retracted","flow_estimate","level_percent","high_level","low_level","signal_ma","open_limit","closed_limit","covered","echo_ok","top_limit","bottom_limit","spindle_position","at_home","at_position","door_closed","faulted"}
    dtype="float32" if name in {"flow_estimate","level_percent","signal_ma","speed_percent","spindle_position"} else ("int32" if name in {"position","position_command"} else "bool")
    return {"id":name,"dataType":dtype,"direction":"output" if output else "input","unit":None,"kinematicAxis":None,"description":name.replace("_"," ").capitalize()+"."}

def main():
    doc=json.loads(CATALOG.read_text(encoding="utf-8")); by_id={a["id"]:a for a in doc["assets"]}
    for slug,(asset_id,name,category,bounds,signals,axis) in SPECS.items():
        kin=[]
        if axis:
            aid,kind,node,unit,minimum,maximum,maximum_rate=axis
            kin=[{"id":aid,"kind":kind,"nodePath":node,"minimum":minimum,"maximum":maximum,"unit":unit,"maximumRate":maximum_rate}]
        entry={"id":asset_id,"displayName":name,"category":category,"tags":[name.lower(),slug.replace("_"," ")],
          "model":{"sourceBlend":f"res://assets/scene_core/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/scene_core/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/scene_core/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/scene_core/{slug}/thumbnail.png"},
          "bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],"kinematics":kin,"signals":[signal(s) for s in signals],
          "quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":True,"materialReviewed":True,"scaleReviewed":True,"animationReviewed":False}}
        by_id[asset_id]=entry
    doc["assets"]=list(by_id.values());CATALOG.write_text(json.dumps(doc,indent=2)+"\n",encoding="utf-8")
    print("REGISTERED_CANDIDATES",len(doc["assets"]))
if __name__=="__main__":main()
