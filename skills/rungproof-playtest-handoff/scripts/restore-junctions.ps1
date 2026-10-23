param(
    [Parameter(Mandatory = $true)][string]$Root,
    [switch]$Apply
)

# Restore recorded internal aliases after copying to NTFS. Preview by default.
# Existing directories are never removed or replaced.
$ErrorActionPreference = 'Stop'
$rpRoot = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Root).Path).TrimEnd('\', '/')
$rpPrefix = $rpRoot + [IO.Path]::DirectorySeparatorChar
$rpMapPath = Join-Path $PSScriptRoot '..\references\junction-map.json'
$rpMappings = Get-Content -LiteralPath $rpMapPath -Raw | ConvertFrom-Json

function Get-ContainedPath([string]$Relative) {
    if ([IO.Path]::IsPathRooted($Relative)) { throw 'Manifest path must be relative.' }
    $rpFull = [IO.Path]::GetFullPath((Join-Path $rpRoot $Relative))
    if (-not $rpFull.StartsWith($rpPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Manifest path escapes the restored checkout: $Relative"
    }
    return $rpFull
}

foreach ($rpEntry in $rpMappings) {
    $rpLink = Get-ContainedPath $rpEntry.path
    $rpTarget = Get-ContainedPath $rpEntry.target
    if (-not (Test-Path -LiteralPath $rpTarget -PathType Container)) {
        throw "Restore target data first: $rpTarget"
    }
    if (Test-Path -LiteralPath $rpLink) {
        $rpExisting = Get-Item -LiteralPath $rpLink -Force
        if ($rpExisting.LinkType -eq 'Junction' -and
            [IO.Path]::GetFullPath([string]@($rpExisting.Target)[0]).Equals(
                $rpTarget, [StringComparison]::OrdinalIgnoreCase)) {
            Write-Output "Already correct: $($rpEntry.path)"
            continue
        }
        throw "Existing path preserved; inspect it before restoring this alias: $rpLink"
    }
    Write-Output "Junction: $($rpEntry.path) -> $($rpEntry.target)"
    if ($Apply) {
        $rpParent = Split-Path -Parent $rpLink
        if (-not (Test-Path -LiteralPath $rpParent)) {
            New-Item -ItemType Directory -Path $rpParent | Out-Null
        }
        New-Item -ItemType Junction -Path $rpLink -Target $rpTarget | Out-Null
    }
}

if (-not $Apply) { Write-Output 'Preview only. Use -Apply on an NTFS restore to create missing aliases.' }
