[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$registryPath = Join-Path $projectRoot 'docs\tia\evidence-registry.json'
$localRunPath = Join-Path $projectRoot 'docs\tia\tia-local-run-evidence.json'
$selectedReviewPath = Join-Path $projectRoot 'docs\tia\tia-renderer-visual-review.json'
$offlineReviewPath = Join-Path $projectRoot 'docs\tia\tia-renderer-unselected-visual-review.json'

foreach ($required in @($registryPath, $localRunPath, $selectedReviewPath, $offlineReviewPath)) {
    if (-not [IO.File]::Exists($required)) {
        throw "Required TIA evidence file is missing: $required"
    }
}

$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
$siemensEntries = @($registry.entries | Where-Object supportClass -eq 'siemens-subset')
if ($siemensEntries.Count -ne 50) {
    throw "Expected exactly 50 Siemens-facing registry entries; found $($siemensEntries.Count)."
}

$localRunHash = (Get-FileHash -LiteralPath $localRunPath -Algorithm SHA256).Hash
$selectedReviewHash = (Get-FileHash -LiteralPath $selectedReviewPath -Algorithm SHA256).Hash
$offlineReviewHash = (Get-FileHash -LiteralPath $offlineReviewPath -Algorithm SHA256).Hash

foreach ($entry in $siemensEntries) {
    $key = [string]$entry.key
    $localArtifactId = "artifact.$key.local-run.v1"
    $selectedArtifactId = "artifact.$key.selected-review.v1"
    $offlineArtifactId = "artifact.$key.offline-review.v1"

    $offlineGate = $entry.gates | Where-Object state -eq 'offline'
    if ($null -eq $offlineGate) { throw "Entry '$key' has no offline gate." }
    $offlineGate.localStatus = 'automated-and-visual'
    $offlineGate.artifactIds = @($localArtifactId, $offlineArtifactId)

    $localArtifact = $entry.artifacts | Where-Object id -eq $localArtifactId
    $selectedArtifact = $entry.artifacts | Where-Object id -eq $selectedArtifactId
    if ($null -eq $localArtifact -or $null -eq $selectedArtifact) {
        throw "Entry '$key' is missing its existing local or selected evidence binding."
    }
    $localArtifact.sha256 = $localRunHash
    $selectedArtifact.sha256 = $selectedReviewHash

    $offlineArtifact = $entry.artifacts | Where-Object id -eq $offlineArtifactId
    if ($null -eq $offlineArtifact) {
        $offlineArtifact = [pscustomobject][ordered]@{
            id = $offlineArtifactId
            path = 'docs/tia/tia-renderer-unselected-visual-review.json'
            kind = 'json'
            durability = 'ephemeral-local'
            sha256 = $offlineReviewHash
        }
        $entry.artifacts = @($entry.artifacts) + @($offlineArtifact)
    }
    else {
        $offlineArtifact.path = 'docs/tia/tia-renderer-unselected-visual-review.json'
        $offlineArtifact.kind = 'json'
        $offlineArtifact.durability = 'ephemeral-local'
        $offlineArtifact.sha256 = $offlineReviewHash
    }
}

$json = $registry | ConvertTo-Json -Depth 100
[IO.File]::WriteAllText($registryPath, $json + "`n", [Text.UTF8Encoding]::new($false))

Write-Output "TIA_EVIDENCE_REGISTRY_UPDATE_PASS entries=$($siemensEntries.Count) local=$localRunHash selected=$selectedReviewHash offline=$offlineReviewHash"
