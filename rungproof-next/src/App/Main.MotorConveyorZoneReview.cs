using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyMotorConveyorZones;
    private async void VerifyMotorConveyorZones()
    {
        var failures=0;
        void Check(bool condition,string name)
        { GD.Print($"MOTOR_CONVEYOR_AUDIT {name}={condition}"); if(!condition) failures++; }
        try
        {
            VerifyDraftRunLifecycle(Check);
            VerifyTemplateProjectWorkflow(Check);
            await VerifyIndexedDeviceTraceWorkflow(Check);
            // Analytic finite-load fixtures, separate from app/controller proof.
            var all=Enumerable.Repeat(true,10).ToArray(); var first=new bool[10];first[0]=true;
            var model=new MotorConveyorZonePlantModel(); model.Step(100,first);
            Check(Math.Abs(model.LeadingEdge-1.4)<1e-9,"large_step_stopped_downstream_no_tunneling");
            model.Step(.3,all);var straddled=model.Center;var stoppedSecond=(bool[])all.Clone();stoppedSecond[1]=false;
            model.Step(10,stoppedSecond);Check(Math.Abs(model.Center-straddled)<1e-9,"already_straddled_stop_no_reverse_teleport");
            var coarse=new MotorConveyorZonePlantModel();var fine=new MotorConveyorZonePlantModel();
            coarse.Step(12,all);for(var i=0;i<1200;i++)fine.Step(.01,all);
            Check(Math.Abs(coarse.Center-fine.Center)<1e-9,"constant_command_fixed_delta_equivalence");
            model.Step(100,all);Check(model.AtDischarge && Math.Abs(model.TrailingEdge-13.4)<1e-9,"terminal_finite_load_hold");

            foreach(var wrong in new[]{false,true})
            {
                AddMigratedScene("lab-10-06-ten-motor-array-startup",_candidateCatalog!,_mainCamera!,false,false);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var runtime=_sceneRuntime!;var root=_sceneCompositionRoot!;
                var installation=root.GetNode<Node3D>("MotorConveyorInstallation");
                Check(SceneComposer.VerifyMotorConveyorGeometry(root)==10,"ten_actual_mapped_supported_motors");
                var document=new LadderEditorDocument();
                document.ResetProject(wrong?"zone-partial-qa":"zone-all-on-qa","TenZone_QA",TimeSpan.FromMilliseconds(20));
                document.SourceSceneId="lab-10-06-ten-motor-array-startup";
                document.AddTag("request",PlcVariableRole.Input,"group_start_request");
                for(var sensor=0;sensor<10;sensor++) document.AddTag("sense_"+sensor,PlcVariableRole.Input,"zone_occupied_"+sensor);
                document.AddAggregateTag("zones",PlcVariableRole.Output,PlcVariableType.Array,
                    new(ElementType:PlcVariableType.Bool,LowerBound:0,Length:10),new bool[10],"motor_commands");
                for(var zone=0;zone<(wrong?1:10);zone++)
                {document.AddRung("Drive zone "+zone,$"zones[{zone}]");document.AddContact(zone,0,"request",false);}
                GD.Print($"MOTOR_CONVEYOR_PROGRAM provenance=explicit-ephemeral-QA validWrong={wrong} sha256="+
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(LadderEditorProjectJson.Save(document)))));
                EnableVirtualControllerProgram(document.BuildProgram());
                var observedSensingLines=new bool[10];
                Check(Enumerable.Range(0,10).All(i=>Equals(runtime.Points["zone_occupied_"+i],i==0)),"reset_initial_actual_sensing_line_image");
                void Scan()
                {
                    var preceding=Enumerable.Range(0,10).Select(i=>runtime.Points["zone_occupied_"+i] is true).ToArray();
                    _virtualController!.Advance(.020,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
                        CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,runtime.AdvanceSimulation,
                        SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
                    var projected=Enumerable.Range(0,10).Select(i=>runtime.Points["zone_occupied_"+i] is true).ToArray();
                    if(_virtualController.Snapshot.ScanNumber==1 || !preceding.SequenceEqual(projected))
                    {
                        Check(Enumerable.Range(0,10).All(i=>_virtualController.Snapshot.Variables["sense_"+i]==preceding[i]),"actual_controller_samples_preceding_plant_feedback");
                        GD.Print($"MOTOR_CONVEYOR_SENSOR_TRACE scan={_virtualController.Snapshot.ScanNumber} validWrong={wrong} sampled="+string.Concat(preceding.Select(v=>v?'1':'0'))+" projected="+string.Concat(projected.Select(v=>v?'1':'0'))+" center="+runtime.CaptureMotorConveyorZones().Center);
                    }
                    for(var i=0;i<10;i++) observedSensingLines[i]|=projected[i]||preceding[i];
                }
                var homes=root.GetChildren().OfType<Node3D>().Where(n=>n.HasMeta("motorConveyorZone")).ToDictionary(n=>n,n=>n.Transform);
                var shafts=homes.Keys.SelectMany(n=>n.FindChildren("KIN_motor_*","MeshInstance3D",true,false).OfType<MeshInstance3D>()).ToDictionary(n=>n,n=>n.Transform);
                var bodies=homes.Keys.SelectMany(n=>n.FindChildren("MOTOR_*","MeshInstance3D",true,false).OfType<MeshInstance3D>()).ToDictionary(n=>n,n=>n.Transform);
                var drums=Enumerable.Range(0,10).Select(i=>installation.GetNode<Node3D>($"Zone_{i}/KIN_zone_drive")).ToDictionary(n=>n,n=>n.Transform);
                StandardMaterial3D Belt(int i)=>(StandardMaterial3D)installation.GetNode<MeshInstance3D>($"Zone_{i}/ZoneBeltSurface").MaterialOverride;
                RunActiveController();Check(ExecuteSelectedControllerAction("toggle-group_start_request"),"accepted_actual_request_action");Scan();
                Check(_simulatorShell!.VerifyDeviceIndexedBinding(document.BuildProgram(),_virtualController!.Snapshot,"motor_0","zones[0]"),"device_trace_actual_indexed_leaf_binding_after_accepted_scan");
                Check(Enumerable.Range(0,10).All(i=>Equals(runtime.Points[$"motor_commands[{i}]"],!wrong||i==0)),"accepted_scan_image_"+(wrong?"partial":"all"));
                for(var i=0;i<10;i++)
                {
                    var motor=root.GetNode<Node3D>("motor_"+i);var running=!wrong||i==0;
                    Check(shafts.Where(p=>motor.IsAncestorOf(p.Key)).Any(p=>!p.Key.Transform.IsEqualApprox(p.Value))==running,"runtime_only_actual_shaft_"+i+"_wrong_"+wrong);
                    var drum=installation.GetNode<Node3D>($"Zone_{i}/KIN_zone_drive");
                    Check(!drum.Transform.IsEqualApprox(drums[drum])==running && (Belt(i).Uv1Offset.Length()>0)==running,"actual_drum_unique_belt_uv_"+i+"_wrong_"+wrong);
                }
                Check(homes.All(p=>p.Key.Transform.IsEqualApprox(p.Value))&&bodies.All(p=>p.Key.Transform.IsEqualApprox(p.Value)),"moving_drive_stationary_roots_and_stators");
                Check(Math.Abs(runtime.CaptureMotorConveyorZones().Center-.309)<1e-6,"one_accepted_scan_one_material_tick");
                var ticks=wrong?500:1700;for(var scan=0;scan<ticks;scan++)Scan();
                var observed=runtime.CaptureMotorConveyorZones();
                Check(wrong?observedSensingLines[0]&&!observedSensingLines.Skip(1).Any(v=>v):observedSensingLines.All(v=>v),"plant_feedback_trace_distinguishes_valid_wrong_program");
                Check(wrong?!observed.AtDischarge&&Math.Abs(observed.Center-1.1)<1e-6:observed.AtDischarge,"valid_wrong_code_yields_wrong_observed_goal_"+wrong);
                GD.Print($"MOTOR_CONVEYOR_SCAN_RESULT scan={_virtualController!.Snapshot.ScanNumber} validWrong={wrong} center={observed.Center} zone={observed.Zone} discharge={observed.AtDischarge} evaluatorOutputInjection=false");
                var carton=installation.GetNode<Node3D>("TeachingCarton");
                Check(Math.Abs(carton.Position.X-(-7+observed.Center))<1e-5,"actual_carton_matches_observed_model");
                var stoppedMeshes=shafts.Keys.Concat(drums.Keys).Append(carton).ToDictionary(n=>n,n=>n.Transform);
                var stoppedUvs=Enumerable.Range(0,10).Select(i=>Belt(i).Uv1Offset).ToArray();
                StopActiveController();for(var i=0;i<10;i++)runtime.AdvanceSimulation(.2);
                Check(stoppedMeshes.All(p=>p.Key.Transform.IsEqualApprox(p.Value))&&Enumerable.Range(0,10).All(i=>Belt(i).Uv1Offset.IsEqualApprox(stoppedUvs[i])),"actual_Main_Stop_all_plant_visuals_freeze");
                ResetActiveController();
                Check(homes.All(p=>p.Key.Transform.IsEqualApprox(p.Value))&&shafts.All(p=>p.Key.Transform.IsEqualApprox(p.Value))&&drums.All(p=>p.Key.Transform.IsEqualApprox(p.Value))
                    &&Enumerable.Range(0,10).All(i=>Belt(i).Uv1Offset.IsZeroApprox())&&Math.Abs(runtime.CaptureMotorConveyorZones().Center-.3)<1e-9
                    &&SceneComposer.VerifyMotorConveyorGeometry(root)==10,"actual_Main_Reset_all_supported_homes");
                Check(Enumerable.Range(0,10).All(i=>Equals(runtime.Points["zone_occupied_"+i],i==0)),"Main_Reset_reprojects_actual_loaded_zone0_beam");
                DisableVirtualController();
            }
            // Closed-loop QA: the actual final beam becomes a controller input
            // on the following scan, and that program stops all driven zones.
            AddMigratedScene("lab-10-06-ten-motor-array-startup",_candidateCatalog!,_mainCamera!,false,false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var feedbackRuntime=_sceneRuntime!;var feedbackRoot=_sceneCompositionRoot!;
            var feedbackDocument=new LadderEditorDocument();
            feedbackDocument.ResetProject("zone-final-beam-stop-qa","FinalBeam_Stop_QA",TimeSpan.FromMilliseconds(20));
            feedbackDocument.SourceSceneId="lab-10-06-ten-motor-array-startup";
            feedbackDocument.AddTag("request",PlcVariableRole.Input,"group_start_request");
            feedbackDocument.AddTag("last_beam",PlcVariableRole.Input,"zone_occupied_9");
            feedbackDocument.AddAggregateTag("zones",PlcVariableRole.Output,PlcVariableType.Array,
                new(ElementType:PlcVariableType.Bool,LowerBound:0,Length:10),new bool[10],"motor_commands");
            for(var i=0;i<10;i++)
            {
                feedbackDocument.AddRung("Stop at physical final beam "+i,$"zones[{i}]");
                feedbackDocument.AddContact(i,0,"request",false);
                feedbackDocument.AddContact(i,0,"last_beam",true);
            }
            GD.Print("MOTOR_CONVEYOR_FEEDBACK_PROGRAM provenance=explicit-ephemeral-QA goal=final-sensing-line-stop sha256="+
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(LadderEditorProjectJson.Save(feedbackDocument)))));
            EnableVirtualControllerProgram(feedbackDocument.BuildProgram());
            void FeedbackScan()=>_virtualController!.Advance(.020,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
                CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,feedbackRuntime.AdvanceSimulation,
                SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
            var illegalFeedbackCommitRejected=false;
            try{feedbackRuntime.CommitVirtualControllerOutputs(new Dictionary<string,bool>{["zone_occupied_9"]=true});}
            catch(InvalidOperationException){illegalFeedbackCommitRejected=true;}
            Check(illegalFeedbackCommitRejected && feedbackRuntime.Points["zone_occupied_9"] is false,"plant_PC_sensor_rejects_PLC_output_write");
            Check(_activeSceneDefinition!.Simulation.GetProperty("actions").EnumerateArray().All(a=>
                !a.TryGetProperty("point",out var point)||!point.GetString()!.StartsWith("zone_occupied_",StringComparison.Ordinal)),"generated_sensors_have_no_manual_scene_actions");
            RunActiveController();Check(ExecuteSelectedControllerAction("toggle-group_start_request"),"feedback_QA_actual_request_action");
            var physicalIntersection=false;
            for(var scan=0;scan<2000;scan++)
            {
                FeedbackScan();
                if(feedbackRuntime.Points["zone_occupied_9"] is true)
                {physicalIntersection=true;break;}
            }
            Check(physicalIntersection && _virtualController!.Snapshot.Variables["last_beam"] is false
                &&Enumerable.Range(0,10).All(i=>feedbackRuntime.Points[$"motor_commands[{i}]"] is true),"physical_final_beam_projects_after_scan_before_next_sample");
            var finalInstallation=feedbackRoot.GetNode<Node3D>("MotorConveyorInstallation");
            var finalMeshes=feedbackRoot.GetChildren().OfType<Node3D>().Where(n=>n.HasMeta("motorConveyorZone"))
                .SelectMany(n=>n.FindChildren("KIN_motor_*","MeshInstance3D",true,false).OfType<Node3D>())
                .Concat(Enumerable.Range(0,10).Select(i=>finalInstallation.GetNode<Node3D>($"Zone_{i}/KIN_zone_drive")))
                .Append(finalInstallation.GetNode<Node3D>("TeachingCarton")).ToDictionary(n=>n,n=>n.Transform);
            var finalUvs=Enumerable.Range(0,10).Select(i=>((StandardMaterial3D)finalInstallation.GetNode<MeshInstance3D>($"Zone_{i}/ZoneBeltSurface").MaterialOverride).Uv1Offset).ToArray();
            var finalPosition=feedbackRuntime.CaptureMotorConveyorZones().Center;
            FeedbackScan();
            Check(_virtualController!.Snapshot.Variables["last_beam"] is true&&Enumerable.Range(0,10).All(i=>feedbackRuntime.Points[$"motor_commands[{i}]"] is false),"next_accepted_scan_physical_input_controls_all_outputs_off");
            for(var scan=0;scan<20;scan++)FeedbackScan();
            Check(finalMeshes.All(p=>p.Key.Transform.IsEqualApprox(p.Value))&&Enumerable.Range(0,10).All(i=>
                ((StandardMaterial3D)finalInstallation.GetNode<MeshInstance3D>($"Zone_{i}/ZoneBeltSurface").MaterialOverride).Uv1Offset.IsEqualApprox(finalUvs[i]))
                &&Math.Abs(feedbackRuntime.CaptureMotorConveyorZones().Center-finalPosition)<1e-9
                &&!feedbackRuntime.CaptureMotorConveyorZones().AtDischarge,"feedback_controlled_actual_plant_hold_not_discharge_claim");
            GD.Print($"MOTOR_CONVEYOR_CLOSED_LOOP_RESULT scan={_virtualController.Snapshot.ScanNumber} physicalInput9={feedbackRuntime.Points["zone_occupied_9"]} sampledInput9={_virtualController.Snapshot.Variables["last_beam"]} center={finalPosition} goal=final-beam-stop injectedFeedback=false");
            ResetActiveController();DisableVirtualController();
            // Separate accepted output-image fixtures prove alias arbitration;
            // they do not substitute for the loaded-program proof above.
            var rt=_sceneRuntime!;rt.UsesExternalClock=true;rt.ResetSimulation();rt.SetControllerPlaybackRunning(true);
            void Image(Dictionary<string,bool> image)=>rt.CommitVirtualControllerOutputs(image);
            Image(new(){["motor_0_run"]=true,["motor_commands[0]"]=true});rt.AdvanceSimulation(.2);
            Image(new(){["motor_0_run"]=false});var before=rt.CaptureMotorConveyorZones().Center;rt.AdvanceSimulation(.2);
            Check(rt.CaptureMotorConveyorZones().Center>before,"mixed_alias_clear_scalar_retains_array_transport");
            Image(new(){["motor_commands[0]"]=false});before=rt.CaptureMotorConveyorZones().Center;rt.AdvanceSimulation(.2);
            Check(rt.CaptureMotorConveyorZones().Center==before,"mixed_alias_clear_last_source_stops");
            Image(new(){["motor_array_run"]=true,["motor_9_run"]=true});rt.AdvanceSimulation(.2);
            Image(new(){["motor_array_run"]=false});
            var motors=_sceneCompositionRoot!.GetChildren().OfType<Node3D>().Where(n=>n.HasMeta("motorConveyorZone"));
            Check(motors.All(m=>m.FindChildren("*","",true,false).OfType<EquipmentMotionController>().Single().Running==(m.Name=="motor_9")),"clear_group_retains_only_individual_zone9");
            rt.ResetSimulation();
        }
        catch(Exception error){failures++;GD.PushError("MOTOR_CONVEYOR_AUDIT_EXCEPTION "+error);}
        finally{DisableVirtualController();}
        GD.Print($"MOTOR_CONVEYOR_AUDIT_RESULT failures={failures} provenance=explicit-QA-program-scans-plus-labeled-output-fixtures scope=offline-symbolic-single-load-not-hardware-or-visual-acceptance");
        GetTree().Quit(failures==0?0:1);
    }
}
