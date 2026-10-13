$root = 'C:/Users/mattn/Documents/Codex/Worktrees/rungproof-studio-20261010/rungproof'
$app = "$root/rungproof-next"
$docs = "$root/docs/studio-execution/2026-10-10"
$catalog = Get-Content "$app/scenes/catalog/original-scenes.catalog.json" -Raw | ConvertFrom-Json
$baseline = Get-Content "$docs/industrial-mount-source-freeze-v12.json" -Raw | ConvertFrom-Json
$dFreeze = Get-Content "$docs/industrial-mount-source-freeze-v13-d.json" -Raw | ConvertFrom-Json
$eFreeze = Get-Content "$docs/industrial-mount-source-freeze-v13-e.json" -Raw | ConvertFrom-Json
$manifest = Get-Content "$app/.tools/studio-verification/all77-realism-current-v12/capture-manifest.json" -Raw | ConvertFrom-Json
$dFrames = Get-Content "$docs/cabinet-legend-frame-identities-v13-d.json" -Raw | ConvertFrom-Json
$eFrames = Get-Content "$docs/identification-support-frame-identities-v13-e.json" -Raw | ConvertFrom-Json
$controlPath = "$app/.tools/studio-verification/control-installations-v13-e.log"
$controlLines = Get-Content $controlPath
$open = @{}
function Add-Requirement($ids,$text) { foreach($id in $ids) { $open[$id]=@($open[$id]) + $text } }
Add-Requirement @('lab-2-13-coolant-jug-fill','lab-2-14-sump-pump','lab-2-17-pallet-robot','lab-2-20-bottle-shuttle','lab-2-21-tote-finishing','lab-2-22-dual-spindle','lab-2-23-parcel-sorter','lab-2-24-robot-cnc','lab-2-18-pallet-pickup','scene-1-conveyor-stop','scene-2-conveyor-pusher') 'V13f approved identification plan pending implementation/native/operator acceptance; E does not close remaining station labels/mode/toggle role.'
Add-Requirement @('lab-2-19-service-door','lab-5-09-bag-indexing-conveyor','lab-6-08-hand-dryer') 'Six raw-input/output-command pilot labels planned; final geometry/native/operator evidence pending.'
Add-Requirement @('lab-11-12-mobile-traffic-lights') 'Front-face detail must establish both supported heads and all six lamp faces.'
Add-Requirement @('lab-2-16-safe-drill') 'Both field hand-control identities need actual detail evidence.'
Add-Requirement @('lab-2-21-tote-finishing') 'Process inspection annotation readability needs field detail.'
Add-Requirement @('lab-4-11-barrel-fill-station') 'Process meter/source/nozzle identity and projection need detail.'
Add-Requirement @('tank-high-low','tank-level','tank-radar') 'Close roof/side instrument support/identity evidence required.'
Add-Requirement @('conveyor-cell','lab-10-01-drive-alarm-code-string','lab-10-02-chicken-label-print','lab-10-03-vision-package-sorter','lab-10-04-motor-enum-state') 'Connected annex/recess purpose largely hidden in baseline: require visible depth/task context; material rollout separately held.'
Add-Requirement @('lab-2-04-two-station-call') 'North/south station associations need visibly distinct context if claiming two purposeful work areas.'
Add-Requirement @('lab-9-12-ev-charging-manager') 'Bay A/B energy-readout association must be understandable; historical detail proves digits only.'
Add-Requirement @('lab-10-02-chicken-label-print') 'Retain honest distinction between static scale artwork and floating observed/simulated caption.'
$reviews=@('environment-all77-dot-render-review-v12-part1.md','environment-all77-render-review-v12-part2.md','environment-all77-render-review-v12-part3.md')
$rows=@();$index=0
foreach($entry in $catalog.scenes) {
  $index++; $relative=$entry.path.Replace('res://','');$scene=Get-Content "$app/$relative" -Raw|ConvertFrom-Json
  $hash=(Get-FileHash "$app/$relative").Hash
  $frozen=@($baseline.source|Where-Object {$_.path.Replace('\','/') -eq $relative})
  if($frozen.Count -ne 1){throw "Missing unique V12 definition: $relative"}
  $report=$reviews[[Math]::Min(2,[Math]::Floor(($index-1)/26))]
  $reviewText=Get-Content "$docs/$report"
  $observations=@($reviewText|Where-Object {$_ -like "|*$($entry.id)*|*"})
  $whole=@($manifest|Where-Object {$_.sceneId -eq $entry.id}|ForEach-Object {
    [ordered]@{view=$_.view;image=$_.image;sha256=$_.sha256;identity=$_.identity;candidate=$baseline.assemblySha256;reviewReport="$docs/$report";historical=$true}
  })
  if($whole.Count -ne 2){throw "Missing whole/split pair $($entry.id)"}
  $later=@()
  foreach($version in @('D','E')){
    $frames=if($version -eq 'D'){$dFrames}else{$eFrames};$freeze=if($version -eq 'D'){$dFreeze}else{$eFreeze}
    foreach($frame in @($frames|Where-Object {$_.scene -eq $entry.id})) {
      $later+= [ordered]@{image=$frame.file;sha256=$frame.sha256;view=$(if($frame.view){$frame.view}else{'operator'});candidate=$freeze.assemblySha256;version="V13-$($version.ToLower())";hashMatchesNow=((Get-FileHash -LiteralPath $frame.file).Hash -eq $frame.sha256);reportIndex="$docs/environment-bounded-repair-checkpoint-v13-e.md";notRelabeled=$true}
    }
  }
  $case=@();for($line=0;$line -lt $controlLines.Count;$line++){if($controlLines[$line] -match ('^CONTROL_INSTALLATION_CASE PASS '+[regex]::Escape($entry.id)+' ')){$case+=[ordered]@{line=$line+1;text=$controlLines[$line]}}}
  if($case.Count -ne 1){throw "Missing unique native control case $($entry.id)"}
  $rows+=[ordered]@{index=$index;sceneId=$entry.id;name=$entry.name;equipmentIds=@($scene.equipment.id);definition=[ordered]@{path="$app/$relative";sha256AtLedger=$hash;v12Sha256=$frozen[0].sha256;matchesV12=($hash -eq $frozen[0].sha256);scope='definition equality does not establish composer/environment equality'};status='IN_PROGRESS_BOUNDED_EVIDENCE_NEEDS_FINAL_DELTA_REVIEW';nativeControls=[ordered]@{log=$controlPath;candidate=$eFreeze.assemblySha256;case=$case[0];scope='scripted mounted control case only'};v12WholeSplit=$whole;laterDE=$later;openRequirements=@($open[$entry.id]|Where-Object {$_});purposeVarietyReview=[ordered]@{baselineReport="$docs/$report";actualBaselineObservation=$observations;status='REVIEWED_WITH_LIMITS_NOT_AUTOMATIC_FINAL_PASS';retainedLimits='Assess purposeful visible location and variety against baseline; held material changes are not authorized by this ledger.'};finalDeltaReview='Required: compare composition/environment source and changed geometry evidence, preserve field inventory and versioned acceptance.'}
}
if($rows.Count -ne 77 -or @($rows.sceneId|Sort-Object -Unique).Count -ne 77){throw '77 unique scene validation failed'}
$packet=[ordered]@{status='IN_PROGRESS';version=1;qualifiedCheckpoint='V13-e';checkpointCandidate=$eFreeze.assemblySha256;rowCount=77;globalGates=[ordered]@{material75='HELD';handle='OPEN: environment77 terminal geometry PASS with40 exceptions is error-tainted';humanFinalApproval=$false;handsOn='not evidenced';overallGoal='ACTIVE all77, not narrowed to caption/support subset'};closureAudit="$docs/environment-dot-all77-remaining-closure-audit-v13-e.md";rows=$rows}
$packet|ConvertTo-Json -Depth 18|Set-Content -LiteralPath "$docs/environment-all77-authoritative-closure-ledger-v13-e.json" -Encoding utf8
$md=@('# All77 authoritative closure ledger — IN_PROGRESS','','V13-e bounded checkpoint; no row grants final global acceptance. Material75 HELD; Handle OPEN; final human approval false. JSON contains exact equipment IDs, hashes, case log lines, versioned image/identity paths and scene observations. Definition equality alone does not qualify presentation code.','','| # | Exact scene | Equipment | Definition vs V12 | D/E frames | Explicit open requirements |','|---|---|---|---|---|---|')
foreach($row in $rows){$md+="| $($row.index) | $($row.sceneId) | $($row.equipmentIds.Count) | $($row.definition.matchesV12) | $($row.laterDE.Count) | $($row.openRequirements.Count); final delta/purpose review pending |"}
$md -join [Environment]::NewLine|Set-Content -LiteralPath "$docs/environment-all77-authoritative-closure-ledger-v13-e.md" -Encoding utf8
"Validated rows=$($rows.Count), unique=$(@($rows.sceneId|Sort-Object -Unique).Count), native cases77; historical pairs154; laterD/E hashes checked."
