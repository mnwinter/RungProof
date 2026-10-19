[CmdletBinding()]
param(
    [string]$EditorArtifactPath = '.tools/tia-parity-full-contract-v6-artifacts/tia-editor-control-gates.json',
    [string]$ProjectTreeArtifactPath = '.tools/tia-parity-full-contract-v6-artifacts/tia-project-tree-gates.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$registryPath = Join-Path $projectRoot 'docs\tia\editor-control-evidence-registry.json'

function Resolve-ProjectPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Path))
}

function ConvertTo-ProjectPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($projectRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Evidence artifact is outside the project root: $full"
    }
    return $full.Substring($projectRoot.Length).TrimStart('\', '/').Replace('\', '/')
}

function Read-And-ValidateArtifact(
    [string]$Path,
    [string]$CommandId,
    [string]$ResultKind,
    [string[]]$ExpectedGateIds
) {
    $full = Resolve-ProjectPath $Path
    if (-not [IO.File]::Exists($full)) { throw "Required structured artifact is missing: $full" }
    $artifact = Get-Content -LiteralPath $full -Raw | ConvertFrom-Json
    if (($artifact.resultKind -ne $ResultKind) -or ($artifact.commandId -ne $CommandId) -or
        ($artifact.status -ne 'passed') -or ($artifact.externalTia -ne $false) -or
        ($artifact.livePlc -ne $false)) {
        throw "Structured artifact has invalid identity, status, or execution boundaries: $full"
    }
    $gates = @($artifact.gates)
    $ids = @($gates | ForEach-Object { [string]$_.gateId })
    if (($gates.Count -ne $ExpectedGateIds.Count) -or
        (@($ids | Sort-Object -Unique).Count -ne $ExpectedGateIds.Count) -or
        (@(Compare-Object ($ExpectedGateIds | Sort-Object) ($ids | Sort-Object)).Count -ne 0)) {
        throw "Structured artifact does not contain the exact expected gate set: $full"
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
            throw "Structured artifact contains an incomplete or failed gate '$($gate.gateId)': $full"
        }
    }
    return [pscustomobject]@{
        FullPath = $full
        PortablePath = ConvertTo-ProjectPath $full
        Sha256 = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
        Artifact = $artifact
    }
}

$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
$editorIds = @($registry.gates | Where-Object group -eq 'editor' | ForEach-Object { [string]$_.id })
$treeIds = @($registry.gates | Where-Object group -eq 'project-tree' | ForEach-Object { [string]$_.id })
if ($editorIds.Count -ne 7 -or $treeIds.Count -ne 11) {
    throw "Registry must contain exactly 7 editor and 11 project-tree gates."
}

$editor = Read-And-ValidateArtifact $EditorArtifactPath 'tia-editor-control-gate-sweep' `
    'tia-editor-control-gate-sweep' $editorIds
$tree = Read-And-ValidateArtifact $ProjectTreeArtifactPath 'tia-project-tree-gate-sweep' `
    'tia-project-tree-gate-sweep' $treeIds
$totalAssertions = @(
    @($editor.Artifact.gates | ForEach-Object { @($_.assertions).Count })
    @($tree.Artifact.gates | ForEach-Object { @($_.assertions).Count })
) | Measure-Object -Sum | Select-Object -ExpandProperty Sum

$editorArtifactId = 'artifact.tia-editor-control-gate-sweep.v1'
$treeArtifactId = 'artifact.tia-project-tree-gate-sweep.v1'
$registry | Add-Member -Force NoteProperty evidenceArtifacts @(
    [pscustomobject][ordered]@{
        id = $editorArtifactId
        commandId = 'tia-editor-control-gate-sweep'
        path = $editor.PortablePath
        kind = 'json'
        durability = 'ephemeral-local'
        sha256 = $editor.Sha256
        status = 'passed'
        gateCount = 7
        assertionCount = @($editor.Artifact.gates.assertions).Count
        externalTia = $false
        livePlc = $false
    },
    [pscustomobject][ordered]@{
        id = $treeArtifactId
        commandId = 'tia-project-tree-gate-sweep'
        path = $tree.PortablePath
        kind = 'json'
        durability = 'ephemeral-local'
        sha256 = $tree.Sha256
        status = 'passed'
        gateCount = 11
        assertionCount = @($tree.Artifact.gates.assertions).Count
        externalTia = $false
        livePlc = $false
    }
)

foreach ($contract in $registry.structuredSweepContracts) {
    $contract.implemented = $true
    $artifactId = if ($contract.commandId -eq 'tia-editor-control-gate-sweep') { $editorArtifactId } else { $treeArtifactId }
    $contract | Add-Member -Force NoteProperty latestArtifactId $artifactId
}
foreach ($gate in $registry.gates) {
    $gate.localStatus = 'automated-pass'
    $gate.evidenceArtifactIds = @($(if ($gate.group -eq 'editor') { $editorArtifactId } else { $treeArtifactId }))
}
$registry.evidencePolicy.currentAssessment = "Exact structured production-path artifacts pass all 18 local gates ($totalAssertions assertions). These artifacts remain offline-only and do not establish installed-TIA, import/export, or live-PLC parity."

$json = $registry | ConvertTo-Json -Depth 100
[IO.File]::WriteAllText($registryPath, $json + "`n", [Text.UTF8Encoding]::new($false))
Write-Output "TIA_EDITOR_CONTROL_REGISTRY_UPDATE_PASS editor=7/7 editorAssertions=$(@($editor.Artifact.gates.assertions).Count) tree=11/11 treeAssertions=$(@($tree.Artifact.gates.assertions).Count)"

