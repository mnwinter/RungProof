[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('fast', 'interaction', 'visual', 'full')]
    [string]$Profile = 'fast',

    [string]$OutputPath,

    [ValidateRange(30, 3600)]
    [int]$CommandTimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dotnetRoot = Join-Path $projectRoot '.tools\dotnet'
$dotnet = Join-Path $dotnetRoot 'dotnet.exe'
$godot = Join-Path $projectRoot '.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
$registryPath = Join-Path $projectRoot 'docs\tia\evidence-registry.json'
$editorControlRegistryPath = Join-Path $projectRoot 'docs\tia\editor-control-evidence-registry.json'
$defaultOutputDirectory = Join-Path $projectRoot '.tools\tia-parity-suite'
$runStarted = [DateTimeOffset]::UtcNow
$stamp = $runStarted.ToString('yyyyMMdd-HHmmss')

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputDirectory = $defaultOutputDirectory
    $resultPath = Join-Path $outputDirectory "tia-parity-$Profile-$stamp.json"
}
elseif ([IO.Path]::GetExtension($OutputPath).Equals('.json', [StringComparison]::OrdinalIgnoreCase)) {
    $resultPath = if ([IO.Path]::IsPathRooted($OutputPath)) {
        [IO.Path]::GetFullPath($OutputPath)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
    }
    $outputDirectory = Split-Path -Parent $resultPath
}
else {
    $outputDirectory = if ([IO.Path]::IsPathRooted($OutputPath)) {
        [IO.Path]::GetFullPath($OutputPath)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
    }
    $resultPath = Join-Path $outputDirectory "tia-parity-$Profile-$stamp.json"
}

if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    $outputDirectory = Get-Location
}
$resultStem = [IO.Path]::GetFileNameWithoutExtension($resultPath)
$logDirectory = Join-Path $outputDirectory "$resultStem-logs"
$artifactDirectory = Join-Path $outputDirectory "$resultStem-artifacts"
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
[IO.Directory]::CreateDirectory($artifactDirectory) | Out-Null

function ConvertTo-PortablePath {
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($projectRoot, [StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($projectRoot.Length).TrimStart('\', '/').Replace('\', '/')
    }
    return $fullPath.Replace('\', '/')
}

function Quote-NativeArgument {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    if ($Value.Length -gt 0 -and $Value -notmatch '[\s"]') {
        return $Value
    }
    # The suite arguments are paths and switches, not shell expressions. This
    # quoting is for CreateProcess/Start-Process only; no command shell is used.
    return '"' + $Value.Replace('"', '\"') + '"'
}

function New-SuiteCommand {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][ValidateSet('none', 'headless', 'normal-vulkan')][string]$Renderer,
        [string[]]$GateIds = @(),
        [string[]]$ArtifactPaths = @(),
        [string[]]$DependsOn = @()
    )

    [pscustomobject]@{
        Id = $Id
        Name = $Name
        Executable = $Executable
        Arguments = $Arguments
        Renderer = $Renderer
        GateIds = @($GateIds)
        ArtifactPaths = @($ArtifactPaths)
        DependsOn = @($DependsOn)
    }
}

function Test-StructuredGateArtifact {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$CommandId,
        [Parameter(Mandatory)][string]$ResultKind,
        [Parameter(Mandatory)][string[]]$ExpectedGateIds
    )

    $artifact = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ($artifact.resultKind -ne $ResultKind -or $artifact.commandId -ne $CommandId -or $artifact.status -ne 'passed') {
        throw "Structured artifact '$Path' has an invalid identity or non-passing status."
    }
    $gates = @($artifact.gates)
    $actualGateIds = @($gates | ForEach-Object { [string]$_.gateId })
    if (($gates.Count -ne $ExpectedGateIds.Count) -or
        (@($actualGateIds | Sort-Object -Unique).Count -ne $ExpectedGateIds.Count) -or
        (@(Compare-Object ($ExpectedGateIds | Sort-Object) ($actualGateIds | Sort-Object)).Count -ne 0)) {
        throw "Structured artifact '$Path' does not contain the exact expected gate-ID set."
    }
    foreach ($gate in $gates) {
        $assertions = @($gate.assertions)
        if (($gate.status -ne 'passed') -or ($gate.commandId -ne $CommandId) -or
            [string]::IsNullOrWhiteSpace([string]$gate.sourceRevision) -or
            ([string]$gate.assemblySha256 -notmatch '^[0-9A-Fa-f]{64}$') -or
            [string]::IsNullOrWhiteSpace([string]$gate.startedUtc) -or
            [string]::IsNullOrWhiteSpace([string]$gate.completedUtc) -or
            ($gate.externalTia -ne $false) -or ($gate.livePlc -ne $false) -or
            ($assertions.Count -eq 0) -or
            (@($assertions | Where-Object { $_.status -ne 'passed' -or $_.passed -ne $true }).Count -ne 0)) {
            throw "Structured artifact '$Path' has an incomplete or failed gate '$($gate.gateId)'."
        }
    }
}

$build = New-SuiteCommand -Id 'dotnet-build' -Name 'Build RungProof.Next' -Executable $dotnet `
    -Arguments @('build', (Join-Path $projectRoot 'RungProof.Next.csproj'), '--no-restore', '--nologo') -Renderer 'none'
$tests = New-SuiteCommand -Id 'controller-editor-tests' -Name 'Controller/editor regression suite' -Executable $dotnet `
    -Arguments @('run', '--project', (Join-Path $projectRoot 'tests\RungProof.Next.VirtualController.Tests.csproj'), '--no-restore') `
    -Renderer 'none' -DependsOn @('dotnet-build')

function New-GodotVerificationCommand {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Switch,
        [switch]$NativeRenderer,
        [string]$NativeResolution = '1600x900',
        [string[]]$GateIds = @(),
        [string[]]$ExtraUserArguments = @(),
        [string[]]$ArtifactPaths = @()
    )

    $engineArguments = @('--path', $projectRoot)
    $renderer = 'headless'
    if ($NativeRenderer) {
        $engineArguments += @('--resolution', $NativeResolution, '--position', '0,0')
        $renderer = 'normal-vulkan'
    }
    else {
        $engineArguments += '--headless'
    }
    $engineArguments += @('--', $Switch)
    $engineArguments += $ExtraUserArguments
    New-SuiteCommand -Id $Id -Name $Name -Executable $godot -Arguments $engineArguments `
        -Renderer $renderer -DependsOn @('dotnet-build') -GateIds $GateIds -ArtifactPaths $ArtifactPaths
}

$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
$rendererGateIds = @($registry.entries |
    Where-Object { $_.supportClass -eq 'siemens-subset' } |
    ForEach-Object { $_.gates } |
    Where-Object { $_.state -in @('offline', 'selected') } |
    ForEach-Object { [string]$_.id })
if ($rendererGateIds.Count -ne 100 -or @($rendererGateIds | Sort-Object -Unique).Count -ne 100) {
    throw "Universal renderer sweep requires exactly 100 unique offline/selected Siemens gate IDs; found $($rendererGateIds.Count)."
}
$monitorGateIds = @($registry.entries |
    Where-Object { $_.supportClass -eq 'siemens-subset' } |
    ForEach-Object { $_.gates } |
    Where-Object { $_.state -eq 'monitor' } |
    ForEach-Object { [string]$_.id })
if ($monitorGateIds.Count -ne 50 -or @($monitorGateIds | Sort-Object -Unique).Count -ne 50) {
    throw "Universal monitor sweep requires exactly 50 unique Siemens monitor gate IDs; found $($monitorGateIds.Count)."
}
$editGateIds = @($registry.entries |
    Where-Object { $_.supportClass -eq 'siemens-subset' } |
    ForEach-Object { $_.gates } |
    Where-Object { $_.state -eq 'edit' } |
    ForEach-Object { [string]$_.id })
if ($editGateIds.Count -ne 50 -or @($editGateIds | Sort-Object -Unique).Count -ne 50) {
    throw "Universal edit sweep requires exactly 50 unique Siemens edit gate IDs; found $($editGateIds.Count)."
}
$editorControlRegistry = Get-Content -LiteralPath $editorControlRegistryPath -Raw | ConvertFrom-Json
$editorControlGateIds = @($editorControlRegistry.gates |
    Where-Object group -eq 'editor' |
    ForEach-Object { [string]$_.id })
if ($editorControlGateIds.Count -ne 7 -or @($editorControlGateIds | Sort-Object -Unique).Count -ne 7) {
    throw "Editor control sweep requires exactly 7 unique gate IDs; found $($editorControlGateIds.Count)."
}
$projectTreeGateIds = @($editorControlRegistry.gates |
    Where-Object group -eq 'project-tree' |
    ForEach-Object { [string]$_.id })
if ($projectTreeGateIds.Count -ne 11 -or @($projectTreeGateIds | Sort-Object -Unique).Count -ne 11) {
    throw "Project-tree control sweep requires exactly 11 unique gate IDs; found $($projectTreeGateIds.Count)."
}
$monitorEvidence = Join-Path $artifactDirectory 'tia-monitor-gate-sweep.json'
$editEvidence = Join-Path $artifactDirectory 'tia-edit-gate-sweep.json'
$editorControlEvidence = Join-Path $artifactDirectory 'tia-editor-control-gates.json'
$projectTreeEvidence = Join-Path $artifactDirectory 'tia-project-tree-gates.json'

$interactionCommands = @(
    (New-GodotVerificationCommand -Id 'editor-transactions' -Name 'Editor transaction routes' -Switch '--verify-editor-transactions'),
    (New-GodotVerificationCommand -Id 'ladder-editor' -Name 'Ladder editor routes' -Switch '--verify-ladder-editor'),
    (New-GodotVerificationCommand -Id 'call-authoring' -Name 'Atomic FC/FB CALL authoring routes' -Switch '--verify-call-authoring'),
    (New-GodotVerificationCommand -Id 'operand-placement' -Name 'Physical operand and property-form routes' -Switch '--verify-operand-placement' -NativeRenderer),
    (New-GodotVerificationCommand -Id 'project-tree-pointer' -Name 'Physical project-tree routes' -Switch '--verify-project-tree-pointer' -NativeRenderer),
    (New-GodotVerificationCommand -Id 'tia-empty-box' -Name 'TIA empty-box replacements' -Switch '--verify-tia-empty-box'),
    (New-GodotVerificationCommand -Id 'tia-renderer-sweep' -Name 'All-key TIA renderer geometry sweep' `
        -Switch '--verify-tia-renderer-sweep' -NativeRenderer -GateIds $rendererGateIds),
    (New-GodotVerificationCommand -Id 'tia-monitor-gate-sweep' -Name 'All-key TIA monitor state sweep' `
        -Switch '--verify-tia-monitor-sweep' -GateIds $monitorGateIds `
        -ExtraUserArguments @("--monitor-evidence=$monitorEvidence") -ArtifactPaths @($monitorEvidence)),
    (New-GodotVerificationCommand -Id 'tia-edit-gate-sweep' -Name 'All-key TIA physical edit sweep' `
        -Switch '--verify-tia-edit-sweep' -NativeRenderer -GateIds $editGateIds `
        -ExtraUserArguments @("--edit-evidence=$editEvidence") -ArtifactPaths @($editEvidence)),
    (New-GodotVerificationCommand -Id 'tia-editor-control-gate-sweep' -Name 'TIA editor control gate sweep' `
        -Switch '--verify-tia-editor-control-gates' -NativeRenderer -GateIds $editorControlGateIds `
        -ExtraUserArguments @("--editor-control-evidence=$editorControlEvidence") -ArtifactPaths @($editorControlEvidence)),
    (New-GodotVerificationCommand -Id 'tia-project-tree-gate-sweep' -Name 'TIA project-tree control gate sweep' `
        -Switch '--verify-tia-project-tree-gates' -NativeRenderer -GateIds $projectTreeGateIds `
        -ExtraUserArguments @("--project-tree-evidence=$projectTreeEvidence") -ArtifactPaths @($projectTreeEvidence)),
    # The cross-vendor view-state fixture rebuilds and redraws both workbenches.
    # Godot's headless renderer can stop producing the UI frames it needs while
    # switching from TIA to Studio 5000, so keep this on the bounded native path.
    (New-GodotVerificationCommand -Id 'editor-view-state' -Name 'Editor block view-state routes' -Switch '--verify-editor-view-state' -NativeRenderer)
)

$zoomCapture = Join-Path $artifactDirectory 'tia-editor-zoom-nested-100.png'
$visualCommand = New-GodotVerificationCommand -Id 'editor-zoom-review' -Name 'Bounded editor zoom review' `
    -Switch '--editor-zoom-review' -NativeRenderer `
    -ExtraUserArguments @('--review-style=tia', '--review-zoom=100', '--review-fixture=nested', "--capture=$zoomCapture") `
    -ArtifactPaths @($zoomCapture)

$contactSheetArtifacts = [Collections.Generic.List[string]]::new()
foreach ($zoom in @(75, 100, 150)) {
    foreach ($family in @(
        [pscustomobject]@{ slug = 'bit-logic'; pages = 1 },
        [pscustomobject]@{ slug = 'timers'; pages = 1 },
        [pscustomobject]@{ slug = 'counters'; pages = 1 },
        [pscustomobject]@{ slug = 'program'; pages = 1 },
        [pscustomobject]@{ slug = 'compare'; pages = 1 },
        [pscustomobject]@{ slug = 'move-and-math'; pages = 3 }
    )) {
        for ($page = 1; $page -le $family.pages; $page++) {
            $contactSheetArtifacts.Add((Join-Path $artifactDirectory `
                ('tia-renderer-contact-{0}-z{1:D3}-p{2:D2}-of-{3:D2}.png' -f $family.slug, $zoom, $page, $family.pages)))
        }
    }
}
$contactManifest = Join-Path $artifactDirectory 'tia-renderer-contact-sheets-manifest.json'
$contactSheetArtifacts.Add($contactManifest)
$unselectedContactDirectory = Join-Path $artifactDirectory 'tia-renderer-unselected'
$unselectedContactSheetArtifacts = [Collections.Generic.List[string]]::new()
foreach ($zoom in @(75, 100, 150)) {
    foreach ($family in @(
        [pscustomobject]@{ slug = 'bit-logic'; pages = 1 },
        [pscustomobject]@{ slug = 'timers'; pages = 1 },
        [pscustomobject]@{ slug = 'counters'; pages = 1 },
        [pscustomobject]@{ slug = 'program'; pages = 1 },
        [pscustomobject]@{ slug = 'compare'; pages = 1 },
        [pscustomobject]@{ slug = 'move-and-math'; pages = 3 }
    )) {
        for ($page = 1; $page -le $family.pages; $page++) {
            $unselectedContactSheetArtifacts.Add((Join-Path $unselectedContactDirectory `
                ('tia-renderer-unselected-contact-{0}-z{1:D3}-p{2:D2}-of-{3:D2}.png' -f $family.slug, $zoom, $page, $family.pages)))
        }
    }
}
$unselectedContactManifest = Join-Path $unselectedContactDirectory 'tia-renderer-contact-sheets-manifest.json'
$unselectedContactSheetArtifacts.Add($unselectedContactManifest)
$allContactSheetArtifacts = @($contactSheetArtifacts) + @($unselectedContactSheetArtifacts)
$contactSheetCommand = New-GodotVerificationCommand -Id 'tia-renderer-contact-sheets' `
    -Name 'All-key TIA renderer contact sheets' -Switch '--verify-tia-renderer-sweep' -NativeRenderer `
    -NativeResolution '1600x1080' `
    -GateIds $rendererGateIds -ExtraUserArguments @("--contact-sheet-dir=$artifactDirectory", "--unselected-contact-sheet-dir=$unselectedContactDirectory") `
    -ArtifactPaths $allContactSheetArtifacts

$commands = switch ($Profile) {
    'fast' { @($build, $tests) }
    'interaction' { @($build, $tests) + $interactionCommands }
    'visual' { @($build, $contactSheetCommand, $visualCommand) }
    'full' { @($build, $tests) + @($interactionCommands | Where-Object { $_.Id -ne 'tia-renderer-sweep' }) + @($contactSheetCommand, $visualCommand) }
}

$preflight = [ordered]@{
    dotnetPath = ConvertTo-PortablePath $dotnet
    dotnetExists = Test-Path -LiteralPath $dotnet -PathType Leaf
    godotPath = ConvertTo-PortablePath $godot
    godotExists = Test-Path -LiteralPath $godot -PathType Leaf
}
$missingTools = @()
if (-not $preflight.dotnetExists) { $missingTools += $dotnet }
if ($commands.Executable -contains $godot -and -not $preflight.godotExists) { $missingTools += $godot }

$outcomes = [Collections.Generic.List[object]]::new()
$outcomeById = @{}
$priorDotnetRoot = $env:DOTNET_ROOT
$priorPath = $env:PATH

try {
    $env:DOTNET_ROOT = $dotnetRoot
    $env:PATH = "$dotnetRoot;$priorPath"

    foreach ($command in $commands) {
        $logPath = Join-Path $logDirectory ($command.Id + '.log')
        $stdoutPath = Join-Path $logDirectory ($command.Id + '.stdout.tmp')
        $stderrPath = Join-Path $logDirectory ($command.Id + '.stderr.tmp')
        $started = [DateTimeOffset]::UtcNow
        $status = 'passed'
        $exitCode = 0
        $message = $null

        $failedDependency = $command.DependsOn | Where-Object {
            $outcomeById.ContainsKey($_) -and $outcomeById[$_].status -ne 'passed'
        } | Select-Object -First 1

        if ($missingTools.Count -gt 0) {
            $status = 'skipped'
            $exitCode = $null
            $message = 'Preflight failed: missing bundled tool(s): ' + ($missingTools -join ', ')
            Set-Content -LiteralPath $logPath -Value $message -Encoding UTF8
        }
        elseif ($null -ne $failedDependency) {
            $status = 'skipped'
            $exitCode = $null
            $message = "Dependency '$failedDependency' did not pass."
            Set-Content -LiteralPath $logPath -Value $message -Encoding UTF8
        }
        else {
            $argumentLine = ($command.Arguments | ForEach-Object { Quote-NativeArgument ([string]$_) }) -join ' '
            @(
                "Command: $($command.Executable) $argumentLine"
                "Working directory: $projectRoot"
                "Started UTC: $($started.ToString('o'))"
                ''
            ) | Set-Content -LiteralPath $logPath -Encoding UTF8

            try {
                $process = Start-Process -FilePath $command.Executable -ArgumentList $argumentLine `
                    -WorkingDirectory $projectRoot -PassThru -NoNewWindow `
                    -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
                $completedWithinLimit = $process.WaitForExit($CommandTimeoutSeconds * 1000)
                if (-not $completedWithinLimit) {
                    try { $process.Kill() } catch { }
                    $process.WaitForExit()
                    $status = 'failed'
                    $exitCode = -1
                    $message = "Timed out after $CommandTimeoutSeconds seconds."
                }
                else {
                    $exitCode = $process.ExitCode
                    if ($exitCode -ne 0) { $status = 'failed' }
                }
            }
            catch {
                $status = 'failed'
                $exitCode = -1
                $message = $_.Exception.Message
            }
            finally {
                if (Test-Path -LiteralPath $stdoutPath) {
                    Get-Content -LiteralPath $stdoutPath | Add-Content -LiteralPath $logPath -Encoding UTF8
                    Remove-Item -LiteralPath $stdoutPath -Force
                }
                if (Test-Path -LiteralPath $stderrPath) {
                    $stderr = @(Get-Content -LiteralPath $stderrPath)
                    if ($stderr.Count -gt 0) {
                        @('', '--- STDERR ---') | Add-Content -LiteralPath $logPath -Encoding UTF8
                        $stderr | Add-Content -LiteralPath $logPath -Encoding UTF8
                    }
                    Remove-Item -LiteralPath $stderrPath -Force
                }
                if ($null -ne $message) {
                    @('', "Runner: $message") | Add-Content -LiteralPath $logPath -Encoding UTF8
                }
            }
        }

        $completed = [DateTimeOffset]::UtcNow
        $artifactRows = @($command.ArtifactPaths | ForEach-Object {
            [ordered]@{
                path = ConvertTo-PortablePath $_
                exists = Test-Path -LiteralPath $_ -PathType Leaf
            }
        })
        if ($status -eq 'passed' -and $artifactRows.Where({ -not $_.exists }).Count -gt 0) {
            $status = 'failed'
            $message = 'Command exited successfully but one or more declared artifacts were not created.'
            Add-Content -LiteralPath $logPath -Value "`nRunner: $message" -Encoding UTF8
        }
        if ($status -eq 'passed' -and $command.Id -eq 'tia-editor-control-gate-sweep') {
            try {
                Test-StructuredGateArtifact -Path $editorControlEvidence -CommandId $command.Id `
                    -ResultKind 'tia-editor-control-gate-sweep' -ExpectedGateIds $editorControlGateIds
            }
            catch {
                $status = 'failed'
                $message = $_.Exception.Message
                Add-Content -LiteralPath $logPath -Value "`nRunner: $message" -Encoding UTF8
            }
        }
        if ($status -eq 'passed' -and $command.Id -eq 'tia-project-tree-gate-sweep') {
            try {
                Test-StructuredGateArtifact -Path $projectTreeEvidence -CommandId $command.Id `
                    -ResultKind 'tia-project-tree-gate-sweep' -ExpectedGateIds $projectTreeGateIds
            }
            catch {
                $status = 'failed'
                $message = $_.Exception.Message
                Add-Content -LiteralPath $logPath -Value "`nRunner: $message" -Encoding UTF8
            }
        }

        $outcome = [ordered]@{
            id = $command.Id
            name = $command.Name
            status = $status
            exitCode = $exitCode
            durationMs = [long][Math]::Round(($completed - $started).TotalMilliseconds)
            startedUtc = $started.ToString('o')
            completedUtc = $completed.ToString('o')
            renderer = $command.Renderer
            executable = ConvertTo-PortablePath $command.Executable
            arguments = @($command.Arguments)
            gateIds = @($command.GateIds)
            logPath = ConvertTo-PortablePath $logPath
            artifacts = $artifactRows
            message = $message
        }
        $outcomes.Add([pscustomobject]$outcome)
        $outcomeById[$command.Id] = $outcome
        Write-Host ("[{0}] {1} ({2} ms)" -f $status.ToUpperInvariant(), $command.Id, $outcome.durationMs)
    }
}
finally {
    $env:DOTNET_ROOT = $priorDotnetRoot
    $env:PATH = $priorPath
}

$runCompleted = [DateTimeOffset]::UtcNow
$overallStatus = if ($outcomes.Count -gt 0 -and $outcomes.Where({ $_.status -eq 'passed' }).Count -eq $outcomes.Count) {
    'passed'
}
else {
    'failed'
}
$result = [ordered]@{
    schemaVersion = 1
    resultKind = 'tia-parity-suite-run'
    suiteVersion = 1
    profile = $Profile
    status = $overallStatus
    startedUtc = $runStarted.ToString('o')
    completedUtc = $runCompleted.ToString('o')
    durationMs = [long][Math]::Round(($runCompleted - $runStarted).TotalMilliseconds)
    repositoryRoot = $projectRoot.Replace('\', '/')
    resultPath = ConvertTo-PortablePath $resultPath
    registryPath = 'docs/tia/evidence-registry.json'
    offlineOnly = $true
    installedTiaExecuted = $false
    livePlcExecuted = $false
    preflight = $preflight
    commands = @($outcomes)
}

$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resultPath -Encoding UTF8
Write-Host "TIA parity suite $overallStatus. Result: $resultPath"

if ($overallStatus -ne 'passed') { exit 1 }
exit 0
