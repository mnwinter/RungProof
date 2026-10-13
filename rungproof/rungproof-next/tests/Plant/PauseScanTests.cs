using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.VirtualController;
internal static class Program
{
    static int checks;
    static void Check(bool condition,string name){checks++;if(!condition)throw new Exception(name);}
    static VirtualControllerSession Session(int ms)
    {
        var p=new LadderProgram(1,"pause-test","Pause test","LD",TimeSpan.FromMilliseconds(ms),
            [new("input",PlcVariableType.Bool,PlcVariableRole.Input,false),new("output",PlcVariableType.Bool,PlcVariableRole.Output,false)],
            [new("network","Network",new("contact",LadderNodeKind.Contact,"input"),new LadderCoil("coil","output"))]);
        return new(new VirtualControllerRuntime(LadderCompiler.Compile(p).Program!));
    }
    sealed class Fixture
    {
        public readonly VirtualControllerSession S;public int Samples,Commits,Plants;public double Travel;public bool Beam;public readonly List<string> Order=[];
        readonly Dictionary<string,bool> inputs=new();
        public Fixture(int ms){S=Session(ms);S.ScanCompleted+=_=>Order.Add("event");}
        public IReadOnlyDictionary<string,bool> Sample(){Samples++;Order.Add("sample");inputs["input"]=Beam;return inputs;}
        public IReadOnlyDictionary<string,double> Numbers()=>new Dictionary<string,double>();
        public void Commit(IReadOnlyDictionary<string,bool> o){Commits++;Order.Add("commit");}
        public void Plant(double dt){Plants++;Travel+=dt;Beam=true;Order.Add("plant");}
        public VirtualControllerSignalImage Post(){Order.Add("post");return new(new Dictionary<string,bool>{{"input",Beam}},new Dictionary<string,double>(),new Dictionary<string,string>());}
        public int Advance(double dt)=>S.Advance(dt,Sample,Numbers,Commit,_=>{},Plant,samplePostPlantInputs:Post);
        public int Step()=>S.StepOneAcceptedScan(Sample,Numbers,Commit,_=>{},Plant,samplePostPlantInputs:Post);
    }
    static void Main()
    {
        foreach(int ms in new[]{20,40,100})
        {
            double dt=ms/1000.0;var f=new Fixture(ms);f.S.Run();f.Advance(dt/2);f.S.PulseInput("input");f.S.Pause();
            Check(f.Advance(10000)==0&&f.Samples==0&&f.Plants==0,"paused wall time inert "+ms);
            Check(f.S.TryGetPendingInput("input",out var pending)&&pending,"pending pulse visible");
            var pendingCopy=f.S.PendingInputs;Check(f.Step()==1,"one step accepted");
            Check(f.S.IsPaused&&f.S.Snapshot.ScanNumber==1&&f.S.Snapshot.SimulatedTime==TimeSpan.FromMilliseconds(ms),"step period and latch");
            Check(f.Order.SequenceEqual(new[]{"sample","commit","plant","post","event"}),"accepted ordering");
            Check(f.S.History[0].CompletedScan.SampledInputs.Bools["input"]&&f.S.History[0].PostPlantInputs.Bools["input"],"sample and post separate");
            Check(f.S.TryGetPendingInput("input",out pending)&&!pending&&pendingCopy["input"],"single pulse consumed copied pending remains");
            var old=f.S.History;f.Beam=false;f.Step();Check(old.Count==1&&old[0].CompletedScan.ScanNumber==1,"history copy remains immutable");
            Check(!f.S.History[1].CompletedScan.SampledInputs.Bools["input"]&&f.S.History[1].PostPlantInputs.Bools["input"],"postfeedback not sampled same scan");
            f.S.Run();Check(f.Advance(dt/2)==1,"pre-pause fraction retained across manual steps");
            Check(f.S.Snapshot.ScanNumber==3,"resume no manualstep backlog injection");
            f.S.Pause();for(int i=0;i<130;i++)f.Step();Check(f.S.History.Count==128&&f.S.HistoryDroppedCount==5,"bounded ring drops exact rows");
            var before=f.S.Snapshot.ScanNumber;f.S.Stop();Check(f.S.History.Count==128&&!f.S.IsPaused&&f.Step()==0,"Stop preserves historical rows rejectsStep");
            f.S.Reset();Check(f.S.History.Count==0&&f.S.HistoryDroppedCount==0&&f.S.Snapshot.ScanNumber==0,"Reset clears history");
            var enabled=new Fixture(ms);var disabled=new Fixture(ms);disabled.S.HistoryRecordingEnabled=false;enabled.S.Run();disabled.S.Run();enabled.S.Pause();disabled.S.Pause();for(int i=0;i<15;i++){enabled.Step();disabled.Step();}
            Check(enabled.Travel==disabled.Travel&&enabled.S.Snapshot.SimulatedTime==disabled.S.Snapshot.SimulatedTime&&enabled.S.Snapshot.Outputs["output"]==disabled.S.Snapshot.Outputs["output"]&&disabled.S.History.Count==0,"recording disabled trace parity");
        }
        foreach(string change in new[]{"stop","pause","stop-run","reset-run"})
        {
            var s=Session(20);s.Run();s.Pause();int commits=0,plants=0;
            int accepted=s.StepOneAcceptedScan(()=>{if(change=="stop")s.Stop();else if(change=="pause")s.Pause();else if(change=="stop-run"){s.Stop();s.Run();}else{s.Reset();s.Run();}return new Dictionary<string,bool>();},()=>new Dictionary<string,double>(),_=>commits++,_=>{},_=>plants++);
            Check(accepted==0&&commits==0&&plants==0&&s.History.Count==0,"sampling lifecycle generation rejection "+change);
        }
        var auto=new Fixture(20);auto.S.Run();Check(auto.Step()==0,"step rejects nonpaused");auto.Advance(.02);auto.S.Pause();Check(auto.S.Snapshot.Outputs["output"]==false,"pause retains prior image");
        var paused=new Fixture(20);paused.S.Run();paused.S.SetBoolForce("output",true);var image=paused.S.Snapshot;paused.S.Pause();Check(paused.S.Snapshot.Outputs["output"]&&paused.S.Snapshot.Forces.Count==image.Forces.Count,"Pause preserves output forces");
        var b=new Fixture(20);b.S.Run();Check(b.Advance(20.01)==1000,"advance bounded backlog");b.S.Pause();b.Step();b.S.Run();Check(b.Advance(.01)==1,"backlog retained independent manualstep");
        Console.WriteLine("PAUSE_SCAN_PURE_PASS checks="+checks+" periods=20/40/100ms");
    }
}
