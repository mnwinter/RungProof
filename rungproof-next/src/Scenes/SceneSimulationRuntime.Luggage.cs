using System;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasLuggagePlant => RuntimeType == "luggageWeightSort";
    private Node3D? _luggageBag, _luggageGate;
    private MeshInstance3D? _luggageBase, _luggageDeck, _luggageBody;
    private Vector3[] _luggageBodyFaces=[];
    private MeshInstance3D[] _luggageMeshes=[], _luggageTx=[], _luggageRx=[];
    private EquipmentMotionController? _luggageRollers;
    private ConveyorController? _luggageInfeed, _luggageOutfeed;
    private float _luggageX, _luggageZ, _luggageMass, _luggageGuideProjection;
    private bool? _luggageRejectRoute;
    private bool LuggageFinished => AsBool(_points["bag_at_normal_exit"]) || AsBool(_points["bag_at_reject_exit"]);
    private bool LuggageCommandsOff => !AsBool(_points["infeed_run"]) && !AsBool(_points["discharge_run"]);
    private bool LuggageFixtureAvailable => Math.Abs(_luggageX+3.2f)<.001f || LuggageFinished;

    private void ResetLuggagePlant()
    {
        if(!HasLuggagePlant)return;
        _luggageBag=_sceneRoot.GetNode<Node3D>("training_accessory_5");
        _luggageGate=_sceneRoot.GetNode<Node3D>("training_accessory_7/LuggageGatePivot");
        _luggageRollers=_sceneRoot.GetNode<EquipmentMotionController>("training_accessory_7/LuggageRollerMotion");
        _luggageMeshes=_luggageBag.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
        _luggageBody=(MeshInstance3D)_luggageBag.FindChild("LUGGAGE_BODY",true,false);
        _luggageBodyFaces=_luggageBody.Mesh.GetFaces();
        _luggageBase=(MeshInstance3D)_luggageBag.FindChild("LUGGAGE_base",true,false);
        _luggageDeck=(MeshInstance3D)_sceneRoot.GetNode<Node3D>("training_accessory_4").FindChild("SCALE_DECK_surface",true,false);
        var sensors=new[]{"luggage_entry","luggage_station","luggage_normal_exit","luggage_reject_exit"}
            .Select(id=>_sceneRoot.GetNode<Node3D>(id)).ToArray();
        _luggageTx=sensors.Select(n=>(MeshInstance3D)n.FindChild("TX_lens",true,false)).ToArray();
        _luggageRx=sensors.Select(n=>(MeshInstance3D)n.FindChild("RX_lens",true,false)).ToArray();
        _luggageInfeed=_sceneRoot.GetNode<Node3D>("conveyor_0").FindChildren("*","",true,false).OfType<ConveyorController>().Single();
        _luggageOutfeed=_sceneRoot.GetNode<Node3D>("reject_outfeed").FindChildren("*","",true,false).OfType<ConveyorController>().Single();
        _luggageX=-3.2f;_luggageZ=0;_luggageRejectRoute=null;_luggageMass=(float)Convert.ToDouble(_points["fixture_mass_kg"],System.Globalization.CultureInfo.InvariantCulture);
        _luggageGate.Rotation=Vector3.Zero;
        // The prescribed reject path is conservative with respect to every
        // delivered suitcase corner, including handles and the baggage tag.
        var normal=new Basis(Vector3.Up,Mathf.DegToRad(55)).Z;
        var saved=_luggageBag.Position;_luggageBag.Position=Vector3.Zero;
        _luggageGuideProjection=_luggageMeshes.SelectMany(m=>Enumerable.Range(0,8).Select(i=> {
            var a=m.GetAabb();return m.GlobalTransform*(a.Position+a.Size*new Vector3((i&1)==0?0:1,(i&2)==0?0:1,(i&4)==0?0:1));
        })).Max(v=>v.Dot(normal));
        _luggageBag.Position=saved;
        _luggageRollers.RunCommand=false;
        FreezeLuggageAdapters();_luggageInfeed.ResetPlantTravel();_luggageOutfeed.ResetPlantTravel();ProjectLuggagePlant();
    }
    private void FreezeLuggageAdapters() { _luggageInfeed?.SetPhysicsProcess(false);_luggageOutfeed?.SetPhysicsProcess(false);_luggageRollers?.SetPhysicsProcess(false); }
    private void PauseLuggagePlant()
    {
        if(_luggageBag is null)return;
        _luggageRollers!.RunCommand=false;
        _luggageInfeed!.ApplyPlantTravel(0,0);_luggageOutfeed!.ApplyPlantTravel(0,0);
        SetPoint("bag_speed",0d);SetPoint("weight_valid",false);SetPoint("weight_kg",0d);ApplyBindings();StateChanged?.Invoke();
    }
    private bool LoadNextLuggage()
    {
        if(!LuggageFinished || !LuggageCommandsOff)return false;
        _luggageX=-3.2f;_luggageZ=0;_luggageRejectRoute=null;
        _luggageMass=(float)Convert.ToDouble(_points["fixture_mass_kg"],System.Globalization.CultureInfo.InvariantCulture);_luggageGate!.Rotation=Vector3.Zero;
        ProjectLuggagePlant();ApplyBindings();StateChanged?.Invoke();return true;
    }
    private void AdvanceLuggagePlant(double seconds)
    {
        var infeed=AsBool(_points["infeed_run"]);var discharge=AsBool(_points["discharge_run"]);
        var reject=AsBool(_points["reject_select"]);var oldX=_luggageX;var oldZ=_luggageZ;
        var inhibited=(infeed&&discharge) || ((infeed||discharge)&&AsBool(_points["weigh_cycle"]));
        var step=(float)(.5*Math.Max(0,seconds));
        if(!inhibited && infeed && _luggageX<0) {
            if(_luggageX<=-3.19f)_luggageMass=(float)Convert.ToDouble(_points["fixture_mass_kg"],System.Globalization.CultureInfo.InvariantCulture);
            _luggageX=Math.Min(0,_luggageX+step);
        }
        if(discharge && !inhibited) {
            // A discharge command must start with the bag at the weighing stop.
            // Route changes during travel are refused without rewriting commands.
            if(_luggageRejectRoute is null && Math.Abs(_luggageX)<.02f)_luggageRejectRoute=reject;
            inhibited=_luggageRejectRoute is null || _luggageRejectRoute!=reject || AsBool(_points["weigh_cycle"]);
            if(!inhibited) {
                _luggageGate!.Rotation=new(0,reject?Mathf.DegToRad(55):0,0);
                if(!reject)_luggageX=Math.Min(2.65f,_luggageX+step);
                else if(_luggageX<2.3f) {
                    _luggageX=Math.Min(2.3f,_luggageX+step);
                    var normal=_luggageGate.GlobalBasis.Z.Normalized();
                    _luggageZ=Math.Min(0,(_luggageGate.GlobalPosition.Dot(normal)-.055f-_luggageGuideProjection-_luggageX*normal.X)/normal.Z);
                } else _luggageZ=Math.Max(-3.03f,_luggageZ-step);
            }
        }
        ProjectLuggagePlant();
        var distance=Math.Sqrt(Math.Pow(_luggageX-oldX,2)+Math.Pow(_luggageZ-oldZ,2));
        SetPoint("bag_speed",seconds>0?distance/seconds:0d);SetPoint("motion_inhibited",inhibited);
        SetPoint("travel_limited",(discharge&&(_luggageX>=2.65f || _luggageZ<=-3.03f)) || (infeed&&_luggageX>=0));
        _luggageInfeed!.ApplyPlantTravel(_luggageX-oldX,infeed&&!inhibited?.5f:0);
        _luggageOutfeed!.ApplyPlantTravel(oldZ-_luggageZ,reject&&discharge&&!inhibited?.5f:0);
        _luggageRollers!.RunCommand=discharge && !inhibited && distance>0;_luggageRollers._PhysicsProcess(seconds);
        ApplyBindings();StateChanged?.Invoke();
    }
    private void ProjectLuggagePlant()
    {
        if(_luggageBag is null)return;
        _luggageBag.Position=new(_luggageX,.9f,_luggageZ);
        var names=new[]{"bag_at_entry","bag_at_scale","bag_at_normal_exit","bag_at_reject_exit"};
        for(var i=0;i<names.Length;i++) {
            var from=_luggageTx[i].GlobalTransform*_luggageTx[i].GetAabb().GetCenter();
            var to=_luggageRx[i].GlobalTransform*_luggageRx[i].GetAabb().GetCenter();
            SetPoint(names[i],LuggageBodyBlocksBeam(from,to));
        }
        var load=_luggageBase!.GlobalTransform*_luggageBase.GetAabb();
        var deck=_luggageDeck!.GlobalTransform*_luggageDeck.GetAabb();
        var supported=Math.Abs(load.Position.Y-deck.End.Y)<.005 && load.Position.X>=deck.Position.X && load.End.X<=deck.End.X
            && load.Position.Z>=deck.Position.Z && load.End.Z<=deck.End.Z;
        var valid=PlantPlaybackRunning && supported && AsBool(_points["weigh_cycle"]) && AsBool(_points["scale_ready"])
            && float.IsFinite(_luggageMass) && _luggageMass>=0 && !AsBool(_points["infeed_run"]) && !AsBool(_points["discharge_run"]);
        SetPoint("weight_valid",valid);SetPoint("weight_kg",valid?(double)_luggageMass:0d);
        SetPoint("bag_x",(double)_luggageX);SetPoint("bag_z",(double)_luggageZ);
        SetPoint("diverter_ready",_luggageRejectRoute is not null && _luggageRejectRoute==AsBool(_points["reject_select"]));
    }
    private bool LuggageBodyBlocksBeam(Vector3 from,Vector3 to)
    {
        var inverse=_luggageBody!.GlobalTransform.AffineInverse();from=inverse*from;to=inverse*to;
        var direction=to-from;
        for(var i=0;i<_luggageBodyFaces.Length;i+=3) {
            var a=_luggageBodyFaces[i];var e1=_luggageBodyFaces[i+1]-a;var e2=_luggageBodyFaces[i+2]-a;
            var cross=direction.Cross(e2);var determinant=e1.Dot(cross);if(MathF.Abs(determinant)<1e-8f)continue;
            var offset=from-a;var u=offset.Dot(cross)/determinant;if(u < -1e-6f || u>1.000001f)continue;
            var q=offset.Cross(e1);var v=direction.Dot(q)/determinant;if(v < -1e-6f || u+v>1.000001f)continue;
            var t=e2.Dot(q)/determinant;if(t>=0 && t<=1)return true;
        }
        return false;
    }

}
