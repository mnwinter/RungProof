"""Register robot architectures, tracks, tooling, and vision assets."""
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];CATALOG=ROOT/"assets"/"catalog"/"candidates.catalog.json"
SPECS={
"scara_robot":("robotics.robot.scara-four-axis.v1","Four-Axis SCARA Robot","robotics/robots/scara",[2.7,1.8,1.5],[("joint_1","rotary","KIN_JOINT_1",-180,180,"deg",360),("joint_2","rotary","KIN_JOINT_2",-150,150,"deg",360),("z_axis","linear","KIN_Z_QUILL",0,.65,"m",1.5),("tool_rotation","continuous","KIN_TOOL_ROTATION",0,360,"deg",720)]),
"delta_pick_robot":("robotics.robot.delta-three-arm.v1","Three-Arm Delta Pick Robot","robotics/robots/delta",[2.3,2.8,2.2],[("shoulder_a","rotary","KIN_SHOULDER_0",-55,55,"deg",500),("shoulder_b","rotary","KIN_SHOULDER_1",-55,55,"deg",500),("shoulder_c","rotary","KIN_SHOULDER_2",-55,55,"deg",500),("platform_z","linear","KIN_MOVING_PLATFORM",0,1.2,"m",4.0)]),
"six_axis_cobot":("robotics.robot.collaborative-six-axis.v1","Six-Axis Collaborative Robot","robotics/robots/collaborative",[2.4,2.2,1.4],[(f"joint_{i}","rotary",f"KIN_J{i}",-180,180,"deg",180) for i in range(1,7)]),
"cartesian_gantry_robot":("robotics.robot.cartesian-xyz-gantry.v1","Cartesian XYZ Gantry Robot","robotics/robots/cartesian",[3.5,2.7,2.5],[("x_axis","linear","KIN_X_BRIDGE",0,2.6,"m",2.0),("y_axis","linear","KIN_Y_CARRIAGE",0,1.4,"m",2.0),("z_axis","linear","KIN_Z_AXIS",0,1.3,"m",1.5)]),
"four_axis_palletizer":("robotics.robot.palletizer-four-axis.v1","Four-Axis Palletizing Robot","robotics/robots/palletizing",[2.6,2.7,1.8],[("axis_1","rotary","KIN_AXIS_1",-180,180,"deg",140),("axis_2","rotary","KIN_AXIS_2",-90,90,"deg",120),("axis_3","rotary","KIN_AXIS_3",-120,120,"deg",150),("axis_4","continuous","KIN_AXIS_4",0,360,"deg",240)]),
"robot_linear_track":("robotics.positioner.linear-track-seventh-axis.v1","Robot Seventh-Axis Linear Track","robotics/positioners/tracks",[3.8,1.1,1.0],[("track_position","linear","KIN_TRACK_CARRIAGE",0,2.7,"m",1.5)]),
"automatic_tool_changer":("robotics.tooling.changer.automatic-pneumatic.v1","Automatic Robot Tool Changer","robotics/tooling/tool-changers",[1.6,1.2,1.3],[("lock_position","linear","KIN_MASTER_COUPLER",0,.04,"m",.10)]),
"robotic_mig_torch":("robotics.end-effector.welding.mig-torch.v1","Robotic MIG Welding Torch","robotics/end-effectors/welding",[1.7,1.2,1.3],[]),
"servo_spot_weld_gun":("robotics.end-effector.welding.servo-spot-gun.v1","Robotic Servo Spot-Weld Gun","robotics/end-effectors/welding",[1.7,2.0,1.5],[("electrode_position","linear","KIN_MOVING_ELECTRODE",0,.32,"m",.25)]),
"robotic_paint_spray_gun":("robotics.end-effector.paint.automatic-spray-gun.v1","Robotic Automatic Paint Spray Gun","robotics/end-effectors/painting",[1.6,1.3,1.3],[]),
"robotic_high_speed_spindle":("robotics.end-effector.machining.high-speed-spindle.v1","Robotic High-Speed Machining Spindle","robotics/end-effectors/machining",[1.4,1.7,1.4],[("spindle_rotation","continuous","KIN_SPINDLE_MOTOR",0,360,"deg",24000)]),
"electromagnetic_sheet_gripper":("robotics.end-effector.magnetic.sheet-gripper-six-pole.v1","Six-Pole Electromagnetic Sheet Gripper","robotics/end-effectors/magnetic",[1.8,1.5,1.5],[]),
"robotic_pallet_fork":("robotics.end-effector.fork.adjustable-pallet.v1","Adjustable Robotic Pallet-Fork End Effector","robotics/end-effectors/forks",[2.0,1.7,1.6],[("left_fork_position","linear","KIN_FORK_LEFT",0,.35,"m",.25),("right_fork_position","linear","KIN_FORK_RIGHT",0,.35,"m",.25)]),
"industrial_3d_vision_camera":("robotics.vision.camera.stereo-3d-industrial.v1","Industrial Stereo 3D Vision Camera","robotics/vision/cameras",[1.4,1.6,1.3],[]),
}
def signals(slug,axes):
    rows=[]
    for aid,*_ in axes:rows.append({"id":aid+"_command","dataType":"float32","direction":"input","unit":None,"kinematicAxis":aid,"description":aid.replace('_',' ').capitalize()+" command."})
    rows.extend([{"id":"enable_command","dataType":"bool","direction":"input","unit":None,"kinematicAxis":None,"description":"Enable command."},{"id":"ready","dataType":"bool","direction":"output","unit":None,"kinematicAxis":None,"description":"Ready status."},{"id":"fault","dataType":"bool","direction":"output","unit":None,"kinematicAxis":None,"description":"Fault status."}])
    if "weld" in slug:rows.extend([{"id":"weld_trigger","dataType":"bool","direction":"input","unit":None,"kinematicAxis":None,"description":"Weld trigger."},{"id":"weld_complete","dataType":"bool","direction":"output","unit":None,"kinematicAxis":None,"description":"Weld complete."}])
    if "paint" in slug:rows.append({"id":"spray_command","dataType":"bool","direction":"input","unit":None,"kinematicAxis":None,"description":"Spray command."})
    if "magnetic" in slug:rows.append({"id":"magnet_command","dataType":"bool","direction":"input","unit":None,"kinematicAxis":None,"description":"Magnet command."})
    if "vision" in slug:rows.extend([{"id":"trigger","dataType":"bool","direction":"input","unit":None,"kinematicAxis":None,"description":"Acquisition trigger."},{"id":"inspection_passed","dataType":"bool","direction":"output","unit":None,"kinematicAxis":None,"description":"Inspection result."}])
    return rows
def axis(a):i,k,n,lo,hi,u,r=a;return {"id":i,"kind":k,"nodePath":n,"minimum":lo,"maximum":hi,"unit":u,"maximumRate":r}
def main():
    doc=json.loads(CATALOG.read_text(encoding="utf-8"));by={a["id"]:a for a in doc["assets"]}
    for slug,(aid,name,category,bounds,axes) in SPECS.items():
        by[aid]={"id":aid,"displayName":name,"category":category,"tags":[name.lower(),slug.replace('_',' ')],"model":{"sourceBlend":f"res://assets/robotics/{slug}/source/{slug}.blend","deliveryGltf":f"res://assets/robotics/{slug}/delivery/{slug}.glb","lodFiles":[],"collisionFile":f"res://assets/robotics/{slug}/collision/{slug}_collision.glb","thumbnailFile":f"res://assets/robotics/{slug}/thumbnail.png"},"bounds":{"widthM":bounds[0],"heightM":bounds[1],"depthM":bounds[2]},"connectors":[],"kinematics":[axis(a) for a in axes],"signals":signals(slug,axes),"quality":{"status":"candidate","blindReviewId":None,"recognitionConfidence":None,"topologyReviewed":True,"materialReviewed":True,"scaleReviewed":True,"animationReviewed":False}}
    ids=[v[0] for v in SPECS.values()];base=[a for a in doc["assets"] if a["id"] not in set(ids)];doc["assets"]=base+[by[i] for i in ids];CATALOG.write_text(json.dumps(doc,indent=2)+"\n",encoding="utf-8");print("REGISTERED_ROBOTICS",len(SPECS));print("TOTAL_CANDIDATES",len(doc["assets"]))
if __name__=="__main__":main()
