[CmdletBinding()]
param([string]$OutputDirectory = '', [switch]$AllowDirty, [switch]$DirectHeadlessExport)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'offline_export_log.ps1')
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$godotPath = Join-Path $projectRoot '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
# An explicitly selected direct executable avoids the Windows console wrapper
# when that wrapper remains alive after its export child has terminated.
# Both paths use the same pinned engine and the export stays headless.
$exportGodotPath = if ($DirectHeadlessExport) { $godotPath.Replace('_console.exe', '.exe') } else { $godotPath }
$dotnetRoot = Join-Path $projectRoot '.tools/dotnet'
$requiredPaths = @($godotPath, $exportGodotPath, (Join-Path $dotnetRoot 'dotnet.exe'),
    (Join-Path $projectRoot '.tools/export-templates/windows/windows_release_x86_64.exe'),
    (Join-Path $projectRoot '.tools/export-templates/windows/windows_release_x86_64_console.exe'),
    (Join-Path $projectRoot '.tools/export-templates/GODOT_LICENSE.txt'),
    (Join-Path $projectRoot '.tools/export-templates/GODOT_COPYRIGHT.txt'),
    (Join-Path $dotnetRoot 'LICENSE.txt'), (Join-Path $dotnetRoot 'ThirdPartyNotices.txt'),
    (Join-Path $projectRoot 'RungProof.Next.sln'))
foreach ($requiredPath in $requiredPaths) {
    if (!(Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Missing release prerequisite: $requiredPath" }
}

$gitState = @(& git -C $projectRoot status --porcelain)
if ($LASTEXITCODE) { throw 'Cannot read source Git state' }
if ($gitState.Count -and !$AllowDirty) { throw 'Source is dirty. Review the changes, then use -AllowDirty for an internal candidate.' }
if (!$OutputDirectory) {
    $buildId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $OutputDirectory = Join-Path $projectRoot "artifacts/offline-demo/$buildId"
}
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputRoot) { throw "Use a fresh output directory; existing work is preserved: $outputRoot" }
New-Item -ItemType Directory -Path $outputRoot | Out-Null
$evidenceRoot = Join-Path $outputRoot 'build-evidence'
New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$priorDotnetRoot = $env:DOTNET_ROOT
$priorPath = $env:PATH
$priorOfflinePolicy = $env:RungProofOfflineDemo

function Get-SourceCodeFingerprint {
    $codePaths = @((Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -File -Recurse).FullName)
    $codePaths += @('RungProof.Next.csproj', 'RungProof.Next.sln', 'project.godot', 'export_presets.cfg') | ForEach-Object { Join-Path $projectRoot $_ }
    $codePaths += @('challenges/catalog.json', 'challenges/conveyor-stop.challenge.json',
        'challenges/starters/conveyor-stop.rpproj.json', 'challenges/semantic-manifest.json',
        'challenges/presentation/conveyor-stop.presentation.json',
        'tools/build_challenge_semantic_manifest.ps1') | ForEach-Object { Join-Path $projectRoot $_ }
    $rows = @($codePaths | Sort-Object | ForEach-Object {
        [IO.Path]::GetRelativePath($projectRoot, $_) + ' ' + (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash
    })
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($rows -join "`n")))).Replace('-', '') }
    finally { $hasher.Dispose() }
}

function Get-SourceFreezeInventory {
    $paths = @('assets','scenes','programs','challenges','src','tools','docs') | ForEach-Object {
        (Get-ChildItem -LiteralPath (Join-Path $projectRoot $_) -File -Recurse).FullName
    }
    $paths += @('RungProof.Next.csproj','RungProof.Next.sln','project.godot','export_presets.cfg','README.md','docs/OFFLINE_DEMO_CANDIDATE.md') | ForEach-Object { Join-Path $projectRoot $_ }
    return @($paths | Sort-Object -Unique | ForEach-Object {
        [ordered]@{ path=[IO.Path]::GetRelativePath($projectRoot,$_); bytes=(Get-Item -LiteralPath $_).Length; sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
    })
}
function Get-SourceFreezeJson { return (Get-SourceFreezeInventory | ConvertTo-Json -Depth 5 -Compress) }
function Invoke-Checked([string]$Executable, [string[]]$Arguments, [string]$LogPath, [string]$RequiredMarker = '') {
    if ($DirectHeadlessExport -and $Executable -eq $exportGodotPath) {
        # PowerShell does not necessarily wait for a Windows GUI-subsystem EXE.
        # Observe the actual pinned headless engine process, with both pipes
        # drained concurrently, before inspecting any exported files.
        $info = [Diagnostics.ProcessStartInfo]::new($Executable)
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
        $process = [Diagnostics.Process]::Start($info)
        try {
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            [IO.File]::WriteAllText($LogPath, $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult())
            if ($process.ExitCode -ne 0) { throw "Command failed ($($process.ExitCode)). See $LogPath" }
        }
        finally { $process.Dispose() }
    }
    else {
        & $Executable @Arguments *> $LogPath
        if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE). See $LogPath" }
    }
    $log = [IO.File]::ReadAllText($LogPath)
    # Godot can return zero after an export-plugin error. Never accept that
    # EXE/PCK pair or qualify it merely because files were created.
    try { Assert-OfflineExportLog $log $RequiredMarker }
    catch { throw "Rejected $LogPath : $($_.Exception.Message)" }
}

try {
    & (Join-Path $PSScriptRoot 'verify_godot_source_utf8.ps1') -SourceRoot $projectRoot
    $env:DOTNET_ROOT = $dotnetRoot
    $env:PATH = "$dotnetRoot;$priorPath"
    $env:RungProofOfflineDemo = 'true'
    & (Join-Path $PSScriptRoot 'build_challenge_semantic_manifest.ps1') -SourceRoot $projectRoot -OutputPath (Join-Path $projectRoot 'challenges/semantic-manifest.json')
    $sourceCodeFingerprint = Get-SourceCodeFingerprint
    $sourceFreezeJson = Get-SourceFreezeJson
    Invoke-Checked (Join-Path $dotnetRoot 'dotnet.exe') @('run', '--project', (Join-Path $projectRoot 'tests/OfflineDemo/RungProof.Next.OfflineDemo.Tests.csproj')) (Join-Path $evidenceRoot 'policy-tests.log') 'failures=0 bridgeFactoryCalls=0'
    $exePath = Join-Path $outputRoot 'RungProof.exe'
    $exportLog = Join-Path $evidenceRoot 'export.log'
    Invoke-Checked $exportGodotPath @('--headless', '--quit', '--path', $projectRoot, '--export-release', 'Windows Offline Demo', $exePath) $exportLog
    $assemblyPath = Join-Path $outputRoot 'data_RungProof.Next_windows_x86_64/RungProof.Next.dll'
    foreach ($artifactPath in @($exePath, (Join-Path $outputRoot 'RungProof.pck'), $assemblyPath)) {
        if (!(Test-Path -LiteralPath $artifactPath -PathType Leaf)) { throw "Export omitted required artifact: $artifactPath" }
    }
    $packedPaths = @(Get-OfflineExportResourcePaths ([IO.File]::ReadAllText($exportLog)))
    if (!$packedPaths.Count) { throw 'No export resource inventory was recorded' }
    foreach ($resource in @('res://challenges/catalog.json', 'res://challenges/conveyor-stop.challenge.json',
        'res://challenges/starters/conveyor-stop.rpproj.json', 'res://challenges/semantic-manifest.json',
        'res://challenges/presentation/conveyor-stop.presentation.json')) {
        if ($resource -cnotin $packedPaths) { throw "Challenge resource omitted from export: $resource" }
    }
    $forbiddenPaths = @($packedPaths | Where-Object { $_ -match '^res://(tests|tools|artifacts)/|/(source|review)/|\.blend\d*$|\.(py|ps1)$' })
    if ($forbiddenPaths.Count) { throw "Developer-only files exported: $($forbiddenPaths -join ', ')" }
    $packedPaths | Set-Content -LiteralPath (Join-Path $evidenceRoot 'packed-resources.txt') -Encoding UTF8
    $consolePath = Join-Path $outputRoot 'RungProof.console.exe'
    Invoke-Checked $consolePath @('--headless', '--', '--verify-offline-build-policy') (Join-Path $evidenceRoot 'artifact-policy.log') 'OFFLINE_BUILD_POLICY PASS'
    Invoke-Checked $consolePath @('--headless', '--', '--verify-app-shell') (Join-Path $evidenceRoot 'artifact-shell.log') 'APP_SHELL_VERIFY PASS'
    Invoke-Checked $consolePath @('--headless', '--', '--verify-demo-sequence') (Join-Path $evidenceRoot 'artifact-conveyor.log') 'DEMO_SEQUENCE_VERIFY PASS'
    Invoke-Checked $consolePath @('--headless', '--', '--verify-conveyor-challenge') (Join-Path $evidenceRoot 'artifact-challenge.log') 'CONVEYOR_CHALLENGE_VERIFY PASS'
    Invoke-Checked $consolePath @('--headless', '--', '--app-shell', '--verify-challenge-ui') (Join-Path $evidenceRoot 'artifact-challenge-ui.log') 'CONVEYOR_CHALLENGE_UI_VERIFY PASS'
    if ((Get-SourceFreezeJson) -cne $sourceFreezeJson) { throw 'Source/art/assets/scripts changed during export; create a fresh candidate.' }
    [IO.File]::WriteAllText((Join-Path $evidenceRoot 'source-freeze-inventory.json'),$sourceFreezeJson,[Text.UTF8Encoding]::new($false))
    if ((Get-SourceCodeFingerprint) -ne $sourceCodeFingerprint) { throw 'Source code/build configuration changed during export; create a fresh candidate.' }
    # Open Project uses ordinary filesystem paths, so references must also be
    # shipped beside the executable even when they are included in the PCK.
    Copy-Item -LiteralPath (Join-Path $projectRoot 'programs') -Destination (Join-Path $outputRoot 'programs') -Recurse
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/OFFLINE_DEMO_CANDIDATE.md') -Destination (Join-Path $outputRoot 'README.md')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'verify_demo_package.ps1') -Destination (Join-Path $outputRoot 'Verify-Package.ps1')
    Copy-Item -LiteralPath (Join-Path $projectRoot '.tools/export-templates/GODOT_LICENSE.txt') -Destination $outputRoot
    Copy-Item -LiteralPath (Join-Path $projectRoot '.tools/export-templates/GODOT_COPYRIGHT.txt') -Destination $outputRoot
    Copy-Item -LiteralPath (Join-Path $dotnetRoot 'LICENSE.txt') -Destination (Join-Path $outputRoot 'DOTNET_LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') -Destination (Join-Path $outputRoot 'DOTNET_ThirdPartyNotices.txt')
    $files = @(Get-ChildItem -LiteralPath $outputRoot -File -Recurse | ForEach-Object {
        [ordered]@{ path = [IO.Path]::GetRelativePath($outputRoot, $_.FullName).Replace('\','/'); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    $metadata = [ordered]@{
        schemaVersion = 1; builtUtc = [DateTime]::UtcNow.ToString('o'); sourceCommit = (& git -C $projectRoot rev-parse HEAD).Trim()
        sourceDirty = [bool]$gitState.Count; sourceStatus = $gitState; policy = 'offline-demo'; distributionApproved = $false
        sourceCodeConfigurationSha256 = $sourceCodeFingerprint
        engineVersion = (& $godotPath --version).Trim(); dotnetSdkVersion = (& (Join-Path $dotnetRoot 'dotnet.exe') --version).Trim()
        resourceCount = $packedPaths.Count; files = $files
        openGates = @('actual rendered exported candidate (source compatibility frames checked)', 'native keyboard, DPI and screen reader', 'clean Windows machine', 'minimum GPU', 'asset/runtime/font rights and notices', 'signing and distribution approval')
    }
    $metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputRoot 'candidate-manifest.json') -Encoding UTF8
    & (Join-Path $outputRoot 'Verify-Package.ps1') -PackageDirectory $outputRoot
    if ($LASTEXITCODE -ne 0) { throw 'Exported package integrity check failed.' }
    Write-Output "OFFLINE_DEMO_CANDIDATE_PASS $outputRoot"
    Write-Output 'Internal candidate only; clean-machine/native/distribution gates remain open.'
}
finally {
    $env:DOTNET_ROOT = $priorDotnetRoot
    $env:PATH = $priorPath
    $env:RungProofOfflineDemo = $priorOfflinePolicy
}
