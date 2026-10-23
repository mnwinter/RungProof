using System.Text.Json;
using RungProof.Next.Scenes;
var variants=new[]{"authored-double","authored-float32","float32-world-AABB"};
foreach(var variant in variants){
 double[] homes={-4.6,-5.8,-6.65}, lengths={1.3,.9,.6},widths={1,.8,.6},heights={1.1,.8,.5};
 if(variant!="authored-double")for(int i=0;i<3;i++){homes[i]=(float)homes[i];lengths[i]=(float)lengths[i];widths[i]=(float)widths[i];heights[i]=(float)heights[i];}
 if(variant=="float32-world-AABB")for(int i=0;i<3;i++){float x=(float)homes[i],half=(float)lengths[i]/2;lengths[i]=(float)((float)(x+half)-(float)(x-half));}
 ParcelTransferPlantModel Make()=>new(homes,lengths,widths,heights,.9,-2.7,-2.75,1.8,1.2,new[]{.245,.595,.995},new[]{4.935,10.635,4.935});
 var closed=Make();int opened=0;
 for(int i=0;i<4000;i++){closed.Advance(.02,true,true,true,false,0);if(closed.GateOpen)opened++;}
 Console.WriteLine(JsonSerializer.Serialize(new{variant,kind="never-opened-closed-command",opened,positions=closed.Parcels.Select(p=>p.X),fronts=closed.Parcels.Select(p=>p.X+p.HalfLength),lengths,homes}));
 // Direct floating boundary roundtrip for every actual authored length.
 foreach(var length in lengths){double half=length/2,center=-2.7-half,front=center+half;Console.WriteLine(JsonSerializer.Serialize(new{variant,kind="exact-closed-stop-roundtrip",length,center,front,strictOverlap=front> -2.7,error=front+2.7}));}
 // Mirror V4 phase/output ordering only; source model executes in isolation.
 var p=Make();int step=10,last=0,klass=0;bool reported=false;int priorCount=0;
 for(int scan=0;scan<5000;scan++){
   int snap=step;bool loaded=p.TableLoaded,clear=p.TableClear,aligned=p.TableAligned,gateClear=p.GateClear;
   int detected=p.DetectedClass;if(snap==10&&detected>0)klass=detected;
   bool enable=snap!=0&&!p.Complete,feed=snap is 10 or 20 or 30,gate=snap==20;
   bool transfer=((snap is 20 or 30)&&!loaded)||snap==50;
   double target=snap is 40 or 50?(klass-1)/2.0:0;
   if(snap==last){if(snap==10&&detected>0&&clear&&aligned)step=20;else if(snap==20&&gateClear)step=30;else if(snap==30&&loaded)step=40;else if(snap==40&&aligned)step=50;else if(snap==50&&clear)step=60;else if(snap==60&&aligned)step=10;}
   if(p.Complete)step=0;last=snap;
   var before=p.Parcels.Select(q=>new{q.X,q.HalfLength,q.Route}).ToArray();
   p.Advance(.02,enable,feed,transfer,gate,target);
   if(!gate&&p.GateOpen&&!reported&&p.Parcels.Skip(1).Any(q=>q.Route<0&&q.X+q.HalfLength> -2.7)){
    reported=true;Console.WriteLine(JsonSerializer.Serialize(new{variant,kind="v4-closed-gate-occupied",scan,seconds=scan*.02,snap,step,feed,transfer,target,gateClear,before,after=p.Parcels.Select(q=>new{q.X,q.HalfLength,q.Route,front=q.X+q.HalfLength,rear=q.X-q.HalfLength}),p.Inhibited}));}
   if(p.SortedCount!=priorCount){priorCount=p.SortedCount;Console.WriteLine(JsonSerializer.Serialize(new{variant,kind="arrival",scan,seconds=scan*.02,p.SortedCount,step}));}
 }
 Console.WriteLine(JsonSerializer.Serialize(new{variant,kind="v4-final",step,klass,p.SortedCount,p.TableLoaded,p.TableClear,p.TableAligned,p.TablePosition,p.Inhibited,p.Misroute,parcels=p.Parcels.Select(q=>new{q.X,q.Z,q.Route,q.Outbound,q.Arrived})}));
}
