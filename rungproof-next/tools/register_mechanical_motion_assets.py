"""Register mechanical-motion tranche as candidate-only catalog assets."""
from __future__ import annotations

import json
from pathlib import Path


ROOT=Path(__file__).resolve().parents[1]
CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"

# slug: id, display name, category, bounds, kinematic axes, signals
SPECS={
"iso_tie_rod_pneumatic_cylinder":("actuation.pneumatic.cylinder.iso-tie-rod.v1","ISO Tie-Rod Pneumatic Cylinder","actuation/pneumatic/cylinders",[2.0,.60,.90],[('rod_travel','linear','KIN_rod',0,.60,'m',1.0)],[('extend_command','bool','input',None,'rod_travel'),('retract_command','bool','input',None,'rod_travel'),('position_m','float32','output','m','rod_travel'),('extended','bool','output',None,None),('retracted','bool','output',None,None)]),
"guided_pneumatic_cylinder":("actuation.pneumatic.cylinder.guided.v1","Guided Pneumatic Cylinder","actuation/pneumatic/cylinders",[1.05,.82,.88],[('tool_plate_travel','linear','KIN_TOOL_PLATE',0,.55,'m',.8)],[('extend_command','bool','input',None,'tool_plate_travel'),('retract_command','bool','input',None,'tool_plate_travel'),('position_m','float32','output','m','tool_plate_travel'),('extended','bool','output',None,None),('retracted','bool','output',None,None)]),
"rodless_pneumatic_cylinder":("actuation.pneumatic.cylinder.rodless.v1","Rodless Pneumatic Cylinder","actuation/pneumatic/cylinders",[2.0,.62,.80],[('carriage_travel','linear','KIN_CARRIAGE',-.65,.65,'m',1.2)],[('position_command_m','float32','input','m','carriage_travel'),('position_m','float32','output','m','carriage_travel'),('in_position','bool','output',None,None)]),
"pneumatic_rotary_actuator":("actuation.pneumatic.rotary.rack-pinion.v1","Rack-and-Pinion Pneumatic Rotary Actuator","actuation/pneumatic/rotary",[.95,.86,1.0],[('output_angle','angular','KIN_OUTPUT_FLANGE',0,180,'deg',180)],[('clockwise_command','bool','input',None,'output_angle'),('counterclockwise_command','bool','input',None,'output_angle'),('angle_deg','float32','output','deg','output_angle'),('clockwise_limit','bool','output',None,None),('counterclockwise_limit','bool','output',None,None)]),
"hydraulic_welded_body_cylinder":("actuation.hydraulic.cylinder.welded-body.v1","Industrial Welded-Body Hydraulic Cylinder","actuation/hydraulic/cylinders",[2.6,1.30,1.20],[('rod_travel','linear','KIN_CHROME_ROD',0,.70,'m',.45)],[('extend_command','bool','input',None,'rod_travel'),('retract_command','bool','input',None,'rod_travel'),('position_m','float32','output','m','rod_travel'),('pressure_bar','float32','output','bar',None)]),
"parallel_two_jaw_gripper":("robotics.end-effector.parallel-two-jaw.v1","Industrial Parallel Two-Jaw Gripper","robotics/end-effectors/grippers",[2.1,1.15,1.10],[('jaw_left','linear','KIN_JAW_SLIDE_-0.23',0,.12,'m',.5),('jaw_right','linear','KIN_JAW_SLIDE_0.23',0,.12,'m',.5)],[('close_command','bool','input',None,'jaw_left'),('open_command','bool','input',None,'jaw_left'),('grip_width_m','float32','output','m','jaw_left'),('part_present','bool','output',None,None)]),
"vacuum_multi_cup_gripper":("robotics.end-effector.vacuum-four-cup.v1","Four-Cup Vacuum Carton Gripper","robotics/end-effectors/vacuum",[2.0,1.05,1.25],[],[('vacuum_command','bool','input',None,None),('vacuum_ok','bool','output',None,None),('part_present','bool','output',None,None)]),
"ac_servo_motor":("drives.motor.ac-servo-flange.v1","Industrial AC Servo Motor","drives/motors/servo",[1.05,.72,.82],[('shaft_rotation','angular_continuous','KIN_SERVO_SHAFT',-3000,3000,'rpm',6000)],[('enable','bool','input',None,'shaft_rotation'),('speed_setpoint_rpm','float32','input','rpm','shaft_rotation'),('actual_speed_rpm','float32','output','rpm','shaft_rotation'),('in_position','bool','output',None,None),('faulted','bool','output',None,None)]),
"nema_stepper_motor":("drives.motor.stepper-nema.v1","NEMA Frame Stepper Motor","drives/motors/stepper",[1.25,.68,.82],[('shaft_rotation','angular_continuous','KIN_STEPPER_SHAFT',-1500,1500,'rpm',4000)],[('enable','bool','input',None,'shaft_rotation'),('step_command','int32','input','count','shaft_rotation'),('direction','bool','input',None,'shaft_rotation'),('position_steps','int32','output','count','shaft_rotation')]),
"inline_helical_gearmotor":("drives.gearmotor.inline-helical.v1","Inline Helical Gearmotor","drives/gearmotors/helical",[1.45,.76,.92],[('output_rotation','angular_continuous','KIN_OUTPUT_SHAFT',-500,500,'rpm',800)],[('run_command','bool','input',None,'output_rotation'),('speed_setpoint_rpm','float32','input','rpm','output_rotation'),('actual_speed_rpm','float32','output','rpm','output_rotation'),('faulted','bool','output',None,None)]),
"right_angle_worm_gearmotor":("drives.gearmotor.right-angle-worm.v1","Right-Angle Worm Gearmotor with Hollow Output","drives/gearmotors/worm",[1.25,1.28,1.02],[('output_rotation','angular_continuous','KIN_OUTPUT_HUB',-250,250,'rpm',500)],[('run_command','bool','input',None,'output_rotation'),('speed_setpoint_rpm','float32','input','rpm','output_rotation'),('actual_speed_rpm','float32','output','rpm','output_rotation'),('faulted','bool','output',None,None)]),
"pillow_block_bearing":("mechanical.bearing.pillow-block.v1","Mounted Pillow-Block Bearing","mechanical/bearings/mounted",[1.20,.95,.82],[('shaft_rotation','angular_continuous','KIN_INNER_RACE',-3000,3000,'rpm',6000)],[('shaft_speed_rpm','float32','input','rpm','shaft_rotation'),('bearing_temperature_c','float32','output','degC',None),('vibration_mm_s','float32','output','mm/s',None)]),
"flexible_jaw_coupling":("mechanical.coupling.flexible-jaw.v1","Flexible Jaw Coupling with Elastomer Spider","mechanical/couplings",[1.60,.68,.72],[('input_rotation','angular_continuous','KIN_HUB_INPUT',-3000,3000,'rpm',6000),('output_rotation','angular_continuous','KIN_HUB_OUTPUT',-3000,3000,'rpm',6000)],[('input_speed_rpm','float32','input','rpm','input_rotation'),('output_speed_rpm','float32','output','rpm','output_rotation'),('slip_detected','bool','output',None,None)]),
"profile_rail_linear_guide":("mechanical.linear-guide.profile-rail.v1","Profile Rail Linear Guide and Carriage","mechanical/linear-motion/guides",[2.1,.72,.70],[('carriage_travel','linear','KIN_GUIDE_CARRIAGE',-.75,.75,'m',2.0)],[('position_m','float32','input','m','carriage_travel')]),
"ball_screw_linear_actuator":("actuation.electric.linear.ball-screw.v1","Servo Ball-Screw Linear Actuator","actuation/electric/linear",[2.90,.90,.92],[('carriage_travel','linear','KIN_BALL_NUT_CARRIAGE',-.75,.75,'m',1.5)],[('enable','bool','input',None,'carriage_travel'),('position_setpoint_m','float32','input','m','carriage_travel'),('position_m','float32','output','m','carriage_travel'),('in_position','bool','output',None,None),('faulted','bool','output',None,None)]),
"rack_pinion_linear_actuator":("actuation.electric.linear.rack-pinion.v1","Motorized Rack-and-Pinion Linear Actuator","actuation/electric/linear",[2.40,1.75,1.02],[('carriage_travel','linear','KIN_CARRIAGE',-.75,.75,'m',2.5)],[('enable','bool','input',None,'carriage_travel'),('position_setpoint_m','float32','input','m','carriage_travel'),('position_m','float32','output','m','carriage_travel'),('in_position','bool','output',None,None),('faulted','bool','output',None,None)]),
}


def axis(row):
    ident,kind,node,minimum,maximum,unit,rate=row
    return {"id":ident,"kind":kind,"nodePath":node,"minimum":minimum,"maximum":maximum,"unit":unit,"maximumRate":rate}


def signal(row):
    ident,dtype,direction,unit,kinematic=row
    return {"id":ident,"dataType":dtype,"direction":direction,"unit":unit,"kinematicAxis":kinematic,
            "description":ident.replace('_',' ').capitalize()+"."}


def main():
    doc=json.loads(CATALOG.read_text(encoding="utf-8")); by_id={a["id"]:a for a in doc["assets"]}
    by_id.pop("actuation.hydraulic.cylinder.tie-rod.v1",None)
    for slug,(asset_id,name,category,bounds,axes,signals) in SPECS.items():
        by_id[asset_id]={
            "id":asset_id,"displayName":name,"category":category,"tags":[name.lower(),slug.replace('_',' ')],
            "model":{"sourceBlend":f"res://assets/mechanical_motion/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/mechanical_motion/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/mechanical_motion/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/mechanical_motion/{slug}/thumbnail.png"},
            "bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],
            "kinematics":[axis(v) for v in axes],"signals":[signal(v) for v in signals],
            "quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":True,"materialReviewed":True,"scaleReviewed":True,"animationReviewed":False},
        }
    tranche={spec[0] for spec in SPECS.values()}|{"actuation.hydraulic.cylinder.tie-rod.v1"}
    base=[a for a in doc["assets"] if a["id"] not in tranche]
    ordered=[spec[0] for spec in SPECS.values()]
    doc["assets"]=base+[by_id[v] for v in ordered]
    CATALOG.write_text(json.dumps(doc,indent=2)+"\n",encoding="utf-8")
    print("REGISTERED_MECHANICAL_MOTION",len(SPECS));print("TOTAL_CANDIDATES",len(doc["assets"]))


if __name__=="__main__": main()
