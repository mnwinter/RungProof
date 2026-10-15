[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string[]]$SceneIds = @(),
    [ValidateSet('scene','split')][string[]]$Views = @('scene','split')
)
$ErrorActionPreference = 'Stop'
$catalogCaptureRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$catalogOutput = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $catalogOutput) { throw 'Existing evidence directory preserved; use a fresh output directory.' }
[IO.Directory]::CreateDirectory($catalogOutput) | Out-Null
$catalogEntries = (Get-Content (Join-Path $catalogCaptureRoot 'scenes/catalog/original-scenes.catalog.json') -Raw | ConvertFrom-Json).scenes
if ($SceneIds.Count) {
    foreach ($id in $SceneIds) { if ($id -cnotin $catalogEntries.id) { throw "Unknown scene: $id" } }
    $catalogEntries = @($catalogEntries | Where-Object { $_.id -cin $SceneIds })
}
$catalogResults = [Collections.Generic.List[object]]::new()
$catalogIndex = 0
foreach ($entry in $catalogEntries) {
    $catalogIndex++
    foreach ($view in $Views) {
        $capturePath = Join-Path $catalogOutput ("{0:D2}-{1}.{2}.png" -f $catalogIndex,$entry.id,$view)
        # Captures are sequential: the existing launcher owns override.cfg and
        # the native viewport. Parallel captures would corrupt those shared states.
        & (Join-Path $PSScriptRoot 'capture_demo_nonfocus.ps1') -SceneId $entry.id -CapturePath $capturePath -View $view -State stopped
        $catalogResults.Add([ordered]@{
            sceneId=$entry.id; name=$entry.name; view=$view; image=$capturePath
            sha256=(Get-FileHash -LiteralPath $capturePath -Algorithm SHA256).Hash
            log=($capturePath + '.log'); renderer='gl_compatibility'; inspected=$false
            identity=($capturePath + '.identity.json')
        })
        $catalogResults | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $catalogOutput 'capture-manifest.json') -Encoding utf8
        Write-Output "CATALOG_CAPTURE_PROGRESS scene=$catalogIndex/$($catalogEntries.Count) view=$view id=$($entry.id)"
    }
}
Write-Output "CATALOG_CAPTURE_COMPLETE scenes=$($catalogEntries.Count) images=$($catalogResults.Count)"
