using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using RungProof.Next.Catalog;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;

// First TDD tracer: a saved connection must supply typed I/O while preserving
// its real source scene. Reflection lets this harness compile against today's
// production types and fail functionally while the policy is still absent.
// No test-only policy implementation or Godot dependency is substituted.
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var root=Path.GetFullPath(args.Length==1?args[0]:".");
            var options=new JsonSerializerOptions{PropertyNameCaseInsensitive=true};
            var scene=JsonSerializer.Deserialize<SceneDefinition>(File.ReadAllText(Path.Combine(root,"scenes/migrated/scene-1-conveyor-stop.scene.json")),options)!;
            var catalog=JsonSerializer.Deserialize<AssetCatalogDocument>(File.ReadAllText(Path.Combine(root,"assets/catalog/production.catalog.json")),options)!;
            var originalSimulation=scene.Simulation.GetRawText();
            var policy=typeof(WorkspaceDocument).Assembly.GetType("RungProof.Next.Workspace.WorkspaceDriveConnectionPolicy");
            if(policy is null)throw new InvalidOperationException("Saved generic belt connection cannot supply typed scene I/O: production WorkspaceDriveConnectionPolicy is absent.");
            var relation=Invoke(policy,"CreateRelation",scene,"learner_belt");
            var documentJson=JsonSerializer.SerializeToNode(new WorkspaceDocument(9,scene.Id,
                new[]{new WorkspacePlacement("learner_belt","material-handling.belt-conveyor.600x6000.v1",new double[]{0,0,3},new double[]{0,0,0},new double[]{1,1,1})},
                SignalLinks:new[]{new WorkspaceSignalLink("learner_run","learner_belt","run_command","learner_belt_run"),
                    new WorkspaceSignalLink("learner_running","learner_belt","running","learner_belt_running"),
                    new WorkspaceSignalLink("learner_speed","learner_belt","actual_speed","learner_belt_actual_speed_mps")}),options)!.AsObject();
            documentJson["driveConnection"]=JsonSerializer.SerializeToNode(relation,relation.GetType(),options);
            var document=documentJson.Deserialize<WorkspaceDocument>(options)!;
            var augmented=(SceneDefinition)Invoke(policy,"AugmentSource",scene,document,catalog);
            var points=augmented.Simulation.GetProperty("points").EnumerateArray().ToArray();
            Assert(points.Length==scene.Simulation.GetProperty("points").GetArrayLength()+3,"Exactly three connection I/O declarations must be appended.");
            CheckPoint("learner_belt_run","BOOL","PLC","output");
            CheckPoint("learner_belt_running","BOOL","PC","input");
            var speed=CheckPoint("learner_belt_actual_speed_mps","REAL","PC","input");
            Assert(speed.GetProperty("unit").GetString()=="m/s","Measured speed requires m/s, not arbitrary compatible REAL units.");
            Assert(augmented.Simulation.GetProperty("actions").GetRawText()==scene.Simulation.GetProperty("actions").GetRawText(),"Original actions changed.");
            Assert(augmented.Simulation.GetProperty("pointBindings").GetRawText()==scene.Simulation.GetProperty("pointBindings").GetRawText(),"Original point bindings changed.");
            Assert(scene.Simulation.GetRawText()==originalSimulation,"Original scene was mutated.");
            Assert(augmented.Id==scene.Id&&augmented.Equipment.SequenceEqual(scene.Equipment),"Original scene identity/equipment changed.");
            Console.WriteLine("WORKSPACE_DRIVE_PURE_CHECK saved_connection_adds_owned_typed_unit_correct_io_preserves_source=True");
            var tests=1;
            void Check(bool ok,string name){Assert(ok,name);tests++;Console.WriteLine("WORKSPACE_DRIVE_PURE_CHECK "+name+"=True");}
            void Reject(Action action,string name)
            {var rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Check(rejected,name);}
            WorkspaceDocument Fixture(string id)
            {
                var r=WorkspaceDriveConnectionPolicy.CreateRelation(scene,id);
                return new(9,scene.Id,new[]{new WorkspacePlacement(id,WorkspaceDriveConnectionPolicy.BeltAsset,new double[]{0,0,3},new double[]{0,0,0},new double[]{1,1,1})},
                    SignalLinks:new[]{new WorkspaceSignalLink("run",id,"run_command",r.RunPoint),new WorkspaceSignalLink("running",id,"running",r.RunningPoint),new WorkspaceSignalLink("speed",id,"actual_speed",r.ActualSpeedPoint)},DriveConnection:r);
            }
            var hyphen=Fixture("placement-001-material.handling-belt");
            WorkspaceDriveConnectionPolicy.Validate(hyphen,scene,catalog);
            Check(hyphen.DriveConnection!.RunPoint=="placement_001_material_handling_belt_run","actual_hyphen_instance_normalizes_ascii_identifier");
            Check(WorkspaceDriveConnectionPolicy.NormalizeInstanceId("1-belt") == "belt_1_belt"&&WorkspaceDriveConnectionPolicy.NormalizeInstanceId("bélt/x") == "b_lt_x","leading_digit_prefixed_nonascii_replaced");
            var roundtrip=JsonSerializer.Deserialize<WorkspaceDocument>(JsonSerializer.Serialize(hyphen),options)!;
            WorkspaceDriveConnectionPolicy.Validate(roundtrip,scene,catalog);
            Check(roundtrip.DriveConnection==hyphen.DriveConnection&&roundtrip.SignalLinks!.SequenceEqual(hyphen.SignalLinks!),"exact_relation_mappings_survive_pure_json_roundtrip");
            Check(WorkspaceDriveConnectionPolicy.Eligibility(hyphen) is {CommandMapped:true,FeedbackMapped:true,CanRun:true,CommandDetachCanCoast:false},"complete_maps_eligible_requires_runtime_guard_separately");
            var noCommand=hyphen with{SignalLinks=hyphen.SignalLinks!.Where(l=>l.SignalId!="run_command").ToArray()};
            WorkspaceDriveConnectionPolicy.Validate(noCommand,scene,catalog);
            Check(WorkspaceDriveConnectionPolicy.Eligibility(noCommand) is {CommandMapped:false,FeedbackMapped:true,CanRun:true,CommandDetachCanCoast:true},"detached_command_readable_coast_mapping_eligibility");
            foreach(var feedback in new[]{"running","actual_speed"})
            {
                var detached=hyphen with{SignalLinks=hyphen.SignalLinks!.Where(l=>l.SignalId!=feedback).ToArray()};
                WorkspaceDriveConnectionPolicy.Validate(detached,scene,catalog);
                Check(WorkspaceDriveConnectionPolicy.Eligibility(detached) is {FeedbackMapped:false,CanRun:false,CommandDetachCanCoast:false},"detached_"+feedback+"_readable_but_execution_retired");
            }
            var allDetached=hyphen with{SignalLinks=Array.Empty<WorkspaceSignalLink>()};WorkspaceDriveConnectionPolicy.Validate(allDetached,scene,catalog);
            Check(WorkspaceDriveConnectionPolicy.Eligibility(allDetached) is {CommandMapped:false,FeedbackMapped:false,CanRun:false},"all_detached_preserved_without_autorepair");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{DriveConnection=null}),"version9_requires_relation");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Version=5}),"relation_only_workspace9");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{DriveConnection=hyphen.DriveConnection with{Version=2}}),"relation_version1_only");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{SourceSceneId="lab-5-01-delayed-lamp"}),"wrong_source_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{DriveConnection=hyphen.DriveConnection with{RunPoint="conveyor_running"}}),"custom_point_alias_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{DriveConnection=hyphen.DriveConnection with{SourceFingerprint="x"}}),"malformed_fingerprint_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.Validate(hyphen,scene with{Name=scene.Name+" changed"},catalog),"source_revision_drift_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Placements=Array.Empty<WorkspacePlacement>()}),"missing_target_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Placements=new[]{hyphen.Placements[0] with{AssetId="material-handling.pallet-roller-conveyor.1000x4000.v1"}}}),"unsupported_target_asset_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Placements=new[]{hyphen.Placements[0] with{Scale=new double[]{1,2,1}}}}),"nonunit_scale_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Placements=new[]{hyphen.Placements[0] with{Rotation=new double[]{0,90,0}}}}),"rotation_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Placements=new[]{hyphen.Placements[0] with{Position=new double[]{double.NaN,0,0}}}}),"nonfinite_position_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{Placements=hyphen.Placements.Concat(new[]{hyphen.Placements[0] with{InstanceId="placement_001_material_handling_belt"}}).ToArray()}),"normalization_collision_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{FiniteBeltPreset=WorkspaceFiniteBeltPolicy.Relation()}),"finite_belt_relation_conflict_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{FiniteRollerPreset=WorkspaceFiniteRollerPolicy.Relation()}),"finite_roller_relation_conflict_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{TemplateOverride=new(1,"ControlInstallation",hyphen.DriveConnection.SourceFingerprint,new double[]{0,0,0},0)}),"template_relation_conflict_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{SignalLinks=hyphen.SignalLinks!.Concat(new[]{new WorkspaceSignalLink("other",hyphen.DriveConnection.InstanceId,"estop_ok",hyphen.DriveConnection.RunPoint)}).ToArray()}),"other_opted_asset_signal_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{SignalLinks=hyphen.SignalLinks!.Concat(new[]{hyphen.SignalLinks![0] with{Id="duplicate"}}).ToArray()}),"duplicate_canonical_input_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{SignalLinks=hyphen.SignalLinks!.Concat(new[]{hyphen.SignalLinks![2] with{Id="cross",InstanceId="another_asset"}}).ToArray()}),"cross_instance_owned_feedback_claim_rejected");
            Reject(()=>WorkspaceDriveConnectionPolicy.ValidateShape(hyphen with{SignalLinks=new[]{hyphen.SignalLinks![0] with{PointName=hyphen.DriveConnection.RunningPoint}}}),"plc_command_to_pc_feedback_alias_rejected");
            var belt=catalog.Assets.Single(a=>a.Id==WorkspaceDriveConnectionPolicy.BeltAsset);
            foreach(var mutation in new[]{("actual_speed","unit"),("actual_speed","type"),("running","direction")})
            {
                var altered=belt with{Signals=belt.Signals.Select(s=>s.Id!=mutation.Item1?s:mutation.Item2 switch{"unit"=>s with{Unit="m"},"type"=>s with{DataType="bool"},_=>s with{Direction="input"}}).ToArray()};
                var badCatalog=catalog with{Assets=catalog.Assets.Select(a=>a.Id==belt.Id?altered:a).ToArray()};
                Reject(()=>WorkspaceDriveConnectionPolicy.Validate(hyphen,scene,badCatalog),"catalog_exact_"+mutation.Item1+"_"+mutation.Item2+"_required");
            }
            var collisionSimulation=JsonNode.Parse(scene.Simulation.GetRawText())!.AsObject();
            collisionSimulation["points"]!.AsArray().Add(new JsonObject{{"name",hyphen.DriveConnection.RunPoint},{"type","BOOL"},{"owner","PC"},{"initial",false}});
            var collisionSource=scene with{Simulation=JsonSerializer.SerializeToElement(collisionSimulation)};
            var collisionDocument=hyphen with{DriveConnection=WorkspaceDriveConnectionPolicy.CreateRelation(collisionSource,hyphen.DriveConnection.InstanceId)};
            Reject(()=>WorkspaceDriveConnectionPolicy.Validate(collisionDocument,collisionSource,catalog),"existing_source_point_collision_rejected_even_matching_fingerprint");
            Check(augmented.PlcTestProfile is null&&scene.PlcTestProfile is not null,"derived_added_drive_offline_original_external_profile_not_inherited");
            foreach(var property in scene.Simulation.EnumerateObject().Where(p=>p.Name!="points"))
                Check(augmented.Simulation.GetProperty(property.Name).GetRawText()==property.Value.GetRawText(),"original_simulation_property_preserved_"+property.Name);
            Console.WriteLine("WORKSPACE_DRIVE_PURE_RESULT failures=0 tests="+tests);return 0;
            JsonElement CheckPoint(string name,string type,string owner,string role)
            {
                var point=points.Single(p=>p.GetProperty("name").GetString()==name);
                Assert(point.GetProperty("type").GetString()==type&&point.GetProperty("owner").GetString()==owner&&point.GetProperty("role").GetString()==role,"Incorrect declaration for "+name);return point;
            }
        }
        catch(Exception error){Console.WriteLine("WORKSPACE_DRIVE_PURE_FAILURE "+error);Console.WriteLine("WORKSPACE_DRIVE_PURE_RESULT failures=1 tests=1");return 1;}
    }
    private static object Invoke(Type policy,string method,params object[] args)=>policy.GetMethod(method,BindingFlags.Public|BindingFlags.Static)?.Invoke(null,args)
        ??throw new InvalidOperationException("Required production connection operation is unavailable: "+method);
    private static void Assert(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);}
}
