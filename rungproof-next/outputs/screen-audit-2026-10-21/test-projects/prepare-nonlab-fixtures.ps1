$ErrorActionPreference='Stop'
function CopyJson($x){$x|ConvertTo-Json -Depth 35|ConvertFrom-Json}
$b=Get-Content "$PSScriptRoot/lab-4-02-counter-reset-lamp.rpproj.json" -Raw|ConvertFrom-Json
$script:uid=100
function Leaf($v,$nc=$false){$x=CopyJson $b.blocks[0].rungs[0].root.children[0];$script:uid++;$x.id="node-$script:uid";$x.variable=$v;$x.normallyClosed=$nc;return $x}
function Tree($kind,$nodes){$x=CopyJson $b.blocks[0].rungs[0].root;$script:uid++;$x.id="node-$script:uid";$x.kind=$kind;$x.children=@($nodes);return $x}
function Network($coil,$root){$x=CopyJson $b.blocks[0].rungs[2];$script:uid++;$x.id="rung-$script:uid";$x.terminalActionId="$($x.id)-action";$x.coilVariable=$coil;$x.root=$root;$x.label="QA $coil";return $x}
$entries=@()
foreach($id in @('conveyor-cell','scene-1-conveyor-stop','tank-radar')){
 $j=CopyJson $b;$j.id="qa-learner-$id";$j.name="QA_$id";$j.sourceSceneId=$id;$j.blocks[0].name=$j.name
 $pts=(Get-Content "$PSScriptRoot/../../../scenes/migrated/$id.scene.json" -Raw|ConvertFrom-Json).simulation.points
 $tags=@();foreach($pt in @($pts|Where-Object owner -in @('PC','PLC'))){$t=CopyJson $b.tags[0];$script:uid++;$t.symbolId="symbol-$script:uid";$t.name=$pt.name;$t.binding=$pt.name;$t.type=@{BOOL='Bool';DINT='DInt';REAL='Real'}[$pt.type];$t.initial=$pt.initial;$t.role=if($pt.owner -eq 'PLC'){'Output'}else{'Input'};$tags+=$t}
 if($id -ne 'tank-radar'){
  foreach($command in @('start','stop')){$t=CopyJson $b.tags[0];$script:uid++;$t.symbolId="symbol-$script:uid";$t.name="operator_$command";$t.binding="operator.$command";$t.comment='Explicit scene controller command binding, not invented PC feedback';$tags+=$t}
  $out=if($id -eq 'conveyor-cell'){'conveyor_run'}else{'conveyor_running'}
  $parts=@((Leaf operator_stop $true),(Tree 3 @((Leaf operator_start),(Leaf $out))))
  if($id -eq 'scene-1-conveyor-stop'){$parts+=Leaf simulated_photoeye $true}
  $rs=@((Network $out (Tree 2 $parts)))
 }else{
  $rs=@((Network inlet_pump_run (Tree 2 @((Leaf cycle_request),(Leaf manual_drain_request $true)))),(Network drain_valve_open (Tree 2 @((Leaf cycle_request),(Leaf manual_drain_request)))))
 }
 $j.tags=$tags;$j.blocks[0].rungs=$rs;$j.watchVariables=@($tags.name);$j.watchTables=@();$j.nextId=2000
 $leaf="fixture-$id-normal.rpproj.json";$p="$PSScriptRoot/$leaf";if(Test-Path $p){throw "Refuse overwrite $p"}
 $j|ConvertTo-Json -Depth 35|Set-Content $p -Encoding utf8;Get-Content $p -Raw|ConvertFrom-Json|Out-Null
 $entries+=[pscustomobject]@{file=$leaf;sourceSceneId=$id;sha256=(Get-FileHash $p -Algorithm SHA256).Hash.ToLower();status='NOT_UI_LOADED_OR_VALIDATED';networks=$rs.Count}
}
[pscustomobject]@{purpose='Static learner test inputs; JSON syntax only checked, no compiler/runtime/API execution';entries=$entries}|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/nonlab-fixture-manifest.json" -Encoding utf8
