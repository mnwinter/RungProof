using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyWorkspaceRollerGeometry;
    private string? _workspaceRollerGeometryCapture;
    private bool _workspaceRollerGeometryCapturePending;
    private Node3D? _workspaceRollerGeometryCaptureRoot;
    private Vector3 _workspaceRollerOpticsTarget;
    // Ephemeral geometry experiment only. No workspace, program, signal, controller, or asset writes.
    private void VerifyWorkspaceRollerGeometry()=>VerifyWorkspaceRollerGeometryExperiment(false);
    private async void VerifyWorkspaceRollerGeometryExperiment(bool externalPair)
    {
        var jsonOptions=new JsonSerializerOptions{WriteIndented=true,NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals};
        var failures=0;var checks=new List<object>();var travel=new List<object>();var intervals=new List<object>();
        var experiment=new Node3D{Name="EphemeralRollerGeometry"};
        const string rollerId="material-handling.pallet-roller-conveyor.1000x4000.v1";
        const string palletId="loads.pallet.gma-48x40.v1";
        string AssetPath(string id)=>_candidateCatalog!.Assets.Single(a=>a.Id==id).Model.DeliveryGltf;
        string HashFile(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var rollerPath=ProjectSettings.GlobalizePath(AssetPath(rollerId));var palletPath=ProjectSettings.GlobalizePath(AssetPath(palletId));
        var beforeRoller=HashFile(rollerPath);var beforePallet=HashFile(palletPath);
        const string externalId="sensing.photoelectric.through-beam.v1";
        var externalPath=externalPair?ProjectSettings.GlobalizePath(AssetPath(externalId)):null;
        var beforeExternal=externalPath is null?null:HashFile(externalPath);
        var evidencePath=ProjectSettings.GlobalizePath(externalPair?"res://.tools/studio-verification/finite-roller-external-geometry-native-v2.json":"res://.tools/studio-verification/finite-roller-geometry-native-v5.json");
        var dllPath=ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/RungProof.Next.dll");
        string DllHash()=>File.Exists(dllPath)?HashFile(dllPath):"UNAVAILABLE";

        void Check(bool pass,string name,object? measured=null){checks.Add(new{name,pass,measured});GD.Print($"WORKSPACE_ROLLER_GEOMETRY {name}={pass} measured={JsonSerializer.Serialize(measured,jsonOptions)}");if(!pass)failures++;}
        try
        {
            Check(File.Exists(dllPath),"pinned_current_dll_exists",dllPath);
            AddChild(experiment);
            Node3D Import(string id,string name,Vector3 position)
            {
                var model=ResourceLoader.Load<PackedScene>(AssetPath(id)).Instantiate<Node3D>();model.Name=name;model.Position=position;experiment.AddChild(model);return model;
            }
            var conveyor=Import(rollerId,"Roller",new Vector3(0,-.04f,0));
            var pallet=Import(palletId,"Pallet",Vector3.Zero);
            var station=Import(WorkspaceFiniteBeltPolicy.OperatorAsset,"Operator",new Vector3(2.9f,0,2.2f));
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var conveyorMeshes=ReviewMeshes(conveyor).ToArray();var palletMeshes=ReviewMeshes(pallet).ToArray();
            MeshInstance3D Part(string name)=>conveyorMeshes.Single(m=>m.Name.ToString()==name);
            var rollers=conveyorMeshes.Where(m=>m.Name.ToString().StartsWith("KIN_roller_",StringComparison.Ordinal)).OrderBy(m=>ReviewBounds(m).GetCenter().X).ToArray();
            var feet=conveyorMeshes.Where(m=>m.Name.ToString().EndsWith("_foot",StringComparison.Ordinal)).ToArray();
            Check(rollers.Length==31,"exact_31_delivered_rollers");
            Check(feet.Length==6&&feet.All(m=>Math.Abs(ReviewBounds(m).Position.Y)<.001),"six_foot_plates_grounded",feet.Select(m=>new{name=m.Name.ToString(),bottom=ReviewBounds(m).Position.Y}).ToArray());
            var crowns=rollers.Select(m=>m.Mesh.GetFaces().Max(v=>(m.GlobalTransform*v).Y)).ToArray();var crown=crowns.Max();
            var centres=rollers.Select(m=>ReviewBounds(m).GetCenter()).ToArray();
            Check(crowns.All(y=>Math.Abs(y-1.053)<.001)&&centres.All(c=>Math.Abs(c.Y-1.015)<.001),"common_measured_crown_and_axis",new{crown,first=centres.First().ToString(),last=centres.Last().ToString()});
            Check(rollers.All(m=>Math.Abs(ReviewBounds(m).Size.X/2-.038)<.001&&Math.Abs(ReviewBounds(m).Size.Z-1.02)<.001
                &&Math.Abs(m.GlobalBasis.Y.Normalized().Dot(Vector3.Back))>.999),"radius_face_and_local_rotation_axis");
            var axis=rollers[0].GlobalBasis.Y.Normalized();var radial=Vector3.Up*.038f;
            var positiveTangent=axis.Cross(radial);var phaseSign=positiveTangent.Dot(Vector3.Left)>=0?1:-1;
            Check(rollers.All(m=>(m.GlobalBasis.Y.Normalized().Cross(radial)*phaseSign).Normalized().Dot(Vector3.Left)>.999),"derived_signed_phase_crown_velocity_negative_x",new{axis=axis.ToString(),positiveTangent=positiveTangent.ToString(),phaseSign});
            Check(centres.Skip(1).Select((c,i)=>Math.Abs(c.X-centres[i].X-.128)<.001).All(v=>v),"actual_roller_pitch");
            var palletBefore=palletMeshes.Select(ReviewBounds).Aggregate((a,b)=>a.Merge(b));
            pallet.Position=new Vector3(0,crown-palletBefore.Position.Y,0);
            var palletBounds=palletMeshes.Select(ReviewBounds).Aggregate((a,b)=>a.Merge(b));
            var runners=palletMeshes.Where(m=>m.Name.ToString().StartsWith("PALLET_bottom_runner",StringComparison.Ordinal)).ToArray();
            Check(runners.Length==3&&runners.All(m=>Math.Abs(ReviewBounds(m).Position.Y-crown)<.001),"unit_pallet_runner_bottom_seated",new{palletScale=pallet.Scale.ToString(),palletBounds=palletBounds.ToString()});
            var near=centres.First().X;var far=centres.Last().X;var terminal=near+.05f-palletBounds.Position.X;var home=far-.05f-palletBounds.End.X;
            Check(home>terminal&&Math.Abs(home-terminal-2.525)<.003,"finite_full_pallet_footprint_limits",new{home,terminal,travel=home-terminal});
            var guides=conveyorMeshes.Where(m=>m.Name.ToString().StartsWith("FRAME_",StringComparison.Ordinal)&&m.Name.ToString().EndsWith("_guide",StringComparison.Ordinal)).ToArray();
            var positiveGuide=guides.Single(m=>ReviewBounds(m).GetCenter().Z>0);var negativeGuide=guides.Single(m=>ReviewBounds(m).GetCenter().Z<0);
            var sideClearance=Math.Min(ReviewBounds(positiveGuide).Position.Z-palletBounds.End.Z,palletBounds.Position.Z-ReviewBounds(negativeGuide).End.Z);
            Check(sideClearance>=.01,"measured_guide_inner_clearance",sideClearance);
            Vector3 from,to,normal;
            var stationaryOccluders=new List<object>();
            MeshInstance3D[] stationary=conveyorMeshes;
            var proposedMount="rootY-.04; sensor body/lens yaw180 about body centre; bracket/cable retained";
            if(externalPair)
            {
                // Height derives from actual imported top-board solids and optical lens centre.
                // These retained posts/feet move together. No original embedded hardware moves.
                var topBoards=palletMeshes.Where(m=>m.Name.ToString().StartsWith("PALLET_top_board",StringComparison.Ordinal)).ToArray();
                Check(topBoards.Length>0,"actual_topboard_meshes_present");
                var woodBandBottom=topBoards.Max(m=>ReviewBounds(m).Position.Y);
                var woodBandTop=topBoards.Min(m=>ReviewBounds(m).End.Y);
                var beamHeight=(woodBandBottom+woodBandTop)/2;
                Check(woodBandTop-woodBandBottom>.02,"shared_actual_topboard_interior_band",new{woodBandBottom,woodBandTop,beamHeight});
                var pair=Import(externalId,"ExternalThroughBeam",new Vector3(-1.20f,0,0));
                var pairMeshes=ReviewMeshes(pair).ToArray();
                // B2 fixed diagnostic pose: move each complete named tower by the same
                // world-Z translation. Geometry, rotations and unit scale remain intact.
                // The dashed visual witnesses are not part of either mechanical tower.
                foreach(var towerPrefix in new[]{"TX_","RX_"})
                {
                    var towerMeshes=pairMeshes.Where(m=>m.Name.ToString().StartsWith(towerPrefix,StringComparison.Ordinal)).ToArray();
                    var foot=towerMeshes.Single(m=>m.Name.ToString()==towerPrefix+"foot");
                    var shift=Vector3.Back*(Math.Sign(ReviewBounds(foot).GetCenter().Z)*.12f);
                    foreach(var mesh in towerMeshes)mesh.GlobalPosition+=shift;
                    Check(towerMeshes.Length>0,"complete_tower_outward_translation_"+towerPrefix,new{shift=shift.ToString(),members=towerMeshes.Select(m=>m.Name.ToString()).ToArray()});
                }
                var tx=pairMeshes.Single(m=>m.Name.ToString()=="TX_lens");
                var rx=pairMeshes.Single(m=>m.Name.ToString()=="RX_lens");
                pair.Position+=Vector3.Up*(beamHeight-ReviewBounds(tx).GetCenter().Y);
                var txBox=ReviewBounds(tx);var rxBox=ReviewBounds(rx);
                var txHousing=pairMeshes.Single(m=>m.Name.ToString()=="TX_housing");
                var rxHousing=pairMeshes.Single(m=>m.Name.ToString()=="RX_housing");
                var txDirection=(txBox.GetCenter()-ReviewBounds(txHousing).GetCenter()).Normalized();
                var rxDirection=(rxBox.GetCenter()-ReviewBounds(rxHousing).GetCenter()).Normalized();
                normal=txDirection;
                from=new Vector3(txBox.GetCenter().X,txBox.GetCenter().Y,txDirection.Z<0?txBox.Position.Z:txBox.End.Z);
                to=new Vector3(rxBox.GetCenter().X,rxBox.GetCenter().Y,rxDirection.Z<0?rxBox.Position.Z:rxBox.End.Z);
                Check(txDirection.Dot((to-from).Normalized())>.999&&rxDirection.Dot((from-to).Normalized())>.999
                    &&Math.Abs(from.Y-to.Y)<.001&&Math.Abs(from.X+1.20)<.001,"actual_opposed_inward_external_lens_faces",new{from=from.ToString(),to=to.ToString(),txDirection=txDirection.ToString(),rxDirection=rxDirection.ToString()});
                var supports=new List<MeshInstance3D>();
                var pairFeet=pairMeshes.Where(m=>m.Name.ToString().EndsWith("_foot",StringComparison.Ordinal)).ToArray();
                foreach(var foot in pairFeet)
                {
                    var fb=ReviewBounds(foot);
                    if(fb.Position.Y<=0)throw new InvalidOperationException("External sensor foot must be above the grounded pedestal datum.");
                    // Exact footprint avoids inventing a wider support crossing the rail clearance.
                    var support=new MeshInstance3D{Name="ExternalPedestal_"+foot.Name,Position=new Vector3(fb.GetCenter().X,fb.Position.Y/2,fb.GetCenter().Z),
                        Mesh=new BoxMesh{Size=new Vector3(fb.Size.X,fb.Position.Y,fb.Size.Z)},
                        MaterialOverride=new StandardMaterial3D{AlbedoColor=new Color("64716c").SrgbToLinear()}};
                    experiment.AddChild(support);supports.Add(support);
                }
                Check(pairFeet.Length==2&&supports.Count==2&&supports.All(m=>Math.Abs(ReviewBounds(m).Position.Y)<.001),"two_grounded_pedestals_bearing_retained_feet",supports.Select(m=>new{name=m.Name.ToString(),bounds=ReviewBounds(m).ToString()}).ToArray());
                var witnesses=pairMeshes.Where(m=>m.Name.ToString().StartsWith("KIN_beam_dash",StringComparison.Ordinal)).ToArray();
                foreach(var witness in witnesses)witness.Visible=false; // Optical witnesses are not solids.
                var pairSolids=pairMeshes.Except(witnesses).ToArray();
                var sweptPallet=new Aabb(new Vector3(terminal+palletBounds.Position.X,palletBounds.Position.Y,palletBounds.Position.Z),
                    new Vector3(home-terminal+palletBounds.Size.X,palletBounds.Size.Y,palletBounds.Size.Z));
                Check(!pairSolids.Concat(supports).Any(m=>ReviewBounds(m).Intersects(sweptPallet)),"external_hardware_clear_full_pallet_swept_volume",sweptPallet.ToString());
                Check(pair.Scale==Vector3.One&&pairMeshes.Count(m=>m.Name.ToString().EndsWith("_m12_connector",StringComparison.Ordinal))==2
                    &&pairMeshes.Count(m=>m.Name.ToString().EndsWith("_cable",StringComparison.Ordinal))==2,"retained_unit_pair_posts_and_terminated_pigtails",new{pairPosition=pair.Position.ToString(),pairScale=pair.Scale.ToString()});
                var collisions=new List<object>();
                foreach(var added in pairSolids.Concat(supports))
                foreach(var retained in conveyorMeshes)
                {
                    // Conservative genuine 3D clearance gate: positive-volume AABB overlap
                    // is reported, never waived as a camera or planar-projection artefact.
                    var a=ReviewBounds(added);var b=ReviewBounds(retained);var overlap=a.Intersection(b);
                    if(overlap.Size.X>1e-5f&&overlap.Size.Y>1e-5f&&overlap.Size.Z>1e-5f)
                        collisions.Add(new{added=added.Name.ToString(),retained=retained.Name.ToString(),overlap=overlap.ToString()});
                }
                Check(collisions.Count==0,"external_pair_and_pedestals_clear_retained_hardware",collisions);
                stationary=conveyorMeshes.Concat(pairSolids.Where(m=>m!=tx&&m!=rx)).Concat(supports).ToArray();
                proposedMount=$"externalPair B2 unit scale stationX=-1.20 rootY={pair.Position.Y}; entire TX/RX towers outward Z±.12; actual topboard interior; exact-foot grounded pedestals; retained original embedded hardware unchanged";
            }
            else
            {
                var body=Part("SENSOR_photoeye");var lens=Part("SENSOR_photoeye_lens");var reflector=Part("SENSOR_reflector");var bracket=Part("SENSOR_photoeye_bracket");
                var bodyCentre=ReviewBounds(body).GetCenter();var corrected=new Node3D{Name="EphemeralInwardOptics",Position=bodyCentre};experiment.AddChild(corrected);
                body.Reparent(corrected,true);lens.Reparent(corrected,true);corrected.Rotation=new Vector3(0,MathF.PI,0);
                var lensBounds=ReviewBounds(lens);var reflectorBounds=ReviewBounds(reflector);normal=lens.GlobalBasis.Y.Normalized();
                from=lensBounds.GetCenter()+normal*(lensBounds.Size.Z/2);to=new Vector3(reflectorBounds.GetCenter().X,from.Y,reflectorBounds.End.Z);
                Check(normal.Dot((to-from).Normalized())>.999&&Math.Abs(from.X-to.X)<.001,"inward_lens_normal_and_opposed_ray",new{from=from.ToString(),to=to.ToString(),normal=normal.ToString()});
                Check(ReviewBounds(body).Intersects(ReviewBounds(bracket)),"corrected_body_retains_bracket_contact");
                stationary=conveyorMeshes.Where(m=>m!=body&&m!=lens&&m!=reflector).ToArray();
                var bodyBox=ReviewBounds(body);float DistanceBox(Vector3 v)=>v.DistanceTo(new Vector3(Math.Clamp(v.X,bodyBox.Position.X,bodyBox.End.X),Math.Clamp(v.Y,bodyBox.Position.Y,bodyBox.End.Y),Math.Clamp(v.Z,bodyBox.Position.Z,bodyBox.End.Z)));
                var cable=Part("CTRL_sensor_cable");var cableDistance=cable.Mesh.GetFaces().Min(v=>DistanceBox(cable.GlobalTransform*v));
                Check(cableDistance<=.015,"original_cable_reaches_corrected_body",cableDistance);
            }
            // Every stationary solid triangle is tested, including retained embedded hardware.
            // Only actual lens endpoints and explicitly nonphysical beam witnesses are excluded.
            foreach(var solid in stationary)
            {
                var inverse=solid.GlobalTransform.AffineInverse();var localFrom=inverse*from;var localTo=inverse*to;var triangles=solid.Mesh.GetFaces();
                for(var t=0;t+2<triangles.Length;t+=3)
                {
                    var hit=Geometry3D.SegmentIntersectsTriangle(localFrom,localTo,triangles[t],triangles[t+1],triangles[t+2]);
                    if(hit.VariantType==Variant.Type.Nil)continue;
                    stationaryOccluders.Add(new{name=solid.Name.ToString(),intersection=(solid.GlobalTransform*hit.AsVector3()).ToString(),bounds=ReviewBounds(solid).ToString()});break;
                }
            }
            Check(stationaryOccluders.Count==0,"actual_optical_ray_clear_of_stationary_solids",new{from=from.ToString(),to=to.ToString(),stationaryOccluders});
            var wood=palletMeshes.Where(m=>m.Name.ToString().StartsWith("PALLET_",StringComparison.Ordinal)&&!m.Name.ToString().Contains("nail",StringComparison.OrdinalIgnoreCase)).ToArray();
            var faces=wood.ToDictionary(m=>m,m=>m.Mesh.GetFaces());
            bool Hits(MeshInstance3D mesh,Vector3 start,Vector3 end)
            {
                var inverse=mesh.GlobalTransform.AffineInverse();var a=inverse*start;var b=inverse*end;var triangles=faces[mesh];
                for(var t=0;t+2<triangles.Length;t+=3)
                    if(Geometry3D.SegmentIntersectsTriangle(a,b,triangles[t],triangles[t+1],triangles[t+2]).VariantType!=Variant.Type.Nil)return true;
                return false;
            }
            bool Beam()=>wood.Any(m=>Hits(m,from,to));
            // Exact bottom-triangle section at Z0. Union translations that put any roller
            // centre under those real flat intervals: proves continuous point bearing,
            // not full-width capacity, stress, friction, or physical safety.
            List<(float left,float right)> Merge(IEnumerable<(float left,float right)> source)
            {
                var result=new List<(float left,float right)>();
                foreach(var v in source.OrderBy(v=>v.left))
                {if(result.Count==0||v.left>result[^1].right+1e-5f)result.Add(v);else result[^1]=(result[^1].left,Math.Max(result[^1].right,v.right));}
                return result;
            }
            var continuous=new List<object>();
            foreach(var runner in runners)
            {
                var triangles=faces[runner];var bottom=ReviewBounds(runner).Position.Y;var sections=new List<(float left,float right)>();
                for(var i=0;i+2<triangles.Length;i+=3)
                {
                    var vertices=new[]{runner.GlobalTransform*triangles[i],runner.GlobalTransform*triangles[i+1],runner.GlobalTransform*triangles[i+2]};
                    if(vertices.Any(v=>Math.Abs(v.Y-bottom)>1e-5f))continue;
                    var cut=new List<float>();
                    for(var e=0;e<3;e++)
                    {
                        var a=vertices[e];var b=vertices[(e+1)%3];
                        if(Math.Abs(a.Z)<1e-6f)cut.Add(a.X);
                        if(a.Z*b.Z<0)cut.Add(a.X+(b.X-a.X)*(-a.Z)/(b.Z-a.Z));
                    }
                    if(cut.Count>=2)sections.Add((cut.Min(),cut.Max()));
                }
                var actual=Merge(sections);var coverage=Merge(centres.SelectMany(c=>actual.Select(v=>(left:c.X-v.right,right:c.X-v.left))));
                var covers=coverage.Any(v=>v.left<=terminal+1e-5&&v.right>=home-1e-5);
                Check(covers,"continuous_real_bottom_z0_bearing_"+runner.Name,new{sections=actual.Select(v=>new{v.left,v.right}).ToArray(),coverage=coverage.Select(v=>new{v.left,v.right}).ToArray(),home,terminal});
                continuous.Add(new{runner=runner.Name.ToString(),covers,sections=actual.Select(v=>new{v.left,v.right}).ToArray()});
            }
            void Place(float x)=>pallet.Position=new Vector3(x,crown-palletBefore.Position.Y,0);
            double entry20=double.NaN,settled20=double.NaN;
            var contactMisses=0;var supportedSamples=0;var blockedSamples=0;var clearSamples=0;bool? previous=null;float segmentStart=home;
            for(float x=home;x>=terminal-.0001f;x-=.005f)
            {
                Place(Math.Max(x,terminal));supportedSamples++;
                foreach(var runner in runners)
                {
                    var rb=ReviewBounds(runner);
                    var contacts=centres.Where(c=>c.X>=rb.Position.X+.0005f&&c.X<=rb.End.X-.0005f)
                        .Count(c=>Hits(runner,new Vector3(c.X,crown+.004f,0),new Vector3(c.X,crown-.004f,0)));
                    if(contacts==0)contactMisses++;
                }
                var blocked=Beam();if(blocked)blockedSamples++;else clearSamples++;
                if(previous is not null&&blocked!=previous){intervals.Add(new{start=segmentStart,end=x,blocked=previous.Value});segmentStart=x;}
                previous=blocked;
            }
            intervals.Add(new{start=segmentStart,end=terminal,blocked=previous});
            Check(contactMisses==0,"actual_triangle_runner_contact_every_5mm",new{supportedSamples,contactMisses});
            Check(blockedSamples>0&&clearSamples>0,"real_wood_ray_has_entries_and_gaps",new{blockedSamples,clearSamples,triangles=faces.Sum(k=>k.Value.Length/3)});
            Place(home);Check(!Beam(),"reset_beam_clear");
            foreach(var dt in new[]{.020,.040,.100})
            {
                double x=home,speed=0;bool sawBeam=false;double first=double.NaN;int scans=0;bool supported=true;
                while(scans++<2000)
                {
                    var target=sawBeam?0:.45;var acceleration=Math.Sign(target-speed)*.60;var ramp=Math.Min(dt,Math.Abs(target-speed)/.60);
                    var distance=speed*ramp+.5*acceleration*ramp*ramp+target*(dt-ramp);speed+=acceleration*ramp;
                    x-=distance;Place((float)x);
                    supported&=x>=terminal-1e-6&&x<=home+1e-6;
                    if(!sawBeam&&Beam()){sawBeam=true;first=x;}
                    if(sawBeam&&Math.Abs(speed)<1e-9)break;
                }
                var settled=x;var settledBeam=Beam();
                if(dt==.020){entry20=first;settled20=settled;}
                for(var hold=0;hold<Math.Ceiling(.5/dt);hold++)Place((float)settled);
                var margin=settled-terminal;
                Check(sawBeam&&scans<2000&&supported&&margin>=.05,"event_off_coast_remains_supported_"+dt,new{first,settled,margin,settledBeam,scans,speed});
                travel.Add(new{dt,first,settled,margin,settledBeam,scans,speed,policy="geometry-only integral ramp .45/.60; next accepted command off; beam may clear"});
            }
            Place(home);
            var operatorBounds=ReviewMeshes(station).Select(ReviewBounds).Aggregate((a,b)=>a.Merge(b));
            var access=new Aabb(new Vector3(operatorBounds.GetCenter().X-.6f,0,operatorBounds.End.Z+.25f),new Vector3(1.2f,2,2));
            Check(!stationary.Any(m=>ReviewBounds(m).Intersects(access))&&!palletMeshes.Any(m=>ReviewBounds(m).Intersects(access)),"operator_standing_approach_outside_field",new{operatorBounds=operatorBounds.ToString(),access=access.ToString()});
            Check(!experiment.FindChildren("*","Node",true,false).Any(n=>n is RollerConveyorController or ConveyorController),"no_controller_or_autonomous_runtime_attached");
            Check(HashFile(rollerPath)==beforeRoller&&HashFile(palletPath)==beforePallet&&(externalPath is null||HashFile(externalPath)==beforeExternal),"original_delivery_bytes_unchanged");
            var output=evidencePath;
            if(_workspaceRollerGeometryCapture is null)File.WriteAllText(output,JsonSerializer.Serialize(new{readOnlyGeometry=true,ephemeralQA=true,currentDllSha256=DllHash(),installation=externalPair?"externalPair":"embeddedCorrected",assetSha=new{roller=beforeRoller,pallet=beforePallet,externalPair=beforeExternal},proposedMount,limits=new{home,terminal,crown,sideClearance},primitives=new{rollers=rollers.Length,woodMeshes=wood.Length,woodTriangles=faces.Sum(k=>k.Value.Length/3)},checks,intervals,travel,continuous,phaseSign,stationaryOccluders,failures},jsonOptions));
            if(_workspaceRollerGeometryCapture is not null)
            {
                if(failures>0||_capturePath is null||_workspaceRollerGeometryCapture is not ("home" or "entry" or "settled" or "optics"))
                    throw new InvalidOperationException("Geometry capture requires successful audit, supported state and --capture.");
                var state=_workspaceRollerGeometryCapture;
                Place(state=="entry"||state=="optics"&&!externalPair?(float)entry20:state=="settled"?(float)settled20:home);
                if(_sceneCompositionRoot is not null)_sceneCompositionRoot.Visible=false;
                if(_demoEnvironment is not null)_demoEnvironment.Visible=false;
                _sceneRuntime?.StopSimulation();_workspaceRollerGeometryCaptureRoot=experiment;
                _workspaceRollerOpticsTarget=(from+to)/2;
                _shellView="scene";_simulatorShell?.SetReviewState("scene",string.Empty);
                // Diagnostic ray between measured faces, not a manufactured PC signal.
                var line=new ImmediateMesh();line.SurfaceBegin(Mesh.PrimitiveType.Lines);line.SurfaceAddVertex(from);line.SurfaceAddVertex(to);line.SurfaceEnd();
                experiment.AddChild(new MeshInstance3D{Name="MeasuredOpticalRay",Mesh=line,MaterialOverride=new StandardMaterial3D{AlbedoColor=new Color("df5e29"),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded}});
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                ApplyWorkspaceRollerGeometryCaptureView();
                var layer=new CanvasLayer{Name="GeometryExperimentCaption",Layer=100};AddChild(layer);
                var caption=new Label{Position=_simulatorShell!.SceneViewportRect().Position+new Vector2(12,12),Text=$"EPHEMERAL GEOMETRY EXPERIMENT · {state}\nNo controller execution or learner workspace.\nUnit-scale pallet · { (externalPair?"external opposed pair":"corrected embedded optics") } · actual wood triangles\nCrown {crown:F3} m · centre X {pallet.Position.X:F4} m · beam intersection {Beam()}"};
                caption.AddThemeColorOverride("font_color",new Color("edf5f1"));caption.AddThemeColorOverride("font_shadow_color",new Color("15242a"));caption.AddThemeConstantOverride("shadow_offset_x",1);caption.AddThemeConstantOverride("shadow_offset_y",1);layer.AddChild(caption);
                File.WriteAllText(_capturePath+".roller-geometry.json",JsonSerializer.Serialize(new{state,readOnlyGeometry=true,ephemeralQA=true,controllerExecution=false,currentDllSha256=DllHash(),installation=externalPair?"externalPair":"embeddedCorrected",assetSha=new{roller=beforeRoller,pallet=beforePallet,externalPair=beforeExternal},palletCenter=pallet.Position.X,crown,phaseSign,ray=new{from=from.ToString(),to=to.ToString(),normal=normal.ToString(),woodIntersection=Beam()},limits=new{home,terminal},primitives=new{rollers=rollers.Length,woodTriangles=faces.Sum(k=>k.Value.Length/3)},geometryFailures=failures},jsonOptions));
                _workspaceRollerGeometryCapturePending=false;
                GD.Print("WORKSPACE_ROLLER_GEOMETRY_CAPTURE_READY state="+state+" readOnlyGeometry=true");
                GD.Print("WORKSPACE_ROLLER_GEOMETRY_RESULT failures="+failures);
                return;
            }
        }
        catch(Exception error)
        {
            failures++;GD.PushError("WORKSPACE_ROLLER_GEOMETRY_EXCEPTION "+error);
            try {File.WriteAllText(_workspaceRollerGeometryCapture is null?evidencePath:_capturePath+".roller-geometry.json",JsonSerializer.Serialize(new{readOnlyGeometry=true,ephemeralQA=true,currentDllSha256=DllHash(),installation=externalPair?"externalPair":"embeddedCorrected",assetSha=new{roller=beforeRoller,pallet=beforePallet,externalPair=beforeExternal},checks,intervals,travel,failures,exception=error.ToString(),originalBytesUnchanged=HashFile(rollerPath)==beforeRoller&&HashFile(palletPath)==beforePallet&&(externalPath is null||HashFile(externalPath)==beforeExternal)},jsonOptions)); }
            catch(Exception evidenceError){GD.PushError("WORKSPACE_ROLLER_GEOMETRY_EVIDENCE_FAILED "+evidenceError);}

        }
        GD.Print("WORKSPACE_ROLLER_GEOMETRY_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
    private void ApplyWorkspaceRollerGeometryCaptureView()
    {
        var camera=_sceneCamera??_mainCamera;
        if(_workspaceRollerGeometryCaptureRoot is null||camera is null)return;
        foreach(var path in new[]{"Workspace/LeftDock","Workspace/RightDock","Workspace/DiagnosticsDock","Workspace/TransportDock"})
            _simulatorShell?.GetNodeOrNull<Control>(path)?.Hide();
        if(_workspaceRollerGeometryCapture=="optics")
        {
            camera.KeepAspect=Camera3D.KeepAspectEnum.Height;
            camera.HOffset=0;camera.VOffset=0;
            camera.Position=_workspaceRollerOpticsTarget+new Vector3(1.2f,1.8f,3.2f);
            camera.LookAt(_workspaceRollerOpticsTarget,Vector3.Up);camera.Fov=42;
            // Synchronize the existing aperture projection; previous whole-frame offsets used a different distance.
            _cameraController?.CaptureCurrentView(_workspaceRollerOpticsTarget);
        }
        else FrameComposition(camera,_workspaceRollerGeometryCaptureRoot,new Vector3(7,5,7));
    }
}
