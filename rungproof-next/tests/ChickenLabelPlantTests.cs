using System;
using RungProof.Next.Scenes;
internal static class ChickenLabelPlantTests
{
 static int Main()
 {
  var m=new ChickenLabelPlantModel();m.Reset();m.LoadProduct();
  void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
  void Step(double t,bool print=false,string text="",bool apply=false,bool ready=true)=>m.Advance(t,print,text,apply,ready);
  Check(m.ProductPresent&&!m.WeighStable&&m.MeasuredKg==0,"reset measurement empty");
  Step(.49);Check(!m.WeighStable,"measurement not early");Step(.01);Check(m.WeighStable&&m.MeasuredKg==1.237,"measurement after settle");
  Step(0,true,"lot A",false,false);Check(!m.Printing,"printer permissive required");
  Step(0,true,"lot A");Check(!m.Printing,"held rejected edge not retried");Step(0);Step(0,true,"lot A");Check(m.Printing&&m.PrintedLabelText=="lot A","accepted text captured");
  Step(.59,true,"changed");Check(!m.PrintComplete&&m.PrintedLabelText=="lot A","text immutable and print not early");Step(.01,true,"changed");Check(m.PrintComplete&&!m.ApplicationComplete,"print completion separate");
  Step(1,true,"changed");Check(!m.Applying,"no automatic apply");Step(0,false,"",true);Step(.49,false,"",true);Check(!m.ApplicationComplete,"apply not early");Step(.01,false,"",true);Check(m.ApplicationComplete&&m.ApplyFraction==1,"application complete");
  m.Reset();Step(0,false,"",true);Check(!m.Applying&&!m.PrintComplete,"apply requires print and reset clears job");
  m.UnloadProduct();Step(1);Check(!m.ProductPresent&&!m.WeighStable&&m.MeasuredKg==0,"unloaded measurement absent");m.LoadProduct(2);Step(.5);Check(m.MeasuredKg==2,"fixture mass measured");
  foreach(var bad in new[]{double.NaN,double.PositiveInfinity,-1d}){bool rejected=false;try{m.LoadProduct(bad);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid mass rejected");}
  bool longRejected=false;try{Step(0,true,new string('x',256));}catch(ArgumentException){longRejected=true;}Check(longRejected,"text limit enforced");
    m.Reset();Check(!m.ProductPresent&&!m.WeighStable&&m.PrintedLabelText=="","reset absent home");
  m.LoadProduct();Step(.5);Step(0,true," exact text ");Step(.2,true,"other");var held=m.PrintFraction;Step(1,true,"other",false,false);Check(m.Printing&&m.PrintFraction==held,"printer loss pauses actual printing");
  bool busyRejected=false;try{m.LoadProduct();}catch(InvalidOperationException){busyRejected=true;}Check(busyRejected,"active product overwrite blocked");Step(.4,true,"other");Check(m.PrintComplete&&m.PrintedLabelText==" exact text ","ready resumes immutable print");
  Step(0,true,"new");Check(m.PrintedLabelText==" exact text ","completed job not duplicated");
  foreach(var bad in new[]{double.NaN,double.PositiveInfinity,-.1}){bool rejected=false;try{Step(bad);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid delta rejected");}
  Console.WriteLine("CHICKEN_LABEL_PLANT_TESTS_PASS");return 0;
 }
}
