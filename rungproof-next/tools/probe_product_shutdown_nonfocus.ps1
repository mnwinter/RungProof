[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SceneId,
    [Parameter(Mandatory=$true)][string]$LogPath)
$ErrorActionPreference='Stop'
$probeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$probeLog=[IO.Path]::GetFullPath($LogPath)
$probeOverride=Join-Path $probeRoot 'override.cfg'
if(Test-Path -LiteralPath $probeOverride){throw 'Existing override.cfg preserved; wait for exclusive capture to finish.'}
if(Test-Path -LiteralPath $probeLog){throw 'Existing probe log preserved; choose a fresh path.'}
$probeEngine=Join-Path $probeRoot '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$probeDll=Join-Path $probeRoot '.godot/mono/temp/bin/Debug/RungProof.Next.dll'
$probeBootstrap=Join-Path $PSScriptRoot 'nonfocus_capture_bootstrap.gd'
$probeHash=(Get-FileHash -LiteralPath $probeDll).Hash
$probePreviousDotnet=$env:DOTNET_ROOT
$probePreviousPath=$env:PATH
$probeSettings=@'
[application]
boot_splash/show_image=false
boot_splash/bg_color=Color(0,0,0,0)
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
try{
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($probeLog))|Out-Null
    [IO.File]::WriteAllText($probeOverride,$probeSettings,[Text.UTF8Encoding]::new($false))
    $env:DOTNET_ROOT=Join-Path $probeRoot '.tools/dotnet'
    $env:PATH="$env:DOTNET_ROOT;$probePreviousPath"
    # No --capture argument: exercise normal app lifetime and engine-requested
    # exit, without calling the capture-only GC cleanup. The existing bootstrap
    # verifies native no-focus and pass-through before showing the second screen.
    & $probeEngine --path $probeRoot --script $probeBootstrap --rendering-method gl_compatibility --max-fps 30 --quit-after 180 -- --app-shell "--shell-scene=$SceneId" '--shell-view=scene' '--capture-size=1600x900' *> $probeLog
    $probeExit=$LASTEXITCODE
    $probeText=[IO.File]::ReadAllText($probeLog)
    if($probeExit -ne 0 -or $probeText -match '(?m)^ERROR:|NONFOCUS_CAPTURE_FAIL'){throw "Normal-runtime teardown probe failed; preserved log: $probeLog"}
    if($probeText -notmatch 'NONFOCUS_CAPTURE_READY' -or $probeText -notmatch "MIGRATED_SCENE_LOADED $([regex]::Escape($SceneId)) "){throw 'Probe did not establish requested no-focus normal runtime.'}
    if($probeText -match 'RUNTIME_CAPTURE_SAVED|RUNTIME_CAPTURE_MANAGED_FINALIZERS_DRAINED'){throw 'Probe entered capture cleanup; ordinary teardown was not tested.'}
    if((Get-FileHash -LiteralPath $probeDll).Hash -ne $probeHash){throw 'Assembly changed during normal-runtime probe.'}
    @{sceneId=$SceneId;assemblySha256=$probeHash;exitCode=$probeExit;scope='Normal app startup and automatic engine exit; no capture path, keyboard, mouse, manual close or save-prompt coverage';logSha256=(Get-FileHash -LiteralPath $probeLog).Hash}|ConvertTo-Json|Set-Content ($probeLog+'.identity.json')
    Write-Output "NORMAL_RUNTIME_TEARDOWN_PROBE_PASS scene=$SceneId scope=automatic-engine-exit-only"
}finally{
    if(Test-Path -LiteralPath $probeOverride){Remove-Item -LiteralPath $probeOverride}
    $env:DOTNET_ROOT=$probePreviousDotnet
    $env:PATH=$probePreviousPath
}
