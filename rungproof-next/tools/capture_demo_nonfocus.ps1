[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$SceneId,
    [Parameter(Mandatory = $true)][string]$CapturePath,
    [ValidateSet('split','scene')][string]$View = 'split',
    [ValidateSet('','intended','hint','history','invalid','request-count','preset-two','held-active')][string]$RepeatLearnerState='',
    [ValidateSet('','ev-unequal','load','weighing','stable','changed-mass','reset')][string]$ReadoutState='',
    [switch]$BarrelValveOpen,
    [switch]$UserReadoutNavigation,
    [switch]$CollectiveReadouts,
    [ValidateSet('','manual','numeric','results','status')][string]$ReadoutGroup='',
    [ValidateSet('','numeric_display_0','numeric_display_1','numeric_display_2')][string]$NumericCycleInput='',
    [string]$RetainedControl='',
    [switch]$RunDemo,
    [switch]$Challenge,
    [ValidateSet('running','stopped')][string]$State = 'running',
    [ValidateSet('teaching','operator','side','motor','machine-support','mount-detail','ev-readouts','accessory-support','field-detail','field-reverse','field-meter','field-source','field-face','field-joints','purpose-detail','purpose-exterior')][string]$StationView = 'teaching',
    [ValidateSet('','all-running','blocked-downstream','final-beam-stop','invalid-current')][string]$TenZoneState = '',
    [switch]$TenZoneDetail,
    [switch]$TemplateEditCapture,
    [switch]$TemplateEditDetail,
    [switch]$TemplateEditorDetail,
    [ValidateSet('','valid','invalid')][string]$TemplateDraftState='',
    [switch]$DeviceTraceCapture,
    [ValidateSet('','true','false')][string]$IndexedTraceState='',
    [ValidateSet('','intended','wrong-terminal','invalid-current')][string]$WorkspaceBeltState='',
    [switch]$WorkspaceBeltDetail,
    [switch]$WorkspaceBeltObjective,
    [ValidateSet('','intended','coasting','wrong-terminal','invalid-current')][string]$WorkspaceRollerState='',
    [switch]$WorkspaceRollerDetail,
    [switch]$WorkspaceRollerObjective,
    [ValidateSet('','initial','carry','completed','wrong-carry','invalid-current')][string]$Demo5LearnerState='',
    [ValidateSet('','timer','carry','coast','history','invalid')][string]$ScanInspectionState='',
    [ValidateSet('','home','entry','settled','optics')][string]$WorkspaceRollerGeometryState='',
    [switch]$WorkspaceRollerExternalGeometry,
    [ValidateSet('1600x900','1280x720')][string]$Viewport = '1600x900',
    [ValidateSet('','intended','wrong','invalid','history')][string]$TypedLearnerState='',
    [ValidateSet('','eligible','connected','intended','wrong','invalid','history')][string]$WorkspaceDriveState='',
    [switch]$TypedLearnerDetail,
    [switch]$VerifyTypedLabelPresentation,
    [switch]$WithoutEnvironment,
    [string[]]$SceneActions = @())
if ($ReadoutState -and ($State -ne 'stopped' -or $RunDemo -or $Challenge -or $TypedLearnerState -or $SceneActions.Count -gt 0)) { throw 'Readout QA requires stopped isolated capture.' }
if ($BarrelValveOpen -and ($SceneId -cne 'lab-4-11-barrel-fill-station' -or $StationView -ne 'field-joints' -or $State -ne 'stopped' -or $RunDemo -or $Challenge)) { throw 'Barrel pointer QA requires exact stopped joint view.' }
if ($ReadoutState -and (($ReadoutState -eq 'ev-unequal' -and $SceneId -cne 'lab-9-12-ev-charging-manager') -or ($ReadoutState -ne 'ev-unequal' -and $SceneId -cne 'lab-10-02-chicken-label-print'))) { throw 'Readout QA scene mismatch.' }
if ($UserReadoutNavigation -and ((!$NumericCycleInput -and !$RetainedControl -and !$CollectiveReadouts -and $ReadoutState -notin @('ev-unequal','stable')) -or $StationView -ne 'teaching' -or $WithoutEnvironment -or $TypedLearnerDetail -or $BarrelValveOpen)) { throw 'Ordinary readout navigation requires an isolated normal teaching view and explicit readout fixture.' }
if ($NumericCycleInput -and (!$UserReadoutNavigation -or $State -ne 'stopped' -or $ReadoutState -or $RunDemo -or $Challenge -or $RepeatLearnerState -or $TypedLearnerState -or $WorkspaceDriveState -or $ScanInspectionState -or $Demo5LearnerState -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $SceneActions.Count -gt 0)) { throw 'Numeric cycle QA requires isolated stopped ordinary navigation.' }
if ($NumericCycleInput -and ($SceneId -notin @('lab-9-01-sum-function','lab-9-02-product-function','lab-9-03-sum-and-counter-function','lab-9-04-function-selector','lab-9-10-box-volume') -or ($NumericCycleInput -eq 'numeric_display_2' -and $SceneId -notin @('lab-9-04-function-selector','lab-9-10-box-volume')))) { throw 'Numeric cycle target is outside the twelve approved inputs.' }
if ($RetainedControl -and (!$UserReadoutNavigation -or $NumericCycleInput -or $ReadoutState -or $State -ne 'stopped' -or $RunDemo -or $Challenge -or $RepeatLearnerState -or $TypedLearnerState -or $WorkspaceDriveState -or $ScanInspectionState -or $Demo5LearnerState -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $SceneActions.Count -gt 0)) { throw 'Retained control QA requires isolated stopped ordinary navigation.' }
if ($CollectiveReadouts -and (!$UserReadoutNavigation -or $RetainedControl -or $NumericCycleInput -or $ReadoutState -or $State -ne 'stopped' -or $RunDemo -or $Challenge -or $SceneActions.Count -gt 0)) { throw 'Collective QA requires isolated stopped ordinary navigation.' }
if ($ReadoutGroup -and (!$CollectiveReadouts -or $SceneId -cne 'lab-9-03-sum-and-counter-function')) { throw 'Readout group requires exact dense station collective mode.' }
$ErrorActionPreference = 'Stop'
if ($StationView -eq 'machine-support' -and $SceneId -cne 'lab-10-02-chicken-label-print') { throw 'Machine-support camera requires exact Chicken scale scene.' }
if ($StationView -eq 'ev-readouts' -and $SceneId -cne 'lab-9-12-ev-charging-manager') { throw 'EV-readouts camera requires exact EV charging scene.' }
if ($RepeatLearnerState -and ($SceneId -cne 'lab-4-03-repeat-cycle-counter' -or $TypedLearnerState -or $WorkspaceDriveState -or $ScanInspectionState -or $Demo5LearnerState -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge -or $SceneActions.Count -gt 0)) { throw 'Repeat learner capture requires exact lesson and excludes other diagnostic modes.' }
if ($WorkspaceDriveState -and ($SceneId -cne 'scene-1-conveyor-stop' -or $TypedLearnerState -or $ScanInspectionState -or $Demo5LearnerState -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge -or $SceneActions.Count -gt 0)) { throw 'Drive capture requires its exact source and excludes other diagnostic modes.' }
if ($VerifyTypedLabelPresentation -and ($SceneId -cne 'lab-10-02-chicken-label-print' -or $TypedLearnerState -cnotin @('intended','wrong'))) { throw 'Label presentation check requires a completed Chicken intended/wrong capture.' }
if ($TypedLearnerDetail -and !$TypedLearnerState) { throw 'Typed detail requires a typed learner capture state.' }
if ($TypedLearnerState -and ($SceneId -cnotin @('lab-10-04-motor-enum-state','lab-10-02-chicken-label-print','lab-10-01-drive-alarm-code-string') -or $ScanInspectionState -or $Demo5LearnerState -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge -or $SceneActions.Count -gt 0)) { throw 'Typed learner capture requires one existing typed lesson and excludes other diagnostic/action modes.' }
if ($ScanInspectionState -and ($Demo5LearnerState -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge -or $SceneActions.Count -gt 0)) { throw 'Scan inspection capture excludes other diagnostic and action modes.' }
if ($Demo5LearnerState -and ($SceneId -cne 'lab-11-13-xy-palletizing' -or $WorkspaceRollerState -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge -or $SceneActions.Count -gt 0)) { throw 'Demo5 learner capture requires exact palletizing scene and excludes other diagnostic/action modes.' }
if ($WorkspaceRollerState -and ($SceneId -cne 'scene-1-conveyor-stop' -or $WorkspaceBeltState -or $WorkspaceRollerGeometryState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge)) { throw 'Roller learner capture requires exact source scene and excludes other diagnostic modes.' }
if ($WorkspaceRollerDetail -and (!$WorkspaceRollerState -or $View -ne 'split')) { throw 'Roller detail requires explicit state and split view.' }
if ($WorkspaceRollerObjective -and (!$WorkspaceRollerState -or $WorkspaceRollerDetail -or $View -ne 'scene')) { throw 'Roller objective requires explicit state and scene view.' }
if ($WorkspaceRollerExternalGeometry -and !$WorkspaceRollerGeometryState) { throw 'External roller geometry requires an explicit geometry capture state.' }
if ($WorkspaceRollerGeometryState -and ($SceneId -cne 'scene-1-conveyor-stop' -or $View -ne 'scene' -or $WorkspaceBeltState -or $IndexedTraceState -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge)) { throw 'Roller geometry capture requires the source scene in scene view and excludes execution diagnostics.' }
if ($IndexedTraceState -and ($SceneId -cne 'lab-10-06-ten-motor-array-startup' -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $WorkspaceBeltState -or $RunDemo -or $Challenge)) { throw 'Indexed trace capture requires exact ten-zone scene and excludes other capture/demo modes.' }
if ($WorkspaceBeltDetail -and !$WorkspaceBeltState) { throw 'Finite belt detail requires an explicit diagnostic state.' }
if ($WorkspaceBeltObjective -and (!$WorkspaceBeltState -or $WorkspaceBeltDetail -or $View -ne 'scene')) { throw 'Finite belt objective capture requires explicit diagnostic state, scene view, and no split detail.' }
if ($WorkspaceBeltState -and ($SceneId -cne 'scene-1-conveyor-stop' -or $TemplateEditCapture -or $DeviceTraceCapture -or $TenZoneState -or $RunDemo -or $Challenge)) { throw 'Finite belt diagnostic capture requires exact source scene and excludes other diagnostic/demo modes.' }
$captureRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$captureOutput = [IO.Path]::GetFullPath($CapturePath)
$captureOverride = Join-Path $captureRoot 'override.cfg'
if (Test-Path -LiteralPath $captureOverride) { throw 'Existing override.cfg preserved; capture will not replace it.' }
if (Test-Path -LiteralPath $captureOutput) { throw 'Existing capture preserved; use a fresh output path.' }
if ($WorkspaceDriveState -and (Test-Path -LiteralPath ($captureOutput + '.workspace-drive.json'))) { throw 'Existing drive capture metadata preserved; use a fresh path.' }
$captureIdentityPath = $captureOutput + '.identity.json'
if (Test-Path -LiteralPath $captureIdentityPath) { throw 'Existing capture identity preserved; use a fresh output path.' }
if ($RepeatLearnerState -and (Test-Path -LiteralPath ($captureOutput+'.repeat-learner.json'))) { throw 'Existing repeat learner metadata preserved; use a fresh path.' }
if ($ScanInspectionState -and (Test-Path -LiteralPath ($captureOutput + '.scan-inspection.json'))) { throw 'Existing scan inspection metadata preserved; use a fresh path.' }
if ($Demo5LearnerState -and (Test-Path -LiteralPath ($captureOutput + '.demo5-learner.json'))) { throw 'Existing Demo5 learner metadata preserved; use a fresh path.' }
if ($IndexedTraceState -and (Test-Path -LiteralPath ($captureOutput + '.indexed-trace.json'))) { throw 'Existing indexed trace metadata preserved; use a fresh output path.' }
if ($WorkspaceBeltState -and (Test-Path -LiteralPath ($captureOutput + '.workspace-belt.json'))) { throw 'Existing finite belt metadata preserved; use a fresh output path.' }
if ($WorkspaceRollerState -and (Test-Path -LiteralPath ($captureOutput + '.workspace-roller.json'))) { throw 'Existing roller learner metadata preserved; use a fresh output path.' }
if ($WorkspaceRollerGeometryState -and (Test-Path -LiteralPath ($captureOutput + '.roller-geometry.json'))) { throw 'Existing roller geometry metadata preserved; use a fresh output path.' }
$captureEngine = Join-Path $captureRoot '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$captureAssembly = Join-Path $captureRoot '.godot/mono/temp/bin/Debug/RungProof.Next.dll'
$captureBootstrap = Join-Path $PSScriptRoot 'nonfocus_capture_bootstrap.gd'
foreach ($required in @($captureEngine,$captureBootstrap,$captureAssembly)) { if (!(Test-Path -LiteralPath $required -PathType Leaf)) { throw "Missing capture prerequisite: $required" } }
$captureAssemblyHash = (Get-FileHash -LiteralPath $captureAssembly -Algorithm SHA256).Hash
$captureEngineHash = (Get-FileHash -LiteralPath $captureEngine -Algorithm SHA256).Hash
$captureStartedUtc = [DateTime]::UtcNow.ToString('o')
if ($TenZoneState -and $SceneId -cne 'lab-10-06-ten-motor-array-startup') {
    throw 'Ten-zone diagnostic captures require the exact ten-zone application.'
}
if ($TenZoneDetail -and !$TenZoneState) { throw 'Detail captures require an explicit ten-zone diagnostic state.' }
if ($TemplateEditDetail -and !$TemplateEditCapture) { throw 'Template inspector detail requires actual template edit capture.' }
if ($TemplateEditorDetail -and (!$TemplateEditCapture -or $TemplateEditDetail)) { throw 'Template editor detail requires template capture and excludes inspector detail.' }
if ($TemplateDraftState -and (!$TemplateEditCapture -or $SceneId -cne 'lab-4-01-press-count-lamp')) { throw 'Custom draft capture requires exact Lab4.1 template capture.' }
if ($TemplateEditCapture) {
    if ($TenZoneState -or $SceneId -cnotin @('lab-2-01-workstation-call','lab-4-01-press-count-lamp','lab-5-01-delayed-lamp')) {
        throw 'Template edit captures require one of the three supported starter lessons.'
    }
    if (Test-Path -LiteralPath ($captureOutput + '.template.json')) { throw 'Existing template capture metadata preserved.' }
}
if ($DeviceTraceCapture) {
    if ($TemplateEditCapture -or $TenZoneState -or $SceneId -cne 'lab-4-01-press-count-lamp') { throw 'Device trace capture requires exact Lab 4.1 and no other diagnostic mode.' }
    if (Test-Path -LiteralPath ($captureOutput + '.device-trace.json')) { throw 'Existing device trace identity preserved.' }
}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($captureOutput)) | Out-Null
$captureSettings = @'
[application]
boot_splash/show_image=false
boot_splash/bg_color=Color(0, 0, 0, 0)
[display]
window/size/no_focus=true
window/size/mode=0
window/size/borderless=true
window/size/transparent=true
window/size/window_width_override=1
window/size/window_height_override=1
window/size/min_width=1
window/size/min_height=1
window/per_pixel_transparency/allowed=true
[rendering]
viewport/transparent_background=true
'@
$priorCaptureDotnet = $env:DOTNET_ROOT
$priorCapturePath = $env:PATH
try {
    [IO.File]::WriteAllText($captureOverride,$captureSettings,[Text.UTF8Encoding]::new($false))
    $env:DOTNET_ROOT = Join-Path $captureRoot '.tools/dotnet'
    $env:PATH = "$env:DOTNET_ROOT;$priorCapturePath"
    $captureArgs = @('--path',$captureRoot,'--script',$captureBootstrap,
        '--rendering-method','gl_compatibility','--max-fps','30','--quit-after','300',
        '--','--app-shell',"--shell-scene=$SceneId", "--shell-view=$View", "--capture=$captureOutput", "--capture-size=$Viewport", "--capture-state=$State")
    if ($RunDemo) { $captureArgs += '--capture-run-demo' }
    if ($Challenge) { $captureArgs += '--capture-challenge' }
    if ($WithoutEnvironment) { $captureArgs += '--capture-no-environment' }
    if ($TenZoneState) { $captureArgs += "--capture-tenzone-state=$TenZoneState" }
    if ($TenZoneDetail) { $captureArgs += '--capture-tenzone-detail' }
    if ($TemplateEditCapture) { $captureArgs += '--template-edit-capture' }
    if ($TemplateEditDetail) { $captureArgs += '--template-edit-detail' }
    if ($TemplateEditorDetail) { $captureArgs += '--template-editor-detail' }
    if ($TemplateDraftState) { $captureArgs += "--template-draft-state=$TemplateDraftState" }
    if ($DeviceTraceCapture) { $captureArgs += '--device-trace-capture' }
    if ($IndexedTraceState) { $captureArgs += "--capture-indexed-device-trace=$IndexedTraceState" }
    if ($WorkspaceBeltState) { $captureArgs += "--capture-workspace-belt-state=$WorkspaceBeltState" }
    if ($WorkspaceBeltDetail) { $captureArgs += '--workspace-belt-detail' }
    if ($WorkspaceBeltObjective) { $captureArgs += '--workspace-belt-objective' }
    if ($WorkspaceRollerState) { $captureArgs += "--capture-workspace-roller-state=$WorkspaceRollerState" }
    if ($WorkspaceRollerDetail) { $captureArgs += '--workspace-roller-detail' }
    if ($WorkspaceRollerObjective) { $captureArgs += '--workspace-roller-objective' }
    if ($Demo5LearnerState) { $captureArgs += "--capture-demo5-learner-state=$Demo5LearnerState" }
    if ($ScanInspectionState) { $captureArgs += "--capture-scan-inspection-state=$ScanInspectionState" }
    if ($WorkspaceDriveState) { $captureArgs += "--capture-workspace-drive-state=$WorkspaceDriveState" }
    if ($RepeatLearnerState) { $captureArgs += "--capture-repeat-learner-state=$RepeatLearnerState" }
    if ($TypedLearnerState) { $captureArgs += "--capture-typed-learner-state=$TypedLearnerState" }
    if ($TypedLearnerDetail) { $captureArgs += '--typed-learner-detail' }
    if ($VerifyTypedLabelPresentation) { $captureArgs += '--verify-typed-label-presentation' }
    if ($WorkspaceRollerGeometryState) { $captureArgs += $(if ($WorkspaceRollerExternalGeometry) {'--verify-workspace-roller-external-geometry'} else {'--verify-workspace-roller-geometry'}); $captureArgs += "--workspace-roller-geometry-capture=$WorkspaceRollerGeometryState" }
    foreach ($sceneAction in $SceneActions) {
        if ($sceneAction -notmatch '^[A-Za-z0-9_-]+$') { throw "Invalid capture action id: $sceneAction" }
        $captureArgs += "--capture-action=$sceneAction"
    }
    if ($ReadoutGroup) { $captureArgs += "--capture-readout-group=$ReadoutGroup" }
    if ($CollectiveReadouts) { $captureArgs += '--capture-collective-readouts' }
    if ($RetainedControl) { $captureArgs += "--capture-retained-control=$RetainedControl" }
    if ($NumericCycleInput) { $captureArgs += "--capture-numeric-cycle-input=$NumericCycleInput" }
    if ($ReadoutState) { $captureArgs += "--capture-readout-state=$ReadoutState" }
    if ($UserReadoutNavigation) { $captureArgs += '--capture-user-readout-navigation' }
    if ($BarrelValveOpen) { $captureArgs += '--capture-barrel-valve-open' }
    if ($StationView -ne 'teaching') { $captureArgs += "--capture-station-view=$StationView" }
    & $captureEngine @captureArgs *> ($captureOutput + '.log')
    if ($LASTEXITCODE -ne 0) { throw "Capture process failed. See $captureOutput.log" }
    $captureLog = [IO.File]::ReadAllText($captureOutput + '.log')
    if ($NumericCycleInput -and (!$captureLog.Contains('NUMERIC_CYCLE_CAPTURE PASS') -or !(Test-Path -LiteralPath ($captureOutput + '.numeric-cycle.json')))) { throw 'Numeric cycle input did not qualify.' }
    if ($RetainedControl -and (!$captureLog.Contains('RETAINED_CONTROL_CAPTURE PASS') -or !(Test-Path -LiteralPath ($captureOutput + '.retained-control.json')))) { throw 'Retained control did not qualify.' }
    if ($UserReadoutNavigation -and !$RetainedControl -and (!$captureLog.Contains('USER_READOUT_CAPTURE PASS') -or !(Test-Path -LiteralPath ($captureOutput + '.user-readout.json')))) { throw 'Ordinary readout navigation did not qualify.' }
    if ($ReadoutState -and (!$captureLog.Contains("READOUT_PRESENTATION_CAPTURE PASS state=$ReadoutState") -or !(Test-Path -LiteralPath ($captureOutput + '.readout.json')))) { throw 'Readout fixture did not qualify.' }
    if ($captureLog -match '(?m)^ERROR:|NONFOCUS_CAPTURE_FAIL' -or
        !$captureLog.Contains('NONFOCUS_CAPTURE_READY') -or
        !$captureLog.Contains('RUNTIME_CAPTURE_SAVED') -or
        !(Test-Path -LiteralPath $captureOutput -PathType Leaf)) { throw "Capture did not qualify. See $captureOutput.log" }
    if ($WorkspaceDriveState -and (!$captureLog.Contains("WORKSPACE_DRIVE_CAPTURE_READY state=$WorkspaceDriveState") -or !(Test-Path -LiteralPath ($captureOutput + '.workspace-drive.json')))) { throw 'Actual drive capture did not qualify.' }
    $repeatLearnerIdentity=$null
    if ($RepeatLearnerState) {
        if (!$captureLog.Contains("REPEAT_LEARNER_CAPTURE_READY $RepeatLearnerState")) { throw 'Repeat learner preparation did not qualify.' }
        $repeatLearnerIdentity=Get-Content -LiteralPath ($captureOutput+'.repeat-learner.json') -Raw | ConvertFrom-Json
        if ($repeatLearnerIdentity.scene -cne $SceneId -or $repeatLearnerIdentity.mode -cne $RepeatLearnerState -or !$repeatLearnerIdentity.diagnosticClockHeld -or $repeatLearnerIdentity.learnerPauseUsed -or $repeatLearnerIdentity.dllSha256 -cne $captureAssemblyHash) { throw 'Repeat learner held-clock/DLL identity mismatch.' }
        $expectedRepeatStatus = switch ($RepeatLearnerState) { 'invalid' {'Unavailable'} 'request-count' {'NeedsAttention'} 'preset-two' {'NeedsAttention'} 'held-active' {'Incomplete'} default {'Observed'} }
        if ($repeatLearnerIdentity.assessmentStatus -cne $expectedRepeatStatus) { throw 'Repeat learner assessment status differs from explicit expected state.' }
    }
    $workspaceDriveAcceptedIdentity = $null
    if ($WorkspaceDriveState) {
        $driveMetadataPath = $captureOutput + '.workspace-drive.json'
        $driveMetadata = Get-Content -LiteralPath $driveMetadataPath -Raw | ConvertFrom-Json
        if ($driveMetadata.mode -cne $WorkspaceDriveState -or !$driveMetadata.ephemeralQA -or !$driveMetadata.diagnosticClockHeld) { throw 'Drive capture metadata did not qualify.' }
        $workspaceDriveAcceptedIdentity = [ordered]@{ state=$driveMetadata.mode; instance=$driveMetadata.instance; diagnosticClockHeld=$driveMetadata.diagnosticClockHeld; adapterSpeed=$driveMetadata.adapterSpeed; buttonVisible=$driveMetadata.buttonVisible; buttonDisabled=$driveMetadata.buttonDisabled; selectedTab=$driveMetadata.selectedTab; metadataPath=$driveMetadataPath; metadataSha256=(Get-FileHash -LiteralPath $driveMetadataPath -Algorithm SHA256).Hash }
    }
    $loadedScenes = @([regex]::Matches($captureLog, 'MIGRATED_SCENE_LOADED ([^\s]+)') | ForEach-Object { $_.Groups[1].Value })
    if ($TenZoneState -and !$captureLog.Contains("TENZONE_CAPTURE_READY state=$TenZoneState")) {
        throw 'Ten-zone accepted-program diagnostic state did not qualify.'
    }
    $tenZoneAcceptedIdentity = $null
    $templateAcceptedIdentity = $null
    $deviceTraceAcceptedIdentity = $null
    $indexedTraceAcceptedIdentity = $null
    if ($IndexedTraceState) {
        if (!$captureLog.Contains("INDEXED_TRACE_CAPTURE_READY state=$IndexedTraceState")) { throw 'Indexed trace actual state did not qualify.' }
        $indexedTraceAcceptedIdentity=Get-Content -LiteralPath ($captureOutput + '.indexed-trace.json') -Raw | ConvertFrom-Json
        if ($indexedTraceAcceptedIdentity.scene -cne $SceneId -or $indexedTraceAcceptedIdentity.state -cne $IndexedTraceState -or !$indexedTraceAcceptedIdentity.clockHeld -or !$indexedTraceAcceptedIdentity.ephemeralQA -or $indexedTraceAcceptedIdentity.programSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $indexedTraceAcceptedIdentity.scan -ne $(if ($IndexedTraceState -eq 'true') {1} else {2})) { throw 'Indexed trace identity did not qualify.' }
    }
    $workspaceBeltAcceptedIdentity = $null
    $demo5LearnerAcceptedIdentity = $null
    if ($ScanInspectionState) {
        if (!$captureLog.Contains("SCAN_INSPECTION_CAPTURE_READY state=$ScanInspectionState")) { throw 'Actual scan inspection state did not qualify.' }
        $inspectionIdentity = Get-Content -LiteralPath ($captureOutput + '.scan-inspection.json') -Raw | ConvertFrom-Json
        if (!$inspectionIdentity.paused -or !$inspectionIdentity.ephemeralQA -or $inspectionIdentity.scene -cne $SceneId -or $inspectionIdentity.dllSha256 -cne $captureAssemblyHash -or $inspectionIdentity.historyCount -lt 1) { throw 'Scan inspection actual identity did not qualify.' }
        if ($ScanInspectionState -eq 'invalid' -and $inspectionIdentity.currentDraftMatches) { throw 'Invalid scan inspection requires rejected current draft.' }
    }
    if ($Demo5LearnerState) {
        if (!$captureLog.Contains("DEMO5_LEARNER_CAPTURE_READY state=$Demo5LearnerState")) { throw 'Demo5 actual learner state did not qualify.' }
        $demo5LearnerAcceptedIdentity=Get-Content -LiteralPath ($captureOutput + '.demo5-learner.json') -Raw | ConvertFrom-Json
        if ($demo5LearnerAcceptedIdentity.state -cne $Demo5LearnerState -or $demo5LearnerAcceptedIdentity.scene -cne $SceneId -or !$demo5LearnerAcceptedIdentity.clockHeld -or !$demo5LearnerAcceptedIdentity.ephemeralQA -or !$demo5LearnerAcceptedIdentity.ioCollapsed -or !$demo5LearnerAcceptedIdentity.jointReopenedBeforeRun -or $demo5LearnerAcceptedIdentity.dllSha256 -cne $captureAssemblyHash -or $demo5LearnerAcceptedIdentity.programSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'Demo5 current learner identity did not qualify.' }
        if ($Demo5LearnerState -eq 'completed' -and ($demo5LearnerAcceptedIdentity.points.placed_cartons -ne 4 -or !$demo5LearnerAcceptedIdentity.points.layer_complete -or @($demo5LearnerAcceptedIdentity.placed | Where-Object visible).Count -ne 4)) { throw 'Completed Demo5 requires actual four-carton layer.' }
        if ($Demo5LearnerState -eq 'wrong-carry' -and (!$demo5LearnerAcceptedIdentity.points.palletizer_fault -or !$demo5LearnerAcceptedIdentity.points.carton_attached -or $demo5LearnerAcceptedIdentity.points.placed_cartons -ne 0 -or $demo5LearnerAcceptedIdentity.points.vacuum_pick)) { throw 'Wrong Demo5 requires genuine vacuum-loss carry fault without false placement.' }
        if ($Demo5LearnerState -eq 'invalid-current' -and ($demo5LearnerAcceptedIdentity.currentDraftValid -or !$demo5LearnerAcceptedIdentity.acceptedProgramHistorical -or $demo5LearnerAcceptedIdentity.runtimeState -eq 'Running' -or $demo5LearnerAcceptedIdentity.acceptedOutputs.psobject.Properties.Value -contains $true)) { throw 'Invalid Demo5 must be stopped with historical program and cleared command image.' }
    }
    $workspaceRollerAcceptedIdentity = $null
    if ($WorkspaceRollerState) {
        if (!$captureLog.Contains("WORKSPACE_ROLLER_CAPTURE_READY state=$WorkspaceRollerState")) { throw 'Roller learner state did not qualify.' }
        $workspaceRollerAcceptedIdentity=Get-Content -LiteralPath ($captureOutput + '.workspace-roller.json') -Raw | ConvertFrom-Json
        $expectedRollerTrial = switch ($WorkspaceRollerState) { 'intended' {'Observed'} 'coasting' {'Running'} 'wrong-terminal' {'NeedsAttention'} 'invalid-current' {'Unavailable'} }
        if ($workspaceRollerAcceptedIdentity.state -cne $WorkspaceRollerState -or $workspaceRollerAcceptedIdentity.scene -cne $SceneId -or !$workspaceRollerAcceptedIdentity.clockHeld -or !$workspaceRollerAcceptedIdentity.ephemeralQA -or $workspaceRollerAcceptedIdentity.dllSha256 -cne $captureAssemblyHash -or $workspaceRollerAcceptedIdentity.observedTrialState -cne $expectedRollerTrial -or $workspaceRollerAcceptedIdentity.programSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'Roller accepted learner identity did not qualify.' }
        if ($WorkspaceRollerDetail -and (!$workspaceRollerAcceptedIdentity.detail -or !$captureLog.Contains('WORKSPACE_ROLLER_CAPTURE_DETAIL ioCollapsed=true'))) { throw 'Roller detail did not qualify.' }
        if ($WorkspaceRollerObjective -and (!$workspaceRollerAcceptedIdentity.objective -or !$captureLog.Contains('WORKSPACE_ROLLER_CAPTURE_OBJECTIVE ioCollapsed=true objective=true cardFits=true'))) { throw 'Roller objective did not qualify.' }
        if ($WorkspaceRollerState -eq 'invalid-current' -and ($workspaceRollerAcceptedIdentity.currentDraftValid -or !$workspaceRollerAcceptedIdentity.acceptedProgramHistorical -or $workspaceRollerAcceptedIdentity.runtimeState -eq 'Running')) { throw 'Invalid roller draft must be stopped with historical accepted program.' }
        if ($WorkspaceRollerState -eq 'coasting' -and ($workspaceRollerAcceptedIdentity.points.roller_workspace_run -ne $false -or $workspaceRollerAcceptedIdentity.physical.SpeedMps -le 0)) { throw 'Coasting requires PLC command OFF and continued drive motion.' }
    }
    $workspaceRollerGeometryIdentity = $null
    if ($WorkspaceRollerGeometryState) {
        if (!$captureLog.Contains("WORKSPACE_ROLLER_GEOMETRY_CAPTURE_READY state=$WorkspaceRollerGeometryState")) { throw 'Roller geometry render preparation did not qualify.' }
        $workspaceRollerGeometryIdentity = Get-Content -LiteralPath ($captureOutput + '.roller-geometry.json') -Raw | ConvertFrom-Json
        if ($workspaceRollerGeometryIdentity.state -cne $WorkspaceRollerGeometryState -or !$workspaceRollerGeometryIdentity.readOnlyGeometry -or !$workspaceRollerGeometryIdentity.ephemeralQA -or $workspaceRollerGeometryIdentity.controllerExecution -ne $false -or $workspaceRollerGeometryIdentity.geometryFailures -ne 0 -or !$captureLog.Contains('WORKSPACE_ROLLER_GEOMETRY_RESULT failures=0') -or $workspaceRollerGeometryIdentity.currentDllSha256 -cne $captureAssemblyHash) { throw 'Roller geometry identity did not qualify.' }
        if ($WorkspaceRollerExternalGeometry -and $workspaceRollerGeometryIdentity.installation -cne 'externalPair') { throw 'External roller optical installation identity did not qualify.' }
    }
    if ($WorkspaceBeltState) {
        if (!$captureLog.Contains("WORKSPACE_BELT_CAPTURE_READY state=$WorkspaceBeltState")) { throw 'Finite belt actual state did not qualify.' }
        $workspaceBeltAcceptedIdentity=Get-Content -LiteralPath ($captureOutput + '.workspace-belt.json') -Raw | ConvertFrom-Json
        $expectedTrialState = switch ($WorkspaceBeltState) { 'intended' { 'Observed' } 'wrong-terminal' { 'NeedsAttention' } 'invalid-current' { 'Unavailable' } }
        if ($workspaceBeltAcceptedIdentity.observedTrialState -cne $expectedTrialState) { throw "Finite belt observed trial did not qualify: expected $expectedTrialState." }
        if ($workspaceBeltAcceptedIdentity.state -cne $WorkspaceBeltState -or $workspaceBeltAcceptedIdentity.scene -cne $SceneId -or !$workspaceBeltAcceptedIdentity.clockHeld -or !$workspaceBeltAcceptedIdentity.ephemeralQA -or !$workspaceBeltAcceptedIdentity.relation -or $workspaceBeltAcceptedIdentity.programSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'Finite belt accepted identity did not qualify.' }
        if ($workspaceBeltAcceptedIdentity.cartonScale -ne .65) { throw 'Finite belt simulated carton size disclosure does not match the preset.' }
        if ($WorkspaceBeltDetail -and (!$workspaceBeltAcceptedIdentity.detail -or !$captureLog.Contains('WORKSPACE_BELT_CAPTURE_DETAIL ioCollapsed=true'))) { throw 'Finite belt detail did not qualify actual Scene I/O collapse.' }
        if ($WorkspaceBeltObjective -and (!$workspaceBeltAcceptedIdentity.objective -or !$captureLog.Contains('WORKSPACE_BELT_CAPTURE_OBJECTIVE ioCollapsed=true'))) { throw 'Finite belt objective did not qualify actual operator panel and Scene I/O collapse.' }
        if ($WorkspaceBeltState -eq 'invalid-current' -and ($workspaceBeltAcceptedIdentity.currentDraftValid -or !$workspaceBeltAcceptedIdentity.acceptedProgramHistorical -or $workspaceBeltAcceptedIdentity.runtimeState -eq 'Running')) { throw 'Invalid finite draft capture must be stopped and identify retained program as historical.' }
    }
    if ($DeviceTraceCapture) {
        if (!$captureLog.Contains('DEVICE_TRACE_CAPTURE_READY acceptedScan=1 input=True output=True ephemeralQA=true')) { throw 'Actual accepted device trace capture did not qualify.' }
        $deviceTraceAcceptedIdentity=Get-Content -LiteralPath ($captureOutput + '.device-trace.json') -Raw | ConvertFrom-Json
        if ($deviceTraceAcceptedIdentity.scene -cne $SceneId -or $deviceTraceAcceptedIdentity.scan -ne 1 -or !$deviceTraceAcceptedIdentity.input -or !$deviceTraceAcceptedIdentity.output -or !$deviceTraceAcceptedIdentity.clockHeld) { throw 'Device trace identity did not qualify.' }
    }
    if ($TemplateEditCapture) {
        if (!$captureLog.Contains("TEMPLATE_CAPTURE_READY ephemeralQA=true stopped=true scene=$SceneId")) { throw 'Actual template edit and joint roundtrip did not qualify.' }
        $templateAcceptedIdentity = Get-Content -LiteralPath ($captureOutput + '.template.json') -Raw | ConvertFrom-Json
        if ($templateAcceptedIdentity.scene -cne $SceneId -or !$templateAcceptedIdentity.draftPreserved -or $templateAcceptedIdentity.runtime -cne 'stopped') { throw 'Template identity did not qualify.' }
        if ($TemplateDraftState -and $templateAcceptedIdentity.draftState -cne $TemplateDraftState) { throw 'Captured template draft does not match the requested valid/invalid state.' }
        if ($TemplateEditorDetail -and (!$templateAcceptedIdentity.editorDetail -or !$captureLog.Contains('TEMPLATE_EDITOR_DETAIL_READY actualSceneIoCollapsed=true'))) { throw 'Template editor detail did not qualify actual Scene I/O collapse.' }
    }
    if ($TenZoneDetail -and !$captureLog.Contains('TENZONE_CAPTURE_DETAIL ioCollapsed=true')) {
        throw 'Supplemental detail capture did not collapse Scene I/O through the real UI.'
    }
    if ($TenZoneState) {
        $tenZoneIdentityMatch = [regex]::Match($captureLog, '(?m)^TENZONE_CAPTURE_IDENTITY (.+)$')
        if (!$tenZoneIdentityMatch.Success) { throw 'Missing accepted-program capture identity.' }
        $tenZoneAcceptedIdentity = $tenZoneIdentityMatch.Groups[1].Value | ConvertFrom-Json
        if ($tenZoneAcceptedIdentity.state -cne $TenZoneState -or !$tenZoneAcceptedIdentity.clockHeld) {
            throw 'Diagnostic state or clock-hold identity does not match the requested capture.'
        }
    }
    foreach ($sceneAction in $SceneActions) {
        if (!$captureLog.Contains("RUNTIME_CAPTURE_ACTION_APPLIED $sceneAction")) { throw "Capture action was not applied: $sceneAction" }
    }
    if (!$loadedScenes.Count -or $loadedScenes[-1] -cne $SceneId) { throw "Capture loaded a different scene: $($loadedScenes -join ',')" }
    $pngBytes = [IO.File]::ReadAllBytes($captureOutput)
    if ($pngBytes.Length -lt 24) { throw 'Capture has no PNG dimensions.' }
    $pngSignature=@(137,80,78,71,13,10,26,10)
    for ($signatureIndex=0; $signatureIndex -lt 8; $signatureIndex++) { if ($pngBytes[$signatureIndex] -ne $pngSignature[$signatureIndex]) { throw 'Capture does not have the PNG signature.' } }
    if ($pngBytes[8] -ne 0 -or $pngBytes[9] -ne 0 -or $pngBytes[10] -ne 0 -or $pngBytes[11] -ne 13 -or $pngBytes[12] -ne 73 -or $pngBytes[13] -ne 72 -or $pngBytes[14] -ne 68 -or $pngBytes[15] -ne 82) { throw 'PNG first chunk is not the 13-byte IHDR header.' }
    $pngWidth = ($pngBytes[16] * 16777216) + ($pngBytes[17] * 65536) + ($pngBytes[18] * 256) + $pngBytes[19]
    $pngHeight = ($pngBytes[20] * 16777216) + ($pngBytes[21] * 65536) + ($pngBytes[22] * 256) + $pngBytes[23]
    $requestedSize=$Viewport.Split('x'); if ($pngWidth -ne [int]$requestedSize[0] -or $pngHeight -ne [int]$requestedSize[1]) { throw "Capture viewport differs from requested ${Viewport}: ${pngWidth}x${pngHeight}" }
    if ($pngWidth -lt 1200 -or $pngHeight -lt 675) { throw "Capture viewport is too small: ${pngWidth}x${pngHeight}" }
    if ((Get-FileHash -LiteralPath $captureAssembly -Algorithm SHA256).Hash -cne $captureAssemblyHash) {
        throw 'Application assembly changed during capture; evidence is not qualified.'
    }
    [ordered]@{
        repeatLearnerState=$RepeatLearnerState; repeatLearnerAcceptedIdentity=$repeatLearnerIdentity
        sceneId=$SceneId; view=$View; stationView=$StationView; requestedCaptureState=$State; sceneActions=@($SceneActions)
        actualRuntimeState=if ($workspaceBeltAcceptedIdentity) { $workspaceBeltAcceptedIdentity.runtimeState } elseif ($tenZoneAcceptedIdentity) { $tenZoneAcceptedIdentity.runtimeState } elseif ($templateAcceptedIdentity) { $templateAcceptedIdentity.runtime } else { $null }
        tenZoneDiagnosticState=$TenZoneState; diagnosticClockHeld=[bool]($TenZoneState -or $WorkspaceDriveState -or $RepeatLearnerState)
        tenZoneDetail=[bool]$TenZoneDetail
        templateEditCapture=[bool]$TemplateEditCapture; templateAcceptedIdentity=$templateAcceptedIdentity
        templateEditDetail=[bool]$TemplateEditDetail
        templateEditorDetail=[bool]$TemplateEditorDetail; requestedTemplateDraftState=$TemplateDraftState
        deviceTraceCapture=[bool]$DeviceTraceCapture; deviceTraceAcceptedIdentity=$deviceTraceAcceptedIdentity
        indexedTraceState=$IndexedTraceState; indexedTraceAcceptedIdentity=$indexedTraceAcceptedIdentity
        workspaceDriveState=$WorkspaceDriveState; workspaceDriveAcceptedIdentity=$workspaceDriveAcceptedIdentity
        workspaceBeltState=$WorkspaceBeltState; workspaceBeltAcceptedIdentity=$workspaceBeltAcceptedIdentity
        workspaceRollerState=$WorkspaceRollerState; workspaceRollerAcceptedIdentity=$workspaceRollerAcceptedIdentity
        demo5LearnerState=$Demo5LearnerState; demo5LearnerAcceptedIdentity=$demo5LearnerAcceptedIdentity
        workspaceBeltDetail=[bool]$WorkspaceBeltDetail
        workspaceRollerGeometryState=$WorkspaceRollerGeometryState; workspaceRollerGeometryIdentity=$workspaceRollerGeometryIdentity
        workspaceRollerExternalGeometry=[bool]$WorkspaceRollerExternalGeometry
        tenZoneAcceptedIdentity=$tenZoneAcceptedIdentity
        startedUtc=$captureStartedUtc; completedUtc=[DateTime]::UtcNow.ToString('o')
        assemblyPath=$captureAssembly; assemblySha256=$captureAssemblyHash
        enginePath=$captureEngine; engineSha256=$captureEngineHash
        repeatLearnerSourceSha256=if ($RepeatLearnerState) { (Get-FileHash -LiteralPath (Join-Path $captureRoot 'src/App/Main.RepeatCycleLearnerCapture.cs') -Algorithm SHA256).Hash } else { $null }
        imageSha256=(Get-FileHash -LiteralPath $captureOutput -Algorithm SHA256).Hash
        width=$pngWidth; height=$pngHeight; assemblyUnchangedDuringCapture=$true
        limitation='Exact rendered binary identity; visual inspection and source-to-build qualification are separate.'
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $captureIdentityPath -Encoding utf8
    Write-Output "NONFOCUS_SCENE_CAPTURE_PASS scene=$SceneId renderer=gl_compatibility image=$captureOutput"
}
finally {
    # Only this invocation's new override is removed; pre-existing files were
    # refused above. The user's project/window preferences are not persisted.
    if ([IO.File]::Exists($captureOverride)) { [IO.File]::Delete($captureOverride) }
    $env:DOTNET_ROOT = $priorCaptureDotnet
    $env:PATH = $priorCapturePath
}
