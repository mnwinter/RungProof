$ErrorActionPreference='Stop'
function CopyJson($x){$x|ConvertTo-Json -Depth 35|ConvertFrom-Json}
$b=Get-Content "$PSScriptRoot/lab-4-02-counter-reset-lamp.rpproj.json" -Raw|ConvertFrom-Json
$numeric=(Get-Content "$PSScriptRoot/lab-4-05-dual-input-count-window-v2.rpproj.json" -Raw|ConvertFrom-Json).blocks[0].rungs|Where-Object isNumericOperation|Select-Object -First 1
$script:uid=100
function Leaf($v,$nc=$false){$x=CopyJson $b.blocks[0].rungs[0].root.children[0];$script:uid++;$x.id="node-$script:uid";$x.variable=$v;$x.normallyClosed=$nc;return $x}
function Tree($kind,$nodes){$x=CopyJson $b.blocks[0].rungs[0].root;$script:uid++;$x.id="node-$script:uid";$x.kind=$kind;$x.children=@($nodes);return $x}
function LessTwo{$x=Leaf occupancy_count;$x.kind=1;$x.compareOperator=4;$x.rightOperand='2';return $x}
function Network($coil,$root){$x=CopyJson $b.blocks[0].rungs[2];$script:uid++;$x.id="rung-$script:uid";$x.terminalActionId="$($x.id)-action";$x.coilVariable=$coil;$x.root=$root;$x.label="QA feedback $coil";return $x}
function CounterNetwork($name,$point,$preset,$db){$x=CopyJson $b.blocks[0].rungs[0];$script:uid++;$x.id="rung-$script:uid";$x.terminalActionId="$($x.id)-action";$x.counterVariable=$name;$x.counterPreset=$preset;$x.counterInstanceDataBlock.number=$db;$x.counterInstanceDataBlock.name="QA_DB$db";$x.root=Tree 2 @((Leaf $point));$x.label="Actual $point edges";return $x}
function NumericNetwork($op,$a,$bb,$dest){$x=CopyJson $numeric;$script:uid++;$x.id="rung-$script:uid";$x.terminalActionId="$($x.id)-action";$x.numericOperationKind=$op;$x.numericSourceA=$a;$x.numericSourceB=$bb;$x.numericDestination=$dest;$x.root=Tree 2 @((Leaf publish));$x.label="Publish $dest";return $x}
$entries=@()
foreach($kind in @('parking','grouping')){
 $id=if($kind -eq 'parking'){'lab-4-07-parking-garage-entry'}else{'lab-4-08-package-grouping'}
 $j=CopyJson $b;$j.id="qa-learner-$id";$j.name="QA_$kind";$j.sourceSceneId=$id;$j.blocks[0].name=$j.name
 $pts=(Get-Content "$PSScriptRoot/../../../scenes/migrated/$id.scene.json" -Raw|ConvertFrom-Json).simulation.points
 $tags=@();foreach($pt in $pts){$t=CopyJson $b.tags[0];$script:uid++;$t.symbolId="symbol-$script:uid";$t.name=$pt.name;$t.binding=$pt.name;$t.type=@{BOOL='Bool';DINT='DInt';REAL='Real'}[$pt.type];$t.initial=$pt.initial;$t.role=if($pt.owner -eq 'PLC'){'Output'}else{'Input'};$tags+=$t}
 $mem=if($kind -eq 'parking'){@('arrivals','departures','publish')}else{@('arrivals','release_active','publish')}
 foreach($name in $mem){$t=CopyJson $b.tags[3];$script:uid++;$t.symbolId="symbol-$script:uid";$t.name=$name;$t.type=if($name -in @('arrivals','departures')){'Counter'}else{'Bool'};$t.initial=if($name -eq 'publish'){$true}elseif($t.type -eq 'Bool'){$false}else{0};$tags+=$t};$j.tags=$tags
 if($kind -eq 'parking'){
  $rs=@((CounterNetwork arrivals entry_passed 2 107),(CounterNetwork departures exit_passed 2 108),(NumericNetwork Subtract arrivals.ACC departures.ACC occupancy_count))
  $branches=@((Tree 2 @((Leaf machine_enabled),(Leaf exit_clear),(Leaf entry_detected),(LessTwo))),(Tree 2 @((Leaf machine_enabled),(Leaf exit_clear),(Leaf exit_requested))),(Tree 2 @((Leaf machine_enabled),(Leaf passage_occupied))))
  $rs+=Network barrier_open (Tree 3 $branches)
  $rs+=Network vehicle_run (Tree 2 @((Leaf machine_enabled),(Leaf exit_clear)))
  $rs+=Network garage_available (Tree 2 @((Leaf machine_enabled),(Leaf space_available),(LessTwo)))
 }else{
  $rs=@((CounterNetwork arrivals package_detected 3 109),(NumericNetwork Move arrivals.ACC '' group_count))
  $x=Network release_active (Tree 2 @((Leaf machine_enabled),(Leaf release_clear),(Leaf arrivals.DN),(Leaf group_staged)));$x.coilMode='Set';$rs+=$x
  $x=Network release_active (Tree 2 @((Leaf transfer_complete)));$x.coilMode='Reset';$rs+=$x
  $rs+=Network group_conveyor_run (Tree 2 @((Leaf machine_enabled),(Leaf release_clear),(Leaf transfer_complete $true)))
  $rs+=Network group_release (Tree 2 @((Leaf machine_enabled),(Leaf release_clear),(Leaf release_active)))
  $rs+=Network group_ready (Tree 2 @((Leaf arrivals.DN),(Leaf transfer_complete $true)))
 }
 $j.blocks[0].rungs=$rs;$j.watchVariables=@($tags.name);$j.watchTables=@();$j.nextId=2000
 $leaf="fixture-$id-feedback.rpproj.json";$p="$PSScriptRoot/$leaf";if(Test-Path $p){throw "Refuse overwrite $p"}
 $j|ConvertTo-Json -Depth 35|Set-Content $p -Encoding utf8;Get-Content $p -Raw|ConvertFrom-Json|Out-Null
 $entries+=[pscustomobject]@{file=$leaf;sourceSceneId=$id;sha256=(Get-FileHash $p -Algorithm SHA256).Hash.ToLower();status='NOT_UI_LOADED_OR_VALIDATED';networks=$rs.Count}
}
[pscustomobject]@{purpose='Static declarative translation only; no compiler/runtime/backend execution';entries=$entries}|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/additional-parking-grouping-manifest.json" -Encoding utf8
